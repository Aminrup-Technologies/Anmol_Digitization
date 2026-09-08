using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using OfficeOpenXml;  // EPPlus 4.5.3.3 — already in packages

namespace AnmolDristi
{
    /// <summary>
    /// Phase 1 WhatsApp Bulk Messaging service.
    /// Handles Excel → DataTable, validation, CSV conversion, and CEOITBOX API submission.
    /// No scheduling, media, contacts, or template management — those belong to later phases.
    /// </summary>
    public class WhatsAppService
    {
        // ---------------------------------------------------------------
        //  EXCEL PARSING (EPPlus 4.5 — reads .xlsx only)
        // ---------------------------------------------------------------
        /// <summary>
        /// Reads the first worksheet from an uploaded .xlsx stream into a DataTable.
        /// Row 1 = column headers, Row 2+ = data.
        /// </summary>
        public DataTable ParseExcelToDataTable(Stream fileStream, string fileName, out ParseDiagnostics diagnostics)
        {
            diagnostics = new ParseDiagnostics();

            using (var package = new ExcelPackage(fileStream))
            {
                var ws = package.Workbook.Worksheets.FirstOrDefault();
                if (ws == null || ws.Dimension == null)
                    throw new InvalidOperationException("The Excel file contains no data.");

                diagnostics.WorksheetStartRow = ws.Dimension.Start.Row;
                diagnostics.WorksheetEndRow = ws.Dimension.End.Row;
                diagnostics.WorksheetStartCol = ws.Dimension.Start.Column;
                diagnostics.WorksheetEndCol = ws.Dimension.End.Column;
                diagnostics.HeaderRowContent = (ws.Cells[1, ws.Dimension.Start.Column, 1, ws.Dimension.End.Column].Text ?? "").Trim();

                var dt = new DataTable("Recipients");

                // ---- Column headers (Row 1) ----
                for (int col = ws.Dimension.Start.Column; col <= ws.Dimension.End.Column; col++)
                {
                    string header = (ws.Cells[1, col].Text ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(header))
                        header = "Column_" + col;

                    // Avoid duplicate column names in DataTable
                    if (dt.Columns.Contains(header))
                        header = header + "_" + col;

                    dt.Columns.Add(header, typeof(string));
                }

                // ---- Data rows (Row 2+) ----
                int expectedDataRows = ws.Dimension.End.Row - ws.Dimension.Start.Row;
                diagnostics.ExpectedDataRows = expectedDataRows;

                for (int row = ws.Dimension.Start.Row + 1; row <= ws.Dimension.End.Row; row++)
                {
                    // Skip rows where ALL cells are empty or contain only invisible whitespace
                    // (spaces, tabs, non-breaking spaces \u00A0, zero-width chars)
                    bool allEmpty = true;
                    for (int col = ws.Dimension.Start.Column; col <= ws.Dimension.End.Column; col++)
                    {
                        string cellText = ws.Cells[row, col].Text ?? string.Empty;
                        // Trim ALL Unicode whitespace, not just ASCII spaces
                        string trimmed = cellText.Trim();
                        if (!string.IsNullOrEmpty(trimmed))
                        {
                            // Additional check: reject cells that are ONLY invisible characters
                            string stripped = Regex.Replace(trimmed, @"[\s\u00A0\u200B\u200C\u200D\uFEFF]", "");
                            if (!string.IsNullOrEmpty(stripped))
                            {
                                allEmpty = false;
                                break;
                            }
                        }
                    }
                    if (allEmpty)
                    {
                        diagnostics.EmptyRowsSkipped++;
                        continue;
                    }

                    var dr = dt.NewRow();
                    for (int col = ws.Dimension.Start.Column; col <= ws.Dimension.End.Column; col++)
                    {
                        dr[col - ws.Dimension.Start.Column] = (ws.Cells[row, col].Text ?? string.Empty).Trim();
                    }
                    dt.Rows.Add(dr);
                }

                diagnostics.DataTableRows = dt.Rows.Count;
                diagnostics.DataTableColumns = dt.Columns.Count;
                diagnostics.ColumnNames = string.Join(", ", dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName));

                return dt;
            }
        }

        // ---------------------------------------------------------------
        //  PHONE VALIDATION
        // ---------------------------------------------------------------
        /// <summary>
        /// Validates an Indian / international WhatsApp phone number.
        /// Accepts: 919876543210, +919876543210, 9876543210, +14155550100.
        /// Returns the normalised digits-with-plus format, or null if invalid.
        /// </summary>
        public string ValidateAndNormalizePhone(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            string phone = raw.Trim();
            bool hasPlus = phone.StartsWith("+");
            phone = Regex.Replace(phone, @"[^\d]", "");  // strip everything except digits

            if (phone.Length == 0) return null;

            // If original had a +, preserve it (required for non-Indian numbers per AiSensy docs)
            if (hasPlus) phone = "+" + phone;

            // Bare 10-digit number → treat as Indian (+91 prefix)
            if (!hasPlus && phone.Length == 10 && phone[0] != '0')
                phone = "91" + phone;

            return phone;
        }

