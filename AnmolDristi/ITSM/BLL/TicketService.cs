using System;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using AnmolDristi.ITSM.DAL;
using AnmolDristi.ITSM.DAL.Entities;

namespace AnmolDristi.ITSM.BLL
{
    /// <summary>
    /// Master orchestrator for new-ticket creation (schema §5.1–5.4).
    /// Coordinates priority, dispatch, SLA, approvals, and NotificationQueue.
    /// </summary>
    public class TicketService
    {
        private readonly PriorityCalculationService _priorityService =
            new PriorityCalculationService();

        private readonly DispatchService _dispatchService =
            new DispatchService();

        private readonly SlaService _slaService =
            new SlaService();

        /// <summary>
        /// Creates a ticket: resolves requester context, priority, assignee,
        /// ticket number, optional manager approval, and queues an email notification.
        /// </summary>
        public void CreateTicket(
            string requesterCode,
            int requestTypeId,
            string title,
            string description,
            int categoryId,
            int subCategoryId,
            int? itemId,
            int impactId,
            int urgencyId)
        {
            if (string.IsNullOrWhiteSpace(requesterCode))
            {
                throw new ArgumentException("Requester code is required.", nameof(requesterCode));
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Title is required.", nameof(title));
            }

            using (var context = new ItsmDbContext())
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {
                    var requester = context.UserMasters
                        .AsNoTracking()
                        .FirstOrDefault(u => u.EmployeeCode == requesterCode);

                    if (requester == null)
                    {
                        throw new InvalidOperationException(
                            "Requester was not found in MST_UserMaster for code: " + requesterCode);
                    }

                    var requestType = context.RequestTypes
                        .AsNoTracking()
                        .FirstOrDefault(r => r.Id == requestTypeId && r.IsActive);

                    if (requestType == null)
                    {
                        throw new InvalidOperationException(
                            "Request type was not found or is inactive: " + requestTypeId);
                    }

                    var priorityId = _priorityService.CalculatePriorityId(impactId, urgencyId);

                    var priority = context.Priorities
                        .AsNoTracking()
                        .FirstOrDefault(p => p.Id == priorityId);

                    var resolutionHours = priority != null ? priority.SlaResolutionHours : 0;
                    var createdOn = DateTime.Now;
                    var sla = _slaService.CalculateSla(
                        requester.BranchId,
                        createdOn,
                        resolutionHours);

                    var assigneeCode = _dispatchService.ResolveAssignee(
                        requester.BranchId,
                        categoryId,
                        subCategoryId,
                        itemId);

                    var ticketNumber = GenerateTicketNumber(
                        context, requestType, createdOn);

                    var requiresApproval = requestType.RequiresManagerApproval;
                    var statusId = ResolveInitialStatusId(context, requiresApproval);

                    var ticket = new Ticket
                    {
                        TicketNumber = ticketNumber,
                        RequestTypeId = requestType.Id,
                        Title = title.Trim(),
                        Description = description,
                        RequesterCode = requester.EmployeeCode,
                        AssigneeCode = assigneeCode,
                        PlantId = requester.PlantId,
                        BranchId = requester.BranchId,
                        CategoryId = categoryId,
                        SubCategoryId = subCategoryId,
                        ItemId = itemId,
                        ImpactId = impactId,
                        UrgencyId = urgencyId,
                        PriorityId = priorityId,
                        StatusId = statusId,
                        CreatedOn = createdOn,
                        CreatedByCode = requester.EmployeeCode
                    };

                    context.Tickets.Add(ticket);
                    context.SaveChanges();

                    if (requiresApproval)
                    {
                        if (string.IsNullOrWhiteSpace(requester.ReportingManagerCode))
                        {
                            throw new InvalidOperationException(
                                "Request type requires manager approval, but the requester has no ReportingManagerCode.");
                        }

                        // SoD: requester cannot approve their own CR/SR (compliance §3).
                        if (string.Equals(
                            requester.EmployeeCode,
                            requester.ReportingManagerCode,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException(
                                "Separation of Duties violation: RequesterCode cannot match ApproverCode.");
                        }

                        context.TicketApprovals.Add(new TicketApproval
                        {
                            TicketId = ticket.Id,
                            ApprovalLevel = "Manager",
                            ApproverCode = requester.ReportingManagerCode,
                            ApprovalStatus = "Pending",
                            CreatedOn = createdOn
                        });
                        context.SaveChanges();
                    }

                    EnqueueTicketCreatedNotification(
                        context,
                        ticket,
                        requester,
                        assigneeCode,
                        sla.SlaStartTime,
                        sla.DueDate);

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private static string GenerateTicketNumber(
            ItsmDbContext context,
            RequestType requestType,
            DateTime createdOn)
        {
            var prefix = (requestType.Prefix ?? requestType.Code ?? "XX").Trim();
            if (prefix.Length > 2)
            {
                prefix = prefix.Substring(0, 2);
            }

            var yy = createdOn.ToString("yy", CultureInfo.InvariantCulture);
            var mm = createdOn.ToString("MM", CultureInfo.InvariantCulture);
            var numberPrefix = prefix + yy + mm; // e.g. CR2608

            var maxExisting = context.Tickets
                .AsNoTracking()
                .Where(t =>
                    t.RequestTypeId == requestType.Id
                    && t.TicketNumber.StartsWith(numberPrefix))
                .Select(t => t.TicketNumber)
                .ToList();

            var nextSequence = 1;
            if (maxExisting.Count > 0)
            {
                nextSequence = maxExisting
                    .Select(n =>
                    {
                        int seq;
                        if (n != null && n.Length >= 10
                            && int.TryParse(n.Substring(n.Length - 4), out seq))
                        {
                            return seq;
                        }

                        return 0;
                    })
                    .DefaultIfEmpty(0)
                    .Max() + 1;
            }

            return numberPrefix + nextSequence.ToString("D4", CultureInfo.InvariantCulture);
        }

        private static int ResolveInitialStatusId(ItsmDbContext context, bool requiresApproval)
        {
            var statusName = requiresApproval ? "Pending Approval" : "New";
            var statusId = context.TicketStatuses
                .AsNoTracking()
                .Where(s => s.IsActive && s.Name == statusName)
                .Select(s => (int?)s.Id)
                .FirstOrDefault();

            if (statusId.HasValue)
            {
                return statusId.Value;
            }

            // Fallback to any active non-terminal status if seed names differ.
            var fallback = context.TicketStatuses
                .AsNoTracking()
                .Where(s => s.IsActive && !s.IsTerminal)
                .OrderBy(s => s.Id)
                .Select(s => (int?)s.Id)
                .FirstOrDefault();

            if (!fallback.HasValue)
            {
                throw new InvalidOperationException(
                    "No active ticket status found in MST_TicketStatus. Seed statuses before creating tickets.");
            }

            return fallback.Value;
        }

        private static void EnqueueTicketCreatedNotification(
            ItsmDbContext context,
            Ticket ticket,
            UserMaster requester,
            string assigneeCode,
            DateTime slaStartTime,
            DateTime slaDueDate)
        {
            var recipientEmail = requester.Email;
            var recipientName = requester.EmployeeName;

            // Prefer notifying the assigned agent when one was resolved.
            if (!string.IsNullOrWhiteSpace(assigneeCode))
            {
                var assignee = context.UserMasters
                    .AsNoTracking()
                    .FirstOrDefault(u => u.EmployeeCode == assigneeCode);

                if (assignee != null && !string.IsNullOrWhiteSpace(assignee.Email))
                {
                    recipientEmail = assignee.Email;
                    recipientName = assignee.EmployeeName;
                }
            }

            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                return;
            }

            var ticketLink = "~/ITSM/UI/TicketDetail.aspx?id=" + ticket.Id;
            var body = new StringBuilder();
            body.Append("<p>A new ITSM ticket has been created.</p>");
            body.Append("<p><strong>Ticket:</strong> ").Append(ticket.TicketNumber).Append("<br/>");
            body.Append("<strong>Title:</strong> ").Append(System.Net.WebUtility.HtmlEncode(ticket.Title)).Append("<br/>");
            body.Append("<strong>SLA Start:</strong> ").Append(slaStartTime.ToString("dd-MMM-yyyy HH:mm")).Append("<br/>");
            body.Append("<strong>SLA Due:</strong> ").Append(slaDueDate.ToString("dd-MMM-yyyy HH:mm"));
            body.Append("</p>");
            body.Append("<p><a href=\"").Append(ticketLink).Append("\">View ticket</a></p>");
            body.Append("<hr />");
            body.Append("<p style=\"color:#666;font-size:12px;\">");
            body.Append("This is an automated message from the ITSM system. ");
            body.Append("<strong>Do not reply to this email.</strong> ");
            body.Append("To view or update this ticket, use the link above.");
            body.Append("</p>");

            const string sql = @"
INSERT INTO dbo.NotificationQueue
    (FormSubmissionId, EscalationLevel, RecipientEmail, RecipientName, Subject, Body, NotificationType, Status, SourceModule)
VALUES
    (@FormSubmissionId, 0, @RecipientEmail, @RecipientName, @Subject, @Body, 'Email', 'Pending', 'ITSM')";

            context.Database.ExecuteSqlCommand(
                sql,
                new SqlParameter("@FormSubmissionId", ticket.Id),
                new SqlParameter("@RecipientEmail", recipientEmail),
                new SqlParameter("@RecipientName", (object)recipientName ?? DBNull.Value),
                new SqlParameter("@Subject", "ITSM Ticket Created: " + ticket.TicketNumber),
                new SqlParameter("@Body", body.ToString()));
        }
    }
}
