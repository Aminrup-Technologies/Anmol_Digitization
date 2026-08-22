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
    /// When: 2026-08-21
    /// What: Snapshot requester.CompanyId onto Ticket.CompanyId at create.
    /// Why: Ticket tenant must be immutable; live Requester.CompanyId can change in HRMS.
    /// When: 2026-08-22
    /// What: Resolve legacy BranchId (MST_Branch.Branch_ID) → MST_Branch.Id for
    ///       Tickets.BranchId FK and DispatchService routing. Resolve PlantId
    ///       deterministically — return NULL when PlantID_ID maps to multiple
    ///       MST_Plant rows (ambiguous) or zero rows (missing).
    /// Why: MST_UserMaster.BranchId stores legacy Branch_ID (not MST_Branch.Id).
    ///      MST_UserMaster.PlantId stores legacy PlantID_ID (not MST_Plant.Id).
    ///      Tickets.BranchId FK references MST_Branch.Id. Tickets.PlantId FK
    ///      references MST_Plant.Id. PlantID_ID is not globally unique (e.g.
    ///      PlantID_ID=3277 → Id 40 "Greater Noida" + Id 41 "Nirmali").
    ///      Branch is mandatory (routing + dispatch); Plant is optional (location context).
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

                    if (!requester.CompanyId.HasValue || requester.CompanyId.Value <= 0)
                    {
                        throw new InvalidOperationException(
                            "Requester has no CompanyId on MST_UserMaster; ticket creation is blocked for tenant isolation.");
                    }

                    // ── Branch resolution ──────────────────────────────────────
                    // MST_UserMaster.BranchId stores legacy Branch_ID (e.g. 1966),
                    // NOT MST_Branch.Id (identity PK, e.g. 4).
                    // Tickets.BranchId FK references MST_Branch.Id.
                    // DispatchService.ResolveAssignee also expects MST_Branch.Id
                    // (SupportMapping.BranchId FK → MST_Branch.Id).
                    // Branch_ID is verified unique — exactly one match expected.
                    var branch = context.Branches
                        .AsNoTracking()
                        .FirstOrDefault(b => b.Branch_ID == requester.BranchId);

                    if (branch == null)
                    {
                        throw new InvalidOperationException(
                            "Requester BranchId " + requester.BranchId
                            + " does not resolve to MST_Branch.Branch_ID for EmployeeCode: "
                            + requester.EmployeeCode + ".");
                    }

                    var resolvedBranchId = branch.Id;

                    // ── Plant resolution ───────────────────────────────────────
                    // MST_UserMaster.PlantId stores legacy PlantID_ID (business key),
                    // NOT MST_Plant.Id (identity PK). Tickets.PlantId FK references
                    // MST_Plant.Id.
                    // PlantID_ID is NOT globally unique — e.g. PlantID_ID=3277 maps
                    // to both Id=40 (Greater Noida) and Id=41 (Nirmali).
                    // Semantics: NULL when <= 0, missing, or ambiguous (multiple matches).
                    int? resolvedPlantId = ResolvePlantId(context, requester.PlantId);

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

                    // SlaService uses VMS_PlantTimings.Branch_ID which stores the
                    // legacy Branch_ID value (not MST_Branch.Id), so we pass the
                    // original legacy value from the requester.
                    var sla = _slaService.CalculateSla(
                        requester.BranchId,
                        createdOn,
                        resolutionHours);

                    // DispatchService.ResolveAssignee expects MST_Branch.Id
                    // (SupportMapping.BranchId FK → MST_Branch.Id).
                    var assigneeCode = _dispatchService.ResolveAssignee(
                        resolvedBranchId,
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
                        PlantId = resolvedPlantId,
                        BranchId = resolvedBranchId,
                        CompanyId = requester.CompanyId,
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

                    context.TicketAuditLogs.Add(new TicketAuditLog
                    {
                        TicketId = ticket.Id,
                        CompanyId = ticket.CompanyId.Value,
                        ActionType = "Created",
                        OldValue = null,
                        NewValue = requiresApproval ? "Pending Approval" : "New",
                        Detail = ticket.TicketNumber,
                        ActorCode = requester.EmployeeCode,
                        ActionedOn = createdOn
                    });
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

        /// <summary>
        /// Resolves legacy MST_UserMaster.PlantId (PlantID_ID business key) to
        /// MST_Plant.Id (identity PK) for the Tickets.PlantId FK.
        ///
        /// Returns NULL when:
        ///   - PlantId &lt;= 0 (no plant assignment — Corporate Office)
        ///   - Zero MST_Plant rows match PlantID_ID
        ///   - Multiple MST_Plant rows match PlantID_ID (ambiguous — data quality issue)
        ///
        /// Returns MST_Plant.Id only when exactly one row matches (deterministic).
        /// Does NOT use FirstOrDefault, Min, Max, or any ordering to break ties.
        /// </summary>
        private static int? ResolvePlantId(ItsmDbContext context, int legacyPlantId)
        {
            if (legacyPlantId <= 0)
            {
                return null;
            }

            var matchingPlantIds = context.Plants
                .AsNoTracking()
                .Where(p => p.PlantID_ID == legacyPlantId)
                .Select(p => (int?)p.Id)
                .ToList();

            if (matchingPlantIds.Count == 1)
            {
                return matchingPlantIds[0];
            }

            // Zero matches: PlantID_ID does not exist in MST_Plant.
            // Multiple matches: PlantID_ID is ambiguous (e.g. 3277 → Id 40 + 41).
            // In both cases, return NULL (Corporate Office / unresolved).
            return null;
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
