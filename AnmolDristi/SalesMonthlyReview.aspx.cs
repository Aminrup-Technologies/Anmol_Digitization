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

namespace AnmolDristi
{
    public partial class SalesMonthlyReview : BasePage
    {
        private string connString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                PopulateYearMonth();
                lbl_msg.Text = "";
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

        protected void btn_fetch_Click(object sender, EventArgs e) => BindGrid();

        private void BindGrid()
        {
            lbl_msg.Text = "";
            int year = int.Parse(DDL_Year.SelectedValue);
            int month = int.Parse(DDL_Month.SelectedValue);

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

            gvActivities.DataSource = dt;
            gvActivities.DataBind();
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

            //    //if (!string.IsNullOrWhiteSpace(activityDate))
            //    //{
            //    //    TXT_HotelFromDate.Text = activityDate; // defaults to activity date
            //    //    TXT_HotelToDate.Text = activityDate;
            //    //}
            //    //else
            //    //{
            //    //    TXT_HotelFromDate.Text = "";
            //    //    TXT_HotelToDate.Text = "";
            //    //}

            //    // Load existing expenses
            //    LoadExpenseForActivity(activityId);

            //    // Try to parse and set the hotel date controls in yyyy-MM-dd format (for input[type=date])
            //    DateTime parsed;
            //    if (!string.IsNullOrWhiteSpace(activityDate) && DateTime.TryParse(activityDate, out parsed))
            //    {
            //        TXT_HotelFromDate.Text = parsed.ToString("yyyy-MM-dd");
            //        TXT_HotelToDate.Text = parsed.ToString("yyyy-MM-dd");
            //    }
            //    else
            //    {
            //        // fallback to activity date (if missing) or today's IST date
            //        var ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            //        var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ist);
            //        TXT_HotelFromDate.Text = nowIst.ToString("yyyy-MM-dd");
            //        TXT_HotelToDate.Text = nowIst.ToString("yyyy-MM-dd");
            //    }

            //    // show modal
            //    ClientScript.RegisterStartupScript(this.GetType(), "showExpense", "showExpenseModal();", true);
            //}

            if (e.CommandName == "OpenExpenses")
            {
                string arg = e.CommandArgument.ToString();
                var parts = arg.Split('|');
                string activityId = parts[0];
                string activityDate = parts.Length > 1 ? parts[1] : "";

                HF_ActivityId.Value = activityId;
                HF_ActivityDate.Value = activityDate;

                // Load existing expenses and get the DataTable back
                DataTable dt = LoadExpenseForActivity(activityId);

                // If no expense rows exist for this activity, CLEAR the modal inputs so user can enter new ones
                if (dt == null || dt.Rows.Count == 0)
                {
                    TXT_DAAmount.Text = string.Empty;
                    TXT_HotelAmount.Text = string.Empty;
                    TXT_HotelBillNo.Text = string.Empty;
                    TXT_HotelPurpose.Text = string.Empty;

                    // Set hotel dates to activity date if available, else today's IST
                    DateTime parsed;
                    if (!string.IsNullOrWhiteSpace(activityDate) && DateTime.TryParse(activityDate, out parsed))
                    {
                        TXT_HotelFromDate.Text = parsed.ToString("yyyy-MM-dd");
                        TXT_HotelToDate.Text = parsed.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        var ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                        var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ist);
                        TXT_HotelFromDate.Text = nowIst.ToString("yyyy-MM-dd");
                        TXT_HotelToDate.Text = nowIst.ToString("yyyy-MM-dd");
                    }

                    // Clear any prior messages
                    LBL_ExpenseMsg.Text = "";

                    // Clear the file input via client script (FileUpload value cannot be set server-side)
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "clearFileInput_" + FU_Attachment.ClientID, $"$('#{FU_Attachment.ClientID}').val('');", true);
                }
                else
                {
                    // aggregate/collect values (handles any combination of DA/Hotel rows)
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
                    //TXT_DAAmount.Text = daSum > 0m ? daSum.ToString("0.##") : string.Empty;
                    //TXT_HotelAmount.Text = hotelSum > 0m ? hotelSum.ToString("0.##") : string.Empty;

                    TXT_HotelFromDate.Text = hotelFrom.HasValue ? hotelFrom.Value.ToString("yyyy-MM-dd") : string.Empty;
                    TXT_HotelToDate.Text = hotelTo.HasValue ? hotelTo.Value.ToString("yyyy-MM-dd") : string.Empty;

                    //TXT_HotelBillNo.Text = hotelBill;
                    //TXT_HotelPurpose.Text = hotelPurpose;

                    // Keep existing attachments visible in the Repeater. Do NOT clear the FileUpload here.
                    LBL_ExpenseMsg.Text = string.Empty;
                }



                // show modal reliably
                ScriptManager.RegisterStartupScript(this, this.GetType(), "showExpense", "$('#expenseModal').modal('show');", true);
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
                        BindGrid();
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
                    BindGrid();
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

        protected void gvActivities_RowDataBound(object sender, GridViewRowEventArgs e)
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
            BindGrid();
        }

