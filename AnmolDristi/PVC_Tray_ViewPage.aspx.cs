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
	public partial class PVC_Tray_ViewPage : System.Web.UI.Page
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
				ProductBrandsBinder(selectedPlantValue);

			}
			else
			{
				DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductBrand);

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
		private void ProductBrandsBinder(string selectedPlantValue)
		{
			// Construct the SQL query with parameters
			string query = "SELECT brand_id, brand_name FROM MST_LineCatBrands WHERE plant_id = @PlantId order by brand_id";
			string textField = "brand_name"; // Assuming this is the correct field for displaying in the DropDownList
			string valueField = "brand_id"; // Assuming this is the correct field for storing in the DropDownList

			// Create SQL parameters for plant_id and line_id
			SqlParameter[] parameters = new SqlParameter[]
			{
				new SqlParameter("@PlantId", selectedPlantValue)
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

			public string Material { get; set; }

			public string SubmittedDate { get; set; }
			public string SubmittedTime { get; set; }
			public string SubmittedById { get; set; }
			public string Supplier_Name { get; set; }
            public string SampleSize { get; set; }
            public string RjtdQty { get; set; }
			public string Challan_No { get; set; }
			public string Challan_Date { get; set; }
			public string Lot_No { get; set; }
			public string Vehicle_No { get; set; }
			public string Dimension_Obs_L { get; set; }
			public string Dimension_Obs_W { get; set; }
			public string Dimension_Obs_H { get; set; }
			public string GSM_Obs { get; set; }
			public string Remarks { get; set; }

		}
		private void BindGridView()
		{
			try
			{
				string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
				using (SqlConnection con = new SqlConnection(connectionString))
				{
					string query = @"SELECT * FROM TRN_PVC_Tray;";
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
				string query = @"SELECT * FROM TRN_PVC_Tray WHERE PlantName = @selectedPlantValue";
				using (SqlCommand cmd = new SqlCommand(query, con))
				{

					cmd.Parameters.AddWithValue("@selectedPlantValue", string.IsNullOrEmpty(DDL_Plant.SelectedValue) || DDL_Plant.SelectedValue == "0" ? (object)DBNull.Value : Convert.ToInt32(DDL_Plant.SelectedValue));

					//cmd.Parameters.AddWithValue("@ProductBrand", string.IsNullOrEmpty(DDL_ProductBrand.SelectedValue) || DDL_ProductBrand.SelectedValue == "0" ? (object)DBNull.Value : Convert.ToInt32(DDL_ProductBrand.SelectedValue));

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
						c.PVCID as RecordID,
						p.plant_name AS PlantName,
						c.ProductBrand AS Material,
						c.SubmittedByEmployeeCode as EmpCode,
						u.EmployeeName AS EmpName,
						c.SubmittedDate as SDate,
						c.SubmittedTime as STime,
						c.Approver1EmployeeCode as L1,
						c.Approver1_Status,
						c.Approver2EmployeeCode as L2,
						c.Approver2_Status,
						c.DottedLineApproverEmployeeCode as L3,
						c.DottedApprover_Status
					FROM
						TRN_PVC_Tray c
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
					c.PVCID as RecordID,
					p.plant_name AS PlantName,
					c.ProductBrand AS Material,
					c.SubmittedByEmployeeCode AS EmpCode,
					u.EmployeeName AS EmpName,
					c.SubmittedDate AS SDate,
					c.SubmittedTime AS STime,
					c.Approver1EmployeeCode AS L1,
					c.Approver1_Status,
					c.Approver2EmployeeCode AS L2,
					c.Approver2_Status,
					c.DottedLineApproverEmployeeCode AS L3,
					c.DottedApprover_Status
				FROM TRN_PVC_Tray c
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

			if (!string.IsNullOrEmpty(DDL_Plant.SelectedValue) && DDL_Plant.SelectedValue != "0")
			{
				queryBuilder.Append(" AND c.PlantName = @PlantId");
				parameters.Add(new SqlParameter("@PlantId", SqlDbType.Int) { Value = DDL_Plant.SelectedValue });
			}

			//if (!string.IsNullOrEmpty(DDL_ProductBrand.SelectedValue) && DDL_ProductBrand.SelectedValue != "0")
			//{
			//	queryBuilder.Append(" AND c.ProductBrand = @ProductBrand");
			//	parameters.Add(new SqlParameter("@ProductBrand", SqlDbType.Int) { Value = DDL_ProductBrand.SelectedValue });
			//}

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
				//Response.Redirect("vw_app_qcireport.aspx?ID=" + dbid + "&VM=1", false);
			}
		}

	}
}
