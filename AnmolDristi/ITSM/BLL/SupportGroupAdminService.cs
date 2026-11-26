using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using AnmolDristi.ITSM.DAL;
using AnmolDristi.ITSM.DAL.Entities;

namespace AnmolDristi.ITSM.BLL
{
    public class SupportGroupListItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CompanyId { get; set; }
        public int BranchId { get; set; }
        public string BranchName { get; set; }
        public bool IsActive { get; set; }
        public string TeamLeaderCode { get; set; }
        public string TeamLeaderName { get; set; }
        public int ActiveMemberCount { get; set; }
    }

    public class SupportGroupMemberDto
    {
        public int Id { get; set; }
        public int SupportGroupId { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public bool IsTeamLeader { get; set; }
        public bool IsActive { get; set; }
    }

    public class SupportGroupMappingDto
    {
        public int Id { get; set; }
        public int SupportGroupId { get; set; }
        public int CompanyId { get; set; }
        public int BranchId { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int? SubCategoryId { get; set; }
        public string SubCategoryName { get; set; }
        public bool IsActive { get; set; }
        public string LevelLabel { get; set; }
    }

    public class SupportGroupAdminResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public int? Id { get; set; }

        public static SupportGroupAdminResult Ok(int? id = null)
        {
            return new SupportGroupAdminResult { Success = true, Id = id };
        }

        public static SupportGroupAdminResult Fail(string error)
        {
            return new SupportGroupAdminResult { Success = false, Error = error };
        }
    }

    /// <summary>
    /// R6 admin BLL — company and branch on every operation. No System.Web.
    /// </summary>
    public class SupportGroupAdminService
    {
        public List<SupportGroupListItemDto> ListGroups(int companyId, int branchId)
        {
            if (companyId <= 0 || branchId <= 0)
            {
                return new List<SupportGroupListItemDto>();
            }

            using (var context = new ItsmDbContext())
            {
                var groups = context.SupportGroups
                    .AsNoTracking()
                    .Where(g => g.CompanyId == companyId && g.BranchId == branchId)
                    .Select(g => new
                    {
                        g.Id,
                        g.Name,
                        g.CompanyId,
                        g.BranchId,
                        BranchName = g.Branch != null ? g.Branch.Branch_Description : "",
                        g.IsActive
                    })
                    .ToList();

                var groupIds = groups.Select(g => g.Id).ToList();
                var members = context.SupportGroupMembers
                    .AsNoTracking()
                    .Where(m => groupIds.Contains(m.SupportGroupId) && m.IsActive)
                    .Select(m => new
                    {
                        m.SupportGroupId,
                        m.EmployeeCode,
                        m.IsTeamLeader,
                        EmployeeName = m.Employee != null ? m.Employee.EmployeeName : m.EmployeeCode
                    })
                    .ToList();

                return groups.Select(g =>
                {
                    var active = members.Where(m => m.SupportGroupId == g.Id).ToList();
                    var tl = active.FirstOrDefault(m => m.IsTeamLeader);
                    return new SupportGroupListItemDto
                    {
                        Id = g.Id,
                        Name = g.Name,
                        CompanyId = g.CompanyId,
                        BranchId = g.BranchId,
                        BranchName = g.BranchName,
                        IsActive = g.IsActive,
                        TeamLeaderCode = tl != null ? tl.EmployeeCode : null,
                        TeamLeaderName = tl != null ? tl.EmployeeName : null,
                        ActiveMemberCount = active.Count
                    };
                }).OrderBy(g => g.Name).ToList();
            }
        }

        public List<SupportGroupMemberDto> ListMembers(int companyId, int branchId, int supportGroupId)
        {
            using (var context = new ItsmDbContext())
            {
                if (!GroupOwned(context, companyId, branchId, supportGroupId))
                {
                    return new List<SupportGroupMemberDto>();
                }

                return context.SupportGroupMembers
                    .AsNoTracking()
                    .Where(m => m.SupportGroupId == supportGroupId)
                    .Select(m => new SupportGroupMemberDto
                    {
                        Id = m.Id,
                        SupportGroupId = m.SupportGroupId,
                        EmployeeCode = m.EmployeeCode,
                        EmployeeName = m.Employee != null ? m.Employee.EmployeeName : m.EmployeeCode,
                        IsTeamLeader = m.IsTeamLeader,
                        IsActive = m.IsActive
                    })
                    .OrderByDescending(m => m.IsTeamLeader)
                    .ThenBy(m => m.EmployeeName)
                    .ToList();
            }
        }

        public List<SupportGroupMappingDto> ListMappings(int companyId, int branchId, int supportGroupId)
        {
            using (var context = new ItsmDbContext())
            {
                if (!GroupOwned(context, companyId, branchId, supportGroupId))
                {
                    return new List<SupportGroupMappingDto>();
                }

                return context.SupportGroupMappings
                    .AsNoTracking()
                    .Where(m => m.SupportGroupId == supportGroupId
                        && m.CompanyId == companyId
                        && m.BranchId == branchId)
                    .Select(m => new SupportGroupMappingDto
                    {
                        Id = m.Id,
                        SupportGroupId = m.SupportGroupId,
                        CompanyId = m.CompanyId,
                        BranchId = m.BranchId,
                        CategoryId = m.CategoryId,
                        CategoryName = m.Category != null ? m.Category.Name : "",
                        SubCategoryId = m.SubCategoryId,
                        SubCategoryName = m.SubCategory != null ? m.SubCategory.Name : null,
                        IsActive = m.IsActive,
                        LevelLabel = m.SubCategoryId == null ? "Parent Category" : "Category"
                    })
                    .OrderBy(m => m.LevelLabel)
                    .ThenBy(m => m.CategoryName)
                    .ThenBy(m => m.SubCategoryName)
                    .ToList();
            }
        }

        public SupportGroupAdminResult CreateGroup(
            int companyId,
            int branchId,
            string name,
            string actorCode)
        {
            if (companyId <= 0 || branchId <= 0)
            {
                return SupportGroupAdminResult.Fail("Company and branch are required.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return SupportGroupAdminResult.Fail("Group name is required.");
            }

            using (var context = new ItsmDbContext())
            using (var tx = context.Database.BeginTransaction())
            {
                var branchOk = context.Branches.AsNoTracking().Any(b => b.Id == branchId);
                if (!branchOk)
                {
                    return SupportGroupAdminResult.Fail("Branch was not found.");
                }

                var group = new SupportGroup
                {
                    CompanyId = companyId,
                    BranchId = branchId,
                    Name = name.Trim(),
                    IsActive = false,
                    CreatedOn = DateTime.Now,
                    CreatedByCode = actorCode
                };
                context.SupportGroups.Add(group);
                context.SaveChanges();
                tx.Commit();
                return SupportGroupAdminResult.Ok(group.Id);
            }
        }

        public SupportGroupAdminResult SetGroupActive(
            int companyId,
            int branchId,
            int supportGroupId,
            bool isActive,
            string actorCode)
        {
            using (var context = new ItsmDbContext())
            using (var tx = context.Database.BeginTransaction())
            {
                var group = context.SupportGroups
                    .FirstOrDefault(g => g.Id == supportGroupId
                        && g.CompanyId == companyId
                        && g.BranchId == branchId);
                if (group == null)
                {
                    return SupportGroupAdminResult.Fail("Support group not found.");
                }

                if (isActive)
                {
                    var members = LoadMemberRows(context, supportGroupId);
                    var leaderError = SupportGroupRules.ValidateActiveGroupHasLeader(
                        members, supportGroupId, true);
                    if (leaderError != null)
                    {
                        return SupportGroupAdminResult.Fail(leaderError);
                    }
                }
                else
                {
                    var hasOpen = HasUnresolvedTickets(context, supportGroupId);
                    var deactivateError = SupportGroupRules.ValidateDeactivateGroup(hasOpen);
                    if (deactivateError != null)
                    {
                        return SupportGroupAdminResult.Fail(deactivateError);
                    }
                }

                group.IsActive = isActive;
                group.ModifiedOn = DateTime.Now;
                group.ModifiedByCode = actorCode;
                context.SaveChanges();
                tx.Commit();
                return SupportGroupAdminResult.Ok(group.Id);
            }
        }

        public SupportGroupAdminResult UpsertMember(
            int companyId,
            int branchId,
            int supportGroupId,
            string employeeCode,
            bool isTeamLeader,
            bool isActive,
            string actorCode)
        {
            if (string.IsNullOrWhiteSpace(employeeCode))
            {
                return SupportGroupAdminResult.Fail("Employee code is required.");
            }

            employeeCode = employeeCode.Trim();

            using (var context = new ItsmDbContext())
            using (var tx = context.Database.BeginTransaction())
            {
                if (!GroupOwned(context, companyId, branchId, supportGroupId))
                {
                    return SupportGroupAdminResult.Fail("Support group not found.");
                }

                var user = context.UserMasters.AsNoTracking()
                    .FirstOrDefault(u => u.EmployeeCode == employeeCode);
                if (user == null)
                {
                    return SupportGroupAdminResult.Fail("Employee was not found.");
                }

                if (user.CompanyId != companyId)
                {
                    return SupportGroupAdminResult.Fail("Employee is not in this company.");
                }

                var groupBranchLegacyId = context.Branches.AsNoTracking()
                    .Where(b => b.Id == branchId)
                    .Select(b => (int?)b.Branch_ID)
                    .FirstOrDefault();
                if (!groupBranchLegacyId.HasValue || user.BranchId != groupBranchLegacyId.Value)
                {
                    return SupportGroupAdminResult.Fail("Employee is not in this group's branch.");
                }

                if (isActive && !string.Equals(user.EmployeeStatus, "Active", StringComparison.OrdinalIgnoreCase))
                {
                    return SupportGroupAdminResult.Fail("Inactive employees cannot be active group members.");
                }

                var members = LoadMemberRows(context, supportGroupId);
                var leaderError = SupportGroupRules.ValidateOneActiveLeader(
                    members, supportGroupId, employeeCode, isTeamLeader, isActive);
                if (leaderError != null)
                {
                    return SupportGroupAdminResult.Fail(leaderError);
                }

                var existing = context.SupportGroupMembers
                    .FirstOrDefault(m => m.SupportGroupId == supportGroupId
                        && m.EmployeeCode == employeeCode);

                if (existing == null)
                {
                    existing = new SupportGroupMember
                    {
                        SupportGroupId = supportGroupId,
                        EmployeeCode = employeeCode,
                        CreatedOn = DateTime.Now
                    };
                    context.SupportGroupMembers.Add(existing);
                }

                existing.IsTeamLeader = isTeamLeader;
                existing.IsActive = isActive;
                existing.ModifiedOn = DateTime.Now;

                // If group is active, ensure a leader remains after this change.
                var group = context.SupportGroups.First(g => g.Id == supportGroupId);
                context.SaveChanges();

                var after = LoadMemberRows(context, supportGroupId);
                var activeError = SupportGroupRules.ValidateActiveGroupHasLeader(
                    after, supportGroupId, group.IsActive);
                if (activeError != null)
                {
                    tx.Rollback();
                    return SupportGroupAdminResult.Fail(activeError);
                }

                group.ModifiedOn = DateTime.Now;
                group.ModifiedByCode = actorCode;
                context.SaveChanges();
                tx.Commit();
                return SupportGroupAdminResult.Ok(existing.Id);
            }
        }

        public SupportGroupAdminResult SaveMapping(
            int companyId,
            int branchId,
            int supportGroupId,
            int categoryId,
            int? subCategoryId,
            string actorCode)
        {
            var isCategoryLevel = subCategoryId.HasValue;

            using (var context = new ItsmDbContext())
            using (var tx = context.Database.BeginTransaction())
            {
                if (!GroupOwned(context, companyId, branchId, supportGroupId))
                {
                    return SupportGroupAdminResult.Fail("Support group not found.");
                }

                if (!context.Categories.AsNoTracking().Any(c => c.Id == categoryId && c.IsActive))
                {
                    return SupportGroupAdminResult.Fail("Parent Category was not found.");
                }

                if (isCategoryLevel)
                {
                    var subOk = context.SubCategories.AsNoTracking()
                        .Any(s => s.Id == subCategoryId.Value
                            && s.CategoryId == categoryId
                            && s.IsActive);
                    if (!subOk)
                    {
                        return SupportGroupAdminResult.Fail("Category was not found under that Parent Category.");
                    }
                }

                var individuals = context.SupportMappings
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

                var conflict = SupportGroupRules.ValidateNoConflictingIndividual(
                    isCategoryLevel, categoryId, subCategoryId, individuals);
                if (conflict != null)
                {
                    return SupportGroupAdminResult.Fail(conflict);
                }

                var existingActive = context.SupportGroupMappings
                    .AsNoTracking()
                    .Where(m => m.IsActive
                        && m.CompanyId == companyId
                        && m.BranchId == branchId)
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

                // Allow re-saving the same group at the same level; reject other groups.
                var otherConflict = existingActive.Where(m => m.SupportGroupId != supportGroupId);
                var groupConflict = SupportGroupRules.ValidateNoConflictingGroup(
                    isCategoryLevel, categoryId, subCategoryId, otherConflict);
                if (groupConflict != null)
                {
                    return SupportGroupAdminResult.Fail(groupConflict);
                }

                var mapping = context.SupportGroupMappings.FirstOrDefault(m =>
                    m.CompanyId == companyId
                    && m.BranchId == branchId
                    && m.CategoryId == categoryId
                    && ((isCategoryLevel && m.SubCategoryId == subCategoryId)
                        || (!isCategoryLevel && m.SubCategoryId == null)));

                if (mapping == null)
                {
                    mapping = new SupportGroupMapping
                    {
                        CompanyId = companyId,
                        BranchId = branchId,
                        CategoryId = categoryId,
                        SubCategoryId = subCategoryId,
                        CreatedOn = DateTime.Now
                    };
                    context.SupportGroupMappings.Add(mapping);
                }

                mapping.SupportGroupId = supportGroupId;
                mapping.IsActive = true;
                mapping.ModifiedOn = DateTime.Now;

                context.SaveChanges();
                tx.Commit();
                return SupportGroupAdminResult.Ok(mapping.Id);
            }
        }

        public SupportGroupAdminResult DeactivateMapping(
            int companyId,
            int branchId,
            int mappingId,
            string actorCode)
        {
            using (var context = new ItsmDbContext())
            using (var tx = context.Database.BeginTransaction())
            {
                var mapping = context.SupportGroupMappings.FirstOrDefault(m =>
                    m.Id == mappingId
                    && m.CompanyId == companyId
                    && m.BranchId == branchId);
                if (mapping == null)
                {
                    return SupportGroupAdminResult.Fail("Mapping not found.");
                }

                mapping.IsActive = false;
                mapping.ModifiedOn = DateTime.Now;
                context.SaveChanges();
                tx.Commit();
                return SupportGroupAdminResult.Ok(mapping.Id);
            }
        }

        public List<AgentLookupDto> ListCompanyActiveAgents(int companyId, int branchId)
        {
            if (companyId <= 0 || branchId <= 0)
            {
                return new List<AgentLookupDto>();
            }

            using (var context = new ItsmDbContext())
            {
                var legacyBranchId = context.Branches.AsNoTracking()
                    .Where(b => b.Id == branchId)
                    .Select(b => (int?)b.Branch_ID)
                    .FirstOrDefault();
                if (!legacyBranchId.HasValue)
                {
                    return new List<AgentLookupDto>();
                }

                return context.UserMasters
                    .AsNoTracking()
                    .Where(u => u.CompanyId == companyId
                        && u.BranchId == legacyBranchId.Value
                        && u.EmployeeStatus == "Active")
                    .OrderBy(u => u.EmployeeName)
                    .Select(u => new AgentLookupDto
                    {
                        EmployeeCode = u.EmployeeCode,
                        EmployeeName = u.EmployeeName
                    })
                    .ToList();
            }
        }

        private static bool GroupOwned(
            ItsmDbContext context,
            int companyId,
            int branchId,
            int supportGroupId)
        {
            return context.SupportGroups.AsNoTracking()
                .Any(g => g.Id == supportGroupId
                    && g.CompanyId == companyId
                    && g.BranchId == branchId);
        }

        private static List<GroupMemberRow> LoadMemberRows(ItsmDbContext context, int supportGroupId)
        {
            return context.SupportGroupMembers
                .AsNoTracking()
                .Where(m => m.SupportGroupId == supportGroupId)
                .Select(m => new GroupMemberRow
                {
                    SupportGroupId = m.SupportGroupId,
                    EmployeeCode = m.EmployeeCode,
                    IsTeamLeader = m.IsTeamLeader,
                    IsActive = m.IsActive,
                    EmployeeStatus = m.Employee != null ? m.Employee.EmployeeStatus : null,
                    EmployeeCompanyId = m.Employee != null ? m.Employee.CompanyId : (int?)null
                })
                .ToList();
        }

        private static bool HasUnresolvedTickets(ItsmDbContext context, int supportGroupId)
        {
            return context.Tickets.AsNoTracking()
                .Any(t => t.SupportGroupId == supportGroupId
                    && t.Status != null
                    && !t.Status.IsTerminal);
        }
    }
}
