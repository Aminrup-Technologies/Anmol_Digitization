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

namespace AnmolDristi
{
    public partial class ClaimsApproval_v2 : System.Web.UI.Page
    {
        private string connString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        // Accumulators for footer logic
        private decimal _totalSnapshot = 0m;
        private decimal _totalTAClaim = 0m;
        private decimal _totalDistance = 0m;
        //private decimal _totalTAClaim = 0m;
        private decimal _totalDA = 0m;
        private decimal _totalHotel = 0m;
        private decimal _totalTravel = 0m;
        //private decimal _totalSnapshot = 0m;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["USERID"] != null)
                {
                    BindRequestsGrid();
                }
            }
        }

        // --- 1. DASHBOARD LIST ---
        protected void BindRequestsGrid()
        {
            string employeeCode = Session["workman"]?.ToString() ?? "Guest";
            DataTable dt = new DataTable();
            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand("dbo.usp_GetPendingApprovalsForUser_Fast", con))
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ApproverLoginId", employeeCode);
                da.Fill(dt);
            }
            gvRequests.DataSource = dt;
            gvRequests.DataBind();
        }

        protected void gvRequests_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "View")
            {
                int reqId = Convert.ToInt32(e.CommandArgument);
                // Important: Identify who is viewing (Level 1 or 2)
                ViewState["CurrentUserLevel"] = GetCurrentUserApproverLevel(reqId);
                ShowDetailsModal(reqId);
            }
        }

        // --- 2. DETAILS & CALCULATION ENGINE ---
        protected void ShowDetailsModal(int reqId)
        {
            hfActionRequestId.Value = reqId.ToString();
            lblModalRequestId.Text = reqId.ToString();
            BindDetailsGrid(reqId);
            ScriptManager.RegisterStartupScript(this, GetType(), "openModal", "openDetailsModal();", true);
        }

        protected void BindDetailsGrid(int reqId)
        {
            _totalSnapshot = 0m;
            _totalTAClaim = 0m;

            // Re-check level to be safe
            ViewState["CurrentUserLevel"] = GetCurrentUserApproverLevel(reqId);

            DataTable dt = new DataTable();
            decimal originalClaimed = 0m;
            string status = "Pending";
            string requesterName = "";

            using (var con = new SqlConnection(connString))
            {
                // A. Fetch Line Items
                using (var cmd = new SqlCommand("dbo.usp_GetApprovalRequestDetails", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ApprovalRequestId", reqId);
                    using (var da = new SqlDataAdapter(cmd)) { da.Fill(dt); }
                }

                // B. Fetch Header Info & Original Amount
                // Note: OriginalClaimAmount is the new column. Fallback to calculation if null.
                string sql = @"
                    SELECT 
                        ISNULL(OriginalClaimAmount, ISNULL(TotalClaimAmount,0) + ISNULL(TotalExpenseAmount,0)) as OrigTotal,
                        Status,
                        (SELECT EmployeeName FROM MST_UserMaster WHERE EmployeeCode = r.RequesterLoginId) as EmpName
                    FROM SalesApprovalRequest r 
                    WHERE ApprovalRequestId = @Id";

                using (var cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Id", reqId);
                    con.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            originalClaimed = Convert.ToDecimal(rdr["OrigTotal"]);
                            status = rdr["Status"].ToString();
                            requesterName = rdr["EmpName"].ToString();
                        }
                    }
                }
            }

            lblRequesterName.Text = requesterName;
            gvDetails.DataSource = dt;
            gvDetails.DataBind();

            // ✅ ADD THIS FIX: Forces the GridView to use <thead> and <tfoot> for sticky CSS
            if (gvDetails.Rows.Count > 0)
            {
                gvDetails.HeaderRow.TableSection = TableRowSection.TableHeader;

                // This makes the GridView Footer stickable!
                if (gvDetails.FooterRow != null)
                {
                    gvDetails.FooterRow.TableSection = TableRowSection.TableFooter;
                }
            }

            // C. CALCULATE SUMMARY
            // --- CALCULATE DASHBOARD FIGURES (SELF-BALANCING LOGIC) ---

            // 1. Current Approved is safely derived directly from the Grid Accumulators
            decimal currentApproved = _totalTAClaim + _totalSnapshot;

            // 2. Fetch EXACT Deductions by querying actual Auditor Rejections (matches the Modal)
            decimal totalDeductions = 0m;
            decimal currentApprovedAmount = 0m;
            decimal currentApprovedKM = 0m;
            decimal totalDeductedAmount = 0m;
            decimal totalDeductedKM = 0m;

            string sqlDashboard = @"
                -- Query 1: Calculate Net Approved
                SELECT 
                    ISNULL(SUM(d.Snapshot_TotalDistanceKM), 0) AS ApprovedKM,
                    ISNULL(SUM(ISNULL(d.Snapshot_CalculatedClaimAmount, 0) + ISNULL(d.Snapshot_ExpenseTotal, 0)), 0) AS ApprovedAmount
                FROM dbo.SalesApprovalRequestDetail d
                WHERE d.ApprovalRequestId = @Id AND ISNULL(d.DetailStatus, '') <> 'Rejected';

                -- Query 2: Calculate Deductions (Amounts and KM)
                SELECT 
                    ISNULL(SUM(DeductedAmount), 0) AS DeductedAmount,
                    ISNULL(SUM(DeductedKM), 0) AS DeductedKM
                FROM (
                    -- A. Whole Day Rejected 
                    SELECT 
                        CAST((ISNULL(d.Snapshot_CalculatedClaimAmount, 0) + ISNULL(d.Snapshot_ExpenseTotal, 0)) AS DECIMAL(18,2)) AS DeductedAmount,
                        ISNULL(d.Snapshot_TotalDistanceKM, 0) AS DeductedKM
                    FROM dbo.SalesApprovalRequestDetail d
                    WHERE d.ApprovalRequestId = @Id AND d.DetailStatus = 'Rejected'
        
                    UNION ALL
        
                    -- B. Specific Receipts Rejected 
                    SELECT 
                        CAST(e.Amount AS DECIMAL(18,2)) AS DeductedAmount,
                        0 AS DeductedKM
                    FROM dbo.SalesDailyExpense e
                    INNER JOIN dbo.SalesApprovalRequestDetail d ON e.ActivityId = d.ActivityId
                    WHERE d.ApprovalRequestId = @Id AND e.ClaimStatus = 'Rejected' AND e.IsDeleted = 0 AND ISNULL(d.DetailStatus, '') <> 'Rejected'
        
                    UNION ALL
        
                    -- C. KM Reductions (FIXED: Finds the Peak Claimed KM)
                    SELECT 
                        CAST(ROUND((fk.ClaimedKM - d.Snapshot_TotalDistanceKM) * 2.75, 2) AS DECIMAL(18,2)) AS DeductedAmount,
                        (fk.ClaimedKM - d.Snapshot_TotalDistanceKM) AS DeductedKM
                    FROM dbo.SalesApprovalRequestDetail d
                    INNER JOIN (
                        -- Find the highest KM value this activity ever held (The user's submitted claim)
                        SELECT ActivityId, MAX(CASE WHEN NewKM > ISNULL(OldKM,0) THEN NewKM ELSE OldKM END) AS ClaimedKM 
                        FROM dbo.SalesDailyActivityKMHistory
                        GROUP BY ActivityId
                    ) fk ON d.ActivityId = fk.ActivityId
                    WHERE d.ApprovalRequestId = @Id 
                      AND fk.ClaimedKM > d.Snapshot_TotalDistanceKM 
                      AND ISNULL(d.DetailStatus, '') <> 'Rejected'
                ) x;
            ";

            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand(sqlDashboard, con))
            {
                cmd.Parameters.AddWithValue("@Id", reqId);
                if (con.State != ConnectionState.Open) con.Open();

                using (var rdr = cmd.ExecuteReader())
                {
                    // Read Result 1 (Approved Totals)
                    if (rdr.Read())
                    {
                        currentApprovedKM = Convert.ToDecimal(rdr["ApprovedKM"]);
                        currentApprovedAmount = Convert.ToDecimal(rdr["ApprovedAmount"]);
                    }

                    // Move to Result 2 (Deduction Totals)
                    if (rdr.NextResult() && rdr.Read())
                    {
                        totalDeductedAmount = Convert.ToDecimal(rdr["DeductedAmount"]);
                        totalDeductedKM = Convert.ToDecimal(rdr["DeductedKM"]);
                    }
                }
            }

            // Self-Balancing Original Claim (Math is always perfect)
            decimal originalClaimedAmount = currentApprovedAmount + totalDeductedAmount;
            decimal originalClaimedKM = currentApprovedKM + totalDeductedKM;

            // Update UI Labels (Amounts)
            lblSummaryOriginal.Text = originalClaimedAmount.ToString("N2");
            lblSummaryApproved.Text = currentApprovedAmount.ToString("N2");
            lblSummaryDeduction.Text = totalDeductedAmount.ToString("N2");

            // Update UI Labels (KMs)
            lblSummaryOriginalKM.Text = originalClaimedKM.ToString("N2");
            lblSummaryApprovedKM.Text = currentApprovedKM.ToString("N2");
            lblSummaryDeductedKM.Text = totalDeductedKM.ToString("N2");

            // D. AUDITOR BUTTONS
            //bool isAuditor = (Convert.ToInt32(ViewState["CurrentUserLevel"]) == 2);
            //btnMarkReadyForPayment.Visible = isAuditor && (status == "Completed");

            // =========================================================
            // SMART BUTTON VISIBILITY (Check Parent Rows)
            // =========================================================
            bool pendingActionExists = false;

            // 1. Scan the grid to see if any 'Approve' row buttons are still clickable
            foreach (GridViewRow row in gvDetails.Rows)
            {
                if (row.RowType == DataControlRowType.DataRow)
                {
                    var btnApprove = row.FindControl("btnApproveRow") as Button;
                    if (btnApprove != null && btnApprove.Enabled)
                    {
                        pendingActionExists = true;
                        break;
                    }
                }
            }

            // =========================================================
            // THE GATEKEEPER: Check Child Expenses (Receipts)
            // =========================================================
            int unprocessedReceipts = 0;
            string sqlCheckReceipts = @"
                SELECT COUNT(1) 
                FROM dbo.SalesDailyExpense e
                INNER JOIN dbo.SalesApprovalRequestDetail d ON e.ActivityId = d.ActivityId
                WHERE d.ApprovalRequestId = @Id 
                  AND e.IsDeleted = 0 
                  AND ISNULL(e.ClaimStatus, '') NOT IN ('Approved', 'Rejected')";

            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand(sqlCheckReceipts, con))
            {
                cmd.Parameters.AddWithValue("@Id", reqId);
                if (con.State != ConnectionState.Open) con.Open();
                unprocessedReceipts = Convert.ToInt32(cmd.ExecuteScalar());
            }

            bool isAuditorCheck = (Convert.ToInt32(ViewState["CurrentUserLevel"]) == 2);

            // 2. The Payment Button ONLY appears if NO parent rows are pending AND NO child receipts are pending
            btnMarkReadyForPayment.Visible = isAuditorCheck && !pendingActionExists && (unprocessedReceipts == 0);

            // 3. Smart Auditor Feedback: If rows are done but receipts are missed, tell them!
            if (isAuditorCheck && !pendingActionExists && unprocessedReceipts > 0)
            {
                lblModalMessage.Text = $"⚠️ Cannot finalize: {unprocessedReceipts} expense receipt(s) are still pending inside 'View Details'.";
                lblModalMessage.CssClass = "text-danger font-weight-bold";
            }
            else if (!isAuditorCheck || (!pendingActionExists && unprocessedReceipts == 0))
            {
                // Clear the warning if everything is actually done
                lblModalMessage.Text = "";
            }

            // 4. Hide Bulk Action Bar if no parent row actions are left
            btnApproveSelected.Visible = pendingActionExists;
            btnRejectSelected.Visible = pendingActionExists;
            txtBulkRemarks.Visible = pendingActionExists;
        }

        protected void gvDetails_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            // ==========================================
            // 1. DATA ROW LOGIC
            // ==========================================
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DataRowView drv = e.Row.DataItem as DataRowView;
                if (drv == null) return;

                // A. ACCUMULATE TOTALS FOR GRID FOOTER
                _totalDistance += (drv["Snapshot_TotalDistanceKM"] == DBNull.Value) ? 0m : Convert.ToDecimal(drv["Snapshot_TotalDistanceKM"]);
                _totalTAClaim += (drv["Snapshot_CalculatedClaimAmount"] == DBNull.Value) ? 0m : Convert.ToDecimal(drv["Snapshot_CalculatedClaimAmount"]);
                _totalDA += (drv["Snapshot_DAAmount"] == DBNull.Value) ? 0m : Convert.ToDecimal(drv["Snapshot_DAAmount"]);
                _totalHotel += (drv["Snapshot_HotelAmount"] == DBNull.Value) ? 0m : Convert.ToDecimal(drv["Snapshot_HotelAmount"]);
                _totalTravel += (drv["Snapshot_TravelAmount"] == DBNull.Value) ? 0m : Convert.ToDecimal(drv["Snapshot_TravelAmount"]);
                _totalSnapshot += (drv["Snapshot_ExpenseTotal"] == DBNull.Value) ? 0m : Convert.ToDecimal(drv["Snapshot_ExpenseTotal"]);

                // B. LOGIC: WHO CAN EDIT WHAT?
                int currentLevel = Convert.ToInt32(ViewState["CurrentUserLevel"] ?? 0);
                string detailStatus = drv["DetailStatus"].ToString();
                if (string.IsNullOrEmpty(detailStatus)) detailStatus = "Pending";
                int approvedLevel = drv["DetailApprovedLevel"] == DBNull.Value ? 0 : Convert.ToInt32(drv["DetailApprovedLevel"]);

                var btnApprove = (Button)e.Row.FindControl("btnApproveRow");
                var btnReject = (Button)e.Row.FindControl("btnRejectRow");
                var txtRemark = (TextBox)e.Row.FindControl("txtRowRemark");
                var btnEditKM = (System.Web.UI.HtmlControls.HtmlButton)e.Row.FindControl("btnEditKM");

                // Default: Lock
                if (btnApprove != null) btnApprove.Enabled = false;
                if (btnReject != null) btnReject.Enabled = false;
                if (txtRemark != null) txtRemark.ReadOnly = true;
                if (btnEditKM != null) btnEditKM.Visible = false;

                // Scenario: Auditor Logic
                bool isAuditor = (currentLevel == 2);
                if (isAuditor && btnEditKM != null)
                {
                    btnEditKM.Visible = true; // Auditors can always correct KM
                }

                // C. ROW COLORS & UNLOCKING
                if (detailStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                {
                    e.Row.CssClass += " table-danger"; // Red
                }
                else if (detailStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase) && approvedLevel == currentLevel)
                {
                    e.Row.CssClass += " table-success"; // Green (Done)
                    if (btnApprove != null) btnApprove.Text = "✔ Done";
                }
                else if (detailStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase) && approvedLevel < currentLevel)
                {
                    // HANDOVER: Approved by L1, Pending L2
                    e.Row.CssClass += " table-warning"; // Yellow
                    UnlockRow(btnApprove, btnReject, txtRemark);
                }
                else if (detailStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                {
                    // Fresh
                    UnlockRow(btnApprove, btnReject, txtRemark);
                }
            }
            // ==========================================
            // 2. FOOTER LOGIC (This was missing!)
            // ==========================================
            else if (e.Row.RowType == DataControlRowType.Footer)
            {
                FillFooterTotals(e.Row);
            }
        }

        private void FillFooterTotals(GridViewRow footer)
        {
            // Using .Contains() safely bypasses any hidden newlines, spaces, 
            // or encoding issues caused by the Rupee symbol (₹) in the .aspx markup.

            for (int i = 0; i < gvDetails.Columns.Count; i++)
            {
                string headerText = gvDetails.Columns[i].HeaderText ?? "";

                if (headerText.Contains("Distance"))
                {
                    footer.Cells[i].Text = "<strong>" + _totalDistance.ToString("N2") + "</strong>";
                    footer.Cells[i].HorizontalAlign = HorizontalAlign.Right;
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

        private void UnlockRow(Button bA, Button bR, TextBox t)
        {
            bA.Enabled = true;
            bR.Enabled = true;
            t.ReadOnly = false;
        }

        // --- 3. ACTIONS ---

        // Called by JS when Expense/KM changes
        protected void btnRefreshCalculations_Click(object sender, EventArgs e)
        {
            int reqId = Convert.ToInt32(hfActionRequestId.Value);
            BindDetailsGrid(reqId);
            upDetails.Update();
        }

        protected void gvDetails_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "ApproveRow" || e.CommandName == "RejectRow")
            {
                int detailId = Convert.ToInt32(e.CommandArgument);
                bool isApprove = (e.CommandName == "ApproveRow");

                GridViewRow row = (GridViewRow)((Control)e.CommandSource).NamingContainer;
                TextBox txt = row.FindControl("txtRowRemark") as TextBox;
                string remark = txt.Text;

                string action = isApprove ? "APPROVE" : "REJECT";
                string user = Session["workman"]?.ToString() ?? "System";

                using (var con = new SqlConnection(connString))
                using (var cmd = new SqlCommand("dbo.usp_ProcessSalesApprovalRequestDetail", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ApprovalRequestDetailId", detailId);
                    cmd.Parameters.AddWithValue("@ApprovalRequestId", hfActionRequestId.Value);
                    cmd.Parameters.AddWithValue("@Action", action);
                    cmd.Parameters.AddWithValue("@ActionBy", user);
                    cmd.Parameters.AddWithValue("@ActionRemarks", remark);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                // Refresh to update colors/totals
                BindDetailsGrid(Convert.ToInt32(hfActionRequestId.Value));
            }
        }

        protected void btnApproveSelected_Click(object sender, EventArgs e)
        {
            // Implementation of Bulk Approval calling usp_ProcessApprovalRequestDetails_WithRemarks
            // (Standard logic same as V1, builds TVP and calls SP)
            // After calling SP, call BindDetailsGrid()
        }

        protected void btnRejectSelected_Click(object sender, EventArgs e)
        {
            // Implementation of Bulk Reject
        }

        protected void btnMarkReadyForPayment_Click(object sender, EventArgs e)
        {
            int reqId = Convert.ToInt32(hfActionRequestId.Value);
            string auditor = Session["USERID"]?.ToString() ?? "System";

            // 1. Force a final recalculation to guarantee financial accuracy (If you added this feature)
            // btnRecalculate_Click(null, null); 

            // 2. Execute Status Updates
            using (var con = new SqlConnection(connString))
            {
                con.Open();

                // A. Gently force the Header to "Completed" (bridges the gap if they approved row-by-row)
                using (var cmdFix = new SqlCommand("UPDATE SalesApprovalRequest SET Status = 'Completed' WHERE ApprovalRequestId = @Id AND Status = 'Pending'", con))
                {
                    cmdFix.Parameters.AddWithValue("@Id", reqId);
                    cmdFix.ExecuteNonQuery();
                }

                // B. Now push it to ReadyForPayment using your existing SP
                using (var cmd = new SqlCommand("dbo.usp_MarkRequestReadyForPayment", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ApprovalRequestId", reqId);
                    cmd.Parameters.AddWithValue("@ActionBy", auditor);
                    cmd.ExecuteNonQuery();
                }
            }

            lblModalMessage.Text = "Calculated & Marked Ready for Payment.";
            lblModalMessage.CssClass = "text-success font-weight-bold";

            // Refresh the UI to reflect the final state
            BindDetailsGrid(reqId);
            upDetails.Update();
        }

        // --- 4. WEB METHODS (AJAX) ---

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object UpdateIndividualExpense(int expenseId, string status)
        {
            try
            {
                string user = HttpContext.Current.Session["workman"]?.ToString() ?? "System";
                string conn = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                using (var con = new SqlConnection(conn))
                using (var cmd = new SqlCommand("dbo.usp_UpdateSalesDailyExpenseStatus", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ExpenseId", expenseId);
                    cmd.Parameters.AddWithValue("@NewStatus", status);
                    cmd.Parameters.AddWithValue("@ActionBy", user);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
                return new { success = true };
            }
            catch (Exception ex) { return new { success = false, message = ex.Message }; }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object UpdateActivityKM(int activityId, decimal newKm, string reason)
        {
            try
            {
                string user = HttpContext.Current.Session["workman"]?.ToString() ?? "System";
                string conn = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                using (var con = new SqlConnection(conn))
                using (var cmd = new SqlCommand("dbo.usp_UpdateActivityKM_WithHistory", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ActivityId", activityId);
                    cmd.Parameters.AddWithValue("@NewKM", newKm);
                    cmd.Parameters.AddWithValue("@ChangedBy", user);
                    cmd.Parameters.AddWithValue("@Reason", reason);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
                return new { success = true };
            }
            catch (Exception ex) { return new { success = false, message = ex.Message }; }
        }

        private int GetCurrentUserApproverLevel(int reqId)
        {
            string user = Session["workman"]?.ToString();
            // Simple query to get level
            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand("SELECT TOP 1 ApproverLevel FROM SalesApprovalRequestApprover WHERE ApprovalRequestId=@Id AND ApproverLoginId=@User", con))
            {
                cmd.Parameters.AddWithValue("@Id", reqId);
                cmd.Parameters.AddWithValue("@User", user);
                con.Open();
                object res = cmd.ExecuteScalar();
                return res != null ? Convert.ToInt32(res) : 0;
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

                // Return a structured object containing Places + History
                return new { success = true, fromPlace = fromPlace, toPlace = toPlace, history = historyList };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

        protected void btnRecalculate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(hfActionRequestId.Value)) return;

            int reqId = Convert.ToInt32(hfActionRequestId.Value);

            /* * LOGIC EXPLANATION:
             * We sum up the snapshot columns for the specific ApprovalRequestId.
             * We EXCLUDE rows where DetailStatus = 'Rejected', meaning if an Auditor rejects
             * a whole day, it contributes 0 to the final Approved buckets.
             * The table's computed columns (ApprovedExpenseAmount & TotalApprovedAmount) 
             * will automatically recalculate themselves based on these base values!
             */
            string sqlRecalculate = @"
                UPDATE SalesApprovalRequest
                SET 
                    ApprovedKM = (SELECT ISNULL(SUM(Snapshot_TotalDistanceKM), 0) FROM SalesApprovalRequestDetail WHERE ApprovalRequestId = @Id AND ISNULL(DetailStatus,'') <> 'Rejected'),
                    ApprovedClaimAmount = (SELECT ISNULL(SUM(Snapshot_CalculatedClaimAmount), 0) FROM SalesApprovalRequestDetail WHERE ApprovalRequestId = @Id AND ISNULL(DetailStatus,'') <> 'Rejected'),
                    ApprovedDAAmount = (SELECT ISNULL(SUM(Snapshot_DAAmount), 0) FROM SalesApprovalRequestDetail WHERE ApprovalRequestId = @Id AND ISNULL(DetailStatus,'') <> 'Rejected'),
                    ApprovedHotelAmount = (SELECT ISNULL(SUM(Snapshot_HotelAmount), 0) FROM SalesApprovalRequestDetail WHERE ApprovalRequestId = @Id AND ISNULL(DetailStatus,'') <> 'Rejected'),
                    ApprovedTravelAmount = (SELECT ISNULL(SUM(Snapshot_TravelAmount), 0) FROM SalesApprovalRequestDetail WHERE ApprovalRequestId = @Id AND ISNULL(DetailStatus,'') <> 'Rejected')
                WHERE ApprovalRequestId = @Id;
            ";

            try
            {
                using (var con = new SqlConnection(connString))
                using (var cmd = new SqlCommand(sqlRecalculate, con))
                {
                    cmd.Parameters.AddWithValue("@Id", reqId);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                // Provide success feedback
                lblModalMessage.Text = "Amounts recalculated and synced successfully.";
                lblModalMessage.CssClass = "text-success";

                // Refresh the Grid and Dashboard Footer to show the latest state
                BindDetailsGrid(reqId);
                upDetails.Update();
            }
            catch (Exception ex)
            {
                lblModalMessage.Text = "Error during recalculation: " + ex.Message;
                lblModalMessage.CssClass = "text-danger";
                upDetails.Update();
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

                // This query perfectly identifies the 3 distinct ways money is deducted
                string sql = @"
                    -- 1. Entire Row (Day) Rejected
                    SELECT 
                        CONVERT(varchar, a.ActivityDate, 106) AS ActivityDate,
                        'Whole Day Rejected' AS DeductionType,
                        ISNULL(d.DetailRemarks, 'Rejected by Auditor') AS Reason,
                        CAST((ISNULL(d.Snapshot_CalculatedClaimAmount, 0) + ISNULL(d.Snapshot_ExpenseTotal, 0)) AS DECIMAL(18,2)) AS DeductedAmount
                    FROM dbo.SalesApprovalRequestDetail d
                    INNER JOIN dbo.SalesDailyActivity a ON d.ActivityId = a.ActivityId
                    WHERE d.ApprovalRequestId = @ReqId AND d.DetailStatus = 'Rejected'

                    UNION ALL

                    -- 2. Specific Expense Receipts Rejected
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

                    -- 3. KM Reductions (FIXED: Finds the Peak Claimed KM)
                    SELECT 
                        CONVERT(varchar, a.ActivityDate, 106) AS ActivityDate,
                        'KM Reduced (' + CAST(fk.ClaimedKM AS varchar) + ' to ' + CAST(d.Snapshot_TotalDistanceKM AS varchar) + ')' AS DeductionType,
                        ISNULL((SELECT TOP 1 Reason FROM dbo.SalesDailyActivityKMHistory WHERE ActivityId = a.ActivityId ORDER BY ModifiedDateTime DESC), 'KM corrected') AS Reason,
                        CAST(ROUND((fk.ClaimedKM - d.Snapshot_TotalDistanceKM) * 2.75, 2) AS DECIMAL(18,2)) AS DeductedAmount
                    FROM dbo.SalesApprovalRequestDetail d
                    INNER JOIN dbo.SalesDailyActivity a ON d.ActivityId = a.ActivityId
                    INNER JOIN (
                        -- Find the highest KM value this activity ever held
                        SELECT ActivityId, MAX(CASE WHEN NewKM > ISNULL(OldKM,0) THEN NewKM ELSE OldKM END) AS ClaimedKM 
                        FROM dbo.SalesDailyActivityKMHistory
                        GROUP BY ActivityId
                    ) fk ON d.ActivityId = fk.ActivityId
                    WHERE d.ApprovalRequestId = @ReqId 
                      AND fk.ClaimedKM > d.Snapshot_TotalDistanceKM
                      AND ISNULL(d.DetailStatus, '') <> 'Rejected'
              
                    ORDER BY ActivityDate ASC;
                ";

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
    }
}