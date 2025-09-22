using System;
using System.Data;
using System.Data.SqlClient;

namespace AnmolDristi
{
    public partial class RolePermissionAudit : BasePage
    {
        private string Conn => System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        protected void Page_Load(object sender, EventArgs e)
        {
            //RequirePermission("UI.Admin.Roles");
            if (!IsPostBack) Bind();
        }

        protected void btnFilter_Click(object sender, EventArgs e) { Bind(); }

        private void Bind()
        {
            int roleId; int permId;
            DateTime dtFrom, dtTo;
            bool hasRole = int.TryParse(txtRoleId.Text.Trim(), out roleId);
            bool hasPerm = int.TryParse(txtPermId.Text.Trim(), out permId);
            bool hasFrom = DateTime.TryParse(txtFrom.Text.Trim(), out dtFrom);
            bool hasTo = DateTime.TryParse(txtTo.Text.Trim(), out dtTo);

            using (var cn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand("SELECT TOP (1000) * FROM dbo.RolePermissionAudit WHERE 1=1 "
                + (hasRole ? " AND RoleId = @r" : "")
                + (hasPerm ? " AND PermissionId = @p" : "")
                + (hasFrom ? " AND ChangedAt >= @from" : "")
                + (hasTo ? " AND ChangedAt <= @to" : "")
                + " ORDER BY ChangedAt DESC", cn))
            {
                if (hasRole) cmd.Parameters.AddWithValue("@r", roleId);
                if (hasPerm) cmd.Parameters.AddWithValue("@p", permId);
                if (hasFrom) cmd.Parameters.AddWithValue("@from", dtFrom);
                if (hasTo) cmd.Parameters.AddWithValue("@to", dtTo);
                cn.Open();
                var dt = new DataTable(); dt.Load(cmd.ExecuteReader());
                gvAudit.DataSource = dt; gvAudit.DataBind();
            }
        }
    }
}