using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AnmolDristi
{
    /// <summary>
    /// Summary description for PermDump
    /// </summary>
    public class PermDump : IHttpHandler, System.Web.SessionState.IReadOnlySessionState
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            var appPerms = PermissionCache.GetAllPermissions(System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString);
            var sessionPerms = context.Session["PH_UserPermSet"] as HashSet<string> ?? new HashSet<string>();
            var outObj = new
            {
                AppPermissions = appPerms.Values.Select(p => new { p.PermissionId, p.PermissionName, p.PermissionType, p.PagePath, p.ControlId }),
                SessionPermissions = sessionPerms.OrderBy(x => x).ToArray(),
                CurrentUserId = context.Session["CurrentUserId"]
            };
            var json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(outObj);
            context.Response.Write(json);
        }

        public bool IsReusable => false;
    }
}