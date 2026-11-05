using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.UI.WebControls;
using System.Drawing;
using System.IO;
using QRCoder;
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
                int branchId = Convert.ToInt32(e.CommandArgument);
                hfBranchID.Value = branchId.ToString();
                pnlList.Visible = false;
                pnlEdit.Visible = true;
                lblMsg.Text = "";
                LoadOfficeDetails(branchId);
            }
            else if (e.CommandName == "GenerateQR" || e.CommandName == "GenerateGateQR")
            {
                string[] args = e.CommandArgument.ToString().Split('|');
                string branchId = args[0];
                string aliasName = args[1];
                string fullAddress = args[2];
                string branchName = args.Length > 3 ? args[3] : "";

                if (aliasName == "Not Configured")
                {
                    lblMsg.ForeColor = System.Drawing.Color.Red;
                    lblMsg.Text = "Please configure the Basic Details for this office before generating a QR code.";
                    return;
                }

                if (e.CommandName == "GenerateQR")
                {
                    GenerateReceptionQRPdf(branchId, aliasName, fullAddress, branchName);
                }
                else if (e.CommandName == "GenerateGateQR")
                {
                    GenerateGateQRPdf(branchId, aliasName, fullAddress, branchName);
                }
            }
        }

        private void AddCompanyDetailsToPDF(Document pdfDoc, string aliasName, string branchName, string fullAddress)
        {
            iTextSharp.text.Font companyFont = FontFactory.GetFont("Arial", 22, iTextSharp.text.Font.BOLD);
            iTextSharp.text.Font subCompanyFont = FontFactory.GetFont("Arial", 16, iTextSharp.text.Font.BOLD, BaseColor.DARK_GRAY);
            iTextSharp.text.Font branchFont = FontFactory.GetFont("Arial", 12, iTextSharp.text.Font.BOLD, BaseColor.BLACK);
            iTextSharp.text.Font addressFont = FontFactory.GetFont("Arial", 11, iTextSharp.text.Font.NORMAL, BaseColor.DARK_GRAY);

            string companyLine1 = aliasName;
            string companyLine2 = string.Empty;

            int dashIndex = aliasName.IndexOf('-');
            if (dashIndex > 0)
            {
                companyLine1 = aliasName.Substring(0, dashIndex).Trim() + ",";
                companyLine2 = aliasName.Substring(dashIndex + 1).Trim();
            }

            Paragraph comp1 = new Paragraph(companyLine1, companyFont);
            comp1.Alignment = Element.ALIGN_CENTER;
            comp1.SpacingBefore = 20f;
            pdfDoc.Add(comp1);

            if (!string.IsNullOrEmpty(companyLine2))
            {
                Paragraph comp2 = new Paragraph(companyLine2, subCompanyFont);
                comp2.Alignment = Element.ALIGN_CENTER;
                comp2.SpacingAfter = 5f;
                pdfDoc.Add(comp2);
            }
            else
            {
                comp1.SpacingAfter = 5f;
            }

            if (!string.IsNullOrEmpty(branchName))
            {
                Paragraph branchP = new Paragraph("Branch: " + branchName, branchFont);
                branchP.Alignment = Element.ALIGN_CENTER;
                branchP.SpacingAfter = 5f;
                pdfDoc.Add(branchP);
            }

            Paragraph address = new Paragraph(fullAddress, addressFont);
            address.Alignment = Element.ALIGN_CENTER;
            address.SpacingAfter = 25f;
            pdfDoc.Add(address);
        }

        private void GenerateReceptionQRPdf_OLD(string branchId, string aliasName, string fullAddress, string branchName)
        {
            string kioskUrl = VmsHelper.BuildSelfLoginUrl(Convert.ToInt32(branchId));

            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(kioskUrl, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);

            //Bitmap qrBitmap = qrCode.GetGraphic(20);
            using (Bitmap qrBitmap = qrCode.GetGraphic(20))
            {
                Document pdfDoc = new Document(PageSize.A4, 30f, 30f, 40f, 30f);
                MemoryStream memStream = new MemoryStream();
                PdfWriter.GetInstance(pdfDoc, memStream);
                pdfDoc.Open();

                try
                {
                    string logoPath = Server.MapPath("~/WebData/anmol_logo_GPTW.jpg");
                    if (File.Exists(logoPath))
                    {
                        iTextSharp.text.Image logoImg = iTextSharp.text.Image.GetInstance(logoPath);
                        logoImg.Alignment = Element.ALIGN_CENTER;
                        logoImg.ScaleToFit(150f, 80f);
                        logoImg.SpacingAfter = 10f;
                        pdfDoc.Add(logoImg);
                    }
                }
                catch (Exception) { }

                iTextSharp.text.Font titleFont = FontFactory.GetFont("Arial", 36, iTextSharp.text.Font.BOLD);
                iTextSharp.text.Font subTitleFont = FontFactory.GetFont("Arial", 16, iTextSharp.text.Font.NORMAL);
                iTextSharp.text.Font footerFont = FontFactory.GetFont("Arial", 10, iTextSharp.text.Font.ITALIC, BaseColor.GRAY);

                Paragraph title = new Paragraph("WELCOME VISITORS", titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                title.SpacingAfter = 5f;
                pdfDoc.Add(title);

                Paragraph subTitle = new Paragraph("Scan the QR & generate your pass", subTitleFont);
                subTitle.Alignment = Element.ALIGN_CENTER;
                subTitle.SpacingAfter = 20f;
                pdfDoc.Add(subTitle);

                using (MemoryStream imgStream = new MemoryStream())
                {
                    qrBitmap.Save(imgStream, System.Drawing.Imaging.ImageFormat.Png);
                    iTextSharp.text.Image pdfImg = iTextSharp.text.Image.GetInstance(imgStream.ToArray());
                    pdfImg.Alignment = Element.ALIGN_CENTER;
                    pdfImg.ScaleToFit(300f, 300f);
                    pdfDoc.Add(pdfImg);
                }

                AddCompanyDetailsToPDF(pdfDoc, aliasName, branchName, fullAddress);

                Paragraph footer = new Paragraph("Powered by Anmol-IT", footerFont);
                footer.Alignment = Element.ALIGN_CENTER;
                pdfDoc.Add(footer);

                pdfDoc.Close();
                byte[] bytes = memStream.ToArray();
                memStream.Close();

                Response.Clear();
                Response.ContentType = "application/pdf";
                Response.AddHeader("content-disposition", $"attachment;filename=ReceptionQR_{branchId}.pdf");
                Response.Buffer = true;
                Response.Cache.SetCacheability(HttpCacheability.NoCache);
                Response.BinaryWrite(bytes);
                Response.End();
            }
        }

        // Refactored PDF Generation logic
        private void GenerateReceptionQRPdf(string branchId, string aliasName, string fullAddress, string branchName)
        {
            string kioskUrl = VmsHelper.BuildSelfLoginUrl(Convert.ToInt32(branchId));

            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(kioskUrl, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);

            using (Bitmap qrBitmap = qrCode.GetGraphic(20))
            {
                using (MemoryStream memStream = new MemoryStream())
                {
                    Document pdfDoc = new Document(PageSize.A4, 30f, 30f, 40f, 30f);
                    PdfWriter.GetInstance(pdfDoc, memStream);
                    pdfDoc.Open();

                    try
                    {
                        string logoPath = Server.MapPath("~/WebData/anmol_logo_GPTW.jpg");
                        if (File.Exists(logoPath))
                        {
                            iTextSharp.text.Image logoImg = iTextSharp.text.Image.GetInstance(logoPath);
                            logoImg.Alignment = Element.ALIGN_CENTER;
                            logoImg.ScaleToFit(150f, 80f);
                            logoImg.SpacingAfter = 10f;
                            pdfDoc.Add(logoImg);
                        }
                    }
                    catch (Exception) { }

                    iTextSharp.text.Font titleFont = FontFactory.GetFont("Arial", 36, iTextSharp.text.Font.BOLD);
                    iTextSharp.text.Font subTitleFont = FontFactory.GetFont("Arial", 16, iTextSharp.text.Font.NORMAL);
                    iTextSharp.text.Font footerFont = FontFactory.GetFont("Arial", 10, iTextSharp.text.Font.ITALIC, BaseColor.GRAY);

                    Paragraph title = new Paragraph("WELCOME VISITORS", titleFont) { Alignment = Element.ALIGN_CENTER, SpacingAfter = 5f };
                    pdfDoc.Add(title);

                    Paragraph subTitle = new Paragraph("Scan the QR & generate your pass", subTitleFont) { Alignment = Element.ALIGN_CENTER, SpacingAfter = 20f };
                    pdfDoc.Add(subTitle);

                    using (MemoryStream imgStream = new MemoryStream())
                    {
                        qrBitmap.Save(imgStream, System.Drawing.Imaging.ImageFormat.Png);
                        iTextSharp.text.Image pdfImg = iTextSharp.text.Image.GetInstance(imgStream.ToArray());
                        pdfImg.Alignment = Element.ALIGN_CENTER;
                        pdfImg.ScaleToFit(300f, 300f);
                        pdfDoc.Add(pdfImg);
                    }

                    AddCompanyDetailsToPDF(pdfDoc, aliasName, branchName, fullAddress);

                    Paragraph footer = new Paragraph("Powered by Anmol-IT", footerFont) { Alignment = Element.ALIGN_CENTER };
                    pdfDoc.Add(footer);

                    pdfDoc.Close();
                    byte[] bytes = memStream.ToArray();

                    Response.Clear();
                    Response.ContentType = "application/pdf";
                    Response.AddHeader("content-disposition", $"attachment;filename=ReceptionQR_{branchId}.pdf");
                    Response.Buffer = true;
                    Response.Cache.SetCacheability(HttpCacheability.NoCache);
                    Response.BinaryWrite(bytes);
                    Response.End();
                }
            }
        }

        private void GenerateGateQRPdf(string branchId, string aliasName, string fullAddress, string branchName)
        {
            string gateUrl = VmsHelper.BuildGateScannerUrl(Convert.ToInt32(branchId));

            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(gateUrl, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);

            Bitmap qrBitmap = qrCode.GetGraphic(20);

            Document pdfDoc = new Document(PageSize.A4, 30f, 30f, 40f, 30f);
            MemoryStream memStream = new MemoryStream();
            PdfWriter.GetInstance(pdfDoc, memStream);
            pdfDoc.Open();

            try
            {
                string logoPath = Server.MapPath("~/WebData/anmol_logo_GPTW.jpg");
                if (File.Exists(logoPath))
                {
                    iTextSharp.text.Image logoImg = iTextSharp.text.Image.GetInstance(logoPath);
                    logoImg.Alignment = Element.ALIGN_CENTER;
                    logoImg.ScaleToFit(150f, 80f);
                    logoImg.SpacingAfter = 10f;
                    pdfDoc.Add(logoImg);
                }
            }
            catch (Exception) { }

            iTextSharp.text.Font titleFont = FontFactory.GetFont("Arial", 32, iTextSharp.text.Font.BOLD, BaseColor.RED);
            iTextSharp.text.Font subTitleFont = FontFactory.GetFont("Arial", 16, iTextSharp.text.Font.BOLD);
            iTextSharp.text.Font footerFont = FontFactory.GetFont("Arial", 10, iTextSharp.text.Font.ITALIC, BaseColor.GRAY);

            Paragraph title = new Paragraph("SECURITY DESK ONLY", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            title.SpacingAfter = 5f;
            pdfDoc.Add(title);

            Paragraph subTitle = new Paragraph("Guards: Scan this QR to open your Pass Scanner App", subTitleFont);
            subTitle.Alignment = Element.ALIGN_CENTER;
            subTitle.SpacingAfter = 20f;
            pdfDoc.Add(subTitle);

            using (MemoryStream imgStream = new MemoryStream())
            {
                qrBitmap.Save(imgStream, System.Drawing.Imaging.ImageFormat.Png);
                iTextSharp.text.Image pdfImg = iTextSharp.text.Image.GetInstance(imgStream.ToArray());
                pdfImg.Alignment = Element.ALIGN_CENTER;
                pdfImg.ScaleToFit(300f, 300f);
                pdfDoc.Add(pdfImg);
            }

            AddCompanyDetailsToPDF(pdfDoc, aliasName, branchName, fullAddress);

            Paragraph footer = new Paragraph("RESTRICTED ACCESS - INTERNAL USE ONLY", FontFactory.GetFont("Arial", 12, iTextSharp.text.Font.BOLD, BaseColor.RED));
            footer.Alignment = Element.ALIGN_CENTER;
            pdfDoc.Add(footer);

            pdfDoc.Close();
            byte[] bytes = memStream.ToArray();
            memStream.Close();

            Response.Clear();
            Response.ContentType = "application/pdf";
            Response.AddHeader("content-disposition", $"attachment;filename=SecurityGateQR_{branchId}.pdf");
            Response.Buffer = true;
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.BinaryWrite(bytes);
            Response.End();
        }

        private void LoadOfficeDetails(int branchId)
        {
            string currentAlias = "Not Configured";

            using (SqlConnection con = new SqlConnection(connStr))
            {
                string queryBranch = "SELECT Branch_Description FROM MST_Branch WHERE Branch_ID = @BranchID";
                using (SqlCommand cmd = new SqlCommand(queryBranch, con))
                {
                    cmd.Parameters.AddWithValue("@BranchID", branchId);
                    con.Open();
                    lblCurrentBranch.Text = cmd.ExecuteScalar()?.ToString();
                    con.Close();
                }

                string querySettings = @"
                    SELECT AliasName, FullAddress, VisitorPolicyText, TimeZone,
                           PreconfiguredHostCodes, EscalationEmailList, EscalationTimeoutMinutes
                    FROM VMS_OfficeSettings
                    WHERE Branch_ID = @BranchID";
                using (SqlCommand cmd = new SqlCommand(querySettings, con))
                {
                    cmd.Parameters.AddWithValue("@BranchID", branchId);
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);

                        if (dt.Rows.Count > 0)
                        {
                            currentAlias = dt.Rows[0]["AliasName"].ToString();
                            txtAlias.Text = currentAlias;
                            txtAddress.Text = dt.Rows[0]["FullAddress"].ToString();
                            txtPolicy.Text = dt.Rows[0]["VisitorPolicyText"].ToString();
                            txtPreconfiguredHosts.Text = dt.Rows[0]["PreconfiguredHostCodes"] != DBNull.Value
                                ? dt.Rows[0]["PreconfiguredHostCodes"].ToString()
                                : "";
                            txtEscalationEmails.Text = dt.Rows[0]["EscalationEmailList"] != DBNull.Value
                                ? dt.Rows[0]["EscalationEmailList"].ToString()
                                : "";
                            txtEscalationTimeout.Text = dt.Rows[0]["EscalationTimeoutMinutes"] != DBNull.Value
                                ? dt.Rows[0]["EscalationTimeoutMinutes"].ToString()
                                : "15";

                            if (ddlTimeZone.Items.FindByValue(dt.Rows[0]["TimeZone"].ToString()) != null)
                            {
                                ddlTimeZone.SelectedValue = dt.Rows[0]["TimeZone"].ToString();
                            }
                        }
                        else
                        {
                            txtAlias.Text = "";
                            txtAddress.Text = "";
                            txtPolicy.Text = "1. Suit Up! Wear required Hair Net...\n2. No Smoking or Gutkha...\n3. Stay Alert! Be mindful of moving machinery...";
                            txtPreconfiguredHosts.Text = "";
                            txtEscalationEmails.Text = "";
                            txtEscalationTimeout.Text = "15";
                        }
                    }
                }

                LoadOfficeTimings(branchId);

                // NEW: Load the on-screen QR visual previews
                LoadQRPreviews(branchId.ToString(), currentAlias);
            }
        }

        // --- NEW: ON-SCREEN QR PREVIEW GENERATOR ---
        private void LoadQRPreviews(string branchId, string aliasName)
        {
            if (string.IsNullOrEmpty(aliasName) || aliasName == "Not Configured")
            {
                imgPreviewVisitor.Visible = false;
                imgPreviewSecurity.Visible = false;
                lblPreviewVisitorAlias.Text = "Save Basic Details first to preview QR.";
                lblPreviewSecurityAlias.Text = "Save Basic Details first to preview QR.";
                return;
            }

            imgPreviewVisitor.Visible = true;
            imgPreviewSecurity.Visible = true;

            // Apply same hyphen-split logic as the PDF
            string companyLine1 = aliasName;
            int dashIndex = aliasName.IndexOf('-');
            if (dashIndex > 0)
            {
                companyLine1 = aliasName.Substring(0, dashIndex).Trim();
            }

            lblPreviewVisitorAlias.Text = companyLine1;
            lblPreviewSecurityAlias.Text = companyLine1;

            string kioskUrl = VmsHelper.BuildSelfLoginUrl(Convert.ToInt32(branchId));
            string gateUrl = VmsHelper.BuildGateScannerUrl(Convert.ToInt32(branchId));

            imgPreviewVisitor.ImageUrl = GenerateBase64QR(kioskUrl);
            imgPreviewSecurity.ImageUrl = GenerateBase64QR(gateUrl);
        }

        private string GenerateBase64QR(string url)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            // ECCLevel.Q matches the robust error correction used in the PDFs
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);

            using (Bitmap bitMap = qrCode.GetGraphic(20))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    bitMap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    byte[] byteImage = ms.ToArray();
                    return "data:image/png;base64," + Convert.ToBase64String(byteImage);
                }
            }
        }
        // -------------------------------------------

        protected void btnSave_Click(object sender, EventArgs e)
        {
            int branchId = Convert.ToInt32(hfBranchID.Value);
            string empCode = Session["EmployeeCode"] != null ? Session["EmployeeCode"].ToString() : "Admin";

            string query = @"
            IF EXISTS (SELECT 1 FROM VMS_OfficeSettings WHERE Branch_ID = @BranchID)
            BEGIN
                UPDATE VMS_OfficeSettings 
                SET AliasName = @AliasName, 
                    FullAddress = @FullAddress, 
                    VisitorPolicyText = @Policy, 
                    TimeZone = @TimeZone, 
                    PreconfiguredHostCodes = @PreconfiguredHostCodes,
                    EscalationEmailList = @EscalationEmailList,
                    EscalationTimeoutMinutes = @EscalationTimeout,
                    UpdatedAt = GETDATE(), 
                    UpdatedBy = @EmpCode
                WHERE Branch_ID = @BranchID
            END
            ELSE
            BEGIN
                INSERT INTO VMS_OfficeSettings 
                (Branch_ID, AliasName, FullAddress, VisitorPolicyText, TimeZone, PreconfiguredHostCodes, EscalationEmailList, EscalationTimeoutMinutes, UpdatedBy)
                VALUES 
                (@BranchID, @AliasName, @FullAddress, @Policy, @TimeZone, @PreconfiguredHostCodes, @EscalationEmailList, @EscalationTimeout, @EmpCode)
            END";

            try
            {
                string preconfiguredHosts = txtPreconfiguredHosts.Text.Trim();
                string escalationEmails = txtEscalationEmails.Text.Trim();

                // VMS_OfficeSettings and VMS_PlantTimings commit or roll back together.
                using (SqlConnection con = new SqlConnection(connStr))
                {
                    con.Open();
                    using (SqlTransaction trans = con.BeginTransaction())
                    {
                        try
                        {
                            string timingError;
                            List<PlantDayTiming> timings = ValidateSubmittedTimings(
                                VmsVisitScheduleHelper.LoadPlantTimings(branchId, con, trans), out timingError);
                            if (timings == null)
                            {
                                trans.Rollback();
                                lblMsg.ForeColor = Color.Red;
                                lblMsg.Text = "Nothing was saved. Correct the office timings:<br/>" + timingError;
                                return;
                            }

                            using (SqlCommand cmd = new SqlCommand(query, con, trans))
                            {
                                cmd.Parameters.AddWithValue("@BranchID", branchId);
                                cmd.Parameters.AddWithValue("@AliasName", txtAlias.Text.Trim());
                                cmd.Parameters.AddWithValue("@FullAddress", txtAddress.Text.Trim());
                                cmd.Parameters.AddWithValue("@Policy", txtPolicy.Text.Trim());
                                cmd.Parameters.AddWithValue("@TimeZone", ddlTimeZone.SelectedValue);
                                cmd.Parameters.AddWithValue("@EmpCode", empCode);
                                cmd.Parameters.AddWithValue("@PreconfiguredHostCodes",
                                    string.IsNullOrEmpty(preconfiguredHosts) ? (object)DBNull.Value : preconfiguredHosts);
                                cmd.Parameters.AddWithValue("@EscalationEmailList",
                                    string.IsNullOrEmpty(escalationEmails) ? (object)DBNull.Value : escalationEmails);
                                cmd.Parameters.AddWithValue("@EscalationTimeout", string.IsNullOrEmpty(txtEscalationTimeout.Text) ? 15 : Convert.ToInt32(txtEscalationTimeout.Text));
                                cmd.ExecuteNonQuery();
                            }

                            // Persist office timings from Tab 3 in the same save action
                            SaveOfficeTimings(con, trans, branchId, timings);

                            trans.Commit();
                        }
                        catch
                        {
                            try { trans.Rollback(); } catch { }
                            throw;
                        }
                    }
                }

                lblMsg.ForeColor = Color.Green;
                lblMsg.Text = "Details updated successfully!";

                // Re-load the office details so the newly saved Alias immediately updates the QR Previews
                LoadOfficeDetails(branchId);
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
            ClearHostsAndEscalationFields();
            pnlEdit.Visible = false;
            pnlList.Visible = true;
        }

        private void ClearHostsAndEscalationFields()
        {
            txtPreconfiguredHosts.Text = "";
            txtEscalationEmails.Text = "";
        }

        private void LoadOfficeTimings(int branchId)
        {
            DataTable dtTimings = new DataTable();
            dtTimings.Columns.Add("DayOfWeek");
            dtTimings.Columns.Add("DayState");
            dtTimings.Columns.Add("OpenTime");
            dtTimings.Columns.Add("CloseTime");
            dtTimings.Columns.Add("SavedStatus");
            dtTimings.Columns.Add("SavedStatusCss");

            Dictionary<string, PlantDayTiming> saved = VmsVisitScheduleHelper.LoadPlantTimings(branchId);
            foreach (string day in VmsVisitScheduleHelper.PlantTimingWeekdays)
            {
                PlantDayTiming timing;
                saved.TryGetValue(day, out timing);
                switch (VmsVisitScheduleHelper.ClassifyPlantDayConfig(timing))
                {
                    case VmsVisitScheduleHelper.PlantDayConfigState.Open:
                        dtTimings.Rows.Add(day, "OPEN", FormatPlantTime(timing.OpenTime), FormatPlantTime(timing.CloseTime), "Open", "label label-success");
                        break;
                    case VmsVisitScheduleHelper.PlantDayConfigState.Closed:
                        dtTimings.Rows.Add(day, "CLOSED", "", "", "Closed", "label label-default");
                        break;
                    case VmsVisitScheduleHelper.PlantDayConfigState.Invalid:
                        dtTimings.Rows.Add(day, "", FormatPlantTime(timing.OpenTime), FormatPlantTime(timing.CloseTime),
                            "Invalid - select Open or Closed", "label label-danger");
                        break;
                    default:
                        dtTimings.Rows.Add(day, "", "", "", "Not Configured", "label label-warning");
                        break;
                }
            }

            rptTimings.DataSource = dtTimings;
            rptTimings.DataBind();
        }

        private static string FormatPlantTime(TimeSpan? time)
        {
            return time.HasValue && time.Value >= TimeSpan.Zero && time.Value < TimeSpan.FromDays(1)
                ? time.Value.ToString(@"hh\:mm", CultureInfo.InvariantCulture) : "";
        }

        /// <summary>
        /// Validates all seven submitted weekdays against the persisted state. Returns null (with error) when any day fails.
        /// </summary>
        private List<PlantDayTiming> ValidateSubmittedTimings(Dictionary<string, PlantDayTiming> existing, out string error)
        {
            var timings = new List<PlantDayTiming>();
            var errors = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (RepeaterItem item in rptTimings.Items)
            {
                if (item.ItemType != ListItemType.Item && item.ItemType != ListItemType.AlternatingItem)
                    continue;

                string submittedDay = ((HiddenField)item.FindControl("hfDay")).Value.Trim();
                string day = Array.Find(VmsVisitScheduleHelper.PlantTimingWeekdays,
                    d => string.Equals(d, submittedDay, StringComparison.OrdinalIgnoreCase));
                if (day == null || !seen.Add(day))
                {
                    errors.Add("Unrecognized or duplicate weekday in the timing grid.");
                    continue;
                }

                PlantDayTiming persisted;
                existing.TryGetValue(day, out persisted);
                VmsVisitScheduleHelper.PlantDayConfigState persistedState = VmsVisitScheduleHelper.ClassifyPlantDayConfig(persisted);
                string state = ((DropDownList)item.FindControl("ddlDayState")).SelectedValue;
                string openText = ((TextBox)item.FindControl("txtOpenTime")).Text.Trim();
                string closeText = ((TextBox)item.FindControl("txtCloseTime")).Text.Trim();
                TimeSpan open, close;

                if (state == "")
                {
                    if (persistedState != VmsVisitScheduleHelper.PlantDayConfigState.Missing)
                        errors.Add(day + ": a saved or invalid day cannot be reset to Not Configured; select Open or Closed.");
                    else if (openText.Length > 0 || closeText.Length > 0)
                        errors.Add(day + ": select Open to save these times, or clear them to leave the day not configured.");
                }
                else if (state == "CLOSED")
                    timings.Add(new PlantDayTiming { DayOfWeek = day });
                else if (state != "OPEN")
                    errors.Add(day + ": unrecognized Day Status.");
                else if (!VmsVisitScheduleHelper.TryParsePlantTime(openText, out open)
                    || !VmsVisitScheduleHelper.TryParsePlantTime(closeText, out close))
                    errors.Add(day + ": an open day needs a valid Open Time and Close Time.");
                else if (close <= open)
                    errors.Add(day + ": Close Time must be after Open Time; overnight hours are not supported.");
                else
                    timings.Add(new PlantDayTiming { DayOfWeek = day, OpenTime = open, CloseTime = close });
            }

            if (seen.Count != VmsVisitScheduleHelper.PlantTimingWeekdays.Length)
                errors.Add("The timing grid is incomplete; reopen the office and try again.");

            error = string.Join("<br/>", errors);
            return errors.Count == 0 ? timings : null;
        }

        /// <summary>
        /// Replaces this branch's VMS_PlantTimings rows inside the caller's open transaction; the caller commits or rolls back.
        /// </summary>
        private void SaveOfficeTimings(SqlConnection con, SqlTransaction trans, int branchId, List<PlantDayTiming> timings)
        {
            string delQuery = "DELETE FROM VMS_PlantTimings WHERE Branch_ID = @BranchID";
            using (SqlCommand cmdDel = new SqlCommand(delQuery, con, trans))
            {
                cmdDel.Parameters.AddWithValue("@BranchID", branchId);
                cmdDel.ExecuteNonQuery();
            }

            string insQuery = "INSERT INTO VMS_PlantTimings (Branch_ID, DayOfWeek, OpenTime, CloseTime) VALUES (@BranchID, @DayOfWeek, @OpenTime, @CloseTime)";

            foreach (PlantDayTiming timing in timings)
            {
                using (SqlCommand cmdIns = new SqlCommand(insQuery, con, trans))
                {
                    cmdIns.Parameters.AddWithValue("@BranchID", branchId);
                    cmdIns.Parameters.AddWithValue("@DayOfWeek", timing.DayOfWeek);
                    cmdIns.Parameters.AddWithValue("@OpenTime", timing.OpenTime.HasValue ? (object)timing.OpenTime.Value : DBNull.Value);
                    cmdIns.Parameters.AddWithValue("@CloseTime", timing.CloseTime.HasValue ? (object)timing.CloseTime.Value : DBNull.Value);
                    cmdIns.ExecuteNonQuery();
                }
            }
        }
    }
}