/*
=========================================================
Name: VMS_MyVisitors_Session_Fix
When: 2026-05-14
Why:  The Host Quick Login feature was creating Session["EmpCode"], 
      but this page was strictly enforcing Session["WORKMAN"], 
      causing an unwanted redirect to the standard login.
What: Updated Page_Load and BindGrids to accept either 
      Session["WORKMAN"] (Standard ERP) OR Session["EmpCode"] (Quick Login).
=========================================================
*/

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
                // FIX: Check for BOTH the standard ERP session and the Quick Verification session
                if (Session["WORKMAN"] == null && Session["EmpCode"] == null)
                {
                    Response.Redirect("Login.aspx");
                    return;
                }

                BindGrids();
            }
        }

        private void BindGrids()
        {
            // FIX: Safely determine which session key holds the Employee Code
            string empCode = "";
            if (Session["WORKMAN"] != null)
            {
                empCode = Session["WORKMAN"].ToString();
            }
            else if (Session["EmpCode"] != null)
            {
                empCode = Session["EmpCode"].ToString();
            }

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
            Guid qrToken = Guid.NewGuid();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                string updateQuery = "UPDATE VMS_VisitLog SET ApprovalStatus = 'Approved', VisitStatus = 'Scheduled', QRToken = @Token, ApprovedOn = GETDATE() WHERE LogID = @LogID";
                using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                {
                    cmd.Parameters.AddWithValue("@Token", qrToken);
                    cmd.Parameters.AddWithValue("@LogID", logId);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                VmsHelper.QueueVisitorPassNotification(con, logId, qrToken, name, email, mobile);
            }

            lblPendingMsg.ForeColor = Color.Green;
            lblPendingMsg.Text = "Visitor Approved! Digital pass sent.";
            BindGrids();
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