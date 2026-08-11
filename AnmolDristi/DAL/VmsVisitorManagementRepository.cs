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
    }

    public class VisitorApprovalResult
    {
        public bool Succeeded { get; set; }
        public ApprovalFailureReason FailureReason { get; set; }
        public int LogId { get; set; }
        public Guid? QrToken { get; set; }
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

        public void RejectVisitor(int logId)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string updateQuery =
                    "UPDATE VMS_VisitLog SET ApprovalStatus = 'Rejected', VisitStatus = 'Rejected', ApprovedOn = GETDATE() WHERE LogID = @LogID";

                using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                {
                    cmd.Parameters.AddWithValue("@LogID", logId);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }
            }
        }

        public void ManualCheckIn(int logId)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                // FIXED: Changed CheckInTime to ActualInTime
                string updateQuery =
                    "UPDATE VMS_VisitLog SET VisitStatus = 'Checked-In', ActualInTime = GETDATE() WHERE LogID = @LogID";

                using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                {
                    cmd.Parameters.AddWithValue("@LogID", logId);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }
            }
        }

        public void ManualCheckOut(int logId)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                // FIXED: Changed CheckOutTime to ActualOutTime
                string updateQuery =
                    "UPDATE VMS_VisitLog SET VisitStatus = 'Checked-Out', ActualOutTime = GETDATE() WHERE LogID = @LogID";

                using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                {
                    cmd.Parameters.AddWithValue("@LogID", logId);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }
            }
        }

        public VisitorApprovalResult ApprovePendingVisit(VisitorApprovalRequest request)
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

                        string updateQuery = @"
                            UPDATE VMS_VisitLog
                            SET ApprovalStatus = 'Approved',
                                VisitStatus = 'Scheduled',
                                QRToken = @Token,
                                ApprovedOn = GETDATE()
                            WHERE LogID = @LogID
                              AND HostEmployeeCode = @HostEmployeeCode
                              AND ApprovalStatus = 'Pending'";

                        int rowsAffected;
                        using (SqlCommand cmd = new SqlCommand(updateQuery, con, trans))
                        {
                            cmd.Parameters.AddWithValue("@Token", qrToken);
                            cmd.Parameters.AddWithValue("@LogID", request.LogId);
                            cmd.Parameters.AddWithValue("@HostEmployeeCode", hostEmployeeCode);
                            rowsAffected = cmd.ExecuteNonQuery();
                        }

                        if (rowsAffected != 1)
                        {
                            ApprovalFailureReason reason =
                                ResolveApprovalFailure(con, trans, request.LogId, hostEmployeeCode);

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

                        trans.Commit();

                        return new VisitorApprovalResult
                        {
                            Succeeded = true,
                            FailureReason = ApprovalFailureReason.None,
                            LogId = request.LogId,
                            QrToken = qrToken
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
            string hostEmployeeCode)
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

                    if (!string.Equals(rowHost, hostEmployeeCode, StringComparison.OrdinalIgnoreCase))
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
