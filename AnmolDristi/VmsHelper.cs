using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web;

namespace AnmolDristi
{
    /// <summary>
    /// Shared VMS logic for visitor login, registration, approval, and gate pass flows.
    /// BranchId in QR codes maps to MST_Branch.Branch_ID (not MST_PlantDetails.plant_id).
    /// </summary>
    public static class VmsHelper
    {
        private const string DefaultProductionBaseUrl = "https://apps.anmolindustries.com";

        private static string ConnStr =>
            ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        /// <summary>
        /// Public site base URL for VMS links (emails, QR PDFs, SMS).
        /// Configure via appSettings key VmsPublicBaseUrl:
        /// - https://apps.anmolindustries.com (LIVE, default)
        /// - https://appsuat.anmolindustries.com (UAT)
        /// - auto (derive from the current HTTP request host)
        /// </summary>
        public static string GetPublicBaseUrl()
        {
            string configured = ConfigurationManager.AppSettings["VmsPublicBaseUrl"];

            if (!string.IsNullOrWhiteSpace(configured))
            {
                configured = configured.Trim();

                if (configured.Equals("auto", StringComparison.OrdinalIgnoreCase))
                {
                    string fromRequest = GetBaseUrlFromCurrentRequest();
                    if (!string.IsNullOrEmpty(fromRequest))
                        return fromRequest;
                }
                else
                {
                    return configured.TrimEnd('/');
                }
            }

            return DefaultProductionBaseUrl;
        }

        public static string BuildPublicUrl(string pageAndQuery)
        {
            if (string.IsNullOrWhiteSpace(pageAndQuery))
                throw new ArgumentException("Page path is required.", nameof(pageAndQuery));

            return $"{GetPublicBaseUrl()}/{pageAndQuery.TrimStart('/')}";
        }

        public static string BuildDigitalPassUrl(Guid qrToken)
        {
            return BuildPublicUrl($"VMS_DigitalPass.aspx?token={qrToken}");
        }

        /// <summary>
        /// Public HostQuickLogin capability URL (AR-VMS-13).
        /// Issues a visit-bound, expiring, MachineKey-protected token as query parameter t.
        /// Callers must pass the raw HostEmployeeCode (not Base64).
        /// </summary>
        public static string BuildHostQuickLoginUrl(int logId, string hostEmployeeCode)
        {
            string token = VmsHostQuickLoginToken.Create(logId, hostEmployeeCode);
            return BuildPublicUrl($"VMS_HostQuickLogin.aspx?vid={logId}&t={token}");
        }

        public static string BuildSelfLoginUrl(int branchId)
        {
            return BuildPublicUrl($"VMS_SelfLogin.aspx?BranchId={branchId}");
        }

        public static string BuildGateScannerUrl(int branchId)
        {
            return BuildPublicUrl($"VMS_GateScanner.aspx?BranchId={branchId}");
        }

        private static string GetBaseUrlFromCurrentRequest()
        {
            HttpRequest request = HttpContext.Current?.Request;
            if (request?.Url == null)
                return null;

            return $"{request.Url.Scheme}://{request.Url.Authority}";
        }

        public static string NormalizeEmail(string email)
        {
            return string.IsNullOrWhiteSpace(email) ? "N/A" : email.Trim();
        }

        public static string NormalizeMobile(string mobile)
        {
            return string.IsNullOrWhiteSpace(mobile) ? "N/A" : mobile.Trim();
        }

        public static string GetBranchDisplayName(int branchId)
        {
            using (SqlConnection con = new SqlConnection(ConnStr))
            {
                string query = @"
                    SELECT ISNULL(s.AliasName, b.Branch_Description) AS DisplayName
                    FROM MST_Branch b
                    LEFT JOIN VMS_OfficeSettings s ON b.Branch_ID = s.Branch_ID
                    WHERE b.Branch_ID = @BranchId";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@BranchId", branchId);
                    con.Open();
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        return result.ToString();
                }
            }

