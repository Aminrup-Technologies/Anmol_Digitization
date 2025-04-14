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
//using ExcelInterop = Microsoft.Office.Interop.Excel;


namespace AnmolDristi
{
    public partial class FinalCbbWtVM : System.Web.UI.Page
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

                    lbl_docnumber.Text = "Search Filters for CBB Weight Checklist Report";
                    label_lower.Text = "View and Select for Detailed View  ||  ";

                    PlantBinder();
                    LoadAllData();
                    BindGridView();
                }

            }
        }

        public void LoadAllData()
        {
            SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString);
            SqlCommand cmd = new SqlCommand("select * from TRN_FINAL_CBB_Weights", con);
            SqlDataAdapter sda = new SqlDataAdapter(cmd);
            System.Data.DataTable dt = new System.Data.DataTable();
            sda.Fill(dt);
            GridView1.DataSource = dt;
            GridView1.DataBind();
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
            GetFilteredData();
        }

        protected void GridView1_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                for (int i = 0; i < e.Row.Cells.Count; i++)
                {
                    // Check if the next row exists
                    if (e.Row.RowIndex > 0)
                    {
                        GridViewRow previousRow = GridView1.Rows[e.Row.RowIndex - 1];

                        // If the current cell and the previous cell have the same text, merge them
                        if (e.Row.Cells[i].Text == previousRow.Cells[i].Text)
                        {
                            previousRow.Cells[i].RowSpan = previousRow.Cells[i].RowSpan < 2 ? 2 : previousRow.Cells[i].RowSpan + 1;
                            e.Row.Cells[i].Visible = false;
                        }
                    }
                }
            }
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
                getReportData(fromDate, toDate);
            }
        }

        private void getReportData(DateTime fromDate, DateTime toDate)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SP_CBB_DateFilter", con))
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
            Response.Redirect("FinalCbbWtVM.aspx");
        }

        public class ReportInfo
        {
            public int Id { get; set; }
            public string CBB_PK { get; set; }
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
            public string BatchNo { get; set; }
            public decimal MRP { get; set; }
            public string GrossWeightJson { get; set; }
            public decimal AverageGrossWeight { get; set; }
            public string ViewMode { get; set; }
            public string DeleteMode { get; set; }
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
                string sqlQuery = "SELECT * FROM TRN_FINAL_CBB_Weights WHERE ViewMode=1";

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

                sqlQuery += " order by Id desc";

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
            System.Data.DataTable dt = GetFilteredData();

            // Convert DataTable to List<ReportInfo>
            List<ReportInfo> dataSave = new List<ReportInfo>();

            foreach (DataRow row in dt.Rows)
            {
                ReportInfo info = new ReportInfo
                {
                    Id = row["Id"] == DBNull.Value ? 0 : Convert.ToInt32(row["Id"]),
                    CBB_PK = row["CBB_PK"] == DBNull.Value ? string.Empty : row["CBB_PK"].ToString(),
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
                    BatchNo = row["BatchNo"] == DBNull.Value ? string.Empty : row["BatchNo"].ToString(),
                    MRP = row["MRP"] == DBNull.Value ? 0 : Convert.ToDecimal(row["MRP"]),
                    GrossWeightJson = row["GrossWeightJson"] == DBNull.Value ? string.Empty : row["GrossWeightJson"].ToString(),
                    AverageGrossWeight = row["AverageGrossWeight"] == DBNull.Value ? 0 : Convert.ToDecimal(row["AverageGrossWeight"]),
                    ViewMode = row["ViewMode"] == DBNull.Value ? string.Empty : row["ViewMode"].ToString(),
                    DeleteMode = row["DeleteMode"] == DBNull.Value ? string.Empty : row["DeleteMode"].ToString(),
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

        //export button
        //protected void ExportBtn_Click(object sender, EventArgs e)
        //{
        //    string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        //    string sqlQuery = "SELECT TOP (1000) [Id], [CBB_PK], [FormID], [SubmittedById], [SubmittedDate], [SubmittedTime], [Shift], [SubmittedByEmployeeCode], [PlantName], [Line], [ProductCategory], [ProductBrand], [SKUId], [BatchNo], [MRP], [GrossWeightJson], [AverageGrossWeight], [ViewMode], [DeleteMode], [Approver1EmployeeCode], [Approver1_Status], [Approver1_TimeStamp], [Approver2EmployeeCode], [Approver2_Status], [Approver2_TimeStamp], [DottedLineApproverEmployeeCode], [DottedApprover_Status], [DottedApprover_TimeStamp] FROM [AnmolDristi].[dbo].[TRN_FINAL_CBB_Weights] WHERE 1=1";

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
        //                    string GridViewDataExportedFileInfo = "CBB_Weight_ViewPage_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + ".xls";

        //                    #region Export In Documents Folder
        //                    object misValue = System.Reflection.Missing.Value;
        //                    ExcelInterop.Application application = new ExcelInterop.Application();
        //                    application.Visible = false;

        //                    ExcelInterop.Workbook workbook = application.Workbooks.Add(misValue);
        //                    ExcelInterop.Worksheet worksheet = (ExcelInterop.Worksheet)workbook.Worksheets[1];
        //                    worksheet.Name = "CBB_Weight_ViewPage_";
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

    }
}