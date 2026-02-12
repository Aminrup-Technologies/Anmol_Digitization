using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.UI.HtmlControls;
using System.Web.Services;
using System.Web.Script.Services;

namespace AnmolDristi
{

    public partial class ClaimsApproval : BasePage
    {
        private string connString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        // class-level accumulators (page-level)
        private decimal _totalDistance = 0m;
        private decimal _totalTAClaim = 0m;
        private decimal _totalDA = 0m;
        private decimal _totalHotel = 0m;
        private decimal _totalTravel = 0m;
        private decimal _totalSnapshot = 0m;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // require USERID and USERNAME as minimum for a logged-in user
                if (Session["USERID"] != null && Session["USERNAME"] != null)
                {
                    BindRequestsGrid();
                }
            }
        }

        // ------------- BIND HEADER GRID ----------------
        protected void BindRequestsGrid_OLD()
        {
            string sql = @"SELECT A.ApprovalRequestId, A.RequesterLoginId, UM.EmployeeName, A.RequestDate, A.YearNo, A.MonthNo, DATENAME(MONTH, DATEFROMPARTS(A.YearNo, A.MonthNo, 1)) AS MonthName, A.Status, A.TotalKM, A.TotalClaimAmount, A.TotalDAAmount, A.TotalHotelAmount, A.TotalTravelAmount, A.TotalExpenseAmount, A.RequestRemarks, A.CreatedOn, A.CreatedBy, A.ModifiedOn, A.ModifiedBy FROM dbo.SalesApprovalRequest A INNER JOIN MST_UserMaster UM ON A.RequesterLoginId = UM.EmployeeCode WHERE A.Status = 'Pending' ORDER BY A.RequestDate DESC;";

            DataTable dt = new DataTable();
            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand(sql, con))
            using (var da = new SqlDataAdapter(cmd))
            {
                da.Fill(dt);
            }

            gvRequests.DataSource = dt;
            gvRequests.DataBind();
        }

        protected void BindRequestsGrid()
        {
            // current approver's employee code (adjust if you store elsewhere)
            var approverCode = GetLoggedInEmployeeCode();
            if (string.IsNullOrWhiteSpace(approverCode))
            {
                gvRequests.DataSource = null;
                gvRequests.DataBind();
                return;
            }

            DataTable dt = new DataTable();
            try
            {
                using (var con = new SqlConnection(connString))
                using (var cmd = new SqlCommand("dbo.usp_GetPendingApprovalsForUser_Fast", con))
                using (var da = new SqlDataAdapter(cmd))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ApproverLoginId", approverCode);
                    da.Fill(dt);
                }

                gvRequests.DataSource = dt;
                gvRequests.DataBind();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("BindRequestsGrid failed: " + ex);
                gvRequests.DataSource = null;
                gvRequests.DataBind();
                // Optionally set an admin-visible error label here
            }
        }



        protected void Page_Init(object sender, EventArgs e)
        {
            // reset totals at start of page lifecycle
            _totalDistance = _totalTAClaim = _totalDA = _totalHotel = _totalTravel = _totalSnapshot = 0m;
        }

        private decimal SafeDec(object value)
        {
            return (value == null || value == DBNull.Value) ? 0m : Convert.ToDecimal(value);
        }
        protected void gvDetails_RowDataBound_07Nov25(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DataRowView drv = e.Row.DataItem as DataRowView;
                if (drv == null) return;

                _totalDistance += SafeDec(drv["Snapshot_TotalDistanceKM"]);
                _totalTAClaim += SafeDec(drv["Snapshot_CalculatedClaimAmount"]);
                _totalDA += SafeDec(drv["Snapshot_DAAmount"]);
                _totalHotel += SafeDec(drv["Snapshot_HotelAmount"]);
                _totalTravel += SafeDec(drv["Snapshot_TravelAmount"]);
                _totalSnapshot += SafeDec(drv["Snapshot_ExpenseTotal"]);

                // status styling: show background for rejected items etc.
                string claimStatus = drv["ClaimStatus"] == DBNull.Value ? "" : drv["ClaimStatus"].ToString();
                var statusCell = e.Row.Cells[ /* index of ClaimStatus template field - see note below */ 9];
                // Instead of hard-coded index, find the cell by control / replace with a safer mechanism if needed.

                // Example: highlight rows that are already processed
                string detailStatus = drv["DetailStatus"] == DBNull.Value ? "" : drv["DetailStatus"].ToString();
                if (detailStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                {
                    e.Row.CssClass += " table-danger";
                }
                else if (detailStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                {
                    e.Row.CssClass += " table-success";
                }

                // Make per-row controls accessible if you need to set properties
                var btnApproveRow = e.Row.FindControl("btnApproveRow") as Button;
                var btnRejectRow = e.Row.FindControl("btnRejectRow") as Button;
                if (btnApproveRow != null)
                    btnApproveRow.Enabled = !detailStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase);

                if (btnRejectRow != null)
                    btnRejectRow.Enabled = !detailStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase);
            }
            else if (e.Row.RowType == DataControlRowType.Footer)
            {
                // put totals into footer cells: calculate which cell index corresponds to which bound field
                // find footer cell by header text for reliability
                FillFooterTotals(e.Row);
            }
        }

        protected void gvDetails_RowDataBound_OLD3(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            var drv = e.Row.DataItem as DataRowView;
            if (drv == null) return;

            string detailStatus = drv["DetailStatus"] == DBNull.Value ? "" : drv["DetailStatus"].ToString();

            // Add attribute on the TR so JS can inspect it
            e.Row.Attributes["data-detail-status"] = detailStatus;

            // Optionally disable/grey-out checkbox for non-pending statuses
            var chk = e.Row.FindControl("chkSelect") as CheckBox;
            if (chk != null)
            {
                if (!string.Equals(detailStatus, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    chk.Enabled = false;
                    chk.InputAttributes["disabled"] = "disabled"; // ensure HTML shows disabled
                                                                  // add a CSS class for styling
                    e.Row.CssClass += " table-secondary";
                }
            }

            // also optionally set per-row remark textbox readonly when approved/rejected
            var txtRowRemark = e.Row.FindControl("txtRowRemark") as TextBox;
            if (txtRowRemark != null)
            {
                if (string.Equals(detailStatus, "Approved", StringComparison.OrdinalIgnoreCase))
                {
                    txtRowRemark.ReadOnly = true;
                    txtRowRemark.CssClass += " bg-light";
                }
            }
        }

        protected void gvDetails_RowDataBound_09Jan26(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DataRowView drv = e.Row.DataItem as DataRowView;
                if (drv == null) return;

                // accumulate totals (your original logic)
                _totalDistance += SafeDec(drv["Snapshot_TotalDistanceKM"]);
                _totalTAClaim += SafeDec(drv["Snapshot_CalculatedClaimAmount"]);
                _totalDA += SafeDec(drv["Snapshot_DAAmount"]);
                _totalHotel += SafeDec(drv["Snapshot_HotelAmount"]);
                _totalTravel += SafeDec(drv["Snapshot_TravelAmount"]);
                _totalSnapshot += SafeDec(drv["Snapshot_ExpenseTotal"]);


                // centralized per-row setup
                PopulateRowControls(e.Row, drv);
            }
            else if (e.Row.RowType == DataControlRowType.Footer)
            {
                FillFooterTotals(e.Row);
            }
        }

        protected void gvDetails_RowDataBound_09Jna26_2(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DataRowView drv = e.Row.DataItem as DataRowView;
                if (drv == null) return;

                /* ============================
                   1️⃣ ACCUMULATE TOTALS
                ============================ */
                _totalDistance += SafeDec(drv["Snapshot_TotalDistanceKM"]);
                _totalTAClaim += SafeDec(drv["Snapshot_CalculatedClaimAmount"]);
                _totalDA += SafeDec(drv["Snapshot_DAAmount"]);
                _totalHotel += SafeDec(drv["Snapshot_HotelAmount"]);
                _totalTravel += SafeDec(drv["Snapshot_TravelAmount"]);
                _totalSnapshot += SafeDec(drv["Snapshot_ExpenseTotal"]);

                /* ============================
                   2️⃣ APPLY NON-APPROVAL UI
                      (KM edit, styling, expenses, tooltip)
                ============================ */
                PopulateRowControls(e.Row, drv);

                /* ============================
                   3️⃣ APPLY APPROVAL AUTHORITY
                ============================ */

                if (ViewState["CurrentUserLevel"] == null)
                    throw new InvalidOperationException("CurrentUserLevel not set before binding gvDetails.");

                int currentUserLevel = Convert.ToInt32(ViewState["CurrentUserLevel"]);

                string detailStatus = Convert.ToString(drv["DetailStatus"])?.Trim();
                int approvedLevel = drv["DetailApprovedLevel"] == DBNull.Value
                    ? 0
                    : Convert.ToInt32(drv["DetailApprovedLevel"]);

                var btnApprove = (Button)e.Row.FindControl("btnApproveRow");
                var btnReject = (Button)e.Row.FindControl("btnRejectRow");
                var txtRemark = (TextBox)e.Row.FindControl("txtRowRemark");
                var chkSelect = (CheckBox)e.Row.FindControl("chkSelect");

                // Defensive defaults
                if (btnApprove != null) btnApprove.Enabled = false;
                if (btnReject != null) btnReject.Enabled = false;
                if (txtRemark != null) txtRemark.ReadOnly = true;
                if (chkSelect != null) chkSelect.Enabled = false;

                // 🔒 Rejected → locked forever
                if (detailStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                    return;

                // 🔒 Approved by THIS user → locked
                if (detailStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase)
                    && approvedLevel == currentUserLevel)
                    return;

                // ✅ Approved by previous level → allow current user
                if (detailStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase)
                    && approvedLevel < currentUserLevel)
                {
                    if (btnApprove != null) btnApprove.Enabled = true;
                    if (btnReject != null) btnReject.Enabled = true;
                    if (txtRemark != null)
                    {
                        txtRemark.ReadOnly = false;
                        txtRemark.Text = ""; // fresh remarks for auditor
                    }
                    if (chkSelect != null) chkSelect.Enabled = true;
                    return;
                }

                // ✅ Pending → allow current user
                if (detailStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                {
                    if (btnApprove != null) btnApprove.Enabled = true;
                    if (btnReject != null) btnReject.Enabled = true;
                    if (txtRemark != null) txtRemark.ReadOnly = false;
                    if (chkSelect != null) chkSelect.Enabled = true;
                }
            }
            else if (e.Row.RowType == DataControlRowType.Footer)
            {
                /* ============================
                   4️⃣ FOOTER TOTALS
                ============================ */
                FillFooterTotals(e.Row);
            }
        }

        protected void gvDetails_RowDataBound_11Feb26(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow)
                return;

            DataRowView drv = e.Row.DataItem as DataRowView;
            if (drv == null) return;

            // 1️⃣ Restore ALL non-approval wiring
            PopulateRowControls(e.Row, drv);

            // 2️⃣ Approval authority (your working logic)
            if (ViewState["CurrentUserLevel"] == null)
                throw new InvalidOperationException("CurrentUserLevel not set before binding gvDetails.");

            int currentUserLevel = Convert.ToInt32(ViewState["CurrentUserLevel"]);

            string detailStatus = Convert.ToString(drv["DetailStatus"])?.Trim();
            int approvedLevel = drv["DetailApprovedLevel"] == DBNull.Value
                ? 0
                : Convert.ToInt32(drv["DetailApprovedLevel"]);

            var btnApprove = (Button)e.Row.FindControl("btnApproveRow");
            var btnReject = (Button)e.Row.FindControl("btnRejectRow");
            var txtRemark = (TextBox)e.Row.FindControl("txtRowRemark");
            var chkSelect = (CheckBox)e.Row.FindControl("chkSelect");

            // Default lock
            btnApprove.Enabled = false;
            btnReject.Enabled = false;
            txtRemark.ReadOnly = true;
            chkSelect.Enabled = false;

            if (lblHeaderStatus.Text.Equals("Reopened", StringComparison.OrdinalIgnoreCase))
            {
                // allow only Auditor to act
                if (Convert.ToInt32(ViewState["CurrentUserLevel"]) != 2)
                {
                    e.Row.FindControl("btnApproveRow").Visible = false;
                    e.Row.FindControl("btnRejectRow").Visible = false;
                    return;
                }
            }


            if (detailStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                return;

            if (detailStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase)
                && approvedLevel == currentUserLevel)
                return;

            if (detailStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase)
                && approvedLevel < currentUserLevel)
            {
                btnApprove.Enabled = true;
                btnReject.Enabled = true;
                txtRemark.ReadOnly = false;
                txtRemark.Text = "";
                chkSelect.Enabled = true;
                return;
            }

            if (detailStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                btnApprove.Enabled = true;
                btnReject.Enabled = true;
                txtRemark.ReadOnly = false;
                chkSelect.Enabled = true;
            }
        }

        protected void gvDetails_RowDataBound_11Feb26_2(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            DataRowView drv = e.Row.DataItem as DataRowView;
            if (drv == null) return;

            // 1️⃣ Restore wiring
            PopulateRowControls(e.Row, drv);

            // 2️⃣ Setup Authority
            if (ViewState["CurrentUserLevel"] == null) return; // Fail safe

            int currentUserLevel = Convert.ToInt32(ViewState["CurrentUserLevel"]);

            // Handle NULLs safely
            string detailStatus = Convert.ToString(drv["DetailStatus"]);
            if (string.IsNullOrWhiteSpace(detailStatus)) detailStatus = "Pending"; // ✅ FIX: Treat NULL as Pending

            int approvedLevel = drv["DetailApprovedLevel"] == DBNull.Value ? 0 : Convert.ToInt32(drv["DetailApprovedLevel"]);

            // Find Controls
            var btnApprove = (Button)e.Row.FindControl("btnApproveRow");
            var btnReject = (Button)e.Row.FindControl("btnRejectRow");
            var txtRemark = (TextBox)e.Row.FindControl("txtRowRemark");
            var chkSelect = (CheckBox)e.Row.FindControl("chkSelect");

            // 🔒 DEFAULT: LOCK EVERYTHING
            btnApprove.Enabled = false;
            btnApprove.CssClass = "btn btn-sm btn-outline-secondary disabled"; // Visual gray out
            btnReject.Enabled = false;
            txtRemark.ReadOnly = true;
            chkSelect.Enabled = false;

            // 3️⃣ LOGIC: REJECTED (Dead End)
            if (detailStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
            {
                e.Row.CssClass += " table-danger"; // Make row red
                return;
            }

            // 4️⃣ LOGIC: ALREADY DONE BY ME
            if (detailStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase) && approvedLevel == currentUserLevel)
            {
                e.Row.CssClass += " table-success"; // Make row green
                btnApprove.Text = "✔ Done"; // Visual feedback
                return;
            }

            // 5️⃣ LOGIC: HANDOVER (Approved by Lower Level -> Needs Me)
            if (detailStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase) && approvedLevel < currentUserLevel)
            {
                // ✅ Enable controls
                UnlockControls(btnApprove, btnReject, txtRemark, chkSelect);

                // ✅ Visual Cue: Yellow to show "Incoming Work"
                e.Row.CssClass += " table-warning";
                e.Row.ToolTip = $"Approved by Level {approvedLevel}, waiting for Level {currentUserLevel}.";
                return;
            }

            // 6️⃣ LOGIC: FRESH ITEM (Pending or Null)
            if (detailStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                // ✅ Enable controls
                UnlockControls(btnApprove, btnReject, txtRemark, chkSelect);
            }
        }

        protected void gvDetails_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            // ==========================================
            // 1️⃣ DATA ROW LOGIC
            // ==========================================
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DataRowView drv = e.Row.DataItem as DataRowView;
                if (drv == null) return;

                // 🟢 A. ACCUMULATE TOTALS (Missing in your snippet)
                _totalDistance += SafeDec(drv["Snapshot_TotalDistanceKM"]);
                _totalTAClaim += SafeDec(drv["Snapshot_CalculatedClaimAmount"]);
                _totalDA += SafeDec(drv["Snapshot_DAAmount"]);
                _totalHotel += SafeDec(drv["Snapshot_HotelAmount"]);
                _totalTravel += SafeDec(drv["Snapshot_TravelAmount"]);
                _totalSnapshot += SafeDec(drv["Snapshot_ExpenseTotal"]);

                // 🔵 B. RESTORE WIRING (Tooltips, KM Edit, etc.)
                PopulateRowControls(e.Row, drv);

                // 🟠 C. AUTHORITY & BUTTON LOGIC
                if (ViewState["CurrentUserLevel"] == null) return;

                int currentUserLevel = Convert.ToInt32(ViewState["CurrentUserLevel"]);
                string detailStatus = Convert.ToString(drv["DetailStatus"]);
                if (string.IsNullOrWhiteSpace(detailStatus)) detailStatus = "Pending";

                int approvedLevel = drv["DetailApprovedLevel"] == DBNull.Value ? 0 : Convert.ToInt32(drv["DetailApprovedLevel"]);

                // Find Controls
                var btnApprove = (Button)e.Row.FindControl("btnApproveRow");
                var btnReject = (Button)e.Row.FindControl("btnRejectRow");
                var txtRemark = (TextBox)e.Row.FindControl("txtRowRemark");
                var chkSelect = (CheckBox)e.Row.FindControl("chkSelect");

                // 🔒 DEFAULT: LOCK EVERYTHING
                if (btnApprove != null)
                {
                    btnApprove.Enabled = false;
                    btnApprove.CssClass = "btn btn-sm btn-outline-secondary disabled";
                }
                if (btnReject != null) btnReject.Enabled = false;
                if (txtRemark != null) txtRemark.ReadOnly = true;
                if (chkSelect != null) chkSelect.Enabled = false;

                // LOGIC: REJECTED (Dead End)
                if (detailStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                {
                    e.Row.CssClass += " table-danger";
                    return;
                }

                // LOGIC: ALREADY DONE BY ME
                if (detailStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase) && approvedLevel == currentUserLevel)
                {
                    e.Row.CssClass += " table-success";
                    if (btnApprove != null) btnApprove.Text = "✔ Done";
                    return;
                }

                // LOGIC: HANDOVER (Approved by Lower Level -> Needs Me)
                if (detailStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase) && approvedLevel < currentUserLevel)
                {
                    UnlockControls(btnApprove, btnReject, txtRemark, chkSelect);
                    e.Row.CssClass += " table-warning";
                    e.Row.ToolTip = $"Approved by Level {approvedLevel}, waiting for Level {currentUserLevel}.";
                    return;
                }

                // LOGIC: FRESH ITEM (Pending or Null)
                if (detailStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                {
                    UnlockControls(btnApprove, btnReject, txtRemark, chkSelect);
                }
            }
            // ==========================================
            // 2️⃣ FOOTER LOGIC (Missing in your snippet)
            // ==========================================
            else if (e.Row.RowType == DataControlRowType.Footer)
            {
                FillFooterTotals(e.Row);
            }
        }

        // Helper to keep code clean
        private void UnlockControls(Button btnA, Button btnR, TextBox txt, CheckBox chk)
        {
            btnA.Enabled = true;
            btnA.CssClass = "btn btn-sm btn-outline-success"; // Restore color
            btnR.Enabled = true;
            txt.ReadOnly = false;
            txt.Text = ""; // Clear previous remarks if any, or keep them depending on preference
            chk.Enabled = true;
        }

        private void PopulateRowControls(GridViewRow row, DataRowView drv)
        {
            // Defensive
            if (drv == null || row == null) return;

            /* =====================================================
               1️⃣ CLAIM STATUS (FOR KM EDIT & TOOLTIP ONLY)
            ===================================================== */
            string claimStatus = drv["ClaimStatus"] == DBNull.Value
                ? string.Empty
                : drv["ClaimStatus"].ToString().Trim();

            /* =====================================================
               2️⃣ KM EDIT RULES (UNCHANGED)
            ===================================================== */
            //var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            //{
            //    "Draft", "Submitted"
            //};

            //var lnkEdit = row.FindControl("lnkEditKM") as LinkButton;
            //if (lnkEdit != null)
            //{
            //    if (allowed.Contains(claimStatus))
            //    {
            //        lnkEdit.Visible = true;
            //        lnkEdit.Enabled = true;
            //        if (!lnkEdit.CssClass.Contains("btn-edit-km"))
            //            lnkEdit.CssClass = (lnkEdit.CssClass + " btn-edit-km").Trim();
            //    }
            //    else
            //    {
            //        lnkEdit.Visible = false;
            //    }
            //}

            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Draft", "Submitted"
            };

            int currentUserLevel = ViewState["CurrentUserLevel"] != null
                ? Convert.ToInt32(ViewState["CurrentUserLevel"])
                : 0;

            bool isAuditor = currentUserLevel == 2;

            bool isProcessed =
                claimStatus.Equals("Processed", StringComparison.OrdinalIgnoreCase);

            bool canEditKM =
                !isProcessed &&
                (allowed.Contains(claimStatus) || isAuditor);

            var lnkEdit = row.FindControl("lnkEditKM") as LinkButton;
            if (lnkEdit != null)
            {
                lnkEdit.Visible = canEditKM;
                lnkEdit.Enabled = canEditKM;

                if (canEditKM && !lnkEdit.CssClass.Contains("btn-edit-km"))
                    lnkEdit.CssClass = (lnkEdit.CssClass + " btn-edit-km").Trim();
            }

            /* =====================================================
               3️⃣ ROW STYLING (SAFE)
            ===================================================== */
            string detailStatus = drv["DetailStatus"] == DBNull.Value
                ? string.Empty
                : drv["DetailStatus"].ToString().Trim();

            if (detailStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                AddCssClass(row, "table-danger");
            else if (detailStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                AddCssClass(row, "table-success");
            else
                RemoveCssClass(row, "table-danger table-success");

            /* =====================================================
               4️⃣ ❌ APPROVAL ENABLE/DISABLE REMOVED
               (Handled ONLY in gvDetails_RowDataBound)
            ===================================================== */

            /* =====================================================
               5️⃣ EXPENSES JSON WIRING (UNCHANGED)
            ===================================================== */
            string expensesJson = "[]";
            if (drv.DataView.Table.Columns.Contains("ExpensesJson"))
                expensesJson = drv["ExpensesJson"] == DBNull.Value
                    ? "[]"
                    : drv["ExpensesJson"].ToString();

            Control btnExpensesCtrl = FindControlRecursiveByCssClass(row, "btn-expenses");
            if (btnExpensesCtrl != null)
            {
                var wc = btnExpensesCtrl as WebControl;
                if (wc != null)
                {
                    wc.Attributes["data-expenses"] = HttpUtility.HtmlAttributeEncode(expensesJson);
                    if (drv.DataView.Table.Columns.Contains("ExpenseCount"))
                        wc.Attributes["data-expensecount"] = Convert.ToString(drv["ExpenseCount"]);
                }
                else
                {
                    var hc = btnExpensesCtrl as HtmlControl;
                    if (hc != null)
                    {
                        hc.Attributes["data-expenses"] = HttpUtility.HtmlAttributeEncode(expensesJson);
                        if (drv.DataView.Table.Columns.Contains("ExpenseCount"))
                            hc.Attributes["data-expensecount"] = Convert.ToString(drv["ExpenseCount"]);
                    }
                }
            }
            else
            {
                var hf = FindControlRecursive(row, "hfExpensesJson") as HiddenField;
                if (hf != null) hf.Value = expensesJson;
            }

            /* =====================================================
               6️⃣ STATUS TOOLTIP (SAFE, READ-ONLY)
            ===================================================== */
            Control statusCtrl = FindControlRecursiveByCssClass(row, "badge");
            if (statusCtrl != null)
            {
                string tooltip = "Unknown status";
                switch (claimStatus)
                {
                    case "Draft": tooltip = "Draft – not submitted yet"; break;
                    case "Submitted": tooltip = "Submitted – awaiting approval"; break;
                    case "Approved": tooltip = "Approved by previous level"; break;
                    case "Rejected": tooltip = "Rejected – please review"; break;
                    case "Processed": tooltip = "Processed in accounts"; break;
                }

                var swc = statusCtrl as WebControl;
                if (swc != null) swc.Attributes["title"] = tooltip;
                else
                {
                    var shc = statusCtrl as HtmlControl;
                    if (shc != null) shc.Attributes["title"] = tooltip;
                }
            }
        }


        private void PopulateRowControls_09Jan26(GridViewRow row, DataRowView drv)
        {
            // ====== INSERT THIS AT THE TOP OF PopulateRowControls(...) ======
            // Defensive: if drv or row null, bail (keeps old logic safe)
            if (drv == null || row == null) return;

            // Get claim status for the current row (safe null handling)
            string claimStatus = drv["ClaimStatus"] == DBNull.Value ? string.Empty : drv["ClaimStatus"].ToString().Trim();

            // Allowed statuses that can edit KM
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Draft", "Submitted" };

            // Find edit control by ID (matches TemplateField LinkButton ID: lnkEditKM)
            var lnkEdit = row.FindControl("lnkEditKM") as LinkButton;
            if (lnkEdit != null)
            {
                if (allowed.Contains(claimStatus))
                {
                    lnkEdit.Visible = true;
                    lnkEdit.Enabled = true;
                    // ensure client-side class for JS
                    if (!lnkEdit.CssClass.Contains("btn-edit-km")) lnkEdit.CssClass = (lnkEdit.CssClass + " btn-edit-km").Trim();
                }
                else
                {
                    // hide the edit action for non-editable statuses (keeps UI clean)
                    lnkEdit.Visible = false;

                    // If you prefer a disabled button instead of hiding, comment the previous line and uncomment below:
                    // lnkEdit.Visible = true;
                    // lnkEdit.Enabled = false;
                    // lnkEdit.CssClass = (lnkEdit.CssClass + " disabled").Trim();
                    // lnkEdit.ToolTip = "KM cannot be edited once Approved/Rejected/Processed";
                    // lnkEdit.Attributes["onclick"] = "return false;";
                }
            }
            // ====== END INSERT ======


            if (row == null || drv == null) return;

            string detailStatus = drv["DetailStatus"] == DBNull.Value ? String.Empty : drv["DetailStatus"].ToString();
            //string claimStatus = drv["ClaimStatus"] == DBNull.Value ? String.Empty : drv["ClaimStatus"].ToString();

            // Row styling
            if (detailStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                AddCssClass(row, "table-danger");
            else if (detailStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                AddCssClass(row, "table-success");
            else
                RemoveCssClass(row, "table-danger table-success");

            // Enable/disable buttons
            var btnApprove = FindControlRecursive(row, "btnApproveRow") as Button;
            var btnReject = FindControlRecursive(row, "btnRejectRow") as Button;
            if (btnApprove != null)
                btnApprove.Enabled = !detailStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase);
            if (btnReject != null)
                btnReject.Enabled = !detailStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase);

            // Expenses JSON
            string expensesJson = "[]";
            if (drv.DataView.Table.Columns.Contains("ExpensesJson"))
                expensesJson = drv["ExpensesJson"] == DBNull.Value ? "[]" : drv["ExpensesJson"].ToString();

            // Find control by CSS class
            Control btnExpensesCtrl = FindControlRecursiveByCssClass(row, "btn-expenses");
            if (btnExpensesCtrl != null)
            {
                // use 'as' casts for compatibility
                var wc = btnExpensesCtrl as WebControl;
                if (wc != null)
                {
                    wc.Attributes["data-expenses"] = HttpUtility.HtmlAttributeEncode(expensesJson);
                    if (drv.DataView.Table.Columns.Contains("ExpenseCount"))
                        wc.Attributes["data-expensecount"] = Convert.ToString(drv["ExpenseCount"]);
                }
                else
                {
                    var hc = btnExpensesCtrl as HtmlControl;
                    if (hc != null)
                    {
                        hc.Attributes["data-expenses"] = HttpUtility.HtmlAttributeEncode(expensesJson);
                        if (drv.DataView.Table.Columns.Contains("ExpenseCount"))
                            hc.Attributes["data-expensecount"] = Convert.ToString(drv["ExpenseCount"]);
                    }
                    else
                    {
                        // last-resort via reflection (keeps previous behaviour)
                        try
                        {
                            var pi = btnExpensesCtrl.GetType().GetProperty("Attributes");
                            if (pi != null)
                            {
                                var attrs = pi.GetValue(btnExpensesCtrl, null) as AttributeCollection;
                                if (attrs != null)
                                    attrs["data-expenses"] = HttpUtility.HtmlAttributeEncode(expensesJson);
                            }
                        }
                        catch
                        {
                            // ignore
                        }
                    }
                }
            }
            else
            {
                // fallback hidden field
                var hf = FindControlRecursive(row, "hfExpensesJson") as HiddenField;
                if (hf != null) hf.Value = expensesJson;
            }

            // Status tooltip
            Control statusCtrl = FindControlRecursiveByCssClass(row, "badge");
            if (statusCtrl != null)
            {
                string tooltip = "Unknown status";
                switch (claimStatus)
                {
                    case "Draft": tooltip = "Draft – not submitted yet"; break;
                    case "Submitted": tooltip = "Submitted – awaiting approval"; break;
                    case "Approved": tooltip = "Approved by manager"; break;
                    case "Rejected": tooltip = "Rejected – please review"; break;
                    case "Processed": tooltip = "Processed in accounts"; break;
                }

                var swc = statusCtrl as WebControl;
                if (swc != null) swc.Attributes["title"] = tooltip;
                else
                {
                    var shc = statusCtrl as HtmlControl;
                    if (shc != null) shc.Attributes["title"] = tooltip;
                }
            }
        }

        private int GetCurrentUserApproverLevel(int approvalRequestId)
        {
            using (SqlConnection con = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand(@"
        SELECT ApproverLevel
        FROM SalesApprovalRequestApprover
        WHERE ApprovalRequestId = @Id
          AND ApproverLoginId = @Login
          AND Status = 'Pending'", con))
            {
                cmd.Parameters.AddWithValue("@Id", approvalRequestId);
                cmd.Parameters.AddWithValue("@Login", GetLoggedInEmployeeCode());
                con.Open();
                object val = cmd.ExecuteScalar();
                return val == null ? -1 : Convert.ToInt32(val);
            }
        }


        /// <summary>Recursively find control by ID.</summary>
        private Control FindControlRecursive(Control root, string id)
        {
            if (root == null) return null;
            if (!string.IsNullOrEmpty(root.ID) && root.ID.Equals(id, StringComparison.OrdinalIgnoreCase))
                return root;

            foreach (Control c in root.Controls)
            {
                var found = FindControlRecursive(c, id);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>Recursively find first control that contains the given CSS class token.</summary>
        private Control FindControlRecursiveByCssClass(Control root, string cssClass)
        {
            if (root == null) return null;

            // check WebControl
            var wc = root as WebControl;
            if (wc != null)
            {
                if (HasCssToken(wc.CssClass, cssClass)) return root;
            }
            else
            {
                // check HtmlControl
                var hc = root as HtmlControl;
                if (hc != null)
                {
                    string cls = hc.Attributes["class"] ?? string.Empty;
                    if (HasCssToken(cls, cssClass)) return root;
                }
            }

            foreach (Control c in root.Controls)
            {
                var f = FindControlRecursiveByCssClass(c, cssClass);
                if (f != null) return f;
            }
            return null;
        }


        private bool HasCssToken(string classAttr, string token)
        {
            if (string.IsNullOrWhiteSpace(classAttr) || string.IsNullOrWhiteSpace(token)) return false;
            var tokens = classAttr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var t in tokens)
                if (t.Equals(token, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }


        /// <summary>Safely add a CSS class to a GridViewRow without removing existing classes.</summary>
        private void AddCssClass(GridViewRow row, string cssClass)
        {
            if (row == null) return;
            string current = row.CssClass ?? String.Empty;
            if (!HasCssToken(current, cssClass))
                row.CssClass = (current + " " + cssClass).Trim();
        }

        /// <summary>Remove given css tokens from row.CssClass.</summary>
        private void RemoveCssClass(GridViewRow row, string cssClasses)
        {
            if (row == null) return;
            if (string.IsNullOrWhiteSpace(row.CssClass)) return;

            var remove = cssClasses.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var keepList = new System.Collections.Generic.List<string>();
            foreach (var t in row.CssClass.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                bool rem = false;
                foreach (var r in remove) if (t.Equals(r, StringComparison.OrdinalIgnoreCase)) { rem = true; break; }
                if (!rem) keepList.Add(t);
            }
            row.CssClass = string.Join(" ", keepList);
        }

        private void BulkUpdateSelectedDetails(string newStatus)
        {
            var idsToProcess = new List<int>();
            foreach (GridViewRow row in gvDetails.Rows)
            {
                var cb = row.FindControl("chkSelect") as CheckBox;
                if (cb != null && cb.Checked)
                {
                    int id = Convert.ToInt32(gvDetails.DataKeys[row.RowIndex].Value);
                    idsToProcess.Add(id);
                }
            }

            if (idsToProcess.Count == 0)
            {
                lblModalMessage.CssClass = "text-warning";
                lblModalMessage.Text = "No rows selected.";
                return;
            }

            // Use a TVP or comma-separated list. Example: call a stored proc that accepts @ApprovalRequestDetailIds TVP
            // For simplicity: call SP per id (but you should batch with TVP for production)
            using (var con = new SqlConnection(connString))
            {
                con.Open();
                foreach (int detailId in idsToProcess)
                {
                    using (var cmd = new SqlCommand("dbo.usp_UpdateApprovalRequestDetailStatus", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ApprovalRequestDetailId", detailId);
                        cmd.Parameters.AddWithValue("@NewStatus", newStatus);
                        // Bulk remark fallback: check footer txtBulkRemarks or footer txtFooterRemarks
                        string bulkRemark = txtBulkRemarks.Text.Trim();
                        if (string.IsNullOrEmpty(bulkRemark))
                        {
                            // try footer textbox inside grid footer if present
                            var footerRemark = gvDetails.FooterRow?.FindControl("txtFooterRemarks") as TextBox;
                            if (footerRemark != null) bulkRemark = footerRemark.Text.Trim();
                        }
                        cmd.Parameters.AddWithValue("@ManagerRemark", bulkRemark ?? string.Empty);
                        cmd.Parameters.AddWithValue("@ProcessedBy", User.Identity.Name ?? HttpContext.Current.User.Identity.Name ?? "system");
                        cmd.Parameters.AddWithValue("@ProcessedOn", DateTime.Now);

                        cmd.ExecuteNonQuery();
                    }
                }
            }

            // Rebind and show message
            int requestId = Convert.ToInt32(lblModalRequestId.Text);
            BindDetailsGrid(requestId);

            lblModalMessage.CssClass = "text-success";
            lblModalMessage.Text = (newStatus == "Approved") ? "Selected rows approved." : "Selected rows rejected.";
        }

        // helper to fill footer totals by finding the column index by header text
        private void FillFooterTotals_11Feb26(GridViewRow footer)
        {
            // map header texts to footer values
            var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    {"Distance (KM)", _totalDistance.ToString("N2")},
                    {"TA Claim (₹)", _totalTAClaim.ToString("N2")},
                    {"DA (₹)", _totalDA.ToString("N2")},
                    {"Hotel (₹)", _totalHotel.ToString("N2")},
                    {"Travel (₹)", _totalTravel.ToString("N2")},
                    {"Snapshot Total (₹)", _totalSnapshot.ToString("N2")}
                };

            // attempt to find column index by header text and set footer text
            for (int i = 0; i < gvDetails.Columns.Count; i++)
            {
                string headerText = gvDetails.Columns[i].HeaderText?.Trim() ?? "";
                if (mapping.ContainsKey(headerText))
                {
                    footer.Cells[i].HorizontalAlign = HorizontalAlign.Right;
                    footer.Cells[i].Text = "<strong>" + mapping[headerText] + "</strong>";
                }
            }
        }

        private void FillFooterTotals(GridViewRow footer)
        {
            // Map header texts to the accumulated values
            var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    {"Distance (KM)", _totalDistance.ToString("N2")},
                    {"TA (₹)",        _totalTAClaim.ToString("N2")},   // TA Column Total
                    {"DA (₹)",        _totalDA.ToString("N2")},
                    {"Hotel (₹)",     _totalHotel.ToString("N2")},
                    {"Travel (₹)",    _totalTravel.ToString("N2")},
                    {"Total (₹)",     _totalSnapshot.ToString("N2")}   // Expenses Column Total
                };

            // Note: The "Grand Total" (TA + Expenses) is NOT in the grid footer columns by default.
            // It is displayed in the Modal Header. 

            for (int i = 0; i < gvDetails.Columns.Count; i++)
            {
                string headerText = gvDetails.Columns[i].HeaderText?.Trim() ?? "";
                if (mapping.ContainsKey(headerText))
                {
                    footer.Cells[i].HorizontalAlign = HorizontalAlign.Right;
                    footer.Cells[i].Text = "<strong>" + mapping[headerText] + "</strong>";
                }
            }
        }

        protected void gvDetails_RowCommand_OLD(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "ApproveRow" || e.CommandName == "RejectRow")
            {
                int approvalDetailId;
                if (!int.TryParse(e.CommandArgument?.ToString(), out approvalDetailId)) return;

                bool approve = e.CommandName == "ApproveRow";
                string mgrRemark = ""; // you could fetch the row's remark textbox if desired

                // call stored proc to update the single detail (example stored proc name)
                using (var con = new SqlConnection(connString))
                using (var cmd = new SqlCommand("dbo.usp_UpdateApprovalRequestDetailStatus", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ApprovalRequestDetailId", approvalDetailId);
                    cmd.Parameters.AddWithValue("@NewStatus", approve ? "Approved" : "Rejected");
                    cmd.Parameters.AddWithValue("@ManagerRemark", mgrRemark ?? string.Empty);
                    cmd.Parameters.AddWithValue("@ProcessedBy", User.Identity.Name ?? HttpContext.Current.User.Identity.Name ?? "system");
                    cmd.Parameters.AddWithValue("@ProcessedOn", DateTime.Now);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                // rebind grid and refresh header totals
                int reqId = Convert.ToInt32(lblModalRequestId.Text);
                BindDetailsGrid(reqId);

                // show a small message
                lblModalMessage.CssClass = "text-success";
                lblModalMessage.Text = approve ? "Detail approved." : "Detail rejected.";
            }
        }

        protected void gvDetails_RowCommand_OLD2(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "ApproveRow" || e.CommandName == "RejectRow")
            {
                // Get the row that raised the command
                var source = (Control)e.CommandSource;
                var row = source.NamingContainer as GridViewRow;
                if (row == null) return;

                // Get the ID from CommandArgument OR DataKeys
                int detailId;
                if (!int.TryParse(Convert.ToString(e.CommandArgument), out detailId))
                {
                    // fallback: use DataKeys by row index
                    if (gvDetails.DataKeys != null && row.RowIndex >= 0 && row.RowIndex < gvDetails.DataKeys.Count)
                    {
                        int.TryParse(Convert.ToString(gvDetails.DataKeys[row.RowIndex].Value), out detailId);
                    }
                }
                if (detailId <= 0) return;

                // Read remark from THIS row
                var txtRowRemark = row.FindControl("txtRowRemark") as TextBox;
                var mgrRemark = txtRowRemark != null ? txtRowRemark.Text.Trim() : string.Empty;

                // Server-side safety net: block empty remark on Reject
                if (e.CommandName == "RejectRow" && string.IsNullOrWhiteSpace(mgrRemark))
                {
                    lblModalMessage.CssClass = "text-danger";
                    lblModalMessage.Text = "Manager Remark is required to reject.";
                    upDetails.Update();
                    return; // do not update DB
                }

                var newStatus = (e.CommandName == "ApproveRow") ? "Approved" : "Rejected";

                int requestId = 0; int.TryParse(lblModalRequestId.Text, out requestId);

                try
                {
                    using (var con = new SqlConnection(connString))
                    using (var cmd = new SqlCommand("dbo.usp_UpdateApprovalRequestDetailStatus", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ApprovalRequestDetailId", detailId);
                        cmd.Parameters.AddWithValue("@NewStatus", newStatus);
                        cmd.Parameters.AddWithValue("@ManagerRemark", (object)mgrRemark ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ProcessedBy", GetLoggedInEmployeeCode());
                        cmd.Parameters.AddWithValue("@ProcessedOn", DateTime.Now);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }

                    // Rebind + keep modal open
                    BindDetailsGrid(requestId);
                    upDetails.Update();
                    ScriptManager.RegisterStartupScript(this, GetType(), "openModal", "openDetailsModal();", true);

                    lblModalMessage.CssClass = "text-success";
                    lblModalMessage.Text = (newStatus == "Approved") ? "Detail approved." : "Detail rejected.";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error updating detail status: " + ex);
                    lblModalMessage.CssClass = "text-danger";
                    lblModalMessage.Text = "Error updating status. See server logs.";
                    upDetails.Update();
                }
            }
        }

        protected void gvDetails_RowCommand_OLD3(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "ApproveRow" || e.CommandName == "RejectRow")
            {
                string newStatus = (e.CommandName == "ApproveRow") ? "Approved" : "Rejected";
                int detailId = 0;
                if (!int.TryParse(Convert.ToString(e.CommandArgument), out detailId)) return;

                int requestId = 0;
                int parsedRequestId;
                if (int.TryParse(lblModalRequestId.Text, out parsedRequestId))
                {
                    requestId = parsedRequestId;
                }

                GridViewRow row = (GridViewRow)((Control)e.CommandSource).NamingContainer;
                TextBox txtRowRemark = row.FindControl("txtRowRemark") as TextBox;
                string mgrRemark = txtRowRemark != null ? txtRowRemark.Text.Trim() : string.Empty;

                string processedBy = GetLoggedInEmployeeCode();
                DateTime processedOn = DateTime.Now;

                using (SqlConnection con = new SqlConnection(connString))
                using (SqlCommand cmd = new SqlCommand("dbo.usp_UpdateApprovalRequestDetailStatus_AndMaybeActivity", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ApprovalRequestDetailId", detailId);
                    cmd.Parameters.AddWithValue("@NewStatus", newStatus);
                    cmd.Parameters.AddWithValue("@ManagerRemark", mgrRemark);
                    cmd.Parameters.AddWithValue("@ProcessedBy", processedBy);
                    cmd.Parameters.AddWithValue("@ProcessedOn", processedOn);
                    // If you want to FORCE setting activity's ClaimStatus immediately for single-row approve, set to 1:
                    cmd.Parameters.AddWithValue("@ForceSetActivityClaim", 1);
                    //cmd.Parameters.AddWithValue("@ForceSetActivityClaim", 0);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
                // Rebind the grid and update UpdatePanel
                BindDetailsGrid(requestId);
                upDetails.Update();

                lblModalMessage.CssClass = "text-success";
                lblModalMessage.Text = (newStatus == "Approved") ? "Detail approved." : "Detail rejected.";
            }
        }

        protected void gvDetails_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "ApproveRow" && e.CommandName != "RejectRow")
                return;

            // Identify row
            Control source = (Control)e.CommandSource;
            GridViewRow row = source.NamingContainer as GridViewRow;
            if (row == null) return;

            // Detail Id
            int detailId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out detailId))
                return;

            // ApprovalRequestId (single source of truth)
            if (string.IsNullOrWhiteSpace(hfActionRequestId.Value))
                return;

            int requestId = Convert.ToInt32(hfActionRequestId.Value);

            // 🔑 ALWAYS restore approver context
            int currentUserLevel = GetCurrentUserApproverLevel(requestId);
            ViewState["CurrentUserLevel"] = currentUserLevel;

            // Remarks (generic approver remark, NOT manager-specific)
            TextBox txtRowRemark = row.FindControl("txtRowRemark") as TextBox;
            string approverRemark = txtRowRemark != null
                ? txtRowRemark.Text.Trim()
                : string.Empty;

            // Reject requires remark
            if (e.CommandName == "RejectRow" && string.IsNullOrWhiteSpace(approverRemark))
            {
                lblModalMessage.CssClass = "text-danger";
                lblModalMessage.Text = "Remark is required to reject.";
                upDetails.Update();
                return;
            }

            bool isApprove = e.CommandName == "ApproveRow";
            string newStatus = isApprove ? "Approved" : "Rejected";

            string processedBy = GetLoggedInEmployeeCode();

            DateTime processedOn;
            try
            {
                processedOn = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
                    DateTime.UtcNow, "India Standard Time");
            }
            catch
            {
                processedOn = DateTime.Now;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                using (SqlCommand cmd = new SqlCommand(
                    "dbo.usp_ProcessSalesApprovalRequestDetail", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@ApprovalRequestDetailId", detailId);
                    cmd.Parameters.AddWithValue("@ApprovalRequestId", requestId);

                    cmd.Parameters.AddWithValue(
                        "@Action",
                        isApprove ? "APPROVE" : "REJECT"
                    );

                    cmd.Parameters.AddWithValue("@ActionBy", processedBy);

                    cmd.Parameters.AddWithValue(
                        "@ActionRemarks",
                        string.IsNullOrWhiteSpace(approverRemark)
                            ? (object)DBNull.Value
                            : approverRemark
                    );

                    con.Open();
                    cmd.ExecuteNonQuery();
                }


                // 🔁 Rebind safely (context-aware)
                BindDetailsGrid(requestId);
                upDetails.Update();

                ScriptManager.RegisterStartupScript(
                    this, GetType(), "openModal", "openDetailsModal();", true);

                lblModalMessage.CssClass = "text-success";
                lblModalMessage.Text = isApprove
                    ? "Detail approved successfully."
                    : "Detail rejected successfully.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Detail approval error: " + ex);

                lblModalMessage.CssClass = "text-danger";
                lblModalMessage.Text = "Error while updating detail status.";
            }

            upDetails.Update();
        }

        //protected void gvDetails_RowCommand(object sender, GridViewCommandEventArgs e)
        //{
        //    if (e.CommandName != "ApproveRow" && e.CommandName != "RejectRow")
        //        return;

        //    // Identify the row and get the ID
        //    var source = (Control)e.CommandSource;
        //    var row = source.NamingContainer as GridViewRow;
        //    if (row == null) return;

        //    int detailId = 0;
        //    if (!int.TryParse(Convert.ToString(e.CommandArgument), out detailId))
        //    {
        //        // fallback via DataKeys
        //        if (gvDetails.DataKeys != null && row.RowIndex >= 0 && row.RowIndex < gvDetails.DataKeys.Count)
        //        {
        //            int.TryParse(Convert.ToString(gvDetails.DataKeys[row.RowIndex].Value), out detailId);
        //        }
        //    }
        //    if (detailId <= 0) return;

        //    // Get manager remark from the row textbox
        //    var txtRowRemark = row.FindControl("txtRowRemark") as TextBox;
        //    string mgrRemark = txtRowRemark != null ? txtRowRemark.Text.Trim() : string.Empty;

        //    // Reject requires remark
        //    if (e.CommandName == "RejectRow" && string.IsNullOrWhiteSpace(mgrRemark))
        //    {
        //        lblModalMessage.CssClass = "text-danger";
        //        lblModalMessage.Text = "Manager Remark is required to reject.";
        //        upDetails.Update();
        //        return;
        //    }

        //    string newStatus = (e.CommandName == "ApproveRow") ? "Approved" : "Rejected";

        //    int requestId = 0;
        //    int.TryParse(lblModalRequestId.Text, out requestId);

        //    string processedBy = GetLoggedInEmployeeCode();

        //    // Get current IST time
        //    DateTime processedOn;
        //    try
        //    {
        //        processedOn = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "India Standard Time");
        //    }
        //    catch
        //    {
        //        processedOn = DateTime.Now;
        //    }

        //    try
        //    {
        //        using (var con = new SqlConnection(connString))
        //        using (var cmd = new SqlCommand("dbo.usp_UpdateApprovalRequestDetailStatus_AndMaybeActivity", con))
        //        {
        //            cmd.CommandType = CommandType.StoredProcedure;
        //            cmd.Parameters.AddWithValue("@ApprovalRequestDetailId", detailId);
        //            cmd.Parameters.AddWithValue("@NewStatus", newStatus);
        //            cmd.Parameters.AddWithValue("@ManagerRemark", string.IsNullOrEmpty(mgrRemark) ? (object)DBNull.Value : mgrRemark);
        //            cmd.Parameters.AddWithValue("@ProcessedBy", processedBy);
        //            cmd.Parameters.AddWithValue("@ProcessedOn", processedOn);
        //            cmd.Parameters.AddWithValue("@ForceSetActivityClaim", 1); // 🔸 Always set to 1 as required

        //            con.Open();
        //            cmd.ExecuteNonQuery();
        //        }

        //        // Rebind and keep modal open
        //        BindDetailsGrid(requestId);
        //        upDetails.Update();
        //        ScriptManager.RegisterStartupScript(this, GetType(), "openModal", "openDetailsModal();", true);

        //        lblModalMessage.CssClass = "text-success";
        //        lblModalMessage.Text = (newStatus == "Approved") ? "Detail approved." : "Detail rejected.";
        //        upDetails.Update();
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine("Error updating detail status: " + ex);
        //        lblModalMessage.CssClass = "text-danger";
        //        lblModalMessage.Text = "Error updating status. See server logs.";
        //        upDetails.Update();
        //    }
        //}


        private decimal SafeDecNew(object value)
        {
            if (value == null || value == DBNull.Value) return 0m;
            try
            {
                return Convert.ToDecimal(value);
            }
            catch
            {
                return 0m;
            }
        }

        // --- returns history rows for display ---
        //[WebMethod(EnableSession = true)]
        //[ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        //public static object GetKMHistory(int activityId)
        //{
        //    var results = new List<object>();
        //    string connString = System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        //    using (var con = new System.Data.SqlClient.SqlConnection(connString))
        //    using (var cmd = new System.Data.SqlClient.SqlCommand(
        //        "SELECT HistoryId, ActivityId, OldKM, NewKM, ModifiedBy, ModifiedDateTime, Reason FROM dbo.SalesDailyActivityKMHistory WHERE ActivityId = @ActivityId ORDER BY ModifiedDateTime DESC", con))
        //    {
        //        cmd.Parameters.AddWithValue("@ActivityId", activityId);
        //        con.Open();
        //        using (var rdr = cmd.ExecuteReader())
        //        {
        //            while (rdr.Read())
        //            {
        //                DateTime utc = rdr.GetDateTime(rdr.GetOrdinal("ModifiedDateTime"));
        //                // convert to IST string for display
        //                DateTime ist;
        //                try
        //                {
        //                    ist = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(utc, "UTC", "India Standard Time");
        //                }
        //                catch
        //                {
        //                    ist = utc.AddHours(5).AddMinutes(30);
        //                }
        //                results.Add(new
        //                {
        //                    HistoryId = rdr["HistoryId"],
        //                    ActivityId = rdr["ActivityId"],
        //                    OldKM = rdr["OldKM"],
        //                    NewKM = rdr["NewKM"],
        //                    ModifiedBy = rdr["ModifiedBy"] as string,
        //                    ModifiedDateTime = utc.ToString("dd-MM-yyyy HH:mm:ss tt"),
        //                    ModifiedDateTimeIST = ist.ToString("dd-MM-yyyy HH:mm:ss tt"),
        //                    Reason = rdr["Reason"] as string
        //                });
        //            }
        //        }
        //    }
        //    return results;
        //}


        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object GetKMHistory(int activityId)
        {
            var results = new List<object>();
            string connString = System.Configuration.ConfigurationManager
                                    .ConnectionStrings["DbConn"].ConnectionString;

            using (var con = new System.Data.SqlClient.SqlConnection(connString))
            using (var cmd = new System.Data.SqlClient.SqlCommand(
                "dbo.usp_GetSalesDailyActivityKMHistory", con))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ActivityId", activityId);

                con.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        DateTime utc = rdr.GetDateTime(
                            rdr.GetOrdinal("ModifiedDateTime"));

                        // Convert UTC → IST for display
                        DateTime ist;
                        try
                        {
                            ist = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
                                utc, "UTC", "India Standard Time");
                        }
                        catch
                        {
                            ist = utc.AddHours(5).AddMinutes(30);
                        }

                        results.Add(new
                        {
                            HistoryId = rdr["HistoryId"],
                            ActivityId = rdr["ActivityId"],
                            OldKM = rdr["OldKM"],
                            NewKM = rdr["NewKM"],
                            ModifiedBy = rdr["ModifiedBy"] as string,
                            Reason = rdr["Reason"] as string,

                            TravelFromPlace = rdr["TravelFromPlace"] as string,
                            TravelToPlace = rdr["TravelToPlace"] as string,

                            ModifiedDateTime = utc.ToString("dd-MM-yyyy HH:mm:ss tt"),
                            ModifiedDateTimeIST = ist.ToString("dd-MM-yyyy HH:mm:ss tt")
                        });
                    }
                }
            }
            return results;
        }


        //[WebMethod(EnableSession = true)]
        //[ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        //public static object UpdateActivityKM(int activityId, decimal? newKm, string reason)
        //{
        //    try
        //    {
        //        if (!newKm.HasValue)
        //            return new { success = false, message = "New KM value is required." };

        //        decimal nm = newKm.Value;
        //        string connString = System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        //        // Check ClaimStatus
        //        string Status = null;
        //        using (var con = new System.Data.SqlClient.SqlConnection(connString))
        //        using (var cmd = new System.Data.SqlClient.SqlCommand(
        //            "SELECT TOP 1 DetailStatus FROM dbo.SalesApprovalRequestDetail WHERE ActivityId = @ActivityId", con))
        //        {
        //            cmd.Parameters.AddWithValue("@ActivityId", activityId);
        //            con.Open();
        //            var obj = cmd.ExecuteScalar();
        //            Status = obj == DBNull.Value || obj == null ? null : obj.ToString();
        //        }

        //        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Draft", "Submitted" };
        //        if (!allowed.Contains(Status ?? string.Empty))
        //        {
        //            return new { success = false, message = $"KM cannot be edited when Status = '{Status ?? "Unknown"}'." };
        //        }

        //        // call stored procedure
        //        string changedBy = (HttpContext.Current.Session["WORKMAN"] as string) ?? (HttpContext.Current.User?.Identity?.Name ?? "system");

        //        using (var con = new System.Data.SqlClient.SqlConnection(connString))
        //        using (var cmd = new System.Data.SqlClient.SqlCommand("dbo.usp_UpdateActivityKM_WithHistory", con))
        //        {
        //            cmd.CommandType = System.Data.CommandType.StoredProcedure;
        //            cmd.Parameters.AddWithValue("@ActivityId", activityId);
        //            cmd.Parameters.AddWithValue("@NewKM", nm);
        //            cmd.Parameters.AddWithValue("@ChangedBy", changedBy);
        //            cmd.Parameters.AddWithValue("@Reason", (object)reason ?? DBNull.Value);

        //            con.Open();
        //            cmd.CommandTimeout = 60;
        //            cmd.ExecuteNonQuery();
        //        }

        //        return new { success = true };
        //    }
        //    catch (System.Data.SqlClient.SqlException sqlex)
        //    {
        //        return new { success = false, message = sqlex.Message };
        //    }
        //    catch (System.Exception ex)
        //    {
        //        return new { success = false, message = ex.Message };
        //    }
        //}



        //[WebMethod(EnableSession = true)]
        //[ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        //public static object UpdateActivityKM(int activityId, decimal newKm, string reason)
        //{
        //    try
        //    {
        //        string connString = System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        //        string claimStatus = null;
        //        using (var con = new System.Data.SqlClient.SqlConnection(connString))
        //        using (var cmd = new System.Data.SqlClient.SqlCommand(
        //            "SELECT TOP 1 ClaimStatus FROM dbo.SalesApprovalRequestDetail WHERE ActivityId = @ActivityId", con))
        //        {
        //            cmd.Parameters.AddWithValue("@ActivityId", activityId);
        //            con.Open();
        //            var obj = cmd.ExecuteScalar();
        //            claimStatus = obj == DBNull.Value || obj == null ? null : obj.ToString();
        //        }

        //        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Draft", "Submitted" };
        //        if (!allowed.Contains(claimStatus ?? string.Empty))
        //        {
        //            return new { success = false, message = $"KM cannot be edited when ClaimStatus = '{claimStatus ?? "Unknown"}'." };
        //        }

        //        string changedBy = (HttpContext.Current.Session["WORKMAN"] as string) ?? (HttpContext.Current.User?.Identity?.Name ?? "system");

        //        using (var con = new System.Data.SqlClient.SqlConnection(connString))
        //        using (var cmd = new System.Data.SqlClient.SqlCommand("dbo.usp_UpdateActivityKM_WithHistory", con))
        //        {
        //            cmd.CommandType = System.Data.CommandType.StoredProcedure;
        //            cmd.Parameters.AddWithValue("@ActivityId", activityId);
        //            cmd.Parameters.AddWithValue("@NewKM", newKm);
        //            cmd.Parameters.AddWithValue("@ChangedBy", changedBy);
        //            cmd.Parameters.AddWithValue("@Reason", (object)reason ?? DBNull.Value);

        //            con.Open();
        //            cmd.CommandTimeout = 60;
        //            cmd.ExecuteNonQuery();
        //        }

        //        return new { success = true };
        //    }
        //    catch (System.Data.SqlClient.SqlException sqlex)
        //    {
        //        return new { success = false, message = sqlex.Message };
        //    }
        //    catch (System.Exception ex)
        //    {
        //        return new { success = false, message = ex.Message };
        //    }
        //}

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object UpdateActivityKM(int activityId, decimal? newKm, string reason)
        {
            try
            {
                if (!newKm.HasValue)
                    return new { success = false, message = "New KM value is required." };

                string connString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

                bool canEdit = true;
                int approvalRequestId;

                using (var con = new SqlConnection(connString))
                using (var cmd = new SqlCommand(@"
                    SELECT TOP 1 ApprovalRequestId
                    FROM dbo.SalesApprovalRequestDetail
                    WHERE ActivityId = @ActivityId", con))
                {
                    cmd.Parameters.AddWithValue("@ActivityId", activityId);
                    con.Open();
                    approvalRequestId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // 🔒 Check Accounts stage
                using (var con = new SqlConnection(connString))
                using (var cmd = new SqlCommand(@"
                    SELECT TOP 1 Status
                    FROM dbo.SalesApprovalRequestApprover
                    WHERE ApprovalRequestId = @ApprovalRequestId
                      AND ApproverRole = 'Accounts'", con))
                {
                    cmd.Parameters.AddWithValue("@ApprovalRequestId", approvalRequestId);
                    con.Open();

                    var statusObj = cmd.ExecuteScalar();
                    string accountsStatus = statusObj?.ToString();

                    // Lock ONLY if Accounts has acted
                    if (!string.IsNullOrEmpty(accountsStatus) &&
                        !accountsStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                    {
                        canEdit = false;
                    }
                }

                if (!canEdit)
                {
                    return new
                    {
                        success = false,
                        message = "KM editing is locked as Accounts has already processed the request."
                    };
                }



                string changedBy =
                    (HttpContext.Current.Session["WORKMAN"] as string)
                    ?? HttpContext.Current.User.Identity.Name
                    ?? "system";

                using (var con = new SqlConnection(connString))
                using (var cmd = new SqlCommand("dbo.usp_UpdateActivityKM_WithHistory", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ActivityId", activityId);
                    cmd.Parameters.AddWithValue("@NewKM", newKm.Value);
                    cmd.Parameters.AddWithValue("@ChangedBy", changedBy);
                    cmd.Parameters.AddWithValue("@Reason", (object)reason ?? DBNull.Value);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                return new { success = true };
            }
            catch (SqlException ex)
            {
                return new { success = false, message = ex.Message };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }


        protected void gvRequests_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvRequests.PageIndex = e.NewPageIndex;
            BindRequestsGrid();
        }

        protected void gvRequests_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "View")
            {
                int approvalId = Convert.ToInt32(e.CommandArgument);
                int approvalRequestId;
                if (!int.TryParse(e.CommandArgument?.ToString(), out approvalRequestId)) return;

                // 🔑 SET CURRENT USER LEVEL (MANDATORY)
                ViewState["CurrentUserLevel"] = GetCurrentUserApproverLevel(approvalRequestId);

                lblModalRequestId.Text = approvalRequestId.ToString();
                hfActionRequestId.Value = approvalRequestId.ToString();

                BindDetailsGrid(approvalRequestId);
                upDetails.Update();

                ScriptManager.RegisterStartupScript(this, GetType(), "logGV",
                    $"console.log('After bind - gvDetails client id = {gvDetails.ClientID}');", true);

                ScriptManager.RegisterStartupScript(this, GetType(), "openModal", "openDetailsModal();", true);

                ShowDetailsModal(approvalRequestId);
                // Put this after DataBind() of gvDetails or just for debugging in Page_Load (when !IsPostBack you can skip)
                //ScriptManager.RegisterStartupScript(this, GetType(), "logGVId", $"console.log('gvDetails client id = {gvDetails.ClientID}');", true);

            }
            else if (e.CommandName == "ApproveQuick")
            {
                int approvalId = Convert.ToInt32(e.CommandArgument);
                ShowDetailsModal(approvalId);
            }
        }

        protected void ShowDetailsModal(int approvalRequestId)
        {
            // Store id in hidden field for later actions
            hfActionRequestId.Value = approvalRequestId.ToString();
            lblModalRequestId.Text = approvalRequestId.ToString();

            // Bind the grid (this will now have correct footer totals due to the fix above)
            BindDetailsGrid(approvalRequestId);

            // 1️⃣ UPDATED SQL: Fetch User Info + Both Amounts (TA & Expenses)
            string headerSql = @"
                SELECT 
                    r.TotalClaimAmount, 
                    r.TotalExpenseAmount, 
                    r.Status,
                    r.RequesterLoginId,
                    ISNULL(u.EmployeeName, 'Unknown') as EmployeeName
                FROM dbo.SalesApprovalRequest r
                LEFT JOIN dbo.MST_UserMaster u ON r.RequesterLoginId = u.EmployeeCode
                WHERE r.ApprovalRequestId = @ApprovalRequestId";

            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand(headerSql, con))
            {
                cmd.Parameters.AddWithValue("@ApprovalRequestId", approvalRequestId);
                con.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        // 2️⃣ Populate User Info
                        // Ensure you have these Label controls (lblRequesterName, lblRequesterCode) in your .aspx
                        // If the controls don't exist yet, you might need to check for null or add them to markup first.
                        var lblName = detailsModal.FindControl("lblRequesterName") as Label;
                        var lblCode = detailsModal.FindControl("lblRequesterCode") as Label;

                        if (lblName != null) lblName.Text = rdr["EmployeeName"].ToString();
                        if (lblCode != null) lblCode.Text = rdr["RequesterLoginId"].ToString();

                        // 3️⃣ Calculate Grand Total (TA + Expenses)
                        decimal taAmount = rdr["TotalClaimAmount"] != DBNull.Value ? Convert.ToDecimal(rdr["TotalClaimAmount"]) : 0m;
                        decimal expAmount = rdr["TotalExpenseAmount"] != DBNull.Value ? Convert.ToDecimal(rdr["TotalExpenseAmount"]) : 0m;

                        decimal grandTotal = taAmount + expAmount;

                        lblHeaderTotalClaim.Text = grandTotal.ToString("N2");
                        lblHeaderStatus.Text = rdr["Status"]?.ToString() ?? "Pending";
                    }
                }
            }

            // --- Existing Permission/Button Logic ---
            string approverRole = GetCurrentUserApproverRole(approvalRequestId);
            bool isAuditor = IsAuditor(approvalRequestId);

            // Reopen button logic
            btnReopenRequest.Visible = isAuditor && lblHeaderStatus.Text != null &&
            (
                lblHeaderStatus.Text.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
                lblHeaderStatus.Text.Equals("Rejected", StringComparison.OrdinalIgnoreCase)
            );

            // Ready for Payment button logic
            // int currentUserLevel = Convert.ToInt32(ViewState["CurrentUserLevel"] ?? 0); // (Not strictly needed here if purely Auditor check)
            btnMarkReadyForPayment.Visible = isAuditor && lblHeaderStatus.Text != null &&
            lblHeaderStatus.Text.Equals("Completed", StringComparison.OrdinalIgnoreCase);

            // Open modal safely
            ScriptManager.RegisterStartupScript(this, GetType(), "openModal", "$('#detailsModal').modal('show');", true);
        }

        protected void ShowDetailsModal_OLD(int approvalRequestId)
        {
            // store id in hidden field for later actions
            hfActionRequestId.Value = approvalRequestId.ToString();
            lblModalRequestId.Text = approvalRequestId.ToString();

            BindDetailsGrid(approvalRequestId);

            // 1️⃣ UPDATED SQL: Fetch User Info + Both Amounts (TA & Expenses)
            string headerSql = @"
                SELECT 
                    r.TotalClaimAmount, 
                    r.TotalExpenseAmount, 
                    r.Status,
                    r.RequesterLoginId,
                    ISNULL(u.EmployeeName, 'Unknown') as EmployeeName
                FROM dbo.SalesApprovalRequest r
                LEFT JOIN dbo.MST_UserMaster u ON r.RequesterLoginId = u.EmployeeCode
                WHERE r.ApprovalRequestId = @ApprovalRequestId";

            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand(headerSql, con))
            {
                cmd.Parameters.AddWithValue("@ApprovalRequestId", approvalRequestId);
                con.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        // 2️⃣ Populate User Info
                        lblRequesterName.Text = rdr["EmployeeName"].ToString();
                        lblRequesterCode.Text = rdr["RequesterLoginId"].ToString();

                        // 3️⃣ Calculate Grand Total (TA + Expenses) matching your Grid logic
                        decimal taAmount = rdr["TotalClaimAmount"] != DBNull.Value ? Convert.ToDecimal(rdr["TotalClaimAmount"]) : 0m;
                        decimal expAmount = rdr["TotalExpenseAmount"] != DBNull.Value ? Convert.ToDecimal(rdr["TotalExpenseAmount"]) : 0m;

                        decimal grandTotal = taAmount + expAmount;

                        lblHeaderTotalClaim.Text = grandTotal.ToString("N2");
                        lblHeaderStatus.Text = rdr["Status"]?.ToString() ?? "Pending";
                    }
                }
            }

            // ... (Existing logic for btnReopenRequest / Auditor checks remains the same)
            string approverRole = GetCurrentUserApproverRole(approvalRequestId);
            bool isAuditor = IsAuditor(approvalRequestId);

            btnReopenRequest.Visible = isAuditor && lblHeaderStatus.Text != null &&
            (
                lblHeaderStatus.Text.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
                lblHeaderStatus.Text.Equals("Rejected", StringComparison.OrdinalIgnoreCase)
            );

            int currentUserLevel = Convert.ToInt32(ViewState["CurrentUserLevel"] ?? 0);
            btnMarkReadyForPayment.Visible = isAuditor && lblHeaderStatus.Text != null &&
            lblHeaderStatus.Text.Equals("Completed", StringComparison.OrdinalIgnoreCase);

            // open modal
            ScriptManager.RegisterStartupScript(this, GetType(), "openModal", "$('#detailsModal').modal('show');", true);
        }

        //protected void ShowDetailsModal_OLD(int approvalRequestId)
        //{
        //    // store id in hidden field for later actions
        //    hfActionRequestId.Value = approvalRequestId.ToString();
        //    lblModalRequestId.Text = approvalRequestId.ToString();

        //    BindDetailsGrid(approvalRequestId);

        //    // fetch header totals & status (optional display)
        //    string headerSql = "SELECT TotalExpenseAmount, Status FROM dbo.SalesApprovalRequest WHERE ApprovalRequestId = @ApprovalRequestId";
        //    using (var con = new SqlConnection(connString))
        //    using (var cmd = new SqlCommand(headerSql, con))
        //    {
        //        cmd.Parameters.AddWithValue("@ApprovalRequestId", approvalRequestId);
        //        con.Open();
        //        using (var rdr = cmd.ExecuteReader())
        //        {
        //            if (rdr.Read())
        //            {
        //                lblHeaderTotalClaim.Text = rdr["TotalExpenseAmount"] == DBNull.Value ? "0.00" : Convert.ToDecimal(rdr["TotalExpenseAmount"]).ToString("N2");
        //                lblHeaderStatus.Text = rdr["Status"]?.ToString() ?? "Pending";
        //            }
        //        }
        //    }

        //    string approverRole = GetCurrentUserApproverRole(approvalRequestId);
        //    bool isAuditor = IsAuditor(approvalRequestId);

        //    btnReopenRequest.Visible = isAuditor && lblHeaderStatus.Text != null &&
        //    (
        //        lblHeaderStatus.Text.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
        //        lblHeaderStatus.Text.Equals("Rejected", StringComparison.OrdinalIgnoreCase)
        //    );

        //    int currentUserLevel = Convert.ToInt32(ViewState["CurrentUserLevel"] ?? 0);
        //    btnMarkReadyForPayment.Visible = isAuditor && lblHeaderStatus.Text != null &&
        //    lblHeaderStatus.Text.Equals("Completed", StringComparison.OrdinalIgnoreCase);

        //    // open modal (partial postback safe)
        //    ScriptManager.RegisterStartupScript(this, GetType(), "openModal", "$('#detailsModal').modal('show');", true);
        //}

        private bool IsAuditor(int approvalRequestId)
        {
            string role = GetCurrentUserApproverRole(approvalRequestId);
            return !string.IsNullOrEmpty(role) &&
                   role.Equals("Auditor", StringComparison.OrdinalIgnoreCase);
        }


        private string GetCurrentUserApproverRole(int approvalRequestId)
        {
            using (SqlConnection con = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand(@"
                SELECT ApproverRole
                FROM SalesApprovalRequestApprover
                WHERE ApprovalRequestId = @ApprovalRequestId
                  AND ApproverLoginId = @LoginId", con))
            {
                cmd.Parameters.AddWithValue("@ApprovalRequestId", approvalRequestId);
                cmd.Parameters.AddWithValue("@LoginId", GetLoggedInEmployeeCode());

                con.Open();
                object role = cmd.ExecuteScalar();
                return role == null ? null : role.ToString();
            }
        }


        protected void btnReopenRequest_Click(object sender, EventArgs e)
        {
            int approvalRequestId = Convert.ToInt32(lblModalRequestId.Text);
            string auditor = GetLoggedInEmployeeCode();

            using (SqlConnection con = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_ReopenApprovalRequestByAuditor", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ApprovalRequestId", approvalRequestId);
                cmd.Parameters.AddWithValue("@ActionBy", auditor);
                cmd.Parameters.AddWithValue("@Remarks", "Reopened by Auditor");
                con.Open();
                cmd.ExecuteNonQuery();
            }

            // 🔁 Refresh UI
            BindDetailsGrid(approvalRequestId);
            BindRequestsGrid();

            lblModalMessage.Text = "Request reopened successfully.";
            lblModalMessage.CssClass = "text-success";

            ScriptManager.RegisterStartupScript(
                this, GetType(), "openModal", "openDetailsModal();", true);
        }


        protected void BindDetailsGrid_Direct(int approvalRequestId)
        {
            string sql = @"
            SELECT
                d.ApprovalRequestDetailId,
                d.ApprovalRequestId,
                d.ActivityId,
                a.ActivityDate,
                a.User_Id,
                a.User_Name,
                a.Login_Id,
                a.TotalDistanceKM,
                a.CalculatedClaimAmount,
                a.ClaimStatus,
                a.ClaimProcessedBy,
                a.ClaimProcessedDate,
                d.Snapshot_TotalDistanceKM,
                d.Snapshot_CalculatedClaimAmount,
                d.Snapshot_DAAmount,
                d.Snapshot_HotelAmount,
                d.Snapshot_TravelAmount,
                d.Snapshot_ExpenseTotal,
                d.DetailStatus,
                d.DetailRemarks,
                d.DetailApprovedBy,
                d.DetailApprovedOn,
                d.CreatedOn as DetailCreatedOn,
                a.Remarks as ActivityRemarks
            FROM dbo.SalesApprovalRequestDetail d
            INNER JOIN dbo.SalesDailyActivity a ON a.ActivityId = d.ActivityId
            WHERE d.ApprovalRequestId = @ApprovalRequestId
            ORDER BY d.ApprovalRequestDetailId;";

            DataTable dt = new DataTable();
            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@ApprovalRequestId", approvalRequestId);
                using (var da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }

            gvDetails.DataSource = dt;
            gvDetails.DataBind();
        }

        //protected void BindDetailsGrid(int approvalRequestId)
        //{
        //    // 🔑 ALWAYS set before binding
        //    ViewState["CurrentUserLevel"] = GetCurrentUserApproverLevel(approvalRequestId);

        //    DataTable dt = new DataTable();
        //    using (var con = new SqlConnection(connString))
        //    using (var cmd = new SqlCommand("dbo.usp_GetApprovalRequestDetails", con))
        //    {
        //        cmd.CommandType = CommandType.StoredProcedure;
        //        cmd.Parameters.AddWithValue("@ApprovalRequestId", approvalRequestId);

        //        using (var da = new SqlDataAdapter(cmd))
        //        {
        //            da.Fill(dt);
        //        }
        //    }

        //    gvDetails.DataSource = dt;
        //    gvDetails.DataBind();
        //}

        protected void BindDetailsGrid(int approvalRequestId)
        {
            // 1️⃣ RESET ACCUMULATORS (Crucial Fix: Resets totals before summing new rows)
            _totalDistance = 0m;
            _totalTAClaim = 0m;   // TA
            _totalDA = 0m;
            _totalHotel = 0m;
            _totalTravel = 0m;
            _totalSnapshot = 0m;  // Expenses (DA + Hotel + Travel)

            // 🔑 ALWAYS set authority before binding so RowDataBound knows the level
            ViewState["CurrentUserLevel"] = GetCurrentUserApproverLevel(approvalRequestId);

            DataTable dt = new DataTable();
            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand("dbo.usp_GetApprovalRequestDetails", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ApprovalRequestId", approvalRequestId);

                using (var da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }

            gvDetails.DataSource = dt;
            gvDetails.DataBind();

            // ✅ ADD THIS LINE: This forces the GridView to use <thead> for the header row
            if (gvDetails.Rows.Count > 0)
            {
                gvDetails.HeaderRow.TableSection = TableRowSection.TableHeader;
            }
        }


        // optionally disable inputs for already approved rows
        protected void gvDetails_RowDataBound_OLD(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var drv = (DataRowView)e.Row.DataItem;
            string detailStatus = drv["DetailStatus"] == DBNull.Value ? null : drv["DetailStatus"].ToString();

            var chk = e.Row.FindControl("chkSelect") as CheckBox;
            var txt = e.Row.FindControl("txtRowRemark") as TextBox;

            if (!string.IsNullOrEmpty(detailStatus) && detailStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase))
            {
                if (chk != null) chk.Enabled = false;
                if (txt != null) { txt.ReadOnly = true; txt.CssClass += " bg-light"; }
            }
        }

        // ------------- Collect selected ids + per-row remarks (helpers used by Approve/Reject handlers) --------------
        private DataTable BuildSelectedIdsTVP_OLD11Feb26()
        {
            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(int));

            foreach (GridViewRow row in gvDetails.Rows)
            {
                var chk = row.FindControl("chkSelect") as CheckBox;
                if (chk != null && chk.Checked)
                {
                    int detailId = Convert.ToInt32(gvDetails.DataKeys[row.RowIndex].Value);
                    dt.Rows.Add(detailId);
                }
            }
            return dt;
        }

        private DataTable BuildSelectedIdsTVP()
        {
            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(int));

            foreach (GridViewRow row in gvDetails.Rows)
            {
                // 1️⃣ Find the Checkbox
                var chk = row.FindControl("chkSelect") as CheckBox;

                // 2️⃣ CHECK ENABLED STATE (Critical Safety Check)
                // We only want rows that are actually actionable (Enabled).
                if (chk != null && chk.Checked && chk.Enabled)
                {
                    // 3️⃣ Safe to add
                    int detailId = Convert.ToInt32(gvDetails.DataKeys[row.RowIndex].Value);
                    dt.Rows.Add(detailId);
                }
            }
            return dt;
        }

        private DataTable BuildDetailRemarksTVP()
        {
            var dt = new DataTable();
            dt.Columns.Add("ApprovalRequestDetailId", typeof(int));
            dt.Columns.Add("Remark", typeof(string));

            foreach (GridViewRow row in gvDetails.Rows)
            {
                int detailId = Convert.ToInt32(gvDetails.DataKeys[row.RowIndex].Value);
                var txt = row.FindControl("txtRowRemark") as TextBox;
                if (txt != null)
                {
                    string remark = txt.Text?.Trim();
                    if (!string.IsNullOrEmpty(remark))
                    {
                        dt.Rows.Add(detailId, remark);
                    }
                }
            }
            return dt;
        }

        // ------------- Approve / Reject Selected button handlers (call your stored proc) --------------
        protected void btnApproveSelected_Click(object sender, EventArgs e)
        {
            ProcessSelectedDetails("Approve");
        }

        protected void btnRejectSelected_Click(object sender, EventArgs e)
        {
            // require remarks if rejecting (either per-row or bulk)
            var tvpRemarks = BuildDetailRemarksTVP();
            var tvpIds = BuildSelectedIdsTVP();
            if (tvpIds.Rows.Count == 0)
            {
                lblModalMessage.Text = "Select at least one item to reject.";
                return;
            }
            if (tvpRemarks.Rows.Count == 0 && string.IsNullOrWhiteSpace(txtBulkRemarks.Text))
            {
                lblModalMessage.Text = "Provide per-row remark or bulk remark before rejecting.";
                return;
            }
            ProcessSelectedDetails("Reject");
        }

        private void ProcessSelectedDetails_OLD(string action)
        {
            lblModalMessage.Text = string.Empty;
            int approvalRequestId = int.Parse(hfActionRequestId.Value);
            string manager = Page.User?.Identity?.Name ?? "MANAGER";
            string bulkRemarks = string.IsNullOrWhiteSpace(txtBulkRemarks.Text) ? null : txtBulkRemarks.Text.Trim();

            var dtIds = BuildSelectedIdsTVP();
            if (dtIds.Rows.Count == 0)
            {
                lblModalMessage.Text = "Please select at least one row.";
                return;
            }
            var dtRemarks = BuildDetailRemarksTVP();

            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand("usp_ProcessApprovalRequestDetails_WithRemarks", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                var pIds = cmd.Parameters.AddWithValue("@DetailIds", dtIds);
                pIds.SqlDbType = SqlDbType.Structured;
                pIds.TypeName = "dbo.TVP_IntIds";

                var pRemarks = cmd.Parameters.AddWithValue("@DetailRemarks", dtRemarks);
                pRemarks.SqlDbType = SqlDbType.Structured;
                pRemarks.TypeName = "dbo.TVP_DetailRemarks";

                cmd.Parameters.AddWithValue("@ApprovalRequestId", approvalRequestId);
                cmd.Parameters.AddWithValue("@Action", action);
                cmd.Parameters.AddWithValue("@ChangedBy", manager);
                cmd.Parameters.AddWithValue("@BulkRemarks", (object)bulkRemarks ?? DBNull.Value);

                con.Open();
                cmd.CommandTimeout = 120;
                cmd.ExecuteNonQuery();
            }

            // refresh UI
            BindDetailsGrid(approvalRequestId);
            ShowDetailsModal(approvalRequestId);
            BindRequestsGrid();

            // keep modal open and show success
            lblModalMessage.CssClass = "text-success";
            lblModalMessage.Text = $"Selected items {action}ed successfully.";
            ScriptManager.RegisterStartupScript(this, GetType(), "openModalAgain", "$('#detailsModal').modal('show');", true);
        }

        private void ProcessSelectedDetails(string action)
        {
            lblModalMessage.Text = string.Empty;
            int approvalRequestId = int.Parse(hfActionRequestId.Value);
            string manager = GetLoggedInEmployeeCode();
            string bulkRemarks = string.IsNullOrWhiteSpace(txtBulkRemarks.Text) ? null : txtBulkRemarks.Text.Trim();

            var dtIds = BuildSelectedIdsTVP();
            if (dtIds.Rows.Count == 0)
            {
                lblModalMessage.Text = "Please select at least one row.";
                return;
            }
            var dtRemarks = BuildDetailRemarksTVP();

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var infoMessages = new System.Text.StringBuilder();
            int approverRowsUpdated = -1;
            string nextApprover = null;
            var approverQueue = new DataTable();

            try
            {
                using (var con = new SqlConnection(connString))
                using (var cmd = new SqlCommand("usp_ProcessApprovalRequestDetails_WithRemarks", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 120;

                    con.InfoMessage += (s, e) =>
                    {
                        infoMessages.AppendLine(e.Message);
                    };
                    con.FireInfoMessageEventOnUserErrors = true;

                    cmd.Parameters.AddWithValue("@ApprovalRequestId", approvalRequestId);
                    var pIds = cmd.Parameters.AddWithValue("@DetailIds", dtIds);
                    pIds.SqlDbType = SqlDbType.Structured;
                    pIds.TypeName = "dbo.TVP_IntIds";

                    var pRemarks = cmd.Parameters.AddWithValue("@DetailRemarks", dtRemarks);
                    pRemarks.SqlDbType = SqlDbType.Structured;
                    pRemarks.TypeName = "dbo.TVP_DetailRemarks";

                    cmd.Parameters.AddWithValue("@Action", action);
                    cmd.Parameters.AddWithValue("@ChangedBy", manager);
                    cmd.Parameters.AddWithValue("@BulkRemarks", (object)bulkRemarks ?? DBNull.Value);

                    // OUTPUT params
                    var pRowsUpdated = new SqlParameter("@ApproverRowsUpdated", SqlDbType.Int) { Direction = ParameterDirection.Output };
                    cmd.Parameters.Add(pRowsUpdated);

                    var pNextApprover = new SqlParameter("@NextApproverLoginId", SqlDbType.NVarChar, 100) { Direction = ParameterDirection.Output };
                    cmd.Parameters.Add(pNextApprover);

                    con.Open();

                    // Use ExecuteReader to read the returned approver queue resultset (if SP returns it)
                    using (var reader = cmd.ExecuteReader())
                    {
                        approverQueue.Load(reader);
                        // If SP returns additional resultsets, call reader.NextResult() and load as needed
                    }

                    // read outputs (after reader closed)
                    approverRowsUpdated = pRowsUpdated.Value != DBNull.Value ? (int)pRowsUpdated.Value : 0;
                    nextApprover = pNextApprover.Value != DBNull.Value ? (string)pNextApprover.Value : null;

                    sw.Stop();
                }

                // Log/trace results
                System.Diagnostics.Trace.TraceInformation(
                    $"Approval proc succeeded. Request={approvalRequestId}, Action={action}, By={manager}, ApproverRowsUpdated={approverRowsUpdated}, NextApprover={nextApprover}, DurationMs={sw.ElapsedMilliseconds}. Info: {infoMessages}");

                // Sanity check: if approverRowsUpdated=0, show warning
                if (approverRowsUpdated == 0)
                {
                    lblModalMessage.CssClass = "text-warning";
                    lblModalMessage.Text = "Request processed, but no approver row was updated. Please refresh the page and check the approval queue.";
                }
                else
                {
                    lblModalMessage.CssClass = "text-success";
                    lblModalMessage.Text = $"Selected items {action}ed successfully. Next approver: {(nextApprover ?? "none")}";
                }

                // Optionally show the approver queue in a debug panel or log it
                // e.g., store approverQueue in ViewState for admin view, or trace it
                System.Diagnostics.Trace.TraceInformation("Approver queue snapshot:\n" + DataTableToString(approverQueue));

                // refresh UI
                BindDetailsGrid(approvalRequestId);
                ShowDetailsModal(approvalRequestId);
                BindRequestsGrid();

                ScriptManager.RegisterStartupScript(this, GetType(), "openModalAgain", "$('#detailsModal').modal('show');", true);
            }
            catch (SqlException sqlex)
            {
                sw.Stop();
                var err = $"SQL error: {sqlex.Message} (Number={sqlex.Number})";
                System.Diagnostics.Trace.TraceError(err + "\n" + sqlex);
                lblModalMessage.CssClass = "text-danger";
                lblModalMessage.Text = "Error while processing approval. Contact support. Ref: " + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                ScriptManager.RegisterStartupScript(this, GetType(), "openModalOnError", "$('#detailsModal').modal('show');", true);
            }
            catch (Exception ex)
            {
                sw.Stop();
                System.Diagnostics.Trace.TraceError("Error in ProcessSelectedDetails: " + ex);
                lblModalMessage.CssClass = "text-danger";
                lblModalMessage.Text = "Unexpected error. Contact support. Ref: " + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                ScriptManager.RegisterStartupScript(this, GetType(), "openModalOnError2", "$('#detailsModal').modal('show');", true);
            }
        }

        // helper to pretty-print a DataTable for trace (simple)
        private string DataTableToString(DataTable dt)
        {
            if (dt == null) return "(null)";
            var sb = new System.Text.StringBuilder();
            foreach (DataColumn c in dt.Columns) sb.Append(c.ColumnName).Append('\t');
            sb.AppendLine();
            foreach (DataRow r in dt.Rows)
            {
                foreach (var cell in r.ItemArray) sb.Append(cell?.ToString()).Append('\t');
                sb.AppendLine();
            }
            return sb.ToString();
        }


        // Gather selected detail ids from grid
        private DataTable BuildSelectedDetailsTVP()
        {
            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(int));

            foreach (GridViewRow row in gvDetails.Rows)
            {
                var chk = row.FindControl("chkSelect") as CheckBox;
                if (chk != null && chk.Checked)
                {
                    // DataKey is ApprovalRequestDetailId
                    var id = Convert.ToInt32(gvDetails.DataKeys[row.RowIndex].Value);
                    dt.Rows.Add(id);
                }
            }
            return dt;
        }

        protected void btnApproveSelected_Click_OLD1(object sender, EventArgs e)
        {
            var tvp = BuildSelectedDetailsTVP();
            if (tvp.Rows.Count == 0)
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "alert", "alert('Select at least one item to approve.');", true);
                return;
            }

            int approvalRequestId = int.Parse(hfActionRequestId.Value); // set when modal opened
            string manager = User.Identity.Name ?? "MANAGER";
            string remarks = txtBulkRemarks.Text.Trim();

            // Optionally pass ChangedOn in IST (if you want DB to store IST). Here we call SP which uses SYSUTCDATETIME()
            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand("usp_ProcessApprovalRequestDetails", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                // TVP param
                var p = cmd.Parameters.AddWithValue("@DetailIds", tvp);
                p.SqlDbType = SqlDbType.Structured;
                p.TypeName = "dbo.TVP_IntIds";

                cmd.Parameters.AddWithValue("@ApprovalRequestId", approvalRequestId);
                cmd.Parameters.AddWithValue("@Action", "Approve");
                cmd.Parameters.AddWithValue("@ChangedBy", manager);
                cmd.Parameters.AddWithValue("@Remarks", string.IsNullOrEmpty(remarks) ? (object)DBNull.Value : remarks);

                con.Open();
                cmd.ExecuteNonQuery();
            }

            // reload details and header grid
            //ShowDetailsModal(approvalRequestId);
            BindRequestsGrid();
        }

        protected void btnApproveSelected_Click_OLD2(object sender, EventArgs e)
        {
            ProcessSelectedDetails("Approve");
        }

        protected void btnRejectSelected_Click_OLD1(object sender, EventArgs e)
        {
            var tvp = BuildSelectedDetailsTVP();
            if (tvp.Rows.Count == 0)
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "alert", "alert('Select at least one item to reject.');", true);
                return;
            }

            int approvalRequestId = int.Parse(hfActionRequestId.Value);
            string manager = User.Identity.Name ?? "MANAGER";
            string remarks = txtBulkRemarks.Text.Trim();
            if (string.IsNullOrEmpty(remarks))
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "alert", "alert('Please add remarks for rejection.');", true);
                return;
            }

            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand("usp_ProcessApprovalRequestDetails", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                var p = cmd.Parameters.AddWithValue("@DetailIds", tvp);
                p.SqlDbType = SqlDbType.Structured;
                p.TypeName = "dbo.TVP_IntIds";

                cmd.Parameters.AddWithValue("@ApprovalRequestId", approvalRequestId);
                cmd.Parameters.AddWithValue("@Action", "Reject");
                cmd.Parameters.AddWithValue("@ChangedBy", manager);
                cmd.Parameters.AddWithValue("@Remarks", remarks);

                con.Open();
                cmd.ExecuteNonQuery();
            }

            //ShowDetailsModal(approvalRequestId);
            BindRequestsGrid();
        }

        protected void btnRejectSelected_Click_OLD2(object sender, EventArgs e)
        {
            // ensure either per-row remarks exist or bulk remarks provided
            var tvpRemarks = BuildDetailRemarksTVP();
            var tvpIds = BuildSelectedIdsTVP();
            if (tvpIds.Rows.Count == 0)
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "alert", "alert('Select at least one item to process.');", true);
                return;
            }
            // If there are no per-row remarks AND bulk remarks empty, require bulk remarks for Reject
            if (tvpRemarks.Rows.Count == 0 && string.IsNullOrWhiteSpace(txtBulkRemarks.Text))
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "alert", "alert('Please provide per-row remarks or a bulk remark for rejection.');", true);
                return;
            }

            ProcessSelectedDetails("Reject");
        }

        private void ProcessSelectedDetails_OLD1(string action)
        {
            int approvalRequestId = int.Parse(hfActionRequestId.Value);
            string manager = User.Identity.Name ?? "MANAGER";
            string bulkRemarks = string.IsNullOrWhiteSpace(txtBulkRemarks.Text) ? null : txtBulkRemarks.Text.Trim();

            var dtIds = BuildSelectedIdsTVP();
            var dtRemarks = BuildDetailRemarksTVP();

            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand("usp_ProcessApprovalRequestDetails_WithRemarks", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                var pIds = cmd.Parameters.AddWithValue("@DetailIds", dtIds);
                pIds.SqlDbType = SqlDbType.Structured;
                pIds.TypeName = "dbo.TVP_IntIds";

                var pRemarks = cmd.Parameters.AddWithValue("@DetailRemarks", dtRemarks);
                pRemarks.SqlDbType = SqlDbType.Structured;
                pRemarks.TypeName = "dbo.TVP_DetailRemarks";

                cmd.Parameters.AddWithValue("@ApprovalRequestId", approvalRequestId);
                cmd.Parameters.AddWithValue("@Action", action);
                cmd.Parameters.AddWithValue("@ChangedBy", manager);
                cmd.Parameters.AddWithValue("@BulkRemarks", (object)bulkRemarks ?? DBNull.Value);

                con.Open();
                cmd.CommandTimeout = 120;
                cmd.ExecuteNonQuery();
            }

            // refresh UI
            //ShowDetailsModal(approvalRequestId);
            BindRequestsGrid();
        }

        protected void gvDetails_RowDataBound_OLD1(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var dataItem = e.Row.DataItem as DataRowView;
            if (dataItem == null) return;

            string detailStatus = Convert.ToString(dataItem["DetailStatus"]);
            var txtRowRemark = e.Row.FindControl("txtRowRemark") as TextBox;
            var chk = e.Row.FindControl("chkSelect") as CheckBox;

            if (detailStatus == "Approved")
            {
                // disable remark edit and checkbox if already approved
                if (txtRowRemark != null) txtRowRemark.Enabled = false;
                if (chk != null) chk.Enabled = false;
            }
        }

        private DataTable BuildSelectedIdsTVP_OLD1()
        {
            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(int));

            foreach (GridViewRow row in gvDetails.Rows)
            {
                var chk = row.FindControl("chkSelect") as CheckBox;
                if (chk != null && chk.Checked)
                {
                    var detailId = Convert.ToInt32(gvDetails.DataKeys[row.RowIndex].Value);
                    dt.Rows.Add(detailId);
                }
            }
            return dt;
        }

        private DataTable BuildDetailRemarksTVP_OLD1()
        {
            var dt = new DataTable();
            dt.Columns.Add("ApprovalRequestDetailId", typeof(int));
            dt.Columns.Add("Remark", typeof(string));

            foreach (GridViewRow row in gvDetails.Rows)
            {
                var detailId = Convert.ToInt32(gvDetails.DataKeys[row.RowIndex].Value);
                var txt = row.FindControl("txtRowRemark") as TextBox;
                if (txt != null)
                {
                    string remark = txt.Text?.Trim();
                    if (!string.IsNullOrEmpty(remark))
                    {
                        dt.Rows.Add(detailId, remark);
                    }
                }
            }
            return dt;
        }

        private DateTime NowIndia()
        {
            var utcNow = DateTime.UtcNow;
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);
            }
            catch
            {
                return utcNow.AddHours(5).AddMinutes(30);
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
            int approvalRequestId = Convert.ToInt32(lblModalRequestId.Text);
            string auditor = GetLoggedInEmployeeCode();

            using (SqlConnection con = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_MarkRequestReadyForPayment", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ApprovalRequestId", approvalRequestId);
                cmd.Parameters.AddWithValue("@ActionBy", auditor);
                con.Open();
                cmd.ExecuteNonQuery();
            }

            lblHeaderStatus.Text = "ReadyForPayment";
            lblModalMessage.Text = "Request marked Ready for Payment.";
            lblModalMessage.CssClass = "text-success";
        }

    }
}