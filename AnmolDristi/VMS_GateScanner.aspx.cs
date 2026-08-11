using System;
using System.Data;
using System.Web.UI.WebControls;
using System.Web.UI;
using AnmolDristi.DAL;

namespace AnmolDristi
{
    public partial class VMS_GateScanner : System.Web.UI.Page
    {
        private readonly VmsGateScannerRepository _repository = new VmsGateScannerRepository();

        /// <summary>
        /// When gate config is valid: null = universal gate; non-null = branch-scoped gate.
        /// </summary>
        private int? _specificBranchId;

        /// <summary>
        /// True when BranchId query string is present but not a valid positive integer.
        /// </summary>
        private bool _gateConfigInvalid;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!TryResolveGateBranchContext(out _specificBranchId))
            {
                _gateConfigInvalid = true;
                DenyInvalidGateConfiguration();
                return;
            }

            if (!IsPostBack)
            {
                LoadGateLocation();
                BindInsideVisitors();
            }
        }

        /// <summary>
        /// Distinguishes absent BranchId (universal) from present-but-invalid (deny).
        /// Valid specific BranchIds are positive integers (aligned with other VMS pages using branchId &gt; 0).
        /// </summary>
        private bool TryResolveGateBranchContext(out int? specificBranchId)
        {
            specificBranchId = null;

            // Absent key only — empty string means the parameter is present.
            if (Request.QueryString["BranchId"] == null)
                return true;

            string raw = Request.QueryString["BranchId"];
            int branchId;
            if (!int.TryParse(raw, out branchId) || branchId <= 0)
                return false;

            specificBranchId = branchId;
            return true;
        }

        private void DenyInvalidGateConfiguration()
        {
            lblGateLocation.Text = "Invalid gate configuration.";
            ClearInsideVisitorsDisplay();
            ShowResult(
                "result-card result-danger",
                "Invalid Gate Configuration",
                "Invalid gate configuration.",
                "",
                null);
            hfScannedToken.Value = "";
        }

        private void ClearInsideVisitorsDisplay()
        {
            rptVisitorLogs.DataSource = null;
            rptVisitorLogs.DataBind();
            lblInsideCount.Text = "0";
            upGrid.Update();
            upBadge.Update();
        }

        private void KeepTabActive(string tabId)
        {
            // Dynamically force Bootstrap to keep the relevant tab open after postback
            ClientScript.RegisterStartupScript(this.GetType(), "FocusTab", $"$('#{tabId}').tab('show');", true);
        }

        private void LoadGateLocation()
        {
            if (_specificBranchId.HasValue)
            {
                string locationName = _repository.GetGateLocation(_specificBranchId.Value);
                lblGateLocation.Text = locationName != null ? locationName : "Unknown Facility";
            }
            else
            {
                lblGateLocation.Text = "Master Control / Universal Gate";
            }
        }

        private void BindInsideVisitors()
        {
            if (_gateConfigInvalid)
            {
                ClearInsideVisitorsDisplay();
                return;
            }

            DataTable dt = _repository.GetInsideVisitors(_specificBranchId);

            // Bind to the new responsive Repeater instead of the old GridView
            rptVisitorLogs.DataSource = dt;
            rptVisitorLogs.DataBind();

            lblInsideCount.Text = dt.Rows.Count.ToString();

            // TRIGGER AJAX PANELS TO UPDATE WITHOUT FLICKERING THE SCREEN
            upGrid.Update();
            upBadge.Update();
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
                if (_gateConfigInvalid)
                {
                    DenyInvalidGateConfiguration();
                    return;
                }

                string[] parts = e.CommandArgument.ToString().Split('|');
                int logId = Convert.ToInt32(parts[0]);
                int dayLogId = parts.Length > 1 ? Convert.ToInt32(parts[1]) : 0;
                GateMutationResult result = ProcessDatabaseCheckOut(logId, dayLogId);

                if (result.Succeeded)
                {
                    lblGridMsg.Text = "Visitor successfully checked out from the premises.";
                }
                else
                {
                    lblGridMsg.Text = "This visitor's status has changed. Please refresh the inside list and try again.";
                }

                BindInsideVisitors(); // This will auto-refresh the repeater via AJAX
                KeepTabActive("inside-tab");
            }
        }

        // --- SCANNER EVENTS ---

        protected void btnProcessScan_Click(object sender, EventArgs e)
        {
            if (_gateConfigInvalid)
            {
                DenyInvalidGateConfiguration();
                return;
            }

            string scannedToken = hfScannedToken.Value;
            Guid tokenGuid;

            if (!Guid.TryParse(scannedToken, out tokenGuid))
            {
                ShowResult("result-card result-danger", "Invalid QR Code format.", "This does not belong to the Anmol VMS system.", "", null);
                return;
            }

            bool isSpecificGate = _specificBranchId.HasValue;
            int scannerBranchId = _specificBranchId ?? 0;

            // Strict daily validation: visitor must have an approved day-log row for today
            // (covers both SingleDay and MultiDay; each visit creates VMS_VisitDayLog rows).
            DataTable scanDt = _repository.GetScanVisit(tokenGuid);

            int logId = 0;
            string status = "";
            string photoPath = "";
            int visitBranchId = 0;
            string visitBranchName = "";
            string visName = "";
            string company = "";
            string contact = "";
            string hostName = "";
            string hostDept = "";
            int dayLogId = 0;
            DateTime dayValidFrom = DateTime.Now;
            DateTime dayValidUpto = DateTime.Now;
            string dayStatus = "Scheduled";
            DateTime? dayActualInTime = null;
            bool passMatchedForToday = false;

            if (scanDt.Rows.Count > 0)
            {
                DataRow sdr = scanDt.Rows[0];
                passMatchedForToday = true;

                logId = Convert.ToInt32(sdr["LogID"]);
                status = sdr["ApprovalStatus"].ToString();
                photoPath = sdr["PhotoPath"].ToString();
                visitBranchId = sdr["TargetBranchID"] != DBNull.Value ? Convert.ToInt32(sdr["TargetBranchID"]) : 0;
                visitBranchName = sdr["VisitBranchName"].ToString();
                visName = sdr["VisitorName"].ToString();
                string category = sdr["PassCategory"].ToString();
                company = string.IsNullOrEmpty(category) ? sdr["VisitorCompany"].ToString() : $"[{category}] - {sdr["VisitorCompany"]}";
                contact = !string.IsNullOrEmpty(sdr["VisitorMobile"].ToString()) ? sdr["VisitorMobile"].ToString() : sdr["VisitorEmail"].ToString();
                hostName = sdr["HostName"].ToString();
                hostDept = sdr["HostDepartment"].ToString();

                dayLogId = Convert.ToInt32(sdr["DayLogID"]);
                dayValidFrom = Convert.ToDateTime(sdr["DayValidFrom"]);
                dayValidUpto = Convert.ToDateTime(sdr["DayValidUpto"]);
                dayStatus = sdr["DayVisitStatus"] != DBNull.Value && !string.IsNullOrWhiteSpace(sdr["DayVisitStatus"].ToString())
                    ? sdr["DayVisitStatus"].ToString()
                    : "Scheduled";
                if (sdr["DayActualInTime"] != DBNull.Value)
                    dayActualInTime = Convert.ToDateTime(sdr["DayActualInTime"]);

                hfCurrentLogId.Value = logId.ToString();
                hfHostEmail.Value = sdr["HostEmail"].ToString();
                hfHostName.Value = hostName;
                hfVisName.Value = visName;
                hfCompany.Value = sdr["VisitorCompany"].ToString();
            }

            if (!passMatchedForToday)
            {
                // Distinguish unknown QR vs valid pass that is not approved for today
                string deniedValidUpto = "";
                if (TryLoadPassByToken(tokenGuid,
                    out photoPath, out visName, out company, out contact, out hostName, out hostDept, out deniedValidUpto))
                {
                    ShowResult("result-card result-danger", "Access Denied",
                        "Access Denied: Valid Pass, but the host has NOT approved entry for today's date.",
                        photoPath, visName, company, contact, hostName, hostDept, deniedValidUpto);
                }
                else
                {
                    ShowResult("result-card result-danger", "Pass Not Found",
                        "This pass does not exist in the database.", "", null);
                }
                return;
            }

            if (isSpecificGate && scannerBranchId != visitBranchId)
            {
                ShowResult("result-card result-danger",
                    "Wrong Facility Alert",
                    $"This pass is strictly valid for <b>{visitBranchName}</b>. The visitor is at the wrong location.",
                    photoPath, visName, company, contact, hostName, hostDept, dayValidUpto.ToString("dd MMM, hh:mm tt"));
                return;
            }

            if (status != "Approved")
            {
                ShowResult("result-card result-danger", "Access Denied", $"Pass is currently marked as: {status}", photoPath, null);
                return;
            }

            hfCurrentDayLogId.Value = dayLogId.ToString();
            string validUptoDisplay = dayValidUpto.ToString("dd MMM, hh:mm tt");
            DateTime now = DateTime.Now;

            if (dayStatus == "Scheduled")
            {
                if (now < dayValidFrom)
                {
                    ShowResult("result-card result-warning", "Too Early",
                        $"Today's visiting window starts at {dayValidFrom:hh:mm tt}.",
                        photoPath, visName, company, contact, hostName, hostDept, validUptoDisplay);
                    return;
                }

                if (now > dayValidUpto)
                {
                    ShowResult("result-card result-danger", "Today's Window Ended",
                        $"Today's approved visiting hours ended at {dayValidUpto:hh:mm tt}.",
                        photoPath, visName, company, contact, hostName, hostDept, validUptoDisplay);
                    return;
                }

                btnManualCheckIn.Enabled = true;
                btnManualCheckOut.Enabled = false;
                liCheckInTime.Visible = false;
                ShowResult("result-card result-info", "Visitor Identified",
                    $"Approved for today ({dayValidFrom:hh:mm tt} - {dayValidUpto:hh:mm tt}). Verify photo and confirm Check-IN.",
                    photoPath, visName, company, contact, hostName, hostDept, validUptoDisplay);
            }
            else if (dayStatus == "Checked-In")
            {
                DateTime inTime = dayActualInTime ?? DateTime.Now;
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

                if (DateTime.Now > dayValidUpto)
                {
                    ShowResult("result-card result-warning", "Visitor Overstayed",
                        $"Today's window ended at {validUptoDisplay}. Please verify and confirm Check-OUT.",
                        photoPath, visName, company, contact, hostName, hostDept, validUptoDisplay);
                }
                else
                {
                    ShowResult("result-card result-info", "Visitor Exiting",
                        "Review today's check-in time and confirm Check-OUT.",
                        photoPath, visName, company, contact, hostName, hostDept, validUptoDisplay);
                }
            }
            else if (dayStatus == "Checked-Out")
            {
                ShowResult("result-card result-danger", "Already Checked-Out Today",
                    $"{visName} has already completed today's gate check-out.",
                    photoPath, visName, company, contact, hostName, hostDept, validUptoDisplay);
            }
        }

        /// <summary>
        /// Loads basic pass details by QR token (without the today/IsDayApproved filter)
        /// so the scanner can show a clear denial when the pass exists but is not approved for today.
        /// </summary>
        private bool TryLoadPassByToken(
            Guid tokenGuid,
            out string photoPath,
            out string visName,
            out string company,
            out string contact,
            out string hostName,
            out string hostDept,
            out string validUptoStr)
        {
            photoPath = "";
            visName = "";
            company = "";
            contact = "";
            hostName = "";
            hostDept = "";
            validUptoStr = "";

            DataTable dt = _repository.GetVisitByToken(tokenGuid);
            if (dt.Rows.Count == 0)
                return false;

            DataRow sdr = dt.Rows[0];
            photoPath = sdr["PhotoPath"].ToString();
            visName = sdr["VisitorName"].ToString();
            string category = sdr["PassCategory"].ToString();
            company = string.IsNullOrEmpty(category)
                ? sdr["VisitorCompany"].ToString()
                : $"[{category}] - {sdr["VisitorCompany"]}";
            contact = !string.IsNullOrEmpty(sdr["VisitorMobile"].ToString())
                ? sdr["VisitorMobile"].ToString()
                : sdr["VisitorEmail"].ToString();
            hostName = sdr["HostName"].ToString();
            hostDept = sdr["HostDepartment"].ToString();
            if (sdr["ValidUpto"] != DBNull.Value)
                validUptoStr = Convert.ToDateTime(sdr["ValidUpto"]).ToString("dd MMM, hh:mm tt");
            return true;
        }

        protected void btnManualCheckIn_Click(object sender, EventArgs e)
        {
            if (_gateConfigInvalid)
            {
                DenyInvalidGateConfiguration();
                return;
            }

            int logId = Convert.ToInt32(hfCurrentLogId.Value);
            int dayLogId = 0;
            int.TryParse(hfCurrentDayLogId.Value, out dayLogId);
            GateMutationResult result = ProcessDatabaseCheckIn(logId, dayLogId);

            if (!result.Succeeded)
            {
                ShowStateChangedFailure();
                return;
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
            if (_gateConfigInvalid)
            {
                DenyInvalidGateConfiguration();
                return;
            }

            int logId = Convert.ToInt32(hfCurrentLogId.Value);
            int dayLogId = 0;
            int.TryParse(hfCurrentDayLogId.Value, out dayLogId);
            GateMutationResult result = ProcessDatabaseCheckOut(logId, dayLogId);

            if (!result.Succeeded)
            {
                ShowStateChangedFailure();
                return;
            }

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

        // --- REUSABLE CHECK-IN DATABASE LOGIC ---
        private GateMutationResult ProcessDatabaseCheckIn(int logId, int dayLogId = 0)
        {
            string guardId = Session["EmployeeCode"]?.ToString() ?? "SecurityGate";
            return _repository.CheckIn(
                logId,
                dayLogId,
                guardId,
                hfHostName.Value,
                hfHostEmail.Value,
                hfVisName.Value,
                hfCompany.Value);
        }

        // --- REUSABLE CHECK-OUT DATABASE LOGIC ---
        private GateMutationResult ProcessDatabaseCheckOut(int logId, int dayLogId = 0)
        {
            string guardId = Session["EmployeeCode"]?.ToString() ?? "SecurityGate";
            return _repository.CheckOut(logId, dayLogId, guardId);
        }

        private void ShowStateChangedFailure()
        {
            ShowResult(
                "result-card result-danger",
                "Status Changed",
                "This visitor's status has changed. Please scan the pass again.",
                "",
                null);
            hfScannedToken.Value = "";
            BindInsideVisitors();
        }

        protected void btnResetScanner_Click(object sender, EventArgs e)
        {
            if (_gateConfigInvalid)
            {
                DenyInvalidGateConfiguration();
                return;
            }

            pnlCamera.Visible = true;
            pnlResult.Visible = false;
            hfScannedToken.Value = "";
            KeepTabActive("scanner-tab");
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
    }
}