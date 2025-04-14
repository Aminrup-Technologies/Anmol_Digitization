using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;


namespace AnmolDristi
{
    public partial class PM_LaminateTesting : System.Web.UI.Page
    {
        public static string LtrId = string.Empty;
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


                    lbl_docname.Text = "QC - Daily Laminate Testing Report";
                    lbl_docnumber.Text = "ANMOL/DOC/CORP/QC/PKNG/01";
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

                lbl_DDL_Plant_Value.Text = selectedPlantValue;
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
                    cmd.Parameters.AddWithValue("@FormID", 10); // Replace with actual value
                    cmd.Parameters.AddWithValue("@FormName", "PM_LaminateTesting"); // Replace with actual value

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
                            hdn_formid.Value = "10";
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
        private void ProductBrandsBinder(string selectedPlantValue)
        {
            // Construct the SQL query with parameters
            //string query = "SELECT brand_id, brand_name FROM MST_LineCatBrands WHERE plant_id = @PlantId";
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
                        type: 'warning',
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
                lbl_DDL_ProductBrand_Value.Text = selectedProductBrandValue;
                BrandSKUBinder(selectedProductBrandValue);

                StandardValueBinder(selectedProductBrandValue);


                //System.Data.DataTable dataTable = DatabaseHelper.GetBrandFieldsControlByBrandId(Convert.ToInt16(selectedProductBrandValue));


                // Example: Querying the DataTable for a specific field name
                //string fieldName = "no_of_pcs"; // Specify the field name you want to query
                //DataRow[] rows = dataTable.Select($"brand_id = {selectedProductBrandValue} AND field_name = '{fieldName}'");

                // Iterate through the filtered rows and extract validation criteria
                //foreach (DataRow row in dataTable.Rows)
                //{
                //    // Extract field name from the current row
                //    string fieldName = row["field_name"].ToString();

                //    // Extract validation criteria from the DataRow
                //    bool rfvEnabled = Convert.ToBoolean(row["RFV_YesNo"]);
                //    string rfvErrorMessage = row["RFV_ErrorMsg"].ToString();
                //    bool revEnabled = Convert.ToBoolean(row["REV_YesNo"]);
                //    string revErrorMessage = row["REV_ErrorMsg"].ToString();
                //    string revExpression = row["REV_Expression"].ToString();
                //    bool rvEnabled = Convert.ToBoolean(row["RV_Yesno"]);
                //    string rvErrorMessage = row["RV_ErrorMsg"].ToString();
                //    string rvMinValue = row["RV_MinValue"].ToString();
                //    string rvMaxValue = row["RV_MaxValue"].ToString();

                //    // Create a new instance of ValidationCriteria and populate it with data from the DataRow
                //    ValidationCriteria criteria = new ValidationCriteria();
                //    criteria.RequiredFieldErrorMessage = rfvErrorMessage;
                //    criteria.IsRequired = rfvEnabled;
                //    criteria.RegularExpressionErrorMessage = revErrorMessage;
                //    criteria.IsRegularExpressionRequired = revEnabled;
                //    criteria.RegularExpression = revExpression;
                //    criteria.RangeErrorMessage = rvErrorMessage;
                //    criteria.IsRangeRequired = rvEnabled;
                //    criteria.MinimumValue = rvMinValue;
                //    criteria.MaximumValue = rvMaxValue;

                //    // Use the criteria as needed
                //    // For example, you can pass it to a method to set up validators
                //    //SetUpValidatorsForField(fieldName, criteria);
                //}
            }
            else
            {
               //DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductBrand);

                string DDL_ProductBrand_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error 259 : ',
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
            string query = "SELECT Dimension_Std_L , Dimension_Std_W , Dimension_Std_H , GMS_Std FROM PM_Laminate_StandardValuesControl WHERE brand_id = @SelectedPlantValue";

            bool recordsBound;
            // Ensure the literal controls are initialized before passing them
            if (span_L == null || span_W == null || span_H == null || span_GSM == null)
            {
                throw new InvalidOperationException("Literal controls are not initialized.");
            }

            try
            {
                // Call the method to bind values to the Literal controls
                DatabaseHelper.BindLiteralControl(query, span_L, span_W, span_H, span_GSM, new SqlParameter("@SelectedPlantValue", selectedProductBrandValue), out recordsBound);

                if (!recordsBound)
                {
                    //DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductBrand);



                    string BrandSKUBinder_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'No Records of Standard Value !',
                                type: 'warning',
                                styling: 'bootstrap3'
                            });
                        </script>";
                    ClientScript.RegisterStartupScript(this.GetType(), "ShowBrandSKUBinderErrorNotification", BrandSKUBinder_Error_script, false);
                }
            }
            catch (Exception ex)
            {
                // Optional: handle or log exceptions as needed
                string errorScript = $@"<script type='text/javascript'>
                        new PNotify({{
                            title: 'Exception',
                            text: 'Error: {ex.Message.Replace("'", "\\'")}',
                            type: 'error',
                            styling: 'bootstrap3'
                        }});
                    </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowExceptionNotification", errorScript, false);
            }
            
        }

       
        private string GenerateUnique()
        {

            string newLtrValue;
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Fetch the maximum LTR01 value
                    string query = "SELECT ISNULL(MAX(CAST(SUBSTRING(LTRID, 4, LEN(LTRID)) AS INT)), 0) FROM TRN_PM_LaminateTestingReport";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        object result = command.ExecuteScalar();
                        int maxLTRValue = Convert.ToInt32(result);

                        // Increment the numeric part
                        int numericPart = maxLTRValue + 1;

                        // Format the new value
                        newLtrValue = $"LTR{numericPart:D3}"; // Ensure three digits (e.g., LTR001, LTR002)
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                Console.WriteLine("Error generating LTR01: " + ex.Message);
                throw;
            }

            LtrId = newLtrValue;
            return newLtrValue;
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

            string ltrId = GenerateUnique();  // Generate unique value
            int formID = Convert.ToInt32(hdn_formid.Value.ToString());

            string plantName = DDL_Plant.SelectedValue;
            string productBrand = DDL_ProductBrand.SelectedValue;

            string supplier = string.IsNullOrEmpty(TB_Supplier.Text) ? null : TB_Supplier.Text;
            decimal? sampleSize = !string.IsNullOrEmpty(TB_Size.Text) ? Convert.ToDecimal(TB_Size.Text) : (decimal?)null;

            int smell = Convert.ToInt32(RBL_Smell.SelectedValue);
            string smellRemarks = string.IsNullOrEmpty(TXB_Smell_Remarks.Text) ? null : TXB_Smell_Remarks.Text;

            string challanNo = string.IsNullOrEmpty(TB_ChallanNo.Text) ? null : TB_ChallanNo.Text;
            DateTime challanDate = DateTime.Parse(TB_ChallanDate.Text).Date;

            string lotNo = string.IsNullOrEmpty(TB_LotNo.Text) ? null : TB_LotNo.Text;
            string vehicleNo = string.IsNullOrEmpty(TB_VehicleNo.Text) ? null : TB_VehicleNo.Text;

            decimal? bond = !string.IsNullOrEmpty(TB_Bond.Text) ? Convert.ToDecimal(TB_Bond.Text) : (decimal?)null;
            decimal? seal = !string.IsNullOrEmpty(TB_Seal.Text) ? Convert.ToDecimal(TB_Seal.Text) : (decimal?)null;

            decimal? stdLength = !string.IsNullOrEmpty(TB_Std_DimensionL.Text) ? Convert.ToDecimal(TB_Std_DimensionL.Text) : (decimal?)null;
            decimal? obsLength = !string.IsNullOrEmpty(TB_Length.Text) ? Convert.ToDecimal(TB_Length.Text) : (decimal?)null;
            string lengthRemarks = string.IsNullOrEmpty(TXB_Length_Remarks.Text) ? null : TXB_Length_Remarks.Text;

            decimal? stdWidth = !string.IsNullOrEmpty(TB_Std_DimensionW.Text) ? Convert.ToDecimal(TB_Std_DimensionW.Text) : (decimal?)null;
            decimal? obsWidth = !string.IsNullOrEmpty(TB_Width.Text) ? Convert.ToDecimal(TB_Width.Text) : (decimal?)null;
            string widthRemarks = string.IsNullOrEmpty(TXB_Width_Remarks.Text) ? null : TXB_Width_Remarks.Text;

            decimal? stdHeight = !string.IsNullOrEmpty(TB_Std_DimensionH.Text) ? Convert.ToDecimal(TB_Std_DimensionH.Text) : (decimal?)null;
            decimal? obsHeight = !string.IsNullOrEmpty(TB_Height.Text) ? Convert.ToDecimal(TB_Height.Text) : (decimal?)null;
            string heightRemarks =string.IsNullOrEmpty(TXB_Height_Remarks.Text) ? null : TXB_Height_Remarks.Text;

            decimal? stdGsm = !string.IsNullOrEmpty(TB_Std_GSM.Text) ? Convert.ToDecimal(TB_Std_GSM.Text) : (decimal?)null;
            decimal? obsGsm = !string.IsNullOrEmpty(TB_GSM.Text) ? Convert.ToDecimal(TB_GSM.Text) : (decimal?)null;
            string gsmRemarks = string.IsNullOrEmpty(TXB_GSM_Remarks.Text) ? null : TXB_GSM_Remarks.Text;

            string remarks = string.IsNullOrEmpty(TXB_Remarks.Text) ? null : TXB_Remarks.Text;

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
                    using (SqlCommand command = new SqlCommand("SP_PM_Laminate", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Add parameters
                        command.Parameters.AddWithValue("@LTRID", ltrId);
                        command.Parameters.AddWithValue("@FormID", formID);

                        command.Parameters.AddWithValue("@PlantName", plantName);
                        command.Parameters.AddWithValue("@ProductBrand", productBrand);
                        command.Parameters.AddWithValue("@Supplier_Name", supplier);
                        command.Parameters.AddWithValue("@SampleSize", sampleSize);
                        command.Parameters.AddWithValue("@Smell", smell);
                        command.Parameters.AddWithValue("@Smell_Remarks", smellRemarks);
                        command.Parameters.AddWithValue("@Challan_No", challanNo);
                        command.Parameters.AddWithValue("@Challan_Date", challanDate);
                        command.Parameters.AddWithValue("@Lot_No", lotNo);
                        command.Parameters.AddWithValue("@Vehicle_No", vehicleNo);
                        command.Parameters.AddWithValue("@Bond_Strength", bond);
                        command.Parameters.AddWithValue("@Seal_Strength", seal);

                        command.Parameters.AddWithValue("@Std_Length", stdLength);
                        command.Parameters.AddWithValue("@Obs_Length", obsLength);
                        command.Parameters.AddWithValue("@Length_Remarks", lengthRemarks);

                        command.Parameters.AddWithValue("@Std_Width", stdWidth);
                        command.Parameters.AddWithValue("@Obs_Width", obsWidth);
                        command.Parameters.AddWithValue("@Width_Remarks", widthRemarks);

                        command.Parameters.AddWithValue("@Std_Height", stdHeight);
                        command.Parameters.AddWithValue("@Obs_Height", obsHeight);
                        command.Parameters.AddWithValue("@Height_Remarks", heightRemarks);

                        command.Parameters.AddWithValue("@GMS_Std", stdGsm);
                        command.Parameters.AddWithValue("@GSM_Obs", obsGsm);
                        command.Parameters.AddWithValue("@GSM_Remarks", gsmRemarks);

                        command.Parameters.AddWithValue("@Remarks", remarks);

                        command.Parameters.AddWithValue("@SubmittedDate", submittedDate);
                        command.Parameters.AddWithValue("@SubmittedTime", submittedTime);
                        command.Parameters.AddWithValue("@SubmittedById", submittedById);
                        command.Parameters.AddWithValue("@SubmittedByEmployeeCode", submittedByEmployeeCode);

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
            DDL_ProductBrand.Enabled = false;
            DDL_BrandSKU.Enabled = false;

            TB_Supplier.ReadOnly = true;
            TB_Size.ReadOnly = true;

            RBL_Smell.Enabled = false;
            TXB_Smell_Remarks.ReadOnly = true;

            TB_ChallanNo.ReadOnly = true;
            TB_ChallanDate.ReadOnly = true;

            TB_LotNo.ReadOnly = true;
            TB_VehicleNo.ReadOnly = true;

            TB_Bond.ReadOnly = true;
            TB_Seal.ReadOnly = true;

            TB_Std_DimensionL.ReadOnly = true;
            TB_Length.ReadOnly = true;
            TXB_Length_Remarks.ReadOnly = true;

            TB_Std_DimensionW.ReadOnly = true;
            TB_Width.ReadOnly = true;
            TXB_Width_Remarks.ReadOnly = true;

            TB_Std_DimensionH.ReadOnly = true;
            TB_Height.ReadOnly = true;
            TXB_Height_Remarks.ReadOnly = true;

            TB_Std_GSM.ReadOnly = true;
            TB_GSM.ReadOnly = true;
            TXB_GSM_Remarks.ReadOnly = true;

            TXB_Remarks.ReadOnly = true;

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
            Response.Redirect("PM_LaminateTesting.aspx");
        }
    }
}