            return "our Facility";
        }

        // FIX-1 (2026-08-20): Host-aware duplicate check.
        // What: Added optional hostEmployeeCode parameter.
        // Why:  Business rule requires blocking duplicate requests against the SAME
        //       host at the same branch, but allowing requests to a DIFFERENT host.

        /// <summary>
        /// When hostEmployeeCode is provided, the check is scoped to that specific host.
        /// </summary>
        public static bool HasActiveVisit(string mobile, string email, int branchId, string hostEmployeeCode = null)
        {
            return GetActiveVisitLogId(mobile, email, branchId, hostEmployeeCode).HasValue;
        }

        public static int? GetActiveVisitLogId(string mobile, string email, int branchId, string hostEmployeeCode = null)
        {
            string normalizedMobile = NormalizeMobile(mobile);
            string normalizedEmail = NormalizeEmail(email);

            using (SqlConnection con = new SqlConnection(ConnStr))
            {
                string hostFilter = !string.IsNullOrWhiteSpace(hostEmployeeCode)
                    ? "AND HostEmployeeCode = @HostEmployeeCode"
                    : string.Empty;

                string query = $@"
                    SELECT TOP 1 LogID
                    FROM VMS_VisitLog
                    WHERE TargetBranchID = @BranchId
                      AND (
                            (ApprovalStatus = 'Pending' AND ISNULL(RequestedValidUpto, ValidUpto) >= GETDATE())
                            OR (ApprovalStatus = 'Approved' AND ValidUpto >= GETDATE() AND ISNULL(VisitStatus, '') <> 'Checked-Out')
                          )
                      AND (
                            VisitorMobile = @Mobile
                            OR (@Email <> 'N/A' AND VisitorEmail = @Email)
                          )
                      {hostFilter}";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Mobile", normalizedMobile);
                    cmd.Parameters.AddWithValue("@Email", normalizedEmail);
                    cmd.Parameters.AddWithValue("@BranchId", branchId);
                    if (!string.IsNullOrWhiteSpace(hostEmployeeCode))
                        cmd.Parameters.AddWithValue("@HostEmployeeCode", hostEmployeeCode.Trim());

                    con.Open();
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        return Convert.ToInt32(result);
                }
            }

            return null;
        }

        public static bool IsFacilityOpen(int branchId, out string closedMessage)
        {
            closedMessage = null;
            DateTime now = DateTime.Now;
            string currentDay = now.ToString("dddd");

            using (SqlConnection con = new SqlConnection(ConnStr))
            {
                string query = @"
                    SELECT OpenTime, CloseTime
                    FROM VMS_PlantTimings
                    WHERE Branch_ID = @BranchID AND DayOfWeek = @DayOfWeek";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@BranchID", branchId);
                    cmd.Parameters.AddWithValue("@DayOfWeek", currentDay);
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            return true;

                        if (reader["OpenTime"] != DBNull.Value)
                        {
                            TimeSpan openTime = (TimeSpan)reader["OpenTime"];
                            DateTime openDateTime = new DateTime(
                                now.Year, now.Month, now.Day,
                                openTime.Hours, openTime.Minutes, openTime.Seconds);

                            if (now < openDateTime)
                            {
                                closedMessage =
                                    $"Visiting hours have not started yet. Please return after {openDateTime:hh:mm tt}.";
                                return false;
                            }
                        }

                        if (reader["CloseTime"] != DBNull.Value)
                        {
                            TimeSpan closeTime = (TimeSpan)reader["CloseTime"];
                            DateTime closeDateTime = new DateTime(
                                now.Year, now.Month, now.Day,
                                closeTime.Hours, closeTime.Minutes, closeTime.Seconds);

                            if (now >= closeDateTime)
                            {
                                closedMessage =
                                    "Our visiting hours for today have concluded. Please coordinate with your host to schedule a visit for our next working day.";
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        public static DateTime CalculateValidUpto(
            int branchId,
            DateTime validFrom,
            SqlConnection con,
            SqlTransaction trans = null)
        {
            DateTime validUpto = validFrom.AddHours(8);
            string currentDay = validFrom.ToString("dddd");

            string timingQuery = @"
                SELECT OpenTime, CloseTime
                FROM VMS_PlantTimings
                WHERE Branch_ID = @BranchID AND DayOfWeek = @DayOfWeek";

            using (SqlCommand timeCmd = new SqlCommand(timingQuery, con, trans))
            {
                timeCmd.Parameters.AddWithValue("@BranchID", branchId);
                timeCmd.Parameters.AddWithValue("@DayOfWeek", currentDay);

                using (SqlDataReader reader = timeCmd.ExecuteReader())
                {
                    if (reader.Read() && reader["CloseTime"] != DBNull.Value)
                    {
                        TimeSpan closingTime = (TimeSpan)reader["CloseTime"];
                        validUpto = new DateTime(
                            validFrom.Year, validFrom.Month, validFrom.Day,
                            closingTime.Hours, closingTime.Minutes, closingTime.Seconds);
                    }
                }
            }

            return validUpto;
        }

        public static void ValidateWithinVisitingHours(
            int branchId,
            DateTime validFrom,
            SqlConnection con,
            SqlTransaction trans = null)
        {
            string currentDay = validFrom.ToString("dddd");

            string timingQuery = @"
                SELECT OpenTime, CloseTime
                FROM VMS_PlantTimings
                WHERE Branch_ID = @BranchID AND DayOfWeek = @DayOfWeek";

            using (SqlCommand timeCmd = new SqlCommand(timingQuery, con, trans))
            {
                timeCmd.Parameters.AddWithValue("@BranchID", branchId);
                timeCmd.Parameters.AddWithValue("@DayOfWeek", currentDay);

                using (SqlDataReader reader = timeCmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return;

                    if (reader["OpenTime"] != DBNull.Value)
                    {
                        TimeSpan openTime = (TimeSpan)reader["OpenTime"];
                        DateTime openDateTime = new DateTime(
                            validFrom.Year, validFrom.Month, validFrom.Day,
                            openTime.Hours, openTime.Minutes, openTime.Seconds);

                        if (validFrom < openDateTime)
                        {
                            throw new Exception(
                                $"Registration denied. Visiting hours start at {openDateTime:hh:mm tt}.");
                        }
                    }

                    if (reader["CloseTime"] != DBNull.Value)
                    {
                        TimeSpan closingTime = (TimeSpan)reader["CloseTime"];
                        DateTime validUpto = new DateTime(
                            validFrom.Year, validFrom.Month, validFrom.Day,
                            closingTime.Hours, closingTime.Minutes, closingTime.Seconds);

                        if (validFrom >= validUpto)
                        {
                            throw new Exception(
                                $"Registration denied. The visiting hours for this facility ended at {validUpto:hh:mm tt}.");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Formats selectively approved visit days for multi-day visits (e.g. "12-Nov, 14-Nov").
        /// Returns null for standard single-day passes.
        /// </summary>
        public static string GetMultiDayApprovedDatesText(
            SqlConnection con,
            int logId,
            SqlTransaction trans = null)
        {
            string scheduleType = "SingleDay";
            using (SqlCommand typeCmd = new SqlCommand(
                "SELECT ISNULL(ScheduleType, 'SingleDay') FROM VMS_VisitLog WHERE LogID = @LogID", con, trans))
            {
                typeCmd.Parameters.AddWithValue("@LogID", logId);
                object result = typeCmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                    scheduleType = result.ToString();
            }

            var approvedDates = new List<DateTime>();
            using (SqlCommand dayCmd = new SqlCommand(@"
                SELECT VisitDate
                FROM VMS_VisitDayLog
                WHERE LogID = @LogID AND IsDayApproved = 1
                ORDER BY VisitDate", con, trans))
            {
                dayCmd.Parameters.AddWithValue("@LogID", logId);
                using (SqlDataReader reader = dayCmd.ExecuteReader())
                {
                    while (reader.Read())
                        approvedDates.Add(Convert.ToDateTime(reader["VisitDate"]));
                }
            }

            bool isMultiDay = scheduleType == "MultiDay" || approvedDates.Count > 1;
            if (!isMultiDay || approvedDates.Count == 0)
                return null;

            return string.Join(", ", approvedDates.ConvertAll(d => d.ToString("dd-MMM")));
        }

        public static void QueueVisitorPassNotification(
            SqlConnection con,
            int visitId,
            Guid qrToken,
            string visitorName,
            string visitorEmail,
            string visitorMobile,
            SqlTransaction trans = null)
        {
            string passUrl = BuildDigitalPassUrl(qrToken);
            string approvedDatesText = GetMultiDayApprovedDatesText(con, visitId, trans);
            string approvedDatesHtml = string.IsNullOrEmpty(approvedDatesText)
                ? ""
                : $"<p><b>Approved Dates:</b> {approvedDatesText}</p>";

            string emailBody = $@"
                <div style='font-family: Arial; padding: 20px; border: 1px solid #1ABB9C; border-radius: 10px; text-align: center; max-width: 400px;'>
                    <h2 style='color: #1ABB9C;'>Visit Approved</h2>
                    <p>Hi {visitorName}, your visit has been approved.</p>
                    {approvedDatesHtml}
                    <p>Click the link below to open your Digital Gate Pass and show it to the security guard.</p>
                    <br/>
                    <a href='{passUrl}' style='background-color: #1ABB9C; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>View Digital Pass</a>
                </div>";

            string normalizedEmail = NormalizeEmail(visitorEmail);
            if (normalizedEmail != "N/A")
            {
                string emailQuery = @"
                    INSERT INTO NotificationQueue
                    (FormSubmissionId, EscalationLevel, RecipientEmail, RecipientName, Subject, Body, NotificationType, Status, SourceModule)
                    VALUES
                    (@LogId, 0, @Email, @Name, 'Your Anmol Gate Pass', @Body, 'Email', 'Pending', 'VMS_PASS')";

                using (SqlCommand qCmd = new SqlCommand(emailQuery, con, trans))
                {
                    qCmd.Parameters.AddWithValue("@LogId", visitId);
                    qCmd.Parameters.AddWithValue("@Email", normalizedEmail);
                    qCmd.Parameters.AddWithValue("@Name", visitorName);
                    qCmd.Parameters.AddWithValue("@Body", emailBody);
                    qCmd.ExecuteNonQuery();
                }
            }

            string normalizedMobile = NormalizeMobile(visitorMobile);
            if (normalizedMobile != "N/A")
            {
                string smsBody = $"Your visit to Anmol Industries is approved. View your Gate Pass: {passUrl}";
                string smsQuery = @"
                    INSERT INTO NotificationQueue
                    (FormSubmissionId, EscalationLevel, RecipientEmail, RecipientName, Subject, Body, NotificationType, Status, SourceModule)
                    VALUES
                    (@LogId, 0, @Mobile, @Name, 'SMS', @Body, 'SMS', 'Pending', 'VMS_PASS_SMS')";

                using (SqlCommand smsCmd = new SqlCommand(smsQuery, con, trans))
                {
                    smsCmd.Parameters.AddWithValue("@LogId", visitId);
                    smsCmd.Parameters.AddWithValue("@Mobile", normalizedMobile);
                    smsCmd.Parameters.AddWithValue("@Name", visitorName);
                    smsCmd.Parameters.AddWithValue("@Body", smsBody);
                    smsCmd.ExecuteNonQuery();
                }
            }
        }
    }
}
