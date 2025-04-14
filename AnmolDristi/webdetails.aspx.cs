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
    public partial class webdetails : System.Web.UI.Page
    {
        private static readonly string EncryptionKey = "Your$uper$ecureKey123!"; // Replace with strong key (save in config)
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
                    BindGrid();
                }
            }
        }

        private void BindGrid()
        {
            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            string currentUserCode = Session["WORKMAN"]?.ToString();
            string currentUserRole = Session["WORKMAN"]?.ToString();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = @"SELECT Id, SiteName, SiteURL, Purpose, UsageFrequency, LoginId, RegisteredMobile, RegisteredEmail, IsOTPRequired FROM WebsiteCredentials WHERE IsDeleted = 0";

                if (currentUserRole != "Admin")
                {
                    query += " AND AddedByCode = @AddedByCode";
                }

                query += " ORDER BY CreatedDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (currentUserRole != "D006" || currentUserRole != "ADMIN")
                    {
                        cmd.Parameters.AddWithValue("@AddedByCode", currentUserCode);
                    }

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        GridViewCredentials.DataSource = dt;
                        GridViewCredentials.DataBind();
                    }
                }
            }
        }


        private void BindGridNew()
        {
            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            string currentUserCode = Session["USERID"]?.ToString(); // Or however you're storing it

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = @"SELECT Id, SiteName, SiteURL, Purpose, UsageFrequency, LoginId, RegisteredMobile, RegisteredEmail, IsOTPRequired FROM WebsiteCredentials WHERE IsDeleted = 0 AND AddedByCode = @AddedByCode 
            ORDER BY CreatedDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@AddedByCode", currentUserCode);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        GridViewCredentials.DataSource = dt;
                        GridViewCredentials.DataBind();
                    }
                }
            }
        }

        private void BindGridOld()
        {
            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = @"SELECT Id, SiteName, SiteURL, Purpose, UsageFrequency, LoginId, RegisteredMobile, RegisteredEmail, IsOTPRequired FROM WebsiteCredentials WHERE IsDeleted = 0 ORDER BY CreatedDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        GridViewCredentials.DataSource = dt;
                        GridViewCredentials.DataBind();
                    }
                }
            }
        }

        protected void GridViewCredentials_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridViewCredentials.PageIndex = e.NewPageIndex;
            BindGrid();
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            if (ViewState["EditID"] != null)
            {
                UpdateCredential(Convert.ToInt32(ViewState["EditID"]));
            }
            else
            {
                InsertCredential();
            }
            BindGrid();
            ClearForm();
            ViewState["EditID"] = null;
        }

        private void InsertCredential()
        {
            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
            string siteName = txtSiteName.Text.Trim();
            string siteURL = txtSiteURL.Text.Trim();
            string purpose = txtPurpose.Text.Trim();
            string frequency = ddlFrequency.SelectedValue;
            string loginId = txtLoginID.Text.Trim();
            string plainPassword = txtPassword.Text.Trim();
            string registeredMobile = txtMobile.Text.Trim();
            string registeredEmail = txtEmail.Text.Trim();
            bool isOTPRequired = chkOTPRequired.Checked;
            string notes = txtNotes.Text.Trim();

            // Encryption
            //string encryptionKey = "YourStaticKeyOrKeyRef"; // Replace with your actual reference or logic
            byte[] encryptedPassword = AesEncryptionHelper.Encrypt(plainPassword);

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = @"INSERT INTO WebsiteCredentials (SiteName, SiteURL, Purpose, UsageFrequency, LoginId, EncryptedPassword, EncryptionKeyRef, RegisteredMobile, RegisteredEmail, IsOTPRequired, Notes, AddedByCode, AddedByName) VALUES (@SiteName, @SiteURL, @Purpose, @Frequency, @LoginId, @EncryptedPassword, @KeyRef, @Mobile, @Email, @IsOTP, @Notes, @AddedByCode, @AddedByName)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SiteName", siteName);
                    cmd.Parameters.AddWithValue("@SiteURL", siteURL);
                    cmd.Parameters.AddWithValue("@Purpose", purpose);
                    cmd.Parameters.AddWithValue("@Frequency", frequency);
                    cmd.Parameters.AddWithValue("@LoginId", loginId);
                    cmd.Parameters.AddWithValue("@EncryptedPassword", encryptedPassword);
                    cmd.Parameters.AddWithValue("@KeyRef", EncryptionKey);
                    cmd.Parameters.AddWithValue("@Mobile", registeredMobile);
                    cmd.Parameters.AddWithValue("@Email", registeredEmail);
                    cmd.Parameters.AddWithValue("@IsOTP", isOTPRequired);
                    cmd.Parameters.AddWithValue("@Notes", notes);
                    cmd.Parameters.AddWithValue("@AddedByCode", Session["WORKMAN"].ToString());
                    cmd.Parameters.AddWithValue("@AddedByName", Session["USERNAME"].ToString());

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    string script = "new PNotify({ title: 'Success', text: 'Record saved successfully!', type: 'success', styling: 'bootstrap3' });";
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "NotifySuccess", script, true);
                }
            }
            BindGrid();
            ClearForm();
        }

        private void ClearForm()
        {
            btnSubmit.Text = "Submit";
            txtSiteName.Text = string.Empty;
            txtSiteURL.Text = string.Empty;
            txtPurpose.Text = string.Empty;
            txtLoginID.Text = string.Empty;
            txtPassword.Text = string.Empty;
            txtMobile.Text = string.Empty;
            txtEmail.Text = string.Empty;
            txtNotes.Text = string.Empty;
            ddlFrequency.SelectedIndex = 0;
            chkOTPRequired.Checked = false;

            Page.Validate("Submit");
        }

        protected void GridViewCredentials_RowEditing(object sender, GridViewEditEventArgs e)
        {
            GridViewCredentials.EditIndex = e.NewEditIndex;
            BindGrid();

            int id = Convert.ToInt32(GridViewCredentials.DataKeys[e.NewEditIndex].Value);

            LoadCredentialForEdit(id);
            btnSubmit.Text = "Update";

            // Optional: highlight the editing panel
            //divCredentialForm.Attributes["class"] = "card p-3 border border-warning shadow-sm bg-warning bg-opacity-10";
            //lblMessage.Text = "Editing Mode";
            //lblMessage.CssClass = "text-warning fw-bold";
        }

        private void LoadCredentialForEdit(int id)
        {
            string query = "SELECT * FROM WebsiteCredentials WHERE Id = @Id";
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    // Fill the fields
                    txtSiteName.Text = reader["SiteName"].ToString();
                    txtSiteURL.Text = reader["SiteURL"].ToString();
                    txtPurpose.Text = reader["Purpose"].ToString();
                    ddlFrequency.SelectedValue = reader["UsageFrequency"].ToString();
                    txtLoginID.Text = reader["LoginId"].ToString();

                    // Decrypt password
                    byte[] encryptedPassword = (byte[])reader["EncryptedPassword"];
                    string password = AesEncryptionHelper.Decrypt(encryptedPassword);
                    txtPassword.Text = password;

                    txtMobile.Text = reader["RegisteredMobile"].ToString();
                    txtEmail.Text = reader["RegisteredEmail"].ToString();
                    chkOTPRequired.Checked = Convert.ToBoolean(reader["IsOTPRequired"]);
                    txtNotes.Text = reader["Notes"].ToString();

                    // Save the ID for update
                    ViewState["EditID"] = id;
                }
            }
        }

        private void UpdateCredential(int id)
        {
            byte[] encryptedPassword = AesEncryptionHelper.Encrypt(txtPassword.Text);

            string query = @"UPDATE WebsiteCredentials SET SiteName = @SiteName, SiteURL = @SiteURL, Purpose = @Purpose, UsageFrequency = @UsageFrequency, LoginId = @LoginId, EncryptedPassword = @EncryptedPassword, RegisteredMobile = @RegisteredMobile, RegisteredEmail = @RegisteredEmail, IsOTPRequired = @IsOTPRequired, Notes = @Notes, LastUpdated = GETDATE() WHERE Id = @Id";

            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@SiteName", txtSiteName.Text);
                cmd.Parameters.AddWithValue("@SiteURL", txtSiteURL.Text);
                cmd.Parameters.AddWithValue("@Purpose", txtPurpose.Text);
                cmd.Parameters.AddWithValue("@UsageFrequency", ddlFrequency.SelectedValue);
                cmd.Parameters.AddWithValue("@LoginId", txtLoginID.Text);
                cmd.Parameters.AddWithValue("@EncryptedPassword", encryptedPassword);
                cmd.Parameters.AddWithValue("@RegisteredMobile", txtMobile.Text);
                cmd.Parameters.AddWithValue("@RegisteredEmail", txtEmail.Text);
                cmd.Parameters.AddWithValue("@IsOTPRequired", chkOTPRequired.Checked);
                cmd.Parameters.AddWithValue("@Notes", txtNotes.Text);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@ModifiedByCode", Session["WORKMAN"].ToString());
                cmd.Parameters.AddWithValue("@ModifiedByName", Session["USERNAME"].ToString());

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        protected void btnReset_Click(object sender, EventArgs e)
        {
            ClearForm();
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            // Reset form
            ClearForm();

            // Cancel any edit mode in GridView
            GridViewCredentials.EditIndex = -1;
            BindGrid(); // rebind to reflect cancellation

            // Optional UI reset
            //divCredentialForm.Attributes["class"] = "card p-3 border shadow-sm";
            //lblMessage.Text = "";
            //lblMessage.CssClass = "";
        }

        protected void GridViewCredentials_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "EditRecord")
            {
                int id = Convert.ToInt32(e.CommandArgument);
                LoadCredentialForEdit(id);

                btnSubmit.Text = "Update";

                // Highlight UI panel for edit
                //pnlForm.Attributes["class"] += " border border-primary rounded p-3 shadow-sm"; // Optional UI boost
            }
        }



        protected void GridViewCredentials_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            int id = Convert.ToInt32(GridViewCredentials.DataKeys[e.RowIndex].Value);

            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("UPDATE WebsiteCredentials SET IsDeleted = 1, DeletedByCode=@DeletedByCode, DeletedByName=@DeletedByName, LastUpdated = GETDATE(), DeletedDate = GETDATE() WHERE Id = @Id", con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@DeletedByCode", Session["WORKMAN"].ToString());
                    cmd.Parameters.AddWithValue("@DeletedByName", Session["USERNAME"].ToString());
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            BindGrid();

            ScriptManager.RegisterStartupScript(this, GetType(), "notify", @"
            new PNotify({
                title: 'Deleted',
                text: 'The record has been deleted successfully.',
                type: 'info',
                styling: 'bootstrap3'
            });", true);
        }

    }
}