using System;
using System.Data.SqlClient;
using System.Configuration;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Web;

namespace AnmolDristi
{
    public partial class VMS_SelfLogin : System.Web.UI.Page
    {
        string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                Session.Clear();
                Session.Abandon();

                Response.Cache.SetCacheability(HttpCacheability.NoCache);
                Response.Cache.SetExpires(DateTime.UtcNow.AddHours(-1));
                Response.Cache.SetNoStore();

                if (Request.Cookies["ASP.NET_SessionId"] != null)
                {
                    Response.Cookies["ASP.NET_SessionId"].Expires = DateTime.Now.AddDays(-1);
                }

                if (Request.QueryString["BranchId"] != null)
                {
                    string branchId = Request.QueryString["BranchId"].ToString();
                    hfBranchId.Value = branchId;
                    LoadBranchName(branchId);
                }
                else
                {
                    lblMessage.ForeColor = Color.Red;
                    lblMessage.Text = "Invalid QR Code. Missing Location ID.";
                    pnlRequestOTP.Visible = false;
                }
            }
        }

        private void LoadBranchName(string branchId)
        {
            try
            {
                if (int.TryParse(branchId, out int parsedBranchId))
                {
                    lblBranchName.Text = VmsHelper.GetBranchDisplayName(parsedBranchId);
                }
            }
            catch (Exception ex)
            {
                lblBranchName.Text = "our Facility";
                System.Diagnostics.Debug.WriteLine($"Error fetching branch name: {ex.Message}");
            }
        }

        protected void btnSendOTP_Click(object sender, EventArgs e)
        {
            string mobile = txtMobile.Text.Trim();

            if (mobile.Length != 10 || !Regex.IsMatch(mobile, @"^\d{10}$"))
            {
                lblMessage.ForeColor = Color.Red;
                lblMessage.Text = "Please enter a valid 10-digit Mobile Number.";
                return;
            }

            Random rand = new Random();
            string mobileOTP = rand.Next(100000, 999999).ToString();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();
                string query = @"
                    INSERT INTO VMS_OTPTracking
                    (MobileNo, EmailAddress, MobileOTP, EmailOTP, ExpiresAt)
                    VALUES
                    (@Mobile, '', @MobileOTP, 'N/A', DATEADD(minute, 10, GETDATE()))";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Mobile", mobile);
                    cmd.Parameters.AddWithValue("@MobileOTP", mobileOTP);
                    cmd.ExecuteNonQuery();
                }
            }

            SendOtpSms(mobile, mobileOTP);

            lblDisplayMobile.Text = "+91 " + mobile;
            lblDisplayEmail.Text = string.Empty;

            pnlRequestOTP.Visible = false;
            pnlVerifyOTP.Visible = true;
            pnlEmailOTPInput.Visible = false;
            lblMessage.Text = string.Empty;
            lblMessage.ForeColor = Color.Green;
            lblMessage.Text = "Verification code sent to your mobile number.";
        }

        protected void btnVerifyOTP_Click(object sender, EventArgs e)
        {
            int attempts = Session["OTPAttempts"] != null ? Convert.ToInt32(Session["OTPAttempts"]) : 0;
            if (attempts >= 5)
            {
                lblMessage.ForeColor = Color.Red;
                lblMessage.Text = "Too many failed attempts. Please request a new verification code.";
                return;
            }

            string mobile = txtMobile.Text.Trim();
            string enteredMobileOTP = txtMobileOTP.Text.Trim();
            bool isValid = false;

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();
                string query = @"
                    SELECT Id FROM VMS_OTPTracking
                    WHERE MobileNo = @Mobile
                      AND EmailAddress = ''
                      AND MobileOTP = @MobileOTP
                      AND EmailOTP = 'N/A'
                      AND IsVerified = 0
                      AND ExpiresAt > GETDATE()";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Mobile", mobile);
                    cmd.Parameters.AddWithValue("@MobileOTP", enteredMobileOTP);

                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        isValid = true;
                        string updateQuery = "UPDATE VMS_OTPTracking SET IsVerified = 1 WHERE Id = @Id";
                        using (SqlCommand updateCmd = new SqlCommand(updateQuery, con))
                        {
                            updateCmd.Parameters.AddWithValue("@Id", result.ToString());
                            updateCmd.ExecuteNonQuery();
                        }
                    }
                }
            }

            if (!isValid)
            {
                Session["OTPAttempts"] = attempts + 1;
                lblMessage.ForeColor = Color.Red;
                lblMessage.Text = $"Invalid verification code. Attempt {attempts + 1} of 5.";
                return;
            }

            Session.Remove("OTPAttempts");
            Session["VerifiedMobile"] = mobile;
            Session["VerifiedEmail"] = string.Empty;

            string branchId = hfBranchId.Value;
            if (string.IsNullOrEmpty(branchId) || !int.TryParse(branchId, out int parsedBranchId))
            {
                lblMessage.ForeColor = Color.Red;
                lblMessage.Text = "Session lost. Please scan the physical QR code again.";
                return;
            }

            if (VmsHelper.HasActiveVisit(mobile, string.Empty, parsedBranchId))
            {
                Response.Redirect("VMS_MyVisits.aspx", false);
            }
            else
            {
                Response.Redirect($"VMS_Registration.aspx?BranchId={branchId}", false);
            }

            Context.ApplicationInstance.CompleteRequest();
        }

        protected void lnkResend_Click(object sender, EventArgs e)
        {
            btnSendOTP_Click(sender, e);
            lblMessage.ForeColor = Color.Green;
            lblMessage.Text = "A new verification code has been sent to your mobile number.";
        }

        private void QueueOtpEmail(string email, string otp)
        {
            string subject = "Your Anmol VMS Verification Code";
            string body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd;'>
                    <h2 style='color: #1ABB9C;'>Anmol Visitor Management</h2>
                    <p>Your Email Verification Code for facility check-in is:</p>
                    <h1 style='letter-spacing: 5px; color: #333;'>{otp}</h1>
                    <p style='color: #777; font-size: 12px;'>This code is valid for 10 minutes. Do not share this code.</p>
                </div>";

            using (SqlConnection con = new SqlConnection(connStr))
            {
                string queueQuery = @"
                    INSERT INTO NotificationQueue
                    (FormSubmissionId, EscalationLevel, RecipientEmail, RecipientName, Subject, Body, NotificationType, Status, SourceModule)
                    VALUES
                    (0, 0, @Email, 'Visitor', @Subject, @Body, 'Email', 'Pending', 'VMS_OTP')";

                using (SqlCommand cmd = new SqlCommand(queueQuery, con))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Subject", subject);
                    cmd.Parameters.AddWithValue("@Body", body);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void SendOtpSms(string mobile, string otp)
        {
            try
            {
                string username = "anmolb";
                string password = "Support@2023";
                string senderId = "ANMOLB";
                string rawMessage = $"OTP to login into your Anmol CRM Portal is {otp}. Thanks Team ANMOL INDUSTRIES LTD";
                string encodedMessage = HttpUtility.UrlEncode(rawMessage);

                string url = $"http://api.smscountry.com/SMSCwebservice_bulk.aspx" +
                             $"?User={username}" +
                             $"&passwd={password}" +
                             $"&mobilenumber={mobile}" +
                             $"&message={encodedMessage}" +
                             $"&sid={senderId}" +
                             $"&mtype=N" +
                             $"&DR=Y";

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    System.Diagnostics.Debug.WriteLine($"SMS Sent Successfully. Response: {reader.ReadToEnd()}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SMS API Error: {ex.Message}");
            }
        }
    }
}
