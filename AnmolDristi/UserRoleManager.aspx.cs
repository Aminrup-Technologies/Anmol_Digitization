using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;
using System.Collections.Generic;

namespace AnmolDristi
{
    public partial class UserRoleManager : BasePage
    {
        private string Conn => System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            //RequirePermission("UI.Admin.UserRoles");
            if (!IsPostBack)
            {
                LoadAllRoles();
                btnSaveRoles.Enabled = false; // Disable save until a user is loaded
            }
        }

        private void LoadAllRoles()
        {
            cblRoles.Items.Clear();
            using (var cn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand("SELECT RoleId, RoleName FROM dbo.Role ORDER BY RoleName", cn))
            {
                cn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        cblRoles.Items.Add(new ListItem(rdr.GetString(1), rdr.GetInt32(0).ToString()));
                    }
                }
            }
        }

        private int ResolveUserId(string employeeCode)
        {
            if (string.IsNullOrWhiteSpace(employeeCode)) return 0;

            using (var cn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand("SELECT Id FROM dbo.MST_UserMaster WHERE EmployeeCode = @code", cn))
            {
                cmd.Parameters.AddWithValue("@code", employeeCode.Trim());
                cn.Open();
                var obj = cmd.ExecuteScalar();
                return obj == null ? 0 : Convert.ToInt32(obj);
            }
        }

        protected void btnLoad_Click(object sender, EventArgs e)
        {
            string empCode = txtEmployeeCode.Text.Trim();
            int userId = ResolveUserId(empCode);

            if (userId == 0)
            {
                lblMessage.Text = "Error: Employee Code '" + empCode + "' not found.";
                lblMessage.CssClass = "text-danger";
                foreach (ListItem li in cblRoles.Items) li.Selected = false;
                btnSaveRoles.Enabled = false;
                return;
            }

            try
            {
                // Load assigned roles
                using (var cn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand("SELECT RoleId FROM dbo.UserRole WHERE UserId = @u", cn))
                {
                    cmd.Parameters.AddWithValue("@u", userId);
                    cn.Open();

                    var assigned = new HashSet<int>();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read()) assigned.Add(rdr.GetInt32(0));
                    }

                    foreach (ListItem li in cblRoles.Items)
                    {
                        li.Selected = assigned.Contains(int.Parse(li.Value));
                    }
                }

                lblMessage.Text = "Loaded roles for Employee: " + empCode;
                lblMessage.CssClass = "text-info";
                btnSaveRoles.Enabled = true; // Enable the save button
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Error loading roles: " + ex.Message;
                lblMessage.CssClass = "text-danger";
            }
        }

        protected void btnSaveRoles_Click(object sender, EventArgs e)
        {
            string empCode = txtEmployeeCode.Text.Trim();
            int userId = ResolveUserId(empCode);

            if (userId == 0)
            {
                lblMessage.Text = "Error: Employee Code not found.";
                lblMessage.CssClass = "text-danger";
                return;
            }

            try
            {
                string currentUser = User?.Identity?.Name ?? "SystemAdmin";

                using (var cn = new SqlConnection(Conn))
                {
                    cn.Open();

                    foreach (ListItem li in cblRoles.Items)
                    {
                        int currentRoleId = int.Parse(li.Value);
                        bool shouldHaveRole = li.Selected;

                        // 1. Check current database state
                        bool currentlyHasRole = false;
                        int existingUserRoleId = 0;

                        using (var checkCmd = new SqlCommand("SELECT UserRoleId FROM dbo.UserRole WHERE UserId = @u AND RoleId = @r", cn))
                        {
                            checkCmd.Parameters.AddWithValue("@u", userId);
                            checkCmd.Parameters.AddWithValue("@r", currentRoleId);
                            var res = checkCmd.ExecuteScalar();
                            if (res != null)
                            {
                                currentlyHasRole = true;
                                existingUserRoleId = Convert.ToInt32(res);
                            }
                        }

                        // 2. Process GRANT if they don't have it but it is checked
                        if (shouldHaveRole && !currentlyHasRole)
                        {
                            using (var cmd = new SqlCommand(@"
                        DECLARE @NewUrId INT;
                        
                        -- Insert the mapping
                        INSERT INTO dbo.UserRole (UserId, RoleId, AssignedBy) 
                        VALUES (@u, @r, @User);
                        
                        SET @NewUrId = SCOPE_IDENTITY();
                        
                        -- Insert the Audit Log
                        INSERT INTO dbo.UserRoleAudit (UserRoleId, UserId, RoleId, ActionType, ChangedBy, Details)
                        VALUES (@NewUrId, @u, @r, 'GRANT', @User, 'Role assigned via Admin Panel');
                    ", cn))
                            {
                                cmd.Parameters.AddWithValue("@u", userId);
                                cmd.Parameters.AddWithValue("@r", currentRoleId);
                                cmd.Parameters.AddWithValue("@User", currentUser);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        // 3. Process REVOKE if they have it but it is unchecked
                        else if (!shouldHaveRole && currentlyHasRole)
                        {
                            using (var cmd = new SqlCommand(@"
                        -- Delete the mapping
                        DELETE FROM dbo.UserRole WHERE UserRoleId = @UrId;
                        
                        -- Insert the Audit Log
                        INSERT INTO dbo.UserRoleAudit (UserRoleId, UserId, RoleId, ActionType, ChangedBy, Details)
                        VALUES (@UrId, @u, @r, 'REVOKE', @User, 'Role removed via Admin Panel');
                    ", cn))
                            {
                                cmd.Parameters.AddWithValue("@u", userId);
                                cmd.Parameters.AddWithValue("@r", currentRoleId);
                                cmd.Parameters.AddWithValue("@UrId", existingUserRoleId);
                                cmd.Parameters.AddWithValue("@User", currentUser);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }

                lblMessage.Text = "Roles and Audit Logs updated successfully for Employee: " + empCode;
                lblMessage.CssClass = "text-success";
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Error saving roles: " + ex.Message;
                lblMessage.CssClass = "text-danger";
            }
        }
    }
}