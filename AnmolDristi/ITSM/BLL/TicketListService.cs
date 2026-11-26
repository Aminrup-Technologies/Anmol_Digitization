// ========================================================================
// When: 2026-11-25
// What: Apply the approved R4 queue order consistently and add a stable
//       ticket-ID tie-breaker after priority and creation timestamp.
// Why: Equal-priority/equal-time tickets must not jump between pages or
//      queue views. Sort in the query before materializing the result.
// ========================================================================
// When: 2026-08-21
// What: Queue reads stay AsNoTracking projections and require the actor's
//       MST_UserMaster.CompanyId. Open is company-scoped. A missing company
//       returns no rows.
// Why: Ticket.CompanyId is the tenant. D-1 does not authorize a shared
//      cross-company Open queue.
// ========================================================================

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using AnmolDristi.ITSM.DAL;
using AnmolDristi.ITSM.DAL.Entities;

namespace AnmolDristi.ITSM.BLL
{
    public class TicketListItemDto
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; }
        public string Title { get; set; }
        public string RequestTypeName { get; set; }
        public string StatusName { get; set; }
        public string PriorityName { get; set; }
        public string RequesterName { get; set; }
        public string AssigneeName { get; set; }
        public string CategoryName { get; set; }
        public string BranchName { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? ResolvedOn { get; set; }
        public int PrioritySortOrder { get; set; }
        public string RequesterCode { get; set; }
        public string AssigneeCode { get; set; }
        public int BranchId { get; set; }
        public int CategoryId { get; set; }
        public int SubCategoryId { get; set; }
        public int? ItemId { get; set; }
        public int? SupportGroupId { get; set; }
        public int? CompanyId { get; set; }
    }

    public class TicketListFilter
    {
        public int? StatusId { get; set; }
        public int? RequestTypeId { get; set; }
        public int? BranchId { get; set; }
        public List<int> BranchIds { get; set; }
        public string AssigneeCode { get; set; }
        public string SearchText { get; set; }
        public int? PriorityId { get; set; }
        public bool OpenOnly { get; set; }
        public bool UnassignedOnly { get; set; }
    }

    public class LookupItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class AgentLookupDto
    {
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
    }

    /// <summary>
    /// Every queue is limited to Ticket.CompanyId for the actor's current
    /// MST_UserMaster.CompanyId. A missing company returns no rows.
    /// </summary>
    public class TicketListService
    {
        public List<TicketListItemDto> GetAgentQueue(string assigneeCode, TicketListFilter filter)
        {
            var companyId = ItsmTenantContext.ResolveCompanyId(assigneeCode);
            if (companyId <= 0)
            {
                return new List<TicketListItemDto>();
            }

            if (filter == null)
            {
                filter = new TicketListFilter();
            }

            filter.AssigneeCode = assigneeCode;
            filter.OpenOnly = true;

            using (var context = new ItsmDbContext())
            {
                return SortQueue(BuildQueueQuery(context, filter, companyId)).ToList();
            }
        }

        public List<TicketListItemDto> GetOpenQueue(string actorCode, TicketListFilter filter)
        {
            var companyId = ItsmTenantContext.ResolveCompanyId(actorCode);
            if (companyId <= 0)
            {
                return new List<TicketListItemDto>();
            }

            if (filter == null)
            {
                filter = new TicketListFilter();
            }

            filter.OpenOnly = true;

            using (var context = new ItsmDbContext())
            {
                return SortQueue(BuildQueueQuery(context, filter, companyId)).ToList();
            }
        }

        public List<TicketListItemDto> GetMyTickets(string requesterCode, TicketListFilter filter)
        {
            var companyId = ItsmTenantContext.ResolveCompanyId(requesterCode);
            if (companyId <= 0)
            {
                return new List<TicketListItemDto>();
            }

            using (var context = new ItsmDbContext())
            {
                return SortQueue(
                    BuildQueueQuery(context, filter, companyId)
                        .Where(t => t.RequesterCode == requesterCode))
                    .ToList();
            }
        }

        public List<TicketListItemDto> GetApprovalQueue(string approverCode)
        {
            var companyId = ItsmTenantContext.ResolveCompanyId(approverCode);
            if (companyId <= 0)
            {
                return new List<TicketListItemDto>();
            }

            using (var context = new ItsmDbContext())
            {
                var query = context.TicketApprovals
                    .AsNoTracking()
                    .Where(a => a.ApproverCode == approverCode
                                && a.ApprovalStatus == "Pending")
                    .Join(
                        BuildQueueQuery(context, null, companyId),
                        approval => approval.TicketId,
                        ticket => ticket.Id,
                        (approval, ticket) => ticket)
                    .Distinct();
                return SortQueue(query).ToList();
            }
        }

        /// <summary>
        /// Unassigned open tickets whose dispatch cascade names this employee.
        /// The lowest active mapping Id wins at each step. Other agents on a
        /// duplicate row are not given the ticket.
        /// </summary>
        public List<TicketListItemDto> GetMappedUnassignedQueue(string actorCode, TicketListFilter filter)
        {
            if (string.IsNullOrWhiteSpace(actorCode))
            {
                return new List<TicketListItemDto>();
            }

            var companyId = ItsmTenantContext.ResolveCompanyId(actorCode);
            if (companyId <= 0)
            {
                return new List<TicketListItemDto>();
            }

            if (filter == null)
            {
                filter = new TicketListFilter();
            }

            filter.OpenOnly = true;
            filter.UnassignedOnly = true;
            filter.AssigneeCode = null;

            using (var context = new ItsmDbContext())
            {
                var branchIds = context.SupportMappings
                    .AsNoTracking()
                    .Where(m => m.IsActive && m.SupportAgentCode == actorCode)
                    .Select(m => m.BranchId)
                    .Distinct()
                    .ToList();
                if (branchIds.Count == 0)
                {
                    return new List<TicketListItemDto>();
                }

                filter.BranchIds = branchIds;

                var mappings = context.SupportMappings
                    .AsNoTracking()
                    .Where(m => m.IsActive && branchIds.Contains(m.BranchId))
                    .ToList();

                var candidates = BuildQueueQuery(context, filter, companyId).ToList();

                return candidates
                    .Where(row =>
                    {
                        var resolved = DispatchService.ResolveFromMappings(
                            mappings.Where(m => m.BranchId == row.BranchId),
                            row.CategoryId,
                            row.SubCategoryId,
                            row.ItemId);
                        return !string.IsNullOrWhiteSpace(resolved)
                            && string.Equals(resolved.Trim(), actorCode.Trim(), StringComparison.OrdinalIgnoreCase);
                    })
                    .OrderBy(t => t.PrioritySortOrder)
                    .ThenByDescending(t => t.CreatedOn)
                    .ThenByDescending(t => t.Id)
                    .ToList();
            }
        }

        /// <summary>
        /// R6 unclaimed group queue: open tickets with blank assignee whose
        /// SupportGroupId lists the actor as an active member in the ticket company.
        /// </summary>
        public List<TicketListItemDto> GetGroupUnassignedQueue(string actorCode, TicketListFilter filter)
        {
            if (string.IsNullOrWhiteSpace(actorCode))
            {
                return new List<TicketListItemDto>();
            }

            var companyId = ItsmTenantContext.ResolveCompanyId(actorCode);
            if (companyId <= 0)
            {
                return new List<TicketListItemDto>();
            }

            if (filter == null)
            {
                filter = new TicketListFilter();
            }

            filter.OpenOnly = true;

            using (var context = new ItsmDbContext())
            {
                // Keep membership + ticket filters on IQueryable so EF6 emits
                // EXISTS / join SQL before materialization (AsNoTracking + Select).
                var membershipQuery = context.SupportGroupMembers
                    .AsNoTracking()
                    .Where(m => m.IsActive
                        && m.EmployeeCode == actorCode
                        && m.Employee != null
                        && m.Employee.EmployeeStatus == "Active"
                        && m.Employee.CompanyId != null
                        && m.SupportGroup != null
                        && m.SupportGroup.IsActive
                        && m.SupportGroup.CompanyId == m.Employee.CompanyId.Value)
                    .Select(m => new
                    {
                        m.SupportGroupId,
                        CompanyId = m.Employee.CompanyId.Value
                    });

                var queue = BuildQueueQuery(context, filter, companyId)
                    .Where(t => t.SupportGroupId != null
                        && (t.AssigneeCode == null || t.AssigneeCode == "")
                        && t.CompanyId != null
                        && membershipQuery.Any(m =>
                            m.SupportGroupId == t.SupportGroupId.Value
                            && m.CompanyId == t.CompanyId.Value));

                return SortQueue(queue).ToList();
            }
        }

        /// <summary>
        /// Active members of the ticket's support group (same company).
        /// </summary>
        public List<AgentLookupDto> GetGroupActiveMembers(int supportGroupId, int ticketCompanyId)
        {
            if (supportGroupId <= 0 || ticketCompanyId <= 0)
            {
                return new List<AgentLookupDto>();
            }

            using (var context = new ItsmDbContext())
            {
                return context.SupportGroupMembers
                    .AsNoTracking()
                    .Where(m => m.SupportGroupId == supportGroupId
                        && m.IsActive
                        && m.Employee != null
                        && m.Employee.EmployeeStatus == "Active"
                        && m.Employee.CompanyId == ticketCompanyId
                        && m.SupportGroup != null
                        && m.SupportGroup.IsActive
                        && m.SupportGroup.CompanyId == ticketCompanyId)
                    .OrderBy(m => m.Employee.EmployeeName)
                    .Select(m => new AgentLookupDto
                    {
                        EmployeeCode = m.EmployeeCode,
                        EmployeeName = m.Employee.EmployeeName
                    })
                    .ToList();
            }
        }

        public List<LookupItemDto> GetActiveStatuses()
        {
            using (var context = new ItsmDbContext())
            {
                return context.TicketStatuses
                    .AsNoTracking()
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.Id)
                    .Select(s => new LookupItemDto { Id = s.Id, Name = s.Name })
                    .ToList();
            }
        }

        public List<LookupItemDto> GetActivePriorities()
        {
            using (var context = new ItsmDbContext())
            {
                return context.Priorities
                    .AsNoTracking()
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Id)
                    .Select(s => new LookupItemDto { Id = s.Id, Name = s.Name })
                    .ToList();
            }
        }

        public List<LookupItemDto> GetBranches()
        {
            using (var context = new ItsmDbContext())
            {
                return context.Branches
                    .AsNoTracking()
                    .Select(b => new LookupItemDto
                    {
                        Id = b.Id,
                        Name = b.Branch_Description
                    })
                    .OrderBy(b => b.Name)
                    .ToList();
            }
        }

        public List<AgentLookupDto> GetActiveAgents()
        {
            using (var context = new ItsmDbContext())
            {
                return context.UserMasters
                    .AsNoTracking()
                    .Where(u => u.EmployeeStatus == "Active")
                    .OrderBy(u => u.EmployeeName)
                    .Select(u => new AgentLookupDto
                    {
                        EmployeeCode = u.EmployeeCode,
                        EmployeeName = u.EmployeeName
                    })
                    .ToList();
            }
        }

        private static IQueryable<TicketListItemDto> BuildQueueQuery(
            ItsmDbContext context,
            TicketListFilter filter,
            int actorCompanyId)
        {
            IQueryable<Ticket> query = context.Tickets.AsNoTracking()
                .Where(t => t.CompanyId == actorCompanyId);

            if (filter != null)
            {
                if (filter.OpenOnly)
                {
                    query = query.Where(t => !t.Status.IsTerminal);
                }

                if (filter.StatusId.HasValue)
                {
                    query = query.Where(t => t.StatusId == filter.StatusId.Value);
                }

                if (filter.RequestTypeId.HasValue)
                {
                    query = query.Where(t => t.RequestTypeId == filter.RequestTypeId.Value);
                }

                if (filter.BranchId.HasValue)
                {
                    query = query.Where(t => t.BranchId == filter.BranchId.Value);
                }

                if (filter.BranchIds != null && filter.BranchIds.Count > 0)
                {
                    var limitedBranches = filter.BranchIds;
                    query = query.Where(t => limitedBranches.Contains(t.BranchId));
                }

                if (!string.IsNullOrWhiteSpace(filter.AssigneeCode))
                {
                    query = query.Where(t => t.AssigneeCode == filter.AssigneeCode);
                }

                if (filter.UnassignedOnly)
                {
                    query = query.Where(t => t.AssigneeCode == null || t.AssigneeCode == "");
                }

                if (filter.PriorityId.HasValue)
                {
                    query = query.Where(t => t.PriorityId == filter.PriorityId.Value);
                }

                if (!string.IsNullOrWhiteSpace(filter.SearchText))
                {
                    var search = filter.SearchText.Trim().ToLower();
                    query = query.Where(t =>
                        t.Title.ToLower().Contains(search)
                        || t.TicketNumber.ToLower().Contains(search)
                        || (t.Description != null && t.Description.ToLower().Contains(search)));
                }
            }

            return query.Select(t => new TicketListItemDto
            {
                Id = t.Id,
                TicketNumber = t.TicketNumber,
                Title = t.Title,
                RequestTypeName = t.RequestType.Code + " - " + t.RequestType.Name,
                StatusName = t.Status.Name,
                PriorityName = t.Priority.Name,
                RequesterName = t.Requester.EmployeeName,
                AssigneeName = t.Assignee != null ? t.Assignee.EmployeeName : "Unassigned",
                CategoryName = t.Category.Name,
                BranchName = t.Branch.Branch_Description,
                CreatedOn = t.CreatedOn,
                ResolvedOn = t.ResolvedOn,
                PrioritySortOrder = t.Priority.SlaResolutionHours,
                RequesterCode = t.RequesterCode,
                AssigneeCode = t.AssigneeCode,
                BranchId = t.BranchId,
                CategoryId = t.CategoryId,
                SubCategoryId = t.SubCategoryId,
                ItemId = t.ItemId,
                SupportGroupId = t.SupportGroupId,
                CompanyId = t.CompanyId
            });
        }

        private static IQueryable<TicketListItemDto> SortQueue(IQueryable<TicketListItemDto> query)
        {
            return query
                .OrderBy(t => t.PrioritySortOrder)
                .ThenByDescending(t => t.CreatedOn)
                .ThenByDescending(t => t.Id);
        }
    }
}
