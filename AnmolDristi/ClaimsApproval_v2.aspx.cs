using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Services;
using System.Web.Script.Services;
using System.Globalization;
using Newtonsoft.Json.Linq;

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
        private decimal _totalStationary = 0m; // <-- ADD THIS

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["USERID"] == null)
                {
                    Response.Redirect("~/Login.aspx", false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }

                PopulateFilters();
                BindRequestsGrid();
            }
        }

        private const string SessionApproverRoleKey = "ClaimsApproval_ApproverRole";

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
            string employeeCode = GetLoggedInEmployeeCode() ?? "Guest";
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
            UpdateRequestSummaryCounters(dt);
            pnlEmptyRequests.Visible = dt.Rows.Count == 0;
            pnlRequests.Visible = dt.Rows.Count > 0;

            // REQUIRED FOR DATATABLES: Forces the Grid to use <thead>
            if (gvRequests.Rows.Count > 0)
            {
                gvRequests.UseAccessibleHeader = true;
                gvRequests.HeaderRow.TableSection = TableRowSection.TableHeader;
            }

            if (upMainGrid != null) upMainGrid.Update();
        }

        protected void gvRequests_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            // Status badges are applied via GetStatusBadgeCss in markup.
        }

        protected string GetStatusBadgeCss(object statusObj)
        {
            string status = statusObj?.ToString() ?? string.Empty;
            if (ClaimStatuses.Equals(status, ClaimStatuses.Pending) ||
                ClaimStatuses.Equals(status, ClaimStatuses.Submitted) ||
                ClaimStatuses.Equals(status, ClaimStatuses.Draft))
                return "claims-badge claims-badge-pending";
            if (ClaimStatuses.Equals(status, ClaimStatuses.Returned))
                return "claims-badge claims-badge-returned";
            if (ClaimStatuses.Equals(status, ClaimStatuses.Approved))
                return "claims-badge claims-badge-approved";
            if (ClaimStatuses.Equals(status, ClaimStatuses.Completed))
                return "claims-badge claims-badge-completed";
            if (ClaimStatuses.Equals(status, ClaimStatuses.ReadyForPayment))
                return "claims-badge claims-badge-ready";
            if (ClaimStatuses.Equals(status, ClaimStatuses.Rejected))
                return "claims-badge claims-badge-rejected";
            return "claims-badge claims-badge-default";
        }

        private void UpdateRequestSummaryCounters(DataTable dt)
        {
            int pending = 0, returned = 0, completed = 0, ready = 0;
            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    string status = row.Table.Columns.Contains("RequestStatus")
                        ? row["RequestStatus"]?.ToString() ?? string.Empty
                        : string.Empty;
                    if (ClaimStatuses.Equals(status, ClaimStatuses.Pending)) pending++;
                    else if (ClaimStatuses.Equals(status, ClaimStatuses.Returned)) returned++;
                    else if (ClaimStatuses.Equals(status, ClaimStatuses.Completed)) completed++;
                    else if (ClaimStatuses.Equals(status, ClaimStatuses.ReadyForPayment)) ready++;
                }
            }

            lblCountPending.Text = pending.ToString();
            lblCountReturned.Text = returned.ToString();
            lblCountCompleted.Text = completed.ToString();
            lblCountReadyForPayment.Text = ready.ToString();
        }

        private static void ApplyExpenseCountHint(LinkButton lnkReview, string expensesJson)
        {
            if (lnkReview == null) return;
            int total = 0, approved = 0, pending = 0, rejected = 0;
            ParseExpenseCounts(expensesJson, out total, out approved, out pending, out rejected);
            if (total > 0)
            {
                lnkReview.Text = "<i class=\"fa fa-folder-open\"></i> (" + total + ")";
                lnkReview.ToolTip = string.Format("Expenses ({0}) — Approved: {1}, Pending: {2}, Rejected: {3}", total, approved, pending, rejected);
            }
        }

        private static void ParseExpenseCounts(string expensesJson, out int total, out int approved, out int pending, out int rejected)
        {
            total = approved = pending = rejected = 0;
            if (string.IsNullOrWhiteSpace(expensesJson) || expensesJson.Trim() == "[]") return;
            try
            {
                var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                var list = serializer.Deserialize<List<Dictionary<string, object>>>(expensesJson);
                if (list == null) return;
                foreach (var item in list)
                {
                    total++;
                    string st = item.ContainsKey("ClaimStatus") ? (item["ClaimStatus"]?.ToString() ?? "") : "";
                    if (ClaimStatuses.Equals(st, ClaimStatuses.Approved)) approved++;
                    else if (ClaimStatuses.Equals(st, ClaimStatuses.Rejected)) rejected++;
                    else pending++;
                }
            }
            catch { /* display-only helper */ }
        }

        private sealed class ApprovalPolicyDto
        {
            public decimal? Resolved_MaxKM { get; set; }
            public decimal? Resolved_RatePerKm { get; set; }
            public decimal? Resolved_DA { get; set; }
            public decimal? Resolved_OutstationDA { get; set; }
            public decimal? Resolved_OwnAllowance { get; set; }
            public string PolicyJson { get; set; }
            public string PolicyMode { get; set; }
        }

        private static void ApplyResolvedPolicyAttributes(LinkButton lnkReview, string expensesJson)
        {
            if (lnkReview == null) return;

            ApprovalPolicyDto dto = ResolveApprovalPolicy(expensesJson);
            SetPolicyAttribute(lnkReview, "data-policymode", dto.PolicyMode);
            SetPolicyAttribute(lnkReview, "data-resolved-maxkm", FormatPolicyDecimal(dto.Resolved_MaxKM));
            SetPolicyAttribute(lnkReview, "data-resolved-rateperkm", FormatPolicyDecimal(dto.Resolved_RatePerKm));
            SetPolicyAttribute(lnkReview, "data-resolved-da", FormatPolicyDecimal(dto.Resolved_DA));
            SetPolicyAttribute(lnkReview, "data-resolved-outstationda", FormatPolicyDecimal(dto.Resolved_OutstationDA));
            SetPolicyAttribute(lnkReview, "data-resolved-ownallowance", FormatPolicyDecimal(dto.Resolved_OwnAllowance));
        }

        private static void SetPolicyAttribute(LinkButton lnkReview, string name, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            lnkReview.Attributes[name] = value;
        }

        private static string FormatPolicyDecimal(decimal? value)
        {
            return value.HasValue
                ? value.Value.ToString("0.00", CultureInfo.InvariantCulture)
                : null;
        }

        private static ApprovalPolicyDto ResolveApprovalPolicy(string expensesJson)
        {
            var dto = new ApprovalPolicyDto();
            if (string.IsNullOrWhiteSpace(expensesJson) || expensesJson.Trim() == "[]")
                return dto;

            JObject firstSnapshotJson = null;

            try
            {
                var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                var list = serializer.Deserialize<List<Dictionary<string, object>>>(expensesJson);
                if (list == null) return dto;

                foreach (var item in list)
                {
                    if (item == null) continue;

                    string mode = ReadDictionaryString(item, "PolicyMode");
                    string snapshot = ReadDictionaryString(item, "PolicySnapshot");
                    if (string.IsNullOrEmpty(snapshot))
                    {
                        if (string.IsNullOrEmpty(dto.PolicyMode))
                            dto.PolicyMode = mode;
                        continue;
                    }

                    // First non-empty PolicySnapshot owns display resolution for the day.
                    if (!string.IsNullOrEmpty(dto.PolicyJson))
                        continue;

                    dto.PolicyJson = snapshot;
                    if (!string.IsNullOrEmpty(mode))
                        dto.PolicyMode = mode;

                    try
                    {
                        firstSnapshotJson = JObject.Parse(snapshot);
                    }
                    catch
                    {
                        firstSnapshotJson = null;
                    }

                    if (firstSnapshotJson != null)
                        ApplyResolvedColumns(dto, firstSnapshotJson);
                }
            }
            catch
            {
                return dto;
            }

            if ((dto.Resolved_MaxKM == null &&
                 dto.Resolved_RatePerKm == null &&
                 dto.Resolved_DA == null &&
                 dto.Resolved_OutstationDA == null &&
                 dto.Resolved_OwnAllowance == null)
                && !string.IsNullOrEmpty(dto.PolicyJson))
            {
                TryParseJsonFallback(dto, firstSnapshotJson);
            }

            return dto;
        }

        private static void ApplyResolvedColumns(ApprovalPolicyDto dto, JObject json)
        {
            dto.Resolved_MaxKM = ReadJsonDecimal(json, "Resolved_MaxKM");
            dto.Resolved_RatePerKm = ReadJsonDecimal(json, "Resolved_RatePerKm");
            dto.Resolved_DA = ReadJsonDecimal(json, "Resolved_DA");
            dto.Resolved_OutstationDA = ReadJsonDecimal(json, "Resolved_OutstationDA");
            dto.Resolved_OwnAllowance = ReadJsonDecimal(json, "Resolved_OwnAllowance");
        }

        private static void TryParseJsonFallback(ApprovalPolicyDto dto, JObject json)
        {
            if (dto == null || string.IsNullOrEmpty(dto.PolicyJson)) return;

            if (json == null)
            {
                try
                {
                    json = JObject.Parse(dto.PolicyJson);
                }
                catch
                {
                    return;
                }
            }

            if (string.IsNullOrEmpty(dto.PolicyMode))
                dto.PolicyMode = ReadJsonString(json, "PolicyMode");

            dto.Resolved_MaxKM = ReadJsonDecimal(json, "MaxKM", "Resolved_MaxKM");
            dto.Resolved_RatePerKm = ReadJsonDecimal(json, "RatePerKm", "Resolved_RatePerKm");
            dto.Resolved_DA = ReadJsonDecimal(json, "ResolvedDA", "DA", "Resolved_DA");
            dto.Resolved_OutstationDA = ReadJsonDecimal(json, "OutstationDA", "Outstation_DA", "Resolved_OutstationDA");
            dto.Resolved_OwnAllowance = ReadJsonDecimal(json, "OwnArrangementDA", "OwnArrangementAllowance", "OwnArrangement_DA", "Resolved_OwnAllowance");
        }

        private static string ReadDictionaryString(Dictionary<string, object> item, string key)
        {
            if (item == null || !item.ContainsKey(key) || item[key] == null) return null;
            string value = Convert.ToString(item[key]);
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string ReadJsonString(JObject json, string key)
        {
            if (json == null || json[key] == null || json[key].Type == JTokenType.Null) return null;
            string value = Convert.ToString(json[key]);
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static decimal? ReadJsonDecimal(JObject json, params string[] keys)
        {
            if (json == null || keys == null) return null;
            foreach (string key in keys)
            {
                if (json[key] == null || json[key].Type == JTokenType.Null) continue;
                decimal value;
                if (decimal.TryParse(Convert.ToString(json[key]), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                    return value;
            }
            return null;
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
            _totalDA = 0m; _totalHotel = 0m; _totalTravel = 0m; _totalStationary = 0m;

            SetCurrentUserApproverDetails(reqId); // Ensure state is fresh

            DataTable dt = new DataTable();
            string requesterName = "";
            string requesterCode = "";
            decimal headerNetPayable = 0m;
            decimal headerApprovedKM = 0m;

            // FSOT: SQL owns request approved totals before the modal binds.
            ExecuteRecalculationSQL(reqId);

            using (var con = new SqlConnection(connString))
            {
                // Details
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
                BindLatestDetailSummaryCounts(dt);

                // Header (canonical Approved* / FinalApprovedAmount)
                using (var cmd = new SqlCommand("dbo.usp_GetApprovalRequest", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ApprovalRequestId", reqId);

                    con.Open();

                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            requesterCode =
                                rdr["RequesterLoginId"].ToString();

                            requesterName =
                                rdr["RequesterName"].ToString();

                            headerNetPayable =
                                rdr["FinalApprovedAmount"] == DBNull.Value
                                    ? 0m
                                    : Convert.ToDecimal(rdr["FinalApprovedAmount"]);

                            headerApprovedKM =
                                rdr["ApprovedKM"] == DBNull.Value
                                    ? 0m
                                    : Convert.ToDecimal(rdr["ApprovedKM"]);
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

            // Informational deduction breakdown only — net payable comes from header.
            decimal totalDeductedAmount = 0m;
            decimal totalDeductedKM = 0m;

            string sqlDeduction = @"
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
                ) x;
            ";

            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand(sqlDeduction, con))
            {
                cmd.Parameters.AddWithValue("@Id", reqId);
                if (con.State != ConnectionState.Open) con.Open();

                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        totalDeductedAmount = Convert.ToDecimal(rdr["DeductedAmount"]);
                        totalDeductedKM = Convert.ToDecimal(rdr["DeductedKM"]);
                    }
                }
            }

            lblSummaryApproved.Text = headerNetPayable.ToString("N2");
            lblSummaryApprovedKM.Text = headerApprovedKM.ToString("N2");
            lblSummaryDeduction.Text = totalDeductedAmount.ToString("N2");
            lblSummaryDeductedKM.Text = totalDeductedKM.ToString("N2");
            lblSummaryOriginal.Text = (headerNetPayable + totalDeductedAmount).ToString("N2");
            lblSummaryOriginalKM.Text = (headerApprovedKM + totalDeductedKM).ToString("N2");

            // =========================================================
            // SMART BUTTON VISIBILITY & COUNTING (Check Parent Rows)
            // =========================================================
            int pendingDayRowsCount = 0;
            foreach (GridViewRow row in gvDetails.Rows)
            {
                if (row.RowType == DataControlRowType.DataRow)
                {
                    var btnApprove = row.FindControl("btnApproveRow") as Button;
                    var btnReturn = row.FindControl("btnReturnRow") as Button;
                    var btnRejectDay = row.FindControl("btnRejectDayRow") as Button;
                    if ((btnApprove != null && btnApprove.Enabled) ||
                        (btnReturn != null && btnReturn.Enabled) ||
                        (btnRejectDay != null && btnRejectDay.Enabled))
                    {
                        pendingDayRowsCount++;
                    }
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
                " + SalesApprovalDetailVersionHelper.LatestDetailJoinForD + @"
                WHERE d.ApprovalRequestId = @Id 
                  AND e.IsDeleted = 0 
                  AND ISNULL(e.ClaimStatus, '') NOT IN ('" + ClaimStatuses.Approved + @"', '" + ClaimStatuses.Rejected + @"')";

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

            bool isAuditorCheck = IsCurrentUserAuditor();
            bool isIntermediate = (currentLvl > 0 && !isAuditorCheck);

            hfCurrentUserLevel.Value = currentLvl.ToString();
            hfIsAuditor.Value = isAuditorCheck ? "1" : "0";

            int approvalRequestId = Convert.ToInt32(hfActionRequestId.Value);
            SetFinalApproverFlag(approvalRequestId);

            int returnedItemCount = GetReturnedItemCount(reqId);
            bool hasReturnedItems = returnedItemCount > 0;
            bool canFinalize = !pendingActionExists && unprocessedReceipts == 0 && !hasReturnedItems;

            // Intermediates (managers) forward when every day/expense is Approved or Rejected.
            btnForwardLevel.Visible = isIntermediate && canFinalize;

            // Auditor marks ready for payment when no Returned items remain.
            btnMarkReadyForPayment.Visible = isAuditorCheck && canFinalize;

            // Whole-claim return is a manager action only.
            btnReturnToEmployee.Visible = isIntermediate;

            if (hasReturnedItems)
            {
                lblModalMessage.Text = $"⚠️ Cannot finalize: {returnedItemCount} returned item(s) awaiting employee correction and resubmission.";
                lblModalMessage.CssClass = "text-danger font-weight-bold";
            }
            else if (!pendingActionExists && unprocessedReceipts > 0)
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
            btnRejectSelected.Visible = pendingActionExists && !isAuditorCheck;
            btnRejectDaysSelected.Visible = pendingActionExists && isAuditorCheck;
            txtBulkRemarks.Visible = pendingActionExists;
        }

        private enum DayDetailAction
        {
            Approve,
            Return,
            Reject
        }

        private bool IsCurrentUserAuditor()
        {
            string role = ViewState["CurrentUserRole"]?.ToString() ?? "";
            return role.Equals("Auditor", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsCurrentUserManager()
        {
            int level = Convert.ToInt32(ViewState["CurrentUserLevel"] ?? 0);
            return level > 0 && !IsCurrentUserAuditor();
        }

        private static bool IsAuditorRole(string role)
        {
            return role != null && role.Equals("Auditor", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetApproverRoleFromSession()
        {
            return HttpContext.Current?.Session?[SessionApproverRoleKey] as string ?? "";
        }

        private int GetReturnedItemCount(int reqId)
        {
            string sql = @"
                SELECT
                    (SELECT COUNT(1)
                     FROM dbo.SalesApprovalRequestDetail d
                     WHERE d.ApprovalRequestId = @Id
                       AND d.DetailStatus = '" + ClaimStatuses.Returned + @"'
                       AND " + SalesApprovalDetailVersionHelper.LatestDetailWhereForD.Trim() + @")
                  + (SELECT COUNT(1)
                     FROM dbo.SalesDailyExpense e
                     INNER JOIN dbo.SalesApprovalRequestDetail d ON e.ActivityId = d.ActivityId
                     " + SalesApprovalDetailVersionHelper.LatestDetailJoinForD + @"
                     WHERE d.ApprovalRequestId = @Id
                       AND e.IsDeleted = 0
                       AND e.ClaimStatus = '" + ClaimStatuses.Returned + @"')";

            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@Id", reqId);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private void EnsureCanFinalizeRequest(int reqId)
        {
            if (GetReturnedItemCount(reqId) > 0)
                throw new Exception("Cannot finalize while returned days or expenses are awaiting employee correction.");

            string sql = @"
                SELECT
                    (SELECT COUNT(1)
                     FROM dbo.SalesApprovalRequestDetail d
                     WHERE d.ApprovalRequestId = @Id
                       AND ISNULL(d.DetailStatus, '" + ClaimStatuses.Pending + @"') NOT IN ('" + ClaimStatuses.Approved + @"', '" + ClaimStatuses.Rejected + @"')
                       AND " + SalesApprovalDetailVersionHelper.LatestDetailWhereForD.Trim() + @")
                  + (SELECT COUNT(1)
                     FROM dbo.SalesDailyExpense e
                     INNER JOIN dbo.SalesApprovalRequestDetail d ON e.ActivityId = d.ActivityId
                     " + SalesApprovalDetailVersionHelper.LatestDetailJoinForD + @"
                     WHERE d.ApprovalRequestId = @Id
                       AND e.IsDeleted = 0
                       AND ISNULL(e.ClaimStatus, '') NOT IN ('" + ClaimStatuses.Approved + @"', '" + ClaimStatuses.Rejected + @"'))";

            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@Id", reqId);
                con.Open();
                int unresolved = Convert.ToInt32(cmd.ExecuteScalar());
                if (unresolved > 0)
                    throw new Exception("All day rows and expense lines must be Approved or Rejected before finalization.");
            }
        }

        private void BindLatestDetailSummaryCounts(DataTable dt)
        {
            int daysApproved = 0;
            int daysReturned = 0;
            int daysRejected = 0;

            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    string status = row.Table.Columns.Contains("DetailStatus") && row["DetailStatus"] != DBNull.Value
                        ? row["DetailStatus"].ToString()
                        : string.Empty;

                    if (ClaimStatuses.Equals(status, ClaimStatuses.Approved)) daysApproved++;
                    else if (ClaimStatuses.Equals(status, ClaimStatuses.Returned)) daysReturned++;
                    else if (ClaimStatuses.Equals(status, ClaimStatuses.Rejected)) daysRejected++;
                }
            }

            hfSummaryDaysApproved.Value = daysApproved.ToString();
            hfSummaryDaysReturned.Value = daysReturned.ToString();
            hfSummaryDaysRejected.Value = daysRejected.ToString();
        }

        private void EnsureHeaderCompletedForPayment(int reqId, string auditor, SqlConnection con)
        {
            using (var cmd = new SqlCommand(@"
                UPDATE dbo.SalesApprovalRequest
                SET Status = @Completed,
                    ModifiedBy = @Auditor,
                    ModifiedOn = SYSUTCDATETIME()
                WHERE ApprovalRequestId = @Id
                  AND Status = @Pending;

                IF @@ROWCOUNT > 0
                BEGIN
                    INSERT INTO dbo.ApprovalRequestStatusHistory
                    (
                          ApprovalRequestId
                        , OldStatus
                        , NewStatus
                        , ChangedBy
                        , Remarks
                    )
                    VALUES
                    (
                          @Id
                        , @Pending
                        , @Completed
                        , @Auditor
                        , N'Auditor finalized all rows before Ready for Payment.'
                    );
                END", con))
            {
                cmd.Parameters.AddWithValue("@Id", reqId);
                cmd.Parameters.AddWithValue("@Auditor", auditor ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Pending", ClaimStatuses.Pending);
                cmd.Parameters.AddWithValue("@Completed", ClaimStatuses.Completed);
                cmd.ExecuteNonQuery();
            }
        }

        protected void btnForwardLevel_Click(object sender, EventArgs e)
        {
            try
            {
                int reqId = Convert.ToInt32(hfActionRequestId.Value);
                string user = GetLoggedInEmployeeCode();
                int currentLevel = Convert.ToInt32(ViewState["CurrentUserLevel"] ?? 1);

                if (IsCurrentUserAuditor())
                    throw new Exception("Auditors forward claims only via final payment processing.");

                EnsureCanFinalizeRequest(reqId);
                ExecuteRecalculationSQL(reqId); // Keep this outside the main workflow transaction

                //using (var con = new SqlConnection(connString))
                //{
                //    // Start the Transaction
                //    using (var tran = con.BeginTransaction())
                //    {
                //        try
                //        {
                //            //    string sqlWorkflow = @"
                //            //UPDATE dbo.SalesApprovalRequestApprover 
                //            //SET Status = '" + ClaimStatuses.Approved + @"', ActionBy = @User, ActionDate = SYSUTCDATETIME() 
                //            //WHERE ApprovalRequestId = @Id AND ApproverLevel = @Level AND Status = '" + ClaimStatuses.Pending + @"';

                //            //DECLARE @NextLevel INT, @NextLogin NVARCHAR(100), @NextRole NVARCHAR(100);

                //            //SELECT TOP 1 
                //            //    @NextLevel = ApproverLevel, 
                //            //    @NextLogin = ApproverLoginId, 
                //            //    @NextRole = ApproverRole
                //            //FROM dbo.SalesApprovalRequestApprover
                //            //WHERE ApprovalRequestId = @Id AND ApproverLevel > @Level AND Status = '" + ClaimStatuses.Pending + @"'
                //            //ORDER BY ApproverLevel ASC;

                //            //IF @NextLevel IS NOT NULL
                //            //BEGIN
                //            //    UPDATE dbo.SalesApprovalRequest
                //            //    SET 
                //            //        CurrentApproverLevel = @NextLevel,
                //            //        CurrentApproverLoginId = @NextLogin,
                //            //        ModifiedBy = @User,
                //            //        ModifiedOn = SYSUTCDATETIME()
                //            //    WHERE ApprovalRequestId = @Id;

                //            //    INSERT INTO dbo.ApprovalRequestStatusHistory 
                //            //    (ApprovalRequestId, OldStatus, NewStatus, ChangedBy, Remarks)
                //            //    VALUES 
                //            //    (@Id, '" + ClaimStatuses.Pending + @"', '" + ClaimStatuses.Pending + @"', @User, CONCAT('Level ', @Level, ' approved. Forwarded to ', @NextRole, ' (', ISNULL(@NextLogin, 'Audit Pool'), ')'));
                //            //END
                //            //ELSE
                //            //BEGIN
                //            //    UPDATE dbo.SalesApprovalRequest
                //            //    SET Status = '" + ClaimStatuses.Completed + @"', ModifiedBy = @User, ModifiedOn = SYSUTCDATETIME()
                //            //    WHERE ApprovalRequestId = @Id AND Status = '" + ClaimStatuses.Pending + @"';

                //            //    INSERT INTO dbo.ApprovalRequestStatusHistory 
                //            //    (ApprovalRequestId, OldStatus, NewStatus, ChangedBy, Remarks)
                //            //    VALUES 
                //            //    (@Id, 'Pending', 'Completed', @User, 'All approval levels finished. Awaiting Payment.');
                //            //END";

                //            //    using (var cmd = new SqlCommand(sqlWorkflow, con, tran)) // Pass the transaction to the command
                //            //    {
                //            //        cmd.Parameters.AddWithValue("@Id", reqId);
                //            //        cmd.Parameters.AddWithValue("@User", user);
                //            //        cmd.Parameters.AddWithValue("@Level", currentLevel);
                //            //        cmd.ExecuteNonQuery();
                //            //    }

                //            using (SqlCommand cmd = new SqlCommand("dbo.usp_ForwardApprovalRequest", con, tran))
                //            {
                //                cmd.CommandType = CommandType.StoredProcedure;

                //                cmd.Parameters.AddWithValue(
                //                    "@ApprovalRequestId",
                //                    reqId);

                //                cmd.Parameters.AddWithValue(
                //                    "@ActionBy",
                //                    user);

                //                con.Open();

                //                cmd.ExecuteNonQuery();
                //            }

                //            // If we reach here, no errors occurred. Commit everything!
                //            tran.Commit();
                //        }
                //        catch (Exception ex)
                //        {
                //            // An error occurred during the SQL execution. Roll everything back!
                //            tran.Rollback();
                //            throw new Exception("Workflow update failed. Transaction rolled back.", ex);
                //        }
                //    }
                //}

                using (SqlConnection con = new SqlConnection(connString))
                using (SqlCommand cmd =
                    new SqlCommand("dbo.usp_ForwardApprovalRequest", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@ApprovalRequestId", reqId);
                    cmd.Parameters.AddWithValue("@ActionBy", user);

                    con.Open();

                    cmd.ExecuteNonQuery();
                }

                CloseDetailsAfterSuccessfulForward();
            }
            catch (Exception ex)
            {
                lblModalMessage.Text = "Error during handover: " + ex.Message;
                lblModalMessage.CssClass = "text-danger font-weight-bold";
                upDetails.Update();
            }
        }

        protected void btnForwardLevel_Click_OLD(object sender, EventArgs e)
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
                        SET Status = '" + ClaimStatuses.Approved + @"', ActionBy = @User, ActionDate = SYSUTCDATETIME() 
                        WHERE ApprovalRequestId = @Id AND ApproverLevel = @Level AND Status = '" + ClaimStatuses.Pending + @"';

                        DECLARE @NextLevel INT, @NextLogin NVARCHAR(100), @NextRole NVARCHAR(100);
                        
                        SELECT TOP 1 
                            @NextLevel = ApproverLevel, 
                            @NextLogin = ApproverLoginId, 
                            @NextRole = ApproverRole
                        FROM dbo.SalesApprovalRequestApprover
                        WHERE ApprovalRequestId = @Id AND ApproverLevel > @Level AND Status = '" + ClaimStatuses.Pending + @"'
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
                            (@Id, '" + ClaimStatuses.Pending + @"', '" + ClaimStatuses.Pending + @"', @User, CONCAT('Level ', @Level, ' approved. Forwarded to ', @NextRole, ' (', ISNULL(@NextLogin, 'Audit Pool'), ')'));
                        END
                        ELSE
                        BEGIN
                            UPDATE dbo.SalesApprovalRequest
                            SET Status = '" + ClaimStatuses.Completed + @"', ModifiedBy = @User, ModifiedOn = SYSUTCDATETIME()
                            WHERE ApprovalRequestId = @Id AND Status = '" + ClaimStatuses.Pending + @"';

                            INSERT INTO dbo.ApprovalRequestStatusHistory 
                            (ApprovalRequestId, OldStatus, NewStatus, ChangedBy, Remarks)
                            VALUES 
                            (@Id, '" + ClaimStatuses.Pending + @"', '" + ClaimStatuses.Completed + @"', @User, 'All approval levels finished. Awaiting Payment.');
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
                _totalStationary += (drv["Snapshot_StationaryAmount"] == DBNull.Value) ? 0m : Convert.ToDecimal(drv["Snapshot_StationaryAmount"]);

                int currentLevel = Convert.ToInt32(ViewState["CurrentUserLevel"] ?? 0);
                string detailStatus = drv["DetailStatus"].ToString();
                if (string.IsNullOrEmpty(detailStatus)) detailStatus = ClaimStatuses.Pending;
                int approvedLevel = drv["DetailApprovedLevel"] == DBNull.Value ? 0 : Convert.ToInt32(drv["DetailApprovedLevel"]);

                if (drv.Row.Table.Columns.Contains("DetailVersion") && drv["DetailVersion"] != DBNull.Value)
                {
                    int detailVersion = Convert.ToInt32(drv["DetailVersion"]);
                    if (detailVersion > 1)
                    {
                        var lblVersion = e.Row.FindControl("lblDetailVersion") as Label;
                        if (lblVersion != null)
                            lblVersion.Text = "v" + detailVersion;
                    }
                }

                var btnApprove = (Button)e.Row.FindControl("btnApproveRow");
                var btnReturn = (Button)e.Row.FindControl("btnReturnRow");
                var btnRejectDay = (Button)e.Row.FindControl("btnRejectDayRow");
                var txtRemark = (TextBox)e.Row.FindControl("txtRowRemark");
                var lnkReview = e.Row.FindControl("lnkReview") as LinkButton;

                if (btnApprove != null) btnApprove.Enabled = false;
                if (btnReturn != null) btnReturn.Enabled = false;
                if (btnRejectDay != null) btnRejectDay.Enabled = false;
                if (txtRemark != null) txtRemark.ReadOnly = true;

                string expensesJson = drv["ExpensesJson"] != DBNull.Value ? drv["ExpensesJson"].ToString() : "";
                ApplyResolvedPolicyAttributes(lnkReview, expensesJson);
                //bool hasPendingReceipts = expensesJson.Contains("\"ClaimStatus\":\"Submitted\"") ||
                //                          expensesJson.Contains("\"ClaimStatus\":\"Draft\"") ||
                //                          expensesJson.Contains("\"ClaimStatus\":\"Pending\"");

                bool hasPendingReceipts = expensesJson.Contains("\"ClaimStatus\":\"Pending\"") || expensesJson.Contains("\"ClaimStatus\":\"Returned\"");

                string remarks = drv.Row.Table.Columns.Contains("DetailRemarks") && drv["DetailRemarks"] != DBNull.Value
                    ? drv["DetailRemarks"].ToString().Trim()
                    : string.Empty;

                ApplyDayRowState(detailStatus, e.Row, approvedLevel, currentLevel, hasPendingReceipts, remarks);

                if (btnReturn != null)
                    btnReturn.Visible = !IsCurrentUserAuditor();
                if (btnRejectDay != null)
                    btnRejectDay.Visible = IsCurrentUserAuditor();

                // DYNAMIC HIERARCHY AUDITOR CHECK
                string currentRole = ViewState["CurrentUserRole"]?.ToString() ?? "";
                bool isAuditor = currentRole.Equals("Auditor", StringComparison.OrdinalIgnoreCase);

                if (isAuditor && hasPendingReceipts &&
                    !ClaimStatuses.Equals(detailStatus, ClaimStatuses.Rejected) &&
                    !ClaimStatuses.Equals(detailStatus, ClaimStatuses.Returned))
                {
                    if (lnkReview != null)
                    {
                        lnkReview.CssClass = "btn btn-sm btn-danger text-white btn-day-action pulse-warning shadow-sm";
                        lnkReview.Text = "<i class=\"fa fa-exclamation-triangle\"></i>";
                        lnkReview.ToolTip = "Pending receipts require your approval or rejection!";
                    }
                }
                else if (lnkReview != null)
                {
                    ApplyExpenseCountHint(lnkReview, expensesJson);
                }
            }
            else if (e.Row.RowType == DataControlRowType.Footer)
            {
                FillFooterTotals(e.Row);
            }
        }

        private void ApplyDayRowState(
            string detailStatus,
            GridViewRow row,
            int approvedLevel,
            int currentLevel,
            bool hasPendingReceipts,
            string remarks)
        {
            if (row == null) return;

            var btnApprove = row.FindControl("btnApproveRow") as Button;
            var btnReturn = row.FindControl("btnReturnRow") as Button;
            var btnRejectDay = row.FindControl("btnRejectDayRow") as Button;
            var txtRemark = row.FindControl("txtRowRemark") as TextBox;
            var lnkReview = row.FindControl("lnkReview") as LinkButton;

            bool isAuditor = IsCurrentUserAuditor();
            DayActionPermission perm = ResolveDayActionPermissions(
                detailStatus, approvedLevel, currentLevel, hasPendingReceipts, isAuditor);

            if (ClaimStatuses.Equals(detailStatus, ClaimStatuses.Rejected))
            {
                ApplyRejectedDayRowState(row);
                ApplyActionEnablement(btnApprove, btnReturn, btnRejectDay, perm);
            }
            else if (ClaimStatuses.Equals(detailStatus, ClaimStatuses.Returned))
            {
                ApplyReturnedDayRowState(row, remarks);
                ApplyActionEnablement(btnApprove, btnReturn, btnRejectDay, perm);
            }
            else if (ClaimStatuses.Equals(detailStatus, ClaimStatuses.Approved) && approvedLevel == currentLevel)
            {
                row.CssClass += " table-success";
                if (btnApprove != null)
                {
                    btnApprove.Text = "✔ Done";
                    btnApprove.CssClass = "btn btn-sm btn-success";
                }
                ApplyActionEnablement(btnApprove, btnReturn, btnRejectDay, perm);
            }
            else if (ClaimStatuses.Equals(detailStatus, ClaimStatuses.Approved) && approvedLevel < currentLevel)
            {
                row.CssClass += " table-warning";
                ApplyGatekeeperLogic(btnApprove, btnReturn, btnRejectDay, txtRemark, lnkReview, hasPendingReceipts, isAuditor, perm);
            }
            else if (ClaimStatuses.Equals(detailStatus, ClaimStatuses.Pending))
            {
                ApplyGatekeeperLogic(btnApprove, btnReturn, btnRejectDay, txtRemark, lnkReview, hasPendingReceipts, isAuditor, perm);
            }
        }

        /// <summary>
        /// Declarative State × Action contract. Both ApplyDayRowState and the client
        /// DayActionMatrix must stay aligned (RC-021).
        /// </summary>
        private enum DayActionRule
        {
            Disabled = 0,
            Enabled = 1,
            ManagerOnly = 2,
            AuditorOnly = 3,
            GatekeeperApprove = 4,
            DependsOnLevel = 5
        }

        private sealed class DayActionMatrixRow
        {
            public DayActionRule Approve { get; set; }
            public DayActionRule Return { get; set; }
            public DayActionRule Reject { get; set; }
        }

        private sealed class DayActionPermission
        {
            public bool CanApprove { get; set; }
            public bool CanReturn { get; set; }
            public bool CanReject { get; set; }
            public string ApproveTitle { get; set; }
            public string ReturnTitle { get; set; }
            public string RejectTitle { get; set; }
            public bool IsDoneAtLevel { get; set; }
        }

        private static readonly Dictionary<string, DayActionMatrixRow> DayActionMatrix =
            new Dictionary<string, DayActionMatrixRow>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    ClaimStatuses.Pending,
                    new DayActionMatrixRow
                    {
                        Approve = DayActionRule.GatekeeperApprove,
                        Return = DayActionRule.ManagerOnly,
                        Reject = DayActionRule.AuditorOnly
                    }
                },
                {
                    ClaimStatuses.Approved,
                    new DayActionMatrixRow
                    {
                        Approve = DayActionRule.DependsOnLevel,
                        Return = DayActionRule.DependsOnLevel,
                        Reject = DayActionRule.DependsOnLevel
                    }
                },
                {
                    ClaimStatuses.Returned,
                    new DayActionMatrixRow
                    {
                        Approve = DayActionRule.Disabled,
                        Return = DayActionRule.Disabled,
                        Reject = DayActionRule.Disabled
                    }
                },
                {
                    ClaimStatuses.Rejected,
                    new DayActionMatrixRow
                    {
                        Approve = DayActionRule.Enabled,
                        Return = DayActionRule.Disabled,
                        Reject = DayActionRule.Disabled
                    }
                }
            };

        private static DayActionPermission ResolveDayActionPermissions(
            string detailStatus,
            int approvedLevel,
            int currentLevel,
            bool hasPendingReceipts,
            bool isAuditor)
        {
            string status = string.IsNullOrEmpty(detailStatus) ? ClaimStatuses.Pending : detailStatus;
            DayActionMatrixRow row;
            if (!DayActionMatrix.TryGetValue(status, out row))
                row = DayActionMatrix[ClaimStatuses.Pending];

            bool approvedAtCurrentLevel = ClaimStatuses.Equals(status, ClaimStatuses.Approved)
                && approvedLevel == currentLevel;

            var perm = new DayActionPermission
            {
                IsDoneAtLevel = approvedAtCurrentLevel,
                CanApprove = EvaluateDayActionRule(
                    row.Approve, DayActionRule.GatekeeperApprove,
                    approvedLevel, currentLevel, hasPendingReceipts, isAuditor),
                CanReturn = EvaluateDayActionRule(
                    row.Return, DayActionRule.ManagerOnly,
                    approvedLevel, currentLevel, hasPendingReceipts, isAuditor),
                CanReject = EvaluateDayActionRule(
                    row.Reject, DayActionRule.AuditorOnly,
                    approvedLevel, currentLevel, hasPendingReceipts, isAuditor)
            };

            if (ClaimStatuses.Equals(status, ClaimStatuses.Returned))
            {
                perm.ApproveTitle = "Returned days cannot be approved until the employee resubmits.";
                perm.ReturnTitle = "Already returned to employee.";
                perm.RejectTitle = "Already returned to employee.";
            }
            else if (ClaimStatuses.Equals(status, ClaimStatuses.Rejected))
            {
                perm.ApproveTitle = perm.CanApprove ? "Approve entire day" : "";
                perm.RejectTitle = "This day has already been rejected.";
                perm.ReturnTitle = "";
            }
            else if (approvedAtCurrentLevel)
            {
                perm.ApproveTitle = "";
                perm.ReturnTitle = "";
                perm.RejectTitle = "";
            }
            else if (!perm.CanApprove && hasPendingReceipts)
            {
                perm.ApproveTitle = "You must open the folder and approve or reject all receipts first.";
                perm.ReturnTitle = perm.CanReturn ? "Return entire day to employee for correction" : "";
                perm.RejectTitle = perm.CanReject ? "Permanently reject this day" : "";
            }
            else
            {
                perm.ApproveTitle = perm.CanApprove ? "Approve entire day" : "";
                perm.ReturnTitle = perm.CanReturn ? "Return entire day to employee for correction" : "";
                perm.RejectTitle = perm.CanReject ? "Permanently reject this day" : "";
            }

            return perm;
        }

        private static bool EvaluateDayActionRule(
            DayActionRule rule,
            DayActionRule dependsFallback,
            int approvedLevel,
            int currentLevel,
            bool hasPendingReceipts,
            bool isAuditor)
        {
            if (rule == DayActionRule.DependsOnLevel)
            {
                if (approvedLevel == currentLevel)
                    return false;
                if (approvedLevel < currentLevel)
                    return EvaluateDayActionRule(
                        dependsFallback, dependsFallback,
                        approvedLevel, currentLevel, hasPendingReceipts, isAuditor);
                return false;
            }

            switch (rule)
            {
                case DayActionRule.Enabled:
                    return true;
                case DayActionRule.ManagerOnly:
                    return !isAuditor;
                case DayActionRule.AuditorOnly:
                    return isAuditor;
                case DayActionRule.GatekeeperApprove:
                    return !hasPendingReceipts;
                default:
                    return false;
            }
        }

        private static void ApplyActionEnablement(
            Button btnApprove,
            Button btnReturn,
            Button btnRejectDay,
            DayActionPermission perm)
        {
            if (perm == null) return;

            if (btnApprove != null)
            {
                btnApprove.Enabled = perm.CanApprove;
                if (!string.IsNullOrEmpty(perm.ApproveTitle))
                    btnApprove.ToolTip = perm.ApproveTitle;
            }

            if (btnReturn != null)
            {
                btnReturn.Enabled = perm.CanReturn;
                if (!string.IsNullOrEmpty(perm.ReturnTitle))
                    btnReturn.ToolTip = perm.ReturnTitle;
            }

            if (btnRejectDay != null)
            {
                btnRejectDay.Enabled = perm.CanReject;
                if (!string.IsNullOrEmpty(perm.RejectTitle))
                    btnRejectDay.ToolTip = perm.RejectTitle;
            }
        }

        private static void ApplyRejectedDayRowState(GridViewRow row)
        {
            if (row == null) return;

            string css = (row.CssClass ?? "").Replace("table-success", "").Replace("table-warning", "").Trim();
            if (css.IndexOf("table-danger", StringComparison.OrdinalIgnoreCase) < 0)
                css = (css + " table-danger").Trim();
            row.CssClass = css;

            var btnApprove = row.FindControl("btnApproveRow") as Button;
            if (btnApprove != null)
            {
                btnApprove.Text = "Approve";
                btnApprove.ToolTip = "Approve entire day";
                btnApprove.CssClass = "btn btn-sm btn-success pulse-success text-white";
            }

            var btnRejectDay = row.FindControl("btnRejectDayRow") as Button;
            if (btnRejectDay != null)
            {
                btnRejectDay.Text = "Rejected";
                btnRejectDay.ToolTip = "This day has already been rejected.";
                btnRejectDay.CssClass = "btn btn-sm btn-outline-danger mt-1 claims-ux-postback-btn";
            }

            var txtRemark = row.FindControl("txtRowRemark") as TextBox;
            if (txtRemark != null) txtRemark.ReadOnly = false;

            var lnkReview = row.FindControl("lnkReview") as LinkButton;
            if (lnkReview != null)
                lnkReview.Attributes["data-status"] = ClaimStatuses.Rejected;
        }

        private static void ApplyReturnedDayRowState(GridViewRow row, string remarks)
        {
            if (row == null) return;

            string css = (row.CssClass ?? "").Replace("table-success", "").Trim();
            if (css.IndexOf("table-warning", StringComparison.OrdinalIgnoreCase) < 0)
                css = (css + " table-warning").Trim();
            row.CssClass = css;

            var lblReturnedBadge = row.FindControl("lblReturnedBadge") as Label;
            if (lblReturnedBadge != null) lblReturnedBadge.Visible = true;

            var lblReturnedReason = row.FindControl("lblReturnedReason") as Label;
            if (lblReturnedReason != null && !string.IsNullOrWhiteSpace(remarks))
            {
                lblReturnedReason.Visible = true;
                lblReturnedReason.Text = "Reason: " + HttpUtility.HtmlEncode(remarks);
            }

            var btnApprove = row.FindControl("btnApproveRow") as Button;
            if (btnApprove != null)
            {
                btnApprove.Text = "Approve";
                btnApprove.ToolTip = "Returned days cannot be approved until the employee resubmits.";
                btnApprove.CssClass = "btn btn-sm btn-outline-secondary";
            }

            var btnReturn = row.FindControl("btnReturnRow") as Button;
            if (btnReturn != null)
            {
                btnReturn.Text = "Already Returned";
                btnReturn.ToolTip = "Already returned to employee.";
            }

            var lnkReview = row.FindControl("lnkReview") as LinkButton;
            if (lnkReview != null)
                lnkReview.Attributes["data-status"] = ClaimStatuses.Returned;
        }

        private void ApplyGatekeeperLogic(
            Button btnApprove,
            Button btnReturn,
            Button btnRejectDay,
            TextBox txtRemark,
            LinkButton lnkReview,
            bool hasPendingReceipts,
            bool isAuditor,
            DayActionPermission perm)
        {
            if (txtRemark != null) txtRemark.ReadOnly = false;

            if (perm == null)
            {
                perm = ResolveDayActionPermissions(
                    ClaimStatuses.Pending, 0, 0, hasPendingReceipts, isAuditor);
            }

            ApplyActionEnablement(btnApprove, btnReturn, btnRejectDay, perm);

            if (btnApprove != null)
            {
                if (perm.CanApprove)
                {
                    btnApprove.CssClass = "btn btn-sm btn-success pulse-success text-white";
                }
                else
                {
                    btnApprove.CssClass = "btn btn-sm btn-outline-secondary";
                }
            }

            if (hasPendingReceipts)
            {
                if (lnkReview != null)
                {
                    if (!lnkReview.CssClass.Contains("pulse-info"))
                        lnkReview.CssClass += " pulse-info";
                }
            }
            else if (lnkReview != null)
            {
                lnkReview.CssClass = lnkReview.CssClass.Replace("pulse-info", "").Trim();
            }
        }

        private sealed class BulkDetailDbContext
        {
            public int ApprovalRequestDetailId { get; set; }
            public int ActivityId { get; set; }
            public bool ManualReview { get; set; }
            public string DetailStatus { get; set; }
            public int DetailApprovedLevel { get; set; }
            public IList<string> ExpenseStatuses { get; set; }
            public bool IsLatestDetailVersion { get; set; }
        }

        private Dictionary<int, BulkDetailDbContext> LoadBulkDetailContextsFromDatabase(int reqId, IList<int> detailIds)
        {
            var result = new Dictionary<int, BulkDetailDbContext>();
            if (detailIds == null || detailIds.Count == 0)
                return result;

            var wanted = new HashSet<int>(detailIds);

            using (var con = new SqlConnection(connString))
            {
                con.Open();

                var spDetails = new DataTable();
                using (var cmd = new SqlCommand("dbo.usp_GetApprovalRequestDetails", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ApprovalRequestId", reqId);
                    using (var da = new SqlDataAdapter(cmd))
                        da.Fill(spDetails);
                }

                spDetails = SalesApprovalDetailVersionHelper.KeepLatestPerActivity(spDetails);

                foreach (DataRow row in spDetails.Rows)
                {
                    int detailId = Convert.ToInt32(row["ApprovalRequestDetailId"]);
                    if (!wanted.Contains(detailId))
                        continue;

                    result[detailId] = new BulkDetailDbContext
                    {
                        ApprovalRequestDetailId = detailId,
                        ActivityId = Convert.ToInt32(row["ActivityId"]),
                        ManualReview = row.Table.Columns.Contains("ManualReview")
                            && row["ManualReview"] != DBNull.Value
                            && Convert.ToBoolean(row["ManualReview"]),
                        DetailStatus = row["DetailStatus"] == DBNull.Value
                            ? ClaimStatuses.Pending
                            : row["DetailStatus"].ToString(),
                        DetailApprovedLevel = row.Table.Columns.Contains("DetailApprovedLevel") && row["DetailApprovedLevel"] != DBNull.Value
                            ? Convert.ToInt32(row["DetailApprovedLevel"])
                            : 0,
                        IsLatestDetailVersion = true
                    };
                }

                foreach (int detailId in detailIds)
                {
                    if (result.ContainsKey(detailId))
                        continue;

                    result[detailId] = new BulkDetailDbContext
                    {
                        ApprovalRequestDetailId = detailId,
                        IsLatestDetailVersion = false
                    };
                }

                var activityIds = result.Values
                    .Where(c => c.IsLatestDetailVersion && c.ActivityId > 0)
                    .Select(c => c.ActivityId)
                    .Distinct()
                    .ToList();

                if (activityIds.Count > 0)
                {
                    var expenseMap = LoadExpenseStatusesByActivity(con, activityIds);
                    foreach (var ctx in result.Values.Where(c => c.IsLatestDetailVersion))
                    {
                        List<string> statuses;
                        ctx.ExpenseStatuses = expenseMap.TryGetValue(ctx.ActivityId, out statuses)
                            ? statuses
                            : new List<string>();
                    }
                }
                else
                {
                    foreach (var ctx in result.Values.Where(c => c.IsLatestDetailVersion))
                        ctx.ExpenseStatuses = new List<string>();
                }
            }

            return result;
        }

        private static Dictionary<int, List<string>> LoadExpenseStatusesByActivity(SqlConnection con, IList<int> activityIds)
        {
            var map = activityIds.ToDictionary(id => id, id => new List<string>());
            if (activityIds.Count == 0)
                return map;

            var sql = new StringBuilder();
            sql.Append(@"SELECT ActivityId, ISNULL(ClaimStatus, '') AS ClaimStatus
                         FROM dbo.SalesDailyExpense
                         WHERE IsDeleted = 0 AND ActivityId IN (");
            for (int i = 0; i < activityIds.Count; i++)
            {
                if (i > 0) sql.Append(",");
                sql.Append("@a").Append(i);
            }
            sql.Append(")");

            using (var cmd = new SqlCommand(sql.ToString(), con))
            {
                for (int i = 0; i < activityIds.Count; i++)
                    cmd.Parameters.AddWithValue("@a" + i, activityIds[i]);

                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        int activityId = Convert.ToInt32(rdr["ActivityId"]);
                        map[activityId].Add(rdr["ClaimStatus"].ToString());
                    }
                }
            }

            return map;
        }

        private bool IsDetailActionableAtCurrentLevel(BulkDetailDbContext ctx, int currentLevel)
        {
            if (ctx == null || !ctx.IsLatestDetailVersion)
                return false;

            string status = ctx.DetailStatus ?? ClaimStatuses.Pending;
            if (ClaimStatuses.Equals(status, ClaimStatuses.Rejected) || ClaimStatuses.Equals(status, ClaimStatuses.Returned))
                return false;
            if (ClaimStatuses.Equals(status, ClaimStatuses.Approved) && ctx.DetailApprovedLevel == currentLevel)
                return false;

            return ClaimStatuses.Equals(status, ClaimStatuses.Pending)
                || (ClaimStatuses.Equals(status, ClaimStatuses.Approved) && ctx.DetailApprovedLevel < currentLevel);
        }

        private static bool HasPendingReceiptsFromDb(IList<string> expenseStatuses)
        {
            if (expenseStatuses == null || expenseStatuses.Count == 0)
                return false;

            return expenseStatuses.Any(st =>
                ClaimStatuses.Equals(st, ClaimStatuses.Pending) ||
                ClaimStatuses.Equals(st, ClaimStatuses.Returned));
        }

        private static bool AreAllExpensesApprovedFromDb(IList<string> expenseStatuses)
        {
            if (expenseStatuses == null || expenseStatuses.Count == 0)
                return true;

            return expenseStatuses.All(st => ClaimStatuses.Equals(st, ClaimStatuses.Approved));
        }

        private static string GetManualExpenseSkipReasonFromDb(IList<string> expenseStatuses)
        {
            if (expenseStatuses == null || expenseStatuses.Count == 0)
                return "Manual expenses still require review.";

            bool hasPending = false;
            bool hasReturned = false;
            bool hasDraftOrNull = false;

            foreach (var status in expenseStatuses)
            {
                if (ClaimStatuses.Equals(status, ClaimStatuses.Approved) ||
                    ClaimStatuses.Equals(status, ClaimStatuses.Rejected))
                {
                    continue;
                }

                if (ClaimStatuses.Equals(status, ClaimStatuses.Returned))
                    hasReturned = true;
                else if (ClaimStatuses.Equals(status, ClaimStatuses.Pending) ||
                         ClaimStatuses.Equals(status, ClaimStatuses.Submitted))
                    hasPending = true;
                else
                    hasDraftOrNull = true;
            }

            int unresolvedCategories = 0;
            if (hasReturned) unresolvedCategories++;
            if (hasPending) unresolvedCategories++;
            if (hasDraftOrNull) unresolvedCategories++;

            if (unresolvedCategories > 1)
                return "Mixed expense states";
            if (hasReturned)
                return "Returned expenses";
            if (hasPending)
                return "Pending manual expenses";
            return "Manual expenses still require review.";
        }

        private bool TryEvaluateBulkEligibility(BulkDetailDbContext ctx, DayDetailAction action, out string skipReason)
        {
            skipReason = null;
            int currentLevel = Convert.ToInt32(ViewState["CurrentUserLevel"] ?? 0);

            if (action == DayDetailAction.Return && ctx != null && ClaimStatuses.Equals(ctx.DetailStatus, ClaimStatuses.Returned))
            {
                skipReason = "Selected activities are already returned.";
                return false;
            }

            if (action == DayDetailAction.Approve && ctx != null && ClaimStatuses.Equals(ctx.DetailStatus, ClaimStatuses.Returned))
            {
                skipReason = "Returned days must be resubmitted before approval.";
                return false;
            }

            if (!IsDetailActionableAtCurrentLevel(ctx, currentLevel))
            {
                skipReason = "Already processed";
                return false;
            }

            switch (action)
            {
                case DayDetailAction.Approve:
                    if (ctx.ManualReview && !AreAllExpensesApprovedFromDb(ctx.ExpenseStatuses))
                    {
                        skipReason = GetManualExpenseSkipReasonFromDb(ctx.ExpenseStatuses);
                        return false;
                    }

                    if (HasPendingReceiptsFromDb(ctx.ExpenseStatuses))
                    {
                        skipReason = GetManualExpenseSkipReasonFromDb(ctx.ExpenseStatuses);
                        return false;
                    }

                    return true;

                case DayDetailAction.Return:
                    return true;

                case DayDetailAction.Reject:
                    return true;

                default:
                    skipReason = "Already processed";
                    return false;
            }
        }

        private static string BuildBulkResultSummary(DayDetailAction action, int processedCount, Dictionary<string, int> skipReasons)
        {
            int approved = action == DayDetailAction.Approve ? processedCount : 0;
            int rejected = action == DayDetailAction.Reject ? processedCount : 0;
            int returned = action == DayDetailAction.Return ? processedCount : 0;
            int skipped = skipReasons.Values.Sum();

            var sb = new System.Text.StringBuilder();
            sb.AppendFormat("Approved: {0} | Rejected: {1} | Returned: {2} | Skipped: {3}",
                approved, rejected, returned, skipped);

            if (skipped > 0)
            {
                sb.Append("<br/><strong>Skipped Reasons</strong><br/>");
                foreach (var entry in skipReasons.Where(x => x.Value > 0).OrderBy(x => x.Key))
                    sb.AppendFormat("- {0}: {1}<br/>", entry.Key, entry.Value);
            }

            return sb.ToString();
        }

        private static Dictionary<string, int> CreateBulkSkipReasonMap()
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Already processed"] = 0,
                ["Selected activities are already returned."] = 0,
                ["Pending manual expenses"] = 0,
                ["Returned expenses"] = 0,
                ["Mixed expense states"] = 0,
                ["Manual expenses still require review"] = 0
            };
        }

        private static void RecordBulkSkip(Dictionary<string, int> skipReasons, string skipReason)
        {
            if (string.IsNullOrWhiteSpace(skipReason))
                skipReason = "Already processed";

            if (!skipReasons.ContainsKey(skipReason))
                skipReasons[skipReason] = 0;

            skipReasons[skipReason]++;
        }

        private void FillFooterTotals(GridViewRow footer)
        {
            for (int i = 0; i < gvDetails.Columns.Count; i++)
            {
                string headerText = gvDetails.Columns[i].HeaderText ?? "";

                if (headerText.Contains("Dist")) // Changed from "Distance" to "Dist"
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
                else if (headerText.Contains("Stat")) // <-- ADD THIS FOR STATIONARY FOOTER
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
            if (e.CommandName == "ApproveRow" || e.CommandName == "ReturnRow" || e.CommandName == "RejectDayRow")
            {
                int detailId = Convert.ToInt32(e.CommandArgument);

                GridViewRow row =
                    (GridViewRow)((Control)e.CommandSource).NamingContainer;

                TextBox txt =
                    row.FindControl("txtRowRemark") as TextBox;

                string user = GetLoggedInEmployeeCode();
                string rawRemark = txt != null ? txt.Text : "";
                string remark = AppendUserToRemark(rawRemark, user);
                int reqId = Convert.ToInt32(hfActionRequestId.Value);

                DayDetailAction action;
                if (e.CommandName == "ApproveRow")
                    action = DayDetailAction.Approve;
                else if (e.CommandName == "ReturnRow")
                    action = DayDetailAction.Return;
                else
                    action = DayDetailAction.Reject;

                try
                {
                    if (action == DayDetailAction.Approve && ShouldConfirmForwardToNextApprover(reqId, detailId))
                    {
                        hfPendingBulkApprove.Value = "0";
                        hfPendingApproveDetailId.Value = detailId.ToString();
                        hfPendingApproveRemark.Value = remark ?? "";
                        ScriptManager.RegisterStartupScript(upDetails, upDetails.GetType(), "showForwardNext",
                            "showForwardNextApproverModal();", true);
                        upDetails.Update();
                        return;
                    }

                    ProcessDayDetailAction(detailId, reqId, action, remark);
                    if (action == DayDetailAction.Return)
                    {
                        ApplyDayRowState(ClaimStatuses.Returned, row, 0, 0, false, remark);
                        ScriptManager.RegisterStartupScript(upDetails, upDetails.GetType(), "pnotifyReturned",
                            "closeClaimWorkflowAfterReturn();showReturnedSuccessNotify();", true);
                    }

                    ExecuteRecalculationSQL(reqId);
                    BindDetailsGrid(reqId);
                    upDetails.Update();
                    BindRequestsGrid();
                }
                catch (Exception ex)
                {
                    lblModalMessage.Text = ex.Message;
                    lblModalMessage.CssClass = "text-danger font-weight-bold";
                    upDetails.Update();
                }
            }
        }

        protected void gvDetails_RowCommand_OLD300726(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "ApproveRow" || e.CommandName == "RejectRow")
            {
                int detailId = Convert.ToInt32(e.CommandArgument);
                bool isApprove = (e.CommandName == "ApproveRow");

                GridViewRow row = (GridViewRow)((Control)e.CommandSource).NamingContainer;
                TextBox txt = row.FindControl("txtRowRemark") as TextBox;

                string dbStatus = isApprove ? ClaimStatuses.Approved : ClaimStatuses.Rejected;
                string user = GetLoggedInEmployeeCode();
                string rawRemark = txt != null ? txt.Text : "";
                string remark = AppendUserToRemark(rawRemark, user);

                int currentLevel = Convert.ToInt32(ViewState["CurrentUserLevel"] ?? 0);
                int reqId = Convert.ToInt32(hfActionRequestId.Value);

                //using (var con = new SqlConnection(connString))
                //{
                //    con.Open();
                //    using (var tran = con.BeginTransaction())
                //    {
                //        try
                //        {
                //            // 1. Level-Aware Inline SQL (Replaces the rigid SP)
                //            string sql = @"
                //        UPDATE dbo.SalesApprovalRequestDetail 
                //        SET DetailStatus = @Status, 
                //            DetailRemarks = ISNULL(@Remarks, DetailRemarks), 
                //            DetailApprovedBy = @ActionBy, 
                //            DetailApprovedOn = SYSUTCDATETIME(),
                //            DetailApprovedLevel = @Level
                //        WHERE ApprovalRequestDetailId = @DetailId AND ApprovalRequestId = @ReqId;

                //        -- 2. Cascade down to child receipts
                //        UPDATE dbo.SalesDailyExpense 
                //        SET ClaimStatus = @Status, 
                //            ClaimProcessedBy = @ActionBy, 
                //            ClaimProcessedDate = DATEADD(MINUTE, 330, SYSUTCDATETIME()),
                //            ModifiedBy = @ActionBy,
                //            ModifiedOn = SYSUTCDATETIME(),
                //            Remarks = ISNULL(Remarks, @Remarks)
                //        WHERE ActivityId = (SELECT ActivityId FROM dbo.SalesApprovalRequestDetail WHERE ApprovalRequestDetailId = @DetailId) 
                //          AND ClaimStatus NOT IN ('" + ClaimStatuses.Approved + @"', '" + ClaimStatuses.Rejected + @"') 
                //          AND IsDeleted = 0;";

                //            using (var cmd = new SqlCommand(sql, con, tran))
                //            {
                //                cmd.Parameters.AddWithValue("@Status", dbStatus);
                //                cmd.Parameters.AddWithValue("@Remarks", string.IsNullOrEmpty(remark) ? (object)DBNull.Value : remark);
                //                cmd.Parameters.AddWithValue("@ActionBy", user);
                //                cmd.Parameters.AddWithValue("@Level", currentLevel);
                //                cmd.Parameters.AddWithValue("@DetailId", detailId);
                //                cmd.Parameters.AddWithValue("@ReqId", reqId);

                //                cmd.ExecuteNonQuery();
                //            }

                //            tran.Commit();
                //        }
                //        catch (Exception ex)
                //        {
                //            tran.Rollback();
                //            throw new Exception("Inline action failed. Transaction rolled back.", ex);
                //        }
                //    }
                //}

                using (SqlConnection con = new SqlConnection(connString))
                using (SqlCommand cmd = new SqlCommand("dbo.usp_ProcessApprovalAction", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@ApprovalRequestDetailId", detailId);
                    cmd.Parameters.AddWithValue("@Action", isApprove ? "APPROVE" : "REJECT");
                    cmd.Parameters.AddWithValue("@ActionBy", user);
                    cmd.Parameters.AddWithValue("@Remarks",
                        string.IsNullOrWhiteSpace(remark)
                            ? (object)DBNull.Value
                            : remark);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        protected void gvDetails_RowCommand_OLD(object sender, GridViewCommandEventArgs e)
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
                          AND ClaimStatus NOT IN ('" + ClaimStatuses.Approved + @"', '" + ClaimStatuses.Rejected + @"') 
                          AND IsDeleted = 0;";

                    using (var cascadeCmd = new SqlCommand(cascadeSql, con))
                    {
                        cascadeCmd.Parameters.AddWithValue("@ActionStatus", isApprove ? ClaimStatuses.Approved : ClaimStatuses.Rejected);
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

        protected void btnApproveSelected_Click(object sender, EventArgs e) { ProcessBulkAction(DayDetailAction.Approve); }
        protected void btnReturnSelected_Click(object sender, EventArgs e) { ProcessBulkAction(DayDetailAction.Return); }
        protected void btnRejectDaysSelected_Click(object sender, EventArgs e) { ProcessBulkAction(DayDetailAction.Reject); }

        private void ProcessBulkAction(DayDetailAction action)
        {
            ProcessBulkAction(action, false);
        }

        private void ProcessBulkAction(DayDetailAction action, bool forwardConfirmed)
        {
            try
            {
                if (string.IsNullOrEmpty(hfActionRequestId.Value)) return;

                int reqId = Convert.ToInt32(hfActionRequestId.Value);
                string auditor = GetLoggedInEmployeeCode();
                string rawRemarks = txtBulkRemarks.Text.Trim();
                string remarks = AppendUserToRemark(rawRemarks, auditor);
                int processedCount = 0;
                var skipReasons = CreateBulkSkipReasonMap();
                var rowsToProcess = new List<Tuple<int, string>>();

                if (action == DayDetailAction.Return && IsCurrentUserAuditor())
                {
                    lblModalMessage.Text = "Auditors cannot return days.";
                    lblModalMessage.CssClass = "text-warning font-weight-bold";
                    upDetails.Update();
                    return;
                }

                if (action == DayDetailAction.Reject && !IsCurrentUserAuditor())
                {
                    lblModalMessage.Text = "Only auditors can reject days.";
                    lblModalMessage.CssClass = "text-warning font-weight-bold";
                    upDetails.Update();
                    return;
                }

                if (action != DayDetailAction.Approve && string.IsNullOrWhiteSpace(remarks))
                {
                    lblModalMessage.Text = action == DayDetailAction.Reject
                        ? "Bulk reject remarks are mandatory."
                        : "Bulk return remarks are mandatory.";
                    lblModalMessage.CssClass = "text-warning font-weight-bold";
                    upDetails.Update();
                    return;
                }

                var selectedRows = new List<Tuple<int, string>>();

                foreach (GridViewRow row in gvDetails.Rows)
                {
                    if (row.RowType != DataControlRowType.DataRow) continue;

                    var chk = row.FindControl("chkSelect") as CheckBox;
                    if (chk == null || !chk.Checked) continue;

                    int detailId = Convert.ToInt32(gvDetails.DataKeys[row.RowIndex].Value);
                    string rowRemark = remarks;
                    if (action != DayDetailAction.Approve)
                    {
                        var txtRowRemark = row.FindControl("txtRowRemark") as TextBox;
                        if (txtRowRemark != null && !string.IsNullOrWhiteSpace(txtRowRemark.Text))
                            rowRemark = AppendUserToRemark(txtRowRemark.Text.Trim(), auditor);
                    }

                    selectedRows.Add(Tuple.Create(detailId, rowRemark));
                }

                var detailIds = selectedRows.Select(x => x.Item1).ToList();
                var dbContexts = LoadBulkDetailContextsFromDatabase(reqId, detailIds);

                foreach (var selectedRow in selectedRows)
                {
                    int detailId = selectedRow.Item1;
                    BulkDetailDbContext ctx;
                    if (!dbContexts.TryGetValue(detailId, out ctx))
                        ctx = null;

                    string skipReason;
                    if (!TryEvaluateBulkEligibility(ctx, action, out skipReason))
                    {
                        RecordBulkSkip(skipReasons, skipReason);
                        continue;
                    }

                    rowsToProcess.Add(selectedRow);
                }

                int alreadyReturnedCount;
                skipReasons.TryGetValue("Selected activities are already returned.", out alreadyReturnedCount);
                if (action == DayDetailAction.Return && rowsToProcess.Count == 0 && selectedRows.Count > 0 && alreadyReturnedCount == selectedRows.Count)
                {
                    lblModalMessage.Text = "Selected activities are already returned.";
                    lblModalMessage.CssClass = "text-warning font-weight-bold";
                    BindDetailsGrid(reqId);
                    upDetails.Update();
                    return;
                }

                if (rowsToProcess.Count > 0)
                {
                    if (action == DayDetailAction.Approve && !forwardConfirmed
                        && ShouldConfirmForwardToNextApprover(reqId, rowsToProcess.Select(x => x.Item1).ToList()))
                    {
                        hfPendingBulkApprove.Value = "1";
                        hfPendingApproveDetailId.Value = "";
                        hfPendingApproveRemark.Value = "";
                        ScriptManager.RegisterStartupScript(upDetails, upDetails.GetType(), "showForwardNext",
                            "showForwardNextApproverModal();", true);
                        upDetails.Update();
                        return;
                    }

                    using (var con = new SqlConnection(connString))
                    {
                        con.Open();
                        using (var tran = con.BeginTransaction())
                        {
                            try
                            {
                                foreach (var rowToProcess in rowsToProcess)
                                {
                                    ProcessDayDetailAction(rowToProcess.Item1, reqId, action, rowToProcess.Item2, con, tran);
                                    processedCount++;
                                }

                                tran.Commit();
                            }
                            catch
                            {
                                tran.Rollback();
                                throw;
                            }
                        }
                    }

                    ExecuteRecalculationSQL(reqId);
                    txtBulkRemarks.Text = "";

                    if (action == DayDetailAction.Return)
                    {
                        ScriptManager.RegisterStartupScript(upDetails, upDetails.GetType(), "pnotifyReturnedBulk",
                            "closeClaimWorkflowAfterReturn();showReturnedSuccessNotify();", true);
                    }
                }

                int skippedCount = skipReasons.Values.Sum();
                if (forwardConfirmed && processedCount > 0)
                {
                    CloseDetailsAfterSuccessfulForward();
                    return;
                }

                if (processedCount == 0 && skippedCount == 0)
                {
                    lblModalMessage.Text = "Please select at least one row to apply the bulk action.";
                    lblModalMessage.CssClass = "text-warning font-weight-bold";
                }
                else
                {
                    lblModalMessage.Text = BuildBulkResultSummary(action, processedCount, skipReasons);
                    lblModalMessage.CssClass = processedCount > 0
                        ? (action == DayDetailAction.Approve
                            ? "text-success font-weight-bold"
                            : (action == DayDetailAction.Reject ? "text-danger font-weight-bold" : "text-warning font-weight-bold"))
                        : "text-warning font-weight-bold";
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

        private void ProcessBulkAction_OLD(string newStatus)
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
                                      AND ClaimStatus NOT IN ('" + ClaimStatuses.Approved + @"', '" + ClaimStatuses.Rejected + @"') 
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
                    lblModalMessage.CssClass = newStatus == ClaimStatuses.Approved ? "text-success font-weight-bold" : "text-danger font-weight-bold";
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
            return (Session["WORKMAN"] ?? Session["workman"]) as string;
        }

        private static string GetLoggedInEmployeeCodeFromSession()
        {
            var ctx = HttpContext.Current;
            if (ctx?.Session == null) return null;
            return (ctx.Session["WORKMAN"] ?? ctx.Session["workman"]) as string;
        }

        private static string GetLatestDetailStatus(int detailId, int reqId)
        {
            using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            using (var cmd = new SqlCommand(
                @"SELECT d.DetailStatus
                  FROM dbo.SalesApprovalRequestDetail d
                  WHERE d.ApprovalRequestId = @Id
                    AND d.ActivityId = (SELECT ActivityId FROM dbo.SalesApprovalRequestDetail WHERE ApprovalRequestDetailId = @DetailId)
                    AND " + SalesApprovalDetailVersionHelper.LatestDetailWhereForD.Trim(), con))
            {
                cmd.Parameters.AddWithValue("@Id", reqId);
                cmd.Parameters.AddWithValue("@DetailId", detailId);
                con.Open();
                object val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? string.Empty : val.ToString().Trim();
            }
        }

        private void ProcessDayDetailAction(int detailId, int reqId, DayDetailAction action, string remark, SqlConnection con = null, SqlTransaction tran = null)
        {
            string user = GetLoggedInEmployeeCode();
            if (string.IsNullOrWhiteSpace(user))
                throw new Exception("User session expired. Please log in again.");

            if (action == DayDetailAction.Return && IsCurrentUserAuditor())
                throw new Exception("Auditors cannot return days. Managers return days for employee correction.");

            if (action == DayDetailAction.Return)
            {
                string detailStatus = GetLatestDetailStatus(detailId, reqId);
                if (ClaimStatuses.Equals(detailStatus, ClaimStatuses.Returned))
                    return;
            }

            if (action == DayDetailAction.Approve)
            {
                string detailStatus = GetLatestDetailStatus(detailId, reqId);
                if (ClaimStatuses.Equals(detailStatus, ClaimStatuses.Returned))
                    throw new Exception("Returned days cannot be approved until the employee resubmits.");
            }

            if (action == DayDetailAction.Reject && !IsCurrentUserAuditor())
                throw new Exception("Only auditors can reject days.");

            if (action != DayDetailAction.Approve && string.IsNullOrWhiteSpace(remark))
                throw new Exception(action == DayDetailAction.Reject
                    ? "Remarks are required when rejecting a day."
                    : "Remarks are required when returning a day.");

            bool ownsConnection = con == null;
            if (ownsConnection)
            {
                con = new SqlConnection(connString);
                con.Open();
            }

            try
            {
                switch (action)
                {
                    case DayDetailAction.Approve:
                        ExecuteProcessApprovalAction(detailId, "APPROVE", remark, user, con, tran);
                        break;
                    case DayDetailAction.Return:
                        // Activity/expense sync is handled inside usp_ProcessApprovalAction.
                        ExecuteProcessApprovalAction(detailId, "RETURN", remark, user, con, tran);
                        break;
                    case DayDetailAction.Reject:
                        ExecuteProcessApprovalAction(detailId, "REJECT", remark, user, con, tran);
                        break;
                }
            }
            finally
            {
                if (ownsConnection)
                    con.Dispose();
            }
        }

        private static void ExecuteProcessSalesApprovalRequestDetail(int detailId, int reqId, string action, string remark, string user, SqlConnection con, SqlTransaction tran = null)
        {
            using (var cmd = new SqlCommand("dbo.usp_ProcessSalesApprovalRequestDetail", con, tran))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ApprovalRequestDetailId", detailId);
                cmd.Parameters.AddWithValue("@ApprovalRequestId", reqId);
                cmd.Parameters.AddWithValue("@Action", action);
                cmd.Parameters.AddWithValue("@ActionBy", user);
                cmd.Parameters.AddWithValue("@ActionRemarks", string.IsNullOrWhiteSpace(remark) ? (object)DBNull.Value : remark);
                cmd.ExecuteNonQuery();
            }
        }

        private static void ExecuteProcessApprovalAction(int detailId, string action, string remark, string user, SqlConnection con, SqlTransaction tran = null)
        {
            using (var cmd = new SqlCommand("dbo.usp_ProcessApprovalAction", con, tran))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ApprovalRequestDetailId", detailId);
                cmd.Parameters.AddWithValue("@Action", action);
                cmd.Parameters.AddWithValue("@ActionBy", user);
                cmd.Parameters.AddWithValue("@Remarks", string.IsNullOrWhiteSpace(remark) ? (object)DBNull.Value : remark);
                cmd.ExecuteNonQuery();
            }
        }

        private static void MarkActivityRejectedForDay(int detailId, string user, SqlConnection con, SqlTransaction tran = null)
        {
            const string sql = @"
                DECLARE @ActivityId INT;
                SELECT @ActivityId = ActivityId
                FROM dbo.SalesApprovalRequestDetail
                WHERE ApprovalRequestDetailId = @DetailId;

                IF @ActivityId IS NOT NULL
                BEGIN
                    UPDATE dbo.SalesDailyActivity
                    SET ClaimStatus = '" + ClaimStatuses.Rejected + @"',
                        ModifiedByUserId = @User,
                        ModifiedDateTime = DATEADD(MINUTE, 330, SYSUTCDATETIME())
                    WHERE ActivityId = @ActivityId;

                    UPDATE dbo.SalesDailyExpense
                    SET ClaimStatus = '" + ClaimStatuses.Rejected + @"',
                        ModifiedBy = @User,
                        ModifiedOn = SYSUTCDATETIME()
                    WHERE ActivityId = @ActivityId
                      AND IsDeleted = 0
                      AND ISNULL(ClaimStatus, '') <> '" + ClaimStatuses.Approved + @"';
                END";

            using (var cmd = new SqlCommand(sql, con, tran))
            {
                cmd.Parameters.AddWithValue("@DetailId", detailId);
                cmd.Parameters.AddWithValue("@User", user);
                cmd.ExecuteNonQuery();
            }
        }

        private static string MapExpenseActionToClaimStatus(string action)
        {
            switch ((action ?? "").Trim().ToUpperInvariant())
            {
                case "APPROVE": return ClaimStatuses.Approved;
                case "RETURN": return ClaimStatuses.Returned;
                case "REJECT": return ClaimStatuses.Rejected;
                default: throw new ArgumentException("Invalid expense action.");
            }
        }

        protected void btnMarkReadyForPayment_Click_OLD(object sender, EventArgs e)
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
                        SET Status = '" + ClaimStatuses.Completed + @"' 
                        WHERE ApprovalRequestId = @Id AND Status = '" + ClaimStatuses.Pending + @"';

                        IF @@ROWCOUNT > 0
                        BEGIN
                            INSERT INTO dbo.ApprovalRequestStatusHistory 
                            (ApprovalRequestId, OldStatus, NewStatus, ChangedBy, Remarks)
                            VALUES 
                            (@Id, '" + ClaimStatuses.Pending + @"', '" + ClaimStatuses.Completed + @"', @AuditorId, 'Auditor finalized all rows and synced calculations.');
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
                            Status = '" + ClaimStatuses.Approved + @"', 
                            ActionBy = @AuditorId, 
                            ActionDate = GETDATE() 
                        WHERE ApprovalRequestId = @Id 
                          AND ApproverLevel = 2 
                          AND Status = '" + ClaimStatuses.Pending + @"'";

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

        protected void btnMarkReadyForPayment_Click(object sender, EventArgs e)
        {
            try
            {
                int reqId = Convert.ToInt32(hfActionRequestId.Value);
                string auditor = GetLoggedInEmployeeCode();

                if (!IsCurrentUserAuditor())
                    throw new Exception("Only auditors can mark a claim ready for payment.");

                EnsureCanFinalizeRequest(reqId);
                ExecuteRecalculationSQL(reqId);

                using (var con = new SqlConnection(connString))
                {
                    con.Open();
                    EnsureHeaderCompletedForPayment(reqId, auditor, con);
                    using (var cmd = new SqlCommand("dbo.usp_MarkRequestReadyForPayment", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ApprovalRequestId", reqId);
                        cmd.Parameters.AddWithValue("@ActionBy", auditor);
                        cmd.ExecuteNonQuery();
                    }

                    //con.Open();
                    //using (var tran = con.BeginTransaction())
                    //{
                    //    try
                    //    {
                    //        //    string fixStatusSql = @"
                    //        //UPDATE dbo.SalesApprovalRequest 
                    //        //SET Status = '" + ClaimStatuses.Completed + @"' 
                    //        //WHERE ApprovalRequestId = @Id AND Status = '" + ClaimStatuses.Pending + @"';

                    //        //IF @@ROWCOUNT > 0
                    //        //BEGIN
                    //        //    INSERT INTO dbo.ApprovalRequestStatusHistory 
                    //        //    (ApprovalRequestId, OldStatus, NewStatus, ChangedBy, Remarks)
                    //        //    VALUES 
                    //        //    (@Id, 'Pending', 'Completed', @AuditorId, 'Auditor finalized all rows and synced calculations.');
                    //        //END";

                    //        //    using (var cmdFix = new SqlCommand(fixStatusSql, con, tran))
                    //        //    {
                    //        //        cmdFix.Parameters.AddWithValue("@Id", reqId);
                    //        //        cmdFix.Parameters.AddWithValue("@AuditorId", auditor);
                    //        //        cmdFix.ExecuteNonQuery();
                    //        //    }

                    //        //    string sqlWorkflow = @"
                    //        //UPDATE dbo.SalesApprovalRequestApprover 
                    //        //SET 
                    //        //    Status = '" + ClaimStatuses.Approved + @"', 
                    //        //    ActionBy = @AuditorId, 
                    //        //    ActionDate = GETDATE() 
                    //        //WHERE ApprovalRequestId = @Id 
                    //        //  AND ApproverLevel = 2 
                    //        //  AND Status = '" + ClaimStatuses.Pending + @"'";

                    //        //    using (var cmdWorkflow = new SqlCommand(sqlWorkflow, con, tran))
                    //        //    {
                    //        //        cmdWorkflow.Parameters.AddWithValue("@Id", reqId);
                    //        //        cmdWorkflow.Parameters.AddWithValue("@AuditorId", auditor);
                    //        //        cmdWorkflow.ExecuteNonQuery();
                    //        //    }

                    //        using (var cmd = new SqlCommand("dbo.usp_MarkRequestReadyForPayment", con, tran))
                    //        {
                    //            cmd.CommandType = CommandType.StoredProcedure;
                    //            cmd.Parameters.AddWithValue("@ApprovalRequestId", reqId);
                    //            cmd.Parameters.AddWithValue("@ActionBy", auditor);
                    //            cmd.ExecuteNonQuery();
                    //        }

                    //        // Commit the transaction
                    //        tran.Commit();
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        tran.Rollback();
                    //        throw new Exception("Finalization failed. Transaction rolled back.", ex);
                    //    }
                    //}
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
        public static object UpdateIndividualExpense(int expenseId, string action, string remark)
        {
            try
            {
                string user = GetLoggedInEmployeeCodeFromSession() ?? "System";
                string approverRole = GetApproverRoleFromSession();

                // Normalize Action
                action = (action ?? "").Trim().ToUpperInvariant();

                // Validate Action
                if (action != "APPROVE" && action != "RETURN" && action != "REJECT")
                {
                    return new { success = false, message = "Invalid action." };
                }

                // Remarks mandatory for Return & Reject
                if ((action == "RETURN" || action == "REJECT") && string.IsNullOrWhiteSpace(remark))
                {
                    return new { success = false, message = "Remarks are mandatory." };
                }

                string formattedRemark = null;
                if (!string.IsNullOrWhiteSpace(remark))
                {
                    formattedRemark = AppendUserToRemark(remark.Trim(), user);
                }

                // 🟢 NEW: Map the action command to the correct Database Status Word
                string dbStatus = ClaimStatuses.Pending;
                if (action == "APPROVE") dbStatus = ClaimStatuses.Approved;
                else if (action == "REJECT") dbStatus = ClaimStatuses.Rejected;
                else if (action == "RETURN") dbStatus = ClaimStatuses.Returned;

                string conn = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

                using (SqlConnection con = new SqlConnection(conn))
                using (SqlCommand cmd = new SqlCommand("dbo.usp_UpdateSalesDailyExpenseStatus", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // 🟢 FIXED: Parameter names now exactly match your updated Stored Procedure
                    cmd.Parameters.AddWithValue("@ExpenseId", expenseId);
                    cmd.Parameters.AddWithValue("@NewStatus", dbStatus);
                    cmd.Parameters.AddWithValue("@ActionBy", user);
                    cmd.Parameters.AddWithValue("@Remarks", string.IsNullOrWhiteSpace(formattedRemark) ? (object)DBNull.Value : formattedRemark);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                return new { success = true };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object UpdateActivityKM(int activityId, decimal newKm, string reason)
        {
            try
            {
                string user = GetLoggedInEmployeeCodeFromSession() ?? "System";
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

        private static bool TryGetApproverLevelBounds(int approvalRequestId, out int current, out int max)
        {
            current = 0;
            max = 0;
            string conn = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection con = new SqlConnection(conn))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_GetApprovalRequest", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ApprovalRequestId", approvalRequestId);
                con.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return false;
                    current = dr["CurrentApproverLevel"] == DBNull.Value ? 0 : Convert.ToInt32(dr["CurrentApproverLevel"]);
                    max = dr["MaxApproverLevel"] == DBNull.Value ? 0 : Convert.ToInt32(dr["MaxApproverLevel"]);
                    return true;
                }
            }
        }

        private static bool NextApproverExists(int approvalRequestId)
        {
            int current, max;
            return TryGetApproverLevelBounds(approvalRequestId, out current, out max) && current < max;
        }

        private static int GetLatestUnresolvedDetailCount(int reqId)
        {
            string sql = @"
                SELECT COUNT(1)
                FROM dbo.SalesApprovalRequestDetail d
                WHERE d.ApprovalRequestId = @Id
                  AND ISNULL(d.DetailStatus, '" + ClaimStatuses.Pending + @"') NOT IN ('" + ClaimStatuses.Approved + @"', '" + ClaimStatuses.Rejected + @"')
                  AND " + SalesApprovalDetailVersionHelper.LatestDetailWhereForD.Trim();

            using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@Id", reqId);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private static bool ShouldConfirmForwardToNextApprover(int reqId, int detailId)
        {
            return ShouldConfirmForwardToNextApprover(reqId, new[] { detailId });
        }

        private static bool ShouldConfirmForwardToNextApprover(int reqId, IList<int> detailIds)
        {
            if (detailIds == null || detailIds.Count == 0 || !NextApproverExists(reqId))
                return false;

            int pendingBefore = GetLatestUnresolvedDetailCount(reqId);
            int wouldResolve = 0;
            foreach (int detailId in detailIds.Distinct())
            {
                string status = GetLatestDetailStatus(detailId, reqId);
                if (!ClaimStatuses.Equals(status, ClaimStatuses.Approved) &&
                    !ClaimStatuses.Equals(status, ClaimStatuses.Rejected))
                    wouldResolve++;
            }
            return wouldResolve > 0 && (pendingBefore - wouldResolve) == 0;
        }

        protected void btnConfirmForward_Click(object sender, EventArgs e)
        {
            if (hfPendingBulkApprove.Value == "1")
            {
                hfPendingBulkApprove.Value = "0";
                hfPendingApproveDetailId.Value = "";
                hfPendingApproveRemark.Value = "";
                ProcessBulkAction(DayDetailAction.Approve, true);
                return;
            }

            int detailId;
            int reqId;
            if (!int.TryParse(hfPendingApproveDetailId.Value, out detailId) ||
                !int.TryParse(hfActionRequestId.Value, out reqId) ||
                detailId <= 0 || reqId <= 0)
                return;

            string remark = hfPendingApproveRemark.Value ?? "";
            hfPendingApproveDetailId.Value = "";
            hfPendingApproveRemark.Value = "";

            try
            {
                ProcessDayDetailAction(detailId, reqId, DayDetailAction.Approve, remark);
                ExecuteRecalculationSQL(reqId);
                CloseDetailsAfterSuccessfulForward();
            }
            catch (Exception ex)
            {
                lblModalMessage.Text = ex.Message;
                lblModalMessage.CssClass = "text-danger font-weight-bold";
                upDetails.Update();
            }
        }

        private void CloseDetailsAfterSuccessfulForward()
        {
            BindRequestsGrid();
            ScriptManager.RegisterStartupScript(this, GetType(), "closeModal",
                "showForwardedSuccessNotify();closeDetailsAfterForward();", true);
        }

        private void SetFinalApproverFlag(int approvalRequestId)
        {
            int current, max;
            if (!TryGetApproverLevelBounds(approvalRequestId, out current, out max))
                return;
            hfIsFinalApprover.Value = (current == max) ? "1" : "0";
        }

        private void SetFinalApproverFlag_OLD(int approvalRequestId)
        {
            string conn =
                ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection con = new SqlConnection(conn))
            {
                con.Open();

                string sql = @"
                SELECT
                    CurrentLevel =
                        R.CurrentApproverLevel,
                    MaxLevel =
                    (
                        SELECT MAX(ApproverLevel)
                        FROM SalesApprovalRequestApprover
                        WHERE ApprovalRequestId = R.ApprovalRequestId
                          AND IsRequired = 1
                    )
                FROM SalesApprovalRequest R
                WHERE ApprovalRequestId=@Id";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Id", approvalRequestId);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            int current =
                                dr["CurrentLevel"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(dr["CurrentLevel"]);

                            int max =
                                dr["MaxLevel"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(dr["MaxLevel"]);

                            hfIsFinalApprover.Value =
                                current == max
                                ? "1"
                                : "0";
                        }
                    }
                }
            }
        }

        // NEW ENGINE: DYNAMIC HIERARCHY IDENTIFIER
        private void SetCurrentUserApproverDetails(int reqId)
        {
            string user = GetLoggedInEmployeeCode();
            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand(@"
                SELECT TOP 1 a.ApproverLevel, a.ApproverRole
                FROM dbo.SalesApprovalRequestApprover a
                INNER JOIN dbo.SalesApprovalRequest r ON r.ApprovalRequestId = a.ApprovalRequestId
                WHERE a.ApprovalRequestId = @Id
                  AND a.ApproverLoginId = @User
                  AND (
                        a.Status = @Pending
                     OR (a.ApproverRole = @Auditor AND r.Status = @Completed)
                  )
                ORDER BY CASE WHEN a.Status = @Pending THEN 0 ELSE 1 END", con))
            {
                cmd.Parameters.AddWithValue("@Id", reqId);
                cmd.Parameters.AddWithValue("@User", user);
                cmd.Parameters.AddWithValue("@Pending", ClaimStatuses.Pending);
                cmd.Parameters.AddWithValue("@Auditor", "Auditor");
                cmd.Parameters.AddWithValue("@Completed", ClaimStatuses.Completed);
                con.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        string role = rdr["ApproverRole"].ToString();
                        ViewState["CurrentUserLevel"] = Convert.ToInt32(rdr["ApproverLevel"]);
                        ViewState["CurrentUserRole"] = role;
                        Session[SessionApproverRoleKey] = role;
                        hfIsAuditor.Value = IsAuditorRole(role) ? "1" : "0";
                    }
                    else
                    {
                        ViewState["CurrentUserLevel"] = 0;
                        ViewState["CurrentUserRole"] = "";
                        Session[SessionApproverRoleKey] = "";
                        hfIsAuditor.Value = "0";
                    }
                }
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

                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();

                    // -------------------------------------------------------
                    // 1. Get Journey Details (From DA Claim)
                    // -------------------------------------------------------
                    string sqlPlaces = @"
                SELECT TOP (1)
                    FromPlace,
                    ToPlace
                FROM dbo.SalesDailyExpense
                WHERE ActivityId = @ActivityId
                  AND ExpenseType = 'DA'
                  AND IsDeleted = 0
                ORDER BY ExpenseId DESC;";

                    using (SqlCommand cmd = new SqlCommand(sqlPlaces, con))
                    {
                        cmd.Parameters.AddWithValue("@ActivityId", activityId);

                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                fromPlace = rdr["FromPlace"] != DBNull.Value
                                    ? rdr["FromPlace"].ToString()
                                    : "";

                                toPlace = rdr["ToPlace"] != DBNull.Value
                                    ? rdr["ToPlace"].ToString()
                                    : "";
                            }
                        }
                    }

                    // -------------------------------------------------------
                    // 2. Get KM Modification History
                    // -------------------------------------------------------
                    string sqlHistory = @"
                SELECT
                    HistoryId,
                    ActivityId,
                    OldKM,
                    NewKM,
                    ModifiedBy,
                    ModifiedDateTime,
                    Reason
                FROM dbo.SalesDailyActivityKMHistory
                WHERE ActivityId = @ActivityId
                ORDER BY ModifiedDateTime DESC;";

                    using (SqlCommand cmd = new SqlCommand(sqlHistory, con))
                    {
                        cmd.Parameters.AddWithValue("@ActivityId", activityId);

                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                DateTime utc = Convert.ToDateTime(rdr["ModifiedDateTime"]);

                                DateTime ist;
                                try
                                {
                                    ist = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
                                        utc,
                                        "UTC",
                                        "India Standard Time");
                                }
                                catch
                                {
                                    ist = utc.AddHours(5).AddMinutes(30);
                                }

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

                return new
                {
                    success = true,
                    fromPlace = fromPlace,
                    toPlace = toPlace,
                    history = historyList
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = ex.Message
                };
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

        private void ExecuteRecalculationSQL(int reqId)
        {
            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand("dbo.usp_RecalculateApprovalRequest", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ApprovalRequestId", reqId);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private void ExecuteRecalculationSQL_OLD(int reqId)
        {
            string sqlRecalculate = @"
            UPDATE d
            SET 
                Snapshot_DAAmount = ISNULL((SELECT SUM(Amount) FROM SalesDailyExpense e WHERE e.ActivityId = d.ActivityId AND e.ExpenseType = 'DA' AND e.IsDeleted = 0 AND ISNULL(e.ClaimStatus,'') <> '" + ClaimStatuses.Rejected + @"'), 0),
                Snapshot_HotelAmount = ISNULL((SELECT SUM(Amount) FROM SalesDailyExpense e WHERE e.ActivityId = d.ActivityId AND e.ExpenseType = 'Hotel' AND e.IsDeleted = 0 AND ISNULL(e.ClaimStatus,'') <> '" + ClaimStatuses.Rejected + @"'), 0),
                Snapshot_TravelAmount = ISNULL((SELECT SUM(Amount) FROM SalesDailyExpense e WHERE e.ActivityId = d.ActivityId AND e.ExpenseType = 'Travel' AND e.IsDeleted = 0 AND ISNULL(e.ClaimStatus,'') <> '" + ClaimStatuses.Rejected + @"'), 0),
                Snapshot_StationaryAmount = ISNULL((SELECT SUM(Amount) FROM SalesDailyExpense e WHERE e.ActivityId = d.ActivityId AND e.ExpenseType = 'Stationary' AND e.IsDeleted = 0 AND ISNULL(e.ClaimStatus,'') <> '" + ClaimStatuses.Rejected + @"'), 0)
            FROM SalesApprovalRequestDetail d WHERE d.ApprovalRequestId = @Id;

            UPDATE SalesApprovalRequestDetail 
            SET Snapshot_ExpenseTotal = ISNULL(Snapshot_DAAmount, 0) + ISNULL(Snapshot_HotelAmount, 0) + ISNULL(Snapshot_TravelAmount, 0) 
            WHERE ApprovalRequestId = @Id;

            UPDATE SalesApprovalRequest
            SET 
                ApprovedKM = (SELECT ISNULL(SUM(Snapshot_TotalDistanceKM), 0) FROM SalesApprovalRequestDetail WHERE ApprovalRequestId = @Id AND ISNULL(DetailStatus,'') <> '" + ClaimStatuses.Rejected + @"'),
                ApprovedClaimAmount = (SELECT ISNULL(SUM(Snapshot_CalculatedClaimAmount), 0) FROM SalesApprovalRequestDetail WHERE ApprovalRequestId = @Id AND ISNULL(DetailStatus,'') <> '" + ClaimStatuses.Rejected + @"'),
                ApprovedDAAmount = (SELECT ISNULL(SUM(Snapshot_DAAmount), 0) FROM SalesApprovalRequestDetail WHERE ApprovalRequestId = @Id AND ISNULL(DetailStatus,'') <> '" + ClaimStatuses.Rejected + @"'),
                ApprovedHotelAmount = (SELECT ISNULL(SUM(Snapshot_HotelAmount), 0) FROM SalesApprovalRequestDetail WHERE ApprovalRequestId = @Id AND ISNULL(DetailStatus,'') <> '" + ClaimStatuses.Rejected + @"'),
                ApprovedTravelAmount = (SELECT ISNULL(SUM(Snapshot_TravelAmount), 0) FROM SalesApprovalRequestDetail WHERE ApprovalRequestId = @Id AND ISNULL(DetailStatus,'') <> '" + ClaimStatuses.Rejected + @"'),
                Snapshot_ExpenseTotal = ISNULL(Snapshot_DAAmount, 0) + ISNULL(Snapshot_HotelAmount, 0) + ISNULL(Snapshot_TravelAmount, 0) + ISNULL(Snapshot_StationaryAmount, 0) 
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
        public static object ActionDayRowSilent(int detailId, int reqId, string action, bool forwardConfirmed)
        {
            try
            {
                string user = GetLoggedInEmployeeCodeFromSession();
                if (string.IsNullOrWhiteSpace(user))
                    return new { success = false, message = "User session expired. Please log in again." };

                string normalized = (action ?? "").Trim().ToUpperInvariant();
                bool isApprove = normalized == "APPROVE";
                bool isReturn = normalized == "RETURN";
                bool isReject = normalized == "REJECT";
                string approverRole = GetApproverRoleFromSession();

                if (!isApprove && !isReturn && !isReject)
                    return new { success = false, message = "Invalid day action." };

                if (isReturn && IsAuditorRole(approverRole))
                    return new { success = false, message = "Auditors cannot return days. Managers return days for employee correction." };

                if (isReturn)
                {
                    string detailStatus = GetLatestDetailStatus(detailId, reqId);
                    if (ClaimStatuses.Equals(detailStatus, ClaimStatuses.Returned))
                        return new { success = false, message = "Already returned to employee." };
                }

                if (isApprove)
                {
                    string approveStatus = GetLatestDetailStatus(detailId, reqId);
                    if (ClaimStatuses.Equals(approveStatus, ClaimStatuses.Returned))
                        return new { success = false, message = "Returned days cannot be approved until the employee resubmits." };
                }

                if (isReject && !IsAuditorRole(approverRole))
                    return new { success = false, message = "Only auditors can reject days." };

                if (isApprove && !forwardConfirmed && ShouldConfirmForwardToNextApprover(reqId, detailId))
                    return new { success = false, needsForwardConfirm = true };

                string remark = AppendUserToRemark(
                    isApprove
                        ? "Approved via Unified Day Modal"
                        : (isReject ? "Rejected via Unified Day Modal" : "Returned via Unified Day Modal"),
                    user);

                string conn = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

                using (SqlConnection con = new SqlConnection(conn))
                {
                    con.Open();

                    if (isApprove)
                    {
                        ExecuteProcessApprovalAction(detailId, "APPROVE", remark, user, con);
                    }
                    else if (isReturn)
                    {
                        // Activity/expense sync is handled inside usp_ProcessApprovalAction.
                        ExecuteProcessApprovalAction(detailId, "RETURN", remark, user, con);
                    }
                    else
                    {
                        ExecuteProcessApprovalAction(detailId, "REJECT", remark, user, con);
                    }
                }

                return isReturn
                    ? (object)new { success = true, detailId, detailStatus = ClaimStatuses.Returned }
                    : new { success = true };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object ActionDayRowSilent_OLD(int detailId, int reqId, string action)
        {
            try
            {
                string user =
                    HttpContext.Current.Session["workman"]?.ToString()
                    ?? "System";

                string conn =
                    ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

                using (SqlConnection con = new SqlConnection(conn))
                {
                    con.Open();

                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        try
                        {
                            using (SqlCommand cmd =
                                new SqlCommand(
                                    "dbo.usp_ProcessSalesApprovalRequestDetail",
                                    con,
                                    tran))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;

                                cmd.Parameters.AddWithValue(
                                    "@ApprovalRequestDetailId",
                                    detailId);

                                cmd.Parameters.AddWithValue(
                                    "@ApprovalRequestId",
                                    reqId);

                                cmd.Parameters.AddWithValue(
                                    "@Action",
                                    action.ToUpper());

                                cmd.Parameters.AddWithValue(
                                    "@ActionBy",
                                    user);

                                cmd.Parameters.AddWithValue(
                                    "@ActionRemarks",
                                    "Processed via Unified Day Modal");

                                cmd.ExecuteNonQuery();
                            }

                            tran.Commit();

                            return new
                            {
                                success = true
                            };
                        }
                        catch
                        {
                            tran.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = ex.Message
                };
            }
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