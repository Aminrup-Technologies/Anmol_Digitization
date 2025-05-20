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
using ClosedXML.Excel;
using System.IO;

namespace AnmolDristi
{
    public partial class Aata_Maida_Approval : System.Web.UI.Page
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

                    lbl_docname.Text = "Search Filter for QC Aata Maida Report";
                    lbl_viewname.Text = "View and Search for Detailed View || ";

                    MaterialBinder();
                    loadAlldata();
                }

            }
        }

        private void MaterialBinder()
        {
            string query = "SELECT Material_Id, Material_Name FROM RM_Material where Class = 6";
            string textField = "Material_Name";
            string valueField = "Material_Id";

            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_Material, textField, valueField, out recordsBound);

            if (!recordsBound)
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_Material);

                string MaterialBinder_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'An error occurred!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowRegionBinderErrorNotification", MaterialBinder_Error_script, false);
            }
        }

        protected void DDL_Material_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Material.SelectedIndex != 0)
            {
                string selectedMaterialValue = DDL_Material.SelectedValue.ToString();
                PlantBinder();
                DataLoader();
                // Call method to show relevant controls based on selected material
                //DivBinders(selectedMaterialValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_Plant);

                string DDL_Material_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'Invalid Selection!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowRegionInvalidErrorNotification", DDL_Material_Error_script, false);
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
                DataLoader();
            }
            else
            {
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



        protected void ReportbtnCancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("qaqc_qcapprovals.aspx");
        }

        protected void ReportbtnReset_Click(object sender, EventArgs e)
        {
            Response.Redirect("Aata_Maida_Submitter.aspx");
        }

        protected void ReportbtnSubmit_Click(object sender, EventArgs e)
        {
            DataLoader();
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
                        AMFID as RecordID,
                        m.Material_Name AS MaterialName,
                        p.plant_name AS PlantName,
                        c.SubmittedByEmployeeCode as EmpCode,
                        u.EmployeeName AS EmpName,
                        c.SubmittedDate as SDate,
                        c.SubmittedTime as STime,
                        c.ProductBrand,
                        c.Quantity,
                        c.Supplier_Name,
                        c.Size,
                        c.Size_Remarks,
                        c.Challan_No,
                        c.Challan_Dates,
                        c.Mfg_Dates,
                        c.BatchNo,
                        c.LotNo,
                        c.VehicleNo,
                        CASE 
		                    WHEN c.MfgYesNo = 1 THEN 'Ok'
		                    WHEN c.MfgYesNo = 0 THEN 'Not Ok'
	                    END AS MfgYesNo,
	                    c.MfgYesNoRemarks
                        c.MfgName,
                        CASE 
		                    WHEN c.BBDateYesNo = 1 THEN 'Ok'
		                    WHEN c.BBDateYesNo = 0 THEN 'Not Ok'
	                    END AS BBDateYesNo,
	                    c.BBDateYesNoRemarks
                        c.BB_Dates,
	                    CASE 
		                    WHEN c.fssaiYesNo = 1 THEN 'Ok'
		                    WHEN c.fssaiYesNo = 0 THEN 'Not Ok'
	                    END AS fssaiYesNo,
	                    c.fssaiYesNoRemarks
                        c.FssaiNo,
	                    CASE 
		                    WHEN c.Fssai_Logo = 1 THEN 'Present'
		                    WHEN c.Fssai_Logo = 0 THEN 'Absent'
	                    END AS Fssai_Logo,
	                    CASE 
		                    WHEN c.Veg_Logo = 1 THEN 'Present'
		                    WHEN c.Veg_Logo = 0 THEN 'Absent'
	                    END AS Veg_Logo,
	                    CASE 
		                    WHEN c.Packing_Condition = 1 THEN 'Ok'
		                    WHEN c.Packing_Condition = 0 THEN 'Not Ok'
	                    END AS Packing_Condition,
	                    c.PackingCondition_Remarks
	                    CASE 
		                    WHEN c.ColorApp = 1 THEN 'Ok'
		                    WHEN c.ColorApp = 0 THEN 'Not Ok'
	                    END AS ColorApp,
	                    c.ColorApp_Remarks
	                    CASE 
		                    WHEN c.Odour = 1 THEN 'Ok'
		                    WHEN c.Odour = 0 THEN 'Not Ok'
	                    END AS Odour,
	                    c.Odour_Remarks
	                    CASE 
		                    WHEN c.TasteFlavor = 1 THEN 'Ok'
		                    WHEN c.TasteFlavor = 0 THEN 'Not Ok'
	                    END AS TasteFlavor,
	                    c.TasteFlavor_Remarks
	                    CASE 
		                    WHEN c.Impurities = 1 THEN 'Ok'
		                    WHEN c.Impurities = 0 THEN 'Not Ok'
	                    END AS Impurities,
	                    c.Impurities_Remarks
                        c.Moisture,
                        c.Moisture_Remarks,
                        c.TotalAsh,
                        c.Ash_Remarks,
                        c.InsolubleAsh,
                        c.InsolubleAsh_Remarks,
                        c.GlutentContent,
                        c.GlutentContent_Remarks,
                        c.AlcoholicAcidity,
                        c.AlcoholicAcidity_Remarks,
                        c.Absorption,
                        c.Absorption_Remarks,
                        c.Sedimentation,
                        c.Sedimentation_Remarks,
	                    CASE 
		                    WHEN c.Grittiness = 1 THEN 'Positive'
		                    WHEN c.Grittiness = 0 THEN 'Negative'
	                    END AS Grittiness,
	                    CASE 
		                    WHEN c.Grittiness_Remarks IS NULL OR c.Grittiness_Remarks = '' THEN 'No Remarks'
		                    ELSE c.Grittiness_Remarks
	                    END AS Grittiness_Remarks,
                        c.Acidity,
                        c.Acidity_Remarks,
                        c.Granularity,
                        c.Granularity_Remarks,
                        c.GranularityRetention,
                        c.GranularityRetention_Remarks,
                        c.Retention,
                        c.Retention_Remarks,
                        c.Bromate,
                        c.Material_Image,
                        c.Remarks,
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
                            TRN_Aata_Maida c
                        LEFT JOIN
                            RM_MATERIAL m ON c.MaterialName = m.Material_Id
                        LEFT JOIN 
                            MST_PlantDetails p ON c.PlantName = p.plant_id
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
                        ViewState["ExportData"] = dt;
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
                    AMFID as RecordID,
	                m.Material_Name AS MaterialName,
                    p.plant_name AS PlantName,
                    c.SubmittedByEmployeeCode AS EmpCode,
                    u.EmployeeName AS EmpName,
                    c.SubmittedDate AS SDate,
                    c.SubmittedTime AS STime,
                    c.ProductBrand,
                    c.Quantity,
                    c.Supplier_Name,
                    c.Size,
                    c.Size_Remarks,
                    c.Challan_No,
                    c.Challan_Dates,
                    c.Mfg_Dates,
                    c.BatchNo,
                    c.LotNo,
                    c.VehicleNo,
                    c.VehicleNo,
                    CASE 
		                WHEN c.MfgYesNo = 1 THEN 'Ok'
		                WHEN c.MfgYesNo = 0 THEN 'Not Ok'
	                END AS MfgYesNo,
	                c.MfgYesNoRemarks
                    c.MfgName,
                    CASE 
		                WHEN c.BBDateYesNo = 1 THEN 'Ok'
		                WHEN c.BBDateYesNo = 0 THEN 'Not Ok'
	                END AS BBDateYesNo,
	                c.BBDateYesNoRemarks
                    c.BB_Dates,
	                CASE 
		                WHEN c.fssaiYesNo = 1 THEN 'Ok'
		                WHEN c.fssaiYesNo = 0 THEN 'Not Ok'
	                END AS fssaiYesNo,
	                c.fssaiYesNoRemarks
                    c.FssaiNo,
	                CASE 
		                WHEN c.Fssai_Logo = 1 THEN 'Present'
		                WHEN c.Fssai_Logo = 0 THEN 'Absent'
	                END AS Fssai_Logo,
	                CASE 
		                WHEN c.Veg_Logo = 1 THEN 'Present'
		                WHEN c.Veg_Logo = 0 THEN 'Absent'
	                END AS Veg_Logo,
	                CASE 
		                WHEN c.Packing_Condition = 1 THEN 'Ok'
		                WHEN c.Packing_Condition = 0 THEN 'Not Ok'
	                END AS Packing_Condition,
	                c.PackingCondition_Remarks
	                CASE 
		                WHEN c.ColorApp = 1 THEN 'Ok'
		                WHEN c.ColorApp = 0 THEN 'Not Ok'
	                END AS ColorApp,
	                c.ColorApp_Remarks
	                CASE 
		                WHEN c.Odour = 1 THEN 'Ok'
		                WHEN c.Odour = 0 THEN 'Not Ok'
	                END AS Odour,
	                c.Odour_Remarks
	                CASE 
		                WHEN c.TasteFlavor = 1 THEN 'Ok'
		                WHEN c.TasteFlavor = 0 THEN 'Not Ok'
	                END AS TasteFlavor,
	                c.TasteFlavor_Remarks
	                CASE 
		                WHEN c.Impurities = 1 THEN 'Ok'
		                WHEN c.Impurities = 0 THEN 'Not Ok'
	                END AS Impurities,
	                c.Impurities_Remarks
                    c.Moisture,
                    c.Moisture_Remarks,
                    c.TotalAsh,
                    c.Ash_Remarks,
                    c.InsolubleAsh,
                    c.InsolubleAsh_Remarks,
                    c.GlutentContent,
                    c.GlutentContent_Remarks,
                    c.AlcoholicAcidity,
                    c.AlcoholicAcidity_Remarks,
                    c.Absorption,
                    c.Absorption_Remarks,
                    c.Sedimentation,
                    c.Sedimentation_Remarks,
	                CASE 
		                WHEN c.Grittiness = 1 THEN 'Positive'
		                WHEN c.Grittiness = 0 THEN 'Negative'
	                END AS Grittiness,
	                CASE 
		                WHEN c.Grittiness_Remarks IS NULL OR c.Grittiness_Remarks = '' THEN 'No Remarks'
		                ELSE c.Grittiness_Remarks
	                END AS Grittiness_Remarks,
                    c.Acidity,
                    c.Acidity_Remarks,
                    c.Granularity,
                    c.Granularity_Remarks,
                    c.GranularityRetention,
                    c.GranularityRetention_Remarks,
                    c.Retention,
                    c.Retention_Remarks,
                    c.Bromate,
                    c.Material_Image,
                    c.Remarks AS Remarks,
                    c.Approver1EmployeeCode AS L1,
                    c.Approver1_Status,
                    c.Approver1_TimeStamp,
                    c.Approver2EmployeeCode AS L2,
                    c.Approver2_Status,
                    c.Approver2_TimeStamp,
                    c.DottedLineApproverEmployeeCode AS L3,
                    c.DottedApprover_Status,
                    c.DottedApprover_TimeStamp
                FROM TRN_Aata_Maida c
                LEFT JOIN RM_MATERIAL m ON c.MaterialName = m.Material_Id
                LEFT JOIN MST_PlantDetails p ON c.PlantName = p.plant_id
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

            if (!string.IsNullOrEmpty(DDL_Material.SelectedValue) && DDL_Material.SelectedValue != "0")
            {
                queryBuilder.Append(" AND c.MaterialName = @Material_Id");
                parameters.Add(new SqlParameter("@Material_Id", SqlDbType.Int) { Value = DDL_Material.SelectedValue });
            }

            if (!string.IsNullOrEmpty(DDL_Plant.SelectedValue) && DDL_Plant.SelectedValue != "0")
            {
                queryBuilder.Append(" AND c.PlantName = @PlantId");
                parameters.Add(new SqlParameter("@PlantId", SqlDbType.Int) { Value = DDL_Plant.SelectedValue });
            }

            //if (!string.IsNullOrEmpty(DDL_PlantLine.SelectedValue) && DDL_PlantLine.SelectedValue != "0")
            //{
            //    queryBuilder.Append(" AND c.Line = @LineName");
            //    parameters.Add(new SqlParameter("@LineName", SqlDbType.Int) { Value = DDL_PlantLine.SelectedValue });
            //}

            //if (!string.IsNullOrEmpty(DDL_ProductCategory.SelectedValue) && DDL_ProductCategory.SelectedValue != "0")
            //{
            //    queryBuilder.Append(" AND c.ProductCategory = @ProductCategory");
            //    parameters.Add(new SqlParameter("@ProductCategory", SqlDbType.Int) { Value = DDL_ProductCategory.SelectedValue });
            //}

            //if (!string.IsNullOrEmpty(DDL_ProductBrand.SelectedValue) && DDL_ProductBrand.SelectedValue != "0")
            //{
            //    queryBuilder.Append(" AND c.ProductBrand = @ProductBrand");
            //    parameters.Add(new SqlParameter("@ProductBrand", SqlDbType.Int) { Value = DDL_ProductBrand.SelectedValue });
            //}

            queryBuilder.Append(" ORDER BY c.SubmittedDate DESC, c.SubmittedTime DESC");

            try
            {
                DataTable filteredData = GetDataFromTable(queryBuilder.ToString(), parameters.ToArray());
                GridView1.DataSource = filteredData;
                ViewState["ExportData"] = filteredData;
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

        protected void GridView1_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "View")
            {
                // Get the DBID from the CommandArgument.
                int id = Convert.ToInt32(e.CommandArgument);

                if (id > 0)
                {
                    // Redirect with the correct DBID.
                    Response.Redirect("Aata_Maida_Detailed.aspx?Id=" + id);
                }
                else
                {
                    // Handle cases where DBID is not valid.
                    // Show an error message or log the issue.
                    System.Diagnostics.Debug.WriteLine("Invalid DBID passed: " + e.CommandArgument);
                }
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
                        wb.Worksheets.Add(dt, "QCAataMaidaReport");

                        Response.Clear();
                        Response.Buffer = true;
                        Response.Charset = "";
                        Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        Response.AddHeader("content-disposition", "attachment;filename=QCAataMaidaRecords.xlsx");

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


    }
}