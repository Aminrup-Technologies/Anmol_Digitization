using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using static AnmolDristi.qaqc.qaqc_inspector_rpt;
using Newtonsoft.Json;
using AnmolDristi.DAL;

namespace AnmolDristi
{
    public partial class ccp_checklist : System.Web.UI.Page
    {
        public static string M_CheckId_key = String.Empty;
        DB_Utility_OH4Y dbcl = new DB_Utility_OH4Y();
        public static String CcpId = String.Empty;

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
                    DDL_PlantLine.Enabled = false;

                    int CCPChecklist_MagnetCheck = int.Parse(ConfigurationManager.AppSettings["CCPChecklist_MagnetCheck_GBV"]);
                    int CCPChecklist_SieveCheck = int.Parse(ConfigurationManager.AppSettings["CCPChecklist_SieveCheck_GBV"]);

                    lbl_docname.Text = "CCP CHECKLIST: MD, SS";
                    lbl_docnumber.Text = "ANMOL/DOC/CORP/QA/03";

                    PlantBinder();
                    BindGridView(CCPChecklist_MagnetCheck);
                    Bind_GridView_Shieve(CCPChecklist_SieveCheck);
                    //BindMetalDetector_GridView(5);
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

        private void BindGridView(int rowCount)
        {
            // Create a DataTable with the necessary columns
            DataTable dt = new DataTable();

            // Create columns: Serial No (Sl), Location, Qty of Metal Found(gm), Cleaned Status, Remarks for not ok
            dt.Columns.Add("Sl", typeof(int));
            dt.Columns.Add("Location", typeof(string));
            dt.Columns.Add("Qty of Metal Found(gm)", typeof(string)); // Default to empty, filled later by the user
            dt.Columns.Add("CleanedStatus", typeof(string)); // "Ok" or "Not Ok"
            dt.Columns.Add("Remarks for not ok", typeof(string)); // Optional remarks

            // Predefined list of locations
            List<string> locations = new List<string>
                {
                    "Maida 1", "Maida 2", "Maida 3", "Maida 4", "Maida 5",
                    "Sugar 1", "Sugar 2", "Broken Biscuit / Cake Crumb 1", "Broken Biscuit / Cake Crumb 2"
                };

            // Generate rows based on the provided row count
            for (int i = 0; i < rowCount; i++)
            {
                DataRow dr = dt.NewRow();
                dr["Sl"] = i + 1; // Serial number (Sl)

                // Assign a location from the list, cycling through if needed
                dr["Location"] = locations[i % locations.Count];

                // Initializing other fields with default values, if necessary
                dr["Qty of Metal Found(gm)"] = string.Empty; // To be filled by the user
                dr["CleanedStatus"] = string.Empty; // User selection
                dr["Remarks for not ok"] = string.Empty; // Optional remarks

                // Add the row to the DataTable
                dt.Rows.Add(dr);
            }

            GridView_MetalCheck.DataSource = dt;
            GridView_MetalCheck.DataBind();
        }


        private void Bind_GridView_Shieve(int rowCount)
        {
            // Create a DataTable with the necessary columns
            DataTable dt = new DataTable();

            // Create columns: Serial No (Sl), Sieve No, INITIAL SAMPLE, FINAL RETENTION, % OF RETENTION
            dt.Columns.Add("Sl", typeof(int));
            dt.Columns.Add("SieveNo", typeof(string));
            dt.Columns.Add("InitialSample", typeof(double)); // Default to 0.0, filled later by the user
            dt.Columns.Add("FinalRetention", typeof(double)); // Default to 0.0, filled later by the user
            dt.Columns.Add("PercentageRetention", typeof(double)); // Calculated field

            //dt.Columns.Add("InitialSample", typeof(string)); // Default to 0.0, filled later by the user
            //dt.Columns.Add("FinalRetention", typeof(string)); // Default to 0.0, filled later by the user
            //dt.Columns.Add("PercentageRetention", typeof(string)); // Calculated field

            // Predefined list of sieve numbers
            List<string> sieveNumbers = new List<string>
            {
                "MS01", "MS02", "MS03", "MS04", "MS05", "SS01", "SS02", "SS03", "BBS01", "BBS02", "BBS03"
            };

            // Generate rows based on the provided row count
            for (int i = 0; i < rowCount; i++)
            {
                DataRow dr = dt.NewRow();
                dr["Sl"] = i + 1; // Serial number (Sl)

                // Assign a sieve number from the list, cycling through if needed
                dr["SieveNo"] = sieveNumbers[i % sieveNumbers.Count];

                // Initializing other fields with default values, if necessary
                dr["InitialSample"] = 0.0; // To be filled by the user
                dr["FinalRetention"] = 0.0; // To be filled by the user
                dr["PercentageRetention"] = 0.0; // To be calculated based on InitialSample and FinalRetention

                //// Initializing other fields with default values, if necessary
                //dr["InitialSample"] = string.Empty; // To be filled by the user
                //dr["FinalRetention"] = string.Empty; // To be filled by the user
                //dr["PercentageRetention"] = string.Empty; // To be calculated based on InitialSample and FinalRetention

                // Add the row to the DataTable
                dt.Rows.Add(dr);
            }

            // Bind the DataTable to the GridView
            GridView_Shieve.DataSource = dt;
            GridView_Shieve.DataBind();
        }

        private void BindMetalDetector_GridView(int rowCount)
        {
            // Create a DataTable with the necessary columns
            DataTable dt = new DataTable();

            // Add columns for each field
            dt.Columns.Add("FF_Status", typeof(string));
            dt.Columns.Add("FF_Remarks", typeof(string));
            dt.Columns.Add("NFE_Status", typeof(string));
            dt.Columns.Add("NFE_Remarks", typeof(string));
            dt.Columns.Add("SS_Status", typeof(string));
            dt.Columns.Add("SS_Remarks", typeof(string));
            dt.Columns.Add("MD_Remarks", typeof(string));

            // Generate rows based on the provided row count
            for (int i = 0; i < rowCount; i++)
            {
                DataRow dr = dt.NewRow();

                // Initialize columns with default values
                dr["FF_Status"] = string.Empty; // To be selected by the user
                dr["FF_Remarks"] = string.Empty;
                dr["NFE_Status"] = string.Empty;
                dr["NFE_Remarks"] = string.Empty;
                dr["SS_Status"] = string.Empty;
                dr["SS_Remarks"] = string.Empty;
                dr["MD_Remarks"] = string.Empty;

                // Add the row to the DataTable
                dt.Rows.Add(dr);
            }

            // Bind the DataTable to the GridView
            //GridView_Status.DataSource = dt;
            //GridView_Status.DataBind();
        }


        //protected void SaveData()
        //{
        //    // Get the current shift
        //    string shift = GetCurrentShift();

        //    foreach (GridViewRow row in GridView_MetalCheck.Rows)
        //    {  
        //        // Find controls in the current row
        //        Label lblLineName = (Label)row.FindControl("Label_LineName");
        //        TextBox txtQtyMetalFound = (TextBox)row.FindControl("TB_QtyMetalFound");
        //        RadioButtonList rblCleanedStatus = (RadioButtonList)row.FindControl("RBL_CleanedStatus");
        //        TextBox txtRemarks = (TextBox)row.FindControl("TXB_CleanedStatusRemarks");

