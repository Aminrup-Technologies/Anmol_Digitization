using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.UI.WebControls;
using System.Web.UI;
using System.IO;
using System.Web;

namespace AnmolDristi
{
    public partial class viewusers : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["USERID"] == null || Session["USERNAME"] == null || Session["WORKMAN"] == null)
                {
                    Response.Redirect("login.aspx");
                }
                else
                {
                    BindEmployeeData();
                }
            }
        }

        private void BindEmployeeData(string searchKeyword = "", string statusFilter = "All", string activityFilter = "All")
        {
            string query = @"
                SELECT 
                    u.Id, 
                    u.EmployeeCode, 
                    u.EmployeeName, 
                    u.Mobile, 
                    u.EmployeeStatus,
                    u.ReportingManager,
                    u.ReportingManagerCode,
                    d.Department_Description AS DepartmentName,
                    desg.Designation_Description AS DesignationName,
                    b.Branch_Description AS BranchName,
                    p.PlantID_Description AS PlantName,
                    r.Region_Description AS RegionName,
                    g.Grade_Description AS GradeName,
                    s.LoginTime AS LastLogin,
                    s.IsActive AS IsOnline
                FROM dbo.MST_UserMaster u
                LEFT JOIN dbo.MST_Designation desg ON u.Designation_ID = desg.Id
                LEFT JOIN dbo.MST_Branch b ON u.BranchId = b.Branch_ID
                LEFT JOIN dbo.MST_Department d ON CAST(u.DepartmentId AS VARCHAR(50)) = d.Department_ID
                LEFT JOIN dbo.MST_Plant p ON CAST(u.PlantId AS VARCHAR(50)) = p.PlantID_ID
                LEFT JOIN dbo.MST_Region r ON CAST(u.RegionId AS VARCHAR(50)) = r.Region_ID
                LEFT JOIN dbo.MST_Grade g ON CAST(u.GradeId AS VARCHAR(50)) = g.Grade_ID
                OUTER APPLY (
                    SELECT TOP 1 LoginTime, IsActive FROM ActiveUserSession WITH(NOLOCK) 
                    WHERE EmployeeCode = u.EmployeeCode ORDER BY LoginTime DESC
                ) s
                WHERE 1=1";

            // 1. Name/Code Search Filter
            if (!string.IsNullOrEmpty(searchKeyword))
            {
                query += " AND (u.EmployeeName LIKE @Search OR u.EmployeeCode LIKE @Search)";
            }

            // 2. HR Status Filter
            if (statusFilter != "All")
            {
                query += " AND u.EmployeeStatus = @Status";
            }

            // 3. NEW: System Activity Filter
            if (activityFilter != "All")
            {
                if (activityFilter == "OnlineNow")
                    query += " AND s.IsActive = 1";
                else if (activityFilter == "Active30")
                    query += " AND s.LoginTime >= DATEADD(day, -30, GETDATE())";
                else if (activityFilter == "Inactive30")
                    query += " AND s.LoginTime < DATEADD(day, -30, GETDATE())";
                else if (activityFilter == "Never")
                    query += " AND s.LoginTime IS NULL";
            }

            query += " ORDER BY u.Id DESC";

            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(searchKeyword))
                        cmd.Parameters.AddWithValue("@Search", "%" + searchKeyword.Trim() + "%");

                    if (statusFilter != "All")
                        cmd.Parameters.AddWithValue("@Status", statusFilter);

                    conn.Open();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    gvEmployees.DataSource = dt;
                    gvEmployees.DataBind();
                }
            }
        }

        // UPDATED: Pass the new filter on Search
        protected void btnSearch_Click(object sender, EventArgs e)
        {
            gvEmployees.PageIndex = 0;
            BindEmployeeData(txtSearch.Text, ddlStatusFilter.SelectedValue, ddlActivityFilter.SelectedValue);
        }

        // UPDATED: Reset the new filter on Clear
        protected void btnClear_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            ddlStatusFilter.SelectedIndex = 0;
            ddlActivityFilter.SelectedIndex = 0;
            gvEmployees.PageIndex = 0;
            BindEmployeeData();
        }

        // UPDATED: Maintain the new filter across pages
        protected void gvEmployees_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvEmployees.PageIndex = e.NewPageIndex;
            BindEmployeeData(txtSearch.Text, ddlStatusFilter.SelectedValue, ddlActivityFilter.SelectedValue);
        }

        protected void gvEmployees_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            string employeeCode = e.CommandArgument.ToString();

            if (e.CommandName == "EditEmployee")
            {
                Response.Redirect($"EditEmployee.aspx?EmployeeCode={employeeCode}", false);
            }
            else if (e.CommandName == "ToggleStatus")
            {
                ToggleEmployeeStatus(employeeCode);
            }
            else if (e.CommandName == "ResetPassword")
            {
                ResetEmployeePassword(employeeCode);
            }
        }

        private void ToggleEmployeeStatus(string employeeCode)
        {
            string query = "UPDATE MST_UserMaster SET EmployeeStatus = CASE WHEN EmployeeStatus = 'Active' THEN 'Blocked' ELSE 'Active' END WHERE EmployeeCode = @EmployeeCode";
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@EmployeeCode", employeeCode);
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                LogAudit(employeeCode, "STATUS_TOGGLE", "Admin toggled user Active/Blocked status from Grid View.");
                BindEmployeeData(txtSearch.Text, ddlStatusFilter.SelectedValue, ddlActivityFilter.SelectedValue);
                ShowPopup("Success", $"Status for {employeeCode} has been successfully updated.");
            }
            catch (Exception ex)
            {
                ShowPopup("Database Error", "Unable to update status: " + ex.Message);
            }
        }

        private void ResetEmployeePassword(string employeeCode)
        {
            try
            {
                string newPlainPassword = GenerateTemporaryPassword();
                int defaultIterations = 10000;
                var hashTuple = PasswordHasher.CreateHash(newPlainPassword, defaultIterations);

                string newHash = hashTuple.Item1;
                string newSalt = hashTuple.Item2;
                int newIterations = hashTuple.Item3;

                string query = @"UPDATE MST_UserMaster 
                         SET PasswordHash = @PasswordHash, 
                             PasswordSalt = @PasswordSalt, 
                             PasswordIterations = @Iterations,
                             Password = NULL, 
                             Pass_UpdateDate = GETDATE(),
                             PassUpdatedByName = @AdminName,
                             PassUpdatedByWrk = @AdminWrk,
                             PasswordExpiry = @Expiry 
                         WHERE EmployeeCode = @EmployeeCode";

                string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@PasswordHash", newHash);
                        cmd.Parameters.AddWithValue("@PasswordSalt", newSalt);
                        cmd.Parameters.AddWithValue("@Iterations", newIterations);

                        cmd.Parameters.AddWithValue("@AdminName", Session["USERNAME"] != null ? Session["USERNAME"].ToString() : "SYSTEM_ADMIN");
                        cmd.Parameters.AddWithValue("@AdminWrk", Session["WORKMAN"] != null ? Session["WORKMAN"].ToString() : "SYSTEM_ADMIN");
                        cmd.Parameters.AddWithValue("@Expiry", DateTime.Now.AddDays(15));
                        cmd.Parameters.AddWithValue("@EmployeeCode", employeeCode);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                LogAudit(employeeCode, "PASSWORD_RESET", "Admin triggered manual password reset. Set to auto-generated temporary password.");

                string modalHtml = $@"
                    <div class='text-center' style='padding: 10px;'>
                        <i class='fa fa-check-circle fa-4x text-success' style='margin-bottom: 15px;'></i>
                        <h4 style='color: #2A3F54;'>Reset Successful</h4>
                        <p>The password for <strong>{employeeCode}</strong> has been reset.</p>
                
                        <div style='background: #f4f6f9; padding: 15px; border-radius: 5px; border: 2px dashed #1ABB9C; margin: 20px 0;'>
                            <span style='font-size: 22px; font-family: Consolas, monospace; font-weight: bold; color: #d9534f; letter-spacing: 2px;'>
                                {newPlainPassword}
                            </span>
                        </div>
                
                        <p class='text-muted small'>
                            <i class='fa fa-info-circle'></i> Please copy and share this temporary password with the user. They will be required to change it within 15 days.
                        </p>
                    </div>";

                ShowPopup("Security Notice", modalHtml);
            }
            catch (Exception ex)
            {
                ShowPopup("Reset Error", "Unable to reset password: " + ex.Message.Replace("'", ""));
            }
        }

        private void ShowPopup(string title, string body)
        {
            string safeTitle = HttpUtility.JavaScriptStringEncode(title);
            string safeBody = HttpUtility.JavaScriptStringEncode(body);

            string script = string.Format("ShowPopup('{0}', '{1}');", safeTitle, safeBody);
            ClientScript.RegisterStartupScript(this.GetType(), "Popup", script, true);
        }

        private void LogAudit(string empCode, string type, string details)
        {
            try
            {
                string logPath = Server.MapPath("~/Logs/UserEdits/");
                if (!Directory.Exists(logPath)) Directory.CreateDirectory(logPath);

                string userName = Session["USERNAME"] != null ? Session["USERNAME"].ToString() : "SYSTEM";
                string line = string.Format("{0:yyyy-MM-dd HH:mm:ss} | {1} | {2} | By: {3}{4}", DateTime.Now, type, details, userName, Environment.NewLine);

                File.AppendAllText(Path.Combine(logPath, "UserLog_" + empCode + ".txt"), line);
            }
            catch
            {
                // Silently fail if file logging is locked
            }
        }

        private string GenerateTemporaryPassword()
        {
            Random rnd = new Random();
            return "Pass@" + rnd.Next(1000, 9999).ToString();
        }
    }
}