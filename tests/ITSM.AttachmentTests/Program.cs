using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnmolDristi.ITSM.BLL;

namespace AnmolDristi.ITSM.AttachmentTests
{
    internal static class Program
    {
        private static int _failures;

        private static int Main()
        {
            NoFileSelectionLeavesNoDirectory();
            MultipleValidFilesAreStoredUnderTicketGuidNames();
            MixedValidAndInvalidKeepsTheValidSample();
            ExactTwentyMebibyteBoundary();
            StreamLongerThanDeclaredLengthIsDeleted();
            TraversalNamesAndStoredPathsStayInsideRoot();
            MetadataFailureDeletesOnlyThatSample();
            ThrownMetadataFailureDeletesTheSample();
            DownloadAuthorizationRules();
            MissingAndUnauthorizedDownloadsShare404();
            AllowlistUsesTheFinalExtension();

            if (_failures == 0)
            {
                Console.WriteLine("All ITSM attachment tests passed.");
                return 0;
            }

            Console.WriteLine(_failures + " ITSM attachment test(s) failed.");
            return 1;
        }

        private static void NoFileSelectionLeavesNoDirectory()
        {
            using (var root = new TempRoot())
            {
                var none = AttachmentUploadService.SaveFiles(root.Path, 41, null, InsertOk);
                var blank = AttachmentUploadService.SaveFiles(
                    root.Path,
                    41,
                    new List<IncomingAttachment>
                    {
                        new IncomingAttachment { FileName = "", DeclaredLength = 0, Content = StreamOf(0) }
                    },
                    InsertOk);
                Expect(none.Count == 0, "no-file selection reports no failures");
                Expect(blank.Count == 0, "blank posted file is treated as no selection");
                Expect(!Directory.Exists(Path.Combine(root.Path, "41")), "no-file selection creates no ticket directory");
            }
        }

        private static void MultipleValidFilesAreStoredUnderTicketGuidNames()
        {
            using (var root = new TempRoot())
            {
                var savedNames = new List<string>();
                var failures = AttachmentUploadService.SaveFiles(
                    root.Path,
                    7,
                    new List<IncomingAttachment>
                    {
                        Sample("notes.pdf", 4),
                        Sample("photo.JPG", 6),
                        Sample("sheet.xlsx", 3)
                    },
                    (relative, display) =>
                    {
                        savedNames.Add(display + "|" + relative);
                        return null;
                    });

                Expect(failures.Count == 0, "three allowed files produce no failures");
                Expect(savedNames.Count == 3, "three allowed files are inserted");
                var files = Directory.GetFiles(Path.Combine(root.Path, "7"));
                Expect(files.Length == 3, "three files exist under the ticket directory");
                var ticketDir = Path.Combine(root.Path, "7") + Path.DirectorySeparatorChar;
                Expect(files.All(path => path.StartsWith(ticketDir, StringComparison.OrdinalIgnoreCase)),
                    "stored files stay under the ticket directory");
                foreach (var entry in savedNames)
                {
                    var parts = entry.Split('|');
                    var storedName = Path.GetFileName(parts[1].Replace('/', Path.DirectorySeparatorChar));
                    Expect(!storedName.Equals(parts[0], StringComparison.OrdinalIgnoreCase),
                        "stored name is not the client name " + parts[0]);
                }
            }
        }

        private static void MixedValidAndInvalidKeepsTheValidSample()
        {
            using (var root = new TempRoot())
            {
                var inserted = 0;
                var failures = AttachmentUploadService.SaveFiles(
                    root.Path,
                    8,
                    new List<IncomingAttachment>
                    {
                        Sample("ok.pdf", 5),
                        Sample("run.exe", 5),
                        Sample("bundle.zip", 5),
                        Sample("script.aspx", 5),
                        Sample("also-ok.docx", 2)
                    },
                    (relative, display) =>
                    {
                        inserted++;
                        return null;
                    });

                Expect(inserted == 2, "mixed batch inserts only the allowed files");
                Expect(failures.Count == 3, "mixed batch reports each rejected file");
                Expect(failures.Any(f => f.FileName == "run.exe" && f.Reason == AttachmentValidationService.ReasonType),
                    "exe is rejected by the allowlist");
                Expect(failures.Any(f => f.FileName == "bundle.zip"), "zip stays disabled");
                Expect(Directory.GetFiles(Path.Combine(root.Path, "8")).Length == 2, "rejected files are not written");
            }
        }

