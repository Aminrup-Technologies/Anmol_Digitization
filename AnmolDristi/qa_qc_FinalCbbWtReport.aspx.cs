using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using static AnmolDristi.qaqc.qaqc_inspector_rpt;
using Newtonsoft.Json;
using System.Configuration;
using AnmolDristi.DAL;
using System.Web.Script.Serialization;

namespace AnmolDristi
{
    public partial class qa_qc_FinalCbbWtReport : System.Web.UI.Page
    {
        public static string CBB_key = String.Empty;
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

                    lbl_docname.Text = "FINAL CBB WEIGHT CHECKLIST";
                    lbl_docnumber.Text = "ANMOL/DOC/CORP/QA/08";
                    PlantBinder();
                    int gridBinderValue = int.Parse(ConfigurationManager.AppSettings["FinalCBBWeight_GridBinderValue"]);
                    GridBinder(gridBinderValue);
                    DisplayCurrentShift();

                }
            }

        }

        private void DisplayCurrentShift()
        {
            ShiftManager shiftManager = new ShiftManager();
            string currentShift = shiftManager.GetCurrentShiftType();
            hdn_shiftvalue.Value = currentShift;
        }

        private void GridBinder(int rowCount)
        {
            DataTable dt = new DataTable();

            // Create two columns: Sl and GrossWeightValue
            dt.Columns.Add("Sl");
            dt.Columns.Add("GrossWeightValue");

            for (int i = 1; i <= rowCount; i++)
            {
                DataRow dr = dt.NewRow();
                dr["Sl"] = i; // Serial number (Sl)
                dt.Rows.Add(dr);
            }

            GridView1.DataSource = dt;
            GridView1.DataBind();
        }

        public class WeightData
        {
            public int sl { get; set; }
            public decimal weight { get; set; }
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {

            if (Btn_Save.Text== "SAVED" && CBB_key != string.Empty)
            {
                try
                {
                    // Create a list to hold WeightData objects
                    List<WeightData> weightDataList = new List<WeightData>();

                    // Iterate through GridView rows and populate the list
                    foreach (GridViewRow row in GridView1.Rows)
                    {
                        // Assuming you have a TextBox for gross weight
                        TextBox txtGrossWeight = (TextBox)row.FindControl("txtGrossWeight");

                        if (txtGrossWeight != null && !string.IsNullOrEmpty(txtGrossWeight.Text))
                        {
                            decimal grossWeight;
                            if (decimal.TryParse(txtGrossWeight.Text, out grossWeight))
                            {
                                int sl = row.RowIndex + 1; // Serial number
                                weightDataList.Add(new WeightData
                                {
                                    sl = sl,
                                    weight = grossWeight
                                });
                            }
                        }
                    }

                    string jsonData = JsonConvert.SerializeObject(weightDataList);


                    string avgGrossWgt = lblAvgWeights.Text.ToString();
                    //string jsonData = JsonConvert.SerializeObject(grossWeightData);
                    UpdateGrossWeightsInDatabase(jsonData, avgGrossWgt);

                    BindDataToGridView(jsonData);
                }
                catch (Exception ex)
                {

                    //throw;
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



            //List<Dictionary<string, object>> recordsList = new List<Dictionary<string, object>>();
            ////Dictionary<int, decimal> grossWeightData = new Dictionary<int, decimal>();

            //foreach (GridViewRow row in GridView1.Rows)
            //{
            //    TextBox txtGrossWeight = (TextBox)row.FindControl("txtGrossWeight");

            //    if (txtGrossWeight != null && !string.IsNullOrEmpty(txtGrossWeight.Text))
            //    {
            //        decimal grossWeight;
            //        if (decimal.TryParse(txtGrossWeight.Text, out grossWeight))
            //        {
            //            int sl = row.RowIndex + 1;
            //            //grossWeightData.Add(sl, grossWeight);

            //            Dictionary<string, object> record = new Dictionary<string, object>
            //            {
            //                { "sl", sl },
            //                { "weight", grossWeight }
            //            };
            //            recordsList.Add(record);
            //        }
            //    }
            //}
            //JavaScriptSerializer serializer = new JavaScriptSerializer();
            //string jsonString = serializer.Serialize(recordsList);

            
        }

        protected void BindDataToGridView(string jsonString)
        {
            // Deserialize the JSON string into a dictionary
            List<WeightData> weightDataList = JsonConvert.DeserializeObject<List<WeightData>>(jsonString);

            // Create a DataTable and define its columns
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("SlNo", typeof(int));
            dataTable.Columns.Add("GrossWeight", typeof(decimal));

            // Populate the DataTable with data from the list
            foreach (var item in weightDataList)
            {
                DataRow row = dataTable.NewRow();
                row["SlNo"] = item.sl;
                row["GrossWeight"] = item.weight;
                dataTable.Rows.Add(row);
            }

            // Bind the DataTable to the GridView
            yourGridView.DataSource = dataTable;
            yourGridView.DataBind();
        }

        private void UpdateGrossWeightsInDatabase(String jsonData, string avgGrossWgt)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE TRN_FINAL_CBB_Weights SET GrossWeightJson = @GrossWeightJson, AverageGrossWeight=@AverageGrossWeight WHERE CBB_PK = @cbb_pk";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@GrossWeightJson", jsonData);
                        cmd.Parameters.AddWithValue("@AverageGrossWeight", avgGrossWgt);
                        cmd.Parameters.AddWithValue("@cbb_pk", CBB_key);
                        cmd.ExecuteNonQuery();

                        RowCount_DIV.Visible = false;
                        GridView1.Visible = false;
                        //AvgWt_TB.Visible = false;

                        yourGridView.Visible = true;
                        btnSubmit.Enabled = false;
                        btnSubmit.Text = "SAVED";
                        Label6.Text = "Weights Captured";

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

        private void PopulateGridView()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            // Create a DataTable to hold the data
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("SlNo", typeof(int));
            dataTable.Columns.Add("GrossWeight", typeof(decimal));

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                // Query to fetch the data from the database
                string query = "SELECT CBB_PK, GrossWeightJson FROM FINAL_CBB_basic_data";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DataRow row = dataTable.NewRow();
                            string cbbPk = reader["CBB_PK"].ToString();

                            // Assuming CBB_PK is in the format "CBB_PK001", "CBB_PK002", etc.
                            int slNo = int.Parse(cbbPk.Substring(7));  // Extract the numeric part

                            row["SlNo"] = slNo;
                            row["GrossWeightJson"] = reader["GrossWeightJson"];
                            dataTable.Rows.Add(row);
                        }
                    }
                }
            }

            // Bind the DataTable to the GridView
            yourGridView.DataSource = dataTable;
            yourGridView.DataBind();
        }

        private string GenerateUniqueCBB_PK()
        {
            string newCbbPkValue;
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Fetch the maximum numeric part of CBB_PK safely
                    string query = @"
                SELECT ISNULL(MAX(CAST(SUBSTRING(CBB_PK, 7, LEN(CBB_PK) - 6) AS INT)), 0) 
                FROM FINAL_CBB_basic_data
                WHERE LEN(CBB_PK) >= 7 AND ISNUMERIC(SUBSTRING(CBB_PK, 7, LEN(CBB_PK) - 6)) = 1";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        object result = command.ExecuteScalar();
                        int maxNumericValue = Convert.ToInt32(result);

                        // Increment the numeric part
                        int numericPart = maxNumericValue + 1;

                        // Format the new value
                        newCbbPkValue = $"CBB_PK{numericPart:D3}"; // Ensures format like CBB_PK001, CBB_PK002
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                Console.WriteLine("Error generating CBB_PK: " + ex.Message);
                throw;
            }
            CBB_key = newCbbPkValue;
            return newCbbPkValue;
        }

        private string Find_DBCode()
        {
            string aa = null;
            dbcl.Sqlconnection();
            dbcl.ConnectDb();
            string kk = null;
            string cmdString1 = "select Id,CBB_PK from TRN_FINAL_CBB_Weights where Id=(select max(Id)from TRN_FINAL_CBB_Weights)";
            SqlCommand com1 = new SqlCommand(cmdString1, dbcl.Conn);
            SqlDataReader DR1 = com1.ExecuteReader();
            if (DR1.Read())
            {
                aa = DR1.GetValue(1).ToString();
                string bb = aa.Substring(5);
                int k = Convert.ToInt32(bb);
                k = k + 1;
                string q = Convert.ToString(k);
                kk = "FCBB0" + q;
            }
            else
            {
                kk = "FCBB01";
            }
            dbcl.DisconnectDb();
            CBB_key = kk;
            return kk;
        }


        private string GetCurrentShift()
        {
            TimeSpan currentTime = DateTime.Now.TimeOfDay;

            // Define shift times
            TimeSpan shiftAStart = new TimeSpan(6, 0, 0);  // 06:00 AM
            TimeSpan shiftAEnd = new TimeSpan(14, 0, 0);   // 02:00 PM
            TimeSpan shiftBStart = new TimeSpan(14, 0, 0); // 02:00 PM
            TimeSpan shiftBEnd = new TimeSpan(22, 0, 0);   // 10:00 PM
            TimeSpan shiftCStart = new TimeSpan(22, 0, 0); // 10:00 PM
            TimeSpan shiftCEnd = new TimeSpan(6, 0, 0);    // 06:00 AM (next day)

            // Determine the current shift based on time
            if (currentTime >= shiftAStart && currentTime < shiftAEnd)
            {
                return "A";  // Morning Shift
            }
            else if (currentTime >= shiftBStart && currentTime < shiftBEnd)
            {
                return "B";  // Afternoon Shift
            }
            else if (currentTime >= shiftCStart || currentTime < shiftCEnd)
            {
                return "C";  // Night Shift
            }

            return "Unknown";
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
            string textField = "brand_name";
            string valueField = "brand_id";

            SqlParameter[] parameters = new SqlParameter[]
            {
              new SqlParameter("@PlantId", selectedPlantValue),
              new SqlParameter("@LineId", selectedPlantLineValue),
              new SqlParameter("@CategoryId", selectedProductCategoryValue)
            };

            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_ProductBrand, textField, valueField, parameters, out recordsBound);

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
                    SetUpValidatorsForField(fieldName, criteria);
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

        private void SetUpValidatorsForField(string fieldName, ValidationCriteria criteria)
        {
            //switch (fieldName)
            //{
            //    //case "no_of_pcs":

            //    //    RFV_TB_NoOfPcs.ErrorMessage = criteria.RequiredFieldErrorMessage;
            //    //    RFV_TB_NoOfPcs.Enabled = criteria.IsRequired;

            //    //    TB_NoOfPcs.Attributes["placeholder"] = criteria.RangeErrorMessage;

            //    //    REV_TB_NoOfPcs.ErrorMessage = criteria.RegularExpressionErrorMessage;
            //    //    REV_TB_NoOfPcs.ValidationExpression = criteria.RegularExpression;
            //    //    REV_TB_NoOfPcs.Enabled = criteria.IsRegularExpressionRequired;

            //    //    RV_TB_NoOfPcs.ErrorMessage = criteria.RangeErrorMessage;
            //    //    RV_TB_NoOfPcs.MinimumValue = criteria.MinimumValue;
            //    //    RV_TB_NoOfPcs.MaximumValue = criteria.MaximumValue;
            //    //    RV_TB_NoOfPcs.Enabled = criteria.IsRangeRequired;

            //    //    //hdnMinNoOfPcs.Value = criteria.MinimumValue.ToString();
            //    //    //hdnMaxNoOfPcs.Value = criteria.MaximumValue.ToString();

            //    //    break;

            //    //case "GaugeVal":

            //    //    //RFV_TB_GaugeVal.ErrorMessage = criteria.RequiredFieldErrorMessage;
            //    //    //RFV_TB_GaugeVal.Enabled = criteria.IsRequired;

            //    //    //TB_GaugeVal.Attributes["placeholder"] = criteria.RangeErrorMessage;

            //    //    //REV_TB_GaugeVal.ErrorMessage = criteria.RegularExpressionErrorMessage;
            //    //    //REV_TB_GaugeVal.ValidationExpression = criteria.RegularExpression;
            //    //    //REV_TB_GaugeVal.Enabled = criteria.IsRegularExpressionRequired;

            //    //    //RV_TB_GaugeVal.ErrorMessage = criteria.RangeErrorMessage;
            //    //    //RV_TB_GaugeVal.MinimumValue = criteria.MinimumValue;
            //    //    //RV_TB_GaugeVal.MaximumValue = criteria.MaximumValue;
            //    //    //RV_TB_GaugeVal.Enabled = criteria.IsRangeRequired;
            //    //    break;

            //    //case "DryWeight":

            //    //    //RFV_TB_DryWeight.ErrorMessage = criteria.RequiredFieldErrorMessage;
            //    //    //RFV_TB_DryWeight.Enabled = criteria.IsRequired;

            //    //    //TB_DryWeight.Attributes["placeholder"] = criteria.RangeErrorMessage;

            //    //    //REV_TB_DryWeight.ErrorMessage = criteria.RegularExpressionErrorMessage;
            //    //    //REV_TB_DryWeight.ValidationExpression = criteria.RegularExpression;
            //    //    //REV_TB_DryWeight.Enabled = criteria.IsRegularExpressionRequired;

            //    //    ////RV_TB_DryWeight.ErrorMessage = criteria.RangeErrorMessage;
            //    //    ////RV_TB_DryWeight.MinimumValue = criteria.MinimumValue;
            //    //    ////RV_TB_DryWeight.MaximumValue = criteria.MaximumValue;
            //    //    ////RV_TB_DryWeight.Enabled = criteria.IsRangeRequired;
            //    //    break;

            //    //case "DippedWeight":

            //    //    //RFV_TB_DippedWeight.ErrorMessage = criteria.RequiredFieldErrorMessage;
            //    //    //RFV_TB_DippedWeight.Enabled = criteria.IsRequired;

            //    //    //TB_DippedWeight.Attributes["placeholder"] = criteria.RangeErrorMessage;

            //    //    //REV_TB_DippedWeight.ErrorMessage = criteria.RegularExpressionErrorMessage;
            //    //    //REV_TB_DippedWeight.ValidationExpression = criteria.RegularExpression;
            //    //    //REV_TB_DippedWeight.Enabled = criteria.IsRegularExpressionRequired;

            //    //    //RV_TB_DippedWeight.ErrorMessage = criteria.RangeErrorMessage;
            //    //    //RV_TB_DippedWeight.MinimumValue = criteria.MinimumValue;
            //    //    //RV_TB_DippedWeight.MaximumValue = criteria.MaximumValue;
            //    //    //RV_TB_DippedWeight.Enabled = criteria.IsRangeRequired;
            //    //    break;

            //    //case "VartyPkt":

            //    //    RFV_TB_VartyPkt.ErrorMessage = criteria.RequiredFieldErrorMessage;
            //    //    RFV_TB_VartyPkt.Enabled = criteria.IsRequired;

            //    //    TB_VartyPkt.Attributes["placeholder"] = criteria.RangeErrorMessage;

            //    //    REV_TB_VartyPkt.ErrorMessage = criteria.RegularExpressionErrorMessage;
            //    //    REV_TB_VartyPkt.ValidationExpression = criteria.RegularExpression;
            //    //    REV_TB_VartyPkt.Enabled = criteria.IsRegularExpressionRequired;

            //    //    //RV_TB_VartyPkt.ErrorMessage = criteria.RangeErrorMessage;
            //    //    //RV_TB_VartyPkt.MinimumValue = criteria.MinimumValue;
            //    //    //RV_TB_VartyPkt.MaximumValue = criteria.MaximumValue;
            //    //    //RV_TB_VartyPkt.Enabled = criteria.IsRangeRequired;
            //    //    break;

            //    //case "BakingTime":

            //    //    RFV_TB_BakingTime.ErrorMessage = criteria.RequiredFieldErrorMessage;
            //    //    RFV_TB_BakingTime.Enabled = criteria.IsRequired;

            //    //    TB_BakingTime.Attributes["placeholder"] = criteria.RangeErrorMessage;

            //    //    REV_TB_BakingTime.ErrorMessage = criteria.RegularExpressionErrorMessage;
            //    //    REV_TB_BakingTime.ValidationExpression = criteria.RegularExpression;
            //    //    REV_TB_BakingTime.Enabled = criteria.IsRegularExpressionRequired;

            //    //    RV_TB_BakingTime.ErrorMessage = criteria.RangeErrorMessage;
            //    //    RV_TB_BakingTime.MinimumValue = criteria.MinimumValue;
            //    //    RV_TB_BakingTime.MaximumValue = criteria.MaximumValue;
            //    //    RV_TB_BakingTime.Enabled = criteria.IsRangeRequired;

            //    //    hdnMinBakingTime.Value = criteria.MinimumValue.ToString();
            //    //    hdnMaxBakingTime.Value = criteria.MaximumValue.ToString();

            //    //    break;

            //    //case "FlavTst":

            //    //    RFV_RBL_FlavTst.ErrorMessage = criteria.RequiredFieldErrorMessage;
            //    //    RFV_RBL_FlavTst.Enabled = criteria.IsRequired;

            //    //    //RBL_FlavTst.Attributes["placeholder"] = criteria.RangeErrorMessage;

            //    //    //REV_RBL_FlavTst.ErrorMessage = criteria.RegularExpressionErrorMessage;
            //    //    //REV_RBL_FlavTst.ValidationExpression = criteria.RegularExpression;
            //    //    //REV_RBL_FlavTst.Enabled = criteria.IsRegularExpressionRequired;

            //    //    //RV_TB_DippedWeight.ErrorMessage = criteria.RangeErrorMessage;
            //    //    //RV_TB_DippedWeight.MinimumValue = criteria.MinimumValue;
            //    //    //RV_TB_DippedWeight.MaximumValue = criteria.MaximumValue;
            //    //    //RV_TB_DippedWeight.Enabled = criteria.IsRangeRequired;
            //    //    break;

            //    //case "SpSz":

            //    //    RFV_TB_ShapeSize.ErrorMessage = criteria.RequiredFieldErrorMessage;
            //    //    RFV_TB_ShapeSize.Enabled = criteria.IsRequired;

            //    //    TB_ShapeSize.Attributes["placeholder"] = criteria.RangeErrorMessage;

            //    //    REV_TB_ShapeSize.ErrorMessage = criteria.RegularExpressionErrorMessage;
            //    //    REV_TB_ShapeSize.ValidationExpression = criteria.RegularExpression;
            //    //    REV_TB_ShapeSize.Enabled = criteria.IsRegularExpressionRequired;

            //    //    CV_TB_ShapeSize.ErrorMessage = criteria.RangeErrorMessage;
            //    //    //RV_TB_ShapeSize.ErrorMessage = criteria.RangeErrorMessage;
            //    //    //RV_TB_ShapeSize.MinimumValue = criteria.MinimumValue;
            //    //    //RV_TB_ShapeSize.MaximumValue = criteria.MaximumValue;
            //    //    //RV_TB_ShapeSize.Enabled = criteria.IsRangeRequired;

            //    //    hdnMinShapeSize.Value = criteria.MinimumValue.ToString();
            //    //    hdnMaxShapeSize.Value = criteria.MaximumValue.ToString();
            //    //    break;

            //    //case "TextureBite":

            //    //    //RFV_TB_TextureBite.ErrorMessage = criteria.RequiredFieldErrorMessage;
            //    //    //RFV_TB_TextureBite.Enabled = criteria.IsRequired;

            //    //    //TB_TextureBite.Attributes["placeholder"] = criteria.RangeErrorMessage;

            //    //    //REV_TB_TextureBite.ErrorMessage = criteria.RegularExpressionErrorMessage;
            //    //    //REV_TB_TextureBite.ValidationExpression = criteria.RegularExpression;
            //    //    //REV_TB_TextureBite.Enabled = criteria.IsRegularExpressionRequired;

            //    //    //RV_TB_TextureBite.ErrorMessage = criteria.RangeErrorMessage;
            //    //    //RV_TB_TextureBite.MinimumValue = criteria.MinimumValue;
            //    //    //RV_TB_TextureBite.MaximumValue = criteria.MaximumValue;
            //    //    //RV_TB_TextureBite.Enabled = criteria.IsRangeRequired;
            //    //    break;

            //    //case "Moisture":

            //    //    RFV_TB_Moisture.ErrorMessage = criteria.RequiredFieldErrorMessage;
            //    //    RFV_TB_Moisture.Enabled = criteria.IsRequired;

            //    //    TB_Moisture.Attributes["placeholder"] = criteria.RangeErrorMessage;

            //    //    REV_TB_Moisture.ErrorMessage = criteria.RegularExpressionErrorMessage;
            //    //    REV_TB_Moisture.ValidationExpression = criteria.RegularExpression;
            //    //    REV_TB_Moisture.Enabled = criteria.IsRegularExpressionRequired;

            //    //    RV_TB_Moisture.ErrorMessage = criteria.RangeErrorMessage;
            //    //    RV_TB_Moisture.MinimumValue = criteria.MinimumValue;
            //    //    RV_TB_Moisture.MaximumValue = criteria.MaximumValue;
            //    //    RV_TB_Moisture.Enabled = criteria.IsRangeRequired;
            //    //    break;

            //    //case "GaugeLen":

            //    //    RFV_TB_GaugeLen.ErrorMessage = criteria.RequiredFieldErrorMessage;
            //    //    RFV_TB_GaugeLen.Enabled = criteria.IsRequired;

            //    //    TB_GaugeLen.Attributes["placeholder"] = criteria.RangeErrorMessage;

            //    //    REV_TB_GaugeLen.ErrorMessage = criteria.RegularExpressionErrorMessage;
            //    //    REV_TB_GaugeLen.ValidationExpression = criteria.RegularExpression;
            //    //    REV_TB_GaugeLen.Enabled = criteria.IsRegularExpressionRequired;

            //    //    CV_TB_GaugeLen.ErrorMessage = criteria.RangeErrorMessage;
            //    //    //CV_TB_GaugeLen.MinimumValue = criteria.MinimumValue;
            //    //    //RV_TB_GaugeLen.MaximumValue = criteria.MaximumValue;
            //    //    CV_TB_GaugeLen.Enabled = criteria.IsRangeRequired;

            //    //    hdnMinGaugelen.Value = criteria.MinimumValue.ToString();
            //    //    hdnMaxGaugelen.Value = criteria.MaximumValue.ToString();

            //    //    break;

            //    //case "wgtwtoil":
            //    //    //weight without oil or dry weight
            //    //    RFV_TB_wgtwtoil.ErrorMessage = criteria.RequiredFieldErrorMessage;
            //    //    RFV_TB_wgtwtoil.Enabled = criteria.IsRequired;

            //    //    TB_wgtwtoil.Attributes["placeholder"] = criteria.RangeErrorMessage;

            //    //    REV_TB_wgtwtoil.ErrorMessage = criteria.RegularExpressionErrorMessage;
            //    //    REV_TB_wgtwtoil.ValidationExpression = criteria.RegularExpression;
            //    //    REV_TB_wgtwtoil.Enabled = criteria.IsRegularExpressionRequired;

            //    //    RV_TB_wgtwtoil.ErrorMessage = criteria.RangeErrorMessage;
            //    //    RV_TB_wgtwtoil.MinimumValue = criteria.MinimumValue;
            //    //    RV_TB_wgtwtoil.MaximumValue = criteria.MaximumValue;
            //    //    RV_TB_wgtwtoil.Enabled = criteria.IsRangeRequired;
            //    //    break;

            //    //case "wgtwoil":
            //    //    //weight with oil or dipped weight
            //    //    RFV_TB_wgtwoil.ErrorMessage = criteria.RequiredFieldErrorMessage;
            //    //    RFV_TB_wgtwoil.Enabled = criteria.IsRequired;

            //    //    TB_wgtwoil.Attributes["placeholder"] = criteria.RangeErrorMessage;

            //    //    REV_TB_wgtwoil.ErrorMessage = criteria.RegularExpressionErrorMessage;
            //    //    REV_TB_wgtwoil.ValidationExpression = criteria.RegularExpression;
            //    //    REV_TB_wgtwoil.Enabled = criteria.IsRegularExpressionRequired;

            //    //    RV_TB_wgtwoil.ErrorMessage = criteria.RangeErrorMessage;
            //    //    RV_TB_wgtwoil.MinimumValue = criteria.MinimumValue;
            //    //    RV_TB_wgtwoil.MaximumValue = criteria.MaximumValue;
            //    //    RV_TB_wgtwoil.Enabled = criteria.IsRangeRequired;
            //    //    break;

            //    //case "oilpercent":

            //    //    RFV_TB_oilpercent.ErrorMessage = criteria.RequiredFieldErrorMessage;
            //    //    RFV_TB_oilpercent.Enabled = criteria.IsRequired;

            //    //    TB_oilpercent.Attributes["placeholder"] = criteria.RangeErrorMessage;

            //    //    REV_TB_oilpercent.ErrorMessage = criteria.RegularExpressionErrorMessage;
            //    //    REV_TB_oilpercent.ValidationExpression = criteria.RegularExpression;
            //    //    REV_TB_oilpercent.Enabled = criteria.IsRegularExpressionRequired;

            //    //    RV_TB_oilpercent.ErrorMessage = criteria.RangeErrorMessage;
            //    //    RV_TB_oilpercent.MinimumValue = criteria.MinimumValue;
            //    //    RV_TB_oilpercent.MaximumValue = criteria.MaximumValue;
            //    //    RV_TB_oilpercent.Enabled = criteria.IsRangeRequired;
            //    //    break;

            //    //case "PktWgt":

            //    //    RFV_TB_PktWgt.ErrorMessage = criteria.RequiredFieldErrorMessage;
            //    //    RFV_TB_PktWgt.Enabled = criteria.IsRequired;

            //    //    TB_PktWgt.Attributes["placeholder"] = criteria.RangeErrorMessage;

            //    //    REV_TB_PktWgt.ErrorMessage = criteria.RegularExpressionErrorMessage;
            //    //    REV_TB_PktWgt.ValidationExpression = criteria.RegularExpression;
            //    //    REV_TB_PktWgt.Enabled = criteria.IsRegularExpressionRequired;

            //    //    RV_TB_PktWgt.ErrorMessage = criteria.RangeErrorMessage;
            //    //    RV_TB_PktWgt.MinimumValue = criteria.MinimumValue;
            //    //    RV_TB_PktWgt.MaximumValue = criteria.MaximumValue;
            //    //    RV_TB_PktWgt.Enabled = criteria.IsRangeRequired;
            //    //    break;

            //    //    default:
            //    //    // Handle unrecognized field names
            //    //    break;
            //}
        }

        protected void Btn_Reset_Click(object sender, EventArgs e)
        {
            DDL_Plant.SelectedIndex = 0;
            DDL_PlantLine.SelectedIndex = 0;
            DDL_ProductCategory.SelectedIndex = 0;
            DDL_ProductBrand.SelectedIndex = 0;
            DDL_BrandSKU.SelectedIndex = 0;

            TXT_BatchNo.Text = string.Empty;
            TXT_MRP.Text = string.Empty;

            lblMessage.Text = string.Empty;
        }

        protected void Btn_Save_Click(object sender, EventArgs e)
        {
            string plantName = DDL_Plant.SelectedValue;
            string plantLine = DDL_PlantLine.SelectedValue;
            string productCategory = DDL_ProductCategory.SelectedValue;
            string productBrand = DDL_ProductBrand.SelectedValue;
            string brandSKU = DDL_BrandSKU.SelectedValue;

            string brandSKUText = DDL_BrandSKU.SelectedItem.Text.ToString();

            // Convert the selected SKU text to a decimal
            decimal skuwt;
            if (decimal.TryParse(brandSKUText, out skuwt))
            {
                // Calculate min and max weights
                decimal minWeight = skuwt - 2;
                decimal maxWeight = skuwt + 2;

                // Set values to hidden fields
                hdn_minwt.Value = minWeight.ToString();
                hdn_maxwt.Value = maxWeight.ToString();
            }
            else
            {
                // Handle conversion failure if needed
                // For example, you could set default values or log an error
                hdn_minwt.Value = "0";
                hdn_maxwt.Value = "0";
            }


            string cbbPK = Find_DBCode();
            int formID = Convert.ToInt32(hdn_formid.Value.ToString());
            int submittedById = Convert.ToInt32(Session["USERID"].ToString());
            DateTime submittedDate = DateTime.Now.Date;
            TimeSpan submittedTime = DateTime.Now.TimeOfDay;
            string shift = hdn_shiftvalue.Value.ToString();
            string submittedByEmployeeCode = Session["WORKMAN"].ToString();
            string batchNo = TXT_BatchNo.Text;
            //decimal mrp;
            //if (!decimal.TryParse(TXT_MRP.Text, out mrp))
            //{
            //    lblMessage.Text = "Please enter a valid MRP.";
            //    lblMessage.ForeColor = System.Drawing.Color.Red;
            //    return;
            //}

            decimal mrp;
            string mrpInput = TXT_MRP.Text.Trim();

            if (string.IsNullOrWhiteSpace(mrpInput) || !decimal.TryParse(mrpInput, out mrp) || mrp == 0)
            {
                mrp = 0;  // Treat as zero
                lblMessage.Text = "MRP is set to 0.";
                lblMessage.ForeColor = System.Drawing.Color.Blue;
            }
            else
            {
                lblMessage.Text = "Valid MRP entered.";
                lblMessage.ForeColor = System.Drawing.Color.Green;
            }

            Int32 pkts = Convert.ToInt32(TB_NoPacks.Text.ToString());
            int viewMode = 1;
            int deleteMode = 0;
            string approver1EmployeeCode = Approver1CodeLabel.Text.ToString();
            string approver2EmployeeCode = Approver2CodeLabel.Text.ToString();
            string dottedLineApproverEmployeeCode = DottedLineApproverCodeLabel.Text.ToString();

            try
            {
                InsertIntoTRN_FINAL_CBB_Weights(
                    cbbPK, formID, submittedById, submittedDate, submittedTime, shift,
                    submittedByEmployeeCode, plantName, plantLine, productCategory, productBrand,
                    brandSKU, batchNo, mrp, pkts, viewMode,
                    deleteMode, approver1EmployeeCode,
                    approver2EmployeeCode,
                    dottedLineApproverEmployeeCode
                );

                DDL_Plant.Enabled = false;
                DDL_PlantLine.Enabled = false;
                DDL_ProductCategory.Enabled = false;
                DDL_ProductBrand.Enabled = false;
                DDL_BrandSKU.Enabled = false;

                TXT_BatchNo.ReadOnly = true;
                TXT_MRP.ReadOnly = true;

                Btn_Save.Enabled = false;
                Btn_Save.Text = "SAVED";

                lblMessage.Text = "Data inserted successfully!";

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
            catch (Exception ex)
            {
                lblMessage.Text = "An error occurred: " + ex.Message;
                string errorMessage = ex.Message.Replace("'", "\\'"); // Escape single quotes in the error message
                string errorScript1 = "<script type='text/javascript'>\n" +
                                     $"new PNotify({{\n" +
                                     "    title: 'Error',\n" +
                                     $"    text: '{errorMessage}',\n" +
                                     "    type: 'error',\n" +
                                     "    styling: 'bootstrap3'\n" +
                                     "});\n" +
                                     "</script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowErrorNotification1", errorScript1, false);
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
                    cmd.Parameters.AddWithValue("@FormID", 2);
                    cmd.Parameters.AddWithValue("@FormName", "qa_qc_FinalCbbWtReport");

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        hdn_formid.Value = "2";
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
                            dbcl.InsertDefaultApprovers(selectedPlantValue, selectedPlantLineValue, 2);

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
                    cmd.Parameters.AddWithValue("@FormID", 2);
                    cmd.Parameters.AddWithValue("@FormName", "qa_qc_FinalCbbWtReport");

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        hdn_formid.Value = "2";
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
                            bool isInserted = dbcl.InsertDefaultApprovers(selectedPlantValue, selectedPlantLineValue, 2);

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

        public void InsertIntoTRN_FINAL_CBB_Weights(
        string cbbPK, int formID, int submittedById, DateTime submittedDate, TimeSpan submittedTime,
        string shift, string submittedByEmployeeCode, string plantName, string line,
        string productCategory, string productBrand, string skuId, string batchNo,
        decimal mrp, Int32 pkts, int viewMode,
        int deleteMode, string approver1EmployeeCode,
        string approver2EmployeeCode,
        string dottedLineApproverEmployeeCode)
        {
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            {
                // Define the insert command
                string insertCommand = @"
                INSERT INTO [dbo].[TRN_FINAL_CBB_Weights] (
                    [CBB_PK], [FormID], [SubmittedById], [SubmittedDate], [SubmittedTime], 
                    [Shift], [SubmittedByEmployeeCode], [PlantName], [Line], [ProductCategory], 
                    [ProductBrand], [SKUId], [BatchNo], [MRP], [NumberPkts],
                    [ViewMode], [DeleteMode], [Approver1EmployeeCode],
                    [Approver2EmployeeCode],
                    [DottedLineApproverEmployeeCode]
                ) VALUES (
                    @CBB_PK, @FormID, @SubmittedById, @SubmittedDate, @SubmittedTime, 
                    @Shift, @SubmittedByEmployeeCode, @PlantName, @Line, @ProductCategory, 
                    @ProductBrand, @SKUId, @BatchNo, @MRP, @NumberPkts,
                    @ViewMode, @DeleteMode, @Approver1EmployeeCode,
                    @Approver2EmployeeCode,
                    @DottedLineApproverEmployeeCode
                )";

                using (SqlCommand command = new SqlCommand(insertCommand, connection))
                {
                    // Add parameters to the command
                    command.Parameters.AddWithValue("@CBB_PK", cbbPK);
                    command.Parameters.AddWithValue("@FormID", formID);
                    command.Parameters.AddWithValue("@SubmittedById", submittedById);
                    command.Parameters.AddWithValue("@SubmittedDate", submittedDate);
                    command.Parameters.AddWithValue("@SubmittedTime", submittedTime);
                    command.Parameters.AddWithValue("@Shift", shift);
                    command.Parameters.AddWithValue("@SubmittedByEmployeeCode", submittedByEmployeeCode);
                    command.Parameters.AddWithValue("@PlantName", plantName);
                    command.Parameters.AddWithValue("@Line", line);
                    command.Parameters.AddWithValue("@ProductCategory", productCategory);
                    command.Parameters.AddWithValue("@ProductBrand", productBrand);
                    command.Parameters.AddWithValue("@SKUId", skuId);
                    command.Parameters.AddWithValue("@BatchNo", batchNo);
                    command.Parameters.AddWithValue("@MRP", mrp);
                    command.Parameters.AddWithValue("@NumberPkts", mrp);
                    //command.Parameters.AddWithValue("@GrossWeightJson", grossWeightJson);
                    //command.Parameters.AddWithValue("@AverageGrossWeight", averageGrossWeight.HasValue ? (object)averageGrossWeight.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@ViewMode", viewMode);
                    command.Parameters.AddWithValue("@DeleteMode", deleteMode);
                    command.Parameters.AddWithValue("@Approver1EmployeeCode", (object)approver1EmployeeCode ?? DBNull.Value);
                    //command.Parameters.AddWithValue("@Approver1_Status", (object)approver1Status ?? DBNull.Value);
                    //command.Parameters.AddWithValue("@Approver1_TimeStamp", (object)approver1TimeStamp ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Approver2EmployeeCode", (object)approver2EmployeeCode ?? DBNull.Value);
                    //command.Parameters.AddWithValue("@Approver2_Status", (object)approver2Status ?? DBNull.Value);
                    //command.Parameters.AddWithValue("@Approver2_TimeStamp", (object)approver2TimeStamp ?? DBNull.Value);
                    command.Parameters.AddWithValue("@DottedLineApproverEmployeeCode", (object)dottedLineApproverEmployeeCode ?? DBNull.Value);
                    //command.Parameters.AddWithValue("@DottedApprover_Status", (object)dottedApproverStatus ?? DBNull.Value);
                    //command.Parameters.AddWithValue("@DottedApprover_TimeStamp", (object)dottedApproverTimeStamp ?? DBNull.Value);

                    // Open the connection, execute the command and close the connection
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}