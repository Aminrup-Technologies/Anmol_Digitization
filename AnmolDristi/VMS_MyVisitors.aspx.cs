using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.UI.WebControls;
using System.Drawing;

namespace AnmolDristi
{
    public partial class VMS_MyVisitors : System.Web.UI.Page
    {
        string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // In production, grab this from your global ERP Login Session
                if (Session["WORKMAN"] == null)
                {
                    // Redirect to login or set a fallback for testing
                    //Session["WORKMAN"] = "AHO004"; // Testing as Arjun Pal
                    Response.Redirect("Login.aspx");
                    return;
                }

                BindGrids();
            }
        }

        private void BindGrids()
        {
            string empCode = Session["WORKMAN"].ToString();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                // 1. Bind Pending Grid
                string pendingQuery = "SELECT * FROM VMS_VisitLog WHERE HostEmployeeCode = @EmpCode AND ApprovalStatus = 'Pending' ORDER BY CreatedDate DESC";
                using (SqlDataAdapter sda = new SqlDataAdapter(pendingQuery, con))
                {
                    sda.SelectCommand.Parameters.AddWithValue("@EmpCode", empCode);
                    DataTable dtPending = new DataTable();
                    sda.Fill(dtPending);
                    gvPending.DataSource = dtPending;
                    gvPending.DataBind();

                    badgeCount.InnerText = dtPending.Rows.Count.ToString();
                }

                // 2. Bind History Grid (Now includes logic to power the Manual Check-In/Out)
                string historyQuery = "SELECT TOP 50 * FROM VMS_VisitLog WHERE HostEmployeeCode = @EmpCode AND ApprovalStatus IN ('Approved', 'Rejected') ORDER BY CreatedDate DESC";
                using (SqlDataAdapter sda = new SqlDataAdapter(historyQuery, con))
                {
                    sda.SelectCommand.Parameters.AddWithValue("@EmpCode", empCode);
                    DataTable dtHistory = new DataTable();
                    sda.Fill(dtHistory);
                    gvHistory.DataSource = dtHistory;
                    gvHistory.DataBind();
                }
            }
        }

        protected void gvPending_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int index = Convert.ToInt32(e.CommandArgument);
            int logId = Convert.ToInt32(gvPending.DataKeys[index].Values["LogID"]);
            string visEmail = gvPending.DataKeys[index].Values["VisitorEmail"].ToString();
            string visMobile = gvPending.DataKeys[index].Values["VisitorMobile"].ToString();
            string visName = gvPending.DataKeys[index].Values["VisitorName"].ToString();

            if (e.CommandName == "ApproveVisit")
            {
                ApproveVisitor(logId, visEmail, visMobile, visName);
            }
            else if (e.CommandName == "RejectVisit")
            {
                RejectVisitor(logId);
            }
        }

        private void ApproveVisitor(int logId, string email, string mobile, string name)
        {
            // 1. Generate the Secure QR Token
            Guid qrToken = Guid.NewGuid();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                // Set VisitStatus to Scheduled along with ApprovalStatus
                string updateQuery = "UPDATE VMS_VisitLog SET ApprovalStatus = 'Approved', VisitStatus = 'Scheduled', QRToken = @Token, ApprovedOn = GETDATE() WHERE LogID = @LogID";
                using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                {
                    cmd.Parameters.AddWithValue("@Token", qrToken);
                    cmd.Parameters.AddWithValue("@LogID", logId);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }

                // 2. Dispatch the Digital Pass to the Visitor
                string passUrl = $"https://apps.anmolindustries.com/VMS_DigitalPass.aspx?token={qrToken}";

                if (!string.IsNullOrEmpty(email))
                {
                    // Send Email Pass via Queue
                    string body = $@"
                        <div style='font-family: Arial; padding: 20px; border: 1px solid #1ABB9C; border-radius: 10px; text-align: center; max-width: 400px;'>
                            <h2 style='color: #1ABB9C;'>Visit Approved</h2>
                            <p>Hi {name}, your host has approved your visit.</p>
                            <p>Click the link below to open your Digital Gate Pass and show it to the security guard.</p>
                            <br/>
                            <a href='{passUrl}' style='background-color: #1ABB9C; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>View Digital Pass</a>
                        </div>";

                    string qQuery = "INSERT INTO NotificationQueue (FormSubmissionId, EscalationLevel, RecipientEmail, RecipientName, Subject, Body, NotificationType, Status, SourceModule) VALUES (@LogId, 0, @Email, @Name, 'Your Anmol Gate Pass', @Body, 'Email', 'Pending', 'VMS_PASS')";
                    using (SqlCommand qCmd = new SqlCommand(qQuery, con))
                    {
                        qCmd.Parameters.AddWithValue("@LogId", logId);
                        qCmd.Parameters.AddWithValue("@Email", email);
                        qCmd.Parameters.AddWithValue("@Name", name);
                        qCmd.Parameters.AddWithValue("@Body", body);
                        con.Open();
                        qCmd.ExecuteNonQuery();
                        con.Close();
                    }
                }
                else if (!string.IsNullOrEmpty(mobile))
                {
                    // TODO: Trigger SMS Gateway
                }
            }

            lblPendingMsg.ForeColor = Color.Green;
            lblPendingMsg.Text = "Visitor Approved! Digital pass sent.";
            BindGrids(); // Refresh UI
        }

        private void RejectVisitor(int logId)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                string updateQuery = "UPDATE VMS_VisitLog SET ApprovalStatus = 'Rejected', VisitStatus = 'Rejected', ApprovedOn = GETDATE() WHERE LogID = @LogID";
                using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                {
                    cmd.Parameters.AddWithValue("@LogID", logId);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }
            }
            lblPendingMsg.ForeColor = Color.Red;
            lblPendingMsg.Text = "Visitor request rejected.";
            BindGrids(); // Refresh UI
        }

        // ========================================================
        // NEW METHOD: Handle Manual Check-In and Check-Out by Host
        // ========================================================
        protected void gvHistory_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "ManualCheckIn" || e.CommandName == "ManualCheckOut")
            {
                int logId = Convert.ToInt32(e.CommandArgument);

                using (SqlConnection con = new SqlConnection(connStr))
                {
                    string updateQuery = "";

                    if (e.CommandName == "ManualCheckIn")
                    {
                        // FIXED: Changed CheckInTime to ActualInTime
                        updateQuery = "UPDATE VMS_VisitLog SET VisitStatus = 'Checked-In', ActualInTime = GETDATE() WHERE LogID = @LogID";
                        lblHistoryMsg.ForeColor = Color.Green;
                        lblHistoryMsg.Text = "Visitor successfully Checked-IN manually.";
                    }
                    else if (e.CommandName == "ManualCheckOut")
                    {
                        // FIXED: Changed CheckOutTime to ActualOutTime
                        updateQuery = "UPDATE VMS_VisitLog SET VisitStatus = 'Checked-Out', ActualOutTime = GETDATE() WHERE LogID = @LogID";
                        lblHistoryMsg.ForeColor = Color.Green;
                        lblHistoryMsg.Text = "Visitor successfully Checked-OUT manually.";
                    }

                    using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@LogID", logId);
                        con.Open();
                        cmd.ExecuteNonQuery();
                        con.Close();
                    }
                }

                // Refresh the Grid to show updated statuses and swap the buttons immediately
                BindGrids();
            }
        }
    }
}