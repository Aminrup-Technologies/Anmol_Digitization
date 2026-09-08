using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
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
        public DataTable ParseExcelToDataTable(Stream fileStream, string fileName)
        {
            // EPPlus 4.5 requires this for non-commercial use; set once per AppDomain.
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage(fileStream))
            {
                var ws = package.Workbook.Worksheets.FirstOrDefault();
                if (ws == null || ws.Dimension == null)
                    throw new InvalidOperationException("The Excel file contains no data.");

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
                for (int row = ws.Dimension.Start.Row + 1; row <= ws.Dimension.End.Row; row++)
                {
                    // Skip completely empty rows
                    bool allEmpty = true;
                    for (int col = ws.Dimension.Start.Column; col <= ws.Dimension.End.Column; col++)
                    {
                        if (!string.IsNullOrWhiteSpace(ws.Cells[row, col].Text))
                        {
                            allEmpty = false;
                            break;
                        }
                    }
                    if (allEmpty) continue;

                    var dr = dt.NewRow();
                    for (int col = ws.Dimension.Start.Column; col <= ws.Dimension.End.Column; col++)
                    {
                        dr[col - ws.Dimension.Start.Column] = (ws.Cells[row, col].Text ?? string.Empty).Trim();
                    }
                    dt.Rows.Add(dr);
                }

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
        public ValidationResult ValidateData(DataTable dt, ColumnMapping phoneMapping)
        {
            var result = new ValidationResult();
            result.TotalRows = dt.Rows.Count;

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
            var seenPhones = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string phoneRaw = dt.Rows[i][phoneMapping.ExcelColumn] as string ?? "";
                string phone = ValidateAndNormalizePhone(phoneRaw);

                if (string.IsNullOrEmpty(phone))
                {
                    invalidCount++;
                    if (invalidCount <= 10)
                        result.Warnings.Add("Row " + (i + 2) + ": invalid phone '" + phoneRaw + "'");
                    continue;
                }

                if (seenPhones.Contains(phone))
                {
                    duplicateCount++;
                    if (duplicateCount <= 10)
                        result.Warnings.Add("Row " + (i + 2) + ": duplicate phone '" + phone + "'");
                    continue;
                }

                seenPhones.Add(phone);
                validCount++;
            }

            result.ValidRows = validCount;
            result.InvalidRows = invalidCount;
            result.DuplicateRows = duplicateCount;

            int totalSkipped = invalidCount + duplicateCount;
            if (totalSkipped > 10)
                result.Warnings.Add("... and " + (totalSkipped - 10) + " more invalid/duplicate rows.");

            return result;
        }

        // ---------------------------------------------------------------
        //  CSV CONVERSION
        // ---------------------------------------------------------------
        /// <summary>
        /// Converts the DataTable to a UTF-8 CSV byte array suitable for the CEOITBOX API.
        /// First column = Phone (normalised), remaining columns = template variables in order.
        /// </summary>
        public byte[] ConvertToCsvBytes(DataTable dt, CampaignConfig config)
        {
            var sb = new StringBuilder();

            // --- Header row ---
            var orderedMappings = config.ColumnMappings
                .OrderBy(m => m.IsPhone ? 0 : 1)           // phone first
                .ThenBy(m => m.IsPhone ? 0 : m.VariableIndex) // then variable order
                .ToList();

            sb.AppendLine(string.Join(",", orderedMappings.Select(m => CsvEscape(m.ExcelColumn))));

            // --- Data rows ---
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
            }

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
                    client.Timeout = TimeSpan.FromSeconds(120);
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
            catch (HttpRequestException ex)
            {
                return new SendResult { Success = false, StatusCode = 0, ErrorMessage = "Network error: " + ex.Message };
            }
            catch (TaskCanceledException)
            {
                return new SendResult { Success = false, StatusCode = 0, ErrorMessage = "Request timed out after 120 seconds." };
            }
            catch (Exception ex)
            {
                return new SendResult { Success = false, StatusCode = 0, ErrorMessage = "Unexpected error: " + ex.Message };
            }
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

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s ?? "";
            return s.Substring(0, max);
        }
    }
}