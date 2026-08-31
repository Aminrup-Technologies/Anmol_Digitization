using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using AnmolDristi.DAL;

namespace AnmolDristi.BLL
{
    /// <summary>
    /// Single publisher for VMS_AuditEvent. Workflows call Publish; pages must not
    /// insert into the table. Failures are swallowed so missing tables or insert
    /// errors never roll back VisitLog / DayLog / attendance / after-hours writes.
    /// </summary>
    public static class AuditEventService
    {
        public static void Publish(
            int logId,
            string eventType,
            string actorRole,
            string actorId,
            int? dayLogId = null,
            string metadata = null,
            DateTime? eventTime = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(
                    System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
                {
                    con.Open();
                    Publish(con, null, logId, eventType, actorRole, actorId, dayLogId, metadata, eventTime);
                }
            }
            catch
            {
                // Timeline must not block operational writes.
            }
        }

        public static void Publish(
            SqlConnection con,
            SqlTransaction trans,
            int logId,
            string eventType,
            string actorRole,
            string actorId,
            int? dayLogId = null,
            string metadata = null,
            DateTime? eventTime = null)
        {
            try
            {
                new VmsAuditEventRepository().Insert(
                    con,
                    trans,
                    logId,
                    eventType,
                    actorRole,
                    actorId,
                    dayLogId,
                    metadata,
                    eventTime);
            }
            catch
            {
                // Timeline must not block operational writes.
            }
        }

        /// <summary>
        /// Reusable visit timeline for future pages
        /// (09:00 Registered / 09:02 OTP Verified / 09:05 Approved / …).
        /// </summary>
        public static IList<AuditEventRecord> GetTimeline(int logId)
        {
            try
            {
                return new VmsAuditEventRepository().GetTimeline(logId);
            }
            catch
            {
                return new List<AuditEventRecord>();
            }
        }

        public static string ActorRoleFromAttendanceActor(string actor)
        {
            string value = (actor ?? string.Empty).Trim();
            if (string.Equals(value, "HostManual", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Host", StringComparison.OrdinalIgnoreCase))
                return AuditActorRole.Host;
            if (value.IndexOf("Reception", StringComparison.OrdinalIgnoreCase) >= 0)
                return AuditActorRole.Receptionist;
            if (string.Equals(value, "ADMIN_INLINE", StringComparison.OrdinalIgnoreCase) ||
                value.IndexOf("Admin", StringComparison.OrdinalIgnoreCase) >= 0)
                return AuditActorRole.Admin;
            return AuditActorRole.Gate;
        }

        public static string Metadata(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                return null;
            return "{\"" + JsonEscape(key) + "\":\"" + JsonEscape(value ?? string.Empty) + "\"}";
        }

        public static string Metadata(IDictionary<string, string> pairs)
        {
            if (pairs == null || pairs.Count == 0)
                return null;

            var sb = new StringBuilder();
            sb.Append('{');
            bool first = true;
            foreach (KeyValuePair<string, string> pair in pairs)
            {
                if (string.IsNullOrEmpty(pair.Key))
                    continue;
                if (!first)
                    sb.Append(',');
                first = false;
                sb.Append('"').Append(JsonEscape(pair.Key)).Append("\":\"");
                sb.Append(JsonEscape(pair.Value ?? string.Empty)).Append('"');
            }

            if (sb.Length == 1)
                return null;
            sb.Append('}');
            return sb.ToString();
        }

        private static string JsonEscape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
        }
    }
}
