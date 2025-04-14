using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class home : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["USERID"] != null || Session["USERNAME"] != null || Session["WORKMAN"] != null || Session["PLANTID"] != null)
                {

                    lbl_username.Text = Session["USERNAME"].ToString();
                    lbl_empcode.Text = lbl_workmansl.Text = Session["WORKMAN"].ToString();

                    string PN_WelcomeBack_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Regular Success',
                                text: 'Welcome Back!!',
                                type: 'success',
                                styling: 'bootstrap3'
                            });
                        </script>";

                    // RegisterStartupScript adds the JavaScript code to the page
                    ClientScript.RegisterStartupScript(this.GetType(), "ShowWelcomeNotification", PN_WelcomeBack_script, false);
                }
                else
                {
                }
            }                    
        }
    }
}