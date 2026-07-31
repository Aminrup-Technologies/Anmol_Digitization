using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.UI.WebControls;
using System.Web.UI;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace AnmolDristi
{
    public partial class VMS_MyVisits : System.Web.UI.Page
    {
        string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Security Check: Look for the NEW Dual-Auth session keys
                if (Session["VerifiedMobile"] == null ||
                    string.IsNullOrEmpty(Session["VerifiedMobile"].ToString()))
                {
                    // Safe Redirect Pattern
                    Response.Redirect("VMS_SelfLogin.aspx", false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }

                LoadVisitorHistory();
            }
        }

        private void LoadVisitorHistory()
        {
            string mobile = Session["VerifiedMobile"] != null
                ? Session["VerifiedMobile"].ToString()
                : string.Empty;
            string email = Session["VerifiedEmail"] != null
                ? Session["VerifiedEmail"].ToString()
                : string.Empty;

            using (SqlConnection con = new SqlConnection(connStr))
            {
                string query = @"
                    SELECT 
                        v.LogID,
                        v.CreatedDate,
                        v.PurposeOfVisit,
                        v.ApprovalStatus,
                        v.VisitStatus,
                        v.QRToken,
                        ISNULL(o.AliasName, b.Branch_Description) AS AliasName,
                        u.EmployeeName AS HostName
                    FROM 
                        VMS_VisitLog v
                    LEFT JOIN 
                        VMS_OfficeSettings o ON v.TargetBranchID = o.Branch_ID
                    LEFT JOIN 
                        MST_Branch b ON v.TargetBranchID = b.Branch_ID
                    LEFT JOIN 
                        MST_UserMaster u ON v.HostEmployeeCode = u.EmployeeCode
                    WHERE 
                        (
                            (@Mobile <> '' AND v.VisitorMobile = @Mobile)
                            OR (@Email <> '' AND @Email <> 'N/A' AND v.VisitorEmail = @Email)
                        )
                        AND v.CreatedDate >= DATEADD(month, -6, GETDATE())
                    ORDER BY 
                        v.CreatedDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Mobile", VmsHelper.NormalizeMobile(mobile));
                    cmd.Parameters.AddWithValue("@Email", VmsHelper.NormalizeEmail(email));

                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);

                        rptVisits.DataSource = dt;
                        rptVisits.DataBind();
                    }
                }
            }
        }

        protected void rptVisits_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                // 1. Style the Status Badge dynamically
                Label lblStatus = (Label)e.Item.FindControl("lblStatus");
                string status = lblStatus.Text.ToLower();

                if (status == "approved")
                    lblStatus.CssClass = "badge badge-approved";
                else if (status == "rejected")
                    lblStatus.CssClass = "badge badge-rejected";
                else
                    lblStatus.CssClass = "badge badge-pending";

                // 2. Process Digital Pass Footer & QR Code
                Panel pnlFooter = (Panel)e.Item.FindControl("pnlFooter");

                if (status == "approved")
                {
                    object tokenObj = DataBinder.Eval(e.Item.DataItem, "QRToken");

                    if (tokenObj != DBNull.Value && tokenObj != null && !string.IsNullOrEmpty(tokenObj.ToString()))
                    {
                        // Show the footer wrapper
                        pnlFooter.Visible = true;

                        // Find the Image and Label controls inside the repeater
                        System.Web.UI.WebControls.Image imgQRCode = (System.Web.UI.WebControls.Image)e.Item.FindControl("imgQRCode");
                        Label lblQRMessage = (Label)e.Item.FindControl("lblQRMessage");

                        if (imgQRCode != null)
                        {
                            // Initialize the QR Generator using the secure DB Token
                            string qrTokenFromDb = tokenObj.ToString();
                            QRCodeGenerator qrGenerator = new QRCodeGenerator();
                            QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrTokenFromDb, QRCodeGenerator.ECCLevel.Q);
                            QRCode qrCode = new QRCode(qrCodeData);

                            // Render graphic (Size 5 is optimal for mobile screens)
                            using (Bitmap qrCodeImage = qrCode.GetGraphic(5))
                            {
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    // Convert the image directly to Base64 to serve it without saving to disk
                                    qrCodeImage.Save(ms, ImageFormat.Png);
                                    byte[] byteImage = ms.ToArray();
                                    string base64String = Convert.ToBase64String(byteImage);

                                    // Bind the generated image string to the UI control
                                    imgQRCode.ImageUrl = "data:image/png;base64," + base64String;
                                    imgQRCode.Visible = true;
                                }
                            }

                            if (lblQRMessage != null)
                            {
                                lblQRMessage.Text = "Scan this at the security gate to check in.";
                                lblQRMessage.Visible = true;
                            }

                            Panel pnlPassLink = (Panel)e.Item.FindControl("pnlPassLink");
                            if (pnlPassLink != null)
                                pnlPassLink.Visible = true;
                        }
                    }
                }
                else
                {
                    if (pnlFooter != null)
                        pnlFooter.Visible = true;

                    Panel pnlPassLink = (Panel)e.Item.FindControl("pnlPassLink");
                    if (pnlPassLink != null)
                        pnlPassLink.Visible = false;

                    Label lblQRMessage = (Label)e.Item.FindControl("lblQRMessage");
                    if (lblQRMessage != null)
                    {
                        if (status == "pending")
                        {
                            lblQRMessage.Text = "Waiting for host approval. This page will show your gate pass once approved.";
                            lblQRMessage.Visible = true;
                        }
                        else if (status == "rejected")
                        {
                            lblQRMessage.Text = "This visit request was rejected by the host.";
                            lblQRMessage.Visible = true;
                        }
                    }
                }
            }
        }
    }
}