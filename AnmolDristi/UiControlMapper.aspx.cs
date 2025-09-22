using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using System.Text.RegularExpressions;

namespace AnmolDristi
{
    public partial class UiControlMapper : System.Web.UI.Page
    {
        //string Conn => System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        string Conn
        {
            get
            {
                var cs = System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"];
                if (cs != null) return cs.ConnectionString;
                cs = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"];
                if (cs != null) return cs.ConnectionString;
                throw new InvalidOperationException("No connection string 'DbConn' or 'DefaultConnection' found in web.config");
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadPageList();
                LoadPermissions();
            }
        }

        private static readonly Regex RunatIdRegex = new Regex(@"(?i)(?:id\s*=\s*['""](?<id>[^'""]+)['""]\s*[^>]*runat\s*=\s*['""]server['""])|(?:runat\s*=\s*['""]server['""]\s*[^>]*id\s*=\s*['""](?<id2>[^'""]+)['""])", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private void LoadControlsForPage(string pageVirtualPath)
        {
            lbControls.Items.Clear();
            if (string.IsNullOrWhiteSpace(pageVirtualPath)) return;
            if (!pageVirtualPath.StartsWith("~/")) pageVirtualPath = "~/" + pageVirtualPath.TrimStart('/');
            var physical = Server.MapPath(pageVirtualPath);
            if (!File.Exists(physical)) return;

            var text = File.ReadAllText(physical);
            var matches = RunatIdRegex.Matches(text);
            var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match m in matches)
            {
                var id = m.Groups["id"].Success ? m.Groups["id"].Value : m.Groups["id2"].Value;
                if (string.IsNullOrWhiteSpace(id)) continue;
                if (!added.Contains(id))
                {
                    lbControls.Items.Add(new ListItem(id));
                    added.Add(id);
                }
            }

            // pre-select first, and also try to show mapped permission (see next snippet)
            if (lbControls.Items.Count > 0) lbControls.SelectedIndex = 0;
            ShowSelectedControlMapping(pageVirtualPath, lbControls.SelectedValue);
        }

