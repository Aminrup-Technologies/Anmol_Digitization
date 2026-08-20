// ========================================================================
// File:    TicketDetailService.cs
// Layer:   BLL — Service
// When:    2026-08-20
// What:    Read-only service for full ticket detail retrieval.
// Why:     Loads a single ticket with all associated comments,
//          approvals, and audit history for the TicketDetail.aspx
//          processing surface (requirement §15).
// ========================================================================

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using AnmolDristi.ITSM.DAL;
using AnmolDristi.ITSM.DAL.Entities;

namespace AnmolDristi.ITSM.BLL
{
    /// <summary>
    /// Full ticket detail DTO for the processing surface.
    /// Contains ticket metadata plus child collections.
    /// </summary>
    public class TicketDetailDto
    {
        // --- Core ticket fields ---
        public int Id { get; set; }
        public string TicketNumber { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        // --- Requester / Assignment ---
        public string RequesterCode { get; set; }
        public string RequesterName { get; set; }
        public string AssigneeCode { get; set; }
        public string AssigneeName { get; set; }

        // --- Taxonomy ---
        public string RequestTypeName { get; set; }
        public string CategoryName { get; set; }
        public string SubCategoryName { get; set; }
        public string ItemName { get; set; }
        public string BranchName { get; set; }

        // --- Priority / Impact / Urgency ---
        public string ImpactName { get; set; }
        public string UrgencyName { get; set; }
        public string PriorityName { get; set; }
        public int PriorityId { get; set; }
        public int StatusId { get; set; }
        public string StatusName { get; set; }

        // --- Resolution / CSAT ---
        public string ResolutionText { get; set; }
        public byte? CsatScore { get; set; }
        public string CsatFeedback { get; set; }

        // --- Timestamps ---
        public DateTime CreatedOn { get; set; }
        public DateTime? ResolvedOn { get; set; }
        public DateTime? ClosedOn { get; set; }
        public string CreatedByCode { get; set; }

        // --- Concurrency ---
        public byte[] RowVersion { get; set; }

        // --- Child collections ---
        public List<TicketCommentDto> Comments { get; set; }
        public List<TicketApprovalDto> Approvals { get; set; }
        public List<TicketAuditLogDto> AuditHistory { get; set; }
        public List<TicketStatusDto> AvailableStatuses { get; set; }
    }

    public class TicketCommentDto
    {
        public int Id { get; set; }
        public string CommentText { get; set; }
        public string CreatedByName { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool IsInternal { get; set; }
    }

    public class TicketApprovalDto
    {
        public int Id { get; set; }
        public string ApprovalLevel { get; set; }
        public string ApproverName { get; set; }
        public string ApprovalStatus { get; set; }
        public string Comments { get; set; }
        public DateTime? ActionedOn { get; set; }
    }

    public class TicketAuditLogDto
    {
        public string ActionType { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string Detail { get; set; }
        public string ActorName { get; set; }
        public DateTime ActionedOn { get; set; }
    }

    public class TicketStatusDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// Read-only service for loading full ticket detail.
    /// Used by TicketDetail.aspx code-behind — no business rules here.
    /// </summary>
    public class TicketDetailService
    {
        /// <summary>
        /// Loads the full ticket with comments, approvals, audit history,
        /// and available status transitions. Returns null if ticket not found
        /// or outside tenant scope.
        /// </summary>
        public TicketDetailDto GetTicketDetail(int companyId, int ticketId)
        {
            using (var context = new ItsmDbContext())
            {
                var ticket = context.Tickets
                    .AsNoTracking()
                    .FirstOrDefault(t => t.Id == ticketId
                                        && t.Branch.CompanyId == companyId);

                if (ticket == null)
                {
                    return null;
                }

                var dto = MapToDetailDto(ticket);

                // Load child collections
                dto.Comments = context.TicketComments
                    .AsNoTracking()
                    .Where(c => c.TicketId == ticketId)
                    .OrderByDescending(c => c.CreatedOn)
                    .Select(c => new TicketCommentDto
                    {
                        Id = c.Id,
                        CommentText = c.CommentText,
                        CreatedByName = c.Creator.EmployeeName,
                        CreatedOn = c.CreatedOn,
                        IsInternal = c.IsInternal
                    })
                    .ToList();

                dto.Approvals = context.TicketApprovals
                    .AsNoTracking()
                    .Where(a => a.TicketId == ticketId)
                    .OrderBy(a => a.CreatedOn)
                    .Select(a => new TicketApprovalDto
                    {
                        Id = a.Id,
                        ApprovalLevel = a.ApprovalLevel,
                        ApproverName = a.Approver.EmployeeName,
                        ApprovalStatus = a.ApprovalStatus,
                        Comments = a.Comments,
                        ActionedOn = a.ActionedOn
                    })
                    .ToList();

                dto.AuditHistory = context.TicketAuditLogs
                    .AsNoTracking()
                    .Where(a => a.TicketId == ticketId)
                    .OrderByDescending(a => a.ActionedOn)
                    .Select(a => new TicketAuditLogDto
                    {
                        ActionType = a.ActionType,
                        OldValue = a.OldValue,
                        NewValue = a.NewValue,
                        Detail = a.Detail,
                        ActorName = a.ActorCode,
                        ActionedOn = a.ActionedOn
                    })
                    .ToList();

                dto.AvailableStatuses = GetAvailableStatuses(context, ticket.StatusId);

                return dto;
            }
        }

