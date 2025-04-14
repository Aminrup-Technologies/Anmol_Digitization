using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class add_branch : System.Web.UI.Page
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
                    TB_Branch_Description.Focus();
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
                    // Check if the record exists based on Branch_ID
                    string checkIfExistsQuery = "SELECT COUNT(*) FROM MST_Branch WHERE Branch_ID = @BranchID";
                    SqlCommand checkIfExistsCommand = new SqlCommand(checkIfExistsQuery, connection, transaction);
                    checkIfExistsCommand.Parameters.AddWithValue("@BranchID", TB_Branch_ID.Text.Trim());

                    int existingCount = (int)checkIfExistsCommand.ExecuteScalar();

                    if (existingCount > 0)
                    {
                        // Record already exists, handle accordingly (e.g., show error message)
                        lbl_msg.Text = "Branch with the same ID already exists!";
                        lbl_msg.ForeColor = System.Drawing.Color.Red;
                        return; // Exit method
                    }

                    // Insert into MST_Branch table
                    string insertQuery = "INSERT INTO MST_Branch (Branch_Description, Branch_ID) VALUES (@BranchDescription, @BranchID)";
                    SqlCommand insertCommand = new SqlCommand(insertQuery, connection, transaction);
                    insertCommand.Parameters.AddWithValue("@BranchDescription", TB_Branch_Description.Text.Trim());
                    insertCommand.Parameters.AddWithValue("@BranchID", TB_Branch_ID.Text.Trim());

                    insertCommand.ExecuteNonQuery();

                    // Commit transaction
                    transaction.Commit();

                    // Clear form or show success message
                    TB_Branch_Description.Text = "";
                    TB_Branch_ID.Text = "";
                    lbl_msg.Text = "Branch added successfully!";
                    lbl_msg.ForeColor = System.Drawing.Color.Green;
                }
                catch (Exception ex)
                {
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
            string query = "SELECT Id, Branch_Description, Branch_ID FROM MST_Branch";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataTable dataTable = new DataTable();

                adapter.Fill(dataTable);

                GridViewBranches.DataSource = dataTable;
                GridViewBranches.DataBind();
            }
        }

        protected void GridViewBranches_RowEditing(object sender, GridViewEditEventArgs e)
        {
            GridViewBranches.EditIndex = e.NewEditIndex;
            BindGridView();
        }

        protected void GridViewBranches_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            GridViewBranches.EditIndex = -1;
            BindGridView();
        }

        protected void GridViewBranches_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            GridViewRow row = GridViewBranches.Rows[e.RowIndex];
            int id = Convert.ToInt32(GridViewBranches.DataKeys[e.RowIndex].Values["Id"]);

            TextBox tbBranchDescription = row.FindControl("TextBox1") as TextBox;
            TextBox tbBranchID = row.FindControl("TextBox2") as TextBox;

            string branchDescription = tbBranchDescription.Text.Trim();
            string branchID = tbBranchID.Text.Trim();

            UpdateBranch(id, branchDescription, branchID);

            GridViewBranches.EditIndex = -1;
            BindGridView();
        }

        protected void UpdateBranch(int id, string branchDescription, string branchID)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "UPDATE MST_Branch SET Branch_Description = @BranchDescription, Branch_ID = @BranchID WHERE Id = @Id";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@BranchDescription", branchDescription);
                command.Parameters.AddWithValue("@BranchID", branchID);
                command.Parameters.AddWithValue("@Id", id);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        protected void GridViewBranches_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            int id = Convert.ToInt32(GridViewBranches.DataKeys[e.RowIndex].Values["Id"]);
            DeleteBranch(id);
            BindGridView();
        }

        protected void DeleteBranch(int id)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "DELETE FROM MST_Branch WHERE Id = @Id";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        protected void GridViewBranches_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            // Optional: You can customize row data binding, e.g., for formatting or additional logic
        }

        protected void GridViewBranches_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridViewBranches.PageIndex = e.NewPageIndex;
            BindGridView();
        }

        protected void btn_cancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("datamastering_home.aspx");
        }
    }
}