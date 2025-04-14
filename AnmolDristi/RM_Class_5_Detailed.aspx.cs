using DocumentFormat.OpenXml.Vml;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Drawing;
using DocumentFormat.OpenXml.Bibliography;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Database;

namespace AnmolDristi
{
    public partial class RM_Class_5_Detailed : System.Web.UI.Page
    {
        public static Int32 RecordID = 0;

        public static string MaterialId = string.Empty;
        public static string MaterialName = string.Empty;
        public static string PlantId = string.Empty;
        public static string PlantName = string.Empty;
        public static string PlantLine = string.Empty;
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
                    BtnBack.PostBackUrl = "~/RM_Class_5_Submitter.aspx";
                    Lbl_BasicbtnApprove.Text = "Click on your Action";

                }
                else
                {
                    BtnBack.PostBackUrl = "~/RM_Class_5_Approval.aspx";
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
                        P.PlantName as PlantID ,

                        M.Material_Name,
                        A.plant_name,
                        P.*
                    FROM
                        TRN_RM_CLASS_5 P
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
            string query = "SELECT Material_Id, Material_Name FROM RM_Material where Class = 5";
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

        private void PopulateColorDropdown()
        {
            // Add the "Select" option as the first item
            DDL_Color.Items.Add(new ListItem("Select", "0")); // Empty value for default selection

            // Add items to the DropDownList
            DDL_Color.Items.Add(new ListItem("Brown", "Brown"));
            DDL_Color.Items.Add(new ListItem("Yellow", "Yellow"));
            DDL_Color.Items.Add(new ListItem("White", "White"));
            DDL_Color.Items.Add(new ListItem("Colourless", "Colourless"));
            DDL_Color.Items.Add(new ListItem("Other", "Other"));

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

                PlantBinder(MaterialId);
                PlantId = dt.Rows[0]["PlantId"].ToString();
                PlantName = dt.Rows[0]["plant_name"].ToString();
                DDL_Plant.SelectedValue = PlantId;
                DDL_Plant.Enabled = false;

                TB_Brand.Text = dt.Rows[0]["ProductBrand"].ToString();
                TB_Supplier.Text = dt.Rows[0]["Supplier_Name"].ToString();

                TB_ChallanNo.Text = dt.Rows[0]["Challan_No"].ToString();
                TB_ChallanDate.Text = dt.Rows[0]["Challan_Date"].ToString();

                TB_LotNo.Text = dt.Rows[0]["Lot_No"].ToString();
                TB_BatchNo.Text = dt.Rows[0]["Batch_No"].ToString();
                TB_VehicleNo.Text = dt.Rows[0]["Vehicle_No"].ToString();
                TB_PkdMfg.Text = dt.Rows[0]["Pkd_Date"].ToString();

                TB_Quantity.Text = dt.Rows[0]["Quantity"].ToString();
                TXB_Quantity_Remarks.Text = dt.Rows[0]["CommentsForQuantity"].ToString();

                RBL_Grade.SelectedValue = dt.Rows[0]["Grade"].ToString();
                TXB_Grade_Remarks.Text = dt.Rows[0]["CommentsForGrade"].ToString();

                PopulateColorDropdown();
                Color = dt.Rows[0]["Color"].ToString();
                DDL_Color.SelectedValue = Color;
                TXB_Color_Remarks.Text = dt.Rows[0]["CommentsForColor"].ToString();
                DDL_Color.Enabled = false;

                RBL_Smell.SelectedValue = dt.Rows[0]["Smell"].ToString();
                TXB_Smell_Remarks.Text = dt.Rows[0]["CommentsForSmell"].ToString();

                RBL_Appearance.SelectedValue = dt.Rows[0]["Appearance"].ToString();
                TXB_Appearance_Remarks.Text = dt.Rows[0]["CommentsForAppearance"].ToString();

                RBL_TasteFlavor.SelectedValue = dt.Rows[0]["Taste"].ToString();
                TXB_TasteFlavor_Remarks.Text = dt.Rows[0]["CommentsForTaste"].ToString();

                TB_Foreign_Impurities.Text = dt.Rows[0]["Foreign_Matter_Impurities"].ToString();

                TB_PH.Text = dt.Rows[0]["Purity"].ToString();
                TXB_PH_Remarks.Text = dt.Rows[0]["CommentsForPurity"].ToString();

                TB_PH.Text = dt.Rows[0]["PH"].ToString();
                TXB_PH_Remarks.Text = dt.Rows[0]["CommentsForPH"].ToString();

                TB_Moisture.Text = dt.Rows[0]["Moisture"].ToString();
                TXB_Moisture_Remarks.Text = dt.Rows[0]["CommentsForMoisture"].ToString();

                TB_Acid.Text = dt.Rows[0]["Acid"].ToString();
                TXB_Acid_Remarks.Text = dt.Rows[0]["CommentsForAcid"].ToString();

                TB_Mpcp.Text = dt.Rows[0]["MpCp"].ToString();
                TXB_Mpcp_Remarks.Text = dt.Rows[0]["CommentsForMpCp"].ToString();

                TB_Dispersability.Text = dt.Rows[0]["Dispersability"].ToString();
                TXB_Dispersability_Remarks.Text = dt.Rows[0]["CommentsForDispersability"].ToString();

                TB_Drc.Text = dt.Rows[0]["Drc"].ToString();
                TXB_Drc_Remarks.Text = dt.Rows[0]["CommentsForDrc"].ToString();

                TB_MonoGlycerideContent.Text = dt.Rows[0]["MonoGlycerideContent"].ToString();
                TXB_MonoGlycerideContent_Remarks.Text = dt.Rows[0]["CommentsForMonoGlycerideContent"].ToString();

                TB_Neutralizing.Text = dt.Rows[0]["NeutralizingValue"].ToString();
                TXB_Neutralizing_Remarks.Text = dt.Rows[0]["CommentsForNeutralizingValue"].ToString();

                TB_Brix.Text = dt.Rows[0]["Brix"].ToString();
                TXB_Brix_Remarks.Text = dt.Rows[0]["CommentsForBrix"].ToString();

                RBL_AppStatus.SelectedValue = dt.Rows[0]["AppStatus"].ToString();
                TXB_AppStatus_Remarks.Text = dt.Rows[0]["AppStatusRemarks"].ToString();

                TB_WIM.Text = dt.Rows[0]["WIM"].ToString();
                TXB_WIM.Text = dt.Rows[0]["CommentsForWIM"].ToString();

                imgMaterial.ImageUrl = dt.Rows[0]["Material_Image"].ToString();


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
            DataTable dataTable = DatabaseHelper.GetBrandFieldsControlByMaterialIdAndPlantId(Convert.ToInt32(MaterialId), Convert.ToInt32(PlantId));


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
                case "ProductBrand":

                    BrandDIV.Visible = criteria.IsVisible;
                    Label_TB_Brand.Text = criteria.DisplayName;

                    RFV_TB_Brand.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Brand.Enabled = criteria.IsRequired;

                    TB_Brand.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Brand.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Brand.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Brand.Enabled = criteria.IsRegularExpressionRequired;

                    hdnMinBrandValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxBrandValue.Value = criteria.MaximumValue.ToString();

                    break;

                case "Supplier":
                    SupplierDIV.Visible = criteria.IsVisible;
                    Lbl_TB_Supplier.Text = criteria.DisplayName;

                    RFV_TB_Supplier.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Supplier.Enabled = criteria.IsRequired;

                    TB_Supplier.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Supplier.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Supplier.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Supplier.Enabled = criteria.IsRegularExpressionRequired;

                    hdnMinSupplierValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxSupplierValue.Value = criteria.MaximumValue.ToString();

                    break;

                case "ChallanDate":

                    ChallanDateDIV.Visible = criteria.IsVisible;
                    Lbl_TB_ChallanDate.Text = criteria.DisplayName;

                    RFV_TB_ChallanDate.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_ChallanDate.Enabled = criteria.IsRequired;

                    break;

                case "ChallanNo":

                    ChallanNoDIV.Visible = criteria.IsVisible;
                    Lbl_TB_ChallanNo.Text = criteria.DisplayName;

                    RFV_TB_ChallanNo.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_ChallanNo.Enabled = criteria.IsRequired;

                    TB_ChallanNo.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_ChallanNo.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_ChallanNo.ValidationExpression = criteria.RegularExpression;
                    REV_TB_ChallanNo.Enabled = criteria.IsRegularExpressionRequired;


                    break;

                case "Quantity":

                    QuantityDIV.Visible = criteria.IsVisible;
                    Lbl_TB_Quantity.Text = criteria.DisplayName;

                    RFV_TB_Quantity.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Quantity.Enabled = criteria.IsRequired;

                    TB_Quantity.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Quantity.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Quantity.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Quantity.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_Quantity.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_Quantity.Enabled = criteria.IsRangeRequired;

                    hdnMinQtyValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxQtyValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "Lot/Gate":

                    LotNoDIV.Visible = criteria.IsVisible;
                    Lbl_TB_LotNo.Text = criteria.DisplayName;

                    RFV_TB_LotNo.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_LotNo.Enabled = criteria.IsRequired;

                    TB_LotNo.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_LotNo.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_LotNo.ValidationExpression = criteria.RegularExpression;
                    REV_TB_LotNo.Enabled = criteria.IsRegularExpressionRequired;

                    break;

                case "Batch":

                    BatchNoDIV.Visible = criteria.IsVisible;
                    Lbl_TB_BatchNo.Text = criteria.DisplayName;

                    RFV_TB_BatchNo.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_BatchNo.Enabled = criteria.IsRequired;

                    TB_BatchNo.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_BatchNo.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_BatchNo.ValidationExpression = criteria.RegularExpression;
                    REV_TB_BatchNo.Enabled = criteria.IsRegularExpressionRequired;

                    break;

                case "Vehicle":

                    VehicleNoDIV.Visible = criteria.IsVisible;
                    Lbl_TB_VehicleNo.Text = criteria.DisplayName;

                    RFV_TB_VehicleNo.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_VehicleNo.Enabled = criteria.IsRequired;

                    TB_VehicleNo.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_VehicleNo.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_VehicleNo.ValidationExpression = criteria.RegularExpression;
                    REV_TB_VehicleNo.Enabled = criteria.IsRegularExpressionRequired;

                    break;

                case "Pkd/MfgDate":

                    PkdMfgDIV.Visible = criteria.IsVisible;
                    Lbl_TB_PkdMfg.Text = criteria.DisplayName;

                    RFV_TB_PkdMfg.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_PkdMfg.Enabled = criteria.IsRequired;

                    break;

                case "Grade":

                    GradeDIV.Visible = criteria.IsVisible;
                    Lbl_RBL_Grade.Text = criteria.DisplayName;

                    RFV_RBL_Grade.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_RBL_Grade.Enabled = criteria.IsRequired;

                    break;

                case "Color":

                    ColorDIV.Visible = criteria.IsVisible;
                    LabelColor.Text = criteria.DisplayName;

                    RFV_DDL_Color.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_DDL_Color.Enabled = criteria.IsRequired;

                    break;

                case "Odour/Smell":

                    SmellDIV.Visible = criteria.IsVisible;
                    LabelSmell.Text = criteria.DisplayName;

                    RFV_RBL_Smell.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_RBL_Smell.Enabled = criteria.IsRequired;

                    break;

                case "Appearance":

                    AppearanceDIV.Visible = criteria.IsVisible;
                    LabelAppearance.Text = criteria.DisplayName;

                    RFV_RBL_Appearance.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_RBL_Appearance.Enabled = criteria.IsRequired;

                    break;

                case "Taste/Flavor":

                    TasteFlavorDIV.Visible = criteria.IsVisible;
                    LabelTasteFlavor.Text = criteria.DisplayName;

                    RFV_RBL_TasteFlavor.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_RBL_TasteFlavor.Enabled = criteria.IsRequired;

                    break;

                case "ForeignMatter/Impurities":

                    ImpuritiesDIV.Visible = criteria.IsVisible;
                    Lbl_TB_Foreign_Impurities.Text = criteria.DisplayName;

                    RFV_TB_Foreign_Impurities.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Foreign_Impurities.Enabled = criteria.IsRequired;

                    TB_Foreign_Impurities.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Foreign_Impurities.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Foreign_Impurities.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Foreign_Impurities.Enabled = criteria.IsRegularExpressionRequired;

                    hdnMinImpuritiesValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxImpuritiesValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "PurityValue":

                    PurityDIV.Visible = criteria.IsVisible;
                    Lbl_TB_Purity.Text = criteria.DisplayName;

                    RFV_TB_Purity.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Purity.Enabled = criteria.IsRequired;

                    TB_Purity.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Purity.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Purity.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Purity.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_Purity.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_Purity.Enabled = criteria.IsRangeRequired;

                    hdnMinPurityValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxPurityValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "PhValue":

                    PHDIV.Visible = criteria.IsVisible;
                    Lbl_TB_PH.Text = criteria.DisplayName;

                    RFV_TB_PH.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_PH.Enabled = criteria.IsRequired;

                    TB_PH.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_PH.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_PH.ValidationExpression = criteria.RegularExpression;
                    REV_TB_PH.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_PH.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_PH.Enabled = criteria.IsRangeRequired;

                    hdnMinPhValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxPhValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "MoistureValue":

                    MoistureDIV.Visible = criteria.IsVisible;
                    Lbl_TB_Moisture.Text = criteria.DisplayName;

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


                case "AcidValue":

                    AcidDIV.Visible = criteria.IsVisible;
                    Lbl_TB_Acid.Text = criteria.DisplayName;

                    RFV_TB_Acid.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Acid.Enabled = criteria.IsRequired;

                    TB_Acid.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Acid.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Acid.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Acid.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_Acid.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_Acid.Enabled = criteria.IsRangeRequired;

                    hdnMinAcidValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxAcidValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "MpCp":

                    MpcpDIV.Visible = criteria.IsVisible;
                    Lbl_TB_Mpcp.Text = criteria.DisplayName;

                    RFV_TB_Mpcp.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Mpcp.Enabled = criteria.IsRequired;

                    TB_Mpcp.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Mpcp.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Mpcp.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Mpcp.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_Mpcp.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_Mpcp.Enabled = criteria.IsRangeRequired;

                    hdnMinMpcpValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxMpcpValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "DispersabilityValue":

                    DispersabilityDIV.Visible = criteria.IsVisible;
                    Lbl_TB_Dispersability.Text = criteria.DisplayName;

                    RFV_TB_Dispersability.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Dispersability.Enabled = criteria.IsRequired;

                    TB_Dispersability.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Dispersability.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Dispersability.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Dispersability.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_Dispersability.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_Dispersability.Enabled = criteria.IsRangeRequired;

                    hdnMinDispersabilityValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxDispersabilityValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "DrcValue":

                    DrcDIV.Visible = criteria.IsVisible;
                    Lbl_TB_Drc.Text = criteria.DisplayName;

                    RFV_TB_Drc.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Drc.Enabled = criteria.IsRequired;

                    TB_Drc.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Drc.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Drc.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Drc.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_Drc.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_Drc.Enabled = criteria.IsRangeRequired;

                    hdnMinDrcValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxDrcValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "MonoGlycerideContentValue":

                    MonoGlycerideContentDIV.Visible = criteria.IsVisible;
                    Lbl_TB_MonoGlycerideContent.Text = criteria.DisplayName;

                    RFV_TB_MonoGlycerideContent.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_MonoGlycerideContent.Enabled = criteria.IsRequired;

                    TB_MonoGlycerideContent.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_MonoGlycerideContent.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_MonoGlycerideContent.ValidationExpression = criteria.RegularExpression;
                    REV_TB_MonoGlycerideContent.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_MonoGlycerideContent.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_MonoGlycerideContent.Enabled = criteria.IsRangeRequired;

                    hdnMinMonoGlycerideContentValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxMonoGlycerideContentValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "NeutralizingValue":

                    NeutralizingDIV.Visible = criteria.IsVisible;
                    Lbl_TB_Neutralizing.Text = criteria.DisplayName;

                    RFV_TB_Neutralizing.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Neutralizing.Enabled = criteria.IsRequired;

                    TB_Neutralizing.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Neutralizing.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Neutralizing.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Neutralizing.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_Neutralizing.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_Neutralizing.Enabled = criteria.IsRangeRequired;

                    hdnMinNeutralizingValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxNeutralizingValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "BrixValue":

                    BrixDIV.Visible = criteria.IsVisible;
                    Lbl_TB_Brix.Text = criteria.DisplayName;

                    RFV_TB_Brix.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Brix.Enabled = criteria.IsRequired;

                    TB_Brix.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Brix.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Brix.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Brix.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_Brix.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_Brix.Enabled = criteria.IsRangeRequired;

                    hdnMinBrixValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxBrixValue.Value = criteria.MaximumValue.ToString();
                    break;


                case "WIMValue":

                    WIMDIV.Visible = criteria.IsVisible;
                    Lbl_TB_WIM.Text = criteria.DisplayName;

                    RFV_TB_WIM.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_WIM.Enabled = criteria.IsRequired;

                    TB_WIM.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_WIM.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_WIM.ValidationExpression = criteria.RegularExpression;
                    REV_TB_WIM.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_WIM.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_WIM.Enabled = criteria.IsRangeRequired;

                    hdnMinWIMValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxWIMValue.Value = criteria.MaximumValue.ToString();
                    break;


                default:
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
                    cmd.Parameters.AddWithValue("@FormName", "RM_Class_5"); // Replace with actual value

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
                            updateQuery = "UPDATE TRN_RM_CLASS_5 SET Approver1_Status = 1, Approver1_TimeStamp = @TimeStamp WHERE Approver1EmployeeCode = @Condition and Id=@Id";
                            break;
                        case "Approver2":
                            updateQuery = "UPDATE TRN_RM_CLASS_5 SET Approver2_Status = 1, Approver2_TimeStamp = @TimeStamp WHERE Approver2EmployeeCode = @Condition and Id=@Id";
                            break;
                        case "DottedLineApprover":
                            updateQuery = "UPDATE TRN_RM_CLASS_5 SET DottedApprover_Status = 1, DottedApprover_TimeStamp = @TimeStamp WHERE DottedLineApproverEmployeeCode = @Condition and Id=@Id";
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
                            updateQuery = "UPDATE TRN_RM_CLASS_5 SET Approver1_Status = 0, Approver1_TimeStamp = @TimeStamp WHERE Approver1EmployeeCode = @Condition and Id=@Id";
                            break;
                        case "Approver2":
                            updateQuery = "UPDATE TRN_RM_CLASS_5 SET Approver2_Status = 0, Approver2_TimeStamp = @TimeStamp WHERE Approver2EmployeeCode = @Condition and Id=@Id";
                            break;
                        case "DottedLineApprover":
                            updateQuery = "UPDATE TRN_RM_CLASS_5 SET DottedApprover_Status = 0, DottedApprover_TimeStamp = @TimeStamp WHERE DottedLineApproverEmployeeCode = @Condition and Id=@Id";
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