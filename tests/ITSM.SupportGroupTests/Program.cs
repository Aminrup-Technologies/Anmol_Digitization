using System;
using System.Collections.Generic;
using AnmolDristi.ITSM.BLL;

namespace AnmolDristi.ITSM.SupportGroupTests
{
    /// <summary>
    /// R6 pure-rule tests (15 cases). No database, IIS, or browser.
    /// Style mirrors tests/ITSM.AttachmentTests.
    /// </summary>
    internal static class Program
    {
        private static int _failures;

        private static int Main()
        {
            CategoryGroupBeatsParentCategoryGroup();
            ParentCategoryGroupWinsWhenCategoryHasNone();
            CategoryGroupBeatsParentCategoryIndividual();
            ParentCategoryIndividualWinsWhenNoGroups();
            UnmappedWhenNothingMatches();
            GroupWinLeavesAssigneeNullNotTeamLeader();
            IndividualItemMappingUsedWhenNoGroupWins();
            ConflictingIndividualAndGroupRejected();
            SecondActiveTeamLeaderRejected();
            ActivateGroupWithoutActiveLeaderRejected();
            InactiveMemberCannotBeAssignmentTarget();
            NonTeamLeaderCannotClaim();
            ActiveTeamLeaderCanClaim();
            ActiveTeamLeaderCanAssignActiveMember();
            DeactivateBlockedWhileUnresolvedTicketsReferenceGroup();
            // Critical gaps called out by independent review:
            HrInactiveTeamLeaderCannotSatisfyActiveGroup();
            CategoryGroupIgnoresMismatchedParentCategoryId();

            if (_failures == 0)
            {
                Console.WriteLine("All ITSM R6 support-group tests passed.");
                return 0;
            }

            Console.WriteLine(_failures + " ITSM R6 support-group test(s) failed.");
            return 1;
        }

        private static void CategoryGroupBeatsParentCategoryGroup()
        {
            var outcome = SupportGroupRules.ResolveDispatch(
                companyId: 1,
                branchId: 10,
                categoryId: 100,
                subCategoryId: 200,
                itemId: null,
                groupMappings: new[]
                {
                    Map(1, 1, 10, 100, null, 501),
                    Map(2, 1, 10, 100, 200, 502)
                },
                individualMappings: NoneIndividuals());

            Expect(outcome.SupportGroupId == 502, "1 Category group beats Parent Category group");
            Expect(outcome.AssigneeCode == null, "1 Category group leaves assignee null");
        }

        private static void ParentCategoryGroupWinsWhenCategoryHasNone()
        {
            var outcome = SupportGroupRules.ResolveDispatch(
                1, 10, 100, 200, null,
                new[] { Map(1, 1, 10, 100, null, 501) },
                NoneIndividuals());

            Expect(outcome.SupportGroupId == 501, "2 Parent Category group wins when Category has none");
            Expect(outcome.IsGroupQueue, "2 Parent Category group is group queue");
        }

        private static void CategoryGroupBeatsParentCategoryIndividual()
        {
            var outcome = SupportGroupRules.ResolveDispatch(
                1, 10, 100, 200, null,
                new[] { Map(1, 1, 10, 100, 200, 502) },
                new[]
                {
                    Ind(1, 10, 100, null, null, "AGENT1", false)
                });

            Expect(outcome.SupportGroupId == 502, "3 Category group beats Parent Category individual");
            Expect(outcome.AssigneeCode == null, "3 individual agent not assigned on group win");
        }

        private static void ParentCategoryIndividualWinsWhenNoGroups()
        {
            var outcome = SupportGroupRules.ResolveDispatch(
                1, 10, 100, 200, null,
                new GroupMappingRow[0],
                new[]
                {
                    Ind(1, 10, 100, null, null, "AGENT1", false)
                });

            Expect(outcome.SupportGroupId == null, "4 no group when only individual");
            Expect(outcome.AssigneeCode == "AGENT1", "4 Parent Category individual wins when no groups");
        }

        private static void UnmappedWhenNothingMatches()
        {
            var outcome = SupportGroupRules.ResolveDispatch(
                1, 10, 100, 200, null,
                new GroupMappingRow[0],
                NoneIndividuals());

            Expect(outcome.SupportGroupId == null && outcome.AssigneeCode == null,
                "5 unmapped when nothing matches");
        }

        private static void GroupWinLeavesAssigneeNullNotTeamLeader()
        {
            var outcome = SupportGroupRules.GroupWin(777);
            Expect(outcome.AssigneeCode == null, "6 group win assignee is null (not TL)");
            Expect(outcome.SupportGroupId == 777, "6 group win stores group id");
            Expect(outcome.IsGroupQueue, "6 group win is group queue");
        }

        private static void IndividualItemMappingUsedWhenNoGroupWins()
        {
            var outcome = SupportGroupRules.ResolveDispatch(
                1, 10, 100, 200, 300,
                new GroupMappingRow[0],
                new[]
                {
                    Ind(1, 10, 100, 200, 300, "ITEM_AGENT", false),
                    Ind(2, 10, 100, null, null, "CAT_AGENT", false)
                });

            Expect(outcome.AssigneeCode == "ITEM_AGENT",
                "7 individual Item mapping used when no group wins");
        }

        private static void ConflictingIndividualAndGroupRejected()
        {
            var individuals = new[]
            {
                Ind(1, 10, 100, 200, null, "AGENT1", false)
            };
            var err = SupportGroupRules.ValidateNoConflictingIndividual(
                true, 100, 200, individuals);
            Expect(err != null, "8 conflicting individual + group at Category level rejected");

            var parentErr = SupportGroupRules.ValidateNoConflictingIndividual(
                false, 100, null, new[] { Ind(2, 10, 100, null, null, "AGENT2", false) });
            Expect(parentErr != null, "8 conflicting individual at Parent Category rejected");
        }

