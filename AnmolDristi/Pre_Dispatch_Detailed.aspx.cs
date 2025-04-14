using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Drawing;

namespace AnmolDristi
{
    public partial class Pre_Dispatch_Detailed : System.Web.UI.Page
    {
        public static string DBID = string.Empty;
        public static string PlantId = string.Empty;
        public static string PlantName = string.Empty;
        public static string PlantLine = string.Empty;
        public static string ProductCategory = string.Empty;
        public static string CategoryBrand = string.Empty;
        public static string BrandSKU = string.Empty;
        public static string App1_Status = string.Empty;
        public static string App2_Status = string.Empty;
        public static string DottedApp_Status = string.Empty;


        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Request.QueryString["ID"] != null)
                {
                    DBID = Request.QueryString["ID"];

                    PlantBinder();
                    getDetails(DBID);
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
        void getDetails(string pdcr)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                string query = @"
                    SELECT
                        P.PlantName as PlantID,
                        A.plant_name,
                        P.Line,
                        B.line_name,
                        C.category_name,
                        D.brand_name,
	                    E.SKU_name,
                        P.*
                    FROM
                        TRN_Pre_Dispatch_Clearance_Report P
                    JOIN dbo.MST_PlantDetails A ON P.PlantName = A.plant_id
                    JOIN dbo.MST_Plant_Lines B ON P.Line = B.line_id
                    JOIN dbo.MST_LineCategory C ON P.ProductCategory = C.category_id
                    JOIN dbo.MST_LineCatBrands D ON P.ProductBrand = D.brand_id
                    JOIN dbo.MST_Brand_SKU E ON P.SKUId = E.SKUId
                    WHERE P.Id = @DBID";


                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@DBID", pdcr);
                        con.Open();
                        using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                        {
                            System.Data.DataTable dt = new System.Data.DataTable();
                            sda.Fill(dt);

                            if (dt.Rows.Count > 0)
                            {
                                // Retrieve the first row
                                DataRow row = dt.Rows[0];

                                string PlantId = dt.Rows[0]["PlantID"].ToString();
                                string PlantName = dt.Rows[0]["plant_name"].ToString();
                                DDL_Plant.SelectedItem.Text = PlantName;

                                PlantLinesBinder(PlantId);
                                string PlantLine = dt.Rows[0]["Line"].ToString();
                                DDL_PlantLine.SelectedValue = PlantLine;

                                LineProductsBinder(PlantId, PlantLine);
                                string ProductCategory = dt.Rows[0]["ProductCategory"].ToString();
                                DDL_ProductCategory.SelectedValue = ProductCategory;

                                ProductBrandsBinder(PlantId, PlantLine, ProductCategory);
                                string CategoryBrand = dt.Rows[0]["ProductBrand"].ToString();
                                DDL_ProductBrand.SelectedValue = CategoryBrand;

                                BrandSKUBinder(CategoryBrand);
                                string BrandSKU = dt.Rows[0]["SKUId"].ToString();
                                DDL_BrandSKU.SelectedValue = BrandSKU;

                                TXT_InspectionLot.Text = dt.Rows[0]["InspectionLot"].ToString();
                                TXT_MaterialCode.Text = dt.Rows[0]["MaterialCode"].ToString();
                                TXT_CBB_Produced.Text = dt.Rows[0]["CBB_Produced"].ToString();
                                TXT_CBB_Checked.Text = dt.Rows[0]["CBB_Checked"].ToString();

                                RBL_BoxCondition.SelectedValue = dt.Rows[0]["CBB_Box_Condition"].ToString();
                                TXB_BoxCondition_Remarks.Text = dt.Rows[0]["CBB_Box_ConditionRemarks"].ToString();

                                TXT_Packets_CBB.Text = dt.Rows[0]["Packets_CBB"].ToString();

                                RBL_Tapping.SelectedValue = dt.Rows[0]["CBB_tapping"].ToString();
                                TXB_Tapping_Remarks.Text = dt.Rows[0]["CBB_tappingRemarks"].ToString();

                                TXT_Pkts_Checked_Per_CBB.Text = dt.Rows[0]["PacketsChecked_CBB"].ToString();
                                TXT_Wt_of_Pkts.Text = dt.Rows[0]["WeightofPackets"].ToString();
                                TXT_PackageDate.Text = dt.Rows[0]["PackageDate"].ToString();
                                TXT_BatchNo.Text = dt.Rows[0]["BatchNo"].ToString();

                                TXT_PacketsMRP.Text = dt.Rows[0]["PacketsMRP"].ToString();

                                RBL_LongSeal.SelectedValue = dt.Rows[0]["LongSeal"].ToString();
                                TXB_LongSeal_Remarks.Text = dt.Rows[0]["LongSealRemarks"].ToString();

                                RBL_EndSeal.SelectedValue = dt.Rows[0]["EndSeal"].ToString();
                                TXB_EndSeal_Remarks.Text = dt.Rows[0]["EndSealRemarks"].ToString();

                                RBL_MainPanel.SelectedValue = dt.Rows[0]["MainPanel"].ToString();
                                TXB_MainPanel_Remarks.Text = dt.Rows[0]["MainPanelRemarks"].ToString();

                                RBL_CutsPackets.SelectedValue = dt.Rows[0]["Cuts_Packets"].ToString();
                                TXB_CutsPackets_Remarks.Text = dt.Rows[0]["Cuts_PacketsRemarks"].ToString();

                                RBL_BackingStatus.SelectedValue = dt.Rows[0]["BackingStatus"].ToString();
                                TXB_BackingStatus_Remarks.Text = dt.Rows[0]["BackingStatusRemarks"].ToString();

                                RBL_ElongOval.SelectedValue = dt.Rows[0]["ElongOval"].ToString();
                                TXB_ElongOval_Remarks.Text = dt.Rows[0]["ElongOvalRemarks"].ToString();

                                RBL_Cupping.SelectedValue = dt.Rows[0]["Cupping"].ToString();
                                TXB_Cupping_Remarks.Text = dt.Rows[0]["CuppingRemarks"].ToString();

                                RBL_Impression.SelectedValue = dt.Rows[0]["Impression"].ToString();
                                TXB_Impression_Remarks.Text = dt.Rows[0]["ImpressionRemarks"].ToString();

                                RBL_SoggyStatus.SelectedValue = dt.Rows[0]["SoggyStatus"].ToString();
                                TXB_SoggyStatus_Remarks.Text = dt.Rows[0]["SoggyStatusRemarks"].ToString();

                                RBL_ForeignBody.SelectedValue = dt.Rows[0]["ForeignBody"].ToString();
                                TXB_ForeignBody_Remarks.Text = dt.Rows[0]["ForeignBodyRemarks"].ToString();

                                RBL_OffOdour.SelectedValue = dt.Rows[0]["OffOdour"].ToString();
                                TXB_OffOdour_Remarks.Text = dt.Rows[0]["OffOdourRemarks"].ToString();

                                TXB_Remarks.Text = dt.Rows[0]["Remarks"].ToString();

                                // Assume the logged-in user's Employee Code is stored in a session variable
                                string loggedInUserCode = Session["WORKMAN"].ToString();

                                App1_Status = row["Approver1_Status"].ToString();
                                Approver1CodeLabel.Text = row["Approver1EmployeeCode"].ToString();
                                App2_Status = row["Approver2_Status"].ToString();
                                Approver2CodeLabel.Text = row["Approver2EmployeeCode"].ToString();
                                DottedApp_Status = row["DottedApprover_Status"].ToString();
                                DottedLineApproverCodeLabel.Text = row["DottedLineApproverEmployeeCode"].ToString();

                                bool isApprover = false;

                                // Approver 1
                                if (App1_Status == "0") // Pending
                                {
                                    Approver1CodeLabel.ForeColor = Color.Brown;
                                    if (loggedInUserCode == row["Approver1EmployeeCode"].ToString())
                                    {
                                        btnApprove.Enabled = true;
                                        btnReject.Enabled = true;
                                        isApprover = true;
                                    }
                                }
                                else if (App1_Status == "1") // Approved
                                {
                                    Approver1CodeLabel.ForeColor = Color.Green;
                                    if (loggedInUserCode == row["Approver1EmployeeCode"].ToString())
                                    {
                                        btnApprove.Text = "Approved";
                                        btnApprove.Enabled = false;
                                        btnReject.Enabled = false;
                                        isApprover = true;
                                        Lbl_btnSubmit.Text = "You have approved!";
                                    }
                                }

                                // Approver 2
                                if (App2_Status == "0") // Pending
                                {
                                    Approver2CodeLabel.ForeColor = Color.Brown;
                                    if (loggedInUserCode == row["Approver2EmployeeCode"].ToString())
                                    {
                                        btnApprove.Enabled = true;
                                        btnReject.Enabled = true;
                                        isApprover = true;
                                    }
                                }
                                else if (App2_Status == "1") // Approved
                                {
                                    Approver2CodeLabel.ForeColor = Color.Green;
                                    if (loggedInUserCode == row["Approver2EmployeeCode"].ToString())
                                    {
                                        btnApprove.Text = "Approved";
                                        btnApprove.Enabled = false;
                                        btnReject.Enabled = false;
                                        isApprover = true;
                                        Lbl_btnSubmit.Text = "You have approved!";
                                    }
                                }

                                // Dotted Line Approver
                                if (DottedApp_Status == "0") // Pending
                                {
                                    DottedLineApproverCodeLabel.ForeColor = Color.Brown;
                                    if (loggedInUserCode == row["DottedLineApproverEmployeeCode"].ToString())
                                    {
                                        btnApprove.Enabled = true;
                                        btnReject.Enabled = true;
                                        isApprover = true;
                                    }
                                }
                                else if (DottedApp_Status == "1") // Approved
                                {
                                    DottedLineApproverCodeLabel.ForeColor = Color.Green;
                                    if (loggedInUserCode == row["DottedLineApproverEmployeeCode"].ToString())
                                    {
                                        btnApprove.Text = "Approved";
                                        btnApprove.Enabled = false;
                                        btnReject.Enabled = false;
                                        isApprover = true;
                                        Lbl_btnSubmit.Text = "You have approved!";
                                    }
                                }

                                // If the logged-in user is not any of the approvers
                                if (!isApprover)
                                {
                                    // Option 1: Disable the buttons
                                    btnApprove.Enabled = false;
                                    btnReject.Enabled = false;

                                    // Option 2: Hide the buttons entirely
                                    // btnApprove.Visible = false;
                                    // btnReject.Visible = false;
                                    string PlantBinder_Error_script = @"<script type='text/javascript'>
                                        new PNotify({
                                            title: 'Error',
                                            text: 'You are not authorized to approve!',
                                            type: 'error',
                                            styling: 'bootstrap3'
                                        });
                                    </script>";

                                    // RegisterStartupScript adds the JavaScript code to the page
                                    ClientScript.RegisterStartupScript(this.GetType(), "ShowPlantBinderErrorNotification", PlantBinder_Error_script, false);
                                    Lbl_btnSubmit.Text = "You are not authorized to approve or reject this form.";

                                }


                                // Combined Actions - Example for handling when all approvers have approved
                                if (App1_Status == "1" && App2_Status == "1" && DottedApp_Status == "1")
                                {
                                    // Perform action when all approvers have approved
                                    // Example: Allow form submission or update status
                                }
                                else if (App1_Status == "0" || App2_Status == "0" || DottedApp_Status == "0")
                                {
                                    // Perform action when any approver is still pending
                                    // Example: Disable form submission or show a pending message
                                }

                            }
                        }
                    }
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

