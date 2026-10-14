using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using AnmolDristi.BLL;

namespace AnmolDristi.DAL
{
    /// <summary>
    /// Persistence for VMS_AuditEvent. Inserts are no-ops when the table is missing
    /// so pre-migration databases keep working. Does not write VisitLog / DayLog /
    /// attendance / after-hours audit rows.
    /// </summary>
    public class VmsAuditEventRepository
    {
        private readonly string _connectionString;

        public VmsAuditEventRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        }

        public void Insert(
            SqlConnection con,
            SqlTransaction trans,
            int logId,
            string eventType,
            string actorRole,
            string actorId,
            int? dayLogId,
            string metadata,
            DateTime? eventTime)
        {
            if (con == null)
                throw new ArgumentNullException("con");
            if (logId <= 0 || string.IsNullOrWhiteSpace(eventType))
                return;

            using (SqlCommand cmd = new SqlCommand(@"
                IF OBJECT_ID('dbo.VMS_AuditEvent', 'U') IS NOT NULL
                BEGIN
                    INSERT INTO VMS_AuditEvent
                        (LogID, DayLogID, EventType, ActorRole, ActorID, EventTime, Metadata, CreatedOn)
                    VALUES
                        (@LogID, @DayLogID, @EventType, @ActorRole, @ActorID, @EventTime, @Metadata, GETDATE())
                END", con, trans))
            {
                cmd.Parameters.AddWithValue("@LogID", logId);
                cmd.Parameters.AddWithValue("@DayLogID", dayLogId.HasValue ? (object)dayLogId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@EventType", Truncate(eventType, 50));
                cmd.Parameters.AddWithValue("@ActorRole",
                    string.IsNullOrWhiteSpace(actorRole) ? (object)DBNull.Value : Truncate(actorRole, 30));
                cmd.Parameters.AddWithValue("@ActorID",
                    string.IsNullOrWhiteSpace(actorId) ? (object)DBNull.Value : Truncate(actorId, 50));
                cmd.Parameters.AddWithValue("@EventTime", eventTime ?? DateTime.Now);
                cmd.Parameters.AddWithValue("@Metadata",
                    string.IsNullOrWhiteSpace(metadata) ? (object)DBNull.Value : Truncate(metadata, 2000));
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Canonical visit timeline: EventTime, then EventID, matching vw_VMS_VisitAuditTimeline.
        /// </summary>
        public IList<AuditEventRecord> GetTimeline(int logId)
        {
            var rows = new List<AuditEventRecord>();
            if (logId <= 0)
                return rows;

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(@"
                    IF OBJECT_ID('dbo.VMS_AuditEvent', 'U') IS NOT NULL
                    BEGIN
                        SELECT EventID, LogID, DayLogID, EventType,
                               ActorRole, ActorID, EventTime, Metadata, CreatedOn
                        FROM dbo.VMS_AuditEvent
                        WHERE LogID = @LogID
                        ORDER BY EventTime ASC, EventID ASC
                    END", con))
                {
                    cmd.Parameters.AddWithValue("@LogID", logId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rows.Add(Map(reader));
                        }
                    }
                }
            }

            return rows;
        }

        private static AuditEventRecord Map(SqlDataReader reader)
        {
            string eventType = reader["EventType"] == DBNull.Value
                ? null
                : reader["EventType"].ToString();

            return new AuditEventRecord
            {
                EventID = Convert.ToInt32(reader["EventID"]),
                LogID = Convert.ToInt32(reader["LogID"]),
                DayLogID = reader["DayLogID"] == DBNull.Value
                    ? (int?)null
                    : Convert.ToInt32(reader["DayLogID"]),
                EventType = eventType,
                DisplayLabel = AuditEventType.ToDisplayLabel(eventType),
                ActorRole = reader["ActorRole"] == DBNull.Value ? null : reader["ActorRole"].ToString(),
                ActorID = reader["ActorID"] == DBNull.Value ? null : reader["ActorID"].ToString(),
                EventTime = Convert.ToDateTime(reader["EventTime"]),
                Metadata = reader["Metadata"] == DBNull.Value ? null : reader["Metadata"].ToString(),
                CreatedOn = Convert.ToDateTime(reader["CreatedOn"])
            };
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;
            return value.Substring(0, maxLength);
        }
    }
}
