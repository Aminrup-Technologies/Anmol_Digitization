using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Services;
using System.Web.Script.Services;
using ClosedXML.Excel;
using System.IO;

namespace AnmolDristi
{
    public partial class MyClaimsView_v2 : BasePage
    {
        private string connString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        // Accumulators for footer logic
        private decimal _totalSnapshot = 0m;
        private decimal _totalTAClaim = 0m;
        private decimal _totalDistance = 0m;
        private decimal _totalDA = 0m;
        private decimal _totalHotel = 0m;
        private decimal _totalTravel = 0m;
        private decimal _totalStationary = 0m;
        // NEW: Track the overall claim status during DataBinding
        private string _currentHeaderStatus = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["workman"] != null)
                {
                    PopulateYearDropdown();
                    if (!TryApplyQueryStringContext())
                        SetDefaultClaimMonth();
                    BindRequestsGrid();
                    TryOpenSubmittedRequest();
                }
            }
        }

        private bool TryApplyQueryStringContext()
        {
            int year, month;
            bool hasYear = int.TryParse(Request.QueryString["year"], out year) && year > 0;
            bool hasMonth = int.TryParse(Request.QueryString["month"], out month) && month >= 1 && month <= 12;
            if (!hasYear && !hasMonth)
                return false;

            if (hasYear)
            {
                if (ddlSearchYear.Items.FindByValue(year.ToString()) == null)
                    ddlSearchYear.Items.Add(new ListItem(year.ToString(), year.ToString()));
                ViewState["FilterYear"] = year;
            }
            else
                ViewState["FilterYear"] = DateTime.Now.AddMonths(-1).Year;

            if (hasMonth)
                ViewState["FilterMonth"] = month;
            else
                ViewState["FilterMonth"] = DateTime.Now.AddMonths(-1).Month;

            ddlSearchStatus.SelectedValue = "";
            SyncUIWithViewState();
            return true;
        }

        private void TryOpenSubmittedRequest()
        {
            if (!string.Equals(Request.QueryString["open"], "view", StringComparison.OrdinalIgnoreCase))
                return;

            int reqId;
            if (!int.TryParse(Request.QueryString["requestId"], out reqId) || reqId <= 0)
                return;

            ShowDetailsModal(reqId);

            string message = Request.QueryString["msg"];
            if (string.IsNullOrWhiteSpace(message))
            {
                string mode = Request.QueryString["mode"];
                int count;
                int.TryParse(Request.QueryString["count"], out count);
                if (string.Equals(mode, "RESUBMIT", StringComparison.OrdinalIgnoreCase))
                    message = count == 1
                        ? string.Format("1 returned activity has been resubmitted under Request #{0}.", reqId)
                        : string.Format("{0} returned activities have been resubmitted under Request #{1}.", count, reqId);
                else
                    message = string.Format("Monthly claim submitted successfully. Request #{0} has been created and sent for approval.", reqId);
            }

            string safe = HttpUtility.JavaScriptStringEncode(message);
            ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyInboundSubmit",
                "if (typeof PNotify !== 'undefined') { new PNotify({ title: 'Submitted', text: '" + safe + "', type: 'success', delay: 4000, styling: 'bootstrap3' }); }", true);
        }

        private void PopulateYearDropdown()
        {
            ddlSearchYear.Items.Clear();
            ddlSearchYear.Items.Add(new ListItem("-- Select Year --", "0"));
            int currentYear = DateTime.Now.Year;
            for (int i = currentYear - 2; i <= currentYear + 1; i++)
            {
                ddlSearchYear.Items.Add(new ListItem(i.ToString(), i.ToString()));
            }
        }

        private void SetDefaultClaimMonth()
        {
            // Operational claim month = previous calendar month (Sep → Aug; Jan → Dec prior year)
            DateTime activeClaimMonth = DateTime.Now.AddMonths(-1);
            ViewState["FilterYear"] = activeClaimMonth.Year;
            ViewState["FilterMonth"] = activeClaimMonth.Month;
            ddlSearchStatus.SelectedValue = "";
            SyncUIWithViewState();
        }

        private void SyncUIWithViewState()
        {
            int y = (int)ViewState["FilterYear"];
            int m = (int)ViewState["FilterMonth"];

            DateTime dt = new DateTime(y, m, 1);
            lblCurrentHeader.Text = dt.ToString("MMMM, yyyy");

            string yearValue = y.ToString();
            if (ddlSearchYear.Items.FindByValue(yearValue) == null)
                ddlSearchYear.Items.Add(new ListItem(yearValue, yearValue));
            ddlSearchYear.SelectedValue = yearValue;

            if (ddlSearchMonth.Items.FindByValue(m.ToString()) != null)
                ddlSearchMonth.SelectedValue = m.ToString();
        }

        protected void btnPrevMonth_Click(object sender, EventArgs e)
        {
            DateTime currentFilter = new DateTime((int)ViewState["FilterYear"], (int)ViewState["FilterMonth"], 1);
            DateTime prevMonth = currentFilter.AddMonths(-1);
            ViewState["FilterYear"] = prevMonth.Year;
            ViewState["FilterMonth"] = prevMonth.Month;
            SyncUIWithViewState();
            BindRequestsGrid(true);
        }

        protected void btnNextMonth_Click(object sender, EventArgs e)
        {
            DateTime currentFilter = new DateTime((int)ViewState["FilterYear"], (int)ViewState["FilterMonth"], 1);
            DateTime nextMonth = currentFilter.AddMonths(1);
            ViewState["FilterYear"] = nextMonth.Year;
            ViewState["FilterMonth"] = nextMonth.Month;
            SyncUIWithViewState();
            BindRequestsGrid(true);
        }

        protected void btnCurrentMonth_Click(object sender, EventArgs e)
        {
            // "Current Month" = active operational claim month (previous calendar month)
            DateTime activeClaimMonth = DateTime.Now.AddMonths(-1);
            ViewState["FilterYear"] = activeClaimMonth.Year;
            ViewState["FilterMonth"] = activeClaimMonth.Month;
            ddlSearchStatus.SelectedValue = "";
            SyncUIWithViewState();
            BindRequestsGrid(true);
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            if (ddlSearchYear.SelectedValue != "0")
                ViewState["FilterYear"] = Convert.ToInt32(ddlSearchYear.SelectedValue);

            if (ddlSearchMonth.SelectedValue != "0")
                ViewState["FilterMonth"] = Convert.ToInt32(ddlSearchMonth.SelectedValue);

            lblCurrentHeader.Text = "Custom Search Results";
            BindRequestsGrid(true);
        }

        protected void btnReset_Click(object sender, EventArgs e)
        {
            SetDefaultClaimMonth();
            BindRequestsGrid(true);
        }

        protected void gvRequests_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvRequests.PageIndex = e.NewPageIndex;
            BindRequestsGrid();
        }

        protected void BindRequestsGrid(bool resetPage = false)
        {
            if (resetPage)
                gvRequests.PageIndex = 0;

            string employeeCode = Session["workman"]?.ToString() ?? "Guest";
            DataTable dt = new DataTable();

            using (var con = new SqlConnection(connString))
            {
                using (var cmd = new SqlCommand("dbo.usp_GetMyClaims", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserLoginId", employeeCode);

                    // Pass dropdown selection directly — do not override with DateTime.Now / calendar month
                    int filterYear;
                    if (int.TryParse(ddlSearchYear.SelectedValue, out filterYear) && filterYear > 0)
                        cmd.Parameters.AddWithValue("@FilterYear", filterYear);
                    else
                        cmd.Parameters.AddWithValue("@FilterYear", DBNull.Value);

                    int filterMonth;
                    if (int.TryParse(ddlSearchMonth.SelectedValue, out filterMonth) && filterMonth > 0)
                        cmd.Parameters.AddWithValue("@FilterMonth", filterMonth);
                    else
                        cmd.Parameters.AddWithValue("@FilterMonth", DBNull.Value);

                    if (!string.IsNullOrWhiteSpace(ddlSearchStatus.SelectedValue))
                        cmd.Parameters.AddWithValue("@FilterStatus", ddlSearchStatus.SelectedValue);
                    else
                        cmd.Parameters.AddWithValue("@FilterStatus", DBNull.Value);

                    using (var da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
            if (gvRequests.PageSize > 0)
            {
                int pageCount = (int)Math.Ceiling(dt.Rows.Count / (double)gvRequests.PageSize);
                if (gvRequests.PageIndex < 0 || (pageCount > 0 && gvRequests.PageIndex >= pageCount) || pageCount == 0)
                    gvRequests.PageIndex = 0;
            }

            gvRequests.DataSource = dt;
            gvRequests.DataBind();
        }

        protected void gvRequests_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "View")
            {
                int reqId = Convert.ToInt32(e.CommandArgument);
                ShowDetailsModal(reqId);
            }
            else if (e.CommandName == "ExportExcel")
            {
                int reqId = Convert.ToInt32(e.CommandArgument);
                ExportClaimToExcel(reqId);
            }
        }

        protected void gvRequests_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DataRowView drv = e.Row.DataItem as DataRowView;
                if (drv != null)
                {
                    string status = drv["RequestStatus"].ToString();
                    Label lblStatus = e.Row.FindControl("lblStatus") as Label;

                    Label lblPendingWith = e.Row.FindControl("lblPendingWith") as Label;
                    bool isReturned = ClaimStatuses.Equals(status, ClaimStatuses.Returned);

                    if (isReturned && lblPendingWith != null)
                    {
                        lblPendingWith.Text = ClaimStatuses.Returned;
                        lblPendingWith.CssClass = "badge badge-warning text-dark px-2 py-1";
                    }

                    // Pending/Returned header approved totals are not final until SQL recalculates them.
                    Label lblApprovedAmount = e.Row.FindControl("lblApprovedAmount") as Label;
                    if (lblApprovedAmount != null && IsApprovedAmountHiddenForStatus(status))
                    {
                        lblApprovedAmount.Text = isReturned ? "Corrections Required" : "Under Review";
                        lblApprovedAmount.CssClass = "small font-weight-bold text-muted";
                    }

                    if (lblStatus != null)
                    {
                        if (ClaimStatuses.Equals(status, ClaimStatuses.Rejected))
                        {
                            lblStatus.CssClass = "badge badge-danger px-2 py-1";
                            e.Row.Style["background-color"] = "#fff5f5";
                            e.Row.CssClass += " text-muted";
                        }
                        else if (isReturned)
                        {
                            lblStatus.CssClass = "badge badge-warning text-dark px-2 py-1";
                        }
                        else if (ClaimStatuses.Equals(status, ClaimStatuses.Approved) ||
                                 ClaimStatuses.Equals(status, ClaimStatuses.Completed) ||
                                 ClaimStatuses.Equals(status, ClaimStatuses.ReadyForPayment))
                        {
                            lblStatus.CssClass = "badge badge-success px-2 py-1";
                        }
                        else if (ClaimStatuses.Equals(status, ClaimStatuses.Pending))
                        {
                            lblStatus.CssClass = "badge badge-warning text-dark px-2 py-1";
                        }
                        else
                        {
                            lblStatus.CssClass = "badge badge-info px-2 py-1";
                        }
                    }
                }
            }
        }

        // ==========================================
        // EXPORT LOGIC (CLOSEDXML)
        // ==========================================
        private void ExportClaimToExcel(int reqId)
        {
            if (!UserOwnsRequest(reqId)) RejectUnauthorized();
            DataTable dtExport = new DataTable();
            using (var con = new SqlConnection(connString))
            {
                using (var cmd = new SqlCommand("dbo.usp_Export_Auditor_Final_Excel", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ApprovalRequestId", reqId);
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dtExport);
                    }
                }
            }

            if (dtExport.Rows.Count == 0)
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "exportEmpty",
                    "if (typeof PNotify !== 'undefined') { new PNotify({ title: 'Export', text: 'No exportable data found for this claim.', type: 'info', styling: 'bootstrap3', delay: 4000 }); }"
                    + " else if (window.jQuery) { window.jQuery('.x_content').first().prepend('<div class=\"alert alert-info\">No exportable data found for this claim.</div>'); }", true);
                return;
            }

            string empCode = dtExport.Rows[0]["Employee Code"].ToString();
            string fileName = $"Claim_{reqId}_{empCode}.xlsx";

            DataRow totalRow = dtExport.NewRow();
            totalRow["Claim No"] = DBNull.Value;
            totalRow["Employee Code"] = DBNull.Value;
            totalRow["Employee Name"] = DBNull.Value;
            totalRow["Date"] = DBNull.Value;
            totalRow["Day"] = "TOTAL:";
            totalRow["From Place"] = DBNull.Value;
            totalRow["To Place"] = DBNull.Value;

            totalRow["KM Claimed"] = dtExport.Compute("SUM([KM Claimed])", string.Empty);
            totalRow["TA Bill"] = dtExport.Compute("SUM([TA Bill])", string.Empty);
            totalRow["DA"] = dtExport.Compute("SUM([DA])", string.Empty);
            totalRow["Hotel"] = dtExport.Compute("SUM([Hotel])", string.Empty);
            totalRow["Travel / OPA"] = dtExport.Compute("SUM([Travel / OPA])", string.Empty);
            totalRow["Stationary"] = dtExport.Compute("SUM([Stationary])", string.Empty);
            totalRow["Total Expenses (DA+Hotel+Trv+Stat)"] = dtExport.Compute("SUM([Total Expenses (DA+Hotel+Trv+Stat)])", string.Empty);
            totalRow["Final Approved Amount"] = dtExport.Compute("SUM([Final Approved Amount])", string.Empty);

            dtExport.Rows.Add(totalRow);

            using (XLWorkbook wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add(dtExport, "My Claim");
                var lastRow = ws.LastRowUsed();
                lastRow.Style.Font.Bold = true;
                lastRow.Style.Fill.BackgroundColor = XLColor.LightGray;
                ws.Columns().AdjustToContents();

                if (Response.HeadersWritten)
                {
                    throw new InvalidOperationException(
                        "Excel export cannot start because HTTP headers have already been sent.");
                }

                Response.Clear();
                Response.Buffer = true;
                Response.Charset = "";
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("Content-Disposition", "attachment;filename=" + fileName);
                wb.SaveAs(Response.OutputStream);
                Response.Flush();
                Response.SuppressContent = true;
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
        }

        // ==========================================
        // DETAILS MODAL LOGIC
        // ==========================================
        protected void ShowDetailsModal(int reqId)
        {
            if (!UserOwnsRequest(reqId)) RejectUnauthorized();
            lblModalRequestId.Text = reqId.ToString();
            BindDetailsGrid(reqId);
            ScriptManager.RegisterStartupScript(this, GetType(), "openModal", "openDetailsModal();", true);
        }

        protected void BindDetailsGrid(int reqId)
        {
            _totalSnapshot = 0m;
            _totalTAClaim = 0m;
            _totalDistance = 0m;
            _totalDA = 0m;
            _totalHotel = 0m;
            _totalTravel = 0m;
            _totalStationary = 0m;
            _currentHeaderStatus = ""; // Reset for safety

            DataTable dt = new DataTable();
            string requesterName = "";

            using (var con = new SqlConnection(connString))
            {
                using (var cmd = new SqlCommand("dbo.usp_GetApprovalRequestDetails", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ApprovalRequestId", reqId);
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }

                dt = SalesApprovalDetailVersionHelper.KeepLatestPerActivity(dt);

                // 🚩 NEW: Fetch Header Status alongside Employee Name
                string sql = "SELECT (SELECT EmployeeName FROM MST_UserMaster WHERE EmployeeCode = r.RequesterLoginId) as EmpName, Status FROM SalesApprovalRequest r WHERE ApprovalRequestId = @Id";
                using (var cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Id", reqId);
                    con.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            requesterName = rdr["EmpName"]?.ToString() ?? "";
                            _currentHeaderStatus = rdr["Status"]?.ToString() ?? "";
                        }
                    }
                }
            }

            lblRequesterName.Text = requesterName;

            // ==========================================
            // BIND APPROVAL PROGRESS TRACKER
            // ==========================================
            string sqlSteps = @"
                SELECT 
                    0 AS ApproverLevel,
                    'REQUESTER' AS ApproverRole,
                    ISNULL(um.EmployeeName, sar.RequesterLoginId) AS ApproverName,
                    '" + ClaimStatuses.Submitted + @"' AS Status,
                    sar.RequestDate AS ActionDate,
                    sar.RequestDate
                FROM SalesApprovalRequest sar
                LEFT JOIN MST_UserMaster um ON um.EmployeeCode = sar.RequesterLoginId
                WHERE sar.ApprovalRequestId = @Id

                UNION ALL

                SELECT 
                    sara.ApproverLevel,
                    sara.ApproverRole,
                    ISNULL(um.EmployeeName, sara.ApproverLoginId) AS ApproverName,
                    sara.Status,
                    sara.ActionDate,
                    sar.RequestDate
                FROM SalesApprovalRequestApprover sara
                INNER JOIN SalesApprovalRequest sar ON sar.ApprovalRequestId = sara.ApprovalRequestId
                LEFT JOIN MST_UserMaster um ON um.EmployeeCode = sara.ApproverLoginId
                WHERE sara.ApprovalRequestId = @Id
                ORDER BY ApproverLevel ASC";

            DataTable dtSteps = new DataTable();
            using (var conSteps = new SqlConnection(connString))
            {
                using (var cmd = new SqlCommand(sqlSteps, conSteps))
                {
                    cmd.Parameters.AddWithValue("@Id", reqId);
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dtSteps);
                    }
                }
            }

            dtSteps.Columns.Add("BadgeColor", typeof(string));
            dtSteps.Columns.Add("LineColor", typeof(string));
            dtSteps.Columns.Add("Icon", typeof(string));
            dtSteps.Columns.Add("BootstrapColor", typeof(string));
            dtSteps.Columns.Add("ActionDateStr", typeof(string));
            dtSteps.Columns.Add("DaysText", typeof(string));
            dtSteps.Columns.Add("DaysCssClass", typeof(string));

            DateTime previousDate = dtSteps.Rows.Count > 0 ? Convert.ToDateTime(dtSteps.Rows[0]["RequestDate"]) : DateTime.Now;

            for (int i = 0; i < dtSteps.Rows.Count; i++)
            {
                DataRow row = dtSteps.Rows[i];
                string status = row["Status"].ToString();
                DateTime? actionDate = row["ActionDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["ActionDate"]) : null;
                DateTime stepStartDate = previousDate;

                // 1. Set Colors & Icons based on Status
                if (status == ClaimStatuses.Submitted)
                {
                    row["BadgeColor"] = "#007bff";
                    row["LineColor"] = "#28a745";
                    row["Icon"] = "fa fa-paper-plane";
                    row["BootstrapColor"] = "primary";
                }
                else if (status == ClaimStatuses.Approved || status == ClaimStatuses.Completed || status == ClaimStatuses.ReadyForPayment || status == ClaimStatuses.Processed)
                {
                    row["BadgeColor"] = "#28a745";
                    row["LineColor"] = "#28a745";
                    row["Icon"] = "fa fa-check";
                    row["BootstrapColor"] = "success";
                }
                else if (status == ClaimStatuses.Rejected)
                {
                    row["BadgeColor"] = "#dc3545";
                    row["LineColor"] = "#dee2e6";
                    row["Icon"] = "fa fa-times";
                    row["BootstrapColor"] = "danger";
                }
                else if (status == "Cancelled") // 🚩 NEW: Handle Cancelled
                {
                    row["BadgeColor"] = "#6c757d";
                    row["LineColor"] = "#dee2e6";
                    row["Icon"] = "fa fa-ban";
                    row["BootstrapColor"] = "secondary";
                }
                else // Pending
                {
                    row["BadgeColor"] = "#ffc107";
                    row["LineColor"] = "#dee2e6";
                    row["Icon"] = "fa fa-hourglass-half";
                    row["BootstrapColor"] = "warning text-dark";
                }

                // 2. Calculate Days
                if (actionDate.HasValue)
                {
                    row["ActionDateStr"] = actionDate.Value.ToString("dd-MMM yy, hh:mm tt");

                    if (status == ClaimStatuses.Submitted)
                    {
                        row["DaysText"] = "Initial Submission";
                        row["DaysCssClass"] = "text-primary";
                    }
                    else
                    {
                        int days = (int)(actionDate.Value - stepStartDate).TotalDays;
                        row["DaysText"] = days == 0 ? "Actioned same day" : $"Took {days} day(s)";
                        row["DaysCssClass"] = "text-success";
                    }
                    previousDate = actionDate.Value;
                }
                else
                {
                    if (status == "Cancelled") // 🚩 NEW: Cancelled has no pending days
                    {
                        row["ActionDateStr"] = "-";
                        row["DaysText"] = "Cancelled";
                        row["DaysCssClass"] = "text-muted";
                    }
                    else
                    {
                        row["ActionDateStr"] = "Awaiting Action";
                        int daysPending = (int)(DateTime.Now - stepStartDate).TotalDays;
                        row["DaysText"] = $"Pending for {daysPending} day(s)";
                        row["DaysCssClass"] = daysPending > 3 ? "text-danger" : "text-warning text-dark";
                    }
                }
            }

            rptApprovalSteps.DataSource = dtSteps;
            rptApprovalSteps.DataBind();

            // ==========================================
            // BIND GRID & FOOTERS
            // ==========================================
            gvDetails.DataSource = dt;
            gvDetails.DataBind();

            if (gvDetails.Rows.Count > 0)
            {
                gvDetails.HeaderRow.TableSection = TableRowSection.TableHeader;
                if (gvDetails.FooterRow != null)
                {
                    gvDetails.FooterRow.TableSection = TableRowSection.TableFooter;
                }
            }

            // ==========================================
            // 🚩 NEW: REVISED DASHBOARD MATH
            // ==========================================
            decimal originalClaimedKM = 0m;
            decimal originalClaimedAmount = 0m;
            decimal currentApprovedKM = 0m;
            decimal currentApprovedAmount = 0m;
            decimal totalDeductedKM = 0m;
            decimal totalDeductedAmount = 0m;

            string sqlDashboard = @"
                -- 1. Original Totals (latest version per day)
                SELECT 
                    ISNULL(SUM(d.Snapshot_TotalDistanceKM), 0) AS OriginalKM,
                    ISNULL(SUM(ISNULL(d.Snapshot_CalculatedClaimAmount, 0) + ISNULL(d.Snapshot_ExpenseTotal, 0)), 0) AS OriginalAmount
                FROM dbo.SalesApprovalRequestDetail d
                WHERE d.ApprovalRequestId = @Id
                  AND " + SalesApprovalDetailVersionHelper.LatestDetailWhereForD.Trim() + @";

                -- 2. Approved Totals (latest version per day)
                SELECT 
                    ISNULL(SUM(d.Snapshot_TotalDistanceKM), 0) AS ApprovedKM,
                    ISNULL(SUM(ISNULL(d.Snapshot_CalculatedClaimAmount, 0) + ISNULL(d.Snapshot_ExpenseTotal, 0)), 0) AS ApprovedAmount
                FROM dbo.SalesApprovalRequestDetail d
                WHERE d.ApprovalRequestId = @Id
                  AND ISNULL(d.DetailStatus, '') <> '" + ClaimStatuses.Rejected + @"'
                  AND " + SalesApprovalDetailVersionHelper.LatestDetailWhereForD.Trim() + @";

                -- 3. Deduction Totals (latest version per day)
                SELECT 
                    ISNULL(SUM(DeductedAmount), 0) AS DeductedAmount,
                    ISNULL(SUM(DeductedKM), 0) AS DeductedKM
                FROM (
                    SELECT CAST((ISNULL(d.Snapshot_CalculatedClaimAmount, 0) + ISNULL(d.Snapshot_ExpenseTotal, 0)) AS DECIMAL(18,2)) AS DeductedAmount,
                           ISNULL(d.Snapshot_TotalDistanceKM, 0) AS DeductedKM
                    FROM dbo.SalesApprovalRequestDetail d
                    WHERE d.ApprovalRequestId = @Id
                      AND d.DetailStatus = '" + ClaimStatuses.Rejected + @"'
                      AND " + SalesApprovalDetailVersionHelper.LatestDetailWhereForD.Trim() + @"
                    UNION ALL
                    SELECT CAST(e.Amount AS DECIMAL(18,2)) AS DeductedAmount, 0 AS DeductedKM
                    FROM dbo.SalesDailyExpense e
                    INNER JOIN dbo.SalesApprovalRequestDetail d ON e.ActivityId = d.ActivityId
                    " + SalesApprovalDetailVersionHelper.LatestDetailJoinForD + @"
                    WHERE d.ApprovalRequestId = @Id AND e.ClaimStatus = '" + ClaimStatuses.Rejected + @"' AND e.IsDeleted = 0 AND ISNULL(d.DetailStatus, '') <> '" + ClaimStatuses.Rejected + @"'
                    UNION ALL
                    SELECT
                        CAST(d.SubmittedClaimAmount - ISNULL(d.Snapshot_CalculatedClaimAmount, 0) AS DECIMAL(18,2)) AS DeductedAmount,
                        CAST(CASE
                            WHEN d.SubmittedDistanceKM IS NULL THEN 0
                            WHEN d.SubmittedDistanceKM > ISNULL(d.Snapshot_TotalDistanceKM, 0)
                                THEN d.SubmittedDistanceKM - ISNULL(d.Snapshot_TotalDistanceKM, 0)
                            ELSE 0
                        END AS DECIMAL(18,2)) AS DeductedKM
                    FROM dbo.SalesApprovalRequestDetail d
                    WHERE d.ApprovalRequestId = @Id
                      AND d.SubmittedClaimAmount IS NOT NULL
                      AND d.SubmittedClaimAmount > ISNULL(d.Snapshot_CalculatedClaimAmount, 0)
                      AND ISNULL(d.DetailStatus, '') <> '" + ClaimStatuses.Rejected + @"'
                      AND " + SalesApprovalDetailVersionHelper.LatestDetailWhereForD.Trim() + @"
                ) x;";

            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand(sqlDashboard, con))
            {
                cmd.Parameters.AddWithValue("@Id", reqId);
                if (con.State != ConnectionState.Open) con.Open();

                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        originalClaimedKM = Convert.ToDecimal(rdr["OriginalKM"]);
                        originalClaimedAmount = Convert.ToDecimal(rdr["OriginalAmount"]);
                    }

                    if (rdr.NextResult() && rdr.Read())
                    {
                        currentApprovedKM = Convert.ToDecimal(rdr["ApprovedKM"]);
                        currentApprovedAmount = Convert.ToDecimal(rdr["ApprovedAmount"]);
                    }

                    if (rdr.NextResult() && rdr.Read())
                    {
                        totalDeductedAmount = Convert.ToDecimal(rdr["DeductedAmount"]);
                        totalDeductedKM = Convert.ToDecimal(rdr["DeductedKM"]);
                    }
                }
            }

            // 🚩 NEW: Force Math Override if Header is Rejected
            if (ClaimStatuses.Equals(_currentHeaderStatus, ClaimStatuses.Rejected))
            {
                currentApprovedAmount = 0m;
                currentApprovedKM = 0m;
                totalDeductedAmount = originalClaimedAmount;
                totalDeductedKM = originalClaimedKM;
            }

            lblSummaryOriginal.Text = originalClaimedAmount.ToString("N2");
            lblSummaryApproved.Text = currentApprovedAmount.ToString("N2");
            lblSummaryDeduction.Text = totalDeductedAmount.ToString("N2");
            lblSummaryOriginalKM.Text = originalClaimedKM.ToString("N2");
            lblSummaryApprovedKM.Text = currentApprovedKM.ToString("N2");
            lblSummaryDeductedKM.Text = totalDeductedKM.ToString("N2");

            bool hideApproved = IsApprovedAmountHiddenForStatus(_currentHeaderStatus);
            divSummaryDeduction.Visible = !hideApproved;
            divSummaryApproved.Visible = !hideApproved;
        }

        private static bool IsApprovedAmountHiddenForStatus(string headerStatus)
        {
            return ClaimStatuses.Equals(headerStatus, ClaimStatuses.Pending)
                || ClaimStatuses.Equals(headerStatus, ClaimStatuses.Returned);
        }

        protected void gvDetails_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DataRowView drv = e.Row.DataItem as DataRowView;
                if (drv == null) return;

                _totalDistance += (drv["Snapshot_TotalDistanceKM"] == DBNull.Value) ? 0m : Convert.ToDecimal(drv["Snapshot_TotalDistanceKM"]);
                _totalTAClaim += (drv["Snapshot_CalculatedClaimAmount"] == DBNull.Value) ? 0m : Convert.ToDecimal(drv["Snapshot_CalculatedClaimAmount"]);
                _totalDA += (drv["Snapshot_DAAmount"] == DBNull.Value) ? 0m : Convert.ToDecimal(drv["Snapshot_DAAmount"]);
                _totalHotel += (drv["Snapshot_HotelAmount"] == DBNull.Value) ? 0m : Convert.ToDecimal(drv["Snapshot_HotelAmount"]);
                _totalTravel += (drv["Snapshot_TravelAmount"] == DBNull.Value) ? 0m : Convert.ToDecimal(drv["Snapshot_TravelAmount"]);
                _totalStationary += (drv["Snapshot_StationaryAmount"] == DBNull.Value) ? 0m : Convert.ToDecimal(drv["Snapshot_StationaryAmount"]);
                _totalSnapshot += (drv["Snapshot_ExpenseTotal"] == DBNull.Value) ? 0m : Convert.ToDecimal(drv["Snapshot_ExpenseTotal"]);

                string detailStatus = drv["DetailStatus"].ToString();
                Label lblDetailStatus = e.Row.FindControl("lblDetailStatus") as Label;

                // 🚩 NEW: Force Line Items to reflect overall Rejection
                if (ClaimStatuses.Equals(_currentHeaderStatus, ClaimStatuses.Rejected))
                {
                    detailStatus = ClaimStatuses.Rejected;
                    if (lblDetailStatus != null) lblDetailStatus.Text = ClaimStatuses.Rejected;
                }

                if (ClaimStatuses.Equals(detailStatus, ClaimStatuses.Rejected))
                {
                    e.Row.CssClass += " table-danger text-muted";
                    if (lblDetailStatus != null) lblDetailStatus.CssClass = "badge badge-danger px-2 py-1";
                }
                else if (ClaimStatuses.Equals(detailStatus, ClaimStatuses.Approved))
                {
                    e.Row.CssClass += " table-success";
                    if (lblDetailStatus != null) lblDetailStatus.CssClass = "badge badge-success px-2 py-1";
                }
                else if (ClaimStatuses.Equals(detailStatus, ClaimStatuses.Pending) || string.IsNullOrEmpty(detailStatus))
                {
                    if (lblDetailStatus != null)
                    {
                        lblDetailStatus.Text = string.IsNullOrEmpty(detailStatus) ? ClaimStatuses.Pending : detailStatus;
                        lblDetailStatus.CssClass = "badge badge-warning text-dark px-2 py-1";
                    }
                }
                else
                {
                    if (lblDetailStatus != null) lblDetailStatus.CssClass = "badge badge-info px-2 py-1";
                }
            }
            else if (e.Row.RowType == DataControlRowType.Footer)
            {
                FillFooterTotals(e.Row);
            }
        }

        private void FillFooterTotals(GridViewRow footer)
        {
            for (int i = 0; i < gvDetails.Columns.Count; i++)
            {
                string headerText = gvDetails.Columns[i].HeaderText ?? "";

                if (headerText.Contains("Distance"))
                {
                    footer.Cells[i].Text = "<strong>" + _totalDistance.ToString("N2") + "</strong>";
                }
                else if (headerText.Contains("TA"))
                {
                    footer.Cells[i].Text = "<strong>" + _totalTAClaim.ToString("N2") + "</strong>";
                    footer.Cells[i].HorizontalAlign = HorizontalAlign.Right;
                }
                else if (headerText.Contains("DA"))
                {
                    footer.Cells[i].Text = "<strong>" + _totalDA.ToString("N2") + "</strong>";
                    footer.Cells[i].HorizontalAlign = HorizontalAlign.Right;
                }
                else if (headerText.Contains("Hotel"))
                {
                    footer.Cells[i].Text = "<strong>" + _totalHotel.ToString("N2") + "</strong>";
                    footer.Cells[i].HorizontalAlign = HorizontalAlign.Right;
                }
                else if (headerText.Contains("Travel"))
                {
                    footer.Cells[i].Text = "<strong>" + _totalTravel.ToString("N2") + "</strong>";
                    footer.Cells[i].HorizontalAlign = HorizontalAlign.Right;
                }
                else if (headerText.Contains("Stat"))
                {
                    footer.Cells[i].Text = "<strong>" + _totalStationary.ToString("N2") + "</strong>";
                    footer.Cells[i].HorizontalAlign = HorizontalAlign.Right;
                }
                else if (headerText.Contains("Total"))
                {
                    footer.Cells[i].Text = "<strong>" + _totalSnapshot.ToString("N2") + "</strong>";
                    footer.Cells[i].HorizontalAlign = HorizontalAlign.Right;
                }
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object GetDeductionBreakdown(int approvalRequestId)
        {
            if (!UserOwnsRequest(approvalRequestId)) RejectUnauthorized();
            try
            {
                string connString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                var results = new List<object>();

                // 🚩 NEW: Adjusted to handle overall Header Rejection
                string sql = @"
                    DECLARE @HeaderStatus NVARCHAR(50);
                    SELECT @HeaderStatus = Status FROM dbo.SalesApprovalRequest WHERE ApprovalRequestId = @ReqId;

                    IF @HeaderStatus = '" + ClaimStatuses.Rejected + @"'
                    BEGIN
                        SELECT 
                            CONVERT(varchar, a.RequestDate, 106) AS ActivityDate,
                            'Claim Fully Rejected' AS DeductionType,
                            ISNULL(a.RequestRemarks, 'Rejected by Approver') AS Reason,
                            CAST((ISNULL(a.TotalClaimAmount, 0) + ISNULL(a.TotalExpenseAmount, 0)) AS DECIMAL(18,2)) AS DeductedAmount
                        FROM dbo.SalesApprovalRequest a
                        WHERE a.ApprovalRequestId = @ReqId;
                    END
                    ELSE
                    BEGIN
                        SELECT 
                            CONVERT(varchar, a.ActivityDate, 106) AS ActivityDate,
                            'Whole Day Rejected' AS DeductionType,
                            ISNULL(d.DetailRemarks, 'Rejected by Auditor') AS Reason,
                            CAST((ISNULL(d.Snapshot_CalculatedClaimAmount, 0) + ISNULL(d.Snapshot_ExpenseTotal, 0)) AS DECIMAL(18,2)) AS DeductedAmount
                        FROM dbo.SalesApprovalRequestDetail d
                        INNER JOIN dbo.SalesDailyActivity a ON d.ActivityId = a.ActivityId
                        WHERE d.ApprovalRequestId = @ReqId AND d.DetailStatus = '" + ClaimStatuses.Rejected + @"'
                          AND d.ApprovalRequestDetailId IN (
                              SELECT MAX(ApprovalRequestDetailId)
                              FROM dbo.SalesApprovalRequestDetail
                              WHERE ApprovalRequestId = @ReqId
                              GROUP BY ActivityId
                          )

                        UNION ALL

                        SELECT 
                            CONVERT(varchar, a.ActivityDate, 106) AS ActivityDate,
                            'Rejected Receipt: ' + e.ExpenseType AS DeductionType,
                            'Receipt marked as Rejected' AS Reason,
                            CAST(e.Amount AS DECIMAL(18,2)) AS DeductedAmount
                        FROM dbo.SalesDailyExpense e
                        INNER JOIN dbo.SalesApprovalRequestDetail d ON e.ActivityId = d.ActivityId
                        INNER JOIN (
                            SELECT ActivityId, MAX(ApprovalRequestDetailId) AS LatestDetailId
                            FROM dbo.SalesApprovalRequestDetail
                            WHERE ApprovalRequestId = @ReqId
                            GROUP BY ActivityId
                        ) _latestDetail ON d.ApprovalRequestDetailId = _latestDetail.LatestDetailId
                        INNER JOIN dbo.SalesDailyActivity a ON d.ActivityId = a.ActivityId
                        WHERE d.ApprovalRequestId = @ReqId 
                          AND e.ClaimStatus = '" + ClaimStatuses.Rejected + @"' 
                          AND e.IsDeleted = 0
                          AND ISNULL(d.DetailStatus, '') <> '" + ClaimStatuses.Rejected + @"'

                        UNION ALL

                        SELECT
                            CONVERT(varchar, a.ActivityDate, 106) AS ActivityDate,
                            'Submitted travel ₹' + CONVERT(varchar(40), d.SubmittedClaimAmount)
                                + ' to ₹' + CONVERT(varchar(40), ISNULL(d.Snapshot_CalculatedClaimAmount, 0))
                                + CASE
                                    WHEN d.SubmittedDistanceKM IS NULL THEN ''
                                    ELSE ' (' + CONVERT(varchar(40), d.SubmittedDistanceKM) + ' km to '
                                         + CONVERT(varchar(40), ISNULL(d.Snapshot_TotalDistanceKM, 0)) + ' km)'
                                  END AS DeductionType,
                            'Current travel snapshot is below the submitted travel amount' AS Reason,
                            CAST(d.SubmittedClaimAmount - ISNULL(d.Snapshot_CalculatedClaimAmount, 0) AS DECIMAL(18,2)) AS DeductedAmount
                        FROM dbo.SalesApprovalRequestDetail d
                        INNER JOIN dbo.SalesDailyActivity a ON d.ActivityId = a.ActivityId
                        WHERE d.ApprovalRequestId = @ReqId
                          AND d.SubmittedClaimAmount IS NOT NULL
                          AND d.SubmittedClaimAmount > ISNULL(d.Snapshot_CalculatedClaimAmount, 0)
                          AND ISNULL(d.DetailStatus, '') <> '" + ClaimStatuses.Rejected + @"'
                          AND d.ApprovalRequestDetailId IN (
                              SELECT MAX(d2.ApprovalRequestDetailId)
                              FROM dbo.SalesApprovalRequestDetail d2
                              INNER JOIN (
                                  SELECT ActivityId, MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
                                  FROM dbo.SalesApprovalRequestDetail
                                  WHERE ApprovalRequestId = @ReqId
                                  GROUP BY ActivityId
                              ) lv
                                  ON lv.ActivityId = d2.ActivityId
                                 AND ISNULL(d2.DetailVersion, 1) = lv.LatestVersion
                              WHERE d2.ApprovalRequestId = @ReqId
                              GROUP BY d2.ActivityId
                          )
                        ORDER BY ActivityDate ASC;
                    END";

                using (var con = new SqlConnection(connString))
                using (var cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@ReqId", approvalRequestId);
                    con.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            results.Add(new
                            {
                                ActivityDate = rdr["ActivityDate"].ToString(),
                                DeductionType = rdr["DeductionType"].ToString(),
                                Reason = rdr["Reason"].ToString(),
                                DeductedAmount = Convert.ToDecimal(rdr["DeductedAmount"])
                            });
                        }
                    }
                }
                return new { success = true, data = results };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object GetKMHistory(int activityId)
        {
            if (!UserOwnsActivity(activityId)) RejectUnauthorized();
            try
            {
                string connString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                string fromPlace = "";
                string toPlace = "";
                var historyList = new List<object>();

                using (var con = new SqlConnection(connString))
                {
                    con.Open();

                    string sqlPlaces = @"
                        SELECT 
                            (SELECT TOP 1 FromPlace FROM dbo.SalesDailyExpense WHERE ActivityId = @ActivityId AND IsDeleted = 0 AND FromPlace IS NOT NULL ORDER BY ExpenseId ASC) as FromPlace,
                            (SELECT TOP 1 ToPlace FROM dbo.SalesDailyExpense WHERE ActivityId = @ActivityId AND IsDeleted = 0 AND ToPlace IS NOT NULL ORDER BY ExpenseId DESC) as ToPlace";

                    using (var cmd = new SqlCommand(sqlPlaces, con))
                    {
                        cmd.Parameters.AddWithValue("@ActivityId", activityId);
                        using (var rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                fromPlace = rdr["FromPlace"]?.ToString() ?? "";
                                toPlace = rdr["ToPlace"]?.ToString() ?? "";
                            }
                        }
                    }

                    string sqlHistory = @"
                        SELECT HistoryId, ActivityId, OldKM, NewKM, ModifiedBy, ModifiedDateTime, Reason 
                        FROM dbo.SalesDailyActivityKMHistory 
                        WHERE ActivityId = @ActivityId 
                        ORDER BY ModifiedDateTime DESC";

                    using (var cmd = new SqlCommand(sqlHistory, con))
                    {
                        cmd.Parameters.AddWithValue("@ActivityId", activityId);
                        using (var rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                DateTime utc = rdr.GetDateTime(rdr.GetOrdinal("ModifiedDateTime"));
                                DateTime ist;
                                try { ist = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(utc, "UTC", "India Standard Time"); }
                                catch { ist = utc.AddHours(5).AddMinutes(30); }

                                historyList.Add(new
                                {
                                    OldKM = rdr["OldKM"],
                                    NewKM = rdr["NewKM"],
                                    ModifiedBy = rdr["ModifiedBy"]?.ToString(),
                                    Reason = rdr["Reason"]?.ToString(),
                                    ModifiedDateTimeIST = ist.ToString("dd-MM-yyyy hh:mm:ss tt")
                                });
                            }
                        }
                    }
                }

                return new { success = true, fromPlace = fromPlace, toPlace = toPlace, history = historyList };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

        protected void btnExportSelected_Click(object sender, EventArgs e)
        {
            List<int> selectedReqIds = new List<int>();
            Dictionary<int, string> reqEmpMap = new Dictionary<int, string>();

            foreach (GridViewRow row in gvRequests.Rows)
            {
                if (row.RowType == DataControlRowType.DataRow)
                {
                    CheckBox chk = row.FindControl("chkSelectReq") as CheckBox;
                    if (chk != null && chk.Checked)
                    {
                        HiddenField hfReqId = row.FindControl("hfReqId") as HiddenField;
                        HiddenField hfEmpCode = row.FindControl("hfEmpCode") as HiddenField;
                        int reqId;
                        if (hfReqId != null && int.TryParse(hfReqId.Value, out reqId))
                        {
                            selectedReqIds.Add(reqId);
                            reqEmpMap[reqId] = hfEmpCode != null ? hfEmpCode.Value : "EMP";
                        }
                    }
                }
            }

            if (selectedReqIds.Count == 0)
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "alert", "alert('Please select at least one claim to export.');", true);
                return;
            }

            foreach (int reqId in selectedReqIds)
            {
                if (!UserOwnsRequest(reqId)) RejectUnauthorized();
            }

            using (XLWorkbook wb = new XLWorkbook())
            {
                using (var con = new SqlConnection(connString))
                {
                    con.Open();

                    foreach (int reqId in selectedReqIds)
                    {
                        DataTable dtExport = new DataTable();
                        using (var cmd = new SqlCommand("dbo.usp_Export_Auditor_Final_Excel", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@ApprovalRequestId", reqId);
                            using (var da = new SqlDataAdapter(cmd))
                            {
                                da.Fill(dtExport);
                            }
                        }

                        if (dtExport.Rows.Count > 0)
                        {
                            DataRow totalRow = dtExport.NewRow();
                            totalRow["Claim No"] = DBNull.Value;
                            totalRow["Employee Code"] = DBNull.Value;
                            totalRow["Employee Name"] = DBNull.Value;
                            totalRow["Date"] = DBNull.Value;
                            totalRow["Day"] = "TOTAL:";
                            totalRow["From Place"] = DBNull.Value;
                            totalRow["To Place"] = DBNull.Value;

                            totalRow["KM Claimed"] = dtExport.Compute("SUM([KM Claimed])", string.Empty);
                            totalRow["TA Bill"] = dtExport.Compute("SUM([TA Bill])", string.Empty);
                            totalRow["DA"] = dtExport.Compute("SUM([DA])", string.Empty);
                            totalRow["Hotel"] = dtExport.Compute("SUM([Hotel])", string.Empty);
                            totalRow["Travel / OPA"] = dtExport.Compute("SUM([Travel / OPA])", string.Empty);
                            totalRow["Stationary"] = dtExport.Compute("SUM([Stationary])", string.Empty);
                            totalRow["Total Expenses (DA+Hotel+Trv+Stat)"] = dtExport.Compute("SUM([Total Expenses (DA+Hotel+Trv+Stat)])", string.Empty);
                            totalRow["Final Approved Amount"] = dtExport.Compute("SUM([Final Approved Amount])", string.Empty);

                            dtExport.Rows.Add(totalRow);

                            string sheetName = $"ID_{reqId}_{reqEmpMap[reqId]}";
                            string finalSheetName = sheetName;

                            if (finalSheetName.Length > 31)
                            {
                                finalSheetName = finalSheetName.Substring(0, 31);
                            }

                            int counter = 1;
                            while (wb.Worksheets.Contains(finalSheetName))
                            {
                                string suffix = $"_{counter}";
                                int allowedLength = 31 - suffix.Length;
                                finalSheetName = $"{sheetName.Substring(0, Math.Min(sheetName.Length, allowedLength))}{suffix}";
                                counter++;
                            }

                            var ws = wb.Worksheets.Add(dtExport, finalSheetName);
                            var lastRow = ws.LastRowUsed();
                            lastRow.Style.Font.Bold = true;
                            lastRow.Style.Fill.BackgroundColor = XLColor.LightGray;
                            ws.Columns().AdjustToContents();
                        }
                    }
                }

                if (wb.Worksheets.Count > 0)
                {
                    string fileName = $"Bulk_MyClaims_Export_{DateTime.Now:ddMMMyyyy}.xlsx";

                    Response.Clear();
                    Response.Buffer = true;
                    Response.Charset = "";
                    Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    Response.AddHeader("content-disposition", "attachment;filename=" + fileName);

                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        wb.SaveAs(memoryStream);
                        memoryStream.WriteTo(Response.OutputStream);
                        Response.Flush();
                        Response.End();
                    }
                }
                else
                {
                    ScriptManager.RegisterStartupScript(this, GetType(), "alert", "alert('No exportable data found for the selected claims.');", true);
                }
            }
        }

        private static string CurrentUserLoginId()
        {
            var s = HttpContext.Current != null ? HttpContext.Current.Session : null;
            if (s == null) return null;
            return (s["WORKMAN"] ?? s["workman"]) as string;
        }

        private static bool UserOwnsRequest(int approvalRequestId)
        {
            string user = CurrentUserLoginId();
            if (string.IsNullOrWhiteSpace(user) || approvalRequestId <= 0) return false;
            using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            using (var cmd = new SqlCommand("SELECT TOP 1 1 FROM dbo.SalesApprovalRequest WHERE ApprovalRequestId=@Id AND RequesterLoginId=@User", con))
            {
                cmd.Parameters.AddWithValue("@Id", approvalRequestId);
                cmd.Parameters.AddWithValue("@User", user);
                con.Open();
                return cmd.ExecuteScalar() != null;
            }
        }

        private static bool UserOwnsActivity(int activityId)
        {
            string user = CurrentUserLoginId();
            if (string.IsNullOrWhiteSpace(user) || activityId <= 0) return false;
            using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            using (var cmd = new SqlCommand(@"
                SELECT TOP 1 1
                FROM dbo.SalesApprovalRequestDetail d
                INNER JOIN dbo.SalesApprovalRequest r ON r.ApprovalRequestId = d.ApprovalRequestId
                WHERE d.ActivityId=@ActivityId AND r.RequesterLoginId=@User", con))
            {
                cmd.Parameters.AddWithValue("@ActivityId", activityId);
                cmd.Parameters.AddWithValue("@User", user);
                con.Open();
                return cmd.ExecuteScalar() != null;
            }
        }

        private static void RejectUnauthorized()
        {
            throw new HttpException(403, "Unauthorized");
        }
    }
}