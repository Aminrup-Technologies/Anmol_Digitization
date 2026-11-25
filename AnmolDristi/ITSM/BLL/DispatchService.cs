using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using AnmolDristi.ITSM.DAL;
using AnmolDristi.ITSM.DAL.Entities;

namespace AnmolDristi.ITSM.BLL
{
    /// <summary>
    /// Intelligent Dispatch Matrix — resolves the default support agent for a new ticket
    /// via cascading fallback on <c>itsm.MST_SupportMapping</c> (schema §5.2).
    /// </summary>
    public class DispatchService
    {
        /// <summary>
        /// Cascading fallback (most specific wins):
        /// 1. Item → BranchId + CategoryId + SubCategoryId + ItemId
        /// 2. SubCategory → BranchId + CategoryId + SubCategoryId (ItemId IS NULL)
        /// 3. Category → BranchId + CategoryId (SubCategoryId IS NULL)
        /// 4. Branch Default → BranchId + IsBranchDefault = 1
        /// Returns <c>SupportAgentCode</c>, or <c>null</c> if unassigned (supervisor queue).
        /// </summary>
        public string ResolveAssignee(int branchId, int categoryId, int subCategoryId, int? itemId)
        {
            using (var context = new ItsmDbContext())
            {
                return ResolveAssignee(context, branchId, categoryId, subCategoryId, itemId);
            }
        }

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
