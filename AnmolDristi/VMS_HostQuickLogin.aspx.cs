using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Configuration;
using System.Globalization;
using System.Web.UI.WebControls;
using AnmolDristi.DAL;

namespace AnmolDristi
{
    public partial class VMS_HostQuickLogin : System.Web.UI.Page
    {
        string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        private readonly VmsVisitorManagementRepository _repository = new VmsVisitorManagementRepository();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                ValidateAndLoadVisitor();
            }
        }

        private void ValidateAndLoadVisitor()
        {
            if (Request.QueryString["vid"] == null || Request.QueryString["t"] == null)
            {
                lblMessage.Text = "Invalid access link. Please check your email.";
                return;
            }

            string visitId = Request.QueryString["vid"];
            string encodedToken = Request.QueryString["t"];
            string decodedEmpCode = "";

            try
            {
                // Decode the Base64 Employee Code from the URL
                byte[] base64EncodedBytes = Convert.FromBase64String(encodedToken);
                decodedEmpCode = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            }
            catch
            {
                lblMessage.Text = "Malformed security token.";
                return;
            }

            using (SqlConnection con = new SqlConnection(connStr))
            {
                // Verify the Visit belongs to this exact decoded Employee Code
                string query = @"
                    SELECT VisitorName, VisitorCompany, PurposeOfVisit, ApprovalStatus, 
                           ISNULL(NULLIF(VisitorMobile, ''), VisitorEmail) AS ContactInfo,
                           ISNULL(ScheduleType, 'SingleDay') AS ScheduleType,
                           RequestedValidFrom, RequestedValidUpto
                    FROM VMS_VisitLog 
                    WHERE LogID = @VisitId AND HostEmployeeCode = @EmpCode";

                string scheduleType = "SingleDay";
                bool isPending = false;

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@VisitId", visitId);
                    cmd.Parameters.AddWithValue("@EmpCode", decodedEmpCode);

                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        if (sdr.Read())
                        {
                            string status = sdr["ApprovalStatus"].ToString();

                            if (status == "Pending")
                            {
                                // Show the visitor details and action buttons
                                lblVisName.Text = sdr["VisitorName"].ToString();
                                lblCompany.Text = sdr["VisitorCompany"].ToString();
                                lblPurpose.Text = sdr["PurposeOfVisit"].ToString();
                                lblContact.Text = sdr["ContactInfo"].ToString();
                                scheduleType = sdr["ScheduleType"].ToString();

                                if (sdr["RequestedValidFrom"] != DBNull.Value && sdr["RequestedValidUpto"] != DBNull.Value)
                                {
                                    DateTime reqFrom = Convert.ToDateTime(sdr["RequestedValidFrom"]);
                                    DateTime reqTo = Convert.ToDateTime(sdr["RequestedValidUpto"]);
                                    lblSchedule.Text = scheduleType == "MultiDay"
                                        ? $"{reqFrom:dd MMM yyyy} to {reqTo:dd MMM yyyy}, daily {reqFrom:hh:mm tt} - {reqTo:hh:mm tt}"
                                        : $"{reqFrom:dd MMM yyyy}, {reqFrom:hh:mm tt} - {reqTo:hh:mm tt}";
                                }
                                else
                                {
                                    lblSchedule.Text = "Not specified";
                                }

                                pnlAction.Visible = true;
                                lblMessage.Visible = false;
                                isPending = true;
                            }
                            else
                            {
                                // Pass is already processed (e.g. Host clicked the link twice, or Admin handled it)
                                lblMessage.CssClass = "success";
                                lblMessage.Text = $"This request has already been processed.<br/>Current Status: {status}";
                            }
                        }
                        else
                        {
                            lblMessage.Text = "Visitor not found or you are not authorized to view this request.";
                        }
                    }
                }

                if (isPending && scheduleType == "MultiDay")
                {
                    BindRequestedDays(con, visitId);
                }

                if (isPending)
                {
                    PrefillApprovedValidityTimes(con, visitId);
                }
            }
        }

        private void PrefillApprovedValidityTimes(SqlConnection con, string visitId)
        {
            TimeSpan plantOpen = new TimeSpan(9, 0, 0);
            TimeSpan plantClose = new TimeSpan(18, 0, 0);

            int branchId = 0;
            using (SqlCommand branchCmd = new SqlCommand(
                "SELECT ISNULL(TargetBranchID, 0) FROM VMS_VisitLog WHERE LogID = @LogID", con))
            {
                branchCmd.Parameters.AddWithValue("@LogID", visitId);
                object branchResult = branchCmd.ExecuteScalar();
                if (branchResult != null && branchResult != DBNull.Value)
                    branchId = Convert.ToInt32(branchResult);
            }

            if (branchId > 0)
            {
                using (SqlCommand timeCmd = new SqlCommand(@"
                    SELECT OpenTime, CloseTime
                    FROM VMS_PlantTimings
                    WHERE Branch_ID = @BranchID AND DayOfWeek = @DayOfWeek", con))
                {
                    timeCmd.Parameters.AddWithValue("@BranchID", branchId);
                    timeCmd.Parameters.AddWithValue("@DayOfWeek", DateTime.Now.ToString("dddd"));
                    using (SqlDataReader reader = timeCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (reader["OpenTime"] != DBNull.Value)
                                plantOpen = (TimeSpan)reader["OpenTime"];
                            if (reader["CloseTime"] != DBNull.Value)
                                plantClose = (TimeSpan)reader["CloseTime"];
                        }
                    }
                }
            }

            DateTime now = DateTime.Now;
            DateTime plantOpenToday = now.Date.Add(plantOpen);
            DateTime plantCloseToday = now.Date.Add(plantClose);

            if (now < plantOpenToday)
            {
                // Before hours: start validity from now, still end at plant close
                txtApprovedTimeFrom.Text = now.ToString("HH:mm");
                txtApprovedTimeTo.Text = plantClose.ToString(@"hh\:mm");
            }
            else if (now > plantCloseToday)
            {
                // After hours: start now and allow a short 2-hour window (same calendar day)
                txtApprovedTimeFrom.Text = now.ToString("HH:mm");
                DateTime afterHoursEnd = now.AddHours(2);
                if (afterHoursEnd.Date > now.Date)
                    afterHoursEnd = now.Date.AddHours(23).AddMinutes(59);
                txtApprovedTimeTo.Text = afterHoursEnd.ToString("HH:mm");
            }
            else
            {
                // Within plant hours: default to standard open/close
                txtApprovedTimeFrom.Text = plantOpen.ToString(@"hh\:mm");
                txtApprovedTimeTo.Text = plantClose.ToString(@"hh\:mm");
            }
        }

        private void BindRequestedDays(SqlConnection con, string visitId)
        {
            cblRequestedDays.Items.Clear();

            using (SqlCommand dayCmd = new SqlCommand(@"
                SELECT VisitDate
                FROM VMS_VisitDayLog
                WHERE LogID = @LogID
                ORDER BY VisitDate", con))
            {
                dayCmd.Parameters.AddWithValue("@LogID", visitId);
                using (SqlDataReader dayReader = dayCmd.ExecuteReader())
                {
                    while (dayReader.Read())
                    {
                        DateTime visitDate = Convert.ToDateTime(dayReader["VisitDate"]);
                        var item = new ListItem(visitDate.ToString("dd MMM yyyy"), visitDate.ToString("yyyy-MM-dd"));
                        item.Selected = true;
                        cblRequestedDays.Items.Add(item);
                    }
                }
            }

            cblRequestedDays.Visible = cblRequestedDays.Items.Count > 0;
        }

        protected void btnApprove_Click(object sender, EventArgs e)
        {
            if (cblRequestedDays.Visible)
            {
                bool anySelected = false;
                foreach (ListItem item in cblRequestedDays.Items)
                {
                    if (item.Selected)
                    {
                        anySelected = true;
                        break;
                    }
                }

                if (!anySelected)
                {
                    lblMessage.CssClass = "error";
                    lblMessage.Visible = true;
                    lblMessage.Text = "Please select at least one day to approve.";
                    return;
                }
            }

            TimeSpan approvedFromTime;
            TimeSpan approvedToTime;
            if (!TimeSpan.TryParse(txtApprovedTimeFrom.Text, out approvedFromTime) ||
                !TimeSpan.TryParse(txtApprovedTimeTo.Text, out approvedToTime))
            {
                lblMessage.CssClass = "error";
                lblMessage.Visible = true;
                lblMessage.Text = "Please enter valid From and Until times.";
                return;
            }

            if (approvedToTime <= approvedFromTime)
            {
                lblMessage.CssClass = "error";
                lblMessage.Visible = true;
                lblMessage.Text = "Valid Until must be after Valid From.";
                return;
            }

            ProcessApproval("Approved", "Scheduled", approvedFromTime, approvedToTime);
        }

        protected void btnReject_Click(object sender, EventArgs e)
        {
            ProcessApproval("Rejected", "Rejected");
        }

        private void ProcessApproval(
            string approvalStatus,
            string visitStatus,
            TimeSpan? approvedFromTime = null,
            TimeSpan? approvedToTime = null)
        {
            string visitId = Request.QueryString["vid"];

            if (approvalStatus == "Approved")
            {
                string hostEmployeeCode;
                if (!TryResolveHostEmployeeCode(out hostEmployeeCode))
                {
                    lblMessage.CssClass = "error";
                    lblMessage.Visible = true;
                    if (string.IsNullOrEmpty(lblMessage.Text))
                        lblMessage.Text = "Invalid access link. Please check your email.";
                    return;
                }

                int logId = Convert.ToInt32(visitId);

                IList<SelectedVisitDay> selectedDays = null;
                if (cblRequestedDays.Visible)
                {
                    selectedDays = new List<SelectedVisitDay>();
                    foreach (ListItem item in cblRequestedDays.Items)
                    {
                        DateTime visitDate = DateTime.ParseExact(item.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        selectedDays.Add(new SelectedVisitDay
                        {
                            VisitDate = visitDate.Date,
                            IsApproved = item.Selected
                        });
                    }
                }

                VisitorApprovalResult result = _repository.ApprovePendingVisit(new VisitorApprovalRequest
                {
                    LogId = logId,
                    HostEmployeeCode = hostEmployeeCode,
                    SelectedDays = selectedDays,
                    ApprovedFromTime = approvedFromTime,
                    ApprovedToTime = approvedToTime
                });

                if (!result.Succeeded)
                {
                    lblMessage.CssClass = "error";
                    lblMessage.Visible = true;
                    lblMessage.Text = FormatApprovalFailureMessage(result.FailureReason);
                    return;
                }

                pnlAction.Visible = false;
                lblMessage.Visible = true;
                lblMessage.CssClass = "success";
                lblMessage.Text = "Visitor Approved successfully! The digital gate pass has been sent to them.";
                return;
            }

            // AR-VMS-06B: Reject routes through canonical repository RejectVisitor
            // (Pending guard + DayLog cascade). Do not duplicate Parent SQL here.
            int rejectLogId = Convert.ToInt32(visitId);
            if (!_repository.RejectVisitor(rejectLogId))
            {
                lblMessage.CssClass = "error";
                lblMessage.Visible = true;
                lblMessage.Text = "Unable to reject this visit. It may already have been processed.";
                return;
            }

            pnlAction.Visible = false;
            lblMessage.Visible = true;
            lblMessage.CssClass = "success";
            lblMessage.Text = "Visitor request has been rejected.";
        }

        private bool TryResolveHostEmployeeCode(out string hostEmployeeCode)
        {
            hostEmployeeCode = string.Empty;

            if (Request.QueryString["t"] == null)
            {
                lblMessage.Text = "Invalid access link. Please check your email.";
                return false;
            }

            try
            {
                byte[] base64EncodedBytes = Convert.FromBase64String(Request.QueryString["t"]);
                hostEmployeeCode = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
                return !string.IsNullOrEmpty(hostEmployeeCode);
            }
            catch
            {
                lblMessage.Text = "Malformed security token.";
                return false;
            }
        }

        private static string FormatApprovalFailureMessage(ApprovalFailureReason reason)
        {
            switch (reason)
            {
                case ApprovalFailureReason.NotFoundOrNotOwned:
                    return "Visitor not found or you are not authorized to approve this request.";
                case ApprovalFailureReason.NotPending:
                    return "This request has already been processed.";
                default:
                    return "Unable to approve this visit. Please try again.";
            }
        }

        private void DispatchVisitorNotification_OBSOLETE(SqlConnection con, string visitId, Guid token)
        {
            string visEmail = "", visName = "", visMobile = "";

            // 1. Get both email and mobile details
            using (SqlCommand cmd = new SqlCommand("SELECT VisitorEmail, VisitorMobile, VisitorName FROM VMS_VisitLog WHERE LogID = @LogID", con))
            {
                cmd.Parameters.AddWithValue("@LogID", visitId);
                using (SqlDataReader sdr = cmd.ExecuteReader())
                {
                    if (sdr.Read())
                    {
                        visEmail = sdr["VisitorEmail"].ToString();
                        visMobile = sdr["VisitorMobile"].ToString();
                        visName = sdr["VisitorName"].ToString();
                    }
                }
            }

            string passUrl = VmsHelper.BuildDigitalPassUrl(token);

            // 2. Dispatch Email (If provided and valid)
            if (!string.IsNullOrEmpty(visEmail) && visEmail != "N/A")
            {
                string emailBody = $@"
                <div style='font-family: Arial; padding: 20px; border: 1px solid #1ABB9C; border-radius: 10px; text-align: center; max-width: 400px;'>
                    <h2 style='color: #1ABB9C;'>Visit Approved</h2>
                    <p>Hi {visName}, your host has approved your visit.</p>
                    <p>Click the link below to open your Digital Gate Pass and show it to the security guard.</p>
                    <br/>
                    <a href='{passUrl}' style='background-color: #1ABB9C; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>View Digital Pass</a>
                </div>";

                string qQuery = "INSERT INTO NotificationQueue (FormSubmissionId, EscalationLevel, RecipientEmail, RecipientName, Subject, Body, NotificationType, Status, SourceModule) VALUES (@LogId, 0, @Email, @Name, 'Your Anmol Gate Pass', @Body, 'Email', 'Pending', 'VMS_PASS_EMAIL')";
                using (SqlCommand qCmd = new SqlCommand(qQuery, con))
                {
                    qCmd.Parameters.AddWithValue("@LogId", visitId);
                    qCmd.Parameters.AddWithValue("@Email", visEmail);
                    qCmd.Parameters.AddWithValue("@Name", visName);
                    qCmd.Parameters.AddWithValue("@Body", emailBody);
                    qCmd.ExecuteNonQuery();
                }
            }

            // 3. Dispatch SMS (If mobile is available)
            if (!string.IsNullOrEmpty(visMobile))
            {
                // NOTE: Ensure you have an approved DLT Template in SMSCountry for this exact string format
                string smsBody = $"Your visit to Anmol Industries is approved. Click to view your Gate Pass: {passUrl}";

                string smsQuery = "INSERT INTO NotificationQueue (FormSubmissionId, EscalationLevel, RecipientEmail, RecipientName, Subject, Body, NotificationType, Status, SourceModule) VALUES (@LogId, 0, @Mobile, @Name, 'SMS', @Body, 'SMS', 'Pending', 'VMS_PASS_SMS')";
                using (SqlCommand smsCmd = new SqlCommand(smsQuery, con))
                {
                    smsCmd.Parameters.AddWithValue("@LogId", visitId);
                    smsCmd.Parameters.AddWithValue("@Mobile", visMobile); // Stored in the Email column of the queue for simplicity, or adjust schema
                    smsCmd.Parameters.AddWithValue("@Name", visName);
                    smsCmd.Parameters.AddWithValue("@Body", smsBody);
                    smsCmd.ExecuteNonQuery();
                }
            }
        }

        private void DispatchVisitorEmail(SqlConnection con, string visitId, Guid token)
        {
            string visEmail = "", visName = "";

            // Get visitor email details
            using (SqlCommand cmd = new SqlCommand("SELECT VisitorEmail, VisitorName FROM VMS_VisitLog WHERE LogID = @LogID", con))
            {
                cmd.Parameters.AddWithValue("@LogID", visitId);
                using (SqlDataReader sdr = cmd.ExecuteReader())
                {
                    if (sdr.Read())
                    {
                        visEmail = sdr["VisitorEmail"].ToString();
                        visName = sdr["VisitorName"].ToString();
                    }
                }
            }

            if (!string.IsNullOrEmpty(visEmail))
            {
                string passUrl = VmsHelper.BuildDigitalPassUrl(token);
                string body = $@"
                    <div style='font-family: Arial; padding: 20px; border: 1px solid #1ABB9C; border-radius: 10px; text-align: center; max-width: 400px;'>
                        <h2 style='color: #1ABB9C;'>Visit Approved</h2>
                        <p>Hi {visName}, your host has approved your visit.</p>
                        <p>Click the link below to open your Digital Gate Pass and show it to the security guard.</p>
                        <br/>
                        <a href='{passUrl}' style='background-color: #1ABB9C; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>View Digital Pass</a>
                    </div>";

                string qQuery = "INSERT INTO NotificationQueue (FormSubmissionId, EscalationLevel, RecipientEmail, RecipientName, Subject, Body, NotificationType, Status, SourceModule) VALUES (@LogId, 0, @Email, @Name, 'Your Anmol Gate Pass', @Body, 'Email', 'Pending', 'VMS_PASS')";
                using (SqlCommand qCmd = new SqlCommand(qQuery, con))
                {
                    qCmd.Parameters.AddWithValue("@LogId", visitId);
                    qCmd.Parameters.AddWithValue("@Email", visEmail);
                    qCmd.Parameters.AddWithValue("@Name", visName);
                    qCmd.Parameters.AddWithValue("@Body", body);
                    qCmd.ExecuteNonQuery();
                }
            }
        }
    }
}