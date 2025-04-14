using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class bopp_tape : System.Web.UI.Page
    {
        public static string BOPPId = string.Empty;
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
                    lbl_docname.Text = "QC - Adhesive Tape Report";
                    lbl_docnumber.Text = "ANMOL/DOC/CORP/QC/PKNG/02 ";
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

        protected void DDL_Plant_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Plant.SelectedIndex != 0)
            {
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                //ProductBrandsBinder(selectedPlantValue);
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
                    cmd.Parameters.AddWithValue("@FormID", 13); // Replace with actual value
                    cmd.Parameters.AddWithValue("@FormName", "PM_BoppTapeReport"); // Replace with actual value

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
                            hdn_formid.Value = "13";
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
                            hdn_formid.Value = "13";
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

        private string GenerateUnique()
        {
            string newBoppId;
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Fetch the maximum BOPPID value
                    string query = "SELECT ISNULL(MAX(CAST(SUBSTRING(BOPPID, 5, LEN(BOPPID)) AS INT)), 0) FROM TRN_BOPP_Tape";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        object result = command.ExecuteScalar();
                        int maxBoppIdValue = Convert.ToInt32(result);

                        // Increment the numeric part
                        int numericPart = maxBoppIdValue + 1;

                        // Format the new value as 'BOPP001', 'BOPP002', etc.
                        newBoppId = $"BOPP{numericPart:D3}";
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                Console.WriteLine("Error generating BOPPID: " + ex.Message);
                throw;
            }

            // Assuming you store the BOPPID somewhere
            return newBoppId;
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

            string boppId = GenerateUnique();  // Generate unique BOPP ID
            int formID;
            if (!int.TryParse(hdn_formid.Value, out formID))
            {
                // Handle invalid int input, for example by logging an error or assigning a default value
                formID = 1; // Assign a default value if parsing fails, or take appropriate action
            }


            string plantName = DDL_Plant.SelectedValue;
            //string productBrand = DDL_ProductBrand.SelectedValue;
            string productBrand = TB_MatVarietyName.Text.ToString();

            decimal sampleSize = Convert.ToDecimal(TB_Size.Text);
            string supplier = TB_SupplierName.Text;
            string challanNo = TB_ChallanNo.Text;
            DateTime challanDate;
            if (!DateTime.TryParse(TB_ChallanDate.Text, out challanDate))
            {
                // Handle invalid date input here, e.g., show a message to the user
                challanDate = DateTime.Now; // Default or handle as appropriate
            }

            string lotGateNo = TB_LotNo.Text;
            string vehicleNo = TB_VehicleNo.Text;
            string printingColour = TB_PrintingColour.Text;
            string adhesiveProperty = TB_AdhesiveProperty.Text;

            // Extract Standard Dimension (Only the numeric part)
            decimal dimensionStd = 0;
            string dimStdInput = TB_StandardDimension.Text.Trim();
            if (!string.IsNullOrEmpty(dimStdInput))
            {
                Regex regex = new Regex(@"^([\d.]+)"); // Match only the first numeric part
                Match match = regex.Match(dimStdInput);
                if (match.Success)
                    dimensionStd = Convert.ToDecimal(match.Groups[1].Value, CultureInfo.InvariantCulture);
            }

            // Extract Observed Dimension
            decimal dimensionObs = 0;
            if (!decimal.TryParse(TB_DimensionObservation.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out dimensionObs))
            {
                dimensionObs = 0; // Handle invalid input
            }

            string remarkForDimensionStd = string.IsNullOrEmpty(TB_StandardDimensionRemarks.Text) ? null : TB_StandardDimensionRemarks.Text;

            // Extract Standard GSM and its tolerance
            decimal gsmStd = 0, gsmTolerancePercent = 0.05m; // Default tolerance 5%
            string gsmStdInput = TB_StandardGSM.Text.Trim();

            Regex gsmRegex = new Regex(@"^([\d.]+)\s*±\s*([\d.]+)%?$"); // Match "30.5 ± 5%"
            Match gsmMatch = gsmRegex.Match(gsmStdInput);

            if (gsmMatch.Success)
            {
                gsmStd = Convert.ToDecimal(gsmMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                gsmTolerancePercent = Convert.ToDecimal(gsmMatch.Groups[2].Value, CultureInfo.InvariantCulture) / 100; // Convert 5% to 0.05
            }
            else
            {
                decimal.TryParse(gsmStdInput, NumberStyles.Any, CultureInfo.InvariantCulture, out gsmStd);
                gsmTolerancePercent = 0.05m; // Default 5% tolerance
            }

            // Extract Observed GSM
            decimal gsmObs = 0;
            if (!decimal.TryParse(TB_GSMObservation.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out gsmObs))
            {
                gsmObs = 0; // Handle invalid input
            }

            string remarkForGsmStd = string.IsNullOrEmpty(TB_GSMRemarks.Text) ? null : TB_GSMRemarks.Text;

            string remarks = TB_Remarks.Text;

            DateTime submittedDate = DateTime.Now.Date;
            TimeSpan submittedTime = DateTime.Now.TimeOfDay;
            int submittedById;
            if (!int.TryParse(Session["USERID"].ToString(), out submittedById))
            {
                // Handle invalid int input, for example by logging an error or assigning a default value
                submittedById = 0; // Assign a default value if parsing fails, or take appropriate action
            }

            string submittedByEmployeeCode = Session["WORKMAN"].ToString();

            string approver1EmployeeCode = Approver1CodeLabel.Text.ToString();
            string approver2EmployeeCode = Approver2CodeLabel.Text.ToString();
            string dottedLineApproverEmployeeCode = DottedLineApproverCodeLabel.Text.ToString();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("SP_TRN_BOPP_Tape", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Add parameters for the table TRN_BOPP_Tape
                        command.Parameters.AddWithValue("@BOPPID", boppId);
                        command.Parameters.AddWithValue("@FormID", formID);
                        command.Parameters.AddWithValue("@PlantName", plantName);
                        command.Parameters.AddWithValue("@ProductBrand", productBrand);
                        command.Parameters.AddWithValue("@SupplierName", supplier);
                        command.Parameters.AddWithValue("@SampleSize", sampleSize);
                        command.Parameters.AddWithValue("@ChallanNo", challanNo);
                        command.Parameters.AddWithValue("@ChallanDate", challanDate);
                        command.Parameters.AddWithValue("@LotGateNo", lotGateNo);
                        command.Parameters.AddWithValue("@VehicleNo", vehicleNo);
                        command.Parameters.AddWithValue("@PrintingColour", printingColour);
                        command.Parameters.AddWithValue("@AdhesiveProperty", adhesiveProperty);

                        command.Parameters.AddWithValue("@DimensionStd", dimensionStd);
                        command.Parameters.AddWithValue("@DimensionObs", dimensionObs);
                        command.Parameters.AddWithValue("@RemarkForDimensionStd", remarkForDimensionStd);

                        command.Parameters.AddWithValue("@GMS_Std", gsmStd);
                        command.Parameters.AddWithValue("@GSM_Obs", gsmObs);
                        command.Parameters.AddWithValue("@RemarkForGSM_Std", remarkForGsmStd);

                        command.Parameters.AddWithValue("@Remarks", remarks);

                        command.Parameters.AddWithValue("@SubmittedDate", submittedDate);
                        command.Parameters.AddWithValue("@SubmittedTime", submittedTime);
                        command.Parameters.AddWithValue("@SubmittedById", submittedById);
                        command.Parameters.AddWithValue("@SubmittedByEmployeeCode", submittedByEmployeeCode);

                        // Default status and approvers
                        command.Parameters.AddWithValue("@Approver1_Status", 0); // Default status for Approvers
                        command.Parameters.AddWithValue("@Approver2_Status", 0); // Default status for Approvers
                        command.Parameters.AddWithValue("@DottedApprover_Status", 0); // Default status for Dotted Line Approver
                        command.Parameters.AddWithValue("@ViewMode", 0); // Default View Mode
                        command.Parameters.AddWithValue("@DeleteMode", 0); // Default Delete Mode

                        command.Parameters.AddWithValue("@Approver1EmployeeCode", approver1EmployeeCode);
                        command.Parameters.AddWithValue("@Approver2EmployeeCode", approver2EmployeeCode);
                        command.Parameters.AddWithValue("@DottedLineApproverEmployeeCode", dottedLineApproverEmployeeCode);

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
            //DDL_ProductBrand.Enabled = false;
            TB_MatVarietyName.ReadOnly = true;
            TB_SupplierName.ReadOnly = true;
            TB_Size.ReadOnly = true;
            TB_ChallanNo.ReadOnly = true;
            TB_ChallanDate.ReadOnly = true;
            TB_LotNo.ReadOnly = true;
            TB_VehicleNo.ReadOnly = true;
            TB_PrintingColour.ReadOnly = true;
            TB_AdhesiveProperty.ReadOnly = true;
            TB_StandardDimension.ReadOnly = true;
            TB_DimensionObservation.ReadOnly = true;
            TB_StandardDimensionRemarks.ReadOnly = true;
            TB_StandardGSM.ReadOnly = true;
            TB_GSMRemarks.ReadOnly = true;
            TB_GSMObservation.ReadOnly = true;
            TB_Remarks.ReadOnly = true;

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

            ClientScript.RegisterStartupScript(this.GetType(), "ShowDataSuccessNotification", Data_SuccessScript, false);
        }


        protected void DDL_ProductBrand_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        protected void btnBoppSave_Click(object sender, EventArgs e)
        {

        }

        protected void btnBoppReset_Click(object sender, EventArgs e)
        {

        }

        //protected void BtnSubmit_Click(object sender, EventArgs e)
        //{

        //}

        protected void BtnReset_Click(object sender, EventArgs e)
        {

        }
    }
}