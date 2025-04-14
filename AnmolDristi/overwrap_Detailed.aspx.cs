using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Configuration;

namespace AnmolDristi
{
    public partial class overwrap_Detailed : System.Web.UI.Page
    {
        public static Int32 RecordID = 0;
        public static Int32 ViewerMode = 0;

        public static string PlantId = string.Empty;
        public static string PlantName = string.Empty;
        public static string CategoryBrand = string.Empty;
        public static string App1_Status = string.Empty;
        public static string App2_Status = string.Empty;
        public static string DottedApp_Status = string.Empty;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Request.QueryString["DBID"] != null)
                {
                    string id = Request.QueryString["DBID"];
                    RecordID = Convert.ToInt32(id);
                    PlantBinder();
                    LoadRecordData(RecordID);
                }

                string source = Request.QueryString["source"];

                if (source == "report")
                {
                    btnApprove.Visible = false;
                    btnReject.Visible = false;
                    btnBack.PostBackUrl = "~/vm_overwrap.aspx";
                }
                else
                {
                    btnBack.PostBackUrl = "~/overwrap_approval.aspx";
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

        //private void ProductBrandsBinder(string selectedPlantValue)
        //{
        //    // Construct the SQL query with parameters
        //    string query = "SELECT brand_id, brand_name FROM MST_LineCatBrands WHERE plant_id = @PlantId ";
        //    string textField = "brand_name"; // Assuming this is the correct field for displaying in the DropDownList
        //    string valueField = "brand_id"; // Assuming this is the correct field for storing in the DropDownList

        //    // Create SQL parameters for plant_id and line_id
        //    SqlParameter[] parameters = new SqlParameter[]
        //    {
        //        new SqlParameter("@PlantId", selectedPlantValue)
        //    };

        //    // Call the BindDropDownList method with parameters
        //    bool recordsBound;
        //    DatabaseHelper.BindDropDownList(query, DDL_ProductBrand, textField, valueField, parameters, out recordsBound);

        //    // Check if any records were bound
        //    if (!recordsBound)
        //    {
        //        string ProductBrands_Error_script = @"<script type='text/javascript'>
        //            new PNotify({
        //                title: 'Error',
        //                text: 'No Brands found for the selected plant and line!',
        //                type: 'error',
        //                styling: 'bootstrap3'
        //            });
        //        </script>";

        //        // RegisterStartupScript adds the JavaScript code to the page
        //        ClientScript.RegisterStartupScript(this.GetType(), "ShowProductBrandsBinderErrorNotification", ProductBrands_Error_script, false);
        //    }
        //}


        void LoadRecordData(int id)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                string query = @"
                    SELECT
                        P.PlantName as PlantID,
                        A.plant_name,
                        P.*
                    FROM
                        TRN_OVERWRAP P
                    LEFT JOIN dbo.MST_PlantDetails A ON P.PlantName = A.plant_id
                    WHERE P.Id = @Id";


                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        con.Open();
                        using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                        {
                            System.Data.DataTable dt = new System.Data.DataTable();
                            sda.Fill(dt);
                            if (dt.Rows.Count > 0)
                            {
                                DataRow row = dt.Rows[0];

                                PlantId = dt.Rows[0]["PlantId"].ToString();
                                PlantName = dt.Rows[0]["plant_name"].ToString();
                                DDL_Plant.SelectedItem.Text = PlantName; //This is for binding the DDL using TEXT
                                DDL_Plant.Enabled = false;
                                TB_MatVarietyName.Text = dt.Rows[0]["ProductBrand"].ToString();
                                TB_Supplier.Text = dt.Rows[0]["SupplierName"].ToString();

                                TB_ChallanNo.Text = dt.Rows[0]["ChallanNo"].ToString();
                                TB_ChallanDate.Text = dt.Rows[0]["ChallanDate"].ToString();
                                TB_LotNo.Text = dt.Rows[0]["LotGateNo"].ToString();
                                TB_VehicleNo.Text = dt.Rows[0]["VehicleNo"].ToString();

                                TB_SealingValue.Text = dt.Rows[0]["SealingValue"].ToString();

                                Label_TB_Std_Dimension.Text = dt.Rows[0]["DimensionStd"].ToString();
                                TB_Std_Dimension.Text = dt.Rows[0]["DimensionObs"].ToString();
                                TXB_Std_Dimension_Remarks.Text = dt.Rows[0]["RemarkForDimensionStd"].ToString();

                                Label_Std_GSM.Text = dt.Rows[0]["GMS_Std"].ToString();
                                TB_GSM.Text = dt.Rows[0]["GSM_Obs"].ToString();
                                TXB_GSM_Remarks.Text = dt.Rows[0]["RemarkForGSM_Std"].ToString();

                                TXB_Remarks.Text = dt.Rows[0]["Remarks"].ToString();

                                //Approver1CodeLabel.Text = dt.Rows[0]["Approver1EmployeeCode"].ToString();
                                //Approver2CodeLabel.Text = dt.Rows[0]["Approver2EmployeeCode"].ToString();
                                //DottedLineApproverCodeLabel.Text = dt.Rows[0]["DottedLineApproverEmployeeCode"].ToString();

                                // Assume the logged-in user's Employee Code is stored in a session variable
                                string loggedInUserCode = Session["WORKMAN"].ToString(); // Example session variable

                                // Retrieve approval statuses from the row
                                App1_Status = row["Approver1_Status"].ToString();
                                Approver1CodeLabel.Text = row["Approver1EmployeeCode"].ToString();

                                App2_Status = row["Approver2_Status"].ToString();
                                Approver2CodeLabel.Text = row["Approver2EmployeeCode"].ToString();

                                DottedApp_Status = row["DottedApprover_Status"].ToString();
                                DottedLineApproverCodeLabel.Text = row["DottedLineApproverEmployeeCode"].ToString();

                                // Boolean flag to track if the logged-in user is one of the approvers
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
            LoadRecordData(RecordID);
        }

        protected void btnReject_Click(object sender, EventArgs e)
        {
            RejectionBasedOnApproverType();
            LoadRecordData(RecordID);
        }


        public void UpdateColumnBasedOnApproverType()
        {
            // Get the logged-in employee code from session
            string employeeCode = Session["WORKMAN"] as string;

            if (!string.IsNullOrEmpty(employeeCode))
            {
                // Retrieve the approver codes from the labels in the approver-flow div
                string approver1Code = Approver1CodeLabel.Text.ToString();
                string approver2Code = Approver2CodeLabel.Text.ToString();
                string dottedLineApproverCode = DottedLineApproverCodeLabel.Text.ToString();

                // Determine the approver type based on the employee code
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
                    // Define the connection string
                    string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

                    // Perform SQL operation based on the approver type
                    string updateQuery = string.Empty;

                    switch (approverType)
                    {
                        case "Approver1":
                            updateQuery = "UPDATE TRN_qcinspector SET Approver1_Status = 1, Approver1_TimeStamp = @TimeStamp WHERE Approver1EmployeeCode = @Condition and ID=@ID";
                            break;
                        case "Approver2":
                            updateQuery = "UPDATE TRN_qcinspector SET Approver2_Status = 1, Approver2_TimeStamp = @TimeStamp WHERE Approver2EmployeeCode = @Condition and ID=@ID";
                            break;
                        case "DottedLineApprover":
                            updateQuery = "UPDATE TRN_qcinspector SET DottedApprover_Status = 1, DottedApprover_TimeStamp = @TimeStamp WHERE DottedLineApproverEmployeeCode = @Condition and ID=@ID";
                            break;
                    }

                    if (!string.IsNullOrEmpty(updateQuery))
                    {
                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            SqlCommand cmd = new SqlCommand(updateQuery, conn);
                            cmd.Parameters.AddWithValue("@TimeStamp", DateTime.Now);
                            cmd.Parameters.AddWithValue("@Condition", employeeCode);
                            cmd.Parameters.AddWithValue("@ID", RecordID);
                            try
                            {
                                conn.Open();
                                cmd.ExecuteNonQuery();
                                Lbl_btnSubmit.Text = "Approved";
                            }
                            catch (Exception ex)
                            {
                                // Handle exceptions (e.g., logging, rethrowing)
                                //throw new Exception("Error updating the table.", ex);
                                Lbl_btnSubmit.Text = ex.Message;
                            }
                        }
                    }
                }
            }
        }

