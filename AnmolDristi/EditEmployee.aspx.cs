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
        private const string ViewStateOrgSnapshot = "ORG_DB_SNAPSHOT";
        private const string ViewStateOrgUnresolved = "ORG_UNRESOLVED";

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

        private OrgBinding[] GetOrgBindings()
        {
            return new[]
            {
                new OrgBinding("RegionId", DDL_Region),
                new OrgBinding("DivisionId", DDL_Division),
                new OrgBinding("DepartmentId", DDL_Department),
                new OrgBinding("SubDepartmentId", DDL_SubDepartment),
                new OrgBinding("CategoryId", DDL_Category),
                new OrgBinding("PlantId", DDL_Plant),
                new OrgBinding("GradeId", DDL_Grade),
                new OrgBinding("BranchId", DDL_Branch)
            };
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

                            Dictionary<string, string> snapshot = new Dictionary<string, string>();
                            Dictionary<string, bool> unresolved = new Dictionary<string, bool>();

                            SetDropdownValue(DDL_Company, reader["CompanyId"].ToString());

                            foreach (OrgBinding binding in GetOrgBindings())
                            {
                                string persisted = ReadOrgDbValue(reader, binding.Column);
                                snapshot[binding.Column] = persisted;
                                ApplyOrganizationalDropdown(binding.List, persisted, unresolved, binding.Column);
                            }

                            StoreOrgState(snapshot, unresolved);
                            CaptureOriginalValues();
                        }
                    }
                }
            }
        }

        private static string ReadOrgDbValue(SqlDataReader reader, string column)
        {
            object raw = reader[column];
            if (raw == null || raw == DBNull.Value)
                return string.Empty;
            return OrganizationalValueResolver.Normalize(Convert.ToString(raw));
        }

        private void ApplyOrganizationalDropdown(DropDownList ddl, string persisted, Dictionary<string, bool> unresolved, string column)
        {
            string value = OrganizationalValueResolver.Normalize(persisted);
            bool existsInDropdown = value.Length > 0 && ddl.Items.FindByValue(value) != null;
            bool isUnresolved = OrganizationalValueResolver.IsUnresolvedNonZero(value, existsInDropdown);

            if (existsInDropdown)
            {
                ddl.ClearSelection();
                ddl.Items.FindByValue(value).Selected = true;
            }
            else if (isUnresolved)
            {
                ListItem synthetic = new ListItem(OrganizationalValueResolver.UnresolvedItemText(value), value);
                int insertAt = ddl.Items.Count > 0 ? 1 : 0;
                ddl.Items.Insert(insertAt, synthetic);
                ddl.ClearSelection();
                ddl.Items.FindByValue(value).Selected = true;
                AppendUnresolvedCss(ddl);
            }

            unresolved[column] = isUnresolved;
        }

        private static void AppendUnresolvedCss(DropDownList ddl)
        {
            string css = ddl.CssClass ?? string.Empty;
            if (css.IndexOf(OrganizationalValueResolver.UnresolvedCssClass, StringComparison.Ordinal) < 0)
                ddl.CssClass = (css + " " + OrganizationalValueResolver.UnresolvedCssClass).Trim();
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
            Dictionary<string, string> resolvedOrg = ResolveAllOrganizationalValues();
            string changes = GetAllFieldChanges(resolvedOrg);
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
                        cmd.Parameters.AddWithValue("@RegionId", resolvedOrg["RegionId"]);
                        cmd.Parameters.AddWithValue("@DivisionId", resolvedOrg["DivisionId"]);
                        cmd.Parameters.AddWithValue("@DepartmentId", resolvedOrg["DepartmentId"]);
                        cmd.Parameters.AddWithValue("@SubDepartmentId", resolvedOrg["SubDepartmentId"]);
                        cmd.Parameters.AddWithValue("@CategoryId", resolvedOrg["CategoryId"]);
                        cmd.Parameters.AddWithValue("@PlantId", resolvedOrg["PlantId"]);
                        cmd.Parameters.AddWithValue("@GradeId", resolvedOrg["GradeId"]);
                        cmd.Parameters.AddWithValue("@BranchId", resolvedOrg["BranchId"]);
                        cmd.Parameters.AddWithValue("@DOJ", TB_DOJ.Text);
                        cmd.Parameters.AddWithValue("@Email", TB_Email.Text);
                        cmd.Parameters.AddWithValue("@Mobile", TB_Mobile.Text);
                        cmd.Parameters.AddWithValue("@Gender", RBL_Gender.SelectedItem != null ? RBL_Gender.SelectedItem.Text : "");
                        cmd.Parameters.AddWithValue("@Password", TB_Password.Text);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                RefreshOrgStateAfterSave(resolvedOrg);

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

        private Dictionary<string, string> ResolveAllOrganizationalValues()
        {
            Dictionary<string, string> snapshot = GetOrgSnapshot();
            Dictionary<string, bool> unresolved = GetOrgUnresolved();
            Dictionary<string, string> resolved = new Dictionary<string, string>();

            foreach (OrgBinding binding in GetOrgBindings())
            {
                resolved[binding.Column] = ResolveOrganizationalValue(binding, snapshot, unresolved);
            }

            return resolved;
        }

        private static string ResolveOrganizationalValue(
            OrgBinding binding,
            Dictionary<string, string> snapshot,
            Dictionary<string, bool> unresolved)
        {
            string original = snapshot.ContainsKey(binding.Column) ? snapshot[binding.Column] : string.Empty;
            bool wasUnresolved = unresolved.ContainsKey(binding.Column) && unresolved[binding.Column];
            string selected = binding.List.SelectedValue ?? string.Empty;
            bool selectedIsKnown = binding.List.Items.FindByValue(selected) != null;
            return OrganizationalValueResolver.Resolve(original, wasUnresolved, selected, selectedIsKnown);
        }

        private void RefreshOrgStateAfterSave(Dictionary<string, string> resolvedOrg)
        {
            Dictionary<string, bool> unresolved = new Dictionary<string, bool>();
            foreach (OrgBinding binding in GetOrgBindings())
            {
                string written = resolvedOrg.ContainsKey(binding.Column) ? resolvedOrg[binding.Column] : string.Empty;
                ListItem selectedItem = binding.List.SelectedItem;
                bool stillSynthetic = selectedItem != null
                    && string.Equals(OrganizationalValueResolver.Normalize(selectedItem.Value), written, StringComparison.Ordinal)
                    && (selectedItem.Text ?? string.Empty).StartsWith(OrganizationalValueResolver.UnresolvedItemTextPrefix, StringComparison.Ordinal);
                unresolved[binding.Column] = stillSynthetic;
            }
            StoreOrgState(new Dictionary<string, string>(resolvedOrg), unresolved);
        }

        private void StoreOrgState(Dictionary<string, string> snapshot, Dictionary<string, bool> unresolved)
        {
            ViewState[ViewStateOrgSnapshot] = snapshot;
            ViewState[ViewStateOrgUnresolved] = unresolved;
        }

        private Dictionary<string, string> GetOrgSnapshot()
        {
            return ViewState[ViewStateOrgSnapshot] as Dictionary<string, string> ?? new Dictionary<string, string>();
        }

        private Dictionary<string, bool> GetOrgUnresolved()
        {
            return ViewState[ViewStateOrgUnresolved] as Dictionary<string, bool> ?? new Dictionary<string, bool>();
        }

        // =========================================================
        // --- AUDIT LOGGING SYSTEM ---
        // =========================================================

        private void CaptureOriginalValues()
        {
            Dictionary<string, string> original = new Dictionary<string, string>();
            Dictionary<string, string> snapshot = GetOrgSnapshot();

            original["EmployeeName"] = TB_EmpName.Text;
            original["CompanyId"] = DDL_Company.SelectedValue;
            original["RegionId"] = OrgOriginalForCapture(snapshot, "RegionId", DDL_Region);
            original["DivisionId"] = OrgOriginalForCapture(snapshot, "DivisionId", DDL_Division);
            original["DepartmentId"] = OrgOriginalForCapture(snapshot, "DepartmentId", DDL_Department);
            original["SubDepartmentId"] = OrgOriginalForCapture(snapshot, "SubDepartmentId", DDL_SubDepartment);
            original["CategoryId"] = OrgOriginalForCapture(snapshot, "CategoryId", DDL_Category);
            original["PlantId"] = OrgOriginalForCapture(snapshot, "PlantId", DDL_Plant);
            original["GradeId"] = OrgOriginalForCapture(snapshot, "GradeId", DDL_Grade);
            original["BranchId"] = OrgOriginalForCapture(snapshot, "BranchId", DDL_Branch);
            original["DOJ"] = TB_DOJ.Text;
            original["Email"] = TB_Email.Text;
            original["Mobile"] = TB_Mobile.Text;
            original["Gender"] = RBL_Gender.SelectedItem != null ? RBL_Gender.SelectedItem.Text : "";
            original["Password"] = TB_Password.Text;

            ViewState["ORIGINAL_DATA"] = original;
        }

        private static string OrgOriginalForCapture(Dictionary<string, string> snapshot, string column, DropDownList ddl)
        {
            if (snapshot != null && snapshot.ContainsKey(column))
                return snapshot[column] ?? string.Empty;
            return ddl.SelectedValue ?? string.Empty;
        }

        private string GetAllFieldChanges(Dictionary<string, string> resolvedOrg)
        {
            if (ViewState["ORIGINAL_DATA"] == null) return "";

            Dictionary<string, string> original = (Dictionary<string, string>)ViewState["ORIGINAL_DATA"];
            List<string> changes = new List<string>();

            CompareValue(changes, original, "EmployeeName", TB_EmpName.Text);
            CompareValue(changes, original, "CompanyId", DDL_Company.SelectedValue);
            CompareValue(changes, original, "RegionId", resolvedOrg["RegionId"]);
            CompareValue(changes, original, "DivisionId", resolvedOrg["DivisionId"]);
            CompareValue(changes, original, "DepartmentId", resolvedOrg["DepartmentId"]);
            CompareValue(changes, original, "SubDepartmentId", resolvedOrg["SubDepartmentId"]);
            CompareValue(changes, original, "CategoryId", resolvedOrg["CategoryId"]);
            CompareValue(changes, original, "PlantId", resolvedOrg["PlantId"]);
            CompareValue(changes, original, "GradeId", resolvedOrg["GradeId"]);
            CompareValue(changes, original, "BranchId", resolvedOrg["BranchId"]);
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

        private sealed class OrgBinding
        {
            public readonly string Column;
            public readonly DropDownList List;

            public OrgBinding(string column, DropDownList list)
            {
                Column = column;
                List = list;
            }
        }
    }
}
