// ========================================================================
// When: 2026-08-21
// What: Split ticket authorization by operation: CanViewTicket,
//       CanApproveTicket, CanProcessTicket. Pending approval is not
//       general processing. Historical approval rows are not view/process.
// Why: A TicketApprovals row must not make an approver a general processor.
//       Non-terminal status is not authorization.
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
    /// UI.ITSM.* keys are not registered on these pages — not invented here.
    /// </summary>
    internal static class ItsmTicketAccess
    {
        public static bool CanViewTicket(
            ItsmDbContext context,
            int ticketId,
            string actorCode,
            string requesterCode,
            string assigneeCode)
        {
            if (IsRequesterOrAssignee(actorCode, requesterCode, assigneeCode))
            {
                return true;
            }

            return HasPendingApproval(context, ticketId, actorCode);
        }

        public static bool CanProcessTicket(
            string actorCode,
            string requesterCode,
            string assigneeCode)
        {
            return IsRequesterOrAssignee(actorCode, requesterCode, assigneeCode);
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
