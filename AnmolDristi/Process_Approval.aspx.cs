using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.IO;
using System.Web.Services;

//using Microsoft.Office.Interop.Excel;
//using System.Runtime.InteropServices; // Optional, for Excel interop cleanup
//using ExcelInterop = Microsoft.Office.Interop.Excel;



namespace AnmolDristi
{
    public partial class Process_Approval : System.Web.UI.Page
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

                    lbl_docname.Text = "Search Filter for QA Process Checking Report";
                    lbl_viewname.Text = "View and Search for Detailed View || ";
                    PlantBinder();
                    BindGridView();
                    ShowHideGridViewColumns();

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

        protected void DDL_Plant_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Plant.SelectedIndex != 0)
            {
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                PlantLinesBinder(selectedPlantValue);
                FilterData();
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
                FilterData();
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
                FilterData();
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
                FilterData();
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
            FilterData();
        }


        protected void ReportbtnCancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("home.aspx");
        }
        protected void ReportbtnReset_Click(object sender, EventArgs e)
        {
            Response.Redirect("Process_Approval.aspx");
        }
        protected void ReportbtnSubmit_Click(object sender, EventArgs e)
        {
            DateTime fromDate = Convert.ToDateTime(TB_Date_From.Text);
            DateTime toDate = Convert.ToDateTime(TB_Date_To.Text);
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
                using (SqlCommand cmd = new SqlCommand("SP_PCR_DateFilter", con))
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
        public class ReportInfo
        {
            public string PcrNo { get; set; }
            public string plant_name { get; set; }
            public string line_name { get; set; }
            public string category_name { get; set; }
            public string brand_name { get; set; }
            public string SKU_name { get; set; }
            public string SubmittedDate { get; set; }
            public string SubmittedTime { get; set; }
            public string SubmittedById { get; set; }
            public string ProcessWaterTemp { get; set; }
            public string WaterPH { get; set; }
            public string WaterHardness { get; set; }
            public string WaterTest { get; set; }
            public string TDS { get; set; }
            public string MaidaBrand { get; set; }
            public string MaidaBatchNo { get; set; }
            public string MaidaMfgDate { get; set; }
            public string MaidaAppearanceColor { get; set; }
            public string CommentForMaidaColor { get; set; }
            public string MaidaFlavorAndTaste { get; set; }
            public string CommentsForMaidaFlavourAndTaste { get; set; }
            public string MaidaGrittiness { get; set; }
            public string CommentForGrittiness { get; set; }
            public string BBAppearanceColor { get; set; }
            public string CommentForBBColor { get; set; }
            public string BBMouthFeel { get; set; }
            public string CommentForBBMouthFeel { get; set; }
            public string BBFlavorAndTaste { get; set; }
            public string CommentForBBFlavorAndTaste { get; set; }
            public string HvoSmell { get; set; }
            public string CommentForHvoSmell { get; set; }
            public string HvoTaste { get; set; }
            public string CommentForHvoTaste { get; set; }
            public string HvoTemp { get; set; }
            public string SmpSmell { get; set; }
            public string CommentForSmpSmell { get; set; }
            public string SmpTaste { get; set; }
            public string CommentForSmpTaste { get; set; }
            public string SmpColor { get; set; }
            public string CommentForSmpColor { get; set; }
            public string SyrupTemp { get; set; }
            public string SyrupColor { get; set; }
            public string CommentForSyrupColor { get; set; }
            public string SyrupPH { get; set; }
            public string InvertSyrpBucketFilter { get; set; }
            public string CommentForISBF { get; set; }
            public string SugarSolBucketFilter { get; set; }
            public string CommentForSSBF { get; set; }
            public string CreamerBucketFilter { get; set; }
            public string CommentForCBF { get; set; }
            public string SugarGrindedSheet { get; set; }
            public string CommentForSGS { get; set; }
            public string OilSystemBucketFilter { get; set; }
            public string CommentForOSBF { get; set; }
            public string OilSpray { get; set; }
            public string CommentForOilSpray { get; set; }
            public string MilkSpray { get; set; }
            public string CommentForMilkSpray { get; set; }
            public string ColdRoomTemp { get; set; }
            public string DeepFreezeTemp { get; set; }
            public string MaidaImageUrl { get; set; }
            public string BBImageUrl { get; set; }
            public string Approver1EmployeeCode { get; set; }
            public string Approver2EmployeeCode { get; set; }
            public string DottedLineApproverEmployeeCode { get; set; }
            public string Id {  get; set; }

            //added on 09/04/2025
            public string EmployeeName { get; set; }
            public string SubmittedByEmployeeCode { get; set; }
        }
        private void BindGridView()
        {
            //var dataSave = new List<ReportInfo>
            //{
            //     new ReportInfo {},
            //};

            //// Bind to GridView
            //GridView1.DataSource = dataSave;
            //GridView1.DataBind();

            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SP_PCR_ViewPage", con))
                {

                    cmd.CommandType = CommandType.StoredProcedure;
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        System.Data.DataTable dt = new System.Data.DataTable();
                        sda.Fill(dt);
                        GridView1.DataSource = dt;
                        GridView1.DataBind();
                    }
                }
            }
        }

        protected string BindSubmittedTime(object submittedTime)
        {
            if (submittedTime != null && submittedTime != DBNull.Value)
            {
                if (submittedTime is TimeSpan)
                {
                    TimeSpan timeValue = (TimeSpan)submittedTime;
                    DateTime baseDate = DateTime.Today;
                    DateTime fullTime = baseDate.Add(timeValue);
                    return fullTime.ToString("HH:mm") + " HRS";
                }
                else if (submittedTime is DateTime)
                {
                    DateTime dateTimeValue = Convert.ToDateTime(submittedTime);
                    return dateTimeValue.ToString("HH:mm") + " HRS";
                }
            }
            return string.Empty;
        }

        protected void FilterData()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SP_PCR_FilteredData", con))
                {

                    cmd.Parameters.AddWithValue("@Plant_Name", string.IsNullOrEmpty(DDL_Plant.SelectedValue) || DDL_Plant.SelectedValue == "0" ? (object)DBNull.Value : Convert.ToInt32(DDL_Plant.SelectedValue));
                    //cmd.Parameters.AddWithValue("@Plant_Name", string.IsNullOrEmpty(DDL_Plant.SelectedValue) ? (object)DBNull.Value : int.Parse(DDL_Plant.SelectedValue));
                    cmd.Parameters.AddWithValue("@Plant_Line", string.IsNullOrEmpty(DDL_PlantLine.SelectedValue) || DDL_PlantLine.SelectedValue == "0" ? (object)DBNull.Value : Convert.ToInt32(DDL_PlantLine.SelectedValue));
                    //cmd.Parameters.AddWithValue("@Plant_Line", string.IsNullOrEmpty(DDL_PlantLine.SelectedValue) ? (object)DBNull.Value : int.Parse(DDL_PlantLine.SelectedValue));
                    cmd.Parameters.AddWithValue("@Product_Category", string.IsNullOrEmpty(DDL_ProductCategory.SelectedValue) || DDL_ProductCategory.SelectedValue == "0" ? (object)DBNull.Value : Convert.ToInt32(DDL_ProductCategory.SelectedValue));
                    //cmd.Parameters.AddWithValue("@Product_Category", string.IsNullOrEmpty(DDL_ProductCategory.SelectedValue) ? (object)DBNull.Value : int.Parse(DDL_ProductCategory.SelectedValue));
                    cmd.Parameters.AddWithValue("@Product_Brand", string.IsNullOrEmpty(DDL_ProductBrand.SelectedValue) || DDL_ProductBrand.SelectedValue == "0" ? (object)DBNull.Value : Convert.ToInt32(DDL_ProductBrand.SelectedValue));
                    //cmd.Parameters.AddWithValue("@Product_Brand", string.IsNullOrEmpty(DDL_ProductBrand.SelectedValue) ? (object)DBNull.Value : int.Parse(DDL_ProductBrand.SelectedValue));
                    cmd.Parameters.AddWithValue("@SkuId", string.IsNullOrEmpty(DDL_BrandSKU.SelectedValue) || DDL_BrandSKU.SelectedValue == "0" ? (object)DBNull.Value : DDL_BrandSKU.SelectedValue);
                    //cmd.Parameters.AddWithValue("@SkuId", string.IsNullOrEmpty(DDL_BrandSKU.SelectedValue) ? (object)DBNull.Value : int.Parse(DDL_BrandSKU.SelectedValue));

                    cmd.CommandType = CommandType.StoredProcedure;
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        System.Data.DataTable dt = new System.Data.DataTable();
                        sda.Fill(dt);
                        GridView1.DataSource = dt;
                        GridView1.DataBind();
                    }
                }
            }
        }
        private void ShowHideGridViewColumns()
        {
            if (User.IsInRole("Admin"))
            {
                // Admins see all columns
                for (int i = 0; i < GridView1.Columns.Count; i++)
                {
                    GridView1.Columns[i].Visible = true;
                }
            }
            else
            {
                // Regular users see 
                for (int i = 0; i < GridView1.Columns.Count; i++)
                {
                    if (i < 10 || i >= GridView1.Columns.Count - 1)
                    {
                        // Show first 10 columns and last 3 columns
                        GridView1.Columns[i].Visible = true;
                    }
                    else
                    {
                        // Hide all other columns
                        GridView1.Columns[i].Visible = false;
                    }
                }
            }
        }

        //protected void ExportBtn_Click(object sender, EventArgs e)
        //{
            
        //    string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        //    using (SqlConnection con = new SqlConnection(connectionString))
        //    {
        //        using (SqlCommand cmd = new SqlCommand("SP_PCR_ViewPage", con))
        //        {
        //            cmd.CommandType = CommandType.StoredProcedure;
        //            con.Open();

        //            using (SqlDataReader reader = cmd.ExecuteReader())
        //            {
        //                if (reader.HasRows)
        //                {
        //                    string GridViewDataExportedFileInfo = "ProcessChecking_ViewPage_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + ".xls";

        //                    #region Export In Documents Folder
        //                    object misValue = System.Reflection.Missing.Value;
        //                    ExcelInterop.Application application = new ExcelInterop.Application();
        //                    application.Visible = false;

        //                    ExcelInterop.Workbook workbook = application.Workbooks.Add(misValue);
        //                    ExcelInterop.Worksheet worksheet = (ExcelInterop.Worksheet)workbook.Worksheets[1];
        //                    worksheet.Name = "ProcessChecking_ViewPage";
        //                    worksheet.Cells.Font.Size = 12;

        //                    // Add Column Headers from SqlDataReader and set background color
        //                    ExcelInterop.Range headerRange = worksheet.get_Range("A1", worksheet.Cells[1, reader.FieldCount]);
        //                    headerRange.Font.Bold = true;
        //                    headerRange.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.LightBlue); // Set header color

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

        //public void AddColumnInSheetFromReader(Worksheet worksheet, SqlDataReader reader)
        //{
        //    for (int i = 0; i < reader.FieldCount; i++)
        //    {
        //        string columnName = reader.GetName(i);
        //        worksheet.Cells[1, i + 1] = columnName; // Write column headers in the first row
        //    }
        //}



        protected void ApproveBtn_Click(object sender, EventArgs e)
        {
            // Get the ID from the CommandArgument of the button
            System.Web.UI.WebControls.Button btn = (System.Web.UI.WebControls.Button)sender;
            string pcrno = btn.CommandArgument;

            // Redirect to the Process_FinalApproval.aspx page with the PcrNo in the query string
            Response.Redirect("Process_FinalApproval.aspx?PcrNo=" + pcrno);
        }

        protected void GridView1_RowCommand(object sender, System.Web.UI.WebControls.GridViewCommandEventArgs e)
        {
            int rowIndex = Convert.ToInt32(e.CommandArgument);
            GridViewRow row = GridView1.Rows[rowIndex];

            string pcrno = (row.FindControl("lblPcrno") as Label).Text;
            string dbid = (row.FindControl("lbl_rowid") as Label).Text;
            //string app1 = (row.FindControl("lbl_Approver1") as Label).Text;
            //string app2 = (row.FindControl("lbl_Approver2") as Label).Text;
            //string dtapp = (row.FindControl("lbl_DottedLineApproverEmployeeCode") as Label).Text;

            if (e.CommandName == "ViewApprove")
            {
                Response.Redirect("Process_FinalApproval.aspx?PcrNo="+pcrno +"&ID="+dbid+"&VM=1", false);

            }
        }

    }
}