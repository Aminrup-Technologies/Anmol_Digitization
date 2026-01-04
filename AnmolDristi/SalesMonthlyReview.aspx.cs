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

        // class-level accumulators
        private decimal _totalCalculated = 0m;
        private decimal _totalDA = 0m;
        private decimal _totalHotel = 0m;
        private decimal _totalExpense = 0m;
        private decimal _totalKM = 0m;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // require USERID and USERNAME as minimum for a logged-in user
                if (Session["USERID"] != null && Session["USERNAME"] != null)
                {
                    PopulateYearMonth();
                    lbl_msg.Text = "";
                }
                else
                {
                    // no session - redirect to login or handle as per your app flow
                    Response.Redirect("~/Login.aspx");
                }
            }
        }

        private void PopulateYearMonth()
        {
            int currentYear = DateTime.UtcNow.AddMinutes(330).Year; // IST
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
            Session.Remove(SESSION_KEY_GV_ACTIVITIES);
            BindGrid(true);
        }
        private void BindGrid_OLD()
        {
            lbl_msg.Text = "";
            int year = int.Parse(DDL_Year.SelectedValue);
            int month = int.Parse(DDL_Month.SelectedValue);

            bool isPolicyEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings["EnablePreviousMonthPolicy"]);
            bool allowExecution = false;  // final flag to control main code execution

            if (isPolicyEnabled == true)
            {
                // Current date in IST
                DateTime currentDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                                    TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                int currentYear = currentDate.Year;
                int currentMonth = currentDate.Month;

                // Allow only 1st–15th of current month for previous month data
                bool isAllowedWindow = currentDate.Day >= 1 && currentDate.Day <= 15;
                bool isPreviousMonthSelected =
                    (year == currentYear && month == currentMonth - 1) ||
                    (currentMonth == 1 && month == 12 && year == currentYear - 1); // handles Jan → Dec case

                if (isAllowedWindow && isPreviousMonthSelected)
                {
                    allowExecution = true;
                }
                else
                {
                    // ❌ Block execution and show message
                    lbl_msg.Text = "You can process previous month data only between 1st and 15th of the current month.";
                    lbl_msg.ForeColor = System.Drawing.Color.Red;

                    // Show PNotify alert
                    ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyPolicyBlock",
                        "showPNotify('Policy Restriction', 'Processing is allowed only between 1st and 15th for the previous month.', 'error');", true);
                    //return;
                }
            }
            else
            {
                // 🚀 Policy disabled → always allow
                allowExecution = true;
                //// Trigger PNotify (client-side)
                //ScriptManager.RegisterStartupScript(this, GetType(), "policyBypassNotice", "showPolicyBypassNotice();", true);

                // Show bypass PNotify
                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyBypass", "showPNotify('Policy Disabled', 'Monthly processing policy is disabled. Validation skipped.', 'info');", true);
            }

            if (allowExecution)
            {
                try
                {
                    // ✅ Proceed with your normal code here
                    DataTable dt = new DataTable();
                    using (SqlConnection con = new SqlConnection(connString))
                    using (SqlCommand cmd = new SqlCommand("dbo.usp_GetSalesDailyActivity_Month", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@LoginId", GetCurrentUserLoginId());
                        cmd.Parameters.AddWithValue("@Year", year);
                        cmd.Parameters.AddWithValue("@Month", month);
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
                    }

                    // Ensure calculated column exists
                    if (!dt.Columns.Contains("CalculatedClaimAmount"))
                        dt.Columns.Add("CalculatedClaimAmount", typeof(decimal));

                    // pick default rate from web.config or fallback
                    decimal defaultRate = 2.75m;
                    var cfg = ConfigurationManager.AppSettings["DefaultRatePerKm"];
                    if (!string.IsNullOrWhiteSpace(cfg))
                    {
                        decimal tmp;
                        if (decimal.TryParse(cfg, out tmp)) defaultRate = tmp;
                    }

                    // For each row compute CalculatedClaimAmount = RatePerKm * TotalDistanceKM
                    foreach (DataRow row in dt.Rows)
                    {
                        // TotalDistanceKM safe parse
                        decimal totalKm = 0m;
                        if (row.Table.Columns.Contains("TotalDistanceKM") && row["TotalDistanceKM"] != DBNull.Value)
                        {
                            decimal.TryParse(row["TotalDistanceKM"].ToString(), out totalKm);
                        }

                        // get per-row rate (from common column names) or fallback to defaultRate
                        decimal rate = GetRatePerKm(row, defaultRate);

                        // compute
                        decimal claim = Math.Round(rate * totalKm, 2, MidpointRounding.AwayFromZero);

                        row["CalculatedClaimAmount"] = claim;

                        // Optionally put the rate into a visible column for the template small text
                        if (!row.Table.Columns.Contains("RatePerKm"))
                            row.Table.Columns.Add("RatePerKm", typeof(decimal));

                        // set rate column only if not already set (so we don't overwrite if SP returned it)
                        if (row["RatePerKm"] == DBNull.Value || string.IsNullOrWhiteSpace(row["RatePerKm"].ToString()))
                            row["RatePerKm"] = rate;
                    }

                    gvActivities.DataSource = dt;
                    gvActivities.DataBind();

                    // Compute sum across VISIBLE rows (current GridView rows) and set footer label
                    decimal visibleTotal = 0m;
                    foreach (GridViewRow r in gvActivities.Rows)
                    {
                        Label lbl = r.FindControl("lblCalculatedClaim") as Label;
                        if (lbl != null)
                        {
                            decimal v;
                            if (decimal.TryParse(lbl.Text, out v))
                                visibleTotal += v;
                            else
                            {
                                // try removing commas/currency if format varies
                                var cleaned = lbl.Text.Replace(",", "").Trim();
                                if (decimal.TryParse(cleaned, out v)) visibleTotal += v;
                            }
                        }
                    }

                    // after gvActivities.DataBind();
                    decimal pageDA = 0m, pageHotel = 0m, pageExpense = 0m;
                    foreach (GridViewRow r in gvActivities.Rows)
                    {
                        // read per-row labels (IDs from TemplateField)
                        Label lDA = r.FindControl("lblDA") as Label;
                        Label lHotel = r.FindControl("lblHotel") as Label;
                        Label lExp = r.FindControl("lblExpenseTotal") as Label;

                        decimal tmp;
                        if (lDA != null && decimal.TryParse(lDA.Text, out tmp)) pageDA += tmp;
                        if (lHotel != null && decimal.TryParse(lHotel.Text, out tmp)) pageHotel += tmp;
                        if (lExp != null && decimal.TryParse(lExp.Text, out tmp)) pageExpense += tmp;
                    }

                    // set footer labels
                    if (gvActivities.FooterRow != null)
                    {
                        var fDA = gvActivities.FooterRow.FindControl("lblFooterDA") as Label;
                        var fHotel = gvActivities.FooterRow.FindControl("lblFooterHotel") as Label;
                        var fExpense = gvActivities.FooterRow.FindControl("lblFooterExpenseTotal") as Label;

                        if (fDA != null) fDA.Text = pageDA.ToString("0.##");
                        if (fHotel != null) fHotel.Text = pageHotel.ToString("0.##");
                        if (fExpense != null) fExpense.Text = pageExpense.ToString("0.##");
                    }


                    // Set footer label if footer exists
                    if (gvActivities.FooterRow != null)
                    {
                        Label lblFooter = gvActivities.FooterRow.FindControl("lblFooterCalculated") as Label;
                        if (lblFooter != null)
                        {
                            lblFooter.Text = visibleTotal.ToString("0.00");
                        }
                        else
                        {
                            // alternative: set cell text directly (fallback)
                            int calcColIndex = -1;
                            for (int i = 0; i < gvActivities.Columns.Count; i++)
                            {
                                if (gvActivities.Columns[i] is TemplateField && ((TemplateField)gvActivities.Columns[i]).HeaderText == "Calculated Claim")
                                {
                                    calcColIndex = i;
                                    break;
                                }
                            }
                            if (calcColIndex >= 0 && calcColIndex < gvActivities.FooterRow.Cells.Count)
                                gvActivities.FooterRow.Cells[calcColIndex].Text = "Total: " + visibleTotal.ToString("0.00");
                        }
                    }

                    Actionable.Visible = true;
                    lbl_msg.Text = "Action executed successfully.";
                    lbl_msg.ForeColor = System.Drawing.Color.Green;

                    ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySuccess", "showPNotify('Success', 'Process executed successfully.', 'success');", true);
                }
                catch (Exception ex)
                {

                    lbl_msg.Text = "An error occurred: " + ex.Message;
                    lbl_msg.ForeColor = System.Drawing.Color.Red;

                    ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyError", $"showPNotify('Error', '{ex.Message.Replace("'", "\\'")}', 'error');", true);
                }
            }

        }
        private void BindGrid_OLD1(Boolean policybinder, bool showNotification = true)
        {
            lbl_msg.Text = "";
            int year = int.Parse(DDL_Year.SelectedValue);
            int month = int.Parse(DDL_Month.SelectedValue);

            bool isPolicyEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings["EnablePreviousMonthPolicy"]);
            bool allowExecution = false;

            if (isPolicyEnabled)
            {
                DateTime currentDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                                          TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                int currentYear = currentDate.Year;
                int currentMonth = currentDate.Month;

                bool isAllowedWindow = currentDate.Day >= 1 && currentDate.Day <= 15;
                bool isPreviousMonthSelected =
                    (year == currentYear && month == currentMonth - 1) ||
                    (currentMonth == 1 && month == 12 && year == currentYear - 1);

                if (isAllowedWindow && isPreviousMonthSelected)
                    allowExecution = true;
                else
                {
                    lbl_msg.Text = "You can process previous month data only between 1st and 15th of the current month.";
                    lbl_msg.ForeColor = System.Drawing.Color.Red;
                    ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyPolicyBlock",
                        "showPNotify('Policy Restriction', 'Processing is allowed only between 1st and 15th for the previous month.', 'error');", true);
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
                    string employeeCode = GetLoggedInEmployeeCode(); // implement as per your auth
                    int? regionId = GetLoggedInRegionId();
                    int? branchId = GetLoggedInBranchId();
                    int? gradeId = GetLoggedInGradeId();
                    int? designationId = GetLoggedInDesignationId();
                    var policy = GetResolvedPolicy(employeeCode, regionId, branchId, gradeId, designationId);
                    if (policy != null)
                    {
                        // Use the policy values in UI or server-side validation
                        BindPolicyToModal(policy);
                        ShowPolicyNotification(policy);
                    }
                    else
                    {
                        // optional: show message when no policy found
                        string msg = "No resolved policy found for the selected context.";
                        RegisterClientNotification(msg, "warning");
                    }
                }


                //----------Once Policy is Loaded, Bind the Sales Activity Records

                //TXT_DAAmount.Text = lblResolvedDA.Text.ToString();

                DataTable dt = new DataTable();
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

                // Ensure required columns exist before processing
                if (!dt.Columns.Contains("CalculatedClaimAmount"))
                    dt.Columns.Add("CalculatedClaimAmount", typeof(decimal));

                if (!dt.Columns.Contains("RatePerKm"))
                    dt.Columns.Add("RatePerKm", typeof(decimal));

                // pick default rate from web.config or fallback
                decimal defaultRate = 2.75m;
                var cfg = ConfigurationManager.AppSettings["DefaultRatePerKm"];
                decimal tmpCfg;
                if (!string.IsNullOrWhiteSpace(cfg) && decimal.TryParse(cfg, out tmpCfg))
                    defaultRate = tmpCfg;

                // Compute per-row CalculatedClaimAmount and ensure RatePerKm set
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

                // --- Compute GRAND totals (entire DataTable) using DataTable.Compute
                // This is the recommended approach for Finance views when paging is enabled.
                decimal grandCalc = 0m, grandDA = 0m, grandHotel = 0m, grandTravel = 0m, grandExpense = 0m, grandKM = 0m;
                CultureInfo ci = CultureInfo.GetCultureInfo("en-IN");

                if (dt.Rows.Count > 0)
                {
                    // Use Compute for numeric columns (handles DBNull)
                    object o;
                    o = dt.Compute("SUM(CalculatedClaimAmount)", "");
                    grandCalc = o != DBNull.Value ? Convert.ToDecimal(o) : 0m;

                    if (dt.Columns.Contains("DAAmount")) { o = dt.Compute("SUM(DAAmount)", ""); grandDA = o != DBNull.Value ? Convert.ToDecimal(o) : 0m; }
                    if (dt.Columns.Contains("HotelAmount")) { o = dt.Compute("SUM(HotelAmount)", ""); grandHotel = o != DBNull.Value ? Convert.ToDecimal(o) : 0m; }
                    if (dt.Columns.Contains("TravelAmount")) { o = dt.Compute("SUM(TravelAmount)", ""); grandTravel = o != DBNull.Value ? Convert.ToDecimal(o) : 0m; }
                    if (dt.Columns.Contains("ExpenseTotal")) { o = dt.Compute("SUM(ExpenseTotal)", ""); grandExpense = o != DBNull.Value ? Convert.ToDecimal(o) : 0m; }
                    if (dt.Columns.Contains("TotalDistanceKM")) { o = dt.Compute("SUM(TotalDistanceKM)", ""); grandKM = o != DBNull.Value ? Convert.ToDecimal(o) : 0m; }
                }

                // Bind and then compute page (visible) totals for footer
                gvActivities.DataSource = dt;
                gvActivities.DataBind();

                gvActivities.UseAccessibleHeader = true;
                if (gvActivities.HeaderRow != null)
                    gvActivities.HeaderRow.TableSection = TableRowSection.TableHeader;

                // PAGE (visible) totals: parse label text from rows (safe sanitized parsing)
                decimal pageCalc = 0m, pageDA = 0m, pageHotel = 0m, pageExpense = 0m, pageKM = 0m;
                foreach (GridViewRow r in gvActivities.Rows)
                {
                    Label lCalc = r.FindControl("lblCalculatedClaim") as Label;
                    Label lDA = r.FindControl("lblDA") as Label;
                    Label lHotel = r.FindControl("lblHotel") as Label;
                    Label lExp = r.FindControl("lblExpenseTotal") as Label;
                    Label lKM = r.FindControl("lblTolRawMin") as Label; // if you display KM elsewhere, adjust accordingly

                    //if (lCalc != null) { if (SanitizeDecimal(lCalc.Text, ci, out decimal v)) pageCalc += v; }
                    //if (lDA != null) { if (SanitizeDecimal(lDA.Text, ci, out decimal v)) pageDA += v; }
                    //if (lHotel != null) { if (SanitizeDecimal(lHotel.Text, ci, out decimal v)) pageHotel += v; }
                    //if (lExp != null) { if (SanitizeDecimal(lExp.Text, ci, out decimal v)) pageExpense += v; }

                    decimal v = 0m;

                    if (lCalc != null)
                    {
                        if (SanitizeDecimal(lCalc.Text, ci, out v))
                            pageCalc += v;
                    }

                    if (lDA != null)
                    {
                        if (SanitizeDecimal(lDA.Text, ci, out v))
                            pageDA += v;
                    }

                    if (lHotel != null)
                    {
                        if (SanitizeDecimal(lHotel.Text, ci, out v))
                            pageHotel += v;
                    }

                    if (lExp != null)
                    {
                        if (SanitizeDecimal(lExp.Text, ci, out v))
                            pageExpense += v;
                    }

                }

                // Set footer labels (show page totals but also optionally display grand totals via tooltip or small text)
                //if (gvActivities.FooterRow != null)
                //{
                //    var fCalc = gvActivities.FooterRow.FindControl("lblFooterCalculated") as Label;
                //    var fDA = gvActivities.FooterRow.FindControl("lblFooterDA") as Label;
                //    var fHotel = gvActivities.FooterRow.FindControl("lblFooterHotel") as Label;
                //    var fExpense = gvActivities.FooterRow.FindControl("lblFooterExpenseTotal") as Label;
                //    var fKM = gvActivities.FooterRow.FindControl("lblFooterTotalKM") as Label;

                //    if (fCalc != null) fCalc.Text = string.Format(ci, "₹ {0:N2}", pageCalc);
                //    if (fDA != null) fDA.Text = string.Format(ci, "₹ {0:N2}", pageDA);
                //    if (fHotel != null) fHotel.Text = string.Format(ci, "₹ {0:N2}", pageHotel);
                //    if (fExpense != null) fExpense.Text = string.Format(ci, "₹ {0:N2}", pageExpense);
                //    if (fKM != null) fKM.Text = string.Format(ci, "{0:N2} km", pageKM);

                //    // Optionally add small tooltip with grand total (entire dataset) for accounting accuracy
                //    if (fCalc != null) fCalc.ToolTip = $"Grand total (all rows): {string.Format(ci, "₹ {0:N2}", grandCalc)}";
                //    if (fDA != null) fDA.ToolTip = $"Grand total (all rows): {string.Format(ci, "₹ {0:N2}", grandDA)}";
                //    if (fHotel != null) fHotel.ToolTip = $"Grand total (all rows): {string.Format(ci, "₹ {0:N2}", grandHotel)}";
                //    if (fExpense != null) fExpense.ToolTip = $"Grand total (all rows): {string.Format(ci, "₹ {0:N2}", grandExpense)}";
                //}

                // ✅ Append this block after gvActivities.DataBind(); and before any closing braces
                if (gvActivities.FooterRow != null && dt != null && dt.Rows.Count > 0)
                {
                    // --- Compute grand totals using DataTable.Compute() ---
                    //decimal grandCalc = 0m, grandDA = 0m, grandHotel = 0m, grandExpense = 0m;

                    object obj;
                    if (dt.Columns.Contains("CalculatedClaimAmount"))
                    {
                        obj = dt.Compute("SUM(CalculatedClaimAmount)", "");
                        if (obj != DBNull.Value) grandCalc = Convert.ToDecimal(obj);
                    }
                    if (dt.Columns.Contains("DAAmount"))
                    {
                        obj = dt.Compute("SUM(DAAmount)", "");
                        if (obj != DBNull.Value) grandDA = Convert.ToDecimal(obj);
                    }
                    if (dt.Columns.Contains("HotelAmount"))
                    {
                        obj = dt.Compute("SUM(HotelAmount)", "");
                        if (obj != DBNull.Value) grandHotel = Convert.ToDecimal(obj);
                    }
                    if (dt.Columns.Contains("TravelAmount"))
                    {
                        obj = dt.Compute("SUM(TravelAmount)", "");
                        if (obj != DBNull.Value) grandTravel = Convert.ToDecimal(obj);
                    }
                    if (dt.Columns.Contains("ExpenseTotal"))
                    {
                        obj = dt.Compute("SUM(ExpenseTotal)", "");
                        if (obj != DBNull.Value) grandExpense = Convert.ToDecimal(obj);
                    }

                    // --- Find footer labels and assign grand totals ---
                    Label fCalc = gvActivities.FooterRow.FindControl("lblFooterCalculated") as Label;
                    Label fDA = gvActivities.FooterRow.FindControl("lblFooterDA") as Label;
                    Label fHotel = gvActivities.FooterRow.FindControl("lblFooterHotel") as Label;
                    Label fTravel = gvActivities.FooterRow.FindControl("lblFooterTravel") as Label;
                    Label fExpense = gvActivities.FooterRow.FindControl("lblFooterExpenseTotal") as Label;

                    if (fCalc != null) fCalc.Text = "₹ " + grandCalc.ToString("N2");
                    if (fDA != null) fDA.Text = "₹ " + grandDA.ToString("N2");
                    if (fHotel != null) fHotel.Text = "₹ " + grandHotel.ToString("N2");
                    if (fTravel != null) fTravel.Text = "₹ " + grandTravel.ToString("N2");
                    if (fExpense != null) fExpense.Text = "₹ " + grandExpense.ToString("N2");

                    // Compute grand total of TotalDistanceKM from the DataTable
                    decimal grandTotalKM = 0m;
                    if (dt != null && dt.Rows.Count > 0 && dt.Columns.Contains("TotalDistanceKM"))
                    {
                        object objTotalKM = dt.Compute("SUM(TotalDistanceKM)", "");
                        if (objTotalKM != DBNull.Value)
                            grandTotalKM = Convert.ToDecimal(objTotalKM);
                    }

                    // Find footer label for KM and update it
                    Label lblFooterTotalKM = gvActivities.FooterRow.FindControl("lblFooterTotalKM") as Label;
                    if (lblFooterTotalKM != null)
                        lblFooterTotalKM.Text = string.Format("{0:N2} km", grandTotalKM); // or "{0:N2} km" if you don't want ₹

                    // Optional: Tooltip for confirmation
                    if (lblFooterTotalKM != null)
                        lblFooterTotalKM.ToolTip = "Grand Total KM (all rows): " + grandTotalKM.ToString("N2");

                    // Optional: tooltips for confirmation
                    if (fCalc != null) fCalc.ToolTip = "Grand Total Claim: ₹ " + grandCalc.ToString("N2");
                    if (fDA != null) fDA.ToolTip = "Grand Total DA: ₹ " + grandDA.ToString("N2");
                    if (fHotel != null) fHotel.ToolTip = "Grand Total Hotel: ₹ " + grandHotel.ToString("N2");
                    if (fTravel != null) fTravel.ToolTip = "Grand Total Hotel: ₹ " + grandTravel.ToString("N2");
                    if (fExpense != null) fExpense.ToolTip = "Grand Total Day Expense: ₹ " + grandExpense.ToString("N2");

                    // ✅ After computing grandCalc, grandDA, grandHotel, grandExpense

                    // Compute overall (combined) grand total
                    decimal grandOverall = grandCalc + grandDA + grandHotel + grandTravel;

                    // Find the last footer cell (the yellow one in your grid)
                    if (gvActivities.FooterRow != null)
                    {
                        int lastCellIndex = gvActivities.FooterRow.Cells.Count - 2;

                        gvActivities.FooterRow.Cells[lastCellIndex].Text = "₹ " + grandOverall.ToString("N2");
                        gvActivities.FooterRow.Cells[lastCellIndex].HorizontalAlign = HorizontalAlign.Center;
                        gvActivities.FooterRow.Cells[lastCellIndex].Font.Bold = true;
                        gvActivities.FooterRow.Cells[lastCellIndex].ForeColor = System.Drawing.Color.DarkBlue;
                        gvActivities.FooterRow.Cells[lastCellIndex].ToolTip = "Grand Total of Claim + DA + Hotel + Day Total";
                    }

                }


                Actionable.Visible = dt.Rows.Count > 0;
                //lbl_msg.Text = "Action executed successfully.";
                //lbl_msg.ForeColor = System.Drawing.Color.Green;
                //ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySuccess", "showPNotify('Success', 'Process executed successfully.', 'success');", true);

                // where you currently show success pnotify at end, gate it:
                if (showNotification)
                {
                    lbl_msg.Text = "Action executed successfully.";
                    lbl_msg.ForeColor = System.Drawing.Color.Green;
                    ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySuccess", "showPNotify('Success', 'Process executed successfully.', 'success');", true);
                }
                else
                {
                    lbl_msg.Text = "";
                }


            }
            catch (Exception ex)
            {
                lbl_msg.Text = "An error occurred: " + ex.Message;
                lbl_msg.ForeColor = System.Drawing.Color.Red;

                ScriptManager.RegisterStartupScript(
                    this,
                    GetType(),
                    "pnotifyError",
                    "showPNotify('Error', '" + ex.Message.Replace("'", "\\'") + "', 'error');",
                    true
                );
            }
        }
        private void BindGrid_Old_05Nov25(Boolean policybinder, bool showNotification = true)
        {
            lbl_msg.Text = "";
            int year = int.Parse(DDL_Year.SelectedValue);
            int month = int.Parse(DDL_Month.SelectedValue);

            bool isPolicyEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings["EnablePreviousMonthPolicy"]);
            bool allowExecution = false;

            if (isPolicyEnabled)
            {
                DateTime currentDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                                          TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                int currentYear = currentDate.Year;
                int currentMonth = currentDate.Month;

                bool isAllowedWindow = currentDate.Day >= 1 && currentDate.Day <= 15;
                bool isPreviousMonthSelected =
                    (year == currentYear && month == currentMonth - 1) ||
                    (currentMonth == 1 && month == 12 && year == currentYear - 1);

                if (isAllowedWindow && isPreviousMonthSelected)
                    allowExecution = true;
                else
                {
                    lbl_msg.Text = "You can process previous month data only between 1st and 15th of the current month.";
                    lbl_msg.ForeColor = System.Drawing.Color.Red;
                    ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyPolicyBlock",
                        "showPNotify('Policy Restriction', 'Processing is allowed only between 1st and 15th for the previous month.', 'error');", true);
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
                    {
                        BindPolicyToModal(policy);
                        //ShowPolicyNotification(policy);
                    }
                    else
                    {
                        RegisterClientNotification("No resolved policy found for the selected context.", "warning");
                    }
                }

                // --- Attempt to use cached session DataTable first ---
                DataTable dt = Session[SESSION_KEY_GV_ACTIVITIES] as DataTable;

                // If session copy not present, fetch from DB and cache it
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

                    // Ensure required columns exist
                    if (!dt.Columns.Contains("CalculatedClaimAmount"))
                        dt.Columns.Add("CalculatedClaimAmount", typeof(decimal));
                    if (!dt.Columns.Contains("RatePerKm"))
                        dt.Columns.Add("RatePerKm", typeof(decimal));

                    // compute derived columns
                    decimal defaultRate = 2.75m;
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

                    // Set primary key for quick lookup
                    if (dt.Columns.Contains("ActivityId"))
                    {
                        try
                        {
                            dt.PrimaryKey = new DataColumn[] { dt.Columns["ActivityId"] };
                        }
                        catch { /* ignore if already set or invalid */ }
                    }

                    // cache it
                    Session[SESSION_KEY_GV_ACTIVITIES] = dt;
                }

                // --- Bind grid from cached dt ---
                gvActivities.DataSource = dt;
                gvActivities.DataBind();

                gvActivities.UseAccessibleHeader = true;
                if (gvActivities.HeaderRow != null)
                    gvActivities.HeaderRow.TableSection = TableRowSection.TableHeader;

                // Compute grand totals from DataTable (the canonical source)
                decimal grandCalc = 0m, grandDA = 0m, grandHotel = 0m, grandTravel = 0m, grandExpense = 0m, grandKM = 0m;
                CultureInfo ci = CultureInfo.GetCultureInfo("en-IN");

                if (dt.Rows.Count > 0)
                {
                    object o;
                    o = dt.Compute("SUM(CalculatedClaimAmount)", "");
                    grandCalc = o != DBNull.Value ? Convert.ToDecimal(o) : 0m;

                    if (dt.Columns.Contains("DAAmount")) { o = dt.Compute("SUM(DAAmount)", ""); grandDA = o != DBNull.Value ? Convert.ToDecimal(o) : 0m; }
                    if (dt.Columns.Contains("HotelAmount")) { o = dt.Compute("SUM(HotelAmount)", ""); grandHotel = o != DBNull.Value ? Convert.ToDecimal(o) : 0m; }
                    if (dt.Columns.Contains("TravelAmount")) { o = dt.Compute("SUM(TravelAmount)", ""); grandTravel = o != DBNull.Value ? Convert.ToDecimal(o) : 0m; }
                    if (dt.Columns.Contains("ExpenseTotal")) { o = dt.Compute("SUM(ExpenseTotal)", ""); grandExpense = o != DBNull.Value ? Convert.ToDecimal(o) : 0m; }
                    if (dt.Columns.Contains("TotalDistanceKM")) { o = dt.Compute("SUM(TotalDistanceKM)", ""); grandKM = o != DBNull.Value ? Convert.ToDecimal(o) : 0m; }
                }

                // Update footer labels (grand totals)
                if (gvActivities.FooterRow != null)
                {
                    Label fCalc = gvActivities.FooterRow.FindControl("lblFooterCalculated") as Label;
                    Label fDA = gvActivities.FooterRow.FindControl("lblFooterDA") as Label;
                    Label fHotel = gvActivities.FooterRow.FindControl("lblFooterHotel") as Label;
                    Label fTravel = gvActivities.FooterRow.FindControl("lblFooterTravel") as Label;
                    Label fExpense = gvActivities.FooterRow.FindControl("lblFooterExpenseTotal") as Label;
                    Label lblFooterTotalKM = gvActivities.FooterRow.FindControl("lblFooterTotalKM") as Label;

                    if (fCalc != null) fCalc.Text = "₹ " + grandCalc.ToString("N2");
                    if (fDA != null) fDA.Text = "₹ " + grandDA.ToString("N2");
                    if (fHotel != null) fHotel.Text = "₹ " + grandHotel.ToString("N2");
                    if (fTravel != null) fTravel.Text = "₹ " + grandTravel.ToString("N2");
                    if (fExpense != null) fExpense.Text = "₹ " + grandExpense.ToString("N2");
                    if (lblFooterTotalKM != null) lblFooterTotalKM.Text = string.Format("{0:N2} km", grandKM);

                    // combined grand overall
                    decimal grandOverall = grandCalc + grandDA + grandHotel + grandTravel;
                    int lastCellIndex = Math.Max(0, gvActivities.FooterRow.Cells.Count - 5);
                    if (lastCellIndex < gvActivities.FooterRow.Cells.Count)
                    {
                        gvActivities.FooterRow.Cells[lastCellIndex].Text = "₹ " + grandOverall.ToString("N2");
                        gvActivities.FooterRow.Cells[lastCellIndex].HorizontalAlign = HorizontalAlign.Center;
                        gvActivities.FooterRow.Cells[lastCellIndex].Font.Bold = true;
                        gvActivities.FooterRow.Cells[lastCellIndex].ForeColor = System.Drawing.Color.DarkBlue;
                        gvActivities.FooterRow.Cells[lastCellIndex].ToolTip = "Grand Total of Claim + DA + Hotel + Travel";
                    }
                    else
                    {
                        System.Diagnostics.Trace.TraceWarning($"Footer cell index out of range. Cells.Count={gvActivities.FooterRow.Cells.Count}, computedIndex={lastCellIndex}");
                    }
                }

                Actionable.Visible = dt.Rows.Count > 0;

                if (showNotification)
                {
                    lbl_msg.Text = "Action executed successfully.";
                    lbl_msg.ForeColor = System.Drawing.Color.Green;
                    ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySuccess", "showPNotify('Success', 'Process executed successfully.', 'success');", true);
                }
                else
                {
                    lbl_msg.Text = "";
                }
            }
            catch (Exception ex)
            {
                lbl_msg.Text = "An error occurred: " + ex.Message;
                lbl_msg.ForeColor = System.Drawing.Color.Red;
                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyError", "showPNotify('Error', '" + ex.Message.Replace("'", "\\'") + "', 'error');", true);
            }
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
                DateTime currentDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                                          TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                int currentYear = currentDate.Year;
                int currentMonth = currentDate.Month;

                bool isAllowedWindow = currentDate.Day >= 1 && currentDate.Day <= 15;
                bool isPreviousMonthSelected =
                    (year == currentYear && month == currentMonth - 1) ||
                    (currentMonth == 1 && month == 12 && year == currentYear - 1);

                if (isAllowedWindow && isPreviousMonthSelected)
                    allowExecution = true;
                else
                {
                    lbl_msg.Text = "You can process previous month data only between 1st and 15th of the current month.";
                    lbl_msg.ForeColor = System.Drawing.Color.Red;
                    ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyPolicyBlock",
                        "showPNotify('Policy Restriction', 'Processing is allowed only between 1st and 15th for the previous month.', 'error');", true);
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
                    {
                        BindPolicyToModal(policy);
                        //ShowPolicyNotification(policy);
                    }
                    else
                    {
                        RegisterClientNotification("No resolved policy found for the selected context.", "warning");
                    }
                }

                DataTable dt = Session[SESSION_KEY_GV_ACTIVITIES] as DataTable;
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

                    if (!dt.Columns.Contains("CalculatedClaimAmount"))
                        dt.Columns.Add("CalculatedClaimAmount", typeof(decimal));
                    if (!dt.Columns.Contains("RatePerKm"))
                        dt.Columns.Add("RatePerKm", typeof(decimal));

                    decimal defaultRate = 2.75m;
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
                        try
                        {
                            dt.PrimaryKey = new DataColumn[] { dt.Columns["ActivityId"] };
                        }
                        catch { /* ignore if already set or invalid */ }
                    }
                    Session[SESSION_KEY_GV_ACTIVITIES] = dt;
                }

                // --- START: Approval lock logic ---
                if (!dt.Columns.Contains("IsEditable"))
                    dt.Columns.Add("IsEditable", typeof(bool));

                bool monthLocked = false;       // true => month-level edits blocked (except rejected lines)
                bool allowSubmission = false;   // true => allow user to submit/reshubmit the month
                int? approvalRequestId = null;
                string approvalStatus = null;
                string loginId = GetCurrentUserLoginId();

                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();

                    // get latest request if any
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT TOP 1 ApprovalRequestId, Status FROM dbo.SalesApprovalRequest " +
                        "WHERE RequesterLoginId = @LoginId AND YearNo = @Year AND MonthNo = @Month " +
                        "ORDER BY RequestDate DESC", con))
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

                    // Decide behaviour based on overall status
                    if (!approvalRequestId.HasValue)
                    {
                        // no request -> draft-like behavior: allow edits and submission
                        monthLocked = false;
                        allowSubmission = true;
                        foreach (DataRow row in dt.Rows) row["IsEditable"] = true;
                    }
                    else
                    {
                        // There is a request. Normalize status string for comparison:
                        string s = (approvalStatus ?? "").Trim();
                        if (string.Equals(s, "Draft", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(s, "Rejected", StringComparison.OrdinalIgnoreCase))
                        {
                            // allow edits and (re)submit
                            monthLocked = false;
                            allowSubmission = true;
                            foreach (DataRow row in dt.Rows) row["IsEditable"] = true;
                        }
                        else
                        {
                            // Submitted / Pending / Approved or other -> lock month-level editing
                            monthLocked = true;
                            allowSubmission = false;

                            // find per-row Rejected detail statuses so they remain editable
                            var editableActivityIds = new HashSet<int>();
                            using (SqlCommand cmd2 = new SqlCommand(
                                "SELECT ActivityId, DetailStatus FROM dbo.SalesApprovalRequestDetail WHERE ApprovalRequestId = @Id", con))
                            {
                                cmd2.Parameters.AddWithValue("@Id", approvalRequestId.Value);
                                using (SqlDataReader rdr = cmd2.ExecuteReader())
                                {
                                    while (rdr.Read())
                                    {
                                        int aid = rdr["ActivityId"] != DBNull.Value ? Convert.ToInt32(rdr["ActivityId"]) : 0;
                                        string detail = rdr["DetailStatus"] != DBNull.Value ? rdr["DetailStatus"].ToString() : null;
                                        if (string.Equals(detail, "Rejected", StringComparison.OrdinalIgnoreCase))
                                            editableActivityIds.Add(aid);
                                    }
                                }
                            }

                            // Mark only rejected lines as editable
                            foreach (DataRow row in dt.Rows)
                            {
                                int actId = row["ActivityId"] != DBNull.Value ? Convert.ToInt32(row["ActivityId"]) : 0;
                                row["IsEditable"] = editableActivityIds.Contains(actId);
                            }
                        }
                    }
                } // connection closed

                // Expose these flags to UI usage afterwards
                ViewState["MonthLocked"] = monthLocked;
                ViewState["AllowSubmission"] = allowSubmission;
                // --- END: Approval lock logic ---

                // after dt is prepared and cached:
                PopulateStatusSummary(dt);

                Summary.Visible = true;
                Activities.Visible = true;
                gvActivities.DataSource = dt;
                gvActivities.DataBind();

                gvActivities.UseAccessibleHeader = true;
                if (gvActivities.HeaderRow != null)
                    gvActivities.HeaderRow.TableSection = TableRowSection.TableHeader;

                // Compute grand totals from DataTable (the canonical source)
                decimal grandCalc = 0m, grandDA = 0m, grandHotel = 0m, grandTravel = 0m, grandExpense = 0m, grandKM = 0m;
                CultureInfo ci = CultureInfo.GetCultureInfo("en-IN");

                if (dt.Rows.Count > 0)
                {
                    object o;
                    o = dt.Compute("SUM(CalculatedClaimAmount)", "");
                    grandCalc = o != DBNull.Value ? Convert.ToDecimal(o) : 0m;

                    if (dt.Columns.Contains("DAAmount")) { o = dt.Compute("SUM(DAAmount)", ""); grandDA = o != DBNull.Value ? Convert.ToDecimal(o) : 0m; }
                    if (dt.Columns.Contains("HotelAmount")) { o = dt.Compute("SUM(HotelAmount)", ""); grandHotel = o != DBNull.Value ? Convert.ToDecimal(o) : 0m; }
                    if (dt.Columns.Contains("TravelAmount")) { o = dt.Compute("SUM(TravelAmount)", ""); grandTravel = o != DBNull.Value ? Convert.ToDecimal(o) : 0m; }
                    if (dt.Columns.Contains("ExpenseTotal")) { o = dt.Compute("SUM(ExpenseTotal)", ""); grandExpense = o != DBNull.Value ? Convert.ToDecimal(o) : 0m; }
                    if (dt.Columns.Contains("TotalDistanceKM")) { o = dt.Compute("SUM(TotalDistanceKM)", ""); grandKM = o != DBNull.Value ? Convert.ToDecimal(o) : 0m; }
                }

                // Update footer labels (grand totals)
                if (gvActivities.FooterRow != null)
                {
                    Label fCalc = gvActivities.FooterRow.FindControl("lblFooterCalculated") as Label;
                    Label fDA = gvActivities.FooterRow.FindControl("lblFooterDA") as Label;
                    Label fHotel = gvActivities.FooterRow.FindControl("lblFooterHotel") as Label;
                    Label fTravel = gvActivities.FooterRow.FindControl("lblFooterTravel") as Label;
                    Label fExpense = gvActivities.FooterRow.FindControl("lblFooterExpenseTotal") as Label;
                    Label lblFooterTotalKM = gvActivities.FooterRow.FindControl("lblFooterTotalKM") as Label;

                    if (fCalc != null) fCalc.Text = "₹ " + grandCalc.ToString("N2");
                    if (fDA != null) fDA.Text = "₹ " + grandDA.ToString("N2");
                    if (fHotel != null) fHotel.Text = "₹ " + grandHotel.ToString("N2");
                    if (fTravel != null) fTravel.Text = "₹ " + grandTravel.ToString("N2");
                    if (fExpense != null) fExpense.Text = "₹ " + grandExpense.ToString("N2");
                    if (lblFooterTotalKM != null) lblFooterTotalKM.Text = string.Format("{0:N2} km", grandKM);

                    // combined grand overall
                    decimal grandOverall = grandCalc + grandDA + grandHotel + grandTravel;
                    int lastCellIndex = Math.Max(0, gvActivities.FooterRow.Cells.Count - 5);
                    if (lastCellIndex < gvActivities.FooterRow.Cells.Count)
                    {
                        gvActivities.FooterRow.Cells[lastCellIndex].Text = "₹ " + grandOverall.ToString("N2");
                        gvActivities.FooterRow.Cells[lastCellIndex].HorizontalAlign = HorizontalAlign.Center;
                        gvActivities.FooterRow.Cells[lastCellIndex].Font.Bold = true;
                        gvActivities.FooterRow.Cells[lastCellIndex].ForeColor = System.Drawing.Color.DarkBlue;
                        gvActivities.FooterRow.Cells[lastCellIndex].ToolTip = "Grand Total of Claim + DA + Hotel + Travel";
                    }
                    else
                    {
                        System.Diagnostics.Trace.TraceWarning($"Footer cell index out of range. Cells.Count={gvActivities.FooterRow.Cells.Count}, computedIndex={lastCellIndex}");
                    }
                }

                // If the month is locked (there was a submission and it's not Rejected), hide/disable Actionable area
                Actionable.Visible = dt.Rows.Count > 0 && !monthLocked; // if monthLocked true => hide bulk actions

                // Also show a message to inform the user why editing is blocked
                if (monthLocked)
                {
                    lbl_msg.Text = "Monthly claims for this period have been submitted for approval. Editing is disabled except for rows that were rejected by approver.";
                    lbl_msg.ForeColor = System.Drawing.Color.Orange;
                    ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyLocked", "showPNotify('Submitted for Approval', 'Monthly claims are submitted. Editing disabled except rejected rows.', 'info');", true);
                }
                else
                {
                    if (showNotification)
                    {
                        lbl_msg.Text = "Action executed successfully.";
                        lbl_msg.ForeColor = System.Drawing.Color.Green;
                        ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySuccess", "showPNotify('Success', 'Process executed successfully.', 'success');", true);
                    }
                    else
                    {
                        lbl_msg.Text = "";
                    }
                }

                //bool allowSubmissionFlag = ViewState["AllowSubmission"] != null && (bool)ViewState["AllowSubmission"];
                //bool monthLockedFlag = ViewState["MonthLocked"] != null && (bool)ViewState["MonthLocked"];

                //// Show/hide / enable actionable panel/buttons
                //Actionable.Visible = dt.Rows.Count > 0 && !monthLockedFlag; // if locked, hide panel (you can also show but disable)
                //btn_fetch.Enabled = allowSubmissionFlag;  // assuming button ID is btnSubmitAll
                ////btnSaveSelected.Enabled = !monthLockedFlag;  // you may want to allow saving selected only when not locked

                // After gvActivities.DataBind() in BindGrid (or at the end of BindGrid)
                bool monthLockedFlag = ViewState["MonthLocked"] != null && (bool)ViewState["MonthLocked"];
                bool allowSubmissionFlag = ViewState["AllowSubmission"] != null && (bool)ViewState["AllowSubmission"];

                // Show/hide actionable panel and enable submission button appropriately
                Actionable.Visible = gvActivities.Rows.Count > 0 && !monthLockedFlag; // hide bulk actions when month locked
                btn_save_draft.Enabled = !monthLockedFlag;   // saving as draft allowed when month isn't locked
                btn_submit.Enabled = allowSubmissionFlag;    // submission allowed per allowSubmission flag

                // If you prefer to show the panel but disable buttons instead of hiding:
                // Actionable.Visible = gvActivities.Rows.Count > 0;
                // btn_save_draft.Enabled = !monthLockedFlag;
                // btn_submit.Enabled = allowSubmissionFlag;

            }
            catch (Exception ex)
            {
                lbl_msg.Text = "An error occurred: " + ex.Message;
                lbl_msg.ForeColor = System.Drawing.Color.Red;
                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyError", "showPNotify('Error', '" + ex.Message.Replace("'", "\\'") + "', 'error');", true);
            }
        }

        private void ResetExpenseEntryFields()
        {
            // Text inputs
            TXT_DAAmount.Text = string.Empty;
            TXT_DAFromPlace.Text = string.Empty;
            TXT_DAFromPlace.ReadOnly = false;
            TXT_DAToPlace.Text = string.Empty;
            TXT_DAToPlace.ReadOnly = false;
            TXT_TravelDescription.Text = string.Empty;
            TXT_TravelDescription.ReadOnly = false;

            TXT_HotelAmount.Text = string.Empty;
            TXT_HotelAmount.ReadOnly = false;
            TXT_HotelBillNo.Text = string.Empty;
            TXT_HotelBillNo.ReadOnly = false;
            TXT_HotelPurpose.Text = string.Empty;
            TXT_HotelPurpose.ReadOnly = false;

            TXT_TravelAmount.Text = string.Empty;
            TXT_TravelAmount.ReadOnly = false;
            TXT_TravelInvoiceNo.Text = string.Empty;
            TXT_TravelInvoiceNo.ReadOnly = false;

            // Dates (default to today IST)
            DateTime istNow = DateTime.UtcNow.AddMinutes(330);
            TXT_TravelDate.Text = istNow.ToString("yyyy-MM-dd");
            TXT_HotelFromDate.Text = istNow.ToString("yyyy-MM-dd");
            TXT_HotelToDate.Text = istNow.ToString("yyyy-MM-dd");

            // Checkboxes & dropdowns
            CHK_TravelInvoice.Checked = false;
            if (DDL_ExpenseType != null) DDL_ExpenseType.SelectedIndex = 0;
            if (DDL_TravelType != null) DDL_TravelType.SelectedIndex = 0;

            // Messages
            LBL_ExpenseMsg.Text = "";

            // 🔥 Clear hidden fields (MOST IMPORTANT)
            HF_ActivityId.Value = "";
            HF_ActivityDate.Value = "";
            HF_SnapResolvedDA.Value = "";
            HF_SnapPolicyMode.Value = "";

            // Clear file upload via JS
            ScriptManager.RegisterStartupScript(
                this,
                this.GetType(),
                "clearFile",
                $"$('#{FU_Attachment.ClientID}').val('');",
                true
            );
        }


        private bool SanitizeDecimal(string input, CultureInfo ci, out decimal result)
        {
            result = 0m;
            if (string.IsNullOrWhiteSpace(input)) return false;

            // Remove common non-numeric characters (₹ and other currency glyphs)
            // Keep digits, minus, dot and comma (commas handled by TryParse with culture)
            var cleaned = input.Replace("₹", "").Replace("Rs.", "").Replace("INR", "").Trim();

            // Remove any non-digit except '.' and ',' and '-' (defensive)
            // Note: Keep commas so TryParse with culture understands thousands separators
            var sb = new System.Text.StringBuilder();
            foreach (char c in cleaned)
            {
                if (char.IsDigit(c) || c == '.' || c == ',' || c == '-') sb.Append(c);
            }
            cleaned = sb.ToString();

            // Try parse with the provided culture (en-IN)
            return decimal.TryParse(cleaned, NumberStyles.Number | NumberStyles.AllowLeadingSign, ci, out result);
        }

        private void BindPolicyToModal_OLD(PolicyResolved policy)
        {
            if (policy == null)
                return;

            // Basic meta
            lblPolicyEmployeeCode.Text = HttpUtility.HtmlEncode(!string.IsNullOrEmpty(policy.EmployeeCode) ? policy.EmployeeCode : GetLoggedInEmployeeCode());
            lblPolicySource.Text = HttpUtility.HtmlEncode(!string.IsNullOrEmpty(policy.Source) ? policy.Source : "HR-SOP");
            lblPolicyRowId.Text = policy.RowId.HasValue ? policy.RowId.Value.ToString() : "—";
            lblPolicyPriority.Text = policy.Priority.HasValue ? policy.Priority.Value.ToString() : "—";
            //lblPolicySpecificity.Text = policy.Specificity.HasValue ? policy.Specificity.Value.ToString() : "No Data";

            // Build Specificity Description using Region / Branch / Grade / Designation
            //string regionText = policy.RegionId.HasValue ? $"R{policy.RegionId}" : "All Regions";
            //string branchText = policy.BranchId.HasValue ? $"B{policy.BranchId}" : "All Branches";
            //string gradeText = policy.GradeId.HasValue ? $"G{policy.GradeId}" : "All Grades";
            //string desigText = policy.DesignationId.HasValue ? $"D{policy.DesignationId}" : "All Designations";
            //// Combine into a readable string
            //string specificityCombo = $"{regionText} / {branchText} / {gradeText} / {desigText}";
            //// If your policy.Specificity numeric value still matters, you can append it
            //if (policy.Specificity.HasValue)
            //    specificityCombo += $"  (Spec: {policy.Specificity.Value})";
            //lblPolicySpecificity.Text = HttpUtility.HtmlEncode(specificityCombo);

            // --- Resolve textual specificity (Region / Branch / Grade / Designation) ---
            string regionName = GetNameFromMaster("MST_Region", "Id", "Region_Description", policy.RegionId);
            string branchName = GetNameFromMaster("MST_Branch", "Id", "Branch_Description", policy.BranchId);
            string gradeName = GetNameFromMaster("MST_Grade", "Id", "Grade_Description", policy.GradeId);
            string desigName = GetNameFromMaster("MST_Designation", "Id", "Designation_Description", policy.DesignationId);

            // Fallback defaults if null
            regionName = !string.IsNullOrEmpty(regionName) ? regionName : "All Regions";
            branchName = !string.IsNullOrEmpty(branchName) ? branchName : "All Branches";
            gradeName = !string.IsNullOrEmpty(gradeName) ? gradeName : "All Grades";
            desigName = !string.IsNullOrEmpty(desigName) ? desigName : "All Designations";

            // Build clean textual combination
            lblPolicySpecificity.Text = $"{regionName} / {branchName} / {gradeName} / {desigName}";

            // Effective / active
            var effFrom = policy.EffectiveFrom.HasValue ? policy.EffectiveFrom.Value.ToString("yyyy-MM-dd") : "—";
            var effTo = policy.EffectiveTo.HasValue ? policy.EffectiveTo.Value.ToString("yyyy-MM-dd") : "—";
            lblPolicyEffective.Text = $"{effFrom} to {effTo}";
            lblPolicyIsActive.Text = policy.IsActive.HasValue ? (policy.IsActive.Value ? "Yes" : "No") : "—";

            // Designation / Grade display (combine if available)
            string desig = policy.DesignationId.HasValue ? policy.DesignationId.Value.ToString() : null;
            string grade = policy.GradeId.HasValue ? policy.GradeId.Value.ToString() : null;
            lblPolicyDesigGrade.Text = !string.IsNullOrEmpty(desig) || !string.IsNullOrEmpty(grade) ?
                                        $"{(string.IsNullOrEmpty(desig) ? "—" : desig)} / {(string.IsNullOrEmpty(grade) ? "—" : grade)}" : "—";

            // Resolved numeric values (format to 2 decimals)
            lblResolvedMaxKM.Text = policy.Resolved_MaxKM.HasValue ? policy.Resolved_MaxKM.Value.ToString("N2") : "—";
            lblResolvedRatePerKm.Text = policy.Resolved_RatePerKm.HasValue ? policy.Resolved_RatePerKm.Value.ToString("N2") : "—";
            lblResolvedDA.Text = policy.Resolved_DA.HasValue ? policy.Resolved_DA.Value.ToString("N2") : "—";
            lblResolvedHotelCap.Text = policy.Resolved_HotelCap.HasValue ? policy.Resolved_HotelCap.Value.ToString("N2") : "—";

            // new: own arrangement allowance — either from a resolved column or from PolicyJson fallback
            decimal ownAllowance = 0;
            if (policy.Resolved_OwnAllowance.HasValue) ownAllowance = policy.Resolved_OwnAllowance.Value;
            else
            {
                // try parse from PolicyJson, e.g. PolicyJson contains "OwnArrangementAllowance":100
                try
                {
                    dynamic j = JsonConvert.DeserializeObject(policy.PolicyJson);
                    if (j != null && j.OwnArrangementAllowance != null) decimal.TryParse(Convert.ToString(j.OwnArrangementAllowance), out ownAllowance);
                }
                catch { }
            }


            // Remarks & RAW JSON
            lblResolvedRemarks.Text = HttpUtility.HtmlEncode(!string.IsNullOrEmpty(policy.Resolved_Remarks) ? policy.Resolved_Remarks : "Per Day Capping is Displayed");

            // Put raw JSON into the textbox but keep it hidden by default (toggle button shows it)
            policyJsonRaw.Text = policy.PolicyJson ?? "";
            policyJsonRaw.Visible = false;

            // Hidden fields (for possible future use)
            HF_Policy_RegionId.Value = policy.RegionId.HasValue ? policy.RegionId.Value.ToString() : string.Empty;
            HF_Policy_BranchId.Value = policy.BranchId.HasValue ? policy.BranchId.Value.ToString() : string.Empty;
            HF_Policy_GradeId.Value = policy.GradeId.HasValue ? policy.GradeId.Value.ToString() : string.Empty;
            HF_Policy_DesignationId.Value = policy.DesignationId.HasValue ? policy.DesignationId.Value.ToString() : string.Empty;
            HF_ResolvedHotelCap.Value = policy.Resolved_HotelCap.HasValue ? policy.Resolved_HotelCap.Value.ToString("F2") : "0";
            HF_ResolvedDA.Value = policy.Resolved_DA.HasValue ? policy.Resolved_DA.Value.ToString("F2") : "0";
            HF_ResolvedMaxKM.Value = policy.Resolved_MaxKM.HasValue ? policy.Resolved_MaxKM.Value.ToString("F2") : "0";
            HF_ResolvedRatePerKm.Value = policy.Resolved_RatePerKm.HasValue ? policy.Resolved_RatePerKm.Value.ToString("F2") : "0";
            //HF_ResolvedOwnAllowance.Value = ownAllowance.ToString("F2");

            // Clear page-level message label if you use it
            LBL_ExpenseMsg.Text = string.Empty;

            // Show modal (works with or without UpdatePanel)
            ShowPolicyModal();
        }

        protected void BindPolicyApprovers()
        {
            // read session values (use same keys you already set)
            string rOneName = (Session["RONE_NAME"] ?? string.Empty).ToString().Trim();
            string rOneCode = (Session["RONE_CODE"] ?? string.Empty).ToString().Trim();

            string rTwoName = (Session["RTWO_NAME"] ?? string.Empty).ToString().Trim();
            string rTwoCode = (Session["RTWO_CODE"] ?? string.Empty).ToString().Trim();

            // Auditor scope - keep placeholder session keys for future population
            // Set these from wherever you set RONE/RTWO (example below)
            string auditorName = (Session["AUDITOR_NAME"] ?? string.Empty).ToString().Trim();
            string auditorCode = (Session["AUDITOR_CODE"] ?? string.Empty).ToString().Trim();

            // Compose display text - prefer "Name (Code)" when code exists
            lblApproverL1.Text = ComposeNameWithCode(rOneName, rOneCode);
            lblApproverL2.Text = ComposeNameWithCode(rTwoName, rTwoCode);
            lblApproverAuditor.Text = ComposeNameWithCode(auditorName, auditorCode);

            // Toggle visibility of entire columns depending on availability
            approverL1Container.Visible = !string.IsNullOrEmpty(rOneName) || !string.IsNullOrEmpty(rOneCode);
            approverL2Container.Visible = !string.IsNullOrEmpty(rTwoName) || !string.IsNullOrEmpty(rTwoCode);
            approverAuditorContainer.Visible = !string.IsNullOrEmpty(auditorName) || !string.IsNullOrEmpty(auditorCode);
        }

        // small helper to create "Name (Code)" or fallback
        private string ComposeNameWithCode(string name, string code)
        {
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(code))
                return $"{HttpUtility.HtmlEncode(name)} ({HttpUtility.HtmlEncode(code)})";
            if (!string.IsNullOrEmpty(name))
                return HttpUtility.HtmlEncode(name);
            if (!string.IsNullOrEmpty(code))
                return $"({HttpUtility.HtmlEncode(code)})";
            return "—";
        }


        private void BindPolicyToModal(PolicyResolved policy)
        {
            if (policy == null) return;

            lblPolicyEmployeeCode.Text = HttpUtility.HtmlEncode(!string.IsNullOrEmpty(policy.EmployeeCode) ? policy.EmployeeCode : GetLoggedInEmployeeCode());
            lblPolicySource.Text = HttpUtility.HtmlEncode(!string.IsNullOrEmpty(policy.Source) ? policy.Source : "HR-SOP");
            lblPolicyRowId.Text = policy.RowId.HasValue ? policy.RowId.Value.ToString() : "—";
            lblPolicyPriority.Text = policy.Priority.HasValue ? policy.Priority.Value.ToString() : "—";

            string regionName = GetNameFromMaster("MST_Region", "Id", "Region_Description", policy.RegionId) ?? "All Regions";
            string branchName = GetNameFromMaster("MST_Branch", "Id", "Branch_Description", policy.BranchId) ?? "All Branches";
            string gradeName = GetNameFromMaster("MST_Grade", "Id", "Grade_Description", policy.GradeId) ?? "All Grades";
            string desigName = GetNameFromMaster("MST_Designation", "Id", "Designation_Description", policy.DesignationId) ?? "All Designations";

            lblPolicySpecificity.Text = $"{regionName} / {branchName} / {gradeName} / {desigName}";

            var effFrom = policy.EffectiveFrom.HasValue ? policy.EffectiveFrom.Value.ToString("dd-MM-yyyy") : "—";
            var effTo = policy.EffectiveTo.HasValue ? policy.EffectiveTo.Value.ToString("dd-MM-yyyy") : "—";
            lblPolicyEffective.Text = $"{effFrom} to {effTo}";
            lblPolicyIsActive.Text = policy.IsActive.HasValue ? (policy.IsActive.Value ? "Yes" : "No") : "—";
            lblPolicyDesigGrade.Text = (policy.DesignationId.HasValue || policy.GradeId.HasValue) ? $"{(policy.DesignationId.HasValue ? policy.DesignationId.Value.ToString() : "—")} / {(policy.GradeId.HasValue ? policy.GradeId.Value.ToString() : "—")}" : "—";

            // Numeric displays (formatting)
            lblResolvedMaxKM.Text = policy.Resolved_MaxKM.HasValue ? policy.Resolved_MaxKM.Value.ToString("N2") : "—";
            lblResolvedRatePerKm.Text = policy.Resolved_RatePerKm.HasValue ? policy.Resolved_RatePerKm.Value.ToString("N2") : "—";
            lblResolvedDA.Text = policy.Resolved_DA.HasValue ? policy.Resolved_DA.Value.ToString("N2") : "—";
            lblResolvedHotelCap.Text = policy.Resolved_HotelCap.HasValue ? policy.Resolved_HotelCap.Value.ToString("N2") : "—";

            // NEW fields: Outstation DA and Own Arrangement allowance display (if you have labels, otherwise store in hidden fields)
            // (Add labels if you want to show these explicitly in UI.)
            // Populate hidden fields used by ExpenseModal
            HF_Policy_RegionId.Value = policy.RegionId.HasValue ? policy.RegionId.Value.ToString() : string.Empty;
            HF_Policy_BranchId.Value = policy.BranchId.HasValue ? policy.BranchId.Value.ToString() : string.Empty;
            HF_Policy_GradeId.Value = policy.GradeId.HasValue ? policy.GradeId.Value.ToString() : string.Empty;
            HF_Policy_DesignationId.Value = policy.DesignationId.HasValue ? policy.DesignationId.Value.ToString() : string.Empty;

            // Hidden fields expected by expense modal
            HF_ResolvedHotelCap.Value = policy.Resolved_HotelCap.HasValue ? policy.Resolved_HotelCap.Value.ToString("F2") : "0";
            HF_ResolvedDA.Value = policy.Resolved_DA.HasValue ? policy.Resolved_DA.Value.ToString("F2") : "200";
            // NEW: Outstation / OwnArrangement hidden fields (these must exist in the expense modal markup)
            // If you don't already have them, add HF_OutstationDA and HF_OwnArrangementDA in expense modal.
            // If they exist on this page, set them (use FindControl or server-side IDs if on master page)
            var outstationVal = policy.Resolved_OutstationDA.HasValue ? policy.Resolved_OutstationDA.Value.ToString("F2") : "0";
            var ownArrVal = policy.Resolved_OwnAllowance.HasValue ? policy.Resolved_OwnAllowance.Value.ToString("F2") : "0";

            // try to find the HFs by ID and set values (these controls are on the page too)
            var hfOut = this.FindControl("HF_OutstationDA") as HiddenField;
            if (hfOut != null) hfOut.Value = outstationVal;
            var hfOwn = this.FindControl("HF_OwnArrangementDA") as HiddenField;
            if (hfOwn != null) hfOwn.Value = ownArrVal;
            HF_OutstationDA.Value = policy.Resolved_OutstationDA.HasValue ? policy.Resolved_OutstationDA.Value.ToString("F2") : "350";
            HF_OwnArrangementDA.Value = policy.Resolved_OwnAllowance.HasValue ? policy.Resolved_OwnAllowance.Value.ToString("F2") : "750";

            HF_ResolvedMaxKM.Value = policy.Resolved_MaxKM.HasValue ? policy.Resolved_MaxKM.Value.ToString("F2") : "0";
            HF_ResolvedRatePerKm.Value = policy.Resolved_RatePerKm.HasValue ? policy.Resolved_RatePerKm.Value.ToString("F2") : "0";

            // Remarks & raw JSON
            lblResolvedRemarks.Text = HttpUtility.HtmlEncode(!string.IsNullOrEmpty(policy.Resolved_Remarks) ? policy.Resolved_Remarks : "Per Day Capping is Displayed");
            policyJsonRaw.Text = policy.PolicyJson ?? "";
            policyJsonRaw.Visible = false;

            LBL_ExpenseMsg.Text = string.Empty;
            BindPolicyApprovers();
            // show policy modal
            //ShowPolicyModal();
        }

        private void ShowPolicyNotificationNew(PolicyResolved policy)
        {
            string source = !string.IsNullOrEmpty(policy.Source) ? policy.Source : "—";
            string priority = policy.Priority.HasValue ? policy.Priority.Value.ToString() : "—";
            string msg = $"Resolved policy loaded. Source: {source}, Priority: {priority}";

            RegisterClientNotification(msg, "info");
        }

        private string GetNameFromMaster(string tableName, string idColumn, string textColumn, int? id)
        {
            if (!id.HasValue)
                return null;

            string name = null;
            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (var con = new SqlConnection(connStr))
            using (var cmd = new SqlCommand($"SELECT TOP 1 {textColumn} FROM {tableName} WHERE {idColumn} = @Id", con))
            {
                cmd.Parameters.AddWithValue("@Id", id.Value);
                con.Open();
                var result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                    name = Convert.ToString(result);
            }

            return name;
        }

        private void RegisterClientNotification(string message, string severity = "info")
        {
            // Escape for JS string literal
            string jsMessage = HttpUtility.JavaScriptStringEncode(message);

            // Simple script: prefer PNotify or toastr if present, else alert()
            string script = $@"
                (function() {{
                    var msg = '{jsMessage}';
                    try {{
                        if (window.PNotify) {{
                            // PNotify 3 usage pattern (adjust if you use v4)
                            new PNotify({{ text: msg, type: '{severity}', delay: 2500 }});
                        }} else if (window.toastr) {{
                            toastr['{severity}'](msg);
                        }} else {{
                            alert(msg);
                        }}
                    }} catch(e) {{
                        try {{ alert(msg); }} catch(e){{}}
                    }}
                }})();";

            if (ScriptManager.GetCurrent(Page) != null)
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "policyNotify_" + Guid.NewGuid().ToString("N"), script, true);
            }
            else
            {
                ClientScript.RegisterStartupScript(this.GetType(), "policyNotify_" + Guid.NewGuid().ToString("N"), script, true);
            }
        }

        private void ShowPolicyModal()
        {
            string script = "$('#policyModal').modal('show');";

            if (ScriptManager.GetCurrent(Page) != null)
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "showPolicyModal", script, true);
            }
            else
            {
                ClientScript.RegisterStartupScript(this.GetType(), "showPolicyModal", script, true);
            }
        }
        private decimal GetRatePerKm(DataRow row, decimal fallbackRate)
        {
            if (row == null) return fallbackRate;

            // check several common column names your SP might return
            string[] rateCols = new[] { "RatePerKm", "Rate", "PerKmRate", "RatePerKM", "ClaimRate" };

            foreach (var col in rateCols)
            {
                if (row.Table.Columns.Contains(col) && row[col] != DBNull.Value)
                {
                    decimal val;
                    if (decimal.TryParse(row[col].ToString(), out val))
                        return val;
                }
            }

            // optional: if you have per-user or per-role rate logic, call it here instead of fallback
            return fallbackRate;
        }

        protected void gvActivities_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            //if (e.CommandName == "OpenExpenses")
            //{
            //    string arg = e.CommandArgument.ToString();
            //    var parts = arg.Split('|');
            //    string activityId = parts[0];
            //    string activityDate = parts.Length > 1 ? parts[1] : "";

            //    HF_ActivityId.Value = activityId;
            //    HF_ActivityDate.Value = activityDate;

            //    // Load existing expenses and get the DataTable back
            //    DataTable dt = LoadExpenseForActivity(activityId);

            //    // If no expense rows exist for this activity, CLEAR the modal inputs so user can enter new ones
            //    if (dt == null || dt.Rows.Count == 0)
            //    {
            //        //TXT_DAAmount.Text = string.Empty;
            //        TXT_DAAmount.Text = lblResolvedDA.Text.ToString();
            //        TXT_HotelAmount.Text = string.Empty;
            //        TXT_HotelBillNo.Text = string.Empty;
            //        TXT_HotelPurpose.Text = string.Empty;

            //        // Set hotel dates to activity date if available, else today's IST
            //        DateTime parsed;
            //        if (!string.IsNullOrWhiteSpace(activityDate) && DateTime.TryParse(activityDate, out parsed))
            //        {
            //            TXT_HotelFromDate.Text = parsed.ToString("yyyy-MM-dd");
            //            TXT_HotelToDate.Text = parsed.ToString("yyyy-MM-dd");
            //            TXT_TravelDate.Text = parsed.ToString("yyyy-MM-dd");
            //            lbl_traveldate.Text = parsed.ToString("dd-MM-yyyy");
            //        }
            //        else
            //        {
            //            var ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            //            var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ist);
            //            TXT_HotelFromDate.Text = nowIst.ToString("yyyy-MM-dd");
            //            TXT_TravelDate.Text = nowIst.ToString("yyyy-MM-dd");
            //        }

            //        // Clear any prior messages
            //        LBL_ExpenseMsg.Text = "";

            //        // Clear the file input via client script (FileUpload value cannot be set server-side)
            //        ScriptManager.RegisterStartupScript(this, this.GetType(), "clearFileInput_" + FU_Attachment.ClientID, $"$('#{FU_Attachment.ClientID}').val('');", true);
            //    }
            //    else
            //    {
            //        // aggregate/collect values (handles any combination of DA/Hotel rows)
            //        decimal daSum = 0m;
            //        decimal hotelSum = 0m;
            //        DateTime? hotelFrom = null;
            //        DateTime? hotelTo = null;
            //        string hotelBill = string.Empty;
            //        string hotelPurpose = string.Empty;

            //        foreach (DataRow r in dt.Rows)
            //        {
            //            string type = (r["ExpenseType"] ?? "").ToString();
            //            decimal amt = r["Amount"] != DBNull.Value ? Convert.ToDecimal(r["Amount"]) : 0m;

            //            if (string.Equals(type, "DA", StringComparison.OrdinalIgnoreCase))
            //            {
            //                TXT_DAAmount.Text = amt.ToString();
            //                daSum += amt;
            //            }
            //            else if (string.Equals(type, "Hotel", StringComparison.OrdinalIgnoreCase))
            //            {
            //                hotelSum += amt;

            //                // collect from/to range (earliest from, latest to)
            //                if (r["FromDate"] != DBNull.Value)
            //                {
            //                    DateTime f = Convert.ToDateTime(r["FromDate"]);
            //                    hotelFrom = !hotelFrom.HasValue ? f : (f < hotelFrom ? f : hotelFrom);
            //                }
            //                if (r["ToDate"] != DBNull.Value)
            //                {
            //                    DateTime t = Convert.ToDateTime(r["ToDate"]);
            //                    hotelTo = !hotelTo.HasValue ? t : (t > hotelTo ? t : hotelTo);
            //                }

            //                // pick first non-empty BillNo and Purpose (change logic here if you prefer last)
            //                if (string.IsNullOrWhiteSpace(hotelBill) && r["BillNo"] != DBNull.Value)
            //                    hotelBill = r["BillNo"].ToString();
            //                if (string.IsNullOrWhiteSpace(hotelPurpose) && r["Purpose"] != DBNull.Value)
            //                    hotelPurpose = r["Purpose"].ToString();
            //            }
            //        }

            //        // If no hotel date range found in DB, try to use HF_ActivityDate or fallback to IST today
            //        if (!hotelFrom.HasValue || !hotelTo.HasValue)
            //        {
            //            DateTime parsedActivity;
            //            if (!string.IsNullOrWhiteSpace(HF_ActivityDate.Value) && DateTime.TryParse(HF_ActivityDate.Value, out parsedActivity))
            //            {
            //                // use activity date for missing values
            //                if (!hotelFrom.HasValue) hotelFrom = parsedActivity.Date;
            //                if (!hotelTo.HasValue) hotelTo = parsedActivity.Date;
            //            }
            //            else
            //            {
            //                // fallback to IST today
            //                var ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            //                var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ist).Date;
            //                if (!hotelFrom.HasValue) hotelFrom = nowIst;
            //                if (!hotelTo.HasValue) hotelTo = nowIst;
            //            }
            //        }

            //        // Populate controls independently so missing categories don't affect the other
            //        //TXT_DAAmount.Text = daSum > 0m ? daSum.ToString("0.##") : string.Empty;
            //        //TXT_HotelAmount.Text = hotelSum > 0m ? hotelSum.ToString("0.##") : string.Empty;

            //        TXT_HotelFromDate.Text = hotelFrom.HasValue ? hotelFrom.Value.ToString("yyyy-MM-dd") : string.Empty;
            //        TXT_HotelToDate.Text = hotelTo.HasValue ? hotelTo.Value.ToString("yyyy-MM-dd") : string.Empty;
            //        TXT_TravelDate.Text = hotelTo.HasValue ? hotelTo.Value.ToString("yyyy-MM-dd") : string.Empty;

            //        lbl_traveldate.Text = hotelFrom.Value.ToString("dd-MM-yyyy");
            //        //TXT_HotelBillNo.Text = hotelBill;
            //        //TXT_HotelPurpose.Text = hotelPurpose;

            //        // Keep existing attachments visible in the Repeater. Do NOT clear the FileUpload here.
            //        LBL_ExpenseMsg.Text = string.Empty;
            //    }



            //    // show modal reliably
            //    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
            //}


            if (e.CommandName == "OpenExpenses")
            {
                ResetExpenseEntryFields();

                string arg = e.CommandArgument.ToString();
                var parts = arg.Split('|');
                string activityId = parts[0];
                string activityDate = parts.Length > 1 ? parts[1] : "";

                string PlannedBeat = (parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2])) ? parts[2] : "-";
                string OrderDistributorName = (parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3])) ? parts[3] : "-";
                string TotalDistance = (parts.Length > 4 && !string.IsNullOrWhiteSpace(parts[4])) ? parts[4] : "-";
                lbl_BeatTravelled.Text = PlannedBeat;
                lbl_DistributorName.Text = OrderDistributorName;
                lbl_HQTravelDistance.Text = TotalDistance;

                HF_ActivityId.Value = activityId;
                HF_ActivityDate.Value = activityDate;

                // Load existing expenses and get the DataTable back (this method binds rptExpenses and populates HF_Snap* if rows exist)
                DataTable dt = LoadExpenseForActivity(activityId);

                // after LoadExpenseForActivity(...)
                string policyModeToSelect = !string.IsNullOrWhiteSpace(HF_SnapPolicyMode?.Value)
                    ? HF_SnapPolicyMode.Value.Trim()
                    : (!string.IsNullOrWhiteSpace(HF_ResolvedPolicyMode?.Value) ? HF_ResolvedPolicyMode.Value.Trim() : "");

                if (!string.IsNullOrWhiteSpace(policyModeToSelect))
                {
                    // escape single quotes for JS
                    string safe = policyModeToSelect.Replace("'", "\\'");
                    string script = $@"
                    (function(){{
                        var setVal = function(){{
                            var el = document.getElementById('SEL_PolicyMode');
                            if (!el) el = document.querySelector('[id$=""_SEL_PolicyMode""]');
                            if (el) {{
                                try {{ el.value = '{safe}'; $(el).trigger('change'); }}
                                catch(e) {{ try {{ el.value = '{safe}'; }} catch(e2) {{}} }}
                            }}
                        }};
                        // run immediately and also when modal shown (defensive)
                        setVal();
                        $('#expenseModal').on('shown.bs.modal.selectPolicy', setVal);
                    }})();";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "selectPolicyMode_" + HF_ActivityId.Value, script, true);
                }


                //// If no expense rows exist for this activity, CLEAR the modal inputs so user can enter new ones
                //if (dt == null || dt.Rows.Count == 0)
                //{
                //    // --- Ensure expense-level snapshot HFs are available ---
                //    // If LoadExpenseForActivity didn't populate HF_Snap* (no rows), copy from master/employee-level HFs if present.
                //    try
                //    {
                //        // Only copy if snapshot HF is empty - prevents overwriting if already set elsewhere
                //        if (string.IsNullOrWhiteSpace(HF_SnapPolicyMode?.Value) && HF_ResolvedPolicyMode != null && !string.IsNullOrWhiteSpace(HF_ResolvedPolicyMode.Value))
                //            HF_SnapPolicyMode.Value = HF_ResolvedPolicyMode.Value;

                //        if (string.IsNullOrWhiteSpace(HF_SnapResolvedDA?.Value) && HF_ResolvedDA != null && !string.IsNullOrWhiteSpace(HF_ResolvedDA.Value))
                //            HF_SnapResolvedDA.Value = HF_ResolvedDA.Value;

                //        if (string.IsNullOrWhiteSpace(HF_SnapHotelCap?.Value) && HF_ResolvedHotelCap != null && !string.IsNullOrWhiteSpace(HF_ResolvedHotelCap.Value))
                //            HF_SnapHotelCap.Value = HF_ResolvedHotelCap.Value;

                //        if (string.IsNullOrWhiteSpace(HF_SnapOutstationDA?.Value) && HF_OutstationDA != null && !string.IsNullOrWhiteSpace(HF_OutstationDA.Value))
                //            HF_SnapOutstationDA.Value = HF_OutstationDA.Value;

                //        if (string.IsNullOrWhiteSpace(HF_SnapOwnArrangementDA?.Value) && HF_OwnArrangementDA != null && !string.IsNullOrWhiteSpace(HF_OwnArrangementDA.Value))
                //            HF_SnapOwnArrangementDA.Value = HF_OwnArrangementDA.Value;
                //    }
                //    catch
                //    {
                //        // ignore if any HF missing — fallback below
                //    }

                //    // TXT_DAAmount gets snapshot resolved DA when available; otherwise fallback to lblResolvedDA
                //    if (!string.IsNullOrWhiteSpace(HF_SnapResolvedDA?.Value))
                //        TXT_DAAmount.Text = HF_SnapResolvedDA.Value;
                //    else if (lblResolvedDA != null && !string.IsNullOrWhiteSpace(lblResolvedDA.Text))
                //        TXT_DAAmount.Text = lblResolvedDA.Text;
                //    else
                //        TXT_DAAmount.Text = string.Empty;

                //    TXT_HotelAmount.Text = string.Empty;
                //    TXT_HotelBillNo.Text = string.Empty;
                //    TXT_HotelPurpose.Text = string.Empty;

                //    // Set hotel/travel dates to activity date if available, else today's IST
                //    DateTime parsed;
                //    if (!string.IsNullOrWhiteSpace(activityDate) && DateTime.TryParse(activityDate, out parsed))
                //    {
                //        TXT_HotelFromDate.Text = parsed.ToString("yyyy-MM-dd");
                //        TXT_HotelToDate.Text = parsed.ToString("yyyy-MM-dd");
                //        TXT_TravelDate.Text = parsed.ToString("yyyy-MM-dd");
                //        lbl_traveldate.Text = parsed.ToString("dd-MM-yyyy");
                //    }
                //    else
                //    {
                //        var ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                //        var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ist);
                //        TXT_HotelFromDate.Text = nowIst.ToString("yyyy-MM-dd");
                //        TXT_TravelDate.Text = nowIst.ToString("yyyy-MM-dd");
                //        lbl_traveldate.Text = nowIst.ToString("dd-MM-yyyy");
                //    }

                //    // Clear any prior messages
                //    LBL_ExpenseMsg.Text = "";
                //    HF_SnapPolicyMode.Value = "HQ";
                //    ApplyPolicyMode_ClientCall("HQ");

                //    // Clear the file input via client script (FileUpload value cannot be set server-side)
                //    ScriptManager.RegisterStartupScript(this, this.GetType(), "clearFileInput_" + FU_Attachment.ClientID, $"$('#{FU_Attachment.ClientID}').val('');", true);
                //}

                if (dt == null || dt.Rows.Count == 0)
                {
                    // 👉 NEW entry for this date (no saved expenses)

                    // Use policy resolved defaults ONLY (fresh snapshot)
                    if (!string.IsNullOrWhiteSpace(HF_ResolvedPolicyMode?.Value))
                        HF_SnapPolicyMode.Value = HF_ResolvedPolicyMode.Value;

                    if (!string.IsNullOrWhiteSpace(HF_ResolvedDA?.Value))
                        HF_SnapResolvedDA.Value = HF_ResolvedDA.Value;

                    if (!string.IsNullOrWhiteSpace(HF_ResolvedHotelCap?.Value))
                        HF_SnapHotelCap.Value = HF_ResolvedHotelCap.Value;

                    if (!string.IsNullOrWhiteSpace(HF_OutstationDA?.Value))
                        HF_SnapOutstationDA.Value = HF_OutstationDA.Value;

                    if (!string.IsNullOrWhiteSpace(HF_OwnArrangementDA?.Value))
                        HF_SnapOwnArrangementDA.Value = HF_OwnArrangementDA.Value;

                    // Apply default DA amount (policy-based)
                    TXT_DAAmount.Text = HF_SnapResolvedDA.Value ?? "";

                    DateTime parsed;
                    // Date defaults = selected activity date
                    if (!string.IsNullOrWhiteSpace(activityDate) && DateTime.TryParse(activityDate, out parsed))
                    {
                        TXT_HotelFromDate.Text = parsed.ToString("yyyy-MM-dd");
                        TXT_HotelToDate.Text = parsed.ToString("yyyy-MM-dd");
                        TXT_TravelDate.Text = parsed.ToString("yyyy-MM-dd");
                        lbl_traveldate.Text = parsed.ToString("dd-MM-yyyy");
                    }

                    // Policy UI sync
                    if (!string.IsNullOrWhiteSpace(HF_SnapPolicyMode.Value))
                        ApplyPolicyMode_ClientCall(HF_SnapPolicyMode.Value);

                    // Clear file input (browser-side)
                    ScriptManager.RegisterStartupScript(
                        this, GetType(),
                        "clearFile_" + activityId,
                        $"$('#{FU_Attachment.ClientID}').val('');",
                        true
                    );
                }
                else
                {
                    // aggregate/collect values (handles any combination of DA/Hotel rows). Uses DB values only (authoritative).
                    decimal daSum = 0m;
                    decimal hotelSum = 0m;
                    DateTime? hotelFrom = null;
                    DateTime? hotelTo = null;
                    string hotelBill = string.Empty;
                    string hotelPurpose = string.Empty;

                    foreach (DataRow r in dt.Rows)
                    {
                        string type = (r["ExpenseType"] ?? "").ToString();
                        decimal amt = r["Amount"] != DBNull.Value ? Convert.ToDecimal(r["Amount"]) : 0m;

                        if (string.Equals(type, "DA", StringComparison.OrdinalIgnoreCase))
                        {
                            // show last DA amount as single value, keep sum for totals
                            TXT_DAAmount.Text = amt.ToString("0.##");
                            daSum += amt;
                        }
                        else if (string.Equals(type, "Hotel", StringComparison.OrdinalIgnoreCase))
                        {
                            hotelSum += amt;

                            // collect from/to range (earliest from, latest to)
                            if (r["FromDate"] != DBNull.Value)
                            {
                                DateTime f = Convert.ToDateTime(r["FromDate"]);
                                hotelFrom = !hotelFrom.HasValue ? f : (f < hotelFrom ? f : hotelFrom);
                            }
                            if (r["ToDate"] != DBNull.Value)
                            {
                                DateTime t = Convert.ToDateTime(r["ToDate"]);
                                hotelTo = !hotelTo.HasValue ? t : (t > hotelTo ? t : hotelTo);
                            }

                            // pick first non-empty BillNo and Purpose (change logic here if you prefer last)
                            if (string.IsNullOrWhiteSpace(hotelBill) && r["BillNo"] != DBNull.Value)
                                hotelBill = r["BillNo"].ToString();
                            if (string.IsNullOrWhiteSpace(hotelPurpose) && r["Purpose"] != DBNull.Value)
                                hotelPurpose = r["Purpose"].ToString();
                        }
                    }

                    // If no hotel date range found in DB, try to use HF_ActivityDate or fallback to IST today
                    if (!hotelFrom.HasValue || !hotelTo.HasValue)
                    {
                        DateTime parsedActivity;
                        if (!string.IsNullOrWhiteSpace(HF_ActivityDate.Value) && DateTime.TryParse(HF_ActivityDate.Value, out parsedActivity))
                        {
                            // use activity date for missing values
                            if (!hotelFrom.HasValue) hotelFrom = parsedActivity.Date;
                            if (!hotelTo.HasValue) hotelTo = parsedActivity.Date;
                        }
                        else
                        {
                            // fallback to IST today
                            var ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                            var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ist).Date;
                            if (!hotelFrom.HasValue) hotelFrom = nowIst;
                            if (!hotelTo.HasValue) hotelTo = nowIst;
                        }
                    }

                    // Populate controls independently so missing categories don't affect the other
                    TXT_HotelFromDate.Text = hotelFrom.HasValue ? hotelFrom.Value.ToString("yyyy-MM-dd") : string.Empty;
                    TXT_HotelToDate.Text = hotelTo.HasValue ? hotelTo.Value.ToString("yyyy-MM-dd") : string.Empty;
                    TXT_TravelDate.Text = hotelTo.HasValue ? hotelTo.Value.ToString("yyyy-MM-dd") : string.Empty;

                    lbl_traveldate.Text = hotelFrom.HasValue ? hotelFrom.Value.ToString("dd-MM-yyyy") : "NA";
                    TXT_HotelBillNo.Text = hotelBill;
                    TXT_HotelPurpose.Text = hotelPurpose;

                    //HF_SnapPolicyMode.Value = "Daily";
                    //ApplyPolicyMode_ClientCall("Daily");

                    // Keep existing attachments visible in the Repeater. Do NOT clear the FileUpload here.
                    LBL_ExpenseMsg.Text = string.Empty;
                }

                //HF_SnapPolicyMode.Value = "Daily";
                //ClickApplyButtonFromServer("Daily");
                //ApplyPolicyModeFromCodeBehind("Daily");
                //ApplyPolicyMode_ClientCall("Daily");
                // show modal reliably (unique key)
                string skey = "showExpense_" + activityId + "_" + Guid.NewGuid().ToString("N");
                ScriptManager.RegisterStartupScript(this, this.GetType(), skey, "$('#expenseModal').modal('show');", true);
            }

            else if (e.CommandName == "OpenEditModal")
            {
                //// CommandArgument: ActivityId|TotalDistanceKM|MinAllowedKM|MaxAllowedKM
                //string arg = e.CommandArgument as string;
                //if (string.IsNullOrEmpty(arg)) return;

                //string[] parts = arg.Split('|');
                //string activityId = parts.Length > 0 ? parts[0] : "";
                //string currentKm = parts.Length > 1 ? parts[1] : "0";
                //string minKm = parts.Length > 2 ? parts[2] : "";
                //string maxKm = parts.Length > 3 ? parts[3] : "";

                //// set server controls (HiddenField, TextBox, labels)
                //hdnActivityId.Value = activityId;
                //txtModalKM.Text = currentKm;
                //lblModalCurrentKm.Text = "Current: " + currentKm + " km";

                //// call client function to open modal and pass min/max (use RegisterStartupScript)
                //string script = string.Format("openEditKmModal('{0}', '{1}', '{2}', '{3}');",
                //    activityId.Replace("'", "\\'"),
                //    currentKm.Replace("'", "\\'"),
                //    minKm.Replace("'", "\\'"),
                //    maxKm.Replace("'", "\\'")
                //);

                //ScriptManager.RegisterStartupScript(this, this.GetType(), "openEditKmModalScript", script, true);

                // CommandArgument expected to be: ActivityId|TotalDistanceKM|MinAllowedKM|MaxAllowedKM
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
                {
                    lbl_kmmodal_date.Text = parsed.ToString("dd-MM-yyyy");
                }
                // set modal controls
                //hdnActivityId.Value = activityId.ToString();
                //txtModalKM.Text = currentKm;
                //lblModalCurrentKm.Text = "Current: " + currentKm + " km";

                hdnActivityId.Value = activityId.ToString();
                txtModalKM.Text = currentKm;
                hdnOriginalKm.Value = currentKm;               // <--- set original KM
                lblModalCurrentKm.Text = "Current: " + currentKm + " km";

                // set JS-visible min/max - we will pass them to the modal UI via RegisterStartupScript
                string minDisplay = string.IsNullOrEmpty(minKm) ? "—" : minKm;
                string maxDisplay = string.IsNullOrEmpty(maxKm) ? "—" : maxKm;

                // bind history
                BindKmHistory(activityId);

                // ensure modal opens after server processing
                string modalId = "editKmModal"; // plain id (we used static id in markup)
                string script = string.Format("setTimeout(function(){{ $('#{0}').modal('show'); $('#spMinKm').text('{1}'); $('#spMaxKm').text('{2}'); }}, 50);",
                    modalId, minDisplay.Replace("'", "\\'"), maxDisplay.Replace("'", "\\'"));

                ScriptManager.RegisterStartupScript(this, this.GetType(), "openEditKmModal_" + activityId, script, true);
            }

            else if (e.CommandName == "EditRow")
            {
                int activityId;
                if (int.TryParse(e.CommandArgument.ToString(), out activityId))
                {
                    int rowIndex = FindRowIndexByActivityId(activityId);
                    if (rowIndex >= 0)
                    {
                        gvActivities.EditIndex = rowIndex;
                        Session.Remove(SESSION_KEY_GV_ACTIVITIES);
                        BindGrid(false);
                    }
                }
            }

            else if (e.CommandName == "UpdateRow")
            {
                int activityId = Convert.ToInt32(e.CommandArgument);
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
                    Session.Remove(SESSION_KEY_GV_ACTIVITIES);
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
            if (!int.TryParse(HF_ActivityId?.Value, out activityId) || activityId <= 0)
            {
                RegisterClientNotification("Invalid activity selected for KM edit.", "error");
                return;
            }

            // Try Session cache first (fast)
            DataTable dt = Session[SESSION_KEY_GV_ACTIVITIES] as DataTable;
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

            if (row != null)
                PopulateEditKmModalFromRow(row);
            else
                PopulateEditKmModalFromDb(activityId);

            // Optionally hide expense modal immediately so inner modal appears on top
            // Register client script to hide expenseModal just before edit modal shows
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

            string minKm = row.Table.Columns.Contains("MinAllowedKM") && row["MinAllowedKM"] != DBNull.Value
                           ? row["MinAllowedKM"].ToString() : "";
            string maxKm = row.Table.Columns.Contains("MaxAllowedKM") && row["MaxAllowedKM"] != DBNull.Value
                           ? row["MaxAllowedKM"].ToString() : "";
            string activityDate = row.Table.Columns.Contains("ActivityDate") && row["ActivityDate"] != DBNull.Value
                                  ? Convert.ToDateTime(row["ActivityDate"]).ToString("dd-MM-yyyy") : "";

            // set controls
            hdnActivityId.Value = activityId.ToString();
            txtModalKM.Text = currentKm;
            hdnOriginalKm.Value = currentKm;
            lblModalCurrentKm.Text = "Current: " + currentKm + " km";
            if (!string.IsNullOrEmpty(activityDate)) lbl_kmmodal_date.Text = activityDate;

            string minDisplay = string.IsNullOrEmpty(minKm) ? "—" : minKm;
            string maxDisplay = string.IsNullOrEmpty(maxKm) ? "—" : maxKm;

            // bind history (your existing method)
            BindKmHistory(activityId);

            // hide outer and show inner modal (small timeout to ensure hide completes)
            string script = string.Format("setTimeout(function(){{ $('#{0}').modal('show'); $('#spMinKm').text('{1}'); $('#spMaxKm').text('{2}'); }}, 150);",
                "editKmModal", minDisplay.Replace("'", "\\'"), maxDisplay.Replace("'", "\\'"));
            ScriptManager.RegisterStartupScript(this, this.GetType(), "openEditKmModal_" + activityId, script, true);

            // ⭐ Paste this RIGHT BELOW the above:
            string reopen = "$('#editKmModal').one('hidden.bs.modal', function(){ $('#expenseModal').modal('show'); });";
            ScriptManager.RegisterStartupScript(this, this.GetType(), "reopenExpenseModal_" + activityId, reopen, true);
        }

        private void PopulateEditKmModalFromDb(int activityId)
        {
            if (activityId <= 0) { RegisterClientNotification("Activity not found.", "warning"); return; }

            using (SqlConnection con = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand(
                "SELECT TOP 1 ActivityId, TotalDistanceKM, MinAllowedKM, MaxAllowedKM, ActivityDate FROM dbo.SalesDailyActivity WHERE ActivityId = @ActivityId", con))
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

                        hdnActivityId.Value = activityId.ToString();
                        txtModalKM.Text = currentKm;
                        hdnOriginalKm.Value = currentKm;
                        lblModalCurrentKm.Text = "Current: " + currentKm + " km";
                        if (aDate.HasValue) lbl_kmmodal_date.Text = aDate.Value.ToString("dd-MM-yyyy");

                        string minDisplay = string.IsNullOrEmpty(minKm) ? "—" : minKm;
                        string maxDisplay = string.IsNullOrEmpty(maxKm) ? "—" : maxKm;

                        BindKmHistory(activityId);

                        string script = string.Format("setTimeout(function(){{ $('#{0}').modal('show'); $('#spMinKm').text('{1}'); $('#spMaxKm').text('{2}'); }}, 150);",
                            "editKmModal", minDisplay.Replace("'", "\\'"), maxDisplay.Replace("'", "\\'"));
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "openEditKmModal_" + activityId, script, true);
                        return;
                    }
                }
            }

            RegisterClientNotification("Activity not found.", "warning");
        }


        private void BindKmHistory(int activityId)
        {
            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            DataTable dt = new DataTable();
            using (SqlConnection con = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand(@"
        SELECT HistoryId, ActivityId, OldKM, NewKM, ModifiedBy, ModifiedDateTime, Reason
        FROM dbo.SalesDailyActivityKMHistory
        WHERE ActivityId = @ActivityId
        ORDER BY HistoryId DESC", con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@ActivityId", activityId);
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }

            gvKmHistory.DataSource = dt;
            gvKmHistory.DataBind();
        }


        protected void btnSaveKm_Click_OLD17Nov25(object sender, EventArgs e)
        {
            decimal newKm;
            // validate format
            if (!decimal.TryParse(txtModalKM.Text.Trim(), out newKm))
            {
                // show modal error (client side label)
                ScriptManager.RegisterStartupScript(
                    this, this.GetType(), "kmInvalidNumber", "document.getElementById('spClientKmError').style.display='';" +
                    "document.getElementById('spClientKmError').innerText='Please enter a valid number.';", true);
                return;
            }

            string activityId = hdnActivityId.Value;
            if (string.IsNullOrEmpty(activityId))
            {
                // silently do nothing
                return;
            }

            // ----- OPTIONAL: server-side Min/Max validation -----
            // If you want stronger rule enforcement:
            // decimal min = decimal.Parse(hdnMin.Value);
            // decimal max = decimal.Parse(hdnMax.Value);
            // if(newKm < min || newKm > max) { ... }
            // -----------------------------------------------------

            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

                using (SqlConnection con = new SqlConnection(connStr))
                using (SqlCommand cmd = new SqlCommand("usp_UpdateActivityKM", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@ActivityId", activityId);
                    cmd.Parameters.AddWithValue("@NewKM", newKm);
                    cmd.Parameters.AddWithValue("@ModifiedBy", GetCurrentUserLoginId());   // your method

                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                // Refresh grid
                Session.Remove(SESSION_KEY_GV_ACTIVITIES);
                BindGrid(false);

                // Close modal
                ScriptManager.RegisterStartupScript(
                    this, this.GetType(),
                    "closeKmModal",
                    "$('#editKmModal').modal('hide');",
                    true);
            }
            catch (Exception ex)
            {
                // Show error & reopen modal with same values to avoid losing user data
                ScriptManager.RegisterStartupScript(
                    this, this.GetType(),
                    "kmSaveError",
                    "alert('Error saving KM: " + ex.Message.Replace("'", "") + "');" +
                    "$('#editKmModal').modal('show');",
                    true);
            }
        }

        protected void btnSaveKm_Click(object sender, EventArgs e)
        {
            decimal newKm;
            // validate numeric
            if (!decimal.TryParse(txtModalKM.Text.Trim(), out newKm))
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "kmInvalidNumber",
                    "document.getElementById('spClientKmError').style.display=''; document.getElementById('spClientKmError').innerText='Please enter a valid number.';", true);
                return;
            }

            decimal origKm = 0;
            if (!decimal.TryParse(hdnOriginalKm.Value, out origKm))
            {
                // missing original KM: re-open modal and error
                ScriptManager.RegisterStartupScript(this, this.GetType(), "kmOrigMissing",
                    "document.getElementById('spClientKmError').style.display=''; document.getElementById('spClientKmError').innerText='Original KM not found, cannot validate.'; $('#editKmModal').modal('show');", true);
                return;
            }

            if (newKm == origKm)
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "kmNotChanged",
                    "document.getElementById('spClientKmError').style.display=''; document.getElementById('spClientKmError').innerText='KM value has not changed. Please modify before saving.'; $('#editKmModal').modal('show');", true);
                return;
            }

            string reason = txtKmReason.Text.Trim();
            if (string.IsNullOrEmpty(reason))
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "kmReasonReq",
                    "document.getElementById('spClientKmError').style.display=''; document.getElementById('spClientKmError').innerText='Please provide a reason for changing the KM.'; $('#editKmModal').modal('show');", true);
                return;
            }

            int activityId;
            if (!int.TryParse(hdnActivityId.Value, out activityId))
            {
                // invalid activity id
                return;
            }

            string modifiedBy = GetCurrentUserLoginId();
            //string reason = txtKmReason.Text.Trim();

            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

                using (SqlConnection con = new SqlConnection(connStr))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("usp_UpdateActivityKM", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ActivityId", activityId);
                        cmd.Parameters.AddWithValue("@NewKM", newKm);
                        cmd.Parameters.AddWithValue("@ModifiedBy", modifiedBy);
                        cmd.Parameters.AddWithValue("@Reason", txtKmReason.Text.Trim()); // pass reason or DBNull.Value
                        cmd.ExecuteNonQuery();
                    }

                    // 2) OPTIONAL: If you want the Reason saved AND your SP can't be changed,
                    //    insert the Reason into history by updating the most recent history row for this ActivityId.
                    //    We will update the latest History row's Reason where ModifiedBy = modifiedBy and NewKM = newKm.
                    //if (!string.IsNullOrEmpty(reason))
                    //{
                    //    using (SqlCommand cmdUpdateReason = new SqlCommand(@"
                    //        UPDATE TOP (1) dbo.SalesDailyActivityKMHistory
                    //        SET Reason = @Reason
                    //        WHERE ActivityId = @ActivityId
                    //          AND NewKM = @NewKM
                    //        ORDER BY HistoryId DESC;
                    //    ", con))
                    //    {
                    //        cmdUpdateReason.Parameters.AddWithValue("@Reason", reason);
                    //        cmdUpdateReason.Parameters.AddWithValue("@ActivityId", activityId);
                    //        cmdUpdateReason.Parameters.AddWithValue("@NewKM", newKm);
                    //        cmdUpdateReason.ExecuteNonQuery();
                    //    }
                    //}
                }

                // Refresh grid (main grid) - so updated KM is visible in list
                // Refresh grid
                Session.Remove(SESSION_KEY_GV_ACTIVITIES);
                BindGrid(false);

                // Re-bind history so user can see the change just created
                BindKmHistory(activityId);

                // Clear reason text so it doesn't persist
                txtKmReason.Text = string.Empty;

                // Re-open modal (optional) OR close it. Here I close modal and rely on user to re-open to view history,
                // but since we have freshly bound history, we can show it if needed. I'll re-open and keep it open to show the history update.
                string modalScript = $"setTimeout(function(){{ $('#editKmModal').modal('show'); $('#spMinKm').text('—'); $('#spMaxKm').text('—'); }}, 50);";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "reopenEditKmModal_" + activityId, modalScript, true);
            }
            catch (SqlException sqlEx)
            {
                // surface DB error
                string msg = sqlEx.Message.Replace("'", "");
                ScriptManager.RegisterStartupScript(this, this.GetType(), "kmSaveSqlError", $"alert('Database error: {msg}'); $('#editKmModal').modal('show');", true);
            }
            catch (Exception ex)
            {
                string msg = ex.Message.Replace("'", "");
                ScriptManager.RegisterStartupScript(this, this.GetType(), "kmSaveError", $"alert('Error saving KM: {msg}'); $('#editKmModal').modal('show');", true);
            }
        }


        protected void SetPolicyModeAndTrigger_Client(string modeFromDb)
        {
            // ensure server hidden field also set (so server-side persists)
            HF_ResolvedPolicyMode.Value = modeFromDb ?? string.Empty;

            // safe JS-string encode to avoid breaking the script (handles quotes, newlines)
            string safeMode = HttpUtility.JavaScriptStringEncode(modeFromDb ?? string.Empty);

            string script = $@"if(typeof applyPolicyModeFromServer === 'function') applyPolicyModeFromServer('{safeMode}'); else (function(){{ try {{ applyPolicyModeFromServer && applyPolicyModeFromServer('{safeMode}'); }} catch(e){{console && console.log && console.log(e);}} }})();";

            // Register startup script so it runs after partial postback DOM update
            ScriptManager.RegisterStartupScript(this.Page, this.Page.GetType(), "applyPolicyModeFromServer", script, true);
        }

        // call this after you set HF_ResolvedPolicyMode server-side (or do that here)
        protected void ApplyPolicyModeFromCodeBehind(string modeFromDb)
        {
            // set server-side hidden field value so postbacks retain it
            HF_ResolvedPolicyMode.Value = modeFromDb ?? string.Empty; // HF_ResolvedPolicyMode is your asp:HiddenField

            // build client script that will:
            // 1) set the HF element value (in case UpdatePanel replaced DOM)
            // 2) set SEL_PolicyMode select value if present
            // 3) set localStorage (optional)
            // 4) dispatch policy.updated event so client code picks it up
            string script = $@"
                (function() {{
                    try {{
                        // write HF (by client id)
                        var hf = document.getElementById('{HF_ResolvedPolicyMode.ClientID}');
                        if (hf) hf.value = '{HttpUtility.JavaScriptStringEncode(modeFromDb ?? "")}';

                        // try to set visible selector if it exists (selector id may be static)
                        var sel = document.getElementById('SEL_PolicyMode') 
                                  || document.querySelector('[id$=""_SEL_PolicyMode""]') 
                                  || document.querySelector('[id$=""SEL_PolicyMode""]');
                        if (sel) {{
                            try {{ sel.value = '{HttpUtility.JavaScriptStringEncode(modeFromDb ?? "")}'; }} catch(e){{console && console.log && console.log('sel.value set failed', e);}}
                        }}

                        // optional: persist to localStorage so reload keeps selection
                        try {{ localStorage.setItem('so_policy_mode', '{HttpUtility.JavaScriptStringEncode(modeFromDb ?? "")}'); }} catch(ex){{}}

                        // notify other client handlers
                        var ev = new CustomEvent('policy.updated', {{ detail: {{ mode: '{HttpUtility.JavaScriptStringEncode(modeFromDb ?? "")}', source: 'server' }} }} );
                        document.dispatchEvent(ev);

                        // If applyPolicyMode exists, call it directly as well
                        if (typeof applyPolicyMode === 'function') {{
                            try {{ applyPolicyMode(); }} catch(e){{}}
                        }}
                    }} catch(err) {{
                        console && console.log && console.log('ApplyPolicyModeFromCodeBehind error', err);
                    }}
                }})();";

            // register script so it runs after update/postback. Use ScriptManager for UpdatePanel scenarios.
            ScriptManager.RegisterStartupScript(this.Page, this.Page.GetType(), "applyPolicyModeFromServer", script, true);
        }

        protected void ApplyPolicyMode_ClientCall(string modeFromDb)
        {
            // defensive: normalize
            string mode = modeFromDb ?? string.Empty;

            // ensure HF exists on server (optional but recommended)
            try
            {
                if (HF_ResolvedPolicyMode != null)
                {
                    HF_ResolvedPolicyMode.Value = mode;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[ApplyPolicyMode_ClientCall] WARNING: HF_ResolvedPolicyMode server control is NULL.");
                }
            }
            catch (Exception ex)
            {
                // don't abort script registration just because HF set failed
                System.Diagnostics.Debug.WriteLine("[ApplyPolicyMode_ClientCall] Error setting HF_ResolvedPolicyMode: " + ex.Message);
            }

            // JS-safe encoding of the mode string
            string safeMode = HttpUtility.JavaScriptStringEncode(mode);

            // client script that logs clearly to browser console
            string script = $@"
            (function() {{
                try {{
                    console.log('%c[Server → Client] applyPolicyModeFromServer about to run','color:#ff9800;font-weight:bold;');
                    if (typeof applyPolicyModeFromServer === 'function') {{
                        try {{
                            applyPolicyModeFromServer('{safeMode}');
                            console.log('%c[Server → Client] applyPolicyModeFromServer executed OK','color:green;font-weight:bold;');
                        }} catch(e) {{
                            console.error('%c[Server → Client] applyPolicyModeFromServer threw:','color:red;font-weight:bold;', e);
                        }}
                    }} else {{
                        console.warn('%c[Server → Client] applyPolicyModeFromServer not defined yet. Queuing for DOMContentLoaded.','color:#f57c00;font-weight:bold;');
                        // queue for later: try after DOM ready / modal shown
                        window.__so_pending_policy_mode = '{safeMode}';
                        document.addEventListener('DOMContentLoaded', function() {{
                            try {{
                                if (typeof applyPolicyModeFromServer === 'function' && window.__so_pending_policy_mode) {{
                                    applyPolicyModeFromServer(window.__so_pending_policy_mode);
                                    console.log('%c[Server → Client] applyPolicyModeFromServer executed from DOMContentLoaded','color:green;font-weight:bold;');
                                    window.__so_pending_policy_mode = null;
                                }}
                            }} catch(e) {{ console.error('queued applyPolicyModeFromServer failed', e); }}
                        }});
                    }}
                }} catch(err) {{
                    console.error('%c[Server → Client] unexpected error preparing applyPolicyModeFromServer call','color:red;font-weight:bold;', err);
                }}
            }})();";

            try
            {
                // prefer ScriptManager if present (works for UpdatePanel partial postbacks)
                ScriptManager sm = ScriptManager.GetCurrent(this.Page);
                if (sm != null)
                {
                    ScriptManager.RegisterStartupScript(this.Page, this.Page.GetType(), "ApplyPolicyMode_ClientCall", script, true);
                    System.Diagnostics.Debug.WriteLine("[ApplyPolicyMode_ClientCall] Script registered via ScriptManager.");
                }
                else
                {
                    // fallback for pages without ScriptManager
                    ClientScript.RegisterStartupScript(this.GetType(), "ApplyPolicyMode_ClientCall_Fallback", script, true);
                    System.Diagnostics.Debug.WriteLine("[ApplyPolicyMode_ClientCall] Script registered via ClientScript fallback.");
                }
            }
            catch (Exception ex)
            {
                // last-resort: log error server-side so you can inspect it
                System.Diagnostics.Debug.WriteLine("[ApplyPolicyMode_ClientCall] Error registering script: " + ex.ToString());
                // optionally: throw; // if you want to fail loud
            }
        }

        protected void ClickApplyButtonFromServer(string modeFromDb)
        {
            HF_ResolvedPolicyMode.Value = modeFromDb ?? string.Empty;

            string script = $@"
            (function() {{
                try {{
                    // set HF
                    var hf = document.getElementById('{HF_ResolvedPolicyMode.ClientID}');
                    if (hf) hf.value = '{HttpUtility.JavaScriptStringEncode(modeFromDb ?? "")}';

                    // set selector if present
                    var sel = document.getElementById('SEL_PolicyMode') 
                              || document.querySelector('[id$=""_SEL_PolicyMode""]') 
                              || document.querySelector('[id$=""SEL_PolicyMode""]');
                    if (sel) {{ try {{ sel.value = '{HttpUtility.JavaScriptStringEncode(modeFromDb ?? "")}'; }} catch(e){{}} }}

                    // try click the Apply button
                    var btn = document.getElementById('BTN_ApplyPolicyMode') 
                              || document.querySelector('[id$=""_BTN_ApplyPolicyMode""]') 
                              || document.querySelector('[id$=""BTN_ApplyPolicyMode""]');
                    if (btn && typeof btn.click === 'function') {{
                        try {{ btn.click(); return; }} catch(e){{ console && console.log && console.log('btn.click failed', e); }}
                    }}

                    // fallback: dispatch event
                    var ev = new CustomEvent('policy.updated', {{ detail: {{ mode: '{HttpUtility.JavaScriptStringEncode(modeFromDb ?? "")}', source: 'server' }} }} );
                    document.dispatchEvent(ev);

                    if (typeof applyPolicyMode === 'function') try {{ applyPolicyMode(); }} catch(e){{}}
                }} catch(err) {{ console && console.log && console.log('ClickApplyButtonFromServer error', err); }}
            }})();";

            ScriptManager.RegisterStartupScript(this.Page, this.Page.GetType(), "clickApplyBtnFromServer", script, true);
        }


        protected void gvActivities_RowDataBound_OLD(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            // Cast to DataRowView
            var drv = e.Row.DataItem as System.Data.DataRowView;
            if (drv == null) return;

            // Safely parse values
            decimal totalKm = drv["TotalDistanceKM"] != DBNull.Value ? Convert.ToDecimal(drv["TotalDistanceKM"]) : 0m;
            decimal? minTol = drv["MinAllowedKM"] != DBNull.Value ? (decimal?)Convert.ToDecimal(drv["MinAllowedKM"]) : null;
            decimal? maxTol = drv["MaxAllowedKM"] != DBNull.Value ? (decimal?)Convert.ToDecimal(drv["MaxAllowedKM"]) : null;

            // Compute allowed bounds relative to current TotalDistanceKM
            decimal? allowedLower = null;
            decimal? allowedUpper = null;

            if (minTol.HasValue)
            {
                allowedLower = totalKm - minTol.Value;
                if (allowedLower < 0) allowedLower = 0; // clamp at 0
            }

            if (maxTol.HasValue)
            {
                allowedUpper = totalKm + maxTol.Value;
            }

            // Find labels in ItemTemplate
            Label lblTolLower = e.Row.FindControl("lblTolLower") as Label;
            Label lblTolUpper = e.Row.FindControl("lblTolUpper") as Label;

            // Find labels in EditItemTemplate
            Label lblTolLower_Edit = e.Row.FindControl("lblTolLower_Edit") as Label;
            Label lblTolUpper_Edit = e.Row.FindControl("lblTolUpper_Edit") as Label;

            // Helper: format decimals or show "—"
            Func<decimal?, string> F = delegate (decimal? d)
            {
                return d.HasValue ? d.Value.ToString("0.00") : "—";
            };

            if (lblTolLower != null) lblTolLower.Text = F(allowedLower);
            if (lblTolUpper != null) lblTolUpper.Text = F(allowedUpper);

            if (lblTolLower_Edit != null) lblTolLower_Edit.Text = F(allowedLower);
            if (lblTolUpper_Edit != null) lblTolUpper_Edit.Text = F(allowedUpper);
        }
        protected void gvActivities_RowDataBound_OLD_05Nov25(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var drv = e.Row.DataItem as System.Data.DataRowView;
            if (drv == null) return;

            decimal totalKm = drv["TotalDistanceKM"] != DBNull.Value ? Convert.ToDecimal(drv["TotalDistanceKM"]) : 0m;
            //decimal? minTol = drv.Table.Columns.Contains("MinAllowedKM") && drv["MinAllowedKM"] != DBNull.Value ?
            //                  (decimal?)Convert.ToDecimal(drv["MinAllowedKM"]) : null;
            //decimal? maxTol = drv.Table.Columns.Contains("MaxAllowedKM") && drv["MaxAllowedKM"] != DBNull.Value ?
            //                  (decimal?)Convert.ToDecimal(drv["MaxAllowedKM"]) : null;

            decimal? minTol = null;
            decimal? maxTol = null;

            if (drv != null && drv.Row != null && drv.Row.Table != null)
            {
                if (drv.Row.Table.Columns.Contains("MinAllowedKM") && drv["MinAllowedKM"] != DBNull.Value)
                    minTol = Convert.ToDecimal(drv["MinAllowedKM"]);

                if (drv.Row.Table.Columns.Contains("MaxAllowedKM") && drv["MaxAllowedKM"] != DBNull.Value)
                    maxTol = Convert.ToDecimal(drv["MaxAllowedKM"]);
            }

            // Interpretation note:
            // - If MinAllowedKM / MaxAllowedKM are **absolute bounds** then display them directly.
            // - If they are **tolerances** (i.e., +/- tolerance around current TotalDistanceKM),
            //   then allowedLower = TotalDistanceKM - MinAllowedKM ; allowedUpper = TotalDistanceKM + MaxAllowedKM.
            //
            // I implement a safe heuristic: if MinAllowedKM/MaxAllowedKM look small (<= totalKm), we assume tolerance.
            // If they look like absolute minimums (e.g., > totalKm), we display as absolute.
            decimal? allowedLower = null, allowedUpper = null;
            if (minTol.HasValue && maxTol.HasValue)
            {
                // heuristic: if minTol <= totalKm -> treat as tolerance; else treat as absolute bound
                if (minTol.Value <= totalKm)
                    allowedLower = Math.Max(0, totalKm - minTol.Value); // tolerance interpretation
                else
                    allowedLower = minTol.Value; // absolute interpretation

                if (maxTol.Value <= (totalKm * 5)) // arbitrary heuristic to avoid crazy upper bound
                    allowedUpper = totalKm + maxTol.Value; // tolerance interpretation
                else
                    allowedUpper = maxTol.Value; // absolute interpretation
            }
            else
            {
                // fallback when only one is present
                if (minTol.HasValue)
                {
                    allowedLower = Math.Max(0, totalKm - minTol.Value);
                }
                if (maxTol.HasValue)
                {
                    allowedUpper = totalKm + maxTol.Value;
                }
            }

            Label lblTolLower = e.Row.FindControl("lblTolLower") as Label;
            Label lblTolUpper = e.Row.FindControl("lblTolUpper") as Label;
            Label lblTolLower_Edit = e.Row.FindControl("lblTolLower_Edit") as Label;
            Label lblTolUpper_Edit = e.Row.FindControl("lblTolUpper_Edit") as Label;

            Func<decimal?, string> F = d => d.HasValue ? d.Value.ToString("0.00") : "—";

            if (lblTolLower != null) lblTolLower.Text = F(allowedLower);
            if (lblTolUpper != null) lblTolUpper.Text = F(allowedUpper);
            if (lblTolLower_Edit != null) lblTolLower_Edit.Text = F(allowedLower);
            if (lblTolUpper_Edit != null) lblTolUpper_Edit.Text = F(allowedUpper);
        }

        protected void gvActivities_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var drv = e.Row.DataItem as System.Data.DataRowView;
            if (drv == null) return;

            // 1) existing tolerance logic (unchanged)
            PopulateKmTolerance(drv, e.Row);

            // 2) new editability / approval enforcement
            ApplyApprovalEditability(drv, e.Row);
        }

        private void PopulateKmTolerance(System.Data.DataRowView drv, GridViewRow row)
        {
            decimal totalKm = drv["TotalDistanceKM"] != DBNull.Value ? Convert.ToDecimal(drv["TotalDistanceKM"]) : 0m;

            decimal? minTol = null;
            decimal? maxTol = null;

            if (drv.Row.Table.Columns.Contains("MinAllowedKM") && drv["MinAllowedKM"] != DBNull.Value)
                minTol = Convert.ToDecimal(drv["MinAllowedKM"]);

            if (drv.Row.Table.Columns.Contains("MaxAllowedKM") && drv["MaxAllowedKM"] != DBNull.Value)
                maxTol = Convert.ToDecimal(drv["MaxAllowedKM"]);

            decimal? allowedLower = null, allowedUpper = null;
            if (minTol.HasValue && maxTol.HasValue)
            {
                if (minTol.Value <= totalKm)
                    allowedLower = Math.Max(0, totalKm - minTol.Value);
                else
                    allowedLower = minTol.Value;

                if (maxTol.Value <= (totalKm * 5))
                    allowedUpper = totalKm + maxTol.Value;
                else
                    allowedUpper = maxTol.Value;
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
            // determine row-level editability (IsEditable set by BindGrid)
            bool isEditable = true;
            if (drv.Row.Table.Columns.Contains("IsEditable") && drv["IsEditable"] != DBNull.Value)
            {
                bool parsed;
                if (bool.TryParse(drv["IsEditable"].ToString(), out parsed))
                    isEditable = parsed;
            }

            // controls from your ASPX
            LinkButton lnkEdit = row.FindControl("lnkEdit") as LinkButton;
            LinkButton lnkExpenses = row.FindControl("lnkExpenses") as LinkButton;
            TextBox txtKM = row.FindControl("txtKM") as TextBox;
            Label lblKM = row.FindControl("lblKM") as Label;
            LinkButton lnkUpdate = row.FindControl("lnkUpdate") as LinkButton;
            LinkButton lnkCancel = row.FindControl("lnkCancel") as LinkButton;

            // claim status
            string claimStatus = drv.Row.Table.Columns.Contains("ClaimStatus") && drv["ClaimStatus"] != DBNull.Value
                                 ? drv["ClaimStatus"].ToString().Trim()
                                 : string.Empty;

            // Determine effective visual status
            string effectiveVisualStatus;
            if (string.Equals(claimStatus, "Draft", StringComparison.OrdinalIgnoreCase))
            {
                effectiveVisualStatus = "Draft";
            }
            else if (isEditable)
            {
                effectiveVisualStatus = "Rejected"; // visually show editable rows as rejected
            }
            else
            {
                effectiveVisualStatus = !string.IsNullOrEmpty(claimStatus) ? claimStatus : "Unknown";
            }

            // helper to produce icon HTML (renamed parameter to avoid conflict)
            Func<string, string, string> IconHtml = delegate (string iconClass, string clrClass)
            {
                return string.Format("<i class='{0} {1}' aria-hidden='true'></i>", iconClass, clrClass);
            };

            // map status -> icon + color
            string editIconHtml;
            string expenseIconHtml;
            string colorClass;

            switch (effectiveVisualStatus.ToLowerInvariant())
            {
                case "rejected":
                    colorClass = "text-danger";
                    editIconHtml = IconHtml("fa fa-pencil", colorClass);
                    expenseIconHtml = IconHtml("fa fa-exclamation-triangle", colorClass);
                    break;
                case "draft":
                    colorClass = "text-primary";
                    editIconHtml = IconHtml("fa fa-pencil", colorClass);
                    expenseIconHtml = IconHtml("fa fa-briefcase", colorClass);
                    break;
                case "submitted":
                    colorClass = "text-warning";
                    editIconHtml = IconHtml("fa fa-paper-plane", colorClass);
                    expenseIconHtml = IconHtml("fa fa-briefcase", colorClass);
                    break;
                case "approved":
                    colorClass = "text-success";
                    editIconHtml = IconHtml("fa fa-check-circle", colorClass);
                    expenseIconHtml = IconHtml("fa fa-briefcase", colorClass);
                    break;
                default:
                    colorClass = "text-muted";
                    editIconHtml = IconHtml("fa fa-clock-o", colorClass);
                    expenseIconHtml = IconHtml("fa fa-briefcase", colorClass);
                    break;
            }

            // neutral button background
            const string neutralBtnClass = "btn btn-sm btn-light";

            // ---------- Edit button ----------
            if (lnkEdit != null)
            {
                bool showEdit = isEditable || string.Equals(claimStatus, "Draft", StringComparison.OrdinalIgnoreCase);

                lnkEdit.Visible = showEdit;
                lnkEdit.Enabled = showEdit;
                lnkEdit.CssClass = neutralBtnClass;
                lnkEdit.Text = editIconHtml;
                lnkEdit.ToolTip = showEdit ? "Edit" : "Edit (disabled)";
            }

            // ---------- Expenses button ----------
            if (lnkExpenses != null)
            {
                lnkExpenses.Visible = true;
                lnkExpenses.Enabled = true;
                lnkExpenses.CssClass = neutralBtnClass;
                lnkExpenses.Text = expenseIconHtml;
                lnkExpenses.ToolTip = "View / Add Expenses";
            }

            // ---------- Edit-mode controls ----------
            if (txtKM != null)
            {
                txtKM.ReadOnly = !isEditable;
                txtKM.CssClass = txtKM.CssClass + (isEditable ? " editable" : " readonly");
            }
            if (lnkUpdate != null) lnkUpdate.Enabled = isEditable;
            if (lnkCancel != null) lnkCancel.Enabled = true;

            // ---------- Display label ----------
            if (lblKM != null)
            {
                lblKM.Text = drv["TotalDistanceKM"] != DBNull.Value
                    ? Convert.ToDecimal(drv["TotalDistanceKM"]).ToString("N2") + " km"
                    : "—";
                lblKM.Visible = true;
            }

            // ---------- Visual cues ----------
            if (string.Equals(effectiveVisualStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
            {
                row.CssClass = (row.CssClass + " row-rejected").Trim();
                row.ToolTip = (row.ToolTip ?? "") + " Editable (reopened by approver).";
            }

            bool monthLocked = ViewState["MonthLocked"] != null && (bool)ViewState["MonthLocked"];
            if (monthLocked && !isEditable)
            {
                row.CssClass = (row.CssClass + " row-locked-nofade").Trim();
                row.ToolTip = (row.ToolTip ?? "") + " Row locked due to month submitted for approval.";
            }
        }


        private int FindRowIndexByActivityId(int activityId)
        {
            for (int i = 0; i < gvActivities.Rows.Count; i++)
            {
                if (gvActivities.DataKeys[i] != null && Convert.ToInt32(gvActivities.DataKeys[i].Value) == activityId)
                    return i;
            }
            return -1;
        }

        protected void gvActivities_RowEditing(object sender, GridViewEditEventArgs e)
        {
            gvActivities.EditIndex = e.NewEditIndex;
            Session.Remove(SESSION_KEY_GV_ACTIVITIES);
            BindGrid(false);
        }

        protected void gvActivities_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            gvActivities.EditIndex = -1;
            Session.Remove(SESSION_KEY_GV_ACTIVITIES);
            BindGrid(false);
        }

        protected void gvActivities_RowUpdating_OLD(object sender, GridViewUpdateEventArgs e)
        {
            int activityId = Convert.ToInt32(gvActivities.DataKeys[e.RowIndex].Value);
            GridViewRow row = gvActivities.Rows[e.RowIndex];
            TextBox txtKM = row.FindControl("txtKM") as TextBox;
            if (txtKM == null) { lbl_msg.Text = "KM textbox not found."; return; }
            decimal newKm;
            if (!decimal.TryParse(txtKM.Text.Trim(), out newKm)) { lbl_msg.Text = "Enter valid KM value."; return; }

            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                using (SqlCommand cmd = new SqlCommand("usp_UpdateActivityKM", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ActivityId", activityId);
                    cmd.Parameters.AddWithValue("@NewKM", newKm);       // exact param name from your SP
                    cmd.Parameters.AddWithValue("@ModifiedBy", GetCurrentUserLoginId());
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
                gvActivities.EditIndex = -1;
                BindGrid(false);
            }
            catch (SqlException sqx)
            {
                // surface DB-raised message (RAISERROR / THROW)
                lbl_msg.ForeColor = System.Drawing.Color.Red;
                lbl_msg.Text = sqx.Message;
                // keep row in edit mode
            }
        }

        protected void gvActivities_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            int rowIndex = e.RowIndex;

            if (gvActivities.DataKeys == null || gvActivities.DataKeys.Count <= rowIndex)
            {
                lbl_msg.Text = "Unable to locate DataKey for updated row.";
                lbl_msg.ForeColor = System.Drawing.Color.Red;
                return;
            }

            int activityId = Convert.ToInt32(gvActivities.DataKeys[rowIndex].Value);
            GridViewRow row = gvActivities.Rows[rowIndex];
            TextBox txtKM = row.FindControl("txtKM") as TextBox;
            if (txtKM == null) { lbl_msg.Text = "KM textbox not found."; lbl_msg.ForeColor = System.Drawing.Color.Red; return; }

            decimal newKm;
            CultureInfo ci = CultureInfo.GetCultureInfo("en-IN");
            if (!decimal.TryParse(txtKM.Text.Trim(), NumberStyles.AllowDecimalPoint, ci, out newKm)) { lbl_msg.Text = "Enter valid KM value."; lbl_msg.ForeColor = System.Drawing.Color.Red; return; }

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

                // Update the Session DataTable (canonical copy) so BindGrid will rebind with latest values
                var dt = Session[SESSION_KEY_GV_ACTIVITIES] as DataTable;
                if (dt != null)
                {
                    DataRow found = null;
                    if (dt.PrimaryKey != null && dt.PrimaryKey.Length > 0)
                    {
                        found = dt.Rows.Find(activityId);
                    }
                    else
                    {
                        DataRow[] sel = dt.Select($"ActivityId = {activityId}");
                        if (sel != null && sel.Length > 0) found = sel[0];
                    }

                    if (found != null)
                    {
                        if (dt.Columns.Contains("TotalDistanceKM"))
                            found["TotalDistanceKM"] = newKm;

                        // Recompute derived claim amount using RatePerKm if available
                        decimal rate = 2.75m;
                        decimal parsedRate = 0m;
                        if (dt.Columns.Contains("RatePerKm") && found["RatePerKm"] != DBNull.Value && decimal.TryParse(found["RatePerKm"].ToString(), out parsedRate))
                            rate = parsedRate;

                        decimal calc = Math.Round(rate * newKm, 2, MidpointRounding.AwayFromZero);
                        if (dt.Columns.Contains("CalculatedClaimAmount"))
                            found["CalculatedClaimAmount"] = calc;
                    }
                }

                // End edit and rebind from session (do not force DB reload)
                gvActivities.EditIndex = -1;
                Session.Remove(SESSION_KEY_GV_ACTIVITIES);
                BindGrid(false, false); // no notification, bind from session copy

                lbl_msg.Text = $"Activity {activityId} updated (KM: {newKm:N2}).";
                lbl_msg.ForeColor = System.Drawing.Color.Green;
            }
            catch (SqlException sqx)
            {
                lbl_msg.ForeColor = System.Drawing.Color.Red;
                lbl_msg.Text = sqx.Message;
                // optionally keep row in edit mode
            }
            catch (Exception ex)
            {
                lbl_msg.ForeColor = System.Drawing.Color.Red;
                lbl_msg.Text = ex.Message;
            }
        }


        protected void BTN_SaveExpense_Click_OLD(object sender, EventArgs e)
        {
            LBL_ExpenseMsg.Text = "";
            int activityId;
            if (!int.TryParse(HF_ActivityId.Value, out activityId))
            {
                LBL_ExpenseMsg.Text = "Invalid activity.";
                ClientScript.RegisterStartupScript(this.GetType(), "showExpense", "showExpenseModal();", true);
                return;
            }

            decimal daAmt = 0m;
            decimal.TryParse(TXT_DAAmount.Text.Trim(), out daAmt);
            decimal hotelAmt = 0m;
            decimal.TryParse(TXT_HotelAmount.Text.Trim(), out hotelAmt);

            string savedRelativePath = null; // will store in DB.AttachmentFileName
            string savedFileName = null;

            // Handle file save to filesystem
            try
            {
                //if (FU_Attachment.HasFile)
                //{
                //    if (FU_Attachment.PostedFile.ContentLength > 5 * 1024 * 1024)
                //    {
                //        LBL_ExpenseMsg.Text = "Attachment too large (max 5MB).";
                //        ClientScript.RegisterStartupScript(this.GetType(), "showExpense", "showExpenseModal();", true);
                //        return;
                //    }

                //    string ext = Path.GetExtension(FU_Attachment.FileName).ToLower();
                //    var allowed = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                //    if (Array.IndexOf(allowed, ext) < 0)
                //    {
                //        LBL_ExpenseMsg.Text = "Invalid file type. Allowed: JPG, PNG, PDF.";
                //        ClientScript.RegisterStartupScript(this.GetType(), "showExpense", "showExpenseModal();", true);
                //        return;
                //    }

                //    // Build folder: UploadedFiles/ClaimsAttachment/{yyyy}/{MM}/
                //    string baseFolder = Server.MapPath("~/UploadedFiles/ClaimsAttachment/");
                //    string year = DateTime.UtcNow.AddMinutes(330).ToString("yyyy");
                //    string month = DateTime.UtcNow.AddMinutes(330).ToString("MM");
                //    string uploadsFolder = Path.Combine(baseFolder, year, month);
                //    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                //    string safeLogin = GetCurrentUserLoginId().Replace(" ", "_");
                //    string uniquePart = DateTime.UtcNow.AddMinutes(330).ToString("yyyyMMddHHmmss") + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                //    savedFileName = $"{safeLogin}_{activityId}_{uniquePart}{ext}";
                //    string savedFullPath = Path.Combine(uploadsFolder, savedFileName);
                //    FU_Attachment.SaveAs(savedFullPath);

                //    // Save relative path (recommended) so downloads can map correctly
                //    savedRelativePath = Path.Combine("UploadedFiles/ClaimsAttachment", year, month, savedFileName).Replace("\\", "/");
                //}

                // --- BEFORE using this block ensure you have activityId available in this scope ---
                // int activityId = ...; // obtain from page context or method parameter

                if (FU_Attachment.HasFile)
                {
                    // Max 5 MB
                    if (FU_Attachment.PostedFile.ContentLength > 5 * 1024 * 1024)
                    {
                        LBL_ExpenseMsg.Text = "Attachment too large (max 5MB).";
                        ClientScript.RegisterStartupScript(this.GetType(), "showExpense", "showExpenseModal();", true);
                        return;
                    }

                    // sanitize & validate extension
                    string ext = Path.GetExtension(Path.GetFileName(FU_Attachment.FileName)).ToLowerInvariant();
                    var allowed = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                    if (Array.IndexOf(allowed, ext) < 0)
                    {
                        LBL_ExpenseMsg.Text = "Invalid file type. Allowed: JPG, PNG, PDF.";
                        ClientScript.RegisterStartupScript(this.GetType(), "showExpense", "showExpenseModal();", true);
                        return;
                    }

                    // Determine IST now — prefer TimeZoneInfo but fall back to UTC+5:30
                    DateTime istNow;
                    try
                    {
                        var tz = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); // Windows ID
                        istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
                    }
                    catch
                    {
                        // fallback (UTC + 5:30)
                        istNow = DateTime.UtcNow.AddMinutes(330);
                    }

                    // Build folder: UploadedFiles/ClaimsAttachment/{yyyy}/{MM}/
                    string baseFolder = Server.MapPath("~/UploadedFiles/ClaimsAttachment/");
                    string year = istNow.ToString("yyyy");
                    string month = istNow.ToString("MM");
                    string uploadsFolder = Path.Combine(baseFolder, year, month);
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    // sanitize login id and activityId presence
                    string safeLogin = (GetCurrentUserLoginId() ?? "unknown").Replace(" ", "_");
                    // ensure activityId is available as string
                    string activityPart = activityId > 0 ? activityId.ToString() : "0";

                    string uniquePart = istNow.ToString("yyyyMMddHHmmss") + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    savedFileName = $"{safeLogin}_{activityPart}_{uniquePart}{ext}";

                    string savedFullPath = Path.Combine(uploadsFolder, savedFileName);

                    // Save to disk
                    FU_Attachment.SaveAs(savedFullPath);

                    // Save relative path (use forward slashes) — DO NOT store server absolute path in DB
                    // e.g.: UploadedFiles/ClaimsAttachment/2025/09/username_1234_202509... .pdf
                    savedRelativePath = Path.Combine("UploadedFiles", "ClaimsAttachment", year, month, savedFileName)
                                                       .Replace("\\", "/"); // normalize to forward slashes

                    // Persist 'savedRelativePath' to your DB column (AttachmentFileName or AttachmentPath)
                    // e.g. UPDATE dbo.SalesDailyExpense SET AttachmentFileName = @savedRelativePath WHERE ExpenseId = @ExpenseId
                    //
                    // IMPORTANT: store exactly savedRelativePath (no leading "~/" and no server absolute path)
                }


                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();

                    if (daAmt > 0)
                    {
                        using (SqlCommand cmd = new SqlCommand("usp_SaveDailyExpense", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@ActivityId", activityId);
                            cmd.Parameters.AddWithValue("@ExpenseType", "DA");
                            cmd.Parameters.AddWithValue("@ExpenseDate", HF_ActivityDate.Value != "" ? (object)DateTime.Parse(HF_ActivityDate.Value) : DBNull.Value);
                            cmd.Parameters.AddWithValue("@Amount", daAmt);
                            cmd.Parameters.AddWithValue("@Purpose", DBNull.Value);
                            cmd.Parameters.AddWithValue("@BillNo", DBNull.Value);
                            // store relative path in AttachmentFileName column
                            cmd.Parameters.AddWithValue("@AttachmentFileName", savedRelativePath ?? (object)DBNull.Value);
                            // We are using filesystem; set AttachmentContent = NULL
                            cmd.Parameters.AddWithValue("@AttachmentContent", DBNull.Value);
                            cmd.Parameters.AddWithValue("@CreatedBy", GetCurrentUserLoginId());
                            cmd.ExecuteNonQuery();
                        }
                    }

                    if (hotelAmt > 0)
                    {
                        using (SqlCommand cmd = new SqlCommand("usp_SaveHotelExpense", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            DateTime actDate = HF_ActivityDate.Value != "" ? DateTime.Parse(HF_ActivityDate.Value) : DateTime.UtcNow.AddMinutes(330);
                            cmd.Parameters.AddWithValue("@ActivityId", activityId);
                            cmd.Parameters.AddWithValue("@FromDate", actDate);
                            cmd.Parameters.AddWithValue("@ToDate", actDate);
                            cmd.Parameters.AddWithValue("@BillNo", DBNull.Value);
                            cmd.Parameters.AddWithValue("@Amount", hotelAmt);
                            cmd.Parameters.AddWithValue("@Purpose", DBNull.Value);
                            cmd.Parameters.AddWithValue("@AttachmentFileName", savedRelativePath ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@AttachmentContent", DBNull.Value);
                            cmd.Parameters.AddWithValue("@CreatedBy", GetCurrentUserLoginId());
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                LoadExpenseForActivity(activityId.ToString());
                ClientScript.RegisterStartupScript(this.GetType(), "showExpense", "showExpenseModal();", true);
                BindGrid(false);
                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Green;
                LBL_ExpenseMsg.Text = "Expense saved.";
            }
            catch (SqlException sqx)
            {
                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                LBL_ExpenseMsg.Text = sqx.Message;
                ClientScript.RegisterStartupScript(this.GetType(), "showExpense", "showExpenseModal();", true);
            }
            catch (Exception ex)
            {
                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                LBL_ExpenseMsg.Text = ex.Message;
                ClientScript.RegisterStartupScript(this.GetType(), "showExpense", "showExpenseModal();", true);

                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyError1", $"showPNotify('Error', '{ex.Message.Replace("'", "\\'")}', 'error');", true);
            }
        }

        protected void BTN_SaveExpense_Click_OLD2(object sender, EventArgs e)
        {
            LBL_ExpenseMsg.Text = "";
            LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;

            int activityId;
            if (!int.TryParse(HF_ActivityId.Value, out activityId))
            {
                LBL_ExpenseMsg.Text = "Invalid activity.";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                return;
            }

            // Parse amounts
            decimal daAmt = 0m;
            decimal.TryParse(TXT_DAAmount.Text.Trim(), out daAmt);

            decimal hotelAmt = 0m;
            decimal.TryParse(TXT_HotelAmount.Text.Trim(), out hotelAmt);

            // Parse hotel dates only if hotel amount provided or date fields filled
            DateTime? fromDate = null;
            DateTime? toDate = null;
            if (!string.IsNullOrWhiteSpace(TXT_HotelFromDate.Text))
            {
                DateTime tmp;
                if (DateTime.TryParse(TXT_HotelFromDate.Text.Trim(), out tmp))
                    fromDate = tmp.Date;
                else
                {
                    LBL_ExpenseMsg.Text = "Invalid Hotel From Date.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(TXT_HotelToDate.Text))
            {
                DateTime tmp;
                if (DateTime.TryParse(TXT_HotelToDate.Text.Trim(), out tmp))
                    toDate = tmp.Date;
                else
                {
                    LBL_ExpenseMsg.Text = "Invalid Hotel To Date.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
            }

            // If hotel amount specified, require FromDate and ToDate
            if (hotelAmt > 0m)
            {
                if (!fromDate.HasValue || !toDate.HasValue)
                {
                    LBL_ExpenseMsg.Text = "Please provide From Date and To Date for Hotel expense.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
                if (toDate.Value < fromDate.Value)
                {
                    LBL_ExpenseMsg.Text = "Hotel To Date cannot be earlier than From Date.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
            }

            // If nothing to save
            if (daAmt <= 0m && hotelAmt <= 0m && !FU_Attachment.HasFile)
            {
                LBL_ExpenseMsg.Text = "Nothing to save. Enter DA or Hotel amount or attach a file.";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                return;
            }

            // handle file save (shared for both DA and Hotel rows)
            string savedRelativePath = null;
            string savedFileName = null;

            try
            {
                if (FU_Attachment.HasFile)
                {
                    if (FU_Attachment.PostedFile.ContentLength > 5 * 1024 * 1024)
                    {
                        LBL_ExpenseMsg.Text = "Attachment too large (max 5MB).";
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                        return;
                    }

                    string ext = Path.GetExtension(FU_Attachment.FileName).ToLower();
                    var allowed = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                    if (Array.IndexOf(allowed, ext) < 0)
                    {
                        LBL_ExpenseMsg.Text = "Invalid file type. Allowed: JPG, PNG, PDF.";
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                        return;
                    }

                    string baseFolder = Server.MapPath("~/UploadedFiles/ClaimsAttachment/");
                    string year = DateTime.UtcNow.AddMinutes(330).ToString("yyyy");
                    string month = DateTime.UtcNow.AddMinutes(330).ToString("MM");
                    string uploadsFolder = Path.Combine(baseFolder, year, month);
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string safeLogin = (GetCurrentUserLoginId(false) ?? "anon").Replace(" ", "_");
                    string uniquePart = DateTime.UtcNow.AddMinutes(330).ToString("yyyyMMddHHmmss") + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    savedFileName = $"{safeLogin}_{activityId}_{uniquePart}{ext}";
                    string savedFullPath = Path.Combine(uploadsFolder, savedFileName);
                    FU_Attachment.SaveAs(savedFullPath);
                    savedRelativePath = Path.Combine("UploadedFiles/ClaimsAttachment", year, month, savedFileName).Replace("\\", "/");
                }

                // DB inserts
                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();

                    // DA - use usp_SaveDailyExpense if amount > 0
                    if (daAmt > 0m)
                    {
                        using (SqlCommand cmd = new SqlCommand("usp_SaveDailyExpense", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@ActivityId", activityId);
                            cmd.Parameters.AddWithValue("@ExpenseType", "DA");
                            DateTime? expDate = null;
                            if (!string.IsNullOrWhiteSpace(HF_ActivityDate.Value))
                            {
                                DateTime tmp;
                                if (DateTime.TryParse(HF_ActivityDate.Value, out tmp)) expDate = tmp.Date;
                            }
                            cmd.Parameters.AddWithValue("@ExpenseDate", expDate.HasValue ? (object)expDate.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@Amount", daAmt);
                            cmd.Parameters.AddWithValue("@Purpose", DBNull.Value);
                            cmd.Parameters.AddWithValue("@BillNo", DBNull.Value);
                            cmd.Parameters.AddWithValue("@AttachmentContent", DBNull.Value);
                            cmd.Parameters.AddWithValue("@AttachmentFileName", savedFileName);
                            cmd.Parameters.AddWithValue("@AttachmentPath", savedRelativePath ?? (object)DBNull.Value);
                            cmd.Parameters.Add("@AttachmentContent", SqlDbType.VarBinary, -1).Value = new byte[0];
                            cmd.Parameters.AddWithValue("@CreatedBy", GetCurrentUserLoginId());
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Hotel - use usp_SaveHotelExpense if amount > 0
                    if (hotelAmt > 0m)
                    {
                        using (SqlCommand cmd = new SqlCommand("usp_SaveHotelExpense", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@ActivityId", activityId);
                            cmd.Parameters.AddWithValue("@FromDate", fromDate.Value);
                            cmd.Parameters.AddWithValue("@ToDate", toDate.Value);
                            cmd.Parameters.AddWithValue("@BillNo", string.IsNullOrWhiteSpace(TXT_HotelBillNo.Text) ? (object)DBNull.Value : TXT_HotelBillNo.Text.Trim());
                            cmd.Parameters.AddWithValue("@Amount", hotelAmt);
                            cmd.Parameters.AddWithValue("@Purpose", string.IsNullOrWhiteSpace(TXT_HotelPurpose.Text) ? (object)DBNull.Value : TXT_HotelPurpose.Text.Trim());
                            //cmd.Parameters.AddWithValue("@AttachmentFileName", savedRelativePath ?? (object)DBNull.Value);
                            //cmd.Parameters.AddWithValue("@AttachmentContent", DBNull.Value);
                            // AttachmentFileName gets the relative path string (or NULL)
                            //cmd.Parameters.AddWithValue("@AttachmentFileName", savedRelativePath ?? (object)DBNull.Value);
                            // AttachmentContent must be VARBINARY; set to NULL explicitly
                            //cmd.Parameters.AddWithValue("@AttachmentContent", savedFileName);
                            //byte[] fileBytes = FU_Attachment.FileBytes;
                            //cmd.Parameters.Add("@AttachmentContent", SqlDbType.VarBinary, -1).Value = fileBytes;
                            //cmd.Parameters.Add("@AttachmentContent", SqlDbType.VarBinary, -1).Value = new byte[0];

                            cmd.Parameters.AddWithValue("@AttachmentContent", DBNull.Value);
                            cmd.Parameters.AddWithValue("@AttachmentFileName", savedFileName);
                            cmd.Parameters.AddWithValue("@AttachmentPath", savedRelativePath ?? (object)DBNull.Value);

                            cmd.Parameters.AddWithValue("@CreatedBy", GetCurrentUserLoginId());
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                // reload list and UI
                //LoadExpenseForActivity(activityId.ToString());

                DataTable dtAfter = LoadExpenseForActivity(activityId.ToString());
                BindGrid(false);

                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Green;
                LBL_ExpenseMsg.Text = "Expense saved.";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
            }
            catch (SqlException sqx)
            {
                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                LBL_ExpenseMsg.Text = sqx.Message;
                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
            }
            catch (Exception ex)
            {
                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                LBL_ExpenseMsg.Text = ex.Message;
                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
            }
        }
        protected void BTN_SaveExpense_Click_OLD3(object sender, EventArgs e)
        {
            LBL_ExpenseMsg.Text = "";
            LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;

            int activityId;
            if (!int.TryParse(HF_ActivityId.Value, out activityId))
            {
                LBL_ExpenseMsg.Text = "Invalid activity.";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                return;
            }

            // Expense type selector: must exist on UI (DDL_ExpenseType)
            string expenseType = "DA";
            if (DDL_ExpenseType != null && !string.IsNullOrWhiteSpace(DDL_ExpenseType.SelectedValue))
                expenseType = DDL_ExpenseType.SelectedValue.Trim();

            // Common validations & parse amounts
            decimal daAmt = 0m;
            decimal.TryParse(TXT_DAAmount.Text.Trim(), out daAmt);

            decimal hotelAmt = 0m;
            decimal.TryParse(TXT_HotelAmount.Text.Trim(), out hotelAmt);

            // Travel specific amount and date
            decimal travelAmt = 0m;
            DateTime travelDate = DateTime.MinValue;
            DateTime? travelDateNullable = null;
            if (!string.IsNullOrWhiteSpace(TXT_TravelAmount?.Text))
                decimal.TryParse(TXT_TravelAmount.Text.Trim(), out travelAmt);

            if (!string.IsNullOrWhiteSpace(TXT_TravelDate?.Text))
            {
                DateTime tmp;
                if (DateTime.TryParse(TXT_TravelDate.Text.Trim(), out tmp))
                {
                    travelDate = tmp.Date;
                    travelDateNullable = travelDate;
                }
                else
                {
                    // if user selected Travel type and entered invalid date -> error
                    if (expenseType.Equals("Travel", StringComparison.OrdinalIgnoreCase))
                    {
                        LBL_ExpenseMsg.Text = "Invalid Travel Date.";
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                        return;
                    }
                }
            }

            // Hotel From/To parsing (only used for hotel)
            DateTime? fromDate = null;
            DateTime? toDate = null;
            if (!string.IsNullOrWhiteSpace(TXT_HotelFromDate.Text))
            {
                DateTime tmp;
                if (DateTime.TryParse(TXT_HotelFromDate.Text.Trim(), out tmp))
                    fromDate = tmp.Date;
                else
                {
                    LBL_ExpenseMsg.Text = "Invalid Hotel From Date.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(TXT_HotelToDate.Text))
            {
                DateTime tmp;
                if (DateTime.TryParse(TXT_HotelToDate.Text.Trim(), out tmp))
                    toDate = tmp.Date;
                else
                {
                    LBL_ExpenseMsg.Text = "Invalid Hotel To Date.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
            }

            // If hotel amount specified, require FromDate and ToDate
            if (expenseType.Equals("Hotel", StringComparison.OrdinalIgnoreCase) && hotelAmt > 0m)
            {
                if (!fromDate.HasValue || !toDate.HasValue)
                {
                    LBL_ExpenseMsg.Text = "Please provide From Date and To Date for Hotel expense.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
                if (toDate.Value < fromDate.Value)
                {
                    LBL_ExpenseMsg.Text = "Hotel To Date cannot be earlier than From Date.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
            }

            // If travel selected, require travel date and amount > 0
            if (expenseType.Equals("Travel", StringComparison.OrdinalIgnoreCase))
            {
                if (travelAmt <= 0m)
                {
                    LBL_ExpenseMsg.Text = "Please enter Travel Amount.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
                if (!travelDateNullable.HasValue)
                {
                    LBL_ExpenseMsg.Text = "Please enter Travel Date.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
            }

            // If nothing to save
            bool anyFile = FU_Attachment != null && FU_Attachment.HasFile;
            if (expenseType.Equals("DA", StringComparison.OrdinalIgnoreCase) && daAmt <= 0m && !anyFile)
            {
                LBL_ExpenseMsg.Text = "Nothing to save. Enter DA amount or attach a file.";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                return;
            }

            // For Hotel branch, check hotelAmt or file or bill no etc.
            if (expenseType.Equals("Hotel", StringComparison.OrdinalIgnoreCase) && hotelAmt <= 0m && !anyFile)
            {
                LBL_ExpenseMsg.Text = "Nothing to save. Enter Hotel amount or attach a file.";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                return;
            }

            // For Travel branch we already validated travelAmt and travelDate.

            // handle file save (shared for both DA, Hotel, Travel)
            string savedRelativePath = null;
            string savedFileName = null;

            try
            {
                if (FU_Attachment != null && FU_Attachment.HasFile)
                {
                    if (FU_Attachment.PostedFile.ContentLength > 5 * 1024 * 1024)
                    {
                        LBL_ExpenseMsg.Text = "Attachment too large (max 5MB).";
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                        return;
                    }

                    string ext = Path.GetExtension(FU_Attachment.FileName).ToLower();
                    var allowed = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                    if (Array.IndexOf(allowed, ext) < 0)
                    {
                        LBL_ExpenseMsg.Text = "Invalid file type. Allowed: JPG, PNG, PDF.";
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                        return;
                    }

                    string baseFolder = Server.MapPath("~/UploadedFiles/ClaimsAttachment/");
                    string year = DateTime.UtcNow.AddMinutes(330).ToString("yyyy");
                    string month = DateTime.UtcNow.AddMinutes(330).ToString("MM");
                    string uploadsFolder = Path.Combine(baseFolder, year, month);
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string safeLogin = (GetCurrentUserLoginId(false) ?? "anon").Replace(" ", "_");
                    string uniquePart = DateTime.UtcNow.AddMinutes(330).ToString("yyyyMMddHHmmss") + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    savedFileName = $"{safeLogin}_{activityId}_{uniquePart}{ext}";
                    string savedFullPath = Path.Combine(uploadsFolder, savedFileName);
                    FU_Attachment.SaveAs(savedFullPath);
                    savedRelativePath = Path.Combine("UploadedFiles/ClaimsAttachment", year, month, savedFileName).Replace("\\", "/");
                }

                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();

                    // DA branch - use usp_SaveDailyExpense if amount > 0
                    if (expenseType.Equals("DA", StringComparison.OrdinalIgnoreCase) && daAmt > 0m)
                    {
                        using (SqlCommand cmd = new SqlCommand("usp_SaveDailyExpense", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@ActivityId", activityId);
                            cmd.Parameters.AddWithValue("@ExpenseType", "DA");

                            DateTime? expDate = null;
                            if (!string.IsNullOrWhiteSpace(HF_ActivityDate.Value))
                            {
                                DateTime tmp;
                                if (DateTime.TryParse(HF_ActivityDate.Value, out tmp)) expDate = tmp.Date;
                            }
                            cmd.Parameters.AddWithValue("@ExpenseDate", expDate.HasValue ? (object)expDate.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@Amount", daAmt);
                            cmd.Parameters.AddWithValue("@Purpose", DBNull.Value);
                            cmd.Parameters.AddWithValue("@BillNo", DBNull.Value);
                            //cmd.Parameters.AddWithValue("@AttachmentFileName", savedRelativePath ?? (object)DBNull.Value);
                            // pass NULL for AttachmentContent when file saved to disk
                            //cmd.Parameters.Add("@AttachmentContent", SqlDbType.VarBinary, -1).Value = savedFileName != null ? (object)DBNull.Value : (object)DBNull.Value;

                            //cmd.Parameters.AddWithValue("@AttachmentContent", DBNull.Value);
                            cmd.Parameters.Add("@AttachmentContent", SqlDbType.VarBinary, -1).Value = savedFileName != null ? (object)DBNull.Value : (object)DBNull.Value;
                            cmd.Parameters.AddWithValue("@AttachmentFileName", savedFileName);
                            cmd.Parameters.AddWithValue("@AttachmentPath", savedRelativePath ?? (object)DBNull.Value);

                            cmd.Parameters.AddWithValue("@CreatedBy", GetCurrentUserLoginId());
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Hotel branch - use usp_SaveHotelExpense if amount > 0
                    if (expenseType.Equals("Hotel", StringComparison.OrdinalIgnoreCase) && hotelAmt > 0m)
                    {
                        using (SqlCommand cmd = new SqlCommand("usp_SaveHotelExpense", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@ActivityId", activityId);
                            cmd.Parameters.AddWithValue("@FromDate", fromDate.Value);
                            cmd.Parameters.AddWithValue("@ToDate", toDate.Value);
                            cmd.Parameters.AddWithValue("@BillNo", string.IsNullOrWhiteSpace(TXT_HotelBillNo.Text) ? (object)DBNull.Value : TXT_HotelBillNo.Text.Trim());
                            cmd.Parameters.AddWithValue("@Amount", hotelAmt);
                            cmd.Parameters.AddWithValue("@Purpose", string.IsNullOrWhiteSpace(TXT_HotelPurpose.Text) ? (object)DBNull.Value : TXT_HotelPurpose.Text.Trim());
                            //cmd.Parameters.AddWithValue("@AttachmentFileName", savedRelativePath ?? (object)DBNull.Value);
                            //cmd.Parameters.Add("@AttachmentContent", SqlDbType.VarBinary, -1).Value = savedFileName != null ? (object)DBNull.Value : (object)DBNull.Value;

                            cmd.Parameters.Add("@AttachmentContent", SqlDbType.VarBinary, -1).Value = savedFileName != null ? (object)DBNull.Value : (object)DBNull.Value;
                            cmd.Parameters.AddWithValue("@AttachmentFileName", savedFileName);
                            cmd.Parameters.AddWithValue("@AttachmentPath", savedRelativePath ?? (object)DBNull.Value);

                            cmd.Parameters.AddWithValue("@CreatedBy", GetCurrentUserLoginId());
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // --- Travel branch: use structured columns (Option B) ---
                    if (expenseType.Equals("Travel", StringComparison.OrdinalIgnoreCase) && travelAmt > 0m)
                    {
                        string travelType = DDL_TravelType != null ? DDL_TravelType.SelectedValue : "";
                        string fromPlace = TXT_FromPlace != null ? TXT_FromPlace.Text.Trim() : "";
                        string toPlace = TXT_ToPlace != null ? TXT_ToPlace.Text.Trim() : "";
                        string invoiceNo = (CHK_TravelInvoice != null && CHK_TravelInvoice.Checked) ? (TXT_TravelInvoiceNo != null ? TXT_TravelInvoiceNo.Text.Trim() : "") : null;
                        string travelPurpose = TXT_TravelPurpose != null ? TXT_TravelPurpose.Text.Trim() : "";

                        using (SqlCommand cmd = new SqlCommand(@"
                                INSERT INTO dbo.SalesDailyExpense
                                  (ActivityId, ExpenseType, ExpenseDate, FromDate, ToDate, BillNo, Amount, Currency, Purpose, AttachmentFileName, AttachmentContent, CreatedBy,
                                   TravelType, FromPlace, ToPlace, TravelDate)
                                VALUES
                                  (@ActivityId, @ExpenseType, @ExpenseDate, NULL, NULL, @BillNo, @Amount, @Currency, @Purpose, @AttachmentFileName, @AttachmentContent, @CreatedBy,
                                   @TravelType, @FromPlace, @ToPlace, @TravelDate)
                            ", con))
                        {
                            cmd.CommandType = CommandType.Text;
                            cmd.Parameters.AddWithValue("@ActivityId", activityId);
                            cmd.Parameters.AddWithValue("@ExpenseType", "Travel");
                            cmd.Parameters.AddWithValue("@ExpenseDate", travelDateNullable.HasValue ? (object)travelDateNullable.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@BillNo", string.IsNullOrWhiteSpace(invoiceNo) ? (object)DBNull.Value : invoiceNo);
                            cmd.Parameters.AddWithValue("@Amount", travelAmt);
                            cmd.Parameters.AddWithValue("@Currency", DBNull.Value); // or pass textbox if you have currency field
                            cmd.Parameters.AddWithValue("@Purpose", string.IsNullOrWhiteSpace(travelPurpose) ? (object)DBNull.Value : travelPurpose);
                            //cmd.Parameters.AddWithValue("@AttachmentFileName", savedRelativePath ?? (object)DBNull.Value);

                            // AttachmentContent: we store NULL (file on disk). If you want to store bytes, change this to file bytes.
                            cmd.Parameters.Add("@AttachmentContent", SqlDbType.VarBinary, -1).Value = DBNull.Value;

                            //cmd.Parameters.Add("@AttachmentContent", SqlDbType.VarBinary, -1).Value = savedFileName != null ? (object)DBNull.Value : (object)DBNull.Value;
                            cmd.Parameters.AddWithValue("@AttachmentFileName", savedFileName);
                            cmd.Parameters.AddWithValue("@AttachmentPath", savedRelativePath ?? (object)DBNull.Value);

                            // New travel fields
                            cmd.Parameters.AddWithValue("@TravelType", string.IsNullOrWhiteSpace(travelType) ? (object)DBNull.Value : travelType);
                            cmd.Parameters.AddWithValue("@FromPlace", string.IsNullOrWhiteSpace(fromPlace) ? (object)DBNull.Value : fromPlace);
                            cmd.Parameters.AddWithValue("@ToPlace", string.IsNullOrWhiteSpace(toPlace) ? (object)DBNull.Value : toPlace);
                            cmd.Parameters.AddWithValue("@TravelDate", travelDateNullable.HasValue ? (object)travelDateNullable.Value : DBNull.Value);

                            cmd.Parameters.AddWithValue("@CreatedBy", GetCurrentUserLoginId());
                            cmd.ExecuteNonQuery();
                        }
                    }


                    //// Travel branch - route through usp_SaveDailyExpense, storing travel fields in Purpose (structured string)
                    //if (expenseType.Equals("Travel", StringComparison.OrdinalIgnoreCase) && travelAmt > 0m)
                    //{
                    //    string travelType = DDL_TravelType != null ? DDL_TravelType.SelectedValue : "";
                    //    string fromPlace = TXT_FromPlace != null ? TXT_FromPlace.Text.Trim() : "";
                    //    string toPlace = TXT_ToPlace != null ? TXT_ToPlace.Text.Trim() : "";
                    //    string invoiceNo = (CHK_TravelInvoice != null && CHK_TravelInvoice.Checked) ? (TXT_TravelInvoiceNo != null ? TXT_TravelInvoiceNo.Text.Trim() : "") : null;
                    //    string travelPurpose = TXT_TravelPurpose != null ? TXT_TravelPurpose.Text.Trim() : "";

                    //    // Build structured purpose:
                    //    // "[Train] Mumbai -> Pune | TravelDate=2025-10-10 | Invoice=INV123 | Meeting with client"
                    //    var parts = new List<string>();
                    //    if (!string.IsNullOrWhiteSpace(travelType)) parts.Add($"[{travelType}]");
                    //    if (!string.IsNullOrWhiteSpace(fromPlace) || !string.IsNullOrWhiteSpace(toPlace)) parts.Add($"{fromPlace} -> {toPlace}");
                    //    if (travelDateNullable.HasValue) parts.Add($"TravelDate={travelDateNullable.Value:yyyy-MM-dd}");
                    //    if (!string.IsNullOrWhiteSpace(invoiceNo)) parts.Add($"Invoice={invoiceNo}");
                    //    string structuredPrefix = string.Join(" | ", parts);
                    //    string finalPurpose = string.IsNullOrWhiteSpace(travelPurpose) ? structuredPrefix : $"{structuredPrefix} | {travelPurpose}";

                    //    using (SqlCommand cmd = new SqlCommand("usp_SaveDailyExpense", con))
                    //    {
                    //        cmd.CommandType = CommandType.StoredProcedure;
                    //        cmd.Parameters.AddWithValue("@ActivityId", activityId);
                    //        cmd.Parameters.AddWithValue("@ExpenseType", "Travel");
                    //        // use travelDate as ExpenseDate
                    //        cmd.Parameters.AddWithValue("@ExpenseDate", travelDateNullable.HasValue ? (object)travelDateNullable.Value : DBNull.Value);
                    //        cmd.Parameters.AddWithValue("@Amount", travelAmt);
                    //        cmd.Parameters.AddWithValue("@Purpose", string.IsNullOrWhiteSpace(finalPurpose) ? (object)DBNull.Value : finalPurpose);
                    //        cmd.Parameters.AddWithValue("@BillNo", string.IsNullOrWhiteSpace(invoiceNo) ? (object)DBNull.Value : invoiceNo);
                    //        cmd.Parameters.AddWithValue("@AttachmentFileName", savedRelativePath ?? (object)DBNull.Value);
                    //        cmd.Parameters.Add("@AttachmentContent", SqlDbType.VarBinary, -1).Value = savedFileName != null ? (object)DBNull.Value : (object)DBNull.Value;
                    //        cmd.Parameters.AddWithValue("@CreatedBy", GetCurrentUserLoginId());
                    //        cmd.ExecuteNonQuery();
                    //    }
                    //}
                } // using con

                // reload list and UI
                DataTable dtAfter = LoadExpenseForActivity(activityId.ToString());
                BindGrid(false);

                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Green;
                LBL_ExpenseMsg.Text = "Expense saved.";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
            }
            catch (SqlException sqx)
            {
                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                LBL_ExpenseMsg.Text = sqx.Message;
                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
            }
            catch (Exception ex)
            {
                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                LBL_ExpenseMsg.Text = ex.Message;
                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);

                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyError2", $"showPNotify('Error', '{ex.Message.Replace("'", "\\'")}', 'error');", true);
            }
        }

        protected void BTN_SaveExpense_Click(object sender, EventArgs e)
        {
            LBL_ExpenseMsg.Text = "";
            LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;

            int activityId;
            if (!int.TryParse(HF_ActivityId.Value, out activityId))
            {
                LBL_ExpenseMsg.Text = "Invalid activity.";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                return;
            }

            // Expense type
            string expenseType = "DA";
            if (DDL_ExpenseType != null && !string.IsNullOrWhiteSpace(DDL_ExpenseType.SelectedValue))
                expenseType = DDL_ExpenseType.SelectedValue.Trim();

            // Parse numeric fields
            decimal daAmt = 0m;
            decimal.TryParse((TXT_DAAmount?.Text ?? "").Trim(), out daAmt);

            decimal hotelAmt = 0m;
            decimal.TryParse((TXT_HotelAmount?.Text ?? "").Trim(), out hotelAmt);

            decimal travelAmt = 0m;
            decimal.TryParse((TXT_TravelAmount?.Text ?? "").Trim(), out travelAmt);

            // Dates
            DateTime? travelDateNullable = null;
            if (!string.IsNullOrWhiteSpace(TXT_TravelDate?.Text))
            {
                DateTime tmp;
                if (DateTime.TryParse(TXT_TravelDate.Text.Trim(), out tmp)) travelDateNullable = tmp.Date;
                else if (expenseType.Equals("Travel", StringComparison.OrdinalIgnoreCase))
                {
                    LBL_ExpenseMsg.Text = "Invalid Travel Date.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
            }

            DateTime? fromDate = null, toDate = null;
            if (!string.IsNullOrWhiteSpace(TXT_HotelFromDate?.Text))
            {
                DateTime tmp;
                if (DateTime.TryParse(TXT_HotelFromDate.Text.Trim(), out tmp)) fromDate = tmp.Date;
                else
                {
                    LBL_ExpenseMsg.Text = "Invalid Hotel From Date.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
            }
            if (!string.IsNullOrWhiteSpace(TXT_HotelToDate?.Text))
            {
                DateTime tmp;
                if (DateTime.TryParse(TXT_HotelToDate.Text.Trim(), out tmp)) toDate = tmp.Date;
                else
                {
                    LBL_ExpenseMsg.Text = "Invalid Hotel To Date.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
            }

            if (expenseType.Equals("DA", StringComparison.OrdinalIgnoreCase))
            {
                // Parse DA amount (assumes TXT_DAAmount.Text contains numeric or formatted value)
                //decimal daAmt = 0m;
                //decimal.TryParse(System.Text.RegularExpressions.Regex.Replace(TXT_DAAmount.Text ?? "", @"[^\d\.\-]", ""), out daAmt);

                //if (daAmt <= 0m)
                //{
                //    LBL_ExpenseMsg.Text = "Please enter DA Amount.";
                //    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                //    return;
                //}

                // From Place
                if (string.IsNullOrWhiteSpace(TXT_DAFromPlace?.Text))
                {
                    LBL_ExpenseMsg.Text = "Please enter From Place for DA.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
                if (TXT_DAFromPlace.Text.Trim().Length > 100)
                {
                    LBL_ExpenseMsg.Text = "From Place cannot exceed 100 characters.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }

                // To Place
                if (string.IsNullOrWhiteSpace(TXT_DAToPlace?.Text))
                {
                    LBL_ExpenseMsg.Text = "Please enter To Place for DA.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
                if (TXT_DAToPlace.Text.Trim().Length > 100)
                {
                    LBL_ExpenseMsg.Text = "To Place cannot exceed 100 characters.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }

                // Optional business rule: From != To
                if (string.Equals(TXT_DAFromPlace.Text.Trim(), TXT_DAToPlace.Text.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    LBL_ExpenseMsg.Text = "From Place and To Place cannot be the same for DA.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }

                // Travel Description
                if (string.IsNullOrWhiteSpace(TXT_TravelDescription?.Text))
                {
                    LBL_ExpenseMsg.Text = "Travel Description is required for DA.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
                if (TXT_TravelDescription.Text.Trim().Length < 5)
                {
                    LBL_ExpenseMsg.Text = "Travel Description must be at least 5 characters.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
            }

            // Per-branch validations (mirror client-side)
            if (expenseType.Equals("Hotel", StringComparison.OrdinalIgnoreCase))
            {
                if (hotelAmt <= 0m && !(FU_Attachment != null && FU_Attachment.HasFile))
                {
                    LBL_ExpenseMsg.Text = "Nothing to save. Enter Hotel amount or attach a file.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
                if (!fromDate.HasValue || !toDate.HasValue)
                {
                    LBL_ExpenseMsg.Text = "Please provide From Date and To Date for Hotel expense.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
                if (toDate.Value < fromDate.Value)
                {
                    LBL_ExpenseMsg.Text = "Hotel To Date cannot be earlier than From Date.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
                if (string.IsNullOrWhiteSpace(TXT_HotelBillNo?.Text))
                {
                    LBL_ExpenseMsg.Text = "Bill/Invoice No is required for Hotel expense.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
                if (!(FU_Attachment != null && FU_Attachment.HasFile))
                {
                    LBL_ExpenseMsg.Text = "Please attach a valid hotel receipt (JPG, PNG or PDF, ≤ 5MB).";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
            }

            if (expenseType.Equals("Travel", StringComparison.OrdinalIgnoreCase))
            {
                if (travelAmt <= 0m)
                {
                    LBL_ExpenseMsg.Text = "Please enter Travel Amount.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
                if (!travelDateNullable.HasValue)
                {
                    LBL_ExpenseMsg.Text = "Please enter Travel Date.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }
                if (CHK_TravelInvoice != null && CHK_TravelInvoice.Checked)
                {
                    if (string.IsNullOrWhiteSpace(TXT_TravelInvoiceNo?.Text))
                    {
                        LBL_ExpenseMsg.Text = "Invoice No is required when 'With Invoice' is checked.";
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                        return;
                    }
                    if (!(FU_Attachment != null && FU_Attachment.HasFile))
                    {
                        LBL_ExpenseMsg.Text = "Please attach invoice/receipt when 'With Invoice' is checked.";
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                        return;
                    }
                }
            }

            // DA branch: require amount > 0 or an attachment
            bool anyFile = FU_Attachment != null && FU_Attachment.HasFile;
            if (expenseType.Equals("DA", StringComparison.OrdinalIgnoreCase) && daAmt <= 0m && !anyFile)
            {
                LBL_ExpenseMsg.Text = "Nothing to save. Enter DA amount or attach a file.";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                return;
            }

            // Validate file if present
            string savedRelativePath = null;
            string savedFileName = null;
            try
            {
                if (FU_Attachment != null && FU_Attachment.HasFile)
                {
                    if (FU_Attachment.PostedFile.ContentLength > 5 * 1024 * 1024)
                    {
                        LBL_ExpenseMsg.Text = "Attachment too large (max 5MB).";
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                        return;
                    }
                    string ext = Path.GetExtension(FU_Attachment.FileName).ToLower();
                    var allowed = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                    if (Array.IndexOf(allowed, ext) < 0)
                    {
                        LBL_ExpenseMsg.Text = "Invalid file type. Allowed: JPG, PNG, PDF.";
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                        return;
                    }

                    // IST now
                    DateTime istNow;
                    try
                    {
                        var tz = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                        istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
                    }
                    catch
                    {
                        istNow = DateTime.UtcNow.AddMinutes(330);
                    }

                    string baseFolder = Server.MapPath("~/UploadedFiles/ClaimsAttachment/");
                    string year = istNow.ToString("yyyy");
                    string month = istNow.ToString("MM");
                    string uploadsFolder = Path.Combine(baseFolder, year, month);
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string safeLogin = (GetCurrentUserLoginId(false) ?? "anon").Replace(" ", "_");
                    string uniquePart = istNow.ToString("yyyyMMddHHmmss") + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    savedFileName = $"{safeLogin}_{activityId}_{uniquePart}{ext}";
                    string savedFullPath = Path.Combine(uploadsFolder, savedFileName);
                    FU_Attachment.SaveAs(savedFullPath);
                    savedRelativePath = Path.Combine("UploadedFiles/ClaimsAttachment", year, month, savedFileName).Replace("\\", "/");
                }

                // Build policy snapshot JSON (simple, no JSON lib)
                string policyMode = (ResolveHiddenFieldValue("HF_ResolvedPolicyMode") ?? "").Trim();
                string hfDA = (ResolveHiddenFieldValue("HF_ResolvedDA") ?? "").Trim();
                string hfOutDA = (ResolveHiddenFieldValue("HF_OutstationDA") ?? "").Trim();
                string hfOwnDA = (ResolveHiddenFieldValue("HF_OwnArrangementDA") ?? "").Trim();
                string hfHotelCap = (ResolveHiddenFieldValue("HF_ResolvedHotelCap") ?? "").Trim();

                string policySnapshot = "{"
                    + "\"PolicyMode\":\"" + EscapeJson(policyMode) + "\","
                    + "\"ResolvedDA\":\"" + EscapeJson(hfDA) + "\","
                    + "\"OutstationDA\":\"" + EscapeJson(hfOutDA) + "\","
                    + "\"OwnArrangementDA\":\"" + EscapeJson(hfOwnDA) + "\","
                    + "\"HotelCap\":\"" + EscapeJson(hfHotelCap) + "\""
                    + "}";

                // CreatedByIP
                string createdByIp = null;
                try { createdByIp = Request.UserHostAddress; } catch { createdByIp = null; }

                // Convert HF decimals safely (nullable)
                decimal? resolvedDA = TryParseDecimalOrNull(hfDA);
                decimal? resolvedOutstationDA = TryParseDecimalOrNull(hfOutDA);
                decimal? resolvedOwnDA = TryParseDecimalOrNull(hfOwnDA);
                decimal? resolvedHotelCap = TryParseDecimalOrNull(hfHotelCap);

                // DB operations
                string connString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();

                    // Use updated stored procedures for DA and Hotel (they accept policy params & output ErrCode/ErrMessage)
                    if (expenseType.Equals("DA", StringComparison.OrdinalIgnoreCase) && (daAmt > 0m || savedFileName != null))
                    {
                        string fromPlace = TXT_DAFromPlace != null ? TXT_DAFromPlace.Text.Trim() : null;
                        string toPlace = TXT_DAToPlace != null ? TXT_DAToPlace.Text.Trim() : null;

                        using (SqlCommand cmd = new SqlCommand("usp_SaveDailyExpense", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@ActivityId", activityId);
                            cmd.Parameters.AddWithValue("@ExpenseType", "DA");
                            // ExpenseDate: prefer HF_ActivityDate if present
                            DateTime expDate;
                            DateTime? expDateNullable = null;
                            if (!string.IsNullOrWhiteSpace(HF_ActivityDate.Value) && DateTime.TryParse(HF_ActivityDate.Value.Trim(), out expDate))
                                expDateNullable = expDate.Date;
                            cmd.Parameters.AddWithValue("@ExpenseDate", expDateNullable.HasValue ? (object)expDateNullable.Value : (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Amount", daAmt);
                            cmd.Parameters.AddWithValue("@FromPlace", string.IsNullOrWhiteSpace(fromPlace) ? (object)DBNull.Value : fromPlace);
                            cmd.Parameters.AddWithValue("@ToPlace", string.IsNullOrWhiteSpace(toPlace) ? (object)DBNull.Value : toPlace);
                            cmd.Parameters.AddWithValue("@Purpose", string.IsNullOrWhiteSpace(TXT_TravelDescription?.Text) ? (object)DBNull.Value : TXT_TravelDescription.Text.Trim());
                            cmd.Parameters.AddWithValue("@BillNo", DBNull.Value);
                            cmd.Parameters.AddWithValue("@AttachmentFileName", savedFileName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@AttachmentPath", savedRelativePath ?? (object)DBNull.Value);
                            //cmd.Parameters.AddWithValue("@AttachmentContent", DBNull.Value);
                            cmd.Parameters.Add("@AttachmentContent", SqlDbType.VarBinary, -1).Value = DBNull.Value;
                            cmd.Parameters.AddWithValue("@CreatedBy", GetCurrentUserLoginId());

                            cmd.Parameters.AddWithValue("@PolicyMode", string.IsNullOrWhiteSpace(policyMode) ? (object)DBNull.Value : policyMode);
                            cmd.Parameters.AddWithValue("@PolicySnapshot", string.IsNullOrWhiteSpace(policySnapshot) ? (object)DBNull.Value : policySnapshot);
                            cmd.Parameters.AddWithValue("@IsAutoDA", 0); // if your logic determines auto-DA set 1
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
                            if (err != 0)
                            {
                                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                                LBL_ExpenseMsg.Text = errMsg ?? "Action blocked by policy.";
                                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                                return;
                            }

                            // SUCCESS → Make controls readonly
                            TXT_DAFromPlace.ReadOnly = true;
                            TXT_DAToPlace.ReadOnly = true;
                            TXT_TravelDescription.ReadOnly = true;

                            if (FU_Attachment != null)
                                FU_Attachment.Enabled = false;

                            if (TXT_DAAmount != null)
                                TXT_DAAmount.ReadOnly = true;

                            LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Green;
                            LBL_ExpenseMsg.Text = "DA saved successfully.";
                        }
                    }

                    if (expenseType.Equals("Hotel", StringComparison.OrdinalIgnoreCase) && (hotelAmt > 0m || savedFileName != null))
                    {
                        using (SqlCommand cmd = new SqlCommand("usp_SaveHotelExpense", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@ActivityId", activityId);
                            cmd.Parameters.AddWithValue("@FromDate", fromDate.Value);
                            cmd.Parameters.AddWithValue("@ToDate", toDate.Value);
                            cmd.Parameters.AddWithValue("@BillNo", string.IsNullOrWhiteSpace(TXT_HotelBillNo.Text) ? (object)DBNull.Value : TXT_HotelBillNo.Text.Trim());
                            cmd.Parameters.AddWithValue("@Amount", hotelAmt);
                            cmd.Parameters.AddWithValue("@Purpose", string.IsNullOrWhiteSpace(TXT_HotelPurpose?.Text) ? (object)DBNull.Value : TXT_HotelPurpose.Text.Trim());
                            cmd.Parameters.AddWithValue("@AttachmentFileName", savedFileName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@AttachmentPath", savedRelativePath ?? (object)DBNull.Value);
                            //cmd.Parameters.AddWithValue("@AttachmentContent", DBNull.Value);
                            cmd.Parameters.Add("@AttachmentContent", SqlDbType.VarBinary, -1).Value = DBNull.Value;
                            cmd.Parameters.AddWithValue("@CreatedBy", GetCurrentUserLoginId());

                            // policy params
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
                            if (err != 0)
                            {
                                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                                LBL_ExpenseMsg.Text = errMsg ?? "Action blocked by policy.";
                                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                                return;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(policyMode) &&
                            (policyMode.Equals("Outstation", StringComparison.OrdinalIgnoreCase) ||
                             policyMode.Equals("OwnArrangement", StringComparison.OrdinalIgnoreCase)))
                        {
                            // pick policy DA value (OutstationDA or OwnArrangementDA)
                            decimal autoDA = 0m;
                            if (policyMode.Equals("Outstation", StringComparison.OrdinalIgnoreCase))
                                decimal.TryParse(HF_OutstationDA?.Value, out autoDA);
                            else
                                decimal.TryParse(HF_OwnArrangementDA?.Value, out autoDA);

                            if (autoDA > 0m)
                            {
                                // Determine which date to stamp (activity / travel / hotel)
                                DateTime daDate = DateTime.Now;
                                if (!string.IsNullOrWhiteSpace(HF_ActivityDate?.Value))
                                {
                                    DateTime.TryParse(HF_ActivityDate.Value, out daDate);
                                }
                                else if (fromDate.HasValue) daDate = fromDate.Value;
                                else if (travelDateNullable.HasValue) daDate = travelDateNullable.Value;

                                // snapshot JSON (optional, can be blank)
                                //string snapshot = string.Format("DA={0};HotelCap={1}",HF_ResolvedDA?.Value ?? "NA", HF_ResolvedHotelCap?.Value ?? "NA");

                                EnsureAutoDA(activityId, daDate, autoDA, policyMode, policySnapshot, GetCurrentUserLoginId());
                            }
                        }
                    }

                    // Travel: insert inline (preserve existing behavior) but include policy metadata columns
                    if (expenseType.Equals("Travel", StringComparison.OrdinalIgnoreCase) && travelAmt > 0m)
                    {
                        string travelType = DDL_TravelType != null ? DDL_TravelType.SelectedValue : null;
                        string fromPlace = TXT_FromPlace != null ? TXT_FromPlace.Text.Trim() : null;
                        string toPlace = TXT_ToPlace != null ? TXT_ToPlace.Text.Trim() : null;
                        string invoiceNo = (CHK_TravelInvoice != null && CHK_TravelInvoice.Checked) ? (TXT_TravelInvoiceNo != null ? TXT_TravelInvoiceNo.Text.Trim() : null) : null;
                        string travelPurpose = TXT_TravelPurpose != null ? TXT_TravelPurpose.Text.Trim() : null;

                        //        using (SqlCommand cmd = new SqlCommand(@"
                        //    INSERT INTO dbo.SalesDailyExpense
                        //      (ActivityId, ExpenseType, ExpenseDate, FromDate, ToDate, BillNo, Amount, Currency, Purpose, AttachmentFileName, AttachmentContent, CreatedBy,
                        //       CreatedDateTime, TravelType, FromPlace, ToPlace, TravelDate,
                        //       PolicyMode, PolicySnapshot, IsAutoDA, CreatedByIP, Source, AttachmentPath)
                        //    VALUES
                        //      (@ActivityId, @ExpenseType, @ExpenseDate, NULL, NULL, @BillNo, @Amount, @Currency, @Purpose, @AttachmentFileName, @AttachmentContent, @CreatedBy,
                        //       GETUTCDATE(), @TravelType, @FromPlace, @ToPlace, @TravelDate,
                        //       @PolicyMode, @PolicySnapshot, @IsAutoDA, @CreatedByIP, @Source, @AttachmentPath)
                        //", con))
                        //        {
                        //            cmd.CommandType = CommandType.Text;
                        //            cmd.Parameters.AddWithValue("@ActivityId", activityId);
                        //            cmd.Parameters.AddWithValue("@ExpenseType", "Travel");
                        //            cmd.Parameters.AddWithValue("@ExpenseDate", travelDateNullable.HasValue ? (object)travelDateNullable.Value : (object)DBNull.Value);
                        //            cmd.Parameters.AddWithValue("@BillNo", string.IsNullOrWhiteSpace(invoiceNo) ? (object)DBNull.Value : invoiceNo);
                        //            cmd.Parameters.AddWithValue("@Amount", travelAmt);
                        //            cmd.Parameters.AddWithValue("@Currency", DBNull.Value);
                        //            cmd.Parameters.AddWithValue("@Purpose", string.IsNullOrWhiteSpace(travelPurpose) ? (object)DBNull.Value : travelPurpose);
                        //            cmd.Parameters.AddWithValue("@AttachmentFileName", savedFileName ?? (object)DBNull.Value);
                        //            //cmd.Parameters.AddWithValue("@AttachmentContent", DBNull.Value);
                        //            cmd.Parameters.Add("@AttachmentContent", SqlDbType.VarBinary, -1).Value = DBNull.Value;
                        //            cmd.Parameters.AddWithValue("@CreatedBy", GetCurrentUserLoginId());

                        //            cmd.Parameters.AddWithValue("@TravelType", string.IsNullOrWhiteSpace(travelType) ? (object)DBNull.Value : travelType);
                        //            cmd.Parameters.AddWithValue("@FromPlace", string.IsNullOrWhiteSpace(fromPlace) ? (object)DBNull.Value : fromPlace);
                        //            cmd.Parameters.AddWithValue("@ToPlace", string.IsNullOrWhiteSpace(toPlace) ? (object)DBNull.Value : toPlace);
                        //            cmd.Parameters.AddWithValue("@TravelDate", travelDateNullable.HasValue ? (object)travelDateNullable.Value : (object)DBNull.Value);

                        //            // policy metadata
                        //            cmd.Parameters.AddWithValue("@PolicyMode", string.IsNullOrWhiteSpace(policyMode) ? (object)DBNull.Value : policyMode);
                        //            cmd.Parameters.AddWithValue("@PolicySnapshot", string.IsNullOrWhiteSpace(policySnapshot) ? (object)DBNull.Value : policySnapshot);
                        //            cmd.Parameters.AddWithValue("@IsAutoDA", 0);
                        //            cmd.Parameters.AddWithValue("@CreatedByIP", string.IsNullOrWhiteSpace(createdByIp) ? (object)DBNull.Value : createdByIp);
                        //            cmd.Parameters.AddWithValue("@Source", "UI");
                        //            cmd.Parameters.AddWithValue("@AttachmentPath", savedRelativePath ?? (object)DBNull.Value);

                        //            cmd.CommandTimeout = 90;
                        //            cmd.ExecuteNonQuery();
                        //        }

                        //        // --- Auto-DA trigger for Outstation / OwnArrangement ---
                        //        //string policyMode = HF_ResolvedPolicyMode?.Value ?? "";
                        //        if (!string.IsNullOrWhiteSpace(policyMode) &&
                        //            (policyMode.Equals("Outstation", StringComparison.OrdinalIgnoreCase) ||
                        //             policyMode.Equals("OwnArrangement", StringComparison.OrdinalIgnoreCase)))
                        //        {
                        //            // pick policy DA value (OutstationDA or OwnArrangementDA)
                        //            decimal autoDA = 0m;
                        //            if (policyMode.Equals("Outstation", StringComparison.OrdinalIgnoreCase))
                        //                decimal.TryParse(HF_OutstationDA?.Value, out autoDA);
                        //            else
                        //                decimal.TryParse(HF_OwnArrangementDA?.Value, out autoDA);

                        //            if (autoDA > 0m)
                        //            {
                        //                // Determine which date to stamp (activity / travel / hotel)
                        //                DateTime daDate = DateTime.Now;
                        //                if (!string.IsNullOrWhiteSpace(HF_ActivityDate?.Value))
                        //                {
                        //                    DateTime.TryParse(HF_ActivityDate.Value, out daDate);
                        //                }
                        //                else if (fromDate.HasValue) daDate = fromDate.Value;
                        //                else if (travelDateNullable.HasValue) daDate = travelDateNullable.Value;

                        //                // snapshot JSON (optional, can be blank)
                        //                //string snapshot = string.Format("DA={0};HotelCap={1}",
                        //                    //HF_ResolvedDA?.Value ?? "NA", HF_ResolvedHotelCap?.Value ?? "NA");

                        //                EnsureAutoDA(activityId, daDate, autoDA, policyMode, policySnapshot, GetCurrentUserLoginId());
                        //            }
                        //        }

                        using (SqlCommand cmd = new SqlCommand("dbo.usp_InsertTravel_WithLock", con))
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

                            cmd.Parameters.Add(pIsFirst);
                            cmd.Parameters.Add(pInsertedId);
                            cmd.Parameters.Add(pErr);
                            cmd.Parameters.Add(pMsg);

                            //con.Open();
                            cmd.ExecuteNonQuery();

                            int err = pErr.Value != DBNull.Value ? (int)pErr.Value : 0;
                            if (err != 0)
                            {
                                // handle error - show message
                                throw new InvalidOperationException(pMsg.Value?.ToString() ?? "DB error");
                            }

                            bool isFirst = (pIsFirst.Value != DBNull.Value && (bool)pIsFirst.Value);
                            int insertedExpenseId = pInsertedId.Value != DBNull.Value ? (int)pInsertedId.Value : 0;

                            if (!string.IsNullOrWhiteSpace(policyMode) &&
                                    (policyMode.Equals("Outstation", StringComparison.OrdinalIgnoreCase) ||
                                     policyMode.Equals("OwnArrangement", StringComparison.OrdinalIgnoreCase)))
                            {
                                // pick policy DA value (OutstationDA or OwnArrangementDA)
                                decimal autoDA = 0m;
                                if (policyMode.Equals("Outstation", StringComparison.OrdinalIgnoreCase))
                                    decimal.TryParse(HF_OutstationDA?.Value, out autoDA);
                                else
                                    decimal.TryParse(HF_OwnArrangementDA?.Value, out autoDA);

                                // Only trigger Auto-DA if this was the first travel for this activity+date
                                if (isFirst)
                                {
                                    // compute autoDA as before (from policy)
                                    EnsureAutoDA(activityId, travelDateNullable ?? DateTime.Now, autoDA, policyMode, policySnapshot, GetCurrentUserLoginId());
                                }
                                else
                                {
                                    // skip autoDA for 2nd+ travel rows
                                }
                            }

                        }
                    }
                }

                // reload UI
                DataTable dtAfter = LoadExpenseForActivity(activityId.ToString());
                Session.Remove(SESSION_KEY_GV_ACTIVITIES);
                BindGrid(false);

                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Green;
                LBL_ExpenseMsg.Text = "Expense saved.";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
            }
            catch (SqlException sqx)
            {
                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                LBL_ExpenseMsg.Text = sqx.Message;
                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
            }
            catch (Exception ex)
            {
                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                LBL_ExpenseMsg.Text = ex.Message;
                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);

                string safeMessage = ex.Message.Replace("'", "\\'").Replace(Environment.NewLine, " ");
                ScriptManager.RegisterStartupScript(
                    this,
                    GetType(),
                    "pnotifyError2",
                    "showPNotify('Error', '" + safeMessage + "', 'error');",
                    true
                );
            }
        }

        protected void rptExpenses_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
                return;

            var rowView = e.Item.DataItem as System.Data.DataRowView;
            if (rowView == null) return;

            var r = rowView.Row;

            // read DB columns defensively
            string claimStatus = r.Table.Columns.Contains("ClaimStatus") && r["ClaimStatus"] != DBNull.Value ? r["ClaimStatus"].ToString() : "";
            bool isAuto = r.Table.Columns.Contains("IsAutoDA") && r["IsAutoDA"] != DBNull.Value && Convert.ToInt32(r["IsAutoDA"]) == 1;
            string policyMode = r.Table.Columns.Contains("PolicyMode") && r["PolicyMode"] != DBNull.Value ? r["PolicyMode"].ToString() : "";

            LinkButton lnkDelete = e.Item.FindControl("lnkDeleteExpense") as LinkButton;
            LinkButton lnkDeleteDisabled = e.Item.FindControl("lnkDeleteExpenseDisabled") as LinkButton;
            Literal litBadges = e.Item.FindControl("litPolicyBadges") as Literal;

            // Decide allowed actions (authoritative)
            bool canDelete = true;

            // If expense-level claim status exists and is submitted/approved — block delete
            if (!string.IsNullOrWhiteSpace(claimStatus) &&
                (claimStatus.Equals("Submitted", StringComparison.OrdinalIgnoreCase) ||
                 claimStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase)))
            {
                canDelete = false;
            }

            // Block delete when this is an auto-applied row
            if (isAuto) canDelete = false;

            if (lnkDelete != null) lnkDelete.Visible = canDelete;
            if (lnkDeleteDisabled != null) lnkDeleteDisabled.Visible = !canDelete;

            // Badges: PolicyMode, AUTO, ClaimStatus
            if (litBadges != null)
            {
                var sb = new System.Text.StringBuilder();
                if (!string.IsNullOrWhiteSpace(policyMode))
                    sb.AppendFormat("<span class='badge badge-info mr-1'>{0}</span>", HttpUtility.HtmlEncode(policyMode));
                if (isAuto)
                    sb.Append("<span class='badge badge-warning mr-1'>AUTO</span>");
                if (!string.IsNullOrWhiteSpace(claimStatus))
                    sb.AppendFormat("<span class='badge badge-secondary'>{0}</span>", HttpUtility.HtmlEncode(claimStatus));

                litBadges.Text = sb.ToString();
            }

            // Ensure CommandArgument includes AttachmentPath (helps server delete)
            if (lnkDelete != null && (string.IsNullOrWhiteSpace(lnkDelete.CommandArgument) || !lnkDelete.CommandArgument.Contains("|")))
            {
                string expenseId = r.Table.Columns.Contains("ExpenseId") && r["ExpenseId"] != DBNull.Value ? r["ExpenseId"].ToString() : "";
                string activityId = r.Table.Columns.Contains("ActivityId") && r["ActivityId"] != DBNull.Value ? r["ActivityId"].ToString() : "";
                string attachmentPath = r.Table.Columns.Contains("AttachmentPath") && r["AttachmentPath"] != DBNull.Value ? r["AttachmentPath"].ToString() : "";
                lnkDelete.CommandArgument = $"{expenseId}|{attachmentPath}|{activityId}";
            }
        }


        private void EnsureAutoDA_OLD(int activityId, DateTime expenseDate, decimal amount, string policyMode, string policySnapshot, string createdBy)
        {
            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                // ✅ Best Practice: Check if DA already exists for same Activity & Date
                using (SqlCommand chk = new SqlCommand(@"
                    SELECT CASE WHEN EXISTS (
                        SELECT 1
                        FROM dbo.SalesDailyExpense WITH (NOLOCK)
                        WHERE ActivityId = @ActivityId
                          AND LOWER(ExpenseType) = 'da'
                          AND CAST(ExpenseDate AS date) = @ExpenseDate
                    ) THEN 1 ELSE 0 END", con))
                {
                    chk.CommandType = CommandType.Text;

                    chk.Parameters.Add("@ActivityId", SqlDbType.Int).Value = activityId;
                    chk.Parameters.Add("@ExpenseDate", SqlDbType.Date).Value = expenseDate.Date;

                    int existsFlag = Convert.ToInt32(chk.ExecuteScalar() ?? 0);

                    if (existsFlag == 1)
                    {
                        // 🟡 Already exists — skip insert
                        return;
                    }


                    // 2️⃣  Insert Auto-DA via SP (honours policy guards)
                    using (SqlCommand cmd = new SqlCommand("usp_SaveDailyExpense", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@ActivityId", activityId);
                        cmd.Parameters.AddWithValue("@ExpenseType", "DA");
                        cmd.Parameters.AddWithValue("@ExpenseDate", expenseDate.Date);
                        cmd.Parameters.AddWithValue("@Amount", amount);
                        cmd.Parameters.AddWithValue("@Purpose", "Auto-applied " + policyMode + " DA");
                        cmd.Parameters.AddWithValue("@BillNo", DBNull.Value);
                        cmd.Parameters.AddWithValue("@AttachmentFileName", DBNull.Value);
                        cmd.Parameters.AddWithValue("@AttachmentPath", DBNull.Value);
                        //cmd.Parameters.AddWithValue("@AttachmentContent", DBNull.Value);
                        cmd.Parameters.Add("@AttachmentContent", SqlDbType.VarBinary, -1).Value = DBNull.Value;
                        cmd.Parameters.AddWithValue("@CreatedBy", createdBy ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PolicyMode", policyMode ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PolicySnapshot", policySnapshot ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsAutoDA", 1);
                        cmd.Parameters.AddWithValue("@CreatedByIP", Request.UserHostAddress ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Source", "AUTO-DA");

                        var pErr = new SqlParameter("@ErrCode", SqlDbType.Int) { Direction = ParameterDirection.Output };
                        var pMsg = new SqlParameter("@ErrMessage", SqlDbType.NVarChar, 400) { Direction = ParameterDirection.Output };
                        cmd.Parameters.Add(pErr);
                        cmd.Parameters.Add(pMsg);

                        cmd.ExecuteNonQuery();
                        // optional: log if pErr.Value != 0
                    }
                }
            }
        }

        private void EnsureAutoDA(int activityId, DateTime expenseDate, decimal amount, string policyMode, string policySnapshot, string createdBy)
        {
            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection con = new SqlConnection(connStr))
            {
                try
                {
                    if (con.State != ConnectionState.Open)
                        con.Open();

                    // ✅ Check if DA already exists for same Activity & Date
                    using (SqlCommand chk = new SqlCommand(@"
                SELECT CASE WHEN EXISTS (
                    SELECT 1
                    FROM dbo.SalesDailyExpense WITH (NOLOCK)
                    WHERE ActivityId = @ActivityId
                      AND LOWER(ExpenseType) = 'da'
                      AND CAST(ExpenseDate AS date) = @ExpenseDate
                ) THEN 1 ELSE 0 END", con))
                    {
                        chk.CommandType = CommandType.Text;
                        chk.Parameters.Add("@ActivityId", SqlDbType.Int).Value = activityId;
                        chk.Parameters.Add("@ExpenseDate", SqlDbType.Date).Value = expenseDate.Date;

                        int existsFlag = Convert.ToInt32(chk.ExecuteScalar() ?? 0);
                        if (existsFlag == 1)
                            return; // 🟡 Already exists — skip
                    }

                    // 2️⃣ Insert Auto-DA
                    using (SqlCommand cmd = new SqlCommand("usp_SaveDailyExpense", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("@ActivityId", SqlDbType.Int).Value = activityId;
                        cmd.Parameters.Add("@ExpenseType", SqlDbType.NVarChar, 50).Value = "DA";
                        cmd.Parameters.Add("@ExpenseDate", SqlDbType.Date).Value = expenseDate.Date;
                        cmd.Parameters.Add("@Amount", SqlDbType.Decimal).Value = amount;
                        cmd.Parameters["@Amount"].Precision = 18;
                        cmd.Parameters["@Amount"].Scale = 2;
                        cmd.Parameters.Add("@Purpose", SqlDbType.NVarChar, 250).Value =
                            ("Auto-applied " + (policyMode ?? "DA")).Substring(0, Math.Min(250, ("Auto-applied " + (policyMode ?? "DA")).Length));

                        cmd.Parameters.Add("@BillNo", SqlDbType.NVarChar, 100).Value = DBNull.Value;
                        cmd.Parameters.Add("@AttachmentFileName", SqlDbType.NVarChar, 255).Value = DBNull.Value;
                        cmd.Parameters.Add("@AttachmentPath", SqlDbType.NVarChar, 500).Value = DBNull.Value;

                        // ✅ Use zero-length binary instead of NULL
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
                            throw new InvalidOperationException($"usp_SaveDailyExpense returned error {errCode}: {errMsg}");
                    }
                }
                catch (SqlException sqx)
                {
                    LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                    LBL_ExpenseMsg.Text = sqx.Message;
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense2", "$('#expenseModal').modal('show');", true);
                }
                catch (Exception ex)
                {
                    LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                    LBL_ExpenseMsg.Text = ex.Message;
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense3", "$('#expenseModal').modal('show');", true);

                    string safeMessage = ex.Message.Replace("'", "\\'").Replace(Environment.NewLine, " ");
                    ScriptManager.RegisterStartupScript(
                        this,
                        GetType(),
                        "pnotifyError2",
                        "showPNotify('Error', '" + safeMessage + "', 'error');",
                        true
                    );
                }
                finally
                {
                    if (con.State != ConnectionState.Closed)
                        con.Close();
                }
            }
        }


        // --- small helpers used above (place inside the code-behind class) ---
        private static decimal? TryParseDecimalOrNull(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            decimal d;
            if (decimal.TryParse(s.Trim(), out d)) return d;
            return null;
        }

        // Resolve HF value by ID (handles different clientID modes)
        private string ResolveHiddenFieldValue(string hfId)
        {
            try
            {
                var hf = FindControlRecursive(this.Page, hfId) as HiddenField;
                if (hf != null) return hf.Value;
                // fallback by id search
                var ctrl = this.Page.Request.Form[hfId];
                return ctrl;
            }
            catch { return null; }
        }

        // Simple JSON escaper for small strings (very small footprint)
        private static string EscapeJson(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        // Recursive find (utility)
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

            // Read resolved caps from labels (or better: store them in hidden fields when binding)
            // If you stored them in hidden fields use those instead (HF_...).
            decimal resolvedHotelCap = 0M;
            decimal resolvedDA = 0M;
            decimal resolvedMaxKM = 0M;
            decimal resolvedRatePerKm = 0M;

            // Try safe parse of the label texts (strip non-numeric)
            decimal.TryParse(System.Text.RegularExpressions.Regex.Replace(lblResolvedHotelCap.Text ?? "0", "[^0-9.-]+", ""), out resolvedHotelCap);
            decimal.TryParse(System.Text.RegularExpressions.Regex.Replace(lblResolvedDA.Text ?? "0", "[^0-9.-]+", ""), out resolvedDA);
            decimal.TryParse(System.Text.RegularExpressions.Regex.Replace(lblResolvedMaxKM.Text ?? "0", "[^0-9.-]+", ""), out resolvedMaxKM);
            decimal.TryParse(System.Text.RegularExpressions.Regex.Replace(lblResolvedRatePerKm.Text ?? "0", "[^0-9.-]+", ""), out resolvedRatePerKm);

            // Common helpers
            DateTime dt;
            decimal amt;

            if (expenseType == "Hotel")
            {
                // Validate required inputs
                DateTime fromDate;
                if (!DateTime.TryParse(TXT_HotelFromDate.Text, out fromDate))
                {
                    errorMessage = "Please enter a valid 'Stay From' date.";
                    return false;
                }

                DateTime toDate;
                if (!DateTime.TryParse(TXT_HotelToDate.Text, out toDate))
                {
                    errorMessage = "Please enter a valid 'Stay To' date.";
                    return false;
                }

                if (toDate < fromDate)
                {
                    errorMessage = "'Stay To' cannot be before 'Stay From'.";
                    return false;
                }
                if (!decimal.TryParse(TXT_HotelAmount.Text, out amt) || amt <= 0)
                {
                    errorMessage = "Please enter a valid hotel amount.";
                    return false;
                }

                // Nights calculation: difference in days; treat at least 1 night
                int nights = (int)(toDate.Date - fromDate.Date).TotalDays;
                if (nights <= 0) nights = 1;

                // If resolvedHotelCap is present (>0) enforce capping: allowed = cap * nights
                if (resolvedHotelCap > 0)
                {
                    decimal allowed = resolvedHotelCap * nights;
                    if (amt > allowed)
                    {
                        errorMessage = string.Format("Hotel amount exceeds allowed cap. Allowed upto {0} per night x {1} night(s) = {2:N2}.", resolvedHotelCap, nights, allowed);
                        return false;
                    }
                }
            }
            else if (expenseType == "DA")
            {
                if (!decimal.TryParse(TXT_DAAmount.Text, out amt) || amt <= 0)
                {
                    errorMessage = "Please enter a valid DA amount.";
                    return false;
                }

                // If resolvedDA is present, ensure amount <= resolvedDA
                if (resolvedDA > 0 && amt > resolvedDA)
                {
                    errorMessage = string.Format("DA amount exceeds policy limit ({0:N2}).", resolvedDA);
                    return false;
                }
            }
            else if (expenseType == "Travel")
            {
                if (!DateTime.TryParse(TXT_TravelDate.Text, out dt))
                {
                    errorMessage = "Please enter a valid Travel Date.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(TXT_FromPlace.Text) || string.IsNullOrWhiteSpace(TXT_ToPlace.Text))
                {
                    errorMessage = "Please enter From and To places for travel.";
                    return false;
                }
                if (!decimal.TryParse(TXT_TravelAmount.Text, out amt) || amt <= 0)
                {
                    errorMessage = "Please enter a valid Travel amount.";
                    return false;
                }

                // If invoice checkbox checked, invoice no required
                if (CHK_TravelInvoice.Checked && string.IsNullOrWhiteSpace(TXT_TravelInvoiceNo.Text))
                {
                    errorMessage = "Please enter Invoice No when 'With Invoice' is checked.";
                    return false;
                }

                // Optional: if your flow collects distance, apply MaxKM*RatePerKm cap.
                // Example: if you kept Distance in a hidden field HF_TravelDistance (km):
                // int travelDistance; if (int.TryParse(HF_TravelDistance.Value, out travelDistance) && resolvedMaxKM>0 && resolvedRatePerKm>0) { var allowed = travelDistance * resolvedRatePerKm; if (amt > allowed) { ... } }

                // If you only have MaxKM & RatePerKm but no actual distance, skip this check.
            }

            // All validations passed
            return true;
        }
        private void LoadExpenseForActivity_OLD(string activityId)
        {
            DataTable dt = new DataTable();
            using (SqlConnection con = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand("SELECT ExpenseId, ExpenseType, ExpenseDate, Amount, BillNo, AttachmentFileName FROM dbo.SalesDailyExpense WHERE ActivityId = @ActivityId ORDER BY ExpenseDate, ExpenseId", con))
            {
                cmd.Parameters.AddWithValue("@ActivityId", activityId);
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }
            rptExpenses.DataSource = dt;
            rptExpenses.DataBind();
        }

        private DataTable LoadExpenseForActivity_OLD2(string activityId)
        {
            DataTable dt = new DataTable();
            using (SqlConnection con = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand(@"SELECT ExpenseId, ActivityId, ExpenseType, ExpenseDate, FromDate, ToDate, BillNo, Amount, Currency, Purpose,
                       AttachmentFileName, TravelType, FromPlace, ToPlace, TravelDate
                FROM dbo.SalesDailyExpense
                WHERE ActivityId = @ActivityId
                ORDER BY ExpenseDate DESC, ExpenseId DESC;", con))
            {
                cmd.Parameters.AddWithValue("@ActivityId", activityId);
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }

            rptExpenses.DataSource = dt;
            rptExpenses.DataBind();

            UpdateExpenseTotals(dt);
            return dt;
        }

        protected DataTable LoadExpenseForActivity_06Nov25_V1(string activityIdStr)
        {
            int activityId = 0;
            int.TryParse(activityIdStr, out activityId);

            DataTable dt = new DataTable();
            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand(@"
                SELECT ExpenseId, ActivityId, ExpenseType, ExpenseDate, FromDate, ToDate, BillNo, Amount, Currency, Purpose,
                       AttachmentFileName, TravelType, FromPlace, ToPlace, TravelDate
                FROM dbo.SalesDailyExpense
                WHERE ActivityId = @ActivityId
                ORDER BY ExpenseDate DESC, ExpenseId DESC
            ", con))
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("@ActivityId", activityId);
                da.Fill(dt);


            }

            rptExpenses.DataSource = dt;
            rptExpenses.DataBind();

            UpdateExpenseTotals(dt);

            // reset modal inputs for add
            HF_ActivityId.Value = activityId.ToString();
            HF_ActivityDate.Value = HF_ActivityDate.Value; // keep existing
            //if (!string.IsNullOrEmpty(HF_ActivityDate.Value))
            //{
            //    DateTime activityDate;
            //    if (DateTime.TryParse(HF_ActivityDate.Value, out activityDate))
            //    {
            //        TXT_TravelDate.Text = activityDate.ToString("dd-MM-yyyy");
            //    }
            //    else
            //    {
            //        TXT_TravelDate.Text = string.Empty; // optional: handle invalid date
            //    }
            //}
            //DDL_ExpenseType.SelectedValue = "DA";
            // Clear travel fields
            DDL_TravelType.SelectedIndex = 0;
            TXT_FromPlace.Text = TXT_ToPlace.Text = TXT_TravelPurpose.Text = TXT_TravelInvoiceNo.Text = "";
            CHK_TravelInvoice.Checked = false;
            TXT_DAAmount.Text = TXT_HotelAmount.Text = TXT_HotelBillNo.Text = TXT_HotelPurpose.Text = "";

            // show modal if caller expects it
            ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);

            return dt;
        }
        protected DataTable LoadExpenseForActivity_06Nov25_V2(string activityIdStr)
        {
            int activityId = 0;
            int.TryParse(activityIdStr, out activityId);

            DataTable dt = new DataTable();
            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand(@"
                SELECT ExpenseId, ActivityId, ExpenseType, ExpenseDate, FromDate, ToDate, BillNo, Amount, Currency, Purpose,
                       AttachmentFileName, AttachmentPath, TravelType, FromPlace, ToPlace, TravelDate,
                       PolicyMode, PolicySnapshot, IsAutoDA, ClaimStatus
                FROM dbo.SalesDailyExpense
                WHERE ActivityId = @ActivityId
                ORDER BY ExpenseDate DESC, ExpenseId DESC
            ", con))
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.Add(new SqlParameter("@ActivityId", SqlDbType.Int) { Value = activityId });
                da.Fill(dt);
            }

            // --- parse PolicySnapshot JSON and populate EXPENSE-LEVEL hidden fields (no conflict with employee policy) ---
            try
            {
                HF_SnapPolicyMode.Value = "";
                HF_SnapResolvedDA.Value = "";
                HF_SnapHotelCap.Value = "";
                HF_SnapOutstationDA.Value = "";
                HF_SnapOwnArrangementDA.Value = "";

                if (dt != null && dt.Rows.Count > 0)
                {
                    var snapRow = dt.AsEnumerable()
                                    .FirstOrDefault(r => dt.Columns.Contains("PolicySnapshot") && r["PolicySnapshot"] != DBNull.Value
                                                         && !string.IsNullOrWhiteSpace(Convert.ToString(r["PolicySnapshot"])));
                    if (snapRow != null)
                    {
                        string json = Convert.ToString(snapRow["PolicySnapshot"]);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            try
                            {
                                var jObj = JObject.Parse(json);
                                HF_SnapPolicyMode.Value = (string)jObj["PolicyMode"] ?? string.Empty;
                                HF_SnapResolvedDA.Value = (string)jObj["ResolvedDA"] ?? string.Empty;
                                HF_SnapHotelCap.Value = (string)jObj["HotelCap"] ?? string.Empty;
                                HF_SnapOutstationDA.Value = (string)jObj["OutstationDA"] ?? string.Empty;
                                HF_SnapOwnArrangementDA.Value = (string)jObj["OwnArrangementDA"] ?? string.Empty;
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Trace.TraceWarning("PolicySnapshot parse failed: " + ex.Message);
                            }
                        }
                    }
                    else if (dt.Columns.Contains("PolicyMode") && dt.Rows[0]["PolicyMode"] != DBNull.Value)
                    {
                        HF_SnapPolicyMode.Value = dt.Rows[0]["PolicyMode"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning("LoadExpenseForActivity snapshot population error: " + ex.Message);
            }

            // bind and recalc totals
            rptExpenses.DataSource = dt;
            rptExpenses.DataBind();
            UpdateExpenseTotals(dt);

            HF_ActivityId.Value = activityId.ToString();

            // clear add-entry fields (not the hidden snapshot)
            if (DDL_TravelType != null) DDL_TravelType.SelectedIndex = 0;
            TXT_FromPlace.Text = TXT_ToPlace.Text = TXT_TravelPurpose.Text = TXT_TravelInvoiceNo.Text = "";
            CHK_TravelInvoice.Checked = false;
            TXT_DAAmount.Text = TXT_HotelAmount.Text = TXT_HotelBillNo.Text = TXT_HotelPurpose.Text = "";

            // show modal with unique script key
            string scriptKey = "showExpense_" + activityId + "_" + Guid.NewGuid().ToString("N");
            ScriptManager.RegisterStartupScript(this, this.GetType(), scriptKey, "$('#expenseModal').modal('show');", true);

            return dt;
        }

        protected DataTable LoadExpenseForActivity(string activityIdStr)
        {
            int activityId = 0;
            int.TryParse(activityIdStr, out activityId);

            DataTable dt = new DataTable();
            using (var con = new SqlConnection(connString))
            using (var cmd = new SqlCommand(@"
                SELECT ExpenseId, ActivityId, ExpenseType, ExpenseDate, FromDate, ToDate, BillNo, Amount, Currency, Purpose,
                       AttachmentFileName, AttachmentPath, TravelType, FromPlace, ToPlace, TravelDate,
                       PolicyMode, PolicySnapshot, IsAutoDA, ClaimStatus
                FROM dbo.SalesDailyExpense
                WHERE ActivityId = @ActivityId
                ORDER BY ExpenseDate DESC, ExpenseId DESC
            ", con))
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.Add(new SqlParameter("@ActivityId", SqlDbType.Int) { Value = activityId });
                da.Fill(dt);
            }

            // reset snapshot HFs (expense-level)
            HF_SnapPolicyMode.Value = "";
            HF_SnapResolvedDA.Value = "";
            HF_SnapHotelCap.Value = "";
            HF_SnapOutstationDA.Value = "";
            HF_SnapOwnArrangementDA.Value = "";

            // If rows exist, find first usable JSON snapshot and populate HF_Snap* from it
            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow r in dt.Rows)
                {
                    if (!dt.Columns.Contains("PolicySnapshot") || r["PolicySnapshot"] == DBNull.Value) continue;

                    string json = Convert.ToString(r["PolicySnapshot"]).Trim();
                    if (string.IsNullOrWhiteSpace(json)) continue;

                    // Only attempt JSON parse (assumes migration done)
                    if (json.StartsWith("{"))
                    {
                        try
                        {
                            var jObj = JObject.Parse(json);

                            HF_SnapPolicyMode.Value = (string)jObj["PolicyMode"] ?? string.Empty;
                            HF_SnapResolvedDA.Value = (string)jObj["ResolvedDA"] ?? string.Empty;
                            HF_SnapHotelCap.Value = (string)jObj["HotelCap"] ?? string.Empty;
                            HF_SnapOutstationDA.Value = (string)jObj["OutstationDA"] ?? string.Empty;
                            HF_SnapOwnArrangementDA.Value = (string)jObj["OwnArrangementDA"] ?? string.Empty;

                            // found a good JSON snapshot — stop searching
                            break;
                        }
                        catch (Exception ex)
                        {
                            // parse failed for this row - log and continue searching other rows (defensive)
                            System.Diagnostics.Trace.TraceWarning("PolicySnapshot JSON parse failed for ExpenseId "
                                + (r.Table.Columns.Contains("ExpenseId") ? r["ExpenseId"].ToString() : "?")
                                + ": " + ex.Message);
                            // continue to next row
                        }
                    }
                }

                //HF_SnapPolicyMode.Value = "Daily";
                ApplyPolicyMode_ClientCall(HF_SnapPolicyMode.Value.ToString());

                // if still empty, fallback to PolicyMode column of first row
                if (string.IsNullOrWhiteSpace(HF_SnapPolicyMode.Value) && dt.Columns.Contains("PolicyMode") && dt.Rows[0]["PolicyMode"] != DBNull.Value)
                    HF_SnapPolicyMode.Value = dt.Rows[0]["PolicyMode"].ToString();
            }
            else
            {
                HF_SnapPolicyMode.Value = "HQ";
                ApplyPolicyMode_ClientCall("HQ");
            }

            // Bind repeater and totals from authoritative DB result
            rptExpenses.DataSource = dt;
            rptExpenses.DataBind();

            UpdateExpenseTotals(dt);

            // set HF_ActivityId and preserve HF_ActivityDate (caller sets HF_ActivityDate)
            HF_ActivityId.Value = activityId.ToString();

            // Clear only the add-entry inputs (do not clear snapshot HFs or existing attachments)
            if (DDL_TravelType != null) DDL_TravelType.SelectedIndex = 0;
            TXT_FromPlace.Text = TXT_ToPlace.Text = TXT_TravelPurpose.Text = TXT_TravelInvoiceNo.Text = "";
            CHK_TravelInvoice.Checked = false;
            TXT_DAAmount.Text = TXT_HotelAmount.Text = TXT_HotelBillNo.Text = TXT_HotelPurpose.Text = "";

            // Show modal (unique key to ensure script runs)
            string scriptKey = "showExpense_" + activityId + "_" + Guid.NewGuid().ToString("N");
            ScriptManager.RegisterStartupScript(this, this.GetType(), scriptKey, "$('#expenseModal').modal('show');", true);

            return dt;
        }


        private void UpdateExpenseTotals(DataTable dt)
        {
            decimal daTotal = 0m;
            decimal hotelTotal = 0m;
            decimal otherTotal = 0m;

            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow r in dt.Rows)
                {
                    decimal amt = 0m;
                    if (r["Amount"] != DBNull.Value)
                        amt = Convert.ToDecimal(r["Amount"]);

                    string type = (r["ExpenseType"] ?? "").ToString().Trim();

                    if (string.Equals(type, "DA", StringComparison.OrdinalIgnoreCase))
                        daTotal += amt;
                    else if (string.Equals(type, "Hotel", StringComparison.OrdinalIgnoreCase))
                        hotelTotal += amt;
                    else
                        otherTotal += amt;
                }
            }

            decimal overall = daTotal + hotelTotal + otherTotal;

            lblTotalDA.Text = daTotal.ToString("0.##");
            lblTotalHotel.Text = hotelTotal.ToString("0.##");
            lblTotalOther.Text = otherTotal.ToString("0.##");
            lblOverallTotal.Text = overall.ToString("0.##");
        }



        private string GetCurrentUserLoginId(bool redirectIfMissing = true)
        {
            try
            {
                // Primary: session value (your app uses this)
                if (Session != null && Session["WORKMAN"] != null)
                {
                    string s = Session["WORKMAN"].ToString();
                    if (!string.IsNullOrWhiteSpace(s)) return s;
                }

                // Not found
                if (redirectIfMissing)
                {
                    // You can change the login URL if your app uses a different path
                    string loginUrl = ResolveUrl("~/Login.aspx");
                    // Preserve return url to come back after login
                    string returnUrl = HttpContext.Current.Request.RawUrl;
                    HttpContext.Current.Response.Redirect(loginUrl + "?ReturnUrl=" + HttpUtility.UrlEncode(returnUrl), true);
                }
            }
            catch (Exception ex)
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyError6", $"showPNotify('Error', '{ex.Message.Replace("'", "\\'")}', 'error');", true);
                // swallow and return null
            }

            return null;
        }

        protected void rptExpenses_ItemCommand_OLD(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "DeleteExpense") return;

            // CommandArgument = "ExpenseId|AttachmentFileName|ActivityId"
            var arg = (e.CommandArgument ?? "").ToString();
            var parts = arg.Split(new[] { '|' }, 3);
            if (parts.Length < 1) return;

            int expenseId;
            if (!int.TryParse(parts[0], out expenseId))
            {
                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                LBL_ExpenseMsg.Text = "Invalid expense id.";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                return;
            }

            string attachmentPath = parts.Length > 1 ? parts[1] : null;
            int activityId = 0;
            int.TryParse(parts.Length > 2 ? parts[2] : null, out activityId);

            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();

                    // 1) Check business rule: allow delete only when parent activity is Draft (adjust as needed)
                    string claimStatus = null;
                    using (SqlCommand chk = new SqlCommand(@"SELECT a.ClaimStatus FROM dbo.SalesDailyExpense e INNER JOIN dbo.SalesDailyActivity a ON e.ActivityId = a.ActivityId WHERE e.ExpenseId = @ExpenseId", con))
                    {
                        chk.Parameters.AddWithValue("@ExpenseId", expenseId);
                        object o = chk.ExecuteScalar();
                        claimStatus = o == DBNull.Value || o == null ? null : Convert.ToString(o);
                    }

                    if (!string.IsNullOrEmpty(claimStatus) && !string.Equals(claimStatus, "Draft", StringComparison.OrdinalIgnoreCase))
                    {
                        LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Orange;
                        LBL_ExpenseMsg.Text = "Cannot delete expense once activity is submitted/approved.";
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                        return;
                    }

                    // 2) Delete record inside a transaction (so file deletion and DB delete can be atomic-ish)
                    using (SqlTransaction tx = con.BeginTransaction())
                    {
                        try
                        {
                            using (SqlCommand del = new SqlCommand("DELETE FROM dbo.SalesDailyExpense WHERE ExpenseId = @ExpenseId", con, tx))
                            {
                                del.Parameters.AddWithValue("@ExpenseId", expenseId);
                                int rows = del.ExecuteNonQuery();
                                if (rows <= 0)
                                {
                                    tx.Rollback();
                                    LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                                    LBL_ExpenseMsg.Text = "Expense not found or already removed.";
                                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                                    return;
                                }
                            }

                            // commit DB first
                            tx.Commit();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    } // transaction disposed

                    // 3) Delete file from filesystem if present and was stored as relative path
                    if (!string.IsNullOrWhiteSpace(attachmentPath))
                    {
                        try
                        {
                            // Normalize path: attachmentPath is likely like "UploadedFiles/ClaimsAttachment/2025/09/xxx.pdf"
                            string virtualPath = "~/" + attachmentPath.TrimStart('~', '/', '\\');
                            string fullPath = Server.MapPath(virtualPath);

                            if (System.IO.File.Exists(fullPath))
                            {
                                System.IO.File.Delete(fullPath);
                            }
                        }
                        catch (Exception ex)
                        {
                            // file deletion failure is non-fatal; log and continue
                            // TODO: replace with your logging mechanism
                            System.Diagnostics.Trace.TraceWarning("Failed to delete expense attachment file: " + ex.Message);
                        }
                    }

                } // using connection

                // refresh UI
                if (activityId == 0)
                {
                    // if ActivityId not included in CommandArgument (defensive), try to reload from DB using expenseId (optional)
                    // but we earlier selected ActivityId in the markup, so normally activityId will be present.
                }

                LoadExpenseForActivity(activityId.ToString()); // rebind the repeater
                BindGrid(false); // refresh grid totals/statuses
                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Green;
                LBL_ExpenseMsg.Text = "Expense deleted.";
            }
            catch (SqlException sqx)
            {
                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                LBL_ExpenseMsg.Text = sqx.Message;
            }
            catch (Exception ex)
            {
                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                LBL_ExpenseMsg.Text = ex.Message;
            }

            // show modal again so user sees updated list & message
            ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
        }
        protected void rptExpenses_ItemCommand_06Nov25(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "DeleteExpense") return;

            // CommandArgument (optional): "ExpenseId|AttachmentFileName|ActivityId"
            var arg = (e.CommandArgument ?? "").ToString();
            var parts = arg.Split(new[] { '|' }, 3);

            int expenseId;
            if (parts.Length < 1 || !int.TryParse(parts[0], out expenseId))
            {
                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                LBL_ExpenseMsg.Text = "Invalid expense id.";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                return;
            }

            // Try get attachmentPath and activityId from Repeater CommandArgument if provided.
            string attachmentPath = parts.Length > 1 ? (string.IsNullOrWhiteSpace(parts[1]) ? null : parts[1]) : null;
            int activityId = 0;
            if (parts.Length > 2) int.TryParse(parts[2], out activityId);

            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    con.Open();

                    // If we don't have activityId or attachmentPath, fetch the current values from DB before delete
                    using (var lookup = new SqlCommand(@"
                SELECT e.ActivityId, a.ClaimStatus, e.AttachmentFileName
                FROM dbo.SalesDailyExpense e
                LEFT JOIN dbo.SalesDailyActivity a ON e.ActivityId = a.ActivityId
                WHERE e.ExpenseId = @ExpenseId", con))
                    {
                        lookup.Parameters.AddWithValue("@ExpenseId", expenseId);
                        using (var rdr = lookup.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                if (activityId == 0 && rdr["ActivityId"] != DBNull.Value)
                                    activityId = Convert.ToInt32(rdr["ActivityId"]);

                                if (string.IsNullOrWhiteSpace(attachmentPath) && rdr["AttachmentFileName"] != DBNull.Value)
                                    attachmentPath = Convert.ToString(rdr["AttachmentFileName"]);

                                // claimStatus may be null
                                string claimStatus = rdr["ClaimStatus"] != DBNull.Value ? Convert.ToString(rdr["ClaimStatus"]) : null;

                                // Business rule: prevent delete if parent activity is not Draft
                                if (!string.IsNullOrEmpty(claimStatus) && !string.Equals(claimStatus, "Draft", StringComparison.OrdinalIgnoreCase))
                                {
                                    LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Orange;
                                    LBL_ExpenseMsg.Text = "Cannot delete expense once activity is submitted/approved.";
                                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                                    return;
                                }
                            }
                            else
                            {
                                // expense not found
                                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                                LBL_ExpenseMsg.Text = "Expense not found.";
                                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                                return;
                            }
                        } // rdr
                    } // lookup

                    // Delete inside a transaction for DB integrity
                    using (SqlTransaction tx = con.BeginTransaction())
                    using (SqlCommand del = new SqlCommand("DELETE FROM dbo.SalesDailyExpense WHERE ExpenseId = @ExpenseId", con, tx))
                    {
                        del.Parameters.AddWithValue("@ExpenseId", expenseId);
                        int rows = 0;
                        try
                        {
                            rows = del.ExecuteNonQuery();
                            if (rows <= 0)
                            {
                                tx.Rollback();
                                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                                LBL_ExpenseMsg.Text = "Expense not found or already removed.";
                                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                                return;
                            }

                            tx.Commit();
                        }
                        catch
                        {
                            try { tx.Rollback(); } catch { /* ignore */ }
                            throw;
                        }
                    } // transaction

                    // After DB delete, delete the file from disk if path is known
                    if (!string.IsNullOrWhiteSpace(attachmentPath))
                    {
                        try
                        {
                            // normalize to virtual path (attachmentPath likely "UploadedFiles/ClaimsAttachment/..." or with leading '/')
                            string cleaned = attachmentPath.TrimStart('~', '/', '\\');
                            string virtualPath = "~/" + cleaned;
                            string fullPath = Server.MapPath(virtualPath);

                            if (System.IO.File.Exists(fullPath))
                                System.IO.File.Delete(fullPath);
                        }
                        catch (Exception ex)
                        {
                            // non-fatal - log and continue
                            System.Diagnostics.Trace.TraceWarning("Failed to delete expense attachment file: " + ex.Message);
                        }
                    }
                } // connection closed

                // Refresh UI: prefer to use the activityId we found; if still zero fallback to HF_ActivityId
                if (activityId == 0)
                {
                    int tmpAct = 0;
                    if (int.TryParse(HF_ActivityId.Value, out tmpAct)) activityId = tmpAct;
                }

                LoadExpenseForActivity(activityId.ToString());
                Session.Remove(SESSION_KEY_GV_ACTIVITIES);
                BindGrid(false);

                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Green;
                LBL_ExpenseMsg.Text = "Expense deleted.";
            }
            catch (SqlException sqx)
            {
                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                LBL_ExpenseMsg.Text = sqx.Message;

                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyError5", $"showPNotify('Error', '{sqx.Message.Replace("'", "\\'")}', 'error');", true);
            }
            catch (Exception ex)
            {
                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                LBL_ExpenseMsg.Text = ex.Message;

                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyError4", $"showPNotify('Error', '{ex.Message.Replace("'", "\\'")}', 'error');", true);
            }

            // show modal again so user sees updated list & message
            ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
        }
        protected void rptExpenses_ItemCommand_OLD17Nov25(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "DeleteExpense")
            {
                var arg = (e.CommandArgument ?? "").ToString();
                var parts = arg.Split(new[] { '|' }, 3);

                int expenseId;
                if (parts.Length < 1 || !int.TryParse(parts[0], out expenseId))
                {
                    LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                    LBL_ExpenseMsg.Text = "Invalid expense id.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }

                // optional incoming values (may be empty)
                string incomingAttachmentPath = parts.Length > 1 ? (string.IsNullOrWhiteSpace(parts[1]) ? null : parts[1]) : null;
                int activityIdFromArg = 0;
                if (parts.Length > 2) int.TryParse(parts[2], out activityIdFromArg);

                try
                {
                    using (SqlConnection con = new SqlConnection(connString))
                    {
                        con.Open();

                        // 1) Load authoritative expense row details (including dates, type, parent activity claim status)
                        string lookupSql = @"
                        SELECT e.ExpenseId, e.ActivityId, e.ExpenseType, e.ExpenseDate, e.FromDate, e.ToDate, e.AttachmentPath, e.IsAutoDA, e.ClaimStatus AS ExpenseClaimStatus, a.ClaimStatus AS ActivityClaimStatus
                        FROM dbo.SalesDailyExpense e LEFT JOIN dbo.SalesDailyActivity a ON e.ActivityId = a.ActivityId WHERE e.ExpenseId = @ExpenseId";

                        int activityId = 0;
                        string expenseType = null;
                        DateTime? expDate = null;
                        DateTime? fromDate = null;
                        DateTime? toDate = null;
                        string attachmentPath = incomingAttachmentPath;
                        bool isAutoRow = false;
                        string expenseClaimStatus = null;
                        string activityClaimStatus = null;

                        using (var lookup = new SqlCommand(lookupSql, con))
                        {
                            lookup.Parameters.Add(new SqlParameter("@ExpenseId", SqlDbType.Int) { Value = expenseId });

                            using (var rdr = lookup.ExecuteReader())
                            {
                                if (!rdr.Read())
                                {
                                    LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                                    LBL_ExpenseMsg.Text = "Expense not found.";
                                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                                    return;
                                }

                                activityId = rdr["ActivityId"] != DBNull.Value ? Convert.ToInt32(rdr["ActivityId"]) : 0;
                                expenseType = rdr["ExpenseType"] != DBNull.Value ? rdr["ExpenseType"].ToString() : null;

                                if (rdr["ExpenseDate"] != DBNull.Value) expDate = Convert.ToDateTime(rdr["ExpenseDate"]);
                                if (rdr["FromDate"] != DBNull.Value) fromDate = Convert.ToDateTime(rdr["FromDate"]);
                                if (rdr["ToDate"] != DBNull.Value) toDate = Convert.ToDateTime(rdr["ToDate"]);

                                if (string.IsNullOrWhiteSpace(attachmentPath) && rdr["AttachmentPath"] != DBNull.Value)
                                    attachmentPath = rdr["AttachmentPath"].ToString();

                                isAutoRow = rdr["IsAutoDA"] != DBNull.Value && Convert.ToInt32(rdr["IsAutoDA"]) == 1;
                                expenseClaimStatus = rdr["ExpenseClaimStatus"] != DBNull.Value ? rdr["ExpenseClaimStatus"].ToString() : null;
                                activityClaimStatus = rdr["ActivityClaimStatus"] != DBNull.Value ? rdr["ActivityClaimStatus"].ToString() : null;
                            }
                        }

                        // authoritative permissions: block deleting if parent activity is not Draft
                        if (!string.IsNullOrWhiteSpace(activityClaimStatus) && !activityClaimStatus.Equals("Draft", StringComparison.OrdinalIgnoreCase))
                        {
                            LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Orange;
                            LBL_ExpenseMsg.Text = "Cannot delete expense because parent activity is not in Draft state.";
                            ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                            return;
                        }

                        // block manual deletion of auto rows (users should not delete IsAutoDA rows directly)
                        if (isAutoRow)
                        {
                            LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Orange;
                            LBL_ExpenseMsg.Text = "Cannot delete system-generated (auto) expense directly.";
                            ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                            return;
                        }

                        // 2) Determine date range to match auto DA rows
                        DateTime matchFrom, matchTo;
                        if (expenseType != null && expenseType.Equals("Hotel", StringComparison.OrdinalIgnoreCase))
                        {
                            // Hotel: use FromDate..ToDate if available, else fall back to ExpenseDate
                            if (fromDate.HasValue && toDate.HasValue)
                            {
                                matchFrom = fromDate.Value.Date;
                                matchTo = toDate.Value.Date;
                            }
                            else if (expDate.HasValue)
                            {
                                matchFrom = matchTo = expDate.Value.Date;
                            }
                            else
                            {
                                // fallback: only exact match to null (no auto rows will match)
                                matchFrom = matchTo = DateTime.MinValue;
                            }
                        }
                        else if (expenseType != null && expenseType.Equals("Travel", StringComparison.OrdinalIgnoreCase))
                        {
                            // Travel: use ExpenseDate / TravelDate
                            if (expDate.HasValue)
                            {
                                matchFrom = matchTo = expDate.Value.Date;
                            }
                            else
                            {
                                matchFrom = matchTo = DateTime.MinValue;
                            }
                        }
                        else
                        {
                            // generic: use ExpenseDate
                            if (expDate.HasValue)
                            {
                                matchFrom = matchTo = expDate.Value.Date;
                            }
                            else
                            {
                                matchFrom = matchTo = DateTime.MinValue;
                            }
                        }

                        // 3) In a transaction: delete the requested expense row AND any matching auto-DA rows for same activity/date-range
                        using (SqlTransaction tx = con.BeginTransaction())
                        {
                            try
                            {
                                // a) collect auto-DA expenseIds to delete (if any)
                                var autoIds = new List<int>();
                                if (matchFrom != DateTime.MinValue)
                                {
                                    string findAutoSql = @"
                                    SELECT ExpenseId, AttachmentPath
                                    FROM dbo.SalesDailyExpense
                                    WHERE ActivityId = @ActivityId
                                      AND ExpenseType = 'DA'
                                      AND IsAutoDA = 1
                                      AND CAST(ExpenseDate AS date) BETWEEN @FromDate AND @ToDate";

                                    using (var findCmd = new SqlCommand(findAutoSql, con, tx))
                                    {
                                        findCmd.Parameters.Add(new SqlParameter("@ActivityId", SqlDbType.Int) { Value = activityId });
                                        findCmd.Parameters.Add(new SqlParameter("@FromDate", SqlDbType.Date) { Value = matchFrom });
                                        findCmd.Parameters.Add(new SqlParameter("@ToDate", SqlDbType.Date) { Value = matchTo });

                                        using (var rdr = findCmd.ExecuteReader())
                                        {
                                            while (rdr.Read())
                                            {
                                                if (rdr["ExpenseId"] != DBNull.Value)
                                                    autoIds.Add(Convert.ToInt32(rdr["ExpenseId"]));
                                            }
                                        }
                                    }
                                }

                                // b) delete primary expense
                                using (var del = new SqlCommand("DELETE FROM dbo.SalesDailyExpense WHERE ExpenseId = @ExpenseId", con, tx))
                                {
                                    del.Parameters.Add(new SqlParameter("@ExpenseId", SqlDbType.Int) { Value = expenseId });
                                    int affected = del.ExecuteNonQuery();
                                    if (affected <= 0)
                                    {
                                        tx.Rollback();
                                        LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                                        LBL_ExpenseMsg.Text = "Expense not found or already removed.";
                                        ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                                        return;
                                    }
                                }

                                // c) delete auto DA rows if found
                                if (autoIds.Count > 0)
                                {
                                    // Build parameterized IN delete (safe)
                                    string inList = string.Join(",", autoIds); // safe because these are ints from DB (already validated)
                                    string delAutoSql = $"DELETE FROM dbo.SalesDailyExpense WHERE ExpenseId IN ({inList})";
                                    using (var delAuto = new SqlCommand(delAutoSql, con, tx))
                                    {
                                        delAuto.ExecuteNonQuery();
                                    }
                                }

                                tx.Commit();

                                // 4) After DB commit, delete attachments for primary expense and auto rows (if any)
                                // Delete primary file
                                Action<string> tryDeleteFile = (virtualPath) =>
                                {
                                    try
                                    {
                                        if (string.IsNullOrWhiteSpace(virtualPath)) return;
                                        string cleaned = virtualPath.TrimStart('~', '/', '\\');
                                        string fullVirtual = "~/" + cleaned;
                                        string fullPath = Server.MapPath(fullVirtual);
                                        string allowedRoot = Server.MapPath("~/UploadedFiles/ClaimsAttachment/");

                                        if (!string.IsNullOrWhiteSpace(fullPath) && fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(fullPath))
                                            System.IO.File.Delete(fullPath);
                                    }
                                    catch (Exception exFile)
                                    {
                                        System.Diagnostics.Trace.TraceWarning("Failed to delete attachment: " + exFile.Message);
                                    }
                                };

                                // delete for primary
                                tryDeleteFile(attachmentPath);

                                // delete for auto rows attachments (fetch paths and delete)
                                if (autoIds.Count > 0)
                                {
                                    // fetch attachment paths
                                    string fetchPathsSql = $"SELECT AttachmentPath FROM dbo.SalesDailyExpense WHERE ExpenseId IN ({string.Join(",", autoIds)})";
                                    // Note: rows already deleted; better: we should have captured paths earlier.
                                    // To keep it robust, re-query the DB using OUTER TRACING earlier (but since we deleted, we didn't capture paths).
                                    // So safer approach: capture attachment paths when finding autoIds earlier.
                                    // For simplicity here: re-run a query before deletion step to capture paths — but in this snippet we kept it simple.
                                    // (Alternatively, you can capture attachment paths into a list when you populated autoIds.)
                                }

                                // Reload UI from DB (authoritative)
                                LoadExpenseForActivity(activityId.ToString());
                                HF_SnapPolicyMode.Value = "HQ";
                                //ApplyPolicyModeFromCodeBehind("Daily");
                                ApplyPolicyMode_ClientCall("HQ");
                                //ClickApplyButtonFromServer("Daily");
                                Session.Remove(SESSION_KEY_GV_ACTIVITIES);
                                BindGrid(false);

                                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Green;
                                LBL_ExpenseMsg.Text = "Expense deleted.";
                            }
                            catch (Exception ex)
                            {
                                try { tx.Rollback(); } catch { }
                                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                                LBL_ExpenseMsg.Text = ex.Message;
                                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                                return;
                            }
                        } // tx
                    } // con
                }
                catch (SqlException sqx)
                {
                    LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                    LBL_ExpenseMsg.Text = sqx.Message;
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                }
                catch (Exception ex)
                {
                    LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                    LBL_ExpenseMsg.Text = ex.Message;
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                }
            }

        }

        protected void rptExpenses_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "DeleteExpense")
            {
                var arg = (e.CommandArgument ?? "").ToString();
                var parts = arg.Split(new[] { '|' }, 3);

                int expenseId;
                if (parts.Length < 1 || !int.TryParse(parts[0], out expenseId))
                {
                    LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                    LBL_ExpenseMsg.Text = "Invalid expense id.";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                    return;
                }

                // optional incoming values (may be empty)
                string incomingAttachmentPath = parts.Length > 1 ? (string.IsNullOrWhiteSpace(parts[1]) ? null : parts[1]) : null;
                int activityIdFromArg = 0;
                if (parts.Length > 2) int.TryParse(parts[2], out activityIdFromArg);

                try
                {
                    using (SqlConnection con = new SqlConnection(connString))
                    {
                        con.Open();

                        // 1) Load authoritative expense row details (including dates, type, parent activity claim status)
                        string lookupSql = @"
                    SELECT e.ExpenseId, e.ActivityId, e.ExpenseType, e.ExpenseDate, e.FromDate, e.ToDate, e.AttachmentPath,
                           e.IsAutoDA, e.ClaimStatus AS ExpenseClaimStatus, a.ClaimStatus AS ActivityClaimStatus,
                           e.ExpenseSeq, e.CreatedDateTime
                    FROM dbo.SalesDailyExpense e
                    LEFT JOIN dbo.SalesDailyActivity a ON e.ActivityId = a.ActivityId
                    WHERE e.ExpenseId = @ExpenseId";

                        int activityId = 0;
                        string expenseType = null;
                        DateTime? expDate = null;
                        DateTime? fromDate = null;
                        DateTime? toDate = null;
                        string attachmentPath = incomingAttachmentPath;
                        bool isAutoRow = false;
                        string expenseClaimStatus = null;
                        string activityClaimStatus = null;
                        int? expenseSeq = null;
                        DateTime? expenseCreatedDate = null;

                        using (var lookup = new SqlCommand(lookupSql, con))
                        {
                            lookup.Parameters.Add(new SqlParameter("@ExpenseId", SqlDbType.Int) { Value = expenseId });

                            using (var rdr = lookup.ExecuteReader())
                            {
                                if (!rdr.Read())
                                {
                                    LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                                    LBL_ExpenseMsg.Text = "Expense not found.";
                                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                                    return;
                                }

                                activityId = rdr["ActivityId"] != DBNull.Value ? Convert.ToInt32(rdr["ActivityId"]) : 0;
                                expenseType = rdr["ExpenseType"] != DBNull.Value ? rdr["ExpenseType"].ToString() : null;

                                if (rdr["ExpenseDate"] != DBNull.Value) expDate = Convert.ToDateTime(rdr["ExpenseDate"]);
                                if (rdr["FromDate"] != DBNull.Value) fromDate = Convert.ToDateTime(rdr["FromDate"]);
                                if (rdr["ToDate"] != DBNull.Value) toDate = Convert.ToDateTime(rdr["ToDate"]);

                                if (string.IsNullOrWhiteSpace(attachmentPath) && rdr["AttachmentPath"] != DBNull.Value)
                                    attachmentPath = rdr["AttachmentPath"].ToString();

                                isAutoRow = rdr["IsAutoDA"] != DBNull.Value && Convert.ToInt32(rdr["IsAutoDA"]) == 1;
                                expenseClaimStatus = rdr["ExpenseClaimStatus"] != DBNull.Value ? rdr["ExpenseClaimStatus"].ToString() : null;
                                activityClaimStatus = rdr["ActivityClaimStatus"] != DBNull.Value ? rdr["ActivityClaimStatus"].ToString() : null;

                                if (rdr["ExpenseSeq"] != DBNull.Value)
                                    expenseSeq = Convert.ToInt32(rdr["ExpenseSeq"]);

                                if (rdr["CreatedDateTime"] != DBNull.Value)
                                    expenseCreatedDate = Convert.ToDateTime(rdr["CreatedDateTime"]);
                            }
                        }

                        // authoritative permissions: block deleting if parent activity is not Draft
                        if (!string.IsNullOrWhiteSpace(activityClaimStatus) && !activityClaimStatus.Equals("Draft", StringComparison.OrdinalIgnoreCase))
                        {
                            LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Orange;
                            LBL_ExpenseMsg.Text = "Cannot delete expense because parent activity is not in Draft state.";
                            ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                            return;
                        }

                        // block manual deletion of auto rows (users should not delete IsAutoDA rows directly)
                        if (isAutoRow)
                        {
                            LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Orange;
                            LBL_ExpenseMsg.Text = "Cannot delete system-generated (auto) expense directly.";
                            ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                            return;
                        }

                        // 2) Determine date range to match auto DA rows
                        DateTime matchFrom, matchTo;
                        if (expenseType != null && expenseType.Equals("Hotel", StringComparison.OrdinalIgnoreCase))
                        {
                            // Hotel: use FromDate..ToDate if available, else fall back to ExpenseDate
                            if (fromDate.HasValue && toDate.HasValue)
                            {
                                matchFrom = fromDate.Value.Date;
                                matchTo = toDate.Value.Date;
                            }
                            else if (expDate.HasValue)
                            {
                                matchFrom = matchTo = expDate.Value.Date;
                            }
                            else
                            {
                                // fallback: only exact match to null (no auto rows will match)
                                matchFrom = matchTo = DateTime.MinValue;
                            }
                        }
                        else if (expenseType != null && expenseType.Equals("Travel", StringComparison.OrdinalIgnoreCase))
                        {
                            // Travel: use ExpenseDate / TravelDate
                            if (expDate.HasValue)
                            {
                                matchFrom = matchTo = expDate.Value.Date;
                            }
                            else
                            {
                                matchFrom = matchTo = DateTime.MinValue;
                            }
                        }
                        else
                        {
                            // generic: use ExpenseDate
                            if (expDate.HasValue)
                            {
                                matchFrom = matchTo = expDate.Value.Date;
                            }
                            else
                            {
                                matchFrom = matchTo = DateTime.MinValue;
                            }
                        }

                        // 3) In a transaction: determine whether this expense is the FIRST for that activity+date+type,
                        //    collect auto-DA ids & attachment paths (if we will delete them), then delete.
                        using (SqlTransaction tx = con.BeginTransaction())
                        {
                            try
                            {
                                bool isFirstForType = false;

                                if (matchFrom != DateTime.MinValue)
                                {
                                    // Determine the earliest expense for this activity/date/type using ExpenseSeq (if present) then CreatedDateTime then ExpenseId
                                    string firstSql = @"
                                SELECT TOP 1 ExpenseId
                                FROM dbo.SalesDailyExpense WITH (UPDLOCK, HOLDLOCK)
                                WHERE ActivityId = @ActivityId
                                  AND LOWER(ISNULL(ExpenseType,'')) = LOWER(ISNULL(@ExpenseType,''))
                                  AND CAST(ISNULL(ExpenseDate, GETDATE()) AS DATE) = @MatchDate
                                ORDER BY 
                                  CASE WHEN ExpenseSeq IS NULL THEN 2147483647 ELSE ExpenseSeq END ASC,
                                  ISNULL(CreatedDateTime, '1900-01-01') ASC,
                                  ExpenseId ASC";

                                    using (var firstCmd = new SqlCommand(firstSql, con, tx))
                                    {
                                        firstCmd.Parameters.Add(new SqlParameter("@ActivityId", SqlDbType.Int) { Value = activityId });
                                        firstCmd.Parameters.Add(new SqlParameter("@ExpenseType", SqlDbType.NVarChar, 50) { Value = expenseType ?? (object)DBNull.Value });
                                        firstCmd.Parameters.Add(new SqlParameter("@MatchDate", SqlDbType.Date) { Value = matchFrom });

                                        object firstObj = firstCmd.ExecuteScalar();
                                        if (firstObj != null && firstObj != DBNull.Value)
                                        {
                                            int firstExpenseId = Convert.ToInt32(firstObj);
                                            isFirstForType = (firstExpenseId == expenseId);
                                        }
                                        else
                                        {
                                            // no other rows found; if this row exists then treat it as first
                                            isFirstForType = true;
                                        }
                                    }
                                }
                                else
                                {
                                    // no valid match date -> cannot reliably find related Auto-DA rows; conservative approach: treat as not-first
                                    isFirstForType = false;
                                }

                                // collect auto-DA rows (only if we will delete them)
                                var autoIds = new List<int>();
                                var autoPaths = new List<string>();

                                if (isFirstForType && matchFrom != DateTime.MinValue)
                                {
                                    string findAutoSql = @"
                                SELECT ExpenseId, AttachmentPath
                                FROM dbo.SalesDailyExpense WITH (UPDLOCK, HOLDLOCK)
                                WHERE ActivityId = @ActivityId
                                  AND ExpenseType = 'DA'
                                  AND IsAutoDA = 1
                                  AND CAST(ExpenseDate AS date) BETWEEN @FromDate AND @ToDate";

                                    using (var findCmd = new SqlCommand(findAutoSql, con, tx))
                                    {
                                        findCmd.Parameters.Add(new SqlParameter("@ActivityId", SqlDbType.Int) { Value = activityId });
                                        findCmd.Parameters.Add(new SqlParameter("@FromDate", SqlDbType.Date) { Value = matchFrom });
                                        findCmd.Parameters.Add(new SqlParameter("@ToDate", SqlDbType.Date) { Value = matchTo });

                                        using (var rdr = findCmd.ExecuteReader())
                                        {
                                            while (rdr.Read())
                                            {
                                                if (rdr["ExpenseId"] != DBNull.Value)
                                                    autoIds.Add(Convert.ToInt32(rdr["ExpenseId"]));
                                                if (rdr["AttachmentPath"] != DBNull.Value)
                                                    autoPaths.Add(rdr["AttachmentPath"].ToString());
                                            }
                                        }
                                    }
                                }

                                // delete primary expense
                                using (var del = new SqlCommand("DELETE FROM dbo.SalesDailyExpense WHERE ExpenseId = @ExpenseId", con, tx))
                                {
                                    del.Parameters.Add(new SqlParameter("@ExpenseId", SqlDbType.Int) { Value = expenseId });
                                    int affected = del.ExecuteNonQuery();
                                    if (affected <= 0)
                                    {
                                        tx.Rollback();
                                        LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                                        LBL_ExpenseMsg.Text = "Expense not found or already removed.";
                                        ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                                        return;
                                    }
                                }

                                // delete auto DA rows if found and if this was the first expense
                                if (autoIds.Count > 0)
                                {
                                    // build parameterized IN list safely
                                    string inList = string.Join(",", autoIds); // safe because values are from DB
                                    string delAutoSql = $"DELETE FROM dbo.SalesDailyExpense WHERE ExpenseId IN ({inList})";
                                    using (var delAuto = new SqlCommand(delAutoSql, con, tx))
                                    {
                                        delAuto.ExecuteNonQuery();
                                    }
                                }

                                tx.Commit();

                                // 4) After DB commit, delete attachments for primary expense and auto rows (if any)
                                Action<string> tryDeleteFile = (virtualPath) =>
                                {
                                    try
                                    {
                                        if (string.IsNullOrWhiteSpace(virtualPath)) return;
                                        string cleaned = virtualPath.TrimStart('~', '/', '\\');
                                        string fullVirtual = "~/" + cleaned;
                                        string fullPath = Server.MapPath(fullVirtual);
                                        string allowedRoot = Server.MapPath("~/UploadedFiles/ClaimsAttachment/");

                                        if (!string.IsNullOrWhiteSpace(fullPath) && fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(fullPath))
                                            System.IO.File.Delete(fullPath);
                                    }
                                    catch (Exception exFile)
                                    {
                                        System.Diagnostics.Trace.TraceWarning("Failed to delete attachment: " + exFile.Message);
                                    }
                                };

                                // delete for primary
                                tryDeleteFile(attachmentPath);

                                // delete for auto rows attachments collected earlier
                                foreach (var pth in autoPaths)
                                    tryDeleteFile(pth);

                                // Reload UI from DB (authoritative)
                                LoadExpenseForActivity(activityId.ToString());
                                HF_SnapPolicyMode.Value = "HQ";
                                ApplyPolicyMode_ClientCall("HQ");
                                Session.Remove(SESSION_KEY_GV_ACTIVITIES);
                                BindGrid(false);

                                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Green;
                                LBL_ExpenseMsg.Text = "Expense deleted.";
                            }
                            catch (Exception ex)
                            {
                                try { tx.Rollback(); } catch { }
                                LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                                LBL_ExpenseMsg.Text = ex.Message;
                                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                                return;
                            }
                        } // tx
                    } // con
                }
                catch (SqlException sqx)
                {
                    LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                    LBL_ExpenseMsg.Text = sqx.Message;
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                }
                catch (Exception ex)
                {
                    LBL_ExpenseMsg.ForeColor = System.Drawing.Color.Red;
                    LBL_ExpenseMsg.Text = ex.Message;
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
                }
            }
        }


        //--------------------Polcy in Runtime--------------//
        public class PolicyResolved
        {
            public string Source { get; set; }            // 'EX' or 'MX'
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
            // Resolved numeric fields (from stored proc)
            public decimal? Resolved_MaxKM { get; set; }
            public decimal? Resolved_RatePerKm { get; set; }
            public decimal? Resolved_DA { get; set; }
            public decimal? Resolved_HotelCap { get; set; }
            public decimal? Resolved_OutstationDA { get; set; }
            public decimal? Resolved_OwnAllowance { get; set; }
            public string Resolved_Remarks { get; set; }
        }

        // Example: call this from Page_Load or wherever you need policy
        //protected void Page_Load(object sender, EventArgs e)
        //{
        //    if (!IsPostBack)
        //    {
        //        // Example values — replace with actual logged-in user's context
        //        string employeeCode = GetLoggedInEmployeeCode(); // implement as per your auth
        //        int? regionId = GetLoggedInRegionId();
        //        int? branchId = GetLoggedInBranchId();
        //        int? gradeId = GetLoggedInGradeId();
        //        int? designationId = GetLoggedInDesignationId();

        //        var policy = GetResolvedPolicy(employeeCode, regionId, branchId, gradeId, designationId);
        //        if (policy != null)
        //        {
        //            // Use the policy values in UI or server-side validation
        //            // Example: display notification
        //            ShowPolicyNotification(policy);
        //        }
        //    }
        //}

        private PolicyResolved GetResolvedPolicy_OLD(string employeeCode, int? regionId, int? branchId, int? gradeId, int? designationId)
        {
            var dto = (PolicyResolved)null;

            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (var con = new SqlConnection(connStr))
            using (var cmd = new SqlCommand("dbo.usp_GetResolvedPolicy", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@EmployeeCode", (object)employeeCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RegionId", (object)regionId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BranchId", (object)branchId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@GradeId", (object)gradeId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DesignationId", (object)designationId ?? DBNull.Value);

                // AsOfDate: Use server UTC -> IST conversion to provide consistent business date
                DateTime istDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "India Standard Time").Date;
                cmd.Parameters.AddWithValue("@AsOfDate", istDate);

                con.Open();
                using (var reader = cmd.ExecuteReader(CommandBehavior.SingleResult))
                {
                    if (reader.Read())
                    {
                        dto = new PolicyResolved();
                        dto.Source = reader["Source"] as string;
                        dto.RowId = reader["RowId"] != DBNull.Value ? (int?)Convert.ToInt32(reader["RowId"]) : null;
                        dto.EmployeeCode = reader["EmployeeCode"] != DBNull.Value ? reader["EmployeeCode"].ToString() : null;
                        dto.RegionId = reader["RegionId"] != DBNull.Value ? (int?)Convert.ToInt32(reader["RegionId"]) : null;
                        dto.BranchId = reader["BranchId"] != DBNull.Value ? (int?)Convert.ToInt32(reader["BranchId"]) : null;
                        dto.GradeId = reader["GradeId"] != DBNull.Value ? (int?)Convert.ToInt32(reader["GradeId"]) : null;
                        dto.DesignationId = reader["DesignationId"] != DBNull.Value ? (int?)Convert.ToInt32(reader["DesignationId"]) : null;
                        dto.PolicyJson = reader["PolicyJson"] != DBNull.Value ? reader["PolicyJson"].ToString() : null;
                        dto.Priority = reader["Priority"] != DBNull.Value ? (int?)Convert.ToInt32(reader["Priority"]) : null;
                        dto.EffectiveFrom = reader["EffectiveFrom"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["EffectiveFrom"]) : null;
                        dto.EffectiveTo = reader["EffectiveTo"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["EffectiveTo"]) : null;
                        dto.IsActive = reader["IsActive"] != DBNull.Value ? (bool?)Convert.ToBoolean(reader["IsActive"]) : null;
                        dto.Specificity = reader["Specificity"] != DBNull.Value ? (int?)Convert.ToInt32(reader["Specificity"]) : null;

                        // parsed columns from the proc (try reading typed columns first)
                        dto.Resolved_MaxKM = reader["Resolved_MaxKM"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_MaxKM"]) : null;
                        dto.Resolved_RatePerKm = reader["Resolved_RatePerKm"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_RatePerKm"]) : null;
                        dto.Resolved_DA = reader["Resolved_DA"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_DA"]) : null;
                        dto.Resolved_HotelCap = reader["Resolved_HotelCap"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_HotelCap"]) : null;
                        dto.Resolved_Remarks = reader["Resolved_Remarks"] != DBNull.Value ? reader["Resolved_Remarks"].ToString() : null;

                        // If the proc didn't return parsed columns or they are null, optionally parse PolicyJson
                        if ((dto.Resolved_MaxKM == null && dto.Resolved_RatePerKm == null && dto.Resolved_DA == null) && !string.IsNullOrEmpty(dto.PolicyJson))
                        {
                            TryParseJsonFallback(dto);
                        }
                    }
                }
            }

            return dto;
        }
        private PolicyResolved GetResolvedPolicy_OLD2(string employeeCode, int? regionId, int? branchId, int? gradeId, int? designationId)
        {
            PolicyResolved dto = null;

            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (var con = new SqlConnection(connStr))
            using (var cmd = new SqlCommand("dbo.usp_GetResolvedPolicy", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                // optional: increase timeout if proc may take longer
                // cmd.CommandTimeout = 60;

                // Required param
                cmd.Parameters.Add("@EmployeeCode", SqlDbType.VarChar, 50).Value = (object)employeeCode ?? DBNull.Value;

                // IMPORTANT: stored proc expects VARCHAR inputs (can be PK or custom-code). Convert ints to strings.
                cmd.Parameters.Add("@RegionIdParam", SqlDbType.VarChar, 200).Value = regionId.HasValue ? (object)regionId.Value.ToString() : DBNull.Value;
                cmd.Parameters.Add("@BranchIdParam", SqlDbType.VarChar, 200).Value = branchId.HasValue ? (object)branchId.Value.ToString() : DBNull.Value;
                cmd.Parameters.Add("@GradeIdParam", SqlDbType.VarChar, 200).Value = gradeId.HasValue ? (object)gradeId.Value.ToString() : DBNull.Value;
                cmd.Parameters.Add("@DesignationIdParam", SqlDbType.VarChar, 200).Value = designationId.HasValue ? (object)designationId.Value.ToString() : DBNull.Value;

                // NOTE: if you later add an @AsOfDate (or similar) parameter to the stored proc, add it here:
                // DateTime istDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "India Standard Time").Date;
                // cmd.Parameters.Add("@AsOfDate", SqlDbType.Date).Value = istDate;

                con.Open();
                using (var reader = cmd.ExecuteReader(CommandBehavior.SingleResult))
                {
                    if (reader.Read())
                    {
                        dto = new PolicyResolved();

                        // helper to safely check column existence
                        Func<IDataRecord, string, bool> ColExists = (rec, colName) =>
                        {
                            try
                            {
                                return rec.GetOrdinal(colName) >= 0;
                            }
                            catch (IndexOutOfRangeException)
                            {
                                return false;
                            }
                        };

                        // Safe reads: only read if column exists
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

                        // Resolved typed columns (optional — the proc may or may not return these)
                        if (ColExists(reader, "Resolved_MaxKM")) dto.Resolved_MaxKM = reader["Resolved_MaxKM"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_MaxKM"]) : null;
                        if (ColExists(reader, "Resolved_RatePerKm")) dto.Resolved_RatePerKm = reader["Resolved_RatePerKm"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_RatePerKm"]) : null;
                        if (ColExists(reader, "Resolved_DA")) dto.Resolved_DA = reader["Resolved_DA"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_DA"]) : null;
                        if (ColExists(reader, "Resolved_HotelCap")) dto.Resolved_HotelCap = reader["Resolved_HotelCap"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_HotelCap"]) : null;
                        if (ColExists(reader, "Resolved_Remarks")) dto.Resolved_Remarks = reader["Resolved_Remarks"] != DBNull.Value ? reader["Resolved_Remarks"].ToString() : null;

                        // If the proc didn't return parsed columns or they are null, optionally parse PolicyJson
                        if ((dto.Resolved_MaxKM == null && dto.Resolved_RatePerKm == null && dto.Resolved_DA == null) && !string.IsNullOrEmpty(dto.PolicyJson))
                        {
                            TryParseJsonFallback(dto);
                        }
                    }
                }
            }

            return dto;
        }
        private PolicyResolved GetResolvedPolicy_OLD3(string employeeCode, int? regionId, int? branchId, int? gradeId, int? designationId)
        {
            PolicyResolved dto = null;

            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (var con = new SqlConnection(connStr))
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

                        Func<IDataRecord, string, bool> ColExists = (rec, colName) =>
                        {
                            try { return rec.GetOrdinal(colName) >= 0; }
                            catch (IndexOutOfRangeException) { return false; }
                        };

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

                        // Resolved typed columns (the proc may or may not return these)
                        if (ColExists(reader, "Resolved_MaxKM")) dto.Resolved_MaxKM = reader["Resolved_MaxKM"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_MaxKM"]) : null;
                        if (ColExists(reader, "Resolved_RatePerKm")) dto.Resolved_RatePerKm = reader["Resolved_RatePerKm"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_RatePerKm"]) : null;
                        if (ColExists(reader, "Resolved_DA")) dto.Resolved_DA = reader["Resolved_DA"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_DA"]) : null;
                        if (ColExists(reader, "Resolved_HotelCap")) dto.Resolved_HotelCap = reader["Resolved_HotelCap"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_HotelCap"]) : null;

                        // NEW: optional resolved values from SP
                        if (ColExists(reader, "Resolved_OutstationDA")) dto.Resolved_OutstationDA = reader["Resolved_OutstationDA"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_OutstationDA"]) : null;
                        if (ColExists(reader, "Resolved_OwnAllowance")) dto.Resolved_OwnAllowance = reader["Resolved_OwnAllowance"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_OwnAllowance"]) : null;

                        if (ColExists(reader, "Resolved_Remarks")) dto.Resolved_Remarks = reader["Resolved_Remarks"] != DBNull.Value ? reader["Resolved_Remarks"].ToString() : null;

                        // If some resolved values are missing, parse PolicyJson for fallback
                        if ((dto.Resolved_MaxKM == null && dto.Resolved_RatePerKm == null && dto.Resolved_DA == null && dto.Resolved_OutstationDA == null && dto.Resolved_OwnAllowance == null)
                            && !string.IsNullOrEmpty(dto.PolicyJson))
                        {
                            TryParseJsonFallback(dto);
                        }
                        else
                        {
                            // Even if some are present, ensure any other missing ones are attempted from JSON
                            TryParseJsonFallback(dto);
                        }
                    }
                }
            }

            return dto;
        }

        private PolicyResolved GetResolvedPolicy(string employeeCode, int? regionId, int? branchId, int? gradeId, int? designationId)
        {
            PolicyResolved dto = null;

            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (var con = new SqlConnection(connStr))
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

                        Func<IDataRecord, string, bool> ColExists = (rec, colName) =>
                        {
                            try { return rec.GetOrdinal(colName) >= 0; }
                            catch (IndexOutOfRangeException) { return false; }
                        };

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

                        // Resolved typed columns (new + existing)
                        if (ColExists(reader, "Resolved_MaxKM")) dto.Resolved_MaxKM = reader["Resolved_MaxKM"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_MaxKM"]) : null;
                        if (ColExists(reader, "Resolved_RatePerKm")) dto.Resolved_RatePerKm = reader["Resolved_RatePerKm"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_RatePerKm"]) : null;
                        if (ColExists(reader, "Resolved_DA")) dto.Resolved_DA = reader["Resolved_DA"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_DA"]) : null;
                        if (ColExists(reader, "Resolved_HotelCap")) dto.Resolved_HotelCap = reader["Resolved_HotelCap"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_HotelCap"]) : null;

                        // NEW: Outstation & OwnArrangement values (optional cols)
                        if (ColExists(reader, "Resolved_OutstationDA")) dto.Resolved_OutstationDA = reader["Resolved_OutstationDA"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_OutstationDA"]) : null;
                        if (ColExists(reader, "Resolved_OwnAllowance")) dto.Resolved_OwnAllowance = reader["Resolved_OwnAllowance"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["Resolved_OwnAllowance"]) : null;

                        if (ColExists(reader, "Resolved_Remarks")) dto.Resolved_Remarks = reader["Resolved_Remarks"] != DBNull.Value ? reader["Resolved_Remarks"].ToString() : null;

                        // If core resolved numeric values are missing, try JSON fallback (will also look for OutstationDA / OwnArrangementAllowance)
                        if ((dto.Resolved_MaxKM == null && dto.Resolved_RatePerKm == null && dto.Resolved_DA == null && dto.Resolved_OutstationDA == null && dto.Resolved_OwnAllowance == null)
                            && !string.IsNullOrEmpty(dto.PolicyJson))
                        {
                            TryParseJsonFallback(dto);
                        }
                    }
                }
            }

            return dto;
        }


        // Fallback JSON parse using Newtonsoft.Json
        private void TryParseJsonFallback_OLD(PolicyResolved dto)
        {
            if (string.IsNullOrWhiteSpace(dto.PolicyJson))
                return;

            try
            {
                JObject json = JObject.Parse(dto.PolicyJson);

                dto.Resolved_MaxKM = TryGetDecimal(json, "MaxKM");
                dto.Resolved_RatePerKm = TryGetDecimal(json, "RatePerKm");
                dto.Resolved_DA = TryGetDecimal(json, "DA");
                dto.Resolved_HotelCap = TryGetDecimal(json, "HotelCap");
                dto.Resolved_Remarks = json["Remarks"] != null ? Convert.ToString(json["Remarks"]) : null;
            }
            catch (Exception ex)
            {
                // Optional: log the error
                System.Diagnostics.Debug.WriteLine("TryParseJsonFallback error: " + ex.Message);
            }
        }

        private void TryParseJsonFallback_OLD1(PolicyResolved dto)
        {
            if (dto?.PolicyJson == null) return;

            try
            {
                var json = JsonConvert.DeserializeObject<JObject>(dto.PolicyJson);
                if (json == null) return;

                dto.Resolved_MaxKM = json["MaxKM"]?.Value<decimal?>() ?? dto.Resolved_MaxKM;
                dto.Resolved_RatePerKm = json["RatePerKm"]?.Value<decimal?>() ?? dto.Resolved_RatePerKm;
                dto.Resolved_DA = json["DA"]?.Value<decimal?>() ?? dto.Resolved_DA;

                dto.Resolved_HotelCap = json["HotelCap"]?.Value<decimal?>()
                                        ?? json["ResolvedHotelCap"]?.Value<decimal?>()
                                        ?? dto.Resolved_HotelCap;

                dto.Resolved_OutstationDA = json["OutstationDA"]?.Value<decimal?>()
                                            ?? json["Outstation_DA"]?.Value<decimal?>()
                                            ?? dto.Resolved_OutstationDA;

                dto.Resolved_OwnAllowance = json["OwnArrangementAllowance"]?.Value<decimal?>()
                                            ?? json["OwnArrangementDa"]?.Value<decimal?>()
                                            ?? json["OwnArrangement_DA"]?.Value<decimal?>()
                                            ?? dto.Resolved_OwnAllowance;

                dto.Resolved_Remarks = json["Remarks"]?.Value<string>()
                                       ?? json["resolved_remarks"]?.Value<string>()
                                       ?? dto.Resolved_Remarks;
            }
            catch (Exception ex)
            {
                // Optional: log the error
                System.Diagnostics.Debug.WriteLine("TryParseJsonFallback error: " + ex.Message);
            }
        }

        private void TryParseJsonFallback(PolicyResolved dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.PolicyJson))
                return;

            try
            {
                JObject json = JObject.Parse(dto.PolicyJson);

                // existing keys (keeps old behavior)
                dto.Resolved_MaxKM = TryGetDecimal(json, "MaxKM") ?? dto.Resolved_MaxKM;
                dto.Resolved_RatePerKm = TryGetDecimal(json, "RatePerKm") ?? dto.Resolved_RatePerKm;
                dto.Resolved_DA = TryGetDecimal(json, "DA") ?? dto.Resolved_DA;
                dto.Resolved_HotelCap = TryGetDecimal(json, "HotelCap") ?? dto.Resolved_HotelCap;

                // new keys: Outstation DA (several common variants)
                decimal? outstation = null;
                outstation = outstation ?? TryGetDecimal(json, "OutstationDA");
                outstation = outstation ?? TryGetDecimal(json, "Outstation_DA");
                outstation = outstation ?? TryGetDecimal(json, "OutstationDa");
                if (outstation.HasValue) dto.Resolved_OutstationDA = outstation;

                // new keys: Own arrangement allowance (several common variants)
                decimal? ownArr = null;
                ownArr = ownArr ?? TryGetDecimal(json, "OwnArrangementAllowance");
                ownArr = ownArr ?? TryGetDecimal(json, "OwnArrangementDa");
                ownArr = ownArr ?? TryGetDecimal(json, "OwnArrangement_DA");
                ownArr = ownArr ?? TryGetDecimal(json, "OwnArrangementDA");
                if (ownArr.HasValue) dto.Resolved_OwnAllowance = ownArr;

                // Remarks (keep previous behavior)
                if (json["Remarks"] != null)
                    dto.Resolved_Remarks = Convert.ToString(json["Remarks"]);
                else if (json["resolved_remarks"] != null)
                    dto.Resolved_Remarks = Convert.ToString(json["resolved_remarks"]);
            }
            catch (Exception ex)
            {
                // Optional: log for debugging. Production: replace with your logging framework if available.
                System.Diagnostics.Debug.WriteLine("TryParseJsonFallback error: " + ex.Message);
            }
        }


        private decimal? TryGetDecimal(JObject json, string key)
        {
            if (json[key] == null || json[key].Type == JTokenType.Null)
                return null;

            decimal value;
            if (decimal.TryParse(Convert.ToString(json[key]), out value))
                return value;

            return null;
        }

        // Compose and register a PNotify alert on the page
        private void ShowPolicyNotification_OLD(PolicyResolved policy)
        {
            if (policy == null) return;

            // Build the message content
            var sb = new StringBuilder();
            sb.AppendFormat("<strong>Policy applied:</strong> {0}<br/>", policy.Source == "EX" ? "Employee Exception" : "Policy Matrix");
            sb.AppendFormat("<strong>Specificity:</strong> {0}<br/>", policy.Specificity ?? 0);
            if (policy.Resolved_RatePerKm != null) sb.AppendFormat("<strong>Rate/km:</strong> ₹{0:N2}<br/>", policy.Resolved_RatePerKm);
            if (policy.Resolved_MaxKM != null) sb.AppendFormat("<strong>Max KM (month):</strong> {0:N0}<br/>", policy.Resolved_MaxKM);
            if (policy.Resolved_DA != null) sb.AppendFormat("<strong>DA:</strong> ₹{0:N0}<br/>", policy.Resolved_DA);
            if (policy.Resolved_HotelCap != null) sb.AppendFormat("<strong>Hotel Cap:</strong> ₹{0:N0}<br/>", policy.Resolved_HotelCap);
            if (!string.IsNullOrEmpty(policy.Resolved_Remarks)) sb.AppendFormat("<strong>Remarks:</strong> {0}<br/>", HttpUtility.JavaScriptStringEncode(policy.Resolved_Remarks));

            // Title and type selection
            string title = policy.Source == "EX" ? "Employee exception applied" : "Policy applied";
            string type = policy.Source == "EX" ? "info" : "notice"; // customize per PNotify version

            // Build JS to show PNotify (this code assumes your PNotify exposes a global PNotify function)
            // Adjust JS to match your PNotify version; below is a generic example.
            string safeTitle = HttpUtility.JavaScriptStringEncode(title);
            string safeText = HttpUtility.JavaScriptStringEncode(sb.ToString());
            string safeAlert = HttpUtility.JavaScriptStringEncode(title + "\\n" + sb.ToString());

            string js = @"
                (function() {
                    if (typeof PNotify !== 'undefined' && typeof PNotify.alert === 'function') {
                        PNotify.alert({
                            title: '" + safeTitle + @"',
                            text: '" + safeText + @"',
                            styling: 'brighttheme',
                            delay: 6000
                        });
                    } else if (typeof PNotify !== 'undefined') {
                        console.log('PNotify available but version mismatch.');
                    } else {
                        alert('" + safeAlert + @"');
                    }
                })();";

            // Register startup script to execute after page loads
            ScriptManager.RegisterStartupScript(this.Page, this.Page.GetType(), "policyNotify_" + Guid.NewGuid().ToString("N"), js, true);
        }

        private void ShowPolicyNotification_OLD1(PolicyResolved policy)
        {
            if (policy == null) return;

            var sb = new StringBuilder();
            sb.AppendFormat("<strong>Policy applied:</strong> {0}<br/>", policy.Source == "EX" ? "Employee Exception" : "Policy Matrix");
            sb.AppendFormat("<strong>Specificity:</strong> {0}<br/>", policy.Specificity ?? 0);
            if (policy.Resolved_RatePerKm != null) sb.AppendFormat("<strong>Rate/km:</strong> ₹{0:N2}<br/>", policy.Resolved_RatePerKm);
            if (policy.Resolved_MaxKM != null) sb.AppendFormat("<strong>Max KM (month):</strong> {0:N0}<br/>", policy.Resolved_MaxKM);
            if (policy.Resolved_DA != null) sb.AppendFormat("<strong>DA:</strong> ₹{0:N0}<br/>", policy.Resolved_DA);
            // NEW: Outstation DA / Own Arrangement allowance
            if (policy.Resolved_OutstationDA != null) sb.AppendFormat("<strong>Outstation DA:</strong> ₹{0:N0}<br/>", policy.Resolved_OutstationDA);
            if (policy.Resolved_OwnAllowance != null) sb.AppendFormat("<strong>Own Arrangement Allowance:</strong> ₹{0:N0}<br/>", policy.Resolved_OwnAllowance);
            if (policy.Resolved_HotelCap != null) sb.AppendFormat("<strong>Hotel Cap:</strong> ₹{0:N0}<br/>", policy.Resolved_HotelCap);
            if (!string.IsNullOrEmpty(policy.Resolved_Remarks)) sb.AppendFormat("<strong>Remarks:</strong> {0}<br/>", HttpUtility.JavaScriptStringEncode(policy.Resolved_Remarks));

            string title = policy.Source == "EX" ? "Employee exception applied" : "Policy applied";
            string safeTitle = HttpUtility.JavaScriptStringEncode(title);
            string safeText = HttpUtility.JavaScriptStringEncode(sb.ToString());
            string safeAlert = HttpUtility.JavaScriptStringEncode(title + "\\n" + sb.ToString());

            string js = @"
                (function() {
                    if (typeof PNotify !== 'undefined' && typeof PNotify.alert === 'function') {
                        PNotify.alert({
                            title: '" + safeTitle + @"',
                            text: '" + safeText + @"',
                            styling: 'brighttheme',
                            delay: 6000
                        });
                    } else if (typeof PNotify !== 'undefined') {
                        try { new PNotify({ title: '" + safeTitle + @"', text: '" + safeText + @"', delay: 6000 }); } catch(e) { console.log('PNotify fallback'); }
                    } else {
                        alert('" + safeAlert + @"');
                    }
                })();";

            ScriptManager.RegisterStartupScript(this.Page, this.Page.GetType(), "policyNotify_" + Guid.NewGuid().ToString("N"), js, true);
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
            if (!string.IsNullOrEmpty(policy.Resolved_Remarks))
                sb.AppendFormat("<strong>Remarks:</strong> {0}<br/>", HttpUtility.JavaScriptStringEncode(policy.Resolved_Remarks));

            string title = policy.Source == "EX" ? "Employee exception applied" : "Policy applied";
            string safeTitle = HttpUtility.JavaScriptStringEncode(title);
            string safeText = HttpUtility.JavaScriptStringEncode(sb.ToString());
            string notifyType = policy.Source == "EX" ? "warning" : "info";

            // --- Just trigger your existing JS function ---
            string js = @"
                try {
                    if (typeof showPNotify === 'function') {
                        showPNotify('" + safeTitle + @"', '" + safeText + @"', '" + notifyType + @"');
                    } else {
                        console.log('showPNotify() not found on page.');
                    }
                } catch(e) {
                    console.log('showPNotify() call failed:', e);
                }";

            ScriptManager.RegisterStartupScript(
                this.Page,
                this.Page.GetType(),
                "policyNotify_" + Guid.NewGuid().ToString("N"),
                js,
                true
            );
        }



        // Stub helper methods - implement according to your project auth/session
        private string GetLoggedInEmployeeCode()
        {
            // Example: from Session or User.Identity
            return Session["WORKMAN"] as string;
            // return HttpContext.Current.User?.Identity?.Name ?? "EMP001";
        }

        private int? GetLoggedInRegionId() { return Session["REGION"] != null ? (int?)Convert.ToInt32(Session["REGION"]) : null; }
        private int? GetLoggedInBranchId() { return Session["BRANCH"] != null ? (int?)Convert.ToInt32(Session["BRANCH"]) : null; }
        private int? GetLoggedInGradeId() { return Session["GRADE"] != null ? (int?)Convert.ToInt32(Session["GRADE"]) : null; }
        private int? GetLoggedInDesignationId() { return Session["DESG"] != null ? (int?)Convert.ToInt32(Session["DESG"]) : null; }


        //------------------------- SAVE AS DRAFT & SUBMIT ------------------------//

        protected void btn_save_draft_Click_OLD(object sender, EventArgs e)
        {
            BindGrid(false);
            lbl_msg.ForeColor = System.Drawing.Color.Green;
            lbl_msg.Text = "Draft saved.";
        }

        protected void btn_submit_Click_OLD(object sender, EventArgs e)
        {
            lbl_msg.Text = "";
            int year = int.Parse(DDL_Year.SelectedValue);
            int month = int.Parse(DDL_Month.SelectedValue);

            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                using (SqlCommand cmd = new SqlCommand("usp_SubmitMonthForApproval", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@LoginId", GetCurrentUserLoginId());
                    cmd.Parameters.AddWithValue("@Year", year);
                    cmd.Parameters.AddWithValue("@Month", month);
                    cmd.Parameters.AddWithValue("@SubmittedBy", GetCurrentUserLoginId());

                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                DataTable missingDt = new DataTable();
                using (SqlConnection con = new SqlConnection(connString))
                using (SqlCommand cmd = new SqlCommand("usp_GetMissingAttachmentsForUserMonth", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@LoginId", GetCurrentUserLoginId());
                    cmd.Parameters.AddWithValue("@Year", year);
                    cmd.Parameters.AddWithValue("@Month", month);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(missingDt);
                }

                if (missingDt.Rows.Count > 0)
                {
                    string msg = "Missing attachments for days: ";
                    foreach (DataRow r in missingDt.Rows)
                        msg += Convert.ToDateTime(r["ActivityDate"]).ToString("yyyy-MM-dd") + ", ";
                    msg = msg.TrimEnd(' ', ',');
                    lbl_msg.ForeColor = System.Drawing.Color.Orange;
                    lbl_msg.Text = msg;
                }
                else
                {
                    lbl_msg.ForeColor = System.Drawing.Color.Green;
                    lbl_msg.Text = "Submitted for approval.";
                }

                BindGrid(false);
            }
            catch (SqlException sqx)
            {
                lbl_msg.ForeColor = System.Drawing.Color.Red;
                lbl_msg.Text = sqx.Message; // SP RAISERROR/THROW message bubbled here

                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyError3", $"showPNotify('Error', '{sqx.Message.Replace("'", "\\'")}', 'error');", true);
            }
        }

        private DataTable BuildActivityUpdateDataTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("ActivityId", typeof(int));
            dt.Columns.Add("TotalDistanceKM", typeof(decimal));
            dt.Columns.Add("CalculatedClaimAmount", typeof(decimal));
            return dt;
        }

        // returns DataTable (TVP) and out errors (if any)
        private DataTable CollectGridActivityUpdates_OLD(out List<string> errors)
        {
            errors = new List<string>();
            DataTable tvp = BuildActivityUpdateDataTable();

            // default rate fallback
            decimal defaultRate = 2.75m;
            var cfg = ConfigurationManager.AppSettings["DefaultRatePerKm"];
            decimal tmpCfg;
            if (!string.IsNullOrWhiteSpace(cfg) && decimal.TryParse(cfg, out tmpCfg))
                defaultRate = tmpCfg;

            CultureInfo ci = CultureInfo.GetCultureInfo("en-IN");

            for (int i = 0; i < gvActivities.Rows.Count; i++)
            {
                GridViewRow row = gvActivities.Rows[i];

                // Read ActivityId from DataKeys
                int activityId = 0;
                object key = gvActivities.DataKeys[row.RowIndex].Value;
                if (key == null || !int.TryParse(key.ToString(), out activityId))
                {
                    errors.Add($"Row {i + 1}: missing ActivityId.");
                    continue;
                }

                // Read KM either from edit textbox (txtKM) or fallback hidden label lblTolRawMin
                decimal km = 0m;
                var txtKM = row.FindControl("txtKM") as TextBox;
                if (txtKM != null && !string.IsNullOrWhiteSpace(txtKM.Text))
                {
                    if (!decimal.TryParse(txtKM.Text, NumberStyles.AllowDecimalPoint, ci, out km))
                    {
                        errors.Add($"Activity {activityId}: invalid KM value.");
                        continue;
                    }
                }
                else
                {
                    // fallback - try to get value from the DataItem rendered label (you had lblTolRawMin used earlier as hidden)
                    var lblRawMin = row.FindControl("lblTolRawMin") as Label;
                    decimal fallbackKm;
                    if (lblRawMin != null && decimal.TryParse(lblRawMin.Text, NumberStyles.AllowDecimalPoint, ci, out fallbackKm))
                    {
                        km = fallbackKm;
                    }
                    else
                    {
                        // if we can't determine a km value, treat as zero (or optionally error)
                        km = 0m;
                    }
                }

                // Fetch per-row allowed min/max from hidden labels (present in your Grid markup)
                decimal minAllowed = Decimal.MinValue;
                decimal maxAllowed = Decimal.MaxValue;

                var lblMin = row.FindControl("lblTolRawMin") as Label;
                var lblMax = row.FindControl("lblTolRawMax") as Label;

                if (lblMin != null && !string.IsNullOrWhiteSpace(lblMin.Text))
                {
                    if (!decimal.TryParse(lblMin.Text, NumberStyles.AllowDecimalPoint, ci, out minAllowed))
                    {
                        minAllowed = Decimal.MinValue;
                    }
                }

                if (lblMax != null && !string.IsNullOrWhiteSpace(lblMax.Text))
                {
                    if (!decimal.TryParse(lblMax.Text, NumberStyles.AllowDecimalPoint, ci, out maxAllowed))
                    {
                        maxAllowed = Decimal.MaxValue;
                    }
                }

                // Validate bounds: if both min and max are present as finite numbers, validate
                bool hasMin = (minAllowed != Decimal.MinValue);
                bool hasMax = (maxAllowed != Decimal.MaxValue);

                if (hasMin && km < minAllowed - 0.0001m) // small epsilon
                {
                    errors.Add($"Activity {activityId} (row {i + 1}): KM {km:N2} is less than allowed minimum {minAllowed:N2}.");
                    // continue collecting others to show consolidated errors
                }

                if (hasMax && km > maxAllowed + 0.0001m)
                {
                    errors.Add($"Activity {activityId} (row {i + 1}): KM {km:N2} exceeds allowed maximum {maxAllowed:N2}.");
                }

                // Determine rate: try to read a RatePerKm hidden label if you have one (use "lblRatePerKm" or similar).
                decimal rate = defaultRate;
                var lblRateHidden = row.FindControl("lblRatePerKm") as Label; // try find hidden label if you added it
                if (lblRateHidden != null && !string.IsNullOrWhiteSpace(lblRateHidden.Text))
                {
                    decimal parsedRate;
                    if (!decimal.TryParse(lblRateHidden.Text, NumberStyles.AllowDecimalPoint, ci, out parsedRate))
                        parsedRate = defaultRate;
                    rate = parsedRate;
                }

                // Compute calculated claim amount server-side (safety)
                decimal calcClaim = Math.Round(rate * km, 2, MidpointRounding.AwayFromZero);

                DataRow dr = tvp.NewRow();
                dr["ActivityId"] = activityId;
                dr["TotalDistanceKM"] = km;
                dr["CalculatedClaimAmount"] = calcClaim;
                tvp.Rows.Add(dr);
            }

            return tvp;
        }

        private DataTable CollectGridActivityUpdates_OLD1(out List<string> errors)
        {
            errors = new List<string>();
            DataTable tvp = BuildActivityUpdateDataTable();

            decimal defaultRate = 2.75m;
            var cfg = ConfigurationManager.AppSettings["DefaultRatePerKm"];
            decimal tmpCfg;
            if (!string.IsNullOrWhiteSpace(cfg) && decimal.TryParse(cfg, out tmpCfg))
                defaultRate = tmpCfg;

            CultureInfo ci = CultureInfo.GetCultureInfo("en-IN");

            if (gvActivities == null)
            {
                errors.Add("gvActivities is null.");
                return tvp;
            }

            // Diagnostic - useful to log during debugging
            System.Diagnostics.Trace.TraceInformation($"CollectGridActivityUpdates: Rows={gvActivities.Rows.Count}, DataKeys={gvActivities.DataKeys?.Count ?? -1}");

            for (int i = 0; i < gvActivities.Rows.Count; i++)
            {
                GridViewRow row = gvActivities.Rows[i];

                // SAFER: use loop index 'i' to index DataKeys and guard against missing keys
                if (gvActivities.DataKeys == null || gvActivities.DataKeys.Count <= i)
                {
                    errors.Add($"Row {i + 1}: DataKey missing for row index {i}. Check viewstate/paging/data-binding.");
                    continue;
                }

                object keyObj = gvActivities.DataKeys[i].Value;
                if (keyObj == null || keyObj == DBNull.Value)
                {
                    errors.Add($"Row {i + 1}: DataKey value is null.");
                    continue;
                }

                int activityId;
                bool parsed = int.TryParse(keyObj.ToString(), out activityId);
                if (!parsed)
                {
                    // As a fallback, try named value if DataKeyNames had multiple names (unlikely here but safe)
                    try
                    {
                        var vals = gvActivities.DataKeys[i].Values;
                        if (vals != null && vals["ActivityId"] != null)
                            parsed = int.TryParse(vals["ActivityId"].ToString(), out activityId);
                    }
                    catch { /* ignore */ }
                }

                if (!parsed)
                {
                    errors.Add($"Row {i + 1}: cannot parse ActivityId from DataKeys (value: '{keyObj}').");
                    continue;
                }

                // Read KM from edit textbox or fallback label (same as your original logic)
                decimal km = 0m;
                var txtKM = row.FindControl("txtKM") as TextBox;
                if (txtKM != null && !string.IsNullOrWhiteSpace(txtKM.Text))
                {
                    if (!decimal.TryParse(txtKM.Text, NumberStyles.AllowDecimalPoint, ci, out km))
                    {
                        errors.Add($"Activity {activityId}: invalid KM value.");
                        continue;
                    }
                }
                else
                {
                    var lblRawMin = row.FindControl("lblTolRawMin") as Label;
                    decimal fallbackKm;
                    if (lblRawMin != null && !string.IsNullOrWhiteSpace(lblRawMin.Text)
                        && decimal.TryParse(lblRawMin.Text, NumberStyles.AllowDecimalPoint, ci, out fallbackKm))
                    {
                        km = fallbackKm;
                    }
                    else
                    {
                        km = 0m;
                    }
                }

                // min/max labels (same as before, but with TryParse)
                decimal minAllowed = Decimal.MinValue, maxAllowed = Decimal.MaxValue;
                var lblMin = row.FindControl("lblTolRawMin") as Label;
                var lblMax = row.FindControl("lblTolRawMax") as Label;

                decimal tmp;
                if (lblMin != null && !string.IsNullOrWhiteSpace(lblMin.Text) && decimal.TryParse(lblMin.Text, NumberStyles.AllowDecimalPoint, ci, out tmp))
                    minAllowed = tmp;
                if (lblMax != null && !string.IsNullOrWhiteSpace(lblMax.Text) && decimal.TryParse(lblMax.Text, NumberStyles.AllowDecimalPoint, ci, out tmp))
                    maxAllowed = tmp;

                bool hasMin = (minAllowed != Decimal.MinValue);
                bool hasMax = (maxAllowed != Decimal.MaxValue);

                if (hasMin && km < minAllowed - 0.0001m)
                    errors.Add($"Activity {activityId} (row {i + 1}): KM {km:N2} is less than allowed minimum {minAllowed:N2}.");

                if (hasMax && km > maxAllowed + 0.0001m)
                    errors.Add($"Activity {activityId} (row {i + 1}): KM {km:N2} exceeds allowed maximum {maxAllowed:N2}.");

                // Rate
                decimal rate = defaultRate;
                var lblRateHidden = row.FindControl("lblRatePerKm") as Label;
                if (lblRateHidden != null && !string.IsNullOrWhiteSpace(lblRateHidden.Text) && decimal.TryParse(lblRateHidden.Text, NumberStyles.AllowDecimalPoint, ci, out tmp))
                    rate = tmp;

                decimal calcClaim = Math.Round(rate * km, 2, MidpointRounding.AwayFromZero);

                DataRow dr = tvp.NewRow();
                dr["ActivityId"] = activityId;
                dr["TotalDistanceKM"] = km;
                dr["CalculatedClaimAmount"] = calcClaim;
                tvp.Rows.Add(dr);
            }

            return tvp;
        }

        private DataTable CollectGridActivityUpdates(out List<string> errors)
        {
            errors = new List<string>();
            DataTable tvp = BuildActivityUpdateDataTable();

            var sessionDt = Session[SESSION_KEY_GV_ACTIVITIES] as DataTable;
            if (sessionDt == null)
            {
                errors.Add("Session data missing. Please refresh the page.");
                return tvp;
            }

            CultureInfo ci = CultureInfo.GetCultureInfo("en-IN");
            decimal defaultRate = 2.75m;
            var cfg = ConfigurationManager.AppSettings["DefaultRatePerKm"];
            decimal tmpCfg;
            if (!string.IsNullOrWhiteSpace(cfg) && decimal.TryParse(cfg, out tmpCfg))
                defaultRate = tmpCfg;

            // Diagnostic (optional)
            System.Diagnostics.Trace.TraceInformation($"CollectGridActivityUpdates: sessionRows={sessionDt.Rows.Count}, gridRows={gvActivities.Rows.Count}");

            for (int i = 0; i < gvActivities.Rows.Count; i++)
            {
                if (gvActivities.DataKeys == null || gvActivities.DataKeys.Count <= i)
                {
                    errors.Add($"Row {i + 1}: DataKey missing.");
                    continue;
                }

                object keyObj = gvActivities.DataKeys[i].Value;
                if (keyObj == null || keyObj == DBNull.Value)
                {
                    errors.Add($"Row {i + 1}: DataKey null.");
                    continue;
                }

                int activityId;
                if (!int.TryParse(keyObj.ToString(), out activityId))
                {
                    errors.Add($"Row {i + 1}: invalid ActivityId in DataKey.");
                    continue;
                }

                // locate session row
                DataRow found = null;
                if (sessionDt.PrimaryKey != null && sessionDt.PrimaryKey.Length > 0)
                    found = sessionDt.Rows.Find(activityId);
                else
                {
                    DataRow[] arr = sessionDt.Select($"ActivityId = {activityId}");
                    if (arr != null && arr.Length > 0) found = arr[0];
                }

                if (found == null)
                {
                    errors.Add($"Activity {activityId}: session data not found.");
                    continue;
                }

                // Read canonical km from session row
                decimal km = 0m;
                if (found.Table.Columns.Contains("TotalDistanceKM") && found["TotalDistanceKM"] != DBNull.Value)
                    decimal.TryParse(found["TotalDistanceKM"].ToString(), NumberStyles.AllowDecimalPoint, ci, out km);

                decimal minAllowed = Decimal.MinValue, maxAllowed = Decimal.MaxValue, rate = defaultRate;
                decimal tmp;
                if (found.Table.Columns.Contains("MinAllowedKM") && found["MinAllowedKM"] != DBNull.Value && decimal.TryParse(found["MinAllowedKM"].ToString(), NumberStyles.AllowDecimalPoint, ci, out tmp))
                    minAllowed = tmp;
                if (found.Table.Columns.Contains("MaxAllowedKM") && found["MaxAllowedKM"] != DBNull.Value && decimal.TryParse(found["MaxAllowedKM"].ToString(), NumberStyles.AllowDecimalPoint, ci, out tmp))
                    maxAllowed = tmp;
                if (found.Table.Columns.Contains("RatePerKm") && found["RatePerKm"] != DBNull.Value && decimal.TryParse(found["RatePerKm"].ToString(), NumberStyles.AllowDecimalPoint, ci, out tmp))
                    rate = tmp;

                bool hasMin = (minAllowed != Decimal.MinValue);
                bool hasMax = (maxAllowed != Decimal.MaxValue);

                if (hasMin && km < minAllowed - 0.0001m)
                    errors.Add($"Activity {activityId}: KM {km:N2} less than minimum {minAllowed:N2}.");

                if (hasMax && km > maxAllowed + 0.0001m)
                    errors.Add($"Activity {activityId}: KM {km:N2} greater than maximum {maxAllowed:N2}.");

                decimal calcClaim = Math.Round(rate * km, 2, MidpointRounding.AwayFromZero);

                DataRow dr = tvp.NewRow();
                dr["ActivityId"] = activityId;
                dr["TotalDistanceKM"] = km;
                dr["CalculatedClaimAmount"] = calcClaim;
                tvp.Rows.Add(dr);
            }

            return tvp;
        }



        // Helper: execute usp_SaveActivitiesDraft (returns updated rows)
        // --------------------------------------------
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
                // usp_SaveActivitiesDraft returns UpdatedRows as a single resultset (SELECT @@ROWCOUNT)
                //using (var rdr = cmd.ExecuteReader())
                //{
                //    if (rdr.Read())
                //    {
                //        updatedRows = rdr.GetInt32(0);
                //    }
                //}
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        int rowsUpserted = rdr.GetInt32(0);
                        int expensesUpdated = rdr.GetInt32(1);
                    }
                }

            }

            return updatedRows;
        }

        // Helper: execute usp_SubmitSalesDailyActivities (returns ApprovalRequestId)
        // --------------------------------------------
        private int ExecuteSubmitForApproval_OLD(DataTable tvp, int year, int month, string remarks)
        {
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

                con.Open();
                //using (var rdr = cmd.ExecuteReader())
                //{
                //    if (rdr.Read())
                //    {
                //        // stored proc SELECTs ApprovalRequestId
                //        if (!rdr.IsDBNull(0))
                //            approvalRequestId = rdr.GetInt32(0);
                //    }
                //}

                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        approvalRequestId = rdr.GetInt32(0);
                        decimal totalKM = rdr.IsDBNull(1) ? 0m : rdr.GetDecimal(1);
                        decimal totalClaim = rdr.IsDBNull(2) ? 0m : rdr.GetDecimal(2);
                        // read other totals similarly...
                        // you can store in out params or show via showPNotify
                        ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySubmit",
                            $"showPNotify('Submitted','Submitted for approval successfully. Request ID: {approvalRequestId}<br/>KM: {totalKM:N2} | Claim: ₹ {totalClaim:N2}','success');", true);
                    }
                }
            }

            return approvalRequestId;
        }

        private int ExecuteSubmitForApproval(DataTable tvp, int year, int month, string remarks)
        {
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

                con.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        // Always check FieldCount before accessing ordinals > 0
                        int fields = rdr.FieldCount;

                        // ApprovalRequestId expected at ordinal 0 — be defensive
                        if (fields >= 1 && !rdr.IsDBNull(0))
                            approvalRequestId = rdr.GetInt32(0);
                        else
                            approvalRequestId = 0; // or leave as default

                        // Optional: read totals only if the SP returned them. Check FieldCount.
                        decimal totalKM = 0m, totalClaim = 0m;
                        if (fields >= 2 && !rdr.IsDBNull(1))
                        {
                            // Use GetDecimal safely (ordinal exists)
                            try { totalKM = rdr.GetDecimal(1); }
                            catch { decimal.TryParse(rdr[1]?.ToString(), out totalKM); }
                        }
                        if (fields >= 3 && !rdr.IsDBNull(2))
                        {
                            try { totalClaim = rdr.GetDecimal(2); }
                            catch { decimal.TryParse(rdr[2]?.ToString(), out totalClaim); }
                        }

                        // If you expected additional columns, add similar guarded reads here.
                        // Build a safe message to display to user
                        string msg = $"Submitted for approval. Request ID: {approvalRequestId}";
                        if (fields >= 3)
                        {
                            msg += $"<br/>KM: {totalKM:N2} | Claim: ₹ {totalClaim:N2}";
                        }

                        // Safely inject to client
                        ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySubmit",
                            $"showPNotify('Submitted','{HttpUtility.JavaScriptStringEncode(msg)}','success');", true);
                    }
                    else
                    {
                        // No rows returned — still not an SP failure; handle gracefully
                        ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySubmitNoRow",
                            $"showPNotify('Submitted','Submitted for approval. (No summary returned)','success');", true);
                    }
                }
            }
            return approvalRequestId;
        }


        // ------------------------------------------------
        // Save as Draft button handler
        // ------------------------------------------------
        protected void btn_save_draft_Click_OLD2(object sender, EventArgs e)
        {
            try
            {
                // Defensive check: ensure grid is bound and footer available
                if (gvActivities.FooterRow == null)
                {
                    ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyNoFooter",
                        "showPNotify('Warning','No data to save as draft.','warning');", true);
                    return;
                }

                // Read footer totals
                decimal totalKM = ParseDecimalFromLabel("lblFooterTotalKM");
                decimal totalClaim = ParseCurrencyFromLabel("lblFooterCalculated");

                // Create a TVP with one dummy row representing the summary
                DataTable tvp = BuildActivityUpdateDataTable();
                DataRow dr = tvp.NewRow();
                dr["ActivityId"] = 0; // no specific ActivityId — or pick 1st if needed
                dr["TotalDistanceKM"] = totalKM;
                dr["CalculatedClaimAmount"] = totalClaim;
                tvp.Rows.Add(dr);

                // Execute draft SP
                int updated = ExecuteSaveDraft(tvp);
                //int updated = ExecuteSaveMonthlySummary(totalKM, totalClaim);

                // After save, query saved summary and show values
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(connString))
                using (SqlCommand cmd = new SqlCommand("SELECT TotalKM, TotalClaimAmount, CreatedOn FROM dbo.SalesMonthlyDraftSummary WHERE Login_Id=@Login AND YearNo=@Y AND MonthNo=@M", con))
                {
                    cmd.Parameters.AddWithValue("@Login", GetCurrentUserLoginId());
                    cmd.Parameters.AddWithValue("@Y", int.Parse(DDL_Year.SelectedValue));
                    cmd.Parameters.AddWithValue("@M", int.Parse(DDL_Month.SelectedValue));
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
                }
                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    ScriptManager.RegisterStartupScript(this, GetType(), "showSavedSummary",
                       $"showPNotify('Saved','Draft saved — KM: {row["TotalKM"]} Claim: ₹ {row["TotalClaimAmount"]}','success');", true);
                }


                BindGrid(true, false);

                lbl_msg.Text = $"Saved draft summary successfully (Total KM: {totalKM:N2}, Total Claim: ₹ {totalClaim:N2}).";
                lbl_msg.ForeColor = System.Drawing.Color.Green;
                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyDraft",
                    $"showPNotify('Saved','Draft saved successfully.<br/>KM: {totalKM:N2} | Claim: ₹ {totalClaim:N2}','success');", true);
            }
            catch (Exception ex)
            {
                lbl_msg.Text = "Error saving draft: " + ex.Message;
                lbl_msg.ForeColor = System.Drawing.Color.Red;
                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyDraftErr",
                    $"showPNotify('Error','{ex.Message.Replace("'", "\\'")}','error');", true);
            }
        }

        protected void btn_save_draft_Click(object sender, EventArgs e)
        {
            try
            {
                // Defensive check: ensure grid is bound and footer available
                if (gvActivities.FooterRow == null)
                {
                    ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyNoFooter",
                        "showPNotify('Warning','No data to save as draft.','warning');", true);
                    return;
                }

                // Read footer totals
                decimal totalKM = ParseDecimalFromLabel("lblFooterTotalKM");
                decimal totalClaim = ParseCurrencyFromLabel("lblFooterCalculated");

                // Execute new stored procedure for monthly summary draft
                var result = ExecuteSaveMonthlySummary(totalKM, totalClaim);
                int rowsUpserted = result.Item1;
                int expensesUpdated = result.Item2;

                // After save, query saved summary and show values
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(connString))
                using (SqlCommand cmd = new SqlCommand(@"SELECT TotalKM, TotalClaimAmount, CreatedOn 
                                                 FROM dbo.SalesMonthlyDraftSummary 
                                                 WHERE Login_Id=@Login AND YearNo=@Y AND MonthNo=@M", con))
                {
                    cmd.Parameters.AddWithValue("@Login", GetCurrentUserLoginId());
                    cmd.Parameters.AddWithValue("@Y", int.Parse(DDL_Year.SelectedValue));
                    cmd.Parameters.AddWithValue("@M", int.Parse(DDL_Month.SelectedValue));
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd)) da.Fill(dt);
                }

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    ScriptManager.RegisterStartupScript(this, GetType(), "showSavedSummary",
                        $"showPNotify('Saved','Draft saved — KM: {row["TotalKM"]} Claim: ₹ {row["TotalClaimAmount"]}','success');", true);
                }
                Session.Remove(SESSION_KEY_GV_ACTIVITIES);
                // Refresh grid and show success info
                BindGrid(true, false);

                lbl_msg.Text = $"Saved draft summary successfully (Total KM: {totalKM:N2}, Total Claim: ₹ {totalClaim:N2}). " +
                               $"[Upserted: {rowsUpserted}, Expenses Updated: {expensesUpdated}]";
                lbl_msg.ForeColor = System.Drawing.Color.Green;

                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyDraft",
                    $"showPNotify('Saved','Draft saved successfully.<br/>KM: {totalKM:N2} | Claim: ₹ {totalClaim:N2}<br/>" +
                    $"Rows: {rowsUpserted}, Expenses: {expensesUpdated}','success');", true);
            }
            catch (Exception ex)
            {
                lbl_msg.Text = "Error saving draft: " + ex.Message;
                lbl_msg.ForeColor = System.Drawing.Color.Red;
                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyDraftErr",
                    $"showPNotify('Error','{ex.Message.Replace("'", "\\'")}','error');", true);
            }
        }

        private Tuple<int, int> ExecuteSaveMonthlySummary(decimal totalKM, decimal totalClaim)
        {
            int rowsUpserted = 0;
            int expensesUpdated = 0;

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
                            // Ensure we handle nulls & non-int safely
                            rowsUpserted = Convert.ToInt32(rdr["RowsUpserted"]);
                            expensesUpdated = Convert.ToInt32(rdr["ExpensesUpdated"]);
                        }
                        else
                        {
                            // For debugging
                            System.Diagnostics.Debug.WriteLine("usp_SaveMonthlyDraftSummary returned no rows.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // optional: log or display friendly message
                System.Diagnostics.Debug.WriteLine("Error in ExecuteSaveMonthlySummary: " + ex);
                throw; // let caller handle UI message
            }

            return new Tuple<int, int>(rowsUpserted, expensesUpdated);
        }



        private decimal ParseCurrencyFromLabel(string labelId)
        {
            Label lbl = gvActivities.FooterRow?.FindControl(labelId) as Label;
            if (lbl == null || string.IsNullOrWhiteSpace(lbl.Text)) return 0m;

            string text = lbl.Text.Replace("₹", "").Replace(",", "").Trim();
            decimal val;
            decimal.TryParse(text, out val);
            return val;
        }

        private decimal ParseDecimalFromLabel(string labelId)
        {
            Label lbl = gvActivities.FooterRow?.FindControl(labelId) as Label;
            if (lbl == null || string.IsNullOrWhiteSpace(lbl.Text)) return 0m;

            string text = lbl.Text.Replace("km", "").Replace(",", "").Trim();
            decimal val;
            decimal.TryParse(text, out val);
            return val;
        }

        // ------------------------------------------------
        // Submit for Approval button handler
        // ------------------------------------------------
        protected void btn_submit_Click(object sender, EventArgs e)
        {
            try
            {
                int year = int.Parse(DDL_Year.SelectedValue);
                int month = int.Parse(DDL_Month.SelectedValue);

                // Re-check server-side policy window if configured (same logic as BindGrid)
                bool isPolicyEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings["EnablePreviousMonthPolicy"]);
                if (isPolicyEnabled)
                {
                    DateTime currentDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                    int currentYear = currentDate.Year;
                    int currentMonth = currentDate.Month;
                    bool isAllowedWindow = currentDate.Day >= 1 && currentDate.Day <= 15;
                    bool isPreviousMonthSelected =
                        (year == currentYear && month == currentMonth - 1) ||
                        (currentMonth == 1 && month == 12 && year == currentYear - 1);

                    if (!(isAllowedWindow && isPreviousMonthSelected))
                    {
                        string msg = "You can submit previous month data only between 1st and 15th of the current month.";
                        lbl_msg.Text = msg;
                        lbl_msg.ForeColor = System.Drawing.Color.Red;
                        ScriptManager.RegisterStartupScript(this, GetType(), "pnotifyPolicyBlockSubmit",
                            "showPNotify('Policy Restriction', 'Submission is allowed only between 1st and 15th for the previous month.', 'error');", true);
                        return;
                    }
                }

                // Collect and validate
                List<string> errors;
                DataTable tvp = CollectGridActivityUpdates(out errors);

                if (errors.Any())
                {
                    string msg = "Unable to submit due to validation errors:<br/>" + string.Join("<br/>", errors);
                    lbl_msg.Text = "Validation failed.";
                    lbl_msg.ForeColor = System.Drawing.Color.Red;
                    ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySubmitErr", $"showPNotify('Validation Error','{msg.Replace("'", "\\'")}', 'error');", true);
                    return;
                }

                // Optional: quick check if there are any rows to submit
                if (tvp.Rows.Count == 0)
                {
                    lbl_msg.Text = "No activity rows found to submit.";
                    lbl_msg.ForeColor = System.Drawing.Color.Red;
                    ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySubmitNoRows", "showPNotify('Nothing to submit', 'No activity rows found.', 'warning');", true);
                    return;
                }

                // Call SP to submit - returns ApprovalRequestId
                string remarks = "Submitted via UI";
                int approvalRequestId = ExecuteSubmitForApproval(tvp, year, month, remarks);

                if (approvalRequestId > 0)
                {
                    Session.Remove(SESSION_KEY_GV_ACTIVITIES);
                    BindGrid(true);
                    lbl_msg.Text = $"Submitted {tvp.Rows.Count} rows for approval. Request Id: {approvalRequestId}";
                    lbl_msg.ForeColor = System.Drawing.Color.Green;
                    ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySubmit", $"showPNotify('Submitted','Submitted for approval successfully. Request ID: {approvalRequestId}','success');", true);
                }
                else
                {
                    // If stored proc didn't return id but didn't fail, show generic
                    Session.Remove(SESSION_KEY_GV_ACTIVITIES);
                    BindGrid(true);
                    lbl_msg.Text = "Submitted for approval (no request id returned).";
                    lbl_msg.ForeColor = System.Drawing.Color.Green;
                    ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySubmit2", "showPNotify('Submitted','Submitted for approval successfully.','success');", true);
                }
            }
            catch (SqlException sqx)
            {
                // Handle stored proc raiserror (e.g., duplicate pending request)
                lbl_msg.Text = "SQL Error while submitting: " + sqx.Message;
                lbl_msg.ForeColor = System.Drawing.Color.Red;
                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySubmitSqlErr", $"showPNotify('SQL Error','{sqx.Message.Replace("'", "\\'")}','error');", true);
            }
            catch (Exception ex)
            {
                lbl_msg.Text = "Error during submit: " + ex.Message;
                lbl_msg.ForeColor = System.Drawing.Color.Red;
                ScriptManager.RegisterStartupScript(this, GetType(), "pnotifySubmitErr2", $"showPNotify('Error','{ex.Message.Replace("'", "\\'")}','error');", true);
            }
        }

        private void PopulateStatusSummary(DataTable dt)
        {
            // safe guard
            if (dt == null)
                return;

            // statuses we show
            string[] statuses = new string[] { "Draft", "Submitted", "Approved", "Editable", "Other" };

            // helper: sum a column for rows matching a predicate - keep robust to missing columns
            Func<string, Func<DataRow, bool>, decimal> SumMoney = (colName, predicate) =>
            {
                decimal sum = 0m;
                if (!dt.Columns.Contains(colName)) return 0m;
                foreach (DataRow r in dt.Rows)
                {
                    if (!predicate(r)) continue;
                    if (r[colName] != DBNull.Value)
                    {
                        decimal val;
                        if (decimal.TryParse(r[colName].ToString(), out val))
                            sum += val;
                    }
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
                    if (r["TotalDistanceKM"] != DBNull.Value)
                    {
                        decimal km;
                        if (decimal.TryParse(r["TotalDistanceKM"].ToString(), out km))
                            sum += km;
                    }
                }
                return sum;
            };

            // predicate builders
            Func<string, Func<DataRow, bool>> byClaimStatus = (status) =>
            {
                return (r) =>
                {
                    string claim = r.Table.Columns.Contains("ClaimStatus") && r["ClaimStatus"] != DBNull.Value
                                   ? r["ClaimStatus"].ToString() : string.Empty;

                    if (string.Equals(status, "Submitted", StringComparison.OrdinalIgnoreCase))
                    {
                        // treat "Submitted" and "Pending" as submitted bucket
                        return string.Equals(claim, "Submitted", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(claim, "Pending", StringComparison.OrdinalIgnoreCase);
                    }

                    if (string.Equals(status, "Other", StringComparison.OrdinalIgnoreCase))
                    {
                        // other = not Draft/Submitted/Approved and not editable
                        bool isDraft = string.Equals(claim, "Draft", StringComparison.OrdinalIgnoreCase);
                        bool isSubmitted = string.Equals(claim, "Submitted", StringComparison.OrdinalIgnoreCase) || string.Equals(claim, "Pending", StringComparison.OrdinalIgnoreCase);
                        bool isApproved = string.Equals(claim, "Approved", StringComparison.OrdinalIgnoreCase);
                        bool isEditable = r.Table.Columns.Contains("IsEditable") && r["IsEditable"] != DBNull.Value && Convert.ToBoolean(r["IsEditable"]);
                        return !(isDraft || isSubmitted || isApproved || isEditable);
                    }

                    // normal equality
                    return string.Equals(claim, status, StringComparison.OrdinalIgnoreCase);
                };
            };

            Func<DataRow, bool> isEditablePredicate = (r) =>
            {
                // Editable: IsEditable==true but ClaimStatus != Draft
                bool isEditable = r.Table.Columns.Contains("IsEditable") && r["IsEditable"] != DBNull.Value && Convert.ToBoolean(r["IsEditable"]);
                string claim = r.Table.Columns.Contains("ClaimStatus") && r["ClaimStatus"] != DBNull.Value ? r["ClaimStatus"].ToString() : string.Empty;
                return isEditable && !string.Equals(claim, "Draft", StringComparison.OrdinalIgnoreCase);
            };

            // fill Draft
            decimal d_ta = SumMoney("CalculatedClaimAmount", byClaimStatus("Draft"));
            decimal d_da = SumMoney("DAAmount", byClaimStatus("Draft"));
            decimal d_hotel = SumMoney("HotelAmount", byClaimStatus("Draft"));
            decimal d_travel = SumMoney("TravelAmount", byClaimStatus("Draft"));
            decimal d_exp = SumMoney("ExpenseTotal", byClaimStatus("Draft"));
            decimal d_km = SumKM(byClaimStatus("Draft"));

            lblDraft_TA.Text = "₹ " + d_ta.ToString("N2");
            lblDraft_DA.Text = "₹ " + d_da.ToString("N2");
            lblDraft_Hotel.Text = "₹ " + d_hotel.ToString("N2");
            lblDraft_Travel.Text = "₹ " + d_travel.ToString("N2");
            lblDraft_Expense.Text = "₹ " + d_exp.ToString("N2");
            lblDraft_KM.Text = d_km.ToString("N2");

            // fill Submitted
            decimal s_ta = SumMoney("CalculatedClaimAmount", byClaimStatus("Submitted"));
            decimal s_da = SumMoney("DAAmount", byClaimStatus("Submitted"));
            decimal s_hotel = SumMoney("HotelAmount", byClaimStatus("Submitted"));
            decimal s_travel = SumMoney("TravelAmount", byClaimStatus("Submitted"));
            decimal s_exp = SumMoney("ExpenseTotal", byClaimStatus("Submitted"));
            decimal s_km = SumKM(byClaimStatus("Submitted"));

            lblSubmitted_TA.Text = "₹ " + s_ta.ToString("N2");
            lblSubmitted_DA.Text = "₹ " + s_da.ToString("N2");
            lblSubmitted_Hotel.Text = "₹ " + s_hotel.ToString("N2");
            lblSubmitted_Travel.Text = "₹ " + s_travel.ToString("N2");
            lblSubmitted_Expense.Text = "₹ " + s_exp.ToString("N2");
            lblSubmitted_KM.Text = s_km.ToString("N2");

            // fill Approved
            decimal a_ta = SumMoney("CalculatedClaimAmount", byClaimStatus("Approved"));
            decimal a_da = SumMoney("DAAmount", byClaimStatus("Approved"));
            decimal a_hotel = SumMoney("HotelAmount", byClaimStatus("Approved"));
            decimal a_travel = SumMoney("TravelAmount", byClaimStatus("Approved"));
            decimal a_exp = SumMoney("ExpenseTotal", byClaimStatus("Approved"));
            decimal a_km = SumKM(byClaimStatus("Approved"));

            lblApproved_TA.Text = "₹ " + a_ta.ToString("N2");
            lblApproved_DA.Text = "₹ " + a_da.ToString("N2");
            lblApproved_Hotel.Text = "₹ " + a_hotel.ToString("N2");
            lblApproved_Travel.Text = "₹ " + a_travel.ToString("N2");
            lblApproved_Expense.Text = "₹ " + a_exp.ToString("N2");
            lblApproved_KM.Text = a_km.ToString("N2");

            // fill Editable (IsEditable==true but not Draft)
            decimal e_ta = SumMoney("CalculatedClaimAmount", isEditablePredicate);
            decimal e_da = SumMoney("DAAmount", isEditablePredicate);
            decimal e_hotel = SumMoney("HotelAmount", isEditablePredicate);
            decimal e_travel = SumMoney("TravelAmount", isEditablePredicate);
            decimal e_exp = SumMoney("ExpenseTotal", isEditablePredicate);
            decimal e_km = SumKM(isEditablePredicate);

            lblEditable_TA.Text = "₹ " + e_ta.ToString("N2");
            lblEditable_DA.Text = "₹ " + e_da.ToString("N2");
            lblEditable_Hotel.Text = "₹ " + e_hotel.ToString("N2");
            lblEditable_Travel.Text = "₹ " + e_travel.ToString("N2");
            lblEditable_Expense.Text = "₹ " + e_exp.ToString("N2");
            lblEditable_KM.Text = e_km.ToString("N2");

            // fill Other
            decimal o_ta = SumMoney("CalculatedClaimAmount", byClaimStatus("Other"));
            decimal o_da = SumMoney("DAAmount", byClaimStatus("Other"));
            decimal o_hotel = SumMoney("HotelAmount", byClaimStatus("Other"));
            decimal o_travel = SumMoney("TravelAmount", byClaimStatus("Other"));
            decimal o_exp = SumMoney("ExpenseTotal", byClaimStatus("Other"));
            decimal o_km = SumKM(byClaimStatus("Other"));

            lblOther_TA.Text = "₹ " + o_ta.ToString("N2");
            lblOther_DA.Text = "₹ " + o_da.ToString("N2");
            lblOther_Hotel.Text = "₹ " + o_hotel.ToString("N2");
            lblOther_Travel.Text = "₹ " + o_travel.ToString("N2");
            lblOther_Expense.Text = "₹ " + o_exp.ToString("N2");
            lblOther_KM.Text = o_km.ToString("N2");
        }

    }
}