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
    public partial class RM_Class_1 : System.Web.UI.Page
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

                    lbl_docname.Text = "QC - RM Class 1 Report";
                    lbl_docnumber.Text = "ANMOL/DOC/QC/1";
                    MaterialBinder();
                    PopulateColorDropdown();

                    // Call a helper method to make all controls visible
                    //SetControlsVisible(Page.Controls, true);

                    // set validators for fields or columns
                    //SetQuanitytValidators();
                    //SetPhValidators();
                    //SetMoistureValidators();
                    //SetTotalAshValidators();
                    //SetInsolubleAshValidators();
                    //SetDensityValidators();
                    //SetFatValidators();
                    //SetTotalSolidValidators();
                    //SetReducingSugarValidators();
                    //SetDrainableSyrupValidators();
                    //SetSeedValidators();
                    //SetBrixValidators();
                    //SetShapeOrSizeValidators();
                    //SetTSValidators();
                    //SetVehicleNoValidators();
                    //SetLotBatchNoValidators();
                    //SetChallanNoValidators();
                    //SetChallanDateValidators();
                }

            }
        }

        private void DisableReqFields()
        {
            RFV_ColorRemarks.Enabled = false;
            RFV_DDL_Color.Enabled = false;
            //RFV_DDL_Material.Enabled = false;
            //RFV_DDL_Plant.Enabled = false;
            RFV_FU_MaterialImage.Enabled = false;
            RFV_RBL_Appearance.Enabled = false;
            RFV_RBL_AppStatus.Enabled = false;
            RFV_RBL_Smell.Enabled = false;
            RFV_RBL_TasteFlavor.Enabled = false;
            //RFV_TB_BrandName.Enabled = false;
            RFV_TB_Brix.Enabled = false;
            RFV_TB_ChallanDate.Enabled = false;
            //RFV_TB_ChallanNo.Enabled = false;
            RFV_TB_Density.Enabled = false;
            RFV_TB_DrainableSyrup.Enabled = false;
            RFV_TB_FatContent.Enabled = false;
            RFV_TB_Foreign_Impurities.Enabled = false;
            RFV_TB_InsolubleAsh.Enabled = false;
            RFV_TB_LotNo.Enabled = false;
            RFV_TB_Matured_Immatured_Seeds.Enabled = false;
            RFV_TB_Moisture.Enabled = false;
            RFV_TB_PH.Enabled = false;
            RFV_TB_Quantity.Enabled = false;
            RFV_TB_ReducingSugar.Enabled = false;
            RFV_TB_ShapeOrSize.Enabled = false;
            //RFV_TB_Supplier.Enabled = false;
            RFV_TB_TotalAsh.Enabled = false;
            RFV_TB_TotalSolid.Enabled = false;
            RFV_TB_TS.Enabled = false;
            RFV_TB_VehicleNo.Enabled = false;
            RFV_TXB_Appearance_Remarks.Enabled = false;
            RFV_TXB_AppStatus_Remarks.Enabled = false;
            RFV_TXB_Ash_Remarks.Enabled = false;
            RFV_TXB_Brix_Remarks.Enabled = false;
            RFV_TXB_Density_Remarks.Enabled = false;
            RFV_TXB_Fat_Remarks.Enabled = false;
            RFV_TXB_InsolubleAsh_Remarks.Enabled = false;
            RFV_TXB_Moisture_Remarks.Enabled = false;
            RFV_TXB_PH_Remarks.Enabled = false;
            RFV_TXB_Quantity_Remarks.Enabled = false;
            RFV_TXB_Seeds_Remarks.Enabled = false;
            RFV_TXB_ShapeOrSize_Remarks.Enabled = false;
            RFV_TXB_Smell_Remarks.Enabled = false;
            RFV_TXB_Solid_Remarks.Enabled = false;
            RFV_TXB_Sugar_Remarks.Enabled = false;
            RFV_TXB_Syrup_Remarks.Enabled = false;
            RFV_TXB_TasteFlavor_Remarks.Enabled = false;
            RFV_TXB_TS_Remarks.Enabled = false;
        }


        //private void SetQuanitytValidators()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_Quantity.ErrorMessage = "*";
        //    RFV_TB_Quantity.ForeColor = System.Drawing.Color.Red;
        //    RFV_TB_Quantity.Enabled = true;

        //    // Set properties of RegularExpressionValidator
        //    REV_TB_Quantity.ErrorMessage = "Decimal Only";
        //    REV_TB_Quantity.ForeColor = System.Drawing.Color.Red;
        //    REV_TB_Quantity.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

        //    // Set properties of RangeValidator
        //    CV_TB_PH.ErrorMessage = "[0.00 - 2000.00]";
        //    CV_TB_PH.ForeColor = System.Drawing.Color.Red;
        //}
        //private void SetPhValidators()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_PH.ErrorMessage = "*";
        //    RFV_TB_PH.ForeColor = System.Drawing.Color.Red;
        //    RFV_TB_PH.Enabled = true;

        //    // Set properties of RegularExpressionValidator
        //    REV_TB_PH.ErrorMessage = "Decimal Only";
        //    REV_TB_PH.ForeColor = System.Drawing.Color.Red;
        //    REV_TB_PH.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

        //    // Set properties of RangeValidator
        //    CV_TB_PH.ErrorMessage = "[0.00 - 1000.00]";
        //    CV_TB_PH.ForeColor = System.Drawing.Color.Red;
        //}
        //private void SetMoistureValidators()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_Moisture.ErrorMessage = "*";
        //    RFV_TB_Moisture.ForeColor = System.Drawing.Color.Red;
        //    RFV_TB_Moisture.Enabled = true;

        //    // Set properties of RegularExpressionValidator
        //    REV_TB_Moisture.ErrorMessage = "Decimal Only";
        //    REV_TB_Moisture.ForeColor = System.Drawing.Color.Red;
        //    REV_TB_Moisture.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

        //    // Set properties of RangeValidator
        //    CV_TB_Moisture.ErrorMessage = "[0.00 - 1000.00]";
        //    CV_TB_Moisture.ForeColor = System.Drawing.Color.Red;
        //}
        //private void SetTotalAshValidators()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_TotalAsh.ErrorMessage = "*";
        //    RFV_TB_TotalAsh.ForeColor = System.Drawing.Color.Red;
        //    RFV_TB_TotalAsh.Enabled = true;

        //    // Set properties of RegularExpressionValidator
        //    REV_TB_TotalAsh.ErrorMessage = "Decimal Only";
        //    REV_TB_TotalAsh.ForeColor = System.Drawing.Color.Red;
        //    REV_TB_TotalAsh.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

        //    // Set properties of RangeValidator
        //    CV_TB_TotalAsh.ErrorMessage = "[0.00 - 1000.00]";
        //    CV_TB_TotalAsh.ForeColor = System.Drawing.Color.Red;
        //}
        //private void SetInsolubleAshValidators()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_InsolubleAsh.ErrorMessage = "*";
        //    RFV_TB_InsolubleAsh.ForeColor = System.Drawing.Color.Red;
        //    RFV_TB_InsolubleAsh.Enabled = true;

        //    // Set properties of RegularExpressionValidator
        //    REV_TB_InsolubleAsh.ErrorMessage = "Decimal Only";
        //    REV_TB_InsolubleAsh.ForeColor = System.Drawing.Color.Red;
        //    REV_TB_InsolubleAsh.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

        //    // Set properties of RangeValidator
        //    CV_TB_InsolubleAsh.ErrorMessage = "[0.00 - 1000.00]";
        //    CV_TB_InsolubleAsh.ForeColor = System.Drawing.Color.Red;
        //}
        //private void SetDensityValidators()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_Density.ErrorMessage = "*";
        //    RFV_TB_Density.ForeColor = System.Drawing.Color.Red;
        //    RFV_TB_Density.Enabled = true;

        //    // Set properties of RegularExpressionValidator
        //    REV_TB_Density.ErrorMessage = "Decimal Only";
        //    REV_TB_Density.ForeColor = System.Drawing.Color.Red;
        //    REV_TB_Density.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

        //    // Set properties of RangeValidator
        //    CV_TB_Density.ErrorMessage = "[0.00 - 1000.00]";
        //    CV_TB_Density.ForeColor = System.Drawing.Color.Red;
        //}
        //private void SetFatValidators()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_FatContent.ErrorMessage = "*";
        //    RFV_TB_FatContent.ForeColor = System.Drawing.Color.Red;
        //    RFV_TB_FatContent.Enabled = true;

        //    // Set properties of RegularExpressionValidator
        //    REV_TB_FatContent.ErrorMessage = "Decimal Only";
        //    REV_TB_FatContent.ForeColor = System.Drawing.Color.Red;
        //    REV_TB_FatContent.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

        //    // Set properties of RangeValidator
        //    CV_TB_FatContent.ErrorMessage = "[0.00 - 1000.00]";
        //    CV_TB_FatContent.ForeColor = System.Drawing.Color.Red;
        //}
        //private void SetTotalSolidValidators()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_TotalSolid.ErrorMessage = "*";
        //    RFV_TB_TotalSolid.ForeColor = System.Drawing.Color.Red;
        //    RFV_TB_TotalSolid.Enabled = true;

        //    // Set properties of RegularExpressionValidator
        //    REV_TB_TotalSolid.ErrorMessage = "Decimal Only";
        //    REV_TB_TotalSolid.ForeColor = System.Drawing.Color.Red;
        //    REV_TB_TotalSolid.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

        //    // Set properties of RangeValidator
        //    CV_TB_TotalSolid.ErrorMessage = "[0.00 - 1000.00]";
        //    CV_TB_TotalSolid.ForeColor = System.Drawing.Color.Red;
        //}
        //private void SetReducingSugarValidators()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_ReducingSugar.ErrorMessage = "*";
        //    RFV_TB_ReducingSugar.ForeColor = System.Drawing.Color.Red;
        //    RFV_TB_ReducingSugar.Enabled = true;

        //    // Set properties of RegularExpressionValidator
        //    REV_TB_ReducingSugar.ErrorMessage = "Decimal Only";
        //    REV_TB_ReducingSugar.ForeColor = System.Drawing.Color.Red;
        //    REV_TB_ReducingSugar.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

        //    // Set properties of RangeValidator
        //    CV_TB_ReducingSugar.ErrorMessage = "[0.00 - 1000.00]";
        //    CV_TB_ReducingSugar.ForeColor = System.Drawing.Color.Red;
        //}
        //private void SetDrainableSyrupValidators()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_DrainableSyrup.ErrorMessage = "*";
        //    RFV_TB_DrainableSyrup.ForeColor = System.Drawing.Color.Red;
        //    RFV_TB_DrainableSyrup.Enabled = true;

        //    // Set properties of RegularExpressionValidator
        //    REV_TB_DrainableSyrup.ErrorMessage = "Decimal Only";
        //    REV_TB_DrainableSyrup.ForeColor = System.Drawing.Color.Red;
        //    REV_TB_DrainableSyrup.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

        //    // Set properties of RangeValidator
        //    CV_TB_ReducingSugar.ErrorMessage = "[0.00 - 1000.00]";
        //    CV_TB_ReducingSugar.ForeColor = System.Drawing.Color.Red;
        //}
        //private void SetSeedValidators()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_Matured_Immatured_Seeds.ErrorMessage = "*";
        //    RFV_TB_Matured_Immatured_Seeds.ForeColor = System.Drawing.Color.Red;
        //    RFV_TB_Matured_Immatured_Seeds.Enabled = true;

        //    // Set properties of RegularExpressionValidator
        //    REV_TB_Matured_Immatured_Seeds.ErrorMessage = "Integer Only";
        //    REV_TB_Matured_Immatured_Seeds.ForeColor = System.Drawing.Color.Red;
        //    REV_TB_Matured_Immatured_Seeds.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

        //    // Set properties of RangeValidator
        //    CV_TB_Matured_Immatured_Seeds.ErrorMessage = "[0.00 - 1000.00]";
        //    CV_TB_Matured_Immatured_Seeds.ForeColor = System.Drawing.Color.Red;
        //}
        //private void SetBrixValidators()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_Brix.ErrorMessage = "*";
        //    RFV_TB_Brix.ForeColor = System.Drawing.Color.Red;
        //    RFV_TB_Brix.Enabled = true;

        //    // Set properties of RegularExpressionValidator
        //    REV_TB_Brix.ErrorMessage = "Decimal Only";
        //    REV_TB_Brix.ForeColor = System.Drawing.Color.Red;
        //    REV_TB_Brix.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

        //    // Set properties of RangeValidator
        //    CV_TB_Brix.ErrorMessage = "[0.00 - 1000.00]";
        //    CV_TB_Brix.ForeColor = System.Drawing.Color.Red;
        //}
        //private void SetShapeOrSizeValidators()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_ShapeOrSize.ErrorMessage = "*";
        //    RFV_TB_ShapeOrSize.ForeColor = System.Drawing.Color.Red;
        //    RFV_TB_ShapeOrSize.Enabled = true;

        //    // Set properties of RegularExpressionValidator
        //    REV_TB_ShapeOrSize.ErrorMessage = "Decimal Only";
        //    REV_TB_ShapeOrSize.ForeColor = System.Drawing.Color.Red;
        //    REV_TB_ShapeOrSize.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

        //    // Set properties of RangeValidator
        //    CV_TB_ShapeOrSize.ErrorMessage = "[0.00 - 1000.00]";
        //    CV_TB_ShapeOrSize.ForeColor = System.Drawing.Color.Red;
        //}
        //private void SetTSValidators()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_TS.ErrorMessage = "*";
        //    RFV_TB_TS.ForeColor = System.Drawing.Color.Red;
        //    RFV_TB_TS.Enabled = true;

        //    // Set properties of RegularExpressionValidator
        //    REV_TB_TS.ErrorMessage = "Decimal Only";
        //    REV_TB_TS.ForeColor = System.Drawing.Color.Red;
        //    REV_TB_TS.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

        //    // Set properties of RangeValidator
        //    CV_TB_TS.ErrorMessage = "[0.00 - 1000.00]";
        //    CV_TB_ShapeOrSize.ForeColor = System.Drawing.Color.Red;
        //}
        //private void SetVehicleNoValidators()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_VehicleNo.ErrorMessage = "*";
        //    RFV_TB_VehicleNo.ForeColor = System.Drawing.Color.Red;
        //    RFV_TB_VehicleNo.Enabled = true;

        //    // Set properties of RegularExpressionValidator
        //    REV_TB_VehicleNo.ErrorMessage = "Alphanumeric Only";
        //    REV_TB_VehicleNo.ForeColor = System.Drawing.Color.Red;
        //    REV_TB_VehicleNo.ValidationExpression = "^[a-zA-Z0-9]*$"; // Regular expression for alphanumeric input
        //}
        //private void SetLotBatchNoValidators()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_LotNo.ErrorMessage = "*";
        //    RFV_TB_LotNo.ForeColor = System.Drawing.Color.Red;
        //    RFV_TB_LotNo.Enabled = true;

        //    // Set properties of RegularExpressionValidator
        //    REV_TB_LotNo.ErrorMessage = "Alphanumeric Only";
        //    REV_TB_LotNo.ForeColor = System.Drawing.Color.Red;
        //    REV_TB_LotNo.ValidationExpression = "^[a-zA-Z0-9]*$"; // Regular expression for alphanumeric input
        //}
        //private void SetChallanNoValidators()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_ChallanNo.ErrorMessage = "*";
        //    RFV_TB_ChallanNo.ForeColor = System.Drawing.Color.Red;
        //    RFV_TB_ChallanNo.Enabled = true;

        //    // Set properties of RegularExpressionValidator
        //    REV_TB_ChallanNo.ErrorMessage = "Alphanumeric Only";
        //    REV_TB_ChallanNo.ForeColor = System.Drawing.Color.Red;
        //    REV_TB_ChallanNo.ValidationExpression = "^[a-zA-Z0-9]*$"; // Regular expression for alphanumeric input
        //}
        //private void SetChallanDateValidators()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_ChallanDate.ErrorMessage = "*";
        //    RFV_TB_ChallanDate.ForeColor = System.Drawing.Color.Red;
        //    RFV_TB_ChallanDate.Enabled = true;
        //}



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
                    int result = InsertQCClass1Rpt_BrandFieldsControl(
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



        public int InsertQCClass1Rpt_BrandFieldsControl(int materialId, string materialName, int plantId, string plantName)
        {
            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.InsertQCRMClass1Rpt_BrandFieldsControl", conn))
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
                    cmd.Parameters.AddWithValue("@FormID", 15);
                    cmd.Parameters.AddWithValue("@FormName", "RM_Class_1");

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        hdn_formid.Value = "15";
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
                            bool isInserted = dbcl.InsertDefaultApprovers(selectedPlantValue, selectedPlantLineValue, 15);

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
                    string query = "SELECT ISNULL(MAX(CAST(SUBSTRING(RMFID, 4, LEN(RMFID)) AS INT)), 0) FROM TRN_RM_CLASS_1";
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
            int formID = Convert.ToInt32(hdn_formid.Value.ToString());

            string materialName = DDL_Material.SelectedValue;
            string plantName = DDL_Plant.SelectedValue;

            //string line = DDL_PlantLine.SelectedValue;
            //string productCategory = string.IsNullOrEmpty(DDL_ProductCategory.SelectedValue) ? null : DDL_ProductCategory.Text;
            //string productBrand = string.IsNullOrEmpty(DDL_ProductBrand.SelectedValue) ? null : DDL_ProductBrand.Text;


            //string line = string.Empty;
            //string productCategory = string.Empty;
            string Brand = string.IsNullOrEmpty(TB_BrandName.Text) ? null : TB_BrandName.Text;

            string supplier = string.IsNullOrEmpty(TB_Supplier.Text) ? null : TB_Supplier.Text;
            string challanNo = string.IsNullOrEmpty(TB_ChallanNo.Text) ? null : TB_ChallanNo.Text;
            DateTime? challanDate = string.IsNullOrEmpty(TB_ChallanDate.Text) ? (DateTime?)null : DateTime.Parse(TB_ChallanDate.Text).Date;
            string lotNo = string.IsNullOrEmpty(TB_LotNo.Text) ? null : TB_LotNo.Text;
            string vehicleNo = string.IsNullOrEmpty(TB_VehicleNo.Text) ? null : TB_VehicleNo.Text;

            decimal? size = !string.IsNullOrWhiteSpace(TB_Size.Text) ? Convert.ToDecimal(TB_Size.Text) : (decimal?)null;

            decimal? quantity = !string.IsNullOrWhiteSpace(TB_Quantity.Text) ? Convert.ToDecimal(TB_Quantity.Text) : (decimal?)null;
            string quantityRemarks = string.IsNullOrEmpty(TXB_Quantity_Remarks.Text) ? null : TXB_Quantity_Remarks.Text;

            string color = string.IsNullOrEmpty(DDL_Color.SelectedItem.Text) ? null : DDL_Color.Text;
            string colorRemarks = string.IsNullOrEmpty(TXB_Color_Remarks.Text) ? null : TXB_Color_Remarks.Text;

            int? smell = string.IsNullOrEmpty(RBL_Smell.SelectedValue) ? (int?)null : (int?)Convert.ToInt32(RBL_Smell.SelectedValue);
            string smellRemarks = string.IsNullOrEmpty(TXB_Smell_Remarks.Text) ? null : TXB_Smell_Remarks.Text;

            int? appearance = string.IsNullOrEmpty(RBL_Appearance.SelectedValue) ? (int?)null : (int?)Convert.ToInt32(RBL_Appearance.SelectedValue);
            string appearanceRemarks = string.IsNullOrEmpty(TXB_Appearance_Remarks.Text) ? null : TXB_Appearance_Remarks.Text;

            int? taste = string.IsNullOrEmpty(RBL_TasteFlavor.SelectedValue) ? (int?)null : (int?)Convert.ToInt32(RBL_TasteFlavor.SelectedValue);
            string tasteRemarks = string.IsNullOrEmpty(TXB_TasteFlavor_Remarks.Text) ? null : TXB_TasteFlavor_Remarks.Text;

            string foreignImpurites = string.IsNullOrEmpty(TB_Foreign_Impurities.Text) ? null : TB_Foreign_Impurities.Text;

            decimal? ph = !string.IsNullOrWhiteSpace(TB_PH.Text) ? Convert.ToDecimal(TB_PH.Text) : (decimal?)null;
            string phRemarks = string.IsNullOrEmpty(TXB_PH_Remarks.Text) ? null : TXB_PH_Remarks.Text;

            decimal? moisture = !string.IsNullOrWhiteSpace(TB_Moisture.Text) ? Convert.ToDecimal(TB_Moisture.Text) : (decimal?)null;
            string moistureRemarks = string.IsNullOrEmpty(TXB_PH_Remarks.Text) ? null : TXB_PH_Remarks.Text;

            decimal? protein = !string.IsNullOrWhiteSpace(TB_Protein.Text) ? Convert.ToDecimal(TB_Protein.Text) : (decimal?)null;
            string proteinRemarks = string.IsNullOrEmpty(TXB_Protein_Remarks.Text) ? null : TXB_Protein_Remarks.Text;

            decimal? ash = !string.IsNullOrWhiteSpace(TB_TotalAsh.Text) ? Convert.ToDecimal(TB_TotalAsh.Text) : (decimal?)null;
            string ashRemarks = string.IsNullOrEmpty(TXB_Ash_Remarks.Text) ? null : TXB_Ash_Remarks.Text;

            decimal? insolubleAsh = !string.IsNullOrWhiteSpace(TB_InsolubleAsh.Text) ? Convert.ToDecimal(TB_InsolubleAsh.Text) : (decimal?)null;
            string insolubleAshRemarks = string.IsNullOrEmpty(TXB_InsolubleAsh_Remarks.Text) ? null : TXB_InsolubleAsh_Remarks.Text;

            decimal? density = !string.IsNullOrWhiteSpace(TB_Density.Text) ? Convert.ToDecimal(TB_Density.Text) : (decimal?)null;
            string densityRemarks = string.IsNullOrEmpty(TXB_Density_Remarks.Text) ? null : TXB_Density_Remarks.Text;

            decimal? fat = !string.IsNullOrWhiteSpace(TB_FatContent.Text) ? Convert.ToDecimal(TB_FatContent.Text) : (decimal?)null;
            string fatRemarks = string.IsNullOrEmpty(TXB_Fat_Remarks.Text) ? null : TXB_Fat_Remarks.Text;

            decimal? solid = !string.IsNullOrWhiteSpace(TB_TotalSolid.Text) ? Convert.ToDecimal(TB_TotalSolid.Text) : (decimal?)null;
            string solidRemarks = string.IsNullOrEmpty(TXB_Solid_Remarks.Text) ? null : TXB_Solid_Remarks.Text;

            decimal? sugar = !string.IsNullOrWhiteSpace(TB_ReducingSugar.Text) ? Convert.ToDecimal(TB_ReducingSugar.Text) : (decimal?)null;
            string sugarRemarks = string.IsNullOrEmpty(TXB_Sugar_Remarks.Text) ? null : TXB_Sugar_Remarks.Text;

            decimal? syrup = !string.IsNullOrWhiteSpace(TB_DrainableSyrup.Text) ? Convert.ToDecimal(TB_DrainableSyrup.Text) : (decimal?)null;
            string syrupRemarks = string.IsNullOrEmpty(TXB_Syrup_Remarks.Text) ? null : TXB_Syrup_Remarks.Text;

            int? seed = string.IsNullOrEmpty(TB_Matured_Immatured_Seeds.Text) ? (int?)null : Convert.ToInt32(TB_Matured_Immatured_Seeds.Text);
            string seedRemarks = string.IsNullOrEmpty(TXB_Seeds_Remarks.Text) ? null : TXB_Seeds_Remarks.Text;

            decimal? brix = !string.IsNullOrWhiteSpace(TB_Brix.Text) ? Convert.ToDecimal(TB_Brix.Text) : (decimal?)null;
            string brixRemarks = string.IsNullOrEmpty(TXB_Brix_Remarks.Text) ? null : TXB_Brix_Remarks.Text;

            decimal? shapeOrSize = !string.IsNullOrWhiteSpace(TB_ShapeOrSize.Text) ? Convert.ToDecimal(TB_ShapeOrSize.Text) : (decimal?)null;
            string shapeOrSizeRemarks = string.IsNullOrEmpty(TXB_ShapeOrSize_Remarks.Text) ? null : TXB_ShapeOrSize_Remarks.Text;

            decimal? ts = !string.IsNullOrWhiteSpace(TB_TS.Text) ? Convert.ToDecimal(TB_TS.Text) : (decimal?)null;
            string tsRemarks = string.IsNullOrEmpty(TXB_TS_Remarks.Text) ? null : TXB_TS_Remarks.Text;

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
                    using (SqlCommand command = new SqlCommand("SP_RM_CLASS_1", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Add parameters
                        command.Parameters.AddWithValue("@RMFID", rmfId);
                        command.Parameters.AddWithValue("@FormID", formID);

                        command.Parameters.AddWithValue("@MaterialName", materialName);
                        command.Parameters.AddWithValue("@PlantName", plantName);
                        //command.Parameters.AddWithValue("@Line", line);
                        //command.Parameters.AddWithValue("@ProductCategory", (object)productCategory ?? DBNull.Value);
                        //command.Parameters.AddWithValue("@ProductBrand", productBrand);
                        command.Parameters.AddWithValue("@Brand", Brand);

                        command.Parameters.AddWithValue("@Supplier_Name", (object)supplier ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Challan_No", (object)challanNo ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Challan_Date", (object)challanDate ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Lot_No", (object)lotNo ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Vehicle_No", (object)vehicleNo ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Size", (object)size ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Quantity", (object)quantity ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForQuantity", (object)quantityRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Color", (object)color ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForColor", (object)colorRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Smell", (object)smell ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForSmell", (object)smellRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Appearance", (object)appearance ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForAppearance", (object)appearanceRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Taste", (object)taste ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForTaste", (object)tasteRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Foreign_Matter_Impurities", (object)foreignImpurites ?? DBNull.Value);

                        command.Parameters.AddWithValue("@PH", (object)ph ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForPH", (object)phRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Moisture", (object)moisture ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForMoisture", (object)moistureRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Protein", (object)protein ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForProtein", (object)proteinRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Total_Ash", (object)ash ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForAsh", (object)ashRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Acid_Insoluble_Ash", (object)insolubleAsh ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForInsolubleAsh", (object)insolubleAshRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Density", (object)density ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForDensity", (object)densityRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Fat_Content", (object)fat ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForFat", (object)fatRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Total_Solids", (object)solid ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForSolid", (object)solidRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Reducing_Sugar", (object)sugar ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForSugar", (object)sugarRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Drainable_Syrup", (object)syrup ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForSyrup", (object)syrupRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Matured_Immatured_seeds", (object)seed ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForSeed", (object)seedRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Brix", (object)brix ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForBrix", (object)brixRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@ShapeOrSize", (object)shapeOrSize ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForShapeOrSize", (object)shapeOrSizeRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@TS", (object)ts ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForTS", (object)tsRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@ApprovalStatus", (object)accepted ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Remarks", (object)acceptedRemarks ?? DBNull.Value);

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

            //DDL_PlantLine.Enabled = false;
            //DDL_ProductCategory.Enabled = false;
            //DDL_ProductBrand.Enabled = false;

            TB_Supplier.ReadOnly = true;
            TB_ChallanNo.ReadOnly = true;
            TB_ChallanDate.ReadOnly = true;
            TB_LotNo.ReadOnly = true;
            TB_VehicleNo.ReadOnly = true;

            TB_Size.ReadOnly = true;

            TB_Quantity.ReadOnly = true;
            TXB_Quantity_Remarks.ReadOnly = true;
            
            RBL_Smell.Enabled = false;
            TXB_Smell_Remarks.ReadOnly = true;

            RBL_Appearance.Enabled = false;
            TXB_Appearance_Remarks.ReadOnly = true;

            DDL_Color.Enabled = false;
            TXB_Color_Remarks.ReadOnly = true;

            RBL_TasteFlavor.Enabled = false;
            TXB_TasteFlavor_Remarks.ReadOnly = true;

            TB_Foreign_Impurities.ReadOnly = true;

            TB_PH.ReadOnly = true;
            TXB_PH_Remarks.ReadOnly = true;

            TB_Moisture.ReadOnly = true;
            TXB_Moisture_Remarks.ReadOnly = true;

            TB_Protein.ReadOnly = true;
            TXB_Protein_Remarks.ReadOnly = true;

            TB_TotalAsh.ReadOnly = true;
            TXB_Ash_Remarks.ReadOnly = true;

            TB_InsolubleAsh.ReadOnly = true;
            TXB_InsolubleAsh_Remarks.ReadOnly = true;

            TB_Density.ReadOnly = true;
            TXB_Density_Remarks.ReadOnly = true;

            TB_FatContent.ReadOnly = true;
            TXB_Fat_Remarks.ReadOnly = true;

            TB_TotalSolid.ReadOnly = true;
            TXB_Solid_Remarks.ReadOnly = true;

            TB_ReducingSugar.ReadOnly = true;
            TXB_Sugar_Remarks.ReadOnly = true;

            TB_DrainableSyrup.ReadOnly = true;
            TXB_Syrup_Remarks.ReadOnly = true;

            TB_Matured_Immatured_Seeds.ReadOnly = true;
            TXB_Seeds_Remarks.ReadOnly = true;

            TB_Brix.ReadOnly = true;
            TXB_Brix_Remarks.ReadOnly = true;

            TB_ShapeOrSize.ReadOnly = true;
            TXB_ShapeOrSize_Remarks.ReadOnly = true;

            TB_TS.ReadOnly = true;
            TXB_TS_Remarks.ReadOnly = true;

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
            Response.Redirect("RM_Class_1.aspx");
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
                ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantInvalidErrorNotification", DDL_Color_Error_script, false);


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


        private void RemoveReqField()
        {

        }



        //private void SetControlsVisible(ControlCollection controls, bool visible)
        //{
        //    foreach (Control ctrl in controls)
        //    {
        //        // Only hide/show divs that are not related to the Plant dropdown
        //        if (ctrl is HtmlGenericControl && (ctrl as HtmlGenericControl).TagName == "div")
        //        {
        //            // Assuming you have named your divs properly or added IDs to them (e.g., divSupplierSection, divOtherSection, etc.)
        //            if (ctrl.ID != "PlantDIV" && ctrl.ID != "MaterialDIV")
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

        //    SetControlsVisible(Page.Controls, false);
        //    FU_MaterialImage_Upldr.Visible = false;
        //    FU_MaterialImage.Visible = false;

        //    // Show relevant controls based on the selected material
        //    switch (selectedMaterial)
        //    {
        //        case "Choco Chips":

        //            //Product Brand
        //            BrandDIV.Visible = true;

        //            // Supplier
        //            SupplierDIV.Visible = true;

        //            //Challan No & Date	
        //            ChallanNoDIV.Visible = true;
        //            ChallanDateDIV.Visible = true;

        //            //Lot No/ Batch No.
        //            LotNoDIV.Visible = true;

        //            //Vehicle No	
        //            VehicleNoDIV.Visible = true;

        //            //Colour	
        //            ColorDIV.Visible = true;
        //            ColorRemarksDiv.Visible = true;

        //            //Odour/Smell	
        //            SmellDIV.Visible = true;
        //            //SmellRemarksDiv.Visible = true;

        //            //Taste/Flavour	
        //            TasteFlavorDIV.Visible = true;
        //            //TasteFlavorRemarksDiv.Visible = true;

        //            //Moisture		
        //            MoistureDIV.Visible = true;
        //            //MoistureRemarksDIV.Visible = true;

        //            //Fat Content
        //            FatContentDIV.Visible = true;
        //            //FatContentRemarksDIV.Visible = true;

        //            //Image
        //            FU_MaterialImage_Upldr.Visible = true;
        //            FU_MaterialImage.Visible = true;
        //            break;

        //        case "Malt":
        //            //Supplier	
        //            SupplierDIV.Visible = true;

        //            //Product Brand
        //            BrandDIV.Visible = true;

        //            //Challan No & Date	
        //            ChallanNoDIV.Visible = true;
        //            ChallanDateDIV.Visible = true;

        //            //Lot No/ Batch No.
        //            LotNoDIV.Visible = true;

        //            //Vehicle No	
        //            VehicleNoDIV.Visible = true;

        //            //Colour	
        //            ColorDIV.Visible = true;
        //            ColorRemarksDiv.Visible = true;

        //            //Appearance	
        //            AppearanceDIV.Visible = true;
        //            //AppearanceRemarksDiv.Visible = true;

        //            //Taste/Flavour	
        //            TasteFlavorDIV.Visible = true;
        //            //TasteFlavorRemarksDiv.Visible = true;

        //            //Foreign Matter/Impurities
        //            ImpuritiesDIV.Visible = true;

        //            //pH
        //            PHDIV.Visible = true;
        //            //PHRemarksDIV.Visible = true;

        //            //Density		
        //            DensityDIV.Visible = true;
        //            //DensityRemarksDIV.Visible = true;

        //            //Total Solids	
        //            SolidDIV.Visible = true;
        //            //SolidRemarksDIV.Visible = true;

        //            //Reducing Sugar as Maltose	
        //            SugarDIV.Visible = true;
        //            //SugarRemarksDIV.Visible = true;

        //            //Image
        //            FU_MaterialImage_Upldr.Visible = true;
        //            FU_MaterialImage.Visible = true;
        //            break;

        //        case "Cocoa Powder":

        //            //Product Category	
        //            CategoryDIV.Visible = true;

        //            // Supplier
        //            SupplierDIV.Visible = true;

        //            //Challan No & Date	
        //            ChallanDateDIV.Visible = true;
        //            ChallanNoDIV.Visible = true;

        //            //Vehicle No	
        //            VehicleNoDIV.Visible = true;

        //            //Colour	
        //            ColorDIV.Visible = true;
        //            ColorRemarksDiv.Visible = true;

        //            //Taste/Flavour	
        //            TasteFlavorDIV.Visible = true;
        //            //TasteFlavorRemarksDiv.Visible = true;

        //            //pH
        //            PHDIV.Visible = true;
        //            //PHRemarksDIV.Visible = true;

        //            //Moisture		
        //            MoistureDIV.Visible = true;
        //            //MoistureRemarksDIV.Visible = true;

        //            //Total Ash			
        //            AshDIV.Visible = true;
        //            //AshRemarksDIV.Visible = true;

        //            //Acid Insoluble Ash
        //            InsolubleAshDIV.Visible = true;
        //            //InsolubleAshRemarksDIV.Visible = true;

        //            //Image
        //            FU_MaterialImage_Upldr.Visible = true;
        //            FU_MaterialImage.Visible = true;
        //            break;

        //        case "Desiccated Coconut":

        //            // Supplier
        //            SupplierDIV.Visible = true;

        //            //Colour	
        //            ColorDIV.Visible = true;
        //            ColorRemarksDiv.Visible = true;

        //            //Taste/Flavour	
        //            TasteFlavorDIV.Visible = true;
        //            //TasteFlavorRemarksDiv.Visible = true;

        //            //pH
        //            PHDIV.Visible = true;
        //            //PHRemarksDIV.Visible = true;

        //            //Moisture		
        //            MoistureDIV.Visible = true;
        //            //MoistureRemarksDIV.Visible = true;

        //            //Total Ash			
        //            AshDIV.Visible = true;
        //            //AshRemarksDIV.Visible = true;

        //            //Image
        //            FU_MaterialImage_Upldr.Visible = true;
        //            FU_MaterialImage.Visible = true;
        //            break;

        //        case "Egg Powder":

        //            // Supplier
        //            SupplierDIV.Visible = true;

        //            //Challan No & Date	
        //            ChallanDateDIV.Visible = true;
        //            ChallanNoDIV.Visible = true;

        //            //QTY	
        //            QuantityDIV.Visible = true;

        //            //Vehicle No	
        //            VehicleNoDIV.Visible = true;

        //            //Colour	
        //            ColorDIV.Visible = true;
        //            ColorRemarksDiv.Visible = true;

        //            //Odour/Smell	
        //            SmellDIV.Visible = true;
        //            //SmellRemarksDiv.Visible = true;

        //            //Taste/Flavour	
        //            TasteFlavorDIV.Visible = true;
        //            //TasteFlavorRemarksDiv.Visible = true;

        //            //Foreign Matter/Impurities
        //            ImpuritiesDIV.Visible = true;

        //            //pH
        //            PHDIV.Visible = true;
        //            //PHRemarksDIV.Visible = true;

        //            //Total Ash			
        //            AshDIV.Visible = true;
        //            //AshRemarksDIV.Visible = true;

        //            //Acid Insoluble Ash
        //            InsolubleAshDIV.Visible = true;
        //            //InsolubleAshRemarksDIV.Visible = true;

        //            //TS
        //            TSDIV.Visible = true;
        //            //TSRemarksDIV.Visible = true;

        //            //Image
        //            FU_MaterialImage_Upldr.Visible = true;
        //            FU_MaterialImage.Visible = true;
        //            break;

        //        case "Besan":
        //            // Supplier
        //            SupplierDIV.Visible = true;

        //            //Product Brand
        //            BrandDIV.Visible = true;

        //            //Challan No & Date	
        //            ChallanDateDIV.Visible = true;
        //            ChallanNoDIV.Visible = true;

        //            //Lot No/ Batch No.
        //            LotNoDIV.Visible = true;

        //            //Vehicle No	
        //            VehicleNoDIV.Visible = true;

        //            //Appearance	
        //            AppearanceDIV.Visible = true;
        //            //AppearanceRemarksDiv.Visible = true;

        //            //Taste/Flavour	
        //            TasteFlavorDIV.Visible = true;
        //            //TasteFlavorRemarksDiv.Visible = true;

        //            //Foreign Matter/Impurities
        //            ImpuritiesDIV.Visible = true;

        //            //Moisture		
        //            MoistureDIV.Visible = true;
        //            //MoistureRemarksDIV.Visible = true;

        //            //Total Ash			
        //            AshDIV.Visible = true;
        //            //AshRemarksDIV.Visible = true;

        //            //Acid Insoluble Ash
        //            InsolubleAshDIV.Visible = true;
        //            //InsolubleAshRemarksDIV.Visible = true;

        //            //Image
        //            FU_MaterialImage_Upldr.Visible = true;
        //            FU_MaterialImage.Visible = true;
        //            break;

        //        case "Cashew Nut":

        //            // Supplier
        //            SupplierDIV.Visible = true;

        //            //Product Brand
        //            BrandDIV.Visible = true;

        //            //Challan No & Date	
        //            ChallanNoDIV.Visible = true;
        //            ChallanDateDIV.Visible = true;

        //            //Lot No/ Batch No.
        //            LotNoDIV.Visible = true;

        //            //Vehicle No	
        //            VehicleNoDIV.Visible = true;

        //            //Colour	
        //            ColorDIV.Visible = true;
        //            ColorRemarksDiv.Visible = true;

        //            //Odour/Smell	
        //            SmellDIV.Visible = true;
        //            //SmellRemarksDiv.Visible = true;

        //            //Taste/Flavour	
        //            TasteFlavorDIV.Visible = true;
        //            //TasteFlavorRemarksDiv.Visible = true;

        //            //Foreign Matter/Impurities
        //            ImpuritiesDIV.Visible = true;

        //            //Image
        //            FU_MaterialImage_Upldr.Visible = true;
        //            FU_MaterialImage.Visible = true;
        //            break;

        //        case "Tutty Frutti":
        //            // Supplier
        //            SupplierDIV.Visible = true;

        //            //Challan No & Date	
        //            ChallanNoDIV.Visible = true;
        //            ChallanDateDIV.Visible = true;

        //            //QTY	
        //            QuantityDIV.Visible = true;

        //            //Vehicle No	
        //            VehicleNoDIV.Visible = true;

        //            //Colour	
        //            ColorDIV.Visible = true;
        //            ColorRemarksDiv.Visible = true;

        //            //Odour/Smell	
        //            SmellDIV.Visible = true;
        //            //SmellRemarksDiv.Visible = true;

        //            //Taste/Flavour	
        //            TasteFlavorDIV.Visible = true;
        //            //TasteFlavorRemarksDiv.Visible = true;

        //            //Foreign Matter/Impurities
        //            ImpuritiesDIV.Visible = true;

        //            //Moisture		
        //            MoistureDIV.Visible = true;
        //            //MoistureRemarksDIV.Visible = true;

        //            //Drainable Syrup	
        //            SyrupDIV.Visible = true;
        //            //SyrupRemarksDIV.Visible = true;

        //            //No of Matured/Immatured seeds	
        //            SeedsDIV.Visible = true;
        //            //SeedsRemarksDIV.Visible = true;

        //            //Brix	
        //            BrixDIV.Visible = true;
        //            //BrixRemarksDIV.Visible = true;

        //            //Shape/Size 
        //            ShapeOrSizeDIV.Visible = true;
        //            //ShapeOrSizeRemarksDIV.Visible = true;

        //            //Image
        //            FU_MaterialImage_Upldr.Visible = true;
        //            FU_MaterialImage.Visible = true;

        //            break;

        //        default:
        //            // Optionally handle a default case
        //            break;
        //    }
        //}

    }
}
