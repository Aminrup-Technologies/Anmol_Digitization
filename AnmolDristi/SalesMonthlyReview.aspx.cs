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
using System.Web.Services;
using System.Web.Script.Services;
using AnmolDristi.BLL;

namespace AnmolDristi
{
    public partial class SalesMonthlyReview : BasePage
    {
        private string connString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        private const string SESSION_KEY_GV_ACTIVITIES = "gvActivities_DataTable";
        private readonly PolicyResolutionService _policyResolutionService = new PolicyResolutionService();

        // --- FIX FOR BROKEN VISUAL STUDIO DESIGNER FILE ---
        // Explicitly declaring these so C# knows they exist even if the designer file failed to update.
        protected global::System.Web.UI.WebControls.HiddenField HF_SnapPolicyMode;
        protected global::System.Web.UI.WebControls.HiddenField HF_SnapResolvedDA;
        protected global::System.Web.UI.WebControls.HiddenField HF_SnapHotelCap;
        protected global::System.Web.UI.WebControls.HiddenField HF_SnapOutstationDA;
        protected global::System.Web.UI.WebControls.HiddenField HF_SnapOwnArrangementDA;
        //protected global::System.Web.UI.WebControls.HiddenField HF_FixedTravelAllowance;
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
                ResolvedPolicyDto resolvedPolicy = null;
                if (policybinder == true)
                {
                    string employeeCode = GetLoggedInEmployeeCode();
                    int? regionId = GetLoggedInRegionId();
                    int? branchId = GetLoggedInBranchId();
                    int? gradeId = GetLoggedInGradeId();
                    int? designationId = GetLoggedInDesignationId();
                    DateTime claimDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));
                    resolvedPolicy = _policyResolutionService.ResolvePolicy(
                        employeeCode,
                        claimDate,
                        regionId,
                        branchId,
                        gradeId,
                        designationId);

                    if (resolvedPolicy != null)
                        BindPolicyToModal(resolvedPolicy);
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

                    decimal defaultRate = GetDefaultRatePerKm();
                    decimal policyRate;
                    decimal? fixedTravelAllowance;
                    GetTravelAllowanceInputs(out policyRate, out fixedTravelAllowance, resolvedPolicy);

                    foreach (DataRow row in dt.Rows)
                    {
                        decimal totalKm = 0m;
                        if (row.Table.Columns.Contains("TotalDistanceKM") && row["TotalDistanceKM"] != DBNull.Value)
                            decimal.TryParse(row["TotalDistanceKM"].ToString(), out totalKm);

                        decimal rate = GetRatePerKm(row, policyRate > 0 ? policyRate : defaultRate);
                        decimal claim = TravelAllowanceHelper.CalculateAmount(totalKm, rate, fixedTravelAllowance);
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
                        if (ClaimStatuses.Equals(s, ClaimStatuses.Draft) ||
                            ClaimStatuses.Equals(s, ClaimStatuses.Rejected) ||
                            ClaimStatuses.Equals(s, ClaimStatuses.Returned))
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
                            var detailRemarksByActivity = new Dictionary<int, string>();
                            var detailApprovedByByActivity = new Dictionary<int, string>();
                            using (SqlCommand cmd2 = new SqlCommand(@"
                                SELECT ActivityId, DetailStatus, DetailRemarks, DetailApprovedBy
                                FROM (
                                    SELECT ActivityId, DetailStatus, DetailRemarks, DetailApprovedBy,
                                           ROW_NUMBER() OVER (
                                               PARTITION BY ActivityId
                                               ORDER BY ISNULL(DetailVersion, 1) DESC, ApprovalRequestDetailId DESC
                                           ) AS rn
                                    FROM dbo.SalesApprovalRequestDetail
                                    WHERE ApprovalRequestId = @Id
                                ) x
                                WHERE rn = 1", con))
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

                                        if (aid > 0 && rdr["DetailRemarks"] != DBNull.Value)
                                        {
                                            string remarks = rdr["DetailRemarks"].ToString();
                                            if (!string.IsNullOrWhiteSpace(remarks))
                                                detailRemarksByActivity[aid] = remarks;
                                        }

                                        if (aid > 0 && rdr["DetailApprovedBy"] != DBNull.Value)
                                        {
                                            string approvedBy = rdr["DetailApprovedBy"].ToString();
                                            if (!string.IsNullOrWhiteSpace(approvedBy))
                                                detailApprovedByByActivity[aid] = approvedBy;
                                        }

                                        // ⬅️ 2. Add "Returned" to the Line Item check
                                        if (ClaimEditabilityHelper.IsDetailStatusEditableForCorrection(detail))
                                        {
                                            editableActivityIds.Add(aid);
                                        }
                                    }
                                }
                            }

                            if (!dt.Columns.Contains("DetailStatus"))
                                dt.Columns.Add("DetailStatus", typeof(string));
                            if (!dt.Columns.Contains("DetailRemarks"))
                                dt.Columns.Add("DetailRemarks", typeof(string));
                            if (!dt.Columns.Contains("DetailApprovedBy"))
                                dt.Columns.Add("DetailApprovedBy", typeof(string));

