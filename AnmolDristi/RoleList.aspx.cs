using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;

namespace AnmolDristi
{
    public partial class RoleList : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            //RequirePermission("UI.Admin.Roles");
            if (!IsPostBack) BindRoles();
        }

        private string Conn => System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        private void BindRoles()
        {
            using (var cn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand("SELECT RoleId, RoleKey, RoleName, IsSystem FROM dbo.Role ORDER BY RoleId", cn))
            {
                cn.Open();
                var dt = new DataTable();
                dt.Load(cmd.ExecuteReader());
                gvRoles.DataSource = dt;
                gvRoles.DataBind();
            }
        }

        protected void btnCreate_Click(object sender, EventArgs e)
        {
            int outId = 0;
            using (var cn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand("dbo.usp_CreateRole", cn) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("@RoleKey", txtKey.Text.Trim());
                cmd.Parameters.AddWithValue("@RoleName", txtName.Text.Trim());
                cmd.Parameters.AddWithValue("@IsSystem", chkIsSystem.Checked);
                var p = new SqlParameter("@OutRoleId", SqlDbType.Int) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(p);
                cn.Open();
                cmd.ExecuteNonQuery();
                outId = (int)p.Value;
            }
            BindRoles();
        }

        protected void gvRoles_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "DeleteRole")
            {
                int row = Convert.ToInt32(((GridViewRow)((Control)e.CommandSource).NamingContainer).RowIndex);
                int roleId = (int)gvRoles.DataKeys[row].Value;
                // Protect system roles
                using (var cn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand("SELECT IsSystem FROM dbo.Role WHERE RoleId = @r", cn))
                {
                    cmd.Parameters.AddWithValue("@r", roleId);
                    cn.Open();
                    var isSys = Convert.ToBoolean(cmd.ExecuteScalar() ?? false);
                    if (isSys) throw new InvalidOperationException("Cannot delete system role");
                }

                using (var cn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand("DELETE FROM dbo.Role WHERE RoleId = @r", cn))
                {
                    cmd.Parameters.AddWithValue("@r", roleId);
                    cn.Open();
                    cmd.ExecuteNonQuery();
                }
                BindRoles();
            }

            if (e.CommandName == "EditRole")
            {
                // implement popup/edit flow as needed — keep simple: reuse create form with hidden id
            }
        }
    }
}