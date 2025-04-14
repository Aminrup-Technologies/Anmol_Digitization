using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class add_plants : System.Web.UI.Page
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
                    TB_PlantID_Description.Focus();
                    BindGridView();
                }
            }
        }

        protected void btn_submit_Click(object sender, EventArgs e)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Begin transaction
                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    // Check if the record exists based on PlantID_ID
                    string checkIfExistsQuery = "SELECT COUNT(*) FROM MST_Plant WHERE PlantID_ID = @PlantIDID";
                    SqlCommand checkIfExistsCommand = new SqlCommand(checkIfExistsQuery, connection, transaction);
                    checkIfExistsCommand.Parameters.AddWithValue("@PlantIDID", TB_PlantID_ID.Text.Trim());

                    int existingCount = (int)checkIfExistsCommand.ExecuteScalar();

                    if (existingCount > 0)
                    {
                        // Record already exists, handle accordingly (e.g., show error message)
                        lbl_msg.Text = "Branch with the same ID already exists!";
                        lbl_msg.ForeColor = System.Drawing.Color.Red;
                        return; // Exit method
                    }

                    // Insert into MST_Branch table
                    string insertQuery = "INSERT INTO MST_Plant (PlantID_Description, PlantID_ID) VALUES (@PlantIDDescription, @PlantIDID)";
                    SqlCommand insertCommand = new SqlCommand(insertQuery, connection, transaction);
                    insertCommand.Parameters.AddWithValue("@PlantIDDescription", TB_PlantID_Description.Text.Trim());
                    insertCommand.Parameters.AddWithValue("@PlantIDID", TB_PlantID_ID.Text.Trim());

                    insertCommand.ExecuteNonQuery();

                    // Commit transaction
                    transaction.Commit();

                    // Clear form or show success message
                    TB_PlantID_Description.Text = "";
                    TB_PlantID_ID.Text = "";
                    lbl_msg.Text = "Branch added successfully!";
                    lbl_msg.ForeColor = System.Drawing.Color.Green;
                }
                catch (Exception ex)
                {
                    var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                    EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                    // Handle exceptions
                    transaction.Rollback();
                    lbl_msg.Text = "Error: " + ex.Message;
                    lbl_msg.ForeColor = System.Drawing.Color.Red;
                }
            }
        }


        protected void BindGridView()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            string query = "SELECT Id, PlantID_Description, PlantID_ID FROM MST_Plant";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataTable dataTable = new DataTable();

                adapter.Fill(dataTable);

                GridViewPlants.DataSource = dataTable;
                GridViewPlants.DataBind();
            }
        }

        protected void GridViewPlants_RowEditing(object sender, GridViewEditEventArgs e)
        {
            GridViewPlants.EditIndex = e.NewEditIndex;
            BindGridView();
        }

        protected void GridViewPlants_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            GridViewPlants.EditIndex = -1;
            BindGridView();
        }

        protected void GridViewPlants_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            GridViewRow row = GridViewPlants.Rows[e.RowIndex];
            int id = Convert.ToInt32(GridViewPlants.DataKeys[e.RowIndex].Values["Id"]);

            TextBox tbBranchDescription = row.FindControl("TextBox1") as TextBox;
            TextBox tbPlantIDID = row.FindControl("TextBox2") as TextBox;

            string branchDescription = tbBranchDescription.Text.Trim();
            string branchID = tbPlantIDID.Text.Trim();

            UpdateBranch(id, branchDescription, branchID);

            GridViewPlants.EditIndex = -1;
            BindGridView();
        }

        protected void UpdateBranch(int id, string branchDescription, string branchID)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "UPDATE MST_Plant SET PlantID_Description = @PlantIDDescription, PlantID_ID = @PlantIDID WHERE Id = @Id";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@PlantIDDescription", branchDescription);
                command.Parameters.AddWithValue("@PlantIDID", branchID);
                command.Parameters.AddWithValue("@Id", id);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        protected void GridViewPlants_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            int id = Convert.ToInt32(GridViewPlants.DataKeys[e.RowIndex].Values["Id"]);
            DeleteBranch(id);
            BindGridView();
        }

        protected void DeleteBranch(int id)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "DELETE FROM MST_Plant WHERE Id = @Id";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        protected void GridViewPlants_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            // Optional: You can customize row data binding, e.g., for formatting or additional logic
        }

        protected void GridViewPlants_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridViewPlants.PageIndex = e.NewPageIndex;
            BindGridView();
        }

        protected void btn_cancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("datamastering_home.aspx");
        }
    }
}