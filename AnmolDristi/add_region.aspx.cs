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
    public partial class add_region : System.Web.UI.Page
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
                    TB_Region_Description.Focus();
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
                    // Check if the record exists based on Region_ID
                    string checkIfExistsQuery = "SELECT COUNT(*) FROM MST_Region WHERE Region_ID = @RegionID";
                    SqlCommand checkIfExistsCommand = new SqlCommand(checkIfExistsQuery, connection, transaction);
                    checkIfExistsCommand.Parameters.AddWithValue("@RegionID", TB_Region_ID.Text.Trim());

                    int existingCount = (int)checkIfExistsCommand.ExecuteScalar();

                    if (existingCount > 0)
                    {
                        // Record already exists, handle accordingly (e.g., show error message)
                        lbl_msg.Text = "Region with the same ID already exists!";
                        lbl_msg.ForeColor = System.Drawing.Color.Red;
                        return; // Exit method
                    }

                    // Insert into MST_Region table
                    string insertQuery = "INSERT INTO MST_Region (Region_Description, Region_ID) VALUES (@RegionDescription, @RegionID)";
                    SqlCommand insertCommand = new SqlCommand(insertQuery, connection, transaction);
                    insertCommand.Parameters.AddWithValue("@RegionDescription", TB_Region_Description.Text.Trim());
                    insertCommand.Parameters.AddWithValue("@RegionID", TB_Region_ID.Text.Trim());

                    insertCommand.ExecuteNonQuery();

                    // Commit transaction
                    transaction.Commit();

                    // Clear form or show success message
                    TB_Region_Description.Text = "";
                    TB_Region_ID.Text = "";
                    lbl_msg.Text = "Region added successfully!";
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
            string query = "SELECT Id, Region_Description, Region_ID FROM MST_Region";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                DataTable dataTable = new DataTable();

                adapter.Fill(dataTable);

                GridView1.DataSource = dataTable;
                GridView1.DataBind();
            }
        }

        protected void GridView1_RowEditing(object sender, GridViewEditEventArgs e)
        {
            GridView1.EditIndex = e.NewEditIndex;
            BindGridView();
        }

        protected void GridView1_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            GridView1.EditIndex = -1;
            BindGridView();
        }

        protected void GridView1_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            GridViewRow row = GridView1.Rows[e.RowIndex];
            int id = Convert.ToInt32(GridView1.DataKeys[e.RowIndex].Values["Id"]);

            TextBox tbRegionDescription = row.FindControl("TextBox1") as TextBox;
            TextBox tbRegionID = row.FindControl("TextBox2") as TextBox;

            string regionDescription = tbRegionDescription.Text.Trim();
            string regionID = tbRegionID.Text.Trim();

            UpdateRegion(id, regionDescription, regionID);

            GridView1.EditIndex = -1;
            BindGridView();
        }

        protected void UpdateRegion(int id, string regionDescription, string regionID)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "UPDATE MST_Region SET Region_Description = @RegionDescription, Region_ID = @RegionID WHERE Id = @Id";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@RegionDescription", regionDescription);
                command.Parameters.AddWithValue("@RegionID", regionID);
                command.Parameters.AddWithValue("@Id", id);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        protected void GridView1_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            int id = Convert.ToInt32(GridView1.DataKeys[e.RowIndex].Values["Id"]);
            DeleteRegion(id);
            BindGridView();
        }

        protected void DeleteRegion(int id)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "DELETE FROM MST_Region WHERE Id = @Id";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        protected void GridView1_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            // Optional: You can customize row data binding, e.g., for formatting or additional logic
        }

        protected void btn_cancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("datamastering_home.aspx");
        }
    }
}