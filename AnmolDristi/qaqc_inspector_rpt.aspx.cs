using AnmolDristi.DAL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Transactions;
using System.Configuration;

namespace AnmolDristi.qaqc
{
    public partial class qaqc_inspector_rpt : BasePage
    {
        private readonly string cs = System.Configuration.ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        DB_Utility_OH4Y dbcl = new DB_Utility_OH4Y();

        protected void Page_Load(object sender, EventArgs e)
        {
            ValidateSession();

            if (!IsPostBack)
            {
                lbl_docname.Text = "QC - Inspection Report";
                lbl_docnumber.Text = "ANMOL/DOC/DAN/QA/02";

                lbl_QCIR_ClrApp.Attributes["data-prefix"] = "QCIR/ClrApp";
                lbl_QCIR_DesignImp.Attributes["data-prefix"] = "QCIR/DesignImp";

                PlantBinder();
                DisplayCurrentShift();
            }
        }

        private void ValidateSession()
        {
            if (Session["CurrentUserId"] == null)
            {
                Response.Redirect("qaqc_inspector_rpt.aspx", true);
            }
        }

        private void DisplayCurrentShift()
        {
            ShiftManager shiftManager = new ShiftManager();
            string currentShift = shiftManager.GetCurrentShiftType();
            hdn_shiftvalue.Value = currentShift;
        }

        private void LoadApprovers(string selectedPlantValue, string selectedPlantLineValue)
        {
            using (SqlConnection conn = new SqlConnection(cs))
            {
                using (SqlCommand cmd = new SqlCommand("usp_GetFormsApprovalMatrix", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@PlantId", selectedPlantValue);
                    cmd.Parameters.AddWithValue("@LineId", string.IsNullOrEmpty(selectedPlantLineValue) ? (object)DBNull.Value : selectedPlantLineValue);
                    cmd.Parameters.AddWithValue("@FormID", 1);
                    cmd.Parameters.AddWithValue("@FormName", "qaqc_inspector_rpt");

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        hdn_formid.Value = "1";
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        if (dt.Rows.Count > 0)
                        {
                            BindApproversData(dt);
                        }
                        else
                        {
                            bool isInserted = dbcl.InsertDefaultApprovers(selectedPlantValue, selectedPlantLineValue, 1);

                            if (isInserted)
                            {
                                da.Fill(dt);
                                if (dt.Rows.Count > 0)
                                {
                                    BindApproversData(dt);
                                }
                                else
                                {
                                    ShowErrorNotification("Failed to load approver data even after insertion.");
                                }
                            }
                            else
                            {
                                ShowErrorNotification("Failed to insert default approvers.");
                            }
                        }
                    }
                }
            }
        }

        private void BindApproversData(DataTable dt)
        {
            GridViewApprovers.DataSource = dt;
            GridViewApprovers.DataBind();

            DataRow row = dt.Rows[0];
            Approver1NameLabel.Text = row["Approver1Name"].ToString();
            Approver1CodeLabel.Text = row["Approver1EmployeeCode"].ToString();

            Approver2NameLabel.Text = row["Approver2Name"].ToString();
            Approver2CodeLabel.Text = row["Approver2EmployeeCode"].ToString();

            DottedLineApproverNameLabel.Text = row["DottedLineApproverName"].ToString();
            DottedLineApproverCodeLabel.Text = row["DottedLineApproverEmployeeCode"].ToString();
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

        public void PlantBinder()
        {
            string query = "SELECT plant_id, CONCAT(plant_name, '[', sap_code, ']') AS plant_name FROM MST_PlantDetails";
            bool recordsBound;

            DatabaseHelper.BindDropDownList(query, DDL_Plant, "plant_name", "plant_id", out recordsBound);

            if (!recordsBound)
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_Plant);
                ShowErrorNotification("An error occurred while loading plants!");
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

        protected void DDL_Plant_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Plant.SelectedIndex != 0)
            {
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                lbl_DDL_Plant_Value.Text = selectedPlantValue;
                PlantLinesBinder(selectedPlantValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_PlantLine);
                ShowErrorNotification("Invalid Selection!");
            }
        }

