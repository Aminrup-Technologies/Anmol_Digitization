using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Configuration;
using static AnmolDristi.qaqc_oven_viewpage;
using System.Text;
using System.IO;

namespace AnmolDristi
{
    public partial class qaqc_rotaryline_view : System.Web.UI.Page
    {
        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

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

                    //lbl_upper.Text = "Search Filters for Rotary Line Report";
                    lbl_lower.Text = "View and Select for Detailed View  ||   ";
                    DataLoader();
                    PlantBinder();
                    //loadAlldata();  // Load all data on initial load
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
            //BindGridView();

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
        private ValidationCriteria GetValidationCriteriaFromDatabase(string fieldName)
        {
            // Query the database to fetch validation criteria based on the field name
            // Implement database querying logic here, and return the fetched data
            // For example:
            // SELECT * FROM ValidationCriteria WHERE FieldName = fieldName

            // Simulated data for demonstration
            ValidationCriteria criteria = new ValidationCriteria();
            criteria.RequiredFieldErrorMessage = "*";
            criteria.RegularExpressionErrorMessage = "[30-40]";
            criteria.RegularExpression = @"\d+";
            criteria.RangeErrorMessage = "[30-40]";
            criteria.MinimumValue = "30";
            criteria.MaximumValue = "40";
            criteria.IsRequired = true;
            criteria.IsRegularExpressionRequired = true;
            criteria.IsRangeRequired = false;

            return criteria;
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
            DataLoader();

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
            DataLoader();

        }
        protected void DDL_PlantLine_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_PlantLine.SelectedIndex != 0)
            {
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                string selectedPlantLineValue = DDL_PlantLine.SelectedValue.ToString();
                LineProductsBinder(selectedPlantValue, selectedPlantLineValue);
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
            DataLoader();


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
            DataLoader();

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
            DataLoader();

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
            DataLoader();

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
            DataLoader();

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
            DataLoader();
        }



        //private void loadAlldata()
        //{
        //    SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString);
        //    SqlCommand cmd = new SqlCommand("select * from BasicRl_Data_Table", con);
        //    SqlDataAdapter sda = new SqlDataAdapter(cmd);
        //    DataTable dt = new DataTable();
        //    sda.Fill(dt);
        //    GridView1.DataSource = dt;
        //    GridView1.DataBind();
        //}

        public class ReportInfo
        {
            public int Id { get; set; }
            public string PlantName { get; set; }  // Updated to match database column
            public string Line { get; set; }
            public string ProductCategory { get; set; }
            public string ProductBrand { get; set; }
            public string SKUID { get; set; }
            public string RLWt { get; set; }
            public DateTime? SubmittedDate { get; set; }  // Renamed to match the database column
            public TimeSpan? SubmittedTime { get; set; }  // Renamed to match the database column
            public string Shift { get; set; }
            public string Variety { get; set; }
            public string SubmittedByEmployeeCode { get; set; }  // Added this field based on database
            public int? Approver1_Status { get; set; }  // Changed type to int to match database
            public DateTime? Approver1_TimeStamp { get; set; }  // Changed to DateTime? to match database
            public string Approver1EmployeeCode { get; set; }  // Added this field based on database
            public int? Approver2_Status { get; set; }  // Changed type to int to match database
            public DateTime? Approver2_TimeStamp { get; set; }  // Changed to DateTime? to match database
            public string Approver2EmployeeCode { get; set; }  // Added this field based on database
            public int? DottedApprover_Status { get; set; }  // Changed type to int to match database
            public DateTime? DottedApprover_TimeStamp { get; set; }  // Changed to DateTime? to match database
            public string DottedLineApproverEmployeeCode { get; set; }  // Added this field based on database
            public string LineWt { get; set; }
            public string GaugeAndWeight { get; set; }
            public decimal? AvgLineWt { get; set; }
            public decimal? AvgGaugeValue { get; set; }
            public decimal? AvgWeightValue { get; set; }
        }
        private DataTable GetFilteredData()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            DataTable dt = new DataTable();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string sqlQuery = "SELECT * FROM TRN_RotaryLine_OvenEnd WHERE ViewMode=1";

                // Add filters to SQL query based on the selected values
                if (!string.IsNullOrEmpty(DDL_Plant.SelectedValue) && DDL_Plant.SelectedValue != "0")
                {
                    sqlQuery += " AND PlantName = @PlantName";
                }

