using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using AnmolDristi.ITSM.DAL;
using AnmolDristi.ITSM.DAL.Entities;

namespace AnmolDristi.ITSM.BLL
{
    /// <summary>
    /// Intelligent Dispatch Matrix — R6 group precedence then individual cascade.
    /// </summary>
    public class DispatchService
    {
        /// <summary>
        /// Legacy wrapper: returns only the agent code (null on group win or unmapped).
        /// Prefer <see cref="ResolveDispatch"/>.
        /// </summary>
        public string ResolveAssignee(int branchId, int categoryId, int subCategoryId, int? itemId)
        {
            // Company-agnostic legacy path cannot see group mappings (they are company-scoped).
            // Falls through to individual cascade only.
            using (var context = new ItsmDbContext())
            {
                var individuals = LoadIndividuals(context, branchId);
                return SupportGroupRules.ResolveIndividual(
                    branchId, categoryId, subCategoryId, itemId, individuals);
            }
        }

        /// <summary>
        /// R6 create-time dispatch with company isolation.
        /// </summary>
        public DispatchOutcome ResolveDispatch(
            int companyId,
            int branchId,
            int categoryId,
            int subCategoryId,
            int? itemId)
        {
            using (var context = new ItsmDbContext())
            {
                return ResolveDispatch(context, companyId, branchId, categoryId, subCategoryId, itemId);
            }
        }

        internal static DispatchOutcome ResolveDispatch(
            ItsmDbContext context,
            int companyId,
            int branchId,
            int categoryId,
            int subCategoryId,
            int? itemId)
        {
            // Company, branch, taxonomy, and active-group filters stay on
            // IQueryable (AsNoTracking + Select) before materialization.
            var groups = context.SupportGroupMappings
                .AsNoTracking()
                .Where(m => m.IsActive
                    && m.CompanyId == companyId
                    && m.BranchId == branchId
                    && ((m.SubCategoryId == subCategoryId && m.CategoryId == categoryId)
                        || (m.SubCategoryId == null && m.CategoryId == categoryId))
                    && m.SupportGroup != null
                    && m.SupportGroup.IsActive
                    && m.SupportGroup.CompanyId == companyId
                    && m.SupportGroup.BranchId == branchId)
                .Select(m => new GroupMappingRow
                {
                    Id = m.Id,
                    CompanyId = m.CompanyId,
                    BranchId = m.BranchId,
                    CategoryId = m.CategoryId,
                    SubCategoryId = m.SubCategoryId,
                    SupportGroupId = m.SupportGroupId,
                    IsActive = m.IsActive
                })
                .ToList();

            var individuals = LoadIndividuals(context, branchId);
            return SupportGroupRules.ResolveDispatch(
                companyId, branchId, categoryId, subCategoryId, itemId, groups, individuals);
        }

        private static List<IndividualMappingRow> LoadIndividuals(ItsmDbContext context, int branchId)
        {
            return context.SupportMappings
                .AsNoTracking()
                .Where(m => m.IsActive && m.BranchId == branchId)
                .Select(m => new IndividualMappingRow
                {
                    Id = m.Id,
                    BranchId = m.BranchId,
                    CategoryId = m.CategoryId,
                    SubCategoryId = m.SubCategoryId,
                    ItemId = m.ItemId,
                    SupportAgentCode = m.SupportAgentCode,
                    IsBranchDefault = m.IsBranchDefault,
                    IsActive = m.IsActive
                })
                .ToList();
        }
    }
}