        private static void ExactTwentyMebibyteBoundary()
        {
            using (var root = new TempRoot())
            {
                var exact = AttachmentUploadService.SaveFiles(
                    root.Path,
                    9,
                    new List<IncomingAttachment> { Sample("exact.pdf", AttachmentValidationService.MaxBytes) },
                    InsertOk);
                var stored = Directory.GetFiles(Path.Combine(root.Path, "9"));
                Expect(exact.Count == 0 && stored.Length == 1, "exactly 20 MiB is stored");
                Expect(new FileInfo(stored[0]).Length == AttachmentValidationService.MaxBytes, "stored length is 20971520 bytes");

                var over = AttachmentUploadService.SaveFiles(
                    root.Path,
                    9,
                    new List<IncomingAttachment> { Sample("over.pdf", AttachmentValidationService.MaxBytes + 1) },
                    InsertOk);
                Expect(over.Count == 1 && over[0].Reason == AttachmentValidationService.ReasonSize,
                    "20971521 declared bytes are rejected before write");
                Expect(Directory.GetFiles(Path.Combine(root.Path, "9")).Length == 1, "over-size file is not added");

                var lied = AttachmentUploadService.SaveFiles(
                    root.Path,
                    19,
                    new List<IncomingAttachment>
                    {
                        new IncomingAttachment
                        {
                            FileName = "lied.pdf",
                            DeclaredLength = AttachmentValidationService.MaxBytes,
                            Content = new RepeatStream(AttachmentValidationService.MaxBytes + 1)
                        }
                    },
                    InsertOk);
                Expect(lied.Count == 1 && lied[0].Reason == AttachmentValidationService.ReasonSize,
                    "a stream past 20 MiB is deleted even when the declared length is 20 MiB");
                Expect(!Directory.Exists(Path.Combine(root.Path, "19")),
                    "the over-long stream does not leave a file");
            }
        }

        private static void StreamLongerThanDeclaredLengthIsDeleted()
        {
            using (var root = new TempRoot())
            {
                var called = false;
                var failures = AttachmentUploadService.SaveFiles(
                    root.Path,
                    10,
                    new List<IncomingAttachment>
                    {
                        new IncomingAttachment
                        {
                            FileName = "short.pdf",
                            DeclaredLength = 4,
                            Content = new RepeatStream(5)
                        }
                    },
                    (relative, display) =>
                    {
                        called = true;
                        return null;
                    });

                Expect(!called, "a stream longer than its declared length is not inserted");
                Expect(failures.Count == 1 && failures[0].Reason == AttachmentValidationService.ReasonSave,
                    "declared-length overrun is reported as a save failure");
                Expect(!Directory.Exists(Path.Combine(root.Path, "10")), "partial overrun file and empty directory are removed");
            }
        }

        private static void TraversalNamesAndStoredPathsStayInsideRoot()
        {
            using (var root = new TempRoot())
            {
                var outside = Path.Combine(Path.GetDirectoryName(root.Path), "sentinel.txt");
                File.WriteAllText(outside, "secret");
                try
                {
                    var failures = AttachmentUploadService.SaveFiles(
                        root.Path,
                        11,
                        new List<IncomingAttachment>
                        {
                            Sample("..\\..\\sentinel.txt", 4),
                            Sample("../notes.pdf", 4),
                            Sample("good.pdf", 4)
                        },
                        InsertOk);

                    Expect(failures.Count == 2, "path-like names are rejected");
                    Expect(failures.All(f => f.Reason == AttachmentValidationService.ReasonName), "traversal uses the file-name reason");
                    Expect(File.ReadAllText(outside) == "secret", "traversal does not overwrite a file outside the root");
                    Expect(Directory.GetFiles(Path.Combine(root.Path, "11")).Length == 1, "the safe file is still stored");
                    Expect(AttachmentUploadService.ResolveExistingFile(root.Path, "../sentinel.txt") == null,
                        "a stored relative path with .. does not resolve");
                    Expect(AttachmentUploadService.ResolveExistingFile(root.Path, "11/../../sentinel.txt") == null,
                        "a nested .. segment does not resolve");
                    var stored = Directory.GetFiles(Path.Combine(root.Path, "11"))[0];
                    var relative = "11/" + Path.GetFileName(stored);
                    var resolved = AttachmentUploadService.ResolveExistingFile(root.Path, relative);
                    Expect(string.Equals(resolved, stored, StringComparison.OrdinalIgnoreCase),
                        "a guid path inside the ticket directory resolves");
                }
                finally
                {
                    File.Delete(outside);
                }
            }
        }

        private static void MetadataFailureDeletesOnlyThatSample()
        {
            using (var root = new TempRoot())
            {
                var failures = AttachmentUploadService.SaveFiles(
                    root.Path,
                    12,
                    new List<IncomingAttachment>
                    {
                        Sample("bad-meta.pdf", 4),
                        Sample("kept.pdf", 4)
                    },
                    (relative, display) => display == "bad-meta.pdf"
                        ? new InvalidOperationException("metadata")
                        : null);

                Expect(failures.Count == 1 && failures[0].FileName == "bad-meta.pdf",
                    "metadata failure is reported for that file");
                Expect(failures[0].Reason == AttachmentValidationService.ReasonSave,
                    "metadata failure uses the save reason");
                Expect(Directory.GetFiles(Path.Combine(root.Path, "12")).Length == 1,
                    "the successful file remains after the other metadata insert fails");
            }
        }