        private void ShowSelectedControlMapping_OLD(string page, string controlId)
        {
            if (string.IsNullOrEmpty(page) || string.IsNullOrEmpty(controlId)) return;

            using (var cn = new System.Data.SqlClient.SqlConnection(Conn))
            using (var cmd = new System.Data.SqlClient.SqlCommand(
                "SELECT PermissionId, PermissionKey, PermissionName FROM dbo.Permission WHERE PagePath = @page AND ControlId = @ctrl AND IsObsolete = 0", cn))
            {
                cmd.Parameters.AddWithValue("@page", page);
                cmd.Parameters.AddWithValue("@ctrl", controlId);
                cn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        int pid = rdr.IsDBNull(0) ? 0 : rdr.GetInt32(0);
                        string key = rdr.IsDBNull(1) ? null : rdr.GetString(1);
                        string name = rdr.IsDBNull(2) ? null : rdr.GetString(2);
                        lblMessage.Text = $"Mapped to: {HttpUtility.HtmlEncode(key ?? name ?? pid.ToString())}";
                        // select in ddlPermissions
                        if (pid > 0)
                        {
                            var li = ddlPermissions.Items.FindByValue(pid.ToString());
                            if (li != null) ddlPermissions.ClearSelection(); li.Selected = true;
                        }
                    }
                    else
                    {
                        lblMessage.Text = "No permission mapped for this control.";
                        ddlPermissions.ClearSelection();
                    }
                }
            }
        }

        private void ShowSelectedControlMapping(string page, string controlId)
        {
            if (string.IsNullOrEmpty(page) || string.IsNullOrEmpty(controlId)) return;

            using (var cn = new System.Data.SqlClient.SqlConnection(Conn))
            using (var cmd = new System.Data.SqlClient.SqlCommand(
                "SELECT PermissionId, PermissionKey, PermissionName FROM dbo.Permission WHERE PagePath = @page AND ControlId = @ctrl AND IsObsolete = 0", cn))
            {
                cmd.Parameters.AddWithValue("@page", page);
                cmd.Parameters.AddWithValue("@ctrl", controlId);
                cn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        int pid = rdr.IsDBNull(0) ? 0 : rdr.GetInt32(0);
                        string key = rdr.IsDBNull(1) ? null : rdr.GetString(1);
                        string name = rdr.IsDBNull(2) ? null : rdr.GetString(2);
                        lblMessage.Text = $"Mapped to: {HttpUtility.HtmlEncode(key ?? name ?? pid.ToString())}";
                        // select in ddlPermissions
                        if (pid > 0)
                        {
                            var li = ddlPermissions.Items.FindByValue(pid.ToString());
                            if (li != null) ddlPermissions.ClearSelection(); li.Selected = true;
                        }
                    }
                    else
                    {
                        lblMessage.Text = "No permission mapped for this control.";
                        ddlPermissions.ClearSelection();
                    }
                }
            }
        }


        //------------------------//
        private void LoadPageList()
        {
            // Basic implementation: scan ~/*.aspx in web root and subfolders
            var root = Server.MapPath("~");
            var pages = Directory.GetFiles(root, "*.aspx", SearchOption.AllDirectories)
                .Select(p => "~/" + p.Substring(root.Length).Replace("\\", "/")).OrderBy(x => x).ToList();
            ddlPages.Items.Clear();
            foreach (var p in pages) ddlPages.Items.Add(new ListItem(p, p));
            if (ddlPages.Items.Count > 0) ddlPages.SelectedIndex = 0;
            LoadControlsForPage(ddlPages.SelectedValue);
        }

        protected void btnScan_Click(object sender, EventArgs e) => LoadPageList();

        protected void ddlPages_ServerChange(object sender, EventArgs e)
        {
            LoadControlsForPage(ddlPages.SelectedValue);
        }

        private void LoadControlsForPage_OLD(string pageVirtualPath)
        {
            lbControls.Items.Clear();
            var physical = Server.MapPath(pageVirtualPath);
            if (!File.Exists(physical)) return;

            var text = File.ReadAllText(physical);
            // crude regex to find runat="server" id="..."
            var matches = System.Text.RegularExpressions.Regex.Matches(text, @"id\s*=\s*[""'](?<id>[^""']+)[""'][^>]*runat\s*=\s*[""']server[""']", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                var id = m.Groups["id"].Value;
                lbControls.Items.Add(new ListItem(id));
            }
        }

        private void LoadPermissions()
        {
            ddlPermissions.Items.Clear();
            using (var cn = new System.Data.SqlClient.SqlConnection(Conn))
            using (var cmd = new System.Data.SqlClient.SqlCommand("SELECT PermissionId, PermissionName FROM dbo.Permission WHERE IsObsolete = 0 ORDER BY PermissionName", cn))
            {
                cn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read()) ddlPermissions.Items.Add(new ListItem(rdr.GetString(1), rdr.GetInt32(0).ToString()));
                }
            }
        }

        protected void btnMap_Click_OLD(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(ddlPages.SelectedValue) || lbControls.SelectedIndex < 0 || ddlPermissions.SelectedIndex < 0) return;
            int permId = int.Parse(ddlPermissions.SelectedValue);
            string page = ddlPages.SelectedValue;
            string controlId = lbControls.SelectedValue;

            var svc = new PermissionService(Conn);
            svc.SavePermissionUiMetadata(permId, page, controlId, "UI");

            // optionally create a mapping permission row if the selected permission is placeholder - you can add more logic
            PermissionCache.InvalidateAllPermissionsCache();
            // success message
        }

        protected void btnMap_Click(object sender, EventArgs e)
        {
            lblMessage.Text = "";
            if (string.IsNullOrEmpty(ddlPages.SelectedValue) || lbControls.SelectedIndex < 0 || ddlPermissions.SelectedIndex < 0)
            {
                lblMessage.Text = "Select a page, control and permission first.";
                return;
            }

            int permId = int.Parse(ddlPermissions.SelectedValue);
            string page = ddlPages.SelectedValue;
            string controlId = lbControls.SelectedValue;

            try
            {
                // Save via SP to allow audit: create a small proc if you don't have it
                using (var cn = new System.Data.SqlClient.SqlConnection(Conn))
                using (var cmd = new System.Data.SqlClient.SqlCommand("dbo.usp_UpdatePermissionUiMetadata", cn) { CommandType = System.Data.CommandType.StoredProcedure })
                {
                    cmd.Parameters.AddWithValue("@PermissionId", permId);
                    cmd.Parameters.AddWithValue("@PagePath", page);
                    cmd.Parameters.AddWithValue("@ControlId", controlId);
                    var changedBy = Session["USERID"] ?? (object)User.Identity.Name ?? System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                    cmd.Parameters.AddWithValue("@ChangedBy", changedBy);
                    cn.Open();
                    cmd.ExecuteNonQuery();
                }

                PermissionCache.InvalidateAllPermissionsCache();
                lblMessage.Text = "Mapping saved successfully.";
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Error saving mapping: " + ex.Message;
            }
        }

    }
}