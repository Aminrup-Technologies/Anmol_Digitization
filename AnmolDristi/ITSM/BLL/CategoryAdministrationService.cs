// ========================================================================
// When: 2026-11-25
// What: Administer the global ITSM catalog: Parent Category
//       (itsm.MST_Category), Category (itsm.MST_SubCategory), and optional
//       Sub-Category (itsm.MST_Item).
// Why:  ITSM Phase 1B R1. Entries are added, renamed, or deactivated.
//       Rows are not deleted, and a parent is not moved once a ticket or
//       support mapping references the entry. No CompanyId filter: these
//       tables are global. Does not write tickets, status, or audit rows,
//       and does not write dbo.MST_Category.
// ========================================================================

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Linq;
using AnmolDristi.ITSM.DAL;
using AnmolDristi.ITSM.DAL.Entities;

namespace AnmolDristi.ITSM.BLL
{
    /// <summary>
    /// One catalog row for the category administration grids.
    /// </summary>
    public class CatalogEntryDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public bool IsActive { get; set; }

        public int ParentId { get; set; }

        public string ParentName { get; set; }

        public bool ParentIsActive { get; set; }

        /// <summary>
        /// True when a ticket or support mapping already stores this row,
        /// or stores one of its child items. Parent changes are refused.
        /// </summary>
        public bool IsReferenced { get; set; }
    }

    /// <summary>
    /// Active parent choice for an add or move dropdown.
    /// </summary>
    public class CatalogOptionDto
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    /// <summary>
    /// Result of a catalog add or save. <see cref="Error"/> is safe to show.
    /// </summary>
    public class CatalogSaveResult
    {
        public bool Succeeded { get; set; }

        public string Error { get; set; }

        public static CatalogSaveResult Ok()
        {
            return new CatalogSaveResult { Succeeded = true };
        }

        public static CatalogSaveResult Fail(string error)
        {
            return new CatalogSaveResult { Succeeded = false, Error = error };
        }
    }

    /// <summary>
    /// BLL for ITSM category administration. Reads project DTOs with
    /// AsNoTracking and Select. Writes load one tracked catalog row.
    /// Independent of System.Web.
    /// </summary>
    public class CategoryAdministrationService
    {
        private const int MaxNameLength = 100;

        public List<CatalogEntryDto> GetParentCategories()
        {
            using (var context = new ItsmDbContext())
            {
                var referenced = LoadReferencedParentIds(context);
                var rows = context.Categories
                    .AsNoTracking()
                    .OrderBy(c => c.Name)
                    .Select(c => new CatalogEntryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        IsActive = c.IsActive,
                        ParentIsActive = true
                    })
                    .ToList();

                MarkReferenced(rows, referenced);
                return rows;
            }
        }

        public List<CatalogEntryDto> GetCategories()
        {
            using (var context = new ItsmDbContext())
            {
                var referenced = LoadReferencedCategoryIds(context);
                var rows = context.SubCategories
                    .AsNoTracking()
                    .OrderBy(s => s.Category.Name)
                    .ThenBy(s => s.Name)
                    .Select(s => new CatalogEntryDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        IsActive = s.IsActive,
                        ParentId = s.CategoryId,
                        ParentName = s.Category.Name,
                        ParentIsActive = s.Category.IsActive
                    })
                    .ToList();

                MarkReferenced(rows, referenced);
                return rows;
            }
        }

        public List<CatalogEntryDto> GetSubCategories()
        {
            using (var context = new ItsmDbContext())
            {
                var referenced = LoadReferencedSubCategoryIds(context);
                var rows = context.Items
                    .AsNoTracking()
                    .OrderBy(i => i.SubCategory.Category.Name)
                    .ThenBy(i => i.SubCategory.Name)
                    .ThenBy(i => i.Name)
                    .Select(i => new CatalogEntryDto
                    {
                        Id = i.Id,
                        Name = i.Name,
                        IsActive = i.IsActive,
                        ParentId = i.SubCategoryId,
                        ParentName = i.SubCategory.Category.Name + " / " + i.SubCategory.Name,
                        ParentIsActive = i.SubCategory.IsActive && i.SubCategory.Category.IsActive
                    })
                    .ToList();

                MarkReferenced(rows, referenced);
                return rows;
            }
        }

        /// <summary>
        /// Active parent categories for a new or unreferenced category.
        /// </summary>
        public List<CatalogOptionDto> GetActiveParentCategoryOptions()
        {
            using (var context = new ItsmDbContext())
            {
                return context.Categories
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .Select(c => new CatalogOptionDto
                    {
                        Id = c.Id,
                        Name = c.Name
                    })
                    .ToList();
            }
        }

        /// <summary>
        /// Active categories whose parent category is also active.
        /// Used when adding or moving an unreferenced sub-category.
        /// </summary>
        public List<CatalogOptionDto> GetActiveCategoryOptions()
        {
            using (var context = new ItsmDbContext())
            {
                return context.SubCategories
                    .AsNoTracking()
                    .Where(s => s.IsActive && s.Category.IsActive)
                    .OrderBy(s => s.Category.Name)
                    .ThenBy(s => s.Name)
                    .Select(s => new CatalogOptionDto
                    {
                        Id = s.Id,
                        Name = s.Category.Name + " / " + s.Name
                    })
                    .ToList();
            }
        }

        public CatalogSaveResult AddParentCategory(string name, string actorCode)
        {
            var normalized = NormalizeName(name);
            var nameError = ValidateName(normalized);
            if (nameError != null)
            {
                return CatalogSaveResult.Fail(nameError);
            }

            using (var context = new ItsmDbContext())
            {
                var actorError = RequireActor(context, actorCode);
                if (actorError != null)
                {
                    return CatalogSaveResult.Fail(actorError);
                }

                if (ParentNameTaken(context, 0, normalized))
                {
                    return CatalogSaveResult.Fail(DuplicateNameMessage);
                }

                using (var transaction = context.Database.BeginTransaction())
                {
                    context.Categories.Add(new Category
                    {
                        Name = normalized,
                        IsActive = true
                    });
                    return Commit(context, transaction);
                }
            }
        }

        public CatalogSaveResult SaveParentCategory(int id, string name, bool isActive, string actorCode)
        {
            var normalized = NormalizeName(name);
            var nameError = ValidateName(normalized);
            if (nameError != null)
            {
                return CatalogSaveResult.Fail(nameError);
            }

            using (var context = new ItsmDbContext())
            {
                var actorError = RequireActor(context, actorCode);
                if (actorError != null)
                {
                    return CatalogSaveResult.Fail(actorError);
                }

                var current = context.Categories.FirstOrDefault(c => c.Id == id);
                if (current == null)
                {
                    return CatalogSaveResult.Fail("The parent category was not found.");
                }

                if (ParentNameTaken(context, id, normalized))
                {
                    return CatalogSaveResult.Fail(DuplicateNameMessage);
                }

                using (var transaction = context.Database.BeginTransaction())
                {
                    current.Name = normalized;
                    current.IsActive = isActive;
                    return Commit(context, transaction);
                }
            }
        }

        public CatalogSaveResult AddCategory(int parentCategoryId, string name, string actorCode)
        {
            var normalized = NormalizeName(name);
            var nameError = ValidateName(normalized);
            if (nameError != null)
            {
                return CatalogSaveResult.Fail(nameError);
            }

            using (var context = new ItsmDbContext())
            {
                var actorError = RequireActor(context, actorCode);
                if (actorError != null)
                {
                    return CatalogSaveResult.Fail(actorError);
                }

                var parentError = ValidateParentCategory(context, parentCategoryId, true);
                if (parentError != null)
                {
                    return CatalogSaveResult.Fail(parentError);
                }

                if (CategoryNameTaken(context, 0, parentCategoryId, normalized))
                {
                    return CatalogSaveResult.Fail(DuplicateNameMessage);
                }

                using (var transaction = context.Database.BeginTransaction())
                {
                    context.SubCategories.Add(new SubCategory
                    {
                        CategoryId = parentCategoryId,
                        Name = normalized,
                        IsActive = true
                    });
                    return Commit(context, transaction);
                }
            }
        }

        public CatalogSaveResult SaveCategory(
            int id,
            int parentCategoryId,
            string name,
            bool isActive,
            string actorCode)
        {
            var normalized = NormalizeName(name);
            var nameError = ValidateName(normalized);
            if (nameError != null)
            {
                return CatalogSaveResult.Fail(nameError);
            }

            using (var context = new ItsmDbContext())
            {
                var actorError = RequireActor(context, actorCode);
                if (actorError != null)
                {
                    return CatalogSaveResult.Fail(actorError);
                }

                var current = context.SubCategories.FirstOrDefault(s => s.Id == id);
                if (current == null)
                {
                    return CatalogSaveResult.Fail("The category was not found.");
                }

                if (current.CategoryId != parentCategoryId && IsCategoryReferenced(context, id))
                {
                    return CatalogSaveResult.Fail(ParentLockedMessage);
                }

                var parentError = ValidateParentCategory(context, parentCategoryId, isActive);
                if (parentError != null)
                {
                    return CatalogSaveResult.Fail(parentError);
                }

                if (CategoryNameTaken(context, id, parentCategoryId, normalized))
                {
                    return CatalogSaveResult.Fail(DuplicateNameMessage);
                }

                using (var transaction = context.Database.BeginTransaction())
                {
                    current.CategoryId = parentCategoryId;
                    current.Name = normalized;
                    current.IsActive = isActive;
                    return Commit(context, transaction);
                }
            }
        }

        public CatalogSaveResult AddSubCategory(int categoryId, string name, string actorCode)
        {
            var normalized = NormalizeName(name);
            var nameError = ValidateName(normalized);
            if (nameError != null)
            {
                return CatalogSaveResult.Fail(nameError);
            }

            using (var context = new ItsmDbContext())
            {
                var actorError = RequireActor(context, actorCode);
                if (actorError != null)
                {
                    return CatalogSaveResult.Fail(actorError);
                }

                var parentError = ValidateCategoryParent(context, categoryId, true);
                if (parentError != null)
                {
                    return CatalogSaveResult.Fail(parentError);
                }

                if (SubCategoryNameTaken(context, 0, categoryId, normalized))
                {
                    return CatalogSaveResult.Fail(DuplicateNameMessage);
                }

                using (var transaction = context.Database.BeginTransaction())
                {
                    context.Items.Add(new Item
                    {
                        SubCategoryId = categoryId,
                        Name = normalized,
                        IsActive = true
                    });
                    return Commit(context, transaction);
                }
            }
        }

        public CatalogSaveResult SaveSubCategory(
            int id,
            int categoryId,
            string name,
            bool isActive,
            string actorCode)
        {
            var normalized = NormalizeName(name);
            var nameError = ValidateName(normalized);
            if (nameError != null)
            {
                return CatalogSaveResult.Fail(nameError);
            }

            using (var context = new ItsmDbContext())
            {
                var actorError = RequireActor(context, actorCode);
                if (actorError != null)
                {
                    return CatalogSaveResult.Fail(actorError);
                }

                var current = context.Items.FirstOrDefault(i => i.Id == id);
                if (current == null)
                {
                    return CatalogSaveResult.Fail("The sub-category was not found.");
                }

                if (current.SubCategoryId != categoryId && IsSubCategoryReferenced(context, id))
                {
                    return CatalogSaveResult.Fail(ParentLockedMessage);
                }

                var parentError = ValidateCategoryParent(context, categoryId, isActive);
                if (parentError != null)
                {
                    return CatalogSaveResult.Fail(parentError);
                }

                if (SubCategoryNameTaken(context, id, categoryId, normalized))
                {
                    return CatalogSaveResult.Fail(DuplicateNameMessage);
                }

                using (var transaction = context.Database.BeginTransaction())
                {
                    current.SubCategoryId = categoryId;
                    current.Name = normalized;
                    current.IsActive = isActive;
                    return Commit(context, transaction);
                }
            }
        }

        private const string DuplicateNameMessage =
            "An entry with this name already exists under the same parent. Edit that entry instead.";

        private const string ParentLockedMessage =
            "This entry is already used by a ticket or a support mapping, so its parent cannot be changed.";

        private static void MarkReferenced(List<CatalogEntryDto> rows, HashSet<int> referenced)
        {
            foreach (var row in rows)
            {
                row.IsReferenced = referenced.Contains(row.Id);
            }
        }

        private static HashSet<int> LoadReferencedParentIds(ItsmDbContext context)
        {
            var ids = new HashSet<int>(
                context.Tickets.AsNoTracking()
                    .Select(t => t.CategoryId)
                    .Distinct()
                    .ToList());

            foreach (var id in context.SupportMappings.AsNoTracking()
                .Where(m => m.CategoryId != null)
                .Select(m => m.CategoryId.Value)
                .Distinct()
                .ToList())
            {
                ids.Add(id);
            }

            return ids;
        }

        private static HashSet<int> LoadReferencedCategoryIds(ItsmDbContext context)
        {
            var ids = new HashSet<int>(
                context.Tickets.AsNoTracking()
                    .Select(t => t.SubCategoryId)
                    .Distinct()
                    .ToList());

            foreach (var id in context.SupportMappings.AsNoTracking()
                .Where(m => m.SubCategoryId != null)
                .Select(m => m.SubCategoryId.Value)
                .Distinct()
                .ToList())
            {
                ids.Add(id);
            }

            foreach (var id in context.Items.AsNoTracking()
                .Where(i => context.Tickets.Any(t => t.ItemId == i.Id)
                         || context.SupportMappings.Any(m => m.ItemId == i.Id))
                .Select(i => i.SubCategoryId)
                .Distinct()
                .ToList())
            {
                ids.Add(id);
            }

            return ids;
        }

        private static HashSet<int> LoadReferencedSubCategoryIds(ItsmDbContext context)
        {
            var ids = new HashSet<int>(
                context.Tickets.AsNoTracking()
                    .Where(t => t.ItemId != null)
                    .Select(t => t.ItemId.Value)
                    .Distinct()
                    .ToList());

            foreach (var id in context.SupportMappings.AsNoTracking()
                .Where(m => m.ItemId != null)
                .Select(m => m.ItemId.Value)
                .Distinct()
                .ToList())
            {
                ids.Add(id);
            }

            return ids;
        }

        private static bool IsCategoryReferenced(ItsmDbContext context, int subCategoryId)
        {
            if (context.Tickets.AsNoTracking().Any(t => t.SubCategoryId == subCategoryId))
            {
                return true;
            }

            if (context.SupportMappings.AsNoTracking().Any(m => m.SubCategoryId == subCategoryId))
            {
                return true;
            }

            return context.Items.AsNoTracking().Any(i =>
                i.SubCategoryId == subCategoryId
                && (context.Tickets.Any(t => t.ItemId == i.Id)
                    || context.SupportMappings.Any(m => m.ItemId == i.Id)));
        }

        private static bool IsSubCategoryReferenced(ItsmDbContext context, int itemId)
        {
            return context.Tickets.AsNoTracking().Any(t => t.ItemId == itemId)
                || context.SupportMappings.AsNoTracking().Any(m => m.ItemId == itemId);
        }

        private static string ValidateParentCategory(ItsmDbContext context, int parentCategoryId, bool childWillBeActive)
        {
            var parent = context.Categories
                .AsNoTracking()
                .Where(c => c.Id == parentCategoryId)
                .Select(c => new { c.IsActive })
                .FirstOrDefault();

            if (parent == null)
            {
                return "Select a parent category.";
            }

            if (childWillBeActive && !parent.IsActive)
            {
                return "Activate the parent category before activating this category.";
            }

            return null;
        }

        private static string ValidateCategoryParent(ItsmDbContext context, int categoryId, bool childWillBeActive)
        {
            var parent = context.SubCategories
                .AsNoTracking()
                .Where(s => s.Id == categoryId)
                .Select(s => new
                {
                    s.IsActive,
                    CategoryIsActive = s.Category.IsActive
                })
                .FirstOrDefault();

            if (parent == null)
            {
                return "Select a category.";
            }

            if (childWillBeActive && (!parent.IsActive || !parent.CategoryIsActive))
            {
                return "Activate the category and its parent category before activating this sub-category.";
            }

            return null;
        }

        private static bool ParentNameTaken(ItsmDbContext context, int id, string normalized)
        {
            var names = context.Categories
                .AsNoTracking()
                .Where(c => c.Id != id)
                .Select(c => c.Name)
                .ToList();
            return NameTaken(names, normalized);
        }

        private static bool CategoryNameTaken(
            ItsmDbContext context,
            int id,
            int parentCategoryId,
            string normalized)
        {
            var names = context.SubCategories
                .AsNoTracking()
                .Where(s => s.Id != id && s.CategoryId == parentCategoryId)
                .Select(s => s.Name)
                .ToList();
            return NameTaken(names, normalized);
        }

        private static bool SubCategoryNameTaken(
            ItsmDbContext context,
            int id,
            int categoryId,
            string normalized)
        {
            var names = context.Items
                .AsNoTracking()
                .Where(i => i.Id != id && i.SubCategoryId == categoryId)
                .Select(i => i.Name)
                .ToList();
            return NameTaken(names, normalized);
        }

        private static bool NameTaken(IEnumerable<string> names, string normalized)
        {
            foreach (var name in names)
            {
                if (name == null)
                {
                    continue;
                }

                if (string.Equals(name.Trim(), normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return name.Trim();
        }

        private static string ValidateName(string normalized)
        {
            if (normalized == null)
            {
                return "Enter a name.";
            }

            if (normalized.Length > MaxNameLength)
            {
                return "The name cannot exceed 100 characters.";
            }

            if (normalized.IndexOf('<') >= 0 || normalized.IndexOf('>') >= 0)
            {
                return "The name cannot contain < or >.";
            }

            return null;
        }

        private static string RequireActor(ItsmDbContext context, string actorCode)
        {
            if (string.IsNullOrWhiteSpace(actorCode))
            {
                return "A signed-in employee is required.";
            }

            var active = context.UserMasters
                .AsNoTracking()
                .Any(u => u.EmployeeCode == actorCode && u.EmployeeStatus == "Active");

            if (!active)
            {
                return "The signed-in employee is not active.";
            }

            return null;
        }

        private static CatalogSaveResult Commit(ItsmDbContext context, DbContextTransaction transaction)
        {
            try
            {
                context.SaveChanges();
                transaction.Commit();
                return CatalogSaveResult.Ok();
            }
            catch (DbUpdateException ex)
            {
                Trace.TraceError("ITSM category catalog update failed. " + ex);
                transaction.Rollback();
                return CatalogSaveResult.Fail("The catalog change could not be saved.");
            }
            catch (DbEntityValidationException ex)
            {
                Trace.TraceError("ITSM category catalog validation failed. " + ex);
                transaction.Rollback();
                return CatalogSaveResult.Fail("The catalog change could not be saved.");
            }
        }
    }
}
