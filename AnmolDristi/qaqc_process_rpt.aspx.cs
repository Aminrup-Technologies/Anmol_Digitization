using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;
using AnmolDristi.DAL;
using Newtonsoft.Json;
using static AnmolDristi.DAL.QAProcessCheckingDataAcess;

namespace AnmolDristi
{
    public partial class qaqc_ProcessChecking_rpt : System.Web.UI.Page
    {
        DB_Utility_OH4Y dbcl = new DB_Utility_OH4Y();
        public static string ImgLink1 = string.Empty;
        public static string ImgLink2 = string.Empty;
        public static string PcrNo = string.Empty;
        public static string Brand_BTCode = string.Empty;

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
                    hdn_formid.Value = string.Empty;
                    hdn_shiftvalue.Value = string.Empty;

                    lbl_docname.Text = "QA - Process Checking Report";
                    lbl_docnumber.Text = "ANMOL/DOC/CORP/QA/02";
                    PlantBinder();

                    //BindGridView();
                    DisplayCurrentShift();
                    BindGridView1();
                    
                }

            }
        }
        private void DisplayCurrentShift()
        {
            ShiftManager shiftManager = new ShiftManager();
            string currentShift = shiftManager.GetCurrentShiftType();
            hdn_shiftvalue.Value = currentShift;
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

        public class ValidationCriteria
        {
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


        protected void DDL_Plant_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Plant.SelectedIndex != 0)
            {
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                PlantLinesBinder(selectedPlantValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_PlantLine);

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
        protected void DDL_PlantLine_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_PlantLine.SelectedIndex != 0)
            {
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                string selectedPlantLineValue = DDL_PlantLine.SelectedValue.ToString();
                LineProductsBinder(selectedPlantValue, selectedPlantLineValue);
                LoadApprovers(selectedPlantValue, selectedPlantLineValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductCategory);

                string DDL_PlantLine_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'Invalid Selection!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantInvalidErrorNotification", DDL_PlantLine_Error_script, false);
            }

        }

        private void LoadApprovers(string selectedPlantValue, string selectedPlantLineValue)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("usp_GetFormsApprovalMatrix", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@PlantId", selectedPlantValue);
                    //cmd.Parameters.AddWithValue("@LineId", selectedPlantLineValue);
                    cmd.Parameters.AddWithValue("@LineId", string.IsNullOrEmpty(selectedPlantLineValue) ? (object)DBNull.Value : selectedPlantLineValue);
                    cmd.Parameters.AddWithValue("@FormID", 6); // Replace with actual value
                    cmd.Parameters.AddWithValue("@FormName", "qaqc_process_rpt"); // Replace with actual value

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        hdn_formid.Value = "6";
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        if (dt.Rows.Count > 0)
                        {
                            // Populate the GridView
                            GridViewApprovers.DataSource = dt;
                            GridViewApprovers.DataBind();

                            // Populate labels with approver data
                            DataRow row = dt.Rows[0];

                            Approver1NameLabel.Text = row["Approver1Name"].ToString();
                            Approver1CodeLabel.Text = row["Approver1EmployeeCode"].ToString();
                            //Approver1Photo.ImageUrl = row["Approver1Photo"].ToString();

                            Approver2NameLabel.Text = row["Approver2Name"].ToString();
                            Approver2CodeLabel.Text = row["Approver2EmployeeCode"].ToString();
                            //Approver2Photo.ImageUrl = row["Approver2Photo"].ToString();

                            DottedLineApproverNameLabel.Text = row["DottedLineApproverName"].ToString();
                            DottedLineApproverCodeLabel.Text = row["DottedLineApproverEmployeeCode"].ToString();
                            //DottedLineApproverPhoto.ImageUrl = row["DottedLineApproverPhoto"].ToString();
                        }
                        else
                        {
                            // Insert default approvers
                            bool isInserted = dbcl.InsertDefaultApprovers(selectedPlantValue, selectedPlantLineValue, 6);

                            if (isInserted)
                            {
                                // Re-fetch data after insertion (no recursion)
                                da.Fill(dt);
                                if (dt.Rows.Count > 0)
                                {
                                    GridViewApprovers.DataSource = dt;
                                    GridViewApprovers.DataBind();

                                    // Populate labels with approver data
                                    DataRow row = dt.Rows[0];

                                    Approver1NameLabel.Text = row["Approver1Name"].ToString();
                                    Approver1CodeLabel.Text = row["Approver1EmployeeCode"].ToString();
                                    //Approver1Photo.ImageUrl = row["Approver1Photo"].ToString();

                                    Approver2NameLabel.Text = row["Approver2Name"].ToString();
                                    Approver2CodeLabel.Text = row["Approver2EmployeeCode"].ToString();
                                    //Approver2Photo.ImageUrl = row["Approver2Photo"].ToString();

                                    DottedLineApproverNameLabel.Text = row["DottedLineApproverName"].ToString();
                                    DottedLineApproverCodeLabel.Text = row["DottedLineApproverEmployeeCode"].ToString();
                                    //DottedLineApproverPhoto.ImageUrl = row["DottedLineApproverPhoto"].ToString();
                                }
                                else
                                {
                                    ShowErrorNotification("Failed to load approver data even after insertion.");
                                }
                            }
                            else
                            {
                                // If default insertion fails
                                ShowErrorNotification("Failed to insert default approvers.");
                            }
                        }
                    }
                }
            }
        }

        private void ShowErrorNotification(string message)
        {
            string script = $@"<script type='text/javascript'>
                        new PNotify({{
                            title: 'Error',
                            text: '{message}',
                            type: 'error',
                            styling: 'bootstrap3'
                        }});
                      </script>";
            ClientScript.RegisterStartupScript(this.GetType(), "ErrorNotification", script, false);
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
        protected void DDL_ProductCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_ProductCategory.SelectedIndex != 0)
            {
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                string selectedPlantLineValue = DDL_PlantLine.SelectedValue.ToString();
                string selectedProductCategoryValue = DDL_ProductCategory.SelectedValue.ToString();
                ProductBrandsBinder(selectedPlantValue, selectedPlantLineValue, selectedProductCategoryValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductBrand);

                string DDL_PlantLine_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'Invalid Selection!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantInvalidErrorNotification", DDL_PlantLine_Error_script, false);
            }
        }
        private void ProductBrandsBinder(string selectedPlantValue, string selectedPlantLineValue, string selectedProductCategoryValue)
        {
            string query = "SELECT brand_id, brand_name FROM MST_LineCatBrands WHERE plant_id = @PlantId AND line_id = @LineId and category_id=@CategoryId";
            string textField = "brand_name";
            string valueField = "brand_id";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@PlantId", selectedPlantValue),
                new SqlParameter("@LineId", selectedPlantLineValue),
                new SqlParameter("@CategoryId", selectedProductCategoryValue)
            };

            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_ProductBrand, textField, valueField, parameters, out recordsBound);

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

                ClientScript.RegisterStartupScript(this.GetType(), "ShowProductBrandsBinderErrorNotification", ProductBrands_Error_script, false);
            }
        }
        protected void DDL_ProductBrand_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_ProductBrand.SelectedIndex != 0)
            {
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                string selectedProductBrandValue = DDL_ProductBrand.SelectedValue.ToString();
                BrandSKUBinder(selectedProductBrandValue);
                GetSAPCodes(Convert.ToInt32(selectedProductBrandValue));

                //Here goes the function call to get the BT code against the Plant and Brand Selection which will be used for binding the recipe in RM Section


                //BindGridViewfromDB(selectedPlantValue, selectedProductBrandValue);

                DataTable dataTable = DatabaseHelper.GetBrandFieldsControlByBrandId(Convert.ToInt16(selectedProductBrandValue));


                // Example: Querying the DataTable for a specific field name
                //string fieldName = "no_of_pcs"; // Specify the field name you want to query
                //DataRow[] rows = dataTable.Select($"brand_id = {selectedProductBrandValue} AND field_name = '{fieldName}'");

                // Iterate through the filtered rows and extract validation criteria
                foreach (DataRow row in dataTable.Rows)
                {
                    // Extract field name from the current row
                    string fieldName = row["field_name"].ToString();

                    // Extract validation criteria from the DataRow
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
                    //SetUpValidatorsForField(fieldName, criteria);
                }
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductBrand);

                string DDL_ProductBrand_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'Invalid Selection!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowSKUInvalidErrorNotification", DDL_ProductBrand_Error_script, false);
            }
        }

        private void GetSAPCodes(int brandId)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            string query = @"SELECT p.[sap_code] AS Plant_SAP_Code, b.[brand_sapcode] AS Brand_SAP_Code FROM [MST_PlantDetails] p JOIN [MST_LineCatBrands] b ON p.[plant_id] = b.[plant_id] AND  b.[brand_id]= @brand_id"; 
            // Using parameterized query for brand_id

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    // Add the brand_id parameter to the SQL command
                    cmd.Parameters.AddWithValue("@brand_id", brandId);

                    try
                    {
                        // Open the connection
                        conn.Open();

                        // Execute the query and fetch the result in a DataReader
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            // Bind the result to a GridView or process the data as needed
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    string plantSapCode = reader["Plant_SAP_Code"].ToString();
                                    string brandSapCode = reader["Brand_SAP_Code"].ToString();
                                    BindGridViewfromDB(plantSapCode, brandSapCode);
                                    // You can log, display, or store these values
                                    //Response.Write($"Plant SAP Code: {plantSapCode}, Brand SAP Code: {brandSapCode}<br>");
                                }
                            }
                            else
                            {
                                Response.Write("No data found.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Handle any errors
                        Response.Write($"Error: {ex.Message}");
                    }
                }
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

        protected void BtnUploadFU_MaidaImage_Click(object sender, EventArgs e)
        {
            //Maida image
            if (UploadImage1() == true)
            {
                string UI_1_Successscript = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Upload Success',
                                text: 'Image Saved!!',
                                type: 'success',
                                styling: 'bootstrap3'
                            });
                        </script>";

                // RegisterStartupScript adds the JavaScript code to the page
                ClientScript.RegisterStartupScript(this.GetType(), "ShowImage1SuccessNotification", UI_1_Successscript, false);
            }
        }
        private bool UploadImage1()
        {
            bool imgSaved = false;

            try
            {
                string TBPhotoId = "QAPC";
                DateTime now = DateTime.Now;

                // Define the target folder path
                string targetFolderPath = Server.MapPath("~/UploadedFiles/QAPC/MaidaImage/");

                // Check if the target folder exists, if not, create it
                if (!Directory.Exists(targetFolderPath))
                {
                    Directory.CreateDirectory(targetFolderPath);
                }

                // Check if a file is posted
                if (FU_MaidaImage.PostedFile != null)
                {
                    // Check the extension of the image
                    string extension = Path.GetExtension(FU_MaidaImage.FileName);
                    if (extension.ToLower() == ".png" || extension.ToLower() == ".jpg" || extension.ToLower() == ".jpeg")
                    {
                        // Get the uploaded image stream
                        Stream strm = FU_MaidaImage.PostedFile.InputStream;
                        using (var uploadedImage = System.Drawing.Image.FromStream(strm))
                        {

                            // Resize the image
                            int maxWidth = 800;
                            int newWidth = uploadedImage.Width > maxWidth ? maxWidth : uploadedImage.Width;
                            int newHeight = (int)((double)newWidth / uploadedImage.Width * uploadedImage.Height);
                            using (var resizedImage = uploadedImage.GetThumbnailImage(newWidth, newHeight, null, IntPtr.Zero))
                            {
                                string fileName = $"{TBPhotoId}_{DateTime.Now:yyyyMMddHHmmss}.jpg";
                                // Save the resized image to the target folder
                                string targetPath = Path.Combine(targetFolderPath, fileName);
                                resizedImage.Save(targetPath, System.Drawing.Imaging.ImageFormat.Jpeg);

                                // Set the image link for database
                                ImgLink1 = "~/UploadedFiles/QAPC/MaidaImage/" + fileName;
                                //imgfilename = fileName;

                                // Show the image instantly
                                FU_MaidaImage_img.Visible = true;
                                uploadedImage1.ImageUrl = ImgLink1;

                                // Image saved successfully
                                imgSaved = true;

                                FU_MaidaImage_Upldr.Visible = false;
                            }
                        }
                    }
                    else
                    {
                        // Display error notification for inappropriate file type
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception or display an error message

            }

            return imgSaved;
        }
        private bool UploadImage2()
        {
            bool imgSaved = false;

            try
            {
                string TBPhotoId = "QAPC";
                DateTime now = DateTime.Now;

                // Define the target folder path
                string targetFolderPath = Server.MapPath("~/UploadedFiles/QAPC/BBImage/");

                // Check if the target folder exists, if not, create it
                if (!Directory.Exists(targetFolderPath))
                {
                    Directory.CreateDirectory(targetFolderPath);
                }

                // Check if a file is posted
                if (FU_BBImage.PostedFile != null)
                {
                    // Check the extension of the image
                    string extension = Path.GetExtension(FU_BBImage.FileName);
                    if (extension.ToLower() == ".png" || extension.ToLower() == ".jpg" || extension.ToLower() == ".jpeg")
                    {
                        // Get the uploaded image stream
                        Stream strm = FU_BBImage.PostedFile.InputStream;
                        using (var uploadedImage = System.Drawing.Image.FromStream(strm))
                        {

                            // Resize the image
                            int maxWidth = 800;
                            int newWidth = uploadedImage.Width > maxWidth ? maxWidth : uploadedImage.Width;
                            int newHeight = (int)((double)newWidth / uploadedImage.Width * uploadedImage.Height);
                            using (var resizedImage = uploadedImage.GetThumbnailImage(newWidth, newHeight, null, IntPtr.Zero))
                            {
                                string fileName = $"{TBPhotoId}_{DateTime.Now:yyyyMMddHHmmss}.jpg";
                                // Save the resized image to the target folder
                                string targetPath = Path.Combine(targetFolderPath, fileName);
                                resizedImage.Save(targetPath, System.Drawing.Imaging.ImageFormat.Jpeg);

                                // Set the image link for database
                                ImgLink2 = "~/UploadedFiles/QAPC/BBImage/" + fileName;
                                //imgfilename = fileName;

                                // Show the image instantly
                                FU_BBImage_Img.Visible = true;
                                uploadedImage2.ImageUrl = ImgLink2;

                                // Image saved successfully
                                imgSaved = true;

                                FU_BBImage_Upldr.Visible = false;
                            }
                        }
                    }
                    else
                    {
                        // Display error notification for inappropriate file type
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception or display an error message
            }

            return imgSaved;
        }
        protected void BtnUploadFU_BBImage_Click(object sender, EventArgs e)
        {
            //Broken biscuit image
            if (UploadImage2() == true)
            {
                string UI_2_Successscript = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Upload Success',
                                text: 'Image Saved!!',
                                type: 'success',
                                styling: 'bootstrap3'
                            });
                        </script>";

                // RegisterStartupScript adds the JavaScript code to the page
                ClientScript.RegisterStartupScript(this.GetType(), "ShowImage2SuccessNotification", UI_2_Successscript, false);
            }
        }

        protected void BasicBtnSubmit_Click(object sender, EventArgs e)
        {
            if (Page.IsValid)
            {
                // Ensure hidden fields have values before proceeding
                if (!string.IsNullOrWhiteSpace(hdn_img1.Value) && !string.IsNullOrWhiteSpace(hdn_img2.Value))
                {
                    try
                    {
                        // Collect form data
                        ProcessCheckingBasicData data = CollectFormData();

                        // Save the data
                        SaveData(data);

                        // Make inputs read-only and enable the next step
                        MakeInputsReadOnly();
                        WgtbtnSubmit.Enabled = true;
                        WgtbtnSubmit.Visible = true;
                    }
                    catch (Exception ex)
                    {
                        ShowErrorNotification(ex.Message);
                    }
                }
                else
                {
                    // Show error message if hidden fields are empty
                    ShowErrorNotification("Please upload both images before submitting.");
                }
            }
            else
            {
                ShowErrorNotification("Please input all the fields and Upload both the Images");
            }
        }


        private ProcessCheckingBasicData CollectFormData()
        {
            return new ProcessCheckingBasicData
            {
                PcrNo = GenerateUnique(),
                FormID = Convert.ToInt32(hdn_formid.Value.ToString()),
                SubmittedDate = DateTime.Now.Date,
                SubmittedTime = DateTime.Now.TimeOfDay,
                Shift = hdn_shiftvalue.Value.ToString(),
                SubmittedById = Convert.ToInt32(Session["USERID"].ToString()),
                SubmittedByEmployeeCode = Session["WORKMAN"].ToString(),
                PlantName = DDL_Plant.SelectedValue,
                Line = DDL_PlantLine.SelectedValue,
                ProductCategory = DDL_ProductCategory.SelectedValue,
                ProductBrand = DDL_ProductBrand.SelectedValue,
                SKUId = DDL_BrandSKU.SelectedValue,

                ProcessWaterTemp = string.IsNullOrWhiteSpace(TB_ProcessWaterTemp.Text) ? (decimal?)null : decimal.Parse(TB_ProcessWaterTemp.Text),
                ProcessWaterCmnt = TXB_ProcessWaterTemp_Remarks.Text,

                WaterPH = string.IsNullOrWhiteSpace(TB_WaterPH.Text) ? (decimal?)null : decimal.Parse(TB_WaterPH.Text),
                WaterPhCmnt = TXB_WaterPH_Remarks.Text,

                WaterHardness = string.IsNullOrWhiteSpace(TB_WaterHardness.Text) ? (decimal?)null : decimal.Parse(TB_WaterHardness.Text),
                HardnessCmnt = TXB_WaterHardness_Remarks.Text,

                WaterTest = TB_WaterTest.Text,

                TDS = string.IsNullOrWhiteSpace(TB_TDS.Text) ? (decimal?)null : decimal.Parse(TB_TDS.Text),
                TdsCmnt = TXB_TDS_Remarks.Text,

                MaidaBrand = TB_MaidaBrand.Text,
                MaidaBatchNo = TB_MaidaBatchNo.Text,
                MaidaMfgDate = string.IsNullOrWhiteSpace(TB_MaidaMfg.Text) ? (DateTime?)null : DateTime.Parse(TB_MaidaMfg.Text),
                MaidaAppearanceColor = Convert.ToInt32(RBL_MaidaColorApp.SelectedValue),
                CommentForMaidaColor = TXB_MaidaColorApp_Remarks.Text,

                MaidaFlavorAndTaste = Convert.ToInt32(RBL_MaidaFlavorTaste.SelectedValue),
                CommentsForMaidaFlavourAndTaste = TXB_MaidaFlavorTaste_Remarks.Text,

                MaidaGrittiness = Convert.ToInt32(RBL_MaidaGrittiness.SelectedValue),
                CommentForGrittiness = TXB_MaidaGrittiness_Remarks.Text,

                BBAppearanceColor = Convert.ToInt32(RBL_BBColorApp.SelectedValue),
                CommentForBBColor = TXB_BBColorApp_Remarks.Text,

                BBMouthFeel = Convert.ToInt32(RBL_BBMouthFeel.SelectedValue),
                CommentForBBMouthFeel = TXB_BBMouthFeel_Remarks.Text,

                BBFlavorAndTaste = Convert.ToInt32(RBL_BBFlavorTaste.SelectedValue),
                CommentForBBFlavorAndTaste = TXB_BBFlavorTaste_Remarks.Text,

                HvoSmell = Convert.ToInt32(RBL_HvoSmell.SelectedValue),
                CommentForHvoSmell = TXB_HvoSmell_Remarks.Text,

                HvoTaste = Convert.ToInt32(RBL_HvoTaste.SelectedValue),
                CommentForHvoTaste = TXB_HvoTaste_Remarks.Text,

                HvoTemp = string.IsNullOrWhiteSpace(TB_HvoTemp.Text) ? (decimal?)null : decimal.Parse(TB_HvoTemp.Text),
                HvoCmnt = TXB_HvoTemp_Remarks.Text,

                SmpSmell = Convert.ToInt32(RBL_SMPSmell.SelectedValue),
                CommentForSmpSmell = TXB_SMPSmell_Remarks.Text,

                SmpTaste = Convert.ToInt32(RBL_SMPTaste.SelectedValue),
                CommentForSmpTaste = TXB_SMPTaste_Remarks.Text,

                SmpColor = Convert.ToInt32(RBL_SMPColor.SelectedValue),
                CommentForSmpColor = TXB_SMPColor_Remarks.Text,

                SyrupTemp = string.IsNullOrWhiteSpace(TB_SyrupTemp.Text) ? (decimal?)null : decimal.Parse(TB_SyrupTemp.Text),
                SyrupCmnt = TXB_SyrupTemp_Remarks.Text,

                SyrupColor = Convert.ToInt32(RBL_SyrupColor.SelectedValue),
                CommentForSyrupColor = TXB_SyrupColor_Remarks.Text,

                SyrupPH = string.IsNullOrWhiteSpace(TB_SyrupPH.Text) ? (decimal?)null : decimal.Parse(TB_SyrupPH.Text),
                SyrupPhCmnt = TXB_SyrupPH_Remarks.Text,

                InvertSyrpBucketFilter = Convert.ToInt32(RBL_InvertSyrupBucket.SelectedValue),
                CommentForISBF = TXB_InvertSyrupBucket_Remarks.Text,

                SugarSolBucketFilter = Convert.ToInt32(RBL_SugarSolBucket.SelectedValue),
                CommentForSSBF = TXB_SugarSolBucket_Remarks.Text,

                CreamerBucketFilter = Convert.ToInt32(RBL_CreamerBucketFilter.SelectedValue),
                CommentForCBF = TXB_CreamerBucket_Remarks.Text,

                SugarGrindedSheet = Convert.ToInt32(RBL_SugarGrinder.SelectedValue),
                CommentForSGS = TXB_SugarGrinder_Remarks.Text,

                OilSystemBucketFilter = Convert.ToInt32(RBL_OilSystem.SelectedValue),
                CommentForOSBF = TXB_OilSystem_Remarks.Text,

                OilSpray = Convert.ToInt32(RBL_OilSpray.SelectedValue),
                CommentForOilSpray = TXB_OilSpray_Remarks.Text,

                MilkSpray = Convert.ToInt32(RBL_MilkSpray.SelectedValue),
                CommentForMilkSpray = TXB_MilkSpray_Remarks.Text,

                ColdRoomTemp = string.IsNullOrWhiteSpace(TB_ColdRoomTemp.Text) ? (decimal?)null : decimal.Parse(TB_ColdRoomTemp.Text),
                ColdRoomCmnt = TXB_ColdRoomTemp_Remarks.Text,

                DeepFreezeTemp = string.IsNullOrWhiteSpace(TB_DeepFreezeTemp.Text) ? (decimal?)null : decimal.Parse(TB_DeepFreezeTemp.Text),
                DeepFreezeCmnt = TXB_DeepFreezeTemp_Remarks.Text,

                MaidaImageUrl = hdn_img1.Value,
                BBImageUrl = hdn_img2.Value,

                Approver1EmployeeCode = Approver1CodeLabel.Text.ToString(),
                Approver2EmployeeCode = Approver2CodeLabel.Text.ToString(),
                DottedLineApproverEmployeeCode = DottedLineApproverCodeLabel.Text.ToString(),
            };
        }

        private void SaveData(ProcessCheckingBasicData data)
        {
            QAProcessCheckingDataAcess dal = new QAProcessCheckingDataAcess();
            dal.InsertProcessCheckingBasicData(data);
        }

        //private void ShowErrorNotification(string errorMessage)
        //{
        //    string sanitizedMessage = errorMessage.Replace("'", "\\'");
        //    string errorScript = "<script type='text/javascript'>\n" +
        //                         $"new PNotify({{\n" +
        //                         "    title: 'Error',\n" +
        //                         $"    text: '{sanitizedMessage}',\n" +
        //                         "    type: 'error',\n" +
        //                         "    styling: 'bootstrap3'\n" +
        //                         "});\n" +
        //                         "</script>";
        //    ClientScript.RegisterStartupScript(this.GetType(), "ShowErrorNotification", errorScript, false);
        //}


        private void HandleError(Exception ex)
        {
            string errorMessage = ex.Message.Replace("'", "\\'");
            string errorScript = $"<script>new PNotify({{ title: 'Error', text: '{errorMessage}', type: 'error', styling: 'bootstrap3' }});</script>";
            ClientScript.RegisterStartupScript(this.GetType(), "ShowErrorNotification", errorScript, false);
            // Optionally, log the error to a database or file
        }

        protected void BasicBtnSubmit_Click1(object sender, EventArgs e)
        {
            // Retrieve values from controls
            string pcrNo = GenerateUnique();  // Generate unique value
            string plantName = DDL_Plant.SelectedValue;
            string line = DDL_PlantLine.SelectedValue;
            string productCategory = DDL_ProductCategory.SelectedValue;
            string productBrand = DDL_ProductBrand.SelectedValue;
            string skuId = DDL_BrandSKU.SelectedValue;
            string shift = hdn_shiftvalue.Value.ToString();
            decimal processWaterTemp = Convert.ToDecimal(TB_ProcessWaterTemp.Text);
            string commentForProcessWater = "";
            decimal waterPh = Convert.ToDecimal(TB_WaterPH.Text);
            string commentForWaterPh = "";
            decimal waterHardness = Convert.ToDecimal(TB_WaterHardness.Text);
            string commentForWaterHardness = "";
            string waterTest = TB_WaterTest.Text;
            decimal tds = Convert.ToDecimal(TB_TDS.Text);
            string commentForTds = "";
            string maidaBrand = TB_MaidaBrand.Text;
            string maidaBatchNo = TB_MaidaBatchNo.Text;
            DateTime maidaMfg = DateTime.Parse(TB_MaidaMfg.Text).Date;
            int maidaAppColor = Convert.ToInt32(RBL_MaidaColorApp.SelectedValue);
            string commentForMaidaAppColor = TXB_MaidaColorApp_Remarks.Text;
            int maidaFlavorTaste = Convert.ToInt32(RBL_MaidaFlavorTaste.SelectedValue);
            string commentForMaidaFlavorTaste = TXB_MaidaFlavorTaste_Remarks.Text;
            int maidaGrittiness = Convert.ToInt32(RBL_MaidaGrittiness.SelectedValue);
            string commentsForMaidaGrittiness = TXB_MaidaGrittiness_Remarks.Text;
            int bbAppColor = Convert.ToInt32(RBL_BBColorApp.SelectedValue);
            string commentForBBColor = TXB_BBColorApp_Remarks.Text;
            int bbMouthFeel = Convert.ToInt32(RBL_BBMouthFeel.SelectedValue);
            string commentForBBMouthFeel = TXB_BBMouthFeel_Remarks.Text;
            int bbFlavorTaste = Convert.ToInt32(RBL_BBFlavorTaste.SelectedValue);
            string commentForBBFlavorTaste = TXB_BBFlavorTaste_Remarks.Text;
            int hvoSmell = Convert.ToInt32(RBL_HvoSmell.SelectedValue);
            string commentForHvoSmell = TXB_HvoSmell_Remarks.Text;
            int hvoTaste = Convert.ToInt32(RBL_HvoTaste.SelectedValue);
            string commentForHvoTaste = TXB_HvoTaste_Remarks.Text;
            decimal hvoTemp = Convert.ToDecimal(TB_HvoTemp.Text);
            string commentForHvoTemp = "";
            int smpSmell = Convert.ToInt32(RBL_SMPSmell.SelectedValue);
            string commentForSmpSmell = TXB_SMPSmell_Remarks.Text;
            int smpTaste = Convert.ToInt32(RBL_SMPTaste.SelectedValue);
            string commentForSmpTaste = TXB_SMPSmell_Remarks.Text;
            int smpColor = Convert.ToInt32(RBL_SMPColor.SelectedValue);
            string commentForSmpColor = TXB_SMPColor_Remarks.Text;
            decimal syrupTemp = Convert.ToDecimal(TB_SyrupTemp.Text);
            string commentForSyrupTemp = "";
            int syrupColor = Convert.ToInt32(RBL_SyrupColor.SelectedValue);
            string commentForSyrupColor = TXB_SyrupColor_Remarks.Text;
            decimal syrupPh = Convert.ToDecimal(TB_SyrupPH.Text);
            string commentForSyrupPh = "";
            int invertSyrup = Convert.ToInt32(RBL_InvertSyrupBucket.SelectedValue);
            string commentForInvertSyrup = TXB_InvertSyrupBucket_Remarks.Text;
            int sugarSol = Convert.ToInt32(RBL_SugarSolBucket.SelectedValue);
            string commentForSugarSol = TXB_SugarSolBucket_Remarks.Text;
            int creamerBucket = Convert.ToInt32(RBL_CreamerBucketFilter.SelectedValue);
            string commentForCreamerBucket = TXB_CreamerBucket_Remarks.Text;
            int sugarGrinder = Convert.ToInt32(RBL_SugarGrinder.SelectedValue);
            string commentForSugarGrinder = TXB_SugarGrinder_Remarks.Text;
            int oilSystem = Convert.ToInt32(RBL_OilSystem.SelectedValue);
            string commentForOilSystem = TXB_OilSystem_Remarks.Text;
            int oilSpray = Convert.ToInt32(RBL_OilSpray.SelectedValue);
            string commentForOilSpray = TXB_OilSystem_Remarks.Text;
            int milkSpray = Convert.ToInt32(RBL_MilkSpray.SelectedValue);
            string commentForMilkSpray = TXB_MilkSpray_Remarks.Text;
            decimal coldRoomTemp = Convert.ToDecimal(TB_ColdRoomTemp.Text);
            string commentForColdRoom = "";
            decimal DeepFreezeTemp = Convert.ToDecimal(TB_DeepFreezeTemp.Text);
            string commentForDeepFreeze = "";


            //QAProcessCheckingDataAcess dataAccess = new QAProcessCheckingDataAcess();
            
            try
            {
                // Call the InsertBasicData method with the retrieved values
                //dataAccess.InsertBasicData(pcrNo, plantName, line, productCategory, productBrand, skuId, DateTime.Now.Date, shift, DateTime.Now.TimeOfDay, processWaterTemp,
                //                            commentForProcessWater, waterPh, commentForWaterPh, waterHardness, commentForWaterHardness, waterTest, tds, commentForTds,
                //                            maidaBrand, maidaBatchNo, maidaMfg, maidaAppColor, commentForMaidaAppColor, maidaFlavorTaste, commentForMaidaFlavorTaste,
                //                            maidaGrittiness, commentsForMaidaGrittiness, bbAppColor, commentForBBColor, bbMouthFeel, commentForBBMouthFeel,
                //                            bbFlavorTaste, commentForBBFlavorTaste, hvoSmell, commentForHvoSmell, hvoTaste, commentForHvoTaste, hvoTemp,
                //                            commentForHvoTemp, smpSmell, commentForSmpSmell, smpTaste, commentForSmpTaste, smpColor, commentForSmpColor,
                //                            syrupTemp, commentForSyrupTemp, syrupColor, commentForSyrupColor, syrupPh, commentForSyrupPh, invertSyrup,
                //                            commentForInvertSyrup, sugarSol, commentForSugarSol, creamerBucket, commentForCreamerBucket, sugarGrinder,
                //                            commentForSugarGrinder, oilSystem, commentForOilSystem, oilSpray, commentForOilSpray, milkSpray,
                //                            commentForMilkSpray, coldRoomTemp, commentForColdRoom, DeepFreezeTemp, commentForDeepFreeze, ImgLink1, ImgLink2);

                ProcessCheckingBasicData data = new ProcessCheckingBasicData
                {
                    PcrNo = pcrNo,
                    FormID = Convert.ToInt32(hdn_formid.Value.ToString()),
                    SubmittedDate = DateTime.Now.Date,
                    SubmittedTime = DateTime.Now.TimeOfDay,
                    Shift = hdn_shiftvalue.Value.ToString(),
                    SubmittedById = Convert.ToInt32(Session["USERID"].ToString()),
                    SubmittedByEmployeeCode = Session["WORKMAN"].ToString(),
                    PlantName = DDL_Plant.SelectedValue,
                    Line = DDL_PlantLine.SelectedValue,
                    ProductCategory = DDL_ProductCategory.SelectedValue,
                    ProductBrand = DDL_ProductBrand.SelectedValue,
                    SKUId = DDL_BrandSKU.SelectedValue,

                    ProcessWaterTemp = string.IsNullOrWhiteSpace(TB_ProcessWaterTemp.Text) ? (decimal?)null : decimal.Parse(TB_ProcessWaterTemp.Text),
                    ProcessWaterCmnt = TXB_ProcessWaterTemp_Remarks.Text,

                    WaterPH = string.IsNullOrWhiteSpace(TB_WaterPH.Text) ? (decimal?)null : decimal.Parse(TB_WaterPH.Text),
                    WaterPhCmnt = TXB_WaterPH_Remarks.Text,

                    WaterHardness = string.IsNullOrWhiteSpace(TB_WaterHardness.Text) ? (decimal?)null : decimal.Parse(TB_WaterHardness.Text),
                    HardnessCmnt = TXB_WaterHardness_Remarks.Text,

                    WaterTest = TB_WaterTest.Text,

                    TDS = string.IsNullOrWhiteSpace(TB_TDS.Text) ? (decimal?)null : decimal.Parse(TB_TDS.Text),
                    TdsCmnt = TXB_TDS_Remarks.Text,

                    MaidaBrand = TB_MaidaBrand.Text,
                    MaidaBatchNo = TB_MaidaBatchNo.Text,
                    MaidaMfgDate = string.IsNullOrWhiteSpace(TB_MaidaMfg.Text) ? (DateTime?)null : DateTime.Parse(TB_MaidaMfg.Text),
                    MaidaAppearanceColor = Convert.ToInt32(RBL_MaidaColorApp.SelectedValue),
                    CommentForMaidaColor = TXB_MaidaColorApp_Remarks.Text,

                    MaidaFlavorAndTaste = Convert.ToInt32(RBL_MaidaFlavorTaste.SelectedValue),
                    CommentsForMaidaFlavourAndTaste = TXB_MaidaFlavorTaste_Remarks.Text,

                    MaidaGrittiness = Convert.ToInt32(RBL_MaidaGrittiness.SelectedValue),
                    CommentForGrittiness = TXB_MaidaGrittiness_Remarks.Text,

                    BBAppearanceColor = Convert.ToInt32(RBL_BBColorApp.SelectedValue),
                    CommentForBBColor = TXB_BBColorApp_Remarks.Text,

                    BBMouthFeel = Convert.ToInt32(RBL_BBMouthFeel.SelectedValue),
                    CommentForBBMouthFeel = TXB_BBMouthFeel_Remarks.Text,

                    BBFlavorAndTaste = Convert.ToInt32(RBL_BBFlavorTaste.SelectedValue),
                    CommentForBBFlavorAndTaste = TXB_BBFlavorTaste_Remarks.Text,

                    HvoSmell = Convert.ToInt32(RBL_HvoSmell.SelectedValue),
                    CommentForHvoSmell = TXB_HvoSmell_Remarks.Text,

                    HvoTaste = Convert.ToInt32(RBL_HvoTaste.SelectedValue),
                    CommentForHvoTaste = TXB_HvoTemp_Remarks.Text,

                    HvoTemp = string.IsNullOrWhiteSpace(TB_HvoTemp.Text) ? (decimal?)null : decimal.Parse(TB_HvoTemp.Text),
                    HvoCmnt = TXB_HvoTemp_Remarks.Text,

                    SmpSmell = Convert.ToInt32(RBL_SMPSmell.SelectedValue),
                    CommentForSmpSmell = TXB_SMPSmell_Remarks.Text,

                    SmpTaste = Convert.ToInt32(RBL_SMPTaste.SelectedValue),
                    CommentForSmpTaste = TXB_SMPTaste_Remarks.Text,

                    SmpColor = Convert.ToInt32(RBL_SMPColor.SelectedValue),
                    CommentForSmpColor = TXB_SMPSmell_Remarks.Text,

                    SyrupTemp = string.IsNullOrWhiteSpace(TB_SyrupTemp.Text) ? (decimal?)null : decimal.Parse(TB_SyrupTemp.Text),
                    SyrupCmnt = TXB_SyrupTemp_Remarks.Text,

                    SyrupColor = Convert.ToInt32(RBL_SyrupColor.SelectedValue),
                    CommentForSyrupColor = TXB_SyrupColor_Remarks.Text,

                    SyrupPH = string.IsNullOrWhiteSpace(TB_SyrupPH.Text) ? (decimal?)null : decimal.Parse(TB_SyrupPH.Text),
                    SyrupPhCmnt = TXB_SyrupPH_Remarks.Text,

                    InvertSyrpBucketFilter = Convert.ToInt32(RBL_InvertSyrupBucket.SelectedValue),
                    CommentForISBF = TXB_InvertSyrupBucket_Remarks.Text,

                    SugarSolBucketFilter = Convert.ToInt32(RBL_SugarSolBucket.SelectedValue),
                    CommentForSSBF = TXB_SugarSolBucket_Remarks.Text,

                    CreamerBucketFilter = Convert.ToInt32(RBL_CreamerBucketFilter.SelectedValue),
                    CommentForCBF = TXB_CreamerBucket_Remarks.Text,

                    //SugarGrindedSheet = string.IsNullOrWhiteSpace(txtSugarGrindedSheet.Text) ? (int?)null : int.Parse(txtSugarGrindedSheet.Text),
                    SugarGrindedSheet = Convert.ToInt32(RBL_SugarGrinder.SelectedValue),
                    CommentForSGS = TXB_SugarGrinder_Remarks.Text,

                    OilSystemBucketFilter = Convert.ToInt32(RBL_OilSystem.SelectedValue),
                    CommentForOSBF = TXB_OilSystem_Remarks.Text,

                    OilSpray = Convert.ToInt32(RBL_OilSpray.SelectedValue),
                    CommentForOilSpray = TXB_OilSpray_Remarks.Text,

                    MilkSpray = Convert.ToInt32(RBL_MilkSpray.SelectedValue),
                    CommentForMilkSpray = TXB_MilkSpray_Remarks.Text,

                    ColdRoomTemp = string.IsNullOrWhiteSpace(TB_ColdRoomTemp.Text) ? (decimal?)null : decimal.Parse(TB_ColdRoomTemp.Text),
                    ColdRoomCmnt = TXB_ColdRoomTemp_Remarks.Text,

                    DeepFreezeTemp = string.IsNullOrWhiteSpace(TB_DeepFreezeTemp.Text) ? (decimal?)null : decimal.Parse(TB_DeepFreezeTemp.Text),
                    DeepFreezeCmnt = TXB_DeepFreezeTemp_Remarks.Text,

                    MaidaImageUrl = ImgLink1,
                    BBImageUrl = ImgLink2,

                    //WghBalanceCond = Convert.ToInt32(RBL_BalanceCondition.SelectedValue),
                    //WghtBalanceCmnt = TXB_BalanceCondition_Remarks.Text,

                    //RawBiscuitWgt = TB_RawBiscuitWgt.Text,

                    Approver1EmployeeCode = Approver1CodeLabel.Text.ToString(),
                    //Approver1_TimeStamp = string.IsNullOrWhiteSpace(txtApprover1_TimeStamp.Text) ? (TimeSpan?)null : TimeSpan.Parse(txtApprover1_TimeStamp.Text),

                    Approver2EmployeeCode = Approver2CodeLabel.Text.ToString(),
                    //Approver2_TimeStamp = string.IsNullOrWhiteSpace(txtApprover2_TimeStamp.Text) ? (TimeSpan?)null : TimeSpan.Parse(txtApprover2_TimeStamp.Text),

                    DottedLineApproverEmployeeCode = DottedLineApproverCodeLabel.Text.ToString(),
                    //DottedApprover_TimeStamp = string.IsNullOrWhiteSpace(txtDottedApprover_TimeStamp.Text) ? (TimeSpan?)null : TimeSpan.Parse(txtDottedApprover_TimeStamp.Text),
                };


                QAProcessCheckingDataAcess dal = new QAProcessCheckingDataAcess();
                dal.InsertProcessCheckingBasicData(data);

                // Optionally, display a success message or redirect
                //lblMessage.Text = "Data inserted successfully!";

                //Make the inputs readonly
                MakeInputsReadOnly();

                //enable the button in second tab
                WgtbtnSubmit.Enabled = true;
                WgtbtnSubmit.Visible = true;

                //BindGridViewfromDB(plantName, productBrand);
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
        protected void BasicBtnReset_Click(object sender, EventArgs e)
        {
            Response.Redirect("qaqc_process_rpt.aspx");
        }
        private void MakeInputsReadOnly()
        {
            //BasicData
            DDL_Plant.Enabled = false;
            DDL_PlantLine.Enabled = false;
            DDL_ProductCategory.Enabled = false;
            DDL_ProductBrand.Enabled = false;
            DDL_BrandSKU.Enabled = false;
            TB_ProcessWaterTemp.ReadOnly = true;
            TB_WaterHardness.ReadOnly = true;
            TB_WaterTest.ReadOnly = true;
            TB_TDS.ReadOnly = true;
            TB_MaidaBrand.ReadOnly = true;
            TB_MaidaBatchNo.ReadOnly = true;
            TB_MaidaMfg.ReadOnly = true;
            RBL_MaidaColorApp.Enabled = false;
            TXB_MaidaColorApp_Remarks.ReadOnly = true;
            RBL_MaidaFlavorTaste.Enabled = false;
            TXB_MaidaFlavorTaste_Remarks.ReadOnly = true;
            RBL_MaidaGrittiness.Enabled = false;
            TXB_MaidaGrittiness_Remarks.ReadOnly = true;
            RBL_BBColorApp.Enabled = false;
            TXB_BBColorApp_Remarks.ReadOnly = true;
            RBL_BBFlavorTaste.Enabled = false;
            TXB_BBFlavorTaste_Remarks.ReadOnly = true;
            RBL_BBMouthFeel.Enabled = false;
            TXB_BBMouthFeel_Remarks.ReadOnly = true;
            RBL_HvoSmell.Enabled = false;
            TXB_HvoSmell_Remarks.ReadOnly = true;
            RBL_HvoTaste.Enabled = false;
            TXB_HvoTaste_Remarks.ReadOnly = true;
            TB_HvoTemp.ReadOnly = true;
            RBL_SMPSmell.Enabled = false;
            TXB_SMPSmell_Remarks.ReadOnly = true;
            RBL_SMPTaste.Enabled = false;
            TXB_SMPTaste_Remarks.ReadOnly = true;
            RBL_SMPColor.Enabled = false;
            TXB_SMPColor_Remarks.ReadOnly = true;
            TB_SyrupTemp.ReadOnly = true;
            RBL_SyrupColor.Enabled = false;
            TXB_SyrupColor_Remarks.ReadOnly = true;
            TB_SyrupPH.ReadOnly = true;
            RBL_InvertSyrupBucket.Enabled = false;
            TXB_InvertSyrupBucket_Remarks.ReadOnly = true;
            RBL_SugarSolBucket.Enabled = false;
            TXB_SugarSolBucket_Remarks.ReadOnly = true;
            RBL_CreamerBucketFilter.Enabled = false;
            TXB_CreamerBucket_Remarks.ReadOnly = true;
            RBL_SugarGrinder.Enabled = false;
            TXB_SugarGrinder_Remarks.ReadOnly = true;
            RBL_OilSystem.Enabled = false;
            TXB_OilSystem_Remarks.ReadOnly = true;
            RBL_OilSpray.Enabled = false;
            TXB_OilSpray_Remarks.ReadOnly = true;
            RBL_MilkSpray.Enabled = false;
            TXB_MilkSpray_Remarks.ReadOnly = true;
            TB_ColdRoomTemp.ReadOnly = true;
            TB_DeepFreezeTemp.ReadOnly = true;

            ImgLink1 = string.Empty;
            ImgLink2 = string.Empty;

            BasicBtnSubmit.Enabled = false;
            BasicBtnSubmit.Text = "SAVED";
            BasicBtnSubmit.CssClass = "btn btn-sm btn-success";
            BasicBtnReset.Enabled = false;

            string Data_SuccessScript = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Data Success',
                                text: 'Recorded Successfully!!',
                                type: 'success',
                                styling: 'bootstrap3'
                            });
                        </script>";

            // RegisterStartupScript adds the JavaScript code to the page
            ClientScript.RegisterStartupScript(this.GetType(), "ShowDataSuccessNotification", Data_SuccessScript, false);
            ScriptManager.RegisterStartupScript(this, this.GetType(), "SwitchTab", "document.getElementById('weight-tab').click();", true);

        }

        //protected void RawBtnSubmit_Click(object sender, EventArgs e)
        //{
        //    // Retrieve values from controls

        //    string maidaBrandName = TB_MaidaBrandName.Text;
        //    decimal maidaWgt = Convert.ToDecimal(TB_MaidaActWgt.Text);
        //    decimal sugarWgt = Convert.ToDecimal(TB_SugarActWgt.Text);
        //    decimal butter = Convert.ToDecimal(TB_ButterActWgt.Text);
        //    decimal smp = Convert.ToDecimal(TB_SMPActWgt.Text);
        //    decimal processWater = Convert.ToDecimal(TB_PWActWgt.Text);
        //    decimal lecithin = Convert.ToDecimal(TB_LecithinActWgt.Text);
        //    decimal gms = Convert.ToDecimal(TB_GMSActWgt.Text);
        //    decimal ssl = Convert.ToDecimal(TB_SSLActWgt.Text);
        //    decimal glucose = Convert.ToDecimal(TB_GlucoseActWgt.Text);
        //    decimal hvo = Convert.ToDecimal(TB_HVOActWgt.Text);
        //    decimal syrup = Convert.ToDecimal(TB_SyrupActWgt.Text);
        //    decimal malt = Convert.ToDecimal(TB_MaltActWgt.Text);
        //    decimal bb = Convert.ToDecimal(TB_BBActWgt.Text);
        //    decimal abc = Convert.ToDecimal(TB_ABCActWgt.Text);
        //    decimal sbc = Convert.ToDecimal(TB_SBCActWgt.Text);
        //    decimal smbs = Convert.ToDecimal(TB_SMBSActWgt.Text);
        //    decimal wheyPowder = Convert.ToDecimal(TB_WheyPowderActWgt.Text);
        //    decimal condenceMilk = Convert.ToDecimal(TB_MilkActWgt.Text);
        //    decimal salt = Convert.ToDecimal(TB_SaltActWgt.Text);
        //    decimal yeast = Convert.ToDecimal(TB_YeastActWgt.Text);
        //    decimal e1 = Convert.ToDecimal(TB_E1ActWgt.Text);
        //    decimal caramel = Convert.ToDecimal(TB_CaramelActWgt.Text);

        //    QAProcessCheckingDataAcess dataAccess = new QAProcessCheckingDataAcess();

        //    try
        //    {
        //        // Call the InsertRawMaterialData method with the retrieved values
        //        dataAccess.InsertRawMaterialData(PcrNo, maidaBrandName, maidaWgt, sugarWgt, butter, smp, processWater, lecithin, gms, ssl, glucose, hvo, syrup,
        //                                    malt, bb, abc, sbc, smbs, wheyPowder, condenceMilk, salt, yeast, e1, caramel);

        //        //Make the inputs readonly
        //        MakeInputsReadOnly1();
        //    }

        //    catch (Exception ex)
        //    {
        //        string errorMessage = ex.Message.Replace("'", "\\'"); // Escape single quotes in the error message
        //        string errorScript = "<script type='text/javascript'>\n" +
        //                             $"new PNotify({{\n" +
        //                             "    title: 'Error',\n" +
        //                             $"    text: '{errorMessage}',\n" +
        //                             "    type: 'error',\n" +
        //                             "    styling: 'bootstrap3'\n" +
        //                             "});\n" +
        //                             "</script>";
        //        ClientScript.RegisterStartupScript(this.GetType(), "ShowErrorNotification", errorScript, false);
        //    }
        //}
        //protected void RawBtnReset_Click(object sender, EventArgs e)
        //{
        //    Response.Redirect("qaqc_process_rpt.aspx");
        //    ScriptManager.RegisterStartupScript(this, GetType(), "activateTab", "activateTab('rawMaterial-tab');", true);
        //    //or
        //    // ScriptManager.RegisterStartupScript(this, this.GetType(), "clearAndActivate", "clearFields('rawMaterial-tab'); activateTab();", true);
        //    //or
        //    //ScriptManager.RegisterStartupScript(this, this.GetType(), "resetSection", "resetSectionFields('rawMaterial-tab');", true);

        //}
        //private void MakeInputsReadOnly1()
        //{
        //    //Raw Material Data
        //    TB_MaidaActWgt.ReadOnly = true;
        //    TB_SugarActWgt.ReadOnly = true;
        //    TB_ButterActWgt.ReadOnly = true;
        //    TB_SMPActWgt.ReadOnly = true;
        //    TB_ProcessWaterTemp.ReadOnly = true;
        //    TB_LecithinActWgt.ReadOnly = true;
        //    TB_GMSActWgt.ReadOnly = true;
        //    TB_SSLActWgt.ReadOnly = true;
        //    TB_GlucoseActWgt.ReadOnly = true;
        //    TB_HVOActWgt.ReadOnly = true;
        //    TB_SyrupActWgt.ReadOnly = true;
        //    TB_MaltActWgt.ReadOnly = true;
        //    TB_BBActWgt.ReadOnly = true;
        //    TB_ABCActWgt.ReadOnly = true;
        //    TB_SBCActWgt.ReadOnly = true;
        //    TB_SMBSActWgt.ReadOnly = true;
        //    TB_WheyPowderActWgt.ReadOnly = true;
        //    TB_MilkActWgt.ReadOnly = true;
        //    TB_SaltActWgt.ReadOnly = true;
        //    TB_YeastActWgt.ReadOnly = true;
        //    TB_E1ActWgt.ReadOnly = true;
        //    TB_CaramelActWgt.ReadOnly = true;


        //    BasicBtnSubmit.Enabled = false;
        //    BasicBtnSubmit.Text = "SAVED";
        //    BasicBtnSubmit.CssClass = "btn btn-sm btn-success";

        //    string Data_SuccessScript = @"<script type='text/javascript'>
        //            new PNotify({
        //                title: 'Data Success',
        //                text: 'Recorded Successfully!!',
        //                type: 'success',
        //                styling: 'bootstrap3'
        //            });
        //        </script>";

        //    // RegisterStartupScript adds the JavaScript code to the page
        //    ClientScript.RegisterStartupScript(this.GetType(), "ShowDataSuccessNotification", Data_SuccessScript, false);
        //    ScriptManager.RegisterStartupScript(this, this.GetType(), "SwitchTab", "document.getElementById('spongeData-tab').click();", true);
        //}


        protected void SpongeBtnSubmit_Click(object sender, EventArgs e)
        {

            if (string.IsNullOrWhiteSpace(PcrNo))
            {

            }
            else
            {
                // Retrieve values from controls
                decimal roomTemp = Convert.ToDecimal(TB_RoomTemp.Text);
                string commentForRoomTemp = TXB_RoomTemp_Remarks.Text;
                int drumCovered = Convert.ToInt32(RBL_DrumCovered.SelectedValue);
                string commentForDrumCovered = TXB_DrumCovered_Remarks.Text;
                int quality = Convert.ToInt32(RBL_Quality.SelectedValue);
                string commentForQuality = TXB_Quality_Remarks.Text;
                TimeSpan standingTime = TimeSpan.Parse(TB_StandingTime.Text);
                string commentForStandingTime = "";
                decimal temp = Convert.ToDecimal(TB_Temp.Text);
                string commentForTemp = TXB_Temp_Remarks.Text;

                QAProcessCheckingDataAcess dataAccess = new QAProcessCheckingDataAcess();


                try
                {
                    // Call the InsertSpongeData method with the retrieved values
                    dataAccess.InsertSpongeData(PcrNo, roomTemp, commentForRoomTemp, drumCovered, commentForDrumCovered, quality,
                                                    commentForQuality, standingTime, commentForStandingTime, temp, commentForTemp, "Yes");

                    //Make the inputs readonly
                    MakeInputsReadOnly2();

                    DoughBtnSubmit.Visible = true;
                    DoughBtnSubmit.Enabled = true;

                }
                catch (Exception ex)
                {
                    var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                    EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

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
        }
        protected void Spongebtn_Reset_Click(object sender, EventArgs e)
        {
            ScriptManager.RegisterStartupScript(this, GetType(), "activateTab", "activateTab('spongeData-tab');", true);
            Response.Redirect("qaqc_process_rpt.aspx");
        }
        private void MakeInputsReadOnly2()
        {
            //SpongeData
            TB_RoomTemp.ReadOnly = true;
            TXB_RoomTemp_Remarks.ReadOnly = true;
            RBL_DrumCovered.Enabled = false;
            TXB_DrumCovered_Remarks.ReadOnly = true;
            RBL_Quality.Enabled = false;
            TXB_Quality_Remarks.ReadOnly = true;
            TB_StandingTime.ReadOnly = true;
            TB_Temp.ReadOnly = true;
            TXB_Temp_Remarks.ReadOnly = true;

            SpongeBtnSubmit.Enabled = false;
            SpongeBtnSubmit.Text = "SAVED";
            SpongeBtnSubmit.CssClass = "btn btn-sm btn-success";
            Spongebtn_Reset.Enabled = false;

            string Data_SuccessScript = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Data Success',
                                text: 'Recorded Successfully!!',
                                type: 'success',
                                styling: 'bootstrap3'
                            });
                        </script>";

            // RegisterStartupScript adds the JavaScript code to the page
            ClientScript.RegisterStartupScript(this.GetType(), "ShowDataSuccessNotification", Data_SuccessScript, false);
            ScriptManager.RegisterStartupScript(this, this.GetType(), "SwitchTab", "document.getElementById('doughData-tab').click();", true);

        }


        protected void DoughBtnSubmit_Click(object sender, EventArgs e)
        {
            // Retrieve values from controls
            decimal doughTemp = Convert.ToDecimal(TB_DoughTemp.Text);
            string commentForDoughTemp = TXB_DoughTemp_Remarks.Text;
            TimeSpan restTime = TimeSpan.Parse(TB_DoughRestTime.Text);
            string commentForRestTime = "";
            int metalDetector = Convert.ToInt32(RBL_MetalDectector.SelectedValue);
            string commentForMetalDetector = TXB_MetalDetector_Remarks.Text;
            int processSequence = Convert.ToInt32(RBL_ProcessSequence.SelectedValue);
            string commentForProcessSequence = TXB_ProcessSequence_Remarks.Text;
            TimeSpan creamingTime = TimeSpan.Parse(TB_CreamingTime.Text);
            string commentForCreamingTime = "";
            TimeSpan mixingTime = TimeSpan.Parse(TB_MixingTime.Text);
            string commentForMixingTime = "";
            TimeSpan bakingTime = TimeSpan.Parse(TB_BakingTime.Text);
            string commentForBakingTime = "";
            int diceRmp = Convert.ToInt32(TB_DiceRpm.Text);
            string commentForDice = "";
            int doughCondition = Convert.ToInt32(RBL_DoughCondition.SelectedValue);
            string commentForDoughCondition = TXB_DoughCondition_Remarks.Text;

            QAProcessCheckingDataAcess dataAccess = new QAProcessCheckingDataAcess();

            try
            {
                // Call the InsertDoughData method with the retrieved values
                dataAccess.InsertDoughData(PcrNo, doughTemp, commentForDoughTemp, restTime, commentForRestTime, metalDetector,
                                            commentForMetalDetector, processSequence, commentForProcessSequence, creamingTime, commentForCreamingTime,
                                            mixingTime, commentForMixingTime, bakingTime, commentForBakingTime, diceRmp, commentForDice, doughCondition,
                                                commentForDoughCondition);

                //Make the inputs readonly
                MakeInputsReadOnly3();

                OvenBtnSubmit.Visible = true;
                OvenBtnSubmit.Enabled = true;
            }
            catch (Exception ex)
            {
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

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
        protected void DoughBtnReset_Click(object sender, EventArgs e)
        {
            ScriptManager.RegisterStartupScript(this, GetType(), "activateTab", "activateTab('doughData-tab');", true);
            Response.Redirect("qaqc_process_rpt.aspx");
        }
        private void MakeInputsReadOnly3()
        {
            //DoughData
            TB_DoughTemp.ReadOnly = true;
            TXB_DoughTemp_Remarks.ReadOnly = true;
            TB_DoughRestTime.ReadOnly = true;
            RBL_MetalDectector.Enabled = false;
            TXB_MetalDetector_Remarks.ReadOnly = true;
            RBL_ProcessSequence.Enabled = false;
            TXB_ProcessSequence_Remarks.ReadOnly = true;
            TB_CreamingTime.ReadOnly = true;
            TB_MixingTime.ReadOnly = true;
            TB_BakingTime.ReadOnly = true;
            TB_DiceRpm.ReadOnly = true;
            RBL_DoughCondition.Enabled = false;
            TXB_DoughCondition_Remarks.ReadOnly = true;


            DoughBtnSubmit.Enabled = false;
            DoughBtnSubmit.Text = "SAVED";
            DoughBtnSubmit.CssClass = "btn btn-sm btn-success";
            DoughBtnReset.Enabled = false;

            string Data_SuccessScript = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Data Success',
                                text: 'Recorded Successfully!!',
                                type: 'success',
                                styling: 'bootstrap3'
                            });
                        </script>";

            // RegisterStartupScript adds the JavaScript code to the page
            ClientScript.RegisterStartupScript(this.GetType(), "ShowDataSuccessNotification", Data_SuccessScript, false);
            ScriptManager.RegisterStartupScript(this, this.GetType(), "SwitchTab", "document.getElementById('ovenData-tab').click();", true);

        }

        protected void FinalBtnSubmit_Click(object sender, EventArgs e)
        {
            // Retrieve values from controls
            int balanceCond = Convert.ToInt32(RBL_BalanceCondition.SelectedValue);
            string commentForBalanceCond = TXB_BalanceCondition_Remarks.Text;
            string rawBiscuitWgt = TB_RawBiscuitWgt.Text;
            //int submittedById = Convert.ToInt32(null);
            //DateTime submittedDate = DateTime.Now.Date;
            //TimeSpan submittedTime = DateTime.Now.TimeOfDay;
            //string submittedByPno = null;
            //string submittedByEmployeeCode = null;
            //string approver1EmployeeCode = null;
            //int approver1_Status = Convert.ToInt32(null);
            //TimeSpan? approver1_TimeStamp = null;
            //string approver2EmployeeCode = null;
            //int approver2_Status = Convert.ToInt32(null);
            //TimeSpan? approver2_TimeStamp = null;
            //string dottedLineApproverEmployeeCode = null;
            //int dottedApprover_Status = Convert.ToInt32(null);
            //TimeSpan? dottedApprover_TimeStamp = null;


            //QAProcessCheckingDataAcess dataAccess = new QAProcessCheckingDataAcess();

            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    string query = "UPDATE TRN_ProcessChecking_BasicData SET WghBalanceCond=@WghBalanceCond, WghtBalanceCmnt=@WghtBalanceCmnt, RawBiscuitWgt=@RawBiscuitWgt, FinalSubmission=@FinalSubmission where PcrNo=@PcrNo";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PcrNo ", PcrNo);
                        cmd.Parameters.AddWithValue("@WghBalanceCond", balanceCond);
                        cmd.Parameters.AddWithValue("@WghtBalanceCmnt", commentForBalanceCond);
                        cmd.Parameters.AddWithValue("@RawBiscuitWgt", rawBiscuitWgt);
                        cmd.Parameters.AddWithValue("@FinalSubmission", 1);
                        cmd.ExecuteNonQuery();
                    }

                }

                // Call the InsertVerifiedData method with the retrieved values
                //dataAccess.InsertVerifiedData(PcrNo, balanceCond, commentForBalanceCond, rawBiscuitWgt, submittedById, submittedDate,
                //                                submittedTime, submittedByPno, submittedByEmployeeCode, approver1EmployeeCode, approver1_Status,
                //                                approver1_TimeStamp, approver2EmployeeCode, approver2_Status, approver2_TimeStamp,
                //                                    dottedLineApproverEmployeeCode, dottedApprover_Status, dottedApprover_TimeStamp);

                //Make the inputs readonly
                MakeInputsReadOnly5();

            }
            catch (Exception ex)
            {
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

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
        protected void FinalBtnReset_Click(object sender, EventArgs e)
        {
            ScriptManager.RegisterStartupScript(this, GetType(), "activateTab", "activateTab('verificationData-tab');", true);
            Response.Redirect("qaqc_process_rpt.aspx");

        }
        private void MakeInputsReadOnly5()
        {
            //VerifiedData
            RBL_BalanceCondition.Enabled = false;
            TXB_BalanceCondition_Remarks.ReadOnly = true;
            TB_RawBiscuitWgt.ReadOnly = true;


            FinalBtnSubmit.Enabled = false;
            FinalBtnSubmit.Text = "SAVED";
            FinalBtnSubmit.CssClass = "btn btn-sm btn-success";
            FinalBtnReset.Enabled = false;

            string Data_SuccessScript = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Data Success',
                                text: 'Recorded Successfully!!',
                                type: 'success',
                                styling: 'bootstrap3'
                            });
                        </script>";

            // RegisterStartupScript adds the JavaScript code to the page
            ClientScript.RegisterStartupScript(this.GetType(), "ShowDataSuccessNotification", Data_SuccessScript, false);

        }


        private string GenerateUnique()
        {

            string newPcrValue = string.Empty;
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Fetch the maximum PCR01 value
                    string query = "SELECT ISNULL(MAX(CAST(SUBSTRING(PcrNo, 4, LEN(PcrNo)) AS INT)), 0) FROM TRN_ProcessChecking_BasicData";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        object result = command.ExecuteScalar();
                        int maxPCRValue = Convert.ToInt32(result);

                        // Increment the numeric part
                        int numericPart = maxPCRValue + 1;

                        // Format the new value
                        newPcrValue = $"PCR{numericPart:D3}"; // Ensure three digits (e.g., PCR001, PCR002)
                    }
                }
            }
            catch (Exception ex)
            {
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                // Handle exceptions
                //Console.WriteLine("Error generating PCR01: " + ex.Message);
                //throw;
            }

            PcrNo = newPcrValue;
            return newPcrValue;
        }
        private string GetCurrentShift()
        {
            TimeSpan currentTime = DateTime.Now.TimeOfDay;

            // Define shift times
            TimeSpan shiftAStart = new TimeSpan(6, 0, 0);  // 06:00 AM
            TimeSpan shiftAEnd = new TimeSpan(14, 0, 0);   // 02:00 PM
            TimeSpan shiftBStart = new TimeSpan(14, 0, 0); // 02:00 PM
            TimeSpan shiftBEnd = new TimeSpan(22, 0, 0);   // 10:00 PM
            TimeSpan shiftCStart = new TimeSpan(22, 0, 0); // 10:00 PM
            TimeSpan shiftCEnd = new TimeSpan(6, 0, 0);    // 06:00 AM (next day)

            // Determine the current shift based on time
            if (currentTime >= shiftAStart && currentTime < shiftAEnd)
            {
                return "A";  // Morning Shift
            }
            else if (currentTime >= shiftBStart && currentTime < shiftBEnd)
            {
                return "B";  // Afternoon Shift
            }
            else if (currentTime >= shiftCStart || currentTime < shiftCEnd)
            {
                return "C";  // Night Shift
            }

            return "Unknown";
        }

        private decimal? TryParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // Return null if the input is null, empty, or consists only of whitespace
                return null;
            }

            decimal result;
            return decimal.TryParse(value, out result) ? (decimal?)result : null;
        }
        private int? TryParseInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // Return null if the input is null, empty, or consists only of whitespace
                return null;
            }

            int result;
            return int.TryParse(value, out result) ? (int?)result : null;
        }

        //--------- Weight section data ---------
        private void BindGridView()             // to show all 22 varieties and standard weight 
        {
            // Sample data for Variety and Standard Weight
            var dataSave = new List<VarietyInfo>
            {
                // Add all 22 varieties here
                new VarietyInfo {Sl = 1,   sap_IngredientName = "Maida", BOM_Qnty = 500 } ,
                new VarietyInfo {Sl = 2,   sap_IngredientName = "Sugar", BOM_Qnty = 500 } ,
                new VarietyInfo {Sl = 3,   sap_IngredientName = "Butter", BOM_Qnty = 500  },
                new VarietyInfo {Sl = 4,   sap_IngredientName = "S.M.P", BOM_Qnty = 500  },
                new VarietyInfo {Sl = 5,   sap_IngredientName = "Process Water", BOM_Qnty = 500  },
                new VarietyInfo {Sl = 6,   sap_IngredientName = "Lecithin", BOM_Qnty = 500  },
                new VarietyInfo {Sl = 7,   sap_IngredientName = "GMS Paste", BOM_Qnty = 500  },
                new VarietyInfo {Sl = 8,   sap_IngredientName = "SSL paste/ LS. Powder", BOM_Qnty = 500  },
                new VarietyInfo {Sl = 9,   sap_IngredientName = "Glucose", BOM_Qnty = 500 },
                new VarietyInfo {Sl = 10,  sap_IngredientName = "H.V.O", BOM_Qnty = 500  },
                new VarietyInfo {Sl = 11,  sap_IngredientName = "Syrup", BOM_Qnty = 500  },
                new VarietyInfo {Sl = 12,  sap_IngredientName = "Malt", BOM_Qnty = 500  },
                new VarietyInfo {Sl = 13,  sap_IngredientName = "Broken Biscuit", BOM_Qnty = 500  },
                new VarietyInfo {Sl = 14,  sap_IngredientName = "A.B.C.", BOM_Qnty = 500  },
                new VarietyInfo {Sl = 15,  sap_IngredientName = "S.B.C.", BOM_Qnty = 500  },
                new VarietyInfo {Sl = 16,  sap_IngredientName = "S.M.B.S.", BOM_Qnty = 500  },
                new VarietyInfo {Sl = 17,  sap_IngredientName = "Whey Powder", BOM_Qnty = 500  },
                new VarietyInfo {Sl = 18,  sap_IngredientName = "Condence Milk", BOM_Qnty = 500  },
                new VarietyInfo {Sl = 19,  sap_IngredientName = "Salt", BOM_Qnty = 500 } ,
                new VarietyInfo {Sl = 20,  sap_IngredientName = "Yeast (Smell & Wt.)", BOM_Qnty = 500  },
                new VarietyInfo {Sl = 21,  sap_IngredientName = "E1", BOM_Qnty = 500 } ,
                new VarietyInfo {Sl = 22,  sap_IngredientName = "Caramel", BOM_Qnty = 500 } ,

            };

            // Add 5 blank rows
            for (int i = 0; i < 8; i++)
            {
                dataSave.Add(new VarietyInfo { Sl = dataSave.Count + 1, sap_IngredientName = "", BOM_Qnty = 0 });
            }
            // Store in Session for postback persistence
            Session["VarietyData"] = dataSave;

            // Bind to GridView
            GridView1.DataSource = dataSave;
            GridView1.DataBind();
        }

        protected void GridView1_RowEditing(object sender, GridViewEditEventArgs e)
        {
            GridView1.EditIndex = e.NewEditIndex;
            BindGridView(); // Rebind the data to refresh the GridView
        }

        protected void GridView1_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            // Retrieve the original data source (e.g., from ViewState or Session)
            List<VarietyInfo> data = (List<VarietyInfo>)Session["VarietyData"];
            if (data == null)
            {
                // Handle the case where data is missing
                return;
            }

            // Get the GridView row
            int index = e.RowIndex;
            GridViewRow row = GridView1.Rows[index];

            // Retrieve values from input fields
            string variety = ((TextBox)row.FindControl("txtVariety")).Text;
            string standardWeight = ((TextBox)row.FindControl("txtStandardWeightEdit")).Text;

            // Update the list
            data[index].sap_IngredientName = variety;
            data[index].BOM_Qnty = int.Parse(standardWeight);

            // Save updated data back to Session
            Session["VarietyData"] = data;

            // Reset edit index
            GridView1.EditIndex = -1;

            // Rebind the updated list
            GridView1.DataSource = data;
            GridView1.DataBind();
        }

        protected void GridView1_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            GridView1.EditIndex = -1;
            BindGridView(); // Rebind to show the original data
        }

        private void BindGridViewfromDB(string Plant_Id, string Brand_Id)
        {
            // Connection string to your database (update with your actual connection details)
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            // Query to fetch data from the database
            string query = "SELECT ROW_NUMBER() OVER (ORDER BY ComponentDescription) AS Sl, ComponentDescription as sap_IngredientName, BOMQuantity as BOM_Qnty FROM PlantMaterialData where PlantID=@plant_id and MaterialID=@brand_id and IsActive=1 order by Sl";

            // Create a DataTable to hold the data
            DataTable dataTable = new DataTable();

            try
            {
                // Using block ensures connection is properly disposed of
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    // Create a SqlCommand to execute the query
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Add parameters to the command to prevent SQL injection
                        command.Parameters.AddWithValue("@plant_id", Plant_Id);
                        command.Parameters.AddWithValue("@brand_id", Brand_Id);

                        // Open the connection
                        connection.Open();

                        // Execute the query and load the data into the DataTable
                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }
                    }
                }

                //// Check if DataTable contains any rows
                //if (dataTable.Rows.Count > 0)
                //{
                //    // Bind the DataTable to the GridView
                //    GridView1.DataSource = dataTable;
                //    GridView1.DataBind();
                //}
                //else
                //{
                //    // Call another function if no records are found
                //    BindGridView();
                //}

                BindGridView();
            }
            catch (Exception ex)
            {
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                // Handle exceptions (e.g., log the error or display a message)
                Console.WriteLine("Error: " + ex.Message);
            }
        }


        public class VarietyInfo
        {
            public int Sl { get; set; }
            public string sap_IngredientName { get; set; }
            public decimal BOM_Qnty { get; set; }
            public decimal ActualWeight { get; set; }
            public decimal DeviationWeight { get; set; }
            public decimal DeviationPercentage { get; set; }
        }


        [WebMethod]
        public static void SaveData(string jsonData)
        {

            // Deserialize JSON data to a list of objects
            var data = JsonConvert.DeserializeObject<List<VarietyInfo>>(jsonData);

            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    string query = "UPDATE TRN_ProcessChecking_BasicData SET RM_Weights=@RM_Weights, RM_Status=@RM_Status where PcrNo=@PcrNo";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PcrNo ", PcrNo);
                        cmd.Parameters.AddWithValue("@RM_Weights", jsonData);
                        cmd.Parameters.AddWithValue("@RM_Status", 1);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);
            }
        }
        protected void WgtbtnSubmit_Click(object sender, EventArgs e)
        {
            WgtbtnSubmit.Enabled = false;
            WgtbtnSubmit.Text = "SAVED";
            WgtbtnSubmit.CssClass = "btn btn-sm btn-success";
            WgtbtnReset.Enabled = false;

            SpongeBtnSubmit.Enabled = true;
            SpongeBtnSubmit.Visible = true;

            string Data_SuccessScript = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Data Success',
                                text: 'Recorded Successfully!!',
                                type: 'success',
                                styling: 'bootstrap3'
                            });
                        </script>";

            //// RegisterStartupScript adds the JavaScript code to the page
            ClientScript.RegisterStartupScript(this.GetType(), "ShowDataSuccessNotification", Data_SuccessScript, false);
            ScriptManager.RegisterStartupScript(this, this.GetType(), "SwitchTab", "document.getElementById('spongeData-tab').click();", true);



        }
        protected void WgtbtnReset_Click(object sender, EventArgs e)
        {

            ScriptManager.RegisterStartupScript(this, GetType(), "activateTab", "activateTab('weight-tab');", true);
            Response.Redirect("qaqc_process_rpt.aspx");
        }


        //------- Oven section data ---------
        private void BindGridView1()             // to show all  6 zone
        {
            // Sample data for Variety and Standard Weight
            var dataSave1 = new List<ZoneInfo>
            {
                // Add all 22 varieties here
                new ZoneInfo {Sl = 1,   Zone = "Zone 1" },
                new ZoneInfo {Sl = 2,   Zone = "Zone 2" },
                new ZoneInfo {Sl = 3,   Zone = "Zone 3" },
                new ZoneInfo {Sl = 4,   Zone = "Zone 4" },
                new ZoneInfo {Sl = 5,   Zone = "Zone 5" },
                new ZoneInfo {Sl = 6,   Zone = "Zone 6" },
                new ZoneInfo {Sl = 7,   Zone = "Zone 7" },
                new ZoneInfo {Sl = 8,   Zone = "Zone 8" },
            };

            // Bind to GridView
            GridView2.DataSource = dataSave1;
            GridView2.DataBind();
        }
        public class ZoneInfo
        {
            public int Sl { get; set; }
            public string Zone { get; set; }
            public decimal OvenTop { get; set; }
            public decimal OvenBottom { get; set; }
            public decimal DamperTop { get; set; }
            public decimal DamperBottom { get; set; }
        }


        [WebMethod]
        public static void SaveData1(string jsonData )
        {

            // Deserialize JSON data to a list of objects
            var data = JsonConvert.DeserializeObject<List<ZoneInfo>>(jsonData);

            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    string query = "UPDATE TRN_ProcessChecking_BasicData SET Oven_Temperatures=@Oven_Temperatures, OvenData_Status=@OvenData_Status where PcrNo=@PcrNo";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PcrNo ", PcrNo);
                        cmd.Parameters.AddWithValue("@Oven_Temperatures", jsonData);
                        cmd.Parameters.AddWithValue("@OvenData_Status", 1);
                        cmd.ExecuteNonQuery();
                    }

                }
            }
            catch (Exception ex)
            {
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);
                //throw;
            }
        }
        protected void OvenBtnSubmit_Click(object sender, EventArgs e)
        {
            OvenBtnSubmit.Enabled = false;
            OvenBtnSubmit.Text = "SAVED";
            OvenBtnSubmit.CssClass = "btn btn-sm btn-success";
            OvenBtnReset.Enabled = false;


            FinalBtnSubmit.Enabled = true;
            FinalBtnSubmit.Visible = true;

            string Data_SuccessScript = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Data Success',
                                text: 'Recorded Successfully!!',
                                type: 'success',
                                styling: 'bootstrap3'
                            });
                        </script>";

            //// RegisterStartupScript adds the JavaScript code to the page
            ClientScript.RegisterStartupScript(this.GetType(), "ShowDataSuccessNotification", Data_SuccessScript, false);
            ScriptManager.RegisterStartupScript(this, this.GetType(), "SwitchTab", "document.getElementById('verificationData-tab').click();", true);

            

        }
        protected void OvenBtnReset_Click(object sender, EventArgs e)
        {
            ScriptManager.RegisterStartupScript(this, GetType(), "activateTab", "activateTab('ovenData-tab');", true);
            Response.Redirect("qaqc_process_rpt.aspx");
        }

        protected void SpongeBtnNotApplicable_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PcrNo))
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "activateTab", "activateTab('basicData-tab');", true);
            }
            else
            {
                QAProcessCheckingDataAcess dataAccess = new QAProcessCheckingDataAcess();
                dataAccess.InsertSpongeData(PcrNo, null, null, null, null, null, null, null, null, null, null, "No");

                DoughBtnSubmit.Enabled = true;
                DoughBtnSubmit.Visible = true;

                //Make the inputs readonly
                MakeInputsReadOnly2();

                ScriptManager.RegisterStartupScript(this, GetType(), "activateTab", "activateTab('doughData-tab');", true);

                
            }
        }
    }

}
