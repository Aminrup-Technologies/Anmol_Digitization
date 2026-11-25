// ========================================================================
// When: 2026-11-25
// What: Queue ITSM mail through dbo.NotificationQueue and build the ticket
//       link from deployment configuration. No host name is compiled in.
//       A missing or non-HTTP origin does not write a relative link.
// Why: Section 14 notices share one insert. An empty address, or no
//      absolute origin, skips that notice and still lets the ticket commit.
//      A SQL failure is the caller's transaction to roll back.
// ========================================================================

using System;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net;
using AnmolDristi.ITSM.DAL;

namespace AnmolDristi.ITSM.BLL
{
    internal static class ItsmNotification
    {
        /// <summary>
        /// Absolute ticket URL, or null when neither configured origin is http or https.
        /// Callers skip the notice. This method does not invent a host or a ~/ path.
        /// </summary>
        public static string TicketUrl(int ticketId)
        {
            var origin = ReadOrigin();
            if (origin == null)
            {
                return null;
            }

            return origin + "/ITSM/UI/TicketDetail.aspx?id=" + ticketId;
        }

        public static void EnqueueForEmployee(
            ItsmDbContext context,
            int ticketId,
            string employeeCode,
            string subject,
            string introText)
        {
            if (context == null || string.IsNullOrWhiteSpace(employeeCode))
            {
                return;
            }

            var user = context.UserMasters
                .AsNoTracking()
                .FirstOrDefault(u => u.EmployeeCode == employeeCode);
            if (user == null || string.IsNullOrWhiteSpace(user.Email))
            {
                return;
            }

            var url = TicketUrl(ticketId);
            if (url == null)
            {
                Trace.TraceWarning(
                    "ITSM notification skipped for ticket " + ticketId
                    + ". No absolute public origin is configured.");
                return;
            }

            var body = "<p>" + WebUtility.HtmlEncode(introText ?? string.Empty) + "</p>"
                + "<p><a href=\"" + url + "\">View ticket</a></p>";
            Enqueue(context, ticketId, user.Email, user.EmployeeName, subject, body);
        }

        public static void Enqueue(
            ItsmDbContext context,
            int ticketId,
            string recipientEmail,
            string recipientName,
            string subject,
            string body)
        {
            if (context == null || string.IsNullOrWhiteSpace(recipientEmail))
            {
                return;
            }

            if (ReadOrigin() == null)
            {
                Trace.TraceWarning(
                    "ITSM notification skipped for ticket " + ticketId
                    + ". No absolute public origin is configured.");
                return;
            }

            const string sql = @"
INSERT INTO dbo.NotificationQueue
    (FormSubmissionId, EscalationLevel, RecipientEmail, RecipientName, Subject, Body, NotificationType, Status, SourceModule)
VALUES
    (@FormSubmissionId, 0, @RecipientEmail, @RecipientName, @Subject, @Body, 'Email', 'Pending', 'ITSM')";

            context.Database.ExecuteSqlCommand(
                sql,
                new SqlParameter("@FormSubmissionId", ticketId),
                new SqlParameter("@RecipientEmail", recipientEmail),
                new SqlParameter("@RecipientName", (object)recipientName ?? DBNull.Value),
                new SqlParameter("@Subject", subject ?? string.Empty),
                new SqlParameter("@Body", body ?? string.Empty));
        }

        /// <summary>
        /// ItsmPublicBaseUrl wins. VmsPublicBaseUrl is the existing site origin
        /// when ITSM has no key of its own. "auto" and non-HTTP values are
        /// ignored here because this layer cannot read the browser request.
        /// </summary>
        private static string ReadOrigin()
        {
            var configured = ConfigurationManager.AppSettings["ItsmPublicBaseUrl"];
            if (string.IsNullOrWhiteSpace(configured))
            {
                configured = ConfigurationManager.AppSettings["VmsPublicBaseUrl"];
            }

            if (string.IsNullOrWhiteSpace(configured))
            {
                return null;
            }

            configured = configured.Trim();
            if (configured.Equals("auto", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!configured.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                && !configured.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return configured.TrimEnd('/');
        }
    }
}
