using System;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.IO;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Globalization;

namespace AnmolDristi
{
    public partial class Aata_Maida_Form : System.Web.UI.Page
    {
        DB_Utility_OH4Y dbcl = new DB_Utility_OH4Y();
        public static string ImgLink1 = string.Empty;
        public static string AMfId = string.Empty;
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
                    hdn_formid.Value = "20";

                    lbl_docname.Text = "QC - Atta/Refined Wheat Flour Report";
                    lbl_docnumber.Text = "ANMOL/DOC/CORP/QC/01-02";
                    MaterialBinder();

                    // Call a helper method to make all controls visible
                    //SetControlsVisible(Page.Controls, true);
                }
            }
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
                LoadApprovers(selectedPlantValue, "");
                string selectedMaterialValue = DDL_Material.SelectedValue.ToString();
                DataTable dataTable = DatabaseHelper.GetBrandFieldsControlByMaterialIdAndPlantId(Convert.ToInt16(selectedMaterialValue), Convert.ToInt16(selectedPlantValue));

                //DataRow[] rows = dataTable.Select($"brand_id = {selectedProductBrandValue} AND field_name = '{fieldName}'");

                // Iterate through the filtered rows and extract validation criteria
                foreach (DataRow row in dataTable.Rows)
                {
                    // Extract field name from the current row
                    string fieldName = row["field_name"].ToString();

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
            else
            {
                //DatabaseHelper.BindWithDefaultNoRecords(DDL_PlantLine);

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

        //private void PlantLinesBinder(string selectedPlantValue)
        //{
        //    string query = "SELECT line_id, line_name FROM MST_Plant_Lines WHERE plant_id = @SelectedPlantValue";
        //    string textField = "line_name";
        //    string valueField = "line_id";

        //    bool recordsBound;
        //    DatabaseHelper.BindDropDownList(query, DDL_PlantLine, textField, valueField, new SqlParameter("@SelectedPlantValue", selectedPlantValue), out recordsBound);

        //    if (!recordsBound)
        //    {
        //        DatabaseHelper.BindWithDefaultNoRecords(DDL_PlantLine);

        //        string PlantLinesBinder_Error_script = @"<script type='text/javascript'>
        //                    new PNotify({
        //                        title: 'Error',
        //                        text: 'An error occurred!',
        //                        type: 'error',
        //                        styling: 'bootstrap3'
        //                    });
        //                </script>";
        //        ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantLinesBinderErrorNotification", PlantLinesBinder_Error_script, false);
        //    }
        //}

        //protected void DDL_PlantLine_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    if (DDL_PlantLine.SelectedIndex != 0)
        //    {
        //        string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
        //        string selectedPlantLineValue = DDL_PlantLine.SelectedValue.ToString();
        //        LoadApprovers(selectedPlantValue, selectedPlantLineValue);
        //        ProductBrandsBinder(selectedPlantValue, selectedPlantLineValue);
        //    }
        //    else
        //    {
        //        DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductBrand);

        //        string DDL_PlantLine_Error_script = @"<script type='text/javascript'>
        //                    new PNotify({
        //                        title: 'Error',
        //                        text: 'Invalid Selection!',
        //                        type: 'error',
        //                        styling: 'bootstrap3'
        //                    });
        //                </script>";
        //        ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantInvalidErrorNotification", DDL_PlantLine_Error_script, false);
        //    }

        //}

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
                    cmd.Parameters.AddWithValue("@FormID", 20);
                    cmd.Parameters.AddWithValue("@FormName", "RM_Class_6");

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        hdn_formid.Value = "20";
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
                            bool isInserted = dbcl.InsertDefaultApprovers(selectedPlantValue, selectedPlantLineValue, 20);

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

        //private void ProductBrandsBinder(string selectedPlantValue, string selectedPlantLineValue)
        //{
        //    // Construct the SQL query with parameters
        //    string query = "SELECT brand_id, brand_name FROM MST_LineCatBrands WHERE plant_id = @PlantId AND line_id = @LineId ";
        //    string textField = "brand_name"; // Assuming this is the correct field for displaying in the DropDownList
        //    string valueField = "brand_id"; // Assuming this is the correct field for storing in the DropDownList

        //    // Create SQL parameters for plant_id and line_id
        //    SqlParameter[] parameters = new SqlParameter[]
        //    {
        //        new SqlParameter("@PlantId", selectedPlantValue),
        //        new SqlParameter("@LineId", selectedPlantLineValue)
        //    };

        //    // Call the BindDropDownList method with parameters
        //    bool recordsBound;
        //    DatabaseHelper.BindDropDownList(query, DDL_ProductBrand, textField, valueField, parameters, out recordsBound);

        //    // Check if any records were bound
        //    if (!recordsBound)
        //    {
        //        string ProductBrands_Error_script = @"<script type='text/javascript'>
        //            new PNotify({
        //                title: 'Error',
        //                text: 'No Brands found for the selected plant and line!',
        //                type: 'error',
        //                styling: 'bootstrap3'
        //            });
        //        </script>";

        //        // RegisterStartupScript adds the JavaScript code to the page
        //        ClientScript.RegisterStartupScript(this.GetType(), "ShowProductBrandsBinderErrorNotification", ProductBrands_Error_script, false);
        //    }
        //}

        //protected void DDL_ProductBrand_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    if (DDL_Material.SelectedIndex != 0 && DDL_ProductBrand.SelectedIndex != 0)
        //    {
        //        string selectedMaterialValue = DDL_Material.SelectedValue.ToString();
        //        string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
        //        string selectedProductBrandValue = DDL_ProductBrand.SelectedValue.ToString();
        //        DataTable dataTable = DatabaseHelper.GetBrandFieldsControlByMaterialIdAndBrandId(Convert.ToInt16(selectedMaterialValue),Convert.ToInt16(selectedProductBrandValue));


        //        // Example: Querying the DataTable for a specific field name
        //        //string fieldName = "no_of_pcs"; // Specify the field name you want to query
        //        //DataRow[] rows = dataTable.Select($"brand_id = {selectedProductBrandValue} AND field_name = '{fieldName}'");

        //        // Iterate through the filtered rows and extract validation criteria
        //        foreach (DataRow row in dataTable.Rows)
        //        {
        //            // Extract field name from the current row
        //            string fieldName = row["field_name"].ToString();

        //            // Extract validation criteria from the DataRow
        //            bool isVisible = Convert.ToBoolean(row["ViewMode"]);
        //            bool rfvEnabled = Convert.ToBoolean(row["RFV_YesNo"]);
        //            string rfvErrorMessage = row["RFV_ErrorMsg"].ToString();
        //            bool revEnabled = Convert.ToBoolean(row["REV_YesNo"]);
        //            string revErrorMessage = row["REV_ErrorMsg"].ToString();
        //            string revExpression = row["REV_Expression"].ToString();
        //            bool rvEnabled = Convert.ToBoolean(row["RV_Yesno"]);
        //            string rvErrorMessage = row["RV_ErrorMsg"].ToString();
        //            string rvMinValue = row["RV_MinValue"].ToString();
        //            string rvMaxValue = row["RV_MaxValue"].ToString();

        //            // Create a new instance of ValidationCriteria and populate it with data from the DataRow
        //            ValidationCriteria criteria = new ValidationCriteria();
        //            criteria.IsVisible = isVisible;
        //            criteria.RequiredFieldErrorMessage = rfvErrorMessage;
        //            criteria.IsRequired = rfvEnabled;
        //            criteria.RegularExpressionErrorMessage = revErrorMessage;
        //            criteria.IsRegularExpressionRequired = revEnabled;
        //            criteria.RegularExpression = revExpression;
        //            criteria.RangeErrorMessage = rvErrorMessage;
        //            criteria.IsRangeRequired = rvEnabled;
        //            criteria.MinimumValue = rvMinValue;
        //            criteria.MaximumValue = rvMaxValue;

        //            // Use the criteria as needed
        //            // For example, you can pass it to a method to set up validators
        //            SetUpValidatorsForField(fieldName, criteria);
        //        }
        //    }
        //    else
        //    {
        //        DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductBrand);

        //        string DDL_ProductBrand_Error_script = @"<script type='text/javascript'>
        //                    new PNotify({
        //                        title: 'Error',
        //                        text: 'Invalid Selection!',
        //                        type: 'error',
        //                        styling: 'bootstrap3'
        //                    });
        //                </script>";
        //        ClientScript.RegisterStartupScript(this.GetType(), "ShowSKUInvalidErrorNotification", DDL_ProductBrand_Error_script, false);
        //    }
        //}

        private bool UploadImage1()
        {
            bool imgSaved = false;

            try
            {
                string TBPhotoId = "QAPC";
                DateTime now = DateTime.Now;

                // Define the target folder path
                string targetFolderPath = Server.MapPath("~/UploadedFiles/QAPC/MaterialImage/");

                // Check if the target folder exists, if not, create it
                if (!Directory.Exists(targetFolderPath))
                {
                    Directory.CreateDirectory(targetFolderPath);
                }

                // Check if a file is posted
                if (FU_MaterialImage.PostedFile != null)
                {
                    // Check the extension of the image
                    string extension = Path.GetExtension(FU_MaterialImage.FileName);
                    if (extension.ToLower() == ".png" || extension.ToLower() == ".jpg" || extension.ToLower() == ".jpeg")
                    {
                        // Get the uploaded image stream
                        Stream strm = FU_MaterialImage.PostedFile.InputStream;
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
                                ImgLink1 = "~/UploadedFiles/QAPC/MaterialImage/" + fileName;
                                //imgfilename = fileName;

                                // Show the image instantly
                                FU_MaterialImage_img.Visible = true;
                                uploadedImage1.ImageUrl = ImgLink1;

                                // Image saved successfully
                                imgSaved = true;

                                FU_MaterialImage_Upldr.Visible = false;
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
                throw ex;

            }

            return imgSaved;
        }
        protected void BtnUploadFU_MaterialImage_Click(object sender, EventArgs e)
        {
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

        private string GenerateUnique()
        {

            string newAMfValue;
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Fetch the maximum AMF01 value
                    string query = "SELECT ISNULL(MAX(CAST(SUBSTRING(AMFID, 4, LEN(AMFID)) AS INT)), 0) FROM TRN_Aata_Maida";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        object result = command.ExecuteScalar();
                        int maxAMFValue = Convert.ToInt32(result);

                        // Increment the numeric part
                        int numericPart = maxAMFValue + 1;

                        // Format the new value
                        newAMfValue = $"AMF{numericPart:D3}"; // Ensure three digits (e.g., AMF001, AMF002)
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                Console.WriteLine("Error generating AMF01: " + ex.Message);
                throw;
            }

            AMfId = newAMfValue;
            return newAMfValue;
        }

        protected void BtnSubmit_Click(object sender, EventArgs e)
        {
            //Console.Write("Button clicked");
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            string amfId = GenerateUnique();  // Generate unique value
            int formID = Convert.ToInt32(hdn_formid.Value.ToString());

            string materialName = DDL_Material.SelectedValue;
            string plantName = DDL_Plant.SelectedValue;
            //string line = DDL_PlantLine.SelectedValue;

            //string productBrand = DDL_ProductBrand.SelectedValue;
            string productBrand = TB_BrandName.Text.ToString();

            decimal? quantity = !string.IsNullOrWhiteSpace(TB_Quantity.Text) ? Convert.ToDecimal(TB_Quantity.Text) : (decimal?)null;
            string supplier = string.IsNullOrEmpty(TB_Supplier.Text) ? null : TB_Supplier.Text;

            decimal? size = !string.IsNullOrWhiteSpace(TB_Size.Text) ? Convert.ToDecimal(TB_Size.Text) : (decimal?)null;
            //string sizeRemarks = string.IsNullOrEmpty(TXB_Size_Remarks.Text) ? null : TXB_Size_Remarks.Text;

            string challanNo = string.IsNullOrEmpty(TB_ChallanNo.Text) ? null : TB_ChallanNo.Text;
            //DateTime? challanDate = string.IsNullOrEmpty(TB_ChallanDate.Text) ? (DateTime?)null : DateTime.Parse(TB_ChallanDate.Text).Date;

            List<DateTime?> challanDates = TB_ChallanDate.Text
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(dateStr =>
            {
                DateTime parsedDate;
                return DateTime.TryParseExact(dateStr.Trim(), "dd-MM-yyyy",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate)
                    ? (DateTime?)parsedDate
                    : null;
            })
            .ToList();

            //DateTime? mfgDate = string.IsNullOrEmpty(TB_Mfg.Text) ? (DateTime?)null : DateTime.Parse(TB_Mfg.Text).Date;

            List<DateTime?> mfgDates = string.IsNullOrEmpty(TB_Mfg.Text)
            ? new List<DateTime?>() // Return an empty list if the textbox is empty
            : TB_Mfg.Text
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries) // Split by commas
                .Select(dateStr =>
                {
                    DateTime parsedDate;
                    return DateTime.TryParseExact(dateStr.Trim(), "dd-MM-yyyy",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate)
                        ? (DateTime?)parsedDate
                        : null;
                })
                .ToList();

            string batchNo = string.IsNullOrEmpty(TB_BatchNo.Text) ? null : TB_BatchNo.Text;
            string lotNo = string.IsNullOrEmpty(TB_LotNo.Text) ? null : TB_LotNo.Text;
            string vehicleNo = string.IsNullOrEmpty(TB_VehicleNo.Text) ? null : TB_VehicleNo.Text;

            int? mfgNameyesno = string.IsNullOrEmpty(RBL_ManufNameAdd.SelectedValue) ? (int?)null : (int?)Convert.ToInt32(RBL_ManufNameAdd.SelectedValue);
            string mfgNameyesnoRemarks = string.IsNullOrEmpty(TXB_ManufNameAdd_Remarks.Text) ? null : TXB_ManufNameAdd_Remarks.Text;
            string mfgName = string.IsNullOrEmpty(TB_MfgName.Text) ? null : TB_MfgName.Text;

            int? bbdateyesno = string.IsNullOrEmpty(RBL_BBDateYesNo.SelectedValue) ? (int?)null : (int?)Convert.ToInt32(RBL_BBDateYesNo.SelectedValue);
            string bbdateyesnoRemarks = string.IsNullOrEmpty(TXB_BBDateYesNoRemarks.Text) ? null : TXB_BBDateYesNoRemarks.Text;
            DateTime? bbDate = string.IsNullOrEmpty(TB_BeforeDate.Text) ? (DateTime?)null : DateTime.Parse(TB_BeforeDate.Text).Date;

            int? fssaiyesno = string.IsNullOrEmpty(RBL_FassaiNoLogo.SelectedValue) ? (int?)null : (int?)Convert.ToInt32(RBL_FassaiNoLogo.SelectedValue);
            string fssaiyesnoRemarks = string.IsNullOrEmpty(TXB_FassaiNoLogo_Remarks.Text) ? null : TXB_FassaiNoLogo_Remarks.Text;
            string licenceNo = string.IsNullOrEmpty(TB_FssaiNo.Text) ? null : TB_FssaiNo.Text;

            int? logo = string.IsNullOrEmpty(RBL_Fssai_Logo.SelectedValue) ? (int?)null : (int?)Convert.ToInt32(RBL_Fssai_Logo.SelectedValue);
            int? vegLogo = string.IsNullOrEmpty(RBL_Veg_Logo.SelectedValue) ? (int?)null : (int?)Convert.ToInt32(RBL_Veg_Logo.SelectedValue);

            int? packing = string.IsNullOrEmpty(RBL_Packing_Condition.SelectedValue) ? (int?)null : (int?)Convert.ToInt32(RBL_Packing_Condition.SelectedValue);
            string packingRemarks = string.IsNullOrEmpty(TXB_PackingCondition_Remarks.Text) ? null : TXB_PackingCondition_Remarks.Text;

            int? color = string.IsNullOrEmpty(RBL_ColorApp.SelectedValue) ? (int?)null : (int?)Convert.ToInt32(RBL_ColorApp.SelectedValue);
            string colorRemarks = string.IsNullOrEmpty(TXB_ColorApp_Remarks.Text) ? null : TXB_ColorApp_Remarks.Text;

            int? odour = string.IsNullOrEmpty(RBL_Odour.SelectedValue) ? (int?)null : (int?)Convert.ToInt32(RBL_Odour.SelectedValue);
            string odourRemarks = string.IsNullOrEmpty(TXB_Odour_Remarks.Text) ? null : TXB_Odour_Remarks.Text;

            int? taste = string.IsNullOrEmpty(RBL_TasteFlavor.SelectedValue) ? (int?)null : (int?)Convert.ToInt32(RBL_TasteFlavor.SelectedValue);
            string tasteRemarks = string.IsNullOrEmpty(TXB_TasteFlavor_Remarks.Text) ? null : TXB_TasteFlavor_Remarks.Text;

            int? impurities = string.IsNullOrEmpty(RBL_Impurities.SelectedValue) ? (int?)null : (int?)Convert.ToInt32(RBL_Impurities.SelectedValue);
            string impuritiesRemarks = string.IsNullOrEmpty(TXB_Impurities_Remarks.Text) ? null : TXB_Impurities_Remarks.Text;

            decimal? moisture = !string.IsNullOrEmpty(TB_Moisture.Text) ? Convert.ToDecimal(TB_Moisture.Text) : (decimal?)null;
            string moistureRemarks = string.IsNullOrEmpty(TXB_Moisture_Remarks.Text) ? null : TXB_Moisture_Remarks.Text;

            decimal? ash = !string.IsNullOrEmpty(TB_TotalAsh.Text) ? Convert.ToDecimal(TB_TotalAsh.Text) : (decimal?)null;
            string ashRemarks = string.IsNullOrEmpty(TXB_Ash_Remarks.Text) ? null : TXB_Ash_Remarks.Text;

            decimal? insolubleAsh = !string.IsNullOrEmpty(TB_InsolubleAsh.Text) ? Convert.ToDecimal(TB_InsolubleAsh.Text) : (decimal?)null;
            string insolubleAshRemarks = string.IsNullOrEmpty(TXB_InsolubleAsh_Remarks.Text) ? null : TXB_InsolubleAsh_Remarks.Text;

            decimal? glutentContent = !string.IsNullOrEmpty(TB_GlutentContent.Text) ? Convert.ToDecimal(TB_GlutentContent.Text) : (decimal?)null;
            string glutentContentRemarks = string.IsNullOrEmpty(TXB_GlutentContent_Remarks.Text) ? null : TXB_GlutentContent_Remarks.Text;

            decimal? alcoholicAcidity = !string.IsNullOrEmpty(TB_AlcoholicAcidity.Text) ? Convert.ToDecimal(TB_AlcoholicAcidity.Text) : (decimal?)null;
            string alcoholicAcidityRemarks = string.IsNullOrEmpty(TXB_AlcoholicAcidity_Remarks.Text) ? null : TXB_AlcoholicAcidity_Remarks.Text;

            decimal? absorption = !string.IsNullOrEmpty(TB_Absorption.Text) ? Convert.ToDecimal(TB_Absorption.Text) : (decimal?)null;
            string absorptionRemarks = string.IsNullOrEmpty(TXB_Absorption_Remarks.Text) ? null : TXB_Absorption_Remarks.Text;

            decimal? sediment = !string.IsNullOrEmpty(TB_Sedimentation.Text) ? Convert.ToDecimal(TB_Sedimentation.Text) : (decimal?)null;
            string sedimentRemarks = string.IsNullOrEmpty(TXB_Sedimentation_Remarks.Text) ? null : TXB_Sedimentation_Remarks.Text;

            //int? sediment = string.IsNullOrEmpty(TB_Sedimentation.Text) ? (int?)null : Convert.ToInt32(TB_Sedimentation.Text);
            //string sedimentRemarks = string.IsNullOrEmpty(TXB_Sedimentation_Remarks.Text) ? null : TXB_Sedimentation_Remarks.Text;

            int? grittiness = string.IsNullOrEmpty(RBL_Grittiness.SelectedValue) ? (int?)null : (int?)Convert.ToInt32(RBL_Grittiness.SelectedValue);
            string grittinessRemarks = string.IsNullOrEmpty(TXB_Grittiness_Remarks.Text) ? null : TXB_Grittiness_Remarks.Text;

            decimal? germ = !string.IsNullOrEmpty(TB_Acidity.Text) ? Convert.ToDecimal(TB_Acidity.Text) : (decimal?)null;
            string germRemarks = string.IsNullOrEmpty(TXB_Acidity_Remarks.Text) ? null : TXB_Acidity_Remarks.Text;

            decimal? granularity = !string.IsNullOrEmpty(TB_Granularity.Text) ? Convert.ToDecimal(TB_Granularity.Text) : (decimal?)null;
            string granularityRemarks = string.IsNullOrEmpty(TXB_Granularity_Remarks.Text) ? null : TXB_Granularity_Remarks.Text;

            decimal? granularityRetention = !string.IsNullOrEmpty(TB_GranularityRetention.Text) ? Convert.ToDecimal(TB_GranularityRetention.Text) : (decimal?)null;
            string granularityRetentionRemarks = string.IsNullOrEmpty(TXB_GranularityRetention_Remarks.Text) ? null : TXB_GranularityRetention_Remarks.Text;

            decimal? retention = !string.IsNullOrEmpty(TB_Retention.Text) ? Convert.ToDecimal(TB_Retention.Text) : (decimal?)null;
            string retentionRemarks = string.IsNullOrEmpty(TXB_Retention_Remarks.Text) ? null : TXB_Retention_Remarks.Text;

            string bromate = string.IsNullOrEmpty(TB_Bromate.Text) ? null : TB_Bromate.Text;

            int submitstatus = 0;
            if (!string.IsNullOrEmpty(ash?.ToString()) && !string.IsNullOrEmpty(insolubleAsh?.ToString()) && !string.IsNullOrEmpty(glutentContent?.ToString()) && !string.IsNullOrEmpty(alcoholicAcidity?.ToString()))
            {
                submitstatus = 1;
            }


            DateTime submittedDate = DateTime.Now.Date;
            TimeSpan submittedTime = DateTime.Now.TimeOfDay;
            int submittedById = Convert.ToInt32(Session["USERID"].ToString());
            string submittedByEmployeeCode = Session["WORKMAN"].ToString();

            string approver1EmployeeCode = Approver1CodeLabel.Text.ToString();
            string approver2EmployeeCode = Approver1CodeLabel.Text.ToString();
            string dottedLineApproverEmployeeCode = DottedLineApproverCodeLabel.Text.ToString();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("SP_Aata_Maida", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Add parameters
                        command.Parameters.AddWithValue("@AMFID", amfId);
                        command.Parameters.AddWithValue("@FormID", formID);


                        command.Parameters.AddWithValue("@MaterialName", materialName);
                        command.Parameters.AddWithValue("@PlantName", plantName);
                        //command.Parameters.AddWithValue("@Line", line);
                        command.Parameters.AddWithValue("@ProductBrand", productBrand);

                        command.Parameters.AddWithValue("@Quantity", (object)quantity ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Supplier", (object)supplier ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Size", (object)size ?? DBNull.Value);
                        //command.Parameters.AddWithValue("@Size_Remarks", (object)sizeRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@ChallanNo", (object)challanNo ?? DBNull.Value);
                        //command.Parameters.AddWithValue("@ChallanDate", (object)challanDate ?? DBNull.Value);
                        //command.Parameters.AddWithValue("@Mfg", (object)mfgDate ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Challan_Dates", challanDates.Any() ? string.Join(",", challanDates.Where(d => d.HasValue).Select(d => d.Value.ToString("yyyy-MM-dd"))) : (object)DBNull.Value);

                        command.Parameters.AddWithValue("@Mfg_Dates", mfgDates.Any() ? string.Join(",", mfgDates.Where(d => d.HasValue).Select(d => d.Value.ToString("yyyy-MM-dd"))) : (object)DBNull.Value);

                        command.Parameters.AddWithValue("@BatchNo", (object)batchNo ?? DBNull.Value);
                        command.Parameters.AddWithValue("@LotNo", (object)lotNo ?? DBNull.Value);
                        command.Parameters.AddWithValue("@VehicleNo", (object)vehicleNo ?? DBNull.Value);

                        command.Parameters.AddWithValue("@MfgYesNo", (object)mfgNameyesno ?? DBNull.Value);
                        command.Parameters.AddWithValue("@MfgYesNoRemarks", (object)mfgNameyesnoRemarks ?? DBNull.Value);
                        command.Parameters.AddWithValue("@MfgName", (object)mfgName ?? DBNull.Value);

                        command.Parameters.AddWithValue("@BBDateYesNo", (object)bbdateyesno ?? DBNull.Value);
                        command.Parameters.AddWithValue("@BBDateYesNoRemarks", (object)bbdateyesnoRemarks ?? DBNull.Value);
                        command.Parameters.AddWithValue("@BeforeDate", (object)bbDate ?? DBNull.Value);

                        command.Parameters.AddWithValue("@fssaiYesNo", (object)fssaiyesno ?? DBNull.Value);
                        command.Parameters.AddWithValue("@fssaiYesNoRemarks", (object)fssaiyesnoRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@FssaiNo", (object)licenceNo ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Fssai_Logo", (object)logo ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Veg_Logo", (object)vegLogo ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Packing_Condition", (object)packing ?? DBNull.Value);
                        command.Parameters.AddWithValue("@PackingCondition_Remarks", (object)packingRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@ColorApp", (object)color ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ColorApp_Remarks", (object)colorRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Odour", (object)odour ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Odour_Remarks", (object)odourRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@TasteFlavor", (object)taste ?? DBNull.Value);
                        command.Parameters.AddWithValue("@TasteFlavor_Remarks", (object)tasteRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Impurities", (object)impurities ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Impurities_Remarks", (object)impuritiesRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Moisture", (object)moisture ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Moisture_Remarks", (object)moistureRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@TotalAsh", (object)ash ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Ash_Remarks", (object)ashRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@InsolubleAsh", (object)insolubleAsh ?? DBNull.Value);
                        command.Parameters.AddWithValue("@InsolubleAsh_Remarks", (object)insolubleAshRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@GlutentContent", (object)glutentContent ?? DBNull.Value);
                        command.Parameters.AddWithValue("@GlutentContent_Remarks", (object)glutentContentRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@AlcoholicAcidity", (object)alcoholicAcidity ?? DBNull.Value);
                        command.Parameters.AddWithValue("@AlcoholicAcidity_Remarks", (object)alcoholicAcidityRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Absorption", (object)absorption ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Absorption_Remarks", (object)absorptionRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Sedimentation", (object)sediment ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Sedimentation_Remarks", (object)sedimentRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Grittiness", (object)grittiness ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Grittiness_Remarks", (object)grittinessRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Acidity", (object)germ ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Acidity_Remarks", (object)germRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Granularity", (object)granularity ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Granularity_Remarks", (object)granularityRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@GranularityRetention", (object)granularityRetention ?? DBNull.Value);
                        command.Parameters.AddWithValue("@GranularityRetention_Remarks", (object)granularityRetentionRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Retention", (object)retention ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Retention_Remarks", (object)retentionRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Bromate", (object)bromate ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Material_Image", (object)ImgLink1 ?? DBNull.Value);

                        command.Parameters.AddWithValue("@SubmittedDate", submittedDate);
                        command.Parameters.AddWithValue("@SubmittedTime", submittedTime);
                        command.Parameters.AddWithValue("@SubmittedById", submittedById);
                        command.Parameters.AddWithValue("@SubmittedByEmployeeCode", (object)submittedByEmployeeCode ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Approver1EmployeeCode", (object)approver1EmployeeCode ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Approver2EmployeeCode", (object)approver2EmployeeCode ?? DBNull.Value);
                        command.Parameters.AddWithValue("@DottedLineApproverEmployeeCode", (object)dottedLineApproverEmployeeCode ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Submission_Status", submitstatus);
                        // Execute the query
                        command.ExecuteNonQuery();
                        MakeInputsReadOnly();
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

        private void MakeInputsReadOnly()
        {

            DDL_Material.Enabled = false;
            DDL_Plant.Enabled = false;

            //Below is commented on 07-Jan-2024, as Only Plant Data is required for RM Lab Test
            //DDL_PlantLine.Enabled = false;
            //DDL_ProductBrand.Enabled = false;

            TB_BrandName.ReadOnly = true;
            TB_Quantity.ReadOnly = true;
            TB_Supplier.ReadOnly = true;

            TB_Size.ReadOnly = true;
            //TXB_Size_Remarks.ReadOnly = true;

            TB_ChallanNo.ReadOnly = true;
            TB_ChallanDate.ReadOnly = true;

            TB_Mfg.ReadOnly = true;
            TB_BatchNo.ReadOnly = true;
            TB_LotNo.ReadOnly = true;
            TB_VehicleNo.ReadOnly = true;

            TB_MfgName.ReadOnly = true;
            TB_BeforeDate.ReadOnly = true;
            TB_FssaiNo.ReadOnly = true;

            RBL_ManufNameAdd.Enabled = false;
            TXB_ManufNameAdd_Remarks.ReadOnly = true;

            RBL_FassaiNoLogo.Enabled = false;
            TXB_FassaiNoLogo_Remarks.ReadOnly = true;

            RBL_BBDateYesNo.Enabled = false;
            TXB_BBDateYesNoRemarks.ReadOnly = true;

            RBL_Fssai_Logo.Enabled = false;
            RBL_Veg_Logo.Enabled = false;

            RBL_Packing_Condition.Enabled = false;
            TXB_PackingCondition_Remarks.ReadOnly = true;

            RBL_ColorApp.Enabled = false;
            TXB_ColorApp_Remarks.ReadOnly = true;

            RBL_Odour.Enabled = false;
            TXB_Odour_Remarks.ReadOnly = true;

            RBL_TasteFlavor.Enabled = false;
            TXB_TasteFlavor_Remarks.ReadOnly = true;

            RBL_Impurities.Enabled = false;
            TXB_Impurities_Remarks.ReadOnly = true;

            TB_Moisture.ReadOnly = true;
            TXB_Moisture_Remarks.ReadOnly = true;

            TB_TotalAsh.ReadOnly = true;
            TXB_Ash_Remarks.ReadOnly = true;

            TB_InsolubleAsh.ReadOnly = true;
            TXB_InsolubleAsh_Remarks.ReadOnly = true;

            TB_GlutentContent.ReadOnly = true;
            TXB_GlutentContent_Remarks.ReadOnly = true;

            TB_AlcoholicAcidity.ReadOnly = true;
            TXB_AlcoholicAcidity_Remarks.ReadOnly = true;

            TB_Absorption.ReadOnly = true;
            TXB_Absorption_Remarks.ReadOnly = true;

            TB_Sedimentation.ReadOnly = true;
            TXB_Sedimentation_Remarks.ReadOnly = true;

            RBL_Grittiness.Enabled = false;
            TXB_Grittiness_Remarks.ReadOnly = true;

            TB_Acidity.ReadOnly = true;
            TXB_Acidity_Remarks.ReadOnly = true;

            TB_Granularity.ReadOnly = true;
            TXB_Granularity_Remarks.ReadOnly = true;

            TB_GranularityRetention.ReadOnly = true;
            TXB_GranularityRetention_Remarks.ReadOnly = true;

            TB_Retention.ReadOnly = true;
            TXB_Retention_Remarks.ReadOnly = true;

            TB_Bromate.ReadOnly = true;

            BtnSubmit.Enabled = false;
            BtnSubmit.Text = "SAVED";
            BtnSubmit.CssClass = "btn btn-sm btn-success";

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



        protected void BtnReset_Click(object sender, EventArgs e)
        {
            Response.Redirect("Aata_Maida_Form.aspx");
        }

        public class ValidationCriteria
        {
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


        //private void SetControlsVisible(ControlCollection controls, bool visible)
        //{
        //    foreach (System.Web.UI.Control ctrl in controls)
        //    {
        //        // Only hide/show divs that are not related to the Plant dropdown
        //        if (ctrl is HtmlGenericControl && (ctrl as HtmlGenericControl).TagName == "div")
        //        {
        //            // Assuming you have named your divs properly or added IDs to them (e.g., divSupplierSection, divOtherSection, etc.)
        //            if (ctrl.ID != "MaterialDIV" && ctrl.ID != "PlantDIV" && ctrl.ID != "PlantLineDIV" && ctrl.ID != "ProductBrandDIV")
        //            {
        //                ctrl.Visible = visible;
        //            }
        //        }
        //        // Check for remarks TextBoxes
        //        if (ctrl is TextBox && (ctrl.ID.EndsWith("RemarksDiv", StringComparison.OrdinalIgnoreCase)))
        //        {
        //            if (!visible)
        //            {
        //                (ctrl as TextBox).CssClass += " hidden"; // Add 'hidden' class to hide
        //            }
        //            else
        //            {
        //                (ctrl as TextBox).CssClass = (ctrl as TextBox).CssClass.Replace(" hidden", ""); // Remove 'hidden' class
        //            }
        //        }

        //        // Recursively check nested controls for divs
        //        if (ctrl.HasControls())
        //        {
        //            SetControlsVisible(ctrl.Controls, visible);
        //        }
        //    }

        //}


        //private void DivBinders(string selectedMaterialValue)
        //{
        //    string selectedMaterial = DDL_Material.SelectedItem.Text;

        //    // Make all controls visible by default
        //    SetControlsVisible(Page.Controls, true);
        //    FU_MaterialImage_Upldr.Visible = true;
        //    FU_MaterialImage.Visible = true;

        //    // Hide only the controls that are not applicable for the selected material
        //    switch (selectedMaterial)
        //    {
        //        case "Aata":
        //            // Hide controls not applicable to Aata
        //            //Water Absorbtion
        //            AbsorptionDIV.Visible = false;

        //            //Sediment Value
        //            SedimentationDIV.Visible = false;

        //            //Germ oil Acidity
        //            AcidityDIV.Visible = false;

        //            ///Granularity on 70 mesh
        //            GranularityDIV.Visible = false;

        //            //Bromate and Iodate
        //            BromateDIV.Visible = false;
        //            break;

        //        case "Maida":
        //            // Hide controls not applicable to Maida
        //            //Granularity/Retention on 85 mesh
        //            GranularityRetentionDIV.Visible = false;

        //            //Granularity/Retention on 36 mesh
        //            RetentionDIV.Visible = false;
        //            break;

        //        default:
        //            // Optionally handle a default case, e.g., hide all controls for unknown materials
        //            SetControlsVisible(Page.Controls, false);
        //            FU_MaterialImage_Upldr.Visible = false;
        //            FU_MaterialImage.Visible = false;
        //            break;
        //    }
        //}


    }
}