// ========================================================================
// When: 2026-08-21
// What: Process mutations use CanProcessTicket; Approve/Reject use
//       CanApproveTicket; NULL CompanyId blocks mutation; audit copies
//       ticket.CompanyId.Value; RowVersion on all mutation paths.
//       2026-11-25: Assignee processes. Requester cancels early and confirms
//       or returns a resolution. Hold class, reopen reason, reassignment
//       reason, and the resolution summary are refused when they will not
//       fit the audit row. State and audit commit together.
// Why: Pending/historical approvers must not gain processing rights.
//      A state change without its audit row must not commit.
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
        private const int AuditDetailMax = 1000;
        private const int ApprovalCommentMax = 500;
        public const string ConcurrencyMessage =
            "This ticket was modified by another agent. Please refresh and re-apply your changes.";

        public TicketUpdateResult ChangeStatus(
            int ticketId,
            int targetStatusId,
            string actorCode,
            string resolutionText,
            byte[] rowVersion,
            string holdClass)
        {
            using (var context = new ItsmDbContext())
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    string loadError;
                    var ticket = LoadMutableTicket(context, ticketId, actorCode, rowVersion, out loadError);
                    if (ticket == null)
                    {
                        return TicketUpdateResult.Fail(loadError);
                    }

                    ticket = RequireRecordedTenant(ticket, out loadError);
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

                    var allowed = TicketActionPolicy.AllowedTargetNames(
                        currentStatus.Name,
                        currentStatus.IsTerminal,
                        actorCode,
                        ticket.RequesterCode,
                        ticket.AssigneeCode);
                    if (!allowed.Contains(targetStatus.Name))
                    {
                        return TicketUpdateResult.Fail(
                            "Invalid transition from '" + currentStatus.Name
                            + "' to '" + targetStatus.Name + "'.");
                    }

                    var now = DateTime.Now;
                    string detail = null;
                    var text = resolutionText == null ? string.Empty : resolutionText.Trim();

                    if (targetStatus.Name == "Resolved")
                    {
                        if (text.Length == 0)
                        {
                            return TicketUpdateResult.Fail(
                                "Resolution text is required when marking a ticket as Resolved.");
                        }

                        if (text.Length > AuditDetailMax)
                        {
                            return TicketUpdateResult.Fail(
                                "Resolution summary must be 1000 characters or fewer.");
                        }

                        var previous = ticket.ResolutionText == null
                            ? string.Empty
                            : ticket.ResolutionText.Trim();
                        if (previous.Length > AuditDetailMax)
                        {
                            return TicketUpdateResult.Fail(
                                "The previous resolution is longer than the audit detail can store. It was not replaced.");
                        }

                        if (previous.Length > 0)
                        {
                            var previousError = AddAudit(
                                context, ticket, "Resolution", null, "Superseded", previous, actorCode, now);
                            if (previousError != null)
                            {
                                return TicketUpdateResult.Fail(previousError);
                            }
                        }

                        ticket.ResolutionText = text;
                        ticket.ResolvedOn = now;
                        detail = text;
                    }
                    else if (targetStatus.Name == "On Hold")
                    {
                        if (!TicketActionPolicy.IsHoldClass(holdClass))
                        {
                            return TicketUpdateResult.Fail(
                                "On Hold requires a reason class of Requester, Vendor, or Internal.");
                        }

                        detail = TicketActionPolicy.HoldDetail(holdClass);
                    }
                    else if (targetStatus.Name == "Cancelled")
                    {
                        if (text.Length == 0)
                        {
                            return TicketUpdateResult.Fail("A reason is required to cancel a ticket.");
                        }

                        if (text.Length > AuditDetailMax)
                        {
                            return TicketUpdateResult.Fail(
                                "Cancellation reason must be 1000 characters or fewer.");
                        }

                        if (currentStatus.Name == "Pending Approval")
                        {
                            var granted = context.TicketApprovals.Any(a =>
                                a.TicketId == ticket.Id && a.ApprovalStatus == "Approved");
                            if (granted)
                            {
                                return TicketUpdateResult.Fail(
                                    "The ticket cannot be cancelled after an approval has been granted.");
                            }

                            var pendingRows = context.TicketApprovals
                                .Where(a => a.TicketId == ticket.Id && a.ApprovalStatus == "Pending")
                                .ToList();
                            for (var i = 0; i < pendingRows.Count; i++)
                            {
                                pendingRows[i].ApprovalStatus = "Rejected";
                                pendingRows[i].ActionedOn = now;
                                pendingRows[i].Comments = "Closed because the requester cancelled the ticket.";
                            }
                        }

                        detail = text;
                    }
                    else if (targetStatus.Name == "In Progress" && currentStatus.Name == "Resolved")
                    {
                        if (text.Length == 0)
                        {
                            return TicketUpdateResult.Fail(
                                "A reason is required to return a resolved ticket to processing.");
                        }

                        if (text.Length > AuditDetailMax)
                        {
                            return TicketUpdateResult.Fail(
                                "Reopen reason must be 1000 characters or fewer.");
                        }

                        // ResolutionText and ResolvedOn stay. There is no stored SLA to restart.
                        detail = text;
                    }
                    else if (targetStatus.Name == "Closed")
                    {
                        ticket.ClosedOn = now;
                    }

                    var oldStatusName = currentStatus.Name;
                    ticket.StatusId = targetStatus.Id;
                    ticket.ModifiedOn = now;
                    ticket.ModifiedByCode = actorCode;

                    var auditError = AddAudit(
                        context, ticket, "StatusChange", oldStatusName, targetStatus.Name, detail, actorCode, now);
                    if (auditError != null)
                    {
                        return TicketUpdateResult.Fail(auditError);
                    }

                    NotifyStatus(context, ticket, currentStatus.Name, targetStatus.Name, holdClass);
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

        public TicketUpdateResult TakeOwnership(int ticketId, string actorCode, byte[] rowVersion)
        {
            using (var context = new ItsmDbContext())
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    string loadError;
                    var ticket = LoadMutableTicket(context, ticketId, actorCode, rowVersion, out loadError);
                    if (ticket == null)
                    {
                        return TicketUpdateResult.Fail(loadError);
                    }

                    ticket = RequireRecordedTenant(ticket, out loadError);
                    if (ticket == null)
                    {
                        return TicketUpdateResult.Fail(loadError);
                    }

                    var currentStatus = context.TicketStatuses
                        .FirstOrDefault(s => s.Id == ticket.StatusId);
                    if (currentStatus == null || currentStatus.IsTerminal)
                    {
                        return TicketUpdateResult.Fail("Cannot take a ticket in terminal status.");
                    }

                    if (!string.IsNullOrWhiteSpace(ticket.AssigneeCode))
                    {
                        return TicketUpdateResult.Fail("This ticket already has an assignee.");
                    }

                    var actorActive = context.UserMasters.AsNoTracking().Any(u =>
                        u.EmployeeCode == actorCode && u.EmployeeStatus == "Active");
                    if (!actorActive)
                    {
                        return TicketUpdateResult.Fail("Only an active employee can take a ticket.");
                    }

                    var mappedAgent = DispatchService.IsMappedAgent(
                        context,
                        ticket.BranchId,
                        ticket.CategoryId,
                        ticket.SubCategoryId,
                        ticket.ItemId,
                        actorCode);
                    var groupTl = ticket.SupportGroupId.HasValue
                        && ticket.CompanyId.HasValue
                        && ItsmTicketAccess.IsActiveTeamLeader(
                            context, ticket.SupportGroupId.Value, actorCode, ticket.CompanyId.Value);
                    if (!mappedAgent && !groupTl)
                    {
                        return TicketUpdateResult.Fail("Ticket not found or access denied.");
                    }

                    var now = DateTime.Now;
                    ticket.AssigneeCode = actorCode.Trim();
                    ticket.ModifiedOn = now;
                    ticket.ModifiedByCode = actorCode;

                    var auditError = AddAudit(
                        context, ticket, "Reassignment", "Unassigned", ticket.AssigneeCode, "Accepted", actorCode, now);
                    if (auditError != null)
                    {
                        return TicketUpdateResult.Fail(auditError);
                    }

                    ItsmNotification.EnqueueForEmployee(
                        context,
                        ticket.Id,
                        ticket.AssigneeCode,
                        "ITSM Ticket Assigned: " + ticket.TicketNumber,
                        "You took ticket " + ticket.TicketNumber + ".");
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
            byte[] rowVersion,
            string reason)
        {
            if (string.IsNullOrWhiteSpace(newAssigneeCode))
            {
                return TicketUpdateResult.Fail("Select an active employee to reassign to.");
            }

            using (var context = new ItsmDbContext())
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    string loadError;
                    var ticket = LoadMutableTicket(context, ticketId, actorCode, rowVersion, out loadError);
                    if (ticket == null)
                    {
                        return TicketUpdateResult.Fail(loadError);
                    }

                    ticket = RequireRecordedTenant(ticket, out loadError);
                    if (ticket == null)
                    {
                        return TicketUpdateResult.Fail(loadError);
                    }

                    var isGroupTlAssign = string.IsNullOrWhiteSpace(ticket.AssigneeCode)
                        && ticket.SupportGroupId.HasValue
                        && ItsmTicketAccess.IsActiveTeamLeader(
                            context, ticket.SupportGroupId.Value, actorCode, ticket.CompanyId.Value);

                    if (!isGroupTlAssign
                        && !ItsmTicketAccess.CanProcessTicket(actorCode, ticket.AssigneeCode))
                    {
                        return TicketUpdateResult.Fail("Ticket not found or access denied.");
                    }

                    var reassignReason = reason == null ? string.Empty : reason.Trim();
                    if (isGroupTlAssign)
                    {
                        if (reassignReason.Length == 0)
                        {
                            reassignReason = "GroupAssign";
                        }
                    }
                    else if (reassignReason.Length == 0)
                    {
                        return TicketUpdateResult.Fail("Reassignment requires a reason.");
                    }

                    if (reassignReason.Length > AuditDetailMax)
                    {
                        return TicketUpdateResult.Fail("Reassignment reason must be 1000 characters or fewer.");
                    }

                    var currentStatus = context.TicketStatuses
                        .FirstOrDefault(s => s.Id == ticket.StatusId);
                    if (currentStatus == null || currentStatus.IsTerminal
                        || (!isGroupTlAssign && currentStatus.Name == "Pending Approval"))
                    {
                        return TicketUpdateResult.Fail("Cannot reassign this ticket.");
                    }

                    var assignee = context.UserMasters.AsNoTracking()
                        .Where(u => u.EmployeeCode == newAssigneeCode && u.EmployeeStatus == "Active")
                        .Select(u => new { u.CompanyId })
                        .FirstOrDefault();
                    if (assignee == null)
                    {
                        return TicketUpdateResult.Fail("Target assignee is not an active user.");
                    }

                    if (!assignee.CompanyId.HasValue
                        || !ticket.CompanyId.HasValue
                        || assignee.CompanyId.Value != ticket.CompanyId.Value)
                    {
                        return TicketUpdateResult.Fail("Ticket not found or access denied.");
                    }

                    if (isGroupTlAssign
                        && !ItsmTicketAccess.IsActiveGroupMember(
                            context, ticket.SupportGroupId.Value, newAssigneeCode.Trim(), ticket.CompanyId.Value))
                    {
                        return TicketUpdateResult.Fail(
                            "Target must be an active member of the support group.");
                    }

                    if (TicketActionPolicy.SameCode(ticket.AssigneeCode, newAssigneeCode))
                    {
                        return TicketUpdateResult.Fail("The ticket is already assigned to that employee.");
                    }

                    var now = DateTime.Now;
                    var oldAssigneeCode = ticket.AssigneeCode;
                    ticket.AssigneeCode = newAssigneeCode.Trim();
                    ticket.ModifiedOn = now;
                    ticket.ModifiedByCode = actorCode;

                    var auditError = AddAudit(
                        context,
                        ticket,
                        "Reassignment",
                        string.IsNullOrWhiteSpace(oldAssigneeCode) ? "Unassigned" : oldAssigneeCode,
                        ticket.AssigneeCode,
                        reassignReason,
                        actorCode,
                        now);
                    if (auditError != null)
                    {
                        return TicketUpdateResult.Fail(auditError);
                    }

                    ItsmNotification.EnqueueForEmployee(
                        context,
                        ticket.Id,
                        ticket.AssigneeCode,
                        "ITSM Ticket Reassigned: " + ticket.TicketNumber,
                        "Ticket " + ticket.TicketNumber + " has been assigned to you.");
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
            var overrideReason = reason == null ? string.Empty : reason.Trim();
            if (overrideReason.Length == 0)
            {
                return TicketUpdateResult.Fail(
                    "Priority override requires a documented ITGC justification.");
            }

            if (overrideReason.Length > AuditDetailMax)
            {
                return TicketUpdateResult.Fail(
                    "Priority override reason must be 1000 characters or fewer.");
            }

            using (var context = new ItsmDbContext())
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    string loadError;
                    var ticket = LoadForProcess(context, ticketId, actorCode, rowVersion, out loadError);
                    if (ticket == null)
                    {
                        return TicketUpdateResult.Fail(loadError);
                    }

                    var newPriority = context.Priorities.AsNoTracking()
                        .FirstOrDefault(p => p.Id == newPriorityId && p.IsActive);
                    if (newPriority == null)
                    {
                        return TicketUpdateResult.Fail("Invalid priority ID.");
                    }

                    var currentStatus = context.TicketStatuses
                        .FirstOrDefault(s => s.Id == ticket.StatusId);
                    if (currentStatus == null || currentStatus.IsTerminal || currentStatus.Name == "Pending Approval")
                    {
                        return TicketUpdateResult.Fail("Cannot override priority on this ticket.");
                    }

                    var oldPriorityName = context.Priorities.AsNoTracking()
                        .Where(p => p.Id == ticket.PriorityId)
                        .Select(p => p.Name)
                        .FirstOrDefault() ?? ticket.PriorityId.ToString();

                    var now = DateTime.Now;
                    ticket.PriorityId = newPriorityId;
                    ticket.ModifiedOn = now;
                    ticket.ModifiedByCode = actorCode;

                    var auditError = AddAudit(
                        context, ticket, "PriorityOverride", oldPriorityName, newPriority.Name, overrideReason, actorCode, now);
                    if (auditError != null)
                    {
                        return TicketUpdateResult.Fail(auditError);
                    }

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
            if (comments != null && comments.Length > ApprovalCommentMax)
            {
                return TicketUpdateResult.Fail("Approval comments must be 500 characters or fewer.");
            }

            using (var context = new ItsmDbContext())
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    string loadError;
                    var ticket = LoadForApprove(context, ticketId, approverCode, rowVersion, out loadError);
                    if (ticket == null)
                    {
                        return TicketUpdateResult.Fail(loadError);
                    }

                    if (TicketActionPolicy.SameCode(ticket.RequesterCode, approverCode))
                    {
                        return TicketUpdateResult.Fail(
                            "Separation of Duties violation: requester cannot approve their own ticket.");
                    }

                    var currentStatus = context.TicketStatuses
                        .FirstOrDefault(s => s.Id == ticket.StatusId);
                    if (currentStatus == null || currentStatus.Name != "Pending Approval")
                    {
                        return TicketUpdateResult.Fail("Ticket is not awaiting approval.");
                    }

                    var approval = context.TicketApprovals.FirstOrDefault(a =>
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

                    var now = DateTime.Now;
                    approval.ApprovalStatus = "Approved";
                    approval.ActionedOn = now;
                    approval.Comments = comments;

                    var stillPending = context.TicketApprovals.Any(a =>
                        a.TicketId == ticketId
                        && a.Id != approval.Id
                        && a.ApprovalStatus == "Pending");

                    if (!stillPending)
                    {
                        var inProgressStatus = context.TicketStatuses
                            .FirstOrDefault(s => s.Name == "In Progress" && s.IsActive);
                        if (inProgressStatus == null)
                        {
                            return TicketUpdateResult.Fail(
                                "In Progress is not an active status. The approval was not saved.");
                        }

                        ticket.StatusId = inProgressStatus.Id;
                    }

                    ticket.ModifiedOn = now;
                    ticket.ModifiedByCode = approverCode;

                    var auditError = AddAudit(
                        context,
                        ticket,
                        "Approval",
                        "Pending",
                        stillPending ? "Approved (partial)" : "Approved",
                        comments,
                        approverCode,
                        now);
                    if (auditError != null)
                    {
                        return TicketUpdateResult.Fail(auditError);
                    }

                    if (!stillPending)
                    {
                        ItsmNotification.EnqueueForEmployee(
                            context,
                            ticket.Id,
                            ticket.RequesterCode,
                            "ITSM Ticket Approved: " + ticket.TicketNumber,
                            "Ticket " + ticket.TicketNumber + " was approved.");
                        if (!TicketActionPolicy.SameCode(ticket.RequesterCode, ticket.AssigneeCode))
                        {
                            ItsmNotification.EnqueueForEmployee(
                                context,
                                ticket.Id,
                                ticket.AssigneeCode,
                                "ITSM Ticket Approved: " + ticket.TicketNumber,
                                "Ticket " + ticket.TicketNumber + " was approved and is ready for work.");
                        }
                    }

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
            var rejectionReason = reason == null ? string.Empty : reason.Trim();
            if (rejectionReason.Length == 0)
            {
                return TicketUpdateResult.Fail(
                    "Rejection reason is mandatory and cannot be empty or whitespace.");
            }

            if (rejectionReason.Length > ApprovalCommentMax)
            {
                return TicketUpdateResult.Fail("Rejection reason must be 500 characters or fewer.");
            }

            using (var context = new ItsmDbContext())
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    string loadError;
                    var ticket = LoadForApprove(context, ticketId, approverCode, rowVersion, out loadError);
                    if (ticket == null)
                    {
                        return TicketUpdateResult.Fail(loadError);
                    }

                    if (TicketActionPolicy.SameCode(ticket.RequesterCode, approverCode))
                    {
                        return TicketUpdateResult.Fail(
                            "Separation of Duties violation: requester cannot reject their own ticket.");
                    }

                    var currentStatus = context.TicketStatuses
                        .FirstOrDefault(s => s.Id == ticket.StatusId);
                    if (currentStatus == null || currentStatus.Name != "Pending Approval")
                    {
                        return TicketUpdateResult.Fail("Ticket is not awaiting approval.");
                    }

                    var approval = context.TicketApprovals.FirstOrDefault(a =>
                        a.TicketId == ticketId
                        && a.ApproverCode == approverCode
                        && a.ApprovalStatus == "Pending");
                    if (approval == null)
                    {
                        return TicketUpdateResult.Fail(
                            "No pending approval found for this approver on this ticket.");
                    }

                    var now = DateTime.Now;
                    approval.ApprovalStatus = "Rejected";
                    approval.ActionedOn = now;
                    approval.Comments = rejectionReason;

                    var remainingPending = context.TicketApprovals
                        .Where(a => a.TicketId == ticketId
                                    && a.Id != approval.Id
                                    && a.ApprovalStatus == "Pending")
                        .ToList();
                    for (var i = 0; i < remainingPending.Count; i++)
                    {
                        remainingPending[i].ApprovalStatus = "Rejected";
                        remainingPending[i].ActionedOn = now;
                        remainingPending[i].Comments =
                            "Closed because the ticket was rejected at another approval level.";
                    }

                    var rejectedStatus = context.TicketStatuses
                        .FirstOrDefault(s => s.Name == "Rejected" && s.IsActive)
                        ?? context.TicketStatuses
                            .FirstOrDefault(s => s.Name == "Cancelled" && s.IsActive);
                    if (rejectedStatus == null)
                    {
                        return TicketUpdateResult.Fail(
                            "Rejected is not an active status. The rejection was not saved.");
                    }

                    ticket.StatusId = rejectedStatus.Id;
                    ticket.ModifiedOn = now;
                    ticket.ModifiedByCode = approverCode;

                    var auditError = AddAudit(
                        context,
                        ticket,
                        "Rejection",
                        "Pending Approval",
                        rejectedStatus.Name,
                        rejectionReason,
                        approverCode,
                        now);
                    if (auditError != null)
                    {
                        return TicketUpdateResult.Fail(auditError);
                    }

                    ItsmNotification.EnqueueForEmployee(
                        context,
                        ticket.Id,
                        ticket.RequesterCode,
                        "ITSM Ticket Rejected: " + ticket.TicketNumber,
                        "Ticket " + ticket.TicketNumber + " was rejected. Reason: " + rejectionReason);
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
            var text = commentText == null ? string.Empty : commentText.Trim();
            if (text.Length == 0)
            {
                return TicketUpdateResult.Fail("Comment text is required.");
            }

            var detail = (isInternal ? "[Internal] " : string.Empty) + text;
            if (detail.Length > AuditDetailMax)
            {
                return TicketUpdateResult.Fail(
                    isInternal
                        ? "An internal note must be 989 characters or fewer so the audit copy is complete."
                        : "A comment must be 1000 characters or fewer so the audit copy is complete.");
            }

            using (var context = new ItsmDbContext())
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    string loadError;
                    var ticket = LoadMutableTicket(context, ticketId, actorCode, rowVersion, out loadError);
                    if (ticket == null)
                    {
                        return TicketUpdateResult.Fail(loadError);
                    }

                    ticket = RequireRecordedTenant(ticket, out loadError);
                    if (ticket == null)
                    {
                        return TicketUpdateResult.Fail(loadError);
                    }

                    if (!ItsmTicketAccess.CanComment(actorCode, ticket.RequesterCode, ticket.AssigneeCode))
                    {
                        loadError = "Ticket not found or access denied.";
                        return TicketUpdateResult.Fail(loadError);
                    }

                    if (isInternal && !ItsmTicketAccess.CanProcessTicket(actorCode, ticket.AssigneeCode))
                    {
                        return TicketUpdateResult.Fail(
                            "Requesters cannot add internal notes on their own tickets.");
                    }

                    var now = DateTime.Now;
                    context.TicketComments.Add(new TicketComment
                    {
                        TicketId = ticketId,
                        CommentText = text,
                        CreatedByCode = actorCode,
                        CreatedOn = now,
                        IsInternal = isInternal
                    });
                    ticket.ModifiedOn = now;
                    ticket.ModifiedByCode = actorCode;

                    var auditError = AddAudit(context, ticket, "Comment", null, null, detail, actorCode, now);
                    if (auditError != null)
                    {
                        return TicketUpdateResult.Fail(auditError);
                    }

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

        private static void NotifyStatus(
            ItsmDbContext context,
            Ticket ticket,
            string fromStatus,
            string toStatus,
            string holdClass)
        {
            if (toStatus == "On Hold" && TicketActionPolicy.IsHoldClass(holdClass)
                && holdClass.Trim() == TicketActionPolicy.HoldRequester)
            {
                ItsmNotification.EnqueueForEmployee(
                    context,
                    ticket.Id,
                    ticket.RequesterCode,
                    "ITSM Ticket On Hold: " + ticket.TicketNumber,
                    "Ticket " + ticket.TicketNumber + " is waiting on you.");
            }
            else if (toStatus == "Resolved")
            {
                ItsmNotification.EnqueueForEmployee(
                    context,
                    ticket.Id,
                    ticket.RequesterCode,
                    "ITSM Ticket Resolved: " + ticket.TicketNumber,
                    "Ticket " + ticket.TicketNumber + " was resolved.");
            }
            else if (toStatus == "Closed")
            {
                ItsmNotification.EnqueueForEmployee(
                    context,
                    ticket.Id,
                    ticket.AssigneeCode,
                    "ITSM Ticket Closed: " + ticket.TicketNumber,
                    "Ticket " + ticket.TicketNumber + " was closed.");
            }
            else if (toStatus == "In Progress" && fromStatus == "Resolved")
            {
                ItsmNotification.EnqueueForEmployee(
                    context,
                    ticket.Id,
                    ticket.AssigneeCode,
                    "ITSM Ticket Reopened: " + ticket.TicketNumber,
                    "Ticket " + ticket.TicketNumber + " was returned to processing.");
            }
        }

        private static Ticket LoadForProcess(
            ItsmDbContext context,
            int ticketId,
            string actorCode,
            byte[] rowVersion,
            out string error)
        {
            var ticket = LoadMutableTicket(context, ticketId, actorCode, rowVersion, out error);
            if (ticket == null)
            {
                return null;
            }

            if (!ItsmTicketAccess.CanProcessTicket(actorCode, ticket.AssigneeCode))
            {
                error = "Ticket not found or access denied.";
                return null;
            }

            return RequireRecordedTenant(ticket, out error);
        }

        private static Ticket LoadForApprove(
            ItsmDbContext context,
            int ticketId,
            string actorCode,
            byte[] rowVersion,
            out string error)
        {
            var ticket = LoadMutableTicket(context, ticketId, actorCode, rowVersion, out error);
            if (ticket == null)
            {
                return null;
            }

            if (!ItsmTicketAccess.CanApproveTicket(context, ticket.Id, actorCode, ticket.RequesterCode))
            {
                error = "Ticket not found or access denied.";
                return null;
            }

            return RequireRecordedTenant(ticket, out error);
        }

        private static Ticket LoadMutableTicket(
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

            var actorCompanyId = context.UserMasters.AsNoTracking()
                .Where(u => u.EmployeeCode == actorCode)
                .Select(u => u.CompanyId)
                .FirstOrDefault();
            if (!actorCompanyId.HasValue || actorCompanyId.Value <= 0)
            {
                error = "Ticket not found or access denied.";
                return null;
            }

            var ticket = context.Tickets.FirstOrDefault(t =>
                t.Id == ticketId && t.CompanyId == actorCompanyId.Value);
            if (ticket == null)
            {
                error = "Ticket not found or access denied.";
                return null;
            }

            context.Entry(ticket).Property(x => x.RowVersion).OriginalValue = rowVersion;
            error = null;
            return ticket;
        }

        private static Ticket RequireRecordedTenant(Ticket ticket, out string error)
        {
            if (!ticket.CompanyId.HasValue)
            {
                error = "This ticket has no recorded tenant (CompanyId). "
                    + "Mutations are blocked until tenant ownership is remediated.";
                return null;
            }

            error = null;
            return ticket;
        }

        /// <summary>
        /// Returns an error instead of cutting the row. A null return means the
        /// audit entity was added to the same context as the ticket change.
        /// </summary>
        private static string AddAudit(
            ItsmDbContext context,
            Ticket ticket,
            string actionType,
            string oldValue,
            string newValue,
            string detail,
            string actorCode,
            DateTime actionedOn)
        {
            if (actionType != null && actionType.Length > 50)
            {
                return "Audit action type is too long.";
            }

            if (oldValue != null && oldValue.Length > 200)
            {
                return "Audit previous value is too long.";
            }

            if (newValue != null && newValue.Length > 200)
            {
                return "Audit new value is too long.";
            }

            if (detail != null && detail.Length > AuditDetailMax)
            {
                return "Audit detail must be 1000 characters or fewer.";
            }

            if (actorCode != null && actorCode.Length > 50)
            {
                return "Actor code is too long.";
            }

            context.TicketAuditLogs.Add(new TicketAuditLog
            {
                TicketId = ticket.Id,
                CompanyId = ticket.CompanyId.Value,
                ActionType = actionType,
                OldValue = oldValue,
                NewValue = newValue,
                Detail = detail,
                ActorCode = actorCode,
                ActionedOn = actionedOn
            });
            return null;
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
