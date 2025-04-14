using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Configuration;

namespace AnmolDristi
{
    public partial class vw_app_qcireport : System.Web.UI.Page
    {
        public static Int32 RecordID =0;
        public static Int32 ViewerMode = 0;

        public static string ImgLink1 = string.Empty;
        public static string ImgLink2 = string.Empty;

        public static string PlantID = string.Empty;
        public static string LineID = string.Empty;
        public static string ProdCategoryID = string.Empty;
        public static string ProdBrand = string.Empty;
        public static string BrandSKU = string.Empty;

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
                    PlantBinder();

                    //below to bind the report details from TRN Tables
                    RecordID = Convert.ToInt32(Request.QueryString["ID"]);
                    ViewerMode = Convert.ToInt32(Request.QueryString["VM"]);
                    // Call the new method to load data
                    LoadRecordData(RecordID);
                }

            }
        }

        private void LoadRecordData(int recordID)
        {
            DataTable dt = FetchRecordById(recordID);
            if (dt.Rows.Count > 0)
            {
                BindData(dt);
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
            string query = "SELECT * FROM TRN_qcinspector WHERE ID = @ID";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        sda.Fill(dt);
                    }
                }
            }
            return dt;
        }

        private void BindData(DataTable dt)
        {
            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];

                PlantID = row["PlantName"].ToString();
                LineID = row["Line"].ToString();
                ProdCategoryID = row["ProductCategory"].ToString();
                ProdBrand = row["ProductBrand"].ToString();
                BrandSKU = row["SKUId"].ToString();

                PlantLinesBinder(PlantID);
                DDL_Plant.SelectedValue = PlantID;
                DDL_PlantLine.SelectedValue = LineID;

                LineProductsBinder(PlantID, LineID);
                DDL_ProductCategory.SelectedValue = ProdCategoryID;

                ProductBrandsBinder(PlantID, LineID, ProdCategoryID);
                DDL_ProductBrand.SelectedValue = ProdBrand;

                BrandSKUBinder(ProdBrand);
                DDL_BrandSKU.SelectedValue = BrandSKU;

                lbl_DDL_Plant_Value.Text = PlantID;

                TB_NoOfPcs.Text = row["NumberOfPieces"].ToString();
                TB_VartyPkt.Text = row["VarietyOrLotNo"].ToString();
                TB_BakingTime.Text = row["BakingTime"].ToString();
                TB_BakingTime2.Text = row["BakingTime2"].ToString();

                string colorAppearanceValue = row["ColorAppearance"].ToString();
                string colorAppearanceComments = row["CommentsForColorAppearance"].ToString();
                if (colorAppearanceValue == "1")
                {
                    RBL_ColorApp.SelectedValue = colorAppearanceValue;
                }
                else
                {
                    ColorAppRemarksDiv.Visible = true;
                    TXB_ColorApp_Remarks.Text = colorAppearanceComments;
                }


                RBL_FlavTst.SelectedValue = row["FlavourAndTaste"].ToString();
                RBL_DesignImp.SelectedValue = row["DesignImplementation"].ToString();
                RBL_TextureBite.SelectedValue = row["TextureBite"].ToString();

                TB_ShapeSize.Text = row["ShapeOrSize"].ToString();
                TB_Moisture.Text = row["Moisture"].ToString();

                TB_aWMAX.Text = row["aW_max"].ToString();
                TB_pHvalue.Text = row["pH_value"].ToString();

                TB_Length.Text = row["GaugeValue"].ToString();
                TB_Breadth.Text = row["DryWeight"].ToString();
                TB_Height.Text = row["DippedWeight"].ToString();

                TB_GaugeLen.Text = row["GaugeLength"].ToString();

                TB_wgtwtoil.Text = row["WeightWithoutOil"].ToString();
                TB_wgtwoil.Text = row["WeightWithOil"].ToString();
                TB_oilpercent.Text = row["OilPercentage"].ToString();

                TB_PktWgt.Text = row["PacketWeight"].ToString();

                //uploadedImage1.ImageUrl = row["DesignAndImplementation"].ToString();
                if (row.Table.Columns.Contains("DesignAndImplementation") && !Convert.IsDBNull(row["DesignAndImplementation"]))
                {
                    string imageUrl = row["DesignAndImplementation"].ToString();

                    // Validate if the image URL exists on the server
                    if (File.Exists(Server.MapPath(imageUrl)))
                    {
                        // Hide file uploader and show the uploaded image
                        FU_DesgImp_Upldr.Visible = false;
                        FU_DesgImp_Img.Visible = true;

                        // Set the valid image URL from the database
                        uploadedImage1.ImageUrl = imageUrl;
                    }
                    else
                    {
                        // If the image URL is invalid, set the default "No Image" placeholder
                        uploadedImage1.ImageUrl = ResolveUrl("~/WebData/No_Image.jpg");

                        // Show the file uploader and hide the image control
                        FU_DesgImp_Upldr.Visible = false;
                        FU_DesgImp_Img.Visible = true;
                    }
                }
                else
                {
                    // If no data exists, display a default "No Image" placeholder
                    uploadedImage1.ImageUrl = ResolveUrl("~/WebData/No_Image.jpg");

                    // Show the file uploader and hide the image control
                    FU_DesgImp_Upldr.Visible = true;
                    FU_DesgImp_Img.Visible = false;
                }

                if (row.Table.Columns.Contains("ColourAndAppearance") && !Convert.IsDBNull(row["ColourAndAppearance"]))
                {
                    string imageUrl = row["ColourAndAppearance"].ToString();

                    // Validate if the image URL exists on the server
                    if (File.Exists(Server.MapPath(imageUrl)))
                    {
                        // Hide file uploader and show the uploaded image
                        FU_ClrApp_Upldr.Visible = false;
                        FU_ClrApp_Img.Visible = true;

                        // Set the valid image URL from the database
                        uploadedImage2.ImageUrl = imageUrl;
                    }
                    else
                    {
                        // If the image URL is invalid, set the default "No Image" placeholder
                        uploadedImage2.ImageUrl = ResolveUrl("~/WebData/No_Image.jpg");

                        // Show the file uploader and hide the image control
                        FU_ClrApp_Upldr.Visible = false;
                        FU_ClrApp_Img.Visible = true;
                    }
                }
                else
                {
                    // If no data exists, display a default "No Image" placeholder
                    uploadedImage2.ImageUrl = ResolveUrl("~/WebData/No_Image.jpg");

                    // Show the file uploader and hide the image control
                    FU_ClrApp_Upldr.Visible = true;
                    FU_ClrApp_Img.Visible = false;
                }

                //uploadedImage2.ImageUrl = row["ColourAndAppearance"].ToString();

                StandardValue_Binder(ProdBrand);
                string FormID = row["FormID"].ToString();
                LoadFormDetails(FormID, PlantID, LineID);


                // Assume the logged-in user's Employee Code is stored in a session variable
                string loggedInUserCode = Session["WORKMAN"].ToString(); // Example session variable

                // Retrieve approval statuses from the row
                App1_Status = row["Approver1_Status"].ToString();
                Approver1CodeLabel.Text = row["Approver1EmployeeCode"].ToString();

                App2_Status = row["Approver2_Status"].ToString();
                Approver2CodeLabel.Text = row["Approver2EmployeeCode"].ToString();

                DottedApp_Status = row["DottedApprover_Status"].ToString();
                DottedLineApproverCodeLabel.Text = row["DottedLineApproverEmployeeCode"].ToString();

                // Boolean flag to track if the logged-in user is one of the approvers
                bool isApprover = false;

                // Approver 1
                if (App1_Status == "0") // Pending
                {
                    Approver1CodeLabel.ForeColor = Color.Brown;
                    if (loggedInUserCode == row["Approver1EmployeeCode"].ToString())
                    {
                        btnApprove.Enabled = true;
                        btnReject.Enabled = true;
                        isApprover = true;
                    }
                }
                else if (App1_Status == "1") // Approved
                {
                    Approver1CodeLabel.ForeColor = Color.Green;
                    if (loggedInUserCode == row["Approver1EmployeeCode"].ToString())
                    {
                        btnApprove.Text = "Approved";
                        btnApprove.Enabled = false;
                        btnReject.Enabled = false;
                        isApprover = true;
                        Lbl_btnSubmit.Text = "You have approved!";
                    }
                }

                // Approver 2
                if (App2_Status == "0") // Pending
                {
                    Approver2CodeLabel.ForeColor = Color.Brown;
                    if (loggedInUserCode == row["Approver2EmployeeCode"].ToString())
                    {
                        btnApprove.Enabled = true;
                        btnReject.Enabled = true;
                        isApprover = true;
                    }
                }
                else if (App2_Status == "1") // Approved
                {
                    Approver2CodeLabel.ForeColor = Color.Green;
                    if (loggedInUserCode == row["Approver2EmployeeCode"].ToString())
                    {
                        btnApprove.Text = "Approved";
                        btnApprove.Enabled = false;
                        btnReject.Enabled = false;
                        isApprover = true;
                        Lbl_btnSubmit.Text = "You have approved!";
                    }
                }

                // Dotted Line Approver
                if (DottedApp_Status == "0") // Pending
                {
                    DottedLineApproverCodeLabel.ForeColor = Color.Brown;
                    if (loggedInUserCode == row["DottedLineApproverEmployeeCode"].ToString())
                    {
                        btnApprove.Enabled = true;
                        btnReject.Enabled = true;
                        isApprover = true;
                    }
                }
                else if (DottedApp_Status == "1") // Approved
                {
                    DottedLineApproverCodeLabel.ForeColor = Color.Green;
                    if (loggedInUserCode == row["DottedLineApproverEmployeeCode"].ToString())
                    {
                        btnApprove.Text = "Approved";
                        btnApprove.Enabled = false;
                        btnReject.Enabled = false;
                        isApprover = true;
                        Lbl_btnSubmit.Text = "You have approved!";
                    }
                }

                // If the logged-in user is not any of the approvers
                if (!isApprover)
                {
                    // Option 1: Disable the buttons
                    btnApprove.Enabled = false;
                    btnReject.Enabled = false;

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
                    Lbl_btnSubmit.Text = "You are not authorized to approve or reject this form.";

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


                

                ScriptManager.RegisterStartupScript(this, GetType(), "triggerButtonClick", Page.ClientScript.GetPostBackEventReference(btnSubmit, ""), true);
            }
        }

        private void PerformLINQQueryOnDataTable(DataTable dt)
        {
            // Assuming you want to query for rows where 'Moisture' is above a certain threshold
            var query = from row in dt.AsEnumerable()
                        where Convert.ToDouble(row["Moisture"]) > 5.0
                        select row;

            foreach (var row in query)
            {
                // Process or display the filtered data
                string moisture = row["Moisture"].ToString();
                // Do something with the result
            }
        }

        private void LoadFormDetails(string FormID, string selectedPlantValue, string selectedPlantLineValue)
        {
            // Replace with your actual connection string
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("usp_GetFormsApprovalMatrix", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Set parameters for the stored procedure
                    cmd.Parameters.AddWithValue("@PlantId", selectedPlantValue); // Replace with actual value
                    cmd.Parameters.AddWithValue("@LineId", selectedPlantLineValue);  // Replace with actual value
                    cmd.Parameters.AddWithValue("@FormID", FormID); // Replace with actual value
                    cmd.Parameters.AddWithValue("@FormName", "qaqc_inspector_rpt"); // Replace with actual value

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

        public void PlantBinder()
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

        private void PlantLinesBinder(string selectedPlantValue)
        {
            string query = "SELECT line_id, line_name FROM MST_Plant_Lines WHERE plant_id = @SelectedPlantValue and view_status=1 and delete_status=0 order by plant_id";
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

        private void LineProductsBinder(string selectedPlantValue, string selectedPlantLineValue)
        {
            // Construct the SQL query with parameters
            string query = "SELECT category_id, category_name FROM MST_LineCategory WHERE plant_id = @PlantId AND line_id = @LineId and view_status=1 and delete_status=0 order by category_id";
            string textField = "category_name"; // Assuming this is the correct field for displaying in the DropDownList
            string valueField = "category_id"; // Assuming this is the correct field for storing in the DropDownList

            // Create SQL parameters for plant_id and line_id
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@PlantId", selectedPlantValue),
                new SqlParameter("@LineId", selectedPlantLineValue)
            };

            // Call the BindDropDownList method with parameters
            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_ProductCategory, textField, valueField, parameters, out recordsBound);

            // Check if any records were bound
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

                // RegisterStartupScript adds the JavaScript code to the page
                ClientScript.RegisterStartupScript(this.GetType(), "ShowLineProductsBinderErrorNotification", PN_Error_script, false);
            }
        }


        private void ProductBrandsBinder(string selectedPlantValue, string selectedPlantLineValue, string selectedProductCategoryValue)
        {
            // Construct the SQL query with parameters
            string query = "SELECT brand_id, brand_name FROM MST_LineCatBrands WHERE plant_id = @PlantId AND line_id = @LineId and category_id=@CategoryId and view_status=1 and delete_status=0 order by brand_id";
            string textField = "brand_name"; // Assuming this is the correct field for displaying in the DropDownList
            string valueField = "brand_id"; // Assuming this is the correct field for storing in the DropDownList

            // Create SQL parameters for plant_id and line_id
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@PlantId", selectedPlantValue),
                new SqlParameter("@LineId", selectedPlantLineValue),
                new SqlParameter("@CategoryId", selectedProductCategoryValue)
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
        }

        private void BrandSKUBinder(string selectedProductBrandValue)
        {
            string query = "SELECT SKUId, SKU_name FROM MST_Brand_SKU WHERE brand_id = @SelectedPlantValue and ViewMode=1 order by SKUId";
            string textField = "SKU_name";
            string valueField = "SKUId";

            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_BrandSKU, textField, valueField, new SqlParameter("@SelectedPlantValue", selectedProductBrandValue), out recordsBound);

            if (!recordsBound)
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_PlantLine);

                string BrandSKUBinder_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'An error occurred!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowBrandSKUBinderErrorNotification", BrandSKUBinder_Error_script, false);
            }
        }

        public class ValidationCriteria
        {
            public string DisplayName { get; set; }
            public string RequiredFieldErrorMessage { get; set; }
            public string RegularExpressionErrorMessage { get; set; }
            public string RegularExpression { get; set; }
            public string RangeErrorMessage { get; set; }
            public string MinimumValue { get; set; }
            public string MaximumValue { get; set; }
            public bool IsRequired { get; set; }
            public bool IsRegularExpressionRequired { get; set; }
            public bool IsRangeRequired { get; set; }
            public bool Visibility { get; set; }
        }

        private void StandardValue_Binder(string ProdBrand)
        {
            DataTable dataTable = DatabaseHelper.GetBrandFieldsControlByBrandId(Convert.ToInt32(ProdBrand));


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
                bool rfvEnabled = Convert.ToBoolean(row["RFV_YesNo"]);

                string rfvErrorMessage = row["RFV_ErrorMsg"].ToString();
                bool revEnabled = Convert.ToBoolean(row["REV_YesNo"]);
                string revErrorMessage = row["REV_ErrorMsg"].ToString();
                string revExpression = row["REV_Expression"].ToString();
                //bool rvEnabled = Convert.ToBoolean(row["RV_Yesno"]);
                bool rvEnabled = true;
                string rvErrorMessage = row["RV_ErrorMsg"].ToString();
                string rvMinValue = row["RV_MinValue"].ToString();
                string rvMaxValue = row["RV_MaxValue"].ToString();
                bool isVisible = Convert.ToBoolean(row["ViewMode"]);

                // Create a new instance of ValidationCriteria and populate it with data from the DataRow
                ValidationCriteria criteria = new ValidationCriteria();
                criteria.DisplayName = displayName;
                criteria.RequiredFieldErrorMessage = rfvErrorMessage;
                criteria.IsRequired = rfvEnabled;
                criteria.RegularExpressionErrorMessage = revErrorMessage;
                criteria.IsRegularExpressionRequired = revEnabled;
                criteria.RegularExpression = revExpression;
                criteria.RangeErrorMessage = rvErrorMessage;
                criteria.IsRangeRequired = rvEnabled;
                criteria.MinimumValue = rvMinValue;
                criteria.MaximumValue = rvMaxValue;
                criteria.Visibility = isVisible;

                // Use the criteria as needed
                // For example, you can pass it to a method to set up validators
                SetUpValidatorsForField(fieldName, criteria);
            }
        }

        private void SetUpValidatorsForField(string fieldName, ValidationCriteria criteria)
        {
            switch (fieldName)
            {
                case "no_of_pcs":

                    TB_NoOfPcs_DIV.Visible = criteria.Visibility;
                    Lbl_TB_NoOfPcs.Text = criteria.DisplayName;

                    RFV_TB_NoOfPcs.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_NoOfPcs.Enabled = criteria.IsRequired;

                    TB_NoOfPcs.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_NoOfPcs.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_NoOfPcs.ValidationExpression = criteria.RegularExpression;
                    REV_TB_NoOfPcs.Enabled = criteria.IsRegularExpressionRequired;

                    RV_TB_NoOfPcs.ErrorMessage = criteria.RangeErrorMessage;
                    RV_TB_NoOfPcs.MinimumValue = criteria.MinimumValue;
                    RV_TB_NoOfPcs.MaximumValue = criteria.MaximumValue;
                    RV_TB_NoOfPcs.Enabled = criteria.IsRangeRequired;

                    //hdnMinNoOfPcs.Value = criteria.MinimumValue.ToString();
                    //hdnMaxNoOfPcs.Value = criteria.MaximumValue.ToString();

                    break;

                case "length":

                    TB_Length_DIV.Visible = criteria.Visibility;
                    Lbl_TB_Length.Text = criteria.DisplayName;

                    RFV_TB_Length.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Length.Enabled = criteria.IsRequired;

                    TB_Length.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Length.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Length.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Length.Enabled = criteria.IsRegularExpressionRequired;

                    RV_TB_Length.ErrorMessage = criteria.RangeErrorMessage;
                    RV_TB_Length.MinimumValue = criteria.MinimumValue;
                    RV_TB_Length.MaximumValue = criteria.MaximumValue;
                    RV_TB_Length.Enabled = criteria.IsRangeRequired;

                    break;

                case "breadth":

                    TB_Breadth_DIV.Visible = criteria.Visibility;
                    Lbl_TB_Breadth.Text = criteria.DisplayName;
                    RFV_TB_Breadth.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Breadth.Enabled = criteria.IsRequired;

                    TB_Breadth.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Breadth.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Breadth.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Breadth.Enabled = criteria.IsRegularExpressionRequired;

                    RV_TB_Breadth.ErrorMessage = criteria.RangeErrorMessage;
                    RV_TB_Breadth.MinimumValue = criteria.MinimumValue;
                    RV_TB_Breadth.MaximumValue = criteria.MaximumValue;
                    RV_TB_Breadth.Enabled = criteria.IsRangeRequired;
                    break;

                case "height":

                    TB_Height_DIV.Visible = criteria.Visibility;
                    Lbl_TB_Height.Text = criteria.DisplayName;
                    RFV_TB_Height.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Height.Enabled = criteria.IsRequired;

                    TB_Height.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Height.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Height.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Height.Enabled = criteria.IsRegularExpressionRequired;

                    RV_TB_Height.ErrorMessage = criteria.RangeErrorMessage;
                    RV_TB_Height.MinimumValue = criteria.MinimumValue;
                    RV_TB_Height.MaximumValue = criteria.MaximumValue;
                    RV_TB_Height.Enabled = criteria.IsRangeRequired;
                    break;

                case "VartyPkt":

                    TB_VartyPkt_DIV.Visible = criteria.Visibility;
                    Lbl_TB_VartyPkt.Text = criteria.DisplayName;

                    RFV_TB_VartyPkt.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_VartyPkt.Enabled = criteria.IsRequired;

                    TB_VartyPkt.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_VartyPkt.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_VartyPkt.ValidationExpression = criteria.RegularExpression;
                    REV_TB_VartyPkt.Enabled = criteria.IsRegularExpressionRequired;

                    //RV_TB_VartyPkt.ErrorMessage = criteria.RangeErrorMessage;
                    //RV_TB_VartyPkt.MinimumValue = criteria.MinimumValue;
                    //RV_TB_VartyPkt.MaximumValue = criteria.MaximumValue;
                    //RV_TB_VartyPkt.Enabled = criteria.IsRangeRequired;
                    break;

                case "BakingTime":

                    TB_BakingTime_DIV.Visible = criteria.Visibility;
                    Lbl_TB_BakingTime.Text = criteria.DisplayName;

                    RFV_TB_BakingTime.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_BakingTime.Enabled = criteria.IsRequired;

                    TB_BakingTime.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_BakingTime.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_BakingTime.ValidationExpression = criteria.RegularExpression;
                    REV_TB_BakingTime.Enabled = criteria.IsRegularExpressionRequired;

                    RV_TB_BakingTime.ErrorMessage = criteria.RangeErrorMessage;
                    RV_TB_BakingTime.MinimumValue = criteria.MinimumValue;
                    RV_TB_BakingTime.MaximumValue = criteria.MaximumValue;
                    RV_TB_BakingTime.Enabled = criteria.IsRangeRequired;

                    hdnMinBakingTime.Value = criteria.MinimumValue.ToString();
                    hdnMaxBakingTime.Value = criteria.MaximumValue.ToString();

                    break;


                case "BakingTime2":

                    TB_BakingTime2_DIV.Visible = criteria.Visibility;
                    Lbl_TB_BakingTime2.Text = criteria.DisplayName;

                    RFV_TB_BakingTime2.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_BakingTime2.Enabled = criteria.IsRequired;

                    TB_BakingTime2.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_BakingTime2.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_BakingTime2.ValidationExpression = criteria.RegularExpression;
                    REV_TB_BakingTime2.Enabled = criteria.IsRegularExpressionRequired;

                    RV_TB_BakingTime2.ErrorMessage = criteria.RangeErrorMessage;
                    RV_TB_BakingTime2.MinimumValue = criteria.MinimumValue;
                    RV_TB_BakingTime2.MaximumValue = criteria.MaximumValue;
                    RV_TB_BakingTime2.Enabled = criteria.IsRangeRequired;

                    hdnMinBakingTime2.Value = criteria.MinimumValue.ToString();
                    hdnMaxBakingTime2.Value = criteria.MaximumValue.ToString();

                    break;

                case "FlavTst":


                    Lbl_RBL_FlavTst.Text = criteria.DisplayName;

                    RFV_RBL_FlavTst.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_RBL_FlavTst.Enabled = criteria.IsRequired;

                    //RBL_FlavTst.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    //REV_RBL_FlavTst.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    //REV_RBL_FlavTst.ValidationExpression = criteria.RegularExpression;
                    //REV_RBL_FlavTst.Enabled = criteria.IsRegularExpressionRequired;

                    //RV_TB_DippedWeight.ErrorMessage = criteria.RangeErrorMessage;
                    //RV_TB_DippedWeight.MinimumValue = criteria.MinimumValue;
                    //RV_TB_DippedWeight.MaximumValue = criteria.MaximumValue;
                    //RV_TB_DippedWeight.Enabled = criteria.IsRangeRequired;
                    break;

                case "SpSz":

                    TB_ShapeSize_DIV.Visible = criteria.Visibility;
                    Lbl_TB_ShapeSize.Text = criteria.DisplayName;

                    RFV_TB_ShapeSize.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_ShapeSize.Enabled = criteria.IsRequired;

                    TB_ShapeSize.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_ShapeSize.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_ShapeSize.ValidationExpression = criteria.RegularExpression;
                    REV_TB_ShapeSize.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_ShapeSize.ErrorMessage = criteria.RangeErrorMessage;
                    //RV_TB_ShapeSize.ErrorMessage = criteria.RangeErrorMessage;
                    //RV_TB_ShapeSize.MinimumValue = criteria.MinimumValue;
                    //RV_TB_ShapeSize.MaximumValue = criteria.MaximumValue;
                    //RV_TB_ShapeSize.Enabled = criteria.IsRangeRequired;

                    hdnMinShapeSize.Value = criteria.MinimumValue.ToString();
                    hdnMaxShapeSize.Value = criteria.MaximumValue.ToString();
                    break;

                case "TextureBite":

                    Lbl_RBL_TextureBite.Text = criteria.DisplayName;
                    RFV_RBL_TextureBite.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_RBL_TextureBite.Enabled = criteria.IsRequired;

                    //TB_TextureBite.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    //REV_TB_TextureBite.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    //REV_TB_TextureBite.ValidationExpression = criteria.RegularExpression;
                    //REV_TB_TextureBite.Enabled = criteria.IsRegularExpressionRequired;

                    //RV_TB_TextureBite.ErrorMessage = criteria.RangeErrorMessage;
                    //RV_TB_TextureBite.MinimumValue = criteria.MinimumValue;
                    //RV_TB_TextureBite.MaximumValue = criteria.MaximumValue;
                    //RV_TB_TextureBite.Enabled = criteria.IsRangeRequired;
                    break;

                case "Moisture":

                    Lbl_TB_Moisture.Text = criteria.DisplayName;

                    RFV_TB_Moisture.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Moisture.Enabled = criteria.IsRequired;

                    TB_Moisture.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Moisture.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Moisture.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Moisture.Enabled = criteria.IsRegularExpressionRequired;

                    RV_TB_Moisture.ErrorMessage = criteria.RangeErrorMessage;
                    RV_TB_Moisture.MinimumValue = criteria.MinimumValue;
                    RV_TB_Moisture.MaximumValue = criteria.MaximumValue;
                    RV_TB_Moisture.Enabled = criteria.IsRangeRequired;
                    break;

                case "aW_max":

                    TB_aWMAX_DIV.Visible = criteria.Visibility;
                    Lbl_TB_aWMAX.Text = criteria.DisplayName;

                    RFV_TB_aWMAX.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_aWMAX.Enabled = criteria.IsRequired;

                    TB_aWMAX.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_aWMAX.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_aWMAX.ValidationExpression = criteria.RegularExpression;
                    REV_TB_aWMAX.Enabled = criteria.IsRegularExpressionRequired;

                    RV_TB_aWMAX.ErrorMessage = criteria.RangeErrorMessage;
                    RV_TB_aWMAX.MinimumValue = criteria.MinimumValue;
                    RV_TB_aWMAX.MaximumValue = criteria.MaximumValue;
                    RV_TB_aWMAX.Enabled = criteria.IsRangeRequired;
                    break;


                case "pH_value":

                    TB_pHvalue_DIV.Visible = criteria.Visibility;
                    Lbl_TB_pHvalue.Text = criteria.DisplayName;

                    RFV_TB_pHvalue.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_pHvalue.Enabled = criteria.IsRequired;

                    TB_pHvalue.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_pHvalue.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_pHvalue.ValidationExpression = criteria.RegularExpression;
                    REV_TB_pHvalue.Enabled = criteria.IsRegularExpressionRequired;

                    RV_TB_pHvalue.ErrorMessage = criteria.RangeErrorMessage;
                    RV_TB_pHvalue.MinimumValue = criteria.MinimumValue;
                    RV_TB_pHvalue.MaximumValue = criteria.MaximumValue;
                    RV_TB_pHvalue.Enabled = criteria.IsRangeRequired;
                    break;

                case "GaugeLen":

                    TB_GaugeLen_DIV.Visible = criteria.Visibility;
                    Lbl_TB_GaugeLen.Text = criteria.DisplayName;

                    RFV_TB_GaugeLen.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_GaugeLen.Enabled = criteria.IsRequired;

                    TB_GaugeLen.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_GaugeLen.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_GaugeLen.ValidationExpression = criteria.RegularExpression;
                    REV_TB_GaugeLen.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_GaugeLen.ErrorMessage = criteria.RangeErrorMessage;
                    //CV_TB_GaugeLen.MinimumValue = criteria.MinimumValue;
                    //RV_TB_GaugeLen.MaximumValue = criteria.MaximumValue;
                    CV_TB_GaugeLen.Enabled = criteria.IsRangeRequired;

                    hdnMinGaugelen.Value = criteria.MinimumValue.ToString();
                    hdnMaxGaugelen.Value = criteria.MaximumValue.ToString();

                    break;

                case "wgtwtoil":

                    TB_wgtwtoil_DIV.Visible = criteria.Visibility;
                    Lbl_TB_wgtwtoil.Text = criteria.DisplayName;

                    //weight without oil or dry weight
                    RFV_TB_wgtwtoil.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_wgtwtoil.Enabled = criteria.IsRequired;

                    TB_wgtwtoil.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_wgtwtoil.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_wgtwtoil.ValidationExpression = criteria.RegularExpression;
                    REV_TB_wgtwtoil.Enabled = criteria.IsRegularExpressionRequired;

                    RV_TB_wgtwtoil.ErrorMessage = criteria.RangeErrorMessage;
                    RV_TB_wgtwtoil.MinimumValue = criteria.MinimumValue;
                    RV_TB_wgtwtoil.MaximumValue = criteria.MaximumValue;
                    RV_TB_wgtwtoil.Enabled = criteria.IsRangeRequired;
                    break;

                case "wgtwoil":

                    TB_wgtwoil_DIV.Visible = criteria.Visibility;
                    Lbl_TB_wgtwoil.Text = criteria.DisplayName;

                    //weight with oil or dipped weight
                    RFV_TB_wgtwoil.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_wgtwoil.Enabled = criteria.IsRequired;

                    TB_wgtwoil.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_wgtwoil.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_wgtwoil.ValidationExpression = criteria.RegularExpression;
                    REV_TB_wgtwoil.Enabled = criteria.IsRegularExpressionRequired;

                    RV_TB_wgtwoil.ErrorMessage = criteria.RangeErrorMessage;
                    RV_TB_wgtwoil.MinimumValue = criteria.MinimumValue;
                    RV_TB_wgtwoil.MaximumValue = criteria.MaximumValue;
                    RV_TB_wgtwoil.Enabled = criteria.IsRangeRequired;
                    break;

                case "oilpercent":

                    TB_oilpercent_DIV.Visible = criteria.Visibility;

                    RV_TB_oilpercent.Text = criteria.DisplayName;

                    RFV_TB_oilpercent.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_oilpercent.Enabled = criteria.IsRequired;

                    TB_oilpercent.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_oilpercent.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_oilpercent.ValidationExpression = criteria.RegularExpression;
                    REV_TB_oilpercent.Enabled = criteria.IsRegularExpressionRequired;

                    RV_TB_oilpercent.ErrorMessage = criteria.RangeErrorMessage;
                    RV_TB_oilpercent.MinimumValue = criteria.MinimumValue;
                    RV_TB_oilpercent.MaximumValue = criteria.MaximumValue;
                    RV_TB_oilpercent.Enabled = criteria.IsRangeRequired;
                    break;

                case "PktWgt":

                    Lbl_TB_PktWgt.Text = criteria.DisplayName;

                    RFV_TB_PktWgt.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_PktWgt.Enabled = criteria.IsRequired;

                    TB_PktWgt.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_PktWgt.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_PktWgt.ValidationExpression = criteria.RegularExpression;
                    REV_TB_PktWgt.Enabled = criteria.IsRegularExpressionRequired;

                    RV_TB_PktWgt.ErrorMessage = criteria.RangeErrorMessage;
                    RV_TB_PktWgt.MinimumValue = criteria.MinimumValue;
                    RV_TB_PktWgt.MaximumValue = criteria.MaximumValue;
                    RV_TB_PktWgt.Enabled = criteria.IsRangeRequired;
                    break;

                case "productview":

                    Lbl_FU_DesgImp.Text = criteria.DisplayName;

                    RFV_FU_DesgImp.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_FU_DesgImp.Enabled = criteria.IsRequired;

                    //TB_PktWgt.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    //REV_TB_PktWgt.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    //REV_TB_PktWgt.ValidationExpression = criteria.RegularExpression;
                    //REV_TB_PktWgt.Enabled = criteria.IsRegularExpressionRequired;

                    //RV_TB_PktWgt.ErrorMessage = criteria.RangeErrorMessage;
                    //RV_TB_PktWgt.MinimumValue = criteria.MinimumValue;
                    //RV_TB_PktWgt.MaximumValue = criteria.MaximumValue;
                    //RV_TB_PktWgt.Enabled = criteria.IsRangeRequired;
                    break;

                case "packetview":

                    Lbl_FU_ClrApp.Text = criteria.DisplayName;

                    RFV_FU_ClrApp.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_FU_ClrApp.Enabled = criteria.IsRequired;

                    //TB_PktWgt.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    //REV_TB_PktWgt.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    //REV_TB_PktWgt.ValidationExpression = criteria.RegularExpression;
                    //REV_TB_PktWgt.Enabled = criteria.IsRegularExpressionRequired;

                    //RV_TB_PktWgt.ErrorMessage = criteria.RangeErrorMessage;
                    //RV_TB_PktWgt.MinimumValue = criteria.MinimumValue;
                    //RV_TB_PktWgt.MaximumValue = criteria.MaximumValue;
                    //RV_TB_PktWgt.Enabled = criteria.IsRangeRequired;
                    break;

                default:
                    // Handle unrecognized field names
                    break;
            }
        }

        protected void btnApprove_Click(object sender, EventArgs e)
        {
            UpdateColumnBasedOnApproverType();
            LoadRecordData(RecordID);
        }

        protected void btnReject_Click(object sender, EventArgs e)
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
                            updateQuery = "UPDATE TRN_qcinspector SET Approver1_Status = 1, Approver1_TimeStamp = @TimeStamp WHERE Approver1EmployeeCode = @Condition and ID=@ID";
                            break;
                        case "Approver2":
                            updateQuery = "UPDATE TRN_qcinspector SET Approver2_Status = 1, Approver2_TimeStamp = @TimeStamp WHERE Approver2EmployeeCode = @Condition and ID=@ID";
                            break;
                        case "DottedLineApprover":
                            updateQuery = "UPDATE TRN_qcinspector SET DottedApprover_Status = 1, DottedApprover_TimeStamp = @TimeStamp WHERE DottedLineApproverEmployeeCode = @Condition and ID=@ID";
                            break;
                    }

                    if (!string.IsNullOrEmpty(updateQuery))
                    {
                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            SqlCommand cmd = new SqlCommand(updateQuery, conn);
                            cmd.Parameters.AddWithValue("@TimeStamp", DateTime.Now);
                            cmd.Parameters.AddWithValue("@Condition", employeeCode);
                            cmd.Parameters.AddWithValue("@ID", RecordID);
                            try
                            {
                                conn.Open();
                                cmd.ExecuteNonQuery();
                                Lbl_btnSubmit.Text = "Approved";
                            }
                            catch (Exception ex)
                            {
                                // Handle exceptions (e.g., logging, rethrowing)
                                //throw new Exception("Error updating the table.", ex);
                                Lbl_btnSubmit.Text = ex.Message;
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
                            updateQuery = "UPDATE TRN_qcinspector SET Approver1_Status = 0, Approver1_TimeStamp = @TimeStamp WHERE Approver1EmployeeCode = @Condition and ID=@ID";
                            break;
                        case "Approver2":
                            updateQuery = "UPDATE TRN_qcinspector SET Approver2_Status = 0, Approver2_TimeStamp = @TimeStamp WHERE Approver2EmployeeCode = @Condition and ID=@ID";
                            break;
                        case "DottedLineApprover":
                            updateQuery = "UPDATE TRN_qcinspector SET DottedApprover_Status = 0, DottedApprover_TimeStamp = @TimeStamp WHERE DottedLineApproverEmployeeCode = @Condition and ID=@ID";
                            break;
                    }

                    if (!string.IsNullOrEmpty(updateQuery))
                    {
                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            SqlCommand cmd = new SqlCommand(updateQuery, conn);
                            cmd.Parameters.AddWithValue("@TimeStamp", DateTime.Now);
                            cmd.Parameters.AddWithValue("@Condition", employeeCode);
                            cmd.Parameters.AddWithValue("@ID", RecordID);
                            try
                            {
                                conn.Open();
                                cmd.ExecuteNonQuery();
                                Lbl_btnSubmit.Text = "Rejected";
                            }
                            catch (Exception ex)
                            {
                                // Handle exceptions (e.g., logging, rethrowing)
                                Lbl_btnSubmit.Text = ex.Message;
                                //throw new Exception("Error updating the table.", ex);
                            }
                        }
                    }
                }
            }
        }

        protected void btnBack_Click(object sender, EventArgs e)
        {
            if (ViewerMode == 0)
            {
                Response.Redirect("qaqc_qcinspector_rpt_.aspx", false);
            }
            else if (ViewerMode == 1)
            {
                Response.Redirect("app_qc_inspectorreport.aspx", false);
            }
            else
            {
                Response.Redirect("home.aspx", false);
            }
        }
    }
}