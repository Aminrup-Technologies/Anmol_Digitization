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
                    v.VisitorName, 
                    v.VisitorCompany, 
                    ISNULL(NULLIF(v.VisitorMobile, ''), v.VisitorEmail) AS ContactInfo,
                    ISNULL(u.EmployeeName, 'Unknown') AS HostName,
                    v.ActualInTime,
                    v.ValidUpto,
                    v.PhotoPath
                FROM VMS_VisitLog v
                LEFT JOIN MST_UserMaster u ON v.HostEmployeeCode = u.EmployeeCode
                WHERE v.VisitStatus = 'Checked-In'";

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
                        gvInside.DataSource = dt;
                        gvInside.DataBind();

                        lblInsideCount.Text = dt.Rows.Count.ToString();

                        // TRIGGER AJAX PANELS TO UPDATE WITHOUT FLICKERING THE SCREEN
                        upGrid.Update();
                        upBadge.Update();
                    }
                }
            }
        }

        private void BindInsideVisitors_OLD()
        {
            string query = @"
                SELECT 
                    v.LogID,
                    v.VisitorName, 
                    v.VisitorCompany, 
                    ISNULL(NULLIF(v.VisitorMobile, ''), v.VisitorEmail) AS ContactInfo,
                    ISNULL(u.EmployeeName, 'Unknown') AS HostName,
                    v.ActualInTime,
                    v.ValidUpto,
                    v.PhotoPath
                FROM VMS_VisitLog v
                LEFT JOIN MST_UserMaster u ON v.HostEmployeeCode = u.EmployeeCode
                WHERE v.VisitStatus = 'Checked-In'
                ORDER BY v.ActualInTime DESC";

            using (SqlConnection con = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        gvInside.DataSource = dt;
                        gvInside.DataBind();

                        lblInsideCount.Text = dt.Rows.Count.ToString();

                        // TRIGGER AJAX PANELS TO UPDATE WITHOUT FLICKERING THE SCREEN
                        upGrid.Update();
                        upBadge.Update();
                    }
                }
            }
        }

        // --- GRIDVIEW EVENTS FOR MANUAL CHECKOUT ---
        protected void gvInside_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DateTime validUpto = Convert.ToDateTime(DataBinder.Eval(e.Row.DataItem, "ValidUpto"));
                LinkButton btnForceCheckOut = (LinkButton)e.Row.FindControl("btnForceCheckOut");

                // If the pass has expired, change the button to yellow/warning
                if (DateTime.Now > validUpto)
                {
                    btnForceCheckOut.CssClass = "btn btn-warning btn-xs";
                    btnForceCheckOut.Text = "<i class='fa fa-exclamation-triangle'></i> Expired: Force Out";
                }
            }
        }

        protected void gvInside_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "ForceCheckOut")
            {
                int logId = Convert.ToInt32(e.CommandArgument);
                ProcessDatabaseCheckOut(logId);

                lblGridMsg.Text = "Visitor successfully checked out from the premises.";

                BindInsideVisitors(); // This will auto-refresh the grid via AJAX
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
                // REALIGNMENT 2: Added v.PassCategory to the SELECT query
                string query = @"
                SELECT 
                    v.LogID, v.VisitorName, v.VisitorCompany, v.VisitorMobile, v.VisitorEmail, 
                    v.ApprovalStatus, v.VisitStatus, v.ValidUpto, v.PhotoPath, v.ActualInTime, v.PassCategory,
                    u.EmployeeName AS HostName, u.Email AS HostEmail, u.BranchId AS VisitBranchId,
                    ISNULL(d.Department_Description, 'General') AS HostDepartment,
                    ISNULL(b.Branch_Description, 'Unknown Branch') AS VisitBranchName
                FROM VMS_VisitLog v
                LEFT JOIN MST_UserMaster u ON v.HostEmployeeCode = u.EmployeeCode
                LEFT JOIN MST_Department d ON CAST(u.DepartmentId AS VARCHAR) = d.Department_ID
                LEFT JOIN MST_Branch b ON u.BranchId = b.Branch_ID
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

                            // REALIGNMENT 1: Safe Null-handling for VisitStatus defaulting to 'Scheduled'
                            string visitStatus = sdr["VisitStatus"] != DBNull.Value && !string.IsNullOrWhiteSpace(sdr["VisitStatus"].ToString())
                                                 ? sdr["VisitStatus"].ToString()
                                                 : "Scheduled";

                            DateTime validUpto = Convert.ToDateTime(sdr["ValidUpto"]);
                            string photoPath = sdr["PhotoPath"].ToString();

                            int visitBranchId = sdr["VisitBranchId"] != DBNull.Value ? Convert.ToInt32(sdr["VisitBranchId"]) : 0;
                            string visitBranchName = sdr["VisitBranchName"].ToString();

                            hfCurrentLogId.Value = logId.ToString();
                            hfHostEmail.Value = sdr["HostEmail"].ToString();
                            hfHostName.Value = sdr["HostName"].ToString();
                            hfVisName.Value = sdr["VisitorName"].ToString();
                            hfCompany.Value = sdr["VisitorCompany"].ToString();

                            string visName = hfVisName.Value;

                            // Prepend the Category to the Company name for the Guard's visibility
                            string category = sdr["PassCategory"].ToString();
                            string company = string.IsNullOrEmpty(category) ? hfCompany.Value : $"[{category}] - {hfCompany.Value}";

                            string contact = !string.IsNullOrEmpty(sdr["VisitorMobile"].ToString()) ? sdr["VisitorMobile"].ToString() : sdr["VisitorEmail"].ToString();
                            string hostName = hfHostName.Value;
                            string hostDept = sdr["HostDepartment"].ToString();
                            string validUptoStr = validUpto.ToString("dd MMM, hh:mm tt");

                            // CROSS-PLANT RESTRICTION
                            if (isSpecificGate && scannerBranchId != visitBranchId)
                            {
                                ShowResult("result-card result-danger",
                                    "Wrong Facility Alert",
                                    $"This pass is strictly valid for <b>{visitBranchName}</b>. The visitor is at the wrong location.",
                                    photoPath, visName, company, contact, hostName, hostDept, validUptoStr);
                                return;
                            }

                            if (status != "Approved")
                            {
                                ShowResult("result-card result-danger", "Access Denied", $"Pass is currently marked as: {status}", photoPath, null);
                                return;
                            }

                            // EXPIRATION CHECK: Blocks Check-In, allows Check-Out
                            if (DateTime.Now > validUpto && visitStatus == "Scheduled")
                            {
                                ShowResult("result-card result-danger", "Pass Expired", $"This pass expired on {validUptoStr}. Cannot Check-In.", photoPath, visName, company, contact, hostName, hostDept, validUptoStr);
                                return;
                            }

                            if (visitStatus == "Scheduled")
                            {
                                btnManualCheckIn.Enabled = true;
                                btnManualCheckOut.Enabled = false;
                                liCheckInTime.Visible = false;
                                ShowResult("result-card result-info", "Visitor Identified", "Verify photo and click Confirm Check-IN.", photoPath, visName, company, contact, hostName, hostDept, validUptoStr);
                            }
                            else if (visitStatus == "Checked-In")
                            {
                                DateTime inTime = sdr["ActualInTime"] != DBNull.Value ? Convert.ToDateTime(sdr["ActualInTime"]) : DateTime.Now;

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

                                if (DateTime.Now > validUpto)
                                {
                                    ShowResult("result-card result-warning", "Visitor Overstayed", $"Pass expired on {validUptoStr}. Please verify and confirm Check-OUT.", photoPath, visName, company, contact, hostName, hostDept, validUptoStr);
                                }
                                else
                                {
                                    ShowResult("result-card result-info", "Visitor Exiting", "Review Check-In time and confirm Check-OUT.", photoPath, visName, company, contact, hostName, hostDept, validUptoStr);
                                }
                            }
                            else if (visitStatus == "Checked-Out")
                            {
                                ShowResult("result-card result-danger", "Already Checked-Out", $"{visName} has already left the premises.", photoPath, visName, company, contact, hostName, hostDept, validUptoStr);
                            }
                        }
                        else
                        {
                            ShowResult("result-card result-danger", "Pass Not Found", "This pass does not exist in the database.", "", null);
                        }
                    }
                }
            }
        }
        protected void btnProcessScan_Click_OLD(object sender, EventArgs e)
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
                    v.ApprovalStatus, v.VisitStatus, v.ValidUpto, v.PhotoPath, v.ActualInTime,
                    u.EmployeeName AS HostName, u.Email AS HostEmail, u.BranchId AS VisitBranchId,
                    ISNULL(d.Department_Description, 'General') AS HostDepartment,
                    ISNULL(b.Branch_Description, 'Unknown Branch') AS VisitBranchName
                FROM VMS_VisitLog v
                LEFT JOIN MST_UserMaster u ON v.HostEmployeeCode = u.EmployeeCode
                LEFT JOIN MST_Department d ON CAST(u.DepartmentId AS VARCHAR) = d.Department_ID
                LEFT JOIN MST_Branch b ON u.BranchId = b.Branch_ID
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

                            int visitBranchId = sdr["VisitBranchId"] != DBNull.Value ? Convert.ToInt32(sdr["VisitBranchId"]) : 0;
                            string visitBranchName = sdr["VisitBranchName"].ToString();

                            hfCurrentLogId.Value = logId.ToString();
                            hfHostEmail.Value = sdr["HostEmail"].ToString();
                            hfHostName.Value = sdr["HostName"].ToString();
                            hfVisName.Value = sdr["VisitorName"].ToString();
                            hfCompany.Value = sdr["VisitorCompany"].ToString();

                            string visName = hfVisName.Value;
                            string company = hfCompany.Value;
                            string contact = !string.IsNullOrEmpty(sdr["VisitorMobile"].ToString()) ? sdr["VisitorMobile"].ToString() : sdr["VisitorEmail"].ToString();
                            string hostName = hfHostName.Value;
                            string hostDept = sdr["HostDepartment"].ToString();
                            string validUptoStr = validUpto.ToString("dd MMM, hh:mm tt");

                            // CROSS-PLANT RESTRICTION
                            if (isSpecificGate && scannerBranchId != visitBranchId)
                            {
                                ShowResult("result-card result-danger",
                                    "Wrong Facility Alert",
                                    $"This pass is strictly valid for <b>{visitBranchName}</b>. The visitor is at the wrong location.",
                                    photoPath, visName, company, contact, hostName, hostDept, validUptoStr);
                                return;
                            }

                            if (status != "Approved")
                            {
                                ShowResult("result-card result-danger", "Access Denied", $"Pass is currently marked as: {status}", photoPath, null);
                                return;
                            }

                            // EXPIRATION CHECK: Blocks Check-In, allows Check-Out
                            if (DateTime.Now > validUpto && visitStatus == "Scheduled")
                            {
                                ShowResult("result-card result-danger", "Pass Expired", $"This pass expired on {validUptoStr}. Cannot Check-In.", photoPath, visName, company, contact, hostName, hostDept, validUptoStr);
                                return;
                            }

                            if (visitStatus == "Scheduled")
                            {
                                btnManualCheckIn.Enabled = true;
                                btnManualCheckOut.Enabled = false;
                                liCheckInTime.Visible = false;
                                ShowResult("result-card result-info", "Visitor Identified", "Verify photo and click Confirm Check-IN.", photoPath, visName, company, contact, hostName, hostDept, validUptoStr);
                            }
                            else if (visitStatus == "Checked-In")
                            {
                                DateTime inTime = sdr["ActualInTime"] != DBNull.Value ? Convert.ToDateTime(sdr["ActualInTime"]) : DateTime.Now;

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

                                if (DateTime.Now > validUpto)
                                {
                                    ShowResult("result-card result-warning", "Visitor Overstayed", $"Pass expired on {validUptoStr}. Please verify and confirm Check-OUT.", photoPath, visName, company, contact, hostName, hostDept, validUptoStr);
                                }
                                else
                                {
                                    ShowResult("result-card result-info", "Visitor Exiting", "Review Check-In time and confirm Check-OUT.", photoPath, visName, company, contact, hostName, hostDept, validUptoStr);
                                }
                            }
                            else if (visitStatus == "Checked-Out")
                            {
                                ShowResult("result-card result-danger", "Already Checked-Out", $"{visName} has already left the premises.", photoPath, visName, company, contact, hostName, hostDept, validUptoStr);
                            }
                        }
                        else
                        {
                            ShowResult("result-card result-danger", "Pass Not Found", "This pass does not exist in the database.", "", null);
                        }
                    }
                }
            }
        }

        protected void btnManualCheckIn_Click(object sender, EventArgs e)
        {
            int logId = Convert.ToInt32(hfCurrentLogId.Value);

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();
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
            ProcessDatabaseCheckOut(logId);

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
        private void ProcessDatabaseCheckOut(int logId)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                string visName = "", company = "", hostName = "", hostEmail = "";

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

                UpdateGateStatus(con, logId, "Checked-Out", "ActualOutTime");

                if (!string.IsNullOrEmpty(hostEmail))
                {
                    NotifyHostDeparture(con, logId, hostName, hostEmail, visName, company);
                }

                con.Close();
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