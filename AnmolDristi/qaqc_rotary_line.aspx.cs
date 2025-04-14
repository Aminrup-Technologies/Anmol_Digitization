using AnmolDristi.DAL;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;



namespace AnmolDristi
{
    public partial class qaqc_rotary_line : System.Web.UI.Page
    {
        string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        public static String RLWt = String.Empty;
        DB_Utility_OH4Y dbcl = new DB_Utility_OH4Y();

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

                    lbl_docname.Text = "Roatary Line & Oven End Report";
                    lbl_docnumber.Text = "ANMOL/DOC/CORQ/QA/02";
                    PlantBinder();

                    Btn_Save.Enabled = true;
                    btn_rawSubmit.Enabled = false;
                    btnSubmit.Enabled = false;

                    int gridBinderValue = int.Parse(ConfigurationManager.AppSettings["RotaryLineOvenend_GridBinderValue"]);
                    TB_LineNos.Text = gridBinderValue.ToString();
                    TB_OvenNos.Text = gridBinderValue.ToString();

                    GridBinder1(gridBinderValue);
                    GridBinder(gridBinderValue);

                    DisplayCurrentShift();
                }
            }
        }

        private void DisplayCurrentShift()
        {
            ShiftManager shiftManager = new ShiftManager();
            string currentShift = shiftManager.GetCurrentShiftType();
            hdn_shiftvalue.Value = currentShift;
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
            string query = "SELECT line_id, line_name FROM MST_Plant_Lines WHERE plant_id = @SelectedPlantValue";
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
            string query = "SELECT category_id, category_name FROM MST_LineCategory WHERE plant_id = @PlantId AND line_id = @LineId";
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
            string query = "SELECT brand_id, brand_name FROM MST_LineCatBrands WHERE plant_id = @PlantId AND line_id = @LineId and category_id=@CategoryId";
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

        protected void DDL_ProductBrand_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_ProductBrand.SelectedIndex != 0)
            {
                string selectedProductBrandValue = DDL_ProductBrand.SelectedValue.ToString();
                //BrandSKUBinder(selectedProductBrandValue);

                DataTable dataTable = DatabaseHelper.GetBrandFieldsControlByBrandId(Convert.ToInt16(selectedProductBrandValue));


                // Example: Querying the DataTable for a specific field name
                //string fieldName = "no_of_pcs"; // Specify the field name you want to query
                //DataRow[] rows = dataTable.Select($"brand_id = {selectedProductBrandValue} AND field_name = '{fieldName}'");

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

                    // Use the criteria as needed
                    // For example, you can pass it to a method to set up validators
                    //SetUpValidatorsForField(fieldName, criteria);
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

        //private void BrandSKUBinder(string selectedProductBrandValue)
        //{
        //    string query = "SELECT SKUId, SKU_name FROM MST_Brand_SKU WHERE brand_id = @SelectedPlantValue";
        //    string textField = "SKU_name";
        //    string valueField = "SKUId";

        //    bool recordsBound;
        //    DatabaseHelper.BindDropDownList(query, DDL_BrandSKU, textField, valueField, new SqlParameter("@SelectedPlantValue", selectedProductBrandValue), out recordsBound);

        //    if (!recordsBound)
        //    {
        //        DatabaseHelper.BindWithDefaultNoRecords(DDL_PlantLine);

        //        string BrandSKUBinder_Error_script = @"<script type='text/javascript'>
        //                    new PNotify({
        //                        title: 'Error',
        //                        text: 'An error occurred!',
        //                        type: 'error',
        //                        styling: 'bootstrap3'
        //                    });
        //                </script>";
        //        ClientScript.RegisterStartupScript(this.GetType(), "ShowBrandSKUBinderErrorNotification", BrandSKUBinder_Error_script, false);
        //    }
        //}


        protected void Btn_Save_Click(object sender, EventArgs e)
        {
            // Validate and save the data
            SaveData();
            Btn_Save.Enabled = false;
            btn_rawSubmit.Enabled = true;

            ScriptManager.RegisterStartupScript(this, this.GetType(), "SwitchTab", "document.getElementById('rawBiscuts-tab').click();", true);
        }

        private string Find_DBCode()
        {
            string aa = null;
            dbcl.Sqlconnection();
            dbcl.ConnectDb();
            string kk = null;
            string cmdString1 = "select Id,RLWt from TRN_RotaryLine_OvenEnd where Id=(select max(Id)from TRN_RotaryLine_OvenEnd)";
            SqlCommand com1 = new SqlCommand(cmdString1, dbcl.Conn);
            SqlDataReader DR1 = com1.ExecuteReader();
            if (DR1.Read())
            {
                aa = DR1.GetValue(1).ToString();
                string bb = aa.Substring(5);
                int k = Convert.ToInt32(bb);
                k = k + 1;
                string q = Convert.ToString(k);
                kk = "RLOE0" + q;
            }
            else
            {
                kk = "RLOE01";
            }
            dbcl.DisconnectDb();
            //RLWt = kk;
            return kk;
        }


        private void SaveData()
        {
            // Get values from the UI controls
            string plantName = DDL_Plant.SelectedValue;
            string plantLine = DDL_PlantLine.SelectedValue;
            //string brandSKU = DDL_BrandSKU.SelectedValue;
            //string varietyPacket = TB_VartyPkt.Text;

            try
            {

                // Collect form data from your ASP.NET form controls (TextBoxes, DropDownLists, etc.)
                int formID = Convert.ToInt32(hdn_formid.Value.ToString());
                int submittedById = Convert.ToInt32(Session["USERID"].ToString());
                DateTime submittedDate = DateTime.Today; // Assuming submission date is today's date
                TimeSpan submittedTime = DateTime.Now.TimeOfDay; // Assuming submission time is current time
                string shift = hdn_shiftvalue.Value.ToString();
                string submittedByEmployeeCode = Session["WORKMAN"].ToString();
                string line = DDL_PlantLine.SelectedValue;
                string productCategory = DDL_ProductCategory.SelectedValue;
                string productBrand = DDL_ProductBrand.SelectedValue;
                //string skuId = DDL_BrandSKU.SelectedValue;
                string skuId = string.Empty;
                RLWt = Find_DBCode();
                //string variety = TB_VartyPkt.Text.ToString();
                string variety = string.Empty;
                //int viewMode = Convert.ToInt32(ddlViewMode.SelectedValue); // Dropdown for view mode
                //int deleteMode = Convert.ToInt32(ddlDeleteMode.SelectedValue); // Dropdown for delete mode

                // Optional parameters
                //int? approver1Status = string.IsNullOrEmpty(txtApprover1Status.Text) ? (int?)null : Convert.ToInt32(txtApprover1Status.Text);
                //DateTime? approver1TimeStamp = string.IsNullOrEmpty(txtApprover1TimeStamp.Text) ? (DateTime?)null : Convert.ToDateTime(txtApprover1TimeStamp.Text);

                //int? approver2Status = string.IsNullOrEmpty(txtApprover2Status.Text) ? (int?)null : Convert.ToInt32(txtApprover2Status.Text);
                //DateTime? approver2TimeStamp = string.IsNullOrEmpty(txtApprover2TimeStamp.Text) ? (DateTime?)null : Convert.ToDateTime(txtApprover2TimeStamp.Text);

                //int? dottedApproverStatus = string.IsNullOrEmpty(txtDottedApproverStatus.Text) ? (int?)null : Convert.ToInt32(txtDottedApproverStatus.Text);
                //DateTime? dottedApproverTimeStamp = string.IsNullOrEmpty(txtDottedApproverTimeStamp.Text) ? (DateTime?)null : Convert.ToDateTime(txtDottedApproverTimeStamp.Text);

                string approver1EmployeeCode = Approver1CodeLabel.Text.ToString();
                string approver2EmployeeCode = Approver2CodeLabel.Text.ToString();
                string dottedLineApproverEmployeeCode = DottedLineApproverCodeLabel.Text.ToString();

                //string linewt = txtLineWeight.Text;
                //string gaugeandweight = txtGaugeAndWeight.Text;
                //decimal? avglinewt = string.IsNullOrEmpty(txtAvgLineWeight.Text) ? (decimal?)null : Convert.ToDecimal(txtAvgLineWeight.Text);
                //decimal? avggaugevalue = string.IsNullOrEmpty(txtAvgGaugeValue.Text) ? (decimal?)null : Convert.ToDecimal(txtAvgGaugeValue.Text);
                //decimal? avgweightvalue = string.IsNullOrEmpty(txtAvgWeightValue.Text) ? (decimal?)null : Convert.ToDecimal(txtAvgWeightValue.Text);

                // Call the Insert method
                InsertRotaryLineOvenEnd(
                    formID, submittedById, submittedDate, submittedTime, shift, submittedByEmployeeCode,
                    plantName, line, productCategory, productBrand, skuId, RLWt, variety,
                    approver1EmployeeCode,
                    approver2EmployeeCode,
                    dottedLineApproverEmployeeCode
                );

                Btn_Save.Enabled = false;
                Btn_Save.Text = "SAVED";

                //TB_VartyPkt.ReadOnly = true;

                lblMessage.Text = "Data inserted successfully!";

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
            catch (Exception ex)
            {
                lblMessage.Text = "An error occurred: " + ex.Message;
                string errorMessage = ex.Message.Replace("'", "\\'"); // Escape single quotes in the error message
                string errorScript1 = "<script type='text/javascript'>\n" +
                                     $"new PNotify({{\n" +
                                     "    title: 'Error',\n" +
                                     $"    text: '{errorMessage}',\n" +
                                     "    type: 'error',\n" +
                                     "    styling: 'bootstrap3'\n" +
                                     "});\n" +
                                     "</script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowErrorNotification1", errorScript1, false);
            }
        }

        public void InsertRotaryLineOvenEnd(
        int formID, int submittedById, DateTime submittedDate, TimeSpan submittedTime, string shift,
        string submittedByEmployeeCode, string plantName, string line, string productCategory,
        string productBrand, string skuId, string rlWt, string variety,
        string approver1EmployeeCode,
        string approver2EmployeeCode,
        string dottedLineApproverEmployeeCode)
        {
            try
            {
                // Get the connection string from Web.config
                string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("InsertRotaryLineOvenEnd", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Add parameters
                        cmd.Parameters.AddWithValue("@FormID", formID);
                        cmd.Parameters.AddWithValue("@SubmittedById", submittedById);
                        cmd.Parameters.AddWithValue("@SubmittedDate", submittedDate);
                        cmd.Parameters.AddWithValue("@SubmittedTime", submittedTime);
                        cmd.Parameters.AddWithValue("@Shift", shift);
                        cmd.Parameters.AddWithValue("@SubmittedByEmployeeCode", submittedByEmployeeCode);
                        cmd.Parameters.AddWithValue("@PlantName", plantName);
                        cmd.Parameters.AddWithValue("@Line", line);
                        cmd.Parameters.AddWithValue("@ProductCategory", productCategory);
                        cmd.Parameters.AddWithValue("@ProductBrand", productBrand);
                        cmd.Parameters.AddWithValue("@SKUId", skuId);
                        cmd.Parameters.AddWithValue("@RLWt", rlWt);
                        cmd.Parameters.AddWithValue("@Variety", variety);
                        //cmd.Parameters.AddWithValue("@ViewMode", viewMode);
                        //cmd.Parameters.AddWithValue("@DeleteMode", deleteMode);

                        cmd.Parameters.AddWithValue("@Approver1EmployeeCode", (object)approver1EmployeeCode ?? DBNull.Value);
                        //cmd.Parameters.AddWithValue("@Approver1_Status", (object)approver1Status ?? DBNull.Value);
                        //cmd.Parameters.AddWithValue("@Approver1_TimeStamp", (object)approver1TimeStamp ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Approver2EmployeeCode", (object)approver2EmployeeCode ?? DBNull.Value);
                        //cmd.Parameters.AddWithValue("@Approver2_Status", (object)approver2Status ?? DBNull.Value);
                        //cmd.Parameters.AddWithValue("@Approver2_TimeStamp", (object)approver2TimeStamp ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DottedLineApproverEmployeeCode", (object)dottedLineApproverEmployeeCode ?? DBNull.Value);
                        //cmd.Parameters.AddWithValue("@DottedApprover_Status", (object)dottedApproverStatus ?? DBNull.Value);
                        //cmd.Parameters.AddWithValue("@DottedApprover_TimeStamp", (object)dottedApproverTimeStamp ?? DBNull.Value);

                        //cmd.Parameters.AddWithValue("@linewt", (object)linewt ?? DBNull.Value);
                        //cmd.Parameters.AddWithValue("@gaugeandweight", (object)gaugeandweight ?? DBNull.Value);
                        //cmd.Parameters.AddWithValue("@avglinewt", (object)avglinewt ?? DBNull.Value);
                        //cmd.Parameters.AddWithValue("@avggaugevalue", (object)avggaugevalue ?? DBNull.Value);
                        //cmd.Parameters.AddWithValue("@avgweightvalue", (object)avgweightvalue ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@Panel1_Status", 1);
                        cmd.Parameters.AddWithValue("@Panel1_Timestamp",DateTime.Now);
                        cmd.Parameters.AddWithValue("@Panel2_Status", 0);
                        cmd.Parameters.AddWithValue("@Panel2_Timestamp", DBNull.Value);
                        cmd.Parameters.AddWithValue("@Panel3_Status", 0);
                        cmd.Parameters.AddWithValue("@Panel3_Timestamp", DBNull.Value);


                        // Open connection and execute the command
                        conn.Open();
                        cmd.ExecuteNonQuery();

                        //string InsertRotaryLineOvenEnd_SuccessScript = @"<script type='text/javascript'>
                        //    new PNotify({
                        //        title: 'Data Success',
                        //        text: 'Recorded Successfully!!',
                        //        type: 'success',
                        //        styling: 'bootstrap3'
                        //    });
                        //</script>";
                        //ClientScript.RegisterStartupScript(this.GetType(), "InsertRotaryLineOvenEnd_SuccessNotification", InsertRotaryLineOvenEnd_SuccessScript, false);
                    }
                }
            }
            catch (Exception ex)
            {
                string InsertRotaryLineOvenEnd_ErrorMessage = ex.Message.Replace("'", "\\'"); // Escape single quotes in the error message
                string InsertRotaryLineOvenEnd_ErrorScript = "<script type='text/javascript'>\n" +
                                     $"new PNotify({{\n" +
                                     "    title: 'Error',\n" +
                                     $"    text: '{InsertRotaryLineOvenEnd_ErrorMessage}',\n" +
                                     "    type: 'error',\n" +
                                     "    styling: 'bootstrap3'\n" +
                                     "});\n" +
                                     "</script>";
                ClientScript.RegisterStartupScript(this.GetType(), "InsertRotaryLineOvenEnd_ErrorNotification", InsertRotaryLineOvenEnd_ErrorScript, false);
            }
        }

        private void LoadApproversOld(string selectedPlantValue, string selectedPlantLineValue)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("usp_GetFormsApprovalMatrix", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@PlantId", selectedPlantValue);
                    cmd.Parameters.AddWithValue("@LineId", selectedPlantLineValue);
                    cmd.Parameters.AddWithValue("@FormID", 3);
                    cmd.Parameters.AddWithValue("@FormName", "qaqc_rotary_line");

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        hdn_formid.Value = "3";
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        GridViewApprovers.DataSource = dt;
                        GridViewApprovers.DataBind();

                        if (dt.Rows.Count > 0)
                        {
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
                            // Set default values to ADMIN if no rows are found
                            //Approver1NameLabel.Text = "ADMIN";
                            //Approver1CodeLabel.Text = "ADMIN";

                            //Approver2NameLabel.Text = "ADMIN";
                            //Approver2CodeLabel.Text = "ADMIN";

                            //DottedLineApproverNameLabel.Text = "ADMIN";
                            //DottedLineApproverCodeLabel.Text = "ADMIN";

                            // Insert default record
                            dbcl.InsertDefaultApprovers(selectedPlantValue, selectedPlantLineValue, 3);

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
                    cmd.Parameters.AddWithValue("@FormID", 3);
                    cmd.Parameters.AddWithValue("@FormName", "qaqc_rotary_line");

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        hdn_formid.Value = "3";
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
                            bool isInserted = dbcl.InsertDefaultApprovers(selectedPlantValue, selectedPlantLineValue, 3);

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


        private string GenerateUniqueRLWT01()
        {

            string newRlValue;
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Fetch the maximum RLW01 value
                    string query = "SELECT ISNULL(MAX(CAST(SUBSTRING(RLWT, 4, LEN(RLWT)) AS INT)), 0) FROM BasicRl_Data_Table";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        object result = command.ExecuteScalar();
                        int maxRlValue = Convert.ToInt32(result);

                        // Increment the numeric part
                        int numericPart = maxRlValue + 1;

                        // Format the new value
                        newRlValue = $"RLW{numericPart:D3}"; // Ensure three digits (e.g., RLW001, RLW002)
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                Console.WriteLine("Error generating RLW01: " + ex.Message);
                throw;
            }

            RLWt = newRlValue;
            return newRlValue;
        }

        private string GetCurrentShift()
        {
            TimeSpan currentTime = DateTime.Now.TimeOfDay;

            // Define shift times
            TimeSpan shiftAStart = new TimeSpan(6, 0, 0);  // 06:00 AM
            TimeSpan shiftAEnd = new TimeSpan(14, 0, 0);   // 02:00 PM
            TimeSpan shiftBStart = new TimeSpan(14, 0, 0); // 02:00 PM
            TimeSpan shiftBEnd = new TimeSpan(22, 0, 0);   // 10:00 PM
            TimeSpan shiftCStart = new TimeSpan(22, 0, 0); // 10:00 PM
            TimeSpan shiftCEnd = new TimeSpan(6, 0, 0);    // 06:00 AM (next day)

            // Determine the current shift based on time
            if (currentTime >= shiftAStart && currentTime < shiftAEnd)
            {
                return "A";  // Morning Shift
            }
            else if (currentTime >= shiftBStart && currentTime < shiftBEnd)
            {
                return "B";  // Afternoon Shift
            }
            else if (currentTime >= shiftCStart || currentTime < shiftCEnd)
            {
                return "C";  // Night Shift
            }

            return "Unknown";
        }

        protected void btnReset_Click(object sender, EventArgs e)
        {
            //// Clear all the controls
            DDL_Plant.SelectedIndex = 0;
            DDL_PlantLine.SelectedIndex = 0;
            DDL_ProductCategory.SelectedIndex = 0;
            DDL_ProductBrand.SelectedIndex = 0;
            //DDL_BrandSKU.SelectedIndex = 0;
            //TB_VartyPkt.Text = string.Empty;
            lblMessage.Text = string.Empty;
        }

        /// <summary>
        /// Raw wt line rpt
        /// </summary>

        private void GridBinder1(int rowCount)
        {
            DataTable dt = new DataTable();

            // Create columns
            dt.Columns.Add("Sl", typeof(int)); // Serial number column
            dt.Columns.Add("stlWeightValue", typeof(decimal)); // Column for stl weight values
            dt.Columns.Add("edlWeightValue", typeof(decimal)); // Column for edl weight values

            for (int i = 1; i <= rowCount; i++)
            {
                DataRow dr = dt.NewRow();
                dr["Sl"] = i; // Serial number
                dt.Rows.Add(dr);
            }

            LineWeights_Grid.DataSource = dt;
            LineWeights_Grid.DataBind();
        }

        public class WeightData
        {
            public int sl { get; set; }
            public decimal weight { get; set; }
        }

        protected void btn_rawSubmit_Click(object sender, EventArgs e)
        {
            if (Btn_Save.Text == "SAVED" && RLWt != string.Empty)
            {
                Dictionary<int, decimal> stlweightData = new Dictionary<int, decimal>();
                List<WeightData> weightDataList = new List<WeightData>();

                decimal totalWeight = 0;
                int weightCount = 0;

                foreach (GridViewRow row in LineWeights_Grid.Rows)
                {
                    TextBox txtStlWeight = (TextBox)row.FindControl("txtStlWeight");

                    if (txtStlWeight != null && !string.IsNullOrEmpty(txtStlWeight.Text))
                    {
                        decimal stlweight;
                        if (decimal.TryParse(txtStlWeight.Text, out stlweight))
                        {
                            int sl = row.RowIndex + 1;  // Serial number
                            weightDataList.Add(new WeightData
                            {
                                sl = sl,
                                weight = stlweight
                            });
                            totalWeight += stlweight;
                            weightCount++;
                        }
                    }
                }
                decimal averageWeight = (weightCount > 0) ? (totalWeight / weightCount) : 0;
                string jsonData = JsonConvert.SerializeObject(weightDataList);

                UpdatelinewtInDatabase(jsonData, averageWeight);

                btnSubmit.Enabled = true;


                ScriptManager.RegisterStartupScript(this, this.GetType(), "SwitchTab", "document.getElementById('ovenReport-tab').click();", true);
                //LineWeights_Grid.Visible = false;
            }
            else
            {

            }
        }


        private void UpdatelinewtInDatabase(string jsonData, decimal averageWeight)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE TRN_RotaryLine_OvenEnd SET linewt = @linewt,avglinewt = @avglinewt, Panel2_Status=@Panel2_Status, Panel2_Timestamp=@Panel2_Timestamp WHERE RLWt = @RLWt";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@linewt", jsonData);
                        cmd.Parameters.AddWithValue("@avglinewt", averageWeight);
                        cmd.Parameters.AddWithValue("@RLWt", RLWt);
                        cmd.Parameters.AddWithValue("@Panel2_Status", 1);
                        cmd.Parameters.AddWithValue("@Panel2_Timestamp", DateTime.Now);
                        cmd.ExecuteNonQuery();

                        btn_rawSubmit.Enabled = false;
                        btn_rawSubmit.Text = "SAVED";


                        string UpdatelinewtInDatabaseSuccessScript = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Data Success',
                                text: 'Recorded Successfully!!',
                                type: 'success',
                                styling: 'bootstrap3'
                            });
                        </script>";

                        // RegisterStartupScript adds the JavaScript code to the page
                        ClientScript.RegisterStartupScript(this.GetType(), "UpdatelinewtInDatabaseNotification2", UpdatelinewtInDatabaseSuccessScript, false);

                    }
                }
            }
            catch (Exception ex)
            {
                string UpdatelinewtInDatabaseerrorMessage = ex.Message.Replace("'", "\\'"); // Escape single quotes in the error message
                string UpdatelinewtInDatabaseerrorScript = "<script type='text/javascript'>\n" +
                                     $"new PNotify({{\n" +
                                     "    title: 'Error',\n" +
                                     $"    text: '{UpdatelinewtInDatabaseerrorMessage}',\n" +
                                     "    type: 'error',\n" +
                                     "    styling: 'bootstrap3'\n" +
                                     "});\n" +
                                     "</script>";
                ClientScript.RegisterStartupScript(this.GetType(), "UpdatelinewtInDatabaseErrorNotification", UpdatelinewtInDatabaseerrorScript, false);
            }
        }

        protected void btn_rawrest_Click(object sender, EventArgs e)
        {
            foreach (GridViewRow row in LineWeights_Grid.Rows)
            {
                TextBox txtStlWeight = (TextBox)row.FindControl("txtStlWeight");
                TextBox txtEdlWeight = (TextBox)row.FindControl("txtEdlWeight");

                if (txtStlWeight != null)
                {
                    txtStlWeight.Text = string.Empty; // Clear STL weight text
                }

                if (txtEdlWeight != null)
                {
                    txtEdlWeight.Text = string.Empty; // Clear EDL weight text
                }
            }
        }


        private void GridBinder(int rowCount)
        {
            DataTable dt = new DataTable();

            // Create columns: Sl, GaugeLengthValue, WeightValue
            dt.Columns.Add("Sl", typeof(int));
            dt.Columns.Add("GaugeLengthValue", typeof(decimal));
            dt.Columns.Add("WeightValue", typeof(decimal));

            for (int i = 1; i <= rowCount; i++)
            {
                DataRow dr = dt.NewRow();
                dr["Sl"] = i; // Serial number
                dt.Rows.Add(dr);
            }

            OvenEnd_GridView.DataSource = dt;
            OvenEnd_GridView.DataBind();
        }

        public class OWeightData
        {
            public int sl { get; set; }
            public decimal wt { get; set; }
        }

        public class GaugeData
        {
            public int sl { get; set; }
            public decimal ge { get; set; }
        }

        public class OvenEndData
        {
            public int sl { get; set; }
            public decimal ge { get; set; }
            public decimal wt { get; set; }
        }

        protected void btnOvenSubmit_Click(object sender, EventArgs e)
        {
            if (Btn_Save.Text == "SAVED" && RLWt != string.Empty && btn_rawSubmit.Text == "SAVED")
            {
                List<GaugeData> gaugeDataList = new List<GaugeData>();
                List<OWeightData> weightDataList = new List<OWeightData>();
                List<OvenEndData> OvenEndDataList = new List<OvenEndData>();

                decimal TTL_GaugeValues = 0;
                int TTL_GaugeValuesRows = 0;
                decimal TTL_OEWeightValues = 0;
                int TTL_OEWeightValuesRows = 0;

                foreach (GridViewRow row in OvenEnd_GridView.Rows)
                {
                    TextBox txtGaugeLength = (TextBox)row.FindControl("txtGaugeLength");
                    TextBox txtWeight = (TextBox)row.FindControl("txtWeight");

                    if (txtGaugeLength != null && !string.IsNullOrEmpty(txtGaugeLength.Text) && txtWeight != null && !string.IsNullOrEmpty(txtWeight.Text))
                    {
                        decimal wts;
                        decimal ggv;
                        if (decimal.TryParse(txtGaugeLength.Text, out wts))
                        {
                            if (decimal.TryParse(txtWeight.Text, out ggv))
                            {
                                int sl = row.RowIndex + 1; // Serial number
                                OvenEndDataList.Add(new OvenEndData
                                {
                                    sl = sl,
                                    ge = ggv,
                                    wt = wts
                                });

                                weightDataList.Add(new OWeightData
                                {
                                    sl = sl,
                                    wt = wts
                                });

                                gaugeDataList.Add(new GaugeData
                                {
                                    sl = sl,
                                    ge = wts
                                });

                                TTL_GaugeValues += ggv;
                                TTL_GaugeValuesRows++;

                                TTL_OEWeightValues += wts;
                                TTL_OEWeightValuesRows++;
                            }
                        }
                    }
                }
                string CombinedJSON = JsonConvert.SerializeObject(OvenEndDataList);
                string WeightsJSON = JsonConvert.SerializeObject(weightDataList);
                string GaugeValueJSON = JsonConvert.SerializeObject(gaugeDataList);

                decimal averageGaugeWeights = (TTL_GaugeValuesRows > 0) ? (TTL_GaugeValues / TTL_GaugeValuesRows) : 0;
                decimal averageOEWeights = (TTL_OEWeightValuesRows > 0) ? (TTL_OEWeightValues / TTL_OEWeightValuesRows) : 0;


                // Update existing records in the database
                UpdateOvenEND_InDatabase(GaugeValueJSON, averageGaugeWeights, WeightsJSON, averageOEWeights, CombinedJSON);

                //ScriptManager.RegisterStartupScript(this, this.GetType(), "SwitchTab", "document.getElementById('GrossWeightData-tab').click();", true);
                ScriptManager.RegisterStartupScript(this, this.GetType(), "SwitchTab", "document.getElementById('basicData-tab').click();", true);
                //OvenEnd_GridView.Visible=false;
            }
        }

        private void UpdateOvenEND_InDatabase(string GaugeValueJSON, decimal averageGaugeWeights,  string WeightsJSON, decimal averageOEWeights, string CombinedJSON)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = "UPDATE TRN_RotaryLine_OvenEnd SET gaugevalues = @gaugevalues, avggaugevalue = @avggaugevalue, weightvalue = @weightvalue, avgweightvalue = @avgweightvalue, gaugeandweight = @gaugeandweight, Panel3_Status=@Panel3_Status, Panel3_Timestamp=@Panel3_Timestamp WHERE RLWt = @RLWt";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@gaugevalues", GaugeValueJSON);
                        cmd.Parameters.AddWithValue("@avggaugevalue", averageGaugeWeights);
                        cmd.Parameters.AddWithValue("@weightvalue", WeightsJSON);
                        cmd.Parameters.AddWithValue("@avgweightvalue", averageOEWeights);
                        cmd.Parameters.AddWithValue("@gaugeandweight", CombinedJSON);
                        cmd.Parameters.AddWithValue("@RLWt", RLWt);
                        cmd.Parameters.AddWithValue("@Panel3_Status", 1);
                        cmd.Parameters.AddWithValue("@Panel3_Timestamp", DateTime.Now);
                        cmd.ExecuteNonQuery();

                        btnSubmit.Enabled = false;
                        btnSubmit.Text = "SAVED";

                        string UpdateOvenEND_SuccessScript = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Data Success',
                                text: 'Recorded Successfully!!',
                                type: 'success',
                                styling: 'bootstrap3'
                            });
                        </script>";

                        // RegisterStartupScript adds the JavaScript code to the page
                        ClientScript.RegisterStartupScript(this.GetType(), "UpdateOvenENDSuccessNotification", UpdateOvenEND_SuccessScript, false);
                    }
                }
            }
            catch (Exception ex)
            {
                string UpdateOvenEND_InDatabase_errorMessage = ex.Message.Replace("'", "\\'"); // Escape single quotes in the error message
                string UpdateOvenEND_InDatabase_errorScript = "<script type='text/javascript'>\n" +
                                     $"new PNotify({{\n" +
                                     "    title: 'Error',\n" +
                                     $"    text: '{UpdateOvenEND_InDatabase_errorMessage}',\n" +
                                     "    type: 'error',\n" +
                                     "    styling: 'bootstrap3'\n" +
                                     "});\n" +
                                     "</script>";
                ClientScript.RegisterStartupScript(this.GetType(), "UpdateOvenEND_InDatabaseErrorNotification", UpdateOvenEND_InDatabase_errorScript, false);
            }
        }

        protected void btn_ovenrest_Click(object sender, EventArgs e)
        {
            // Iterate through each row of the GridView
            foreach (GridViewRow row in OvenEnd_GridView.Rows)
            {
                // Find the TextBox controls in the current row
                TextBox txtGaugeLength = (TextBox)row.FindControl("txtGaugeLength");
                TextBox txtWeight = (TextBox)row.FindControl("txtWeight");

                // Check if the TextBox for Gauge Length exists and clear its content
                if (txtGaugeLength != null)
                {
                    txtGaugeLength.Text = string.Empty; // Clear Gauge Length text
                }

                // Check if the TextBox for Weight exists and clear its content
                if (txtWeight != null)
                {
                    txtWeight.Text = string.Empty; // Clear Weight text
                }
            }
        }

    }
}