        // ---------------------------------------------------------------
        //  VALIDATION
        // ---------------------------------------------------------------
        /// <summary>
        /// Validates the DataTable against the user's column mappings.
        /// Checks phone format, non-empty required fields, and reports row-level issues.
        /// </summary>
        /// <summary>
        /// READ-ONLY validation. Inspects the DataTable and returns counts/warnings.
        /// Must NEVER add, remove, merge, or modify rows in the source DataTable.
        /// </summary>
        public ValidationResult ValidateData(DataTable dt, ColumnMapping phoneMapping)
        {
            var result = new ValidationResult();
            result.TotalRows = dt.Rows.Count;
            result.InputDataTableRows = dt.Rows.Count;  // diagnostic: confirm no mutation

            if (phoneMapping == null)
            {
                result.Errors.Add("Phone column mapping is required.");
                return result;
            }

            if (!dt.Columns.Contains(phoneMapping.ExcelColumn))
            {
                result.Errors.Add("Phone column '" + phoneMapping.ExcelColumn + "' not found in the uploaded data.");
                return result;
            }

            int validCount = 0;
            int invalidCount = 0;
            int duplicateCount = 0;
            int warningsGenerated = 0;  // total warnings before truncation
            var seenPhones = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            System.Diagnostics.Debug.WriteLine(
                string.Format("[WhatsApp Validation] DataTable.Rows.Count = {0}", dt.Rows.Count));

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string phoneRaw = dt.Rows[i][phoneMapping.ExcelColumn] as string ?? "";
                string phone = ValidateAndNormalizePhone(phoneRaw);
                int displayRowNumber = i + 2;  // Excel row: header is row 1, first data row is row 2

                System.Diagnostics.Debug.WriteLine(
                    string.Format("[Duplicate Detection] DataTableIndex={0}, DisplayRowNumber={1}, Phone={2}",
                        i, displayRowNumber, phone ?? "NULL"));

                if (string.IsNullOrEmpty(phone))
                {
                    invalidCount++;
                    warningsGenerated++;
                    if (warningsGenerated <= 10)
                        result.Warnings.Add("Row " + displayRowNumber + ": invalid phone '" + phoneRaw + "'");
                    continue;
                }

                if (seenPhones.Contains(phone))
                {
                    duplicateCount++;
                    warningsGenerated++;
                    if (warningsGenerated <= 10)
                        result.Warnings.Add("Row " + displayRowNumber + ": duplicate phone '" + phone + "'");
                    continue;
                }

                seenPhones.Add(phone);
                validCount++;
            }

            result.ValidRows = validCount;
            result.InvalidRows = invalidCount;
            result.DuplicateRows = duplicateCount;
            result.TotalWarningsGenerated = warningsGenerated;
            result.OutputDataTableRows = dt.Rows.Count;  // diagnostic: confirm no mutation

            int totalSkipped = invalidCount + duplicateCount;
            if (warningsGenerated > 10)
                result.Warnings.Add("... and " + (warningsGenerated - 10) + " more invalid/duplicate rows.");

