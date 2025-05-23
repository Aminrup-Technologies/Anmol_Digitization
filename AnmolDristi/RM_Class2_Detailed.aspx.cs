using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using DocumentFormat.OpenXml.Vml;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using DocumentFormat.OpenXml.Bibliography;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Database;

namespace AnmolDristi
{
	public partial class RM_Class2_Detailed : System.Web.UI.Page
	{
		public static Int32 RecordID = 0;

		public static string MaterialId = string.Empty;
		public static string MaterialName = string.Empty;
		public static string PlantId = string.Empty;
		public static string PlantName = string.Empty;
		public static string PlantLine = string.Empty;
		public static string ProductCategory = string.Empty;
		public static string CategoryBrand = string.Empty;
		public static string Color = string.Empty;

		public static string App1_Status = string.Empty;
		public static string App2_Status = string.Empty;
		public static string DottedApp_Status = string.Empty;

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
					MaterialBinder();

					//below to bind the report details from TRN Tables
					RecordID = Convert.ToInt32(Request.QueryString["ID"]);

					// Call the new method to load data
					LoadRecordData(RecordID);
				}

				string source = Request.QueryString["source"];

				if (source == "submitter")
				{
					BtnApprove.Visible = false;
					BtnReject.Visible = false;
					BtnBack.PostBackUrl = "~/RM_Class2_Submitter.aspx";
					Lbl_BasicbtnApprove.Text = "Click on your Action";

				}
				else
				{
					BtnBack.PostBackUrl = "~/RM_Class2_Approval.aspx";
				}
			}
		}

		private void LoadRecordData(int recordID)
		{
			DataTable dt = FetchRecordById(recordID);
			if (dt.Rows.Count > 0)
			{
				getDetails(dt);
			}
			else
			{
				// Handle case where no data is found
			}
		}

		private DataTable FetchRecordById(int id)
		{
			DataTable dt = new DataTable();
			string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
			string query = @"
						SELECT
						P.MaterialName as MaterialId,
						P.PlantName as PlantID,
						P.Brand,
						M.Material_Name,
						A.plant_name,
						P.*
					FROM
						TRN_RM_CLASS_2 P
					LEFT JOIN dbo.RM_MATERIAL M ON P.MaterialName = M.Material_Id
					LEFT JOIN dbo.MST_PlantDetails A ON P.PlantName = A.plant_id
					WHERE P.Id = @Id";

			using (SqlConnection con = new SqlConnection(connectionString))
			{
				using (SqlCommand cmd = new SqlCommand(query, con))
				{
					cmd.Parameters.AddWithValue("@Id", id);
					con.Open();
					using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
					{
						sda.Fill(dt);
					}
				}
			}
			return dt;
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

		void getDetails(DataTable dt)
		{

			if (dt.Rows.Count > 0)
			{
				DataRow row = dt.Rows[0];

				MaterialId = dt.Rows[0]["MaterialId"].ToString();
				MaterialName = dt.Rows[0]["Material_Name"].ToString();
				DDL_Material.SelectedItem.Text = MaterialName;
				DDL_Material.Enabled = false;

				PlantBinder();
				PlantId = dt.Rows[0]["PlantId"].ToString();
				PlantName = dt.Rows[0]["plant_name"].ToString();
				DDL_Plant.SelectedValue = PlantId;
				DDL_Plant.Enabled = false;


				TB_BrandName.Text = dt.Rows[0]["Brand"].ToString();
				TB_Supplier.Text = dt.Rows[0]["Supplier_Name"].ToString();

				TB_ChalanNo.Text = dt.Rows[0]["Challan_No"].ToString();
				TB_ChalanDate.Text = Convert.ToDateTime(dt.Rows[0]["Challan_Date"]).ToString("yyyy-MM-dd");
				TB_LotNo.Text = dt.Rows[0]["Lot_No"].ToString();
				TB_VehicleNo.Text = dt.Rows[0]["Vehicle_No"].ToString();
				TB_BatchNo.Text = dt.Rows[0]["Batch_No"].ToString();
				TB_Grade.Text = dt.Rows[0]["Grade"].ToString();

				RBL_Color.SelectedValue = dt.Rows[0]["Color"].ToString();
				TB_ColorRemarks.Text = dt.Rows[0]["CommentsForColor"].ToString();

				RBL_Smell.SelectedValue = dt.Rows[0]["Smell"].ToString();
				TB_Smell_Remarks.Text = dt.Rows[0]["CommentsForSmell"].ToString();

				//RBL_Appearance.SelectedValue = dt.Rows[0]["Appearance"].ToString();
				//TXB_Appearance_Remarks.Text = dt.Rows[0]["CommentsForAppearance"].ToString();

				RBL_TasteFlavor.SelectedValue = dt.Rows[0]["Taste"].ToString();
				TB_TasteFlavor_Remarks.Text = dt.Rows[0]["CommentsForTaste"].ToString();

				RBL_Consistency.SelectedValue = dt.Rows[0]["Taste"].ToString();
				TB_ConsistencyRemarks.Text = dt.Rows[0]["CommentsForTaste"].ToString();

				TB_Foreign_Impurities.Text = dt.Rows[0]["Foreign_Matter_Impurities"].ToString();

				TB_MP.Text = dt.Rows[0]["MP_CP"].ToString();
				TB_PV.Text = dt.Rows[0]["PV"].ToString();

				TB_Moisture.Text = dt.Rows[0]["Moisture"].ToString();
				TB_IodineVal.Text = dt.Rows[0]["Iodine_Val"].ToString();
				TB_SapVal.Text = dt.Rows[0]["SAP_Val"].ToString();
				TB_AcidValue.Text = dt.Rows[0]["Acid_Val"].ToString();

				RBL_KriesTest.Text = dt.Rows[0]["KriesTest"].ToString();
				TB_KriesTest_Remarks.Text = dt.Rows[0]["CommentsForKriesTest"].ToString();

				RBL_OilTest.Text = dt.Rows[0]["OilTest"].ToString();
				TB_OilTest_Remarks.Text = dt.Rows[0]["CommentsForOilTest"].ToString();

				RBL_AppStatus.SelectedValue = dt.Rows[0]["AppStatus"].ToString();
				TXB_AppStatus_Remarks.Text = dt.Rows[0]["AppStatusRemarks"].ToString();


				StandardValue_Binder(MaterialId, PlantId); // Texbox bind with std values
				string FormID = row["FormID"].ToString();
				LoadFormDetails(FormID, PlantId);

				// Assume the logged-in user's Employee Code is stored in a session variable
				string loggedInUserCode = Session["WORKMAN"].ToString(); // Example session variable

				// Retrieve approval statuses from the row
				App1_Status = row["Approver1_Status"].ToString();
				Approver1CodeLabel.Text = row["Approver1EmployeeCode"].ToString();

				App2_Status = row["Approver2_Status"].ToString();
				Approver2CodeLabel.Text = row["Approver2EmployeeCode"].ToString();

				DottedApp_Status = row["DottedApprover_Status"].ToString();
				DottedLineApproverCodeLabel.Text = row["DottedLineApproverEmployeeCode"].ToString();

				// Get the source parameter from the query string
				string source = Request.QueryString["source"];

				// Check if the page is accessed as a submitter
				bool isSubmitter = (source == "submitter");


				// Boolean flag to track if the logged-in user is one of the approvers
				bool isApprover = false;

				// Approver 1
				if (App1_Status == "0") // Pending
				{
					Approver1CodeLabel.ForeColor = System.Drawing.Color.Brown;
					if (loggedInUserCode == row["Approver1EmployeeCode"].ToString())
					{
						BtnApprove.Enabled = true;
						BtnReject.Enabled = true;
						isApprover = true;
					}
				}
				else if (App1_Status == "1") // Approved
				{
					Approver1CodeLabel.ForeColor = System.Drawing.Color.Green;
					if (loggedInUserCode == row["Approver1EmployeeCode"].ToString())
					{
						BtnApprove.Text = "Approved";
						BtnApprove.Enabled = false;
						BtnReject.Enabled = false;
						isApprover = true;
						Lbl_BasicbtnApprove.Text = "You have approved!";
					}
				}

				// Approver 2
				if (App2_Status == "0") // Pending
				{
					Approver2CodeLabel.ForeColor = System.Drawing.Color.Brown;
					if (loggedInUserCode == row["Approver2EmployeeCode"].ToString())
					{
						BtnApprove.Enabled = true;
						BtnReject.Enabled = true;
						isApprover = true;
					}
				}
				else if (App2_Status == "1") // Approved
				{
					Approver2CodeLabel.ForeColor = System.Drawing.Color.Green;
					if (loggedInUserCode == row["Approver2EmployeeCode"].ToString())
					{
						BtnApprove.Text = "Approved";
						BtnApprove.Enabled = false;
						BtnReject.Enabled = false;
						isApprover = true;
						Lbl_BasicbtnApprove.Text = "You have approved!";

					}
				}

				// Dotted Line Approver
				if (DottedApp_Status == "0") // Pending
				{
					DottedLineApproverCodeLabel.ForeColor = System.Drawing.Color.Brown;
					if (loggedInUserCode == row["DottedLineApproverEmployeeCode"].ToString())
					{
						BtnApprove.Enabled = true;
						BtnReject.Enabled = true;
						isApprover = true;
					}
				}
				else if (DottedApp_Status == "1")  // Approved
				{
					DottedLineApproverCodeLabel.ForeColor = System.Drawing.Color.Green;
					if (loggedInUserCode == row["DottedLineApproverEmployeeCode"].ToString())
					{
						BtnApprove.Text = "Approved";
						BtnApprove.Enabled = false;
						BtnReject.Enabled = false;
						isApprover = true;
						Lbl_BasicbtnApprove.Text = "You have approved!";
					}
				}
				// If the logged-in user is not any of the approvers
				if (!isApprover && !isSubmitter)
				{
					// Option 1: Disable the buttons
					BtnApprove.Enabled = false;
					BtnReject.Enabled = false;

					// Option 2: Hide the buttons entirely
					// btnApprove.Visible = false;
					// btnReject.Visible = false;
					string PlantBinder_Error_script = @"<script type='text/javascript'>
							new PNotify({
								title: 'Error',
								text: 'You are not authorized to approve!',
								type: 'error',
								styling: 'bootstrap3'
							});
						</script>";

					// RegisterStartupScript adds the JavaScript code to the page
					ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantBinderErrorNotification", PlantBinder_Error_script, false);
					Lbl_BasicbtnApprove.Text = "You are not authorized to approve or reject this form.";

				}
				// Combined Actions - Example for handling when all approvers have approved
				if (App1_Status == "1" && App2_Status == "1" && DottedApp_Status == "1")
				{
					// Perform action when all approvers have approved
					// Example: Allow form submission or update status
				}
				else if (App1_Status == "0" || App2_Status == "0" || DottedApp_Status == "0")
				{
					// Perform action when any approver is still pending
					// Example: Disable form submission or show a pending message
				}



				//ScriptManager.RegisterStartupScript(this, GetType(), "triggerButtonClick", Page.ClientScript.GetPostBackEventReference(BtnValidate, ""), true);

			}

		}
		public class ValidationCriteria
		{
			public string DisplayName { get; set; }
			public bool IsVisible { get; set; }
			public string RequiredFieldErrorMessage { get; set; }
			public string RegularExpressionErrorMessage { get; set; }
			public string RegularExpression { get; set; }
			public string RangeErrorMessage { get; set; }
			public string MinimumValue { get; set; }
			public string MaximumValue { get; set; }
			public bool IsRequired { get; set; }
			public bool IsRegularExpressionRequired { get; set; }
			public bool IsRangeRequired { get; set; }
		}
		private void StandardValue_Binder(string MaterialId, string PlantId)
		{
			//DataTable dataTable = DatabaseHelper.GetRMFieldsControlByPlantId(Convert.ToInt32(MaterialId), Convert.ToInt32(PlantId));
			DataTable dataTable = DatabaseHelper.GetBrandFieldsControlByMaterialIdAndPlantId(Convert.ToInt16(MaterialId), Convert.ToInt16(PlantId));

			// Example: Querying the DataTable for a specific field name
			//string fieldName = "no_of_pcs"; // Specify the field name you want to query
			//DataRow[] rows = dataTable.Select($"brand_id = {selectedProductBrandValue} AND field_name = '{fieldName}'");

			// Iterate through the filtered rows and extract validation criteria
			foreach (DataRow row in dataTable.Rows)
			{
				// Extract field name from the current row
				string fieldName = row["field_name"].ToString();
				string displayName = row["DisplayName"].ToString();

				// Extract validation criteria from the DataRow
				bool isVisible = Convert.ToBoolean(row["ViewMode"]);
				bool rfvEnabled = Convert.ToBoolean(row["RFV_YesNo"]);
				string rfvErrorMessage = row["RFV_ErrorMsg"].ToString();
				bool revEnabled = Convert.ToBoolean(row["REV_YesNo"]);
				string revErrorMessage = row["REV_ErrorMsg"].ToString();
				string revExpression = row["REV_Expression"].ToString();
				bool rvEnabled = Convert.ToBoolean(row["RV_Yesno"]);
				string rvErrorMessage = row["RV_ErrorMsg"].ToString();
				string rvMinValue = row["RV_MinValue"].ToString();
				string rvMaxValue = row["RV_MaxValue"].ToString();

				// Create a new instance of ValidationCriteria and populate it with data from the DataRow
				ValidationCriteria criteria = new ValidationCriteria();
				criteria.DisplayName = displayName;
				criteria.IsVisible = isVisible;
				criteria.RequiredFieldErrorMessage = rfvErrorMessage;
				criteria.IsRequired = rfvEnabled;
				criteria.RegularExpressionErrorMessage = revErrorMessage;
				criteria.IsRegularExpressionRequired = revEnabled;
				criteria.RegularExpression = revExpression;
				criteria.RangeErrorMessage = rvErrorMessage;
				criteria.IsRangeRequired = rvEnabled;
				criteria.MinimumValue = rvMinValue;
				criteria.MaximumValue = rvMaxValue;

				// Use the criteria as needed
				// For example, you can pass it to a method to set up validators
				SetUpValidatorsForField(fieldName, criteria);

			}
		}
		private void SetUpValidatorsForField(string fieldName, ValidationCriteria criteria)
		{
			switch (fieldName)
			{
				case "Product":

					ProductNameDIV.Visible = criteria.IsVisible;

					RFV_TB_ProductName.ErrorMessage = criteria.RequiredFieldErrorMessage;
					RFV_TB_ProductName.Enabled = criteria.IsRequired;

					break;

				case "Brand":

					BrandDIV.Visible = criteria.IsVisible;

					RFV_TB_BrandName.ErrorMessage = criteria.RequiredFieldErrorMessage;
					RFV_TB_BrandName.Enabled = criteria.IsRequired;

					break;


				case "Supplier_Name":

					SupplierDIV.Visible = criteria.IsVisible;

					RFV_TB_Supplier.ErrorMessage = criteria.RequiredFieldErrorMessage;
					RFV_TB_Supplier.Enabled = criteria.IsRequired;

					break;


				case "Challan_Date":

					ChallanDateDIV.Visible = criteria.IsVisible;

					RFV_TB_ChalanDate.ErrorMessage = criteria.RequiredFieldErrorMessage;
					RFV_TB_ChalanDate.Enabled = criteria.IsRequired;

					break;

				case "Challan_No":

					ChallanNoDIV.Visible = criteria.IsVisible;

					RFV_TB_ChalanNo.ErrorMessage = criteria.RequiredFieldErrorMessage;
					RFV_TB_ChalanNo.Enabled = criteria.IsRequired;

					break;

				case "Lot_No":

					LotNoDIV.Visible = criteria.IsVisible;

					RFV_TB_LotNo.ErrorMessage = criteria.RequiredFieldErrorMessage;
					RFV_TB_LotNo.Enabled = criteria.IsRequired;

					break;

				case "Batch_No":

					BatchNoDIV.Visible = criteria.IsVisible;

					RFV_TB_BatchNo.ErrorMessage = criteria.RequiredFieldErrorMessage;
					RFV_TB_BatchNo.Enabled = criteria.IsRequired;

					break;

				case "Vehicle_No":

					VehicleNoDIV.Visible = criteria.IsVisible;

					RFV_TB_VehicleNo.ErrorMessage = criteria.RequiredFieldErrorMessage;
					RFV_TB_VehicleNo.Enabled = criteria.IsRequired;

					break;

				case "Grade":

					GradeDIV.Visible = criteria.IsVisible;

					RFV_TB_Grade.ErrorMessage = criteria.RequiredFieldErrorMessage;
					RFV_TB_Grade.Enabled = criteria.IsRequired;

					break;

				case "Color":

					ColorDIV.Visible = criteria.IsVisible;

					RFV_RBL_Color.ErrorMessage = criteria.RequiredFieldErrorMessage;
					RFV_RBL_Color.Enabled = criteria.IsRequired;

					break;

				case "Smell":

					SmellDIV.Visible = criteria.IsVisible;

					RFV_RBL_Smell.ErrorMessage = criteria.RequiredFieldErrorMessage;
					RFV_RBL_Smell.Enabled = criteria.IsRequired;

					break;

				case "Taste":

					TasteFlavorDIV.Visible = criteria.IsVisible;

					RFV_RBL_TasteFlavor.ErrorMessage = criteria.RequiredFieldErrorMessage;
					RFV_RBL_TasteFlavor.Enabled = criteria.IsRequired;

					break;

				case "Consistency":

					ConsistencyDiv.Visible = criteria.IsVisible;

					RFV_RBL_Consistency.ErrorMessage = criteria.RequiredFieldErrorMessage;
					RFV_RBL_Consistency.Enabled = criteria.IsRequired;

					break;

				case "Foreign_Matter_Impurities":

					ImpuritiesDIV.Visible = criteria.IsVisible;

					RFV_TB_Foreign_Impurities.ErrorMessage = criteria.RequiredFieldErrorMessage;
					RFV_TB_Foreign_Impurities.Enabled = criteria.IsRequired;

					REV_TB_Foreign_Impurities.ErrorMessage = criteria.RegularExpressionErrorMessage;
					REV_TB_Foreign_Impurities.ValidationExpression = criteria.RegularExpression;
					REV_TB_Foreign_Impurities.Enabled = criteria.IsRegularExpressionRequired;

					break;

				case "MP_CP":

					MP_CP_DIV.Visible = criteria.IsVisible;

					RFV_TB_MP.ErrorMessage = criteria.RequiredFieldErrorMessage;
					RFV_TB_MP.Enabled = criteria.IsRequired;

					REV_TB_MP.ErrorMessage = criteria.RegularExpressionErrorMessage;
					REV_TB_MP.ValidationExpression = criteria.RegularExpression;
					REV_TB_MP.Enabled = criteria.IsRegularExpressionRequired;

					break;

				case "PV":

					PV_DIV.Visible = criteria.IsVisible;

					RFV_TB_PV.ErrorMessage = criteria.RequiredFieldErrorMessage;
					RFV_TB_PV.Enabled = criteria.IsRequired;

					REV_TB_PV.ErrorMessage = criteria.RegularExpressionErrorMessage;
					REV_TB_PV.ValidationExpression = criteria.RegularExpression;
					REV_TB_PV.Enabled = criteria.IsRegularExpressionRequired;

					break;

				case "Moisture":

					MoistureDIV.Visible = criteria.IsVisible;

					RFV_TB_Moisture.ErrorMessage = criteria.RequiredFieldErrorMessage;
					RFV_TB_Moisture.Enabled = criteria.IsRequired;

					REV_TB_Moisture.ErrorMessage = criteria.RegularExpressionErrorMessage;
					REV_TB_Moisture.ValidationExpression = criteria.RegularExpression;
					REV_TB_Moisture.Enabled = criteria.IsRegularExpressionRequired;

					break;

				case "Iodine_Val":

					IodineDIV.Visible = criteria.IsVisible;

					RFV_TB_IodineVal.ErrorMessage = criteria.RequiredFieldErrorMessage;
					RFV_TB_IodineVal.Enabled = criteria.IsRequired;

					REV_TB_IodineVal.ErrorMessage = criteria.RegularExpressionErrorMessage;
					REV_TB_IodineVal.ValidationExpression = criteria.RegularExpression;
					REV_TB_IodineVal.Enabled = criteria.IsRegularExpressionRequired;

					break;

				case "SAP_Val":

					SapDiv.Visible = criteria.IsVisible;

					RFV_TB_SapVal.ErrorMessage = criteria.RequiredFieldErrorMessage;
					RFV_TB_SapVal.Enabled = criteria.IsRequired;

					REV_TB_SapVal.ErrorMessage = criteria.RegularExpressionErrorMessage;
					REV_TB_SapVal.ValidationExpression = criteria.RegularExpression;
					REV_TB_SapVal.Enabled = criteria.IsRegularExpressionRequired;

					break;

				case "Acid_Val":

					AcidDIV.Visible = criteria.IsVisible;

					RFV_TB_AcidValue.ErrorMessage = criteria.RequiredFieldErrorMessage;
					RFV_TB_AcidValue.Enabled = criteria.IsRequired;

					REV_TB_AcidValue.ErrorMessage = criteria.RegularExpressionErrorMessage;
					REV_TB_AcidValue.ValidationExpression = criteria.RegularExpression;
					REV_TB_AcidValue.Enabled = criteria.IsRegularExpressionRequired;

					break;

				case "KriesTest":

					KriesTestDIV.Visible = criteria.IsVisible;

					RFV_RBL_KriesTest.ErrorMessage = criteria.RequiredFieldErrorMessage;
					RFV_RBL_KriesTest.Enabled = criteria.IsRequired;

					break;

				case "OilTest":

					OilTestDiv.Visible = criteria.IsVisible;

					RFV_RBL_OilTest.ErrorMessage = criteria.RequiredFieldErrorMessage;
					RFV_RBL_OilTest.Enabled = criteria.IsRequired;

					break;

				default:
					// Handle unrecognized field names
					break;
			}
		}
		private void LoadFormDetails(string FormID, string selectedPlantValue)
		{
			// Replace with your actual connection string
			string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

			using (SqlConnection conn = new SqlConnection(connectionString))
			{
				using (SqlCommand cmd = new SqlCommand("usp_GetFormsApprovalMatrix_PM", conn))
				{
					cmd.CommandType = CommandType.StoredProcedure;

					// Set parameters for the stored procedure
					cmd.Parameters.AddWithValue("@PlantId", selectedPlantValue); // Replace with actual value
																				 //cmd.Parameters.AddWithValue("@LineId", selectedPlantLineValue);  // Replace with actual value
					cmd.Parameters.AddWithValue("@FormID", FormID); // Replace with actual value
					cmd.Parameters.AddWithValue("@FormName", "RM_Class_2"); // Replace with actual value

					using (SqlDataAdapter da = new SqlDataAdapter(cmd))
					{
						DataTable dt = new DataTable();
						da.Fill(dt);

						// Bind the data to a GridView or another control
						GridViewApprovers.DataSource = dt;
						GridViewApprovers.DataBind();

						// Bind data to Flow Diagram if needed
						if (dt.Rows.Count > 0)
						{
							DataRow row = dt.Rows[0];
							lbl_docname.Text = row["DocumentName"].ToString();
							lbl_docnumber.Text = row["DocumentNumber"].ToString();
							// Set data for flow diagram
							Approver1NameLabel.Text = row["Approver1Name"].ToString();
							//Approver1CodeLabel.Text = row["Approver1EmployeeCode"].ToString();
							//Approver1Photo.ImageUrl = row["Approver1Photo"].ToString(); // Adjust field name for photo

							Approver2NameLabel.Text = row["Approver2Name"].ToString();
							//Approver2CodeLabel.Text = row["Approver2EmployeeCode"].ToString();
							//Approver2Photo.ImageUrl = row["Approver2Photo"].ToString(); // Adjust field name for photo

							DottedLineApproverNameLabel.Text = row["DottedLineApproverName"].ToString();
							//DottedLineApproverCodeLabel.Text = row["DottedLineApproverEmployeeCode"].ToString();
							//DottedLineApproverPhoto.ImageUrl = row["DottedLineApproverPhoto"].ToString(); // Adjust field name for photo
						}
						else
						{
							// Set default values to ADMIN if no rows are found
							Approver1NameLabel.Text = "ADMIN";
							Approver1CodeLabel.Text = "ADMIN";

							Approver2NameLabel.Text = "ADMIN";
							Approver2CodeLabel.Text = "ADMIN";

							DottedLineApproverNameLabel.Text = "ADMIN";
							DottedLineApproverCodeLabel.Text = "ADMIN";
						}
					}
				}
			}
		}
		protected void BtnApprove_Click(object sender, EventArgs e)
		{
			UpdateColumnBasedOnApproverType();
			LoadRecordData(RecordID);
		}
		protected void BtnReject_Click(object sender, EventArgs e)
		{
			RejectionBasedOnApproverType();
			LoadRecordData(RecordID);

		}
		public void UpdateColumnBasedOnApproverType()
		{
			// Get the logged-in employee code from session
			string employeeCode = Session["WORKMAN"] as string;

			if (!string.IsNullOrEmpty(employeeCode))
			{
				// Retrieve the approver codes from the labels in the approver-flow div
				string approver1Code = Approver1CodeLabel.Text.ToString();
				string approver2Code = Approver2CodeLabel.Text.ToString();
				string dottedLineApproverCode = DottedLineApproverCodeLabel.Text.ToString();

				// Determine the approver type based on the employee code
				string approverType = string.Empty;

				if (employeeCode == approver1Code)
				{
					approverType = "Approver1";
				}
				else if (employeeCode == approver2Code)
				{
					approverType = "Approver2";
				}
				else if (employeeCode == dottedLineApproverCode)
				{
					approverType = "DottedLineApprover";
				}
				if (!string.IsNullOrEmpty(approverType))
				{
					// Define the connection string
					string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

					// Perform SQL operation based on the approver type
					string updateQuery = string.Empty;

					switch (approverType)
					{
						case "Approver1":
							updateQuery = "UPDATE TRN_RM_CLASS_2 SET Approver1_Status = 1, Approver1_TimeStamp = @TimeStamp WHERE Approver1EmployeeCode = @Condition and Id=@Id";
							break;
						case "Approver2":
							updateQuery = "UPDATE TRN_RM_CLASS_2 SET Approver2_Status = 1, Approver2_TimeStamp = @TimeStamp WHERE Approver2EmployeeCode = @Condition and Id=@Id";
							break;
						case "DottedLineApprover":
							updateQuery = "UPDATE TRN_RM_CLASS_2 SET DottedApprover_Status = 1, DottedApprover_TimeStamp = @TimeStamp WHERE DottedLineApproverEmployeeCode = @Condition and Id=@Id";
							break;
					}
					if (!string.IsNullOrEmpty(updateQuery))
					{
						using (SqlConnection conn = new SqlConnection(connectionString))
						{
							SqlCommand cmd = new SqlCommand(updateQuery, conn);
							cmd.Parameters.AddWithValue("@TimeStamp", DateTime.Now);
							cmd.Parameters.AddWithValue("@Condition", employeeCode);
							cmd.Parameters.AddWithValue("@Id", RecordID);
							try
							{
								conn.Open();
								cmd.ExecuteNonQuery();
								Lbl_BasicbtnApprove.Text = "Approved";
							}
							catch (Exception ex)
							{
								// Handle exceptions (e.g., logging, rethrowing)
								//throw new Exception("Error updating the table.", ex);
								Lbl_BasicbtnApprove.Text = ex.Message;
							}
						}
					}
				}
			}
		}
		public void RejectionBasedOnApproverType()
		{
			// Get the logged-in employee code from session
			string employeeCode = Session["WORKMAN"] as string;

			if (!string.IsNullOrEmpty(employeeCode))
			{
				// Retrieve the approver codes from the labels in the approver-flow div
				string approver1Code = Approver1CodeLabel.Text.ToString();
				string approver2Code = Approver2CodeLabel.Text.ToString();
				string dottedLineApproverCode = DottedLineApproverCodeLabel.Text.ToString();

				// Determine the approver type based on the employee code
				string approverType = string.Empty;

				if (employeeCode == approver1Code)
				{
					approverType = "Approver1";
				}
				else if (employeeCode == approver2Code)
				{
					approverType = "Approver2";
				}
				else if (employeeCode == dottedLineApproverCode)
				{
					approverType = "DottedLineApprover";
				}
				if (!string.IsNullOrEmpty(approverType))
				{
					// Define the connection string
					string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

					// Perform SQL operation based on the approver type
					string updateQuery = string.Empty;

					switch (approverType)
					{
						case "Approver1":
							updateQuery = "UPDATE TRN_RM_CLASS_2 SET Approver1_Status = 0, Approver1_TimeStamp = @TimeStamp WHERE Approver1EmployeeCode = @Condition and Id=@Id";
							break;
						case "Approver2":
							updateQuery = "UPDATE TRN_RM_CLASS_2 SET Approver2_Status = 0, Approver2_TimeStamp = @TimeStamp WHERE Approver2EmployeeCode = @Condition and Id=@Id";
							break;
						case "DottedLineApprover":
							updateQuery = "UPDATE TRN_RM_CLASS_2 SET DottedApprover_Status = 0, DottedApprover_TimeStamp = @TimeStamp WHERE DottedLineApproverEmployeeCode = @Condition and Id=@Id";
							break;
					}
					if (!string.IsNullOrEmpty(updateQuery))
					{
						using (SqlConnection conn = new SqlConnection(connectionString))
						{
							SqlCommand cmd = new SqlCommand(updateQuery, conn);
							cmd.Parameters.AddWithValue("@TimeStamp", DateTime.Now);
							cmd.Parameters.AddWithValue("@Condition", employeeCode);
							cmd.Parameters.AddWithValue("@Id", RecordID);
							try
							{
								conn.Open();
								cmd.ExecuteNonQuery();
								Lbl_BasicbtnApprove.Text = "Rejected";
							}
							catch (Exception ex)
							{
								// Handle exceptions (e.g., logging, rethrowing)
								Lbl_BasicbtnApprove.Text = ex.Message;
								//throw new Exception("Error updating the table.", ex);
							}
						}
					}
				}
			}
		}
	}
}