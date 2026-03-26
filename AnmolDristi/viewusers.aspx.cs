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

        private void BindEmployeeData(string searchKeyword = "", string statusFilter = "All")
        {
            // Updated query to include Grade, Region, and Reporting Manager fields
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
                    g.Grade_Description AS GradeName
                FROM dbo.MST_UserMaster u
                -- 1. Designation joins on the Primary Key 'Id' (Because of Phase 4 mapping)
                LEFT JOIN dbo.MST_Designation desg ON u.Designation_ID = desg.Id

                -- 2. Branch joins INT to INT (No casting needed)
                LEFT JOIN dbo.MST_Branch b ON u.BranchId = b.Branch_ID

                -- 3. The rest join INT to VARCHAR (Requires CAST to prevent conversion crashes)
                LEFT JOIN dbo.MST_Department d ON CAST(u.DepartmentId AS VARCHAR(50)) = d.Department_ID
                LEFT JOIN dbo.MST_Plant p ON CAST(u.PlantId AS VARCHAR(50)) = p.PlantID_ID
                LEFT JOIN dbo.MST_Region r ON CAST(u.RegionId AS VARCHAR(50)) = r.Region_ID
                LEFT JOIN dbo.MST_Grade g ON CAST(u.GradeId AS VARCHAR(50)) = g.Grade_ID
                WHERE 1=1";

            if (!string.IsNullOrEmpty(searchKeyword))
            {
                query += " AND (u.EmployeeName LIKE @Search OR u.EmployeeCode LIKE @Search)";
            }

            if (statusFilter != "All")
            {
                query += " AND u.EmployeeStatus = @Status";
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

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            gvEmployees.PageIndex = 0;
            BindEmployeeData(txtSearch.Text, ddlStatusFilter.SelectedValue);
        }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            ddlStatusFilter.SelectedIndex = 0;
            gvEmployees.PageIndex = 0;
            BindEmployeeData();
        }

        protected void gvEmployees_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvEmployees.PageIndex = e.NewPageIndex;
            BindEmployeeData(txtSearch.Text, ddlStatusFilter.SelectedValue);
        }

        protected void gvEmployees_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            string employeeCode = e.CommandArgument.ToString();

            if (e.CommandName == "EditEmployee")
            {
                // This would likely redirect to your detailed master view
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

                // Write to Audit Log
                LogAudit(employeeCode, "STATUS_TOGGLE", "Admin toggled user Active/Blocked status from Grid View.");

                // Refresh Grid and show Modal
                BindEmployeeData(txtSearch.Text, ddlStatusFilter.SelectedValue);
                ShowPopup("Success", $"Status for {employeeCode} has been successfully updated.");
            }
            catch (Exception ex)
            {
                ShowPopup("Database Error", "Unable to update status: " + ex.Message);
            }
        }

        private void ResetEmployeePassword_OLD(string employeeCode)
        {
            try
            {
                // 1. Define the universal default password for reset users
                string defaultPassword = "Welcome@123";

                // 2. Generate the secure Hash and Salt using your existing utility
                int defaultIterations = 10000; // Fallback in case PasswordHasher.DefaultIterations isn't accessible

                // Attempt to use your native PasswordHasher from login.aspx.cs
                var hashTuple = PasswordHasher.CreateHash(defaultPassword, defaultIterations);
                string newHash = hashTuple.Item1;
                string newSalt = hashTuple.Item2;
                int newIterations = hashTuple.Item3;

                // 3. Update the Database (Nullify plain password, update Hash, and set Expiry)
                string query = @"UPDATE MST_UserMaster 
                         SET PasswordHash = @PasswordHash, 
                             PasswordSalt = @PasswordSalt, 
                             PasswordIterations = @Iterations,
                             Password = 'Welcome@123', 
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

                        // Force the user to change it soon (15 days, matching your pwdchange.aspx logic)
                        cmd.Parameters.AddWithValue("@Expiry", DateTime.Now.AddDays(15));
                        cmd.Parameters.AddWithValue("@EmployeeCode", employeeCode);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                // 4. Log the action and notify the Admin
                LogAudit(employeeCode, "PASSWORD_RESET", "Admin triggered manual password reset. Set to default.");
                ShowPopup("Password Reset Successful", $"The password for user {employeeCode} has been securely reset to '{defaultPassword}'. They will be prompted to change it within 15 days.");
            }
            catch (Exception ex)
            {
                ShowPopup("Reset Error", "Unable to reset password: " + ex.Message.Replace("'", ""));
            }
        }

        private void ResetEmployeePassword(string employeeCode)
        {
            try
            {
                // 1. Generate a secure, random temporary password
                string newPlainPassword = GenerateTemporaryPassword();

                // 2. Generate the secure Hash and Salt using your existing utility
                int defaultIterations = 10000;

                // Use your native PasswordHasher to create the secure hash
                var hashTuple = PasswordHasher.CreateHash(newPlainPassword, defaultIterations);
                string newHash = hashTuple.Item1;
                string newSalt = hashTuple.Item2;
                int newIterations = hashTuple.Item3;

                // 3. Update the Database
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

                        // Force the user to change it soon (15 days)
                        cmd.Parameters.AddWithValue("@Expiry", DateTime.Now.AddDays(15));
                        cmd.Parameters.AddWithValue("@EmployeeCode", employeeCode);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                // 4. Log the action
                LogAudit(employeeCode, "PASSWORD_RESET", "Admin triggered manual password reset. Set to auto-generated temporary password.");

                // 5. Format a beautiful HTML payload for the Bootstrap Modal
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

                // 6. Trigger the modal (Our ShowPopup method safely handles this string)
                ShowPopup("Security Notice", modalHtml);
            }
            catch (Exception ex)
            {
                ShowPopup("Reset Error", "Unable to reset password: " + ex.Message.Replace("'", ""));
            }
        }

        // --- HELPER METHODS PORTED FROM REFERENCE ---

        private void ShowPopup(string title, string body)
        {
            // Safely encode the strings for JavaScript to prevent Syntax Errors and broken HTML
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
                // Silently fail if file logging is locked, just like the reference code
            }
        }

        private string GenerateTemporaryPassword()
        {
            // Generates a random 4-digit number to append to a strong base
            // This ensures it meets your Regex: ^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$
            Random rnd = new Random();
            return "Pass@" + rnd.Next(1000, 9999).ToString();
        }
    }
}