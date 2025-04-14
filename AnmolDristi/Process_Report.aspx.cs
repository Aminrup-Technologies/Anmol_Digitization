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
using System.Security.Cryptography;
using System.Text;

//using Microsoft.Office.Interop.Excel;
//using System.Runtime.InteropServices; // Optional, for Excel interop cleanup
//using ExcelInterop = Microsoft.Office.Interop.Excel;

namespace AnmolDristi
{
    public partial class Process_Report : System.Web.UI.Page
    {
        //public static string PcrNo = string.Empty;
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
                    loadAlldata();
                    //BindGridView();
                    //ShowHideGridViewColumns();

                }

            }
        }

        private void loadAlldata()
        {
            try
            {
                string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

                string query = @"
                    SELECT TOP(30)
                        c.ID AS DBID,
                        c.PcrNo,
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
                        'N/A' as Remarks,
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
                        TRN_ProcessChecking_BasicData c
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
                DataLoader();
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
                DataLoader();
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
                DataLoader();
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

                //DataTable dataTable = DatabaseHelper.GetBrandFieldsControlByBrandId(Convert.ToInt16(selectedProductBrandValue));
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
                DataLoader();
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
            DataLoader();
        }


        protected void ReportbtnCancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("home.aspx",false);
        }
        protected void ReportbtnReset_Click(object sender, EventArgs e)
        {
            Response.Redirect("Process_Report.aspx",false);
        }
        protected void ReportbtnSubmit_Click(object sender, EventArgs e)
        {
            //DateTime fromDate = Convert.ToDateTime(TB_Date_From.Text);
            //DateTime toDate = Convert.ToDateTime(TB_Date_To.Text);
            //if (toDate > DateTime.Now)
            //{
            //    Response.Write("<script>alert('ToDate cannot be greater than current date!');</script>");
            //}
            //else if (fromDate > toDate)
            //{
            //    Response.Write("<script>alert('FromDate cannot be greater than ToDate!');</script>");
            //}
            //else
            //{
            //    getReportData(fromDate, toDate);
            //}

            DataLoader();
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
        }
        private void BindGridView()
        {
            var dataSave = new List<ReportInfo>
            {
                 new ReportInfo {},
            };

            // Bind to GridView
            GridView1.DataSource = dataSave;
            GridView1.DataBind();

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
                    if (i < 10 || i >= GridView1.Columns.Count - 3)
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
        //    //Response.Clear();
        //    //Response.Buffer = true;
        //    //Response.ContentType = "application/ms-excel";
        //    //Response.AddHeader("content-disposition", "attachment; filename= ProcessChecking_ViewPage.xls");
        //    //Response.Charset = "";
        //    //StringWriter sw = new StringWriter();
        //    //HtmlTextWriter htw = new HtmlTextWriter(sw);
        //    //GridView1.RenderControl(htw);
        //    //Response.Output.Write(sw.ToString());
        //    //Response.End();


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

        //public void AddColumnInSheetFromReader(Worksheet worksheet, SqlDataReader reader)
        //{
        //    for (int i = 0; i < reader.FieldCount; i++)
        //    {
        //        string columnName = reader.GetName(i);
        //        worksheet.Cells[1, i + 1] = columnName; // Write column headers in the first row
        //    }
        //}


        //public override void VerifyRenderingInServerForm(Control control)
        //{

        //}

        private void DataLoader()
        {
            DateTime? dateFrom = string.IsNullOrEmpty(TB_Date_From.Text) ? (DateTime?)null : DateTime.ParseExact(TB_Date_From.Text, "yyyy-MM-dd", null);
            DateTime? dateTo = string.IsNullOrEmpty(TB_Date_To.Text) ? (DateTime?)null : DateTime.ParseExact(TB_Date_To.Text, "yyyy-MM-dd", null);

            StringBuilder queryBuilder = new StringBuilder(@"
                SELECT 
                    c.ID AS DBID,
                    c.PcrNo,
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
                FROM TRN_ProcessChecking_BasicData c
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

            string pcrno = (row.FindControl("lblPcrno") as Label).Text;
            string dbid = (row.FindControl("lbl_rowid") as Label).Text;

            if (e.CommandName == "View")
            {
                Response.Redirect("Process_FinalApproval.aspx?PcrNo=" + pcrno + "&ID=" + dbid + "&VM=0", false);
            }
        }
    }
}

