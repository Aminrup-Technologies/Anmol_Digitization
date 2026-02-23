using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class RoleList : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            //RequirePermission("UI.Admin.Roles");
            if (!IsPostBack)
            {
                BindRoles();
            }
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
            try
            {
                // hfRoleId needs to be declared in your ASPX file:
                // <asp:HiddenField ID="hfRoleId" runat="server" Value="0" />
                int roleId = Convert.ToInt32(hfRoleId.Value);

                using (var cn = new SqlConnection(Conn))
                {
                    // Logic: If hfRoleId > 0, Update, else Insert
                    string spName = (roleId > 0) ? "dbo.usp_UpdateRole" : "dbo.usp_CreateRole";
                    using (var cmd = new SqlCommand(spName, cn) { CommandType = CommandType.StoredProcedure })
                    {
                        if (roleId > 0)
                        {
                            cmd.Parameters.AddWithValue("@RoleId", roleId);
                        }

                        cmd.Parameters.AddWithValue("@RoleKey", txtKey.Text.Trim());
                        cmd.Parameters.AddWithValue("@RoleName", txtName.Text.Trim());
                        cmd.Parameters.AddWithValue("@IsSystem", chkIsSystem.Checked);

                        if (roleId == 0)
                        {
                            var p = new SqlParameter("@OutRoleId", SqlDbType.Int) { Direction = ParameterDirection.Output };
                            cmd.Parameters.Add(p);

                            // Pass the currently logged-in user to satisfy the SQL procedure we created
                            string currentUser = User?.Identity?.Name;
                            if (string.IsNullOrEmpty(currentUser)) currentUser = "SystemAdmin"; // Fallback

                            cmd.Parameters.AddWithValue("@CreatedBy", currentUser);
                        }

                        cn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                ClearForm();
                BindRoles();
                lblMessage.Text = (roleId > 0) ? "Role updated successfully!" : "Role created successfully!";
                lblMessage.CssClass = "text-success"; // Optional: Make it green
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Error: " + ex.Message;
                lblMessage.CssClass = "text-danger"; // Optional: Make it red
            }
        }

        protected void gvRoles_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            // Ensure we are clicking a button that passes the row index (like our Edit/Delete buttons)
            if (e.CommandName == "EditRole" || e.CommandName == "DeleteRole")
            {
                int index = Convert.ToInt32(e.CommandArgument);
                int roleId = Convert.ToInt32(gvRoles.DataKeys[index].Value);
                GridViewRow row = gvRoles.Rows[index];

                // Safely extract the CheckBox state for the IsSystem column
                bool isSys = ((CheckBox)row.Cells[3].Controls[0]).Checked;

                if (e.CommandName == "EditRole")
                {
                    hfRoleId.Value = roleId.ToString();
                    txtKey.Text = row.Cells[1].Text;   // RoleKey
                    txtName.Text = row.Cells[2].Text;  // RoleName
                    chkIsSystem.Checked = isSys;

                    // Safety: Disable key modification if it's a System Role
                    txtKey.Enabled = !isSys;
                    chkIsSystem.Enabled = !isSys;

                    btnCreate.Text = "Update"; // Visual feedback
                    lblMessage.Text = isSys ? "Editing System Role (Limited Fields)" : "Editing Role";
                    lblMessage.CssClass = "text-info";
                }
                else if (e.CommandName == "DeleteRole")
                {
                    // Fallback check: even though the button is hidden via RowDataBound, 
                    // a smart user might try to trigger the postback manually. We must protect it here!
                    if (isSys)
                    {
                        lblMessage.Text = "Action Denied: Cannot delete system roles!";
                        lblMessage.CssClass = "text-danger";
                        return;
                    }

                    try
                    {
                        using (var cn = new SqlConnection(Conn))
                        using (var cmd = new SqlCommand("DELETE FROM dbo.Role WHERE RoleId = @r", cn))
                        {
                            cmd.Parameters.AddWithValue("@r", roleId);
                            cn.Open();
                            cmd.ExecuteNonQuery();
                        }

                        ClearForm();
                        BindRoles();
                        lblMessage.Text = "Role deleted successfully!";
                        lblMessage.CssClass = "text-success";
                    }
                    catch (Exception ex)
                    {
                        lblMessage.Text = "Error deleting role: " + ex.Message;
                        lblMessage.CssClass = "text-danger";
                    }
                }
            }
        }

        protected void gvRoles_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            // Check if the row being bound is an actual DataRow
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                // Safely extract the 'IsSystem' value from the underlying database row
                bool isSystem = Convert.ToBoolean(DataBinder.Eval(e.Row.DataItem, "IsSystem"));

                if (isSystem)
                {
                    // Index 5 represents the 6th column (which is the Delete button)
                    if (e.Row.Cells[5].Controls.Count > 0)
                    {
                        // Classic C# casting instead of inline pattern matching
                        Button btnDelete = e.Row.Cells[5].Controls[0] as Button;

                        if (btnDelete != null)
                        {
                            // Hide the delete button to prevent UI confusion
                            btnDelete.Visible = false;
                        }
                    }
                }
            }
        }

        private void ClearForm()
        {
            // Reset everything to default state
            hfRoleId.Value = "0";
            txtKey.Text = "";
            txtName.Text = "";
            chkIsSystem.Checked = false;

            // Very important: Re-enable fields in case the user just finished editing a System role
            txtKey.Enabled = true;
            chkIsSystem.Enabled = true;

            btnCreate.Text = "Create";
            lblMessage.Text = "Click SUBMIT to Save Data!!";
            lblMessage.CssClass = ""; // clear custom colors
        }
    }
}