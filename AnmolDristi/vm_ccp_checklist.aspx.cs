using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Text;
using System.IO;
using Newtonsoft.Json;

namespace AnmolDristi
{
    public partial class vm_ccp_checklist : System.Web.UI.Page
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
                    //lbl_docnumber.Text = "ANMOL/DOC/DAN/QA/05";
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

        public class ReportInfoOld
        {

            public int Id { get; set; }
            public string CcpId { get; set; }
            public int FormID { get; set; }
            public string PlantId { get; set; }
            public string Line { get; set; }
            public string ProductCategory { get; set; }
            public string ProductBrand { get; set; }
            public string SKUID { get; set; }
            public int SubmittedById { get; set; }
            public string SubmittedByEmployeeCode { get; set; }
            public DateTime SubmittedDate { get; set; }
            public TimeSpan SubmittedTime { get; set; }
            public string Shift { get; set; }
            public int ViewMode { get; set; }
            public int DeleteMode { get; set; }
            public string Approver1EmployeeCode { get; set; }
            public int? Approver1_Status { get; set; }
            public DateTime? Approver1_TimeStamp { get; set; }
            public string Approver2EmployeeCode { get; set; }
            public int? Approver2_Status { get; set; }
            public DateTime? Approver2_TimeStamp { get; set; }
            public string DottedLineApproverEmployeeCode { get; set; }
            public int? DottedApprover_Status { get; set; }
            public DateTime? DottedApprover_TimeStamp { get; set; }
            public string MetalCheck { get; set; }
            public string SieveCheck { get; set; }
            public int? FF_Status { get; set; }
            public string FF_Remarks { get; set; }
            public int? NFE_Status { get; set; }
            public string NFE_Remarks { get; set; }
            public int? SS_Status { get; set; }
            public string SS_Remarks { get; set; }
            public string MD_Remarks { get; set; }
            public int? T1_Status { get; set; }
            public int? T2_Status { get; set; }
            public int? T3_Status { get; set; }
            public int? T4_Status { get; set; }
            public int? Final_Status { get; set; }
        }

        public class ReportInfo
        {
            public int DBID { get; set; }
            public string RecordID { get; set; }
            public int FormID { get; set; }
            public string PlantName { get; set; } // Added for MST_PlantDetails.plant_name
            public string EmpCode { get; set; }
            public string EmpName { get; set; } // Added for MST_UserMaster.EmployeeName
            public DateTime SDate { get; set; }
            public TimeSpan STime { get; set; }
            public string SShift { get; set; }
            public string MetalCheck { get; set; }
            public string L1 { get; set; }
            public int? Approver1_Status { get; set; }
            public DateTime? Approver1_TimeStamp { get; set; }
            public string L2 { get; set; }
            public int? Approver2_Status { get; set; }
            public DateTime? Approver2_TimeStamp { get; set; }
            public string L3 { get; set; }
            public int? DottedApprover_Status { get; set; }
            public DateTime? DottedApprover_TimeStamp { get; set; }
            public string Remarks { get; set; }
        }

        //private DataTable GetFilteredDataOld()
        //{
        //    string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        //    DataTable dt = new DataTable();

        //    using (SqlConnection con = new SqlConnection(connectionString))
        //    {
        //        // Start building the SQL query
        //        StringBuilder sqlQuery = new StringBuilder("SELECT * FROM [dbo].[TRN_CCP_Checklist] WHERE 1=1 ");

        //        // List of parameters to add to the command later
        //        List<SqlParameter> parameters = new List<SqlParameter>();

        //        // Add filters to SQL query based on the selected values
        //        if (!string.IsNullOrEmpty(DDL_Plant.SelectedValue) && DDL_Plant.SelectedValue != "0")
        //        {
        //            sqlQuery.Append(" AND PlantId = @Plant_Id ");
        //            parameters.Add(new SqlParameter("@Plant_Id", DDL_Plant.SelectedValue));
        //        }

        //        if (!string.IsNullOrEmpty(DDL_PlantLine.SelectedValue) && DDL_PlantLine.SelectedValue != "0")
        //        {
        //            sqlQuery.Append(" AND Line = @Plant_Line ");
        //            parameters.Add(new SqlParameter("@Plant_Line", DDL_PlantLine.SelectedValue));
        //        }

