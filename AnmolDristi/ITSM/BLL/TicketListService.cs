// ========================================================================
// File:    TicketListService.cs
// Layer:   BLL — read-only queues
// When:    2026-08-21
// What:    Dropped CompanyId from GetAgentQueue / GetOpenQueue LINQ filters.
//          Queries stay AsNoTracking + Select into TicketListItemDto (no DataTable).
// Why:     Shared-service agents work tickets across companies. Tenant isolation
//          is enforced on mutations only (TicketUpdateService / TicketDetailService).
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
    }

    public class TicketListFilter
    {
        public int? StatusId { get; set; }
        public int? RequestTypeId { get; set; }
        public int? BranchId { get; set; }
        public string AssigneeCode { get; set; }
        public string SearchText { get; set; }
        public int? PriorityId { get; set; }
        public bool OpenOnly { get; set; }
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
    /// Agent queues are tenant-agnostic (shared services). CompanyId is applied
    /// only on requester/approval reads that are not shared dispatch surfaces.
    /// </summary>
    public class TicketListService
    {
        public List<TicketListItemDto> GetAgentQueue(string assigneeCode, TicketListFilter filter)
        {
            if (filter == null)
            {
                filter = new TicketListFilter();
            }

            filter.AssigneeCode = assigneeCode;
            filter.OpenOnly = true;

            using (var context = new ItsmDbContext())
            {
                return SortQueue(BuildQueueQuery(context, null, filter)).ToList();
            }
        }

        public List<TicketListItemDto> GetOpenQueue(TicketListFilter filter)
        {
            if (filter == null)
            {
                filter = new TicketListFilter();
            }

            filter.OpenOnly = true;

            using (var context = new ItsmDbContext())
            {
                return SortQueue(BuildQueueQuery(context, null, filter)).ToList();
            }
        }

        public List<TicketListItemDto> GetMyTickets(
            int companyId,
            string requesterCode,
            TicketListFilter filter)
        {
            using (var context = new ItsmDbContext())
            {
                return SortQueue(
                    BuildQueueQuery(context, companyId, filter)
                        .Where(t => t.RequesterCode == requesterCode))
                    .ToList();
            }
        }

        public List<TicketListItemDto> GetApprovalQueue(int companyId, string approverCode)
        {
            using (var context = new ItsmDbContext())
            {
                return context.TicketApprovals
                    .AsNoTracking()
                    .Where(a => a.ApproverCode == approverCode
                                && a.ApprovalStatus == "Pending")
                    .Join(
                        BuildQueueQuery(context, companyId, null),
                        approval => approval.TicketId,
                        ticket => ticket.Id,
                        (approval, ticket) => ticket)
                    .Distinct()
                    .OrderBy(t => t.PrioritySortOrder)
                    .ThenByDescending(t => t.CreatedOn)
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
                    .Select(p => new LookupItemDto { Id = p.Id, Name = p.Name })
                    .ToList();
            }
        }

        public List<LookupItemDto> GetBranches(int companyId)
        {
            using (var context = new ItsmDbContext())
            {
                return context.UserMasters
                    .AsNoTracking()
                    .Where(u => u.CompanyId == companyId && u.Branch != null)
                    .Select(u => new LookupItemDto
                    {
                        Id = u.Branch.Id,
                        Name = u.Branch.Branch_Description
                    })
                    .Distinct()
                    .OrderBy(b => b.Name)
                    .ToList();
            }
        }

        public List<AgentLookupDto> GetActiveAgents(int companyId)
        {
            using (var context = new ItsmDbContext())
            {
                return context.UserMasters
                    .AsNoTracking()
                    .Where(u => u.EmployeeStatus == "Active"
                                && u.CompanyId == companyId)
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
            int? companyId,
            TicketListFilter filter)
        {
            IQueryable<Ticket> query = context.Tickets.AsNoTracking();

            if (companyId.HasValue)
            {
                query = query.Where(t => t.Requester.CompanyId == companyId.Value);
            }

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

                if (!string.IsNullOrWhiteSpace(filter.AssigneeCode))
                {
                    query = query.Where(t => t.AssigneeCode == filter.AssigneeCode);
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
                AssigneeCode = t.AssigneeCode
            });
        }

        private static IQueryable<TicketListItemDto> SortQueue(IQueryable<TicketListItemDto> query)
        {
            return query
                .OrderBy(t => t.PrioritySortOrder)
                .ThenByDescending(t => t.CreatedOn);
        }
    }
}
