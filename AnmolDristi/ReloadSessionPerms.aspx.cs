using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class ReloadSessionPerms : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            //if (!HasPermission("UI.Admin.Roles"))
            //{
            //    Response.StatusCode = 403;
            //    Response.Write("Access denied.");
            //    Response.End();
            //    return;
            //}

            Response.ContentType = "text/plain";

            if (Session["CurrentUserId"] != null)
            {
                RebuildSessionPermissions(); // comes from BasePage
                Response.Write("Rebuilt session permissions for userId=" + Session["CurrentUserId"]);
            }
            else
            {
                Response.Write("No CurrentUserId in session.");
            }

            Response.End();
        }
    }
}