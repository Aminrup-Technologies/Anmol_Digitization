using System;
using System.Data;
using System.Data.SqlClient;

namespace AnmolDristi
{
    public partial class PermissionList : BasePage
    {
        private string Conn => System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;


        protected void Page_Load(object sender, EventArgs e)
        {
            //RequirePermission("UI.Admin.Roles");
            if (!IsPostBack) Bind();
        }

        private void Bind()
        {
            using (var cn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand("SELECT PermissionId, PermissionKey, PermissionName, PermissionType, PagePath, ControlId, IsObsolete FROM dbo.Permission ORDER BY PermissionId", cn))
            {
                cn.Open();
                var dt = new DataTable(); dt.Load(cmd.ExecuteReader());
                gvPerms.DataSource = dt;
                gvPerms.DataBind();
            }
        }

        protected void btnCreatePerm_Click(object sender, EventArgs e)
        {
            int outId = 0;
            using (var cn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand("dbo.usp_CreatePermission", cn) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("@PermissionKey", txtPKey.Text.Trim());
                cmd.Parameters.AddWithValue("@PermissionName", txtPName.Text.Trim());
                cmd.Parameters.AddWithValue("@PermissionType", txtPType.Text.Trim());
                cmd.Parameters.AddWithValue("@PagePath", txtPagePath.Text.Trim());
                cmd.Parameters.AddWithValue("@ControlId", txtCtrl.Text.Trim());
                var p = new SqlParameter("@OutPermissionId", SqlDbType.Int) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(p);
                cn.Open();
                cmd.ExecuteNonQuery();
                outId = (int)p.Value;
            }
            Bind();
        }
    }
}