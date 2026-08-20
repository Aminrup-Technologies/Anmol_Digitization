using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using AnmolDristi.ITSM.DAL;
using AnmolDristi.ITSM.DAL.Entities;

namespace AnmolDristi.ITSM.BLL
{
    public class TicketDetailDto
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string RequesterCode { get; set; }
        public string RequesterName { get; set; }
        public string AssigneeCode { get; set; }
        public string AssigneeName { get; set; }
        public string RequestTypeName { get; set; }
        public string CategoryName { get; set; }
        public string SubCategoryName { get; set; }
        public string ItemName { get; set; }
        public string BranchName { get; set; }
        public string ImpactName { get; set; }
        public string UrgencyName { get; set; }
        public string PriorityName { get; set; }
        public int PriorityId { get; set; }
        public int StatusId { get; set; }
        public string StatusName { get; set; }
        public bool IsTerminal { get; set; }
        public string ResolutionText { get; set; }
        public byte? CsatScore { get; set; }
        public string CsatFeedback { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? ResolvedOn { get; set; }
        public DateTime? ClosedOn { get; set; }
        public string CreatedByCode { get; set; }
        public int CompanyId { get; set; }
        public bool CanApprove { get; set; }
        public bool CanAddInternalNote { get; set; }
        public byte[] RowVersion { get; set; }
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

    public class TicketDetailService
    {
        public TicketDetailDto GetTicketDetail(int companyId, int ticketId, string viewerCode)
        {
            using (var context = new ItsmDbContext())
            {
                var ticket = context.Tickets
                    .AsNoTracking()
                    .Include(t => t.Requester)
                    .Include(t => t.Assignee)
                    .Include(t => t.RequestType)
                    .Include(t => t.Category)
                    .Include(t => t.SubCategory)
                    .Include(t => t.Item)
                    .Include(t => t.Branch)
                    .Include(t => t.Impact)
                    .Include(t => t.Urgency)
                    .Include(t => t.Priority)
                    .Include(t => t.Status)
                    .FirstOrDefault(t => t.Id == ticketId
                                        && t.Requester.CompanyId == companyId);

                if (ticket == null)
                {
                    return null;
                }

                var dto = MapToDetailDto(ticket);
                dto.CanApprove = context.TicketApprovals
                    .AsNoTracking()
                    .Any(a => a.TicketId == ticketId
                              && a.ApproverCode == viewerCode
                              && a.ApprovalStatus == "Pending");
                dto.CanAddInternalNote =
                    !string.Equals(ticket.RequesterCode, viewerCode, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(ticket.AssigneeCode, viewerCode, StringComparison.OrdinalIgnoreCase);

                var commentsQuery = context.TicketComments
                    .AsNoTracking()
                    .Where(c => c.TicketId == ticketId);

                if (!dto.CanAddInternalNote)
                {
                    commentsQuery = commentsQuery.Where(c => !c.IsInternal);
                }

                dto.Comments = commentsQuery
                    .OrderByDescending(c => c.CreatedOn)
                    .Select(c => new TicketCommentDto
                    {
                        Id = c.Id,
                        CommentText = c.CommentText,
                        CreatedByName = c.CreatedBy != null
                            ? c.CreatedBy.EmployeeName
                            : c.CreatedByCode,
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
                        ApproverName = a.Approver != null
                            ? a.Approver.EmployeeName
                            : a.ApproverCode,
                        ApprovalStatus = a.ApprovalStatus,
                        Comments = a.Comments,
                        ActionedOn = a.ActionedOn
                    })
                    .ToList();

                var auditRows = context.TicketAuditLogs
                    .AsNoTracking()
                    .Where(a => a.TicketId == ticketId && a.CompanyId == companyId)
                    .OrderByDescending(a => a.ActionedOn)
                    .ToList();

                var actorCodes = auditRows
                    .Select(a => a.ActorCode)
                    .Distinct()
                    .ToList();

                var actorNames = context.UserMasters
                    .AsNoTracking()
                    .Where(u => actorCodes.Contains(u.EmployeeCode))
                    .ToList()
                    .ToDictionary(u => u.EmployeeCode, u => u.EmployeeName);

                dto.AuditHistory = auditRows.Select(a =>
                {
                    string actorName;
                    if (!actorNames.TryGetValue(a.ActorCode, out actorName))
                    {
                        actorName = a.ActorCode;
                    }

                    return new TicketAuditLogDto
                    {
                        ActionType = a.ActionType,
                        OldValue = a.OldValue,
                        NewValue = a.NewValue,
                        Detail = a.Detail,
                        ActorName = actorName,
                        ActionedOn = a.ActionedOn
                    };
                }).ToList();

                dto.AvailableStatuses = TicketWorkflow.GetAvailableStatuses(context, ticket.Status);

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
                IsTerminal = ticket.Status != null && ticket.Status.IsTerminal,
                ResolutionText = ticket.ResolutionText,
                CsatScore = ticket.CsatScore,
                CsatFeedback = ticket.CsatFeedback,
                CreatedOn = ticket.CreatedOn,
                ResolvedOn = ticket.ResolvedOn,
                ClosedOn = ticket.ClosedOn,
                CreatedByCode = ticket.CreatedByCode,
                CompanyId = ticket.Requester != null && ticket.Requester.CompanyId.HasValue
                    ? ticket.Requester.CompanyId.Value
                    : 0,
                RowVersion = ticket.RowVersion
            };
        }
    }
}
