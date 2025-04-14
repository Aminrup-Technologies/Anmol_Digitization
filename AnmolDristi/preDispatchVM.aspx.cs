using AnmolDristi.DAL;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Configuration;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Security.Cryptography;
//using Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using System.Text;
//using ExcelInterop = Microsoft.Office.Interop.Excel;

namespace AnmolDristi
{
    public partial class preDispatchVM : System.Web.UI.Page
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

                    lbl_docnumber.Text = "Search Filters for Pre Dispatch QI Report";
                    label_lower.Text = "View and Select for Detailed View  ||  ";

                    PlantBinder();
                    loadAlldata();


                    //LoadAllData();
                    //BindGridView();
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
        }

        protected void DDL_ProductBrand_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_ProductBrand.SelectedIndex != 0)
            {
                string selectedProductBrandValue = DDL_ProductBrand.SelectedValue.ToString();
                BrandSKUBinder(selectedProductBrandValue);

                System.Data.DataTable dataTable = DatabaseHelper.GetBrandFieldsControlByBrandId(Convert.ToInt16(selectedProductBrandValue));


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

        public void LoadAllData()
        {
            SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString);
            SqlCommand cmd = new SqlCommand("select * from TRN_Pre_Dispatch_Clearance_Report", con);
            SqlDataAdapter sda = new SqlDataAdapter(cmd);
            System.Data.DataTable dt = new System.Data.DataTable();
            sda.Fill(dt);
            GridView1.DataSource = dt;
            GridView1.DataBind();
        }

        public class ReportInfo
        {
            public int Id { get; set; }
            public string FormID { get; set; }
            public string PDCR_PK { get; set; }
            public string PlantName { get; set; }
            public string Line { get; set; }
            public string ProductCategory { get; set; }
            public string ProductBrand { get; set; }
            public string SKUId { get; set; }
            public string InspectionLot { get; set; }
            public string MaterialCode { get; set; }
            public decimal? CBB_Produced { get; set; }
            public decimal? CBB_Checked { get; set; }
            public bool? CBB_Box_Condition { get; set; }
            public string CBB_Box_ConditionRemarks { get; set; }
            public decimal? Packets_CBB { get; set; }
            public bool? CBB_tapping { get; set; }
            public string CBB_tappingRemarks { get; set; }
            public decimal? PacketsChecked_CBB { get; set; }
            public decimal? WeightofPackets { get; set; }
            public DateTime? PackageDate { get; set; }
            public string BatchNo { get; set; }
            public decimal? PacketsMRP { get; set; }
            public bool? LongSeal { get; set; }
            public string LongSealRemarks { get; set; }
            public bool? EndSeal { get; set; }
            public string EndSealRemarks { get; set; }
            public bool? MainPanel { get; set; }
            public string MainPanelRemarks { get; set; }
            public bool? Cuts_Packets { get; set; }
            public string Cuts_PacketsRemarks { get; set; }
            public bool? BackingStatus { get; set; }
            public string BackingStatusRemarks { get; set; }
            public bool? ElongOval { get; set; }
            public string ElongOvalRemarks { get; set; }
            public bool? Cupping { get; set; }
            public string CuppingRemarks { get; set; }
            public bool? Impression { get; set; }
            public string ImpressionRemarks { get; set; }
            public bool? SoggyStatus { get; set; }
            public string SoggyStatusRemarks { get; set; }
            public bool? ForeignBody { get; set; }
            public string ForeignBodyRemarks { get; set; }
            public bool? OffOdour { get; set; }
            public string OffOdourRemarks { get; set; }
            public string Remarks { get; set; }
            public string Shift { get; set; }
            public int SubmittedById { get; set; }
            public DateTime? SubmittedDate { get; set; }
            public TimeSpan? SubmittedTime { get; set; }
            public string SubmittedByEmployeeCode { get; set; }
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

        private System.Data.DataTable GetFilteredData()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            System.Data.DataTable dt = new System.Data.DataTable();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string sqlQuery = "SELECT * FROM [dbo].[TRN_Pre_Dispatch_Clearance_Report] WHERE ViewMode=1";

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
                    sqlQuery += " AND SKUId = @SKUId";
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
                        cmd.Parameters.AddWithValue("@SKUId", DDL_BrandSKU.SelectedValue);
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
            System.Data.DataTable dt = GetFilteredData();

            // Convert DataTable to List<ReportInfo>
            List<ReportInfo> dataSave = new List<ReportInfo>();

            foreach (DataRow row in dt.Rows)
            {
                ReportInfo info = new ReportInfo
                {
                    Id = row["Id"] == DBNull.Value ? 0 : Convert.ToInt32(row["Id"]),
                    FormID = row["FormID"] == DBNull.Value ? string.Empty : row["FormID"].ToString(),
                    PDCR_PK = row["PDCR_PK"] == DBNull.Value ? string.Empty : row["PDCR_PK"].ToString(),
                    PlantName = row["PlantName"] == DBNull.Value ? string.Empty : row["PlantName"].ToString(),
                    Line = row["Line"] == DBNull.Value ? string.Empty : row["Line"].ToString(),
                    ProductCategory = row["ProductCategory"] == DBNull.Value ? string.Empty : row["ProductCategory"].ToString(),
                    ProductBrand = row["ProductBrand"] == DBNull.Value ? string.Empty : row["ProductBrand"].ToString(),
                    SKUId = row["SKUId"] == DBNull.Value ? string.Empty : row["SKUId"].ToString(),
                    InspectionLot = row["InspectionLot"] == DBNull.Value ? string.Empty : row["InspectionLot"].ToString(),
                    MaterialCode = row["MaterialCode"] == DBNull.Value ? string.Empty : row["MaterialCode"].ToString(),
                    CBB_Produced = row["CBB_Produced"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(row["CBB_Produced"]),
                    CBB_Checked = row["CBB_Checked"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(row["CBB_Checked"]),
                    CBB_Box_Condition = row["CBB_Box_Condition"] == DBNull.Value ? (bool?)null : Convert.ToBoolean(row["CBB_Box_Condition"]),
                    CBB_Box_ConditionRemarks = row["CBB_Box_ConditionRemarks"] == DBNull.Value ? string.Empty : row["CBB_Box_ConditionRemarks"].ToString(),
                    Packets_CBB = row["Packets_CBB"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(row["Packets_CBB"]),
                    CBB_tapping = row["CBB_tapping"] == DBNull.Value ? (bool?)null : Convert.ToBoolean(row["CBB_tapping"]),
                    CBB_tappingRemarks = row["CBB_tappingRemarks"] == DBNull.Value ? string.Empty : row["CBB_tappingRemarks"].ToString(),
                    PacketsChecked_CBB = row["PacketsChecked_CBB"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(row["PacketsChecked_CBB"]),
                    WeightofPackets = row["WeightofPackets"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(row["WeightofPackets"]),
                    PackageDate = row["PackageDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["PackageDate"]),
                    BatchNo = row["BatchNo"] == DBNull.Value ? string.Empty : row["BatchNo"].ToString(),
                    PacketsMRP = row["PacketsMRP"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(row["PacketsMRP"]),
                    LongSeal = row["LongSeal"] == DBNull.Value ? (bool?)null : Convert.ToBoolean(row["LongSeal"]),
                    LongSealRemarks = row["LongSealRemarks"] == DBNull.Value ? string.Empty : row["LongSealRemarks"].ToString(),
                    EndSeal = row["EndSeal"] == DBNull.Value ? (bool?)null : Convert.ToBoolean(row["EndSeal"]),
                    EndSealRemarks = row["EndSealRemarks"] == DBNull.Value ? string.Empty : row["EndSealRemarks"].ToString(),
                    MainPanel = row["MainPanel"] == DBNull.Value ? (bool?)null : Convert.ToBoolean(row["MainPanel"]),
                    MainPanelRemarks = row["MainPanelRemarks"] == DBNull.Value ? string.Empty : row["MainPanelRemarks"].ToString(),
                    Cuts_Packets = row["Cuts_Packets"] == DBNull.Value ? (bool?)null : Convert.ToBoolean(row["Cuts_Packets"]),
                    Cuts_PacketsRemarks = row["Cuts_PacketsRemarks"] == DBNull.Value ? string.Empty : row["Cuts_PacketsRemarks"].ToString(),
                    BackingStatus = row["BackingStatus"] == DBNull.Value ? (bool?)null : Convert.ToBoolean(row["BackingStatus"]),
                    BackingStatusRemarks = row["BackingStatusRemarks"] == DBNull.Value ? string.Empty : row["BackingStatusRemarks"].ToString(),
                    ElongOval = row["ElongOval"] == DBNull.Value ? (bool?)null : Convert.ToBoolean(row["ElongOval"]),
                    ElongOvalRemarks = row["ElongOvalRemarks"] == DBNull.Value ? string.Empty : row["ElongOvalRemarks"].ToString(),
                    Cupping = row["Cupping"] == DBNull.Value ? (bool?)null : Convert.ToBoolean(row["Cupping"]),
                    CuppingRemarks = row["CuppingRemarks"] == DBNull.Value ? string.Empty : row["CuppingRemarks"].ToString(),
                    Impression = row["Impression"] == DBNull.Value ? (bool?)null : Convert.ToBoolean(row["Impression"]),
                    ImpressionRemarks = row["ImpressionRemarks"] == DBNull.Value ? string.Empty : row["ImpressionRemarks"].ToString(),
                    SoggyStatus = row["SoggyStatus"] == DBNull.Value ? (bool?)null : Convert.ToBoolean(row["SoggyStatus"]),
                    SoggyStatusRemarks = row["SoggyStatusRemarks"] == DBNull.Value ? string.Empty : row["SoggyStatusRemarks"].ToString(),
                    ForeignBody = row["ForeignBody"] == DBNull.Value ? (bool?)null : Convert.ToBoolean(row["ForeignBody"]),
                    ForeignBodyRemarks = row["ForeignBodyRemarks"] == DBNull.Value ? string.Empty : row["ForeignBodyRemarks"].ToString(),
                    OffOdour = row["OffOdour"] == DBNull.Value ? (bool?)null : Convert.ToBoolean(row["OffOdour"]),
                    OffOdourRemarks = row["OffOdourRemarks"] == DBNull.Value ? string.Empty : row["OffOdourRemarks"].ToString(),
                    Remarks = row["Remarks"] == DBNull.Value ? string.Empty : row["Remarks"].ToString(),
                    Shift = row["Shift"] == DBNull.Value ? string.Empty : row["Shift"].ToString(),
                    SubmittedById = row["SubmittedById"] == DBNull.Value ? 0 : Convert.ToInt32(row["SubmittedById"]),
                    SubmittedDate = row["SubmittedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["SubmittedDate"]),
                    SubmittedTime = row["SubmittedTime"] == DBNull.Value ? (TimeSpan?)null : TimeSpan.Parse(row["SubmittedTime"].ToString()),
                    SubmittedByEmployeeCode = row["SubmittedByEmployeeCode"] == DBNull.Value ? string.Empty : row["SubmittedByEmployeeCode"].ToString(),
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

        protected void Submit_Click(object sender, EventArgs e)
        {
            DateTime fromDate = Convert.ToDateTime(TXT_PackageDateFrom.Text);
            DateTime toDate = Convert.ToDateTime(TXT_PackageDateTo.Text);
            if (toDate > DateTime.Now)
            {
                Response.Write("<script>alert('ToDate cannot be greater than current date!');</script>");
            }
            else if (fromDate > toDate)
            {
                Response.Write("<script>alert('FromDate cannot be greater than ToDate!');</script>");
            }
            else
            {
                DataLoader();
            }
        }

        private void getReportData(DateTime fromDate, DateTime toDate)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SP_PDCR_DateFilter", con))
                {
                    cmd.Parameters.AddWithValue("@FromDate", fromDate);
                    cmd.Parameters.AddWithValue("@ToDate", toDate);
                    cmd.CommandType = CommandType.StoredProcedure;
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        System.Data.DataTable dt = new System.Data.DataTable();
                        sda.Fill(dt);
                        if (dt.Rows.Count > 0)
                        {
                            GridView1.DataSource = dt;
                            GridView1.DataBind();
                        }
                    }
                }
            }
        }

        protected void Cancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("home.aspx");
        }

        protected void Reset_Click(object sender, EventArgs e)
        {
            Response.Redirect("preDispatchVM.aspx");
        }

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

        //export button
        //protected void ExportBtn_Click(object sender, EventArgs e)
        //{
        //    string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        //    string sqlQuery = "SELECT * FROM [dbo].[TRN_Pre_Dispatch_Clearance_Report] WHERE 1=1";

        //    using (SqlConnection con = new SqlConnection(connectionString))
        //    {
        //        using (SqlCommand cmd = new SqlCommand(sqlQuery, con))
        //        {
        //            //cmd.CommandType = CommandType.StoredProcedure;
        //            con.Open();

        //            using (SqlDataReader reader = cmd.ExecuteReader())
        //            {
        //                if (reader.HasRows)
        //                {
        //                    string GridViewDataExportedFileInfo = "PDCR_ViewPage_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + ".xls";

        //                    #region Export In Documents Folder
        //                    object misValue = System.Reflection.Missing.Value;
        //                    ExcelInterop.Application application = new ExcelInterop.Application();
        //                    application.Visible = false;

        //                    ExcelInterop.Workbook workbook = application.Workbooks.Add(misValue);
        //                    ExcelInterop.Worksheet worksheet = (ExcelInterop.Worksheet)workbook.Worksheets[1];
        //                    worksheet.Name = "PDCR_ViewPage_";
        //                    worksheet.Cells.Font.Size = 12;

        //                    // Add Column Headers from SqlDataReader
        //                    for (int i = 0; i < reader.FieldCount; i++)
        //                    {
        //                        worksheet.Cells[1, i + 1] = reader.GetName(i);  // Adding column headers
        //                    }

        //                    // Add Data from SqlDataReader
        //                    int rowIndex = 2; // Data starts from row 2
        //                    while (reader.Read())
        //                    {
        //                        for (int i = 0; i < reader.FieldCount; i++)
        //                        {
        //                            worksheet.Cells[rowIndex, i + 1] = reader.GetValue(i).ToString();  // Add data to the cells
        //                        }
        //                        rowIndex++;
        //                    }

        //                    // Apply AutoFilter
        //                    ExcelInterop.Range usedRange = worksheet.UsedRange;
        //                    usedRange.AutoFilter(1, Type.Missing, ExcelInterop.XlAutoFilterOperator.xlFilterValues, Type.Missing, true);

        //                    // Save the workbook
        //                    workbook.SaveAs(GridViewDataExportedFileInfo,
        //                        ExcelInterop.XlFileFormat.xlWorkbookNormal, misValue, misValue, misValue, misValue,
        //                        ExcelInterop.XlSaveAsAccessMode.xlExclusive, misValue, misValue, misValue, misValue, misValue);

        //                    // Close the workbook and quit the application
        //                    workbook.Close(true, misValue, misValue);
        //                    application.Quit();

        //                    // Notify the user
        //                    this.ClientScript.RegisterStartupScript(this.GetType(), "GridViewData Exported Alert Box.",
        //                        "alert('Data File Exported with name " + GridViewDataExportedFileInfo + " in Document folder');", true);
        //                    #endregion
        //                }
        //                else
        //                {
        //                    this.ClientScript.RegisterStartupScript(this.GetType(), "No Data Alert Box.",
        //                        "alert('There are no records to Download.');", true);
        //                }
        //            }
        //        }
        //    }
        //}

        //add colunm in sheet
        //public void AddColumnInSheetFromReader(Worksheet worksheet, SqlDataReader reader)
        //{
        //    for (int i = 0; i < reader.FieldCount; i++)
        //    {
        //        string columnName = reader.GetName(i);
        //        worksheet.Cells[1, i + 1] = columnName; // Write column headers in the first row
        //    }
        //}



        private void loadAlldata()
        {
            try
            {
                string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

                string query = @"
                    SELECT TOP(30)
                        c.ID AS DBID,
                        c.FormID as FormID,
                        'NA' as RecordID,
                        p.plant_name AS PlantName,
                        l.line_name AS LineName,
	                    pc.category_name AS ProductCategory,
	                    pb.brand_name AS ProductBrand,
                        c.SubmittedByEmployeeCode as EmpCode,
                        u.EmployeeName AS EmpName,
                        c.SubmittedDate as SDate,
                        c.SubmittedTime as STime,
                        c.Shift as SShift,
                        c.Remarks as Remarks,
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
                        TRN_Pre_Dispatch_Clearance_Report c
                    LEFT JOIN 
                        MST_PlantDetails p ON c.PlantName = p.plant_id
                    LEFT JOIN 
                        MST_Plant_Lines l ON c.Line = l.line_id
                    LEFT JOIN 
                        MST_LineCategory pc ON c.ProductCategory = pc.category_id
                    LEFT JOIN 
                        MST_LineCatBrands pb ON c.ProductBrand = pb.brand_id
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
                        GridView1.DataSource = dt;
                        GridView1.DataBind();
                    }
                }
            }
            catch (Exception ex)
            {
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);
            }
        }

        private void DataLoader()
        {
            DateTime? dateFrom = string.IsNullOrEmpty(TXT_PackageDateFrom.Text) ? (DateTime?)null : DateTime.ParseExact(TXT_PackageDateFrom.Text, "yyyy-MM-dd", null);
            DateTime? dateTo = string.IsNullOrEmpty(TXT_PackageDateTo.Text) ? (DateTime?)null : DateTime.ParseExact(TXT_PackageDateTo.Text, "yyyy-MM-dd", null);

            StringBuilder queryBuilder = new StringBuilder(@"
                SELECT 
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
                FROM TRN_Pre_Dispatch_Clearance_Report c
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
                Response.Redirect("Pre_Dispatch_Detailed.aspx?ID=" + dbid + "&VM=1", false);
            }
        }
    }
}