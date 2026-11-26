// ========================================================================
// When: 2026-08-21
// What: Split ticket authorization by operation: CanViewTicket,
//       CanApproveTicket, CanProcessTicket. Pending approval is not
//       general processing. Historical approval rows are not view/process.
//       2026-08-24: Added RBAC-gated ITSM.Ticket.View authorization.
//       2026-11-25: Process is the assignee. The mapped agent may view an
//       unassigned ticket in order to take it. Requester comment and
//       confirmation are separate from process.
//       A user with the permission can view tickets belonging to their
//       authorized tenant (Ticket.CompanyId), not arbitrary tickets.
// Why: A TicketApprovals row must not make an approver a general processor.
//       Non-terminal status is not authorization. The permission gate
//       must be tenant-scoped to prevent cross-company access via ?id=.
// ========================================================================

using System;
using System.Data.Entity;
using System.Linq;
using AnmolDristi.ITSM.DAL;

namespace AnmolDristi.ITSM.BLL
{
    /// <summary>
    /// Operation-specific ticket authorization from stored relationships.
    /// SupportMapping is create-time dispatch, not a live ACL.
    /// ITSM.Ticket.View permission is evaluated by the UI layer and passed
    /// in — BLL remains System.Web-agnostic.
    /// </summary>
    internal static class ItsmTicketAccess
    {
        /// <summary>
        /// Determines whether the actor may view ticket detail.
        /// The ticket company must equal the actor company first. A missing
        /// company denies access. Relationship and permission checks run
        /// only after that match:
        ///   1. Actor is the ticket requester
        ///   2. Actor is the ticket assignee
        ///   3. Actor has a pending approval for the ticket
        ///   4. Actor has ITSM.Ticket.View
        ///   5. Actor is the mapped agent of an unassigned ticket
        /// authorizedCompanyId is the company resolved from the actor's
        /// employee code. BLL never references System.Web.
        /// </summary>
        public static bool CanViewTicket(
            ItsmDbContext context,
            int ticketId,
            string actorCode,
            string requesterCode,
            string assigneeCode,
            bool hasTicketViewPermission,
            int authorizedCompanyId,
            int? ticketCompanyId,
            int branchId,
            int categoryId,
            int subCategoryId,
            int? itemId)
        {
            if (authorizedCompanyId <= 0
                || !ticketCompanyId.HasValue
                || ticketCompanyId.Value != authorizedCompanyId)
            {
                return false;
            }

            if (IsRequesterOrAssignee(actorCode, requesterCode, assigneeCode))
            {
                return true;
            }

            if (HasPendingApproval(context, ticketId, actorCode))
            {
                return true;
            }

            if (hasTicketViewPermission)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(assigneeCode)
                && DispatchService.IsMappedAgent(
                    context, branchId, categoryId, subCategoryId, itemId, actorCode))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Work actions belong to the current assignee. The requester is not
        /// a processor. Taking an unassigned ticket is a separate check.
        /// </summary>
        public static bool CanProcessTicket(string actorCode, string assigneeCode)
        {
            if (string.IsNullOrWhiteSpace(actorCode) || string.IsNullOrWhiteSpace(assigneeCode))
            {
                return false;
            }

            return string.Equals(assigneeCode.Trim(), actorCode.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public static bool CanComment(string actorCode, string requesterCode, string assigneeCode)
        {
            return AttachmentAccessRules.IsRequesterOrAssignee(
                actorCode,
                requesterCode,
                assigneeCode);
        }

        public static bool CanApproveTicket(
            ItsmDbContext context,
            int ticketId,
            string actorCode,
            string requesterCode)
        {
            if (string.IsNullOrWhiteSpace(actorCode))
            {
                return false;
            }

            if (string.Equals(requesterCode, actorCode, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return HasPendingApproval(context, ticketId, actorCode);
        }

        private static bool IsRequesterOrAssignee(
            string actorCode,
            string requesterCode,
            string assigneeCode)
        {
            if (string.IsNullOrWhiteSpace(actorCode))
            {
                return false;
            }

            if (string.Equals(requesterCode, actorCode, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(assigneeCode)
                && string.Equals(assigneeCode, actorCode, StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasPendingApproval(
            ItsmDbContext context,
            int ticketId,
            string actorCode)
        {
            return context.TicketApprovals.AsNoTracking()
                .Any(a => a.TicketId == ticketId
                          && a.ApproverCode == actorCode
                          && a.ApprovalStatus == "Pending");
        }
    }
}
