using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class RM_Class_4 : System.Web.UI.Page
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


                    lbl_docname.Text = "QC - RM Class 4 Report";
                    lbl_docnumber.Text = "ANMOL/DOC/QC/4";
                    MaterialBinder();
                    PopulateColorDropdown();

                    // Call a helper method to make all controls visible
                    //SetControlsVisible(Page.Controls, true);

                    // set validators for fields or columns
                    //SetQuantityValidators();
                    //SetPhValidators();
                    //SetMoistureValidators();
                    //SetTotalAshValidators();
                    //SetFatContentValidators();
                    //SetMilkValidators();
                    //SetTotalSolidValidators();
                    //SetLactoseValidators();
                    //SetProteinValidators();
                    //SetMilkFatValidators();
                    //SetTitrableAcidityValidators();
                    //SetSO2Validators();
                    //SetGlucoseContentValidators();
                    //SetLossOnDryingValidators();;
                    //SetVehicleNoValidators();
                    //SetLotBatchNoValidators();
                    //SetChallanNoValidators();
                    //SetChallanDateValidators();
                    //SetPkdDateValidators();
                }

            }
        }

        //private void SetQuantityValidators()
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
        //private void SetFatContentValidators()
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
        //private void SetMilkValidators()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_Milk.ErrorMessage = "*";
        //    RFV_TB_Milk.ForeColor = System.Drawing.Color.Red;
        //    RFV_TB_Milk.Enabled = true;

        //    // Set properties of RegularExpressionValidator
        //    REV_TB_Milk.ErrorMessage = "Decimal Only";
        //    REV_TB_Milk.ForeColor = System.Drawing.Color.Red;
        //    REV_TB_Milk.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

        //    // Set properties of RangeValidator
        //    CV_TB_Milk.ErrorMessage = "[0.00 - 1000.00]";
        //    CV_TB_Milk.ForeColor = System.Drawing.Color.Red;
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
        //private void SetLactoseValidators()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_Lactose.ErrorMessage = "*";
        //    RFV_TB_Lactose.ForeColor = System.Drawing.Color.Red;
        //    RFV_TB_Lactose.Enabled = true;

        //    // Set properties of RegularExpressionValidator
        //    REV_TB_Lactose.ErrorMessage = "Integer Only";
        //    REV_TB_Lactose.ForeColor = System.Drawing.Color.Red;
        //    REV_TB_Lactose.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

        //    // Set properties of RangeValidator
        //    CV_TB_Lactose.ErrorMessage = "[0.00 - 1000.00]";
        //    CV_TB_Lactose.ForeColor = System.Drawing.Color.Red;
        //}
        //private void SetProteinValidators()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_Protein.ErrorMessage = "*";
        //    RFV_TB_Protein.ForeColor = System.Drawing.Color.Red;
        //    RFV_TB_Protein.Enabled = true;

        //    // Set properties of RegularExpressionValidator
        //    REV_TB_Protein.ErrorMessage = "Integer Only";
        //    REV_TB_Protein.ForeColor = System.Drawing.Color.Red;
        //    REV_TB_Protein.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

        //    // Set properties of RangeValidator
        //    CV_TB_Protein.ErrorMessage = "[0.00 - 1000.00]";
        //    CV_TB_Protein.ForeColor = System.Drawing.Color.Red;
        //}
        //private void SetMilkFatValidators()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_MilkFat.ErrorMessage = "*";
        //    RFV_TB_MilkFat.ForeColor = System.Drawing.Color.Red;
        //    RFV_TB_MilkFat.Enabled = true;

        //    // Set properties of RegularExpressionValidator
        //    REV_TB_MilkFat.ErrorMessage = "Decimal Only";
        //    REV_TB_MilkFat.ForeColor = System.Drawing.Color.Red;
        //    REV_TB_MilkFat.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

        //    // Set properties of RangeValidator
        //    CV_TB_MilkFat.ErrorMessage = "[0.00 - 1000.00]";
        //    CV_TB_MilkFat.ForeColor = System.Drawing.Color.Red;
        //}
        //private void SetTitrableAcidityValidators()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_TitrableAcidity.ErrorMessage = "*";
        //    RFV_TB_TitrableAcidity.ForeColor = System.Drawing.Color.Red;
        //    RFV_TB_TitrableAcidity.Enabled = true;

        //    // Set properties of RegularExpressionValidator
        //    REV_TB_TitrableAcidity.ErrorMessage = "Decimal Only";
        //    REV_TB_TitrableAcidity.ForeColor = System.Drawing.Color.Red;
        //    REV_TB_TitrableAcidity.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

        //    // Set properties of RangeValidator
        //    CV_TB_TitrableAcidity.ErrorMessage = "[0.00 - 1000.00]";
        //    CV_TB_TitrableAcidity.ForeColor = System.Drawing.Color.Red;
        //}    
        //private void SetSO2Validators()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_SO2.ErrorMessage = "*";
        //    RFV_TB_SO2.ForeColor = System.Drawing.Color.Red;
        //    RFV_TB_SO2.Enabled = true;

        //    // Set properties of RegularExpressionValidator
        //    REV_TB_SO2.ErrorMessage = "Integer Only";
        //    REV_TB_SO2.ForeColor = System.Drawing.Color.Red;
        //    REV_TB_SO2.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

        //    // Set properties of RangeValidator
        //    CV_TB_SO2.ErrorMessage = "[0.00 - 1000.00]";
        //    CV_TB_SO2.ForeColor = System.Drawing.Color.Red;
        //}     
        //private void SetGlucoseContentValidators()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_GlucoseContent.ErrorMessage = "*";
        //    RFV_TB_GlucoseContent.ForeColor = System.Drawing.Color.Red;
        //    RFV_TB_GlucoseContent.Enabled = true;

        //    // Set properties of RegularExpressionValidator
        //    REV_TB_GlucoseContent.ErrorMessage = "Integer Only";
        //    REV_TB_GlucoseContent.ForeColor = System.Drawing.Color.Red;
        //    REV_TB_GlucoseContent.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

        //    // Set properties of RangeValidator
        //    CV_TB_GlucoseContent.ErrorMessage = "[0.00 - 1000.00]";
        //    CV_TB_GlucoseContent.ForeColor = System.Drawing.Color.Red;
        //}
        //private void SetLossOnDryingValidators()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_LossOnDrying.ErrorMessage = "*";
        //    RFV_TB_LossOnDrying.ForeColor = System.Drawing.Color.Red;
        //    RFV_TB_LossOnDrying.Enabled = true;

        //    // Set properties of RegularExpressionValidator
        //    REV_TB_LossOnDrying.ErrorMessage = "Decimal Only";
        //    REV_TB_LossOnDrying.ForeColor = System.Drawing.Color.Red;
        //    REV_TB_LossOnDrying.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

        //    // Set properties of RangeValidator
        //    CV_TB_LossOnDrying.ErrorMessage = "[0.00 - 1000.00]";
        //    CV_TB_LossOnDrying.ForeColor = System.Drawing.Color.Red;
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
        //    REV_TB_LotNo.ValidationExpression = "^[a-zA-Z0-9, /]*$"; // Regular expression for alphanumeric input
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
        //private void SetPkdDateValidators()
        //{
        //    // Set properties of RequiredFieldValidator
        //    RFV_TB_PkdMfg.ErrorMessage = "*";
        //    RFV_TB_PkdMfg.ForeColor = System.Drawing.Color.Red;
        //    RFV_TB_PkdMfg.Enabled = true;
        //}
        

        private void MaterialBinder()
        {
            string query = "SELECT Material_Id, Material_Name FROM RM_Material where Class = 4";
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

                //PlantLinesBinder(selectedPlantValue);

                //DataTable dataTable = DatabaseHelper.GetRMFieldsControlByPlantId(Convert.ToInt32(selectedMaterialValue), Convert.ToInt32(selectedPlantValue));
                DataTable dataTable = DatabaseHelper.GetBrandFieldsControlByMaterialIdAndPlantId(
                    Convert.ToInt16(selectedMaterialValue),
                    Convert.ToInt16(selectedPlantValue)
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    int result = InsertQCClass4Rpt_BrandFieldsControl(Convert.ToInt16(selectedMaterialValue), selectedMaterialText, Convert.ToInt16(selectedPlantValue), selectedPlantText);

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

        public int InsertQCClass4Rpt_BrandFieldsControl(int materialId, string materialName, int plantId, string plantName)
        {
            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.InsertQCRMClass4Rpt_BrandFieldsControl", conn))
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
                    string query = "SELECT ISNULL(MAX(CAST(SUBSTRING(RMFID, 4, LEN(RMFID)) AS INT)), 0) FROM TRN_RM_CLASS_4";
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

        protected void BtnSubmit_Click(object sender, EventArgs e)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            string rmfId = GenerateUnique();  // Generate unique value
            int formID = Convert.ToInt32(hdn_formid.Value.ToString());

            string materialName = DDL_Material.SelectedValue;
            string plantName = DDL_Plant.SelectedValue;

            //string line = DDL_PlantLine.SelectedValue;
            //string productBrand = string.IsNullOrEmpty(DDL_ProductBrand.SelectedValue) ? null : DDL_ProductBrand.Text;

            string line = string.Empty;
            string productBrand = string.Empty;

            string supplier = string.IsNullOrEmpty(TB_Supplier.Text) ? null : TB_Supplier.Text;
            string challanNo = string.IsNullOrEmpty(TB_ChallanNo.Text) ? null : TB_ChallanNo.Text;
            DateTime? challanDate = string.IsNullOrEmpty(TB_ChallanDate.Text) ? (DateTime?)null : DateTime.Parse(TB_ChallanDate.Text).Date;
            string lotNo = string.IsNullOrEmpty(TB_LotNo.Text) ? null : TB_LotNo.Text;
            DateTime? pkdMfgDate = string.IsNullOrEmpty(TB_PkdMfg.Text) ? (DateTime?)null : DateTime.Parse(TB_PkdMfg.Text).Date;
            string vehicleNo = string.IsNullOrEmpty(TB_VehicleNo.Text) ? null : TB_VehicleNo.Text;

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

            decimal? fungus = !string.IsNullOrWhiteSpace(TB_Fungus.Text) ? Convert.ToDecimal(TB_Fungus.Text) : (decimal?)null;
            string fungusRemarks = string.IsNullOrEmpty(TXB_Fungus_Remarks.Text) ? null : TXB_Fungus_Remarks.Text;

            decimal? ph = !string.IsNullOrWhiteSpace(TB_PH.Text) ? Convert.ToDecimal(TB_PH.Text) : (decimal?)null;
            string phRemarks = string.IsNullOrEmpty(TXB_PH_Remarks.Text) ? null : TXB_PH_Remarks.Text;

            decimal? moisture = !string.IsNullOrWhiteSpace(TB_Moisture.Text) ? Convert.ToDecimal(TB_Moisture.Text) : (decimal?)null;
            string moistureRemarks = string.IsNullOrEmpty(TXB_Moisture_Remarks.Text) ? null : TXB_Moisture_Remarks.Text;

            decimal? ash = !string.IsNullOrWhiteSpace(TB_TotalAsh.Text) ? Convert.ToDecimal(TB_TotalAsh.Text) : (decimal?)null;
            string ashRemarks = string.IsNullOrEmpty(TXB_Ash_Remarks.Text) ? null : TXB_Ash_Remarks.Text;

            decimal? fatContent = !string.IsNullOrWhiteSpace(TB_FatContent.Text) ? Convert.ToDecimal(TB_FatContent.Text) : (decimal?)null;
            string fatContentRemarks = string.IsNullOrEmpty(TXB_Fat_Remarks.Text) ? null : TXB_Fat_Remarks.Text;

            decimal? milk = !string.IsNullOrWhiteSpace(TB_Milk.Text) ? Convert.ToDecimal(TB_Milk.Text) : (decimal?)null;
            string milkRemarks = string.IsNullOrEmpty(TXB_Milk_Remarks.Text) ? null : TXB_Milk_Remarks.Text;

            decimal? solid = !string.IsNullOrWhiteSpace(TB_TotalSolid.Text) ? Convert.ToDecimal(TB_TotalSolid.Text) : (decimal?)null;
            string solidRemarks = string.IsNullOrEmpty(TXB_Solid_Remarks.Text) ? null : TXB_Solid_Remarks.Text;

            decimal? lactoseContent = !string.IsNullOrWhiteSpace(TB_Lactose.Text) ? Convert.ToDecimal(TB_Lactose.Text) : (decimal?)null;
            string lactoseContentRemarks = string.IsNullOrEmpty(TXB_Lactose_Remarks.Text) ? null : TXB_Lactose_Remarks.Text;

            decimal? protein = !string.IsNullOrWhiteSpace(TB_Protein.Text) ? Convert.ToDecimal(TB_Protein.Text) : (decimal?)null;
            string proteinRemarks = string.IsNullOrEmpty(TXB_Protein_Remarks.Text) ? null : TXB_Protein_Remarks.Text;

            decimal? milkFat = !string.IsNullOrWhiteSpace(TB_MilkFat.Text) ? Convert.ToDecimal(TB_MilkFat.Text) : (decimal?)null;
            string milkFatRemarks = string.IsNullOrEmpty(TXB_MilkFat_Remarks.Text) ? null : TXB_MilkFat_Remarks.Text;

            decimal? titrableAcidity = !string.IsNullOrWhiteSpace(TB_TitrableAcidity.Text) ? Convert.ToDecimal(TB_TitrableAcidity.Text) : (decimal?)null;
            string titrableAcidityRemarks = string.IsNullOrEmpty(TXB_TitrableAcidity_Remarks.Text) ? null : TXB_TitrableAcidity_Remarks.Text;

            decimal? sO2 = !string.IsNullOrWhiteSpace(TB_SO2.Text) ? Convert.ToDecimal(TB_SO2.Text) : (decimal?)null;
            string sO2Remarks = string.IsNullOrEmpty(TXB_SO2_Remarks.Text) ? null : TXB_SO2_Remarks.Text;

            int? glucoseContent = string.IsNullOrEmpty(TB_GlucoseContent.Text) ? (int?)null : Convert.ToInt32(TB_GlucoseContent.Text);
            string glucoseContentRemarks = string.IsNullOrEmpty(TXB_GlucoseContent_Remarks.Text) ? null : TXB_GlucoseContent_Remarks.Text;

            decimal? lossOnDrying = !string.IsNullOrWhiteSpace(TB_LossOnDrying.Text) ? Convert.ToDecimal(TB_LossOnDrying.Text) : (decimal?)null;
            string lossOnDryingRemarks = string.IsNullOrEmpty(TXB_LossOnDrying_Remarks.Text) ? null : TXB_LossOnDrying_Remarks.Text;

            decimal? sulphatedAsh = !string.IsNullOrWhiteSpace(TB_SulphatedAsh.Text) ? Convert.ToDecimal(TB_SulphatedAsh.Text) : (decimal?)null;
            string sulphatedAshRemarks = string.IsNullOrEmpty(TXB_SulphatedAsh_Remarks.Text) ? null : TXB_SulphatedAsh_Remarks.Text;

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
                    using (SqlCommand command = new SqlCommand("SP_RM_CLASS_4", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Add parameters
                        command.Parameters.AddWithValue("@RMFID", rmfId);
                        command.Parameters.AddWithValue("@FormID", formID);

                        command.Parameters.AddWithValue("@MaterialName", materialName);
                        command.Parameters.AddWithValue("@PlantName", plantName);
                        command.Parameters.AddWithValue("@Line", line);
                        command.Parameters.AddWithValue("@ProductBrand", (object)productBrand ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Supplier_Name", (object)supplier ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Challan_No", (object)challanNo ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Challan_Date", (object)challanDate ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Lot_No", (object)lotNo ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Pkd_Date", (object)pkdMfgDate ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Vehicle_No", (object)vehicleNo ?? DBNull.Value);

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

                        command.Parameters.AddWithValue("@Fungus", (object)fungus ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForFungus", (object)fungusRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@PH", (object)ph ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForPH", (object)phRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Moisture", (object)moisture ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForMoisture", (object)moistureRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Total_Ash", (object)ash ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForAsh", (object)ashRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Fat_Content", (object)fatContent ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForFatContent", (object)fatContentRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Milk", (object)milk ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForMilk", (object)milkRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Total_Solids", (object)solid ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForSolid", (object)solidRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Lactose", (object)lactoseContent ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForLactose", (object)lactoseContentRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Protein", (object)protein ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForProtein", (object)proteinRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@MilkFat", (object)milkFat ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForMilkFat", (object)milkFatRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Titrable_Acidity", (object)titrableAcidity ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForTitrableAcidity", (object)titrableAcidityRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@SO2", (object)sO2 ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForSO2", (object)sO2Remarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Glucose_Content", (object)glucoseContent ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForGlucoseContent", (object)glucoseContentRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@Loss_On_Drying", (object)lossOnDrying ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForLossOnDrying", (object)lossOnDryingRemarks ?? DBNull.Value);

                        command.Parameters.AddWithValue("@SulphatedAsh", (object)sulphatedAsh ?? DBNull.Value);
                        command.Parameters.AddWithValue("@CommentsForSulphatedAsh", (object)sulphatedAshRemarks ?? DBNull.Value);

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
            //DDL_PlantLine.Enabled = false;
            //DDL_ProductBrand.Enabled = false;

            TB_Supplier.ReadOnly = true;
            TB_ChallanNo.ReadOnly = true;
            TB_ChallanDate.ReadOnly = true;
            TB_Quantity.ReadOnly = true;
            TXB_Quantity_Remarks.ReadOnly = true;
            TB_LotNo.ReadOnly = true;
            TB_PkdMfg.ReadOnly = true;
            TB_VehicleNo.ReadOnly = true;

            RBL_Smell.Enabled = false;
            TXB_Smell_Remarks.ReadOnly = true;

            RBL_Appearance.Enabled = false;
            TXB_Appearance_Remarks.ReadOnly = true;

            DDL_Color.Enabled = false;
            TXB_Color_Remarks.ReadOnly = true;

            RBL_TasteFlavor.Enabled = false;
            TXB_TasteFlavor_Remarks.ReadOnly = true;

            TB_Fungus.ReadOnly = true;
            TXB_Fungus_Remarks.ReadOnly = true;

            TB_PH.ReadOnly = true;
            TXB_PH_Remarks.ReadOnly = true;

            TB_Moisture.ReadOnly = true;
            TXB_Moisture_Remarks.ReadOnly = true;

            TB_TotalAsh.ReadOnly = true;
            TXB_Ash_Remarks.ReadOnly = true;

            TB_FatContent.ReadOnly = true;
            TXB_Fat_Remarks.ReadOnly = true;

            TB_Milk.ReadOnly = true;
            TXB_Milk_Remarks.ReadOnly = true;

            TB_TotalSolid.ReadOnly = true;
            TXB_Solid_Remarks.ReadOnly = true;

            TB_Lactose.ReadOnly = true;
            TXB_Lactose_Remarks.ReadOnly = true;

            TB_Protein.ReadOnly = true;
            TXB_Protein_Remarks.ReadOnly = true;

            TB_MilkFat.ReadOnly = true;
            TXB_MilkFat_Remarks.ReadOnly = true;

            TB_TitrableAcidity.ReadOnly = true;
            TXB_TitrableAcidity_Remarks.ReadOnly = true;

            TB_SO2.ReadOnly = true;
            TXB_SO2_Remarks.ReadOnly = true;

            TB_GlucoseContent.ReadOnly = true;
            TXB_GlucoseContent_Remarks.ReadOnly = true;

            TB_LossOnDrying.ReadOnly = true;
            TXB_LossOnDrying_Remarks.ReadOnly = true;

            TB_SulphatedAsh.ReadOnly = true;
            TXB_SulphatedAsh_Remarks.ReadOnly = true;

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
            Response.Redirect("RM_Class_4.aspx");
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

        private void SetUpValidatorsForField(string fieldName, ValidationCriteria criteria)
        {
            switch (fieldName)
            {
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

                case "ProductBrand":

                    //BrandDIV.Visible = criteria.IsVisible;

                    //RFV_DDL_ProductBrand.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    //RFV_DDL_ProductBrand.Enabled = criteria.IsRequired;

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

                case "Pkd/MfgDate":

                    PkdMfgDIV.Visible = criteria.IsVisible;

                    RFV_TB_PkdMfg.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_PkdMfg.Enabled = criteria.IsRequired;

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

                case "FungusValue":

                    FungusDIV.Visible = criteria.IsVisible;

                    RFV_TB_Fungus.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Fungus.Enabled = criteria.IsRequired;

                    TB_Fungus.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Fungus.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Fungus.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Fungus.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_Fungus.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_Fungus.Enabled = criteria.IsRangeRequired;

                    hdnMinFungusValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxFungusValue.Value = criteria.MaximumValue.ToString();
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

                    hdnMinFatContentValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxFatContentValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "MilkValue":

                    MilkDIV.Visible = criteria.IsVisible;

                    RFV_TB_Milk.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Milk.Enabled = criteria.IsRequired;

                    TB_Milk.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Milk.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Milk.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Milk.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_Milk.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_Milk.Enabled = criteria.IsRangeRequired;

                    hdnMinMilkValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxMilkValue.Value = criteria.MaximumValue.ToString();
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

                case "LactoseContentValue":

                    LactoseDIV.Visible = criteria.IsVisible;

                    RFV_TB_Lactose.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_Lactose.Enabled = criteria.IsRequired;

                    TB_Lactose.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_Lactose.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_Lactose.ValidationExpression = criteria.RegularExpression;
                    REV_TB_Lactose.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_Lactose.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_Lactose.Enabled = criteria.IsRangeRequired;

                    hdnMinLactoseContentValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxLactoseContentValue.Value = criteria.MaximumValue.ToString();
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

                case "MilkFatValue":

                    MilkFatDIV.Visible = criteria.IsVisible;

                    RFV_TB_MilkFat.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_MilkFat.Enabled = criteria.IsRequired;

                    TB_MilkFat.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_MilkFat.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_MilkFat.ValidationExpression = criteria.RegularExpression;
                    REV_TB_MilkFat.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_MilkFat.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_MilkFat.Enabled = criteria.IsRangeRequired;

                    hdnMinMilkFatValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxMilkFatValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "TitrableAcidityValue":

                    TitrableAcidityDIV.Visible = criteria.IsVisible;

                    RFV_TB_TitrableAcidity.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_TitrableAcidity.Enabled = criteria.IsRequired;

                    TB_TitrableAcidity.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_TitrableAcidity.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_TitrableAcidity.ValidationExpression = criteria.RegularExpression;
                    REV_TB_TitrableAcidity.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_TitrableAcidity.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_TitrableAcidity.Enabled = criteria.IsRangeRequired;

                    hdnMinTitrableAcidityValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxTitrableAcidityValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "SO2Value":

                    SO2DIV.Visible = criteria.IsVisible;

                    RFV_TB_SO2.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_SO2.Enabled = criteria.IsRequired;

                    TB_SO2.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_SO2.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_SO2.ValidationExpression = criteria.RegularExpression;
                    REV_TB_SO2.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_SO2.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_SO2.Enabled = criteria.IsRangeRequired;

                    hdnMinSO2Value.Value = criteria.MinimumValue.ToString();
                    hdnMaxSO2Value.Value = criteria.MaximumValue.ToString();
                    break;
                
                case "GlucoseContentValue":

                    GlucoseContentDIV.Visible = criteria.IsVisible;

                    RFV_TB_GlucoseContent.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_GlucoseContent.Enabled = criteria.IsRequired;

                    TB_GlucoseContent.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_GlucoseContent.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_GlucoseContent.ValidationExpression = criteria.RegularExpression;
                    REV_TB_GlucoseContent.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_GlucoseContent.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_GlucoseContent.Enabled = criteria.IsRangeRequired;

                    hdnMinGlucoseContentValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxGlucoseContentValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "LossOnDryingValue":

                    LossOnDryingDIV.Visible = criteria.IsVisible;

                    RFV_TB_LossOnDrying.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_LossOnDrying.Enabled = criteria.IsRequired;

                    TB_LossOnDrying.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_LossOnDrying.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_LossOnDrying.ValidationExpression = criteria.RegularExpression;
                    REV_TB_LossOnDrying.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_LossOnDrying.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_LossOnDrying.Enabled = criteria.IsRangeRequired;

                    hdnMinLossonDryingValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxLossonDryingValue.Value = criteria.MaximumValue.ToString();
                    break;

                case "SulphatedAshValue":

                    SulphatedAshDIV.Visible = criteria.IsVisible;

                    RFV_TB_SulphatedAsh.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_SulphatedAsh.Enabled = criteria.IsRequired;

                    TB_SulphatedAsh.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_SulphatedAsh.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_SulphatedAsh.ValidationExpression = criteria.RegularExpression;
                    REV_TB_SulphatedAsh.Enabled = criteria.IsRegularExpressionRequired;

                    CV_TB_SulphatedAsh.ErrorMessage = criteria.RangeErrorMessage;
                    CV_TB_SulphatedAsh.Enabled = criteria.IsRangeRequired;

                    hdnMinSulphatedAshValue.Value = criteria.MinimumValue.ToString();
                    hdnMaxSulphatedAshValue.Value = criteria.MaximumValue.ToString();
                    break;


                default:
                    // Handle unrecognized field names
                    break;
            }
        }

        public class ValidationCriteria
        {
            public bool IsVisible { get; set; }
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
        //        case "Butter":

        //            //Product Brand
        //            BrandDIV.Visible = true;

        //            //Supplier	
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

        //            //Appearance	
        //            AppearanceDIV.Visible = true;

        //            //Taste/Flavour	
        //            TasteFlavorDIV.Visible = true;

        //            //Fungus Infestation
        //            FungusDIV.Visible = true;

        //            //Moisture
        //            MoistureDIV.Visible = true;

        //            //Fat Content 
        //            FatContentDIV.Visible = true;

        //            //Milk Snf
        //            MilkDIV.Visible = true;

        //            //Image
        //            FU_MaterialImage_Upldr.Visible = true;
        //            FU_MaterialImage.Visible = true;
        //            break;

        //        case "Condensed Milk":
        //            //Supplier	
        //            SupplierDIV.Visible = true;

        //            //Challan No & Date	
        //            ChallanNoDIV.Visible = true;
        //            ChallanDateDIV.Visible = true;

        //            //Lot No/ Batch No.
        //            LotNoDIV.Visible = true;

        //            //Pkd/Mfg Date	
        //            PkdMfgDIV.Visible = true;

        //            //Vehicle No	
        //            VehicleNoDIV.Visible = true;

        //            //Colour	
        //            ColorDIV.Visible = true;
        //            ColorRemarksDiv.Visible = true;

        //            //Taste/Flavour	
        //            TasteFlavorDIV.Visible = true;

        //            //Total Solids	
        //            SolidDIV.Visible = true;

        //            //Titrable Acidity
        //            TitrableAcidityDIV.Visible = true;


        //            //Image
        //            FU_MaterialImage_Upldr.Visible = true;
        //            FU_MaterialImage.Visible = true;
        //            break;

        //        case "SMP":

        //            //Product Brand
        //            BrandDIV.Visible = true;

        //            // Supplier
        //            SupplierDIV.Visible = true;

        //            //Challan No & Date	
        //            ChallanDateDIV.Visible = true;
        //            ChallanNoDIV.Visible = true;

        //            //Lot No/ Batch No.
        //            LotNoDIV.Visible = true;

        //            //Pkd/Mfg Date	
        //            PkdMfgDIV.Visible = true;

        //            //Vehicle No	
        //            VehicleNoDIV.Visible = true;

        //            //Taste/Flavour	
        //            TasteFlavorDIV.Visible = true;

        //            //Moisture
        //            MoistureDIV.Visible = true;

        //            //Total Ash
        //            AshDIV.Visible = true;

        //            //Protein	
        //            ProteinDIV.Visible = true;

        //            //Milk Fat
        //            MilkFatDIV.Visible = true;

        //            //Titrable Acidity
        //            TitrableAcidityDIV.Visible = true;

        //            //SO2
        //            SO2DIV.Visible = true;

        //            //Image
        //            FU_MaterialImage_Upldr.Visible = true;
        //            FU_MaterialImage.Visible = true;
        //            break;

        //        case "Whey Powder":

        //            //Product Brand
        //            BrandDIV.Visible = true;

        //            // Supplier
        //            SupplierDIV.Visible = true;

        //            //Challan No & Date	
        //            ChallanDateDIV.Visible = true;
        //            ChallanNoDIV.Visible = true;

        //            //Lot No/ Batch No.
        //            LotNoDIV.Visible = true;

        //            //Pkd/Mfg Date	
        //            PkdMfgDIV.Visible = true;

        //            //Vehicle No	
        //            VehicleNoDIV.Visible = true;

        //            //Taste/Flavour	
        //            TasteFlavorDIV.Visible = true;

        //            //pH
        //            PHDIV.Visible = true;

        //            //Moisture
        //            MoistureDIV.Visible = true;

        //            //Total Ash
        //            AshDIV.Visible = true;

        //            //Lactose Content
        //            LactoseDIV.Visible = true;

        //            //Protein	
        //            ProteinDIV.Visible = true;

        //            //Milk Fat
        //            MilkFatDIV.Visible = true;

        //            //Image
        //            FU_MaterialImage_Upldr.Visible = true;
        //            FU_MaterialImage.Visible = true;
        //            break;

        //        case "Whole Milk Powder":

        //            //Product Brand
        //            BrandDIV.Visible = true;

        //            // Supplier
        //            SupplierDIV.Visible = true;

        //            //Challan No & Date	
        //            ChallanDateDIV.Visible = true;
        //            ChallanNoDIV.Visible = true;

        //            //QTY	
        //            QuantityDIV.Visible = true;

        //            //Lot No/ Batch No.
        //            LotNoDIV.Visible = true;

        //            //Pkd/Mfg Date	
        //            PkdMfgDIV.Visible = true;


        //            //Colour	
        //            ColorDIV.Visible = true;
        //            ColorRemarksDiv.Visible = true;

        //            //Odour/Smell	
        //            SmellDIV.Visible = true;

        //            //Appearance	
        //            AppearanceDIV.Visible = true;

        //            //Taste/Flavour	
        //            TasteFlavorDIV.Visible = true;

        //            //Moisture		
        //            MoistureDIV.Visible = true;

        //            //Titrable Acidity	
        //            TitrableAcidityDIV.Visible = true;

        //            //SO2	
        //            SO2DIV.Visible = true;

        //            //Loss on Drying	
        //            LossOnDryingDIV.Visible = true;

        //            //Glucose Content
        //            GlucoseContentDIV.Visible = true;

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