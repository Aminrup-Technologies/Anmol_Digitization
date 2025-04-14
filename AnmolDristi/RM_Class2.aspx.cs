using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.EnterpriseServices;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System.Windows.Media.Media3D;

namespace AnmolDristi
{
    public partial class RM_Class2 : System.Web.UI.Page
    {
        DB_Utility_OH4Y dbcl = new DB_Utility_OH4Y();
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
                    hdn_formid.Value = "15";
                    DisableReqFields();

                    lbl_docname.Text = "QC - RM Class 2 Report";
                    lbl_docnumber.Text = "Oils and Fats";
                    MaterialBinder();


                }

            }
        }
        private void DisableReqFields()
        {
            //RFV_DDL_Material.Enabled = false;
            //RFV_DDL_Plant.Enabled = false;
            RFV_TB_ProductName.Enabled = false;
            RFV_TB_BrandName.Enabled = false;
            RFV_TB_Supplier.Enabled = false;
            RFV_TB_ChalanNo.Enabled = false;
            RFV_TB_ChalanDate.Enabled = false;
            RFV_TB_LotNo.Enabled = false;
            RFV_TB_VehicleNo.Enabled = false;
            RFV_TB_BatchNo.Enabled = false;
            RFV_TB_Grade.Enabled = false;
            RFV_RBL_Color.Enabled = false;
            RFV_TB_ColorRemarks.Enabled = false;
            RFV_RBL_Smell.Enabled = false;
            RFV_TB_Smell_Remarks.Enabled = false;
            RFV_RBL_TasteFlavor.Enabled = false;
            RFV_TB_TasteFlavor_Remarks.Enabled = false;
            RFV_RBL_Consistency.Enabled = false;
            RFV_TB_ConsistencyRemarks.Enabled = false;
            RFV_TB_Foreign_Impurities.Enabled = false;
            REV_TB_MP.Enabled = false;
            REV_TB_PV.Enabled = false;
            RFV_TB_Moisture.Enabled = false;
            RFV_TB_IodineVal.Enabled = false;
            RFV_TB_SapVal.Enabled = false;
            REV_TB_AcidValue.Enabled = false;
            RFV_RBL_KriesTest.Enabled = false;
            RFV_TB_KriesTest_Remarks.Enabled = false;
            RFV_RBL_OilTest.Enabled = false;
            RFV_TB_OilTest_Remarks.Enabled = false;
            //RFV_TB_FinalRemarks.Enabled = false;
            RFV_RBL_AppStatus.Enabled = false;
            RFV_TXB_AppStatus_Remarks.Enabled = false;
            RFV_FU_MaterialImage.Enabled = false;
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
                ClientScript.RegisterStartupScript(this.GetType(), "ShowMaterialBinderErrorNotification", MaterialBinder_Error_script, false);
            }
        }
        protected void DDL_Material_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Material.SelectedIndex != 0)
            {
                string selectedMaterialValue = DDL_Material.SelectedValue.ToString();
                PlantBinder(selectedMaterialValue);

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
        protected void DDL_Plant_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Plant.SelectedIndex != 0)
            {
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                string selectedMaterialValue = DDL_Material.SelectedValue.ToString();

                string selectedPlantText = DDL_Plant.SelectedItem.Text.ToString();
                string selectedMaterialText = DDL_Material.SelectedItem.Text.ToString();

                LoadApprovers(selectedPlantValue, "");

                DataTable dataTable = DatabaseHelper.GetBrandFieldsControlByMaterialIdAndPlantId(
                    Convert.ToInt16(selectedMaterialValue),
                    Convert.ToInt16(selectedPlantValue)
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    int result = InsertQCAataMaidaRpt_BrandFieldsControl(
                        Convert.ToInt16(selectedMaterialValue),
                        selectedMaterialText,
                        Convert.ToInt16(selectedPlantValue),
                        selectedPlantText
                    );

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
        public int InsertQCAataMaidaRpt_BrandFieldsControl(int materialId, string materialName, int plantId, string plantName)
        {
            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.InsertQCRMClass2Rpt_MaterialsFieldsControl", conn))
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
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("usp_GetFormsApprovalMatrix", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@PlantId", selectedPlantValue);
                    //cmd.Parameters.AddWithValue("@LineId", selectedPlantLineValue);
                    cmd.Parameters.AddWithValue("@LineId", string.IsNullOrEmpty(selectedPlantLineValue) ? (object)DBNull.Value : selectedPlantLineValue);
                    cmd.Parameters.AddWithValue("@FormID", 16);
                    cmd.Parameters.AddWithValue("@FormName", "RM_Class_2");

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        hdn_formid.Value = "16";
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
                            bool isInserted = dbcl.InsertDefaultApprovers(selectedPlantValue, selectedPlantLineValue, 16);

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
                    string query = "SELECT ISNULL(MAX(CAST(SUBSTRING(RMFID, 4, LEN(RMFID)) AS INT)), 0) FROM TRN_RM_CLASS_2";
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
        protected void BtnSubmit_Click1(object sender, EventArgs e)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            string rmfId = GenerateUnique();  // Generate unique value

            int FormID = Convert.ToInt32(hdn_formid.Value.ToString());

            string MaterialName = DDL_Material.SelectedValue;
            string PlantName = DDL_Plant.SelectedValue;

            string Product = string.IsNullOrEmpty(TB_ProductName.Text) ? null : TB_ProductName.Text;
            string Brand = string.IsNullOrEmpty(TB_BrandName.Text) ? null : TB_BrandName.Text;
            string Supplier_Name = string.IsNullOrEmpty(TB_Supplier.Text) ? null : TB_Supplier.Text;
            string Challan_No = string.IsNullOrEmpty(TB_ChalanNo.Text) ? null : TB_ChalanNo.Text;
            DateTime? Challan_Date = string.IsNullOrEmpty(TB_ChalanDate.Text) ? (DateTime?)null : DateTime.Parse(TB_ChalanDate.Text).Date;
            string Vehicle_No = string.IsNullOrEmpty(TB_VehicleNo.Text) ? null : TB_VehicleNo.Text;
            int? Lot_No = TryParseInt(TB_LotNo.Text);
            string Batch_No = string.IsNullOrEmpty(TB_BatchNo.Text) ? null : TB_BatchNo.Text;
            string Grade = string.IsNullOrEmpty(TB_Grade.Text) ? null : TB_Grade.Text;

            int? Color = string.IsNullOrEmpty(RBL_Color.SelectedValue) ? (int?)null : (int?)Convert.ToInt32(RBL_Color.SelectedValue);
            string CommentsForColor = string.IsNullOrEmpty(TB_ColorRemarks.Text) ? null : TB_ColorRemarks.Text;

            int? Smell = string.IsNullOrEmpty(RBL_Smell.SelectedValue) ? (int?)null : (int?)Convert.ToInt32(RBL_Smell.SelectedValue);
            string CommentsForSmell = string.IsNullOrEmpty(TB_Smell_Remarks.Text) ? null : TB_Smell_Remarks.Text;

            int? Taste = string.IsNullOrEmpty(RBL_TasteFlavor.SelectedValue) ? (int?)null : (int?)Convert.ToInt32(RBL_TasteFlavor.SelectedValue);
            string CommentsForTaste = string.IsNullOrEmpty(TB_TasteFlavor_Remarks.Text) ? null : TB_TasteFlavor_Remarks.Text;

            int? Consistency = string.IsNullOrEmpty(RBL_Consistency.SelectedValue) ? (int?)null : (int?)Convert.ToInt32(RBL_Consistency.SelectedValue);
            string CommentsForConsistency = string.IsNullOrEmpty(TB_ConsistencyRemarks.Text) ? null : TB_ConsistencyRemarks.Text;

            //string foreignImpurites = string.IsNullOrEmpty(TB_Foreign_Impurities.Text) ? null : TB_Foreign_Impurities.Text;
            decimal? Foreign_Matter_Impurities = TryParseDecimal(TB_Foreign_Impurities.Text);
            decimal? MP_CP = TryParseDecimal(TB_MP.Text);
            decimal? PV = TryParseDecimal(TB_PV.Text);
            decimal? Moisture = TryParseDecimal(TB_Moisture.Text);
            decimal? Iodine_Val = TryParseDecimal(TB_IodineVal.Text);
            decimal? SAP_Val = TryParseDecimal(TB_IodineVal.Text);
            decimal? Acid_Val = TryParseDecimal(TB_IodineVal.Text);

            int? KriesTest = string.IsNullOrEmpty(RBL_KriesTest.SelectedValue) ? (int?)null : (int?)Convert.ToInt32(RBL_KriesTest.SelectedValue);
            string CommentsForKriesTest = string.IsNullOrEmpty(TB_KriesTest_Remarks.Text) ? null : TB_KriesTest_Remarks.Text;

            int? OilTest = string.IsNullOrEmpty(RBL_OilTest.SelectedValue) ? (int?)null : (int?)Convert.ToInt32(RBL_OilTest.SelectedValue);
            string CommentsForOilTest = string.IsNullOrEmpty(TB_OilTest_Remarks.Text) ? null : TB_OilTest_Remarks.Text;

            int? AppStatus = string.IsNullOrEmpty(RBL_AppStatus.SelectedValue) ? (int?)null : (int?)Convert.ToInt32(RBL_AppStatus.SelectedValue);
            string AppStatusRemarks = string.IsNullOrEmpty(TXB_AppStatus_Remarks.Text) ? null : TXB_AppStatus_Remarks.Text;

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
                    using (SqlCommand command = new SqlCommand("SP_RM_CLASS_2", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Add parameters
                        command.Parameters.AddWithValue("@RMFID", rmfId);
                        command.Parameters.AddWithValue("@FormID", FormID);

                        command.Parameters.AddWithValue("@MaterialName", MaterialName);
                        command.Parameters.AddWithValue("@PlantName", PlantName);

                        command.Parameters.AddWithValue("@Product", Product);
                        command.Parameters.AddWithValue("@Brand", Brand);

                        command.Parameters.AddWithValue("@Supplier_Name", Supplier_Name);
                        command.Parameters.AddWithValue("@Challan_No", Challan_No);
                        command.Parameters.AddWithValue("@Challan_Date", Challan_Date);
                        command.Parameters.AddWithValue("@Lot_No", Lot_No);
                        command.Parameters.AddWithValue("@Vehicle_No", Vehicle_No);

                        command.Parameters.AddWithValue("@Batch_No", (object)Batch_No ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Grade", (object)Grade ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Color", Color);
                        command.Parameters.AddWithValue("@CommentsForColor", (object)CommentsForColor ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Smell", Smell);
                        command.Parameters.AddWithValue("@CommentsForSmell", (object)CommentsForSmell ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Taste", (object)Taste ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForTaste", (object)CommentsForTaste ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Consistency", (object)Consistency ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForConsistency", (object)CommentsForConsistency ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Foreign_Matter_Impurities", (object)Foreign_Matter_Impurities ?? DBNull.Value);
                        command.Parameters.AddWithValue("@MP_CP", (object)MP_CP ?? DBNull.Value);
                        command.Parameters.AddWithValue("@PV", (object)PV ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Moisture", (object)Moisture ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Iodine_Val", (object)Iodine_Val ?? DBNull.Value);
                        command.Parameters.AddWithValue("@SAP_Val", (object)SAP_Val ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Acid_Val", (object)Acid_Val ?? DBNull.Value);

                        command.Parameters.AddWithValue("@KriesTest", (object)KriesTest ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForKriesTest", (object)CommentsForKriesTest ?? DBNull.Value);

                        command.Parameters.AddWithValue("@OilTest", (object)OilTest ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForOilTest", (object)CommentsForOilTest ?? DBNull.Value);

                        command.Parameters.AddWithValue("@AppStatus", (object)AppStatus ?? DBNull.Value);
                        command.Parameters.AddWithValue("@AppStatusRemarks", (object)AppStatusRemarks ?? DBNull.Value);

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
            DDL_Material.Enabled = false;
            DDL_Plant.Enabled = false;

            TB_ProductName.Enabled = true;
            TB_BrandName.Enabled = true;
            TB_Supplier.ReadOnly = true;
            TB_ChalanNo.ReadOnly = true;
            TB_ChalanDate.ReadOnly = true;
            TB_LotNo.ReadOnly = true;
            TB_VehicleNo.ReadOnly = true;

            TB_BatchNo.ReadOnly = true;
            TB_Grade.ReadOnly = true;

            RBL_Smell.Enabled = false;
            TB_Smell_Remarks.ReadOnly = true;

            RBL_Color.Enabled = false;
            TB_ColorRemarks.ReadOnly = true;

            RBL_TasteFlavor.Enabled = false;
            TB_TasteFlavor_Remarks.ReadOnly = true;

            RBL_Consistency.Enabled = false;
            TB_ConsistencyRemarks.ReadOnly = true;

            TB_Foreign_Impurities.ReadOnly = true;
            TB_Moisture.ReadOnly = true;
            TB_MP.ReadOnly = true;
            TB_PV.ReadOnly = true;
            TB_IodineVal.ReadOnly = true;
            TB_SapVal.ReadOnly = true;
            TB_AcidValue.ReadOnly = true;

            RBL_KriesTest.Enabled = false;
            TB_KriesTest_Remarks.ReadOnly = true;

            RBL_OilTest.Enabled = false;
            TB_OilTest_Remarks.ReadOnly = true;

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
            Response.Redirect("RM_Class2.aspx");
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


        private void RemoveReqField()
        {

        }

    }
}