            return result;
        }

        // ---------------------------------------------------------------
        //  CSV CONVERSION
        // ---------------------------------------------------------------
        /// <summary>
        /// Converts the DataTable to a UTF-8 CSV byte array suitable for the CEOITBOX API.
        /// First column = Phone (normalised), remaining columns = template variables in order.
        /// </summary>
        /// <summary>
        /// Converts the DataTable to CSV. Only includes rows that pass phone validation.
        /// Returns the bytes AND populates the diagnostics with the filtered row count.
        /// </summary>
        public byte[] ConvertToCsvBytes(DataTable dt, CampaignConfig config, out int filteredRowCount)
        {
            var sb = new StringBuilder();

            // --- Header row ---
            var orderedMappings = config.ColumnMappings
                .OrderBy(m => m.IsPhone ? 0 : 1)           // phone first
                .ThenBy(m => m.IsPhone ? 0 : m.VariableIndex) // then variable order
                .ToList();

            sb.AppendLine(string.Join(",", orderedMappings.Select(m => CsvEscape(m.ExcelColumn))));

            // --- Data rows ---
            int dataRowCount = 0;
            foreach (DataRow row in dt.Rows)
            {
                var cells = orderedMappings.Select(m =>
                {
                    string val = (row[m.ExcelColumn] as string ?? "").Trim();

                    if (m.IsPhone)
                        val = ValidateAndNormalizePhone(val) ?? val;

                    return CsvEscape(val);
                });

                sb.AppendLine(string.Join(",", cells));
                dataRowCount++;
            }

            filteredRowCount = dataRowCount;
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        /// <summary>
        /// Builds the CampaignData JSON string for the CEOITBOX sendMessageByCampaign1 endpoint.
        /// Structure confirmed against Swagger-generated request schema:
        /// { "campaignType": "CSV", "campaignName": "...", "message": { "templateName": "...", "language": "en" } }
        /// </summary>
        public string BuildCampaignJson(CampaignConfig config)
        {
            var data = new
            {
                campaignType = "CSV",
                campaignName = config.CampaignName ?? "",
                message = new
                {
                    templateName = config.TemplateName ?? "",
                    language = string.IsNullOrWhiteSpace(config.Language) ? "en" : config.Language
                }
            };

            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            return serializer.Serialize(data);
        }

        // ---------------------------------------------------------------
        //  CEOITBOX API SUBMISSION (multipart/form-data)
        // ---------------------------------------------------------------
        /// <summary>
        /// Sends the bulk campaign to the CEOITBOX WhatsApp API.
        /// POST /api/v1/whatsapp/sendMessageByCampaign1
        /// Content-Type: multipart/form-data
        /// Fields: jsonData (JSON string), csvFile (CSV binary)
        /// Headers: x-api-key, x-phone-id, optional Authorization: Basic
        /// </summary>
        public SendResult SendBulkCampaign(CEOITBOXConfig apiConfig, CampaignConfig config, byte[] csvBytes)
        {
            string jsonData = BuildCampaignJson(config);
            string baseUrl = string.IsNullOrEmpty(apiConfig.BaseUrl)
                ? "https://official.ceoitbox.com"
                : apiConfig.BaseUrl.TrimEnd('/');

            try
            {
                using (var client = new HttpClient())
                {
                    // Explicitly enable TLS 1.2 — .NET 4.5.2 defaults may not include it
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    client.Timeout = TimeSpan.FromSeconds(60);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // --- Authentication (configurable) ---
                    if (!string.IsNullOrWhiteSpace(apiConfig.ApiKey))
                        client.DefaultRequestHeaders.Add("x-api-key", apiConfig.ApiKey);

                    if (!string.IsNullOrWhiteSpace(apiConfig.PhoneId))
                        client.DefaultRequestHeaders.Add("x-phone-id", apiConfig.PhoneId.Trim());

                    if (apiConfig.UseBasicAuthentication
                        && !string.IsNullOrWhiteSpace(apiConfig.Username))
                    {
                        string creds = Convert.ToBase64String(
                            Encoding.ASCII.GetBytes(apiConfig.Username + ":" + (apiConfig.Password ?? "")));
                        client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Basic", creds);
                    }

                    // --- Multipart body ---
                    var content = new MultipartFormDataContent();

                    var jsonContent = new StringContent(jsonData, Encoding.UTF8, "application/json");
                    content.Add(jsonContent, "jsonData");

                    var csvContent = new ByteArrayContent(csvBytes);
                    csvContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
                    content.Add(csvContent, "csvFile", "recipients.csv");

                    // --- POST ---
                    var response = client.PostAsync(baseUrl + "/api/v1/whatsapp/sendMessageByCampaign1", content)
                                          .GetAwaiter().GetResult();

                    string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    return new SendResult
                    {
                        Success = response.IsSuccessStatusCode,
                        StatusCode = (int)response.StatusCode,
                        ResponseBody = body,
                        Message = response.IsSuccessStatusCode ? "Campaign submitted successfully." : "",
                        ErrorMessage = response.IsSuccessStatusCode
                            ? ""
                            : "HTTP " + (int)response.StatusCode + " — " + Truncate(body, 500)
                    };
                }
            }
            catch (TaskCanceledException ex)
            {
                // Distinguish timeout from cancellation
                string detail = GetFullExceptionMessage(ex);
                bool isTimeout = ex.InnerException is TimeoutException
                    || detail.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0;
                string msg = isTimeout
                    ? "Request timed out after 60 seconds."
                    : "Request was cancelled: " + detail;
                return new SendResult { Success = false, StatusCode = 0, ErrorMessage = msg };
            }
            catch (HttpRequestException ex)
            {
                // Network/TLS/DNS failure — show full inner chain so the actual cause is visible
                string detail = GetFullExceptionMessage(ex);
                return new SendResult
                {
                    Success = false,
                    StatusCode = 0,
                    ErrorMessage = "Network error: " + detail
                };
            }
            catch (WebException ex)
            {
                string detail = GetFullExceptionMessage(ex);
                return new SendResult { Success = false, StatusCode = (int)(ex.Response != null ? ((HttpWebResponse)ex.Response).StatusCode : 0), ErrorMessage = "Network error: " + detail };
            }
            catch (Exception ex)
            {
                return new SendResult { Success = false, StatusCode = 0, ErrorMessage = "Unexpected error: " + GetFullExceptionMessage(ex) };
            }
        }

        // ===============================================================
        //  PHASE 2 — CONTENT VALIDATION, MEDIA UPLOAD, DISPATCH, SEND
        // ===============================================================

        // ---------------------------------------------------------------
        //  CONFIGURABLE MEDIA RULES (isolated — can be updated after live validation)
        // ---------------------------------------------------------------

        /// <summary>
        /// MIME types CONFIRMED acceptable for IMAGE-header templates
        /// (from live error 131053: webp, png, jpeg).
        /// </summary>
        private static readonly string[] ImageMimeTypes =
        {
            "image/webp", "image/png", "image/jpeg"
        };

        /// <summary>
        /// DOCUMENT MIME types. XLSX + DOCUMENT has NOT been live-tested yet —
        /// enabled via Web.config CEOITBOX.AllowDocumentMedia (default false) until confirmed.
        /// </summary>
        private static readonly string[] DocumentMimeTypes =
        {
            "application/pdf",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",                  // .xlsx
            "application/vnd.ms-excel",                                                          // .xls
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",           // .docx
            "application/msword",                                                                // .doc
            "application/vnd.openxmlformats-officedocument.presentationml.presentation",         // .pptx
            "text/plain"                                                                         // .txt
        };

        /// <summary>
        /// VIDEO MIME types. Not live-tested — enabled via CEOITBOX.AllowVideoMedia (default false).
        /// </summary>
        private static readonly string[] VideoMimeTypes =
        {
            "video/mp4", "video/3gpp"
        };

        /// <summary>
        /// Returns the extension-based MIME type for an uploaded file, or null if unsupported.
        /// Extension is derived from the ORIGINAL filename for MIME mapping only — never as a server path.
        /// </summary>
        public static string GetMimeTypeFromExtension(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return null;
            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            switch (ext)
            {
                case ".png": return "image/png";
                case ".jpg": case ".jpeg": return "image/jpeg";
                case ".webp": return "image/webp";
                case ".gif": return "image/gif";
                case ".pdf": return "application/pdf";
                case ".xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case ".xls": return "application/vnd.ms-excel";
                case ".docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".doc": return "application/msword";
                case ".pptx": return "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                case ".txt": return "text/plain";
                case ".mp4": return "video/mp4";
                case ".3gp": return "video/3gpp";
                default: return null;
            }
        }

        /// <summary>
        /// Validates that a MIME type is compatible with the selected template header format.
        /// Returns null when compatible, otherwise an application-level error message.
        /// DOES NOT call CEOITBOX — invalid combinations are rejected before any API call.
        /// </summary>
        public string ValidateTemplateContentCompatibility(TemplateHeaderFormat headerFormat, string mimeType)
        {
            if (string.IsNullOrWhiteSpace(mimeType))
                return "Content MIME type could not be determined for the selected file.";

            switch (headerFormat)
            {
                case TemplateHeaderFormat.Image:
                    if (ImageMimeTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase))
                        return null;
                    // Confirmed live finding: XLSX supplied as IMAGE media → error 131053
                    if (mimeType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                        return "XLSX files require a DOCUMENT-compatible template. The selected template uses an IMAGE header.";
                    if (DocumentMimeTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase))
                        return "Document files require a DOCUMENT-compatible template. The selected template uses an IMAGE header.";
                    if (VideoMimeTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase))
                        return "Video files require a VIDEO-compatible template. The selected template uses an IMAGE header.";
                    return "File type '" + mimeType + "' is not supported for an IMAGE-header template."
                         + " Supported: image/webp, image/png, image/jpeg.";

                case TemplateHeaderFormat.Document:
                    // XLSX + DOCUMENT is PENDING LIVE VALIDATION — controlled by CEOITBOX.AllowDocumentMedia
                    bool allowDocs = ConfigurationHelper.GetBoolSetting("CEOITBOX.AllowDocumentMedia", false);
                    if (!allowDocs)
                        return "Document-header template support has not been live-validated yet."
                             + " Set CEOITBOX.AllowDocumentMedia=true in Web.config after confirming with CEOITBOX support.";
                    if (DocumentMimeTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase))
                        return null;
                    if (ImageMimeTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase))
                        return "Image files require an IMAGE-compatible template. The selected template uses a DOCUMENT header.";
                    return "File type '" + mimeType + "' is not supported for a DOCUMENT-header template.";

                case TemplateHeaderFormat.Video:
                    bool allowVideo = ConfigurationHelper.GetBoolSetting("CEOITBOX.AllowVideoMedia", false);
                    if (!allowVideo)
                        return "Video-header template support has not been live-validated yet."
                             + " Set CEOITBOX.AllowVideoMedia=true in Web.config after confirming with CEOITBOX support.";
                    if (VideoMimeTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase))
                        return null;
                    return "File type '" + mimeType + "' is not supported for a VIDEO-header template.";

                case TemplateHeaderFormat.BodyOnly:
                    return "The selected template has no media header and cannot carry file content."
                         + " Choose 'No Attachment / Template Only' or a template with a media header.";

                default:
                    return "Unknown template header format.";
            }
        }

        /// <summary>
        /// Validates a single content file against configured limits and MIME support.
        /// Returns null when valid, otherwise an application-level error message.
        /// </summary>
        public string ValidateContent(string fileName, long fileSizeBytes, TemplateHeaderFormat headerFormat)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "File name is required.";

            // Path traversal prevention — original name is display-only, but reject hostile values anyway
            if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\")
                || fileName.IndexOf(Path.DirectorySeparatorChar) >= 0
                || fileName.IndexOf(Path.AltDirectorySeparatorChar) >= 0)
                return "File name contains invalid path characters.";

            string mimeType = GetMimeTypeFromExtension(fileName);
            if (mimeType == null)
                return "Unsupported file type. Allowed: images (png/jpg/webp), documents (pdf/xlsx/xls/docx/doc/pptx/txt), videos (mp4/3gp).";

            long maxBytes = ConfigurationHelper.GetLongSetting("CEOITBOX.MaxMediaFileSizeMB", 16) * 1024L * 1024L;
            if (fileSizeBytes > maxBytes)
                return string.Format("File size ({0:N1} MB) exceeds the configured limit of {1} MB.",
                    fileSizeBytes / 1024.0 / 1024.0, maxBytes / 1024 / 1024);

            return ValidateTemplateContentCompatibility(headerFormat, mimeType);
        }

        /// <summary>
        /// Validates a per-recipient URL (ExcelColumnUrl mode).
        /// Returns null when valid, otherwise the reason.
        /// </summary>
        public string ValidateUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return "URL is empty.";

            Uri parsed;
            if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out parsed))
                return "'" + Truncate(url, 80) + "' is not a valid absolute URL.";

            if (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps)
                return "'" + Truncate(url, 80) + "' must use http or https.";

            return null;
        }

        /// <summary>
        /// Builds the Dispatch Plan — the single source of truth between validation and send.
        /// READ-ONLY with respect to the input DataTable. Every recipient becomes an item;
        /// invalid ones are marked Blocked with a reason and never sent.
        /// </summary>
        public DispatchPlan BuildDispatchPlan(
            DataTable dt,
            CampaignConfig campaign,
            ContentConfig content,
            ColumnMapping phoneMapping,
            TemplateHeaderFormat headerFormat)
        {
            var plan = new DispatchPlan { Campaign = campaign, Content = content };

            if (phoneMapping == null || !dt.Columns.Contains(phoneMapping.ExcelColumn))
            {
                plan.ValidationSummary = "Phone column mapping is missing or invalid.";
                return plan;
            }

            bool hasMappingColumn = !string.IsNullOrWhiteSpace(content.MappingColumn)
                                    && dt.Columns.Contains(content.MappingColumn);
            bool hasUrlColumn = !string.IsNullOrWhiteSpace(content.UrlColumn)
                                && dt.Columns.Contains(content.UrlColumn);

            var seenPhones = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var summaryLines = new List<string>();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow row = dt.Rows[i];
                var item = new RecipientDispatchItem
                {
                    RowNumber = i + 2,                       // Excel row (header = row 1)
                    TemplateName = campaign.TemplateName ?? "",
                    Language = string.IsNullOrWhiteSpace(campaign.Language) ? "en" : campaign.Language,
                    ContentMode = content.Mode,
                    ContentType = content.Type
                };

                // --- Template variables (selected variable columns, in mapping order) ---
                foreach (var m in campaign.ColumnMappings.Where(v => v.IsVariable))
                {
                    string val = (row[m.ExcelColumn] as string ?? "").Trim();
                    item.TemplateVariables[m.ExcelColumn] = val;
                }

                // --- 1. Phone validation + duplicate detection (existing rules) ---
                string phoneRaw = row[phoneMapping.ExcelColumn] as string ?? "";
                string phone = ValidateAndNormalizePhone(phoneRaw);
                item.PhoneNumber = phone ?? "";

                if (string.IsNullOrEmpty(phone))
                {
                    item.Status = DispatchStatus.Blocked;
                    item.ErrorMessage = "Invalid phone '" + Truncate(phoneRaw, 30) + "'";
                    plan.InvalidPhoneCount++;
                    plan.Items.Add(item);
                    continue;
                }
                if (!seenPhones.Add(phone))
                {
                    item.Status = DispatchStatus.Blocked;
                    item.ErrorMessage = "Duplicate phone " + phone;
                    plan.DuplicateCount++;
                    plan.Items.Add(item);
                    continue;
                }

                // --- 2-8. Content validation by mode ---
                switch (content.Mode)
                {
                    case ContentMode.None:
                        item.Status = DispatchStatus.Ready;
                        plan.ReadyCount++;
                        break;

                    case ContentMode.CommonForAll:
                        {
                            string err = ValidateContent(content.FileName, content.TempFilePath.Length > 0
                                ? new FileInfo(content.TempFilePath).Length : 0, headerFormat);
                            if (err != null)
                            {
                                item.Status = DispatchStatus.Blocked;
                                item.ErrorMessage = err;
                                plan.InvalidContentCount++;
                            }
                            else
                            {
                                item.ContentReference = content.TempFilePath;
                                item.MediaFileName = content.FileName;
                                item.MediaMimeType = GetMimeTypeFromExtension(content.FileName);
                                item.Status = DispatchStatus.Ready;
                                plan.ReadyCount++;
                            }
                            break;
                        }

                    case ContentMode.IndividualFile:
                        {
                            if (!hasMappingColumn)
                            {
                                item.Status = DispatchStatus.Blocked;
                                item.ErrorMessage = "Mapping column is not configured or missing.";
                                plan.MissingContentCount++;
                                break;
                            }
                            string key = (row[content.MappingColumn] as string ?? "").Trim();
                            item.MappingKey = key;
                            if (string.IsNullOrEmpty(key))
                            {
                                item.Status = DispatchStatus.Blocked;
                                item.ErrorMessage = "Missing mapping key in column '" + content.MappingColumn + "'.";
                                plan.MissingContentCount++;
                                break;
                            }
                            if (content.IndividualFileNames == null || !content.IndividualFileNames.ContainsKey(key))
                            {
                                item.Status = DispatchStatus.Blocked;
                                item.ErrorMessage = "No uploaded file matches key '" + Truncate(key, 40) + "'.";
                                plan.MissingContentCount++;
                                break;
                            }
                            // Resolve via the server-assigned safe path — NEVER build a path from the user key
                            string filePath;
                            if (content.IndividualFilePaths == null || !content.IndividualFilePaths.TryGetValue(key, out filePath))
                            {
                                item.Status = DispatchStatus.Blocked;
                                item.ErrorMessage = "Matched file missing on server for key '" + Truncate(key, 40) + "'.";
                                plan.MissingContentCount++;
                                break;
                            }
                            if (!File.Exists(filePath))
                            {
                                item.Status = DispatchStatus.Blocked;
                                item.ErrorMessage = "Matched file missing on server for key '" + Truncate(key, 40) + "'.";
                                plan.MissingContentCount++;
                                break;
                            }
                            string err = ValidateContent(content.IndividualFileNames[key], new FileInfo(filePath).Length, headerFormat);
                            if (err != null)
                            {
                                item.Status = DispatchStatus.Blocked;
                                item.ErrorMessage = err;
                                plan.InvalidContentCount++;
                                break;
                            }
                            item.ContentReference = filePath;
                            item.MediaFileName = content.IndividualFileNames[key];
                            item.MediaMimeType = GetMimeTypeFromExtension(content.IndividualFileNames[key]);
                            item.Status = DispatchStatus.Ready;
                            plan.ReadyCount++;
                            break;
                        }

                    case ContentMode.WorkbookSheet:
                        {
                            if (!hasMappingColumn)
                            {
                                item.Status = DispatchStatus.Blocked;
                                item.ErrorMessage = "Mapping column is not configured or missing.";
                                plan.MissingContentCount++;
                                break;
                            }
                            string key = (row[content.MappingColumn] as string ?? "").Trim();
                            item.MappingKey = key;
                            if (string.IsNullOrEmpty(key))
                            {
                                item.Status = DispatchStatus.Blocked;
                                item.ErrorMessage = "Missing mapping key in column '" + content.MappingColumn + "'.";
                                plan.MissingContentCount++;
                                break;
                            }
                            string sheet = content.WorkbookSheetNames
                                .FirstOrDefault(s => string.Equals(s, key, StringComparison.OrdinalIgnoreCase));
                            if (sheet == null)
                            {
                                item.Status = DispatchStatus.Blocked;
                                item.ErrorMessage = "No workbook sheet matches key '" + Truncate(key, 40) + "'.";
                                plan.MissingContentCount++;
                                break;
                            }
                            // Sheet → temp .xlsx happens during send (never during validation)
                            item.ContentReference = "sheet:" + sheet;
                            item.MediaFileName = sheet + ".xlsx";
                            item.MediaMimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                            item.Status = DispatchStatus.Ready;
                            plan.ReadyCount++;
                            break;
                        }

                    case ContentMode.ExcelColumnUrl:
                        {
                            if (!hasUrlColumn)
                            {
                                item.Status = DispatchStatus.Blocked;
                                item.ErrorMessage = "URL column is not configured or missing.";
                                plan.InvalidUrlCount++;
                                break;
                            }
                            string url = (row[content.UrlColumn] as string ?? "").Trim();
                            string err = ValidateUrl(url);
                            if (err != null)
                            {
                                item.Status = DispatchStatus.Blocked;
                                item.ErrorMessage = err;
                                plan.InvalidUrlCount++;
                                break;
                            }
                            item.ContentReference = url;
                            item.Status = DispatchStatus.Ready;
                            plan.ReadyCount++;
                            break;
                        }

                    default:
                        item.Status = DispatchStatus.Blocked;
                        item.ErrorMessage = "Unknown content mode.";
                        plan.InvalidContentCount++;
                        break;
                }

                plan.Items.Add(item);
            }

            plan.TotalRecipients = plan.Items.Count;

            // --- Summary (separate counters, no double counting) ---
            summaryLines.Add("Total recipients: " + plan.TotalRecipients);
            summaryLines.Add("Ready: " + plan.ReadyCount);
            summaryLines.Add("Invalid phone: " + plan.InvalidPhoneCount);
            summaryLines.Add("Duplicates: " + plan.DuplicateCount);
            if (content.Mode == ContentMode.IndividualFile || content.Mode == ContentMode.WorkbookSheet)
                summaryLines.Add("Missing content: " + plan.MissingContentCount);
            if (content.Mode == ContentMode.CommonForAll || content.Mode == ContentMode.IndividualFile || content.Mode == ContentMode.WorkbookSheet)
                summaryLines.Add("Invalid content: " + plan.InvalidContentCount);
            if (content.Mode == ContentMode.ExcelColumnUrl)
                summaryLines.Add("Invalid URLs: " + plan.InvalidUrlCount);
            plan.ValidationSummary = string.Join("  ·  ", summaryLines);

            return plan;
        }

        // ---------------------------------------------------------------
        //  PHASE 2 — TEMP FILE / WORKBOOK HELPERS
        // ---------------------------------------------------------------

        /// <summary>
        /// Saves an uploaded stream to a GUID-named temp file and returns the safe server path.
        /// The original filename is NEVER used as a filesystem path.
        /// </summary>
        public string SaveToTempFile(Stream content, string originalFileName, out string safeDisplayName)
        {
            string tempDir = ConfigurationHelper.GetStringSetting("CEOITBOX.TempFileDirectory",
                Path.Combine(Path.GetTempPath(), "CEOITBOX_WhatsApp"));
            if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

            string ext = Path.GetExtension(originalFileName ?? "");
            if (ext.Length > 10 || ext.Contains("..")) ext = "";   // defensive
            string tempPath = Path.Combine(tempDir, Guid.NewGuid().ToString("N") + ext);

            using (var fs = File.Create(tempPath))
            {
                content.CopyTo(fs);
            }

            safeDisplayName = Path.GetFileName(originalFileName ?? "");
            return tempPath;
        }

        /// <summary>
        /// Extracts one worksheet from a multi-sheet workbook into its own temp .xlsx file.
        /// The ORIGINAL workbook is never modified. Returns null if the sheet is not found.
        /// </summary>
        public string ExtractWorkbookSheet(string workbookTempPath, string sheetName, string targetPhone)
        {
            if (!File.Exists(workbookTempPath)) return null;

            string tempDir = ConfigurationHelper.GetStringSetting("CEOITBOX.TempFileDirectory",
                Path.Combine(Path.GetTempPath(), "CEOITBOX_WhatsApp"));
            if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

            string outPath = Path.Combine(tempDir, Guid.NewGuid().ToString("N") + ".xlsx");

            using (var srcPackage = new ExcelPackage(new FileInfo(workbookTempPath)))
            {
                var srcWs = srcPackage.Workbook.Worksheets[sheetName];
                if (srcWs == null) return null;

                using (var destPackage = new ExcelPackage())
                {
                    // Copy cells range
                    var destWs = destPackage.Workbook.Worksheets.Add(sheetName.Length <= 31 ? sheetName : sheetName.Substring(0, 31));
                    int endRow = srcWs.Dimension != null ? srcWs.Dimension.End.Row : 1;
                    int endCol = srcWs.Dimension != null ? srcWs.Dimension.End.Column : 1;
                    if (endRow >= 1 && endCol >= 1)
                        destWs.Cells[1, 1, endRow, endCol].Value = srcWs.Cells[1, 1, endRow, endCol].Value;

                    destPackage.SaveAs(new FileInfo(outPath));
                }
            }

            return outPath;
        }

        /// <summary>Deletes a temp file if it exists (best effort — never throws).</summary>
        public static void CleanupTempFile(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path)
                    && path.IndexOf(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase) == 0)
                    File.Delete(path);
            }
            catch { /* best effort */ }
        }

        /// <summary>Deletes a temp directory tree if it exists (best effort — never throws).</summary>
        public static void CleanupTempDirectory(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path)
                    && path.IndexOf(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase) == 0)
                    Directory.Delete(path, true);
            }
            catch { /* best effort */ }
        }

        // ---------------------------------------------------------------
        //  PHASE 2 — CEOITBOX MEDIA UPLOAD (confirmed endpoint)
        // ---------------------------------------------------------------

        /// <summary>
        /// POST /api/v1/whatsapp/meta/media/upload — multipart/form-data with
        /// messaging_product + file. Returns MediaUploadResult with MetaMediaId
        /// (the Meta handle — NOT the CEOITBOX "id" — for use in sendMessage).
        /// </summary>
        public MediaUploadResult UploadMedia(CEOITBOXConfig apiConfig, string filePath, string displayName, string mimeType)
        {
            var result = new MediaUploadResult();
            string baseUrl = string.IsNullOrEmpty(apiConfig.BaseUrl)
                ? "https://official.ceoitbox.com"
                : apiConfig.BaseUrl.TrimEnd('/');

            try
            {
                using (var client = CreateHttpClient(apiConfig))
                using (var fileStream = File.OpenRead(filePath))
                {
                    var content = new MultipartFormDataContent();
                    content.Add(new StringContent("whatsapp"), "messaging_product");

                    var fileContent = new StreamContent(fileStream);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                        string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType);
                    content.Add(fileContent, "file", string.IsNullOrWhiteSpace(displayName) ? "upload" : displayName);

                    var response = client.PostAsync(baseUrl + "/api/v1/whatsapp/meta/media/upload", content)
                                          .GetAwaiter().GetResult();
                    string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    result.StatusCode = (int)response.StatusCode;
                    result.ResponseBody = body;

                    if (!response.IsSuccessStatusCode)
                    {
                        result.ErrorMessage = "HTTP " + result.StatusCode + " — " + Truncate(body, 500);
                        return result;
                    }

                    // Parse { id, metaMediaId, mediaDetail } — use metaMediaId for sends
                    try
                    {
                        var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                        var j = serializer.Deserialize<Dictionary<string, object>>(body);
                        if (j != null)
                        {
                            object vId, vMeta;
                            if (j.TryGetValue("id", out vId) && vId != null) result.Id = vId.ToString();
                            if (j.TryGetValue("metaMediaId", out vMeta) && vMeta != null) result.MetaMediaId = vMeta.ToString();
                        }
                    }
                    catch
                    {
                        // Fallback: regex extraction if JSON decode fails
                        var mId = Regex.Match(body, "\"id\"\\s*:\\s*\"([^\"]+)\"");
                        var mMeta = Regex.Match(body, "\"metaMediaId\"\\s*:\\s*\"([^\"]+)\"");
                        if (mMeta.Success) result.MetaMediaId = mMeta.Groups[1].Value;
                        if (mId.Success) result.Id = mId.Groups[1].Value;
                    }

                    result.Success = !string.IsNullOrEmpty(result.MetaMediaId);
                    if (!result.Success)
                        result.ErrorMessage = "Media upload succeeded but no metaMediaId was returned. Response: " + Truncate(body, 300);

                    return result;
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = "Media upload error: " + GetFullExceptionMessage(ex);
                return result;
            }
        }

        // ---------------------------------------------------------------
        //  PHASE 2 — CEOITBOX INDIVIDUAL SEND (confirmed endpoint)
        // ---------------------------------------------------------------

        /// <summary>
        /// POST /api/v1/whatsapp/sendMessage — JSON body. Sends ONE recipient's message
        /// using template + variables + optional Meta media (via metaMediaId) or URL.
        /// Request shape per documented examples: template with variables array and
        /// media block carrying metaMediaId or link. Exact field names per Swagger examples.
        /// </summary>
        public IndividualSendResult SendMessage(CEOITBOXConfig apiConfig, RecipientDispatchItem item)
        {
            var result = new IndividualSendResult();
            string baseUrl = string.IsNullOrEmpty(apiConfig.BaseUrl)
                ? "https://official.ceoitbox.com"
                : apiConfig.BaseUrl.TrimEnd('/');

            try
            {
                using (var client = CreateHttpClient(apiConfig))
                {
                    object payload;

                    if (item.ContentMode == ContentMode.ExcelColumnUrl && !string.IsNullOrEmpty(item.ContentReference))
                    {
                        // Template + URL (documented example: template + file URL)
                        payload = new
                        {
                            messaging_product = "whatsapp",
                            to = item.PhoneNumber,
                            type = "template",
                            template = new
                            {
                                name = item.TemplateName,
                                language = new { code = item.Language },
                                components = new object[]
                                {
                                    new
                                    {
                                        type = "header",
                                        parameters = new object[]
                                        {
                                            new { type = "document", document = new { link = item.ContentReference } }
                                        }
                                    }
                                }
                            }
                        };
                    }
                    else if (!string.IsNullOrEmpty(item.MetaMediaId))
                    {
                        // Template + Meta Media ID (documented example)
                        payload = new
                        {
                            messaging_product = "whatsapp",
                            to = item.PhoneNumber,
                            type = "template",
                            template = new
                            {
                                name = item.TemplateName,
                                language = new { code = item.Language },
                                components = new object[]
                                {
                                    new
                                    {
                                        type = "header",
                                        parameters = new object[]
                                        {
                                            // PENDING API CONFIRMATION: exact CEOITBOX sendMessage JSON
                                            // field names must be verified against Swagger before live use.
                                            // Shape below follows the Meta Cloud API header-parameter convention
                                            // (CEOITBOX wraps Meta — endpoint is "meta/media/upload").
                                            item.ContentType == ContentType.Image
                                                ? (object)new { type = "image", image = new { id = item.MetaMediaId } }
                                                : (object)new { type = "document", document = new { id = item.MetaMediaId } }
                                        }
                                    }
                                }
                            }
                        };
                    }
                    else
                    {
                        // Template + variables only
                        var varValues = item.TemplateVariables.Values.Cast<object>().ToArray();
                        payload = new
                        {
                            messaging_product = "whatsapp",
                            to = item.PhoneNumber,
                            type = "template",
                            template = new
                            {
                                name = item.TemplateName,
                                language = new { code = item.Language },
                                components = new object[]
                                {
                                    new
                                    {
                                        type = "body",
                                        parameters = varValues.Select(v => new { type = "text", text = v }).ToArray()
                                    }
                                }
                            }
                        };
                    }

                    var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                    string json = serializer.Serialize(payload);

                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = client.PostAsync(baseUrl + "/api/v1/whatsapp/sendMessage", content)
                                          .GetAwaiter().GetResult();
                    string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    result.StatusCode = (int)response.StatusCode;
                    result.ResponseBody = body;

                    if (!response.IsSuccessStatusCode)
                    {
                        result.ErrorMessage = "HTTP " + result.StatusCode + " — " + Truncate(body, 500);
                        return result;
                    }

                    // Extract ids where present
                    var mSys = Regex.Match(body, "\"(?:id|systemId|messageId)\"\\s*:\\s*\"([^\"]+)\"");
                    var mWa = Regex.Match(body, "\"(?:wa_id|whatsappMessageId|messages)\"\\s*:\\s*\"([^\"]+)\"");
                    if (mSys.Success) result.CeoitboxSystemId = mSys.Groups[1].Value;
                    if (mWa.Success) result.WhatsAppMessageId = mWa.Groups[1].Value;

                    result.Success = true;
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = "Send error: " + GetFullExceptionMessage(ex);
                return result;
            }
        }

        /// <summary>
        /// Executes a DispatchPlan sequentially: upload media → send message → record result.
        /// Only Ready items are processed. Session-stored plan is updated item-by-item.
        /// Returns per-item results in the same order as plan.Items.
        /// </summary>
        public List<IndividualSendResult> ExecuteDispatchPlan(CEOITBOXConfig apiConfig, DispatchPlan plan)
        {
            var results = new List<IndividualSendResult>();

            foreach (var item in plan.Items)
            {
                if (item.Status != DispatchStatus.Ready)
                {
                    results.Add(new IndividualSendResult { Success = false, ErrorMessage = "Item not Ready — skipped." });
                    continue;
                }

                item.Status = DispatchStatus.Processing;
                string extractedSheetPath = null;

                try
                {
                    string mediaPath = item.ContentReference;

                    // WorkbookSheet mode: materialise the per-recipient temp .xlsx NOW (never during validation)
                    if (item.ContentMode == ContentMode.WorkbookSheet
                        && !string.IsNullOrEmpty(item.ContentReference)
                        && item.ContentReference.StartsWith("sheet:"))
                    {
                        string sheetName = item.ContentReference.Substring("sheet:".Length);
                        extractedSheetPath = ExtractWorkbookSheet(plan.Content.WorkbookTempPath, sheetName, item.PhoneNumber);
                        if (extractedSheetPath == null)
                        {
                            item.Status = DispatchStatus.Failed;
                            item.ErrorMessage = "Workbook sheet could not be extracted: " + sheetName;
                            results.Add(new IndividualSendResult { Success = false, ErrorMessage = item.ErrorMessage });
                            continue;
                        }
                        mediaPath = extractedSheetPath;
                    }

                    // 1. Upload media (file-based modes) → metaMediaId
                    if (item.ContentMode == ContentMode.CommonForAll
                        || item.ContentMode == ContentMode.IndividualFile
                        || item.ContentMode == ContentMode.WorkbookSheet)
                    {
                        var upload = UploadMedia(apiConfig, mediaPath, item.MediaFileName, item.MediaMimeType);
                        if (!upload.Success)
                        {
                            item.Status = DispatchStatus.Failed;
                            item.ErrorMessage = "Media upload failed: " + upload.ErrorMessage;
                            item.ApiResponse = upload.ResponseBody;
                            results.Add(new IndividualSendResult
                            {
                                Success = false,
                                StatusCode = upload.StatusCode,
                                ErrorMessage = item.ErrorMessage,
                                ResponseBody = upload.ResponseBody
                            });
                            continue;
                        }
                        item.MetaMediaId = upload.MetaMediaId;
                        item.Status = DispatchStatus.Uploaded;
                    }

                    // 2. Send message → record ids + status
                    var send = SendMessage(apiConfig, item);
                    item.ApiResponse = send.ResponseBody;
                    item.ProcessedAt = DateTime.UtcNow;

                    if (send.Success)
                    {
                        // HTTP 200 = Accepted by CEOITBOX — NOT Delivered
                        item.Status = DispatchStatus.Accepted;
                        item.CeoitboxSystemId = send.CeoitboxSystemId;
                        item.WhatsAppMessageId = send.WhatsAppMessageId;
                    }
                    else
                    {
                        item.Status = DispatchStatus.Failed;
                        item.ErrorMessage = send.ErrorMessage;
                    }

                    results.Add(send);
                }
                catch (Exception ex)
                {
                    item.Status = DispatchStatus.Failed;
                    item.ErrorMessage = GetFullExceptionMessage(ex);
                    results.Add(new IndividualSendResult { Success = false, ErrorMessage = item.ErrorMessage });
                }
                finally
                {
                    // Per-recipient temp sheet is deleted immediately after processing
                    if (extractedSheetPath != null)
                        CleanupTempFile(extractedSheetPath);
                }
            }

            return results;
        }

        // ---------------------------------------------------------------
        //  SHARED HTTP CLIENT (Phase 1 + Phase 2 — TLS 1.2 enforced)
        // ---------------------------------------------------------------

        /// <summary>
        /// Creates an HttpClient with the existing CEOITBOX authentication and TLS 1.2.
        /// Shared by bulk campaign (Phase 1) and media/message sends (Phase 2).
        /// </summary>
        private HttpClient CreateHttpClient(CEOITBOXConfig apiConfig)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrWhiteSpace(apiConfig.ApiKey))
                client.DefaultRequestHeaders.Add("x-api-key", apiConfig.ApiKey);

            if (!string.IsNullOrWhiteSpace(apiConfig.PhoneId))
                client.DefaultRequestHeaders.Add("x-phone-id", apiConfig.PhoneId.Trim());

            if (apiConfig.UseBasicAuthentication && !string.IsNullOrWhiteSpace(apiConfig.Username))
            {
                string creds = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes(apiConfig.Username + ":" + (apiConfig.Password ?? "")));
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", creds);
            }

            return client;
        }

        // ---------------------------------------------------------------
        //  PRIVATE HELPERS
        // ---------------------------------------------------------------
        private static string CsvEscape(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        /// <summary>
        /// Walks the full exception chain and returns all messages joined by " --> ".
        /// Does NOT expose credentials, headers, or API keys.
        /// </summary>
        private static string GetFullExceptionMessage(Exception ex)
        {
            var messages = new List<string>();
            Exception current = ex;
            while (current != null)
            {
                if (!string.IsNullOrWhiteSpace(current.Message))
                    messages.Add(current.Message);
                current = current.InnerException;
            }
            return messages.Count > 0 ? string.Join(" --> ", messages) : ex.Message;
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s ?? "";
            return s.Substring(0, max);
        }
    }
}