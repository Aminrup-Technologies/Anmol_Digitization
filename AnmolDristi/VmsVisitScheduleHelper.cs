using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;

namespace AnmolDristi
{
    public class VisitScheduleRequest
    {
        public string ScheduleType { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public TimeSpan TimeFrom { get; set; }
        public TimeSpan TimeTo { get; set; }
        public DateTime RequestedValidFrom { get; set; }
        public DateTime RequestedValidUpto { get; set; }
        public int TotalDays { get; set; }
    }

    public class VisitDayLogInfo
    {
        public int DayLogId { get; set; }
        public int LogId { get; set; }
        public DateTime VisitDate { get; set; }
        public DateTime DayValidFrom { get; set; }
        public DateTime DayValidUpto { get; set; }
        public string VisitStatus { get; set; }
        public DateTime? ActualInTime { get; set; }
        public DateTime? ActualOutTime { get; set; }
        public bool IsDayApproved { get; set; }
    }

    public static class VmsVisitScheduleHelper
    {
        public const int MaxScheduleDays = 30;

        public static VisitScheduleRequest ParseSchedule(
            string scheduleType,
            string fromDateText,
            string toDateText,
            string timeFromText,
            string timeToText)
        {
            if (string.IsNullOrWhiteSpace(scheduleType))
                throw new Exception("Please select a visit schedule type.");
            DateTime fromDate;
            if (!DateTime.TryParse(fromDateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out fromDate))
                throw new Exception("Please select a valid visit start date.");

            DateTime toDate = fromDate;
            if (scheduleType.Equals("MultiDay", StringComparison.OrdinalIgnoreCase))
            {
                if (!DateTime.TryParse(toDateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out toDate))
                    throw new Exception("Please select a valid visit end date.");

                if (toDate.Date < fromDate.Date)
                    throw new Exception("Visit end date cannot be before the start date.");
            }
            TimeSpan timeFrom;
            if (!TimeSpan.TryParse(timeFromText, out timeFrom))
                throw new Exception("Please select a valid expected arrival time.");
            TimeSpan timeTo;
            if (!TimeSpan.TryParse(timeToText, out timeTo))
                throw new Exception("Please select a valid expected departure time.");

            if (timeTo <= timeFrom)
                throw new Exception("Departure time must be after arrival time.");

            int totalDays = (toDate.Date - fromDate.Date).Days + 1;
            if (totalDays > MaxScheduleDays)
                throw new Exception($"Visits can be scheduled for up to {MaxScheduleDays} days.");

            DateTime requestedFrom = fromDate.Date.Add(timeFrom);
            DateTime requestedTo = toDate.Date.Add(timeTo);

            if (requestedTo < DateTime.Now.AddMinutes(-5))
                throw new Exception("The selected visit window is already in the past.");

            return new VisitScheduleRequest
            {
                ScheduleType = scheduleType,
                FromDate = fromDate.Date,
                ToDate = toDate.Date,
                TimeFrom = timeFrom,
                TimeTo = timeTo,
                RequestedValidFrom = requestedFrom,
                RequestedValidUpto = requestedTo,
                TotalDays = totalDays
            };
        }

