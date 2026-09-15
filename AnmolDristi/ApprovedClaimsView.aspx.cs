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
    public partial class ApprovedClaimsView : BasePage
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

        protected void Page_Load(object sender, EventArgs e)
        {
            RequirePermission("UI.SO_TaDa_ViewApproved.View");
            if (!IsPostBack)
            {
                if (Session["USERID"] != null)
                {
                    PopulateYearDropdown();
                    SetDefaultClaimMonth();
                    BindRequestsGrid();
                }
            }
        }

        // ==========================================
        // UI & FILTER LOGIC
        // ==========================================
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
            // Claims are typically filed for the previous month
            DateTime claimMonth = DateTime.Now.AddMonths(-1);

            ViewState["FilterYear"] = claimMonth.Year;
            ViewState["FilterMonth"] = claimMonth.Month;
            txtSearchEmpCode.Text = "";

            SyncUIWithViewState();
        }

        private void SyncUIWithViewState()
        {
            int y = (int)ViewState["FilterYear"];
            int m = (int)ViewState["FilterMonth"];

            DateTime dt = new DateTime(y, m, 1);
            lblCurrentHeader.Text = dt.ToString("MMMM, yyyy");

            if (ddlSearchYear.Items.FindByValue(y.ToString()) != null)
                ddlSearchYear.SelectedValue = y.ToString();

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
            BindRequestsGrid();
        }

        protected void btnNextMonth_Click(object sender, EventArgs e)
        {
            DateTime currentFilter = new DateTime((int)ViewState["FilterYear"], (int)ViewState["FilterMonth"], 1);
            DateTime nextMonth = currentFilter.AddMonths(1);

            ViewState["FilterYear"] = nextMonth.Year;
            ViewState["FilterMonth"] = nextMonth.Month;

            SyncUIWithViewState();
            BindRequestsGrid();
        }

        protected void btnCurrentMonth_Click(object sender, EventArgs e)
        {
            SetDefaultClaimMonth();
            BindRequestsGrid();
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            this.SelectedExportItems = new Dictionary<int, string>(); // Clear memory
            if (ddlSearchYear.SelectedValue != "0")
                ViewState["FilterYear"] = Convert.ToInt32(ddlSearchYear.SelectedValue);

            if (ddlSearchMonth.SelectedValue != "0")
                ViewState["FilterMonth"] = Convert.ToInt32(ddlSearchMonth.SelectedValue);

            lblCurrentHeader.Text = "Custom Search Results";
            BindRequestsGrid();
        }

        protected void btnReset_Click(object sender, EventArgs e)
        {
            this.SelectedExportItems = new Dictionary<int, string>(); // Clear memory

            SetDefaultClaimMonth();
            BindRequestsGrid();
        }

        // ==========================================
        // DATA BINDING
        // ==========================================
        protected void BindRequestsGrid()
        {
            string employeeCode = Session["workman"]?.ToString() ?? "Guest";
            DataTable dt = new DataTable();

            using (var con = new SqlConnection(connString))
            {
                using (var cmd = new SqlCommand("dbo.usp_GetApprovedClaimsByUser", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserLoginId", employeeCode);

                    if (ViewState["FilterYear"] != null && ddlSearchYear.SelectedValue != "0")
                        cmd.Parameters.AddWithValue("@FilterYear", ViewState["FilterYear"]);
                    else
                        cmd.Parameters.AddWithValue("@FilterYear", DBNull.Value);

                    if (ViewState["FilterMonth"] != null && ddlSearchMonth.SelectedValue != "0")
                        cmd.Parameters.AddWithValue("@FilterMonth", ViewState["FilterMonth"]);
                    else
                        cmd.Parameters.AddWithValue("@FilterMonth", DBNull.Value);

                    if (!string.IsNullOrWhiteSpace(txtSearchEmpCode.Text))
                        cmd.Parameters.AddWithValue("@SearchEmpCode", txtSearchEmpCode.Text.Trim());
                    else
                        cmd.Parameters.AddWithValue("@SearchEmpCode", DBNull.Value);

                    using (var da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
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

        // ==========================================
        // EXPORT LOGIC (CLOSEDXML)
        // ==========================================
        private void ExportClaimToExcel(int reqId)
        {
            if (!UserIsApproverOnRequest(reqId)) RejectUnauthorized();
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

            if (dtExport.Rows.Count > 0)
            {
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
                totalRow["Total DA + Hotel + OPA"] = dtExport.Compute("SUM([Total DA + Hotel + OPA])", string.Empty);
                totalRow["Final Approved Amount"] = dtExport.Compute("SUM([Final Approved Amount])", string.Empty);

                dtExport.Rows.Add(totalRow);

                using (XLWorkbook wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add(dtExport, "Approved Claim");

                    var lastRow = ws.LastRowUsed();
                    lastRow.Style.Font.Bold = true;
                    lastRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                    ws.Columns().AdjustToContents();

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
            }
        }

        // ==========================================
        // DETAILS MODAL LOGIC
        // ==========================================
        protected void ShowDetailsModal(int reqId)
        {
            if (!UserIsApproverOnRequest(reqId)) RejectUnauthorized();
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

                string sql = "SELECT (SELECT EmployeeName FROM MST_UserMaster WHERE EmployeeCode = r.RequesterLoginId) as EmpName FROM SalesApprovalRequest r WHERE ApprovalRequestId = @Id";
                using (var cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Id", reqId);
                    con.Open();
                    object result = cmd.ExecuteScalar();
                    if (result != null) requesterName = result.ToString();
                }
            }

            lblRequesterName.Text = requesterName;
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

            // Dashboard Math
            decimal currentApproved = _totalTAClaim + _totalSnapshot;
            decimal totalDeductions = 0m;
            decimal currentApprovedAmount = 0m;
            decimal currentApprovedKM = 0m;
            decimal totalDeductedAmount = 0m;
            decimal totalDeductedKM = 0m;

            string sqlDashboard = @"
                SELECT 
                    ISNULL(SUM(d.Snapshot_TotalDistanceKM), 0) AS ApprovedKM,
                    ISNULL(SUM(ISNULL(d.Snapshot_CalculatedClaimAmount, 0) + ISNULL(d.Snapshot_ExpenseTotal, 0)), 0) AS ApprovedAmount
                FROM dbo.SalesApprovalRequestDetail d
                WHERE d.ApprovalRequestId = @Id
                  AND ISNULL(d.DetailStatus, '') NOT IN ('" + ClaimStatuses.Rejected + @"', '" + ClaimStatuses.Returned + @"')
                  AND " + SalesApprovalDetailVersionHelper.LatestDetailWhereForD.Trim() + @";

                SELECT 
                    ISNULL(SUM(DeductedAmount), 0) AS DeductedAmount,
                    ISNULL(SUM(DeductedKM), 0) AS DeductedKM
                FROM (
                    SELECT 
                        CAST((ISNULL(d.Snapshot_CalculatedClaimAmount, 0) + ISNULL(d.Snapshot_ExpenseTotal, 0)) AS DECIMAL(18,2)) AS DeductedAmount,
                        ISNULL(d.Snapshot_TotalDistanceKM, 0) AS DeductedKM
                    FROM dbo.SalesApprovalRequestDetail d
                    WHERE d.ApprovalRequestId = @Id
                      AND d.DetailStatus IN ('" + ClaimStatuses.Rejected + @"', '" + ClaimStatuses.Returned + @"')
                      AND " + SalesApprovalDetailVersionHelper.LatestDetailWhereForD.Trim() + @"

                    UNION ALL

                    SELECT 
                        CAST(e.Amount AS DECIMAL(18,2)) AS DeductedAmount,
                        0 AS DeductedKM
                    FROM dbo.SalesDailyExpense e
                    INNER JOIN dbo.SalesApprovalRequestDetail d ON e.ActivityId = d.ActivityId
                    " + SalesApprovalDetailVersionHelper.LatestDetailJoinForD + @"
                    WHERE d.ApprovalRequestId = @Id AND e.ClaimStatus = '" + ClaimStatuses.Rejected + @"' AND e.IsDeleted = 0 AND ISNULL(d.DetailStatus, '') <> '" + ClaimStatuses.Rejected + @"'

                    UNION ALL

                    SELECT 
                        CAST(ROUND((fk.ClaimedKM - d.Snapshot_TotalDistanceKM) * 
                            CASE WHEN ISNULL(d.Snapshot_TotalDistanceKM, 0) > 0 
                                 THEN ISNULL(d.Snapshot_CalculatedClaimAmount, 0) / d.Snapshot_TotalDistanceKM 
                                 ELSE 3.75 END, 2) AS DECIMAL(18,2)) AS DeductedAmount,
                        (fk.ClaimedKM - d.Snapshot_TotalDistanceKM) AS DeductedKM
                    FROM dbo.SalesApprovalRequestDetail d
                    INNER JOIN (
                        SELECT ActivityId, MAX(CASE WHEN NewKM > ISNULL(OldKM,0) THEN NewKM ELSE OldKM END) AS ClaimedKM 
                        FROM dbo.SalesDailyActivityKMHistory
                        GROUP BY ActivityId
                    ) fk ON d.ActivityId = fk.ActivityId
                    WHERE d.ApprovalRequestId = @Id 
                      AND fk.ClaimedKM > d.Snapshot_TotalDistanceKM 
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

            decimal originalClaimedAmount = currentApprovedAmount + totalDeductedAmount;
            decimal originalClaimedKM = currentApprovedKM + totalDeductedKM;

            lblSummaryOriginal.Text = originalClaimedAmount.ToString("N2");
            lblSummaryApproved.Text = currentApprovedAmount.ToString("N2");
            lblSummaryDeduction.Text = totalDeductedAmount.ToString("N2");
            lblSummaryOriginalKM.Text = originalClaimedKM.ToString("N2");
            lblSummaryApprovedKM.Text = currentApprovedKM.ToString("N2");
            lblSummaryDeductedKM.Text = totalDeductedKM.ToString("N2");
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
                _totalSnapshot += (drv["Snapshot_ExpenseTotal"] == DBNull.Value) ? 0m : Convert.ToDecimal(drv["Snapshot_ExpenseTotal"]);
                _totalStationary += (drv["Snapshot_StationaryAmount"] == DBNull.Value) ? 0m : Convert.ToDecimal(drv["Snapshot_StationaryAmount"]);

                string detailStatus = drv["DetailStatus"].ToString();
                if (ClaimStatuses.Equals(detailStatus, ClaimStatuses.Rejected))
                {
                    e.Row.CssClass += " table-danger";
                }
                else if (ClaimStatuses.Equals(detailStatus, ClaimStatuses.Approved))
                {
                    e.Row.CssClass += " table-success";
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

        // ==========================================
        // WEBMETHODS
        // ==========================================
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object GetDeductionBreakdown(int approvalRequestId)
        {
            if (!UserIsApproverOnRequest(approvalRequestId)) RejectUnauthorized();
            try
            {
                string connString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                var results = new List<object>();
                string sql = @"
                    SELECT 
                        CONVERT(varchar, a.ActivityDate, 106) AS ActivityDate,
                        'Whole Day Rejected' AS DeductionType,
                        ISNULL(d.DetailRemarks, 'Rejected by Approver') AS Reason,
                        CAST((ISNULL(d.Snapshot_CalculatedClaimAmount, 0) + ISNULL(d.Snapshot_ExpenseTotal, 0)) AS DECIMAL(18,2)) AS DeductedAmount
                    FROM dbo.SalesApprovalRequestDetail d
                    INNER JOIN dbo.SalesDailyActivity a ON d.ActivityId = a.ActivityId
                    WHERE d.ApprovalRequestId = @Id
                      AND d.DetailStatus IN ('" + ClaimStatuses.Rejected + @"', '" + ClaimStatuses.Returned + @"')
                      AND " + SalesApprovalDetailVersionHelper.LatestDetailWhereForD.Trim() + @"

                    UNION ALL

                    SELECT 
                        CONVERT(varchar, a.ActivityDate, 106) AS ActivityDate,
                        'Rejected Receipt: ' + e.ExpenseType AS DeductionType,
                        ISNULL(e.Remarks, 'Receipt marked as Rejected') AS Reason,
                        CAST(e.Amount AS DECIMAL(18,2)) AS DeductedAmount
                    FROM dbo.SalesDailyExpense e
                    INNER JOIN dbo.SalesApprovalRequestDetail d ON e.ActivityId = d.ActivityId
                    " + SalesApprovalDetailVersionHelper.LatestDetailJoinForD + @"
                    INNER JOIN dbo.SalesDailyActivity a ON d.ActivityId = a.ActivityId
                    WHERE d.ApprovalRequestId = @Id 
                      AND e.ClaimStatus = '" + ClaimStatuses.Rejected + @"' 
                      AND e.IsDeleted = 0
                      AND ISNULL(d.DetailStatus, '') <> '" + ClaimStatuses.Rejected + @"'

                    UNION ALL

                    SELECT 
                        CONVERT(varchar, a.ActivityDate, 106) AS ActivityDate,
                        'KM Reduced (' + CAST(fk.ClaimedKM AS varchar) + ' to ' + CAST(d.Snapshot_TotalDistanceKM AS varchar) + ')' AS DeductionType,
                        ISNULL((SELECT TOP 1 Reason FROM dbo.SalesDailyActivityKMHistory WHERE ActivityId = a.ActivityId ORDER BY ModifiedDateTime DESC), 'KM corrected') AS Reason,
                        CAST(ROUND((fk.ClaimedKM - d.Snapshot_TotalDistanceKM) * 
                            CASE WHEN ISNULL(d.Snapshot_TotalDistanceKM, 0) > 0 
                                 THEN ISNULL(d.Snapshot_CalculatedClaimAmount, 0) / d.Snapshot_TotalDistanceKM 
                                 ELSE 3.75 END, 2) AS DECIMAL(18,2)) AS DeductedAmount
                    FROM dbo.SalesApprovalRequestDetail d
                    INNER JOIN dbo.SalesDailyActivity a ON d.ActivityId = a.ActivityId
                    INNER JOIN (
                        SELECT ActivityId, MAX(CASE WHEN NewKM > ISNULL(OldKM,0) THEN NewKM ELSE OldKM END) AS ClaimedKM 
                        FROM dbo.SalesDailyActivityKMHistory
                        GROUP BY ActivityId
                    ) fk ON d.ActivityId = fk.ActivityId
                    WHERE d.ApprovalRequestId = @Id 
                      AND fk.ClaimedKM > d.Snapshot_TotalDistanceKM
                      AND ISNULL(d.DetailStatus, '') <> '" + ClaimStatuses.Rejected + @"'
                      AND " + SalesApprovalDetailVersionHelper.LatestDetailWhereForD.Trim() + @"        
                    ORDER BY ActivityDate ASC;";

                using (var con = new SqlConnection(connString))
                using (var cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Id", approvalRequestId);
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
            if (!UserIsApproverOnActivity(activityId)) RejectUnauthorized();
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

        //protected void gvRequests_PageIndexChanging(object sender, GridViewPageEventArgs e)
        //{
        //    // 1. Set the new page index
        //    gvRequests.PageIndex = e.NewPageIndex;

        //    // 2. Re-fetch the data and bind the grid to show the new page
        //    BindRequestsGrid();
        //}

        protected void gvRequests_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            SaveCurrentPageSelection();          // 1. Save current checkboxes
            gvRequests.PageIndex = e.NewPageIndex; // 2. Change page
            BindRequestsGrid();                  // 3. Fetch new data
            RestorePageSelection();              // 4. Restore checkboxes for this page
        }

        protected void btnExportSelected_Click_OLD(object sender, EventArgs e)
        {
            List<int> selectedReqIds = new List<int>();
            Dictionary<int, string> reqEmpMap = new Dictionary<int, string>();

            // 1. Gather all selected Request IDs from the grid
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
                            // Store the Employee Code so we can name the Excel sheet nicely
                            reqEmpMap[reqId] = hfEmpCode != null ? hfEmpCode.Value : "EMP";
                        }
                    }
                }
            }

            // Guard clause: if they clicked the button without selecting anything
            if (selectedReqIds.Count == 0)
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "alert", "alert('Please select at least one claim to export.');", true);
                return;
            }

            // 2. Create a single Workbook and iterate through the selected IDs
            using (XLWorkbook wb = new XLWorkbook())
            {
                using (var con = new SqlConnection(connString))
                {
                    con.Open(); // Open the connection just once for better performance

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
                            // A. Create the Totals Row
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
                            totalRow["Total DA + Hotel + OPA"] = dtExport.Compute("SUM([Total DA + Hotel + OPA])", string.Empty);
                            totalRow["Final Approved Amount"] = dtExport.Compute("SUM([Final Approved Amount])", string.Empty);

                            dtExport.Rows.Add(totalRow);

                            // B. Name the Sheet (Excel limits sheet names to 31 characters max)
                            string sheetName = $"ID_{reqId}_{reqEmpMap[reqId]}";
                            string finalSheetName = sheetName;

                            // Ensure initial name is 31 characters max
                            if (finalSheetName.Length > 31)
                            {
                                finalSheetName = finalSheetName.Substring(0, 31);
                            }

                            // Make absolutely sure the sheet name is unique in the workbook
                            int counter = 1;
                            while (wb.Worksheets.Contains(finalSheetName))
                            {
                                string suffix = $"_{counter}";
                                // Calculate exactly how many characters we can keep from the original name
                                int allowedLength = 31 - suffix.Length;

                                finalSheetName = $"{sheetName.Substring(0, Math.Min(sheetName.Length, allowedLength))}{suffix}";
                                counter++;
                            }

                            // C. Add to Workbook and Style
                            var ws = wb.Worksheets.Add(dtExport, finalSheetName);
                            var lastRow = ws.LastRowUsed();
                            lastRow.Style.Font.Bold = true;
                            lastRow.Style.Fill.BackgroundColor = XLColor.LightGray;
                            ws.Columns().AdjustToContents();
                        }
                    }
                }

                // 3. Final Download Logic
                if (wb.Worksheets.Count > 0)
                {
                    string fileName = $"Bulk_Claims_Export_{DateTime.Now:ddMMMyyyy}.xlsx";

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

        protected void btnExportSelected_Click(object sender, EventArgs e)
        {
            SaveCurrentPageSelection();
            var itemsToExport = this.SelectedExportItems;

            if (itemsToExport == null || itemsToExport.Count == 0)
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "alert", "alert('Please select at least one claim to export.');", true);
                return;
            }

            foreach (int reqId in itemsToExport.Keys)
            {
                if (!UserIsApproverOnRequest(reqId)) RejectUnauthorized();
            }

            using (XLWorkbook wb = new XLWorkbook())
            {
                using (var con = new SqlConnection(connString))
                {
                    con.Open();
                    foreach (var kvp in itemsToExport)
                    {
                        int reqId = kvp.Key;
                        string empCode = kvp.Value;

                        DataTable dtExport = new DataTable();
                        using (var cmd = new SqlCommand("dbo.usp_Export_Auditor_Final_Excel", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@ApprovalRequestId", reqId);
                            using (var da = new SqlDataAdapter(cmd)) { da.Fill(dtExport); }
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
                            totalRow["Stationary"] = dtExport.Compute("SUM([Stationary])", string.Empty); // NEW
                            totalRow["Total Expenses (DA+Hotel+Trv+Stat)"] = dtExport.Compute("SUM([Total Expenses (DA+Hotel+Trv+Stat)])", string.Empty);
                            totalRow["Final Approved Amount"] = dtExport.Compute("SUM([Final Approved Amount])", string.Empty);

                            dtExport.Rows.Add(totalRow);

                            string sheetName = $"ID_{reqId}_{empCode}";
                            string finalSheetName = sheetName.Length > 31 ? sheetName.Substring(0, 31) : sheetName;

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
                    string fileName = $"Bulk_Claims_Export_{DateTime.Now:ddMMMyyyy}.xlsx";
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
                    ScriptManager.RegisterStartupScript(this, GetType(), "alert", "alert('No exportable data found.');", true);
                }
            }
        }

        // This stores the Request ID (Key) and the Employee Code (Value) across page loads
        private Dictionary<int, string> SelectedExportItems
        {
            get
            {
                if (ViewState["SelectedExportItems"] == null)
                    ViewState["SelectedExportItems"] = new Dictionary<int, string>();
                return (Dictionary<int, string>)ViewState["SelectedExportItems"];
            }
            set { ViewState["SelectedExportItems"] = value; }
        }

        private void SaveCurrentPageSelection()
        {
            Dictionary<int, string> selectedItems = this.SelectedExportItems;

            foreach (GridViewRow row in gvRequests.Rows)
            {
                if (row.RowType == DataControlRowType.DataRow)
                {
                    CheckBox chk = row.FindControl("chkSelectReq") as CheckBox;
                    HiddenField hfReqId = row.FindControl("hfReqId") as HiddenField;
                    HiddenField hfEmpCode = row.FindControl("hfEmpCode") as HiddenField;

                    if (chk != null && hfReqId != null && hfEmpCode != null)
                    {
                        int reqId = Convert.ToInt32(hfReqId.Value);
                        string empCode = hfEmpCode.Value;

                        if (chk.Checked)
                        {
                            if (!selectedItems.ContainsKey(reqId))
                                selectedItems.Add(reqId, empCode); // Add to memory
                        }
                        else
                        {
                            if (selectedItems.ContainsKey(reqId))
                                selectedItems.Remove(reqId); // Remove from memory if unchecked
                        }
                    }
                }
            }
            this.SelectedExportItems = selectedItems;
        }

        private void RestorePageSelection()
        {
            Dictionary<int, string> selectedItems = this.SelectedExportItems;

            foreach (GridViewRow row in gvRequests.Rows)
            {
                if (row.RowType == DataControlRowType.DataRow)
                {
                    HiddenField hfReqId = row.FindControl("hfReqId") as HiddenField;
                    CheckBox chk = row.FindControl("chkSelectReq") as CheckBox;

                    if (hfReqId != null && chk != null)
                    {
                        int reqId = Convert.ToInt32(hfReqId.Value);
                        // If it's in our memory bank, re-check the box automatically
                        if (selectedItems.ContainsKey(reqId))
                        {
                            chk.Checked = true;
                        }
                    }
                }
            }
        }

        private static string CurrentUserLoginId()
        {
            var s = HttpContext.Current != null ? HttpContext.Current.Session : null;
            if (s == null) return null;
            return (s["WORKMAN"] ?? s["workman"]) as string;
        }

        private static bool UserIsApproverOnRequest(int approvalRequestId)
        {
            string user = CurrentUserLoginId();
            if (string.IsNullOrWhiteSpace(user) || approvalRequestId <= 0) return false;
            using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            using (var cmd = new SqlCommand("SELECT TOP 1 1 FROM dbo.SalesApprovalRequestApprover WHERE ApprovalRequestId=@Id AND ApproverLoginId=@User", con))
            {
                cmd.Parameters.AddWithValue("@Id", approvalRequestId);
                cmd.Parameters.AddWithValue("@User", user);
                con.Open();
                return cmd.ExecuteScalar() != null;
            }
        }

        private static bool UserIsApproverOnActivity(int activityId)
        {
            string user = CurrentUserLoginId();
            if (string.IsNullOrWhiteSpace(user) || activityId <= 0) return false;
            using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            using (var cmd = new SqlCommand(@"
                SELECT TOP 1 1
                FROM dbo.SalesApprovalRequestDetail d
                INNER JOIN dbo.SalesApprovalRequestApprover saa ON saa.ApprovalRequestId = d.ApprovalRequestId
                WHERE d.ActivityId=@ActivityId AND saa.ApproverLoginId=@User", con))
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