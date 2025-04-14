using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class add_prod_plants : System.Web.UI.Page
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
                    TB_plant_name.Focus();
                    BindGridView();
                    BindNextPlantId();
                }
            }
        }

        private void BindGridView()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("SELECT id, plant_id, plant_name, sap_code, local_name, added_on, view_status, delete_status FROM MST_PlantDetails order by Id desc", connection);
                SqlDataAdapter sda = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                sda.Fill(dt);
                GridViewProdPlants.DataSource = dt;
                GridViewProdPlants.DataBind();
            }
        }

        private void BindNextPlantId()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT TOP 1 plant_id FROM [MST_PlantDetails] ORDER BY id DESC;";
                SqlCommand cmd = new SqlCommand(query, conn);

                conn.Open();
                object result = cmd.ExecuteScalar();
                if (result != DBNull.Value)
                {
                    int lastPlantID = Convert.ToInt32(result);
                    int nextPlantID = lastPlantID + 1;
                    TB_plant_id.Text = nextPlantID.ToString(); // lblNextPlantID is your Label control
                }
                else
                {
                    TB_plant_id.Text = "100"; // In case there is no record, start with ID 1
                }
            }
        }

        protected void btn_submit_Click(object sender, EventArgs e)
        {
            int plant_id = int.Parse(TB_plant_id.Text.Trim());
            string plant_name = TB_plant_name.Text.Trim();
            string sap_code = TB_SapCode.Text.Trim();
            string local_name = TB_LocalName.Text.Trim();

            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            SqlTransaction transaction = null;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    transaction = connection.BeginTransaction();

                    // Check if a record with the same plant_id already exists
                    string checkQuery = "SELECT COUNT(*) FROM MST_PlantDetails WITH (UPDLOCK) WHERE plant_id = @plant_id";
                    SqlCommand checkCmd = new SqlCommand(checkQuery, connection, transaction);
                    checkCmd.Parameters.AddWithValue("@plant_id", plant_id);
                    int existingCount = (int)checkCmd.ExecuteScalar();

                    if (existingCount > 0)
                    {
                        // If record exists, you may update it within the transaction
                        //string updateQuery = "UPDATE MST_PlantDetails SET plant_name = @plant_name, sap_code = @sap_code, local_name = @local_name WHERE plant_id = @plant_id";
                        //SqlCommand updateCmd = new SqlCommand(updateQuery, connection, transaction);
                        //updateCmd.Parameters.AddWithValue("@plant_id", plant_id);
                        //updateCmd.Parameters.AddWithValue("@plant_name", plant_name);
                        //updateCmd.Parameters.AddWithValue("@sap_code", sap_code);
                        //updateCmd.Parameters.AddWithValue("@local_name", local_name);
                        //updateCmd.ExecuteNonQuery();
                        lbl_msg.Text = "Company with the same ID already exists.";
                        return;
                    }
                    else
                    {
                        try
                        {
                            // Insert new record if no existing record found
                            string insertQuery = "INSERT INTO MST_PlantDetails (plant_id, plant_name, sap_code, local_name) VALUES (@plant_id, @plant_name, @sap_code, @local_name)";
                            SqlCommand insertCmd = new SqlCommand(insertQuery, connection, transaction);
                            insertCmd.Parameters.AddWithValue("@plant_id", plant_id);
                            insertCmd.Parameters.AddWithValue("@plant_name", plant_name);
                            insertCmd.Parameters.AddWithValue("@sap_code", sap_code);
                            insertCmd.Parameters.AddWithValue("@local_name", local_name);
                            // Execute insert command
                            int rowsAffected = insertCmd.ExecuteNonQuery();
                            // Commit transaction if successful
                            transaction.Commit();

                            if (rowsAffected > 0)
                            {
                                lbl_msg.Text = "Data inserted successfully!";
                            }
                            else
                            {
                                transaction.Rollback();
                                lbl_msg.Text = "Failed to insert data.";
                            }
                        }
                        catch (Exception ex)
                        {
                            var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                            EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                            transaction.Rollback();
                            lbl_msg.Text = "Error: " + ex.Message;
                        }
                    }

                    transaction.Commit();
                    connection.Close();
                }

                ClearForm();
                BindGridView();
            }
            catch (Exception ex)
            {
                var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                // Handle any exceptions here
                if (transaction != null)
                {
                    transaction.Rollback();
                }
                throw new Exception("Error occurred: " + ex.Message);
            }
        }


        private void ClearForm()
        {
            TB_plant_id.Text = "";
            TB_plant_name.Text = "";
            TB_SapCode.Text = "";
            TB_LocalName.Text = "";
        }


        protected void GridViewProdPlants_RowEditing(object sender, GridViewEditEventArgs e)
        {
            GridViewProdPlants.EditIndex = e.NewEditIndex;
            BindGridView();
        }

        protected void GridViewProdPlants_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            GridViewProdPlants.EditIndex = -1;
            BindGridView();
        }

        protected void GridViewProdPlants_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            GridViewRow row = GridViewProdPlants.Rows[e.RowIndex];
            int id = Convert.ToInt32(GridViewProdPlants.DataKeys[e.RowIndex].Values["id"]);

            // Ensure these IDs match exactly with those in your GridView template
            TextBox txtPlantID = row.FindControl("plant_id") as TextBox;
            TextBox txtPlantName = row.FindControl("plant_name") as TextBox;
            TextBox txtSapCode = row.FindControl("sap_code") as TextBox;
            TextBox txtLocalName = row.FindControl("local_name") as TextBox;
            int chkViewStatus = (row.FindControl("view_status") as CheckBox)?.Checked == true ? 1 : 0;
            int delete_status = (row.FindControl("delete_status") as CheckBox)?.Checked == true ? 1 : 0;

            if (txtPlantID == null || txtPlantName == null || txtSapCode == null || txtLocalName == null)
            {
                throw new Exception("One or more controls are missing from the GridView row.");
            }

            int plant_id = Convert.ToInt32(txtPlantID.Text);
            string plant_name = txtPlantName.Text;
            string sap_code = txtSapCode.Text;
            string local_name = txtLocalName.Text;

            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    string query = @"
                UPDATE MST_PlantDetails 
                SET plant_id = @plant_id, plant_name = @plant_name, sap_code = @sap_code, local_name = @local_name, 
                    view_status = @view_status, delete_status = @delete_status 
                WHERE id = @id";

                    using (SqlCommand cmd = new SqlCommand(query, connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@plant_id", plant_id);
                        cmd.Parameters.AddWithValue("@plant_name", plant_name);
                        cmd.Parameters.AddWithValue("@sap_code", sap_code);
                        cmd.Parameters.AddWithValue("@local_name", local_name);
                        cmd.Parameters.AddWithValue("@view_status", chkViewStatus);
                        cmd.Parameters.AddWithValue("@delete_status", delete_status);
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                    EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                    transaction.Rollback();
                    // Log or handle the error
                    throw new Exception("Error updating the record: " + ex.Message);
                }
            }

            GridViewProdPlants.EditIndex = -1;
            BindGridView();
        }

        protected void GridViewProdPlants_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            int id = Convert.ToInt32(GridViewProdPlants.DataKeys[e.RowIndex].Values["id"]);

            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    string query = @"
                UPDATE MST_PlantDetails 
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
                    var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                    EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                    transaction.Rollback();
                    // Log or handle the error
                    throw new Exception("Error deleting the record: " + ex.Message);
                }
            }

            BindGridView();
        }

        protected void GridViewProdPlants_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridViewProdPlants.PageIndex = e.NewPageIndex;
            BindGridView();
        }

        protected void btn_cancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("datamastering_home.aspx");
        }
    }
}