        public static void CreateRequestedDayLogs(
            SqlConnection con,
            SqlTransaction trans,
            int logId,
            VisitScheduleRequest schedule,
            bool approveImmediately)
        {
            string insertSql = @"
                INSERT INTO VMS_VisitDayLog
                (LogID, VisitDate, DayValidFrom, DayValidUpto, VisitStatus, IsDayApproved)
                VALUES
                (@LogID, @VisitDate, @DayValidFrom, @DayValidUpto, 'Scheduled', @IsDayApproved)";

            for (DateTime day = schedule.FromDate; day <= schedule.ToDate; day = day.AddDays(1))
            {
                using (SqlCommand cmd = new SqlCommand(insertSql, con, trans))
                {
                    cmd.Parameters.AddWithValue("@LogID", logId);
                    cmd.Parameters.AddWithValue("@VisitDate", day.Date);
                    cmd.Parameters.AddWithValue("@DayValidFrom", day.Date.Add(schedule.TimeFrom));
                    cmd.Parameters.AddWithValue("@DayValidUpto", day.Date.Add(schedule.TimeTo));
                    cmd.Parameters.AddWithValue("@IsDayApproved", approveImmediately);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void ApproveVisitSchedule(
            SqlConnection con,
            SqlTransaction trans,
            int logId,
            DateTime? approvedFrom,
            DateTime? approvedTo)
        {
            DateTime? requestedFrom = null;
            DateTime? requestedTo = null;

            using (SqlCommand readCmd = new SqlCommand(
                "SELECT RequestedValidFrom, RequestedValidUpto FROM VMS_VisitLog WHERE LogID = @LogID", con, trans))
            {
                readCmd.Parameters.AddWithValue("@LogID", logId);
                using (SqlDataReader reader = readCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        if (reader["RequestedValidFrom"] != DBNull.Value)
                            requestedFrom = Convert.ToDateTime(reader["RequestedValidFrom"]);
                        if (reader["RequestedValidUpto"] != DBNull.Value)
                            requestedTo = Convert.ToDateTime(reader["RequestedValidUpto"]);
                    }
                }
            }

            DateTime finalFrom = approvedFrom ?? requestedFrom ?? DateTime.Now;
            DateTime finalTo = approvedTo ?? requestedTo ?? finalFrom.AddHours(8);

            using (SqlCommand approveDaysCmd = new SqlCommand(@"
                UPDATE VMS_VisitDayLog
                SET IsDayApproved = 1
                WHERE LogID = @LogID
                  AND VisitDate >= CAST(@ApprovedFrom AS date)
                  AND VisitDate <= CAST(@ApprovedTo AS date)", con, trans))
            {
                approveDaysCmd.Parameters.AddWithValue("@LogID", logId);
                approveDaysCmd.Parameters.AddWithValue("@ApprovedFrom", finalFrom);
                approveDaysCmd.Parameters.AddWithValue("@ApprovedTo", finalTo);
                approveDaysCmd.ExecuteNonQuery();
            }

            using (SqlCommand unapproveDaysCmd = new SqlCommand(@"
                UPDATE VMS_VisitDayLog
                SET IsDayApproved = 0, VisitStatus = 'Cancelled'
                WHERE LogID = @LogID
                  AND (VisitDate < CAST(@ApprovedFrom AS date) OR VisitDate > CAST(@ApprovedTo AS date))", con, trans))
            {
                unapproveDaysCmd.Parameters.AddWithValue("@LogID", logId);
                unapproveDaysCmd.Parameters.AddWithValue("@ApprovedFrom", finalFrom);
                unapproveDaysCmd.Parameters.AddWithValue("@ApprovedTo", finalTo);
                unapproveDaysCmd.ExecuteNonQuery();
            }

            using (SqlCommand updateVisitCmd = new SqlCommand(@"
                UPDATE VMS_VisitLog
                SET ApprovedValidFrom = @ApprovedFrom,
                    ApprovedValidUpto = @ApprovedTo,
                    ValidFrom = @ApprovedFrom,
                    ValidUpto = @ApprovedTo
                WHERE LogID = @LogID", con, trans))
            {
                updateVisitCmd.Parameters.AddWithValue("@LogID", logId);
                updateVisitCmd.Parameters.AddWithValue("@ApprovedFrom", finalFrom);
                updateVisitCmd.Parameters.AddWithValue("@ApprovedTo", finalTo);
                updateVisitCmd.ExecuteNonQuery();
            }
        }

        public static VisitDayLogInfo GetDayLogForDate(
            SqlConnection con,
            int logId,
            DateTime visitDate,
            SqlTransaction trans = null)
        {
            string query = @"
                SELECT DayLogID, LogID, VisitDate, DayValidFrom, DayValidUpto,
                       VisitStatus, ActualInTime, ActualOutTime, IsDayApproved
                FROM VMS_VisitDayLog
                WHERE LogID = @LogID AND VisitDate = @VisitDate";

            using (SqlCommand cmd = new SqlCommand(query, con, trans))
            {
                cmd.Parameters.AddWithValue("@LogID", logId);
                cmd.Parameters.AddWithValue("@VisitDate", visitDate.Date);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    return MapDayLog(reader);
                }
            }
        }

        public static VisitDayLogInfo GetTodayDayLog(SqlConnection con, int logId, SqlTransaction trans = null)
        {
            return GetDayLogForDate(con, logId, DateTime.Now.Date, trans);
        }

        public static VisitDayLogInfo GetTodayDayLogOrLegacyFallback(
            SqlConnection con,
            int logId,
            DateTime validFrom,
            DateTime validUpto,
            string visitStatus,
            SqlTransaction trans = null)
        {
            VisitDayLogInfo todayLog = GetTodayDayLog(con, logId, trans);
            if (todayLog != null)
                return todayLog;

            DateTime today = DateTime.Now.Date;
            if (today >= validFrom.Date && today <= validUpto.Date)
            {
                return new VisitDayLogInfo
                {
                    DayLogId = 0,
                    LogId = logId,
                    VisitDate = today,
                    DayValidFrom = validFrom,
                    DayValidUpto = validUpto,
                    VisitStatus = string.IsNullOrWhiteSpace(visitStatus) ? "Scheduled" : visitStatus,
                    IsDayApproved = true
                };
            }

            return null;
        }

        public static bool HasFutureApprovedDays(SqlConnection con, int logId, DateTime afterDate, SqlTransaction trans = null)
        {
            using (SqlCommand cmd = new SqlCommand(@"
                SELECT COUNT(1)
                FROM VMS_VisitDayLog
                WHERE LogID = @LogID
                  AND IsDayApproved = 1
                  AND VisitDate > @AfterDate", con, trans))
            {
                cmd.Parameters.AddWithValue("@LogID", logId);
                cmd.Parameters.AddWithValue("@AfterDate", afterDate.Date);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        /// <summary>
        /// Day-aware check-in. Returns true only when both DayLog and VisitLog
        /// transitioned (rowsAffected == 1 each).
        /// DayLogID must belong to the same LogID (AR-VMS-08A).
        /// Requires today's approved DayLog and Parent ApprovalStatus='Approved' (AR-VMS-10).
        /// DayLog may be Scheduled or Checked-Out (same-day re-entry).
        /// Parent may be Scheduled or Checked-Out (SingleDay after checkout is Checked-Out).
        /// ActualInTime is the latest active check-in stamp for this DayLog row.
        /// One DayLog exists per (LogID, VisitDate); this UPDATE is that row's current state.
        /// </summary>
        public static bool CheckInDay(
            SqlConnection con,
            int dayLogId,
            int logId,
            string guardId,
            SqlTransaction trans = null)
        {
            int dayRows;
            using (SqlCommand cmd = new SqlCommand(@"
                UPDATE d
                SET d.VisitStatus = 'Checked-In',
                    d.ActualInTime = GETDATE(),
                    d.ActualOutTime = NULL
                FROM VMS_VisitDayLog d
                INNER JOIN VMS_VisitLog v ON v.LogID = d.LogID
                WHERE d.DayLogID = @DayLogID
                  AND d.LogID = @LogID
                  AND d.VisitDate = CAST(GETDATE() AS date)
                  AND d.IsDayApproved = 1
                  AND d.VisitStatus IN ('Scheduled', 'Checked-Out')
                  AND v.ApprovalStatus = 'Approved'", con, trans))
            {
                cmd.Parameters.AddWithValue("@DayLogID", dayLogId);
                cmd.Parameters.AddWithValue("@LogID", logId);
                dayRows = cmd.ExecuteNonQuery();
            }

            if (dayRows != 1)
                return false;

            int visitRows;
            using (SqlCommand cmd = new SqlCommand(@"
                UPDATE VMS_VisitLog
                SET VisitStatus = 'Checked-In',
                    ActualInTime = GETDATE(),
                    ActualOutTime = NULL
                WHERE LogID = @LogID
                  AND VisitStatus IN ('Scheduled', 'Checked-Out')
                  AND ApprovalStatus = 'Approved'", con, trans))
            {
                cmd.Parameters.AddWithValue("@LogID", logId);
                visitRows = cmd.ExecuteNonQuery();
            }

            return visitRows == 1;
        }

        /// <summary>
        /// Day-aware check-out. Returns true only when both DayLog and VisitLog
        /// transitioned from Checked-In (rowsAffected == 1 each).
        /// DayLogID must belong to the same LogID (AR-VMS-08A).
        /// Requires today's approved DayLog and Parent ApprovalStatus='Approved' (AR-VMS-10).
        /// </summary>
        public static bool CheckOutDay(
            SqlConnection con,
            int dayLogId,
            int logId,
            string guardId,
            SqlTransaction trans = null)
        {
            int dayRows;
            using (SqlCommand cmd = new SqlCommand(@"
                UPDATE d
                SET d.VisitStatus = 'Checked-Out',
                    d.ActualOutTime = GETDATE(),
                    d.CheckedOutBy = @GuardId
                FROM VMS_VisitDayLog d
                INNER JOIN VMS_VisitLog v ON v.LogID = d.LogID
                WHERE d.DayLogID = @DayLogID
                  AND d.LogID = @LogID
                  AND d.VisitDate = CAST(GETDATE() AS date)
                  AND d.IsDayApproved = 1
                  AND d.VisitStatus = 'Checked-In'
                  AND v.ApprovalStatus = 'Approved'", con, trans))
            {
                cmd.Parameters.AddWithValue("@DayLogID", dayLogId);
                cmd.Parameters.AddWithValue("@LogID", logId);
                cmd.Parameters.AddWithValue("@GuardId", guardId ?? "SecurityGate");
                dayRows = cmd.ExecuteNonQuery();
            }

            if (dayRows != 1)
                return false;

            bool hasFutureDays = HasFutureApprovedDays(con, logId, DateTime.Now.Date, trans);
            string nextVisitStatus = hasFutureDays ? "Scheduled" : "Checked-Out";

            int visitRows;
            using (SqlCommand cmd = new SqlCommand(@"
                UPDATE VMS_VisitLog
                SET VisitStatus = @VisitStatus,
                    ActualOutTime = GETDATE(),
                    CheckedOutBy = @GuardId
                WHERE LogID = @LogID
                  AND VisitStatus = 'Checked-In'
                  AND ApprovalStatus = 'Approved'", con, trans))
            {
                cmd.Parameters.AddWithValue("@VisitStatus", nextVisitStatus);
                cmd.Parameters.AddWithValue("@GuardId", guardId ?? "SecurityGate");
                cmd.Parameters.AddWithValue("@LogID", logId);
                visitRows = cmd.ExecuteNonQuery();
            }

            return visitRows == 1;
        }

        public static string FormatScheduleSummary(VisitScheduleRequest schedule)
        {
            if (schedule.ScheduleType == "MultiDay")
            {
                return $"{schedule.FromDate:dd MMM yyyy} to {schedule.ToDate:dd MMM yyyy}, daily {schedule.TimeFrom:hh\\:mm} - {schedule.TimeTo:hh\\:mm}";
            }

            return $"{schedule.FromDate:dd MMM yyyy}, {schedule.TimeFrom:hh\\:mm} - {schedule.TimeTo:hh\\:mm}";
        }

        private static VisitDayLogInfo MapDayLog(SqlDataReader reader)
        {
            return new VisitDayLogInfo
            {
                DayLogId = Convert.ToInt32(reader["DayLogID"]),
                LogId = Convert.ToInt32(reader["LogID"]),
                VisitDate = Convert.ToDateTime(reader["VisitDate"]),
                DayValidFrom = Convert.ToDateTime(reader["DayValidFrom"]),
                DayValidUpto = Convert.ToDateTime(reader["DayValidUpto"]),
                VisitStatus = reader["VisitStatus"]?.ToString() ?? "Scheduled",
                ActualInTime = reader["ActualInTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["ActualInTime"]),
                ActualOutTime = reader["ActualOutTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["ActualOutTime"]),
                IsDayApproved = reader["IsDayApproved"] != DBNull.Value && Convert.ToBoolean(reader["IsDayApproved"])
            };
        }
    }
}
