using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using System.Text.RegularExpressions;
using System.Data.SqlClient;

namespace AnmolDristi
{
    public partial class UiControlMapper : BasePage
    {
        // Use the connection string property from your BasePage or define it directly
        private string Conn => System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        // Optimized Regex for finding runat="server" controls
        private static readonly Regex RunatIdRegex = new Regex(@"(?i)(?:id\s*=\s*['""](?<id>[^'""]+)['""]\s*[^>]*runat\s*=\s*['""]server['""])|(?:runat\s*=\s*['""]server['""]\s*[^>]*id\s*=\s*['""](?<id2>[^'""]+)['""])", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        protected void Page_Load(object sender, EventArgs e)
        {
            //RequirePermission("UI.Admin.ControlMapper");
            if (!IsPostBack)
            {
                LoadPermissions();
                LoadPageList();
            }
        }

        private void LoadPageList()
        {
            var root = Server.MapPath("~");

            // Find both ASPX pages and Master pages
            var aspxFiles = Directory.GetFiles(root, "*.aspx", SearchOption.AllDirectories);
            var masterFiles = Directory.GetFiles(root, "*.master", SearchOption.AllDirectories);

            // Combine, format, and sort them
            var pages = aspxFiles.Concat(masterFiles)
                .Select(p => "~/" + p.Substring(root.Length).Replace("\\", "/"))
                .OrderBy(x => x)
                .ToList();

            ddlPages.Items.Clear();
            foreach (var p in pages)
            {
                ddlPages.Items.Add(new ListItem(p, p));
            }

            if (ddlPages.Items.Count > 0)
            {
                ddlPages.SelectedIndex = 0;
                LoadControlsForPage(ddlPages.SelectedValue);
            }
        }

        private void LoadPageList_OLD()
        {
            // Scan ~/*.aspx in web root and subfolders
            var root = Server.MapPath("~");
            var pages = Directory.GetFiles(root, "*.aspx", SearchOption.AllDirectories)
                .Select(p => "~/" + p.Substring(root.Length).Replace("\\", "/"))
                .OrderBy(x => x).ToList();

            ddlPages.Items.Clear();
            foreach (var p in pages) ddlPages.Items.Add(new ListItem(p, p));

            if (ddlPages.Items.Count > 0)
            {
                ddlPages.SelectedIndex = 0;
                LoadControlsForPage(ddlPages.SelectedValue);
            }
        }

        private void LoadPermissions()
        {
            ddlPermissions.Items.Clear();
            ddlPermissions.Items.Add(new ListItem("-- Select a Permission to Map --", "0"));

            using (var cn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand("SELECT PermissionId, PermissionName FROM dbo.Permission WHERE IsObsolete = 0 ORDER BY PermissionName", cn))
            {
                cn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        ddlPermissions.Items.Add(new ListItem(rdr.GetString(1), rdr.GetInt32(0).ToString()));
                    }
                }
            }
        }

        private void LoadControlsForPage(string pageVirtualPath)
        {
            lbControls.Items.Clear();
            lblMappingStatus.Text = "Select a control to view mapping.";

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

            if (lbControls.Items.Count > 0)
            {
                lbControls.SelectedIndex = 0;
                ShowSelectedControlMapping(pageVirtualPath, lbControls.SelectedValue);
            }
        }

        private void ShowSelectedControlMapping(string page, string controlId)
        {
            if (string.IsNullOrEmpty(page) || string.IsNullOrEmpty(controlId)) return;

            using (var cn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand("SELECT PermissionId, PermissionKey, PermissionName FROM dbo.Permission WHERE PagePath = @page AND ControlId = @ctrl AND IsObsolete = 0", cn))
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

                        lblMappingStatus.Text = $"Currently mapped to: <strong>{HttpUtility.HtmlEncode(name ?? key)}</strong>";
                        lblMappingStatus.CssClass = "text-success";

                        if (pid > 0)
                        {
                            var li = ddlPermissions.Items.FindByValue(pid.ToString());
                            if (li != null)
                            {
                                ddlPermissions.ClearSelection();
                                li.Selected = true;
                            }
                        }
                    }
                    else
                    {
                        lblMappingStatus.Text = "No permission mapped for this control yet.";
                        lblMappingStatus.CssClass = "text-warning";
                        ddlPermissions.ClearSelection();
                    }
                }
            }
        }

        // --- Event Handlers ---

        protected void btnScan_Click(object sender, EventArgs e)
        {
            LoadPageList();
            lblMessage.Text = "Pages rescanned successfully.";
            lblMessage.CssClass = "text-info";
        }

        protected void ddlPages_ServerChange(object sender, EventArgs e)
        {
            LoadControlsForPage(ddlPages.SelectedValue);
        }

        protected void lbControls_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowSelectedControlMapping(ddlPages.SelectedValue, lbControls.SelectedValue);
        }

        protected void btnMap_Click(object sender, EventArgs e)
        {
            lblMessage.Text = "";
            lblMessage.CssClass = "";

            if (string.IsNullOrEmpty(ddlPages.SelectedValue) || lbControls.SelectedIndex < 0 || ddlPermissions.SelectedValue == "0")
            {
                lblMessage.Text = "Please select a Page, a Control, and a Permission.";
                lblMessage.CssClass = "text-danger";
                return;
            }

            int permId = int.Parse(ddlPermissions.SelectedValue);
            string page = ddlPages.SelectedValue;
            string controlId = lbControls.SelectedValue;

            try
            {
                using (var cn = new SqlConnection(Conn))
                {
                    cn.Open();

                    // First, unmap this control from any other permission it might currently have 
                    // (to prevent one control from being assigned to multiple permissions)
                    using (var cmdClear = new SqlCommand("UPDATE dbo.Permission SET PagePath = NULL, ControlId = NULL WHERE PagePath = @PagePath AND ControlId = @ControlId", cn))
                    {
                        cmdClear.Parameters.AddWithValue("@PagePath", page);
                        cmdClear.Parameters.AddWithValue("@ControlId", controlId);
                        cmdClear.ExecuteNonQuery();
                    }

                    // Now, assign the Page and Control to the newly selected Permission
                    using (var cmdUpdate = new SqlCommand("UPDATE dbo.Permission SET PagePath = @PagePath, ControlId = @ControlId WHERE PermissionId = @PermissionId", cn))
                    {
                        cmdUpdate.Parameters.AddWithValue("@PermissionId", permId);
                        cmdUpdate.Parameters.AddWithValue("@PagePath", page);
                        cmdUpdate.Parameters.AddWithValue("@ControlId", controlId);
                        cmdUpdate.ExecuteNonQuery();
                    }
                }

                // Cache refresh skipped per instructions
                // try { PermissionCache.InvalidateAllPermissionsCache(); } catch { }

                ShowSelectedControlMapping(page, controlId); // Refresh UI to show new mapping
                lblMessage.Text = "Mapping saved successfully.";
                lblMessage.CssClass = "text-success";
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Error saving mapping: " + ex.Message;
                lblMessage.CssClass = "text-danger";
            }
        }
    }
}