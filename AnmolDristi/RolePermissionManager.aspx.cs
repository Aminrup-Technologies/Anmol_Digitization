using System;
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
            //RequirePermission("UI.Admin.RolePermissions");
            if (!IsPostBack)
            {
                LoadRoles();
                // We don't bind the grid until a role is selected
            }
        }

        private void LoadRoles()
        {
            ddlRoles.Items.Clear();

            // Add a default "Select" item
            ddlRoles.Items.Add(new ListItem("-- Select a Role --", "0"));

            using (var cn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand("SELECT RoleId, RoleName FROM dbo.Role ORDER BY RoleName", cn))
            {
                cn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        ddlRoles.Items.Add(new ListItem(rdr.GetString(1), rdr.GetInt32(0).ToString()));
                    }
                }
            }
        }

        private void BindGrid()
        {
            if (ddlRoles.SelectedValue == "0")
            {
                // Clear grid if the default item is selected
                gvPerms.DataSource = null;
                gvPerms.DataBind();
                return;
            }

            int roleId = int.Parse(ddlRoles.SelectedValue);
            var dt = new DataTable();

            using (var cn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
                SELECT p.PermissionId, p.PermissionKey, p.PermissionName, p.PermissionType, p.PagePath, p.ControlId,
                CAST(CASE WHEN rp.RolePermissionId IS NULL THEN 0 ELSE 1 END AS BIT) AS IsGranted
                FROM dbo.Permission p
                LEFT JOIN dbo.RolePermission rp ON rp.PermissionId = p.PermissionId AND rp.RoleId = @r
                WHERE p.IsObsolete = 0
                ORDER BY p.PermissionType, p.PermissionKey", cn))
            {
                cmd.Parameters.AddWithValue("@r", roleId);
                cn.Open();
                dt.Load(cmd.ExecuteReader());
            }

            gvPerms.DataSource = dt;
            gvPerms.DataBind();
        }

        protected void ddlRoles_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindGrid();
            lblMessage.Text = "Select a role, check the desired permissions, and click Save.";
            lblMessage.CssClass = "";
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            if (ddlRoles.SelectedValue == "0") return;

            int roleId = int.Parse(ddlRoles.SelectedValue);

            // Capture the actual application user making the change
            string currentUser = User?.Identity?.Name ?? "SystemAdmin";

            try
            {
                using (var cn = new SqlConnection(Conn))
                {
                    cn.Open();
                    foreach (GridViewRow row in gvPerms.Rows)
                    {
                        int permissionId = (int)gvPerms.DataKeys[row.RowIndex].Value;
                        var cb = row.FindControl("cbGrant") as CheckBox;

                        if (cb == null) continue;

                        bool shouldGrant = cb.Checked;

                        // 1. Check the current database state
                        bool currentlyGranted = false;
                        int existingRpId = 0;

                        using (var checkCmd = new SqlCommand("SELECT RolePermissionId FROM dbo.RolePermission WHERE RoleId = @r AND PermissionId = @p", cn))
                        {
                            checkCmd.Parameters.AddWithValue("@r", roleId);
                            checkCmd.Parameters.AddWithValue("@p", permissionId);
                            var res = checkCmd.ExecuteScalar();
                            if (res != null)
                            {
                                currentlyGranted = true;
                                existingRpId = Convert.ToInt32(res);
                            }
                        }

                        // 2. Process GRANT if it changed from unchecked to checked
                        if (shouldGrant && !currentlyGranted)
                        {
                            using (var cmd = new SqlCommand(@"
                        -- Insert the actual permission mapping
                        DECLARE @NewRpId INT;
                        INSERT INTO dbo.RolePermission (RoleId, PermissionId) VALUES (@r, @p);
                        SET @NewRpId = SCOPE_IDENTITY();
                        
                        -- Insert the Audit Log
                        INSERT INTO dbo.RolePermissionAudit (RolePermissionId, RoleId, PermissionId, ActionType, ChangedBy)
                        VALUES (@NewRpId, @r, @p, 'GRANT', @User);
                    ", cn))
                            {
                                cmd.Parameters.AddWithValue("@r", roleId);
                                cmd.Parameters.AddWithValue("@p", permissionId);
                                cmd.Parameters.AddWithValue("@User", currentUser);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        // 3. Process REVOKE if it changed from checked to unchecked
                        else if (!shouldGrant && currentlyGranted)
                        {
                            using (var cmd = new SqlCommand(@"
                        -- Delete the permission mapping
                        DELETE FROM dbo.RolePermission WHERE RolePermissionId = @RpId;
                        
                        -- Insert the Audit Log
                        INSERT INTO dbo.RolePermissionAudit (RolePermissionId, RoleId, PermissionId, ActionType, ChangedBy)
                        VALUES (@RpId, @r, @p, 'REVOKE', @User);
                    ", cn))
                            {
                                cmd.Parameters.AddWithValue("@r", roleId);
                                cmd.Parameters.AddWithValue("@p", permissionId);
                                cmd.Parameters.AddWithValue("@RpId", existingRpId);
                                cmd.Parameters.AddWithValue("@User", currentUser);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }

                // Try to clear caches if implemented
                try { PermissionCache.InvalidateAllPermissionsCache(); } catch { }

                BindGrid();
                lblMessage.Text = "Permissions updated and audited successfully!";
                lblMessage.CssClass = "text-success";
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Error saving permissions: " + ex.Message;
                lblMessage.CssClass = "text-danger";
            }
        }
    }
}