        //        // Extract values
        //        string lineName = lblLineName.Text;
        //        decimal qtyMetalFound = string.IsNullOrEmpty(txtQtyMetalFound.Text) ? 0 : Convert.ToDecimal(txtQtyMetalFound.Text);
        //        bool cleanedStatus = rblCleanedStatus.SelectedValue == "1"; // "Ok" = 1, "Not Ok" = 0
        //        string remarks = txtRemarks.Text;

        //        // Define your other parameters
        //        string ccpId = GetNextCcpId(); // Generate CCP ID value
        //        DateTime date = DateTime.Now; // Use current date
        //        int lineNo = GetLineNo(lineName); // Map LineName to LineNo

        //        // SQL query to insert into the database
        //        string query = "INSERT INTO Metal_Check_Table (CcpId, LineNo, Date, Shift, QtyOfMetalFound, CleanedStatus, RemarksForCleanedStatus) " +
        //                       "VALUES (@CcpId, @LineNo, @Date, @Shift, @QtyOfMetalFound, @CleanedStatus, @RemarksForCleanedStatus)";

        //        using (SqlConnection conn = new SqlConnection("your_connection_string_here"))
        //        {
        //            using (SqlCommand cmd = new SqlCommand(query, conn))
        //            {
        //                cmd.Parameters.AddWithValue("@CcpId", ccpId);
        //                cmd.Parameters.AddWithValue("@LineNo", lineNo);
        //                cmd.Parameters.AddWithValue("@Date", date);
        //                cmd.Parameters.AddWithValue("@Shift", shift);
        //                cmd.Parameters.AddWithValue("@QtyOfMetalFound", qtyMetalFound);
        //                cmd.Parameters.AddWithValue("@CleanedStatus", cleanedStatus);
        //                cmd.Parameters.AddWithValue("@RemarksForCleanedStatus", remarks);

        //                conn.Open();
        //                cmd.ExecuteNonQuery();
        //                conn.Close();
        //            }
        //        }
        //    }
        //}

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            SaveGridViewDataToJson();

            //foreach (GridViewRow row in GridView_MetalCheck.Rows)
            //{
            //    // Get controls in each row
            //    Label lblLineNo = (Label)row.FindControl("lblLineNo"); // Assuming LineNo is a label
            //    TextBox txtQtyOfMetalFound = (TextBox)row.FindControl("txtQtyOfMetalFound");
            //    RadioButtonList rblCleanedStatus = (RadioButtonList)row.FindControl("rblCleanedStatus");
            //    TextBox txtRemarks = (TextBox)row.FindControl("txtRemarks");

            //    if (lblLineNo != null && txtQtyOfMetalFound != null && rblCleanedStatus != null)
            //    {
            //        string lineNo = lblLineNo.Text;
            //        decimal qtyOfMetalFound = decimal.Parse(txtQtyOfMetalFound.Text);
            //        string cleanedStatus = rblCleanedStatus.SelectedValue;
            //        string remarks = cleanedStatus == "Not Ok" ? txtRemarks.Text : null;

            //        int sl = row.RowIndex + 1;  // Serial number (Sl)
            //        int mCheckId = GetMCheckIdBySerialNumber(sl); // Retrieve the M_CheckId based on the Sl

            //        UpdateMetalCheckRecord(mCheckId, lineNo, qtyOfMetalFound, cleanedStatus, remarks);
            //    }
            //}
        }

        protected void SaveGridViewDataToJson()
        {
            // List to hold the row data
            var gridViewData = new List<Dictionary<string, object>>();

            // Loop through the rows of the GridView
            foreach (GridViewRow row in GridView_MetalCheck.Rows)
            {
                var rowData = new Dictionary<string, object>();

                // Extract data from each control in the row
                rowData["Sl"] = (row.RowIndex + 1).ToString();
                rowData["L"] = ((Label)row.FindControl("lbl_Location")).Text;
                rowData["QMF"] = ((TextBox)row.FindControl("TB_QtyMetalFound")).Text;
                rowData["CS"] = ((RadioButtonList)row.FindControl("RBL_CleanedStatus")).SelectedValue;
                rowData["CSR"] = ((TextBox)row.FindControl("TXB_CleanedStatusRemarks")).Text;

                // Add the row data to the list
                gridViewData.Add(rowData);
            }

            // Serialize the list to JSON
            string jsonData = JsonConvert.SerializeObject(gridViewData);

            // Save the JSON data to your database
            SaveToDatabase(jsonData);
        }

