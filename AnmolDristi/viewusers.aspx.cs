using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;

namespace AnmolDristi
{
    public partial class viewusers : System.Web.UI.Page
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
                    BindEmployeeData();
                }
            }
        }

        private void BindEmployeeData()
        {
            string query = "SELECT EmployeeCode, EmployeeName, EmployeeStatus FROM MST_UserMaster order by Id desc";
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    gvEmployees.DataSource = dt;
                    gvEmployees.DataBind();
                }
            }
        }

        protected void gvEmployees_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            string employeeCode = e.CommandArgument.ToString();

            if (e.CommandName == "EditEmployee")
            {
                // Redirect to the details page for editing
                Response.Redirect($"EditEmployee.aspx?EmployeeCode={employeeCode}",false);
            }
            else if (e.CommandName == "ToggleStatus")
            {
                ToggleEmployeeStatus(employeeCode);
                BindEmployeeData(); // Refresh the grid to show updated status
            }
        }

        private void ToggleEmployeeStatus(string employeeCode)
        {
            string query = "UPDATE MST_UserMaster SET EmployeeStatus = CASE WHEN EmployeeStatus = 'Active' THEN 'Blocked' ELSE 'Active' END WHERE EmployeeCode = @EmployeeCode";
            string connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeCode", employeeCode);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }


    }
}