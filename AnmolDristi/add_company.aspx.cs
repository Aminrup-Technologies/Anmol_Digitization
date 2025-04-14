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
    public partial class add_company : System.Web.UI.Page
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
                    TB_Company_Description.Focus();
                    BindGridView();
                }
            }
        }

        protected void btn_submit_Click(object sender, EventArgs e)
        {
            string companyDescription = TB_Company_Description.Text.Trim();
            string companyID = TB_Company_ID.Text.Trim();

            // Retrieve connection string from web.config
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            // Check if record already exists
            bool recordExists = CheckIfRecordExists(connectionString, companyID);

            if (recordExists)
            {
                lbl_msg.Text = "Company with the same ID already exists.";
                return; // Exit if record already exists
            }

            // Insert data using transaction
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    string insertQuery = "INSERT INTO MST_Company (Company_Description, Company_ID) VALUES (@Company_Description, @Company_ID)";
                    SqlCommand cmd = new SqlCommand(insertQuery, conn, transaction);

                    // Add parameters
                    cmd.Parameters.AddWithValue("@Company_Description", companyDescription);
                    cmd.Parameters.AddWithValue("@Company_ID", companyID);

                    // Execute insert command
                    int rowsAffected = cmd.ExecuteNonQuery();

                    // Commit transaction if successful
                    transaction.Commit();

                    if (rowsAffected > 0)
                    {
                        lbl_msg.Text = "Data inserted successfully!";
                    }
                    else
                    {
                        lbl_msg.Text = "Failed to insert data.";
                    }
                }
                catch (Exception ex)
                {
                    var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                    EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                    if (transaction != null)
                    {
                        transaction.Rollback();
                    }
                    lbl_msg.Text = "Error: " + ex.Message;
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        private bool CheckIfRecordExists(string connectionString, string companyID)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT COUNT(*) FROM MST_Company WHERE Company_ID = @Company_ID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Company_ID", companyID);
                conn.Open();
                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
        }


        private void BindGridView()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT Id, Company_Description, Company_ID FROM MST_Company";
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                GridView1.DataSource = dt;
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
            int id = Convert.ToInt32(GridView1.DataKeys[e.RowIndex].Value);
            string companyDescription = (row.FindControl("TextBoxCompanyDescription") as TextBox).Text;
            string companyID = (row.FindControl("TextBoxCompanyID") as TextBox).Text;

            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "UPDATE MST_Company SET Company_Description = @Company_Description, Company_ID = @Company_ID WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Company_Description", companyDescription);
                cmd.Parameters.AddWithValue("@Company_ID", companyID);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }

            GridView1.EditIndex = -1;
            BindGridView();
        }

        protected void GridView1_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            int id = Convert.ToInt32(GridView1.DataKeys[e.RowIndex].Value);

            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "DELETE FROM MST_Company WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }

            BindGridView();
        }

        protected void GridView1_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "Page")
            {
                // Handle page index changing here if needed
            }
        }

        protected void GridView1_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridView1.PageIndex = e.NewPageIndex;
            BindGridView();
        }

        protected void btn_cancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("datamastering_home.aspx");
        }
    }
}