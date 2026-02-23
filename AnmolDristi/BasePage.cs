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
        private const string SessionPermSetKey = "PH_UserPermSet";

        protected virtual bool ValidateTokenOnEveryRequest => true;

        protected string CurrentEmployeeCode => Session["WORKMAN"] != null ? Session["WORKMAN"].ToString() : string.Empty;
        protected string CurrentEmployeeName => Session["USERNAME"] != null ? Session["USERNAME"].ToString() : string.Empty;
        protected string CurrentEmployeeFName => Session["USERFNAME"] != null ? Session["USERFNAME"].ToString() : string.Empty;
        protected string CurrentLoginToken => Session["LoginToken"] != null ? Session["LoginToken"].ToString() : null;
        protected string ActiveSessionId => Session["ActiveSessionId"] != null ? Session["ActiveSessionId"].ToString() : null;

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            EnsureSessionValid();
        }

        //------------------------------- Session & Heartbeat Flow -----------------------//
        private const int HEARTBEAT_SEC = 60;
        private const int LOGOUT_GRACE_MIN = 5;
        private const string LAST_BEAT_KEY = "__LastHeartUtc";
        private const string LAST_OK_KEY = "__LastSessionOkUtc";

        protected void EnsureSessionValid()
        {
            if (Session["LoginToken"] == null) return;

            Guid token;
            if (!Guid.TryParse(CurrentLoginToken, out token)) return;

            try
            {
                var now = DateTime.UtcNow;

                // Heartbeat update
                DateTime lastBeat = DateTime.MinValue;
                if (Session[LAST_BEAT_KEY] != null)
                    DateTime.TryParse(Session[LAST_BEAT_KEY].ToString(), out lastBeat);

                if ((now - lastBeat).TotalSeconds >= HEARTBEAT_SEC)
                {
                    if (SessionHelper.UpdateHeartbeat(token))
                    {
                        Session[LAST_BEAT_KEY] = now.ToString("o");
                        Session[LAST_OK_KEY] = now.ToString("o");
                    }
                }

                // Soft validation
                DateTime lastOk = DateTime.MinValue;
                if (Session[LAST_OK_KEY] != null)
                    DateTime.TryParse(Session[LAST_OK_KEY].ToString(), out lastOk);

                if ((now - lastOk).TotalMinutes >= LOGOUT_GRACE_MIN)
                {
                    if (!SessionHelper.IsSessionActive(token))
                    {
                        RedirectToLogin();
                    }
                    else
                    {
                        Session[LAST_OK_KEY] = now.ToString("o");
                    }
                }
            }
            catch
            {
                // NEVER logout on transient failures
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

        //------------------------------- Access Controls -----------------------//

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

        protected bool TryGetCurrentUserId(out int userId)
        {
            userId = 0;
            try
            {
                if (Session["CurrentUserId"] != null && int.TryParse(Session["CurrentUserId"].ToString(), out userId))
                    return true;

                if (Session["USERID"] != null && int.TryParse(Session["USERID"].ToString(), out userId))
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
        }

        // Single Source of Truth for Permissions
        protected HashSet<string> UserPermissionSet
        {
            get
            {
                var userPermSet = Session[SessionPermSetKey] as HashSet<string>;
                if (userPermSet == null)
                {
                    RebuildSessionPermissions();
                    userPermSet = Session[SessionPermSetKey] as HashSet<string> ?? new HashSet<string>();
                }
                return userPermSet;
            }
        }

        // Call to refresh, e.g., after role change
        protected void RefreshUserPermissions()
        {
            Session.Remove(SessionPermSetKey);
            var _ = UserPermissionSet; // reload
        }

        protected void RebuildSessionPermissions()
        {
            int userId;
            if (!TryGetCurrentUserId(out userId)) return;

            var perms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (var cn = new SqlConnection(_connString))
            using (var cmd = new SqlCommand(@"
                SELECT p.PermissionKey FROM dbo.UserRole ur
                JOIN dbo.RolePermission rp ON ur.RoleId = rp.RoleId
                JOIN dbo.Permission p ON rp.PermissionId = p.PermissionId
                WHERE ur.UserId = @u AND p.IsObsolete = 0", cn))
            {
                cmd.Parameters.AddWithValue("@u", userId);
                cn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    // FIXED: We are now grabbing PermissionKey instead of PermissionName
                    while (rdr.Read())
                    {
                        var key = rdr.GetString(0);
                        if (!string.IsNullOrEmpty(key)) perms.Add(key);
                    }
                }
            }
            Session[SessionPermSetKey] = perms;
        }

        protected bool HasPermission(string permissionKey)
        {
            if (string.IsNullOrWhiteSpace(permissionKey)) return false;
            return UserPermissionSet.Contains(permissionKey);
        }

        protected void RequirePermission(string permissionKey)
        {
            if (string.IsNullOrEmpty(permissionKey)) return;

            if (!UserPermissionSet.Contains(permissionKey))
            {
                Response.StatusCode = 403;
                // Redirect with false is safer when followed by CompleteRequest
                Response.Redirect("~/AccessDenied.aspx?perm=" + HttpUtility.UrlEncode(permissionKey), false);
                Context.ApplicationInstance.CompleteRequest();
            }
        }

        //------------------------------- UI Permissions (Show/Hide Controls) -----------------------//

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

        protected void SetControlVisible(Control c, bool visible)
        {
            var prop = c.GetType().GetProperty("Visible");
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(c, visible, null);
            }
        }

        private static HashSet<string> _permissionKeysCache = null;
        private bool PermissionKeyExistsInSystem(string key)
        {
            if (_permissionKeysCache == null)
            {
                _permissionKeysCache = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var cn = new SqlConnection(_connString))
                using (var cmd = new SqlCommand("SELECT PermissionKey FROM dbo.Permission WHERE IsObsolete = 0", cn))
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
    }
}