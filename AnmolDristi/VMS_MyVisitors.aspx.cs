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
using AnmolDristi.BLL;

namespace AnmolDristi
{
    public partial class VMS_MyVisitors : System.Web.UI.Page
    {
        string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        private readonly VmsVisitorManagementRepository _repository = new VmsVisitorManagementRepository();
        private readonly ApprovalService _approvalService = new ApprovalService();
        private Dictionary<int, List<VisitDayLogInfo>> _dayLogsByVisit;

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
            DataTable dtHistory = _repository.GetHostVisitHistory(empCode);
            LoadDayLogs(dtPending, dtHistory);

            gvPending.DataSource = dtPending;
            gvPending.DataBind();
            badgeCount.InnerText = dtPending.Rows.Count.ToString();

            BindPlantTimingsJson(dtPending);

            gvHistory.DataSource = dtHistory;
            gvHistory.DataBind();
        }

        private void LoadDayLogs(DataTable pending, DataTable history)
        {
            var logIds = new List<int>();
            CollectLogIds(pending, logIds);
            CollectLogIds(history, logIds);
            _dayLogsByVisit = logIds.Count == 0
                ? new Dictionary<int, List<VisitDayLogInfo>>()
                : _repository.GetDayLogsForVisits(logIds);
        }

        private static void CollectLogIds(DataTable dt, List<int> logIds)
        {
            if (dt == null || !dt.Columns.Contains("LogID"))
                return;

            foreach (DataRow row in dt.Rows)
            {
                if (row["LogID"] == DBNull.Value)
                    continue;
                int logId = Convert.ToInt32(row["LogID"]);
                if (logId > 0 && !logIds.Contains(logId))
                    logIds.Add(logId);
            }
        }

        private void BindPlantTimingsJson(DataTable pending)
        {
            int branchId = 0;
            if (pending != null && pending.Columns.Contains("TargetBranchID"))
            {
                foreach (DataRow row in pending.Rows)
                {
                    if (row["TargetBranchID"] == DBNull.Value)
                        continue;
                    branchId = Convert.ToInt32(row["TargetBranchID"]);
                    if (branchId > 0)
                        break;
                }
            }

            hfPlantTimingsJson.Value = branchId > 0
                ? VmsVisitScheduleHelper.GetPlantTimingsJson(branchId)
                : "{}";
        }

        protected void gvPending_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow)
                return;

            BindHostScheduleCell(e.Row, "Pending");

            LinkButton btnApprove = e.Row.FindControl("btnApprove") as LinkButton;
            if (btnApprove == null)
                return;

            int logId = Convert.ToInt32(DataBinder.Eval(e.Row.DataItem, "LogID"));
            VisitHoursEvaluation evaluation;
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();
                evaluation = VmsVisitScheduleHelper.EvaluateVisitHoursForLog(con, logId);
            }

            if (evaluation != null && evaluation.RequiresNotice)
                btnApprove.OnClientClick = ToJsConfirm(evaluation.HostWarningMessage);
        }

        private static string ToJsConfirm(string message)
        {
            if (string.IsNullOrEmpty(message))
                return "return true;";
            return "return confirm('" +
                message.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", "").Replace("\n", "\\n") +
                "');";
        }

        protected void gvHistory_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow)
                return;

            string approvalStatus = Convert.ToString(DataBinder.Eval(e.Row.DataItem, "ApprovalStatus"));
            BindHostScheduleCell(e.Row, approvalStatus);
        }

        private void BindHostScheduleCell(GridViewRow row, string approvalStatus)
        {
            Literal litDayWindow = row.FindControl("litDayWindow") as Literal;
            if (litDayWindow == null)
                return;

            object logIdObj = DataBinder.Eval(row.DataItem, "LogID");
            if (logIdObj == null || logIdObj == DBNull.Value)
                return;

            int logId = Convert.ToInt32(logIdObj);
            List<VisitDayLogInfo> days = null;
            if (_dayLogsByVisit != null)
                _dayLogsByVisit.TryGetValue(logId, out days);

            litDayWindow.Text = VmsVisitScheduleHelper.BuildHostScheduleDisplayHtml(
                Convert.ToString(DataBinder.Eval(row.DataItem, "ScheduleType")),
                approvalStatus,
                ReadDate(DataBinder.Eval(row.DataItem, "RequestedValidFrom")),
                ReadDate(DataBinder.Eval(row.DataItem, "RequestedValidUpto")),
                ReadDate(DataBinder.Eval(row.DataItem, "ValidFrom")),
                ReadDate(DataBinder.Eval(row.DataItem, "ValidUpto")),
                days);
        }

        private static DateTime? ReadDate(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;
            return Convert.ToDateTime(value);
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
                        timeCmd.Parameters.AddWithValue("@DayOfWeek", requestedValidFrom.Value.Date.ToString("dddd"));
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

            List<VisitDayLogInfo> days;
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();
                days = VmsVisitScheduleHelper.GetDayLogsForVisit(con, logId);
            }

            foreach (VisitDayLogInfo day in days)
            {
                var item = new ListItem(
                    VmsVisitScheduleHelper.FormatDayCheckboxLabel(day),
                    day.VisitDate.ToString("yyyy-MM-dd"));
                item.Selected = true;
                cblModalDays.Items.Add(item);
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

            ApprovalResult result = ApproveVisitor(
                logId,
                selectedDays,
                approvedFromTime,
                approvedToTime);

            if (result.Success)
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "HideScheduleModal",
                    "$('#ScheduleApprovalModal').modal('hide'); $('body').removeClass('modal-open'); $('.modal-backdrop').remove();", true);
            }
            else
            {
                lblModalMsg.Text = result.Message;
                ScriptManager.RegisterStartupScript(this, GetType(), "ShowScheduleModal",
                    "$('#ScheduleApprovalModal').modal('show');", true);
            }
        }

        private ApprovalResult ApproveVisitor(
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

            ApprovalResult result = _approvalService.Execute(ApprovalRequest.ForHost(
                logId,
                hostEmployeeCode,
                selectedDays,
                approvedFromTime,
                approvedToTime));

            if (result.Success)
            {
                lblPendingMsg.ForeColor = Color.Green;
                lblPendingMsg.Text = result.Message;
                BindGrids();
            }
            else
            {
                lblPendingMsg.ForeColor = Color.Red;
                lblPendingMsg.Text = result.Message;
                BindGrids();
            }

            return result;
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
