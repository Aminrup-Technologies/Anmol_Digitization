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
    public partial class qaqc_leaktest_rev1 : System.Web.UI.Page
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
                    DDL_BrandSKU.Enabled = false;
                    btn_AddList.Enabled = false;
                    btn_resetgrid.Enabled = false;


                    lbl_docname.Text = "QA - LEAK / SEAL & SLANTED PACK REPORT";
                    lbl_docnumber.Text = "ANMOL/DOC/DAN/QA/05 ";
                    PlantBinder();
                    DisplayCurrentShift();
                    GetGridViewData(); // Initialize DataTable on first load
                    BindGridView();
                    // Set the default state for the hidden field if needed
                    hfLockState.Value = "false"; // Default to unlocked
                }

            }
            else
            {
                // Read the lock state from the hidden field
                bool isLocked = hfLockState.Value == "true";

                // Lock or unlock fields based on the stored state
                LockUnlockFields(isLocked);
            }
        }

        private void LockUnlockFields(bool isLocked)
        {
            // Lock/unlock fields based on the value of isLocked
            DDL_Plant.Enabled = !isLocked;
            DDL_PlantLine.Enabled = !isLocked;
            DDL_ProductCategory.Enabled = !isLocked;
            DDL_ProductBrand.Enabled = !isLocked;
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

        //private void PackingMCNo()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_PackingMCNo.ErrorMessage = "*";
        //    RFV_TB_PackingMCNo.ForeColor = System.Drawing.Color.Red;

        //    // Set properties of RegularExpressionValidator
        //    RFV_TB_PackingMCNo.ErrorMessage = "Invalid format";
        //    RFV_TB_PackingMCNo.ForeColor = System.Drawing.Color.Red;
        //    //RFV_TB_PackingMCNo.ValidationExpression = "^[a-zA-Z0-9\\s]+$"; // Regular expression to allow alphanumeric and spaces
        //}

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
                    //cmd.Parameters.AddWithValue("@LineId", selectedPlantLineValue);
                    cmd.Parameters.AddWithValue("@LineId", string.IsNullOrEmpty(selectedPlantLineValue) ? (object)DBNull.Value : selectedPlantLineValue);
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
        //private void PercentageSlanted()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_PercentageSlanted.ErrorMessage = "*";
        //    RFV_TB_PercentageSlanted.ForeColor = System.Drawing.Color.Red;

        //    // Set properties of RangeValidator
        //    RFV_TB_PercentageSlanted.ErrorMessage = "Value should be between 0 and 100";
        //    RFV_TB_PercentageSlanted.ForeColor = System.Drawing.Color.Red;
        //    //RV_TB_PercentageSlanted.MinimumValue = "0";
        //    //RV_TB_PercentageSlanted.MaximumValue = "100";
        //}

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

        protected void btnSubmit_Click(object sender, EventArgs e)
        {

        }

        private void InsertDataToDatabase(string skuType, string packingMCNo, string leakTest, string comments, string slantedPack)
        {
            // Your database insert logic here
            string query = "INSERT INTO YourTable (SKUType, PackingMCNo, LeakTest, Comments, SlantedPack) VALUES (@SKUType, @PackingMCNo, @LeakTest, @Comments, @SlantedPack)";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@SKUType", skuType);
                cmd.Parameters.AddWithValue("@PackingMCNo", packingMCNo);
                cmd.Parameters.AddWithValue("@LeakTest", leakTest);
                cmd.Parameters.AddWithValue("@Comments", comments);
                cmd.Parameters.AddWithValue("@SlantedPack", slantedPack);

                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        protected void btn_AddList_Click(object sender, EventArgs e)
        {
            try
            {
                // Retrieve the DataTable from ViewState
                DataTable dtCurrentTable = ViewState["GridData"] as DataTable;

                if (dtCurrentTable != null)
                {
                    // Create a new row
                    DataRow dr = dtCurrentTable.NewRow();
                    dr["Plant"] = DDL_Plant.SelectedItem.Text;
                    dr["PlantId"] = DDL_Plant.SelectedValue;
                    dr["PlantLine"] = DDL_PlantLine.SelectedItem.Text;
                    dr["PlantLineId"] = DDL_PlantLine.SelectedValue;
                    dr["ProductCategory"] = DDL_ProductCategory.SelectedItem.Text;
                    dr["ProductCategoryId"] = DDL_ProductCategory.SelectedValue;
                    dr["ProductBrand"] = DDL_ProductBrand.SelectedItem.Text;
                    dr["ProductBrandId"] = DDL_ProductBrand.SelectedValue;
                    dr["BrandSKU"] = DDL_BrandSKU.SelectedItem.Text;
                    dr["BrandSKUId"] = DDL_BrandSKU.SelectedValue;
                    dr["PackingMCNo"] = TB_PackingMCNo.Text;
                    dr["LeakTestSealIntegrity"] = RBL_PassFail.SelectedItem.Text;
                    dr["PassFailRemarks"] = TXB_PassFail_Remarks.Text;
                    dr["PercentageSlanted"] = TB_PercentageSlanted.Text;

                    // Add the row to the DataTable
                    dtCurrentTable.Rows.Add(dr);

                    // Save the updated DataTable back to ViewState
                    ViewState["GridData"] = dtCurrentTable;

                    DDL_BrandSKU.Enabled = true;
                    btn_AddList.Enabled = true;
                    btn_resetgrid.Enabled = true;

                    final_save_btns.Visible = true;
                    // Rebind the GridView
                    BindGridView();

                    // Clear the form inputs
                    ClearFormInputs();

                    // Show success message
                    lbl_message.Text = "Row added successfully.";
                    lbl_message.ForeColor = System.Drawing.Color.Green;
                }
            }
            catch (Exception ex)
            {
                lbl_message.Text = "Error: " + ex.Message;
                lbl_message.ForeColor = System.Drawing.Color.Red;
            }
        }



        private DataTable GetGridViewData()
        {
            if (ViewState["GridData"] == null)
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("Plant");
                dt.Columns.Add("PlantId");
                dt.Columns.Add("PlantLine");
                dt.Columns.Add("PlantLineId");
                dt.Columns.Add("ProductCategory");
                dt.Columns.Add("ProductCategoryId");
                dt.Columns.Add("ProductBrand");
                dt.Columns.Add("ProductBrandId");
                dt.Columns.Add("BrandSKU");
                dt.Columns.Add("BrandSKUId");
                dt.Columns.Add("PackingMCNo");
                dt.Columns.Add("LeakTestSealIntegrity");
                dt.Columns.Add("PassFailRemarks");
                dt.Columns.Add("PercentageSlanted");

                ViewState["GridData"] = dt;
            }

            return ViewState["GridData"] as DataTable;
        }

        private void DisableFormInputs()
        {
            // Disable all the form controls
            DDL_BrandSKU.Enabled = false;  // Disable dropdown
            TB_PackingMCNo.Enabled = false;  // Disable TextBox
            RBL_PassFail.Enabled = false;  // Disable RadioButtonList
            TXB_PassFail_Remarks.Enabled = false;  // Disable TextBox
            TB_PercentageSlanted.Enabled = false;  // Disable TextBox

            AddMoreDiv.Visible = false;
        }



        private void ClearFormInputs()
        {
            // Reset the form controls after adding the data
            DDL_BrandSKU.SelectedIndex = 0;  // Reset dropdown
            TB_PackingMCNo.Text = "";
            RBL_PassFail.SelectedIndex = -1;
            TXB_PassFail_Remarks.Text = "";
            TB_PercentageSlanted.Text = "";
        }

        protected void btn_resetgrid_Click(object sender, EventArgs e)
        {
            // Clear the form inputs and reset the GridView
            ClearFormInputs();

            // Optionally, reset GridView data if you want to clear it
            Session["GridData"] = null;
            gvData.DataSource = null;
            gvData.DataBind();
        }
        private void BindGridView()
        {
            if (ViewState["GridData"] != null)
            {
                DataTable dt = (DataTable)ViewState["GridData"];

                if ((dt != null) && (dt.Rows.Count > 0))
                {
                    gvData.Visible = true;
                    gvData.DataSource = dt;
                    gvData.DataBind();
                }
                else
                {
                    gvData.Visible = false;
                }
            }
        }

        protected void gvData_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            // Ensure DataTable is initialized
            GetGridViewData();

            try
            {
                // Retrieve the DataTable from ViewState
                DataTable dt = ViewState["GridData"] as DataTable;

                if (dt != null)
                {
                    // Remove the row at the specified index
                    dt.Rows[e.RowIndex].Delete();

                    // Save the updated DataTable back to ViewState
                    dt.AcceptChanges();
                    ViewState["GridData"] = dt;

                    // Rebind the GridView
                    BindGridView();

                    // Show success message
                    lbl_message.Text = "Row deleted successfully.";
                    lbl_message.ForeColor = System.Drawing.Color.Green;
                }
            }
            catch (Exception ex)
            {
                lbl_message.Text = "Error: " + ex.Message;
                lbl_message.ForeColor = System.Drawing.Color.Red;
            }
        }


        private void InsertGridViewDataToDatabase()
        {
            try
            {
                // Ensure the DataTable is available
                GetGridViewData();

                // Retrieve the DataTable from ViewState
                DataTable dt = ViewState["GridData"] as DataTable;

                if (dt != null && dt.Rows.Count > 0)
                {
                    using (SqlConnection con = new SqlConnection("your_connection_string_here"))
                    {
                        con.Open();

                        // Prepare the SQL Insert Command
                        string query = @"
                INSERT INTO YourTableName 
                (Plant, PlantId, PlantLine, PlantLineId, ProductCategory, ProductCategoryId, 
                 ProductBrand, ProductBrandId, BrandSKU, BrandSKUId, PackingMCNo, 
                 LeakTestSealIntegrity, PassFailRemarks, PercentageSlanted)
                VALUES
                (@Plant, @PlantId, @PlantLine, @PlantLineId, @ProductCategory, @ProductCategoryId, 
                 @ProductBrand, @ProductBrandId, @BrandSKU, @BrandSKUId, @PackingMCNo, 
                 @LeakTestSealIntegrity, @PassFailRemarks, @PercentageSlanted)";

                        foreach (DataRow row in dt.Rows)
                        {
                            using (SqlCommand cmd = new SqlCommand(query, con))
                            {
                                // Add parameters from DataRow
                                cmd.Parameters.AddWithValue("@Plant", row["Plant"]);
                                cmd.Parameters.AddWithValue("@PlantId", row["PlantId"]);
                                cmd.Parameters.AddWithValue("@PlantLine", row["PlantLine"]);
                                cmd.Parameters.AddWithValue("@PlantLineId", row["PlantLineId"]);
                                cmd.Parameters.AddWithValue("@ProductCategory", row["ProductCategory"]);
                                cmd.Parameters.AddWithValue("@ProductCategoryId", row["ProductCategoryId"]);
                                cmd.Parameters.AddWithValue("@ProductBrand", row["ProductBrand"]);
                                cmd.Parameters.AddWithValue("@ProductBrandId", row["ProductBrandId"]);
                                cmd.Parameters.AddWithValue("@BrandSKU", row["BrandSKU"]);
                                cmd.Parameters.AddWithValue("@BrandSKUId", row["BrandSKUId"]);
                                cmd.Parameters.AddWithValue("@PackingMCNo", row["PackingMCNo"]);
                                cmd.Parameters.AddWithValue("@LeakTestSealIntegrity", row["LeakTestSealIntegrity"]);
                                cmd.Parameters.AddWithValue("@PassFailRemarks", row["PassFailRemarks"]);
                                cmd.Parameters.AddWithValue("@PercentageSlanted", row["PercentageSlanted"]);

                                // Execute the command
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    lbl_message.Text = "Data inserted successfully into the database.";
                    lbl_message.ForeColor = System.Drawing.Color.Green;
                }
                else
                {
                    lbl_message.Text = "No data available in the grid to insert.";
                    lbl_message.ForeColor = System.Drawing.Color.Red;
                }
            }
            catch (Exception ex)
            {
                lbl_message.Text = "Error: " + ex.Message;
                lbl_message.ForeColor = System.Drawing.Color.Red;
            }
        }

        protected void btn_svall_Click(object sender, EventArgs e)
        {
            InsertAllLeakSealSlantedDataFromGrid();
        }

        public bool IsLSPIdDuplicate(string lspId)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            string query = @"SELECT COUNT(1) FROM [dbo].[TRN_LeakSealSlanted_Data] WHERE LSP_Id = @LSP_Id";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@LSP_Id", lspId);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        public void InsertAllLeakSealSlantedDataFromGrid()
        {
            try
            {
                // Ensure there is data in ViewState for gvData
                if (ViewState["GridData"] != null)
                {
                    DataTable dtGridData = ViewState["GridData"] as DataTable;
                    if (dtGridData.Rows.Count > 0)
                    {
                        string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                        string shift = hdn_shiftvalue.Value.ToString();
                        int formID = Convert.ToInt32(hdn_formid.Value.ToString());

                        int submittedById = Convert.ToInt32(Session["USERID"].ToString());
                        string submittedByEmployeeCode = Session["WORKMAN"].ToString();

                        string approver1EmployeeCode = Approver1CodeLabel.Text.ToString();
                        string approver2EmployeeCode = Approver2CodeLabel.Text.ToString();
                        string dottedLineApproverEmployeeCode = DottedLineApproverCodeLabel.Text.ToString();

                        
                        string batchId = string.Format("{0}{1}{2}{3}", DateTime.Now.ToString("yyMMddHHmmss"), shift, formID, submittedByEmployeeCode);

                        string query = @"INSERT INTO [dbo].[TRN_LeakSealSlanted_Data] 
                                        (FormID, LSP_Id, SubmittedById, SubmittedByEmployeeCode, SubmittedDate, 
                                        SubmittedTime, Shift, PlantName, Line, ProductCategory, ProductBrand, SKUId, 
                                        Packing_MC_No, LeakTestStatus, RemarksForFail, Slanted_Percent, 
                                        Approver1EmployeeCode, Approver2EmployeeCode, DottedLineApproverEmployeeCode, BatchID)
                                    VALUES 
                                        (@FormID, @LSP_Id, @SubmittedById, @SubmittedByEmployeeCode, @SubmittedDate, 
                                        @SubmittedTime, @Shift, @PlantName, @Line, @ProductCategory, @ProductBrand, @SKUId, 
                                        @Packing_MC_No, @LeakTestStatus, @RemarksForFail, @Slanted_Percent, 
                                        @Approver1EmployeeCode, @Approver2EmployeeCode, @DottedLineApproverEmployeeCode, @BatchID)";

                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            conn.Open();

                            foreach (DataRow row in dtGridData.Rows)
                            {
                                using (SqlCommand cmd = new SqlCommand(query, conn))
                                {
                                    // Extract values from DataTable row
                                    string plantName = row["PlantId"].ToString();
                                    string plantLine = row["PlantLineId"].ToString();
                                    string productCategory = row["ProductCategoryId"].ToString();
                                    string productBrand = row["ProductBrandId"].ToString();
                                    string brandSKU = row["BrandSKUId"].ToString();
                                    string packingMCNo = row["PackingMCNo"].ToString().Trim().ToUpper();
                                    int leakTestStatus = row["LeakTestSealIntegrity"].ToString().Trim().ToUpper() == "OK" ? 1 : 0;
                                    //int leakTestStatus = Convert.ToInt32(row["LeakTestSealIntegrity"]);
                                    string remarksForFail = row["PassFailRemarks"]?.ToString();
                                    decimal? slantedPercent = string.IsNullOrEmpty(row["PercentageSlanted"].ToString())
                                        ? (decimal?)null
                                        : Convert.ToDecimal(row["PercentageSlanted"]);

                                    string lspId = Find_DBCode(); // Get LSP_Id value
                                    if (IsLSPIdDuplicate(lspId)) // Check if the LSP_Id is already in the database
                                    {
                                        // Handle duplicate, you can either skip or update the record
                                        // For example, you could generate a new LSP_Id here if needed.
                                        lspId = Find_DBCode(); // Implement this logic as required
                                    }

                                    // Add parameters to SQL command
                                    cmd.Parameters.AddWithValue("@FormID", formID);
                                    cmd.Parameters.AddWithValue("@LSP_Id", lspId);
                                    cmd.Parameters.AddWithValue("@SubmittedById", submittedById);
                                    cmd.Parameters.AddWithValue("@SubmittedByEmployeeCode", submittedByEmployeeCode);
                                    cmd.Parameters.AddWithValue("@SubmittedDate", DateTime.Now.Date);
                                    cmd.Parameters.AddWithValue("@SubmittedTime", DateTime.Now.TimeOfDay);
                                    cmd.Parameters.AddWithValue("@Shift", shift);
                                    cmd.Parameters.AddWithValue("@PlantName", plantName);
                                    cmd.Parameters.AddWithValue("@Line", plantLine);
                                    cmd.Parameters.AddWithValue("@ProductCategory", productCategory);
                                    cmd.Parameters.AddWithValue("@ProductBrand", productBrand);
                                    cmd.Parameters.AddWithValue("@SKUId", brandSKU);
                                    cmd.Parameters.AddWithValue("@Packing_MC_No", packingMCNo);
                                    cmd.Parameters.AddWithValue("@LeakTestStatus", leakTestStatus);
                                    cmd.Parameters.AddWithValue("@RemarksForFail", remarksForFail ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Slanted_Percent", slantedPercent.HasValue ? (object)slantedPercent.Value : DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Approver1EmployeeCode", approver1EmployeeCode ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Approver2EmployeeCode", approver2EmployeeCode ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DottedLineApproverEmployeeCode", dottedLineApproverEmployeeCode ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@BatchID", batchId);
                                    // Execute the query
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        // Show success notification
                        string Data_SuccessScript = @"<script type='text/javascript'>
                                new PNotify({
                                    title: 'Data Success',
                                    text: 'All records have been saved successfully!',
                                    type: 'success',
                                    styling: 'bootstrap3'
                                });
                            </script>";
                        ClientScript.RegisterStartupScript(this.GetType(), "ShowDataSuccessNotification", Data_SuccessScript, false);

                        // Optionally clear the GridView and ViewState
                        //ViewState["GridData"] = null;
                        //gvData.DataSource = null;
                        //gvData.DataBind();

                        DisableFormInputs();
                    }
                    else
                    {
                        lbl_svall_msg.Text = "No data available in the grid to insert.";
                        lbl_svall_msg.ForeColor = System.Drawing.Color.Red;
                    }
                }
                else
                {
                    lbl_svall_msg.Text = "No data available in ViewState.";
                    lbl_svall_msg.ForeColor = System.Drawing.Color.Red;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                lbl_svall_msg.Text = "Error: " + ex.Message;
                lbl_svall_msg.ForeColor = System.Drawing.Color.Red;
            }
        }

        protected void btn_resetgrid_Click1(object sender, EventArgs e)
        {
            ClearFormInputs();
        }
    }
}