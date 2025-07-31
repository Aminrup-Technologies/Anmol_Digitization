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
using ClosedXML.Excel;
using System.Text;
using Newtonsoft.Json;

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
                    //ShowHideGridViewColumns();

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
                lbl_DDL_Plant_Value.Text = selectedPlantValue;
                PlantLinesBinder(selectedPlantValue);
                //FilterData();
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
                lbl_DDL_PlantLine_Value.Text = selectedPlantLineValue;
                LineProductsBinder(selectedPlantValue, selectedPlantLineValue);
                //FilterData();
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
                lbl_DDL_ProductCategory_Value.Text = selectedProductCategoryValue;
                ProductBrandsBinder(selectedPlantValue, selectedPlantLineValue, selectedProductCategoryValue);
                //FilterData();
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
                lbl_DDL_ProductBrand_Value.Text = selectedProductBrandValue;
                BrandSKUBinder(selectedProductBrandValue);
                //FilterData();                
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
            //FilterData();
        }

        protected void DDL_BrandSKU_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Plant.SelectedIndex != 0)
            {
                if (DDL_PlantLine.SelectedIndex != 0)
                {
                    if (DDL_ProductCategory.SelectedIndex != 0)
                    {
                        if (DDL_ProductBrand.SelectedIndex != 0)
                        {
                            if (DDL_BrandSKU.SelectedIndex != 0)
                            {
                                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                                string selectedPlantLineValue = DDL_PlantLine.SelectedValue.ToString();
                                string selectedProductCategoryValue = DDL_ProductCategory.SelectedValue.ToString();
                                string selectedProductBrandValue = DDL_ProductBrand.SelectedValue.ToString();
                                string selectedBrandSKU = DDL_BrandSKU.SelectedValue.ToString();

                                FilterData();
                            }
                        }

                    }
                }
            }
        }

        protected void ReportbtnCancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("qaqc_qaapprovals.aspx");
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

                    cmd.Parameters.AddWithValue("@Plant_Name", string.IsNullOrEmpty(DDL_Plant.SelectedValue) || DDL_Plant.SelectedValue == "0" ? (object)DBNull.Value : Convert.ToInt32(DDL_Plant.SelectedValue));
                    cmd.Parameters.AddWithValue("@Plant_Line", string.IsNullOrEmpty(DDL_PlantLine.SelectedValue) || DDL_PlantLine.SelectedValue == "0" ? (object)DBNull.Value : Convert.ToInt32(DDL_PlantLine.SelectedValue));
                    cmd.Parameters.AddWithValue("@Product_Category", string.IsNullOrEmpty(DDL_ProductCategory.SelectedValue) || DDL_ProductCategory.SelectedValue == "0" ? (object)DBNull.Value : Convert.ToInt32(DDL_ProductCategory.SelectedValue));
                    cmd.Parameters.AddWithValue("@Product_Brand", string.IsNullOrEmpty(DDL_ProductBrand.SelectedValue) || DDL_ProductBrand.SelectedValue == "0" ? (object)DBNull.Value : Convert.ToInt32(DDL_ProductBrand.SelectedValue));
                    cmd.Parameters.AddWithValue("@SkuId", string.IsNullOrEmpty(DDL_BrandSKU.SelectedValue) || DDL_BrandSKU.SelectedValue == "0" ? (object)DBNull.Value : DDL_BrandSKU.SelectedValue);

                    cmd.CommandType = CommandType.StoredProcedure;
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        System.Data.DataTable dt = new System.Data.DataTable();
                        sda.Fill(dt);
                        //if (dt.Rows.Count > 0)
                        //{
                            ViewState["ExportData"] = dt;
                            GridView1.DataSource = dt;
                            GridView1.DataBind();
                        //}
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
            public string Id { get; set; }

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
                        ViewState["ExportData"] = dt;
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
                        ViewState["ExportData"] = dt;
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
                Response.Redirect("Process_FinalApproval.aspx?PcrNo=" + pcrno + "&ID=" + dbid + "&VM=1", false);

            }
        }

        protected void ExportBtn_Click(object sender, EventArgs e)
        {
            if (ViewState["ExportData"] != null)
            {
                DataTable dt = ViewState["ExportData"] as DataTable;

                if (dt != null && dt.Rows.Count > 0)
                {
                    using (XLWorkbook wb = new XLWorkbook())
                    {
                        wb.Worksheets.Add(dt, "QCProcessCheckingReport");

                        Response.Clear();
                        Response.Buffer = true;
                        Response.Charset = "";
                        Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        Response.AddHeader("content-disposition", "attachment;filename=QCProcessCheckingRecords.xlsx");

                        using (MemoryStream MyMemoryStream = new MemoryStream())
                        {
                            wb.SaveAs(MyMemoryStream);
                            MyMemoryStream.WriteTo(Response.OutputStream);
                            Response.Flush();
                            Response.End();
                        }
                    }
                }
                else
                {
                    // Handle the case where the DataTable is empty
                    Response.Write("No data available to export.");
                }
            }
            else
            {
                // Handle the case where ViewState["ExportData"] is null
                Response.Write("No data available to export.");
            }

        }

        protected void btn_devrpt_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TB_Date_From.Text) &&
                !string.IsNullOrWhiteSpace(TB_Date_To.Text) &&
                DDL_Plant.SelectedValue != "-1" &&
                DDL_PlantLine.SelectedValue != "-1" &&
                DDL_ProductCategory.SelectedValue != "-1" &&
                DDL_ProductBrand.SelectedValue != "-1")
            {
                DateTime startDate = Convert.ToDateTime(TB_Date_From.Text.Trim());
                DateTime endDate = Convert.ToDateTime(TB_Date_To.Text.Trim());

                int plantId = Convert.ToInt32(DDL_Plant.SelectedValue);
                int lineId = Convert.ToInt32(DDL_PlantLine.SelectedValue);
                int categoryId = Convert.ToInt32(DDL_ProductCategory.SelectedValue);
                int brandId = Convert.ToInt32(DDL_ProductBrand.SelectedValue);

                DataTable dt = GetWeeklyReportData(plantId, lineId, categoryId, brandId, startDate, endDate);

                if (dt != null && dt.Rows.Count > 0)
                {
                    List<string> dates = dt.AsEnumerable()
                        .Select(r => Convert.ToDateTime(r["SubmittedDate"]).ToString("dd-MM-yyyy"))
                        .Distinct()
                        .ToList();

                    // 1. Manual parameter-to-field_name mapping
                    Dictionary<string, string> parameterFieldMap = new Dictionary<string, string>
                    {
                        // BasicData
                        { "BS_ColdRoomTemp", "ColdRoomTemp" },
                        { "BS_DeepFreezeTemp", "DeepFreezeTemp" },
                        { "BS_HvoTemp", "HvoTemp" },
                        { "BS_WaterPH", "WaterPH" },
                        { "BS_ProcessWaterTemp", "ProcessWaterTemp" },
                        { "BS_SyrupTemp", "SyrupTemp" },
                        { "BS_SyrupPH", "SyrupPH" },
                        { "BS_TDS", "TDS" },
                        { "BS_WaterHardness", "WaterHardness" },

                        // DoughData
                        { "Dough_DoughTemp", "DoughTemp" },
                        { "Dough_DoughRestTime", "DoughRestTime" },
                        { "Dough_BakingTime", "DoughBakingTime" },
                        { "Dough_CreamingTime", "DoughCreamingTime" },
                        { "Dough_MixingTime", "DoughMixingTime" },
                        { "Dough_DiceRpm", "DiceRpm" },

                        // SpongeData
                        { "Sponge_Temp", "SP_Temp" },
                        { "Sponge_StandingTime", "SP_StandingTime" },
                        { "Sponge_RoomTemp", "SP_RoomTemp" },

                        // OvenData (assuming names are zone-based, map directly)
                        { "Oven_Zone 1_Top", "oven_top" },
                        { "Oven_Zone 1_Bottom", "oven_bottom" },
                        { "Oven_Zone 2_Top", "oven_top" },
                        { "Oven_Zone 2_Bottom", "oven_bottom" },
                        { "Oven_Zone 3_Top", "oven_top" },
                        { "Oven_Zone 3_Bottom", "oven_bottom" },
                        { "Oven_Zone 4_Top", "oven_top" },
                        { "Oven_Zone 4_Bottom", "oven_bottom" },
                        { "Oven_Zone 5_Top", "oven_top" },
                        { "Oven_Zone 5_Bottom", "oven_bottom" },

                        // The rest can be left unmapped or handled as display-only without standards:
                        // These include RM_*, BS_*, and Dough_* that are qualitative, like OK/Not OK
                        // You can check for null mappings before trying to fetch from DB
                    };


                    // 2. Fetch standards from DB
                    Dictionary<string, string> standardMap = new Dictionary<string, string>();
                    using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
                    {
                        con.Open();
                        string query = @"SELECT field_name, RV_MinValue, RV_MaxValue
                                 FROM MST_FormFieldControls
                                 WHERE FormID = 6 AND RecordType = 'Specific' AND ControlType = 'textbox'
                                 AND RV_Yesno = 1 AND ViewMode = 1 AND IsLatest = 1 AND IsDeleted = 0
                                 AND plant_id = @plantId AND LineId = @lineId AND category_id = @categoryId AND material_id = @brandId";

                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@plantId", plantId);
                            cmd.Parameters.AddWithValue("@lineId", lineId);
                            cmd.Parameters.AddWithValue("@categoryId", categoryId);
                            cmd.Parameters.AddWithValue("@brandId", brandId);

                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string fieldName = reader["field_name"].ToString();
                                    string min = reader["RV_MinValue"].ToString();
                                    string max = reader["RV_MaxValue"].ToString();
                                    standardMap[fieldName] = $"{min} ~ {max}";
                                }
                            }
                        }
                    }

                    // 3. Pivot the data
                    DataTable pivot = BuildPivotTable(dt, dates, standardMap);

                    // 4. Inject Standard column before UOM
                    if (!pivot.Columns.Contains("Standard (Min~Max)"))
                    {
                        pivot.Columns.Add("Standard (Min~Max)", typeof(string));
                        int uomIndex = pivot.Columns["UOM"].Ordinal;
                        pivot.Columns["Standard (Min~Max)"].SetOrdinal(uomIndex); // move before UOM
                    }

                    foreach (DataRow row in pivot.Rows)
                    {
                        string parameter = row["Parameter"].ToString();
                        string fieldName = "";
                        if (parameterFieldMap.TryGetValue(parameter, out fieldName))
                        {
                            string stdRange = "";
                            if (standardMap.TryGetValue(fieldName, out stdRange))
                                row["Standard (Min~Max)"] = stdRange;
                            else
                                row["Standard (Min~Max)"] = "";
                        }
                        else
                        {
                            row["Standard (Min~Max)"] = "";
                        }
                    }

                    // 5. Export the final pivoted report
                    ExportToExcelNew(pivot, "ProcessChecking_SummaryReport", plantId, lineId, categoryId, brandId, startDate, endDate);
                }
                else
                {
                    // Optional: Alert or feedback
                }
            }
        }


        private void ExportToExcelNew(DataTable pivotTable, string fileName, int plantId, int lineId, int categoryId, int brandId, DateTime fromDate, DateTime toDate)
        {
            using (XLWorkbook wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Process Checking Report");

                int row = 1;

                // Title
                ws.Cell(row, 1).Value = "Process Checking Summary Report";
                ws.Range(row, 1, row, 6).Merge().Style
                    .Font.SetBold().Font.FontSize = 16;
                row++;

                // Timestamp
                ws.Cell(row, 1).Value = "Exported on: " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
                ws.Range(row, 1, row, 4).Merge();
                ws.Range(row, 1, row, 4).Style
                    .Font.SetItalic()
                    .Font.SetFontSize(10)
                    .Font.SetFontColor(XLColor.DarkGray);
                row += 2;

                // Filters
                ws.Cell(row++, 1).Value = "Plant:";
                ws.Cell(row - 1, 2).Value = DDL_Plant.SelectedItem.Text;

                ws.Cell(row++, 1).Value = "Line:";
                ws.Cell(row - 1, 2).Value = DDL_PlantLine.SelectedItem.Text;

                ws.Cell(row++, 1).Value = "Category:";
                ws.Cell(row - 1, 2).Value = DDL_ProductCategory.SelectedItem.Text;

                ws.Cell(row++, 1).Value = "Brand:";
                ws.Cell(row - 1, 2).Value = DDL_ProductBrand.SelectedItem.Text;

                ws.Cell(row++, 1).Value = "From Date:";
                ws.Cell(row - 1, 2).Value = fromDate.ToString("dd-MM-yyyy");

                ws.Cell(row++, 1).Value = "To Date:";
                ws.Cell(row - 1, 2).Value = toDate.ToString("dd-MM-yyyy");

                row++; // leave empty row

                // Insert pivot table
                ws.Cell(row, 1).InsertTable(pivotTable);

                // Freeze pane (below header, right of section/param/standard/uom)
                ws.SheetView.FreezeRows(11); // freeze up to data headers
                ws.SheetView.FreezeColumns(4); // fix A-D columns

                // Auto-fit
                ws.Columns().AdjustToContents();

                // Download
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    byte[] content = stream.ToArray();

                    HttpContext.Current.Response.Clear();
                    HttpContext.Current.Response.Buffer = true;
                    HttpContext.Current.Response.AddHeader("content-disposition", $"attachment;filename={fileName}.xlsx");
                    HttpContext.Current.Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    HttpContext.Current.Response.BinaryWrite(content);
                    HttpContext.Current.Response.End();
                }
            }
        }

        private void ExportToExcel_OLD(DataTable dt, string fileName)
        {
            HttpContext.Current.Response.Clear();
            HttpContext.Current.Response.Buffer = true;
            HttpContext.Current.Response.AddHeader("content-disposition", "attachment;filename=" + fileName);
            HttpContext.Current.Response.Charset = "";
            HttpContext.Current.Response.ContentType = "application/vnd.ms-excel";

            using (StringWriter sw = new StringWriter())
            {
                HtmlTextWriter hw = new HtmlTextWriter(sw);
                GridView gv = new GridView();
                gv.DataSource = dt;
                gv.DataBind();
                gv.RenderControl(hw);
                HttpContext.Current.Response.Output.Write(sw.ToString());
                HttpContext.Current.Response.Flush();
                HttpContext.Current.Response.End();
            }
        }

        private void ExportToExcel_Basic(DataTable pivotTable, string fileName, int plantId, int lineId, int categoryId, int brandId, DateTime fromDate, DateTime toDate)
        {
            using (XLWorkbook wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Process Checking Report");

                int row = 1;

                // Title
                ws.Cell(row, 1).Value = "Process Checking Summarized Report";
                ws.Range(row, 1, row, 4).Merge().Style
                    .Font.SetBold()
                    .Font.FontSize = 16;
                row++;

                // Timestamp
                ws.Cell(row, 1).Value = "Exported on: " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
                ws.Range(row, 1, row, 4).Merge();
                ws.Range(row, 1, row, 4).Style
                    .Font.SetItalic()
                    .Font.SetFontSize(10)
                    .Font.SetFontColor(XLColor.DarkGray);

                row += 2;

                // Filter metadata
                ws.Cell(row++, 1).Value = "Plant:";
                ws.Cell(row - 1, 2).Value = DDL_Plant.SelectedItem.Text;

                ws.Cell(row++, 1).Value = "Line:";
                ws.Cell(row - 1, 2).Value = DDL_PlantLine.SelectedItem.Text;

                ws.Cell(row++, 1).Value = "Category:";
                ws.Cell(row - 1, 2).Value = DDL_ProductCategory.SelectedItem.Text;

                ws.Cell(row++, 1).Value = "Brand:";
                ws.Cell(row - 1, 2).Value = DDL_ProductBrand.SelectedItem.Text;

                ws.Cell(row++, 1).Value = "From Date:";
                ws.Cell(row - 1, 2).Value = fromDate.ToString("dd-MM-yyyy");

                ws.Cell(row++, 1).Value = "To Date:";
                ws.Cell(row - 1, 2).Value = toDate.ToString("dd-MM-yyyy");

                row++; // empty row

                // Insert pivot table
                var tableStartRow = row;
                var table = ws.Cell(row, 1).InsertTable(pivotTable, "PivotReport", true);
                var tableEndRow = tableStartRow + pivotTable.Rows.Count;
                //var tableEndRow = tableStartRow;

                // Freeze top rows and left columns
                ws.SheetView.Freeze(tableStartRow, 3);

                // Alternate section color
                string currentSection = "";
                bool shade = false;

                for (int r = tableStartRow + 1; r <= tableEndRow; r++)
                {
                    string sectionValue = ws.Cell(r, 1).GetString();
                    if (!string.Equals(sectionValue, currentSection, StringComparison.OrdinalIgnoreCase))
                    {
                        shade = !shade;
                        currentSection = sectionValue;
                    }

                    if (shade)
                    {
                        ws.Range(r, 1, r, ws.LastColumnUsed().ColumnNumber())
                          .Style.Fill.BackgroundColor = XLColor.LightGray;
                    }
                }

                // Bold headers
                ws.Row(tableStartRow).Style.Font.SetBold();

                // Fit columns
                ws.Columns().AdjustToContents();

                // Export to browser
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    byte[] content = stream.ToArray();

                    HttpContext.Current.Response.Clear();
                    HttpContext.Current.Response.Buffer = true;
                    HttpContext.Current.Response.AddHeader("content-disposition", $"attachment;filename={fileName}.xlsx");
                    HttpContext.Current.Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    HttpContext.Current.Response.BinaryWrite(content);
                    HttpContext.Current.Response.Flush();
                    HttpContext.Current.Response.End();
                }
            }
        }

        private void ExportToExcel_Basic0(DataTable pivotTable, string fileName, int plantId, int lineId, int categoryId, int brandId, DateTime fromDate, DateTime toDate)
        {
            using (XLWorkbook wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Process Checking Report");

                int row = 1;

                // Add filter metadata at the top
                ws.Cell(row++, 1).Value = "Plant:";
                ws.Cell(row - 1, 2).Value = DDL_Plant.SelectedItem.Text;

                ws.Cell(row++, 1).Value = "Line:";
                ws.Cell(row - 1, 2).Value = DDL_PlantLine.SelectedItem.Text;

                ws.Cell(row++, 1).Value = "Category:";
                ws.Cell(row - 1, 2).Value = DDL_ProductCategory.SelectedItem.Text;

                ws.Cell(row++, 1).Value = "Brand:";
                ws.Cell(row - 1, 2).Value = DDL_ProductBrand.SelectedItem.Text;

                ws.Cell(row++, 1).Value = "From Date:";
                ws.Cell(row - 1, 2).Value = fromDate.ToString("dd-MM-yyyy");

                ws.Cell(row++, 1).Value = "To Date:";
                ws.Cell(row - 1, 2).Value = toDate.ToString("dd-MM-yyyy");

                row++; // leave an empty row

                // Insert the pivot table (only once)
                ws.Cell(row, 1).InsertTable(pivotTable);

                // Freeze top filter rows (up to filter row 8) and first 3 columns (A, B, C)
                int freezeRow = row; // first row of actual data
                ws.SheetView.Freeze(freezeRow, 3); // freeze rows 1–(row) and columns A–C

                // Auto-fit columns
                ws.Columns().AdjustToContents();

                // Prepare file for download
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    byte[] content = stream.ToArray();

                    HttpContext.Current.Response.Clear();
                    HttpContext.Current.Response.Buffer = true;
                    HttpContext.Current.Response.AddHeader("content-disposition", $"attachment;filename={fileName}.xlsx");
                    HttpContext.Current.Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    HttpContext.Current.Response.BinaryWrite(content);
                    HttpContext.Current.Response.Flush(); // optional but safe
                    HttpContext.Current.Response.End();
                }
            }
        }

        private DataTable GetWeeklyReportData(int plant, int line, int category, int brand, DateTime fromDate, DateTime toDate)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["dbConn"].ConnectionString))
            using (SqlCommand cmd = new SqlCommand("usp_GetProcessCheckingPivotReport", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PlantName", plant);
                cmd.Parameters.AddWithValue("@Line", line);
                cmd.Parameters.AddWithValue("@Category", category);
                cmd.Parameters.AddWithValue("@Brand", brand);
                cmd.Parameters.AddWithValue("@StartDate", fromDate);
                cmd.Parameters.AddWithValue("@EndDate", toDate);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private DataTable BuildPivotTable_Basic(DataTable source, List<string> dates)
        {
            DataTable pivot = new DataTable();
            pivot.Columns.Add("Section");
            pivot.Columns.Add("Parameter");
            pivot.Columns.Add("UOM");

            // Add shift-wise columns for each date
            foreach (var date in dates)
            {
                pivot.Columns.Add($"{date}_A");
                pivot.Columns.Add($"{date}_B");
                pivot.Columns.Add($"{date}_C");
            }

            // Group rows by Section + Parameter + UOM
            var grouped = source.AsEnumerable()
                .GroupBy(r => new
                {
                    Section = r.Field<string>("Parameter").Split('_')[0],
                    Parameter = r.Field<string>("Parameter"),
                    UOM = r.Field<string>("UOM")
                });

            foreach (var group in grouped)
            {
                var row = pivot.NewRow();
                row["Section"] = group.Key.Section;
                row["Parameter"] = group.Key.Parameter;
                row["UOM"] = group.Key.UOM;

                foreach (var record in group)
                {
                    string date = Convert.ToDateTime(record["SubmittedDate"]).ToString("dd-MM-yyyy");

                    // Safely assign each shift's value if column exists
                    if (source.Columns.Contains("Shift_A"))
                        row[$"{date}_A"] = record["Shift_A"];
                    if (source.Columns.Contains("Shift_B"))
                        row[$"{date}_B"] = record["Shift_B"];
                    if (source.Columns.Contains("Shift_C"))
                        row[$"{date}_C"] = record["Shift_C"];
                }

                pivot.Rows.Add(row);
            }

            return pivot;
        }

        private DataTable BuildPivotTable(DataTable source, List<string> dates, Dictionary<string, string> standardMap)
        {
            DataTable pivot = new DataTable();
            pivot.Columns.Add("Section");
            pivot.Columns.Add("Parameter");
            pivot.Columns.Add("Standard (Min~Max)"); // Single Standard column
            pivot.Columns.Add("UOM");

            // Add columns per date-shift (e.g., 30-07-2025_A)
            foreach (var date in dates)
            {
                foreach (string shift in new[] { "A", "B", "C" })
                {
                    pivot.Columns.Add($"{date}_{shift}");
                }
            }

            var grouped = source.AsEnumerable()
                .GroupBy(r => new
                {
                    Section = r.Field<string>("Parameter").Split('_')[0],
                    Parameter = r.Field<string>("Parameter"),
                    UOM = r.Field<string>("UOM")
                });

            foreach (var group in grouped)
            {
                var row = pivot.NewRow();
                row["Section"] = group.Key.Section;
                row["Parameter"] = group.Key.Parameter;
                row["UOM"] = group.Key.UOM;

                string fieldName = group.Key.Parameter;
                row["Standard (Min~Max)"] = standardMap.ContainsKey(fieldName)
                                           ? standardMap[fieldName]
                                           : "";

                foreach (var record in group)
                {
                    string date = Convert.ToDateTime(record["SubmittedDate"]).ToString("dd-MM-yyyy");

                    if (record.Table.Columns.Contains("Shift_A"))
                        row[$"{date}_A"] = record["Shift_A"];
                    if (record.Table.Columns.Contains("Shift_B"))
                        row[$"{date}_B"] = record["Shift_B"];
                    if (record.Table.Columns.Contains("Shift_C"))
                        row[$"{date}_C"] = record["Shift_C"];
                }

                pivot.Rows.Add(row);
            }

            return pivot;
        }

        public override void VerifyRenderingInServerForm(Control control)
        {
            // Required for exporting GridView without rendering
        }


        // Model for report rows
        public class ReportRow1
        {
            public string Name { get; set; }
            public string UOM { get; set; }
            public string Standard { get; set; }
            public string A { get; set; }
            public string B { get; set; }
            public string C { get; set; }
            public string ADiff { get; set; }  // Optional
            public string BDiff { get; set; }  // Optional
            public string CDiff { get; set; }  // Optional
        }

        //private void LoadReport(int plantId, int lineId, int categoryId, int brandId, DateTime startDate, DateTime endDate, bool exportToExcel = false)
        //{
        //    StringBuilder html = new StringBuilder();

        //    html.Append("<h3 style='text-align:center;'>Production Process Report</h3>");

        //    using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
        //    {
        //        con.Open();

        //        List<Tuple<DateTime, string>> dateShiftPairs = GetDateShiftPairs(con, plantId, lineId, categoryId, brandId, startDate, endDate);

        //        foreach (Tuple<DateTime, string> pair in dateShiftPairs)
        //        {
        //            DateTime reportDate = pair.Item1;
        //            string shift = pair.Item2;

        //            html.AppendFormat("<h4 style='margin-top:30px;'>Date: {0:dd-MM-yyyy} &nbsp;&nbsp;&nbsp;&nbsp; Shift: {1}</h4>", reportDate, shift);

        //            html.Append(GetRawMaterialWeights(con, plantId, lineId, categoryId, brandId, reportDate, shift));
        //            html.Append(GetDoughData(con, plantId, lineId, categoryId, brandId, reportDate, shift));
        //            html.Append(GetSpongeData(con, plantId, lineId, categoryId, brandId, reportDate, shift));
        //            html.Append(GetOvenZoneTemperatures(con, plantId, lineId, categoryId, brandId, reportDate, shift));
        //        }
        //    }

        //    if (exportToExcel)
        //    {
        //        HttpContext.Current.Response.Clear();
        //        HttpContext.Current.Response.Buffer = true;
        //        HttpContext.Current.Response.AddHeader("content-disposition", "attachment;filename=ProductionReport_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xls");
        //        HttpContext.Current.Response.Charset = "";
        //        HttpContext.Current.Response.ContentType = "application/vnd.ms-excel";
        //        HttpContext.Current.Response.Write("<html><head><meta http-equiv='Content-Type' content='text/html; charset=utf-8'></head><body>");
        //        HttpContext.Current.Response.Write(html.ToString());
        //        HttpContext.Current.Response.Write("</body></html>");
        //        HttpContext.Current.Response.Flush();
        //        HttpContext.Current.Response.End();
        //    }
        //    else
        //    {
        //        litReport.Text = html.ToString();
        //    }
        //}

        //private void LoadReport(int plantId, int lineId, int categoryId, int brandId, DateTime startDate, DateTime endDate, bool exportToExcel = false)
        //{
        //    StringBuilder html = new StringBuilder();

        //    html.Append("<h3 style='text-align:center;'>Production Process Report</h3>");

        //    using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
        //    {
        //        con.Open();

        //        // Loop through each date
        //        for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
        //        {
        //            html.Append($"<h4 style='text-align:center;'>Date: {date:dd-MM-yyyy}</h4>");

        //            // Append each section with date filtering
        //            html.Append(GetRawMaterialWeights(con, plantId, lineId, categoryId, brandId, date));
        //            html.Append(GetDoughData(con, plantId, lineId, categoryId, brandId, date));
        //            html.Append(GetSpongeData(con, plantId, lineId, categoryId, brandId, date));
        //            html.Append(GetOvenZoneTemperatures(con, plantId, lineId, categoryId, brandId, date));
        //        }
        //    }

        //    if (exportToExcel)
        //    {
        //        HttpContext.Current.Response.Clear();
        //        HttpContext.Current.Response.Buffer = true;
        //        HttpContext.Current.Response.AddHeader("content-disposition", "attachment;filename=ProductionReport_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xls");
        //        HttpContext.Current.Response.Charset = "";
        //        HttpContext.Current.Response.ContentType = "application/vnd.ms-excel";
        //        HttpContext.Current.Response.Write("<html><head><meta http-equiv='Content-Type' content='text/html; charset=utf-8'></head><body>");
        //        HttpContext.Current.Response.Write(html.ToString());
        //        HttpContext.Current.Response.Write("</body></html>");
        //        HttpContext.Current.Response.Flush();
        //        HttpContext.Current.Response.End();
        //    }
        //    else
        //    {
        //        litReport.Text = html.ToString();
        //    }
        //}
        private string GetSpongeData1(SqlConnection con, int plantId, int lineId, int categoryId, int brandId, DateTime startDate, DateTime endDate)
        {
            StringBuilder html = new StringBuilder();
            string query = @"
    SELECT 
        CAST(basicData.Date AS DATE) AS ReportDate,
        basicData.Shift,
        sponge.Parameter,
        sponge.UOM,
        sponge.Value,
        ISNULL(ctrl.RV_MinValue, 0) AS MinVal,
        ISNULL(ctrl.RV_MaxValue, 0) AS MaxVal
    FROM TRN_ProcessChecking_BasicData basicData
    JOIN PCR_SpongeData sponge ON sponge.PcrNo = basicData.PcrNo
    LEFT JOIN MST_FormFieldControls ctrl ON 
        ctrl.FieldName = sponge.Parameter AND
        ctrl.Sub_Form = 'SpongeData' AND
        ctrl.FormID = 6 AND
        ctrl.RecordType = 'Specific' AND
        ctrl.ViewMode = 1 AND
        ctrl.IsLatest = 1 AND
        ctrl.IsDeleted = 0 AND
        ctrl.plant_id = @plantId AND
        ctrl.LineId = @lineId AND
        ctrl.category_id = @categoryId AND
        ctrl.brand_id = @brandId
    WHERE 
        basicData.PlantID = @plantId AND
        basicData.LineID = @lineId AND
        basicData.ProductCategory = @categoryId AND
        basicData.ProductBrand = @brandId AND
        CAST(basicData.Date AS DATE) BETWEEN @startDate AND @endDate
    ORDER BY ReportDate, Shift, sponge.Parameter;
    ";

            SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@plantId", plantId);
            cmd.Parameters.AddWithValue("@lineId", lineId);
            cmd.Parameters.AddWithValue("@categoryId", categoryId);
            cmd.Parameters.AddWithValue("@brandId", brandId);
            cmd.Parameters.AddWithValue("@startDate", startDate);
            cmd.Parameters.AddWithValue("@endDate", endDate);

            SqlDataReader rdr = cmd.ExecuteReader();
            string currentDateShift = "";
            int sl = 1;

            while (rdr.Read())
            {
                string dateShift = $"{Convert.ToDateTime(rdr["ReportDate"]):dd-MM-yyyy} | Shift {rdr["Shift"]}";
                if (currentDateShift != dateShift)
                {
                    if (currentDateShift != "")
                        html.Append("</table><br/>");

                    html.Append($"<h4 style='text-align:left;'>Sponge Profile - {dateShift}</h4>");
                    html.Append("<table border='1' cellspacing='0' cellpadding='5' style='width:100%; border-collapse:collapse;'>");
                    html.Append("<tr style='background-color:#dff0d8;font-weight:bold;'><th>SL</th><th>Parameter</th><th>UOM</th><th>STD</th><th>Actual</th></tr>");
                    currentDateShift = dateShift;
                    sl = 1;
                }

                string std = $"{rdr["MinVal"]} - {rdr["MaxVal"]}";

                html.AppendFormat("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td></tr>",
                    sl++,
                    rdr["Parameter"],
                    rdr["UOM"],
                    std,
                    rdr["Value"]);
            }

            if (currentDateShift != "")
                html.Append("</table><br/>");

            rdr.Close();
            return html.ToString();
        }
        private string GetSpongeData5(SqlConnection con, int plantId, int lineId, int categoryId, int brandId, DateTime startDate, DateTime endDate)
        {
            StringBuilder html = new StringBuilder();
            string query = @"
    SELECT 
        CAST(basicData.Date AS DATE) AS ReportDate,
        basicData.Shift,
        sponge.Parameter,
        sponge.UOM,
        sponge.Value,
        ISNULL(ctrl.RV_MinValue, 0) AS MinVal,
        ISNULL(ctrl.RV_MaxValue, 0) AS MaxVal
    FROM TRN_ProcessChecking_BasicData basicData
    JOIN PCR_SpongeData sponge ON sponge.PcrNo = basicData.PcrNo
    LEFT JOIN MST_FormFieldControls ctrl ON 
        ctrl.FieldName = sponge.Parameter AND
        ctrl.Sub_Form = 'SpongeData' AND
        ctrl.FormID = 6 AND
        ctrl.RecordType = 'Specific' AND
        ctrl.ViewMode = 1 AND
        ctrl.IsLatest = 1 AND
        ctrl.IsDeleted = 0 AND
        ctrl.plant_id = @plantId AND
        ctrl.LineId = @lineId AND
        ctrl.category_id = @categoryId AND
        ctrl.brand_id = @brandId
    WHERE 
        basicData.PlantID = @plantId AND
        basicData.LineID = @lineId AND
        basicData.ProductCategory = @categoryId AND
        basicData.ProductBrand = @brandId AND
        CAST(basicData.Date AS DATE) BETWEEN @startDate AND @endDate
    ORDER BY ReportDate, Shift, sponge.Parameter;
    ";

            SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@plantId", plantId);
            cmd.Parameters.AddWithValue("@lineId", lineId);
            cmd.Parameters.AddWithValue("@categoryId", categoryId);
            cmd.Parameters.AddWithValue("@brandId", brandId);
            cmd.Parameters.AddWithValue("@startDate", startDate);
            cmd.Parameters.AddWithValue("@endDate", endDate);

            SqlDataReader rdr = cmd.ExecuteReader();
            string currentDateShift = "";
            int sl = 1;

            while (rdr.Read())
            {
                string dateShift = $"{Convert.ToDateTime(rdr["ReportDate"]):dd-MM-yyyy} | Shift {rdr["Shift"]}";
                if (currentDateShift != dateShift)
                {
                    if (currentDateShift != "")
                        html.Append("</table><br/>");

                    html.Append($"<h4 style='text-align:left;'>Sponge Profile - {dateShift}</h4>");
                    html.Append("<table border='1' cellspacing='0' cellpadding='5' style='width:100%; border-collapse:collapse;'>");
                    html.Append("<tr style='background-color:#dff0d8;font-weight:bold;'><th>SL</th><th>Parameter</th><th>UOM</th><th>STD</th><th>Actual</th></tr>");
                    currentDateShift = dateShift;
                    sl = 1;
                }

                string std = $"{rdr["MinVal"]} - {rdr["MaxVal"]}";

                html.AppendFormat("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td></tr>",
                    sl++,
                    rdr["Parameter"],
                    rdr["UOM"],
                    std,
                    rdr["Value"]);
            }

            if (currentDateShift != "")
                html.Append("</table><br/>");

            rdr.Close();
            return html.ToString();
        }
        private string GetRawMaterialWeights225(SqlConnection con, int plantId, int lineId, int categoryId, int brandId, DateTime startDate, DateTime endDate)
        {
            StringBuilder html = new StringBuilder();
            string query = @"
    SELECT 
        CAST(basicData.Date AS DATE) AS ReportDate,
        basicData.Shift,
        rmWeights.material_name,
        rmWeights.UOM,
        ISNULL(standard.RV_MinValue, 0) AS Standard,
        ISNULL(rmWeights.ActualWeight, 0) AS ActualWeight
    FROM TRN_ProcessChecking_BasicData basicData
    CROSS APPLY OPENJSON(basicData.RM_Weights)
    WITH (
        material_id INT,
        material_name NVARCHAR(100),
        UOM NVARCHAR(20),
        ActualWeight FLOAT
    ) rmWeights
    LEFT JOIN MST_FormFieldControls standard ON 
        standard.material_id = rmWeights.material_id AND
        standard.Sub_Form = 'RawMaterial' AND
        standard.FormID = 6 AND
        standard.RecordType = 'Specific' AND
        standard.ViewMode = 1 AND
        standard.IsLatest = 1 AND
        standard.IsDeleted = 0 AND
        standard.plant_id = @plantId AND
        standard.LineId = @lineId AND
        standard.category_id = @categoryId AND
        standard.brand_id = @brandId
    WHERE 
        basicData.PlantID = @plantId AND
        basicData.LineID = @lineId AND
        basicData.ProductCategory = @categoryId AND
        basicData.ProductBrand = @brandId AND
        CAST(basicData.Date AS DATE) BETWEEN @startDate AND @endDate
    ORDER BY ReportDate, Shift, rmWeights.material_name;
    ";

            SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@plantId", plantId);
            cmd.Parameters.AddWithValue("@lineId", lineId);
            cmd.Parameters.AddWithValue("@categoryId", categoryId);
            cmd.Parameters.AddWithValue("@brandId", brandId);
            cmd.Parameters.AddWithValue("@startDate", startDate);
            cmd.Parameters.AddWithValue("@endDate", endDate);

            if (con.State != ConnectionState.Open)
                con.Open();

            SqlDataReader rdr = cmd.ExecuteReader();
            string currentDateShift = "";
            int sl = 1;

            while (rdr.Read())
            {
                string dateShift = $"{Convert.ToDateTime(rdr["ReportDate"]):dd-MM-yyyy} | Shift {rdr["Shift"]}";
                if (currentDateShift != dateShift)
                {
                    if (currentDateShift != "")
                        html.Append("</table><br/>");

                    html.Append($"<h4 style='text-align:left;'>Raw Materials - {dateShift}</h4>");
                    html.Append("<table border='1' cellspacing='0' cellpadding='5' style='width:100%; border-collapse:collapse;'>");
                    html.Append("<tr style='background-color:#d9edf7;font-weight:bold;'><th>SL</th><th>Raw Material</th><th>UOM</th><th>STD</th><th>Actual</th><th>Diff.</th></tr>");
                    currentDateShift = dateShift;
                    sl = 1;
                }

                double std = Convert.ToDouble(rdr["Standard"]);
                double actual = Convert.ToDouble(rdr["ActualWeight"]);
                double diff = actual - std;

                html.AppendFormat("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td></tr>",
                    sl++,
                    rdr["material_name"],
                    rdr["UOM"],
                    std,
                    actual,
                    diff);
            }

            if (currentDateShift != "")
                html.Append("</table><br/>");

            rdr.Close();
            return html.ToString();
        }
        private List<Tuple<DateTime, string>> GetDateShiftPairs(SqlConnection con, int plantId, int lineId, int categoryId, int brandId, DateTime startDate, DateTime endDate)
        {
            List<Tuple<DateTime, string>> dateShifts = new List<Tuple<DateTime, string>>();

            string query = @"
        SELECT DISTINCT CAST(SubmittedDate AS DATE) AS ReportDate, Shift 
        FROM TRN_ProcessChecking_BasicData 
        WHERE PlantName = @PlantId AND Line = @LineId 
              AND ProductCategory = @CategoryId AND ProductBrand = @BrandId 
              AND SubmittedDate BETWEEN @StartDate AND @EndDate 
        ORDER BY ReportDate, Shift";

            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@PlantId", plantId);
                cmd.Parameters.AddWithValue("@LineId", lineId);
                cmd.Parameters.AddWithValue("@CategoryId", categoryId);
                cmd.Parameters.AddWithValue("@BrandId", brandId);
                cmd.Parameters.AddWithValue("@StartDate", startDate);
                cmd.Parameters.AddWithValue("@EndDate", endDate);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DateTime date = Convert.ToDateTime(reader["ReportDate"]);
                        string shift = Convert.ToString(reader["Shift"]);
                        dateShifts.Add(Tuple.Create(date, shift));
                    }
                }
            }

            return dateShifts;
        }

        //private void LoadReport(int plantId, int lineId, int categoryId, int brandId, DateTime startDate, DateTime endDate)
        //{
        //    StringBuilder html = new StringBuilder();

        //    html.Append("<h3 style='text-align:center;'>Production Process Report</h3>");

        //    using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["YourConnectionString"].ConnectionString))
        //    {
        //        con.Open();

        //        var dateShiftPairs = GetDateShiftPairs(con, plantId, lineId, categoryId, brandId, startDate, endDate);

        //        foreach (var pair in dateShiftPairs)
        //        {
        //            DateTime reportDate = pair.Item1;
        //            string shift = pair.Item2;

        //            html.Append($"<h4 style='margin-top:30px;'>Date: {reportDate:dd-MM-yyyy} &nbsp;&nbsp;&nbsp;&nbsp; Shift: {shift}</h4>");

        //            html.Append(GetRawMaterialWeights(con, plantId, lineId, categoryId, brandId, reportDate, shift));
        //            html.Append(GetDoughData(con, plantId, lineId, categoryId, brandId, reportDate, shift));
        //            html.Append(GetSpongeData(con, plantId, lineId, categoryId, brandId, reportDate, shift));
        //            html.Append(GetOvenZoneTemperatures(con, plantId, lineId, categoryId, brandId, reportDate, shift));
        //        }
        //    }

        //    litReport.Text = html.ToString(); // display on screen

        //    // Optional: export directly to Excel after loading
        //    // ExportToExcel(html.ToString());  // UNCOMMENT if needed
        //}
        public class ReportRow
        {
            public string Name { get; set; }
            public string UOM { get; set; }
            public string Standard { get; set; }
            public string A { get; set; }
            public string B { get; set; }
            public string C { get; set; }
        }
        //private string GetRawMaterialWeights(SqlConnection con, int plantId, int lineId, int categoryId, int brandId, DateTime startDate, DateTime endDate)
        //{
        //    StringBuilder html = new StringBuilder();
        //    html.Append("<table border='1' style='width:100%; border-collapse: collapse;'>");
        //    html.Append("<tr style='background-color:#f2f2f2;'><th>Date</th><th>Shift</th><th>Material Name</th><th>Actual Weight</th><th>Standard</th></tr>");

        //        string query = @"
        //    SELECT ID, PcrNo, RM_Weights, SubmittedDate, Shift
        //    FROM TRN_ProcessChecking_BasicData
        //    WHERE PlantName = @Plant AND Line = @Line AND ProductCategory = @Category AND ProductBrand = @Brand
        //      AND CAST(SubmittedDate AS DATE) BETWEEN @StartDate AND @EndDate
        //";

        //        using (SqlCommand cmd = new SqlCommand(query, con))
        //        {
        //            cmd.Parameters.AddWithValue("@Plant", plantId.ToString());
        //            cmd.Parameters.AddWithValue("@Line", lineId.ToString());
        //            cmd.Parameters.AddWithValue("@Category", categoryId.ToString());
        //            cmd.Parameters.AddWithValue("@Brand", brandId.ToString());
        //            cmd.Parameters.AddWithValue("@StartDate", startDate);
        //            cmd.Parameters.AddWithValue("@EndDate", endDate);

        //            con.Open();
        //            using (SqlDataReader reader = cmd.ExecuteReader())
        //            {
        //                while (reader.Read())
        //                {
        //                    string rmWeightsJson = reader["RM_Weights"].ToString();
        //                    DateTime createdDate = Convert.ToDateTime(reader["SubmittedDate"]);
        //                    string shift = reader["Shift"].ToString();

        //                    var materialList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(rmWeightsJson);

        //                    foreach (var material in materialList)
        //                    {
        //                        string materialName = material.ContainsKey("MaterialName") ? material["MaterialName"] : "";
        //                        string actualWeight = material.ContainsKey("ActualWeight") ? material["ActualWeight"] : "";
        //                        string materialId = material.ContainsKey("MaterialId") ? material["MaterialId"] : "";

        //                        string standard = GetStandardValueForMaterial(materialId, plantId, lineId, categoryId, brandId, "RawMaterial");

        //                        html.Append("<tr>");
        //                        html.AppendFormat("<td>{0:dd-MM-yyyy}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td>",
        //                            createdDate, shift, materialName, actualWeight, standard);
        //                        html.Append("</tr>");
        //                    }
        //                }
        //            }
        //        }

        //    html.Append("</table>");
        //    return html.ToString();
        //}
        private string GetStandardValueForMaterial2(string materialId, int plantId, int lineId, int categoryId, int brandId, string subForm)
        {
            string standard = "";

            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            {
                string stdQuery = @"
            SELECT RV_MinValue, RV_MaxValue
            FROM MST_FormFieldControls
            WHERE FormID = 6 AND Sub_Form = @SubForm AND RecordType = 'Specific'
              AND ViewMode = 1 AND IsLatest = 1 AND IsDeleted = 0
              AND material_id = @MaterialId
              AND plant_id = @PlantId AND LineId = @LineId AND category_id = @CategoryId AND brand_id = @BrandId
        ";

                using (SqlCommand cmd = new SqlCommand(stdQuery, con))
                {
                    cmd.Parameters.AddWithValue("@SubForm", subForm);
                    cmd.Parameters.AddWithValue("@MaterialId", materialId);
                    cmd.Parameters.AddWithValue("@PlantId", plantId);
                    cmd.Parameters.AddWithValue("@LineId", lineId);
                    cmd.Parameters.AddWithValue("@CategoryId", categoryId);
                    cmd.Parameters.AddWithValue("@BrandId", brandId);

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string min = reader["RV_MinValue"] != DBNull.Value ? reader["RV_MinValue"].ToString() : "";
                            string max = reader["RV_MaxValue"] != DBNull.Value ? reader["RV_MaxValue"].ToString() : "";
                            standard = string.Format("{0} ~ {1}", min, max);
                        }
                    }
                }
            }

            return standard;
        }
        private string GetDoughData2(SqlConnection con, int plantId, int lineId, int categoryId, int brandId, DateTime reportDate, string shift)
        {
            StringBuilder html = new StringBuilder();
            html.Append("<h4 style='margin-top:20px;'>Dough Data - Date: " + reportDate.ToString("dd-MM-yyyy") + ", Shift: " + shift + "</h4>");
            html.Append("<table border='1' style='border-collapse:collapse; width:100%; font-size:10pt;'>");
            html.Append("<tr style='background-color:#f2f2f2;'>");
            html.Append("<th>PCR No</th>");
            html.Append("<th>Dough Temp</th>");
            html.Append("<th>Mixing Time</th>");
            html.Append("<th>Creaming Time</th>");
            html.Append("<th>Dice RPM</th>");
            html.Append("<th>Standing Time</th>");
            html.Append("<th>Standard (Min~Max)</th>");
            html.Append("</tr>");

            string query = @"
        SELECT 
            d.PcrNo,
            d.DoughTemp,
            d.MixingTime,
            d.CreamingTime,
            d.DiceRpm,
            d.StandingTime,
            b.SubmittedDate,
            b.Shift,
            b.material_id
        FROM PCR_DoughData d
        INNER JOIN TRN_ProcessChecking_BasicData b ON d.PcrNo = b.PcrNo
        WHERE 
            b.PlantName = @PlantId AND
            b.Line = @LineId AND
            b.ProductCategory = @CategoryId AND
            b.ProductBrand = @BrandId AND
            CAST(b.SubmittedDate AS DATE) = @ReportDate AND
            b.Shift = @Shift
        ORDER BY b.SubmittedDate, b.Shift, d.PcrNo";

            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@PlantId", plantId);
                cmd.Parameters.AddWithValue("@LineId", lineId);
                cmd.Parameters.AddWithValue("@CategoryId", categoryId);
                cmd.Parameters.AddWithValue("@BrandId", brandId);
                cmd.Parameters.AddWithValue("@ReportDate", reportDate.Date);
                cmd.Parameters.AddWithValue("@Shift", shift);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string pcrNo = reader["PcrNo"].ToString();
                        string doughTemp = reader["DoughTemp"].ToString();
                        string mixingTime = reader["MixingTime"].ToString();
                        string creamingTime = reader["CreamingTime"].ToString();
                        string diceRpm = reader["DiceRpm"].ToString();
                        string standingTime = reader["StandingTime"].ToString();
                        string standardRange = "-";

                        // OPTIONAL: Fetch from MST_FormFieldControls using material_id
                        int materialId = reader["material_id"] != DBNull.Value ? Convert.ToInt32(reader["material_id"]) : 0;

                        using (SqlCommand stdCmd = new SqlCommand(@"
                    SELECT RV_MinValue, RV_MaxValue 
                    FROM MST_FormFieldControls
                    WHERE 
                        Sub_Form = 'DoughData' AND 
                        FormID = 6 AND 
                        RecordType = 'Specific' AND 
                        material_id = @MaterialId AND 
                        plant_id = @PlantId AND 
                        LineId = @LineId AND 
                        category_id = @CategoryId AND 
                        brand_id = @BrandId AND 
                        ViewMode = 1 AND 
                        IsLatest = 1 AND 
                        IsDeleted = 0", con))
                        {
                            stdCmd.Parameters.AddWithValue("@MaterialId", materialId);
                            stdCmd.Parameters.AddWithValue("@PlantId", plantId);
                            stdCmd.Parameters.AddWithValue("@LineId", lineId);
                            stdCmd.Parameters.AddWithValue("@CategoryId", categoryId);
                            stdCmd.Parameters.AddWithValue("@BrandId", brandId);

                            using (SqlDataReader stdReader = stdCmd.ExecuteReader())
                            {
                                if (stdReader.Read())
                                {
                                    string minVal = stdReader["RV_MinValue"]?.ToString();
                                    string maxVal = stdReader["RV_MaxValue"]?.ToString();
                                    if (!string.IsNullOrEmpty(minVal) && !string.IsNullOrEmpty(maxVal))
                                    {
                                        standardRange = minVal + " ~ " + maxVal;
                                    }
                                }
                            }
                        }

                        html.Append("<tr>");
                        html.Append("<td>" + pcrNo + "</td>");
                        html.Append("<td>" + doughTemp + "</td>");
                        html.Append("<td>" + mixingTime + "</td>");
                        html.Append("<td>" + creamingTime + "</td>");
                        html.Append("<td>" + diceRpm + "</td>");
                        html.Append("<td>" + standingTime + "</td>");
                        html.Append("<td>" + standardRange + "</td>");
                        html.Append("</tr>");
                    }
                }
            }

            html.Append("</table>");
            return html.ToString();
        }
        private string GetRawMaterialWeights2(SqlConnection con, int plantId, int lineId, int categoryId, int brandId, DateTime date)
        {
            StringBuilder html = new StringBuilder();
            html.Append("<h5>Raw Material</h5>");
            html.Append("<table border='1'><tr><th>SL</th><th>Raw Material</th><th>UOM</th><th>STD</th><th>Diff. A</th><th>Diff. B</th><th>Diff. C</th></tr>");

            string sql = @"
        SELECT DISTINCT c.material_id, c.material_name, c.UOM, c.RV_MinValue, c.RV_MaxValue
        FROM MST_FormFieldControls c
        WHERE c.FormID = 6 AND c.Sub_Form = 'RawMaterial' AND c.RecordType = 'Specific' AND c.ViewMode = 1
        AND c.IsLatest = 1 AND c.IsDeleted = 0
        AND c.plant_id = @plantId AND c.LineId = @lineId AND c.category_id = @categoryId AND c.brand_id = @brandId";

            SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@plantId", plantId);
            cmd.Parameters.AddWithValue("@lineId", lineId);
            cmd.Parameters.AddWithValue("@categoryId", categoryId);
            cmd.Parameters.AddWithValue("@brandId", brandId);

            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);

            int sl = 1;
            foreach (DataRow row in dt.Rows)
            {
                int materialId = Convert.ToInt32(row["material_id"]);
                string name = row["material_name"].ToString();
                string uom = row["UOM"].ToString();
                decimal? min = row["RV_MinValue"] as decimal?;
                decimal? max = row["RV_MaxValue"] as decimal?;
                string std = $"{min}~{max}";

                Dictionary<string, string> shiftValues = new Dictionary<string, string> { { "A", "0" }, { "B", "0" }, { "C", "0" } };

                foreach (string shift in shiftValues.Keys.ToList())
                {
                    string subQuery = @"
                SELECT TRY_CAST(JSON_VALUE(RM_Weights, '$[?].ActualWeight') AS decimal(18,2)) AS ActualWeight
                FROM TRN_ProcessChecking_BasicData
                WHERE Shift = @shift AND CAST(Date AS DATE) = @date
                AND PlantName = @plant AND Line = @line AND ProductCategory = @cat AND ProductBrand = @brand
                AND ISJSON(RM_Weights) = 1 AND RM_Weights LIKE '%material_id%""' + CAST(@mid AS varchar) + '""%'";

                    SqlCommand scmd = new SqlCommand(subQuery, con);
                    scmd.Parameters.AddWithValue("@shift", shift);
                    scmd.Parameters.AddWithValue("@date", date.Date);
                    scmd.Parameters.AddWithValue("@plant", plantId);
                    scmd.Parameters.AddWithValue("@line", lineId);
                    scmd.Parameters.AddWithValue("@cat", categoryId);
                    scmd.Parameters.AddWithValue("@brand", brandId);
                    scmd.Parameters.AddWithValue("@mid", materialId);

                    object val = scmd.ExecuteScalar();
                    shiftValues[shift] = val != DBNull.Value && val != null ? val.ToString() : "0";
                }

                html.AppendFormat("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td>{6}</td></tr>",
                    sl++, name, uom, std, shiftValues["A"], shiftValues["B"], shiftValues["C"]);
            }

            html.Append("</table>");
            return html.ToString();
        }
        private string GetSpongeData1(SqlConnection con, int plantId, int lineId, int categoryId, int brandId, DateTime date)
        {
            StringBuilder html = new StringBuilder();
            html.Append("<h5>Sponge Profile</h5>");
            html.Append("<table border='1'><tr><th>SL</th><th>Parameter</th><th>UOM</th><th>STD</th><th>A</th><th>B</th><th>C</th></tr>");

            string query = @"
        SELECT DISTINCT FieldID, FieldName, UOM, RV_MinValue, RV_MaxValue
        FROM MST_FormFieldControls
        WHERE FormID = 6 AND Sub_Form = 'SpongeData' AND RecordType = 'Specific' AND ViewMode = 1 
        AND IsLatest = 1 AND IsDeleted = 0
        AND plant_id = @plantId AND LineId = @lineId AND category_id = @categoryId AND brand_id = @brandId";

            SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@plantId", plantId);
            cmd.Parameters.AddWithValue("@lineId", lineId);
            cmd.Parameters.AddWithValue("@categoryId", categoryId);
            cmd.Parameters.AddWithValue("@brandId", brandId);

            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);

            int sl = 1;
            foreach (DataRow row in dt.Rows)
            {
                string field = row["FieldName"].ToString();
                string uom = row["UOM"].ToString();
                string std = $"{row["RV_MinValue"]}~{row["RV_MaxValue"]}";

                Dictionary<string, string> shiftVals = new Dictionary<string, string> { { "A", "" }, { "B", "" }, { "C", "" } };

                foreach (string shift in shiftVals.Keys.ToList())
                {
                    string q = @"
                SELECT s.[{0}]
                FROM TRN_ProcessChecking_BasicData b
                JOIN PCR_SpongeData s ON b.PcrNo = s.PcrNo
                WHERE Shift = @shift AND CAST(Date AS DATE) = @date
                AND PlantName = @plant AND Line = @line AND ProductCategory = @cat AND ProductBrand = @brand";

                    SqlCommand scmd = new SqlCommand(string.Format(q, field), con);
                    scmd.Parameters.AddWithValue("@shift", shift);
                    scmd.Parameters.AddWithValue("@date", date.Date);
                    scmd.Parameters.AddWithValue("@plant", plantId);
                    scmd.Parameters.AddWithValue("@line", lineId);
                    scmd.Parameters.AddWithValue("@cat", categoryId);
                    scmd.Parameters.AddWithValue("@brand", brandId);

                    object val = scmd.ExecuteScalar();
                    shiftVals[shift] = val != DBNull.Value && val != null ? val.ToString() : "";
                }

                html.AppendFormat("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td>{6}</td></tr>",
                    sl++, field, uom, std, shiftVals["A"], shiftVals["B"], shiftVals["C"]);
            }

            html.Append("</table>");
            return html.ToString();
        }
        private string GetOvenZoneTemperatures222(SqlConnection con, int plantId, int lineId, int categoryId, int brandId, DateTime startDate, DateTime endDate)
        {
            StringBuilder html = new StringBuilder();
            string query = @"
    SELECT 
        CAST(basicData.Date AS DATE) AS ReportDate,
        basicData.Shift,
        oven.Zone,
        oven.UOM,
        oven.OvenTop,
        oven.OvenBottom,
        ISNULL(ctrl.RV_MinValue, 0) AS MinVal,
        ISNULL(ctrl.RV_MaxValue, 0) AS MaxVal
    FROM TRN_ProcessChecking_BasicData basicData
    CROSS APPLY OPENJSON(basicData.Oven_Temperatures)
    WITH (
        Zone NVARCHAR(100),
        UOM NVARCHAR(20),
        OvenTop FLOAT,
        OvenBottom FLOAT,
        DamperTop FLOAT,
        DamperBottom FLOAT
    ) oven
    LEFT JOIN MST_FormFieldControls ctrl ON 
        ctrl.FieldName = oven.Zone AND
        ctrl.Sub_Form = 'OvenData' AND
        ctrl.FormID = 6 AND
        ctrl.RecordType = 'Specific' AND
        ctrl.ViewMode = 1 AND
        ctrl.IsLatest = 1 AND
        ctrl.IsDeleted = 0 AND
        ctrl.plant_id = @plantId AND
        ctrl.LineId = @lineId AND
        ctrl.category_id = @categoryId AND
        ctrl.brand_id = @brandId
    WHERE 
        basicData.PlantID = @plantId AND
        basicData.LineID = @lineId AND
        basicData.ProductCategory = @categoryId AND
        basicData.ProductBrand = @brandId AND
        CAST(basicData.Date AS DATE) BETWEEN @startDate AND @endDate
    ORDER BY ReportDate, Shift, oven.Zone;
    ";

            SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@plantId", plantId);
            cmd.Parameters.AddWithValue("@lineId", lineId);
            cmd.Parameters.AddWithValue("@categoryId", categoryId);
            cmd.Parameters.AddWithValue("@brandId", brandId);
            cmd.Parameters.AddWithValue("@startDate", startDate);
            cmd.Parameters.AddWithValue("@endDate", endDate);

            SqlDataReader rdr = cmd.ExecuteReader();
            string currentDateShift = "";
            int sl = 1;

            while (rdr.Read())
            {
                string dateShift = $"{Convert.ToDateTime(rdr["ReportDate"]):dd-MM-yyyy} | Shift {rdr["Shift"]}";
                if (currentDateShift != dateShift)
                {
                    if (currentDateShift != "")
                        html.Append("</table><br/>");

                    html.Append($"<h4 style='text-align:left;'>Oven Zone Temperatures - {dateShift}</h4>");
                    html.Append("<table border='1' cellspacing='0' cellpadding='5' style='width:100%; border-collapse:collapse;'>");
                    html.Append("<tr style='background-color:#fcf8e3;font-weight:bold;'><th>SL</th><th>Zone</th><th>UOM</th><th>STD</th><th>Top</th><th>Bottom</th></tr>");
                    currentDateShift = dateShift;
                    sl = 1;
                }

                string std = $"{rdr["MinVal"]} - {rdr["MaxVal"]}";

                html.AppendFormat("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td></tr>",
                    sl++,
                    rdr["Zone"],
                    rdr["UOM"],
                    std,
                    rdr["OvenTop"],
                    rdr["OvenBottom"]);
            }

            if (currentDateShift != "")
                html.Append("</table><br/>");

            rdr.Close();
            return html.ToString();
        }
        private string GetDoughData2(SqlConnection con, int plantId, int lineId, int categoryId, int brandId, DateTime startDate, DateTime endDate)
        {
            StringBuilder html = new StringBuilder();

            string query = @"
        SELECT b.Shift, b.SubmittedDate, d.DoughTemp, b.PcrNo
        FROM TRN_ProcessChecking_BasicData b
        INNER JOIN PCR_DoughData d ON b.PcrNo = d.PcrNo
        WHERE b.PlantName = @PlantId AND b.Line = @LineId AND b.ProductCategory = @CategoryId AND b.ProductBrand = @BrandId
              AND b.SubmittedDate BETWEEN @StartDate AND @EndDate";

            var shiftValues = new Dictionary<string, decimal>();

            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@PlantId", plantId);
                cmd.Parameters.AddWithValue("@LineId", lineId);
                cmd.Parameters.AddWithValue("@CategoryId", categoryId);
                cmd.Parameters.AddWithValue("@BrandId", brandId);
                cmd.Parameters.AddWithValue("@StartDate", startDate);
                cmd.Parameters.AddWithValue("@EndDate", endDate);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        decimal actual;
                        string shift = reader["Shift"]?.ToString();
                        if (!string.IsNullOrEmpty(shift) && decimal.TryParse(reader["DoughTemp"]?.ToString(), out actual))
                        {
                            shiftValues[shift] = actual; // Only one value per shift will be stored
                        }
                    }
                }
            }

            // Fetch standard range for DoughTemp
            string standard = GetStandardRange(con, "DoughData", "DoughTemp", plantId, lineId, categoryId, brandId);

            // Build HTML
            html.Append("<h4>Dough Data</h4>");
            html.Append("<table border='1' cellpadding='5' cellspacing='0'>");
            html.Append("<tr><th>Parameter</th><th>Standard</th><th>A</th><th>B</th><th>C</th></tr>");

            html.Append("<tr>");
            html.Append("<td>DoughTemp</td>");
            html.Append($"<td>{standard}</td>");

            foreach (string shift in new[] { "A", "B", "C" })
            {
                string value = shiftValues.ContainsKey(shift) ? shiftValues[shift].ToString() : "";
                html.Append($"<td>{value}</td>");
            }

            html.Append("</tr>");
            html.Append("</table>");

            return html.ToString();
        }
        private string GetOvenZoneTemperatures2(SqlConnection con, int plantId, int lineId, int categoryId, int brandId, DateTime date)
        {
            StringBuilder html = new StringBuilder();
            html.Append("<h5>Oven Zone Temperatures</h5>");
            html.Append("<table border='1'><tr><th>SL</th><th>Zone</th><th>UOM</th><th>STD</th><th>A</th><th>B</th><th>C</th></tr>");

            string query = @"
        SELECT DISTINCT FieldID, FieldName, UOM, RV_MinValue, RV_MaxValue
        FROM MST_FormFieldControls
        WHERE FormID = 6 AND Sub_Form = 'OvenData' AND RecordType = 'Specific' AND ViewMode = 1 
        AND IsLatest = 1 AND IsDeleted = 0
        AND plant_id = @plantId AND LineId = @lineId AND category_id = @categoryId AND brand_id = @brandId";

            SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@plantId", plantId);
            cmd.Parameters.AddWithValue("@lineId", lineId);
            cmd.Parameters.AddWithValue("@categoryId", categoryId);
            cmd.Parameters.AddWithValue("@brandId", brandId);

            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);

            int sl = 1;
            foreach (DataRow row in dt.Rows)
            {
                string zoneField = row["FieldName"].ToString();
                string uom = row["UOM"].ToString();
                string std = $"{row["RV_MinValue"]}~{row["RV_MaxValue"]}";

                Dictionary<string, string> shiftVals = new Dictionary<string, string> { { "A", "" }, { "B", "" }, { "C", "" } };

                foreach (string shift in shiftVals.Keys.ToList())
                {
                    string queryTemp = @"
                SELECT TRY_CAST(JSON_VALUE(Oven_Temperatures, '$[0]." + zoneField + @"') AS varchar)
                FROM TRN_ProcessChecking_BasicData
                WHERE Shift = @shift AND CAST(Date AS DATE) = @date
                AND PlantName = @plant AND Line = @line AND ProductCategory = @cat AND ProductBrand = @brand";

                    SqlCommand scmd = new SqlCommand(queryTemp, con);
                    scmd.Parameters.AddWithValue("@shift", shift);
                    scmd.Parameters.AddWithValue("@date", date.Date);
                    scmd.Parameters.AddWithValue("@plant", plantId);
                    scmd.Parameters.AddWithValue("@line", lineId);
                    scmd.Parameters.AddWithValue("@cat", categoryId);
                    scmd.Parameters.AddWithValue("@brand", brandId);

                    object val = scmd.ExecuteScalar();
                    shiftVals[shift] = val != DBNull.Value && val != null ? val.ToString() : "";
                }

                html.AppendFormat("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td>{6}</td></tr>",
                    sl++, zoneField, uom, std, shiftVals["A"], shiftVals["B"], shiftVals["C"]);
            }

            html.Append("</table>");
            return html.ToString();
        }
        private string GetSpongeData12(SqlConnection con, int plantId, int lineId, int categoryId, int brandId, DateTime startDate, DateTime endDate)
        {
            StringBuilder html = new StringBuilder();

            string query = @"
        SELECT b.Shift, b.SubmittedDate, s.Temp, b.PcrNo
        FROM TRN_ProcessChecking_BasicData b
        INNER JOIN PCR_SpongeData s ON b.PcrNo = s.PcrNo
        WHERE b.PlantName = @PlantId 
              AND b.Line = @LineId 
              AND b.ProductCategory = @CategoryId 
              AND b.ProductBrand = @BrandId
              AND b.SubmittedDate BETWEEN @StartDate AND @EndDate";

            var shiftValues = new Dictionary<string, decimal>();

            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@PlantId", plantId);
                cmd.Parameters.AddWithValue("@LineId", lineId);
                cmd.Parameters.AddWithValue("@CategoryId", categoryId);
                cmd.Parameters.AddWithValue("@BrandId", brandId);
                cmd.Parameters.AddWithValue("@StartDate", startDate);
                cmd.Parameters.AddWithValue("@EndDate", endDate);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string shift = reader["Shift"]?.ToString();
                        decimal actual;
                        if (decimal.TryParse(reader["Temp"]?.ToString(), out actual) && !string.IsNullOrEmpty(shift))
                        {
                            shiftValues[shift] = actual; // Latest value per shift
                        }
                    }
                }
            }

            html.Append("<h4>Sponge Data</h4>");
            html.Append("<table border='1' cellpadding='5' cellspacing='0'><tr><th>Parameter</th><th>Standard</th><th>A</th><th>B</th><th>C</th></tr>");

            string std = GetStandardRange(con, "SpongeData", "Temp", plantId, lineId, categoryId, brandId);
            html.Append($"<tr><td>Temp</td><td>{std}</td>");

            foreach (string shift in new[] { "A", "B", "C" })
            {
                string value = shiftValues.ContainsKey(shift) ? shiftValues[shift].ToString() : "";
                html.Append($"<td>{value}</td>");
            }

            html.Append("</tr></table>");
            return html.ToString();
        }
        private string GetStandardRange(SqlConnection con, string subForm, string parameter, int plantId, int lineId, int categoryId, int brandId)
        {
            string query = @"
            SELECT RV_MinValue, RV_MaxValue
            FROM MST_FormFieldControls
            WHERE Sub_Form = @SubForm AND field_name = @Parameter
                  AND FormID = 6 AND RecordType = 'Specific' AND ViewMode = 1
                  AND IsLatest = 1 AND IsDeleted = 0
                  AND plant_id = @PlantId AND LineId = @LineId AND category_id = @CategoryId AND material_id = @BrandId";

            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@SubForm", subForm);
                cmd.Parameters.AddWithValue("@Parameter", parameter);
                cmd.Parameters.AddWithValue("@PlantId", plantId);
                cmd.Parameters.AddWithValue("@LineId", lineId);
                cmd.Parameters.AddWithValue("@CategoryId", categoryId);
                cmd.Parameters.AddWithValue("@BrandId", brandId);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string min = reader["RV_MinValue"]?.ToString();
                        string max = reader["RV_MaxValue"]?.ToString();
                        return $"{min} ~ {max}";
                    }
                }
            }
            return "-";
        }
        private string GetOvenZoneTemperatures1(SqlConnection con, int plantId, int lineId, int categoryId, int brandId, DateTime startDate, DateTime endDate)
        {
            StringBuilder html = new StringBuilder();

            string query = @"
                SELECT SubmittedDate, Shift, Oven_Temperatures
                FROM TRN_ProcessChecking_BasicData
                WHERE PlantName = @PlantId AND Line = @LineId AND ProductCategory = @CategoryId AND ProductBrand = @BrandId
                AND SubmittedDate BETWEEN @StartDate AND @EndDate";

            var zones = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var shiftData = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@PlantId", plantId);
                cmd.Parameters.AddWithValue("@LineId", lineId);
                cmd.Parameters.AddWithValue("@CategoryId", categoryId);
                cmd.Parameters.AddWithValue("@BrandId", brandId);
                cmd.Parameters.AddWithValue("@StartDate", startDate);
                cmd.Parameters.AddWithValue("@EndDate", endDate);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string shift = reader["Shift"]?.ToString();
                        string json = reader["Oven_Temperatures"]?.ToString();
                        if (string.IsNullOrWhiteSpace(shift) || string.IsNullOrWhiteSpace(json)) continue;

                        DataTable table = JsonConvert.DeserializeObject<DataTable>(json);
                        foreach (DataRow row in table.Rows)
                        {
                            string zone = row["Zone"]?.ToString()?.Trim();
                            if (string.IsNullOrEmpty(zone)) continue;

                            zones.Add(zone);
                            string key = $"{zone}~{shift}";

                            string top = row["OvenTop"]?.ToString();
                            string bottom = row["OvenBottom"]?.ToString();
                            shiftData[key] = new Dictionary<string, string>
                    {
                        { "Top", top ?? "" },
                        { "Bottom", bottom ?? "" }
                    };
                        }
                    }
                }
            }

            html.Append("<h4>Oven Zone Temperatures</h4>");
            html.Append("<table border='1' cellpadding='5' cellspacing='0'><tr><th>Zone</th><th>Standard</th><th>A</th><th>B</th><th>C</th></tr>");

            foreach (string zone in zones.OrderBy(z => z))
            {
                string std = GetStandardRange(con, "OvenData", zone, plantId, lineId, categoryId, brandId);
                html.Append($"<tr><td>{zone}</td><td>{std}</td>");

                foreach (string shift in new[] { "A", "B", "C" })
                {
                    string key = $"{zone}~{shift}";
                    Dictionary<string, string> data;
                    if (shiftData.TryGetValue(key, out data))
                    {
                        html.Append($"<td>Top: {data["Top"]}<br/>Bottom: {data["Bottom"]}</td>");
                    }
                    else
                    {
                        html.Append("<td></td>");
                    }
                }

                html.Append("</tr>");
            }

            html.Append("</table>");
            return html.ToString();
        }
        public class ProcessCheckingSummaryReport
        {
            public string Plant { get; set; }
            public string Line { get; set; }
            public string Category { get; set; }
            public string Brand { get; set; }
            public DateTime DateFrom { get; set; }
            public DateTime DateTo { get; set; }

            public List<ReportRow> RawMaterials { get; set; }
            public List<ReportRow> DoughProfile { get; set; }
            public List<ReportRow> SpongeProfile { get; set; }
            public List<ReportRow> OvenZones { get; set; }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------//

        //private void ExportRawMaterialReport()
        //{
        //    DateTime startDate = Convert.ToDateTime(TB_Date_From.Text.Trim());
        //    DateTime endDate = Convert.ToDateTime(TB_Date_To.Text.Trim());
        //    int plantId = Convert.ToInt32(DDL_Plant.SelectedValue);
        //    int lineId = Convert.ToInt32(DDL_PlantLine.SelectedValue);
        //    int categoryId = Convert.ToInt32(DDL_ProductCategory.SelectedValue);
        //    int brandId = Convert.ToInt32(DDL_ProductBrand.SelectedValue);

        //    DataTable dt = FetchRawMaterialReport(plantId, lineId, categoryId, brandId, startDate, endDate);

        //    if (dt.Rows.Count > 0)
        //    {
        //        ExportToExcel(dt, "RawMaterialWeightReport");
        //    }
        //}

        private DataTable FetchRawMaterialReport(int plantId, int lineId, int categoryId, int brandId, DateTime fromDate, DateTime toDate)
        {
            DataTable dtResult = new DataTable();
            dtResult.Columns.Add("Variety");
            dtResult.Columns.Add("Standard");

            // Add dynamic columns: e.g., 2025-07-25 A, 2025-07-25 B, ..., 2025-07-30 C
            for (DateTime date = fromDate; date <= toDate; date = date.AddDays(1))
            {
                dtResult.Columns.Add(date.ToString("yyyy-MM-dd") + " A");
                dtResult.Columns.Add(date.ToString("yyyy-MM-dd") + " B");
                dtResult.Columns.Add(date.ToString("yyyy-MM-dd") + " C");
            }

            // Get raw data from DB
            DataTable dtRaw = new DataTable();
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SELECT * FROM dbo.GetRawMaterialWeights(@plantId, @lineId, @categoryId, @brandId, @fromDate, @toDate)", con))
                {
                    cmd.Parameters.AddWithValue("@plantId", plantId);
                    cmd.Parameters.AddWithValue("@lineId", lineId);
                    cmd.Parameters.AddWithValue("@categoryId", categoryId);
                    cmd.Parameters.AddWithValue("@brandId", brandId);
                    cmd.Parameters.AddWithValue("@fromDate", fromDate);
                    cmd.Parameters.AddWithValue("@toDate", toDate);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dtRaw);
                    }
                }
            }

            // Pivot logic
            var uniqueVarieties = dtRaw.DefaultView.ToTable(true, "Variety");

            foreach (DataRow row in uniqueVarieties.Rows)
            {
                string variety = row["Variety"].ToString();
                var matchingRows = dtRaw.Select("Variety = '" + variety.Replace("'", "''") + "'");

                DataRow newRow = dtResult.NewRow();
                newRow["Variety"] = variety;

                // Take first available standard value
                foreach (var dataRow in matchingRows)
                {
                    if (dataRow["StandardWeight"] != DBNull.Value && !string.IsNullOrEmpty(dataRow["StandardWeight"].ToString()))
                    {
                        newRow["Standard"] = dataRow["StandardWeight"].ToString();
                        break;
                    }
                }

                foreach (var dataRow in matchingRows)
                {
                    string key = Convert.ToDateTime(dataRow["EntryDate"]).ToString("yyyy-MM-dd") + " " + dataRow["Shift"].ToString();
                    if (dtResult.Columns.Contains(key))
                    {
                        newRow[key] = dataRow["ActualWeight"].ToString();
                    }
                }

                dtResult.Rows.Add(newRow);
            }

            return dtResult;
        }
        private void ExportToExcel2(DataTable dt, string fileName)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("<table border='1'>");
            sb.Append("<tr>");

            // Header Row
            foreach (DataColumn col in dt.Columns)
            {
                sb.AppendFormat("<th style='background-color: #d3d3d3; font-weight:bold;'>{0}</th>", col.ColumnName);
            }
            sb.Append("</tr>");

            // Data Rows
            foreach (DataRow row in dt.Rows)
            {
                sb.Append("<tr>");
                foreach (DataColumn col in dt.Columns)
                {
                    sb.AppendFormat("<td style='mso-number-format:\\@'>{0}</td>", row[col]);
                }
                sb.Append("</tr>");
            }
            sb.Append("</table>");

            string filename = fileName + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xls";
            HttpContext.Current.Response.Clear();
            HttpContext.Current.Response.Buffer = true;
            HttpContext.Current.Response.ContentType = "application/vnd.ms-excel";
            HttpContext.Current.Response.AddHeader("content-disposition", "attachment;filename=" + filename);
            HttpContext.Current.Response.Charset = "";
            HttpContext.Current.Response.Output.Write(sb.ToString());
            HttpContext.Current.Response.Flush();
            HttpContext.Current.Response.End();
        }
    }
}