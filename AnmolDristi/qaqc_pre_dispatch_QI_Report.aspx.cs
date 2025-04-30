using AnmolDristi.DAL;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Configuration;

namespace AnmolDristi
{
    public partial class qaqc_pre_dispatch_QI_Report : System.Web.UI.Page
    {
        public static string PDC_key = String.Empty;
        DB_Utility_OH4Y dbcl = new DB_Utility_OH4Y();
        public static string ImgLink1 = string.Empty;
        public static string ImgLink2 = string.Empty;
        string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
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
                    DDL_BrandSKU.Enabled = false;
                    btn_AddList.Enabled = false;
                    btn_resetgrid.Enabled = false;

                    lbl_docname.Text = "QA - PRE DISPATCH CLEARANCE REPORT";
                    lbl_docnumber.Text = "ANMOL/DOC/CORP/QA/06";

                    PlantBinder();
                    DisplayCurrentShift();

                    GetGridViewData(); // Initialize DataTable on first load
                    BindGridView();
                    // Set the default state for the hidden field if needed
                    hfLockState.Value = "false"; // Default to unlocked
                }

            }
            else
            {
                // Read the lock state from the hidden field
                bool isLocked = hfLockState.Value == "true";

                // Lock or unlock fields based on the stored state
                LockUnlockFields(isLocked);
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

        private string Find_DBCode()
        {
            string newLspId = null;
            dbcl.Sqlconnection();
            dbcl.ConnectDb();

            try
            {
                string query = "SELECT PDCR_PK FROM TRN_Pre_Dispatch_Clearance_Report WHERE Id = (SELECT MAX(Id) FROM TRN_Pre_Dispatch_Clearance_Report)";
                SqlCommand command = new SqlCommand(query, dbcl.Conn);
                SqlDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    string lastLspId = reader["PDCR_PK"].ToString();
                    // Extract the numeric part from the LSP_Id (Assumes LSP is always 3 characters long)
                    string numericPart = lastLspId.Substring(3);
                    int numericValue = Convert.ToInt32(numericPart);

                    // Increment the numeric part
                    numericValue++;

                    // Generate the new LSP_Id, preserving the "LSP" prefix and ensuring proper zero-padding
                    newLspId = "PDC" + numericValue.ToString("D2"); // D2 ensures 2 digits (e.g., 09 -> 10)
                }
                else
                {
                    // If there are no records, start with the initial LSP001
                    newLspId = "PDC01"; // Adjusted to start from LSP01 to match the two-digit pattern
                }

                reader.Close();
            }
            catch (Exception ex)
            {
                // Handle exception (log it, rethrow it, etc.)
                throw ex;
            }
            finally
            {
                dbcl.DisconnectDb();
            }

            PDC_key = newLspId;
            return newLspId;
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
                    cmd.Parameters.AddWithValue("@FormID", 8);
                    cmd.Parameters.AddWithValue("@FormName", "qaqc_pre_dispatch_QI_Report");

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        hdn_formid.Value = "8";
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
                            dbcl.InsertDefaultApprovers(selectedPlantValue, selectedPlantLineValue, 8);

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
                    cmd.Parameters.AddWithValue("@FormID", 8);
                    cmd.Parameters.AddWithValue("@FormName", "qaqc_pre_dispatch_QI_Report");

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        hdn_formid.Value = "8";
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
                            bool isInserted = dbcl.InsertDefaultApprovers(selectedPlantValue, selectedPlantLineValue, 8);

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



        protected void btnSubmit_Click(object sender, EventArgs e)

        {
            //    string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            //    // Get form values
            //    string plantName = DDL_Plant.SelectedValue;
            //    string plantLine = DDL_PlantLine.SelectedValue;
            //    string productCategory = DDL_ProductCategory.SelectedValue;
            //    string productBrand = DDL_ProductBrand.SelectedValue;
            //    string brandSKU = DDL_BrandSKU.SelectedValue;

            //    //string shift = TXT_Shift.Text;
            //    string shift = hdn_shiftvalue.Value.ToString();

            //    // string productBrand = TXT_ProductBrand.Text;
            //    string inspectionLot = TXT_InspectionLot.Text;
            //    string materialCode = TXT_MaterialCode.Text;
            //    decimal? cbbProduced = string.IsNullOrEmpty(TXT_CBB_Produced.Text) ? (decimal?)null : Convert.ToDecimal(TXT_CBB_Produced.Text);
            //    decimal? cbbChecked = string.IsNullOrEmpty(TXT_CBB_Checked.Text) ? (decimal?)null : Convert.ToDecimal(TXT_CBB_Checked.Text);
            //    int cbbBoxCondition = Convert.ToInt32(RBL_BoxCondition.SelectedValue);
            //    string cbbBoxConditionRemarks = TXB_BoxCondition_Remarks.Text;
            //    decimal? packetsCBB = string.IsNullOrEmpty(TXT_Packets_CBB.Text) ? (decimal?)null : Convert.ToDecimal(TXT_Packets_CBB.Text);
            //    int cbbTapping = Convert.ToInt32(RBL_Tapping.SelectedValue);
            //    string cbbTappingRemarks = TXB_Tapping_Remarks.Text;
            //    decimal? packetsCheckedCBB = string.IsNullOrEmpty(TXT_Pkts_Checked_Per_CBB.Text) ? (decimal?)null : Convert.ToDecimal(TXT_Pkts_Checked_Per_CBB.Text);
            //    decimal? weightOfPackets = string.IsNullOrEmpty(TXT_Wt_of_Pkts.Text) ? (decimal?)null : Convert.ToDecimal(TXT_Wt_of_Pkts.Text);
            //    DateTime? packageDate = string.IsNullOrEmpty(TXT_PackageDate.Text) ? (DateTime?)null : Convert.ToDateTime(TXT_PackageDate.Text);
            //    string batchNo = TXT_BatchNo.Text;
            //    decimal? packetsMRP = string.IsNullOrEmpty(TXT_PacketsMRP.Text) ? (decimal?)null : Convert.ToDecimal(TXT_PacketsMRP.Text);
            //    int longSeal = Convert.ToInt32(RBL_LongSeal.SelectedValue);
            //    string longSealRemarks = TXB_LongSeal_Remarks.Text;
            //    int endSeal = Convert.ToInt32(RBL_EndSeal.SelectedValue);
            //    string endSealRemarks = TXB_EndSeal_Remarks.Text;
            //    int mainPanel = Convert.ToInt32(RBL_MainPanel.SelectedValue);
            //    string mainPanelRemarks = TXB_MainPanel_Remarks.Text;
            //    int cutsPackets = Convert.ToInt32(RBL_CutsPackets.SelectedValue);
            //    string cutsPacketsRemarks = TXB_CutsPackets_Remarks.Text;
            //    int backingStatus = Convert.ToInt32(RBL_BackingStatus.SelectedValue);
            //    string backingStatusRemarks = TXB_BackingStatus_Remarks.Text;
            //    int elongOval = Convert.ToInt32(RBL_ElongOval.SelectedValue);
            //    string elongOvalRemarks = TXB_ElongOval_Remarks.Text;
            //    int cupping = Convert.ToInt32(RBL_Cupping.SelectedValue);
            //    string cuppingRemarks = TXB_Cupping_Remarks.Text;
            //    int impression = Convert.ToInt32(RBL_Impression.SelectedValue);
            //    string impressionRemarks = TXB_Impression_Remarks.Text;
            //    int soggyStatus = Convert.ToInt32(RBL_SoggyStatus.SelectedValue);
            //    string soggyStatusRemarks = TXB_SoggyStatus_Remarks.Text;
            //    int foreignBody = Convert.ToInt32(RBL_ForeignBody.SelectedValue);
            //    string foreignBodyRemarks = TXB_ForeignBody_Remarks.Text;
            //    int offOdour = Convert.ToInt32(RBL_OffOdour.SelectedValue);
            //    string offOdourRemarks = TXB_OffOdour_Remarks.Text;
            //    string remarks = TXB_Remarks.Text;

            //    try
            //    {
            //        using (SqlConnection conn = new SqlConnection(connectionString))
            //        {
            //            conn.Open();

            //            // SQL Insert Query
            //            string query = @"
            //        INSERT INTO TRN_Pre_Dispatch_Clearance_Report
            //        (FormID, PDCR_PK, PlantName, Line, ProductCategory, SubmittedById, SubmittedByEmployeeCode, Approver1EmployeeCode, 
            //        Approver1_Status, Approver1_TimeStamp, SubmittedDate, SubmittedTime, Shift, ProductBrand, 
            //        InspectionLot, MaterialCode, CBB_Produced, CBB_Checked, CBB_Box_Condition, CBB_Box_ConditionRemarks, Packets_CBB, 
            //        CBB_tapping, CBB_tappingRemarks, PacketsChecked_CBB, WeightofPackets, PackageDate, BatchNo, PacketsMRP, 
            //        LongSeal, LongSealRemarks, EndSeal, EndSealRemarks, MainPanel, MainPanelRemarks, Cuts_Packets, Cuts_PacketsRemarks, 
            //        BackingStatus, BackingStatusRemarks, ElongOval, ElongOvalRemarks, Cupping, CuppingRemarks, 
            //        Impression, ImpressionRemarks, SoggyStatus, SoggyStatusRemarks, ForeignBody, ForeignBodyRemarks, OffOdour, OffOdourRemarks, 
            //        Remarks, SKUId, ViewMode, DeleteMode, Approver2EmployeeCode, Approver2_Status, Approver2_TimeStamp, 
            //        DottedLineApproverEmployeeCode, DottedApprover_Status, DottedApprover_TimeStamp)
            //        VALUES 
            //        (@FormID, @PDCR_PK, @PlantName, @Line, @ProductCategory, @SubmittedById, @SubmittedByEmployeeCode, 
            //        @Approver1EmployeeCode, @Approver1_Status, @Approver1_TimeStamp, @SubmittedDate, @SubmittedTime, @Shift, 
            //        @ProductBrand, @InspectionLot, @MaterialCode, @CBB_Produced, @CBB_Checked, @CBB_Box_Condition, 
            //        @CBB_Box_ConditionRemarks, @Packets_CBB, @CBB_tapping, @CBB_tappingRemarks, @PacketsChecked_CBB, 
            //        @WeightofPackets, @PackageDate, @BatchNo, @PacketsMRP, @LongSeal, @LongSealRemarks, @EndSeal, 
            //        @EndSealRemarks, @MainPanel, @MainPanelRemarks, @Cuts_Packets, @Cuts_PacketsRemarks, @BackingStatus, 
            //        @BackingStatusRemarks, @ElongOval, @ElongOvalRemarks, @Cupping, @CuppingRemarks, @Impression, 
            //        @ImpressionRemarks, @SoggyStatus, @SoggyStatusRemarks, @ForeignBody, @ForeignBodyRemarks, 
            //        @OffOdour, @OffOdourRemarks, @Remarks, @SKUId, @ViewMode, @DeleteMode, @Approver2EmployeeCode, 
            //        @Approver2_Status, @Approver2_TimeStamp, @DottedLineApproverEmployeeCode, 
            //        @DottedApprover_Status, @DottedApprover_TimeStamp)";

            //            using (SqlCommand cmd = new SqlCommand(query, conn))
            //            {
            //                // Add parameters
            //                cmd.Parameters.AddWithValue("@FormID", Convert.ToInt32(hdn_formid.Value.ToString()));
            //                cmd.Parameters.AddWithValue("@PDCR_PK", Find_DBCode());
            //                cmd.Parameters.AddWithValue("@PlantName", plantName);
            //                cmd.Parameters.AddWithValue("@Line", plantLine);
            //                cmd.Parameters.AddWithValue("@ProductCategory", productCategory);
            //                cmd.Parameters.AddWithValue("@ProductBrand", productBrand);
            //                cmd.Parameters.AddWithValue("@SKUId", brandSKU);
            //                cmd.Parameters.AddWithValue("@SubmittedById", Convert.ToInt32(Convert.ToInt32(Session["USERID"].ToString())));
            //                cmd.Parameters.AddWithValue("@SubmittedByEmployeeCode", Session["WORKMAN"].ToString());
            //                cmd.Parameters.AddWithValue("@Approver1EmployeeCode", Approver1CodeLabel.Text.ToString());
            //                cmd.Parameters.AddWithValue("@Approver1_Status", 0);
            //                cmd.Parameters.AddWithValue("@Approver1_TimeStamp", DBNull.Value);
            //                cmd.Parameters.AddWithValue("@Approver2EmployeeCode", Approver2CodeLabel.Text.ToString());
            //                cmd.Parameters.AddWithValue("@Approver2_Status", 0);
            //                cmd.Parameters.AddWithValue("@Approver2_TimeStamp", DBNull.Value);
            //                cmd.Parameters.AddWithValue("@DottedLineApproverEmployeeCode", DottedLineApproverCodeLabel.Text.ToString());
            //                cmd.Parameters.AddWithValue("@DottedApprover_Status", 0);
            //                cmd.Parameters.AddWithValue("@DottedApprover_TimeStamp", DBNull.Value);
            //                cmd.Parameters.AddWithValue("@ViewMode", 1);
            //                cmd.Parameters.AddWithValue("@DeleteMode", 0);
            //                cmd.Parameters.AddWithValue("@SubmittedDate", DateTime.Now.Date);
            //                cmd.Parameters.AddWithValue("@SubmittedTime", DateTime.Now.TimeOfDay);
            //                cmd.Parameters.AddWithValue("@Shift", shift);
            //                cmd.Parameters.AddWithValue("@InspectionLot", (object)inspectionLot ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@MaterialCode", (object)materialCode ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@CBB_Produced", (object)cbbProduced ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@CBB_Checked", (object)cbbChecked ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@CBB_Box_Condition", (object)cbbBoxCondition ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@CBB_Box_ConditionRemarks", (object)cbbBoxConditionRemarks ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@Packets_CBB", (object)packetsCBB ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@CBB_tapping", (object)cbbTapping ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@CBB_tappingRemarks", (object)cbbTappingRemarks ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@PacketsChecked_CBB", (object)packetsCheckedCBB ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@WeightofPackets", (object)weightOfPackets ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@PackageDate", (object)packageDate ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@BatchNo", (object)batchNo ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@PacketsMRP", (object)packetsMRP ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@LongSeal", (object)longSeal ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@LongSealRemarks", (object)longSealRemarks ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@EndSeal", (object)endSeal ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@EndSealRemarks", (object)endSealRemarks ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@MainPanel", (object)mainPanel ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@MainPanelRemarks", (object)mainPanelRemarks ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@Cuts_Packets", (object)cutsPackets ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@Cuts_PacketsRemarks", (object)cutsPacketsRemarks ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@BackingStatus", (object)backingStatus ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@BackingStatusRemarks", (object)backingStatusRemarks ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@ElongOval", (object)elongOval ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@ElongOvalRemarks", (object)elongOvalRemarks ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@Cupping", (object)cupping ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@CuppingRemarks", (object)cuppingRemarks ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@Impression", (object)impression ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@ImpressionRemarks", (object)impressionRemarks ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@SoggyStatus", (object)soggyStatus ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@SoggyStatusRemarks", (object)soggyStatusRemarks ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@ForeignBody", (object)foreignBody ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@ForeignBodyRemarks", (object)foreignBodyRemarks ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@OffOdour", (object)offOdour ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@OffOdourRemarks", (object)offOdourRemarks ?? DBNull.Value);
            //                cmd.Parameters.AddWithValue("@Remarks", (object)remarks ?? DBNull.Value);

            //                // Execute the query
            //                cmd.ExecuteNonQuery();
            //            }

            //            conn.Close();
            //        }


            //        btnSubmit.Enabled = false;
            //        btnSubmit.Text = "SAVED";
            //        btnSubmit.CssClass = "btn btn-sm btn-success";


            //        string Data_SuccessScript = @"<script type='text/javascript'>
            //                        new PNotify({
            //                            title: 'Data Success',
            //                            text: 'Recorded Successfully!!',
            //                            type: 'success',
            //                            styling: 'bootstrap3'
            //                        });
            //                    </script>";

            //        //// RegisterStartupScript adds the JavaScript code to the page
            //        ClientScript.RegisterStartupScript(this.GetType(), "ShowDataSuccessNotification", Data_SuccessScript, false);



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
        }



        protected void DDL_ProductBrand_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_ProductBrand.SelectedIndex != 0)
            {
                string selectedProductBrandValue = DDL_ProductBrand.SelectedValue.ToString();
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



        private void BrandSKUBinder(string selectedProductBrandValue)
        {
            string query = "SELECT SKUId, SKU_name FROM MST_Brand_SKU WHERE brand_id = @SelectedPlantValue";
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



        protected void btnReset_Click(object sender, EventArgs e)
        {

            //    // Reset form fields after submission, if needed
            //    DDL_Plant.SelectedIndex = 0;
            //    DDL_PlantLine.SelectedIndex = 0;
            //    DDL_ProductCategory.SelectedIndex = 0;
            //    DDL_ProductBrand.SelectedIndex = 0;
            //    DDL_BrandSKU.SelectedIndex = 0;
            //    hdn_shiftvalue.Value = string.Empty;

            //    TXT_InspectionLot.Text = string.Empty;
            //    TXT_MaterialCode.Text = string.Empty;
            //    TXT_CBB_Produced.Text = string.Empty;
            //    TXT_CBB_Checked.Text = string.Empty;
            //    RBL_BoxCondition.SelectedIndex = -1;
            //    TXB_BoxCondition_Remarks.Text = string.Empty;
            //    TXT_Packets_CBB.Text = string.Empty;
            //    RBL_Tapping.SelectedIndex = -1;
            //    TXB_Tapping_Remarks.Text = string.Empty;
            //    TXT_Pkts_Checked_Per_CBB.Text = string.Empty;
            //    TXT_Wt_of_Pkts.Text = string.Empty;
            //    TXT_PackageDate.Text = string.Empty;
            //    TXT_BatchNo.Text = string.Empty;
            //    TXT_PacketsMRP.Text = string.Empty;
            //    RBL_LongSeal.SelectedIndex = -1;
            //    TXB_LongSeal_Remarks.Text = string.Empty;
            //    RBL_EndSeal.SelectedIndex = -1;
            //    TXB_EndSeal_Remarks.Text = string.Empty;
            //    RBL_MainPanel.SelectedIndex = -1;
            //    TXB_MainPanel_Remarks.Text = string.Empty;
            //    RBL_CutsPackets.SelectedIndex = -1;
            //    TXB_CutsPackets_Remarks.Text = string.Empty;
            //    RBL_BackingStatus.SelectedIndex = -1;
            //    TXB_BackingStatus_Remarks.Text = string.Empty;
            //    RBL_ElongOval.SelectedIndex = -1;
            //    TXB_ElongOval_Remarks.Text = string.Empty;
            //    RBL_Cupping.SelectedIndex = -1;
            //    TXB_Cupping_Remarks.Text = string.Empty;
            //    RBL_Impression.SelectedIndex = -1;
            //    TXB_Impression_Remarks.Text = string.Empty;
            //    RBL_SoggyStatus.SelectedIndex = -1;
            //    TXB_SoggyStatus_Remarks.Text = string.Empty;
            //    RBL_ForeignBody.SelectedIndex = -1;
            //    TXB_ForeignBody_Remarks.Text = string.Empty;
            //    RBL_OffOdour.SelectedIndex = -1;
            //    TXB_OffOdour_Remarks.Text = string.Empty;
            //    TXB_Remarks.Text = string.Empty;

            //    // Reset any labels or messages if needed
            //    //lblMessage.Text = string.Empty;

            //    // Reset the submit button
            //    btnSubmit.Enabled = true;  // Re-enable the submit button if needed
            //    btnSubmit.Text = "Submit";  // Reset the text to "Submit"
            //    btnSubmit.CssClass = "btn btn-sm btn-primary";

        }


        //25 April 2025

        private void LockUnlockFields(bool isLocked)
        {
            // Lock/unlock fields based on the value of isLocked
            DDL_Plant.Enabled = !isLocked;
            DDL_PlantLine.Enabled = !isLocked;
            DDL_ProductCategory.Enabled = !isLocked;
            DDL_ProductBrand.Enabled = !isLocked;
        }

        protected void btn_AddList_Click(object sender, EventArgs e)
        {
            try
            {
                // Retrieve the DataTable from ViewState
                DataTable dtCurrentTable = ViewState["GridData"] as DataTable;

                if (dtCurrentTable != null)
                {
                    // Create a new row
                    DataRow dr = dtCurrentTable.NewRow();
                    dr["Plant"] = DDL_Plant.SelectedItem.Text;
                    dr["PlantId"] = DDL_Plant.SelectedValue;
                    dr["PlantLine"] = DDL_PlantLine.SelectedItem.Text;
                    dr["PlantLineId"] = DDL_PlantLine.SelectedValue;
                    dr["ProductCategory"] = DDL_ProductCategory.SelectedItem.Text;
                    dr["ProductCategoryId"] = DDL_ProductCategory.SelectedValue;
                    dr["ProductBrand"] = DDL_ProductBrand.SelectedItem.Text;
                    dr["ProductBrandId"] = DDL_ProductBrand.SelectedValue;
                    dr["BrandSKU"] = DDL_BrandSKU.SelectedItem.Text;
                    dr["BrandSKUId"] = DDL_BrandSKU.SelectedValue;

                    dr["InspectionLot"] = TXT_InspectionLot.Text;
                    dr["MaterialCode"] = TXT_MaterialCode.Text;
                    dr["NoOfCBBProduced"] = TXT_CBB_Produced.Text;
                    dr["NoOfCBBChecked"] = TXT_CBB_Checked.Text;
                    dr["BoxCondition"] = RBL_BoxCondition.SelectedItem.Text;
                    dr["BoxConditionNotOkRemarks"] = TXB_BoxCondition_Remarks.Text;
                    dr["NoOfPacketsInCBB"] = TXT_Packets_CBB.Text;
                    dr["TappingOfCBB"] = RBL_Tapping.SelectedItem.Text;
                    dr["TappingOfCBBNotOkRemarks"] = TXB_Tapping_Remarks.Text;
                    dr["PktsCheckedCBBBox"] = TXT_Pkts_Checked_Per_CBB.Text;
                    dr["WtOfPkts"] = TXT_Wt_of_Pkts.Text;
                    dr["PkgDate"] = TXT_PackageDate.Text;
                    dr["BatchLotNo"] = TXT_BatchNo.Text;
                    dr["MRPOfPkts"] = TXT_PacketsMRP.Text;
                    dr["LongSealCondition"] = RBL_LongSeal.SelectedItem.Text;
                    dr["LongSealConditionNotOkRemarks"] = TXB_LongSeal_Remarks.Text;
                    dr["EndSealCondition"] = RBL_EndSeal.SelectedItem.Text;
                    dr["EndSealConditionNotOkRemarks"] = TXB_EndSeal_Remarks.Text;
                    dr["MainPanelCentralNot"] = RBL_MainPanel.SelectedItem.Text;
                    dr["MainPanelCentralNotOkRemarks"] = TXB_MainPanel_Remarks.Text;
                    dr["CutsInPackets"] = RBL_CutsPackets.SelectedItem.Text;
                    dr["CutsInPacketsNotOkRemarks"] = TXB_CutsPackets_Remarks.Text;
                    dr["BakingUnderOverBaked"] = RBL_BackingStatus.SelectedItem.Text;
                    dr["BakingUnderOverBakedNotOkRemarks"] = TXB_BackingStatus_Remarks.Text;
                    dr["ShapeConditionElongatedOrOval"] = RBL_ElongOval.SelectedItem.Text;
                    dr["ShapeConditionElongatedOrOvalNotOkRemarks"] = TXB_ElongOval_Remarks.Text;
                    dr["CuppingCondition"] = RBL_Cupping.SelectedItem.Text;
                    dr["CuppingConditionNotOkRemarks"] = TXB_Cupping_Remarks.Text;
                    dr["ImpressionOnBiscuits"] = RBL_Impression.SelectedItem.Text;
                    dr["ImpressionOnBiscuitsNotOkRemarks"] = TXB_Impression_Remarks.Text;
                    dr["BiscuitSoggy"] = RBL_SoggyStatus.SelectedItem.Text;
                    dr["BiscuitSoggyNotOkRemarks"] = TXB_SoggyStatus_Remarks.Text;
                    dr["ForeignBodyInBiscuit"] = RBL_ForeignBody.SelectedItem.Text;
                    dr["ForeignBodyInBiscuitNotOkRemarks"] = TXB_ForeignBody_Remarks.Text;
                    dr["OffOdour"] = RBL_OffOdour.SelectedItem.Text;
                    dr["OffOdourNotOkRemarks"] = TXB_OffOdour_Remarks.Text;
                    dr["Remarks"] = TXB_Remarks.Text;


                    // Add the row to the DataTable
                    dtCurrentTable.Rows.Add(dr);

                    // Save the updated DataTable back to ViewState
                    ViewState["GridData"] = dtCurrentTable;

                    DDL_BrandSKU.Enabled = true;
                    btn_AddList.Enabled = true;
                    btn_resetgrid.Enabled = true;

                    final_save_btns.Visible = true;
                    // Rebind the GridView
                    BindGridView();

                    // Clear the form inputs
                    ClearFormInputs();

                    // Show success message
                    lbl_message.Text = "Row added successfully.";
                    lbl_message.ForeColor = System.Drawing.Color.Green;
                }
            }
            catch (Exception ex)
            {
                lbl_message.Text = "Error: " + ex.Message;
                lbl_message.ForeColor = System.Drawing.Color.Red;
            }
        }

        private DataTable GetGridViewData()
        {
            if (ViewState["GridData"] == null)
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("Plant");
                dt.Columns.Add("PlantId");
                dt.Columns.Add("PlantLine");
                dt.Columns.Add("PlantLineId");
                dt.Columns.Add("ProductCategory");
                dt.Columns.Add("ProductCategoryId");
                dt.Columns.Add("ProductBrand");
                dt.Columns.Add("ProductBrandId");
                dt.Columns.Add("BrandSKU");
                dt.Columns.Add("BrandSKUId");

                dt.Columns.Add("InspectionLot");
                dt.Columns.Add("MaterialCode");
                dt.Columns.Add("NoOfCBBProduced");
                dt.Columns.Add("NoOfCBBChecked");
                dt.Columns.Add("BoxCondition");
                dt.Columns.Add("BoxConditionNotOkRemarks");
                dt.Columns.Add("NoOfPacketsInCBB");
                dt.Columns.Add("TappingOfCBB");
                dt.Columns.Add("TappingOfCBBNotOkRemarks");
                dt.Columns.Add("PktsCheckedCBBBox");
                dt.Columns.Add("WtOfPkts");
                dt.Columns.Add("PkgDate");
                dt.Columns.Add("BatchLotNo");
                dt.Columns.Add("MRPOfPkts");
                dt.Columns.Add("LongSealCondition");
                dt.Columns.Add("LongSealConditionNotOkRemarks");
                dt.Columns.Add("EndSealCondition");
                dt.Columns.Add("EndSealConditionNotOkRemarks");
                dt.Columns.Add("MainPanelCentralNot");
                dt.Columns.Add("MainPanelCentralNotOkRemarks");
                dt.Columns.Add("CutsInPackets");
                dt.Columns.Add("CutsInPacketsNotOkRemarks");
                dt.Columns.Add("BakingUnderOverBaked");
                dt.Columns.Add("BakingUnderOverBakedNotOkRemarks");
                dt.Columns.Add("ShapeConditionElongatedOrOval");
                dt.Columns.Add("ShapeConditionElongatedOrOvalNotOkRemarks");
                dt.Columns.Add("CuppingCondition");
                dt.Columns.Add("CuppingConditionNotOkRemarks");
                dt.Columns.Add("ImpressionOnBiscuits");
                dt.Columns.Add("ImpressionOnBiscuitsNotOkRemarks");
                dt.Columns.Add("BiscuitSoggy");
                dt.Columns.Add("BiscuitSoggyNotOkRemarks");
                dt.Columns.Add("ForeignBodyInBiscuit");
                dt.Columns.Add("ForeignBodyInBiscuitNotOkRemarks");
                dt.Columns.Add("OffOdour");
                dt.Columns.Add("OffOdourNotOkRemarks");
                dt.Columns.Add("Remarks");


                ViewState["GridData"] = dt;
            }

            return ViewState["GridData"] as DataTable;
        }

        private void DisableFormInputs()
        {
            // Disable all the form controls
            DDL_BrandSKU.Enabled = false;  // Disable dropdown

            TXT_InspectionLot.Enabled = false;
            TXT_MaterialCode.Enabled = false;
            TXT_CBB_Produced.Enabled = false;
            TXT_CBB_Checked.Enabled = false;
            RBL_BoxCondition.Enabled = false;
            TXB_BoxCondition_Remarks.Enabled = false;
            TXT_Packets_CBB.Enabled = false;
            RBL_Tapping.Enabled = false;
            TXB_Tapping_Remarks.Enabled = false;
            TXT_Pkts_Checked_Per_CBB.Enabled = false;
            TXT_Wt_of_Pkts.Enabled = false;
            TXT_PackageDate.Enabled = false;
            TXT_BatchNo.Enabled = false;
            TXT_PacketsMRP.Enabled = false;
            RBL_LongSeal.Enabled = false;
            TXB_LongSeal_Remarks.Enabled = false;
            RBL_EndSeal.Enabled = false;
            TXB_EndSeal_Remarks.Enabled = false;
            RBL_MainPanel.Enabled = false;
            TXB_MainPanel_Remarks.Enabled = false;
            RBL_CutsPackets.Enabled = false;
            TXB_CutsPackets_Remarks.Enabled = false;
            RBL_BackingStatus.Enabled = false;
            TXB_BackingStatus_Remarks.Enabled = false;
            RBL_ElongOval.Enabled = false;
            TXB_ElongOval_Remarks.Enabled = false;
            RBL_Cupping.Enabled = false;
            TXB_Cupping_Remarks.Enabled = false;
            RBL_Impression.Enabled = false;
            TXB_Impression_Remarks.Enabled = false;
            RBL_SoggyStatus.Enabled = false;
            TXB_SoggyStatus_Remarks.Enabled = false;
            RBL_ForeignBody.Enabled = false;
            TXB_ForeignBody_Remarks.Enabled = false;
            RBL_OffOdour.Enabled = false;
            TXB_OffOdour_Remarks.Enabled = false;
            TXB_Remarks.Enabled = false;

            AddMoreDiv.Visible = false;
        }

        private void ClearFormInputs()
        {
            DDL_BrandSKU.SelectedIndex = 0;

            TXT_InspectionLot.Text = "";
            TXT_MaterialCode.Text = "";
            TXT_CBB_Produced.Text = "";
            TXT_CBB_Checked.Text = "";
            RBL_BoxCondition.SelectedIndex = -1;
            TXB_BoxCondition_Remarks.Text = "";
            TXT_Packets_CBB.Text = "";
            RBL_Tapping.SelectedIndex = -1;
            TXB_Tapping_Remarks.Text = "";
            TXT_Pkts_Checked_Per_CBB.Text = "";
            TXT_Wt_of_Pkts.Text = "";
            TXT_PackageDate.Text = "";
            TXT_BatchNo.Text = "";
            TXT_PacketsMRP.Text = "";
            RBL_LongSeal.SelectedIndex = -1;
            TXB_LongSeal_Remarks.Text = "";
            RBL_EndSeal.SelectedIndex = -1;
            TXB_EndSeal_Remarks.Text = "";
            RBL_MainPanel.SelectedIndex = -1;
            TXB_MainPanel_Remarks.Text = "";
            RBL_CutsPackets.SelectedIndex = -1;
            TXB_CutsPackets_Remarks.Text = "";
            RBL_BackingStatus.SelectedIndex = -1;
            TXB_BackingStatus_Remarks.Text = "";
            RBL_ElongOval.SelectedIndex = -1;
            TXB_ElongOval_Remarks.Text = "";
            RBL_Cupping.SelectedIndex = -1;
            TXB_Cupping_Remarks.Text = "";
            RBL_Impression.SelectedIndex = -1;
            TXB_Impression_Remarks.Text = "";
            RBL_SoggyStatus.SelectedIndex = -1;
            TXB_SoggyStatus_Remarks.Text = "";
            RBL_ForeignBody.SelectedIndex = -1;
            TXB_ForeignBody_Remarks.Text = "";
            RBL_OffOdour.SelectedIndex = -1;
            TXB_OffOdour_Remarks.Text = "";
            TXB_Remarks.Text = "";
        }

        protected void btn_resetgrid_Click(object sender, EventArgs e)
        {
            ClearFormInputs();
        }

        private void BindGridView()
        {
            if (ViewState["GridData"] != null)
            {
                DataTable dt = (DataTable)ViewState["GridData"];

                if ((dt != null) && (dt.Rows.Count > 0))
                {
                    gvData.Visible = true;
                    gvData.DataSource = dt;
                    gvData.DataBind();
                }
                else
                {
                    gvData.Visible = false;
                }
            }
        }

        protected void gvData_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            // Ensure DataTable is initialized
            GetGridViewData();

            try
            {
                // Retrieve the DataTable from ViewState
                DataTable dt = ViewState["GridData"] as DataTable;

                if (dt != null)
                {
                    // Remove the row at the specified index
                    dt.Rows[e.RowIndex].Delete();

                    // Save the updated DataTable back to ViewState
                    dt.AcceptChanges();
                    ViewState["GridData"] = dt;

                    // Rebind the GridView
                    BindGridView();

                    // Show success message
                    lbl_message.Text = "Row deleted successfully.";
                    lbl_message.ForeColor = System.Drawing.Color.Green;
                }
            }
            catch (Exception ex)
            {
                lbl_message.Text = "Error: " + ex.Message;
                lbl_message.ForeColor = System.Drawing.Color.Red;
            }
        }

        protected void btn_svall_Click(object sender, EventArgs e)
        {
            InsertAllPreDispatchDataFromGrid();
        }

        public bool IsPDCIdDuplicate(string pdcId)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            string query = @"SELECT COUNT(1) FROM [dbo].[TRN_Pre_Dispatch_Clearance_Report] WHERE PDCR_PK = @PDCR_PK";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@PDCR_PK", pdcId);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        public void InsertAllPreDispatchDataFromGrid()
        {
            try
            {
                // Ensure there is data in ViewState for gvData
                if (ViewState["GridData"] != null)
                {
                    DataTable dtGridData = ViewState["GridData"] as DataTable;
                    if (dtGridData.Rows.Count > 0)
                    {
                        string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                        string shift = hdn_shiftvalue.Value.ToString();
                        int formID = Convert.ToInt32(hdn_formid.Value.ToString());

                        int submittedById = Convert.ToInt32(Session["USERID"].ToString());
                        string submittedByEmployeeCode = Session["WORKMAN"].ToString();

                        string approver1EmployeeCode = Approver1CodeLabel.Text.ToString();
                        string approver2EmployeeCode = Approver2CodeLabel.Text.ToString();
                        string dottedLineApproverEmployeeCode = DottedLineApproverCodeLabel.Text.ToString();


                        string batchId = string.Format("{0}{1}{2}{3}", DateTime.Now.ToString("yyMMddHHmmss"), shift, formID, submittedByEmployeeCode);

                        string query = @"INSERT INTO TRN_Pre_Dispatch_Clearance_Report
                                        (FormID, PDCR_PK, PlantName, Line, ProductCategory, ProductBrand, SKUId, 
                                        SubmittedById, SubmittedByEmployeeCode, SubmittedDate, SubmittedTime, Shift,
                                        InspectionLot, MaterialCode, CBB_Produced, CBB_Checked, CBB_Box_Condition, CBB_Box_ConditionRemarks, Packets_CBB, 
                                        CBB_tapping, CBB_tappingRemarks, PacketsChecked_CBB, WeightofPackets, PackageDate, BatchNo, PacketsMRP, 
                                        LongSeal, LongSealRemarks, EndSeal, EndSealRemarks, MainPanel, MainPanelRemarks, Cuts_Packets, Cuts_PacketsRemarks, 
                                        BackingStatus, BackingStatusRemarks, ElongOval, ElongOvalRemarks, Cupping, CuppingRemarks, Impression, ImpressionRemarks, 
                                        SoggyStatus, SoggyStatusRemarks, ForeignBody, ForeignBodyRemarks, OffOdour, OffOdourRemarks, Remarks,
                                        Approver1EmployeeCode, Approver2EmployeeCode, DottedLineApproverEmployeeCode, BatchID)
                                    VALUES 
                                        (@FormID, @PDCR_PK, @PlantName, @Line, @ProductCategory, @ProductBrand,  @SKUId, 
                                        @SubmittedById, @SubmittedByEmployeeCode, @SubmittedDate, @SubmittedTime, @Shift, 
                                        @InspectionLot, @MaterialCode, @CBB_Produced, @CBB_Checked, @CBB_Box_Condition, @CBB_Box_ConditionRemarks, @Packets_CBB, 
                                        @CBB_tapping, @CBB_tappingRemarks, @PacketsChecked_CBB, @WeightofPackets, @PackageDate, @BatchNo, @PacketsMRP, 
                                        @LongSeal, @LongSealRemarks, @EndSeal, @EndSealRemarks, @MainPanel, @MainPanelRemarks, @Cuts_Packets, @Cuts_PacketsRemarks, 
                                        @BackingStatus, @BackingStatusRemarks, @ElongOval, @ElongOvalRemarks, @Cupping, @CuppingRemarks, @Impression, @ImpressionRemarks, 
                                        @SoggyStatus, @SoggyStatusRemarks, @ForeignBody, @ForeignBodyRemarks, @OffOdour, @OffOdourRemarks, @Remarks,
                                        @Approver1EmployeeCode, @Approver2EmployeeCode, @DottedLineApproverEmployeeCode, @BatchID)";

                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            conn.Open();

                            foreach (DataRow row in dtGridData.Rows)
                            {
                                using (SqlCommand cmd = new SqlCommand(query, conn))
                                {
                                    // Extract values from DataTable row
                                    string plantName = row["PlantId"].ToString();
                                    string plantLine = row["PlantLineId"].ToString();
                                    string productCategory = row["ProductCategoryId"].ToString();
                                    string productBrand = row["ProductBrandId"].ToString();
                                    string brandSKU = row["BrandSKUId"].ToString();

                                    string inspectionLot = row["InspectionLot"]?.ToString().Trim().ToUpper();
                                    string materialCode = row["MaterialCode"]?.ToString().Trim().ToUpper();
                                    decimal? cbbProduced = string.IsNullOrEmpty(row["NoOfCBBProduced"]?.ToString()) ? (decimal?)null : Convert.ToDecimal(row["NoOfCBBProduced"]);
                                    decimal? cbbChecked = string.IsNullOrEmpty(row["NoOfCBBChecked"]?.ToString()) ? (decimal?)null : Convert.ToDecimal(row["NoOfCBBChecked"]);
                                    int cbbBoxCondition = row["BoxCondition"].ToString().Trim().ToUpper() == "OK" ? 1 : 0;
                                    string cbbBoxConditionRemarks = row["BoxConditionNotOkRemarks"]?.ToString().Trim();
                                    decimal? packetsCBB = string.IsNullOrEmpty(row["NoOfPacketsInCBB"]?.ToString()) ? (decimal?)null : Convert.ToDecimal(row["NoOfPacketsInCBB"]);
                                    int cbbTapping = row["TappingOfCBB"].ToString().Trim().ToUpper() == "OK" ? 1 : 0;
                                    string cbbTappingRemarks = row["TappingOfCBBNotOkRemarks"]?.ToString().Trim();
                                    decimal? packetsCheckedCBB = string.IsNullOrEmpty(row["PktsCheckedCBBBox"]?.ToString()) ? (decimal?)null : Convert.ToDecimal(row["PktsCheckedCBBBox"]);
                                    decimal? weightOfPackets = string.IsNullOrEmpty(row["WtOfPkts"]?.ToString()) ? (decimal?)null : Convert.ToDecimal(row["WtOfPkts"]);
                                    string packageDate = row["PkgDate"]?.ToString().Trim(); 
                                    string batchNo = row["BatchLotNo"]?.ToString().Trim();
                                    decimal? packetsMRP = string.IsNullOrEmpty(row["MRPOfPkts"]?.ToString()) ? (decimal?)null : Convert.ToDecimal(row["MRPOfPkts"]);
                                    int longSeal = row["LongSealCondition"].ToString().Trim().ToUpper() == "OK" ? 1 : 0;
                                    string longSealRemarks = row["LongSealConditionNotOkRemarks"]?.ToString().Trim();
                                    int endSeal = row["EndSealCondition"].ToString().Trim().ToUpper() == "OK" ? 1 : 0;
                                    string endSealRemarks = row["EndSealConditionNotOkRemarks"]?.ToString().Trim();
                                    int mainPanel = row["MainPanelCentralNot"].ToString().Trim().ToUpper() == "OK" ? 1 : 0;
                                    string mainPanelRemarks = row["MainPanelCentralNotOkRemarks"]?.ToString().Trim();
                                    int cutsPackets = row["CutsInPackets"].ToString().Trim().ToUpper() == "NO" ? 1 : 0;
                                    string cutsPacketsRemarks = row["CutsInPacketsNotOkRemarks"]?.ToString().Trim();
                                    int backingStatus = row["BakingUnderOverBaked"].ToString().Trim().ToUpper() == "OK" ? 1 : 0;
                                    string backingStatusRemarks = row["BakingUnderOverBakedNotOkRemarks"]?.ToString().Trim();
                                    int elongOval = row["ShapeConditionElongatedOrOval"].ToString().Trim().ToUpper() == "OK" ? 1 : 0;
                                    string elongOvalRemarks = row["ShapeConditionElongatedOrOvalNotOkRemarks"]?.ToString().Trim();
                                    int cupping = row["CuppingCondition"].ToString().Trim().ToUpper() == "NO" ? 1 : 0;
                                    string cuppingRemarks = row["CuppingConditionNotOkRemarks"]?.ToString().Trim();
                                    int impression = row["ImpressionOnBiscuits"].ToString().Trim().ToUpper() == "OK" ? 1 : 0;
                                    string impressionRemarks = row["ImpressionOnBiscuitsNotOkRemarks"]?.ToString().Trim();
                                    int soggyStatus = row["BiscuitSoggy"].ToString().Trim().ToUpper() == "OK" ? 1 : 0;
                                    string soggyStatusRemarks = row["BiscuitSoggyNotOkRemarks"]?.ToString().Trim();
                                    int foreignBody = row["ForeignBodyInBiscuit"].ToString().Trim().ToUpper() == "NO" ? 1 : 0;
                                    string foreignBodyRemarks = row["ForeignBodyInBiscuitNotOkRemarks"]?.ToString().Trim();
                                    int offOdour = row["OffOdour"].ToString().Trim().ToUpper() == "NO" ? 1 : 0;
                                    string offOdourRemarks = row["OffOdourNotOkRemarks"]?.ToString().Trim();
                                    string remarks = row["Remarks"]?.ToString().Trim();

                                    string pdcId = Find_DBCode(); // Get LSP_Id value
                                    if (IsPDCIdDuplicate(pdcId)) // Check if the LSP_Id is already in the database
                                    {
                                        // Handle duplicate, you can either skip or update the record
                                        // For example, you could generate a new LSP_Id here if needed.
                                        pdcId = Find_DBCode(); // Implement this logic as required
                                    }

                                    // Add parameters to SQL command
                                    cmd.Parameters.AddWithValue("@FormID", formID);
                                    cmd.Parameters.AddWithValue("@PDCR_PK", pdcId);
                                    cmd.Parameters.AddWithValue("@SubmittedById", submittedById);
                                    cmd.Parameters.AddWithValue("@SubmittedByEmployeeCode", submittedByEmployeeCode);
                                    cmd.Parameters.AddWithValue("@SubmittedDate", DateTime.Now.Date);
                                    cmd.Parameters.AddWithValue("@SubmittedTime", DateTime.Now.TimeOfDay);
                                    cmd.Parameters.AddWithValue("@Shift", shift);
                                    cmd.Parameters.AddWithValue("@PlantName", plantName);
                                    cmd.Parameters.AddWithValue("@Line", plantLine);
                                    cmd.Parameters.AddWithValue("@ProductCategory", productCategory);
                                    cmd.Parameters.AddWithValue("@ProductBrand", productBrand);
                                    cmd.Parameters.AddWithValue("@SKUId", brandSKU);
                                    cmd.Parameters.AddWithValue("@InspectionLot", (object)inspectionLot ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@MaterialCode", (object)materialCode ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@CBB_Produced", (object)cbbProduced ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@CBB_Checked", (object)cbbChecked ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@CBB_Box_Condition", (object)cbbBoxCondition ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@CBB_Box_ConditionRemarks", (object)cbbBoxConditionRemarks ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Packets_CBB", (object)packetsCBB ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@CBB_tapping", (object)cbbTapping ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@CBB_tappingRemarks", (object)cbbTappingRemarks ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@PacketsChecked_CBB", (object)packetsCheckedCBB ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@WeightofPackets", (object)weightOfPackets ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@PackageDate", (object)packageDate ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@BatchNo", (object)batchNo ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@PacketsMRP", (object)packetsMRP ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@LongSeal", (object)longSeal ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@LongSealRemarks", (object)longSealRemarks ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@EndSeal", (object)endSeal ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@EndSealRemarks", (object)endSealRemarks ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@MainPanel", (object)mainPanel ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@MainPanelRemarks", (object)mainPanelRemarks ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Cuts_Packets", (object)cutsPackets ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Cuts_PacketsRemarks", (object)cutsPacketsRemarks ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@BackingStatus", (object)backingStatus ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@BackingStatusRemarks", (object)backingStatusRemarks ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@ElongOval", (object)elongOval ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@ElongOvalRemarks", (object)elongOvalRemarks ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Cupping", (object)cupping ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@CuppingRemarks", (object)cuppingRemarks ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Impression", (object)impression ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@ImpressionRemarks", (object)impressionRemarks ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@SoggyStatus", (object)soggyStatus ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@SoggyStatusRemarks", (object)soggyStatusRemarks ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@ForeignBody", (object)foreignBody ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@ForeignBodyRemarks", (object)foreignBodyRemarks ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@OffOdour", (object)offOdour ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@OffOdourRemarks", (object)offOdourRemarks ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Remarks", (object)remarks ?? DBNull.Value);

                                    cmd.Parameters.AddWithValue("@Approver1EmployeeCode", approver1EmployeeCode ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Approver2EmployeeCode", approver2EmployeeCode ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DottedLineApproverEmployeeCode", dottedLineApproverEmployeeCode ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@BatchID", batchId);
                                    // Execute the query
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        // Show success notification
                        string Data_SuccessScript = @"<script type='text/javascript'>
                                new PNotify({
                                    title: 'Data Success',
                                    text: 'All records have been saved successfully!',
                                    type: 'success',
                                    styling: 'bootstrap3'
                                });
                            </script>";
                        ClientScript.RegisterStartupScript(this.GetType(), "ShowDataSuccessNotification", Data_SuccessScript, false);

                        // Optionally clear the GridView and ViewState
                        //ViewState["GridData"] = null;
                        //gvData.DataSource = null;
                        //gvData.DataBind();

                        DisableFormInputs();
                    }
                    else
                    {
                        lbl_svall_msg.Text = "No data available in the grid to insert.";
                        lbl_svall_msg.ForeColor = System.Drawing.Color.Red;
                    }
                }
                else
                {
                    lbl_svall_msg.Text = "No data available in ViewState.";
                    lbl_svall_msg.ForeColor = System.Drawing.Color.Red;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                lbl_svall_msg.Text = "Error: " + ex.Message;
                lbl_svall_msg.ForeColor = System.Drawing.Color.Red;
            }
        }

    }
}