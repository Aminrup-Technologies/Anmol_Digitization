using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.UI.WebControls;
using System.Drawing;
using System.Web.UI;
using AnmolDristi.DAL;

namespace AnmolDristi
{
    public partial class VMS_AdminPending : System.Web.UI.Page
    {
        string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        private readonly VmsVisitorManagementRepository _visitorManagementRepository =
            new VmsVisitorManagementRepository();
        private Dictionary<int, List<VisitDayLogInfo>> _dayLogsByVisit;

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
                // RESTORED: Added back 'where VMS_Menu=1' from your original code
                string query = "SELECT Branch_ID, Branch_Description FROM MST_Branch WHERE VMS_Menu=1 ORDER BY Branch_Description";
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
                // --- TAB 1: PENDING ACTIONS (MERGED) ---
                string pendingQuery = @"
                SELECT 
                    v.LogID, v.VisitorName, v.VisitorEmail, v.VisitorCompany, v.PhotoPath,
                    v.PurposeOfVisit, v.CreatedDate, v.TargetBranchID,
                    ISNULL(v.ScheduleType, 'SingleDay') AS ScheduleType,
                    v.RequestedValidFrom, v.RequestedValidUpto, v.ValidFrom, v.ValidUpto,
                    v.ApprovalStatus,
                    u.EmployeeName AS HostName, u.EmployeeCode AS HostEmployeeCode,
                    u.Email AS HostEmail, ISNULL(b.Branch_Description, 'Unknown Location') AS Branch_Description
                FROM VMS_VisitLog v
                LEFT JOIN MST_UserMaster u ON v.HostEmployeeCode = u.EmployeeCode
                LEFT JOIN MST_Branch b ON v.TargetBranchID = b.Branch_ID
                WHERE v.ApprovalStatus = 'Pending'
                ORDER BY v.CreatedDate DESC";

                DataTable dtPending = new DataTable();
                using (SqlDataAdapter sda = new SqlDataAdapter(pendingQuery, con))
                {
                    sda.Fill(dtPending);
                }

                // --- TAB 2: GATE OVERRIDE ---
                string activeQuery = @"
                SELECT 
                    v.LogID, v.VisitorName, v.VisitorEmail, v.VisitStatus, v.PhotoPath,
                    v.ActualInTime, v.ActualOutTime, v.ValidFrom, v.ValidUpto,
                    ISNULL(v.ScheduleType, 'SingleDay') AS ScheduleType,
                    v.RequestedValidFrom, v.RequestedValidUpto, v.ApprovalStatus,
                    v.CreatedDate, v.ApprovedOn,
                    u.EmployeeName AS HostName, u.DepartmentId, 
                    u.EmployeeCode AS HostEmployeeCode, u.Email AS HostEmail
                FROM VMS_VisitLog v
                LEFT JOIN MST_UserMaster u ON v.HostEmployeeCode = u.EmployeeCode
                WHERE v.ApprovalStatus = 'Approved' 
                  AND v.VisitStatus IN ('Scheduled', 'Checked-In') " +
                          (branchFilter != "0" ? " AND v.TargetBranchID = @BranchID " : "") +
                        " ORDER BY v.ApprovedOn DESC";

                DataTable dtActive = new DataTable();
                using (SqlCommand cmd = new SqlCommand(activeQuery, con))
                {
                    if (branchFilter != "0") cmd.Parameters.AddWithValue("@BranchID", branchFilter);
                    using (SqlDataAdapter sdaActive = new SqlDataAdapter(cmd))
                    {
                        sdaActive.Fill(dtActive);
                    }
                }

