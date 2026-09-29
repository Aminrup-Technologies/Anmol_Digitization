// ========================================================================
// When: 2026-08-21
// What: Immutable ITIL transition map (IReadOnlyDictionary).
// Why: public Dictionary remained mutable even when marked readonly.
// ========================================================================

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using AnmolDristi.ITSM.DAL;

namespace AnmolDristi.ITSM.BLL
{
    public static class TicketWorkflow
    {
        private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Transitions;

        static TicketWorkflow()
        {
            var map = new Dictionary<string, IReadOnlyList<string>>
            {
                { "New", new ReadOnlyCollection<string>(new[] { "In Progress", "On Hold", "Cancelled" }) },
                { "In Progress", new ReadOnlyCollection<string>(new[] { "Resolved", "On Hold", "Cancelled" }) },
                { "On Hold", new ReadOnlyCollection<string>(new[] { "In Progress", "Cancelled" }) },
                { "Resolved", new ReadOnlyCollection<string>(new[] { "Closed", "In Progress" }) }
            };
            Transitions = new ReadOnlyDictionary<string, IReadOnlyList<string>>(map);
        }

        public static List<TicketStatusDto> GetAvailableStatuses(
            ItsmDbContext context,
            string currentStatusName,
            bool isTerminal)
        {
            if (isTerminal || string.IsNullOrWhiteSpace(currentStatusName))
            {
                return new List<TicketStatusDto>();
            }

            IReadOnlyList<string> validNextNames;
            if (!Transitions.TryGetValue(currentStatusName, out validNextNames))
            {
                return new List<TicketStatusDto>();
            }

            var nextNames = validNextNames.ToList();
            return context.TicketStatuses
                .AsNoTracking()
                .Where(s => s.IsActive && nextNames.Contains(s.Name))
                .Select(s => new TicketStatusDto
                {
                    Id = s.Id,
                    Name = s.Name
                })
                .ToList();
        }

        public static bool IsValidTransition(string fromName, string toName)
        {
            if (string.IsNullOrWhiteSpace(fromName) || string.IsNullOrWhiteSpace(toName))
            {
                return false;
            }

            IReadOnlyList<string> next;
            if (!Transitions.TryGetValue(fromName, out next))
            {
                return false;
            }

            return next.Contains(toName);
        }
    }
}
