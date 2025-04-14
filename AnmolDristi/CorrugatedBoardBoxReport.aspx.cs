using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using System.Data;
using AnmolDristi.DAL;
using System.Configuration;



namespace AnmolDristi
{
    public partial class CorrugatedBoardBoxReport : System.Web.UI.Page
    {
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

                    lbl_docname.Text = "Corrugated Board Box Report";
                    lbl_docnumber.Text = "ANMOL/DOC/CORP/QC/PKNG/02";
                    PlantBinder();
                }

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
                ProductBrandsBinder(selectedPlantValue);
                LoadApprovers(selectedPlantValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductBrand);

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

        private void ProductBrandsBinder(string selectedPlantValue)
        {
            // Construct the SQL query with parameters
            //string query = "SELECT brand_id, brand_name FROM MST_LineCatBrands WHERE plant_id = @PlantId ";
            string query = "SELECT c.brand_id, CONCAT (c.brand_name,'[' ,l.line_name, ']') as brand_name  FROM MST_LineCatBrands c, MST_Plant_Lines l WHERE c.plant_id = @PlantId and c.line_id = l.line_id order by l.line_name";
            string textField = "brand_name"; // Assuming this is the correct field for displaying in the DropDownList
            string valueField = "brand_id"; // Assuming this is the correct field for storing in the DropDownList

            // Create SQL parameters for plant_id and line_id
            SqlParameter[] parameters = new SqlParameter[]
            {
        new SqlParameter("@PlantId", selectedPlantValue)
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

        protected void DDL_ProductBrand_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_ProductBrand.SelectedIndex != 0)
            {
                string selectedProductBrandValue = DDL_ProductBrand.SelectedValue.ToString();
                StandardValueBinder(selectedProductBrandValue);
                //BrandSKUBinder(selectedProductBrandValue);

                System.Data.DataTable dataTable = DatabaseHelper.GetBrandFieldsControlByBrandId(Convert.ToInt16(selectedProductBrandValue));


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

        private void BrandSKUBinder(string selectedProductBrandValue)
        {
            string query = "SELECT SKUId, SKU_name FROM MST_Brand_SKU WHERE brand_id = @SelectedPlantValue and ViewMode=1 order by SKUId";
            string textField = "SKU_name";
            string valueField = "SKUId";

            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_BrandSKU, textField, valueField, new SqlParameter("@SelectedPlantValue", selectedProductBrandValue), out recordsBound);

            if (!recordsBound)
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_BrandSKU);

                string BrandSKUBinder_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'No Records Found!',
                                type: 'warning',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowBrandSKUBinderErrorNotification", BrandSKUBinder_Error_script, false);
            }
        }

        private void StandardValueBinder(string selectedProductBrandValue)
        {
            // Construct the SQL query with parameters
            //string query = "SELECT Dimension_Std, GMS_Std FROM PM_Overwrap_StandardValuesControl WHERE brand_id = @SelectedBrandId";

            //bool recordsBound;

            // Ensure the literal controls are initialized before passing them
            //if (span_Dimension == null || span_GMS == null)
            //{
            //    throw new InvalidOperationException("Literal controls are not initialized.");
            //}

            try
            {
                // Call the method to bind values to the Literal controls
                //DatabaseHelper.BindLiteralControl(query, span_Dimension, span_GMS,
                //    new SqlParameter("@SelectedBrandId", selectedProductBrandValue), out recordsBound);

                //if (!recordsBound)
                //{
                //    DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductBrand);

                //    string BrandSKUBinder_Error_script = @"<script type='text/javascript'>
                //    new PNotify({
                //        title: 'Error',
                //        text: 'No records found!',
                //        type: 'error',
                //        styling: 'bootstrap3'
                //    });
                //</script>";
                //    ClientScript.RegisterStartupScript(this.GetType(), "ShowBrandSKUBinderErrorNotification", BrandSKUBinder_Error_script, false);
                //}
            }
            catch (Exception)
            {

            }
            //{
            //    // Handle or log exceptions as needed
            //    string errorScript = $@"<script type='text/javascript'>
            //            new PNotify({title: 'Exception',
            //                text: 'Error: {ex.Message}',
            //                type: 'error',
            //                styling: 'bootstrap3'
            //            });
            //        </script>";
            //    ClientScript.RegisterStartupScript(this.GetType(), "ShowExceptionNotification", errorScript, false);
            //}
        }

        private string GenerateUniqueCBBID()
        {
            string newCBBID;
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Fetch the maximum CBBID value
                    string query = "SELECT ISNULL(MAX(CAST(SUBSTRING(CBBID, 4, LEN(CBBID)) AS INT)), 0) FROM TRN_Corrugated_Board_Box_Report";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        object result = command.ExecuteScalar();
                        int maxCBBIDValue = Convert.ToInt32(result);

                        // Increment the numeric part
                        int numericPart = maxCBBIDValue + 1;

                        // Format the new value, ensuring it's in the desired format (CBB001, CBB002, etc.)
                        newCBBID = $"CBB{numericPart:D3}";
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                Console.WriteLine("Error generating CBBID: " + ex.Message);
                throw;
            }

            return newCBBID;
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            // Assuming GenerateUnique() generates the CBBID
            string cbbId = GenerateUniqueCBBID();
            DateTime submittedDate = DateTime.Now.Date;
            TimeSpan submittedTime = DateTime.Now.TimeOfDay;
              // int submittedById = Convert.ToInt32(Session["USERID"]);
              // string submittedByEmployeeCode = Session["WORKMAN"].ToString();

            string plantName = DDL_Plant.SelectedValue;
            string productBrand = DDL_ProductBrand.SelectedValue;
            string supplierName = TB_SupplierName.Text;
            string challanNo = TB_ChallanNo.Text;
            DateTime challanDate = DateTime.Parse(TB_ChallanDate.Text);
            string lotGateNo = TB_LotNo.Text;
            string vehicleNo = TB_VehicleNo.Text;

            decimal dimensionStdL = Convert.ToDecimal(TB_DimensionStdL.Text);
            decimal dimensionObsL = Convert.ToDecimal(TB_DimensionObsL.Text);
            string remarkForDimensionStdL = string.IsNullOrEmpty(TB_Remark_DimensionL.Text) ? null : TB_Remark_DimensionL.Text;

            decimal dimensionStdW = Convert.ToDecimal(TB_DimensionStdW.Text);
            decimal dimensionObsW = Convert.ToDecimal(TB_DimensionObsW.Text);
            string remarkForDimensionStdW = string.IsNullOrEmpty(TB_Remark_DimensionW.Text) ? null : TB_Remark_DimensionW.Text;

            decimal dimensionStdH = Convert.ToDecimal(TB_DimensionStdH.Text);
            decimal dimensionObsH = Convert.ToDecimal(TB_DimensionObsH.Text);
            string remarkForDimensionStdH = string.IsNullOrEmpty(TB_Remark_DimensionH.Text) ? null : TB_Remark_DimensionH.Text;

            decimal gmsStd = Convert.ToDecimal(TB_GSMStd.Text);
            decimal gsmObs = Convert.ToDecimal(TB_GSMObs.Text);
            string remarkForGsmStd = string.IsNullOrEmpty(TB_Remark_GSMStd.Text) ? null : TB_Remark_GSMStd.Text;

            //Below remarks fields are added on 14-Jan-2025
            decimal bsKgPerCm2 = Convert.ToDecimal(TB_BurstingStrength.Text);
            string bsremarks = string.IsNullOrEmpty(TB_Remarks_BurstingStrength.Text) ? null : TB_Remarks_BurstingStrength.Text;

            decimal compStrength = Convert.ToDecimal(TB_CompressionStrength.Text);
            string csremarks = string.IsNullOrEmpty(TB_Remarks_CompressionStrength.Text) ? null : TB_Remarks_CompressionStrength.Text;

            decimal flutePercentage = Convert.ToDecimal(TB_FlutePercent.Text);
            string fluteremarks = string.IsNullOrEmpty(TB_Remarks_FlutePercent.Text) ? null : TB_Remarks_FlutePercent.Text;

            decimal moisturePercentage = Convert.ToDecimal(TB_MoisturePercent.Text);
            string moistremarks = string.IsNullOrEmpty(TB_Remarks_MoisturePercent.Text) ? null : TB_Remarks_MoisturePercent.Text; 
            string remarks = TB_Remarks.Text;

            string approver1EmployeeCode = Approver1CodeLabel.Text;
            string approver2EmployeeCode = Approver2CodeLabel.Text;
            string dottedLineApproverEmployeeCode = DottedLineApproverCodeLabel.Text;

            // added on 18-Mar-2025
            string noOfPackets = TB_NoOfPkt.Text.Trim();
            decimal sampleSize = Convert.ToDecimal(TB_Size.Text);

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("sp_CorrugatedBoardBoxReport", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Add parameters for the stored procedure
                        command.Parameters.AddWithValue("@CBBID", cbbId);
                        command.Parameters.AddWithValue("@FormID", 1);
                        command.Parameters.AddWithValue("@SubmittedDate", submittedDate);
                        command.Parameters.AddWithValue("@SubmittedTime", submittedTime);
                        command.Parameters.AddWithValue("@SubmittedById",1);
                        command.Parameters.AddWithValue("@SubmittedByEmployeeCode",4);
                        command.Parameters.AddWithValue("@PlantName", plantName);
                        command.Parameters.AddWithValue("@ProductBrand", productBrand);
                        command.Parameters.AddWithValue("@SupplierName", supplierName);
                        command.Parameters.AddWithValue("@ChallanNo", challanNo);
                        command.Parameters.AddWithValue("@ChallanDate", challanDate);
                        command.Parameters.AddWithValue("@LotGateNo", lotGateNo);
                        command.Parameters.AddWithValue("@VehicleNo", vehicleNo);
                        command.Parameters.AddWithValue("@NoOfPackets", noOfPackets);
                        command.Parameters.AddWithValue("@SampleSize", sampleSize);
                        command.Parameters.AddWithValue("@DimensionStd_L", dimensionStdL);
                        command.Parameters.AddWithValue("@RemarkForDimensionStd_L", remarkForDimensionStdL);
                        command.Parameters.AddWithValue("@DimensionStd_W", dimensionStdW);
                        command.Parameters.AddWithValue("@RemarkForDimensionStd_W", remarkForDimensionStdW);
                        command.Parameters.AddWithValue("@DimensionStd_H", dimensionStdH);
                        command.Parameters.AddWithValue("@RemarkForDimensionStd_H", remarkForDimensionStdH);
                        command.Parameters.AddWithValue("@DimensionObs_L", dimensionObsL);
                        command.Parameters.AddWithValue("@DimensionObs_W", dimensionObsW);
                        command.Parameters.AddWithValue("@DimensionObs_H", dimensionObsH);
                        command.Parameters.AddWithValue("@GMS_Std", gmsStd);
                        command.Parameters.AddWithValue("@RemarkForGSM_Std", remarkForGsmStd);
                        command.Parameters.AddWithValue("@GSM_Obs", gsmObs);
                        command.Parameters.AddWithValue("@BS_KgPerCm2", bsKgPerCm2);
                        command.Parameters.AddWithValue("@BS_Remarks", bsremarks);
                        command.Parameters.AddWithValue("@Comp_Strength", compStrength);
                        command.Parameters.AddWithValue("@Comp_Remarks", csremarks);
                        command.Parameters.AddWithValue("@Flute_Percentage", flutePercentage);
                        command.Parameters.AddWithValue("@Flute_Remarks", fluteremarks);
                        command.Parameters.AddWithValue("@Moisture_Percentage", moisturePercentage);
                        command.Parameters.AddWithValue("@Moisture_Remarks", moistremarks);
                        command.Parameters.AddWithValue("@Remarks", remarks);
                        command.Parameters.AddWithValue("@Approver1EmployeeCode", approver1EmployeeCode);
                        command.Parameters.AddWithValue("@Approver2EmployeeCode", approver2EmployeeCode);
                        command.Parameters.AddWithValue("@DottedLineApproverEmployeeCode", dottedLineApproverEmployeeCode);
                        command.Parameters.AddWithValue("@Approver1_Status", 0); // Default status for Approvers
                        command.Parameters.AddWithValue("@Approver2_Status", 0); // Default status for Approvers
                        command.Parameters.AddWithValue("@DottedApprover_Status", 0); // Default status for Dotted Line Approver
                        command.Parameters.AddWithValue("@ViewMode", 0); // Default View Mode
                        command.Parameters.AddWithValue("@DeleteMode", 0); // Default Delete Mode

                        // Execute the stored procedure
                        command.ExecuteNonQuery();
                        MakeInputsReadOnly();
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message.Replace("'", "\\'");
                string errorScript = "<script type='text/javascript'>\n" +
                                     $"new PNotify({{\n" +
                                     "    title: 'Error',\n" +
                                     $"    text: '{errorMessage}',\n" +
                                     "    type: 'error',\n" +
                                     "    styling: 'bootstrap3'\n" +
                                     "}});\n" +
                                     "</script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowErrorNotification", errorScript, false);
            }
        }

        private void MakeInputsReadOnly()
        {
            // Disabling DropDownLists
            DDL_Plant.Enabled = false;
            DDL_ProductBrand.Enabled = false;

            // Making textboxes readonly
            TB_SupplierName.ReadOnly = true;
            TB_ChallanNo.ReadOnly = true;
            TB_ChallanDate.ReadOnly = true;
            TB_LotNo.ReadOnly = true;
            TB_VehicleNo.ReadOnly = true;

            TB_NoOfPkt.ReadOnly = true;
            TB_Size.ReadOnly = true;

            // Dimension fields
            TB_DimensionStdL.ReadOnly = true;
            TB_DimensionObsL.ReadOnly = true;
            TB_Remark_DimensionL.ReadOnly = true;

            TB_DimensionStdW.ReadOnly = true;
            TB_DimensionObsW.ReadOnly = true;
            TB_Remark_DimensionW.ReadOnly = true;

            TB_DimensionStdH.ReadOnly = true;
            TB_DimensionObsH.ReadOnly = true;
            TB_Remark_DimensionH.ReadOnly = true;

            // GSM and strength fields
            TB_GSMStd.ReadOnly = true;
            TB_GSMObs.ReadOnly = true;
            TB_Remark_GSMStd.ReadOnly = true;

            TB_BurstingStrength.ReadOnly = true;
            TB_Remarks_BurstingStrength.ReadOnly = true;

            TB_CompressionStrength.ReadOnly = true;
            TB_Remarks_CompressionStrength.ReadOnly = true;

            TB_FlutePercent.ReadOnly = true;
            TB_Remarks_FlutePercent.ReadOnly=true;

            TB_MoisturePercent.ReadOnly = true;
            TB_Remarks_MoisturePercent.ReadOnly = true;    

            // Remarks
            TB_Remarks.ReadOnly = true;

            // Disable and change the appearance of the Submit button
            btnSubmit.Enabled = false;
            btnSubmit.Text = "SAVED";
            btnSubmit.CssClass = "btn btn-sm btn-success"; // Add any additional styling as needed

            // Notify the user of successful data submission
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
            Response.Redirect("CorrugatedBoardBoxReport.aspx");
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
                    cmd.Parameters.AddWithValue("@FormID", 12); // Replace with actual value
                    cmd.Parameters.AddWithValue("@FormName", "PM_CorrugatedBoardBoxReport"); // Replace with actual value

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
                            hdn_formid.Value = "12";
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
                            hdn_formid.Value = "6";
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