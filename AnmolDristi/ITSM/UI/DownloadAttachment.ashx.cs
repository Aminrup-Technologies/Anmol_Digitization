using System;
using System.IO;
using System.Text;
using System.Web;
using System.Web.SessionState;
using AnmolDristi.ITSM.BLL;

namespace AnmolDristi.ITSM.UI
{
    /// <summary>
    /// Authorized ticket-attachment download.
    /// Direct requests to ~/UploadedFiles/ITSM/ are not blocked by this
    /// handler. That denial is an IIS deployment prerequisite and is not
    /// claimed here.
    /// </summary>
    public class DownloadAttachment : IHttpHandler, IReadOnlySessionState
    {
        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            if (context.Session == null
                || context.Session["WORKMAN"] == null
                || context.Session["USERID"] == null
                || context.Session["BRANCH"] == null)
            {
                context.Response.Redirect("~/login.aspx", false);
                context.ApplicationInstance.CompleteRequest();
                return;
            }

            int attachmentId;
            if (!int.TryParse(context.Request.QueryString["id"], out attachmentId) || attachmentId <= 0)
            {
                WriteNotFound(context);
                return;
            }

            var actorCode = context.Session["WORKMAN"].ToString();
            var hasTicketViewPermission = BasePage.SessionHasPermission(
                context,
                "ITSM.Ticket.View");
            var authorizedCompanyId = ItsmTenantContext.ResolveCompanyId(actorCode);
            var download = new TicketAttachmentService().ResolveDownload(
                attachmentId,
                actorCode,
                hasTicketViewPermission,
                authorizedCompanyId,
                context.Server.MapPath("~/UploadedFiles/ITSM"));

            var sendFile = download != null
                && !string.IsNullOrEmpty(download.PhysicalPath)
                && File.Exists(download.PhysicalPath);
            if (AttachmentAccessRules.DownloadStatusCode(sendFile) == 404)
            {
                WriteNotFound(context);
                return;
            }

            var downloadName = HeaderFileName(download.DownloadName);
            context.Response.Clear();
            context.Response.StatusCode = 200;
            context.Response.ContentType = MimeMapping.GetMimeMapping(downloadName);
            context.Response.AddHeader("X-Content-Type-Options", "nosniff");
            context.Response.AddHeader(
                "Content-Disposition",
                "attachment; filename=\""
                + downloadName
                + "\"; filename*=UTF-8''"
                + Uri.EscapeDataString(downloadName));
            context.Response.TransmitFile(download.PhysicalPath);
            context.ApplicationInstance.CompleteRequest();
        }

        private static void WriteNotFound(HttpContext context)
        {
            context.Response.Clear();
            context.Response.StatusCode = AttachmentAccessRules.DownloadStatusCode(false);
            context.Response.TrySkipIisCustomErrors = true;
            context.Response.ContentType = "text/plain";
            context.Response.Write("Not found.");
        }

        private static string HeaderFileName(string value)
        {
            var builder = new StringBuilder();
            if (value != null)
            {
                foreach (var ch in value)
                {
                    if (ch == '"' || ch == '\\' || ch < 32 || ch > 126)
                    {
                        continue;
                    }

                    builder.Append(ch);
                }
            }

            return builder.Length == 0 ? "attachment" : builder.ToString();
        }
    }
}
