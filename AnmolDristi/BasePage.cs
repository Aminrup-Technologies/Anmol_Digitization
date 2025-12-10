using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.UI;

namespace AnmolDristi  // use your project namespace
{
    
    public class BasePage : System.Web.UI.Page
    {
        private readonly string _connString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        private const string SessionPermSetKey = "PH_UserPermSet"; // already in use

        protected virtual bool ValidateTokenOnEveryRequest => true;

        //protected int CurrentUserId =>
        //    Session["USERID"] != null ? Convert.ToInt32(Session["USERID"]) : 0;

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

        private const int HEARTBEAT_SEC = 30;
        private const string LAST_BEAT_KEY = "__LastHeartUtc";

        protected void EnsureSessionValid()
        {
            if (Session["USERID"] == null && Session["USERNAME"] == null && Session["WORKMAN"] == null && Session["LoginToken"] == null)
            {
                //RedirectToLogin();
                return;
            }

            if (!ValidateTokenOnEveryRequest) return;

            Guid token;
            if (!Guid.TryParse(CurrentLoginToken, out token))
            {
                //RedirectToLogin();
                return;
            }

            try
            {
                DateTime last = DateTime.MinValue;
                if (Session[LAST_BEAT_KEY] != null) DateTime.TryParse(Session[LAST_BEAT_KEY].ToString(), out last);

                if ((DateTime.UtcNow - last).TotalSeconds >= HEARTBEAT_SEC)
                {
                    bool updated = SessionHelper.UpdateHeartbeat(token);
                    if (updated) Session[LAST_BEAT_KEY] = DateTime.UtcNow.ToString("o");
                    else
                    {
                        // If DB says session not active then force logout
                        if (!SessionHelper.IsSessionActive(token)) RedirectToLogin();
                    }
                }
                else
                {
                    // we refreshed recently; optionally do nothing else
                    // if you need stronger validation uncomment:
                    // if (!SessionHelper.IsSessionActive(token)) RedirectToLogin();
                }
            }
            catch
            {
                // On DB error, safer to redirect to login (reduce partial auth states)
                RedirectToLogin();
            }
        }
        protected void EnsureSessionValid_09Dec25()
        {
            if (Session["USERID"] == null && Session["USERNAME"] == null && Session["WORKMAN"] == null && Session["LoginToken"] == null)
            {
                //RedirectToLogin();
                return;
            }

            if (ValidateTokenOnEveryRequest)
            {
                Guid token;
                if (!Guid.TryParse(CurrentLoginToken, out token) || !SessionHelper.IsSessionActive(token))
                {
                    //RedirectToLogin();
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


        //-------------------------------Access Controls-----------------------//
        // Current user id (should be set at login into Session)
        protected int CurrentUserId_OLD1
        {
            get
            {
                if (Session["USERID"] != null) return (int)Session["USERID"];
                // fallback - throw or handle anon
                throw new InvalidOperationException("CurrentUserId not set in Session. Ensure login code sets it.");
            }
        }

        public int CurrentUserId
        {
            get
            {
                int id;
                if (TryGetCurrentUserId(out id))
                    return id;

                throw new InvalidOperationException("CurrentUserId not set in Session. Ensure login code sets it.");
            }
        }



        // Load permission keys into Session if not already present (HashSet for fast lookup)
        protected HashSet<string> UserPermissionSet
        {
            get
            {
                if (Session["UserPermissionSet"] == null)
                {
                    var set = LoadPermissionsFromDb(CurrentUserId);
                    Session["UserPermissionSet"] = set;
                }
                return (HashSet<string>)Session["UserPermissionSet"];
            }
        }

        // Call to refresh, e.g., after role change
        protected void RefreshUserPermissions()
        {
            Session.Remove("UserPermissionSet");
            var _ = UserPermissionSet; // reload
        }

        private HashSet<string> LoadPermissionsFromDb(int userId)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var cn = new SqlConnection(_connString))
            using (var cmd = new SqlCommand("dbo.GetUserPermissionKeys", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);
                cn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        var key = rdr.GetString(0);
                        if (!string.IsNullOrEmpty(key)) set.Add(key);
                    }
                }
            }
            return set;
        }

        protected bool HasPermission(string permissionKey)
        {
            if (string.IsNullOrWhiteSpace(permissionKey)) return false;
            var set = UserPermissionSet;
            return set.Contains(permissionKey);
        }

        // Optional: helper to check module-level entitlement from MST_UserMaster.Module (if you want)
        protected string UserModule
        {
            get
            {
                if (Session["UserModule"] == null)
                {
                    // you can fetch from DB here and set Session["UserModule"]
                    // minimal example omitted — fetch from MST_UserMaster where Id = CurrentUserId
                }
                return Session["UserModule"] as string;
            }
        }

        // Utility: recursively apply permission rules to controls
        protected void ApplyUiPermissions_OLD(Control rootControl)
        {
            if (rootControl == null) return;
            foreach (Control c in rootControl.Controls)
            {
                // Only server controls with ID matter
                if (!string.IsNullOrEmpty(c.ID))
                {
                    string permKey = $"UI.{c.ID}.View";
                    if (PermissionKeyExistsInSystem(permKey))
                    {
                        // If permission defined in system, set visibility based on permission
                        var visible = HasPermission(permKey);
                        SetControlVisible(c, visible);
                    }
                    // else: if no permission created for this control, leave default visibility (or decide policy)
                }

                // Recurse
                if (c.HasControls())
                    ApplyUiPermissions(c);
            }
        }


