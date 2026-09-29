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
using ClosedXML.Excel;

namespace AnmolDristi
{
	public partial class RM_Class2_Approval : System.Web.UI.Page
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

					lbl_docname.Text = "Search Filter for QC RM Class 2 Report";


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
			Response.Redirect("home.aspx");
		}

		protected void ReportbtnReset_Click(object sender, EventArgs e)
		{
			Response.Redirect("RM_Class2_Approval.aspx");
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
					'NA' as RecordID,
					m.Material_Name AS Material_Name,
					p.plant_name AS Plant_Name,
					c.SubmittedByEmployeeCode as EmpCode,
					u.EmployeeName AS EmpName,
					c.SubmittedDate as Submitted_Date,
					c.SubmittedTime as Submitted_Time,
					c.Approver1EmployeeCode as L1,
					c.Approver1_Status as L1_Status,
					c.Approver1_TimeStamp as L1_TimeStamp,
					c.Approver2EmployeeCode as L2,
					c.Approver2_Status as L2_Status,
					c.Approver2_TimeStamp L2_TimeStamp,
					c.DottedLineApproverEmployeeCode as L3,
					c.DottedApprover_Status as L3_Status,
					c.DottedApprover_TimeStamp AS L3_TimeStamp
					FROM 
						TRN_RM_CLASS_2 c
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
						ViewState["ExportData"] = dt;
						if (dt.Rows.Count > 0)
						{
							GridView1.DataSource = dt;
							GridView1.DataBind();
						}
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
					m.Material_Name AS Material_Name,
					p.plant_name AS Plant_Name,
					c.SubmittedByEmployeeCode AS EmpCode,
					u.EmployeeName AS EmpName,
					c.SubmittedDate AS Submitted_Date,
					c.SubmittedTime AS Submitted_Time,
					'No Comment' AS Remarks,
					c.Approver1EmployeeCode AS L1,
					c.Approver1_Status as L1_Status,
					c.Approver1_TimeStamp as L1_TimeStamp,
					c.Approver2EmployeeCode as L2,
					c.Approver2_Status as L2_Status,
					c.Approver2_TimeStamp L2_TimeStamp,
					c.DottedLineApproverEmployeeCode as L3,
					c.DottedApprover_Status as L3_Status,
					c.DottedApprover_TimeStamp AS L3_TimeStamp
					
				FROM TRN_RM_CLASS_2 c
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

			queryBuilder.Append(" ORDER BY c.SubmittedDate DESC, c.SubmittedTime DESC");

			try
			{
				DataTable filteredData = GetDataFromTable(queryBuilder.ToString(), parameters.ToArray());
                ViewState["ExportData"] = filteredData;
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
		protected void GridView1_RowCommand(object sender, GridViewCommandEventArgs e)
		{
			if (e.CommandName == "View")
			{
				// Get the DBID from the CommandArgument.
				int id = Convert.ToInt32(e.CommandArgument);

				if (id > 0)
				{
					// Redirect with the correct DBID.
					Response.Redirect("RM_Class2_Detailed.aspx?Id=" + id);
				}
				else
				{
					// Handle cases where DBID is not valid.
					// Show an error message or log the issue.
					System.Diagnostics.Debug.WriteLine("Invalid DBID passed: " + e.CommandArgument);
				}
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
						wb.Worksheets.Add(dt, "RM_Class2_Report");

						Response.Clear();
						Response.Buffer = true;
						Response.Charset = "";
						Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
						Response.AddHeader("content-disposition", "attachment;filename=RM_Class2_Report.xlsx");

						using (MemoryStream MyMemoryStream = new MemoryStream())
						{
							wb.SaveAs(MyMemoryStream);
							MyMemoryStream.WriteTo(Response.OutputStream);
							Response.Flush();
							//Response.End();
                            HttpContext.Current.ApplicationInstance.CompleteRequest();
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