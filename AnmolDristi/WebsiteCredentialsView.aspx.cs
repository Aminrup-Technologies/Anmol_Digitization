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
            if (Session["USERID"] == null || Session["USERNAME"] == null || Session["WORKMAN"] == null)
            {
                Response.Redirect("login.aspx");
            }
            else
            {
                BindGrid();
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
                string query = @"SELECT SiteName, Purpose, UsageFrequency, AddedByCode, AddedByName FROM WebsiteCredentials WHERE IsDeleted = 0";

                List<string> conditions = new List<string>();

                if (!string.IsNullOrWhiteSpace(txtAddedByCode.Text))
                    conditions.Add("AddedByCode LIKE @AddedByCode");

                if (!string.IsNullOrWhiteSpace(txtAddedByName.Text))
                    conditions.Add("AddedByName LIKE @AddedByName");

                if (!string.IsNullOrWhiteSpace(ddlWebsiteType.SelectedValue))
                    conditions.Add("Purpose = @WebsiteType");

                if (!string.IsNullOrWhiteSpace(txtSearchSiteName.Text))
                    conditions.Add("SiteName LIKE @SiteName");

                if (conditions.Any())
                    query += " AND " + string.Join(" AND ", conditions);

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (!string.IsNullOrWhiteSpace(txtAddedByCode.Text))
                        cmd.Parameters.AddWithValue("@AddedByCode", "%" + txtAddedByCode.Text.Trim() + "%");

                    if (!string.IsNullOrWhiteSpace(txtAddedByName.Text))
                        cmd.Parameters.AddWithValue("@AddedByName", "%" + txtAddedByName.Text.Trim() + "%");

                    if (!string.IsNullOrWhiteSpace(ddlWebsiteType.SelectedValue))
                        cmd.Parameters.AddWithValue("@WebsiteType", ddlWebsiteType.SelectedValue);

                    if (!string.IsNullOrWhiteSpace(txtSearchSiteName.Text))
                        cmd.Parameters.AddWithValue("@SiteName", "%" + txtSearchSiteName.Text.Trim() + "%");

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

                        string detailsHtml = $@"
                            <b>Site Name:</b> {reader["SiteName"]}<br />
                            <b>Purpose:</b> {reader["Purpose"]}<br />
                            <b>Frequency:</b> {reader["UsageFrequency"]}<br />
                            <b>Login ID:</b> {reader["LoginId"]}<br />
                            <b><span class='text-danger'>Password:</span></b> <span class='font-weight-bold'>{decryptedPassword}</span><br />
                            <b>Email:</b> {reader["RegisteredEmail"]}<br />
                            <b>Mobile:</b> {reader["RegisteredMobile"]}<br />
                            <b>OTP Required:</b> {(Convert.ToBoolean(reader["IsOTPRequired"]) ? "Yes" : "No")}<br />
                            <b>Notes:</b> {reader["Notes"]}<br />
                        ";

                        lblModalContent.Text = detailsHtml;
                        ScriptManager.RegisterStartupScript(this, this.GetType(), "ShowPopup", "$('#detailsModal').modal('show');", true);
                    }
                }
            }
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