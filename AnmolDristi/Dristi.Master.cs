using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class Dristi : System.Web.UI.MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["USERID"] == null || Session["USERNAME"] == null || Session["WORKMAN"] == null)
                {
                    Response.Redirect("login.aspx");
                }
                else
                {
                    // --- Switch User: show menu link + impersonation banner ---
                    SetupSwitchUserBannerAndMenu();
                    Label lbl1 = (Label)Page.Master.FindControl("lbl_loginusername2");
                    lbl1.Text = Session["USERNAME"].ToString();

                    Label lbl2 = (Label)Page.Master.FindControl("lbl_loginusername1");
                    lbl2.Text = Session["USERFNAME"].ToString();

                    if (Session["WORKMAN"].ToString() == "AHO445" || Session["WORKMAN"].ToString() == "ADMIN")
                    {
                        AdminControl.Visible = true;
                        WebLogins.Visible = true;
                        WebLogins_Viewall.Visible = true;
                    }
                    else if (Session["WORKMAN"].ToString() == "D006")
                    {
                        AdminControl.Visible = false;
                        WebLogins.Visible = true;
                        FormsApprovals.Visible = false;
                        WebLogins_Viewall.Visible = true;
                    }
                    else
                    {
                        AdminControl.Visible = false;
                        WebLogins.Visible = false;
                    }
                }
            }
        }

        protected void btn_Logout_Click(object sender, EventArgs e)
        {
            // --- If impersonating, return to original user first (preserve original session on logout) ---
            if (SwitchUserHelper.IsCurrentlyImpersonating(Session))
            {
                SwitchUserHelper.ReturnToOriginalSession(Session);
            }

            Session.Abandon();
            Response.Redirect("login.aspx");
        }

        /// <summary>
        /// Controls the impersonation banner and the Switch User sidebar link.
        /// </summary>
        private void SetupSwitchUserBannerAndMenu()
        {
            try
            {
                // --- Impersonation Banner ---
                if (SwitchUserHelper.IsCurrentlyImpersonating(Session))
                {
                    pnlSuBanner.Visible = true;
                    string currentName = Session["USERNAME"]?.ToString() ?? "Unknown";
                    string currentCode = Session["WORKMAN"]?.ToString() ?? "Unknown";
                    string originalName = Session["Original_USERNAME"]?.ToString() ?? "Unknown";
                    string originalCode = Session["Original_WORKMAN"]?.ToString() ?? "Unknown";
                    lblSuBannerCurrent.Text = currentName + " (" + currentCode + ")";
                    lblSuBannerOriginal.Text = originalName + " (" + originalCode + ")";
                }
                else
                {
                    pnlSuBanner.Visible = false;
                }

                // --- Switch User sidebar link ---
                // Only show to authorized users when feature is enabled
                if (SwitchUserHelper.IsFeatureEnabled())
                {
                    string workman = Session["WORKMAN"]?.ToString();
                    SwitchUserLink.Visible = !string.IsNullOrEmpty(workman) && SwitchUserHelper.IsEmployeeAuthorized(workman);
                }
                else
                {
                    SwitchUserLink.Visible = false;
                }
            }
            catch
            {
                // Fail-closed: hide everything on error
                try { pnlSuBanner.Visible = false; } catch { }
                try { SwitchUserLink.Visible = false; } catch { }
            }
        }
    }
}