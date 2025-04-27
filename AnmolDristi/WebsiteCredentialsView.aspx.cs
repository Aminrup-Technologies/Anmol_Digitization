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
    public partial class WebsiteCredentialsView : System.Web.UI.Page
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
                    DepartmentBinder();
                    BranchBinder();

                    BindGrid();
                }
            }
        }

        private void DepartmentBinder()
        {
            string query = "SELECT Department_Description, Department_ID FROM MST_Department";
            string textField = "Department_Description";
            string valueField = "Department_ID";

            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_Department, textField, valueField, out recordsBound);

            if (!recordsBound)
            {
                string DDL_Department_Error_script = @"<script type='text/javascript'>
                    new PNotify({
                        title: 'Error',
                        text: 'Invalid Selection!',
                        type: 'error',
                        styling: 'bootstrap3'
                    });
                </script>";
                ClientScript.RegisterStartupScript(this.GetType(), "ShowDepartmentBinderBinderErrorNotification", DDL_Department_Error_script, false);
            }
        }

        private void BranchBinder()
        {
            string query = "SELECT Branch_Description, Branch_ID FROM MST_Branch";
            string textField = "Branch_Description";
            string valueField = "Branch_ID";

            bool recordsBound;
            DatabaseHelper.BindDropDownList(query, DDL_Branch, textField, valueField, out recordsBound);

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

                ClientScript.RegisterStartupScript(this.GetType(), "ShowLineProductsBinderErrorNotification", PN_Error_script, false);
            }
        }

        private void BindGrid()
        {
            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = @"SELECT * FROM WebsiteCredentials WHERE IsDeleted = 0 ORDER BY CreatedDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        GridViewFiltered.DataSource = dt;
                        GridViewFiltered.DataBind();
                    }
                }
            }
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = @"SELECT Id, SiteName, Purpose, OfficeLocation, Department, UsageFrequency, AddedByCode, AddedByName 
                         FROM WebsiteCredentials WHERE IsDeleted = 0";

                List<string> conditions = new List<string>();

                // Collect conditions based on input values
                if (!string.IsNullOrWhiteSpace(txtAddedByCode.Text))
                    conditions.Add("AddedByCode LIKE @AddedByCode");

                if (!string.IsNullOrWhiteSpace(txtAddedByName.Text))
                    conditions.Add("AddedByName LIKE @AddedByName");

                if (!string.IsNullOrWhiteSpace(DDL_Branch.SelectedValue) && DDL_Branch.SelectedValue != "0")
                    conditions.Add("OfficeLocation = @OfficeLocation");

                if (!string.IsNullOrWhiteSpace(DDL_Department.SelectedValue) && DDL_Department.SelectedValue != "0")
                    conditions.Add("Department = @Department");

                if (!string.IsNullOrWhiteSpace(txtSearchSiteName.Text))
                    conditions.Add("SiteName LIKE @SiteName");

                // Add conditions to the query if there are any
                if (conditions.Any())
                    query += " AND " + string.Join(" AND ", conditions);

                // Output the final query string
                //string finalQuery = query;
                //Console.WriteLine("Final Query: " + finalQuery);

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    // Add parameters for the conditions to the command
                    if (!string.IsNullOrWhiteSpace(txtAddedByCode.Text))
                        cmd.Parameters.AddWithValue("@AddedByCode", "%" + txtAddedByCode.Text.Trim() + "%");

                    if (!string.IsNullOrWhiteSpace(txtAddedByName.Text))
                        cmd.Parameters.AddWithValue("@AddedByName", "%" + txtAddedByName.Text.Trim() + "%");

                    if (!string.IsNullOrWhiteSpace(DDL_Branch.SelectedValue))
                        cmd.Parameters.AddWithValue("@OfficeLocation", DDL_Branch.SelectedValue);

                    if (!string.IsNullOrWhiteSpace(DDL_Department.SelectedValue))
                        cmd.Parameters.AddWithValue("@Department", DDL_Department.SelectedValue);

                    if (!string.IsNullOrWhiteSpace(txtSearchSiteName.Text))
                        cmd.Parameters.AddWithValue("@SiteName", "%" + txtSearchSiteName.Text.Trim() + "%");

                    // Execute the query and bind the result to GridView
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        // Clear existing GridView data (optional but good practice)
                        GridViewFiltered.DataSource = null;
                        GridViewFiltered.DataBind();

                        // Bind new results to the GridView
                        GridViewFiltered.DataSource = dt;
                        GridViewFiltered.DataBind();
                    }
                }
            }
        }


        protected void GridViewFiltered_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "ViewDetails")
            {
                int id = Convert.ToInt32(e.CommandArgument);
                ShowDetailsInModal(id);
            }
        }

        private void ShowDetailsInModal(int id)
        {
            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = @"SELECT * FROM WebsiteCredentials WHERE Id = @Id";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        string decryptedPassword = "";
                        try
                        {
                            decryptedPassword = AesEncryptionHelper.Decrypt((byte[])reader["EncryptedPassword"]);
                        }
                        catch
                        {
                            decryptedPassword = "[Unable to decrypt]";
                        }

                        //string detailsHtml = $@"
                        //    <b>Site Name:</b> {reader["SiteName"]}<br />
                        //    <b>Purpose:</b> {reader["Purpose"]}<br />
                        //    <b>Frequency:</b> {reader["UsageFrequency"]}<br />
                        //    <b>Login ID:</b> {reader["LoginId"]}<br />
                        //    <b><span class='text-danger'>Password:</span></b> <span class='font-weight-bold'>{decryptedPassword}</span><br />
                        //    <b>Email:</b> {reader["RegisteredEmail"]}<br />
                        //    <b>Mobile:</b> {reader["RegisteredMobile"]}<br />
                        //    <b>OTP Required:</b> {(Convert.ToBoolean(reader["IsOTPRequired"]) ? "Yes" : "No")}<br />
                        //    <b>Notes:</b> {reader["Notes"]}<br />
                        //";


                        // Retrieve the dropdown text for OfficeLocation and Department
                        string officeLocationText = GetDropDownText(DDL_Branch, reader["OfficeLocation"].ToString());
                        string departmentText = GetDropDownText(DDL_Department, reader["Department"].ToString());

                        string detailsHtml = $@"
                            <b>Site Name:</b> {reader["SiteName"]}<br />
                            <b>Site URL:</b> {reader["SiteURL"]}<br />
                            <b>Purpose:</b> {reader["Purpose"]}<br />
                            <b>Frequency:</b> {reader["UsageFrequency"]}<br />
                            <b>Login ID:</b> {reader["LoginId"]}<br />
                            <b><span class='text-danger'>Password:</span></b> <span class='font-weight-bold'>{decryptedPassword}</span><br />
                            <b>Email:</b> {reader["RegisteredEmail"]}<br />
                            <b>Email Owner Name:</b> {reader["EmailOwnerName"]}<br />
                            <b>Mobile:</b> {reader["RegisteredMobile"]}<br />
                            <b>Mobile Owner Name:</b> {reader["MobileOwnerName"]}<br />
                            <b>OTP Required:</b> {(Convert.ToBoolean(reader["IsOTPRequired"]) ? "Yes" : "No")}<br />
                            <b>OTP Type:</b> {reader["OTPType"]}<br />
                            <b>Office Location:</b> {officeLocationText}<br />
                            <b>Department:</b> {departmentText}<br />
                            <b>Notes:</b> {reader["Notes"]}<br /><br />
                            <b>Created Date:</b> {reader["CreatedDate"]}<br />
                            <b>Last Updated:</b> {reader["LastUpdated"]}<br />
                        ";

                        lblModalContent.Text = detailsHtml;
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "ShowPopup", "$('#detailsModal').modal('show');", true);
                    }
                }
            }
        }

        private string GetDropDownText(DropDownList ddl, string selectedValue)
        {
            foreach (ListItem item in ddl.Items)
            {
                if (item.Value == selectedValue)
                {
                    return item.Text;
                }
            }
            return "N/A"; // Return a default value if not found
        }

        protected void btnTestDecrypt_Click(object sender, EventArgs e)
        {
            string original = "TestPassword123!";
            byte[] encryptedBytes = AesEncryptionHelper.Encrypt(original);
            string hexString = BitConverter.ToString(encryptedBytes).Replace("-", "");
            string decrypted = AesEncryptionHelper.Decrypt(encryptedBytes);
            Response.Write($"<script>alert('Encrypted: {hexString}\\nDecrypted: {decrypted}');</script>");
        }

        private byte[] ConvertHexStringToBytes(string hex)
        {
            int numberChars = hex.Length;
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }



    }
}