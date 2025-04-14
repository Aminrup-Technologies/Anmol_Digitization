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
        public static string PDCR_key = String.Empty;
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

                    lbl_docname.Text = "QA - PRE DISPATCH CLEARANCE REPORT";
                    lbl_docnumber.Text = "ANMOL/DOC/CORP/QA/06";

                    PlantBinder();
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

            PDCR_key = newLspId;
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
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            // Get form values
            string plantName = DDL_Plant.SelectedValue;
            string plantLine = DDL_PlantLine.SelectedValue;
            string productCategory = DDL_ProductCategory.SelectedValue;
            string productBrand = DDL_ProductBrand.SelectedValue;
            string brandSKU = DDL_BrandSKU.SelectedValue;

            //string shift = TB_Shift.Text;
            string shift = hdn_shiftvalue.Value.ToString();

            // string productBrand = TB_ProductBrand.Text;
            string inspectionLot = TXT_InspectionLot.Text;
            string materialCode = TXT_MaterialCode.Text;
            decimal? cbbProduced = string.IsNullOrEmpty(TXT_CBB_Produced.Text) ? (decimal?)null : Convert.ToDecimal(TXT_CBB_Produced.Text);
            decimal? cbbChecked = string.IsNullOrEmpty(TXT_CBB_Checked.Text) ? (decimal?)null : Convert.ToDecimal(TXT_CBB_Checked.Text);
            int cbbBoxCondition = Convert.ToInt32(RBL_BoxCondition.SelectedValue);
            string cbbBoxConditionRemarks = TXB_BoxCondition_Remarks.Text;
            decimal? packetsCBB = string.IsNullOrEmpty(TXT_Packets_CBB.Text) ? (decimal?)null : Convert.ToDecimal(TXT_Packets_CBB.Text);
            int cbbTapping = Convert.ToInt32(RBL_Tapping.SelectedValue);
            string cbbTappingRemarks = TXB_Tapping_Remarks.Text;
            decimal? packetsCheckedCBB = string.IsNullOrEmpty(TXT_Pkts_Checked_Per_CBB.Text) ? (decimal?)null : Convert.ToDecimal(TXT_Pkts_Checked_Per_CBB.Text);
            decimal? weightOfPackets = string.IsNullOrEmpty(TXT_Wt_of_Pkts.Text) ? (decimal?)null : Convert.ToDecimal(TXT_Wt_of_Pkts.Text);
            DateTime? packageDate = string.IsNullOrEmpty(TXT_PackageDate.Text) ? (DateTime?)null : Convert.ToDateTime(TXT_PackageDate.Text);
            string batchNo = TXT_BatchNo.Text;
            decimal? packetsMRP = string.IsNullOrEmpty(TXT_PacketsMRP.Text) ? (decimal?)null : Convert.ToDecimal(TXT_PacketsMRP.Text);
            int longSeal = Convert.ToInt32(RBL_LongSeal.SelectedValue);
            string longSealRemarks = TXB_LongSeal_Remarks.Text;
            int endSeal = Convert.ToInt32(RBL_EndSeal.SelectedValue);
            string endSealRemarks = TXB_EndSeal_Remarks.Text;
            int mainPanel = Convert.ToInt32(RBL_MainPanel.SelectedValue);
            string mainPanelRemarks = TXB_MainPanel_Remarks.Text;
            int cutsPackets = Convert.ToInt32(RBL_CutsPackets.SelectedValue);
            string cutsPacketsRemarks = TXB_CutsPackets_Remarks.Text;
            int backingStatus = Convert.ToInt32(RBL_BackingStatus.SelectedValue);
            string backingStatusRemarks = TXB_BackingStatus_Remarks.Text;
            int elongOval = Convert.ToInt32(RBL_ElongOval.SelectedValue);
            string elongOvalRemarks = TXB_ElongOval_Remarks.Text;
            int cupping = Convert.ToInt32(RBL_Cupping.SelectedValue);
            string cuppingRemarks = TXB_Cupping_Remarks.Text;
            int impression = Convert.ToInt32(RBL_Impression.SelectedValue);
            string impressionRemarks = TXB_Impression_Remarks.Text;
            int soggyStatus = Convert.ToInt32(RBL_SoggyStatus.SelectedValue);
            string soggyStatusRemarks = TXB_SoggyStatus_Remarks.Text;
            int foreignBody = Convert.ToInt32(RBL_ForeignBody.SelectedValue);
            string foreignBodyRemarks = TXB_ForeignBody_Remarks.Text;
            int offOdour = Convert.ToInt32(RBL_OffOdour.SelectedValue);
            string offOdourRemarks = TXB_OffOdour_Remarks.Text;
            string remarks = TXB_Remarks.Text;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // SQL Insert Query
                    string query = @"
                INSERT INTO TRN_Pre_Dispatch_Clearance_Report
                (FormID, PDCR_PK, PlantName, Line, ProductCategory, SubmittedById, SubmittedByEmployeeCode, Approver1EmployeeCode, 
                Approver1_Status, Approver1_TimeStamp, SubmittedDate, SubmittedTime, Shift, ProductBrand, 
                InspectionLot, MaterialCode, CBB_Produced, CBB_Checked, CBB_Box_Condition, CBB_Box_ConditionRemarks, Packets_CBB, 
                CBB_tapping, CBB_tappingRemarks, PacketsChecked_CBB, WeightofPackets, PackageDate, BatchNo, PacketsMRP, 
                LongSeal, LongSealRemarks, EndSeal, EndSealRemarks, MainPanel, MainPanelRemarks, Cuts_Packets, Cuts_PacketsRemarks, 
                BackingStatus, BackingStatusRemarks, ElongOval, ElongOvalRemarks, Cupping, CuppingRemarks, 
                Impression, ImpressionRemarks, SoggyStatus, SoggyStatusRemarks, ForeignBody, ForeignBodyRemarks, OffOdour, OffOdourRemarks, 
                Remarks, SKUId, ViewMode, DeleteMode, Approver2EmployeeCode, Approver2_Status, Approver2_TimeStamp, 
                DottedLineApproverEmployeeCode, DottedApprover_Status, DottedApprover_TimeStamp)
                VALUES 
                (@FormID, @PDCR_PK, @PlantName, @Line, @ProductCategory, @SubmittedById, @SubmittedByEmployeeCode, 
                @Approver1EmployeeCode, @Approver1_Status, @Approver1_TimeStamp, @SubmittedDate, @SubmittedTime, @Shift, 
                @ProductBrand, @InspectionLot, @MaterialCode, @CBB_Produced, @CBB_Checked, @CBB_Box_Condition, 
                @CBB_Box_ConditionRemarks, @Packets_CBB, @CBB_tapping, @CBB_tappingRemarks, @PacketsChecked_CBB, 
                @WeightofPackets, @PackageDate, @BatchNo, @PacketsMRP, @LongSeal, @LongSealRemarks, @EndSeal, 
                @EndSealRemarks, @MainPanel, @MainPanelRemarks, @Cuts_Packets, @Cuts_PacketsRemarks, @BackingStatus, 
                @BackingStatusRemarks, @ElongOval, @ElongOvalRemarks, @Cupping, @CuppingRemarks, @Impression, 
                @ImpressionRemarks, @SoggyStatus, @SoggyStatusRemarks, @ForeignBody, @ForeignBodyRemarks, 
                @OffOdour, @OffOdourRemarks, @Remarks, @SKUId, @ViewMode, @DeleteMode, @Approver2EmployeeCode, 
                @Approver2_Status, @Approver2_TimeStamp, @DottedLineApproverEmployeeCode, 
                @DottedApprover_Status, @DottedApprover_TimeStamp)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        // Add parameters
                        cmd.Parameters.AddWithValue("@FormID", Convert.ToInt32(hdn_formid.Value.ToString()));
                        cmd.Parameters.AddWithValue("@PDCR_PK", Find_DBCode());
                        cmd.Parameters.AddWithValue("@PlantName", plantName);
                        cmd.Parameters.AddWithValue("@Line", plantLine);
                        cmd.Parameters.AddWithValue("@ProductCategory", productCategory);
                        cmd.Parameters.AddWithValue("@ProductBrand", productBrand);
                        cmd.Parameters.AddWithValue("@SKUId", brandSKU);
                        cmd.Parameters.AddWithValue("@SubmittedById", Convert.ToInt32(Convert.ToInt32(Session["USERID"].ToString())));
                        cmd.Parameters.AddWithValue("@SubmittedByEmployeeCode", Session["WORKMAN"].ToString());
                        cmd.Parameters.AddWithValue("@Approver1EmployeeCode", Approver1CodeLabel.Text.ToString());
                        cmd.Parameters.AddWithValue("@Approver1_Status", 0);
                        cmd.Parameters.AddWithValue("@Approver1_TimeStamp", DBNull.Value);
                        cmd.Parameters.AddWithValue("@Approver2EmployeeCode", Approver2CodeLabel.Text.ToString());
                        cmd.Parameters.AddWithValue("@Approver2_Status", 0);
                        cmd.Parameters.AddWithValue("@Approver2_TimeStamp", DBNull.Value);
                        cmd.Parameters.AddWithValue("@DottedLineApproverEmployeeCode", DottedLineApproverCodeLabel.Text.ToString());
                        cmd.Parameters.AddWithValue("@DottedApprover_Status", 0);
                        cmd.Parameters.AddWithValue("@DottedApprover_TimeStamp", DBNull.Value);
                        cmd.Parameters.AddWithValue("@ViewMode", 1);
                        cmd.Parameters.AddWithValue("@DeleteMode", 0);
                        cmd.Parameters.AddWithValue("@SubmittedDate", DateTime.Now.Date);
                        cmd.Parameters.AddWithValue("@SubmittedTime", DateTime.Now.TimeOfDay);
                        cmd.Parameters.AddWithValue("@Shift", shift);
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

                        // Execute the query
                        cmd.ExecuteNonQuery();
                    }

                    conn.Close();
                }


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

                //// RegisterStartupScript adds the JavaScript code to the page
                ClientScript.RegisterStartupScript(this.GetType(), "ShowDataSuccessNotification", Data_SuccessScript, false);



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

            // Reset form fields after submission, if needed
            DDL_Plant.SelectedIndex = 0;
            DDL_PlantLine.SelectedIndex = 0;
            DDL_ProductCategory.SelectedIndex = 0;
            DDL_ProductBrand.SelectedIndex = 0;
            DDL_BrandSKU.SelectedIndex = 0;
            hdn_shiftvalue.Value = string.Empty;

            TXT_InspectionLot.Text = string.Empty;
            TXT_MaterialCode.Text = string.Empty;
            TXT_CBB_Produced.Text = string.Empty;
            TXT_CBB_Checked.Text = string.Empty;
            RBL_BoxCondition.SelectedIndex = -1;
            TXB_BoxCondition_Remarks.Text = string.Empty;
            TXT_Packets_CBB.Text = string.Empty;
            RBL_Tapping.SelectedIndex = -1;
            TXB_Tapping_Remarks.Text = string.Empty;
            TXT_Pkts_Checked_Per_CBB.Text = string.Empty;
            TXT_Wt_of_Pkts.Text = string.Empty;
            TXT_PackageDate.Text = string.Empty;
            TXT_BatchNo.Text = string.Empty;
            TXT_PacketsMRP.Text = string.Empty;
            RBL_LongSeal.SelectedIndex = -1;
            TXB_LongSeal_Remarks.Text = string.Empty;
            RBL_EndSeal.SelectedIndex = -1;
            TXB_EndSeal_Remarks.Text = string.Empty;
            RBL_MainPanel.SelectedIndex = -1;
            TXB_MainPanel_Remarks.Text = string.Empty;
            RBL_CutsPackets.SelectedIndex = -1;
            TXB_CutsPackets_Remarks.Text = string.Empty;
            RBL_BackingStatus.SelectedIndex = -1;
            TXB_BackingStatus_Remarks.Text = string.Empty;
            RBL_ElongOval.SelectedIndex = -1;
            TXB_ElongOval_Remarks.Text = string.Empty;
            RBL_Cupping.SelectedIndex = -1;
            TXB_Cupping_Remarks.Text = string.Empty;
            RBL_Impression.SelectedIndex = -1;
            TXB_Impression_Remarks.Text = string.Empty;
            RBL_SoggyStatus.SelectedIndex = -1;
            TXB_SoggyStatus_Remarks.Text = string.Empty;
            RBL_ForeignBody.SelectedIndex = -1;
            TXB_ForeignBody_Remarks.Text = string.Empty;
            RBL_OffOdour.SelectedIndex = -1;
            TXB_OffOdour_Remarks.Text = string.Empty;
            TXB_Remarks.Text = string.Empty;

            // Reset any labels or messages if needed
            //lblMessage.Text = string.Empty;

            // Reset the submit button
            btnSubmit.Enabled = true;  // Re-enable the submit button if needed
            btnSubmit.Text = "Submit";  // Reset the text to "Submit"
            btnSubmit.CssClass = "btn btn-sm btn-primary";

        }
    }
}