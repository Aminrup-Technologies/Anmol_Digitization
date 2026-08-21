// ========================================================================
// When: 2026-08-21
// What: Documented as live UserMaster.CompanyId lookup only — not ticket tenant.
// Why: Ticket.CompanyId is the authoritative tenant; this helper must not
//      gate TicketDetail / TicketUpdate.
// ========================================================================

using System.Data.Entity;
using System.Linq;
using AnmolDristi.ITSM.DAL;

namespace AnmolDristi.ITSM.BLL
{
    /// <summary>
    /// Resolves the signed-in employee's current MST_UserMaster.CompanyId.
    /// This is live HRMS data — not ticket tenant ownership. Do not use as
    /// a TicketDetail / TicketUpdate authorization gate.
    /// </summary>
    public static class ItsmTenantContext
    {
        public static int ResolveCompanyId(string employeeCode)
        {
            if (string.IsNullOrWhiteSpace(employeeCode))
            {
                return 0;
            }

            using (var context = new ItsmDbContext())
            {
                var companyId = context.UserMasters
                    .AsNoTracking()
                    .Where(u => u.EmployeeCode == employeeCode)
                    .Select(u => u.CompanyId)
                    .FirstOrDefault();

                return companyId.HasValue ? companyId.Value : 0;
            }
        }

        public static bool IsActiveEmployee(string employeeCode)
        {
            if (string.IsNullOrWhiteSpace(employeeCode))
            {
                return false;
            }

            using (var context = new ItsmDbContext())
            {
                return context.UserMasters
                    .AsNoTracking()
                    .Any(u => u.EmployeeCode == employeeCode
                              && u.EmployeeStatus == "Active");
            }
        }
    }
}
