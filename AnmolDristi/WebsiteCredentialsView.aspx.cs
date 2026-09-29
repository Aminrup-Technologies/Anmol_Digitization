using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
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

        protected void GridViewFiltered_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                // Get current page index and page size
                int index = e.Row.RowIndex + (GridViewFiltered.PageIndex * GridViewFiltered.PageSize);
                Label lblSerial = (Label)e.Row.FindControl("lblSerial");
                lblSerial.Text = (index + 1).ToString(); // SL No starts from 1
            }
        }

        protected void GridViewFiltered_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridViewFiltered.PageIndex = e.NewPageIndex;

            // Call your bind method again (e.g., BindGrid or LoadData)
            BindGrid(); // Replace with your actual method
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

                        // Retrieve the dropdown text for OfficeLocation and Department
                        string officeLocationText = GetDropDownText(DDL_Branch, reader["OfficeLocation"].ToString());
                        string departmentText = GetDropDownText(DDL_Department, reader["Department"].ToString());

                        string detailsHtml = $@"
                            <b>Office Location:</b> {officeLocationText}<br />
                            <b>Department:</b> {departmentText}<br /><br />
                            <b>Site Name:</b> {reader["SiteName"]}<br />
                            <b>Site URL:</b> {reader["SiteURL"]}<br />
                            <b>Purpose:</b> {reader["Purpose"]}<br /><br />
                            

                            <b>Frequency:</b> {reader["UsageFrequency"]}<br />
                            <b>Login ID:</b> {reader["LoginId"]}<br />
                            <b><span class='text-danger'>Password:</span></b> <span class='font-weight-bold'>{decryptedPassword}</span><br /><br />
                            <b>Registered Email-id:</b> {reader["RegisteredEmail"]}<br />
                            <b>Registered Person Name for Email OTP:</b> {reader["EmailOwnerName"]}<br />
                            <b>Registered Mobile:</b> {reader["RegisteredMobile"]}<br />
                            <b>Registered Person Name for Mobile OTP:</b> {reader["MobileOwnerName"]}<br />
                            <b>OTP Required:</b> {(Convert.ToBoolean(reader["IsOTPRequired"]) ? "Yes" : "No")}<br />
                            <b>OTP Type:</b> {reader["OTPType"]}<br /><br />
                            
                            <b>Notes:</b> {reader["Notes"]}<br /><br />
                            <b>Creation Date:</b> {Convert.ToDateTime(reader["CreatedDate"]).ToString("dd-MMM-yyyy hh:mm:ss tt")}<br />
                            <b>Last Updated:</b> {Convert.ToDateTime(reader["LastUpdated"]).ToString("dd-MMM-yyyy hh:mm:ss tt")}<br />
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

        //protected void btnExport_Click(object sender, EventArgs e)
        //{
        //    string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        //    using (SqlConnection conn = new SqlConnection(connStr))
        //    {
        //        string query = @"SELECT * FROM WebsiteCredentials WHERE IsDeleted = 0 ORDER BY CreatedDate DESC";

        //        using (SqlCommand cmd = new SqlCommand(query, conn))
        //        {
        //            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
        //            {
        //                DataTable dt = new DataTable();
        //                da.Fill(dt);

        //                if (dt.Rows.Count > 0)
        //                {
        //                    // Prepare GridView for export
        //                    GridView gvExport = new GridView();
        //                    gvExport.DataSource = dt;
        //                    gvExport.DataBind();

        //                    // Export logic
        //                    Response.Clear();
        //                    Response.Buffer = true;
        //                    Response.AddHeader("content-disposition", "attachment;filename=WebsiteCredentials.xls");
        //                    Response.Charset = "";
        //                    Response.ContentType = "application/vnd.ms-excel";

        //                    using (System.IO.StringWriter sw = new System.IO.StringWriter())
        //                    {
        //                        HtmlTextWriter hw = new HtmlTextWriter(sw);
        //                        gvExport.RenderControl(hw);
        //                        Response.Output.Write(sw.ToString());
        //                        Response.Flush();
        //                        Response.End();
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        //// Required override
        //public override void VerifyRenderingInServerForm(Control control)
        //{
        //    // Required for GridView export
        //}


        //protected void btnExport_Click(object sender, EventArgs e)
        //{
        //    string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        //    using (SqlConnection conn = new SqlConnection(connStr))
        //    {
        //        string query = @"SELECT * FROM WebsiteCredentials WHERE IsDeleted = 0 ORDER BY CreatedDate DESC";

        //        using (SqlCommand cmd = new SqlCommand(query, conn))
        //        {
        //            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
        //            {
        //                DataTable dt = new DataTable();
        //                da.Fill(dt);

        //                if (dt.Rows.Count > 0)
        //                {
        //                    using (OfficeOpenXml.ExcelPackage pck = new OfficeOpenXml.ExcelPackage())
        //                    {
        //                        var ws = pck.Workbook.Worksheets.Add("WebsiteCredentials");
        //                        ws.Cells["A1"].LoadFromDataTable(dt, true);

        //                        // Optional: AutoFit columns
        //                        ws.Cells[ws.Dimension.Address].AutoFitColumns();

        //                        // Export as .xlsx
        //                        Response.Clear();
        //                        Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        //                        Response.AddHeader("content-disposition", "attachment; filename=WebsiteCredentials.xlsx");
        //                        Response.BinaryWrite(pck.GetAsByteArray());
        //                        Response.End();
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        //protected void btnExport_Click(object sender, EventArgs e)
        //{
        //    string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        //    using (SqlConnection conn = new SqlConnection(connStr))
        //    {
        //        string query = @"SELECT SiteName, SiteURL, Purpose, UsageFrequency, LoginId, EncryptedPassword,
        //                        RegisteredMobile, MobileOwnerName, RegisteredEmail, EmailOwnerName,
        //                        IsOTPRequired, OTPType, OfficeLocation, Department, Notes, CreatedDate
        //                 FROM WebsiteCredentials
        //                 WHERE IsDeleted = 0
        //                 ORDER BY CreatedDate DESC";

        //        using (SqlCommand cmd = new SqlCommand(query, conn))
        //        {
        //            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
        //            {
        //                DataTable dt = new DataTable();
        //                da.Fill(dt);

        //                // Add DecryptedPassword column
        //                dt.Columns.Add("DecryptedPassword", typeof(string));

        //                TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;

        //                foreach (DataRow row in dt.Rows)
        //                {
        //                    // Decrypt password
        //                    if (row["EncryptedPassword"] != DBNull.Value)
        //                    {
        //                        byte[] encrypted = (byte[])row["EncryptedPassword"];
        //                        row["DecryptedPassword"] = AesEncryptionHelper.Decrypt(encrypted);
        //                    }

        //                    // Optional: Sentence/title case certain fields
        //                    if (row["Purpose"] != DBNull.Value)
        //                        row["Purpose"] = textInfo.ToTitleCase(row["Purpose"].ToString().ToLower());

        //                    if (row["UsageFrequency"] != DBNull.Value)
        //                        row["UsageFrequency"] = textInfo.ToTitleCase(row["UsageFrequency"].ToString().ToLower());
        //                }

        //                // Remove encrypted password column
        //                dt.Columns.Remove("EncryptedPassword");

        //                // Export to Excel
        //                using (OfficeOpenXml.ExcelPackage pck = new OfficeOpenXml.ExcelPackage())
        //                {
        //                    var ws = pck.Workbook.Worksheets.Add("WebsiteCredentials");
        //                    ws.Cells["A1"].LoadFromDataTable(dt, true);

        //                    // Format CreatedDate column
        //                    int createdDateCol = dt.Columns.IndexOf("CreatedDate") + 1;
        //                    if (createdDateCol > 0)
        //                    {
        //                        ws.Column(createdDateCol).Style.Numberformat.Format = "dd-MMM-yyyy";
        //                    }

        //                    // Header formatting
        //                    for (int col = 1; col <= dt.Columns.Count; col++)
        //                    {
        //                        string originalHeader = dt.Columns[col - 1].ColumnName;
        //                        string formattedHeader = textInfo.ToTitleCase(originalHeader.Replace("_", " ").ToLower());

        //                        ws.Cells[1, col].Value = formattedHeader;
        //                        ws.Cells[1, col].Style.Font.Bold = true;
        //                        ws.Cells[1, col].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        //                        ws.Cells[1, col].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
        //                        ws.Column(col).AutoFit();
        //                    }

        //                    // Row styling
        //                    using (var range = ws.Cells[2, 1, dt.Rows.Count + 1, dt.Columns.Count])
        //                    {
        //                        range.Style.Font.Size = 11;
        //                        range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
        //                        range.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
        //                    }

        //                    // Output the file
        //                    Response.Clear();
        //                    Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        //                    Response.AddHeader("content-disposition", "attachment; filename=WebsiteCredentials.xlsx");
        //                    Response.BinaryWrite(pck.GetAsByteArray());
        //                    Response.End();
        //                }
        //            }
        //        }
        //    }
        //}

        protected void btnExport_Click(object sender, EventArgs e)
        {
            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = @"
            SELECT 
                wc.[Id] as RecordId,
                wc.[SiteName] as Website_Name,
                wc.[SiteURL] as Website_URL,
                wc.[Purpose],
                wc.[MobileOwnerName],
                wc.[EmailOwnerName],
                wc.[OTPType],
                br.[Branch_Description] AS [OfficeLocation_Description],
                dp.[Department_Description] AS [Department_Description],
                wc.[UsageFrequency],
                wc.[LoginId],
                wc.[EncryptedPassword],
                wc.[RegisteredMobile],
                wc.[RegisteredEmail],
                wc.[IsOTPRequired],
                wc.[Notes],
                wc.[CreatedDate],
                wc.[LastUpdated],
                wc.[AddedByName]              
            FROM [AnmolDristi].[dbo].[WebsiteCredentials] wc
            LEFT JOIN [AnmolDristi].[dbo].[MST_Branch] br 
                ON wc.[OfficeLocation] = br.[Branch_ID]
            LEFT JOIN [AnmolDristi].[dbo].[MST_Department] dp 
                ON wc.[Department] = dp.[Department_ID]
            WHERE wc.IsDeleted = 0
                ORDER BY wc.CreatedDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        // Add decrypted password column
                        dt.Columns.Add("DecryptedPassword", typeof(string));

                        TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;

                        foreach (DataRow row in dt.Rows)
                        {
                            // Decrypt EncryptedPassword if not null
                            if (row["EncryptedPassword"] != DBNull.Value)
                            {
                                byte[] encrypted = (byte[])row["EncryptedPassword"];
                                row["DecryptedPassword"] = AesEncryptionHelper.Decrypt(encrypted);
                            }
                        }

                        dt.Columns.Remove("EncryptedPassword"); // Remove original encrypted column

                        using (OfficeOpenXml.ExcelPackage pck = new OfficeOpenXml.ExcelPackage())
                        {
                            var ws = pck.Workbook.Worksheets.Add("WebsiteCredentials");
                            ws.Cells["A1"].LoadFromDataTable(dt, true);

                            // Format headers as Title Case
                            for (int col = 1; col <= dt.Columns.Count; col++)
                            {
                                string originalHeader = dt.Columns[col - 1].ColumnName;
                                string formattedHeader = textInfo.ToTitleCase(originalHeader.Replace("_", " ").ToLower());
                                ws.Cells[1, col].Value = formattedHeader;

                                ws.Cells[1, col].Style.Font.Bold = true;
                                ws.Cells[1, col].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                ws.Cells[1, col].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                                ws.Column(col).AutoFit();
                            }

                            // Apply date formatting to all known date columns
                            string[] dateColumns = { "CreatedDate", "LastUpdated", "DeletedDate" };
                            foreach (string dateCol in dateColumns)
                            {
                                int colIndex = dt.Columns.IndexOf(dateCol) + 1;
                                if (colIndex > 0)
                                    ws.Column(colIndex).Style.Numberformat.Format = "dd-MMM-yyyy HH:mm";
                            }

                            // General row styling
                            using (var range = ws.Cells[2, 1, dt.Rows.Count + 1, dt.Columns.Count])
                            {
                                range.Style.Font.Size = 11;
                                range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
                                range.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                            }

                            // Send file to browser
                            Response.Clear();
                            Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                            Response.AddHeader("content-disposition", "attachment; filename=WebsiteCredentials.xlsx");
                            Response.BinaryWrite(pck.GetAsByteArray());
                            Response.End();
                        }
                    }
                }
            }
        }

    }
}