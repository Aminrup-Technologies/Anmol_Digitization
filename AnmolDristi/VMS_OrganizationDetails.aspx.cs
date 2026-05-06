using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.UI.WebControls;
using System.Drawing;
using System.IO;
using QRCoder; // Requires QRCoder NuGet Package
using System.Web.UI;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Web;

namespace AnmolDristi
{
    public partial class VMS_OrganizationDetails : System.Web.UI.Page
    {
        string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindOfficesGrid();
            }
        }

        private void BindOfficesGrid()
        {
            // Join MST_Branch with VMS_OfficeSettings to show both configured and unconfigured offices
            string query = @"
                SELECT 
                    b.Branch_ID, 
                    b.Branch_Description, 
                    ISNULL(s.AliasName, 'Not Configured') AS AliasName, 
                    ISNULL(s.FullAddress, 'Not Configured') AS FullAddress
                FROM MST_Branch b
                LEFT JOIN VMS_OfficeSettings s ON b.Branch_ID = s.Branch_ID
                WHERE b.Branch_ID > 0
                ORDER BY b.Branch_Description ASC";

            using (SqlConnection con = new SqlConnection(connStr))
            {
                using (SqlDataAdapter sda = new SqlDataAdapter(query, con))
                {
                    DataTable dt = new DataTable();
                    sda.Fill(dt);
                    gvOffices.DataSource = dt;
                    gvOffices.DataBind();
                }
            }
        }

        protected void gvOffices_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "EditOffice")
            {
                // Existing Edit Logic...
                int branchId = Convert.ToInt32(e.CommandArgument);
                hfBranchID.Value = branchId.ToString();
                pnlList.Visible = false;
                pnlEdit.Visible = true;
                lblMsg.Text = "";
                LoadOfficeDetails(branchId);
            }
            else if (e.CommandName == "GenerateQR")
            {
                // Extract the three arguments we passed from the GridView
                string[] args = e.CommandArgument.ToString().Split('|');
                string branchId = args[0];
                string aliasName = args[1];
                string fullAddress = args[2];

                if (aliasName == "Not Configured")
                {
                    lblMsg.ForeColor = System.Drawing.Color.Red;
                    lblMsg.Text = "Please configure the Basic Details for this office before generating a QR code.";
                    return;
                }

                GenerateReceptionQRPdf(branchId, aliasName, fullAddress);
            }
        }

        private void GenerateReceptionQRPdf(string branchId, string aliasName, string fullAddress)
        {
            // 1. Generate the QR Code Image using QRCoder
            string baseUrl = "https://apps.anmolindustries.com/VMS_SelfLogin.aspx";
            string kioskUrl = $"{baseUrl}?BranchId={branchId}";

            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(kioskUrl, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);

            Bitmap qrBitmap = qrCode.GetGraphic(20);

            // 2. Initialize the PDF Document using iTextSharp (A4 Size, with margins)
            Document pdfDoc = new Document(PageSize.A4, 50, 50, 80, 50);
            MemoryStream memStream = new MemoryStream();
            PdfWriter.GetInstance(pdfDoc, memStream);
            pdfDoc.Open();

            // 3. Define Fonts
            iTextSharp.text.Font titleFont = FontFactory.GetFont("Arial", 36, iTextSharp.text.Font.BOLD);
            iTextSharp.text.Font subTitleFont = FontFactory.GetFont("Arial", 16, iTextSharp.text.Font.NORMAL);
            iTextSharp.text.Font companyFont = FontFactory.GetFont("Arial", 22, iTextSharp.text.Font.BOLD);
            iTextSharp.text.Font addressFont = FontFactory.GetFont("Arial", 12, iTextSharp.text.Font.NORMAL, BaseColor.DARK_GRAY);
            iTextSharp.text.Font footerFont = FontFactory.GetFont("Arial", 10, iTextSharp.text.Font.ITALIC, BaseColor.GRAY);

            // 4. Add "VISITORS" Title
            Paragraph title = new Paragraph("WELCOME VISITORS", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            title.SpacingAfter = 10;
            pdfDoc.Add(title);

            // 5. Add Subtitle
            Paragraph subTitle = new Paragraph("Scan the QR & generate your pass", subTitleFont);
            subTitle.Alignment = Element.ALIGN_CENTER;
            subTitle.SpacingAfter = 40;
            pdfDoc.Add(subTitle);

            // 6. Convert Bitmap to iTextSharp Image and add to PDF
            using (MemoryStream imgStream = new MemoryStream())
            {
                qrBitmap.Save(imgStream, System.Drawing.Imaging.ImageFormat.Png);
                iTextSharp.text.Image pdfImg = iTextSharp.text.Image.GetInstance(imgStream.ToArray());
                pdfImg.Alignment = Element.ALIGN_CENTER;
                // Scale the QR code to be large and scannable from a distance
                pdfImg.ScaleToFit(350f, 350f);
                pdfDoc.Add(pdfImg);
            }

            // Add spacing after QR Code
            pdfDoc.Add(new Paragraph("\n\n"));

            // 7. Add Company Name
            Paragraph company = new Paragraph(aliasName, companyFont);
            company.Alignment = Element.ALIGN_CENTER;
            company.SpacingAfter = 5;
            pdfDoc.Add(company);

            // 8. Add Address
            Paragraph address = new Paragraph(fullAddress, addressFont);
            address.Alignment = Element.ALIGN_CENTER;
            address.SpacingAfter = 50;
            pdfDoc.Add(address);

            // 9. Add Footer
            Paragraph footer = new Paragraph("Powered by Anmol-IT", footerFont);
            footer.Alignment = Element.ALIGN_CENTER;
            pdfDoc.Add(footer);

            // 10. Close Document and Stream to Browser for Download
            pdfDoc.Close();
            byte[] bytes = memStream.ToArray();
            memStream.Close();

            Response.Clear();
            Response.ContentType = "application/pdf";
            Response.AddHeader("content-disposition", $"attachment;filename=ReceptionQR_{aliasName.Replace(" ", "_")}.pdf");
            Response.Buffer = true;
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.BinaryWrite(bytes);
            Response.End();
        }

        private void GenerateReceptionQR(string branchId, string aliasName)
        {
            // 1. Define the base public URL of your VMS portal
            // IMPORTANT: Change this to your actual production domain
            string baseUrl = "https://vms.anmolindustries.com/VMS_SelfLogin.aspx";

            // 2. Append the specific Branch ID
            string kioskUrl = $"{baseUrl}?BranchId={branchId}";

            // 3. Generate the QR Code
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            // ECCLevel.Q allows 25% error correction, perfect for printed posters that might get smudged
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(kioskUrl, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);

            using (Bitmap bitMap = qrCode.GetGraphic(20))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    bitMap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    byte[] byteImage = ms.ToArray();

                    // 4. Bind to the UI
                    imgKioskQR.ImageUrl = "data:image/png;base64," + Convert.ToBase64String(byteImage);
                    lblModalPlantName.Text = aliasName;
                    lblModalBranchId.Text = branchId;

                    // 5. Trigger the Bootstrap Modal to open via JavaScript
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "Pop", "$('#qrModal').modal('show');", true);
                }
            }
        }

        private void LoadOfficeDetails(int branchId)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                // 1. Get the basic ERP name for the header
                string queryBranch = "SELECT Branch_Description FROM MST_Branch WHERE Branch_ID = @BranchID";
                using (SqlCommand cmd = new SqlCommand(queryBranch, con))
                {
                    cmd.Parameters.AddWithValue("@BranchID", branchId);
                    con.Open();
                    lblCurrentBranch.Text = cmd.ExecuteScalar()?.ToString();
                    con.Close();
                }

                // 2. Load existing VMS settings if they exist
                string querySettings = "SELECT * FROM VMS_OfficeSettings WHERE Branch_ID = @BranchID";
                using (SqlCommand cmd = new SqlCommand(querySettings, con))
                {
                    cmd.Parameters.AddWithValue("@BranchID", branchId);
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);

                        if (dt.Rows.Count > 0)
                        {
                            txtAlias.Text = dt.Rows[0]["AliasName"].ToString();
                            txtAddress.Text = dt.Rows[0]["FullAddress"].ToString();
                            txtPolicy.Text = dt.Rows[0]["VisitorPolicyText"].ToString();

                            if (ddlTimeZone.Items.FindByValue(dt.Rows[0]["TimeZone"].ToString()) != null)
                            {
                                ddlTimeZone.SelectedValue = dt.Rows[0]["TimeZone"].ToString();
                            }
                        }
                        else
                        {
                            // Clear form for new configuration
                            txtAlias.Text = "";
                            txtAddress.Text = "";
                            txtPolicy.Text = "1. Suit Up! Wear required Hair Net...\n2. No Smoking or Gutkha...\n3. Stay Alert! Be mindful of moving machinery...";
                        }
                    }
                }

                LoadOfficeTimings(branchId);
            }
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            int branchId = Convert.ToInt32(hfBranchID.Value);
            string empCode = Session["EmployeeCode"] != null ? Session["EmployeeCode"].ToString() : "Admin";

            string query = @"
                IF EXISTS (SELECT 1 FROM VMS_OfficeSettings WHERE Branch_ID = @BranchID)
                BEGIN
                    UPDATE VMS_OfficeSettings 
                    SET AliasName = @AliasName, FullAddress = @FullAddress, 
                        VisitorPolicyText = @Policy, TimeZone = @TimeZone, 
                        UpdatedAt = GETDATE(), UpdatedBy = @EmpCode
                    WHERE Branch_ID = @BranchID
                END
                ELSE
                BEGIN
                    INSERT INTO VMS_OfficeSettings (Branch_ID, AliasName, FullAddress, VisitorPolicyText, TimeZone, UpdatedBy)
                    VALUES (@BranchID, @AliasName, @FullAddress, @Policy, @TimeZone, @EmpCode)
                END";

            try
            {
                using (SqlConnection con = new SqlConnection(connStr))
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@BranchID", branchId);
                        cmd.Parameters.AddWithValue("@AliasName", txtAlias.Text.Trim());
                        cmd.Parameters.AddWithValue("@FullAddress", txtAddress.Text.Trim());
                        cmd.Parameters.AddWithValue("@Policy", txtPolicy.Text.Trim());
                        cmd.Parameters.AddWithValue("@TimeZone", ddlTimeZone.SelectedValue);
                        cmd.Parameters.AddWithValue("@EmpCode", empCode);

                        con.Open();
                        cmd.ExecuteNonQuery();
                        con.Close();
                    }
                }

                lblMsg.ForeColor = Color.Green;
                lblMsg.Text = "Details updated successfully!";

                // Refresh grid but stay on the edit screen to allow further tab editing
                BindOfficesGrid();
            }
            catch (Exception ex)
            {
                lblMsg.ForeColor = Color.Red;
                lblMsg.Text = "Error updating details: " + ex.Message;
            }
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            pnlEdit.Visible = false;
            pnlList.Visible = true;
        }

        private void LoadOfficeTimings(int branchId)
        {
            // 1. Create a dummy table to ensure all 7 days are always shown on the UI
            DataTable dtTimings = new DataTable();
            dtTimings.Columns.Add("DayOfWeek");
            dtTimings.Columns.Add("IsOpen");
            dtTimings.Columns.Add("OpenTime");
            dtTimings.Columns.Add("CloseTime");

            using (SqlConnection con = new SqlConnection(connStr))
            {
                string query = "SELECT DayOfWeek, OpenTime, CloseTime FROM VMS_PlantTimings WHERE Branch_ID = @BranchID";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@BranchID", branchId);
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dbData = new DataTable();
                        sda.Fill(dbData);

                        string[] days = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

                        foreach (string day in days)
                        {
                            // Look for existing timing in DB
                            DataRow[] foundRows = dbData.Select($"DayOfWeek = '{day}'");

                            if (foundRows.Length > 0)
                            {
                                // Record exists: Extract times (Handling NULLs if the office is closed that day)
                                string openT = foundRows[0]["OpenTime"] != DBNull.Value ? DateTime.Parse(foundRows[0]["OpenTime"].ToString()).ToString("HH:mm") : "";
                                string closeT = foundRows[0]["CloseTime"] != DBNull.Value ? DateTime.Parse(foundRows[0]["CloseTime"].ToString()).ToString("HH:mm") : "";
                                string isOpen = string.IsNullOrEmpty(openT) ? "0" : "1";

                                dtTimings.Rows.Add(day, isOpen, openT, closeT);
                            }
                            else
                            {
                                // No record exists: Provide smart defaults (Sunday closed, Mon-Sat 10:00 to 18:30)
                                if (day == "Sunday")
                                    dtTimings.Rows.Add(day, "0", "", "");
                                else
                                    dtTimings.Rows.Add(day, "1", "10:00", "18:30");
                            }
                        }
                    }
                }
            }

            // Bind to the new Repeater in Tab 3
            rptTimings.DataSource = dtTimings;
            rptTimings.DataBind();
        }

        // --- ADD THIS NEW CLICK EVENT METHOD ---
        protected void btnSaveTimings_Click(object sender, EventArgs e)
        {
            int branchId = Convert.ToInt32(hfBranchID.Value);

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                // 1. Wipe existing timings for a clean slate update
                string delQuery = "DELETE FROM VMS_PlantTimings WHERE Branch_ID = @BranchID";
                using (SqlCommand cmdDel = new SqlCommand(delQuery, con))
                {
                    cmdDel.Parameters.AddWithValue("@BranchID", branchId);
                    cmdDel.ExecuteNonQuery();
                }

                // 2. Insert new timings from the UI Grid
                string insQuery = "INSERT INTO VMS_PlantTimings (Branch_ID, DayOfWeek, OpenTime, CloseTime) VALUES (@BranchID, @DayOfWeek, @OpenTime, @CloseTime)";

                foreach (RepeaterItem item in rptTimings.Items)
                {
                    if (item.ItemType == ListItemType.Item || item.ItemType == ListItemType.AlternatingItem)
                    {
                        HiddenField hfDay = (HiddenField)item.FindControl("hfDay");
                        CheckBox chkIsOpen = (CheckBox)item.FindControl("chkIsOpen");
                        TextBox txtOpenTime = (TextBox)item.FindControl("txtOpenTime");
                        TextBox txtCloseTime = (TextBox)item.FindControl("txtCloseTime");

                        using (SqlCommand cmdIns = new SqlCommand(insQuery, con))
                        {
                            cmdIns.Parameters.AddWithValue("@BranchID", branchId);
                            cmdIns.Parameters.AddWithValue("@DayOfWeek", hfDay.Value);

                            // If checked AND times are provided, save the times. Otherwise, save NULLs (Office Closed).
                            if (chkIsOpen.Checked && !string.IsNullOrEmpty(txtOpenTime.Text) && !string.IsNullOrEmpty(txtCloseTime.Text))
                            {
                                cmdIns.Parameters.AddWithValue("@OpenTime", txtOpenTime.Text);
                                cmdIns.Parameters.AddWithValue("@CloseTime", txtCloseTime.Text);
                            }
                            else
                            {
                                cmdIns.Parameters.AddWithValue("@OpenTime", DBNull.Value);
                                cmdIns.Parameters.AddWithValue("@CloseTime", DBNull.Value);
                            }
                            cmdIns.ExecuteNonQuery();
                        }
                    }
                }
                con.Close();
            }

            lblMsg.ForeColor = Color.Green;
            lblMsg.Text = "Office Timings updated successfully!";
        }
    }
}