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
}