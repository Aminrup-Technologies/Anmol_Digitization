using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.IO;
using System.Globalization;
using Newtonsoft.Json;
using System.Text;
using Newtonsoft.Json.Linq;

namespace AnmolDristi
{
    public partial class SalesMonthlyReview : BasePage
    {
        private string connString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        private const string SESSION_KEY_GV_ACTIVITIES = "gvActivities_DataTable";

        // --- FIX FOR BROKEN VISUAL STUDIO DESIGNER FILE ---
        // Explicitly declaring these so C# knows they exist even if the designer file failed to update.
        protected global::System.Web.UI.WebControls.HiddenField HF_SnapPolicyMode;
        protected global::System.Web.UI.WebControls.HiddenField HF_SnapResolvedDA;
        protected global::System.Web.UI.WebControls.HiddenField HF_SnapHotelCap;
        protected global::System.Web.UI.WebControls.HiddenField HF_SnapOutstationDA;
        protected global::System.Web.UI.WebControls.HiddenField HF_SnapOwnArrangementDA;
        // --------------------------------------------------

        protected void Page_Load(object sender, EventArgs e)
        {
            Summary.Visible = true;

            if (!IsPostBack)
            {
                if (Session["USERID"] != null && Session["USERNAME"] != null)
                {
                    PopulateYearMonth();
                    lbl_msg.Text = "";
                }
                else
                {
                    Response.Redirect("~/Login.aspx", false);
                    Context.ApplicationInstance.CompleteRequest();
                }
            }
        }

        private string GetCurrentSessionKey()
        {
            return string.Format("{0}_{1}_{2}", SESSION_KEY_GV_ACTIVITIES, DDL_Year.SelectedValue, DDL_Month.SelectedValue);
        }

        private void PopulateYearMonth()
        {
            int currentYear = DateTime.UtcNow.AddMinutes(330).Year;
            DDL_Year.Items.Clear();
            for (int y = currentYear - 2; y <= currentYear + 1; y++)
                DDL_Year.Items.Add(new ListItem(y.ToString(), y.ToString()));
            DDL_Year.SelectedValue = currentYear.ToString();

            DDL_Month.Items.Clear();
            for (int m = 1; m <= 12; m++)
                DDL_Month.Items.Add(new ListItem(new DateTime(2000, m, 1).ToString("MMMM"), m.ToString()));
            DDL_Month.SelectedValue = DateTime.UtcNow.AddMinutes(330).Month.ToString();
        }

        protected void btn_fetch_Click(object sender, EventArgs e)
        {
            Session.Remove(GetCurrentSessionKey());
            BindGrid(true);
        }

        private void BindGrid(Boolean policybinder, bool showNotification = true)
        {
            lbl_msg.Text = "";
            int year = int.Parse(DDL_Year.SelectedValue);
            int month = int.Parse(DDL_Month.SelectedValue);

            bool isPolicyEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings["EnablePreviousMonthPolicy"]);
            bool allowExecution = false;

            if (isPolicyEnabled)
            {
                DateTime currentDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                int currentYear = currentDate.Year;
                int currentMonth = currentDate.Month;

                bool isAllowedWindow = currentDate.Day >= 1 && currentDate.Day <= 15;
                bool isPreviousMonthSelected = (year == currentYear && month == currentMonth - 1) || (currentMonth == 1 && month == 12 && year == currentYear - 1);

                if (isAllowedWindow && isPreviousMonthSelected)
                    allowExecution = true;
                else
                {
                    lbl_msg.Text = "You can process previous month data only between 1st and 15th of the current month.";
                    lbl_msg.ForeColor = System.Drawing.Color.Red;
                    ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyPolicyBlock", "showPNotify('Policy Restriction', 'Processing is allowed only between 1st and 15th for the previous month.', 'error');", true);
                }
            }
            else
            {
                allowExecution = true;
                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyBypass", "showPNotify('Policy Disabled', 'Monthly processing policy is disabled. Validation skipped.', 'info');", true);
            }

            if (!allowExecution) return;

            try
            {
                if (policybinder == true)
                {
                    string employeeCode = GetLoggedInEmployeeCode();
                    int? regionId = GetLoggedInRegionId();
                    int? branchId = GetLoggedInBranchId();
                    int? gradeId = GetLoggedInGradeId();
                    int? designationId = GetLoggedInDesignationId();
                    var policy = GetResolvedPolicy(employeeCode, regionId, branchId, gradeId, designationId);

                    if (policy != null)
                        BindPolicyToModal(policy);
                    else
                        RegisterClientNotification("No resolved policy found for the selected context.", "warning");
                }

                string sessionKey = GetCurrentSessionKey();
                DataTable dt = Session[sessionKey] as DataTable;
                if (dt == null)
                {
                    dt = new DataTable();
                    using (SqlConnection con = new SqlConnection(connString))
                    using (SqlCommand cmd = new SqlCommand("dbo.usp_GetSalesDailyActivity_Month", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@LoginId", GetCurrentUserLoginId());
                        cmd.Parameters.AddWithValue("@Year", year);
                        cmd.Parameters.AddWithValue("@Month", month);
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            da.Fill(dt);
                    }

                    if (!dt.Columns.Contains("CalculatedClaimAmount")) dt.Columns.Add("CalculatedClaimAmount", typeof(decimal));
                    if (!dt.Columns.Contains("RatePerKm")) dt.Columns.Add("RatePerKm", typeof(decimal));
                    if (!dt.Columns.Contains("StationaryAmount")) dt.Columns.Add("StationaryAmount", typeof(decimal));

                    decimal defaultRate = 3.75m;
                    var cfg = ConfigurationManager.AppSettings["DefaultRatePerKm"];
                    decimal tmpCfg;
                    if (!string.IsNullOrWhiteSpace(cfg) && decimal.TryParse(cfg, out tmpCfg))
                        defaultRate = tmpCfg;

                    foreach (DataRow row in dt.Rows)
                    {
                        decimal totalKm = 0m;
                        if (row.Table.Columns.Contains("TotalDistanceKM") && row["TotalDistanceKM"] != DBNull.Value)
                            decimal.TryParse(row["TotalDistanceKM"].ToString(), out totalKm);

                        decimal rate = GetRatePerKm(row, defaultRate);
                        decimal claim = Math.Round(rate * totalKm, 2, MidpointRounding.AwayFromZero);
                        row["CalculatedClaimAmount"] = claim;

                        if (row.Table.Columns.Contains("RatePerKm"))
                        {
                            if (row["RatePerKm"] == DBNull.Value || string.IsNullOrWhiteSpace(row["RatePerKm"].ToString()))
                                row["RatePerKm"] = rate;
                        }
                    }

                    if (dt.Columns.Contains("ActivityId"))
                    {
                        try { dt.PrimaryKey = new DataColumn[] { dt.Columns["ActivityId"] }; }
                        catch { /* ignore */ }
                    }
                    Session[sessionKey] = dt;
                }

                if (!dt.Columns.Contains("IsEditable"))
                    dt.Columns.Add("IsEditable", typeof(bool));

                bool monthLocked = false;
                bool allowSubmission = false;
                int? approvalRequestId = null;
                string approvalStatus = null;
                string loginId = GetCurrentUserLoginId();

                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 ApprovalRequestId, Status FROM dbo.SalesApprovalRequest WHERE RequesterLoginId = @LoginId AND YearNo = @Year AND MonthNo = @Month ORDER BY RequestDate DESC", con))
                    {
                        cmd.Parameters.AddWithValue("@LoginId", loginId);
                        cmd.Parameters.AddWithValue("@Year", year);
                        cmd.Parameters.AddWithValue("@Month", month);
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                approvalRequestId = rdr["ApprovalRequestId"] != DBNull.Value ? (int?)Convert.ToInt32(rdr["ApprovalRequestId"]) : null;
                                approvalStatus = rdr["Status"] != DBNull.Value ? rdr["Status"].ToString() : null;
                            }
                        }
                    }

                    if (!approvalRequestId.HasValue)
                    {
                        monthLocked = false;
                        allowSubmission = true;
                        foreach (DataRow row in dt.Rows) row["IsEditable"] = true;
                    }
                    else
                    {
                        string s = (approvalStatus ?? "").Trim();

                        // ⬅️ 1. Add "Returned" to the overall status check
                        if (string.Equals(s, "Draft", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(s, "Rejected", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(s, "Returned", StringComparison.OrdinalIgnoreCase))
                        {
                            monthLocked = false;
                            allowSubmission = true;
                            foreach (DataRow row in dt.Rows) row["IsEditable"] = true;
                        }
                        else
                        {
                            monthLocked = true;
                            allowSubmission = false;

                            var editableActivityIds = new HashSet<int>();
                            var detailStatusByActivity = new Dictionary<int, string>();
                            using (SqlCommand cmd2 = new SqlCommand("SELECT ActivityId, DetailStatus FROM dbo.SalesApprovalRequestDetail WHERE ApprovalRequestId = @Id", con))
                            {
                                cmd2.Parameters.AddWithValue("@Id", approvalRequestId.Value);
                                using (SqlDataReader rdr = cmd2.ExecuteReader())
                                {
                                    while (rdr.Read())
                                    {
                                        int aid = rdr["ActivityId"] != DBNull.Value ? Convert.ToInt32(rdr["ActivityId"]) : 0;
                                        string detail = rdr["DetailStatus"] != DBNull.Value ? rdr["DetailStatus"].ToString() : null;

                                        if (aid > 0 && !string.IsNullOrWhiteSpace(detail))
                                            detailStatusByActivity[aid] = detail;

                                        // ⬅️ 2. Add "Returned" to the Line Item check
                                        if (string.Equals(detail, "Rejected", StringComparison.OrdinalIgnoreCase) ||
                                            string.Equals(detail, "Returned", StringComparison.OrdinalIgnoreCase))
                                        {
                                            editableActivityIds.Add(aid);
                                        }
                                    }
                                }
                            }

                            if (!dt.Columns.Contains("DetailStatus"))
                                dt.Columns.Add("DetailStatus", typeof(string));

                            foreach (DataRow row in dt.Rows)
                            {
                                int actId = row["ActivityId"] != DBNull.Value ? Convert.ToInt32(row["ActivityId"]) : 0;
                                row["IsEditable"] = editableActivityIds.Contains(actId);
                                row["DetailStatus"] = detailStatusByActivity.ContainsKey(actId)
                                    ? detailStatusByActivity[actId]
                                    : (object)DBNull.Value;
                            }

                            // ⬅️ 3. NEW: If there are returned/rejected rows, UNLOCK the Submit button!
                            if (editableActivityIds.Count > 0)
                            {
                                allowSubmission = true;
                            }
                        }
                    }
                    //else
                    //{
                    //    string s = (approvalStatus ?? "").Trim();
                    //    if (string.Equals(s, "Draft", StringComparison.OrdinalIgnoreCase) || string.Equals(s, "Rejected", StringComparison.OrdinalIgnoreCase))
                    //    {
                    //        monthLocked = false;
                    //        allowSubmission = true;
                    //        foreach (DataRow row in dt.Rows) row["IsEditable"] = true;
                    //    }
                    //    else
                    //    {
                    //        monthLocked = true;
                    //        allowSubmission = false;

                    //        var editableActivityIds = new HashSet<int>();
                    //        using (SqlCommand cmd2 = new SqlCommand("SELECT ActivityId, DetailStatus FROM dbo.SalesApprovalRequestDetail WHERE ApprovalRequestId = @Id", con))
                    //        {
                    //            cmd2.Parameters.AddWithValue("@Id", approvalRequestId.Value);
                    //            using (SqlDataReader rdr = cmd2.ExecuteReader())
                    //            {
                    //                while (rdr.Read())
                    //                {
                    //                    int aid = rdr["ActivityId"] != DBNull.Value ? Convert.ToInt32(rdr["ActivityId"]) : 0;
                    //                    string detail = rdr["DetailStatus"] != DBNull.Value ? rdr["DetailStatus"].ToString() : null;
                    //                    if (string.Equals(detail, "Rejected", StringComparison.OrdinalIgnoreCase))
                    //                        editableActivityIds.Add(aid);
                    //                }
                    //            }
                    //        }
                    //        foreach (DataRow row in dt.Rows)
                    //        {
                    //            int actId = row["ActivityId"] != DBNull.Value ? Convert.ToInt32(row["ActivityId"]) : 0;
                    //            row["IsEditable"] = editableActivityIds.Contains(actId);
                    //        }
                    //    }
                    //}
                }
                ViewState["MonthLocked"] = monthLocked;
                ViewState["AllowSubmission"] = allowSubmission;

                PopulateStatusSummary(dt);
                Summary.Visible = true;
                Activities.Visible = true;
                gvActivities.DataSource = dt;
                gvActivities.DataBind();

                gvActivities.UseAccessibleHeader = true;
                if (gvActivities.HeaderRow != null)
                    gvActivities.HeaderRow.TableSection = TableRowSection.TableHeader;

                decimal grandCalc = 0m, grandDA = 0m, grandHotel = 0m, grandTravel = 0m, grandStationary = 0m, grandExpense = 0m, grandKM = 0m;

                if (dt.Rows.Count > 0)
                {
                    object o;
                    o = dt.Compute("SUM(CalculatedClaimAmount)", "");
                    grandCalc = o != DBNull.Value ? Convert.ToDecimal(o) : 0m;

                    if (dt.Columns.Contains("DAAmount")) { o = dt.Compute("SUM(DAAmount)", ""); grandDA = o != DBNull.Value ? Convert.ToDecimal(o) : 0m; }
                    if (dt.Columns.Contains("HotelAmount")) { o = dt.Compute("SUM(HotelAmount)", ""); grandHotel = o != DBNull.Value ? Convert.ToDecimal(o) : 0m; }
                    if (dt.Columns.Contains("TravelAmount")) { o = dt.Compute("SUM(TravelAmount)", ""); grandTravel = o != DBNull.Value ? Convert.ToDecimal(o) : 0m; }
                    if (dt.Columns.Contains("StationaryAmount")) { o = dt.Compute("SUM(StationaryAmount)", ""); grandStationary = o != DBNull.Value ? Convert.ToDecimal(o) : 0m; }
                    if (dt.Columns.Contains("ExpenseTotal")) { o = dt.Compute("SUM(ExpenseTotal)", ""); grandExpense = o != DBNull.Value ? Convert.ToDecimal(o) : 0m; }
                    if (dt.Columns.Contains("TotalDistanceKM")) { o = dt.Compute("SUM(TotalDistanceKM)", ""); grandKM = o != DBNull.Value ? Convert.ToDecimal(o) : 0m; }
                }

                if (gvActivities.FooterRow != null)
                {
                    Label fCalc = gvActivities.FooterRow.FindControl("lblFooterCalculated") as Label;
                    Label fDA = gvActivities.FooterRow.FindControl("lblFooterDA") as Label;
                    Label fHotel = gvActivities.FooterRow.FindControl("lblFooterHotel") as Label;
                    Label fTravel = gvActivities.FooterRow.FindControl("lblFooterTravel") as Label;
                    Label fStationary = gvActivities.FooterRow.FindControl("lblFooterStationary") as Label;
                    Label fExpense = gvActivities.FooterRow.FindControl("lblFooterExpenseTotal") as Label;
                    Label lblFooterTotalKM = gvActivities.FooterRow.FindControl("lblFooterTotalKM") as Label;

                    if (fCalc != null) fCalc.Text = "₹ " + grandCalc.ToString("N2");
                    if (fDA != null) fDA.Text = "₹ " + grandDA.ToString("N2");
                    if (fHotel != null) fHotel.Text = "₹ " + grandHotel.ToString("N2");
                    if (fTravel != null) fTravel.Text = "₹ " + grandTravel.ToString("N2");
                    if (fStationary != null) fStationary.Text = "₹ " + grandStationary.ToString("N2");
                    if (fExpense != null) fExpense.Text = "₹ " + grandExpense.ToString("N2");
                    if (lblFooterTotalKM != null) lblFooterTotalKM.Text = string.Format("{0:N2} km", grandKM);

                    // Grand Total Calculation - Now including Stationary
                    decimal grandOverall = grandCalc + grandDA + grandHotel + grandTravel + grandStationary;

                    // FIX: Changed from -5 to -6 to account for the new Stationary column
                    int lastCellIndex = Math.Max(0, gvActivities.FooterRow.Cells.Count - 6);

                    if (lastCellIndex < gvActivities.FooterRow.Cells.Count)
                    {
                        gvActivities.FooterRow.Cells[lastCellIndex].Text = "₹ " + grandOverall.ToString("N2");
                        gvActivities.FooterRow.Cells[lastCellIndex].HorizontalAlign = HorizontalAlign.Center;
                        gvActivities.FooterRow.Cells[lastCellIndex].Font.Bold = true;
                        gvActivities.FooterRow.Cells[lastCellIndex].ForeColor = System.Drawing.Color.DarkBlue;
                        gvActivities.FooterRow.Cells[lastCellIndex].ToolTip = "Grand Total of Claim + DA + Hotel + Travel + Stationary";
                    }
                }

                // ⬅️ 4. Ensure the panel is visible if submissions are allowed
                Actionable.Visible = gvActivities.Rows.Count > 0 && (!monthLocked || allowSubmission);
                btn_save_draft.Enabled = !monthLocked || allowSubmission;
                btn_submit.Enabled = allowSubmission;

                if (monthLocked && !allowSubmission)
                {
                    lbl_msg.Text = "Monthly claims for this period have been submitted for approval. Editing is disabled except for returned days awaiting your correction.";
                    lbl_msg.ForeColor = System.Drawing.Color.Orange;
                    ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyLocked", "showPNotify('Submitted for Approval', 'Monthly claims are submitted. Editing disabled except rejected rows.', 'info');", true);
                }
                else if (monthLocked && allowSubmission)
                {
                    // ⬅️ 5. NEW: Bold warning telling the SO to fix and Submit
                    lbl_msg.Text = "⚠️ Action Required: You have Returned/Rejected claims. Please edit the highlighted rows and click 'Submit' below to re-send them for approval.";
                    lbl_msg.ForeColor = System.Drawing.Color.Red;
                    ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyAction", "showPNotify('Action Required', 'Please fix your Returned claims and hit Submit.', 'error');", true);
                }
                else
                {
                    if (showNotification)
                    {
                        lbl_msg.Text = "Action executed successfully.";
                        lbl_msg.ForeColor = System.Drawing.Color.Green;
                        ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySuccess", "showPNotify('Success', 'Process executed successfully.', 'success');", true);
                    }
                }

                //Actionable.Visible = gvActivities.Rows.Count > 0 && !monthLocked;
                //btn_save_draft.Enabled = !monthLocked;
                //btn_submit.Enabled = allowSubmission;

                //if (monthLocked)
                //{
                //    lbl_msg.Text = "Monthly claims for this period have been submitted for approval. Editing is disabled except for rows that were rejected by approver.";
                //    lbl_msg.ForeColor = System.Drawing.Color.Orange;
                //    ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyLocked", "showPNotify('Submitted for Approval', 'Monthly claims are submitted. Editing disabled except rejected rows.', 'info');", true);
                //}
                //else
                //{
                //    if (showNotification)
                //    {
                //        lbl_msg.Text = "Action executed successfully.";
                //        lbl_msg.ForeColor = System.Drawing.Color.Green;
                //        ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySuccess", "showPNotify('Success', 'Process executed successfully.', 'success');", true);
                //    }
                //}
            }
            catch (Exception ex)
            {
                lbl_msg.Text = "An error occurred: " + ex.Message;
                lbl_msg.ForeColor = System.Drawing.Color.Red;
                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyError", "showPNotify('Error', '" + ex.Message.Replace("'", "\\'") + "', 'error');", true);
            }
        }

