using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using AnmolDristi.ITSM.DAL;
using AnmolDristi.ITSM.DAL.Entities;

namespace AnmolDristi.ITSM.BLL
{
    /// <summary>
    /// Intelligent Dispatch Matrix — R6 group precedence then individual cascade,
    /// plus Digitization mapped-agent helpers used by list/detail/update/access.
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
        /// Digitization path: resolve from active individual mappings in-context.
        /// Used by <see cref="IsMappedAgent"/> and callers that already loaded mappings.
        /// </summary>
        public static string ResolveAssignee(
            ItsmDbContext context,
            int branchId,
            int categoryId,
            int subCategoryId,
            int? itemId)
        {
            var rows = context.SupportMappings
                .AsNoTracking()
                .Where(m => m.IsActive && m.BranchId == branchId)
                .ToList();
            return ResolveFromMappings(rows, categoryId, subCategoryId, itemId);
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

        public static bool IsMappedAgent(
            ItsmDbContext context,
            int branchId,
            int categoryId,
            int subCategoryId,
            int? itemId,
            string actorCode)
        {
            var resolved = ResolveAssignee(context, branchId, categoryId, subCategoryId, itemId);
            return !string.IsNullOrWhiteSpace(resolved)
                && !string.IsNullOrWhiteSpace(actorCode)
                && string.Equals(resolved.Trim(), actorCode.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// One cascade, lowest mapping Id wins at each step. A blank agent code
        /// does not fall through to the next step: that count was not read.
        /// </summary>
        public static string ResolveFromMappings(
            IEnumerable<SupportMapping> mappings,
            int categoryId,
            int subCategoryId,
            int? itemId)
        {
            var rows = mappings == null
                ? new List<SupportMapping>()
                : mappings.ToList();

            if (itemId.HasValue)
            {
                var itemAgent = FirstCode(rows, m =>
                    m.CategoryId == categoryId
                    && m.SubCategoryId == subCategoryId
                    && m.ItemId == itemId
                    && !m.IsBranchDefault);
                if (itemAgent != null)
                {
                    return itemAgent;
                }
            }

            var subAgent = FirstCode(rows, m =>
                m.CategoryId == categoryId
                && m.SubCategoryId == subCategoryId
                && m.ItemId == null
                && !m.IsBranchDefault);
            if (subAgent != null)
            {
                return subAgent;
            }

            var categoryAgent = FirstCode(rows, m =>
                m.CategoryId == categoryId
                && m.SubCategoryId == null
                && m.ItemId == null
                && !m.IsBranchDefault);
            if (categoryAgent != null)
            {
                return categoryAgent;
            }

            return FirstCode(rows, m =>
                m.IsBranchDefault
                && m.CategoryId == null
                && m.SubCategoryId == null
                && m.ItemId == null);
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

        private static string FirstCode(List<SupportMapping> rows, Func<SupportMapping, bool> predicate)
        {
            return rows
                .Where(predicate)
                .OrderBy(m => m.Id)
                .Select(m => m.SupportAgentCode)
                .FirstOrDefault();
        }
    }
}
