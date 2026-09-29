using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Web;

namespace AnmolDristi
{
    public class DownloadExpense : IHttpHandler
    {
        public void ProcessRequest_OLD(HttpContext context)
        {
            context.Response.Buffer = true;
            context.Response.Clear();

            // Validate input
            int expenseId;
            if (!int.TryParse(context.Request.QueryString["expenseId"], out expenseId))
            {
                context.Response.StatusCode = 400;
                context.Response.Write("Invalid expense id.");
                return;
            }

            string connString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            string attachmentPath = null;
            string attachmentFileName = null;
            string ownerLoginId = null;

            // Fetch attachment path and owner from DB
            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT e.AttachmentFileName, a.Login_Id " +
                    "FROM dbo.SalesDailyExpense e " +
                    "JOIN dbo.SalesDailyActivity a ON e.ActivityId = a.ActivityId " +
                    "WHERE e.ExpenseId = @ExpenseId", con))
                {
                    cmd.Parameters.AddWithValue("@ExpenseId", expenseId);
                    con.Open();
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (!r.Read())
                        {
                            context.Response.StatusCode = 404;
                            context.Response.Write("Expense not found.");
                            return;
                        }

                        attachmentPath = r["AttachmentFileName"] != DBNull.Value ? r["AttachmentFileName"].ToString() : null;
                        ownerLoginId = r["Login_Id"] != DBNull.Value ? r["Login_Id"].ToString() : null;
                    }
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                context.Response.Write("Error reading attachment metadata: " + ex.Message);
                return;
            }

            // Simple authorization: only allow owner to download (adjust as needed)
            string sessionLogin = context.Session != null && context.Session["Login_Id"] != null ? context.Session["Login_Id"].ToString() : null;
            if (!string.IsNullOrEmpty(ownerLoginId) && !string.IsNullOrEmpty(sessionLogin) && !ownerLoginId.Equals(sessionLogin, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 403;
                context.Response.Write("You are not authorized to download this file.");
                return;
            }

            if (string.IsNullOrWhiteSpace(attachmentPath))
            {
                context.Response.StatusCode = 404;
                context.Response.Write("No attachment recorded for this expense.");
                return;
            }

            // Normalize and map path to server filesystem
            string mappedPath;
            try
            {
                // Allow stored values in several formats:
                // - "~ /UploadedFiles/ClaimsAttachment/2025/09/file.pdf"
                // - "UploadedFiles/ClaimsAttachment/2025/09/file.pdf"
                // - "file.pdf" (if only filename stored)
                string trimmed = attachmentPath.Trim();
                if (trimmed.StartsWith("~"))
                {
                    mappedPath = context.Server.MapPath(trimmed);
                }
                else if (trimmed.StartsWith("/") || trimmed.StartsWith("\\"))
                {
                    // leading slash — treat as application-root relative
                    mappedPath = context.Server.MapPath("~" + trimmed.Replace("/", "\\"));
                }
                else if (trimmed.IndexOf("UploadedFiles", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // likely contains relative path starting from UploadedFiles
                    mappedPath = context.Server.MapPath("~/" + trimmed.Replace("/", "\\"));
                }
                else
                {
                    // only filename stored — assume default folder
                    mappedPath = context.Server.MapPath("~/UploadedFiles/ClaimsAttachment/" + trimmed);
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                context.Response.Write("Error mapping file path: " + ex.Message);
                return;
            }

            if (!File.Exists(mappedPath))
            {
                context.Response.StatusCode = 404;
                context.Response.Write("File not found on server.");
                return;
            }

            // Determine filename to send to user
            attachmentFileName = Path.GetFileName(mappedPath);

            try
            {
                // Use MimeMapping (available in .NET 4.5+) to get content type
                string mime = MimeMapping.GetMimeMapping(attachmentFileName);
                context.Response.Clear();
                context.Response.ContentType = mime ?? "application/octet-stream";
                context.Response.AddHeader("Content-Disposition", "attachment; filename=\"" + HttpUtility.UrlPathEncode(attachmentFileName) + "\"");

                // Transmit file efficiently
                context.Response.TransmitFile(mappedPath);
                context.Response.Flush();
            }
            catch (HttpException hex)
            {
                // Commonly thrown when client disconnects — just end
                context.Response.StatusCode = 500;
                context.Response.Write("Error transmitting file: " + hex.Message);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                context.Response.Write("Error streaming file: " + ex.Message);
            }
            finally
            {
                // End response to avoid extra HTML
                try { context.Response.End(); } catch { /* swallow thread abort */ }
            }
        }

        public void ProcessRequest(HttpContext context)
        {
            context.Response.Clear();
            context.Response.BufferOutput = true;
            int expenseId;
            // Validate input
            if (!int.TryParse(context.Request.QueryString["expenseId"], out expenseId))
            {
                context.Response.StatusCode = 400;
                context.Response.Write("Invalid expense id.");
                return;
            }

            string connString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            string storedAttachment = null;     // this will be either a relative path or just filename
            string ownerLoginId = null;

            // Read stored attachment value (prefer AttachmentFileName, fallback to AttachmentPath)
            try
            {
                using (var con = new SqlConnection(connString))
                using (var cmd = new SqlCommand(
                    "SELECT COALESCE(NULLIF(e.AttachmentFileName, ''), NULLIF(e.AttachmentPath, '')) AS StoredAttachment, a.Login_Id " +
                    "FROM dbo.SalesDailyExpense e " +
                    "JOIN dbo.SalesDailyActivity a ON e.ActivityId = a.ActivityId " +
                    "WHERE e.ExpenseId = @ExpenseId", con))
                {
                    cmd.Parameters.AddWithValue("@ExpenseId", expenseId);
                    con.Open();
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read())
                        {
                            context.Response.StatusCode = 404;
                            context.Response.Write("Expense not found.");
                            return;
                        }

                        storedAttachment = r["StoredAttachment"] != DBNull.Value ? r["StoredAttachment"].ToString().Trim() : null;
                        ownerLoginId = r["Login_Id"] != DBNull.Value ? r["Login_Id"].ToString() : null;
                    }
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                context.Response.Write("Error reading attachment metadata.");
                // Log full exception server-side for debugging:
                System.Diagnostics.Trace.WriteLine("DownloadHandler: DB read error: " + ex.ToString());
                return;
            }

            // Authorization (owner-only). Adjust logic if you want managers/admins to access.
            string sessionLogin = context.Session != null && context.Session["Login_Id"] != null ? context.Session["Login_Id"].ToString() : null;
            if (!string.IsNullOrEmpty(ownerLoginId) && !string.IsNullOrEmpty(sessionLogin)
                && !ownerLoginId.Equals(sessionLogin, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 403;
                context.Response.Write("You are not authorized to download this file.");
                System.Diagnostics.Trace.WriteLine($"DownloadHandler: Auth failed. owner='{ownerLoginId}' session='{sessionLogin}'");
                return;
            }

            if (string.IsNullOrWhiteSpace(storedAttachment))
            {
                context.Response.StatusCode = 404;
                context.Response.Write("No attachment recorded for this expense.");
                return;
            }

            // Map storedAttachment (various possible formats) to physical file path
            string mappedPath = MapStoredToPhysicalPath(storedAttachment, context.Server);

            // Diagnostic trace to help quickly identify mapping problems -- remove in production if desired
            System.Diagnostics.Trace.WriteLine($"DownloadHandler: ExpenseId={expenseId}, Stored='{storedAttachment}', Mapped='{mappedPath}'");

            if (string.IsNullOrEmpty(mappedPath) || !File.Exists(mappedPath))
            {
                context.Response.StatusCode = 404;
                context.Response.Write("File not found on server.");
                return;
            }

            // Determine filename to send to user
            string sendFileName = Path.GetFileName(mappedPath);
            try
            {
                string mime = MimeMapping.GetMimeMapping(sendFileName) ?? "application/octet-stream";

                // Clear possible previous output
                context.Response.Clear();
                context.Response.ContentType = mime;
                context.Response.BufferOutput = true;

                // Add a safe Content-Disposition supporting UTF-8 (filename*), fall back to quoted filename
                // Use Uri.EscapeDataString to produce proper percent-encoding for filename*
                string asciiSafe = sendFileName; // we'll put this in quoted filename (browser fallback)
                string utf8Escaped = Uri.EscapeDataString(sendFileName);
                string contentDisposition = $"attachment; filename=\"{asciiSafe}\"; filename*=UTF-8''{utf8Escaped}";
                context.Response.AddHeader("Content-Disposition", contentDisposition);

                // Optionally set content-length (useful for some clients)
                try
                {
                    var fi = new FileInfo(mappedPath);
                    context.Response.AddHeader("Content-Length", fi.Length.ToString());
                }
                catch { /* ignore if file info fails */ }

                // Efficient transmit
                context.Response.TransmitFile(mappedPath);
                context.Response.Flush();
            }
            catch (HttpException hex)
            {
                // often indicates client disconnected
                System.Diagnostics.Trace.WriteLine("DownloadHandler: HttpException while transmitting: " + hex.ToString());
                // Don't reveal internal details to client
                context.Response.StatusCode = 500;
                context.Response.Write("Error transmitting file.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("DownloadHandler: Error while streaming file: " + ex.ToString());
                context.Response.StatusCode = 500;
                context.Response.Write("Error streaming file.");
            }
            finally
            {
                // CompleteRequest instead of Response.End
                try
                {
                    context.ApplicationInstance.CompleteRequest();
                }
                catch { /* swallow */ }
            }
        }

        // Helper: robust mapping of DB-stored string to physical file path
        private string MapStoredToPhysicalPath(string stored, HttpServerUtility server)
        {
            if (string.IsNullOrWhiteSpace(stored)) return null;

            string normalized = stored.Replace('\\', '/').Trim();

            // 1) If already starts with "~/" use MapPath directly
            if (normalized.StartsWith("~/", StringComparison.Ordinal))
            {
                try { return server.MapPath(normalized); } catch { return null; }
            }

            // 2) Leading slash => application-root relative
            if (normalized.StartsWith("/", StringComparison.Ordinal))
            {
                try { return server.MapPath("~" + normalized); } catch { return null; }
            }

            // 3) Contains "UploadedFiles" -> treat as app-root relative
            if (normalized.IndexOf("UploadedFiles", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                string rel = normalized.TrimStart('~', '/', '\\');
                try { return server.MapPath("~/" + rel); } catch { return null; }
            }

            // 4) Plain filename fallback: try to locate under UploadedFiles/ClaimsAttachment/{yyyy}/{MM}/filename
            //    Only a shallow search over two levels to avoid scanning entire site
            try
            {
                string claimsRoot = server.MapPath("~/UploadedFiles/ClaimsAttachment/");
                if (!Directory.Exists(claimsRoot)) return null;

                // First, check direct location(s) that might be used in some older code
                string candidateRoot = Path.Combine(claimsRoot, normalized);
                if (File.Exists(candidateRoot)) return candidateRoot;

                // enumerate year directories (e.g. 2024, 2025, ...)
                foreach (var yearDir in Directory.EnumerateDirectories(claimsRoot))
                {
                    // check year root
                    string candidateInYear = Path.Combine(yearDir, normalized);
                    if (File.Exists(candidateInYear)) return candidateInYear;

                    // enumerate months
                    foreach (var monthDir in Directory.EnumerateDirectories(yearDir))
                    {
                        string candidate = Path.Combine(monthDir, normalized);
                        if (File.Exists(candidate)) return candidate;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("MapStoredToPhysicalPath: search error: " + ex.ToString());
                // swallow and return null so caller returns 404
            }

            return null;
        }


        public bool IsReusable => false;
    }
}