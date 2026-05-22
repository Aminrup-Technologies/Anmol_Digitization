using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class ViewUiControlMappings : BasePage
    {
        private string Conn => ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        // Same robust regex used in your Mapper
        private static readonly Regex RunatIdRegex = new Regex(
            @"(?i)<(?<tag>[a-zA-Z0-9:]+)(?=[^>]*\bid\s*=\s*['""](?<id>[^'""]+)['""])(?=[^>]*\brunat\s*=\s*['""]server['""])[^>]*>",
            RegexOptions.Compiled | RegexOptions.Singleline);

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                PopulateFilters();
                // Optionally auto-select the first page if exists
                if (ddlFilterPage.Items.Count > 1)
                {
                    ddlFilterPage.SelectedIndex = 1;
                    BindTree();
                }
            }
        }

        private void BindTree()
        {
            tvMappingOverview.Nodes.Clear();
            string selectedPage = ddlFilterPage.SelectedValue;
            if (string.IsNullOrEmpty(selectedPage)) return;

            var physical = Server.MapPath(selectedPage);
            if (!File.Exists(physical)) { lblMessage.Text = "File not found."; return; }

            var text = File.ReadAllText(physical);
            var matches = RunatIdRegex.Matches(text);

            TreeNode pageNode = new TreeNode($"📄 {Path.GetFileName(selectedPage)}");
            pageNode.Expanded = true;
            tvMappingOverview.Nodes.Add(pageNode);

            // Fetch all mappings for this page into a dictionary for O(1) lookup
            var mappings = GetMappingsForPage(selectedPage);

            TreeNode currentParent = pageNode;

            foreach (Match m in matches)
            {
                var id = m.Groups["id"].Value;
                var tag = m.Groups["tag"].Value.ToLower();

                // Logic: Groups buttons under their parent panels
                if (tag == "div" || tag == "asp:panel" || tag == "section")
                {
                    currentParent = new TreeNode($"📁 {id}");
                    currentParent.Expanded = true;
                    pageNode.ChildNodes.Add(currentParent);
                }
                else
                {
                    // Check if this specific control has a mapping in the DB
                    bool isMapped = mappings.ContainsKey(id);
                    string permName = isMapped ? mappings[id] : "Unrestricted";
                    string icon = isMapped ? "✅" : "⚠️";

                    TreeNode leaf = new TreeNode($"{icon} {id} ({permName})");
                    currentParent.ChildNodes.Add(leaf);
                }
            }
            tvMappingOverview.ExpandAll();
        }

        private Dictionary<string, string> GetMappingsForPage(string page)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (SqlConnection cn = new SqlConnection(Conn))
            using (SqlCommand cmd = new SqlCommand(@"
                SELECT m.ControlId, p.PermissionName 
                FROM dbo.UiControlMap m
                INNER JOIN dbo.Permission p ON m.PermissionId = p.PermissionId
                WHERE m.PagePath = @page", cn))
            {
                cmd.Parameters.AddWithValue("@page", page);
                cn.Open();
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        dict[rdr.GetString(0)] = rdr.GetString(1);
                    }
                }
            }
            return dict;
        }

        private void PopulateFilters()
        {
            using (SqlConnection cn = new SqlConnection(Conn))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT DISTINCT PagePath FROM dbo.UiControlMap ORDER BY PagePath", cn))
                {
                    cn.Open();
                    ddlFilterPage.DataSource = cmd.ExecuteReader();
                    ddlFilterPage.DataTextField = "PagePath";
                    ddlFilterPage.DataValueField = "PagePath";
                    ddlFilterPage.DataBind();
                    ddlFilterPage.Items.Insert(0, new ListItem("-- Select a Page --", ""));
                }
            }
        }

        protected void Filter_Changed(object sender, EventArgs e)
        {
            BindTree();
        }

        protected void btnExpand_Click(object sender, EventArgs e)
        {
            // Re-bind the tree first so it knows what to expand
            BindTree();
            tvMappingOverview.ExpandAll();
        }

        protected void btnCollapse_Click(object sender, EventArgs e)
        {
            BindTree();
            tvMappingOverview.CollapseAll();
        }
    }
}