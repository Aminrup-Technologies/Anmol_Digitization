using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class DebugPermissions : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            //var sb = new StringBuilder();
            //var ctx = HttpContext.Current;

            //sb.AppendLine("<h3>Session keys</h3><ul>");
            //foreach (string k in ctx.Session.Keys)
            //{
            //    try { sb.AppendFormat("<li>{0}: {1}</li>", HttpUtility.HtmlEncode(k), HttpUtility.HtmlEncode((ctx.Session[k] ?? "(null)").ToString())); }
            //    catch { sb.AppendFormat("<li>{0}: (unserializable)</li>", HttpUtility.HtmlEncode(k)); }
            //}
            //sb.AppendLine("</ul>");

            //// Resolved numeric ID via PermissionHelper (this also caches CurrentUserId)
            //int resolved = 0;
            //try { resolved = (int)typeof(PermissionHelper).GetMethod("GetCurrentUserIdFromSession", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, null); }
            //catch { /* if reflection fails, call ApplyUiPermissions to force resolution */ }

            //sb.AppendFormat("<p>Resolved numeric user id: <b>{0}</b></p>", resolved);

            //// Show cached permission set
            //object permsObj = null;
            //try { permsObj = ctx.Session["PH_UserPermSet"]; } catch { }
            //sb.AppendLine("<h3>Session Permission Set (PH_UserPermSet)</h3>");
            //if (permsObj == null) sb.AppendLine("<p>(null)</p>");
            //else
            //{
            //    sb.AppendLine("<ul>");
            //    var ie = permsObj as IEnumerable<string>;
            //    if (ie != null)
            //    {
            //        foreach (var p in ie) sb.AppendFormat("<li>{0}</li>", HttpUtility.HtmlEncode(p));
            //    }
            //    else
            //    {
            //        try { sb.AppendFormat("<li>{0}</li>", HttpUtility.HtmlEncode(permsObj.ToString())); } catch { sb.AppendFormat("<li>(unprintable)</li>"); }
            //    }
            //    sb.AppendLine("</ul>");
            //}

            //Response.Write(sb.ToString());
            //Response.End();


            var sb = new StringBuilder();
            var ctx = HttpContext.Current;

            sb.AppendLine("<h3>Session keys</h3><ul>");
            foreach (string k in ctx.Session.Keys)
            {
                try { sb.AppendFormat("<li>{0}: {1}</li>", HttpUtility.HtmlEncode(k), HttpUtility.HtmlEncode((ctx.Session[k] ?? "(null)").ToString())); }
                catch { sb.AppendFormat("<li>{0}: (unserializable)</li>", HttpUtility.HtmlEncode(k)); }
            }
            sb.AppendLine("</ul>");

            // Resolved numeric ID
            int resolvedId = 0;
            try
            {
                // prefer direct Session value if present
                if (ctx.Session["CurrentUserId"] != null)
                {
                    int tmp;
                    if (int.TryParse(ctx.Session["CurrentUserId"].ToString(), out tmp)) resolvedId = tmp;
                }
            }
            catch { }
            sb.AppendFormat("<p>Resolved numeric user id (from session): <b>{0}</b></p>", resolvedId);

            // Show PH_UserPermSet contents
            sb.AppendLine("<h3>PH_UserPermSet (session)</h3>");
            object permsObj = null;
            try { permsObj = ctx.Session["PH_UserPermSet"]; } catch { permsObj = null; }

            if (permsObj == null) sb.AppendLine("<p><i>(null)</i></p>");
            else
            {
                var ie = permsObj as IEnumerable<string>;
                if (ie != null)
                {
                    int count = 0;
                    int collCount = -1;
                    var coll = ie as ICollection<string>;
                    if (coll != null)
                        collCount = coll.Count;

                    sb.AppendLine("<p>Count: " + collCount + "</p><ul>");
                    foreach (var p in ie)
                    {
                        sb.AppendFormat("<li>{0}</li>", HttpUtility.HtmlEncode(p));
                        count++;
                    }
                    sb.AppendLine("</ul>");
                }
                else
                {
                    sb.AppendFormat("<p>SESSION object type: {0}</p><p>Value: {1}</p>",
                        HttpUtility.HtmlEncode(permsObj.GetType().FullName),
                        HttpUtility.HtmlEncode(permsObj.ToString()));
                }
            }

            // Force-load from DB using PermissionHelper (call public ApplyUiPermissions with same session user),
            // or if you want to call internal loader, try reflection (for debugging only)
            try
            {
                // Attempt to call private static LoadUserPermissionsFromDb via reflection (if it exists)
                var phType = typeof(PermissionHelper);
                var mi = phType.GetMethod("LoadUserPermissionsFromDb", BindingFlags.NonPublic | BindingFlags.Static);
                if (mi != null)
                {
                    var dbSet = mi.Invoke(null, new object[] { resolvedId }) as HashSet<string>;
                    if (dbSet != null)
                    {
                        sb.AppendLine("<h3>Permissions loaded direct from DB (reflection)</h3>");
                        sb.AppendFormat("<p>Count: {0}</p><ul>", dbSet.Count);
                        foreach (var p in dbSet) sb.AppendFormat("<li>{0}</li>", HttpUtility.HtmlEncode(p));
                        sb.AppendLine("</ul>");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendFormat("<p>Reflection call failed: {0}</p>", HttpUtility.HtmlEncode(ex.Message));
            }

            Response.Write(sb.ToString());
            Response.End();
        }
    }
}