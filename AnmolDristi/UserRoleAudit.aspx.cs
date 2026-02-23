using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class UserRoleAudit : BasePage
    {
        private string Conn => System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            //RequirePermission("UI.Admin.UserRoleAudit");
            if (!IsPostBack)
            {
                LoadRoleDropdown();
                Bind(); // Load top 1000 recent on first hit
            }
        }

        private void LoadRoleDropdown()
        {
            ddlRole.Items.Clear();
            ddlRole.Items.Add(new ListItem("-- All Roles --", "0"));

            using (var cn = new SqlConnection(Conn))
            {
                cn.Open();
                using (var cmd = new SqlCommand("SELECT RoleId, RoleName FROM dbo.Role ORDER BY RoleName", cn))
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        ddlRole.Items.Add(new ListItem(rdr.GetString(1), rdr.GetInt32(0).ToString()));
                    }
                }
            }
        }

        protected void btnFilter_Click(object sender, EventArgs e)
        {
            Bind();
        }

        private void Bind()
        {
            string empCode = txtEmployeeCode.Text.Trim();
            int roleId = 0;
            DateTime dtFrom, dtTo;

            bool hasEmpCode = !string.IsNullOrEmpty(empCode);
            bool hasRole = int.TryParse(ddlRole.SelectedValue, out roleId) && roleId > 0;
            bool hasFrom = DateTime.TryParse(txtFrom.Text.Trim(), out dtFrom);
            bool hasTo = DateTime.TryParse(txtTo.Text.Trim(), out dtTo);

            try
            {
                using (var cn = new SqlConnection(Conn))
                {
                    // JOINing to MST_UserMaster so we can display the readable EmployeeCode 
                    // and filter by it, instead of expecting the admin to know the User's internal ID.
                    string sql = @"
                        SELECT TOP (1000) 
                            a.AuditId, 
                            u.EmployeeCode, 
                            r.RoleName, 
                            a.ActionType, 
                            a.Details,
                            a.ChangedBy, 
                            a.ChangedAtIST AS ActionDate 
                        FROM dbo.UserRoleAudit a
                        LEFT JOIN dbo.Role r ON a.RoleId = r.RoleId
                        LEFT JOIN dbo.MST_UserMaster u ON a.UserId = u.Id
                        WHERE 1=1 ";

                    if (hasEmpCode) sql += " AND u.EmployeeCode = @empCode";
                    if (hasRole) sql += " AND a.RoleId = @r";
                    if (hasFrom) sql += " AND a.ChangedAtIST >= @from";
                    if (hasTo) sql += " AND a.ChangedAtIST <= @to";

                    sql += " ORDER BY a.ChangedAtIST DESC";

                    using (var cmd = new SqlCommand(sql, cn))
                    {
                        if (hasEmpCode) cmd.Parameters.AddWithValue("@empCode", empCode);
                        if (hasRole) cmd.Parameters.AddWithValue("@r", roleId);
                        if (hasFrom) cmd.Parameters.AddWithValue("@from", dtFrom);
                        if (hasTo)
                        {
                            // Include the entire "To" day up until 11:59:59 PM
                            dtTo = dtTo.AddDays(1).AddSeconds(-1);
                            cmd.Parameters.AddWithValue("@to", dtTo);
                        }

                        cn.Open();
                        var dt = new DataTable();
                        dt.Load(cmd.ExecuteReader());

                        gvAudit.DataSource = dt;
                        gvAudit.DataBind();

                        lblMessage.Text = $"Loaded {dt.Rows.Count} audit record(s).";
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