        private static void SecondActiveTeamLeaderRejected()
        {
            var members = new List<GroupMemberRow>
            {
                Member(9, "TL1", true, true, "Active", 1)
            };
            var err = SupportGroupRules.ValidateOneActiveLeader(
                members, 9, "TL2", true, true);
            Expect(err != null, "9 second active Team Leader rejected");
        }

        private static void ActivateGroupWithoutActiveLeaderRejected()
        {
            var members = new List<GroupMemberRow>
            {
                Member(9, "M1", false, true, "Active", 1)
            };
            var err = SupportGroupRules.ValidateActiveGroupHasLeader(members, 9, true);
            Expect(err != null, "10 activate group without active TL rejected");
        }

        private static void InactiveMemberCannotBeAssignmentTarget()
        {
            var members = new List<GroupMemberRow>
            {
                Member(9, "TL1", true, true, "Active", 1),
                Member(9, "M2", false, true, "Inactive", 1)
            };
            var ok = SupportGroupRules.IsActiveGroupMember(members, 9, "M2", 1);
            Expect(!ok, "11 inactive member cannot be assignment target");
        }

        private static void NonTeamLeaderCannotClaim()
        {
            var members = new List<GroupMemberRow>
            {
                Member(9, "TL1", true, true, "Active", 1),
                Member(9, "M2", false, true, "Active", 1)
            };
            var ok = SupportGroupRules.CanClaimAsTeamLeader(members, 9, "M2", 1, null);
            Expect(!ok, "12 non-TL cannot claim");
        }

        private static void ActiveTeamLeaderCanClaim()
        {
            var members = new List<GroupMemberRow>
            {
                Member(9, "TL1", true, true, "Active", 1)
            };
            var ok = SupportGroupRules.CanClaimAsTeamLeader(members, 9, "TL1", 1, null);
            Expect(ok, "13 active TL can claim");
        }

        private static void ActiveTeamLeaderCanAssignActiveMember()
        {
            var members = new List<GroupMemberRow>
            {
                Member(9, "TL1", true, true, "Active", 1),
                Member(9, "M2", false, true, "Active", 1)
            };
            var ok = SupportGroupRules.CanTeamLeaderAssignMember(
                members, 9, "TL1", "M2", 1, null);
            Expect(ok, "14 active TL can assign active member");
        }

        private static void DeactivateBlockedWhileUnresolvedTicketsReferenceGroup()
        {
            var err = SupportGroupRules.ValidateDeactivateGroup(true);
            Expect(err != null, "15 deactivate blocked while unresolved tickets reference group");
            var ok = SupportGroupRules.ValidateDeactivateGroup(false);
            Expect(ok == null, "15 deactivate allowed when no unresolved tickets");
        }

        private static void HrInactiveTeamLeaderCannotSatisfyActiveGroup()
        {
            var members = new List<GroupMemberRow>
            {
                Member(9, "TL1", true, true, "Inactive", 1)
            };
            var err = SupportGroupRules.ValidateActiveGroupHasLeader(members, 9, true);
            Expect(err != null, "16 HR-inactive Team Leader cannot keep group active");
        }

        private static void CategoryGroupIgnoresMismatchedParentCategoryId()
        {
            var outcome = SupportGroupRules.ResolveDispatch(
                1, 10, 100, 200, null,
                new[]
                {
                    // SubCategory 200 belongs under parent 100 on the ticket,
                    // but mapping wrongly points at parent 999.
                    Map(1, 1, 10, 999, 200, 502),
                    Map(2, 1, 10, 100, null, 501)
                },
                NoneIndividuals());

            Expect(outcome.SupportGroupId == 501,
                "17 Category mapping with mismatched CategoryId does not win");
            Expect(outcome.IsGroupQueue, "17 falls through to matching Parent Category group");
        }

        private static GroupMappingRow Map(
            int id, int companyId, int branchId, int categoryId, int? subCategoryId, int groupId)
        {
            return new GroupMappingRow
            {
                Id = id,
                CompanyId = companyId,
                BranchId = branchId,
                CategoryId = categoryId,
                SubCategoryId = subCategoryId,
                SupportGroupId = groupId,
                IsActive = true
            };
        }

        private static IndividualMappingRow Ind(
            int id, int branchId, int? categoryId, int? subCategoryId, int? itemId,
            string agent, bool isBranchDefault)
        {
            return new IndividualMappingRow
            {
                Id = id,
                BranchId = branchId,
                CategoryId = categoryId,
                SubCategoryId = subCategoryId,
                ItemId = itemId,
                SupportAgentCode = agent,
                IsBranchDefault = isBranchDefault,
                IsActive = true
            };
        }

        private static IndividualMappingRow[] NoneIndividuals()
        {
            return new IndividualMappingRow[0];
        }

        private static GroupMemberRow Member(
            int groupId, string code, bool isTl, bool isActive, string status, int companyId)
        {
            return new GroupMemberRow
            {
                SupportGroupId = groupId,
                EmployeeCode = code,
                IsTeamLeader = isTl,
                IsActive = isActive,
                EmployeeStatus = status,
                EmployeeCompanyId = companyId
            };
        }

        private static void Expect(bool condition, string name)
        {
            if (condition)
            {
                Console.WriteLine("PASS  " + name);
                return;
            }

            _failures++;
            Console.WriteLine("FAIL  " + name);
        }
    }
}