        private static TicketDetailDto MapToDetailDto(Ticket ticket)
        {
            return new TicketDetailDto
            {
                Id = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                Title = ticket.Title,
                Description = ticket.Description,
                RequesterCode = ticket.RequesterCode,
                RequesterName = ticket.Requester != null
                    ? ticket.Requester.EmployeeName
                    : ticket.RequesterCode,
                AssigneeCode = ticket.AssigneeCode,
                AssigneeName = ticket.Assignee != null
                    ? ticket.Assignee.EmployeeName
                    : "Unassigned",
                RequestTypeName = ticket.RequestType != null
                    ? ticket.RequestType.Code + " - " + ticket.RequestType.Name
                    : "",
                CategoryName = ticket.Category != null ? ticket.Category.Name : "",
                SubCategoryName = ticket.SubCategory != null ? ticket.SubCategory.Name : "",
                ItemName = ticket.Item != null ? ticket.Item.Name : "",
                BranchName = ticket.Branch != null ? ticket.Branch.Branch_Description : "",
                ImpactName = ticket.Impact != null ? ticket.Impact.Name : "",
                UrgencyName = ticket.Urgency != null ? ticket.Urgency.Name : "",
                PriorityName = ticket.Priority != null ? ticket.Priority.Name : "",
                PriorityId = ticket.PriorityId,
                StatusId = ticket.StatusId,
                StatusName = ticket.Status != null ? ticket.Status.Name : "",
                ResolutionText = ticket.ResolutionText,
                CsatScore = ticket.CsatScore,
                CsatFeedback = ticket.CsatFeedback,
                CreatedOn = ticket.CreatedOn,
                ResolvedOn = ticket.ResolvedOn,
                ClosedOn = ticket.ClosedOn,
                CreatedByCode = ticket.CreatedByCode,
                RowVersion = ticket.RowVersion
            };
        }

        /// <summary>
        /// Returns valid next statuses from the current status.
        /// Phase 1B lifecycle: In Queue → In Progress → Resolved → Closed.
        /// </summary>
        private static List<TicketStatusDto> GetAvailableStatuses(
            ItsmDbContext context, int currentStatusId)
        {
            var currentStatus = context.TicketStatuses
                .AsNoTracking()
                .FirstOrDefault(s => s.Id == currentStatusId && s.IsActive);

            if (currentStatus == null || currentStatus.IsTerminal)
            {
                return new List<TicketStatusDto>();
            }

            // Map current status name to valid next statuses
            var validNextNames = new List<string>();

            switch (currentStatus.Name)
            {
                case "In Queue":
                case "New":
                    validNextNames.Add("In Progress");
                    validNextNames.Add("Cancelled");
                    break;
                case "In Progress":
                    validNextNames.Add("Resolved");
                    validNextNames.Add("In Queue");
                    validNextNames.Add("On Hold");
                    break;
                case "On Hold":
                    validNextNames.Add("In Progress");
                    validNextNames.Add("Cancelled");
                    break;
                case "Resolved":
                    validNextNames.Add("Closed");
                    validNextNames.Add("In Progress");
                    break;
                case "Pending Approval":
                    validNextNames.Add("In Progress");
                    validNextNames.Add("Cancelled");
                    break;
                default:
                    break;
            }

            if (validNextNames.Count == 0)
            {
                return new List<TicketStatusDto>();
            }

            return context.TicketStatuses
                .AsNoTracking()
                .Where(s => s.IsActive && validNextNames.Contains(s.Name))
                .Select(s => new TicketStatusDto
                {
                    Id = s.Id,
                    Name = s.Name
                })
                .ToList();
        }
    }
}
