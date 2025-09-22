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
    /// Lightweight UI visibility controller.
    /// Call UiPermissionController.Apply(this) from master Page_Load (or pass specific container).
    /// </summary>
    public class UiPermissionController
    {
        private const string SESSION_USERPERMSET = "PH_UserPermSet";
        private const string SESSION_CURRENT_USERID = "CurrentUserId";

        // app-wide cache of Permission.PermissionKey (loaded once)
        private static HashSet<string> _permissionKeysCache = null;
        private static readonly object _permCacheLock = new object();

        private static string ConnString
        {
            get
            {
                try { return ConfigurationManager.ConnectionStrings["AnmolDristi"].ConnectionString; }
                catch { return null; }
            }
        }

        /// <summary>
        /// Entry point. Traverses the control tree and toggles .Visible based on permissions present in session.
        /// Call from master page Page_Load: UiPermissionControllerExisting.Apply(this.sidebarMenu);
        /// </summary>
        public static void Apply(Control root)
        {
            if (root == null) return;

            // Load user's permission set from session (normalize)
            var userPerms = LoadPermissionSetFromSession();

            // Load permission keys cache from DB (if not loaded)
            EnsurePermissionKeysCache();

            // Dictionary to accumulate parent -> anyChildVisible
            var parentVisible = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            // Walk controls and set child visibility
            WalkControls(root, ctrl =>
            {
                if (string.IsNullOrEmpty(ctrl.ID)) return;

                // Build candidate permission keys (ordered)
                var candidates = GetPermissionCandidatesForControl(ctrl.ID);

                // If there are candidates, the control is permission-driven
                if (candidates != null && candidates.Any())
                {
                    bool allowed = false;
                    foreach (var cand in candidates)
                    {
                        if (string.IsNullOrEmpty(cand)) continue;
                        if (userPerms != null && userPerms.Contains(cand))
                        {
                            allowed = true;
                            break;
                        }
                    }
                    SetControlVisible(ctrl, allowed);
                }
                // else: no candidates found -> leave default visibility (do not hide)

                // Record parent visibility (naming container or parent id)
                string parentId = ResolveParentKeyForControl(ctrl);
                if (!string.IsNullOrEmpty(parentId))
                {
                    if (!parentVisible.ContainsKey(parentId)) parentVisible[parentId] = false;
                    if (IsControlVisible(ctrl)) parentVisible[parentId] = true;
                }
            });

            // Apply parent visibility
            foreach (var kv in parentVisible)
            {
                var parentCtrl = FindControlRecursive(root, kv.Key);
                if (parentCtrl != null)
                    SetControlVisible(parentCtrl, kv.Value);
            }
        }

        #region Permission candidates & heuristics

        // Generate ordered list of candidate permission keys for a control id
        private static IEnumerable<string> GetPermissionCandidatesForControl(string controlId)
        {
            if (string.IsNullOrEmpty(controlId)) yield break;

            // 1) explicit UI.<id>.View
            yield return $"UI.{controlId}.View";

            // 2) exact match to a permission key (control id equals a permission key) - uncommon but we try
            if (_permissionKeysCache != null && _permissionKeysCache.Contains(controlId))
                yield return controlId;

            // 3) heuristic: token-match Permission keys containing tokens of control id
            // tokenization: split by '_' '-' and non-letter/digit
            var tokens = SplitTokens(controlId);
            if (tokens.Length == 0) yield break;

            // Prefer candidate keys from permission keys cache that contain all tokens (in any order)
            // To reduce noise, require at least one token length >= 2 (avoid matching lots of small tokens like 'a' or 'i')
            foreach (var pk in _permissionKeysCache)
            {
                string low = pk.ToLowerInvariant();
                bool allFound = true;
                foreach (var t in tokens)
                {
                    if (string.IsNullOrEmpty(t)) { allFound = false; break; }
                    if (t.Length < 2)
                    {
                        // optional: still allow short tokens but they are weaker match
                    }
                    if (!low.Contains(t.ToLowerInvariant())) { allFound = false; break; }
                }
                if (allFound)
                {
                    yield return pk; // candidate
                }
            }
        }

        private static string[] SplitTokens(string id)
        {
            if (string.IsNullOrEmpty(id)) return new string[0];
            // replace non-alphanum with space, split
            var cleaned = new string(id.Select(ch => char.IsLetterOrDigit(ch) ? ch : ' ').ToArray());
            var toks = cleaned.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return toks;
        }

        #endregion

        #region Cache & session helpers

        private static void EnsurePermissionKeysCache()
        {
            if (_permissionKeysCache != null) return;
            lock (_permCacheLock)
            {
                if (_permissionKeysCache != null) return;
                var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                string cs = ConnString;
                if (!string.IsNullOrEmpty(cs))
                {
                    try
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
                                        if (!string.IsNullOrEmpty(pk)) set.Add(pk);
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // ignore DB errors - cache remains what we have
                    }
                }
                _permissionKeysCache = set;
            }
        }

        private static HashSet<string> LoadPermissionSetFromSession()
        {
            try
            {
                var ctx = HttpContext.Current;
                if (ctx?.Session == null) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var obj = ctx.Session[SESSION_USERPERMSET];
                if (obj == null) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var hs = obj as HashSet<string>;
                if (hs != null) return hs;

                var ie = obj as IEnumerable<string>;
                if (ie != null) return new HashSet<string>(ie, StringComparer.OrdinalIgnoreCase);

                var s = obj as string;
                if (!string.IsNullOrEmpty(s))
                {
                    var parts = s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
                    return new HashSet<string>(parts, StringComparer.OrdinalIgnoreCase);
                }
            }
            catch { }
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        #endregion

        #region Control helpers

        private static void WalkControls(Control root, Action<Control> visit)
        {
            if (root == null || visit == null) return;
            foreach (Control c in root.Controls)
            {
                visit(c);
                if (c.HasControls()) WalkControls(c, visit);
            }
        }

        public static Control FindControlRecursive(Control root, string id)
        {
            if (root == null || string.IsNullOrEmpty(id)) return null;
            if (string.Equals(root.ID, id, StringComparison.OrdinalIgnoreCase)) return root;
            foreach (Control c in root.Controls)
            {
                var found = FindControlRecursive(c, id);
                if (found != null) return found;
            }
            return null;
        }

        private static string ResolveParentKeyForControl(Control ctrl)
        {
            if (ctrl == null) return null;
            if (ctrl.NamingContainer != null && !string.IsNullOrEmpty(ctrl.NamingContainer.ID)) return ctrl.NamingContainer.ID;
            if (ctrl.Parent != null && !string.IsNullOrEmpty(ctrl.Parent.ID)) return ctrl.Parent.ID;
            return null;
        }

        private static void SetControlVisible(Control c, bool visible)
        {
            if (c == null) return;
            try
            {
                var prop = c.GetType().GetProperty("Visible");
                if (prop != null && prop.CanWrite) prop.SetValue(c, visible, null);
            }
            catch { }
        }

        private static bool IsControlVisible(Control c)
        {
            if (c == null) return false;
            try
            {
                var prop = c.GetType().GetProperty("Visible");
                if (prop != null && prop.CanRead)
                {
                    var val = prop.GetValue(c, null);
                    if (val is bool) return (bool)val;
                }
            }
            catch { }
            return false;
        }

        #endregion

        #region Debug helpers

        /// <summary>Write ID + Visible to Visual Studio Output (dev only).</summary>
        public static void DumpVisible(Control root)
        {
            WalkControls(root, (c) =>
            {
                if (!string.IsNullOrEmpty(c.ID))
                {
                    var vis = IsControlVisible(c);
                    System.Diagnostics.Debug.WriteLine($"ID={c.ID} Visible={vis}");
                }
            });
        }

        #endregion
    }
}