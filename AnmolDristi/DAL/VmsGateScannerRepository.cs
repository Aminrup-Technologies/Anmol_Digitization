using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace AnmolDristi.DAL
{
    public enum GateMutationFailureReason
    {
        None = 0,
        StateChanged = 1
    }

    public class GateMutationResult
    {
        public bool Succeeded { get; set; }
        public GateMutationFailureReason FailureReason { get; set; }
        public int LogId { get; set; }
    }

    /// <summary>
    /// Persistence access for VMS Gate Scanner read and check-in/out operations.
    /// </summary>
    public class VmsGateScannerRepository
    {
        private readonly string _connectionString;

        public VmsGateScannerRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        }

        public string GetGateLocation(int branchId)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT ISNULL(s.AliasName, b.Branch_Description) AS LocationName
                    FROM MST_Branch b
                    LEFT JOIN VMS_OfficeSettings s ON b.Branch_ID = s.Branch_ID
                    WHERE b.Branch_ID = @BranchID";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@BranchID", branchId);
                    con.Open();
                    object result = cmd.ExecuteScalar();
                    return result != null ? result.ToString() : null;
                }
            }
        }

        public DataTable GetInsideVisitors(int? branchId)
        {
            string query = @"
                SELECT 
                    v.LogID,
                    ISNULL(d.DayLogID, 0) AS DayLogID,
                    v.VisitorName, 
                    v.VisitorCompany, 
                    ISNULL(NULLIF(v.VisitorMobile, ''), v.VisitorEmail) AS ContactInfo,
                    ISNULL(u.EmployeeName, 'Unknown') AS HostName,
                    COALESCE(d.ActualInTime, v.ActualInTime) AS ActualInTime,
                    COALESCE(d.DayValidUpto, v.ValidUpto) AS ValidUpto,
                    v.PhotoPath
                FROM VMS_VisitLog v
                LEFT JOIN VMS_VisitDayLog d ON v.LogID = d.LogID
                    AND d.VisitDate = CAST(GETDATE() AS date)
                    AND d.VisitStatus = 'Checked-In'
                LEFT JOIN MST_UserMaster u ON v.HostEmployeeCode = u.EmployeeCode
                WHERE (
                    (d.DayLogID IS NOT NULL)
                    OR (v.VisitStatus = 'Checked-In' AND NOT EXISTS (
                        SELECT 1 FROM VMS_VisitDayLog dx WHERE dx.LogID = v.LogID
                    ))
                )";

            if (branchId.HasValue)
            {
                query += " AND v.TargetBranchID = @BranchId";
            }

            query += " ORDER BY v.ActualInTime DESC";

            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                if (branchId.HasValue)
                {
                    cmd.Parameters.AddWithValue("@BranchId", branchId.Value);
                }

                using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    sda.Fill(dt);
                    return dt;
                }
            }
        }

        public DataTable GetScanVisit(Guid token)
        {
            string query = @"
                SELECT 
                    v.LogID, v.VisitorName, v.VisitorCompany, v.VisitorMobile, v.VisitorEmail, 
                    v.ApprovalStatus, v.VisitStatus, v.ValidFrom, v.ValidUpto, v.PhotoPath, v.ActualInTime, v.PassCategory,
                    v.TargetBranchID,
                    u.EmployeeName AS HostName, u.Email AS HostEmail,
                    ISNULL(dept.Department_Description, 'General') AS HostDepartment,
                    ISNULL(b.Branch_Description, 'Unknown Branch') AS VisitBranchName,
                    d.DayLogID, d.DayValidFrom, d.DayValidUpto, d.VisitStatus AS DayVisitStatus,
                    d.ActualInTime AS DayActualInTime, d.IsDayApproved
                FROM VMS_VisitLog v
                INNER JOIN VMS_VisitDayLog d ON v.LogID = d.LogID
                LEFT JOIN MST_UserMaster u ON v.HostEmployeeCode = u.EmployeeCode
                LEFT JOIN MST_Department dept ON CAST(u.DepartmentId AS VARCHAR) = dept.Department_ID
                LEFT JOIN MST_Branch b ON v.TargetBranchID = b.Branch_ID
                WHERE v.QRToken = @Token
                  AND d.VisitDate = CAST(GETDATE() AS DATE)
                  AND d.IsDayApproved = 1";

            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@Token", token);
                using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    sda.Fill(dt);
                    return dt;
                }
            }
        }

        public DataTable GetVisitByToken(Guid token)
        {
            string query = @"
                SELECT 
                    v.VisitorName, v.VisitorCompany, v.VisitorMobile, v.VisitorEmail,
                    v.PhotoPath, v.PassCategory, v.ValidUpto,
                    u.EmployeeName AS HostName,
                    ISNULL(dept.Department_Description, 'General') AS HostDepartment
                FROM VMS_VisitLog v
                LEFT JOIN MST_UserMaster u ON v.HostEmployeeCode = u.EmployeeCode
                LEFT JOIN MST_Department dept ON CAST(u.DepartmentId AS VARCHAR) = dept.Department_ID
                WHERE v.QRToken = @Token";

            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@Token", token);
                using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    sda.Fill(dt);
                    return dt;
                }
            }
        }

        public DataTable GetCheckoutVisitor(int logId)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(CheckoutVisitorQuery, con))
            {
                cmd.Parameters.AddWithValue("@LogID", logId);
                using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    sda.Fill(dt);
                    return dt;
                }
            }
        }

        /// <summary>
        /// Atomically check in a visit (day-aware or legacy) and optionally queue arrival notification.
        /// When <paramref name="gateBranchId"/> is set (Specific Gate), VisitLog.TargetBranchID must match
        /// under the same transaction (AR-VMS-08B). Null = Universal Gate (no branch restriction).
        /// Arrival notification identity is loaded from VMS_VisitLog + MST_UserMaster for the mutated LogID
        /// (AR-VMS-09); page HiddenFields are not authoritative.
        /// </summary>
        public GateMutationResult CheckIn(
            int logId,
            int dayLogId,
            string guardId,
            int? gateBranchId = null)
        {
            string resolvedGuardId = guardId ?? "SecurityGate";

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (SqlTransaction trans = con.BeginTransaction())
                {
                    try
                    {
                        if (!TryAssertVisitBelongsToGateBranch(con, trans, logId, gateBranchId))
                        {
                            try
                            {
                                trans.Rollback();
                            }
                            catch
                            {
                                // Do not mask the controlled failure.
                            }

                            return new GateMutationResult
                            {
                                Succeeded = false,
                                FailureReason = GateMutationFailureReason.StateChanged,
                                LogId = logId
                            };
                        }

                        // AR-VMS-06A: when dayLogId is missing, resolve today's DayLog before
                        // legacy Parent-only Check-In. Unapproved today must fail closed.
                        if (dayLogId <= 0)
                        {
                            VisitDayLogInfo todayLog = VmsVisitScheduleHelper.GetTodayDayLog(con, logId, trans);
                            if (todayLog != null && todayLog.DayLogId > 0)
                            {
                                if (!todayLog.IsDayApproved)
                                {
                                    try
                                    {
                                        trans.Rollback();
                                    }
                                    catch
                                    {
                                        // Do not mask the controlled failure.
                                    }

                                    return new GateMutationResult
                                    {
                                        Succeeded = false,
                                        FailureReason = GateMutationFailureReason.StateChanged,
                                        LogId = logId
                                    };
                                }

                                dayLogId = todayLog.DayLogId;
                            }
                        }

                        bool transitionSucceeded;
                        if (dayLogId > 0)
                            transitionSucceeded = VmsVisitScheduleHelper.CheckInDay(con, dayLogId, logId, resolvedGuardId, trans);
                        else
                            transitionSucceeded = UpdateGateStatus(
                                con, trans, logId, "Checked-In", "ActualInTime", resolvedGuardId, "Scheduled", gateBranchId);

                        if (!transitionSucceeded)
                        {
                            try
                            {
                                trans.Rollback();
                            }
                            catch
                            {
                                // Do not mask the state-transition failure.
                            }

                            return new GateMutationResult
                            {
                                Succeeded = false,
                                FailureReason = GateMutationFailureReason.StateChanged,
                                LogId = logId
                            };
                        }

                        // AR-VMS-09: bind arrival notification to the mutated LogID (same pattern as Check-Out).
                        string visName = "";
                        string company = "";
                        string hostName = "";
                        string hostEmail = "";
                        LoadCheckoutVisitor(con, trans, logId, out visName, out company, out hostName, out hostEmail);

                        if (!string.IsNullOrEmpty(hostEmail))
                        {
                            NotifyHostArrival(con, trans, logId, hostName, hostEmail, visName, company);
                        }

                        trans.Commit();

                        return new GateMutationResult
                        {
                            Succeeded = true,
                            FailureReason = GateMutationFailureReason.None,
                            LogId = logId
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

        /// <summary>
        /// Atomically check out a visit (day-aware or legacy) and optionally queue departure notification.
        /// When <paramref name="gateBranchId"/> is set (Specific Gate), VisitLog.TargetBranchID must match
        /// under the same transaction (AR-VMS-08B). Null = Universal Gate (no branch restriction).
        /// </summary>
        public GateMutationResult CheckOut(int logId, int dayLogId, string guardId, int? gateBranchId = null)
        {
            string resolvedGuardId = guardId ?? "SecurityGate";

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (SqlTransaction trans = con.BeginTransaction())
                {
                    try
                    {
                        if (!TryAssertVisitBelongsToGateBranch(con, trans, logId, gateBranchId))
                        {
                            try
                            {
                                trans.Rollback();
                            }
                            catch
                            {
                                // Do not mask the controlled failure.
                            }

                            return new GateMutationResult
                            {
                                Succeeded = false,
                                FailureReason = GateMutationFailureReason.StateChanged,
                                LogId = logId
                            };
                        }

                        string visName = "";
                        string company = "";
                        string hostName = "";
                        string hostEmail = "";
                        LoadCheckoutVisitor(con, trans, logId, out visName, out company, out hostName, out hostEmail);

                        if (dayLogId <= 0)
                        {
                            VisitDayLogInfo todayLog = VmsVisitScheduleHelper.GetTodayDayLog(con, logId, trans);
                            if (todayLog != null)
                                dayLogId = todayLog.DayLogId;
                        }

                        bool transitionSucceeded;
                        if (dayLogId > 0)
                            transitionSucceeded = VmsVisitScheduleHelper.CheckOutDay(con, dayLogId, logId, resolvedGuardId, trans);
                        else
                            transitionSucceeded = UpdateGateStatus(
                                con, trans, logId, "Checked-Out", "ActualOutTime", resolvedGuardId, "Checked-In", gateBranchId);

                        if (!transitionSucceeded)
                        {
                            try
                            {
                                trans.Rollback();
                            }
                            catch
                            {
                                // Do not mask the state-transition failure.
                            }

                            return new GateMutationResult
                            {
                                Succeeded = false,
                                FailureReason = GateMutationFailureReason.StateChanged,
                                LogId = logId
                            };
                        }

                        if (!string.IsNullOrEmpty(hostEmail))
                        {
                            NotifyHostDeparture(con, trans, logId, hostName, hostEmail, visName, company);
                        }

                        trans.Commit();

                        return new GateMutationResult
                        {
                            Succeeded = true,
                            FailureReason = GateMutationFailureReason.None,
                            LogId = logId
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

        private const string CheckoutVisitorQuery = @"
                SELECT v.VisitorName, v.VisitorCompany, u.EmployeeName AS HostName, u.Email AS HostEmail 
                FROM VMS_VisitLog v 
                LEFT JOIN MST_UserMaster u ON v.HostEmployeeCode = u.EmployeeCode 
                WHERE v.LogID = @LogID";

        /// <summary>
        /// Specific Gate: UPDLOCK VisitLog and require TargetBranchID == gateBranchId.
        /// Universal Gate (null): no restriction.
        /// </summary>
        private static bool TryAssertVisitBelongsToGateBranch(
            SqlConnection con,
            SqlTransaction trans,
            int logId,
            int? gateBranchId)
        {
            if (!gateBranchId.HasValue)
                return true;

            using (SqlCommand cmd = new SqlCommand(@"
                SELECT TargetBranchID
                FROM VMS_VisitLog WITH (UPDLOCK, ROWLOCK)
                WHERE LogID = @LogID", con, trans))
            {
                cmd.Parameters.AddWithValue("@LogID", logId);
                object value = cmd.ExecuteScalar();
                if (value == null || value == DBNull.Value)
                    return false;

                int visitBranchId;
                if (!int.TryParse(value.ToString(), out visitBranchId))
                    return false;

                return visitBranchId == gateBranchId.Value;
            }
        }

        /// <summary>
        /// Loads visitor/host notification identity for a VisitLog row (Check-Out departure and
        /// Check-In arrival — AR-VMS-09). Bound to LogID under the caller's transaction.
        /// </summary>
        private static void LoadCheckoutVisitor(
            SqlConnection con,
            SqlTransaction trans,
            int logId,
            out string visName,
            out string company,
            out string hostName,
            out string hostEmail)
        {
            visName = "";
            company = "";
            hostName = "";
            hostEmail = "";

            using (SqlCommand cmd = new SqlCommand(CheckoutVisitorQuery, con, trans))
            {
                cmd.Parameters.AddWithValue("@LogID", logId);
                using (SqlDataReader sdr = cmd.ExecuteReader())
                {
                    if (sdr.Read())
                    {
                        visName = sdr["VisitorName"].ToString();
                        company = sdr["VisitorCompany"].ToString();
                        hostName = sdr["HostName"].ToString();
                        hostEmail = sdr["HostEmail"].ToString();
                    }
                }
            }
        }

        /// <summary>
        /// Legacy parent-only VisitLog status update. Requires expectedCurrentStatus,
        /// ApprovalStatus='Approved' (AR-VMS-07B), and rowsAffected == 1.
        /// When gateBranchId is set (Specific Gate), also requires TargetBranchID match (AR-VMS-08B).
        /// </summary>
        private static bool UpdateGateStatus(
            SqlConnection con,
            SqlTransaction trans,
            int logId,
            string newStatus,
            string timeColumn,
            string guardId,
            string expectedCurrentStatus,
            int? gateBranchId = null)
        {
            string branchPredicate = gateBranchId.HasValue
                ? " AND TargetBranchID = @GateBranchId"
                : string.Empty;

            string updateQuery =
                $"UPDATE VMS_VisitLog SET VisitStatus = @Status, {timeColumn} = GETDATE(), CheckedOutBy = @GuardId WHERE LogID = @LogID AND VisitStatus = @ExpectedStatus AND ApprovalStatus = 'Approved'{branchPredicate}";
            using (SqlCommand cmd = new SqlCommand(updateQuery, con, trans))
            {
                cmd.Parameters.AddWithValue("@Status", newStatus);
                cmd.Parameters.AddWithValue("@LogID", logId);
                cmd.Parameters.AddWithValue("@GuardId", guardId ?? "SecurityGate");
                cmd.Parameters.AddWithValue("@ExpectedStatus", expectedCurrentStatus);
                if (gateBranchId.HasValue)
                    cmd.Parameters.AddWithValue("@GateBranchId", gateBranchId.Value);
                return cmd.ExecuteNonQuery() == 1;
            }
        }

        private static void NotifyHostArrival(
            SqlConnection con,
            SqlTransaction trans,
            int logId,
            string hostName,
            string hostEmail,
            string visName,
            string company)
        {
            string subject = $"Your visitor {visName} has arrived at the gate";
            string body = $@"
            <div style='font-family: Arial; padding: 20px; border-left: 4px solid #1ABB9C;'>
                <h3 style='color: #1ABB9C;'>Visitor Arrival Notification</h3>
                <p>Hello <b>{hostName}</b>,</p>
                <p>Your approved visitor, <b>{visName}</b> from <b>{company}</b>, has successfully checked in at the security gate and is now entering the premises.</p>
            </div>";

            string qQuery = "INSERT INTO NotificationQueue (FormSubmissionId, EscalationLevel, RecipientEmail, RecipientName, Subject, Body, NotificationType, Status, SourceModule) VALUES (@LogId, 0, @Email, @Name, @Subject, @Body, 'Email', 'Pending', 'VMS_ARRIVAL')";
            using (SqlCommand qCmd = new SqlCommand(qQuery, con, trans))
            {
                qCmd.Parameters.AddWithValue("@LogId", logId);
                qCmd.Parameters.AddWithValue("@Email", hostEmail);
                qCmd.Parameters.AddWithValue("@Name", hostName);
                qCmd.Parameters.AddWithValue("@Subject", subject);
                qCmd.Parameters.AddWithValue("@Body", body);
                qCmd.ExecuteNonQuery();
            }
        }

        private static void NotifyHostDeparture(
            SqlConnection con,
            SqlTransaction trans,
            int logId,
            string hostName,
            string hostEmail,
            string visName,
            string company)
        {
            string subject = $"Visitor Departure: {visName} has left the premises";
            string body = $@"
            <div style='font-family: Arial; padding: 20px; border-left: 4px solid #f0ad4e;'>
                <h3 style='color: #f0ad4e;'>Visitor Departure Notification</h3>
                <p>Hello <b>{hostName}</b>,</p>
                <p>Your visitor, <b>{visName}</b> from <b>{company}</b>, has successfully checked out at the security gate and has left the premises.</p>
                <br />
                <p style='color: #777; font-size: 12px;'>Thank you for using the Anmol VMS.</p>
            </div>";

            string qQuery = "INSERT INTO NotificationQueue (FormSubmissionId, EscalationLevel, RecipientEmail, RecipientName, Subject, Body, NotificationType, Status, SourceModule) VALUES (@LogId, 0, @Email, @Name, @Subject, @Body, 'Email', 'Pending', 'VMS_DEPARTURE')";
            using (SqlCommand qCmd = new SqlCommand(qQuery, con, trans))
            {
                qCmd.Parameters.AddWithValue("@LogId", logId);
                qCmd.Parameters.AddWithValue("@Email", hostEmail);
                qCmd.Parameters.AddWithValue("@Name", hostName);
                qCmd.Parameters.AddWithValue("@Subject", subject);
                qCmd.Parameters.AddWithValue("@Body", body);
                qCmd.ExecuteNonQuery();
            }
        }
    }
}
