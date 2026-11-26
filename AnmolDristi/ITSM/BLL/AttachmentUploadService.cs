using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;

namespace AnmolDristi.ITSM.BLL
{
    /// <summary>
    /// Writes validated bytes under the ITSM attachment root, then asks the
    /// caller to insert metadata. A metadata failure deletes that file only.
    /// </summary>
    public static class AttachmentUploadService
    {
        public delegate Exception MetadataInsert(string relativePath, string displayName);

        public static List<AttachmentFailure> SaveFiles(
            string attachmentRoot,
            int ticketId,
            IList<IncomingAttachment> files,
            MetadataInsert insertMetadata)
        {
            if (string.IsNullOrWhiteSpace(attachmentRoot))
            {
                throw new ArgumentException("Attachment root is required.", "attachmentRoot");
            }

            if (ticketId <= 0)
            {
                throw new ArgumentOutOfRangeException("ticketId");
            }

            if (insertMetadata == null)
            {
                throw new ArgumentNullException("insertMetadata");
            }

            var failures = new List<AttachmentFailure>();
            if (files == null || files.Count == 0)
            {
                return failures;
            }

            var keptFile = false;
            foreach (var file in files)
            {
                if (file == null
                    || (string.IsNullOrWhiteSpace(file.FileName) && file.DeclaredLength <= 0))
                {
                    continue;
                }

                var validation = AttachmentValidationService.Validate(
                    file.FileName,
                    file.DeclaredLength);
                if (!validation.IsValid)
                {
                    failures.Add(Failure(validation.DisplayName, validation.Reason));
                    continue;
                }

                if (file.Content == null)
                {
                    failures.Add(Failure(validation.DisplayName, AttachmentValidationService.ReasonEmpty));
                    continue;
                }

                string failureReason;
                var saved = WriteOne(
                    attachmentRoot,
                    ticketId,
                    validation,
                    file,
                    insertMetadata,
                    out failureReason);
                if (!saved)
                {
                    failures.Add(Failure(validation.DisplayName, failureReason));
                }
                else
                {
                    keptFile = true;
                }
            }

            if (!keptFile)
            {
                RemoveTicketDirectoryIfEmpty(attachmentRoot, ticketId);
            }

            return failures;
        }

        public static string ResolveExistingFile(string attachmentRoot, string relativePath)
        {
            string fullPath;
            if (!TryCombineUnderRoot(attachmentRoot, relativePath, true, out fullPath))
            {
                return null;
            }

            return fullPath;
        }

        private static bool WriteOne(
            string attachmentRoot,
            int ticketId,
            AttachmentValidationResult validation,
            IncomingAttachment file,
            MetadataInsert insertMetadata,
            out string failureReason)
        {
            failureReason = AttachmentValidationService.ReasonSave;
            var relative = ticketId.ToString(CultureInfo.InvariantCulture)
                + "/"
                + Guid.NewGuid().ToString("N")
                + validation.Extension;

            string fullPath;
            if (!TryCombineUnderRoot(attachmentRoot, relative, false, out fullPath))
            {
                return false;
            }

            var exceeded = false;
            long total = 0;
            try
            {
                var directory = Path.GetDirectoryName(fullPath);
                Directory.CreateDirectory(directory);
                using (var output = new FileStream(
                    fullPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    var buffer = new byte[81920];
                    int read;
                    while ((read = file.Content.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        total += read;
                        if (total > AttachmentValidationService.MaxBytes
                            || total > file.DeclaredLength)
                        {
                            exceeded = true;
                            break;
                        }

                        output.Write(buffer, 0, read);
                    }
                }
            }
            catch (IOException ex)
            {
                DeleteIfExists(fullPath);
                TraceFailure(ticketId, validation.DisplayName, ex);
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                DeleteIfExists(fullPath);
                TraceFailure(ticketId, validation.DisplayName, ex);
                return false;
            }

            if (exceeded || total <= 0)
            {
                DeleteIfExists(fullPath);
                if (exceeded && total > AttachmentValidationService.MaxBytes)
                {
                    failureReason = AttachmentValidationService.ReasonSize;
                }
                else if (total <= 0)
                {
                    failureReason = AttachmentValidationService.ReasonEmpty;
                }

                return false;
            }

            if (!TryInsert(insertMetadata, relative, validation.DisplayName, ticketId))
            {
                DeleteIfExists(fullPath);
                return false;
            }

            return true;
        }

        private static bool TryInsert(
            MetadataInsert insertMetadata,
            string relativePath,
            string displayName,
            int ticketId)
        {
            try
            {
                var error = insertMetadata(relativePath, displayName);
                if (error == null)
                {
                    return true;
                }

                TraceFailure(ticketId, displayName, error);
                return false;
            }
            catch (IOException ex)
            {
                TraceFailure(ticketId, displayName, ex);
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                TraceFailure(ticketId, displayName, ex);
                return false;
            }
            catch (DataException ex)
            {
                TraceFailure(ticketId, displayName, ex);
                return false;
            }
            catch (InvalidOperationException ex)
            {
                TraceFailure(ticketId, displayName, ex);
                return false;
            }
        }

        private static bool TryCombineUnderRoot(
            string attachmentRoot,
            string relativePath,
            bool requireExistingFile,
            out string fullPath)
        {
            fullPath = null;
            if (string.IsNullOrWhiteSpace(attachmentRoot)
                || string.IsNullOrWhiteSpace(relativePath))
            {
                return false;
            }

            if (relativePath.IndexOf("..", StringComparison.Ordinal) >= 0
                || relativePath.IndexOf(':') >= 0
                || Path.IsPathRooted(relativePath))
            {
                return false;
            }

            var rootFull = Path.GetFullPath(attachmentRoot);
            var separator = Path.DirectorySeparatorChar;
            if (rootFull[rootFull.Length - 1] != separator)
            {
                rootFull = rootFull + separator;
            }

            var normalized = relativePath
                .Replace('/', separator)
                .Replace('\\', separator);
            var combined = Path.GetFullPath(Path.Combine(rootFull, normalized));
            if (!combined.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (requireExistingFile && !File.Exists(combined))
            {
                return false;
            }

            fullPath = combined;
            return true;
        }

        private static void RemoveTicketDirectoryIfEmpty(string attachmentRoot, int ticketId)
        {
            try
            {
                string directory;
                var relative = ticketId.ToString(CultureInfo.InvariantCulture);
                if (!TryCombineUnderRoot(attachmentRoot, relative, false, out directory))
                {
                    return;
                }

                if (!Directory.Exists(directory))
                {
                    return;
                }

                if (Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    return;
                }

                Directory.Delete(directory);
            }
            catch (IOException ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "ITSM attachment directory cleanup failed: " + ex.GetType().Name);
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "ITSM attachment directory cleanup failed: " + ex.GetType().Name);
            }
        }

        private static void DeleteIfExists(string fullPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch (IOException ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "ITSM attachment cleanup failed: " + ex.GetType().Name);
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "ITSM attachment cleanup failed: " + ex.GetType().Name);
            }
        }

        private static void TraceFailure(int ticketId, string displayName, Exception ex)
        {
            System.Diagnostics.Trace.TraceError(
                "ITSM attachment failed for ticket "
                + ticketId.ToString(CultureInfo.InvariantCulture)
                + " file "
                + displayName
                + ": "
                + ex.GetType().Name);
        }

        private static AttachmentFailure Failure(string fileName, string reason)
        {
            return new AttachmentFailure
            {
                FileName = fileName,
                Reason = reason
            };
        }
    }
}
