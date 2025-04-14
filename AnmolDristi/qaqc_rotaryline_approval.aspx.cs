using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using System.IO;

namespace AnmolDristi
{
    public partial class qaqc_rotaryline_approval : System.Web.UI.Page
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

                    lbl_upper.Text = "Search Filters for Rotary Line Report";
                    lbl_lower.Text = "View and Select for Detailed View  ||   ";

                    PlantBinder();
                    BindGridView();
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
                FilterGridView();
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
                FilterGridView();
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
                FilterGridView();
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
                FilterGridView();
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



        protected void btn_view_cancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("home.aspx");
        }

        protected void btn_view_reset_Click(object sender, EventArgs e)
        {
            Response.Redirect("qaqc_rotaryline_approval.aspx");
        }


        public class ReportInfo
        {
            public int Id { get; set; }
            public string plant_name { get; set; }
            public string line_name { get; set; }
            public string category_name { get; set; }
            public string brand_name { get; set; }
            public string RLWt { get; set; }
            public DateTime? SubmittedDate { get; set; }  // Renamed to match the database column
            public TimeSpan? SubmittedTime { get; set; }  // Renamed to match the database column
            public string Shift { get; set; }
            public string SubmittedById { get; set; }
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


        protected void btn_view_submit_Click(object sender, EventArgs e)
        {
            try
            {
                // Convert dates from TextBox inputs
                DateTime fromDate = Convert.ToDateTime(TxtDateFrom.Text);
                DateTime toDate = Convert.ToDateTime(TxtDateTo.Text);

                // Validate date range
                if (toDate > DateTime.Now)
                {
                    Response.Write("<script>alert('ToDate cannot be greater than the current date!');</script>");
                    return;
                }
                else if (fromDate > toDate)
                {
                    Response.Write("<script>alert('FromDate cannot be greater than ToDate!');</script>");
                    return;
                }

                // Call method to fetch and bind data
                getReportData(fromDate, toDate);
            }
            catch (FormatException)
            {
                Response.Write("<script>alert('Invalid date format! Please enter a valid date.');</script>");
            }
            catch (Exception ex)
            {
                Response.Write($"<script>alert('An error occurred: {ex.Message}');</script>");
            }
        }

        private void getReportData(DateTime fromDate, DateTime toDate)
        {
            string query = @"
                SELECT
                    A.plant_name,
                    B.line_name,
                    C.category_name,
                    D.brand_name,
                    P.*
                FROM
                    TRN_RotaryLine_OvenEnd P
                JOIN dbo.MST_PlantDetails A ON P.PlantName = A.plant_id
                JOIN dbo.MST_Plant_Lines B ON P.Line = B.line_id
                JOIN dbo.MST_LineCategory C ON P.ProductCategory = C.category_id
                JOIN dbo.MST_LineCatBrands D ON P.ProductBrand = D.brand_id
                WHERE SubmittedDate BETWEEN @FromDate AND @ToDate";

            // Add conditions for optional filters
            if (!string.IsNullOrEmpty(DDL_Plant.SelectedValue))
            {
                query += " AND P.PlantName = @PlantName";
            }
            if (!string.IsNullOrEmpty(DDL_PlantLine.SelectedValue))
            {
                query += " AND P.Line = @Line";
            }
            if (!string.IsNullOrEmpty(DDL_ProductCategory.SelectedValue))
            {
                query += " AND P.ProductCategory = @CategoryName";
            }
            if (!string.IsNullOrEmpty(DDL_ProductBrand.SelectedValue))
            {
                query += " AND P.ProductBrand = @BrandName";
            }

            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.CommandType = CommandType.Text;

                    // Add common parameters
                    cmd.Parameters.AddWithValue("@FromDate", fromDate);
                    cmd.Parameters.AddWithValue("@ToDate", toDate);

                    // Add filter parameters only if DropDownList values are selected
                    if (!string.IsNullOrEmpty(DDL_Plant.SelectedValue))
                    {
                        cmd.Parameters.AddWithValue("@PlantName", DDL_Plant.SelectedValue);
                    }
                    if (!string.IsNullOrEmpty(DDL_PlantLine.SelectedValue))
                    {
                        cmd.Parameters.AddWithValue("@Line", DDL_PlantLine.SelectedValue);
                    }
                    if (!string.IsNullOrEmpty(DDL_ProductCategory.SelectedValue))
                    {
                        cmd.Parameters.AddWithValue("@CategoryName", DDL_ProductCategory.SelectedValue);
                    }
                    if (!string.IsNullOrEmpty(DDL_ProductBrand.SelectedValue))
                    {
                        cmd.Parameters.AddWithValue("@BrandName", DDL_ProductBrand.SelectedValue);
                    }

                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        System.Data.DataTable dt = new System.Data.DataTable();
                        sda.Fill(dt);

                        if (dt.Rows.Count > 0)
                        {
                            GridView1.DataSource = dt;
                            GridView1.DataBind();
                        }
                        else
                        {
                            Response.Write("<script>alert('No records found for the selected filters and date range.');</script>");
                            GridView1.DataSource = null;
                            GridView1.DataBind();
                        }
                    }
                }
            }
        }


        protected void BindGridView()
        {
            try
            {
                // Define query
                string query = @"
                SELECT top(30)
                    A.plant_name,
                    B.line_name,
                    C.category_name,
                    D.brand_name,
                    P.Id as DBID,
                    P.*
                FROM
                    TRN_RotaryLine_OvenEnd P
                JOIN dbo.MST_PlantDetails A ON P.PlantName = A.plant_id
                JOIN dbo.MST_Plant_Lines B ON P.Line = B.line_id
                JOIN dbo.MST_LineCategory C ON P.ProductCategory = C.category_id
                JOIN dbo.MST_LineCatBrands D ON P.ProductBrand = D.brand_id order by P.SubmittedDate desc";

                string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.CommandType = CommandType.Text;

                        using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                        {
                            System.Data.DataTable dt = new System.Data.DataTable();
                            sda.Fill(dt);

                            if (dt.Rows.Count > 0)
                            {
                                // Bind data to GridView
                                GridView1.DataSource = dt;
                                GridView1.DataBind();
                            }
                            else
                            {
                                // Handle empty result
                                GridView1.DataSource = null;
                                GridView1.DataBind();
                                Response.Write("<script>alert('No data found.');</script>");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Response.Write($"<script>alert('An error occurred: {ex.Message}');</script>");
            }
        }

        protected void FilterGridView()
        {
            try
            {
                // Establish database connection
                string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    // Define query with filters
                    string query = @"
                    SELECT
                        A.plant_name,
                        B.line_name,
                        C.category_name,
                        D.brand_name,
                        P.Id as DBID,
                        P.*
                    FROM
                        TRN_RotaryLine_OvenEnd P
                    JOIN dbo.MST_PlantDetails A ON P.PlantName = A.plant_id
                    JOIN dbo.MST_Plant_Lines B ON P.Line = B.line_id
                    JOIN dbo.MST_LineCategory C ON P.ProductCategory = C.category_id
                    JOIN dbo.MST_LineCatBrands D ON P.ProductBrand = D.brand_id
                    WHERE
                        (@Plant_Name IS NULL OR P.PlantName = @Plant_Name) AND
                        (@Plant_Line IS NULL OR P.Line = @Plant_Line) AND
                        (@Product_Category IS NULL OR P.ProductCategory = @Product_Category) AND
                        (@Product_Brand IS NULL OR P.ProductBrand = @Product_Brand) order by P.SubmittedDate desc";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        // Add parameters for filtering
                        cmd.Parameters.AddWithValue("@Plant_Name", string.IsNullOrEmpty(DDL_Plant.SelectedValue) || DDL_Plant.SelectedValue == "0" ? (object)DBNull.Value : Convert.ToInt32(DDL_Plant.SelectedValue));
                        cmd.Parameters.AddWithValue("@Plant_Line", string.IsNullOrEmpty(DDL_PlantLine.SelectedValue) || DDL_PlantLine.SelectedValue == "0" ? (object)DBNull.Value : Convert.ToInt32(DDL_PlantLine.SelectedValue));
                        cmd.Parameters.AddWithValue("@Product_Category", string.IsNullOrEmpty(DDL_ProductCategory.SelectedValue) || DDL_ProductCategory.SelectedValue == "0" ? (object)DBNull.Value : Convert.ToInt32(DDL_ProductCategory.SelectedValue));
                        cmd.Parameters.AddWithValue("@Product_Brand", string.IsNullOrEmpty(DDL_ProductBrand.SelectedValue) || DDL_ProductBrand.SelectedValue == "0" ? (object)DBNull.Value : Convert.ToInt32(DDL_ProductBrand.SelectedValue));

                        cmd.CommandType = CommandType.Text;

                        using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                        {
                            System.Data.DataTable dt = new System.Data.DataTable();
                            sda.Fill(dt);

                            if (dt.Rows.Count > 0)
                            {
                                // Bind filtered data to GridView
                                GridView1.DataSource = dt;
                                GridView1.DataBind();
                            }
                            else
                            {
                                // Handle empty result
                                GridView1.DataSource = null;
                                GridView1.DataBind();
                                Response.Write("<script>alert('No data found for the selected filters.');</script>");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Response.Write($"<script>alert('An error occurred: {ex.Message}');</script>");
            }
        }


        protected void ApproveBtn_Click(object sender, EventArgs e)
        {
            // Get the ID from the CommandArgument of the button
            //System.Web.UI.WebControls.Button btn = (System.Web.UI.WebControls.Button)sender;
            //string rlwt = btn.CommandArgument;

            //Response.Redirect("qaqc_rotaryline_detailview.aspx?RLWt=" + rlwt);
        }


        protected void GridView1_RowCommand(object sender, System.Web.UI.WebControls.GridViewCommandEventArgs e)
        {
            int rowIndex = Convert.ToInt32(e.CommandArgument);
            GridViewRow row = GridView1.Rows[rowIndex];
            string dbid = (row.FindControl("lbl_rowid") as Label).Text;
            if (e.CommandName == "View")
            {
                Response.Redirect("qaqc_rotaryline_detailview.aspx?ID=" + dbid + "&VM=1", false);
            }
        }
    }
}