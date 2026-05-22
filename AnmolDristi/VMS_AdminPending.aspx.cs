using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.UI.WebControls;
using System.Drawing;
using System.Web.UI;

namespace AnmolDristi
{
    public partial class VMS_AdminPending : System.Web.UI.Page
    {
        string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["WORKMAN"] == null)
                {
                    Response.Redirect("Login.aspx");
                    return;
                }

                LoadBranches();
                BindAllGrids();
            }
        }

        private void KeepTabActive(string tabId)
        {
            ScriptManager.RegisterStartupScript(this, this.GetType(), "FocusTab", $"$('#adminTabs a[href=\"#{tabId}\"]').tab('show');", true);
        }

        private void LoadBranches()
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                string query = "SELECT Branch_ID, Branch_Description FROM MST_Branch ORDER BY Branch_Description";
                using (SqlDataAdapter sda = new SqlDataAdapter(query, con))
                {
                    DataTable dt = new DataTable();
                    sda.Fill(dt);
                    ddlBranchFilter.DataSource = dt;
                    ddlBranchFilter.DataTextField = "Branch_Description";
                    ddlBranchFilter.DataValueField = "Branch_ID";
                    ddlBranchFilter.DataBind();
                    ddlBranchFilter.Items.Insert(0, new ListItem("-- All Branches --", "0"));
                }
            }
        }

        protected void ddlBranchFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindAllGrids();
            KeepTabActive("tabGateOverride");
        }

        private void BindAllGrids()
        {
            string branchFilter = ddlBranchFilter.SelectedValue;

            using (SqlConnection con = new SqlConnection(connStr))
            {
                string pendingQuery = @"
                SELECT 
                    v.LogID, v.VisitorName, v.VisitorEmail, v.VisitorCompany, v.PhotoPath,
                    v.PurposeOfVisit, v.CreatedDate, u.EmployeeName AS HostName, u.EmployeeCode AS HostEmployeeCode,
                    u.Email AS HostEmail, ISNULL(b.Branch_Description, 'Unknown Location') AS Branch_Description
                FROM VMS_VisitLog v
                LEFT JOIN MST_UserMaster u ON v.HostEmployeeCode = u.EmployeeCode
                LEFT JOIN MST_Branch b ON v.TargetBranchID = b.Branch_ID
                WHERE v.ApprovalStatus = 'Pending'
                ORDER BY v.CreatedDate DESC";

                using (SqlDataAdapter sda = new SqlDataAdapter(pendingQuery, con))
                {
                    DataTable dtPending = new DataTable();
                    sda.Fill(dtPending);
                    gvAdminPending.DataSource = dtPending;
                    gvAdminPending.DataBind();
                    gvOnBehalf.DataSource = dtPending;
                    gvOnBehalf.DataBind();
                }

                string activeQuery = @"
                SELECT 
                    v.LogID, v.VisitorName, v.VisitorEmail, v.VisitStatus, v.PhotoPath,
                    v.ActualInTime, v.ActualOutTime, v.ValidUpto,
                    v.CreatedDate, v.ApprovedOn,
                    u.EmployeeName AS HostName, u.DepartmentId, 
                    u.EmployeeCode AS HostEmployeeCode, u.Email AS HostEmail
                FROM VMS_VisitLog v
                LEFT JOIN MST_UserMaster u ON v.HostEmployeeCode = u.EmployeeCode
                WHERE v.ApprovalStatus = 'Approved' 
                  AND v.VisitStatus IN ('Scheduled', 'Checked-In') " +
                          (branchFilter != "0" ? " AND v.TargetBranchID = @BranchID " : "") +
                        " ORDER BY v.ApprovedOn DESC";

                using (SqlCommand cmd = new SqlCommand(activeQuery, con))
                {
                    if (branchFilter != "0") cmd.Parameters.AddWithValue("@BranchID", branchFilter);
                    using (SqlDataAdapter sdaActive = new SqlDataAdapter(cmd))
                    {
                        DataTable dtActive = new DataTable();
                        sdaActive.Fill(dtActive);
                        gvAdminGateOverride.DataSource = dtActive;
                        gvAdminGateOverride.DataBind();
                    }
                }
            }
        }

        // ==========================================
        // TAB 3: INLINE AUTO-SAVE LOGIC (STRICT CYCLE & AUDIT ENFORCEMENT)
        // CYCLE: Request <= Approval <= Check-In <= Check-Out <= Validity
        // ==========================================
        protected void InlineEdit_TextChanged(object sender, EventArgs e)
        {
            TextBox txtChanged = (TextBox)sender;
            GridViewRow row = (GridViewRow)txtChanged.NamingContainer;

            int logId = Convert.ToInt32(gvAdminGateOverride.DataKeys[row.RowIndex].Values["LogID"]);
            string visName = gvAdminGateOverride.DataKeys[row.RowIndex].Values["VisitorName"].ToString();

            TextBox txtReason = (TextBox)row.FindControl("txtOverrideReason");
            string reason = txtReason.Text.Trim();

            // 1. Enforce Audit Reason
            if (string.IsNullOrEmpty(reason))
            {
                ShowValidationError("You must type an Audit Reason BEFORE changing the date/time.");
                return;
            }

            // 2. Parse the New Date
            DateTime newTime;
            if (!DateTime.TryParse(txtChanged.Text, out newTime))
            {
                ShowValidationError("Invalid time format selected.");
                return;
            }

            // 3. Fetch entire lifecycle timeline for Strict Boundary Checkpoints
            DateTime? createdDate = null;
            DateTime? approvedOn = null;
            DateTime? currentInTime = null;
            DateTime? currentOutTime = null;
            DateTime? currentValidUpto = null;

            using (SqlConnection con = new SqlConnection(connStr))
            {
                string checkQuery = "SELECT CreatedDate, ApprovedOn, ActualInTime, ActualOutTime, ValidUpto FROM VMS_VisitLog WHERE LogID = @LogID";
                using (SqlCommand cmd = new SqlCommand(checkQuery, con))
                {
                    cmd.Parameters.AddWithValue("@LogID", logId);
                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        if (sdr.Read())
                        {
                            if (sdr["CreatedDate"] != DBNull.Value) createdDate = Convert.ToDateTime(sdr["CreatedDate"]);
                            if (sdr["ApprovedOn"] != DBNull.Value) approvedOn = Convert.ToDateTime(sdr["ApprovedOn"]);
                            if (sdr["ActualInTime"] != DBNull.Value) currentInTime = Convert.ToDateTime(sdr["ActualInTime"]);
                            if (sdr["ActualOutTime"] != DBNull.Value) currentOutTime = Convert.ToDateTime(sdr["ActualOutTime"]);
                            if (sdr["ValidUpto"] != DBNull.Value) currentValidUpto = Convert.ToDateTime(sdr["ValidUpto"]);
                        }
                    }
                }
            }

            // 4. ENFORCE STRICT CYCLE (Req -> App -> In -> Out -> Validity)
            string dbColumnName = "";
            string statusUpdateLogic = "";
            string friendlyName = "";
            string oldTimeRecord = "Empty"; // For enhanced auditing

            if (txtChanged.ID == "txtInTime")
            {
                // In-Time Boundaries
                if (approvedOn.HasValue && newTime < approvedOn.Value)
                { ShowValidationError($"Check-In ({newTime:HH:mm}) cannot be BEFORE Approval ({approvedOn.Value:HH:mm})."); return; }
                if (newTime > DateTime.Now)
                { ShowValidationError("Check-In cannot be set in the future."); return; }
                if (currentOutTime.HasValue && newTime > currentOutTime.Value)
                { ShowValidationError($"Check-In cannot be AFTER the existing Check-Out ({currentOutTime.Value:HH:mm})."); return; }
                if (currentValidUpto.HasValue && newTime > currentValidUpto.Value)
                { ShowValidationError($"Check-In cannot be AFTER Pass Expiry ({currentValidUpto.Value:HH:mm}). Please extend validity first."); return; }

                dbColumnName = "ActualInTime";
                friendlyName = "Check-In Time";
                statusUpdateLogic = ", VisitStatus = 'Checked-In'";
                if (currentInTime.HasValue) oldTimeRecord = currentInTime.Value.ToString("dd-MMM HH:mm");
            }
            else if (txtChanged.ID == "txtOutTime")
            {
                // Out-Time Boundaries
                if (!currentInTime.HasValue)
                { ShowValidationError("Cannot set Check-Out. Visitor has no Check-In time yet."); return; }
                if (newTime < currentInTime.Value)
                { ShowValidationError($"Check-Out ({newTime:HH:mm}) cannot be BEFORE Check-In ({currentInTime.Value:HH:mm})."); return; }
                if (newTime > DateTime.Now)
                { ShowValidationError("Check-Out cannot be set in the future."); return; }
                if (currentValidUpto.HasValue && newTime > currentValidUpto.Value)
                { ShowValidationError($"Check-Out ({newTime:HH:mm}) cannot be AFTER Pass Expiry ({currentValidUpto.Value:HH:mm}). Extend validity first."); return; }

                dbColumnName = "ActualOutTime";
                friendlyName = "Check-Out Time";
                statusUpdateLogic = ", VisitStatus = 'Checked-Out', CheckedOutBy = 'ADMIN_INLINE'";
                if (currentOutTime.HasValue) oldTimeRecord = currentOutTime.Value.ToString("dd-MMM HH:mm");
            }
            else if (txtChanged.ID == "txtValidUpto")
            {
                // Validity Boundaries
                if (currentOutTime.HasValue && newTime < currentOutTime.Value)
                { ShowValidationError($"Pass Expiry cannot be set BEFORE the existing Check-Out ({currentOutTime.Value:HH:mm})."); return; }
                else if (currentInTime.HasValue && newTime < currentInTime.Value)
                { ShowValidationError($"Pass Expiry cannot be set BEFORE the existing Check-In ({currentInTime.Value:HH:mm})."); return; }
                else if (approvedOn.HasValue && newTime < approvedOn.Value)
                { ShowValidationError($"Pass Expiry cannot be set BEFORE the Approval Time ({approvedOn.Value:HH:mm})."); return; }

                dbColumnName = "ValidUpto";
                friendlyName = "Pass Validity";
                statusUpdateLogic = "";
                if (currentValidUpto.HasValue) oldTimeRecord = currentValidUpto.Value.ToString("dd-MMM HH:mm");
            }

            // 5. BUILD ENHANCED AUDIT TRAIL (Captures the transition from Old to New)
            string adminName = Session["UserName"] != null ? Session["UserName"].ToString() : "Admin";
            string auditTrail = $" | [{friendlyName} changed: {oldTimeRecord} -> {newTime:dd-MMM HH:mm} by {adminName}] Reason: {reason}";

            // 6. EXECUTE SECURE UPDATE
            using (SqlConnection con = new SqlConnection(connStr))
            {
                string updateQuery = $@"
                    UPDATE VMS_VisitLog 
                    SET {dbColumnName} = @NewTime 
                        {statusUpdateLogic}, 
                        ApprovalRemarks = ISNULL(ApprovalRemarks, '') + @AuditTrail 
                    WHERE LogID = @LogID";

                using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                {
                    cmd.Parameters.AddWithValue("@NewTime", newTime);
                    cmd.Parameters.AddWithValue("@AuditTrail", auditTrail);
                    cmd.Parameters.AddWithValue("@LogID", logId);

                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }
            }

            lblMessage.ForeColor = System.Drawing.Color.Green;
            lblMessage.Text = $"<i class='fa fa-check-circle'></i> Auto-Saved: {friendlyName} successfully updated for {visName}.";

            BindAllGrids();
            KeepTabActive("tabGateOverride");
        }

        private void ShowValidationError(string message)
        {
            lblMessage.ForeColor = System.Drawing.Color.Red;
            lblMessage.Text = $"<i class='fa fa-exclamation-triangle'></i> Update Cancelled: {message}";
            BindAllGrids();
            KeepTabActive("tabGateOverride");
        }

        // ==========================================
        // TAB 1: REMINDER LOGIC (RESTORED EMAIL)
        // ==========================================
        protected void gvAdminPending_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "Retrigger")
            {
                int logId = Convert.ToInt32(e.CommandArgument);
                try
                {
                    SendReminderToHost(logId);
                    lblMessage.ForeColor = Color.Green;
                    lblMessage.Text = $"<i class='fa fa-check-circle'></i> Reminder successfully queued for Log ID: {logId}.";
                }
                catch (Exception ex)
                {
                    lblMessage.ForeColor = Color.Red;
                    lblMessage.Text = $"<i class='fa fa-exclamation-triangle'></i> Error: {ex.Message}";
                }
                BindAllGrids();
                KeepTabActive("tabReminders");
            }
        }

        private void SendReminderToHost(int logId)
        {
            string hostEmail = "", hostName = "", visitorName = "", company = "", hostEmpCode = "";

            using (SqlConnection con = new SqlConnection(connStr))
            {
                string query = @"
                    SELECT v.VisitorName, v.VisitorCompany, v.HostEmployeeCode,
                           u.EmployeeName AS HostName, u.Email AS HostEmail
                    FROM VMS_VisitLog v
                    LEFT JOIN MST_UserMaster u ON v.HostEmployeeCode = u.EmployeeCode
                    WHERE v.LogID = @LogID AND v.ApprovalStatus = 'Pending'";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@LogID", logId);
                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        if (sdr.Read())
                        {
                            visitorName = sdr["VisitorName"].ToString();
                            company = sdr["VisitorCompany"].ToString();
                            hostName = sdr["HostName"].ToString();
                            hostEmail = sdr["HostEmail"].ToString();
                            hostEmpCode = sdr["HostEmployeeCode"].ToString();
                        }
                    }
                }

                if (!string.IsNullOrEmpty(hostEmail) && !string.IsNullOrEmpty(hostEmpCode))
                {
                    string subject = $"[URGENT] Approval Needed: Visitor {visitorName} is waiting";

                    byte[] plainTextBytes = System.Text.Encoding.UTF8.GetBytes(hostEmpCode);
                    string encodedEmpCode = Convert.ToBase64String(plainTextBytes);

                    string quickApproveLink = $"https://apps.anmolindustries.com/VMS_HostQuickLogin.aspx?vid={logId}&t={encodedEmpCode}";

                    string body = $@"
                        <div style='padding:20px; border: 2px solid #f0ad4e; font-family: Arial, sans-serif;'>
                            <h3 style='color:#f0ad4e;'>Reminder: Visitor Waiting</h3>
                            <p>Hello <b>{hostName}</b>,</p>
                            <p>Priority reminder: <b>{visitorName}</b> from <b>{company}</b> is waiting for approval.</p>
                            <br/>
                            <a href='{quickApproveLink}' style='background: #1ABB9C; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block;'>Click Here to Verify & Approve</a>
                        </div>";

                    string qQuery = "INSERT INTO NotificationQueue (FormSubmissionId, EscalationLevel, RecipientEmail, RecipientName, Subject, Body, NotificationType, Status, SourceModule) VALUES (@LogId, 1, @Email, @Name, @Subject, @Body, 'Email', 'Pending', 'VMS_REMINDER')";
                    using (SqlCommand qCmd = new SqlCommand(qQuery, con))
                    {
                        qCmd.Parameters.AddWithValue("@LogId", logId);
                        qCmd.Parameters.AddWithValue("@Email", hostEmail);
                        qCmd.Parameters.AddWithValue("@Name", hostName);
                        qCmd.Parameters.AddWithValue("@Subject", subject);
                        qCmd.Parameters.AddWithValue("@Body", body);
                        qCmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    throw new Exception("The assigned host does not have a valid email address or employee code on file.");
                }
            }
        }

        // ==========================================
        // TAB 2: ON-BEHALF LOGIC (RESTORED EMAIL & VISITDATE)
        // ==========================================
        protected void gvOnBehalf_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "ForceApprove")
            {
                int index = Convert.ToInt32(e.CommandArgument);
                GridViewRow row = gvOnBehalf.Rows[index];

                int logId = Convert.ToInt32(gvOnBehalf.DataKeys[index].Values["LogID"]);
                string visEmail = gvOnBehalf.DataKeys[index].Values["VisitorEmail"].ToString();
                string visName = gvOnBehalf.DataKeys[index].Values["VisitorName"].ToString();
                string hostEmail = gvOnBehalf.DataKeys[index].Values["HostEmail"].ToString();
                string hostName = gvOnBehalf.DataKeys[index].Values["HostName"].ToString();

                TextBox txtRemarks = (TextBox)row.FindControl("txtRemarks");
                string adminRemarks = txtRemarks.Text.Trim();

                TextBox txtDate = (TextBox)row.FindControl("txtScheduleDate");
                DateTime schedDate = string.IsNullOrEmpty(txtDate.Text) ? DateTime.Now : Convert.ToDateTime(txtDate.Text);

                if (string.IsNullOrEmpty(adminRemarks))
                {
                    lblMessage.ForeColor = Color.Red;
                    lblMessage.Text = "<i class='fa fa-exclamation-triangle'></i> You must enter an Admin Remark to approve on-behalf.";
                    KeepTabActive("tabOnBehalf");
                    return;
                }

                string adminName = Session["UserName"] != null ? Session["UserName"].ToString() : "System Administrator";
                string finalRemark = $"[Approved On-Behalf by {adminName}] - {adminRemarks}";

                ApproveOnBehalf(logId, visEmail, visName, hostEmail, hostName, finalRemark, schedDate);

                BindAllGrids();
                KeepTabActive("tabOnBehalf");
            }
        }

        // ==========================================
        // TAB 2: ON-BEHALF LOGIC (FIXED VALIDITY CALCULATION)
        // ==========================================
        private void ApproveOnBehalf(int logId, string visEmail, string visName, string hostEmail, string hostName, string remarks, DateTime schedDate)
        {
            Guid qrToken = Guid.NewGuid();

            // NEW: Automatically calculate the new expiry to be the end of the scheduled day (11:59:59 PM)
            // This prevents passes from being instantly "Expired" if approved a day late.
            DateTime newValidUpto = schedDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                // NEW: Added ValidUpto = @ValidUpto to the SQL Update Query
                string updateQuery = @"
                UPDATE VMS_VisitLog 
                SET ApprovalStatus = 'Approved', 
                    VisitStatus = 'Scheduled', 
                    ValidFrom = @VisitDate, 
                    ValidUpto = @ValidUpto, 
                    QRToken = @Token, 
                    ApprovedOn = GETDATE(), 
                    ApprovalRemarks = ISNULL(ApprovalRemarks, '') + ' | ' + @Remarks 
                WHERE LogID = @LogID";

                using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                {
                    cmd.Parameters.AddWithValue("@VisitDate", schedDate);
                    cmd.Parameters.AddWithValue("@ValidUpto", newValidUpto); // Pushing the new validity
                    cmd.Parameters.AddWithValue("@Token", qrToken);
                    cmd.Parameters.AddWithValue("@Remarks", remarks);
                    cmd.Parameters.AddWithValue("@LogID", logId);
                    cmd.ExecuteNonQuery();
                }

                string passUrl = $"https://apps.anmolindustries.com/VMS_DigitalPass.aspx?token={qrToken}";

                if (!string.IsNullOrEmpty(visEmail))
                {
                    string visBody = $"<div style='padding:20px;'><h2 style='color:#1ABB9C;'>Visit Approved</h2><p>Hi {visName}, your visit has been scheduled for {schedDate:dd-MMM yyyy HH:mm} and authorized by Administration.</p><a href='{passUrl}'>View Digital Pass</a></div>";
                    QueueEmail(con, logId, visEmail, visName, "Your Anmol Gate Pass (Approved)", visBody, "VMS_PASS");
                }

                if (!string.IsNullOrEmpty(hostEmail))
                {
                    string hostBody = $"<div style='padding:20px;'><h3 style='color:#3498DB;'>Admin Action Notification</h3><p>Hello {hostName},</p><p>Please note that your pending visitor <b>{visName}</b> was successfully <b>Approved On-Behalf</b> by the Administration Team.</p><p>Remarks: {remarks}</p></div>";
                    QueueEmail(con, logId, hostEmail, hostName, $"FYI: Visitor {visName} Approved On-Behalf", hostBody, "VMS_ADMIN_ACTION");
                }
            }

            lblMessage.ForeColor = System.Drawing.Color.Green;
            lblMessage.Text = $"<i class='fa fa-check-circle'></i> Visitor {visName} approved on-behalf (Validity extended to end of scheduled day).";
        }

        private void QueueEmail(SqlConnection con, int logId, string email, string name, string subject, string body, string module)
        {
            string qQuery = "INSERT INTO NotificationQueue (FormSubmissionId, EscalationLevel, RecipientEmail, RecipientName, Subject, Body, NotificationType, Status, SourceModule) VALUES (@LogId, 0, @Email, @Name, @Subject, @Body, 'Email', 'Pending', @Module)";
            using (SqlCommand qCmd = new SqlCommand(qQuery, con))
            {
                qCmd.Parameters.AddWithValue("@LogId", logId);
                qCmd.Parameters.AddWithValue("@Email", email);
                qCmd.Parameters.AddWithValue("@Name", name);
                qCmd.Parameters.AddWithValue("@Subject", subject);
                qCmd.Parameters.AddWithValue("@Body", body);
                qCmd.Parameters.AddWithValue("@Module", module);
                qCmd.ExecuteNonQuery();
            }
        }

        // ==========================================
        // SMART UI SUGGESTION GENERATOR
        // ==========================================
        protected string RenderTimeSuggestion(string fieldType, object currentValue, object createdDate, object approvedOn, object actualInTime)
        {
            // If the field already has data, hide the suggestion pill
            if (currentValue != DBNull.Value && currentValue != null && !string.IsNullOrEmpty(currentValue.ToString()))
                return "";

            DateTime reqDate = createdDate != DBNull.Value ? Convert.ToDateTime(createdDate) : DateTime.Now;
            DateTime appDate = approvedOn != DBNull.Value ? Convert.ToDateTime(approvedOn) : reqDate;
            DateTime? inTime = actualInTime != DBNull.Value ? (DateTime?)Convert.ToDateTime(actualInTime) : null;

            DateTime suggestedTime;

            switch (fieldType)
            {
                case "IN":
                    // Suggest 5 minutes after the admin approved the pass
                    suggestedTime = appDate.AddMinutes(5);
                    break;
                case "OUT":
                    // Suggest 1 hour after they checked in (or 1 hour after approval if In-Time is missing)
                    suggestedTime = inTime.HasValue ? inTime.Value.AddHours(1) : appDate.AddHours(1);
                    break;
                case "VALID":
                    // Suggest the end of the day on which they were approved
                    suggestedTime = appDate.Date.AddHours(23).AddMinutes(59);
                    break;
                default:
                    return "";
            }

            // Ensure the suggestion doesn't accidentally suggest a time in the future
            if (suggestedTime > DateTime.Now && fieldType != "VALID")
            {
                suggestedTime = DateTime.Now;
            }

            string isoTime = suggestedTime.ToString("yyyy-MM-ddTHH:mm");
            string displayTime = suggestedTime.ToString("dd MMM, HH:mm");

            // Renders a clickable Bootstrap label
            return $"<div style='margin-top:6px; text-align:center;'><span class='label label-info' style='cursor:pointer; display:inline-block; padding:5px 8px;' onclick=\"applySmartTime(this, '{isoTime}')\"><i class='fa fa-magic'></i> {displayTime}</span></div>";
        }
    }
}