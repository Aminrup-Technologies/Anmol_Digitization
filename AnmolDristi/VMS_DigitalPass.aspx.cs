using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Drawing;
using System.IO;
using QRCoder;

namespace AnmolDristi
{
    public partial class VMS_DigitalPass : System.Web.UI.Page
    {
        string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Request.QueryString["token"] != null)
                {
                    LoadPassDetails(Request.QueryString["token"]);
                }
                else
                {
                    ShowError("Invalid Link. No security token provided.");
                }
            }
        }

        private void LoadPassDetails(string tokenStr)
        {
            Guid token;
            if (!Guid.TryParse(tokenStr, out token))
            {
                ShowError("Corrupted security token.");
                return;
            }

            using (SqlConnection con = new SqlConnection(connStr))
            {
                // Massive Join to get Visitor, Host, and Plant Settings in one query
                string query = @"
                    SELECT 
                        v.LogID,
                        v.VisitorName, 
                        v.VisitorCompany, 
                        v.PhotoPath, 
                        v.ValidUpto, 
                        v.ApprovalStatus,
                        ISNULL(v.ScheduleType, 'SingleDay') AS ScheduleType,
                        u.EmployeeName AS HostName,
                        ISNULL(s.AliasName, 'Anmol Industries') AS PlantAlias,
                        ISNULL(s.VisitorPolicyText, 'Standard safety rules apply.') AS PolicyText
                    FROM VMS_VisitLog v
                    LEFT JOIN MST_UserMaster u ON v.HostEmployeeCode = u.EmployeeCode
                    LEFT JOIN VMS_OfficeSettings s ON v.TargetBranchID = s.Branch_ID
                    WHERE v.QRToken = @Token";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Token", token);
                    con.Open();

                    int logId = 0;
                    string scheduleType = "SingleDay";
                    bool passFound = false;

                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        if (sdr.Read())
                        {
                            // 1. Verify Approval Status
                            if (sdr["ApprovalStatus"].ToString() != "Approved")
                            {
                                ShowError("This pass has not been approved or has been revoked.");
                                return;
                            }

                            // 2. Bind Data to UI
                            logId = Convert.ToInt32(sdr["LogID"]);
                            scheduleType = sdr["ScheduleType"].ToString();
                            lblVisitorName.Text = sdr["VisitorName"].ToString();
                            lblCompany.Text = sdr["VisitorCompany"].ToString();
                            lblHostName.Text = sdr["HostName"].ToString();
                            lblPlantAlias.Text = sdr["PlantAlias"].ToString();

                            // Format Policy text to keep line breaks
                            litPolicyText.Text = sdr["PolicyText"].ToString().Replace("\n", "<br/>");

                            // Handle absolute vs relative image paths
                            string photoUrl = sdr["PhotoPath"].ToString();
                            imgVisitorPhoto.ImageUrl = string.IsNullOrEmpty(photoUrl) ? "~/images/user.png" : photoUrl;

                            // 3. Time Validation (Has the pass expired?)
                            DateTime validUpto = Convert.ToDateTime(sdr["ValidUpto"]);
                            lblValidUpto.Text = validUpto.ToString("dd MMM yyyy, hh:mm tt");

                            if (DateTime.Now > validUpto)
                            {
                                lblStatusBadge.Text = "<span class='status-badge status-expired'><i class='fa fa-times-circle'></i> EXPIRED</span>";
                                imgScanQR.Visible = false; // Hide QR so guard can't scan it
                            }
                            else
                            {
                                lblStatusBadge.Text = "<span class='status-badge status-valid'><i class='fa fa-check-circle'></i> VALID PASS</span>";
                                GenerateScannableQR(tokenStr);
                            }

                            passFound = true;
                            pnlPass.Visible = true;
                        }
                        else
                        {
                            ShowError("Pass not found in the system.");
                        }
                    }

                    if (passFound)
                    {
                        BindApprovedDates(con, logId, scheduleType);
                    }
                }
            }
        }

        private void BindApprovedDates(SqlConnection con, int logId, string scheduleType)
        {
            // scheduleType is already loaded with the pass; helper re-checks ScheduleType + day log
            string approvedDatesText = VmsHelper.GetMultiDayApprovedDatesText(con, logId);
            if (string.IsNullOrEmpty(approvedDatesText))
            {
                divApprovedDates.Visible = false;
                lblApprovedDates.Text = "";
                return;
            }

            lblApprovedDates.Text = approvedDatesText;
            divApprovedDates.Visible = true;
        }

        private void GenerateScannableQR(string tokenData)
        {
            // The Guard's scanner will read this raw text to Check-In the visitor
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(tokenData, QRCodeGenerator.ECCLevel.M);
            QRCode qrCode = new QRCode(qrCodeData);

            using (Bitmap bitMap = qrCode.GetGraphic(10))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    bitMap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    byte[] byteImage = ms.ToArray();
                    imgScanQR.ImageUrl = "data:image/png;base64," + Convert.ToBase64String(byteImage);
                }
            }
        }

        private void ShowError(string message)
        {
            pnlError.Visible = true;
            pnlPass.Visible = false;
            lblError.Text = message;
        }
    }
}