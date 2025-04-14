using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;

namespace AnmolDristi
{
    public partial class EditEmployee : System.Web.UI.Page
    {
        public static string employeeCode = string.Empty;
        public static string selectedCompanyValue = "CompanyValue";  // Replace with actual selected company value
        public static string selectedRegionValue = "RegionValue";    // Replace with actual selected region value
        public static string selectedDivisionValue = "DivisionValue"; // Replace with actual selected division value
        public static string selectedDepartmentValue = "DepartmentValue"; // Replace with actual selected department value
        public static string selectedSubDepartmentValue = "SubDepartmentValue"; // Replace with actual selected sub-department value
        public static string selectedCategoryValue = "CategoryValue"; // Replace with actual selected category value
        public static string selectedPlantValue = "PlantValue"; // Replace with actual selected plant value
        public static string selectedGradeValue = "GradeValue"; // Replace with actual selected grade value
        public static string selectedBranchValue = "BranchValue"; // Replace with actual selected branch value

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["USERID"] == null || Session["USERNAME"] == null || Session["WORKMAN"] == null)
                {
                    Response.Redirect("login.aspx",false);
                }
                else
                {
                    // Call each binder method with the appropriate parameters
                    CompanyBinder();
                    RegionBinder(selectedCompanyValue);
                    DivisionBinder(selectedCompanyValue, selectedRegionValue);
                    DepartmentBinder(selectedCompanyValue, selectedRegionValue, selectedDivisionValue);
                    SubDepartmentBinder(selectedCompanyValue, selectedRegionValue, selectedDivisionValue, selectedDepartmentValue);
                    CategoryBinder(selectedCompanyValue, selectedRegionValue, selectedDivisionValue, selectedDepartmentValue, selectedSubDepartmentValue);
                    PlantBinder(selectedCompanyValue, selectedRegionValue, selectedDivisionValue, selectedDepartmentValue, selectedSubDepartmentValue, selectedCategoryValue);
                    GradeBinder(selectedCompanyValue, selectedRegionValue, selectedDivisionValue, selectedDepartmentValue, selectedSubDepartmentValue, selectedCategoryValue, selectedPlantValue);
                    BranchBinder(selectedCompanyValue, selectedRegionValue, selectedDivisionValue, selectedDepartmentValue, selectedSubDepartmentValue, selectedCategoryValue, selectedPlantValue, selectedGradeValue);

                    employeeCode = Request.QueryString["EmployeeCode"];
                    if (!string.IsNullOrEmpty(employeeCode))
                    {
                        LoadEmployeeDetails(employeeCode);
                    }
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
            // Bind the DropDownList and get the flag indicating whether records were bound
            //DatabaseHelper.BindDropDownList(query, DDL_Company, textField, valueField, out recordsBound);
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
                DepartmentBinder(selectedCompanyValue, selectedRegionValue, selectedDivisionValue);
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

        private void DepartmentBinder(string selectedCompanyValue, string selectedRegionValue, string selectedDivisionValue)
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
                SubDepartmentBinder(selectedCompanyValue, selectedRegionValue, selectedDivisionValue, selectedDepartmentValue);
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
                PlantBinder(selectedCompanyValue, selectedRegionValue, selectedDivisionValue, selectedDepartmentValue, selectedSubDepartmentValue, selectedCategoryValue);
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
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        // Populate the controls with the employee's data
                        TB_EmpCode.Text = reader["EmployeeCode"].ToString();
                        TB_EmpName.Text = reader["EmployeeName"].ToString();
                        DDL_Company.SelectedValue = reader["CompanyId"].ToString();
                        DDL_Region.SelectedValue = reader["RegionId"].ToString();
                        DDL_Division.SelectedValue = reader["DivisionId"].ToString();
                        DDL_Department.SelectedValue = reader["DepartmentId"].ToString();
                        DDL_SubDepartment.SelectedValue = reader["SubDepartmentId"].ToString();
                        DDL_Category.SelectedValue = reader["CategoryId"].ToString();
                        DDL_Plant.SelectedValue = reader["PlantId"].ToString();
                        DDL_Grade.SelectedValue = reader["GradeId"].ToString();
                        DDL_Branch.SelectedValue = reader["BranchId"].ToString();
                        TB_DOJ.Text = reader["DOJ"] != DBNull.Value ? Convert.ToDateTime(reader["DOJ"]).ToString("yyyy-MM-dd") : "";
                        TB_Email.Text = reader["Email"].ToString();
                        TB_Mobile.Text = reader["Mobile"].ToString();
                        RBL_Gender.SelectedIndex = RBL_Gender.Items.IndexOf(RBL_Gender.Items.FindByText(reader["Gender"].ToString()));
                        TB_Password.Text = reader["Password"].ToString();
                    }
                }
            }
        }

        protected void BtnSubmit_Click(object sender, EventArgs e)
        {
            string query = "UPDATE MST_UserMaster SET EmployeeName = @EmployeeName, CompanyId = @CompanyId, RegionId = @RegionId, DivisionId = @DivisionId, " +
                   "DepartmentId = @DepartmentId, SubDepartmentId = @SubDepartmentId, CategoryId = @CategoryId, PlantId = @PlantId, " +
                   "GradeId = @GradeId, BranchId = @BranchId, DOJ = @DOJ, Email = @Email, Mobile = @Mobile, Gender = @Gender , Password= @Password " +
                   "WHERE EmployeeCode = @EmployeeCode";
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
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
                    cmd.Parameters.AddWithValue("@Gender", RBL_Gender.SelectedItem.Text);
                    cmd.Parameters.AddWithValue("@Password", TB_Password.Text);
                    conn.Open();
                    cmd.ExecuteNonQuery();

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
            }
        }



        protected void BtnReset_Click(object sender, EventArgs e)
        {
            Response.Redirect($"EditEmployee.aspx?EmployeeCode={employeeCode}", false);
        }


    }
}