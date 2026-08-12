using System.Data.Entity;
using System.Linq;
using AnmolDristi.ITSM.DAL;

namespace AnmolDristi.ITSM.BLL
{
    /// <summary>
    /// Resolves ticket priority from the Impact × Urgency matrix (schema §5.4).
    /// Priority is never selected manually on the New Ticket form.
    /// </summary>
    public class PriorityCalculationService
    {
        /// <summary>
        /// Looks up <c>itsm.MST_PriorityMatrix</c> for the given Impact and Urgency.
        /// If no matrix row exists, falls back to the active low priority (<c>P4</c>).
        /// </summary>
        public int CalculatePriorityId(int impactId, int urgencyId)
        {
            using (var context = new ItsmDbContext())
            {
                var matrixPriorityId = context.PriorityMatrices
                    .AsNoTracking()
                    .Where(m => m.ImpactId == impactId && m.UrgencyId == urgencyId)
                    .Select(m => (int?)m.PriorityId)
                    .FirstOrDefault();

                if (matrixPriorityId.HasValue)
                {
                    return matrixPriorityId.Value;
                }

                // Fallback: default low priority (P4) when matrix has no matching row.
                var lowPriorityId = context.Priorities
                    .AsNoTracking()
                    .Where(p => p.IsActive && p.Code == "P4")
                    .Select(p => (int?)p.Id)
                    .FirstOrDefault();

                if (lowPriorityId.HasValue)
                {
                    return lowPriorityId.Value;
                }

                // Ultimate fallback if seed data is incomplete — longest SLA = lowest urgency.
                return context.Priorities
                    .AsNoTracking()
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.SlaResolutionHours)
                    .Select(p => p.Id)
                    .FirstOrDefault();
            }
        }
    }
}
