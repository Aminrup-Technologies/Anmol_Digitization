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
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Drawing;
using AnmolDristi.DAL;

namespace AnmolDristi
{
    public partial class VMS_MyVisitors : System.Web.UI.Page
    {
        string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        private readonly VmsVisitorManagementRepository _repository = new VmsVisitorManagementRepository();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (!ValidateHostSession())
                    return;

                BindGrids();
            }
        }

        private bool ValidateHostSession()
        {
            // Accept either standard ERP session (WORKMAN) or VMS Host Quick Login (EmpCode).
            if (Session["WORKMAN"] == null && Session["EmpCode"] == null)
            {
                Response.Redirect("Login.aspx");
                return false;
            }

            return true;
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

            DataTable dtPending = _repository.GetPendingVisits(empCode);
            gvPending.DataSource = dtPending;
            gvPending.DataBind();
            badgeCount.InnerText = dtPending.Rows.Count.ToString();

            DataTable dtHistory = _repository.GetHostVisitHistory(empCode);
            gvHistory.DataSource = dtHistory;
            gvHistory.DataBind();
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
                    ScriptManager.RegisterStartupScript(this, GetType(), "ShowScheduleModal",
                        "$('#ScheduleApprovalModal').modal('show');", true);
                }
                else
                {
                    ApproveVisitor(logId);
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
                DateTime? requestedValidFrom = null;
                using (SqlCommand branchCmd = new SqlCommand(
                    "SELECT ISNULL(TargetBranchID, 0) AS TargetBranchID, RequestedValidFrom FROM VMS_VisitLog WHERE LogID = @LogID", con))
                {
                    branchCmd.Parameters.AddWithValue("@LogID", logId);
                    using (SqlDataReader visitReader = branchCmd.ExecuteReader())
                    {
                        if (visitReader.Read())
                        {
                            branchId = Convert.ToInt32(visitReader["TargetBranchID"]);
                            if (visitReader["RequestedValidFrom"] != DBNull.Value)
                                requestedValidFrom = Convert.ToDateTime(visitReader["RequestedValidFrom"]);
                        }
                    }
                }

                if (branchId > 0 && requestedValidFrom.HasValue)
                {
                    using (SqlCommand timeCmd = new SqlCommand(@"
                        SELECT OpenTime, CloseTime
                        FROM VMS_PlantTimings
                        WHERE Branch_ID = @BranchID AND DayOfWeek = @DayOfWeek", con))
                    {
                        timeCmd.Parameters.AddWithValue("@BranchID", branchId);
                        timeCmd.Parameters.AddWithValue("@DayOfWeek", requestedValidFrom.Value.ToString("dddd"));
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

            txtApprovedTimeFrom.Text = plantOpen.ToString(@"hh\:mm");
            txtApprovedTimeTo.Text = plantClose.ToString(@"hh\:mm");

            lblModalMsg.ForeColor = Color.FromArgb(0x73, 0x87, 0x9C);
            lblModalMsg.Text = "Branch hours: " + plantOpen.ToString(@"hh\:mm") + " - " + plantClose.ToString(@"hh\:mm");
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
                lblModalMsg.ForeColor = Color.Red;
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
                lblModalMsg.ForeColor = Color.Red;
                lblModalMsg.Text = "Please enter valid From and Until times.";
                ScriptManager.RegisterStartupScript(this, GetType(), "ShowScheduleModal",
                    "$('#ScheduleApprovalModal').modal('show');", true);
                return;
            }

            if (approvedToTime <= approvedFromTime)
            {
                lblModalMsg.ForeColor = Color.Red;
                lblModalMsg.Text = "Valid Until must be after Valid From.";
                ScriptManager.RegisterStartupScript(this, GetType(), "ShowScheduleModal",
                    "$('#ScheduleApprovalModal').modal('show');", true);
                return;
            }

            int logId = Convert.ToInt32(hfModalLogId.Value);

            var selectedDays = new List<SelectedVisitDay>();
            foreach (ListItem item in cblModalDays.Items)
            {
                DateTime visitDate = DateTime.ParseExact(item.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                selectedDays.Add(new SelectedVisitDay
                {
                    VisitDate = visitDate.Date,
                    IsApproved = item.Selected
                });
            }

            VisitorApprovalResult result = ApproveVisitor(
                logId,
                selectedDays,
                approvedFromTime,
                approvedToTime);

            if (result.Succeeded)
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "HideScheduleModal",
                    "$('#ScheduleApprovalModal').modal('hide'); $('body').removeClass('modal-open'); $('.modal-backdrop').remove();", true);
            }
            else
            {
                lblModalMsg.ForeColor = Color.Red;
                lblModalMsg.Text = FormatApprovalFailureMessage(result.FailureReason);
                ScriptManager.RegisterStartupScript(this, GetType(), "ShowScheduleModal",
                    "$('#ScheduleApprovalModal').modal('show');", true);
            }
        }

        private VisitorApprovalResult ApproveVisitor(
            int logId,
            IList<SelectedVisitDay> selectedDays = null,
            TimeSpan? approvedFromTime = null,
            TimeSpan? approvedToTime = null)
        {
            string hostEmployeeCode = "";
            if (Session["WORKMAN"] != null)
            {
                hostEmployeeCode = Session["WORKMAN"].ToString();
            }
            else if (Session["EmpCode"] != null)
            {
                hostEmployeeCode = Session["EmpCode"].ToString();
            }

            VisitorApprovalResult result = _repository.ApprovePendingVisit(new VisitorApprovalRequest
            {
                LogId = logId,
                HostEmployeeCode = hostEmployeeCode,
                SelectedDays = selectedDays,
                ApprovedFromTime = approvedFromTime,
                ApprovedToTime = approvedToTime
            });

            if (result.Succeeded)
            {
                lblPendingMsg.ForeColor = Color.Green;
                lblPendingMsg.Text = "Visitor Approved! Digital pass sent.";
                BindGrids();
            }
            else
            {
                lblPendingMsg.ForeColor = Color.Red;
                lblPendingMsg.Text = FormatApprovalFailureMessage(result.FailureReason);
                BindGrids();
            }

            return result;
        }

        private static string FormatApprovalFailureMessage(ApprovalFailureReason reason)
        {
            switch (reason)
            {
                case ApprovalFailureReason.NotFoundOrNotOwned:
                    return "Unable to approve this visit. It was not found or you are not authorized.";
                case ApprovalFailureReason.NotPending:
                    return "Unable to approve this visit. It is no longer pending.";
                default:
                    return "Unable to approve this visit. Please try again.";
            }
        }

        private void RejectVisitor(int logId)
        {
            string hostEmployeeCode = "";
            if (Session["WORKMAN"] != null)
            {
                hostEmployeeCode = Session["WORKMAN"].ToString();
            }
            else if (Session["EmpCode"] != null)
            {
                hostEmployeeCode = Session["EmpCode"].ToString();
            }

            if (_repository.RejectVisitor(logId, hostEmployeeCode))
            {
                lblPendingMsg.ForeColor = Color.Red;
                lblPendingMsg.Text = "Visitor request rejected.";
            }
            else
            {
                lblPendingMsg.ForeColor = Color.Red;
                lblPendingMsg.Text = "Unable to reject this visit. It may already have been processed.";
            }

            BindGrids(); // Refresh UI
        }

        // ========================================================
        // Manual Check-In / Check-Out by Host (AR-VMS-03A DayLog-aware)
        // ========================================================
        protected void gvHistory_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "ManualCheckIn" || e.CommandName == "ManualCheckOut")
            {
                int logId = Convert.ToInt32(e.CommandArgument);

                string hostEmployeeCode = "";
                if (Session["WORKMAN"] != null)
                {
                    hostEmployeeCode = Session["WORKMAN"].ToString();
                }
                else if (Session["EmpCode"] != null)
                {
                    hostEmployeeCode = Session["EmpCode"].ToString();
                }

                if (e.CommandName == "ManualCheckIn")
                {
                    if (_repository.ManualCheckIn(logId, hostEmployeeCode))
                    {
                        lblHistoryMsg.ForeColor = Color.Green;
                        lblHistoryMsg.Text = "Visitor successfully Checked-IN manually.";
                    }
                    else
                    {
                        lblHistoryMsg.ForeColor = Color.Red;
                        lblHistoryMsg.Text =
                            "Unable to Check-IN. Status may have changed, today is not approved, or the visitor is already checked in.";
                    }
                }
                else if (e.CommandName == "ManualCheckOut")
                {
                    if (_repository.ManualCheckOut(logId, hostEmployeeCode))
                    {
                        lblHistoryMsg.ForeColor = Color.Green;
                        lblHistoryMsg.Text = "Visitor successfully Checked-OUT manually.";
                    }
                    else
                    {
                        lblHistoryMsg.ForeColor = Color.Red;
                        lblHistoryMsg.Text =
                            "Unable to Check-OUT. Status may have changed, today is not approved, or the visitor is not checked in.";
                    }
                }

                // Refresh the Grid to show updated statuses and swap the buttons immediately
                BindGrids();
            }
        }
    }
}
