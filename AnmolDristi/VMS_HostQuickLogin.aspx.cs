using System;
using System.Data.SqlClient;
using System.Configuration;

namespace AnmolDristi
{
    public partial class VMS_HostQuickLogin : System.Web.UI.Page
    {
        string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

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
                           ISNULL(NULLIF(VisitorMobile, ''), VisitorEmail) AS ContactInfo
                    FROM VMS_VisitLog 
                    WHERE LogID = @VisitId AND HostEmployeeCode = @EmpCode";

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

                                pnlAction.Visible = true;
                                lblMessage.Visible = false;
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
            }
        }

        protected void btnApprove_Click(object sender, EventArgs e)
        {
            ProcessApproval("Approved", "Scheduled");
        }

        protected void btnReject_Click(object sender, EventArgs e)
        {
            ProcessApproval("Rejected", "Rejected");
        }

        private void ProcessApproval(string approvalStatus, string visitStatus)
        {
            string visitId = Request.QueryString["vid"];
            Guid qrToken = Guid.NewGuid();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                // 1. Update the Database
                string updateQuery = "UPDATE VMS_VisitLog SET ApprovalStatus = @AppStatus, VisitStatus = @VisStatus, QRToken = @Token, ApprovedOn = GETDATE() WHERE LogID = @LogID";
                using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                {
                    cmd.Parameters.AddWithValue("@AppStatus", approvalStatus);
                    cmd.Parameters.AddWithValue("@VisStatus", visitStatus);
                    cmd.Parameters.AddWithValue("@Token", qrToken);
                    cmd.Parameters.AddWithValue("@LogID", visitId);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                // 2. If Approved, dispatch the Digital Pass to the Visitor
                if (approvalStatus == "Approved")
                {
                    DispatchVisitorEmail(con, visitId, qrToken);
                }
            }

            // Hide action buttons and show success message
            pnlAction.Visible = false;
            lblMessage.Visible = true;
            lblMessage.CssClass = "success";
            lblMessage.Text = approvalStatus == "Approved"
                ? "Visitor Approved successfully! The digital gate pass has been sent to them."
                : "Visitor request has been rejected.";
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
                string passUrl = $"https://apps.anmolindustries.com/VMS_DigitalPass.aspx?token={token}";
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