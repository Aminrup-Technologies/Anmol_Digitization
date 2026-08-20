// ========================================================================
// File:    TicketUpdateService.cs
// Layer:   BLL — Service
// When:    2026-08-20
// What:    Ticket workflow mutations — state transitions, reassignment,
//          priority override, approval, rejection, and comment.
// Why:     Centralizes all ticket state mutations in a single service
//          with mandatory atomic transactions + audit trail (requirement
//          §8, §12, §13). No mutations in code-behind.
// ========================================================================

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using AnmolDristi.ITSM.DAL;
using AnmolDristi.ITSM.DAL.Entities;

namespace AnmolDristi.ITSM.BLL
{
    /// <summary>
    /// Result envelope for all update operations.
    /// Success = true + null Error, or Success = false + Error message.
    /// </summary>
    public class TicketUpdateResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }

        public static TicketUpdateResult Ok()
        {
            return new TicketUpdateResult { Success = true };
        }

        public static TicketUpdateResult Fail(string error)
        {
            return new TicketUpdateResult { Success = false, Error = error };
        }
    }

    /// <summary>
    /// Workflow mutation service. Every public method validates tenant scope,
    /// state-machine rules, and business conditions before applying mutations.
    /// Ticket mutation + TicketAuditLog insertion share a single transaction (requirement §13).
    /// </summary>
    public class TicketUpdateService
    {
        // ---- State Machine Definition ----

        private static readonly Dictionary<string, List<string>> ValidTransitions =
            new Dictionary<string, List<string>>
            {
                { "In Queue", new List<string> { "In Progress", "Cancelled" } },
                { "New", new List<string> { "In Progress", "Cancelled" } },
                { "In Progress", new List<string> { "Resolved", "In Queue", "On Hold" } },
                { "On Hold", new List<string> { "In Progress", "Cancelled" } },
                { "Resolved", new List<string> { "Closed", "In Progress" } },
                { "Pending Approval", new List<string> { "In Progress", "Cancelled" } }
            };

        /// <summary>
        /// Transitions a ticket to a new status. Validates tenant ownership,
        /// state-machine rules, and required business conditions atomically.
        /// </summary>
        public TicketUpdateResult ChangeStatus(
            int companyId,
            int ticketId,
            int targetStatusId,
            string actorCode,
            string resolutionText = null)
        {
            using (var context = new ItsmDbContext())
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Load tenant-scoped ticket
                    var ticket = context.Tickets
                        .FirstOrDefault(t => t.Id == ticketId
                                            && t.Branch.CompanyId == companyId);

                    if (ticket == null)
                    {
                        return TicketUpdateResult.Fail("Ticket not found or access denied.");
                    }

                    // 2. Load target status
                    var targetStatus = context.TicketStatuses
                        .FirstOrDefault(s => s.Id == targetStatusId && s.IsActive);

                    if (targetStatus == null)
                    {
                        return TicketUpdateResult.Fail("Invalid target status.");
                    }

                    // 3. Load current status
                    var currentStatus = context.TicketStatuses
                        .FirstOrDefault(s => s.Id == ticket.StatusId);

                    if (currentStatus == null)
                    {
                        return TicketUpdateResult.Fail("Current ticket status is invalid.");
                    }

                    // 4. Validate state transition
                    if (currentStatus.IsTerminal)
                    {
                        return TicketUpdateResult.Fail(
                            "Cannot modify a ticket in terminal status: " + currentStatus.Name);
                    }

                    if (!ValidTransitions.ContainsKey(currentStatus.Name)
                        || !ValidTransitions[currentStatus.Name].Contains(targetStatus.Name))
                    {
                        return TicketUpdateResult.Fail(
                            "Invalid transition from '" + currentStatus.Name
                            + "' to '" + targetStatus.Name + "'.");
                    }

                    // 5. Business condition: Resolution requires text
                    if (targetStatus.Name == "Resolved"
                        && string.IsNullOrWhiteSpace(resolutionText))
                    {
                        return TicketUpdateResult.Fail(
                            "Resolution text is required when marking a ticket as Resolved.");
                    }

                    // 6. Apply mutation
                    var oldStatusName = currentStatus.Name;
                    ticket.StatusId = targetStatusId;
                    ticket.ModifiedOn = DateTime.Now;
                    ticket.ModifiedByCode = actorCode;

                    if (targetStatus.Name == "Resolved")
                    {
                        ticket.ResolutionText = resolutionText.Trim();
                        ticket.ResolvedOn = DateTime.Now;
                    }

                    if (targetStatus.Name == "Closed")
                    {
                        ticket.ClosedOn = DateTime.Now;
                    }

                    // 7. Atomic audit insertion (requirement §13)
                    context.TicketAuditLogs.Add(new TicketAuditLog
                    {
                        TicketId = ticketId,
                        CompanyId = companyId,
                        ActionType = "StatusChange",
                        OldValue = oldStatusName,
                        NewValue = targetStatus.Name,
                        ActorCode = actorCode,
                        ActionedOn = DateTime.Now
                    });

                    // 8. Commit atomically — both succeed or both rollback
                    context.SaveChanges();
                    transaction.Commit();

                    return TicketUpdateResult.Ok();
                }
                catch (DbUpdateConcurrencyException)
                {
                    transaction.Rollback();
                    return TicketUpdateResult.Fail(
                        "Concurrency conflict: this ticket was modified by another user. Please refresh and try again.");
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Reassigns a ticket to a different agent. Validates tenant ownership,
        /// target assignment, and records reassignment in audit trail.
        /// </summary>
        public TicketUpdateResult ReassignTicket(
            int companyId,
            int ticketId,
            string newAssigneeCode,
            string actorCode)
        {
            using (var context = new ItsmDbContext())
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Load tenant-scoped ticket
                    var ticket = context.Tickets
                        .FirstOrDefault(t => t.Id == ticketId
                                            && t.Branch.CompanyId == companyId);

                    if (ticket == null)
                    {
                        return TicketUpdateResult.Fail("Ticket not found or access denied.");
                    }

                    // 2. Validate target assignee exists
                    if (!string.IsNullOrWhiteSpace(newAssigneeCode))
                    {
                        var assigneeExists = context.UserMasters
                            .AsNoTracking()
                            .Any(u => u.EmployeeCode == newAssigneeCode);

                        if (!assigneeExists)
                        {
                            return TicketUpdateResult.Fail(
                                "Target assignee not found: " + newAssigneeCode);
                        }
                    }

                    // 3. Validate not terminal status
                    var currentStatus = context.TicketStatuses
                        .FirstOrDefault(s => s.Id == ticket.StatusId);

                    if (currentStatus != null && currentStatus.IsTerminal)
                    {
                        return TicketUpdateResult.Fail(
                            "Cannot reassign a ticket in terminal status.");
                    }

                    // 4. Apply mutation
                    var oldAssigneeCode = ticket.AssigneeCode;
                    ticket.AssigneeCode = string.IsNullOrWhiteSpace(newAssigneeCode)
                        ? null
                        : newAssigneeCode.Trim();
                    ticket.ModifiedOn = DateTime.Now;
                    ticket.ModifiedByCode = actorCode;

                    // 5. Atomic audit insertion
                    context.TicketAuditLogs.Add(new TicketAuditLog
                    {
                        TicketId = ticketId,
                        CompanyId = companyId,
                        ActionType = "Reassignment",
                        OldValue = oldAssigneeCode ?? "Unassigned",
                        NewValue = ticket.AssigneeCode ?? "Unassigned",
                        ActorCode = actorCode,
                        ActionedOn = DateTime.Now
                    });

                    // 6. Commit atomically
                    context.SaveChanges();
                    transaction.Commit();

                    return TicketUpdateResult.Ok();
                }
                catch (DbUpdateConcurrencyException)
                {
                    transaction.Rollback();
                    return TicketUpdateResult.Fail(
                        "Concurrency conflict: this ticket was modified by another user. Please refresh and try again.");
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Overrides the auto-calculated priority. Validates tenant ownership,
        /// applies new priority, and records the override in audit trail.
        /// </summary>
        public TicketUpdateResult OverridePriority(
            int companyId,
            int ticketId,
            int newPriorityId,
            string actorCode)
        {
            using (var context = new ItsmDbContext())
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Load tenant-scoped ticket
                    var ticket = context.Tickets
                        .FirstOrDefault(t => t.Id == ticketId
                                            && t.Branch.CompanyId == companyId);

                    if (ticket == null)
                    {
                        return TicketUpdateResult.Fail("Ticket not found or access denied.");
                    }

                    // 2. Validate target priority exists and is active
                    var newPriority = context.Priorities
                        .AsNoTracking()
                        .FirstOrDefault(p => p.Id == newPriorityId && p.IsActive);

                    if (newPriority == null)
                    {
                        return TicketUpdateResult.Fail("Invalid priority ID.");
                    }

                    // 3. Validate not terminal
                    var currentStatus = context.TicketStatuses
                        .FirstOrDefault(s => s.Id == ticket.StatusId);

                    if (currentStatus != null && currentStatus.IsTerminal)
                    {
                        return TicketUpdateResult.Fail(
                            "Cannot override priority on a closed ticket.");
                    }

                    // 4. Apply mutation
                    var oldPriorityId = ticket.PriorityId;
                    var oldPriorityName = context.Priorities
                        .AsNoTracking()
                        .Where(p => p.Id == oldPriorityId)
                        .Select(p => p.Name)
                        .FirstOrDefault() ?? oldPriorityId.ToString();

                    ticket.PriorityId = newPriorityId;
                    ticket.ModifiedOn = DateTime.Now;
                    ticket.ModifiedByCode = actorCode;

                    // 5. Atomic audit insertion (requirement §12)
                    context.TicketAuditLogs.Add(new TicketAuditLog
                    {
                        TicketId = ticketId,
                        CompanyId = companyId,
                        ActionType = "PriorityOverride",
                        OldValue = oldPriorityName,
                        NewValue = newPriority.Name,
                        ActorCode = actorCode,
                        ActionedOn = DateTime.Now
                    });

                    // 6. Commit atomically
                    context.SaveChanges();
                    transaction.Commit();

                    return TicketUpdateResult.Ok();
                }
                catch (DbUpdateConcurrencyException)
                {
                    transaction.Rollback();
                    return TicketUpdateResult.Fail(
                        "Concurrency conflict: this ticket was modified by another user. Please refresh and try again.");
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Approves a pending SR. Validates SoD (requester ≠ approver),
        /// pending approval status, and atomic audit.
        /// </summary>
        public TicketUpdateResult ApproveTicket(
            int companyId,
            int ticketId,
            string approverCode,
            string comments = null)
        {
            using (var context = new ItsmDbContext())
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Load tenant-scoped ticket
                    var ticket = context.Tickets
                        .FirstOrDefault(t => t.Id == ticketId
                                            && t.Branch.CompanyId == companyId);

                    if (ticket == null)
                    {
                        return TicketUpdateResult.Fail("Ticket not found or access denied.");
                    }

                    // 2. Validate pending approval exists for this approver
                    var approval = context.TicketApprovals
                        .FirstOrDefault(a =>
                            a.TicketId == ticketId
                            && a.ApproverCode == approverCode
                            && a.ApprovalStatus == "Pending");

                    if (approval == null)
                    {
                        return TicketUpdateResult.Fail(
                            "No pending approval found for this approver on this ticket.");
                    }

                    // 3. SoD: requester cannot approve their own ticket
                    if (string.Equals(
                        ticket.RequesterCode,
                        approverCode,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return TicketUpdateResult.Fail(
                            "Separation of Duties violation: requester cannot approve their own ticket.");
                    }

                    // 4. Apply approval
                    approval.ApprovalStatus = "Approved";
                    approval.ActionedOn = DateTime.Now;
                    approval.Comments = comments;

                    // 5. Transition ticket status to In Progress
                    var currentStatus = context.TicketStatuses
                        .FirstOrDefault(s => s.Id == ticket.StatusId);

                    if (currentStatus != null && currentStatus.Name == "Pending Approval")
                    {
                        var inProgressStatus = context.TicketStatuses
                            .FirstOrDefault(s => s.Name == "In Progress" && s.IsActive);

                        if (inProgressStatus != null)
                        {
                            ticket.StatusId = inProgressStatus.Id;
                        }
                    }

                    ticket.ModifiedOn = DateTime.Now;
                    ticket.ModifiedByCode = approverCode;

                    // 6. Atomic audit
                    context.TicketAuditLogs.Add(new TicketAuditLog
                    {
                        TicketId = ticketId,
                        CompanyId = companyId,
                        ActionType = "Approval",
                        NewValue = "Approved",
                        Detail = comments,
                        ActorCode = approverCode,
                        ActionedOn = DateTime.Now
                    });

                    // 7. Commit atomically
                    context.SaveChanges();
                    transaction.Commit();

                    return TicketUpdateResult.Ok();
                }
                catch (DbUpdateConcurrencyException)
                {
                    transaction.Rollback();
                    return TicketUpdateResult.Fail(
                        "Concurrency conflict: this ticket was modified by another user. Please refresh and try again.");
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Rejects a pending SR. Mandatory non-whitespace reason required (requirement §10).
        /// </summary>
        public TicketUpdateResult RejectTicket(
            int companyId,
            int ticketId,
            string approverCode,
            string reason)
        {
            // Requirement §10: rejection reason is mandatory — must not be null, empty, or whitespace.
            if (string.IsNullOrWhiteSpace(reason))
            {
                return TicketUpdateResult.Fail(
                    "Rejection reason is mandatory and cannot be empty or whitespace.");
            }

            using (var context = new ItsmDbContext())
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Load tenant-scoped ticket
                    var ticket = context.Tickets
                        .FirstOrDefault(t => t.Id == ticketId
                                            && t.Branch.CompanyId == companyId);

                    if (ticket == null)
                    {
                        return TicketUpdateResult.Fail("Ticket not found or access denied.");
                    }

                    // 2. Validate pending approval exists for this approver
                    var approval = context.TicketApprovals
                        .FirstOrDefault(a =>
                            a.TicketId == ticketId
                            && a.ApproverCode == approverCode
                            && a.ApprovalStatus == "Pending");

                    if (approval == null)
                    {
                        return TicketUpdateResult.Fail(
                            "No pending approval found for this approver on this ticket.");
                    }

                    // 3. Apply rejection
                    approval.ApprovalStatus = "Rejected";
                    approval.ActionedOn = DateTime.Now;
                    approval.Comments = reason.Trim();

                    // 4. Transition to Cancelled status
                    var cancelledStatus = context.TicketStatuses
                        .FirstOrDefault(s => s.Name == "Cancelled" && s.IsActive);

                    if (cancelledStatus != null)
                    {
                        ticket.StatusId = cancelledStatus.Id;
                    }

                    ticket.ModifiedOn = DateTime.Now;
                    ticket.ModifiedByCode = approverCode;

                    // 5. Atomic audit — reason is mandatory in the audit detail
                    context.TicketAuditLogs.Add(new TicketAuditLog
                    {
                        TicketId = ticketId,
                        CompanyId = companyId,
                        ActionType = "Rejection",
                        OldValue = "Pending Approval",
                        NewValue = "Cancelled",
                        Detail = reason.Trim(),
                        ActorCode = approverCode,
                        ActionedOn = DateTime.Now
                    });

                    // 6. Commit atomically
                    context.SaveChanges();
                    transaction.Commit();

                    return TicketUpdateResult.Ok();
                }
                catch (DbUpdateConcurrencyException)
                {
                    transaction.Rollback();
                    return TicketUpdateResult.Fail(
                        "Concurrency conflict: this ticket was modified by another user. Please refresh and try again.");
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Adds a comment to a ticket. Creates an audit record for the comment.
        /// </summary>
        public TicketUpdateResult AddComment(
            int companyId,
            int ticketId,
            string commentText,
            string actorCode,
            bool isInternal = false)
        {
            if (string.IsNullOrWhiteSpace(commentText))
            {
                return TicketUpdateResult.Fail("Comment text is required.");
            }

            using (var context = new ItsmDbContext())
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Verify ticket exists within tenant scope
                    var ticket = context.Tickets
                        .FirstOrDefault(t => t.Id == ticketId
                                            && t.Branch.CompanyId == companyId);

                    if (ticket == null)
                    {
                        return TicketUpdateResult.Fail("Ticket not found or access denied.");
                    }

                    // 2. Add comment
                    context.TicketComments.Add(new TicketComment
                    {
                        TicketId = ticketId,
                        CommentText = commentText.Trim(),
                        CreatedByCode = actorCode,
                        CreatedOn = DateTime.Now,
                        IsInternal = isInternal
                    });

                    // 3. Audit
                    context.TicketAuditLogs.Add(new TicketAuditLog
                    {
                        TicketId = ticketId,
                        CompanyId = companyId,
                        ActionType = "Comment",
                        Detail = isInternal ? "[Internal] " + commentText.Trim() : commentText.Trim(),
                        ActorCode = actorCode,
                        ActionedOn = DateTime.Now
                    });

                    // 4. Commit atomically
                    context.SaveChanges();
                    transaction.Commit();

                    return TicketUpdateResult.Ok();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
}
