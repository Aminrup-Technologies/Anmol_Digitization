using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class add_prodcategory : System.Web.UI.Page
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
                }
            }
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
                BindGridViewbyPlantLine(selectedPlantValue, selectedPlantLineValue);

                //------- function call to fetch the last used code and 

                int categoryID = GetNextCategoryId();
                txtCategoryID.Text = categoryID.ToString();
                txtCategoryID.ReadOnly = true;
            }
            else
            {

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


        public int GetNextCategoryId()
        {
            int nextCategoryId = 0;

            // Replace with your actual connection string
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            // Define your query to call the function
            string query = "SELECT dbo.GetNextLineCategoryId() AS NextCategoryId";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                try
                {
                    connection.Open();
                    // Execute the query and get the next category ID
                    nextCategoryId = (int)command.ExecuteScalar();
                }
                catch (Exception ex)
                {
                    // Handle exceptions
                    Console.WriteLine(ex.Message);
                }
            }

            return nextCategoryId;
        }

        protected void btn_submit_Click(object sender, EventArgs e)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    // Retrieve the values from the input fields
                    //int categoryID = Convert.ToInt32(txtCategoryID.Text);
                    int categoryID = GetNextCategoryId();
                    string categoryName = txtCategoryName.Text;

                    // Check if a record with the same CategoryID and CategoryName already exists
                    string checkQuery = "SELECT COUNT(*) FROM MST_LineCategory WHERE category_id = @CategoryID AND category_name = @CategoryName";
                    using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@CategoryID", categoryID);
                        checkCommand.Parameters.AddWithValue("@CategoryName", categoryName);

                        int count = Convert.ToInt32(checkCommand.ExecuteScalar());
                        if (count > 0)
                        {
                            lbl_msg.Text = "A record with the same Category ID and Category Name already exists.";
                            lbl_msg.Visible = true;
                            return;
                        }
                    }

                    // Insert the new record if no duplicate is found
                    string insertQuery = @"INSERT INTO MST_LineCategory (plant_id, line_id, category_id, category_name, category_sapcode, local_name, view_status, delete_status) 
                                   VALUES (@PlantID, @LineID, @CategoryID, @CategoryName, @CategorySapCode, @LocalName, @ViewStatus, @DeleteStatus)";
                    using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@PlantID", Convert.ToInt32(DDL_Plant.SelectedValue));
                        insertCommand.Parameters.AddWithValue("@LineID", Convert.ToInt32(DDL_PlantLine.SelectedValue));
                        insertCommand.Parameters.AddWithValue("@CategoryID", categoryID);
                        insertCommand.Parameters.AddWithValue("@CategoryName", categoryName);
                        insertCommand.Parameters.AddWithValue("@CategorySapCode", txtCategorySapCode.Text);
                        insertCommand.Parameters.AddWithValue("@LocalName", txtLocalName.Text);
                        insertCommand.Parameters.AddWithValue("@ViewStatus", 1);
                        insertCommand.Parameters.AddWithValue("@DeleteStatus", 0);
                        //insertCommand.Parameters.AddWithValue("@ViewStatus", chkViewStatus.Checked ? 1 : 0);
                        //insertCommand.Parameters.AddWithValue("@DeleteStatus", chkDeleteStatus.Checked ? 1 : 0);

                        insertCommand.ExecuteNonQuery();
                        lbl_msg.Visible = false; // Hide error message if insertion is successful
                    }
                }
                catch (Exception ex)
                {
                    // Handle exceptions
                    lbl_msg.Text = "An error occurred: " + ex.Message;
                    lbl_msg.Visible = true;
                }
            }

            // Optionally, refresh the GridView or perform other actions after successful insertion
        }


        //protected void btn_submit_Click(object sender, EventArgs e)
        //{
        //    string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        //    using (SqlConnection connection = new SqlConnection(connectionString))
        //    {
        //        connection.Open();
        //        SqlTransaction transaction = connection.BeginTransaction();

        //        try
        //        {
        //            int plant_id = Convert.ToInt32(DDL_Plant.SelectedValue);
        //            int line_id = Convert.ToInt32(DDL_PlantLine.SelectedValue);
        //            int category_id = Convert.ToInt32(txtCategoryID.Text);
        //            string category_name = txtCategoryName.Text;
        //            string category_sapcode = txtCategorySapCode.Text;
        //            string local_name = txtLocalName.Text;
        //            bool view_status = chkViewStatus.Checked;
        //            bool delete_status = chkDeleteStatus.Checked;

        //            string query = "INSERT INTO MST_LineCategory (plant_id, line_id, category_id, category_name, category_sapcode, local_name, view_status, delete_status) " +
        //                           "VALUES (@plant_id, @line_id, @category_id, @category_name, @category_sapcode, @local_name, @view_status, @delete_status)";

        //            using (SqlCommand cmd = new SqlCommand(query, connection, transaction))
        //            {
        //                cmd.Parameters.AddWithValue("@plant_id", plant_id);
        //                cmd.Parameters.AddWithValue("@line_id", line_id);
        //                cmd.Parameters.AddWithValue("@category_id", category_id);
        //                cmd.Parameters.AddWithValue("@category_name", category_name);
        //                cmd.Parameters.AddWithValue("@category_sapcode", category_sapcode ?? (object)DBNull.Value);
        //                cmd.Parameters.AddWithValue("@local_name", local_name ?? (object)DBNull.Value);
        //                cmd.Parameters.AddWithValue("@view_status", view_status);
        //                cmd.Parameters.AddWithValue("@delete_status", delete_status);

        //                //cmd.ExecuteNonQuery();
        //            }

        //            transaction.Commit();
        //            // Optionally, show a success message or reset the form
        //        }
        //        catch (Exception ex)
        //        {
        //            transaction.Rollback();
        //            // Handle exception (e.g., show an error message)
        //        }
        //    }
        //}

        protected void GridViewLineCategory_RowEditing(object sender, GridViewEditEventArgs e)
        {
            GridViewLineCategory.EditIndex = e.NewEditIndex;
            BindGridView();
        }

        protected void GridViewLineCategory_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            GridViewLineCategory.EditIndex = -1;
            BindGridView();
        }

        protected void GridViewLineCategory_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            GridViewRow row = GridViewLineCategory.Rows[e.RowIndex];
            int id = Convert.ToInt32(GridViewLineCategory.DataKeys[e.RowIndex].Values["id"]);

            TextBox txtPlantID = row.FindControl("txtPlantID") as TextBox;
            TextBox txtLineID = row.FindControl("txtLineID") as TextBox;
            TextBox txtCategoryID = row.FindControl("txtCategoryID") as TextBox;
            TextBox txtCategoryName = row.FindControl("txtCategoryName") as TextBox;
            TextBox txtCategorySapCode = row.FindControl("txtCategorySapCode") as TextBox;
            TextBox txtLocalName = row.FindControl("txtLocalName") as TextBox;
            //CheckBox chkViewStatus = row.FindControl("chkViewStatus") as CheckBox;
            //CheckBox chkDeleteStatus = row.FindControl("chkDeleteStatus") as CheckBox;

            //if (txtPlantID == null || txtLineID == null || txtCategoryID == null || txtCategoryName == null || txtCategorySapCode == null || txtLocalName == null || chkViewStatus == null || chkDeleteStatus == null)
            //{
            //    throw new Exception("One or more controls are missing from the GridView row.");
            //}

            int plant_id = Convert.ToInt32(txtPlantID.Text);
            int line_id = Convert.ToInt32(txtLineID.Text);
            int category_id = Convert.ToInt32(txtCategoryID.Text);
            string category_name = txtCategoryName.Text;
            string category_sapcode = txtCategorySapCode.Text;
            string local_name = txtLocalName.Text;
            int chkViewStatus = (row.FindControl("view_status") as CheckBox)?.Checked == true ? 1 : 0;
            int delete_status = (row.FindControl("delete_status") as CheckBox)?.Checked == true ? 1 : 0;

            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    string query = @"
                UPDATE MST_LineCategory 
                SET plant_id = @plant_id, line_id = @line_id, category_id = @category_id, category_name = @category_name, 
                    category_sapcode = @category_sapcode, local_name = @local_name, view_status = @view_status, delete_status = @delete_status 
                WHERE id = @id";

                    using (SqlCommand cmd = new SqlCommand(query, connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@plant_id", plant_id);
                        cmd.Parameters.AddWithValue("@line_id", line_id);
                        cmd.Parameters.AddWithValue("@category_id", category_id);
                        cmd.Parameters.AddWithValue("@category_name", category_name);
                        cmd.Parameters.AddWithValue("@category_sapcode", category_sapcode);
                        cmd.Parameters.AddWithValue("@local_name", local_name);
                        cmd.Parameters.AddWithValue("@view_status", chkViewStatus);
                        cmd.Parameters.AddWithValue("@delete_status", delete_status);
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new Exception("Error updating the record: " + ex.Message);
                }
            }

            GridViewLineCategory.EditIndex = -1;
            BindGridView();
        }

        protected void GridViewLineCategory_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            int id = Convert.ToInt32(GridViewLineCategory.DataKeys[e.RowIndex].Values["id"]);

            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    string query = @"
                UPDATE MST_LineCategory 
                SET view_status = 0, delete_status = 1 
                WHERE id = @id";

                    using (SqlCommand cmd = new SqlCommand(query, connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new Exception("Error deleting the record: " + ex.Message);
                }
            }
            BindGridView();
        }

        private void BindGridView()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            string query = "SELECT [id], [plant_id], [line_id], [category_id], [category_name], [category_sapcode], [local_name], [added_on], [view_status], [delete_status] FROM [MST_LineCategory] order by Id desc";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                DataTable dt = new DataTable();

                adapter.Fill(dt);
                GridViewLineCategory.DataSource = dt;
                GridViewLineCategory.DataBind();
            }
        }

        private void BindGridViewbyPlant(string PlantId)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            string query = "SELECT [id], [plant_id], [line_id], [category_id], [category_name], [category_sapcode], [local_name], [added_on], [view_status], [delete_status] FROM [MST_LineCategory] where plant_id = '"+ PlantId + "'";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                DataTable dt = new DataTable();

                adapter.Fill(dt);
                GridViewLineCategory.DataSource = dt;
                GridViewLineCategory.DataBind();
            }
        }


        private void BindGridViewbyPlantLine(string PlantId, string LineId)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            string query = "SELECT [id], [plant_id], [line_id], [category_id], [category_name], [category_sapcode], [local_name], [added_on], [view_status], [delete_status] FROM [MST_LineCategory] where plant_id = '" + PlantId + "' and line_id='"+ LineId + "' order by Id desc";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                DataTable dt = new DataTable();

                adapter.Fill(dt);
                GridViewLineCategory.DataSource = dt;
                GridViewLineCategory.DataBind();
            }
        }

        protected void GridViewLineCategory_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridViewLineCategory.PageIndex = e.NewPageIndex;
            BindGridView();
        }

        protected void btn_cancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("datamastering_home.aspx");
        }
    }
}