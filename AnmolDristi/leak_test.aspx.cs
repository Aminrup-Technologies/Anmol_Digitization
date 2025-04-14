using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Configuration;
using AnmolDristi.DAL;
using System.Drawing;

namespace AnmolDristi
{
    public partial class leak_test : System.Web.UI.Page
    {
        public static String LSP_Id = String.Empty;
        string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        DB_Utility_OH4Y dbcl = new DB_Utility_OH4Y();

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

                    lbl_docname.Text = "QA - LEAK / SEAL & SLANTED PACK REPORT";
                    lbl_docnumber.Text = "ANMOL/DOC/DAN/QA/05 ";
                    PlantBinder();
                    DisplayCurrentShift();
                    PackingMCNo();

                }

            }
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
                BrandSKUBinder(selectedProductBrandValue);

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

        private void BrandSKUBinder(string selectedProductBrandValue)
        {
            string query = "SELECT SKUId, SKU_name FROM MST_Brand_SKU WHERE brand_id = @SelectedPlantValue";
            string textField = "SKU_name";
            string valueField = "SKUId";

            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_BrandSKU, textField, valueField, new SqlParameter("@SelectedPlantValue", selectedProductBrandValue), out recordsBound);

            if (!recordsBound)
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_PlantLine);

                string BrandSKUBinder_Error_script = @"<script type='text/javascript'>
              new PNotify({
                  title: 'Error',
                  text: 'An error occurred!',
                  type: 'error',
                  styling: 'bootstrap3'
              });
          </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowBrandSKUBinderErrorNotification", BrandSKUBinder_Error_script, false);
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

        private void PackingMCNo()
        {
            // Set properties of RequiredFieldValidator
            RFV_TB_PackingMCNo.ErrorMessage = "*";
            RFV_TB_PackingMCNo.ForeColor = System.Drawing.Color.Red;

            // Set properties of RegularExpressionValidator
            RFV_TB_PackingMCNo.ErrorMessage = "Invalid format";
            RFV_TB_PackingMCNo.ForeColor = System.Drawing.Color.Red;
            //RFV_TB_PackingMCNo.ValidationExpression = "^[a-zA-Z0-9\\s]+$"; // Regular expression to allow alphanumeric and spaces
        }


        private string Find_DBCode()
        {
            string newLspId = null;
            dbcl.Sqlconnection();
            dbcl.ConnectDb();

            try
            {
                string query = "SELECT LSP_Id FROM TRN_LeakSealSlanted_Data WHERE Id = (SELECT MAX(Id) FROM TRN_LeakSealSlanted_Data)";
                SqlCommand command = new SqlCommand(query, dbcl.Conn);
                SqlDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    string lastLspId = reader["LSP_Id"].ToString();
                    // Extract the numeric part from the LSP_Id (Assumes LSP is always 3 characters long)
                    string numericPart = lastLspId.Substring(3);
                    int numericValue = Convert.ToInt32(numericPart);

                    // Increment the numeric part
                    numericValue++;

                    // Generate the new LSP_Id, preserving the "LSP" prefix and ensuring proper zero-padding
                    newLspId = "LSP" + numericValue.ToString("D2"); // D2 ensures 2 digits (e.g., 09 -> 10)
                }
                else
                {
                    // If there are no records, start with the initial LSP001
                    newLspId = "LSP01"; // Adjusted to start from LSP01 to match the two-digit pattern
                }

                reader.Close();
            }
            catch (Exception ex)
            {
                // Handle exception (log it, rethrow it, etc.)
                throw ex;
            }
            finally
            {
                dbcl.DisconnectDb();
            }

            LSP_Id = newLspId;
            return newLspId;
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
                    cmd.Parameters.AddWithValue("@FormID", 7);
                    cmd.Parameters.AddWithValue("@FormName", "leak_test");

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        hdn_formid.Value = "7";
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
                            dbcl.InsertDefaultApprovers(selectedPlantValue, selectedPlantLineValue, 7);

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
                    cmd.Parameters.AddWithValue("@LineId", selectedPlantLineValue);
                    cmd.Parameters.AddWithValue("@FormID", 7);
                    cmd.Parameters.AddWithValue("@FormName", "leak_test");

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        hdn_formid.Value = "7";
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
                            bool isInserted = dbcl.InsertDefaultApprovers(selectedPlantValue, selectedPlantLineValue, 7);

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

        protected void btnBDLeakSave_Click(object sender, EventArgs e)
        {
            try
            {
                // Retrieve values from controls
                string plantName = DDL_Plant.SelectedValue;
                string plantLine = DDL_PlantLine.SelectedValue;
                string productCategory = DDL_ProductCategory.SelectedValue;
                string productBrand = DDL_ProductBrand.SelectedValue;
                string brandSKU = DDL_BrandSKU.SelectedValue;
                string shift = hdn_shiftvalue.Value.ToString();
                int formID = Convert.ToInt32(hdn_formid.Value.ToString());

                int submittedById = Convert.ToInt32(Convert.ToInt32(Session["USERID"].ToString()));
                string submittedByEmployeeCode = Session["WORKMAN"].ToString();

                string approver1EmployeeCode = Approver1CodeLabel.Text.ToString();
                string approver2EmployeeCode = Approver2CodeLabel.Text.ToString();
                string dottedLineApproverEmployeeCode = DottedLineApproverCodeLabel.Text.ToString();

                string lspId = Find_DBCode();
                DateTime submittedDate = DateTime.Now.Date;  // Current Date
                TimeSpan submittedTime = DateTime.Now.TimeOfDay;  // Current Time

                string packingMCNo = TB_PackingMCNo.Text.ToString();
                int leakTestStatus = Convert.ToInt32(RBL_PassFail.SelectedValue);
                string remarksForFail = TXB_PassFail_Remarks.Text;
                decimal? slantedPercent = string.IsNullOrEmpty(TB_PercentageSlanted.Text) ? (decimal?)null : Convert.ToDecimal(TB_PercentageSlanted.Text);
                //int? approver1Status = string.IsNullOrEmpty(txtApprover1Status.Text) ? (int?)null : Convert.ToInt32(txtApprover1Status.Text);
                //DateTime? approver1TimeStamp = string.IsNullOrEmpty(txtApprover1TimeStamp.Text) ? (DateTime?)null : Convert.ToDateTime(txtApprover1TimeStamp.Text);
                //int? approver2Status = string.IsNullOrEmpty(txtApprover2Status.Text) ? (int?)null : Convert.ToInt32(txtApprover2Status.Text);
                //DateTime? approver2TimeStamp = string.IsNullOrEmpty(txtApprover2TimeStamp.Text) ? (DateTime?)null : Convert.ToDateTime(txtApprover2TimeStamp.Text);
                //int? dottedApproverStatus = string.IsNullOrEmpty(txtDottedApproverStatus.Text) ? (int?)null : Convert.ToInt32(txtDottedApproverStatus.Text);
                //DateTime? dottedApproverTimeStamp = string.IsNullOrEmpty(txtDottedApproverTimeStamp.Text) ? (DateTime?)null : Convert.ToDateTime(txtDottedApproverTimeStamp.Text);

                // Call the InsertLeakSealSlantedData method to insert the data
                InsertLeakSealSlantedData(formID, lspId, submittedById, submittedByEmployeeCode, submittedDate,
                    submittedTime, shift, plantName, plantLine, productCategory, productBrand, brandSKU, packingMCNo,
                    leakTestStatus, remarksForFail, slantedPercent, approver1EmployeeCode, approver2EmployeeCode, dottedLineApproverEmployeeCode);

            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                string errorScript = $"new PNotify({{ title: 'Error', text: '{errorMessage}', type: 'error', styling: 'bootstrap3' }});";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowErrorNotification", errorScript, true);
                // Handle the exception (log it, display an error message, etc.)
                //lblMessage.Text = "Error: " + ex.Message;
            }

        }

        public void InsertLeakSealSlantedData(
    int formID, string lspId, int submittedById, string submittedByEmployeeCode, DateTime submittedDate,
    TimeSpan submittedTime, string shift, string plantName, string line, string productCategory,
    string productBrand, string skuId, string packingMCNo, int leakTestStatus, string remarksForFail,
    decimal? slantedPercent, string approver1EmployeeCode, string approver2EmployeeCode, string dottedLineApproverEmployeeCode)
        {
            // Define your connection string (stored in Web.config or App.config)
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            // SQL query for inserting data
            string query = @"INSERT INTO [dbo].[TRN_LeakSealSlanted_Data] 
                        (FormID, LSP_Id, SubmittedById, SubmittedByEmployeeCode, SubmittedDate, 
                        SubmittedTime, Shift, PlantName, Line, ProductCategory, ProductBrand, SKUId, 
                        Packing_MC_No, LeakTestStatus, RemarksForFail, Slanted_Percent, 
                        Approver1EmployeeCode, Approver2EmployeeCode, DottedLineApproverEmployeeCode)
                    VALUES 
                        (@FormID, @LSP_Id, @SubmittedById, @SubmittedByEmployeeCode, @SubmittedDate, 
                        @SubmittedTime, @Shift, @PlantName, @Line, @ProductCategory, @ProductBrand, @SKUId, 
                        @Packing_MC_No, @LeakTestStatus, @RemarksForFail, @Slanted_Percent, 
                        @Approver1EmployeeCode, @Approver2EmployeeCode, @DottedLineApproverEmployeeCode)";

            // Create a connection to the database
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                // Create a command object
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    // Add parameters to the command object
                    cmd.Parameters.AddWithValue("@FormID", formID);
                    cmd.Parameters.AddWithValue("@LSP_Id", lspId);
                    cmd.Parameters.AddWithValue("@SubmittedById", submittedById);
                    cmd.Parameters.AddWithValue("@SubmittedByEmployeeCode", submittedByEmployeeCode);
                    cmd.Parameters.AddWithValue("@SubmittedDate", submittedDate);
                    cmd.Parameters.AddWithValue("@SubmittedTime", submittedTime);
                    cmd.Parameters.AddWithValue("@Shift", shift);
                    cmd.Parameters.AddWithValue("@PlantName", plantName);
                    cmd.Parameters.AddWithValue("@Line", line);
                    cmd.Parameters.AddWithValue("@ProductCategory", productCategory);
                    cmd.Parameters.AddWithValue("@ProductBrand", productBrand);
                    cmd.Parameters.AddWithValue("@SKUId", skuId);
                    cmd.Parameters.AddWithValue("@Packing_MC_No", packingMCNo);
                    cmd.Parameters.AddWithValue("@LeakTestStatus", leakTestStatus);
                    cmd.Parameters.AddWithValue("@RemarksForFail", remarksForFail ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Slanted_Percent", slantedPercent.HasValue ? (object)slantedPercent.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Approver1EmployeeCode", approver1EmployeeCode ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Approver2EmployeeCode", approver2EmployeeCode ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@DottedLineApproverEmployeeCode", dottedLineApproverEmployeeCode ?? (object)DBNull.Value);

                    // Open the connection
                    conn.Open();

                    // Execute the query
                    cmd.ExecuteNonQuery();

                    DDL_Plant.Enabled = false;
                    DDL_PlantLine.Enabled = false;
                    DDL_ProductCategory.Enabled = false;
                    DDL_ProductBrand.Enabled = false;
                    DDL_BrandSKU.Enabled = false;

                    TB_PercentageSlanted.ReadOnly = true;

                    btnBDLeakSave.Enabled = false;
                    btnBDLeakSave.Text = "SAVED";

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

        private void PercentageSlanted()
        {
            // Set properties of RequiredFieldValidator
            RFV_TB_PercentageSlanted.ErrorMessage = "*";
            RFV_TB_PercentageSlanted.ForeColor = System.Drawing.Color.Red;

            // Set properties of RangeValidator
            RFV_TB_PercentageSlanted.ErrorMessage = "Value should be between 0 and 100";
            RFV_TB_PercentageSlanted.ForeColor = System.Drawing.Color.Red;
            //RV_TB_PercentageSlanted.MinimumValue = "0";
            //RV_TB_PercentageSlanted.MaximumValue = "100";
        }


        private string GenerateUniqueLSPId()
        {
            string newLSPId;
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Query to get the maximum LSP_Id and increment it
                    string query = "SELECT ISNULL(MAX(CAST(SUBSTRING(LSP_Id, 4, LEN(LSP_Id)) AS INT)), 0) FROM [dbo].[Leak_Test_Data]";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        object result = command.ExecuteScalar();
                        int maxLSPValue = Convert.ToInt32(result);

                        // Increment and format the new LSP_Id
                        int numericPart = maxLSPValue + 1;
                        newLSPId = $"LSP{numericPart:D3}"; // Example format: LSP001, LSP002
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error generating LSP_Id: " + ex.Message);
                throw;
            }
            LSP_Id = newLSPId;
            return newLSPId;
        }

        // Example method to generate LSP Id, replace this with actual logic
        private string GenerateLSPId()
        {
            return "LSP" + DateTime.Now.Ticks.ToString();
        }
        // Method to generate unique LSP_Id
        private string GenerateUniqueLspId()
        {
            string uniqueLspId = "";
            int nextNumber = 1;

            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            string query = "SELECT MAX(LSP_Id) FROM Leak_Test_Data";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    try
                    {
                        connection.Open();
                        object result = command.ExecuteScalar();

                        if (result != DBNull.Value && result != null)
                        {
                            string lastLspId = result.ToString();
                            nextNumber = int.Parse(lastLspId.Substring(3)) + 1;
                        }

                        uniqueLspId = "LSP" + nextNumber.ToString("D2");
                    }
                    catch (Exception ex)
                    {
                        // Handle exceptions
                        throw new Exception("Error generating LSP_Id: " + ex.Message);
                    }
                }
            }

            return uniqueLspId;
        }

        private void DisplayCurrentShift()
        {
            ShiftManager siftManager = new ShiftManager();
            string currentShift = siftManager.GetCurrentShiftType();
            hdn_shiftvalue.Value = currentShift;
        }


        protected void btnBDLeakReset_Click(object sender, EventArgs e)
        {
            // Reset Dropdowns to default values
            DDL_Plant.SelectedIndex = 0;
            DDL_PlantLine.SelectedIndex = 0;
            DDL_ProductCategory.SelectedIndex = 0;
            DDL_ProductBrand.SelectedIndex = 0;
            DDL_BrandSKU.SelectedIndex = 0;

            // Reset TextBoxes to empty
            TB_PackingMCNo.Text = string.Empty;
            TXB_PassFail_Remarks.Text = string.Empty;
            TB_PercentageSlanted.Text = string.Empty;

            // Reset RadioButtonList
            RBL_PassFail.ClearSelection();

            // Reset hidden fields and any other additional controls
            hdn_shiftvalue.Value = string.Empty;

            // Reset the save button state if necessary
            btnBDLeakSave.Enabled = true;
            btnBDLeakSave.Text = "Save";
            btnBDLeakSave.CssClass = "btn btn-sm btn-primary";

            // Optionally reset any other controls on the form
        }





    }
}