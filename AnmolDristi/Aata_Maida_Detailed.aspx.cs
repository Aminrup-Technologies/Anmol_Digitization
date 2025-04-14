using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Vml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Windows.Media.Media3D;

namespace AnmolDristi
{
    public partial class Aata_Maida_Detailed : System.Web.UI.Page
    {
        public static Int32 RecordID = 0;

        public static string MaterialId = string.Empty;
        public static string MaterialName = string.Empty;
        public static string PlantId = string.Empty;
        public static string PlantName = string.Empty;

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
                    BtnBack.PostBackUrl = "~/Aata_Maida_Submitter.aspx";
                    Lbl_BasicbtnApprove.Text = "Click on your Action";
                }
                else
                {
                    BtnBack.PostBackUrl = "~/Aata_Maida_Approval.aspx";
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

                        M.Material_Name,
                        A.plant_name,
                        P.*
                    FROM
                        TRN_Aata_Maida P
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
                ClientScript.RegisterStartupScript(this.GetType(), "ShowMaterialBinderErrorNotification", MaterialBinder_Error_script, false);
            }
        }

        private void PlantBinder(string selectedMaterialValue)
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
            //LoadApprovers(selectedPlantValue, "");
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
                case "BestBeforeDate":

                    BeforeDateDIV.Visible = criteria.IsVisible;

                    RFV_TB_BeforeDate.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_BeforeDate.Enabled = criteria.IsRequired;

                    TB_BeforeDate.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    break;

                case "MoistureValue":

                    MoistureDIV.Visible = criteria.IsVisible;

                    RFV_TB_Moisture.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Moisture.Enabled = criteria.IsRequired;

                    TB_Moisture.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Moisture.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Moisture.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Moisture.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_Moisture.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_Moisture.Enabled = criteria.IsRangeRequired;

                    hdnMinMoistureValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxMoistureValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "TotalAshValue":

                    TotalAshDIV.Visible = criteria.IsVisible;

                    RFV_TB_TotalAsh.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_TotalAsh.Enabled = criteria.IsRequired;

                    TB_TotalAsh.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_TotalAsh.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_TotalAsh.ValidationExpression = criteria.RegularExpression;
                    REV_TB_TotalAsh.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_TotalAsh.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_TotalAsh.Enabled = criteria.IsRangeRequired;

                    hdnMinTotalAshValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxTotalAshValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "InsolubleAshValue":

                    InsolubleAshDIV.Visible = criteria.IsVisible;

                    RFV_TB_InsolubleAsh.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_InsolubleAsh.Enabled = criteria.IsRequired;

                    TB_InsolubleAsh.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_InsolubleAsh.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_InsolubleAsh.ValidationExpression = criteria.RegularExpression;
                    REV_TB_InsolubleAsh.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_InsolubleAsh.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_InsolubleAsh.Enabled = criteria.IsRangeRequired;

                    hdnMinInsolubleAshValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxInsolubleAshValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "GlutentContent":

                    GlutentContentDIV.Visible = criteria.IsVisible;

                    RFV_TB_GlutentContent.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_GlutentContent.Enabled = criteria.IsRequired;

                    TB_GlutentContent.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_GlutentContent.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_GlutentContent.ValidationExpression = criteria.RegularExpression;
                    REV_TB_GlutentContent.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_GlutentContent.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_GlutentContent.Enabled = criteria.IsRangeRequired;

                    hdnMinGlutentContentValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxGlutentContentValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "AlcoholicAcidity":

                    AlcoholicAcidityDIV.Visible = criteria.IsVisible;

                    RFV_TB_AlcoholicAcidity.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_AlcoholicAcidity.Enabled = criteria.IsRequired;

                    TB_AlcoholicAcidity.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_AlcoholicAcidity.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_AlcoholicAcidity.ValidationExpression = criteria.RegularExpression;
                    REV_TB_AlcoholicAcidity.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_AlcoholicAcidity.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_AlcoholicAcidity.Enabled = criteria.IsRangeRequired;

                    hdnMinAlcoholicAcidityValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxAlcoholicAcidityValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "Absorption":

                    AbsorptionDIV.Visible = criteria.IsVisible;

                    RFV_TB_Absorption.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Absorption.Enabled = criteria.IsRequired;

                    TB_Absorption.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Absorption.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Absorption.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Absorption.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_Absorption.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_Absorption.Enabled = criteria.IsRangeRequired;

                    hdnMinAbsorptionValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxAbsorptionValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "Sedimentation":

                    SedimentationDIV.Visible = criteria.IsVisible;

                    RFV_TB_Sedimentation.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Sedimentation.Enabled = criteria.IsRequired;

                    TB_Sedimentation.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Sedimentation.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Sedimentation.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Sedimentation.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_Sedimentation.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_Sedimentation.Enabled = criteria.IsRangeRequired;

                    hdnMinSedimentationValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxSedimentationValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "Acidity":

                    AcidityDIV.Visible = criteria.IsVisible;

                    RFV_TB_Acidity.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Acidity.Enabled = criteria.IsRequired;

                    TB_Acidity.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Acidity.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Acidity.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Acidity.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_Acidity.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_Acidity.Enabled = criteria.IsRangeRequired;

                    hdnMinAcidityValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxAcidityValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "Granularity":

                    GranularityDIV.Visible = criteria.IsVisible;

                    RFV_TB_Granularity.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Granularity.Enabled = criteria.IsRequired;

                    TB_Granularity.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Granularity.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Granularity.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Granularity.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_Granularity.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_Granularity.Enabled = criteria.IsRangeRequired;

                    hdnMinGranularityValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxGranularityValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "GranularityRetention":

                    GranularityRetentionDIV.Visible = criteria.IsVisible;

                    RFV_TB_GranularityRetention.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_GranularityRetention.Enabled = criteria.IsRequired;

                    TB_GranularityRetention.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_GranularityRetention.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_GranularityRetention.ValidationExpression = criteria.RegularExpression;
                    REV_TB_GranularityRetention.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_GranularityRetention.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_GranularityRetention.Enabled = criteria.IsRangeRequired;

                    hdnMinGranularityRetentionValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxGranularityRetentionValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "Retention":

                    RetentionDIV.Visible = criteria.IsVisible;

                    RFV_TB_Retention.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Retention.Enabled = criteria.IsRequired;

                    TB_Retention.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Retention.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Retention.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Retention.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_Retention.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_Retention.Enabled = criteria.IsRangeRequired;

                    hdnMinRetentionValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxRetentionValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "Bromate":

                    BromateDIV.Visible = criteria.IsVisible;

                    RFV_TB_Bromate.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Bromate.Enabled = criteria.IsRequired;

                    TB_Bromate.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Bromate.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Bromate.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Bromate.Enabled = criteria.IsRegularExpressionRequired;

                    hdnMinBromateValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxBromateValue.Value = criteria.MaximumValue.ToString();
                    break;

                default:
                    break;
            }
        }


        void getDetails(DataTable dt)
        {
            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];

                //updation code
                DateTime submittedDate = Convert.ToDateTime(row["SubmittedDate"]); // Get submission date
                TimeSpan submittedTimeSpan = (TimeSpan)row["SubmittedTime"]; // Get submission time

                DateTime submissionDateTime = submittedDate.Add(submittedTimeSpan);

                bool within48Hours = (DateTime.Now - submissionDateTime).TotalHours <= 48;

                // Enable/disable textboxes and buttons based on the condition
                TB_TotalAsh.ReadOnly = !within48Hours;
                TXB_Ash_Remarks.ReadOnly = !within48Hours;

                TB_InsolubleAsh.ReadOnly = !within48Hours; 
                TXB_InsolubleAsh_Remarks.ReadOnly = !within48Hours;

                TB_GlutentContent.ReadOnly = !within48Hours;
                TXB_GlutentContent_Remarks.ReadOnly = !within48Hours;

                TB_AlcoholicAcidity.ReadOnly = !within48Hours;
                TXB_AlcoholicAcidity_Remarks.ReadOnly = !within48Hours;

                AshRemarksDIV.Style["display"] = within48Hours ? "block" : "none";
                InsolubleAshRemarksDIV.Style["display"] = within48Hours ? "block" : "none";
                GlutentContentRemarksDIV.Style["display"] = within48Hours ? "block" : "none";
                AlcoholicAcidityRemarksDIV.Style["display"] = within48Hours ? "block" : "none";

                Lbl_btnUpdate.Visible = within48Hours;
                Update.Visible = within48Hours;
                Reset.Visible = within48Hours;

                //Retrieval code
                MaterialId = dt.Rows[0]["MaterialId"].ToString();
                MaterialName = dt.Rows[0]["Material_Name"].ToString();
                DDL_Material.SelectedItem.Text = MaterialName;
                DDL_Material.Enabled = false;

                PlantBinder(MaterialId);
                PlantId = dt.Rows[0]["PlantId"].ToString();
                PlantName = dt.Rows[0]["plant_name"].ToString();
                DDL_Plant.SelectedValue = PlantId;
                DDL_Plant.Enabled = false;

                StandardValue_Binder(MaterialId, PlantId);

                TB_BrandName.Text = dt.Rows[0]["ProductBrand"].ToString();

                TB_Quantity.Text = dt.Rows[0]["Quantity"].ToString();
                TB_Supplier.Text = dt.Rows[0]["Supplier_Name"].ToString();

                TB_Size.Text = dt.Rows[0]["Size"].ToString();

                TB_ChallanNo.Text = dt.Rows[0]["Challan_No"].ToString();
                TB_ChallanDate.Text = dt.Rows[0]["Challan_Dates"].ToString();

                TB_Mfg.Text = dt.Rows[0]["Mfg_Dates"].ToString();

                TB_BatchNo.Text = dt.Rows[0]["BatchNo"].ToString();
                TB_LotNo.Text = dt.Rows[0]["LotNo"].ToString();
                TB_VehicleNo.Text = dt.Rows[0]["VehicleNo"].ToString();

                RBL_ManufNameAdd.Text = dt.Rows[0]["MfgYesNo"].ToString();
                TXB_ManufNameAdd_Remarks.Text = dt.Rows[0]["MfgYesNoRemarks"].ToString();

                RBL_FassaiNoLogo.Text = dt.Rows[0]["fssaiYesNo"].ToString();
                TXB_FassaiNoLogo_Remarks.Text = dt.Rows[0]["fssaiYesNoRemarks"].ToString();

                RBL_BBDateYesNo.Text = dt.Rows[0]["BBDateYesNo"].ToString();
                TXB_BBDateYesNoRemarks.Text = dt.Rows[0]["BBDateYesNoRemarks"].ToString();

                TB_MfgName.Text = dt.Rows[0]["MfgName"].ToString();

                TB_BeforeDate.Text = dt.Rows[0]["BeforeDate"].ToString();

                TB_FssaiNo.Text = dt.Rows[0]["FssaiNo"].ToString();

                RBL_Fssai_Logo.Text = dt.Rows[0]["Fssai_Logo"].ToString();

                RBL_Veg_Logo.Text = dt.Rows[0]["Veg_Logo"].ToString();

                RBL_Packing_Condition.Text = dt.Rows[0]["Packing_Condition"].ToString();
                TXB_PackingCondition_Remarks.Text = dt.Rows[0]["PackingCondition_Remarks"].ToString();

                RBL_ColorApp.Text = dt.Rows[0]["ColorApp"].ToString();
                TXB_ColorApp_Remarks.Text = dt.Rows[0]["ColorApp_Remarks"].ToString();

                RBL_Odour.Text = dt.Rows[0]["Odour"].ToString();
                TXB_Odour_Remarks.Text = dt.Rows[0]["Odour_Remarks"].ToString();

                RBL_TasteFlavor.Text = dt.Rows[0]["TasteFlavor"].ToString();
                TXB_TasteFlavor_Remarks.Text = dt.Rows[0]["TasteFlavor_Remarks"].ToString();

                RBL_Impurities.Text = dt.Rows[0]["Impurities"].ToString();
                TXB_Impurities_Remarks.Text = dt.Rows[0]["Impurities_Remarks"].ToString();

                TB_Moisture.Text = dt.Rows[0]["Moisture"].ToString();
                TXB_Moisture_Remarks.Text = dt.Rows[0]["Moisture_Remarks"].ToString();

                TB_TotalAsh.Text = dt.Rows[0]["TotalAsh"].ToString();
                TXB_Ash_Remarks.Text = dt.Rows[0]["Ash_Remarks"].ToString();

                TB_InsolubleAsh.Text = dt.Rows[0]["InsolubleAsh"].ToString();
                TXB_InsolubleAsh_Remarks.Text = dt.Rows[0]["InsolubleAsh_Remarks"].ToString();

                TB_GlutentContent.Text = dt.Rows[0]["GlutentContent"].ToString();
                TXB_GlutentContent_Remarks.Text = dt.Rows[0]["GlutentContent_Remarks"].ToString();

                TB_AlcoholicAcidity.Text = dt.Rows[0]["AlcoholicAcidity"].ToString();
                TXB_AlcoholicAcidity_Remarks.Text = dt.Rows[0]["AlcoholicAcidity_Remarks"].ToString();

                TB_Absorption.Text = dt.Rows[0]["Absorption"].ToString();
                TXB_Absorption_Remarks.Text = dt.Rows[0]["Absorption_Remarks"].ToString();

                TB_Sedimentation.Text = dt.Rows[0]["Sedimentation"].ToString();
                TXB_Sedimentation_Remarks.Text = dt.Rows[0]["Sedimentation_Remarks"].ToString();

                RBL_Grittiness.Text = dt.Rows[0]["Grittiness"].ToString();
                TXB_Grittiness_Remarks.Text = dt.Rows[0]["Grittiness_Remarks"].ToString();

                TB_Acidity.Text = dt.Rows[0]["Acidity"].ToString();
                TXB_Acidity_Remarks.Text = dt.Rows[0]["Acidity_Remarks"].ToString();

                TB_Granularity.Text = dt.Rows[0]["Granularity"].ToString();
                TXB_Granularity_Remarks.Text = dt.Rows[0]["Granularity_Remarks"].ToString();

                TB_GranularityRetention.Text = dt.Rows[0]["GranularityRetention"].ToString();
                TXB_GranularityRetention_Remarks.Text = dt.Rows[0]["GranularityRetention_Remarks"].ToString();

                TB_Retention.Text = dt.Rows[0]["Retention"].ToString();
                TXB_Retention_Remarks.Text = dt.Rows[0]["Retention_Remarks"].ToString();

                TB_Bromate.Text = dt.Rows[0]["Bromate"].ToString();

                imgMaterial.ImageUrl = dt.Rows[0]["Material_Image"].ToString();


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
                    cmd.Parameters.AddWithValue("@FormName", "RM_Class_6"); // Replace with actual value

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
                            updateQuery = "UPDATE TRN_Aata_Maida SET Approver1_Status = 1, Approver1_TimeStamp = @TimeStamp WHERE Approver1EmployeeCode = @Condition and Id=@Id";
                            break;
                        case "Approver2":
                            updateQuery = "UPDATE TRN_Aata_Maida SET Approver2_Status = 1, Approver2_TimeStamp = @TimeStamp WHERE Approver2EmployeeCode = @Condition and Id=@Id";
                            break;
                        case "DottedLineApprover":
                            updateQuery = "UPDATE TRN_Aata_Maida SET DottedApprover_Status = 1, DottedApprover_TimeStamp = @TimeStamp WHERE DottedLineApproverEmployeeCode = @Condition and Id=@Id";
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
                            updateQuery = "UPDATE TRN_Aata_Maida SET Approver1_Status = 0, Approver1_TimeStamp = @TimeStamp WHERE Approver1EmployeeCode = @Condition and Id=@Id";
                            break;
                        case "Approver2":
                            updateQuery = "UPDATE TRN_Aata_Maida SET Approver2_Status = 0, Approver2_TimeStamp = @TimeStamp WHERE Approver2EmployeeCode = @Condition and Id=@Id";
                            break;
                        case "DottedLineApprover":
                            updateQuery = "UPDATE TRN_Aata_Maida SET DottedApprover_Status = 0, DottedApprover_TimeStamp = @TimeStamp WHERE DottedLineApproverEmployeeCode = @Condition and Id=@Id";
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

        protected void Update_Click(object sender, EventArgs e)
        {
            Response.Write("<script>alert('Update button clicked!');</script>");

            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            string id = Request.QueryString["Id"];

            decimal? ash = !string.IsNullOrEmpty(TB_TotalAsh.Text) ? Convert.ToDecimal(TB_TotalAsh.Text) : (decimal?)null;
            string ashRemarks = string.IsNullOrEmpty(TXB_Ash_Remarks.Text) ? null : TXB_Ash_Remarks.Text;

            decimal? insolubleAsh = !string.IsNullOrEmpty(TB_InsolubleAsh.Text) ? Convert.ToDecimal(TB_InsolubleAsh.Text) : (decimal?)null;
            string insolubleAshRemarks = string.IsNullOrEmpty(TXB_InsolubleAsh_Remarks.Text) ? null : TXB_InsolubleAsh_Remarks.Text;

            decimal? glutentContent = !string.IsNullOrEmpty(TB_GlutentContent.Text) ? Convert.ToDecimal(TB_GlutentContent.Text) : (decimal?)null;
            string glutentContentRemarks = string.IsNullOrEmpty(TXB_GlutentContent_Remarks.Text) ? null : TXB_GlutentContent_Remarks.Text;

            decimal? alcoholicAcidity = !string.IsNullOrEmpty(TB_AlcoholicAcidity.Text) ? Convert.ToDecimal(TB_AlcoholicAcidity.Text) : (decimal?)null;
            string alcoholicAcidityRemarks = string.IsNullOrEmpty(TXB_AlcoholicAcidity_Remarks.Text) ? null : TXB_AlcoholicAcidity_Remarks.Text;


            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "UPDATE TRN_Aata_Maida SET TotalAsh = @TotalAsh, Ash_Remarks = @Ash_Remarks, InsolubleAsh = @InsolubleAsh, InsolubleAsh_Remarks = @InsolubleAsh_Remarks, GlutentContent = @GlutentContent, " +
                                                                    "GlutentContent_Remarks = @GlutentContent_Remarks, AlcoholicAcidity = @AlcoholicAcidity, AlcoholicAcidity_Remarks = @AlcoholicAcidity_Remarks " +
                                    "WHERE Id = @Id"; 

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {

                        // Add parameters
                        command.Parameters.AddWithValue("@TotalAsh", (object)ash ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Ash_Remarks", (object)ashRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@InsolubleAsh", (object)insolubleAsh ?? DBNull.Value);
                        command.Parameters.AddWithValue("@InsolubleAsh_Remarks", (object)insolubleAshRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@GlutentContent", (object)glutentContent ?? DBNull.Value);
                        command.Parameters.AddWithValue("@GlutentContent_Remarks", (object)glutentContentRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@AlcoholicAcidity", (object)alcoholicAcidity ?? DBNull.Value);
                        command.Parameters.AddWithValue("@AlcoholicAcidity_Remarks", (object)alcoholicAcidityRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Id", id);

                        command.ExecuteNonQuery();

                        string Data_SuccessScript = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Data Updated Success',
                                text: 'Recorded Successfully!!',
                                type: 'success',
                                styling: 'bootstrap3'
                            });
                        </script>";

                        ClientScript.RegisterStartupScript(this.GetType(), "ShowDataSuccessNotification", Data_SuccessScript, false);
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message.Replace("'", "\\'"); // Escape single quotes in the error message
                string errorScript = "<script type='text/javascript'>\n" +
                                     $"new PNotify({{\n" +
                                     "    title: 'Error',\n" +
                                     $"    text: '{errorMessage}',\n" +
                                     "    type: 'error',\n" +
                                     "    styling: 'bootstrap3'\n" +
                                     "});\n" +
                                     "</script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowErrorNotification", errorScript, false);
            }

        }

        protected void Reset_Click(object sender, EventArgs e)
        {
            LoadRecordData(RecordID);
        }
    }
}