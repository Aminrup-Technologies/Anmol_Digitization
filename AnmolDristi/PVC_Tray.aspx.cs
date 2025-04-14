using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using AnmolDristi.DAL;
using System.Configuration;

namespace AnmolDristi
{
    public partial class PVC__Tray : System.Web.UI.Page
    {
        public static string PVCID = string.Empty;
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
                    lbl_docname.Text = "PVC Tray / Mono CB Report";
                    lbl_docnumber.Text = "ANMOL/DOC/CORP/QC/PKNG/05";

                    PlantBinder();
                }

            }
        }

        private void SetValidatorPropertiesFromDatabase()
        {
            // Fetch validation criteria from the database for Dimension_Obs_L
            ValidationCriteria Dimension_Obs_L_VC = GetValidationCriteriaFromDatabase("Dimension_Obs_L");

            // Set properties for Dimension_Obs_L validators
            RFV_TB_DimensionL.ErrorMessage = Dimension_Obs_L_VC.RequiredFieldErrorMessage;
            RFV_TB_DimensionL.Enabled = Dimension_Obs_L_VC.IsRequired;
            REV_TB_DimensionL.ErrorMessage = Dimension_Obs_L_VC.RegularExpressionErrorMessage;
            REV_TB_DimensionL.ValidationExpression = Dimension_Obs_L_VC.RegularExpression;
            REV_TB_DimensionL.Enabled = Dimension_Obs_L_VC.IsRegularExpressionRequired;
            RV_TB_DimensionL.ErrorMessage = Dimension_Obs_L_VC.RangeErrorMessage;
            RV_TB_DimensionL.MinimumValue = Dimension_Obs_L_VC.MinimumValue;
            RV_TB_DimensionL.MaximumValue = Dimension_Obs_L_VC.MaximumValue;
            RV_TB_DimensionL.Enabled = Dimension_Obs_L_VC.IsRangeRequired;

            // Fetch validation criteria from the database for Dimension_Obs_W
            ValidationCriteria Dimension_Obs_W_VC = GetValidationCriteriaFromDatabase("Dimension_Obs_W");

            // Set properties for Dimension_Obs_W validators
            RFV_TB_DimensionW.ErrorMessage = Dimension_Obs_W_VC.RequiredFieldErrorMessage;
            RFV_TB_DimensionW.Enabled = Dimension_Obs_W_VC.IsRequired;
            REV_TB_DimensionW.ErrorMessage = Dimension_Obs_W_VC.RegularExpressionErrorMessage;
            REV_TB_DimensionW.ValidationExpression = Dimension_Obs_W_VC.RegularExpression;
            REV_TB_DimensionW.Enabled = Dimension_Obs_W_VC.IsRegularExpressionRequired;
            RV_TB_DimensionW.ErrorMessage = Dimension_Obs_W_VC.RangeErrorMessage;
            RV_TB_DimensionW.MinimumValue = Dimension_Obs_W_VC.MinimumValue;
            RV_TB_DimensionW.MaximumValue = Dimension_Obs_W_VC.MaximumValue;
            RV_TB_DimensionW.Enabled = Dimension_Obs_W_VC.IsRangeRequired;

            // Fetch validation criteria from the database for Dimension_Obs_H
            ValidationCriteria Dimension_Obs_H_VC = GetValidationCriteriaFromDatabase("Dimension_Obs_H");

            // Set properties for Dimension_Obs_H validators
            RFV_TB_DimensionH.ErrorMessage = Dimension_Obs_H_VC.RequiredFieldErrorMessage;
            RFV_TB_DimensionH.Enabled = Dimension_Obs_H_VC.IsRequired;
            REV_TB_DimensionH.ErrorMessage = Dimension_Obs_H_VC.RegularExpressionErrorMessage;
            REV_TB_DimensionH.ValidationExpression = Dimension_Obs_H_VC.RegularExpression;
            REV_TB_DimensionH.Enabled = Dimension_Obs_H_VC.IsRegularExpressionRequired;
            RV_TB_DimensionH.ErrorMessage = Dimension_Obs_H_VC.RangeErrorMessage;
            RV_TB_DimensionH.MinimumValue = Dimension_Obs_H_VC.MinimumValue;
            RV_TB_DimensionH.MaximumValue = Dimension_Obs_H_VC.MaximumValue;
            RV_TB_DimensionH.Enabled = Dimension_Obs_H_VC.IsRangeRequired;

            // Fetch validation criteria from the database for GSM_Obs
            ValidationCriteria GSM_Obs_VC = GetValidationCriteriaFromDatabase("GSM_Obs");

            // Set properties for GSM_Obs validators
            RFV_TB_GSM.ErrorMessage = GSM_Obs_VC.RequiredFieldErrorMessage;
            RFV_TB_GSM.Enabled = GSM_Obs_VC.IsRequired;
            REV_TB_GSM.ErrorMessage = GSM_Obs_VC.RegularExpressionErrorMessage;
            REV_TB_GSM.ValidationExpression = GSM_Obs_VC.RegularExpression;
            REV_TB_GSM.Enabled = GSM_Obs_VC.IsRegularExpressionRequired;
            RV_TB_GSM.ErrorMessage = GSM_Obs_VC.RangeErrorMessage;
            RV_TB_GSM.MinimumValue = GSM_Obs_VC.MinimumValue;
            RV_TB_GSM.MaximumValue = GSM_Obs_VC.MaximumValue;
            RV_TB_GSM.Enabled = GSM_Obs_VC.IsRangeRequired;

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
        private ValidationCriteria GetValidationCriteriaFromDatabase(string fieldName)
        {
            // Query the database to fetch validation criteria based on the field name
            // Implement database querying logic here, and return the fetched data
            // For example:
            // SELECT * FROM ValidationCriteria WHERE FieldName = fieldName

            // Simulated data for demonstration
            ValidationCriteria criteria = new ValidationCriteria();
            criteria.RequiredFieldErrorMessage = "*";
            criteria.RegularExpressionErrorMessage = "[N1 to N2]";
            criteria.RegularExpression = @"\d+";
            criteria.RangeErrorMessage = "[N1 to N2]";
            criteria.MinimumValue = "50";
            criteria.MaximumValue = "100";
            criteria.IsRequired = true;
            criteria.IsRegularExpressionRequired = true;
            criteria.IsRangeRequired = false;

            return criteria;
        }
        private void Dimension_Obs_L()
        {
            // Set properties of RequiredFieldValidator
            RFV_TB_DimensionL.ErrorMessage = "*";
            RFV_TB_DimensionL.InitialValue = "0";
            RFV_TB_DimensionL.ForeColor = System.Drawing.Color.Red;

            // Set properties of RegularExpressionValidator
            REV_TB_DimensionL.ErrorMessage = "Numeric Only";
            REV_TB_DimensionL.ForeColor = System.Drawing.Color.Red;
            REV_TB_DimensionL.ValidationExpression = @"\d+"; // Regular expression for numeric input

            // Set properties of RangeValidator
            RV_TB_DimensionL.ErrorMessage = "[N1 to N2]";
            RV_TB_DimensionL.ForeColor = System.Drawing.Color.Red;
            RV_TB_DimensionL.MinimumValue = "50";
            RV_TB_DimensionL.MaximumValue = "100";
        }
        private void Dimension_Obs_W()
        {
            // Set properties of RequiredFieldValidator
            RFV_TB_DimensionW.ErrorMessage = "*";
            RFV_TB_DimensionW.InitialValue = "0";
            RFV_TB_DimensionW.ForeColor = System.Drawing.Color.Red;

            // Set properties of RegularExpressionValidator
            REV_TB_DimensionW.ErrorMessage = "Numeric Only";
            REV_TB_DimensionW.ForeColor = System.Drawing.Color.Red;
            REV_TB_DimensionW.ValidationExpression = @"\d+"; // Regular expression for numeric input

            // Set properties of RangeValidator
            RV_TB_DimensionW.ErrorMessage = "[N1 to N2]";
            RV_TB_DimensionW.ForeColor = System.Drawing.Color.Red;
            RV_TB_DimensionW.MinimumValue = "50";
            RV_TB_DimensionW.MaximumValue = "100";
        }
        private void Dimension_Obs_H()
        {
            // Set properties of RequiredFieldValidator
            RFV_TB_DimensionH.ErrorMessage = "*";
            RFV_TB_DimensionH.InitialValue = "0";
            RFV_TB_DimensionH.ForeColor = System.Drawing.Color.Red;

            // Set properties of RegularExpressionValidator
            REV_TB_DimensionH.ErrorMessage = "Numeric Only";
            REV_TB_DimensionH.ForeColor = System.Drawing.Color.Red;
            REV_TB_DimensionH.ValidationExpression = @"\d+"; // Regular expression for numeric input

            // Set properties of RangeValidator
            RV_TB_DimensionH.ErrorMessage = "[N1 to N2]";
            RV_TB_DimensionH.ForeColor = System.Drawing.Color.Red;
            RV_TB_DimensionH.MinimumValue = "50";
            RV_TB_DimensionH.MaximumValue = "100";
        }
        private void GSM_Obs()
        {
            // Set properties of RequiredFieldValidator
            RFV_TB_GSM.ErrorMessage = "*";
            RFV_TB_GSM.InitialValue = "0";
            RFV_TB_GSM.ForeColor = System.Drawing.Color.Red;

            // Set properties of RegularExpressionValidator
            REV_TB_GSM.ErrorMessage = "Numeric Only";
            REV_TB_GSM.ForeColor = System.Drawing.Color.Red;
            REV_TB_GSM.ValidationExpression = @"\d+"; // Regular expression for numeric input

            // Set properties of RangeValidator
            RV_TB_GSM.ErrorMessage = "[N1 to N2]";
            RV_TB_GSM.ForeColor = System.Drawing.Color.Red;
            RV_TB_GSM.MinimumValue = "50";
            RV_TB_GSM.MaximumValue = "100";
        }
        private void SetUpValidatorsForField(string fieldName, ValidationCriteria criteria)
        {
            switch (fieldName)
            {
                case "Dimension_Obs_L":

                    RFV_TB_DimensionL.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_DimensionL.Enabled = criteria.IsRequired;

                    TB_DimensionL.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_DimensionL.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_DimensionL.ValidationExpression = criteria.RegularExpression;
                    REV_TB_DimensionL.Enabled = criteria.IsRegularExpressionRequired;

                    RV_TB_DimensionL.ErrorMessage = criteria.RangeErrorMessage;
                    RV_TB_DimensionL.MinimumValue = criteria.MinimumValue;
                    RV_TB_DimensionL.MaximumValue = criteria.MaximumValue;
                    RV_TB_DimensionL.Enabled = criteria.IsRangeRequired;

                    break;

                case "Dimension_Obs_W":

                    RFV_TB_DimensionW.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_DimensionW.Enabled = criteria.IsRequired;

                    TB_DimensionW.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_DimensionW.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_DimensionW.ValidationExpression = criteria.RegularExpression;
                    REV_TB_DimensionW.Enabled = criteria.IsRegularExpressionRequired;

                    RV_TB_DimensionW.ErrorMessage = criteria.RangeErrorMessage;
                    RV_TB_DimensionW.MinimumValue = criteria.MinimumValue;
                    RV_TB_DimensionW.MaximumValue = criteria.MaximumValue;
                    RV_TB_DimensionW.Enabled = criteria.IsRangeRequired;

                    break;

                case "Dimension_Obs_H":

                    RFV_TB_DimensionH.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_DimensionH.Enabled = criteria.IsRequired;

                    TB_DimensionH.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_DimensionH.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_DimensionH.ValidationExpression = criteria.RegularExpression;
                    REV_TB_DimensionH.Enabled = criteria.IsRegularExpressionRequired;

                    RV_TB_DimensionH.ErrorMessage = criteria.RangeErrorMessage;
                    RV_TB_DimensionH.MinimumValue = criteria.MinimumValue;
                    RV_TB_DimensionH.MaximumValue = criteria.MaximumValue;
                    RV_TB_DimensionH.Enabled = criteria.IsRangeRequired;

                    break;

                case "GSM_Obs":

                    RFV_TB_GSM.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_TB_GSM.Enabled = criteria.IsRequired;

                    TB_DimensionH.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    REV_TB_GSM.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    REV_TB_GSM.ValidationExpression = criteria.RegularExpression;
                    REV_TB_GSM.Enabled = criteria.IsRegularExpressionRequired;

                    RV_TB_GSM.ErrorMessage = criteria.RangeErrorMessage;
                    RV_TB_GSM.MinimumValue = criteria.MinimumValue;
                    RV_TB_GSM.MaximumValue = criteria.MaximumValue;
                    RV_TB_GSM.Enabled = criteria.IsRangeRequired;

                    break;
                default:
                    // Handle unrecognized field names
                    break;
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
        protected void DDL_Plant_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Plant.SelectedIndex != 0)
            {
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                lbl_DDL_Plant_Value.Text = selectedPlantValue;
                //ProductBrandsBinder(selectedPlantValue);
                //BrandSKUBinder(selectedPlantValue);
                LoadApprovers(selectedPlantValue);
            }
            else
            {
                //DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductBrand);

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

        //private void BrandSKUBinder(string selectedPlantValue)
        //{
        //    string query = "SELECT b.SKUId, b.SKU_name FROM MST_Brand_SKU b JOIN MST_LineCatBrands l ON b.brand_id = l.brand_id WHERE l.plant_id = @SelectedPlantValue AND b.ViewMode = 1 ORDER BY b.SKUId;";
        //    string textField = "SKU_name";
        //    string valueField = "SKUId";

        //    bool recordsBound;
        //    DatabaseHelper.BindDropDownList(query, DDL_BrandSKU, textField, valueField, new SqlParameter("@SelectedPlantValue", selectedPlantValue), out recordsBound);

        //    if (!recordsBound)
        //    {
        //        DatabaseHelper.BindWithDefaultNoRecords(DDL_BrandSKU);

        //        string BrandSKUBinder_Error_script = @"<script type='text/javascript'>
        //                    new PNotify({
        //                        title: 'Error',
        //                        text: 'No Records Found!',
        //                        type: 'warning',
        //                        styling: 'bootstrap3'
        //                    });
        //                </script>";
        //        ClientScript.RegisterStartupScript(this.GetType(), "ShowBrandSKUBinderErrorNotification", BrandSKUBinder_Error_script, false);
        //    }
        //}

        private void MakeInputsReadOnly()
        {
            DDL_Plant.Enabled = false;
            //DDL_BrandSKU.Enabled = false;
            TB_BrandSKU.ReadOnly = true;
            TB_ChalanNo.ReadOnly = true;
            TB_ChalanNo.ReadOnly = true;
            TB_MatVarietyName.ReadOnly = true;
            TB_Supplier.ReadOnly = true;
            TB_Size.ReadOnly = true;
            TB_ChalanDate.ReadOnly = true;
            TB_LotNo.ReadOnly = true;
            TB_VehicleNo.ReadOnly = true;
            TB_Std_DimensionL.ReadOnly = true;
            TB_DimensionL.ReadOnly = true;
            TB_Std_DimensionW.ReadOnly = true;
            TB_DimensionW.ReadOnly = true;
            TB_Std_DimensionH.ReadOnly = true;
            TB_DimensionH.ReadOnly = true;
            TB_Std_GSM.ReadOnly = true;
            TB_GSM.ReadOnly = true;
            TB_Remarks.ReadOnly = true;
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

            // RegisterStartupScript adds the JavaScript code to the page
            ClientScript.RegisterStartupScript(this.GetType(), "ShowDataSuccessNotification", Data_SuccessScript, false);
        }
        protected void btnReset_Click(object sender, EventArgs e)
        {
            Response.Redirect("PVC _Tray.aspx");
        }
        private string GenerateUniquePVC_PK()
        {

            string new_PVCID_Value;
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Fetch the maximum PVCID value
                    string query = "SELECT ISNULL(MAX(CAST(SUBSTRING(PVCID, 4, LEN(PVCID) - 3) AS INT)), 0) FROM TRN_PVC_Tray";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        object result = command.ExecuteScalar();
                        int maxPCRValue = Convert.ToInt32(result);

                        // Increment the numeric part
                        int numericPart = maxPCRValue + 1;

                        // Format the new value
                        new_PVCID_Value = $"PVC{numericPart:D3}"; // Ensure three digits (e.g., PVC001, PVC002)
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                Console.WriteLine("Error generating PVC_PK: " + ex.Message);
                throw;
            }

            PVCID = new_PVCID_Value;
            return new_PVCID_Value;
        }
        protected void btnSubmit_Click(object sender, EventArgs e)

        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            string plantName = DDL_Plant.SelectedValue;
            string productBrand = TB_MatVarietyName.Text.ToString();
            //string brandSku = DDL_BrandSKU.SelectedValue;
            string brandSku = TB_BrandSKU.Text.ToString();
            string Supplier_Name = TB_Supplier.Text;
            decimal? sampleSize = !string.IsNullOrEmpty(TB_Size.Text) ? Convert.ToDecimal(TB_Size.Text) : (decimal?)null;
            string Challan_No = TB_ChalanNo.Text;
            DateTime? Challan_Date = string.IsNullOrEmpty(TB_ChalanDate.Text) ? (DateTime?)null : Convert.ToDateTime(TB_ChalanDate.Text);
            string Lot_No = TB_LotNo.Text;
            string Vehicle_No = TB_VehicleNo.Text;
            decimal Dimension_Obs_L = Convert.ToDecimal(TB_DimensionL.Text);
            decimal Dimension_Obs_W = Convert.ToDecimal(TB_DimensionW.Text);
            decimal Dimension_Obs_H = Convert.ToDecimal(TB_DimensionH.Text);
            decimal Dimension_Std_L = Convert.ToDecimal(TB_Std_DimensionL.Text);
            decimal Dimension_Std_W = Convert.ToDecimal(TB_Std_DimensionW.Text);
            decimal Dimension_Std_H = Convert.ToDecimal(TB_Std_DimensionH.Text);
            decimal GSM_Obs = Convert.ToDecimal(TB_GSM.Text);
            decimal GSM_Std = Convert.ToDecimal(TB_Std_GSM.Text);
            string Remarks = TB_Remarks.Text;
            int SubmittedById = Convert.ToInt32(Session["USERID"].ToString());
            string SubmittedByEmployeeCode = Session["WORKMAN"].ToString();
            DateTime SubmittedDate = DateTime.Now.Date;
            TimeSpan SubmittedTime = DateTime.Now.TimeOfDay;

            string approver1EmployeeCode = Approver1CodeLabel.Text;
            string approver2EmployeeCode = Approver2CodeLabel.Text;
            string dottedLineApproverEmployeeCode = DottedLineApproverCodeLabel.Text;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // SQL Insert Query
                    string query = @"INSERT INTO TRN_PVC_Tray (PVCID, FormID, PlantName, ProductBrand, BrandSku, Supplier_Name, SampleSize, Challan_No, Challan_Date, Lot_No, Vehicle_No, Dimension_Std_L, Dimension_Std_W, Dimension_Std_H, GSM_Std, Dimension_Obs_L, Dimension_Obs_W, Dimension_Obs_H, GSM_Obs, Remarks, SubmittedById, SubmittedByEmployeeCode, SubmittedDate, SubmittedTime, Approver1EmployeeCode, Approver2EmployeeCode, DottedLineApproverEmployeeCode, Approver1_Status, Approver2_Status, DottedApprover_Status, SubmissionStatus)
                    VALUES
                    (@PVCID, @FormID, @PlantName, @ProductBrand, @BrandSku, @Supplier_Name, @SampleSize, @Challan_No, @Challan_Date, @Lot_No, @Vehicle_No, @Dimension_Std_L, @Dimension_Std_W, @Dimension_Std_H, @GSM_Std, @Dimension_Obs_L, @Dimension_Obs_W, @Dimension_Obs_H, @GSM_Obs, @Remarks, @SubmittedById, @SubmittedByEmployeeCode, @SubmittedDate, @SubmittedTime, @Approver1EmployeeCode, @Approver2EmployeeCode, @DottedLineApproverEmployeeCode, @Approver1_Status, @Approver2_Status, @DottedApprover_Status, @SubmissionStatus)";


                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        // Add parameters
                        cmd.Parameters.AddWithValue("@PVCID", GenerateUniquePVC_PK());
                        cmd.Parameters.AddWithValue("@FormID", 14);
                        cmd.Parameters.AddWithValue("@PlantName", plantName);
                        cmd.Parameters.AddWithValue("@ProductBrand", productBrand);
                        cmd.Parameters.AddWithValue("@BrandSku", brandSku);
                        cmd.Parameters.AddWithValue("@Supplier_Name", Supplier_Name);
                        cmd.Parameters.AddWithValue("@SampleSize", sampleSize);
                        cmd.Parameters.AddWithValue("@Challan_No", Challan_No);
                        cmd.Parameters.AddWithValue("@Challan_Date", Challan_Date);
                        cmd.Parameters.AddWithValue("@Lot_No", Lot_No);
                        cmd.Parameters.AddWithValue("@Vehicle_No", Vehicle_No);
                        cmd.Parameters.AddWithValue("@Dimension_Std_L", Dimension_Std_L);
                        cmd.Parameters.AddWithValue("@Dimension_Std_W", Dimension_Std_W);
                        cmd.Parameters.AddWithValue("@Dimension_Std_H", Dimension_Std_H);
                        cmd.Parameters.AddWithValue("@GSM_Std", GSM_Std);
                        cmd.Parameters.AddWithValue("@Dimension_Obs_L", Dimension_Obs_L);
                        cmd.Parameters.AddWithValue("@Dimension_Obs_W", Dimension_Obs_W);
                        cmd.Parameters.AddWithValue("@Dimension_Obs_H", Dimension_Obs_H);
                        cmd.Parameters.AddWithValue("@GSM_Obs", GSM_Obs);
                        cmd.Parameters.AddWithValue("@Remarks", Remarks);
                        cmd.Parameters.AddWithValue("@SubmittedById", SubmittedById);
                        cmd.Parameters.AddWithValue("@SubmittedByEmployeeCode", SubmittedByEmployeeCode);
                        cmd.Parameters.AddWithValue("@SubmittedDate", SubmittedDate);
                        cmd.Parameters.AddWithValue("@SubmittedTime", SubmittedTime);
                        cmd.Parameters.AddWithValue("@Approver1EmployeeCode", approver1EmployeeCode);
                        cmd.Parameters.AddWithValue("@Approver2EmployeeCode", approver2EmployeeCode);
                        cmd.Parameters.AddWithValue("@DottedLineApproverEmployeeCode", dottedLineApproverEmployeeCode);
                        cmd.Parameters.AddWithValue("@Approver1_Status", 0);
                        cmd.Parameters.AddWithValue("@Approver2_Status", 0);
                        cmd.Parameters.AddWithValue("@DottedApprover_Status", 0);
                        cmd.Parameters.AddWithValue("@SubmissionStatus", 1);
                        // Execute the query
                        cmd.ExecuteNonQuery();
                        MakeInputsReadOnly();
                    }

                    conn.Close();
                }

            }
            catch (Exception ex)
            {
                string errorMessage = HttpUtility.JavaScriptStringEncode(ex.Message); // Ensure proper encoding
                string errorScript = $@"
                        <script type='text/javascript'>
                            new PNotify({{
                                title: 'Error',
                                text: '{errorMessage}',
                                type: 'error',
                                styling: 'bootstrap3'
                            }});
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowErrorNotification", errorScript, true);
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
                    cmd.Parameters.AddWithValue("@FormID", 14); // Replace with actual value
                    cmd.Parameters.AddWithValue("@FormName", "PM_PVCTrayReport"); // Replace with actual value

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
                            hdn_formid.Value = "14";
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
                            hdn_formid.Value = "14";
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

    }
}