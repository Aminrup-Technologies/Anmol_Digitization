// ========================================================================
// When:    2026-08-25
// What:    Master Data Management landing page. Enforces ITSM.MasterData.Manage
//          at page level. Navigation visibility is separately gated by
//          UI.ITSM_MasterData.View via PermissionHelper in Dristi.Master.
// Why:     ITSM Phase 1B — MDM authorization foundation. Page-level
//          authorization must not rely solely on navigation visibility.
// ========================================================================

using System;
using System.Web.UI;

namespace AnmolDristi.ITSM.UI
{
    public partial class MasterData : BasePage
    {
        private const string ManagePermission = "ITSM.MasterData.Manage";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["USERID"] == null || Session["BRANCH"] == null)
            {
                RedirectToLogin();
                return;
            }

            RequirePermission(ManagePermission);
        }
    }
}
