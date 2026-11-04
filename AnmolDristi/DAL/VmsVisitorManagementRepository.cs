using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;

namespace AnmolDristi.DAL
{
    public enum ApprovalFailureReason
    {
        None = 0,
        NotFoundOrNotOwned = 1,
        NotPending = 2,
        Unexpected = 3,
        /// <summary>Admin On-Behalf only: TargetBranchID is missing or has no MST_Branch record.</summary>
        InvalidBranch = 4,
        /// <summary>Admin On-Behalf only: a day in the approved window is Missing or Invalid in VMS_PlantTimings.</summary>
        PlantTimingNotConfigured = 5
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

        /// <summary>
        /// After-hours audit actor. Host uses employee code; Admin/Receptionist pass "Admin:…" / "Receptionist:…".
        /// </summary>
        public string ApprovedBy { get; set; }
    }

    public class VisitorApprovalResult
    {
        public bool Succeeded { get; set; }
        public ApprovalFailureReason FailureReason { get; set; }
        /// <summary>Weekdays blocking approval when FailureReason is PlantTimingNotConfigured.</summary>
        public string FailureDetail { get; set; }
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
        UnexpectedFailure = 3,
        /// <summary>Target DayLogID has more than one open attendance-history row.</summary>
        InconsistentAttendanceHistory = 4
    }

    /// <summary>
    /// How an Admin Out-Time correction reconciled the target day's existing history rows.
    /// In-Time corrections leave this as <see cref="NotApplicable"/>.
    /// </summary>
    public enum AdminOutTimeHistoryOutcome
    {
        NotApplicable = 0,
        ClosedExisting = 1,
        MissingHistoryNoSyntheticRow = 2,
        NoOpenHistoryRow = 3
    }

