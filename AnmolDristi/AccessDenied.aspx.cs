using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class AccessDenied : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.ContentType = "text/html; charset=utf-8";
            Response.StatusCode = 403;

            // small permission hint (optional)
            var perm = Request.QueryString["perm"] ?? "(not specified)";
            var userId = Session["CurrentUserId"] ?? "anonymous";
            var userName = Session["USERID"] ?? HttpContext.Current.User?.Identity?.Name ?? "unknown";

            // Log the denial (non-blocking)
            try
            {
                LogAccessDenied(Convert.ToInt32(Session["CurrentUserId"] ?? "0"), userName.ToString(), perm, Request.Url.ToString(), Request.UserHostAddress);
            }
            catch
            {
                // swallow any logging error - don't break the UX
            }

            Response.Write("<html><head><title>Access denied</title></head><body style='font-family:Segoe UI, Arial; padding:24px;'>");
            Response.Write("<h2>Access denied</h2>");
            Response.Write("<p>You do not have permission to access this page.</p>");
            Response.Write("<p><strong>Required permission:</strong> " + HttpUtility.HtmlEncode(perm) + "</p>");
            Response.Write("<p>If you think this is an error, contact your administrator.</p>");
            Response.Write("</body></html>");
            Response.End();
        }

        private void LogAccessDenied(int userId, string userIdentifier, string requiredPerm, string url, string ip)
        {
            // lightweight DB write - adjust connection string name as needed
            var connStr = System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (var cn = new System.Data.SqlClient.SqlConnection(connStr))
            using (var cmd = new System.Data.SqlClient.SqlCommand(@"
                INSERT INTO dbo.AccessDeniedLog (UserId, UserIdentifier, RequiredPermission, Url, IpAddress, EventTime)
                VALUES (@uid, @uident, @perm, @url, @ip, SYSDATETIME())", cn))
            {
                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.Parameters.AddWithValue("@uident", userIdentifier ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@perm", requiredPerm ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@url", url ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ip", ip ?? (object)DBNull.Value);
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}