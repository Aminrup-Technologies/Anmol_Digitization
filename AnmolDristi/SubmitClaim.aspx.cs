using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class SubmitClaim : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // enforce permission – prevents direct URL access
            RequirePermission("UI.SO_TaDa_Module.View");
            RequirePermission("UI.SO_TaDa_Submit.View");

            if (!IsPostBack)
            {
                // init page
            }
        }
    }
}