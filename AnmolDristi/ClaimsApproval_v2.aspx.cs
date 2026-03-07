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

            // NEW: Push the fresh data to the background UI!
            if (upMainGrid != null) upMainGrid.Update();
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

            // NEW: Explicitly tell the Modal's UpdatePanel to refresh its HTML!
            if (upDetails != null) upDetails.Update();

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

            // Re-check level to be safe
            ViewState["CurrentUserLevel"] = GetCurrentUserApproverLevel(reqId);

            DataTable dt = new DataTable();
            decimal originalClaimed = 0m;
            string status = "Pending";
            string requesterName = "";
            string requesterCode = ""; // NEW: Variable for Employee Code

            using (var con = new SqlConnection(connString))
            {
                // A. Fetch Line Items
                using (var cmd = new SqlCommand("dbo.usp_GetApprovalRequestDetails", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ApprovalRequestId", reqId);
                    using (var da = new SqlDataAdapter(cmd)) { da.Fill(dt); }
                }

                // B. Fetch Header Info & Original Amount (FIXED: Added RequesterLoginId)
                string sql = @"
                    SELECT 
                        ISNULL(OriginalClaimAmount, ISNULL(TotalClaimAmount,0) + ISNULL(TotalExpenseAmount,0)) as OrigTotal,
                        Status,
                        RequesterLoginId,
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
                            requesterCode = rdr["RequesterLoginId"].ToString(); // NEW: Grabbing the code
                        }
                    }
                }
            }

            // Populate Modal Header Labels beautifully
            lblModalRequestId.Text = reqId.ToString();
            lblRequesterName.Text = $"<i class='fa fa-user mr-1'></i> {requesterName} ({requesterCode})";

            gvDetails.DataSource = dt;
            gvDetails.DataBind();

            // ADD THIS FIX: Forces the GridView to use <thead> and <tfoot> for sticky CSS
            if (gvDetails.Rows.Count > 0)
            {
                gvDetails.HeaderRow.TableSection = TableRowSection.TableHeader;
                if (gvDetails.FooterRow != null)
                {
                    gvDetails.FooterRow.TableSection = TableRowSection.TableFooter;
                }
            }

            // C. CALCULATE SUMMARY
            // 1. Current Approved is safely derived directly from the Grid Accumulators
            decimal currentApproved = _totalTAClaim + _totalSnapshot;

            // 2. Fetch EXACT Deductions by querying actual Auditor Rejections
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
        
                    -- C. KM Reductions (Finds the Peak Claimed KM)
                    SELECT 
                        CAST(ROUND((fk.ClaimedKM - d.Snapshot_TotalDistanceKM) * 2.75, 2) AS DECIMAL(18,2)) AS DeductedAmount,
                        (fk.ClaimedKM - d.Snapshot_TotalDistanceKM) AS DeductedKM
                    FROM dbo.SalesApprovalRequestDetail d
                    INNER JOIN (
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
                    // Read Result 1
                    if (rdr.Read())
                    {
                        currentApprovedKM = Convert.ToDecimal(rdr["ApprovedKM"]);
                        currentApprovedAmount = Convert.ToDecimal(rdr["ApprovedAmount"]);
                    }
                    // Read Result 2
                    if (rdr.NextResult() && rdr.Read())
                    {
                        totalDeductedAmount = Convert.ToDecimal(rdr["DeductedAmount"]);
                        totalDeductedKM = Convert.ToDecimal(rdr["DeductedKM"]);
                    }
                }
            }

            // Self-Balancing Original Claim
            decimal originalClaimedAmount = currentApprovedAmount + totalDeductedAmount;
            decimal originalClaimedKM = currentApprovedKM + totalDeductedKM;

            // Update UI Labels
            lblSummaryOriginal.Text = originalClaimedAmount.ToString("N2");
            lblSummaryApproved.Text = currentApprovedAmount.ToString("N2");
            lblSummaryDeduction.Text = totalDeductedAmount.ToString("N2");
            lblSummaryOriginalKM.Text = originalClaimedKM.ToString("N2");
            lblSummaryApprovedKM.Text = currentApprovedKM.ToString("N2");
            lblSummaryDeductedKM.Text = totalDeductedKM.ToString("N2");

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

            // =========================================================
            // SMART BUTTON VISIBILITY & WORKFLOW ROUTING
            // =========================================================
            int currentLvl = Convert.ToInt32(ViewState["CurrentUserLevel"] ?? 0);
            bool isLevel1 = (currentLvl == 1);
            bool isAuditorCheck = (currentLvl == 2);

            // ADD THIS NEW LINE: Pass the exact level to JavaScript safely!
            hfCurrentUserLevel.Value = currentLvl.ToString();

            // 1. Level 1 gets the Green Forward Button (Requires BOTH Day rows AND Child Receipts to be cleared!)
            btnForwardLevel.Visible = isLevel1 && !pendingActionExists && (unprocessedReceipts == 0);

            // 2. Auditor gets the Yellow Payment Button (Requires BOTH Day rows AND Child Receipts to be cleared)
            btnMarkReadyForPayment.Visible = isAuditorCheck && !pendingActionExists && (unprocessedReceipts == 0);

            // 3. Return to sender visible to any active approver
            btnReturnToEmployee.Visible = (isLevel1 || isAuditorCheck);

            // 4. Smart User Feedback Messaging (Now universally warns BOTH levels about missing receipts)
            if (!pendingActionExists && unprocessedReceipts > 0)
            {
                lblModalMessage.Text = $"⚠️ Cannot finalize: {unprocessedReceipts} expense receipt(s) are still pending inside 'View Details'.";
                lblModalMessage.CssClass = "text-danger font-weight-bold";
            }
            else if (pendingActionExists)
            {
                lblModalMessage.Text = "Please action all day rows to proceed.";
                lblModalMessage.CssClass = "text-warning font-weight-bold";
            }
            else
            {
                lblModalMessage.Text = "";
            }

            // Hide Bulk Action Bar if no parent row actions are left
            btnApproveSelected.Visible = pendingActionExists;
            btnRejectSelected.Visible = pendingActionExists;
            txtBulkRemarks.Visible = pendingActionExists;
        }

        protected void btnForwardLevel_Click(object sender, EventArgs e)
        {
            try
            {
                int reqId = Convert.ToInt32(hfActionRequestId.Value);
                string user = GetLoggedInEmployeeCode();
                int currentLevel = Convert.ToInt32(ViewState["CurrentUserLevel"] ?? 1);

                // 1. Force recalculation to lock in their math before forwarding
                ExecuteRecalculationSQL(reqId);

                using (var con = new SqlConnection(connString))
                {
                    con.Open();

                    // 2. The Handover SQL: Mark Level 1 as Approved AND advance the Master Header to Level 2
                    string sqlWorkflow = @"
                        -- A. Close out the current user's workflow step
                        UPDATE dbo.SalesApprovalRequestApprover 
                        SET Status = 'Approved', ActionBy = @User, ActionDate = SYSUTCDATETIME() 
                        WHERE ApprovalRequestId = @Id AND ApproverLevel = @Level AND Status = 'Pending';

                        -- B. Find the NEXT person in line (The Auditor)
                        DECLARE @NextLevel INT, @NextLogin NVARCHAR(100), @NextRole NVARCHAR(100);
                        
                        SELECT TOP 1 
                            @NextLevel = ApproverLevel, 
                            @NextLogin = ApproverLoginId, 
                            @NextRole = ApproverRole
                        FROM dbo.SalesApprovalRequestApprover
                        WHERE ApprovalRequestId = @Id AND ApproverLevel > @Level AND Status = 'Pending'
                        ORDER BY ApproverLevel ASC;

                        -- C. Advance the Master Header so it appears on the Auditor's Dashboard
                        IF @NextLevel IS NOT NULL
                        BEGIN
                            UPDATE dbo.SalesApprovalRequest
                            SET 
                                CurrentApproverLevel = @NextLevel,
                                CurrentApproverLoginId = @NextLogin,
                                ModifiedBy = @User,
                                ModifiedOn = SYSUTCDATETIME()
                            WHERE ApprovalRequestId = @Id;

                            -- Log the exact handover
                            INSERT INTO dbo.ApprovalRequestStatusHistory 
                            (ApprovalRequestId, OldStatus, NewStatus, ChangedBy, Remarks)
                            VALUES 
                            (@Id, 'Pending', 'Pending', @User, CONCAT('Level ', @Level, ' approved. Forwarded to ', @NextRole, ' (', ISNULL(@NextLogin, 'Audit Pool'), ')'));
                        END
                        ELSE
                        BEGIN
                            -- Fallback: If no Auditor exists, safely mark the whole claim as Completed
                            UPDATE dbo.SalesApprovalRequest
                            SET Status = 'Completed', ModifiedBy = @User, ModifiedOn = SYSUTCDATETIME()
                            WHERE ApprovalRequestId = @Id AND Status = 'Pending';

                            INSERT INTO dbo.ApprovalRequestStatusHistory 
                            (ApprovalRequestId, OldStatus, NewStatus, ChangedBy, Remarks)
                            VALUES 
                            (@Id, 'Pending', 'Completed', @User, 'All approval levels finished. Awaiting Payment.');
                        END";

                    using (var cmd = new SqlCommand(sqlWorkflow, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", reqId);
                        cmd.Parameters.AddWithValue("@User", user);
                        cmd.Parameters.AddWithValue("@Level", currentLevel);
                        cmd.ExecuteNonQuery();
                    }
                }

                // 3. Refresh the main background dashboard so the request disappears from Tanmoy's queue
                BindRequestsGrid();

                // 4. Close the modal seamlessly
                ScriptManager.RegisterStartupScript(this, GetType(), "closeModal", "$('#detailsModal').modal('hide');", true);
            }
            catch (Exception ex)
            {
                lblModalMessage.Text = "Error during handover: " + ex.Message;
                lblModalMessage.CssClass = "text-danger font-weight-bold";
                upDetails.Update();
            }
        }

        // NEW: Global Return Logic
        protected void btnReturnToEmployee_Click(object sender, EventArgs e)
        {
            try
            {
                int reqId = Convert.ToInt32(hfActionRequestId.Value);
                string user = GetLoggedInEmployeeCode();
                string reason = hfReturnReason.Value.Trim(); // Safely fetch the JS prompt input

                if (string.IsNullOrEmpty(reason))
                {
                    lblModalMessage.Text = "A return reason is mandatory.";
                    lblModalMessage.CssClass = "text-danger font-weight-bold";
                    upDetails.Update();
                    return;
                }

                using (var con = new SqlConnection(connString))
                using (var cmd = new SqlCommand("dbo.usp_ReturnClaimToEmployee", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ApprovalRequestId", reqId);
                    cmd.Parameters.AddWithValue("@ActionBy", user);
                    cmd.Parameters.AddWithValue("@ReturnReason", reason);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                hfReturnReason.Value = ""; // Clear state securely

                BindRequestsGrid();
                ScriptManager.RegisterStartupScript(this, GetType(), "closeModal", "$('#detailsModal').modal('hide');", true);
            }
            catch (Exception ex)
            {
                lblModalMessage.Text = "Error returning claim: " + ex.Message;
                lblModalMessage.CssClass = "text-danger font-weight-bold";
                upDetails.Update();
            }
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

                // C. LOGIC: WHO CAN EDIT WHAT?
                int currentLevel = Convert.ToInt32(ViewState["CurrentUserLevel"] ?? 0);
                string detailStatus = drv["DetailStatus"].ToString();
                if (string.IsNullOrEmpty(detailStatus)) detailStatus = "Pending";
                int approvedLevel = drv["DetailApprovedLevel"] == DBNull.Value ? 0 : Convert.ToInt32(drv["DetailApprovedLevel"]);

                var btnApprove = (Button)e.Row.FindControl("btnApproveRow");
                var btnReject = (Button)e.Row.FindControl("btnRejectRow");
                var txtRemark = (TextBox)e.Row.FindControl("txtRowRemark");

                // Default: Lock parent row actions
                if (btnApprove != null) btnApprove.Enabled = false;
                if (btnReject != null) btnReject.Enabled = false;
                if (txtRemark != null) txtRemark.ReadOnly = true;

                // D. ROW COLORS & UNLOCKING
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

                // E. VISUAL GATEKEEPER FLAG (Highlight Pending Receipts)
                string expensesJson = drv["ExpensesJson"] != DBNull.Value ? drv["ExpensesJson"].ToString() : "";
                bool hasPendingReceipts = expensesJson.Contains("\"ClaimStatus\":\"Submitted\"") ||
                                          expensesJson.Contains("\"ClaimStatus\":\"Draft\"") ||
                                          expensesJson.Contains("\"ClaimStatus\":\"Pending\"");

                // Re-declare isAuditor to run the check below
                bool isAuditor = (currentLevel == 2);

                // If Auditor is viewing, and row has pending receipts, AND row isn't rejected
                if (isAuditor && hasPendingReceipts && !detailStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                {
                    var btnExp = e.Row.FindControl("btnExp") as System.Web.UI.HtmlControls.HtmlButton;
                    if (btnExp != null)
                    {
                        // FIXED: Changed 'btn-expenses' to 'btn-day-action shadow-sm' so the unified modal still opens!
                        btnExp.Attributes["class"] = "btn btn-sm btn-danger text-white btn-day-action pulse-warning shadow-sm";
                        btnExp.InnerText = "⚠️ Action";
                        btnExp.Attributes["title"] = "Pending receipts require your approval or rejection!";
                    }
                }
            }
            // ==========================================
            // 2. FOOTER LOGIC
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

            ExecuteRecalculationSQL(reqId);
            BindDetailsGrid(reqId);
            upDetails.Update();

            BindRequestsGrid(); // <-- ADD THIS: Syncs background grid when receipt modal closes
        }

        protected void gvDetails_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "ApproveRow" || e.CommandName == "RejectRow")
            {
                int detailId = Convert.ToInt32(e.CommandArgument);
                bool isApprove = (e.CommandName == "ApproveRow");

                GridViewRow row = (GridViewRow)((Control)e.CommandSource).NamingContainer;
                TextBox txt = row.FindControl("txtRowRemark") as TextBox;
                string remark = txt != null ? txt.Text : "";

                string action = isApprove ? "APPROVE" : "REJECT";
                string user = GetLoggedInEmployeeCode(); // Standardized to your identity method

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

                // 1. CRITICAL: Force recalculation so the Dashboard Amounts instantly react to the approval/rejection!
                int reqId = Convert.ToInt32(hfActionRequestId.Value);
                ExecuteRecalculationSQL(reqId);

                // 2. Refresh the Grid (This also runs the Gatekeeper check for the "Ready for Payment" button)
                BindDetailsGrid(Convert.ToInt32(hfActionRequestId.Value));

                // 3. Tell the UI to paint the changes
                upDetails.Update();

                BindRequestsGrid();
            }
        }

        protected void btnApproveSelected_Click(object sender, EventArgs e)
        {
            ProcessBulkAction("Approved");
        }

        protected void btnRejectSelected_Click(object sender, EventArgs e)
        {
            ProcessBulkAction("Rejected");
        }

        // =========================================================
        // UNIFIED BULK ACTION PROCESSOR
        // =========================================================
        private void ProcessBulkAction(string newStatus)
        {
            try
            {
                if (string.IsNullOrEmpty(hfActionRequestId.Value)) return;

                int reqId = Convert.ToInt32(hfActionRequestId.Value);
                string auditor = GetLoggedInEmployeeCode(); // Rule 1: Safe Identity
                int currentLevel = Convert.ToInt32(ViewState["CurrentUserLevel"] ?? 0);
                string remarks = txtBulkRemarks.Text.Trim();

                int processedCount = 0;

                using (var con = new SqlConnection(connString))
                {
                    con.Open();

                    // Loop through the grid to find checked rows
                    foreach (GridViewRow row in gvDetails.Rows)
                    {
                        if (row.RowType == DataControlRowType.DataRow)
                        {
                            // Update these IDs if your ASPX uses different names for the Checkbox/HiddenField
                            var chk = row.FindControl("chkSelect") as CheckBox;
                            var hfActivityId = row.FindControl("hfActivityId") as HiddenField;

                            if (chk != null && chk.Checked && hfActivityId != null && !string.IsNullOrEmpty(hfActivityId.Value))
                            {
                                int activityId = Convert.ToInt32(hfActivityId.Value);

                                string sql = @"
                            UPDATE dbo.SalesApprovalRequestDetail 
                            SET DetailStatus = @Status, 
                                DetailRemarks = @Remarks, 
                                DetailApprovedBy = @ActionBy, 
                                DetailApprovedOn = GETDATE(),
                                DetailApprovedLevel = @Level
                            WHERE ActivityId = @ActivityId AND ApprovalRequestId = @ReqId";

                                using (var cmd = new SqlCommand(sql, con))
                                {
                                    cmd.Parameters.AddWithValue("@Status", newStatus);
                                    cmd.Parameters.AddWithValue("@Remarks", string.IsNullOrEmpty(remarks) ? (object)DBNull.Value : remarks);
                                    cmd.Parameters.AddWithValue("@ActionBy", auditor);
                                    cmd.Parameters.AddWithValue("@Level", currentLevel);
                                    cmd.Parameters.AddWithValue("@ActivityId", activityId);
                                    cmd.Parameters.AddWithValue("@ReqId", reqId);

                                    cmd.ExecuteNonQuery();
                                    processedCount++;
                                }
                            }
                        }
                    }
                }

                if (processedCount > 0)
                {
                    // Rule 2: Force recalculation so the Dashboard Footers jump instantly!
                    ExecuteRecalculationSQL(reqId);

                    // Clear the remarks box so it doesn't accidentally apply to the next batch
                    txtBulkRemarks.Text = "";

                    lblModalMessage.Text = $"{processedCount} row(s) successfully marked as {newStatus}.";
                    lblModalMessage.CssClass = newStatus == "Approved" ? "text-success font-weight-bold" : "text-danger font-weight-bold";
                }
                else
                {
                    lblModalMessage.Text = "Please select at least one row to apply the bulk action.";
                    lblModalMessage.CssClass = "text-warning font-weight-bold";
                }

                // Rule 3: Bind Details Grid will automatically run the Gatekeeper Logic 
                // to check if the 'Mark Ready for Payment' button should appear!
                BindDetailsGrid(reqId);
                upDetails.Update();

                BindRequestsGrid(); // <-- ADD THIS
            }
            catch (Exception ex)
            {
                lblModalMessage.Text = "Error processing bulk action: " + ex.Message;
                lblModalMessage.CssClass = "text-danger font-weight-bold";
                upDetails.Update();
            }
        }

        private string GetLoggedInEmployeeCode()
        {
            // Example: from Session or User.Identity
            return Session["WORKMAN"] as string;
            // return HttpContext.Current.User?.Identity?.Name ?? "EMP001";
        }

        protected void btnMarkReadyForPayment_Click(object sender, EventArgs e)
        {
            try
            {
                int reqId = Convert.ToInt32(hfActionRequestId.Value);
                string auditor = GetLoggedInEmployeeCode();

                // 1. Force a final recalculation to guarantee financial accuracy
                ExecuteRecalculationSQL(reqId);

                // 2. Execute Status Updates
                using (var con = new SqlConnection(connString))
                {
                    con.Open();

                    // A. Force the Header to "Completed" AND log it in the Status History table
                    string fixStatusSql = @"
                        UPDATE dbo.SalesApprovalRequest 
                        SET Status = 'Completed' 
                        WHERE ApprovalRequestId = @Id AND Status = 'Pending';

                        IF @@ROWCOUNT > 0
                        BEGIN
                            INSERT INTO dbo.ApprovalRequestStatusHistory 
                            (ApprovalRequestId, OldStatus, NewStatus, ChangedBy, Remarks)
                            VALUES 
                            (@Id, 'Pending', 'Completed', @AuditorId, 'Auditor finalized all rows and synced calculations.');
                        END";

                    using (var cmdFix = new SqlCommand(fixStatusSql, con))
                    {
                        cmdFix.Parameters.AddWithValue("@Id", reqId);
                        cmdFix.Parameters.AddWithValue("@AuditorId", auditor);
                        cmdFix.ExecuteNonQuery();
                    }

                    // B. NEW: Officially mark the Workflow Routing Table as Approved!
                    string sqlWorkflow = @"
                        UPDATE dbo.SalesApprovalRequestApprover 
                        SET 
                            Status = 'Approved', 
                            ActionBy = @AuditorId, 
                            ActionDate = GETDATE() 
                        WHERE ApprovalRequestId = @Id 
                          AND ApproverLevel = 2 
                          AND Status = 'Pending'";

                    using (var cmdWorkflow = new SqlCommand(sqlWorkflow, con))
                    {
                        cmdWorkflow.Parameters.AddWithValue("@Id", reqId);
                        cmdWorkflow.Parameters.AddWithValue("@AuditorId", auditor);
                        cmdWorkflow.ExecuteNonQuery();
                    }

                    // C. Push it to ReadyForPayment using your existing SP
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
            catch (Exception ex)
            {
                lblModalMessage.Text = "Error: " + ex.Message;
                lblModalMessage.CssClass = "text-danger font-weight-bold";
                upDetails.Update();
            }
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

            try
            {
                // 1. Run the Math silently
                ExecuteRecalculationSQL(reqId);

                // 2. Set the success message
                lblModalMessage.Text = "Amounts recalculated and synced successfully.";
                lblModalMessage.CssClass = "text-success font-weight-bold";

                // 3. Update UI once
                BindDetailsGrid(reqId);
                upDetails.Update();

                BindRequestsGrid(); // <-- ADD THIS: Syncs background grid when manual refresh clicked
            }
            catch (Exception ex)
            {
                lblModalMessage.Text = "Error during recalculation: " + ex.Message;
                lblModalMessage.CssClass = "text-danger font-weight-bold";
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

        // =========================================================
        // CORE MATH ENGINE: Executes Recalculation SQL Only (No UI Updates)
        // =========================================================
        private void ExecuteRecalculationSQL(int reqId)
        {
            string sqlRecalculate = @"
            UPDATE d
            SET 
                Snapshot_DAAmount = ISNULL((SELECT SUM(Amount) FROM SalesDailyExpense e WHERE e.ActivityId = d.ActivityId AND e.ExpenseType = 'DA' AND e.IsDeleted = 0 AND ISNULL(e.ClaimStatus,'') <> 'Rejected'), 0),
                Snapshot_HotelAmount = ISNULL((SELECT SUM(Amount) FROM SalesDailyExpense e WHERE e.ActivityId = d.ActivityId AND e.ExpenseType = 'Hotel' AND e.IsDeleted = 0 AND ISNULL(e.ClaimStatus,'') <> 'Rejected'), 0),
                Snapshot_TravelAmount = ISNULL((SELECT SUM(Amount) FROM SalesDailyExpense e WHERE e.ActivityId = d.ActivityId AND e.ExpenseType = 'Travel' AND e.IsDeleted = 0 AND ISNULL(e.ClaimStatus,'') <> 'Rejected'), 0)
            FROM SalesApprovalRequestDetail d WHERE d.ApprovalRequestId = @Id;

            UPDATE SalesApprovalRequestDetail 
            SET Snapshot_ExpenseTotal = ISNULL(Snapshot_DAAmount, 0) + ISNULL(Snapshot_HotelAmount, 0) + ISNULL(Snapshot_TravelAmount, 0) 
            WHERE ApprovalRequestId = @Id;

            UPDATE SalesApprovalRequest
            SET 
                ApprovedKM = (SELECT ISNULL(SUM(Snapshot_TotalDistanceKM), 0) FROM SalesApprovalRequestDetail WHERE ApprovalRequestId = @Id AND ISNULL(DetailStatus,'') <> 'Rejected'),
                ApprovedClaimAmount = (SELECT ISNULL(SUM(Snapshot_CalculatedClaimAmount), 0) FROM SalesApprovalRequestDetail WHERE ApprovalRequestId = @Id AND ISNULL(DetailStatus,'') <> 'Rejected'),
                ApprovedDAAmount = (SELECT ISNULL(SUM(Snapshot_DAAmount), 0) FROM SalesApprovalRequestDetail WHERE ApprovalRequestId = @Id AND ISNULL(DetailStatus,'') <> 'Rejected'),
                ApprovedHotelAmount = (SELECT ISNULL(SUM(Snapshot_HotelAmount), 0) FROM SalesApprovalRequestDetail WHERE ApprovalRequestId = @Id AND ISNULL(DetailStatus,'') <> 'Rejected'),
                ApprovedTravelAmount = (SELECT ISNULL(SUM(Snapshot_TravelAmount), 0) FROM SalesApprovalRequestDetail WHERE ApprovalRequestId = @Id AND ISNULL(DetailStatus,'') <> 'Rejected') 
            WHERE ApprovalRequestId = @Id;";

            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand(sqlRecalculate, con))
            {
                cmd.Parameters.AddWithValue("@Id", reqId);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}