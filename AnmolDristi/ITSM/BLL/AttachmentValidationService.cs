using System;
using System.Collections.Generic;
using System.IO;

namespace AnmolDristi.ITSM.BLL
{
    /// <summary>
    /// Extension allowlist from docs/05-data-architecture/compliance-and-security.md §1.1.
    /// ZIP stays disabled. No MIME map is applied.
    /// </summary>
    public static class AttachmentValidationService
    {
        public const long MaxBytes = 20971520L;

        public const string ReasonEmpty = "The file is empty.";

        public const string ReasonName = "The file name is not valid.";

        public const string ReasonType = "This file type is not allowed.";

        public const string ReasonSize = "The file is larger than 20 MB.";

        public const string ReasonSave = "The file could not be saved.";

        private static readonly HashSet<string> AllowedExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv",
                ".png", ".jpg", ".jpeg", ".gif"
            };

        public static AttachmentValidationResult Validate(string fileName, long declaredLength)
        {
            if (string.IsNullOrWhiteSpace(fileName) || declaredLength <= 0)
            {
                return Invalid(Label(fileName), ReasonEmpty);
            }

            if (fileName.IndexOf("..", StringComparison.Ordinal) >= 0
                || fileName.IndexOfAny(new[] { '/', '\\', ':' }) >= 0
                || HasControlCharacter(fileName))
            {
                return Invalid(Label(fileName), ReasonName);
            }

            string displayName;
            try
            {
                displayName = Path.GetFileName(fileName);
            }
            catch (ArgumentException)
            {
                return Invalid("attachment", ReasonName);
            }

            if (string.IsNullOrWhiteSpace(displayName)
                || displayName.Length > 255
                || displayName.IndexOf("..", StringComparison.Ordinal) >= 0
                || HasControlCharacter(displayName))
            {
                return Invalid(Label(displayName), ReasonName);
            }

            var baseName = Path.GetFileNameWithoutExtension(displayName);
            if (string.IsNullOrWhiteSpace(baseName))
            {
                return Invalid(displayName, ReasonName);
            }

            var extension = Path.GetExtension(displayName);
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            {
                return Invalid(displayName, ReasonType);
            }

            if (declaredLength > MaxBytes)
            {
                return Invalid(displayName, ReasonSize);
            }

            return new AttachmentValidationResult
            {
                IsValid = true,
                DisplayName = displayName,
                Extension = extension.ToLowerInvariant()
            };
        }

        private static bool HasControlCharacter(string value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                if (char.IsControl(value[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static string Label(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return "attachment";
            }

            var label = fileName.Replace('\r', ' ').Replace('\n', ' ');
            if (label.Length > 255)
            {
                label = label.Substring(0, 255);
            }

            return label;
        }

        private static AttachmentValidationResult Invalid(string name, string reason)
        {
            return new AttachmentValidationResult
            {
                IsValid = false,
                DisplayName = string.IsNullOrWhiteSpace(name) ? "attachment" : name,
                Reason = reason
            };
        }
    }

    public sealed class AttachmentValidationResult
    {
        public bool IsValid { get; set; }

        public string DisplayName { get; set; }

        public string Extension { get; set; }

        public string Reason { get; set; }
    }
}
