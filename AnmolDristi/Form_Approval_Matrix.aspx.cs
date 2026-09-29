using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class Form_Approval_Matrix : BasePage
    {
        private static string formName = string.Empty;
        private static string documentNumber = string.Empty;
        private static string documentName = string.Empty;
        private static DateTime issueDate = DateTime.MinValue;
        private static string issueNo = string.Empty;
        private static DateTime revisionDate = DateTime.MinValue;
        private static string revNo = string.Empty;
        private static int frequency = 0;

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
                    lbl_docname.Text = "Form Approval Matrix";
                    FormBinder();
                    PlantBinder();
                    DepartmentBinder();
                    SubDepartmentBinder();

                    // REMOVED: BindGridView(); 
                    // By not binding the grid on initial load, the page will render instantly!
                }
            }
        }

        private void FormBinder()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            string query = "SELECT [DocumentName], [FormID] FROM [MST_FormsMaster] WITH(NOLOCK)";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            sda.Fill(dt);

                            // Bind directly to the ListBox
                            DDL_FormName.DataSource = dt;
                            DDL_FormName.DataTextField = "DocumentName";
                            DDL_FormName.DataValueField = "FormID";
                            DDL_FormName.DataBind();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorNotification("Error binding forms: " + ex.Message);
            }

            // Handle empty data manually since we aren't using the DatabaseHelper here
            if (DDL_FormName.Items.Count == 0)
            {
                DDL_FormName.Items.Insert(0, new ListItem("-- No Records Found --", "0"));
            }
        }

        protected void DDL_FormName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_FormName.SelectedIndex != 0)
            {
                string selectedFormValue = DDL_FormName.SelectedValue.ToString();
                lbl_DDL_FormName_Value.Text = selectedFormValue;
                PlantBinder(selectedFormValue);
                LoadFormMasterData(selectedFormValue);
                //BindGridViewbyForm(selectedFormValue);
                BindGridViewByFilters();
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_Plant);

                string DDL_Form_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'Invalid Selection!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowFormInvalidErrorNotification", DDL_Form_Error_script, false);
            }
        }

        private void LoadFormMasterData(string selectedFormValue)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT  FormName, DocumentNumber, DocumentName, IssueDate, IssueNo, RevisionDate, RevNo, Frequency FROM [MST_FormsMaster] WHERE FormId = @FormId";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@FormId", selectedFormValue); // Replace with actual value


                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);

                            // Check if any data was returned
                            if (dt.Rows.Count > 0)
                            {
                                // Example of storing data in variables
                                DataRow row = dt.Rows[0]; // Assuming only one result

                                formName = row["FormName"].ToString();
                                documentNumber = row["DocumentNumber"].ToString();
                                documentName = row["DocumentName"].ToString();
                                issueDate = Convert.ToDateTime(row["IssueDate"]);
                                issueNo = row["IssueNo"].ToString();
                                revisionDate = Convert.ToDateTime(row["RevisionDate"]);
                                revNo = row["RevNo"].ToString();
                                frequency = Convert.ToInt32(row["Frequency"]);

                            }
                            else
                            {
                                // Handle case where no records are found
                                ClientScript.RegisterStartupScript(this.GetType(), "ShowErrorNotification", "<script type='text/javascript'>alert('No form data found');</script>", false);
                            }

                            connection.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message.Replace("'", "\\'"); // Escape single quotes in the error message
                string errorScript = "<script type='text/javascript'>\n" +
                                     $"new PNotify({{\n" +
                                     "    title: 'Error',\n" +
                                     $"    text: '{errorMessage}',\n" +
                                     "    type: 'error',\n" +
                                     "    styling: 'bootstrap3'\n" +
                                     "});\n" +
                                     "</script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowErrorNotification", errorScript, false);
            }
        }

        private void PlantBinder()
        {
            string query = "SELECT plant_id, CONCAT(plant_name, ' [', sap_code, ']') AS plant_name FROM MST_PlantDetails WITH(NOLOCK) WHERE delete_status = 0";
            string textField = "plant_name";
            string valueField = "plant_id";

            bool recordsBound;

            // FIX: Removed the 'null' parameter to call the simpler overload
            DatabaseHelper.BindDropDownList(query, DDL_Plant, textField, valueField, out recordsBound);

            if (!recordsBound)
            {
                ShowErrorNotification("No Plants Found!");
            }
        }

        private void PlantBinder(string selectedFormValue)
        {
            // Construct the SQL query with parameters
            string query = "SELECT plant_id, CONCAT(plant_name, '[', sap_code, ']') AS plant_name FROM MST_PlantDetails";
            string textField = "plant_name";
            string valueField = "plant_id";

            // Create SQL parameters for plant_id and line_id
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@FormID", selectedFormValue)
            };

            // Call the BindDropDownList method with parameters
            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_Plant, textField, valueField, parameters, out recordsBound);

            // Check if any records were bound
            if (!recordsBound)
            {
                string DDL_Plant_Error_script = @"<script type='text/javascript'>
                    new PNotify({
                        title: 'Error',
                        text: 'Invalid Selection!',
                        type: 'error',
                        styling: 'bootstrap3'
                    });
                </script>";

                // RegisterStartupScript adds the JavaScript code to the page
                ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantBinderErrorNotification", DDL_Plant_Error_script, false);
            }
        }
        protected void DDL_Plant_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Plant.SelectedIndex != 0)
            {
                string selectedFormValue = DDL_FormName.SelectedValue.ToString();
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();

                lbl_DDL_Plant_Value.Text = selectedPlantValue;
                //BindGridViewbyFormPlant(selectedFormValue, selectedPlantValue);
                BindGridViewByFilters();
                PlantLinesBinder(selectedFormValue, selectedPlantValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_Department);
                string DDL_Plant_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'Invalid Selection!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantBinderErrorNotification", DDL_Plant_Error_script, false);
            }
        }

        private void DepartmentBinder()
        {
            // Construct the SQL query with parameters
            string query = "SELECT Department_Description, Department_ID FROM MST_Department ";
            string textField = "Department_Description"; // Assuming this is the correct field for displaying in the DropDownList
            string valueField = "Department_ID"; // Assuming this is the correct field for storing in the DropDownList

            // Create SQL parameters for plant_id and line_id
            //SqlParameter[] parameters = new SqlParameter[]
            //{
            //    new SqlParameter("@FormID", selectedFormValue),
            //    new SqlParameter("@PlantID", selectedPlantValue)
            //};

            bool recordsBound;

            DatabaseHelper.BindDropDownList(query, DDL_Department, textField, valueField, out recordsBound);

            // Check if any records were bound
            if (!recordsBound)
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_Department);
                string DDL_Department_Error_script = @"<script type='text/javascript'>
                    new PNotify({
                        title: 'Error',
                        text: 'An error occurred!',
                        type: 'error',
                        styling: 'bootstrap3'
                    });
                </script>";

                // RegisterStartupScript adds the JavaScript code to the page
                ClientScript.RegisterStartupScript(this.GetType(), "ShowDepartmentBinderErrorNotification", DDL_Department_Error_script, false);
            }
        }
        protected void DDL_Department_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Department.SelectedIndex != 0)
            {
                string selectedFormValue = DDL_FormName.SelectedValue.ToString();
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                string selectedDepartmentValue = DDL_Department.SelectedValue.ToString();

                lbl_DDL_Department_Value.Text = selectedDepartmentValue;
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_SubDepartment);

                string DDL_Department_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'Invalid Selection!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowDepartmentInvalidErrorNotification", DDL_Department_Error_script, false);
            }
        }

        private void SubDepartmentBinder()
        {
            // Construct the SQL query with parameters
            string query = "SELECT  SubDepartment_Description, SubDepartment_ID FROM MST_SubDepartment ";
            string textField = "SubDepartment_Description"; // Assuming this is the correct field for displaying in the DropDownList
            string valueField = "SubDepartment_ID"; // Assuming this is the correct field for storing in the DropDownList

            // Create SQL parameters for plant_id and line_id
            //SqlParameter[] parameters = new SqlParameter[]
            //{
            //    new SqlParameter("@FormID", selectedFormValue),
            //    new SqlParameter("@PlantID", selectedPlantValue),
            //    new SqlParameter("@DepartmentID", selectedDepartmentValue)
            //};

            // Call the BindDropDownList method with parameters
            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_Department, textField, valueField, out recordsBound);

            // Check if any records were bound
            if (!recordsBound)
            {
                string SubDepartment_Error_script = @"<script type='text/javascript'>
                    new PNotify({
                        title: 'Error',
                        text: 'Invalid Selection!',
                        type: 'error',
                        styling: 'bootstrap3'
                    });
                </script>";

                // RegisterStartupScript adds the JavaScript code to the page
                ClientScript.RegisterStartupScript(this.GetType(), "ShowSubDepartmentBinderErrorNotification", SubDepartment_Error_script, false);
            }
        }
        protected void DDL_SubDepartment_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_SubDepartment.SelectedIndex != 0)
            {
                string selectedFormValue = DDL_FormName.SelectedValue.ToString();
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                string selectedDepartmentValue = DDL_Department.SelectedValue.ToString();
                string selectedSubDepartmentValue = DDL_SubDepartment.SelectedValue.ToString();

                lbl_DDL_SubDepartment_Value.Text = selectedSubDepartmentValue;
                //PlantLinesBinder(selectedFormValue, selectedPlantValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_Line);

                string DDL_SubDepartment_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'Invalid Selection!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowSubDepartmentInvalidErrorNotification", DDL_SubDepartment_Error_script, false);
            }
        }

        private void PlantLinesBinder(string selectedFormValue, string selectedPlantValue)
        {
            string query = "SELECT line_name, line_id FROM MST_Plant_Lines WHERE plant_id = @SelectedPlantValue";
            string textField = "line_name";
            string valueField = "line_id";

            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_Line, textField, valueField, new SqlParameter("@SelectedPlantValue", selectedPlantValue), out recordsBound);

            if (!recordsBound)
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_Line);

                string PlantLinesBinder_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'An error occurred!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantLinesBinderErrorNotification", PlantLinesBinder_Error_script, false);
            }
        }
        protected void DDL_Line_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Line.SelectedIndex != 0)
            {
                string selectedFormValue = DDL_FormName.SelectedValue.ToString();
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                string selectedPlantLineValue = DDL_Line.SelectedValue.ToString();

                //BindGridViewbyFormPlantLine(selectedFormValue, selectedPlantValue, selectedPlantLineValue);
                BindGridViewByFilters();
            }
            else
            {

                string DDL_Line_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'Invalid Selection!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantInvalidErrorNotification", DDL_Line_Error_script, false);
            }

        }

        protected void BtnSubmit_Click_OLD(object sender, EventArgs e)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            int formId = Convert.ToInt32(DDL_FormName.SelectedValue);
            int plantName = Convert.ToInt32(DDL_Plant.SelectedValue);
            string departmentValue = DDL_Department.SelectedValue;
            string subDepartmentValue = DDL_SubDepartment.SelectedValue;
            int lineId = Convert.ToInt32(DDL_Line.SelectedValue);
            string app1 = TB_App1.Text;
            string app2 = TB_App2.Text;
            string app3 = TB_App3.Text;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "INSERT INTO [MST_FormsApprovalMatrix] ([plant_id] ,[line_id] ,[Department_ID]  ,[SubDepartment_ID]  ,[FormID]  ,[FormName] ,[DocumentNumber]  ,[DocumentName]  ,[IssueDate]  ,[IssueNo]  ,[RevisionDate] " +
                                                                            ",[RevNo]  ,[Frequency] ,[Approver1EmployeeCode]  ,[Approver2EmployeeCode]   ,[DottedLineApproverEmployeeCode]) " +

                                    "VALUES (@plant_id ,@line_id ,@Department_ID  ,@SubDepartment_ID  ,@FormID  ,@FormName ,@DocumentNumber  ,@DocumentName  ,@IssueDate  ,@IssueNo  ,@RevisionDate  ,@RevNo  ,@Frequency ," +
                                                "@Approver1EmployeeCode  , @Approver2EmployeeCode   ,@DottedLineApproverEmployeeCode )";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {

                        // Add parameters
                        command.Parameters.AddWithValue("@plant_id", (object)plantName ?? DBNull.Value);
                        command.Parameters.AddWithValue("@line_id", (object)lineId ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Department_ID", (object)departmentValue ?? DBNull.Value);
                        command.Parameters.AddWithValue("@SubDepartment_ID", (object)subDepartmentValue ?? DBNull.Value);
                        command.Parameters.AddWithValue("@FormID", (object)formId ?? DBNull.Value);
                        command.Parameters.AddWithValue("@FormName", (object)formName ?? DBNull.Value);
                        command.Parameters.AddWithValue("@DocumentNumber", (object)documentNumber ?? DBNull.Value);
                        command.Parameters.AddWithValue("@DocumentName", (object)documentName ?? DBNull.Value);
                        command.Parameters.AddWithValue("@IssueDate", (object)issueDate ?? DBNull.Value);
                        command.Parameters.AddWithValue("@IssueNo", (object)issueNo ?? DBNull.Value);
                        command.Parameters.AddWithValue("@RevisionDate", (object)revisionDate ?? DBNull.Value);
                        command.Parameters.AddWithValue("@RevNo", (object)revNo ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Frequency", (object)frequency ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Approver1EmployeeCode", (object)app1 ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Approver2EmployeeCode", (object)app2 ?? DBNull.Value);
                        command.Parameters.AddWithValue("@DottedLineApproverEmployeeCode", (object)app3 ?? DBNull.Value);


                        // Execute the query
                        command.ExecuteNonQuery();
                        MakeInputsReadOnly();
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message.Replace("'", "\\'"); // Escape single quotes in the error message
                string errorScript = "<script type='text/javascript'>\n" +
                                     $"new PNotify({{\n" +
                                     "    title: 'Error',\n" +
                                     $"    text: '{errorMessage}',\n" +
                                     "    type: 'error',\n" +
                                     "    styling: 'bootstrap3'\n" +
                                     "});\n" +
                                     "</script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowErrorNotification", errorScript, false);
            }
        }

        protected void BtnSubmit_Click(object sender, EventArgs e)
        {
            string app1 = TB_App1.Text.Trim();
            string app2 = TB_App2.Text.Trim();
            string app3 = TB_App3.Text.Trim();

            // --- 1. Validate all entered employees before doing anything ---
            string validationError;
            if (!ValidateEmployeeIsActive(app1, out validationError) ||
                !ValidateEmployeeIsActive(app2, out validationError) ||
                !ValidateEmployeeIsActive(app3, out validationError))
            {
                ShowErrorNotification(validationError);
                return;
            }

            // --- 2. Check if at least one form is selected ---
            if (DDL_FormName.GetSelectedIndices().Length == 0)
            {
                ShowErrorNotification("Please select at least one Form.");
                return;
            }

            // --- 3. PROCEED WITH BULK SAVE ---
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            int plantId = Convert.ToInt32(DDL_Plant.SelectedValue);

            int lineId = 0;
            if (!string.IsNullOrEmpty(DDL_Line.SelectedValue) && DDL_Line.SelectedValue != "0")
                int.TryParse(DDL_Line.SelectedValue, out lineId);

            string departmentValue = DDL_Department.SelectedValue;
            string subDepartmentValue = DDL_SubDepartment.SelectedValue;
            string userId = Session["USERID"] != null ? Session["USERID"].ToString() : "System";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Loop through every item in the Form ListBox
                    foreach (ListItem item in DDL_FormName.Items)
                    {
                        // Only process the ones the user highlighted
                        if (item.Selected)
                        {
                            int formId = Convert.ToInt32(item.Value);

                            using (SqlCommand command = new SqlCommand("sp_BulkUpsert_FormApproval_WithHistory", connection))
                            {
                                command.CommandType = CommandType.StoredProcedure;
                                command.Parameters.AddWithValue("@plant_id", plantId);
                                command.Parameters.AddWithValue("@line_id", lineId); // Passes 0 if bulk across lines
                                command.Parameters.AddWithValue("@FormID", formId);  // Passes specific FormID
                                command.Parameters.AddWithValue("@Department_ID", string.IsNullOrWhiteSpace(departmentValue) || departmentValue == "0" ? (object)DBNull.Value : departmentValue);
                                command.Parameters.AddWithValue("@SubDepartment_ID", string.IsNullOrWhiteSpace(subDepartmentValue) || subDepartmentValue == "0" ? (object)DBNull.Value : subDepartmentValue);
                                command.Parameters.AddWithValue("@Approver1EmployeeCode", string.IsNullOrWhiteSpace(app1) ? (object)DBNull.Value : app1);
                                command.Parameters.AddWithValue("@Approver2EmployeeCode", string.IsNullOrWhiteSpace(app2) ? (object)DBNull.Value : app2);
                                command.Parameters.AddWithValue("@DottedLineApproverEmployeeCode", string.IsNullOrWhiteSpace(app3) ? (object)DBNull.Value : app3);
                                command.Parameters.AddWithValue("@UserId", userId);

                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }

                MakeInputsReadOnly();
                BindGridViewByFilters(); // Reload grid to show new configurations
            }
            catch (Exception ex)
            {
                ShowErrorNotification(ex.Message);
            }
        }

        protected void BtnSubmit_Click_OLD18Mat26(object sender, EventArgs e)
        {
            string app1 = TB_App1.Text.Trim();
            string app2 = TB_App2.Text.Trim();
            string app3 = TB_App3.Text.Trim();

            // --- 1. THE CHECKPOINT: Validate all entered employees before doing anything ---
            string validationError;
            if (!ValidateEmployeeIsActive(app1, out validationError) ||
                !ValidateEmployeeIsActive(app2, out validationError) ||
                !ValidateEmployeeIsActive(app3, out validationError))
            {
                // Abort the save and show the error to the Admin
                string errorScript = $"<script type='text/javascript'>new PNotify({{ title: 'Validation Failed', text: '{validationError.Replace("'", "\\'")}', type: 'error', styling: 'bootstrap3' }});</script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowValidationError", errorScript, false);
                return;
            }

            // --- 2. PROCEED WITH SAVE IF VALID ---
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            int formId = Convert.ToInt32(DDL_FormName.SelectedValue);
            int plantId = Convert.ToInt32(DDL_Plant.SelectedValue);

            int lineId = 0;
            if (!string.IsNullOrEmpty(DDL_Line.SelectedValue)) int.TryParse(DDL_Line.SelectedValue, out lineId);

            string departmentValue = DDL_Department.SelectedValue;
            string subDepartmentValue = DDL_SubDepartment.SelectedValue;
            string userId = Session["USERID"] != null ? Session["USERID"].ToString() : "System";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand("sp_BulkUpsert_FormApproval_WithHistory", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@plant_id", plantId);
                        command.Parameters.AddWithValue("@line_id", lineId);
                        command.Parameters.AddWithValue("@FormID", formId);
                        command.Parameters.AddWithValue("@Department_ID", departmentValue == "0" ? (object)DBNull.Value : departmentValue);
                        command.Parameters.AddWithValue("@SubDepartment_ID", subDepartmentValue == "0" ? (object)DBNull.Value : subDepartmentValue);
                        command.Parameters.AddWithValue("@Approver1EmployeeCode", app1);
                        command.Parameters.AddWithValue("@Approver2EmployeeCode", string.IsNullOrWhiteSpace(app2) ? (object)DBNull.Value : app2);
                        command.Parameters.AddWithValue("@DottedLineApproverEmployeeCode", string.IsNullOrWhiteSpace(app3) ? (object)DBNull.Value : app3);
                        command.Parameters.AddWithValue("@UserId", userId);

                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }

                MakeInputsReadOnly();
                BindGridViewByFilters();
            }
            catch (Exception ex)
            {
                string errorScript = $"<script type='text/javascript'>new PNotify({{ title: 'Database Error', text: '{ex.Message.Replace("'", "\\'")}', type: 'error', styling: 'bootstrap3' }});</script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowErrorNotification", errorScript, false);
            }
        }
        private void MakeInputsReadOnly()
        {
            DDL_FormName.Enabled = false;
            DDL_Plant.Enabled = false;
            DDL_Department.Enabled = false;
            DDL_SubDepartment.Enabled = false;
            DDL_Line.Enabled = false;
            TB_App1.ReadOnly = true;
            TB_App2.ReadOnly = true;
            TB_App3.ReadOnly = true;

            BtnSubmit.Enabled = false;
            BtnSubmit.Text = "SAVED";
            BtnSubmit.CssClass = "btn btn-sm btn-success";

            string Data_SuccessScript = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Data Success',
                                text: 'Recorded Successfully!!',
                                type: 'success',
                                styling: 'bootstrap3'
                            });
                        </script>";

            // RegisterStartupScript adds the JavaScript code to the page
            ClientScript.RegisterStartupScript(this.GetType(), "ShowDataSuccessNotification", Data_SuccessScript, false);
        }

        protected void BtnReset_Click(object sender, EventArgs e)
        {
            Response.Redirect("Form_Approval_Matrix.aspx");
        }


        protected void TB_App1_TextChanged(object sender, EventArgs e)
        {
            // Get Employee Name for Approver 1
            string employeeCode = TB_App1.Text;
            string employeeName = GetEmployeeNameFromDatabase(employeeCode);
            Lbl_EmployeeName1.Text = string.IsNullOrEmpty(employeeName) ? "Employee not found." : "Employee Name: " + employeeName;
        }
        protected void TB_App2_TextChanged(object sender, EventArgs e)
        {
            // Get Employee Name for Approver 2
            string employeeCode = TB_App2.Text;
            string employeeName = GetEmployeeNameFromDatabase(employeeCode);
            Lbl_EmployeeName2.Text = string.IsNullOrEmpty(employeeName) ? "Employee not found." : "Employee Name: " + employeeName;
        }
        protected void TB_App3_TextChanged(object sender, EventArgs e)
        {
            // Get Employee Name for Approver 3
            string employeeCode = TB_App3.Text;
            string employeeName = GetEmployeeNameFromDatabase(employeeCode);
            Lbl_EmployeeName3.Text = string.IsNullOrEmpty(employeeName) ? "Employee not found." : "Employee Name: " + employeeName;
        }
        private string GetEmployeeNameFromDatabase(string employeeCode)
        {
            string employeeName = string.Empty;

            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT EmployeeName FROM [MST_UserMaster] WHERE EmployeeCode = @EmployeeCode";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeCode", employeeCode);
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        employeeName = result.ToString();
                    }
                }
            }
            return employeeName;
        }

        private void BindGridViewbyForm(string FormID)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            // Fetch data from the database and bind it to the GridView
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT * FROM MST_FormsApprovalMatrix WHERE FormID = @FormID", conn))
                {
                    cmd.Parameters.AddWithValue("@FormID", FormID); // Ensure parameter name matches query

                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        GV_FormsApprovalMatrix.DataSource = dt;
                        GV_FormsApprovalMatrix.DataBind();
                    }
                }
            }
        }

        private void BindGridViewbyFormPlant(string FormID, string PlantID)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            // Fetch data from the database and bind it to the GridView
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT * FROM MST_FormsApprovalMatrix WHERE FormID = @FormID and plant_id = @plant_id", conn))
                {
                    cmd.Parameters.AddWithValue("@FormID", FormID); // Ensure parameter name matches query
                    cmd.Parameters.AddWithValue("@plant_id", PlantID);
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        GV_FormsApprovalMatrix.DataSource = dt;
                        GV_FormsApprovalMatrix.DataBind();
                    }
                }
            }
        }

        private void BindGridViewbyFormPlantLine(string FormID, string PlantID, string LineID)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            // Fetch data from the database and bind it to the GridView
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT * FROM MST_FormsApprovalMatrix WHERE FormID = @FormID and plant_id = @plant_id and line_id=@line_id", conn))
                {
                    cmd.Parameters.AddWithValue("@FormID", FormID); // Ensure parameter name matches query
                    cmd.Parameters.AddWithValue("@plant_id", PlantID);
                    cmd.Parameters.AddWithValue("@line_id", LineID);
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        GV_FormsApprovalMatrix.DataSource = dt;
                        GV_FormsApprovalMatrix.DataBind();
                    }
                }
            }
        }

        protected void GV_FormsApprovalMatrix_RowEditing(object sender, GridViewEditEventArgs e)
        {
            // Set the row to edit mode and bind the GridView to display the editable fields
            GV_FormsApprovalMatrix.EditIndex = e.NewEditIndex;
            BindGridViewByFilters();
        }

        protected void GV_FormsApprovalMatrix_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            // Retrieve updated values from the GridView row
            int id = Convert.ToInt32(((Label)GV_FormsApprovalMatrix.Rows[e.RowIndex].FindControl("Lbl_Id")).Text);
            string plantId = ((TextBox)GV_FormsApprovalMatrix.Rows[e.RowIndex].FindControl("Txt_PlantID")).Text;
            string lineId = ((TextBox)GV_FormsApprovalMatrix.Rows[e.RowIndex].FindControl("Txt_LineID")).Text;
            string departmentId = ((TextBox)GV_FormsApprovalMatrix.Rows[e.RowIndex].FindControl("Txt_DepartmentID")).Text;
            string subDepartmentId = ((TextBox)GV_FormsApprovalMatrix.Rows[e.RowIndex].FindControl("Txt_SubDepartmentID")).Text;
            string approver1 = ((TextBox)GV_FormsApprovalMatrix.Rows[e.RowIndex].FindControl("Txt_Approver1")).Text;
            string approver2 = ((TextBox)GV_FormsApprovalMatrix.Rows[e.RowIndex].FindControl("Txt_Approver2")).Text;
            string dottedLineApprover = ((TextBox)GV_FormsApprovalMatrix.Rows[e.RowIndex].FindControl("Txt_DottedLineApprover")).Text;

            // Fetch the user making the update (to store in History table)
            string updatedBy = Session["USERID"] != null ? Session["USERID"].ToString() : "System";

            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_Update_FormApproval_WithHistory", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Add parameters, safely passing DBNull for empty textboxes
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@plant_id", string.IsNullOrWhiteSpace(plantId) ? (object)DBNull.Value : plantId);
                        cmd.Parameters.AddWithValue("@line_id", string.IsNullOrWhiteSpace(lineId) ? (object)DBNull.Value : lineId);
                        cmd.Parameters.AddWithValue("@Department_ID", string.IsNullOrWhiteSpace(departmentId) ? (object)DBNull.Value : departmentId);
                        cmd.Parameters.AddWithValue("@SubDepartment_ID", string.IsNullOrWhiteSpace(subDepartmentId) ? (object)DBNull.Value : subDepartmentId);
                        cmd.Parameters.AddWithValue("@Approver1EmployeeCode", string.IsNullOrWhiteSpace(approver1) ? (object)DBNull.Value : approver1);
                        cmd.Parameters.AddWithValue("@Approver2EmployeeCode", string.IsNullOrWhiteSpace(approver2) ? (object)DBNull.Value : approver2);
                        cmd.Parameters.AddWithValue("@DottedLineApproverEmployeeCode", string.IsNullOrWhiteSpace(dottedLineApprover) ? (object)DBNull.Value : dottedLineApprover);
                        cmd.Parameters.AddWithValue("@UpdatedBy", updatedBy);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                // Reset the GridView edit index and rebind data
                GV_FormsApprovalMatrix.EditIndex = -1;
                BindGridViewByFilters();

                // Optional: Show success alert
                ClientScript.RegisterStartupScript(this.GetType(), "UpdateSuccess", "<script>new PNotify({ title: 'Success', text: 'Matrix Updated and History Captured.', type: 'success', styling: 'bootstrap3' });</script>", false);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message.Replace("'", "\\'");
                string errorScript = $"<script>new PNotify({{ title: 'Error', text: '{errorMessage}', type: 'error', styling: 'bootstrap3' }});</script>";
                ClientScript.RegisterStartupScript(this.GetType(), "UpdateError", errorScript, false);
            }
        }

        protected void GV_FormsApprovalMatrix_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            // Cancel edit mode and reset the GridView edit index
            GV_FormsApprovalMatrix.EditIndex = -1;
            BindGridViewByFilters();
        }

        protected void GV_FormsApprovalMatrix_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            // Additional commands can be handled here, if needed
        }

        // --- NEW VALIDATION HELPER ---
        private bool ValidateEmployeeIsActive(string employeeCode, out string errorMessage)
        {
            errorMessage = "";
            if (string.IsNullOrWhiteSpace(employeeCode)) return true; // Empty is valid for optional approvers

            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT EmployeeName, EmployeeStatus FROM MST_UserMaster WITH(NOLOCK) WHERE EmployeeCode = @EmployeeCode";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeCode", employeeCode);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string status = reader["EmployeeStatus"].ToString();
                            string name = reader["EmployeeName"].ToString();

                            if (status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                            else
                            {
                                errorMessage = $"Employee {employeeCode} ({name}) is currently {status}. You cannot assign inactive employees.";
                                return false;
                            }
                        }
                        else
                        {
                            errorMessage = $"Employee Code {employeeCode} does not exist in the system.";
                            return false;
                        }
                    }
                }
            }
        }

        private void BindGridView()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = GetBaseGridQuery();

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        GV_FormsApprovalMatrix.DataSource = dt;
                        GV_FormsApprovalMatrix.DataBind();
                    }
                }
            }
        }

        private void BindGridViewByFilters_OLD()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            string formId = DDL_FormName.SelectedValue;
            string plantId = DDL_Plant.SelectedValue;
            string lineId = DDL_Line.SelectedValue;
            string departmentId = DDL_Department.SelectedValue;
            string subDepartmentId = DDL_SubDepartment.SelectedValue;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = GetBaseGridQuery();

                // Add conditions dynamically
                if (!string.IsNullOrEmpty(formId) && formId != "0") query += " AND fam.FormID = @FormID";
                if (!string.IsNullOrEmpty(plantId) && plantId != "0") query += " AND fam.plant_id = @plant_id";
                if (!string.IsNullOrEmpty(lineId) && lineId != "0") query += " AND fam.line_id = @line_id";
                if (!string.IsNullOrEmpty(departmentId) && departmentId != "0") query += " AND fam.Department_ID = @Department_ID";
                if (!string.IsNullOrEmpty(subDepartmentId) && subDepartmentId != "0") query += " AND fam.SubDepartment_ID = @SubDepartment_ID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(formId) && formId != "0") cmd.Parameters.AddWithValue("@FormID", formId);
                    if (!string.IsNullOrEmpty(plantId) && plantId != "0") cmd.Parameters.AddWithValue("@plant_id", plantId);
                    if (!string.IsNullOrEmpty(lineId) && lineId != "0") cmd.Parameters.AddWithValue("@line_id", lineId);
                    if (!string.IsNullOrEmpty(departmentId) && departmentId != "0") cmd.Parameters.AddWithValue("@Department_ID", departmentId);
                    if (!string.IsNullOrEmpty(subDepartmentId) && subDepartmentId != "0") cmd.Parameters.AddWithValue("@SubDepartment_ID", subDepartmentId);

                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        GV_FormsApprovalMatrix.DataSource = dt;
                        GV_FormsApprovalMatrix.DataBind();
                    }
                }
            }
        }

        protected void GV_FormsApprovalMatrix_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            // Set the new page index and rebind the grid
            GV_FormsApprovalMatrix.PageIndex = e.NewPageIndex;
            BindGridViewByFilters();
        }

        // Helper method to keep your SQL clean and reusable!

        private string GetBaseGridQuery()
        {
            return @"
                -- CTE to calculate the latest session for ALL users exactly ONCE
                WITH LatestUserSessions AS (
                    SELECT EmployeeCode, LoginTime, IsActive,
                           ROW_NUMBER() OVER(PARTITION BY EmployeeCode ORDER BY LoginTime DESC) as RowNum
                    FROM ActiveUserSession WITH(NOLOCK)
                )
                SELECT 
                    fam.*,
            
                    fm.DocumentName AS FormDesc,
                    p.plant_name AS PlantName,
                    l.line_name AS LineName,
                    d.Department_Description AS DeptName,
                    sd.SubDepartment_Description AS SubDeptName,

                    u1.EmployeeName AS App1Name, u1.EmployeeStatus AS App1Status,
                    s1.LoginTime AS App1Login, s1.IsActive AS App1Online,

                    u2.EmployeeName AS App2Name, u2.EmployeeStatus AS App2Status,
                    s2.LoginTime AS App2Login, s2.IsActive AS App2Online,

                    u3.EmployeeName AS App3Name, u3.EmployeeStatus AS App3Status,
                    s3.LoginTime AS App3Login, s3.IsActive AS App3Online

                FROM MST_FormsApprovalMatrix fam WITH(NOLOCK)
        
                LEFT JOIN MST_FormsMaster fm WITH(NOLOCK) ON fam.FormID = fm.FormID
                LEFT JOIN MST_PlantDetails p WITH(NOLOCK) ON fam.plant_id = p.plant_id
                LEFT JOIN MST_Plant_Lines l WITH(NOLOCK) ON fam.line_id = l.line_id
                LEFT JOIN MST_Department d WITH(NOLOCK) ON fam.Department_ID = d.Department_ID
                LEFT JOIN MST_SubDepartment sd WITH(NOLOCK) ON fam.SubDepartment_ID = sd.SubDepartment_ID

                -- Join Approver 1 against the pre-calculated CTE
                LEFT JOIN MST_UserMaster u1 WITH(NOLOCK) ON fam.Approver1EmployeeCode = u1.EmployeeCode
                LEFT JOIN LatestUserSessions s1 ON u1.EmployeeCode = s1.EmployeeCode AND s1.RowNum = 1

                -- Join Approver 2 against the pre-calculated CTE
                LEFT JOIN MST_UserMaster u2 WITH(NOLOCK) ON fam.Approver2EmployeeCode = u2.EmployeeCode
                LEFT JOIN LatestUserSessions s2 ON u2.EmployeeCode = s2.EmployeeCode AND s2.RowNum = 1

                -- Join Approver 3 against the pre-calculated CTE
                LEFT JOIN MST_UserMaster u3 WITH(NOLOCK) ON fam.DottedLineApproverEmployeeCode = u3.EmployeeCode
                LEFT JOIN LatestUserSessions s3 ON u3.EmployeeCode = s3.EmployeeCode AND s3.RowNum = 1

                WHERE fam.Active = 1";
        }

        private string GetBaseGridQuery_OLD()
        {
            return @"
                SELECT 
                    fam.*,
                    
                    -- Fetching the descriptive names for the UI
                    fm.DocumentName AS FormDesc,
                    p.plant_name AS PlantName,
                    l.line_name AS LineName,
                    d.Department_Description AS DeptName,
                    sd.SubDepartment_Description AS SubDeptName,

                    -- Approver 1 Data
                    u1.EmployeeName AS App1Name, u1.EmployeeStatus AS App1Status,
                    s1.LoginTime AS App1Login, s1.IsActive AS App1Online,
        
                    -- Approver 2 Data
                    u2.EmployeeName AS App2Name, u2.EmployeeStatus AS App2Status,
                    s2.LoginTime AS App2Login, s2.IsActive AS App2Online,
        
                    -- Approver 3 Data
                    u3.EmployeeName AS App3Name, u3.EmployeeStatus AS App3Status,
                    s3.LoginTime AS App3Login, s3.IsActive AS App3Online

                FROM MST_FormsApprovalMatrix fam WITH(NOLOCK)
                
                -- Master Data Joins (Solves the UI crashing bug)
                LEFT JOIN MST_FormsMaster fm WITH(NOLOCK) ON fam.FormID = fm.FormID
                LEFT JOIN MST_PlantDetails p WITH(NOLOCK) ON fam.plant_id = p.plant_id
                LEFT JOIN MST_Plant_Lines l WITH(NOLOCK) ON fam.line_id = l.line_id
                LEFT JOIN MST_Department d WITH(NOLOCK) ON fam.Department_ID = d.Department_ID
                LEFT JOIN MST_SubDepartment sd WITH(NOLOCK) ON fam.SubDepartment_ID = sd.SubDepartment_ID

                -- Approver 1 Joins
                LEFT JOIN MST_UserMaster u1 WITH(NOLOCK) ON fam.Approver1EmployeeCode = u1.EmployeeCode
                OUTER APPLY (
                    SELECT TOP 1 LoginTime, IsActive FROM ActiveUserSession WITH(NOLOCK) 
                    WHERE EmployeeCode = u1.EmployeeCode ORDER BY LoginTime DESC
                ) s1

                -- Approver 2 Joins
                LEFT JOIN MST_UserMaster u2 WITH(NOLOCK) ON fam.Approver2EmployeeCode = u2.EmployeeCode
                OUTER APPLY (
                    SELECT TOP 1 LoginTime, IsActive FROM ActiveUserSession WITH(NOLOCK) 
                    WHERE EmployeeCode = u2.EmployeeCode ORDER BY LoginTime DESC
                ) s2

                -- Dotted Line Approver Joins
                LEFT JOIN MST_UserMaster u3 WITH(NOLOCK) ON fam.DottedLineApproverEmployeeCode = u3.EmployeeCode
                OUTER APPLY (
                    SELECT TOP 1 LoginTime, IsActive FROM ActiveUserSession WITH(NOLOCK) 
                    WHERE EmployeeCode = u3.EmployeeCode ORDER BY LoginTime DESC
                ) s3

                WHERE fam.Active = 1";
        }

        // Global Utility for showing PNotify alerts to keep methods clean
        private void ShowErrorNotification(string message)
        {
            string safeMessage = message.Replace("'", "\\'").Replace("\n", " ").Replace("\r", "");
            string script = $"<script>new PNotify({{ title: 'Error', text: '{safeMessage}', type: 'error', styling: 'bootstrap3' }});</script>";
            ClientScript.RegisterStartupScript(this.GetType(), "ShowError", script, false);
        }

        protected void BtnFilter_Click(object sender, EventArgs e)
        {
            BindGridViewByFilters();
        }

        private void BindGridViewByFilters()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            // 1. Collect all selected Form IDs from the ListBox
            List<string> selectedFormIds = new List<string>();
            foreach (ListItem item in DDL_FormName.Items)
            {
                if (item.Selected && item.Value != "0")
                {
                    selectedFormIds.Add(item.Value);
                }
            }

            string plantId = DDL_Plant.SelectedValue;
            string lineId = DDL_Line.SelectedValue;
            string departmentId = DDL_Department.SelectedValue;
            string subDepartmentId = DDL_SubDepartment.SelectedValue;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = GetBaseGridQuery();

                // 2. Build the dynamic IN clause for multiple selected forms
                if (selectedFormIds.Count > 0)
                {
                    List<string> paramNames = new List<string>();
                    for (int i = 0; i < selectedFormIds.Count; i++)
                    {
                        paramNames.Add("@FormID" + i);
                    }
                    query += " AND fam.FormID IN (" + string.Join(",", paramNames) + ")";
                }

                // Add remaining standard filters
                if (!string.IsNullOrEmpty(plantId) && plantId != "0") query += " AND fam.plant_id = @plant_id";
                if (!string.IsNullOrEmpty(lineId) && lineId != "0") query += " AND fam.line_id = @line_id";
                if (!string.IsNullOrEmpty(departmentId) && departmentId != "0") query += " AND fam.Department_ID = @Department_ID";
                if (!string.IsNullOrEmpty(subDepartmentId) && subDepartmentId != "0") query += " AND fam.SubDepartment_ID = @SubDepartment_ID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    // 3. Inject the dynamic Form parameters safely into the command
                    for (int i = 0; i < selectedFormIds.Count; i++)
                    {
                        cmd.Parameters.AddWithValue("@FormID" + i, selectedFormIds[i]);
                    }

                    // Inject the remaining standard parameters
                    if (!string.IsNullOrEmpty(plantId) && plantId != "0") cmd.Parameters.AddWithValue("@plant_id", plantId);
                    if (!string.IsNullOrEmpty(lineId) && lineId != "0") cmd.Parameters.AddWithValue("@line_id", lineId);
                    if (!string.IsNullOrEmpty(departmentId) && departmentId != "0") cmd.Parameters.AddWithValue("@Department_ID", departmentId);
                    if (!string.IsNullOrEmpty(subDepartmentId) && subDepartmentId != "0") cmd.Parameters.AddWithValue("@SubDepartment_ID", subDepartmentId);

                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        GV_FormsApprovalMatrix.DataSource = dt;
                        GV_FormsApprovalMatrix.DataBind();
                    }
                }
            }
        }
    }
}