        private static void ThrownMetadataFailureDeletesTheSample()
        {
            using (var root = new TempRoot())
            {
                var failures = AttachmentUploadService.SaveFiles(
                    root.Path,
                    13,
                    new List<IncomingAttachment> { Sample("throws.pdf", 3) },
                    (relative, display) => { throw new InvalidOperationException("metadata"); });

                Expect(failures.Count == 1, "a thrown metadata error is reported");
                Expect(!Directory.Exists(Path.Combine(root.Path, "13")), "a thrown metadata error deletes the file");
            }
        }

        private static void DownloadAuthorizationRules()
        {
            Expect(AttachmentAccessRules.AllowsTicketView("A", "A", "B", false, false, 1, 9),
                "requester can view even when the company differs");
            Expect(AttachmentAccessRules.AllowsTicketView("B", "A", "B", false, false, 1, 9),
                "assignee can view");
            Expect(AttachmentAccessRules.AllowsTicketView("C", "A", "B", true, false, 1, 9),
                "pending approver can view");
            Expect(AttachmentAccessRules.AllowsTicketView("C", "A", "B", false, true, 5, 5),
                "company viewer with ITSM.Ticket.View can view the matching company");
            Expect(!AttachmentAccessRules.AllowsTicketView("C", "A", "B", false, true, 5, 9),
                "company viewer cannot view another company");
            Expect(!AttachmentAccessRules.AllowsTicketView("C", "A", "B", false, false, 5, 5),
                "a user who is not requester, assignee, pending approver, or company viewer is denied");
            Expect(AttachmentAccessRules.AllowsTicketView("A", "A", "B", false, false, 5, null),
                "requester can view a ticket that has no company");
            Expect(!AttachmentAccessRules.AllowsTicketView("", "A", "B", false, false, 5, 5),
                "a blank actor is denied");
        }

        private static void MissingAndUnauthorizedDownloadsShare404()
        {
            var missing = AttachmentAccessRules.ConcealDownload(false, true);
            var unauthorized = AttachmentAccessRules.ConcealDownload(true, false);
            Expect(missing && unauthorized, "missing and unauthorized downloads are both concealed");
            Expect(
                AttachmentAccessRules.DownloadStatusCode(!missing)
                == AttachmentAccessRules.DownloadStatusCode(!unauthorized),
                "missing and unauthorized downloads use the same status");
            Expect(AttachmentAccessRules.DownloadStatusCode(false) == 404, "concealed download status is 404");
            Expect(AttachmentAccessRules.DownloadStatusCode(true) == 200, "authorized download status is 200");
            Expect(!AttachmentAccessRules.ConcealDownload(true, true), "an existing visible attachment is not concealed");
        }

        private static void AllowlistUsesTheFinalExtension()
        {
            var pdf = AttachmentValidationService.Validate("report.exe.pdf", 10);
            var blocked = AttachmentValidationService.Validate("report.pdf.exe", 10);
            var upper = AttachmentValidationService.Validate("scan.PDF", 10);
            var docx = AttachmentValidationService.Validate("notes.docx", 10);
            Expect(pdf.IsValid && pdf.Extension == ".pdf", "the final .pdf extension is allowed");
            Expect(!blocked.IsValid && blocked.Reason == AttachmentValidationService.ReasonType,
                "a final .exe extension is rejected");
            Expect(upper.IsValid && upper.Extension == ".pdf", "extension checks are case-insensitive");
            Expect(docx.IsValid && docx.Extension == ".docx", ".docx remains allowed");
        }

        private static Exception InsertOk(string relative, string display)
        {
            return null;
        }

        private static IncomingAttachment Sample(string name, long length)
        {
            return new IncomingAttachment
            {
                FileName = name,
                DeclaredLength = length,
                Content = new RepeatStream(length)
            };
        }

        private static Stream StreamOf(long length)
        {
            return new RepeatStream(length);
        }

        private static void Expect(bool condition, string message)
        {
            if (condition)
            {
                Console.WriteLine("PASS " + message);
                return;
            }

            _failures++;
            Console.WriteLine("FAIL " + message);
        }

        private sealed class RepeatStream : Stream
        {
            private readonly long _length;
            private long _position;

            public RepeatStream(long length)
            {
                _length = length;
            }

            public override bool CanRead { get { return true; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return false; } }
            public override long Length { get { return _length; } }
            public override long Position
            {
                get { return _position; }
                set { throw new NotSupportedException(); }
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                var remaining = _length - _position;
                if (remaining <= 0)
                {
                    return 0;
                }

                var n = (int)Math.Min(count, remaining);
                _position += n;
                return n;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class TempRoot : IDisposable
        {
            public TempRoot()
            {
                Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "itsm-r3-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Path);
            }

            public string Path { get; private set; }

            public void Dispose()
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, true);
                }
            }
        }
    }
}
