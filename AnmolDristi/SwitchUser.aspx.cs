using System;
using System.Data;
using System.Web.UI;

namespace AnmolDristi
{
    public partial class SwitchUser : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Quick-return: banner "Return to My Account" link passes ?action=return
            // Bypass all authorization gates — only needs an active impersonation session.
            if (string.Equals(Request.QueryString["action"], "return", StringComparison.OrdinalIgnoreCase))
            {
                if (SwitchUserHelper.IsCurrentlyImpersonating(Session))
                {
                    string err;
                    SwitchUserHelper.PerformReturn(Session, out err);
                }
                Response.Redirect("~/home.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (!IsPostBack)
            {
                // Gate: feature must be enabled
                if (!SwitchUserHelper.IsFeatureEnabled())
                {
                    Response.Redirect("~/home.aspx", false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }

                // Gate: user must be logged in
                if (Session["WORKMAN"] == null)
                {
                    Response.Redirect("~/login.aspx", false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }

                string callerCode = Session["WORKMAN"].ToString();

                // Gate: caller must be in the allowlist
                if (!SwitchUserHelper.IsEmployeeAuthorized(callerCode))
                {
                    pnlNoAccess.Visible = true;
                    pnlMain.Visible = false;
                    pnlImpersonationBanner.Visible = false;
                    return;
                }

                pnlNoAccess.Visible = false;
                pnlMain.Visible = true;

                // Show impersonation banner if currently impersonating
                RefreshImpersonationBanner();
            }
        }

        #region Banner

        private void RefreshImpersonationBanner()
        {
            if (SwitchUserHelper.IsCurrentlyImpersonating(Session))
            {
                pnlImpersonationBanner.Visible = true;

                string currentName = Session["USERNAME"]?.ToString() ?? "Unknown";
                string currentCode = Session["WORKMAN"]?.ToString() ?? "Unknown";
                lblImpersonatingUser.Text = currentName + " (" + currentCode + ")";

                string originalName = Session["Original_USERNAME"]?.ToString() ?? "Unknown";
                string originalCode = Session["Original_WORKMAN"]?.ToString() ?? "Unknown";
                lblOriginalUser.Text = originalName + " (" + originalCode + ")";
            }
            else
            {
                pnlImpersonationBanner.Visible = false;
            }
        }

        #endregion

        #region Search

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            string searchTerm = txtSearch.Text.Trim();
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                ShowMessage("Please enter an Employee Code or Name to search.", "warning");
                pnlResults.Visible = false;
                return;
            }

            DataTable results = SwitchUserHelper.SearchEmployees(searchTerm);
            if (results == null || results.Rows.Count == 0)
            {
                pnlResults.Visible = true;
                pnlNoResults.Visible = true;
                rptEmployees.Visible = false;
                lblResultCount.Text = "(0)";
                ShowMessage("No active employees found matching '" + System.Net.WebUtility.HtmlEncode(searchTerm) + "'.", "warning");
                return;
            }

            // Remove the current user from results if impersonating (prevent self-switch during impersonation)
            // Actually: allow showing all results, the PerformSwitch method will block self-switch

            rptEmployees.DataSource = results;
            rptEmployees.DataBind();
            rptEmployees.Visible = true;
            pnlNoResults.Visible = false;
            pnlResults.Visible = true;
            lblResultCount.Text = "(" + results.Rows.Count + ")";
            HideMessage();
        }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            pnlResults.Visible = false;
            HideMessage();
            txtSearch.Focus();
        }

        #endregion

        #region Switch

        protected void rptEmployees_ItemCommand(object source, System.Web.UI.WebControls.RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "SwitchTo")
            {
                string targetCode = e.CommandArgument?.ToString();
                if (string.IsNullOrWhiteSpace(targetCode))
                {
                    ShowMessage("Invalid target employee.", "danger");
                    return;
                }

                string errorMessage;
                bool success = SwitchUserHelper.PerformSwitch(Session, targetCode, out errorMessage);

                if (success)
                {
                    // Rebuild PH_UserPermSet for the target, then re-apply master sidebar
                    // on this postback (Master Page_Load already ran before PerformSwitch).
                    RefreshUserPermissions();
                    Dristi master = Master as Dristi;
                    if (master != null)
                        master.RefreshChromeAfterIdentityChange();

                    string targetName = Session["USERNAME"]?.ToString() ?? targetCode;
                    ShowMessage("Successfully switched to " + System.Net.WebUtility.HtmlEncode(targetName) + " (" + System.Net.WebUtility.HtmlEncode(targetCode) + "). You are now operating as this user.", "success");
                    RefreshImpersonationBanner();
                }
                else
                {
                    ShowMessage(errorMessage, "danger");
                }
            }
        }

        #endregion

        #region Return

        protected void btnReturnToOriginal_Click(object sender, EventArgs e)
        {
            string errorMessage;
            bool success = SwitchUserHelper.PerformReturn(Session, out errorMessage);

            if (success)
            {
                // Redirect to home as the original user
                Response.Redirect("~/home.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
            }
            else
            {
                ShowMessage(errorMessage, "danger");
                RefreshImpersonationBanner();
            }
        }

        #endregion

        #region Message Helpers

        private void ShowMessage(string text, string type)
        {
            pnlMessage.Visible = true;
            lblMessage.Text = text;

            string cssClass = "su-message ";
            switch (type)
            {
                case "danger": cssClass += "danger"; break;
                case "success": cssClass += "success"; break;
                case "warning": cssClass += "warning"; break;
                default: cssClass += "warning"; break;
            }
            divMessage.Attributes["class"] = cssClass;
        }

        private void HideMessage()
        {
            pnlMessage.Visible = false;
            lblMessage.Text = string.Empty;
        }

        #endregion
    }
}
