using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Web;

namespace AnmolDristi
{
    public class DownloadExpense : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
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

        public bool IsReusable => false;
    }
}