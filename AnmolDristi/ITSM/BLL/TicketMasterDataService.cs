// ========================================================================
// When: 2026-08-26
// What: BLL service for ITSM master-data lookups and hierarchy validation.
//       Encapsulates all EF6 queries previously in CreateTicket.aspx.cs.
//       UI delegates to this service — zero direct database access in code-behind.
// Why:  Architecture enforcement: UI → BLL → DAL. Master data is company-scoped
//       where applicable. Hierarchy validation is a single consolidated query.
// When: 2026-11-25
// What: Ticket create still reads the same three tables. Parent Category is
//       itsm.MST_Category, Category is itsm.MST_SubCategory, and optional
//       Sub-Category is itsm.MST_Item. ValidateHierarchy now rejects an
//       inactive parent category. The catalog has no CompanyId.
// Why:  Phase 1B R1. Inactive entries must not be accepted on a new ticket,
//       including a posted id whose parent was deactivated after the page loaded.
// When: 2026-11-26
// What: GetCategoryHierarchy reads three flat AsNoTracking projections and
//       orders each by SortOrder, then Id. companyId is still not a filter.
// Why:  Create Ticket shows the display order saved on Category Config.
//       The catalog tables have no CompanyId, so none is added.
// ========================================================================

using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using AnmolDristi.ITSM.DAL;

namespace AnmolDristi.ITSM.BLL
{
    // ====================================================================
    // Presentation DTOs for the category/subcategory/item hierarchy.
    // These are projection DTOs — not EF entities.
    // ====================================================================

