using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class Dristi : System.Web.UI.MasterPage
    {
        //protected void Page_Load(object sender, EventArgs e)
        //{
        //    if (!IsPostBack)
        //    {
        //        if (Session["USERID"] == null || Session["USERNAME"] == null || Session["WORKMAN"] == null)
        //        {
        //            Response.Redirect("login.aspx");
        //        }
        //        else
        //        {
        //            Label lbl1 = (Label)Page.Master.FindControl("lbl_loginusername2");
        //            lbl1.Text = Session["USERNAME"].ToString();

        //            Label lbl2 = (Label)Page.Master.FindControl("lbl_loginusername1");
        //            lbl2.Text = Session["USERFNAME"].ToString();

        //            if (Session["MODULE"].ToString() == "Quality")
        //            {
        //                if (Session["WORKMAN"].ToString() == "AHO445" || Session["WORKMAN"].ToString() == "ADMIN")
        //                {
        //                    AdminControl.Visible = true;
        //                    WebLogins.Visible = true;
        //                    WebLogins_Viewall.Visible = true;
        //                }
        //                else if (Session["WORKMAN"].ToString() == "S449")
        //                {
        //                    AdminControl.Visible = true;
        //                    WebLogins.Visible = false;
        //                    WebLogins_Viewall.Visible = false;
        //                }
        //                else
        //                {
        //                    AdminControl.Visible = false;
        //                    WebLogins.Visible = false;
        //                }
        //            }
        //            else if (Session["MODULE"].ToString() == "Vault")
        //            {
        //                if (Session["WORKMAN"].ToString() == "D006")
        //                {
        //                    ImputForms.Visible = false;
        //                    Reports.Visible = false;
        //                    AdminControl.Visible = false;
        //                    WebLogins.Visible = true;
        //                    FormsApprovals.Visible = false;
        //                    WebLogins_Viewall.Visible = true;
        //                }
        //                else
        //                {
        //                    ImputForms.Visible = false;
        //                    Reports.Visible = false;
        //                    AdminControl.Visible = false;
        //                    WebLogins.Visible = true;
        //                    FormsApprovals.Visible = false;
        //                    WebLogins_Viewall.Visible = false;
        //                }
        //            }
        //        }
        //    }
        //}

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                //// Basic auth/session check
                //if (Session["USERID"] == null || Session["USERNAME"] == null || Session["WORKMAN"] == null)
                //{
                //    Response.Redirect("login.aspx");
                //    return;
                //}

                //// Set browser sessionStorage ActiveSessionId (if present in server session)
                //if (Session["ActiveSessionId"] != null)
                //{
                //    int activeSessionId;
                //    if (int.TryParse(Session["ActiveSessionId"].ToString(), out activeSessionId))
                //    {
                //        string script = $"sessionStorage.setItem('ActiveSessionId','{activeSessionId}');";
                //        ScriptManager.RegisterStartupScript(this, this.GetType(),"setActiveSession", script, true);

                //    }
                //}

                // emit LoginToken and ActiveSessionId to sessionStorage (client)
                if (Session["LoginToken"] != null)
                {
                    string token = Session["LoginToken"].ToString();
                    //string token = CurrentLoginToken;
                    string activeId = Session["ActiveSessionId"] != null ? Session["ActiveSessionId"].ToString() : "";
                    //string activeId = ActiveSessionId;
                    // ScriptManager will wrap the script tag for us
                    string script = $"sessionStorage.setItem('ActiveLoginToken','{token}'); sessionStorage.setItem('ActiveSessionId','{activeId}');";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "setLoginToken", script, true);
                }

                // Set labels (null-safe)
                Label lbl1 = Page.Master.FindControl("lbl_loginusername2") as Label;
                if (lbl1 != null && Session["USERNAME"] != null)
                    lbl1.Text = Session["USERNAME"].ToString();

                Label lbl2 = Page.Master.FindControl("lbl_loginusername1") as Label;
                if (lbl2 != null && Session["USERFNAME"] != null)
                    lbl2.Text = Session["USERFNAME"].ToString();

                // Menu visibility logic - null-safe checks
                string module = Session["MODULE"] != null ? Session["MODULE"].ToString() : string.Empty;
                string workman = Session["WORKMAN"] != null ? Session["WORKMAN"].ToString() : string.Empty;


                // Apply UI permissions to sidebar menu (root of your menu)
                // If your sidebar markup is inside a server control with ID `sidebar-menu`,
                // use that control's reference. Example: SidebarMenuControl

                PermissionHelper.RefreshSessionPermissions();
                PermissionHelper.ApplyUiPermissions(this); // will traverse all controls on the master
                DumpControls(this); // or pass your sidebar wrapper control


                //---- Added on 17-Sep-2025------//
                //UiPermissionController.Apply(this);// apply to whole master
                //UiPermissionController.DumpVisible(this);

                // or apply only to sidebar wrapper if you have runat="server" and id="sidebarMenu":
                // UiPermissionController.Apply(this.sidebarMenu);

                // dev-only: dump visibility for menu controls

                

                // Optionally: ensure top-level menu group visibility policy:
                // HomePage.Visible = HasPermission("UI.HomePage.View");
                // ImputForms.Visible = HasPermission("UI.ImputForms.View");


                //if (module == "Quality")
                //{
                //    if (workman == "AHO445" || workman == "ADMIN")
                //    {
                //        AdminControl.Visible = true;
                //        WebLogins.Visible = true;
                //        WebLogins_Viewall.Visible = true;
                //    }
                //    else if (workman == "S449")
                //    {
                //        AdminControl.Visible = true;
                //        WebLogins.Visible = false;
                //        WebLogins_Viewall.Visible = false;
                //    }
                //    else
                //    {
                //        AdminControl.Visible = false;
                //        WebLogins.Visible = false;
                //    }
                //}
                //else if (module == "Vault")
                //{
                //    if (workman == "D006")
                //    {
                //        ImputForms.Visible = false;
                //        Reports.Visible = false;
                //        AdminControl.Visible = false;
                //        WebLogins.Visible = true;
                //        FormsApprovals.Visible = false;
                //        WebLogins_Viewall.Visible = true;
                //    }
                //    else
                //    {
                //        ImputForms.Visible = false;
                //        Reports.Visible = false;
                //        AdminControl.Visible = false;
                //        WebLogins.Visible = true;
                //        FormsApprovals.Visible = false;
                //        WebLogins_Viewall.Visible = false;
                //    }
                //}
                //else
                //{
                //    // default behavior when Module is empty/other
                //    // hide admin/weblogin controls by default to be safe
                //    AdminControl.Visible = false;
                //    WebLogins.Visible = false;
                //    WebLogins_Viewall.Visible = false;
                //}
            }
        }

        void DumpControls(Control root)
        {
            foreach (Control c in root.Controls)
            {
                if (!string.IsNullOrEmpty(c.ID))
                {
                    var visProp = c.GetType().GetProperty("Visible");
                    var value = visProp != null ? (visProp.GetValue(c, null) ?? "(null)") : "(no Visible)";
                    System.Diagnostics.Debug.WriteLine($"ID={c.ID} Visible={value}");
                }
                if (c.HasControls()) DumpControls(c);
            }
        }


        protected void btn_Logout_Click_OLD(object sender, EventArgs e)
        {
            //dbcl.WriteToFile("User :" + lbl_loginusername1.Text.ToString() + " Singout Successfully");
            //Update loginstatus and Last Login Information i.e. date
            //dbcl.UPDT_EmpMuster_LogoutInfo(Session["WORKMAN"].ToString(), Session["USERID"].ToString());

            Session.Abandon();
            Response.Redirect("login.aspx");
        }


        private void EndActiveSession(int activeSessionId, int userId)
        {
            string sql = @"UPDATE ActiveUserSession SET IsActive = 0, LogoutTime = GETDATE() WHERE ActiveSessionId = @ActiveSessionId;
            UPDATE MST_UserMaster SET LastLogoutDate = GETDATE() WHERE Id = @UserId;";

            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@ActiveSessionId", activeSessionId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // In your logout click handler:
        protected void btn_Logout_Click_OLD2(object sender, EventArgs e)
        {
            try
            {
                if (Session["ActiveSessionId"] != null && Session["USERID"] != null)
                {
                    int activeSessionId = Convert.ToInt32(Session["ActiveSessionId"]);
                    int userId = Convert.ToInt32(Session["USERID"]);
                    EndActiveSession(activeSessionId, userId);
                }
            }
            catch { /* log if needed */ }
            finally
            {
                Session.Abandon();
                Response.Redirect("login.aspx", false);
            }
        }

        protected void btn_Logout_Click_OLD3(object sender, EventArgs e)
        {
            try
            {
                if (Session["LoginToken"] != null)
                {
                    Guid token;
                    if (Guid.TryParse(Session["LoginToken"].ToString(), out token))
                    {
                        SessionHelper.EndActiveSession(token);
                    }
                }
                else if (Session["ActiveSessionId"] != null)
                {
                    int activeId;
                    if (int.TryParse(Session["ActiveSessionId"].ToString(), out activeId))
                    {
                        SessionHelper.EndActiveSession(activeId);
                    }
                }
            }
            catch (Exception ex)
            {
                // optional: log exception (your logging helper)
                // LogError(ex);
            }
            finally
            {
                // Clear server-side session & cookie
                Session.Clear();
                Session.Abandon();

                HttpCookie cookie = new HttpCookie("ASP.NET_SessionId", "");
                cookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(cookie);

                // Optionally: emit client-side script to clear sessionStorage ActiveLoginToken before redirecting.
                // But since we are redirecting to login.aspx, and the login/master page won't set the token,
                // it will remain in sessionStorage until cleared. If you want to clear it now, you can register a small page
                // that runs client script to clear token and then redirect. For simplicity we redirect directly.
                Response.Redirect("login.aspx", false);
            }
        }

        protected void btn_Logout_Click_OLD4(object sender, EventArgs e)
        {
            try
            {
                if (Session["LoginToken"] != null)
                {
                    Guid token;
                    if (Guid.TryParse(Session["LoginToken"].ToString(), out token))
                    {
                        SessionHelper.EndActiveSession(token);
                    }
                }
                else if (Session["ActiveSessionId"] != null)
                {
                    int activeId;
                    if (int.TryParse(Session["ActiveSessionId"].ToString(), out activeId))
                    {
                        SessionHelper.EndActiveSession(activeId);
                    }
                }
            }
            catch (Exception ex)
            {
                // optional: log exception (your logging helper)
                // LogError(ex);
            }
            finally
            {
                // Clear server-side session & cookie
                Session.Clear();
                Session.Abandon();

                HttpCookie cookie = new HttpCookie("ASP.NET_SessionId", "");
                cookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(cookie);

                // Emit client-side script to:
                // 1) set localStorage.app_logout so other tabs receive storage event
                // 2) clear sessionStorage ActiveLoginToken/ActiveSessionId on this tab
                // 3) redirect to login.aspx
                string clientScript = @"
                    try {
                        // notify other tabs
                        localStorage.setItem('app_logout', JSON.stringify({ ts: Date.now(), reason: 'user_logout' }));
                    } catch(e) { }

                    try {
                        sessionStorage.removeItem('ActiveLoginToken');
                        sessionStorage.removeItem('ActiveSessionId');
                    } catch(e) { }

                    // small delay to ensure storage event propagation in other tabs, then redirect
                    setTimeout(function(){ window.location.href = 'login.aspx'; }, 150);
                ";

                // Use ScriptManager if available (MasterPage should have a ScriptManager); fallback to Page.ClientScript
                if (ScriptManager.GetCurrent(this.Page) != null)
                {
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "clientLogout", clientScript, true);
                }
                else
                {
                    Page.ClientScript.RegisterStartupScript(this.GetType(), "clientLogout", "<script>" + clientScript + "</script>");
                }

                // IMPORTANT: don't call Response.Redirect here — the client script will redirect.
            }
        }

        protected void btn_Logout_Click(object sender, EventArgs e)
        {
            bool ended = false;
            try
            {
                // 1) Try token-based logout (preferred)
                if (Session["LoginToken"] != null)
                {
                    Guid token;
                    if (Guid.TryParse(Convert.ToString(Session["LoginToken"]), out token))
                    {
                        try
                        {
                            ended = SessionHelper.EndActiveSession(token);
                        }
                        catch (Exception ex)
                        {
                            // optional: log the exception for later debugging
                            // Logger.Log("EndActiveSession(token) failed: " + ex.Message);
                        }
                    }
                }

                // 2) If token path didn't work, try ActiveSessionId (and optionally userId)
                if (!ended && Session["ActiveSessionId"] != null)
                {
                    int activeId;
                    if (int.TryParse(Convert.ToString(Session["ActiveSessionId"]), out activeId))
                    {
                        try
                        {
                            // Prefer calling the stronger overload if we have USERID
                            if (Session["USERID"] != null)
                            {
                                int userId;
                                if (int.TryParse(Convert.ToString(Session["USERID"]), out userId))
                                {
                                    ended = SessionHelper.EndActiveSession(activeId, userId);
                                }
                            }

                            // fallback to plain ActiveSessionId if above didn't run or failed
                            if (!ended)
                            {
                                ended = SessionHelper.EndActiveSession(activeId);
                            }
                        }
                        catch (Exception ex)
                        {
                            // optional: log the exception
                            //Logger.Log("EndActiveSession(activeId) failed: " + ex.Message);
                        }
                    }
                }

                // optional: record audit/log that logout was attempted and whether it succeeded
                // Logger.Log($"Logout attempted. tokenPresent={(Session[\"LoginToken\"]!=null)}, activeIdPresent={(Session[\"ActiveSessionId\"]!=null)}, ended={ended}");
            }
            catch (Exception ex)
            {
                // optional: log unexpected exceptions
                // Logger.Log("Logout handler exception: " + ex.Message);
            }
            finally
            {
                // 3) Clear server session & cookie
                try { Session.Clear(); } catch { }
                try { Session.Abandon(); } catch { }

                HttpCookie cookie = new HttpCookie("ASP.NET_SessionId", "");
                cookie.Expires = DateTime.Now.AddDays(-1);
                cookie.Path = "/"; // ensure path matches app root
                Response.Cookies.Add(cookie);

                // 4) Emit client-side script to notify other tabs and redirect
                string clientScript = @"
            try { localStorage.setItem('app_logout', JSON.stringify({ ts: Date.now(), reason: 'user_logout' })); } catch(e) {}
            try { sessionStorage.removeItem('ActiveLoginToken'); sessionStorage.removeItem('ActiveSessionId'); } catch(e) {}
            setTimeout(function(){ window.location.href = 'login.aspx'; }, 150);";

                if (ScriptManager.GetCurrent(this.Page) != null)
                {
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "clientLogout", clientScript, true);
                }
                else
                {
                    Page.ClientScript.RegisterStartupScript(this.GetType(), "clientLogout", clientScript, true);
                }

                // do not call Response.Redirect here — let client script handle the redirect after it runs
            }
        }


        [WebMethod]
        public static void Heartbeat(int activeSessionId)
        {
            string sql = "UPDATE ActiveUserSession SET LastHeartbeat = GETDATE() WHERE ActiveSessionId = @id";
            // run update using ADO.NET
        }

    }
}