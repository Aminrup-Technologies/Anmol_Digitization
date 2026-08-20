using System.Data.Entity;
using System.Linq;
using AnmolDristi.ITSM.DAL;

namespace AnmolDristi.ITSM.BLL
{
    /// <summary>
    /// Resolves the ITGC tenant boundary from the authenticated employee.
    /// CompanyId is taken from dbo.MST_UserMaster — never from ticket URL or ViewState.
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
