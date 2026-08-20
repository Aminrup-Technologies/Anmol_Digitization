// ========================================================================
// File:    TicketListService.cs
// Layer:   BLL — Service
// When:    2026-08-20
// What:    Read-only service for agent ticket queue population.
// Why:     Provides flattened DTOs via LINQ Select() projections with
//          AsNoTracking() — no full entity retrieval, no DataTable,
//          no raw SQL. All queries are tenant-scoped by CompanyId
//          (requirement §6, §7, §14).
// ========================================================================

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using AnmolDristi.ITSM.DAL;

namespace AnmolDristi.ITSM.BLL
{
    /// <summary>
    /// Lightweight DTO for ticket queue GridViews — contains only fields
    /// required by the queue display. No navigation properties, no
    /// full entities, no DataTables (requirement §6, §18).
    /// </summary>
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
    }

    /// <summary>
    /// Filter criteria for queue queries — server-side filtering only,
    /// no in-memory population (requirement §14).
    /// </summary>
    public class TicketListFilter
    {
        public int? StatusId { get; set; }
        public int? RequestTypeId { get; set; }
        public int? BranchId { get; set; }
        public string AssigneeCode { get; set; }
        public string SearchText { get; set; }
        public int? PriorityId { get; set; }
    }

    /// <summary>
    /// Read-only service for populating agent ticket queues.
    /// All queries use AsNoTracking() and LINQ Select() DTO projection.
    /// CompanyId tenant isolation is applied on every query (requirement §7).
    /// </summary>
    public class TicketListService
    {
        /// <summary>
        /// Retrieves tickets assigned to a specific agent within the tenant scope.
        /// GridView binds directly to the returned DTO list.
        /// </summary>
        public List<TicketListItemDto> GetAgentQueue(
            int companyId,
            string assigneeCode,
            TicketListFilter filter)
        {
            using (var context = new ItsmDbContext())
            {
                return BuildQueueQuery(context, companyId, filter)
                    .Where(t => t.AssigneeCode == assigneeCode)
                    .ToList();
            }
        }

        /// <summary>
        /// Retrieves all open tickets within the tenant scope.
        /// Used for supervisor/manager queue views.
        /// </summary>
        public List<TicketListItemDto> GetOpenQueue(
            int companyId,
            TicketListFilter filter)
        {
            using (var context = new ItsmDbContext())
            {
                return BuildQueueQuery(context, companyId, filter)
                    .ToList();
            }
        }

        /// <summary>
        /// Retrieves tickets created by a specific user within the tenant scope.
        /// Used for the "My Tickets" view.
        /// </summary>
        public List<TicketListItemDto> GetMyTickets(
            int companyId,
            string requesterCode,
            TicketListFilter filter)
        {
            using (var context = new ItsmDbContext())
            {
                return BuildQueueQuery(context, companyId, filter)
                    .Where(t => t.RequesterCode == requesterCode)
                    .ToList();
            }
        }

        /// <summary>
        /// Retrieves pending approval tickets for a specific approver within tenant scope.
        /// </summary>
        public List<TicketListItemDto> GetApprovalQueue(
            int companyId,
            string approverCode)
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

        /// <summary>
        /// Core tenant-scoped query builder. All queue methods funnel through here.
        /// Applies AsNoTracking(), Select() DTO projection, and CompanyId filter.
        /// </summary>
        private static IQueryable<TicketListItemDto> BuildQueueQuery(
            ItsmDbContext context,
            int companyId,
            TicketListFilter filter)
        {
            // Tenant isolation: resolve CompanyId from the user's BranchId mapping.
            // CompanyId is derived from the caller's authenticated context, never from
            // UI input (requirement §7).
            var query = context.Tickets
                .AsNoTracking()
                .Where(t => t.Branch.CompanyId == companyId);

            // --- Server-side filters (requirement §14) ---
            if (filter != null)
            {
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
                        || t.Description.ToLower().Contains(search));
                }
            }

            // DTO projection — only fields required by the queue display.
            // No full entities, no navigation-property loading (requirement §6).
            return query
                .Select(t => new TicketListItemDto
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
                    PrioritySortOrder = t.Priority.Id
                })
                .OrderBy(t => t.PrioritySortOrder)
                .ThenByDescending(t => t.CreatedOn);
        }
    }
}