        protected void btnApprove_Click(object sender, EventArgs e)
        {
            UpdateColumnBasedOnApproverType();
            getDetails(DBID);
        }

        protected void btnReject_Click(object sender, EventArgs e)
        {
            RejectionBasedOnApproverType();
            getDetails(DBID);
        }


        public void UpdateColumnBasedOnApproverType()
        {
            string employeeCode = Session["WORKMAN"] as string;

            if (!string.IsNullOrEmpty(employeeCode))
            {
                string approver1Code = Approver1CodeLabel.Text.ToString();
                string approver2Code = Approver2CodeLabel.Text.ToString();
                string dottedLineApproverCode = DottedLineApproverCodeLabel.Text.ToString();

                string approverType = string.Empty;

                if (employeeCode == approver1Code)
                {
                    approverType = "Approver1";
                }
                else if (employeeCode == approver2Code)
                {
                    approverType = "Approver2";
                }
                else if (employeeCode == dottedLineApproverCode)
                {
                    approverType = "DottedLineApprover";
                }

                if (!string.IsNullOrEmpty(approverType))
                {
                    string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                    string updateQuery = string.Empty;

                    switch (approverType)
                    {
                        case "Approver1":
                            updateQuery = "UPDATE TRN_RotaryLine_OvenEnd SET Approver1_Status = 1, Approver1_TimeStamp = @TimeStamp WHERE Approver1EmployeeCode = @Condition and ID=@ID";
                            break;
                        case "Approver2":
                            updateQuery = "UPDATE TRN_RotaryLine_OvenEnd SET Approver2_Status = 1, Approver2_TimeStamp = @TimeStamp WHERE Approver2EmployeeCode = @Condition and ID=@ID";
                            break;
                        case "DottedLineApprover":
                            updateQuery = "UPDATE TRN_RotaryLine_OvenEnd SET DottedApprover_Status = 1, DottedApprover_TimeStamp = @TimeStamp WHERE DottedLineApproverEmployeeCode = @Condition and ID=@ID";
                            break;
                    }

                    if (!string.IsNullOrEmpty(updateQuery))
                    {
                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            SqlCommand cmd = new SqlCommand(updateQuery, conn);
                            cmd.Parameters.AddWithValue("@TimeStamp", DateTime.Now);
                            cmd.Parameters.AddWithValue("@Condition", employeeCode);
                            cmd.Parameters.AddWithValue("@ID", DBID);
                            try
                            {
                                conn.Open();
                                cmd.ExecuteNonQuery();
                                Lbl_btnSubmit.Text = "Approved";
                            }
                            catch (Exception ex)
                            {
                                Lbl_btnSubmit.Text = ex.Message;
                            }
                        }
                    }
                }
            }
        }

        public void RejectionBasedOnApproverType()
        {
            string employeeCode = Session["WORKMAN"] as string;

            if (!string.IsNullOrEmpty(employeeCode))
            {
                string approver1Code = Approver1CodeLabel.Text.ToString();
                string approver2Code = Approver2CodeLabel.Text.ToString();
                string dottedLineApproverCode = DottedLineApproverCodeLabel.Text.ToString();
                string approverType = string.Empty;

                if (employeeCode == approver1Code)
                {
                    approverType = "Approver1";
                }
                else if (employeeCode == approver2Code)
                {
                    approverType = "Approver2";
                }
                else if (employeeCode == dottedLineApproverCode)
                {
                    approverType = "DottedLineApprover";
                }

                if (!string.IsNullOrEmpty(approverType))
                {
                    string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                    string updateQuery = string.Empty;

                    switch (approverType)
                    {
                        case "Approver1":
                            updateQuery = "UPDATE TRN_RotaryLine_OvenEnd SET Approver1_Status = 0, Approver1_TimeStamp = @TimeStamp WHERE Approver1EmployeeCode = @Condition and ID=@ID";
                            break;
                        case "Approver2":
                            updateQuery = "UPDATE TRN_RotaryLine_OvenEnd SET Approver2_Status = 0, Approver2_TimeStamp = @TimeStamp WHERE Approver2EmployeeCode = @Condition and ID=@ID";
                            break;
                        case "DottedLineApprover":
                            updateQuery = "UPDATE TRN_RotaryLine_OvenEnd SET DottedApprover_Status = 0, DottedApprover_TimeStamp = @TimeStamp WHERE DottedLineApproverEmployeeCode = @Condition and ID=@ID";
                            break;
                    }

                    if (!string.IsNullOrEmpty(updateQuery))
                    {
                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            SqlCommand cmd = new SqlCommand(updateQuery, conn);
                            cmd.Parameters.AddWithValue("@TimeStamp", DateTime.Now);
                            cmd.Parameters.AddWithValue("@Condition", employeeCode);
                            cmd.Parameters.AddWithValue("@ID", DBID);
                            try
                            {
                                conn.Open();
                                cmd.ExecuteNonQuery();
                                Lbl_btnSubmit.Text = "Rejected";
                            }
                            catch (Exception ex)
                            {
                                Lbl_btnSubmit.Text = ex.Message;
                            }
                        }
                    }
                }
            }
        }

        protected void btnBack_Click(object sender, EventArgs e)
        {
            Response.Redirect("preDispatchVM.aspx", false);
        }
    }
}