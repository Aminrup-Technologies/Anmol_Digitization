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
        //protected void btn_submit_Click(object sender, EventArgs e)
        //{
        //    string startdate = txt_date1.Text.ToString();
        //    string enddate = txt_date2.Text.ToString();

        //    string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
        //    string selectedPlantLineValue = DDL_PlantLine.SelectedValue.ToString();
        //    string selectedProductCategoryValue = DDL_ProductCategory.SelectedValue.ToString();
        //    string selectedProductBrandValue = DDL_ProductBrand.SelectedValue.ToString();
        //    string selectedBrandSKU = DDL_BrandSKU.SelectedValue.ToString();



        //    if (DDL_Plant.SelectedIndex != 0)
        //    {
        //        if (DDL_PlantLine.SelectedIndex != 0)
        //        {
        //            if (DDL_ProductBrand.SelectedIndex != 0)
        //            {
        //                if (txt_date1.Text != "" && txt_date1.Text != "")
        //                {
        //                    BindGridView(selectedPlantValue, selectedPlantLineValue, selectedProductCategoryValue, selectedProductBrandValue, selectedBrandSKU, startdate, enddate);
        //                }
        //                else
        //                {
        //                    // Define the PNotify script for no records found
        //                    string noRecordsScript9 = @"<script type='text/javascript'>
        //                        new PNotify({
        //                            title: 'No Records Found',
        //                            text: 'No Calender Date Selection by User',
        //                            type: 'info',
        //                            styling: 'bootstrap3'
        //                        });
        //                    </script>";

        //                    // Register the script to show the notification
        //                    ClientScript.RegisterStartupScript(this.GetType(), "ShowNoRecordsNotification9", noRecordsScript9, false);

        //                    BindGridView(selectedPlantValue, selectedPlantLineValue, selectedProductCategoryValue, selectedProductBrandValue, selectedBrandSKU);
        //                }
        //            }
        //        }
        //    }
        //}


        //protected void btn_submit_Click(object sender, EventArgs e)
        //{
        //    string startDate = txt_date1.Text.Trim();
        //    string endDate = txt_date2.Text.Trim();

        //    string plant = DDL_Plant.SelectedIndex > 0 ? DDL_Plant.SelectedValue : null;
        //    string line = DDL_PlantLine.SelectedIndex > 0 ? DDL_PlantLine.SelectedValue : null;
        //    string category = DDL_ProductCategory.SelectedIndex > 0 ? DDL_ProductCategory.SelectedValue : null;
        //    string brand = DDL_ProductBrand.SelectedIndex > 0 ? DDL_ProductBrand.SelectedValue : null;
        //    string sku = DDL_BrandSKU.SelectedIndex > 0 ? DDL_BrandSKU.SelectedValue : null;

        //    DateTime? start = string.IsNullOrWhiteSpace(startDate) ? (DateTime?)null : DateTime.Parse(startDate);
        //    DateTime? end = string.IsNullOrWhiteSpace(endDate) ? (DateTime?)null : DateTime.Parse(endDate);

        //    if (plant != null || line != null || category != null || brand != null || sku != null || start != null || end != null)
        //    {
        //        BindGridView_FlexibleFilter(plant, line, category, brand, sku, start, end);
        //    }
        //    else
        //    {
        //        // Show warning if no filters selected
        //        string script = @"<script type='text/javascript'>
        //                    new PNotify({
        //                        title: 'Input Required',
        //                        text: 'Please select at least one filter or a date range.',
        //                        type: 'warning',
        //                        styling: 'bootstrap3'
        //                    });
        //                  </script>";
        //        ClientScript.RegisterStartupScript(this.GetType(), "FilterWarning", script, false);
        //    }
        //}

        protected void btn_submit_Click(object sender, EventArgs e)
        {
            string plant = DDL_Plant.SelectedIndex > 0 ? DDL_Plant.SelectedValue : null;
            string line = DDL_PlantLine.SelectedIndex > 0 ? DDL_PlantLine.SelectedValue : null;
            string category = DDL_ProductCategory.SelectedIndex > 0 ? DDL_ProductCategory.SelectedValue : null;
            string brand = DDL_ProductBrand.SelectedIndex > 0 ? DDL_ProductBrand.SelectedValue : null;
            string sku = DDL_BrandSKU.SelectedIndex > 0 ? DDL_BrandSKU.SelectedValue : null;

            // If user hasn't selected dates, default to current month
            DateTime start = DateTime.Today;
            DateTime end = DateTime.Today;

            if (!string.IsNullOrWhiteSpace(txt_date1.Text) && !string.IsNullOrWhiteSpace(txt_date2.Text))
            {
                // Use user-selected dates
                start = DateTime.Parse(txt_date1.Text.Trim());
                end = DateTime.Parse(txt_date2.Text.Trim());
            }
            else
            {
                // Default to first and last day of current month
                start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                end = start.AddMonths(1).AddDays(-1);
            }

            // Proceed only if at least one dropdown is selected
            if (plant != null || line != null || category != null || brand != null || sku != null)
            {
                BindGridView_FlexibleFilter(plant, line, category, brand, sku, start, end);
            }
            else
            {
                // Show warning if no dropdown selected
                string script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Filter Required',
                                text: 'Please select at least one filter (Plant, Line, Category, Brand, or SKU).',
                                type: 'warning',
                                styling: 'bootstrap3'
                            });
                          </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "FilterWarning", script, false);
            }
        }


        private void BindGridView_FlexibleFilter(string plant, string line, string category, string brand, string sku, DateTime? startDate, DateTime? endDate)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            using (SqlCommand cmd = new SqlCommand("GetQCInspectorRecordsFlexibleFilter", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@PlantName", (object)plant ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PlantLine", (object)line ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ProductCategory", (object)category ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BrandName", (object)brand ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SKUId", (object)sku ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@StartDate", (object)startDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EndDate", (object)endDate ?? DBNull.Value);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                ViewState["ExportData"] = dt;

                GridView1.DataSource = dt.Rows.Count > 0 ? dt : null;
                GridView1.DataBind();

                if (dt.Rows.Count == 0)
                {
                    NotifyNoRecordsFound();
                }
            }
        }

        private void NotifyNoRecordsFound()
        {
            string script = @"<script type='text/javascript'>
                        new PNotify({
                            title: 'No Records Found',
                            text: 'No records match the applied filters.',
                            type: 'info',
                            styling: 'bootstrap3'
                        });
                      </script>";

            ClientScript.RegisterStartupScript(this.GetType(), "NoDataAlert", script, false);
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

        // --- HELPER TO GENERATE BEAUTIFUL UI BADGES FOR THE GRID ---
        protected string GetStatusBadge(object statusObj, object empCodeObj)
        {
            string empCode = empCodeObj != null ? empCodeObj.ToString() : "N/A";
            string status = statusObj != null ? statusObj.ToString() : "0";

            if (status == "1")
            {
                return $"<span class='badge bg-success' title='{empCode}'>Approved</span>";
            }
            else if (status == "2")
            {
                return $"<span class='badge bg-danger' title='{empCode}'>Rejected</span>";
            }
            else
            {
                return $"<span class='badge bg-warning text-dark' title='{empCode}'>Pending</span>";
            }
        }

        // --- SMART BULK APPROVAL LOGIC ---
        protected void btnBulkApprove_Click(object sender, EventArgs e)
        {
            string employeeCode = Session["WORKMAN"] as string;
            if (string.IsNullOrEmpty(employeeCode))
            {
                ClientScript.RegisterStartupScript(this.GetType(), "alert", "alert('Session expired. Please log in again.');", true);
                return;
            }

            int approvedCount = 0;
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                foreach (GridViewRow row in GridView1.Rows)
                {
                    if (row.RowType == DataControlRowType.DataRow)
                    {
                        CheckBox chkSelect = (CheckBox)row.FindControl("chkSelect");

                        if (chkSelect != null && chkSelect.Checked)
                        {
                            Label lblRowId = (Label)row.FindControl("lbl_rowid");
                            if (lblRowId != null && !string.IsNullOrEmpty(lblRowId.Text))
                            {
                                int recordId = Convert.ToInt32(lblRowId.Text);

                                // SMART QUERY: Automatically updates ONLY the approval tier belonging to the logged-in user.
                                // Bypasses the update entirely if the user does not have authority for this specific row.
                                string updateQuery = @"
                            UPDATE TRN_qcinspector 
                            SET 
                                Approver1_Status = CASE WHEN Approver1EmployeeCode = @EmpCode THEN 1 ELSE Approver1_Status END,
                                Approver1_TimeStamp = CASE WHEN Approver1EmployeeCode = @EmpCode THEN @TimeStamp ELSE Approver1_TimeStamp END,
                                
                                Approver2_Status = CASE WHEN Approver2EmployeeCode = @EmpCode THEN 1 ELSE Approver2_Status END,
                                Approver2_TimeStamp = CASE WHEN Approver2EmployeeCode = @EmpCode THEN @TimeStamp ELSE Approver2_TimeStamp END,
                                
                                DottedApprover_Status = CASE WHEN DottedLineApproverEmployeeCode = @EmpCode THEN 1 ELSE DottedApprover_Status END,
                                DottedApprover_TimeStamp = CASE WHEN DottedLineApproverEmployeeCode = @EmpCode THEN @TimeStamp ELSE DottedApprover_TimeStamp END
                            WHERE ID = @ID 
                              AND (Approver1EmployeeCode = @EmpCode OR Approver2EmployeeCode = @EmpCode OR DottedLineApproverEmployeeCode = @EmpCode)";

                                using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                                {
                                    cmd.Parameters.AddWithValue("@TimeStamp", DateTime.Now);
                                    cmd.Parameters.AddWithValue("@EmpCode", employeeCode);
                                    cmd.Parameters.AddWithValue("@ID", recordId);

                                    int rowsAffected = cmd.ExecuteNonQuery();
                                    if (rowsAffected > 0)
                                    {
                                        approvedCount++;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (approvedCount > 0)
            {
                string script = $"new PNotify({{ title: 'Success', text: 'Successfully bulk approved {approvedCount} records.', type: 'success', styling: 'bootstrap3' }});";
                ClientScript.RegisterStartupScript(this.GetType(), "BulkApproveSuccess", script, true);

                // Re-trigger the submit button to refresh the grid with the current filters
                btn_submit_Click(null, null);
            }
            else
            {
                string script = "new PNotify({ title: 'Notice', text: 'No records were updated. Make sure you selected records where you are authorized to approve.', type: 'info', styling: 'bootstrap3' });";
                ClientScript.RegisterStartupScript(this.GetType(), "BulkApproveNone", script, true);
            }
        }

        // --- 1. HELPER TO RESOLVE EMPLOYEE NAMES & RENDER BADGES ---
        protected string GetApproverDisplay(object empCodeObj, object statusObj)
        {
            // Fix: Added .Trim() to prevent hidden spaces from failing the empty check
            string empCode = empCodeObj != null ? empCodeObj.ToString().Trim() : "";
            string status = statusObj != null ? statusObj.ToString() : "0";
            string badge = "";

            if (status == "1") badge = "<span class='badge bg-success' style='font-size:10px;'>Approved</span>";
            else if (status == "2") badge = "<span class='badge bg-danger' style='font-size:10px;'>Rejected</span>";
            else badge = "<span class='badge bg-warning text-dark' style='font-size:10px;'>Pending</span>";

            if (string.IsNullOrEmpty(empCode))
            {
                return $"<span class='text-muted'>Unassigned</span> {badge}";
            }

            // Fetch Name from DB and Cache it
            string empName = GetMasterDescription($"EMP::{empCode}",
                "SELECT TOP 1 EmployeeName FROM MST_UserMaster WHERE EmployeeCode = @emp",
                p => p.AddWithValue("@emp", empCode)) ?? "Unknown";

            return $"<b>{empName}</b> <span class='text-muted'>({empCode})</span> {badge}";
        }

        // --- 2. HELPER TO FETCH AND FORMAT HISTORICAL STD LIMITS ---
        protected string GetStdLimitText(object plantObj, object lineObj, object catObj, object brandObj, object dateObj, object timeObj, string fieldName)
        {
            try
            {
                int formId = 1; // 1 = QAQC Inspector Report
                int plantId = Convert.ToInt32(plantObj);
                int lineId = Convert.ToInt32(lineObj);
                int catId = Convert.ToInt32(catObj);
                int brandId = Convert.ToInt32(brandObj);

                // Calculate exact submission time
                DateTime submissionDateTime = DateTime.Now;
                if (dateObj != null && dateObj != DBNull.Value)
                {
                    DateTime date = Convert.ToDateTime(dateObj);
                    if (timeObj != null && timeObj != DBNull.Value)
                        submissionDateTime = date.Add((TimeSpan)timeObj);
                    else
                        submissionDateTime = date.Add(new TimeSpan(23, 59, 59));
                }

                // Cache Key specific to this Form + Date so we don't query the DB constantly
                string cacheKey = $"STD_{formId}_{plantId}_{lineId}_{catId}_{brandId}_{submissionDateTime:yyyyMMdd}";
                DataTable dtConfig = HttpRuntime.Cache[cacheKey] as DataTable;

                if (dtConfig == null)
                {
                    dtConfig = DatabaseHelper.GetFormFieldControlByFormAndBrand(formId, plantId, lineId, brandId, catId, submissionDateTime);
                    if (dtConfig != null)
                    {
                        HttpRuntime.Cache.Insert(cacheKey, dtConfig, null, DateTime.Now.AddMinutes(10), System.Web.Caching.Cache.NoSlidingExpiration);
                    }
                }

                if (dtConfig != null)
                {
                    DataRow[] rows = dtConfig.Select($"field_name = '{fieldName}'");
                    if (rows.Length > 0)
                    {
                        string min = rows[0]["RV_MinValue"].ToString();
                        string max = rows[0]["RV_MaxValue"].ToString();
                        if (!string.IsNullOrEmpty(min) && !string.IsNullOrEmpty(max))
                        {
                            return $"(Std: {min} - {max})";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // If an error happens parsing the IDs, return a small red 'Err' to help debugging instead of failing silently
                return $"<span style='color:red;'>Err</span>";
            }

            return "";
        }

        // --- 3. UNIVERSAL CACHED SQL EXECUTOR ---
        private string GetMasterDescription(string cachePrefix, string sql, Action<SqlParameterCollection> bindParams, int cacheSeconds = 300)
        {
            string cacheKey = cachePrefix;
            var cached = HttpRuntime.Cache[cacheKey] as string;
            if (!string.IsNullOrEmpty(cached)) return cached;

            try
            {
                using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
                using (var cmd = new SqlCommand(sql, con))
                {
                    bindParams(cmd.Parameters);
                    con.Open();
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        string value = Convert.ToString(result);
                        HttpRuntime.Cache.Insert(cacheKey, value, null, DateTime.Now.AddSeconds(cacheSeconds), System.Web.Caching.Cache.NoSlidingExpiration);
                        return value;
                    }
                }
            }
            catch { }
            return null;
        }
    }
}