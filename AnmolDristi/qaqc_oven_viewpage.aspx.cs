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
    public partial class qaqc_oven_viewpage : System.Web.UI.Page
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

                    lbl_upper.Text = "Search Filters for OVEN  Report";
                    lbl_lower.Text = "View and Select for Detailed View  ||   ";

                    PlantBinder();
                    // loadAlldata();  // Load all data on initial load
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
            BindGridView();

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
            BindGridView();

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
            BindGridView();

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
            BindGridView();


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
            BindGridView();

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
            BindGridView();

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
            BindGridView();

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
            BindGridView();

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
            BindGridView();
        }

        public class ReportInfo
        {
            public int Id { get; set; }
            public string OVN_Id { get; set; }
            public string FormID { get; set; }
            public string SubmittedById { get; set; }
            public DateTime SubmittedDate { get; set; }
            public TimeSpan SubmittedTime { get; set; }
            public string Shift { get; set; }
            public string SubmittedByEmployeeCode { get; set; }
            public string PlantName { get; set; }
            public string Line { get; set; }
            public string ProductCategory { get; set; }
            public string ProductBrand { get; set; }
            public string SKUId { get; set; }
            public DateTime Date { get; set; }
            public string Variety { get; set; }
            public string Rejection { get; set; }
            public string ViewMode { get; set; }
            public string DeleteMode { get; set; }
            public TimeSpan Time { get; set; }
            public decimal BT_RPM { get; set; }
            public decimal Dry_Gauge { get; set; }
            public decimal Dry_Weight { get; set; }
            public decimal Dipped_Weight { get; set; }
            public decimal Square_Shape_Length { get; set; }
            public decimal Square_Shape_Width { get; set; }
            public decimal Round_Shape_Diameter { get; set; }
            public decimal PktWeight { get; set; }
            public int BiscuitsPerPkt { get; set; }
            public TimeSpan Oven_Start_Time { get; set; }
            public TimeSpan Oven_Stop_Time { get; set; }
            public TimeSpan Oven_Loss_Time { get; set; }
            public string Reason { get; set; }
            public string Approver1EmployeeCode { get; set; }
            public string Approver1_Status { get; set; }
            public DateTime? Approver1_TimeStamp { get; set; }
            public string Approver2EmployeeCode { get; set; }
            public string Approver2_Status { get; set; }
            public DateTime? Approver2_TimeStamp { get; set; }
            public string DottedLineApproverEmployeeCode { get; set; }
            public string DottedApprover_Status { get; set; }
            public DateTime? DottedApprover_TimeStamp { get; set; }
        }

        private DataTable GetFilteredData()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            DataTable dt = new DataTable();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string sqlQuery = "SELECT * FROM [TRN_FINAL_OVEN_REPORT] WHERE 1=1";

                // Add filters to SQL query based on the selected values
                if (!string.IsNullOrEmpty(DDL_Plant.SelectedValue) && DDL_Plant.SelectedValue != "0")
                {
                    sqlQuery += " AND PlantName = @Plant_Name";
                }

                if (!string.IsNullOrEmpty(DDL_PlantLine.SelectedValue) && DDL_PlantLine.SelectedValue != "0")
                {
                    sqlQuery += " AND Line = @Plant_Line";
                }

                if (!string.IsNullOrEmpty(DDL_ProductCategory.SelectedValue) && DDL_ProductCategory.SelectedValue != "0")
                {
                    sqlQuery += " AND ProductCategory = @Product_Category";
                }

                if (!string.IsNullOrEmpty(DDL_ProductBrand.SelectedValue) && DDL_ProductBrand.SelectedValue != "0")
                {
                    sqlQuery += " AND ProductBrand = @Product_Brand";
                }

                if (!string.IsNullOrEmpty(DDL_BrandSKU.SelectedValue) && DDL_BrandSKU.SelectedValue != "0")
                {
                    sqlQuery += " AND SKUId = @SkuId";
                }

                using (SqlCommand cmd = new SqlCommand(sqlQuery, con))
                {
                    // Add parameters only if they are being used in the query
                    if (!string.IsNullOrEmpty(DDL_Plant.SelectedValue) && DDL_Plant.SelectedValue != "0")
                    {
                        cmd.Parameters.AddWithValue("@Plant_Name", DDL_Plant.SelectedValue);
                    }

                    if (!string.IsNullOrEmpty(DDL_PlantLine.SelectedValue) && DDL_PlantLine.SelectedValue != "0")
                    {
                        cmd.Parameters.AddWithValue("@Plant_Line", DDL_PlantLine.SelectedValue);
                    }

                    if (!string.IsNullOrEmpty(DDL_ProductCategory.SelectedValue) && DDL_ProductCategory.SelectedValue != "0")
                    {
                        cmd.Parameters.AddWithValue("@Product_Category", DDL_ProductCategory.SelectedValue);
                    }

                    if (!string.IsNullOrEmpty(DDL_ProductBrand.SelectedValue) && DDL_ProductBrand.SelectedValue != "0")
                    {
                        cmd.Parameters.AddWithValue("@Product_Brand", DDL_ProductBrand.SelectedValue);
                    }

                    if (!string.IsNullOrEmpty(DDL_BrandSKU.SelectedValue) && DDL_BrandSKU.SelectedValue != "0")
                    {
                        cmd.Parameters.AddWithValue("@SkuId", DDL_BrandSKU.SelectedValue);
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
                    OVN_Id = row["OVN_Id"] == DBNull.Value ? string.Empty : row["OVN_Id"].ToString(),
                    FormID = row["FormID"] == DBNull.Value ? string.Empty : row["FormID"].ToString(),
                    SubmittedById = row["SubmittedById"] == DBNull.Value ? string.Empty : row["SubmittedById"].ToString(),
                    SubmittedDate = row["SubmittedDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["SubmittedDate"]),
                    SubmittedTime = row["SubmittedTime"] == DBNull.Value ? TimeSpan.Zero : TimeSpan.Parse(row["SubmittedTime"].ToString()),
                    Shift = row["Shift"] == DBNull.Value ? string.Empty : row["Shift"].ToString(),
                    SubmittedByEmployeeCode = row["SubmittedByEmployeeCode"] == DBNull.Value ? string.Empty : row["SubmittedByEmployeeCode"].ToString(),
                    PlantName = row["PlantName"] == DBNull.Value ? string.Empty : row["PlantName"].ToString(),
                    Line = row["Line"] == DBNull.Value ? string.Empty : row["Line"].ToString(),
                    ProductCategory = row["ProductCategory"] == DBNull.Value ? string.Empty : row["ProductCategory"].ToString(),
                    ProductBrand = row["ProductBrand"] == DBNull.Value ? string.Empty : row["ProductBrand"].ToString(),
                    SKUId = row["SKUId"] == DBNull.Value ? string.Empty : row["SKUId"].ToString(),
                    Date = row["Date"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["Date"]),
                    Variety = row["Variety"] == DBNull.Value ? string.Empty : row["Variety"].ToString(),
                    Rejection = row["Rejection"] == DBNull.Value ? string.Empty : row["Rejection"].ToString(),
                    ViewMode = row["ViewMode"] == DBNull.Value ? string.Empty : row["ViewMode"].ToString(),
                    DeleteMode = row["DeleteMode"] == DBNull.Value ? string.Empty : row["DeleteMode"].ToString(),
                    //Time = row["Time"] == DBNull.Value ? TimeSpan.Zero : TimeSpan.Parse(row["Time"].ToString()),
                    BT_RPM = row["BT_RPM"] == DBNull.Value ? 0 : Convert.ToDecimal(row["BT_RPM"]),
                    Dry_Gauge = row["Dry_Gauge"] == DBNull.Value ? 0 : Convert.ToDecimal(row["Dry_Gauge"]),
                    Dry_Weight = row["Dry_Weight"] == DBNull.Value ? 0 : Convert.ToDecimal(row["Dry_Weight"]),
                    Dipped_Weight = row["Dipped_Weight"] == DBNull.Value ? 0 : Convert.ToDecimal(row["Dipped_Weight"]),
                    Square_Shape_Length = row["Square_Shape_Length"] == DBNull.Value ? 0 : Convert.ToDecimal(row["Square_Shape_Length"]),
                    Square_Shape_Width = row["Square_Shape_Width"] == DBNull.Value ? 0 : Convert.ToDecimal(row["Square_Shape_Width"]),
                    Round_Shape_Diameter = row["Round_Shape_Diameter"] == DBNull.Value ? 0 : Convert.ToDecimal(row["Round_Shape_Diameter"]),
                    PktWeight = row["PktWeight"] == DBNull.Value ? 0 : Convert.ToDecimal(row["PktWeight"]),
                    BiscuitsPerPkt = row["BiscuitsPerPkt"] == DBNull.Value ? 0 : Convert.ToInt32(row["BiscuitsPerPkt"]),
                    Oven_Start_Time = row["Oven_Start_Time"] == DBNull.Value ? TimeSpan.Zero : TimeSpan.Parse(row["Oven_Start_Time"].ToString()),
                    Oven_Stop_Time = row["Oven_Stop_Time"] == DBNull.Value ? TimeSpan.Zero : TimeSpan.Parse(row["Oven_Stop_Time"].ToString()),
                    Oven_Loss_Time = row["Oven_Loss_Time"] == DBNull.Value ? TimeSpan.Zero : TimeSpan.Parse(row["Oven_Loss_Time"].ToString()),
                    Reason = row["Reason"] == DBNull.Value ? string.Empty : row["Reason"].ToString(),
                    Approver1EmployeeCode = row["Approver1EmployeeCode"] == DBNull.Value ? string.Empty : row["Approver1EmployeeCode"].ToString(),
                    Approver1_Status = row["Approver1_Status"] == DBNull.Value ? string.Empty : row["Approver1_Status"].ToString(),
                    Approver1_TimeStamp = row["Approver1_TimeStamp"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["Approver1_TimeStamp"]),
                    Approver2EmployeeCode = row["Approver2EmployeeCode"] == DBNull.Value ? string.Empty : row["Approver2EmployeeCode"].ToString(),
                    Approver2_Status = row["Approver2_Status"] == DBNull.Value ? string.Empty : row["Approver2_Status"].ToString(),
                    Approver2_TimeStamp = row["Approver2_TimeStamp"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["Approver2_TimeStamp"]),
                    DottedLineApproverEmployeeCode = row["DottedLineApproverEmployeeCode"] == DBNull.Value ? string.Empty : row["DottedLineApproverEmployeeCode"].ToString(),
                    DottedApprover_Status = row["DottedApprover_Status"] == DBNull.Value ? string.Empty : row["DottedApprover_Status"].ToString(),
                    DottedApprover_TimeStamp = row["DottedApprover_TimeStamp"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["DottedApprover_TimeStamp"])
                };

                dataSave.Add(info);
            }

            // Bind to GridView
            GridView1.DataSource = dataSave;
            GridView1.DataBind();
        }

        protected void btn_view_submit_Click(object sender, EventArgs e)
        {
            // Retrieve date range from textboxes
            DateTime? dateFrom = string.IsNullOrEmpty(TxtDateFrom.Text) ? (DateTime?)null : DateTime.ParseExact(TxtDateFrom.Text, "yyyy-MM-dd", null);
            DateTime? dateTo = string.IsNullOrEmpty(TxtDateTo.Text) ? (DateTime?)null : DateTime.ParseExact(TxtDateTo.Text, "yyyy-MM-dd", null);

            // SQL query to fetch data based on date range
            StringBuilder queryBuilder = new StringBuilder("SELECT * FROM TRN_FINAL_OVEN_REPORT WHERE 1 = 1");
            var parameters = new List<SqlParameter>();

            if (dateFrom.HasValue)
            {
                queryBuilder.Append(" AND SubmittedDate >= @DateFrom");
                parameters.Add(new SqlParameter("@DateFrom", SqlDbType.Date) { Value = dateFrom.Value.Date });
            }

            if (dateTo.HasValue)
            {
                queryBuilder.Append(" AND SubmittedDate <= @DateTo");
                parameters.Add(new SqlParameter("@DateTo", SqlDbType.Date) { Value = dateTo.Value.Date });
            }

            try
            {
                // Fetch filtered data using the constructed query and parameters
                DataTable filteredData = GetDataFromTable(queryBuilder.ToString(), parameters.ToArray());

                // Bind filtered data to GridView
                GridView1.DataSource = filteredData;
                GridView1.DataBind();

                // Show a notification if no records are found
                if (filteredData.Rows.Count == 0)
                {
                    //ShowNotification("No Data", "No records found for the selected date range.", "info");
                }
            }
            catch (Exception ex )
            {
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                // Handle exceptions (log it, show an error message, etc.)
                ShowNotification("Error", "An error occurred while processing your request.", "error");
            }
        }
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

        protected void btn_view_cancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("home.aspx");
            
        }

        protected void btn_view_reset_Click(object sender, EventArgs e)
        {
            Response.Redirect("qaqc_oven_viewpage.aspx");
            
          
        }

        protected void btn_view_export_Click(object sender, EventArgs e)
        {
            try
            {
                // Fetch the data that needs to be exported
                DataTable exportData = GetDataForExport(); // Implement this function to retrieve data from DB

                if (exportData != null && exportData.Rows.Count > 0)
                {
                    // Set the response settings
                    Response.Clear();
                    Response.Buffer = true;
                    Response.AddHeader("content-disposition", "attachment;filename=OvenReport.xls");
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
                    //ShowNotification("No Data", "No data available to export.", "info");
                }
            }
            catch (Exception ex)
            {
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                // ShowNotification("Error", $"An error occurred while exporting: {ex.Message}", "error");
            }
        }

        public override void VerifyRenderingInServerForm(Control control)
        {
            // Confirms that an HtmlForm control is rendered for the specified ASP.NET
            // This method is necessary to allow GridView control rendering in the export
        }

        private DataTable GetDataForExport()
        {
            // Your logic to retrieve data from the database for export.
            // This can be the same query you use to bind the GridView, or include applied filters
            StringBuilder queryBuilder = new StringBuilder("SELECT * FROM TRN_FINAL_OVEN_REPORT");
            var parameters = new List<SqlParameter>();

            // Add filters or parameters if needed, similar to the BindGridView method

            return GetDataFromTable(queryBuilder.ToString(), parameters.ToArray());
        }


    }
}