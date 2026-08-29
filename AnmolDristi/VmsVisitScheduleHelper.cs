using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;

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

    public class PlantDayTiming
    {
        public string DayOfWeek { get; set; }
        public TimeSpan? OpenTime { get; set; }
        public TimeSpan? CloseTime { get; set; }
    }

    public class PlantHourWindowClassification
    {
        public bool ArrivalBeforeOpen { get; set; }
        public bool ArrivalAfterClose { get; set; }
        public bool DepartureAfterClose { get; set; }
        public bool PlantClosed { get; set; }
        public TimeSpan? OpenTime { get; set; }
        public TimeSpan? CloseTime { get; set; }

        public bool HasViolation
        {
            get
            {
                return PlantClosed || ArrivalBeforeOpen || ArrivalAfterClose || DepartureAfterClose;
            }
        }

        public bool HasBothArrivalAndDepartureViolation
        {
            get
            {
                return (ArrivalBeforeOpen || ArrivalAfterClose) && DepartureAfterClose;
            }
        }
    }

    public class VisitDayHoursViolation
    {
        public DateTime VisitDate { get; set; }
        public PlantHourWindowClassification Classification { get; set; }
        public TimeSpan Arrival { get; set; }
        public TimeSpan Departure { get; set; }
    }

    public class VisitHoursEvaluation
    {
        public bool IsCurrentAfterHours { get; set; }
        public bool IsScheduledAfterHours { get; set; }
        public List<DateTime> AffectedDates { get; set; }
        public string CurrentViolation { get; set; }
        public bool ArrivalViolation { get; set; }
        public bool DepartureViolation { get; set; }
        public List<string> ViolationMessages { get; set; }
        public List<VisitDayHoursViolation> DayViolations { get; set; }
        public string WarningMessage { get; set; }
        public string HostWarningMessage { get; set; }
        public TimeSpan? PlantOpenTime { get; set; }
        public TimeSpan? PlantCloseTime { get; set; }
        public DateTime? FirstAffectedDate { get; set; }

        public VisitHoursEvaluation()
        {
            AffectedDates = new List<DateTime>();
            ViolationMessages = new List<string>();
            DayViolations = new List<VisitDayHoursViolation>();
            CurrentViolation = string.Empty;
        }

        public bool RequiresNotice
        {
            get { return IsCurrentAfterHours || IsScheduledAfterHours; }
        }
    }

    public static class VmsVisitScheduleHelper
    {
        public const int MaxScheduleDays = 30;
        public const string AfterHoursVisitorNotice =
            "Notice: Your visit falls outside the organization's normal working hours. Your host may still approve this request as an exception.";
        public const string AfterHoursHostWarning =
            "Warning: This visit falls outside configured plant working hours. Approving this request will be recorded as an after-hours approval.";

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
        /// Also appends VMS_VisitAttendanceHistory: auto-closes any open cycle, then inserts a new open row.
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

            if (visitRows != 1)
                return false;

            WriteCheckInHistory(con, trans, dayLogId, logId, guardId);
            return true;
        }

        /// <summary>
        /// Day-aware check-out. Returns true only when both DayLog and VisitLog
        /// transitioned from Checked-In (rowsAffected == 1 each).
        /// DayLogID must belong to the same LogID (AR-VMS-08A).
        /// Requires today's approved DayLog and Parent ApprovalStatus='Approved' (AR-VMS-10).
        /// Closes the open VMS_VisitAttendanceHistory row for this DayLog; does not insert a second row.
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

            if (visitRows != 1)
                return false;

            CloseOpenAttendanceHistory(con, trans, dayLogId);
            return true;
        }

        public static Dictionary<string, PlantDayTiming> LoadPlantTimings(
            int branchId,
            SqlConnection con = null,
            SqlTransaction trans = null)
        {
            var timings = new Dictionary<string, PlantDayTiming>(StringComparer.OrdinalIgnoreCase);
            bool ownsConnection = con == null;
            if (ownsConnection)
                con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString);

            try
            {
                if (ownsConnection)
                    con.Open();

                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT DayOfWeek, OpenTime, CloseTime
                    FROM VMS_PlantTimings
                    WHERE Branch_ID = @BranchID", con, trans))
                {
                    cmd.Parameters.AddWithValue("@BranchID", branchId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string day = reader["DayOfWeek"] == DBNull.Value
                                ? string.Empty
                                : reader["DayOfWeek"].ToString();
                            if (string.IsNullOrWhiteSpace(day))
                                continue;

                            timings[day.Trim()] = new PlantDayTiming
                            {
                                DayOfWeek = day.Trim(),
                                OpenTime = reader["OpenTime"] == DBNull.Value ? (TimeSpan?)null : (TimeSpan)reader["OpenTime"],
                                CloseTime = reader["CloseTime"] == DBNull.Value ? (TimeSpan?)null : (TimeSpan)reader["CloseTime"]
                            };
                        }
                    }
                }
            }
            finally
            {
                if (ownsConnection)
                    con.Dispose();
            }

            return timings;
        }

        public static string GetPlantTimingsJson(int branchId)
        {
            Dictionary<string, PlantDayTiming> timings = LoadPlantTimings(branchId);
            var sb = new StringBuilder();
            sb.Append("{");
            bool first = true;
            foreach (KeyValuePair<string, PlantDayTiming> pair in timings)
            {
                if (!first)
                    sb.Append(",");
                first = false;
                sb.Append("\"").Append(JsonEscape(pair.Key)).Append("\":{");
                sb.Append("\"open\":");
                sb.Append(pair.Value.OpenTime.HasValue
                    ? "\"" + pair.Value.OpenTime.Value.ToString(@"hh\:mm", CultureInfo.InvariantCulture) + "\""
                    : "null");
                sb.Append(",\"close\":");
                sb.Append(pair.Value.CloseTime.HasValue
                    ? "\"" + pair.Value.CloseTime.Value.ToString(@"hh\:mm", CultureInfo.InvariantCulture) + "\""
                    : "null");
                sb.Append("}");
            }
            sb.Append("}");
            return sb.ToString();
        }

        /// <summary>
        /// Evaluates current time and the requested/approved visit window against VMS_PlantTimings.
        /// Missing plant-timing rows are treated as open (same as IsFacilityOpen).
        /// A day with a row but null Open and Close is treated as closed.
        /// </summary>
        public static VisitHoursEvaluation EvaluateVisitHours(
            int branchId,
            DateTime now,
            DateTime fromDate,
            DateTime toDate,
            TimeSpan timeFrom,
            TimeSpan timeTo,
            SqlConnection con = null,
            SqlTransaction trans = null)
        {
            Dictionary<string, PlantDayTiming> timings = LoadPlantTimings(branchId, con, trans);
            var result = new VisitHoursEvaluation();

            PlantDayTiming todayTiming;
            if (timings.TryGetValue(now.ToString("dddd", CultureInfo.InvariantCulture), out todayTiming))
            {
                result.PlantOpenTime = todayTiming.OpenTime;
                result.PlantCloseTime = todayTiming.CloseTime;
                result.CurrentViolation = DescribeCurrentTimeViolation(now.TimeOfDay, todayTiming);
                result.IsCurrentAfterHours = !string.IsNullOrEmpty(result.CurrentViolation);
            }
            else
            {
                result.IsCurrentAfterHours = false;
            }

            DateTime start = fromDate.Date;
            DateTime end = toDate.Date;
            if (end < start)
                end = start;

            for (DateTime day = start; day <= end; day = day.AddDays(1))
            {
                string weekday = day.ToString("dddd", CultureInfo.InvariantCulture);
                PlantDayTiming dayTiming;
                if (!timings.TryGetValue(weekday, out dayTiming))
                    continue;

                PlantHourWindowClassification classification = ClassifyPlantHourWindow(timeFrom, timeTo, dayTiming);
                if (!classification.HasViolation)
                    continue;

                result.IsScheduledAfterHours = true;
                result.AffectedDates.Add(day.Date);
                result.DayViolations.Add(new VisitDayHoursViolation
                {
                    VisitDate = day.Date,
                    Classification = classification,
                    Arrival = timeFrom,
                    Departure = timeTo
                });
                if (classification.ArrivalBeforeOpen || classification.ArrivalAfterClose)
                    result.ArrivalViolation = true;
                if (classification.DepartureAfterClose)
                    result.DepartureViolation = true;
                if (!result.FirstAffectedDate.HasValue)
                {
                    result.FirstAffectedDate = day.Date;
                    result.PlantOpenTime = dayTiming.OpenTime;
                    result.PlantCloseTime = dayTiming.CloseTime;
                }
            }

            result.ViolationMessages = BuildViolationMessages(result);
            result.WarningMessage = BuildVisitorHoursNotice(result);
            result.HostWarningMessage = BuildHostHoursWarning(result);
            return result;
        }

        public static VisitHoursEvaluation EvaluateVisitHoursForLog(
            SqlConnection con,
            int logId,
            TimeSpan? approvedFromTime = null,
            TimeSpan? approvedToTime = null,
            IList<DateTime> selectedDates = null,
            SqlTransaction trans = null)
        {
            int branchId = 0;
            DateTime fromDate = DateTime.Now.Date;
            DateTime toDate = DateTime.Now.Date;
            TimeSpan timeFrom = new TimeSpan(9, 0, 0);
            TimeSpan timeTo = new TimeSpan(18, 0, 0);

            using (SqlCommand cmd = new SqlCommand(@"
                SELECT ISNULL(TargetBranchID, 0) AS TargetBranchID,
                       RequestedValidFrom, RequestedValidUpto,
                       ValidFrom, ValidUpto,
                       DailyTimeFrom, DailyTimeTo
                FROM VMS_VisitLog
                WHERE LogID = @LogID", con, trans))
            {
                cmd.Parameters.AddWithValue("@LogID", logId);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        branchId = Convert.ToInt32(reader["TargetBranchID"]);
                        DateTime? requestedFrom = reader["RequestedValidFrom"] == DBNull.Value
                            ? (DateTime?)null : Convert.ToDateTime(reader["RequestedValidFrom"]);
                        DateTime? requestedTo = reader["RequestedValidUpto"] == DBNull.Value
                            ? (DateTime?)null : Convert.ToDateTime(reader["RequestedValidUpto"]);
                        DateTime? validFrom = reader["ValidFrom"] == DBNull.Value
                            ? (DateTime?)null : Convert.ToDateTime(reader["ValidFrom"]);
                        DateTime? validTo = reader["ValidUpto"] == DBNull.Value
                            ? (DateTime?)null : Convert.ToDateTime(reader["ValidUpto"]);

                        DateTime start = (requestedFrom ?? validFrom ?? DateTime.Now).Date;
                        DateTime end = (requestedTo ?? validTo ?? start).Date;
                        fromDate = start;
                        toDate = end;

                        if (reader["DailyTimeFrom"] != DBNull.Value)
                            timeFrom = (TimeSpan)reader["DailyTimeFrom"];
                        else if (requestedFrom.HasValue)
                            timeFrom = requestedFrom.Value.TimeOfDay;
                        else if (validFrom.HasValue)
                            timeFrom = validFrom.Value.TimeOfDay;

                        if (reader["DailyTimeTo"] != DBNull.Value)
                            timeTo = (TimeSpan)reader["DailyTimeTo"];
                        else if (requestedTo.HasValue)
                            timeTo = requestedTo.Value.TimeOfDay;
                        else if (validTo.HasValue)
                            timeTo = validTo.Value.TimeOfDay;
                    }
                }
            }

            if (approvedFromTime.HasValue)
                timeFrom = approvedFromTime.Value;
            if (approvedToTime.HasValue)
                timeTo = approvedToTime.Value;

            if (selectedDates != null && selectedDates.Count > 0)
            {
                fromDate = selectedDates[0].Date;
                toDate = selectedDates[0].Date;
                foreach (DateTime d in selectedDates)
                {
                    if (d.Date < fromDate)
                        fromDate = d.Date;
                    if (d.Date > toDate)
                        toDate = d.Date;
                }

                var eval = EvaluateVisitHours(branchId, DateTime.Now, fromDate, toDate, timeFrom, timeTo, con, trans);
                eval.DayViolations.RemoveAll(v =>
                {
                    foreach (DateTime selected in selectedDates)
                    {
                        if (selected.Date == v.VisitDate.Date)
                            return false;
                    }
                    return true;
                });
                eval.AffectedDates.RemoveAll(d =>
                {
                    foreach (DateTime selected in selectedDates)
                    {
                        if (selected.Date == d.Date)
                            return false;
                    }
                    return true;
                });
                eval.IsScheduledAfterHours = eval.DayViolations.Count > 0;
                eval.ArrivalViolation = false;
                eval.DepartureViolation = false;
                foreach (VisitDayHoursViolation dayViolation in eval.DayViolations)
                {
                    if (dayViolation.Classification == null)
                        continue;
                    if (dayViolation.Classification.ArrivalBeforeOpen || dayViolation.Classification.ArrivalAfterClose)
                        eval.ArrivalViolation = true;
                    if (dayViolation.Classification.DepartureAfterClose)
                        eval.DepartureViolation = true;
                }
                eval.ViolationMessages = BuildViolationMessages(eval);
                eval.WarningMessage = BuildVisitorHoursNotice(eval);
                eval.HostWarningMessage = BuildHostHoursWarning(eval);
                return eval;
            }

            return EvaluateVisitHours(branchId, DateTime.Now, fromDate, toDate, timeFrom, timeTo, con, trans);
        }

        public static string FormatScheduleSummary(VisitScheduleRequest schedule)
        {
            if (schedule.ScheduleType == "MultiDay")
            {
                return $"{schedule.FromDate:dd MMM yyyy} to {schedule.ToDate:dd MMM yyyy}, daily {schedule.TimeFrom:hh\\:mm} - {schedule.TimeTo:hh\\:mm}";
            }

            return $"{schedule.FromDate:dd MMM yyyy}, {schedule.TimeFrom:hh\\:mm} - {schedule.TimeTo:hh\\:mm}";
        }

        public static PlantHourWindowClassification ClassifyPlantHourWindow(
            TimeSpan arrival,
            TimeSpan departure,
            PlantDayTiming timing)
        {
            var classification = new PlantHourWindowClassification();
            if (timing == null)
                return classification;

            classification.OpenTime = timing.OpenTime;
            classification.CloseTime = timing.CloseTime;

            if (!timing.OpenTime.HasValue && !timing.CloseTime.HasValue)
            {
                classification.PlantClosed = true;
                return classification;
            }

            if (timing.OpenTime.HasValue && arrival < timing.OpenTime.Value)
                classification.ArrivalBeforeOpen = true;

            if (timing.CloseTime.HasValue)
            {
                if (arrival >= timing.CloseTime.Value)
                    classification.ArrivalAfterClose = true;
                if (departure > timing.CloseTime.Value)
                    classification.DepartureAfterClose = true;
            }

            return classification;
        }

        public static bool IsTimeOutsidePlantHours(TimeSpan windowStart, TimeSpan windowEnd, PlantDayTiming timing)
        {
            return ClassifyPlantHourWindow(windowStart, windowEnd, timing).HasViolation;
        }

        public static string FormatClockTime(TimeSpan time)
        {
            DateTime clock = DateTime.Today.Add(time);
            return clock.ToString("hh:mm tt", CultureInfo.InvariantCulture);
        }

        public static string DescribeCurrentTimeViolation(TimeSpan now, PlantDayTiming timing)
        {
            if (timing == null)
                return string.Empty;

            if (!timing.OpenTime.HasValue && !timing.CloseTime.HasValue)
                return "Current registration is outside today's working hours. The plant is closed today.";

            if (timing.OpenTime.HasValue && now < timing.OpenTime.Value)
            {
                return "Current registration is outside today's working hours. The plant opens at " +
                    FormatClockTime(timing.OpenTime.Value) + ".";
            }

            if (timing.CloseTime.HasValue && now >= timing.CloseTime.Value)
            {
                return "Current registration is outside today's working hours. The plant closed at " +
                    FormatClockTime(timing.CloseTime.Value) + ".";
            }

            return string.Empty;
        }

        public static List<string> BuildDayViolationLines(VisitDayHoursViolation dayViolation)
        {
            var lines = new List<string>();
            if (dayViolation == null || dayViolation.Classification == null)
                return lines;

            PlantHourWindowClassification c = dayViolation.Classification;
            if (c.PlantClosed)
            {
                lines.Add("The plant is closed this day.");
                return lines;
            }

            if (c.ArrivalBeforeOpen && c.OpenTime.HasValue)
            {
                lines.Add("Arrival time (" + FormatClockTime(dayViolation.Arrival) +
                    ") is before plant opening (" + FormatClockTime(c.OpenTime.Value) + ").");
            }

            if (c.ArrivalAfterClose && c.CloseTime.HasValue)
            {
                lines.Add("Arrival time (" + FormatClockTime(dayViolation.Arrival) +
                    ") is after plant closing (" + FormatClockTime(c.CloseTime.Value) + ").");
            }

            if (c.DepartureAfterClose && c.CloseTime.HasValue)
            {
                lines.Add("Departure time (" + FormatClockTime(dayViolation.Departure) +
                    ") is after plant closing (" + FormatClockTime(c.CloseTime.Value) + ").");
            }

            return lines;
        }

        private static List<string> BuildViolationMessages(VisitHoursEvaluation evaluation)
        {
            var messages = new List<string>();
            if (evaluation == null)
                return messages;

            if (!string.IsNullOrEmpty(evaluation.CurrentViolation))
                messages.Add(evaluation.CurrentViolation);

            if (evaluation.DayViolations != null)
            {
                foreach (VisitDayHoursViolation dayViolation in evaluation.DayViolations)
                {
                    foreach (string line in BuildDayViolationLines(dayViolation))
                        messages.Add(line);
                }
            }

            return messages;
        }

        private static void AppendDayViolationDetails(
            StringBuilder sb,
            VisitHoursEvaluation evaluation,
            bool leadingBlankLine = true)
        {
            if (evaluation.DayViolations == null)
                return;

            bool first = true;
            foreach (VisitDayHoursViolation dayViolation in evaluation.DayViolations)
            {
                if (first && !leadingBlankLine)
                    sb.Append("\n");
                else
                    sb.Append("\n\n");
                first = false;

                sb.Append(dayViolation.VisitDate.ToString("dddd dd MMM", CultureInfo.InvariantCulture));
                foreach (string line in BuildDayViolationLines(dayViolation))
                {
                    sb.Append("\n• ");
                    sb.Append(line);
                }
            }
        }

        public static string BuildVisitorHoursNotice(VisitHoursEvaluation evaluation)
        {
            if (evaluation == null || !evaluation.RequiresNotice)
                return string.Empty;

            var sb = new StringBuilder();
            sb.Append("Notice: Your visit falls outside the organization's normal working hours.");
            sb.Append("\n\nYour host may still approve this request as an exception.");

            if (!string.IsNullOrEmpty(evaluation.CurrentViolation))
            {
                sb.Append("\n\n");
                sb.Append(evaluation.CurrentViolation);
            }

            AppendDayViolationDetails(sb, evaluation);
            return sb.ToString();
        }

        public static string BuildHostHoursWarning(VisitHoursEvaluation evaluation)
        {
            if (evaluation == null || !evaluation.RequiresNotice)
                return string.Empty;

            var sb = new StringBuilder();
            sb.Append("Warning:");

            if (!string.IsNullOrEmpty(evaluation.CurrentViolation))
            {
                sb.Append("\n");
                sb.Append(evaluation.CurrentViolation);
            }

            AppendDayViolationDetails(sb, evaluation, !string.IsNullOrEmpty(evaluation.CurrentViolation));

            sb.Append("\n\nApproving this visit will be recorded as an after-hours approval.");
            return sb.ToString();
        }

        private static string JsonEscape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
        }

        private static string ResolveAttendanceSource(string actor)
        {
            string value = (actor ?? string.Empty).Trim();
            if (string.Equals(value, "HostManual", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Host", StringComparison.OrdinalIgnoreCase))
                return "Host";
            if (value.IndexOf("Reception", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Reception";
            return "Gate";
        }

        private static void WriteCheckInHistory(
            SqlConnection con,
            SqlTransaction trans,
            int dayLogId,
            int logId,
            string actor)
        {
            CloseOpenAttendanceHistory(con, trans, dayLogId);

            using (SqlCommand cmd = new SqlCommand(@"
                IF OBJECT_ID('dbo.VMS_VisitAttendanceHistory', 'U') IS NOT NULL
                BEGIN
                    INSERT INTO VMS_VisitAttendanceHistory
                        (LogID, DayLogID, CheckInTime, CheckOutTime, Source, Actor, CreatedOn)
                    VALUES
                        (@LogID, @DayLogID, GETDATE(), NULL, @Source, @Actor, GETDATE())
                END", con, trans))
            {
                cmd.Parameters.AddWithValue("@LogID", logId);
                cmd.Parameters.AddWithValue("@DayLogID", dayLogId);
                cmd.Parameters.AddWithValue("@Source", ResolveAttendanceSource(actor));
                cmd.Parameters.AddWithValue("@Actor", actor ?? "SecurityGate");
                cmd.ExecuteNonQuery();
            }
        }

        private static void CloseOpenAttendanceHistory(
            SqlConnection con,
            SqlTransaction trans,
            int dayLogId)
        {
            using (SqlCommand cmd = new SqlCommand(@"
                IF OBJECT_ID('dbo.VMS_VisitAttendanceHistory', 'U') IS NOT NULL
                BEGIN
                    UPDATE VMS_VisitAttendanceHistory
                    SET CheckOutTime = GETDATE()
                    WHERE DayLogID = @DayLogID
                      AND CheckOutTime IS NULL
                END", con, trans))
            {
                cmd.Parameters.AddWithValue("@DayLogID", dayLogId);
                cmd.ExecuteNonQuery();
            }
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
