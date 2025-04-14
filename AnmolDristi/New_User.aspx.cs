using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class New_User : System.Web.UI.Page
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
                    lbl_docname.Text = "Create New User";
                    CompanyBinder();

                }

            }
        }

        private void CompanyBinder()
        {
            string query = "SELECT  Company_Description, Company_ID FROM MST_Company";
            string textField = "Company_Description";
            string valueField = "Company_ID";

            bool recordsBound;

            // Bind the DropDownList and get the flag indicating whether records were bound
            DatabaseHelper.BindDropDownList(query, DDL_Company, textField, valueField, out recordsBound);

            // Check if any records were bound
            if (!recordsBound)
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_Company);
                string CompanyBinder_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'An error occurred!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";

                // RegisterStartupScript adds the JavaScript code to the page
                ClientScript.RegisterStartupScript(this.GetType(), "ShowCompanyBinderErrorNotification", CompanyBinder_Error_script, false);

            }
        }
        protected void DDL_Company_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Company.SelectedIndex != 0)
            {
                string selectedCompanyValue = DDL_Company.SelectedValue.ToString();
                RegionBinder(selectedCompanyValue);

                DDL_Region.Enabled = true;
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_Region);

                string DDL_Company_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'Invalid Selection!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowCompanyInvalidErrorNotification", DDL_Company_Error_script, false);
            }
        }

        private void RegionBinder(string selectedCompanyValue)
        {
            string query = "SELECT Region_Description, Region_ID  FROM MST_Region ";
            string textField = "Region_Description";
            string valueField = "Region_ID";

            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_Region, textField, valueField, new SqlParameter("@SelectedCompanyValue", selectedCompanyValue), out recordsBound);

            if (!recordsBound)
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_Region);

                string RegionBinder_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'An error occurred!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowRegionBinderErrorNotification", RegionBinder_Error_script, false);
            }
        }
        protected void DDL_Region_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Region.SelectedIndex != 0)
            {
                string selectedCompanyValue = DDL_Plant.SelectedValue.ToString();
                string selectedRegionValue = DDL_Region.SelectedValue.ToString();
                DivisionBinder(selectedCompanyValue, selectedRegionValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_Branch);

                string DDL_Region_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'Invalid Selection!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowRegionInvalidErrorNotification", DDL_Region_Error_script, false);
            }
        }

        private void DivisionBinder(string selectedCompanyValue, string selectedRegionValue)
        {
            // Construct the SQL query with parameters
            string query = "SELECT Division_Description, Division_ID FROM MST_Division ";
            string textField = "Division_Description"; // Assuming this is the correct field for displaying in the DropDownList
            string valueField = "Division_ID"; // Assuming this is the correct field for storing in the DropDownList

            // Create SQL parameters for plant_id and line_id
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@CompanyID", selectedCompanyValue),
                new SqlParameter("@RegionID", selectedRegionValue)
            };

            // Call the BindDropDownList method with parameters
            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_Division, textField, valueField, parameters, out recordsBound);

            // Check if any records were bound
            if (!recordsBound)
            {
                string DDL_Division_Error_script = @"<script type='text/javascript'>
                    new PNotify({
                        title: 'Error',
                        text: 'No Brands found for the selected plant and line!',
                        type: 'error',
                        styling: 'bootstrap3'
                    });
                </script>";

                // RegisterStartupScript adds the JavaScript code to the page
                ClientScript.RegisterStartupScript(this.GetType(), "ShowDivisionBinderBinderErrorNotification", DDL_Division_Error_script, false);
            }
        }
        protected void DDL_Division_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Division.SelectedIndex != 0)
            {
                string selectedCompanyValue = DDL_Company.SelectedValue.ToString();
                string selectedRegionValue = DDL_Region.SelectedValue.ToString();
                string selectedDivisionValue = DDL_Division.SelectedValue.ToString();
                DepartmentBinder(selectedCompanyValue, selectedRegionValue,  selectedDivisionValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_Department);

                string DDL_Division_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'Invalid Selection!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowDivisionInvalidErrorNotification", DDL_Division_Error_script, false);
            }
        }

        private void DepartmentBinder(string selectedCompanyValue, string selectedRegionValue, string selectedDivisionValue )
        {
            // Construct the SQL query with parameters
            string query = "SELECT Department_Description, Department_ID FROM MST_Department ";
            string textField = "Department_Description"; // Assuming this is the correct field for displaying in the DropDownList
            string valueField = "Department_ID"; // Assuming this is the correct field for storing in the DropDownList

            // Create SQL parameters for plant_id and line_id
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@CompanyID", selectedCompanyValue),
                new SqlParameter("@RegionID", selectedRegionValue),
                new SqlParameter("@DivisionID", selectedDivisionValue)
            };

            // Call the BindDropDownList method with parameters
            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_Department, textField, valueField, parameters, out recordsBound);

            // Check if any records were bound
            if (!recordsBound)
            {
                string DDL_Department_Error_script = @"<script type='text/javascript'>
                    new PNotify({
                        title: 'Error',
                        text: 'Invalid Selection!',
                        type: 'error',
                        styling: 'bootstrap3'
                    });
                </script>";

                // RegisterStartupScript adds the JavaScript code to the page
                ClientScript.RegisterStartupScript(this.GetType(), "ShowDepartmentBinderBinderErrorNotification", DDL_Department_Error_script, false);
            }
        }
        protected void DDL_Department_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Department.SelectedIndex != 0)
            {
                string selectedCompanyValue = DDL_Company.SelectedValue.ToString();
                string selectedRegionValue = DDL_Region.SelectedValue.ToString();
                string selectedDivisionValue = DDL_Division.SelectedValue.ToString();
                string selectedDepartmentValue = DDL_Department.SelectedValue.ToString();
                SubDepartmentBinder(selectedCompanyValue, selectedRegionValue,  selectedDivisionValue, selectedDepartmentValue);
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

        private void SubDepartmentBinder(string selectedCompanyValue, string selectedRegionValue, string selectedDivisionValue, string selectedDepartmentValue)
        {
            // Construct the SQL query with parameters
            string query = "SELECT  SubDepartment_Description, SubDepartment_ID FROM MST_SubDepartment ";
            string textField = "SubDepartment_Description"; // Assuming this is the correct field for displaying in the DropDownList
            string valueField = "SubDepartment_ID"; // Assuming this is the correct field for storing in the DropDownList

            // Create SQL parameters for plant_id and line_id
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@CompanyID", selectedCompanyValue),
                new SqlParameter("@RegionID", selectedRegionValue),
                new SqlParameter("@DivisionID", selectedDivisionValue),
                new SqlParameter("@DepartmentID", selectedDepartmentValue)
            };

            // Call the BindDropDownList method with parameters
            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_SubDepartment, textField, valueField, parameters, out recordsBound);

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
                string selectedCompanyValue = DDL_Company.SelectedValue.ToString();
                string selectedRegionValue = DDL_Region.SelectedValue.ToString();
                string selectedDivisionValue = DDL_Division.SelectedValue.ToString();
                string selectedDepartmentValue = DDL_Department.SelectedValue.ToString();
                string selectedSubDepartmentValue = DDL_SubDepartment.SelectedValue.ToString();
                CategoryBinder(selectedCompanyValue, selectedRegionValue, selectedDivisionValue, selectedDepartmentValue, selectedSubDepartmentValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_Category);

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

        private void CategoryBinder(string selectedCompanyValue, string selectedRegionValue, string selectedDivisionValue, string selectedDepartmentValue, string selectedSubDepartmentValue)
        {
            // Construct the SQL query with parameters
            string query = "SELECT  Category_Description, Category_ID FROM MST_Category ";
            string textField = "Category_Description"; // Assuming this is the correct field for displaying in the DropDownList
            string valueField = "Category_ID"; // Assuming this is the correct field for storing in the DropDownList

            // Create SQL parameters for plant_id and line_id
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@CompanyID", selectedCompanyValue),
                new SqlParameter("@RegionID", selectedRegionValue),
                new SqlParameter("@DivisionID", selectedDivisionValue),
                new SqlParameter("@DepartmentID", selectedDepartmentValue),
                new SqlParameter("@SubDepartmentID", selectedSubDepartmentValue)
            };

            // Call the BindDropDownList method with parameters
            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_Category, textField, valueField, parameters, out recordsBound);

            // Check if any records were bound
            if (!recordsBound)
            {
                string Category_Error_script = @"<script type='text/javascript'>
                    new PNotify({
                        title: 'Error',
                        text: 'Invalid Selection!',
                        type: 'error',
                        styling: 'bootstrap3'
                    });
                </script>";

                // RegisterStartupScript adds the JavaScript code to the page
                ClientScript.RegisterStartupScript(this.GetType(), "ShowCategoryBinderErrorNotification", Category_Error_script, false);
            }
        }
        protected void DDL_Category_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Category.SelectedIndex != 0)
            {
                string selectedCompanyValue = DDL_Company.SelectedValue.ToString();
                string selectedRegionValue = DDL_Region.SelectedValue.ToString();
                string selectedDivisionValue = DDL_Division.SelectedValue.ToString();
                string selectedDepartmentValue = DDL_Department.SelectedValue.ToString();
                string selectedSubDepartmentValue = DDL_SubDepartment.SelectedValue.ToString();
                string selectedCategoryValue = DDL_Category.SelectedValue.ToString();
                PlantBinder(selectedCompanyValue, selectedRegionValue,  selectedDivisionValue, selectedDepartmentValue, selectedSubDepartmentValue, selectedCategoryValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_Category);

                string DDL_Category_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'Invalid Selection!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowSubDepartmentInvalidErrorNotification", DDL_Category_Error_script, false);
            }
        }

        private void PlantBinder(string selectedCompanyValue, string selectedRegionValue, string selectedDivisionValue, string selectedDepartmentValue, string selectedSubDepartmentValue, string selectedCategoryValue)
        {
            // Construct the SQL query with parameters
            string query = "SELECT PlantID_Description, PlantID_ID FROM MST_Plant ";
            string textField = "PlantID_Description"; // Assuming this is the correct field for displaying in the DropDownList
            string valueField = "PlantID_ID"; // Assuming this is the correct field for storing in the DropDownList

            // Create SQL parameters for plant_id and line_id
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@CompanyID", selectedCompanyValue),
                new SqlParameter("@RegionID", selectedRegionValue),
                new SqlParameter("@DivisionID", selectedDivisionValue),
                new SqlParameter("@DepartmentID", selectedDepartmentValue),
                new SqlParameter("@SubDepartmentID", selectedSubDepartmentValue),
                new SqlParameter("@CategoryID", selectedCategoryValue)
            };

            // Call the BindDropDownList method with parameters
            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_Plant, textField, valueField, parameters, out recordsBound);

            // Check if any records were bound
            if (!recordsBound)
            {
                string PlantBinder_Error_script = @"<script type='text/javascript'>
                    new PNotify({
                        title: 'Error',
                        text: 'No Brands found for the selected plant and line!',
                        type: 'error',
                        styling: 'bootstrap3'
                    });
                </script>";

                // RegisterStartupScript adds the JavaScript code to the page
                ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantBinderBinderErrorNotification", PlantBinder_Error_script, false);
            }
        }
        protected void DDL_Plant_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Plant.SelectedIndex != 0)
            {
                string selectedCompanyValue = DDL_Company.SelectedValue.ToString();
                string selectedRegionValue = DDL_Region.SelectedValue.ToString();
                string selectedDivisionValue = DDL_Division.SelectedValue.ToString();
                string selectedDepartmentValue = DDL_Department.SelectedValue.ToString();
                string selectedSubDepartmentValue = DDL_SubDepartment.SelectedValue.ToString();
                string selectedCategoryValue = DDL_Category.SelectedValue.ToString();
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                GradeBinder(selectedCompanyValue, selectedRegionValue, selectedDivisionValue, selectedDepartmentValue, selectedSubDepartmentValue, selectedCategoryValue, selectedPlantValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_Grade);

                string DDL_Plant_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'Invalid Selection!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantInvalidErrorNotification", DDL_Plant_Error_script, false);
            }
        }

        private void GradeBinder(string selectedCompanyValue, string selectedRegionValue, string selectedDivisionValue, string selectedDepartmentValue, string selectedSubDepartmentValue, string selectedCategoryValue, string selectedPlantValue)
        {
            // Construct the SQL query with parameters
            string query = "SELECT Grade_Description , Grade_ID FROM MST_Grade  ";
            string textField = "Grade_Description"; // Assuming this is the correct field for displaying in the DropDownList
            string valueField = "Grade_ID"; // Assuming this is the correct field for storing in the DropDownList

            // Create SQL parameters for plant_id and line_id
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@CompanyID", selectedCompanyValue),
                new SqlParameter("@RegionID", selectedRegionValue),
                new SqlParameter("@DivisionID", selectedDivisionValue),
                new SqlParameter("@DepartmentID", selectedDepartmentValue),
                new SqlParameter("@SubDepartmentID", selectedSubDepartmentValue),
                new SqlParameter("@CategoryID", selectedCategoryValue),
                new SqlParameter("@PlantID", selectedPlantValue)
            };

            // Call the BindDropDownList method with parameters
            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_Grade, textField, valueField, parameters, out recordsBound);

            // Check if any records were bound
            if (!recordsBound)
            {
                string Grade_Error_script = @"<script type='text/javascript'>
                    new PNotify({
                        title: 'Error',
                        text: 'No Brands found for the selected plant and line!',
                        type: 'error',
                        styling: 'bootstrap3'
                    });
                </script>";

                // RegisterStartupScript adds the JavaScript code to the page
                ClientScript.RegisterStartupScript(this.GetType(), "ShowGradeBinderErrorNotification", Grade_Error_script, false);
            }
        }
        protected void DDL_Grade_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Grade.SelectedIndex != 0)
            {
                string selectedCompanyValue = DDL_Company.SelectedValue.ToString();
                string selectedRegionValue = DDL_Region.SelectedValue.ToString();
                string selectedBranchValue = DDL_Plant.SelectedValue.ToString();
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                string selectedDivisionValue = DDL_Division.SelectedValue.ToString();
                string selectedDepartmentValue = DDL_Department.SelectedValue.ToString();
                string selectedSubDepartmentValue = DDL_SubDepartment.SelectedValue.ToString();
                string selectedCategoryValue = DDL_Category.SelectedValue.ToString();
                string selectedGradeValue = DDL_Grade.SelectedValue.ToString();
                BranchBinder(selectedCompanyValue, selectedRegionValue, selectedDivisionValue, selectedDepartmentValue, selectedSubDepartmentValue, selectedCategoryValue, selectedPlantValue, selectedGradeValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_Grade);

                string DDL_Grade_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'Invalid Selection!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowGradeInvalidErrorNotification", DDL_Grade_Error_script, false);
            }
        }

        private void BranchBinder(string selectedCompanyValue, string selectedRegionValue, string selectedDivisionValue, string selectedDepartmentValue, string selectedSubDepartmentValue, string selectedCategoryValue, string selectedPlantValue, string selectedGradeValue)
        {
            // Construct the SQL query with parameters
            string query = "SELECT Branch_Description, Branch_ID FROM MST_Branch ";
            string textField = "Branch_Description"; // Assuming this is the correct field for displaying in the DropDownList
            string valueField = "Branch_ID"; // Assuming this is the correct field for storing in the DropDownList

            // Create SQL parameters for plant_id and line_id
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@CompanyID", selectedCompanyValue),
                new SqlParameter("@RegionID", selectedRegionValue)
            };

            // Call the BindDropDownList method with parameters
            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_Branch, textField, valueField, parameters, out recordsBound);

            // Check if any records were bound
            if (!recordsBound)
            {
                string PN_Error_script = @"<script type='text/javascript'>
                    new PNotify({
                        title: 'Error',
                        text: 'No line categories found for the selected plant and line!',
                        type: 'error',
                        styling: 'bootstrap3'
                    });
                </script>";

                // RegisterStartupScript adds the JavaScript code to the page
                ClientScript.RegisterStartupScript(this.GetType(), "ShowLineProductsBinderErrorNotification", PN_Error_script, false);
            }
        }
        protected void DDL_Branch_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Branch.SelectedIndex != 0)
            {
                string selectedCompanyValue = DDL_Company.SelectedValue.ToString();
                string selectedRegionValue = DDL_Region.SelectedValue.ToString();
                string selectedBranchValue = DDL_Plant.SelectedValue.ToString();
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                string selectedDivisionValue = DDL_Division.SelectedValue.ToString();
                string selectedDepartmentValue = DDL_Department.SelectedValue.ToString();
                string selectedSubDepartmentValue = DDL_SubDepartment.SelectedValue.ToString();
                string selectedCategoryValue = DDL_Category.SelectedValue.ToString();
                string selectedGradeValue = DDL_Grade.SelectedValue.ToString();
                string selectedBrandValue = DDL_Branch.SelectedValue.ToString();
            }
            else
            {

                string DDL_Branch_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'Invalid Selection!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowBranchInvalidErrorNotification", DDL_Branch_Error_script, false);
            }
        }


        protected void BtnSubmit_Click(object sender, EventArgs e)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            string employeeCode = TB_EmpCode.Text;
            string employeeName = TB_EmpName.Text;
            
            string company = DDL_Company.SelectedValue;
            string region = DDL_Region.SelectedValue;
            string branch = DDL_Branch.SelectedValue;
            string plant = DDL_Plant.SelectedValue;
            string division = DDL_Division.SelectedValue;
            string department = DDL_Department.SelectedValue;
            string subDepartment = DDL_SubDepartment.SelectedValue;
            string category = DDL_Category.SelectedValue;
            string grade = DDL_Grade.SelectedValue;

            DateTime doj = DateTime.Parse(TB_DOJ.Text).Date;
            string email = string.IsNullOrEmpty(TB_Email.Text) ? null : TB_Email.Text;
            string mobile = string.IsNullOrEmpty(TB_Mobile.Text) ? null : TB_Mobile.Text;
            string gender = RBL_Gender.SelectedItem.Text;

            string employeeStatus = "Active";
            DateTime createdDate = DateTime.Now.Date;
            string createdBy = Session["WORKMAN"].ToString();
            string creationMode = "Single";
            string password = GeneratePassword();
            Console.WriteLine(password);
            DateTime? lastLogin = null;
            DateTime? lastlogout = null;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "INSERT INTO MST_UserMaster (EmployeeCode ,EmployeeName, BranchId, PlantId, EmployeeStatus, CompanyId, DepartmentId, GradeId, SubDepartmentId, " +
                                    "DOJ, CategoryId, Email, Mobile, Gender, RegionId, DivisionId, CreatedDate, CreatedBy, CreationMode, Password, LastLoginDate, LastLogoutDate) " +

                                    "VALUES (@EmployeeCode ,@EmployeeName, @BranchId, @PlantId, @EmployeeStatus, @CompanyId, @DepartmentId, @GradeId, @SubDepartmentId, @DOJ, " +
                            "@CategoryId, @Email, @Mobile, @Gender, @RegionId, @DivisionId, @CreatedDate, @CreatedBy, @CreationMode, @Password, @LastLoginDate, @LastLogoutDate)";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {

                        // Add parameters
                        command.Parameters.AddWithValue("@EmployeeCode", (object)employeeCode ?? DBNull.Value );
                        command.Parameters.AddWithValue("@EmployeeName", (object)employeeName ?? DBNull.Value );
                        command.Parameters.AddWithValue("@BranchId", (object)branch ?? DBNull.Value );
                        command.Parameters.AddWithValue("@PlantId", (object)plant ?? DBNull.Value );
                        command.Parameters.AddWithValue("@EmployeeStatus", (object)employeeStatus ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CompanyId", (object)company ?? DBNull.Value );
                        command.Parameters.AddWithValue("@DepartmentId", (object)department ?? DBNull.Value);
                        command.Parameters.AddWithValue("@GradeId", (object)grade ?? DBNull.Value);
                        command.Parameters.AddWithValue("@SubDepartmentId", (object)subDepartment ?? DBNull.Value);
                        command.Parameters.AddWithValue("@DOJ", (object)doj ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CategoryId", (object)category ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Email", (object)email ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Mobile", (object)mobile ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Gender", (object)gender ?? DBNull.Value);
                        command.Parameters.AddWithValue("@RegionId", (object)region ?? DBNull.Value);
                        command.Parameters.AddWithValue("@DivisionId", (object)division ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CreatedDate", (object)createdDate ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CreatedBy", (object)createdBy ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CreationMode", (object)creationMode ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Password", (object)password ?? DBNull.Value);
                        command.Parameters.AddWithValue("@LastLoginDate", (object)lastLogin ?? DBNull.Value);
                        command.Parameters.AddWithValue("@LastLogoutDate", (object)lastlogout ?? DBNull.Value);

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

        private void MakeInputsReadOnly()
        {
            DDL_Company.Enabled = false;
            DDL_Region.Enabled = false;
            DDL_Branch.Enabled = false;
            DDL_Plant.Enabled = false;
            DDL_Division.Enabled = false;
            DDL_Department.Enabled = false;
            DDL_SubDepartment.Enabled = false;
            DDL_Category.Enabled = false;   
            DDL_Grade.Enabled = false;

            TB_EmpCode.ReadOnly = true;
            TB_EmpName.ReadOnly = true;
            TB_DOJ.ReadOnly = true;
            TB_Email.ReadOnly = true;
            TB_Mobile.ReadOnly = true;
            RBL_Gender.Enabled = false;

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
            Response.Redirect("New_User.aspx");
        }

        public static string GeneratePassword()
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()";
            Random random = new Random();

            int length = random.Next(6, 9); // Random length between 6 and 8
            StringBuilder result = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                result.Append(validChars[random.Next(validChars.Length)]);
            }

            return result.ToString();
        }

    }
}