using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using AnmolDristi.ITSM.DAL;
using AnmolDristi.ITSM.DAL.Entities;

namespace AnmolDristi.ITSM.BLL
{
    /// <summary>
    /// ITIL / ISO 20000 aligned lifecycle used by ITSM Phase 1.
    /// Pending Approval is actioned only via Approve/Reject (SoD) — not free status change.
    /// </summary>
    public static class TicketWorkflow
    {
        public static readonly Dictionary<string, List<string>> ValidTransitions =
            new Dictionary<string, List<string>>
            {
                { "New", new List<string> { "In Progress", "On Hold", "Cancelled" } },
                { "In Progress", new List<string> { "Resolved", "On Hold", "Cancelled" } },
                { "On Hold", new List<string> { "In Progress", "Cancelled" } },
                { "Resolved", new List<string> { "Closed", "In Progress" } }
            };

        public static List<TicketStatusDto> GetAvailableStatuses(
            ItsmDbContext context,
            TicketStatus currentStatus)
        {
            if (currentStatus == null || currentStatus.IsTerminal)
            {
                return new List<TicketStatusDto>();
            }

            List<string> validNextNames;
            if (!ValidTransitions.TryGetValue(currentStatus.Name, out validNextNames))
            {
                return new List<TicketStatusDto>();
            }

            return context.TicketStatuses
                .AsNoTracking()
                .Where(s => s.IsActive && validNextNames.Contains(s.Name))
                .Select(s => new TicketStatusDto
                {
                    Id = s.Id,
                    Name = s.Name
                })
                .ToList();
        }
    }
}