                            foreach (DataRow row in dt.Rows)
                            {
                                int actId = row["ActivityId"] != DBNull.Value ? Convert.ToInt32(row["ActivityId"]) : 0;
                                row["IsEditable"] = editableActivityIds.Contains(actId);
                                row["DetailStatus"] = detailStatusByActivity.ContainsKey(actId)
                                    ? detailStatusByActivity[actId]
                                    : (object)DBNull.Value;

                                if (detailRemarksByActivity.ContainsKey(actId))
                                    row["DetailRemarks"] = detailRemarksByActivity[actId];

                                if (detailApprovedByByActivity.ContainsKey(actId))
                                    row["DetailApprovedBy"] = detailApprovedByByActivity[actId];
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
                    //    if (ClaimStatuses.Equals(s, ClaimStatuses.Draft) || ClaimStatuses.Equals(s, ClaimStatuses.Rejected))
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
                    //                    if (ClaimStatuses.Equals(detail, ClaimStatuses.Rejected))
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
                ViewState["PolicyWindowOpen"] = allowExecution;

                PopulateStatusSummary(dt);
                BindReturnedClaimsBanner(dt);
                BindSubmissionChecklist(dt, allowSubmission, monthLocked, allowExecution);
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
                // Deprecated: Save as Draft button removed — edits persist immediately via auto-save.
                // btn_save_draft.Enabled = !monthLocked || allowSubmission;
                btn_submit.Enabled = allowSubmission;

                BindMonthlyConfidenceUx(dt, monthLocked, allowSubmission, allowExecution, grandCalc, grandDA, grandHotel, grandTravel, grandStationary);

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
                string message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                lbl_msg.Text = "An error occurred: " + message;
                lbl_msg.ForeColor = System.Drawing.Color.Red;
                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyError", "showPNotify('Error', '" + message.Replace("'", "\\'") + "', 'error');", true);
            }
        }

        private void ApplyExpenseModalEditability(string activityId)
        {
            if (string.IsNullOrWhiteSpace(activityId)) return;

            int actId;
            if (!int.TryParse(activityId, out actId) || actId <= 0) return;

            string claimStatus = GetActivityClaimStatus(actId);
            string detailStatus = GetActivityDetailStatus(actId);
            bool canEditExpense = ClaimEditabilityHelper.CanEditExpense(claimStatus, detailStatus);

            if (!canEditExpense)
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
                    LBL_ExpenseMsg.Text = ClaimEditabilityHelper.IsRejectedDay(claimStatus, detailStatus)
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

            ClearRouteMetadataFields();
            if (HF_RouteActivityId != null) HF_RouteActivityId.Value = string.Empty;
            SetKmSaveWorkflow(KmSaveWorkflowManual);

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
            string rOneName = GetSessionValue("RONE_NAME");
            string rOneCode = GetSessionValue("RONE_CODE");
            string rTwoName = GetSessionValue("RTWO_NAME");
            string rTwoCode = GetSessionValue("RTWO_CODE");
            string rThreeName = GetSessionValue("RTHREE_NAME");
            string rThreeCode = GetSessionValue("RTHREE_CODE");
            string auditorName = GetSessionValue("AUDITOR_NAME");
            string auditorCode = GetSessionValue("AUDITOR_CODE");

            if (string.IsNullOrEmpty(rOneName) && !string.IsNullOrEmpty(rOneCode))
                rOneName = GetEmployeeNameByCode(rOneCode);
            if (string.IsNullOrEmpty(rTwoName) && !string.IsNullOrEmpty(rTwoCode))
                rTwoName = GetEmployeeNameByCode(rTwoCode);
            if (string.IsNullOrEmpty(rThreeName) && !string.IsNullOrEmpty(rThreeCode))
                rThreeName = GetEmployeeNameByCode(rThreeCode);
            if (string.IsNullOrEmpty(auditorName) && !string.IsNullOrEmpty(auditorCode))
                auditorName = GetEmployeeNameByCode(auditorCode);

            var levels = new List<ApproverDisplayLevel>();
            TryAddApproverLevel(levels, "Manager (L1)", rOneName, rOneCode);
            TryAddApproverLevel(levels, "Regional Manager (L2)", rTwoName, rTwoCode);
            TryAddApproverLevel(levels, "Zonal Manager (L3)", rThreeName, rThreeCode);
            TryAddApproverLevel(levels, "Auditor", auditorName, auditorCode);

            RenderApproverHierarchy(levels);
        }

        private sealed class ApproverDisplayLevel
        {
            public string Title { get; set; }
            public string Name { get; set; }
            public string Code { get; set; }
        }

        private static string GetSessionValue(string key)
        {
            return HttpContext.Current != null && HttpContext.Current.Session != null && HttpContext.Current.Session[key] != null
                ? HttpContext.Current.Session[key].ToString().Trim()
                : string.Empty;
        }

        private static void TryAddApproverLevel(List<ApproverDisplayLevel> levels, string title, string name, string code)
        {
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(code)) return;
            levels.Add(new ApproverDisplayLevel
            {
                Title = title,
                Name = string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim(),
                Code = string.IsNullOrWhiteSpace(code) ? string.Empty : code.Trim()
            });
        }

        private string GetEmployeeNameByCode(string employeeCode)
        {
            if (string.IsNullOrWhiteSpace(employeeCode)) return string.Empty;
            using (SqlConnection con = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 EmployeeName FROM dbo.MST_UserMaster WITH (NOLOCK) WHERE EmployeeCode = @EmployeeCode", con))
            {
                cmd.Parameters.AddWithValue("@EmployeeCode", employeeCode.Trim());
                con.Open();
                object result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value ? result.ToString().Trim() : string.Empty;
            }
        }

        private void RenderApproverHierarchy(List<ApproverDisplayLevel> levels)
        {
            if (phApproverHierarchy == null) return;

            phApproverHierarchy.Controls.Clear();
            if (levels == null || levels.Count == 0)
            {
                phApproverHierarchy.Controls.Add(new Literal { Text = "<div class=\"text-muted small\">No approvers configured for this employee.</div>" });
                return;
            }

            for (int i = 0; i < levels.Count; i++)
            {
                ApproverDisplayLevel level = levels[i];
                string nameHtml = string.IsNullOrEmpty(level.Name)
                    ? string.Empty
                    : string.Format("<div class=\"font-weight-bold text-dark\">{0}</div>", HttpUtility.HtmlEncode(level.Name));
                string codeHtml = string.IsNullOrEmpty(level.Code)
                    ? string.Empty
                    : string.Format("<div class=\"text-muted small\">{0}</div>", HttpUtility.HtmlEncode(level.Code));

                phApproverHierarchy.Controls.Add(new Literal
                {
                    Text = string.Format(
                        "<div class=\"card border shadow-sm mb-0\"><div class=\"card-body py-2 px-3\"><div class=\"small text-uppercase text-muted font-weight-bold mb-1\">{0}</div>{1}{2}</div></div>",
                        HttpUtility.HtmlEncode(level.Title),
                        nameHtml,
                        codeHtml)
                });

                if (i < levels.Count - 1)
                    phApproverHierarchy.Controls.Add(new Literal { Text = "<div class=\"text-center text-muted py-1\"><i class=\"fa fa-arrow-down\"></i></div>" });
            }
        }

        private string ComposeNameWithCode(string name, string code)
        {
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(code)) return string.Format("{0} ({1})", HttpUtility.HtmlEncode(name), HttpUtility.HtmlEncode(code));
            if (!string.IsNullOrEmpty(name)) return HttpUtility.HtmlEncode(name);
            if (!string.IsNullOrEmpty(code)) return string.Format("({0})", HttpUtility.HtmlEncode(code));
            return "—";
        }

        private void BindPolicyToModal(ResolvedPolicyDto policy)
        {
            if (policy == null) return;

            string employeeCode = !string.IsNullOrEmpty(policy.EmployeeCode) ? policy.EmployeeCode : GetLoggedInEmployeeCode();
            if (lblPolicyEmployeeCode != null) lblPolicyEmployeeCode.Text = HttpUtility.HtmlEncode(employeeCode);
            if (lblPolicySource != null)
            {
                string sourceLabel = !string.IsNullOrEmpty(policy.PolicySource)
                    ? policy.PolicySource
                    : (!string.IsNullOrEmpty(policy.Source) ? policy.Source : "HR-SOP");
                lblPolicySource.Text = HttpUtility.HtmlEncode(sourceLabel);
            }
            if (lblPolicyRowId != null) lblPolicyRowId.Text = policy.RowId.HasValue ? policy.RowId.Value.ToString() : "—";
            if (lblPolicyPriority != null)
                lblPolicyPriority.Text = policy.Priority.HasValue ? string.Format("Priority {0}", policy.Priority.Value) : "—";

            string regionName = GetNameFromMaster("MST_Region", "Id", "Region_Description", policy.RegionId) ?? "All Regions";
            string branchName = GetNameFromMaster("MST_Branch", "Id", "Branch_Description", policy.BranchId) ?? "All Branches";
            string gradeName = GetNameFromMaster("MST_Grade", "Id", "Grade_Description", policy.GradeId) ?? "All Grades";
            string desigName = GetNameFromMaster("MST_Designation", "Id", "Designation_Description", policy.DesignationId) ?? "All Designations";

            if (lblPolicySpecificity != null) lblPolicySpecificity.Text = BuildSpecificityChain(regionName, branchName, gradeName, desigName);
            if (lblPolicyDesignation != null) lblPolicyDesignation.Text = HttpUtility.HtmlEncode(desigName);
            if (lblPolicyGrade != null) lblPolicyGrade.Text = HttpUtility.HtmlEncode(gradeName);

            if (lblPolicyEffective != null)
            {
                string effFrom = policy.EffectiveFrom.HasValue ? policy.EffectiveFrom.Value.ToString("dd-MMM-yyyy") : "—";
                string effTo = policy.EffectiveTo.HasValue ? policy.EffectiveTo.Value.ToString("dd-MMM-yyyy") : "—";
                lblPolicyEffective.Text = string.Format("{0} to {1}", effFrom, effTo);
            }

            if (lblPolicyIsActive != null)
            {
                bool isActive = policy.IsActive.HasValue && policy.IsActive.Value;
                lblPolicyIsActive.Text = isActive ? "Active" : "Inactive";
                lblPolicyIsActive.CssClass = isActive ? "badge badge-success" : "badge badge-secondary";
            }

            if (lblPolicyDesigGrade != null)
            {
                string desigIdStr = policy.DesignationId.HasValue ? policy.DesignationId.Value.ToString() : "—";
                string gradeIdStr = policy.GradeId.HasValue ? policy.GradeId.Value.ToString() : "—";
                lblPolicyDesigGrade.Text = (policy.DesignationId.HasValue || policy.GradeId.HasValue)
                    ? string.Format("{0} / {1}", desigIdStr, gradeIdStr)
                    : "—";
            }

            BindResolvedHiddenFields(policy);
            BindResolvedClaimLimitsToModal(policy);
            BindPolicyRemarks(policy);

            if (policyJsonRaw != null)
            {
                policyJsonRaw.Text = policy.PolicyJson ?? "";
                policyJsonRaw.Visible = false;
            }

            if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = string.Empty;
            BindPolicyApprovers();
        }

        private static string BuildSpecificityChain(params string[] parts)
        {
            if (parts == null || parts.Length == 0) return "—";
            var filtered = parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()).ToList();
            return filtered.Count > 0 ? string.Join(" → ", filtered) : "—";
        }

        private void BindResolvedHiddenFields(ResolvedPolicyDto policy)
        {
            if (HF_Policy_RegionId != null) HF_Policy_RegionId.Value = policy.RegionId.HasValue ? policy.RegionId.Value.ToString() : string.Empty;
            if (HF_Policy_BranchId != null) HF_Policy_BranchId.Value = policy.BranchId.HasValue ? policy.BranchId.Value.ToString() : string.Empty;
            if (HF_Policy_GradeId != null) HF_Policy_GradeId.Value = policy.GradeId.HasValue ? policy.GradeId.Value.ToString() : string.Empty;
            if (HF_Policy_DesignationId != null) HF_Policy_DesignationId.Value = policy.DesignationId.HasValue ? policy.DesignationId.Value.ToString() : string.Empty;

            if (HF_ResolvedHotelCap != null) HF_ResolvedHotelCap.Value = policy.Resolved_HotelCap.HasValue ? policy.Resolved_HotelCap.Value.ToString("F2") : "0";
            if (HF_ResolvedDA != null) HF_ResolvedDA.Value = policy.Resolved_DA.HasValue ? policy.Resolved_DA.Value.ToString("F2") : "200";
            if (HF_OutstationDA != null) HF_OutstationDA.Value = policy.Resolved_OutstationDA.HasValue ? policy.Resolved_OutstationDA.Value.ToString("F2") : "350";
            if (HF_OwnArrangementDA != null) HF_OwnArrangementDA.Value = policy.Resolved_OwnAllowance.HasValue ? policy.Resolved_OwnAllowance.Value.ToString("F2") : "750";
            if (HF_ResolvedMaxKM != null) HF_ResolvedMaxKM.Value = policy.Resolved_MaxKM.HasValue ? policy.Resolved_MaxKM.Value.ToString("F2") : "0";
            if (HF_ResolvedRatePerKm != null) HF_ResolvedRatePerKm.Value = policy.Resolved_RatePerKm.HasValue ? policy.Resolved_RatePerKm.Value.ToString("F2") : "0";
            if (HF_FixedTravelAllowance != null) HF_FixedTravelAllowance.Value = policy.FixedTravelAllowance.HasValue ? policy.FixedTravelAllowance.Value.ToString("F2") : string.Empty;

            if (lblResolvedMaxKM != null) lblResolvedMaxKM.Text = policy.Resolved_MaxKM.HasValue ? policy.Resolved_MaxKM.Value.ToString("N2") : "0";
            if (lblResolvedRatePerKm != null) lblResolvedRatePerKm.Text = policy.Resolved_RatePerKm.HasValue ? policy.Resolved_RatePerKm.Value.ToString("N2") : "0";
            if (lblResolvedDA != null) lblResolvedDA.Text = policy.Resolved_DA.HasValue ? policy.Resolved_DA.Value.ToString("N2") : "0";
            if (lblResolvedHotelCap != null) lblResolvedHotelCap.Text = policy.Resolved_HotelCap.HasValue ? policy.Resolved_HotelCap.Value.ToString("N2") : "0";
        }

        private void BindResolvedClaimLimitsToModal(ResolvedPolicyDto policy)
        {
            if (phClaimLimits == null) return;

            phClaimLimits.Controls.Clear();
            bool hasAny = false;
            hasAny |= AddClaimLimitRowIfPresent("Rate per KM", FormatCurrency(policy.Resolved_RatePerKm));
            hasAny |= AddClaimLimitRowIfPresent("Head Office DA", FormatCurrency(policy.Resolved_DA));
            hasAny |= AddClaimLimitRowIfPresent("Outstation DA", FormatCurrency(policy.Resolved_OutstationDA));
            hasAny |= AddClaimLimitRowIfPresent("Own Arrangement", FormatCurrency(policy.Resolved_OwnAllowance));
            hasAny |= AddClaimLimitRowIfPresent("Hotel Limit", FormatCurrency(policy.Resolved_HotelCap, "/night"));
            hasAny |= AddClaimLimitRowIfPresent("Monthly KM Limit", FormatKmLimit(policy.Resolved_MaxKM));
            hasAny |= AddClaimLimitRowIfPresent("Fixed Travel Allowance", FormatCurrency(policy.FixedTravelAllowance));

            if (!hasAny)
            {
                phClaimLimits.Controls.Add(new Literal
                {
                    Text = "<div class=\"alert alert-light border text-muted small mb-0\">No resolved claim limits are available for this policy.</div>"
                });
            }
        }

        private bool AddClaimLimitRowIfPresent(string label, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;

            phClaimLimits.Controls.Add(new Literal
            {
                Text = string.Format(
                    "<div class=\"row py-2 border-bottom\"><div class=\"col-xs-6 col-sm-5 text-muted\">{0}</div><div class=\"col-xs-6 col-sm-7\"><strong>{1}</strong></div></div>",
                    HttpUtility.HtmlEncode(label),
                    HttpUtility.HtmlEncode(value))
            });
            return true;
        }

        private static string FormatCurrency(decimal? amount, string suffix = null)
        {
            if (!amount.HasValue) return null;
            string formatted = string.Format(CultureInfo.GetCultureInfo("en-IN"), "₹{0:N2}", amount.Value);
            return string.IsNullOrEmpty(suffix) ? formatted : formatted + suffix;
        }

        private static string FormatKmLimit(decimal? km)
        {
            if (!km.HasValue) return null;
            return string.Format(CultureInfo.InvariantCulture, "{0:N0} KM", km.Value);
        }

        private void BindPolicyRemarks(ResolvedPolicyDto policy)
        {
            if (lblResolvedRemarks == null) return;

            if (!string.IsNullOrWhiteSpace(policy.Resolved_Remarks))
            {
                lblResolvedRemarks.Text = HttpUtility.HtmlEncode(policy.Resolved_Remarks);
                if (policyRemarksSection != null) policyRemarksSection.Visible = true;
            }
            else
            {
                lblResolvedRemarks.Text = string.Empty;
                if (policyRemarksSection != null) policyRemarksSection.Visible = false;
            }
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

        private decimal GetDefaultRatePerKm()
        {
            decimal defaultRate = 3.75m;
            string cfg = ConfigurationManager.AppSettings["DefaultRatePerKm"];
            decimal tmpCfg;
            if (!string.IsNullOrWhiteSpace(cfg) && decimal.TryParse(cfg, out tmpCfg))
                defaultRate = tmpCfg;
            return defaultRate;
        }

        private void GetTravelAllowanceInputs(out decimal ratePerKm, out decimal? fixedTravelAllowance, ResolvedPolicyDto resolvedPolicy = null)
        {
            ratePerKm = GetDefaultRatePerKm();
            fixedTravelAllowance = null;

            if (resolvedPolicy != null)
            {
                if (resolvedPolicy.Resolved_RatePerKm.HasValue)
                    ratePerKm = resolvedPolicy.Resolved_RatePerKm.Value;
                if (resolvedPolicy.FixedTravelAllowance.HasValue)
                    fixedTravelAllowance = resolvedPolicy.FixedTravelAllowance;
                return;
            }

            decimal parsedRate;
            if (HF_ResolvedRatePerKm != null && decimal.TryParse(HF_ResolvedRatePerKm.Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out parsedRate) && parsedRate > 0)
                ratePerKm = parsedRate;

            decimal parsedFixed;
            if (HF_FixedTravelAllowance != null && decimal.TryParse(HF_FixedTravelAllowance.Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out parsedFixed))
                fixedTravelAllowance = parsedFixed;
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
            switch (e.CommandName)
            {
                case "OpenExpenses":
                    HandleOpenExpensesCommand(e.CommandArgument.ToString());
                    break;
                case "OpenEditModal":
                    HandleOpenEditModalCommand(Convert.ToString(e.CommandArgument));
                    break;
                case "EditRow":
                    HandleEditRowCommand(e.CommandArgument.ToString());
                    break;
                case "UpdateRow":
                    HandleUpdateRowCommand(e);
                    break;
            }
        }

        private void HandleOpenExpensesCommand(string commandArgument)
        {
            ResetExpenseEntryFields();
            string arg = commandArgument;
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

        private void HandleOpenEditModalCommand(string commandArgument)
        {
            string arg = commandArgument;
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

            InitializeKmModalSession(activityId);
            BindKmHistory(activityId);
            ApplyExpenseModalViewOnlyStateForKmModal(activityId);

            decimal baselineKm;
            if (!decimal.TryParse(currentKm, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out baselineKm))
                baselineKm = 0m;

            var tolerance = new EmployeeKmToleranceConfig();
            decimal tmp;
            if (!string.IsNullOrWhiteSpace(minKm) && decimal.TryParse(minKm, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out tmp))
                tolerance.ReductionTolerance = tmp;
            if (!string.IsNullOrWhiteSpace(maxKm) && decimal.TryParse(maxKm, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out tmp))
                tolerance.IncreaseTolerance = tmp;
            tolerance.MaxModificationCount = GetMaxModificationCountForEmployee(GetLoggedInEmployeeCode());

            ConfigureKmModalDisplay(activityId, baselineKm, tolerance);
        }

        private void ApplyExpenseModalViewOnlyStateForKmModal(int activityId)
        {
            string detailStatus = GetActivityDetailStatus(activityId);

            if (!ClaimEditabilityHelper.CanEditExpenseByDetailStatus(detailStatus))
            {
                if (BTN_SaveExpense != null) BTN_SaveExpense.Visible = false;
                if (DDL_ExpenseType != null) DDL_ExpenseType.Enabled = false;
                if (btnOpenEditKmFromExpense != null) btnOpenEditKmFromExpense.Visible = false;

                if (rptExpenses != null)
                {
                    foreach (RepeaterItem item in rptExpenses.Items)
                    {
                        if (item.ItemType == ListItemType.Item || item.ItemType == ListItemType.AlternatingItem)
                        {
                            LinkButton lnkDelete = item.FindControl("lnkDeleteExpense") as LinkButton;
                            LinkButton lnkDisabled = item.FindControl("lnkDeleteExpenseDisabled") as LinkButton;

                            if (lnkDelete != null) lnkDelete.Visible = false;
                            if (lnkDisabled != null) lnkDisabled.Visible = true;
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
                if (BTN_SaveExpense != null) BTN_SaveExpense.Visible = true;
                if (DDL_ExpenseType != null) DDL_ExpenseType.Enabled = true;
                if (btnOpenEditKmFromExpense != null) btnOpenEditKmFromExpense.Visible = true;

                if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "";
            }
        }

        private string GetActivityClaimStatus(int activityId)
        {
            string currentStatus = ClaimStatuses.Draft;
            using (SqlConnection conStat = new SqlConnection(connString))
            using (SqlCommand cmdStat = new SqlCommand("SELECT ClaimStatus FROM dbo.SalesDailyActivity WITH (NOLOCK) WHERE ActivityId = @ActId", conStat))
            {
                cmdStat.Parameters.AddWithValue("@ActId", activityId);
                conStat.Open();
                object statObj = cmdStat.ExecuteScalar();
                if (statObj != null && statObj != DBNull.Value) currentStatus = statObj.ToString().Trim();
            }
            return currentStatus;
        }

        private string GetActivityDetailStatus(int activityId)
        {
            string detailStatus = ClaimStatuses.Draft;
            using (SqlConnection conStat = new SqlConnection(connString))
            using (SqlCommand cmdStat = new SqlCommand(@"
                SELECT ISNULL(d.DetailStatus, '" + ClaimStatuses.Draft + @"')
                FROM dbo.SalesApprovalRequestDetail d WITH (NOLOCK)
                INNER JOIN (
                    SELECT ActivityId, MAX(ISNULL(DetailVersion, 1)) AS LatestVersion
                    FROM dbo.SalesApprovalRequestDetail WITH (NOLOCK)
                    WHERE ActivityId = @ActId
                    GROUP BY ActivityId
                ) latest
                    ON latest.ActivityId = d.ActivityId
                   AND ISNULL(d.DetailVersion, 1) = latest.LatestVersion
                WHERE d.ActivityId = @ActId", conStat))
            {
                cmdStat.Parameters.AddWithValue("@ActId", activityId);
                conStat.Open();
                object statObj = cmdStat.ExecuteScalar();
                if (statObj != null && statObj != DBNull.Value) detailStatus = statObj.ToString().Trim();
            }
            return detailStatus;
        }

        private bool CanPersistActivityKm(int activityId, out string currentStatus)
        {
            string claimStatus = GetActivityClaimStatus(activityId);
            string detailStatus = GetActivityDetailStatus(activityId);

            if (ClaimStatuses.Equals(claimStatus, ClaimStatuses.Approved)
                || ClaimStatuses.Equals(claimStatus, ClaimStatuses.Pending)
                || ClaimStatuses.Equals(claimStatus, ClaimStatuses.Rejected))
            {
                currentStatus = claimStatus;
                return false;
            }

            if (ClaimStatuses.Equals(claimStatus, ClaimStatuses.Draft)
                || ClaimStatuses.Equals(claimStatus, ClaimStatuses.Returned)
                || ClaimStatuses.Equals(detailStatus, ClaimStatuses.Returned))
            {
                currentStatus = ClaimStatuses.Equals(detailStatus, ClaimStatuses.Returned)
                    ? ClaimStatuses.Returned
                    : claimStatus;
                return true;
            }

            currentStatus = !string.IsNullOrWhiteSpace(detailStatus) ? detailStatus : claimStatus;
            return false;
        }

        private void HandleEditRowCommand(string commandArgument)
        {
            int activityId;
            if (!int.TryParse(commandArgument, out activityId)) return;

            string currentStatus;
            if (!CanPersistActivityKm(activityId, out currentStatus))
            {
                lbl_msg.Text = ClaimStatuses.Equals(currentStatus, ClaimStatuses.Rejected)
                    ? "Cannot edit row. This day was rejected by the auditor."
                    : "Cannot edit row. This claim is " + currentStatus + ".";
                lbl_msg.ForeColor = System.Drawing.Color.Red;
                return;
            }

            int rowIndex = FindRowIndexByActivityId(activityId);
            if (rowIndex >= 0)
            {
                gvActivities.EditIndex = rowIndex;
                Session.Remove(GetCurrentSessionKey());
                BindGrid(false);
            }
        }

        private void HandleUpdateRowCommand(GridViewCommandEventArgs e)
        {
            int activityId = Convert.ToInt32(e.CommandArgument);

            string currentStatus;
            if (!CanPersistActivityKm(activityId, out currentStatus))
            {
                lbl_msg.Text = ClaimStatuses.Equals(currentStatus, ClaimStatuses.Rejected)
                    ? "Update blocked. This day was rejected by the auditor."
                    : "Update blocked. This claim is " + currentStatus + ".";
                lbl_msg.ForeColor = System.Drawing.Color.Red;
                gvActivities.EditIndex = -1;
                BindGrid(false);
                return;
            }

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
                NotifyDraftChangeSaved();
                lbl_msg.ForeColor = System.Drawing.Color.Green;
                lbl_msg.Text = "KM updated.";
            }
            catch (SqlException sqx)
            {
                lbl_msg.ForeColor = System.Drawing.Color.Red;
                lbl_msg.Text = sqx.Message;
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

            string currentStatus;
            bool canPersistKm = CanPersistActivityKm(activityId, out currentStatus);
            if (btnSaveKm != null) btnSaveKm.Enabled = canPersistKm;
            if (!canPersistKm)
            {
                string safeErr = "Cannot modify KMs. The claim for this month is currently " + currentStatus + ".";
                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyLockErrKM", "showPNotify('Locked', '" + safeErr + "', 'error');", true);
            }

            string hideOuter = "$('#expenseModal').modal('hide');";
            ScriptManager.RegisterStartupScript(this, this.GetType(), "hideExpenseBeforeOpenInner_" + activityId, hideOuter, true);
        }

        private void PopulateEditKmModalFromRow(DataRow row)
        {
            int activityId = row["ActivityId"] != DBNull.Value ? Convert.ToInt32(row["ActivityId"]) : 0;
            decimal totalKm = 0m;
            if (row.Table.Columns.Contains("TotalDistanceKM") && row["TotalDistanceKM"] != DBNull.Value)
                totalKm = Convert.ToDecimal(row["TotalDistanceKM"]);

            string currentKm = totalKm.ToString("0.##", CultureInfo.InvariantCulture);
            string activityDate = row.Table.Columns.Contains("ActivityDate") && row["ActivityDate"] != DBNull.Value ? Convert.ToDateTime(row["ActivityDate"]).ToString("dd-MM-yyyy") : "";

            if (hdnActivityId != null) hdnActivityId.Value = activityId.ToString();
            if (txtModalKM != null) txtModalKM.Text = currentKm;
            if (hdnOriginalKm != null) hdnOriginalKm.Value = currentKm;
            if (!string.IsNullOrEmpty(activityDate) && lbl_kmmodal_date != null) lbl_kmmodal_date.Text = activityDate;

            InitializeKmModalSession(activityId);
            BindKmHistory(activityId);

            var tolerance = ResolveToleranceForActivity(activityId, row);
            ConfigureKmModalDisplay(activityId, totalKm, tolerance);

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
                        decimal totalKm = rdr["TotalDistanceKM"] != DBNull.Value ? Convert.ToDecimal(rdr["TotalDistanceKM"]) : 0m;
                        string currentKm = totalKm.ToString("0.##", CultureInfo.InvariantCulture);
                        DateTime? aDate = rdr["ActivityDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(rdr["ActivityDate"]) : null;

                        if (hdnActivityId != null) hdnActivityId.Value = activityId.ToString();
                        if (txtModalKM != null) txtModalKM.Text = currentKm;
                        if (hdnOriginalKm != null) hdnOriginalKm.Value = currentKm;
                        if (aDate.HasValue && lbl_kmmodal_date != null) lbl_kmmodal_date.Text = aDate.Value.ToString("dd-MM-yyyy");

                        InitializeKmModalSession(activityId);
                        BindKmHistory(activityId);

                        var tolerance = ResolveToleranceForActivity(activityId, null);
                        if (rdr["MinAllowedKM"] != DBNull.Value) tolerance.ReductionTolerance = Convert.ToDecimal(rdr["MinAllowedKM"]);
                        if (rdr["MaxAllowedKM"] != DBNull.Value) tolerance.IncreaseTolerance = Convert.ToDecimal(rdr["MaxAllowedKM"]);
                        ConfigureKmModalDisplay(activityId, totalKm, tolerance);
                        return;
                    }
                }
            }
            RegisterClientNotification("Activity not found.", "warning");
        }

        private const string KmSaveWorkflowManual = "Manual";
        private const string KmSaveWorkflowRoute = "Route";

        private string GetKmSaveWorkflow()
        {
            string workflow = HF_KmSaveWorkflow != null ? HF_KmSaveWorkflow.Value : null;
            if (string.IsNullOrWhiteSpace(workflow))
            {
                workflow = Request.Form[HF_KmSaveWorkflow != null ? HF_KmSaveWorkflow.UniqueID : string.Empty];
            }
            return string.IsNullOrWhiteSpace(workflow) ? KmSaveWorkflowManual : workflow.Trim();
        }

        private bool IsRouteBasedKmSave()
        {
            return string.Equals(GetKmSaveWorkflow(), KmSaveWorkflowRoute, StringComparison.OrdinalIgnoreCase);
        }

        private void SetKmSaveWorkflow(string workflow)
        {
            if (HF_KmSaveWorkflow != null) HF_KmSaveWorkflow.Value = workflow;
        }

        private void ClearRouteMetadataFields()
        {
            if (HF_RouteOrigin != null) HF_RouteOrigin.Value = string.Empty;
            if (HF_RouteDestination != null) HF_RouteDestination.Value = string.Empty;
            if (HF_RouteWaypoints != null) HF_RouteWaypoints.Value = string.Empty;
            if (HF_RouteDistance != null) HF_RouteDistance.Value = string.Empty;
            if (HF_RouteTravelMode != null) HF_RouteTravelMode.Value = string.Empty;
        }

        /// <summary>
        /// Resets KM modal to manual-override workflow and clears stale route metadata when the activity changes.
        /// </summary>
        private void InitializeKmModalSession(int activityId)
        {
            string previousRouteActivity = HF_RouteActivityId != null ? HF_RouteActivityId.Value : string.Empty;
            bool activityChanged = activityId > 0 && !string.Equals(previousRouteActivity, activityId.ToString(), StringComparison.Ordinal);

            SetKmSaveWorkflow(KmSaveWorkflowManual);
            if (activityChanged) ClearRouteMetadataFields();

            if (HF_RouteActivityId != null) HF_RouteActivityId.Value = activityId.ToString();

            string hideSummary = activityChanged ? "true" : "false";
            string script = string.Format(
                "if (typeof initializeKmModalSessionClient === 'function') initializeKmModalSessionClient({0}, {1});",
                activityId,
                hideSummary);
            ScriptManager.RegisterStartupScript(this, GetType(), "initKmModalSession_" + activityId, script, true);
        }

        private int GetKmModificationCount(int activityId)
        {
            if (activityId <= 0) return 0;
            using (SqlConnection con = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.SalesDailyActivityKMHistory WITH (NOLOCK) WHERE ActivityId = @ActivityId", con))
            {
                cmd.Parameters.AddWithValue("@ActivityId", activityId);
                con.Open();
                object result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
            }
        }

        private static void ApplyToleranceFromRow(DataRow row, EmployeeKmToleranceConfig config)
        {
            if (row == null || config == null) return;

            if (row.Table.Columns.Contains("MinAllowedKM") && row["MinAllowedKM"] != DBNull.Value)
                config.ReductionTolerance = Convert.ToDecimal(row["MinAllowedKM"]);
            if (row.Table.Columns.Contains("MaxAllowedKM") && row["MaxAllowedKM"] != DBNull.Value)
                config.IncreaseTolerance = Convert.ToDecimal(row["MaxAllowedKM"]);
            if (row.Table.Columns.Contains("MaxKmModificationCount") && row["MaxKmModificationCount"] != DBNull.Value)
                config.MaxModificationCount = Convert.ToInt32(row["MaxKmModificationCount"]);
        }

        private int GetMaxModificationCountForEmployee(string employeeCode)
        {
            if (string.IsNullOrWhiteSpace(employeeCode))
                return KmToleranceHelper.DefaultMaxModificationCount;

            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                using (SqlCommand cmd = new SqlCommand(@"
SELECT TOP 1 ISNULL(MaxKmModificationCount, 1)
FROM dbo.MST_EmployeeTolerance WITH (NOLOCK)
WHERE EmployeeCode = @EmployeeCode
  AND IsActive = 1
  AND (EffectiveFrom IS NULL OR EffectiveFrom <= CAST(GETDATE() AS DATE))
  AND (EffectiveTo IS NULL OR EffectiveTo >= CAST(GETDATE() AS DATE))
ORDER BY EffectiveFrom DESC, ToleranceId DESC", con))
                {
                    cmd.Parameters.AddWithValue("@EmployeeCode", employeeCode.Trim());
                    con.Open();
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        int count = Convert.ToInt32(result);
                        return count < 1 ? KmToleranceHelper.DefaultMaxModificationCount : count;
                    }
                }
            }
            catch
            {
                // Column or table may not be deployed yet; fall back to default.
            }

            return KmToleranceHelper.DefaultMaxModificationCount;
        }

        private EmployeeKmToleranceConfig ResolveToleranceForActivity(int activityId, DataRow row)
        {
            var config = new EmployeeKmToleranceConfig();

            if (row != null)
                ApplyToleranceFromRow(row, config);
            else if (activityId > 0)
            {
                using (SqlConnection con = new SqlConnection(connString))
                using (SqlCommand cmd = new SqlCommand("SELECT TOP 1 MinAllowedKM, MaxAllowedKM FROM dbo.SalesDailyActivity WITH (NOLOCK) WHERE ActivityId = @ActivityId", con))
                {
                    cmd.Parameters.AddWithValue("@ActivityId", activityId);
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (reader["MinAllowedKM"] != DBNull.Value)
                                config.ReductionTolerance = Convert.ToDecimal(reader["MinAllowedKM"]);
                            if (reader["MaxAllowedKM"] != DBNull.Value)
                                config.IncreaseTolerance = Convert.ToDecimal(reader["MaxAllowedKM"]);
                        }
                    }
                }
            }

            bool hasModificationLimit = row != null
                && row.Table.Columns.Contains("MaxKmModificationCount")
                && row["MaxKmModificationCount"] != DBNull.Value;

            if (!hasModificationLimit)
                config.MaxModificationCount = GetMaxModificationCountForEmployee(GetLoggedInEmployeeCode());

            return config;
        }

        private void ConfigureKmModalDisplay(int activityId, decimal currentKm, EmployeeKmToleranceConfig tolerance)
        {
            decimal originalKm = currentKm;
            if (hdnOriginalKm != null && !string.IsNullOrWhiteSpace(hdnOriginalKm.Value))
            {
                decimal parsedOriginal;
                if (decimal.TryParse(hdnOriginalKm.Value.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out parsedOriginal))
                    originalKm = parsedOriginal;
            }

            BindKmPolicyInformation(activityId, originalKm, currentKm, tolerance);

            int usedCount = GetKmModificationCount(activityId);
            bool canSave = usedCount < tolerance.MaxModificationCount;
            if (btnSaveKm != null) btnSaveKm.Enabled = canSave;

            decimal? allowedMin;
            decimal? allowedMax;
            KmToleranceHelper.ComputeAllowedRange(currentKm, tolerance.ReductionTolerance, tolerance.IncreaseTolerance, out allowedMin, out allowedMax);
            string minText = allowedMin.HasValue ? allowedMin.Value.ToString("0.##", CultureInfo.InvariantCulture) : "—";
            string maxText = allowedMax.HasValue ? allowedMax.Value.ToString("0.##", CultureInfo.InvariantCulture) : "—";

            string script = string.Format(
                "setTimeout(function(){{ $('#editKmModal').modal('show'); $('#spMinKm').text('{0}'); $('#spMaxKm').text('{1}'); $('#{2}').prop('disabled', {3}); }}, 150);",
                minText.Replace("'", "\\'"),
                maxText.Replace("'", "\\'"),
                btnSaveKm != null ? btnSaveKm.ClientID : string.Empty,
                canSave ? "false" : "true");

            ScriptManager.RegisterStartupScript(this, this.GetType(), "openEditKmModal_" + activityId, script, true);
        }

        private void BindKmPolicyInformation(int activityId, decimal originalKm, decimal currentKm, EmployeeKmToleranceConfig tolerance)
        {
            decimal? allowedMin;
            decimal? allowedMax;
            KmToleranceHelper.ComputeAllowedRange(currentKm, tolerance.ReductionTolerance, tolerance.IncreaseTolerance, out allowedMin, out allowedMax);

            int usedCount = GetKmModificationCount(activityId);
            string remaining = KmToleranceHelper.FormatRemaining(usedCount, tolerance.MaxModificationCount);
            bool hasPolicyInfo = allowedMin.HasValue || allowedMax.HasValue || tolerance.MaxModificationCount > 0;

            if (pnlKmPolicyInfo != null)
                pnlKmPolicyInfo.Visible = hasPolicyInfo;

            if (lblModalOriginalKm != null)
                lblModalOriginalKm.Text = originalKm.ToString("0.##", CultureInfo.InvariantCulture) + " km";
            if (lblModalCurrentKm != null)
                lblModalCurrentKm.Text = currentKm.ToString("0.##", CultureInfo.InvariantCulture) + " km";
            if (lblModalAllowedRange != null)
                lblModalAllowedRange.Text = hasPolicyInfo
                    ? KmToleranceHelper.FormatRange(allowedMin, allowedMax) + " km"
                    : "—";
            if (lblKmModLimit != null)
                lblKmModLimit.Text = tolerance.MaxModificationCount.ToString();
            if (lblKmModRemaining != null)
                lblKmModRemaining.Text = remaining;
        }

        private void BindReturnedClaimsBanner(DataTable dt)
        {
            if (pnlReturnedClaimsBanner == null) return;
            pnlReturnedClaimsBanner.Visible = CountReturnedActivities(dt) > 0;
        }

        private static int CountReturnedActivities(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return 0;

            int count = 0;
            foreach (DataRow row in dt.Rows)
            {
                string claimStatus = row.Table.Columns.Contains("ClaimStatus") && row["ClaimStatus"] != DBNull.Value
                    ? row["ClaimStatus"].ToString().Trim()
                    : string.Empty;
                string detailStatus = row.Table.Columns.Contains("DetailStatus") && row["DetailStatus"] != DBNull.Value
                    ? row["DetailStatus"].ToString().Trim()
                    : string.Empty;
                bool isEditable = row.Table.Columns.Contains("IsEditable")
                    && row["IsEditable"] != DBNull.Value
                    && Convert.ToBoolean(row["IsEditable"]);

                string effectiveStatus = ClaimEditabilityHelper.ResolveEffectiveRowStatus(claimStatus, detailStatus, isEditable);
                if (ClaimStatuses.Equals(effectiveStatus, ClaimStatuses.Returned))
                    count++;
            }

            return count;
        }

        private static string GetDetailRemarksFromRow(DataRow row)
        {
            if (row == null) return string.Empty;
            if (row.Table.Columns.Contains("DetailRemarks") && row["DetailRemarks"] != DBNull.Value)
                return row["DetailRemarks"].ToString().Trim();
            return string.Empty;
        }

        private static string GetReturnedByDisplay(DataRow row, string detailRemarks)
        {
            if (row != null && row.Table.Columns.Contains("DetailApprovedBy") && row["DetailApprovedBy"] != DBNull.Value)
            {
                string approvedBy = row["DetailApprovedBy"].ToString().Trim();
                if (!string.IsNullOrWhiteSpace(approvedBy))
                    return HttpUtility.HtmlEncode(approvedBy);
            }

            if (!string.IsNullOrWhiteSpace(detailRemarks))
            {
                int idx = detailRemarks.LastIndexOf(" - Added by ", StringComparison.OrdinalIgnoreCase);
                if (idx > 0)
                {
                    string suffix = detailRemarks.Substring(idx + " - Added by ".Length).Trim();
                    int parenIdx = suffix.LastIndexOf(" (", StringComparison.Ordinal);
                    if (parenIdx > 0)
                        return HttpUtility.HtmlEncode(suffix.Substring(0, parenIdx).Trim());
                }
            }

            return "Approver";
        }

        private static string StripApproverSuffixFromRemark(string remark)
        {
            if (string.IsNullOrWhiteSpace(remark)) return string.Empty;
            int idx = remark.LastIndexOf(" - Added by ", StringComparison.OrdinalIgnoreCase);
            if (idx > 0) return remark.Substring(0, idx).Trim();
            return remark.Trim();
        }

        private static string GetReturnedActionRequiredText(string remark)
        {
            string lower = (remark ?? string.Empty).ToLowerInvariant();
            if (lower.Contains("receipt") || lower.Contains("upload") || lower.Contains("bill") || lower.Contains("attachment"))
                return "Upload receipt and resubmit.";
            if (lower.Contains("km") || lower.Contains("distance") || lower.Contains("kilometer"))
                return "Update travel distance and resubmit.";
            if (lower.Contains("hotel"))
                return "Correct hotel details and resubmit.";
            if (lower.Contains("da") || lower.Contains("allowance"))
                return "Correct daily allowance and resubmit.";
            return "Make the requested corrections and resubmit.";
        }

        private static string BuildReturnedActivityInfoHtml(string returnedBy, string reason, string actionRequired)
        {
            string safeReturnedBy = string.IsNullOrWhiteSpace(returnedBy) ? "Approver" : returnedBy;
            string safeReason = string.IsNullOrWhiteSpace(reason) ? "No reason provided." : reason;
            string safeAction = HttpUtility.HtmlEncode(actionRequired ?? "Make the requested corrections and resubmit.");

            return string.Format(
                @"<div class=""returned-info-panel"">
                    <div class=""returned-info-row""><span class=""returned-info-label"">Returned by</span><span class=""returned-info-value"">{0}</span></div>
                    <div class=""returned-info-row""><span class=""returned-info-label"">Reason</span><span class=""returned-info-value"">{1}</span></div>
                    <div class=""returned-info-row""><span class=""returned-info-label"">Action Required</span><span class=""returned-info-value text-warning font-weight-bold"">{2}</span></div>
                </div>",
                safeReturnedBy,
                reason,
                safeAction);
        }

        private void BindSubmissionChecklist(DataTable dt, bool allowSubmission, bool monthLocked, bool policyWindowOpen)
        {
            if (pnlSubmissionChecklist == null || phSubmissionChecklist == null) return;

            bool hasActivities = dt != null && dt.Rows.Count > 0;
            bool policyEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings["EnablePreviousMonthPolicy"]);
            bool submissionWindowOpen = !policyEnabled || policyWindowOpen;

            int draftCount = 0;
            int returnedCorrectionCount = 0;

            if (hasActivities)
            {
                foreach (DataRow row in dt.Rows)
                {
                    string claimStatus = row.Table.Columns.Contains("ClaimStatus") && row["ClaimStatus"] != DBNull.Value
                        ? row["ClaimStatus"].ToString().Trim()
                        : string.Empty;
                    string detailStatus = row.Table.Columns.Contains("DetailStatus") && row["DetailStatus"] != DBNull.Value
                        ? row["DetailStatus"].ToString().Trim()
                        : string.Empty;
                    bool isEditable = row.Table.Columns.Contains("IsEditable")
                        && row["IsEditable"] != DBNull.Value
                        && Convert.ToBoolean(row["IsEditable"]);

                    if (ClaimStatuses.Equals(claimStatus, ClaimStatuses.Draft))
                        draftCount++;

                    if (isEditable && (ClaimStatuses.Equals(detailStatus, ClaimStatuses.Returned) || ClaimStatuses.Equals(claimStatus, ClaimStatuses.Returned)))
                        returnedCorrectionCount++;
                }
            }

            bool activitiesReady = hasActivities;
            bool approvalGateOpen = !monthLocked || allowSubmission;
            bool submitReady = allowSubmission;

            var items = new List<Tuple<string, bool, string>>();
            items.Add(Tuple.Create(
                activitiesReady ? "Daily activities loaded" : "Load daily activities for the selected month",
                activitiesReady,
                hasActivities ? string.Format("{0} working day(s) found", dt.Rows.Count) : "Click Search to load activities"));

            items.Add(Tuple.Create(
                submissionWindowOpen ? "Submission window is open" : "Submission window is closed",
                submissionWindowOpen,
                policyEnabled ? "Allowed between 1st and 15th for the previous month" : "Monthly processing policy is disabled"));

            if (monthLocked && allowSubmission && returnedCorrectionCount > 0)
            {
                items.Add(Tuple.Create(
                    string.Format("{0} returned day(s) awaiting correction", returnedCorrectionCount),
                    false,
                    "Edit highlighted rows and resubmit when ready"));
            }
            else if (monthLocked && !allowSubmission)
            {
                items.Add(Tuple.Create(
                    "Monthly claim is with approvers",
                    false,
                    "Editing is disabled until approver action"));
            }
            else
            {
                items.Add(Tuple.Create(
                    "No approval lock blocking submission",
                    approvalGateOpen,
                    approvalGateOpen ? "You may submit or continue editing" : "Submission is currently blocked"));
            }

            if (draftCount > 0 && !monthLocked)
            {
                items.Add(Tuple.Create(
                    string.Format("{0} day(s) still in Draft", draftCount),
                    true,
                    "Draft days will be included when you submit"));
            }

            items.Add(Tuple.Create(
                submitReady ? "Ready to submit for approval" : "Submit for approval is not available yet",
                submitReady,
                submitReady ? "Use the Submit for Approval button when you are done" : "Resolve the items above before submitting"));

            phSubmissionChecklist.Controls.Clear();
            foreach (var item in items)
            {
                string iconClass = item.Item2 ? "check-pass" : "check-fail";
                string icon = item.Item2 ? "✓" : "✗";
                string detail = string.IsNullOrWhiteSpace(item.Item3) ? string.Empty : string.Format(" <span class=\"text-muted\">— {0}</span>", HttpUtility.HtmlEncode(item.Item3));

                phSubmissionChecklist.Controls.Add(new Literal
                {
                    Text = string.Format(
                        "<div class=\"check-item\"><span class=\"check-icon {0}\">{1}</span><span><strong>{2}</strong>{3}</span></div>",
                        iconClass,
                        icon,
                        HttpUtility.HtmlEncode(item.Item1),
                        detail)
                });
            }

            pnlSubmissionChecklist.Visible = hasActivities;
        }

        private bool TryValidateKmModification(
            int activityId,
            decimal currentKm,
            decimal newKm,
            EmployeeKmToleranceConfig tolerance,
            out string errorMessage)
        {
            errorMessage = null;
            int usedCount = GetKmModificationCount(activityId);

            if (usedCount >= tolerance.MaxModificationCount)
            {
                errorMessage = "You have already utilized the maximum allowed KM modifications for this activity.";
                return false;
            }

            if (!KmToleranceHelper.IsKmWithinTolerance(newKm, currentKm, tolerance.ReductionTolerance, tolerance.IncreaseTolerance))
            {
                decimal? allowedMin;
                decimal? allowedMax;
                KmToleranceHelper.ComputeAllowedRange(currentKm, tolerance.ReductionTolerance, tolerance.IncreaseTolerance, out allowedMin, out allowedMax);
                errorMessage = string.Format("KM value {0} is outside the allowed range ({1} km).", newKm.ToString("0.##"), KmToleranceHelper.FormatRange(allowedMin, allowedMax));
                return false;
            }

            return true;
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

            bool isRouteBasedSave = IsRouteBasedKmSave();

            if (isRouteBasedSave)
            {
                decimal routeDistance;

                if (!decimal.TryParse(HF_RouteDistance != null ? HF_RouteDistance.Value : string.Empty, out routeDistance))
                {
                    ScriptManager.RegisterStartupScript(
                        this,
                        GetType(),
                        "RouteKmMissing",
                        "showPNotify('Validation','Route distance is missing. Please apply a calculated route before saving.','error'); $('#editKmModal').modal('show');",
                        true);
                    return;
                }

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
            else
            {
                ClearRouteMetadataFields();
            }

            string currentStatus;
            if (!CanPersistActivityKm(activityId, out currentStatus))
            {
                string safeErr = "Cannot modify KMs. The claim for this month is currently " + currentStatus + ".";
                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyLockErrKM", "showPNotify('Locked', '" + safeErr + "', 'error');", true);
                return;
            }
            string modifiedBy = GetCurrentUserLoginId();

            DataRow sessionRow = null;
            DataTable sessionDt = Session[GetCurrentSessionKey()] as DataTable;
            if (sessionDt != null)
            {
                if (sessionDt.PrimaryKey != null && sessionDt.PrimaryKey.Length > 0)
                    sessionRow = sessionDt.Rows.Find(activityId);
                else
                {
                    foreach (DataRow r in sessionDt.Rows)
                    {
                        if (r["ActivityId"] != DBNull.Value && Convert.ToInt32(r["ActivityId"]) == activityId) { sessionRow = r; break; }
                    }
                }
            }

            var tolerance = ResolveToleranceForActivity(activityId, sessionRow);
            string validationError;
            if (!TryValidateKmModification(activityId, origKm, newKm, tolerance, out validationError))
            {
                string safeErr = HttpUtility.JavaScriptStringEncode(validationError);
                ScriptManager.RegisterStartupScript(this, GetType(), "kmValidationFailed",
                    "showPNotify('Validation', '" + safeErr + "', 'error'); $('#editKmModal').modal('show');", true);
                return;
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

                        if (isRouteBasedSave)
                        {
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
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@RouteOriginName", DBNull.Value);
                            cmd.Parameters.AddWithValue("@RouteDestinationName", DBNull.Value);
                            cmd.Parameters.AddWithValue("@RouteWaypointsJSON", DBNull.Value);
                            cmd.Parameters.AddWithValue("@TravelMode", DBNull.Value);
                        }

                        cmd.ExecuteNonQuery();
                    }
                }

                Session.Remove(GetCurrentSessionKey());
                BindGrid(false);
                NotifyDraftChangeSaved();
                BindKmHistory(activityId);
                if (txtKmReason != null) txtKmReason.Text = string.Empty;
                if (hdnOriginalKm != null) hdnOriginalKm.Value = newKm.ToString("0.##", CultureInfo.InvariantCulture);
                if (txtModalKM != null) txtModalKM.Text = newKm.ToString("0.##", CultureInfo.InvariantCulture);

                if (!isRouteBasedSave)
                {
                    SetKmSaveWorkflow(KmSaveWorkflowManual);
                    string resetRouteScript = "if (typeof clearKmRouteMetadataFields === 'function') clearKmRouteMetadataFields();";
                    ScriptManager.RegisterStartupScript(this, GetType(), "clearKmRouteAfterManualSave_" + activityId, resetRouteScript, true);
                }

                ConfigureKmModalDisplay(activityId, newKm, tolerance);
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
                                lblStatus.Text = "<span class='claims-badge claims-badge-pending' data-toggle='tooltip' title='Draft – not submitted yet'><i class='fa fa-pencil-square-o'></i> Draft</span>";
                                break;
                            case "submitted":
                                lblStatus.Text = "<span class='claims-badge claims-badge-pending' data-toggle='tooltip' title='Submitted – awaiting approval'><i class='fa fa-paper-plane'></i> Submitted</span>";
                                break;
                            case "approved":
                                lblStatus.Text = "<span class='claims-badge claims-badge-approved' data-toggle='tooltip' title='Approved by manager'><i class='fa fa-check-circle'></i> Approved</span>";
                                break;
                            case "returned":
                                lblStatus.Text = $"<span class='claims-badge claims-badge-returned' data-toggle='tooltip' title='Returned for correction'><i class='fa fa-undo'></i> Returned</span>";
                                if (!string.IsNullOrWhiteSpace(remark))
                                {
                                    var lblReturnedReason = e.Row.FindControl("lblReturnedReasonInline") as Label;
                                    if (lblReturnedReason != null)
                                    {
                                        lblReturnedReason.Visible = true;
                                        lblReturnedReason.Text = "Reason: " + HttpUtility.HtmlEncode(remark);
                                    }
                                }
                                break;
                            case "rejected":
                                lblStatus.Text = $"<span class='claims-badge claims-badge-rejected' data-toggle='tooltip' title='Rejected – please review{(string.IsNullOrWhiteSpace(encodedRemark) ? "" : " – " + encodedRemark)}'><i class='fa fa-times-circle'></i> Rejected</span>";
                                break;
                            case "processed":
                                lblStatus.Text = "<span class='claims-badge claims-badge-completed' data-toggle='tooltip' title='Processed in accounts'><i class='fa fa-cogs'></i> Processed</span>";
                                break;
                            default:
                                lblStatus.Text = $"<span class='claims-badge claims-badge-default' data-toggle='tooltip' title='{HttpUtility.HtmlEncode(string.IsNullOrWhiteSpace(effectiveStatus) ? "Unknown status" : effectiveStatus)}'><i class='fa fa-clock-o'></i> {HttpUtility.HtmlEncode(effectiveStatus)}</span>";
                                break;
                        }
                    }

                    PopulateKmTolerance(drv, e.Row);
                    ApplyApprovalEditability(drv, e.Row);
                }
            }
        }

        protected void gvActivities_DataBound(object sender, EventArgs e)
        {
            if (gvActivities == null || gvActivities.Rows.Count == 0) return;

            string sessionKey = GetCurrentSessionKey();
            DataTable dt = Session[sessionKey] as DataTable;
            if (dt == null || !dt.Columns.Contains("ActivityId")) return;

            for (int i = gvActivities.Rows.Count - 1; i >= 0; i--)
            {
                GridViewRow row = gvActivities.Rows[i];
                if (row.RowType != DataControlRowType.DataRow) continue;
                if (!row.CssClass.Contains("returned-activity-row")) continue;

                if (gvActivities.DataKeys == null || gvActivities.DataKeys.Count <= i) continue;
                int activityId = Convert.ToInt32(gvActivities.DataKeys[i].Value);

                DataRow dataRow = null;
                try { dataRow = dt.Rows.Find(activityId); }
                catch { /* ignore */ }

                if (dataRow == null)
                {
                    foreach (DataRow candidate in dt.Rows)
                    {
                        if (candidate["ActivityId"] != DBNull.Value && Convert.ToInt32(candidate["ActivityId"]) == activityId)
                        {
                            dataRow = candidate;
                            break;
                        }
                    }
                }

                if (dataRow == null) continue;

                string rawRemark = GetDetailRemarksFromRow(dataRow);
                string returnedBy = GetReturnedByDisplay(dataRow, rawRemark);
                string reason = HttpUtility.HtmlEncode(StripApproverSuffixFromRemark(rawRemark));
                string actionRequired = GetReturnedActionRequiredText(rawRemark);
                string panelHtml = BuildReturnedActivityInfoHtml(returnedBy, reason, actionRequired);

                var detailRow = new GridViewRow(-1, -1, DataControlRowType.DataRow, DataControlRowState.Normal);
                detailRow.CssClass = "returned-activity-detail-row";

                var cell = new TableCell
                {
                    ColumnSpan = gvActivities.Columns.Count,
                    CssClass = "p-0 border-top-0"
                };
                cell.Controls.Add(new Literal { Text = panelHtml });
                detailRow.Cells.Add(cell);

                var table = row.Parent as Table;
                if (table == null) continue;

                int tableIndex = table.Rows.GetRowIndex(row);
                table.Rows.AddAt(tableIndex + 1, detailRow);
            }
        }

        private static string ResolveEffectiveRowStatus(string claimStatus, string detailStatus, DataRowView drv)
        {
            bool isEditable = drv.Row.Table.Columns.Contains("IsEditable") &&
                              drv["IsEditable"] != DBNull.Value &&
                              Convert.ToBoolean(drv["IsEditable"]);
            return ClaimEditabilityHelper.ResolveEffectiveRowStatus(claimStatus, detailStatus, isEditable);
        }

        private bool IsActivityEditableForExpenseDelete(int activityId)
        {
            if (activityId <= 0) return false;

            string claimStatus = ClaimStatuses.Draft;
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

            return ClaimEditabilityHelper.CanDeleteExpenseForActivity(claimStatus, detailStatus);
        }

        private const string SoftDeleteExpenseSql = @"
            UPDATE dbo.SalesDailyExpense
            SET IsDeleted = 1,
                ClaimStatus = '" + ClaimStatuses.Draft + @"',
                ModifiedBy = @User,
                ModifiedOn = SYSUTCDATETIME(),
                DeletedBy = @User,
                DeletedOn = DATEADD(MINUTE, 330, SYSUTCDATETIME()),
                DeleteReason = @Reason
            WHERE ExpenseId = @ExpenseId
              AND IsDeleted = 0
              AND ClaimStatus IN ('" + ClaimStatuses.Draft + @"', '" + ClaimStatuses.Returned + @"')";

        private static string BuildSoftDeleteExpenseBatchSql(IEnumerable<int> expenseIds)
        {
            string inList = string.Join(",", expenseIds.Select(id => id.ToString()).ToArray());
            return string.Format(@"
                UPDATE dbo.SalesDailyExpense
                SET IsDeleted = 1,
                    ClaimStatus = '" + ClaimStatuses.Draft + @"',
                    ModifiedBy = @User,
                    ModifiedOn = SYSUTCDATETIME(),
                    DeletedBy = @User,
                    DeletedOn = DATEADD(MINUTE, 330, SYSUTCDATETIME()),
                    DeleteReason = @Reason
                WHERE ExpenseId IN ({0})
                  AND IsDeleted = 0
                  AND ClaimStatus IN ('" + ClaimStatuses.Draft + @"', '" + ClaimStatuses.Returned + @"')", inList);
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

            decimal? allowedLower;
            decimal? allowedUpper;
            KmToleranceHelper.ComputeAllowedRange(totalKm, minTol, maxTol, out allowedLower, out allowedUpper);

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

            string effectiveVisualStatus = ClaimEditabilityHelper.ResolveEffectiveVisualStatus(claimStatus, detailStatus, isEditable);

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
                bool showEdit = ClaimEditabilityHelper.CanEditActivity(claimStatus, detailStatus, isEditable);

                lnkEdit.Visible = showEdit; lnkEdit.Enabled = showEdit; lnkEdit.CssClass = neutralBtnClass;
                lnkEdit.Text = editIconHtml; lnkEdit.ToolTip = showEdit ? "Edit" : "Edit (disabled)";
            }

            if (lnkExpenses != null)
            {
                bool canOpenExpenses = ClaimEditabilityHelper.CanOpenExpensesForActivity(claimStatus, detailStatus);
                lnkExpenses.Visible = true; lnkExpenses.Enabled = canOpenExpenses; lnkExpenses.CssClass = neutralBtnClass;
                lnkExpenses.Text = expenseIconHtml;
                lnkExpenses.ToolTip = canOpenExpenses ? "View / Add Expenses" : "Rejected days cannot be edited";
            }

            if (txtKM != null) { txtKM.ReadOnly = !ClaimEditabilityHelper.CanEditActivityKmInGrid(isEditable, claimStatus, detailStatus); txtKM.CssClass = txtKM.CssClass + (isEditable ? " editable" : " readonly"); }
            if (lnkUpdate != null) lnkUpdate.Enabled = ClaimEditabilityHelper.CanEditActivityKmInGrid(isEditable, claimStatus, detailStatus);
            if (lnkCancel != null) lnkCancel.Enabled = true;
            if (lblKM != null) { lblKM.Text = drv["TotalDistanceKM"] != DBNull.Value ? Convert.ToDecimal(drv["TotalDistanceKM"]).ToString("N2") + " km" : "—"; lblKM.Visible = true; }

            if (ClaimStatuses.Equals(effectiveVisualStatus, ClaimStatuses.Returned))
            {
                row.CssClass = (row.CssClass + " row-returned returned-activity-row table-warning").Trim();
                row.ToolTip = "Returned for correction. Edit this day and resubmit.";
            }
            else if (ClaimStatuses.Equals(effectiveVisualStatus, ClaimStatuses.Rejected))
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

            decimal currentKm = 0m;
            DataTable sessionDt = Session[GetCurrentSessionKey()] as DataTable;
            DataRow sessionRow = null;
            if (sessionDt != null)
            {
                if (sessionDt.PrimaryKey != null && sessionDt.PrimaryKey.Length > 0)
                    sessionRow = sessionDt.Rows.Find(activityId);
                else
                {
                    DataRow[] arr = sessionDt.Select(string.Format("ActivityId = {0}", activityId));
                    if (arr != null && arr.Length > 0) sessionRow = arr[0];
                }
            }

            if (sessionRow != null && sessionRow.Table.Columns.Contains("TotalDistanceKM") && sessionRow["TotalDistanceKM"] != DBNull.Value)
                decimal.TryParse(sessionRow["TotalDistanceKM"].ToString(), NumberStyles.AllowDecimalPoint, ci, out currentKm);

            var tolerance = ResolveToleranceForActivity(activityId, sessionRow);
            string validationError;
            if (!TryValidateKmModification(activityId, currentKm, newKm, tolerance, out validationError))
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "kmOutOfBounds",
                    "showPNotify('Validation', '" + HttpUtility.JavaScriptStringEncode(validationError) + "', 'error');", true);
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

                var dt = Session[GetCurrentSessionKey()] as DataTable;
                if (dt != null)
                {
                    DataRow found = null;
                    if (dt.PrimaryKey != null && dt.PrimaryKey.Length > 0) found = dt.Rows.Find(activityId);
                    else { DataRow[] sel = dt.Select(string.Format("ActivityId = {0}", activityId)); if (sel != null && sel.Length > 0) found = sel[0]; }

                    if (found != null)
                    {
                        if (dt.Columns.Contains("TotalDistanceKM")) found["TotalDistanceKM"] = newKm;
                        decimal ratePerKm;
                        decimal? fixedTravelAllowance;
                        decimal parsedRate;
                        GetTravelAllowanceInputs(out ratePerKm, out fixedTravelAllowance);
                        if (dt.Columns.Contains("RatePerKm") && found["RatePerKm"] != DBNull.Value && decimal.TryParse(found["RatePerKm"].ToString(), out parsedRate))
                            ratePerKm = parsedRate;
                        decimal calc = TravelAllowanceHelper.CalculateAmount(newKm, ratePerKm, fixedTravelAllowance);
                        if (dt.Columns.Contains("CalculatedClaimAmount")) found["CalculatedClaimAmount"] = calc;
                    }
                }

                gvActivities.EditIndex = -1;
                Session.Remove(GetCurrentSessionKey());
                BindGrid(false, false);
                NotifyDraftChangeSaved();

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
        private void ResetExpenseSaveMessage()
        {
            if (LBL_ExpenseMsg != null)
            {
                LBL_ExpenseMsg.Text = "";
                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
            }
        }

        private void ShowExpenseModal(string scriptKey = "showExpense")
        {
            ScriptManager.RegisterStartupScript(this, this.GetType(), scriptKey, "$('#expenseModal').modal('show');", true);
        }

        private bool TryGetExpenseSaveActivityId(out int activityId)
        {
            string hfAct = HF_ActivityId != null ? HF_ActivityId.Value : "";
            if (int.TryParse(hfAct, out activityId)) return true;

            if (LBL_ExpenseMsg != null) LBL_ExpenseMsg.Text = "Invalid activity.";
            ShowExpenseModal();
            return false;
        }

        private bool TryValidateExpenseSavePermission(int activityId, out string currentStatus)
        {
            currentStatus = GetActivityDetailStatus(activityId);
            if (ClaimEditabilityHelper.CanEditExpenseByDetailStatus(currentStatus)) return true;

            if (LBL_ExpenseMsg != null)
            {
                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                LBL_ExpenseMsg.Text = "Access Denied: You cannot modify expenses for a submitted claim.";
            }
            string safeErr = "Cannot modify expenses. The claim for this month is currently " + currentStatus + ".";
            ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyLockErr", "showPNotify('Locked', '" + safeErr + "', 'error');", true);
            ShowExpenseModal();
            return false;
        }

        private void CompleteExpenseSaveSuccess(int activityId, string policyMode)
        {
            DataTable dtAfter = LoadExpenseForActivity(activityId.ToString());
            Session.Remove(GetCurrentSessionKey());
            BindGrid(false);
            NotifyDraftChangeSaved();

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

        private void HandleExpenseSaveException(Exception ex)
        {
            if (LBL_ExpenseMsg != null)
            {
                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                LBL_ExpenseMsg.Text = ex.Message;
            }
            string safeMessage = ex.Message.Replace("'", "\\'").Replace(Environment.NewLine, " ");
            ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyError2", "showPNotify('Error', '" + safeMessage + "', 'error');", true);
            ShowExpenseModal();
        }

        protected void BTN_SaveExpense_Click(object sender, EventArgs e)
        {
            ResetExpenseSaveMessage();

            int activityId;
            if (!TryGetExpenseSaveActivityId(out activityId)) return;

            string currentStatus;
            if (!TryValidateExpenseSavePermission(activityId, out currentStatus)) return;

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

                CompleteExpenseSaveSuccess(activityId, policyMode);
            }
            catch (Exception ex)
            {
                HandleExpenseSaveException(ex);
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
            Literal litReturnRemarks = e.Item.FindControl("litExpenseReturnRemarks") as Literal;
            var trExpenseRow = e.Item.FindControl("trExpenseRow") as System.Web.UI.HtmlControls.HtmlTableRow;

            bool canDelete = ClaimEditabilityHelper.CanDeleteExpense(claimStatus, isAuto);

            if (lnkDelete != null) lnkDelete.Visible = canDelete;
            if (lnkDeleteDisabled != null) lnkDeleteDisabled.Visible = !canDelete;

            if (litBadges != null)
            {
                var sb = new System.Text.StringBuilder();
                if (!string.IsNullOrWhiteSpace(policyMode)) sb.AppendFormat("<span class='badge badge-info mr-1'>{0}</span>", HttpUtility.HtmlEncode(policyMode));
                if (isAuto) sb.Append("<span class='badge badge-warning mr-1'>AUTO</span>");
                if (!string.IsNullOrWhiteSpace(claimStatus))
                {
                    if (ClaimStatuses.Equals(claimStatus, ClaimStatuses.Returned))
                    {
                        sb.Append("<span class='badge badge-danger mr-1'><i class='fa fa-exclamation-circle'></i> Returned</span>");
                    }
                    else if (ClaimStatuses.Equals(claimStatus, ClaimStatuses.Approved))
                    {
                        sb.Append("<span class='badge badge-success mr-1'><i class='fa fa-check-circle'></i> Approved</span>");
                    }
                    else if (ClaimStatuses.Equals(claimStatus, ClaimStatuses.Rejected))
                    {
                        sb.AppendFormat("<span class='badge badge-danger mr-1'>{0}</span>", HttpUtility.HtmlEncode(claimStatus));
                    }
                    else
                    {
                        sb.AppendFormat("<span class='badge badge-secondary mr-1'>{0}</span>", HttpUtility.HtmlEncode(claimStatus));
                    }
                }
                litBadges.Text = sb.ToString();
            }

            string expenseRemarks = r.Table.Columns.Contains("Remarks") && r["Remarks"] != DBNull.Value
                ? r["Remarks"].ToString().Trim()
                : string.Empty;

            if (ClaimStatuses.Equals(claimStatus, ClaimStatuses.Returned))
            {
                if (trExpenseRow != null)
                    trExpenseRow.Attributes["class"] = "returned-expense-row table-warning";

                if (litReturnRemarks != null)
                {
                    string displayRemark = StripApproverSuffixFromRemark(expenseRemarks);
                    if (!string.IsNullOrWhiteSpace(displayRemark))
                    {
                        litReturnRemarks.Visible = true;
                        litReturnRemarks.Text = string.Format(
                            "<span class='expense-return-remarks'><i class='fa fa-comment-o'></i> {0}</span>",
                            HttpUtility.HtmlEncode(displayRemark));
                    }
                }
            }
            else if (ClaimStatuses.Equals(claimStatus, ClaimStatuses.Approved))
            {
                if (trExpenseRow != null)
                    trExpenseRow.Attributes["class"] = "expense-approved-row table-success";
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
            using (var cmd = new SqlCommand(@"SELECT ExpenseId, ActivityId, ExpenseType, ExpenseDate, FromDate, ToDate, BillNo, Amount, Currency, Purpose, AttachmentFileName, AttachmentPath, TravelType, FromPlace, ToPlace, TravelDate, PolicyMode, PolicySnapshot, IsAutoDA, ClaimStatus, Remarks FROM dbo.SalesDailyExpense WHERE ActivityId = @ActivityId AND IsDeleted = 0 ORDER BY ExpenseDate DESC, ExpenseId DESC", con))
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
            using (var cmd = new SqlCommand(@"SELECT ExpenseId, ActivityId, ExpenseType, ExpenseDate, FromDate, ToDate, BillNo, Amount, Currency, Purpose, AttachmentFileName, AttachmentPath, TravelType, FromPlace, ToPlace, TravelDate, PolicyMode, PolicySnapshot, IsAutoDA, ClaimStatus, Remarks FROM dbo.SalesDailyExpense WHERE ActivityId = @ActivityId AND IsDeleted = 0 ORDER BY ExpenseDate DESC, ExpenseId DESC", con))
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
                        if (HF_RouteActivityId != null) HF_RouteActivityId.Value = activityId.ToString();

                        // Route metadata is loaded for display only; KM save workflow stays Manual until Apply Route.

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
                HandleDeleteExpenseCommand(e);
        }

        private void HandleDeleteExpenseCommand(RepeaterCommandEventArgs e)
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

                        if (!ClaimEditabilityHelper.IsRemovableExpenseClaimStatus(expenseClaimStatus))
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
                                    string findAutoSql = @"SELECT ExpenseId FROM dbo.SalesDailyExpense WITH (UPDLOCK, HOLDLOCK) WHERE ActivityId = @ActivityId AND ExpenseType = 'DA' AND IsAutoDA = 1 AND ClaimStatus IN ('" + ClaimStatuses.Draft + @"', '" + ClaimStatuses.Returned + @"') AND IsDeleted = 0 AND CAST(ExpenseDate AS date) BETWEEN @FromDate AND @ToDate";
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
                                NotifyDraftChangeSaved();

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

                        if (!ClaimEditabilityHelper.IsRemovableExpenseClaimStatus(expenseClaimStatus))
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
                                    string findAutoSql = @"SELECT ExpenseId FROM dbo.SalesDailyExpense WITH (UPDLOCK, HOLDLOCK) WHERE ActivityId = @ActivityId AND ExpenseType = 'DA' AND IsAutoDA = 1 AND ClaimStatus IN ('" + ClaimStatuses.Draft + @"', '" + ClaimStatuses.Returned + @"') AND IsDeleted = 0 AND CAST(ExpenseDate AS date) BETWEEN @FromDate AND @ToDate";
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
                                NotifyDraftChangeSaved();

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

        private void ShowPolicyNotification(ResolvedPolicyDto policy)
        {
            if (policy == null) return;
            var sb = new StringBuilder();
            bool isException = policy.IsPolicyExceptionApplied || policy.Source == "EX";
            sb.AppendFormat("<strong>Policy applied:</strong> {0}<br/>", isException ? "Employee Exception" : "Policy Matrix");
            sb.AppendFormat("<strong>Specificity:</strong> {0}<br/>", policy.Specificity ?? 0);
            if (policy.Resolved_RatePerKm != null) sb.AppendFormat("<strong>Rate/km:</strong> ₹{0:N2}<br/>", policy.Resolved_RatePerKm);
            if (policy.Resolved_MaxKM != null) sb.AppendFormat("<strong>Max KM (month):</strong> {0:N0}<br/>", policy.Resolved_MaxKM);
            if (policy.Resolved_DA != null) sb.AppendFormat("<strong>DA:</strong> ₹{0:N0}<br/>", policy.Resolved_DA);
            if (policy.Resolved_OutstationDA != null) sb.AppendFormat("<strong>Outstation DA:</strong> ₹{0:N0}<br/>", policy.Resolved_OutstationDA);
            if (policy.Resolved_OwnAllowance != null) sb.AppendFormat("<strong>Own Arrangement Allowance:</strong> ₹{0:N0}<br/>", policy.Resolved_OwnAllowance);
            if (policy.Resolved_HotelCap != null) sb.AppendFormat("<strong>Hotel Cap:</strong> ₹{0:N0}<br/>", policy.Resolved_HotelCap);
            if (!string.IsNullOrEmpty(policy.Resolved_Remarks)) sb.AppendFormat("<strong>Remarks:</strong> {0}<br/>", HttpUtility.JavaScriptStringEncode(policy.Resolved_Remarks));

            string title = isException ? "Employee exception applied" : "Policy applied";
            string safeTitle = HttpUtility.JavaScriptStringEncode(title);
            string safeText = HttpUtility.JavaScriptStringEncode(sb.ToString());
            string notifyType = isException ? "warning" : "info";

            string js = "(function() { try { if (typeof showPNotify === 'function') { showPNotify('" + safeTitle + "', '" + safeText + "', '" + notifyType + "'); } } catch(e) { } })();";
            ScriptManager.RegisterStartupScript(this.Page, this.Page.GetType(), "policyNotify_" + Guid.NewGuid().ToString("N"), js, true);
        }

        // -------------------------------------------------------------------------
        // Resolved Policy self-loading API (Phase 1)
        // Returns structured JSON for client-side modal (and future consumers).
        // Does not change PolicyResolutionService / DataAccess business rules.
        // -------------------------------------------------------------------------
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object GetResolvedPolicy(int year, int month)
        {
            try
            {
                if (HttpContext.Current == null || HttpContext.Current.Session == null)
                    return new { success = false, message = "Session expired. Please sign in again.", policy = (object)null, claimLimits = new object[0], approvalHierarchy = new object[0] };

                string employeeCode = HttpContext.Current.Session["WORKMAN"] as string;
                if (string.IsNullOrWhiteSpace(employeeCode))
                    return new { success = false, message = "Employee context is not available.", policy = (object)null, claimLimits = new object[0], approvalHierarchy = new object[0] };

                if (year < 2000 || year > 2100 || month < 1 || month > 12)
                    return new { success = false, message = "Invalid year or month.", policy = (object)null, claimLimits = new object[0], approvalHierarchy = new object[0] };

                int? regionId = SessionInt("REGION");
                int? branchId = SessionInt("BRANCH");
                int? gradeId = SessionInt("GRADE");
                int? designationId = SessionInt("DESG");
                DateTime claimDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));

                var service = new PolicyResolutionService();
                ResolvedPolicyDto dto = service.ResolvePolicy(
                    employeeCode.Trim(),
                    claimDate,
                    regionId,
                    branchId,
                    gradeId,
                    designationId);

                if (dto == null)
                    return new { success = false, message = "No resolved policy found for the selected context.", policy = (object)null, claimLimits = new object[0], approvalHierarchy = new object[0] };

                string regionName = LookupMasterName("MST_Region", "Id", "Region_Description", dto.RegionId) ?? "All Regions";
                string branchName = LookupMasterName("MST_Branch", "Id", "Branch_Description", dto.BranchId) ?? "All Branches";
                string gradeName = LookupMasterName("MST_Grade", "Id", "Grade_Description", dto.GradeId) ?? "All Grades";
                string desigName = LookupMasterName("MST_Designation", "Id", "Designation_Description", dto.DesignationId) ?? "All Designations";

                string sourceLabel = !string.IsNullOrEmpty(dto.PolicySource)
                    ? dto.PolicySource
                    : (!string.IsNullOrEmpty(dto.Source) ? dto.Source : "HR-SOP");
                bool isActive = dto.IsActive.HasValue && dto.IsActive.Value;
                string effFrom = dto.EffectiveFrom.HasValue ? dto.EffectiveFrom.Value.ToString("dd-MMM-yyyy") : "—";
                string effTo = dto.EffectiveTo.HasValue ? dto.EffectiveTo.Value.ToString("dd-MMM-yyyy") : "—";
                string specificity = string.Join(" → ", new[] { regionName, branchName, gradeName, desigName }
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p.Trim())
                    .ToArray());
                if (string.IsNullOrWhiteSpace(specificity)) specificity = "—";

                var policy = new
                {
                    employeeCode = !string.IsNullOrEmpty(dto.EmployeeCode) ? dto.EmployeeCode : employeeCode.Trim(),
                    rowId = dto.RowId,
                    source = sourceLabel,
                    priority = dto.Priority,
                    isActive = isActive,
                    isActiveLabel = isActive ? "Active" : "Inactive",
                    effectiveFrom = dto.EffectiveFrom,
                    effectiveTo = dto.EffectiveTo,
                    effectivePeriod = string.Format("{0} to {1}", effFrom, effTo),
                    specificity = specificity,
                    designation = desigName,
                    grade = gradeName,
                    regionId = dto.RegionId,
                    branchId = dto.BranchId,
                    gradeId = dto.GradeId,
                    designationId = dto.DesignationId,
                    remarks = dto.Resolved_Remarks,
                    policyJson = dto.PolicyJson,
                    isPolicyExceptionApplied = dto.IsPolicyExceptionApplied,
                    policyExceptionId = dto.PolicyExceptionId,
                    resolved_MaxKM = dto.Resolved_MaxKM,
                    resolved_RatePerKm = dto.Resolved_RatePerKm,
                    resolved_DA = dto.Resolved_DA,
                    resolved_HotelCap = dto.Resolved_HotelCap,
                    resolved_OutstationDA = dto.Resolved_OutstationDA,
                    resolved_OwnAllowance = dto.Resolved_OwnAllowance,
                    fixedTravelAllowance = dto.FixedTravelAllowance,
                    // Values for refreshing expense-related hidden fields on the client
                    hiddenFields = new
                    {
                        hotelCap = dto.Resolved_HotelCap.HasValue ? dto.Resolved_HotelCap.Value.ToString("F2", CultureInfo.InvariantCulture) : "0",
                        da = dto.Resolved_DA.HasValue ? dto.Resolved_DA.Value.ToString("F2", CultureInfo.InvariantCulture) : "200",
                        outstationDA = dto.Resolved_OutstationDA.HasValue ? dto.Resolved_OutstationDA.Value.ToString("F2", CultureInfo.InvariantCulture) : "350",
                        ownArrangementDA = dto.Resolved_OwnAllowance.HasValue ? dto.Resolved_OwnAllowance.Value.ToString("F2", CultureInfo.InvariantCulture) : "750",
                        maxKM = dto.Resolved_MaxKM.HasValue ? dto.Resolved_MaxKM.Value.ToString("F2", CultureInfo.InvariantCulture) : "0",
                        ratePerKm = dto.Resolved_RatePerKm.HasValue ? dto.Resolved_RatePerKm.Value.ToString("F2", CultureInfo.InvariantCulture) : "0",
                        fixedTravelAllowance = dto.FixedTravelAllowance.HasValue ? dto.FixedTravelAllowance.Value.ToString("F2", CultureInfo.InvariantCulture) : string.Empty,
                        regionId = dto.RegionId.HasValue ? dto.RegionId.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                        branchId = dto.BranchId.HasValue ? dto.BranchId.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                        gradeId = dto.GradeId.HasValue ? dto.GradeId.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                        designationId = dto.DesignationId.HasValue ? dto.DesignationId.Value.ToString(CultureInfo.InvariantCulture) : string.Empty
                    }
                };

                var claimLimits = BuildClaimLimitItems(dto);
                var approvalHierarchy = BuildApprovalHierarchyItems();

                return new
                {
                    success = true,
                    message = (string)null,
                    policy = policy,
                    claimLimits = claimLimits,
                    approvalHierarchy = approvalHierarchy
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = "Unable to resolve policy: " + ex.Message,
                    policy = (object)null,
                    claimLimits = new object[0],
                    approvalHierarchy = new object[0]
                };
            }
        }

        private static int? SessionInt(string key)
        {
            if (HttpContext.Current == null || HttpContext.Current.Session == null || HttpContext.Current.Session[key] == null)
                return null;
            int value;
            if (int.TryParse(Convert.ToString(HttpContext.Current.Session[key]), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                return value;
            return null;
        }

        private static string LookupMasterName(string tableName, string idColumn, string textColumn, int? id)
        {
            if (!id.HasValue) return null;
            string conn = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (var con = new SqlConnection(conn))
            using (var cmd = new SqlCommand(string.Format("SELECT TOP 1 {0} FROM {1} WHERE {2} = @Id", textColumn, tableName, idColumn), con))
            {
                cmd.Parameters.AddWithValue("@Id", id.Value);
                con.Open();
                object result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value ? Convert.ToString(result) : null;
            }
        }

        private static string LookupEmployeeNameByCode(string employeeCode)
        {
            if (string.IsNullOrWhiteSpace(employeeCode)) return string.Empty;
            string conn = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (var con = new SqlConnection(conn))
            using (var cmd = new SqlCommand("SELECT TOP 1 EmployeeName FROM dbo.MST_UserMaster WITH (NOLOCK) WHERE EmployeeCode = @EmployeeCode", con))
            {
                cmd.Parameters.AddWithValue("@EmployeeCode", employeeCode.Trim());
                con.Open();
                object result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value ? result.ToString().Trim() : string.Empty;
            }
        }

        private static List<object> BuildClaimLimitItems(ResolvedPolicyDto dto)
        {
            var items = new List<object>();
            AddClaimLimitItem(items, "Rate per KM", FormatCurrencyValue(dto.Resolved_RatePerKm));
            AddClaimLimitItem(items, "Head Office DA", FormatCurrencyValue(dto.Resolved_DA));
            AddClaimLimitItem(items, "Outstation DA", FormatCurrencyValue(dto.Resolved_OutstationDA));
            AddClaimLimitItem(items, "Own Arrangement", FormatCurrencyValue(dto.Resolved_OwnAllowance));
            AddClaimLimitItem(items, "Hotel Limit", FormatCurrencyValue(dto.Resolved_HotelCap, "/night"));
            AddClaimLimitItem(items, "Monthly KM Limit", FormatKmLimitValue(dto.Resolved_MaxKM));
            AddClaimLimitItem(items, "Fixed Travel Allowance", FormatCurrencyValue(dto.FixedTravelAllowance));
            return items;
        }

        private static void AddClaimLimitItem(List<object> items, string label, string displayValue)
        {
            if (string.IsNullOrWhiteSpace(displayValue)) return;
            items.Add(new { label = label, value = displayValue });
        }

        private static string FormatCurrencyValue(decimal? amount, string suffix = null)
        {
            if (!amount.HasValue) return null;
            string formatted = string.Format(CultureInfo.GetCultureInfo("en-IN"), "₹{0:N2}", amount.Value);
            return string.IsNullOrEmpty(suffix) ? formatted : formatted + suffix;
        }

        private static string FormatKmLimitValue(decimal? km)
        {
            if (!km.HasValue) return null;
            return string.Format(CultureInfo.InvariantCulture, "{0:N0} KM", km.Value);
        }

        private static List<object> BuildApprovalHierarchyItems()
        {
            var levels = new List<object>();
            AddHierarchyItem(levels, "Manager (L1)", "RONE_NAME", "RONE_CODE");
            AddHierarchyItem(levels, "Regional Manager (L2)", "RTWO_NAME", "RTWO_CODE");
            AddHierarchyItem(levels, "Zonal Manager (L3)", "RTHREE_NAME", "RTHREE_CODE");
            AddHierarchyItem(levels, "Auditor", "AUDITOR_NAME", "AUDITOR_CODE");
            return levels;
        }

        private static void AddHierarchyItem(List<object> levels, string title, string nameSessionKey, string codeSessionKey)
        {
            string name = GetSessionValue(nameSessionKey);
            string code = GetSessionValue(codeSessionKey);
            if (string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(code))
                name = LookupEmployeeNameByCode(code);
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(code)) return;
            levels.Add(new
            {
                title = title,
                name = string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim(),
                code = string.IsNullOrWhiteSpace(code) ? string.Empty : code.Trim()
            });
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
            decimal policyRate;
            decimal? fixedTravelAllowance;
            GetTravelAllowanceInputs(out policyRate, out fixedTravelAllowance);

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

                decimal? reductionTolerance = null;
                decimal? increaseTolerance = null;
                decimal tmp;
                if (found.Table.Columns.Contains("MinAllowedKM") && found["MinAllowedKM"] != DBNull.Value && decimal.TryParse(found["MinAllowedKM"].ToString(), NumberStyles.AllowDecimalPoint, ci, out tmp))
                    reductionTolerance = tmp;
                if (found.Table.Columns.Contains("MaxAllowedKM") && found["MaxAllowedKM"] != DBNull.Value && decimal.TryParse(found["MaxAllowedKM"].ToString(), NumberStyles.AllowDecimalPoint, ci, out tmp))
                    increaseTolerance = tmp;

                decimal? allowedMin;
                decimal? allowedMax;
                KmToleranceHelper.ComputeAllowedRange(km, reductionTolerance, increaseTolerance, out allowedMin, out allowedMax);

                if (allowedMin.HasValue && km < allowedMin.Value - 0.0001m)
                    errors.Add(string.Format("Activity {0}: KM {1:N2} is below the allowed tolerance range ({2:N2}).", activityId, km, allowedMin.Value));
                if (allowedMax.HasValue && km > allowedMax.Value + 0.0001m)
                    errors.Add(string.Format("Activity {0}: KM {1:N2} is above the allowed tolerance range ({2:N2}).", activityId, km, allowedMax.Value));

                decimal rate = policyRate;
                if (found.Table.Columns.Contains("RatePerKm") && found["RatePerKm"] != DBNull.Value && decimal.TryParse(found["RatePerKm"].ToString(), NumberStyles.AllowDecimalPoint, ci, out tmp))
                    rate = tmp;

                decimal calcClaim = TravelAllowanceHelper.CalculateAmount(km, rate, fixedTravelAllowance);
                DataRow dr = tvp.NewRow(); dr["ActivityId"] = activityId; dr["TotalDistanceKM"] = km; dr["CalculatedClaimAmount"] = calcClaim;
                tvp.Rows.Add(dr);
            }
            return tvp;
        }

        // Deprecated: legacy draft-save SP wrapper — was never wired to the UI button.
        // Application now auto-saves activity/expense/KM edits on each operation.
        /*
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
        */

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

                // Draft/Rejected = global reopen (ApprovalRequestId NULL) → NEW.
                // Returned = line-item return; keep ApprovalRequestId and filter to latest Returned rows.
                if (ClaimStatuses.Equals(requestStatus, ClaimStatuses.Draft) ||
                    ClaimStatuses.Equals(requestStatus, ClaimStatuses.Rejected))
                    return false;

                var returnedActivityIds = LoadLatestReturnedActivityIds(con, requestId.Value);

                if (returnedActivityIds.Count == 0)
                {
                    message = "This month already has a pending approval request, and there are no returned days to resubmit.";
                    filteredTvp = tvp.Clone();
                    return true;
                }

                // SP validates SalesDailyActivity.ClaimStatus = '" + ClaimStatuses.Returned + @"'. Editing/soft-delete
                // can leave ClaimStatus as Draft while DetailStatus stays Returned — sync first.
                string idList = string.Join(",", returnedActivityIds.Select(id => id.ToString()).ToArray());
                string syncSql = string.Format(@"
                    UPDATE dbo.SalesDailyActivity
                    SET ClaimStatus = '" + ClaimStatuses.Returned + @"',
                        ModifiedByUserId = @User,
                        ModifiedDateTime = DATEADD(MINUTE, 330, SYSUTCDATETIME())
                    WHERE ActivityId IN ({0})
                      AND ISNULL(ClaimStatus, '') <> '" + ClaimStatuses.Returned + @"';

                    UPDATE dbo.SalesDailyExpense
                    SET ClaimStatus = '" + ClaimStatuses.Returned + @"',
                        ModifiedBy = @User,
                        ModifiedOn = SYSUTCDATETIME()
                    WHERE ActivityId IN ({0})
                      AND IsDeleted = 0
                      AND ISNULL(ClaimStatus, '') NOT IN ('" + ClaimStatuses.Returned + @"', '" + ClaimStatuses.Rejected + @"', '" + ClaimStatuses.Approved + @"');", idList);

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

        private static HashSet<int> LoadLatestReturnedActivityIds(SqlConnection con, int requestId)
        {
            var details = new DataTable();
            using (SqlCommand cmd = new SqlCommand(
                @"SELECT d.ApprovalRequestDetailId, d.ActivityId, d.DetailStatus, d.DetailVersion
                  FROM dbo.SalesApprovalRequestDetail d
                  WHERE d.ApprovalRequestId = @Id
                    AND " + SalesApprovalDetailVersionHelper.LatestDetailWhereForD.Trim(), con))
            {
                cmd.Parameters.AddWithValue("@Id", requestId);
                using (var da = new SqlDataAdapter(cmd)) da.Fill(details);
            }

            details = SalesApprovalDetailVersionHelper.KeepLatestPerActivity(details);

            var returnedActivityIds = new HashSet<int>();
            foreach (DataRow row in details.Rows)
            {
                if (row["ActivityId"] == DBNull.Value) continue;
                string status = row["DetailStatus"] == DBNull.Value ? string.Empty : row["DetailStatus"].ToString();
                if (ClaimStatuses.Equals(status, ClaimStatuses.Returned))
                    returnedActivityIds.Add(Convert.ToInt32(row["ActivityId"]));
            }
            return returnedActivityIds;
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

#if DEBUG
        private void LogSubmitTvpDiagnostics(DataTable tvp)
        {
            try
            {
                var activityIds = tvp.AsEnumerable()
                    .Where(r => r["ActivityId"] != DBNull.Value)
                    .Select(r => Convert.ToInt32(r["ActivityId"]))
                    .ToList();

                string mode = "NEW";
                string requestIdText = "NULL";
                var statusByActivity = new Dictionary<int, string>();

                if (activityIds.Count > 0)
                {
                    string idList = string.Join(",", activityIds.Select(id => id.ToString()).ToArray());
                    DataTable details = new DataTable();
                    using (SqlConnection diagCon = new SqlConnection(connString))
                    using (SqlCommand cmd = new SqlCommand(
                        @"SELECT a.ActivityId, a.ApprovalRequestId,
                                 d.ApprovalRequestDetailId, d.DetailStatus, d.DetailVersion
                          FROM dbo.SalesDailyActivity a
                          LEFT JOIN dbo.SalesApprovalRequestDetail d ON d.ActivityId = a.ActivityId
                          WHERE a.ActivityId IN (" + idList + @")", diagCon))
                    {
                        diagCon.Open();
                        using (var da = new SqlDataAdapter(cmd)) da.Fill(details);
                    }

                    int? requestId = details.AsEnumerable()
                        .Where(r => r["ApprovalRequestId"] != DBNull.Value)
                        .Select(r => (int?)Convert.ToInt32(r["ApprovalRequestId"]))
                        .FirstOrDefault();
                    if (requestId.HasValue)
                    {
                        mode = "RESUBMIT";
                        requestIdText = requestId.Value.ToString();
                    }

                    DataTable withDetailId = details.Clone();
                    foreach (DataRow row in details.Rows)
                    {
                        if (row["ApprovalRequestDetailId"] == DBNull.Value) continue;
                        withDetailId.ImportRow(row);
                    }

                    withDetailId = SalesApprovalDetailVersionHelper.KeepLatestPerActivity(withDetailId);
                    foreach (DataRow row in withDetailId.Rows)
                    {
                        int activityId = Convert.ToInt32(row["ActivityId"]);
                        statusByActivity[activityId] = row["DetailStatus"] == DBNull.Value
                            ? string.Empty
                            : row["DetailStatus"].ToString();
                    }
                }

                System.Diagnostics.Debug.WriteLine("SubmissionMode=" + mode);
                System.Diagnostics.Debug.WriteLine("ApprovalRequestId=" + requestIdText);
                System.Diagnostics.Debug.WriteLine("ActivityIds=" + string.Join(",", activityIds.Select(id => id.ToString()).ToArray()));
                foreach (int activityId in activityIds)
                {
                    string detailStatus;
                    statusByActivity.TryGetValue(activityId, out detailStatus);
                    System.Diagnostics.Debug.WriteLine("DetailStatus=" + (detailStatus ?? string.Empty));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[SubmitTvp] diagnostic failed: " + ex.Message);
            }
        }
#endif

        private SubmitResult ExecuteSubmitForApproval(DataTable tvp, int year, int month, string remarks)
        {
            if (Session["RONE_CODE"] == null) throw new Exception("Approval hierarchy not loaded in session.");

            var result = new SubmitResult();
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

#if DEBUG
                LogSubmitTvpDiagnostics(tvp);
#endif
                con.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        result.Success = rdr.FieldCount >= 1 && !rdr.IsDBNull(0) && Convert.ToInt32(rdr[0]) == 1;
                        if (rdr.FieldCount >= 2 && !rdr.IsDBNull(1)) result.ApprovalRequestId = Convert.ToInt32(rdr[1]);
                        if (rdr.FieldCount >= 3 && !rdr.IsDBNull(2)) result.SubmissionMode = rdr[2].ToString().Trim();
                        if (rdr.FieldCount >= 4 && !rdr.IsDBNull(3)) result.Message = rdr[3].ToString().Trim();
                    }
                }
            }
            return result;
        }

        private sealed class SubmitResult
        {
            public bool Success { get; set; }
            public int ApprovalRequestId { get; set; }
            public string SubmissionMode { get; set; }
            public string Message { get; set; }
        }

        // ============================================================================================
        // Deprecated: Save as Draft — removed from UI.
        // Application now uses auto-save. Draft is maintained automatically until Submit Claim.
        // The handler below only persisted SalesMonthlyDraftSummary totals and refreshed the grid;
        // it did not create approval requests or affect the submit workflow.
        // ============================================================================================
        /*
        protected void btn_save_draft_Click(object sender, EventArgs e)
        {
            try
            {
                decimal totalKM;
                decimal totalClaim;
                if (!TryGetDraftFooterTotals(out totalKM, out totalClaim)) return;

                int rowsUpserted = 0;
                int expensesUpdated = 0;
                ExecuteSaveMonthlySummary(totalKM, totalClaim, out rowsUpserted, out expensesUpdated);

                ShowDraftSaveNotification();
                Session.Remove(GetCurrentSessionKey());
                BindGrid(true, false);

                lbl_msg.Text = string.Format("Saved draft summary successfully (Total KM: {0:N2}, Total Claim: ₹ {1:N2}). [Upserted: {2}, Expenses Updated: {3}]", totalKM, totalClaim, rowsUpserted, expensesUpdated);
                lbl_msg.ForeColor = System.Drawing.Color.Green;
            }
            catch (Exception ex)
            {
                HandleDraftSaveException(ex);
            }
        }

        private bool TryGetDraftFooterTotals(out decimal totalKM, out decimal totalClaim)
        {
            totalKM = 0m;
            totalClaim = 0m;
            if (gvActivities.FooterRow != null)
            {
                totalKM = ParseDecimalFromLabel("lblFooterTotalKM");
                totalClaim = ParseCurrencyFromLabel("lblFooterCalculated");
                return true;
            }

            ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyNoFooter", "showPNotify('Warning','No data to save as draft.','warning');", true);
            return false;
        }

        private void ShowDraftSaveNotification()
        {
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
        }

        private void HandleDraftSaveException(Exception ex)
        {
            lbl_msg.Text = "Error saving draft: " + ex.Message;
            lbl_msg.ForeColor = System.Drawing.Color.Red;
            string safeErr = ex.Message.Replace("'", "\\'");
            ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyDraftErr", "showPNotify('Error','" + safeErr + "','error');", true);
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
        */

        private bool TryValidateSubmitPolicy(int year, int month)
        {
            bool isPolicyEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings["EnablePreviousMonthPolicy"]);
            if (!isPolicyEnabled) return true;

            DateTime currentDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
            int currentYear = currentDate.Year;
            int currentMonth = currentDate.Month;
            bool isAllowedWindow = currentDate.Day >= 1 && currentDate.Day <= 15;
            bool isPreviousMonthSelected = (year == currentYear && month == currentMonth - 1) || (currentMonth == 1 && month == 12 && year == currentYear - 1);
            if (isAllowedWindow && isPreviousMonthSelected) return true;

            lbl_msg.Text = "You can submit previous month data only between 1st and 15th of the current month.";
            lbl_msg.ForeColor = System.Drawing.Color.Red;
            ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyPolicyBlockSubmit", "showPNotify('Policy Restriction', 'Submission is allowed only between 1st and 15th for the previous month.', 'error');", true);
            return false;
        }

        private bool TryCollectSubmitActivityData(out DataTable tvp)
        {
            List<string> errors;
            tvp = CollectGridActivityUpdates(out errors);
            if (!errors.Any()) return true;

            string msg = "Unable to submit due to validation errors:<br/>" + string.Join("<br/>", errors.ToArray());
            lbl_msg.Text = "Validation failed.";
            lbl_msg.ForeColor = System.Drawing.Color.Red;
            string safeErr = msg.Replace("'", "\\'");
            ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySubmitErr", "showPNotify('Validation Error','" + safeErr + "', 'error');", true);
            return false;
        }

        private bool TryResolveSubmitTvp(int year, int month, ref DataTable tvp)
        {
            if (tvp.Rows.Count == 0)
            {
                lbl_msg.Text = "No activity rows found to submit.";
                lbl_msg.ForeColor = System.Drawing.Color.Red;
                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySubmitNoRows", "showPNotify('Nothing to submit', 'No activity rows found.', 'warning');", true);
                return false;
            }

            DataTable submitTvp;
            string partialMessage;
            if (!TryPreparePartialResubmission(year, month, tvp, out submitTvp, out partialMessage)) return true;

            if (!string.IsNullOrWhiteSpace(partialMessage) || submitTvp == null || submitTvp.Rows.Count == 0)
            {
                string msg = partialMessage ?? "No returned activities found to resubmit.";
                lbl_msg.Text = msg;
                lbl_msg.ForeColor = System.Drawing.Color.Red;
                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySubmitNoReturned",
                    "showPNotify('Nothing to resubmit', '" + msg.Replace("'", "\\'") + "', 'warning');", true);
                return false;
            }

            tvp = submitTvp;
            return true;
        }

        private void CompleteSubmitSuccess(SubmitResult result, DataTable tvp, int year, int month)
        {
            Session.Remove(GetCurrentSessionKey());

            if (result == null || !result.Success || result.ApprovalRequestId <= 0)
            {
                lbl_msg.Text = "Submitted for approval (no request id returned).";
                lbl_msg.ForeColor = System.Drawing.Color.Green;
                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySubmit2", "showPNotify('Submitted','Submitted for approval successfully.','success'); hidePageLoader();", true);
                BindGrid(true);
                return;
            }

            string message = BuildSubmitSuccessMessage(result, tvp != null ? tvp.Rows.Count : 0);
            lbl_msg.Text = message;
            lbl_msg.ForeColor = System.Drawing.Color.Green;

            string url = string.Format(
                "MyClaimsView_v2.aspx?year={0}&month={1}&requestId={2}&open=view&mode={3}&count={4}&msg={5}",
                year,
                month,
                result.ApprovalRequestId,
                HttpUtility.UrlEncode(result.SubmissionMode ?? string.Empty),
                tvp != null ? tvp.Rows.Count : 0,
                HttpUtility.UrlEncode(message));
            Response.Redirect(url, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private static string BuildSubmitSuccessMessage(SubmitResult result, int activityCount)
        {
            if (result != null && !string.IsNullOrWhiteSpace(result.Message)
                && !string.Equals(result.Message.Trim(), "Claim submitted successfully.", StringComparison.OrdinalIgnoreCase))
                return result.Message.Trim();

            string mode = result != null ? (result.SubmissionMode ?? string.Empty) : string.Empty;
            int requestId = result != null ? result.ApprovalRequestId : 0;

            if (string.Equals(mode, "RESUBMIT", StringComparison.OrdinalIgnoreCase))
            {
                if (activityCount == 1)
                    return string.Format("1 returned activity has been resubmitted under Request #{0}.", requestId);
                return string.Format("{0} returned activities have been resubmitted under Request #{1}.", activityCount, requestId);
            }

            return string.Format("Monthly claim submitted successfully. Request #{0} has been created and sent for approval.", requestId);
        }

        private void HandleSubmitSqlException(SqlException sqx)
        {
            lbl_msg.Text = "SQL Error while submitting: " + sqx.Message;
            lbl_msg.ForeColor = System.Drawing.Color.Red;
            string safeErr = sqx.Message.Replace("'", "\\'");
            ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySubmitSqlErr", "hidePageLoader(); showPNotify('SQL Error','" + safeErr + "','error');", true);
        }

        private void HandleSubmitException(Exception ex)
        {
            lbl_msg.Text = "Error during submit: " + ex.Message;
            lbl_msg.ForeColor = System.Drawing.Color.Red;
            string safeErr = ex.Message.Replace("'", "\\'");
            ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySubmitErr2", "hidePageLoader(); showPNotify('Error','" + safeErr + "','error');", true);
        }

        protected void btn_submit_Click(object sender, EventArgs e)
        {
            try
            {
                int year = int.Parse(DDL_Year.SelectedValue);
                int month = int.Parse(DDL_Month.SelectedValue);

                if (!TryValidateSubmitPolicy(year, month)) return;

                DataTable tvp;
                if (!TryCollectSubmitActivityData(out tvp)) return;
                if (!TryResolveSubmitTvp(year, month, ref tvp)) return;

                SubmitResult result = ExecuteSubmitForApproval(tvp, year, month, "Submitted via UI");
                CompleteSubmitSuccess(result, tvp, year, month);
            }
            catch (SqlException sqx)
            {
                HandleSubmitSqlException(sqx);
            }
            catch (Exception ex)
            {
                HandleSubmitException(ex);
            }
        }

        private sealed class ActivityProgressCounts
        {
            public int WorkingDays { get; set; }
            public int Completed { get; set; }
            public int Draft { get; set; }
            public int Returned { get; set; }
            public int Submitted { get; set; }
        }

        private string GetLastSavedSessionKey()
        {
            return string.Format("SO_LastDraftSave_{0}_{1}_{2}", GetCurrentUserLoginId(), DDL_Year.SelectedValue, DDL_Month.SelectedValue);
        }

        private DateTime? GetLastDraftSaveTime()
        {
            object stored = Session[GetLastSavedSessionKey()];
            return stored is DateTime ? (DateTime?)stored : null;
        }

        private void NotifyDraftChangeSaved()
        {
            DateTime savedAt = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
            Session[GetLastSavedSessionKey()] = savedAt;

            string timeLabel = savedAt.ToString("h:mm tt");
            string script = string.Format(
                "if (typeof showAutoSaveIndicator === 'function') showAutoSaveIndicator('{0}', true, false);",
                timeLabel.Replace("'", "\\'"));
            ScriptManager.RegisterStartupScript(this, GetType(), "autoSave_" + Guid.NewGuid().ToString("N"), script, true);
        }

        private static ActivityProgressCounts CountActivityProgress(DataTable dt)
        {
            var counts = new ActivityProgressCounts();
            if (dt == null) return counts;

            counts.WorkingDays = dt.Rows.Count;
            foreach (DataRow row in dt.Rows)
            {
                string claimStatus = row.Table.Columns.Contains("ClaimStatus") && row["ClaimStatus"] != DBNull.Value
                    ? row["ClaimStatus"].ToString().Trim()
                    : string.Empty;
                string detailStatus = row.Table.Columns.Contains("DetailStatus") && row["DetailStatus"] != DBNull.Value
                    ? row["DetailStatus"].ToString().Trim()
                    : string.Empty;
                bool isEditable = row.Table.Columns.Contains("IsEditable")
                    && row["IsEditable"] != DBNull.Value
                    && Convert.ToBoolean(row["IsEditable"]);

                string effectiveStatus = ClaimEditabilityHelper.ResolveEffectiveRowStatus(claimStatus, detailStatus, isEditable);

                if (ClaimStatuses.Equals(effectiveStatus, ClaimStatuses.Draft))
                    counts.Draft++;
                else if (ClaimStatuses.Equals(effectiveStatus, ClaimStatuses.Returned))
                    counts.Returned++;
                else if (ClaimStatuses.Equals(effectiveStatus, ClaimStatuses.Submitted)
                    || ClaimStatuses.Equals(claimStatus, ClaimStatuses.Pending))
                    counts.Submitted++;
                else if (ClaimStatuses.Equals(effectiveStatus, ClaimStatuses.Approved)
                    || ClaimStatuses.Equals(effectiveStatus, ClaimStatuses.Processed))
                    counts.Completed++;
                else
                    counts.Submitted++;
            }

            return counts;
        }

        private void BindAutoSaveIndicator(DataTable dt)
        {
            if (pnlAutoSaveIndicator == null) return;

            ActivityProgressCounts progress = CountActivityProgress(dt);
            DateTime? lastSaved = GetLastDraftSaveTime();
            bool showIndicator = progress.Draft > 0;

            pnlAutoSaveIndicator.Visible = showIndicator;
            if (!showIndicator) return;

            if (litLastSavedTime != null)
                litLastSavedTime.Text = lastSaved.HasValue ? lastSaved.Value.ToString("h:mm tt") : string.Empty;
        }

        private void BindEnhancedMonthlySummary(decimal grandTravel, decimal grandDA, decimal grandHotel, decimal grandOtherExpenses, decimal grandTotal)
        {
            if (pnlEnhancedMonthlySummary == null) return;

            pnlEnhancedMonthlySummary.Visible = true;
            if (lblSummaryTravel != null) lblSummaryTravel.Text = "₹ " + grandTravel.ToString("N2");
            if (lblSummaryDA != null) lblSummaryDA.Text = "₹ " + grandDA.ToString("N2");
            if (lblSummaryHotel != null) lblSummaryHotel.Text = "₹ " + grandHotel.ToString("N2");
            if (lblSummaryOther != null) lblSummaryOther.Text = "₹ " + grandOtherExpenses.ToString("N2");
            if (lblSummaryGrandTotal != null) lblSummaryGrandTotal.Text = "₹ " + grandTotal.ToString("N2");
        }

        private void BindProgressSummary(DataTable dt)
        {
            if (pnlProgressSummary == null) return;

            ActivityProgressCounts progress = CountActivityProgress(dt);
            bool hasActivities = dt != null && dt.Rows.Count > 0;
            pnlProgressSummary.Visible = hasActivities;

            if (!hasActivities) return;

            if (lblProgressWorkingDays != null) lblProgressWorkingDays.Text = progress.WorkingDays.ToString();
            if (lblProgressCompleted != null) lblProgressCompleted.Text = progress.Completed.ToString();
            if (lblProgressDraft != null) lblProgressDraft.Text = progress.Draft.ToString();
            if (lblProgressReturned != null) lblProgressReturned.Text = progress.Returned.ToString();
            if (lblProgressSubmitted != null) lblProgressSubmitted.Text = progress.Submitted.ToString();
        }

        private void BindSubmissionConfidencePanel(DataTable dt, bool monthLocked, bool allowSubmission, bool allowExecution)
        {
            if (pnlSubmissionConfidence == null || phSubmissionConfidence == null) return;

            bool hasActivities = dt != null && dt.Rows.Count > 0;
            ActivityProgressCounts progress = CountActivityProgress(dt);

            bool allActivitiesCompleted = hasActivities && progress.Draft == 0;
            bool expensesReviewed = hasActivities && progress.Returned == 0;
            bool policyLimitsSatisfied = allowExecution;
            bool noReturnedActivities = progress.Returned == 0;

            bool showPanel = hasActivities && (Actionable == null || Actionable.Visible);
            pnlSubmissionConfidence.Visible = showPanel;

            if (!showPanel)
            {
                phSubmissionConfidence.Controls.Clear();
                return;
            }

            var items = new List<Tuple<string, bool>>
            {
                Tuple.Create("All activities completed", allActivitiesCompleted),
                Tuple.Create("Expenses reviewed", expensesReviewed),
                Tuple.Create("Policy limits satisfied", policyLimitsSatisfied),
                Tuple.Create("No Returned activities", noReturnedActivities)
            };

            phSubmissionConfidence.Controls.Clear();
            foreach (var item in items)
            {
                string iconClass = item.Item2 ? "fa-check-circle text-success" : "fa-times-circle text-muted";
                phSubmissionConfidence.Controls.Add(new Literal
                {
                    Text = string.Format(
                        "<div class=\"check-item\"><i class=\"fa {0}\"></i><span>{1}</span></div>",
                        iconClass,
                        HttpUtility.HtmlEncode(item.Item2 ? item.Item1 : item.Item1 + " — pending"))
                });
            }

            if (!allowSubmission && monthLocked && progress.Returned > 0)
            {
                phSubmissionConfidence.Controls.Add(new Literal
                {
                    Text = "<div class=\"small text-warning mt-2 mb-0\"><i class=\"fa fa-info-circle\"></i> Correct returned days before resubmitting.</div>"
                });
            }
        }

        private void BindMonthlyConfidenceUx(DataTable dt, bool monthLocked, bool allowSubmission, bool allowExecution,
            decimal grandTravel, decimal grandDA, decimal grandHotel, decimal grandTravelExpenses, decimal grandStationary)
        {
            decimal grandOther = grandTravelExpenses + grandStationary;
            decimal grandTotal = grandTravel + grandDA + grandHotel + grandOther;

            BindEnhancedMonthlySummary(grandTravel, grandDA, grandHotel, grandOther, grandTotal);
            BindProgressSummary(dt);
            BindSubmissionConfidencePanel(dt, monthLocked, allowSubmission, allowExecution);
            BindAutoSaveIndicator(dt);

            DateTime? lastSaved = GetLastDraftSaveTime();
            if (lastSaved.HasValue && pnlAutoSaveIndicator != null && pnlAutoSaveIndicator.Visible)
            {
                string timeLabel = lastSaved.Value.ToString("h:mm tt");
                ScriptManager.RegisterStartupScript(
                    this,
                    GetType(),
                    "restoreAutoSave_" + Guid.NewGuid().ToString("N"),
                    string.Format("if (typeof showAutoSaveIndicator === 'function') showAutoSaveIndicator('{0}', true, false);", timeLabel.Replace("'", "\\'")),
                    true);
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
                    if (ClaimStatuses.Equals(status, ClaimStatuses.Submitted)) return ClaimStatuses.Equals(claim, ClaimStatuses.Submitted) || ClaimStatuses.Equals(claim, ClaimStatuses.Pending);
                    if (string.Equals(status, "Other", StringComparison.OrdinalIgnoreCase))
                    {
                        bool isDraft = ClaimStatuses.Equals(claim, ClaimStatuses.Draft);
                        bool isSubmitted = ClaimStatuses.Equals(claim, ClaimStatuses.Submitted) || ClaimStatuses.Equals(claim, ClaimStatuses.Pending);
                        bool isApproved = ClaimStatuses.Equals(claim, ClaimStatuses.Approved);
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
                return isEditable && !ClaimStatuses.Equals(claim, ClaimStatuses.Draft);
            };

            decimal d_ta = SumMoney("CalculatedClaimAmount", byClaimStatus(ClaimStatuses.Draft)); decimal d_da = SumMoney("DAAmount", byClaimStatus(ClaimStatuses.Draft)); decimal d_hotel = SumMoney("HotelAmount", byClaimStatus(ClaimStatuses.Draft)); decimal d_travel = SumMoney("TravelAmount", byClaimStatus(ClaimStatuses.Draft)); decimal d_exp = SumMoney("ExpenseTotal", byClaimStatus(ClaimStatuses.Draft)); decimal d_km = SumKM(byClaimStatus(ClaimStatuses.Draft));
            if (lblDraft_TA != null) lblDraft_TA.Text = "₹ " + d_ta.ToString("N2");
            if (lblDraft_DA != null) lblDraft_DA.Text = "₹ " + d_da.ToString("N2");
            if (lblDraft_Hotel != null) lblDraft_Hotel.Text = "₹ " + d_hotel.ToString("N2");
            if (lblDraft_Travel != null) lblDraft_Travel.Text = "₹ " + d_travel.ToString("N2");
            if (lblDraft_Expense != null) lblDraft_Expense.Text = "₹ " + d_exp.ToString("N2");
            if (lblDraft_KM != null) lblDraft_KM.Text = d_km.ToString("N2");

            decimal s_ta = SumMoney("CalculatedClaimAmount", byClaimStatus(ClaimStatuses.Submitted)); decimal s_da = SumMoney("DAAmount", byClaimStatus(ClaimStatuses.Submitted)); decimal s_hotel = SumMoney("HotelAmount", byClaimStatus(ClaimStatuses.Submitted)); decimal s_travel = SumMoney("TravelAmount", byClaimStatus(ClaimStatuses.Submitted)); decimal s_exp = SumMoney("ExpenseTotal", byClaimStatus(ClaimStatuses.Submitted)); decimal s_km = SumKM(byClaimStatus(ClaimStatuses.Submitted));
            if (lblSubmitted_TA != null) lblSubmitted_TA.Text = "₹ " + s_ta.ToString("N2");
            if (lblSubmitted_DA != null) lblSubmitted_DA.Text = "₹ " + s_da.ToString("N2");
            if (lblSubmitted_Hotel != null) lblSubmitted_Hotel.Text = "₹ " + s_hotel.ToString("N2");
            if (lblSubmitted_Travel != null) lblSubmitted_Travel.Text = "₹ " + s_travel.ToString("N2");
            if (lblSubmitted_Expense != null) lblSubmitted_Expense.Text = "₹ " + s_exp.ToString("N2");
            if (lblSubmitted_KM != null) lblSubmitted_KM.Text = s_km.ToString("N2");

            decimal a_ta = SumMoney("CalculatedClaimAmount", byClaimStatus(ClaimStatuses.Approved)); decimal a_da = SumMoney("DAAmount", byClaimStatus(ClaimStatuses.Approved)); decimal a_hotel = SumMoney("HotelAmount", byClaimStatus(ClaimStatuses.Approved)); decimal a_travel = SumMoney("TravelAmount", byClaimStatus(ClaimStatuses.Approved)); decimal a_exp = SumMoney("ExpenseTotal", byClaimStatus(ClaimStatuses.Approved)); decimal a_km = SumKM(byClaimStatus(ClaimStatuses.Approved));
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
                    if (ClaimStatuses.Equals(status, ClaimStatuses.Submitted)) return ClaimStatuses.Equals(claim, ClaimStatuses.Submitted) || ClaimStatuses.Equals(claim, ClaimStatuses.Pending);
                    if (string.Equals(status, "Other", StringComparison.OrdinalIgnoreCase))
                    {
                        bool isDraft = ClaimStatuses.Equals(claim, ClaimStatuses.Draft);
                        bool isSubmitted = ClaimStatuses.Equals(claim, ClaimStatuses.Submitted) || ClaimStatuses.Equals(claim, ClaimStatuses.Pending);
                        bool isApproved = ClaimStatuses.Equals(claim, ClaimStatuses.Approved);
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
                return isEditable && !ClaimStatuses.Equals(claim, ClaimStatuses.Draft);
            };

            // --- DRAFT ---
            decimal d_ta = SumMoney("CalculatedClaimAmount", byClaimStatus(ClaimStatuses.Draft)); decimal d_da = SumMoney("DAAmount", byClaimStatus(ClaimStatuses.Draft)); decimal d_hotel = SumMoney("HotelAmount", byClaimStatus(ClaimStatuses.Draft)); decimal d_travel = SumMoney("TravelAmount", byClaimStatus(ClaimStatuses.Draft)); decimal d_stat = SumMoney("StationaryAmount", byClaimStatus(ClaimStatuses.Draft)); decimal d_exp = SumMoney("ExpenseTotal", byClaimStatus(ClaimStatuses.Draft)); decimal d_km = SumKM(byClaimStatus(ClaimStatuses.Draft));
            if (lblDraft_TA != null) lblDraft_TA.Text = "₹ " + d_ta.ToString("N2");
            if (lblDraft_DA != null) lblDraft_DA.Text = "₹ " + d_da.ToString("N2");
            if (lblDraft_Hotel != null) lblDraft_Hotel.Text = "₹ " + d_hotel.ToString("N2");
            if (lblDraft_Travel != null) lblDraft_Travel.Text = "₹ " + d_travel.ToString("N2");
            if (lblDraft_Stationary != null) lblDraft_Stationary.Text = "₹ " + d_stat.ToString("N2"); // Added
            if (lblDraft_Expense != null) lblDraft_Expense.Text = "₹ " + d_exp.ToString("N2");
            if (lblDraft_KM != null) lblDraft_KM.Text = d_km.ToString("N2");

            // --- SUBMITTED ---
            decimal s_ta = SumMoney("CalculatedClaimAmount", byClaimStatus(ClaimStatuses.Submitted)); decimal s_da = SumMoney("DAAmount", byClaimStatus(ClaimStatuses.Submitted)); decimal s_hotel = SumMoney("HotelAmount", byClaimStatus(ClaimStatuses.Submitted)); decimal s_travel = SumMoney("TravelAmount", byClaimStatus(ClaimStatuses.Submitted)); decimal s_stat = SumMoney("StationaryAmount", byClaimStatus(ClaimStatuses.Submitted)); decimal s_exp = SumMoney("ExpenseTotal", byClaimStatus(ClaimStatuses.Submitted)); decimal s_km = SumKM(byClaimStatus(ClaimStatuses.Submitted));
            if (lblSubmitted_TA != null) lblSubmitted_TA.Text = "₹ " + s_ta.ToString("N2");
            if (lblSubmitted_DA != null) lblSubmitted_DA.Text = "₹ " + s_da.ToString("N2");
            if (lblSubmitted_Hotel != null) lblSubmitted_Hotel.Text = "₹ " + s_hotel.ToString("N2");
            if (lblSubmitted_Travel != null) lblSubmitted_Travel.Text = "₹ " + s_travel.ToString("N2");
            if (lblSubmitted_Stationary != null) lblSubmitted_Stationary.Text = "₹ " + s_stat.ToString("N2"); // Added
            if (lblSubmitted_Expense != null) lblSubmitted_Expense.Text = "₹ " + s_exp.ToString("N2");
            if (lblSubmitted_KM != null) lblSubmitted_KM.Text = s_km.ToString("N2");

            // --- APPROVED ---
            decimal a_ta = SumMoney("CalculatedClaimAmount", byClaimStatus(ClaimStatuses.Approved)); decimal a_da = SumMoney("DAAmount", byClaimStatus(ClaimStatuses.Approved)); decimal a_hotel = SumMoney("HotelAmount", byClaimStatus(ClaimStatuses.Approved)); decimal a_travel = SumMoney("TravelAmount", byClaimStatus(ClaimStatuses.Approved)); decimal a_stat = SumMoney("StationaryAmount", byClaimStatus(ClaimStatuses.Approved)); decimal a_exp = SumMoney("ExpenseTotal", byClaimStatus(ClaimStatuses.Approved)); decimal a_km = SumKM(byClaimStatus(ClaimStatuses.Approved));
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