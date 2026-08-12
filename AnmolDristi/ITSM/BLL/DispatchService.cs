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
                // 1. Most specific: Item-level mapping
                if (itemId.HasValue)
                {
                    var itemAgent = FindMapping(
                        context, branchId, categoryId, subCategoryId, itemId);
                    if (itemAgent != null)
                    {
                        return itemAgent;
                    }
                }

                // 2. SubCategory-level mapping (ItemId IS NULL)
                var subAgent = FindMapping(
                    context, branchId, categoryId, subCategoryId, itemId: null);
                if (subAgent != null)
                {
                    return subAgent;
                }

                // 3. Category-level mapping (SubCategoryId IS NULL, ItemId IS NULL)
                var catAgent = FindCategoryMapping(context, branchId, categoryId);
                if (catAgent != null)
                {
                    return catAgent;
                }

                // 4. Branch default
                var defaultAgent = FindBranchDefault(context, branchId);
                if (defaultAgent != null)
                {
                    return defaultAgent;
                }

                return null; // Unassigned — supervisor queue
            }
        }

        private static string FindMapping(
            ItsmDbContext context,
            int branchId,
            int categoryId,
            int subCategoryId,
            int? itemId)
        {
            IQueryable<SupportMapping> query = context.SupportMappings
                .AsNoTracking()
                .Where(m =>
                    m.IsActive
                    && m.BranchId == branchId
                    && m.CategoryId == categoryId
                    && m.SubCategoryId == subCategoryId
                    && !m.IsBranchDefault);

            if (itemId.HasValue)
            {
                query = query.Where(m => m.ItemId == itemId.Value);
            }
            else
            {
                query = query.Where(m => m.ItemId == null);
            }

            return query
                .Select(m => m.SupportAgentCode)
                .FirstOrDefault();
        }

        private static string FindCategoryMapping(
            ItsmDbContext context,
            int branchId,
            int categoryId)
        {
            return context.SupportMappings
                .AsNoTracking()
                .Where(m =>
                    m.IsActive
                    && m.BranchId == branchId
                    && m.CategoryId == categoryId
                    && m.SubCategoryId == null
                    && m.ItemId == null
                    && !m.IsBranchDefault)
                .Select(m => m.SupportAgentCode)
                .FirstOrDefault();
        }

        private static string FindBranchDefault(ItsmDbContext context, int branchId)
        {
            return context.SupportMappings
                .AsNoTracking()
                .Where(m =>
                    m.IsActive
                    && m.BranchId == branchId
                    && m.IsBranchDefault
                    && m.CategoryId == null
                    && m.SubCategoryId == null
                    && m.ItemId == null)
                .Select(m => m.SupportAgentCode)
                .FirstOrDefault();
        }
    }
}
