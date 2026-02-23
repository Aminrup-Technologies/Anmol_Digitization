using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class PermissionList : BasePage
    {
        private string Conn => System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            //RequirePermission("UI.Admin.Permissions");
            if (!IsPostBack)
            {
                Bind();
            }
        }

        private void Bind()
        {
            using (var cn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand("SELECT PermissionId, PermissionKey, PermissionName, PermissionType, PagePath, ControlId, IsObsolete FROM dbo.Permission ORDER BY PermissionId", cn))
            {
                cn.Open();
                var dt = new DataTable();
                dt.Load(cmd.ExecuteReader());
                gvPerms.DataSource = dt;
                gvPerms.DataBind();
            }
        }

        protected void btnCreatePerm_Click(object sender, EventArgs e)
        {
            try
            {
                int permId = Convert.ToInt32(hfPermissionId.Value);

                using (var cn = new SqlConnection(Conn))
                {
                    string spName = (permId > 0) ? "dbo.usp_UpdatePermission" : "dbo.usp_CreatePermission";
                    using (var cmd = new SqlCommand(spName, cn) { CommandType = CommandType.StoredProcedure })
                    {
                        if (permId > 0) cmd.Parameters.AddWithValue("@PermissionId", permId);

                        cmd.Parameters.AddWithValue("@PermissionKey", txtPKey.Text.Trim());
                        cmd.Parameters.AddWithValue("@PermissionName", txtPName.Text.Trim());

                        // Handle potential nulls for optional fields
                        cmd.Parameters.AddWithValue("@PermissionType", string.IsNullOrWhiteSpace(txtPType.Text) ? (object)DBNull.Value : txtPType.Text.Trim());
                        cmd.Parameters.AddWithValue("@PagePath", string.IsNullOrWhiteSpace(txtPagePath.Text) ? (object)DBNull.Value : txtPagePath.Text.Trim());
                        cmd.Parameters.AddWithValue("@ControlId", string.IsNullOrWhiteSpace(txtCtrl.Text) ? (object)DBNull.Value : txtCtrl.Text.Trim());

                        cmd.Parameters.AddWithValue("@IsObsolete", chkIsObsolete.Checked);

                        if (permId == 0)
                        {
                            var p = new SqlParameter("@OutPermissionId", SqlDbType.Int) { Direction = ParameterDirection.Output };
                            cmd.Parameters.Add(p);

                            string currentUser = User?.Identity?.Name ?? "SystemAdmin";
                            cmd.Parameters.AddWithValue("@CreatedBy", currentUser);
                        }

                        cn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                ClearForm();
                Bind();
                lblMessage.Text = (permId > 0) ? "Permission updated successfully!" : "Permission created successfully!";
                lblMessage.CssClass = "text-success";
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Error: " + ex.Message;
                lblMessage.CssClass = "text-danger";
            }
        }

        protected void gvPerms_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "EditPerm" || e.CommandName == "DeletePerm")
            {
                int index = Convert.ToInt32(e.CommandArgument);
                int permId = Convert.ToInt32(gvPerms.DataKeys[index].Value);
                GridViewRow row = gvPerms.Rows[index];

                if (e.CommandName == "EditPerm")
                {
                    hfPermissionId.Value = permId.ToString();

                    // Decode HTML entities (in case names have special characters like &)
                    txtPKey.Text = Server.HtmlDecode(row.Cells[1].Text).Replace("&nbsp;", "");
                    txtPName.Text = Server.HtmlDecode(row.Cells[2].Text).Replace("&nbsp;", "");
                    txtPType.Text = Server.HtmlDecode(row.Cells[3].Text).Replace("&nbsp;", "");
                    txtPagePath.Text = Server.HtmlDecode(row.Cells[4].Text).Replace("&nbsp;", "");
                    txtCtrl.Text = Server.HtmlDecode(row.Cells[5].Text).Replace("&nbsp;", "");

                    chkIsObsolete.Checked = ((CheckBox)row.Cells[6].Controls[0]).Checked;

                    btnCreatePerm.Text = "Update";
                    lblMessage.Text = "Editing Permission";
                    lblMessage.CssClass = "text-info";
                }
                else if (e.CommandName == "DeletePerm")
                {
                    try
                    {
                        using (var cn = new SqlConnection(Conn))
                        using (var cmd = new SqlCommand("DELETE FROM dbo.Permission WHERE PermissionId = @id", cn))
                        {
                            cmd.Parameters.AddWithValue("@id", permId);
                            cn.Open();
                            cmd.ExecuteNonQuery();
                        }

                        ClearForm();
                        Bind();
                        lblMessage.Text = "Permission deleted successfully!";
                        lblMessage.CssClass = "text-success";
                    }
                    catch (SqlException sqlEx)
                    {
                        // Check if it's a foreign key violation
                        if (sqlEx.Number == 547)
                        {
                            lblMessage.Text = "Cannot delete this permission because it is currently assigned to a Role. Mark it as Obsolete instead.";
                        }
                        else
                        {
                            lblMessage.Text = "Database Error: " + sqlEx.Message;
                        }
                        lblMessage.CssClass = "text-danger";
                    }
                }
            }
        }

        private void ClearForm()
        {
            hfPermissionId.Value = "0";
            txtPKey.Text = "";
            txtPName.Text = "";
            txtPType.Text = "";
            txtPagePath.Text = "";
            txtCtrl.Text = "";
            chkIsObsolete.Checked = false;

            btnCreatePerm.Text = "Create";
            lblMessage.Text = "Enter details and click Create to save data.";
            lblMessage.CssClass = "";
        }
    }
}