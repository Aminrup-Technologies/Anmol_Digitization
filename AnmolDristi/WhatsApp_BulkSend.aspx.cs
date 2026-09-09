using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

// Type aliases — resolve the ContentType/ContentMode enums explicitly.
// The ASP.NET WebForms page compiler resolves the bare name 'ContentType'
// to the string-typed Response.ContentType property in the generated page
// context, so we MUST use the aliases below for every enum reference.
using ContentType = AnmolDristi.ContentType;
using ContentMode = AnmolDristi.ContentMode;

namespace AnmolDristi
{
    public partial class WhatsApp_BulkSend : System.Web.UI.Page
    {
        private const string SESSION_DATA = "WhatsApp_ExcelData";
        private const string SESSION_CONFIG = "WhatsApp_CampaignConfig";
        private const string SESSION_PARSE_DIAG = "WhatsApp_ParseDiagnostics";

        // Phase 2 session keys — metadata only; binaries stay in temp files on disk
        private const string SESSION_CONTENT = "WhatsApp_ContentConfig";
        private const string SESSION_DISPATCH_PLAN = "WhatsApp_DispatchPlan";

        private WhatsAppService Service { get { return new WhatsAppService(); } }

        /// <summary>
        /// Reads CEOITBOX configuration from Web.config appSettings.
        /// Credentials never appear in the UI.
        /// </summary>
        private CEOITBOXConfig LoadApiConfig()
        {
            return new CEOITBOXConfig
            {
                BaseUrl = ConfigurationManager.AppSettings["CEOITBOX.BaseUrl"] ?? "https://official.ceoitbox.com",
                ApiKey = ConfigurationManager.AppSettings["CEOITBOX.ApiKey"] ?? "",
                PhoneId = ConfigurationManager.AppSettings["CEOITBOX.PhoneId"] ?? "",
                UseBasicAuthentication = string.Equals(
                    ConfigurationManager.AppSettings["CEOITBOX.UseBasicAuthentication"], "true",
                    StringComparison.OrdinalIgnoreCase),
                Username = ConfigurationManager.AppSettings["CEOITBOX.Username"] ?? "",
                Password = ConfigurationManager.AppSettings["CEOITBOX.Password"] ?? ""
            };
        }

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
                ShowError("Please select an Excel file (.xlsx) to upload.");
                return;
            }

            string ext = Path.GetExtension(fuExcel.FileName).ToLowerInvariant();
            if (ext != ".xlsx")
            {
                ShowError("Invalid file type. Only .xlsx files are supported in this phase.");
                return;
            }

            try
            {
                // ---- CRITICAL: Clear ALL previous campaign state before parsing ----
                // This ensures a fresh upload never mixes with old session data.
                Session.Remove(SESSION_DATA);
                Session.Remove(SESSION_CONFIG);
                Session.Remove(SESSION_PARSE_DIAG);
                Session.Remove(SESSION_DISPATCH_PLAN);
                pnlDispatchPlan.Visible = false;
                pnlDispatchResults.Visible = false;

                DataTable dt;
                ParseDiagnostics parseDiag;
                using (var stream = fuExcel.PostedFile.InputStream)
                {
                    dt = Service.ParseExcelToDataTable(stream, fuExcel.FileName, out parseDiag);
                }

                // Store diagnostics for display
                Session[SESSION_PARSE_DIAG] = parseDiag;

                // Diagnostic: log the parse result
                System.Diagnostics.Debug.WriteLine(
                    string.Format("[WhatsApp Upload] Worksheet rows: {0}, Header: \"{1}\", Expected data rows: {2}, Empty skipped: {3}, Parsed DataTable rows: {4}, Columns: {5} ({6})",
                        parseDiag.WorksheetEndRow, parseDiag.HeaderRowContent, parseDiag.ExpectedDataRows,
                        parseDiag.EmptyRowsSkipped, parseDiag.DataTableRows, parseDiag.DataTableColumns, parseDiag.ColumnNames));

                if (dt.Rows.Count == 0)
                {
                    ShowError("The Excel file contains headers but no data rows.");
                    return;
                }

                // ---- Store in session — REPLACE, never append ----
                Session[SESSION_DATA] = dt;

                // Diagnostic: confirm stored row count
                System.Diagnostics.Debug.WriteLine(
                    string.Format("[WhatsApp Upload] Session DataTable rows after store: {0}",
                    ((DataTable)Session[SESSION_DATA]).Rows.Count));

                // Populate preview (first 5 rows)
                var previewDt = dt.Clone();
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

                // Phase 2: populate mapping/URL column dropdowns from the uploaded Excel
                BindColumnDropdowns(dt);

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
            // Phase 2: clean temp files + session state
            CleanupContentTempFiles();
            Session.Remove(SESSION_DATA);
            Session.Remove(SESSION_CONFIG);
            Session.Remove(SESSION_PARSE_DIAG);
            Session.Remove(SESSION_CONTENT);
            Session.Remove(SESSION_DISPATCH_PLAN);
            pnlPreview.Visible = false;
            pnlResults.Visible = false;
            pnlDispatchPlan.Visible = false;
            pnlDispatchResults.Visible = false;
            ClearAlerts();
        }

