// ========================================================================
// When: 2026-11-26
// What: Read and save itsm.MST_SupportMapping for the signed-in branch.
// Why:  Category configuration already owns this dispatch table. The row
//       is branch plus catalog, or a branch default, and one active agent.
//       CompanyId is the employee's company. The mapping table has none.
//       Dispatch rules stay in DispatchService. Rows are deactivated,
//       not deleted. No ticket, workflow, or audit write.
// ========================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using AnmolDristi.ITSM.DAL;
using AnmolDristi.ITSM.DAL.Entities;

namespace AnmolDristi.ITSM.BLL
{
    /// <summary>
    /// One support-agent mapping row for the category configuration grid.
    /// </summary>
    public class SupportMappingRowDto
    {
        public int Id { get; set; }

        public bool IsBranchDefault { get; set; }

        public int? CategoryId { get; set; }

        public string CategoryName { get; set; }

        public int? SubCategoryId { get; set; }

        public string SubCategoryName { get; set; }

        public int? ItemId { get; set; }

        public string ItemName { get; set; }

        public string AgentCode { get; set; }

        public string AgentName { get; set; }

        public bool IsActive { get; set; }

        public string Scope { get; set; }

        public int? CategorySort { get; set; }

        public int? SubCategorySort { get; set; }

        public int? ItemSort { get; set; }
    }

