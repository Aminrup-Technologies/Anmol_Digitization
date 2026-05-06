using System;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class VMS_GateScanner : System.Web.UI.Page
    {
        string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString; // Make sure this matches your web.config!

        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void btnProcessScan_Click(object sender, EventArgs e)
        {
            string scannedToken = hfScannedToken.Value;
            Guid tokenGuid;

            if (!Guid.TryParse(scannedToken, out tokenGuid))
            {
                ShowResult(false, "Invalid QR Code format.", "This does not belong to the Anmol VMS system.", "", null);
                return;
            }

            using (SqlConnection con = new SqlConnection(connStr))
            {
                string query = @"
            SELECT 
                v.LogID, v.VisitorName, v.VisitorCompany, v.VisitorMobile, v.VisitorEmail, 
                v.ApprovalStatus, v.VisitStatus, v.ValidUpto, v.PhotoPath,
                u.EmployeeName AS HostName, u.Email AS HostEmail,
                ISNULL(d.Department_Description, 'General') AS HostDepartment
            FROM VMS_VisitLog v
            LEFT JOIN MST_UserMaster u ON v.HostEmployeeCode = u.EmployeeCode
            LEFT JOIN MST_Department d ON CAST(u.DepartmentId AS VARCHAR) = d.Department_ID
            WHERE v.QRToken = @Token";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Token", tokenGuid);
                    con.Open();

                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        if (sdr.Read())
                        {
                            int logId = Convert.ToInt32(sdr["LogID"]);
                            string status = sdr["ApprovalStatus"].ToString();
                            string visitStatus = sdr["VisitStatus"].ToString();
                            DateTime validUpto = Convert.ToDateTime(sdr["ValidUpto"]);
                            string photoPath = sdr["PhotoPath"].ToString();
                            string hostEmail = sdr["HostEmail"].ToString();

                            // Safely extract all details as explicitly typed strings
                            string visName = sdr["VisitorName"].ToString();
                            string company = sdr["VisitorCompany"].ToString();
                            string contact = !string.IsNullOrEmpty(sdr["VisitorMobile"].ToString()) ? sdr["VisitorMobile"].ToString() : sdr["VisitorEmail"].ToString();
                            string hostName = sdr["HostName"].ToString();
                            string hostDept = sdr["HostDepartment"].ToString();
                            string validUptoStr = validUpto.ToString("dd MMM, hh:mm tt");

                            // 1. Check if Approved
                            if (status != "Approved")
                            {
                                ShowResult(false, "Access Denied", $"Pass is currently marked as: {status}", photoPath, null);
                                return;
                            }

                            // 2. Check Expiration
                            if (DateTime.Now > validUpto)
                            {
                                ShowResult(false, "Pass Expired", $"This pass expired on {validUptoStr}.", photoPath, visName, company, contact, hostName, hostDept, validUptoStr);
                                return;
                            }

                            // 3. Handle Check-In vs Check-Out
                            sdr.Close(); // Close reader to execute updates

                            if (visitStatus == "Scheduled")
                            {
                                // Check-In
                                UpdateGateStatus(con, logId, "Checked-In", "ActualInTime");
                                ShowResult(true, "Check-In Successful", "Please guide the visitor to their host.", photoPath, visName, company, contact, hostName, hostDept, validUptoStr);

                                if (!string.IsNullOrEmpty(hostEmail))
                                {
                                    NotifyHostArrival(con, logId, hostName, hostEmail, visName, company);
                                }
                            }
                            else if (visitStatus == "Checked-In")
                            {
                                // Check-Out
                                UpdateGateStatus(con, logId, "Checked-Out", "ActualOutTime");
                                ShowResult(true, "Check-Out Successful", "Visitor departure logged.", photoPath, visName, company, contact, hostName, hostDept, validUptoStr);

                                if (!string.IsNullOrEmpty(hostEmail))
                                {
                                    NotifyHostDeparture(con, logId, hostName, hostEmail, visName, company);
                                }
                            }
                            else if (visitStatus == "Checked-Out")
                            {
                                ShowResult(false, "Already Checked-Out", $"{visName} has already left the premises.", photoPath, visName, company, contact, hostName, hostDept, validUptoStr);
                            }
                        }
                        else
                        {
                            ShowResult(false, "Pass Not Found", "This pass does not exist in the database.", "", null);
                        }
                    }
                }
            }
        }

        private void NotifyHostArrival(SqlConnection con, int logId, string hostName, string hostEmail, string visName, string company)
        {
            string subject = $"Your visitor {visName} has arrived at the gate";
            string body = $@"
        <div style='font-family: Arial; padding: 20px; border-left: 4px solid #1ABB9C;'>
            <h3 style='color: #1ABB9C;'>Visitor Arrival Notification</h3>
            <p>Hello <b>{hostName}</b>,</p>
            <p>Your approved visitor, <b>{visName}</b> from <b>{company}</b>, has successfully checked in at the security gate and is now entering the premises.</p>
        </div>";

            string qQuery = "INSERT INTO NotificationQueue (FormSubmissionId, EscalationLevel, RecipientEmail, RecipientName, Subject, Body, NotificationType, Status, SourceModule) VALUES (@LogId, 0, @Email, @Name, @Subject, @Body, 'Email', 'Pending', 'VMS_ARRIVAL')";
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

        private void NotifyHostDeparture(SqlConnection con, int logId, string hostName, string hostEmail, string visName, string company)
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

            string qQuery = @"
        INSERT INTO NotificationQueue 
        (FormSubmissionId, EscalationLevel, RecipientEmail, RecipientName, Subject, Body, NotificationType, Status, SourceModule) 
        VALUES 
        (@LogId, 0, @Email, @Name, @Subject, @Body, 'Email', 'Pending', 'VMS_DEPARTURE')";

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

        private void UpdateGateStatus(SqlConnection con, int logId, string newStatus, string timeColumn)
        {
            string updateQuery = $"UPDATE VMS_VisitLog SET VisitStatus = @Status, {timeColumn} = GETDATE(), CheckedOutBy = @GuardId WHERE LogID = @LogID";
            using (SqlCommand cmd = new SqlCommand(updateQuery, con))
            {
                cmd.Parameters.AddWithValue("@Status", newStatus);
                cmd.Parameters.AddWithValue("@LogID", logId);
                cmd.Parameters.AddWithValue("@GuardId", Session["EmployeeCode"] ?? "SecurityGate");
                cmd.ExecuteNonQuery();
            }
        }

        // STRONGLY TYPED ShowResult Method! No dynamics used.
        private void ShowResult(bool isSuccess, string title, string message, string photoPath,
            string visName = null, string comp = null, string contact = null,
            string hostName = null, string hostDept = null, string validStr = null)
        {
            pnlResult.Visible = true;

            pnlResult.CssClass = isSuccess ? "result-card result-success" : "result-card result-danger";
            lblScanTitle.Text = isSuccess ? $"<i class='fa fa-check-circle'></i> {title}" : $"<i class='fa fa-ban'></i> {title}";
            lblScanMessage.Text = message;

            if (!string.IsNullOrEmpty(photoPath))
            {
                imgVisitor.ImageUrl = photoPath;
                imgVisitor.Visible = true;
            }
            else
            {
                imgVisitor.Visible = false;
            }

            // Populate the detailed summary list if data is provided
            if (visName != null)
            {
                pnlVisitDetails.Visible = true;
                lblDetName.Text = visName;
                lblDetCompany.Text = comp;
                lblDetContact.Text = contact;
                lblDetHost.Text = hostName;
                lblDetDept.Text = hostDept;
                lblDetValid.Text = validStr;
            }
            else
            {
                pnlVisitDetails.Visible = false;
            }
        }

        protected void btnResetScanner_Click(object sender, EventArgs e)
        {
            pnlResult.Visible = false;
            hfScannedToken.Value = "";
        }
    }
}