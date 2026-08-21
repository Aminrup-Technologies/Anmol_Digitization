// ========================================================================
// When: 2026-08-21
// What: Ticket detail from Ticket.CompanyId; AsNoTracking Select into DTO;
//       CompanyName via dbo.MST_Company.Company_ID; CanViewTicket;
//       CanAddInternalNote requires CanProcessTicket plus existing
//       requester internal-note policy (pending approver does not see internals).
// Why: View is not process. A view-only pending approver must not be
//      advertised internal-note capability or shown internal comments.
// ========================================================================

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using AnmolDristi.ITSM.DAL;

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
        public int? CompanyId { get; set; }
        public string CompanyName { get; set; }
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

    /// <summary>
    /// Read-only ticket detail. Tenant is Ticket.CompanyId (immutable snapshot).
    /// View uses CanViewTicket; TicketId is not sufficient authorization.
    /// </summary>
    public class TicketDetailService
    {
        public TicketDetailDto GetTicketDetail(int ticketId, string actorCode)
        {
            if (string.IsNullOrWhiteSpace(actorCode))
            {
                throw new ArgumentException("Actor is required.", "actorCode");
            }

            using (var context = new ItsmDbContext())
            {
                var dto = (from t in context.Tickets.AsNoTracking()
                           where t.Id == ticketId
                           join c in context.Companies.AsNoTracking()
                               on t.CompanyId equals (int?)c.Company_ID into companyJoin
                           from c in companyJoin.DefaultIfEmpty()
                           select new TicketDetailDto
                           {
                               Id = t.Id,
                               TicketNumber = t.TicketNumber,
                               Title = t.Title,
                               Description = t.Description,
                               RequesterCode = t.RequesterCode,
                               RequesterName = t.Requester != null ? t.Requester.EmployeeName : t.RequesterCode,
                               AssigneeCode = t.AssigneeCode,
                               AssigneeName = t.Assignee != null ? t.Assignee.EmployeeName : "Unassigned",
                               RequestTypeName = t.RequestType.Code + " - " + t.RequestType.Name,
                               CategoryName = t.Category.Name,
                               SubCategoryName = t.SubCategory != null ? t.SubCategory.Name : null,
                               ItemName = t.Item != null ? t.Item.Name : null,
                               BranchName = t.Branch.Branch_Description,
                               ImpactName = t.Impact.Name,
                               UrgencyName = t.Urgency.Name,
                               PriorityName = t.Priority.Name,
                               PriorityId = t.PriorityId,
                               StatusId = t.StatusId,
                               StatusName = t.Status.Name,
                               IsTerminal = t.Status.IsTerminal,
                               ResolutionText = t.ResolutionText,
                               CsatScore = t.CsatScore,
                               CsatFeedback = t.CsatFeedback,
                               CreatedOn = t.CreatedOn,
                               ResolvedOn = t.ResolvedOn,
                               ClosedOn = t.ClosedOn,
                               CreatedByCode = t.CreatedByCode,
                               CompanyId = t.CompanyId,
                               CompanyName = c != null ? c.Company_Description : null,
                               RowVersion = t.RowVersion
                           }).FirstOrDefault();

                if (dto == null)
                {
                    return null;
                }

                if (!ItsmTicketAccess.CanViewTicket(
                    context,
                    ticketId,
                    actorCode,
                    dto.RequesterCode,
                    dto.AssigneeCode))
                {
                    return null;
                }

                dto.CanApprove = context.TicketApprovals
                    .AsNoTracking()
                    .Any(a => a.TicketId == ticketId
                              && a.ApproverCode == actorCode
                              && a.ApprovalStatus == "Pending");
                dto.CanAddInternalNote =
                    ItsmTicketAccess.CanProcessTicket(actorCode, dto.RequesterCode, dto.AssigneeCode)
                    && (!string.Equals(dto.RequesterCode, actorCode, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(dto.AssigneeCode, actorCode, StringComparison.OrdinalIgnoreCase));

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

                dto.AuditHistory = (from a in context.TicketAuditLogs.AsNoTracking()
                                    where a.TicketId == ticketId
                                    join u in context.UserMasters.AsNoTracking()
                                        on a.ActorCode equals u.EmployeeCode into actorJoin
                                    from u in actorJoin.DefaultIfEmpty()
                                    orderby a.ActionedOn descending
                                    select new TicketAuditLogDto
                                    {
                                        ActionType = a.ActionType,
                                        OldValue = a.OldValue,
                                        NewValue = a.NewValue,
                                        Detail = a.Detail,
                                        ActorName = u != null ? u.EmployeeName : a.ActorCode,
                                        ActionedOn = a.ActionedOn
                                    }).ToList();

                dto.AvailableStatuses = TicketWorkflow.GetAvailableStatuses(
                    context,
                    dto.StatusName,
                    dto.IsTerminal);
                return dto;
            }
        }
    }
}
