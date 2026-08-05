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
using System.Globalization;
using System.Web.UI;
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
                string scheduleType = GetScheduleType(logId);
                if (scheduleType == "MultiDay")
                {
                    hfModalLogId.Value = logId.ToString();
                    ViewState["ModalVisEmail"] = visEmail;
                    ViewState["ModalVisMobile"] = visMobile;
                    ViewState["ModalVisName"] = visName;
                    BindModalDays(logId);
                    PrefillApprovedValidityTimes(logId);
                    lblModalMsg.Text = "";
                    ScriptManager.RegisterStartupScript(this, GetType(), "ShowScheduleModal",
                        "$('#ScheduleApprovalModal').modal('show');", true);
                }
                else
                {
                    ApproveVisitor(logId, visEmail, visMobile, visName);
                }
            }
            else if (e.CommandName == "RejectVisit")
            {
                RejectVisitor(logId);
            }
        }

        private string GetScheduleType(int logId)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand(
                "SELECT ISNULL(ScheduleType, 'SingleDay') AS ScheduleType FROM VMS_VisitLog WHERE LogID = @LogID", con))
            {
                cmd.Parameters.AddWithValue("@LogID", logId);
                con.Open();
                object result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? "SingleDay" : result.ToString();
            }
        }

        private void PrefillApprovedValidityTimes(int logId)
        {
            TimeSpan plantOpen = new TimeSpan(9, 0, 0);
            TimeSpan plantClose = new TimeSpan(18, 0, 0);

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                int branchId = 0;
                using (SqlCommand branchCmd = new SqlCommand(
                    "SELECT ISNULL(TargetBranchID, 0) FROM VMS_VisitLog WHERE LogID = @LogID", con))
                {
                    branchCmd.Parameters.AddWithValue("@LogID", logId);
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

        private void BindModalDays(int logId)
        {
            cblModalDays.Items.Clear();

            using (SqlConnection con = new SqlConnection(connStr))
            using (SqlCommand dayCmd = new SqlCommand(@"
                SELECT VisitDate
                FROM VMS_VisitDayLog
                WHERE LogID = @LogID
                ORDER BY VisitDate", con))
            {
                dayCmd.Parameters.AddWithValue("@LogID", logId);
                con.Open();
                using (SqlDataReader dayReader = dayCmd.ExecuteReader())
                {
                    while (dayReader.Read())
                    {
                        DateTime visitDate = Convert.ToDateTime(dayReader["VisitDate"]);
                        var item = new ListItem(visitDate.ToString("dd MMM yyyy"), visitDate.ToString("yyyy-MM-dd"));
                        item.Selected = true;
                        cblModalDays.Items.Add(item);
                    }
                }
            }
        }

        protected void btnConfirmSchedule_Click(object sender, EventArgs e)
        {
            bool anySelected = false;
            foreach (ListItem item in cblModalDays.Items)
            {
                if (item.Selected)
                {
                    anySelected = true;
                    break;
                }
            }

            if (!anySelected)
            {
                lblModalMsg.Text = "Please select at least one day to approve.";
                ScriptManager.RegisterStartupScript(this, GetType(), "ShowScheduleModal",
                    "$('#ScheduleApprovalModal').modal('show');", true);
                return;
            }

            TimeSpan approvedFromTime;
            TimeSpan approvedToTime;
            if (!TimeSpan.TryParse(txtApprovedTimeFrom.Text, out approvedFromTime) ||
                !TimeSpan.TryParse(txtApprovedTimeTo.Text, out approvedToTime))
            {
                lblModalMsg.Text = "Please enter valid From and Until times.";
                ScriptManager.RegisterStartupScript(this, GetType(), "ShowScheduleModal",
                    "$('#ScheduleApprovalModal').modal('show');", true);
                return;
            }

            if (approvedToTime <= approvedFromTime)
            {
                lblModalMsg.Text = "Valid Until must be after Valid From.";
                ScriptManager.RegisterStartupScript(this, GetType(), "ShowScheduleModal",
                    "$('#ScheduleApprovalModal').modal('show');", true);
                return;
            }

            int logId = Convert.ToInt32(hfModalLogId.Value);
            string visEmail = ViewState["ModalVisEmail"] != null ? ViewState["ModalVisEmail"].ToString() : "";
            string visMobile = ViewState["ModalVisMobile"] != null ? ViewState["ModalVisMobile"].ToString() : "";
            string visName = ViewState["ModalVisName"] != null ? ViewState["ModalVisName"].ToString() : "";

            ApproveVisitor(logId, visEmail, visMobile, visName, applySelectedModalDays: true,
                approvedFromTime: approvedFromTime, approvedToTime: approvedToTime);

            ScriptManager.RegisterStartupScript(this, GetType(), "HideScheduleModal",
                "$('#ScheduleApprovalModal').modal('hide'); $('body').removeClass('modal-open'); $('.modal-backdrop').remove();", true);
        }

        private void ApplyModalDayApprovals(SqlConnection con, SqlTransaction trans, int logId)
        {
            foreach (ListItem item in cblModalDays.Items)
            {
                DateTime visitDate = DateTime.ParseExact(item.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                using (SqlCommand dayCmd = new SqlCommand(@"
                    UPDATE VMS_VisitDayLog
                    SET IsDayApproved = @IsDayApproved
                    WHERE LogID = @LogID AND VisitDate = @VisitDate", con, trans))
                {
                    dayCmd.Parameters.AddWithValue("@IsDayApproved", item.Selected ? 1 : 0);
                    dayCmd.Parameters.AddWithValue("@LogID", logId);
                    dayCmd.Parameters.AddWithValue("@VisitDate", visitDate.Date);
                    dayCmd.ExecuteNonQuery();
                }
            }
        }

        private void ApplyApprovedValidityTimes(
            SqlConnection con,
            SqlTransaction trans,
            int logId,
            TimeSpan approvedFromTime,
            TimeSpan approvedToTime)
        {
            // Keep the existing calendar dates; replace only the time portion of ValidFrom / ValidUpto
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

            // Keep per-day gate windows aligned with the host override
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

        private void ApproveVisitor(
            int logId,
            string email,
            string mobile,
            string name,
            bool applySelectedModalDays = false,
            TimeSpan? approvedFromTime = null,
            TimeSpan? approvedToTime = null)
        {
            Guid qrToken = Guid.NewGuid();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();
                using (SqlTransaction trans = con.BeginTransaction())
                {
                    try
                    {
                        if (applySelectedModalDays)
                        {
                            ApplyModalDayApprovals(con, trans, logId);
                        }

                        string updateQuery = "UPDATE VMS_VisitLog SET ApprovalStatus = 'Approved', VisitStatus = 'Scheduled', QRToken = @Token, ApprovedOn = GETDATE() WHERE LogID = @LogID";
                        using (SqlCommand cmd = new SqlCommand(updateQuery, con, trans))
                        {
                            cmd.Parameters.AddWithValue("@Token", qrToken);
                            cmd.Parameters.AddWithValue("@LogID", logId);
                            cmd.ExecuteNonQuery();
                        }

                        VmsVisitScheduleHelper.ApproveVisitSchedule(con, trans, logId, null, null);

                        // Re-apply selective day flags so range-based schedule approval does not overwrite them
                        if (applySelectedModalDays)
                        {
                            ApplyModalDayApprovals(con, trans, logId);
                        }

                        if (approvedFromTime.HasValue && approvedToTime.HasValue)
                        {
                            ApplyApprovedValidityTimes(con, trans, logId, approvedFromTime.Value, approvedToTime.Value);
                        }

                        VmsHelper.QueueVisitorPassNotification(con, logId, qrToken, name, email, mobile, trans);
                        trans.Commit();
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
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
