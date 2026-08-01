using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.UI.WebControls;
using System.Web.UI;

namespace AnmolDristi
{
    public partial class VMS_GateScanner : System.Web.UI.Page
    {
        string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadGateLocation();
                BindInsideVisitors();
            }
        }

        private void KeepTabActive(string tabId)
        {
            // Dynamically force Bootstrap to keep the relevant tab open after postback
            ClientScript.RegisterStartupScript(this.GetType(), "FocusTab", $"$('#{tabId}').tab('show');", true);
        }

        private void LoadGateLocation()
        {
            if (Request.QueryString["BranchId"] != null)
            {
                int branchId;
                if (int.TryParse(Request.QueryString["BranchId"].ToString(), out branchId))
                {
                    using (SqlConnection con = new SqlConnection(connStr))
                    {
                        string query = @"
                            SELECT ISNULL(s.AliasName, b.Branch_Description) AS LocationName
                            FROM MST_Branch b
                            LEFT JOIN VMS_OfficeSettings s ON b.Branch_ID = s.Branch_ID
                            WHERE b.Branch_ID = @BranchID";

                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@BranchID", branchId);
                            con.Open();
                            object result = cmd.ExecuteScalar();
                            lblGateLocation.Text = result != null ? result.ToString() : "Unknown Facility";
                        }
                    }
                }
            }
            else
            {
                lblGateLocation.Text = "Master Control / Universal Gate";
            }
        }

        private void BindInsideVisitors()
        {
            // 1. Check if the guard is at a specific branch
            int branchId = 0;
            bool isSpecificGate = false;

            if (Request.QueryString["BranchId"] != null && int.TryParse(Request.QueryString["BranchId"].ToString(), out branchId))
            {
                isSpecificGate = true;
            }

            // 2. Build the query dynamically
            string query = @"
                SELECT 
                    v.LogID,
                    ISNULL(d.DayLogID, 0) AS DayLogID,
                    v.VisitorName, 
                    v.VisitorCompany, 
                    ISNULL(NULLIF(v.VisitorMobile, ''), v.VisitorEmail) AS ContactInfo,
                    ISNULL(u.EmployeeName, 'Unknown') AS HostName,
                    COALESCE(d.ActualInTime, v.ActualInTime) AS ActualInTime,
                    COALESCE(d.DayValidUpto, v.ValidUpto) AS ValidUpto,
                    v.PhotoPath
                FROM VMS_VisitLog v
                LEFT JOIN VMS_VisitDayLog d ON v.LogID = d.LogID
                    AND d.VisitDate = CAST(GETDATE() AS date)
                    AND d.VisitStatus = 'Checked-In'
                LEFT JOIN MST_UserMaster u ON v.HostEmployeeCode = u.EmployeeCode
                WHERE (
                    (d.DayLogID IS NOT NULL)
                    OR (v.VisitStatus = 'Checked-In' AND NOT EXISTS (
                        SELECT 1 FROM VMS_VisitDayLog dx WHERE dx.LogID = v.LogID
                    ))
                )";

            // 3. Apply the Branch restriction if applicable
            if (isSpecificGate)
            {
                query += " AND v.TargetBranchID = @BranchId";
            }

            query += " ORDER BY v.ActualInTime DESC";

            using (SqlConnection con = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    // 4. Bind the parameter safely
                    if (isSpecificGate)
                    {
                        cmd.Parameters.AddWithValue("@BranchId", branchId);
                    }

                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);

                        // Bind to the new responsive Repeater instead of the old GridView
                        rptVisitorLogs.DataSource = dt;
                        rptVisitorLogs.DataBind();

                        lblInsideCount.Text = dt.Rows.Count.ToString();

                        // TRIGGER AJAX PANELS TO UPDATE WITHOUT FLICKERING THE SCREEN
                        upGrid.Update();
                        upBadge.Update();
                    }
                }
            }
        }

        // --- NEW REPEATER UI HELPER METHODS ---

        protected bool IsPassExpired(object validUptoObj)
        {
            if (validUptoObj != DBNull.Value && validUptoObj != null)
            {
                DateTime validUpto = Convert.ToDateTime(validUptoObj);
                return DateTime.Now > validUpto;
            }
            return false;
        }

        protected string GetButtonCss(object validUptoObj)
        {
            // If expired, turn the button warning yellow. If valid, standard red Check-Out.
            return IsPassExpired(validUptoObj) ? "btn btn-warning btn-block font-weight-bold" : "btn btn-danger btn-block font-weight-bold";
        }

        protected string GetButtonText(object validUptoObj)
        {
            return IsPassExpired(validUptoObj)
                ? "<i class='fa fa-exclamation-triangle'></i> Expired: Force Out"
                : "<i class='fa fa-sign-out'></i> Check-OUT";
        }

        // --- REPEATER EVENT FOR MANUAL CHECKOUT ---
        protected void rptVisitorLogs_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "ForceCheckOut")
            {
                string[] parts = e.CommandArgument.ToString().Split('|');
                int logId = Convert.ToInt32(parts[0]);
                int dayLogId = parts.Length > 1 ? Convert.ToInt32(parts[1]) : 0;
                ProcessDatabaseCheckOut(logId, dayLogId);

                lblGridMsg.Text = "Visitor successfully checked out from the premises.";

                BindInsideVisitors(); // This will auto-refresh the repeater via AJAX
                KeepTabActive("inside-tab");
            }
        }

        // --- SCANNER EVENTS ---

        protected void btnProcessScan_Click(object sender, EventArgs e)
        {
            string scannedToken = hfScannedToken.Value;
            Guid tokenGuid;

            if (!Guid.TryParse(scannedToken, out tokenGuid))
            {
                ShowResult("result-card result-danger", "Invalid QR Code format.", "This does not belong to the Anmol VMS system.", "", null);
                return;
            }

            int scannerBranchId = 0;
            bool isSpecificGate = false;
            if (Request.QueryString["BranchId"] != null && int.TryParse(Request.QueryString["BranchId"], out scannerBranchId))
            {
                isSpecificGate = true;
            }

            using (SqlConnection con = new SqlConnection(connStr))
            {
                string query = @"
                SELECT 
                    v.LogID, v.VisitorName, v.VisitorCompany, v.VisitorMobile, v.VisitorEmail, 
                    v.ApprovalStatus, v.VisitStatus, v.ValidFrom, v.ValidUpto, v.PhotoPath, v.ActualInTime, v.PassCategory,
                    v.TargetBranchID,
                    u.EmployeeName AS HostName, u.Email AS HostEmail,
                    ISNULL(d.Department_Description, 'General') AS HostDepartment,
                    ISNULL(b.Branch_Description, 'Unknown Branch') AS VisitBranchName
                FROM VMS_VisitLog v
                LEFT JOIN MST_UserMaster u ON v.HostEmployeeCode = u.EmployeeCode
                LEFT JOIN MST_Department d ON CAST(u.DepartmentId AS VARCHAR) = d.Department_ID
                LEFT JOIN MST_Branch b ON v.TargetBranchID = b.Branch_ID
                WHERE v.QRToken = @Token";

                con.Open();

                int logId = 0;
                string status = "";
                string visitStatus = "Scheduled";
                DateTime validFrom = DateTime.Now;
                DateTime validUpto = DateTime.Now;
                string photoPath = "";
                int visitBranchId = 0;
                string visitBranchName = "";
                string visName = "";
                string company = "";
                string contact = "";
                string hostName = "";
                string hostDept = "";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Token", tokenGuid);
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        if (!sdr.Read())
                        {
                            ShowResult("result-card result-danger", "Pass Not Found", "This pass does not exist in the database.", "", null);
                            return;
                        }

                        logId = Convert.ToInt32(sdr["LogID"]);
                        status = sdr["ApprovalStatus"].ToString();
                        visitStatus = sdr["VisitStatus"] != DBNull.Value && !string.IsNullOrWhiteSpace(sdr["VisitStatus"].ToString())
                            ? sdr["VisitStatus"].ToString()
                            : "Scheduled";
                        validFrom = Convert.ToDateTime(sdr["ValidFrom"]);
                        validUpto = Convert.ToDateTime(sdr["ValidUpto"]);
                        photoPath = sdr["PhotoPath"].ToString();
                        visitBranchId = sdr["TargetBranchID"] != DBNull.Value ? Convert.ToInt32(sdr["TargetBranchID"]) : 0;
                        visitBranchName = sdr["VisitBranchName"].ToString();
                        visName = sdr["VisitorName"].ToString();
                        string category = sdr["PassCategory"].ToString();
                        company = string.IsNullOrEmpty(category) ? sdr["VisitorCompany"].ToString() : $"[{category}] - {sdr["VisitorCompany"]}";
                        contact = !string.IsNullOrEmpty(sdr["VisitorMobile"].ToString()) ? sdr["VisitorMobile"].ToString() : sdr["VisitorEmail"].ToString();
                        hostName = sdr["HostName"].ToString();
                        hostDept = sdr["HostDepartment"].ToString();

                        hfCurrentLogId.Value = logId.ToString();
                        hfHostEmail.Value = sdr["HostEmail"].ToString();
                        hfHostName.Value = hostName;
                        hfVisName.Value = visName;
                        hfCompany.Value = sdr["VisitorCompany"].ToString();
                    }
                }

                if (isSpecificGate && scannerBranchId != visitBranchId)
                {
                    ShowResult("result-card result-danger",
                        "Wrong Facility Alert",
                        $"This pass is strictly valid for <b>{visitBranchName}</b>. The visitor is at the wrong location.",
                        photoPath, visName, company, contact, hostName, hostDept, validUpto.ToString("dd MMM, hh:mm tt"));
                    return;
                }

                if (status != "Approved")
                {
                    ShowResult("result-card result-danger", "Access Denied", $"Pass is currently marked as: {status}", photoPath, null);
                    return;
                }

                VisitDayLogInfo todayLog = VmsVisitScheduleHelper.GetTodayDayLogOrLegacyFallback(
                    con, logId, validFrom, validUpto, visitStatus);

                if (todayLog == null)
                {
                    ShowResult("result-card result-danger", "No Visit Today",
                        "This pass does not have an approved visit scheduled for today.",
                        photoPath, visName, company, contact, hostName, hostDept, validUpto.ToString("dd MMM, hh:mm tt"));
                    return;
                }

                if (!todayLog.IsDayApproved)
                {
                    ShowResult("result-card result-danger", "Day Not Approved",
                        "Today's visit has not been approved by the host yet.",
                        photoPath, visName, company, contact, hostName, hostDept, todayLog.DayValidUpto.ToString("dd MMM, hh:mm tt"));
                    return;
                }

                hfCurrentDayLogId.Value = todayLog.DayLogId.ToString();
                string validUptoStr = todayLog.DayValidUpto.ToString("dd MMM, hh:mm tt");
                string dayStatus = todayLog.VisitStatus ?? "Scheduled";
                DateTime now = DateTime.Now;

                if (dayStatus == "Scheduled")
                {
                    if (now < todayLog.DayValidFrom)
                    {
                        ShowResult("result-card result-warning", "Too Early",
                            $"Today's visiting window starts at {todayLog.DayValidFrom:hh:mm tt}.",
                            photoPath, visName, company, contact, hostName, hostDept, validUptoStr);
                        return;
                    }

                    if (now > todayLog.DayValidUpto)
                    {
                        ShowResult("result-card result-danger", "Today's Window Ended",
                            $"Today's approved visiting hours ended at {todayLog.DayValidUpto:hh:mm tt}.",
                            photoPath, visName, company, contact, hostName, hostDept, validUptoStr);
                        return;
                    }

                    btnManualCheckIn.Enabled = true;
                    btnManualCheckOut.Enabled = false;
                    liCheckInTime.Visible = false;
                    ShowResult("result-card result-info", "Visitor Identified",
                        $"Approved for today ({todayLog.DayValidFrom:hh:mm tt} - {todayLog.DayValidUpto:hh:mm tt}). Verify photo and confirm Check-IN.",
                        photoPath, visName, company, contact, hostName, hostDept, validUptoStr);
                }
                else if (dayStatus == "Checked-In")
                {
                    DateTime inTime = todayLog.ActualInTime ?? DateTime.Now;
                    liCheckInTime.Visible = true;
                    lblDetInTime.Text = inTime.ToString("dd MMM, hh:mm tt");
                    btnManualCheckIn.Enabled = false;

                    if (DateTime.Now < inTime.AddMinutes(10))
                    {
                        btnManualCheckOut.Enabled = false;
                        btnManualCheckOut.Text = "Cannot Check-OUT yet (< 10 mins)";
                    }
                    else
                    {
                        btnManualCheckOut.Enabled = true;
                        btnManualCheckOut.Text = "Confirm Check-OUT";
                    }

                    if (DateTime.Now > todayLog.DayValidUpto)
                    {
                        ShowResult("result-card result-warning", "Visitor Overstayed",
                            $"Today's window ended at {validUptoStr}. Please verify and confirm Check-OUT.",
                            photoPath, visName, company, contact, hostName, hostDept, validUptoStr);
                    }
                    else
                    {
                        ShowResult("result-card result-info", "Visitor Exiting",
                            "Review today's check-in time and confirm Check-OUT.",
                            photoPath, visName, company, contact, hostName, hostDept, validUptoStr);
                    }
                }
                else if (dayStatus == "Checked-Out")
                {
                    ShowResult("result-card result-danger", "Already Checked-Out Today",
                        $"{visName} has already completed today's gate check-out.",
                        photoPath, visName, company, contact, hostName, hostDept, validUptoStr);
                }
            }
        }

        protected void btnManualCheckIn_Click(object sender, EventArgs e)
        {
            int logId = Convert.ToInt32(hfCurrentLogId.Value);
            int dayLogId = 0;
            int.TryParse(hfCurrentDayLogId.Value, out dayLogId);
            string guardId = Session["EmployeeCode"]?.ToString() ?? "SecurityGate";

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();
                if (dayLogId > 0)
                    VmsVisitScheduleHelper.CheckInDay(con, dayLogId, logId, guardId);
                else
                    UpdateGateStatus(con, logId, "Checked-In", "ActualInTime");

                if (!string.IsNullOrEmpty(hfHostEmail.Value))
                {
                    NotifyHostArrival(con, logId, hfHostName.Value, hfHostEmail.Value, hfVisName.Value, hfCompany.Value);
                }
            }

            divActionButtons.Visible = false;
            divFinishButtons.Visible = true;

            pnlCamera.Visible = false;
            pnlResult.Visible = true;
            pnlResult.CssClass = "result-card result-success";
            lblScanTitle.Text = "<i class='fa fa-check-circle'></i> Check-In Confirmed";
            lblScanMessage.Text = "The visitor has been logged in. Please guide them to their host.";

            BindInsideVisitors();
            KeepTabActive("scanner-tab");
        }

        protected void btnManualCheckOut_Click(object sender, EventArgs e)
        {
            int logId = Convert.ToInt32(hfCurrentLogId.Value);
            int dayLogId = 0;
            int.TryParse(hfCurrentDayLogId.Value, out dayLogId);
            ProcessDatabaseCheckOut(logId, dayLogId);

            divActionButtons.Visible = false;
            divFinishButtons.Visible = true;

            pnlCamera.Visible = false;
            pnlResult.Visible = true;
            pnlResult.CssClass = "result-card result-success";
            lblScanTitle.Text = "<i class='fa fa-check-circle'></i> Check-Out Confirmed";
            lblScanMessage.Text = "The visitor departure has been logged successfully.";

            BindInsideVisitors();
            KeepTabActive("scanner-tab");
        }

        // --- REUSABLE CHECK-OUT DATABASE LOGIC ---
        private void ProcessDatabaseCheckOut(int logId, int dayLogId = 0)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                string visName = "", company = "", hostName = "", hostEmail = "";
                string guardId = Session["EmployeeCode"]?.ToString() ?? "SecurityGate";

                string query = @"
                    SELECT v.VisitorName, v.VisitorCompany, u.EmployeeName AS HostName, u.Email AS HostEmail 
                    FROM VMS_VisitLog v 
                    LEFT JOIN MST_UserMaster u ON v.HostEmployeeCode = u.EmployeeCode 
                    WHERE v.LogID = @LogID";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@LogID", logId);
                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        if (sdr.Read())
                        {
                            visName = sdr["VisitorName"].ToString();
                            company = sdr["VisitorCompany"].ToString();
                            hostName = sdr["HostName"].ToString();
                            hostEmail = sdr["HostEmail"].ToString();
                        }
                    }
                }

                if (dayLogId <= 0)
                {
                    VisitDayLogInfo todayLog = VmsVisitScheduleHelper.GetTodayDayLog(con, logId);
                    if (todayLog != null)
                        dayLogId = todayLog.DayLogId;
                }

                if (dayLogId > 0)
                    VmsVisitScheduleHelper.CheckOutDay(con, dayLogId, logId, guardId);
                else
                    UpdateGateStatus(con, logId, "Checked-Out", "ActualOutTime");

                if (!string.IsNullOrEmpty(hostEmail))
                {
                    NotifyHostDeparture(con, logId, hostName, hostEmail, visName, company);
                }
            }
        }

        // --- UTILITIES ---
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

            string qQuery = "INSERT INTO NotificationQueue (FormSubmissionId, EscalationLevel, RecipientEmail, RecipientName, Subject, Body, NotificationType, Status, SourceModule) VALUES (@LogId, 0, @Email, @Name, @Subject, @Body, 'Email', 'Pending', 'VMS_DEPARTURE')";
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

        private void ShowResult(string cssClass, string title, string message, string photoPath,
            string visName = null, string comp = null, string contact = null,
            string hostName = null, string hostDept = null, string validStr = null)
        {
            pnlCamera.Visible = false;

            pnlResult.Visible = true;
            pnlResult.CssClass = cssClass;

            lblScanTitle.Text = title;
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

            if (visName != null)
            {
                pnlVisitDetails.Visible = true;
                lblDetName.Text = visName;
                lblDetCompany.Text = comp;
                lblDetContact.Text = contact;
                lblDetHost.Text = hostName;
                lblDetDept.Text = hostDept;
                lblDetValid.Text = validStr;

                if (cssClass.Contains("info") || cssClass.Contains("warning"))
                {
                    divActionButtons.Visible = true;
                    divFinishButtons.Visible = false;
                }
                else
                {
                    divActionButtons.Visible = false;
                    divFinishButtons.Visible = true;
                }
            }
            else
            {
                pnlVisitDetails.Visible = false;
                divActionButtons.Visible = false;
                divFinishButtons.Visible = true;
            }

            KeepTabActive("scanner-tab");
        }

        protected void btnResetScanner_Click(object sender, EventArgs e)
        {
            pnlCamera.Visible = true;
            pnlResult.Visible = false;
            hfScannedToken.Value = "";
            KeepTabActive("scanner-tab");
        }
    }
}