using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

namespace AnmolDristi
{
    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            // Create a timer that calls CleanupStaleSessions every 5 minutes
            var timer = new System.Threading.Timer(_ =>
            {
                try
                {
                    SessionHelper.CleanupStaleSessions(20); // expire sessions idle > 20 minutes
                }
                catch (Exception ex)
                {
                    // optional: log error somewhere (file/db/eventlog)
                }
            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));

            // Store reference so GC doesn’t collect the timer
            Application["CleanupTimer"] = timer;
        }

        protected void Application_AcquireRequestState(object sender, EventArgs e)
        {
            HttpContext ctx = HttpContext.Current;
            if (ctx == null || ctx.Session == null)
                return;

            string path = ctx.Request.Url.AbsolutePath.ToLowerInvariant();

            // -------------------------------
            // 1. Pages that must be skipped
            // -------------------------------
            if (
                path.EndsWith("/login.aspx") ||
                path.EndsWith("/logout.aspx") ||
                path.Contains("/heartbeat.aspx") ||
                path.Contains("/webresource.axd") ||
                path.Contains("/scriptresource.axd") ||
                path.EndsWith(".css") ||
                path.EndsWith(".js") ||
                path.EndsWith(".png") ||
                path.EndsWith(".jpg") ||
                path.EndsWith(".gif") ||
                path.EndsWith(".ico")
               )
            {
                return;
            }

            // ---------------------------------
            // 2. Detect broken / partial session
            // ---------------------------------
            bool hasUser =
                ctx.Session["USERID"] != null ||
                ctx.Session["WORKMAN"] != null ||
                ctx.Session["USERNAME"] != null;

            bool hasToken = ctx.Session["LoginToken"] != null;

            // ---------------------------------
            // 3. If user exists but token missing
            //    -> session is corrupt
            // ---------------------------------
            if (hasUser && !hasToken)
            {
                try
                {
                    ctx.Session.Clear();
                    ctx.Session.Abandon();
                }
                catch { }

                // Expire ASP.NET session cookie
                HttpCookie sessionCookie = new HttpCookie("ASP.NET_SessionId", "");
                sessionCookie.Expires = DateTime.Now.AddDays(-1);
                sessionCookie.Path = "/";
                ctx.Response.Cookies.Add(sessionCookie);

                // Redirect safely
                ctx.Response.Redirect("~/login.aspx?reason=sessionexpired", true);
            }
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        //protected void Application_Error(object sender, EventArgs e)
        //{
        //    Exception ex = Server.GetLastError();
        //    if (ex != null)
        //    {
        //        // Clear the error to prevent ASP.NET from showing its own error page
        //        Server.ClearError();

        //        HttpException httpEx = ex as HttpException;
        //        string redirectUrl;

        //        if (httpEx != null)
        //        {
        //            int httpCode = httpEx.GetHttpCode();

        //            // Handle 404 errors specifically
        //            if (httpCode == 404)
        //            {
        //                redirectUrl = "~/404.aspx";
        //            }
        //            else
        //            {
        //                // General error handling
        //                redirectUrl = "~/404.aspx";
        //            }
        //        }
        //        else
        //        {
        //            // General error page for non-HTTP exceptions
        //            redirectUrl = "~/404.aspx";
        //        }

        //        // Ensure no headers or content have been sent before redirecting
        //        if (!Response.HeadersWritten)
        //        {
        //            Response.Redirect(redirectUrl, false);
        //            Context.ApplicationInstance.CompleteRequest(); // End the request pipeline gracefully
        //        }
        //    }
        //}

        protected void Application_Error(object sender, EventArgs e)
        {
            //// Retrieve the last error
            //Exception ex = Server.GetLastError();

            //if (ex != null)
            //{
            //    // Clear the error to prevent ASP.NET from showing its default error page
            //    Server.ClearError();

            //    // Send email notification for the error
            //    try
            //    {
            //        var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");

            //        // Build the error details
            //        string subject = "Application Error Notification";
            //        string body = $@"
            //            <h1>An error occurred in the application</h1>
            //            <p><strong>Message:</strong> {ex.Message}</p>
            //            <p><strong>Source:</strong> {ex.Source}</p>
            //            <p><strong>TargetSite:</strong> {ex.TargetSite}</p>
            //            <p><strong>Stack Trace:</strong></p>
            //            <pre>{ex.StackTrace}</pre>";

            //        EmailNotifier.Notify(subject, body, recipients);
            //    }
            //    catch (Exception emailEx)
            //    {
            //        // Log email notification failure to avoid masking the original error
            //        // Consider adding a fallback mechanism, e.g., logging emailEx to a file or database
            //        System.Diagnostics.Debug.WriteLine($"Failed to send error notification: {emailEx.Message}");
            //    }

            //    // Check if debugging is enabled
            //    if (HttpContext.Current.IsDebuggingEnabled)
            //    {
            //        // In debug mode, show detailed error information
            //        Response.Write("<h1>Error Details</h1>");
            //        Response.Write($"<p>{ex.Message}</p>");
            //        Response.Write($"<pre>{ex.StackTrace}</pre>");
            //        Response.End(); // Stop further processing
            //        return;
            //    }

            //    // Handle HTTP-specific exceptions
            //    HttpException httpEx = ex as HttpException;
            //    string redirectUrl;

            //    if (httpEx != null)
            //    {
            //        int httpCode = httpEx.GetHttpCode();

            //        // Handle 404 errors specifically
            //        redirectUrl = httpCode == 404 ? "~/404.aspx" : "~/Error.aspx";
            //    }
            //    else
            //    {
            //        // General error page for non-HTTP exceptions
            //        redirectUrl = "~/Error.aspx";
            //    }

            //    // Redirect to the appropriate error page
            //    if (!Response.HeadersWritten)
            //    {
            //        Response.Redirect(redirectUrl, false);
            //        Context.ApplicationInstance.CompleteRequest(); // End the request pipeline gracefully
            //    }
            //}
        }


        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {
            // Cleanly dispose timer when app shuts down
            var timer = Application["CleanupTimer"] as System.Threading.Timer;
            if (timer != null)
            {
                try
                {
                    timer.Dispose();
                }
                catch { /* ignore errors */ }
            }
        }
    }
}