using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class RolePermissionManager : BasePage
    {
        private string Conn => System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        protected void Page_Load(object sender, EventArgs e)
        {
            //RequirePermission("UI.Admin.Roles");
            if (!IsPostBack) LoadRoles();
        }

        private void LoadRoles()
        {
            ddlRoles.Items.Clear();
            using (var cn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand("SELECT RoleId, RoleName FROM dbo.Role ORDER BY RoleName", cn))
            {
                cn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read()) ddlRoles.Items.Add(new ListItem(rdr.GetString(1), rdr.GetInt32(0).ToString()));
                }
            }
        }

        private void BindGrid()
        {
            int roleId = ddlRoles.SelectedValue == null ? 0 : int.Parse(ddlRoles.SelectedValue);
            var dt = new DataTable();
            using (var cn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
                SELECT p.PermissionId, p.PermissionKey, p.PermissionName, p.PermissionType, p.PagePath, p.ControlId,
                CAST(CASE WHEN rp.RolePermissionId IS NULL THEN 0 ELSE 1 END AS BIT) AS IsGranted
                FROM dbo.Permission p
                LEFT JOIN dbo.RolePermission rp ON rp.PermissionId = p.PermissionId AND rp.RoleId = @r
                WHERE p.IsObsolete = 0
                ORDER BY p.PermissionKey", cn))
            {
                cmd.Parameters.AddWithValue("@r", roleId);
                cn.Open();
                dt.Load(cmd.ExecuteReader());
            }
            gvPerms.DataSource = dt;
            gvPerms.DataBind();
        }

        protected void ddlRoles_SelectedIndexChanged(object sender, EventArgs e) { BindGrid(); }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            int roleId = int.Parse(ddlRoles.SelectedValue);
            using (var cn = new SqlConnection(Conn))
            {
                cn.Open();
                foreach (GridViewRow row in gvPerms.Rows)
                {
                    //int permissionId = (int)gvPerms.DataKeys[row.RowIndex].Value;
                    //CheckBox cb = (CheckBox)row.FindControl("cbGrant");
                    //if (cb == null) continue;
                    //bool shouldGrant = cb.Checked;

                    int permissionId = (int)gvPerms.DataKeys[row.RowIndex].Value;
                    var cb = row.FindControl("cbGrant") as CheckBox;
                    if (cb == null) continue;
                    bool shouldGrant = cb.Checked;

                    // check current state
                    using (var checkCmd = new SqlCommand("SELECT COUNT(1) FROM dbo.RolePermission WHERE RoleId=@r AND PermissionId=@p", cn))
                    {
                        checkCmd.Parameters.AddWithValue("@r", roleId);
                        checkCmd.Parameters.AddWithValue("@p", permissionId);
                        int exists = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (shouldGrant && exists == 0)
                        {
                            using (var cmd = new SqlCommand("dbo.usp_AssignPermissionToRole", cn) { CommandType = CommandType.StoredProcedure })
                            {
                                cmd.Parameters.AddWithValue("@RoleId", roleId);
                                cmd.Parameters.AddWithValue("@PermissionId", permissionId);
                                var changedBy = Session["USERID"] != null ? Session["USERID"].ToString() : System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                                cmd.Parameters.AddWithValue("@ChangedBy", changedBy);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else if (!shouldGrant && exists > 0)
                        {
                            using (var cmd = new SqlCommand("dbo.usp_RemovePermissionFromRole", cn) { CommandType = CommandType.StoredProcedure })
                            {
                                cmd.Parameters.AddWithValue("@RoleId", roleId);
                                cmd.Parameters.AddWithValue("@PermissionId", permissionId);
                                var changedBy = Session["USERID"] != null ? Session["USERID"].ToString() : System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                                cmd.Parameters.AddWithValue("@ChangedBy", changedBy);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }

            // Invalidate caches (app-level) so changes take effect across app
            PermissionCache.InvalidateAllPermissionsCache();

            // Optionally instruct admins to request users to re-login or use reload endpoint to rebuild session perms
            BindGrid();
        }

        //private void LoadRoles()
        //{
        //    // populate ddlRoles
        //    ddlRoles.Items.Clear();
        //    using (var cn = new System.Data.SqlClient.SqlConnection(Conn))
        //    using (var cmd = new System.Data.SqlClient.SqlCommand("SELECT RoleId, RoleName FROM dbo.Role ORDER BY RoleName", cn))
        //    {
        //        cn.Open();
        //        using (var rdr = cmd.ExecuteReader())
        //        {
        //            while (rdr.Read()) ddlRoles.Items.Add(new ListItem(rdr.GetString(1), rdr.GetInt32(0).ToString()));
        //        }
        //    }
        //    if (ddlRoles.Items.Count > 0) ddlRoles.SelectedIndex = 0;
        //    BindPermissionsGrid();
        //}

        //protected void ddlRoles_SelectedIndexChanged(object sender, EventArgs e) => BindPermissionsGrid();

        //private void BindPermissionsGrid()
        //{
        //    int roleId = int.Parse(ddlRoles.SelectedValue);
        //    var conn = Conn;
        //    var perms = new List<dynamic>();
        //    using (var cn = new System.Data.SqlClient.SqlConnection(conn))
        //    using (var cmd = new System.Data.SqlClient.SqlCommand(@"
        //    SELECT p.PermissionId, p.PermissionName, p.PermissionType, p.PagePath, p.ControlId,
        //           CASE WHEN rp.RoleId IS NULL THEN 0 ELSE 1 END AS IsGranted
        //    FROM dbo.Permission p
        //    LEFT JOIN dbo.RolePermission rp ON rp.PermissionId = p.PermissionId AND rp.RoleId = @r
        //    WHERE p.IsObsolete = 0
        //    ORDER BY p.PermissionType, p.PermissionName", cn))
        //    {
        //        cmd.Parameters.AddWithValue("@r", roleId);
        //        cn.Open();
        //        using (var rdr = cmd.ExecuteReader())
        //        {
        //            while (rdr.Read()) perms.Add(new
        //            {
        //                PermissionId = rdr.GetInt32(0),
        //                Name = rdr.GetString(1),
        //                PermissionType = rdr.IsDBNull(2) ? "" : rdr.GetString(2),
        //                PagePath = rdr.IsDBNull(3) ? "" : rdr.GetString(3),
        //                ControlId = rdr.IsDBNull(4) ? "" : rdr.GetString(4),
        //                IsGranted = rdr.GetInt32(5) == 1
        //            });
        //        }
        //    }
        //    gvPermissions.DataSource = perms;
        //    gvPermissions.DataBind();
        //}

        //protected void btnSave_Click(object sender, EventArgs e)
        //{
        //    int roleId = int.Parse(ddlRoles.SelectedValue);
        //    var service = new PermissionService(Conn);

        //    foreach (GridViewRow row in gvPermissions.Rows)
        //    {
        //        int permissionId = (int)gvPermissions.DataKeys[row.RowIndex].Value; // set DataKeyNames="PermissionId" on GridView
        //        CheckBox cb = (CheckBox)row.FindControl("cbGrant");
        //        if (cb != null)
        //        {
        //            if (cb.Checked)
        //                service.AssignPermissionToRole(roleId, permissionId, User.Identity.Name);
        //            else
        //                service.RemovePermissionFromRole(roleId, permissionId);
        //        }
        //    }

        //    // optional: invalidate session caches (admin can call reload endpoints)
        //    PermissionCache.InvalidateAllPermissionsCache();
        //    // Show success
        //}
    }
}