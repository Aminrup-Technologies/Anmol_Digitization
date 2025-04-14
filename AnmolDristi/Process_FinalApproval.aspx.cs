using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;
using System.Drawing;
using System.IO;

namespace AnmolDristi
{
    public partial class Process_FinalApproval : System.Web.UI.Page
    {
        public static Int32 RecordID = 0;
        public static Int32 ViewerMode = 0;

        public static string PlantId = string.Empty;
        public static string PlantName = string.Empty;
        public static string PlantLine = string.Empty;
        public static string ProductCategory = string.Empty;
        public static string CategoryBrand = string.Empty;
        public static string BrandSKU = string.Empty;

        public static string App1_Status = string.Empty;
        public static string App2_Status = string.Empty;
        public static string DottedApp_Status = string.Empty;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (!string.IsNullOrEmpty(Request.QueryString["PcrNo"]))
                {
                    string pcrNo = Request.QueryString["PcrNo"];
                    int viewerMode = 0;
                    // Safely parse "VM" query parameter
                    if (int.TryParse(Request.QueryString["VM"], out viewerMode))
                    {
                        ViewerMode = viewerMode;
                    }
                    else
                    {
                        ViewerMode = 0; // Default or fallback value
                    }
                    int recordId = 0;
                    // Safely parse "ID" query parameter
                    if (int.TryParse(Request.QueryString["ID"], out recordId))
                    {
                        RecordID = recordId;
                    }
                    else
                    {
                        RecordID = -1; // Default or invalid ID marker
                    }

                    // Call binding methods
                    PlantBinder();
                    DataBinder(pcrNo);
                }
                else
                {
                    // Handle the case where "PcrNo" is missing
                    Response.Write("Error: PcrNo parameter is required.");
                    Response.End();
                }
            }
        }

        private void DataBinder(string pcrNo)
        {
            getBasicDetails(pcrNo);         //Basic Data
            BindGridView_RawWeights(pcrNo); //Raw Weight
            getSpongDetails(pcrNo);         //Sponge
            getDoughDetails(pcrNo);         //Dough
            BindGridView_OvenTemps(pcrNo);  //Oven Temp
            getVerifiedDetails(pcrNo);      //Verified 
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
        private void PlantLinesBinder(string selectedPlantValue)
        {
            string query = "SELECT line_id, line_name FROM MST_Plant_Lines WHERE plant_id = @SelectedPlantValue";
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
            string query = "SELECT category_id, category_name FROM MST_LineCategory WHERE plant_id = @PlantId AND line_id = @LineId";
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
            string query = "SELECT brand_id, brand_name FROM MST_LineCatBrands WHERE plant_id = @PlantId AND line_id = @LineId and category_id=@CategoryId";
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
            string query = "SELECT SKUId, SKU_name FROM MST_Brand_SKU WHERE brand_id = @SelectedPlantValue";
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

        //Basic Data
        void getBasicDetails(string pcrNo)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("SP_PCR_ApprovalData", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@PcrNo", pcrNo);
                        con.Open();
                        using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                        {
                            System.Data.DataTable dt = new System.Data.DataTable();
                            sda.Fill(dt);
                            if (dt.Rows.Count > 0)
                            {
                                DataRow row = dt.Rows[0];
                                string BasicTab = dt.Rows[0]["BasicData_Status"].ToString();
                                if (BasicTab == "1")
                                {
                                    CompleteTab1.Visible = true;
                                    lbl_recordid1.Visible = true;
                                    lbl_recordid1.Text = pcrNo.ToString();
                                    IncompleteTab1.Visible = false;
                                }
                                else
                                {
                                    //when value is 0 , Data record present
                                    CompleteTab1.Visible = false;
                                    lbl_recordid1.Visible = false;
                                    lbl_recordid1.Text = "N/A";
                                    IncompleteTab1.Visible = true;
                                }
                                PlantId = dt.Rows[0]["PlantId"].ToString();
                                PlantName = dt.Rows[0]["plant_name"].ToString();
                                DDL_Plant.SelectedItem.Text = PlantName; //This is for binding the DDL using TEXT

                                PlantLinesBinder(PlantId);
                                PlantLine = dt.Rows[0]["line"].ToString();
                                DDL_PlantLine.SelectedValue = PlantLine; //This is for binding the DDL using Value / ID

                                LineProductsBinder(PlantId, PlantLine);
                                ProductCategory = dt.Rows[0]["ProductCategory"].ToString();
                                DDL_ProductCategory.SelectedValue = ProductCategory;

                                ProductBrandsBinder(PlantId, PlantLine, ProductCategory);
                                CategoryBrand = dt.Rows[0]["ProductBrand"].ToString();
                                DDL_ProductBrand.SelectedValue = CategoryBrand;

                                BrandSKUBinder(CategoryBrand);
                                BrandSKU = dt.Rows[0]["SKUId"].ToString();
                                DDL_BrandSKU.SelectedValue = BrandSKU;

                                string FormID = row["FormID"].ToString();
                                LoadFormDetails(FormID, PlantId, PlantLine);

                                TB_ProcessWaterTemp.Text = dt.Rows[0]["ProcessWaterTemp"].ToString();
                                TB_WaterPH.Text = dt.Rows[0]["WaterPH"].ToString();
                                TB_WaterHardness.Text = dt.Rows[0]["WaterHardness"].ToString();
                                TB_WaterTest.Text = dt.Rows[0]["WaterTest"].ToString();
                                TB_TDS.Text = dt.Rows[0]["TDS"].ToString();

                                TB_MaidaBrand.Text = dt.Rows[0]["MaidaBrand"].ToString();
                                TB_MaidaBatchNo.Text = dt.Rows[0]["MaidaBatchNo"].ToString();
                                TB_MaidaMfg.Text = dt.Rows[0]["MaidaMfgDate"].ToString();

                                RBL_MaidaColorApp.SelectedValue = dt.Rows[0]["MaidaAppearanceColor"].ToString();
                                TXB_MaidaColorApp_Remarks.Text = dt.Rows[0]["CommentForMaidaColor"].ToString();

                                RBL_MaidaFlavorTaste.SelectedValue = dt.Rows[0]["MaidaFlavorAndTaste"].ToString();
                                TXB_MaidaFlavorTaste_Remarks.Text = dt.Rows[0]["CommentsForMaidaFlavourAndTaste"].ToString();

                                RBL_MaidaGrittiness.SelectedValue = dt.Rows[0]["MaidaGrittiness"].ToString();
                                TXB_MaidaGrittiness_Remarks.Text = dt.Rows[0]["CommentForGrittiness"].ToString();

                                RBL_BBColorApp.SelectedValue = dt.Rows[0]["BBAppearanceColor"].ToString();
                                TXB_BBColorApp_Remarks.Text = dt.Rows[0]["CommentForBBColor"].ToString();

                                RBL_BBMouthFeel.SelectedValue = dt.Rows[0]["BBMouthFeel"].ToString();
                                TXB_BBMouthFeel_Remarks.Text = dt.Rows[0]["CommentForBBMouthFeel"].ToString();

                                RBL_BBFlavorTaste.SelectedValue = dt.Rows[0]["BBFlavorAndTaste"].ToString();
                                TXB_BBFlavorTaste_Remarks.Text = dt.Rows[0]["CommentForBBFlavorAndTaste"].ToString();

                                RBL_HvoSmell.SelectedValue = dt.Rows[0]["HvoSmell"].ToString();
                                TXB_HvoSmell_Remarks.Text = dt.Rows[0]["CommentForHvoSmell"].ToString();

                                RBL_HvoTaste.SelectedValue = dt.Rows[0]["HvoTaste"].ToString();
                                TXB_HvoTaste_Remarks.Text = dt.Rows[0]["CommentForHvoTaste"].ToString();

                                TB_HvoTemp.Text = dt.Rows[0]["HvoTemp"].ToString();

                                RBL_SMPSmell.SelectedValue = dt.Rows[0]["SmpSmell"].ToString();
                                TXB_SMPSmell_Remarks.Text = dt.Rows[0]["CommentForSmpSmell"].ToString();

                                RBL_SMPTaste.SelectedValue = dt.Rows[0]["SmpTaste"].ToString();
                                TXB_SMPTaste_Remarks.Text = dt.Rows[0]["CommentForSmpTaste"].ToString();

                                RBL_SMPColor.SelectedValue = dt.Rows[0]["SmpColor"].ToString();
                                TXB_SMPColor_Remarks.Text = dt.Rows[0]["CommentForSmpColor"].ToString();

                                TB_SyrupTemp.Text = dt.Rows[0]["SyrupTemp"].ToString();

                                RBL_SyrupColor.SelectedValue = dt.Rows[0]["SyrupColor"].ToString();
                                TXB_SyrupColor_Remarks.Text = dt.Rows[0]["CommentForSyrupColor"].ToString();

                                TB_SyrupPH.Text = dt.Rows[0]["SyrupPH"].ToString();

                                RBL_InvertSyrupBucket.SelectedValue = dt.Rows[0]["InvertSyrpBucketFilter"].ToString();
                                TXB_InvertSyrupBucket_Remarks.Text = dt.Rows[0]["CommentForISBF"].ToString();

                                RBL_SugarSolBucket.SelectedValue = dt.Rows[0]["SugarSolBucketFilter"].ToString();
                                TXB_SugarSolBucket_Remarks.Text = dt.Rows[0]["CommentForSSBF"].ToString();

                                RBL_CreamerBucketFilter.SelectedValue = dt.Rows[0]["CreamerBucketFilter"].ToString();
                                TXB_CreamerBucket_Remarks.Text = dt.Rows[0]["CommentForCBF"].ToString();

                                RBL_SugarGrinder.SelectedValue = dt.Rows[0]["SugarGrindedSheet"].ToString();
                                TXB_SugarGrinder_Remarks.Text = dt.Rows[0]["CommentForSGS"].ToString();

                                RBL_OilSystem.SelectedValue = dt.Rows[0]["OilSystemBucketFilter"].ToString();
                                TXB_OilSystem_Remarks.Text = dt.Rows[0]["CommentForOSBF"].ToString();

                                RBL_OilSpray.SelectedValue = dt.Rows[0]["OilSpray"].ToString();
                                TXB_OilSpray_Remarks.Text = dt.Rows[0]["CommentForOilSpray"].ToString();

                                RBL_MilkSpray.SelectedValue = dt.Rows[0]["MilkSpray"].ToString();
                                TXB_MilkSpray_Remarks.Text = dt.Rows[0]["CommentForMilkSpray"].ToString();

                                TB_ColdRoomTemp.Text = dt.Rows[0]["ColdRoomTemp"].ToString();
                                TB_DeepFreezeTemp.Text = dt.Rows[0]["DeepFreezeTemp"].ToString();

                                imgMaida.ImageUrl = dt.Rows[0]["MaidaImageUrl"].ToString();
                                imgBB.ImageUrl = dt.Rows[0]["BBImageUrl"].ToString();
                                if (row.Table.Columns.Contains("MaidaImageUrl") && !Convert.IsDBNull(row["MaidaImageUrl"]))
                                {
                                    string imageUrl = row["MaidaImageUrl"].ToString();

                                    // Validate if the image URL exists on the server
                                    if (File.Exists(Server.MapPath(imageUrl)))
                                    {
                                        // Hide file uploader and show the uploaded image
                                        //FU_DesgImp_Upldr.Visible = false;
                                        //FU_DesgImp_Img.Visible = true;

                                        // Set the valid image URL from the database
                                        imgMaida.ImageUrl = imageUrl;
                                    }
                                    else
                                    {
                                        // If the image URL is invalid, set the default "No Image" placeholder
                                        imgMaida.ImageUrl = ResolveUrl("~/WebData/No_Image.jpg");

                                        // Show the file uploader and hide the image control
                                        //FU_DesgImp_Upldr.Visible = false;
                                        //FU_DesgImp_Img.Visible = true;
                                    }
                                }
                                else
                                {
                                    // If no data exists, display a default "No Image" placeholder
                                    imgMaida.ImageUrl = ResolveUrl("~/WebData/No_Image.jpg");

                                    // Show the file uploader and hide the image control
                                    //FU_DesgImp_Upldr.Visible = true;
                                    //FU_DesgImp_Img.Visible = false;
                                }

                                if (row.Table.Columns.Contains("BBImageUrl") && !Convert.IsDBNull(row["BBImageUrl"]))
                                {
                                    string imageUrl = row["BBImageUrl"].ToString();

                                    // Validate if the image URL exists on the server
                                    if (File.Exists(Server.MapPath(imageUrl)))
                                    {
                                        // Hide file uploader and show the uploaded image
                                        //FU_ClrApp_Upldr.Visible = false;
                                        //FU_ClrApp_Img.Visible = true;

                                        // Set the valid image URL from the database
                                        imgBB.ImageUrl = imageUrl;
                                    }
                                    else
                                    {
                                        // If the image URL is invalid, set the default "No Image" placeholder
                                        imgBB.ImageUrl = ResolveUrl("~/WebData/No_Image.jpg");

                                        // Show the file uploader and hide the image control
                                        //FU_ClrApp_Upldr.Visible = false;
                                        //FU_ClrApp_Img.Visible = true;
                                    }
                                }
                                else
                                {
                                    // If no data exists, display a default "No Image" placeholder
                                    imgBB.ImageUrl = ResolveUrl("~/WebData/No_Image.jpg");

                                    // Show the file uploader and hide the image control
                                    //FU_ClrApp_Upldr.Visible = true;
                                    //FU_ClrApp_Img.Visible = false;
                                }

                                // Assume the logged-in user's Employee Code is stored in a session variable
                                string loggedInUserCode = Session["WORKMAN"].ToString(); // Example session variable
                                // Boolean flag to track if the logged-in user is one of the approvers
                                bool isApprover = false;

                                // Approver 1
                                if (App1_Status == "0") // Pending
                                {
                                    Approver1CodeLabel.ForeColor = Color.Brown;
                                    if (loggedInUserCode == dt.Rows[0]["Approver1EmployeeCode"].ToString())
                                    {
                                        btnApprove.Enabled = true;
                                        btnReject.Enabled = true;
                                        isApprover = true;
                                    }
                                }
                                else if (App1_Status == "1") // Approved
                                {
                                    Approver1CodeLabel.ForeColor = Color.Green;
                                    if (loggedInUserCode == dt.Rows[0]["Approver1EmployeeCode"].ToString())
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
                                    if (loggedInUserCode == dt.Rows[0]["Approver2EmployeeCode"].ToString())
                                    {
                                        btnApprove.Enabled = true;
                                        btnReject.Enabled = true;
                                        isApprover = true;
                                    }
                                }
                                else if (App2_Status == "1") // Approved
                                {
                                    Approver2CodeLabel.ForeColor = Color.Green;
                                    if (loggedInUserCode == dt.Rows[0]["Approver2EmployeeCode"].ToString())
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
                                    if (loggedInUserCode == dt.Rows[0]["DottedLineApproverEmployeeCode"].ToString())
                                    {
                                        btnApprove.Enabled = true;
                                        btnReject.Enabled = true;
                                        isApprover = true;
                                    }
                                }
                                else if (DottedApp_Status == "1") // Approved
                                {
                                    DottedLineApproverCodeLabel.ForeColor = Color.Green;
                                    if (loggedInUserCode == dt.Rows[0]["DottedLineApproverEmployeeCode"].ToString())
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
                            }
                        }
                    }
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
                    cmd.Parameters.AddWithValue("@FormName", "qaqc_process_rpt"); // Replace with actual value

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
                            Approver1CodeLabel.Text = row["Approver1EmployeeCode"].ToString();
                            //Approver1Photo.ImageUrl = row["Approver1Photo"].ToString(); // Adjust field name for photo

                            Approver2NameLabel.Text = row["Approver2Name"].ToString();
                            Approver2CodeLabel.Text = row["Approver2EmployeeCode"].ToString();
                            //Approver2Photo.ImageUrl = row["Approver2Photo"].ToString(); // Adjust field name for photo

                            DottedLineApproverNameLabel.Text = row["DottedLineApproverName"].ToString();
                            DottedLineApproverCodeLabel.Text = row["DottedLineApproverEmployeeCode"].ToString();
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


        // Weight data
        public class VarietyInfo
        {
            public string Sl { get; set; }
            public string Variety { get; set; }
            public string StandardWeight { get; set; }
            public string ActualWeight { get; set; }
            public string DeviationWeight { get; set; }
            public string DeviationPercentage { get; set; }
        }
        private void BindGridView_RawWeights(string pcrNo)
        {
            // Step 1: Retrieve the JSON data from the database
            string jsonData = GetRawMaterialWeightsFromDB(pcrNo);

            // Step 2: Deserialize the JSON data into a list of ZoneInfo objects
            if (!string.IsNullOrEmpty(jsonData))
            {
                var varietyInfoList = JsonConvert.DeserializeObject<List<VarietyInfo>>(jsonData);

                // Step 3: Bind the deserialized data to the GridView
                GridView1.DataSource = varietyInfoList;
                GridView1.DataBind();

                //Div1.Visible = true;
                //Label23.Visible = true;
                //Label23.Text = "Success";
            }
            else
            {
                Div2.Visible = true;

                // Handle the case where no data is found
                GridView1.DataSource = null;
                GridView1.DataBind();
            }
        }
        private string GetRawMaterialWeightsFromDB(string pcrNo)
        {
            string jsonData = string.Empty;

            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SP_PCR_ApprovalData", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@PcrNo", pcrNo);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string spongeid = reader["RM_Status"].ToString();
                            if (reader["RM_Status"] != DBNull.Value && !string.IsNullOrEmpty(reader["RM_Status"].ToString()))
                            {
                                if (spongeid == "1")
                                {
                                    jsonData = reader["RM_Weights"].ToString();
                                    Div1.Visible = true;
                                    Div2.Visible = false;
                                    Label23.Visible = true;
                                    Label23.Text = pcrNo.ToString();
                                }
                                else
                                {
                                    Div1.Visible = false;
                                    Div2.Visible = true;
                                }
                            }
                            else
                            {
                                Div1.Visible = false;
                                Div2.Visible = true;
                            }   
                        }
                        else
                        {
                            Div1.Visible = false;
                            Div2.Visible = true;
                        }
                    }
                }
            }

            return jsonData;
        }

        //Spong Data
        void getSpongDetails(string pcrNo)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("SP_PCR_ApprovalData", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@PcrNo", pcrNo);
                        con.Open();
                        using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                        {
                            System.Data.DataTable dt = new System.Data.DataTable();
                            sda.Fill(dt);
                            if (dt.Rows.Count > 0)
                            {
                                DataRow row = dt.Rows[0];
                                string spongeid = dt.Rows[0]["SpongeId"].ToString();
                                if (dt.Rows[0]["SpongeId"] != DBNull.Value && !string.IsNullOrEmpty(dt.Rows[0]["SpongeId"].ToString()))
                                {
                                    TB_RoomTemp.Text = dt.Rows[0]["RoomTemp"].ToString();
                                    TXB_RoomTemp_Remarks.Text = dt.Rows[0]["RoomTempCmnt"].ToString();

                                    RBL_DrumCovered.SelectedValue = dt.Rows[0]["DrumCovered"].ToString();
                                    TXB_DrumCovered_Remarks.Text = dt.Rows[0]["DrumCmnt"].ToString();

                                    RBL_Quality.SelectedValue = dt.Rows[0]["Quality"].ToString();
                                    TXB_Quality_Remarks.Text = dt.Rows[0]["QualityCmnt"].ToString();

                                    TB_StandingTime.Text = dt.Rows[0]["StandingTime"].ToString();

                                    TB_Temp.Text = dt.Rows[0]["Temp"].ToString();
                                    TXB_Temp_Remarks.Text = dt.Rows[0]["TempCmnt"].ToString();

                                    Div3.Visible = true;
                                    Label26.Visible = true;
                                    Label26.Text = spongeid.ToString();
                                }
                                else
                                {
                                    Div4.Visible = true;
                                }
                            }
                            else
                            {
                                Div4.Visible = true;
                            }
                        }
                    }
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

        //Dough Data
        void getDoughDetails(string pcrNo)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("SP_PCR_ApprovalData", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@PcrNo", pcrNo);
                        con.Open();
                        using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                        {
                            System.Data.DataTable dt = new System.Data.DataTable();
                            sda.Fill(dt);
                            if (dt.Rows.Count > 0)
                            {
                                DataRow row = dt.Rows[0];
                                string DoughId = dt.Rows[0]["DoughId"].ToString();
                                if (dt.Rows[0]["DoughId"] != DBNull.Value && !string.IsNullOrEmpty(dt.Rows[0]["DoughId"].ToString()))
                                {
                                    TB_DoughTemp.Text = dt.Rows[0]["DoughTemp"].ToString();
                                    TXB_DoughTemp_Remarks.Text = dt.Rows[0]["DoughTempCmnt"].ToString();

                                    TB_DoughRestTime.Text = dt.Rows[0]["DoughRestTime"].ToString();

                                    RBL_MetalDectector.SelectedValue = dt.Rows[0]["MetalDetector"].ToString();
                                    TXB_MetalDetector_Remarks.Text = dt.Rows[0]["DetectorCmnt"].ToString();

                                    RBL_ProcessSequence.SelectedValue = dt.Rows[0]["ProcessSequence"].ToString();
                                    TXB_ProcessSequence_Remarks.Text = dt.Rows[0]["ProcessCmnt"].ToString();

                                    TB_CreamingTime.Text = dt.Rows[0]["CreamingTime"].ToString();
                                    TB_MixingTime.Text = dt.Rows[0]["MixingTime"].ToString();
                                    TB_BakingTime.Text = dt.Rows[0]["BakingTime"].ToString();

                                    TB_DiceRpm.Text = dt.Rows[0]["DiceRpm"].ToString();

                                    RBL_DoughCondition.SelectedValue = dt.Rows[0]["DoughConditon"].ToString();
                                    TXB_DoughCondition_Remarks.Text = dt.Rows[0]["DoughCmnt"].ToString();
                                    Div5.Visible = true;
                                    Label29.Visible = true;
                                    Label29.Text = DoughId.ToString();
                                }
                                else
                                {
                                    Div6.Visible = true;
                                }
                            }
                            else
                            {
                                Div6.Visible = true;
                            }
                        }
                    }
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

        // Oven data
        public class ZoneInfo
        {
            public string Sl { get; set; }
            public string Zone { get; set; }
            public string OvenTop { get; set; }
            public string OvenBottom { get; set; }
            public string DamperTop { get; set; }
            public string DamperBottom { get; set; }
        }

        private void BindGridView_OvenTemps(string pcrNo)
        {
            // Step 1: Retrieve the JSON data from the database
            string jsonData = GetOvenTemperaturesFromDB(pcrNo);

            // Step 2: Deserialize the JSON data into a list of ZoneInfo objects
            if (!string.IsNullOrEmpty(jsonData))
            {
                var zoneInfoList = JsonConvert.DeserializeObject<List<ZoneInfo>>(jsonData);

                // Step 3: Bind the deserialized data to the GridView
                GridView2.DataSource = zoneInfoList;
                GridView2.DataBind();

                Div7.Visible = true;
            }
            else
            {
                Div8.Visible = true;
                // Handle the case where no data is found
                GridView2.DataSource = null;
                GridView2.DataBind();
            }
        }
        private string GetOvenTemperaturesFromDB(string pcrNo)
        {
            string jsonData = string.Empty;

            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("SP_PCR_ApprovalData", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@PcrNo", pcrNo);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            //jsonData = reader["Oven_Temperatures"].ToString();
                            string spongeid = reader["RM_Status"].ToString();
                            if (reader["OvenData_Status"] != DBNull.Value && !string.IsNullOrEmpty(reader["OvenData_Status"].ToString()))
                            {
                                if (spongeid == "1")
                                {
                                    jsonData = reader["Oven_Temperatures"].ToString();
                                    Div7.Visible = true;
                                    Div8.Visible = false;
                                }
                                else
                                {
                                    Div7.Visible = false;
                                    Div8.Visible = true;
                                }
                            }
                            else
                            {
                                Div7.Visible = false;
                                Div8.Visible = true;
                            }
                        }
                        else
                        {
                            Div7.Visible = false;
                            Div8.Visible = true;
                        }
                    }
                }
            }

            return jsonData;
        }

        // Verified Data
        void getVerifiedDetails(string pcrNo)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("SP_PCR_ApprovalData", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@PcrNo", pcrNo);
                        con.Open();
                        using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                        {
                            System.Data.DataTable dt = new System.Data.DataTable();
                            sda.Fill(dt);
                            if (dt.Rows.Count > 0)
                            {
                                DataRow row = dt.Rows[0];
                                string status = dt.Rows[0]["FinalSubmission"].ToString();
                                if (dt.Rows[0]["FinalSubmission"] != DBNull.Value && !string.IsNullOrEmpty(dt.Rows[0]["FinalSubmission"].ToString()))
                                {
                                    if (status == "1")
                                    {
                                        RBL_BalanceCondition.SelectedValue = dt.Rows[0]["WghBalanceCond"].ToString();
                                        TXB_BalanceCondition_Remarks.Text = dt.Rows[0]["WghtBalanceCmnt"].ToString();

                                        TB_RawBiscuitWgt.Text = dt.Rows[0]["RawBiscuitWgt"].ToString();

                                        Approver1CodeLabel.Text = dt.Rows[0]["Approver1EmployeeCode"].ToString();
                                        Approver2CodeLabel.Text = dt.Rows[0]["Approver2EmployeeCode"].ToString();
                                        DottedLineApproverCodeLabel.Text = dt.Rows[0]["DottedLineApproverEmployeeCode"].ToString();
                                        Div9.Visible = true;
                                        Div10.Visible = false;
                                    }
                                    else
                                    {
                                        Div9.Visible = false;
                                        Div10.Visible = true;
                                    }
                                }
                                else
                                {
                                    Div9.Visible = false;
                                    Div10.Visible = true;
                                }
                            }
                            else
                            {
                                Div9.Visible = false;
                                Div10.Visible = true;
                            }
                        }
                    }
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


        protected void btnApprove_Click(object sender, EventArgs e)
        {
            UpdateColumnBasedOnApproverType();
            //LoadRecordData(RecordID);
        }

        protected void btnReject_Click(object sender, EventArgs e)
        {
            RejectionBasedOnApproverType();
            //LoadRecordData(RecordID);
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
                Response.Redirect("Process_Report.aspx", false);
            }
            else if (ViewerMode == 1)
            {
                Response.Redirect("Process_Approval.aspx", false);
            }
            else
            {
                Response.Redirect("home.aspx", false);
            }
        }
    }
}


