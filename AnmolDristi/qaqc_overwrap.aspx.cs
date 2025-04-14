using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class qaqc_overwrap : System.Web.UI.Page
    {
        private string connectionString = "DbConn";

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
                    lbl_docname.Text = "HM Bag/ PP Bag Report";
                    lbl_docnumber.Text = " ANMOL/DOC/CORP/QC/PKNG/03";
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

        protected void DDL_ProductBrand_SelectedIndexChanged(object sender, EventArgs e)
        {
            //if (DDL_ProductBrand.SelectedIndex != 0)
            //{
            //    string selectedProductBrandValue = DDL_ProductBrand.SelectedValue.ToString();
            //    StandardValueBinder(selectedProductBrandValue);

            //    System.Data.DataTable dataTable = DatabaseHelper.GetBrandFieldsControlByBrandId(Convert.ToInt16(selectedProductBrandValue));


            //    // Example: Querying the DataTable for a specific field name
            //    //string fieldName = "no_of_pcs"; // Specify the field name you want to query
            //    //DataRow[] rows = dataTable.Select($"brand_id = {selectedProductBrandValue} AND field_name = '{fieldName}'");

            //    // Iterate through the filtered rows and extract validation criteria
            //    foreach (DataRow row in dataTable.Rows)
            //    {
            //        // Extract field name from the current row
            //        string fieldName = row["field_name"].ToString();

            //        // Extract validation criteria from the DataRow
            //        bool rfvEnabled = Convert.ToBoolean(row["RFV_YesNo"]);
            //        string rfvErrorMessage = row["RFV_ErrorMsg"].ToString();
            //        bool revEnabled = Convert.ToBoolean(row["REV_YesNo"]);
            //        string revErrorMessage = row["REV_ErrorMsg"].ToString();
            //        string revExpression = row["REV_Expression"].ToString();
            //        bool rvEnabled = Convert.ToBoolean(row["RV_Yesno"]);
            //        string rvErrorMessage = row["RV_ErrorMsg"].ToString();
            //        string rvMinValue = row["RV_MinValue"].ToString();
            //        string rvMaxValue = row["RV_MaxValue"].ToString();

            //        // Create a new instance of ValidationCriteria and populate it with data from the DataRow
            //        ValidationCriteria criteria = new ValidationCriteria();
            //        criteria.RequiredFieldErrorMessage = rfvErrorMessage;
            //        criteria.IsRequired = rfvEnabled;
            //        criteria.RegularExpressionErrorMessage = revErrorMessage;
            //        criteria.IsRegularExpressionRequired = revEnabled;
            //        criteria.RegularExpression = revExpression;
            //        criteria.RangeErrorMessage = rvErrorMessage;
            //        criteria.IsRangeRequired = rvEnabled;
            //        criteria.MinimumValue = rvMinValue;
            //        criteria.MaximumValue = rvMaxValue;

            //        // Use the criteria as needed
            //        // For example, you can pass it to a method to set up validators
            //        //SetUpValidatorsForField(fieldName, criteria);
            //    }
            //}
            //else
            //{
            //    DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductBrand);

            //    string DDL_ProductBrand_Error_script = @"<script type='text/javascript'>
            //                new PNotify({
            //                    title: 'Error',
            //                    text: 'Invalid Selection!',
            //                    type: 'error',
            //                    styling: 'bootstrap3'
            //                });
            //            </script>";
            //    ClientScript.RegisterStartupScript(this.GetType(), "ShowSKUInvalidErrorNotification", DDL_ProductBrand_Error_script, false);
            //}
        }
        private void LoadApprovers(string selectedPlantValue)
        {
            // Replace with your actual connection string
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("Sp_GetFormsApprovalMatrix_PM", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Set parameters for the stored procedure
                    cmd.Parameters.AddWithValue("@PlantId", selectedPlantValue); // Replace with actual value
                    cmd.Parameters.AddWithValue("@FormID", 11); // Replace with actual value
                    cmd.Parameters.AddWithValue("@FormName", "PM_OverwrapReport"); // Replace with actual value

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
                            hdn_formid.Value = "11";
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
        private void StandardValueBinder(string selectedProductBrandValue)
        {
            // Construct the SQL query with parameters
            string query = "SELECT Dimension_Std, GMS_Std FROM PM_Overwrap_StandardValuesControl WHERE brand_id = @SelectedBrandId";

            bool recordsBound;

            // Ensure the literal controls are initialized before passing them
            if (span_Dimension == null || span_GMS == null)
            {
                throw new InvalidOperationException("Literal controls are not initialized.");
            }

            try
            {
                // Call the method to bind values to the Literal controls
                DatabaseHelper.BindLiteralControl(query, span_Dimension, span_GMS,
                    new SqlParameter("@SelectedBrandId", selectedProductBrandValue), out recordsBound);

                if (!recordsBound)
                {
                    //DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductBrand);

                    string BrandSKUBinder_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'No records found!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                    ClientScript.RegisterStartupScript(this.GetType(), "ShowBrandSKUBinderErrorNotification", BrandSKUBinder_Error_script, false);
                }
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

        //protected void btn_overwrap_submit_Click(object sender, EventArgs e)
        //{
        //    if (Page.IsValid)
        //    {
        //        // Call stored procedure to insert or update data
        //        using (SqlConnection conn = new SqlConnection(connectionString))
        //        {
        //            SqlCommand cmd = new SqlCommand("sp_SaveData", conn);
        //            cmd.CommandType = CommandType.StoredProcedure;
        //            cmd.Parameters.AddWithValue("@PlantID", DDL_Plant.SelectedValue);
        //            cmd.Parameters.AddWithValue("@ProductBrandID", DDL_ProductBrand.SelectedValue);
        //            cmd.Parameters.AddWithValue("@SupplierName", TB_SupplierName.Text);
        //            cmd.Parameters.AddWithValue("@ChallanNo", TB_ChallanNo.Text);
        //            cmd.Parameters.AddWithValue("@ChallanDate", TB_ChallanDate.Text);
        //            cmd.Parameters.AddWithValue("@LotGateNo", TB_LotGateNo.Text);
        //            cmd.Parameters.AddWithValue("@VehicleNo", TB_VehicleNo.Text);
        //            cmd.Parameters.AddWithValue("@SealingValue", TB_SealingValue.Text);
        //            cmd.Parameters.AddWithValue("@DimensionStd", TB_DimensionStd.Text);
        //            cmd.Parameters.AddWithValue("@DimensionObs", TB_DimensionObs.Text);
        //            cmd.Parameters.AddWithValue("@GMS_Std", TB_GMS_Std.Text);
        //            cmd.Parameters.AddWithValue("@GSM_Obs", TB_GSM_Obs.Text);
        //            cmd.Parameters.AddWithValue("@Remarks", TB_Remarks.Text);

        //            conn.Open();
        //            cmd.ExecuteNonQuery();
        //            conn.Close();
        //        }
        //        lbl_overwrap.Text = "Data saved successfully!";
        //    }
        //}
        //protected void btn_overwrap_submit_Click(object sender, EventArgs e)
        //{
        //    if (Page.IsValid)
        //    {
        //        // Call stored procedure to insert data into TRN_OVERWRAP table
        //        using (SqlConnection conn = new SqlConnection(connectionString))
        //        {
        //            SqlCommand cmd = new SqlCommand("SP_OVERWRAP", conn);
        //            cmd.CommandType = CommandType.StoredProcedure;

        //            // Pass the form data as parameters to the stored procedure
        //            cmd.Parameters.AddWithValue("@FormID", 123); // Example form ID, update as needed
        //            cmd.Parameters.AddWithValue("@OVRID", GenerateUnique()); // You can generate a unique ID function here
        //            cmd.Parameters.AddWithValue("@SubmittedDate", DateTime.Now.Date);
        //            cmd.Parameters.AddWithValue("@SubmittedTime", DateTime.Now.TimeOfDay);
        //            cmd.Parameters.AddWithValue("@SubmittedById", GetEmployeeId()); // Function to get current user's ID
        //            cmd.Parameters.AddWithValue("@SubmittedByEmployeeCode", GetEmployeeCode()); // Function to get employee code

        //            // Pass input values
        //            cmd.Parameters.AddWithValue("@PlantName", DDL_Plant.SelectedValue);
        //            cmd.Parameters.AddWithValue("@ProductBrand", DDL_ProductBrand.SelectedValue);
        //            cmd.Parameters.AddWithValue("@SupplierName", TB_SupplierName.Text);
        //            cmd.Parameters.AddWithValue("@ChallanNo", TB_ChallanNo.Text);
        //            cmd.Parameters.AddWithValue("@ChallanDate", TB_ChallanDate.Text);
        //            cmd.Parameters.AddWithValue("@LotGateNo", TB_LotGateNo.Text);
        //            cmd.Parameters.AddWithValue("@VehicleNo", TB_VehicleNo.Text);
        //            cmd.Parameters.AddWithValue("@SealingValue", Convert.ToDecimal(TB_SealingValue.Text));
        //            cmd.Parameters.AddWithValue("@DimensionStd", Convert.ToDecimal(TB_DimensionStd.Text));
        //            cmd.Parameters.AddWithValue("@DimensionObs", Convert.ToDecimal(TB_DimensionObs.Text));
        //            cmd.Parameters.AddWithValue("@GMS_Std", Convert.ToDecimal(TB_GMS_Std.Text));
        //            cmd.Parameters.AddWithValue("@GSM_Obs", Convert.ToDecimal(TB_GSM_Obs.Text));
        //            cmd.Parameters.AddWithValue("@Remarks", TB_Remarks.Text);

        //            // Include remarks for any deviations
        //            cmd.Parameters.AddWithValue("@RemarkForDimensionStd", TB_Remark_Dimension.Text ?? (object)DBNull.Value);
        //            cmd.Parameters.AddWithValue("@RemarkForGSM_Std", TB_Remark_GSM.Text ?? (object)DBNull.Value);

        //            // Other columns as per your table structure
        //            cmd.Parameters.AddWithValue("@Approver1_Status", 0); // Default status for Approvers
        //            cmd.Parameters.AddWithValue("@Approver2_Status", 0); // Default status for Approvers
        //            cmd.Parameters.AddWithValue("@DottedApprover_Status", 0); // Default status for Dotted Line Approver
        //            cmd.Parameters.AddWithValue("@ViewMode", 0); // Default View Mode
        //            cmd.Parameters.AddWithValue("@DeleteMode", 0); // Default Delete Mode

        //            // Open the connection and execute the command
        //            conn.Open();
        //            cmd.ExecuteNonQuery();
        //            conn.Close();

        //            // Success message
        //            lbl_overwrap.Text = "Data saved successfully!";
        //        }
        //    }
        //}
        protected void btn_overwrap_submit_Click(object sender, EventArgs e)
        {
            // Define connection string
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            // Generate unique OVRID
            string ovrId = GenerateUnique();  // Use the function to generate unique ID
                                              //int formID = Convert.ToInt32(hdn_formid.Value.ToString());  // Get form ID

            // Retrieve form values
            string plantName = DDL_Plant.SelectedValue;
            string productBrand = TB_MatVarietyName.Text.ToString();
            string supplierName = TB_SupplierName.Text;
            string challanNo = TB_ChallanNo.Text;
            DateTime challanDate = DateTime.Parse(TB_ChallanDate.Text).Date;
            string lotGateNo = TB_LotGateNo.Text;
            string vehicleNo = TB_VehicleNo.Text;
            //decimal sealingValue = Convert.ToDecimal(TB_SealingValue.Text);
            string sealingValue = TB_SealingValue.Text.ToString();
            decimal dimensionStd = Convert.ToDecimal(TB_DimensionStd.Text);
            decimal dimensionObs = Convert.ToDecimal(TB_DimensionObs.Text);
            decimal gmsStd = Convert.ToDecimal(TB_GMS_Std.Text);
            decimal gsmObs = Convert.ToDecimal(TB_GSM_Obs.Text);
            string remarks = TB_Remarks.Text;

            // Additional remarks
            string remarkForDimensionStd = TB_Remark_Dimension.Text;
            Lbl_Remark_GSM.AssociatedControlID = TB_Remark_GSM.Text;

            // Submission data
            DateTime submittedDate = DateTime.Now.Date;
            TimeSpan submittedTime = DateTime.Now.TimeOfDay;
            int submittedById = Convert.ToInt32(Session["USERID"].ToString());  // Get user ID from session
            string submittedByEmployeeCode = Session["WORKMAN"].ToString();  // Get employee code from session

            // Approver details
            string approver1EmployeeCode = Approver1CodeLabel.Text;
            string approver2EmployeeCode = Approver2CodeLabel.Text;
            string dottedLineApproverEmployeeCode = DottedLineApproverCodeLabel.Text;

            try
            {
                // Connect to the database and execute stored procedure
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SP_OVERWRAP", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Add parameters
                        cmd.Parameters.AddWithValue("@OVRID", ovrId);
                        cmd.Parameters.AddWithValue("@FormID", 11);
                        cmd.Parameters.AddWithValue("@SubmittedDate", submittedDate);
                        cmd.Parameters.AddWithValue("@SubmittedTime", submittedTime);
                        cmd.Parameters.AddWithValue("@SubmittedById", submittedById);
                        cmd.Parameters.AddWithValue("@SubmittedByEmployeeCode", submittedByEmployeeCode);

                        // Add form values
                        cmd.Parameters.AddWithValue("@PlantName", plantName);
                        cmd.Parameters.AddWithValue("@ProductBrand", productBrand);
                        cmd.Parameters.AddWithValue("@SupplierName", supplierName);
                        cmd.Parameters.AddWithValue("@ChallanNo", challanNo);
                        cmd.Parameters.AddWithValue("@ChallanDate", challanDate);
                        cmd.Parameters.AddWithValue("@LotGateNo", lotGateNo);
                        cmd.Parameters.AddWithValue("@VehicleNo", vehicleNo);
                        cmd.Parameters.AddWithValue("@SealingValue", sealingValue);
                        cmd.Parameters.AddWithValue("@DimensionStd", dimensionStd);
                        cmd.Parameters.AddWithValue("@DimensionObs", dimensionObs);
                        cmd.Parameters.AddWithValue("@GMS_Std", gmsStd);
                        cmd.Parameters.AddWithValue("@GSM_Obs", gsmObs);
                        cmd.Parameters.AddWithValue("@Remarks", remarks);
                        cmd.Parameters.AddWithValue("@RemarkForDimensionStd", remarkForDimensionStd);
                        cmd.Parameters.AddWithValue("@RemarkForGSM_Std", Lbl_Remark_GSM.AssociatedControlID);

                        // Default status and approvers
                        cmd.Parameters.AddWithValue("@Approver1_Status", 0); // Default status for Approvers
                        cmd.Parameters.AddWithValue("@Approver2_Status", 0); // Default status for Approvers
                        cmd.Parameters.AddWithValue("@DottedApprover_Status", 0); // Default status for Dotted Line Approver
                        cmd.Parameters.AddWithValue("@ViewMode", 1); // Default View Mode
                        cmd.Parameters.AddWithValue("@DeleteMode", 0); // Default Delete Mode

                        // Approver details
                        cmd.Parameters.AddWithValue("@Approver1EmployeeCode", approver1EmployeeCode);
                        cmd.Parameters.AddWithValue("@Approver2EmployeeCode", approver2EmployeeCode);
                        cmd.Parameters.AddWithValue("@DottedLineApproverEmployeeCode", dottedLineApproverEmployeeCode);

                        // Execute the stored procedure
                        cmd.ExecuteNonQuery();
                    }
                    conn.Close();
                }

                bool isReadOnly = true; // Set to false to make inputs editable
                SetReadOnlyMode(isReadOnly);

                btn_overwrap_submit.Enabled = false;
                btn_overwrap_submit.Text = "SAVED";
                btn_overwrap_submit.CssClass = "btn btn-sm btn-success";

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
                // Success message
                //lbl_overwrap.Text = "Data saved successfully!";
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
                // Handle exceptions
                //string errorMessage = ex.Message.Replace("'", "\\'");
                //string errorScript = $"<script type='text/javascript'>new PNotify({{title: 'Error', text: '{errorMessage}', type: 'error', styling: 'bootstrap3'}});</script>";
                //ClientScript.RegisterStartupScript(this.GetType(), "ShowErrorNotification", errorScript, false);
            }
        }

        private void SetReadOnlyMode(bool isReadOnly)
        {
            DDL_Plant.Enabled = false;
            TB_MatVarietyName.ReadOnly = isReadOnly;
            TB_SupplierName.ReadOnly = isReadOnly;
            TB_ChallanNo.ReadOnly = isReadOnly;
            TB_ChallanDate.ReadOnly = isReadOnly;
            TB_LotGateNo.ReadOnly = isReadOnly;
            TB_VehicleNo.ReadOnly = isReadOnly;
            TB_SealingValue.ReadOnly = isReadOnly;
            TB_DimensionStd.ReadOnly = isReadOnly;
            TB_DimensionObs.ReadOnly = isReadOnly;
            TB_GMS_Std.ReadOnly = isReadOnly;
            TB_GSM_Obs.ReadOnly = isReadOnly;
            TB_Remarks.ReadOnly = isReadOnly;
            TB_Remark_Dimension.ReadOnly = isReadOnly;
            TB_Remark_GSM.ReadOnly = isReadOnly;
        }


        private string GenerateUnique()
        {
            string newOvrValue;
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Fetch the maximum OVRID value
                    string query = "SELECT ISNULL(MAX(CAST(SUBSTRING(OVRID, 4, LEN(OVRID)) AS INT)), 0) FROM TRN_OVERWRAP";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        object result = command.ExecuteScalar();
                        int maxOvrValue = Convert.ToInt32(result);

                        // Increment the numeric part
                        int numericPart = maxOvrValue + 1;

                        // Format the new OVRID value
                        newOvrValue = $"OVR{numericPart:D3}"; // Ensure three digits (e.g., OVR001, OVR002)
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                Console.WriteLine("Error generating OVRID: " + ex.Message);
                throw;
            }

            // Return the new OVRID value
            return newOvrValue;
        }

        protected void btn_overwrap_reset_Click(object sender, EventArgs e)
        {
            Response.Redirect("qaqc_overwrap.aspx");
        }


    }
}