                LoadDayLogs(dtPending, dtActive);
                gvAdminPending.DataSource = dtPending;
                gvAdminPending.DataBind();
                gvAdminGateOverride.DataSource = dtActive;
                gvAdminGateOverride.DataBind();
            }
        }

        private void LoadDayLogs(DataTable pending, DataTable active)
        {
            var logIds = new List<int>();
            CollectLogIds(pending, logIds);
            CollectLogIds(active, logIds);
            _dayLogsByVisit = logIds.Count == 0
                ? new Dictionary<int, List<VisitDayLogInfo>>()
                : _visitorManagementRepository.GetDayLogsForVisits(logIds);
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

        private void BindScheduleCell(GridViewRow row, string approvalStatus)
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

        // ==========================================
        // TAB 1: UNIFIED ACTIONS (Remind, Sync, Approve, Delete, Logs)
        // ==========================================
        protected void gvAdminPending_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow)
                return;

            BindScheduleCell(e.Row, "Pending");
        }

        protected void txtHostCode_TextChanged(object sender, EventArgs e)
        {
            TextBox txtCode = (TextBox)sender;
            GridViewRow row = (GridViewRow)txtCode.NamingContainer;

            Label lblDetails = (Label)row.FindControl("lblHostDetails");
            HiddenField hfBranch = (HiddenField)row.FindControl("hfTargetBranchID");

            string empCode = txtCode.Text.Trim();
            string branchId = hfBranch.Value;

            if (string.IsNullOrEmpty(empCode))
            {
                lblDetails.Text = "<span class='text-danger'><i class='fa fa-times-circle'></i> Code required</span>";
                return;
            }

            using (SqlConnection con = new SqlConnection(connStr))
            {
                string query = "SELECT EmployeeName, Email FROM MST_UserMaster WHERE EmployeeCode = @Code AND BranchId = @BranchId AND EmployeeStatus = 'Active'";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Code", empCode);
                    cmd.Parameters.AddWithValue("@BranchId", branchId);
                    con.Open();

                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        if (sdr.Read())
                        {
                            lblDetails.Text = $"<strong style='color:#1ABB9C;'><i class='fa fa-check-circle'></i> {sdr["EmployeeName"]}</strong><br/><small class='text-muted'><i class='fa fa-envelope'></i> {sdr["Email"]}</small>";
                        }
                        else
                        {
                            lblDetails.Text = "<span class='text-danger'><i class='fa fa-exclamation-triangle'></i> Invalid or Wrong Branch</span>";
                        }
                    }
                }
            }
            ScriptManager.RegisterStartupScript(this, this.GetType(), "FocusTab", "$('#adminTabs a[href=\"#tabPending\"]').tab('show');", true);
        }

        protected void gvAdminPending_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            GridViewRow row = (GridViewRow)((Control)e.CommandSource).NamingContainer;
            int logId = Convert.ToInt32(gvAdminPending.DataKeys[row.RowIndex].Values["LogID"]);

            if (e.CommandName == "Retrigger")
            {
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
            }
            else if (e.CommandName == "SoftDelete")
            {
                SoftDeleteDuplicate(logId);
            }
            else if (e.CommandName == "ViewEmails")
            {
                ViewEmailLogs(logId);
            }
            else if (e.CommandName == "SyncHost")
            {
                // AR-VMS-11: Pending-only host reassignment.
                // TargetBranchID and Pending status come from VMS_VisitLog (not hfTargetBranchID).
                TextBox txtCode = (TextBox)row.FindControl("txtHostCode");
                string newHostCode = txtCode != null ? txtCode.Text.Trim() : string.Empty;

                if (string.IsNullOrEmpty(newHostCode))
                {
                    lblMessage.ForeColor = Color.Red;
                    lblMessage.Text = "<i class='fa fa-exclamation-triangle'></i> Save failed. Host employee code is required.";
                }
                else
                {
                    using (SqlConnection con = new SqlConnection(connStr))
                    {
                        con.Open();

                        object branchObj;
                        using (SqlCommand branchCmd = new SqlCommand(@"
                            SELECT TargetBranchID
                            FROM VMS_VisitLog
                            WHERE LogID = @LogID
                              AND ApprovalStatus = 'Pending'", con))
                        {
                            branchCmd.Parameters.AddWithValue("@LogID", logId);
                            branchObj = branchCmd.ExecuteScalar();
                        }

                        if (branchObj == null || branchObj == DBNull.Value)
                        {
                            lblMessage.ForeColor = Color.Red;
                            lblMessage.Text = "<i class='fa fa-exclamation-triangle'></i> Save failed. Visit is not Pending or was not found.";
                        }
                        else
                        {
                            int targetBranchId = Convert.ToInt32(branchObj);

                            int activeHostCount;
                            using (SqlCommand checkCmd = new SqlCommand(@"
                                SELECT COUNT(1)
                                FROM MST_UserMaster
                                WHERE EmployeeCode = @Code
                                  AND BranchId = @BranchId
                                  AND EmployeeStatus = 'Active'", con))
                            {
                                checkCmd.Parameters.AddWithValue("@Code", newHostCode);
                                checkCmd.Parameters.AddWithValue("@BranchId", targetBranchId);
                                activeHostCount = Convert.ToInt32(checkCmd.ExecuteScalar());
                            }

                            if (activeHostCount <= 0)
                            {
                                lblMessage.ForeColor = Color.Red;
                                lblMessage.Text = "<i class='fa fa-exclamation-triangle'></i> Save failed. Invalid or wrong branch.";
                            }
                            else
                            {
                                int rowsAffected;
                                using (SqlCommand updateCmd = new SqlCommand(@"
                                    UPDATE VMS_VisitLog
                                    SET HostEmployeeCode = @Code
                                    WHERE LogID = @LogID
                                      AND ApprovalStatus = 'Pending'
                                      AND TargetBranchID = @BranchId", con))
                                {
                                    updateCmd.Parameters.AddWithValue("@Code", newHostCode);
                                    updateCmd.Parameters.AddWithValue("@LogID", logId);
                                    updateCmd.Parameters.AddWithValue("@BranchId", targetBranchId);
                                    rowsAffected = updateCmd.ExecuteNonQuery();
                                }

                                if (rowsAffected == 1)
                                {
                                    lblMessage.ForeColor = Color.Green;
                                    lblMessage.Text = $"<i class='fa fa-check-circle'></i> Host successfully reassigned to Code: {newHostCode}.";
                                }
                                else
                                {
                                    lblMessage.ForeColor = Color.Red;
                                    lblMessage.Text = "<i class='fa fa-exclamation-triangle'></i> Save failed. Visit status changed. Please refresh and try again.";
                                }
                            }
                        }
                    }
                }
            }
            else if (e.CommandName == "ForceApprove")
            {
                string visEmail = gvAdminPending.DataKeys[row.RowIndex].Values["VisitorEmail"].ToString();
                string visName = gvAdminPending.DataKeys[row.RowIndex].Values["VisitorName"].ToString();
                string hostEmail = gvAdminPending.DataKeys[row.RowIndex].Values["HostEmail"].ToString();
                string hostName = gvAdminPending.DataKeys[row.RowIndex].Values["HostName"].ToString();

                TextBox txtRemarks = (TextBox)row.FindControl("txtRemarks");
                string adminRemarks = txtRemarks.Text.Trim();

                TextBox txtDate = (TextBox)row.FindControl("txtScheduleDate");
                DateTime schedDate = string.IsNullOrEmpty(txtDate.Text) ? DateTime.Now : Convert.ToDateTime(txtDate.Text);

                if (string.IsNullOrEmpty(adminRemarks))
                {
                    lblMessage.ForeColor = Color.Red;
                    lblMessage.Text = "<i class='fa fa-exclamation-triangle'></i> You must enter an Admin Remark to approve on-behalf.";
                    KeepTabActive("tabPending"); return;
                }

                string adminName = Session["UserName"] != null ? Session["UserName"].ToString() : "Admin";
                ApproveOnBehalf(logId, visEmail, visName, hostEmail, hostName, $"[On-Behalf by {adminName}] - {adminRemarks}", schedDate);
            }

            BindAllGrids();
            KeepTabActive("tabPending");
        }


        // ==========================================
        // TAB 2: GATE OVERRIDE (AUTO-SAVE & AUDIT FIX)
        // ==========================================
        protected void gvAdminGateOverride_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                BindScheduleCell(e.Row, "Approved");

                int currentRowLogId = Convert.ToInt32(gvAdminGateOverride.DataKeys[e.Row.RowIndex].Values["LogID"]);

                // Preserve the typed audit reason during postbacks
                if (ViewState["LastOverrideLogID"] != null && ViewState["LastOverrideReason"] != null)
                {
                    if (currentRowLogId == Convert.ToInt32(ViewState["LastOverrideLogID"]))
                    {
                        TextBox txtReason = (TextBox)e.Row.FindControl("txtOverrideReason");
                        if (txtReason != null) txtReason.Text = ViewState["LastOverrideReason"].ToString();
                    }
                }
            }
        }

        protected void InlineEdit_TextChanged(object sender, EventArgs e)
        {
            TextBox txtChanged = (TextBox)sender;
            GridViewRow row = (GridViewRow)txtChanged.NamingContainer;

            int logId = Convert.ToInt32(gvAdminGateOverride.DataKeys[row.RowIndex].Values["LogID"]);
            string visName = gvAdminGateOverride.DataKeys[row.RowIndex].Values["VisitorName"].ToString();

            TextBox txtReason = (TextBox)row.FindControl("txtOverrideReason");
            string reason = txtReason.Text.Trim();

            if (string.IsNullOrEmpty(reason))
            {
                ShowValidationError("You must type an Audit Reason BEFORE changing the date/time.");
                return;
            }

            ViewState["LastOverrideLogID"] = logId;
            ViewState["LastOverrideReason"] = reason;

            DateTime newTime;
            if (!DateTime.TryParse(txtChanged.Text, out newTime))
            {
                ShowValidationError("Invalid time format selected.");
                return;
            }

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

            string adminName = Session["UserName"] != null ? Session["UserName"].ToString() : "Admin";

            // AR-VMS-04: In/Out corrections go through repository (timestamp-aware DayLog sync).
            if (txtChanged.ID == "txtInTime")
            {
                if (approvedOn.HasValue && newTime < approvedOn.Value) { ShowValidationError($"Check-In ({newTime:HH:mm}) cannot be BEFORE Approval."); return; }
                if (newTime > DateTime.Now) { ShowValidationError("Check-In cannot be set in the future."); return; }
                if (currentOutTime.HasValue && newTime > currentOutTime.Value) { ShowValidationError("Check-In cannot be AFTER Check-Out."); return; }
                if (currentValidUpto.HasValue && newTime > currentValidUpto.Value) { ShowValidationError("Check-In cannot be AFTER Pass Expiry."); return; }

                AdminCorrectionResult result = _visitorManagementRepository.AdminCorrectInTime(
                    logId, newTime, reason, adminName);
                HandleAdminCorrectionResult(result, "Check-In Time", visName);
                return;
            }

            if (txtChanged.ID == "txtOutTime")
            {
                if (!currentInTime.HasValue) { ShowValidationError("Cannot set Check-Out. Visitor has no Check-In time yet."); return; }
                if (newTime < currentInTime.Value) { ShowValidationError("Check-Out cannot be BEFORE Check-In."); return; }
                if (newTime > DateTime.Now) { ShowValidationError("Check-Out cannot be set in the future."); return; }
                if (currentValidUpto.HasValue && newTime > currentValidUpto.Value) { ShowValidationError("Check-Out cannot be AFTER Pass Expiry."); return; }

                AdminCorrectionResult result = _visitorManagementRepository.AdminCorrectOutTime(
                    logId, newTime, reason, adminName);
                HandleAdminCorrectionResult(result, "Check-Out Time", visName);
                return;
            }

            if (txtChanged.ID != "txtValidUpto")
            {
                ShowValidationError("Unrecognized override field.");
                return;
            }

            // ValidUpto path — AR-GS-11C unchanged (not part of AR-VMS-04 In/Out correction).
            if (currentOutTime.HasValue && newTime < currentOutTime.Value) { ShowValidationError("Pass Expiry cannot be set BEFORE Check-Out."); return; }
            else if (currentInTime.HasValue && newTime < currentInTime.Value) { ShowValidationError("Pass Expiry cannot be set BEFORE Check-In."); return; }
            else if (approvedOn.HasValue && newTime < approvedOn.Value) { ShowValidationError("Pass Expiry cannot be set BEFORE Approval."); return; }

            string friendlyName = "Pass Validity";
            string oldTimeRecord = currentValidUpto.HasValue
                ? currentValidUpto.Value.ToString("dd-MMM HH:mm")
                : "Empty";
            string auditTrail = $" | [{friendlyName}: {oldTimeRecord}->{newTime:dd-MMM HH:mm} by {adminName}] Rsn: {reason}";

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                // AR-GS-11C: SingleDay ValidUpto edits must sync the corresponding DayLog atomically.
                using (SqlTransaction trans = con.BeginTransaction())
                {
                    try
                    {
                        string scheduleType = "SingleDay";
                        using (SqlCommand scheduleCmd = new SqlCommand(
                            "SELECT ISNULL(ScheduleType, 'SingleDay') AS ScheduleType FROM VMS_VisitLog WHERE LogID = @LogID",
                            con,
                            trans))
                        {
                            scheduleCmd.Parameters.AddWithValue("@LogID", logId);
                            object scheduleResult = scheduleCmd.ExecuteScalar();
                            if (scheduleResult != null && scheduleResult != DBNull.Value)
                                scheduleType = scheduleResult.ToString();
                        }

                        // AR-VMS-12: Parent ValidUpto mutation is authoritative — reassert lifecycle
                        // state in SQL. UI/grid filtering alone is not authorization.
                        string updateQuery = @"
                            UPDATE VMS_VisitLog 
                            SET ValidUpto = @NewTime, 
                                ApprovalRemarks = LEFT(ISNULL(ApprovalRemarks, '') + @AuditTrail, 250) 
                            WHERE LogID = @LogID
                              AND ApprovalStatus = 'Approved'
                              AND VisitStatus IN ('Scheduled', 'Checked-In')";

                        int parentRowsAffected;
                        using (SqlCommand cmd = new SqlCommand(updateQuery, con, trans))
                        {
                            cmd.Parameters.AddWithValue("@NewTime", newTime);
                            cmd.Parameters.AddWithValue("@AuditTrail", auditTrail);
                            cmd.Parameters.AddWithValue("@LogID", logId);
                            parentRowsAffected = cmd.ExecuteNonQuery();
                        }

                        if (parentRowsAffected != 1)
                        {
                            try { trans.Rollback(); } catch { }
                            ShowValidationError(
                                "Cannot update Pass Validity. Visit is no longer Approved and Scheduled/Checked-In. Please refresh.");
                            return;
                        }

                        if (string.Equals(scheduleType, "SingleDay", StringComparison.OrdinalIgnoreCase))
                        {
                            int dayLogCount;
                            int dayLogId = 0;
                            using (SqlCommand countCmd = new SqlCommand(@"
                                SELECT COUNT(1) AS DayCount,
                                       ISNULL(MAX(DayLogID), 0) AS OnlyDayLogID
                                FROM VMS_VisitDayLog
                                WHERE LogID = @LogID", con, trans))
                            {
                                countCmd.Parameters.AddWithValue("@LogID", logId);
                                using (SqlDataReader reader = countCmd.ExecuteReader())
                                {
                                    reader.Read();
                                    dayLogCount = Convert.ToInt32(reader["DayCount"]);
                                    dayLogId = Convert.ToInt32(reader["OnlyDayLogID"]);
                                }
                            }

                            // Ambiguous or missing DayLog: do not guess — reject before partial commit.
                            if (dayLogCount != 1)
                            {
                                try { trans.Rollback(); } catch { }
                                ShowValidationError(
                                    "Cannot sync SingleDay pass validity: expected exactly one VisitDayLog row.");
                                return;
                            }

                            using (SqlCommand dayCmd = new SqlCommand(@"
                                UPDATE VMS_VisitDayLog
                                SET DayValidUpto = @NewTime
                                WHERE DayLogID = @DayLogID
                                  AND LogID = @LogID", con, trans))
                            {
                                dayCmd.Parameters.AddWithValue("@NewTime", newTime);
                                dayCmd.Parameters.AddWithValue("@DayLogID", dayLogId);
                                dayCmd.Parameters.AddWithValue("@LogID", logId);
                                if (dayCmd.ExecuteNonQuery() != 1)
                                {
                                    try { trans.Rollback(); } catch { }
                                    ShowValidationError("Failed to synchronize SingleDay VisitDayLog validity.");
                                    return;
                                }
                            }
                        }

                        // MultiDay: parent ValidUpto only — do not overwrite per-day windows.
                        trans.Commit();
                    }
                    catch
                    {
                        try { trans.Rollback(); } catch { }
                        throw;
                    }
                }
            }

            lblMessage.ForeColor = System.Drawing.Color.Green;
            lblMessage.Text = $"<i class='fa fa-check-circle'></i> Auto-Saved: {friendlyName} updated for {visName}.";

            BindAllGrids();
            KeepTabActive("tabGateOverride");
        }

        private void HandleAdminCorrectionResult(AdminCorrectionResult result, string friendlyName, string visName)
        {
            if (result != null && result.Succeeded)
            {
                lblMessage.ForeColor = System.Drawing.Color.Green;
                lblMessage.Text = $"<i class='fa fa-check-circle'></i> Auto-Saved: {friendlyName} updated for {visName}.";
                BindAllGrids();
                KeepTabActive("tabGateOverride");
                return;
            }

            string message;
            AdminCorrectionFailureReason reason = result != null
                ? result.FailureReason
                : AdminCorrectionFailureReason.UnexpectedFailure;

            switch (reason)
            {
                case AdminCorrectionFailureReason.UnapprovedToday:
                    message = "Today's visit day is not approved. Admin time correction was rejected.";
                    break;
                case AdminCorrectionFailureReason.StateChanged:
                    message = "Visit status changed or is not eligible for this correction (concurrent update or wrong lifecycle state).";
                    break;
                default:
                    message = "Admin time correction failed unexpectedly.";
                    break;
            }

            ShowValidationError(message);
        }

        protected void gvAdminGateOverride_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "ViewEmails")
            {
                int logId = Convert.ToInt32(e.CommandArgument);
                ViewEmailLogs(logId);
                KeepTabActive("tabGateOverride");
            }
        }

        private void ShowValidationError(string message)
        {
            lblMessage.ForeColor = System.Drawing.Color.Red;
            lblMessage.Text = $"<i class='fa fa-exclamation-triangle'></i> Update Cancelled: {message}";
            BindAllGrids();
            KeepTabActive("tabGateOverride");
        }

        // ==========================================
        // SHARED METHODS: EMAIL, DELETE, NOTIFY
        // ==========================================
        private void ViewEmailLogs(int logId)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                string q = "SELECT RecipientEmail, RecipientName, Subject, Body, Status, CreatedOn, SentOn FROM NotificationQueue WHERE FormSubmissionId = @LogId and SourceModule like '%VMS_%' ORDER BY CreatedOn DESC";
                using (SqlDataAdapter sda = new SqlDataAdapter(q, con))
                {
                    sda.SelectCommand.Parameters.AddWithValue("@LogId", logId);
                    DataTable dt = new DataTable();
                    sda.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        rptEmailLogs.DataSource = dt;
                        rptEmailLogs.DataBind();
                        rptEmailLogs.Visible = true;
                        lblNoEmails.Visible = false;
                    }
                    else
                    {
                        rptEmailLogs.Visible = false;
                        lblNoEmails.Visible = true;
                    }
                }
            }
            ScriptManager.RegisterStartupScript(this, this.GetType(), "ShowModal", "$('#emailLogsModal').modal('show');", true);
        }

        private void SoftDeleteDuplicate(int logId)
        {
            string adminName = Session["UserName"] != null ? Session["UserName"].ToString() : "Admin";
            if (_visitorManagementRepository.SoftDeleteDuplicate(logId, adminName))
            {
                lblMessage.ForeColor = Color.Green;
                lblMessage.Text = $"<i class='fa fa-check-circle'></i> Duplicate Request #{logId} has been securely soft-deleted.";
            }
            else
            {
                lblMessage.ForeColor = Color.Red;
                lblMessage.Text = $"<i class='fa fa-exclamation-triangle'></i> Unable to delete request #{logId}. It may already have been processed.";
            }

            BindAllGrids();
            KeepTabActive("tabPending");
        }

        private void SendReminderToHost(int logId)
        {
            string hostEmail = "", hostName = "", visitorName = "", company = "", hostEmpCode = "";
            using (SqlConnection con = new SqlConnection(connStr))
            {
                string query = @"SELECT v.VisitorName, v.VisitorCompany, v.HostEmployeeCode, u.EmployeeName AS HostName, u.Email AS HostEmail 
                                 FROM VMS_VisitLog v
                                 INNER JOIN MST_UserMaster u
                                     ON v.HostEmployeeCode = u.EmployeeCode
                                    AND u.EmployeeStatus = 'Active'
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
                    string quickApproveLink = VmsHelper.BuildHostQuickLoginUrl(logId, hostEmpCode);

                    string body = $@"<div style='padding:20px; border: 2px solid #f0ad4e; font-family: Arial, sans-serif;'>
                                     <h3 style='color:#f0ad4e;'>Reminder: Visitor Waiting</h3><p>Hello <b>{hostName}</b>,</p>
                                     <p>Priority reminder: <b>{visitorName}</b> from <b>{company}</b> is waiting for approval.</p><br/>
                                     <a href='{quickApproveLink}' style='background: #1ABB9C; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block;'>Click Here to Verify & Approve</a></div>";

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
                else { throw new Exception("Host has no valid email address or code."); }
            }
        }

        // ==========================================
        // TAB 1: ON-BEHALF APPROVAL (AR-VMS-02 shared lifecycle)
        // ==========================================
        /// <summary>
        /// Admin on-behalf approval via shared VisitLog+DayLog approval core.
        /// Contract defaults: Pending-only auth; Valid* from RequestedValid* via ApproveVisitSchedule;
        /// schedDate → ApprovedOn only; MultiDay = full requested range; QR + pass notify + VMS_ADMIN_ACTION in tx.
        /// </summary>
        private void ApproveOnBehalf(int logId, string visEmail, string visName, string hostEmail, string hostName, string remarks, DateTime schedDate)
        {
            // visEmail retained for call-site compatibility; pass notify loads contact from DB inside the repository.
            VisitorApprovalResult result = _visitorManagementRepository.ApproveVisitOnBehalf(new VisitorApprovalRequest
            {
                LogId = logId,
                ApprovalRemarksSuffix = remarks,
                ApprovedOnOverride = schedDate,
                QueueHostOnBehalfNotification = true,
                OnBehalfHostEmail = hostEmail,
                OnBehalfHostName = hostName
            });

            if (!result.Succeeded)
            {
                lblMessage.ForeColor = Color.Red;
                lblMessage.Text = $"<i class='fa fa-exclamation-triangle'></i> {FormatOnBehalfFailure(result.FailureReason)}";
                return;
            }

            string validUptoText = result.ValidUpto.HasValue
                ? result.ValidUpto.Value.ToString("dd-MMM HH:mm")
                : "n/a";

            lblMessage.ForeColor = Color.Green;
            lblMessage.Text =
                $"<i class='fa fa-check-circle'></i> Visitor {visName} approved on-behalf (Valid Upto: {validUptoText}).";
        }

        private static string FormatOnBehalfFailure(ApprovalFailureReason reason)
        {
            switch (reason)
            {
                case ApprovalFailureReason.NotFoundOrNotOwned:
                    return "Unable to approve on-behalf. Visit was not found.";
                case ApprovalFailureReason.NotPending:
                    return "Unable to approve on-behalf. Visit is no longer pending.";
                default:
                    return "Unable to approve on-behalf. Please try again.";
            }
        }

        protected string RenderTimeSuggestion(string fieldType, object currentValue, object createdDate, object approvedOn, object actualInTime)
        {
            if (currentValue != DBNull.Value && currentValue != null && !string.IsNullOrEmpty(currentValue.ToString())) return "";

            DateTime reqDate = createdDate != DBNull.Value ? Convert.ToDateTime(createdDate) : DateTime.Now;
            DateTime appDate = approvedOn != DBNull.Value ? Convert.ToDateTime(approvedOn) : reqDate;
            DateTime? inTime = actualInTime != DBNull.Value ? (DateTime?)Convert.ToDateTime(actualInTime) : null;

            DateTime suggestedTime;
            switch (fieldType)
            {
                case "IN": suggestedTime = appDate.AddMinutes(5); break;
                case "OUT": suggestedTime = inTime.HasValue ? inTime.Value.AddHours(1) : appDate.AddHours(1); break;
                case "VALID": suggestedTime = appDate.Date.AddHours(23).AddMinutes(59); break;
                default: return "";
            }

            if (suggestedTime > DateTime.Now && fieldType != "VALID") { suggestedTime = DateTime.Now; }
            return $"<div style='margin-top:6px; text-align:center;'><span class='label label-info' style='cursor:pointer; display:inline-block; padding:5px 8px;' onclick=\"applySmartTime(this, '{suggestedTime:yyyy-MM-ddTHH:mm}')\"><i class='fa fa-magic'></i> {suggestedTime:dd MMM, HH:mm}</span></div>";
        }
    }
}