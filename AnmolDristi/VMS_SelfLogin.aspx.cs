using System;
using System.Data.SqlClient;
using System.Configuration;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Web;
using System.Security.Cryptography;
using AnmolDristi.DAL;

namespace AnmolDristi
{
    public partial class VMS_SelfLogin : System.Web.UI.Page
    {
        string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                ResetVisitorSession();

                PreventResponseCaching();

                InitializeBranchFromRequest();
            }
        }

        /// <summary>
        /// Clears abandoned visitor session state and expires the ASP.NET session cookie.
        /// </summary>
        private void ResetVisitorSession()
        {
            Session.Clear();
            Session.Abandon();

            if (Request.Cookies["ASP.NET_SessionId"] != null)
            {
                Response.Cookies["ASP.NET_SessionId"].Expires = DateTime.Now.AddDays(-1);
            }
        }

        /// <summary>
        /// Prevents browser/proxy caching of the visitor OTP landing page.
        /// </summary>
        private void PreventResponseCaching()
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.UtcNow.AddHours(-1));
            Response.Cache.SetNoStore();
        }

        /// <summary>
        /// Binds BranchId from the QR query string, or shows the invalid-QR UI when missing.
        /// </summary>
        private void InitializeBranchFromRequest()
        {
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

        private void LoadBranchName(string branchId)
        {
            try
            {
                int parsedBranchId;
                if (int.TryParse(branchId, out parsedBranchId))
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

        /// <summary>
        /// Persists a newly generated mobile OTP with the existing 10-minute expiry.
        /// </summary>
        private void PersistMobileOtp(string mobile, string otp)
        {
            DateTime expiresAt = DateTime.Now.AddMinutes(10);
            VmsOtpRepository repository = new VmsOtpRepository();
            repository.CreateOtp(mobile, otp, expiresAt);
        }

        /// <summary>
        /// Page-layer wrapper for mobile OTP verification against VMS_OTPTracking.
        /// </summary>
        private bool ValidateMobileOtp(string mobile, string enteredMobileOtp)
        {
            return new VmsOtpRepository().ValidateOtp(
                mobile,
                enteredMobileOtp);
        }

        /// <summary>
        /// Generates a cryptographically secure 6-digit OTP in the range 100000-999999.
        /// </summary>
        private string GenerateSecureOtp()
        {
            const int minInclusive = 100000;
            const int rangeSize = 900000; // 100000..999999 inclusive
            byte[] bytes = new byte[4];

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                uint value;
                uint exclusiveLimit = uint.MaxValue - (uint.MaxValue % (uint)rangeSize);
                do
                {
                    rng.GetBytes(bytes);
                    value = BitConverter.ToUInt32(bytes, 0);
                }
                while (value >= exclusiveLimit);

                int otp = minInclusive + (int)(value % (uint)rangeSize);
                return otp.ToString();
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

            string mobileOTP = GenerateSecureOtp();

            PersistMobileOtp(mobile, mobileOTP);

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
            if (!ValidateOtpAttemptLimit())
                return;

            string mobile = txtMobile.Text.Trim();
            string enteredMobileOTP = txtMobileOTP.Text.Trim();
            bool isValid = ValidateMobileOtp(
                mobile,
                enteredMobileOTP);

            if (!isValid)
            {
                Session["OTPAttempts"] = attempts + 1;
                lblMessage.ForeColor = Color.Red;
                lblMessage.Text = $"Invalid verification code. Attempt {attempts + 1} of 5.";
                return;
            }

            InitializeVerifiedSession(mobile);

            ResolveVerifiedVisitor(
                mobile,
                hfBranchId.Value);
        }

        protected void lnkResend_Click(object sender, EventArgs e)
        {
            btnSendOTP_Click(sender, e);
            lblMessage.ForeColor = Color.Green;
            lblMessage.Text = "A new verification code has been sent to your mobile number.";
        }

        /// <summary>
        /// Enforces the visitor OTP failed-attempt limit stored in Session["OTPAttempts"].
        /// </summary>
        private bool ValidateOtpAttemptLimit()
        {
            int attempts = Session["OTPAttempts"] != null ? Convert.ToInt32(Session["OTPAttempts"]) : 0;
            if (attempts >= 5)
            {
                lblMessage.ForeColor = Color.Red;
                lblMessage.Text = "Too many failed attempts. Please request a new verification code.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Clears OTP attempt state and stores the verified visitor mobile in Session.
        /// </summary>
        private void InitializeVerifiedSession(string mobile)
        {
            Session.Remove("OTPAttempts");
            Session["VerifiedMobile"] = mobile;
            Session["VerifiedEmail"] = string.Empty;
        }

        /// <summary>
        /// Routes a verified visitor to MyVisits or Registration based on BranchId and active visit.
        /// </summary>
        private void ResolveVerifiedVisitor(string mobile, string branchId)
        {
            int parsedBranchId;
            if (string.IsNullOrEmpty(branchId) || !int.TryParse(branchId, out parsedBranchId))
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
            string username = ConfigurationManager.AppSettings["VmsSmsUsername"];
            string password = ConfigurationManager.AppSettings["VmsSmsPassword"];
            string senderId = ConfigurationManager.AppSettings["VmsSmsSenderId"];
            string apiEndpoint = ConfigurationManager.AppSettings["VmsSmsApiEndpoint"];

            string rawMessage =
                $"OTP to login into your Anmol CRM Portal is {otp}. Thanks Team ANMOL INDUSTRIES LTD";

            string url =
                $"{apiEndpoint}" +
                $"?User={HttpUtility.UrlEncode(username)}" +
                $"&passwd={HttpUtility.UrlEncode(password)}" +
                $"&mobilenumber={HttpUtility.UrlEncode(mobile)}" +
                $"&message={HttpUtility.UrlEncode(rawMessage)}" +
                $"&sid={HttpUtility.UrlEncode(senderId)}" +
                $"&mtype=N" +
                $"&DR=Y";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = 30000;
            request.ReadWriteTimeout = 30000;
            request.UserAgent = "AnmolDristi VMS";

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                string apiResponse = reader.ReadToEnd();

                if (!apiResponse.StartsWith("OK:", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ApplicationException(
                        $"SMSCountry rejected the request. Response: {apiResponse}");
                }
            }
        }
    }
}
