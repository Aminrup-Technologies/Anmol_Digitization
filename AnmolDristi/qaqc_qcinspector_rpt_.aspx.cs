using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using ClosedXML.Excel;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class qaqc_qcinspector_rpt_ : System.Web.UI.Page
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
                    LoadData("TRN_qcinspector");
                    //loadAlldata();
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
                        TRN_qcinspector c
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

        private void LoadData(string tableName)
        {
            try
            {
                string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("USP_LoadViewListData", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@TableName", tableName);
                        cmd.Parameters.AddWithValue("@TopRecords", 30);

                        SqlDataAdapter sda = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        sda.Fill(dt);
                        ViewState["ExportData"] = dt;
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

        private void BindGridView()
        {
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
                            GridView1.DataSource = null;
                            GridView1.DataBind();

                            string noRecordsScript = @"<script type='text/javascript'>
                                    new PNotify({
                                        title: 'No Records Found',
                                        text: 'No records found for the selected plant.',
                                        type: 'info',
                                        styling: 'bootstrap3'
                                    });
                                </script>";
                            ClientScript.RegisterStartupScript(this.GetType(), "ShowNoRecordsNotification", noRecordsScript, false);

                        }
                    }
                }
            }
        }

        private void BindGridView(string plantName)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("GetTop10QCInspectorRecordsByPlant", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
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
                            GridView1.DataSource = null;
                            GridView1.DataBind();
                            string noRecordsScript2 = @"<script type='text/javascript'>
                                    new PNotify({
                                        title: 'No Records Found',
                                        text: 'No records found for the selected plant.',
                                        type: 'info',
                                        styling: 'bootstrap3'
                                    });
                                </script>";
                            ClientScript.RegisterStartupScript(this.GetType(), "ShowNoRecordsNotification2", noRecordsScript2, false);

                        }
                    }
                }
            }
        }
        private void BindGridView(string plantName, string plantLine)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("GetTop10QCInspectorRecordsByPlantLine", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
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
                            GridView1.DataSource = null;
                            GridView1.DataBind();

                            string noRecordsScript3 = @"<script type='text/javascript'>
                                    new PNotify({
                                        title: 'No Records Found',
                                        text: 'No records found for the selected plant and line.',
                                        type: 'info',
                                        styling: 'bootstrap3'
                                    });
                                </script>";
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
            DatabaseHelper.BindDropDownList(query, DDL_Plant, textField, valueField, out recordsBound);
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
                lbl_DDL_PlantLine_Value.Text = selectedPlantLineValue;
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
            string query = "SELECT category_id, category_name FROM MST_LineCategory WHERE plant_id = @PlantId AND line_id = @LineId";
            string textField = "category_name";
            string valueField = "category_id";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@PlantId", selectedPlantValue),
                new SqlParameter("@LineId", selectedPlantLineValue)
            };

            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_ProductCategory, textField, valueField, parameters, out recordsBound);

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
                ClientScript.RegisterStartupScript(this.GetType(), "ShowLineProductsBinderErrorNotification", PN_Error_script, false);
            }
        }
        private void BindGridView(string plantName, string plantLine, string productCategory)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("GetTop10QCInspectorRecordsByPlantLineProduct", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
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
                            GridView1.DataSource = null;
                            GridView1.DataBind();

                            string noRecordsScript4 = @"<script type='text/javascript'>
                                    new PNotify({
                                        title: 'No Records Found',
                                        text: 'No records found for the selected plant, line, and product category.',
                                        type: 'info',
                                        styling: 'bootstrap3'
                                    });
                                </script>";

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
            string query = "SELECT brand_id, brand_name FROM MST_LineCatBrands WHERE plant_id = @PlantId AND line_id = @LineId and category_id=@CategoryId";
            string textField = "brand_name";
            string valueField = "brand_id";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@PlantId", selectedPlantValue),
                new SqlParameter("@LineId", selectedPlantLineValue),
                new SqlParameter("@CategoryId", selectedProductCategoryValue)
            };

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
                ClientScript.RegisterStartupScript(this.GetType(), "ShowProductBrandsBinderErrorNotification", ProductBrands_Error_script, false);
            }
        }
        private void BindGridView(string plantName, string plantLine, string productCategory, string brandName)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("GetTop10QCInspectorRecordsByPlantLineProductBrand", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
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
                            GridView1.DataSource = null;
                            GridView1.DataBind();

                            string noRecordsScript5 = @"<script type='text/javascript'>
                                    new PNotify({
                                        title: 'No Records Found',
                                        text: 'No records found for the selected plant, line, product category, and brand.',
                                        type: 'info',
                                        styling: 'bootstrap3'
                                    });
                                </script>";

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
            DataLoader();
        }

        private void DataLoader()
        {
            DateTime? dateFrom = string.IsNullOrEmpty(txt_date1.Text) ? (DateTime?)null : DateTime.ParseExact(txt_date1.Text, "yyyy-MM-dd", null);
            DateTime? dateTo = string.IsNullOrEmpty(txt_date2.Text) ? (DateTime?)null : DateTime.ParseExact(txt_date2.Text, "yyyy-MM-dd", null);

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
                FROM TRN_qcinspector c
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

        public class ReportInfo
        {
            public int DBID { get; set; }
            public int FormID { get; set; }
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

                            DataLoader();
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
            if (e.CommandName == "View")
            {
                Response.Redirect("vw_app_qcireport.aspx?ID=" + dbid + "&VM=0", false);
            }
        }
    }
}