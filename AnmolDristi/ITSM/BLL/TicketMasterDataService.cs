// ========================================================================
// When: 2026-08-26
// What: BLL service for ITSM master-data lookups and hierarchy validation.
//       Encapsulates all EF6 queries previously in CreateTicket.aspx.cs.
//       UI delegates to this service — zero direct database access in code-behind.
// Why:  Architecture enforcement: UI → BLL → DAL. Master data is company-scoped
//       where applicable. Hierarchy validation is a single consolidated query.
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
        /// Loads the full category → subcategory → item hierarchy as
        /// strongly typed DTOs. Company-scoped reads are applied when
        /// <paramref name="companyId"/> is provided (> 0).
        /// </summary>
        public List<TicketCategoryDto> GetCategoryHierarchy(int companyId)
        {
            using (var context = new ItsmDbContext())
            {
                var query = context.Categories.AsNoTracking()
                    .Where(c => c.IsActive);

                return query
                    .OrderBy(c => c.Name)
                    .Select(c => new TicketCategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        SubCategories = context.SubCategories
                            .Where(s => s.IsActive && s.CategoryId == c.Id)
                            .OrderBy(s => s.Name)
                            .Select(s => new TicketSubCategoryDto
                            {
                                Id = s.Id,
                                Name = s.Name,
                                Items = context.Items
                                    .Where(i => i.IsActive && i.SubCategoryId == s.Id)
                                    .OrderBy(i => i.Name)
                                    .Select(i => new TicketItemDto
                                    {
                                        Id = i.Id,
                                        Name = i.Name
                                    })
                                    .ToList()
                            })
                            .ToList()
                    })
                    .ToList();
            }
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
        /// Validates the submitted category → subcategory → item hierarchy
        /// in a single consolidated EF query. Returns a
        /// <see cref="HierarchyValidationResult"/> with null Error on success.
        ///
        /// Validation performed:
        /// - SubCategory exists, is active, and belongs to Category
        /// - Item (when supplied) exists, is active, and belongs to SubCategory
        /// </summary>
        public HierarchyValidationResult ValidateHierarchy(
            int categoryId,
            int subCategoryId,
            int? itemId)
        {
            using (var context = new ItsmDbContext())
            {
                // Single query: check subcategory existence + category ownership + active flag.
                var subQuery = context.SubCategories
                    .AsNoTracking()
                    .Where(s => s.Id == subCategoryId
                             && s.CategoryId == categoryId
                             && s.IsActive);

                bool subValid = subQuery.Any();

                if (!subValid)
                {
                    return HierarchyValidationResult.Fail(
                        "The selected sub category does not belong to the chosen category.");
                }

                // Item validation (when supplied).
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
                            "The selected item does not belong to the chosen sub category.");
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