        public void RejectionBasedOnApproverType()
        {
            // Get the logged-in employee code from session
            string employeeCode = Session["WORKMAN"] as string;

            if (!string.IsNullOrEmpty(employeeCode))
            {
                // Retrieve the approver codes from the labels in the approver-flow div
                string approver1Code = Approver1CodeLabel.Text.ToString();
                string approver2Code = Approver2CodeLabel.Text.ToString();
                string dottedLineApproverCode = DottedLineApproverCodeLabel.Text.ToString();

                // Determine the approver type based on the employee code
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
                    // Define the connection string
                    string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

                    // Perform SQL operation based on the approver type
                    string updateQuery = string.Empty;

                    switch (approverType)
                    {
                        case "Approver1":
                            updateQuery = "UPDATE TRN_qcinspector SET Approver1_Status = 0, Approver1_TimeStamp = @TimeStamp WHERE Approver1EmployeeCode = @Condition and ID=@ID";
                            break;
                        case "Approver2":
                            updateQuery = "UPDATE TRN_qcinspector SET Approver2_Status = 0, Approver2_TimeStamp = @TimeStamp WHERE Approver2EmployeeCode = @Condition and ID=@ID";
                            break;
                        case "DottedLineApprover":
                            updateQuery = "UPDATE TRN_qcinspector SET DottedApprover_Status = 0, DottedApprover_TimeStamp = @TimeStamp WHERE DottedLineApproverEmployeeCode = @Condition and ID=@ID";
                            break;
                    }

                    if (!string.IsNullOrEmpty(updateQuery))
                    {
                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            SqlCommand cmd = new SqlCommand(updateQuery, conn);
                            cmd.Parameters.AddWithValue("@TimeStamp", DateTime.Now);
                            cmd.Parameters.AddWithValue("@Condition", employeeCode);
                            cmd.Parameters.AddWithValue("@ID", RecordID);
                            try
                            {
                                conn.Open();
                                cmd.ExecuteNonQuery();
                                Lbl_btnSubmit.Text = "Rejected";
                            }
                            catch (Exception ex)
                            {
                                // Handle exceptions (e.g., logging, rethrowing)
                                Lbl_btnSubmit.Text = ex.Message;
                                //throw new Exception("Error updating the table.", ex);
                            }
                        }
                    }
                }
            }
        }

        protected void btnBack_Click(object sender, EventArgs e)
        {
            if (ViewerMode == 0)
            {
                Response.Redirect("qaqc_qcinspector_rpt_.aspx", false);
            }
            else if (ViewerMode == 1)
            {
                Response.Redirect("app_qc_inspectorreport.aspx", false);
            }
            else
            {
                Response.Redirect("home.aspx", false);
            }
        }

    }
}