        private void ApplyExpenseModalEditability(string activityId)
        {
            if (string.IsNullOrWhiteSpace(activityId)) return;

            int actId;
            if (!int.TryParse(activityId, out actId) || actId <= 0) return;

            string claimStatus = "Draft";
            string detailStatus = "Draft";

            using (SqlConnection conStat = new SqlConnection(connString))
            {
                conStat.Open();

                using (SqlCommand cmdStat = new SqlCommand(
                    "SELECT ClaimStatus FROM dbo.SalesDailyActivity WITH (NOLOCK) WHERE ActivityId = @ActId", conStat))
                {
                    cmdStat.Parameters.AddWithValue("@ActId", actId);
                    object statObj = cmdStat.ExecuteScalar();
                    if (statObj != null && statObj != DBNull.Value)
                        claimStatus = statObj.ToString().Trim();
                }

                using (SqlCommand cmdDetail = new SqlCommand(
                    @"SELECT TOP 1 ISNULL(DetailStatus, 'Draft')
                      FROM dbo.SalesApprovalRequestDetail WITH (NOLOCK)
                      WHERE ActivityId = @ActId
                      ORDER BY ApprovalRequestDetailId DESC", conStat))
                {
                    cmdDetail.Parameters.AddWithValue("@ActId", actId);
                    object detailObj = cmdDetail.ExecuteScalar();
                    if (detailObj != null && detailObj != DBNull.Value)
                        detailStatus = detailObj.ToString().Trim();
                }
            }

            bool isReturnedDay =
                string.Equals(detailStatus, "Returned", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(claimStatus, "Returned", StringComparison.OrdinalIgnoreCase);

            bool isRejectedDay =
                string.Equals(detailStatus, "Rejected", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(claimStatus, "Rejected", StringComparison.OrdinalIgnoreCase);

            bool isLockedDay = isRejectedDay ||
                claimStatus.Equals("Submitted", StringComparison.OrdinalIgnoreCase) ||
                claimStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
                claimStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase);

            if (!isReturnedDay && isLockedDay)
            {
                if (BTN_SaveExpense != null) BTN_SaveExpense.Visible = false;
                if (DDL_ExpenseType != null) DDL_ExpenseType.Enabled = false;
                if (btnOpenEditKmFromExpense != null) btnOpenEditKmFromExpense.Visible = false;

                if (rptExpenses != null)
                {
                    foreach (RepeaterItem item in rptExpenses.Items)
                    {
                        if (item.ItemType != ListItemType.Item && item.ItemType != ListItemType.AlternatingItem)
                            continue;

                        LinkButton lnkDelete = item.FindControl("lnkDeleteExpense") as LinkButton;
                        LinkButton lnkDisabled = item.FindControl("lnkDeleteExpenseDisabled") as LinkButton;
                        if (lnkDelete != null) lnkDelete.Visible = false;
                        if (lnkDisabled != null) lnkDisabled.Visible = true;
                    }
                }

                if (LBL_ExpenseMsg != null)
                {
                    LBL_ExpenseMsg.Text = isRejectedDay
                        ? "This day was rejected by the auditor. Expenses cannot be edited."
                        : "This day is " + claimStatus + ". You are in view-only mode.";
                    LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                }
            }
            else
            {
                if (BTN_SaveExpense != null) BTN_SaveExpense.Visible = true;
                if (DDL_ExpenseType != null) DDL_ExpenseType.Enabled = true;
                if (btnOpenEditKmFromExpense != null) btnOpenEditKmFromExpense.Visible = true;
                if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = string.Empty;
            }
        }

        private void ResetExpenseEntryFields()
        {
            if (TXT_DAAmount != null) TXT_DAAmount.Text = string.Empty;
            if (TXT_DAFromPlace != null) { TXT_DAFromPlace.Text = string.Empty; TXT_DAFromPlace.ReadOnly = false; }
            if (TXT_DAToPlace != null) { TXT_DAToPlace.Text = string.Empty; TXT_DAToPlace.ReadOnly = false; }
            if (TXT_TravelDescription != null) { TXT_TravelDescription.Text = string.Empty; TXT_TravelDescription.ReadOnly = false; }

            if (TXT_HotelAmount != null) { TXT_HotelAmount.Text = string.Empty; TXT_HotelAmount.ReadOnly = false; }
            if (TXT_HotelBillNo != null) { TXT_HotelBillNo.Text = string.Empty; TXT_HotelBillNo.ReadOnly = false; }
            if (TXT_HotelPurpose != null) { TXT_HotelPurpose.Text = string.Empty; TXT_HotelPurpose.ReadOnly = false; }

            if (TXT_TravelAmount != null) { TXT_TravelAmount.Text = string.Empty; TXT_TravelAmount.ReadOnly = false; }
            if (TXT_TravelInvoiceNo != null) { TXT_TravelInvoiceNo.Text = string.Empty; TXT_TravelInvoiceNo.ReadOnly = false; }

            DateTime istNow = DateTime.UtcNow.AddMinutes(330);
            if (TXT_TravelDate != null) TXT_TravelDate.Text = istNow.ToString("yyyy-MM-dd");
            if (TXT_HotelFromDate != null) TXT_HotelFromDate.Text = istNow.ToString("yyyy-MM-dd");
            if (TXT_HotelToDate != null) TXT_HotelToDate.Text = istNow.ToString("yyyy-MM-dd");

            if (CHK_TravelInvoice != null) CHK_TravelInvoice.Checked = false;
            if (DDL_ExpenseType != null) DDL_ExpenseType.SelectedIndex = 0;
            if (DDL_TravelType != null) DDL_TravelType.SelectedIndex = 0;

            if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "";

            if (HF_ActivityId != null) HF_ActivityId.Value = "";
            if (HF_ActivityDate != null) HF_ActivityDate.Value = "";
            if (HF_SnapResolvedDA != null) HF_SnapResolvedDA.Value = "";
            if (HF_SnapPolicyMode != null) HF_SnapPolicyMode.Value = "";

            if (TXT_StationaryAmount != null) { TXT_StationaryAmount.Text = string.Empty; TXT_StationaryAmount.ReadOnly = false; }
            if (TXT_StationaryBillNo != null) { TXT_StationaryBillNo.Text = string.Empty; TXT_StationaryBillNo.ReadOnly = false; }
            if (TXT_StationaryPurpose != null) { TXT_StationaryPurpose.Text = string.Empty; TXT_StationaryPurpose.ReadOnly = false; }

            string clearScript = string.Format("$('#{0}').val('');", FU_Attachment != null ? FU_Attachment.ClientID : "FU_Attachment");
            ScriptManager.RegisterStartupScript(this, this.GetType(), "clearFile", clearScript, true);
        }

        private bool SanitizeDecimal(string input, CultureInfo ci, out decimal result)
        {
            result = 0m;
            if (string.IsNullOrWhiteSpace(input)) return false;
            return decimal.TryParse(input, NumberStyles.Currency | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, ci, out result);
        }

        protected void BindPolicyApprovers()
        {
            string rOneName = (Session["RONE_NAME"] != null) ? Session["RONE_NAME"].ToString().Trim() : string.Empty;
            string rOneCode = (Session["RONE_CODE"] != null) ? Session["RONE_CODE"].ToString().Trim() : string.Empty;
            string rTwoName = (Session["RTWO_NAME"] != null) ? Session["RTWO_NAME"].ToString().Trim() : string.Empty;
            string rTwoCode = (Session["RTWO_CODE"] != null) ? Session["RTWO_CODE"].ToString().Trim() : string.Empty;
            string auditorName = (Session["AUDITOR_NAME"] != null) ? Session["AUDITOR_NAME"].ToString().Trim() : string.Empty;
            string auditorCode = (Session["AUDITOR_CODE"] != null) ? Session["AUDITOR_CODE"].ToString().Trim() : string.Empty;

            if (lblApproverL1 != null) lblApproverL1.Text = ComposeNameWithCode(rOneName, rOneCode);
            if (lblApproverL2 != null) lblApproverL2.Text = ComposeNameWithCode(rTwoName, rTwoCode);
            if (lblApproverAuditor != null) lblApproverAuditor.Text = ComposeNameWithCode(auditorName, auditorCode);

            if (approverL1Container != null) approverL1Container.Visible = !string.IsNullOrEmpty(rOneName) || !string.IsNullOrEmpty(rOneCode);
            if (approverL2Container != null) approverL2Container.Visible = !string.IsNullOrEmpty(rTwoName) || !string.IsNullOrEmpty(rTwoCode);
            if (approverAuditorContainer != null) approverAuditorContainer.Visible = !string.IsNullOrEmpty(auditorName) || !string.IsNullOrEmpty(auditorCode);
        }

        private string ComposeNameWithCode(string name, string code)
        {
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(code)) return string.Format("{0} ({1})", HttpUtility.HtmlEncode(name), HttpUtility.HtmlEncode(code));
            if (!string.IsNullOrEmpty(name)) return HttpUtility.HtmlEncode(name);
            if (!string.IsNullOrEmpty(code)) return string.Format("({0})", HttpUtility.HtmlEncode(code));
            return "—";
        }

        private void BindPolicyToModal(PolicyResolved policy)
        {
            if (policy == null) return;

            if (lblPolicyEmployeeCode != null) lblPolicyEmployeeCode.Text = HttpUtility.HtmlEncode(!string.IsNullOrEmpty(policy.EmployeeCode) ? policy.EmployeeCode : GetLoggedInEmployeeCode());
            if (lblPolicySource != null) lblPolicySource.Text = HttpUtility.HtmlEncode(!string.IsNullOrEmpty(policy.Source) ? policy.Source : "HR-SOP");
            if (lblPolicyRowId != null) lblPolicyRowId.Text = policy.RowId.HasValue ? policy.RowId.Value.ToString() : "—";
            if (lblPolicyPriority != null) lblPolicyPriority.Text = policy.Priority.HasValue ? policy.Priority.Value.ToString() : "—";

            string regionName = GetNameFromMaster("MST_Region", "Id", "Region_Description", policy.RegionId) ?? "All Regions";
            string branchName = GetNameFromMaster("MST_Branch", "Id", "Branch_Description", policy.BranchId) ?? "All Branches";
            string gradeName = GetNameFromMaster("MST_Grade", "Id", "Grade_Description", policy.GradeId) ?? "All Grades";
            string desigName = GetNameFromMaster("MST_Designation", "Id", "Designation_Description", policy.DesignationId) ?? "All Designations";

            if (lblPolicySpecificity != null) lblPolicySpecificity.Text = string.Format("{0} / {1} / {2} / {3}", regionName, branchName, gradeName, desigName);

            var effFrom = policy.EffectiveFrom.HasValue ? policy.EffectiveFrom.Value.ToString("dd-MM-yyyy") : "—";
            var effTo = policy.EffectiveTo.HasValue ? policy.EffectiveTo.Value.ToString("dd-MM-yyyy") : "—";
            if (lblPolicyEffective != null) lblPolicyEffective.Text = string.Format("{0} to {1}", effFrom, effTo);
            if (lblPolicyIsActive != null) lblPolicyIsActive.Text = policy.IsActive.HasValue ? (policy.IsActive.Value ? "Yes" : "No") : "—";

            string desigIdStr = policy.DesignationId.HasValue ? policy.DesignationId.Value.ToString() : "—";
            string gradeIdStr = policy.GradeId.HasValue ? policy.GradeId.Value.ToString() : "—";
            if (lblPolicyDesigGrade != null) lblPolicyDesigGrade.Text = (policy.DesignationId.HasValue || policy.GradeId.HasValue) ? string.Format("{0} / {1}", desigIdStr, gradeIdStr) : "—";

            if (lblResolvedMaxKM != null) lblResolvedMaxKM.Text = policy.Resolved_MaxKM.HasValue ? policy.Resolved_MaxKM.Value.ToString("N2") : "—";
            if (lblResolvedRatePerKm != null) lblResolvedRatePerKm.Text = policy.Resolved_RatePerKm.HasValue ? policy.Resolved_RatePerKm.Value.ToString("N2") : "—";
            if (lblResolvedDA != null) lblResolvedDA.Text = policy.Resolved_DA.HasValue ? policy.Resolved_DA.Value.ToString("N2") : "—";
            if (lblResolvedHotelCap != null) lblResolvedHotelCap.Text = policy.Resolved_HotelCap.HasValue ? policy.Resolved_HotelCap.Value.ToString("N2") : "—";

            if (HF_Policy_RegionId != null) HF_Policy_RegionId.Value = policy.RegionId.HasValue ? policy.RegionId.Value.ToString() : string.Empty;
            if (HF_Policy_BranchId != null) HF_Policy_BranchId.Value = policy.BranchId.HasValue ? policy.BranchId.Value.ToString() : string.Empty;
            if (HF_Policy_GradeId != null) HF_Policy_GradeId.Value = policy.GradeId.HasValue ? policy.GradeId.Value.ToString() : string.Empty;
            if (HF_Policy_DesignationId != null) HF_Policy_DesignationId.Value = policy.DesignationId.HasValue ? policy.DesignationId.Value.ToString() : string.Empty;

            if (HF_ResolvedHotelCap != null) HF_ResolvedHotelCap.Value = policy.Resolved_HotelCap.HasValue ? policy.Resolved_HotelCap.Value.ToString("F2") : "0";
            if (HF_ResolvedDA != null) HF_ResolvedDA.Value = policy.Resolved_DA.HasValue ? policy.Resolved_DA.Value.ToString("F2") : "200";

            var outstationVal = policy.Resolved_OutstationDA.HasValue ? policy.Resolved_OutstationDA.Value.ToString("F2") : "0";
            var ownArrVal = policy.Resolved_OwnAllowance.HasValue ? policy.Resolved_OwnAllowance.Value.ToString("F2") : "0";

            if (HF_OutstationDA != null) HF_OutstationDA.Value = policy.Resolved_OutstationDA.HasValue ? policy.Resolved_OutstationDA.Value.ToString("F2") : "350";
            if (HF_OwnArrangementDA != null) HF_OwnArrangementDA.Value = policy.Resolved_OwnAllowance.HasValue ? policy.Resolved_OwnAllowance.Value.ToString("F2") : "750";
            if (HF_ResolvedMaxKM != null) HF_ResolvedMaxKM.Value = policy.Resolved_MaxKM.HasValue ? policy.Resolved_MaxKM.Value.ToString("F2") : "0";
            if (HF_ResolvedRatePerKm != null) HF_ResolvedRatePerKm.Value = policy.Resolved_RatePerKm.HasValue ? policy.Resolved_RatePerKm.Value.ToString("F2") : "0";

            if (lblResolvedRemarks != null) lblResolvedRemarks.Text = HttpUtility.HtmlEncode(!string.IsNullOrEmpty(policy.Resolved_Remarks) ? policy.Resolved_Remarks : "Per Day Capping is Displayed");
            if (policyJsonRaw != null)
            {
                policyJsonRaw.Text = policy.PolicyJson ?? "";
                policyJsonRaw.Visible = false;
            }

            if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = string.Empty;
            BindPolicyApprovers();
        }

        private string GetNameFromMaster(string tableName, string idColumn, string textColumn, int? id)
        {
            if (!id.HasValue) return null;
            string name = null;
            using (var con = new SqlConnection(connString))
            {
                string query = string.Format("SELECT TOP 1 {0} FROM {1} WHERE {2} = @Id", textColumn, tableName, idColumn);
                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id.Value);
                    con.Open();
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value) name = Convert.ToString(result);
                }
            }
            return name;
        }

        private void RegisterClientNotification(string message, string severity = "info")
        {
            string jsMessage = HttpUtility.JavaScriptStringEncode(message);
            string script = "(function() { " +
                "var msg = '" + jsMessage + "'; " +
                "try { " +
                    "if (window.PNotify) { new PNotify({ text: msg, type: '" + severity + "', delay: 2500 }); } " +
                    "else if (window.toastr) { toastr['" + severity + "'](msg); } " +
                    "else { alert(msg); } " +
                "} catch(e) { try { alert(msg); } catch(ex){} } " +
            "})();";

            if (ScriptManager.GetCurrent(Page) != null)
                ScriptManager.RegisterStartupScript(this, this.GetType(), "policyNotify_" + Guid.NewGuid().ToString("N"), script, true);
            else
                ClientScript.RegisterStartupScript(this.GetType(), "policyNotify_" + Guid.NewGuid().ToString("N"), script, true);
        }

        private decimal GetRatePerKm(DataRow row, decimal fallbackRate)
        {
            if (row == null) return fallbackRate;
            string[] rateCols = new[] { "RatePerKm", "Rate", "PerKmRate", "RatePerKM", "ClaimRate" };
            foreach (var col in rateCols)
            {
                if (row.Table.Columns.Contains(col) && row[col] != DBNull.Value)
                {
                    decimal val;
                    if (decimal.TryParse(row[col].ToString(), out val)) return val;
                }
            }
            return fallbackRate;
        }

        protected void gvActivities_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            // Global connection string reference (adjust if yours is named differently)
            string connString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            if (e.CommandName == "OpenExpenses")
            {
                ResetExpenseEntryFields();
                string arg = e.CommandArgument.ToString();
                var parts = arg.Split('|');
                string activityId = parts[0];
                string activityDate = parts.Length > 1 ? parts[1] : "";

                if (lbl_BeatTravelled != null) lbl_BeatTravelled.Text = (parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2])) ? parts[2] : "-";
                if (lbl_DistributorName != null) lbl_DistributorName.Text = (parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3])) ? parts[3] : "-";
                if (lbl_HQTravelDistance != null) lbl_HQTravelDistance.Text = (parts.Length > 4 && !string.IsNullOrWhiteSpace(parts[4])) ? parts[4] : "-";

                // ⬅️ NEW: Safely extract and decode the remark from the 6th argument
                string remark = parts.Length > 5 ? HttpUtility.HtmlDecode(parts[5]) : "";

                if (HF_ActivityId != null) HF_ActivityId.Value = activityId;
                if (HF_ActivityDate != null) HF_ActivityDate.Value = activityDate;

                DataTable dt = LoadExpenseForActivity(activityId);

                // DIRECTLY EXTRACT SAVED POLICY MODE
                string explicitMode = "HQ"; // <-- ALWAYS default to HQ for fresh days

                if (dt != null && dt.Rows.Count > 0 && dt.Columns.Contains("PolicyMode") && dt.Rows[0]["PolicyMode"] != DBNull.Value)
                {
                    explicitMode = dt.Rows[0]["PolicyMode"].ToString().Trim();
                }

                if (string.IsNullOrWhiteSpace(explicitMode)) explicitMode = "HQ";

                // Setup the form fields
                if (dt == null || dt.Rows.Count == 0)
                {
                    if (HF_SnapPolicyMode != null && HF_ResolvedPolicyMode != null && !string.IsNullOrWhiteSpace(HF_ResolvedPolicyMode.Value)) HF_SnapPolicyMode.Value = HF_ResolvedPolicyMode.Value;
                    if (HF_SnapResolvedDA != null && HF_ResolvedDA != null && !string.IsNullOrWhiteSpace(HF_ResolvedDA.Value)) HF_SnapResolvedDA.Value = HF_ResolvedDA.Value;
                    if (HF_SnapHotelCap != null && HF_ResolvedHotelCap != null && !string.IsNullOrWhiteSpace(HF_ResolvedHotelCap.Value)) HF_SnapHotelCap.Value = HF_ResolvedHotelCap.Value;
                    if (HF_SnapOutstationDA != null && HF_OutstationDA != null && !string.IsNullOrWhiteSpace(HF_OutstationDA.Value)) HF_SnapOutstationDA.Value = HF_OutstationDA.Value;
                    if (HF_SnapOwnArrangementDA != null && HF_OwnArrangementDA != null && !string.IsNullOrWhiteSpace(HF_OwnArrangementDA.Value)) HF_SnapOwnArrangementDA.Value = HF_OwnArrangementDA.Value;

                    if (TXT_DAAmount != null && HF_SnapResolvedDA != null) TXT_DAAmount.Text = HF_SnapResolvedDA.Value;

                    DateTime parsed;
                    if (!string.IsNullOrWhiteSpace(activityDate) && DateTime.TryParse(activityDate, out parsed))
                    {
                        if (TXT_HotelFromDate != null) TXT_HotelFromDate.Text = parsed.ToString("yyyy-MM-dd");
                        if (TXT_HotelToDate != null) TXT_HotelToDate.Text = parsed.ToString("yyyy-MM-dd");
                        if (TXT_TravelDate != null) TXT_TravelDate.Text = parsed.ToString("yyyy-MM-dd");
                        if (lbl_traveldate != null) lbl_traveldate.Text = parsed.ToString("dd-MM-yyyy");
                    }

                    string clearScript = string.Format("$('#{0}').val('');", FU_Attachment != null ? FU_Attachment.ClientID : "FU_Attachment");
                    ScriptManager.RegisterStartupScript(this, GetType(), "clearFile_" + activityId, clearScript, true);
                }
                else
                {
                    decimal daSum = 0m, hotelSum = 0m;
                    DateTime? hotelFrom = null, hotelTo = null;
                    string hotelBill = string.Empty, hotelPurpose = string.Empty;

                    foreach (DataRow r in dt.Rows)
                    {
                        string type = r["ExpenseType"] != null ? r["ExpenseType"].ToString() : "";
                        decimal amt = r["Amount"] != DBNull.Value ? Convert.ToDecimal(r["Amount"]) : 0m;

                        if (string.Equals(type, "DA", StringComparison.OrdinalIgnoreCase))
                        {
                            if (TXT_DAAmount != null) TXT_DAAmount.Text = amt.ToString("0.##");
                            daSum += amt;
                        }
                        else if (string.Equals(type, "Hotel", StringComparison.OrdinalIgnoreCase))
                        {
                            hotelSum += amt;
                            if (r["FromDate"] != DBNull.Value) { DateTime f = Convert.ToDateTime(r["FromDate"]); hotelFrom = !hotelFrom.HasValue ? f : (f < hotelFrom ? f : hotelFrom); }
                            if (r["ToDate"] != DBNull.Value) { DateTime t = Convert.ToDateTime(r["ToDate"]); hotelTo = !hotelTo.HasValue ? t : (t > hotelTo ? t : hotelTo); }
                            if (string.IsNullOrWhiteSpace(hotelBill) && r["BillNo"] != DBNull.Value) hotelBill = r["BillNo"].ToString();
                            if (string.IsNullOrWhiteSpace(hotelPurpose) && r["Purpose"] != DBNull.Value) hotelPurpose = r["Purpose"].ToString();
                        }
                    }

                    if (!hotelFrom.HasValue || !hotelTo.HasValue)
                    {
                        DateTime parsedActivity;
                        string actDateStr = HF_ActivityDate != null ? HF_ActivityDate.Value : "";
                        if (!string.IsNullOrWhiteSpace(actDateStr) && DateTime.TryParse(actDateStr, out parsedActivity)) { if (!hotelFrom.HasValue) hotelFrom = parsedActivity.Date; if (!hotelTo.HasValue) hotelTo = parsedActivity.Date; }
                        else { var ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ist).Date; if (!hotelFrom.HasValue) hotelFrom = nowIst; if (!hotelTo.HasValue) hotelTo = nowIst; }
                    }

                    if (TXT_HotelFromDate != null) TXT_HotelFromDate.Text = hotelFrom.HasValue ? hotelFrom.Value.ToString("yyyy-MM-dd") : string.Empty;
                    if (TXT_HotelToDate != null) TXT_HotelToDate.Text = hotelTo.HasValue ? hotelTo.Value.ToString("yyyy-MM-dd") : string.Empty;
                    if (TXT_TravelDate != null) TXT_TravelDate.Text = hotelTo.HasValue ? hotelTo.Value.ToString("yyyy-MM-dd") : string.Empty;
                    if (lbl_traveldate != null) lbl_traveldate.Text = hotelFrom.HasValue ? hotelFrom.Value.ToString("dd-MM-yyyy") : "NA";
                    if (TXT_HotelBillNo != null) TXT_HotelBillNo.Text = hotelBill;
                    if (TXT_HotelPurpose != null) TXT_HotelPurpose.Text = hotelPurpose;
                    if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = string.Empty;
                }

                ApplyExpenseModalEditability(activityId);

                // Format the remark safely for JavaScript injection
                string safeRemark = remark.Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "");

                // ⬅️ NEW: Pass {2} (safeRemark) into the openExpenseModalSafe function
                string script = string.Format("if(typeof openExpenseModalSafe === 'function') openExpenseModalSafe('{0}', '{1}', '{2}'); else $('#expenseModal').modal('show');",
                    activityId,
                    explicitMode.Replace("'", "\\'"),
                    safeRemark);

                ScriptManager.RegisterStartupScript(this, this.GetType(), "openExpenseModalSafe_" + activityId, script, true);
            }
            else if (e.CommandName == "OpenEditModal")
            {
                string arg = Convert.ToString(e.CommandArgument);
                string[] parts = arg.Split('|');

                int activityId = 0;
                int.TryParse(parts[0], out activityId);
                string currentKm = parts.Length > 1 ? parts[1] : "0";
                string minKm = parts.Length > 2 ? parts[2] : "";
                string maxKm = parts.Length > 3 ? parts[3] : "";
                string activityDate = parts.Length > 4 ? parts[4] : "";

                DateTime parsed;
                if (!string.IsNullOrWhiteSpace(activityDate) && DateTime.TryParse(activityDate, out parsed))
                    if (lbl_kmmodal_date != null) lbl_kmmodal_date.Text = parsed.ToString("dd-MM-yyyy");

                if (hdnActivityId != null) hdnActivityId.Value = activityId.ToString();
                if (txtModalKM != null) txtModalKM.Text = currentKm;
                if (hdnOriginalKm != null) hdnOriginalKm.Value = currentKm;
                if (lblModalCurrentKm != null) lblModalCurrentKm.Text = "Current: " + currentKm + " km";

                string minDisplay = string.IsNullOrEmpty(minKm) ? "—" : minKm;
                string maxDisplay = string.IsNullOrEmpty(maxKm) ? "—" : maxKm;

                BindKmHistory(activityId);

                //// ====================================================================
                //// UI LOCK: KM Modal View-Only Mode
                //// ====================================================================
                //string claimStatus = "Draft";
                //using (SqlConnection conStat = new SqlConnection(connString))
                //{
                //    using (SqlCommand cmdStat = new SqlCommand("SELECT ClaimStatus FROM dbo.SalesDailyActivity WITH (NOLOCK) WHERE ActivityId = @ActId", conStat))
                //    {
                //        cmdStat.Parameters.AddWithValue("@ActId", activityId);
                //        conStat.Open();
                //        object statObj = cmdStat.ExecuteScalar();
                //        if (statObj != null && statObj != DBNull.Value) claimStatus = statObj.ToString().Trim();
                //    }
                //}

                // ====================================================================
                // UI LOCK: Expense Modal View-Only Mode
                // ====================================================================
                string detailStatus = "Draft"; // Default to Draft

                using (SqlConnection conStat = new SqlConnection(connString))
                {
                    // ⬅️ NEW: Query the specific LINE ITEM status, not the overall month status
                    string query = @"SELECT ISNULL(DetailStatus, 'Draft') FROM dbo.SalesApprovalRequestDetail WITH (NOLOCK) WHERE ActivityId = @ActId ORDER BY ApprovalRequestDetailId DESC";
                    string query1 = @"SELECT ISNULL(DetailStatus, 'Draft') FROM dbo.SalesApprovalRequestDetail WITH (NOLOCK) WHERE ActivityId = @ActId ORDER BY ApprovalRequestDetailId DESC";
                    using (SqlCommand cmdStat = new SqlCommand(query1, conStat))
                    {
                        cmdStat.Parameters.AddWithValue("@ActId", activityId);
                        conStat.Open();
                        object statObj = cmdStat.ExecuteScalar();
                        if (statObj != null && statObj != DBNull.Value)
                        {
                            detailStatus = statObj.ToString().Trim();
                        }
                    }
                }

                // ⬅️ NEW: Check if the SPECIFIC LINE ITEM is locked. 
                // If it is 'Returned' or 'Rejected' or 'Draft', it stays unlocked!
                if (detailStatus.Equals("Submitted", StringComparison.OrdinalIgnoreCase) ||
                    detailStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
                    detailStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                {
                    // 1. Hide the Save and KM Modify buttons
                    if (BTN_SaveExpense != null) BTN_SaveExpense.Visible = false;
                    if (DDL_ExpenseType != null) DDL_ExpenseType.Enabled = false;
                    if (btnOpenEditKmFromExpense != null) btnOpenEditKmFromExpense.Visible = false;

                    // 2. Loop through the Repeater to swap the Delete icon for the Lock icon
                    if (rptExpenses != null)
                    {
                        foreach (RepeaterItem item in rptExpenses.Items)
                        {
                            if (item.ItemType == ListItemType.Item || item.ItemType == ListItemType.AlternatingItem)
                            {
                                LinkButton lnkDelete = item.FindControl("lnkDeleteExpense") as LinkButton;
                                LinkButton lnkDisabled = item.FindControl("lnkDeleteExpenseDisabled") as LinkButton;

                                if (lnkDelete != null) lnkDelete.Visible = false; // Hide trash can
                                if (lnkDisabled != null) lnkDisabled.Visible = true; // Show padlock
                            }
                        }
                    }

                    if (LBL_ExpenseMsg != null)
                    {
                        LBL_ExpenseMsg.Text = "🔒 This day is " + detailStatus + ". You are in View-Only mode.";
                        LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                    }
                }
                else
                {
                    // It is Draft, Returned, or Rejected - unlock everything!
                    if (BTN_SaveExpense != null) BTN_SaveExpense.Visible = true;
                    if (DDL_ExpenseType != null) DDL_ExpenseType.Enabled = true;
                    if (btnOpenEditKmFromExpense != null) btnOpenEditKmFromExpense.Visible = true;

                    if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "";

                    // Unlocking repeater items is handled by rptExpenses_ItemDataBound natively
                }
                // ====================================================================

                //if (claimStatus.Equals("Submitted", StringComparison.OrdinalIgnoreCase) ||
                //    claimStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
                //    claimStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                //{
                //    if (btnSaveKm != null) btnSaveKm.Visible = false;
                //    if (txtModalKM != null) txtModalKM.Enabled = false;
                //    if (txtKmReason != null) txtKmReason.Enabled = false;

                //    if (lblModalCurrentKm != null)
                //    {
                //        lblModalCurrentKm.Text += "<br/><span style='color:red;'>🔒 <b>This month is " + claimStatus + ". You are in View-Only mode.</b></span>";
                //    }
                //}
                //else
                //{
                //    if (btnSaveKm != null) btnSaveKm.Visible = true;
                //    if (txtModalKM != null) txtModalKM.Enabled = true;
                //    if (txtKmReason != null) txtKmReason.Enabled = true;
                //}
                // ====================================================================

                string modalId = "editKmModal";
                string script = string.Format("setTimeout(function(){{ $('#{0}').modal('show'); $('#spMinKm').text('{1}'); $('#spMaxKm').text('{2}'); }}, 50);",
                    modalId, minDisplay.Replace("'", "\\'"), maxDisplay.Replace("'", "\\'"));

                ScriptManager.RegisterStartupScript(this, this.GetType(), "openEditKmModal_" + activityId, script, true);
            }
            else if (e.CommandName == "EditRow")
            {
                int activityId;
                if (int.TryParse(e.CommandArgument.ToString(), out activityId))
                {
                    // ====================================================================
                    // SECURITY GATE: Prevent Inline Editing if Submitted
                    // ====================================================================
                    string currentStatus = "Draft";
                    using (SqlConnection conStat = new SqlConnection(connString))
                    {
                        using (SqlCommand cmdStat = new SqlCommand("SELECT ClaimStatus FROM dbo.SalesDailyActivity WITH (NOLOCK) WHERE ActivityId = @ActId", conStat))
                        {
                            cmdStat.Parameters.AddWithValue("@ActId", activityId);
                            conStat.Open();
                            object statObj = cmdStat.ExecuteScalar();
                            if (statObj != null && statObj != DBNull.Value) currentStatus = statObj.ToString().Trim();
                        }
                    }

                    if (currentStatus.Equals("Submitted", StringComparison.OrdinalIgnoreCase) ||
                        currentStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
                        currentStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase) ||
                        currentStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                    {
                        lbl_msg.Text = currentStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase)
                            ? "Cannot edit row. This day was rejected by the auditor."
                            : "Cannot edit row. This claim is " + currentStatus + ".";
                        lbl_msg.ForeColor = System.Drawing.Color.Red;
                        return; // Blocks the inline edit entirely
                    }
                    // ====================================================================

                    int rowIndex = FindRowIndexByActivityId(activityId);
                    if (rowIndex >= 0)
                    {
                        gvActivities.EditIndex = rowIndex;
                        Session.Remove(GetCurrentSessionKey());
                        BindGrid(false);
                    }
                }
            }
            else if (e.CommandName == "UpdateRow")
            {
                int activityId = Convert.ToInt32(e.CommandArgument);

                // ====================================================================
                // SECURITY GATE: Prevent Inline Updating if Submitted
                // ====================================================================
                string currentStatus = "Draft";
                using (SqlConnection conStat = new SqlConnection(connString))
                {
                    using (SqlCommand cmdStat = new SqlCommand("SELECT ClaimStatus FROM dbo.SalesDailyActivity WITH (NOLOCK) WHERE ActivityId = @ActId", conStat))
                    {
                        cmdStat.Parameters.AddWithValue("@ActId", activityId);
                        conStat.Open();
                        object statObj = cmdStat.ExecuteScalar();
                        if (statObj != null && statObj != DBNull.Value) currentStatus = statObj.ToString().Trim();
                    }
                }

                if (currentStatus.Equals("Submitted", StringComparison.OrdinalIgnoreCase) ||
                    currentStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
                    currentStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase) ||
                    currentStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                {
                    lbl_msg.Text = currentStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase)
                        ? "Update blocked. This day was rejected by the auditor."
                        : "Update blocked. This claim is " + currentStatus + ".";
                    lbl_msg.ForeColor = System.Drawing.Color.Red;
                    gvActivities.EditIndex = -1; // Reset grid
                    BindGrid(false);
                    return;
                }
                // ====================================================================

                GridViewRow row = ((LinkButton)e.CommandSource).NamingContainer as GridViewRow;
                TextBox txtKM = row.FindControl("txtKM") as TextBox;
                if (txtKM == null) return;

                decimal newKm;
                if (!decimal.TryParse(txtKM.Text.Trim(), out newKm))
                {
                    lbl_msg.Text = "Enter valid KM value.";
                    return;
                }

                try
                {
                    using (SqlConnection con = new SqlConnection(connString))
                    using (SqlCommand cmd = new SqlCommand("usp_UpdateActivityKM", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ActivityId", activityId);
                        cmd.Parameters.AddWithValue("@NewKM", newKm);
                        cmd.Parameters.AddWithValue("@ModifiedBy", GetCurrentUserLoginId());
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                    gvActivities.EditIndex = -1;
                    Session.Remove(GetCurrentSessionKey());
                    BindGrid(false);
                    lbl_msg.ForeColor = System.Drawing.Color.Green;
                    lbl_msg.Text = "KM updated.";
                }
                catch (SqlException sqx)
                {
                    lbl_msg.ForeColor = System.Drawing.Color.Red;
                    lbl_msg.Text = sqx.Message;
                }
            }
        }

        protected void btnOpenEditKmFromExpense_Click(object sender, EventArgs e)
        {
            int activityId = 0;
            string hfActVal = HF_ActivityId != null ? HF_ActivityId.Value : "";
            if (!int.TryParse(hfActVal, out activityId) || activityId <= 0)
            {
                RegisterClientNotification("Invalid activity selected for KM edit.", "error");
                return;
            }

            DataTable dt = Session[GetCurrentSessionKey()] as DataTable;
            DataRow row = null;
            if (dt != null)
            {
                try
                {
                    if (dt.PrimaryKey != null && dt.PrimaryKey.Length > 0)
                        row = dt.Rows.Find(activityId);
                    else
                    {
                        foreach (DataRow r in dt.Rows)
                        {
                            if (r["ActivityId"] != DBNull.Value && Convert.ToInt32(r["ActivityId"]) == activityId) { row = r; break; }
                        }
                    }
                }
                catch { row = null; }
            }

            if (row != null) PopulateEditKmModalFromRow(row);
            else PopulateEditKmModalFromDb(activityId);

            // ====================================================================
            // SECURITY GATE: Prevent saving KMs if the month is already submitted
            // ====================================================================
            string currentStatus = "Draft";
            string connString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString; // Adjust if your variable is different

            using (SqlConnection conStat = new SqlConnection(connString))
            {
                using (SqlCommand cmdStat = new SqlCommand("SELECT ClaimStatus FROM dbo.SalesDailyActivity WITH (NOLOCK) WHERE ActivityId = @ActId", conStat))
                {
                    cmdStat.Parameters.AddWithValue("@ActId", activityId);
                    conStat.Open();
                    object statObj = cmdStat.ExecuteScalar();
                    if (statObj != null && statObj != DBNull.Value) currentStatus = statObj.ToString().Trim();
                }
            }

            if (currentStatus.Equals("Submitted", StringComparison.OrdinalIgnoreCase) ||
                currentStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
                currentStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                string safeErr = "Cannot modify KMs. The claim for this month is currently " + currentStatus + ".";
                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyLockErrKM", "showPNotify('Locked', '" + safeErr + "', 'error');", true);
                btnSaveKm.Enabled = false;
                //return; // <--- Instantly blocks the save!
            }
            // ====================================================================

            string hideOuter = "$('#expenseModal').modal('hide');";
            ScriptManager.RegisterStartupScript(this, this.GetType(), "hideExpenseBeforeOpenInner_" + activityId, hideOuter, true);
        }

        private void PopulateEditKmModalFromRow(DataRow row)
        {
            int activityId = row["ActivityId"] != DBNull.Value ? Convert.ToInt32(row["ActivityId"]) : 0;
            decimal totalKm = 0m;
            if (row.Table.Columns.Contains("TotalDistanceKM") && row["TotalDistanceKM"] != DBNull.Value)
                totalKm = Convert.ToDecimal(row["TotalDistanceKM"]);

            string currentKm = totalKm.ToString("0.##", CultureInfo.GetCultureInfo("en-IN"));
            string minKm = row.Table.Columns.Contains("MinAllowedKM") && row["MinAllowedKM"] != DBNull.Value ? row["MinAllowedKM"].ToString() : "";
            string maxKm = row.Table.Columns.Contains("MaxAllowedKM") && row["MaxAllowedKM"] != DBNull.Value ? row["MaxAllowedKM"].ToString() : "";
            string activityDate = row.Table.Columns.Contains("ActivityDate") && row["ActivityDate"] != DBNull.Value ? Convert.ToDateTime(row["ActivityDate"]).ToString("dd-MM-yyyy") : "";

            if (hdnActivityId != null) hdnActivityId.Value = activityId.ToString();
            if (txtModalKM != null) txtModalKM.Text = currentKm;
            if (hdnOriginalKm != null) hdnOriginalKm.Value = currentKm;
            if (lblModalCurrentKm != null) lblModalCurrentKm.Text = "Current: " + currentKm + " km";
            if (!string.IsNullOrEmpty(activityDate)) if (lbl_kmmodal_date != null) lbl_kmmodal_date.Text = activityDate;

            string minDisplay = string.IsNullOrEmpty(minKm) ? "—" : minKm;
            string maxDisplay = string.IsNullOrEmpty(maxKm) ? "—" : maxKm;

            BindKmHistory(activityId);

            string script = string.Format("setTimeout(function(){{ $('#editKmModal').modal('show'); $('#spMinKm').text('{0}'); $('#spMaxKm').text('{1}'); }}, 150);",
                minDisplay.Replace("'", "\\'"), maxDisplay.Replace("'", "\\'"));
            ScriptManager.RegisterStartupScript(this, this.GetType(), "openEditKmModal_" + activityId, script, true);

            string reopen = "$('#editKmModal').one('hidden.bs.modal', function(){ $('#expenseModal').modal('show'); });";
            ScriptManager.RegisterStartupScript(this, this.GetType(), "reopenExpenseModal_" + activityId, reopen, true);
        }

        private void PopulateEditKmModalFromDb(int activityId)
        {
            if (activityId <= 0) { RegisterClientNotification("Activity not found.", "warning"); return; }

            using (SqlConnection con = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 ActivityId, TotalDistanceKM, MinAllowedKM, MaxAllowedKM, ActivityDate FROM dbo.SalesDailyActivity WHERE ActivityId = @ActivityId", con))
            {
                cmd.Parameters.AddWithValue("@ActivityId", activityId);
                con.Open();
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        string currentKm = rdr["TotalDistanceKM"] != DBNull.Value ? Convert.ToDecimal(rdr["TotalDistanceKM"]).ToString("0.##", CultureInfo.GetCultureInfo("en-IN")) : "0";
                        string minKm = rdr["MinAllowedKM"] != DBNull.Value ? rdr["MinAllowedKM"].ToString() : "";
                        string maxKm = rdr["MaxAllowedKM"] != DBNull.Value ? rdr["MaxAllowedKM"].ToString() : "";
                        DateTime? aDate = rdr["ActivityDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(rdr["ActivityDate"]) : null;

                        if (hdnActivityId != null) hdnActivityId.Value = activityId.ToString();
                        if (txtModalKM != null) txtModalKM.Text = currentKm;
                        if (hdnOriginalKm != null) hdnOriginalKm.Value = currentKm;
                        if (lblModalCurrentKm != null) lblModalCurrentKm.Text = "Current: " + currentKm + " km";
                        if (aDate.HasValue) if (lbl_kmmodal_date != null) lbl_kmmodal_date.Text = aDate.Value.ToString("dd-MM-yyyy");

                        string minDisplay = string.IsNullOrEmpty(minKm) ? "—" : minKm;
                        string maxDisplay = string.IsNullOrEmpty(maxKm) ? "—" : maxKm;

                        BindKmHistory(activityId);

                        string script = string.Format("setTimeout(function(){{ $('#editKmModal').modal('show'); $('#spMinKm').text('{0}'); $('#spMaxKm').text('{1}'); }}, 150);", minDisplay.Replace("'", "\\'"), maxDisplay.Replace("'", "\\'"));
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "openEditKmModal_" + activityId, script, true);
                        return;
                    }
                }
            }
            RegisterClientNotification("Activity not found.", "warning");
        }

        private void BindKmHistory(int activityId)
        {
            DataTable dt = new DataTable();
            using (SqlConnection con = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand(@"SELECT HistoryId, ActivityId, OldKM, NewKM, ModifiedBy, ModifiedDateTime, Reason FROM dbo.SalesDailyActivityKMHistory WHERE ActivityId = @ActivityId ORDER BY HistoryId DESC", con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@ActivityId", activityId);
                using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
            }
            if (gvKmHistory != null)
            {
                gvKmHistory.DataSource = dt;
                gvKmHistory.DataBind();
            }
        }

        protected void btnSaveKm_Click(object sender, EventArgs e)
        {
            decimal newKm, origKm = 0;
            if (txtModalKM == null || !decimal.TryParse(txtModalKM.Text.Trim(), out newKm))
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "kmInvalidNumber", "document.getElementById('spClientKmError').style.display=''; document.getElementById('spClientKmError').innerText='Please enter a valid number.';", true);
                return;
            }

            if (hdnOriginalKm == null || !decimal.TryParse(hdnOriginalKm.Value, out origKm))
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "kmOrigMissing", "document.getElementById('spClientKmError').style.display=''; document.getElementById('spClientKmError').innerText='Original KM not found, cannot validate.'; $('#editKmModal').modal('show');", true);
                return;
            }

            if (newKm == origKm)
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "kmNotChanged", "document.getElementById('spClientKmError').style.display=''; document.getElementById('spClientKmError').innerText='KM value has not changed. Please modify before saving.'; $('#editKmModal').modal('show');", true);
                return;
            }

            string reason = txtKmReason != null ? txtKmReason.Text.Trim() : "";
            if (string.IsNullOrEmpty(reason))
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "kmReasonReq", "document.getElementById('spClientKmError').style.display=''; document.getElementById('spClientKmError').innerText='Please provide a reason for changing the KM.'; $('#editKmModal').modal('show');", true);
                return;
            }

            int activityId;
            if (hdnActivityId == null || !int.TryParse(hdnActivityId.Value, out activityId)) return;

            bool isRouteBased = !string.IsNullOrWhiteSpace(HF_RouteWaypoints.Value);

            if (isRouteBased)
            {
                decimal routeDistance;

                if (decimal.TryParse(HF_RouteDistance.Value, out routeDistance))
                {
                    if (Math.Abs(routeDistance - newKm) > 0.01M)
                    {
                        ScriptManager.RegisterStartupScript(
                            this,
                            GetType(),
                            "RouteKmMismatch",
                            "showPNotify('Validation','Route distance and KM value do not match. Please recalculate route.','error'); $('#editKmModal').modal('show');",
                            true);

                        return;
                    }
                }
            }

            // ====================================================================
            // SECURITY GATE: Prevent saving KMs if the month is already submitted
            // ====================================================================
            string currentStatus = "Draft";
            string connString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString; // Adjust if your variable is different

            using (SqlConnection conStat = new SqlConnection(connString))
            {
                string query1 = @"SELECT ISNULL(DetailStatus, 'Draft') FROM dbo.SalesApprovalRequestDetail WITH (NOLOCK) WHERE ActivityId = @ActId ORDER BY ApprovalRequestDetailId DESC";
                //using (SqlCommand cmdStat = new SqlCommand("SELECT ClaimStatus FROM dbo.SalesDailyActivity WITH (NOLOCK) WHERE ActivityId = @ActId", conStat))
                using (SqlCommand cmdStat = new SqlCommand(query1, conStat))
                {
                    cmdStat.Parameters.AddWithValue("@ActId", activityId);
                    conStat.Open();
                    object statObj = cmdStat.ExecuteScalar();
                    if (statObj != null && statObj != DBNull.Value) currentStatus = statObj.ToString().Trim();
                }
            }

            if (currentStatus.Equals("Submitted", StringComparison.OrdinalIgnoreCase) ||
                currentStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
                currentStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                string safeErr = "Cannot modify KMs. The claim for this month is currently " + currentStatus + ".";
                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyLockErrKM", "showPNotify('Locked', '" + safeErr + "', 'error');", true);
                return; // <--- Instantly blocks the save!
            }
            // ====================================================================
            string modifiedBy = GetCurrentUserLoginId();


            // Inside btnSaveKm_Click and gvActivities_RowUpdating before executing the update:
            decimal allowedMin = 0m, allowedMax = 499m; // Fetch these via SQL from SalesDailyActivity

            if (newKm < allowedMin || newKm > allowedMax)
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "kmOutOfBounds",
                    $"showPNotify('Validation', 'KM value {newKm} is outside the allowed policy range.', 'error');", true);
                return; // Block the save!
            }
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("usp_UpdateActivityKM", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        //cmd.Parameters.AddWithValue("@ActivityId", activityId);
                        //cmd.Parameters.AddWithValue("@NewKM", newKm);
                        //cmd.Parameters.AddWithValue("@ModifiedBy", modifiedBy);
                        //cmd.Parameters.AddWithValue("@Reason", reason);
                        cmd.Parameters.AddWithValue("@ActivityId", activityId);
                        cmd.Parameters.AddWithValue("@NewKM", newKm);
                        cmd.Parameters.AddWithValue("@ModifiedBy", modifiedBy);
                        cmd.Parameters.AddWithValue("@Reason", reason);

                        cmd.Parameters.AddWithValue(
                            "@RouteOriginName",
                            string.IsNullOrWhiteSpace(HF_RouteOrigin.Value)
                                ? (object)DBNull.Value
                                : HF_RouteOrigin.Value.Trim());

                        cmd.Parameters.AddWithValue(
                            "@RouteDestinationName",
                            string.IsNullOrWhiteSpace(HF_RouteDestination.Value)
                                ? (object)DBNull.Value
                                : HF_RouteDestination.Value.Trim());

                        cmd.Parameters.AddWithValue(
                            "@RouteWaypointsJSON",
                            string.IsNullOrWhiteSpace(HF_RouteWaypoints.Value)
                                ? (object)DBNull.Value
                                : HF_RouteWaypoints.Value);

                        cmd.Parameters.AddWithValue(
                            "@TravelMode",
                            string.IsNullOrWhiteSpace(HF_RouteTravelMode.Value)
                                ? (object)DBNull.Value
                                : HF_RouteTravelMode.Value);
                        cmd.ExecuteNonQuery();
                    }
                }

                Session.Remove(GetCurrentSessionKey());
                BindGrid(false);
                BindKmHistory(activityId);
                if (txtKmReason != null) txtKmReason.Text = string.Empty;

                string modalScript = "setTimeout(function(){ $('#editKmModal').modal('show'); $('#spMinKm').text('—'); $('#spMaxKm').text('—'); }, 50);";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "reopenEditKmModal_" + activityId, modalScript, true);
            }
            catch (SqlException sqlEx)
            {
                string msg = sqlEx.Message.Replace("'", "");
                ScriptManager.RegisterStartupScript(this, this.GetType(), "kmSaveSqlError", "alert('Database error: " + msg + "'); $('#editKmModal').modal('show');", true);
            }
            catch (Exception ex)
            {
                string msg = ex.Message.Replace("'", "");
                ScriptManager.RegisterStartupScript(this, this.GetType(), "kmSaveError", "alert('Error saving KM: " + msg + "'); $('#editKmModal').modal('show');", true);
            }
        }

        protected void SetPolicyModeAndTrigger_Client(string modeFromDb)
        {
            if (HF_ResolvedPolicyMode != null) HF_ResolvedPolicyMode.Value = modeFromDb != null ? modeFromDb : string.Empty;
            string safeMode = HttpUtility.JavaScriptStringEncode(modeFromDb != null ? modeFromDb : string.Empty);
            string script = "if(typeof applyPolicyModeFromServer === 'function') applyPolicyModeFromServer('" + safeMode + "'); else (function(){ try { applyPolicyModeFromServer && applyPolicyModeFromServer('" + safeMode + "'); } catch(e){console && console.log && console.log(e);} })();";
            ScriptManager.RegisterStartupScript(this.Page, this.Page.GetType(), "applyPolicyModeFromServer", script, true);
        }

        protected void ApplyPolicyModeFromCodeBehind(string modeFromDb)
        {
            if (HF_ResolvedPolicyMode != null) HF_ResolvedPolicyMode.Value = modeFromDb != null ? modeFromDb : string.Empty;
            string encMode = HttpUtility.JavaScriptStringEncode(modeFromDb != null ? modeFromDb : string.Empty);
            string hfId = HF_ResolvedPolicyMode != null ? HF_ResolvedPolicyMode.ClientID : "";

            string script = "(function() { " +
                "try { " +
                    "var hf = document.getElementById('" + hfId + "'); " +
                    "if (hf) hf.value = '" + encMode + "'; " +
                    "var sel = document.getElementById('SEL_PolicyMode') || document.querySelector('[id$=\"_SEL_PolicyMode\"]') || document.querySelector('[id$=\"SEL_PolicyMode\"]'); " +
                    "if (sel) { try { sel.value = '" + encMode + "'; } catch(e){} } " +
                    "try { localStorage.setItem('so_policy_mode', '" + encMode + "'); } catch(ex){} " +
                    "var ev = new CustomEvent('policy.updated', { detail: { mode: '" + encMode + "', source: 'server' } } ); " +
                    "document.dispatchEvent(ev); " +
                    "if (typeof applyPolicyMode === 'function') { try { applyPolicyMode(); } catch(e){} } " +
                "} catch(err) { } " +
            "})();";
            ScriptManager.RegisterStartupScript(this.Page, this.Page.GetType(), "applyPolicyModeFromServer", script, true);
        }

        protected void ApplyPolicyMode_ClientCall(string modeFromDb)
        {
            string mode = modeFromDb != null ? modeFromDb : string.Empty;
            try { if (HF_ResolvedPolicyMode != null) HF_ResolvedPolicyMode.Value = mode; } catch { }

            string safeMode = HttpUtility.JavaScriptStringEncode(mode);
            string script = "(function() { " +
                "try { " +
                    "if (typeof applyPolicyModeFromServer === 'function') { try { applyPolicyModeFromServer('" + safeMode + "'); } catch(e) { } } " +
                    "else { " +
                        "window.__so_pending_policy_mode = '" + safeMode + "'; " +
                        "document.addEventListener('DOMContentLoaded', function() { " +
                            "try { if (typeof applyPolicyModeFromServer === 'function' && window.__so_pending_policy_mode) { applyPolicyModeFromServer(window.__so_pending_policy_mode); window.__so_pending_policy_mode = null; } } catch(e) { } " +
                        "}); " +
                    "} " +
                "} catch(err) { } " +
            "})();";

            try
            {
                ScriptManager sm = ScriptManager.GetCurrent(this.Page);
                if (sm != null) ScriptManager.RegisterStartupScript(this.Page, this.Page.GetType(), "ApplyPolicyMode_ClientCall", script, true);
                else ClientScript.RegisterStartupScript(this.GetType(), "ApplyPolicyMode_ClientCall_Fallback", script, true);
            }
            catch { }
        }

        protected void gvActivities_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DataRowView drv = e.Row.DataItem as DataRowView;
                if (drv != null)
                {
                    string claimStatus = drv.Row.Table.Columns.Contains("ClaimStatus") && drv["ClaimStatus"] != DBNull.Value
                        ? drv["ClaimStatus"].ToString().Trim()
                        : string.Empty;
                    string detailStatus = drv.Row.Table.Columns.Contains("DetailStatus") && drv["DetailStatus"] != DBNull.Value
                        ? drv["DetailStatus"].ToString().Trim()
                        : string.Empty;

                    string remark = "";
                    if (drv.Row.Table.Columns.Contains("DetailRemarks") && drv["DetailRemarks"] != DBNull.Value)
                        remark = drv["DetailRemarks"].ToString();

                    string effectiveStatus = ResolveEffectiveRowStatus(claimStatus, detailStatus, drv);

                    Label lblStatus = e.Row.FindControl("lblClaimStatus") as Label;
                    if (lblStatus != null)
                    {
                        string encodedRemark = HttpUtility.HtmlEncode(remark);
                        switch (effectiveStatus.ToLowerInvariant())
                        {
                            case "draft":
                                lblStatus.Text = "<span class='badge badge-warning' data-toggle='tooltip' title='Draft – not submitted yet'><i class='fa fa-pencil-square-o'></i></span>";
                                break;
                            case "submitted":
                                lblStatus.Text = "<span class='badge badge-info' data-toggle='tooltip' title='Submitted – awaiting approval'><i class='fa fa-paper-plane'></i></span>";
                                break;
                            case "approved":
                                lblStatus.Text = "<span class='badge badge-success' data-toggle='tooltip' title='Approved by manager'><i class='fa fa-check-circle'></i></span>";
                                break;
                            case "returned":
                                lblStatus.Text = $"<span class='badge badge-warning text-dark' data-toggle='tooltip' title='Returned for correction{(string.IsNullOrWhiteSpace(encodedRemark) ? "" : " – " + encodedRemark)}'><i class='fa fa-undo'></i></span>";
                                break;
                            case "rejected":
                                lblStatus.Text = $"<span class='badge badge-danger' data-toggle='tooltip' title='Rejected – please review{(string.IsNullOrWhiteSpace(encodedRemark) ? "" : " – " + encodedRemark)}'><i class='fa fa-times-circle'></i></span>";
                                break;
                            case "processed":
                                lblStatus.Text = "<span class='badge badge-primary' data-toggle='tooltip' title='Processed in accounts'><i class='fa fa-cogs'></i></span>";
                                break;
                            default:
                                lblStatus.Text = $"<span class='badge badge-secondary' data-toggle='tooltip' title='{HttpUtility.HtmlEncode(string.IsNullOrWhiteSpace(effectiveStatus) ? "Unknown status" : effectiveStatus)}'><i class='fa fa-clock-o'></i></span>";
                                break;
                        }
                    }

                    PopulateKmTolerance(drv, e.Row);
                    ApplyApprovalEditability(drv, e.Row);
                }
            }
        }

        private static string ResolveEffectiveRowStatus(string claimStatus, string detailStatus, DataRowView drv)
        {
            if (string.Equals(detailStatus, "Returned", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(claimStatus, "Returned", StringComparison.OrdinalIgnoreCase))
                return "Returned";

            if (string.Equals(detailStatus, "Rejected", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(claimStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
                return "Rejected";

            if (!string.IsNullOrWhiteSpace(claimStatus))
                return claimStatus;

            bool isEditable = drv.Row.Table.Columns.Contains("IsEditable") &&
                              drv["IsEditable"] != DBNull.Value &&
                              Convert.ToBoolean(drv["IsEditable"]);
            return isEditable ? "Returned" : "Unknown";
        }

        private static bool IsRemovableExpenseStatus(string claimStatus)
        {
            return string.Equals(claimStatus, "Draft", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(claimStatus, "Returned", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsActivityEditableForExpenseDelete(int activityId)
        {
            if (activityId <= 0) return false;

            string claimStatus = "Draft";
            string detailStatus = string.Empty;

            using (SqlConnection con = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand(
                @"SELECT a.ClaimStatus,
                         (SELECT TOP 1 ISNULL(d.DetailStatus, '')
                          FROM dbo.SalesApprovalRequestDetail d
                          WHERE d.ActivityId = a.ActivityId
                          ORDER BY d.ApprovalRequestDetailId DESC) AS DetailStatus
                  FROM dbo.SalesDailyActivity a WITH (NOLOCK)
                  WHERE a.ActivityId = @ActId", con))
            {
                cmd.Parameters.AddWithValue("@ActId", activityId);
                con.Open();
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    if (!rdr.Read()) return false;
                    if (rdr["ClaimStatus"] != DBNull.Value) claimStatus = rdr["ClaimStatus"].ToString().Trim();
                    if (rdr["DetailStatus"] != DBNull.Value) detailStatus = rdr["DetailStatus"].ToString().Trim();
                }
            }

            if (string.Equals(detailStatus, "Returned", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(claimStatus, "Returned", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(claimStatus, "Draft", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private const string SoftDeleteExpenseSql = @"
            UPDATE dbo.SalesDailyExpense
            SET IsDeleted = 1,
                ClaimStatus = 'Draft',
                ModifiedBy = @User,
                ModifiedOn = SYSUTCDATETIME(),
                DeletedBy = @User,
                DeletedOn = DATEADD(MINUTE, 330, SYSUTCDATETIME()),
                DeleteReason = @Reason
            WHERE ExpenseId = @ExpenseId
              AND IsDeleted = 0
              AND ClaimStatus IN ('Draft', 'Returned')";

        private static string BuildSoftDeleteExpenseBatchSql(IEnumerable<int> expenseIds)
        {
            string inList = string.Join(",", expenseIds.Select(id => id.ToString()).ToArray());
            return string.Format(@"
                UPDATE dbo.SalesDailyExpense
                SET IsDeleted = 1,
                    ClaimStatus = 'Draft',
                    ModifiedBy = @User,
                    ModifiedOn = SYSUTCDATETIME(),
                    DeletedBy = @User,
                    DeletedOn = DATEADD(MINUTE, 330, SYSUTCDATETIME()),
                    DeleteReason = @Reason
                WHERE ExpenseId IN ({0})
                  AND IsDeleted = 0
                  AND ClaimStatus IN ('Draft', 'Returned')", inList);
        }

        private static void AddSoftDeleteParameters(SqlCommand cmd, string user, string reason)
        {
            cmd.Parameters.AddWithValue("@User", user);
            cmd.Parameters.AddWithValue("@Reason", reason);
        }

        private void PopulateKmTolerance(System.Data.DataRowView drv, GridViewRow row)
        {
            decimal totalKm = drv["TotalDistanceKM"] != DBNull.Value ? Convert.ToDecimal(drv["TotalDistanceKM"]) : 0m;
            decimal? minTol = drv.Row.Table.Columns.Contains("MinAllowedKM") && drv["MinAllowedKM"] != DBNull.Value ? (decimal?)Convert.ToDecimal(drv["MinAllowedKM"]) : null;
            decimal? maxTol = drv.Row.Table.Columns.Contains("MaxAllowedKM") && drv["MaxAllowedKM"] != DBNull.Value ? (decimal?)Convert.ToDecimal(drv["MaxAllowedKM"]) : null;

            decimal? allowedLower = null, allowedUpper = null;
            if (minTol.HasValue && maxTol.HasValue)
            {
                allowedLower = minTol.Value <= totalKm ? Math.Max(0, totalKm - minTol.Value) : minTol.Value;
                allowedUpper = maxTol.Value <= (totalKm * 5) ? totalKm + maxTol.Value : maxTol.Value;
            }
            else
            {
                if (minTol.HasValue) allowedLower = Math.Max(0, totalKm - minTol.Value);
                if (maxTol.HasValue) allowedUpper = totalKm + maxTol.Value;
            }

            Label lblTolLower = row.FindControl("lblTolLower") as Label;
            Label lblTolUpper = row.FindControl("lblTolUpper") as Label;
            Label lblTolLower_Edit = row.FindControl("lblTolLower_Edit") as Label;
            Label lblTolUpper_Edit = row.FindControl("lblTolUpper_Edit") as Label;

            Func<decimal?, string> F = d => d.HasValue ? d.Value.ToString("0.00") : "—";
            if (lblTolLower != null) lblTolLower.Text = F(allowedLower);
            if (lblTolUpper != null) lblTolUpper.Text = F(allowedUpper);
            if (lblTolLower_Edit != null) lblTolLower_Edit.Text = F(allowedLower);
            if (lblTolUpper_Edit != null) lblTolUpper_Edit.Text = F(allowedUpper);
        }

        private void ApplyApprovalEditability(System.Data.DataRowView drv, GridViewRow row)
        {
            bool isEditable = true;
            if (drv.Row.Table.Columns.Contains("IsEditable") && drv["IsEditable"] != DBNull.Value)
                bool.TryParse(drv["IsEditable"].ToString(), out isEditable);

            LinkButton lnkEdit = row.FindControl("lnkEdit") as LinkButton;
            LinkButton lnkExpenses = row.FindControl("lnkExpenses") as LinkButton;
            TextBox txtKM = row.FindControl("txtKM") as TextBox;
            Label lblKM = row.FindControl("lblKM") as Label;
            LinkButton lnkUpdate = row.FindControl("lnkUpdate") as LinkButton;
            LinkButton lnkCancel = row.FindControl("lnkCancel") as LinkButton;

            string claimStatus = drv.Row.Table.Columns.Contains("ClaimStatus") && drv["ClaimStatus"] != DBNull.Value ? drv["ClaimStatus"].ToString().Trim() : string.Empty;
            string detailStatus = drv.Row.Table.Columns.Contains("DetailStatus") && drv["DetailStatus"] != DBNull.Value ? drv["DetailStatus"].ToString().Trim() : string.Empty;

            string effectiveVisualStatus;
            if (string.Equals(detailStatus, "Returned", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(claimStatus, "Returned", StringComparison.OrdinalIgnoreCase))
                effectiveVisualStatus = "Returned";
            else if (string.Equals(detailStatus, "Rejected", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(claimStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
                effectiveVisualStatus = "Rejected";
            else if (string.Equals(claimStatus, "Draft", StringComparison.OrdinalIgnoreCase))
                effectiveVisualStatus = "Draft";
            else if (isEditable)
                effectiveVisualStatus = "Returned";
            else
                effectiveVisualStatus = !string.IsNullOrEmpty(claimStatus) ? claimStatus : "Unknown";

            Func<string, string, string> IconHtml = delegate (string iconClass, string clrClass) { return string.Format("<i class='{0} {1}' aria-hidden='true'></i>", iconClass, clrClass); };

            string editIconHtml, expenseIconHtml, colorClass;

            switch (effectiveVisualStatus.ToLowerInvariant())
            {
                case "returned": colorClass = "text-warning"; editIconHtml = IconHtml("fa fa-undo", colorClass); expenseIconHtml = IconHtml("fa fa-briefcase", colorClass); break;
                case "rejected": colorClass = "text-danger"; editIconHtml = IconHtml("fa fa-ban", colorClass); expenseIconHtml = IconHtml("fa fa-ban", colorClass); break;
                case "draft": colorClass = "text-primary"; editIconHtml = IconHtml("fa fa-pencil", colorClass); expenseIconHtml = IconHtml("fa fa-briefcase", colorClass); break;
                case "submitted": colorClass = "text-warning"; editIconHtml = IconHtml("fa fa-paper-plane", colorClass); expenseIconHtml = IconHtml("fa fa-briefcase", colorClass); break;
                case "approved": colorClass = "text-success"; editIconHtml = IconHtml("fa fa-check-circle", colorClass); expenseIconHtml = IconHtml("fa fa-briefcase", colorClass); break;
                default: colorClass = "text-muted"; editIconHtml = IconHtml("fa fa-clock-o", colorClass); expenseIconHtml = IconHtml("fa fa-briefcase", colorClass); break;
            }

            const string neutralBtnClass = "btn btn-sm btn-light";

            if (lnkEdit != null)
            {
                bool showEdit = isEditable && !string.Equals(effectiveVisualStatus, "Rejected", StringComparison.OrdinalIgnoreCase);
                if (!showEdit && string.Equals(claimStatus, "Draft", StringComparison.OrdinalIgnoreCase))
                    showEdit = true;

                lnkEdit.Visible = showEdit; lnkEdit.Enabled = showEdit; lnkEdit.CssClass = neutralBtnClass;
                lnkEdit.Text = editIconHtml; lnkEdit.ToolTip = showEdit ? "Edit" : "Edit (disabled)";
            }

            if (lnkExpenses != null)
            {
                bool canOpenExpenses = !string.Equals(effectiveVisualStatus, "Rejected", StringComparison.OrdinalIgnoreCase);
                lnkExpenses.Visible = true; lnkExpenses.Enabled = canOpenExpenses; lnkExpenses.CssClass = neutralBtnClass;
                lnkExpenses.Text = expenseIconHtml;
                lnkExpenses.ToolTip = canOpenExpenses ? "View / Add Expenses" : "Rejected days cannot be edited";
            }

            if (txtKM != null) { txtKM.ReadOnly = !isEditable || string.Equals(effectiveVisualStatus, "Rejected", StringComparison.OrdinalIgnoreCase); txtKM.CssClass = txtKM.CssClass + (isEditable ? " editable" : " readonly"); }
            if (lnkUpdate != null) lnkUpdate.Enabled = isEditable && !string.Equals(effectiveVisualStatus, "Rejected", StringComparison.OrdinalIgnoreCase);
            if (lnkCancel != null) lnkCancel.Enabled = true;
            if (lblKM != null) { lblKM.Text = drv["TotalDistanceKM"] != DBNull.Value ? Convert.ToDecimal(drv["TotalDistanceKM"]).ToString("N2") + " km" : "—"; lblKM.Visible = true; }

            if (string.Equals(effectiveVisualStatus, "Returned", StringComparison.OrdinalIgnoreCase))
            {
                row.CssClass = (row.CssClass + " row-returned").Trim();
                row.ToolTip = "Returned for correction. Edit this day and resubmit.";
            }
            else if (string.Equals(effectiveVisualStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
            {
                row.CssClass = (row.CssClass + " row-rejected").Trim();
                row.ToolTip = "Rejected by auditor. This day cannot be edited.";
            }

            bool monthLocked = ViewState["MonthLocked"] != null && (bool)ViewState["MonthLocked"];
            if (monthLocked && !isEditable)
            {
                row.CssClass = (row.CssClass + " row-locked-nofade").Trim();
                row.ToolTip = (row.ToolTip != null ? row.ToolTip : "") + " Row locked due to month submitted for approval.";
            }
        }

        private int FindRowIndexByActivityId(int activityId)
        {
            if (gvActivities != null)
            {
                for (int i = 0; i < gvActivities.Rows.Count; i++)
                {
                    if (gvActivities.DataKeys[i] != null && Convert.ToInt32(gvActivities.DataKeys[i].Value) == activityId) return i;
                }
            }
            return -1;
        }

        protected void gvActivities_RowEditing(object sender, GridViewEditEventArgs e)
        {
            if (gvActivities != null) gvActivities.EditIndex = e.NewEditIndex;
            Session.Remove(GetCurrentSessionKey());
            BindGrid(false);
        }

        protected void gvActivities_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            if (gvActivities != null) gvActivities.EditIndex = -1;
            Session.Remove(GetCurrentSessionKey());
            BindGrid(false);
        }

        protected void gvActivities_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            int rowIndex = e.RowIndex;

            if (gvActivities == null || gvActivities.DataKeys == null || gvActivities.DataKeys.Count <= rowIndex)
            {
                if (lbl_msg != null) { lbl_msg.Text = "Unable to locate DataKey for updated row."; lbl_msg.ForeColor = System.Drawing.Color.Red; }
                return;
            }

            int activityId = Convert.ToInt32(gvActivities.DataKeys[rowIndex].Value);
            GridViewRow row = gvActivities.Rows[rowIndex];
            TextBox txtKM = row.FindControl("txtKM") as TextBox;
            if (txtKM == null) { if (lbl_msg != null) { lbl_msg.Text = "KM textbox not found."; lbl_msg.ForeColor = System.Drawing.Color.Red; } return; }

            decimal newKm;
            CultureInfo ci = CultureInfo.GetCultureInfo("en-IN");
            if (!decimal.TryParse(txtKM.Text.Trim(), NumberStyles.AllowDecimalPoint, ci, out newKm)) { if (lbl_msg != null) { lbl_msg.Text = "Enter valid KM value."; lbl_msg.ForeColor = System.Drawing.Color.Red; } return; }

            // Inside btnSaveKm_Click and gvActivities_RowUpdating before executing the update:
            decimal allowedMin = 0m, allowedMax = 499m; // Fetch these via SQL from SalesDailyActivity

            if (newKm < allowedMin || newKm > allowedMax)
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "kmOutOfBounds",
                    $"showPNotify('Validation', 'KM value {newKm} is outside the allowed policy range.', 'error');", true);
                return; // Block the save!
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                using (SqlCommand cmd = new SqlCommand("usp_UpdateActivityKM", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ActivityId", activityId);
                    cmd.Parameters.AddWithValue("@NewKM", newKm);
                    cmd.Parameters.AddWithValue("@ModifiedBy", GetCurrentUserLoginId());
                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                var dt = Session[GetCurrentSessionKey()] as DataTable;
                if (dt != null)
                {
                    DataRow found = null;
                    if (dt.PrimaryKey != null && dt.PrimaryKey.Length > 0) found = dt.Rows.Find(activityId);
                    else { DataRow[] sel = dt.Select(string.Format("ActivityId = {0}", activityId)); if (sel != null && sel.Length > 0) found = sel[0]; }

                    if (found != null)
                    {
                        if (dt.Columns.Contains("TotalDistanceKM")) found["TotalDistanceKM"] = newKm;
                        decimal rate = 3.75m;
                        decimal parsedRate = 0m;
                        if (dt.Columns.Contains("RatePerKm") && found["RatePerKm"] != DBNull.Value && decimal.TryParse(found["RatePerKm"].ToString(), out parsedRate)) rate = parsedRate;
                        decimal calc = Math.Round(rate * newKm, 2, MidpointRounding.AwayFromZero);
                        if (dt.Columns.Contains("CalculatedClaimAmount")) found["CalculatedClaimAmount"] = calc;
                    }
                }

                gvActivities.EditIndex = -1;
                Session.Remove(GetCurrentSessionKey());
                BindGrid(false, false);

                if (lbl_msg != null)
                {
                    lbl_msg.Text = string.Format("Activity {0} updated (KM: {1:N2}).", activityId, newKm);
                    lbl_msg.ForeColor = System.Drawing.Color.Green;
                }
            }
            catch (SqlException sqx) { if (lbl_msg != null) { lbl_msg.ForeColor = System.Drawing.Color.Red; lbl_msg.Text = sqx.Message; } }
            catch (Exception ex) { if (lbl_msg != null) { lbl_msg.ForeColor = System.Drawing.Color.Red; lbl_msg.Text = ex.Message; } }
        }

        /*
        =======================================================================
        Script Name: Update_Stationary_Validation_Backend
        When: 2026-07-01
        Why: To enforce mandatory invoice attachments for Stationary expenses.
        What: Added file attachment validation block in BTN_SaveExpense_Click.
        =======================================================================
        */
        protected void BTN_SaveExpense_Click(object sender, EventArgs e)
        {
            if (LBL_ExpenseMsg != null)
            {
                LBL_ExpenseMsg.Text = "";
                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
            }

            int activityId;
            string hfAct = HF_ActivityId != null ? HF_ActivityId.Value : "";
            if (!int.TryParse(hfAct, out activityId))
            {
                if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "Invalid activity.";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                return;
            }

            // ====================================================================
            // SECURITY GATE: Prevent saving if the month is already submitted
            // ====================================================================
            string currentStatus = "Draft";
            using (SqlConnection conStat = new SqlConnection(connString))
            {
                string query1 = @"SELECT ISNULL(DetailStatus, 'Draft') FROM dbo.SalesApprovalRequestDetail WITH (NOLOCK) WHERE ActivityId = @ActId ORDER BY ApprovalRequestDetailId DESC";

                //using (SqlCommand cmdStat = new SqlCommand("SELECT ClaimStatus FROM dbo.SalesDailyActivity WITH (NOLOCK) WHERE ActivityId = @ActId", conStat))
                using (SqlCommand cmdStat = new SqlCommand(query1, conStat))
                {
                    cmdStat.Parameters.AddWithValue("@ActId", activityId);
                    conStat.Open();
                    object statObj = cmdStat.ExecuteScalar();
                    if (statObj != null && statObj != DBNull.Value)
                    {
                        currentStatus = statObj.ToString().Trim();
                    }
                }
            }

            if (currentStatus.Equals("Submitted", StringComparison.OrdinalIgnoreCase) ||
                currentStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
                currentStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                if (LBL_ExpenseMsg != null)
                {
                    LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                    LBL_ExpenseMsg.Text = "Access Denied: You cannot modify expenses for a submitted claim.";
                }
                string safeErr = "Cannot modify expenses. The claim for this month is currently " + currentStatus + ".";
                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyLockErr", "showPNotify('Locked', '" + safeErr + "', 'error');", true);
                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                return;
            }
            // ====================================================================

            string expenseType = "DA";
            if (DDL_ExpenseType != null && !string.IsNullOrWhiteSpace(DDL_ExpenseType.SelectedValue))
                expenseType = DDL_ExpenseType.SelectedValue.Trim();

            // Read from Request.Form to get values changed by JS
            string txtDaStr = Request.Form[TXT_DAAmount.UniqueID] ?? (TXT_DAAmount != null ? TXT_DAAmount.Text : "");
            decimal daAmt = 0m; decimal.TryParse(txtDaStr.Trim(), out daAmt);

            string txtHotelStr = Request.Form[TXT_HotelAmount.UniqueID] ?? (TXT_HotelAmount != null ? TXT_HotelAmount.Text : "");
            decimal hotelAmt = 0m; decimal.TryParse(txtHotelStr.Trim(), out hotelAmt);

            string txtTravelAmtStr = Request.Form[TXT_TravelAmount.UniqueID] ?? (TXT_TravelAmount != null ? TXT_TravelAmount.Text : "");
            decimal travelAmt = 0m; decimal.TryParse(txtTravelAmtStr.Trim(), out travelAmt);

            // Read Stationary Input values
            string txtStationaryAmtStr = Request.Form[TXT_StationaryAmount.UniqueID] ?? (TXT_StationaryAmount != null ? TXT_StationaryAmount.Text : "");
            decimal stationaryAmt = 0m; decimal.TryParse(txtStationaryAmtStr.Trim(), out stationaryAmt);

            string txtStationaryBillStr = Request.Form[TXT_StationaryBillNo.UniqueID] ?? (TXT_StationaryBillNo != null ? TXT_StationaryBillNo.Text : "");
            string txtStationaryPurpStr = Request.Form[TXT_StationaryPurpose.UniqueID] ?? (TXT_StationaryPurpose != null ? TXT_StationaryPurpose.Text : "");

            DateTime? travelDateNullable = null;
            string txtTravelDateStr = Request.Form[TXT_TravelDate.UniqueID] ?? (TXT_TravelDate != null ? TXT_TravelDate.Text : "");
            if (!string.IsNullOrWhiteSpace(txtTravelDateStr))
            {
                DateTime tmp;
                if (DateTime.TryParse(txtTravelDateStr.Trim(), out tmp)) travelDateNullable = tmp.Date;
                else if (expenseType.Equals("Travel", StringComparison.OrdinalIgnoreCase))
                {
                    if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "Invalid Travel Date.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
            }

            DateTime? fromDate = null, toDate = null;
            string txtHotelFromStr = Request.Form[TXT_HotelFromDate.UniqueID] ?? (TXT_HotelFromDate != null ? TXT_HotelFromDate.Text : "");
            if (!string.IsNullOrWhiteSpace(txtHotelFromStr))
            {
                DateTime tmp;
                if (DateTime.TryParse(txtHotelFromStr.Trim(), out tmp)) fromDate = tmp.Date;
                else
                {
                    if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "Invalid Hotel From Date.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
            }

            string txtHotelToStr = Request.Form[TXT_HotelToDate.UniqueID] ?? (TXT_HotelToDate != null ? TXT_HotelToDate.Text : "");
            if (!string.IsNullOrWhiteSpace(txtHotelToStr))
            {
                DateTime tmp;
                if (DateTime.TryParse(txtHotelToStr.Trim(), out tmp)) toDate = tmp.Date;
                else
                {
                    if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "Invalid Hotel To Date.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
            }

            string txtDaFromStr = Request.Form[TXT_DAFromPlace.UniqueID] ?? (TXT_DAFromPlace != null ? TXT_DAFromPlace.Text : "");
            string txtDaToStr = Request.Form[TXT_DAToPlace.UniqueID] ?? (TXT_DAToPlace != null ? TXT_DAToPlace.Text : "");
            string txtTravDescStr = Request.Form[TXT_TravelDescription.UniqueID] ?? (TXT_TravelDescription != null ? TXT_TravelDescription.Text : "");

            if (expenseType.Equals("DA", StringComparison.OrdinalIgnoreCase))
            {
                if (daAmt <= 0m) { if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "Invalid DA Amount. DA value is missing or 0."; ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true); return; }
                if (string.IsNullOrWhiteSpace(txtDaFromStr)) { if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "Please enter From Place for DA."; ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true); return; }
                if (txtDaFromStr.Trim().Length > 200) { if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "From Place cannot exceed 100 characters."; ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true); return; }
                if (string.IsNullOrWhiteSpace(txtDaToStr)) { if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "Please enter To Place for DA."; ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true); return; }
                if (txtDaToStr.Trim().Length > 200) { if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "To Place cannot exceed 100 characters."; ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true); return; }
                if (string.Equals(txtDaFromStr.Trim(), txtDaToStr.Trim(), StringComparison.OrdinalIgnoreCase)) { if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "From Place and To Place cannot be the same for DA."; ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true); return; }
                if (string.IsNullOrWhiteSpace(txtTravDescStr)) { if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "Remarks are required for DA."; ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true); return; }
                if (txtTravDescStr.Trim().Length < 5) { if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "Remarks must be at least 5 characters."; ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true); return; }
            }

            string txtHotelBillStr = Request.Form[TXT_HotelBillNo.UniqueID] ?? (TXT_HotelBillNo != null ? TXT_HotelBillNo.Text : "");

            if (expenseType.Equals("Hotel", StringComparison.OrdinalIgnoreCase))
            {
                if (hotelAmt <= 0m && !(FU_Attachment != null && FU_Attachment.HasFile)) { if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "Nothing to save. Enter Hotel amount or attach a file."; ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true); return; }
                if (!fromDate.HasValue || !toDate.HasValue) { if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "Please provide From Date and To Date for Hotel expense."; ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true); return; }
                if (toDate.Value < fromDate.Value) { if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "Hotel To Date cannot be earlier than From Date."; ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true); return; }
                if (string.IsNullOrWhiteSpace(txtHotelBillStr)) { if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "Bill/Invoice No is required for Hotel expense."; ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true); return; }
                if (!(FU_Attachment != null && FU_Attachment.HasFile)) { if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "Please attach a valid hotel receipt (JPG, PNG or PDF, ≤ 5MB)."; ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true); return; }
            }

            string txtTravelInvStr = Request.Form[TXT_TravelInvoiceNo.UniqueID] ?? (TXT_TravelInvoiceNo != null ? TXT_TravelInvoiceNo.Text : "");

            if (expenseType.Equals("Travel", StringComparison.OrdinalIgnoreCase))
            {
                if (travelAmt <= 0m) { if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "Please enter Travel Amount."; ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true); return; }
                if (!travelDateNullable.HasValue) { if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "Please enter Travel Date."; ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true); return; }
                if (CHK_TravelInvoice != null && CHK_TravelInvoice.Checked)
                {
                    if (string.IsNullOrWhiteSpace(txtTravelInvStr)) { if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "Invoice No is required when 'With Invoice' is checked."; ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true); return; }
                    if (!(FU_Attachment != null && FU_Attachment.HasFile)) { if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "Please attach invoice/receipt when 'With Invoice' is checked."; ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true); return; }
                }
            }

            // ==========================================
            // STATIONARY VALIDATION LOGIC
            // ==========================================
            if (expenseType.Equals("Stationary", StringComparison.OrdinalIgnoreCase))
            {
                if (stationaryAmt <= 0m)
                {
                    if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "Please enter a valid Stationary Amount.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtStationaryPurpStr))
                {
                    if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "Please provide a description of the stationary items.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
                if (!(FU_Attachment != null && FU_Attachment.HasFile))
                {
                    if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "Please attach a valid invoice/receipt for the Stationary expense.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
            }

            bool anyFile = FU_Attachment != null && FU_Attachment.HasFile;
            if (expenseType.Equals("DA", StringComparison.OrdinalIgnoreCase) && daAmt <= 0m && !anyFile)
            {
                if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "Nothing to save. Enter DA amount or attach a file.";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                return;
            }

            string savedRelativePath = null;
            string savedFileName = null;
            try
            {
                if (FU_Attachment != null && FU_Attachment.HasFile)
                {
                    if (FU_Attachment.PostedFile.ContentLength > 5 * 1024 * 1024) { if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "Attachment too large (max 5MB)."; ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true); return; }
                    string ext = Path.GetExtension(FU_Attachment.FileName).ToLower();
                    var allowed = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                    if (Array.IndexOf(allowed, ext) < 0) { if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "Invalid file type. Allowed: JPG, PNG, PDF."; ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true); return; }

                    DateTime istNow;
                    try { istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")); } catch { istNow = DateTime.UtcNow.AddMinutes(330); }

                    string baseFolder = Server.MapPath("~/UploadedFiles/ClaimsAttachment/");
                    string year = istNow.ToString("yyyy");
                    string month = istNow.ToString("MM");
                    string uploadsFolder = Path.Combine(baseFolder, year, month);
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string safeLogin = (GetCurrentUserLoginId(false) ?? "anon").Replace(" ", "_");
                    string uniquePart = istNow.ToString("yyyyMMddHHmmss") + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    savedFileName = string.Format("{0}_{1}_{2}{3}", safeLogin, activityId, uniquePart, ext);
                    string savedFullPath = Path.Combine(uploadsFolder, savedFileName);
                    FU_Attachment.SaveAs(savedFullPath);
                    savedRelativePath = Path.Combine("UploadedFiles/ClaimsAttachment", year, month, savedFileName).Replace("\\", "/");
                }

                string policyMode = Request.Form[HF_ResolvedPolicyMode.UniqueID] ?? (HF_ResolvedPolicyMode != null ? HF_ResolvedPolicyMode.Value : "");
                string hfDA = Request.Form[HF_ResolvedDA.UniqueID] ?? (HF_ResolvedDA != null ? HF_ResolvedDA.Value : "");
                string hfOutDA = Request.Form[HF_OutstationDA.UniqueID] ?? (HF_OutstationDA != null ? HF_OutstationDA.Value : "");
                string hfOwnDA = Request.Form[HF_OwnArrangementDA.UniqueID] ?? (HF_OwnArrangementDA != null ? HF_OwnArrangementDA.Value : "");
                string hfHotelCap = Request.Form[HF_ResolvedHotelCap.UniqueID] ?? (HF_ResolvedHotelCap != null ? HF_ResolvedHotelCap.Value : "");

                policyMode = policyMode.Trim();

                string policySnapshot = "{"
                    + "\"PolicyMode\":\"" + EscapeJson(policyMode) + "\","
                    + "\"ResolvedDA\":\"" + EscapeJson(hfDA) + "\","
                    + "\"OutstationDA\":\"" + EscapeJson(hfOutDA) + "\","
                    + "\"OwnArrangementDA\":\"" + EscapeJson(hfOwnDA) + "\","
                    + "\"HotelCap\":\"" + EscapeJson(hfHotelCap) + "\""
                    + "}";

                string createdByIp = null;
                try { createdByIp = Request.UserHostAddress; } catch { createdByIp = null; }

                decimal? resolvedDA = TryParseDecimalOrNull(hfDA);
                decimal? resolvedOutstationDA = TryParseDecimalOrNull(hfOutDA);
                decimal? resolvedOwnDA = TryParseDecimalOrNull(hfOwnDA);
                decimal? resolvedHotelCap = TryParseDecimalOrNull(hfHotelCap);

                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();

                    using (SqlTransaction tx = con.BeginTransaction())
                    {
                        try
                        {
                            if (expenseType.Equals("DA", StringComparison.OrdinalIgnoreCase) && (daAmt > 0m || savedFileName != null))
                            {
                                string fromPlace = txtDaFromStr.Trim();
                                string toPlace = txtDaToStr.Trim();

                                using (SqlCommand chkCmd = new SqlCommand("SELECT COUNT(1) FROM dbo.SalesDailyExpense WITH (UPDLOCK) WHERE ActivityId = @ActId AND ExpenseType = 'DA' AND IsDeleted = 0", con, tx))
                                {
                                    chkCmd.Parameters.AddWithValue("@ActId", activityId);
                                    int daCount = Convert.ToInt32(chkCmd.ExecuteScalar());
                                    if (daCount > 0)
                                    {
                                        throw new Exception("A DA expense has already been claimed for this activity. Please delete it first to re-enter.");
                                    }
                                }

                                using (SqlCommand cmd = new SqlCommand("usp_SaveDailyExpense", con, tx))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Parameters.AddWithValue("@ActivityId", activityId);
                                    cmd.Parameters.AddWithValue("@ExpenseType", "DA");

                                    DateTime expDate; DateTime? expDateNullable = null;
                                    string hfActDateStr = HF_ActivityDate != null && HF_ActivityDate.Value != null ? HF_ActivityDate.Value : "";
                                    if (!string.IsNullOrWhiteSpace(hfActDateStr) && DateTime.TryParse(hfActDateStr.Trim(), out expDate)) expDateNullable = expDate.Date;

                                    cmd.Parameters.AddWithValue("@ExpenseDate", expDateNullable.HasValue ? (object)expDateNullable.Value : (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Amount", daAmt);
                                    cmd.Parameters.AddWithValue("@FromPlace", string.IsNullOrWhiteSpace(fromPlace) ? (object)DBNull.Value : fromPlace);
                                    cmd.Parameters.AddWithValue("@ToPlace", string.IsNullOrWhiteSpace(toPlace) ? (object)DBNull.Value : toPlace);
                                    cmd.Parameters.AddWithValue("@Purpose", string.IsNullOrWhiteSpace(txtTravDescStr) ? (object)DBNull.Value : txtTravDescStr);
                                    cmd.Parameters.AddWithValue("@BillNo", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@AttachmentFileName", savedFileName ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@AttachmentPath", savedRelativePath ?? (object)DBNull.Value);
                                    cmd.Parameters.Add("@AttachmentContent", SqlDbType.VarBinary, -1).Value = DBNull.Value;
                                    cmd.Parameters.AddWithValue("@CreatedBy", GetCurrentUserLoginId());
                                    cmd.Parameters.AddWithValue("@PolicyMode", string.IsNullOrWhiteSpace(policyMode) ? (object)DBNull.Value : policyMode);
                                    cmd.Parameters.AddWithValue("@PolicySnapshot", string.IsNullOrWhiteSpace(policySnapshot) ? (object)DBNull.Value : policySnapshot);
                                    cmd.Parameters.AddWithValue("@IsAutoDA", 0);
                                    cmd.Parameters.AddWithValue("@CreatedByIP", string.IsNullOrWhiteSpace(createdByIp) ? (object)DBNull.Value : createdByIp);
                                    cmd.Parameters.AddWithValue("@Source", "UI");
                                    cmd.Parameters.AddWithValue("@ResolvedDA", resolvedDA.HasValue ? (object)resolvedDA.Value : (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@ResolvedOutstationDA", resolvedOutstationDA.HasValue ? (object)resolvedOutstationDA.Value : (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@ResolvedOwnArrangementDA", resolvedOwnDA.HasValue ? (object)resolvedOwnDA.Value : (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@ResolvedHotelCap", resolvedHotelCap.HasValue ? (object)resolvedHotelCap.Value : (object)DBNull.Value);

                                    var pErrCode = new SqlParameter("@ErrCode", SqlDbType.Int) { Direction = ParameterDirection.Output };
                                    var pErrMsg = new SqlParameter("@ErrMessage", SqlDbType.NVarChar, 400) { Direction = ParameterDirection.Output };
                                    cmd.Parameters.Add(pErrCode);
                                    cmd.Parameters.Add(pErrMsg);

                                    cmd.CommandTimeout = 90;
                                    cmd.ExecuteNonQuery();

                                    int err = (pErrCode.Value == DBNull.Value) ? 0 : (int)pErrCode.Value;
                                    string errMsg = (pErrMsg.Value == DBNull.Value) ? null : pErrMsg.Value.ToString();
                                    if (err != 0) throw new Exception(errMsg ?? "Action blocked by policy.");

                                    if (TXT_DAFromPlace != null) TXT_DAFromPlace.ReadOnly = true;
                                    if (TXT_DAToPlace != null) TXT_DAToPlace.ReadOnly = true;
                                    if (TXT_TravelDescription != null) TXT_TravelDescription.ReadOnly = true;
                                    if (FU_Attachment != null) FU_Attachment.Enabled = false;
                                    if (TXT_DAAmount != null) TXT_DAAmount.ReadOnly = true;

                                    if (LBL_ExpenseMsg != null)
                                    {
                                        LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Green;
                                        LBL_ExpenseMsg.Text = "DA saved successfully.";
                                    }
                                }
                            }

                            if (expenseType.Equals("Hotel", StringComparison.OrdinalIgnoreCase) && (hotelAmt > 0m || savedFileName != null))
                            {
                                string txtHotelPurpStr = Request.Form[TXT_HotelPurpose.UniqueID] ?? (TXT_HotelPurpose != null && TXT_HotelPurpose.Text != null ? TXT_HotelPurpose.Text.Trim() : null);

                                using (SqlCommand cmd = new SqlCommand("usp_SaveHotelExpense", con, tx))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Parameters.AddWithValue("@ActivityId", activityId);
                                    cmd.Parameters.AddWithValue("@FromDate", fromDate.Value);
                                    cmd.Parameters.AddWithValue("@ToDate", toDate.Value);
                                    cmd.Parameters.AddWithValue("@BillNo", string.IsNullOrWhiteSpace(txtHotelBillStr) ? (object)DBNull.Value : txtHotelBillStr);
                                    cmd.Parameters.AddWithValue("@Amount", hotelAmt);
                                    cmd.Parameters.AddWithValue("@Purpose", string.IsNullOrWhiteSpace(txtHotelPurpStr) ? (object)DBNull.Value : txtHotelPurpStr);
                                    cmd.Parameters.AddWithValue("@AttachmentFileName", savedFileName ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@AttachmentPath", savedRelativePath ?? (object)DBNull.Value);
                                    cmd.Parameters.Add("@AttachmentContent", SqlDbType.VarBinary, -1).Value = DBNull.Value;
                                    cmd.Parameters.AddWithValue("@CreatedBy", GetCurrentUserLoginId());
                                    cmd.Parameters.AddWithValue("@PolicyMode", string.IsNullOrWhiteSpace(policyMode) ? (object)DBNull.Value : policyMode);
                                    cmd.Parameters.AddWithValue("@PolicySnapshot", string.IsNullOrWhiteSpace(policySnapshot) ? (object)DBNull.Value : policySnapshot);
                                    cmd.Parameters.AddWithValue("@IsAutoDA", 0);
                                    cmd.Parameters.AddWithValue("@CreatedByIP", string.IsNullOrWhiteSpace(createdByIp) ? (object)DBNull.Value : createdByIp);
                                    cmd.Parameters.AddWithValue("@Source", "UI");
                                    cmd.Parameters.AddWithValue("@ResolvedHotelCap", resolvedHotelCap.HasValue ? (object)resolvedHotelCap.Value : (object)DBNull.Value);

                                    var pErrCode = new SqlParameter("@ErrCode", SqlDbType.Int) { Direction = ParameterDirection.Output };
                                    var pErrMsg = new SqlParameter("@ErrMessage", SqlDbType.NVarChar, 400) { Direction = ParameterDirection.Output };
                                    cmd.Parameters.Add(pErrCode);
                                    cmd.Parameters.Add(pErrMsg);

                                    cmd.CommandTimeout = 90;
                                    cmd.ExecuteNonQuery();

                                    int err = (pErrCode.Value == DBNull.Value) ? 0 : (int)pErrCode.Value;
                                    string errMsg = (pErrMsg.Value == DBNull.Value) ? null : pErrMsg.Value.ToString();
                                    if (err != 0) throw new Exception(errMsg ?? "Action blocked by policy.");
                                }

                                if (!string.IsNullOrWhiteSpace(policyMode) &&
                                    (policyMode.Equals("Outstation", StringComparison.OrdinalIgnoreCase) ||
                                     policyMode.Equals("OwnArrangement", StringComparison.OrdinalIgnoreCase)))
                                {
                                    decimal autoDA = 0m;
                                    if (policyMode.Equals("Outstation", StringComparison.OrdinalIgnoreCase))
                                        decimal.TryParse(hfOutDA, out autoDA);
                                    else
                                        decimal.TryParse(hfOwnDA, out autoDA);

                                    if (autoDA > 0m)
                                    {
                                        DateTime daDate = DateTime.Now;
                                        string hfActDateStr2 = HF_ActivityDate != null && HF_ActivityDate.Value != null ? HF_ActivityDate.Value : "";
                                        if (!string.IsNullOrWhiteSpace(hfActDateStr2)) DateTime.TryParse(hfActDateStr2, out daDate);
                                        else if (fromDate.HasValue) daDate = fromDate.Value;
                                        else if (travelDateNullable.HasValue) daDate = travelDateNullable.Value;

                                        EnsureAutoDA(activityId, daDate, autoDA, policyMode, policySnapshot, GetCurrentUserLoginId(), con, tx);
                                    }
                                }
                            }

                            if (expenseType.Equals("Travel", StringComparison.OrdinalIgnoreCase) && travelAmt > 0m)
                            {
                                string travelType = DDL_TravelType != null ? DDL_TravelType.SelectedValue : null;
                                string fromPlace = Request.Form[TXT_FromPlace.UniqueID] ?? (TXT_FromPlace != null && TXT_FromPlace.Text != null ? TXT_FromPlace.Text.Trim() : null);
                                string toPlace = Request.Form[TXT_ToPlace.UniqueID] ?? (TXT_ToPlace != null && TXT_ToPlace.Text != null ? TXT_ToPlace.Text.Trim() : null);
                                string invoiceNo = (CHK_TravelInvoice != null && CHK_TravelInvoice.Checked) ? txtTravelInvStr : null;
                                string travelPurpose = Request.Form[TXT_TravelPurpose.UniqueID] ?? (TXT_TravelPurpose != null && TXT_TravelPurpose.Text != null ? TXT_TravelPurpose.Text.Trim() : null);

                                using (SqlCommand cmd = new SqlCommand("dbo.usp_InsertTravel_WithLock", con, tx))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Parameters.AddWithValue("@ActivityId", activityId);
                                    cmd.Parameters.AddWithValue("@ExpenseDate", travelDateNullable.HasValue ? (object)travelDateNullable.Value : DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Amount", travelAmt);
                                    cmd.Parameters.AddWithValue("@TravelType", string.IsNullOrWhiteSpace(travelType) ? (object)DBNull.Value : travelType);
                                    cmd.Parameters.AddWithValue("@FromPlace", string.IsNullOrWhiteSpace(fromPlace) ? (object)DBNull.Value : fromPlace);
                                    cmd.Parameters.AddWithValue("@ToPlace", string.IsNullOrWhiteSpace(toPlace) ? (object)DBNull.Value : toPlace);
                                    cmd.Parameters.AddWithValue("@BillNo", string.IsNullOrWhiteSpace(invoiceNo) ? (object)DBNull.Value : invoiceNo);
                                    cmd.Parameters.AddWithValue("@AttachmentFileName", savedFileName ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@AttachmentPath", savedRelativePath ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Purpose", string.IsNullOrWhiteSpace(travelPurpose) ? (object)DBNull.Value : travelPurpose);
                                    cmd.Parameters.AddWithValue("@PolicyMode", string.IsNullOrWhiteSpace(policyMode) ? (object)DBNull.Value : policyMode);
                                    cmd.Parameters.AddWithValue("@PolicySnapshot", string.IsNullOrWhiteSpace(policySnapshot) ? (object)DBNull.Value : policySnapshot);
                                    cmd.Parameters.AddWithValue("@CreatedBy", GetCurrentUserLoginId() ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@CreatedByIP", string.IsNullOrWhiteSpace(createdByIp) ? (object)DBNull.Value : createdByIp);

                                    var pIsFirst = new SqlParameter("@IsFirst", SqlDbType.Bit) { Direction = ParameterDirection.Output };
                                    var pErr = new SqlParameter("@ErrCode", SqlDbType.Int) { Direction = ParameterDirection.Output };
                                    var pMsg = new SqlParameter("@ErrMessage", SqlDbType.NVarChar, 400) { Direction = ParameterDirection.Output };
                                    var pInsertedId = new SqlParameter("@InsertedExpenseId", SqlDbType.Int) { Direction = ParameterDirection.Output };

                                    cmd.Parameters.Add(pIsFirst); cmd.Parameters.Add(pInsertedId); cmd.Parameters.Add(pErr); cmd.Parameters.Add(pMsg);

                                    cmd.ExecuteNonQuery();

                                    int err = pErr.Value != DBNull.Value ? (int)pErr.Value : 0;
                                    if (err != 0) throw new Exception(pMsg.Value != null ? pMsg.Value.ToString() : "DB error");

                                    bool isFirst = (pIsFirst.Value != DBNull.Value && (bool)pIsFirst.Value);

                                    if (isFirst && !string.IsNullOrWhiteSpace(policyMode) &&
                                        (policyMode.Equals("Outstation", StringComparison.OrdinalIgnoreCase) ||
                                         policyMode.Equals("OwnArrangement", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        decimal autoDA = 0m;
                                        if (policyMode.Equals("Outstation", StringComparison.OrdinalIgnoreCase))
                                            decimal.TryParse(hfOutDA, out autoDA);
                                        else
                                            decimal.TryParse(hfOwnDA, out autoDA);

                                        if (autoDA > 0m)
                                            EnsureAutoDA(activityId, travelDateNullable ?? DateTime.Now, autoDA, policyMode, policySnapshot, GetCurrentUserLoginId(), con, tx);
                                    }
                                }
                            }

                            // ==========================================
                            // STATIONARY SAVING LOGIC
                            // ==========================================
                            if (expenseType.Equals("Stationary", StringComparison.OrdinalIgnoreCase) && (stationaryAmt > 0m || savedFileName != null))
                            {
                                using (SqlCommand cmd = new SqlCommand("usp_SaveDailyExpense", con, tx))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Parameters.AddWithValue("@ActivityId", activityId);
                                    cmd.Parameters.AddWithValue("@ExpenseType", "Stationary");

                                    DateTime expDate; DateTime? expDateNullable = null;
                                    string hfActDateStr = HF_ActivityDate != null ? HF_ActivityDate.Value : "";
                                    if (!string.IsNullOrWhiteSpace(hfActDateStr) && DateTime.TryParse(hfActDateStr.Trim(), out expDate))
                                        expDateNullable = expDate.Date;

                                    cmd.Parameters.AddWithValue("@ExpenseDate", expDateNullable.HasValue ? (object)expDateNullable.Value : DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Amount", stationaryAmt);
                                    cmd.Parameters.AddWithValue("@FromPlace", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@ToPlace", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Purpose", txtStationaryPurpStr.Trim());
                                    cmd.Parameters.AddWithValue("@BillNo", string.IsNullOrWhiteSpace(txtStationaryBillStr) ? DBNull.Value : (object)txtStationaryBillStr.Trim());

                                    cmd.Parameters.AddWithValue("@AttachmentFileName", savedFileName ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@AttachmentPath", savedRelativePath ?? (object)DBNull.Value);
                                    cmd.Parameters.Add("@AttachmentContent", SqlDbType.VarBinary, -1).Value = DBNull.Value;

                                    cmd.Parameters.AddWithValue("@CreatedBy", GetCurrentUserLoginId());
                                    cmd.Parameters.AddWithValue("@PolicyMode", string.IsNullOrWhiteSpace(policyMode) ? DBNull.Value : (object)policyMode);
                                    cmd.Parameters.AddWithValue("@PolicySnapshot", string.IsNullOrWhiteSpace(policySnapshot) ? DBNull.Value : (object)policySnapshot);
                                    cmd.Parameters.AddWithValue("@IsAutoDA", 0);
                                    cmd.Parameters.AddWithValue("@CreatedByIP", string.IsNullOrWhiteSpace(createdByIp) ? DBNull.Value : (object)createdByIp);
                                    cmd.Parameters.AddWithValue("@Source", "UI");

                                    cmd.Parameters.AddWithValue("@ResolvedDA", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@ResolvedOutstationDA", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@ResolvedOwnArrangementDA", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@ResolvedHotelCap", DBNull.Value);

                                    var pErrCode = new SqlParameter("@ErrCode", SqlDbType.Int) { Direction = ParameterDirection.Output };
                                    var pErrMsg = new SqlParameter("@ErrMessage", SqlDbType.NVarChar, 400) { Direction = ParameterDirection.Output };
                                    cmd.Parameters.Add(pErrCode);
                                    cmd.Parameters.Add(pErrMsg);

                                    cmd.ExecuteNonQuery();

                                    int err = (pErrCode.Value == DBNull.Value) ? 0 : (int)pErrCode.Value;
                                    if (err != 0) throw new Exception((pErrMsg.Value == DBNull.Value) ? "Action blocked by policy." : pErrMsg.Value.ToString());
                                }
                            }

                            tx.Commit();
                        }
                        catch (Exception innerEx)
                        {
                            tx.Rollback();
                            throw innerEx;
                        }
                    }
                }

                DataTable dtAfter = LoadExpenseForActivity(activityId.ToString());
                Session.Remove(GetCurrentSessionKey());
                BindGrid(false);

                if (LBL_ExpenseMsg != null)
                {
                    LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Green;
                    LBL_ExpenseMsg.Text = "Expense saved.";
                }

                string savedMode = "";
                if (dtAfter != null && dtAfter.Rows.Count > 0 && dtAfter.Columns.Contains("PolicyMode") && dtAfter.Rows[0]["PolicyMode"] != DBNull.Value)
                    savedMode = dtAfter.Rows[0]["PolicyMode"].ToString().Trim();
                if (string.IsNullOrWhiteSpace(savedMode)) savedMode = policyMode;

                string scriptSave = string.Format("if(typeof openExpenseModalSafe === 'function') openExpenseModalSafe('{0}', '{1}'); else $('#expenseModal').modal('show');", activityId, savedMode.Replace("'", "\\'"));
                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpenseSaved", scriptSave, true);
            }
            catch (Exception ex)
            {
                if (LBL_ExpenseMsg != null)
                {
                    LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                    LBL_ExpenseMsg.Text = ex.Message;
                }
                string safeMessage = ex.Message.Replace("'", "\\'").Replace(Environment.NewLine, " ");
                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyError2", "showPNotify('Error', '" + safeMessage + "', 'error');", true);
                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
            }
        }

        protected void rptExpenses_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem) return;

            var rowView = e.Item.DataItem as System.Data.DataRowView;
            if (rowView == null) return;
            var r = rowView.Row;

            string claimStatus = r.Table.Columns.Contains("ClaimStatus") && r["ClaimStatus"] != DBNull.Value ? r["ClaimStatus"].ToString() : "";
            bool isAuto = r.Table.Columns.Contains("IsAutoDA") && r["IsAutoDA"] != DBNull.Value && Convert.ToInt32(r["IsAutoDA"]) == 1;
            string policyMode = r.Table.Columns.Contains("PolicyMode") && r["PolicyMode"] != DBNull.Value ? r["PolicyMode"].ToString() : "";

            LinkButton lnkDelete = e.Item.FindControl("lnkDeleteExpense") as LinkButton;
            LinkButton lnkDeleteDisabled = e.Item.FindControl("lnkDeleteExpenseDisabled") as LinkButton;
            Literal litBadges = e.Item.FindControl("litPolicyBadges") as Literal;

            bool canDelete = IsRemovableExpenseStatus(claimStatus);
            if (!string.IsNullOrWhiteSpace(claimStatus) &&
                (claimStatus.Equals("Submitted", StringComparison.OrdinalIgnoreCase) ||
                 claimStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
                 claimStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase)))
                canDelete = false;
            if (isAuto) canDelete = false;

            if (lnkDelete != null) lnkDelete.Visible = canDelete;
            if (lnkDeleteDisabled != null) lnkDeleteDisabled.Visible = !canDelete;

            if (litBadges != null)
            {
                var sb = new System.Text.StringBuilder();
                if (!string.IsNullOrWhiteSpace(policyMode)) sb.AppendFormat("<span class='badge badge-info mr-1'>{0}</span>", HttpUtility.HtmlEncode(policyMode));
                if (isAuto) sb.Append("<span class='badge badge-warning mr-1'>AUTO</span>");
                if (!string.IsNullOrWhiteSpace(claimStatus))
                {
                    string badgeClass = string.Equals(claimStatus, "Returned", StringComparison.OrdinalIgnoreCase)
                        ? "badge badge-warning text-dark"
                        : (string.Equals(claimStatus, "Rejected", StringComparison.OrdinalIgnoreCase)
                            ? "badge badge-danger"
                            : "badge badge-secondary");
                    sb.AppendFormat("<span class='{0} mr-1'>{1}</span>", badgeClass, HttpUtility.HtmlEncode(claimStatus));
                }
                litBadges.Text = sb.ToString();
            }

            if (lnkDelete != null && (string.IsNullOrWhiteSpace(lnkDelete.CommandArgument) || !lnkDelete.CommandArgument.Contains("|")))
            {
                string expenseId = r.Table.Columns.Contains("ExpenseId") && r["ExpenseId"] != DBNull.Value ? r["ExpenseId"].ToString() : "";
                string activityId = r.Table.Columns.Contains("ActivityId") && r["ActivityId"] != DBNull.Value ? r["ActivityId"].ToString() : "";
                string attachmentPath = r.Table.Columns.Contains("AttachmentPath") && r["AttachmentPath"] != DBNull.Value ? r["AttachmentPath"].ToString() : "";
                lnkDelete.CommandArgument = string.Format("{0}|{1}|{2}", expenseId, attachmentPath, activityId);
            }
        }

        private void EnsureAutoDA(int activityId, DateTime expenseDate, decimal amount, string policyMode, string policySnapshot, string createdBy, SqlConnection con, SqlTransaction tx)
        {
            // FIX: Added 'WITH (UPDLOCK)' for transaction safety and 'AND IsDeleted = 0' to ignore soft-deleted records!
            using (SqlCommand chk = new SqlCommand(@"SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.SalesDailyExpense WITH (UPDLOCK) WHERE ActivityId = @ActivityId AND LOWER(ExpenseType) = 'da' AND CAST(ExpenseDate AS date) = @ExpenseDate AND IsDeleted = 0) THEN 1 ELSE 0 END", con, tx))
            {
                chk.CommandType = CommandType.Text;
                chk.Parameters.Add("@ActivityId", SqlDbType.Int).Value = activityId;
                chk.Parameters.Add("@ExpenseDate", SqlDbType.Date).Value = expenseDate.Date;

                int existsFlag = Convert.ToInt32(chk.ExecuteScalar() ?? 0);
                if (existsFlag == 1) return; // Silent skip if ACTIVE DA already exists
            }

            using (SqlCommand cmd = new SqlCommand("usp_SaveDailyExpense", con, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@ActivityId", SqlDbType.Int).Value = activityId;
                cmd.Parameters.Add("@ExpenseType", SqlDbType.NVarChar, 50).Value = "DA";
                cmd.Parameters.Add("@ExpenseDate", SqlDbType.Date).Value = expenseDate.Date;
                cmd.Parameters.Add("@Amount", SqlDbType.Decimal).Value = amount;
                cmd.Parameters["@Amount"].Precision = 18;
                cmd.Parameters["@Amount"].Scale = 2;

                string autoDesc = "Auto-applied " + (policyMode != null ? policyMode : "DA");
                cmd.Parameters.Add("@Purpose", SqlDbType.NVarChar, 250).Value = autoDesc.Substring(0, Math.Min(250, autoDesc.Length));

                cmd.Parameters.Add("@BillNo", SqlDbType.NVarChar, 100).Value = DBNull.Value;
                cmd.Parameters.Add("@AttachmentFileName", SqlDbType.NVarChar, 255).Value = DBNull.Value;
                cmd.Parameters.Add("@AttachmentPath", SqlDbType.NVarChar, 500).Value = DBNull.Value;
                cmd.Parameters.Add("@AttachmentContent", SqlDbType.VarBinary, -1).Value = new byte[0];
                cmd.Parameters.Add("@CreatedBy", SqlDbType.NVarChar, 100).Value = (object)createdBy ?? DBNull.Value;
                cmd.Parameters.Add("@PolicyMode", SqlDbType.NVarChar, 50).Value = (object)policyMode ?? DBNull.Value;
                cmd.Parameters.Add("@PolicySnapshot", SqlDbType.NVarChar, -1).Value = (object)policySnapshot ?? DBNull.Value;
                cmd.Parameters.Add("@IsAutoDA", SqlDbType.Bit).Value = 1;
                cmd.Parameters.Add("@CreatedByIP", SqlDbType.NVarChar, 50).Value = (object)Request.UserHostAddress ?? DBNull.Value;
                cmd.Parameters.Add("@Source", SqlDbType.NVarChar, 50).Value = "AUTO-DA";

                var pErr = new SqlParameter("@ErrCode", SqlDbType.Int) { Direction = ParameterDirection.Output };
                var pMsg = new SqlParameter("@ErrMessage", SqlDbType.NVarChar, 400) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(pErr);
                cmd.Parameters.Add(pMsg);

                cmd.ExecuteNonQuery();

                int errCode = pErr.Value != DBNull.Value ? Convert.ToInt32(pErr.Value) : 0;
                string errMsg = pMsg.Value != DBNull.Value ? pMsg.Value.ToString() : null;

                if (errCode != 0)
                    throw new Exception(string.Format("usp_SaveDailyExpense returned error {0}: {1}", errCode, errMsg));
            }
        }

        private static decimal? TryParseDecimalOrNull(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            decimal d;
            if (decimal.TryParse(s.Trim(), out d)) return d;
            return null;
        }

        private string ResolveHiddenFieldValue(string hfId)
        {
            try
            {
                var hf = FindControlRecursive(this.Page, hfId) as HiddenField;
                if (hf != null) return hf.Value;
                var ctrl = this.Page.Request.Form[hfId];
                return ctrl;
            }
            catch { return null; }
        }

        private static string EscapeJson(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private Control FindControlRecursive(Control root, string id)
        {
            if (root == null) return null;
            var c = root.FindControl(id);
            if (c != null) return c;
            foreach (Control child in root.Controls)
            {
                var found = FindControlRecursive(child, id);
                if (found != null) return found;
            }
            return null;
        }

        private bool ValidateExpenseBeforeSave(out string errorMessage)
        {
            errorMessage = null;
            string expenseType = DDL_ExpenseType.SelectedValue;

            decimal resolvedHotelCap = 0M, resolvedDA = 0M, resolvedMaxKM = 0M, resolvedRatePerKm = 0M;
            CultureInfo ci = CultureInfo.GetCultureInfo("en-IN");

            SanitizeDecimal(lblResolvedHotelCap.Text, ci, out resolvedHotelCap);
            SanitizeDecimal(lblResolvedDA.Text, ci, out resolvedDA);
            SanitizeDecimal(lblResolvedMaxKM.Text, ci, out resolvedMaxKM);
            SanitizeDecimal(lblResolvedRatePerKm.Text, ci, out resolvedRatePerKm);

            DateTime dt; decimal amt;

            if (expenseType == "Hotel")
            {
                DateTime fromDate, toDate;
                if (!DateTime.TryParse(TXT_HotelFromDate.Text, out fromDate)) { errorMessage = "Please enter a valid 'Stay From' date."; return false; }
                if (!DateTime.TryParse(TXT_HotelToDate.Text, out toDate)) { errorMessage = "Please enter a valid 'Stay To' date."; return false; }
                if (toDate < fromDate) { errorMessage = "'Stay To' cannot be before 'Stay From'."; return false; }
                if (!decimal.TryParse(TXT_HotelAmount.Text, out amt) || amt <= 0) { errorMessage = "Please enter a valid hotel amount."; return false; }

                int nights = (int)(toDate.Date - fromDate.Date).TotalDays;
                if (nights <= 0) nights = 1;

                if (resolvedHotelCap > 0)
                {
                    decimal allowed = resolvedHotelCap * nights;
                    if (amt > allowed) { errorMessage = string.Format("Hotel amount exceeds allowed cap. Allowed upto {0} per night x {1} night(s) = {2:N2}.", resolvedHotelCap, nights, allowed); return false; }
                }
            }
            else if (expenseType == "DA")
            {
                if (!decimal.TryParse(TXT_DAAmount.Text, out amt) || amt <= 0) { errorMessage = "Please enter a valid DA amount."; return false; }
                if (resolvedDA > 0 && amt > resolvedDA) { errorMessage = string.Format("DA amount exceeds policy limit ({0:N2}).", resolvedDA); return false; }
            }
            else if (expenseType == "Travel")
            {
                if (!DateTime.TryParse(TXT_TravelDate.Text, out dt)) { errorMessage = "Please enter a valid Travel Date."; return false; }
                if (string.IsNullOrWhiteSpace(TXT_FromPlace.Text) || string.IsNullOrWhiteSpace(TXT_ToPlace.Text)) { errorMessage = "Please enter From and To places for travel."; return false; }
                if (!decimal.TryParse(TXT_TravelAmount.Text, out amt) || amt <= 0) { errorMessage = "Please enter a valid Travel amount."; return false; }
                if (CHK_TravelInvoice.Checked && string.IsNullOrWhiteSpace(TXT_TravelInvoiceNo.Text)) { errorMessage = "Please enter Invoice No when 'With Invoice' is checked."; return false; }
            }
            return true;
        }

        protected DataTable LoadExpenseForActivity(string activityIdStr)
        {
            int activityId = 0;
            int.TryParse(activityIdStr, out activityId);

            DataTable dt = new DataTable();
            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand(@"SELECT ExpenseId, ActivityId, ExpenseType, ExpenseDate, FromDate, ToDate, BillNo, Amount, Currency, Purpose, AttachmentFileName, AttachmentPath, TravelType, FromPlace, ToPlace, TravelDate, PolicyMode, PolicySnapshot, IsAutoDA, ClaimStatus FROM dbo.SalesDailyExpense WHERE ActivityId = @ActivityId AND IsDeleted = 0 ORDER BY ExpenseDate DESC, ExpenseId DESC", con))
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.Add(new SqlParameter("@ActivityId", SqlDbType.Int) { Value = activityId });
                da.Fill(dt);
            }

            if (HF_SnapPolicyMode != null) HF_SnapPolicyMode.Value = "";
            if (HF_SnapResolvedDA != null) HF_SnapResolvedDA.Value = "";
            if (HF_SnapHotelCap != null) HF_SnapHotelCap.Value = "";
            if (HF_SnapOutstationDA != null) HF_SnapOutstationDA.Value = "";
            if (HF_SnapOwnArrangementDA != null) HF_SnapOwnArrangementDA.Value = "";

            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow r in dt.Rows)
                {
                    if (!dt.Columns.Contains("PolicySnapshot") || r["PolicySnapshot"] == DBNull.Value) continue;
                    string json = Convert.ToString(r["PolicySnapshot"]).Trim();
                    if (string.IsNullOrWhiteSpace(json)) continue;

                    if (json.StartsWith("{"))
                    {
                        try
                        {
                            var jObj = JObject.Parse(json);
                            if (HF_SnapPolicyMode != null) HF_SnapPolicyMode.Value = (string)jObj["PolicyMode"] ?? string.Empty;
                            if (HF_SnapResolvedDA != null) HF_SnapResolvedDA.Value = (string)jObj["ResolvedDA"] ?? string.Empty;
                            if (HF_SnapHotelCap != null) HF_SnapHotelCap.Value = (string)jObj["HotelCap"] ?? string.Empty;
                            if (HF_SnapOutstationDA != null) HF_SnapOutstationDA.Value = (string)jObj["OutstationDA"] ?? string.Empty;
                            if (HF_SnapOwnArrangementDA != null) HF_SnapOwnArrangementDA.Value = (string)jObj["OwnArrangementDA"] ?? string.Empty;
                            break;
                        }
                        catch (Exception ex) { System.Diagnostics.Trace.TraceWarning("Parse fail: " + ex.Message); }
                    }
                }

                // Ensure HF_SnapPolicyMode is accurately set from the DB column if JSON fails
                if (HF_SnapPolicyMode != null && string.IsNullOrWhiteSpace(HF_SnapPolicyMode.Value) && dt.Columns.Contains("PolicyMode") && dt.Rows[0]["PolicyMode"] != DBNull.Value)
                {
                    HF_SnapPolicyMode.Value = dt.Rows[0]["PolicyMode"].ToString().Trim();
                }
            }
            else
            {
                if (HF_SnapPolicyMode != null) HF_SnapPolicyMode.Value = "HQ";
            }

            rptExpenses.DataSource = dt;
            rptExpenses.DataBind();

            UpdateExpenseTotals(dt);
            if (HF_ActivityId != null) HF_ActivityId.Value = activityId.ToString();

            // Reset standard fields
            if (DDL_TravelType != null) DDL_TravelType.SelectedIndex = 0;
            if (TXT_FromPlace != null) TXT_FromPlace.Text = "";
            if (TXT_ToPlace != null) TXT_ToPlace.Text = "";
            if (TXT_TravelPurpose != null) TXT_TravelPurpose.Text = "";
            if (TXT_TravelInvoiceNo != null) TXT_TravelInvoiceNo.Text = "";
            if (CHK_TravelInvoice != null) CHK_TravelInvoice.Checked = false;
            if (TXT_DAAmount != null) TXT_DAAmount.Text = "";
            if (TXT_HotelAmount != null) TXT_HotelAmount.Text = "";
            if (TXT_HotelBillNo != null) TXT_HotelBillNo.Text = "";
            if (TXT_HotelPurpose != null) TXT_HotelPurpose.Text = "";

            // NEW: Reset Stationary fields so they don't persist between modal opens
            if (TXT_StationaryAmount != null) TXT_StationaryAmount.Text = "";
            if (TXT_StationaryBillNo != null) TXT_StationaryBillNo.Text = "";
            if (TXT_StationaryPurpose != null) TXT_StationaryPurpose.Text = "";

            if (activityId > 0)
            {
                BindRouteData(activityId);
            }

            return dt;
        }

        protected DataTable LoadExpenseForActivity_OLD(string activityIdStr)
        {
            int activityId = 0;
            int.TryParse(activityIdStr, out activityId);

            DataTable dt = new DataTable();
            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand(@"SELECT ExpenseId, ActivityId, ExpenseType, ExpenseDate, FromDate, ToDate, BillNo, Amount, Currency, Purpose, AttachmentFileName, AttachmentPath, TravelType, FromPlace, ToPlace, TravelDate, PolicyMode, PolicySnapshot, IsAutoDA, ClaimStatus FROM dbo.SalesDailyExpense WHERE ActivityId = @ActivityId AND IsDeleted = 0 ORDER BY ExpenseDate DESC, ExpenseId DESC", con))
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.Add(new SqlParameter("@ActivityId", SqlDbType.Int) { Value = activityId });
                da.Fill(dt);
            }

            if (HF_SnapPolicyMode != null) HF_SnapPolicyMode.Value = "";
            if (HF_SnapResolvedDA != null) HF_SnapResolvedDA.Value = "";
            if (HF_SnapHotelCap != null) HF_SnapHotelCap.Value = "";
            if (HF_SnapOutstationDA != null) HF_SnapOutstationDA.Value = "";
            if (HF_SnapOwnArrangementDA != null) HF_SnapOwnArrangementDA.Value = "";

            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow r in dt.Rows)
                {
                    if (!dt.Columns.Contains("PolicySnapshot") || r["PolicySnapshot"] == DBNull.Value) continue;
                    string json = Convert.ToString(r["PolicySnapshot"]).Trim();
                    if (string.IsNullOrWhiteSpace(json)) continue;

                    if (json.StartsWith("{"))
                    {
                        try
                        {
                            var jObj = JObject.Parse(json);
                            if (HF_SnapPolicyMode != null) HF_SnapPolicyMode.Value = (string)jObj["PolicyMode"] ?? string.Empty;
                            if (HF_SnapResolvedDA != null) HF_SnapResolvedDA.Value = (string)jObj["ResolvedDA"] ?? string.Empty;
                            if (HF_SnapHotelCap != null) HF_SnapHotelCap.Value = (string)jObj["HotelCap"] ?? string.Empty;
                            if (HF_SnapOutstationDA != null) HF_SnapOutstationDA.Value = (string)jObj["OutstationDA"] ?? string.Empty;
                            if (HF_SnapOwnArrangementDA != null) HF_SnapOwnArrangementDA.Value = (string)jObj["OwnArrangementDA"] ?? string.Empty;
                            break;
                        }
                        catch (Exception ex) { System.Diagnostics.Trace.TraceWarning("Parse fail: " + ex.Message); }
                    }
                }

                // Ensure HF_SnapPolicyMode is accurately set from the DB column if JSON fails
                if (HF_SnapPolicyMode != null && string.IsNullOrWhiteSpace(HF_SnapPolicyMode.Value) && dt.Columns.Contains("PolicyMode") && dt.Rows[0]["PolicyMode"] != DBNull.Value)
                {
                    HF_SnapPolicyMode.Value = dt.Rows[0]["PolicyMode"].ToString().Trim();
                }
            }
            else
            {
                if (HF_SnapPolicyMode != null) HF_SnapPolicyMode.Value = "HQ";
            }

            rptExpenses.DataSource = dt;
            rptExpenses.DataBind();

            UpdateExpenseTotals(dt);
            if (HF_ActivityId != null) HF_ActivityId.Value = activityId.ToString();

            if (DDL_TravelType != null) DDL_TravelType.SelectedIndex = 0;
            if (TXT_FromPlace != null) TXT_FromPlace.Text = "";
            if (TXT_ToPlace != null) TXT_ToPlace.Text = "";
            if (TXT_TravelPurpose != null) TXT_TravelPurpose.Text = "";
            if (TXT_TravelInvoiceNo != null) TXT_TravelInvoiceNo.Text = "";
            if (CHK_TravelInvoice != null) CHK_TravelInvoice.Checked = false;
            if (TXT_DAAmount != null) TXT_DAAmount.Text = "";
            if (TXT_HotelAmount != null) TXT_HotelAmount.Text = "";
            if (TXT_HotelBillNo != null) TXT_HotelBillNo.Text = "";
            if (TXT_HotelPurpose != null) TXT_HotelPurpose.Text = "";

            if (activityId > 0)
            {
                BindRouteData(activityId);
            }

            return dt;
        }

        private void BindRouteData(int activityId)
        {
            using (SqlConnection con =
                new SqlConnection(connString))
            {
                string sql = @"
        SELECT
            RouteOriginName,
            RouteDestinationName,
            RouteWaypointsJSON,
            TravelMode,
            TotalDistanceKM
        FROM dbo.SalesDailyActivity
        WHERE ActivityId=@ActivityId";

                using (SqlCommand cmd =
                    new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue(
                        "@ActivityId",
                        activityId);

                    con.Open();

                    using (SqlDataReader dr =
                        cmd.ExecuteReader())
                    {
                        if (!dr.Read())
                            return;

                        string origin =
                            dr["RouteOriginName"] == DBNull.Value
                            ? ""
                            : dr["RouteOriginName"].ToString();

                        string destination =
                            dr["RouteDestinationName"] == DBNull.Value
                            ? ""
                            : dr["RouteDestinationName"].ToString();

                        string waypoints =
                            dr["RouteWaypointsJSON"] == DBNull.Value
                            ? ""
                            : dr["RouteWaypointsJSON"].ToString();

                        string travelMode =
                            dr["TravelMode"] == DBNull.Value
                            ? "driving"
                            : dr["TravelMode"].ToString();

                        string distance =
                            dr["TotalDistanceKM"] == DBNull.Value
                            ? ""
                            : Convert.ToDecimal(
                                dr["TotalDistanceKM"])
                                .ToString("0.00");

                        // =====================================
                        // DA Section
                        // =====================================

                        TXT_DAFromPlace.Text = origin;
                        TXT_DAToPlace.Text = destination;

                        // =====================================
                        // Hidden Fields
                        // =====================================

                        HF_RouteOrigin.Value = origin;
                        HF_RouteDestination.Value = destination;
                        HF_RouteWaypoints.Value = waypoints;
                        HF_RouteTravelMode.Value = travelMode;
                        HF_RouteDistance.Value = distance;

                        // =====================================
                        // Route Summary Panel
                        // =====================================

                        RegisterRouteSummaryScript(
                            origin,
                            destination,
                            waypoints,
                            distance);
                    }
                }
            }
        }


        private void RegisterRouteSummaryScript(
    string origin,
    string destination,
    string waypointsJson,
    string distance)
        {
            int stopCount = 0;

            try
            {
                if (!string.IsNullOrWhiteSpace(
                    waypointsJson))
                {
                    JArray arr =
                        JArray.Parse(
                            waypointsJson);

                    stopCount = arr.Count;
                }
            }
            catch
            {
            }

            string js = string.Format(@"

        $('#spRouteOrigin')
            .text('{0}');

        $('#spRouteDestination')
            .text('{1}');

        $('#spWaypointCount')
            .text('{2}');

        $('#spRouteDistance')
            .text('{3}');

        $('#routeSummaryPanel')
            .show();

    ",
            origin.Replace("'", "\\'"),
            destination.Replace("'", "\\'"),
            stopCount,
            distance);

            ScriptManager.RegisterStartupScript(
                this,
                GetType(),
                "RestoreRouteSummary",
                js,
                true);
        }

        private void UpdateExpenseTotals(DataTable dt)
        {
            // Added stationaryTotal to segregate the math
            decimal daTotal = 0m, hotelTotal = 0m, otherTotal = 0m, stationaryTotal = 0m;

            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow r in dt.Rows)
                {
                    decimal amt = 0m;
                    if (r["Amount"] != DBNull.Value) amt = Convert.ToDecimal(r["Amount"]);
                    string type = r["ExpenseType"] != null ? r["ExpenseType"].ToString().Trim() : "";

                    if (string.Equals(type, "DA", StringComparison.OrdinalIgnoreCase))
                        daTotal += amt;
                    else if (string.Equals(type, "Hotel", StringComparison.OrdinalIgnoreCase))
                        hotelTotal += amt;
                    else if (string.Equals(type, "Stationary", StringComparison.OrdinalIgnoreCase))
                        stationaryTotal += amt; // Caught and aggregated separately
                    else
                        otherTotal += amt; // Travel and any historical legacy types
                }
            }

            if (lblTotalDA != null) lblTotalDA.Text = daTotal.ToString("0.##");
            if (lblTotalHotel != null) lblTotalHotel.Text = hotelTotal.ToString("0.##");
            if (lblTotalOther != null) lblTotalOther.Text = otherTotal.ToString("0.##");
            if (lblTotalStationary != null) lblTotalStationary.Text = stationaryTotal.ToString("0.##");

            if (lblOverallTotal != null)
                lblOverallTotal.Text = (daTotal + hotelTotal + otherTotal + stationaryTotal).ToString("0.##");
        }

        private string GetCurrentUserLoginId(bool redirectIfMissing = true)
        {
            try
            {
                if (Session != null && Session["WORKMAN"] != null)
                {
                    string s = Session["WORKMAN"].ToString();
                    if (!string.IsNullOrWhiteSpace(s)) return s;
                }
                if (redirectIfMissing)
                {
                    string loginUrl = ResolveUrl("~/Login.aspx");
                    string returnUrl = HttpContext.Current.Request.RawUrl;
                    HttpContext.Current.Response.Redirect(loginUrl + "?ReturnUrl=" + HttpUtility.UrlEncode(returnUrl), true);
                }
            }
            catch (Exception ex)
            {
                string safeMsg = ex.Message.Replace("'", "\\'");
                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyError6", "showPNotify('Error', '" + safeMsg + "', 'error');", true);
            }
            return null;
        }

        protected void rptExpenses_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "DeleteExpense")
            {
                var arg = e.CommandArgument != null ? e.CommandArgument.ToString() : "";
                var parts = arg.Split(new[] { '|' }, 3);

                int expenseId;
                if (parts.Length < 1 || !int.TryParse(parts[0], out expenseId))
                {
                    if (LBL_ExpenseMsg != null)
                    {
                        LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                        LBL_ExpenseMsg.Text = "Invalid expense id.";
                    }
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }

                string incomingAttachmentPath = parts.Length > 1 ? (string.IsNullOrWhiteSpace(parts[1]) ? null : parts[1]) : null;
                int activityIdFromArg = 0; if (parts.Length > 2) int.TryParse(parts[2], out activityIdFromArg);

                // ====================================================================
                // SECURITY GATE: Prevent deleting when the day is locked for editing
                // ====================================================================
                if (!IsActivityEditableForExpenseDelete(activityIdFromArg))
                {
                    string safeErr = "Cannot delete this expense. This day is not open for editing.";
                    ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyLockErrDel", "showPNotify('Locked', '" + safeErr + "', 'error');", true);
                    return;
                }
                // ====================================================================

                try
                {
                    using (SqlConnection con = new SqlConnection(connString))
                    {
                        con.Open();
                        string lookupSql = @"SELECT e.ExpenseId, e.ActivityId, e.ExpenseType, e.ExpenseDate, e.FromDate, e.ToDate, e.AttachmentPath, e.IsAutoDA, e.ClaimStatus AS ExpenseClaimStatus, a.ClaimStatus AS ActivityClaimStatus, e.ExpenseSeq, e.CreatedDateTime FROM dbo.SalesDailyExpense e LEFT JOIN dbo.SalesDailyActivity a ON e.ActivityId = a.ActivityId WHERE e.ExpenseId = @ExpenseId";
                        int activityId = 0; string expenseType = null; DateTime? expDate = null, fromDate = null, toDate = null;
                        string attachmentPath = incomingAttachmentPath; bool isAutoRow = false; string expenseClaimStatus = null;

                        using (var lookup = new SqlCommand(lookupSql, con))
                        {
                            lookup.Parameters.Add(new SqlParameter("@ExpenseId", SqlDbType.Int) { Value = expenseId });
                            using (var rdr = lookup.ExecuteReader())
                            {
                                if (!rdr.Read())
                                {
                                    if (LBL_ExpenseMsg != null)
                                    {
                                        LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                                        LBL_ExpenseMsg.Text = "Expense not found.";
                                    }
                                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                                    return;
                                }
                                activityId = rdr["ActivityId"] != DBNull.Value ? Convert.ToInt32(rdr["ActivityId"]) : 0;
                                expenseType = rdr["ExpenseType"] != DBNull.Value ? rdr["ExpenseType"].ToString() : null;
                                if (rdr["ExpenseDate"] != DBNull.Value) expDate = Convert.ToDateTime(rdr["ExpenseDate"]);
                                if (rdr["FromDate"] != DBNull.Value) fromDate = Convert.ToDateTime(rdr["FromDate"]);
                                if (rdr["ToDate"] != DBNull.Value) toDate = Convert.ToDateTime(rdr["ToDate"]);
                                if (string.IsNullOrWhiteSpace(attachmentPath) && rdr["AttachmentPath"] != DBNull.Value) attachmentPath = rdr["AttachmentPath"].ToString();
                                isAutoRow = rdr["IsAutoDA"] != DBNull.Value && Convert.ToInt32(rdr["IsAutoDA"]) == 1;
                                expenseClaimStatus = rdr["ExpenseClaimStatus"] != DBNull.Value ? rdr["ExpenseClaimStatus"].ToString() : null;
                            }
                        }

                        if (!IsRemovableExpenseStatus(expenseClaimStatus))
                        {
                            if (LBL_ExpenseMsg != null)
                            {
                                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Orange;
                                LBL_ExpenseMsg.Text = "Only draft or returned expenses can be removed.";
                            }
                            ScriptManager.RegisterStartupScript(this, GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                            return;
                        }

                        if (isAutoRow)
                        {
                            if (LBL_ExpenseMsg != null)
                            {
                                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Orange;
                                LBL_ExpenseMsg.Text = "Cannot delete system-generated (auto) expense directly.";
                            }
                            ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                            return;
                        }

                        DateTime matchFrom = DateTime.MinValue, matchTo = DateTime.MinValue;
                        if (expenseType != null && expenseType.Equals("Hotel", StringComparison.OrdinalIgnoreCase))
                        {
                            if (fromDate.HasValue && toDate.HasValue) { matchFrom = fromDate.Value.Date; matchTo = toDate.Value.Date; }
                            else if (expDate.HasValue) matchFrom = matchTo = expDate.Value.Date;
                        }
                        else if (expenseType != null && expenseType.Equals("Travel", StringComparison.OrdinalIgnoreCase))
                        {
                            if (expDate.HasValue) matchFrom = matchTo = expDate.Value.Date;
                        }
                        else
                        {
                            if (expDate.HasValue) matchFrom = matchTo = expDate.Value.Date;
                        }

                        using (SqlTransaction tx = con.BeginTransaction())
                        {
                            try
                            {
                                bool isFirstForType = false;
                                if (matchFrom != DateTime.MinValue)
                                {
                                    string firstSql = @"SELECT TOP 1 ExpenseId FROM dbo.SalesDailyExpense WITH (UPDLOCK, HOLDLOCK) WHERE ActivityId = @ActivityId AND IsDeleted = 0 AND LOWER(ISNULL(ExpenseType,'')) = LOWER(ISNULL(@ExpenseType,'')) AND CAST(ISNULL(ExpenseDate, GETDATE()) AS DATE) = @MatchDate ORDER BY CASE WHEN ExpenseSeq IS NULL THEN 2147483647 ELSE ExpenseSeq END ASC, ISNULL(CreatedDateTime, '1900-01-01') ASC, ExpenseId ASC";
                                    using (var firstCmd = new SqlCommand(firstSql, con, tx))
                                    {
                                        firstCmd.Parameters.Add(new SqlParameter("@ActivityId", SqlDbType.Int) { Value = activityId });
                                        firstCmd.Parameters.Add(new SqlParameter("@ExpenseType", SqlDbType.NVarChar, 50) { Value = expenseType ?? (object)DBNull.Value });
                                        firstCmd.Parameters.Add(new SqlParameter("@MatchDate", SqlDbType.Date) { Value = matchFrom });
                                        object firstObj = firstCmd.ExecuteScalar();
                                        if (firstObj != null && firstObj != DBNull.Value) isFirstForType = (Convert.ToInt32(firstObj) == expenseId);
                                        else isFirstForType = true;
                                    }
                                }

                                var autoIds = new List<int>();
                                if (isFirstForType && matchFrom != DateTime.MinValue)
                                {
                                    // Make sure to add AND IsDeleted = 0 to this query as well!
                                    string findAutoSql = @"SELECT ExpenseId FROM dbo.SalesDailyExpense WITH (UPDLOCK, HOLDLOCK) WHERE ActivityId = @ActivityId AND ExpenseType = 'DA' AND IsAutoDA = 1 AND ClaimStatus IN ('Draft', 'Returned') AND IsDeleted = 0 AND CAST(ExpenseDate AS date) BETWEEN @FromDate AND @ToDate";
                                    using (var findCmd = new SqlCommand(findAutoSql, con, tx))
                                    {
                                        findCmd.Parameters.AddWithValue("@ActivityId", activityId);
                                        findCmd.Parameters.AddWithValue("@FromDate", matchFrom);
                                        findCmd.Parameters.AddWithValue("@ToDate", matchTo);
                                        using (var rdr = findCmd.ExecuteReader()) { while (rdr.Read()) autoIds.Add(Convert.ToInt32(rdr["ExpenseId"])); }
                                    }
                                }

                                using (var del = new SqlCommand(SoftDeleteExpenseSql, con, tx))
                                {
                                    del.Parameters.AddWithValue("@ExpenseId", expenseId);
                                    AddSoftDeleteParameters(del, Session["WORKMAN"].ToString(), "User removed editable expense");
                                    if (del.ExecuteNonQuery() <= 0)
                                    {
                                        tx.Rollback();
                                        if (LBL_ExpenseMsg != null)
                                        {
                                            LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                                            LBL_ExpenseMsg.Text = "Only draft or returned expenses can be removed.";
                                        }
                                        ScriptManager.RegisterStartupScript(this, GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                                        return;
                                    }
                                }

                                if (autoIds.Count > 0)
                                {
                                    string delAutoSql = BuildSoftDeleteExpenseBatchSql(autoIds);
                                    using (var delAuto = new SqlCommand(delAutoSql, con, tx))
                                    {
                                        AddSoftDeleteParameters(delAuto, Session["WORKMAN"].ToString(), "Auto-DA reversed due to primary expense deletion");
                                        delAuto.ExecuteNonQuery();
                                    }
                                }

                                tx.Commit();
                                DataTable dtAfterDel = LoadExpenseForActivity(activityId.ToString());
                                Session.Remove(GetCurrentSessionKey());
                                BindGrid(false);

                                string currentMode = "";
                                if (dtAfterDel != null && dtAfterDel.Rows.Count > 0 && dtAfterDel.Columns.Contains("PolicyMode") && dtAfterDel.Rows[0]["PolicyMode"] != DBNull.Value)
                                    currentMode = dtAfterDel.Rows[0]["PolicyMode"].ToString().Trim();
                                if (string.IsNullOrWhiteSpace(currentMode) && HF_ResolvedPolicyMode != null) currentMode = HF_ResolvedPolicyMode.Value.Trim();
                                if (string.IsNullOrWhiteSpace(currentMode)) currentMode = "HQ";

                                if (LBL_ExpenseMsg != null)
                                {
                                    LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Green;
                                    LBL_ExpenseMsg.Text = "Expense deleted.";
                                }

                                string scriptDel = string.Format("if(typeof openExpenseModalSafe === 'function') openExpenseModalSafe('{0}', '{1}'); else $('#expenseModal').modal('show');", activityId, currentMode.Replace("'", "\\'"));
                                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpenseDeleted", scriptDel, true);
                            }
                            catch (Exception ex)
                            {
                                try { tx.Rollback(); } catch { }
                                if (LBL_ExpenseMsg != null)
                                {
                                    LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                                    LBL_ExpenseMsg.Text = ex.Message;
                                }
                                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                                return;
                            }
                        }
                    }
                }
                catch (SqlException sqx)
                {
                    if (LBL_ExpenseMsg != null)
                    {
                        LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                        LBL_ExpenseMsg.Text = sqx.Message;
                    }
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                }
                catch (Exception ex)
                {
                    if (LBL_ExpenseMsg != null)
                    {
                        LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                        LBL_ExpenseMsg.Text = ex.Message;
                    }
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                }
            }
        }

        protected void rptExpenses_ItemCommand_OLD(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "DeleteExpense")
            {
                var arg = e.CommandArgument != null ? e.CommandArgument.ToString() : "";
                var parts = arg.Split(new[] { '|' }, 3);

                int expenseId;
                if (parts.Length < 1 || !int.TryParse(parts[0], out expenseId))
                {
                    if (LBL_ExpenseMsg != null)
                    {
                        LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                        LBL_ExpenseMsg.Text = "Invalid expense id.";
                    }
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }

                string incomingAttachmentPath = parts.Length > 1 ? (string.IsNullOrWhiteSpace(parts[1]) ? null : parts[1]) : null;
                int activityIdFromArg = 0; if (parts.Length > 2) int.TryParse(parts[2], out activityIdFromArg);

                try
                {
                    using (SqlConnection con = new SqlConnection(connString))
                    {
                        con.Open();
                        string lookupSql = @"SELECT e.ExpenseId, e.ActivityId, e.ExpenseType, e.ExpenseDate, e.FromDate, e.ToDate, e.AttachmentPath, e.IsAutoDA, e.ClaimStatus AS ExpenseClaimStatus, a.ClaimStatus AS ActivityClaimStatus, e.ExpenseSeq, e.CreatedDateTime FROM dbo.SalesDailyExpense e LEFT JOIN dbo.SalesDailyActivity a ON e.ActivityId = a.ActivityId WHERE e.ExpenseId = @ExpenseId";
                        int activityId = 0; string expenseType = null; DateTime? expDate = null, fromDate = null, toDate = null;
                        string attachmentPath = incomingAttachmentPath; bool isAutoRow = false; string expenseClaimStatus = null;

                        using (var lookup = new SqlCommand(lookupSql, con))
                        {
                            lookup.Parameters.Add(new SqlParameter("@ExpenseId", SqlDbType.Int) { Value = expenseId });
                            using (var rdr = lookup.ExecuteReader())
                            {
                                if (!rdr.Read())
                                {
                                    if (LBL_ExpenseMsg != null)
                                    {
                                        LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                                        LBL_ExpenseMsg.Text = "Expense not found.";
                                    }
                                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                                    return;
                                }
                                activityId = rdr["ActivityId"] != DBNull.Value ? Convert.ToInt32(rdr["ActivityId"]) : 0;
                                expenseType = rdr["ExpenseType"] != DBNull.Value ? rdr["ExpenseType"].ToString() : null;
                                if (rdr["ExpenseDate"] != DBNull.Value) expDate = Convert.ToDateTime(rdr["ExpenseDate"]);
                                if (rdr["FromDate"] != DBNull.Value) fromDate = Convert.ToDateTime(rdr["FromDate"]);
                                if (rdr["ToDate"] != DBNull.Value) toDate = Convert.ToDateTime(rdr["ToDate"]);
                                if (string.IsNullOrWhiteSpace(attachmentPath) && rdr["AttachmentPath"] != DBNull.Value) attachmentPath = rdr["AttachmentPath"].ToString();
                                isAutoRow = rdr["IsAutoDA"] != DBNull.Value && Convert.ToInt32(rdr["IsAutoDA"]) == 1;
                                expenseClaimStatus = rdr["ExpenseClaimStatus"] != DBNull.Value ? rdr["ExpenseClaimStatus"].ToString() : null;
                            }
                        }

                        if (!IsRemovableExpenseStatus(expenseClaimStatus))
                        {
                            if (LBL_ExpenseMsg != null)
                            {
                                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Orange;
                                LBL_ExpenseMsg.Text = "Only draft or returned expenses can be removed.";
                            }
                            ScriptManager.RegisterStartupScript(this, GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                            return;
                        }

                        if (isAutoRow)
                        {
                            if (LBL_ExpenseMsg != null)
                            {
                                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Orange;
                                LBL_ExpenseMsg.Text = "Cannot delete system-generated (auto) expense directly.";
                            }
                            ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                            return;
                        }

                        DateTime matchFrom = DateTime.MinValue, matchTo = DateTime.MinValue;
                        if (expenseType != null && expenseType.Equals("Hotel", StringComparison.OrdinalIgnoreCase))
                        {
                            if (fromDate.HasValue && toDate.HasValue) { matchFrom = fromDate.Value.Date; matchTo = toDate.Value.Date; }
                            else if (expDate.HasValue) matchFrom = matchTo = expDate.Value.Date;
                        }
                        else if (expenseType != null && expenseType.Equals("Travel", StringComparison.OrdinalIgnoreCase))
                        {
                            if (expDate.HasValue) matchFrom = matchTo = expDate.Value.Date;
                        }
                        else
                        {
                            if (expDate.HasValue) matchFrom = matchTo = expDate.Value.Date;
                        }

                        using (SqlTransaction tx = con.BeginTransaction())
                        {
                            try
                            {
                                bool isFirstForType = false;
                                if (matchFrom != DateTime.MinValue)
                                {
                                    string firstSql = @"SELECT TOP 1 ExpenseId FROM dbo.SalesDailyExpense WITH (UPDLOCK, HOLDLOCK) WHERE ActivityId = @ActivityId AND IsDeleted = 0 AND LOWER(ISNULL(ExpenseType,'')) = LOWER(ISNULL(@ExpenseType,'')) AND CAST(ISNULL(ExpenseDate, GETDATE()) AS DATE) = @MatchDate ORDER BY CASE WHEN ExpenseSeq IS NULL THEN 2147483647 ELSE ExpenseSeq END ASC, ISNULL(CreatedDateTime, '1900-01-01') ASC, ExpenseId ASC";
                                    using (var firstCmd = new SqlCommand(firstSql, con, tx))
                                    {
                                        firstCmd.Parameters.Add(new SqlParameter("@ActivityId", SqlDbType.Int) { Value = activityId });
                                        firstCmd.Parameters.Add(new SqlParameter("@ExpenseType", SqlDbType.NVarChar, 50) { Value = expenseType ?? (object)DBNull.Value });
                                        firstCmd.Parameters.Add(new SqlParameter("@MatchDate", SqlDbType.Date) { Value = matchFrom });
                                        object firstObj = firstCmd.ExecuteScalar();
                                        if (firstObj != null && firstObj != DBNull.Value) isFirstForType = (Convert.ToInt32(firstObj) == expenseId);
                                        else isFirstForType = true;
                                    }
                                }

                                var autoIds = new List<int>();
                                if (isFirstForType && matchFrom != DateTime.MinValue)
                                {
                                    string findAutoSql = @"SELECT ExpenseId FROM dbo.SalesDailyExpense WITH (UPDLOCK, HOLDLOCK) WHERE ActivityId = @ActivityId AND ExpenseType = 'DA' AND IsAutoDA = 1 AND ClaimStatus IN ('Draft', 'Returned') AND IsDeleted = 0 AND CAST(ExpenseDate AS date) BETWEEN @FromDate AND @ToDate";
                                    using (var findCmd = new SqlCommand(findAutoSql, con, tx))
                                    {
                                        findCmd.Parameters.AddWithValue("@ActivityId", activityId);
                                        findCmd.Parameters.AddWithValue("@FromDate", matchFrom);
                                        findCmd.Parameters.AddWithValue("@ToDate", matchTo);
                                        using (var rdr = findCmd.ExecuteReader()) { while (rdr.Read()) autoIds.Add(Convert.ToInt32(rdr["ExpenseId"])); }
                                    }
                                }

                                using (var del = new SqlCommand(SoftDeleteExpenseSql, con, tx))
                                {
                                    del.Parameters.AddWithValue("@ExpenseId", expenseId);
                                    AddSoftDeleteParameters(del, Session["WORKMAN"].ToString(), "User removed editable expense");
                                    if (del.ExecuteNonQuery() <= 0)
                                    {
                                        tx.Rollback();
                                        if (LBL_ExpenseMsg != null)
                                        {
                                            LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                                            LBL_ExpenseMsg.Text = "Only draft or returned expenses can be removed.";
                                        }
                                        ScriptManager.RegisterStartupScript(this, GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                                        return;
                                    }
                                }

                                if (autoIds.Count > 0)
                                {
                                    string delAutoSql = BuildSoftDeleteExpenseBatchSql(autoIds);
                                    using (var delAuto = new SqlCommand(delAutoSql, con, tx))
                                    {
                                        AddSoftDeleteParameters(delAuto, Session["WORKMAN"].ToString(), "Auto-DA reversed due to primary expense deletion");
                                        delAuto.ExecuteNonQuery();
                                    }
                                }

                                tx.Commit();
                                DataTable dtAfterDel = LoadExpenseForActivity(activityId.ToString());
                                Session.Remove(GetCurrentSessionKey());
                                BindGrid(false);

                                string currentMode = "";
                                if (dtAfterDel != null && dtAfterDel.Rows.Count > 0 && dtAfterDel.Columns.Contains("PolicyMode") && dtAfterDel.Rows[0]["PolicyMode"] != DBNull.Value)
                                    currentMode = dtAfterDel.Rows[0]["PolicyMode"].ToString().Trim();
                                if (string.IsNullOrWhiteSpace(currentMode) && HF_ResolvedPolicyMode != null) currentMode = HF_ResolvedPolicyMode.Value.Trim();
                                if (string.IsNullOrWhiteSpace(currentMode)) currentMode = "HQ";

                                if (LBL_ExpenseMsg != null)
                                {
                                    LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Green;
                                    LBL_ExpenseMsg.Text = "Expense deleted.";
                                }

                                string scriptDel = string.Format("if(typeof openExpenseModalSafe === 'function') openExpenseModalSafe('{0}', '{1}'); else $('#expenseModal').modal('show');", activityId, currentMode.Replace("'", "\\'"));
                                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpenseDeleted", scriptDel, true);
                            }
                            catch (Exception ex)
                            {
                                try { tx.Rollback(); } catch { }
                                if (LBL_ExpenseMsg != null)
                                {
                                    LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                                    LBL_ExpenseMsg.Text = ex.Message;
                                }
                                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                                return;
                            }
                        }
                    }
                }
                catch (SqlException sqx)
                {
                    if (LBL_ExpenseMsg != null)
                    {
                        LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                        LBL_ExpenseMsg.Text = sqx.Message;
                    }
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                }
                catch (Exception ex)
                {
                    if (LBL_ExpenseMsg != null)
                    {
                        LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                        LBL_ExpenseMsg.Text = ex.Message;
                    }
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                }
            }
        }

        public class PolicyResolved
        {
            public string Source { get; set; }
            public int? RowId { get; set; }
            public string EmployeeCode { get; set; }
            public int? RegionId { get; set; }
            public int? BranchId { get; set; }
            public int? GradeId { get; set; }
            public int? DesignationId { get; set; }
            public string PolicyJson { get; set; }
            public int? Priority { get; set; }
            public DateTime? EffectiveFrom { get; set; }
            public DateTime? EffectiveTo { get; set; }
            public bool? IsActive { get; set; }
            public int? Specificity { get; set; }
            public decimal? Resolved_MaxKM { get; set; }
            public decimal? Resolved_RatePerKm { get; set; }
            public decimal? Resolved_DA { get; set; }
            public decimal? Resolved_HotelCap { get; set; }
            public decimal? Resolved_OutstationDA { get; set; }
            public decimal? Resolved_OwnAllowance { get; set; }
            public string Resolved_Remarks { get; set; }
        }

        private PolicyResolved GetResolvedPolicy(string employeeCode, int? regionId, int? branchId, int? gradeId, int? designationId)
        {
            PolicyResolved dto = null;
            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand("dbo.usp_GetResolvedPolicy", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@EmployeeCode", SqlDbType.VarChar, 50).Value = (object)employeeCode ?? DBNull.Value;
                cmd.Parameters.Add("@RegionIdParam", SqlDbType.VarChar, 200).Value = regionId.HasValue ? (object)regionId.Value.ToString() : DBNull.Value;
                cmd.Parameters.Add("@BranchIdParam", SqlDbType.VarChar, 200).Value = branchId.HasValue ? (object)branchId.Value.ToString() : DBNull.Value;
                cmd.Parameters.Add("@GradeIdParam", SqlDbType.VarChar, 200).Value = gradeId.HasValue ? (object)gradeId.Value.ToString() : DBNull.Value;
                cmd.Parameters.Add("@DesignationIdParam", SqlDbType.VarChar, 200).Value = designationId.HasValue ? (object)designationId.Value.ToString() : DBNull.Value;

                con.Open();
                using (var reader = cmd.ExecuteReader(CommandBehavior.SingleResult))
                {
                    if (reader.Read())
                    {
                        dto = new PolicyResolved();
                        Func<IDataRecord, string, bool> ColExists = (rec, colName) => { try { return rec.GetOrdinal(colName) >= 0; } catch (IndexOutOfRangeException) { return false; } };

                        if (ColExists(reader, "Source")) dto.Source = reader["Source"] as string;
                        if (ColExists(reader, "PolicyId")) dto.RowId = reader["PolicyId"] != DBNull.Value ? (int?)Convert.ToInt32(reader["PolicyId"]) : null;
                        if (ColExists(reader, "EmployeeCode")) dto.EmployeeCode = reader["EmployeeCode"] != DBNull.Value ? reader["EmployeeCode"].ToString() : null;
                        if (ColExists(reader, "RegionId")) dto.RegionId = reader["RegionId"] != DBNull.Value ? (int?)Convert.ToInt32(reader["RegionId"]) : null;
                        if (ColExists(reader, "BranchId")) dto.BranchId = reader["BranchId"] != DBNull.Value ? (int?)Convert.ToInt32(reader["BranchId"]) : null;
                        if (ColExists(reader, "GradeId")) dto.GradeId = reader["GradeId"] != DBNull.Value ? (int?)Convert.ToInt32(reader["GradeId"]) : null;
                        if (ColExists(reader, "DesignationId")) dto.DesignationId = reader["DesignationId"] != DBNull.Value ? (int?)Convert.ToInt32(reader["DesignationId"]) : null;
                        if (ColExists(reader, "PolicyJson")) dto.PolicyJson = reader["PolicyJson"] != DBNull.Value ? reader["PolicyJson"].ToString() : null;
                        if (ColExists(reader, "Priority")) dto.Priority = reader["Priority"] != DBNull.Value ? (int?)Convert.ToInt32(reader["Priority"]) : null;
                        if (ColExists(reader, "EffectiveFrom")) dto.EffectiveFrom = reader["EffectiveFrom"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["EffectiveFrom"]) : null;
                        if (ColExists(reader, "EffectiveTo")) dto.EffectiveTo = reader["EffectiveTo"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["EffectiveTo"]) : null;
                        if (ColExists(reader, "IsActive")) dto.IsActive = reader["IsActive"] != DBNull.Value ? (bool?)Convert.ToBoolean(reader["IsActive"]) : null;
                        if (ColExists(reader, "Specificity")) dto.Specificity = reader["Specificity"] != DBNull.Value ? (int?)Convert.ToInt32(reader["Specificity"]) : null;
                        if (ColExists(reader, "Resolved_MaxKM")) dto.Resolved_MaxKM = reader["Resolved_MaxKM"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_MaxKM"]) : null;
                        if (ColExists(reader, "Resolved_RatePerKm")) dto.Resolved_RatePerKm = reader["Resolved_RatePerKm"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_RatePerKm"]) : null;
                        if (ColExists(reader, "Resolved_DA")) dto.Resolved_DA = reader["Resolved_DA"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_DA"]) : null;
                        if (ColExists(reader, "Resolved_HotelCap")) dto.Resolved_HotelCap = reader["Resolved_HotelCap"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_HotelCap"]) : null;
                        if (ColExists(reader, "Resolved_OutstationDA")) dto.Resolved_OutstationDA = reader["Resolved_OutstationDA"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_OutstationDA"]) : null;
                        if (ColExists(reader, "Resolved_OwnAllowance")) dto.Resolved_OwnAllowance = reader["Resolved_OwnAllowance"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_OwnAllowance"]) : null;
                        if (ColExists(reader, "Resolved_Remarks")) dto.Resolved_Remarks = reader["Resolved_Remarks"] != DBNull.Value ? reader["Resolved_Remarks"].ToString() : null;

                        if ((dto.Resolved_MaxKM == null && dto.Resolved_RatePerKm == null && dto.Resolved_DA == null && dto.Resolved_OutstationDA == null && dto.Resolved_OwnAllowance == null) && !string.IsNullOrEmpty(dto.PolicyJson))
                            TryParseJsonFallback(dto);
                    }
                }
            }
            return dto;
        }

        private void TryParseJsonFallback(PolicyResolved dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.PolicyJson)) return;
            try
            {
                JObject json = JObject.Parse(dto.PolicyJson);
                dto.Resolved_MaxKM = TryGetDecimal(json, "MaxKM") ?? dto.Resolved_MaxKM;
                dto.Resolved_RatePerKm = TryGetDecimal(json, "RatePerKm") ?? dto.Resolved_RatePerKm;
                dto.Resolved_DA = TryGetDecimal(json, "DA") ?? dto.Resolved_DA;
                dto.Resolved_HotelCap = TryGetDecimal(json, "HotelCap") ?? dto.Resolved_HotelCap;

                decimal? outstation = null;
                outstation = outstation ?? TryGetDecimal(json, "OutstationDA"); outstation = outstation ?? TryGetDecimal(json, "Outstation_DA"); outstation = outstation ?? TryGetDecimal(json, "OutstationDa");
                if (outstation.HasValue) dto.Resolved_OutstationDA = outstation;

                decimal? ownArr = null;
                ownArr = ownArr ?? TryGetDecimal(json, "OwnArrangementAllowance"); ownArr = ownArr ?? TryGetDecimal(json, "OwnArrangementDa"); ownArr = ownArr ?? TryGetDecimal(json, "OwnArrangement_DA"); ownArr = ownArr ?? TryGetDecimal(json, "OwnArrangementDA");
                if (ownArr.HasValue) dto.Resolved_OwnAllowance = ownArr;

                if (json["Remarks"] != null) dto.Resolved_Remarks = Convert.ToString(json["Remarks"]);
                else if (json["resolved_remarks"] != null) dto.Resolved_Remarks = Convert.ToString(json["resolved_remarks"]);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("TryParseJsonFallback error: " + ex.Message); }
        }

        private decimal? TryGetDecimal(JObject json, string key)
        {
            if (json[key] == null || json[key].Type == JTokenType.Null) return null;
            decimal value;
            if (decimal.TryParse(Convert.ToString(json[key]), out value)) return value;
            return null;
        }

        private void ShowPolicyNotification(PolicyResolved policy)
        {
            if (policy == null) return;
            var sb = new StringBuilder();
            sb.AppendFormat("<strong>Policy applied:</strong> {0}<br/>", policy.Source == "EX" ? "Employee Exception" : "Policy Matrix");
            sb.AppendFormat("<strong>Specificity:</strong> {0}<br/>", policy.Specificity ?? 0);
            if (policy.Resolved_RatePerKm != null) sb.AppendFormat("<strong>Rate/km:</strong> ₹{0:N2}<br/>", policy.Resolved_RatePerKm);
            if (policy.Resolved_MaxKM != null) sb.AppendFormat("<strong>Max KM (month):</strong> {0:N0}<br/>", policy.Resolved_MaxKM);
            if (policy.Resolved_DA != null) sb.AppendFormat("<strong>DA:</strong> ₹{0:N0}<br/>", policy.Resolved_DA);
            if (policy.Resolved_OutstationDA != null) sb.AppendFormat("<strong>Outstation DA:</strong> ₹{0:N0}<br/>", policy.Resolved_OutstationDA);
            if (policy.Resolved_OwnAllowance != null) sb.AppendFormat("<strong>Own Arrangement Allowance:</strong> ₹{0:N0}<br/>", policy.Resolved_OwnAllowance);
            if (policy.Resolved_HotelCap != null) sb.AppendFormat("<strong>Hotel Cap:</strong> ₹{0:N0}<br/>", policy.Resolved_HotelCap);
            if (!string.IsNullOrEmpty(policy.Resolved_Remarks)) sb.AppendFormat("<strong>Remarks:</strong> {0}<br/>", HttpUtility.JavaScriptStringEncode(policy.Resolved_Remarks));

            string title = policy.Source == "EX" ? "Employee exception applied" : "Policy applied";
            string safeTitle = HttpUtility.JavaScriptStringEncode(title);
            string safeText = HttpUtility.JavaScriptStringEncode(sb.ToString());
            string notifyType = policy.Source == "EX" ? "warning" : "info";

            string js = "(function() { try { if (typeof showPNotify === 'function') { showPNotify('" + safeTitle + "', '" + safeText + "', '" + notifyType + "'); } } catch(e) { } })();";
            ScriptManager.RegisterStartupScript(this.Page, this.Page.GetType(), "policyNotify_" + Guid.NewGuid().ToString("N"), js, true);
        }

        private string GetLoggedInEmployeeCode() { return Session["WORKMAN"] as string; }
        private int? GetLoggedInRegionId() { return Session["REGION"] != null ? (int?)Convert.ToInt32(Session["REGION"]) : null; }
        private int? GetLoggedInBranchId() { return Session["BRANCH"] != null ? (int?)Convert.ToInt32(Session["BRANCH"]) : null; }
        private int? GetLoggedInGradeId() { return Session["GRADE"] != null ? (int?)Convert.ToInt32(Session["GRADE"]) : null; }
        private int? GetLoggedInDesignationId() { return Session["DESG"] != null ? (int?)Convert.ToInt32(Session["DESG"]) : null; }

        private DataTable BuildActivityUpdateDataTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("ActivityId", typeof(int));
            dt.Columns.Add("TotalDistanceKM", typeof(decimal));
            dt.Columns.Add("CalculatedClaimAmount", typeof(decimal));
            return dt;
        }

        private DataTable CollectGridActivityUpdates(out List<string> errors)
        {
            errors = new List<string>();
            DataTable tvp = BuildActivityUpdateDataTable();

            var sessionDt = Session[GetCurrentSessionKey()] as DataTable;
            if (sessionDt == null) { errors.Add("Session data missing. Please refresh the page."); return tvp; }

            CultureInfo ci = CultureInfo.GetCultureInfo("en-IN");
            decimal defaultRate = 3.75m;
            var cfg = ConfigurationManager.AppSettings["DefaultRatePerKm"];
            decimal tmpCfg;
            if (!string.IsNullOrWhiteSpace(cfg) && decimal.TryParse(cfg, out tmpCfg)) defaultRate = tmpCfg;

            for (int i = 0; i < gvActivities.Rows.Count; i++)
            {
                if (gvActivities.DataKeys == null || gvActivities.DataKeys.Count <= i) { errors.Add(string.Format("Row {0}: DataKey missing.", i + 1)); continue; }

                object keyObj = gvActivities.DataKeys[i].Value;
                if (keyObj == null || keyObj == DBNull.Value) { errors.Add(string.Format("Row {0}: DataKey null.", i + 1)); continue; }

                int activityId;
                if (!int.TryParse(keyObj.ToString(), out activityId)) { errors.Add(string.Format("Row {0}: invalid ActivityId.", i + 1)); continue; }

                DataRow found = null;
                if (sessionDt.PrimaryKey != null && sessionDt.PrimaryKey.Length > 0) found = sessionDt.Rows.Find(activityId);
                else { DataRow[] arr = sessionDt.Select(string.Format("ActivityId = {0}", activityId)); if (arr != null && arr.Length > 0) found = arr[0]; }

                if (found == null) { errors.Add(string.Format("Activity {0}: session data not found.", activityId)); continue; }

                decimal km = 0m;
                if (found.Table.Columns.Contains("TotalDistanceKM") && found["TotalDistanceKM"] != DBNull.Value)
                    decimal.TryParse(found["TotalDistanceKM"].ToString(), NumberStyles.AllowDecimalPoint, ci, out km);

                decimal minAllowed = Decimal.MinValue, maxAllowed = Decimal.MaxValue, rate = defaultRate;
                decimal tmp;
                if (found.Table.Columns.Contains("MinAllowedKM") && found["MinAllowedKM"] != DBNull.Value && decimal.TryParse(found["MinAllowedKM"].ToString(), NumberStyles.AllowDecimalPoint, ci, out tmp)) minAllowed = tmp;
                if (found.Table.Columns.Contains("MaxAllowedKM") && found["MaxAllowedKM"] != DBNull.Value && decimal.TryParse(found["MaxAllowedKM"].ToString(), NumberStyles.AllowDecimalPoint, ci, out tmp)) maxAllowed = tmp;
                if (found.Table.Columns.Contains("RatePerKm") && found["RatePerKm"] != DBNull.Value && decimal.TryParse(found["RatePerKm"].ToString(), NumberStyles.AllowDecimalPoint, ci, out tmp)) rate = tmp;

                bool hasMin = (minAllowed != Decimal.MinValue);
                bool hasMax = (maxAllowed != Decimal.MaxValue);

                if (hasMin && km < minAllowed - 0.0001m) errors.Add(string.Format("Activity {0}: KM {1:N2} less than minimum {2:N2}.", activityId, km, minAllowed));
                if (hasMax && km > maxAllowed + 0.0001m) errors.Add(string.Format("Activity {0}: KM {1:N2} greater than maximum {2:N2}.", activityId, km, maxAllowed));

                decimal calcClaim = Math.Round(rate * km, 2, MidpointRounding.AwayFromZero);
                DataRow dr = tvp.NewRow(); dr["ActivityId"] = activityId; dr["TotalDistanceKM"] = km; dr["CalculatedClaimAmount"] = calcClaim;
                tvp.Rows.Add(dr);
            }
            return tvp;
        }

        private int ExecuteSaveDraft(DataTable tvp)
        {
            int updatedRows = 0;
            using (SqlConnection con = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_SaveActivitiesDraft", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                var pUpdates = cmd.Parameters.AddWithValue("@Updates", tvp);
                pUpdates.SqlDbType = SqlDbType.Structured;
                pUpdates.TypeName = "dbo.TVP_ActivityUpdate";
                cmd.Parameters.AddWithValue("@LoginId", GetCurrentUserLoginId());
                con.Open();
                using (var rdr = cmd.ExecuteReader()) { if (rdr.Read()) { updatedRows = rdr.GetInt32(0); } }
            }
            return updatedRows;
        }

        private bool TryPreparePartialResubmission(int year, int month, DataTable tvp, out DataTable filteredTvp, out string message)
        {
            filteredTvp = tvp;
            message = null;
            string loginId = GetCurrentUserLoginId();

            using (SqlConnection con = new SqlConnection(connString))
            {
                con.Open();
                int? requestId = null;
                string requestStatus = null;

                using (SqlCommand cmd = new SqlCommand(
                    @"SELECT TOP 1 ApprovalRequestId, Status
                      FROM dbo.SalesApprovalRequest
                      WHERE RequesterLoginId = @LoginId AND YearNo = @Year AND MonthNo = @Month
                      ORDER BY RequestDate DESC", con))
                {
                    cmd.Parameters.AddWithValue("@LoginId", loginId);
                    cmd.Parameters.AddWithValue("@Year", year);
                    cmd.Parameters.AddWithValue("@Month", month);
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (!rdr.Read())
                            return false;

                        requestId = rdr["ApprovalRequestId"] != DBNull.Value
                            ? (int?)Convert.ToInt32(rdr["ApprovalRequestId"])
                            : null;
                        requestStatus = rdr["Status"] != DBNull.Value ? rdr["Status"].ToString().Trim() : null;
                    }
                }

                if (!requestId.HasValue)
                    return false;

                // Whole-request reopen: submit all rows as a normal submission.
                if (string.Equals(requestStatus, "Draft", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(requestStatus, "Rejected", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(requestStatus, "Returned", StringComparison.OrdinalIgnoreCase))
                    return false;

                var returnedActivityIds = new HashSet<int>();
                using (SqlCommand cmd = new SqlCommand(
                    @"SELECT DISTINCT ActivityId
                      FROM dbo.SalesApprovalRequestDetail
                      WHERE ApprovalRequestId = @Id
                        AND DetailStatus = 'Returned'", con))
                {
                    cmd.Parameters.AddWithValue("@Id", requestId.Value);
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            if (rdr["ActivityId"] != DBNull.Value)
                                returnedActivityIds.Add(Convert.ToInt32(rdr["ActivityId"]));
                        }
                    }
                }

                if (returnedActivityIds.Count == 0)
                {
                    message = "This month already has a pending approval request, and there are no returned days to resubmit.";
                    filteredTvp = tvp.Clone();
                    return true;
                }

                // SP validates SalesDailyActivity.ClaimStatus = 'Returned'. Editing/soft-delete
                // can leave ClaimStatus as Draft while DetailStatus stays Returned — sync first.
                string idList = string.Join(",", returnedActivityIds.Select(id => id.ToString()).ToArray());
                string syncSql = string.Format(@"
                    UPDATE dbo.SalesDailyActivity
                    SET ClaimStatus = 'Returned',
                        ModifiedByUserId = @User,
                        ModifiedDateTime = DATEADD(MINUTE, 330, SYSUTCDATETIME())
                    WHERE ActivityId IN ({0})
                      AND ISNULL(ClaimStatus, '') <> 'Returned';

                    UPDATE dbo.SalesDailyExpense
                    SET ClaimStatus = 'Returned',
                        ModifiedBy = @User,
                        ModifiedOn = SYSUTCDATETIME()
                    WHERE ActivityId IN ({0})
                      AND IsDeleted = 0
                      AND ISNULL(ClaimStatus, '') NOT IN ('Returned', 'Rejected', 'Approved');", idList);

                using (SqlCommand syncCmd = new SqlCommand(syncSql, con))
                {
                    syncCmd.Parameters.AddWithValue("@User", loginId);
                    syncCmd.ExecuteNonQuery();
                }

                filteredTvp = FilterActivityUpdatesToReturnedOnly(tvp, returnedActivityIds);
                if (filteredTvp.Rows.Count == 0)
                {
                    message = "No returned activities were found in the current grid to resubmit. Refresh and try again.";
                    return true;
                }

                return true;
            }
        }

        private static DataTable FilterActivityUpdatesToReturnedOnly(DataTable source, HashSet<int> returnedActivityIds)
        {
            if (source == null || returnedActivityIds == null || returnedActivityIds.Count == 0)
                return source;

            DataTable filtered = source.Clone();
            foreach (DataRow row in source.Rows)
            {
                if (row["ActivityId"] == DBNull.Value) continue;
                int activityId = Convert.ToInt32(row["ActivityId"]);
                if (returnedActivityIds.Contains(activityId))
                    filtered.ImportRow(row);
            }
            return filtered;
        }

        private int ExecuteSubmitForApproval(DataTable tvp, int year, int month, string remarks)
        {
            if (Session["RONE_CODE"] == null) throw new Exception("Approval hierarchy not loaded in session.");

            int approvalRequestId = 0;
            using (SqlConnection con = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_SubmitSalesDailyActivities", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                var pUpdates = cmd.Parameters.AddWithValue("@Updates", tvp);
                pUpdates.SqlDbType = SqlDbType.Structured;
                pUpdates.TypeName = "dbo.TVP_ActivityUpdate";

                cmd.Parameters.AddWithValue("@LoginId", GetCurrentUserLoginId());
                cmd.Parameters.AddWithValue("@Year", year);
                cmd.Parameters.AddWithValue("@Month", month);
                cmd.Parameters.AddWithValue("@Remarks", (object)remarks ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@L1Code", Session["RONE_CODE"] != null ? (object)Session["RONE_CODE"].ToString() : DBNull.Value);
                cmd.Parameters.AddWithValue("@L2Code", DBNull.Value);
                cmd.Parameters.AddWithValue("@L3Code", DBNull.Value);
                cmd.Parameters.AddWithValue("@AuditorCode", Session["AUDITOR_CODE"] != null ? (object)Session["AUDITOR_CODE"].ToString() : DBNull.Value);

                con.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        if (rdr.FieldCount >= 1 && !rdr.IsDBNull(0)) approvalRequestId = rdr.GetInt32(0);
                    }
                }
            }
            return approvalRequestId;
        }

        protected void btn_save_draft_Click(object sender, EventArgs e)
        {
            try
            {
                if (gvActivities.FooterRow == null) { ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyNoFooter", "showPNotify('Warning','No data to save as draft.','warning');", true); return; }
                decimal totalKM = ParseDecimalFromLabel("lblFooterTotalKM");
                decimal totalClaim = ParseCurrencyFromLabel("lblFooterCalculated");

                int rowsUpserted = 0;
                int expensesUpdated = 0;
                ExecuteSaveMonthlySummary(totalKM, totalClaim, out rowsUpserted, out expensesUpdated);

                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(connString))
                using (SqlCommand cmd = new SqlCommand(@"SELECT TotalKM, TotalClaimAmount, CreatedOn FROM dbo.SalesMonthlyDraftSummary WHERE Login_Id=@Login AND YearNo=@Y AND MonthNo=@M", con))
                {
                    cmd.Parameters.AddWithValue("@Login", GetCurrentUserLoginId());
                    cmd.Parameters.AddWithValue("@Y", int.Parse(DDL_Year.SelectedValue));
                    cmd.Parameters.AddWithValue("@M", int.Parse(DDL_Month.SelectedValue));
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
                }

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    string msg = string.Format("showPNotify('Saved','Draft saved — KM: {0} Claim: ₹ {1}','success');", row["TotalKM"], row["TotalClaimAmount"]);
                    ScriptManager.RegisterStartupScript(this, GetType(), "showSavedSummary", msg, true);
                }
                Session.Remove(GetCurrentSessionKey());
                BindGrid(true, false);

                lbl_msg.Text = string.Format("Saved draft summary successfully (Total KM: {0:N2}, Total Claim: ₹ {1:N2}). [Upserted: {2}, Expenses Updated: {3}]", totalKM, totalClaim, rowsUpserted, expensesUpdated);
                lbl_msg.ForeColor = System.Drawing.Color.Green;
            }
            catch (Exception ex)
            {
                lbl_msg.Text = "Error saving draft: " + ex.Message;
                lbl_msg.ForeColor = System.Drawing.Color.Red;
                string safeErr = ex.Message.Replace("'", "\\'");
                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyDraftErr", "showPNotify('Error','" + safeErr + "','error');", true);
            }
        }

        private void ExecuteSaveMonthlySummary(decimal totalKM, decimal totalClaim, out int rowsUpserted, out int expensesUpdated)
        {
            rowsUpserted = 0;
            expensesUpdated = 0;
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                using (SqlCommand cmd = new SqlCommand("dbo.usp_SaveMonthlyDraftSummary", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@LoginId", GetCurrentUserLoginId());
                    cmd.Parameters.AddWithValue("@YearNo", int.Parse(DDL_Year.SelectedValue));
                    cmd.Parameters.AddWithValue("@MonthNo", int.Parse(DDL_Month.SelectedValue));
                    cmd.Parameters.AddWithValue("@TotalKM", totalKM);
                    cmd.Parameters.AddWithValue("@TotalClaimAmount", totalClaim);
                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows && rdr.Read())
                        {
                            rowsUpserted = Convert.ToInt32(rdr["RowsUpserted"]);
                            expensesUpdated = Convert.ToInt32(rdr["ExpensesUpdated"]);
                        }
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Error in ExecuteSaveMonthlySummary: " + ex); throw; }
        }

        private decimal ParseCurrencyFromLabel(string labelId)
        {
            Label lbl = null;
            if (gvActivities.FooterRow != null) lbl = gvActivities.FooterRow.FindControl(labelId) as Label;
            if (lbl == null || string.IsNullOrWhiteSpace(lbl.Text)) return 0m;
            string text = lbl.Text.Replace("₹", "").Replace(",", "").Trim();
            decimal val; decimal.TryParse(text, out val); return val;
        }

        private decimal ParseDecimalFromLabel(string labelId)
        {
            Label lbl = null;
            if (gvActivities.FooterRow != null) lbl = gvActivities.FooterRow.FindControl(labelId) as Label;
            if (lbl == null || string.IsNullOrWhiteSpace(lbl.Text)) return 0m;
            string text = lbl.Text.Replace("km", "").Replace(",", "").Trim();
            decimal val; decimal.TryParse(text, out val); return val;
        }

        protected void btn_submit_Click(object sender, EventArgs e)
        {
            try
            {
                int year = int.Parse(DDL_Year.SelectedValue);
                int month = int.Parse(DDL_Month.SelectedValue);
                bool isPolicyEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings["EnablePreviousMonthPolicy"]);

                if (isPolicyEnabled)
                {
                    DateTime currentDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                    int currentYear = currentDate.Year; int currentMonth = currentDate.Month;
                    bool isAllowedWindow = currentDate.Day >= 1 && currentDate.Day <= 15;
                    bool isPreviousMonthSelected = (year == currentYear && month == currentMonth - 1) || (currentMonth == 1 && month == 12 && year == currentYear - 1);
                    if (!(isAllowedWindow && isPreviousMonthSelected))
                    {
                        lbl_msg.Text = "You can submit previous month data only between 1st and 15th of the current month.";
                        lbl_msg.ForeColor = System.Drawing.Color.Red;
                        ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyPolicyBlockSubmit", "showPNotify('Policy Restriction', 'Submission is allowed only between 1st and 15th for the previous month.', 'error');", true);
                        return;
                    }
                }

                List<string> errors;
                DataTable tvp = CollectGridActivityUpdates(out errors);
                if (errors.Any())
                {
                    string msg = "Unable to submit due to validation errors:<br/>" + string.Join("<br/>", errors.ToArray());
                    lbl_msg.Text = "Validation failed."; lbl_msg.ForeColor = System.Drawing.Color.Red;
                    string safeErr = msg.Replace("'", "\\'");
                    ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySubmitErr", "showPNotify('Validation Error','" + safeErr + "', 'error');", true);
                    return;
                }

                if (tvp.Rows.Count == 0)
                {
                    lbl_msg.Text = "No activity rows found to submit."; lbl_msg.ForeColor = System.Drawing.Color.Red;
                    ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySubmitNoRows", "showPNotify('Nothing to submit', 'No activity rows found.', 'warning');", true);
                    return;
                }

                DataTable submitTvp;
                string partialMessage;
                if (TryPreparePartialResubmission(year, month, tvp, out submitTvp, out partialMessage))
                {
                    if (!string.IsNullOrWhiteSpace(partialMessage) || submitTvp == null || submitTvp.Rows.Count == 0)
                    {
                        string msg = partialMessage ?? "No returned activities found to resubmit.";
                        lbl_msg.Text = msg;
                        lbl_msg.ForeColor = System.Drawing.Color.Red;
                        ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySubmitNoReturned",
                            "showPNotify('Nothing to resubmit', '" + msg.Replace("'", "\\'") + "', 'warning');", true);
                        return;
                    }
                    tvp = submitTvp;
                }

                int approvalRequestId = ExecuteSubmitForApproval(tvp, year, month, "Submitted via UI");
                Session.Remove(GetCurrentSessionKey());
                BindGrid(true);

                if (approvalRequestId > 0)
                {
                    lbl_msg.Text = string.Format("Submitted {0} rows for approval. Request Id: {1}", tvp.Rows.Count, approvalRequestId);
                    lbl_msg.ForeColor = System.Drawing.Color.Green;
                    string scriptMsg = string.Format("showPNotify('Submitted','Submitted for approval successfully. Request ID: {0}','success');", approvalRequestId);
                    ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySubmit", scriptMsg, true);
                }
                else
                {
                    lbl_msg.Text = "Submitted for approval (no request id returned).";
                    lbl_msg.ForeColor = System.Drawing.Color.Green;
                    ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySubmit2", "showPNotify('Submitted','Submitted for approval successfully.','success');", true);
                }
            }
            catch (SqlException sqx)
            {
                lbl_msg.Text = "SQL Error while submitting: " + sqx.Message; lbl_msg.ForeColor = System.Drawing.Color.Red;
                string safeErr = sqx.Message.Replace("'", "\\'");
                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySubmitSqlErr", "showPNotify('SQL Error','" + safeErr + "','error');", true);
            }
            catch (Exception ex)
            {
                lbl_msg.Text = "Error during submit: " + ex.Message; lbl_msg.ForeColor = System.Drawing.Color.Red;
                string safeErr = ex.Message.Replace("'", "\\'");
                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySubmitErr2", "showPNotify('Error','" + safeErr + "','error');", true);
            }
        }

        private void PopulateStatusSummary_OLD(DataTable dt)
        {
            if (dt == null) return;

            Func<string, Func<DataRow, bool>, decimal> SumMoney = (colName, predicate) =>
            {
                decimal sum = 0m;
                if (!dt.Columns.Contains(colName)) return 0m;
                foreach (DataRow r in dt.Rows)
                {
                    if (!predicate(r)) continue;
                    if (r[colName] != DBNull.Value) { decimal val; if (decimal.TryParse(r[colName].ToString(), out val)) sum += val; }
                }
                return sum;
            };

            Func<Func<DataRow, bool>, decimal> SumKM = (predicate) =>
            {
                decimal sum = 0m;
                if (!dt.Columns.Contains("TotalDistanceKM")) return 0m;
                foreach (DataRow r in dt.Rows)
                {
                    if (!predicate(r)) continue;
                    if (r["TotalDistanceKM"] != DBNull.Value) { decimal km; if (decimal.TryParse(r["TotalDistanceKM"].ToString(), out km)) sum += km; }
                }
                return sum;
            };

            Func<string, Func<DataRow, bool>> byClaimStatus = (status) =>
            {
                return (r) =>
                {
                    string claim = r.Table.Columns.Contains("ClaimStatus") && r["ClaimStatus"] != DBNull.Value ? r["ClaimStatus"].ToString() : string.Empty;
                    if (string.Equals(status, "Submitted", StringComparison.OrdinalIgnoreCase)) return string.Equals(claim, "Submitted", StringComparison.OrdinalIgnoreCase) || string.Equals(claim, "Pending", StringComparison.OrdinalIgnoreCase);
                    if (string.Equals(status, "Other", StringComparison.OrdinalIgnoreCase))
                    {
                        bool isDraft = string.Equals(claim, "Draft", StringComparison.OrdinalIgnoreCase);
                        bool isSubmitted = string.Equals(claim, "Submitted", StringComparison.OrdinalIgnoreCase) || string.Equals(claim, "Pending", StringComparison.OrdinalIgnoreCase);
                        bool isApproved = string.Equals(claim, "Approved", StringComparison.OrdinalIgnoreCase);
                        bool isEditable = r.Table.Columns.Contains("IsEditable") && r["IsEditable"] != DBNull.Value && Convert.ToBoolean(r["IsEditable"]);
                        return !(isDraft || isSubmitted || isApproved || isEditable);
                    }
                    return string.Equals(claim, status, StringComparison.OrdinalIgnoreCase);
                };
            };

            Func<DataRow, bool> isEditablePredicate = (r) =>
            {
                bool isEditable = r.Table.Columns.Contains("IsEditable") && r["IsEditable"] != DBNull.Value && Convert.ToBoolean(r["IsEditable"]);
                string claim = r.Table.Columns.Contains("ClaimStatus") && r["ClaimStatus"] != DBNull.Value ? r["ClaimStatus"].ToString() : string.Empty;
                return isEditable && !string.Equals(claim, "Draft", StringComparison.OrdinalIgnoreCase);
            };

            decimal d_ta = SumMoney("CalculatedClaimAmount", byClaimStatus("Draft")); decimal d_da = SumMoney("DAAmount", byClaimStatus("Draft")); decimal d_hotel = SumMoney("HotelAmount", byClaimStatus("Draft")); decimal d_travel = SumMoney("TravelAmount", byClaimStatus("Draft")); decimal d_exp = SumMoney("ExpenseTotal", byClaimStatus("Draft")); decimal d_km = SumKM(byClaimStatus("Draft"));
            if (lblDraft_TA != null) lblDraft_TA.Text = "₹ " + d_ta.ToString("N2");
            if (lblDraft_DA != null) lblDraft_DA.Text = "₹ " + d_da.ToString("N2");
            if (lblDraft_Hotel != null) lblDraft_Hotel.Text = "₹ " + d_hotel.ToString("N2");
            if (lblDraft_Travel != null) lblDraft_Travel.Text = "₹ " + d_travel.ToString("N2");
            if (lblDraft_Expense != null) lblDraft_Expense.Text = "₹ " + d_exp.ToString("N2");
            if (lblDraft_KM != null) lblDraft_KM.Text = d_km.ToString("N2");

            decimal s_ta = SumMoney("CalculatedClaimAmount", byClaimStatus("Submitted")); decimal s_da = SumMoney("DAAmount", byClaimStatus("Submitted")); decimal s_hotel = SumMoney("HotelAmount", byClaimStatus("Submitted")); decimal s_travel = SumMoney("TravelAmount", byClaimStatus("Submitted")); decimal s_exp = SumMoney("ExpenseTotal", byClaimStatus("Submitted")); decimal s_km = SumKM(byClaimStatus("Submitted"));
            if (lblSubmitted_TA != null) lblSubmitted_TA.Text = "₹ " + s_ta.ToString("N2");
            if (lblSubmitted_DA != null) lblSubmitted_DA.Text = "₹ " + s_da.ToString("N2");
            if (lblSubmitted_Hotel != null) lblSubmitted_Hotel.Text = "₹ " + s_hotel.ToString("N2");
            if (lblSubmitted_Travel != null) lblSubmitted_Travel.Text = "₹ " + s_travel.ToString("N2");
            if (lblSubmitted_Expense != null) lblSubmitted_Expense.Text = "₹ " + s_exp.ToString("N2");
            if (lblSubmitted_KM != null) lblSubmitted_KM.Text = s_km.ToString("N2");

            decimal a_ta = SumMoney("CalculatedClaimAmount", byClaimStatus("Approved")); decimal a_da = SumMoney("DAAmount", byClaimStatus("Approved")); decimal a_hotel = SumMoney("HotelAmount", byClaimStatus("Approved")); decimal a_travel = SumMoney("TravelAmount", byClaimStatus("Approved")); decimal a_exp = SumMoney("ExpenseTotal", byClaimStatus("Approved")); decimal a_km = SumKM(byClaimStatus("Approved"));
            if (lblApproved_TA != null) lblApproved_TA.Text = "₹ " + a_ta.ToString("N2");
            if (lblApproved_DA != null) lblApproved_DA.Text = "₹ " + a_da.ToString("N2");
            if (lblApproved_Hotel != null) lblApproved_Hotel.Text = "₹ " + a_hotel.ToString("N2");
            if (lblApproved_Travel != null) lblApproved_Travel.Text = "₹ " + a_travel.ToString("N2");
            if (lblApproved_Expense != null) lblApproved_Expense.Text = "₹ " + a_exp.ToString("N2");
            if (lblApproved_KM != null) lblApproved_KM.Text = a_km.ToString("N2");

            decimal e_ta = SumMoney("CalculatedClaimAmount", isEditablePredicate); decimal e_da = SumMoney("DAAmount", isEditablePredicate); decimal e_hotel = SumMoney("HotelAmount", isEditablePredicate); decimal e_travel = SumMoney("TravelAmount", isEditablePredicate); decimal e_exp = SumMoney("ExpenseTotal", isEditablePredicate); decimal e_km = SumKM(isEditablePredicate);
            if (lblEditable_TA != null) lblEditable_TA.Text = "₹ " + e_ta.ToString("N2");
            if (lblEditable_DA != null) lblEditable_DA.Text = "₹ " + e_da.ToString("N2");
            if (lblEditable_Hotel != null) lblEditable_Hotel.Text = "₹ " + e_hotel.ToString("N2");
            if (lblEditable_Travel != null) lblEditable_Travel.Text = "₹ " + e_travel.ToString("N2");
            if (lblEditable_Expense != null) lblEditable_Expense.Text = "₹ " + e_exp.ToString("N2");
            if (lblEditable_KM != null) lblEditable_KM.Text = e_km.ToString("N2");

            decimal o_ta = SumMoney("CalculatedClaimAmount", byClaimStatus("Other")); decimal o_da = SumMoney("DAAmount", byClaimStatus("Other")); decimal o_hotel = SumMoney("HotelAmount", byClaimStatus("Other")); decimal o_travel = SumMoney("TravelAmount", byClaimStatus("Other")); decimal o_exp = SumMoney("ExpenseTotal", byClaimStatus("Other")); decimal o_km = SumKM(byClaimStatus("Other"));
            if (lblOther_TA != null) lblOther_TA.Text = "₹ " + o_ta.ToString("N2");
            if (lblOther_DA != null) lblOther_DA.Text = "₹ " + o_da.ToString("N2");
            if (lblOther_Hotel != null) lblOther_Hotel.Text = "₹ " + o_hotel.ToString("N2");
            if (lblOther_Travel != null) lblOther_Travel.Text = "₹ " + o_travel.ToString("N2");
            if (lblOther_Expense != null) lblOther_Expense.Text = "₹ " + o_exp.ToString("N2");
            if (lblOther_KM != null) lblOther_KM.Text = o_km.ToString("N2");
        }

        private void PopulateStatusSummary(DataTable dt)
        {
            if (dt == null) return;

            Func<string, Func<DataRow, bool>, decimal> SumMoney = (colName, predicate) =>
            {
                decimal sum = 0m;
                if (!dt.Columns.Contains(colName)) return 0m;
                foreach (DataRow r in dt.Rows)
                {
                    if (!predicate(r)) continue;
                    if (r[colName] != DBNull.Value) { decimal val; if (decimal.TryParse(r[colName].ToString(), out val)) sum += val; }
                }
                return sum;
            };

            Func<Func<DataRow, bool>, decimal> SumKM = (predicate) =>
            {
                decimal sum = 0m;
                if (!dt.Columns.Contains("TotalDistanceKM")) return 0m;
                foreach (DataRow r in dt.Rows)
                {
                    if (!predicate(r)) continue;
                    if (r["TotalDistanceKM"] != DBNull.Value) { decimal km; if (decimal.TryParse(r["TotalDistanceKM"].ToString(), out km)) sum += km; }
                }
                return sum;
            };

            Func<string, Func<DataRow, bool>> byClaimStatus = (status) =>
            {
                return (r) =>
                {
                    string claim = r.Table.Columns.Contains("ClaimStatus") && r["ClaimStatus"] != DBNull.Value ? r["ClaimStatus"].ToString() : string.Empty;
                    if (string.Equals(status, "Submitted", StringComparison.OrdinalIgnoreCase)) return string.Equals(claim, "Submitted", StringComparison.OrdinalIgnoreCase) || string.Equals(claim, "Pending", StringComparison.OrdinalIgnoreCase);
                    if (string.Equals(status, "Other", StringComparison.OrdinalIgnoreCase))
                    {
                        bool isDraft = string.Equals(claim, "Draft", StringComparison.OrdinalIgnoreCase);
                        bool isSubmitted = string.Equals(claim, "Submitted", StringComparison.OrdinalIgnoreCase) || string.Equals(claim, "Pending", StringComparison.OrdinalIgnoreCase);
                        bool isApproved = string.Equals(claim, "Approved", StringComparison.OrdinalIgnoreCase);
                        bool isEditable = r.Table.Columns.Contains("IsEditable") && r["IsEditable"] != DBNull.Value && Convert.ToBoolean(r["IsEditable"]);
                        return !(isDraft || isSubmitted || isApproved || isEditable);
                    }
                    return string.Equals(claim, status, StringComparison.OrdinalIgnoreCase);
                };
            };

            Func<DataRow, bool> isEditablePredicate = (r) =>
            {
                bool isEditable = r.Table.Columns.Contains("IsEditable") && r["IsEditable"] != DBNull.Value && Convert.ToBoolean(r["IsEditable"]);
                string claim = r.Table.Columns.Contains("ClaimStatus") && r["ClaimStatus"] != DBNull.Value ? r["ClaimStatus"].ToString() : string.Empty;
                return isEditable && !string.Equals(claim, "Draft", StringComparison.OrdinalIgnoreCase);
            };

            // --- DRAFT ---
            decimal d_ta = SumMoney("CalculatedClaimAmount", byClaimStatus("Draft")); decimal d_da = SumMoney("DAAmount", byClaimStatus("Draft")); decimal d_hotel = SumMoney("HotelAmount", byClaimStatus("Draft")); decimal d_travel = SumMoney("TravelAmount", byClaimStatus("Draft")); decimal d_stat = SumMoney("StationaryAmount", byClaimStatus("Draft")); decimal d_exp = SumMoney("ExpenseTotal", byClaimStatus("Draft")); decimal d_km = SumKM(byClaimStatus("Draft"));
            if (lblDraft_TA != null) lblDraft_TA.Text = "₹ " + d_ta.ToString("N2");
            if (lblDraft_DA != null) lblDraft_DA.Text = "₹ " + d_da.ToString("N2");
            if (lblDraft_Hotel != null) lblDraft_Hotel.Text = "₹ " + d_hotel.ToString("N2");
            if (lblDraft_Travel != null) lblDraft_Travel.Text = "₹ " + d_travel.ToString("N2");
            if (lblDraft_Stationary != null) lblDraft_Stationary.Text = "₹ " + d_stat.ToString("N2"); // Added
            if (lblDraft_Expense != null) lblDraft_Expense.Text = "₹ " + d_exp.ToString("N2");
            if (lblDraft_KM != null) lblDraft_KM.Text = d_km.ToString("N2");

            // --- SUBMITTED ---
            decimal s_ta = SumMoney("CalculatedClaimAmount", byClaimStatus("Submitted")); decimal s_da = SumMoney("DAAmount", byClaimStatus("Submitted")); decimal s_hotel = SumMoney("HotelAmount", byClaimStatus("Submitted")); decimal s_travel = SumMoney("TravelAmount", byClaimStatus("Submitted")); decimal s_stat = SumMoney("StationaryAmount", byClaimStatus("Submitted")); decimal s_exp = SumMoney("ExpenseTotal", byClaimStatus("Submitted")); decimal s_km = SumKM(byClaimStatus("Submitted"));
            if (lblSubmitted_TA != null) lblSubmitted_TA.Text = "₹ " + s_ta.ToString("N2");
            if (lblSubmitted_DA != null) lblSubmitted_DA.Text = "₹ " + s_da.ToString("N2");
            if (lblSubmitted_Hotel != null) lblSubmitted_Hotel.Text = "₹ " + s_hotel.ToString("N2");
            if (lblSubmitted_Travel != null) lblSubmitted_Travel.Text = "₹ " + s_travel.ToString("N2");
            if (lblSubmitted_Stationary != null) lblSubmitted_Stationary.Text = "₹ " + s_stat.ToString("N2"); // Added
            if (lblSubmitted_Expense != null) lblSubmitted_Expense.Text = "₹ " + s_exp.ToString("N2");
            if (lblSubmitted_KM != null) lblSubmitted_KM.Text = s_km.ToString("N2");

            // --- APPROVED ---
            decimal a_ta = SumMoney("CalculatedClaimAmount", byClaimStatus("Approved")); decimal a_da = SumMoney("DAAmount", byClaimStatus("Approved")); decimal a_hotel = SumMoney("HotelAmount", byClaimStatus("Approved")); decimal a_travel = SumMoney("TravelAmount", byClaimStatus("Approved")); decimal a_stat = SumMoney("StationaryAmount", byClaimStatus("Approved")); decimal a_exp = SumMoney("ExpenseTotal", byClaimStatus("Approved")); decimal a_km = SumKM(byClaimStatus("Approved"));
            if (lblApproved_TA != null) lblApproved_TA.Text = "₹ " + a_ta.ToString("N2");
            if (lblApproved_DA != null) lblApproved_DA.Text = "₹ " + a_da.ToString("N2");
            if (lblApproved_Hotel != null) lblApproved_Hotel.Text = "₹ " + a_hotel.ToString("N2");
            if (lblApproved_Travel != null) lblApproved_Travel.Text = "₹ " + a_travel.ToString("N2");
            if (lblApproved_Stationary != null) lblApproved_Stationary.Text = "₹ " + a_stat.ToString("N2"); // Added
            if (lblApproved_Expense != null) lblApproved_Expense.Text = "₹ " + a_exp.ToString("N2");
            if (lblApproved_KM != null) lblApproved_KM.Text = a_km.ToString("N2");

            // --- EDITABLE ---
            decimal e_ta = SumMoney("CalculatedClaimAmount", isEditablePredicate); decimal e_da = SumMoney("DAAmount", isEditablePredicate); decimal e_hotel = SumMoney("HotelAmount", isEditablePredicate); decimal e_travel = SumMoney("TravelAmount", isEditablePredicate); decimal e_stat = SumMoney("StationaryAmount", isEditablePredicate); decimal e_exp = SumMoney("ExpenseTotal", isEditablePredicate); decimal e_km = SumKM(isEditablePredicate);
            if (lblEditable_TA != null) lblEditable_TA.Text = "₹ " + e_ta.ToString("N2");
            if (lblEditable_DA != null) lblEditable_DA.Text = "₹ " + e_da.ToString("N2");
            if (lblEditable_Hotel != null) lblEditable_Hotel.Text = "₹ " + e_hotel.ToString("N2");
            if (lblEditable_Travel != null) lblEditable_Travel.Text = "₹ " + e_travel.ToString("N2");
            if (lblEditable_Stationary != null) lblEditable_Stationary.Text = "₹ " + e_stat.ToString("N2"); // Added
            if (lblEditable_Expense != null) lblEditable_Expense.Text = "₹ " + e_exp.ToString("N2");
            if (lblEditable_KM != null) lblEditable_KM.Text = e_km.ToString("N2");

            // --- OTHER ---
            decimal o_ta = SumMoney("CalculatedClaimAmount", byClaimStatus("Other")); decimal o_da = SumMoney("DAAmount", byClaimStatus("Other")); decimal o_hotel = SumMoney("HotelAmount", byClaimStatus("Other")); decimal o_travel = SumMoney("TravelAmount", byClaimStatus("Other")); decimal o_stat = SumMoney("StationaryAmount", byClaimStatus("Other")); decimal o_exp = SumMoney("ExpenseTotal", byClaimStatus("Other")); decimal o_km = SumKM(byClaimStatus("Other"));
            if (lblOther_TA != null) lblOther_TA.Text = "₹ " + o_ta.ToString("N2");
            if (lblOther_DA != null) lblOther_DA.Text = "₹ " + o_da.ToString("N2");
            if (lblOther_Hotel != null) lblOther_Hotel.Text = "₹ " + o_hotel.ToString("N2");
            if (lblOther_Travel != null) lblOther_Travel.Text = "₹ " + o_travel.ToString("N2");
            if (lblOther_Stationary != null) lblOther_Stationary.Text = "₹ " + o_stat.ToString("N2"); // Added
            if (lblOther_Expense != null) lblOther_Expense.Text = "₹ " + o_exp.ToString("N2");
            if (lblOther_KM != null) lblOther_KM.Text = o_km.ToString("N2");
        }
    }
}