using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class RolePermissionAudit : BasePage
    {
        private string Conn => System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            //RequirePermission("UI.Admin.Audit");
            if (!IsPostBack)
            {
                LoadDropdowns();
                Bind();
            }
        }

        private void LoadDropdowns()
        {
            using (var cn = new SqlConnection(Conn))
            {
                cn.Open();

                // Load Roles
                ddlRole.Items.Add(new ListItem("-- All Roles --", ""));
                using (var cmd = new SqlCommand("SELECT RoleId, RoleName FROM dbo.Role ORDER BY RoleName", cn))
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                        ddlRole.Items.Add(new ListItem(rdr.GetString(1), rdr.GetInt32(0).ToString()));
                }

                // Load Permissions
                ddlPerm.Items.Add(new ListItem("-- All Permissions --", ""));
                using (var cmd = new SqlCommand("SELECT PermissionId, PermissionName FROM dbo.Permission ORDER BY PermissionName", cn))
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                        ddlPerm.Items.Add(new ListItem(rdr.GetString(1), rdr.GetInt32(0).ToString()));
                }
            }
        }

        protected void btnFilter_Click(object sender, EventArgs e)
        {
            Bind();
        }

        private void Bind()
        {
            int roleId; int permId;
            DateTime dtFrom, dtTo;

            // Validate selections
            bool hasRole = int.TryParse(ddlRole.SelectedValue, out roleId);
            bool hasPerm = int.TryParse(ddlPerm.SelectedValue, out permId);
            bool hasFrom = DateTime.TryParse(txtFrom.Text.Trim(), out dtFrom);
            bool hasTo = DateTime.TryParse(txtTo.Text.Trim(), out dtTo);

            try
            {
                using (var cn = new SqlConnection(Conn))
                {
                    // CHANGED: We are now explicitly selecting ChangedAtIST and aliasing it as 'ChangedAt' for the GridView
                    string sql = @"
                SELECT TOP (1000) 
                    a.AuditId, 
                    r.RoleName, 
                    p.PermissionName, 
                    a.ActionType, 
                    a.ChangedBy, 
                    a.ChangedAtIST AS ActionDate 
                FROM dbo.RolePermissionAudit a
                LEFT JOIN dbo.Role r ON a.RoleId = r.RoleId
                LEFT JOIN dbo.Permission p ON a.PermissionId = p.PermissionId
                WHERE 1=1 ";

                    if (hasRole) sql += " AND a.RoleId = @r";
                    if (hasPerm) sql += " AND a.PermissionId = @p";

                    // CHANGED: Filtering against the IST column so user searches match their local timezone
                    if (hasFrom) sql += " AND a.ChangedAtIST >= @from";
                    if (hasTo) sql += " AND a.ChangedAtIST <= @to";

                    sql += " ORDER BY a.ChangedAtIST DESC";

                    using (var cmd = new SqlCommand(sql, cn))
                    {
                        if (hasRole) cmd.Parameters.AddWithValue("@r", roleId);
                        if (hasPerm) cmd.Parameters.AddWithValue("@p", permId);
                        if (hasFrom) cmd.Parameters.AddWithValue("@from", dtFrom);
                        if (hasTo)
                        {
                            dtTo = dtTo.AddDays(1).AddSeconds(-1);
                            cmd.Parameters.AddWithValue("@to", dtTo);
                        }

                        cn.Open();
                        var dt = new DataTable();
                        dt.Load(cmd.ExecuteReader());

                        gvAudit.DataSource = dt;
                        gvAudit.DataBind();

                        lblMessage.Text = $"Loaded {dt.Rows.Count} record(s).";
                        lblMessage.CssClass = "text-success";
                    }
                }
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Error loading audit log: " + ex.Message;
                lblMessage.CssClass = "text-danger";
            }
        }
    }
}