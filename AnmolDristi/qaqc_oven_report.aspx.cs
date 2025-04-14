using AnmolDristi.DAL;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class qaqc_oven_report : System.Web.UI.Page
    {
        string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        DB_Utility_OH4Y dbcl = new DB_Utility_OH4Y();
        public static String OVN_Id = String.Empty;
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

                    lbl_docname.Text = "QA - Oven Report";
                    lbl_docnumber.Text = "ANMOL/DOC/CORQ/QA/02";
                    PlantBinder();
                    DisplayCurrentShift();
                }
            }
        }

        private void LoadApproversOld(string selectedPlantValue, string selectedPlantLineValue)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("usp_GetFormsApprovalMatrix", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@PlantId", selectedPlantValue);
                    cmd.Parameters.AddWithValue("@LineId", selectedPlantLineValue);
                    cmd.Parameters.AddWithValue("@FormID", 4);
                    cmd.Parameters.AddWithValue("@FormName", "qaqc_oven_report");

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        hdn_formid.Value = "4";
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        GridViewApprovers.DataSource = dt;
                        GridViewApprovers.DataBind();

                        if (dt.Rows.Count > 0)
                        {
                            DataRow row = dt.Rows[0];

                            Approver1NameLabel.Text = row["Approver1Name"].ToString();
                            Approver1CodeLabel.Text = row["Approver1EmployeeCode"].ToString();
                            //Approver1Photo.ImageUrl = row["Approver1Photo"].ToString();

                            Approver2NameLabel.Text = row["Approver2Name"].ToString();
                            Approver2CodeLabel.Text = row["Approver2EmployeeCode"].ToString();
                            //Approver2Photo.ImageUrl = row["Approver2Photo"].ToString();

                            DottedLineApproverNameLabel.Text = row["DottedLineApproverName"].ToString();
                            DottedLineApproverCodeLabel.Text = row["DottedLineApproverEmployeeCode"].ToString();
                            //DottedLineApproverPhoto.ImageUrl = row["DottedLineApproverPhoto"].ToString();
                        }
                        else
                        {

                            // Set default values to ADMIN if no rows are found
                            //Approver1NameLabel.Text = "ADMIN";
                            //Approver1CodeLabel.Text = "ADMIN";

                            //Approver2NameLabel.Text = "ADMIN";
                            //Approver2CodeLabel.Text = "ADMIN";

                            //DottedLineApproverNameLabel.Text = "ADMIN";
                            //DottedLineApproverCodeLabel.Text = "ADMIN";

                            // Insert default record
                            dbcl.InsertDefaultApprovers(selectedPlantValue, selectedPlantLineValue, 1);

                            // Reload after insertion
                            LoadApprovers(selectedPlantValue, selectedPlantLineValue);

                            string PlantBinder_Error_script = @"<script type='text/javascript'>
                                new PNotify({
                                    title: 'Data Success',
                                    text: 'No Approver Mapping Found! Default Approvers Added.',
                                    type: 'success',
                                    styling: 'bootstrap3'
                                });
                            </script>";

                            // RegisterStartupScript adds the JavaScript code to the page
                            ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantBinderErrorNotification", PlantBinder_Error_script, false);
                        }
                    }
                }
            }
        }

        private void LoadApprovers(string selectedPlantValue, string selectedPlantLineValue)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("usp_GetFormsApprovalMatrix", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@PlantId", selectedPlantValue);
                    //cmd.Parameters.AddWithValue("@LineId", selectedPlantLineValue);
                    cmd.Parameters.AddWithValue("@LineId", string.IsNullOrEmpty(selectedPlantLineValue) ? (object)DBNull.Value : selectedPlantLineValue);
                    cmd.Parameters.AddWithValue("@FormID", 4);
                    cmd.Parameters.AddWithValue("@FormName", "qaqc_oven_report");

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        hdn_formid.Value = "4";
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        if (dt.Rows.Count > 0)
                        {
                            // Populate the GridView
                            GridViewApprovers.DataSource = dt;
                            GridViewApprovers.DataBind();

                            // Populate labels with approver data
                            DataRow row = dt.Rows[0];

                            Approver1NameLabel.Text = row["Approver1Name"].ToString();
                            Approver1CodeLabel.Text = row["Approver1EmployeeCode"].ToString();
                            //Approver1Photo.ImageUrl = row["Approver1Photo"].ToString();

                            Approver2NameLabel.Text = row["Approver2Name"].ToString();
                            Approver2CodeLabel.Text = row["Approver2EmployeeCode"].ToString();
                            //Approver2Photo.ImageUrl = row["Approver2Photo"].ToString();

                            DottedLineApproverNameLabel.Text = row["DottedLineApproverName"].ToString();
                            DottedLineApproverCodeLabel.Text = row["DottedLineApproverEmployeeCode"].ToString();
                            //DottedLineApproverPhoto.ImageUrl = row["DottedLineApproverPhoto"].ToString();
                        }
                        else
                        {
                            // Insert default approvers
                            bool isInserted = dbcl.InsertDefaultApprovers(selectedPlantValue, selectedPlantLineValue, 4);

                            if (isInserted)
                            {
                                // Re-fetch data after insertion (no recursion)
                                da.Fill(dt);
                                if (dt.Rows.Count > 0)
                                {
                                    GridViewApprovers.DataSource = dt;
                                    GridViewApprovers.DataBind();

                                    // Populate labels with approver data
                                    DataRow row = dt.Rows[0];

                                    Approver1NameLabel.Text = row["Approver1Name"].ToString();
                                    Approver1CodeLabel.Text = row["Approver1EmployeeCode"].ToString();
                                    //Approver1Photo.ImageUrl = row["Approver1Photo"].ToString();

                                    Approver2NameLabel.Text = row["Approver2Name"].ToString();
                                    Approver2CodeLabel.Text = row["Approver2EmployeeCode"].ToString();
                                    //Approver2Photo.ImageUrl = row["Approver2Photo"].ToString();

                                    DottedLineApproverNameLabel.Text = row["DottedLineApproverName"].ToString();
                                    DottedLineApproverCodeLabel.Text = row["DottedLineApproverEmployeeCode"].ToString();
                                    //DottedLineApproverPhoto.ImageUrl = row["DottedLineApproverPhoto"].ToString();
                                }
                                else
                                {
                                    ShowErrorNotification("Failed to load approver data even after insertion.");
                                }
                            }
                            else
                            {
                                // If default insertion fails
                                ShowErrorNotification("Failed to insert default approvers.");
                            }
                        }
                    }
                }
            }
        }

        // Utility function to display notifications
        private void ShowErrorNotification(string message)
        {
            string script = $@"<script type='text/javascript'>
                        new PNotify({{
                            title: 'Error',
                            text: '{message}',
                            type: 'error',
                            styling: 'bootstrap3'
                        }});
                      </script>";
            ClientScript.RegisterStartupScript(this.GetType(), "ErrorNotification", script, false);
        }


        private void DisplayCurrentShift()
        {
            ShiftManager shiftManager = new ShiftManager();
            string currentShift = shiftManager.GetCurrentShiftType();
            hdn_shiftvalue.Value = currentShift;
        }

        private void PlantBinder()
        {
            string query = "SELECT plant_id, CONCAT(plant_name, '[', sap_code, ']') AS plant_name FROM MST_PlantDetails";
            string textField = "plant_name";
            string valueField = "plant_id";

            bool recordsBound;

            // Bind the DropDownList and get the flag indicating whether records were bound
            DatabaseHelper.BindDropDownList(query, DDL_Plant, textField, valueField, out recordsBound);

            // Check if any records were bound
            if (!recordsBound)
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_Plant);
                string PlantBinder_Error_script = @"<script type='text/javascript'>
                    new PNotify({
                        title: 'Error',
                        text: 'An error occurred!',
                        type: 'error',
                        styling: 'bootstrap3'
                    });
                </script>";

                // RegisterStartupScript adds the JavaScript code to the page
                ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantBinderErrorNotification", PlantBinder_Error_script, false);

            }
        }

        public class ValidationCriteria
        {
            public string RequiredFieldErrorMessage { get; set; }
            public string RegularExpressionErrorMessage { get; set; }
            public string RegularExpression { get; set; }
            public string RangeErrorMessage { get; set; }
            public string MinimumValue { get; set; }
            public string MaximumValue { get; set; }
            public bool IsRequired { get; set; }
            public bool IsRegularExpressionRequired { get; set; }
            public bool IsRangeRequired { get; set; }
        }
        protected void DDL_Plant_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Plant.SelectedIndex != 0)
            {
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                PlantLinesBinder(selectedPlantValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_PlantLine);

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
        private void PlantLinesBinder(string selectedPlantValue)
        {
            string query = "SELECT line_id, line_name FROM MST_Plant_Lines WHERE plant_id = @SelectedPlantValue";
            string textField = "line_name";
            string valueField = "line_id";

            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_PlantLine, textField, valueField, new SqlParameter("@SelectedPlantValue", selectedPlantValue), out recordsBound);

            if (!recordsBound)
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_PlantLine);

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
        protected void DDL_PlantLine_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_PlantLine.SelectedIndex != 0)
            {
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                string selectedPlantLineValue = DDL_PlantLine.SelectedValue.ToString();
                LineProductsBinder(selectedPlantValue, selectedPlantLineValue);

                LoadApprovers(selectedPlantValue, selectedPlantLineValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductCategory);

                string DDL_PlantLine_Error_script = @"<script type='text/javascript'>
                    new PNotify({
                        title: 'Error',
                        text: 'Invalid Selection!',
                        type: 'error',
                        styling: 'bootstrap3'
                    });
                </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantInvalidErrorNotification", DDL_PlantLine_Error_script, false);
            }

        }
        private void LineProductsBinder(string selectedPlantValue, string selectedPlantLineValue)
        {
            // Construct the SQL query with parameters
            string query = "SELECT category_id, category_name FROM MST_LineCategory WHERE plant_id = @PlantId AND line_id = @LineId";
            string textField = "category_name"; // Assuming this is the correct field for displaying in the DropDownList
            string valueField = "category_id"; // Assuming this is the correct field for storing in the DropDownList

            // Create SQL parameters for plant_id and line_id
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@PlantId", selectedPlantValue),
                new SqlParameter("@LineId", selectedPlantLineValue)
            };

            // Call the BindDropDownList method with parameters
            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_ProductCategory, textField, valueField, parameters, out recordsBound);

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
        protected void DDL_ProductCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_ProductCategory.SelectedIndex != 0)
            {
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                string selectedPlantLineValue = DDL_PlantLine.SelectedValue.ToString();
                string selectedProductCategoryValue = DDL_ProductCategory.SelectedValue.ToString();
                ProductBrandsBinder(selectedPlantValue, selectedPlantLineValue, selectedProductCategoryValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductBrand);

                string DDL_PlantLine_Error_script = @"<script type='text/javascript'>
                    new PNotify({
                        title: 'Error',
                        text: 'Invalid Selection!',
                        type: 'error',
                        styling: 'bootstrap3'
                    });
                </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantInvalidErrorNotification", DDL_PlantLine_Error_script, false);
            }
        }
        private void ProductBrandsBinder(string selectedPlantValue, string selectedPlantLineValue, string selectedProductCategoryValue)
        {
            // Construct the SQL query with parameters
            string query = "SELECT brand_id, brand_name FROM MST_LineCatBrands WHERE plant_id = @PlantId AND line_id = @LineId and category_id=@CategoryId";
            string textField = "brand_name"; // Assuming this is the correct field for displaying in the DropDownList
            string valueField = "brand_id"; // Assuming this is the correct field for storing in the DropDownList

            // Create SQL parameters for plant_id and line_id
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@PlantId", selectedPlantValue),
                new SqlParameter("@LineId", selectedPlantLineValue),
                new SqlParameter("@CategoryId", selectedProductCategoryValue)
            };

            // Call the BindDropDownList method with parameters
            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_ProductBrand, textField, valueField, parameters, out recordsBound);

            // Check if any records were bound
            if (!recordsBound)
            {
                string ProductBrands_Error_script = @"<script type='text/javascript'>
                    new PNotify({
                        title: 'Error',
                        text: 'No Brands found for the selected plant and line!',
                        type: 'error',
                        styling: 'bootstrap3'
                    });
                </script>";

                // RegisterStartupScript adds the JavaScript code to the page
                ClientScript.RegisterStartupScript(this.GetType(), "ShowProductBrandsBinderErrorNotification", ProductBrands_Error_script, false);
            }
        }
        protected void DDL_ProductBrand_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_ProductBrand.SelectedIndex != 0)
            {
                string selectedProductBrandValue = DDL_ProductBrand.SelectedValue.ToString();
                //BrandSKUBinder(selectedProductBrandValue);

                DataTable dataTable = DatabaseHelper.GetBrandFieldsControlByBrandId(Convert.ToInt16(selectedProductBrandValue));


                // Example: Querying the DataTable for a specific field name
                //string fieldName = "no_of_pcs"; // Specify the field name you want to query
                //DataRow[] rows = dataTable.Select($"brand_id = {selectedProductBrandValue} AND field_name = '{fieldName}'");

                // Iterate through the filtered rows and extract validation criteria
                foreach (DataRow row in dataTable.Rows)
                {
                    // Extract field name from the current row
                    string fieldName = row["field_name"].ToString();

                    // Extract validation criteria from the DataRow
                    bool rfvEnabled = Convert.ToBoolean(row["RFV_YesNo"]);
                    string rfvErrorMessage = row["RFV_ErrorMsg"].ToString();
                    bool revEnabled = Convert.ToBoolean(row["REV_YesNo"]);
                    string revErrorMessage = row["REV_ErrorMsg"].ToString();
                    string revExpression = row["REV_Expression"].ToString();
                    bool rvEnabled = Convert.ToBoolean(row["RV_Yesno"]);
                    string rvErrorMessage = row["RV_ErrorMsg"].ToString();
                    string rvMinValue = row["RV_MinValue"].ToString();
                    string rvMaxValue = row["RV_MaxValue"].ToString();

                    // Create a new instance of ValidationCriteria and populate it with data from the DataRow
                    ValidationCriteria criteria = new ValidationCriteria();
                    criteria.RequiredFieldErrorMessage = rfvErrorMessage;
                    criteria.IsRequired = rfvEnabled;
                    criteria.RegularExpressionErrorMessage = revErrorMessage;
                    criteria.IsRegularExpressionRequired = revEnabled;
                    criteria.RegularExpression = revExpression;
                    criteria.RangeErrorMessage = rvErrorMessage;
                    criteria.IsRangeRequired = rvEnabled;
                    criteria.MinimumValue = rvMinValue;
                    criteria.MaximumValue = rvMaxValue;

                    // Use the criteria as needed
                    // For example, you can pass it to a method to set up validators
                    //SetUpValidatorsForField(fieldName, criteria);
                }
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductBrand);

                string DDL_ProductBrand_Error_script = @"<script type='text/javascript'>
                    new PNotify({
                        title: 'Error',
                        text: 'Invalid Selection!',
                        type: 'error',
                        styling: 'bootstrap3'
                    });
                </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowSKUInvalidErrorNotification", DDL_ProductBrand_Error_script, false);
            }
        }
        //private void BrandSKUBinder(string selectedProductBrandValue)
        //{
        //    string query = "SELECT SKUId, SKU_name FROM MST_Brand_SKU WHERE brand_id = @SelectedPlantValue";
        //    string textField = "SKU_name";
        //    string valueField = "SKUId";

        //    bool recordsBound;
        //    DatabaseHelper.BindDropDownList(query, DDL_BrandSKU, textField, valueField, new SqlParameter("@SelectedPlantValue", selectedProductBrandValue), out recordsBound);

        //    if (!recordsBound)
        //    {
        //        DatabaseHelper.BindWithDefaultNoRecords(DDL_PlantLine);

        //        string BrandSKUBinder_Error_script = @"<script type='text/javascript'>
        //            new PNotify({
        //                title: 'Error',
        //                text: 'An error occurred!',
        //                type: 'error',
        //                styling: 'bootstrap3'
        //            });
        //        </script>";
        //        ClientScript.RegisterStartupScript(this.GetType(), "ShowBrandSKUBinderErrorNotification", BrandSKUBinder_Error_script, false);
        //    }
        //}


        protected void Btn_Basic_Data_Click(object sender, EventArgs e)
        {
            SaveData();
        }
        private void SaveData()
        {
            string plantName = DDL_Plant.SelectedValue;
            string plantLine = DDL_PlantLine.SelectedValue;
            string productCategory = DDL_ProductCategory.SelectedValue;
            string productBrand = DDL_ProductBrand.SelectedValue;
            //string brandSKU = DDL_BrandSKU.SelectedValue;
            //string varietyPacket = TB_VartyPkt.Text;

            string brandSKU = string.Empty;
            string varietyPacket = string.Empty;
            string RejectionKgs = TB_RejectionKgs.Text;
            string shift = hdn_shiftvalue.Value.ToString();

            string approver1EmployeeCode = Approver1CodeLabel.Text.ToString();
            string approver2EmployeeCode = Approver2CodeLabel.Text.ToString();
            string dottedLineApproverEmployeeCode = DottedLineApproverCodeLabel.Text.ToString();

            string insertQuery = @"
                INSERT INTO TRN_FINAL_OVEN_REPORT ([OVN_Id], [FormID], [SubmittedById], [SubmittedDate], [SubmittedTime], [Shift], [SubmittedByEmployeeCode],
                    [PlantName], [Line], [ProductCategory], [ProductBrand], [SKUId], [Date], [Variety], [Rejection], [Approver1EmployeeCode],
                    [Approver2EmployeeCode], [DottedLineApproverEmployeeCode]
          
                ) VALUES (@OVN_Id, @FormID, @SubmittedById, @SubmittedDate, @SubmittedTime, @Shift, @SubmittedByEmployeeCode,
                    @PlantName, @Line, @ProductCategory, @ProductBrand, @SKUId, @Date, @Variety, @Rejection,
                    @Approver1EmployeeCode,
                    @Approver2EmployeeCode, @DottedLineApproverEmployeeCode
          
                  );";


            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand insertcommand = new SqlCommand(insertQuery, connection))
                    {
                        // Set parameters for the SQL command
                        // Add parameters with values from your form controls
                        //insertCommand.Parameters.AddWithValue("@PlantId", plantName);

                        insertcommand.Parameters.AddWithValue("@OVN_Id", GenerateUniqueOVNId());
                        insertcommand.Parameters.AddWithValue("@FormID", Convert.ToInt32(hdn_formid.Value.ToString()));
                        insertcommand.Parameters.AddWithValue("@SubmittedById", Convert.ToInt32(Session["USERID"].ToString()));
                        insertcommand.Parameters.AddWithValue("@SubmittedDate", DateTime.Now.Date);
                        insertcommand.Parameters.AddWithValue("@SubmittedTime", DateTime.Now.TimeOfDay);
                        insertcommand.Parameters.AddWithValue("@Shift", shift);
                        insertcommand.Parameters.AddWithValue("@SubmittedByEmployeeCode", Session["WORKMAN"].ToString());
                        insertcommand.Parameters.AddWithValue("@PlantName", plantName);
                        insertcommand.Parameters.AddWithValue("@Line", plantLine);
                        insertcommand.Parameters.AddWithValue("@ProductCategory", productCategory);
                        insertcommand.Parameters.AddWithValue("@ProductBrand", productBrand);
                        insertcommand.Parameters.AddWithValue("@SKUId", brandSKU);
                        insertcommand.Parameters.AddWithValue("@Date", DateTime.Now.Date);
                        insertcommand.Parameters.AddWithValue("@Variety", varietyPacket);
                        insertcommand.Parameters.AddWithValue("@Rejection", RejectionKgs);
                        //insertcommand.Parameters.AddWithValue("@ViewMode", 1);
                        //insertcommand.Parameters.AddWithValue("@DeleteMode", 0);
                        //insertcommand.Parameters.AddWithValue("@Time", DateTime.Now.TimeOfDay);
                        //command.Parameters.AddWithValue("@BT_RPM", "1000");
                        //command.Parameters.AddWithValue("@Dry_Gauge", 12.34M);
                        //command.Parameters.AddWithValue("@Dry_Weight", 56.78M);
                        //command.Parameters.AddWithValue("@Dipped_Weight", 90.12M);
                        //command.Parameters.AddWithValue("@Square_Shape_Length", 15.00M);
                        //command.Parameters.AddWithValue("@Square_Shape_Width", 20.00M);
                        //command.Parameters.AddWithValue("@Round_Shape_Diameter", 30.00M);
                        //command.Parameters.AddWithValue("@PktWeight", 25.00M);
                        //command.Parameters.AddWithValue("@BiscuitsPerPkt", 24);
                        //command.Parameters.AddWithValue("@Oven_Start_Time", DateTime.Now.TimeOfDay);
                        //command.Parameters.AddWithValue("@Oven_Stop_Time", DateTime.Now.AddHours(1).TimeOfDay);
                        //command.Parameters.AddWithValue("@Reason", "Routine Check");

                        insertcommand.Parameters.AddWithValue("@Approver1EmployeeCode", DBNull.Value);
                        insertcommand.Parameters.AddWithValue("@Approver1_Status", 1);
                        //insertcommand.Parameters.AddWithValue("@Approver1_TimeStamp", DateTime.Now);
                        insertcommand.Parameters.AddWithValue("@Approver2EmployeeCode", DBNull.Value);
                        insertcommand.Parameters.AddWithValue("@Approver2_Status", 0);
                        //insertcommand.Parameters.AddWithValue("@Approver2_TimeStamp", DateTime.Now);
                        insertcommand.Parameters.AddWithValue("@DottedLineApproverEmployeeCode", DBNull.Value);
                        insertcommand.Parameters.AddWithValue("@DottedApprover_Status", 1);
                        //insertcommand.Parameters.AddWithValue("@DottedApprover_TimeStamp", DateTime.Now);

                        // Open the connection and execute the command
                        connection.Open();
                        insertcommand.ExecuteNonQuery();

                        lblMessage.Text = "Data saved successfully!";
                        lblMessage.ForeColor = System.Drawing.Color.Green;
                        //connection.Open();
                        //command.ExecuteNonQuery();
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "SwitchTab", "document.getElementById('ovenReport-tab').click();", true);

                    }
                }
            }
            catch (Exception ex)
            {
                // Handle the exception
                lblMessage.Text = "An error occurred: " + ex.Message;
                lblMessage.ForeColor = System.Drawing.Color.Red;
            }
        }
        private string GenerateUniqueOVNId()
        {
            string newOVNId;
            //string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Fetch the maximum OVN_Id value
                    string query = "SELECT ISNULL(MAX(CAST(SUBSTRING(OVN_Id, 4, LEN(OVN_Id)) AS INT)), 0) FROM TRN_FINAL_OVEN_REPORT";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        object result = command.ExecuteScalar();
                        int maxOVNValue = Convert.ToInt32(result);

                        // Increment the numeric part
                        int numericPart = maxOVNValue + 1;

                        // Format the new value
                        newOVNId = $"OVN{numericPart:D3}"; // Ensure three digits (e.g., OVN001, OVN002)
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                Console.WriteLine("Error generating OVN_Id: " + ex.Message);
                throw;
            }
            OVN_Id = newOVNId;
            return newOVNId;
        }


        protected void btnReset_Click(object sender, EventArgs e)
        {
            // Reset all dropdown selections to the first item(default item)
            //DDL_Plant.SelectedIndex = 0;
            //DDL_PlantLine.SelectedIndex = 0;
            //DDL_ProductCategory.SelectedIndex = 0;
            //DDL_ProductBrand.SelectedIndex = 0;
            //DDL_BrandSKU.SelectedIndex = 0;
            // Clear all dropdown selections
            DDL_Plant.SelectedIndex = -1;
            DDL_PlantLine.SelectedIndex = -1;
            DDL_ProductCategory.SelectedIndex = -1;
            DDL_ProductBrand.SelectedIndex = -1;
            //DDL_BrandSKU.SelectedIndex = -1;


            //TB_VartyPkt.Text = string.Empty;
            lblMessage.Text = string.Empty;

            TB_RejectionKgs.Text = string.Empty;

            // Clear hidden fields or reset their values if necessary
            //hdn_shiftvalue.Value = string.Empty;

            // Clear or reset any labels used for messages
            // lblMessage.Text = string.Empty;
        }



        protected void btn_oven_save_Click(object sender, EventArgs e)
        {
            if (OVN_Id != string.Empty)
            {
                // Capture the form data
                //string btRpm = TB_BTRPM.Text.Trim();
                string btRpm = TB_BakingTime.Text.ToString();
                string dryGauge = TB_Gauge.Text.Trim();
                //string dryWeight = TB_Weight.Text.Trim();
                //string dippedWeight = TB_DippedWeight.Text.Trim();
                string dryWeight = TB_wgtwtoil.Text.Trim();
                string dippedWeight = TB_wgtwoil.Text.Trim();
                string oilpercent = string.Empty;
                string squareShapeLength = TB_Length.Text.Trim();
                string squareShapeWidth = TB_Width.Text.Trim();
                string roundShapeDiameter = TB_Diameter.Text.Trim();
                //string pktWeight = TB_PktWeight.Text.Trim();
                //the below field stores the oil percentage value
                string pktWeight = TB_oilpercent.Text.Trim();
                string biscuitsPerPkt = TB_BiscuitsPerPkt.Text.Trim();
                string ovenStartTimeStr = TB_StartTime.Text.Trim();
                string ovenStopTimeStr = TB_StopTime.Text.Trim();
                string reason = TB_Reason.Text.Trim();

                try
                {
                    // Convert oven start and stop times to TimeSpan objects
                    TimeSpan ovenStartTime = TimeSpan.Parse(ovenStartTimeStr);
                    TimeSpan ovenStopTime = TimeSpan.Parse(ovenStopTimeStr);

                    // Calculate Oven Loss Time as a TimeSpan
                    TimeSpan ovenLossTimeSpan = ovenStopTime - ovenStartTime;
                    if (ovenLossTimeSpan < TimeSpan.Zero)
                    {
                        ovenLossTimeSpan = TimeSpan.Zero; // Adjust if stop time is before start time
                    }

                    // Convert Oven Loss Time to SQL TIME format (HH:mm:ss)
                    string ovenLossTime = string.Format("{0:D2}:{1:D2}:{2:D2}",
                        ovenLossTimeSpan.Hours, ovenLossTimeSpan.Minutes, ovenLossTimeSpan.Seconds);

                    // Assume the primary key is obtained or passed as a parameter (e.g., from a hidden field or query string)
                    //int primaryKey = int.Parse(GenerateUniqueOVNId().Value);

                    // Call the method to update the database

                    UpdateOvenReport(btRpm, dryGauge, dryWeight, dippedWeight, squareShapeLength, squareShapeWidth, roundShapeDiameter, pktWeight, biscuitsPerPkt, ovenStartTimeStr, ovenStopTimeStr, ovenLossTime, reason);
                    lbl_oven.Text = "Data updated successfully.";
                    lbl_oven.ForeColor = System.Drawing.Color.Green;

                    string Data_SuccessScript2 = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Data Success',
                                text: 'Recorded Successfully!!',
                                type: 'success',
                                styling: 'bootstrap3'
                            });
                        </script>";

                    // RegisterStartupScript adds the JavaScript code to the page
                    ClientScript.RegisterStartupScript(this.GetType(), "ShowDataSuccessNotification2", Data_SuccessScript2, false);
                }
                catch (FormatException ex)
                {
                    lbl_oven.Text = "Invalid time format: " + ex.Message;
                    lbl_oven.ForeColor = System.Drawing.Color.Red;
                    // Handle format errors for date parsing
                    //throw new Exception("Invalid time format: " + ex.Message);
                }
                catch (Exception ex)
                {
                    // Handle general errors
                    lbl_oven.Text = "Error: " + ex.Message;
                    lbl_oven.ForeColor = System.Drawing.Color.Red;
                }
            }
            else
            {
                string NO_BasicData = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'NO Basic Data',
                                text: 'Initiate from Step-1!!',
                                type: 'warning',
                                styling: 'bootstrap3'
                            });
                        </script>";

                // RegisterStartupScript adds the JavaScript code to the page
                ClientScript.RegisterStartupScript(this.GetType(), "NO_BasicData_Error", NO_BasicData, false);

                ScriptManager.RegisterStartupScript(this, this.GetType(), "SwitchTab", "document.getElementById('basicData-tab').click();", true);
            }
        }

        private void UpdateOvenReport(string btRpm, string dryGauge, string dryWeight, string dippedWeight, string squareShapeLength, string squareShapeWidth, string roundShapeDiameter, string pktWeight, string biscuitsPerPkt, string ovenStartTime, string ovenStopTime, string ovenLossTime, string reason)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            string query = @"
                UPDATE TRN_FINAL_OVEN_REPORT
                SET BT_RPM = @BT_RPM,
                    Dry_Gauge = @Dry_Gauge,
                    Dry_Weight = @Dry_Weight,
                    Dipped_Weight = @Dipped_Weight,
                    Square_Shape_Length = @Square_Shape_Length,
                    Square_Shape_Width = @Square_Shape_Width,
                    Round_Shape_Diameter = @Round_Shape_Diameter,
                    PktWeight = @PktWeight,
                    BiscuitsPerPkt = @BiscuitsPerPkt,
                    Oven_Start_Time = @Oven_Start_Time,
                    Oven_Stop_Time = @Oven_Stop_Time,
                    Oven_Loss_Time = @Oven_Loss_Time,
                    Reason = @Reason
                WHERE OVN_Id = @OVN_Id";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    // Add parameters to the command
                    cmd.Parameters.AddWithValue("@OVN_Id", OVN_Id);
                    cmd.Parameters.AddWithValue("@BT_RPM", btRpm);
                    cmd.Parameters.AddWithValue("@Dry_Gauge", dryGauge);
                    cmd.Parameters.AddWithValue("@Dry_Weight", dryWeight);
                    cmd.Parameters.AddWithValue("@Dipped_Weight", dippedWeight);
                    cmd.Parameters.AddWithValue("@Square_Shape_Length", squareShapeLength);
                    cmd.Parameters.AddWithValue("@Square_Shape_Width", squareShapeWidth);
                    cmd.Parameters.AddWithValue("@Round_Shape_Diameter", roundShapeDiameter);
                    cmd.Parameters.AddWithValue("@PktWeight", pktWeight);
                    cmd.Parameters.AddWithValue("@BiscuitsPerPkt", biscuitsPerPkt);
                    cmd.Parameters.AddWithValue("@Oven_Start_Time", ovenStartTime);
                    cmd.Parameters.AddWithValue("@Oven_Stop_Time", ovenStopTime);
                    cmd.Parameters.AddWithValue("@Oven_Loss_Time", ovenLossTime); // Ensure this is declared and added
                    cmd.Parameters.AddWithValue("@Reason", reason);

                    try
                    {
                        connection.Open();
                        cmd.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        // Handle or log the exception
                        throw new Exception("Database update failed: " + ex.Message);
                    }
                }
            }
        }

        protected void btn_oven_reset_Click(object sender, EventArgs e)
        {
            // Clear all input fields
            TB_BakingTime.Text = string.Empty;
            TB_Gauge.Text = string.Empty;
            //dry weight
            TB_wgtwtoil.Text = string.Empty;
            //dipped weight
            TB_wgtwoil.Text = string.Empty;
            TB_Length.Text = string.Empty;
            TB_Width.Text = string.Empty;
            TB_Diameter.Text = string.Empty;
            //TB_PktWeight.Text = string.Empty;
            TB_BiscuitsPerPkt.Text = string.Empty;
            TB_StartTime.Text = string.Empty;
            TB_StopTime.Text = string.Empty;
            TB_Reason.Text = string.Empty;

            // Reset the label for success or error messages
            lbl_oven.Text = string.Empty;

        }
    }
}