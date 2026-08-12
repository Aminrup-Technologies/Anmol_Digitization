using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace AnmolDristi.DAL
{
    public enum ApprovalFailureReason
    {
        None = 0,
        NotFoundOrNotOwned = 1,
        NotPending = 2,
        Unexpected = 3
    }

    /// <summary>
    /// WHO may approve — separate from the VisitLog+DayLog lifecycle WHAT.
    /// HostOwnership: Pending + HostEmployeeCode.
    /// PendingOnly: Pending (Admin/Receptionist page authority; no host match).
    /// </summary>
    public enum ApprovalAuthorizationMode
    {
        HostOwnership = 0,
        PendingOnly = 1
    }

    public class SelectedVisitDay
    {
        public DateTime VisitDate { get; set; }
        public bool IsApproved { get; set; }
    }

    public class VisitorApprovalRequest
    {
        public int LogId { get; set; }
        public string HostEmployeeCode { get; set; }
        public IList<SelectedVisitDay> SelectedDays { get; set; }
        public TimeSpan? ApprovedFromTime { get; set; }
        public TimeSpan? ApprovedToTime { get; set; }

        /// <summary>Optional remark appended to ApprovalRemarks (Admin/Receptionist).</summary>
        public string ApprovalRemarksSuffix { get; set; }

        /// <summary>When set, used instead of GETDATE() for ApprovedOn (Admin schedDate).</summary>
        public DateTime? ApprovedOnOverride { get; set; }

        /// <summary>Queue VMS_ADMIN_ACTION to the visit host after successful approval.</summary>
        public bool QueueHostOnBehalfNotification { get; set; }

        public string OnBehalfHostEmail { get; set; }
        public string OnBehalfHostName { get; set; }
    }

    public class VisitorApprovalResult
    {
        public bool Succeeded { get; set; }
        public ApprovalFailureReason FailureReason { get; set; }
        public int LogId { get; set; }
        public Guid? QrToken { get; set; }
        public DateTime? ValidUpto { get; set; }
    }

    /// <summary>
    /// Controlled failure reasons for Receptionist desk Check-In/Out (AR-VMS-03B).
    /// </summary>
    public enum ReceptionistAttendanceFailureReason
    {
        None = 0,
        /// <summary>Expected VisitStatus/DayLog status did not match (stale or concurrent mutation).</summary>
        StateChanged = 1,
        /// <summary>Today's VisitDayLog exists but IsDayApproved = 0 — normal CI/CO must fail.</summary>
        TodayNotApproved = 2,
        Unexpected = 3
    }

    public class ReceptionistAttendanceResult
    {
        public bool Succeeded { get; set; }
        public ReceptionistAttendanceFailureReason FailureReason { get; set; }
        public int LogId { get; set; }
    }

    /// <summary>
    /// Controlled failure reasons for Admin Inline Gate Override timestamp correction (AR-VMS-04).
    /// </summary>
    public enum AdminCorrectionFailureReason
    {
        None = 0,
        /// <summary>Expected VisitStatus/DayLog status did not match (stale or concurrent mutation).</summary>
        StateChanged = 1,
        /// <summary>Today's VisitDayLog exists but IsDayApproved = 0 — correction must fail closed.</summary>
        UnapprovedToday = 2,
        UnexpectedFailure = 3
    }

    public class AdminCorrectionResult
    {
        public bool Succeeded { get; set; }
        public AdminCorrectionFailureReason FailureReason { get; set; }
        public int LogId { get; set; }
    }

    public class DigitalPassDetails
    {
        public int LogId { get; set; }
        public string VisitorName { get; set; }
        public string VisitorCompany { get; set; }
        public string PhotoPath { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidUpto { get; set; }
        public string ApprovalStatus { get; set; }
        public string ScheduleType { get; set; }
        public string HostName { get; set; }
        public string PlantAlias { get; set; }
        public string PolicyText { get; set; }
    }

    /// <summary>
    /// Persistence access for VMS host My Visitors operations.
    /// </summary>
    public class VmsVisitorManagementRepository
    {
        private readonly string _connectionString;

        public VmsVisitorManagementRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        }

        public DataTable GetPendingVisits(string empCode)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string pendingQuery =
                    "SELECT * FROM VMS_VisitLog WHERE HostEmployeeCode = @EmpCode AND ApprovalStatus = 'Pending' ORDER BY CreatedDate DESC";

                using (SqlDataAdapter sda = new SqlDataAdapter(pendingQuery, con))
                {
                    sda.SelectCommand.Parameters.AddWithValue("@EmpCode", empCode);
                    DataTable dtPending = new DataTable();
                    sda.Fill(dtPending);
                    return dtPending;
                }
            }
        }

        public DataTable GetHostVisitHistory(string empCode)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string historyQuery =
                    "SELECT TOP 50 * FROM VMS_VisitLog WHERE HostEmployeeCode = @EmpCode AND ApprovalStatus IN ('Approved', 'Rejected') ORDER BY CreatedDate DESC";

                using (SqlDataAdapter sda = new SqlDataAdapter(historyQuery, con))
                {
                    sda.SelectCommand.Parameters.AddWithValue("@EmpCode", empCode);
                    DataTable dtHistory = new DataTable();
                    sda.Fill(dtHistory);
                    return dtHistory;
                }
            }
        }

        /// <summary>
        /// Reject a pending visit (AR-VMS-06B). Guarded: ApprovalStatus must be Pending.
        /// Atomically cascades all associated VisitDayLog rows to Cancelled / IsDayApproved=0.
        /// Does not delete DayLog rows. No notifications.
        /// </summary>
        /// <returns>True only when the Parent Pending→Rejected update affected exactly one row.</returns>
        public bool RejectVisitor(int logId)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (SqlTransaction trans = con.BeginTransaction())
                {
                    try
                    {
                        int parentRows;
                        using (SqlCommand cmd = new SqlCommand(@"
                            UPDATE VMS_VisitLog
                            SET ApprovalStatus = 'Rejected',
                                VisitStatus = 'Rejected',
                                ApprovedOn = GETDATE()
                            WHERE LogID = @LogID
                              AND ApprovalStatus = 'Pending'", con, trans))
                        {
                            cmd.Parameters.AddWithValue("@LogID", logId);
                            parentRows = cmd.ExecuteNonQuery();
                        }

                        if (parentRows != 1)
                        {
                            try { trans.Rollback(); } catch { }
                            return false;
                        }

                        CancelVisitDayLogs(con, logId, trans);
                        trans.Commit();
                        return true;
                    }
                    catch
                    {
                        try { trans.Rollback(); } catch { }
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Admin soft-delete of a pending duplicate/invalid request (AR-VMS-06B).
        /// Guarded: ApprovalStatus must be Pending. Cascades all DayLogs to Cancelled /
        /// IsDayApproved=0. Preserves ApprovalRemarks append (LEFT 250). No notifications.
        /// </summary>
        /// <returns>True only when the Parent Pending→Deleted update affected exactly one row.</returns>
        public bool SoftDeleteDuplicate(int logId, string adminName)
        {
            string resolvedAdmin = string.IsNullOrWhiteSpace(adminName) ? "Admin" : adminName.Trim();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (SqlTransaction trans = con.BeginTransaction())
                {
                    try
                    {
                        int parentRows;
                        using (SqlCommand cmd = new SqlCommand(@"
                            UPDATE VMS_VisitLog
                            SET ApprovalStatus = 'Deleted',
                                VisitStatus = 'Cancelled',
                                ApprovalRemarks = LEFT(ISNULL(ApprovalRemarks, '') + ' | Deleted by ' + @AdminName, 250)
                            WHERE LogID = @LogId
                              AND ApprovalStatus = 'Pending'", con, trans))
                        {
                            cmd.Parameters.AddWithValue("@LogId", logId);
                            cmd.Parameters.AddWithValue("@AdminName", resolvedAdmin);
                            parentRows = cmd.ExecuteNonQuery();
                        }

                        if (parentRows != 1)
                        {
                            try { trans.Rollback(); } catch { }
                            return false;
                        }

                        CancelVisitDayLogs(con, logId, trans);
                        trans.Commit();
                        return true;
                    }
                    catch
                    {
                        try { trans.Rollback(); } catch { }
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Cancel all VisitDayLog rows for a visit (AR-VMS-06B). Retains rows; sets
        /// IsDayApproved=0 and VisitStatus=Cancelled. Zero rows affected is OK (legacy).
        /// </summary>
        private static void CancelVisitDayLogs(SqlConnection con, int logId, SqlTransaction trans)
        {
            using (SqlCommand cmd = new SqlCommand(@"
                UPDATE VMS_VisitDayLog
                SET IsDayApproved = 0,
                    VisitStatus = 'Cancelled'
                WHERE LogID = @LogID", con, trans))
            {
                cmd.Parameters.AddWithValue("@LogID", logId);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Host convenience Check-In (AR-VMS-03A).
        /// When an approved today VisitDayLog exists: reuse CheckInDay (Parent+DayLog, guarded).
        /// When no today DayLog row exists: legacy guarded parent-only VisitLog update.
        /// Does not create DayLogs, enforce Gate validity, or queue arrival notifications.
        /// </summary>
        /// <returns>True only when the guarded mutation succeeded (rowsAffected / CheckInDay).</returns>
        public bool ManualCheckIn(int logId)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (SqlTransaction trans = con.BeginTransaction())
                {
                    try
                    {
                        VisitDayLogInfo todayLog = VmsVisitScheduleHelper.GetTodayDayLog(con, logId, trans);

                        // Applicable = today's DayLog row exists and is host-approved for the day.
                        // Missing row → legacy parent-only. Unapproved/cancelled today → not DayLog-aware
                        // (do not fabricate/approve a day); fall through to legacy only when no row.
                        bool useDayLogPath = todayLog != null && todayLog.DayLogId > 0 && todayLog.IsDayApproved;

                        bool succeeded;
                        if (useDayLogPath)
                        {
                            // Reuse canonical CheckInDay — do not duplicate status-guard SQL.
                            succeeded = VmsVisitScheduleHelper.CheckInDay(
                                con,
                                todayLog.DayLogId,
                                logId,
                                "HostManual",
                                trans);
                        }
                        else if (todayLog != null && todayLog.DayLogId > 0 && !todayLog.IsDayApproved)
                        {
                            // Today exists but not approved (or not selectable): do not parent-only
                            // desync; controlled failure.
                            succeeded = false;
                        }
                        else
                        {
                            // Legacy niche: VisitLog with no VisitDayLog for today.
                            succeeded = LegacyManualCheckInParentOnly(con, trans, logId);
                        }

                        if (!succeeded)
                        {
                            try { trans.Rollback(); } catch { }
                            return false;
                        }

                        trans.Commit();
                        return true;
                    }
                    catch
                    {
                        try { trans.Rollback(); } catch { }
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Host convenience Check-Out (AR-VMS-03A).
        /// When an approved today VisitDayLog exists: reuse CheckOutDay (MultiDay → Parent Scheduled
        /// when future approved days remain). Otherwise legacy guarded parent-only Check-Out.
        /// No departure notifications.
        /// </summary>
        public bool ManualCheckOut(int logId)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (SqlTransaction trans = con.BeginTransaction())
                {
                    try
                    {
                        VisitDayLogInfo todayLog = VmsVisitScheduleHelper.GetTodayDayLog(con, logId, trans);

                        bool useDayLogPath = todayLog != null && todayLog.DayLogId > 0 && todayLog.IsDayApproved;

                        bool succeeded;
                        if (useDayLogPath)
                        {
                            succeeded = VmsVisitScheduleHelper.CheckOutDay(
                                con,
                                todayLog.DayLogId,
                                logId,
                                "HostManual",
                                trans);
                        }
                        else if (todayLog != null && todayLog.DayLogId > 0 && !todayLog.IsDayApproved)
                        {
                            succeeded = false;
                        }
                        else
                        {
                            succeeded = LegacyManualCheckOutParentOnly(con, trans, logId);
                        }

                        if (!succeeded)
                        {
                            try { trans.Rollback(); } catch { }
                            return false;
                        }

                        trans.Commit();
                        return true;
                    }
                    catch
                    {
                        try { trans.Rollback(); } catch { }
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Legacy parent-only Check-In when no today VisitDayLog row exists.
        /// Guarded: VisitStatus must be Scheduled; ActualInTime preserves first stamp.
        /// </summary>
        private static bool LegacyManualCheckInParentOnly(
            SqlConnection con,
            SqlTransaction trans,
            int logId)
        {
            using (SqlCommand cmd = new SqlCommand(@"
                UPDATE VMS_VisitLog
                SET VisitStatus = 'Checked-In',
                    ActualInTime = COALESCE(ActualInTime, GETDATE())
                WHERE LogID = @LogID
                  AND VisitStatus = 'Scheduled'", con, trans))
            {
                cmd.Parameters.AddWithValue("@LogID", logId);
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        /// <summary>
        /// Legacy parent-only Check-Out when no today VisitDayLog row exists.
        /// Guarded: VisitStatus must be Checked-In. Does not set CheckedOutBy (MyVisitors contract).
        /// </summary>
        private static bool LegacyManualCheckOutParentOnly(
            SqlConnection con,
            SqlTransaction trans,
            int logId)
        {
            using (SqlCommand cmd = new SqlCommand(@"
                UPDATE VMS_VisitLog
                SET VisitStatus = 'Checked-Out',
                    ActualOutTime = GETDATE()
                WHERE LogID = @LogID
                  AND VisitStatus = 'Checked-In'", con, trans))
            {
                cmd.Parameters.AddWithValue("@LogID", logId);
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        /// <summary>
        /// Receptionist desk Check-In (AR-VMS-03B). DayLog-aware when today's DayLog is approved;
        /// legacy guarded parent-only when no today row; FAIL when today exists but unapproved.
        /// Preserves [Reception Override] remarks. No Gate validity, branch predicate, or notifications.
        /// </summary>
        public ReceptionistAttendanceResult ReceptionistManualCheckIn(int logId)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (SqlTransaction trans = con.BeginTransaction())
                {
                    try
                    {
                        VisitDayLogInfo todayLog = VmsVisitScheduleHelper.GetTodayDayLog(con, logId, trans);

                        if (todayLog != null && todayLog.DayLogId > 0 && !todayLog.IsDayApproved)
                        {
                            try { trans.Rollback(); } catch { }
                            return FailReceptionistAttendance(logId, ReceptionistAttendanceFailureReason.TodayNotApproved);
                        }

                        bool useDayLogPath = todayLog != null && todayLog.DayLogId > 0 && todayLog.IsDayApproved;
                        bool succeeded;

                        if (useDayLogPath)
                        {
                            succeeded = VmsVisitScheduleHelper.CheckInDay(
                                con,
                                todayLog.DayLogId,
                                logId,
                                "Receptionist",
                                trans);

                            if (succeeded)
                            {
                                // Metadata-only: do not alter VisitStatus / ActualInTime / DayLog status.
                                AppendReceptionOverrideRemark(
                                    con,
                                    trans,
                                    logId,
                                    "[Reception Override] Manual Check-In on " + DateTime.Now.ToString("dd-MMM-yy HH:mm"));
                            }
                        }
                        else
                        {
                            succeeded = LegacyReceptionistCheckInParentOnly(con, trans, logId);
                        }

                        if (!succeeded)
                        {
                            try { trans.Rollback(); } catch { }
                            return FailReceptionistAttendance(logId, ReceptionistAttendanceFailureReason.StateChanged);
                        }

                        trans.Commit();
                        return new ReceptionistAttendanceResult
                        {
                            Succeeded = true,
                            FailureReason = ReceptionistAttendanceFailureReason.None,
                            LogId = logId
                        };
                    }
                    catch
                    {
                        try { trans.Rollback(); } catch { }
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Receptionist desk Check-Out (AR-VMS-03B). Reuses CheckOutDay for MultiDay Parent→Scheduled
        /// when future approved days remain. CheckedOutBy = Receptionist on DayLog path via helper
        /// and on legacy path explicitly. No departure notifications.
        /// </summary>
        public ReceptionistAttendanceResult ReceptionistManualCheckOut(int logId)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (SqlTransaction trans = con.BeginTransaction())
                {
                    try
                    {
                        VisitDayLogInfo todayLog = VmsVisitScheduleHelper.GetTodayDayLog(con, logId, trans);

                        if (todayLog != null && todayLog.DayLogId > 0 && !todayLog.IsDayApproved)
                        {
                            try { trans.Rollback(); } catch { }
                            return FailReceptionistAttendance(logId, ReceptionistAttendanceFailureReason.TodayNotApproved);
                        }

                        bool useDayLogPath = todayLog != null && todayLog.DayLogId > 0 && todayLog.IsDayApproved;
                        bool succeeded;

                        if (useDayLogPath)
                        {
                            succeeded = VmsVisitScheduleHelper.CheckOutDay(
                                con,
                                todayLog.DayLogId,
                                logId,
                                "Receptionist",
                                trans);

                            if (succeeded)
                            {
                                AppendReceptionOverrideRemark(
                                    con,
                                    trans,
                                    logId,
                                    "[Reception Override] Manual Check-Out on " + DateTime.Now.ToString("dd-MMM-yy HH:mm"));
                            }
                        }
                        else
                        {
                            succeeded = LegacyReceptionistCheckOutParentOnly(con, trans, logId);
                        }

                        if (!succeeded)
                        {
                            try { trans.Rollback(); } catch { }
                            return FailReceptionistAttendance(logId, ReceptionistAttendanceFailureReason.StateChanged);
                        }

                        trans.Commit();
                        return new ReceptionistAttendanceResult
                        {
                            Succeeded = true,
                            FailureReason = ReceptionistAttendanceFailureReason.None,
                            LogId = logId
                        };
                    }
                    catch
                    {
                        try { trans.Rollback(); } catch { }
                        throw;
                    }
                }
            }
        }

        private static ReceptionistAttendanceResult FailReceptionistAttendance(
            int logId,
            ReceptionistAttendanceFailureReason reason)
        {
            return new ReceptionistAttendanceResult
            {
                Succeeded = false,
                FailureReason = reason,
                LogId = logId
            };
        }

        /// <summary>
        /// Metadata-only remarks append after a successful canonical DayLog transition.
        /// Does not change VisitStatus, ActualInTime, ActualOutTime, or DayLog status.
        /// </summary>
        private static void AppendReceptionOverrideRemark(
            SqlConnection con,
            SqlTransaction trans,
            int logId,
            string remark)
        {
            using (SqlCommand cmd = new SqlCommand(@"
                UPDATE VMS_VisitLog
                SET ApprovalRemarks = ISNULL(CAST(ApprovalRemarks AS NVARCHAR(MAX)), '') + ' | ' + @Remark
                WHERE LogID = @LogID", con, trans))
            {
                cmd.Parameters.AddWithValue("@LogID", logId);
                cmd.Parameters.AddWithValue("@Remark", remark);
                cmd.ExecuteNonQuery();
            }
        }

        private static bool LegacyReceptionistCheckInParentOnly(
            SqlConnection con,
            SqlTransaction trans,
            int logId)
        {
            string remark =
                "[Reception Override] Manual Check-In on " + DateTime.Now.ToString("dd-MMM-yy HH:mm");

            using (SqlCommand cmd = new SqlCommand(@"
                UPDATE VMS_VisitLog
                SET VisitStatus = 'Checked-In',
                    ActualInTime = COALESCE(ActualInTime, GETDATE()),
                    ApprovalRemarks = ISNULL(CAST(ApprovalRemarks AS NVARCHAR(MAX)), '') + ' | ' + @Remark
                WHERE LogID = @LogID
                  AND VisitStatus = 'Scheduled'", con, trans))
            {
                cmd.Parameters.AddWithValue("@LogID", logId);
                cmd.Parameters.AddWithValue("@Remark", remark);
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        private static bool LegacyReceptionistCheckOutParentOnly(
            SqlConnection con,
            SqlTransaction trans,
            int logId)
        {
            string remark =
                "[Reception Override] Manual Check-Out on " + DateTime.Now.ToString("dd-MMM-yy HH:mm");

            using (SqlCommand cmd = new SqlCommand(@"
                UPDATE VMS_VisitLog
                SET VisitStatus = 'Checked-Out',
                    ActualOutTime = GETDATE(),
                    CheckedOutBy = 'Receptionist',
                    ApprovalRemarks = ISNULL(CAST(ApprovalRemarks AS NVARCHAR(MAX)), '') + ' | ' + @Remark
                WHERE LogID = @LogID
                  AND VisitStatus = 'Checked-In'", con, trans))
            {
                cmd.Parameters.AddWithValue("@LogID", logId);
                cmd.Parameters.AddWithValue("@Remark", remark);
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        /// <summary>
        /// Admin Inline Gate Override — In-Time correction (AR-VMS-04).
        /// Uses the Admin-supplied timestamp (not GETDATE). When today's DayLog is approved,
        /// atomically corrects Parent + DayLog. When no today DayLog exists, guarded parent-only.
        /// When today exists but unapproved: FAIL (no parent fallback). Does not call
        /// CheckInDay / ManualCheckIn. No notifications.
        /// </summary>
        public AdminCorrectionResult AdminCorrectInTime(
            int logId,
            DateTime adminInTime,
            string overrideReason,
            string userName)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (SqlTransaction trans = con.BeginTransaction())
                {
                    try
                    {
                        VisitDayLogInfo todayLog = VmsVisitScheduleHelper.GetTodayDayLog(con, logId, trans);

                        if (todayLog != null && todayLog.DayLogId > 0 && !todayLog.IsDayApproved)
                        {
                            try { trans.Rollback(); } catch { }
                            return FailAdminCorrection(logId, AdminCorrectionFailureReason.UnapprovedToday);
                        }

                        DateTime? oldInTime = ReadParentActualInTime(con, trans, logId);

                        bool useDayLogPath = todayLog != null && todayLog.DayLogId > 0 && todayLog.IsDayApproved;
                        bool mutated;

                        if (useDayLogPath)
                        {
                            mutated = AdminCorrectInTimeWithDayLog(
                                con, trans, logId, todayLog.DayLogId, adminInTime);
                        }
                        else
                        {
                            mutated = AdminCorrectInTimeParentOnly(con, trans, logId, adminInTime);
                        }

                        if (!mutated)
                        {
                            try { trans.Rollback(); } catch { }
                            return FailAdminCorrection(logId, AdminCorrectionFailureReason.StateChanged);
                        }

                        string oldRecord = oldInTime.HasValue
                            ? oldInTime.Value.ToString("dd-MMM HH:mm")
                            : "Empty";
                        string auditName = string.IsNullOrWhiteSpace(userName) ? "Admin" : userName.Trim();
                        string reason = overrideReason ?? string.Empty;
                        string auditTrail =
                            $" | [Check-In Time: {oldRecord}->{adminInTime:dd-MMM HH:mm} by {auditName}] Rsn: {reason}";

                        AppendAdminCorrectionRemark(con, trans, logId, auditTrail);

                        trans.Commit();
                        return new AdminCorrectionResult
                        {
                            Succeeded = true,
                            FailureReason = AdminCorrectionFailureReason.None,
                            LogId = logId
                        };
                    }
                    catch
                    {
                        try { trans.Rollback(); } catch { }
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Admin Inline Gate Override — Out-Time correction (AR-VMS-04).
        /// Uses the Admin-supplied timestamp (not GETDATE). Approved today DayLog: atomic
        /// Parent+DayLog with MultiDay Parent→Scheduled when future approved days remain
        /// (HasFutureApprovedDays). No today DayLog: guarded parent-only Checked-Out.
        /// Unapproved today: FAIL. CheckedOutBy = ADMIN_INLINE. Does not call CheckOutDay.
        /// </summary>
        public AdminCorrectionResult AdminCorrectOutTime(
            int logId,
            DateTime adminOutTime,
            string overrideReason,
            string userName)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (SqlTransaction trans = con.BeginTransaction())
                {
                    try
                    {
                        VisitDayLogInfo todayLog = VmsVisitScheduleHelper.GetTodayDayLog(con, logId, trans);

                        if (todayLog != null && todayLog.DayLogId > 0 && !todayLog.IsDayApproved)
                        {
                            try { trans.Rollback(); } catch { }
                            return FailAdminCorrection(logId, AdminCorrectionFailureReason.UnapprovedToday);
                        }

                        DateTime? oldOutTime = ReadParentActualOutTime(con, trans, logId);

                        bool useDayLogPath = todayLog != null && todayLog.DayLogId > 0 && todayLog.IsDayApproved;
                        bool mutated;

                        if (useDayLogPath)
                        {
                            mutated = AdminCorrectOutTimeWithDayLog(
                                con, trans, logId, todayLog.DayLogId, adminOutTime);
                        }
                        else
                        {
                            mutated = AdminCorrectOutTimeParentOnly(con, trans, logId, adminOutTime);
                        }

                        if (!mutated)
                        {
                            try { trans.Rollback(); } catch { }
                            return FailAdminCorrection(logId, AdminCorrectionFailureReason.StateChanged);
                        }

                        string oldRecord = oldOutTime.HasValue
                            ? oldOutTime.Value.ToString("dd-MMM HH:mm")
                            : "Empty";
                        string auditName = string.IsNullOrWhiteSpace(userName) ? "Admin" : userName.Trim();
                        string reason = overrideReason ?? string.Empty;
                        string auditTrail =
                            $" | [Check-Out Time: {oldRecord}->{adminOutTime:dd-MMM HH:mm} by {auditName}] Rsn: {reason}";

                        AppendAdminCorrectionRemark(con, trans, logId, auditTrail);

                        trans.Commit();
                        return new AdminCorrectionResult
                        {
                            Succeeded = true,
                            FailureReason = AdminCorrectionFailureReason.None,
                            LogId = logId
                        };
                    }
                    catch
                    {
                        try { trans.Rollback(); } catch { }
                        throw;
                    }
                }
            }
        }

        private static AdminCorrectionResult FailAdminCorrection(
            int logId,
            AdminCorrectionFailureReason reason)
        {
            return new AdminCorrectionResult
            {
                Succeeded = false,
                FailureReason = reason,
                LogId = logId
            };
        }

        private static DateTime? ReadParentActualInTime(
            SqlConnection con,
            SqlTransaction trans,
            int logId)
        {
            using (SqlCommand cmd = new SqlCommand(
                "SELECT ActualInTime FROM VMS_VisitLog WHERE LogID = @LogID", con, trans))
            {
                cmd.Parameters.AddWithValue("@LogID", logId);
                object value = cmd.ExecuteScalar();
                if (value == null || value == DBNull.Value)
                    return null;
                return Convert.ToDateTime(value);
            }
        }

        private static DateTime? ReadParentActualOutTime(
            SqlConnection con,
            SqlTransaction trans,
            int logId)
        {
            using (SqlCommand cmd = new SqlCommand(
                "SELECT ActualOutTime FROM VMS_VisitLog WHERE LogID = @LogID", con, trans))
            {
                cmd.Parameters.AddWithValue("@LogID", logId);
                object value = cmd.ExecuteScalar();
                if (value == null || value == DBNull.Value)
                    return null;
                return Convert.ToDateTime(value);
            }
        }

        /// <summary>
        /// Approved today DayLog: atomic Parent + DayLog In-Time with Admin-supplied timestamp.
        /// Both rows must transition Scheduled → Checked-In (rowsAffected == 1 each).
        /// </summary>
        private static bool AdminCorrectInTimeWithDayLog(
            SqlConnection con,
            SqlTransaction trans,
            int logId,
            int dayLogId,
            DateTime adminInTime)
        {
            int dayRows;
            using (SqlCommand cmd = new SqlCommand(@"
                UPDATE VMS_VisitDayLog
                SET ActualInTime = @AdminInTime,
                    VisitStatus = 'Checked-In'
                WHERE DayLogID = @DayLogID
                  AND VisitStatus = 'Scheduled'", con, trans))
            {
                cmd.Parameters.AddWithValue("@AdminInTime", adminInTime);
                cmd.Parameters.AddWithValue("@DayLogID", dayLogId);
                dayRows = cmd.ExecuteNonQuery();
            }

            if (dayRows != 1)
                return false;

            int visitRows;
            using (SqlCommand cmd = new SqlCommand(@"
                UPDATE VMS_VisitLog
                SET ActualInTime = @AdminInTime,
                    VisitStatus = 'Checked-In'
                WHERE LogID = @LogID
                  AND VisitStatus = 'Scheduled'", con, trans))
            {
                cmd.Parameters.AddWithValue("@AdminInTime", adminInTime);
                cmd.Parameters.AddWithValue("@LogID", logId);
                visitRows = cmd.ExecuteNonQuery();
            }

            return visitRows == 1;
        }

        /// <summary>
        /// Legacy niche: no today VisitDayLog row — guarded parent-only In-Time correction.
        /// </summary>
        private static bool AdminCorrectInTimeParentOnly(
            SqlConnection con,
            SqlTransaction trans,
            int logId,
            DateTime adminInTime)
        {
            using (SqlCommand cmd = new SqlCommand(@"
                UPDATE VMS_VisitLog
                SET ActualInTime = @AdminInTime,
                    VisitStatus = 'Checked-In'
                WHERE LogID = @LogID
                  AND VisitStatus = 'Scheduled'", con, trans))
            {
                cmd.Parameters.AddWithValue("@AdminInTime", adminInTime);
                cmd.Parameters.AddWithValue("@LogID", logId);
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        /// <summary>
        /// Approved today DayLog: DayLog → Checked-Out with @AdminOutTime; Parent VisitStatus
        /// Scheduled when HasFutureApprovedDays, else Checked-Out. CheckedOutBy = ADMIN_INLINE.
        /// </summary>
        private static bool AdminCorrectOutTimeWithDayLog(
            SqlConnection con,
            SqlTransaction trans,
            int logId,
            int dayLogId,
            DateTime adminOutTime)
        {
            int dayRows;
            using (SqlCommand cmd = new SqlCommand(@"
                UPDATE VMS_VisitDayLog
                SET ActualOutTime = @AdminOutTime,
                    VisitStatus = 'Checked-Out'
                WHERE DayLogID = @DayLogID
                  AND VisitStatus = 'Checked-In'", con, trans))
            {
                cmd.Parameters.AddWithValue("@AdminOutTime", adminOutTime);
                cmd.Parameters.AddWithValue("@DayLogID", dayLogId);
                dayRows = cmd.ExecuteNonQuery();
            }

            if (dayRows != 1)
                return false;

            bool hasFutureDays = VmsVisitScheduleHelper.HasFutureApprovedDays(
                con, logId, DateTime.Now.Date, trans);
            string nextVisitStatus = hasFutureDays ? "Scheduled" : "Checked-Out";

            int visitRows;
            using (SqlCommand cmd = new SqlCommand(@"
                UPDATE VMS_VisitLog
                SET ActualOutTime = @AdminOutTime,
                    VisitStatus = @VisitStatus,
                    CheckedOutBy = 'ADMIN_INLINE'
                WHERE LogID = @LogID
                  AND VisitStatus = 'Checked-In'", con, trans))
            {
                cmd.Parameters.AddWithValue("@AdminOutTime", adminOutTime);
                cmd.Parameters.AddWithValue("@VisitStatus", nextVisitStatus);
                cmd.Parameters.AddWithValue("@LogID", logId);
                visitRows = cmd.ExecuteNonQuery();
            }

            return visitRows == 1;
        }

        /// <summary>
        /// Legacy niche: no today VisitDayLog row — guarded parent-only Out-Time correction.
        /// </summary>
        private static bool AdminCorrectOutTimeParentOnly(
            SqlConnection con,
            SqlTransaction trans,
            int logId,
            DateTime adminOutTime)
        {
            using (SqlCommand cmd = new SqlCommand(@"
                UPDATE VMS_VisitLog
                SET ActualOutTime = @AdminOutTime,
                    VisitStatus = 'Checked-Out',
                    CheckedOutBy = 'ADMIN_INLINE'
                WHERE LogID = @LogID
                  AND VisitStatus = 'Checked-In'", con, trans))
            {
                cmd.Parameters.AddWithValue("@AdminOutTime", adminOutTime);
                cmd.Parameters.AddWithValue("@LogID", logId);
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        /// <summary>
        /// Append Admin correction audit remark after successful lifecycle mutation (same tx).
        /// Preserves LEFT(..., 250) truncation used by Admin Inline Gate Override.
        /// </summary>
        private static void AppendAdminCorrectionRemark(
            SqlConnection con,
            SqlTransaction trans,
            int logId,
            string auditTrail)
        {
            using (SqlCommand cmd = new SqlCommand(@"
                UPDATE VMS_VisitLog
                SET ApprovalRemarks = LEFT(ISNULL(ApprovalRemarks, '') + @AuditTrail, 250)
                WHERE LogID = @LogID", con, trans))
            {
                cmd.Parameters.AddWithValue("@AuditTrail", auditTrail);
                cmd.Parameters.AddWithValue("@LogID", logId);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Host/MyVisitors approval — Pending + HostEmployeeCode authorization.
        /// Lifecycle owned by <see cref="ApproveVisitCore"/>.
        /// </summary>
        public VisitorApprovalResult ApprovePendingVisit(VisitorApprovalRequest request)
        {
            return ApproveVisitCore(request, ApprovalAuthorizationMode.HostOwnership);
        }

        /// <summary>
        /// Admin on-behalf approval — Pending-only authorization (host ownership bypassed).
        /// Produces the same VisitLog + DayLog + QR + validity + pass-notification lifecycle.
        /// Valid* comes from ApproveVisitSchedule(RequestedValid*); ApprovedOn may use schedDate override.
        /// </summary>
        public VisitorApprovalResult ApproveVisitOnBehalf(VisitorApprovalRequest request)
        {
            return ApproveVisitCore(request, ApprovalAuthorizationMode.PendingOnly);
        }

        /// <summary>
        /// Receptionist force-approve — Pending-only authorization.
        /// Issues QR, DayLog authorization via ApproveVisitSchedule, and visitor pass notification.
        /// </summary>
        public VisitorApprovalResult ApproveVisitByReceptionist(VisitorApprovalRequest request)
        {
            return ApproveVisitCore(request, ApprovalAuthorizationMode.PendingOnly);
        }

        /// <summary>
        /// Shared approval lifecycle (AR-VMS-02):
        /// selected days → QR → guarded VisitLog UPDATE → ApproveVisitSchedule →
        /// re-apply selected days → optional time override → pass notify → optional host on-behalf notify → COMMIT.
        /// </summary>
        private VisitorApprovalResult ApproveVisitCore(
            VisitorApprovalRequest request,
            ApprovalAuthorizationMode authMode)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            string hostEmployeeCode = request.HostEmployeeCode ?? string.Empty;

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (SqlTransaction trans = con.BeginTransaction())
                {
                    try
                    {
                        if (request.SelectedDays != null)
                        {
                            ApplySelectedDayApprovals(con, trans, request.LogId, request.SelectedDays);
                        }

                        Guid qrToken = Guid.NewGuid();

                        int rowsAffected = ExecuteGuardedApprovalUpdate(
                            con,
                            trans,
                            request,
                            authMode,
                            hostEmployeeCode,
                            qrToken);

                        if (rowsAffected != 1)
                        {
                            ApprovalFailureReason reason =
                                ResolveApprovalFailure(con, trans, request.LogId, hostEmployeeCode, authMode);

                            try
                            {
                                trans.Rollback();
                            }
                            catch
                            {
                                // Do not mask the approval failure result.
                            }

                            return new VisitorApprovalResult
                            {
                                Succeeded = false,
                                FailureReason = reason,
                                LogId = request.LogId,
                                QrToken = null
                            };
                        }

                        // Full requested range: SelectedDays null → all RequestedValid* days approved;
                        // out-of-range days cancelled. MultiDay Admin/Receptionist use this path (no day UI).
                        VmsVisitScheduleHelper.ApproveVisitSchedule(con, trans, request.LogId, null, null);

                        // Re-apply selective day flags so range-based schedule approval does not overwrite them
                        if (request.SelectedDays != null)
                        {
                            ApplySelectedDayApprovals(con, trans, request.LogId, request.SelectedDays);
                        }

                        if (request.ApprovedFromTime.HasValue && request.ApprovedToTime.HasValue)
                        {
                            ApplyApprovedValidityTimes(
                                con,
                                trans,
                                request.LogId,
                                request.ApprovedFromTime.Value,
                                request.ApprovedToTime.Value);
                        }

                        string visitorName;
                        string visitorEmail;
                        string visitorMobile;
                        LoadVisitorContact(con, trans, request.LogId, out visitorName, out visitorEmail, out visitorMobile);

                        VmsHelper.QueueVisitorPassNotification(
                            con,
                            request.LogId,
                            qrToken,
                            visitorName,
                            visitorEmail,
                            visitorMobile,
                            trans);

                        if (request.QueueHostOnBehalfNotification)
                        {
                            QueueHostOnBehalfNotification(
                                con,
                                trans,
                                request.LogId,
                                visitorName,
                                request.OnBehalfHostEmail,
                                request.OnBehalfHostName,
                                request.ApprovalRemarksSuffix);
                        }

                        DateTime? validUpto = LoadValidUpto(con, trans, request.LogId);

                        trans.Commit();

                        return new VisitorApprovalResult
                        {
                            Succeeded = true,
                            FailureReason = ApprovalFailureReason.None,
                            LogId = request.LogId,
                            QrToken = qrToken,
                            ValidUpto = validUpto
                        };
                    }
                    catch
                    {
                        try
                        {
                            trans.Rollback();
                        }
                        catch
                        {
                            // Do not mask the original exception.
                        }

                        throw;
                    }
                }
            }
        }

        private static int ExecuteGuardedApprovalUpdate(
            SqlConnection con,
            SqlTransaction trans,
            VisitorApprovalRequest request,
            ApprovalAuthorizationMode authMode,
            string hostEmployeeCode,
            Guid qrToken)
        {
            bool hasRemarks = !string.IsNullOrWhiteSpace(request.ApprovalRemarksSuffix);
            bool useApprovedOnOverride = request.ApprovedOnOverride.HasValue;

            string remarksSql = hasRemarks
                ? ", ApprovalRemarks = LEFT(ISNULL(ApprovalRemarks, '') + ' | ' + @Remarks, 250)"
                : string.Empty;

            string approvedOnSql = useApprovedOnOverride
                ? "ApprovedOn = @ApprovedOn"
                : "ApprovedOn = GETDATE()";

            string ownershipSql = authMode == ApprovalAuthorizationMode.HostOwnership
                ? " AND HostEmployeeCode = @HostEmployeeCode"
                : string.Empty;

            string updateQuery = $@"
                UPDATE VMS_VisitLog
                SET ApprovalStatus = 'Approved',
                    VisitStatus = 'Scheduled',
                    QRToken = @Token,
                    {approvedOnSql}
                    {remarksSql}
                WHERE LogID = @LogID
                  AND ApprovalStatus = 'Pending'
                  {ownershipSql}";

            using (SqlCommand cmd = new SqlCommand(updateQuery, con, trans))
            {
                cmd.Parameters.AddWithValue("@Token", qrToken);
                cmd.Parameters.AddWithValue("@LogID", request.LogId);

                if (useApprovedOnOverride)
                    cmd.Parameters.AddWithValue("@ApprovedOn", request.ApprovedOnOverride.Value);

                if (hasRemarks)
                    cmd.Parameters.AddWithValue("@Remarks", request.ApprovalRemarksSuffix.Trim());

                if (authMode == ApprovalAuthorizationMode.HostOwnership)
                    cmd.Parameters.AddWithValue("@HostEmployeeCode", hostEmployeeCode);

                return cmd.ExecuteNonQuery();
            }
        }

        private static void QueueHostOnBehalfNotification(
            SqlConnection con,
            SqlTransaction trans,
            int logId,
            string visitorName,
            string hostEmail,
            string hostName,
            string remarks)
        {
            if (string.IsNullOrWhiteSpace(hostEmail))
                return;

            string resolvedHostName = string.IsNullOrWhiteSpace(hostName) ? "Host" : hostName;
            string resolvedRemarks = string.IsNullOrWhiteSpace(remarks) ? "(none)" : remarks.Trim();
            string subject = $"FYI: Visitor {visitorName} Approved On-Behalf";
            string body = $@"<div style='padding:20px;'><h3 style='color:#3498DB;'>Admin Action Notification</h3><p>Hello {resolvedHostName},</p><p>Please note that your pending visitor <b>{visitorName}</b> was successfully <b>Approved On-Behalf</b> by the Administration Team.</p><p>Remarks: {resolvedRemarks}</p></div>";

            string qQuery = @"
                INSERT INTO NotificationQueue
                (FormSubmissionId, EscalationLevel, RecipientEmail, RecipientName, Subject, Body, NotificationType, Status, SourceModule)
                VALUES
                (@LogId, 0, @Email, @Name, @Subject, @Body, 'Email', 'Pending', 'VMS_ADMIN_ACTION')";

            using (SqlCommand qCmd = new SqlCommand(qQuery, con, trans))
            {
                qCmd.Parameters.AddWithValue("@LogId", logId);
                qCmd.Parameters.AddWithValue("@Email", hostEmail);
                qCmd.Parameters.AddWithValue("@Name", resolvedHostName);
                qCmd.Parameters.AddWithValue("@Subject", subject);
                qCmd.Parameters.AddWithValue("@Body", body);
                qCmd.ExecuteNonQuery();
            }
        }

        private static DateTime? LoadValidUpto(SqlConnection con, SqlTransaction trans, int logId)
        {
            using (SqlCommand cmd = new SqlCommand(
                "SELECT ValidUpto FROM VMS_VisitLog WHERE LogID = @LogID", con, trans))
            {
                cmd.Parameters.AddWithValue("@LogID", logId);
                object result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                    return null;
                return Convert.ToDateTime(result);
            }
        }

        private static void ApplySelectedDayApprovals(
            SqlConnection con,
            SqlTransaction trans,
            int logId,
            IList<SelectedVisitDay> selectedDays)
        {
            foreach (SelectedVisitDay day in selectedDays)
            {
                using (SqlCommand dayCmd = new SqlCommand(@"
                    UPDATE VMS_VisitDayLog
                    SET IsDayApproved = @IsDayApproved
                    WHERE LogID = @LogID AND VisitDate = @VisitDate", con, trans))
                {
                    dayCmd.Parameters.AddWithValue("@IsDayApproved", day.IsApproved ? 1 : 0);
                    dayCmd.Parameters.AddWithValue("@LogID", logId);
                    dayCmd.Parameters.AddWithValue("@VisitDate", day.VisitDate.Date);
                    dayCmd.ExecuteNonQuery();
                }
            }
        }

        private static void ApplyApprovedValidityTimes(
            SqlConnection con,
            SqlTransaction trans,
            int logId,
            TimeSpan approvedFromTime,
            TimeSpan approvedToTime)
        {
            // Preserve MyVisitors calendar-date behavior; replace only the time portion.
            using (SqlCommand cmd = new SqlCommand(@"
                UPDATE VMS_VisitLog
                SET ValidFrom = DATEADD(MINUTE, @FromMinutes, CAST(CAST(ISNULL(ValidFrom, GETDATE()) AS date) AS datetime)),
                    ValidUpto = DATEADD(MINUTE, @ToMinutes, CAST(CAST(ISNULL(ValidUpto, GETDATE()) AS date) AS datetime)),
                    ApprovedValidFrom = DATEADD(MINUTE, @FromMinutes, CAST(CAST(ISNULL(ApprovedValidFrom, ISNULL(ValidFrom, GETDATE())) AS date) AS datetime)),
                    ApprovedValidUpto = DATEADD(MINUTE, @ToMinutes, CAST(CAST(ISNULL(ApprovedValidUpto, ISNULL(ValidUpto, GETDATE())) AS date) AS datetime))
                WHERE LogID = @LogID", con, trans))
            {
                cmd.Parameters.AddWithValue("@LogID", logId);
                cmd.Parameters.AddWithValue("@FromMinutes", (int)approvedFromTime.TotalMinutes);
                cmd.Parameters.AddWithValue("@ToMinutes", (int)approvedToTime.TotalMinutes);
                cmd.ExecuteNonQuery();
            }

            using (SqlCommand dayCmd = new SqlCommand(@"
                UPDATE VMS_VisitDayLog
                SET DayValidFrom = DATEADD(MINUTE, @FromMinutes, CAST(VisitDate AS datetime)),
                    DayValidUpto = DATEADD(MINUTE, @ToMinutes, CAST(VisitDate AS datetime))
                WHERE LogID = @LogID AND IsDayApproved = 1", con, trans))
            {
                dayCmd.Parameters.AddWithValue("@LogID", logId);
                dayCmd.Parameters.AddWithValue("@FromMinutes", (int)approvedFromTime.TotalMinutes);
                dayCmd.Parameters.AddWithValue("@ToMinutes", (int)approvedToTime.TotalMinutes);
                dayCmd.ExecuteNonQuery();
            }
        }

        private static void LoadVisitorContact(
            SqlConnection con,
            SqlTransaction trans,
            int logId,
            out string visitorName,
            out string visitorEmail,
            out string visitorMobile)
        {
            visitorName = string.Empty;
            visitorEmail = string.Empty;
            visitorMobile = string.Empty;

            using (SqlCommand lookupCmd = new SqlCommand(
                "SELECT VisitorEmail, VisitorMobile, VisitorName FROM VMS_VisitLog WHERE LogID = @LogID",
                con,
                trans))
            {
                lookupCmd.Parameters.AddWithValue("@LogID", logId);
                using (SqlDataReader sdr = lookupCmd.ExecuteReader())
                {
                    if (sdr.Read())
                    {
                        visitorEmail = sdr["VisitorEmail"] == DBNull.Value ? string.Empty : sdr["VisitorEmail"].ToString();
                        visitorMobile = sdr["VisitorMobile"] == DBNull.Value ? string.Empty : sdr["VisitorMobile"].ToString();
                        visitorName = sdr["VisitorName"] == DBNull.Value ? string.Empty : sdr["VisitorName"].ToString();
                    }
                }
            }
        }

        private static ApprovalFailureReason ResolveApprovalFailure(
            SqlConnection con,
            SqlTransaction trans,
            int logId,
            string hostEmployeeCode,
            ApprovalAuthorizationMode authMode)
        {
            using (SqlCommand cmd = new SqlCommand(
                "SELECT HostEmployeeCode, ApprovalStatus FROM VMS_VisitLog WHERE LogID = @LogID",
                con,
                trans))
            {
                cmd.Parameters.AddWithValue("@LogID", logId);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return ApprovalFailureReason.NotFoundOrNotOwned;

                    string rowHost = reader["HostEmployeeCode"] == DBNull.Value
                        ? string.Empty
                        : reader["HostEmployeeCode"].ToString();
                    string status = reader["ApprovalStatus"] == DBNull.Value
                        ? string.Empty
                        : reader["ApprovalStatus"].ToString();

                    if (authMode == ApprovalAuthorizationMode.HostOwnership
                        && !string.Equals(rowHost, hostEmployeeCode, StringComparison.OrdinalIgnoreCase))
                        return ApprovalFailureReason.NotFoundOrNotOwned;

                    if (!string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase))
                        return ApprovalFailureReason.NotPending;

                    return ApprovalFailureReason.Unexpected;
                }
            }
        }

        public DigitalPassDetails GetDigitalPassDetails(Guid token)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT 
                        v.LogID,
                        v.VisitorName, 
                        v.VisitorCompany, 
                        v.PhotoPath, 
                        v.ValidFrom,
                        v.ValidUpto, 
                        v.ApprovalStatus,
                        ISNULL(v.ScheduleType, 'SingleDay') AS ScheduleType,
                        u.EmployeeName AS HostName,
                        ISNULL(s.AliasName, 'Anmol Industries') AS PlantAlias,
                        ISNULL(s.VisitorPolicyText, 'Standard safety rules apply.') AS PolicyText
                    FROM VMS_VisitLog v
                    LEFT JOIN MST_UserMaster u ON v.HostEmployeeCode = u.EmployeeCode
                    LEFT JOIN VMS_OfficeSettings s ON v.TargetBranchID = s.Branch_ID
                    WHERE v.QRToken = @Token";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Token", token);
                    con.Open();

                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        if (!sdr.Read())
                            return null;

                        return new DigitalPassDetails
                        {
                            LogId = Convert.ToInt32(sdr["LogID"]),
                            VisitorName = sdr["VisitorName"].ToString(),
                            VisitorCompany = sdr["VisitorCompany"].ToString(),
                            PhotoPath = sdr["PhotoPath"].ToString(),
                            ValidFrom = sdr["ValidFrom"] == DBNull.Value
                                ? (DateTime?)null
                                : Convert.ToDateTime(sdr["ValidFrom"]),
                            ValidUpto = sdr["ValidUpto"] == DBNull.Value
                                ? (DateTime?)null
                                : Convert.ToDateTime(sdr["ValidUpto"]),
                            ApprovalStatus = sdr["ApprovalStatus"].ToString(),
                            ScheduleType = sdr["ScheduleType"].ToString(),
                            HostName = sdr["HostName"].ToString(),
                            PlantAlias = sdr["PlantAlias"].ToString(),
                            PolicyText = sdr["PolicyText"].ToString()
                        };
                    }
                }
            }
        }

        public string GetMultiDayApprovedDatesText(int logId)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                return VmsHelper.GetMultiDayApprovedDatesText(con, logId);
            }
        }

        public VisitDayLogInfo GetTodayVisitDayLog(int logId)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                return VmsVisitScheduleHelper.GetTodayDayLog(con, logId);
            }
        }
    }
}
