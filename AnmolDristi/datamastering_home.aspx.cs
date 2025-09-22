using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class datamastering_home : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // enforce permission – prevents direct URL access
            RequirePermission("UI.AdminControl.View");
            RequirePermission("UI.DataMST.View");

            if (!IsPostBack)
            {
                // init page
            }

        }
    }
}