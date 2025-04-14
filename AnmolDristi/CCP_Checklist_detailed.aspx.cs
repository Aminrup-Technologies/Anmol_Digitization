using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using Newtonsoft.Json;
using System.Drawing;

namespace AnmolDristi
{
    public partial class CCP_Checklist_detailed : System.Web.UI.Page
    {
        public static String RecordID = String.Empty;
        public static Int32 ViewerMode = 0;

        public static string PlantId = string.Empty;
        public static string PlantName = string.Empty;
        public static string PlantLine = string.Empty;

        public static string JSON1 = string.Empty;
        public static string JSON2 = string.Empty;
        public static string JSON3 = string.Empty;

        public static string App1_Status = string.Empty;
        public static string App2_Status = string.Empty;
        public static string DottedApp_Status = string.Empty;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.QueryString["ID"] != null)
            {
                RecordID = Request.QueryString["ID"];
                ViewerMode = Convert.ToInt32(Request.QueryString["VM"]);
                PlantBinder();
                DataBinder(RecordID);
            }
        }

        private void DataBinder(string ccp)
        {
            getBasicDetails(ccp);
            MetalCheckData(ccp);
            SeiveConditionData(ccp);
            MetalDetectorData(ccp);
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
            }
            else
            {

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


        void getBasicDetails(string ccp)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                string query = @"
                        SELECT
                            P.PlantId as PlantID,
                            A.plant_name,
                            
                            P.MetalCheck,
                            P.SieveCheck,
                            P.MD_Remarks,
                            P.MetalDataJson1,
                            P.*
                        FROM
                            TRN_CCP_Checklist P
                        JOIN dbo.MST_PlantDetails A ON P.PlantId = A.plant_id
                        WHERE P.Id = @CcpId";

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@CcpId", ccp);

                        con.Open();
                        using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                        {
                            System.Data.DataTable dt = new System.Data.DataTable();
                            sda.Fill(dt);

                            if (dt.Rows.Count > 0)
                            {
                                DataRow row = dt.Rows[0];

                                string plantId = row["PlantID"].ToString();
                                string plantName = row["plant_name"].ToString();
                                DDL_Plant.SelectedItem.Text = plantName;
                                DDL_Plant.Enabled = false;

                                JSON1 = row["MetalCheck"].ToString();
                                JSON2 = row["SieveCheck"].ToString();
                                TB_MDRemarks.Text = row["MD_Remarks"].ToString();
                                JSON3 = row["MetalDataJson1"].ToString();


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
                            else
                            {
                                Response.Write("<script>alert('No data found for the given CcpId.');</script>");
                            }
                        }
                    }
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
                                     "});\n" +
                                     "</script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowErrorNotification", errorScript, false);
            }
        }


        //Metal Check
        public class MetalCheck
        {
            public int Sl { get; set; }
            public string L { get; set; }
            public string QMF { get; set; }
            public string CS { get; set; }
            public string CSR { get; set; }

        }

        private void MetalCheckData(string ccp)
        {
            Console.WriteLine($"Value of CcpId: {ccp}");

            if (string.IsNullOrEmpty(ccp))
            {
                throw new ArgumentException("The parameter CcpId cannot be null or empty.");
            }


            string jsonData = JSON1;

            if (!string.IsNullOrEmpty(jsonData))
            {
                try
                {
                    List<MetalCheck> MetalCheckList = JsonConvert.DeserializeObject<List<MetalCheck>>(jsonData);

                    // Filter rows where at least one field is not empty
                    var filteredList = MetalCheckList.Where(item =>
                        !string.IsNullOrEmpty(item.QMF) ||
                        !string.IsNullOrEmpty(item.CS) ||
                        !string.IsNullOrEmpty(item.CSR)).ToList();

                    DataTable dataTable = new DataTable();
                    dataTable.Columns.Add("Sl", typeof(int));
                    dataTable.Columns.Add("Location", typeof(string));
                    dataTable.Columns.Add("Qty of Metal Found(gm)", typeof(string));
                    dataTable.Columns.Add("CleanedStatus", typeof(string));
                    dataTable.Columns.Add("Remarks for not ok", typeof(string));

                    foreach (var item in filteredList)
                    {
                        DataRow row = dataTable.NewRow();
                        row["Sl"] = item.Sl;
                        row["Location"] = item.L;
                        row["Qty of Metal Found(gm)"] = item.QMF;
                        row["CleanedStatus"] = item.CS;
                        row["Remarks for not ok"] = item.CSR;
                        dataTable.Rows.Add(row);
                    }

                    GridView_MetalCheck.DataSource = dataTable;
                    GridView_MetalCheck.DataBind();

                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Error deserializing JSON: {ex.Message}");
                }
            }
            else
            {
                GridView_MetalCheck.DataSource = null;
                GridView_MetalCheck.DataBind();
                Console.WriteLine("No CcpId value provided.");
            }
        }


        //Seive Condition
        public class SeiveCheck
        {
            public int Sl { get; set; }
            public string SN { get; set; }
            public double ISV { get; set; }
            public double FRV { get; set; }
            public double PER { get; set; }

        }

        private void SeiveConditionData(string ccp)
        {
            Console.WriteLine($"Value of CcpId: {ccp}");

            if (string.IsNullOrEmpty(ccp))
            {
                throw new ArgumentException("The parameter CcpId cannot be null or empty.");
            }


            string jsonData = JSON2;

            if (!string.IsNullOrEmpty(jsonData))
            {
                try
                {
                    List<SeiveCheck> SeiveCheckList = JsonConvert.DeserializeObject<List<SeiveCheck>>(jsonData);

                    // Filter rows where at least one field has non-zero values
                    var filteredList = SeiveCheckList.Where(item =>
                        item.ISV != 0 || item.FRV != 0 || item.PER != 0).ToList();

                    DataTable dataTable = new DataTable();
                    dataTable.Columns.Add("Sl", typeof(int));
                    dataTable.Columns.Add("Sieve No", typeof(string));
                    dataTable.Columns.Add("Initial Sample", typeof(double));
                    dataTable.Columns.Add("Final Retention", typeof(double));
                    dataTable.Columns.Add("Percentage Retention", typeof(double));

                    foreach (var item in filteredList)
                    {
                        DataRow row = dataTable.NewRow();
                        row["Sl"] = item.Sl;
                        row["Sieve No"] = item.SN;
                        row["Initial Sample"] = item.ISV;
                        row["Final Retention"] = item.FRV;
                        row["Percentage Retention"] = item.PER;
                        dataTable.Rows.Add(row);
                    }

                    GridView_Shieve.DataSource = dataTable;
                    GridView_Shieve.DataBind();

                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Error deserializing JSON: {ex.Message}");
                }
            }
            else
            {
                GridView_Shieve.DataSource = null;
                GridView_Shieve.DataBind();
                Console.WriteLine("No CcpId value provided.");
            }
        }



        //Metal Detector
        public class MetalData
        {
            public string PlantLine { get; set; }
            public string FF_Status { get; set; }
            public string FF_Remarks { get; set; }
            public string NFE_Status { get; set; }
            public string NFE_Remarks { get; set; }
            public string SS_Status { get; set; }
            public string SS_Remarks { get; set; }

        }

        private void MetalDetectorData(string ccp)
        {
            Console.WriteLine($"Value of CcpId: {ccp}");

            if (string.IsNullOrEmpty(ccp))
            {
                throw new ArgumentException("The parameter CcpId cannot be null or empty.");
            }


            string jsonData = JSON3;

            if (!string.IsNullOrEmpty(jsonData))
            {
                try
                {
                    List<MetalData> MetalDataList = JsonConvert.DeserializeObject<List<MetalData>>(jsonData);

                    DataTable dataTable = new DataTable();
                    dataTable.Columns.Add("PlantLine", typeof(string));
                    dataTable.Columns.Add("FF_Status", typeof(string));
                    dataTable.Columns.Add("FF_Remarks", typeof(string));
                    dataTable.Columns.Add("NFE_Status", typeof(string));
                    dataTable.Columns.Add("NFE_Remarks", typeof(string));
                    dataTable.Columns.Add("SS_Status", typeof(string));
                    dataTable.Columns.Add("SS_Remarks", typeof(string));

                    foreach (var item in MetalDataList)
                    {
                        DataRow row = dataTable.NewRow();
                        row["PlantLine"] = item.PlantLine;
                        row["FF_Status"] = item.FF_Status;
                        row["FF_Remarks"] = item.FF_Remarks;
                        row["NFE_Status"] = item.NFE_Status;
                        row["NFE_Remarks"] = item.NFE_Remarks;
                        row["SS_Status"] = item.SS_Status;
                        row["SS_Remarks"] = item.SS_Remarks;
                        dataTable.Rows.Add(row);
                    }

                    Magnetgrid.DataSource = dataTable;
                    Magnetgrid.DataBind();

                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Error deserializing JSON: {ex.Message}");
                }
            }
            else
            {
                Magnetgrid.DataSource = null;
                Magnetgrid.DataBind();
                Console.WriteLine("No CcpId value provided.");
            }
        }

        protected void btnApprove_Click(object sender, EventArgs e)
        {
            UpdateColumnBasedOnApproverType();
            DataBinder(RecordID);
        }

        protected void btnReject_Click(object sender, EventArgs e)
        {
            RejectionBasedOnApproverType();
            DataBinder(RecordID);
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
                            updateQuery = "UPDATE TRN_CCP_Checklist SET Approver1_Status = 1, Approver1_TimeStamp = @TimeStamp WHERE Approver1EmployeeCode = @Condition and ID=@ID";
                            break;
                        case "Approver2":
                            updateQuery = "UPDATE TRN_CCP_Checklist SET Approver2_Status = 1, Approver2_TimeStamp = @TimeStamp WHERE Approver2EmployeeCode = @Condition and ID=@ID";
                            break;
                        case "DottedLineApprover":
                            updateQuery = "UPDATE TRN_CCP_Checklist SET DottedApprover_Status = 1, DottedApprover_TimeStamp = @TimeStamp WHERE DottedLineApproverEmployeeCode = @Condition and ID=@ID";
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
                            updateQuery = "UPDATE TRN_CCP_Checklist SET Approver1_Status = 0, Approver1_TimeStamp = @TimeStamp WHERE Approver1EmployeeCode = @Condition and ID=@ID";
                            break;
                        case "Approver2":
                            updateQuery = "UPDATE TRN_CCP_Checklist SET Approver2_Status = 0, Approver2_TimeStamp = @TimeStamp WHERE Approver2EmployeeCode = @Condition and ID=@ID";
                            break;
                        case "DottedLineApprover":
                            updateQuery = "UPDATE TRN_CCP_Checklist SET DottedApprover_Status = 0, DottedApprover_TimeStamp = @TimeStamp WHERE DottedLineApproverEmployeeCode = @Condition and ID=@ID";
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
                                Lbl_btnSubmit.Text = ex.Message;
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
                Response.Redirect("vm_ccp_checklist.aspx", false);
            }
            else if (ViewerMode == 1)
            {
                Response.Redirect("home.aspx", false);
            }
            else
            {
                Response.Redirect("home.aspx", false);
            }
        }

    }
}