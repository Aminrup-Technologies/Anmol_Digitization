using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AnmolDristi
{
    /// <summary>
    /// Summary description for LogoutHandler
    /// </summary>
    public class LogoutHandler : IHttpHandler, System.Web.SessionState.IRequiresSessionState
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";

            string tokenStr = context.Request["loginToken"];
            if (string.IsNullOrEmpty(tokenStr))
            {
                context.Response.Write("{\"ok\":false,\"msg\":\"no token\"}");
                return;
            }

            Guid token;
            if (!Guid.TryParse(tokenStr, out token))
            {
                context.Response.Write("{\"ok\":false,\"msg\":\"invalid token\"}");
                return;
            }

            bool ok = false;
            try
            {
                // Calls the updated wrapper
                ok = SessionHelper.EndActiveSession(token);

                // Clear server session state for this request
                try
                {
                    context.Session.Clear();
                    context.Session.Abandon();
                }
                catch { /* ignore */ }
            }
            catch (Exception ex)
            {
                context.Response.Write("{\"ok\":false,\"msg\":\"" + ex.Message.Replace("\"", "'") + "\"}");
                return;
            }

            context.Response.Write("{\"ok\":" + (ok ? "true" : "false") + "}");
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}