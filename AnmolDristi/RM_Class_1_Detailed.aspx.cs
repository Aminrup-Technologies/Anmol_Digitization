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
    public partial class RM_Class_1_Detailed : System.Web.UI.Page
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
                    BtnBack.PostBackUrl = "~/RM_Class_1_Submitter.aspx";
                    Lbl_BasicbtnApprove.Text = "Click on your Action";

                }
                else
                {
                    BtnBack.PostBackUrl = "~/RM_Class_1_Approval.aspx";
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
                        TRN_RM_CLASS_1 P
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
            string query = "SELECT Material_Id, Material_Name FROM RM_Material where Class = 1";
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

       

        private void PopulateColorDropdown()
        {
            // Add the "Select" option as the first item
            DDL_Color.Items.Add(new ListItem("Select", "0")); // Empty value for default selection

            // Add items to the DropDownList
            DDL_Color.Items.Add(new ListItem("Brown", "Brown"));
            DDL_Color.Items.Add(new ListItem("Yellow", "Yellow"));
            DDL_Color.Items.Add(new ListItem("White", "White"));
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

                PlantBinder();
                PlantId = dt.Rows[0]["PlantId"].ToString();
                PlantName = dt.Rows[0]["plant_name"].ToString();
                DDL_Plant.SelectedValue = PlantId;
                DDL_Plant.Enabled = false;


                TB_BrandName.Text = dt.Rows[0]["Brand"].ToString();
                TB_Supplier.Text = dt.Rows[0]["Supplier_Name"].ToString();

                TB_ChallanNo.Text = dt.Rows[0]["Challan_No"].ToString();
                TB_ChallanDate.Text = dt.Rows[0]["Challan_Date"].ToString();
                TB_LotNo.Text = dt.Rows[0]["Lot_No"].ToString();
                TB_VehicleNo.Text = dt.Rows[0]["Vehicle_No"].ToString();

                TB_Size.Text = dt.Rows[0]["Size"].ToString();
                TB_Quantity.Text = dt.Rows[0]["Quantity"].ToString();
                TXB_Quantity_Remarks.Text = dt.Rows[0]["CommentsForQuantity"].ToString();


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

                TB_PH.Text = dt.Rows[0]["PH"].ToString();
                TXB_PH_Remarks.Text = dt.Rows[0]["CommentsForPH"].ToString();

                TB_Moisture.Text = dt.Rows[0]["Moisture"].ToString();
                TXB_Moisture_Remarks.Text = dt.Rows[0]["CommentsForMoisture"].ToString();

                TB_Protein.Text = dt.Rows[0]["Protein"].ToString();
                TXB_Protein_Remarks.Text = dt.Rows[0]["CommentsForProtein"].ToString();

                TB_TotalAsh.Text = dt.Rows[0]["Total_Ash"].ToString();
                TXB_Ash_Remarks.Text = dt.Rows[0]["CommentsForTotalAsh"].ToString();

                TB_InsolubleAsh.Text = dt.Rows[0]["Acid_Insoluble_Ash"].ToString();
                TXB_InsolubleAsh_Remarks.Text = dt.Rows[0]["CommentsForInsolubleAsh"].ToString();

                TB_Density.Text = dt.Rows[0]["Density"].ToString();
                TXB_Density_Remarks.Text = dt.Rows[0]["CommentsForDensity"].ToString();

                TB_FatContent.Text = dt.Rows[0]["Fat_Content"].ToString();
                TXB_Fat_Remarks.Text = dt.Rows[0]["CommentsForFatContent"].ToString();

                TB_TotalSolid.Text = dt.Rows[0]["Total_Solids"].ToString();
                TXB_Solid_Remarks.Text = dt.Rows[0]["CommentsForSolid"].ToString();

                TB_ReducingSugar.Text = dt.Rows[0]["Reducing_Sugar"].ToString();
                TXB_Sugar_Remarks.Text = dt.Rows[0]["CommentsForSugar"].ToString();

                TB_DrainableSyrup.Text = dt.Rows[0]["Drainable_Syrup"].ToString();
                TXB_Syrup_Remarks.Text = dt.Rows[0]["CommentsForSyrup"].ToString();

                TB_Matured_Immatured_Seeds.Text = dt.Rows[0]["Matured_Immatured_seeds"].ToString();
                TXB_Seeds_Remarks.Text = dt.Rows[0]["CommentsForSeed"].ToString();

                TB_Brix.Text = dt.Rows[0]["Brix"].ToString();
                TXB_Brix_Remarks.Text = dt.Rows[0]["CommentsForBrix"].ToString();

                TB_ShapeOrSize.Text = dt.Rows[0]["ShapeOrSize"].ToString();
                TXB_ShapeOrSize_Remarks.Text = dt.Rows[0]["CommentsForShapeOrSize"].ToString();

                TB_TS.Text = dt.Rows[0]["TS"].ToString();
                TXB_TS_Remarks.Text = dt.Rows[0]["CommentsForTS"].ToString();

                RBL_AppStatus.SelectedValue = dt.Rows[0]["ApprovalStatus"].ToString();
                TXB_AppStatus_Remarks.Text = dt.Rows[0]["Remarks"].ToString();

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
                //case "ProductCategory":

                //    CategoryDIV.Visible = criteria.IsVisible;

                //    RFV_DDL_ProductCategory.ErrorMessage = criteria.RequiredFieldErrorMessage;
                //    RFV_DDL_ProductCategory.Enabled = criteria.IsRequired;

                //    break;


                case "Brand":

                    BrandDIV.Visible = criteria.IsVisible;

                    RFV_TB_BrandName.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_BrandName.Enabled = criteria.IsRequired;

                    TB_BrandName.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_BrandName.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_BrandName.ValidationExpression = criteria.RegularExpression;
                    REV_TB_BrandName.Enabled = criteria.IsRegularExpressionRequired;

                    //hdnMinSupplierValue.Value = criteria.MinimumValue.ToString();
                    //hdnMaxSupplierValue.Value = criteria.MaximumValue.ToString();

                    break;


                case "Supplier":
                    SupplierDIV.Visible = criteria.IsVisible;

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

                    RFV_TB_ChallanDate.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_ChallanDate.Enabled = criteria.IsRequired;

                    break;

                case "ChallanNo":

                    ChallanNoDIV.Visible = criteria.IsVisible;

                    RFV_TB_ChallanNo.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_ChallanNo.Enabled = criteria.IsRequired;

                    TB_ChallanNo.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_ChallanNo.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_ChallanNo.ValidationExpression = criteria.RegularExpression;
                    REV_TB_ChallanNo.Enabled = criteria.IsRegularExpressionRequired;


                    break;

                case "Size":

                    SizeDIV.Visible = criteria.IsVisible;

                    RFV_TB_Size.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Size.Enabled = criteria.IsRequired;

                    TB_Size.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Size.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Size.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Size.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_Size.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_Size.Enabled = criteria.IsRangeRequired;

                    break;

                case "Quantity":

                    QuantityDIV.Visible = criteria.IsVisible;

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

                case "Lot/Batch":

                    LotNoDIV.Visible = criteria.IsVisible;

                    RFV_TB_LotNo.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_LotNo.Enabled = criteria.IsRequired;

                    TB_LotNo.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_LotNo.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_LotNo.ValidationExpression = criteria.RegularExpression;
                    REV_TB_LotNo.Enabled = criteria.IsRegularExpressionRequired;


                    break;

                case "Vehicle":

                    VehicleNoDIV.Visible = criteria.IsVisible;

                    RFV_TB_VehicleNo.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_VehicleNo.Enabled = criteria.IsRequired;

                    TB_VehicleNo.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_VehicleNo.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_VehicleNo.ValidationExpression = criteria.RegularExpression;
                    REV_TB_VehicleNo.Enabled = criteria.IsRegularExpressionRequired;

                    break;

                case "Color":

                    ColorDIV.Visible = criteria.IsVisible;

                    RFV_DDL_Color.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_DDL_Color.Enabled = criteria.IsRequired;

                    break;

                case "Odour/Smell":

                    SmellDIV.Visible = criteria.IsVisible;

                    RFV_RBL_Smell.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_RBL_Smell.Enabled = criteria.IsRequired;

                    break;

                case "Appearance":

                    AppearanceDIV.Visible = criteria.IsVisible;

                    RFV_RBL_Appearance.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_RBL_Appearance.Enabled = criteria.IsRequired;

                    break;

                case "Taste/Flavor":

                    TasteFlavorDIV.Visible = criteria.IsVisible;

                    RFV_RBL_TasteFlavor.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_RBL_TasteFlavor.Enabled = criteria.IsRequired;

                    break;

                case "Foreign Matter/Impurities":

                    ImpuritiesDIV.Visible = criteria.IsVisible;

                    RFV_TB_Foreign_Impurities.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Foreign_Impurities.Enabled = criteria.IsRequired;

                    TB_Foreign_Impurities.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Foreign_Impurities.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Foreign_Impurities.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Foreign_Impurities.Enabled = criteria.IsRegularExpressionRequired;

                    hdnMinImpuritiesValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxImpuritiesValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "PhValue":

                    PHDIV.Visible = criteria.IsVisible;

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

                case "ProteinValue":

                    ProteinDIV.Visible = criteria.IsVisible;

                    RFV_TB_Protein.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Protein.Enabled = criteria.IsRequired;

                    TB_Protein.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Protein.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Protein.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Protein.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_Protein.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_Protein.Enabled = criteria.IsRangeRequired;

                    hdnMinProteinValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxProteinValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "TotalAshValue":

                    AshDIV.Visible = criteria.IsVisible;

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

                case "DensityValue":

                    DensityDIV.Visible = criteria.IsVisible;

                    RFV_TB_Density.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Density.Enabled = criteria.IsRequired;

                    TB_Density.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Density.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Density.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Density.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_Density.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_Density.Enabled = criteria.IsRangeRequired;

                    hdnMinDensityValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxDensityValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "FatContentValue":

                    FatContentDIV.Visible = criteria.IsVisible;

                    RFV_TB_FatContent.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_FatContent.Enabled = criteria.IsRequired;

                    TB_FatContent.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_FatContent.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_FatContent.ValidationExpression = criteria.RegularExpression;
                    REV_TB_FatContent.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_FatContent.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_FatContent.Enabled = criteria.IsRangeRequired;

                    hdnMinFatValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxFatValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "TotalSolidValue":

                    SolidDIV.Visible = criteria.IsVisible;

                    RFV_TB_TotalSolid.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_TotalSolid.Enabled = criteria.IsRequired;

                    TB_TotalSolid.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_TotalSolid.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_TotalSolid.ValidationExpression = criteria.RegularExpression;
                    REV_TB_TotalSolid.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_TotalSolid.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_TotalSolid.Enabled = criteria.IsRangeRequired;

                    hdnMinTotalSolidValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxTotalSolidValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "ReducingSugarValue":

                    SugarDIV.Visible = criteria.IsVisible;

                    RFV_TB_ReducingSugar.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_ReducingSugar.Enabled = criteria.IsRequired;

                    TB_ReducingSugar.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_ReducingSugar.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_ReducingSugar.ValidationExpression = criteria.RegularExpression;
                    REV_TB_ReducingSugar.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_ReducingSugar.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_ReducingSugar.Enabled = criteria.IsRangeRequired;

                    hdnMinSugarValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxSugarValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "DrainableSyrupValue":

                    SyrupDIV.Visible = criteria.IsVisible;

                    RFV_TB_DrainableSyrup.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_DrainableSyrup.Enabled = criteria.IsRequired;

                    TB_DrainableSyrup.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_DrainableSyrup.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_DrainableSyrup.ValidationExpression = criteria.RegularExpression;
                    REV_TB_DrainableSyrup.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_DrainableSyrup.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_DrainableSyrup.Enabled = criteria.IsRangeRequired;

                    hdnMinSyrupValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxSyrupValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "SeedValue":

                    SeedsDIV.Visible = criteria.IsVisible;

                    RFV_TB_Matured_Immatured_Seeds.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Matured_Immatured_Seeds.Enabled = criteria.IsRequired;

                    TB_Matured_Immatured_Seeds.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Matured_Immatured_Seeds.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Matured_Immatured_Seeds.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Matured_Immatured_Seeds.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_Matured_Immatured_Seeds.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_Matured_Immatured_Seeds.Enabled = criteria.IsRangeRequired;

                    hdnMinSeedValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxSeedValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "BrixValue":

                    BrixDIV.Visible = criteria.IsVisible;

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

                case "ShapeOrSizeValue":

                    ShapeOrSizeDIV.Visible = criteria.IsVisible;

                    RFV_TB_ShapeOrSize.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_ShapeOrSize.Enabled = criteria.IsRequired;

                    TB_ShapeOrSize.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_ShapeOrSize.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_ShapeOrSize.ValidationExpression = criteria.RegularExpression;
                    REV_TB_ShapeOrSize.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_ShapeOrSize.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_ShapeOrSize.Enabled = criteria.IsRangeRequired;

                    hdnMinShapeSizeValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxShapeSizeValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "TSValue":

                    TSDIV.Visible = criteria.IsVisible;

                    RFV_TB_TS.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_TS.Enabled = criteria.IsRequired;

                    TB_TS.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_TS.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_TS.ValidationExpression = criteria.RegularExpression;
                    REV_TB_TS.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_TS.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_TS.Enabled = criteria.IsRangeRequired;

                    hdnMinTSValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxTSValue.Value = criteria.MaximumValue.ToString();
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
                    cmd.Parameters.AddWithValue("@FormName", "RM_Class_1"); // Replace with actual value

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
                            updateQuery = "UPDATE TRN_RM_CLASS_1 SET Approver1_Status = 1, Approver1_TimeStamp = @TimeStamp WHERE Approver1EmployeeCode = @Condition and Id=@Id";
                            break;
                        case "Approver2":
                            updateQuery = "UPDATE TRN_RM_CLASS_1 SET Approver2_Status = 1, Approver2_TimeStamp = @TimeStamp WHERE Approver2EmployeeCode = @Condition and Id=@Id";
                            break;
                        case "DottedLineApprover":
                            updateQuery = "UPDATE TRN_RM_CLASS_1 SET DottedApprover_Status = 1, DottedApprover_TimeStamp = @TimeStamp WHERE DottedLineApproverEmployeeCode = @Condition and Id=@Id";
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
                            updateQuery = "UPDATE TRN_RM_CLASS_1 SET Approver1_Status = 0, Approver1_TimeStamp = @TimeStamp WHERE Approver1EmployeeCode = @Condition and Id=@Id";
                            break;
                        case "Approver2":
                            updateQuery = "UPDATE TRN_RM_CLASS_1 SET Approver2_Status = 0, Approver2_TimeStamp = @TimeStamp WHERE Approver2EmployeeCode = @Condition and Id=@Id";
                            break;
                        case "DottedLineApprover":
                            updateQuery = "UPDATE TRN_RM_CLASS_1 SET DottedApprover_Status = 0, DottedApprover_TimeStamp = @TimeStamp WHERE DottedLineApproverEmployeeCode = @Condition and Id=@Id";
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