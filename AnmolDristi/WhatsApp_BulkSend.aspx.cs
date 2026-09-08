using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class WhatsApp_BulkSend : System.Web.UI.Page
    {
        private const string SESSION_DATA = "WhatsApp_ExcelData";
        private const string SESSION_CONFIG = "WhatsApp_CampaignConfig";

        private WhatsAppService Service { get { return new WhatsAppService(); } }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                pnlPreview.Visible = false;
                pnlResults.Visible = false;
            }
        }

        // ============================================================
        //  STEP 2 — UPLOAD EXCEL
        // ============================================================
        protected void btnUpload_Click(object sender, EventArgs e)
        {
            ClearAlerts();
            pnlResults.Visible = false;

            if (!fuExcel.HasFile)
            {
                ShowError("Please select an Excel file (.xlsx or .xls) to upload.");
                return;
            }

            string ext = Path.GetExtension(fuExcel.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
            {
                ShowError("Invalid file type. Only .xlsx and .xls files are accepted.");
                return;
            }

            try
            {
                DataTable dt;
                using (var stream = fuExcel.PostedFile.InputStream)
                {
                    dt = Service.ParseExcelToDataTable(stream, fuExcel.FileName);
                }

                if (dt.Rows.Count == 0)
                {
                    ShowError("The Excel file contains headers but no data rows.");
                    return;
                }

                // Store in session for postbacks
                Session[SESSION_DATA] = dt;

                // Populate preview (first 5 rows)
                var previewDt = dt.Rows.Count > 5 ? dt.Clone() : dt;
                for (int i = 0; i < Math.Min(5, dt.Rows.Count); i++)
                    previewDt.ImportRow(dt.Rows[i]);

                gvPreview.DataSource = previewDt;
                gvPreview.DataBind();

                // Populate mapping table
                var columns = dt.Columns.Cast<DataColumn>()
                    .Select(c => new { ExcelColumn = c.ColumnName })
                    .ToList();

                gvMapping.DataSource = columns;
                gvMapping.DataBind();

                // Auto-select first column as phone if it looks like a phone column
                string phoneHint = columns.FirstOrDefault(c =>
                    c.ExcelColumn.ToLowerInvariant().Contains("phone") ||
                    c.ExcelColumn.ToLowerInvariant().Contains("mobile") ||
                    c.ExcelColumn.ToLowerInvariant().Contains("whatsapp"))?.ExcelColumn;

                if (phoneHint != null)
                {
                    foreach (GridViewRow row in gvMapping.Rows)
                    {
                        var lbl = row.FindControl("lblVarIndex") as Label;
                        if (lbl != null && lbl.Text == phoneHint)
                        {
                            var rb = row.FindControl("rbPhone") as RadioButton;
                            if (rb != null) rb.Checked = true;
                        }
                    }
                }

                // Default: first column = phone
                if (phoneHint == null && gvMapping.Rows.Count > 0)
                {
                    var rb = gvMapping.Rows[0].FindControl("rbPhone") as RadioButton;
                    if (rb != null) rb.Checked = true;
                }

                pnlPreview.Visible = true;
                pnlValidation.Visible = false;
                btnSend.Visible = false;

                ShowOk(string.Format("Uploaded <strong>{0}</strong> with <strong>{1} rows</strong> and <strong>{2} columns</strong>.",
                    HttpUtility.HtmlEncode(fuExcel.FileName), dt.Rows.Count, dt.Columns.Count));
            }
            catch (Exception ex)
            {
                ShowError("Error reading Excel file: " + ex.Message);
            }
        }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            Session.Remove(SESSION_DATA);
            Session.Remove(SESSION_CONFIG);
            pnlPreview.Visible = false;
            pnlResults.Visible = false;
            ClearAlerts();
        }

        // ============================================================
        //  STEP 3 — VALIDATE & MAP
        // ============================================================
        protected void btnValidate_Click(object sender, EventArgs e)
        {
            ClearAlerts();

            var dt = Session[SESSION_DATA] as DataTable;
            if (dt == null)
            {
                ShowError("No data loaded. Please upload an Excel file first.");
                return;
            }

            // Read mappings from the GridView
            var mappings = ReadMappings();
            var phoneMapping = mappings.FirstOrDefault(m => m.IsPhone);

            if (phoneMapping == null)
            {
                ShowError("Please select a phone number column in the mapping table.");
                return;
            }

            // Run validation
            var validation = Service.ValidateData(dt, phoneMapping);

            lblValidCount.Text = "Valid rows: " + validation.ValidRows;
            lblInvalidCount.Text = validation.InvalidRows > 0
                ? "Invalid/duplicate rows: " + validation.InvalidRows
                : "";

            rptWarnings.DataSource = validation.Warnings.Select(w => new { Text = w });
            rptWarnings.DataBind();

            pnlValidation.Visible = true;

            if (validation.IsValid || validation.ValidRows > 0)
            {
                btnSend.Visible = true;

                // Store config in session for Step 4
                var config = new CampaignConfig
                {
                    BaseUrl = txtBaseUrl.Text.Trim(),
                    ApiKey = txtApiKey.Text.Trim(),
                    PhoneId = txtPhoneId.Text.Trim(),
                    CampaignName = txtCampaignName.Text.Trim(),
                    TemplateName = txtTemplateName.Text.Trim(),
                    ColumnMappings = mappings
                };
                Session[SESSION_CONFIG] = config;

                if (!validation.IsValid)
                    ShowError("Some rows are invalid. Only valid rows will be included in the campaign.");
            }
            else
            {
                btnSend.Visible = false;
                ShowError("No valid rows found. Please check your data and column mapping.");
            }
        }

        // ============================================================
        //  STEP 4 — SEND
        // ============================================================
        protected void btnSend_Click(object sender, EventArgs e)
        {
            ClearAlerts();

            var dt = Session[SESSION_DATA] as DataTable;
            var config = Session[SESSION_CONFIG] as CampaignConfig;

            if (dt == null || config == null)
            {
                ShowError("Session expired. Please upload the file again.");
                pnlPreview.Visible = false;
                return;
            }

            // Re-validate: confirm required fields
            if (string.IsNullOrEmpty(config.ApiKey)) { ShowError("API Key is required."); return; }
            if (string.IsNullOrEmpty(config.CampaignName)) { ShowError("Campaign Name is required."); return; }
            if (string.IsNullOrEmpty(config.TemplateName)) { ShowError("Template Name is required."); return; }

            var phoneMapping = config.ColumnMappings.FirstOrDefault(m => m.IsPhone);
            if (phoneMapping == null) { ShowError("Phone column mapping is missing."); return; }

            try
            {
                // Build filtered DataTable (valid rows only)
                var filteredDt = dt.Clone();
                var seenPhones = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (DataRow row in dt.Rows)
                {
                    string phone = Service.ValidateAndNormalizePhone(row[phoneMapping.ExcelColumn] as string ?? "");
                    if (string.IsNullOrEmpty(phone)) continue;
                    if (!seenPhones.Add(phone)) continue;
                    filteredDt.ImportRow(row);
                }

                if (filteredDt.Rows.Count == 0)
                {
                    ShowError("No valid recipients to send to.");
                    return;
                }

                // Convert to CSV
                byte[] csvBytes = Service.ConvertToCsvBytes(filteredDt, config);

                // Build and store the CampaignData JSON for display
                string campaignJson = Service.BuildCampaignJson(config);
                litCampaignJson.Text = System.Net.WebUtility.HtmlEncode(campaignJson);

                // Send to CEOITBOX
                var result = Service.SendBulkCampaign(config, csvBytes);

                pnlResults.Visible = true;

                if (result.Success)
                {
                    pnlResultOk.Visible = true;
                    pnlResultErr.Visible = false;
                    lblResultOk.Text = string.Format(" {0} recipients submitted. Response code: {1}",
                        filteredDt.Rows.Count, result.StatusCode);
                }
                else
                {
                    pnlResultOk.Visible = false;
                    pnlResultErr.Visible = true;
                    lblResultErr.Text = " " + result.ErrorMessage;
                }

                litResponseBody.Text = System.Net.WebUtility.HtmlEncode(
                    result.ResponseBody.Length > 4000
                        ? result.ResponseBody.Substring(0, 4000) + "\n... (truncated)"
                        : result.ResponseBody);
            }
            catch (Exception ex)
            {
                pnlResults.Visible = true;
                pnlResultOk.Visible = false;
                pnlResultErr.Visible = true;
                lblResultErr.Text = " Unexpected error: " + ex.Message;
                litResponseBody.Text = "";
            }
        }

        // ============================================================
        //  HELPER METHODS
        // ============================================================
        protected void gvMapping_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var lbl = e.Row.FindControl("lblVarIndex") as Label;
            if (lbl != null)
                lbl.Text = DataBinder.Eval(e.Row.DataItem, "ExcelColumn") as string ?? "";
        }

        /// <summary>
        /// Reads column mappings from the mapping GridView.
        /// </summary>
        private List<ColumnMapping> ReadMappings()
        {
            var mappings = new List<ColumnMapping>();
            int varIndex = 1;

            foreach (GridViewRow row in gvMapping.Rows)
            {
                var lbl = row.FindControl("lblVarIndex") as Label;
                var rb = row.FindControl("rbPhone") as RadioButton;
                var chk = row.FindControl("chkVariable") as CheckBox;

                if (lbl == null) continue;

                string column = lbl.Text;

                bool isPhone = rb != null && rb.Checked;
                bool isVar = chk != null && chk.Checked && !isPhone;

                mappings.Add(new ColumnMapping
                {
                    ExcelColumn = column,
                    IsPhone = isPhone,
                    IsVariable = isVar,
                    VariableIndex = isVar ? varIndex++ : 0
                });
            }

            return mappings;
        }

        private void ShowOk(string message)
        {
            pnlUploadOk.Visible = true;
            pnlUploadErr.Visible = false;
            lblUploadOk.Text = message;
        }

        private void ShowError(string message)
        {
            pnlUploadErr.Visible = true;
            lblUploadErr.Text = message;
        }

        private void ClearAlerts()
        {
            pnlUploadOk.Visible = false;
            pnlUploadErr.Visible = false;
        }
    }
}