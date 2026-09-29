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
	public partial class RM_Class2_Submitter : System.Web.UI.Page
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

					lbl_docname.Text = "Search Filter for QC RM Class 2 Report";
					lbl_viewname.Text = "View and Search for Detailed View || ";

					MaterialBinder();
					loadAlldata();
				}

			}

		}

		private void MaterialBinder()
		{
			string query = "SELECT Material_Id, Material_Name FROM RM_Material where Class = 2";
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
			Response.Redirect("qaqc_qchome_rpt.aspx");
		}

		protected void ReportbtnReset_Click(object sender, EventArgs e)
		{
			Response.Redirect("RM_Class2_Submitter.aspx");
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
					SELECT
					c2.[Id] AS DBID,
                        c2.[FormID] AS Form_ID,
                        c2.[RMFID] AS Record_ID,
                        c2.[SubmittedDate] AS SDate,
                        c2.[SubmittedTime] AS STime,
                        c2.[SubmittedByEmployeeCode] AS EmpCode,
                        u.EmployeeName AS EmpName,
                        m.Material_Name AS MaterialName,
                        p.plant_name AS PlantName,
                        c2.[Product] AS Product,
                        c2.[Brand] AS Brand,
                        c2.[Supplier_Name] AS Supplier_Name,
                        c2.[Challan_No] AS Challan_No,
                        c2.[Challan_Date] AS Challan_Date,
                        c2.[Lot_No] AS Lot_No,
                        c2.[Vehicle_No] AS Vehicle_No,
                        c2.[Batch_No] AS Batch_No,
                        c2.[Grade] AS Grade,
                        c2.[Color] AS Color,
                        c2.[CommentsForColor] AS Color_Remarks,
                        c2.[Smell] AS Smell,
                        c2.[CommentsForSmell] AS Smell_Remarks,
                        c2.[Taste] AS Taste,
                        c2.[CommentsForTaste] AS Taste_Remarks,
                        c2.[Consistency] AS Consistency,
                        c2.[CommentsForConsistency] AS Consistency_Remarks,
                        c2.[Foreign_Matter_Impurities] AS Impurities,
                        c2.[MP_CP] AS MP_CP,
                        c2.[PV] AS PV,
                        c2.[Moisture] AS Moisture,
                        c2.[Iodine_Val] AS Iodine_Value,
                        c2.[SAP_Val] AS SAP_Value,
                        c2.[Acid_Val] AS Acid_Value,
                        c2.[KriesTest] AS Kries_Test,
                        c2.[CommentsForKriesTest] AS Kries_Remarks,
                        c2.[OilTest] AS Oil_Test,
                        c2.[CommentsForOilTest] AS Oil_Remarks,
                        c2.[AppStatus] AS Approval_Status,
                        c2.[AppStatusRemarks] AS Approval_Remarks,
                        c2.[Material_Image] AS Material_Image,
                        c2.[Approver1EmployeeCode] AS Approver1_EmpCode,
                        a1.EmployeeName AS Approver1_Name,
                        c2.[Approver1_Status] AS Approver1_Status,
                        c2.[Approver1_TimeStamp] AS Approver1_Timestamp,
                        c2.[Approver2EmployeeCode] AS Approver2_EmpCode,
                        a2.EmployeeName AS Approver2_Name,
                        c2.[Approver2_Status] AS Approver2_Status,
                        c2.[Approver2_TimeStamp] AS Approver2_Timestamp,
                        c2.[DottedLineApproverEmployeeCode] AS DottedLineApprover_EmpCode,
                        ad.EmployeeName AS DottedLineApprover_Name,
                        c2.[DottedApprover_Status] AS DottedLineApprover_Status,
                        c2.[DottedApprover_TimeStamp] AS DottedLineApprover_Timestamp
                    FROM 
                        [TRN_RM_CLASS_2] c2
                    LEFT JOIN 
                        RM_MATERIAL m ON c2.MaterialName = m.Material_Id
                    LEFT JOIN 
                        MST_PlantDetails p ON c2.PlantName = p.plant_id
                    LEFT JOIN 
                        MST_UserMaster u ON c2.SubmittedByEmployeeCode = u.EmployeeCode
                    LEFT JOIN 
                        MST_UserMaster a1 ON c2.Approver1EmployeeCode = a1.EmployeeCode
                    LEFT JOIN 
                        MST_UserMaster a2 ON c2.Approver2EmployeeCode = a2.EmployeeCode
                    LEFT JOIN 
                        MST_UserMaster ad ON c2.DottedLineApproverEmployeeCode = ad.EmployeeCode
					WHERE 
                        YEAR(c2.SubmittedDate) = YEAR(GETDATE()) 
                        AND MONTH(c2.SubmittedDate) = MONTH(GETDATE())
						AND 1 = 1
					ORDER BY 
						c2.[SubmittedDate] DESC, 
						c2.[SubmittedTime] DESC;";

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
                    c2.Id AS DBID,
                    c2.FormID AS FormID,
                    c2.RMFID AS RecordID,
                    m.Material_Name AS MaterialName,
                    p.plant_name AS PlantName,
                    c2.SubmittedByEmployeeCode AS EmpCode,
                    u.EmployeeName AS EmpName,
                    c2.SubmittedDate AS SDate,
                    c2.SubmittedTime AS STime,
                    c2.Product,
                    c2.Brand,
                    c2.Supplier_Name,
                    c2.Challan_No,
                    c2.Challan_Date,
                    c2.Lot_No,
                    c2.Vehicle_No,
                    c2.Batch_No,
                    c2.Grade,
                    CASE 
                        WHEN c2.Color = 1 THEN 'Ok'
                        WHEN c2.Color = 0 THEN 'Not Ok'
                        ELSE 'NA'
                    END AS ColorStatus,
                    c2.CommentsForColor,
                    CASE 
                        WHEN c2.Smell = 1 THEN 'Ok'
                        WHEN c2.Smell = 0 THEN 'Not Ok'
                        ELSE 'NA'
                    END AS SmellStatus,
                    c2.CommentsForSmell,
                    CASE 
                        WHEN c2.Taste = 1 THEN 'Ok'
                        WHEN c2.Taste = 0 THEN 'Not Ok'
                        ELSE 'NA'
                    END AS TasteStatus,
                    c2.CommentsForTaste,
                    CASE 
                        WHEN c2.Consistency = 1 THEN 'Ok'
                        WHEN c2.Consistency = 0 THEN 'Not Ok'
                        ELSE 'NA'
                    END AS ConsistencyStatus,
                    c2.CommentsForConsistency,
                    c2.Foreign_Matter_Impurities,
                    c2.MP_CP,
                    c2.PV,
                    c2.Moisture,
                    c2.Iodine_Val,
                    c2.SAP_Val,
                    c2.Acid_Val,
                    CASE 
                        WHEN c2.KriesTest = 1 THEN 'Ok'
                        WHEN c2.KriesTest = 0 THEN 'Not Ok'
                        ELSE 'NA'
                    END AS KriesTestStatus,
                    c2.CommentsForKriesTest,
                    CASE 
                        WHEN c2.OilTest = 1 THEN 'Ok'
                        WHEN c2.OilTest = 0 THEN 'Not Ok'
                        ELSE 'NA'
                    END AS OilTestStatus,
                    c2.CommentsForOilTest,
                    CASE 
                        WHEN c2.AppStatus = 1 THEN 'Approved'
                        WHEN c2.AppStatus = 0 THEN 'Rejected'
                        ELSE 'NA'
                    END AS ApprovalStatus,
                    c2.AppStatusRemarks,
                    c2.Material_Image,
                    c2.Approver1EmployeeCode AS L1,
                    c2.Approver1_Status,
                    c2.Approver1_TimeStamp,
                    c2.Approver2EmployeeCode AS L2,
                    c2.Approver2_Status,
                    c2.Approver2_TimeStamp,
                    c2.DottedLineApproverEmployeeCode AS L3,
                    c2.DottedApprover_Status,
                    c2.DottedApprover_TimeStamp,
                    c2.ViewMode,
                    c2.DeleteMode,
                    c2.Submission_Status
                FROM TRN_RM_CLASS_2 c2
                LEFT JOIN RM_MATERIAL m ON c2.MaterialName = m.Material_Id
                LEFT JOIN MST_PlantDetails p ON c2.PlantName = p.plant_id
                LEFT JOIN MST_UserMaster u ON c2.SubmittedByEmployeeCode = u.EmployeeCode
                WHERE 1 = 1");

            var parameters = new List<SqlParameter>();

			// Add filters for date range
			if (dateFrom.HasValue)
			{
				queryBuilder.Append(" AND c2.SubmittedDate >= @DateFrom");
				parameters.Add(new SqlParameter("@DateFrom", SqlDbType.Date) { Value = dateFrom.Value.Date });
			}

			if (dateTo.HasValue)
			{
				queryBuilder.Append(" AND c2.SubmittedDate <= @DateTo");
				parameters.Add(new SqlParameter("@DateTo", SqlDbType.Date) { Value = dateTo.Value.Date });
			}

            //if (!string.IsNullOrEmpty(DDL_Material.SelectedValue) && DDL_Material.SelectedValue != "0")
            //{
            //	queryBuilder.Append(" AND c.MaterialName = @Material_Id");
            //	parameters.Add(new SqlParameter("@Material_Id", SqlDbType.Int) { Value = DDL_Material.SelectedValue });
            //}

            //if (!string.IsNullOrEmpty(DDL_Plant.SelectedValue) && DDL_Plant.SelectedValue != "0")
            //{
            //	queryBuilder.Append(" AND c.PlantName = @PlantId");
            //	parameters.Add(new SqlParameter("@PlantId", SqlDbType.Int) { Value = DDL_Plant.SelectedValue });
            //}

            if (!string.IsNullOrEmpty(DDL_Material.SelectedValue) && DDL_Material.SelectedValue != "0")
            {
                queryBuilder.Append(" AND c2.MaterialName = @Material_Id");
                parameters.Add(new SqlParameter("@Material_Id", SqlDbType.VarChar) { Value = DDL_Material.SelectedValue });
            }

            if (!string.IsNullOrEmpty(DDL_Plant.SelectedValue) && DDL_Plant.SelectedValue != "0")
            {
                queryBuilder.Append(" AND c2.PlantName = @PlantId");
                parameters.Add(new SqlParameter("@PlantId", SqlDbType.VarChar) { Value = DDL_Plant.SelectedValue });
            }

            queryBuilder.Append(" ORDER BY c2.SubmittedDate DESC, c2.SubmittedTime DESC");

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
                    Response.Redirect("RM_Class2_Detailed.aspx?Id=" + id + "&source=submitter");
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
                        wb.Worksheets.Add(dt, "QCRMClass2Report");

                        Response.Clear();
                        Response.Buffer = true;
                        Response.Charset = "";
                        Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        Response.AddHeader("content-disposition", "attachment;filename=QCRMClass2Records.xlsx");

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