using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class UserRoleManager : BasePage
    {
        private string Conn => System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        protected void Page_Load(object sender, EventArgs e)
        {
            //RequirePermission("UI.Admin.Roles");
            if (!IsPostBack) LoadAllRoles();
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
                        cblRoles.Items.Add(new ListItem(rdr.GetString(1), rdr.GetInt32(0).ToString()));
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
                // show friendly message
                lblMessage.Text = "EmployeeCode not found: " + empCode;
                foreach (ListItem li in cblRoles.Items) li.Selected = false;
                return;
            }

            // Load assigned roles
            using (var cn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand("SELECT RoleId FROM dbo.UserRole WHERE UserId = @u", cn))
            {
                cmd.Parameters.AddWithValue("@u", userId);
                cn.Open();

                var assigned = new System.Collections.Generic.HashSet<int>();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read()) assigned.Add(rdr.GetInt32(0));
                }

                foreach (ListItem li in cblRoles.Items)
                    li.Selected = assigned.Contains(int.Parse(li.Value));
            }

            lblMessage.Text = "Loaded roles for EmployeeCode: " + empCode;
        }

        protected void btnSaveRoles_Click(object sender, EventArgs e)
        {
            string empCode = txtEmployeeCode.Text.Trim();
            int userId = ResolveUserId(empCode);
            if (userId == 0)
            {
                lblMessage.Text = "EmployeeCode not found: " + empCode;
                return;
            }

            using (var cn = new SqlConnection(Conn))
            {
                cn.Open();
                // Clear existing roles
                using (var del = new SqlCommand("DELETE FROM dbo.UserRole WHERE UserId = @u", cn))
                {
                    del.Parameters.AddWithValue("@u", userId);
                    del.ExecuteNonQuery();
                }

                // Assign selected roles
                foreach (ListItem li in cblRoles.Items)
                {
                    if (li.Selected)
                    {
                        using (var cmd = new SqlCommand("dbo.usp_AssignRoleToUser", cn) { CommandType = CommandType.StoredProcedure })
                        {
                            cmd.Parameters.AddWithValue("@UserId", userId);
                            cmd.Parameters.AddWithValue("@RoleId", int.Parse(li.Value));
                            var changedBy = Session["USERID"] != null ? Session["USERID"].ToString() : System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                            cmd.Parameters.AddWithValue("@ChangedBy", changedBy);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }

            lblMessage.Text = "Roles updated for EmployeeCode: " + empCode;
        }


        private void LoadAllRoles_OLD()
        {
            cblRoles.Items.Clear();
            using (var cn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand("SELECT RoleId, RoleName FROM dbo.Role ORDER BY RoleName", cn))
            {
                cn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read()) cblRoles.Items.Add(new ListItem(rdr.GetString(1), rdr.GetInt32(0).ToString()));
                }
            }
        }

        //protected void btnLoad_Click_OLD(object sender, EventArgs e)
        //{
        //    int userId;
        //    if (!int.TryParse(txtUserId.Text.Trim(), out userId)) return;

        //    // load assigned roles
        //    using (var cn = new SqlConnection(Conn))
        //    using (var cmd = new SqlCommand("SELECT RoleId FROM dbo.UserRole WHERE UserId = @u", cn))
        //    {
        //        cmd.Parameters.AddWithValue("@u", userId);
        //        cn.Open();
        //        var assigned = new System.Collections.Generic.HashSet<int>();
        //        using (var rdr = cmd.ExecuteReader())
        //        {
        //            while (rdr.Read()) assigned.Add(rdr.GetInt32(0));
        //        }

        //        foreach (ListItem li in cblRoles.Items)
        //            li.Selected = assigned.Contains(int.Parse(li.Value));
        //    }
        //}

        //protected void btnSaveRoles_Click_OLD(object sender, EventArgs e)
        //{
        //    int userId;
        //    if (!int.TryParse(txtUserId.Text.Trim(), out userId)) return;

        //    using (var cn = new SqlConnection(Conn))
        //    {
        //        cn.Open();
        //        // Option: remove all and add selected (simple)
        //        using (var del = new SqlCommand("DELETE FROM dbo.UserRole WHERE UserId = @u", cn))
        //        {
        //            del.Parameters.AddWithValue("@u", userId);
        //            del.ExecuteNonQuery();
        //        }

        //        foreach (ListItem li in cblRoles.Items)
        //        {
        //            if (li.Selected)
        //            {
        //                using (var cmd = new SqlCommand("dbo.usp_AssignRoleToUser", cn) { CommandType = CommandType.StoredProcedure })
        //                {
        //                    cmd.Parameters.AddWithValue("@UserId", userId);
        //                    cmd.Parameters.AddWithValue("@RoleId", int.Parse(li.Value));
        //                    var changedBy = Session["USERID"] != null ? Session["USERID"].ToString() : System.Security.Principal.WindowsIdentity.GetCurrent().Name;
        //                    cmd.Parameters.AddWithValue("@ChangedBy", changedBy);
        //                    cmd.ExecuteNonQuery();
        //                }
        //            }
        //        }
        //    }
        //}
    }
}