    /// <summary>
    /// Active catalog choice for a new mapping. ParentId is the row above.
    /// </summary>
    public class MappingChoiceDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int ParentId { get; set; }
    }

    /// <summary>
    /// Active employee in the signed-in company.
    /// </summary>
    public class MappingEmployeeDto
    {
        public string EmployeeCode { get; set; }

        public string EmployeeName { get; set; }
    }

    /// <summary>
    /// Mapping grid, employee list, and catalog choices for one branch.
    /// </summary>
    public class SupportMappingPageDto
    {
        public string BranchName { get; set; }

        public string Error { get; set; }

        public List<SupportMappingRowDto> Rows { get; set; }

        public List<MappingEmployeeDto> Employees { get; set; }

        public List<MappingChoiceDto> Parents { get; set; }

        public List<MappingChoiceDto> Categories { get; set; }

        public List<MappingChoiceDto> SubCategories { get; set; }

        public SupportMappingPageDto()
        {
            Rows = new List<SupportMappingRowDto>();
            Employees = new List<MappingEmployeeDto>();
            Parents = new List<MappingChoiceDto>();
            Categories = new List<MappingChoiceDto>();
            SubCategories = new List<MappingChoiceDto>();
        }
    }

    /// <summary>
    /// BLL for support-agent mapping on the category configuration page.
    /// Reads are AsNoTracking projections. Writes load one mapping row.
    /// </summary>
    public class SupportMappingAdminService
    {
        public SupportMappingPageDto GetForBranch(string actorCode, int legacyBranchId)
        {
            var page = new SupportMappingPageDto();
            var companyId = ItsmTenantContext.ResolveCompanyId(actorCode);
            if (companyId <= 0)
            {
                page.Error = "Your account has no company, so support agent mappings cannot be changed.";
                return page;
            }

            using (var context = new ItsmDbContext())
            {
                var branch = context.Branches
                    .AsNoTracking()
                    .Where(b => b.Branch_ID == legacyBranchId)
                    .Select(b => new { b.Id, b.Branch_Description })
                    .FirstOrDefault();
                if (branch == null)
                {
                    page.Error = "The signed-in branch was not found.";
                    return page;
                }

                page.BranchName = branch.Branch_Description ?? string.Empty;
                page.Employees = context.UserMasters
                    .AsNoTracking()
                    .Where(u => u.EmployeeStatus == "Active" && u.CompanyId == companyId)
                    .OrderBy(u => u.EmployeeName)
                    .ThenBy(u => u.EmployeeCode)
                    .Select(u => new MappingEmployeeDto
                    {
                        EmployeeCode = u.EmployeeCode,
                        EmployeeName = u.EmployeeName
                    })
                    .ToList();
                page.Parents = context.Categories
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.SortOrder)
                    .ThenBy(c => c.Id)
                    .Select(c => new MappingChoiceDto
                    {
                        Id = c.Id,
                        Name = c.Name
                    })
                    .ToList();
                page.Categories = context.SubCategories
                    .AsNoTracking()
                    .Where(s => s.IsActive && s.Category.IsActive)
                    .OrderBy(s => s.Category.SortOrder)
                    .ThenBy(s => s.CategoryId)
                    .ThenBy(s => s.SortOrder)
                    .ThenBy(s => s.Id)
                    .Select(s => new MappingChoiceDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        ParentId = s.CategoryId
                    })
                    .ToList();
                page.SubCategories = context.Items
                    .AsNoTracking()
                    .Where(i => i.IsActive && i.SubCategory.IsActive && i.SubCategory.Category.IsActive)
                    .OrderBy(i => i.SubCategory.Category.SortOrder)
                    .ThenBy(i => i.SubCategory.CategoryId)
                    .ThenBy(i => i.SubCategory.SortOrder)
                    .ThenBy(i => i.SubCategoryId)
                    .ThenBy(i => i.SortOrder)
                    .ThenBy(i => i.Id)
                    .Select(i => new MappingChoiceDto
                    {
                        Id = i.Id,
                        Name = i.Name,
                        ParentId = i.SubCategoryId
                    })
                    .ToList();

                var rows = (
                    from mapping in context.SupportMappings.AsNoTracking()
                    join agent in context.UserMasters.AsNoTracking()
                        on mapping.SupportAgentCode equals agent.EmployeeCode
                    where mapping.BranchId == branch.Id && agent.CompanyId == companyId
                    join category in context.Categories.AsNoTracking()
                        on mapping.CategoryId equals (int?)category.Id into categoryJoin
                    from category in categoryJoin.DefaultIfEmpty()
                    join subCategory in context.SubCategories.AsNoTracking()
                        on mapping.SubCategoryId equals (int?)subCategory.Id into subJoin
                    from subCategory in subJoin.DefaultIfEmpty()
                    join item in context.Items.AsNoTracking()
                        on mapping.ItemId equals (int?)item.Id into itemJoin
                    from item in itemJoin.DefaultIfEmpty()
                    select new SupportMappingRowDto
                    {
                        Id = mapping.Id,
                        IsBranchDefault = mapping.IsBranchDefault,
                        CategoryId = mapping.CategoryId,
                        CategoryName = category.Name,
                        CategorySort = category.SortOrder,
                        SubCategoryId = mapping.SubCategoryId,
                        SubCategoryName = subCategory.Name,
                        SubCategorySort = subCategory.SortOrder,
                        ItemId = mapping.ItemId,
                        ItemName = item.Name,
                        ItemSort = item.SortOrder,
                        AgentCode = agent.EmployeeCode,
                        AgentName = agent.EmployeeName,
                        IsActive = mapping.IsActive
                    }).ToList();

                foreach (var row in rows)
                {
                    row.Scope = ScopeName(row);
                }

                page.Rows = rows
                    .OrderByDescending(row => row.IsBranchDefault)
                    .ThenBy(row => row.CategorySort ?? 0)
                    .ThenBy(row => row.CategoryId)
                    .ThenBy(row => row.SubCategorySort ?? 0)
                    .ThenBy(row => row.SubCategoryId)
                    .ThenBy(row => row.ItemSort ?? 0)
                    .ThenBy(row => row.ItemId)
                    .ThenBy(row => row.Id)
                    .ToList();
                return page;
            }
        }

        public CatalogSaveResult Add(
            string actorCode,
            int legacyBranchId,
            bool isBranchDefault,
            int? categoryId,
            int? subCategoryId,
            int? itemId,
            string agentCode)
        {
            int companyId;
            int branchId;
            string error;
            if (!TryResolveScope(actorCode, legacyBranchId, out companyId, out branchId, out error))
            {
                return CatalogSaveResult.Fail(error);
            }

            categoryId = NullIfEmpty(categoryId);
            subCategoryId = NullIfEmpty(subCategoryId);
            itemId = NullIfEmpty(itemId);

            using (var context = new ItsmDbContext())
            {
                if (!TryEligibleAgent(context, companyId, agentCode, out error))
                {
                    return CatalogSaveResult.Fail(error);
                }

                if (!TryValidTarget(context, isBranchDefault, categoryId, subCategoryId, itemId, out error))
                {
                    return CatalogSaveResult.Fail(error);
                }

                var branchRows = context.SupportMappings
                    .Where(row => row.BranchId == branchId)
                    .ToList();
                if (branchRows.Any(row => row.IsActive && SameKey(row, isBranchDefault, categoryId, subCategoryId, itemId)))
                {
                    return CatalogSaveResult.Fail(
                        "A mapping already exists for this branch and catalog selection.");
                }

                var inactive = branchRows
                    .Where(row => !row.IsActive && SameKey(row, isBranchDefault, categoryId, subCategoryId, itemId))
                    .OrderBy(row => row.Id)
                    .FirstOrDefault();
                if (inactive != null)
                {
                    inactive.SupportAgentCode = agentCode.Trim();
                    inactive.IsActive = true;
                    inactive.ModifiedOn = DateTime.Now;
                    context.SaveChanges();
                    return CatalogSaveResult.Ok();
                }

                context.SupportMappings.Add(new SupportMapping
                {
                    BranchId = branchId,
                    CategoryId = isBranchDefault ? null : categoryId,
                    SubCategoryId = isBranchDefault ? null : subCategoryId,
                    ItemId = isBranchDefault ? null : itemId,
                    SupportAgentCode = agentCode.Trim(),
                    IsBranchDefault = isBranchDefault,
                    IsActive = true,
                    CreatedOn = DateTime.Now
                });
                context.SaveChanges();
                return CatalogSaveResult.Ok();
            }
        }

        public CatalogSaveResult Save(
            string actorCode,
            int legacyBranchId,
            int mappingId,
            string agentCode,
            bool isActive)
        {
            int companyId;
            int branchId;
            string error;
            if (!TryResolveScope(actorCode, legacyBranchId, out companyId, out branchId, out error))
            {
                return CatalogSaveResult.Fail(error);
            }

            using (var context = new ItsmDbContext())
            {
                var row = context.SupportMappings
                    .FirstOrDefault(item => item.Id == mappingId && item.BranchId == branchId);
                if (row == null || !RowBelongsToCompany(context, row.SupportAgentCode, companyId))
                {
                    return CatalogSaveResult.Fail("That mapping was not found for your company and branch.");
                }

                if (isActive && !TryEligibleAgent(context, companyId, agentCode, out error))
                {
                    return CatalogSaveResult.Fail(error);
                }

                if (!isActive && string.IsNullOrWhiteSpace(agentCode))
                {
                    agentCode = row.SupportAgentCode;
                }
                else if (!isActive && !SameAgent(row.SupportAgentCode, agentCode))
                {
                    if (!TryEligibleAgent(context, companyId, agentCode, out error))
                    {
                        return CatalogSaveResult.Fail(error);
                    }
                }

                if (isActive)
                {
                    var duplicate = context.SupportMappings
                        .Where(item => item.Id != row.Id && item.BranchId == branchId && item.IsActive)
                        .ToList()
                        .Any(item => SameKey(
                            item,
                            row.IsBranchDefault,
                            row.CategoryId,
                            row.SubCategoryId,
                            row.ItemId));
                    if (duplicate)
                    {
                        return CatalogSaveResult.Fail(
                            "A mapping already exists for this branch and catalog selection.");
                    }
                }

                row.SupportAgentCode = agentCode.Trim();
                row.IsActive = isActive;
                row.ModifiedOn = DateTime.Now;
                context.SaveChanges();
                return CatalogSaveResult.Ok();
            }
        }

        private static bool TryResolveScope(
            string actorCode,
            int legacyBranchId,
            out int companyId,
            out int branchId,
            out string error)
        {
            companyId = ItsmTenantContext.ResolveCompanyId(actorCode);
            branchId = 0;
            error = null;
            if (companyId <= 0)
            {
                error = "Your account has no company, so support agent mappings cannot be changed.";
                return false;
            }

            using (var context = new ItsmDbContext())
            {
                var resolved = context.Branches
                    .AsNoTracking()
                    .Where(branch => branch.Branch_ID == legacyBranchId)
                    .Select(branch => (int?)branch.Id)
                    .FirstOrDefault();
                if (!resolved.HasValue)
                {
                    error = "The signed-in branch was not found.";
                    return false;
                }

                branchId = resolved.Value;
                return true;
            }
        }

        private static bool TryEligibleAgent(
            ItsmDbContext context,
            int companyId,
            string agentCode,
            out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(agentCode))
            {
                error = "Select an active employee in your company.";
                return false;
            }

            var code = agentCode.Trim();
            var eligible = context.UserMasters
                .AsNoTracking()
                .Any(user => user.EmployeeCode == code
                    && user.EmployeeStatus == "Active"
                    && user.CompanyId == companyId);
            if (!eligible)
            {
                error = "Select an active employee in your company.";
                return false;
            }

            return true;
        }

        private static bool TryValidTarget(
            ItsmDbContext context,
            bool isBranchDefault,
            int? categoryId,
            int? subCategoryId,
            int? itemId,
            out string error)
        {
            error = null;
            if (isBranchDefault)
            {
                if (categoryId.HasValue || subCategoryId.HasValue || itemId.HasValue)
                {
                    error = "A branch default mapping cannot also name a catalog entry.";
                    return false;
                }

                return true;
            }

            if (!categoryId.HasValue)
            {
                error = "Select a parent category, or choose the branch default.";
                return false;
            }

            var parentActive = context.Categories
                .AsNoTracking()
                .Any(category => category.Id == categoryId.Value && category.IsActive);
            if (!parentActive)
            {
                error = "Select an active parent category.";
                return false;
            }

            if (!subCategoryId.HasValue)
            {
                if (itemId.HasValue)
                {
                    error = "Select a category before a sub-category.";
                    return false;
                }

                return true;
            }

            var categoryMatches = context.SubCategories
                .AsNoTracking()
                .Any(subCategory => subCategory.Id == subCategoryId.Value
                    && subCategory.CategoryId == categoryId.Value
                    && subCategory.IsActive);
            if (!categoryMatches)
            {
                error = "Select an active category under that parent.";
                return false;
            }

            if (!itemId.HasValue)
            {
                return true;
            }

            var itemMatches = context.Items
                .AsNoTracking()
                .Any(item => item.Id == itemId.Value
                    && item.SubCategoryId == subCategoryId.Value
                    && item.IsActive);
            if (!itemMatches)
            {
                error = "Select an active sub-category under that category.";
                return false;
            }

            return true;
        }

        private static bool RowBelongsToCompany(ItsmDbContext context, string agentCode, int companyId)
        {
            return context.UserMasters
                .AsNoTracking()
                .Any(user => user.EmployeeCode == agentCode && user.CompanyId == companyId);
        }

        private static bool SameKey(
            SupportMapping row,
            bool isBranchDefault,
            int? categoryId,
            int? subCategoryId,
            int? itemId)
        {
            return row.IsBranchDefault == isBranchDefault
                && row.CategoryId == categoryId
                && row.SubCategoryId == subCategoryId
                && row.ItemId == itemId;
        }

        private static bool SameAgent(string stored, string posted)
        {
            return string.Equals(
                (stored ?? string.Empty).Trim(),
                (posted ?? string.Empty).Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        private static int? NullIfEmpty(int? value)
        {
            if (!value.HasValue || value.Value <= 0)
            {
                return null;
            }

            return value;
        }

        private static string ScopeName(SupportMappingRowDto row)
        {
            if (row.IsBranchDefault)
            {
                return "Branch default";
            }

            if (row.ItemId.HasValue)
            {
                return "Sub-category";
            }

            if (row.SubCategoryId.HasValue)
            {
                return "Category";
            }

            return "Parent category";
        }
    }
}
