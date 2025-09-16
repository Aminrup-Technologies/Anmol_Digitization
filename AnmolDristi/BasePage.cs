using System;
using System.Web;
using System.Web.UI;

namespace AnmolDristi  // use your project namespace
{
    public class BasePage : System.Web.UI.Page
    {
        protected virtual bool ValidateTokenOnEveryRequest => true;

        protected int CurrentUserId =>
            Session["USERID"] != null ? Convert.ToInt32(Session["USERID"]) : 0;

        protected string CurrentEmployeeCode =>
            Session["WORKMAN"] != null ? Session["WORKMAN"].ToString() : string.Empty;

        protected string CurrentEmployeeName =>
            Session["USERNAME"] != null ? Session["USERNAME"].ToString() : string.Empty;

        protected string CurrentEmployeeFName =>
            Session["USERFNAME"] != null ? Session["USERFNAME"].ToString() : string.Empty;

        protected string CurrentLoginToken =>
            Session["LoginToken"] != null ? Session["LoginToken"].ToString() : null;

        protected string ActiveSessionId =>
            Session["ActiveSessionId"] != null ? Session["ActiveSessionId"].ToString() : null;

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            EnsureSessionValid();
        }

        protected void EnsureSessionValid()
        {
            if (Session["USERID"] == null && Session["USERNAME"] == null && Session["WORKMAN"] == null && Session["LoginToken"] == null)
            {
                RedirectToLogin();
                return;
            }

            if (ValidateTokenOnEveryRequest)
            {
                Guid token;
                if (!Guid.TryParse(CurrentLoginToken, out token) || !SessionHelper.IsSessionActive(token))
                {
                    RedirectToLogin();
                }
            }
        }

        protected void RedirectToLogin()
        {
            try { Session.Clear(); } catch { }
            try { Session.Abandon(); } catch { }

            HttpCookie cookie = new HttpCookie("ASP.NET_SessionId", "");
            cookie.Expires = DateTime.Now.AddDays(-1);
            cookie.Path = "/";
            Response.Cookies.Add(cookie);

            Response.Redirect("~/login.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
