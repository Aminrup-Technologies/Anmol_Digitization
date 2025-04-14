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
    public partial class add_plant_lines : System.Web.UI.Page
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
                    BindNextLineId();
                }
            }
        }

        private void BindNextLineId()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT TOP 1 line_id FROM [MST_Plant_Lines] ORDER BY id DESC;";
                SqlCommand cmd = new SqlCommand(query, conn);

                conn.Open();
                object result = cmd.ExecuteScalar();
                if (result != DBNull.Value)
                {
                    int lastLineID = Convert.ToInt32(result);
                    int nextLineID = lastLineID + 1;
                    TB_Line_ID.Text = nextLineID.ToString(); // lblNextLineID is your Label control
                }
                else
                {
                    TB_Line_ID.Text = "201"; // If no records exist, start with 1
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
                BindGridViewbyPlant(selectedPlantValue);

            }
            else
            {
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

        private void BindGridViewbyPlant(string plantid)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            string query = "SELECT [id], [plant_id], [line_id], [line_name], [local_name], [line_sap_code], [added_on], [view_status], [delete_status] FROM [dbo].[MST_Plant_Lines] where plant_id='"+ plantid + "' order by Id desc";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dataTable = new DataTable();

                try
                {
                    connection.Open();
                    adapter.Fill(dataTable);
                    GridViewPlantLines.DataSource = dataTable;
                    GridViewPlantLines.DataBind();
                }
                catch (Exception ex)
                {
                    var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                    EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                    string errorMessage = ex.Message;
                    string errorScript = $"new PNotify({{ title: 'Error', text: '{errorMessage}', type: 'error', styling: 'bootstrap3' }});";
                    ClientScript.RegisterStartupScript(this.GetType(), "ShowErrorNotification", errorScript, true);
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        protected void btn_submit_Click(object sender, EventArgs e)
        {
            int plant_id = int.Parse(DDL_Plant.SelectedValue); // Assuming DDL_Plant is your DropDownList for Plant ID
            int line_id = int.Parse(TB_Line_ID.Text.Trim());
            string line_name = TB_Line_Name.Text.Trim();
            string local_name = TB_Local_Name.Text.Trim();
            string line_sap_code = TB_Line_Sap_Code.Text.Trim();

            // Check if the line_name already exists for the given plant_id
            if (IsLineNameDuplicate(plant_id, line_name))
            {
                // Handle duplicate record scenario
                // You can show a message or handle it as per your application's requirement
                return;
            }

            // Insert into database using transaction
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    // Insert command
                    string insertQuery = @"INSERT INTO MST_Plant_Lines (plant_id, line_id, line_name, local_name, line_sap_code) 
                                   VALUES (@plant_id, @line_id, @line_name, @local_name, @line_sap_code)";
                    SqlCommand cmdInsert = new SqlCommand(insertQuery, connection, transaction);
                    cmdInsert.Parameters.AddWithValue("@plant_id", plant_id);
                    cmdInsert.Parameters.AddWithValue("@line_id", line_id);
                    cmdInsert.Parameters.AddWithValue("@line_name", line_name);
                    cmdInsert.Parameters.AddWithValue("@local_name", local_name);
                    cmdInsert.Parameters.AddWithValue("@line_sap_code", line_sap_code);

                    cmdInsert.ExecuteNonQuery();

                    transaction.Commit(); // Commit transaction if successful
                    ClearForm(); // Clear form inputs after successful insertion
                    BindGridView(); // Update GridView after successful insertion
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                    EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                    string errorMessage = ex.Message;
                    string errorScript = $"new PNotify({{ title: 'Error', text: '{errorMessage}', type: 'error', styling: 'bootstrap3' }});";
                    ClientScript.RegisterStartupScript(this.GetType(), "ShowErrorNotification", errorScript, true);
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        private bool IsLineNameDuplicate(int plant_id, string line_name)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = @"SELECT COUNT(*) FROM MST_Plant_Lines WHERE plant_id = @plant_id AND line_name = @line_name";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@plant_id", plant_id);
                cmd.Parameters.AddWithValue("@line_name", line_name);

                connection.Open();
                int count = (int)cmd.ExecuteScalar();
                connection.Close();

                return count > 0;
            }
        }

        // Method to clear form inputs after successful insertion
        private void ClearForm()
        {
            TB_Line_ID.Text = "";
            TB_Line_Name.Text = "";
            TB_Local_Name.Text = "";
            TB_Line_Sap_Code.Text = "";
        }

        // Method to bind GridView after successful insertion
        private void BindGridView()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            string query = "SELECT [id], [plant_id], [line_id], [line_name], [local_name], [line_sap_code], [added_on], [view_status], [delete_status] FROM [dbo].[MST_Plant_Lines] order by Id desc";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dataTable = new DataTable();

                try
                {
                    connection.Open();
                    adapter.Fill(dataTable);
                    GridViewPlantLines.DataSource = dataTable;
                    GridViewPlantLines.DataBind();
                }
                catch (Exception ex)
                {
                    var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                    EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                    string errorMessage = ex.Message;
                    string errorScript = $"new PNotify({{ title: 'Error', text: '{errorMessage}', type: 'error', styling: 'bootstrap3' }});";
                    // Handle exceptions (log or display error message)
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        protected void GridViewPlantLines_RowEditing(object sender, GridViewEditEventArgs e)
        {
            GridViewPlantLines.EditIndex = e.NewEditIndex;
            BindGridView();
        }

        protected void GridViewPlantLines_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            GridViewPlantLines.EditIndex = -1;
            BindGridView();
        }

        protected void GridViewPlantLines_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            GridViewRow row = GridViewPlantLines.Rows[e.RowIndex];
            int id = Convert.ToInt32(GridViewPlantLines.DataKeys[e.RowIndex].Values["id"]);

            // Extract updated values
            string line_name = (row.FindControl("TB_Edit_Line_Name") as TextBox).Text;
            string local_name = (row.FindControl("TB_Edit_Local_Name") as TextBox).Text;
            string line_sap_code = (row.FindControl("TB_Edit_Line_Sap_Code") as TextBox).Text;

            // Update database
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            string query = "UPDATE MST_Plant_Lines SET line_name = @line_name, local_name = @local_name, line_sap_code = @line_sap_code WHERE id = @id";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@line_name", line_name);
                cmd.Parameters.AddWithValue("@local_name", local_name);
                cmd.Parameters.AddWithValue("@line_sap_code", line_sap_code);
                cmd.Parameters.AddWithValue("@id", id);

                try
                {
                    connection.Open();
                    cmd.ExecuteNonQuery();
                    GridViewPlantLines.EditIndex = -1;
                    BindGridView();
                }
                catch (Exception ex)
                {
                    var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                    EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                    string errorMessage = ex.Message;
                    string errorScript = $"new PNotify({{ title: 'Error', text: '{errorMessage}', type: 'error', styling: 'bootstrap3' }});";
                    // Handle exceptions (log or display error message)
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        protected void GridViewPlantLines_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            int id = Convert.ToInt32(GridViewPlantLines.DataKeys[e.RowIndex].Values["id"]);

            // Soft delete record (set delete_status = 1)
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            string query = "UPDATE MST_Plant_Lines SET delete_status = 1 WHERE id = @id";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@id", id);

                try
                {
                    connection.Open();
                    cmd.ExecuteNonQuery();
                    BindGridView();
                }
                catch (Exception ex)
                {
                    var recipients = EmailRecipientManager.GetRecipients("ErrorNotifications");
                    EmailNotifier.Notify("Application Error", $"<p>Error: {ex.Message}</p><p>Stack Trace: {ex.StackTrace}</p>", recipients);

                    string errorMessage = ex.Message;
                    string errorScript = $"new PNotify({{ title: 'Error', text: '{errorMessage}', type: 'error', styling: 'bootstrap3' }});";
                    // Handle exceptions (log or display error message)
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        protected void GridViewPlantLines_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow && (e.Row.RowState & DataControlRowState.Edit) == DataControlRowState.Edit)
            {
                // Prepopulate fields during edit mode
                int id = Convert.ToInt32(GridViewPlantLines.DataKeys[e.Row.RowIndex].Values["id"]);
                string line_name = DataBinder.Eval(e.Row.DataItem, "line_name").ToString();
                string local_name = DataBinder.Eval(e.Row.DataItem, "local_name").ToString();
                string line_sap_code = DataBinder.Eval(e.Row.DataItem, "line_sap_code").ToString();

                TextBox TB_Edit_Line_Name = e.Row.FindControl("TB_Edit_Line_Name") as TextBox;
                TextBox TB_Edit_Local_Name = e.Row.FindControl("TB_Edit_Local_Name") as TextBox;
                TextBox TB_Edit_Line_Sap_Code = e.Row.FindControl("TB_Edit_Line_Sap_Code") as TextBox;

                if (TB_Edit_Line_Name != null)
                    TB_Edit_Line_Name.Text = line_name;

                if (TB_Edit_Local_Name != null)
                    TB_Edit_Local_Name.Text = local_name;

                if (TB_Edit_Line_Sap_Code != null)
                    TB_Edit_Line_Sap_Code.Text = line_sap_code;
            }
        }

        protected void GridViewPlantLines_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridViewPlantLines.PageIndex = e.NewPageIndex;
            BindGridView();
        }

        protected void btn_cancel_Click(object sender, EventArgs e)
        {
            Response.Redirect("datamastering_home.aspx");
        }
    }
}