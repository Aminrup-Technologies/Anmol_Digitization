using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using ClosedXML.Excel;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class app_qc_inspectorreport : System.Web.UI.Page
    {
        DB_Utility_OH4Y dbcl = new DB_Utility_OH4Y();
        DataTable dt_exportdata = new DataTable();

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
                    BindGridView();
                }

            }
        }

        private void BindGridView()
        {
            // Get the connection string from Web.config
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("GetTop10QCInspectorRecords", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        ViewState["ExportData"] = dt;
                        if (dt.Rows.Count > 0)
                        {
                            GridView1.DataSource = dt;
                            GridView1.DataBind();
                        }
                        else
                        {
                            // Handle no records found
                            GridView1.DataSource = null;
                            GridView1.DataBind();
                            //lblMessage.Text = "No records found for the selected plant.";
                            //lblMessage.Visible = true;

                            // Define the PNotify script for no records found
                            string noRecordsScript = @"<script type='text/javascript'>
                                    new PNotify({
                                        title: 'No Records Found',
                                        text: 'No records found for the selected plant.',
                                        type: 'info',
                                        styling: 'bootstrap3'
                                    });
                                </script>";

                            // Register the script to show the notification
                            ClientScript.RegisterStartupScript(this.GetType(), "ShowNoRecordsNotification", noRecordsScript, false);

                        }
                    }
                }
            }
        }
        private void BindGridView(string plantName)
        {
            // Get the connection string from Web.config
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("GetTop10QCInspectorRecordsByPlant", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add the PlantName parameter and its value
                    cmd.Parameters.Add(new SqlParameter("@PlantName", plantName));

                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        ViewState["ExportData"] = dt;
                        if (dt.Rows.Count > 0)
                        {
                            GridView1.DataSource = dt;
                            GridView1.DataBind();
                        }
                        else
                        {
                            // Handle no records found
                            GridView1.DataSource = null;
                            GridView1.DataBind();
                            //lblMessage.Text = "No records found for the selected plant.";
                            //lblMessage.Visible = true;

                            // Define the PNotify script for no records found
                            string noRecordsScript2 = @"<script type='text/javascript'>
                                    new PNotify({
                                        title: 'No Records Found',
                                        text: 'No records found for the selected plant.',
                                        type: 'info',
                                        styling: 'bootstrap3'
                                    });
                                </script>";

                            // Register the script to show the notification
                            ClientScript.RegisterStartupScript(this.GetType(), "ShowNoRecordsNotification2", noRecordsScript2, false);

                        }
                    }
                }
            }
        }
        private void BindGridView(string plantName, string plantLine)
        {
            // Get the connection string from Web.config
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("GetTop10QCInspectorRecordsByPlantLine", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add the PlantName and PlantLine parameters and their values
                    cmd.Parameters.Add(new SqlParameter("@PlantName", plantName));
                    cmd.Parameters.Add(new SqlParameter("@PlantLine", plantLine));

                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        ViewState["ExportData"] = dt;
                        if (dt.Rows.Count > 0)
                        {
                            GridView1.DataSource = dt;
                            GridView1.DataBind();
                        }
                        else
                        {
                            // Handle no records found
                            GridView1.DataSource = null;
                            GridView1.DataBind();

                            // Define the PNotify script for no records found
                            string noRecordsScript3 = @"<script type='text/javascript'>
                                    new PNotify({
                                        title: 'No Records Found',
                                        text: 'No records found for the selected plant and line.',
                                        type: 'info',
                                        styling: 'bootstrap3'
                                    });
                                </script>";

                            // Register the script to show the notification
                            ClientScript.RegisterStartupScript(this.GetType(), "ShowNoRecordsNotification3", noRecordsScript3, false);
                        }
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

                BindGridView(selectedPlantValue);
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
                lbl_DDL_PlantLine_Value.Text = selectedPlantLineValue;
                BindGridView(selectedPlantValue, selectedPlantLineValue);

                //here goes the code to Bind the Approval Matrix
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
        private void BindGridView(string plantName, string plantLine, string productCategory)
        {
            // Get the connection string from Web.config
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("GetTop10QCInspectorRecordsByPlantLineProduct", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add the parameters and their values
                    cmd.Parameters.Add(new SqlParameter("@PlantName", plantName));
                    cmd.Parameters.Add(new SqlParameter("@PlantLine", plantLine));
                    cmd.Parameters.Add(new SqlParameter("@ProductCategory", productCategory));

                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        ViewState["ExportData"] = dt;
                        if (dt.Rows.Count > 0)
                        {
                            GridView1.DataSource = dt;
                            GridView1.DataBind();
                        }
                        else
                        {
                            // Handle no records found
                            GridView1.DataSource = null;
                            GridView1.DataBind();

                            // Define the PNotify script for no records found
                            string noRecordsScript4 = @"<script type='text/javascript'>
                                    new PNotify({
                                        title: 'No Records Found',
                                        text: 'No records found for the selected plant, line, and product category.',
                                        type: 'info',
                                        styling: 'bootstrap3'
                                    });
                                </script>";

                            // Register the script to show the notification
                            ClientScript.RegisterStartupScript(this.GetType(), "ShowNoRecordsNotification4", noRecordsScript4, false);
                        }
                    }
                }
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
                BindGridView(selectedPlantValue, selectedPlantLineValue, selectedProductCategoryValue);
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
        private void BindGridView(string plantName, string plantLine, string productCategory, string brandName)
        {
            // Get the connection string from Web.config
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("GetTop10QCInspectorRecordsByPlantLineProductBrand", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add the parameters and their values
                    cmd.Parameters.Add(new SqlParameter("@PlantName", plantName));
                    cmd.Parameters.Add(new SqlParameter("@PlantLine", plantLine));
                    cmd.Parameters.Add(new SqlParameter("@ProductCategory", productCategory));
                    cmd.Parameters.Add(new SqlParameter("@BrandName", brandName));

                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        ViewState["ExportData"] = dt;
                        if (dt.Rows.Count > 0)
                        {
                            GridView1.DataSource = dt;
                            GridView1.DataBind();
                        }
                        else
                        {
                            // Handle no records found
                            GridView1.DataSource = null;
                            GridView1.DataBind();

                            // Define the PNotify script for no records found
                            string noRecordsScript5 = @"<script type='text/javascript'>
                                    new PNotify({
                                        title: 'No Records Found',
                                        text: 'No records found for the selected plant, line, product category, and brand.',
                                        type: 'info',
                                        styling: 'bootstrap3'
                                    });
                                </script>";

                            // Register the script to show the notification
                            ClientScript.RegisterStartupScript(this.GetType(), "ShowNoRecordsNotification5", noRecordsScript5, false);
                        }
                    }
                }
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
                BindGridView(selectedPlantValue, selectedPlantLineValue, selectedProductCategoryValue, selectedProductBrandValue);
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
        }
        protected void btn_submit_Click(object sender, EventArgs e)
        {
            string startdate = txt_date1.Text.ToString();
            string enddate = txt_date2.Text.ToString();

            string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
            string selectedPlantLineValue = DDL_PlantLine.SelectedValue.ToString();
            string selectedProductCategoryValue = DDL_ProductCategory.SelectedValue.ToString();
            string selectedProductBrandValue = DDL_ProductBrand.SelectedValue.ToString();
            string selectedBrandSKU = DDL_BrandSKU.SelectedValue.ToString();



            if (DDL_Plant.SelectedIndex != 0)
            {
                if (DDL_PlantLine.SelectedIndex != 0)
                {
                    if (DDL_ProductBrand.SelectedIndex != 0)
                    {
                        if (txt_date1.Text != "" && txt_date1.Text != "")
                        {
                            BindGridView(selectedPlantValue, selectedPlantLineValue, selectedProductCategoryValue, selectedProductBrandValue, selectedBrandSKU, startdate, enddate);
                        }
                        else
                        {
                            // Define the PNotify script for no records found
                            string noRecordsScript9 = @"<script type='text/javascript'>
                                new PNotify({
                                    title: 'No Records Found',
                                    text: 'No Calender Date Selection by User',
                                    type: 'info',
                                    styling: 'bootstrap3'
                                });
                            </script>";

                            // Register the script to show the notification
                            ClientScript.RegisterStartupScript(this.GetType(), "ShowNoRecordsNotification9", noRecordsScript9, false);

                            BindGridView(selectedPlantValue, selectedPlantLineValue, selectedProductCategoryValue, selectedProductBrandValue, selectedBrandSKU);
                        }
                    }
                }
            }
        }
        private void BindGridView(string plantName, string plantLine, string productCategory, string brandName, string skuid, string startDate, string endDate)
        {
            // Get the connection string from Web.config
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("GetTop10QCInspectorRecordsByPlantLineProductBrandSKUDates", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add the parameters and their values
                    cmd.Parameters.Add(new SqlParameter("@PlantName", plantName));
                    cmd.Parameters.Add(new SqlParameter("@PlantLine", plantLine));
                    cmd.Parameters.Add(new SqlParameter("@ProductCategory", productCategory));
                    cmd.Parameters.Add(new SqlParameter("@BrandName", brandName));
                    cmd.Parameters.Add(new SqlParameter("@SKUId", skuid));
                    cmd.Parameters.Add(new SqlParameter("@StartDate", startDate));
                    cmd.Parameters.Add(new SqlParameter("@EndDate", endDate));

                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        ViewState["ExportData"] = dt;
                        if (dt.Rows.Count > 0)
                        {
                            GridView1.DataSource = dt;
                            GridView1.DataBind();
                        }
                        else
                        {
                            // Handle no records found
                            GridView1.DataSource = null;
                            GridView1.DataBind();

                            // Define the PNotify script for no records found
                            string noRecordsScript8 = @"<script type='text/javascript'>
                                new PNotify({
                                    title: 'No Records Found',
                                    text: 'No records found for the selected plant, line, product category, brand, and dates.',
                                    type: 'info',
                                    styling: 'bootstrap3'
                                });
                            </script>";

                            // Register the script to show the notification
                            ClientScript.RegisterStartupScript(this.GetType(), "ShowNoRecordsNotification8", noRecordsScript8, false);
                        }
                    }
                }
            }
        }
        private void BindGrid(string cmdString)
        {
            dbcl.Sqlconnection();
            dbcl.ConnectDb();
            SqlCommand cmd = new SqlCommand(cmdString, dbcl.Conn);
            SqlDataAdapter ad = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            ad.Fill(ds);
            GridView1.DataSource = ds;
            GridView1.DataBind();
            dbcl.Conn.Close();
        }
        protected void DDL_BrandSKU_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Plant.SelectedIndex != 0)
            {
                if (DDL_PlantLine.SelectedIndex != 0)
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

                            BindGridView(selectedPlantValue, selectedPlantLineValue, selectedProductCategoryValue, selectedProductBrandValue, selectedBrandSKU);
                        }
                    }
                }
            }
        }
        private void BindGridView(string plantName, string plantLine, string productCategory, string brandName, string skuid)
        {
            // Get the connection string from Web.config
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("GetTop10QCInspectorRecordsByPlantLineProductBrandSKU", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add the parameters and their values
                    cmd.Parameters.Add(new SqlParameter("@PlantName", plantName));
                    cmd.Parameters.Add(new SqlParameter("@PlantLine", plantLine));
                    cmd.Parameters.Add(new SqlParameter("@ProductCategory", productCategory));
                    cmd.Parameters.Add(new SqlParameter("@BrandName", brandName));
                    cmd.Parameters.Add(new SqlParameter("@SKUId", skuid));

                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        ViewState["ExportData"] = dt;
                        if (dt.Rows.Count > 0)
                        {
                            GridView1.DataSource = dt;
                            GridView1.DataBind();
                        }
                        else
                        {
                            // Handle no records found
                            GridView1.DataSource = null;
                            GridView1.DataBind();

                            // Define the PNotify script for no records found
                            string noRecordsScript7 = @"<script type='text/javascript'>
                                    new PNotify({
                                        title: 'No Records Found',
                                        text: 'No records found for the selected plant, line, product category, brand and SKU',
                                        type: 'info',
                                        styling: 'bootstrap3'
                                    });
                                </script>";

                            // Register the script to show the notification
                            ClientScript.RegisterStartupScript(this.GetType(), "ShowNoRecordsNotification7", noRecordsScript7, false);
                        }
                    }
                }
            }
        }
        protected void ExportExcel_Old(object sender, EventArgs e)
        {
            try
            {
                string constr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    using (SqlCommand cmd = new SqlCommand("GetTop10QCInspectorRecords", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                        {
                            using (DataTable dt = new DataTable())
                            {
                                sda.Fill(dt);
                                using (XLWorkbook wb = new XLWorkbook())
                                {
                                    wb.Worksheets.Add(dt, "QCInspectorReport");

                                    Response.Clear();
                                    Response.Buffer = true;
                                    Response.Charset = "";
                                    Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                                    Response.AddHeader("content-disposition", "attachment;filename=QCInspectorRecords.xlsx");
                                    using (MemoryStream MyMemoryStream = new MemoryStream())
                                    {
                                        wb.SaveAs(MyMemoryStream);
                                        MyMemoryStream.WriteTo(Response.OutputStream);
                                        Response.Flush();
                                        Response.End();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error or handle it appropriately
                Response.Write("An error occurred: " + ex.Message);
            }
        }

        protected void ExportExcel(object sender, EventArgs e)
        {
            if (ViewState["ExportData"] != null)
            {
                DataTable dt = ViewState["ExportData"] as DataTable;

                if (dt != null && dt.Rows.Count > 0)
                {
                    using (XLWorkbook wb = new XLWorkbook())
                    {
                        wb.Worksheets.Add(dt, "QCInspectorReport");

                        Response.Clear();
                        Response.Buffer = true;
                        Response.Charset = "";
                        Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        Response.AddHeader("content-disposition", "attachment;filename=QCInspectorRecords.xlsx");

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

        protected void GridView1_RowCommand(object sender, System.Web.UI.WebControls.GridViewCommandEventArgs e)
        {
            int rowIndex = Convert.ToInt32(e.CommandArgument);
            GridViewRow row = GridView1.Rows[rowIndex];

            string dbid = (row.FindControl("lbl_rowid") as Label).Text;
            //string app1 = (row.FindControl("lbl_Approver1") as Label).Text;
            //string app2 = (row.FindControl("lbl_Approver2") as Label).Text;
            //string dtapp = (row.FindControl("lbl_DottedLineApproverEmployeeCode") as Label).Text;

            if (e.CommandName == "ViewApprove")
            {
                Response.Redirect("vw_app_qcireport.aspx?ID=" + dbid + "&VM=1", false);
            }
        }
    }
}