        private void SaveToDatabase(string jsonData)
        {
            try
            {
                if (CcpId != "" && CcpId!= string.Empty)
                {
                    string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                    string insertQuery = "UPDATE TRN_CCP_Checklist set MetalCheck=@MetalCheck, T2_Status=@T2_Status  where CcpId=@CcpId ";

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    using (SqlCommand command = new SqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@MetalCheck", jsonData);
                        command.Parameters.AddWithValue("@T2_Status", 1);
                        command.Parameters.AddWithValue("@CcpId", CcpId);
                        connection.Open();
                        command.ExecuteNonQuery();

                        BindJsonDataToGridView(jsonData);
                    }
                }
                else
                {
                    string errorMessage = "No Basic Data Found";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "SwitchTab", "document.getElementById('basicData-tab').click();", true);

                    lbl_MagnetCheck.Text = errorMessage;
                    string errorScript = $"new PNotify({{ title: 'Error', text: '{errorMessage}', type: 'error', styling: 'bootstrap3' }});";
                    ClientScript.RegisterStartupScript(this.GetType(), "ShowErrorNotification", errorScript, true);

                }
            }
            catch (Exception ex)
            {
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                string errorMessage = ex.Message.Replace("'", "\\'"); // Escape single quotes in the error message
                lbl_MagnetCheck.Text = errorMessage;
                string JSONInserterrorScript = "<script type='text/javascript'>\n" +
                                     $"new PNotify({{\n" +
                                     "    title: 'Error',\n" +
                                     $"    text: '{errorMessage}',\n" +
                                     "    type: 'error',\n" +
                                     "    styling: 'bootstrap3'\n" +
                                     "}});\n" +
                                     "</script>";
                ClientScript.RegisterStartupScript(this.GetType(), "JSONInsertErrorNotification", JSONInserterrorScript, false);
            }
        }

        protected void BindJsonDataToGridView(string jsonData)
        {
            try
            {
                // Deserialize the JSON data into a list of dictionaries
                var dataList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonData);

                if (dataList == null || !dataList.Any())
                {
                    // Log or handle the case when the deserialized data is null or empty
                    Console.WriteLine("No data found in JSON.");
                    return;
                }

                // Create a DataTable with the necessary columns
                DataTable dt = new DataTable();
                dt.Columns.Add("Location", typeof(string));
                dt.Columns.Add("Qty of Metal Found (gm)", typeof(string));
                dt.Columns.Add("Cleaned Status", typeof(string));
                dt.Columns.Add("Remarks for Not Ok", typeof(string));

                // Populate DataTable from deserialized JSON data
                foreach (var rowData in dataList)
                {
                    DataRow dr = dt.NewRow();
                    dr["Location"] = rowData.ContainsKey("L") ? rowData["L"]?.ToString() : string.Empty;
                    dr["Qty of Metal Found (gm)"] = rowData.ContainsKey("QMF") ? rowData["QMF"]?.ToString() : string.Empty;
                    dr["Cleaned Status"] = rowData.ContainsKey("CS") ? rowData["CS"]?.ToString() : string.Empty;
                    dr["Remarks for Not Ok"] = rowData.ContainsKey("CSR") ? rowData["CSR"]?.ToString() : string.Empty;

                    dt.Rows.Add(dr);
                }

                // Check if DataTable has rows before binding
                if (dt.Rows.Count > 0)
                {
                    GridView1.DataSource = dt;
                    GridView1.DataBind();

                    GridView_MetalCheck.Visible = false;
                    btnSubmit.Enabled = false;
                    btnSubmit.Text = "SAVED";
                    lbl_MagnetCheck.Text = "Data Saved Successfully";

                    ScriptManager.RegisterStartupScript(this, this.GetType(), "SwitchTab", "document.getElementById('Standard_Sieve-tab').click();", true);
                }
                else
                {
                    lbl_MagnetCheck.Text = "DataTable is empty";
                }
            }
            catch (Exception ex)
            {
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                lbl_MagnetCheck.Text = $"Error binding data to GridView: { ex.Message}";
                // Log or handle exceptions
                //Console.WriteLine($"Error binding data to GridView: {ex.Message}");
            }
        }

        protected void GridView1_MetalCheck_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                // Access controls
                Label lblLocation = (Label)e.Row.FindControl("lbl_Location");
                TextBox tbQtyMetalFound = (TextBox)e.Row.FindControl("TB_QtyMetalFound");
                RadioButtonList rblCleanedStatus = (RadioButtonList)e.Row.FindControl("RBL_CleanedStatus");
                TextBox txbCleanedStatusRemarks = (TextBox)e.Row.FindControl("TXB_CleanedStatusRemarks");

                // Customize row based on data
                DataRowView rowView = (DataRowView)e.Row.DataItem;

                // Example customization: Set TextBox value
                tbQtyMetalFound.Text = rowView["Qty of Metal Found (gm)"].ToString();
                rblCleanedStatus.SelectedValue = rowView["Cleaned Status"].ToString();
                txbCleanedStatusRemarks.Text = rowView["Remarks for Not Ok"].ToString();

                // Example: Hide remarks TextBox if status is "Ok"
                if (rblCleanedStatus.SelectedValue == "1")
                {
                    txbCleanedStatusRemarks.Visible = false;
                }
                else
                {
                    txbCleanedStatusRemarks.Visible = true;
                }
            }
        }

        // Method to retrieve M_CheckId based on serial number (Sl)
        private int GetMCheckIdBySerialNumber(int serialNumber)
        {
            // Implement your logic to retrieve the M_CheckId based on serial number
            // For example, you might query the database to get the M_CheckId
            // based on some criteria related to the serial number
            // Example:
            // SELECT M_CheckId FROM Metal_Check_Table WHERE Row_Number = @serialNumber
            return serialNumber; // This is a placeholder. Replace it with your actual retrieval logic.
        }

        // Method to update the Metal_Check_Table record
        private void UpdateMetalCheckRecord(int M_CheckId_key, string lineNo, decimal qtyOfMetalFound, string cleanedStatus, string remarks)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string query = @"
                    UPDATE Metal_Check_Table
                    SET LineNo = @LineNo, 
                        QtyOfMetalFound = @QtyOfMetalFound, 
                        CleanedStatus = @CleanedStatus, 
                        RemarksForCleanedStatus = @Remarks
                    WHERE M_CheckId = @MCheckId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@LineNo", lineNo);
                    cmd.Parameters.AddWithValue("@QtyOfMetalFound", qtyOfMetalFound);
                    cmd.Parameters.AddWithValue("@CleanedStatus", cleanedStatus);
                    cmd.Parameters.AddWithValue("@Remarks", (object)remarks ?? DBNull.Value); // Use DBNull.Value if remarks are null
                    cmd.Parameters.AddWithValue("@MCheckId", M_CheckId_key);

                    cmd.ExecuteNonQuery();
                }
            }
        }
        private string GenerateUniqueM_CheckId()
        {
            string M_CheckId_key;
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Fetch the maximum numeric part of M_CheckId safely
                    string query = @"
            SELECT ISNULL(MAX(CAST(SUBSTRING(M_CheckId, 8, LEN(M_CheckId) - 7) AS INT)), 0) 
            FROM Metal_Check_Table
            WHERE LEN(M_CheckId) >= 8 AND ISNUMERIC(SUBSTRING(M_CheckId, 8, LEN(M_CheckId) - 7)) = 1";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        object result = command.ExecuteScalar();
                        int maxNumericValue = Convert.ToInt32(result);

                        // Increment the numeric part
                        int numericPart = maxNumericValue + 1;

                        // Format the new value
                        M_CheckId_key = $"M_Check{numericPart:D3}"; // Ensures format like M_Check001, M_Check002
                    }
                }
            }
            catch (Exception ex)
            {
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                // Handle exceptions
                Console.WriteLine("Error generating M_CheckId: " + ex.Message);
                throw;
            }

            return M_CheckId_key;
        }




        private int GetLineNo(string lineName)
        {
            // Implement mapping logic for LineName to LineNo
            switch (lineName)
            {
                case "Maida 1": return 1;
                case "Maida 2": return 2;
                case "Maida 3": return 3;
                case "Maida 4": return 4;
                case "Maida 5": return 5;
                case "Sugar 1": return 6;
                case "Sugar 2": return 7;
                case "Broken Biscuit / Cake Crumb 1": return 8;
                case "Broken Biscuit / Cake Crumb 2": return 9;
                default: return 0;
            }
        }


        private string GetNextCcpId()
        {
            // Logic to generate the next CCP ID
            // Assuming it follows the format 00001, 00002, 00003, etc.
            int nextId = 1; // Fetch the last ID from the database and increment
            return nextId.ToString("D5"); // Format as 00001, 00002, etc.
        }

        protected void RBL_CleanedStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            RadioButtonList rbl = (RadioButtonList)sender;
            GridViewRow row = (GridViewRow)rbl.NamingContainer;
            TextBox txtRemarks = (TextBox)row.FindControl("TXB_CleanedStatusRemarks");

            if (rbl.SelectedValue == "0") // "Not Ok"
            {
                txtRemarks.Style["display"] = "block";
            }
            else
            {
                txtRemarks.Style["display"] = "none";
            }
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
                PlantLinesBinder(selectedPlantValue);

                LoadApprovers(selectedPlantValue, "");

                DDL_PlantLine.Enabled = true;
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
                    cmd.Parameters.AddWithValue("@FormID", 5);
                    cmd.Parameters.AddWithValue("@FormName", "ccp_checklist");

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        hdn_formid.Value = "5";
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
                            dbcl.InsertDefaultApprovers(selectedPlantValue, selectedPlantLineValue, 5);

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
                    cmd.Parameters.AddWithValue("@FormID", 5);
                    cmd.Parameters.AddWithValue("@FormName", "ccp_checklist");

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        hdn_formid.Value = "5";
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
                            bool isInserted = dbcl.InsertDefaultApprovers(selectedPlantValue, selectedPlantLineValue, 5);

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
            //string query = "SELECT SKUId, SKU_name FROM MST_Brand_SKU WHERE brand_id = @SelectedPlantValue";
            //string textField = "SKU_name";
            //string valueField = "SKUId";

            //bool recordsBound;
            //DatabaseHelper.BindDropDownList(query, DDL_BrandSKU, textField, valueField, new SqlParameter("@SelectedPlantValue", selectedProductBrandValue), out recordsBound);

            //if (!recordsBound)
            //{
            //    DatabaseHelper.BindWithDefaultNoRecords(DDL_PlantLine);

            //    string BrandSKUBinder_Error_script = @"<script type='text/javascript'>
            //         new PNotify({
            //             title: 'Error',
            //             text: 'An error occurred!',
            //             type: 'error',
            //             styling: 'bootstrap3'
            //         });
            //     </script>";
            //    ClientScript.RegisterStartupScript(this.GetType(), "ShowBrandSKUBinderErrorNotification", BrandSKUBinder_Error_script, false);
            //}
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
        protected void DDL_Plant_QtyOfMetal_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Handle the event here
            // Example: Update related controls or process the selected value
            string selectedValue = ((DropDownList)sender).SelectedValue;
            // Perform necessary actions based on the selected value
        }

        protected void DDL_Plant_MetalCheck_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        protected void RBL_FF_Status_SelectedIndexChanged(object sender, EventArgs e)
        {
            // TB_FF_Remarks.Visible = RBL_FF_Status.SelectedValue == "Sensing";
        }

        protected void RBL_NFE_Status_SelectedIndexChanged(object sender, EventArgs e)
        {
            //TB_NFE_Remarks.Visible = RBL_NFE_Status.SelectedValue == "Sensing";
        }

        protected void RBL_SS_Status_SelectedIndexChanged(object sender, EventArgs e)
        {
            //TB_SS_Remarks.Visible = RBL_SS_Status.SelectedValue == "Sensing";
        }

        private string Find_DBCode()
        {
            string aa = null;
            dbcl.Sqlconnection();
            dbcl.ConnectDb();
            string kk = null;
            string cmdString1 = "select Id,CcpId from TRN_CCP_Checklist where Id=(select max(Id)from TRN_CCP_Checklist)";
            SqlCommand com1 = new SqlCommand(cmdString1, dbcl.Conn);
            SqlDataReader DR1 = com1.ExecuteReader();
            if (DR1.Read())
            {
                aa = DR1.GetValue(1).ToString();
                string bb = aa.Substring(5);
                int k = Convert.ToInt32(bb);
                k = k + 1;
                string q = Convert.ToString(k);
                kk = "CCP00" + q;
            }
            else
            {
                kk = "CCP001";
            }
            dbcl.DisconnectDb();
            CcpId = kk;
            return kk;
        }

        protected void btnBDSave_Click(object sender, EventArgs e)
        {
            BasicData_Insert();

            ScriptManager.RegisterStartupScript(this, this.GetType(), "SwitchTab", "document.getElementById('Metal_Check-tab').click();", true);
        }

        private void BasicData_Insert()
        {
            // Retrieve values from controls
            string plantName = DDL_Plant.SelectedValue;
            string plantLine = DDL_PlantLine.SelectedValue;
            string productCategory = DDL_ProductCategory.SelectedValue;
            string productBrand = DDL_ProductBrand.SelectedValue; // Assuming DDL_ProductBrand is a DropDownList
            //string brandSKU = DDL_BrandSKU.SelectedValue;
            string brandSKU = string.Empty;
            string shift = hdn_shiftvalue.Value.ToString();
            int formID = Convert.ToInt32(hdn_formid.Value.ToString());


            // Generate a new CcpId
            string ccpId = Find_DBCode();
            string approver1EmployeeCode = Approver1CodeLabel.Text.ToString();
            string approver2EmployeeCode = Approver2CodeLabel.Text.ToString();
            string dottedLineApproverEmployeeCode = DottedLineApproverCodeLabel.Text.ToString();

            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            // Insert query for TRN_CCP_Checklist table
            string insertQuery = @"
                INSERT INTO [dbo].[TRN_CCP_Checklist] 
                (CcpId, FormID, PlantId, Line, ProductCategory, ProductBrand, SKUID, SubmittedById, SubmittedByEmployeeCode, 
                SubmittedDate, SubmittedTime, Shift, ViewMode, DeleteMode, Approver1EmployeeCode, Approver1_Status, 
                Approver1_TimeStamp, Approver2EmployeeCode, Approver2_Status, Approver2_TimeStamp, 
                DottedLineApproverEmployeeCode, DottedApprover_Status, DottedApprover_TimeStamp,T1_Status) 
                VALUES 
                (@CcpId, @FormID, @PlantId, @Line, @ProductCategory, @ProductBrand, @SKUID, @SubmittedById, @SubmittedByEmployeeCode, 
                @SubmittedDate, @SubmittedTime, @Shift, @ViewMode, @DeleteMode, @Approver1EmployeeCode, @Approver1_Status, 
                @Approver1_TimeStamp, @Approver2EmployeeCode, @Approver2_Status, @Approver2_TimeStamp, 
                @DottedLineApproverEmployeeCode, @DottedApprover_Status, @DottedApprover_TimeStamp, @T1_Status)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                // Create a SqlCommand object
                using (SqlCommand command = new SqlCommand(insertQuery, connection))
                {
                    // Add parameters to the command
                    command.Parameters.AddWithValue("@CcpId", ccpId); // Example: "CCP12345"
                    command.Parameters.AddWithValue("@FormID", formID);
                    command.Parameters.AddWithValue("@PlantId", plantName); // Example: "PLANT001"
                    command.Parameters.AddWithValue("@Line", plantLine); // Example: "Line1"
                    command.Parameters.AddWithValue("@ProductCategory", productCategory); // Example: "Electronics"
                    command.Parameters.AddWithValue("@ProductBrand", productBrand); // Example: "BrandX"
                    command.Parameters.AddWithValue("@SKUID", brandSKU); // Example: "SKU001"
                    command.Parameters.AddWithValue("@SubmittedById", Convert.ToInt32(Session["USERID"].ToString())); // Example: 1
                    command.Parameters.AddWithValue("@SubmittedByEmployeeCode", Session["WORKMAN"].ToString()); // Example: "EMP123"
                    command.Parameters.AddWithValue("@SubmittedDate", DateTime.Now.Date); // Current date
                    command.Parameters.AddWithValue("@SubmittedTime", DateTime.Now.TimeOfDay); // Current time
                    command.Parameters.AddWithValue("@Shift", shift); // Example: "ShiftA"
                    command.Parameters.AddWithValue("@ViewMode", 1); // Default value 1
                    command.Parameters.AddWithValue("@DeleteMode", 0); // Default value 0
                    command.Parameters.AddWithValue("@Approver1EmployeeCode", approver1EmployeeCode); // NULL value
                    command.Parameters.AddWithValue("@Approver1_Status", 0); // Default value 0
                    command.Parameters.AddWithValue("@Approver1_TimeStamp", DBNull.Value); // NULL value
                    command.Parameters.AddWithValue("@Approver2EmployeeCode", approver2EmployeeCode); // NULL value
                    command.Parameters.AddWithValue("@Approver2_Status", 0); // Default value 0
                    command.Parameters.AddWithValue("@Approver2_TimeStamp", DBNull.Value); // NULL value
                    command.Parameters.AddWithValue("@DottedLineApproverEmployeeCode", dottedLineApproverEmployeeCode); // NULL value
                    command.Parameters.AddWithValue("@DottedApprover_Status", 0); // Default value 0
                    command.Parameters.AddWithValue("@DottedApprover_TimeStamp", DBNull.Value); // NULL value
                    command.Parameters.AddWithValue("@T1_Status", 1); // NULL value

                    try
                    {
                        // Open the connection
                        connection.Open();

                        // Execute the insert command
                        int rowsAffected = command.ExecuteNonQuery();

                        DDL_Plant.Enabled = false;
                        DDL_PlantLine.Enabled = false;
                        DDL_ProductCategory.Enabled = false;
                        DDL_ProductBrand.Enabled = false;
                        //DDL_BrandSKU.Enabled = false;

                        btnBDSave.Enabled = false;
                        btnBDSave.Text = "SAVED";

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
                        var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                        EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                        string errorMessage = ex.Message.Replace("'", "\\'"); // Escape single quotes in the error message
                        string BasicdataerrorScript = "<script type='text/javascript'>\n" +
                                             $"new PNotify({{\n" +
                                             "    title: 'Error',\n" +
                                             $"    text: '{errorMessage}',\n" +
                                             "    type: 'error',\n" +
                                             "    styling: 'bootstrap3'\n" +
                                             "}});\n" +
                                             "</script>";
                        ClientScript.RegisterStartupScript(this.GetType(), "ShowErrorNotification", BasicdataerrorScript, false);
                    }
                }
            }
        }

        private string GenerateUniqueCcpId()
        {
            string newCcpId = String.Empty;
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Fetch the maximum CcpId value
                    string query = "SELECT ISNULL(MAX(CAST(CcpId AS INT)), 0) FROM TRN_CCP_Checklist";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        object result = command.ExecuteScalar();
                        int maxCcpId = Convert.ToInt32(result);

                        // Increment the numeric part
                        int numericPart = maxCcpId + 1;

                        // Format the new value
                        newCcpId = $"{numericPart:D5}"; // Ensure five digits (e.g., 00001, 00002)
                    }
                }
            }
            catch (Exception ex)
            {
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                string CCID_errorMessage = ex.Message.Replace("'", "\\'"); // Escape single quotes in the error message
                string CCID_errorScript = "<script type='text/javascript'>\n" +
                                     $"new PNotify({{\n" +
                                     "    title: 'Error',\n" +
                                     $"    text: '{CCID_errorMessage}',\n" +
                                     "    type: 'error',\n" +
                                     "    styling: 'bootstrap3'\n" +
                                     "}});\n" +
                                     "</script>";
                ClientScript.RegisterStartupScript(this.GetType(), "CCID_ShowErrorNotification", CCID_errorScript, false);
            }

            return newCcpId;
        }

        private void MakeInputsReadOnly()
        {
            // Disable all input controls
            DDL_Plant.Enabled = false;
            DDL_PlantLine.Enabled = false;
            DDL_ProductCategory.Enabled = false;
            DDL_ProductBrand.Enabled = false;


            btnBDSave.Enabled = false;
            btnBDSave.Text = "SAVED";
            btnBDSave.CssClass = "btn btn-sm btn-success";

            string successScript = @"<script type='text/javascript'>
                    new PNotify({
                        title: 'Data Success',
                        text: 'Recorded Successfully!!',
                        type: 'success',
                        styling: 'bootstrap3'
                    });
                </script>";

            // Register the JavaScript code to show success notification
            ClientScript.RegisterStartupScript(this.GetType(), "ShowDataSuccessNotification", successScript, false);
        }

        protected void btnBDReset_Click(object sender, EventArgs e)
        {
            Response.Redirect("ccp_checklist.aspx");
        }

        protected void btn_svSieve_Click(object sender, EventArgs e)
        {
            SaveSieveGridViewDataToJson();

            DDL_PlantLine.Enabled = true;

        }

        protected void SaveSieveGridViewDataToJson()
        {
            // List to hold the row data
            var gridViewData = new List<Dictionary<string, object>>();

            // Loop through the rows of the GridView
            foreach (GridViewRow row in GridView_Shieve.Rows)
            {
                // Only process data rows (skip header row)
                if (row.RowType == DataControlRowType.DataRow)
                {
                    //var rowData = new Dictionary<string, object>();

                    //// Extract data from each control in the row
                    //rowData["Sl"] = (row.RowIndex + 1).ToString();
                    //rowData["SN"] = ((Label)row.FindControl("lbl_SieveNo")).Text; // Assuming you have a Label for Sieve No
                    //rowData["ISV"] = ((TextBox)row.FindControl("TB_InitialSample")).Text;
                    //rowData["FRV"] = ((TextBox)row.FindControl("TB_FinalRetention")).Text;
                    //rowData["PER"] = ((Label)row.FindControl("lbl_PercentageRetention")).Text;

                    //// Add the row data to the list
                    //gridViewData.Add(rowData);


                    var rowData = new Dictionary<string, object>();

                    // Extract data from each control in the row
                    rowData["Sl"] = (row.RowIndex + 1).ToString();
                    rowData["SN"] = ((Label)row.FindControl("lbl_SieveNo")).Text;  // Sieve No
                    string initialSampleStr = ((TextBox)row.FindControl("TB_InitialSample")).Text;
                    string finalRetentionStr = ((TextBox)row.FindControl("TB_FinalRetention")).Text;

                    // Declare the variables before using them with TryParse
                    double initialSample;
                    double finalRetention;

                    // Parse the values to numbers
                    bool isInitialSampleParsed = double.TryParse(initialSampleStr, out initialSample);
                    bool isFinalRetentionParsed = double.TryParse(finalRetentionStr, out finalRetention);

                    // If parsing fails, the value will default to 0
                    rowData["ISV"] = isInitialSampleParsed ? initialSample.ToString() : "0";
                    rowData["FRV"] = isFinalRetentionParsed ? finalRetention.ToString() : "0";

                    // Calculate the percentage if ISV is not 0
                    double percentage = initialSample != 0 ? (initialSample / finalRetention) * 100 : 0;
                    rowData["PER"] = percentage.ToString("0.##");  // Format to two decimal places

                    // Add the row data to the list
                    gridViewData.Add(rowData);
                }
            }

            // Serialize the list to JSON
            string jsonData = JsonConvert.SerializeObject(gridViewData);

            // Save the JSON data to your database
            SaveSieveToDatabase(jsonData);
        }

        protected void BindSieveJsonDataToGridView(string jsonData)
        {
            try
            {
                // Step 2: Deserialize the JSON data into a list of dictionaries
                var dataList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonData);

                if (dataList == null || !dataList.Any())
                {
                    lbl_sivecheckmsg.Text = "No data found in JSON.";
                    return;
                }

                // Step 3: Create a DataTable with column names matching the GridView bindings
                DataTable dt = new DataTable();
                dt.Columns.Add("SieveNo", typeof(string));           // Column name should be "SieveNo"
                dt.Columns.Add("InitialSample", typeof(string));     // Column name should be "InitialSample"
                dt.Columns.Add("FinalRetention", typeof(string));    // Column name should be "FinalRetention"
                dt.Columns.Add("PercentageRetention", typeof(string)); // Column name should be "PercentageRetention"

                // Step 4: Populate DataTable from deserialized JSON data
                foreach (var rowData in dataList)
                {
                    //DataRow dr = dt.NewRow();
                    //dr["SieveNo"] = rowData.ContainsKey("SN") ? rowData["SN"]?.ToString() : string.Empty;
                    //dr["InitialSample"] = rowData.ContainsKey("ISV") ? rowData["ISV"]?.ToString() : string.Empty;
                    //dr["FinalRetention"] = rowData.ContainsKey("FRV") ? rowData["FRV"]?.ToString() : string.Empty;
                    //dr["PercentageRetention"] = rowData.ContainsKey("PER") ? rowData["PER"]?.ToString() : string.Empty;

                    //dt.Rows.Add(dr);

                    DataRow dr = dt.NewRow();
                    dr["SieveNo"] = rowData.ContainsKey("SN") ? rowData["SN"]?.ToString() : string.Empty;

                    // Parse InitialSample and FinalRetention values
                    double initialSample = rowData.ContainsKey("ISV") ? Convert.ToDouble(rowData["ISV"]) : 0;
                    double finalRetention = rowData.ContainsKey("FRV") ? Convert.ToDouble(rowData["FRV"]) : 0;

                    dr["InitialSample"] = initialSample.ToString();
                    dr["FinalRetention"] = finalRetention.ToString();

                    // Calculate PercentageRetention
                    double percentageRetention = initialSample > 0 ? (initialSample / finalRetention) * 100 : 0;
                    dr["PercentageRetention"] = percentageRetention.ToString("F2"); // Format to 2 decimal places

                    dt.Rows.Add(dr);
                }

                // Step 5: Bind DataTable to GridView2 if there is data
                if (dt.Rows.Count > 0)
                {
                    GridView2.Visible = true;
                    GridView2.DataSource = dt;
                    GridView2.DataBind();

                    GridView_Shieve.Visible = false;
                    btn_svSieve.Enabled = false;
                    btn_svSieve.Text = "SAVED";
                    lbl_sivecheckmsg.Text = "Data Saved Successfully";

                    // Optionally switch to the next tab after saving
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "SwitchTab", "document.getElementById('Metal_Dctector_Area-tab').click();", true);
                }
                else
                {
                    lbl_sivecheckmsg.Text = "No data to display.";
                }
            }
            catch (Exception ex)
            {
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                lbl_sivecheckmsg.Text = $"Error binding data to GridView: { ex.Message}";
                // Log or handle exceptions
                //Console.WriteLine($"Error binding data to GridView: {ex.Message}");
            }
        }

        private void SaveSieveToDatabase(string jsonData)
        {
            try
            {
                if (CcpId != "" && CcpId != string.Empty)
                {
                    string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                    string insertQuery = "UPDATE TRN_CCP_Checklist set SieveCheck=@SieveCheck, T3_Status=@T3_Status where CcpId=@CcpId ";

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    using (SqlCommand command = new SqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@SieveCheck", jsonData);
                        command.Parameters.AddWithValue("@T3_Status", 1);
                        command.Parameters.AddWithValue("@CcpId", CcpId);
                        connection.Open();
                        command.ExecuteNonQuery();

                        BindSieveJsonDataToGridView(jsonData);
                    }
                }
                else
                {
                    string SieveerrorMessage = "No Basic Data Found";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "SwitchTab", "document.getElementById('basicData-tab').click();", true);

                    lbl_MagnetCheck.Text = SieveerrorMessage;
                    string SieveerrorScript = $"new PNotify({{ title: 'Error', text: '{SieveerrorMessage}', type: 'error', styling: 'bootstrap3' }});";
                    ClientScript.RegisterStartupScript(this.GetType(), "ShowSieveErrorNotification", SieveerrorScript, true);

                }
            }
            catch (Exception ex)
            {
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                string SieveJSONerrorMessage = ex.Message.Replace("'", "\\'"); // Escape single quotes in the error message
                lbl_MagnetCheck.Text = SieveJSONerrorMessage;
                string SieveJSONInserterrorScript = "<script type='text/javascript'>\n" +
                                     $"new PNotify({{\n" +
                                     "    title: 'Error',\n" +
                                     $"    text: '{SieveJSONerrorMessage}',\n" +
                                     "    type: 'error',\n" +
                                     "    styling: 'bootstrap3'\n" +
                                     "}});\n" +
                                     "</script>";
                ClientScript.RegisterStartupScript(this.GetType(), "SieveJSONInsertErrorNotification", SieveJSONInserterrorScript, false);
            }
        }

        protected void btn_finalsbmt_Click(object sender, EventArgs e)
        {
            //OldDataInsert();
            //SaveMetalDetectorDataToJson();
            NewDatInsert();
            DisableAllFormFields();


            btn_finalsbmt.Enabled = false;
            btn_finalsbmt.Text = "SAVED";
            btn_rst_metaldet.Enabled = false;
            btn_rst_metaldet.Text="NOT ALLOWED";


        }

        private void OldDataInsert()
        {
            if (CcpId != "" && CcpId != string.Empty)
            {
                if (btnSubmit.Text == "SAVED" && btn_svSieve.Text == "SAVED")
                {
                    int FF_Status = Convert.ToInt32(RBL_FF_Status.SelectedValue);
                    string FF_Remarks = TB_FF_Remarks.Text.ToString();
                    int NFE_Status = Convert.ToInt32(RBL_NFE_Status.SelectedValue);
                    string NFE_Remarks = TB_NFE_Remarks.Text.ToString();
                    int SS_Status = Convert.ToInt32(RBL_SS_Status.SelectedValue);
                    string SS_Remarks = TB_SS_Remarks.Text.ToString();
                    string MD_Remarks = TB_MDRemarks.Text.ToString();

                    try
                    {
                        string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
                        string insertQuery = "UPDATE TRN_CCP_Checklist set FF_Status=@FF_Status, FF_Remarks=@FF_Remarks, NFE_Status=@NFE_Status, NFE_Remarks=@NFE_Remarks, SS_Status=@SS_Status, SS_Remarks=@SS_Remarks, MD_Remarks=@MD_Remarks, T4_Status=@T4_Status, Final_Status=@Final_Status where CcpId=@CcpId";

                        using (SqlConnection connection = new SqlConnection(connectionString))
                        using (SqlCommand command = new SqlCommand(insertQuery, connection))
                        {
                            command.Parameters.AddWithValue("@FF_Status", FF_Status);
                            command.Parameters.AddWithValue("@FF_Remarks", FF_Remarks);
                            command.Parameters.AddWithValue("@NFE_Status", NFE_Status);
                            command.Parameters.AddWithValue("@NFE_Remarks", NFE_Remarks);
                            command.Parameters.AddWithValue("@SS_Status", SS_Status);
                            command.Parameters.AddWithValue("@SS_Remarks", SS_Remarks);
                            command.Parameters.AddWithValue("@MD_Remarks", MD_Remarks);
                            command.Parameters.AddWithValue("@T4_Status", 1);
                            command.Parameters.AddWithValue("@Final_Status", 1);
                            command.Parameters.AddWithValue("@CcpId", CcpId);
                            connection.Open();
                            command.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                        EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                        string errorMessage = ex.Message;
                        string errorScript = $"new PNotify({{ title: 'Error', text: '{errorMessage}', type: 'error', styling: 'bootstrap3' }});";
                        throw;
                    }
                }
                else
                {
                    lbl_mtldetmsg.Text = "Sieve Check / Metal Detector Pending";
                }
            }
            else
            {
                string MagnetD_errorMessage = "No Basic Data Found";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "SwitchTab", "document.getElementById('basicData-tab').click();", true);

                lbl_mtldetmsg.Text = MagnetD_errorMessage;
                string MagnetD_errorScript = $"new PNotify({{ title: 'Error', text: '{MagnetD_errorMessage}', type: 'error', styling: 'bootstrap3' }});";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowMDErrorNotification", MagnetD_errorScript, true);
            }
        }


        private void InitializeGridViewDataTable()
        {
            if (ViewState["GridViewData"] == null)
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("PlantLine", typeof(string));
                dt.Columns.Add("FF_Status", typeof(string));
                dt.Columns.Add("FF_Remarks", typeof(string));
                dt.Columns.Add("NFE_Status", typeof(string));
                dt.Columns.Add("NFE_Remarks", typeof(string));
                dt.Columns.Add("SS_Status", typeof(string));
                dt.Columns.Add("SS_Remarks", typeof(string));
                //dt.Columns.Add("MD_Remarks", typeof(string));

                ViewState["GridViewData"] = dt;
            }
        }


        private void AddRowToGridView()
        {
            // Validation for required fields
            if (DDL_PlantLine.SelectedIndex == 0 || RBL_FF_Status.SelectedItem == null || RBL_NFE_Status.SelectedItem == null || RBL_SS_Status.SelectedItem == null)
            {
                ScriptManager.RegisterStartupScript(this, this.GetType(), "ValidationError", "alert('Please fill all required fields.');", true);
                ScriptManager.RegisterStartupScript(this, this.GetType(), "SwitchTab", "document.getElementById('Metal_Dctector_Area-tab').click();", true);
                return;
            }

            // Ensure the DataTable is initialized
            InitializeGridViewDataTable();

            // Fetch the ViewState DataTable
            DataTable dt = ViewState["GridViewData"] as DataTable;

            // Read form values
            string plantLine = DDL_PlantLine.SelectedItem.Text;
            string ffStatus = RBL_FF_Status.SelectedItem != null ? RBL_FF_Status.SelectedItem.Text : "";
            string ffRemarks = TB_FF_Remarks.Text.Trim();
            string nfeStatus = RBL_NFE_Status.SelectedItem != null ? RBL_NFE_Status.SelectedItem.Text : "";
            string nfeRemarks = TB_NFE_Remarks.Text.Trim();
            string ssStatus = RBL_SS_Status.SelectedItem != null ? RBL_SS_Status.SelectedItem.Text : "";
            string ssRemarks = TB_SS_Remarks.Text.Trim();
            //string mdRemarks = TB_MDRemarks.Text.Trim();

            // Check for duplicate entries
            foreach (DataRow row in dt.Rows)
            {
                if (row["PlantLine"].ToString() == plantLine)
                {
                    // Show a message to the user and exit
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "DuplicateEntry", "alert('This PlantLine is already added.');", true);
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "SwitchTab", "document.getElementById('Metal_Dctector_Area-tab').click();", true);
                    return;
                }
            }

            // Add a new row to the DataTable
            DataRow newRow = dt.NewRow();
            newRow["PlantLine"] = plantLine;
            newRow["FF_Status"] = ffStatus;
            newRow["FF_Remarks"] = ffRemarks;
            newRow["NFE_Status"] = nfeStatus;
            newRow["NFE_Remarks"] = nfeRemarks;
            newRow["SS_Status"] = ssStatus;
            newRow["SS_Remarks"] = ssRemarks;
            //newRow["MD_Remarks"] = mdRemarks;

            dt.Rows.Add(newRow);

            // Save the updated DataTable back to ViewState
            ViewState["GridViewData"] = dt;

            // Rebind the GridView
            Magnetgrid.DataSource = dt;
            Magnetgrid.DataBind();

            ScriptManager.RegisterStartupScript(this, this.GetType(), "SwitchTab", "document.getElementById('Metal_Dctector_Area-tab').click();", true);
        }


        private void ClearFormInputs()
        {
            DDL_PlantLine.SelectedIndex = 0;
            RBL_FF_Status.ClearSelection();
            TB_FF_Remarks.Text = string.Empty;
            RBL_NFE_Status.ClearSelection();
            TB_NFE_Remarks.Text = string.Empty;
            RBL_SS_Status.ClearSelection();
            TB_SS_Remarks.Text = string.Empty;
            //TB_MDRemarks.Text = string.Empty;
        }

        private void EnableAllFormFields()
        {
            DDL_PlantLine.Enabled = true;
            RBL_FF_Status.Enabled = true;
            RBL_NFE_Status.Enabled = true;
            RBL_SS_Status.Enabled = true;
            TB_FF_Remarks.Enabled = true;
            TB_NFE_Remarks.Enabled = true;
            TB_SS_Remarks.Enabled = true;
            TB_MDRemarks.Enabled = true;
        }


        private void DisableAllFormFields()
        {
            // Disable dropdown lists
            DDL_PlantLine.Enabled = false;

            // Disable radio button lists
            RBL_FF_Status.Enabled = false;
            RBL_NFE_Status.Enabled = false;
            RBL_SS_Status.Enabled = false;

            // Disable textboxes
            TB_FF_Remarks.Enabled = false;
            TB_NFE_Remarks.Enabled = false;
            TB_SS_Remarks.Enabled = false;
            TB_MDRemarks.Enabled = false;
        }


        protected void btn_resetgrid_Click(object sender, EventArgs e)
        {
            ClearFormInputs();
        }

        protected void btn_addtolist_Click(object sender, EventArgs e)
        {
            AddRowToGridView();
            ClearFormInputs();
        }

        protected void Magnetgrid_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            // Retrieve the DataTable from ViewState
            DataTable dt = ViewState["GridViewData"] as DataTable;

            if (dt != null)
            {
                // Remove the row at the specified index
                dt.Rows.RemoveAt(e.RowIndex);

                // Save the updated DataTable back to ViewState
                ViewState["GridViewData"] = dt;

                // Rebind the GridView
                Magnetgrid.DataSource = dt;
                Magnetgrid.DataBind();

                ScriptManager.RegisterStartupScript(this, this.GetType(), "SwitchTab", "document.getElementById('Metal_Dctector_Area-tab').click();", true);
            }
        }



        private void BindGridView()
        {
            if (ViewState["GridViewData"] != null)
            {
                DataTable dt = (DataTable)ViewState["GridViewData"];

                if ((dt != null) && (dt.Rows.Count > 0))
                {
                    Magnetgrid.Visible = true;
                    Magnetgrid.DataSource = dt;
                    Magnetgrid.DataBind();
                }
                else
                {
                    Magnetgrid.Visible = false;
                }
            }
        }

        protected void btn_rst_metaldet_Click(object sender, EventArgs e)
        {
            //here update the record and set visible false commenting that user left the submission incomplete
        }

        private string ConvertGridViewToJson()
        {
            DataTable dt = new DataTable();

            // Add columns to the DataTable
            foreach (TableCell headerCell in Magnetgrid.HeaderRow.Cells)
            {
                dt.Columns.Add(headerCell.Text);
            }

            // Add rows to the DataTable
            foreach (GridViewRow row in Magnetgrid.Rows)
            {
                DataRow dr = dt.NewRow();
                for (int i = 0; i < row.Cells.Count; i++)
                {
                    dr[i] = row.Cells[i].Text.Trim();
                }
                dt.Rows.Add(dr);
            }

            // Convert the DataTable to JSON
            //return JsonConvert.SerializeObject(dt, Formatting.Indented);
            return JsonConvert.SerializeObject(dt);
        }


        private void NewDatInsert()
        {
            if (CcpId != "" && CcpId != string.Empty)
            {
                if (btnSubmit.Text == "SAVED" && btn_svSieve.Text == "SAVED")
                {
                    string MD_Remarks = TB_MDRemarks.Text.ToString();

                    try
                    {
                        string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

                        // Convert GridView data to JSON
                        //string gridViewJson = ConvertGridViewToJson();
                        string gridViewJson = SaveMetalDetectorDataToJson();

                        // Save JSON to the database
                        string query = @"UPDATE TRN_CCP_Checklist SET MetalDataJson1 = @MetalDataJson1, MetalDataJson1_Time = @MetalDataJson1_Time, T4_Status = @T4_Status, Final_Status = @Final_Status, MD_Remarks = @MD_Remarks WHERE CcpId = @CcpId";

                        using (SqlConnection connection = new SqlConnection(connectionString))
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@MetalDataJson1", gridViewJson);
                            command.Parameters.AddWithValue("@MetalDataJson1_Time", DateTime.Now);
                            command.Parameters.AddWithValue("@T4_Status", 1);
                            command.Parameters.AddWithValue("@Final_Status", 1);
                            command.Parameters.AddWithValue("@MD_Remarks", MD_Remarks);
                            command.Parameters.AddWithValue("@CcpId", CcpId);

                            connection.Open();
                            command.ExecuteNonQuery();
                        }

                        // Notify success
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "UpdateSuccess", "new PNotify({ title: 'Success', text: 'Grid data saved successfully.', type: 'success', styling: 'bootstrap3' });", true);
                    }
                    catch (Exception ex)
                    {
                        var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                        EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                        string errorMessage = ex.Message;
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "Error", "new PNotify({{ title: 'Error', text: '{errorMessage}', type: 'error', styling: 'bootstrap3' }});", true);
                    }
                }
                else
                {
                    lbl_mtldetmsg.Text = "Sieve Check / Metal Detector Pending";
                }
            }
            else
            {
                string errorMessage = "No Basic Data Found";
                ScriptManager.RegisterStartupScript(this, this.GetType(), "SwitchTab", "document.getElementById('basicData-tab').click();", true);
                lbl_mtldetmsg.Text = errorMessage;
                ScriptManager.RegisterStartupScript(this, this.GetType(), "Error", $"new PNotify({{ title: 'Error', text: '{errorMessage}', type: 'error', styling: 'bootstrap3' }});", true);
            }
        }

        //protected void SaveMetalDetectorDataToJson()
        //{
        //    // List to hold the row data
        //    var gridViewData = new List<Dictionary<string, object>>();

        //    // Loop through the rows of the GridView
        //    foreach (GridViewRow row in Magnetgrid.Rows)
        //    {
        //        // Only process data rows (skip header row)
        //        if (row.RowType == DataControlRowType.DataRow)
        //        {
        //            var rowData = new Dictionary<string, object>();

        //            // Extract data from each control in the row
        //            var lblPlantLine = (Label)row.FindControl("lbl_PlantLine");
        //            var lblFF = (Label)row.FindControl("lbl_FF_Status");
        //            var tbFFR = (TextBox)row.FindControl("TB_FF_Remarks");
        //            var lblNFE = (Label)row.FindControl("lbl_NFE_Status");
        //            var tbNFER = (TextBox)row.FindControl("TB_NFE_Remarks");
        //            var lblSS = (Label)row.FindControl("lbl_SS_Status");
        //            var tbSSR = (TextBox)row.FindControl("TB_SS_Remarks");

        //            // Debugging: Check if controls exist
        //            if (lblPlantLine == null || lblFF == null || tbFFR == null || lblNFE == null || tbNFER == null || lblSS == null || tbSSR == null)
        //            {
        //                System.Diagnostics.Debug.WriteLine($"Control not found in row {row.RowIndex}");
        //            }

        //            // Add data to the dictionary (use null-coalescing operator to handle null values)
        //            rowData["Line"] = lblPlantLine?.Text?.Trim() ?? "No Data";
        //            rowData["FF"] = lblFF?.Text?.Trim() ?? "No Data";
        //            rowData["FFR"] = tbFFR?.Text?.Trim() ?? "No Data";
        //            rowData["NFE"] = lblNFE?.Text?.Trim() ?? "No Data";
        //            rowData["NFER"] = tbNFER?.Text?.Trim() ?? "No Data";
        //            rowData["SS"] = lblSS?.Text?.Trim() ?? "No Data";
        //            rowData["SSR"] = tbSSR?.Text?.Trim() ?? "No Data";

        //            // Add the row data to the list
        //            gridViewData.Add(rowData);
        //        }
        //    }

        //    // Serialize the list to JSON
        //    string jsonData = JsonConvert.SerializeObject(gridViewData, Formatting.Indented);

        //}


        private string SaveMetalDetectorDataToJson()
        {
            if (ViewState["GridViewData"] != null)
            {
                DataTable dt = (DataTable)ViewState["GridViewData"];

                if (dt != null && dt.Rows.Count > 0)
                {
                    var gridViewData = new List<Dictionary<string, object>>();

                    foreach (DataRow row in dt.Rows)
                    {
                        var rowData = new Dictionary<string, object>();

                        foreach (DataColumn column in dt.Columns)
                        {
                            rowData[column.ColumnName] = row[column] != DBNull.Value ? row[column].ToString() : null;
                        }

                        gridViewData.Add(rowData);
                    }

                    // Serialize to JSON
                    //return JsonConvert.SerializeObject(gridViewData, Formatting.Indented);
                    return JsonConvert.SerializeObject(gridViewData);
                }
            }

            // Return an empty JSON array if no data is found
            return "[]";
        }


    }
}