        /// <summary>
        /// Make this protected virtual so derived pages and master pages can call/override it.
        /// </summary>
        protected virtual void ApplyUiPermissions(Control rootControl)
        {
            if (rootControl == null) return;
            foreach (Control c in rootControl.Controls)
            {
                if (!string.IsNullOrEmpty(c.ID))
                {
                    string permKey = $"UI.{c.ID}.View";
                    if (PermissionKeyExistsInSystem(permKey))
                    {
                        bool visible = HasPermission(permKey);
                        SetControlVisible(c, visible);
                    }
                }

                if (c.HasControls())
                    ApplyUiPermissions(c);
            }
        }

        // Helper: set Visible property if present (for HtmlControls / WebControls)
        private void SetControlVisible_OLD(Control c, bool visible)
        {
            // Many controls expose .Visible
            var prop = c.GetType().GetProperty("Visible");
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(c, visible, null);
            }
        }

        // Keep SetControlVisible protected as well
        protected void SetControlVisible(Control c, bool visible)
        {
            var prop = c.GetType().GetProperty("Visible");
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(c, visible, null);
            }
        }

        // Check if permission exists in Permission table — small cache to avoid DB hits on every control
        private static HashSet<string> _permissionKeysCache = null;
        private bool PermissionKeyExistsInSystem(string key)
        {
            if (_permissionKeysCache == null)
            {
                _permissionKeysCache = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var cn = new SqlConnection(_connString))
                using (var cmd = new SqlCommand("SELECT PermissionKey FROM Permission", cn))
                {
                    cn.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            _permissionKeysCache.Add(rdr.GetString(0));
                        }
                    }
                }
            }
            return _permissionKeysCache.Contains(key);
        }

        protected void RequirePermission_OLD1(string permissionName)
        {
            if (string.IsNullOrEmpty(permissionName)) return; // if page uses none

            var userPermSet = Session[SessionPermSetKey] as HashSet<string>;
            if (userPermSet == null)
            {
                // Rebuild from DB (fallback) if session expired or not built
                RebuildSessionPermissions();
                userPermSet = Session[SessionPermSetKey] as HashSet<string> ?? new HashSet<string>();
            }

            if (!userPermSet.Contains(permissionName))
            {
                // 403 behavior - you can redirect to access denied page
                Response.StatusCode = 403;
                Response.Redirect("~/AccessDenied.aspx?perm=" + HttpUtility.UrlEncode(permissionName));
                Response.End();
            }
        }

        protected void RequirePermission(string permissionName)
        {
            if (string.IsNullOrEmpty(permissionName)) return;

            var userPermSet = Session[SessionPermSetKey] as HashSet<string>;
            if (userPermSet == null)
            {
                RebuildSessionPermissions();
                userPermSet = Session[SessionPermSetKey] as HashSet<string> ?? new HashSet<string>();
            }

            if (!userPermSet.Contains(permissionName))
            {
                // Send user to AccessDenied.aspx
                Response.StatusCode = 403;

                // Option 1: Redirect (URL changes, new request cycle)
                Response.Redirect("~/AccessDenied.aspx?perm=" + HttpUtility.UrlEncode(permissionName), endResponse: true);

                // Option 2: Transfer (keeps original URL in address bar)
                // Server.Transfer("~/AccessDenied.aspx?perm=" + HttpUtility.UrlEncode(permissionName));

                // After redirect/transfer, stop execution
                Context.ApplicationInstance.CompleteRequest();
            }
        }


        protected void RebuildSessionPermissions()
        {
            // Use CurrentUserId session key present in your app
            if (Session["CurrentUserId"] == null) return;
            int userId = Convert.ToInt32(Session["CurrentUserId"]);
            var conn = System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            var service = new PermissionService(conn);
            var perms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // fetch role ids for user
            using (var cn = new System.Data.SqlClient.SqlConnection(conn))
            using (var cmd = new System.Data.SqlClient.SqlCommand(@"
            SELECT p.PermissionName FROM dbo.UserRole ur
            JOIN dbo.RolePermission rp ON ur.RoleId = rp.RoleId
            JOIN dbo.Permission p ON rp.PermissionId = p.PermissionId
            WHERE ur.UserId = @u", cn))
            {
                cmd.Parameters.AddWithValue("@u", userId);
                cn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read()) perms.Add(rdr.GetString(0));
                }
            }
            Session[SessionPermSetKey] = perms;
        }

        // Add to BasePage.cs
        protected bool TryGetCurrentUserId(out int userId)
        {
            userId = 0;
            try
            {
                // try numeric stored session key first (your app stores numeric id in "CurrentUserId")
                if (Session["CurrentUserId"] != null)
                {
                    if (int.TryParse(Session["CurrentUserId"].ToString(), out userId)) return true;
                }

                // fallback: some sessions store USERID string / employee code; try to map it if needed
                if (Session["USERID"] != null)
                {
                    // if USERID is numeric string, accept it; otherwise return false
                    if (int.TryParse(Session["USERID"].ToString(), out userId)) return true;
                    // otherwise, you might want to map employee code -> userId using DB (optional)
                }

                return false;
            }
            catch
            {
                userId = 0;
                return false;
            }
        }

    }
}
