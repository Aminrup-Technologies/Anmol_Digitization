using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;
using System.Drawing;
using static AnmolDristi.qaqc_rotaryline_detailview;

namespace AnmolDristi
{
    public partial class qaqc_rotaryline_detailview : System.Web.UI.Page
    {
        public static Int32 RecordID = 0;
        public static Int32 ViewerMode = 0;

        public static string PlantId = string.Empty;
        public static string PlantName = string.Empty;
        public static string PlantLine = string.Empty;
        public static string ProductCategory = string.Empty;
        public static string CategoryBrand = string.Empty;

        public static string JSON1 = string.Empty;
        public static string JSON2 = string.Empty;

        public static string App1_Status = string.Empty;
        public static string App2_Status = string.Empty;
        public static string DottedApp_Status = string.Empty;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.QueryString["ID"] != null)
            {
                lbl_docname.Text = "Roatary Line & Oven End Report";
                lbl_docnumber.Text = "ANMOL/DOC/CORQ/QA/02";

                RecordID = Convert.ToInt32(Request.QueryString["ID"]);
                ViewerMode = Convert.ToInt32(Request.QueryString["VM"]);

                PlantBinder();
                DataBinder(RecordID);
            }
        }

        private void DataBinder(int rlwt)
        {
            getBasicDetails(rlwt);
            BindGridView_LineWeights();
            BindGridView_OvenEnd();
        }
        private void PlantBinder()
        {
            string query = "SELECT plant_id, CONCAT(plant_name, '[', sap_code, ']') AS plant_name FROM MST_PlantDetails";
            string textField = "plant_name";
            string valueField = "plant_id";

            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_Plant, textField, valueField, out recordsBound);

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
            string query = "SELECT category_id, category_name FROM MST_LineCategory WHERE plant_id = @PlantId AND line_id = @LineId";
            string textField = "category_name";
            string valueField = "category_id";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@PlantId", selectedPlantValue),
                new SqlParameter("@LineId", selectedPlantLineValue)
            };

            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_ProductCategory, textField, valueField, parameters, out recordsBound);

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


        //Basic Data
        void getBasicDetails(int rlwt)
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
                            P.linewt,
                            P.gaugeandweight,
                            P.Approver1EmployeeCode,
							U.EmployeeName as Approver1Name,
                            P.Approver1_Status,
                            P.Approver1_TimeStamp,
                            P.Approver2EmployeeCode,
							U.EmployeeName as Approver2Name,
                            P.Approver2_Status,
                            P.Approver2_TimeStamp,
							U.EmployeeName as DottedLineApproverName,
                            P.DottedLineApproverEmployeeCode,
                            P.DottedApprover_Status,
                            P.DottedApprover_TimeStamp,
                            P.*
                        FROM
                            TRN_RotaryLine_OvenEnd P
                        JOIN dbo.MST_PlantDetails A ON P.PlantName = A.plant_id
                        JOIN dbo.MST_Plant_Lines B ON P.Line = B.line_id
                        JOIN dbo.MST_LineCategory C ON P.ProductCategory = C.category_id
                        JOIN dbo.MST_LineCatBrands D ON P.ProductBrand = D.brand_id
                        JOIN dbo.MST_UserMaster U ON P.Approver1EmployeeCode = U.EmployeeCode and P.Approver2EmployeeCode = U.EmployeeCode
                        WHERE P.Id = @RLWt";

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@RLWt", rlwt);

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

                                PlantLinesBinder(plantId);
                                string plantLine = row["Line"].ToString();
                                DDL_PlantLine.SelectedValue = plantLine;
                                DDL_PlantLine.Enabled = false;

                                LineProductsBinder(plantId, plantLine);
                                string productCategory = row["ProductCategory"].ToString();
                                DDL_ProductCategory.SelectedValue = productCategory;
                                DDL_ProductCategory.Enabled = false;

                                ProductBrandsBinder(plantId, plantLine, productCategory);
                                string categoryBrand = row["ProductBrand"].ToString();
                                DDL_ProductBrand.SelectedValue = categoryBrand;
                                DDL_ProductBrand.Enabled = false;

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

                                JSON1 = row["linewt"].ToString();
                                JSON2 = row["gaugeandweight"].ToString();
                            }
                            else
                            {
                                Response.Write("<script>alert('No data found for the given RLWt.');</script>");
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

        public class WeightData
        {
            public int sl { get; set; }
            public decimal weight { get; set; }
        }


        private void BindGridView_LineWeights()
        {
            string jsonData = JSON1;

            if (!string.IsNullOrEmpty(jsonData))
            {
                try
                {
                    List<WeightData> weightDataList = JsonConvert.DeserializeObject<List<WeightData>>(jsonData);

                    decimal minValue = weightDataList.Min(w => w.weight);
                    decimal maxValue = weightDataList.Max(w => w.weight);
                    decimal avgValue = weightDataList.Average(w => w.weight);
                    decimal diffMinMax = maxValue - minValue;

                    lblMinValue.Text = minValue.ToString("F2");
                    lblMaxValue.Text = maxValue.ToString("F2");
                    lblDiffMinMax.Text = diffMinMax.ToString("F2");
                    lblAvgWeight.Text = avgValue.ToString("F2");

                    DataTable dataTable = new DataTable();
                    dataTable.Columns.Add("sl", typeof(int));
                    dataTable.Columns.Add("weight", typeof(decimal));

                    foreach (var item in weightDataList)
                    {
                        DataRow row = dataTable.NewRow();
                        row["sl"] = item.sl;
                        row["weight"] = item.weight;
                        dataTable.Rows.Add(row);
                    }

                    LineWeights_Grid.DataSource = dataTable;
                    LineWeights_Grid.DataBind();
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Error deserializing JSON: {ex.Message}");
                }
            }
            else
            {
                LineWeights_Grid.DataSource = null;
                LineWeights_Grid.DataBind();

                lblMinValue.Text = string.Empty;
                lblMaxValue.Text = string.Empty;
                lblDiffMinMax.Text = string.Empty;
                lblAvgWeight.Text = string.Empty;

                Console.WriteLine("No RLWt value provided.");
            }
        }

        private string GetLineWeightsFromDB(string rlwt)
        {
            if (string.IsNullOrEmpty(rlwt))
            {
                throw new ArgumentException("The RLWt parameter cannot be null or empty.");
            }

            string jsonData = string.Empty;
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            string query = @" SELECT linewt FROM TRN_RotaryLine_OvenEnd WHERE Id = @RLWt";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@RLWt", rlwt);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            jsonData = reader["linewt"] != DBNull.Value ? reader["linewt"].ToString() : string.Empty;
                        }
                    }
                }
            }
            return jsonData;
        }

        public class OvenEndData
        {
            public int sl { get; set; }
            public decimal ge { get; set; }
            public decimal wt { get; set; }
        }

        private void BindGridView_OvenEnd()
        {
            string jsonData = JSON2;

            if (!string.IsNullOrEmpty(jsonData))
            {
                try
                {
                    List<OvenEndData> ovenEndDataList = JsonConvert.DeserializeObject<List<OvenEndData>>(jsonData);

                    decimal minGauge = ovenEndDataList.Min(d => d.ge);
                    decimal maxGauge = ovenEndDataList.Max(d => d.ge);
                    decimal avgGauge = ovenEndDataList.Average(d => d.ge);
                    decimal diffGauge = maxGauge - minGauge;

                    decimal minWeight = ovenEndDataList.Min(d => d.wt);
                    decimal maxWeight = ovenEndDataList.Max(d => d.wt);
                    decimal avgWeight = ovenEndDataList.Average(d => d.wt);
                    decimal diffWeight = maxWeight - minWeight;

                    ov_lblMinGauge.Text = minGauge.ToString("F2");
                    ov_lblMaxGauge.Text = maxGauge.ToString("F2");
                    ov_lblDiffGauge.Text = diffGauge.ToString("F2");
                    lblAvgGaugeLength.Text = avgGauge.ToString("F2");

                    ov_lblMinValue.Text = minWeight.ToString("F2");
                    ov_lblMaxValue.Text = maxWeight.ToString("F2");
                    ov_lblDiffMinMax.Text = diffWeight.ToString("F2");
                    lblAvgWeights.Text = avgWeight.ToString("F2");

                    DataTable dataTable = new DataTable();
                    dataTable.Columns.Add("sl", typeof(int));
                    dataTable.Columns.Add("ge", typeof(decimal));
                    dataTable.Columns.Add("wt", typeof(decimal));

                    foreach (var item in ovenEndDataList)
                    {
                        DataRow row = dataTable.NewRow();
                        row["sl"] = item.sl;
                        row["ge"] = item.ge;
                        row["wt"] = item.wt;
                        dataTable.Rows.Add(row);
                    }

                    OvenEnd_GridView.DataSource = dataTable;
                    OvenEnd_GridView.DataBind();
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Error deserializing JSON: {ex.Message}");
                }
            }
            else
            {
                OvenEnd_GridView.DataSource = null;
                OvenEnd_GridView.DataBind();

                ov_lblMinGauge.Text = string.Empty;
                ov_lblMaxGauge.Text = string.Empty;
                ov_lblDiffGauge.Text = string.Empty;
                lblAvgGaugeLength.Text = string.Empty;

                ov_lblMinValue.Text = string.Empty;
                ov_lblMaxValue.Text = string.Empty;
                ov_lblDiffMinMax.Text = string.Empty;
                lblAvgWeights.Text = string.Empty;

                Console.WriteLine("No RLWt value provided.");
            }
        }

        private string GetOvenWeightsFromDB(string rlwt)
        {
            if (string.IsNullOrEmpty(rlwt))
            {
                throw new ArgumentException("The RLWt parameter cannot be null or empty.");
            }

            string jsonData = string.Empty;
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            string query = @"SELECT gaugeandweight FROM TRN_RotaryLine_OvenEnd WHERE Id = @RLWt";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@RLWt", rlwt);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            jsonData = reader["gaugeandweight"].ToString();
                        }
                    }
                }
            }

            return jsonData;
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
                Response.Redirect("qaqc_rotaryline_view.aspx", false);
            }
            else if (ViewerMode == 1)
            {
                Response.Redirect("qaqc_rotaryline_approval.aspx", false);
            }
            else
            {
                Response.Redirect("home.aspx", false);
            }
        }
    }
}


