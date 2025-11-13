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
            BindRequestsGrid();
        }

        // ------------- BIND HEADER GRID ----------------
        protected void BindRequestsGrid()
        {
            string sql = @"SELECT A.ApprovalRequestId, A.RequesterLoginId, UM.EmployeeName, A.RequestDate, A.YearNo, A.MonthNo, DATENAME(MONTH, DATEFROMPARTS(A.YearNo, A.MonthNo, 1)) AS MonthName, A.Status, A.TotalKM, A.TotalClaimAmount, A.TotalDAAmount, A.TotalHotelAmount, A.TotalTravelAmount, A.TotalExpenseAmount, A.RequestRemarks, A.CreatedOn, A.CreatedBy, A.ModifiedOn, A.ModifiedBy FROM dbo.SalesApprovalRequest A INNER JOIN [AnmolDristi].[dbo].[MST_UserMaster] UM ON A.RequesterLoginId = UM.EmployeeCode WHERE A.Status = 'Pending' ORDER BY A.RequestDate DESC;";

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

        protected void gvDetails_RowDataBound(object sender, GridViewRowEventArgs e)
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

        /// <summary>
        /// Sets styling, enables/disables buttons, and attaches Expenses JSON to the row's button.
        /// </summary>
        private void PopulateRowControls(GridViewRow row, DataRowView drv)
        {
            if (row == null || drv == null) return;

            string detailStatus = drv["DetailStatus"] == DBNull.Value ? String.Empty : drv["DetailStatus"].ToString();
            string claimStatus = drv["ClaimStatus"] == DBNull.Value ? String.Empty : drv["ClaimStatus"].ToString();

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
        private void FillFooterTotals(GridViewRow footer)
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

        // RowCommand for per-row approve/reject
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
                // inside gvDetails_RowCommand when processing ApproveRow / RejectRow
                string newStatus = (e.CommandName == "ApproveRow") ? "Approved" : "Rejected";
                int detailId = 0;
                if (!int.TryParse(Convert.ToString(e.CommandArgument), out detailId)) return;

                // get current request id safely
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

            // Identify the row and get the ID
            var source = (Control)e.CommandSource;
            var row = source.NamingContainer as GridViewRow;
            if (row == null) return;

            int detailId = 0;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out detailId))
            {
                // fallback via DataKeys
                if (gvDetails.DataKeys != null && row.RowIndex >= 0 && row.RowIndex < gvDetails.DataKeys.Count)
                {
                    int.TryParse(Convert.ToString(gvDetails.DataKeys[row.RowIndex].Value), out detailId);
                }
            }
            if (detailId <= 0) return;

            // Get manager remark from the row textbox
            var txtRowRemark = row.FindControl("txtRowRemark") as TextBox;
            string mgrRemark = txtRowRemark != null ? txtRowRemark.Text.Trim() : string.Empty;

            // Reject requires remark
            if (e.CommandName == "RejectRow" && string.IsNullOrWhiteSpace(mgrRemark))
            {
                lblModalMessage.CssClass = "text-danger";
                lblModalMessage.Text = "Manager Remark is required to reject.";
                upDetails.Update();
                return;
            }

            string newStatus = (e.CommandName == "ApproveRow") ? "Approved" : "Rejected";

            int requestId = 0;
            int.TryParse(lblModalRequestId.Text, out requestId);

            string processedBy = GetLoggedInEmployeeCode();

            // Get current IST time
            DateTime processedOn;
            try
            {
                processedOn = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "India Standard Time");
            }
            catch
            {
                processedOn = DateTime.Now;
            }

            try
            {
                using (var con = new SqlConnection(connString))
                using (var cmd = new SqlCommand("dbo.usp_UpdateApprovalRequestDetailStatus_AndMaybeActivity", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ApprovalRequestDetailId", detailId);
                    cmd.Parameters.AddWithValue("@NewStatus", newStatus);
                    cmd.Parameters.AddWithValue("@ManagerRemark", string.IsNullOrEmpty(mgrRemark) ? (object)DBNull.Value : mgrRemark);
                    cmd.Parameters.AddWithValue("@ProcessedBy", processedBy);
                    cmd.Parameters.AddWithValue("@ProcessedOn", processedOn);
                    cmd.Parameters.AddWithValue("@ForceSetActivityClaim", 1); // 🔸 Always set to 1 as required

                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                // Rebind and keep modal open
                BindDetailsGrid(requestId);
                upDetails.Update();
                ScriptManager.RegisterStartupScript(this, GetType(), "openModal", "openDetailsModal();", true);

                lblModalMessage.CssClass = "text-success";
                lblModalMessage.Text = (newStatus == "Approved") ? "Detail approved." : "Detail rejected.";
                upDetails.Update();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error updating detail status: " + ex);
                lblModalMessage.CssClass = "text-danger";
                lblModalMessage.Text = "Error updating status. See server logs.";
                upDetails.Update();
            }
        }


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




        // paging for header grid
        protected void gvRequests_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvRequests.PageIndex = e.NewPageIndex;
            BindRequestsGrid();
        }

        // ------------- HANDLE ACTIONS FROM HEADER GRID --------------
        protected void gvRequests_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "View")
            {
                int approvalId = Convert.ToInt32(e.CommandArgument);
                int approvalRequestId;
                if (!int.TryParse(e.CommandArgument?.ToString(), out approvalRequestId)) return;
                // 1) set labels and hidden field
                lblModalRequestId.Text = approvalRequestId.ToString();
                hfActionRequestId.Value = approvalRequestId.ToString();

                // 2) bind grid (this should fill gvDetails)
                BindDetailsGrid(approvalRequestId);
                // 3) update the UpdatePanel that holds the grid
                upDetails.Update(); // ensure upDetails is accessible (ID matches)

                // 4) log grid client id to console for debug (optional)
                ScriptManager.RegisterStartupScript(this, GetType(), "logGV",
                    $"console.log('After bind - gvDetails client id = {gvDetails.ClientID}');", true);

                // 5) open modal (call helper openDetailsModal which uses $('#detailsModal').modal('show'))
                ScriptManager.RegisterStartupScript(this, GetType(), "openModal", "openDetailsModal();", true);

                ShowDetailsModal(approvalRequestId);
                // Put this after DataBind() of gvDetails or just for debugging in Page_Load (when !IsPostBack you can skip)
                //ScriptManager.RegisterStartupScript(this, GetType(), "logGVId", $"console.log('gvDetails client id = {gvDetails.ClientID}');", true);

            }
            else if (e.CommandName == "ApproveQuick")
            {
                int approvalId = Convert.ToInt32(e.CommandArgument);
                // quick approve header-level (optional) or open modal first - implement as per your flow
                // Example: open modal for safety
                ShowDetailsModal(approvalId);
                // Or call a stored proc to approve all details directly (not included here)
            }
        }

        // ------------- BIND DETAILS GRID (JOIN) and OPEN MODAL --------------
        protected void ShowDetailsModal(int approvalRequestId)
        {
            // store id in hidden field for later actions
            hfActionRequestId.Value = approvalRequestId.ToString();
            lblModalRequestId.Text = approvalRequestId.ToString();

            BindDetailsGrid(approvalRequestId);

            // fetch header totals & status (optional display)
            string headerSql = "SELECT TotalExpenseAmount, Status FROM dbo.SalesApprovalRequest WHERE ApprovalRequestId = @ApprovalRequestId";
            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand(headerSql, con))
            {
                cmd.Parameters.AddWithValue("@ApprovalRequestId", approvalRequestId);
                con.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        lblHeaderTotalClaim.Text = rdr["TotalExpenseAmount"] == DBNull.Value ? "0.00" : Convert.ToDecimal(rdr["TotalExpenseAmount"]).ToString("N2");
                        lblHeaderStatus.Text = rdr["Status"]?.ToString() ?? "Pending";
                    }
                }
            }

            // open modal (partial postback safe)
            ScriptManager.RegisterStartupScript(this, GetType(), "openModal", "$('#detailsModal').modal('show');", true);
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

        protected void BindDetailsGrid(int approvalRequestId)
        {
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
        private DataTable BuildSelectedIdsTVP()
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

        private void ProcessSelectedDetails(string action)
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
    }
}