using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using DocumentFormat.OpenXml.Office.Word;

namespace AnmolDristi
{
    public partial class RM_Class_5 : System.Web.UI.Page
    {
        public static string ImgLink1 = string.Empty;
        public static string RmfId = string.Empty;
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


                    lbl_docname.Text = "QC - RM Class 5 Report";
                    lbl_docnumber.Text = "ANMOL/DOC/QC/5";
                    MaterialBinder();
                    PopulateColorDropdown();

                }

            }

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
        protected void DDL_Material_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Material.SelectedIndex != 0)
            {
                string selectedMaterialValue = DDL_Material.SelectedValue.ToString();
                PlantBinder(selectedMaterialValue);
                lbl_DDL_Material_Value.Text = selectedMaterialValue;

                // Call method to show relevant controls based on selected material
                //DivBinders(selectedMaterialValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_Plant);

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
        //protected void DDL_Plant_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    if (DDL_Plant.SelectedIndex != 0)
        //    {
        //        string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
        //        string selectedMaterialValue = DDL_Material.SelectedValue.ToString();
        //        lbl_DDL_Plant_Value.Text = selectedPlantValue;
        //        LoadApprovers(selectedPlantValue);

        //        DataTable dataTable = DatabaseHelper.GetRMFieldsControlByPlantId(Convert.ToInt32(selectedMaterialValue), Convert.ToInt32(selectedPlantValue));

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

        //        string DDL_Plant_Error_script = @"<script type='text/javascript'>
        //                    new PNotify({
        //                        title: 'Error',
        //                        text: 'Invalid Selection!',
        //                        type: 'error',
        //                        styling: 'bootstrap3'
        //                    });
        //                </script>";
        //        ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantInvalidErrorNotification", DDL_Plant_Error_script, false);
        //    }

        //}

        protected void DDL_Plant_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Plant.SelectedIndex != 0)
            {
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                string selectedMaterialValue = DDL_Material.SelectedValue.ToString();

                string selectedPlantText = DDL_Plant.SelectedItem.Text.ToString();
                string selectedMaterialText = DDL_Material.SelectedItem.Text.ToString();

                LoadApprovers(selectedPlantValue, "");

                //PlantLinesBinder(selectedPlantValue);

                //DataTable dataTable = DatabaseHelper.GetRMFieldsControlByPlantId(Convert.ToInt32(selectedMaterialValue), Convert.ToInt32(selectedPlantValue));
                DataTable dataTable = DatabaseHelper.GetBrandFieldsControlByMaterialIdAndPlantId(
                    Convert.ToInt16(selectedMaterialValue),
                    Convert.ToInt16(selectedPlantValue)
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    int result = InsertQCClass5Rpt_BrandFieldsControl(Convert.ToInt16(selectedMaterialValue), selectedMaterialText, Convert.ToInt16(selectedPlantValue), selectedPlantText);

                    if (result != 0)
                    {
                        // Reload the data after successful insertion
                        dataTable = DatabaseHelper.GetBrandFieldsControlByMaterialIdAndPlantId(
                            Convert.ToInt16(selectedMaterialValue),
                            Convert.ToInt16(selectedPlantValue)
                        );

                        if (dataTable != null && dataTable.Rows.Count > 0)
                        {
                            foreach (DataRow row in dataTable.Rows)
                            {
                                string fieldName = row["field_name"].ToString();

                                bool isVisible = Convert.ToBoolean(row["ViewMode"]);
                                string rfvErrorMessage = row["RFV_ErrorMsg"].ToString();
                                bool rfvEnabled = Convert.ToBoolean(row["RFV_YesNo"]);
                                string displayname = row["DisplayName"].ToString();
                                bool revEnabled = Convert.ToBoolean(row["REV_YesNo"]);
                                string revErrorMessage = row["REV_ErrorMsg"].ToString();
                                string revExpression = row["REV_Expression"].ToString();
                                bool rvEnabled = Convert.ToBoolean(row["RV_Yesno"]);
                                string rvErrorMessage = row["RV_ErrorMsg"].ToString();
                                string rvMinValue = row["RV_MinValue"].ToString();
                                string rvMaxValue = row["RV_MaxValue"].ToString();

                                ValidationCriteria criteria = new ValidationCriteria();
                                criteria.IsVisible = isVisible;
                                criteria.DisplayName = displayname;
                                criteria.RequiredFieldErrorMessage = rfvErrorMessage;
                                criteria.IsRequired = rfvEnabled;
                                criteria.RegularExpressionErrorMessage = revErrorMessage;
                                criteria.IsRegularExpressionRequired = revEnabled;
                                criteria.RegularExpression = revExpression;
                                criteria.RangeErrorMessage = rvErrorMessage;
                                criteria.IsRangeRequired = rvEnabled;
                                criteria.MinimumValue = rvMinValue;
                                criteria.MaximumValue = rvMaxValue;

                                SetUpValidatorsForField(fieldName, criteria);
                            }
                        }
                    }
                    else
                    {
                        string NoMapping_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'No Mapping Found!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                        ClientScript.RegisterStartupScript(this.GetType(), "ShowNoMappingErrorNotification", NoMapping_Error_script, false);
                    }
                }
                else
                {
                    // Process existing data logic (as in the original code)
                    foreach (DataRow row in dataTable.Rows)
                    {
                        string fieldName = row["field_name"].ToString();

                        bool isVisible = Convert.ToBoolean(row["ViewMode"]);
                        string displayname = row["DisplayName"].ToString();
                        bool rfvEnabled = Convert.ToBoolean(row["RFV_YesNo"]);
                        string rfvErrorMessage = row["RFV_ErrorMsg"].ToString();
                        bool revEnabled = Convert.ToBoolean(row["REV_YesNo"]);
                        string revErrorMessage = row["REV_ErrorMsg"].ToString();
                        string revExpression = row["REV_Expression"].ToString();
                        bool rvEnabled = Convert.ToBoolean(row["RV_Yesno"]);
                        string rvErrorMessage = row["RV_ErrorMsg"].ToString();
                        string rvMinValue = row["RV_MinValue"].ToString();
                        string rvMaxValue = row["RV_MaxValue"].ToString();

                        ValidationCriteria criteria = new ValidationCriteria();
                        criteria.IsVisible = isVisible;
                        criteria.DisplayName = displayname;
                        criteria.RequiredFieldErrorMessage = rfvErrorMessage;
                        criteria.IsRequired = rfvEnabled;
                        criteria.RegularExpressionErrorMessage = revErrorMessage;
                        criteria.IsRegularExpressionRequired = revEnabled;
                        criteria.RegularExpression = revExpression;
                        criteria.RangeErrorMessage = rvErrorMessage;
                        criteria.IsRangeRequired = rvEnabled;
                        criteria.MinimumValue = rvMinValue;
                        criteria.MaximumValue = rvMaxValue;

                        SetUpValidatorsForField(fieldName, criteria);
                    }
                }
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

        public int InsertQCClass5Rpt_BrandFieldsControl(int materialId, string materialName, int plantId, string plantName)
        {
            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.InsertQCRMClass5Rpt_BrandFieldsControl", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@material_id", materialId);
                    cmd.Parameters.AddWithValue("@material_name", materialName);
                    cmd.Parameters.AddWithValue("@plant_id", plantId);
                    cmd.Parameters.AddWithValue("@plant_name", plantName);

                    SqlParameter returnValue = new SqlParameter();
                    returnValue.Direction = ParameterDirection.ReturnValue;
                    cmd.Parameters.Add(returnValue);

                    try
                    {
                        conn.Open();
                        cmd.ExecuteNonQuery();
                        return (int)returnValue.Value;
                    }
                    catch (SqlException ex)
                    {
                        string sqlerrorScript = "<script type='text/javascript'>\n" +
                                 $"new PNotify({{\n" +
                                 "    title: 'Error',\n" +
                                 $"    text: '{ex.Message.Replace("'", "\\'")}',\n" +
                                 "    type: 'error',\n" +
                                 "    styling: 'bootstrap3'\n" +
                                 "});\n" +
                                 "</script>";
                        ClientScript.RegisterStartupScript(this.GetType(), "SqlExceptionNotification", sqlerrorScript, false);
                        return -1;
                    }
                    catch (Exception ex)
                    {
                        string catcherrorScript = "<script type='text/javascript'>\n" +
                                 $"new PNotify({{\n" +
                                 "    title: 'Error',\n" +
                                 $"    text: '{ex.Message.Replace("'", "\\'")}',\n" +
                                 "    type: 'error',\n" +
                                 "    styling: 'bootstrap3'\n" +
                                 "});\n" +
                                 "</script>";
                        ClientScript.RegisterStartupScript(this.GetType(), "ExceptionErrorNotification", catcherrorScript, false);
                        return -1;
                    }
                }
            }
        }

        private void LoadApprovers(string selectedPlantValue, string selectedPlantLineValue)
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
                    cmd.Parameters.AddWithValue("@FormID", 17); // Replace with actual value
                    cmd.Parameters.AddWithValue("@FormName", "RM_Class_3"); // Replace with actual value

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
                            hdn_formid.Value = "17";
                            DataRow row = dt.Rows[0];

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
                            hdn_formid.Value = "17";
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

        private void LoadApprovers(string selectedPlantValue)
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
                    cmd.Parameters.AddWithValue("@FormID", 19); // Replace with actual value
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
                            hdn_formid.Value = "19";
                            DataRow row = dt.Rows[0];

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
                            hdn_formid.Value = "19";
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

        private string GenerateUnique()
        {

            string newRmfValue;
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Fetch the maximum RMF01 value
                    string query = "SELECT ISNULL(MAX(CAST(SUBSTRING(RMFID, 4, LEN(RMFID)) AS INT)), 0) FROM TRN_RM_CLASS_5";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        object result = command.ExecuteScalar();
                        int maxRMFValue = Convert.ToInt32(result);

                        // Increment the numeric part
                        int numericPart = maxRMFValue + 1;

                        // Format the new value
                        newRmfValue = $"RMF{numericPart:D3}"; // Ensure three digits (e.g., RMF001, RMF002)
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                Console.WriteLine("Error generating RMF01: " + ex.Message);
                throw;
            }

            RmfId = newRmfValue;
            return newRmfValue;
        }

        private decimal? TryParseDecimal(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                // Return null if the input is null, empty, or consists only of whitespace
                return null;
            }

            decimal result;
            return decimal.TryParse(value, out result) ? (decimal?)result : null;
        }
        private int? TryParseInt(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                // Return null if the input is null, empty, or consists only of whitespace
                return null;
            }

            int result;
            return int.TryParse(value, out result) ? (int?)result : null;
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
        protected void DDL_Color_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (DDL_Color.SelectedItem.Text == "Select")
            {

                DatabaseHelper.BindWithDefaultNoRecords(DDL_Color);

                string DDL_Color_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'Invalid Selection!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowColorInvalidErrorNotification", DDL_Color_Error_script, false);


            }
            else if (DDL_Color.SelectedItem.Text == "Other")
            {
                ColorRemarksDiv.Style["display"] = "block";
            }
            else
            {
                // Hide custom color input fields
                ColorRemarksDiv.Style["display"] = "none";
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

        private void SetUpValidatorsForField(string fieldName, ValidationCriteria criteria)
        {
            switch (fieldName)
            {
                case "ProductBrand":

                    BrandDIV.Visible = criteria.IsVisible;
                    //Label_TB_Brand.Text = criteria.DisplayName;

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
                    //Lbl_TB_Supplier.Text = criteria.DisplayName;

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
                    //Lbl_TB_ChallanDate.Text = criteria.DisplayName;

                    RFV_TB_ChallanDate.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_ChallanDate.Enabled = criteria.IsRequired;

                    break;

                case "ChallanNo":

                    ChallanNoDIV.Visible = criteria.IsVisible;
                    //Lbl_TB_ChallanNo.Text = criteria.DisplayName;

                    RFV_TB_ChallanNo.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_ChallanNo.Enabled = criteria.IsRequired;

                    TB_ChallanNo.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_ChallanNo.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_ChallanNo.ValidationExpression = criteria.RegularExpression;
                    REV_TB_ChallanNo.Enabled = criteria.IsRegularExpressionRequired;


                    break;

                case "Quantity":

                    QuantityDIV.Visible = criteria.IsVisible;
                    //Lbl_TB_Quantity.Text = criteria.DisplayName;

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
                    //Lbl_TB_LotNo.Text = criteria.DisplayName;

                    RFV_TB_LotNo.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_LotNo.Enabled = criteria.IsRequired;

                    TB_LotNo.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_LotNo.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_LotNo.ValidationExpression = criteria.RegularExpression;
                    REV_TB_LotNo.Enabled = criteria.IsRegularExpressionRequired;

                    break;

                case "Batch":

                    BatchNoDIV.Visible = criteria.IsVisible;
                    //Lbl_TB_BatchNo.Text = criteria.DisplayName;

                    RFV_TB_BatchNo.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_BatchNo.Enabled = criteria.IsRequired;

                    TB_BatchNo.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_BatchNo.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_BatchNo.ValidationExpression = criteria.RegularExpression;
                    REV_TB_BatchNo.Enabled = criteria.IsRegularExpressionRequired;

                    break;

                case "Vehicle":

                    VehicleNoDIV.Visible = criteria.IsVisible;
                    //Lbl_TB_VehicleNo.Text = criteria.DisplayName;

                    RFV_TB_VehicleNo.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_VehicleNo.Enabled = criteria.IsRequired;

                    TB_VehicleNo.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_VehicleNo.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_VehicleNo.ValidationExpression = criteria.RegularExpression;
                    REV_TB_VehicleNo.Enabled = criteria.IsRegularExpressionRequired;

                    break;

                case "Pkd/MfgDate":

                    PkdMfgDIV.Visible = criteria.IsVisible;
                    //Lbl_TB_PkdMfg.Text = criteria.DisplayName;

                    RFV_TB_PkdMfg.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_PkdMfg.Enabled = criteria.IsRequired;

                    break;

                case "Grade":

                    GradeDIV.Visible = criteria.IsVisible;
                    //Lbl_RBL_Grade.Text = criteria.DisplayName;

                    RFV_RBL_Grade.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_RBL_Grade.Enabled = criteria.IsRequired;

                    break;

                case "Color":

                    ColorDIV.Visible = criteria.IsVisible;
                    //LabelColor.Text = criteria.DisplayName;

                    RFV_DDL_Color.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_DDL_Color.Enabled = criteria.IsRequired;

                    break;

                case "Odour/Smell":

                    SmellDIV.Visible = criteria.IsVisible;
                    //LabelSmell.Text = criteria.DisplayName;

                    RFV_RBL_Smell.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_RBL_Smell.Enabled = criteria.IsRequired;

                    break;

                case "Appearance":

                    AppearanceDIV.Visible = criteria.IsVisible;
                    //LabelAppearance.Text = criteria.DisplayName;

                    RFV_RBL_Appearance.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_RBL_Appearance.Enabled = criteria.IsRequired;

                    break;

                case "Taste/Flavor":

                    TasteFlavorDIV.Visible = criteria.IsVisible;
                    //LabelTasteFlavor.Text = criteria.DisplayName;

                    RFV_RBL_TasteFlavor.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_RBL_TasteFlavor.Enabled = criteria.IsRequired;

                    break;

                case "ForeignMatter/Impurities":

                    ImpuritiesDIV.Visible = criteria.IsVisible;
                    //Lbl_TB_Foreign_Impurities.Text = criteria.DisplayName;

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
                    //Lbl_TB_Purity.Text = criteria.DisplayName;

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
                    //Lbl_TB_PH.Text = criteria.DisplayName;

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
                    //Lbl_TB_Moisture.Text = criteria.DisplayName;

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
                    //Lbl_TB_Acid.Text = criteria.DisplayName;

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
                    //Lbl_TB_Mpcp.Text = criteria.DisplayName;

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
                    //Lbl_TB_Dispersability.Text = criteria.DisplayName;

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
                    //Lbl_TB_Drc.Text = criteria.DisplayName;

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
                    //Lbl_TB_MonoGlycerideContent.Text = criteria.DisplayName;

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
                    //Lbl_TB_Neutralizing.Text = criteria.DisplayName;

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
                    //Lbl_TB_Brix.Text = criteria.DisplayName;

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
                    //Lbl_TB_WIM.Text = criteria.DisplayName;

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
                    // Handle unrecognized field names
                    break;
            }
        }



        protected void BtnSubmit_Click(object sender, EventArgs e)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            string rmfId = GenerateUnique();  // Generate unique value
            int formID = Convert.ToInt32(hdn_formid.Value.ToString());

            string materialName = DDL_Material.SelectedValue;
            string plantName = DDL_Plant.SelectedValue;
            
            string Brand = string.IsNullOrEmpty(TB_Brand.Text) ? null : TB_Brand.Text;
            string supplier = string.IsNullOrEmpty(TB_Supplier.Text) ? null : TB_Supplier.Text;

            string challanNo = string.IsNullOrEmpty(TB_ChallanNo.Text) ? null : TB_ChallanNo.Text;
            DateTime? challanDate = string.IsNullOrEmpty(TB_ChallanDate.Text) ? (DateTime?)null : DateTime.Parse(TB_ChallanDate.Text).Date;
            
            string lotNo = string.IsNullOrEmpty(TB_LotNo.Text) ? null : TB_LotNo.Text;
            string batchNo = string.IsNullOrEmpty(TB_BatchNo.Text) ? null : TB_BatchNo.Text;
            DateTime? pkdMfgDate = string.IsNullOrEmpty(TB_PkdMfg.Text) ? (DateTime?)null : DateTime.Parse(TB_PkdMfg.Text).Date;
            string vehicleNo = string.IsNullOrEmpty(TB_VehicleNo.Text) ? null : TB_VehicleNo.Text;

            decimal? quantity = !string.IsNullOrEmpty(TB_Quantity.Text) ? Convert.ToDecimal(TB_Quantity.Text) : (decimal?)null;
            string quantityRemarks = string.IsNullOrEmpty(TXB_Quantity_Remarks.Text) ? null : TXB_Quantity_Remarks.Text;

            string color = string.IsNullOrEmpty(DDL_Color.SelectedItem.Text) ? null : DDL_Color.Text;
            string colorRemarks = string.IsNullOrEmpty(TXB_Color_Remarks.Text) ? null : TXB_Color_Remarks.Text;
            
            int? grade = string.IsNullOrEmpty(RBL_Grade.SelectedValue) ? (int?)null : (int?)Convert.ToInt32(RBL_Grade.SelectedValue);
            string gradeRemarks = string.IsNullOrEmpty(TXB_Grade_Remarks.Text) ? null : TXB_Grade_Remarks.Text;

            int? smell = string.IsNullOrEmpty(RBL_Smell.SelectedValue) ? (int?)null : (int?)Convert.ToInt32(RBL_Smell.SelectedValue);
            string smellRemarks = string.IsNullOrEmpty(TXB_Smell_Remarks.Text) ? null : TXB_Smell_Remarks.Text;

            int? appearance = string.IsNullOrEmpty(RBL_Appearance.SelectedValue) ? (int?)null : (int?)Convert.ToInt32(RBL_Appearance.SelectedValue);
            string appearanceRemarks = string.IsNullOrEmpty(TXB_Appearance_Remarks.Text) ? null : TXB_Appearance_Remarks.Text;

            int? taste = string.IsNullOrEmpty(RBL_TasteFlavor.SelectedValue) ? (int?)null : (int?)Convert.ToInt32(RBL_TasteFlavor.SelectedValue);
            string tasteRemarks = string.IsNullOrEmpty(TXB_TasteFlavor_Remarks.Text) ? null : TXB_TasteFlavor_Remarks.Text;

            string foreignImpurites = string.IsNullOrEmpty(TB_Foreign_Impurities.Text) ? null : TB_Foreign_Impurities.Text;

            decimal? purity = !string.IsNullOrEmpty(TB_Purity.Text) ? Convert.ToDecimal(TB_Purity.Text) : (decimal?)null;
            string purityRemarks = string.IsNullOrEmpty(TXB_Purity_Remarks.Text) ? null : TXB_Purity_Remarks.Text;

            decimal? ph = !string.IsNullOrEmpty(TB_PH.Text) ? Convert.ToDecimal(TB_PH.Text) : (decimal?)null;
            string phRemarks = string.IsNullOrEmpty(TXB_PH_Remarks.Text) ? null : TXB_PH_Remarks.Text;

            decimal? moisture = !string.IsNullOrEmpty(TB_Moisture.Text) ? Convert.ToDecimal(TB_Moisture.Text) : (decimal?)null;
            string moistureRemarks = string.IsNullOrEmpty(TXB_Moisture_Remarks.Text) ? null : TXB_Moisture_Remarks.Text;

            decimal? acid = !string.IsNullOrEmpty(TB_Acid.Text) ? Convert.ToDecimal(TB_Acid.Text) : (decimal?)null;
            string acidRemarks = string.IsNullOrEmpty(TXB_Acid_Remarks.Text) ? null : TXB_Acid_Remarks.Text;

            decimal? mpcp = !string.IsNullOrEmpty(TB_Mpcp.Text) ? Convert.ToDecimal(TB_Mpcp.Text) : (decimal?)null;
            string mpcpRemarks = string.IsNullOrEmpty(TXB_Mpcp_Remarks.Text) ? null : TXB_Mpcp_Remarks.Text;

            decimal? dispersabiity = !string.IsNullOrEmpty(TB_Dispersability.Text) ? Convert.ToDecimal(TB_Dispersability.Text) : (decimal?)null;
            string dispersabiityRemarks = string.IsNullOrEmpty(TXB_Dispersability_Remarks.Text) ? null : TXB_Dispersability_Remarks.Text;

            decimal? drc = !string.IsNullOrEmpty(TB_Drc.Text) ? Convert.ToDecimal(TB_Drc.Text) : (decimal?)null;
            string drcRemarks = string.IsNullOrEmpty(TXB_Drc_Remarks.Text) ? null : TXB_Drc_Remarks.Text;

            decimal? monoGlycerideContent = !string.IsNullOrEmpty(TB_MonoGlycerideContent.Text) ? Convert.ToDecimal(TB_MonoGlycerideContent.Text) : (decimal?)null;
            string monoGlycerideContentRemarks = string.IsNullOrEmpty(TXB_MonoGlycerideContent_Remarks.Text) ? null : TXB_MonoGlycerideContent_Remarks.Text;

            decimal? neutralizingValue = !string.IsNullOrEmpty(TB_Neutralizing.Text) ? Convert.ToDecimal(TB_Neutralizing.Text) : (decimal?)null;
            string neutralizingValueRemarks = string.IsNullOrEmpty(TXB_Neutralizing_Remarks.Text) ? null : TXB_Neutralizing_Remarks.Text;

            decimal? brix = !string.IsNullOrEmpty(TB_Brix.Text) ? Convert.ToDecimal(TB_Brix.Text) : (decimal?)null;
            string brixRemarks = string.IsNullOrEmpty(TXB_Brix_Remarks.Text) ? null : TXB_Brix_Remarks.Text;

            decimal? wim = !string.IsNullOrEmpty(TB_WIM.Text) ? Convert.ToDecimal(TB_WIM.Text) : (decimal?)null;
            string wimRemarks = string.IsNullOrEmpty(TXB_WIM.Text) ? null : TXB_WIM.Text;

            int? accepted = string.IsNullOrEmpty(RBL_AppStatus.SelectedValue) ? (int?)null : (int?)Convert.ToInt32(RBL_AppStatus.SelectedValue);
            string acceptedRemarks = string.IsNullOrEmpty(TXB_AppStatus_Remarks.Text) ? null : TXB_AppStatus_Remarks.Text;

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
                    using (SqlCommand command = new SqlCommand("SP_RM_CLASS_5", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Add parameters
                        command.Parameters.AddWithValue("@RMFID", rmfId);
                        command.Parameters.AddWithValue("@FormID", formID);

                        command.Parameters.AddWithValue("@MaterialName", materialName);
                        command.Parameters.AddWithValue("@PlantName", plantName);

                        command.Parameters.AddWithValue("@ProductBrand", (object)Brand ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Supplier_Name", (object)supplier ?? DBNull.Value);
                       
                        command.Parameters.AddWithValue("@Challan_No", (object)challanNo ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Challan_Date", (object)challanDate ?? DBNull.Value);
                        
                        command.Parameters.AddWithValue("@Lot_No", (object)lotNo ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Batch_No", (object)batchNo ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Pkd_Date", (object)pkdMfgDate ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Vehicle_No", (object)vehicleNo ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Quantity", (object)quantity ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForQuantity", (object)quantityRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Color", (object)color ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForColor", (object)colorRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Grade", (object)grade ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForGrade", (object)gradeRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Smell", (object)smell ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForSmell", (object)smellRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Appearance", (object)appearance ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForAppearance", (object)appearanceRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Taste", (object)taste ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForTaste", (object)tasteRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Foreign_Matter_Impurities", (object)foreignImpurites ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Purity", (object)purity ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForPurity", (object)purityRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@PH", (object)ph ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForPH", (object)phRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Moisture", (object)moisture ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForMoisture", (object)moistureRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Acid", (object)acid ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForAcid", (object)acidRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@MpCp", (object)mpcp ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForMpCp", (object)mpcpRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Dispersability", (object)dispersabiity ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForDispersability", (object)dispersabiityRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Drc", (object)drc ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForDrc", (object)drcRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@MonoGlycerideContent", (object)monoGlycerideContent ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForMonoGlycerideContent", (object)monoGlycerideContentRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@NeutralizingValue", (object)neutralizingValue ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForNeutralizingValue", (object)neutralizingValueRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Brix", (object)brix ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForBrix", (object)brixRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@WIM", (object)wim ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForWIM", (object)wimRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@AppStatus", (object)accepted ?? DBNull.Value);
                        command.Parameters.AddWithValue("@AppStatusRemarks", (object)acceptedRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Material_Image", (object)ImgLink1 ?? DBNull.Value);

                        command.Parameters.AddWithValue("@SubmittedDate", submittedDate);
                        command.Parameters.AddWithValue("@SubmittedTime", submittedTime);
                        command.Parameters.AddWithValue("@SubmittedById", submittedById);
                        command.Parameters.AddWithValue("@SubmittedByEmployeeCode", (object)submittedByEmployeeCode ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Approver1EmployeeCode", (object)approver1EmployeeCode ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Approver2EmployeeCode", (object)approver2EmployeeCode ?? DBNull.Value);
                        command.Parameters.AddWithValue("@DottedLineApproverEmployeeCode", (object)dottedLineApproverEmployeeCode ?? DBNull.Value);


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
            DDL_Plant.Enabled = false;
            DDL_Material.Enabled = false;
            
            TB_Brand.Enabled = false;
            TB_Supplier.ReadOnly = true;

            TB_ChallanNo.ReadOnly = true;
            TB_ChallanDate.ReadOnly = true;
            
            TB_Quantity.ReadOnly = true;
            TXB_Quantity_Remarks.ReadOnly = true;
            
            TB_LotNo.ReadOnly = true;
            TB_BatchNo.ReadOnly = true;
            TB_PkdMfg.ReadOnly = true;
            TB_VehicleNo.ReadOnly = true;

            RBL_Grade.Enabled = true;
            TXB_Grade_Remarks.ReadOnly = true;

            RBL_Smell.Enabled = false;
            TXB_Smell_Remarks.ReadOnly = true;

            RBL_Appearance.Enabled = false;
            TXB_Appearance_Remarks.ReadOnly = true;

            DDL_Color.Enabled = false;
            TXB_Color_Remarks.ReadOnly = true;

            RBL_TasteFlavor.Enabled = false;
            TXB_TasteFlavor_Remarks.ReadOnly = true;

            TB_Foreign_Impurities.ReadOnly = true;

            TB_Purity.ReadOnly = true;
            TXB_Purity_Remarks.ReadOnly = true;

            TB_PH.ReadOnly = true;
            TXB_PH_Remarks.ReadOnly = true;

            TB_Moisture.ReadOnly = true;
            TXB_Moisture_Remarks.ReadOnly = true;

            TB_Acid.ReadOnly = true;
            TXB_Acid_Remarks.ReadOnly = true;

            TB_Mpcp.ReadOnly = true;
            TXB_Mpcp_Remarks.ReadOnly = true;

            TB_Dispersability.ReadOnly = true;
            TXB_Dispersability_Remarks.ReadOnly = true;

            TB_Drc.ReadOnly = true;
            TXB_Drc_Remarks.ReadOnly = true;

            TB_MonoGlycerideContent.ReadOnly = true;
            TXB_MonoGlycerideContent_Remarks.ReadOnly = true;

            TB_Neutralizing.ReadOnly = true;
            TXB_Neutralizing_Remarks.ReadOnly = true;

            TB_Brix.ReadOnly = true;
            TXB_Brix_Remarks.ReadOnly = true;

            TB_WIM.ReadOnly = true;
            TXB_WIM.ReadOnly = true;

            RBL_AppStatus.Enabled = false;
            TXB_AppStatus_Remarks.ReadOnly = true;

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
            Response.Redirect("RM_Class_5.aspx");
        }
    }
}