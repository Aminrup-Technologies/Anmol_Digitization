using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace AnmolDristi
{
    public partial class vm_leak_test : System.Web.UI.Page
    {
        string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

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
                    PlantBinder();
                    loadAlldata();
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
            //BindGridView();

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
            //BindGridView();

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
            //BindGridView();


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
            //BindGridView();

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
            //BindGridView();

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
            //BindGridView();

        }

        protected void DDL_ProductBrand_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_ProductBrand.SelectedIndex != 0)
            {
                string selectedProductBrandValue = DDL_ProductBrand.SelectedValue.ToString();
                //BrandSKUBinder(selectedProductBrandValue);
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
            //BindGridView();

        }
        //private void BrandSKUBinder(string selectedProductBrandValue)
        //{
        //    string query = "SELECT SKUId, SKU_name FROM MST_Brand_SKU WHERE brand_id = @SelectedPlantValue";
        //    string textField = "SKU_name";
        //    string valueField = "SKUId";

        //    bool recordsBound;
        //    DatabaseHelper.BindDropDownList(query, DDL_BrandSKU, textField, valueField, new SqlParameter("@SelectedPlantValue", selectedProductBrandValue), out recordsBound);

        //    if (!recordsBound)
        //    {
        //        DatabaseHelper.BindWithDefaultNoRecords(DDL_PlantLine);

        //        string BrandSKUBinder_Error_script = @"<script type='text/javascript'>
        //              new PNotify({
        //                  title: 'Error',
        //                  text: 'An error occurred!',
        //                  type: 'error',
        //                  styling: 'bootstrap3'
        //              });
        //          </script>";
        //        ClientScript.RegisterStartupScript(this.GetType(), "ShowBrandSKUBinderErrorNotification", BrandSKUBinder_Error_script, false);
        //    }
        //    //BindGridView();
        //}



        //private void BindGridView(string productBrandId)
        //{
        //    using (SqlConnection con = new SqlConnection(connectionString))
        //    {
        //        string query = "SELECT * FROM Leak_Test_Data WHERE ProductBrand = @ProductBrand";
        //        SqlCommand cmd = new SqlCommand(query, con);
        //        cmd.Parameters.AddWithValue("@ProductBrand", productBrandId);

        //        SqlDataAdapter da = new SqlDataAdapter(cmd);
        //        DataTable dt = new DataTable();
        //        da.Fill(dt);

        //        GridView1.DataSource = dt;
        //        GridView1.DataBind();
        //    }
        //}

        public class ReportInfo
        {
            public int DBID { get; set; }
            public int FormID { get; set; }
            public string RecordID { get; set; }
            public string PlantName { get; set; }
            public string LineName { get; set; }
            public string ProductCategory { get; set; }
            public string ProductBrand { get; set; }
            public string EmpCode { get; set; }
            public string EmpName { get; set; }
            public DateTime SDate { get; set; }
            public TimeSpan STime { get; set; }
            public string SShift { get; set; }
            public string Remarks { get; set; }
            public string L1 { get; set; }
            public int? Approver1_Status { get; set; }
            public DateTime? Approver1_TimeStamp { get; set; }
            public string L2 { get; set; }
            public int? Approver2_Status { get; set; }
            public DateTime? Approver2_TimeStamp { get; set; }
            public string L3 { get; set; }
            public int? DottedApprover_Status { get; set; }
            public DateTime? DottedApprover_TimeStamp { get; set; }
        }


        //public class ReportInfo
        //{
        //    public int DBID { get; set; }
        //    public string LSP_Id { get; set; }
        //    public int FormID { get; set; }
        //    public int SubmittedById { get; set; }
        //    public DateTime SubmittedDate { get; set; }
        //    public TimeSpan SubmittedTime { get; set; }
        //    public string Shift { get; set; }
        //    public string SubmittedByEmployeeCode { get; set; }
        //    public string PlantName { get; set; }
        //    public string Line { get; set; }
        //    public string ProductCategory { get; set; }
        //    public string ProductBrand { get; set; }
        //    public string SKUId { get; set; }
        //    public DateTime? Date { get; set; }
        //    public int ViewMode { get; set; }
        //    public int DeleteMode { get; set; }
        //    public TimeSpan? Time { get; set; }
        //    public string Packing_MC_No { get; set; }
        //    public bool? LeakTestStatus { get; set; }
        //    public string RemarksForFail { get; set; }
        //    public decimal? Slanted_Percent { get; set; }
        //    public string Approver1EmployeeCode { get; set; }
        //    public int? Approver1_Status { get; set; }
        //    public DateTime? Approver1_TimeStamp { get; set; }
        //    public string Approver2EmployeeCode { get; set; }
        //    public int? Approver2_Status { get; set; }
        //    public DateTime? Approver2_TimeStamp { get; set; }
        //    public string DottedLineApproverEmployeeCode { get; set; }
        //    public int? DottedApprover_Status { get; set; }
        //    public DateTime? DottedApprover_TimeStamp { get; set; }
        //}


        //private DataTable GetFilteredData()
        //{
        //    string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        //    DataTable dt = new DataTable();

        //    using (SqlConnection con = new SqlConnection(connectionString))
        //    {
        //        string sqlQuery = "SELECT * FROM TRN_LeakSealSlanted_Data WHERE ViewMode=1";

        //        // Add filters to SQL query based on the selected values
        //        if (!string.IsNullOrEmpty(DDL_Plant.SelectedValue) && DDL_Plant.SelectedValue != "0")
        //        {
        //            sqlQuery += " AND PlantName = @Plant_Name";
        //        }

        //        if (!string.IsNullOrEmpty(DDL_PlantLine.SelectedValue) && DDL_PlantLine.SelectedValue != "0")
        //        {
        //            sqlQuery += " AND Line = @Plant_Line";
        //        }

        //        if (!string.IsNullOrEmpty(DDL_ProductCategory.SelectedValue) && DDL_ProductCategory.SelectedValue != "0")
        //        {
        //            sqlQuery += " AND ProductCategory = @Product_Category";
        //        }

        //        if (!string.IsNullOrEmpty(DDL_ProductBrand.SelectedValue) && DDL_ProductBrand.SelectedValue != "0")
        //        {
        //            sqlQuery += " AND ProductBrand = @Product_Brand";
        //        }

        //        if (!string.IsNullOrEmpty(DDL_BrandSKU.SelectedValue) && DDL_BrandSKU.SelectedValue != "0")
        //        {
        //            sqlQuery += " AND SKUId = @SkuId";
        //        }

        //        sqlQuery += " order by Id desc";

        //        using (SqlCommand cmd = new SqlCommand(sqlQuery, con))
        //        {
        //            // Add parameters only if they are being used in the query
        //            if (!string.IsNullOrEmpty(DDL_Plant.SelectedValue) && DDL_Plant.SelectedValue != "0")
        //            {
        //                cmd.Parameters.AddWithValue("@Plant_Name", DDL_Plant.SelectedValue);
        //            }

        //            if (!string.IsNullOrEmpty(DDL_PlantLine.SelectedValue) && DDL_PlantLine.SelectedValue != "0")
        //            {
        //                cmd.Parameters.AddWithValue("@Plant_Line", DDL_PlantLine.SelectedValue);
        //            }

        //            if (!string.IsNullOrEmpty(DDL_ProductCategory.SelectedValue) && DDL_ProductCategory.SelectedValue != "0")
        //            {
        //                cmd.Parameters.AddWithValue("@Product_Category", DDL_ProductCategory.SelectedValue);
        //            }

        //            if (!string.IsNullOrEmpty(DDL_ProductBrand.SelectedValue) && DDL_ProductBrand.SelectedValue != "0")
        //            {
        //                cmd.Parameters.AddWithValue("@Product_Brand", DDL_ProductBrand.SelectedValue);
        //            }

        //            if (!string.IsNullOrEmpty(DDL_BrandSKU.SelectedValue) && DDL_BrandSKU.SelectedValue != "0")
        //            {
        //                cmd.Parameters.AddWithValue("@SkuId", DDL_BrandSKU.SelectedValue);
        //            }

        //            cmd.CommandType = CommandType.Text;

        //            using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
        //            {
        //                sda.Fill(dt);
        //            }
        //        }
        //    }

        //    return dt;
        //}


        private DataTable GetFilteredData()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            DataTable dt = new DataTable();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                // Base query with alias names
                string sqlQuery = @"
                    SELECT 
                        ID AS DBID,
                        LSP_Id AS RecordID,
                        FormID,
                        PlantName AS PlantName,
                        Line AS LineName,
                        ProductCategory AS ProductCategory,
                        ProductBrand AS ProductBrand,
                        SubmittedByEmployeeCode AS EmpCode,
                        SubmittedDate AS SDate,
                        SubmittedTime AS STime,
                        Shift AS SShift,
                        Approver1EmployeeCode AS L1,
                        Approver1_Status,
                        Approver1_TimeStamp,
                        Approver2EmployeeCode AS L2,
                        Approver2_Status,
                        Approver2_TimeStamp,
                        DottedLineApproverEmployeeCode AS L3,
                        DottedApprover_Status,
                        DottedApprover_TimeStamp
                    FROM TRN_LeakSealSlanted_Data 
                    WHERE ViewMode = 1";

                // Add filters to SQL query based on the selected values
                if (!string.IsNullOrEmpty(DDL_Plant.SelectedValue) && DDL_Plant.SelectedValue != "0")
                {
                    sqlQuery += " AND PlantName = @PlantName";
                }

                if (!string.IsNullOrEmpty(DDL_PlantLine.SelectedValue) && DDL_PlantLine.SelectedValue != "0")
                {
                    sqlQuery += " AND Line = @LineName";
                }

                if (!string.IsNullOrEmpty(DDL_ProductCategory.SelectedValue) && DDL_ProductCategory.SelectedValue != "0")
                {
                    sqlQuery += " AND ProductCategory = @ProductCategory";
                }

                if (!string.IsNullOrEmpty(DDL_ProductBrand.SelectedValue) && DDL_ProductBrand.SelectedValue != "0")
                {
                    sqlQuery += " AND ProductBrand = @ProductBrand";
                }

                sqlQuery += " ORDER BY SDate DESC, STime DESC";

                using (SqlCommand cmd = new SqlCommand(sqlQuery, con))
                {
                    // Add parameters only if they are being used in the query
                    if (!string.IsNullOrEmpty(DDL_Plant.SelectedValue) && DDL_Plant.SelectedValue != "0")
                    {
                        cmd.Parameters.AddWithValue("@PlantName", DDL_Plant.SelectedValue);
                    }

                    if (!string.IsNullOrEmpty(DDL_PlantLine.SelectedValue) && DDL_PlantLine.SelectedValue != "0")
                    {
                        cmd.Parameters.AddWithValue("@LineName", DDL_PlantLine.SelectedValue);
                    }

                    if (!string.IsNullOrEmpty(DDL_ProductCategory.SelectedValue) && DDL_ProductCategory.SelectedValue != "0")
                    {
                        cmd.Parameters.AddWithValue("@ProductCategory", DDL_ProductCategory.SelectedValue);
                    }

                    if (!string.IsNullOrEmpty(DDL_ProductBrand.SelectedValue) && DDL_ProductBrand.SelectedValue != "0")
                    {
                        cmd.Parameters.AddWithValue("@ProductBrand", DDL_ProductBrand.SelectedValue);
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
                    DBID = row["DBID"] == DBNull.Value ? 0 : Convert.ToInt32(row["DBID"]),
                    RecordID = row["RecordID"] == DBNull.Value ? string.Empty : row["RecordID"].ToString(),
                    FormID = row["FormID"] == DBNull.Value ? 0 : Convert.ToInt32(row["FormID"]),
                    PlantName = row["PlantName"] == DBNull.Value ? string.Empty : row["PlantName"].ToString(),
                    LineName = row["LineName"] == DBNull.Value ? string.Empty : row["LineName"].ToString(),
                    ProductCategory = row["ProductCategory"] == DBNull.Value ? string.Empty : row["ProductCategory"].ToString(),
                    ProductBrand = row["ProductBrand"] == DBNull.Value ? string.Empty : row["ProductBrand"].ToString(),
                    EmpCode = row["EmpCode"] == DBNull.Value ? string.Empty : row["EmpCode"].ToString(),
                    EmpName = row["EmpName"] == DBNull.Value ? string.Empty : row["EmpName"].ToString(),
                    SDate = row["SDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["SDate"]),
                    STime = row["STime"] == DBNull.Value ? TimeSpan.Zero : TimeSpan.Parse(row["STime"].ToString()),
                    SShift = row["SShift"] == DBNull.Value ? string.Empty : row["SShift"].ToString(),
                    Remarks = row["Remarks"] == DBNull.Value ? string.Empty : row["Remarks"].ToString(),
                    L1 = row["L1"] == DBNull.Value ? string.Empty : row["L1"].ToString(),
                    Approver1_Status = row["Approver1_Status"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["Approver1_Status"]),
                    Approver1_TimeStamp = row["Approver1_TimeStamp"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["Approver1_TimeStamp"]),
                    L2 = row["L2"] == DBNull.Value ? string.Empty : row["L2"].ToString(),
                    Approver2_Status = row["Approver2_Status"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["Approver2_Status"]),
                    Approver2_TimeStamp = row["Approver2_TimeStamp"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["Approver2_TimeStamp"]),
                    L3 = row["L3"] == DBNull.Value ? string.Empty : row["L3"].ToString(),
                    DottedApprover_Status = row["DottedApprover_Status"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["DottedApprover_Status"]),
                    DottedApprover_TimeStamp = row["DottedApprover_TimeStamp"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["DottedApprover_TimeStamp"])
                };

                dataSave.Add(info);
            }

            // Bind to GridView
            GridView1.DataSource = dataSave;
            GridView1.DataBind();
        }



        //private void BindGridView()
        //{
        //    // Fetch filtered data from the database
        //    DataTable dt = GetFilteredData();

        //    // Convert DataTable to List<ReportInfo>
        //    List<ReportInfo> dataSave = new List<ReportInfo>();

        //    foreach (DataRow row in dt.Rows)
        //    {
        //        ReportInfo info = new ReportInfo
        //        {
        //            DBID = row["DBID"] == DBNull.Value ? 0 : Convert.ToInt32(row["DBID"]),
        //            RecordID = row["RecordID"] == DBNull.Value ? string.Empty : row["RecordID"].ToString(),
        //            FormID = row["FormID"] == DBNull.Value ? 0 : Convert.ToInt32(row["FormID"]),
        //            SubmittedById = row["SubmittedById"] == DBNull.Value ? 0 : Convert.ToInt32(row["SubmittedById"]),
        //            SubmittedDate = row["SubmittedDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["SubmittedDate"]),
        //            SubmittedTime = row["SubmittedTime"] == DBNull.Value ? TimeSpan.Zero : TimeSpan.Parse(row["SubmittedTime"].ToString()),
        //            Shift = row["Shift"] == DBNull.Value ? string.Empty : row["Shift"].ToString(),
        //            SubmittedByEmployeeCode = row["SubmittedByEmployeeCode"] == DBNull.Value ? string.Empty : row["SubmittedByEmployeeCode"].ToString(),
        //            PlantName = row["PlantName"] == DBNull.Value ? string.Empty : row["PlantName"].ToString(),
        //            Line = row["Line"] == DBNull.Value ? string.Empty : row["Line"].ToString(),
        //            ProductCategory = row["ProductCategory"] == DBNull.Value ? string.Empty : row["ProductCategory"].ToString(),
        //            ProductBrand = row["ProductBrand"] == DBNull.Value ? string.Empty : row["ProductBrand"].ToString(),
        //            SKUId = row["SKUId"] == DBNull.Value ? string.Empty : row["SKUId"].ToString(),
        //            //Date = row["Date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["Date"]),
        //            //ViewMode = row["ViewMode"] == DBNull.Value ? 0 : Convert.ToInt32(row["ViewMode"]),
        //            //DeleteMode = row["DeleteMode"] == DBNull.Value ? 0 : Convert.ToInt32(row["DeleteMode"]),
        //            //Time = row["Time"] == DBNull.Value ? (TimeSpan?)null : TimeSpan.Parse(row["Time"].ToString()),
        //            Packing_MC_No = row["Packing_MC_No"] == DBNull.Value ? string.Empty : row["Packing_MC_No"].ToString(),
        //            LeakTestStatus = row["LeakTestStatus"] == DBNull.Value ? (bool?)null : Convert.ToBoolean(row["LeakTestStatus"]),
        //            RemarksForFail = row["RemarksForFail"] == DBNull.Value ? string.Empty : row["RemarksForFail"].ToString(),
        //            Slanted_Percent = row["Slanted_Percent"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(row["Slanted_Percent"]),
        //            Approver1EmployeeCode = row["Approver1EmployeeCode"] == DBNull.Value ? string.Empty : row["Approver1EmployeeCode"].ToString(),
        //            Approver1_Status = row["Approver1_Status"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["Approver1_Status"]),
        //            Approver1_TimeStamp = row["Approver1_TimeStamp"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["Approver1_TimeStamp"]),
        //            Approver2EmployeeCode = row["Approver2EmployeeCode"] == DBNull.Value ? string.Empty : row["Approver2EmployeeCode"].ToString(),
        //            Approver2_Status = row["Approver2_Status"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["Approver2_Status"]),
        //            Approver2_TimeStamp = row["Approver2_TimeStamp"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["Approver2_TimeStamp"]),
        //            DottedLineApproverEmployeeCode = row["DottedLineApproverEmployeeCode"] == DBNull.Value ? string.Empty : row["DottedLineApproverEmployeeCode"].ToString(),
        //            DottedApprover_Status = row["DottedApprover_Status"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["DottedApprover_Status"]),
        //            DottedApprover_TimeStamp = row["DottedApprover_TimeStamp"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["DottedApprover_TimeStamp"])
        //        };

        //        dataSave.Add(info);
        //    }

        //    // Bind to GridView
        //    GridView1.DataSource = dataSave;
        //    GridView1.DataBind();
        //}


        //private void BindGridView()
        //{
        //    // Fetch filtered data from the database
        //    DataTable dt = GetFilteredLeakTestData();

        //    // Bind to GridView
        //    GridView1.DataSource = dt;
        //    GridView1.DataBind();
        //}
        //protected void btnFilter_Click(object sender, EventArgs e)
        //{
        //    BindGridView();
        //}



        private void loadAlldata()
        {
            try
            {
                //string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                //string query = @"
                //    SELECT TOP(30)
                //        c.ID AS DBID,
                //        c.FormID as FormID,
                //        c.LSP_Id as RecordID,
                //        p.plant_name AS PlantName,
                //        l.line_name AS LineName,
                //     pc.category_name AS ProductCategory,
                //     pb.brand_name AS ProductBrand,
                //        c.SubmittedByEmployeeCode as EmpCode,
                //        u.EmployeeName AS EmpName,
                //        c.SubmittedDate as SDate,
                //        c.SubmittedTime as STime,
                //        c.Shift as SShift,
                //        'No Comment' as Remarks,
                //        c.Approver1EmployeeCode as L1,
                //        c.Approver1_Status,
                //        c.Approver1_TimeStamp,
                //        c.Approver2EmployeeCode as L2,
                //        c.Approver2_Status,
                //        c.Approver2_TimeStamp,
                //        c.DottedLineApproverEmployeeCode as L3,
                //        c.DottedApprover_Status,
                //        c.DottedApprover_TimeStamp
                //    FROM 
                //        TRN_LeakSealSlanted_Data c
                //    LEFT JOIN 
                //        MST_PlantDetails p ON c.PlantName = p.plant_id
                //    LEFT JOIN 
                //        MST_Plant_Lines l ON c.Line = l.line_id
                //    LEFT JOIN 
                //        MST_LineCategory pc ON c.ProductCategory = pc.category_id
                //    LEFT JOIN 
                //        MST_LineCatBrands pb ON c.ProductBrand = pb.brand_id
                //    LEFT JOIN
                //        MST_UserMaster u ON c.SubmittedByEmployeeCode = u.EmployeeCode
                //    WHERE 
                //        1 = 1
                //    ORDER BY 
                //        c.[SubmittedDate] DESC, 
                //        c.[SubmittedTime] DESC;
                //";

                string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

                string query = @"
                    WITH RankedData AS (
                        SELECT 
                            c.ID AS DBID,
                            c.FormID AS FormID,
                            c.BatchID,
                            c.LSP_Id AS RecordID,
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
                            c.DottedApprover_TimeStamp,
                            ROW_NUMBER() OVER (
                                PARTITION BY c.BatchID 
                                ORDER BY c.SubmittedDate DESC, c.SubmittedTime DESC
                            ) AS RowNum
                        FROM 
                            TRN_LeakSealSlanted_Data c
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
                    )
                    SELECT TOP(30)
                        DBID, FormID, BatchID, RecordID, PlantName, LineName, ProductCategory, 
                        ProductBrand, EmpCode, EmpName, SDate, STime, SShift, Remarks, 
                        L1, Approver1_Status, Approver1_TimeStamp, 
                        L2, Approver2_Status, Approver2_TimeStamp, 
                        L3, DottedApprover_Status, DottedApprover_TimeStamp
                    FROM 
                        RankedData
                    WHERE 
                        RowNum = 1
                    ORDER BY 
                        SDate DESC, STime DESC;
                ";


                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        SqlDataAdapter sda = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        sda.Fill(dt);

                        //// Parse the MetalCheck column containing JSON
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

        protected void ExportBtn_Click(object sender, EventArgs e)
        {
            try
            {
                // Fetch the data that needs to be exported
                DataTable exportData = GetLeakTestDataForExport(); // Implement this function to retrieve data from DB

                if (exportData != null && exportData.Rows.Count > 0)
                {
                    // Set the response settings
                    Response.Clear();
                    Response.Buffer = true;
                    Response.AddHeader("content-disposition", "attachment;filename=LeakTestData.xls");
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
                    ShowNotification("No Data", "No data available to export.", "info");
                }
            }
            catch (Exception ex)
            {
                ShowNotification("Error", $"An error occurred while exporting: {ex.Message}", "error");
            }
        }

        public override void VerifyRenderingInServerForm(Control control)
        {
            // Confirms that an HtmlForm control is rendered for the specified ASP.NET
        }

        private DataTable GetLeakTestDataForExport()
        {
            // Connection string to your database
            string connString = System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            // Query to fetch data from the Leak_Test_Data table
            string query = "SELECT * FROM Leak_Test_Data"; // Adjust this query as per your table structure

            using (SqlConnection conn = new SqlConnection(connString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        // Optional: Implement a basic notification function (This can be customized or removed)
        private void ShowNotification(string title, string message, string type)
        {
            // You can implement a real notification system here or log to the console
            Console.WriteLine($"{title}: {message} [{type}]");
        }

        //protected void btn_view_submit_Click(object sender, EventArgs e)
        //{
        //    // Retrieve date range from textboxes
        //    DateTime? dateFrom = string.IsNullOrEmpty(TXT_PackageDate.Text) ? (DateTime?)null : DateTime.ParseExact(TXT_PackageDate.Text, "yyyy-MM-dd", null);
        //    DateTime? dateTo = string.IsNullOrEmpty(TextBox1.Text) ? (DateTime?)null : DateTime.ParseExact(TextBox1.Text, "yyyy-MM-dd", null);

        //    // Build SQL query based on date range
        //    StringBuilder queryBuilder = new StringBuilder("SELECT * FROM [dbo].[Leak_Test_Data] WHERE 1 = 1");
        //    var parameters = new List<SqlParameter>();

        //    if (dateFrom.HasValue)
        //    {
        //        queryBuilder.Append(" AND SubmittedDate >= @DateFrom");
        //        parameters.Add(new SqlParameter("@DateFrom", SqlDbType.Date) { Value = dateFrom.Value.Date });
        //    }

        //    if (dateTo.HasValue)
        //    {
        //        queryBuilder.Append(" AND SubmittedDate <= @DateTo");
        //        parameters.Add(new SqlParameter("@DateTo", SqlDbType.Date) { Value = dateTo.Value.Date });
        //    }

        //    try
        //    {
        //        // Fetch filtered data using the constructed query and parameters
        //        DataTable filteredData = GetDataFromTable(queryBuilder.ToString(), parameters.ToArray());

        //        // Convert DataTable to List<ReportInfo>
        //        List<ReportInfo> dataSave = new List<ReportInfo>();
        //        foreach (DataRow row in filteredData.Rows)
        //        {
        //            ReportInfo info = new ReportInfo
        //            {
        //                Id = row["Id"] == DBNull.Value ? 0 : Convert.ToInt32(row["Id"]),
        //                LSP_Id = row["LSP_Id"] == DBNull.Value ? string.Empty : row["LSP_Id"].ToString(),
        //                FormID = row["FormID"] == DBNull.Value ? 0 : Convert.ToInt32(row["FormID"]),
        //                SubmittedById = row["SubmittedById"] == DBNull.Value ? 0 : Convert.ToInt32(row["SubmittedById"]),
        //                SubmittedDate = row["SubmittedDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["SubmittedDate"]),
        //                SubmittedTime = row["SubmittedTime"] == DBNull.Value ? TimeSpan.Zero : TimeSpan.Parse(row["SubmittedTime"].ToString()),
        //                Shift = row["Shift"] == DBNull.Value ? string.Empty : row["Shift"].ToString(),
        //                SubmittedByEmployeeCode = row["SubmittedByEmployeeCode"] == DBNull.Value ? string.Empty : row["SubmittedByEmployeeCode"].ToString(),
        //                PlantName = row["PlantName"] == DBNull.Value ? string.Empty : row["PlantName"].ToString(),
        //                Line = row["Line"] == DBNull.Value ? string.Empty : row["Line"].ToString(),
        //                ProductCategory = row["ProductCategory"] == DBNull.Value ? string.Empty : row["ProductCategory"].ToString(),
        //                ProductBrand = row["ProductBrand"] == DBNull.Value ? string.Empty : row["ProductBrand"].ToString(),
        //                SKUId = row["SKUId"] == DBNull.Value ? string.Empty : row["SKUId"].ToString(),
        //                Date = row["Date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["Date"]),
        //                ViewMode = row["ViewMode"] == DBNull.Value ? 0 : Convert.ToInt32(row["ViewMode"]),
        //                DeleteMode = row["DeleteMode"] == DBNull.Value ? 0 : Convert.ToInt32(row["DeleteMode"]),
        //                Time = row["Time"] == DBNull.Value ? (TimeSpan?)null : TimeSpan.Parse(row["Time"].ToString()),
        //                Packing_MC_No = row["Packing_MC_No"] == DBNull.Value ? string.Empty : row["Packing_MC_No"].ToString(),
        //                LeakTestStatus = row["LeakTestStatus"] == DBNull.Value ? (bool?)null : Convert.ToBoolean(row["LeakTestStatus"]),
        //                RemarksForFail = row["RemarksForFail"] == DBNull.Value ? string.Empty : row["RemarksForFail"].ToString(),
        //                Slanted_Percent = row["Slanted_Percent"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(row["Slanted_Percent"]),
        //                Approver1EmployeeCode = row["Approver1EmployeeCode"] == DBNull.Value ? string.Empty : row["Approver1EmployeeCode"].ToString(),
        //                Approver1_Status = row["Approver1_Status"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["Approver1_Status"]),
        //                Approver1_TimeStamp = row["Approver1_TimeStamp"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["Approver1_TimeStamp"]),
        //                Approver2EmployeeCode = row["Approver2EmployeeCode"] == DBNull.Value ? string.Empty : row["Approver2EmployeeCode"].ToString(),
        //                Approver2_Status = row["Approver2_Status"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["Approver2_Status"]),
        //                Approver2_TimeStamp = row["Approver2_TimeStamp"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["Approver2_TimeStamp"]),
        //                DottedLineApproverEmployeeCode = row["DottedLineApproverEmployeeCode"] == DBNull.Value ? string.Empty : row["DottedLineApproverEmployeeCode"].ToString(),
        //                DottedApprover_Status = row["DottedApprover_Status"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["DottedApprover_Status"]),
        //                DottedApprover_TimeStamp = row["DottedApprover_TimeStamp"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["DottedApprover_TimeStamp"])
        //            };

        //            dataSave.Add(info);
        //        }

        //        // Bind to GridView
        //        GridView1.DataSource = dataSave;
        //        GridView1.DataBind();

        //        // Show a notification if no records are found
        //        if (filteredData.Rows.Count == 0)
        //        {
        //            // ShowNotification("No Data", "No records found for the selected date range.", "info");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Handle exceptions (log it, show an error message, etc.)
        //        // ShowNotification("Error", "An error occurred while processing your request.", "error");
        //    }
        //}

        protected void btn_view_submit_Click_old(object sender, EventArgs e)
        {
            // Retrieve date range from textboxes
            DateTime? dateFrom = string.IsNullOrEmpty(TXT_PackageDate.Text)
                ? (DateTime?)null
                : DateTime.ParseExact(TXT_PackageDate.Text, "yyyy-MM-dd", null);
            DateTime? dateTo = string.IsNullOrEmpty(TextBox1.Text)
                ? (DateTime?)null
                : DateTime.ParseExact(TextBox1.Text, "yyyy-MM-dd", null);

            // Build SQL query dynamically based on date range
            StringBuilder queryBuilder = new StringBuilder(@"
                SELECT TOP(30)
                    c.ID AS DBID,
                    c.FormID AS FormID,
                    c.LSP_Id AS RecordID,
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
                FROM TRN_LeakSealSlanted_Data c
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

            // Filter by selected plant
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
                // Fetch filtered data using the constructed query and parameters
                DataTable filteredData = GetDataFromTable(queryBuilder.ToString(), parameters.ToArray());

                // Convert DataTable to List<ReportInfo>
                List<ReportInfo> dataSave = filteredData.AsEnumerable().Select(row => new ReportInfo
                {
                    DBID = row.Field<int>("DBID"),
                    FormID = row.Field<int>("FormID"),
                    RecordID = row.Field<string>("RecordID"),
                    PlantName = row.Field<string>("PlantName"),
                    LineName = row.Field<string>("LineName"),
                    ProductCategory = row.Field<string>("ProductCategory"),
                    ProductBrand = row.Field<string>("ProductBrand"),
                    EmpCode = row.Field<string>("EmpCode"),
                    EmpName = row.Field<string>("EmpName"),
                    SDate = row.Field<DateTime>("SDate"),
                    STime = row.Field<TimeSpan>("STime"),
                    SShift = row.Field<string>("SShift"),
                    Remarks = row.Field<string>("Remarks"),
                    L1 = row.Field<string>("L1"),
                    Approver1_Status = row.Field<int?>("Approver1_Status"),
                    Approver1_TimeStamp = row.Field<DateTime?>("Approver1_TimeStamp"),
                    L2 = row.Field<string>("L2"),
                    Approver2_Status = row.Field<int?>("Approver2_Status"),
                    Approver2_TimeStamp = row.Field<DateTime?>("Approver2_TimeStamp"),
                    L3 = row.Field<string>("L3"),
                    DottedApprover_Status = row.Field<int?>("DottedApprover_Status"),
                    DottedApprover_TimeStamp = row.Field<DateTime?>("DottedApprover_TimeStamp")
                }).ToList();

                // Bind to GridView
                GridView1.DataSource = dataSave;
                GridView1.DataBind();

                // Show a notification if no records are found
                if (dataSave.Count == 0)
                {
                    // ShowNotification("No Data", "No records found for the selected date range.", "info");
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (log it, show an error message, etc.)
                // ShowNotification("Error", "An error occurred while processing your request.", "error");
            }
        }

        protected void btn_view_submit_Click(object sender, EventArgs e)
        {
            // Retrieve date range from textboxes
            DateTime? dateFrom = string.IsNullOrEmpty(TXT_PackageDate.Text)
                ? (DateTime?)null
                : DateTime.ParseExact(TXT_PackageDate.Text, "yyyy-MM-dd", null);
            DateTime? dateTo = string.IsNullOrEmpty(TextBox1.Text)
                ? (DateTime?)null
                : DateTime.ParseExact(TextBox1.Text, "yyyy-MM-dd", null);

            // Build SQL query dynamically based on date range
            StringBuilder queryBuilder = new StringBuilder(@"
                WITH RankedData AS (
                    SELECT 
                        c.ID AS DBID,
                        c.FormID AS FormID,
                        c.BatchID,
                        c.LSP_Id AS RecordID,
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
                        c.DottedApprover_TimeStamp,
                        ROW_NUMBER() OVER (
                            PARTITION BY c.BatchID 
                            ORDER BY c.SubmittedDate DESC, c.SubmittedTime DESC
                        ) AS RowNum
                    FROM TRN_LeakSealSlanted_Data c
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

            // Filter by selected plant
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

            queryBuilder.Append(@")
                SELECT TOP(30)
                    DBID, FormID, BatchID, RecordID, PlantName, LineName, ProductCategory, 
                    ProductBrand, EmpCode, EmpName, SDate, STime, SShift, Remarks, 
                    L1, Approver1_Status, Approver1_TimeStamp, 
                    L2, Approver2_Status, Approver2_TimeStamp, 
                    L3, DottedApprover_Status, DottedApprover_TimeStamp
                FROM RankedData
                WHERE RowNum = 1
                ORDER BY SDate DESC, STime DESC");

            try
            {
                // Fetch filtered data using the constructed query and parameters
                DataTable filteredData = GetDataFromTable(queryBuilder.ToString(), parameters.ToArray());

                // Convert DataTable to List<ReportInfo>
                List<ReportInfo> dataSave = filteredData.AsEnumerable().Select(row => new ReportInfo
                {
                    DBID = row.Field<int>("DBID"),
                    FormID = row.Field<int>("FormID"),
                    RecordID = row.Field<string>("RecordID"),
                    PlantName = row.Field<string>("PlantName"),
                    LineName = row.Field<string>("LineName"),
                    ProductCategory = row.Field<string>("ProductCategory"),
                    ProductBrand = row.Field<string>("ProductBrand"),
                    EmpCode = row.Field<string>("EmpCode"),
                    EmpName = row.Field<string>("EmpName"),
                    SDate = row.Field<DateTime>("SDate"),
                    STime = row.Field<TimeSpan>("STime"),
                    SShift = row.Field<string>("SShift"),
                    Remarks = row.Field<string>("Remarks"),
                    L1 = row.Field<string>("L1"),
                    Approver1_Status = row.Field<int?>("Approver1_Status"),
                    Approver1_TimeStamp = row.Field<DateTime?>("Approver1_TimeStamp"),
                    L2 = row.Field<string>("L2"),
                    Approver2_Status = row.Field<int?>("Approver2_Status"),
                    Approver2_TimeStamp = row.Field<DateTime?>("Approver2_TimeStamp"),
                    L3 = row.Field<string>("L3"),
                    DottedApprover_Status = row.Field<int?>("DottedApprover_Status"),
                    DottedApprover_TimeStamp = row.Field<DateTime?>("DottedApprover_TimeStamp")
                }).ToList();

                // Bind to GridView
                GridView1.DataSource = dataSave;
                GridView1.DataBind();

                // Show a notification if no records are found
                if (dataSave.Count == 0)
                {
                    // ShowNotification("No Data", "No records found for the selected date range.", "info");
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (log it, show an error message, etc.)
                // ShowNotification("Error", "An error occurred while processing your request.", "error");
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

        protected void btn_view_Cancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("home.aspx");
        }

        protected void btn_view_Reset_Click(object sender, EventArgs e)
        {
            Response.Redirect("vm_leak_test.aspx");
        }

        protected void GridView1_RowCommand(object sender, System.Web.UI.WebControls.GridViewCommandEventArgs e)
        {
            int rowIndex = Convert.ToInt32(e.CommandArgument);
            GridViewRow row = GridView1.Rows[rowIndex];
            string dbid = (row.FindControl("lbl_rowid") as Label).Text;
            string batchid = (row.FindControl("lbl_batchid") as Label).Text;
            if (e.CommandName == "View")
            {
                Response.Redirect("LeakAndSlant_Detailed.aspx?ID=" + batchid + "&VM=1", false);
                //Response.Redirect("#", false);
            }
        }
    }
}