                if (!string.IsNullOrEmpty(DDL_PlantLine.SelectedValue) && DDL_PlantLine.SelectedValue != "0")
                {
                    sqlQuery += " AND Line = @Line";
                }

                if (!string.IsNullOrEmpty(DDL_ProductCategory.SelectedValue) && DDL_ProductCategory.SelectedValue != "0")
                {
                    sqlQuery += " AND ProductCategory = @ProductCategory";
                }

                if (!string.IsNullOrEmpty(DDL_ProductBrand.SelectedValue) && DDL_ProductBrand.SelectedValue != "0")
                {
                    sqlQuery += " AND ProductBrand = @ProductBrand";
                }

                if (!string.IsNullOrEmpty(DDL_BrandSKU.SelectedValue) && DDL_BrandSKU.SelectedValue != "0")
                {
                    sqlQuery += " AND SKUID = @SKUID";
                }


                sqlQuery += " order by Id desc";

                using (SqlCommand cmd = new SqlCommand(sqlQuery, con))
                {
                    // Add parameters only if they are being used in the query
                    if (!string.IsNullOrEmpty(DDL_Plant.SelectedValue) && DDL_Plant.SelectedValue != "0")
                    {
                        cmd.Parameters.AddWithValue("@PlantName", DDL_Plant.SelectedValue);
                    }

                    if (!string.IsNullOrEmpty(DDL_PlantLine.SelectedValue) && DDL_PlantLine.SelectedValue != "0")
                    {
                        cmd.Parameters.AddWithValue("@Line", DDL_PlantLine.SelectedValue);
                    }

                    if (!string.IsNullOrEmpty(DDL_ProductCategory.SelectedValue) && DDL_ProductCategory.SelectedValue != "0")
                    {
                        cmd.Parameters.AddWithValue("@ProductCategory", DDL_ProductCategory.SelectedValue);
                    }

                    if (!string.IsNullOrEmpty(DDL_ProductBrand.SelectedValue) && DDL_ProductBrand.SelectedValue != "0")
                    {
                        cmd.Parameters.AddWithValue("@ProductBrand", DDL_ProductBrand.SelectedValue);
                    }

                    if (!string.IsNullOrEmpty(DDL_BrandSKU.SelectedValue) && DDL_BrandSKU.SelectedValue != "0")
                    {
                        cmd.Parameters.AddWithValue("@SKUID", DDL_BrandSKU.SelectedValue);
                    }

                    cmd.CommandType = CommandType.Text;

                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        sda.Fill(dt);
                    }
                }
            }

            return dt;
        }

        private void BindGridView()
        {
            // Fetch filtered data from the database
            DataTable dt = GetFilteredData();

            // Convert DataTable to List<ReportInfo>
            List<ReportInfo> dataSave = new List<ReportInfo>();

            foreach (DataRow row in dt.Rows)
            {
                ReportInfo info = new ReportInfo
                {
                    Id = row["Id"] == DBNull.Value ? 0 : Convert.ToInt32(row["Id"]),
                    //FormID = row["FormID"] == DBNull.Value ? 0 : Convert.ToInt32(row["FormID"]),
                    //SubmittedById = row["SubmittedById"] == DBNull.Value ? 0 : Convert.ToInt32(row["SubmittedById"]),
                    SubmittedDate = row["SubmittedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["SubmittedDate"]),
                    SubmittedTime = row["SubmittedTime"] == DBNull.Value ? (TimeSpan?)null : TimeSpan.Parse(row["SubmittedTime"].ToString()),
                    Shift = row["Shift"] == DBNull.Value ? string.Empty : row["Shift"].ToString(),
                    SubmittedByEmployeeCode = row["SubmittedByEmployeeCode"] == DBNull.Value ? string.Empty : row["SubmittedByEmployeeCode"].ToString(),
                    PlantName = row["PlantName"] == DBNull.Value ? string.Empty : row["PlantName"].ToString(),
                    Line = row["Line"] == DBNull.Value ? string.Empty : row["Line"].ToString(),
                    ProductCategory = row["ProductCategory"] == DBNull.Value ? string.Empty : row["ProductCategory"].ToString(),
                    ProductBrand = row["ProductBrand"] == DBNull.Value ? string.Empty : row["ProductBrand"].ToString(),
                    SKUID = row["SKUId"] == DBNull.Value ? string.Empty : row["SKUId"].ToString(),
                    RLWt = row["RLWt"] == DBNull.Value ? string.Empty : row["RLWt"].ToString(),
                    Variety = row["Variety"] == DBNull.Value ? string.Empty : row["Variety"].ToString(),
                    //ViewMode = row["ViewMode"] == DBNull.Value ? 0 : Convert.ToInt32(row["ViewMode"]),
                    //DeleteMode = row["DeleteMode"] == DBNull.Value ? 0 : Convert.ToInt32(row["DeleteMode"]),
                    Approver1EmployeeCode = row["Approver1EmployeeCode"] == DBNull.Value ? string.Empty : row["Approver1EmployeeCode"].ToString(),
                    Approver1_Status = row["Approver1_Status"] == DBNull.Value ? 0 : Convert.ToInt32(row["Approver1_Status"]),
                    Approver1_TimeStamp = row["Approver1_TimeStamp"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["Approver1_TimeStamp"]),
                    Approver2EmployeeCode = row["Approver2EmployeeCode"] == DBNull.Value ? string.Empty : row["Approver2EmployeeCode"].ToString(),
                    Approver2_Status = row["Approver2_Status"] == DBNull.Value ? 0 : Convert.ToInt32(row["Approver2_Status"]),
                    Approver2_TimeStamp = row["Approver2_TimeStamp"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["Approver2_TimeStamp"]),
                    DottedLineApproverEmployeeCode = row["DottedLineApproverEmployeeCode"] == DBNull.Value ? string.Empty : row["DottedLineApproverEmployeeCode"].ToString(),
                    DottedApprover_Status = row["DottedApprover_Status"] == DBNull.Value ? 0 : Convert.ToInt32(row["DottedApprover_Status"]),
                    DottedApprover_TimeStamp = row["DottedApprover_TimeStamp"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["DottedApprover_TimeStamp"]),
                    LineWt = row["linewt"] == DBNull.Value ? string.Empty : row["linewt"].ToString(),
                    AvgLineWt = row["avglinewt"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(row["avglinewt"]),
                    //GaugeValues = row["gaugevalues"] == DBNull.Value ? string.Empty : row["gaugevalues"].ToString(),
                    AvgGaugeValue = row["avggaugevalue"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(row["avggaugevalue"]),
                    //WeightValue = row["weightvalue"] == DBNull.Value ? string.Empty : row["weightvalue"].ToString(),
                    AvgWeightValue = row["avgweightvalue"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(row["avgweightvalue"]),
                    GaugeAndWeight = row["gaugeandweight"] == DBNull.Value ? string.Empty : row["gaugeandweight"].ToString()
                };

                dataSave.Add(info);
            }

            // Bind to GridView
            GridView1.DataSource = dataSave;
            GridView1.DataBind();
        }


        protected void btn_view_cancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("home.aspx");
        }

        protected void btn_view_reset_Click(object sender, EventArgs e)
        {
            Response.Redirect("qaqc_rotaryline_view.aspx");
        }

       
        protected void btn_view_submit_Click(object sender, EventArgs e)
        {
            //// Retrieve date range from textboxes
            //DateTime? dateFrom = GetDateFromTextBox(TxtDateFrom.Text);
            //DateTime? dateTo = GetDateFromTextBox(TxtDateTo.Text);

            //// SQL query to fetch data based on date range
            //StringBuilder queryBuilder = new StringBuilder("SELECT * FROM dbo.BasicRl_Data_Table WHERE 1 = 1");
            //var parameters = new List<SqlParameter>();

            //if (dateFrom.HasValue)
            //{
            //    queryBuilder.Append(" AND [Date] >= @DateFrom");
            //    parameters.Add(new SqlParameter("@DateFrom", SqlDbType.Date) { Value = dateFrom.Value.Date });
            //}

            //if (dateTo.HasValue)
            //{
            //    queryBuilder.Append(" AND [Date] <= @DateTo");
            //    parameters.Add(new SqlParameter("@DateTo", SqlDbType.Date) { Value = dateTo.Value.Date });
            //}

            //try
            //{
            //    // Fetch filtered data using the constructed query and parameters
            //    DataTable filteredData = GetDataFromTable(queryBuilder.ToString(), parameters.ToArray());

            //    // Bind filtered data to GridView
            //    GridView1.DataSource = filteredData;
            //    GridView1.DataBind();

            //    // Show a notification if no records are found
            //    if (filteredData.Rows.Count == 0)
            //    {
            //        ShowNotification("No Data", "No records found for the selected date range.", "info");
            //    }
            //}
            //catch (Exception )
            //{
            //    // Handle exceptions (log it, show an error message, etc.)
            //    ShowNotification("Error", "An error occurred while processing your request.", "error");
            //}
            DataLoader();
        }

        // Helper method to convert textbox text to DateTime
        private DateTime? GetDateFromTextBox(string text)
        {
            return string.IsNullOrEmpty(text) ? (DateTime?)null : DateTime.ParseExact(text, "yyyy-MM-dd", null);
        }

        // Helper method to fetch data from the table
        //private DataTable GetDataFromTable(string query, SqlParameter[] parameters)
        //{
        //    // Implementation for retrieving data from the database based on query and parameters
        //    // Ensure to use appropriate connection string and data retrieval logic
        //    string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        //    DataTable dataTable = new DataTable();

        //    using (SqlConnection connection = new SqlConnection(connectionString))
        //    {
        //        using (SqlCommand command = new SqlCommand(query, connection))
        //        {
        //            command.Parameters.AddRange(parameters);
        //            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
        //            {
        //                adapter.Fill(dataTable);
        //            }
        //        }
        //    }

        //    return dataTable;
        //}

        // Helper method to show notifications
        private void ShowNotification(string title, string message, string type)
        {
            // Example: Use PNotify or any other notification mechanism
            string script = $@"
                        <script type='text/javascript'>
                            new PNotify({{
                                title: '{title}',
                                text: '{message}',
                                type: '{type}',
                                styling: 'bootstrap3'
                            }});
                        </script>";

            ClientScript.RegisterStartupScript(this.GetType(), "ShowNotification", script, false);
        }

       

        protected void btn_view_export_Click(object sender, EventArgs e)
        {
            try
            {
                // Fetch the data that needs to be exported
                DataTable exportData = GetDataForExport(); // Retrieve data from the database

                if (exportData != null && exportData.Rows.Count > 0)
                {
                    // Set the response settings
                    Response.Clear();
                    Response.Buffer = true;
                    Response.AddHeader("content-disposition", "attachment;filename=RotaryLine.xls");
                    Response.Charset = "";
                    Response.ContentType = "application/vnd.ms-excel";

                    using (StringWriter sw = new StringWriter())
                    {
                        HtmlTextWriter hw = new HtmlTextWriter(sw);

                        // Create an HTML table to export data
                        GridView exportGridView = new GridView();
                        exportGridView.DataSource = exportData;
                        exportGridView.DataBind();

                        // Render the GridView content
                        exportGridView.RenderControl(hw);

                        // Write the content to the response
                        Response.Output.Write(sw.ToString());
                        Response.Flush();
                        Response.End();
                    }
                }
                else
                {
                    // Optionally, show a notification if no data is available
                    // ShowNotification("No Data", "No data available to export.", "info");
                }
            }
            catch(Exception) /*(Exception ex)*/
            {
                // Handle exceptions and optionally show an error notification
                // ShowNotification("Error", $"An error occurred while exporting: {ex.Message}", "error");
            }
        }

        public override void VerifyRenderingInServerForm(Control control)
        {
            // This method is necessary to allow GridView control rendering in the export
        }

        private DataTable GetDataForExport()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            DataTable dt = new DataTable();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string sqlQuery = "SELECT * FROM dbo.BasicRl_Data_Table"; // Query for fetching data

                using (SqlCommand cmd = new SqlCommand(sqlQuery, con))
                {
                    cmd.CommandType = CommandType.Text;

                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        sda.Fill(dt);
                    }
                }
            }

            return dt;
        }

        private void loadAlldata()
        {
            try
            {
                string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

                string query = @"
                    SELECT TOP(30)
                        c.ID AS DBID,
                        c.FormID as FormID,
                        c.CcpId as RecordID,
                        p.plant_name AS PlantName,
                        c.SubmittedByEmployeeCode as EmpCode,
                        u.EmployeeName AS EmpName,
                        c.SubmittedDate as SDate,
                        c.SubmittedTime as STime,
                        c.Shift as SShift,
                        'No Comment' AS Remarks,
                        c.Approver1EmployeeCode as L1,
                        c.Approver1_Status,
                        c.Approver1_TimeStamp,
                        c.Approver2EmployeeCode as L2,
                        c.Approver2_Status,
                        c.Approver2_TimeStamp,
                        c.DottedLineApproverEmployeeCode as L3,
                        c.DottedApprover_Status,
                        c.DottedApprover_TimeStamp
                    FROM 
                        TRN_RotaryLine_OvenEnd c
                    LEFT JOIN 
                        MST_PlantDetails p ON c.PlantId = p.plant_id
                    LEFT JOIN
                        MST_UserMaster u ON c.SubmittedByEmployeeCode = u.EmployeeCode
                    WHERE 
                        1 = 1
                    ORDER BY 
                        c.[SubmittedDate] DESC, 
                        c.[SubmittedTime] DESC;
                ";

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        SqlDataAdapter sda = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        sda.Fill(dt);

                        // Parse the MetalCheck column containing JSON
                        //foreach (DataRow row in dt.Rows)
                        //{
                        //    if (row["MetalCheck"] != DBNull.Value)
                        //    {
                        //        string json = row["MetalCheck"].ToString();
                        //        row["MetalCheck"] = GenerateHtmlTableFromJson(json);
                        //    }
                        //}

                        // Bind the DataTable to the GridView
                        GridView1.DataSource = dt;
                        GridView1.DataBind();
                    }
                }
            }
            catch (Exception ex)
            {
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);
                ShowNotification("Error", $"An error occurred while exporting: {ex.Message}", "error");
            }
        }


        private void DataLoader()
        {
            DateTime? dateFrom = string.IsNullOrEmpty(TxtDateFrom.Text) ? (DateTime?)null : DateTime.ParseExact(TxtDateFrom.Text, "yyyy-MM-dd", null);
            DateTime? dateTo = string.IsNullOrEmpty(TxtDateTo.Text) ? (DateTime?)null : DateTime.ParseExact(TxtDateTo.Text, "yyyy-MM-dd", null);

            StringBuilder queryBuilder = new StringBuilder(@"
                SELECT TOP(30)
                    c.ID AS DBID,
                    c.FormID AS FormID,
                    p.plant_name AS PlantName,
                    l.line_name AS LineName,
                    pc.category_name AS ProductCategory,
                    pb.brand_name AS ProductBrand,
                    c.SubmittedByEmployeeCode AS EmpCode,
                    u.EmployeeName AS EmpName,
                    c.SubmittedDate AS SDate,
                    c.SubmittedTime AS STime,
                    c.Shift AS SShift,
                    'No Comment' AS Remarks,
                    c.Approver1EmployeeCode AS L1,
                    c.Approver1_Status,
                    c.Approver1_TimeStamp,
                    c.Approver2EmployeeCode AS L2,
                    c.Approver2_Status,
                    c.Approver2_TimeStamp,
                    c.DottedLineApproverEmployeeCode AS L3,
                    c.DottedApprover_Status,
                    c.DottedApprover_TimeStamp
                FROM TRN_RotaryLine_OvenEnd c
                LEFT JOIN MST_PlantDetails p ON c.PlantName = p.plant_id
                LEFT JOIN MST_Plant_Lines l ON c.Line = l.line_id
                LEFT JOIN MST_LineCategory pc ON c.ProductCategory = pc.category_id
                LEFT JOIN MST_LineCatBrands pb ON c.ProductBrand = pb.brand_id
                LEFT JOIN MST_UserMaster u ON c.SubmittedByEmployeeCode = u.EmployeeCode
                WHERE 1 = 1");

            var parameters = new List<SqlParameter>();

            // Add filters for date range
            if (dateFrom.HasValue)
            {
                queryBuilder.Append(" AND c.SubmittedDate >= @DateFrom");
                parameters.Add(new SqlParameter("@DateFrom", SqlDbType.Date) { Value = dateFrom.Value.Date });
            }

            if (dateTo.HasValue)
            {
                queryBuilder.Append(" AND c.SubmittedDate <= @DateTo");
                parameters.Add(new SqlParameter("@DateTo", SqlDbType.Date) { Value = dateTo.Value.Date });
            }

            if (!string.IsNullOrEmpty(DDL_Plant.SelectedValue) && DDL_Plant.SelectedValue != "0")
            {
                queryBuilder.Append(" AND c.PlantName = @PlantId");
                parameters.Add(new SqlParameter("@PlantId", SqlDbType.Int) { Value = DDL_Plant.SelectedValue });
            }

            if (!string.IsNullOrEmpty(DDL_PlantLine.SelectedValue) && DDL_PlantLine.SelectedValue != "0")
            {
                queryBuilder.Append(" AND c.Line = @LineName");
                parameters.Add(new SqlParameter("@LineName", SqlDbType.Int) { Value = DDL_PlantLine.SelectedValue });
            }

            if (!string.IsNullOrEmpty(DDL_ProductCategory.SelectedValue) && DDL_ProductCategory.SelectedValue != "0")
            {
                queryBuilder.Append(" AND c.ProductCategory = @ProductCategory");
                parameters.Add(new SqlParameter("@ProductCategory", SqlDbType.Int) { Value = DDL_ProductCategory.SelectedValue });
            }

            if (!string.IsNullOrEmpty(DDL_ProductBrand.SelectedValue) && DDL_ProductBrand.SelectedValue != "0")
            {
                queryBuilder.Append(" AND c.ProductBrand = @ProductBrand");
                parameters.Add(new SqlParameter("@ProductBrand", SqlDbType.Int) { Value = DDL_ProductBrand.SelectedValue });
            }

            queryBuilder.Append(" ORDER BY c.SubmittedDate DESC, c.SubmittedTime DESC");

            try
            {
                DataTable filteredData = GetDataFromTable(queryBuilder.ToString(), parameters.ToArray());

                // Convert DataTable to List<ReportInfo>
                //List<ReportInfo> dataSave = filteredData.AsEnumerable().Select(row => new ReportInfo
                //{
                //    DBID = row.Field<int>("DBID"),
                //    FormID = row.Field<int>("FormID"),
                //    PlantName = row.Field<string>("PlantName"),
                //    LineName = row.Field<string>("LineName"),
                //    ProductCategory = row.Field<string>("ProductCategory"),
                //    ProductBrand = row.Field<string>("ProductBrand"),
                //    EmpCode = row.Field<string>("EmpCode"),
                //    EmpName = row.Field<string>("EmpName"),
                //    SDate = row.Field<DateTime>("SDate"),
                //    STime = row.Field<TimeSpan>("STime"),
                //    SShift = row.Field<string>("SShift"),
                //    Remarks = row.Field<string>("Remarks"),
                //    L1 = row.Field<string>("L1"),
                //    Approver1_Status = row.Field<int?>("Approver1_Status"),
                //    Approver1_TimeStamp = row.Field<DateTime?>("Approver1_TimeStamp"),
                //    L2 = row.Field<string>("L2"),
                //    Approver2_Status = row.Field<int?>("Approver2_Status"),
                //    Approver2_TimeStamp = row.Field<DateTime?>("Approver2_TimeStamp"),
                //    L3 = row.Field<string>("L3"),
                //    DottedApprover_Status = row.Field<int?>("DottedApprover_Status"),
                //    DottedApprover_TimeStamp = row.Field<DateTime?>("DottedApprover_TimeStamp")
                //}).ToList();

                // Bind to GridView
                GridView1.DataSource = filteredData;
                GridView1.DataBind();
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                string FilterDataerrorScript = $"new PNotify({{ title: 'Error', text: '{errorMessage}', type: 'error', styling: 'bootstrap3' }});";
                ClientScript.RegisterStartupScript(this.GetType(), "FilteredDataerror", FilterDataerrorScript, true);
            }
        }

        private DataTable GetDataFromTable(string query, SqlParameter[] parameters)
        {
            DataTable dt = new DataTable();
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddRange(parameters);
                    cmd.CommandType = CommandType.Text;

                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        sda.Fill(dt);
                    }
                }
            }
            return dt;
        }

        protected void GridView1_RowCommand(object sender, System.Web.UI.WebControls.GridViewCommandEventArgs e)
        {
            int rowIndex = Convert.ToInt32(e.CommandArgument);
            GridViewRow row = GridView1.Rows[rowIndex];
            string dbid = (row.FindControl("lbl_rowid") as Label).Text;
            if (e.CommandName == "View")
            {
                Response.Redirect("qaqc_rotaryline_detailview.aspx?ID=" + dbid + "&VM=0", false);
            }
        }

    }
}