        //        if (!string.IsNullOrEmpty(DDL_ProductCategory.SelectedValue) && DDL_ProductCategory.SelectedValue != "0")
        //        {
        //            sqlQuery.Append(" AND ProductCategory = @Product_Category ");
        //            parameters.Add(new SqlParameter("@Product_Category", DDL_ProductCategory.SelectedValue));
        //        }

        //        if (!string.IsNullOrEmpty(DDL_ProductBrand.SelectedValue) && DDL_ProductBrand.SelectedValue != "0")
        //        {
        //            sqlQuery.Append(" AND ProductBrand = @Product_Brand ");
        //            parameters.Add(new SqlParameter("@Product_Brand", DDL_ProductBrand.SelectedValue));
        //        }

        //        if (!string.IsNullOrEmpty(DDL_BrandSKU.SelectedValue) && DDL_BrandSKU.SelectedValue != "0")
        //        {
        //            sqlQuery.Append(" AND SKUId = @SkuId ");
        //            parameters.Add(new SqlParameter("@SkuId", DDL_BrandSKU.SelectedValue));
        //        }

        //        using (SqlCommand cmd = new SqlCommand(sqlQuery.ToString(), con))
        //        {
        //            // Add parameters to the SqlCommand
        //            cmd.Parameters.AddRange(parameters.ToArray());
        //            cmd.CommandType = CommandType.Text;

