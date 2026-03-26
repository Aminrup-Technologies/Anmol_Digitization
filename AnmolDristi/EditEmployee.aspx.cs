using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class EditEmployee : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["USERID"] == null || Session["USERNAME"] == null || Session["WORKMAN"] == null)
                {
                    Response.Redirect("login.aspx", false);
                }
                else
                {
                    // 1. Load ALL master data into dropdowns immediately
                    BindAllDropdowns();

                    // 2. Check for Employee Code and load their specific details
                    string empCode = Request.QueryString["EmployeeCode"];
                    if (!string.IsNullOrEmpty(empCode))
                    {
                        ViewState["EmpCode"] = empCode;
                        LoadEmployeeDetails(empCode);
                        LoadAuditLogs(empCode);
                    }
                }
            }
        }

        private void BindAllDropdowns()
        {
            bool recordsBound;
            DatabaseHelper.BindDropDownList("SELECT Company_Description, Company_ID FROM MST_Company", DDL_Company, "Company_Description", "Company_ID", out recordsBound);
            DatabaseHelper.BindDropDownList("SELECT Region_Description, Region_ID FROM MST_Region", DDL_Region, "Region_Description", "Region_ID", out recordsBound);
            DatabaseHelper.BindDropDownList("SELECT Division_Description, Division_ID FROM MST_Division", DDL_Division, "Division_Description", "Division_ID", out recordsBound);
            DatabaseHelper.BindDropDownList("SELECT Department_Description, Department_ID FROM MST_Department", DDL_Department, "Department_Description", "Department_ID", out recordsBound);
            DatabaseHelper.BindDropDownList("SELECT SubDepartment_Description, SubDepartment_ID FROM MST_SubDepartment", DDL_SubDepartment, "SubDepartment_Description", "SubDepartment_ID", out recordsBound);
            DatabaseHelper.BindDropDownList("SELECT Category_Description, Category_ID FROM MST_Category", DDL_Category, "Category_Description", "Category_ID", out recordsBound);
            DatabaseHelper.BindDropDownList("SELECT PlantID_Description, PlantID_ID FROM MST_Plant", DDL_Plant, "PlantID_Description", "PlantID_ID", out recordsBound);
            DatabaseHelper.BindDropDownList("SELECT Grade_Description, Grade_ID FROM MST_Grade", DDL_Grade, "Grade_Description", "Grade_ID", out recordsBound);
            DatabaseHelper.BindDropDownList("SELECT Branch_Description, Branch_ID FROM MST_Branch", DDL_Branch, "Branch_Description", "Branch_ID", out recordsBound);
        }

        private void LoadEmployeeDetails(string employeeCode)
        {
            string query = "SELECT * FROM MST_UserMaster WHERE EmployeeCode = @EmployeeCode";
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeCode", employeeCode);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Populate Text Fields
                            TB_EmpCode.Text = reader["EmployeeCode"].ToString();
                            TB_EmpName.Text = reader["EmployeeName"].ToString();
                            TB_DOJ.Text = reader["DOJ"] != DBNull.Value ? Convert.ToDateTime(reader["DOJ"]).ToString("yyyy-MM-dd") : "";
                            TB_Email.Text = reader["Email"].ToString();
                            TB_Mobile.Text = reader["Mobile"].ToString();

                            string gender = reader["Gender"].ToString();
                            if (RBL_Gender.Items.FindByText(gender) != null)
                                RBL_Gender.Items.FindByText(gender).Selected = true;

                            TB_Password.Text = reader["Password"].ToString();

                            // Set Dropdown Values
                            SetDropdownValue(DDL_Company, reader["CompanyId"].ToString());
                            SetDropdownValue(DDL_Region, reader["RegionId"].ToString());
                            SetDropdownValue(DDL_Division, reader["DivisionId"].ToString());
                            SetDropdownValue(DDL_Department, reader["DepartmentId"].ToString());
                            SetDropdownValue(DDL_SubDepartment, reader["SubDepartmentId"].ToString());
                            SetDropdownValue(DDL_Category, reader["CategoryId"].ToString());
                            SetDropdownValue(DDL_Plant, reader["PlantId"].ToString());
                            SetDropdownValue(DDL_Grade, reader["GradeId"].ToString());
                            SetDropdownValue(DDL_Branch, reader["BranchId"].ToString());

                            CaptureOriginalValues();
                        }
                    }
                }
            }
        }

        private void SetDropdownValue(DropDownList ddl, string value)
        {
            if (!string.IsNullOrEmpty(value) && ddl.Items.FindByValue(value) != null)
            {
                ddl.ClearSelection();
                ddl.Items.FindByValue(value).Selected = true;
            }
        }

        protected void BtnSubmit_Click(object sender, EventArgs e)
        {
            string changes = GetAllFieldChanges();
            if (string.IsNullOrEmpty(changes))
            {
                ShowNotification("No Changes", "No data was modified.", "info");
                return;
            }

            string query = @"UPDATE MST_UserMaster SET 
                                EmployeeName = @EmployeeName, CompanyId = @CompanyId, RegionId = @RegionId, 
                                DivisionId = @DivisionId, DepartmentId = @DepartmentId, SubDepartmentId = @SubDepartmentId, 
                                CategoryId = @CategoryId, PlantId = @PlantId, GradeId = @GradeId, BranchId = @BranchId, 
                                DOJ = @DOJ, Email = @Email, Mobile = @Mobile, Gender = @Gender, Password = @Password 
                             WHERE EmployeeCode = @EmployeeCode";

            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@EmployeeCode", TB_EmpCode.Text);
                        cmd.Parameters.AddWithValue("@EmployeeName", TB_EmpName.Text);
                        cmd.Parameters.AddWithValue("@CompanyId", DDL_Company.SelectedValue);
                        cmd.Parameters.AddWithValue("@RegionId", DDL_Region.SelectedValue);
                        cmd.Parameters.AddWithValue("@DivisionId", DDL_Division.SelectedValue);
                        cmd.Parameters.AddWithValue("@DepartmentId", DDL_Department.SelectedValue);
                        cmd.Parameters.AddWithValue("@SubDepartmentId", DDL_SubDepartment.SelectedValue);
                        cmd.Parameters.AddWithValue("@CategoryId", DDL_Category.SelectedValue);
                        cmd.Parameters.AddWithValue("@PlantId", DDL_Plant.SelectedValue);
                        cmd.Parameters.AddWithValue("@GradeId", DDL_Grade.SelectedValue);
                        cmd.Parameters.AddWithValue("@BranchId", DDL_Branch.SelectedValue);
                        cmd.Parameters.AddWithValue("@DOJ", TB_DOJ.Text);
                        cmd.Parameters.AddWithValue("@Email", TB_Email.Text);
                        cmd.Parameters.AddWithValue("@Mobile", TB_Mobile.Text);
                        cmd.Parameters.AddWithValue("@Gender", RBL_Gender.SelectedItem != null ? RBL_Gender.SelectedItem.Text : "");
                        cmd.Parameters.AddWithValue("@Password", TB_Password.Text);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                // Write to log and reload the log panel
                LogAudit(TB_EmpCode.Text, "PROFILE_UPDATE", "CHANGES DETECTED:" + Environment.NewLine + changes);
                LoadAuditLogs(TB_EmpCode.Text);

                CaptureOriginalValues();
                ShowNotification("Data Success", "Employee details updated and logged successfully!", "success");
            }
            catch (Exception ex)
            {
                ShowNotification("Error", "Failed to update: " + ex.Message.Replace("'", ""), "error");
            }
        }

        protected void BtnReset_Click(object sender, EventArgs e)
        {
            string empCode = ViewState["EmpCode"] != null ? ViewState["EmpCode"].ToString() : "";
            Response.Redirect($"EditEmployee.aspx?EmployeeCode={empCode}", false);
        }

        // =========================================================
        // --- AUDIT LOGGING SYSTEM ---
        // =========================================================

        private void CaptureOriginalValues()
        {
            Dictionary<string, string> original = new Dictionary<string, string>();

            original["EmployeeName"] = TB_EmpName.Text;
            original["CompanyId"] = DDL_Company.SelectedValue;
            original["RegionId"] = DDL_Region.SelectedValue;
            original["DivisionId"] = DDL_Division.SelectedValue;
            original["DepartmentId"] = DDL_Department.SelectedValue;
            original["SubDepartmentId"] = DDL_SubDepartment.SelectedValue;
            original["CategoryId"] = DDL_Category.SelectedValue;
            original["PlantId"] = DDL_Plant.SelectedValue;
            original["GradeId"] = DDL_Grade.SelectedValue;
            original["BranchId"] = DDL_Branch.SelectedValue;
            original["DOJ"] = TB_DOJ.Text;
            original["Email"] = TB_Email.Text;
            original["Mobile"] = TB_Mobile.Text;
            original["Gender"] = RBL_Gender.SelectedItem != null ? RBL_Gender.SelectedItem.Text : "";
            original["Password"] = TB_Password.Text;

            ViewState["ORIGINAL_DATA"] = original;
        }

        private string GetAllFieldChanges()
        {
            if (ViewState["ORIGINAL_DATA"] == null) return "";

            Dictionary<string, string> original = (Dictionary<string, string>)ViewState["ORIGINAL_DATA"];
            List<string> changes = new List<string>();

            CompareValue(changes, original, "EmployeeName", TB_EmpName.Text);
            CompareValue(changes, original, "CompanyId", DDL_Company.SelectedValue);
            CompareValue(changes, original, "RegionId", DDL_Region.SelectedValue);
            CompareValue(changes, original, "DivisionId", DDL_Division.SelectedValue);
            CompareValue(changes, original, "DepartmentId", DDL_Department.SelectedValue);
            CompareValue(changes, original, "SubDepartmentId", DDL_SubDepartment.SelectedValue);
            CompareValue(changes, original, "CategoryId", DDL_Category.SelectedValue);
            CompareValue(changes, original, "PlantId", DDL_Plant.SelectedValue);
            CompareValue(changes, original, "GradeId", DDL_Grade.SelectedValue);
            CompareValue(changes, original, "BranchId", DDL_Branch.SelectedValue);
            CompareValue(changes, original, "DOJ", TB_DOJ.Text);
            CompareValue(changes, original, "Email", TB_Email.Text);
            CompareValue(changes, original, "Mobile", TB_Mobile.Text);
            CompareValue(changes, original, "Gender", RBL_Gender.SelectedItem != null ? RBL_Gender.SelectedItem.Text : "");

            string oldPass = original.ContainsKey("Password") ? original["Password"] : "";
            if (oldPass != TB_Password.Text)
            {
                changes.Add("Password: [Hidden] -> [Hidden] (Password was changed)");
            }

            return string.Join(Environment.NewLine, changes);
        }

        private void CompareValue(List<string> changes, Dictionary<string, string> original, string key, string currentValue)
        {
            string oldValue = original.ContainsKey(key) ? (original[key] ?? "").Trim() : "";
            string newValue = (currentValue ?? "").Trim();

            if (oldValue != newValue)
            {
                changes.Add(string.Format("{0}: '{1}' -> '{2}'", key, oldValue, newValue));
            }
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
            catch { /* Silently fail if file is locked */ }
        }

        private void LoadAuditLogs(string empCode)
        {
            try
            {
                string logPath = Server.MapPath("~/Logs/UserEdits/UserLog_" + empCode + ".txt");

                if (File.Exists(logPath))
                {
                    string fileContent = File.ReadAllText(logPath);
                    string pattern = @"(?=\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} \|)";
                    string[] entries = Regex.Split(fileContent, pattern);
                    var reversedEntries = entries.Where(e => !string.IsNullOrWhiteSpace(e)).Reverse();
                    string divider = "<hr style='margin: 10px 0; border-top: 1px dashed #ccc;' />";
                    string formattedContent = string.Join(divider, reversedEntries);

                    lit_AuditLogs.Text = formattedContent.Replace(Environment.NewLine, "<br/>").Replace("\n", "<br/>");
                }
                else
                {
                    lit_AuditLogs.Text = "<span class='text-muted'>No audit logs found for this employee yet. System is tracking changes from now on.</span>";
                }
            }
            catch (Exception ex)
            {
                lit_AuditLogs.Text = "<span class='text-danger'>Unable to load audit logs: " + ex.Message + "</span>";
            }
        }

        private void ShowNotification(string title, string text, string type)
        {
            string script = $@"<script type='text/javascript'>
                        new PNotify({{
                            title: '{title}',
                            text: '{text}',
                            type: '{type}',
                            styling: 'bootstrap3'
                        }});
                    </script>";
            ClientScript.RegisterStartupScript(this.GetType(), "ShowNotification", script, false);
        }
    }
}