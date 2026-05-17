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

                BindAllGrids();
            }
        }

        private void KeepTabActive(string tabId)
        {
            ScriptManager.RegisterStartupScript(this, this.GetType(), "FocusTab", $"$('#adminTabs a[href=\"#{tabId}\"]').tab('show');", true);
        }

        private void BindAllGrids()
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                // --- Fetch Pending Visitiors (For Tabs 1 & 2) ---
                string pendingQuery = @"
                    SELECT 
                        v.LogID, v.VisitorName, v.VisitorEmail, v.VisitorCompany, 
                        v.PurposeOfVisit, v.CreatedDate, u.EmployeeName AS HostName, 
                        u.Email AS HostEmail, ISNULL(b.Branch_Description, 'Unknown Location') AS LocationName
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

                // --- Fetch Approved & Active Visitors (For Tab 3: Inline Gate Override) ---
                string activeQuery = @"
    SELECT 
        v.LogID, 
        v.VisitorName, 
        v.VisitStatus,
        v.ActualInTime, 
        v.ActualOutTime, 
        v.ValidUpto,
        v.CreatedDate,    -- NEW: Added Request Time
        v.ApprovedOn      -- NEW: Added Approval Time
    FROM VMS_VisitLog v
    WHERE v.ApprovalStatus = 'Approved' 
      AND v.VisitStatus IN ('Scheduled', 'Checked-In')
    ORDER BY v.ApprovedOn DESC";

                using (SqlDataAdapter sdaActive = new SqlDataAdapter(activeQuery, con))
                {
                    DataTable dtActive = new DataTable();
                    sdaActive.Fill(dtActive);
                    gvAdminGateOverride.DataSource = dtActive;
                    gvAdminGateOverride.DataBind();
                }
            }
        }

        // ==========================================
        // TAB 3: INLINE AUTO-SAVE LOGIC
        // ==========================================
        // ==========================================
        // TAB 3: INLINE AUTO-SAVE LOGIC (WITH VALIDATIONS)
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
                lblMessage.ForeColor = Color.Red;
                lblMessage.Text = "<i class='fa fa-exclamation-triangle'></i> Update Cancelled: You must type an Audit Reason BEFORE changing the date/time.";
                BindAllGrids();
                KeepTabActive("tabGateOverride");
                return;
            }

            // 2. Parse the New Date
            DateTime newTime;
            if (!DateTime.TryParse(txtChanged.Text, out newTime))
            {
                lblMessage.ForeColor = Color.Red;
                lblMessage.Text = "Invalid time format selected.";
                BindAllGrids();
                KeepTabActive("tabGateOverride");
                return;
            }

            // 3. Fetch current live data for Validation Checkpoints
            DateTime? approvedOn = null;
            DateTime? currentInTime = null;
            DateTime? currentOutTime = null;

            using (SqlConnection con = new SqlConnection(connStr))
            {
                string checkQuery = "SELECT ApprovedOn, ActualInTime, ActualOutTime FROM VMS_VisitLog WHERE LogID = @LogID";
                using (SqlCommand cmd = new SqlCommand(checkQuery, con))
                {
                    cmd.Parameters.AddWithValue("@LogID", logId);
                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        if (sdr.Read())
                        {
                            if (sdr["ApprovedOn"] != DBNull.Value) approvedOn = Convert.ToDateTime(sdr["ApprovedOn"]);
                            if (sdr["ActualInTime"] != DBNull.Value) currentInTime = Convert.ToDateTime(sdr["ActualInTime"]);
                            if (sdr["ActualOutTime"] != DBNull.Value) currentOutTime = Convert.ToDateTime(sdr["ActualOutTime"]);
                        }
                    }
                }
            }

            // 4. APPLY CHRONOLOGICAL CHECKPOINTS
            string dbColumnName = "";
            string statusUpdateLogic = "";
            string friendlyName = "";

            if (txtChanged.ID == "txtInTime")
            {
                // Validation: In-Time cannot be before Approval Time
                if (approvedOn.HasValue && newTime < approvedOn.Value)
                {
                    ShowValidationError($"Check-In time ({newTime:HH:mm}) cannot be BEFORE the Approval time ({approvedOn.Value:HH:mm})."); return;
                }
                // Validation: In-Time cannot be in the future
                if (newTime > DateTime.Now)
                {
                    ShowValidationError("Check-In time cannot be set in the future."); return;
                }
                // Validation: In-Time cannot be after existing Out-Time
                if (currentOutTime.HasValue && newTime > currentOutTime.Value)
                {
                    ShowValidationError($"Check-In time cannot be AFTER the existing Check-Out time ({currentOutTime.Value:HH:mm})."); return;
                }

                dbColumnName = "ActualInTime";
                friendlyName = "Check-In Time";
                statusUpdateLogic = ", VisitStatus = 'Checked-In'";
            }
            else if (txtChanged.ID == "txtOutTime")
            {
                // Validation: Cannot checkout if they never checked in
                if (!currentInTime.HasValue)
                {
                    ShowValidationError("You cannot set a Check-Out time because the visitor has no Check-In time yet."); return;
                }
                // Validation: Out-Time cannot be before In-Time
                if (currentInTime.HasValue && newTime < currentInTime.Value)
                {
                    ShowValidationError($"Check-Out time ({newTime:HH:mm}) cannot be BEFORE the Check-In time ({currentInTime.Value:HH:mm})."); return;
                }
                // Validation: Out-Time cannot be in the future
                if (newTime > DateTime.Now)
                {
                    ShowValidationError("Check-Out time cannot be set in the future."); return;
                }

                dbColumnName = "ActualOutTime";
                friendlyName = "Check-Out Time";
                statusUpdateLogic = ", VisitStatus = 'Checked-Out', CheckedOutBy = 'ADMIN_INLINE'";
            }
            else if (txtChanged.ID == "txtValidUpto")
            {
                // Validation: Expiry cannot be set before they even checked in
                if (currentInTime.HasValue && newTime < currentInTime.Value)
                {
                    ShowValidationError($"Pass Expiry cannot be set BEFORE the visitor's Check-In time ({currentInTime.Value:HH:mm})."); return;
                }

                dbColumnName = "ValidUpto";
                friendlyName = "Pass Validity";
                statusUpdateLogic = "";
            }

            // 5. Build Audit Trail & Execute Update (If all validations pass)
            string adminName = Session["UserName"] != null ? Session["UserName"].ToString() : "Admin";
            string auditTrail = $" | [{friendlyName} modified to {newTime:dd-MMM HH:mm} by {adminName}] Reason: {reason}";

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

        // Small helper method to keep code clean
        private void ShowValidationError(string message)
        {
            lblMessage.ForeColor = System.Drawing.Color.Red;
            lblMessage.Text = $"<i class='fa fa-exclamation-triangle'></i> Update Cancelled: {message}";
            BindAllGrids();
            KeepTabActive("tabGateOverride");
        }

        // ==========================================
        // TAB 1 & 2 METHODS (Unchanged)
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

                if (string.IsNullOrEmpty(adminRemarks))
                {
                    lblMessage.ForeColor = Color.Red;
                    lblMessage.Text = "<i class='fa fa-exclamation-triangle'></i> You must enter an Admin Remark to approve on-behalf.";
                    KeepTabActive("tabOnBehalf");
                    return;
                }

                string adminName = Session["UserName"] != null ? Session["UserName"].ToString() : "System Administrator";
                string finalRemark = $"[Approved On-Behalf by {adminName}] - {adminRemarks}";

                ApproveOnBehalf(logId, visEmail, visName, hostEmail, hostName, finalRemark);

                BindAllGrids();
                KeepTabActive("tabOnBehalf");
            }
        }

        private void ApproveOnBehalf(int logId, string visEmail, string visName, string hostEmail, string hostName, string remarks)
        {
            Guid qrToken = Guid.NewGuid();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();
                string updateQuery = "UPDATE VMS_VisitLog SET ApprovalStatus = 'Approved', VisitStatus = 'Scheduled', QRToken = @Token, ApprovedOn = GETDATE(), ApprovalRemarks = @Remarks WHERE LogID = @LogID";
                using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                {
                    cmd.Parameters.AddWithValue("@Token", qrToken);
                    cmd.Parameters.AddWithValue("@Remarks", remarks);
                    cmd.Parameters.AddWithValue("@LogID", logId);
                    cmd.ExecuteNonQuery();
                }

                string passUrl = $"https://apps.anmolindustries.com/VMS_DigitalPass.aspx?token={qrToken}";

                if (!string.IsNullOrEmpty(visEmail))
                {
                    string visBody = $"<div style='padding:20px;'><h2 style='color:#1ABB9C;'>Visit Approved</h2><p>Hi {visName}, your visit has been authorized by Administration.</p><a href='{passUrl}'>View Digital Pass</a></div>";
                    QueueEmail(con, logId, visEmail, visName, "Your Anmol Gate Pass (Approved)", visBody, "VMS_PASS");
                }

                if (!string.IsNullOrEmpty(hostEmail))
                {
                    string hostBody = $"<div style='padding:20px;'><h3 style='color:#3498DB;'>Admin Action Notification</h3><p>Hello {hostName},</p><p>Please note that your pending visitor <b>{visName}</b> was successfully <b>Approved On-Behalf</b> by the Administration Team.</p><p>Remarks: {remarks}</p></div>";
                    QueueEmail(con, logId, hostEmail, hostName, $"FYI: Visitor {visName} Approved On-Behalf", hostBody, "VMS_ADMIN_ACTION");
                }
                con.Close();
            }

            lblMessage.ForeColor = Color.Green;
            lblMessage.Text = $"<i class='fa fa-check-circle'></i> Visitor {visName} approved on-behalf.";
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

        private void SendReminderToHost(int logId)
        {
            string hostEmail = "", hostName = "", visitorName = "", company = "", purpose = "", hostEmpCode = "";

            using (SqlConnection con = new SqlConnection(connStr))
            {
                // 1. Fetch exact visitor and host details, ADDED HostEmployeeCode to the SELECT list
                string query = @"
            SELECT v.VisitorName, v.VisitorCompany, v.PurposeOfVisit, v.HostEmployeeCode,
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
                            purpose = sdr["PurposeOfVisit"].ToString();
                            hostName = sdr["HostName"].ToString();
                            hostEmail = sdr["HostEmail"].ToString();

                            // Fetch the Employee Code so we can encode it
                            hostEmpCode = sdr["HostEmployeeCode"].ToString();
                        }
                    }
                    con.Close();
                }

                if (!string.IsNullOrEmpty(hostEmail) && !string.IsNullOrEmpty(hostEmpCode))
                {
                    string subject = $"[URGENT] Approval Needed: Visitor {visitorName} is waiting";

                    // 2. Convert the Host Employee Code to a Base64 string to obfuscate it in the URL
                    byte[] plainTextBytes = System.Text.Encoding.UTF8.GetBytes(hostEmpCode);
                    string encodedEmpCode = Convert.ToBase64String(plainTextBytes);

                    // 3. Append BOTH the Visit ID (using logId) and the Encoded Token to the URL
                    string quickApproveLink = $"https://apps.anmolindustries.com/VMS_HostQuickLogin.aspx?vid={logId}&t={encodedEmpCode}";

                    // Styled the button to make it look prominent in the email
                    string body = $@"
                <div style='padding:20px; border: 2px solid #f0ad4e; font-family: Arial, sans-serif;'>
                    <h3 style='color:#f0ad4e;'>Reminder: Visitor Waiting</h3>
                    <p>Hello <b>{hostName}</b>,</p>
                    <p>Priority reminder: <b>{visitorName}</b> from <b>{company}</b> is waiting for approval.</p>
                    <br/>
                    <a href='{quickApproveLink}' style='background: #1ABB9C; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block;'>Click Here to Verify & Approve</a>
                </div>";

                    using (SqlConnection queueCon = new SqlConnection(connStr))
                    {
                        queueCon.Open();
                        string qQuery = "INSERT INTO NotificationQueue (FormSubmissionId, EscalationLevel, RecipientEmail, RecipientName, Subject, Body, NotificationType, Status, SourceModule) VALUES (@LogId, 1, @Email, @Name, @Subject, @Body, 'Email', 'Pending', 'VMS_REMINDER')";
                        using (SqlCommand qCmd = new SqlCommand(qQuery, queueCon))
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
                else
                {
                    throw new Exception("The assigned host does not have a valid email address or employee code on file.");
                }
            }
        }
    }
}