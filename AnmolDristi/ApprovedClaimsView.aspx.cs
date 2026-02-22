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
using System.IO;
using ClosedXML.Excel;

namespace AnmolDristi
{
    public partial class ApprovedClaimsView : System.Web.UI.Page
    {
        private string connString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        // Accumulators for footer logic
        private decimal _totalSnapshot = 0m;
        private decimal _totalTAClaim = 0m;
        private decimal _totalDistance = 0m;
        private decimal _totalDA = 0m;
        private decimal _totalHotel = 0m;
        private decimal _totalTravel = 0m;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["workman"] != null)
                {
                    BindRequestsGrid();
                }
            }
        }

        protected void BindRequestsGrid()
        {
            string employeeCode = Session["workman"]?.ToString() ?? "Guest";
            DataTable dt = new DataTable();
            using (var con = new SqlConnection(connString))
            {
                // TODO: Replace with your actual SP that gets Approved Claims for the logged-in user
                using (var cmd = new SqlCommand("dbo.usp_GetApprovedClaimsByUser", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserLoginId", employeeCode);
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

        private void ExportClaimToExcel(int reqId)
        {
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
                // 1. Get the RequesterLoginId (Employee Code) for the filename
                string empCode = dtExport.Rows[0]["Employee Code"].ToString();
                // Notice we are using .xlsx now!
                string fileName = $"Claim_{reqId}_{empCode}.xlsx";

                // 2. Create a new Totals Row
                DataRow totalRow = dtExport.NewRow();

                totalRow["Claim No"] = DBNull.Value;
                totalRow["Employee Code"] = DBNull.Value;
                totalRow["Employee Name"] = DBNull.Value;
                totalRow["Date"] = DBNull.Value;
                totalRow["Day"] = "TOTAL:";

                // NEW: Leave Route columns blank on the totals row
                totalRow["From Place"] = DBNull.Value;
                totalRow["To Place"] = DBNull.Value;

                // Calculate sums using DataTable.Compute
                totalRow["KM Claimed"] = dtExport.Compute("SUM([KM Claimed])", string.Empty);
                totalRow["TA Bill"] = dtExport.Compute("SUM([TA Bill])", string.Empty);
                totalRow["DA"] = dtExport.Compute("SUM([DA])", string.Empty);
                totalRow["Hotel"] = dtExport.Compute("SUM([Hotel])", string.Empty);
                totalRow["Travel / OPA"] = dtExport.Compute("SUM([Travel / OPA])", string.Empty);
                totalRow["Total DA + Hotel + OPA"] = dtExport.Compute("SUM([Total DA + Hotel + OPA])", string.Empty);
                totalRow["Final Approved Amount"] = dtExport.Compute("SUM([Final Approved Amount])", string.Empty);

                dtExport.Rows.Add(totalRow);

                // 3. Export Logic using ClosedXML
                using (XLWorkbook wb = new XLWorkbook())
                {
                    // Add the DataTable as a worksheet
                    var ws = wb.Worksheets.Add(dtExport, "Approved Claim");

                    // Format the Totals row (the very last used row)
                    var lastRow = ws.LastRowUsed();
                    lastRow.Style.Font.Bold = true;
                    lastRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                    // Auto-fit all columns so the data is instantly readable
                    ws.Columns().AdjustToContents();

                    // Prepare the response for a true .xlsx file
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

        // Required by ASP.NET when using GridView.RenderControl
        public override void VerifyRenderingInServerForm(Control control)
        {
            /* Confirms that an HtmlForm control is rendered for the specified ASP.NET server control at run time. */
        }

        protected void ShowDetailsModal(int reqId)
        {
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

            DataTable dt = new DataTable();
            string requesterName = "";

            using (var con = new SqlConnection(connString))
            {
                // A. Fetch Line Items
                using (var cmd = new SqlCommand("dbo.usp_GetApprovalRequestDetails", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ApprovalRequestId", reqId);
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }

                // B. Fetch Header Info
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

            // C. CALCULATE SUMMARY
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
                WHERE d.ApprovalRequestId = @Id AND ISNULL(d.DetailStatus, '') <> 'Rejected';

                SELECT 
                    ISNULL(SUM(DeductedAmount), 0) AS DeductedAmount,
                    ISNULL(SUM(DeductedKM), 0) AS DeductedKM
                FROM (
                    SELECT CAST((ISNULL(d.Snapshot_CalculatedClaimAmount, 0) + ISNULL(d.Snapshot_ExpenseTotal, 0)) AS DECIMAL(18,2)) AS DeductedAmount,
                           ISNULL(d.Snapshot_TotalDistanceKM, 0) AS DeductedKM
                    FROM dbo.SalesApprovalRequestDetail d
                    WHERE d.ApprovalRequestId = @Id AND d.DetailStatus = 'Rejected'
                    UNION ALL
                    SELECT CAST(e.Amount AS DECIMAL(18,2)) AS DeductedAmount, 0 AS DeductedKM
                    FROM dbo.SalesDailyExpense e
                    INNER JOIN dbo.SalesApprovalRequestDetail d ON e.ActivityId = d.ActivityId
                    WHERE d.ApprovalRequestId = @Id AND e.ClaimStatus = 'Rejected' AND e.IsDeleted = 0 AND ISNULL(d.DetailStatus, '') <> 'Rejected'
                    UNION ALL
                    SELECT CAST(ROUND((fk.ClaimedKM - d.Snapshot_TotalDistanceKM) * 2.75, 2) AS DECIMAL(18,2)) AS DeductedAmount,
                           (fk.ClaimedKM - d.Snapshot_TotalDistanceKM) AS DeductedKM
                    FROM dbo.SalesApprovalRequestDetail d
                    INNER JOIN (
                        SELECT ActivityId, MAX(CASE WHEN NewKM > ISNULL(OldKM,0) THEN NewKM ELSE OldKM END) AS ClaimedKM 
                        FROM dbo.SalesDailyActivityKMHistory GROUP BY ActivityId
                    ) fk ON d.ActivityId = fk.ActivityId
                    WHERE d.ApprovalRequestId = @Id AND fk.ClaimedKM > d.Snapshot_TotalDistanceKM AND ISNULL(d.DetailStatus, '') <> 'Rejected'
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

            // Update UI Labels
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

                // Accumulate totals
                _totalDistance += (drv["Snapshot_TotalDistanceKM"] == DBNull.Value) ? 0m : Convert.ToDecimal(drv["Snapshot_TotalDistanceKM"]);
                _totalTAClaim += (drv["Snapshot_CalculatedClaimAmount"] == DBNull.Value) ? 0m : Convert.ToDecimal(drv["Snapshot_CalculatedClaimAmount"]);
                _totalDA += (drv["Snapshot_DAAmount"] == DBNull.Value) ? 0m : Convert.ToDecimal(drv["Snapshot_DAAmount"]);
                _totalHotel += (drv["Snapshot_HotelAmount"] == DBNull.Value) ? 0m : Convert.ToDecimal(drv["Snapshot_HotelAmount"]);
                _totalTravel += (drv["Snapshot_TravelAmount"] == DBNull.Value) ? 0m : Convert.ToDecimal(drv["Snapshot_TravelAmount"]);
                _totalSnapshot += (drv["Snapshot_ExpenseTotal"] == DBNull.Value) ? 0m : Convert.ToDecimal(drv["Snapshot_ExpenseTotal"]);

                // Apply read-only status coloring
                string detailStatus = drv["DetailStatus"].ToString();
                if (detailStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                {
                    e.Row.CssClass += " table-danger";
                }
                else if (detailStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase))
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
            try
            {
                string connString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                var results = new List<object>();
                string sql = @"
                    SELECT 
                        CONVERT(varchar, a.ActivityDate, 106) AS ActivityDate,
                        'Whole Day Rejected' AS DeductionType,
                        ISNULL(d.DetailRemarks, 'Rejected by Auditor') AS Reason,
                        CAST((ISNULL(d.Snapshot_CalculatedClaimAmount, 0) + ISNULL(d.Snapshot_ExpenseTotal, 0)) AS DECIMAL(18,2)) AS DeductedAmount
                    FROM dbo.SalesApprovalRequestDetail d
                    INNER JOIN dbo.SalesDailyActivity a ON d.ActivityId = a.ActivityId
                    WHERE d.ApprovalRequestId = @ReqId AND d.DetailStatus = 'Rejected'

                    UNION ALL

                    SELECT 
                        CONVERT(varchar, a.ActivityDate, 106) AS ActivityDate,
                        'Rejected Receipt: ' + e.ExpenseType AS DeductionType,
                        'Receipt marked as Rejected' AS Reason,
                        CAST(e.Amount AS DECIMAL(18,2)) AS DeductedAmount
                    FROM dbo.SalesDailyExpense e
                    INNER JOIN dbo.SalesApprovalRequestDetail d ON e.ActivityId = d.ActivityId
                    INNER JOIN dbo.SalesDailyActivity a ON d.ActivityId = a.ActivityId
                    WHERE d.ApprovalRequestId = @ReqId 
                      AND e.ClaimStatus = 'Rejected' 
                      AND e.IsDeleted = 0
                      AND ISNULL(d.DetailStatus, '') <> 'Rejected'

                    UNION ALL

                    SELECT 
                        CONVERT(varchar, a.ActivityDate, 106) AS ActivityDate,
                        'KM Reduced (' + CAST(fk.ClaimedKM AS varchar) + ' to ' + CAST(d.Snapshot_TotalDistanceKM AS varchar) + ')' AS DeductionType,
                        ISNULL((SELECT TOP 1 Reason FROM dbo.SalesDailyActivityKMHistory WHERE ActivityId = a.ActivityId ORDER BY ModifiedDateTime DESC), 'KM corrected') AS Reason,
                        CAST(ROUND((fk.ClaimedKM - d.Snapshot_TotalDistanceKM) * 2.75, 2) AS DECIMAL(18,2)) AS DeductedAmount
                    FROM dbo.SalesApprovalRequestDetail d
                    INNER JOIN dbo.SalesDailyActivity a ON d.ActivityId = a.ActivityId
                    INNER JOIN (
                        SELECT ActivityId, MAX(CASE WHEN NewKM > ISNULL(OldKM,0) THEN NewKM ELSE OldKM END) AS ClaimedKM 
                        FROM dbo.SalesDailyActivityKMHistory
                        GROUP BY ActivityId
                    ) fk ON d.ActivityId = fk.ActivityId
                    WHERE d.ApprovalRequestId = @ReqId 
                      AND fk.ClaimedKM > d.Snapshot_TotalDistanceKM
                      AND ISNULL(d.DetailStatus, '') <> 'Rejected'        
                    ORDER BY ActivityDate ASC;";

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
            try
            {
                string connString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                string fromPlace = "";
                string toPlace = "";
                var historyList = new List<object>();

                using (var con = new SqlConnection(connString))
                {
                    con.Open();

                    // 1. Fetch Travel 'From' and 'To' directly from Expenses
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

                    // 2. Fetch the actual KM Change History
                    string sqlHistory = @"SELECT HistoryId, ActivityId, OldKM, NewKM, ModifiedBy, ModifiedDateTime, Reason 
                    FROM dbo.SalesDailyActivityKMHistory WHERE ActivityId = @ActivityId ORDER BY ModifiedDateTime DESC";

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
    }
}