        // ============================================================
        //  PHASE 2 — CONTENT UPLOAD HANDLERS
        // ============================================================

        private TemplateHeaderFormat GetSelectedHeaderFormat()
        {
            switch (ddlTemplateHeader.SelectedValue)
            {
                case "Image": return TemplateHeaderFormat.Image;
                case "Document": return TemplateHeaderFormat.Document;
                case "Video": return TemplateHeaderFormat.Video;
                default: return TemplateHeaderFormat.BodyOnly;
            }
        }

        private ContentMode GetSelectedContentMode()
        {
            if (rbModeCommon.Checked) return ContentMode.CommonForAll;
            if (rbModeIndividual.Checked) return ContentMode.IndividualFile;
            if (rbModeWorkbook.Checked) return ContentMode.WorkbookSheet;
            if (rbModeUrl.Checked) return ContentMode.ExcelColumnUrl;
            return ContentMode.None;
        }

        protected void btnUploadCommon_Click(object sender, EventArgs e)
        {
            ShowContentAlerts(null, null);

            if (!fuCommonContent.HasFile)
            {
                ShowContentAlerts(null, "Please select a file to upload.");
                return;
            }

            try
            {
                var svc = Service;
                var headerFormat = GetSelectedHeaderFormat();

                // Validate BEFORE saving — extension/MIME/size/template compatibility
                string validationError = svc.ValidateContent(fuCommonContent.FileName, fuCommonContent.PostedFile.ContentLength, headerFormat);
                if (validationError != null)
                {
                    ShowContentAlerts(null, validationError);
                    return;
                }

                // Remove previous common file if any
                var existing = Session[SESSION_CONTENT] as ContentConfig;
                if (existing != null && !string.IsNullOrEmpty(existing.TempFilePath))
                    WhatsAppService.CleanupTempFile(existing.TempFilePath);

                string displayName;
                string tempPath = svc.SaveToTempFile(fuCommonContent.PostedFile.InputStream, fuCommonContent.FileName, out displayName);

                var content = existing ?? new ContentConfig();
                content.Mode = ContentMode.CommonForAll;
                content.MediaType = ContentTypeFromMime(WhatsAppService.GetMimeTypeFromExtension(fuCommonContent.FileName));
                content.FileName = displayName;
                content.TempFilePath = tempPath;
                content.MimeType = WhatsAppService.GetMimeTypeFromExtension(fuCommonContent.FileName);
                Session[SESSION_CONTENT] = content;

                trCommonInfo.Visible = true;
                lblCommonFileInfo.Text = string.Format("{0} ({1:N1} KB) — ready. Click Validate to build the dispatch plan.",
                    HttpUtility.HtmlEncode(displayName), fuCommonContent.PostedFile.ContentLength / 1024.0);
                ShowContentAlerts("Attachment uploaded: " + HttpUtility.HtmlEncode(displayName), null);
            }
            catch (Exception ex)
            {
                ShowContentAlerts(null, "Upload failed: " + HttpUtility.HtmlEncode(ex.Message));
            }
        }