        //            using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
        //            {
        //                // Fill DataTable with the result
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
                // Start building the SQL query
                StringBuilder sqlQuery = new StringBuilder(@"
                SELECT 
                    c.ID AS DBID,
                    c.FormID as FormID,
                    c.CcpId as RecordID,
                    p.plant_name AS PlantName,
                    c.SubmittedByEmployeeCode as EmpCode,
                    u.EmployeeName AS EmpName,
                    c.SubmittedDate as SDate,
                    c.SubmittedTime as STime,
                    c.Shift as SShift,
                    c.MetalCheck as MetalCheck,
                    c.MD_Remarks as Remarks,
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
                    TRN_CCP_Checklist c
                LEFT JOIN 
                    MST_PlantDetails p ON c.PlantId = p.plant_id
                LEFT JOIN
                    MST_UserMaster u ON c.SubmittedByEmployeeCode = u.EmployeeCode
                WHERE 
                1 = 1 ");

                List<SqlParameter> parameters = new List<SqlParameter>();

                if (!string.IsNullOrEmpty(DDL_Plant.SelectedValue) && DDL_Plant.SelectedValue != "0")
                {
                    sqlQuery.Append(" AND c.PlantId = @PlantId ");
                    parameters.Add(new SqlParameter("@PlantId", DDL_Plant.SelectedValue));
                }

                sqlQuery.Append(@"
                ORDER BY 
                c.[SubmittedDate] DESC, 
                c.[SubmittedTime] DESC");

                using (SqlCommand cmd = new SqlCommand(sqlQuery.ToString(), con))
                {
                    cmd.Parameters.AddRange(parameters.ToArray());
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
            DataTable dt = GetFilteredData();
            List<ReportInfo> dataSave = new List<ReportInfo>();
            foreach (DataRow row in dt.Rows)
            {
                ReportInfo info = new ReportInfo
                {
                    PlantName = row["PlantName"] == DBNull.Value ? string.Empty : row["PlantName"].ToString(),
                    RecordID = row["RecordID"] == DBNull.Value ? string.Empty : row["RecordID"].ToString(),
                    FormID = row["FormID"] == DBNull.Value ? 0 : Convert.ToInt32(row["FormID"]),
                    EmpCode = row["EmpCode"] == DBNull.Value ? string.Empty : row["EmpCode"].ToString(),
                    EmpName = row["EmpName"] == DBNull.Value ? string.Empty : row["EmpName"].ToString(),
                    SDate = row["SDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["SDate"]),
                    STime = row["STime"] == DBNull.Value ? TimeSpan.Zero : TimeSpan.Parse(row["STime"].ToString()),
                    SShift = row["SShift"] == DBNull.Value ? string.Empty : row["SShift"].ToString(),
                    MetalCheck = row["MetalCheck"] == DBNull.Value ? string.Empty : row["MetalCheck"].ToString(),
                    L1 = row["L1"] == DBNull.Value ? string.Empty : row["L1"].ToString(),
                    Approver1_Status = row["Approver1_Status"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["Approver1_Status"]),
                    Approver1_TimeStamp = row["Approver1_TimeStamp"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["Approver1_TimeStamp"]),
                    L2 = row["L2"] == DBNull.Value ? string.Empty : row["L2"].ToString(),
                    Approver2_Status = row["Approver2_Status"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["Approver2_Status"]),
                    Approver2_TimeStamp = row["Approver2_TimeStamp"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["Approver2_TimeStamp"]),
                    L3 = row["L3"] == DBNull.Value ? string.Empty : row["L3"].ToString(),
                    DottedApprover_Status = row["DottedApprover_Status"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["DottedApprover_Status"]),
                    DottedApprover_TimeStamp = row["DottedApprover_TimeStamp"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["DottedApprover_TimeStamp"]),
                    Remarks = row["Remarks"] == DBNull.Value ? string.Empty : row["Remarks"].ToString()
                };

                dataSave.Add(info);
            }
            GridView1.DataSource = dataSave;
            GridView1.DataBind();
        }

        //private void loadAlldata()
        //{
        //    SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString);
        //    SqlCommand cmd = new SqlCommand("select * from TRN_CCP_Checklist", con);
        //    SqlDataAdapter sda = new SqlDataAdapter(cmd);
        //    DataTable dt = new DataTable();
        //    sda.Fill(dt);
        //    GridView1.DataSource = dt;
        //    GridView1.DataBind();
        //}

        private void loadAlldata()
        {
            try
            {
                string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

                string query = @"
                    SELECT TOP(30)
                        c.ID AS DBID,
                        c.FormID as FormID,
                        c.CcpId as RecordID,
                        p.plant_name AS PlantName,
                        c.SubmittedByEmployeeCode as EmpCode,
                        u.EmployeeName AS EmpName,
                        c.SubmittedDate as SDate,
                        c.SubmittedTime as STime,
                        c.Shift as SShift,
                        c.MD_Remarks as Remarks,
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
                        TRN_CCP_Checklist c
                    LEFT JOIN 
                        MST_PlantDetails p ON c.PlantId = p.plant_id
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

                        // Parse the MetalCheck column containing JSON
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

        private string GenerateHtmlTableFromJson(string json)
        {
            try
            {
                var jsonData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(json);

                StringBuilder htmlTable = new StringBuilder();
                htmlTable.Append("<table border='1' style='width:100%;border-collapse:collapse;'>");
                htmlTable.Append("<tr>");
                foreach (var key in jsonData[0].Keys)
                {
                    htmlTable.Append($"<th>{key}</th>");
                }
                htmlTable.Append("</tr>");

                foreach (var record in jsonData)
                {
                    htmlTable.Append("<tr>");
                    foreach (var value in record.Values)
                    {
                        htmlTable.Append($"<td>{value}</td>");
                    }
                    htmlTable.Append("</tr>");
                }

                htmlTable.Append("</table>");
                return htmlTable.ToString();
            }
            catch
            {
                return "Invalid JSON data";
            }
        }

        public class Item
        {
            public string Sl { get; set; }
            public string L { get; set; }
            public string QMF { get; set; }
            public string CS { get; set; }
            public string CSR { get; set; }
        }

        protected void ExportBtn_Click(object sender, EventArgs e)
        {
            try
            {
                // Fetch the data that needs to be exported from TRN_CCP_Checklist
                DataTable exportData = GetFilteredData(); // Update this method to retrieve your specific data for export

                if (exportData != null && exportData.Rows.Count > 0)
                {
                    // Set the response settings for Excel export
                    Response.Clear();
                    Response.Buffer = true;
                    Response.AddHeader("content-disposition", "attachment;filename=CCP_Checklist_Export.xls");
                    Response.Charset = "";
                    Response.ContentType = "application/vnd.ms-excel";

                    using (StringWriter sw = new StringWriter())
                    {
                        HtmlTextWriter hw = new HtmlTextWriter(sw);

                        // Create an HTML table to export data using GridView
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
                    // Notify the user if no data is available for export
                    ShowNotification("No Data", "No data available to export.", "info");
                }
            }
            catch (Exception ex)
            {
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);
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

            // Query to fetch data from the TRN_CCP_Checklist table
            string query = "SELECT * FROM TRN_CCP_Checklist"; // Adjust this query as per your table structure

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
        //    StringBuilder queryBuilder = new StringBuilder("SELECT * FROM [dbo].[TRN_CCP_Checklist] WHERE 1 = 1");
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

        //        // Convert DataTable to List<TRN_CCP_ChecklistInfo>
        //        List<ReportInfo> dataSave = new List<ReportInfo>();
        //        foreach (DataRow row in filteredData.Rows)
        //        {
        //            ReportInfo info = new ReportInfo
        //            {
        //                Id = row["Id"] == DBNull.Value ? 0 : Convert.ToInt32(row["Id"]),
        //                CcpId = row["CcpId"] == DBNull.Value ? string.Empty : row["CcpId"].ToString(),
        //                FormID = row["FormID"] == DBNull.Value ? 0 : Convert.ToInt32(row["FormID"]),
        //                PlantId = row["PlantId"] == DBNull.Value ? string.Empty : row["PlantId"].ToString(),
        //                Line = row["Line"] == DBNull.Value ? string.Empty : row["Line"].ToString(),
        //                ProductCategory = row["ProductCategory"] == DBNull.Value ? string.Empty : row["ProductCategory"].ToString(),
        //                ProductBrand = row["ProductBrand"] == DBNull.Value ? string.Empty : row["ProductBrand"].ToString(),
        //                SKUID = row["SKUID"] == DBNull.Value ? string.Empty : row["SKUID"].ToString(),
        //                SubmittedById = row["SubmittedById"] == DBNull.Value ? 0 : Convert.ToInt32(row["SubmittedById"]),
        //                SubmittedByEmployeeCode = row["SubmittedByEmployeeCode"] == DBNull.Value ? string.Empty : row["SubmittedByEmployeeCode"].ToString(),
        //                SubmittedDate = row["SubmittedDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["SubmittedDate"]),
        //                SubmittedTime = row["SubmittedTime"] == DBNull.Value ? TimeSpan.Zero : TimeSpan.Parse(row["SubmittedTime"].ToString()),
        //                Shift = row["Shift"] == DBNull.Value ? string.Empty : row["Shift"].ToString(),
        //                ViewMode = row["ViewMode"] == DBNull.Value ? 0 : Convert.ToInt32(row["ViewMode"]),
        //                DeleteMode = row["DeleteMode"] == DBNull.Value ? 0 : Convert.ToInt32(row["DeleteMode"]),
        //                Approver1EmployeeCode = row["Approver1EmployeeCode"] == DBNull.Value ? string.Empty : row["Approver1EmployeeCode"].ToString(),
        //                Approver1_Status = row["Approver1_Status"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["Approver1_Status"]),
        //                Approver1_TimeStamp = row["Approver1_TimeStamp"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["Approver1_TimeStamp"]),
        //                Approver2EmployeeCode = row["Approver2EmployeeCode"] == DBNull.Value ? string.Empty : row["Approver2EmployeeCode"].ToString(),
        //                Approver2_Status = row["Approver2_Status"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["Approver2_Status"]),
        //                Approver2_TimeStamp = row["Approver2_TimeStamp"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["Approver2_TimeStamp"]),
        //                DottedLineApproverEmployeeCode = row["DottedLineApproverEmployeeCode"] == DBNull.Value ? string.Empty : row["DottedLineApproverEmployeeCode"].ToString(),
        //                DottedApprover_Status = row["DottedApprover_Status"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["DottedApprover_Status"]),
        //                DottedApprover_TimeStamp = row["DottedApprover_TimeStamp"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["DottedApprover_TimeStamp"]),
        //                MetalCheck = row["MetalCheck"] == DBNull.Value ? string.Empty : row["MetalCheck"].ToString(),
        //                SieveCheck = row["SieveCheck"] == DBNull.Value ? string.Empty : row["SieveCheck"].ToString(),
        //                FF_Status = row["FF_Status"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["FF_Status"]),
        //                FF_Remarks = row["FF_Remarks"] == DBNull.Value ? string.Empty : row["FF_Remarks"].ToString(),
        //                NFE_Status = row["NFE_Status"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["NFE_Status"]),
        //                NFE_Remarks = row["NFE_Remarks"] == DBNull.Value ? string.Empty : row["NFE_Remarks"].ToString(),
        //                SS_Status = row["SS_Status"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["SS_Status"]),
        //                SS_Remarks = row["SS_Remarks"] == DBNull.Value ? string.Empty : row["SS_Remarks"].ToString(),
        //                MD_Remarks = row["MD_Remarks"] == DBNull.Value ? string.Empty : row["MD_Remarks"].ToString(),
        //                T1_Status = row["T1_Status"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["T1_Status"]),
        //                T2_Status = row["T2_Status"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["T2_Status"]),
        //                T3_Status = row["T3_Status"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["T3_Status"]),
        //                T4_Status = row["T4_Status"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["T4_Status"]),
        //                Final_Status = row["Final_Status"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["Final_Status"])
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


        protected void btn_view_submit_Click(object sender, EventArgs e)
        {
            try
            {
                // Retrieve date range from textboxes
                DateTime? dateFrom = string.IsNullOrEmpty(TXT_PackageDate.Text) ? (DateTime?)null : DateTime.ParseExact(TXT_PackageDate.Text, "yyyy-MM-dd", null);
                DateTime? dateTo = string.IsNullOrEmpty(TextBox1.Text) ? (DateTime?)null : DateTime.ParseExact(TextBox1.Text, "yyyy-MM-dd", null);

                var queryBuilder = new StringBuilder(@"
                    SELECT 
                        c.ID AS DBID,
                        c.FormID as FormID,
                        p.plant_name AS PlantName,
                        c.SubmittedByEmployeeCode as EmpCode,
                        u.EmployeeName AS EmpName,
                        c.SubmittedDate as SDate,
                        c.SubmittedTime as STime,
                        c.Shift as SShift,
                        c.MD_Remarks as Remarks,
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
                        TRN_CCP_Checklist c
                    LEFT JOIN 
                        MST_PlantDetails p ON c.PlantId = p.plant_id
                    LEFT JOIN
                        MST_UserMaster u ON c.SubmittedByEmployeeCode = u.EmployeeCode
                    WHERE 
                        1 = 1
                ");

                var parameters = new List<SqlParameter>();

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    queryBuilder.Append(" AND c.SubmittedDate BETWEEN @DateFrom AND @DateTo");
                    parameters.Add(new SqlParameter("@DateFrom", SqlDbType.Date) { Value = dateFrom.Value.Date });
                    parameters.Add(new SqlParameter("@DateTo", SqlDbType.Date) { Value = dateTo.Value.Date });
                }

                if (!string.IsNullOrEmpty(DDL_Plant.SelectedValue) && DDL_Plant.SelectedValue != "0")
                {
                    queryBuilder.Append(" AND c.PlantId = @PlantId");
                    parameters.Add(new SqlParameter("@PlantId", SqlDbType.Int) { Value = DDL_Plant.SelectedValue });
                }
                queryBuilder.Append(" ORDER BY c.SubmittedDate DESC, c.SubmittedTime DESC");

                // Fetch filtered data
                var filteredData = GetDataFromTable(queryBuilder.ToString(), parameters.ToArray());

                // Map to ReportInfo list
                //var dataSave = filteredData.AsEnumerable().Select(row => new ReportInfo
                //{
                //    DBID = row.Field<int>("DBID"),
                //    FormID = row.Field<int>("FormID"),
                //    RecordID = row.Field<string>("RecordID"),
                //    PlantName = row.Field<string>("PlantName"),
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

                //if (!filteredData.Any())
                //{
                //    ShowNotification("No Data", "No records found for the selected criteria.", "info");
                //}
            }
            catch (Exception ex)
            {
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);
                ShowNotification("Error", "An unexpected error occurred.", "error");
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
            Response.Redirect("home.aspx",false);
        }

        protected void btn_view_Reset_Click(object sender, EventArgs e)
        {
            Response.Redirect("vm_ccp_checklist.aspx", false);
        }

        protected void GridView1_RowCommand(object sender, System.Web.UI.WebControls.GridViewCommandEventArgs e)
        {
            int rowIndex = Convert.ToInt32(e.CommandArgument);
            GridViewRow row = GridView1.Rows[rowIndex];
            string dbid = (row.FindControl("lbl_rowid") as Label).Text;
            if (e.CommandName == "View")
            {
                //Add View Details Page for CCP Checklist
                Response.Redirect("CCP_Checklist_detailed.aspx?ID=" + dbid + "&VM=0", false);
                //Response.Redirect("#", false);
            }
        }
    }
}