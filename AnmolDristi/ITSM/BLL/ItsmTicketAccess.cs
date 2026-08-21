// ========================================================================
// When: 2026-08-21
// What: Ticket access is requester OR current assignee OR TicketApprovals row.
//       Non-terminal status is not authorization.
// Why: Queue visibility (All Open) is not ticket authorization. An authenticated
//      agent plus an arbitrary TicketID must not grant access.
// ========================================================================

using System;
using System.Data.Entity;
using System.Linq;
using AnmolDristi.ITSM.DAL;

namespace AnmolDristi.ITSM.BLL
{
    /// <summary>
    /// Ticket-level authorization from stored relationships only.
    /// SupportMapping is create-time dispatch (sets AssigneeCode); it is not a live ACL.
    /// App RBAC (UI.ITSM.*) is not registered on these pages — do not invent it here.
    /// </summary>
    internal static class ItsmTicketAccess
    {
        public static bool CanAccess(
            ItsmDbContext context,
            int ticketId,
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

            if (!string.IsNullOrWhiteSpace(assigneeCode)
                && string.Equals(assigneeCode, actorCode, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return context.TicketApprovals.AsNoTracking()
                .Any(a => a.TicketId == ticketId && a.ApproverCode == actorCode);
        }
    }
}