        private void PlantLinesBinder(string selectedPlantValue)
        {
            string query = "SELECT line_id, line_name FROM MST_Plant_Lines WHERE plant_id = @SelectedPlantValue and view_status=1 and delete_status=0 order by plant_id";
            bool recordsBound;

            DatabaseHelper.BindDropDownList(query, DDL_PlantLine, "line_name", "line_id", new SqlParameter("@SelectedPlantValue", selectedPlantValue), out recordsBound);

            if (!recordsBound)
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_PlantLine);
                ShowErrorNotification("An error occurred while loading plant lines!");
            }
        }

        protected void DDL_PlantLine_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_PlantLine.SelectedIndex != 0)
            {
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                string selectedPlantLineValue = DDL_PlantLine.SelectedValue.ToString();
                lbl_DDL_PlantLine_Value.Text = selectedPlantLineValue;

                LineProductsBinder(selectedPlantValue, selectedPlantLineValue);
                LoadApprovers(selectedPlantValue, selectedPlantLineValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductCategory);
                ShowErrorNotification("Invalid Selection!");
            }
        }

        private void LineProductsBinder(string selectedPlantValue, string selectedPlantLineValue)
        {
            string query = "SELECT category_id, category_name FROM MST_LineCategory WHERE plant_id = @PlantId AND line_id = @LineId and view_status=1 and delete_status=0 order by category_id";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@PlantId", selectedPlantValue),
                new SqlParameter("@LineId", selectedPlantLineValue)
            };

            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_ProductCategory, "category_name", "category_id", parameters, out recordsBound);

            if (!recordsBound)
            {
                ShowErrorNotification("No line categories found for the selected plant and line!");
            }
        }

        protected void DDL_ProductCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_ProductCategory.SelectedIndex != 0)
            {
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                string selectedPlantLineValue = DDL_PlantLine.SelectedValue.ToString();
                string selectedProductCategoryValue = DDL_ProductCategory.SelectedValue.ToString();
                lbl_DDL_ProductCategory_Value.Text = selectedProductCategoryValue;

                ProductBrandsBinder(selectedPlantValue, selectedPlantLineValue, selectedProductCategoryValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductBrand);
                ShowErrorNotification("Invalid Selection!");
            }
        }

        private void ProductBrandsBinder(string selectedPlantValue, string selectedPlantLineValue, string selectedProductCategoryValue)
        {
            string query = "SELECT brand_id, brand_name FROM MST_LineCatBrands WHERE plant_id = @PlantId AND line_id = @LineId and category_id=@CategoryId and view_status=1 and delete_status=0 order by brand_id";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@PlantId", selectedPlantValue),
                new SqlParameter("@LineId", selectedPlantLineValue),
                new SqlParameter("@CategoryId", selectedProductCategoryValue)
            };

            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_ProductBrand, "brand_name", "brand_id", parameters, out recordsBound);

            if (!recordsBound)
            {
                ShowErrorNotification("No Brands found for the selected plant and line!");
            }
        }

        private void LogToTextFile(Dictionary<string, object> data)
        {
            string targetFolderPath = Server.MapPath("~/UploadedFiles/Logs/");
            string guid = Guid.NewGuid().ToString("N").Substring(0, 8);
            string filePath = Path.Combine(targetFolderPath, $"QCInspectorLog_{DateTime.Now:yyyy-MM-dd}_{guid}.txt");

            if (!Directory.Exists(targetFolderPath))
            {
                Directory.CreateDirectory(targetFolderPath);
            }

            using (StreamWriter writer = File.CreateText(filePath))
            {
                foreach (var pair in data)
                {
                    writer.WriteLine($"{pair.Key}: {pair.Value}");
                }
            }
        }

        private void MakeInputsReadOnly()
        {
            DDL_Plant.Enabled = false;
            DDL_PlantLine.Enabled = false;
            DDL_ProductCategory.Enabled = false;
            DDL_ProductBrand.Enabled = false;
            DDL_BrandSKU.Enabled = false;
            TB_NoOfPcs.ReadOnly = true;
            TB_Length.ReadOnly = true;
            TB_Breadth.ReadOnly = true;
            TB_Height.ReadOnly = true;
            TB_VartyPkt.ReadOnly = true;
            TB_BakingTime.ReadOnly = true;
            RBL_FlavTst.Enabled = false;
            TXB_RBL_FlavTst_Rmrks.ReadOnly = true;
            TB_aWMAX.ReadOnly = true;
            TB_pHvalue.Enabled = false;
            TB_BakingTime2.ReadOnly = true;
            TB_Moisture.ReadOnly = true;
            TB_GaugeLen.ReadOnly = true;
            TB_wgtwtoil.ReadOnly = true;
            TB_wgtwoil.ReadOnly = true;
            TB_oilpercent.ReadOnly = true;
            TB_PktWgt.ReadOnly = true;

            btnSubmit.Enabled = false;
            btnSubmit.Text = "SAVED";
            btnSubmit.CssClass = "btn btn-sm btn-success";

            string Data_SuccessScript = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Data Success',
                                text: 'Recorded Successfully!!',
                                type: 'success',
                                styling: 'bootstrap3'
                            });
                        </script>";

            ClientScript.RegisterStartupScript(this.GetType(), "ShowDataSuccessNotification", Data_SuccessScript, false);
        }

        protected void BtnUploadFU_DesgImp_Click(object sender, EventArgs e)
        {
            Page.RegisterAsyncTask(new PageAsyncTask(UploadImage1Task));
        }

        private async Task UploadImage1Task()
        {
            if (await UploadImage1Async())
            {
                string UI_1_Successscript = @"<script type='text/javascript'>
                    new PNotify({
                        title: 'Upload Success',
                        text: 'Image Saved!!',
                        type: 'success',
                        styling: 'bootstrap3'
                    });
                </script>";

                ClientScript.RegisterStartupScript(this.GetType(), "ShowImage1SuccessNotification", UI_1_Successscript, false);
            }
        }

        private async Task<bool> UploadImage1Async()
        {
            bool imgSaved = false;

            try
            {
                string TBPhotoId = "QCIR";
                string targetFolderPath = Server.MapPath("~/UploadedFiles/QCIR/DesignImp/");

                if (!Directory.Exists(targetFolderPath))
                {
                    Directory.CreateDirectory(targetFolderPath);
                }

                if (FU_DesgImp.PostedFile != null)
                {
                    string extension = Path.GetExtension(FU_DesgImp.FileName).ToLower();
                    if (extension == ".png" || extension == ".jpg" || extension == ".jpeg")
                    {
                        Stream strm = FU_DesgImp.PostedFile.InputStream;
                        using (var uploadedImage = System.Drawing.Image.FromStream(strm))
                        {
                            int maxWidth = 800;
                            int newWidth = uploadedImage.Width > maxWidth ? maxWidth : uploadedImage.Width;
                            int newHeight = (int)((double)newWidth / uploadedImage.Width * uploadedImage.Height);

                            using (var resizedImage = uploadedImage.GetThumbnailImage(newWidth, newHeight, null, IntPtr.Zero))
                            {
                                string fileName = $"{TBPhotoId}_{DateTime.Now:yyyyMMddHHmmss}.jpg";
                                string targetPath = Path.Combine(targetFolderPath, fileName);
                                resizedImage.Save(targetPath, System.Drawing.Imaging.ImageFormat.Jpeg);

                                string imgLink = "~/UploadedFiles/QCIR/DesignImp/" + fileName;
                                hdn_img1.Value = imgLink;

                                FU_DesgImp_Img.Visible = true;
                                uploadedImage1.ImageUrl = imgLink;
                                imgSaved = true;
                                FU_DesgImp_Upldr.Visible = false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);
                ShowErrorNotification(ex.Message);
            }

            return imgSaved;
        }

        protected void BtnUploadClrApp_Click(object sender, EventArgs e)
        {
            Page.RegisterAsyncTask(new PageAsyncTask(UploadImage2Task));
        }

        private async Task UploadImage2Task()
        {
            if (await UploadImage2Async())
            {
                string UI_2_Successscript = @"<script type='text/javascript'>
                    new PNotify({
                        title: 'Upload Success',
                        text: 'Image Saved!!',
                        type: 'success',
                        styling: 'bootstrap3'
                    });
                </script>";

                ClientScript.RegisterStartupScript(this.GetType(), "ShowImage2SuccessNotification", UI_2_Successscript, false);
            }
        }

        private async Task<bool> UploadImage2Async()
        {
            bool imgSaved = false;

            try
            {
                string TBPhotoId = "QCIR";
                string targetFolderPath = Server.MapPath("~/UploadedFiles/QCIR/ClrApp/");

                if (!Directory.Exists(targetFolderPath))
                {
                    Directory.CreateDirectory(targetFolderPath);
                }

                if (FU_ClrApp.PostedFile != null)
                {
                    string extension = Path.GetExtension(FU_ClrApp.FileName).ToLower();
                    if (extension == ".png" || extension == ".jpg" || extension == ".jpeg")
                    {
                        Stream strm = FU_ClrApp.PostedFile.InputStream;
                        using (var uploadedImage = System.Drawing.Image.FromStream(strm))
                        {
                            int maxWidth = 800;
                            int newWidth = uploadedImage.Width > maxWidth ? maxWidth : uploadedImage.Width;
                            int newHeight = (int)((double)newWidth / uploadedImage.Width * uploadedImage.Height);

                            using (var resizedImage = uploadedImage.GetThumbnailImage(newWidth, newHeight, null, IntPtr.Zero))
                            {
                                string fileName = $"{TBPhotoId}_{DateTime.Now:yyyyMMddHHmmss}.jpg";
                                string targetPath = Path.Combine(targetFolderPath, fileName);
                                resizedImage.Save(targetPath, System.Drawing.Imaging.ImageFormat.Jpeg);

                                string imgLink = "~/UploadedFiles/QCIR/ClrApp/" + fileName;
                                hdn_img2.Value = imgLink;

                                FU_ClrApp_Img.Visible = true;
                                uploadedImage2.ImageUrl = imgLink;
                                imgSaved = true;
                                FU_ClrApp_Upldr.Visible = false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);
                DisplayErrorNotification(ex.Message);
            }

            return imgSaved;
        }

        protected void DDL_ProductBrand_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_ProductBrand.SelectedIndex != 0)
            {
                string selectedProductBrandValue = DDL_ProductBrand.SelectedValue.ToString();
                lbl_DDL_ProductBrand_Value.Text = selectedProductBrandValue;
                BrandSKUBinder(selectedProductBrandValue);

                DataTable dataTable = DatabaseHelper.GetFormFieldControlByFormAndBrand(
                    1,
                    Convert.ToInt32(DDL_Plant.SelectedValue),
                    Convert.ToInt32(DDL_PlantLine.SelectedValue),
                    Convert.ToInt32(DDL_ProductBrand.SelectedValue),
                    Convert.ToInt32(DDL_ProductCategory.SelectedValue)
                );

                foreach (DataRow row in dataTable.Rows)
                {
                    ValidationCriteria criteria = new ValidationCriteria
                    {
                        DisplayName = row["DisplayName"].ToString(),
                        RequiredFieldErrorMessage = row["RFV_ErrorMsg"].ToString(),
                        IsRequired = Convert.ToBoolean(row["RFV_YesNo"]),
                        RegularExpressionErrorMessage = row["REV_ErrorMsg"].ToString(),
                        IsRegularExpressionRequired = Convert.ToBoolean(row["REV_YesNo"]),
                        RegularExpression = row["REV_Expression"].ToString(),
                        RangeErrorMessage = row["RV_ErrorMsg"].ToString(),
                        IsRangeRequired = Convert.ToBoolean(row["RV_Yesno"]),
                        MinimumValue = row["RV_MinValue"].ToString(),
                        MaximumValue = row["RV_MaxValue"].ToString(),
                        Visibility = Convert.ToBoolean(row["ViewMode"])
                    };

                    SetUpValidatorsForField(row["field_name"].ToString(), criteria);
                }
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductBrand);
                ShowErrorNotification("Invalid Selection!");
            }
        }

        private void BrandSKUBinder(string selectedProductBrandValue)
        {
            string query = "SELECT SKUId, SKU_name FROM MST_Brand_SKU WHERE brand_id = @SelectedPlantValue and ViewMode=1 order by SKUId";
            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_BrandSKU, "SKU_name", "SKUId", new SqlParameter("@SelectedPlantValue", selectedProductBrandValue), out recordsBound);

            if (!recordsBound)
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_PlantLine);
                ShowErrorNotification("An error occurred while loading SKUs!");
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
                    hdnMinShapeSize.Value = criteria.MinimumValue.ToString();
                    hdnMaxShapeSize.Value = criteria.MaximumValue.ToString();
                    break;

                case "TextureBite":
                    Lbl_RBL_TextureBite.Text = criteria.DisplayName;
                    RFV_RBL_TextureBite.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_RBL_TextureBite.Enabled = criteria.IsRequired;
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
                    CV_TB_GaugeLen.Enabled = criteria.IsRangeRequired;
                    hdnMinGaugelen.Value = criteria.MinimumValue.ToString();
                    hdnMaxGaugelen.Value = criteria.MaximumValue.ToString();
                    break;

                case "wgtwtoil":
                    TB_wgtwtoil_DIV.Visible = criteria.Visibility;
                    Lbl_TB_wgtwtoil.Text = criteria.DisplayName;
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
                    Lbl_TB_oilpercent.Text = criteria.DisplayName;
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

                case "Shell10Weight":
                    TB_shell10_DIV.Visible = criteria.Visibility;
                    Lbl_TB_shell10.Text = criteria.DisplayName;
                    RFV_TB_shell10.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_shell10.Enabled = criteria.IsRequired;
                    TB_shell10.Attributes["placeholder"] = criteria.RangeErrorMessage;
                    REV_TB_shell10.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_shell10.ValidationExpression = criteria.RegularExpression;
                    REV_TB_shell10.Enabled = criteria.IsRegularExpressionRequired;
                    RV_TB_shell10.ErrorMessage = criteria.RangeErrorMessage;
                    RV_TB_shell10.MinimumValue = criteria.MinimumValue;
                    RV_TB_shell10.MaximumValue = criteria.MaximumValue;
                    RV_TB_shell10.Enabled = criteria.IsRangeRequired;
                    break;

                case "Shell5CreamWeight":
                    TB_shell5cream_DIV.Visible = criteria.Visibility;
                    Lbl_TB_shell5cream.Text = criteria.DisplayName;
                    RFV_TB_shell5cream.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_shell5cream.Enabled = criteria.IsRequired;
                    TB_shell5cream.Attributes["placeholder"] = criteria.RangeErrorMessage;
                    REV_TB_shell5cream.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_shell5cream.ValidationExpression = criteria.RegularExpression;
                    REV_TB_shell5cream.Enabled = criteria.IsRegularExpressionRequired;
                    RV_TB_shell5cream.ErrorMessage = criteria.RangeErrorMessage;
                    RV_TB_shell5cream.MinimumValue = criteria.MinimumValue;
                    RV_TB_shell5cream.MaximumValue = criteria.MaximumValue;
                    RV_TB_shell5cream.Enabled = criteria.IsRangeRequired;
                    break;

                case "CreamPercentage":
                    TB_creampercent_DIV.Visible = criteria.Visibility;
                    Lbl_TB_creampercent.Text = criteria.DisplayName;
                    RFV_TB_creampercent.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_creampercent.Enabled = criteria.IsRequired;
                    TB_creampercent.Attributes["placeholder"] = criteria.RangeErrorMessage;
                    REV_TB_creampercent.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_creampercent.ValidationExpression = criteria.RegularExpression;
                    REV_TB_creampercent.Enabled = criteria.IsRegularExpressionRequired;
                    RV_TB_creampercent.ErrorMessage = criteria.RangeErrorMessage;
                    RV_TB_creampercent.MinimumValue = criteria.MinimumValue;
                    RV_TB_creampercent.MaximumValue = criteria.MaximumValue;
                    RV_TB_creampercent.Enabled = criteria.IsRangeRequired;
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
                    break;

                case "packetview":
                    Lbl_FU_ClrApp.Text = criteria.DisplayName;
                    RFV_FU_ClrApp.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_FU_ClrApp.Enabled = criteria.IsRequired;
                    break;
            }
        }

        protected void btnReset_Click(object sender, EventArgs e)
        {
            Response.Redirect("qaqc_inspector_rpt.aspx");
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            try
            {
                var qcData = RetrieveFormData();
                int newQCId = InsertQCDataWithTransaction(qcData);

                if (newQCId <= 0)
                {
                    throw new Exception("QC save failed. Reference ID not generated.");
                }

                //RedirectToPacketWeight(newQCId);
            }
            catch (Exception ex)
            {
                DisplayErrorNotification("Error submitting QC: " + ex.Message);
            }
        }

        public class FieldParseException : Exception
        {
            public string FieldName { get; private set; }
            public string InputValue { get; private set; }

            public FieldParseException(string fieldName, string inputValue, Exception innerException)
                : base($"Parsing failed for field '{fieldName}' with value '{inputValue}'.", innerException)
            {
                FieldName = fieldName;
                InputValue = inputValue;
            }
        }

        private decimal? TryParseDecimalWithContext(string input, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            // Sanitize the input: remove spaces and convert commas to dots
            string sanitizedInput = input.Replace(" ", "").Replace(",", ".").Trim();

            // Declare the variable BEFORE the out parameter (Compatible with older C# versions)
            decimal value;

            // Use InvariantCulture to ensure standard decimal parsing
            if (decimal.TryParse(sanitizedInput,
                                 System.Globalization.NumberStyles.Any,
                                 System.Globalization.CultureInfo.InvariantCulture,
                                 out value))
            {
                return value;
            }

            throw new FieldParseException(fieldName, input, new FormatException("Invalid decimal format"));
        }

        private int? TryParseIntWithContext(string input, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            // Declare the variable BEFORE the out parameter
            int value;

            // Add .Trim() to safely handle inputs like " 10 "
            if (int.TryParse(input.Trim(), out value))
            {
                return value;
            }

            throw new FieldParseException(fieldName, input, new FormatException("Invalid integer format"));
        }

        private int ParseSessionInt(string sessionKey)
        {
            object sessionValue = Session[sessionKey];

            if (sessionValue == null)
            {
                throw new FieldParseException(sessionKey, "NULL", new Exception("Session value is null"));
            }

            // Declare the variable BEFORE the out parameter
            int result;

            // Add .Trim() to safely handle any trailing spaces in the session string
            if (int.TryParse(sessionValue.ToString().Trim(), out result))
            {
                return result;
            }

            throw new FieldParseException(sessionKey, sessionValue.ToString(), new FormatException("Invalid integer in session"));
        }

        private QCInspectorData RetrieveFormData()
        {
            try
            {
                return new QCInspectorData
                {
                    PlantName = DDL_Plant.SelectedValue,
                    Line = DDL_PlantLine.SelectedValue,
                    ProductCategory = DDL_ProductCategory.SelectedValue,
                    ProductBrand = DDL_ProductBrand.SelectedValue,
                    BrandSKU = DDL_BrandSKU.SelectedValue,

                    NumberOfPieces = TryParseIntWithContext(TB_NoOfPcs.Text.Trim(), "NumberOfPieces") ?? 0,
                    VarietyOrLotNo = string.IsNullOrWhiteSpace(TB_VartyPkt.Text) ? null : TB_VartyPkt.Text.Trim(),
                    BakingTime = string.IsNullOrWhiteSpace(TB_BakingTime.Text) ? null : TB_BakingTime.Text.Trim(),
                    BakingTime2 = string.IsNullOrWhiteSpace(TB_BakingTime2.Text) ? null : TB_BakingTime2.Text.Trim(),

                    ColorAppearance = TryParseIntWithContext(RBL_ColorApp.SelectedValue, "ColorAppearance") ?? 0,
                    CommentsForColorAppearance = TXB_ColorApp_Remarks.Text.Trim(),

                    FlavourAndTaste = TryParseIntWithContext(RBL_FlavTst.SelectedValue, "FlavourAndTaste") ?? 0,
                    CommentsForFlavourAndTaste = TXB_RBL_FlavTst_Rmrks.Text.Trim(),

                    DesignImplementation = TryParseIntWithContext(RBL_DesignImp.SelectedValue, "DesignImplementation") ?? 0,
                    CommentsForDesignImplementation = TXB_DesignImp_Remarks.Text.Trim(),

                    TextureBite = TryParseIntWithContext(RBL_TextureBite.SelectedValue, "TextureBite") ?? 0,
                    CommentsForTextureBite = TXB_TextureBite_Remarks.Text.Trim(),

                    ShapeOrSize = string.IsNullOrWhiteSpace(TB_ShapeSize.Text) ? null : TB_ShapeSize.Text.Trim(),
                    CommentsForShapeOrSize = string.Empty,

                    GaugeValue = TryParseDecimalWithContext(TB_Length.Text.Trim(), "GaugeValue") ?? 0,
                    DryWeight = TryParseDecimalWithContext(TB_Breadth.Text.Trim(), "DryWeight") ?? 0,
                    DippedWeight = TryParseDecimalWithContext(TB_Height.Text.Trim(), "DippedWeight") ?? 0,
                    Moisture = TryParseDecimalWithContext(TB_Moisture.Text.Trim(), "Moisture") ?? 0,
                    aWmax = TryParseDecimalWithContext(TB_aWMAX.Text.Trim(), "aW_max") ?? 0,
                    pHvalue = TryParseDecimalWithContext(TB_pHvalue.Text.Trim(), "pH_value") ?? 0,
                    GaugeLength = TryParseDecimalWithContext(TB_GaugeLen.Text.Trim(), "GaugeLength") ?? 0,
                    CommentsGaugeLength = TXB_GaugeLen_Remarks.Text.Trim(),

                    WeightWithoutOil = TryParseDecimalWithContext(TB_wgtwtoil.Text.Trim(), "WeightWithoutOil") ?? 0,
                    WeightWithOil = TryParseDecimalWithContext(TB_wgtwoil.Text.Trim(), "WeightWithOil") ?? 0,
                    OilPercentValue = TryParseDecimalWithContext(TB_oilpercent.Text.Trim(), "OilPercentage") ?? 0,

                    Shell10Weight = TryParseDecimalWithContext(TB_shell10.Text.Trim(), "Shell10Weight") ?? 0,
                    Shell5CreamWeight = TryParseDecimalWithContext(TB_shell5cream.Text.Trim(), "Shell5CreamWeight") ?? 0,
                    CreamPercentage = TryParseDecimalWithContext(TB_creampercent.Text.Trim(), "CreamPercentage") ?? 0,
                    PacketWeight = TryParseDecimalWithContext(TB_PktWgt.Text.Trim(), "PacketWeight") ?? 0,

                    DesignAndImplementation = hdn_img1.Value,
                    ColourAndAppearance = hdn_img2.Value,

                    Remarks = string.IsNullOrWhiteSpace(TXB_Remarks.Text) ? null : TXB_Remarks.Text.Trim(),

                    SubmittedById = ParseSessionInt("CurrentUserId"),
                    SubmittedByPNo = Session["WORKMAN"]?.ToString() ?? string.Empty,
                    Shift = hdn_shiftvalue.Value,
                    FormID = 1,

                    Approver1EmployeeCode = Approver1CodeLabel.Text.Trim(),
                    Approver2EmployeeCode = Approver2CodeLabel.Text.Trim(),
                    DottedLineApproverEmployeeCode = DottedLineApproverCodeLabel.Text.Trim()
                };
            }
            catch (FieldParseException ex)
            {
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("QC Inspector – Field Parsing Error",
                    $@"<p><b>Field Name:</b> {ex.FieldName}</p><p><b>Input Value:</b> {ex.InputValue}</p><p><b>Error:</b> {ex.InnerException.Message}</p>",
                    recipients);
                throw;
            }
        }

        private int InsertQCDataWithTransaction(QCInspectorData qcData)
        {
            using (var transaction = new TransactionScope())
            {
                try
                {
                    var dataAccess = new QCInspectorDataAccess();
                    int qcInspectorId = dataAccess.InsertQCInspectorData(
                        qcData.PlantName, qcData.Line, qcData.ProductCategory, qcData.ProductBrand, qcData.BrandSKU,
                        qcData.NumberOfPieces, qcData.GaugeValue, qcData.DryWeight, qcData.DippedWeight, qcData.VarietyOrLotNo,
                        qcData.BakingTime, qcData.BakingTime2, qcData.ColorAppearance, qcData.CommentsForColorAppearance,
                        qcData.FlavourAndTaste, qcData.CommentsForFlavourAndTaste, qcData.DesignImplementation,
                        qcData.CommentsForDesignImplementation, qcData.TextureBite, qcData.CommentsForTextureBite,
                        qcData.ShapeOrSize, qcData.CommentsForShapeOrSize, qcData.Moisture, qcData.aWmax,
                        qcData.pHvalue, qcData.GaugeLength, qcData.CommentsGaugeLength, qcData.WeightWithoutOil,
                        qcData.WeightWithOil, qcData.OilPercentValue, qcData.PacketWeight,
                        qcData.DesignAndImplementation, qcData.ColourAndAppearance, qcData.Remarks,
                        qcData.SubmittedById, DateTime.Now.Date, DateTime.Now.TimeOfDay,
                        qcData.Shift, qcData.SubmittedByPNo, qcData.FormID,
                        qcData.Approver1EmployeeCode, qcData.Approver2EmployeeCode,
                        qcData.DottedLineApproverEmployeeCode,
                        qcData.Shell10Weight, qcData.Shell5CreamWeight, qcData.CreamPercentage
                    );

                    transaction.Complete();
                    MakeInputsReadOnly();
                    return qcInspectorId;
                }
                catch
                {
                    throw;
                }
            }
        }

        private void DisplayErrorNotification(string errorMessage)
        {
            string errorScript = "<script type='text/javascript'>\n" +
                                 $"new PNotify({{\n" +
                                 "    title: 'Error',\n" +
                                 $"    text: '{errorMessage.Replace("'", "\\'")}',\n" +
                                 "    type: 'error',\n" +
                                 "    styling: 'bootstrap3'\n" +
                                 "}});\n" +
                                 "</script>";
            ClientScript.RegisterStartupScript(this.GetType(), "ShowErrorNotification", errorScript, false);
        }

        public class QCInspectorData
        {
            public string PlantName { get; set; }
            public string Line { get; set; }
            public string ProductCategory { get; set; }
            public string ProductBrand { get; set; }
            public string BrandSKU { get; set; }
            public int NumberOfPieces { get; set; }
            public decimal GaugeValue { get; set; }
            public decimal DryWeight { get; set; }
            public decimal DippedWeight { get; set; }
            public string VarietyOrLotNo { get; set; }
            public string BakingTime { get; set; }
            public string BakingTime2 { get; set; }
            public int ColorAppearance { get; set; }
            public string CommentsForColorAppearance { get; set; }
            public int FlavourAndTaste { get; set; }
            public string CommentsForFlavourAndTaste { get; set; }
            public int DesignImplementation { get; set; }
            public string CommentsForDesignImplementation { get; set; }
            public int TextureBite { get; set; }
            public string CommentsForTextureBite { get; set; }
            public string ShapeOrSize { get; set; }
            public string CommentsForShapeOrSize { get; set; }
            public decimal Moisture { get; set; }
            public decimal aWmax { get; set; }
            public decimal pHvalue { get; set; }
            public decimal GaugeLength { get; set; }
            public string CommentsGaugeLength { get; set; }
            public decimal WeightWithoutOil { get; set; }
            public decimal WeightWithOil { get; set; }
            public decimal OilPercentValue { get; set; }
            public decimal PacketWeight { get; set; }
            public string DesignAndImplementation { get; set; }
            public string ColourAndAppearance { get; set; }
            public string Remarks { get; set; }
            public int SubmittedById { get; set; }
            public string SubmittedByPNo { get; set; }
            public string Shift { get; set; }
            public int FormID { get; set; }
            public string Approver1EmployeeCode { get; set; }
            public string Approver2EmployeeCode { get; set; }
            public string DottedLineApproverEmployeeCode { get; set; }
            public decimal Shell10Weight { get; set; }
            public decimal Shell5CreamWeight { get; set; }
            public decimal CreamPercentage { get; set; }
        }

        protected void DDL_BrandSKU_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Plant.SelectedIndex <= 0 ||
                DDL_PlantLine.SelectedIndex <= 0 ||
                DDL_ProductCategory.SelectedIndex <= 0 ||
                DDL_ProductBrand.SelectedIndex <= 0 ||
                string.IsNullOrEmpty(DDL_BrandSKU.SelectedValue))
            {
                return;
            }

            //CheckPendingStep2ForContext();
        }

        private void CheckPendingStep2ForContext()
        {
            int userId = Convert.ToInt32(Session["CurrentUserId"]);
            string plant = DDL_Plant.SelectedValue;
            string line = DDL_PlantLine.SelectedValue;
            string category = DDL_ProductCategory.SelectedValue;
            string brand = DDL_ProductBrand.SelectedValue;
            string skuId = DDL_BrandSKU.SelectedValue;

            int pendingQCId = GetPendingStep2Id(userId, plant, line, category, brand, skuId);

            if (pendingQCId > 0)
            {
                //RedirectToPacketWeight(pendingQCId);
            }
        }

        private void RedirectToPacketWeight(int qcId)
        {
            //Session["PendingQCContext"] = true;
            //Response.Redirect($"qaqc_packet_weight.aspx?qcid={qcId}", false);
            //Context.ApplicationInstance.CompleteRequest();
        }

        private int GetPendingStep2Id(int userId, string plant, string line, string category, string brand, string skuId)
        {
            using (SqlConnection con = new SqlConnection(cs))
            using (SqlCommand cmd = new SqlCommand("dbo.GetPendingQCStep2Id_ByContext", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@PlantName", SqlDbType.NVarChar, 100).Value = plant;
                cmd.Parameters.Add("@Line", SqlDbType.NVarChar, 100).Value = line;
                cmd.Parameters.Add("@Category", SqlDbType.NVarChar, 100).Value = category;
                cmd.Parameters.Add("@Brand", SqlDbType.NVarChar, 100).Value = brand;
                cmd.Parameters.Add("@SKUId", SqlDbType.VarChar, 50).Value = skuId;

                con.Open();
                object result = cmd.ExecuteScalar();

                return (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;
            }
        }
    }
}