    public class AdminCorrectionResult
    {
        public bool Succeeded { get; set; }
        public AdminCorrectionFailureReason FailureReason { get; set; }
        public int LogId { get; set; }
        public AdminOutTimeHistoryOutcome HistoryOutcome { get; set; }
        /// <summary>
        /// Controlled operator text for a failed correction. The page shows this instead of a generic reason.
        /// Never assigned from a SQL exception message.
        /// </summary>
        public string OperatorMessage { get; set; }
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
        /// Host reject of a pending visit (AR-VMS-06B + AR-VMS-07A).
        /// Guarded: ApprovalStatus must be Pending AND HostEmployeeCode must match.
        /// Atomically cascades all associated VisitDayLog rows to Cancelled / IsDayApproved=0.
        /// Does not delete DayLog rows. No notifications.
        /// </summary>
        /// <returns>True only when the owned Parent Pending→Rejected update affected exactly one row.</returns>
        public bool RejectVisitor(int logId, string hostEmployeeCode)
        {
            if (string.IsNullOrWhiteSpace(hostEmployeeCode))
                return false;

            string resolvedHost = hostEmployeeCode.Trim();

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
                              AND ApprovalStatus = 'Pending'
                              AND HostEmployeeCode = @HostEmployeeCode", con, trans))
                        {
                            cmd.Parameters.AddWithValue("@LogID", logId);
                            cmd.Parameters.AddWithValue("@HostEmployeeCode", resolvedHost);
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
        /// Host convenience Check-In (AR-VMS-03A + AR-VMS-07A ownership).
        /// Requires an approved today VisitDayLog and reuses CheckInDay, including the
        /// parent ∩ day window. Missing or unapproved today fails closed.
        /// Requires HostEmployeeCode ownership under the same transaction.
        /// Does not create DayLogs or queue arrival notifications.
        /// </summary>
        /// <returns>True only when ownership and the guarded mutation succeeded.</returns>
        public bool ManualCheckIn(int logId, string hostEmployeeCode)
        {
            if (string.IsNullOrWhiteSpace(hostEmployeeCode))
                return false;

            string resolvedHost = hostEmployeeCode.Trim();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (SqlTransaction trans = con.BeginTransaction())
                {
                    try
                    {
                        if (!TryAssertHostOwnsVisit(con, trans, logId, resolvedHost))
                        {
                            try { trans.Rollback(); } catch { }
                            return false;
                        }

                        VisitDayLogInfo todayLog = VmsVisitScheduleHelper.GetTodayDayLog(con, logId, trans);

                        bool succeeded = todayLog != null
                            && todayLog.DayLogId > 0
                            && todayLog.IsDayApproved
                            && VmsVisitScheduleHelper.CheckInDay(
                                con,
                                todayLog.DayLogId,
                                logId,
                                "HostManual",
                                trans);

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
        /// Host convenience Check-Out (AR-VMS-03A + AR-VMS-07A ownership).
        /// Requires an approved today VisitDayLog and reuses CheckOutDay. Missing or
        /// unapproved today fails closed. Parent status remains a display summary.
        /// Requires HostEmployeeCode ownership under the same transaction. No departure notifications.
        /// </summary>
        public bool ManualCheckOut(int logId, string hostEmployeeCode)
        {
            if (string.IsNullOrWhiteSpace(hostEmployeeCode))
                return false;

            string resolvedHost = hostEmployeeCode.Trim();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (SqlTransaction trans = con.BeginTransaction())
                {
                    try
                    {
                        if (!TryAssertHostOwnsVisit(con, trans, logId, resolvedHost))
                        {
                            try { trans.Rollback(); } catch { }
                            return false;
                        }

                        VisitDayLogInfo todayLog = VmsVisitScheduleHelper.GetTodayDayLog(con, logId, trans);

                        bool succeeded = todayLog != null
                            && todayLog.DayLogId > 0
                            && todayLog.IsDayApproved
                            && VmsVisitScheduleHelper.CheckOutDay(
                                con,
                                todayLog.DayLogId,
                                logId,
                                "HostManual",
                                trans);

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
        /// Assert Host ownership under the open transaction (UPDLOCK) so SyncHost reassignment
        /// cannot race a stale Host CI/CO/Reject between check and mutation.
        /// </summary>
        private static bool TryAssertHostOwnsVisit(
            SqlConnection con,
            SqlTransaction trans,
            int logId,
            string hostEmployeeCode)
        {
            using (SqlCommand cmd = new SqlCommand(@"
                SELECT HostEmployeeCode
                FROM VMS_VisitLog WITH (UPDLOCK, ROWLOCK)
                WHERE LogID = @LogID", con, trans))
            {
                cmd.Parameters.AddWithValue("@LogID", logId);
                object value = cmd.ExecuteScalar();
                if (value == null || value == DBNull.Value)
                    return false;

                return string.Equals(
                    value.ToString().Trim(),
                    hostEmployeeCode,
                    StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Receptionist desk Check-In (AR-VMS-03B). Requires an approved today VisitDayLog and
        /// reuses CheckInDay, including the parent ∩ day window. Missing or unapproved today
        /// fails closed. Preserves [Reception Override] remarks. No branch predicate or notifications.
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

                        bool succeeded = todayLog != null
                            && todayLog.DayLogId > 0
                            && todayLog.IsDayApproved
                            && VmsVisitScheduleHelper.CheckInDay(
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
                                "[Reception Override] Manual Check-In on " + DateTime.Now.ToString("dd-MMM-yy HH:mm"));
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
        /// Receptionist desk Check-Out (AR-VMS-03B). Requires an approved today VisitDayLog and
        /// reuses CheckOutDay. Missing or unapproved today fails closed. CheckedOutBy comes from
        /// the shared engine. No departure notifications.
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

                        bool succeeded = todayLog != null
                            && todayLog.DayLogId > 0
                            && todayLog.IsDayApproved
                            && VmsVisitScheduleHelper.CheckOutDay(
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
        /// Admin Inline Gate Override — Out-Time correction.
        /// Resolves the DayLog for the date of <paramref name="adminOutTime"/> via
        /// GetDayLogForDate. Does not call CheckOutDay and does not insert attendance history.
        /// An approved, Checked-In target day is corrected in the same transaction as its
        /// history reconciliation and the untruncated parent audit append.
        /// Day rows for the LogID are held with UPDLOCK, HOLDLOCK on UQ_VMS_VisitDayLog_LogDate
        /// before the target-day or legacy decision and until commit. That blocks CheckInDay's
        /// day update (it locks the day before the parent) and blocks a new day insert for this
        /// LogID. Parent VisitLog is updated only when no later approved day already has
        /// attendance, and then keeps CheckedOutBy = ADMIN_INLINE. The DayLog records the Admin identity.
        /// Legacy parent-only remains only when the locked read shows no VisitDayLog rows at all.
        /// </summary>
        public AdminCorrectionResult AdminCorrectOutTime(
            int logId,
            DateTime adminOutTime,
            string overrideReason,
            string userName)
        {
            if (string.IsNullOrWhiteSpace(overrideReason))
            {
                return new AdminCorrectionResult
                {
                    Succeeded = false,
                    FailureReason = AdminCorrectionFailureReason.UnexpectedFailure,
                    LogId = logId,
                    OperatorMessage = "You must type an Audit Reason BEFORE changing the date/time."
                };
            }

            string adminIdentity = string.IsNullOrWhiteSpace(userName) ? "Admin" : userName.Trim();
            string reason = overrideReason.Trim();
            DateTime targetDate = adminOutTime.Date;

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (SqlTransaction trans = con.BeginTransaction())
                {
                    try
                    {
                        List<VisitDayLogInfo> lockedDays = LockVisitDayLogs(con, trans, logId);
                        VisitDayLogInfo targetLog = null;
                        for (int i = 0; i < lockedDays.Count; i++)
                        {
                            if (lockedDays[i].VisitDate.Date == targetDate)
                            {
                                targetLog = lockedDays[i];
                                break;
                            }
                        }

                        if (targetLog != null && targetLog.DayLogId > 0)
                        {
                            if (targetLog.LogId != logId || targetLog.VisitDate.Date != targetDate)
                            {
                                return FailOutTime(trans, logId, AdminCorrectionFailureReason.StateChanged, null);
                            }

                            if (!targetLog.IsDayApproved)
                            {
                                return FailOutTime(trans, logId, AdminCorrectionFailureReason.UnapprovedToday, null);
                            }

                            ColumnCapacity checkedOutBy = ReadColumnCapacity(con, trans, "VMS_VisitDayLog", "CheckedOutBy");
                            if (!checkedOutBy.Found)
                            {
                                return FailOutTime(
                                    trans,
                                    logId,
                                    AdminCorrectionFailureReason.UnexpectedFailure,
                                    "Out-Time correction was rejected because the visit-day CheckedOutBy column length could not be verified. No change was saved.");
                            }

                            if (!IdentityFits(con, trans, adminIdentity, checkedOutBy))
                            {
                                return FailOutTime(
                                    trans,
                                    logId,
                                    AdminCorrectionFailureReason.UnexpectedFailure,
                                    "Out-Time correction was rejected because the Admin identity does not fit the visit-day CheckedOutBy column. No change was saved.");
                            }

                            if (!UpdateTargetDayOutTime(
                                con,
                                trans,
                                logId,
                                targetLog.DayLogId,
                                targetDate,
                                adminOutTime,
                                adminIdentity,
                                checkedOutBy))
                            {
                                return FailOutTime(trans, logId, AdminCorrectionFailureReason.StateChanged, null);
                            }

                            AdminOutTimeHistoryOutcome historyOutcome;
                            if (!TryReconcileTargetDayHistory(
                                con,
                                trans,
                                logId,
                                targetLog.DayLogId,
                                adminOutTime,
                                out historyOutcome))
                            {
                                return FailOutTime(
                                    trans,
                                    logId,
                                    AdminCorrectionFailureReason.InconsistentAttendanceHistory,
                                    null);
                            }

                            // Later days stay U-locked, so CheckInDay cannot commit a newer cycle
                            // between this decision and the parent update.
                            if (!LaterDayOwnsParentSummary(lockedDays, targetDate))
                            {
                                bool hasFutureApproved = false;
                                for (int i = 0; i < lockedDays.Count; i++)
                                {
                                    if (lockedDays[i].IsDayApproved && lockedDays[i].VisitDate.Date > targetDate)
                                    {
                                        hasFutureApproved = true;
                                        break;
                                    }
                                }

                                if (!UpdateParentOutTimeSummary(con, trans, logId, adminOutTime, hasFutureApproved))
                                {
                                    return FailOutTime(trans, logId, AdminCorrectionFailureReason.StateChanged, null);
                                }
                            }

                            string auditTrail = BuildAdminOutTimeAuditTrail(
                                logId,
                                targetLog.DayLogId,
                                targetLog.VisitDate.Date,
                                targetLog.ActualOutTime,
                                adminOutTime,
                                adminIdentity,
                                reason,
                                HistoryToken(historyOutcome));

                            if (!TryAppendAdminOutTimeAudit(con, trans, logId, auditTrail))
                            {
                                return FailOutTime(
                                    trans,
                                    logId,
                                    AdminCorrectionFailureReason.UnexpectedFailure,
                                    "Out-Time correction was rejected because the audit entry could not be stored in full. No change was saved.");
                            }

                            trans.Commit();
                            return new AdminCorrectionResult
                            {
                                Succeeded = true,
                                FailureReason = AdminCorrectionFailureReason.None,
                                LogId = logId,
                                HistoryOutcome = historyOutcome
                            };
                        }

                        if (lockedDays.Count > 0)
                        {
                            return FailOutTime(
                                trans,
                                logId,
                                AdminCorrectionFailureReason.StateChanged,
                                "Out-Time correction was rejected because no visit day exists for the selected date. No other day was changed.");
                        }

                        DateTime? previousParentOut = ReadParentActualOutTime(con, trans, logId);
                        if (!AdminCorrectOutTimeParentOnly(con, trans, logId, adminOutTime))
                        {
                            return FailOutTime(trans, logId, AdminCorrectionFailureReason.StateChanged, null);
                        }

                        string legacyAudit = BuildAdminOutTimeAuditTrail(
                            logId,
                            null,
                            targetDate,
                            previousParentOut,
                            adminOutTime,
                            adminIdentity,
                            reason,
                            "NO_TARGET_DAYLOG");

                        if (!TryAppendAdminOutTimeAudit(con, trans, logId, legacyAudit))
                        {
                            return FailOutTime(
                                trans,
                                logId,
                                AdminCorrectionFailureReason.UnexpectedFailure,
                                "Out-Time correction was rejected because the audit entry could not be stored in full. No change was saved.");
                        }

                        trans.Commit();
                        return new AdminCorrectionResult
                        {
                            Succeeded = true,
                            FailureReason = AdminCorrectionFailureReason.None,
                            LogId = logId,
                            HistoryOutcome = AdminOutTimeHistoryOutcome.NotApplicable
                        };
                    }
                    catch
                    {
                        try { trans.Rollback(); } catch { }
                        return new AdminCorrectionResult
                        {
                            Succeeded = false,
                            FailureReason = AdminCorrectionFailureReason.UnexpectedFailure,
                            LogId = logId,
                            OperatorMessage = "Out-Time correction could not be completed. No change was saved."
                        };
                    }
                }
            }
        }

        private static AdminCorrectionResult FailOutTime(
            SqlTransaction trans,
            int logId,
            AdminCorrectionFailureReason reason,
            string operatorMessage)
        {
            try
            {
                if (trans != null)
                    trans.Rollback();
            }
            catch
            {
            }

            return new AdminCorrectionResult
            {
                Succeeded = false,
                FailureReason = reason,
                LogId = logId,
                OperatorMessage = operatorMessage
            };
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
        /// DayLogID must belong to the same LogID (AR-VMS-08A).
        /// Requires today's approved DayLog and Parent ApprovalStatus='Approved' (AR-VMS-10).
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
                UPDATE d
                SET d.ActualInTime = @AdminInTime,
                    d.VisitStatus = 'Checked-In'
                FROM VMS_VisitDayLog d
                INNER JOIN VMS_VisitLog v ON v.LogID = d.LogID
                WHERE d.DayLogID = @DayLogID
                  AND d.LogID = @LogID
                  AND d.VisitDate = CAST(GETDATE() AS date)
                  AND d.IsDayApproved = 1
                  AND d.VisitStatus = 'Scheduled'
                  AND v.ApprovalStatus = 'Approved'", con, trans))
            {
                cmd.Parameters.AddWithValue("@AdminInTime", adminInTime);
                cmd.Parameters.AddWithValue("@DayLogID", dayLogId);
                cmd.Parameters.AddWithValue("@LogID", logId);
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
                  AND VisitStatus = 'Scheduled'
                  AND ApprovalStatus = 'Approved'", con, trans))
            {
                cmd.Parameters.AddWithValue("@AdminInTime", adminInTime);
                cmd.Parameters.AddWithValue("@LogID", logId);
                visitRows = cmd.ExecuteNonQuery();
            }

            return visitRows == 1;
        }

        /// <summary>
        /// Legacy niche: no today VisitDayLog row — guarded parent-only In-Time correction.
        /// Requires ApprovalStatus='Approved' (AR-VMS-07B).
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
                  AND ApprovalStatus = 'Approved'
                  AND VisitStatus = 'Scheduled'", con, trans))
            {
                cmd.Parameters.AddWithValue("@AdminInTime", adminInTime);
                cmd.Parameters.AddWithValue("@LogID", logId);
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        /// <summary>
        /// Updates only the target DayLog. VisitDate, LogID, approval, and Checked-In are required.
        /// CheckedOutBy is the Admin identity and is never truncated to fit the column.
        /// </summary>
        private static bool UpdateTargetDayOutTime(
            SqlConnection con,
            SqlTransaction trans,
            int logId,
            int dayLogId,
            DateTime targetDate,
            DateTime adminOutTime,
            string adminIdentity,
            ColumnCapacity checkedOutBy)
        {
            using (SqlCommand cmd = new SqlCommand(@"
                UPDATE d
                SET d.ActualOutTime = @AdminOutTime,
                    d.VisitStatus = 'Checked-Out',
                    d.CheckedOutBy = @CheckedOutBy
                FROM VMS_VisitDayLog d
                INNER JOIN VMS_VisitLog v ON v.LogID = d.LogID
                WHERE d.DayLogID = @DayLogID
                  AND d.LogID = @LogID
                  AND d.VisitDate = @VisitDate
                  AND d.IsDayApproved = 1
                  AND d.VisitStatus = 'Checked-In'
                  AND v.ApprovalStatus = 'Approved'", con, trans))
            {
                cmd.Parameters.AddWithValue("@AdminOutTime", adminOutTime);
                SqlDbType identityType = checkedOutBy.IsUnicode ? SqlDbType.NVarChar : SqlDbType.VarChar;
                int size = checkedOutBy.IsMax
                    ? -1
                    : (checkedOutBy.IsUnicode ? checkedOutBy.MaxLengthBytes / 2 : checkedOutBy.MaxLengthBytes);
                cmd.Parameters.Add("@CheckedOutBy", identityType, size).Value = adminIdentity;
                cmd.Parameters.AddWithValue("@DayLogID", dayLogId);
                cmd.Parameters.AddWithValue("@LogID", logId);
                cmd.Parameters.AddWithValue("@VisitDate", targetDate.Date);
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        /// <summary>
        /// Reconciles existing history for this DayLogID only. Never inserts a row and never
        /// updates Source, Actor, CheckInTime, or CreatedOn. More than one open row fails closed.
        /// </summary>
        private static bool TryReconcileTargetDayHistory(
            SqlConnection con,
            SqlTransaction trans,
            int logId,
            int dayLogId,
            DateTime adminOutTime,
            out AdminOutTimeHistoryOutcome outcome)
        {
            outcome = AdminOutTimeHistoryOutcome.NotApplicable;

            int totalRows;
            int openRows;
            using (SqlCommand cmd = new SqlCommand(@"
                SELECT
                    COUNT(1) AS TotalRows,
                    SUM(CASE WHEN CheckOutTime IS NULL THEN 1 ELSE 0 END) AS OpenRows
                FROM dbo.VMS_VisitAttendanceHistory WITH (UPDLOCK, HOLDLOCK)
                WHERE DayLogID = @DayLogID", con, trans))
            {
                cmd.Parameters.AddWithValue("@DayLogID", dayLogId);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    totalRows = Convert.ToInt32(reader["TotalRows"]);
                    openRows = reader["OpenRows"] == DBNull.Value ? 0 : Convert.ToInt32(reader["OpenRows"]);
                }
            }

            if (openRows > 1)
                return false;

            if (openRows == 1)
            {
                using (SqlCommand cmd = new SqlCommand(@"
                    UPDATE dbo.VMS_VisitAttendanceHistory
                    SET CheckOutTime = @AdminOutTime
                    WHERE DayLogID = @DayLogID
                      AND LogID = @LogID
                      AND CheckOutTime IS NULL", con, trans))
                {
                    cmd.Parameters.AddWithValue("@AdminOutTime", adminOutTime);
                    cmd.Parameters.AddWithValue("@DayLogID", dayLogId);
                    cmd.Parameters.AddWithValue("@LogID", logId);
                    if (cmd.ExecuteNonQuery() != 1)
                        return false;
                }

                outcome = AdminOutTimeHistoryOutcome.ClosedExisting;
                return true;
            }

            if (totalRows == 0)
            {
                outcome = AdminOutTimeHistoryOutcome.MissingHistoryNoSyntheticRow;
                return true;
            }

            outcome = AdminOutTimeHistoryOutcome.NoOpenHistoryRow;
            return true;
        }

        /// <summary>
        /// Locks every day row for this LogID, and the key range a new (LogID, VisitDate) would use.
        /// CheckInDay updates a day row before the parent, so it waits here and cannot change the
        /// parent summary until this transaction ends. CreateRequestedDayLogs inserts into the same
        /// unique key, so it cannot add a day between this read and commit.
        /// </summary>
        private static List<VisitDayLogInfo> LockVisitDayLogs(
            SqlConnection con,
            SqlTransaction trans,
            int logId)
        {
            var days = new List<VisitDayLogInfo>();
            using (SqlCommand cmd = new SqlCommand(@"
                SELECT DayLogID, LogID, VisitDate, DayValidFrom, DayValidUpto,
                       VisitStatus, ActualInTime, ActualOutTime, IsDayApproved
                FROM dbo.VMS_VisitDayLog WITH (UPDLOCK, HOLDLOCK, INDEX(UQ_VMS_VisitDayLog_LogDate))
                WHERE LogID = @LogID
                ORDER BY VisitDate", con, trans))
            {
                cmd.Parameters.AddWithValue("@LogID", logId);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        days.Add(new VisitDayLogInfo
                        {
                            DayLogId = Convert.ToInt32(reader["DayLogID"]),
                            LogId = Convert.ToInt32(reader["LogID"]),
                            VisitDate = Convert.ToDateTime(reader["VisitDate"]),
                            DayValidFrom = Convert.ToDateTime(reader["DayValidFrom"]),
                            DayValidUpto = Convert.ToDateTime(reader["DayValidUpto"]),
                            VisitStatus = reader["VisitStatus"] == DBNull.Value ? "Scheduled" : reader["VisitStatus"].ToString(),
                            ActualInTime = reader["ActualInTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["ActualInTime"]),
                            ActualOutTime = reader["ActualOutTime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["ActualOutTime"]),
                            IsDayApproved = reader["IsDayApproved"] != DBNull.Value && Convert.ToBoolean(reader["IsDayApproved"])
                        });
                    }
                }
            }

            return days;
        }

        private static bool LaterDayOwnsParentSummary(List<VisitDayLogInfo> days, DateTime targetDate)
        {
            for (int i = 0; i < days.Count; i++)
            {
                VisitDayLogInfo day = days[i];
                if (!day.IsDayApproved || day.VisitDate.Date <= targetDate.Date)
                    continue;
                if (day.ActualInTime.HasValue
                    || string.Equals(day.VisitStatus, "Checked-In", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(day.VisitStatus, "Checked-Out", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Parent summary for the cycle being closed. CheckedOutBy stays ADMIN_INLINE.
        /// Caller has already U-locked the visit's day rows.
        /// </summary>
        private static bool UpdateParentOutTimeSummary(
            SqlConnection con,
            SqlTransaction trans,
            int logId,
            DateTime adminOutTime,
            bool hasFutureApprovedDays)
        {
            string nextVisitStatus = hasFutureApprovedDays ? "Scheduled" : "Checked-Out";

            using (SqlCommand cmd = new SqlCommand(@"
                UPDATE VMS_VisitLog
                SET ActualOutTime = @AdminOutTime,
                    VisitStatus = @VisitStatus,
                    CheckedOutBy = 'ADMIN_INLINE'
                WHERE LogID = @LogID
                  AND VisitStatus = 'Checked-In'
                  AND ApprovalStatus = 'Approved'", con, trans))
            {
                cmd.Parameters.AddWithValue("@AdminOutTime", adminOutTime);
                cmd.Parameters.AddWithValue("@VisitStatus", nextVisitStatus);
                cmd.Parameters.AddWithValue("@LogID", logId);
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        private struct ColumnCapacity
        {
            public bool Found;
            public bool IsUnicode;
            public bool IsMax;
            public int MaxLengthBytes;
            public string CollationName;
        }

        /// <summary>
        /// Column byte length and collation. Found is false when the column cannot be read.
        /// The caller must not assign a value that does not fit.
        /// </summary>
        private static ColumnCapacity ReadColumnCapacity(
            SqlConnection con,
            SqlTransaction trans,
            string tableName,
            string columnName)
        {
            using (SqlCommand cmd = new SqlCommand(@"
                SELECT TYPE_NAME(c.system_type_id) AS TypeName, c.max_length, c.collation_name
                FROM sys.columns c
                WHERE c.object_id = OBJECT_ID(@QualifiedName)
                  AND c.name = @ColumnName", con, trans))
            {
                cmd.Parameters.Add("@QualifiedName", SqlDbType.NVarChar, 128).Value = "dbo." + tableName;
                cmd.Parameters.Add("@ColumnName", SqlDbType.NVarChar, 128).Value = columnName;
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return new ColumnCapacity();

                    string typeName = Convert.ToString(reader["TypeName"]);
                    int maxLength = Convert.ToInt32(reader["max_length"]);
                    bool unicode = string.Equals(typeName, "nvarchar", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(typeName, "nchar", StringComparison.OrdinalIgnoreCase);

                    return new ColumnCapacity
                    {
                        Found = true,
                        IsUnicode = unicode,
                        IsMax = maxLength < 0,
                        MaxLengthBytes = maxLength,
                        CollationName = reader["collation_name"] == DBNull.Value
                            ? null
                            : Convert.ToString(reader["collation_name"])
                    };
                }
            }
        }

        /// <summary>
        /// Unicode columns compare UTF-16 length with the character capacity.
        /// Non-Unicode columns compare the collation's varchar byte length and reject a value
        /// that would lose characters. Nothing is truncated.
        /// </summary>
        private static bool IdentityFits(
            SqlConnection con,
            SqlTransaction trans,
            string identity,
            ColumnCapacity column)
        {
            if (!column.Found || string.IsNullOrEmpty(identity))
                return false;
            if (column.IsUnicode)
            {
                if (column.IsMax)
                    return true;
                return identity.Length <= (column.MaxLengthBytes / 2);
            }

            int byteLength;
            bool lossless;
            if (!TryMeasureVarchar(con, trans, identity, column.CollationName, out byteLength, out lossless))
                return false;
            if (!lossless)
                return false;
            if (column.IsMax)
                return true;
            return byteLength >= 0 && byteLength <= column.MaxLengthBytes;
        }

        private static bool TryMeasureVarchar(
            SqlConnection con,
            SqlTransaction trans,
            string identity,
            string collationName,
            out int byteLength,
            out bool lossless)
        {
            byteLength = -1;
            lossless = false;
            string converted = "CONVERT(varchar(max), @Identity)";
            if (!string.IsNullOrEmpty(collationName))
            {
                if (!IsSafeCollationName(collationName))
                    return false;
                converted = converted + " COLLATE " + collationName;
            }

            using (SqlCommand cmd = new SqlCommand(@"
                SELECT DATALENGTH(" + converted + @") AS ByteLength,
                       CASE WHEN CONVERT(nvarchar(max), " + converted + @") = @Identity THEN 1 ELSE 0 END AS Lossless", con, trans))
            {
                cmd.Parameters.Add("@Identity", SqlDbType.NVarChar, -1).Value = identity;
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read() || reader["ByteLength"] == DBNull.Value)
                        return false;
                    byteLength = Convert.ToInt32(reader["ByteLength"]);
                    lossless = Convert.ToInt32(reader["Lossless"]) == 1;
                    return true;
                }
            }
        }

        private static bool IsSafeCollationName(string collationName)
        {
            for (int i = 0; i < collationName.Length; i++)
            {
                char c = collationName[i];
                if (!char.IsLetterOrDigit(c) && c != '_')
                    return false;
            }

            return collationName.Length > 0;
        }

        private static string HistoryToken(AdminOutTimeHistoryOutcome outcome)
        {
            switch (outcome)
            {
                case AdminOutTimeHistoryOutcome.ClosedExisting:
                    return "CLOSED_EXISTING";
                case AdminOutTimeHistoryOutcome.MissingHistoryNoSyntheticRow:
                    return "MISSING_HISTORY_NO_SYNTHETIC_ROW";
                case AdminOutTimeHistoryOutcome.NoOpenHistoryRow:
                    return "NO_OPEN_HISTORY_ROW";
                default:
                    return "NO_TARGET_DAYLOG";
            }
        }

        private static string FormatAuditTime(DateTime? value)
        {
            if (!value.HasValue)
                return "Empty";
            return value.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private static string BuildAdminOutTimeAuditTrail(
            int logId,
            int? dayLogId,
            DateTime visitDate,
            DateTime? previousOutTime,
            DateTime newOutTime,
            string adminIdentity,
            string reason,
            string historyToken)
        {
            string dayText = dayLogId.HasValue
                ? dayLogId.Value.ToString(CultureInfo.InvariantCulture)
                : "none";

            return string.Format(
                CultureInfo.InvariantCulture,
                " | ADMIN_OUTTIME_CORRECTION LogID={0}; DayLogID={1}; VisitDate={2:yyyy-MM-dd}; PreviousOutTime={3}; NewOutTime={4:yyyy-MM-dd HH:mm:ss}; Admin={5}; Reason={6}; History={7}",
                logId,
                dayText,
                visitDate,
                FormatAuditTime(previousOutTime),
                newOutTime,
                adminIdentity,
                reason,
                historyToken);
        }

        /// <summary>
        /// Appends the Out-Time audit event without the 250-character LEFT truncation.
        /// Succeeds only when exactly one parent row is updated. A string-truncation error
        /// returns false so the caller rolls the correction back.
        /// </summary>
        private static bool TryAppendAdminOutTimeAudit(
            SqlConnection con,
            SqlTransaction trans,
            int logId,
            string auditTrail)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand(@"
                    UPDATE VMS_VisitLog
                    SET ApprovalRemarks = ISNULL(CAST(ApprovalRemarks AS NVARCHAR(MAX)), N'') + @AuditTrail
                    WHERE LogID = @LogID", con, trans))
                {
                    cmd.Parameters.Add("@AuditTrail", SqlDbType.NVarChar, -1).Value = auditTrail ?? string.Empty;
                    cmd.Parameters.AddWithValue("@LogID", logId);
                    return cmd.ExecuteNonQuery() == 1;
                }
            }
            catch (SqlException ex)
            {
                if (IsStringTruncation(ex))
                    return false;
                throw;
            }
        }

        private static bool IsStringTruncation(SqlException ex)
        {
            if (ex == null)
                return false;
            if (ex.Number == 8152 || ex.Number == 2628)
                return true;

            foreach (SqlError error in ex.Errors)
            {
                if (error.Number == 8152 || error.Number == 2628)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Legacy niche: the visit has no VisitDayLog rows. Guarded parent-only Out-Time correction.
        /// Requires ApprovalStatus='Approved' and VisitStatus='Checked-In'. Not used when any
        /// day row exists, including a MultiDay visit whose target date has no row.
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
                  AND ApprovalStatus = 'Approved'
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
        /// Missing branch or Missing/Invalid plant timings on any approved day block approval (OOH-02/12).
        /// </summary>
        public VisitorApprovalResult ApproveVisitOnBehalf(VisitorApprovalRequest request)
        {
            return ApproveVisitCore(request, ApprovalAuthorizationMode.PendingOnly, requireConfiguredPlantDays: true);
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
        /// selected days → QR → guarded VisitLog UPDATE → ApproveVisitSchedule → [on-behalf plant-day guard] →
        /// re-apply selected days → optional time override → pass notify → optional host on-behalf notify → COMMIT.
        /// </summary>
        private VisitorApprovalResult ApproveVisitCore(
            VisitorApprovalRequest request,
            ApprovalAuthorizationMode authMode,
            bool requireConfiguredPlantDays = false)
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

                        if (requireConfiguredPlantDays)
                        {
                            string blockedDays;
                            ApprovalFailureReason guardReason = ValidateApprovedPlantDays(con, trans, request.LogId, out blockedDays);
                            if (guardReason != ApprovalFailureReason.None)
                            {
                                try
                                {
                                    trans.Rollback();
                                }
                                catch
                                {
                                    // Do not mask the validation failure result.
                                }

                                return new VisitorApprovalResult
                                {
                                    Succeeded = false,
                                    FailureReason = guardReason,
                                    FailureDetail = blockedDays,
                                    LogId = request.LogId
                                };
                            }
                        }

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

                        TryWriteAfterHoursApprovalAudit(
                            con,
                            trans,
                            request,
                            hostEmployeeCode,
                            authMode);

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

        private static void TryWriteAfterHoursApprovalAudit(
            SqlConnection con,
            SqlTransaction trans,
            VisitorApprovalRequest request,
            string hostEmployeeCode,
            ApprovalAuthorizationMode authMode)
        {
            var selectedDates = new List<DateTime>();
            if (request.SelectedDays != null)
            {
                foreach (SelectedVisitDay day in request.SelectedDays)
                {
                    if (day.IsApproved)
                        selectedDates.Add(day.VisitDate.Date);
                }
            }

            VisitHoursEvaluation evaluation = VmsVisitScheduleHelper.EvaluateVisitHoursForLog(
                con,
                request.LogId,
                request.ApprovedFromTime,
                request.ApprovedToTime,
                selectedDates.Count > 0 ? selectedDates : null,
                trans);

            if (evaluation == null || !evaluation.RequiresNotice)
                return;

            int branchId = 0;
            DateTime scheduledFrom = DateTime.Now;
            DateTime scheduledTo = DateTime.Now;
            string visitHost = hostEmployeeCode ?? string.Empty;
            using (SqlCommand cmd = new SqlCommand(@"
                SELECT ISNULL(TargetBranchID, 0) AS TargetBranchID,
                       HostEmployeeCode,
                       ISNULL(ValidFrom, ISNULL(RequestedValidFrom, GETDATE())) AS ValidFrom,
                       ISNULL(ValidUpto, ISNULL(RequestedValidUpto, GETDATE())) AS ValidUpto
                FROM VMS_VisitLog
                WHERE LogID = @LogID", con, trans))
            {
                cmd.Parameters.AddWithValue("@LogID", request.LogId);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return;
                    branchId = Convert.ToInt32(reader["TargetBranchID"]);
                    if (reader["HostEmployeeCode"] != DBNull.Value)
                        visitHost = reader["HostEmployeeCode"].ToString();
                    scheduledFrom = Convert.ToDateTime(reader["ValidFrom"]);
                    scheduledTo = Convert.ToDateTime(reader["ValidUpto"]);
                }
            }

            DateTime scheduledDate = evaluation.FirstAffectedDate ?? scheduledFrom.Date;
            int? dayLogId = null;
            using (SqlCommand dayCmd = new SqlCommand(@"
                SELECT TOP 1 DayLogID
                FROM VMS_VisitDayLog
                WHERE LogID = @LogID AND VisitDate = @VisitDate", con, trans))
            {
                dayCmd.Parameters.AddWithValue("@LogID", request.LogId);
                dayCmd.Parameters.AddWithValue("@VisitDate", scheduledDate);
                object dayIdObj = dayCmd.ExecuteScalar();
                if (dayIdObj != null && dayIdObj != DBNull.Value)
                    dayLogId = Convert.ToInt32(dayIdObj);
            }

            string approvedBy;
            if (!string.IsNullOrWhiteSpace(request.ApprovedBy))
                approvedBy = request.ApprovedBy.Trim();
            else if (!string.IsNullOrWhiteSpace(hostEmployeeCode))
                approvedBy = hostEmployeeCode.Trim();
            else
                approvedBy = authMode == ApprovalAuthorizationMode.PendingOnly ? "Receptionist" : "Host";
            if (approvedBy.Length > 50)
                approvedBy = approvedBy.Substring(0, 50);

            string reason = evaluation.HostWarningMessage ?? string.Empty;
            if (reason.Length > 500)
                reason = reason.Substring(0, 500);

            using (SqlCommand insertCmd = new SqlCommand(@"
                IF OBJECT_ID('dbo.VMS_AfterHoursApprovalAudit', 'U') IS NOT NULL
                   AND NOT EXISTS (
                        SELECT 1 FROM VMS_AfterHoursApprovalAudit WHERE LogID = @LogID
                   )
                BEGIN
                    INSERT INTO VMS_AfterHoursApprovalAudit
                    (
                        LogID, DayLogID, BranchID, HostEmpCode,
                        ScheduledDate, ScheduledFrom, ScheduledTo,
                        PlantOpenTime, PlantCloseTime,
                        ApprovedOn, ApprovedBy, Reason
                    )
                    VALUES
                    (
                        @LogID, @DayLogID, @BranchID, @HostEmpCode,
                        @ScheduledDate, @ScheduledFrom, @ScheduledTo,
                        @PlantOpenTime, @PlantCloseTime,
                        GETDATE(), @ApprovedBy, @Reason
                    )
                END", con, trans))
            {
                insertCmd.Parameters.AddWithValue("@LogID", request.LogId);
                insertCmd.Parameters.AddWithValue("@DayLogID", (object)dayLogId ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@BranchID", branchId);
                insertCmd.Parameters.AddWithValue("@HostEmpCode",
                    string.IsNullOrWhiteSpace(visitHost) ? (object)DBNull.Value : visitHost);
                insertCmd.Parameters.AddWithValue("@ScheduledDate", scheduledDate);
                insertCmd.Parameters.AddWithValue("@ScheduledFrom", scheduledFrom);
                insertCmd.Parameters.AddWithValue("@ScheduledTo", scheduledTo);
                insertCmd.Parameters.AddWithValue("@PlantOpenTime",
                    evaluation.PlantOpenTime.HasValue ? (object)evaluation.PlantOpenTime.Value : DBNull.Value);
                insertCmd.Parameters.AddWithValue("@PlantCloseTime",
                    evaluation.PlantCloseTime.HasValue ? (object)evaluation.PlantCloseTime.Value : DBNull.Value);
                insertCmd.Parameters.AddWithValue("@ApprovedBy", approvedBy);
                insertCmd.Parameters.AddWithValue("@Reason",
                    string.IsNullOrEmpty(reason) ? (object)DBNull.Value : reason);
                insertCmd.ExecuteNonQuery();
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
                SET ValidFrom = DATEADD(MINUTE, @FromMinutes, CAST(CAST(ISNULL(ValidFrom, RequestedValidFrom) AS date) AS datetime)),
                    ValidUpto = DATEADD(MINUTE, @ToMinutes, CAST(CAST(ISNULL(ValidUpto, RequestedValidUpto) AS date) AS datetime)),
                    ApprovedValidFrom = DATEADD(MINUTE, @FromMinutes, CAST(CAST(ISNULL(ApprovedValidFrom, ISNULL(ValidFrom, RequestedValidFrom)) AS date) AS datetime)),
                    ApprovedValidUpto = DATEADD(MINUTE, @ToMinutes, CAST(CAST(ISNULL(ApprovedValidUpto, ISNULL(ValidUpto, RequestedValidUpto)) AS date) AS datetime))
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

        /// <summary>
        /// Runs inside the approval transaction after ApproveVisitSchedule(null, null), so it checks exactly the
        /// ApprovedValidFrom–ApprovedValidUpto calendar days that method approved. The branch must be &gt; 0 with an
        /// MST_Branch record; each day must classify Open or Closed — Missing and Invalid are never treated as open.
        /// </summary>
        private static ApprovalFailureReason ValidateApprovedPlantDays(
            SqlConnection con,
            SqlTransaction trans,
            int logId,
            out string blockedDays)
        {
            blockedDays = null;
            int branchId;
            DateTime start, end;
            using (SqlCommand cmd = new SqlCommand(@"
                SELECT v.TargetBranchID, v.ApprovedValidFrom, v.ApprovedValidUpto
                FROM VMS_VisitLog v
                WHERE v.LogID = @LogID
                  AND v.TargetBranchID > 0
                  AND EXISTS (SELECT 1 FROM MST_Branch b WHERE b.Branch_ID = v.TargetBranchID)", con, trans))
            {
                cmd.Parameters.AddWithValue("@LogID", logId);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return ApprovalFailureReason.InvalidBranch;
                    branchId = Convert.ToInt32(reader["TargetBranchID"]);
                    start = Convert.ToDateTime(reader["ApprovedValidFrom"]).Date;
                    end = Convert.ToDateTime(reader["ApprovedValidUpto"]).Date;
                }
            }

            Dictionary<string, PlantDayTiming> timings = VmsVisitScheduleHelper.LoadPlantTimings(branchId, con, trans, lockPlantTimings: true);
            var blocked = new List<string>();
            if (end < start)
                end = start;
            for (DateTime day = start; day <= end; day = day.AddDays(1))
            {
                string weekday = day.ToString("dddd", CultureInfo.InvariantCulture);
                PlantDayTiming timing;
                timings.TryGetValue(weekday, out timing);
                VmsVisitScheduleHelper.PlantDayConfigState state = VmsVisitScheduleHelper.ClassifyPlantDayConfig(timing);
                if (state == VmsVisitScheduleHelper.PlantDayConfigState.Open || state == VmsVisitScheduleHelper.PlantDayConfigState.Closed)
                    continue;
                string entry = weekday + (state == VmsVisitScheduleHelper.PlantDayConfigState.Missing ? " (not configured)" : " (invalid timings)");
                if (!blocked.Contains(entry))
                    blocked.Add(entry);
            }

            if (blocked.Count == 0)
                return ApprovalFailureReason.None;
            blockedDays = string.Join(", ", blocked);
            return ApprovalFailureReason.PlantTimingNotConfigured;
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

        public List<VisitDayLogInfo> GetVisitDayLogs(int logId)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                return VmsVisitScheduleHelper.GetDayLogsForVisit(con, logId);
            }
        }

        public Dictionary<int, List<VisitDayLogInfo>> GetDayLogsForVisits(IList<int> logIds)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                return VmsVisitScheduleHelper.GetDayLogsForVisits(con, logIds);
            }
        }
    }
}
