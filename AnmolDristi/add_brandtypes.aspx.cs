using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace AnmolDristi
{
    public partial class add_brandtypes : System.Web.UI.Page
    {
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
                    PlantBinder();
                    BindGridView();
                    //BindGridViewonLoad();
                }
            }
        }


        private void BindGridViewonLoad()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string selectQuery = "SELECT * FROM MST_LineCatBrands order by Id desc";
                using (SqlCommand command = new SqlCommand(selectQuery, connection))
                {
                    connection.Open();
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    GridViewLineCatBrands.DataSource = dt;
                    GridViewLineCatBrands.DataBind();
                }
            }
        }


        private void BindDropDownLists()
        {
            // Bind DDL_Plant, DDL_PlantLine, and DDL_ProductCategory here
            // Example:
            BindDropDownList(DDL_Plant, "SELECT plant_id, plant_name FROM MST_PlantDetails", "plant_name", "plant_id");
            BindDropDownList(DDL_PlantLine, "SELECT line_id, line_name FROM MST_Plant_Lines", "line_name", "line_id");
            BindDropDownList(DDL_ProductCategory, "SELECT category_id, category_name FROM MST_LineCategory", "category_name", "category_id");
        }

        private void BindDropDownList(DropDownList ddl, string query, string textField, string valueField)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    ddl.DataSource = reader;
                    ddl.DataTextField = textField;
                    ddl.DataValueField = valueField;
                    ddl.DataBind();
                }
            }

            ddl.Items.Insert(0, new ListItem("-- Select --", "0"));
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

        protected void DDL_Plant_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Plant.SelectedIndex != 0)
            {
                string selectedPlantValue = DDL_Plant.SelectedValue.ToString();
                lbl_DDL_Plant_Value.Text = selectedPlantValue;
                PlantLinesBinder(selectedPlantValue);
                BindGridViewbyPlant(selectedPlantValue);
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
                lbl_DDL_PlantLine_Value.Text = selectedPlantLineValue;
                LineProductsBinder(selectedPlantValue, selectedPlantLineValue);
                BindGridViewbyPlantLine(selectedPlantValue, selectedPlantLineValue);
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
                lbl_DDL_ProductCategory_Value.Text = selectedProductCategoryValue;
                BindGridViewbyPlantLineCategory(selectedPlantValue, selectedPlantLineValue, selectedProductCategoryValue);
                //ProductBrandsBinder(selectedPlantValue, selectedPlantLineValue, selectedProductCategoryValue);

                int categoryID = GetNextBrandId();
                txtBrandID.Text = categoryID.ToString();
                txtBrandID.ReadOnly = true;
            }
            else
            {
                //DatabaseHelper.BindWithDefaultNoRecords(DDL_ProductBrand);

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

        protected void GridViewLineCatBrands_RowEditing(object sender, GridViewEditEventArgs e)
        {
            GridViewLineCatBrands.EditIndex = e.NewEditIndex;
            BindGridView();
        }

        protected void GridViewLineCatBrands_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            GridViewLineCatBrands.EditIndex = -1;
            BindGridView();
        }

        protected void GridViewLineCatBrands_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            GridViewRow row = GridViewLineCatBrands.Rows[e.RowIndex];
            int id = Convert.ToInt32(GridViewLineCatBrands.DataKeys[e.RowIndex].Values[0]);
            //int plantID = Convert.ToInt32((row.FindControl("DDL_Plant") as DropDownList).SelectedValue);
            //int lineID = Convert.ToInt32((row.FindControl("DDL_PlantLine") as DropDownList).SelectedValue);
            //int categoryID = Convert.ToInt32((row.FindControl("DDL_ProductCategory") as DropDownList).SelectedValue);
            string brandName = (row.FindControl("txtBrandName") as TextBox).Text;
            string brandLocalName = (row.FindControl("txtBrandLocalName") as TextBox).Text;
            string brandSapCode = (row.FindControl("txtBrandSapCode") as TextBox).Text;
            //bool viewStatus = (row.FindControl("view_status") as CheckBox).Checked;
            //bool deleteStatus = (row.FindControl("delete_status") as CheckBox).Checked;

            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string updateQuery = @"UPDATE MST_LineCatBrands SET brand_name = @BrandName, brand_localname = @BrandLocalName, brand_sapcode = @BrandSapCode, view_status = @ViewStatus, delete_status = @DeleteStatus WHERE Id = @Id";
                using (SqlCommand command = new SqlCommand(updateQuery, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    //command.Parameters.AddWithValue("@PlantID", plantID);
                    //command.Parameters.AddWithValue("@LineID", lineID);
                    //command.Parameters.AddWithValue("@CategoryID", categoryID);
                    command.Parameters.AddWithValue("@BrandName", brandName);
                    command.Parameters.AddWithValue("@BrandLocalName", brandLocalName);
                    command.Parameters.AddWithValue("@BrandSapCode", brandSapCode);
                    command.Parameters.AddWithValue("@ViewStatus", 1);
                    command.Parameters.AddWithValue("@DeleteStatus", 0);

                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }

            GridViewLineCatBrands.EditIndex = -1;
            BindGridView();
        }

        protected void GridViewLineCatBrands_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            int id = Convert.ToInt32(GridViewLineCatBrands.DataKeys[e.RowIndex].Values[0]);
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string deleteQuery = "DELETE FROM MST_LineCatBrands WHERE Id = @Id";
                using (SqlCommand command = new SqlCommand(deleteQuery, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }

            BindGridView();
        }

        public int GetNextBrandId()
        {
            int nextBrandId = 0;

            // Replace with your actual connection string
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            // Define your query to call the function
            string query = "SELECT dbo.GetNextBrandId() AS NextBrandId";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                try
                {
                    connection.Open();
                    // Execute the query and get the next brand ID
                    nextBrandId = (int)command.ExecuteScalar();
                }
                catch (Exception ex)
                {
                    // Handle exceptions
                    Console.WriteLine(ex.Message);
                }
            }

            return nextBrandId;
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlTransaction transaction = null;
                try
                {
                    connection.Open();
                    transaction = connection.BeginTransaction();

                    // Retrieve the values from the input fields
                    int plantID = Convert.ToInt32(DDL_Plant.SelectedValue);
                    int lineID = Convert.ToInt32(DDL_PlantLine.SelectedValue);
                    int categoryID = Convert.ToInt32(DDL_ProductCategory.SelectedValue);
                    //int BrandID = Convert.ToInt32(txtBrandID.Text);
                    int BrandID = GetNextBrandId();
                    string brandName = txtBrandName.Text;

                    // Check if a record with the same PlantID, LineID, CategoryID, and BrandName already exists
                    string checkQuery = "SELECT COUNT(*) FROM MST_LineCatBrands WHERE brand_id = @BrandID";
                    using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection, transaction))
                    {
                        checkCommand.Parameters.AddWithValue("@BrandID", BrandID);
                        checkCommand.Parameters.AddWithValue("@BrandName", brandName);

                        int count = Convert.ToInt32(checkCommand.ExecuteScalar());
                        if (count > 0)
                        {
                            lbl_msg.Text = "A record with the provided Brand_ID";
                            lbl_msg.Visible = true;
                            transaction.Rollback();
                            return;
                        }
                    }

                    // Insert the new record if no duplicate is found
                    string insertQuery = @"INSERT INTO MST_LineCatBrands (plant_id, line_id, category_id, brand_id, brand_name, brand_localname, brand_sapcode, view_status, delete_status) 
                                   VALUES (@PlantID, @LineID, @CategoryID, @BrandID, @BrandName, @BrandLocalName, @BrandSapCode, @ViewStatus, @DeleteStatus)";
                    using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection, transaction))
                    {
                        insertCommand.Parameters.AddWithValue("@PlantID", plantID);
                        insertCommand.Parameters.AddWithValue("@LineID", lineID);
                        insertCommand.Parameters.AddWithValue("@CategoryID", categoryID);
                        insertCommand.Parameters.AddWithValue("@BrandID", BrandID);
                        insertCommand.Parameters.AddWithValue("@BrandName", brandName);
                        insertCommand.Parameters.AddWithValue("@BrandLocalName", txtBrandLocalName.Text);
                        insertCommand.Parameters.AddWithValue("@BrandSapCode", txtBrandSapCode.Text);
                        insertCommand.Parameters.AddWithValue("@ViewStatus", 1); // Default value for view_status
                        insertCommand.Parameters.AddWithValue("@DeleteStatus", 0); // Default value for delete_status

                        insertCommand.ExecuteNonQuery();
                        lbl_msg.Visible = false; // Hide error message if insertion is successful
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    // Handle exceptions
                    if (transaction != null) transaction.Rollback();
                    lbl_msg.Text = "An error occurred: " + ex.Message;
                    lbl_msg.Visible = true;
                }
            }

            // Optionally, refresh the GridView or perform other actions after successful insertion
            BindGridView();
        }

        private void BindGridView()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string selectQuery = "SELECT * FROM MST_LineCatBrands order by Id desc";
                using (SqlCommand command = new SqlCommand(selectQuery, connection))
                {
                    connection.Open();
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    GridViewLineCatBrands.DataSource = dt;
                    GridViewLineCatBrands.DataBind();
                }
            }
        }

        private void BindGridViewbyPlant(string selectedPlantValue)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string selectQuery = "SELECT * FROM MST_LineCatBrands where plant_id='"+ selectedPlantValue + "' order by Id desc";
                using (SqlCommand command = new SqlCommand(selectQuery, connection))
                {
                    connection.Open();
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    GridViewLineCatBrands.DataSource = dt;
                    GridViewLineCatBrands.DataBind();
                }
            }
        }


        private void BindGridViewbyPlantLine(string selectedPlantValue, string selectedPlantLineValue)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string selectQuery = "SELECT * FROM MST_LineCatBrands where plant_id='" + selectedPlantValue + "' and line_id='"+ selectedPlantLineValue + "' order by id desc";
                using (SqlCommand command = new SqlCommand(selectQuery, connection))
                {
                    connection.Open();
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    GridViewLineCatBrands.DataSource = dt;
                    GridViewLineCatBrands.DataBind();
                }
            }
        }

        private void BindGridViewbyPlantLineCategory(string selectedPlantValue, string selectedPlantLineValue, string selectedProductCategoryValue)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string selectQuery = "SELECT * FROM MST_LineCatBrands where plant_id='" + selectedPlantValue + "' and line_id='" + selectedPlantLineValue + "' and category_id='"+ selectedProductCategoryValue + "' order by Id desc";
                using (SqlCommand command = new SqlCommand(selectQuery, connection))
                {
                    connection.Open();
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    GridViewLineCatBrands.DataSource = dt;
                    GridViewLineCatBrands.DataBind();
                }
            }
        }

        protected void btn_cancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("datamastering_home.aspx");
        }
    }
}