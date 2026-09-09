using System;
using System.Collections.Generic;

namespace AnmolDristi
{
    /// <summary>
    /// Maps an Excel column to a role in the WhatsApp bulk campaign.
    /// </summary>
    public class ColumnMapping
    {
        /// <summary>Original column header name from the Excel file.</summary>
        public string ExcelColumn { get; set; }

        /// <summary>If true, this column is the recipient phone number.</summary>
        public bool IsPhone { get; set; }

        /// <summary>If true, this column is sent as a template variable.</summary>
        public bool IsVariable { get; set; }

        /// <summary>Positional variable index (1-based). Set only when IsVariable is true.</summary>
        public int VariableIndex { get; set; }
    }

    /// <summary>
    /// Diagnostic data from Excel parsing — used to trace row lifecycle.
    /// </summary>
    public class ParseDiagnostics
    {
        public int WorksheetStartRow { get; set; }
        public int WorksheetEndRow { get; set; }
        public int WorksheetStartCol { get; set; }
        public int WorksheetEndCol { get; set; }
        public string HeaderRowContent { get; set; }
        public int ExpectedDataRows { get; set; }
        public int EmptyRowsSkipped { get; set; }
        public int DataTableRows { get; set; }
        public int DataTableColumns { get; set; }
        public string ColumnNames { get; set; }

        public ParseDiagnostics()
        {
            HeaderRowContent = string.Empty;
            ColumnNames = string.Empty;
        }
    }

