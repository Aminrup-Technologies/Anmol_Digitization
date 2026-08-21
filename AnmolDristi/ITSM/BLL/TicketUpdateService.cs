// ========================================================================
// When: 2026-08-21
// What: Mutations require a ticket relationship (not non-terminal status);
//       NULL CompanyId blocks mutation (no tenant 0); audit copies
//       ticket.CompanyId; RowVersion on all six paths; reassignment is AssigneeCode only.
// Why: Arbitrary TicketID must not authorize. Historical NULL tenant must
//      not be invented as CompanyId 0.
// ========================================================================

using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using AnmolDristi.ITSM.DAL;
using AnmolDristi.ITSM.DAL.Entities;

namespace AnmolDristi.ITSM.BLL
{
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
    /// Ticket mutations + TicketAuditLog in one transaction.
    /// Authoritative tenant is Ticket.CompanyId (immutable snapshot).
    /// </summary>
    public class TicketUpdateService
    {
        private const string ConcurrencyMessage =
            "This ticket was modified by another agent. Please refresh and re-apply your changes.";

        public TicketUpdateResult ChangeStatus(
            int ticketId,
            int targetStatusId,
            string actorCode,
            string resolutionText,
            byte[] rowVersion)
        {
            using (var context = new ItsmDbContext())
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    string loadError;
                    var ticket = LoadAuthorizedTicket(context, ticketId, actorCode, rowVersion, out loadError);
                    if (ticket == null)
                    {
                        return TicketUpdateResult.Fail(loadError);
                    }

                    var targetStatus = context.TicketStatuses
                        .FirstOrDefault(s => s.Id == targetStatusId && s.IsActive);
                    var currentStatus = context.TicketStatuses
                        .FirstOrDefault(s => s.Id == ticket.StatusId);

                    if (targetStatus == null || currentStatus == null)
                    {
                        return TicketUpdateResult.Fail("Invalid ticket status.");
                    }

                    if (currentStatus.IsTerminal)
                    {
                        return TicketUpdateResult.Fail(
                            "Cannot modify a ticket in terminal status: " + currentStatus.Name);
                    }

                    if (!TicketWorkflow.IsValidTransition(currentStatus.Name, targetStatus.Name))
                    {
                        return TicketUpdateResult.Fail(
                            "Invalid transition from '" + currentStatus.Name
                            + "' to '" + targetStatus.Name + "'.");
                    }

                    if (targetStatus.Name == "Resolved"
                        && string.IsNullOrWhiteSpace(resolutionText))
                    {
                        return TicketUpdateResult.Fail(
                            "Resolution text is required when marking a ticket as Resolved.");
                    }

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

                    AddAudit(context, ticket, "StatusChange",
                        oldStatusName, targetStatus.Name, null, actorCode);

                    return Commit(context, transaction);
                }
                catch (DbUpdateConcurrencyException)
                {
                    transaction.Rollback();
                    return ConcurrencyFail();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public TicketUpdateResult ReassignTicket(
            int ticketId,
            string newAssigneeCode,
            string actorCode,
            byte[] rowVersion)
        {
            using (var context = new ItsmDbContext())
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    string loadError;
                    var ticket = LoadAuthorizedTicket(context, ticketId, actorCode, rowVersion, out loadError);
                    if (ticket == null)
                    {
                        return TicketUpdateResult.Fail(loadError);
                    }

                    if (!string.IsNullOrWhiteSpace(newAssigneeCode))
                    {
                        var assigneeOk = context.UserMasters
                            .AsNoTracking()
                            .Any(u => u.EmployeeCode == newAssigneeCode
                                      && u.EmployeeStatus == "Active");

                        if (!assigneeOk)
                        {
                            return TicketUpdateResult.Fail(
                                "Target assignee is not an active user.");
                        }
                    }

                    var currentStatus = context.TicketStatuses
                        .FirstOrDefault(s => s.Id == ticket.StatusId);
                    if (currentStatus != null && currentStatus.IsTerminal)
                    {
                        return TicketUpdateResult.Fail("Cannot reassign a ticket in terminal status.");
                    }

                    var oldAssigneeCode = ticket.AssigneeCode;
                    ticket.AssigneeCode = string.IsNullOrWhiteSpace(newAssigneeCode)
                        ? null
                        : newAssigneeCode.Trim();
                    ticket.ModifiedOn = DateTime.Now;
                    ticket.ModifiedByCode = actorCode;

                    AddAudit(context, ticket, "Reassignment",
                        oldAssigneeCode ?? "Unassigned",
                        ticket.AssigneeCode ?? "Unassigned",
                        null, actorCode);

                    return Commit(context, transaction);
                }
                catch (DbUpdateConcurrencyException)
                {
                    transaction.Rollback();
                    return ConcurrencyFail();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public TicketUpdateResult OverridePriority(
            int ticketId,
            int newPriorityId,
            string actorCode,
            string reason,
            byte[] rowVersion)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return TicketUpdateResult.Fail(
                    "Priority override requires a documented ITGC justification.");
            }

            using (var context = new ItsmDbContext())
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    string loadError;
                    var ticket = LoadAuthorizedTicket(context, ticketId, actorCode, rowVersion, out loadError);
                    if (ticket == null)
                    {
                        return TicketUpdateResult.Fail(loadError);
                    }

                    var newPriority = context.Priorities
                        .AsNoTracking()
                        .FirstOrDefault(p => p.Id == newPriorityId && p.IsActive);

                    if (newPriority == null)
                    {
                        return TicketUpdateResult.Fail("Invalid priority ID.");
                    }

                    var currentStatus = context.TicketStatuses
                        .FirstOrDefault(s => s.Id == ticket.StatusId);
                    if (currentStatus != null && currentStatus.IsTerminal)
                    {
                        return TicketUpdateResult.Fail("Cannot override priority on a closed ticket.");
                    }

                    var oldPriorityName = context.Priorities
                        .AsNoTracking()
                        .Where(p => p.Id == ticket.PriorityId)
                        .Select(p => p.Name)
                        .FirstOrDefault() ?? ticket.PriorityId.ToString();

                    ticket.PriorityId = newPriorityId;
                    ticket.ModifiedOn = DateTime.Now;
                    ticket.ModifiedByCode = actorCode;

                    AddAudit(context, ticket, "PriorityOverride",
                        oldPriorityName, newPriority.Name, reason.Trim(), actorCode);

                    return Commit(context, transaction);
                }
                catch (DbUpdateConcurrencyException)
                {
                    transaction.Rollback();
                    return ConcurrencyFail();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public TicketUpdateResult ApproveTicket(
            int ticketId,
            string approverCode,
            string comments,
            byte[] rowVersion)
        {
            using (var context = new ItsmDbContext())
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    string loadError;
                    var ticket = LoadAuthorizedTicket(context, ticketId, approverCode, rowVersion, out loadError);
                    if (ticket == null)
                    {
                        return TicketUpdateResult.Fail(loadError);
                    }

                    if (string.Equals(ticket.RequesterCode, approverCode, StringComparison.OrdinalIgnoreCase))
                    {
                        return TicketUpdateResult.Fail(
                            "Separation of Duties violation: requester cannot approve their own ticket.");
                    }

                    var currentStatus = context.TicketStatuses
                        .FirstOrDefault(s => s.Id == ticket.StatusId);
                    if (currentStatus == null || currentStatus.Name != "Pending Approval")
                    {
                        return TicketUpdateResult.Fail(
                            "Ticket is not awaiting approval.");
                    }

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

                    if (string.Equals(approval.ApprovalLevel, "ITHead", StringComparison.OrdinalIgnoreCase)
                        && context.TicketApprovals.Any(a =>
                            a.TicketId == ticketId
                            && a.ApprovalLevel == "Manager"
                            && a.ApprovalStatus == "Pending"))
                    {
                        return TicketUpdateResult.Fail(
                            "Manager approval must be completed before IT Head can approve.");
                    }

                    approval.ApprovalStatus = "Approved";
                    approval.ActionedOn = DateTime.Now;
                    approval.Comments = comments;

                    var stillPending = context.TicketApprovals
                        .Any(a => a.TicketId == ticketId
                                  && a.Id != approval.Id
                                  && a.ApprovalStatus == "Pending");

                    if (!stillPending)
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

                    AddAudit(context, ticket, "Approval",
                        "Pending", stillPending ? "Approved (partial)" : "Approved",
                        comments, approverCode);

                    return Commit(context, transaction);
                }
                catch (DbUpdateConcurrencyException)
                {
                    transaction.Rollback();
                    return ConcurrencyFail();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public TicketUpdateResult RejectTicket(
            int ticketId,
            string approverCode,
            string reason,
            byte[] rowVersion)
        {
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
                    string loadError;
                    var ticket = LoadAuthorizedTicket(context, ticketId, approverCode, rowVersion, out loadError);
                    if (ticket == null)
                    {
                        return TicketUpdateResult.Fail(loadError);
                    }

                    if (string.Equals(ticket.RequesterCode, approverCode, StringComparison.OrdinalIgnoreCase))
                    {
                        return TicketUpdateResult.Fail(
                            "Separation of Duties violation: requester cannot reject their own ticket.");
                    }

                    var currentStatus = context.TicketStatuses
                        .FirstOrDefault(s => s.Id == ticket.StatusId);
                    if (currentStatus == null || currentStatus.Name != "Pending Approval")
                    {
                        return TicketUpdateResult.Fail(
                            "Ticket is not awaiting approval.");
                    }

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

                    approval.ApprovalStatus = "Rejected";
                    approval.ActionedOn = DateTime.Now;
                    approval.Comments = reason.Trim();

                    var remainingPending = context.TicketApprovals
                        .Where(a => a.TicketId == ticketId
                                    && a.Id != approval.Id
                                    && a.ApprovalStatus == "Pending")
                        .ToList();
                    for (var i = 0; i < remainingPending.Count; i++)
                    {
                        remainingPending[i].ApprovalStatus = "Rejected";
                        remainingPending[i].ActionedOn = DateTime.Now;
                        remainingPending[i].Comments =
                            "Closed because the ticket was rejected at another approval level.";
                    }

                    var rejectedStatus = context.TicketStatuses
                        .FirstOrDefault(s => s.Name == "Rejected" && s.IsActive)
                        ?? context.TicketStatuses
                            .FirstOrDefault(s => s.Name == "Cancelled" && s.IsActive);

                    if (rejectedStatus != null)
                    {
                        ticket.StatusId = rejectedStatus.Id;
                    }

                    ticket.ModifiedOn = DateTime.Now;
                    ticket.ModifiedByCode = approverCode;

                    AddAudit(context, ticket, "Rejection",
                        "Pending Approval",
                        rejectedStatus != null ? rejectedStatus.Name : "Rejected",
                        reason.Trim(), approverCode);

                    return Commit(context, transaction);
                }
                catch (DbUpdateConcurrencyException)
                {
                    transaction.Rollback();
                    return ConcurrencyFail();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public TicketUpdateResult AddComment(
            int ticketId,
            string commentText,
            string actorCode,
            bool isInternal,
            byte[] rowVersion)
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
                    string loadError;
                    var ticket = LoadAuthorizedTicket(context, ticketId, actorCode, rowVersion, out loadError);
                    if (ticket == null)
                    {
                        return TicketUpdateResult.Fail(loadError);
                    }

                    if (isInternal
                        && string.Equals(ticket.RequesterCode, actorCode, StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(ticket.AssigneeCode, actorCode, StringComparison.OrdinalIgnoreCase))
                    {
                        return TicketUpdateResult.Fail(
                            "Requesters cannot add internal notes on their own tickets.");
                    }

                    context.TicketComments.Add(new TicketComment
                    {
                        TicketId = ticketId,
                        CommentText = commentText.Trim(),
                        CreatedByCode = actorCode,
                        CreatedOn = DateTime.Now,
                        IsInternal = isInternal
                    });

                    ticket.ModifiedOn = DateTime.Now;
                    ticket.ModifiedByCode = actorCode;

                    AddAudit(context, ticket, "Comment",
                        null, null,
                        (isInternal ? "[Internal] " : "") + commentText.Trim(),
                        actorCode);

                    return Commit(context, transaction);
                }
                catch (DbUpdateConcurrencyException)
                {
                    transaction.Rollback();
                    return ConcurrencyFail();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private static Ticket LoadAuthorizedTicket(
            ItsmDbContext context,
            int ticketId,
            string actorCode,
            byte[] rowVersion,
            out string error)
        {
            if (rowVersion == null || rowVersion.Length == 0)
            {
                error = ConcurrencyMessage;
                return null;
            }

            var ticket = context.Tickets.FirstOrDefault(t => t.Id == ticketId);
            if (ticket == null)
            {
                error = "Ticket not found or access denied.";
                return null;
            }

            if (!ItsmTicketAccess.CanAccess(
                context,
                ticket.Id,
                actorCode,
                ticket.RequesterCode,
                ticket.AssigneeCode))
            {
                error = "Ticket not found or access denied.";
                return null;
            }

            if (!ticket.CompanyId.HasValue)
            {
                error = "This ticket has no recorded tenant (CompanyId). "
                    + "Mutations are blocked until tenant ownership is remediated.";
                return null;
            }

            context.Entry(ticket).Property(x => x.RowVersion).OriginalValue = rowVersion;
            error = null;
            return ticket;
        }

        private static void AddAudit(
            ItsmDbContext context,
            Ticket ticket,
            string actionType,
            string oldValue,
            string newValue,
            string detail,
            string actorCode)
        {
            context.TicketAuditLogs.Add(new TicketAuditLog
            {
                TicketId = ticket.Id,
                CompanyId = ticket.CompanyId.Value,
                ActionType = Truncate(actionType, 50),
                OldValue = Truncate(oldValue, 200),
                NewValue = Truncate(newValue, 200),
                Detail = Truncate(detail, 1000),
                ActorCode = actorCode,
                ActionedOn = DateTime.Now
            });
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength);
        }

        private static TicketUpdateResult Commit(
            ItsmDbContext context,
            DbContextTransaction transaction)
        {
            context.SaveChanges();
            transaction.Commit();
            return TicketUpdateResult.Ok();
        }

        private static TicketUpdateResult ConcurrencyFail()
        {
            return TicketUpdateResult.Fail(ConcurrencyMessage);
        }
    }
}
