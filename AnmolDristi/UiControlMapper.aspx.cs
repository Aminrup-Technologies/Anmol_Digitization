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
        private string Conn => System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        // UPGRADED REGEX: Uses lookaheads to reliably capture the HTML Tag Name, ID, and runat="server"
        private static readonly Regex RunatIdRegex = new Regex(
            @"(?i)<(?<tag>[a-zA-Z0-9:]+)(?=[^>]*\bid\s*=\s*['""](?<id>[^'""]+)['""])(?=[^>]*\brunat\s*=\s*['""]server['""])[^>]*>",
            RegexOptions.Compiled);

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                hfStep.Value = "1";
                LoadPermissions();
                LoadPageList();
            }
        }

        private void LoadPageList()
        {
            var root = Server.MapPath("~");
            var aspxFiles = Directory.GetFiles(root, "*.aspx", SearchOption.AllDirectories);
            var masterFiles = Directory.GetFiles(root, "*.master", SearchOption.AllDirectories);

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
            tvControls.Nodes.Clear();
            lblMappingStatus.Text = "Scanning file...";

            if (string.IsNullOrWhiteSpace(pageVirtualPath)) return;
            if (!pageVirtualPath.StartsWith("~/")) pageVirtualPath = "~/" + pageVirtualPath.TrimStart('/');

            var physical = Server.MapPath(pageVirtualPath);
            if (!File.Exists(physical)) return;

            var text = File.ReadAllText(physical);

            // Create the Absolute Root (The Page itself)
            TreeNode pageNode = new TreeNode($"📄 {Path.GetFileName(pageVirtualPath)}", "PAGE_ROOT");
            pageNode.Expanded = true;
            pageNode.SelectAction = TreeNodeSelectAction.None;
            tvControls.Nodes.Add(pageNode);

            // Track the current container we are inside
            TreeNode currentParent = pageNode;

            // Regex to find ANY control with runat="server"
            var matches = Regex.Matches(text,
                @"<(?<tag>[a-zA-Z0-9:]+)[^>]*\bid\s*=\s*['""](?<id>[^'""]+)['""][^>]*\brunat\s*=\s*['""]server['""][^>]*>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Match m in matches)
            {
                var tag = m.Groups["tag"].Value.ToLower();
                var id = m.Groups["id"].Value;

                if (string.IsNullOrWhiteSpace(id) || added.Contains(id)) continue;

                // CHECK: Is this a Panel/Container?
                // If it's a div or panel, it becomes the new parent for subsequent buttons
                if (tag == "div" || tag == "asp:panel" || tag == "section")
                {
                    TreeNode divNode = new TreeNode($"📁 {id}", id);
                    divNode.Expanded = true;
                    divNode.ShowCheckBox = true; // You can map the whole container too!
                    pageNode.ChildNodes.Add(divNode);

                    // Set this as the parent for the next loop iterations
                    currentParent = divNode;
                }
                else
                {
                    // It's a button/link/input -> Add as child of the active div
                    TreeNode leaf = new TreeNode($"🔹 {id}", id);
                    leaf.ShowCheckBox = true;
                    currentParent.ChildNodes.Add(leaf);
                }

                added.Add(id);
            }
        }

        private void ShowSelectedControlMapping(string page, string controlId)
        {
            if (string.IsNullOrEmpty(page) || string.IsNullOrEmpty(controlId) || controlId.StartsWith("~/")) return;

            using (var cn = new SqlConnection(Conn))
            // UPDATED SQL: Now reads from the UiControlMap table!
            using (var cmd = new SqlCommand(@"
        SELECT p.PermissionId, p.PermissionKey, p.PermissionName 
        FROM dbo.UiControlMap m 
        INNER JOIN dbo.Permission p ON m.PermissionId = p.PermissionId 
        WHERE m.PagePath = @page AND m.ControlId = @ctrl AND p.IsObsolete = 0", cn))
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

                        lblMappingStatus.Text = $"Control <b>{controlId}</b> is currently mapped to: <strong>{HttpUtility.HtmlEncode(name ?? key)}</strong>";
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
                        lblMappingStatus.Text = $"Control <b>{controlId}</b> has no permission mapped yet.";
                        lblMappingStatus.CssClass = "text-warning";
                        ddlPermissions.ClearSelection();
                    }
                }
            }
        }

        protected void btnMap_Click(object sender, EventArgs e)
        {
            lblMessage.Text = "";
            lblMessage.CssClass = "";

            List<string> selectedControls = new List<string>();
            GetCheckedNodes(tvControls.Nodes, selectedControls);

            if (string.IsNullOrEmpty(ddlPages.SelectedValue) || selectedControls.Count == 0 || ddlPermissions.SelectedValue == "0")
            {
                lblMessage.Text = "Please check at least one Control box and select a Permission.";
                lblMessage.CssClass = "text-danger";
                hfStep.Value = "3";
                return;
            }

            int permId = int.Parse(ddlPermissions.SelectedValue);
            string page = ddlPages.SelectedValue;

            try
            {
                using (var cn = new SqlConnection(Conn))
                {
                    cn.Open();

                    foreach (string controlId in selectedControls)
                    {
                        // 1. Unmap this specific control from any previous permission to prevent duplicates
                        using (var cmdClear = new SqlCommand("DELETE FROM dbo.UiControlMap WHERE PagePath = @PagePath AND ControlId = @ControlId", cn))
                        {
                            cmdClear.Parameters.AddWithValue("@PagePath", page);
                            cmdClear.Parameters.AddWithValue("@ControlId", controlId);
                            cmdClear.ExecuteNonQuery();
                        }

                        // 2. INSERT into the new Mapping Table!
                        using (var cmdUpdate = new SqlCommand("INSERT INTO dbo.UiControlMap (PagePath, ControlId, PermissionId) VALUES (@PagePath, @ControlId, @PermissionId)", cn))
                        {
                            cmdUpdate.Parameters.AddWithValue("@PermissionId", permId);
                            cmdUpdate.Parameters.AddWithValue("@PagePath", page);
                            cmdUpdate.Parameters.AddWithValue("@ControlId", controlId);
                            cmdUpdate.ExecuteNonQuery();
                        }
                    }
                }

                ShowSelectedControlMapping(page, selectedControls[0]);
                lblMessage.Text = $"<i class='fa fa-check-circle'></i> Successfully mapped {selectedControls.Count} control(s)!";
                lblMessage.CssClass = "text-success font-weight-bold";

                ClearAllChecks(tvControls.Nodes);
                hfStep.Value = "3";
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Error saving mapping: " + ex.Message;
                lblMessage.CssClass = "text-danger";
                hfStep.Value = "3";
            }
        }

        private void ShowSelectedControlMapping_OLD(string page, string controlId)
        {
            if (string.IsNullOrEmpty(page) || string.IsNullOrEmpty(controlId) || controlId.StartsWith("~/")) return;

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

                        lblMappingStatus.Text = $"Control <b>{controlId}</b> is currently mapped to: <strong>{HttpUtility.HtmlEncode(name ?? key)}</strong>";
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
                        lblMappingStatus.Text = $"Control <b>{controlId}</b> has no permission mapped yet.";
                        lblMappingStatus.CssClass = "text-warning";
                        ddlPermissions.ClearSelection();
                    }
                }
            }
        }

        private void GetCheckedNodes(TreeNodeCollection nodes, List<string> selected)
        {
            foreach (TreeNode node in nodes)
            {
                // If the node is checked, add it to our list
                if (node.Checked)
                {
                    // Only add if it's not the Root Page Node itself (the ID might be a virtual path)
                    if (!node.Value.StartsWith("~"))
                    {
                        selected.Add(node.Value);
                    }
                }

                // Recursively check children
                if (node.ChildNodes.Count > 0)
                {
                    GetCheckedNodes(node.ChildNodes, selected);
                }
            }
        }

        // --- Event Handlers ---

        protected void btnScan_Click(object sender, EventArgs e)
        {
            LoadPageList();
            lblMessage.Text = "Pages rescanned successfully.";
            lblMessage.CssClass = "text-info";
            hfStep.Value = "1";
        }

        protected void ddlPages_ServerChange(object sender, EventArgs e)
        {
            LoadControlsForPage(ddlPages.SelectedValue);
            hfStep.Value = "2";
            lblMessage.Text = "";
        }

        // Fixes the CS1061 Error
        protected void tvControls_SelectedNodeChanged(object sender, EventArgs e)
        {
            if (tvControls.SelectedNode != null)
            {
                ShowSelectedControlMapping(ddlPages.SelectedValue, tvControls.SelectedNode.Value);
                hfStep.Value = "3";
                lblMessage.Text = "";
            }
        }

        // Recursive helper to safely clear all checkboxes in the tree
        private void ClearAllChecks(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                node.Checked = false;
                if (node.ChildNodes.Count > 0)
                {
                    ClearAllChecks(node.ChildNodes);
                }
            }
        }

        protected void btnExpandAll_Click(object sender, EventArgs e)
        {
            tvControls.ExpandAll();
            hfStep.Value = "2";
        }

        protected void btnCollapseAll_Click(object sender, EventArgs e)
        {
            tvControls.CollapseAll();
            hfStep.Value = "2";
        }
    }
}