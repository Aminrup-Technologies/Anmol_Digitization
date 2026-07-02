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
        private decimal _totalDA = 0m;
        private decimal _totalHotel = 0m;
        private decimal _totalTravel = 0m;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["USERID"] != null)
                {
                    PopulateFilters();
                    BindRequestsGrid();
                }
            }
        }

        private void PopulateFilters()
        {
            // Business Logic: Active claims are for the previous calendar month
            DateTime activeClaimMonth = DateTime.Now.AddMonths(-1);

            ddlYear.Items.Clear();
            ddlYear.Items.Add(new ListItem("-- All Years --", "0"));

            int actualYear = DateTime.Now.Year;
            for (int i = actualYear - 3; i <= actualYear + 1; i++)
            {
                ddlYear.Items.Add(new ListItem(i.ToString(), i.ToString()));
            }

            if (ddlYear.Items.FindByValue(activeClaimMonth.Year.ToString()) != null)
            {
                ddlYear.SelectedValue = activeClaimMonth.Year.ToString();
            }
            ddlMonth.SelectedValue = activeClaimMonth.Month.ToString();
        }

        protected void BindRequestsGrid()
        {
            string employeeCode = Session["workman"]?.ToString() ?? "Guest";
            DataTable dt = new DataTable();

            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand("dbo.usp_GetPendingApprovalsForUser_Filters", con))
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ApproverLoginId", employeeCode);

                int year = 0; int month = 0;

                if (int.TryParse(ddlYear.SelectedValue, out year) && year > 0)
                    cmd.Parameters.AddWithValue("@Year", year);
                else
                    cmd.Parameters.AddWithValue("@Year", DBNull.Value);

                if (int.TryParse(ddlMonth.SelectedValue, out month) && month > 0)
                    cmd.Parameters.AddWithValue("@Month", month);
                else
                    cmd.Parameters.AddWithValue("@Month", DBNull.Value);

                if (!string.IsNullOrWhiteSpace(txtSearchEmp.Text))
                    cmd.Parameters.AddWithValue("@EmpCode", txtSearchEmp.Text.Trim());
                else
                    cmd.Parameters.AddWithValue("@EmpCode", DBNull.Value);

                da.Fill(dt);
            }

            gvRequests.DataSource = dt;
            gvRequests.DataBind();

            // REQUIRED FOR DATATABLES: Forces the Grid to use <thead>
            if (gvRequests.Rows.Count > 0)
            {
                gvRequests.UseAccessibleHeader = true;
                gvRequests.HeaderRow.TableSection = TableRowSection.TableHeader;
            }

            if (upMainGrid != null) upMainGrid.Update();
        }

        protected void btnSearch_Click(object sender, EventArgs e) { BindRequestsGrid(); }
        protected void btnPrevMonth_Click(object sender, EventArgs e) { AdjustMonth(-1); }
        protected void btnNextMonth_Click(object sender, EventArgs e) { AdjustMonth(1); }
        protected void btnCurrentMonth_Click(object sender, EventArgs e)
        {
            PopulateFilters();
            BindRequestsGrid();
        }

        protected void btnReset_Click(object sender, EventArgs e)
        {
            txtSearchEmp.Text = string.Empty;
            PopulateFilters();
            BindRequestsGrid();
        }

        private void AdjustMonth(int monthOffset)
        {
            int currentY = 0, currentM = 0;
            if (int.TryParse(ddlYear.SelectedValue, out currentY) && int.TryParse(ddlMonth.SelectedValue, out currentM) && currentY > 0 && currentM > 0)
            {
                DateTime viewDate = new DateTime(currentY, currentM, 1);
                DateTime targetDate = viewDate.AddMonths(monthOffset);

                if (ddlYear.Items.FindByValue(targetDate.Year.ToString()) == null)
                {
                    ddlYear.Items.Add(new ListItem(targetDate.Year.ToString(), targetDate.Year.ToString()));
                }
                ddlYear.SelectedValue = targetDate.Year.ToString();
                ddlMonth.SelectedValue = targetDate.Month.ToString();
            }
            BindRequestsGrid();
        }

        protected void gvRequests_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "View")
            {
                int reqId = Convert.ToInt32(e.CommandArgument);
                SetCurrentUserApproverDetails(reqId); // Identifies exact role in hierarchy
                ShowDetailsModal(reqId);
            }
        }

        // --- 2. DETAILS & CALCULATION ENGINE ---
        protected void ShowDetailsModal(int reqId)
        {
            hfActionRequestId.Value = reqId.ToString();
            lblModalRequestId.Text = reqId.ToString();
            BindDetailsGrid(reqId);

            if (upDetails != null) upDetails.Update();
            ScriptManager.RegisterStartupScript(this, GetType(), "openModal", "openDetailsModal();", true);
        }

        protected void BindDetailsGrid(int reqId)
        {
            _totalSnapshot = 0m; _totalTAClaim = 0m; _totalDistance = 0m;
            _totalDA = 0m; _totalHotel = 0m; _totalTravel = 0m;

            SetCurrentUserApproverDetails(reqId); // Ensure state is fresh

            DataTable dt = new DataTable();
            decimal originalClaimed = 0m;
            string status = "Pending";
            string requesterName = "";
            string requesterCode = "";

            using (var con = new SqlConnection(connString))
            {
                using (var cmd = new SqlCommand("dbo.usp_GetApprovalRequestDetails", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ApprovalRequestId", reqId);
                    using (var da = new SqlDataAdapter(cmd)) { da.Fill(dt); }
                }

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
                            requesterCode = rdr["RequesterLoginId"].ToString();
                        }
                    }
                }
            }

            lblModalRequestId.Text = reqId.ToString();
            lblRequesterName.Text = $"<i class='fa fa-user mr-1'></i> {requesterName} ({requesterCode})";

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
                    SELECT 
                        CAST((ISNULL(d.Snapshot_CalculatedClaimAmount, 0) + ISNULL(d.Snapshot_ExpenseTotal, 0)) AS DECIMAL(18,2)) AS DeductedAmount,
                        ISNULL(d.Snapshot_TotalDistanceKM, 0) AS DeductedKM
                    FROM dbo.SalesApprovalRequestDetail d
                    WHERE d.ApprovalRequestId = @Id AND d.DetailStatus = 'Rejected'
        
                    UNION ALL
        
                    SELECT 
                        CAST(e.Amount AS DECIMAL(18,2)) AS DeductedAmount,
                        0 AS DeductedKM
                    FROM dbo.SalesDailyExpense e
                    INNER JOIN dbo.SalesApprovalRequestDetail d ON e.ActivityId = d.ActivityId
                    WHERE d.ApprovalRequestId = @Id AND e.ClaimStatus = 'Rejected' AND e.IsDeleted = 0 AND ISNULL(d.DetailStatus, '') <> 'Rejected'
        
                    UNION ALL
        
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

            // =========================================================
            // SMART BUTTON VISIBILITY & COUNTING (Check Parent Rows)
            // =========================================================
            int pendingDayRowsCount = 0;
            foreach (GridViewRow row in gvDetails.Rows)
            {
                if (row.RowType == DataControlRowType.DataRow)
                {
                    var btnReject = row.FindControl("btnRejectRow") as Button;
                    if (btnReject != null && btnReject.Enabled) { pendingDayRowsCount++; }
                }
            }

            bool pendingActionExists = (pendingDayRowsCount > 0);

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
            // DYNAMIC HIERARCHY WORKFLOW ROUTING
            // =========================================================
            int currentLvl = Convert.ToInt32(ViewState["CurrentUserLevel"] ?? 0);
            string currentRole = ViewState["CurrentUserRole"]?.ToString() ?? "";

            bool isAuditorCheck = currentRole.Equals("Auditor", StringComparison.OrdinalIgnoreCase);
            bool isIntermediate = (currentLvl > 0 && !isAuditorCheck); // Automatically handles ASM, RSM, ZSM

            hfCurrentUserLevel.Value = currentLvl.ToString();

            // Intermediates (ASM/RSM/ZSM) get Forward Button
            btnForwardLevel.Visible = isIntermediate && !pendingActionExists && (unprocessedReceipts == 0);

            // Auditor gets the Final Payment Button
            btnMarkReadyForPayment.Visible = isAuditorCheck && !pendingActionExists && (unprocessedReceipts == 0);

            // Return to sender visible to any active approver
            btnReturnToEmployee.Visible = (currentLvl > 0);

            if (!pendingActionExists && unprocessedReceipts > 0)
            {
                lblModalMessage.Text = $"⚠️ Cannot finalize: {unprocessedReceipts} expense receipt(s) are still pending inside 'View Details'.";
                lblModalMessage.CssClass = "text-danger font-weight-bold";
            }
            else if (pendingActionExists)
            {
                lblModalMessage.Text = $"Please action the {pendingDayRowsCount} pending day row(s) to proceed.";
                lblModalMessage.CssClass = "text-warning font-weight-bold";
            }
            else
            {
                lblModalMessage.Text = "";
            }

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

                ExecuteRecalculationSQL(reqId);

                using (var con = new SqlConnection(connString))
                {
                    con.Open();
                    string sqlWorkflow = @"
                        UPDATE dbo.SalesApprovalRequestApprover 
                        SET Status = 'Approved', ActionBy = @User, ActionDate = SYSUTCDATETIME() 
                        WHERE ApprovalRequestId = @Id AND ApproverLevel = @Level AND Status = 'Pending';

                        DECLARE @NextLevel INT, @NextLogin NVARCHAR(100), @NextRole NVARCHAR(100);
                        
                        SELECT TOP 1 
                            @NextLevel = ApproverLevel, 
                            @NextLogin = ApproverLoginId, 
                            @NextRole = ApproverRole
                        FROM dbo.SalesApprovalRequestApprover
                        WHERE ApprovalRequestId = @Id AND ApproverLevel > @Level AND Status = 'Pending'
                        ORDER BY ApproverLevel ASC;

                        IF @NextLevel IS NOT NULL
                        BEGIN
                            UPDATE dbo.SalesApprovalRequest
                            SET 
                                CurrentApproverLevel = @NextLevel,
                                CurrentApproverLoginId = @NextLogin,
                                ModifiedBy = @User,
                                ModifiedOn = SYSUTCDATETIME()
                            WHERE ApprovalRequestId = @Id;

                            INSERT INTO dbo.ApprovalRequestStatusHistory 
                            (ApprovalRequestId, OldStatus, NewStatus, ChangedBy, Remarks)
                            VALUES 
                            (@Id, 'Pending', 'Pending', @User, CONCAT('Level ', @Level, ' approved. Forwarded to ', @NextRole, ' (', ISNULL(@NextLogin, 'Audit Pool'), ')'));
                        END
                        ELSE
                        BEGIN
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

                BindRequestsGrid();
                ScriptManager.RegisterStartupScript(this, GetType(), "closeModal", "$('#detailsModal').modal('hide');", true);
            }
            catch (Exception ex)
            {
                lblModalMessage.Text = "Error during handover: " + ex.Message;
                lblModalMessage.CssClass = "text-danger font-weight-bold";
                upDetails.Update();
            }
        }

        protected void btnReturnToEmployee_Click(object sender, EventArgs e)
        {
            try
            {
                int reqId = Convert.ToInt32(hfActionRequestId.Value);
                string user = GetLoggedInEmployeeCode();
                //string reason = hfReturnReason.Value.Trim();
                string rawReason = hfReturnReason.Value.Trim();
                string reason = AppendUserToRemark(rawReason, user);

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

                hfReturnReason.Value = "";

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

                int currentLevel = Convert.ToInt32(ViewState["CurrentUserLevel"] ?? 0);
                string detailStatus = drv["DetailStatus"].ToString();
                if (string.IsNullOrEmpty(detailStatus)) detailStatus = "Pending";
                int approvedLevel = drv["DetailApprovedLevel"] == DBNull.Value ? 0 : Convert.ToInt32(drv["DetailApprovedLevel"]);

                var btnApprove = (Button)e.Row.FindControl("btnApproveRow");
                var btnReject = (Button)e.Row.FindControl("btnRejectRow");
                var txtRemark = (TextBox)e.Row.FindControl("txtRowRemark");
                var btnExp = e.Row.FindControl("btnExp") as System.Web.UI.HtmlControls.HtmlButton;

                if (btnApprove != null) btnApprove.Enabled = false;
                if (btnReject != null) btnReject.Enabled = false;
                if (txtRemark != null) txtRemark.ReadOnly = true;

                string expensesJson = drv["ExpensesJson"] != DBNull.Value ? drv["ExpensesJson"].ToString() : "";
                bool hasPendingReceipts = expensesJson.Contains("\"ClaimStatus\":\"Submitted\"") ||
                                          expensesJson.Contains("\"ClaimStatus\":\"Draft\"") ||
                                          expensesJson.Contains("\"ClaimStatus\":\"Pending\"");

                if (detailStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                {
                    e.Row.CssClass += " table-danger";
                }
                else if (detailStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase) && approvedLevel == currentLevel)
                {
                    e.Row.CssClass += " table-success";
                    if (btnApprove != null) { btnApprove.Text = "✔ Done"; btnApprove.CssClass = "btn btn-sm btn-success"; }
                }
                else if (detailStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase) && approvedLevel < currentLevel)
                {
                    e.Row.CssClass += " table-warning";
                    ApplyGatekeeperLogic(btnApprove, btnReject, txtRemark, btnExp, hasPendingReceipts);
                }
                else if (detailStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                {
                    ApplyGatekeeperLogic(btnApprove, btnReject, txtRemark, btnExp, hasPendingReceipts);
                }

                // DYNAMIC HIERARCHY AUDITOR CHECK
                string currentRole = ViewState["CurrentUserRole"]?.ToString() ?? "";
                bool isAuditor = currentRole.Equals("Auditor", StringComparison.OrdinalIgnoreCase);

                if (isAuditor && hasPendingReceipts && !detailStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                {
                    if (btnExp != null)
                    {
                        btnExp.Attributes["class"] = "btn btn-sm btn-danger text-white btn-day-action pulse-warning shadow-sm";
                        btnExp.InnerText = "⚠️ Action";
                        btnExp.Attributes["title"] = "Pending receipts require your approval or rejection!";
                    }
                }
            }
            else if (e.Row.RowType == DataControlRowType.Footer)
            {
                FillFooterTotals(e.Row);
            }
        }

        private void ApplyGatekeeperLogic(Button btnApprove, Button btnReject, TextBox txtRemark, System.Web.UI.HtmlControls.HtmlButton btnExp, bool hasPendingReceipts)
        {
            if (txtRemark != null) txtRemark.ReadOnly = false;
            if (btnReject != null) btnReject.Enabled = true;

            if (hasPendingReceipts)
            {
                if (btnApprove != null)
                {
                    btnApprove.Enabled = false;
                    btnApprove.ToolTip = "You must open the folder and action all receipts first.";
                    btnApprove.CssClass = "btn btn-sm btn-outline-secondary";
                }
                if (btnExp != null)
                {
                    string currentClass = btnExp.Attributes["class"] ?? "";
                    if (!currentClass.Contains("pulse-info"))
                        btnExp.Attributes["class"] = currentClass + " pulse-info";
                }
            }
            else
            {
                if (btnApprove != null)
                {
                    btnApprove.Enabled = true;
                    btnApprove.ToolTip = "Approve entire day";
                    btnApprove.CssClass = "btn btn-sm btn-success pulse-success text-white";
                }
                if (btnExp != null)
                {
                    string currentClass = btnExp.Attributes["class"] ?? "";
                    btnExp.Attributes["class"] = currentClass.Replace("pulse-info", "").Trim();
                }
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

        protected void btnRefreshCalculations_Click(object sender, EventArgs e)
        {
            int reqId = Convert.ToInt32(hfActionRequestId.Value);
            ExecuteRecalculationSQL(reqId);
            BindDetailsGrid(reqId);
            upDetails.Update();
            BindRequestsGrid();
        }

        protected void gvDetails_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "ApproveRow" || e.CommandName == "RejectRow")
            {
                int detailId = Convert.ToInt32(e.CommandArgument);
                bool isApprove = (e.CommandName == "ApproveRow");

                GridViewRow row = (GridViewRow)((Control)e.CommandSource).NamingContainer;
                TextBox txt = row.FindControl("txtRowRemark") as TextBox;
                //string remark = txt != null ? txt.Text : "";
                string action = isApprove ? "APPROVE" : "REJECT";
                string user = GetLoggedInEmployeeCode();
                string rawRemark = txt != null ? txt.Text : "";
                string remark = AppendUserToRemark(rawRemark, user);

                using (var con = new SqlConnection(connString))
                {
                    con.Open();

                    // 1. Existing Parent Row SP Call
                    using (var cmd = new SqlCommand("dbo.usp_ProcessSalesApprovalRequestDetail", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ApprovalRequestDetailId", detailId);
                        cmd.Parameters.AddWithValue("@ApprovalRequestId", hfActionRequestId.Value);
                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@ActionBy", user);
                        cmd.Parameters.AddWithValue("@ActionRemarks", remark);
                        cmd.ExecuteNonQuery();
                    }

                    // 2. NEW: Cascade the decision down to any pending child receipts!
                    string cascadeSql = @"
                        UPDATE dbo.SalesDailyExpense 
                        SET ClaimStatus = @ActionStatus, 
                            ClaimProcessedBy = @ActionBy, 
                            ClaimProcessedDate = DATEADD(MINUTE, 330, SYSUTCDATETIME()),
                            ModifiedBy = @ActionBy,
                            ModifiedOn = SYSUTCDATETIME(),
                            Remarks = ISNULL(Remarks, @Remarks)
                        WHERE ActivityId = (SELECT ActivityId FROM dbo.SalesApprovalRequestDetail WHERE ApprovalRequestDetailId = @DetailId) 
                          AND ClaimStatus NOT IN ('Approved', 'Rejected') 
                          AND IsDeleted = 0;";

                    using (var cascadeCmd = new SqlCommand(cascadeSql, con))
                    {
                        cascadeCmd.Parameters.AddWithValue("@ActionStatus", isApprove ? "Approved" : "Rejected");
                        cascadeCmd.Parameters.AddWithValue("@ActionBy", user);
                        cascadeCmd.Parameters.AddWithValue("@DetailId", detailId);
                        cascadeCmd.Parameters.AddWithValue("@Remarks", string.IsNullOrEmpty(remark) ? (object)DBNull.Value : remark);
                        cascadeCmd.ExecuteNonQuery();
                    }
                }

                int reqId = Convert.ToInt32(hfActionRequestId.Value);
                ExecuteRecalculationSQL(reqId);
                BindDetailsGrid(Convert.ToInt32(hfActionRequestId.Value));
                upDetails.Update();
                BindRequestsGrid();
            }
        }

        protected void btnApproveSelected_Click(object sender, EventArgs e) { ProcessBulkAction("Approved"); }
        protected void btnRejectSelected_Click(object sender, EventArgs e) { ProcessBulkAction("Rejected"); }

        private void ProcessBulkAction(string newStatus)
        {
            try
            {
                if (string.IsNullOrEmpty(hfActionRequestId.Value)) return;

                int reqId = Convert.ToInt32(hfActionRequestId.Value);
                string auditor = GetLoggedInEmployeeCode();
                int currentLevel = Convert.ToInt32(ViewState["CurrentUserLevel"] ?? 0);
                //string remarks = txtBulkRemarks.Text.Trim();
                string rawRemarks = txtBulkRemarks.Text.Trim();
                string remarks = AppendUserToRemark(rawRemarks, auditor);
                int processedCount = 0;

                using (var con = new SqlConnection(connString))
                {
                    con.Open();

                    foreach (GridViewRow row in gvDetails.Rows)
                    {
                        if (row.RowType == DataControlRowType.DataRow)
                        {
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
                                    WHERE ActivityId = @ActivityId AND ApprovalRequestId = @ReqId;

                                    UPDATE dbo.SalesDailyExpense
                                    SET ClaimStatus = @Status,
                                        ClaimProcessedBy = @ActionBy,
                                        ClaimProcessedDate = DATEADD(MINUTE, 330, SYSUTCDATETIME()),
                                        ModifiedBy = @ActionBy,
                                        ModifiedOn = SYSUTCDATETIME(),
                                        Remarks = ISNULL(Remarks, @Remarks)
                                    WHERE ActivityId = @ActivityId 
                                      AND ClaimStatus NOT IN ('Approved', 'Rejected') 
                                      AND IsDeleted = 0;";

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
                    ExecuteRecalculationSQL(reqId);
                    txtBulkRemarks.Text = "";
                    lblModalMessage.Text = $"{processedCount} row(s) successfully marked as {newStatus}.";
                    lblModalMessage.CssClass = newStatus == "Approved" ? "text-success font-weight-bold" : "text-danger font-weight-bold";
                }
                else
                {
                    lblModalMessage.Text = "Please select at least one row to apply the bulk action.";
                    lblModalMessage.CssClass = "text-warning font-weight-bold";
                }

                BindDetailsGrid(reqId);
                upDetails.Update();
                BindRequestsGrid();
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
            return Session["WORKMAN"] as string;
        }

        protected void btnMarkReadyForPayment_Click(object sender, EventArgs e)
        {
            try
            {
                int reqId = Convert.ToInt32(hfActionRequestId.Value);
                string auditor = GetLoggedInEmployeeCode();

                ExecuteRecalculationSQL(reqId);

                using (var con = new SqlConnection(connString))
                {
                    con.Open();

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
        // ADDED REMARK PARAMETER HERE!
        public static object UpdateIndividualExpense(int expenseId, string status, string remark)
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
                    // ADDED REMARK CAPTURE HERE!
                    //cmd.Parameters.AddWithValue("@Remarks", string.IsNullOrEmpty(remark) ? (object)DBNull.Value : remark);
                    // Format the remark before sending to DB
                    string formattedRemark = AppendUserToRemark(remark, user);
                    cmd.Parameters.AddWithValue("@Remarks", string.IsNullOrEmpty(formattedRemark) ? (object)DBNull.Value : formattedRemark);
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
                    //cmd.Parameters.AddWithValue("@Reason", reason);
                    // Format the reason before sending to DB
                    string formattedReason = AppendUserToRemark(reason, user);
                    cmd.Parameters.AddWithValue("@Reason", formattedReason);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
                return new { success = true };
            }
            catch (Exception ex) { return new { success = false, message = ex.Message }; }
        }

        // NEW ENGINE: DYNAMIC HIERARCHY IDENTIFIER
        private void SetCurrentUserApproverDetails(int reqId)
        {
            string user = Session["workman"]?.ToString();
            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand("SELECT TOP 1 ApproverLevel, ApproverRole FROM SalesApprovalRequestApprover WHERE ApprovalRequestId=@Id AND ApproverLoginId=@User AND Status='Pending'", con))
            {
                cmd.Parameters.AddWithValue("@Id", reqId);
                cmd.Parameters.AddWithValue("@User", user);
                con.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        ViewState["CurrentUserLevel"] = Convert.ToInt32(rdr["ApproverLevel"]);
                        ViewState["CurrentUserRole"] = rdr["ApproverRole"].ToString();
                    }
                    else
                    {
                        ViewState["CurrentUserLevel"] = 0;
                        ViewState["CurrentUserRole"] = "";
                    }
                }
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object GetKMHistory_OLD(int activityId)
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

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object GetKMHistory(int activityId)
        {
            try
            {
                string connString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

                // 1. Get Base Activity Info (for the Map)
                string fromPlace = "";
                string toPlace = "";
                using (SqlConnection con = new SqlConnection(connString))
                using (SqlCommand cmdInfo = new SqlCommand("SELECT RouteOriginName, RouteDestinationName FROM dbo.SalesDailyActivity WHERE ActivityId = @Id", con))
                {
                    cmdInfo.Parameters.AddWithValue("@Id", activityId);
                    con.Open();
                    using (SqlDataReader rdr = cmdInfo.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            fromPlace = rdr["RouteOriginName"] != DBNull.Value ? rdr["RouteOriginName"].ToString() : "";
                            toPlace = rdr["RouteDestinationName"] != DBNull.Value ? rdr["RouteDestinationName"].ToString() : "";
                        }
                    }
                }

                // 2. Get the History Array
                var historyList = new List<object>();
                using (SqlConnection con = new SqlConnection(connString))
                using (SqlCommand cmd = new SqlCommand(@"SELECT OldKM, NewKM, ModifiedBy, ModifiedDateTime, Reason FROM dbo.SalesDailyActivityKMHistory WHERE ActivityId = @ActivityId ORDER BY HistoryId DESC", con))
                {
                    cmd.Parameters.AddWithValue("@ActivityId", activityId);
                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            historyList.Add(new
                            {
                                OldKM = rdr["OldKM"],
                                NewKM = rdr["NewKM"],
                                ModifiedBy = rdr["ModifiedBy"],
                                ModifiedDateTimeIST = rdr["ModifiedDateTime"] != DBNull.Value ? Convert.ToDateTime(rdr["ModifiedDateTime"]).ToString("dd-MM-yyyy HH:mm:ss") : "",
                                Reason = rdr["Reason"]
                            });
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

        protected void btnRecalculate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(hfActionRequestId.Value)) return;
            int reqId = Convert.ToInt32(hfActionRequestId.Value);

            try
            {
                ExecuteRecalculationSQL(reqId);
                lblModalMessage.Text = "Amounts recalculated and synced successfully.";
                lblModalMessage.CssClass = "text-success font-weight-bold";
                BindDetailsGrid(reqId);
                upDetails.Update();
                BindRequestsGrid();
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
                        ISNULL(d.DetailRemarks, 'Rejected by Approver') AS Reason,
                        CAST((ISNULL(d.Snapshot_CalculatedClaimAmount, 0) + ISNULL(d.Snapshot_ExpenseTotal, 0)) AS DECIMAL(18,2)) AS DeductedAmount
                    FROM dbo.SalesApprovalRequestDetail d
                    INNER JOIN dbo.SalesDailyActivity a ON d.ActivityId = a.ActivityId
                    WHERE d.ApprovalRequestId = @ReqId AND d.DetailStatus = 'Rejected'

                    UNION ALL

                    SELECT 
                        CONVERT(varchar, a.ActivityDate, 106) AS ActivityDate,
                        'Rejected Receipt: ' + e.ExpenseType AS DeductionType,
                        ISNULL(e.Remarks, 'Receipt marked as Rejected') AS Reason,
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

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object ActionDayRowSilent(int detailId, int reqId, string action)
        {
            try
            {
                string user = HttpContext.Current.Session["workman"]?.ToString() ?? "System";
                string conn = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

                using (var con = new SqlConnection(conn))
                {
                    con.Open();

                    // 1. Existing Parent Row SP Call
                    using (var cmd = new SqlCommand("dbo.usp_ProcessSalesApprovalRequestDetail", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ApprovalRequestDetailId", detailId);
                        cmd.Parameters.AddWithValue("@ApprovalRequestId", reqId);
                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@ActionBy", user);
                        cmd.Parameters.AddWithValue("@ActionRemarks", "Actioned via Day View Modal");
                        cmd.ExecuteNonQuery();
                    }

                    // 2. NEW: Cascade the decision down to any pending child receipts!
                    string cascadeSql = @"
                        UPDATE dbo.SalesDailyExpense 
                        SET ClaimStatus = @ActionStatus, 
                            ClaimProcessedBy = @ActionBy, 
                            ClaimProcessedDate = DATEADD(MINUTE, 330, SYSUTCDATETIME()),
                            ModifiedBy = @ActionBy,
                            ModifiedOn = SYSUTCDATETIME(),
                            Remarks = ISNULL(Remarks, 'Actioned via Day View Modal')
                        WHERE ActivityId = (SELECT ActivityId FROM dbo.SalesApprovalRequestDetail WHERE ApprovalRequestDetailId = @DetailId) 
                          AND ClaimStatus NOT IN ('Approved', 'Rejected') 
                          AND IsDeleted = 0;";

                    using (var cascadeCmd = new SqlCommand(cascadeSql, con))
                    {
                        cascadeCmd.Parameters.AddWithValue("@ActionStatus", action == "APPROVE" ? "Approved" : "Rejected");
                        cascadeCmd.Parameters.AddWithValue("@ActionBy", user);
                        cascadeCmd.Parameters.AddWithValue("@DetailId", detailId);
                        cascadeCmd.ExecuteNonQuery();
                    }
                }
                return new { success = true };
            }
            catch (Exception ex) { return new { success = false, message = ex.Message }; }
        }

        // =========================================================
        // GLOBAL REMARKS FORMATTER (AUDIT TRAIL)
        // =========================================================
        public static string AppendUserToRemark(string rawRemark, string empCode)
        {
            if (string.IsNullOrWhiteSpace(rawRemark)) return rawRemark;
            if (rawRemark.Contains("- Added by")) return rawRemark; // Prevent double-appending

            string empName = empCode; // Fallback to code if name not found
            string connString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            try
            {
                using (var con = new SqlConnection(connString))
                using (var cmd = new SqlCommand("SELECT EmployeeName FROM MST_UserMaster WHERE EmployeeCode = @Code", con))
                {
                    cmd.Parameters.AddWithValue("@Code", empCode);
                    con.Open();
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        empName = result.ToString();
                    }
                }
            }
            catch { /* Keep fallback if DB lookup fails */ }

            return $"{rawRemark.Trim()} - Added by {empName} ({empCode})";
        }

    }
}