using System;

namespace AnmolDristi
{
    /// <summary>
    /// Narrow EditEmployee helper: resolve organizational persistence values
    /// without treating Select/"0" as an intentional replacement for an
    /// unresolved non-zero original.
    /// </summary>
    internal static class OrganizationalValueResolver
    {
        public const string SelectPlaceholderValue = "0";
        public const string UnresolvedItemTextPrefix = "Current value unavailable in master: ";
        public const string UnresolvedCssClass = "org-unresolved";

        public static readonly string[] Columns = new[]
        {
            "RegionId",
            "DivisionId",
            "DepartmentId",
            "SubDepartmentId",
            "CategoryId",
            "PlantId",
            "GradeId",
            "BranchId"
        };

        public static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim();
        }

        public static bool IsSelectPlaceholder(string value)
        {
            return Normalize(value) == SelectPlaceholderValue;
        }

        /// <summary>
        /// Non-zero persisted value that was not present in the dropdown datasource.
        /// Database 0 is never classified as unresolved (Select already uses "0").
        /// </summary>
        public static bool IsUnresolvedNonZero(string originalDatabaseValue, bool valueExistsInDropdown)
        {
            string original = Normalize(originalDatabaseValue);
            if (original.Length == 0 || IsSelectPlaceholder(original))
                return false;
            return !valueExistsInDropdown;
        }

        public static string UnresolvedItemText(string originalDatabaseValue)
        {
            return UnresolvedItemTextPrefix + Normalize(originalDatabaseValue);
        }

        /// <summary>
        /// Deterministic save resolver.
        /// resolved fields persist the posted selection (existing behavior, including 0).
        /// unresolved fields persist the original unless a non-placeholder list item is selected.
        /// </summary>
        public static string Resolve(
            string originalDatabaseValue,
            bool unresolvedAtLoad,
            string currentSelectedValue,
            bool selectedIsKnownListItem)
        {
            string original = Normalize(originalDatabaseValue);
            string selected = Normalize(currentSelectedValue);

            if (!unresolvedAtLoad)
                return selected;

            if (string.Equals(selected, original, StringComparison.Ordinal))
                return original;

            if (selectedIsKnownListItem
                && selected.Length > 0
                && !IsSelectPlaceholder(selected))
            {
                return selected;
            }

            return original;
        }
    }
}
