using AnmolDristi.DAL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Transactions;
using System.Configuration;

namespace AnmolDristi.qaqc
{
    public partial class qaqc_inspector_rpt : System.Web.UI.Page
    {
        DB_Utility_OH4Y dbcl = new DB_Utility_OH4Y();
        public static string ImgLink1 = string.Empty;
        public static string ImgLink2 = string.Empty;

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

                    lbl_docname.Text = "QC - Inspection Report";
                    lbl_docnumber.Text = "ANMOL/DOC/DAN/QA/02";

                    lbl_QCIR_ClrApp.Attributes.Add("data-prefix", "QCIR/ClrApp");
                    lbl_QCIR_DesignImp.Attributes.Add("data-prefix", "QCIR/DesignImp");

                    PlantBinder();
                    DisplayCurrentShift();
                }

            }
        }

        private void DisplayCurrentShift()
        {
            ShiftManager shiftManager = new ShiftManager();
            string currentShift = shiftManager.GetCurrentShiftType();
            hdn_shiftvalue.Value =currentShift;
        }

        private void LoadApproversOld(string selectedPlantValue, string selectedPlantLineValue)
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
                    cmd.Parameters.AddWithValue("@FormID", 1); // Replace with actual value
                    cmd.Parameters.AddWithValue("@FormName", "qaqc_inspector_rpt"); // Replace with actual value

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
                            // Set default values to ADMIN if no rows are found
                            //Approver1NameLabel.Text = "ADMIN";
                            //Approver1CodeLabel.Text = "ADMIN";

                            //Approver2NameLabel.Text = "ADMIN";
                            //Approver2CodeLabel.Text = "ADMIN";

                            //DottedLineApproverNameLabel.Text = "ADMIN";
                            //DottedLineApproverCodeLabel.Text = "ADMIN";

                            // Insert default record
                            dbcl.InsertDefaultApprovers(selectedPlantValue, selectedPlantLineValue, 1);

                            // Reload after insertion
                            LoadApprovers(selectedPlantValue, selectedPlantLineValue);

                            string PlantBinder_Error_script = @"<script type='text/javascript'>
                                new PNotify({
                                    title: 'Data Success',
                                    text: 'No Approver Mapping Found! Default Approvers Added.',
                                    type: 'success',
                                    styling: 'bootstrap3'
                                });
                            </script>";

                            // RegisterStartupScript adds the JavaScript code to the page
                            ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantBinderErrorNotification", PlantBinder_Error_script, false);
                        }
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
                    cmd.Parameters.AddWithValue("@FormID", 1); // Replace with actual value
                    cmd.Parameters.AddWithValue("@FormName", "qaqc_inspector_rpt"); // Replace with actual value

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        hdn_formid.Value = "1";
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
                            bool isInserted = dbcl.InsertDefaultApprovers(selectedPlantValue, selectedPlantLineValue, 1);

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


        private void SetValidatorPropertiesFromDatabase()
        {
            // Fetch validation criteria from the database for NoOfPcs
            ValidationCriteria noOfPcsValidationCriteria = GetValidationCriteriaFromDatabase("NoOfPcs");

            // Set properties for NoOfPcs validators
            RFV_TB_NoOfPcs.ErrorMessage = noOfPcsValidationCriteria.RequiredFieldErrorMessage;
            RFV_TB_NoOfPcs.Enabled = noOfPcsValidationCriteria.IsRequired;
            REV_TB_NoOfPcs.ErrorMessage = noOfPcsValidationCriteria.RegularExpressionErrorMessage;
            REV_TB_NoOfPcs.ValidationExpression = noOfPcsValidationCriteria.RegularExpression;
            REV_TB_NoOfPcs.Enabled = noOfPcsValidationCriteria.IsRegularExpressionRequired;
            RV_TB_NoOfPcs.ErrorMessage = noOfPcsValidationCriteria.RangeErrorMessage;
            RV_TB_NoOfPcs.MinimumValue = noOfPcsValidationCriteria.MinimumValue;
            RV_TB_NoOfPcs.MaximumValue = noOfPcsValidationCriteria.MaximumValue;
            RV_TB_NoOfPcs.Enabled = noOfPcsValidationCriteria.IsRangeRequired;

            //// Fetch validation criteria from the database for GaugeVal
            //ValidationCriteria gaugeValValidationCriteria = GetValidationCriteriaFromDatabase("GaugeVal");

            //// Set properties for GaugeVal validators
            //RFV_TB_GaugeVal.ErrorMessage = gaugeValValidationCriteria.RequiredFieldErrorMessage;
            //RFV_TB_GaugeVal.Enabled = gaugeValValidationCriteria.IsRequired;
            //REV_TB_GaugeVal.ErrorMessage = gaugeValValidationCriteria.RegularExpressionErrorMessage;
            //REV_TB_GaugeVal.ValidationExpression = gaugeValValidationCriteria.RegularExpression;
            //REV_TB_GaugeVal.Enabled = gaugeValValidationCriteria.IsRegularExpressionRequired;
            //RV_TB_GaugeVal.ErrorMessage = gaugeValValidationCriteria.RangeErrorMessage;
            //RV_TB_GaugeVal.MinimumValue = gaugeValValidationCriteria.MinimumValue;
            //RV_TB_GaugeVal.MaximumValue = gaugeValValidationCriteria.MaximumValue;
            //RV_TB_GaugeVal.Enabled = gaugeValValidationCriteria.IsRangeRequired;
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
        private ValidationCriteria GetValidationCriteriaFromDatabase(string fieldName)
        {
            // Query the database to fetch validation criteria based on the field name
            // Implement database querying logic here, and return the fetched data
            // For example:
            // SELECT * FROM ValidationCriteria WHERE FieldName = fieldName

            // Simulated data for demonstration
            ValidationCriteria criteria = new ValidationCriteria();
            criteria.RequiredFieldErrorMessage = "*";
            criteria.RegularExpressionErrorMessage = "[30-40]";
            criteria.RegularExpression = @"\d+";
            criteria.RangeErrorMessage = "[30-40]";
            criteria.MinimumValue = "30";
            criteria.MaximumValue = "40";
            criteria.IsRequired = true;
            criteria.IsRegularExpressionRequired = true;
            criteria.IsRangeRequired = false;

            return criteria;
        }
        private void NoOfPcs()
        {
            // Set properties of RequiredFieldValidator
            RFV_TB_NoOfPcs.ErrorMessage = "*";
            RFV_TB_NoOfPcs.InitialValue = "0";
            RFV_TB_NoOfPcs.ForeColor = System.Drawing.Color.Red;

            // Set properties of RegularExpressionValidator
            REV_TB_NoOfPcs.ErrorMessage = "Numeric Only";
            REV_TB_NoOfPcs.ForeColor = System.Drawing.Color.Red;
            REV_TB_NoOfPcs.ValidationExpression = @"\d+"; // Regular expression for numeric input

            // Set properties of RangeValidator
            RV_TB_NoOfPcs.ErrorMessage = "[30 - 40]";
            RV_TB_NoOfPcs.ForeColor = System.Drawing.Color.Red;
            RV_TB_NoOfPcs.MinimumValue = "30";
            RV_TB_NoOfPcs.MaximumValue = "40";
        }
        private void GaugeValue()
        {
            //// Set properties of RequiredFieldValidator
            //RFV_TB_GaugeVal.ErrorMessage = "*";
            //RFV_TB_GaugeVal.ForeColor = System.Drawing.Color.Red;

            //// Set properties of RegularExpressionValidator
            //REV_TB_GaugeVal.ErrorMessage = "Decimal Only";
            //REV_TB_GaugeVal.ForeColor = System.Drawing.Color.Red;
            //REV_TB_GaugeVal.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

            //// Set properties of RangeValidator
            //RV_TB_GaugeVal.ErrorMessage = "[10.00 - 100.00]";
            //RV_TB_GaugeVal.ForeColor = System.Drawing.Color.Red;
            //RV_TB_GaugeVal.MinimumValue = "10.00";
            //RV_TB_GaugeVal.MaximumValue = "100.00";
        }
        private void GaugeLength()
        {
            // Set properties of RequiredFieldValidator
            RFV_TB_GaugeLen.ErrorMessage = "*";
            RFV_TB_GaugeLen.ForeColor = System.Drawing.Color.Red;

            // Set properties of RegularExpressionValidator
            REV_TB_GaugeLen.ErrorMessage = "Decimal Only";
            REV_TB_GaugeLen.ForeColor = System.Drawing.Color.Red;
            REV_TB_GaugeLen.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

            // Set properties of RangeValidator
            CV_TB_GaugeLen.ErrorMessage = "[10.00 - 100.00]";
            CV_TB_GaugeLen.ForeColor = System.Drawing.Color.Red;
            //RV_TB_GaugeLen.MinimumValue = "10.00";
            //RV_TB_GaugeLen.MaximumValue = "100.00";
        }
        private void SetDryWeightValidators()
        {
            //// Set properties of RequiredFieldValidator
            //RFV_TB_DryWeight.ErrorMessage = "*";
            //RFV_TB_DryWeight.ForeColor = System.Drawing.Color.Red;

            //// Set properties of RegularExpressionValidator
            //REV_TB_DryWeight.ErrorMessage = "Decimal Only";
            //REV_TB_DryWeight.ForeColor = System.Drawing.Color.Red;
            //REV_TB_DryWeight.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

            //// Set properties of RangeValidator
            ////RV_TB_DryWeight.ErrorMessage = "[10.00 - 100.00]";
            ////RV_TB_DryWeight.ForeColor = System.Drawing.Color.Red;
            ////RV_TB_DryWeight.MinimumValue = "10.00";
            ////RV_TB_DryWeight.MaximumValue = "100.00";
        }
        private void SetDippedWeightValidators()
        {
            //// Set properties of RequiredFieldValidator
            //RFV_TB_DippedWeight.ErrorMessage = "*";
            //RFV_TB_DippedWeight.ForeColor = System.Drawing.Color.Red;

            //// Set properties of RegularExpressionValidator
            //REV_TB_DippedWeight.ErrorMessage = "Decimal Only";
            //REV_TB_DippedWeight.ForeColor = System.Drawing.Color.Red;
            //REV_TB_DippedWeight.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

            //// Set properties of RangeValidator
            //RV_TB_DippedWeight.ErrorMessage = "[0.00 - 1000.00]";
            //RV_TB_DippedWeight.ForeColor = System.Drawing.Color.Red;
            //RV_TB_DippedWeight.MinimumValue = "0.00";
            //RV_TB_DippedWeight.MaximumValue = "1000.00";
        }
        private void SetVartyPktValidators()
        {
            // Set properties of RequiredFieldValidator
            RFV_TB_VartyPkt.ErrorMessage = "*";
            RFV_TB_VartyPkt.ForeColor = System.Drawing.Color.Red;

            // Set properties of RegularExpressionValidator
            REV_TB_VartyPkt.ErrorMessage = "Alphanumeric Only";
            REV_TB_VartyPkt.ForeColor = System.Drawing.Color.Red;
            REV_TB_VartyPkt.ValidationExpression = "^[a-zA-Z0-9]*$"; // Regular expression for alphanumeric input
        }
        private void SetTextureBiteValidators()
        {
            //// Set properties of RequiredFieldValidator
            //RFV_TB_TextureBite.ErrorMessage = "*";
            //RFV_TB_TextureBite.ForeColor = System.Drawing.Color.Red;

            //// Set properties of RegularExpressionValidator
            //REV_TB_TextureBite.ErrorMessage = "Decimal Only";
            //REV_TB_TextureBite.ForeColor = System.Drawing.Color.Red;
            //REV_TB_TextureBite.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

            //// Set properties of RangeValidator
            //RV_TB_TextureBite.ErrorMessage = "Texture bite should be between 0.00 and 10.00";
            //RV_TB_TextureBite.ForeColor = System.Drawing.Color.Red;
            //RV_TB_TextureBite.MinimumValue = "0.00";
            //RV_TB_TextureBite.MaximumValue = "10.00";
        }
        private void SetMoistureValidators()
        {
            // Set properties of RequiredFieldValidator
            RFV_TB_Moisture.ErrorMessage = "*";
            RFV_TB_Moisture.ForeColor = System.Drawing.Color.Red;

            // Set properties of RegularExpressionValidator
            REV_TB_Moisture.ErrorMessage = "Decimal Only";
            REV_TB_Moisture.ForeColor = System.Drawing.Color.Red;
            REV_TB_Moisture.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

            // Set properties of RangeValidator
            RV_TB_Moisture.ErrorMessage = "Moisture should be between 0.00% and 100.00%";
            RV_TB_Moisture.ForeColor = System.Drawing.Color.Red;
            RV_TB_Moisture.MinimumValue = "0.00";
            RV_TB_Moisture.MaximumValue = "100.00";
        }
        private void SetWeightWithOilValidators()
        {
            // Set properties of RequiredFieldValidator
            RFV_TB_wgtwtoil.ErrorMessage = "*";
            RFV_TB_wgtwtoil.ForeColor = System.Drawing.Color.Red;

            // Set properties of RegularExpressionValidator
            REV_TB_wgtwtoil.ErrorMessage = "Decimal Only";
            REV_TB_wgtwtoil.ForeColor = System.Drawing.Color.Red;
            REV_TB_wgtwtoil.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

            // Set properties of RangeValidator
            RV_TB_wgtwtoil.ErrorMessage = "Weight with oil should be between 0.00 and 1000.00 kg";
            RV_TB_wgtwtoil.ForeColor = System.Drawing.Color.Red;
            RV_TB_wgtwtoil.MinimumValue = "0.00";
            RV_TB_wgtwtoil.MaximumValue = "1000.00";
        }
        private void SetWeightWithoutOilValidators()
        {
            // Set properties of RequiredFieldValidator
            RFV_TB_wgtwoil.ErrorMessage = "*";
            RFV_TB_wgtwoil.ForeColor = System.Drawing.Color.Red;

            // Set properties of RegularExpressionValidator
            REV_TB_wgtwoil.ErrorMessage = "Decimal Only";
            REV_TB_wgtwoil.ForeColor = System.Drawing.Color.Red;
            REV_TB_wgtwoil.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

            // Set properties of RangeValidator
            RV_TB_wgtwoil.ErrorMessage = "Weight without oil should be between 0.00 and 1000.00 g";
            RV_TB_wgtwoil.ForeColor = System.Drawing.Color.Red;
            RV_TB_wgtwoil.MinimumValue = "0.00";
            RV_TB_wgtwoil.MaximumValue = "1000.00";
        }
        private void SetOilPercentageValidators()
        {
            // Set properties of RequiredFieldValidator
            RFV_TB_oilpercent.ErrorMessage = "*";
            RFV_TB_oilpercent.ForeColor = System.Drawing.Color.Red;

            // Set properties of RegularExpressionValidator
            REV_TB_oilpercent.ErrorMessage = "Decimal Only";
            REV_TB_oilpercent.ForeColor = System.Drawing.Color.Red;
            REV_TB_oilpercent.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

            // Set properties of RangeValidator
            RV_TB_oilpercent.ErrorMessage = "Oil percentage should be between 0.00% and 100.00%";
            RV_TB_oilpercent.ForeColor = System.Drawing.Color.Red;
            RV_TB_oilpercent.MinimumValue = "0.00";
            RV_TB_oilpercent.MaximumValue = "100.00";
        }
        private void SetPacketWeightValidators()
        {
            // Set properties of RequiredFieldValidator
            RFV_TB_PktWgt.ErrorMessage = "*";
            RFV_TB_PktWgt.ForeColor = System.Drawing.Color.Red;

            // Set properties of RegularExpressionValidator
            REV_TB_PktWgt.ErrorMessage = "Decimal Only";
            REV_TB_PktWgt.ForeColor = System.Drawing.Color.Red;
            REV_TB_PktWgt.ValidationExpression = @"\d+(\.\d{1,2})?"; // Regular expression for decimal input

            // Set properties of RangeValidator
            RV_TB_PktWgt.ErrorMessage = "Packet weight should be between 0.00 and 1000.00 g";
            RV_TB_PktWgt.ForeColor = System.Drawing.Color.Red;
            RV_TB_PktWgt.MinimumValue = "0.00";
            RV_TB_PktWgt.MaximumValue = "1000.00";
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

        private void PlantLinesBinder(string selectedPlantValue)
        {
            string query = "SELECT line_id, line_name FROM MST_Plant_Lines WHERE plant_id = @SelectedPlantValue and view_status=1 and delete_status=0 order by plant_id";
            string textField = "line_name";
            string valueField = "line_id";

            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_PlantLine, textField, valueField, new SqlParameter("@SelectedPlantValue", selectedPlantValue), out recordsBound);

            if (!recordsBound)
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_PlantLine);

                string PlantLinesBinder_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'An error occurred!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantLinesBinderErrorNotification", PlantLinesBinder_Error_script, false);
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

                string DDL_PlantLine_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'Invalid Selection!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantInvalidErrorNotification", DDL_PlantLine_Error_script, false);
            }
        }

        private void LineProductsBinder(string selectedPlantValue, string selectedPlantLineValue)
        {
            // Construct the SQL query with parameters
            string query = "SELECT category_id, category_name FROM MST_LineCategory WHERE plant_id = @PlantId AND line_id = @LineId and view_status=1 and delete_status=0 order by category_id";
            string textField = "category_name"; // Assuming this is the correct field for displaying in the DropDownList
            string valueField = "category_id"; // Assuming this is the correct field for storing in the DropDownList

            // Create SQL parameters for plant_id and line_id
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@PlantId", selectedPlantValue),
                new SqlParameter("@LineId", selectedPlantLineValue)
            };

            // Call the BindDropDownList method with parameters
            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_ProductCategory, textField, valueField, parameters, out recordsBound);

            // Check if any records were bound
            if (!recordsBound)
            {
                string PN_Error_script = @"<script type='text/javascript'>
                    new PNotify({
                        title: 'Error',
                        text: 'No line categories found for the selected plant and line!',
                        type: 'error',
                        styling: 'bootstrap3'
                    });
                </script>";

                // RegisterStartupScript adds the JavaScript code to the page
                ClientScript.RegisterStartupScript(this.GetType(), "ShowLineProductsBinderErrorNotification", PN_Error_script, false);
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

                string DDL_PlantLine_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'Invalid Selection!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantInvalidErrorNotification", DDL_PlantLine_Error_script, false);
            }
        }


        private void ProductBrandsBinder(string selectedPlantValue, string selectedPlantLineValue, string selectedProductCategoryValue)
        {
            // Construct the SQL query with parameters
            string query = "SELECT brand_id, brand_name FROM MST_LineCatBrands WHERE plant_id = @PlantId AND line_id = @LineId and category_id=@CategoryId and view_status=1 and delete_status=0 order by brand_id";
            string textField = "brand_name"; // Assuming this is the correct field for displaying in the DropDownList
            string valueField = "brand_id"; // Assuming this is the correct field for storing in the DropDownList

            // Create SQL parameters for plant_id and line_id
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@PlantId", selectedPlantValue),
                new SqlParameter("@LineId", selectedPlantLineValue),
                new SqlParameter("@CategoryId", selectedProductCategoryValue)
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

        //protected void btnSubmit_Click(object sender, EventArgs e)
        //{
        //    //// Retrieve values from DropDownList controls
        //    //string selectedPlant = DDL_Plant.SelectedValue;
        //    //string selectedPlantLine = DDL_PlantLine.SelectedValue;
        //    //string selectedProductCategory = DDL_ProductCategory.SelectedValue;

        //    //// Retrieve values from TextBox controls
        //    //string noOfPcs = TB_NoOfPcs.Text;
        //    //string gaugeVal = TB_GaugeVal.Text;
        //    //string dryWeight = TB_DryWeight.Text;
        //    //string dippedWeight = TB_DippedWeight.Text;
        //    //string vartyPkt = TB_VartyPkt.Text;
        //    //string bakingTime = TB_BakingTime.Text;
        //    //string textureBite = TB_TextureBite.Text;
        //    //string moisture = TB_Moisture.Text;
        //    //string gaugeVal = TB_GaugeLen.Text;
        //    //string wgtwtoil = TB_wgtwtoil.Text;
        //    //string wgtwoil = TB_wgtwoil.Text;
        //    //string oilPercent = TB_oilpercent.Text;
        //    //string pktWgt = TB_PktWgt.Text;

        //    //// Retrieve values from RadioButtonList controls
        //    //string flavTst = RBL_FlavTst.SelectedValue;
        //    //string flavTstRemarks = TXB_RBL_FlavTst_Rmrks.Text;
        //    //string spSz = RBL_SpSz.SelectedValue;
        //    //string spSzRemarks = TB_RBL_SpSz_Rmrks.Text;

        //    // Further processing or saving logic here


        //    // Retrieve values from controls
        //    string plantName = DDL_Plant.SelectedValue;
        //    string line = DDL_PlantLine.SelectedValue;
        //    string productCategory = DDL_ProductCategory.SelectedValue;
        //    string productBrand = DDL_ProductBrand.SelectedValue; // Assuming DDL_ProductBrand is a DropDownList
        //    string brandSKU = DDL_BrandSKU.SelectedValue;

        //    int numberOfPieces = Convert.ToInt32(TB_NoOfPcs.Text);

        //    decimal gaugeValue = 0;
        //    decimal dryWeight =0;
        //    decimal dippedWeight = 0;

        //    string varietyOrLotNo = !string.IsNullOrEmpty(TB_VartyPkt.Text) ? TB_VartyPkt.Text : null;

        //    string bakingTime = !string.IsNullOrEmpty(TB_BakingTime.Text) ? TB_BakingTime.Text : null;

        //    int flavourAndTaste = Convert.ToInt32(RBL_FlavTst.SelectedValue);
        //    string commentsForFlavourAndTaste = TXB_RBL_FlavTst_Rmrks.Text;

        //    int ColorAppearance = Convert.ToInt32(RBL_ColorApp.SelectedValue);
        //    string CommentsForColorAppearance = TXB_ColorApp_Remarks.Text;

        //    int DesignImplementation = Convert.ToInt32(RBL_DesignImp.SelectedValue);
        //    string CommentsForDesignImplementation = TXB_DesignImp_Remarks.Text;

        //    int TextureBite = Convert.ToInt32(RBL_TextureBite.SelectedValue);
        //    string CommentsForTextureBite = TXB_TextureBite_Remarks.Text;

        //    //decimal textureBite = 0;
        //    //decimal textureBite = Convert.ToDecimal(TB_TextureBite.Text);

        //    //int shapeOrSize = 1;
        //    string shapeOrSize = TB_ShapeSize.Text.ToString();
        //    string commentsForShapeOrSize = string.Empty;

        //    decimal moisture = Convert.ToDecimal(TB_Moisture.Text);

        //    decimal gaugeLength = Convert.ToDecimal(TB_GaugeLen.Text);
        //    string CommentsGaugeLength = TXB_GaugeLen_Remarks.Text;

        //    decimal weightWithoutOil;
        //    decimal result;
        //    if (decimal.TryParse(TB_wgtwtoil.Text, out result))
        //    {
        //        weightWithoutOil = result;
        //    }
        //    else
        //    {
        //        weightWithoutOil = 0; // Or any other default value you choose
        //    }


        //    decimal weightWithOil;
        //    decimal result1;
        //    if (decimal.TryParse(TB_wgtwoil.Text, out result1))
        //    {
        //        weightWithOil = result1;
        //    }
        //    else
        //    {
        //        weightWithOil = 0; // Or any other default value you choose
        //    }

        //    decimal oilPercentValue;
        //    decimal result2;
        //    if (decimal.TryParse(TB_oilpercent.Text, out result2))
        //    {
        //        oilPercentValue = result2; // Set to a default value if parsing fails
        //    }
        //    else
        //    {
        //        oilPercentValue = 0;
        //    }
        //    decimal packetWeight = Convert.ToDecimal(TB_PktWgt.Text);
        //    // Retrieve other values in a similar manner

        //    //string designAndImplementation = "";
        //    //string colourAndAppearance = "";
        //    int submittedById = 1;
        //    string SubmittedByPNo = Session["USERID"].ToString();

        //    string Shift = string.Empty;

        //    QCInspectorDataAccess dataAccess = new QCInspectorDataAccess();

        //    try
        //    {
        //        LogToTextFile(plantName, line, productCategory, productBrand, brandSKU,
        //              numberOfPieces, gaugeValue, dryWeight, dippedWeight,
        //              varietyOrLotNo, bakingTime, ColorAppearance, CommentsForColorAppearance, flavourAndTaste, commentsForFlavourAndTaste, DesignImplementation, CommentsForDesignImplementation,
        //              TextureBite, CommentsForTextureBite, shapeOrSize, commentsForShapeOrSize, moisture,
        //              gaugeLength, CommentsGaugeLength, weightWithoutOil, weightWithOil, oilPercentValue,
        //              packetWeight, ImgLink1, ImgLink2,
        //              submittedById, DateTime.Now.Date, DateTime.Now.TimeOfDay, Shift, SubmittedByPNo);

        //        // Call the InsertQCInspectorData method with the retrieved values
        //        dataAccess.InsertQCInspectorData(plantName, line, productCategory, productBrand, brandSKU,
        //              numberOfPieces, gaugeValue, dryWeight, dippedWeight,
        //              varietyOrLotNo, bakingTime, ColorAppearance, CommentsForColorAppearance,  flavourAndTaste, commentsForFlavourAndTaste, DesignImplementation, CommentsForDesignImplementation, 
        //              TextureBite, CommentsForTextureBite, shapeOrSize, commentsForShapeOrSize, moisture,
        //              gaugeLength, CommentsGaugeLength, weightWithoutOil, weightWithOil, oilPercentValue,
        //              packetWeight, ImgLink1, ImgLink2,
        //              submittedById, DateTime.Now.Date, DateTime.Now.TimeOfDay, Shift, SubmittedByPNo);

        //        //Make the inputs readonly
        //        MakeInputsReadOnly();
        //    }
        //    catch (Exception ex)
        //    {
        //        string errorMessage = ex.Message.Replace("'", "\\'"); // Escape single quotes in the error message
        //        string errorScript = "<script type='text/javascript'>\n" +
        //                             $"new PNotify({{\n" +
        //                             "    title: 'Error',\n" +
        //                             $"    text: '{errorMessage}',\n" +
        //                             "    type: 'error',\n" +
        //                             "    styling: 'bootstrap3'\n" +
        //                             "});\n" +
        //                             "</script>";
        //        ClientScript.RegisterStartupScript(this.GetType(), "ShowErrorNotification", errorScript, false);
        //    }


        //}

        private void LogToTextFile(params object[] data)
        {
            string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
            string targetFolderPath = Server.MapPath("~/UploadedFiles/Logs/");
            string filePath = Path.Combine(targetFolderPath, $"QCInspectorLog_{currentDate}.txt");

            // Check if the directory exists, if not, create it
            if (!Directory.Exists(targetFolderPath))
            {
                Directory.CreateDirectory(targetFolderPath);
            }

            // Check if the file exists, if not, create it
            if (!File.Exists(filePath))
            {
                string headers = string.Join(", ", data.Select((param, index) => $"@param{index + 1}"));
                using (StreamWriter writer = File.CreateText(filePath))
                {
                    writer.WriteLine(headers);
                }
            }

            // Write data to text file
            using (StreamWriter writer = File.AppendText(filePath))
            {
                writer.WriteLine(string.Join(", ", data));
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

            ImgLink1 = string.Empty;
            ImgLink2 = string.Empty;

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

                // RegisterStartupScript adds the JavaScript code to the page
                ClientScript.RegisterStartupScript(this.GetType(), "ShowImage1SuccessNotification", UI_1_Successscript, false);
            }
        }

        private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        private async Task<bool> UploadImage1Async()
        {
            bool imgSaved = false;

            await semaphore.WaitAsync();
            try
            {
                string TBPhotoId = "QCIR";
                DateTime now = DateTime.Now;

                // Define the target folder path
                string targetFolderPath = Server.MapPath("~/UploadedFiles/QCIR/DesignImp/");

                // Check if the target folder exists, if not, create it
                if (!Directory.Exists(targetFolderPath))
                {
                    Directory.CreateDirectory(targetFolderPath);
                }

                // Check if a file is posted
                if (FU_DesgImp.PostedFile != null)
                {
                    // Check the extension of the image
                    string extension = Path.GetExtension(FU_DesgImp.FileName);
                    if (extension.ToLower() == ".png" || extension.ToLower() == ".jpg" || extension.ToLower() == ".jpeg")
                    {
                        // Get the uploaded image stream
                        Stream strm = FU_DesgImp.PostedFile.InputStream;
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
                                ImgLink1 = "~/UploadedFiles/QCIR/DesignImp/" + fileName;
                                //imgfilename = fileName;

                                // Show the image instantly
                                FU_DesgImp_Img.Visible = true;
                                uploadedImage1.ImageUrl = ImgLink1;

                                // Image saved successfully
                                imgSaved = true;

                                FU_DesgImp_Upldr.Visible = false;
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
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                //logger.Error(ex, "An error occurred in RetrieveFormData");
                // Log the exception or display an error message
            }
            finally
            {
                semaphore.Release();
            }

            return imgSaved;
        }

        private bool UploadImage1_old()
        {
            bool imgSaved = false;

            try
            {
                string TBPhotoId = "QCIR";
                DateTime now = DateTime.Now;

                // Define the target folder path
                string targetFolderPath = Server.MapPath("~/UploadedFiles/QCIR/DesignImp/");

                // Check if the target folder exists, if not, create it
                if (!Directory.Exists(targetFolderPath))
                {
                    Directory.CreateDirectory(targetFolderPath);
                }

                // Check if a file is posted
                if (FU_DesgImp.PostedFile != null)
                {
                    // Check the extension of the image
                    string extension = Path.GetExtension(FU_DesgImp.FileName);
                    if (extension.ToLower() == ".png" || extension.ToLower() == ".jpg" || extension.ToLower() == ".jpeg")
                    {
                        // Get the uploaded image stream
                        Stream strm = FU_DesgImp.PostedFile.InputStream;
                        using (var uploadedImage = System.Drawing.Image.FromStream(strm))
                        {
                            // Resize the image
                            //int newWidth = 440; // New Width of Image in Pixel
                            //int newHeight = 540; // New Height of Image in Pixel
                            //using (var resizedImage = new Bitmap(newWidth, newHeight))
                            //{
                            //    using (var graphics = Graphics.FromImage(resizedImage))
                            //    {
                            //        graphics.CompositingQuality = CompositingQuality.HighQuality;
                            //        graphics.SmoothingMode = SmoothingMode.HighQuality;
                            //        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            //        var imgRectangle = new Rectangle(0, 0, newWidth, newHeight);
                            //        graphics.DrawImage(uploadedImage, imgRectangle);

                            //        // Save the resized image to the target folder
                            //        string fileName = $"{TBPhotoId}_{imgDate}.jpg";
                            //        string targetPath = Path.Combine(targetFolderPath, fileName);
                            //        resizedImage.Save(targetPath, System.Drawing.Imaging.ImageFormat.Jpeg);

                            //        // Set the image link for database
                            //        string imglink = "~/UploadedFiles" + fileName;
                            //        //imgfilename = fileName;

                            //        // Show the image instantly
                            //        uploadedImage1.ImageUrl = imglink;

                            //        // Image saved successfully
                            //        imgSaved = true;
                            //    }
                            //}

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
                                ImgLink1 = "~/UploadedFiles/QCIR/DesignImp/" + fileName;
                                hdn_img1.Value = ImgLink1;

                                //imgfilename = fileName;

                                // Show the image instantly
                                FU_DesgImp_Img.Visible = true;
                                uploadedImage1.ImageUrl = ImgLink1;

                                // Image saved successfully
                                imgSaved = true;

                                FU_DesgImp_Upldr.Visible = false;
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
                //logger.Error(ex, "An error occurred in RetrieveFormData");
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                string errorMessage = ex.Message;
                string errorScript = $"new PNotify({{ title: 'Error', text: '{errorMessage}', type: 'error', styling: 'bootstrap3' }});";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowErrorNotification", errorScript, true);
                // Log the exception or display an error message
            }

            return imgSaved;
        }

        private static readonly SemaphoreSlim semaphore2 = new SemaphoreSlim(1, 1);

        private bool UploadImage2()
        {
            bool imgSaved = false;

            try
            {
                string TBPhotoId = "QCIR";
                DateTime now = DateTime.Now;

                // Define the target folder path
                string targetFolderPath = Server.MapPath("~/UploadedFiles/QCIR/ClrApp/");

                // Check if the target folder exists, if not, create it
                if (!Directory.Exists(targetFolderPath))
                {
                    Directory.CreateDirectory(targetFolderPath);
                }

                // Check if a file is posted
                if (FU_ClrApp.PostedFile != null)
                {
                    // Check the extension of the image
                    string extension = Path.GetExtension(FU_ClrApp.FileName);
                    if (extension.ToLower() == ".png" || extension.ToLower() == ".jpg" || extension.ToLower() == ".jpeg")
                    {
                        // Get the uploaded image stream
                        Stream strm = FU_ClrApp.PostedFile.InputStream;
                        using (var uploadedImage = System.Drawing.Image.FromStream(strm))
                        {
                            // Resize the image
                            //int newWidth = 440; // New Width of Image in Pixel
                            //int newHeight = 540; // New Height of Image in Pixel
                            //using (var resizedImage = new Bitmap(newWidth, newHeight))
                            //{
                            //    using (var graphics = Graphics.FromImage(resizedImage))
                            //    {
                            //        graphics.CompositingQuality = CompositingQuality.HighQuality;
                            //        graphics.SmoothingMode = SmoothingMode.HighQuality;
                            //        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            //        var imgRectangle = new Rectangle(0, 0, newWidth, newHeight);
                            //        graphics.DrawImage(uploadedImage, imgRectangle);

                            //        // Save the resized image to the target folder
                            //        string fileName = $"{TBPhotoId}_{imgDate}.jpg";
                            //        string targetPath = Path.Combine(targetFolderPath, fileName);
                            //        resizedImage.Save(targetPath, System.Drawing.Imaging.ImageFormat.Jpeg);

                            //        // Set the image link for database
                            //        string imglink = "~/UploadedFiles" + fileName;
                            //        //imgfilename = fileName;

                            //        // Show the image instantly
                            //        uploadedImage1.ImageUrl = imglink;

                            //        // Image saved successfully
                            //        imgSaved = true;
                            //    }
                            //}

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
                                ImgLink2 = "~/UploadedFiles/QCIR/ClrApp/" + fileName;
                                //imgfilename = fileName;

                                // Show the image instantly
                                FU_ClrApp_Img.Visible = true;
                                uploadedImage2.ImageUrl = ImgLink2;

                                // Image saved successfully
                                imgSaved = true;

                                FU_ClrApp_Upldr.Visible = false;
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
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);
                // Log the exception or display an error message
            }

            return imgSaved;
        }

        //protected void BtnUploadClrApp_Click(object sender, EventArgs e)
        //{
        //    if (UploadImage2() == true)
        //    {
        //        string UI_2_Successscript = @"<script type='text/javascript'>
        //                    new PNotify({
        //                        title: 'Upload Success',
        //                        text: 'Image Saved!!',
        //                        type: 'success',
        //                        styling: 'bootstrap3'
        //                    });
        //                </script>";

        //        // RegisterStartupScript adds the JavaScript code to the page
        //        ClientScript.RegisterStartupScript(this.GetType(), "ShowImage2SuccessNotification", UI_2_Successscript, false);
        //    }
        //}


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

                // RegisterStartupScript adds the JavaScript code to the page
                ClientScript.RegisterStartupScript(this.GetType(), "ShowImage2SuccessNotification", UI_2_Successscript, false);
            }
        }


        private async Task<bool> UploadImage2Async()
        {
            bool imgSaved = false;

            await semaphore2.WaitAsync();
            try
            {
                string TBPhotoId = "QCIR";
                DateTime now = DateTime.Now;

                // Define the target folder path
                string targetFolderPath = Server.MapPath("~/UploadedFiles/QCIR/ClrApp/");

                // Check if the target folder exists, if not, create it
                if (!Directory.Exists(targetFolderPath))
                {
                    Directory.CreateDirectory(targetFolderPath);
                }

                // Check if a file is posted
                if (FU_ClrApp.PostedFile != null)
                {
                    // Check the extension of the image
                    string extension = Path.GetExtension(FU_ClrApp.FileName);
                    if (extension.ToLower() == ".png" || extension.ToLower() == ".jpg" || extension.ToLower() == ".jpeg")
                    {
                        // Get the uploaded image stream
                        Stream strm = FU_ClrApp.PostedFile.InputStream;
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
                                ImgLink2 = "~/UploadedFiles/QCIR/ClrApp/" + fileName;
                                hdn_img2.Value = ImgLink2;
                                //imgfilename = fileName;

                                // Show the image instantly
                                FU_ClrApp_Img.Visible = true;
                                uploadedImage2.ImageUrl = ImgLink2;

                                // Image saved successfully
                                imgSaved = true;

                                FU_ClrApp_Upldr.Visible = false;
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
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                DisplayErrorNotification(ex.Message);
            }
            finally
            {
                semaphore2.Release();
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

                DataTable dataTable = DatabaseHelper.GetBrandFieldsControlByBrandId(Convert.ToInt16(selectedProductBrandValue));


                // Example: Querying the DataTable for a specific field name
                //string fieldName = "no_of_pcs"; // Specify the field name you want to query
                //DataRow[] rows = dataTable.Select($"brand_id = {selectedProductBrandValue} AND field_name = '{fieldName}'");

                // Iterate through the filtered rows and extract validation criteria
                foreach (DataRow row in dataTable.Rows)
                {
                    // Extract field name from the current row
                    string fieldName = row["field_name"].ToString();
                    string displayName = row["DisplayName"].ToString();
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
                    bool isVisible = Convert.ToBoolean(row["ViewMode"]);

                    // Create a new instance of ValidationCriteria and populate it with data from the DataRow
                    ValidationCriteria criteria = new ValidationCriteria();
                    criteria.DisplayName = displayName;
                    criteria.RequiredFieldErrorMessage = rfvErrorMessage;
                    criteria.IsRequired = rfvEnabled;
                    criteria.RegularExpressionErrorMessage = revErrorMessage;
                    criteria.IsRegularExpressionRequired = revEnabled;
                    criteria.RegularExpression = revExpression;
                    criteria.RangeErrorMessage = rvErrorMessage;
                    criteria.IsRangeRequired = rvEnabled;
                    criteria.MinimumValue = rvMinValue;
                    criteria.MaximumValue = rvMaxValue;
                    criteria.Visibility = isVisible;

                    // Use the criteria as needed
                    // For example, you can pass it to a method to set up validators
                    SetUpValidatorsForField(fieldName, criteria);
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
                DatabaseHelper.BindWithDefaultNoRecords(DDL_PlantLine);

                string BrandSKUBinder_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'An error occurred!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowBrandSKUBinderErrorNotification", BrandSKUBinder_Error_script, false);
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

                    //hdnMinNoOfPcs.Value = criteria.MinimumValue.ToString();
                    //hdnMaxNoOfPcs.Value = criteria.MaximumValue.ToString();

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

                    //RV_TB_VartyPkt.ErrorMessage = criteria.RangeErrorMessage;
                    //RV_TB_VartyPkt.MinimumValue = criteria.MinimumValue;
                    //RV_TB_VartyPkt.MaximumValue = criteria.MaximumValue;
                    //RV_TB_VartyPkt.Enabled = criteria.IsRangeRequired;
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

                    //RBL_FlavTst.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    //REV_RBL_FlavTst.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    //REV_RBL_FlavTst.ValidationExpression = criteria.RegularExpression;
                    //REV_RBL_FlavTst.Enabled = criteria.IsRegularExpressionRequired;

                    //RV_TB_DippedWeight.ErrorMessage = criteria.RangeErrorMessage;
                    //RV_TB_DippedWeight.MinimumValue = criteria.MinimumValue;
                    //RV_TB_DippedWeight.MaximumValue = criteria.MaximumValue;
                    //RV_TB_DippedWeight.Enabled = criteria.IsRangeRequired;
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
                    //RV_TB_ShapeSize.ErrorMessage = criteria.RangeErrorMessage;
                    //RV_TB_ShapeSize.MinimumValue = criteria.MinimumValue;
                    //RV_TB_ShapeSize.MaximumValue = criteria.MaximumValue;
                    //RV_TB_ShapeSize.Enabled = criteria.IsRangeRequired;

                    hdnMinShapeSize.Value = criteria.MinimumValue.ToString();
                    hdnMaxShapeSize.Value = criteria.MaximumValue.ToString();
                    break;

                case "TextureBite":

                    Lbl_RBL_TextureBite.Text = criteria.DisplayName;
                    RFV_RBL_TextureBite.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_RBL_TextureBite.Enabled = criteria.IsRequired;

                    //TB_TextureBite.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    //REV_TB_TextureBite.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    //REV_TB_TextureBite.ValidationExpression = criteria.RegularExpression;
                    //REV_TB_TextureBite.Enabled = criteria.IsRegularExpressionRequired;

                    //RV_TB_TextureBite.ErrorMessage = criteria.RangeErrorMessage;
                    //RV_TB_TextureBite.MinimumValue = criteria.MinimumValue;
                    //RV_TB_TextureBite.MaximumValue = criteria.MaximumValue;
                    //RV_TB_TextureBite.Enabled = criteria.IsRangeRequired;
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
                    //CV_TB_GaugeLen.MinimumValue = criteria.MinimumValue;
                    //RV_TB_GaugeLen.MaximumValue = criteria.MaximumValue;
                    CV_TB_GaugeLen.Enabled = criteria.IsRangeRequired;

                    hdnMinGaugelen.Value = criteria.MinimumValue.ToString();
                    hdnMaxGaugelen.Value = criteria.MaximumValue.ToString();

                    break;

                case "wgtwtoil":

                    TB_wgtwtoil_DIV.Visible = criteria.Visibility;
                    Lbl_TB_wgtwtoil.Text = criteria.DisplayName;

                    //weight without oil or dry weight
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

                    //weight with oil or dipped weight
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
                    TB_pHvalue_DIV.Visible = criteria.Visibility;
                    RV_TB_oilpercent.Text = criteria.DisplayName;

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

                    //TB_PktWgt.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    //REV_TB_PktWgt.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    //REV_TB_PktWgt.ValidationExpression = criteria.RegularExpression;
                    //REV_TB_PktWgt.Enabled = criteria.IsRegularExpressionRequired;

                    //RV_TB_PktWgt.ErrorMessage = criteria.RangeErrorMessage;
                    //RV_TB_PktWgt.MinimumValue = criteria.MinimumValue;
                    //RV_TB_PktWgt.MaximumValue = criteria.MaximumValue;
                    //RV_TB_PktWgt.Enabled = criteria.IsRangeRequired;
                    break;

                case "packetview":

                    Lbl_FU_ClrApp.Text = criteria.DisplayName;

                    RFV_FU_ClrApp.ErrorMessage = criteria.RequiredFieldErrorMessage;
                    RFV_FU_ClrApp.Enabled = criteria.IsRequired;

                    //TB_PktWgt.Attributes["placeholder"] = criteria.RangeErrorMessage;

                    //REV_TB_PktWgt.ErrorMessage = criteria.RegularExpressionErrorMessage;
                    //REV_TB_PktWgt.ValidationExpression = criteria.RegularExpression;
                    //REV_TB_PktWgt.Enabled = criteria.IsRegularExpressionRequired;

                    //RV_TB_PktWgt.ErrorMessage = criteria.RangeErrorMessage;
                    //RV_TB_PktWgt.MinimumValue = criteria.MinimumValue;
                    //RV_TB_PktWgt.MaximumValue = criteria.MaximumValue;
                    //RV_TB_PktWgt.Enabled = criteria.IsRangeRequired;
                    break;

                default:
                    // Handle unrecognized field names
                    break;
            }
        }

        protected void btnReset_Click(object sender, EventArgs e)
        {
            Response.Redirect("qaqc_inspector_rpt.aspx");
        }


        //-------------------------------- SUBMIT Code redefined ---------------------------------------//

        private static readonly object _lockObject = new object();

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            lock (_lockObject)
            {
                try
                {
                    var qcData = RetrieveFormData();
                    LogFormData(qcData);
                    InsertQCDataWithTransaction(qcData);

                }
                catch (Exception ex)
                {
                    var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                    EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                    //logger.Error(ex, "An error occurred in RetrieveFormData");
                    DisplayErrorNotification(ex.Message);
                }
            }
        }

        private QCInspectorData RetrieveFormDataOld()
        {
            return new QCInspectorData
            {
                //PlantName = DDL_Plant.SelectedValue,
                //Line = DDL_PlantLine.SelectedValue,
                //ProductCategory = DDL_ProductCategory.SelectedValue,
                //ProductBrand = DDL_ProductBrand.SelectedValue,
                //BrandSKU = DDL_BrandSKU.SelectedValue,

                //NumberOfPieces = Convert.ToInt32(TB_NoOfPcs.Text),
                //VarietyOrLotNo = !string.IsNullOrEmpty(TB_VartyPkt.Text) ? TB_VartyPkt.Text : null,
                //BakingTime = !string.IsNullOrEmpty(TB_BakingTime.Text) ? TB_BakingTime.Text : null,

                //BakingTime2 = !string.IsNullOrEmpty(TB_BakingTime2.Text) ? TB_BakingTime2.Text : null,

                //ColorAppearance = Convert.ToInt32(RBL_ColorApp.SelectedValue),
                //CommentsForColorAppearance = TXB_ColorApp_Remarks.Text,

                //FlavourAndTaste = Convert.ToInt32(RBL_FlavTst.SelectedValue),
                //CommentsForFlavourAndTaste = TXB_RBL_FlavTst_Rmrks.Text,

                //DesignImplementation = Convert.ToInt32(RBL_DesignImp.SelectedValue),
                //CommentsForDesignImplementation = TXB_DesignImp_Remarks.Text,

                //TextureBite = Convert.ToInt32(RBL_TextureBite.SelectedValue),
                //CommentsForTextureBite = TXB_TextureBite_Remarks.Text,

                //ShapeOrSize = TB_ShapeSize.Text,
                //CommentsForShapeOrSize = string.Empty,

                //GaugeValue = Convert.ToDecimal(TB_Length.Text), //Length
                //DryWeight = Convert.ToDecimal(TB_Breadth.Text), //Breadth
                //DippedWeight = Convert.ToDecimal(TB_Height.Text), //Height


                //Moisture = Convert.ToDecimal(TB_Moisture.Text),

                //aWmax = Convert.ToDecimal(TB_aWMAX.Text),
                //pHvalue = Convert.ToDecimal(TB_pHvalue.Text),

                //GaugeLength = Convert.ToDecimal(TB_GaugeLen.Text),
                //CommentsGaugeLength = TXB_GaugeLen_Remarks.Text,

                //WeightWithoutOil = TryParseDecimal(TB_wgtwtoil.Text),
                //WeightWithOil = TryParseDecimal(TB_wgtwoil.Text),
                //OilPercentValue = TryParseDecimal(TB_oilpercent.Text),

                //PacketWeight = Convert.ToDecimal(TB_PktWgt.Text),

                //DesignAndImplementation = ImgLink1,
                //ColourAndAppearance = ImgLink2,

                //SubmittedByPNo = Session["WORKMAN"].ToString(),

                //SubmittedById = Convert.ToInt32(Session["USERID"].ToString()),

                //Shift = string.Empty
            };
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
                    BrandSKU = DDL_BrandSKU.SelectedValue, // Changed BrandSKU to SKUId

                    //NumberOfPieces = Convert.ToInt32(TB_NoOfPcs.Text),
                    NumberOfPieces = TryParseInt(TB_Length.Text) ?? 0,
                    VarietyOrLotNo = !string.IsNullOrEmpty(TB_VartyPkt.Text) ? TB_VartyPkt.Text : null,
                    BakingTime = !string.IsNullOrEmpty(TB_BakingTime.Text) ? TB_BakingTime.Text : null,
                    BakingTime2 = !string.IsNullOrEmpty(TB_BakingTime2.Text) ? TB_BakingTime2.Text : null,

                    ColorAppearance = Convert.ToInt32(RBL_ColorApp.SelectedValue),
                    CommentsForColorAppearance = TXB_ColorApp_Remarks.Text,

                    FlavourAndTaste = Convert.ToInt32(RBL_FlavTst.SelectedValue),
                    CommentsForFlavourAndTaste = TXB_RBL_FlavTst_Rmrks.Text,

                    DesignImplementation = Convert.ToInt32(RBL_DesignImp.SelectedValue),
                    CommentsForDesignImplementation = TXB_DesignImp_Remarks.Text,

                    TextureBite = Convert.ToInt32(RBL_TextureBite.SelectedValue), // Changed to decimal
                    CommentsForTextureBite = TXB_TextureBite_Remarks.Text,

                    ShapeOrSize = !string.IsNullOrEmpty(TB_ShapeSize.Text) ? TB_ShapeSize.Text : null,
                    //ShapeOrSize = TB_ShapeSize.Text,
                    CommentsForShapeOrSize = string.Empty,

                    GaugeValue = TryParseDecimal(TB_Length.Text) ?? 0,
                    DryWeight = TryParseDecimal(TB_Breadth.Text) ?? 0,
                    DippedWeight = TryParseDecimal(TB_Height.Text) ?? 0,

                    Moisture = TryParseDecimal(TB_Moisture.Text) ?? 0,
                    aWmax = TryParseDecimal(TB_aWMAX.Text) ?? 0, // Changed to aW_max
                    pHvalue = TryParseDecimal(TB_pHvalue.Text) ?? 0, // Changed to pH_value

                    GaugeLength = TryParseDecimal(TB_GaugeLen.Text) ?? 0,
                    CommentsGaugeLength = TXB_GaugeLen_Remarks.Text,

                    WeightWithoutOil = TryParseDecimal(TB_wgtwtoil.Text) ?? 0,
                    WeightWithOil = TryParseDecimal(TB_wgtwoil.Text) ?? 0,
                    OilPercentValue = TryParseDecimal(TB_oilpercent.Text) ?? 0, // Changed to OilPercentage

                    PacketWeight = TryParseDecimal(TB_PktWgt.Text) ?? 0,

                    
                    DesignAndImplementation = hdn_img1.Value,
                    ColourAndAppearance = hdn_img2.Value,

                    Remarks = !string.IsNullOrEmpty(TXB_Remarks.Text) ? TXB_Remarks.Text : null,

                    SubmittedByPNo = Session["WORKMAN"].ToString(),
                    SubmittedById = Convert.ToInt32(Session["USERID"].ToString()),

                    Shift = hdn_shiftvalue.Value.ToString(),

                    FormID = 1,

                    Approver1EmployeeCode = Approver1CodeLabel.Text.ToString(),
                    Approver2EmployeeCode = Approver2CodeLabel.Text.ToString(),
                    DottedLineApproverEmployeeCode = DottedLineApproverCodeLabel.Text.ToString()
                };
            }
            catch (FormatException ex)
            {
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                //logger.Error(ex, "An error occurred in RetrieveFormData");
                // Log or display the error message
                // For example, you could use Console.WriteLine or a logging framework
                Console.WriteLine($"FormatException: {ex.Message}");
                throw; 
                // Optional: rethrow the exception if you want it to propagate further
            }
            catch (Exception ex)
            {
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                //logger.Error(ex, "An error occurred in RetrieveFormData");
                // Handle other potential exceptions
                Console.WriteLine($"Exception: {ex.Message}");
                throw; // Optional: rethrow the exception if you want it to propagate further
            }
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




        private void LogFormData(QCInspectorData qcData)
        {
            //LogToTextFile(
            //    qcData.PlantName, qcData.Line, qcData.ProductCategory, qcData.ProductBrand, qcData.BrandSKU,
            //    qcData.NumberOfPieces, qcData.GaugeValue, qcData.DryWeight, qcData.DippedWeight,
            //    qcData.VarietyOrLotNo, qcData.BakingTime, qcData.BakingTime2, qcData.ColorAppearance, qcData.CommentsForColorAppearance,
            //    qcData.FlavourAndTaste, qcData.CommentsForFlavourAndTaste, qcData.DesignImplementation,
            //    qcData.CommentsForDesignImplementation, qcData.TextureBite, qcData.CommentsForTextureBite,
            //    qcData.ShapeOrSize, qcData.CommentsForShapeOrSize, qcData.Moisture, qcData.aWmax, qcData.pHvalue, qcData.GaugeLength,
            //    qcData.CommentsGaugeLength, qcData.WeightWithoutOil, qcData.WeightWithOil, qcData.OilPercentValue,
            //    qcData.PacketWeight, qcData.DesignAndImplementation, qcData.ColourAndAppearance, qcData.SubmittedById, DateTime.Now.Date,
            //    DateTime.Now.TimeOfDay, qcData.Shift, qcData.SubmittedByPNo
            //);

            LogToTextFile(
                qcData.PlantName, qcData.Line, qcData.ProductCategory, qcData.ProductBrand, qcData.BrandSKU, // Changed BrandSKU to SKUId
                qcData.NumberOfPieces, qcData.GaugeValue, qcData.DryWeight, qcData.DippedWeight,
                qcData.VarietyOrLotNo, qcData.BakingTime, qcData.BakingTime2, qcData.ColorAppearance, qcData.CommentsForColorAppearance,
                qcData.FlavourAndTaste, qcData.CommentsForFlavourAndTaste, qcData.DesignImplementation,
                qcData.CommentsForDesignImplementation, qcData.TextureBite, qcData.CommentsForTextureBite,
                qcData.ShapeOrSize, qcData.CommentsForShapeOrSize, qcData.Moisture, qcData.aWmax, // Changed aWmax to aW_max
                qcData.pHvalue, // Changed pHvalue to pH_value
                qcData.GaugeLength, qcData.CommentsGaugeLength, qcData.WeightWithoutOil, qcData.WeightWithOil,
                qcData.OilPercentValue, // Changed OilPercentValue to OilPercentage
                qcData.PacketWeight, qcData.DesignAndImplementation, qcData.ColourAndAppearance, qcData.Remarks, qcData.SubmittedById,
                DateTime.Now.Date, DateTime.Now.TimeOfDay, qcData.Shift, qcData.SubmittedByPNo, qcData.FormID, qcData.Approver1EmployeeCode, qcData.Approver2EmployeeCode, qcData.DottedLineApproverEmployeeCode
            );

        }

        private void InsertQCDataWithTransaction(QCInspectorData qcData)
        {
            using (var transaction = new TransactionScope())
            {
                try
                {
                    var dataAccess = new QCInspectorDataAccess();
                    dataAccess.InsertQCInspectorData(
                        qcData.PlantName, qcData.Line, qcData.ProductCategory, qcData.ProductBrand, qcData.BrandSKU,
                        qcData.NumberOfPieces, qcData.GaugeValue, qcData.DryWeight, qcData.DippedWeight, qcData.VarietyOrLotNo,
                        qcData.BakingTime, qcData.BakingTime2, qcData.ColorAppearance, qcData.CommentsForColorAppearance, qcData.FlavourAndTaste,
                        qcData.CommentsForFlavourAndTaste, qcData.DesignImplementation, qcData.CommentsForDesignImplementation,
                        qcData.TextureBite, qcData.CommentsForTextureBite, qcData.ShapeOrSize, qcData.CommentsForShapeOrSize,
                        qcData.Moisture, qcData.aWmax, qcData.pHvalue, qcData.GaugeLength, qcData.CommentsGaugeLength, qcData.WeightWithoutOil,
                        qcData.WeightWithOil, qcData.OilPercentValue, qcData.PacketWeight, qcData.DesignAndImplementation,
                        qcData.ColourAndAppearance, qcData.Remarks, qcData.SubmittedById, DateTime.Now.Date, DateTime.Now.TimeOfDay,
                        qcData.Shift, qcData.SubmittedByPNo, qcData.FormID, qcData.Approver1EmployeeCode, qcData.Approver2EmployeeCode, qcData.DottedLineApproverEmployeeCode
                    );

                    transaction.Complete();
                    MakeInputsReadOnly();
                }
                catch (Exception ex)
                {
                    var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                    EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                    //logger.Error(ex, "An error occurred in RetrieveFormData");
                    DisplayErrorNotification(ex.Message);
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
                                 "});\n" +
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
            public string SubmittedByEmployeeCode { get; set; }

            public string Approver1EmployeeCode { get; set; }
            public string Approver2EmployeeCode { get; set; }
            public string DottedLineApproverEmployeeCode { get; set; }
        }

    }
}