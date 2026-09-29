using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;

namespace AnmolDristi
{
    /// <summary>
    /// Static helper to apply UI.*.View permissions to server controls.
    /// Designed to be called from MasterPages that do NOT inherit BasePage.
    /// Expects session keys:
    ///  - "CurrentUserId" (int) preferred
    ///  - "USERID" (string) may be numeric id as string or EmployeeCode
    ///  - "WORKMAN" (string) may be EmployeeCode like "ADMIN"
    /// </summary>
    public static class PermissionHelper
    {
        private static readonly object _permCacheLock = new object();
        private static HashSet<string> _permissionKeysCache = null;

        // Session keys used in your app
        private const string SESSION_USERPERMSET = "PH_UserPermSet";
        private const string SESSION_CURRENT_USERID = "CurrentUserId";
        private const string SESSION_USERID = "USERID";
        private const string SESSION_WORKMAN = "WORKMAN";

        private static string ConnString
        {
            get
            {
                try
                {
                    return ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Entry method for master page. Uses session to resolve current user.
        /// Example call in Dristi.master.cs Page_Load:
        ///   PermissionHelper.ApplyUiPermissions(this);
        /// or to restrict to a specific container:
        ///   PermissionHelper.ApplyUiPermissions(this.sidebarMenu);
        /// </summary>
        public static void ApplyUiPermissions(Control rootControl)
        {
            if (rootControl == null) return;

            int userId = GetCurrentUserIdFromSession();
            ApplyUiPermissions(rootControl, userId);
        }

        /// <summary>
        /// Apply UI permissions for explicit user id. Useful for tests or impersonation.
        /// </summary>
        public static void ApplyUiPermissions(Control rootControl, int userId)
        {
            if (rootControl == null) return;

            EnsurePermissionKeysCache();

            var userPermSet = GetUserPermissionSetFromSessionOrDb(userId);

            // Recursively apply
            ApplyRecursive(rootControl, userPermSet);
        }

        private static void ApplyRecursive(Control root, HashSet<string> userPermSet)
        {
            foreach (Control c in root.Controls)
            {
                if (!string.IsNullOrEmpty(c.ID))
                {
                    string permissionKey = string.Concat("UI.", c.ID, ".View");

                    // Only enforce if key exists in Permission table
                    if (_permissionKeysCache != null && _permissionKeysCache.Contains(permissionKey))
                    {
                        bool allowed = (userPermSet != null && userPermSet.Contains(permissionKey));
                        SetControlVisible(c, allowed);
                    }
                    // else: permission not registered — leave control's default visibility
                }

                if (c.HasControls())
                    ApplyRecursive(c, userPermSet);
            }
        }

        private static void SetControlVisible(Control c, bool visible)
        {
            try
            {
                var prop = c.GetType().GetProperty("Visible");
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(c, visible, null);
                }
            }
            catch
            {
                // best-effort: swallow any reflection exceptions
            }
        }

        /// <summary>
        /// Ensure the app-wide permission key cache is loaded.
        /// </summary>
        private static void EnsurePermissionKeysCache()
        {
            if (_permissionKeysCache != null) return;
            lock (_permCacheLock)
            {
                if (_permissionKeysCache != null) return;

                var temp = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    string cs = ConnString;
                    if (!string.IsNullOrEmpty(cs))
                    {
                        using (var cn = new SqlConnection(cs))
                        using (var cmd = new SqlCommand("SELECT PermissionKey FROM Permission", cn))
                        {
                            cn.Open();
                            using (var rdr = cmd.ExecuteReader())
                            {
                                while (rdr.Read())
                                {
                                    if (!rdr.IsDBNull(0))
                                    {
                                        var pk = rdr.GetString(0);
                                        if (!string.IsNullOrEmpty(pk)) temp.Add(pk);
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // If DB fails, leave temp empty (we'll treat as no registered permissions)
                }
                _permissionKeysCache = temp;
            }
        }

        /// <summary>
        /// Returns the user's permission set — tries session cache first, otherwise loads from DB.
        /// Normalizes different stored shapes (HashSet<string>, List<string>, string[] or CSV) into HashSet.
        /// </summary>
        private static HashSet<string> GetUserPermissionSetFromSessionOrDb(int userId)
        {
            var ctx = HttpContext.Current;
            if (ctx == null) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                object sessObj = null;
                try { sessObj = ctx.Session[SESSION_USERPERMSET]; } catch { sessObj = null; }

                if (sessObj != null)
                {
                    // Already HashSet<string>
                    var hash = sessObj as HashSet<string>;
                    if (hash != null) return hash;

                    // IEnumerable<string> (List<string> / string[])
                    var ie = sessObj as IEnumerable<string>;
                    if (ie != null)
                    {
                        var normalized = new HashSet<string>(ie, StringComparer.OrdinalIgnoreCase);
                        try { ctx.Session[SESSION_USERPERMSET] = normalized; } catch { }
                        return normalized;
                    }

                    // comma-separated string
                    var s = sessObj as string;
                    if (!string.IsNullOrEmpty(s))
                    {
                        var parts = s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        var norm = new HashSet<string>(parts.Select(p => p.Trim()), StringComparer.OrdinalIgnoreCase);
                        try { ctx.Session[SESSION_USERPERMSET] = norm; } catch { }
                        return norm;
                    }
                }
            }
            catch
            {
                // ignore session read issues
            }

            // Not cached: load from DB, cache and return
            var set = LoadUserPermissionsFromDb(userId) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try { if (ctx?.Session != null) ctx.Session[SESSION_USERPERMSET] = set; } catch { }
            return set;
        }

        /// <summary>
        /// Loads permission keys for the given user from DB.
        /// Uses stored proc dbo.GetUserPermissionKeys(@UserId) if available, otherwise falls back to inline query.
        /// </summary>
        private static HashSet<string> LoadUserPermissionsFromDb(int userId)
        {
            var resultSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (userId <= 0) return resultSet;

            string cs = ConnString;
            if (string.IsNullOrEmpty(cs)) return resultSet;

            // Try stored proc first
            try
            {
                using (var cn = new SqlConnection(cs))
                using (var cmd = new SqlCommand("dbo.GetUserPermissionKeys", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cn.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            if (!rdr.IsDBNull(0))
                            {
                                var key = rdr.GetString(0);
                                if (!string.IsNullOrEmpty(key)) resultSet.Add(key);
                            }
                        }
                    }
                }
                return resultSet;
            }
            catch
            {
                //fall through to inline query fallback
            }

            // Fallback inline join
            try
            {
                using (var cn = new SqlConnection(cs))
                using (var cmd = new SqlCommand(@"
                    SELECT DISTINCT p.PermissionKey
                    FROM Permission p
                    INNER JOIN RolePermission rp ON rp.PermissionId = p.PermissionId
                    INNER JOIN [Role] r ON r.RoleId = rp.RoleId
                    INNER JOIN UserRole ur ON ur.RoleId = r.RoleId
                    WHERE ur.UserId = @UserId", cn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cn.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            if (!rdr.IsDBNull(0))
                            {
                                var key = rdr.GetString(0);
                                if (!string.IsNullOrEmpty(key)) resultSet.Add(key);
                            }
                        }
                    }
                }
            }
            catch
            {
                // final fallback - empty set
            }

            return resultSet;
        }

        /// <summary>
        /// Resolves numeric MST_UserMaster.Id for current session.
        /// Uses (in order):
        ///  - Session["CurrentUserId"] (int preferred)
        ///  - Session["USERID"] (if numeric string -> id, else EmployeeCode -> lookup)
        ///  - Session["WORKMAN"] (EmployeeCode fallback)
        /// Caches resolved numeric id into Session["CurrentUserId"].
        /// </summary>
        private static int GetCurrentUserIdFromSession()
        {
            var ctx = HttpContext.Current;
            if (ctx == null) return 0;

            try
            {
                // 1) CurrentUserId fast path
                try
                {
                    if (ctx.Session != null)
                    {
                        var cur = ctx.Session[SESSION_CURRENT_USERID];
                        if (cur != null)
                        {
                            if (cur is int) return (int)cur;
                            if (cur is long) return (int)(long)cur;
                            var curS = cur as string;
                            int parsedCur;
                            if (!string.IsNullOrEmpty(curS) && int.TryParse(curS, out parsedCur)) return parsedCur;
                        }
                    }
                }
                catch { /* ignore session read issue */ }

                // 2) USERID session slot (may be "49" or "ADMIN")
                try
                {
                    if (ctx.Session != null)
                    {
                        var sid = ctx.Session[SESSION_USERID];
                        if (sid != null)
                        {
                            if (sid is int)
                            {
                                int v = (int)sid; CacheCurrentUserId(v); return v;
                            }
                            if (sid is long)
                            {
                                int v = (int)(long)sid; CacheCurrentUserId(v); return v;
                            }
                            var sVal = sid as string;
                            if (!string.IsNullOrEmpty(sVal))
                            {
                                sVal = sVal.Trim();
                                int sidNum;
                                if (int.TryParse(sVal, out sidNum))
                                {
                                    CacheCurrentUserId(sidNum); return sidNum;
                                }
                                // treat as EmployeeCode
                                int idFromCode = GetUserIdByEmployeeCode(sVal);
                                if (idFromCode > 0) { CacheCurrentUserId(idFromCode); return idFromCode; }
                            }
                        }
                    }
                }
                catch { /* ignore */ }

                // 3) WORKMAN fallback (employee code)
                try
                {
                    if (ctx.Session != null)
                    {
                        var workmanObj = ctx.Session[SESSION_WORKMAN];
                        var workman = workmanObj as string;
                        if (!string.IsNullOrEmpty(workman))
                        {
                            workman = workman.Trim();
                            int wkNum;
                            if (int.TryParse(workman, out wkNum))
                            {
                                CacheCurrentUserId(wkNum); return wkNum;
                            }
                            int idFromWorkman = GetUserIdByEmployeeCode(workman);
                            if (idFromWorkman > 0) { CacheCurrentUserId(idFromWorkman); return idFromWorkman; }
                        }
                    }
                }
                catch { /* ignore */ }
            }
            catch
            {
                // ignore top-level
            }

            return 0;
        }

        private static void CacheCurrentUserId(int id)
        {
            try
            {
                var ctx = HttpContext.Current;
                if (ctx != null && ctx.Session != null)
                {
                    ctx.Session[SESSION_CURRENT_USERID] = id;
                }
            }
            catch
            {
                // ignore session write issues
            }
        }

        /// <summary>
        /// Lookup MST_UserMaster.Id by EmployeeCode. Returns 0 on failure.
        /// </summary>
        private static int GetUserIdByEmployeeCode(string employeeCode)
        {
            if (string.IsNullOrWhiteSpace(employeeCode)) return 0;

            string cs = ConnString;
            if (string.IsNullOrEmpty(cs)) return 0;

            try
            {
                using (var cn = new SqlConnection(cs))
                using (var cmd = new SqlCommand("SELECT Id FROM MST_UserMaster WHERE LTRIM(RTRIM(EmployeeCode)) = @EmpCode", cn))
                {
                    cmd.Parameters.AddWithValue("@EmpCode", employeeCode.Trim());
                    cn.Open();
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        if (result is int) return (int)result;
                        if (result is long) return (int)(long)result;
                        int parsed;
                        if (int.TryParse(result.ToString(), out parsed)) return parsed;
                    }
                }
            }
            catch
            {
                // ignore DB errors here; return 0
            }
            return 0;
        }

        /// <summary>
        /// Remove cached per-session permission set (for current session).
        /// Call this when you change roles/permissions for current user and want reload on next request.
        /// </summary>
        public static void RefreshSessionPermissions()
        {
            try
            {
                var ctx = HttpContext.Current;
                if (ctx != null && ctx.Session != null)
                {
                    ctx.Session.Remove(SESSION_USERPERMSET);
                }
            }
            catch { /* ignore */ }
        }
    }
}
