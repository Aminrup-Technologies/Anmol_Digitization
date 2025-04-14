using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;

namespace AnmolDristi
{
    public partial class Critical_Incident_ViewPage : System.Web.UI.Page
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
                lbl_DDL_Plant_Value.Text = selectedPlantValue;
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
                lbl_DDL_PlantLine_Value.Text = selectedPlantLineValue;

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
                lbl_DDL_ProductCategory_Value.Text = selectedProductCategoryValue;

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
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                string selectedPlantLineValue = DDL_PlantLine.SelectedValue.ToString();
                string selectedProductCategoryValue = DDL_ProductCategory.SelectedValue.ToString();
                string selectedProductBrandValue = DDL_ProductBrand.SelectedValue.ToString();
                BrandSKUBinder(selectedProductBrandValue);
                lbl_DDL_ProductBrand_Value.Text = selectedProductBrandValue;
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

                string BrandSKUBinder_Error_script6 = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'An error occurred!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowBrandSKUBinderErrorNotification6", BrandSKUBinder_Error_script6, false);
            }
            DataLoader();
        }
        protected void ReportbtnCancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("home.aspx");
        }
        protected void ReportbtnReset_Click(object sender, EventArgs e)
        {
            Response.Redirect("critical_quality_report.aspx");
        }
        protected void btn_submit_Click(object sender, EventArgs e)
        {
            DateTime fromDate = Convert.ToDateTime(TB_Date_From.Text);
            DateTime toDate = Convert.ToDateTime(TB_Date_To.Text);

            string selectedPlantValue = DDL_Plant.SelectedValue.ToString();

            if (DDL_Plant.SelectedIndex != 0)
            {
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
        }
        public class ReportInfo
        {
            public string PlantName { get; set; }
            public string Line { get; set; }
            public string ProductCategory { get; set; }
            public string ProductBrand { get; set; }
            public string SKUId { get; set; }
            public string SubmittedDate { get; set; }
            public string SubmittedTime { get; set; }
            public string SubmittedById { get; set; }
            public string QIDetails { get; set; }
            public string RjtdQty { get; set; }
            public string WhenObserved { get; set; }
            public string TgtDtOfComp { get; set; }
            public string ImmediateAction { get; set; }
            public string CorrectiveAction { get; set; }
            public string QCI_EmpCode { get; set; }
            public string SftInCharge_EmpCode { get; set; }
            public string QAQCInCharge { get; set; }
            public string Responsibility_EmpCode { get; set; }
            public string DispatchAppRB { get; set; }
            public string DispatchApp { get; set; }
        }
        private void BindGridView()
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string query = @"SELECT * FROM TRN_Critical_Incident;";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
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
            catch (Exception ex)
            {
                // Log or display the exception
                Response.Write(ex.Message);
            }
        }
        protected void FilterData()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                //string query = @"SELECT * FROM TRN_Critical_Incident WHERE plant_id = @SelectedPlantValue;";
                string query = @"SELECT * FROM TRN_Critical_Incident WHERE PlantName = @selectedPlantValue";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {

                    cmd.Parameters.AddWithValue("@selectedPlantValue", string.IsNullOrEmpty(DDL_Plant.SelectedValue) || DDL_Plant.SelectedValue == "0" ? (object)DBNull.Value : Convert.ToInt32(DDL_Plant.SelectedValue));

                    cmd.Parameters.AddWithValue("@Line", string.IsNullOrEmpty(DDL_PlantLine.SelectedValue) || DDL_PlantLine.SelectedValue == "0" ? (object)DBNull.Value : Convert.ToInt32(DDL_PlantLine.SelectedValue));

                    cmd.Parameters.AddWithValue("@ProductCategory", string.IsNullOrEmpty(DDL_ProductCategory.SelectedValue) || DDL_ProductCategory.SelectedValue == "0" ? (object)DBNull.Value : Convert.ToInt32(DDL_ProductCategory.SelectedValue));

                    cmd.Parameters.AddWithValue("@ProductBrand", string.IsNullOrEmpty(DDL_ProductBrand.SelectedValue) || DDL_ProductBrand.SelectedValue == "0" ? (object)DBNull.Value : Convert.ToInt32(DDL_ProductBrand.SelectedValue));

                    cmd.Parameters.AddWithValue("@SKUId", string.IsNullOrEmpty(DDL_BrandSKU.SelectedValue) || DDL_BrandSKU.SelectedValue == "0" ? (object)DBNull.Value : DDL_BrandSKU.SelectedValue);

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

        private void loadAlldata()
        {
            try
            {
                string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

                string query = @"
                    SELECT TOP(30)
                        c.ID AS DBID,
                        c.FormID as FormID,
                        c.CIRId as RecordID,
                        p.plant_name AS PlantName,
                        l.line_name AS LineName,
	                    pc.category_name AS ProductCategory,
	                    pb.brand_name AS ProductBrand,
                        c.SubmittedByPNo as EmpCode,
                        u.EmployeeName AS EmpName,
                        c.SubmittedDate as SDate,
                        c.SubmittedTime as STime,
                        c.Shift as SShift,
                        c.QIDetails as Remarks,
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
                        TRN_Critical_Incident c
                    LEFT JOIN 
                        MST_PlantDetails p ON c.PlantName = p.plant_id
                    LEFT JOIN 
                        MST_Plant_Lines l ON c.Line = l.line_id
                    LEFT JOIN 
                        MST_LineCategory pc ON c.ProductCategory = pc.category_id
                    LEFT JOIN 
                        MST_LineCatBrands pb ON c.ProductBrand = pb.brand_id
                    LEFT JOIN
                        MST_UserMaster u ON c.SubmittedByPNo = u.EmployeeCode
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
            DateTime? dateFrom = string.IsNullOrEmpty(TB_Date_From.Text) ? (DateTime?)null : DateTime.ParseExact(TB_Date_From.Text, "yyyy-MM-dd", null);
            DateTime? dateTo = string.IsNullOrEmpty(TB_Date_To.Text) ? (DateTime?)null : DateTime.ParseExact(TB_Date_To.Text, "yyyy-MM-dd", null);

            StringBuilder queryBuilder = new StringBuilder(@"
                SELECT 
                    c.ID AS DBID,
                    c.FormID AS FormID,
                    c.CIRId as RecordID,
                    p.plant_name AS PlantName,
                    l.line_name AS LineName,
                    pc.category_name AS ProductCategory,
                    pb.brand_name AS ProductBrand,
                    c.SubmittedByPNo AS EmpCode,
                    u.EmployeeName AS EmpName,
                    c.SubmittedDate AS SDate,
                    c.SubmittedTime AS STime,
                    c.Shift AS SShift,
                    c.QIDetails AS Remarks,
                    c.Approver1EmployeeCode AS L1,
                    c.Approver1_Status,
                    c.Approver1_TimeStamp,
                    c.Approver2EmployeeCode AS L2,
                    c.Approver2_Status,
                    c.Approver2_TimeStamp,
                    c.DottedLineApproverEmployeeCode AS L3,
                    c.DottedApprover_Status,
                    c.DottedApprover_TimeStamp
                FROM TRN_Critical_Incident c
                LEFT JOIN MST_PlantDetails p ON c.PlantName = p.plant_id
                LEFT JOIN MST_Plant_Lines l ON c.Line = l.line_id
                LEFT JOIN MST_LineCategory pc ON c.ProductCategory = pc.category_id
                LEFT JOIN MST_LineCatBrands pb ON c.ProductBrand = pb.brand_id
                LEFT JOIN MST_UserMaster u ON c.SubmittedByPNo = u.EmployeeCode
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
                Response.Redirect("Critical_Incident_Detailed.aspx?ID=" + dbid + "&VM=0", false);
            }
        }
    }
}



