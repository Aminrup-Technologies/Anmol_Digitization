// ========================================================================
// When: 2026-08-21
// What: Removed actor/requester CompanyId from MyTickets, Approvals,
//       GetBranches, GetActiveAgents. Queues stay AsNoTracking + Select.
// Why: Shared-service queues and assignment must not be scoped by the
//      agent's current CompanyId. Ticket.CompanyId is tenant ownership.
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
    /// Agent queues are tenant-agnostic (shared services). Ticket-level
    /// authorization is enforced on detail and mutation, not on queue listing.
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
                return SortQueue(BuildQueueQuery(context, filter)).ToList();
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
                return SortQueue(BuildQueueQuery(context, filter)).ToList();
            }
        }

        public List<TicketListItemDto> GetMyTickets(string requesterCode, TicketListFilter filter)
        {
            using (var context = new ItsmDbContext())
            {
                return SortQueue(
                    BuildQueueQuery(context, filter)
                        .Where(t => t.RequesterCode == requesterCode))
                    .ToList();
            }
        }

        public List<TicketListItemDto> GetApprovalQueue(string approverCode)
        {
            using (var context = new ItsmDbContext())
            {
                return context.TicketApprovals
                    .AsNoTracking()
                    .Where(a => a.ApproverCode == approverCode
                                && a.ApprovalStatus == "Pending")
                    .Join(
                        BuildQueueQuery(context, null),
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
            TicketListFilter filter)
        {
            IQueryable<Ticket> query = context.Tickets.AsNoTracking();

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
