using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;

namespace AnmolDristi
{
    public partial class Final_CBB_Detailed : System.Web.UI.Page
    {
        public static string PlantId = string.Empty;
        public static string PlantName = string.Empty;
        public static string PlantLine = string.Empty;
        public static string ProductCategory = string.Empty;
        public static string CategoryBrand = string.Empty;
        public static string BrandSKU = string.Empty;

        public static string JSON1 = string.Empty;
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Request.QueryString["ID"] != null)
                {
                    string ID = Request.QueryString["ID"];

                    PlantBinder();
                    DataBinder(ID);
                    //ScriptManager.RegisterStartupScript(this, GetType(), "CalculateGridView", "calculateGridViewData();", true);
                }

            }
        }

        private void DataBinder(string cbb)
        {
            getBasicDetails(cbb);
            BindGridView_Weights(cbb);
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


        void getBasicDetails(string cbb)
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
                        P.GrossWeightJson,
                        P.*
                    FROM
                        TRN_FINAL_CBB_Weights P
                    JOIN dbo.MST_PlantDetails A ON P.PlantName = A.plant_id
                    JOIN dbo.MST_Plant_Lines B ON P.Line = B.line_id
                    JOIN dbo.MST_LineCategory C ON P.ProductCategory = C.category_id
                    JOIN dbo.MST_LineCatBrands D ON P.ProductBrand = D.brand_id
                    JOIN dbo.MST_Brand_SKU E ON P.SKUId = E.SKUId
                    WHERE P.Id = @CBB_PK";


                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@CBB_PK", cbb);
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

                                TXT_BatchNo.Text = dt.Rows[0]["BatchNo"].ToString();
                                TXT_MRP.Text = dt.Rows[0]["MRP"].ToString();

                                JSON1 = row["GrossWeightJson"].ToString();

                                Approver1CodeLabel.Text = dt.Rows[0]["Approver1EmployeeCode"].ToString();
                                Approver2CodeLabel.Text = dt.Rows[0]["Approver2EmployeeCode"].ToString();
                                DottedLineApproverCodeLabel.Text = dt.Rows[0]["DottedLineApproverEmployeeCode"].ToString();

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


        public class WeightData
        {
            public int sl { get; set; }
            public decimal weight { get; set; }
        }

        private void BindGridView_Weights(string cbb)
        {
            Console.WriteLine($"Value of CBB_PK: {cbb}");

            if (string.IsNullOrEmpty(cbb))
            {
                throw new ArgumentException("The parameter CBB_PK cannot be null or empty.");
            }

            //string jsonData = GetLineWeightsFromDB(rlwt);
            string jsonData = JSON1;

            if (!string.IsNullOrEmpty(jsonData))
            {
                try
                {
                    List<WeightData> weightDataList = JsonConvert.DeserializeObject<List<WeightData>>(jsonData);

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

                    GridView1.DataSource = dataTable;
                    GridView1.DataBind();

                    CalculateGridViewData();
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Error deserializing JSON: {ex.Message}");
                }
            }
            else
            {
                GridView1.DataSource = null;
                GridView1.DataBind();
                Console.WriteLine("No CBB_PK value provided.");
            }
        }

        private string GetWeightsFromDB(string cbb)
        {
            if (string.IsNullOrEmpty(cbb))
            {
                throw new ArgumentException("The CBB_PK parameter cannot be null or empty.");
            }

            string jsonData = string.Empty;
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            string query = @" SELECT GrossWeightJson FROM TRN_FINAL_CBB_Weights WHERE Id = @CBB_PK";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CBB_PK", cbb);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            jsonData = reader["GrossWeightJson"] != DBNull.Value ? reader["GrossWeightJson"].ToString() : string.Empty;
                        }
                    }
                }
            }
            return jsonData;
        }

        protected void BtnApprove_Click(object sender, EventArgs e)
        {

        }

        protected void BtnReject_Click(object sender, EventArgs e)
        {

        }


        protected void CalculateGridViewData()
        {
            double totalWeight = 0;
            int count = 0;
            double minWeight = double.MaxValue;
            double maxWeight = double.MinValue;

            // Loop through GridView rows
            foreach (GridViewRow row in GridView1.Rows)
            {
                // Access the value in the "weight" column (BoundField)
                string weightText = row.Cells[1].Text; // Index 1 for the second column (Gross Weight)

                double weight;
                if (double.TryParse(weightText, out  weight))
                {
                    totalWeight += weight;
                    count++;

                    minWeight = Math.Min(minWeight, weight);
                    maxWeight = Math.Max(maxWeight, weight);
                }
            }

            // Calculate average and difference
            double averageWeight = count > 0 ? totalWeight / count : 0;
            double difference = count > 0 ? maxWeight - minWeight : 0;

            // Update the labels
            lblMinValue.Text = count > 0 ? minWeight.ToString("F2") + " gm" : "0.00 gm";
            lblMaxValue.Text = count > 0 ? maxWeight.ToString("F2") + " gm" : "0.00 gm";
            lblDiffMinMax.Text = count > 0 ? difference.ToString("F2") + " gm" : "0.00 gm";
            lblAvgWeights.Text = count > 0 ? averageWeight.ToString("F2") : "0.00";
        }





    }
}