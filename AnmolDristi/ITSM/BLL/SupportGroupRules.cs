using System;
using System.Collections.Generic;
using System.Linq;

namespace AnmolDristi.ITSM.BLL
{
    /// <summary>
    /// Pure R6 dispatch and membership rules — no EF, no System.Web.
    /// Unit tests compile this file directly.
    /// </summary>
    public sealed class DispatchOutcome
    {
        public string AssigneeCode { get; set; }

        public int? SupportGroupId { get; set; }

        public bool IsGroupQueue
        {
            get
            {
                return SupportGroupId.HasValue
                    && string.IsNullOrWhiteSpace(AssigneeCode);
            }
        }
    }

    public sealed class GroupMappingRow
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int BranchId { get; set; }
        public int CategoryId { get; set; }
        public int? SubCategoryId { get; set; }
        public int SupportGroupId { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class IndividualMappingRow
    {
        public int Id { get; set; }
        public int BranchId { get; set; }
        public int? CategoryId { get; set; }
        public int? SubCategoryId { get; set; }
        public int? ItemId { get; set; }
        public string SupportAgentCode { get; set; }
        public bool IsBranchDefault { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class GroupMemberRow
    {
        public int SupportGroupId { get; set; }
        public string EmployeeCode { get; set; }
        public bool IsTeamLeader { get; set; }
        public bool IsActive { get; set; }
        public string EmployeeStatus { get; set; }
        public int? EmployeeCompanyId { get; set; }
    }

    public static class SupportGroupRules
    {
        /// <summary>
        /// Approved precedence: Category group → Parent Category group →
        /// existing individual cascade (Item → SubCategory → Category → BranchDefault).
        /// </summary>
        public static DispatchOutcome ResolveDispatch(
            int companyId,
            int branchId,
            int categoryId,
            int subCategoryId,
            int? itemId,
            IEnumerable<GroupMappingRow> groupMappings,
            IEnumerable<IndividualMappingRow> individualMappings)
        {
            var groups = (groupMappings ?? Enumerable.Empty<GroupMappingRow>())
                .Where(m => m.IsActive
                    && m.CompanyId == companyId
                    && m.BranchId == branchId)
                .OrderBy(m => m.Id)
                .ToList();

            // Category-level mappings must match both SubCategoryId and parent CategoryId.
            var categoryGroup = groups.FirstOrDefault(m =>
                m.SubCategoryId.HasValue
                && m.SubCategoryId.Value == subCategoryId
                && m.CategoryId == categoryId);
            if (categoryGroup != null)
            {
                return GroupWin(categoryGroup.SupportGroupId);
            }

            var parentGroup = groups.FirstOrDefault(m =>
                !m.SubCategoryId.HasValue
                && m.CategoryId == categoryId);
            if (parentGroup != null)
            {
                return GroupWin(parentGroup.SupportGroupId);
            }

            var agent = ResolveIndividual(
                branchId, categoryId, subCategoryId, itemId, individualMappings);
            return new DispatchOutcome
            {
                AssigneeCode = agent,
                SupportGroupId = null
            };
        }

        public static DispatchOutcome GroupWin(int supportGroupId)
        {
            return new DispatchOutcome
            {
                AssigneeCode = null,
                SupportGroupId = supportGroupId
            };
        }

        public static string ResolveIndividual(
            int branchId,
            int categoryId,
            int subCategoryId,
            int? itemId,
            IEnumerable<IndividualMappingRow> individualMappings)
        {
            var rows = (individualMappings ?? Enumerable.Empty<IndividualMappingRow>())
                .Where(m => m.IsActive && m.BranchId == branchId)
                .OrderBy(m => m.Id)
                .ToList();

            if (itemId.HasValue)
            {
                var item = rows.FirstOrDefault(m =>
                    !m.IsBranchDefault
                    && m.CategoryId == categoryId
                    && m.SubCategoryId == subCategoryId
                    && m.ItemId == itemId.Value);
                if (item != null)
                {
                    return NormalizeAgent(item.SupportAgentCode);
                }
            }

            var sub = rows.FirstOrDefault(m =>
                !m.IsBranchDefault
                && m.CategoryId == categoryId
                && m.SubCategoryId == subCategoryId
                && m.ItemId == null);
            if (sub != null)
            {
                return NormalizeAgent(sub.SupportAgentCode);
            }

            var cat = rows.FirstOrDefault(m =>
                !m.IsBranchDefault
                && m.CategoryId == categoryId
                && m.SubCategoryId == null
                && m.ItemId == null);
            if (cat != null)
            {
                return NormalizeAgent(cat.SupportAgentCode);
            }

            var branchDefault = rows.FirstOrDefault(m =>
                m.IsBranchDefault
                && m.CategoryId == null
                && m.SubCategoryId == null
                && m.ItemId == null);
            if (branchDefault != null)
            {
                return NormalizeAgent(branchDefault.SupportAgentCode);
            }

            return null;
        }

        public static bool IsActiveTeamLeader(
            IEnumerable<GroupMemberRow> members,
            int supportGroupId,
            string actorCode,
            int ticketCompanyId)
        {
            if (string.IsNullOrWhiteSpace(actorCode) || ticketCompanyId <= 0)
            {
                return false;
            }

            return (members ?? Enumerable.Empty<GroupMemberRow>()).Any(m =>
                m.SupportGroupId == supportGroupId
                && m.IsActive
                && m.IsTeamLeader
                && string.Equals(m.EmployeeCode, actorCode, StringComparison.OrdinalIgnoreCase)
                && string.Equals(m.EmployeeStatus, "Active", StringComparison.OrdinalIgnoreCase)
                && m.EmployeeCompanyId.HasValue
                && m.EmployeeCompanyId.Value == ticketCompanyId);
        }

        public static bool IsActiveGroupMember(
            IEnumerable<GroupMemberRow> members,
            int supportGroupId,
            string employeeCode,
            int ticketCompanyId)
        {
            if (string.IsNullOrWhiteSpace(employeeCode) || ticketCompanyId <= 0)
            {
                return false;
            }

            return (members ?? Enumerable.Empty<GroupMemberRow>()).Any(m =>
                m.SupportGroupId == supportGroupId
                && m.IsActive
                && string.Equals(m.EmployeeCode, employeeCode, StringComparison.OrdinalIgnoreCase)
                && string.Equals(m.EmployeeStatus, "Active", StringComparison.OrdinalIgnoreCase)
                && m.EmployeeCompanyId.HasValue
                && m.EmployeeCompanyId.Value == ticketCompanyId);
        }

        public static bool CanClaimAsTeamLeader(
            IEnumerable<GroupMemberRow> members,
            int? supportGroupId,
            string actorCode,
            int ticketCompanyId,
            string currentAssigneeCode)
        {
            if (!supportGroupId.HasValue || !string.IsNullOrWhiteSpace(currentAssigneeCode))
            {
                return false;
            }

            return IsActiveTeamLeader(members, supportGroupId.Value, actorCode, ticketCompanyId);
        }

        public static bool CanTeamLeaderAssignMember(
            IEnumerable<GroupMemberRow> members,
            int? supportGroupId,
            string actorCode,
            string targetCode,
            int ticketCompanyId,
            string currentAssigneeCode)
        {
            if (!supportGroupId.HasValue
                || string.IsNullOrWhiteSpace(targetCode)
                || !string.IsNullOrWhiteSpace(currentAssigneeCode))
            {
                return false;
            }

            if (!IsActiveTeamLeader(members, supportGroupId.Value, actorCode, ticketCompanyId))
            {
                return false;
            }

            return IsActiveGroupMember(members, supportGroupId.Value, targetCode, ticketCompanyId);
        }

        public static string ValidateOneActiveLeader(
            IEnumerable<GroupMemberRow> members,
            int supportGroupId,
            string candidateEmployeeCode,
            bool candidateIsLeader,
            bool candidateIsActive)
        {
            if (!candidateIsActive || !candidateIsLeader)
            {
                return null;
            }

            var other = (members ?? Enumerable.Empty<GroupMemberRow>()).Any(m =>
                m.SupportGroupId == supportGroupId
                && m.IsActive
                && m.IsTeamLeader
                && !string.Equals(m.EmployeeCode, candidateEmployeeCode, StringComparison.OrdinalIgnoreCase));

            return other
                ? "Only one active Team Leader is allowed per support group."
                : null;
        }

        public static string ValidateActiveGroupHasLeader(
            IEnumerable<GroupMemberRow> members,
            int supportGroupId,
            bool groupIsActive)
        {
            if (!groupIsActive)
            {
                return null;
            }

            var hasLeader = (members ?? Enumerable.Empty<GroupMemberRow>()).Any(m =>
                m.SupportGroupId == supportGroupId
                && m.IsActive
                && m.IsTeamLeader
                && string.Equals(m.EmployeeStatus, "Active", StringComparison.OrdinalIgnoreCase));

            return hasLeader
                ? null
                : "An active support group requires one active Team Leader.";
        }

        public static string ValidateNoConflictingIndividual(
            bool isCategoryLevel,
            int categoryId,
            int? subCategoryId,
            IEnumerable<IndividualMappingRow> individualsAtBranch)
        {
            var rows = individualsAtBranch ?? Enumerable.Empty<IndividualMappingRow>();

            if (isCategoryLevel)
            {
                if (!subCategoryId.HasValue)
                {
                    return "Category-level group mapping requires SubCategoryId.";
                }

                var conflict = rows.Any(m =>
                    m.IsActive
                    && !m.IsBranchDefault
                    && m.CategoryId == categoryId
                    && m.SubCategoryId == subCategoryId.Value
                    && m.ItemId == null);

                return conflict
                    ? "An individual support mapping already exists at this Category level."
                    : null;
            }

            var parentConflict = rows.Any(m =>
                m.IsActive
                && !m.IsBranchDefault
                && m.CategoryId == categoryId
                && m.SubCategoryId == null
                && m.ItemId == null);

            return parentConflict
                ? "An individual support mapping already exists at this Parent Category level."
                : null;
        }

        public static string ValidateNoConflictingGroup(
            bool isCategoryLevel,
            int categoryId,
            int? subCategoryId,
            IEnumerable<GroupMappingRow> groupsAtBranch)
        {
            var rows = groupsAtBranch ?? Enumerable.Empty<GroupMappingRow>();

            if (isCategoryLevel)
            {
                if (!subCategoryId.HasValue)
                {
                    return "Category-level check requires SubCategoryId.";
                }

                var conflict = rows.Any(m =>
                    m.IsActive
                    && m.SubCategoryId.HasValue
                    && m.SubCategoryId.Value == subCategoryId.Value);

                return conflict
                    ? "A support group mapping already exists at this Category level."
                    : null;
            }

            var parentConflict = rows.Any(m =>
                m.IsActive
                && !m.SubCategoryId.HasValue
                && m.CategoryId == categoryId);

            return parentConflict
                ? "A support group mapping already exists at this Parent Category level."
                : null;
        }

        public static string ValidateDeactivateGroup(bool hasUnresolvedTickets)
        {
            return hasUnresolvedTickets
                ? "Cannot deactivate a support group that still owns unresolved tickets."
                : null;
        }

        private static string NormalizeAgent(string code)
        {
            return string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        }
    }
}