    /// <summary>
    /// Presentation DTO for an ITSM Item, projected from
    /// <c>itsm.MST_Item</c>. Used for client-side filtering JSON.
    /// </summary>
    public class TicketItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// Presentation DTO for an ITSM SubCategory, projected from
    /// <c>itsm.MST_SubCategory</c>. Contains child items.
    /// </summary>
    public class TicketSubCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<TicketItemDto> Items { get; set; }
    }

    /// <summary>
    /// Presentation DTO for an ITSM Category, projected from
    /// <c>itsm.MST_Category</c>. Contains child subcategories.
    /// </summary>
    public class TicketCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<TicketSubCategoryDto> SubCategories { get; set; }
    }

    /// <summary>
    /// Presentation DTO for Impact or Urgency (compact button group).
    /// </summary>
    public class TicketLookupItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// Result of server-side hierarchy validation.
    /// Null Error means valid; non-null Error is a user-facing message.
    /// </summary>
    public class HierarchyValidationResult
    {
        public bool IsValid { get; set; }
        public string Error { get; set; }

        public static HierarchyValidationResult Success()
        {
            return new HierarchyValidationResult { IsValid = true };
        }

        public static HierarchyValidationResult Fail(string error)
        {
            return new HierarchyValidationResult { IsValid = false, Error = error };
        }
    }

    /// <summary>
    /// BLL service for ITSM master-data lookups used by CreateTicket.
    /// All queries are company-scoped where applicable. All projections
    /// use <c>AsNoTracking().Select(...)</c> — no EF entities reach the UI.
    /// </summary>
    public class TicketMasterDataService
    {
        /// <summary>
        /// Loads the active Parent Category → Category → Sub-Category tree.
        /// Each level includes only active children of an active parent.
        /// <paramref name="companyId"/> is not a filter: the catalog tables
        /// have no CompanyId and stay global. The argument remains so the
        /// Create Ticket call site does not change.
        /// When: 2026-11-26. Order is SortOrder, then Id, at every level.
        /// </summary>
        public List<TicketCategoryDto> GetCategoryHierarchy(int companyId)
        {
            using (var context = new ItsmDbContext())
            {
                var parents = context.Categories.AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.SortOrder)
                    .ThenBy(c => c.Id)
                    .Select(c => new TicketCategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name
                    })
                    .ToList();

                var categories = context.SubCategories.AsNoTracking()
                    .Where(s => s.IsActive && s.Category.IsActive)
                    .OrderBy(s => s.SortOrder)
                    .ThenBy(s => s.Id)
                    .Select(s => new CatalogFlatRow
                    {
                        Id = s.Id,
                        Name = s.Name,
                        ParentId = s.CategoryId
                    })
                    .ToList();

                var items = context.Items.AsNoTracking()
                    .Where(i => i.IsActive
                        && i.SubCategory.IsActive
                        && i.SubCategory.Category.IsActive)
                    .OrderBy(i => i.SortOrder)
                    .ThenBy(i => i.Id)
                    .Select(i => new CatalogFlatRow
                    {
                        Id = i.Id,
                        Name = i.Name,
                        ParentId = i.SubCategoryId
                    })
                    .ToList();

                return AssembleHierarchy(parents, categories, items);
            }
        }

        /// <summary>
        /// Flat projection of a category or sub-category row. The list order
        /// is already SortOrder, then Id. Grouping must keep that order.
        /// </summary>
        private sealed class CatalogFlatRow
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public int ParentId { get; set; }
        }

        private static List<TicketCategoryDto> AssembleHierarchy(
            List<TicketCategoryDto> parents,
            List<CatalogFlatRow> categories,
            List<CatalogFlatRow> items)
        {
            var byParent = new Dictionary<int, TicketCategoryDto>();
            foreach (var parent in parents)
            {
                parent.SubCategories = new List<TicketSubCategoryDto>();
                byParent[parent.Id] = parent;
            }

            var byCategory = new Dictionary<int, TicketSubCategoryDto>();
            foreach (var row in categories)
            {
                TicketCategoryDto parent;
                if (!byParent.TryGetValue(row.ParentId, out parent))
                {
                    continue;
                }

                var category = new TicketSubCategoryDto
                {
                    Id = row.Id,
                    Name = row.Name,
                    Items = new List<TicketItemDto>()
                };
                parent.SubCategories.Add(category);
                byCategory[row.Id] = category;
            }

            foreach (var row in items)
            {
                TicketSubCategoryDto category;
                if (!byCategory.TryGetValue(row.ParentId, out category))
                {
                    continue;
                }

                category.Items.Add(new TicketItemDto
                {
                    Id = row.Id,
                    Name = row.Name
                });
            }

            return parents;
        }

        /// <summary>
        /// Loads active request types as display DTOs.
        /// </summary>
        public List<TicketLookupItemDto> GetRequestTypes()
        {
            using (var context = new ItsmDbContext())
            {
                return context.RequestTypes.AsNoTracking()
                    .Where(r => r.IsActive)
                    .OrderBy(r => r.Name)
                    .Select(r => new TicketLookupItemDto
                    {
                        Id = r.Id,
                        Name = r.Code + " - " + r.Name
                    })
                    .ToList();
            }
        }

        /// <summary>
        /// Loads active impacts as display DTOs (for button group rendering).
        /// </summary>
        public List<TicketLookupItemDto> GetImpacts()
        {
            using (var context = new ItsmDbContext())
            {
                return context.Impacts.AsNoTracking()
                    .Where(i => i.IsActive)
                    .OrderBy(i => i.SortOrder)
                    .Select(i => new TicketLookupItemDto
                    {
                        Id = i.Id,
                        Name = i.Name
                    })
                    .ToList();
            }
        }

        /// <summary>
        /// Loads active urgencies as display DTOs (for button group rendering).
        /// </summary>
        public List<TicketLookupItemDto> GetUrgencies()
        {
            using (var context = new ItsmDbContext())
            {
                return context.Urgencies.AsNoTracking()
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.SortOrder)
                    .Select(u => new TicketLookupItemDto
                    {
                        Id = u.Id,
                        Name = u.Name
                    })
                    .ToList();
            }
        }

        /// <summary>
        /// Validates the submitted Parent Category → Category → optional
        /// Sub-Category chain. Stored columns remain CategoryId,
        /// SubCategoryId, and ItemId.
        ///
        /// Validation performed:
        /// - Parent category exists and is active
        /// - Category exists, is active, and belongs to that parent
        /// - Sub-category, when supplied, exists, is active, and belongs to that category
        /// </summary>
        public HierarchyValidationResult ValidateHierarchy(
            int categoryId,
            int subCategoryId,
            int? itemId)
        {
            using (var context = new ItsmDbContext())
            {
                var categoryActive = context.Categories
                    .AsNoTracking()
                    .Any(c => c.Id == categoryId && c.IsActive);

                if (!categoryActive)
                {
                    return HierarchyValidationResult.Fail(
                        "The selected parent category is not available.");
                }

                bool subValid = context.SubCategories
                    .AsNoTracking()
                    .Any(s => s.Id == subCategoryId
                             && s.CategoryId == categoryId
                             && s.IsActive);

                if (!subValid)
                {
                    return HierarchyValidationResult.Fail(
                        "The selected category does not belong to the chosen parent category.");
                }

                if (itemId.HasValue)
                {
                    bool itemValid = context.Items
                        .AsNoTracking()
                        .Any(i => i.Id == itemId.Value
                                && i.SubCategoryId == subCategoryId
                                && i.IsActive);

                    if (!itemValid)
                    {
                        return HierarchyValidationResult.Fail(
                            "The selected sub-category does not belong to the chosen category.");
                    }
                }
            }

            return HierarchyValidationResult.Success();
        }

        /// <summary>
        /// Validates that the submitted Impact, Urgency, and RequestType IDs
        /// exist and are active. Returns a <see cref="HierarchyValidationResult"/>
        /// with null Error on success.
        /// </summary>
        public HierarchyValidationResult ValidateMasterDataIds(
            int impactId, int urgencyId, int requestTypeId)
        {
            using (var context = new ItsmDbContext())
            {
                if (!context.Impacts.AsNoTracking()
                    .Any(i => i.Id == impactId && i.IsActive))
                {
                    return HierarchyValidationResult.Fail(
                        "The selected impact level is not valid.");
                }

                if (!context.Urgencies.AsNoTracking()
                    .Any(u => u.Id == urgencyId && u.IsActive))
                {
                    return HierarchyValidationResult.Fail(
                        "The selected urgency level is not valid.");
                }

                if (!context.RequestTypes.AsNoTracking()
                    .Any(r => r.Id == requestTypeId && r.IsActive))
                {
                    return HierarchyValidationResult.Fail(
                        "The selected request type is not valid.");
                }
            }

            return HierarchyValidationResult.Success();
        }

        /// <summary>
        /// Resolves the employee's CompanyId from MST_UserMaster.
        /// Returns 0 if not found (caller should deny access).
        /// </summary>
        public int ResolveCompanyId(string employeeCode)
        {
            return ItsmTenantContext.ResolveCompanyId(employeeCode);
        }
    }
}
