// ========================================================================
// When: 2026-08-21
// What: Ticket access by requester/assignee/approver or non-terminal shared queue.
// Why: Actor CompanyId is not ticket tenant; TicketId alone is not authorization.
// ========================================================================

using System;
using System.Data.Entity;
using System.Linq;
using AnmolDristi.ITSM.DAL;

namespace AnmolDristi.ITSM.BLL
{
    internal static class ItsmTicketAccess
    {
        public static bool CanAccess(
            ItsmDbContext context,
            int ticketId,
            string actorCode,
            string requesterCode,
            string assigneeCode,
            bool isTerminal)
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

            if (context.TicketApprovals.AsNoTracking()
                .Any(a => a.TicketId == ticketId && a.ApproverCode == actorCode))
            {
                return true;
            }

            return !isTerminal;
        }
    }
}
