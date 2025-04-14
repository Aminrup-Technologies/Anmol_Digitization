using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AnmolDristi
{
    public partial class Form_Master : System.Web.UI.Page
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
                    lbl_docname.Text = "Create New Form";
                    DepartmentBinder();
                    BindFormsMasterGrid();
                }

            }
        }

        private void BindFormsMasterGrid()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            string query = "SELECT * FROM MST_FormsMaster order by Id desc";
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        GV_FormsMaster.DataSource = dr;
                        GV_FormsMaster.DataBind();
                    }
                }
            }
        }

        protected void GV_FormsMaster_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "ViewDetails")
            {
                // Get the FormID from the CommandArgument
                int formID = Convert.ToInt32(e.CommandArgument);

                // You can now use formID to load more details or redirect to a details page
                Response.Redirect("FormDetails.aspx?FormID=" + formID);
            }
        }


        protected void GV_FormsMaster_RowEditing(object sender, GridViewEditEventArgs e)
        {
            GV_FormsMaster.EditIndex = e.NewEditIndex;
            BindFormsMasterGrid();
        }

        protected void GV_FormsMaster_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            // Get the FormID of the row being updated
            int formID = Convert.ToInt32(GV_FormsMaster.DataKeys[e.RowIndex].Value);

            // Find the TextBox controls in the EditItemTemplate
            //TextBox txtFormName = (TextBox)GV_FormsMaster.Rows[e.RowIndex].FindControl("txtFormName");
            TextBox txtDocNo = (TextBox)GV_FormsMaster.Rows[e.RowIndex].FindControl("txtDocNo");
            TextBox txtDocName = (TextBox)GV_FormsMaster.Rows[e.RowIndex].FindControl("txtDocName");
            TextBox txtIssueNo = (TextBox)GV_FormsMaster.Rows[e.RowIndex].FindControl("txtIssueNo");

            // Update the database with the new values
            string query = "UPDATE MST_FormsMaster SET DocumentNumber=@DocumentNumber, DocumentName=@DocumentName, IssueNo=@IssueNo WHERE FormID=@FormID";
            using (SqlConnection con = new SqlConnection("YourConnectionString"))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    //cmd.Parameters.AddWithValue("@FormName", txtFormName.Text);
                    cmd.Parameters.AddWithValue("@DocumentNumber", txtDocNo.Text);
                    cmd.Parameters.AddWithValue("@DocumentName", txtDocName.Text);
                    cmd.Parameters.AddWithValue("@IssueNo", txtIssueNo.Text);
                    cmd.Parameters.AddWithValue("@FormID", formID);

                    con.Open();
                    cmd.ExecuteNonQuery();
                    GV_FormsMaster.EditIndex = -1;
                    BindFormsMasterGrid();
                }
            }
        }

        protected void GV_FormsMaster_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            // Cancel editing and revert the GridView back to normal view
            GV_FormsMaster.EditIndex = -1;
            BindFormsMasterGrid();
        }

        private void DepartmentBinder()
        {
            // Construct the SQL query with parameters
            string query = "SELECT Department_Description, Department_ID FROM MST_Department ";
            string textField = "Department_Description"; // Assuming this is the correct field for displaying in the DropDownList
            string valueField = "Department_ID"; // Assuming this is the correct field for storing in the DropDownList


            bool recordsBound;

            DatabaseHelper.BindDropDownList(query, DDL_Department, textField, valueField, out recordsBound);

            // Check if any records were bound
            if (!recordsBound)
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_Department);
                string DDL_Department_Error_script = @"<script type='text/javascript'>
                    new PNotify({
                        title: 'Error',
                        text: 'An error occurred!',
                        type: 'error',
                        styling: 'bootstrap3'
                    });
                </script>";

                // RegisterStartupScript adds the JavaScript code to the page
                ClientScript.RegisterStartupScript(this.GetType(), "ShowDepartmentBinderErrorNotification", DDL_Department_Error_script, false);
            }
        }
        protected void DDL_Department_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_Department.SelectedIndex != 0)
            {
                string selectedDepartmentValue = DDL_Department.SelectedValue.ToString();
                SubDepartmentBinder(selectedDepartmentValue);
            }
            else
            {
                DatabaseHelper.BindWithDefaultNoRecords(DDL_SubDepartment);

                string DDL_Department_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'Invalid Selection!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowDepartmentInvalidErrorNotification", DDL_Department_Error_script, false);
            }
        }

        private void SubDepartmentBinder(string selectedDepartmentValue)
        {
            // Construct the SQL query with parameters
            string query = "SELECT  SubDepartment_Description, SubDepartment_ID FROM MST_SubDepartment ";
            string textField = "SubDepartment_Description"; // Assuming this is the correct field for displaying in the DropDownList
            string valueField = "SubDepartment_ID"; // Assuming this is the correct field for storing in the DropDownList

            // Create SQL parameters for plant_id and line_id
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@DepartmentID", selectedDepartmentValue)
            };

            // Call the BindDropDownList method with parameters
            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_SubDepartment, textField, valueField, parameters, out recordsBound);

            // Check if any records were bound
            if (!recordsBound)
            {
                string SubDepartment_Error_script = @"<script type='text/javascript'>
                    new PNotify({
                        title: 'Error',
                        text: 'Invalid Selection!',
                        type: 'error',
                        styling: 'bootstrap3'
                    });
                </script>";

                // RegisterStartupScript adds the JavaScript code to the page
                ClientScript.RegisterStartupScript(this.GetType(), "ShowSubDepartmentBinderErrorNotification", SubDepartment_Error_script, false);
            }
        }
        protected void DDL_SubDepartment_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DDL_SubDepartment.SelectedIndex != 0)
            {
                string selectedDepartmentValue = DDL_Department.SelectedValue.ToString();
                string selectedSubDepartmentValue = DDL_SubDepartment.SelectedValue.ToString();
            }
            else
            {
                string DDL_SubDepartment_Error_script = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Error',
                                text: 'Invalid Selection!',
                                type: 'error',
                                styling: 'bootstrap3'
                            });
                        </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowSubDepartmentInvalidErrorNotification", DDL_SubDepartment_Error_script, false);
            }
        }


        protected void BtnSubmit_Click(object sender, EventArgs e)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            string formName = TB_FormName.Text;
            string documentNumber = TB_DocNo.Text;
            string documentName = TB_DocName.Text;
            DateTime issueDate = DateTime.Parse(TB_IssueDate.Text).Date;
            string issueNo = TB_IssueNo.Text;
            DateTime revisionDate = DateTime.Parse(TB_RevisionDate.Text).Date;
            string revisionNo = TB_RevisionNo.Text;
            int frequency = Convert.ToInt32(TB_Frequency.Text);
            string selectedDepartmentValue = DDL_Department.SelectedValue.ToString();
            string selectedSubDepartmentValue = DDL_SubDepartment.SelectedValue.ToString();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "INSERT INTO MST_FormsMaster (FormName, DocumentNumber, DocumentName, IssueDate, IssueNo, RevisionDate, RevNo, Frequency) " +

                                    "VALUES (@FormName ,@DocumentNumber, @DocumentName, @IssueDate, @IssueNo, @RevisionDate, @RevNo, @Frequency)";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {

                        // Add parameters
                        command.Parameters.AddWithValue("@FormName", (object)formName ?? DBNull.Value);
                        command.Parameters.AddWithValue("@DocumentNumber", (object)documentNumber ?? DBNull.Value);
                        command.Parameters.AddWithValue("@DocumentName", (object)documentName ?? DBNull.Value);
                        command.Parameters.AddWithValue("@IssueDate", (object)issueDate ?? DBNull.Value);
                        command.Parameters.AddWithValue("@IssueNo", (object)issueNo ?? DBNull.Value);
                        command.Parameters.AddWithValue("@RevisionDate", (object)revisionDate ?? DBNull.Value);
                        command.Parameters.AddWithValue("@RevNo", (object)revisionNo ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Frequency", (object)frequency ?? DBNull.Value);


                        // Execute the query
                        command.ExecuteNonQuery();
                        MakeInputsReadOnly();
                    }
                    connection.Close();
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

        private void MakeInputsReadOnly()
        {
            TB_FormName.ReadOnly = true;
            TB_DocNo.ReadOnly = true;
            TB_DocName.ReadOnly = true;
            TB_IssueDate.ReadOnly = true;   
            TB_IssueNo.ReadOnly = true;
            TB_RevisionDate.ReadOnly = true;
            TB_RevisionNo.ReadOnly = true;
            TB_Frequency.ReadOnly = true;
            DDL_Department.Enabled = false;
            DDL_SubDepartment.Enabled = false;

            BtnSubmit.Enabled = false;
            BtnSubmit.Text = "SAVED";
            BtnSubmit.CssClass = "btn btn-sm btn-success";

            string Data_SuccessScript = @"<script type='text/javascript'>
                            new PNotify({
                                title: 'Data Success',
                                text: 'Recorded Successfully!!',
                                type: 'success',
                                styling: 'bootstrap3'
                            });
                        </script>";

            // RegisterStartupScript adds the JavaScript code to the page
            ClientScript.RegisterStartupScript(this.GetType(), "ShowDataSuccessNotification", Data_SuccessScript, false);
        }

        protected void BtnReset_Click(object sender, EventArgs e)
        {
            Response.Redirect("Form_Master.aspx");
        }
    }
}