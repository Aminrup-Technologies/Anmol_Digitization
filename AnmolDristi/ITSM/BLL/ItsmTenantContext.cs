// ========================================================================
// When: 2026-08-21
// What: Live UserMaster.CompanyId for the authenticated employee code.
// Why: Queue, detail, and mutation checks compare this value with
//      Ticket.CompanyId. A missing or non-positive value fails closed.
//      Session company keys and browser values are not a source.
// ========================================================================

using System.Data.Entity;
using System.Linq;
using AnmolDristi.ITSM.DAL;

namespace AnmolDristi.ITSM.BLL
{
    /// <summary>
    /// Resolves the signed-in employee's current MST_UserMaster.CompanyId
    /// from the employee code. Returns 0 when the employee or company is
    /// missing. Callers must fail closed on a non-positive result.
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