        protected void gvActivities_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            gvActivities.EditIndex = -1;
            BindGrid();
        }

        protected void gvActivities_RowUpdating(object sender, GridViewUpdateEventArgs e)
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
                BindGrid();
            }
            catch (SqlException sqx)
            {
                // surface DB-raised message (RAISERROR / THROW)
                lbl_msg.ForeColor = System.Drawing.Color.Red;
                lbl_msg.Text = sqx.Message;
                // keep row in edit mode
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
                if (FU_Attachment.HasFile)
                {
                    if (FU_Attachment.PostedFile.ContentLength > 5 * 1024 * 1024)
                    {
                        LBL_ExpenseMsg.Text = "Attachment too large (max 5MB).";
                        ClientScript.RegisterStartupScript(this.GetType(), "showExpense", "showExpenseModal();", true);
                        return;
                    }

                    string ext = Path.GetExtension(FU_Attachment.FileName).ToLower();
                    var allowed = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                    if (Array.IndexOf(allowed, ext) < 0)
                    {
                        LBL_ExpenseMsg.Text = "Invalid file type. Allowed: JPG, PNG, PDF.";
                        ClientScript.RegisterStartupScript(this.GetType(), "showExpense", "showExpenseModal();", true);
                        return;
                    }

                    // Build folder: UploadedFiles/ClaimsAttachment/{yyyy}/{MM}/
                    string baseFolder = Server.MapPath("~/UploadedFiles/ClaimsAttachment/");
                    string year = DateTime.UtcNow.AddMinutes(330).ToString("yyyy");
                    string month = DateTime.UtcNow.AddMinutes(330).ToString("MM");
                    string uploadsFolder = Path.Combine(baseFolder, year, month);
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string safeLogin = GetCurrentUserLoginId().Replace(" ", "_");
                    string uniquePart = DateTime.UtcNow.AddMinutes(330).ToString("yyyyMMddHHmmss") + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    savedFileName = $"{safeLogin}_{activityId}_{uniquePart}{ext}";
                    string savedFullPath = Path.Combine(uploadsFolder, savedFileName);
                    FU_Attachment.SaveAs(savedFullPath);

                    // Save relative path (recommended) so downloads can map correctly
                    savedRelativePath = Path.Combine("UploadedFiles/ClaimsAttachment", year, month, savedFileName).Replace("\\", "/");
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
                BindGrid();
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
                            //cmd.Parameters.AddWithValue("@AttachmentFileName", savedRelativePath ?? (object)DBNull.Value);
                            //cmd.Parameters.AddWithValue("@AttachmentContent", DBNull.Value);
                            // AttachmentFileName gets the relative path string (or NULL)
                            cmd.Parameters.AddWithValue("@AttachmentFileName", savedRelativePath ?? (object)DBNull.Value);

                            // AttachmentContent must be VARBINARY; set to NULL explicitly
                            //cmd.Parameters.AddWithValue("@AttachmentContent", savedFileName);
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
                            cmd.Parameters.AddWithValue("@AttachmentFileName", savedRelativePath ?? (object)DBNull.Value);
                            // AttachmentContent must be VARBINARY; set to NULL explicitly
                            //cmd.Parameters.AddWithValue("@AttachmentContent", savedFileName);
                            //byte[] fileBytes = FU_Attachment.FileBytes;
                            //cmd.Parameters.Add("@AttachmentContent", SqlDbType.VarBinary, -1).Value = fileBytes;
                            cmd.Parameters.Add("@AttachmentContent", SqlDbType.VarBinary, -1).Value = new byte[0];
                            cmd.Parameters.AddWithValue("@CreatedBy", GetCurrentUserLoginId());
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                // reload list and UI
                //LoadExpenseForActivity(activityId.ToString());

                DataTable dtAfter = LoadExpenseForActivity(activityId.ToString());
                BindGrid();

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

        private DataTable LoadExpenseForActivity(string activityId)
        {
            DataTable dt = new DataTable();
            using (SqlConnection con = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand(@"SELECT ExpenseId, ActivityId, ExpenseType, ExpenseDate,Purpose, FromDate, ToDate, BillNo, Amount, AttachmentFileName
                                            FROM dbo.SalesDailyExpense 
                                            WHERE ActivityId = @ActivityId 
                                            ORDER BY ExpenseDate, ExpenseId", con))
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
            ScriptManager.RegisterStartupScript(this, this.GetType(), "flashTotals", "$('#lblOverallTotal').fadeOut(100).fadeIn(100);", true);
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


        protected void btn_save_draft_Click(object sender, EventArgs e)
        {
            BindGrid();
            lbl_msg.ForeColor = System.Drawing.Color.Green;
            lbl_msg.Text = "Draft saved.";
        }

        protected void btn_submit_Click(object sender, EventArgs e)
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

                BindGrid();
            }
            catch (SqlException sqx)
            {
                lbl_msg.ForeColor = System.Drawing.Color.Red;
                lbl_msg.Text = sqx.Message; // SP RAISERROR/THROW message bubbled here
            }
        }

        #region Helpers
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
            catch
            {
                // swallow and return null
            }

            return null;
        }

        #endregion

        protected void rptExpenses_ItemCommand(object source, RepeaterCommandEventArgs e)
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
                    using (SqlCommand chk = new SqlCommand(
                        @"SELECT a.ClaimStatus
                          FROM dbo.SalesDailyExpense e
                          INNER JOIN dbo.SalesDailyActivity a ON e.ActivityId = a.ActivityId
                          WHERE e.ExpenseId = @ExpenseId", con))
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
                BindGrid(); // refresh grid totals/statuses
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

    }
}