        protected void btnUploadIndividual_Click(object sender, EventArgs e)
        {
            ShowContentAlerts(null, null);

            if (fuIndividualFiles.PostedFiles == null || fuIndividualFiles.PostedFiles.Count == 0)
            {
                ShowContentAlerts(null, "Please select one or more files to upload.");
                return;
            }

            try
            {
                var svc = Service;
                var headerFormat = GetSelectedHeaderFormat();

                // Validate ALL files first — reject the batch if any file is invalid
                var validationErrors = new List<string>();
                foreach (var pf in fuIndividualFiles.PostedFiles)
                {
                    string err = svc.ValidateContent(pf.FileName, pf.ContentLength, headerFormat);
                    if (err != null) validationErrors.Add(pf.FileName + ": " + err);
                }
                if (validationErrors.Count > 0)
                {
                    ShowContentAlerts(null, string.Join("<br/>", validationErrors.Select(HttpUtility.HtmlEncode).Take(10)));
                    return;
                }

                // Fresh temp directory per upload
                var existing = Session[SESSION_CONTENT] as ContentConfig;
                if (existing != null && !string.IsNullOrEmpty(existing.IndividualFilesDir))
                    WhatsAppService.CleanupTempDirectory(existing.IndividualFilesDir);

                string tempDir = System.IO.Path.Combine(
                    ConfigurationHelper.GetStringSetting("CEOITBOX.TempFileDirectory",
                        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CEOITBOX_WhatsApp")),
                    Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                var fileMap = new Dictionary<string, string>();
                var pathMap = new Dictionary<string, string>();
                foreach (var pf in fuIndividualFiles.PostedFiles)
                {
                    // Mapping key = file base name (no extension). Disk name is always a fresh GUID —
                    // the original filename is never used as a server path.
                    string baseName = System.IO.Path.GetFileNameWithoutExtension(pf.FileName);
                    string fileExt = System.IO.Path.GetExtension(pf.FileName);
                    if (fileExt.Length > 10 || fileExt.Contains("..")) fileExt = "";
                    string safeName = Guid.NewGuid().ToString("N") + fileExt;
                    string safePath = System.IO.Path.Combine(tempDir, safeName);

                    using (var fs = System.IO.File.Create(safePath))
                    {
                        pf.InputStream.Position = 0;
                        pf.InputStream.CopyTo(fs);
                    }

                    // key → original display name (for preview) and key → safe server path (for sends)
                    fileMap[baseName] = System.IO.Path.GetFileName(pf.FileName);
                    pathMap[baseName] = safePath;
                }

                var content = new ContentConfig
                {
                    Mode = ContentMode.IndividualFile,
                    MediaType = ContentType.Document,   // refined per-item during dispatch plan
                    MappingColumn = ddlIndividualMappingColumn.SelectedValue,
                    IndividualFilesDir = tempDir,
                    IndividualFileNames = fileMap,
                    IndividualFilePaths = pathMap
                };
                Session[SESSION_CONTENT] = content;

                trIndividualInfo.Visible = true;
                lblIndividualFileInfo.Text = string.Format("{0} files stored. Mapping column: {1}. Click Validate to build the dispatch plan.",
                    fileMap.Count, HttpUtility.HtmlEncode(content.MappingColumn));
                ShowContentAlerts(fileMap.Count + " files uploaded.", null);
            }
            catch (Exception ex)
            {
                ShowContentAlerts(null, "Upload failed: " + HttpUtility.HtmlEncode(ex.Message));
            }
        }

        protected void btnUploadWorkbook_Click(object sender, EventArgs e)
        {
            ShowContentAlerts(null, null);

            if (!fuWorkbook.HasFile)
            {
                ShowContentAlerts(null, "Please select a workbook to upload.");
                return;
            }

            string ext = System.IO.Path.GetExtension(fuWorkbook.FileName).ToLowerInvariant();
            if (ext != ".xlsx")
            {
                ShowContentAlerts(null, "Only .xlsx workbooks are supported for sheet-per-recipient mode.");
                return;
            }

            try
            {
                var svc = Service;

                // Remove previous workbook if any
                var existing = Session[SESSION_CONTENT] as ContentConfig;
                if (existing != null && !string.IsNullOrEmpty(existing.WorkbookTempPath))
                    WhatsAppService.CleanupTempFile(existing.WorkbookTempPath);

                string displayName;
                string tempPath = svc.SaveToTempFile(fuWorkbook.PostedFile.InputStream, fuWorkbook.FileName, out displayName);

                // Read sheet names ONLY — the workbook stays on disk, never in Session
                var sheetNames = new List<string>();
                using (var fs = System.IO.File.OpenRead(tempPath))
                using (var pkg = new OfficeOpenXml.ExcelPackage(fs))
                {
                    foreach (var ws in pkg.Workbook.Worksheets)
                        sheetNames.Add(ws.Name);
                }

                var content = new ContentConfig
                {
                    Mode = ContentMode.WorkbookSheet,
                    MediaType = ContentType.Document,
                    FileName = displayName,
                    WorkbookTempPath = tempPath,
                    WorkbookSheetNames = sheetNames,
                    MappingColumn = ddlWorkbookMappingColumn.SelectedValue
                };
                Session[SESSION_CONTENT] = content;

                trWorkbookInfo.Visible = true;
                lblWorkbookFileInfo.Text = string.Format("{0} — {1} sheets: {2}. Click Validate to build the dispatch plan.",
                    HttpUtility.HtmlEncode(displayName), sheetNames.Count,
                    HttpUtility.HtmlEncode(string.Join(", ", sheetNames.Take(10)) + (sheetNames.Count > 10 ? "…" : "")));
                ShowContentAlerts("Workbook uploaded with " + sheetNames.Count + " sheets.", null);
            }
            catch (Exception ex)
            {
                ShowContentAlerts(null, "Upload failed: " + HttpUtility.HtmlEncode(ex.Message));
            }
        }

        private ContentType ContentTypeFromMime(string mime)
        {
            if (string.IsNullOrEmpty(mime)) return ContentType.None;
            if (mime.StartsWith("image/")) return ContentType.Image;
            if (mime.StartsWith("video/")) return ContentType.Video;
            return ContentType.Document;
        }

        private void ShowContentAlerts(string ok, string error)
        {
            if (ok != null) { lblContentOk.Text = ok; lblContentOk.Visible = true; } else lblContentOk.Visible = false;
            if (error != null) { lblContentErr.Text = error; lblContentErr.Visible = true; } else lblContentErr.Visible = false;
        }

        private void CleanupContentTempFiles()
        {
            var content = Session[SESSION_CONTENT] as ContentConfig;
            if (content == null) return;
            if (!string.IsNullOrEmpty(content.TempFilePath)) WhatsAppService.CleanupTempFile(content.TempFilePath);
            if (!string.IsNullOrEmpty(content.IndividualFilesDir)) WhatsAppService.CleanupTempDirectory(content.IndividualFilesDir);
            if (!string.IsNullOrEmpty(content.WorkbookTempPath)) WhatsAppService.CleanupTempFile(content.WorkbookTempPath);
        }

        // ============================================================
        //  PHASE 2 — COLUMN DROPDOWN BINDING
        // ============================================================
        private void BindColumnDropdowns(DataTable dt)
        {
            var columns = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();

            ddlIndividualMappingColumn.DataSource = columns;
            ddlIndividualMappingColumn.DataBind();

            ddlWorkbookMappingColumn.DataSource = columns;
            ddlWorkbookMappingColumn.DataBind();

            ddlUrlColumn.DataSource = columns;
            ddlUrlColumn.DataBind();

            // Auto-hint: a column that looks like a mapping key
            string hint = columns.FirstOrDefault(c =>
                c.IndexOf("employee", StringComparison.OrdinalIgnoreCase) >= 0 ||
                c.IndexOf("empcode", StringComparison.OrdinalIgnoreCase) >= 0 ||
                c.IndexOf("vendor", StringComparison.OrdinalIgnoreCase) >= 0 ||
                c.IndexOf("customer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                c.IndexOf("invoice", StringComparison.OrdinalIgnoreCase) >= 0);
            if (hint != null)
            {
                ddlIndividualMappingColumn.SelectedValue = hint;
                ddlWorkbookMappingColumn.SelectedValue = hint;
            }

            // Auto-hint: URL column
            string urlHint = columns.FirstOrDefault(c =>
                c.IndexOf("url", StringComparison.OrdinalIgnoreCase) >= 0 ||
                c.IndexOf("link", StringComparison.OrdinalIgnoreCase) >= 0);
            if (urlHint != null)
                ddlUrlColumn.SelectedValue = urlHint;
        }

        // ============================================================
        //  STEP 3 — VALIDATE & MAP
        // ============================================================
        protected void btnValidate_Click(object sender, EventArgs e)
        {
            ClearAlerts();

            // Clear previous validation results before running new validation
            lblTotalCount.Text = "";
            lblValidCount.Text = "";
            lblInvalidCount.Text = "";
            lblDuplicateCount.Text = "";
            lblWarningsInfo.Visible = false;
            lblParseInfo.Visible = false;
            rptWarnings.DataSource = null;
            rptWarnings.DataBind();

            var dt = Session[SESSION_DATA] as DataTable;
            if (dt == null)
            {
                ShowError("No data loaded. Please upload an Excel file first.");
                return;
            }

            // Diagnostic: confirm row count from session
            System.Diagnostics.Debug.WriteLine(
                string.Format("[WhatsApp Validation] Input DataTable rows from Session: {0}", dt.Rows.Count));
            System.Diagnostics.Debug.WriteLine(
                string.Format("[WhatsApp Validation] DataTable.Rows.Count = {0}, dt.GetHashCode() = {1}",
                    dt.Rows.Count, dt.GetHashCode()));

            // Read mappings from the GridView
            var mappings = ReadMappings();
            var phoneMapping = mappings.FirstOrDefault(m => m.IsPhone);

            if (phoneMapping == null)
            {
                ShowError("Please select a phone number column in the mapping table.");
                return;
            }

            // Run validation (READ-ONLY — never modifies the DataTable)
            var validation = Service.ValidateData(dt, phoneMapping);

            // Diagnostic: confirm DataTable was not mutated
            System.Diagnostics.Debug.WriteLine(
                string.Format("[WhatsApp Validation] Input rows: {0}, Output rows: {1}, Mutated: {2}",
                    validation.InputDataTableRows, validation.OutputDataTableRows,
                    validation.InputDataTableRows != validation.OutputDataTableRows ? "YES — BUG!" : "NO"));

            System.Diagnostics.Debug.WriteLine(
                string.Format("[WhatsApp Validation] Unique valid phones: {0}, Invalid: {1}, Duplicates: {2}, TotalWarningsGenerated: {3}",
                    validation.ValidRows, validation.InvalidRows, validation.DuplicateRows, validation.TotalWarningsGenerated));

            // Re-read DataTable from Session AFTER validation to confirm no mutation
            var dtAfterValidate = Session[SESSION_DATA] as DataTable;
            if (dtAfterValidate != null)
            {
                System.Diagnostics.Debug.WriteLine(
                    string.Format("[WhatsApp Validation] DataTable.Rows.Count AFTER validation = {0} (hash={1})",
                        dtAfterValidate.Rows.Count, dtAfterValidate.GetHashCode()));
            }

            lblTotalCount.Text = "Total rows: " + validation.TotalRows;
            lblValidCount.Text = "Valid rows: " + validation.ValidRows;
            lblInvalidCount.Text = validation.InvalidRows > 0
                ? "Invalid rows: " + validation.InvalidRows
                : "";
            lblDuplicateCount.Text = validation.DuplicateRows > 0
                ? "Duplicates: " + validation.DuplicateRows
                : "";

            // Show total warnings generated (before truncation) for diagnostic
            if (validation.TotalWarningsGenerated > validation.Warnings.Count)
            {
                lblWarningsInfo.Text = string.Format("({0} warnings shown of {1} total)",
                    validation.Warnings.Count, validation.TotalWarningsGenerated);
                lblWarningsInfo.Visible = true;
            }
            else
            {
                lblWarningsInfo.Visible = false;
            }

            // Show parse diagnostics in validation summary for debugging
            var parseDiag = Session[SESSION_PARSE_DIAG] as ParseDiagnostics;
            if (parseDiag != null)
            {
                lblParseInfo.Text = string.Format(
                    "Parse — Worksheet rows: {0}–{1}, Data rows: {2}, Empty skipped: {3}, DataTable rows: {4}, hash: {5}",
                    parseDiag.WorksheetStartRow, parseDiag.WorksheetEndRow,
                    parseDiag.ExpectedDataRows, parseDiag.EmptyRowsSkipped, parseDiag.DataTableRows,
                    dt.GetHashCode());
                lblParseInfo.Visible = true;
            }

            rptWarnings.DataSource = validation.Warnings.Select(w => new { Text = w });
            rptWarnings.DataBind();

            pnlValidation.Visible = true;

            if (validation.IsValid || validation.ValidRows > 0)
            {
                btnSend.Visible = true;

                // Store config in session for Step 4
                var config = new CampaignConfig
                {
                    CampaignName = txtCampaignName.Text.Trim(),
                    TemplateName = txtTemplateName.Text.Trim(),
                    Language = ddlLanguage.SelectedValue,
                    ColumnMappings = mappings
                };
                Session[SESSION_CONFIG] = config;

                if (!validation.IsValid)
                    ShowError("Some rows are invalid. Only valid rows will be included in the campaign.");

                // ============================================================
                //  PHASE 2 — BUILD DISPATCH PLAN (single source of truth)
                // ============================================================
                BuildAndShowDispatchPlan(dt, config, mappings);
            }
            else
            {
                btnSend.Visible = false;
                ShowError("No valid rows found. Please check your data and column mapping.");
            }
        }

        /// <summary>
        /// Phase 2: builds the DispatchPlan from the uploaded data + content config,
        /// then displays the preview grid. The plan — not the UI — is what the send
        /// engine executes.
        /// </summary>
        private void BuildAndShowDispatchPlan(DataTable dt, CampaignConfig config, List<ColumnMapping> mappings)
        {
            try
            {
                var content = Session[SESSION_CONTENT] as ContentConfig ?? new ContentConfig();
                content.Mode = GetSelectedContentMode();

                // Sync mode-specific selections made after upload
                if (content.Mode == ContentMode.IndividualFile)
                    content.MappingColumn = ddlIndividualMappingColumn.SelectedValue;
                if (content.Mode == ContentMode.WorkbookSheet)
                    content.MappingColumn = ddlWorkbookMappingColumn.SelectedValue;
                if (content.Mode == ContentMode.ExcelColumnUrl)
                    content.UrlColumn = ddlUrlColumn.SelectedValue;

                // Cross-checks: a file-based mode needs its content uploaded first
                if (content.Mode == ContentMode.CommonForAll && string.IsNullOrEmpty(content.TempFilePath))
                {
                    lblDispatchSummary.Text = "Common attachment mode selected but no file has been uploaded yet.";
                    pnlDispatchPlan.Visible = true;
                    gvDispatchPlan.DataSource = null;
                    gvDispatchPlan.DataBind();
                    btnSendDispatch.Visible = false;
                    return;
                }
                if (content.Mode == ContentMode.IndividualFile && (content.IndividualFileNames == null || content.IndividualFileNames.Count == 0))
                {
                    lblDispatchSummary.Text = "Individual file mode selected but no files have been uploaded yet.";
                    pnlDispatchPlan.Visible = true;
                    gvDispatchPlan.DataSource = null;
                    gvDispatchPlan.DataBind();
                    btnSendDispatch.Visible = false;
                    return;
                }
                if (content.Mode == ContentMode.WorkbookSheet && string.IsNullOrEmpty(content.WorkbookTempPath))
                {
                    lblDispatchSummary.Text = "Workbook mode selected but no workbook has been uploaded yet.";
                    pnlDispatchPlan.Visible = true;
                    gvDispatchPlan.DataSource = null;
                    gvDispatchPlan.DataBind();
                    btnSendDispatch.Visible = false;
                    return;
                }

                var phoneMapping = mappings.FirstOrDefault(m => m.IsPhone);
                var headerFormat = GetSelectedHeaderFormat();

                var plan = Service.BuildDispatchPlan(dt, config, content, phoneMapping, headerFormat);
                Session[SESSION_DISPATCH_PLAN] = plan;

                lblDispatchSummary.Text = plan.ValidationSummary;
                gvDispatchPlan.DataSource = plan.Items.Take(100).Select(i => new
                {
                    i.RowNumber,
                    i.PhoneNumber,
                    i.MappingKey,
                    i.MediaFileName,
                    i.Status,
                    i.ErrorMessage
                }).ToList();
                gvDispatchPlan.DataBind();

                // Phase 1 bulk send stays visible only for None mode;
                // content modes use the dispatch send button
                bool hasReady = plan.ReadyCount > 0;
                btnSendDispatch.Visible = hasReady && content.Mode != ContentMode.None;
                if (content.Mode == ContentMode.None)
                    btnSend.Visible = true;
                else
                    btnSend.Visible = false;   // per-recipient flow replaces bulk CSV send

                pnlDispatchPlan.Visible = true;
            }
            catch (Exception ex)
            {
                lblDispatchSummary.Text = "Dispatch plan error: " + HttpUtility.HtmlEncode(ex.Message);
                pnlDispatchPlan.Visible = true;
                btnSendDispatch.Visible = false;
            }
        }

        // ============================================================
        //  PHASE 2 — SEND DISPATCH (per-recipient: upload → metaMediaId → send)
        // ============================================================
        protected void btnSendDispatch_Click(object sender, EventArgs e)
        {
            ClearAlerts();

            var plan = Session[SESSION_DISPATCH_PLAN] as DispatchPlan;
            if (plan == null)
            {
                ShowError("No dispatch plan found. Please validate again.");
                return;
            }

            var apiConfig = LoadApiConfig();
            if (string.IsNullOrEmpty(apiConfig.ApiKey))
            {
                ShowError("CEOITBOX API Key is not configured. Check Web.config CEOITBOX.ApiKey.");
                return;
            }

            var readyItems = plan.Items.Where(i => i.Status == DispatchStatus.Ready).ToList();
            if (readyItems.Count == 0)
            {
                ShowError("No recipients are Ready to send.");
                return;
            }

            try
            {
                pnlDispatchResults.Visible = true;
                divDispatchOk.Visible = false;
                divDispatchErr.Visible = false;

                var results = Service.ExecuteDispatchPlan(apiConfig, plan);

                // Bind per-recipient results
                gvDispatchResults.DataSource = plan.Items
                    .Where(i => i.Status == DispatchStatus.Accepted || i.Status == DispatchStatus.Failed)
                    .Select(i => new
                    {
                        i.PhoneNumber,
                        i.Status,
                        i.CeoitboxSystemId,
                        i.WhatsAppMessageId,
                        i.ErrorMessage
                    }).ToList();
                gvDispatchResults.DataBind();

                int accepted = plan.Items.Count(i => i.Status == DispatchStatus.Accepted);
                int failed = plan.Items.Count(i => i.Status == DispatchStatus.Failed);

                if (failed == 0)
                {
                    divDispatchOk.Visible = true;
                    lblDispatchOk.Text = string.Format(" {0} of {1} messages accepted by CEOITBOX.", accepted, readyItems.Count);
                }
                else
                {
                    divDispatchErr.Visible = true;
                    lblDispatchErr.Text = string.Format(" {0} accepted, {1} failed of {2} attempted.", accepted, failed, readyItems.Count);
                }

                // Persist the updated plan (statuses now reflect send outcome)
                Session[SESSION_DISPATCH_PLAN] = plan;
            }
            catch (Exception ex)
            {
                pnlDispatchResults.Visible = true;
                divDispatchErr.Visible = true;
                lblDispatchErr.Text = " Dispatch error: " + HttpUtility.HtmlEncode(ex.Message);
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

            // Diagnostic: confirm row count before send
            System.Diagnostics.Debug.WriteLine(
                string.Format("[WhatsApp Send] DataTable rows from Session: {0}", dt.Rows.Count));

            // Load API config from Web.config (credentials not in UI)
            var apiConfig = LoadApiConfig();
            if (string.IsNullOrEmpty(apiConfig.ApiKey)) { ShowError("CEOITBOX API Key is not configured. Check Web.config CEOITBOX.ApiKey."); return; }

            // Validate required campaign fields
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
                int filteredRowCount;
                byte[] csvBytes = Service.ConvertToCsvBytes(filteredDt, config, out filteredRowCount);

                // Diagnostic: confirm CSV row count matches filtered DataTable
                System.Diagnostics.Debug.WriteLine(
                    string.Format("[WhatsApp Send] Filtered DataTable rows: {0}, CSV data rows: {1}", filteredDt.Rows.Count, filteredRowCount));

                // Build and store the CampaignData JSON for display
                string campaignJson = Service.BuildCampaignJson(config);
                litCampaignJson.Text = System.Net.WebUtility.HtmlEncode(campaignJson);

                // Send to CEOITBOX
                var result = Service.SendBulkCampaign(apiConfig, config, csvBytes);

                pnlResults.Visible = true;

                if (result.Success)
                {
                    pnlResultOk.Visible = true;
                    pnlResultErr.Visible = false;
                    lblResultOk.Text = string.Format(" {0} recipients submitted to CEOITBOX (HTTP {1}).",
                        filteredDt.Rows.Count, result.StatusCode);
                }
                else
                {
                    pnlResultOk.Visible = false;
                    pnlResultErr.Visible = true;
                    lblResultErr.Text = string.Format(" {0}", result.ErrorMessage);
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
            lblUploadOk.Text = message;
            lblUploadOk.Visible = true;
            lblUploadErr.Visible = false;
        }

        private void ShowError(string message)
        {
            lblUploadErr.Text = message;
            lblUploadErr.Visible = true;
            lblUploadOk.Visible = false;
        }

        private void ClearAlerts()
        {
            lblUploadOk.Visible = false;
            lblUploadErr.Visible = false;
        }
    }
}