    /// <summary>
    /// Result of validating an uploaded DataTable against the required column mappings.
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get { return Errors.Count == 0; } }
        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int InvalidRows { get; set; }
        public int DuplicateRows { get; set; }
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }

        // Diagnostic: confirm DataTable was not mutated during validation
        public int InputDataTableRows { get; set; }
        public int OutputDataTableRows { get; set; }

        /// <summary>Total number of warning messages generated (not truncated).</summary>
        public int TotalWarningsGenerated { get; set; }

        public ValidationResult()
        {
            Errors = new List<string>();
            Warnings = new List<string>();
        }
    }

    /// <summary>
    /// Reads CEOITBOX integration settings from Web.config appSettings.
    /// Credentials never appear in the UI or C# source.
    /// </summary>
    public class CEOITBOXConfig
    {
        public string BaseUrl { get; set; }
        public string ApiKey { get; set; }
        public string PhoneId { get; set; }
        public bool UseBasicAuthentication { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    /// <summary>
    /// Captures the user's campaign configuration from the UI.
    /// </summary>
    public class CampaignConfig
    {
        public string CampaignName { get; set; }
        public string TemplateName { get; set; }
        public string Language { get; set; }
        public List<ColumnMapping> ColumnMappings { get; set; }

        public CampaignConfig()
        {
            ColumnMappings = new List<ColumnMapping>();
            Language = "en";
        }
    }

    /// <summary>
    /// Represents the response from the CEOITBOX bulk campaign API.
    /// </summary>
    public class SendResult
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public string ResponseBody { get; set; }
        public string ErrorMessage { get; set; }

        public SendResult()
        {
            Message = string.Empty;
            ResponseBody = string.Empty;
            ErrorMessage = string.Empty;
        }
    }

    // =====================================================================
    //  PHASE 2 — CONTENT / MEDIA / DISPATCH PLAN
    // =====================================================================

    /// <summary>How message content is sourced for recipients.</summary>
    public enum ContentMode
    {
        None = 0,
        CommonForAll = 1,
        IndividualFile = 2,
        WorkbookSheet = 3,
        ExcelColumnUrl = 4
    }

    /// <summary>Type of media/content associated with a message.</summary>
    public enum ContentType
    {
        None = 0,
        Document = 1,
        Image = 2,
        Video = 3
    }

    /// <summary>WhatsApp template header format — participates in media compatibility validation.</summary>
    public enum TemplateHeaderFormat
    {
        BodyOnly = 0,
        Image = 1,
        Document = 2,
        Video = 3
    }

    /// <summary>Lifecycle state of a dispatch item. Kept explicit — do not equate Accepted with Delivered.</summary>
    public static class DispatchStatus
    {
        public const string Pending = "Pending";
        public const string Ready = "Ready";
        public const string Blocked = "Blocked";
        public const string Processing = "Processing";
        public const string Uploaded = "Uploaded";
        public const string Accepted = "Accepted";   // CEOITBOX accepted the send request (HTTP 200)
        public const string Sent = "Sent";            // Confirmed sent by reporting
        public const string Delivered = "Delivered";  // Confirmed delivered by reporting
        public const string Read = "Read";            // Confirmed read by reporting
        public const string Failed = "Failed";
        public const string Skipped = "Skipped";
    }

    /// <summary>
    /// User-selected content configuration for the campaign (Phase 2).
    /// </summary>
    public class ContentConfig
    {
        public ContentMode Mode { get; set; }
        /// <summary>Media/content type (Image, Document, Video) for this campaign.</summary>
        public ContentType MediaType { get; set; }

        /// <summary>Original display filename (never used as a server path).</summary>
        public string FileName { get; set; }

        /// <summary>MIME type of the common attachment.</summary>
        public string MimeType { get; set; }

        /// <summary>GUID temp file path of the common attachment on the server.</summary>
        public string TempFilePath { get; set; }

        /// <summary>Excel column holding per-recipient mapping keys (IndividualFile / WorkbookSheet).</summary>
        public string MappingColumn { get; set; }

        /// <summary>Excel column holding per-recipient URLs (ExcelColumnUrl mode).</summary>
        public string UrlColumn { get; set; }

        /// <summary>GUID temp directory holding individual files (IndividualFile mode).</summary>
        public string IndividualFilesDir { get; set; }

        /// <summary>GUID temp path of the uploaded multi-sheet workbook (WorkbookSheet mode).</summary>
        public string WorkbookTempPath { get; set; }

        /// <summary>Sheet names found in the uploaded workbook (WorkbookSheet mode).</summary>
        public List<string> WorkbookSheetNames { get; set; }

        /// <summary>Mapping key → original file name (IndividualFile mode, for preview display).</summary>
        public Dictionary<string, string> IndividualFileNames { get; set; }

        /// <summary>Mapping key → safe GUID temp path on disk (IndividualFile mode). Never built from user input.</summary>
        public Dictionary<string, string> IndividualFilePaths { get; set; }

        public ContentConfig()
        {
            FileName = string.Empty;
            MimeType = string.Empty;
            TempFilePath = string.Empty;
            MappingColumn = string.Empty;
            UrlColumn = string.Empty;
            IndividualFilesDir = string.Empty;
            WorkbookTempPath = string.Empty;
            WorkbookSheetNames = new List<string>();
            IndividualFileNames = new Dictionary<string, string>();
            IndividualFilePaths = new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Per-recipient dispatch instruction produced by validation. The send engine
    /// executes these items as-is — it never rebuilds recipient/content mappings.
    /// </summary>
    public class RecipientDispatchItem
    {
        public int RowNumber { get; set; }
        public string PhoneNumber { get; set; }
        public string MappingKey { get; set; }
        public string TemplateName { get; set; }
        public string Language { get; set; }
        public Dictionary<string, string> TemplateVariables { get; set; }
        /// <summary>Media/content type (Image, Document, Video). Named ItemContentType to avoid shadowing the ContentType enum.</summary>
        public ContentType ItemContentType { get; set; }

        /// <summary>Content sourcing mode (CommonForAll, IndividualFile, etc). Named ItemContentMode to avoid shadowing the ContentMode enum.</summary>
        public ContentMode ItemContentMode { get; set; }

        /// <summary>Content reference: temp file path, URL, or extracted sheet path.</summary>
        public string ContentReference { get; set; }

        public string MediaFileName { get; set; }
        public string MediaMimeType { get; set; }

        /// <summary>Meta media handle from CEOITBOX media upload (NOT the CEOITBOX "id").</summary>
        public string MetaMediaId { get; set; }

        /// <summary>CEOITBOX system identifier returned by sendMessage (kept separate from MetaMediaId).</summary>
        public string CeoitboxSystemId { get; set; }

        /// <summary>WhatsApp message id (wamid) when present in the response.</summary>
        public string WhatsAppMessageId { get; set; }

        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public string ApiResponse { get; set; }
        public DateTime? ProcessedAt { get; set; }

        public RecipientDispatchItem()
        {
            PhoneNumber = string.Empty;
            MappingKey = string.Empty;
            TemplateName = string.Empty;
            Language = "en";
            TemplateVariables = new Dictionary<string, string>();
            ContentReference = string.Empty;
            MediaFileName = string.Empty;
            MediaMimeType = string.Empty;
            MetaMediaId = string.Empty;
            CeoitboxSystemId = string.Empty;
            WhatsAppMessageId = string.Empty;
            Status = DispatchStatus.Pending;
            ErrorMessage = string.Empty;
            ApiResponse = string.Empty;
        }
    }

    /// <summary>
    /// Single source of truth between validation and sending.
    /// </summary>
    public class DispatchPlan
    {
        public List<RecipientDispatchItem> Items { get; set; }
        public int TotalRecipients { get; set; }
        public int ReadyCount { get; set; }
        public int InvalidPhoneCount { get; set; }
        public int DuplicateCount { get; set; }
        public int MissingContentCount { get; set; }
        public int InvalidContentCount { get; set; }
        public int InvalidUrlCount { get; set; }

        /// <summary>Human-readable summary of all validation dimensions.</summary>
        public string ValidationSummary { get; set; }

        public ContentConfig Content { get; set; }
        public CampaignConfig Campaign { get; set; }

        public DispatchPlan()
        {
            Items = new List<RecipientDispatchItem>();
            ValidationSummary = string.Empty;
            Content = new ContentConfig();
            Campaign = new CampaignConfig();
        }
    }

    /// <summary>
    /// Result of the CEOITBOX media upload endpoint (POST /api/v1/whatsapp/meta/media/upload).
    /// CEOITBOX "id" and Meta "metaMediaId" are DIFFERENT — message sends must use metaMediaId.
    /// </summary>
    public class MediaUploadResult
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }

        /// <summary>CEOITBOX's own id (do NOT use for message sends).</summary>
        public string Id { get; set; }

        /// <summary>Meta media handle — use this in sendMessage calls.</summary>
        public string MetaMediaId { get; set; }

        public string ResponseBody { get; set; }
        public string ErrorMessage { get; set; }

        public MediaUploadResult()
        {
            Id = string.Empty;
            MetaMediaId = string.Empty;
            ResponseBody = string.Empty;
            ErrorMessage = string.Empty;
        }
    }

    /// <summary>
    /// Result of an individual sendMessage call for one recipient.
    /// </summary>
    public class IndividualSendResult
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string CeoitboxSystemId { get; set; }
        public string WhatsAppMessageId { get; set; }
        public string ResponseBody { get; set; }
        public string ErrorMessage { get; set; }

        public IndividualSendResult()
        {
            CeoitboxSystemId = string.Empty;
            WhatsAppMessageId = string.Empty;
            ResponseBody = string.Empty;
            ErrorMessage = string.Empty;
        }
    }

    /// <summary>
    /// Central configuration access for CEOITBOX Phase 2 settings.
    /// Reads from Web.config appSettings — no secrets in source code.
    /// </summary>
    public static class ConfigurationHelper
    {
        public static string GetStringSetting(string key, string defaultValue)
        {
            string v = System.Configuration.ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(v) ? defaultValue : v;
        }

        public static bool GetBoolSetting(string key, bool defaultValue)
        {
            string v = System.Configuration.ConfigurationManager.AppSettings[key];
            bool parsed;
            return bool.TryParse(v, out parsed) ? parsed : defaultValue;
        }

        public static long GetLongSetting(string key, long defaultValue)
        {
            string v = System.Configuration.ConfigurationManager.AppSettings[key];
            long parsed;
            return long.TryParse(v, out parsed) ? parsed : defaultValue;
        }
    }
}