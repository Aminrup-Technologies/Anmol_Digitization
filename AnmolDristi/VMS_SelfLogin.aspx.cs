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
            if (!IsPostBack) // Only run on the very first page load
            {
                // 🌟 FIX 1: THE SESSION KILL-SWITCH
                // Destroy any lingering sessions from previous visitors on this device
                Session.Clear();
                Session.Abandon();

                // 🌟 FIX 2: PREVENT MOBILE BROWSER CACHING
                // Forces mobile WebViews to fetch a fresh page instead of a cached redirect
                Response.Cache.SetCacheability(HttpCacheability.NoCache);
                Response.Cache.SetExpires(DateTime.UtcNow.AddHours(-1));
                Response.Cache.SetNoStore();

                // Clear auth cookies if you are using Forms Authentication
                if (Request.Cookies["ASP.NET_SessionId"] != null)
                {
                    Response.Cookies["ASP.NET_SessionId"].Expires = DateTime.Now.AddDays(-1);
                }

                if (Request.QueryString["BranchId"] != null)
                {
                    string branchId = Request.QueryString["BranchId"].ToString();

                    // Lock the ID into the hidden field so it survives postbacks
                    hfBranchId.Value = branchId;

                    // Fetch and Display the Branch Name dynamically
                    LoadBranchName(branchId);
                }
                else
                {
                    // If they didn't scan the QR code properly, block them immediately
                    lblMessage.ForeColor = System.Drawing.Color.Red;
                    lblMessage.Text = "Invalid QR Code. Missing Location ID.";
                    pnlRequestOTP.Visible = false;
                }
            }
        }

        //protected void Page_Load(object sender, EventArgs e)
        //{
        //    if (!IsPostBack) // Only run on the very first page load
        //    {
        //        if (Request.QueryString["BranchId"] != null)
        //        {
        //            string branchId = Request.QueryString["BranchId"].ToString();

        //            // Lock the ID into the hidden field so it survives postbacks
        //            hfBranchId.Value = branchId;

        //            // Fetch and Display the Branch Name dynamically
        //            LoadBranchName(branchId);
        //        }
        //        else
        //        {
        //            // If they didn't scan the QR code properly, block them immediately
        //            lblMessage.ForeColor = System.Drawing.Color.Red;
        //            lblMessage.Text = "Invalid QR Code. Missing Location ID.";
        //            pnlRequestOTP.Visible = false;
        //        }
        //    }
        //}

        private void LoadBranchName(string branchId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connStr))
                {
                    // Assuming your plant details are mapped in MST_PlantDetails
                    string query = "SELECT plant_name FROM MST_PlantDetails WHERE plant_id = @BranchId";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@BranchId", branchId);
                        con.Open();
                        object result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            // Update the UI Label with the actual facility name
                            lblBranchName.Text = result.ToString();
                        }
                        con.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                // Safe fallback text if the DB query fails so the UI doesn't crash
                lblBranchName.Text = "our Facility";
                System.Diagnostics.Debug.WriteLine($"Error fetching branch name: {ex.Message}");
            }
        }

        protected void btnSendOTP_Click(object sender, EventArgs e)
        {
            string mobile = txtMobile.Text.Trim();
            string email = txtEmail.Text.Trim();

            // 1. Strict Validation
            if (mobile.Length != 10 || !Regex.IsMatch(mobile, @"^\d{10}$"))
            {
                lblMessage.ForeColor = Color.Red;
                lblMessage.Text = "Please enter a valid 10-digit Mobile Number.";
                return;
            }
            //if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            //{
            //    lblMessage.ForeColor = Color.Red;
            //    lblMessage.Text = "Please enter a valid Email Address.";
            //    return;
            //}

            if (!string.IsNullOrEmpty(email) && !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                lblMessage.ForeColor = Color.Red;
                lblMessage.Text = "Please enter a valid Email Address.";
                return;
            }

            // 2. Generate TWO separate OTPs
            //Random rand = new Random();
            //string mobileOTP = rand.Next(100000, 999999).ToString();
            //string emailOTP = rand.Next(100000, 999999).ToString();
            Random rand = new Random();
            string mobileOTP = rand.Next(100000, 999999).ToString();
            string emailOTP = string.IsNullOrEmpty(email) ? "N/A" : rand.Next(100000, 999999).ToString();

            // 3. Save BOTH to a SINGLE Row in the Database
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();
                string query = @"
            INSERT INTO VMS_OTPTracking 
            (MobileNo, EmailAddress, MobileOTP, EmailOTP, ExpiresAt) 
            VALUES 
            (@Mobile, @Email, @MobileOTP, @EmailOTP, DATEADD(minute, 10, GETDATE()))";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Mobile", mobile);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@MobileOTP", mobileOTP);
                    cmd.Parameters.AddWithValue("@EmailOTP", emailOTP);
                    cmd.ExecuteNonQuery();
                }
                con.Close();
            }

            //// 4. Dispatch Both
            //SendOtpSms(mobile, mobileOTP);
            //QueueOtpEmail(email, emailOTP);

            // Dispatch
            SendOtpSms(mobile, mobileOTP);
            if (!string.IsNullOrEmpty(email))
            {
                QueueOtpEmail(email, emailOTP);
            }

            // 5. Update UI
            //lblDisplayMobile.Text = "+91 " + mobile;
            //lblDisplayEmail.Text = email;
            lblDisplayMobile.Text = "+91 " + mobile;
            lblDisplayEmail.Text = string.IsNullOrEmpty(email) ? "(No Email Provided)" : email;

            pnlRequestOTP.Visible = false;
            pnlVerifyOTP.Visible = true;
            pnlEmailOTPInput.Visible = !string.IsNullOrEmpty(email);
            lblMessage.Text = "";

            // [DEV ONLY] - Remove this line in production!
            lblMessage.ForeColor = Color.Green;
            lblMessage.Text = $"[DEV] Mobile OTP: {mobileOTP} | Email OTP: {emailOTP}";
        }

        protected void btnVerifyOTP_Click_OLD(object sender, EventArgs e)
        {
            string mobile = txtMobile.Text.Trim();
            string email = txtEmail.Text.Trim();
            string enteredMobileOTP = txtMobileOTP.Text.Trim();
            string enteredEmailOTP = txtEmailOTP.Text.Trim();

            bool isValid = false;
            string otpTrackingId = "";

            // 1. Verify BOTH OTPs against the OTP Tracking Table
            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();
                string query = @"
            SELECT Id FROM VMS_OTPTracking 
            WHERE MobileNo = @Mobile 
              AND (EmailAddress = @Email OR @Email = '')
              AND MobileOTP = @MobileOTP 
              AND EmailOTP = @EmailOTP
              AND IsVerified = 0 
              AND ExpiresAt > GETDATE()";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Mobile", mobile);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@MobileOTP", enteredMobileOTP);

                    // If email was left blank, the DB stored "N/A" for the OTP. Match that.
                    string emailOtpToMatch = string.IsNullOrEmpty(email) ? "N/A" : enteredEmailOTP;
                    cmd.Parameters.AddWithValue("@EmailOTP", emailOtpToMatch);

                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        isValid = true;
                        otpTrackingId = result.ToString();

                        // Securely mark the OTP as used to prevent replay attacks
                        string updateQuery = "UPDATE VMS_OTPTracking SET IsVerified = 1 WHERE Id = @Id";
                        using (SqlCommand updateCmd = new SqlCommand(updateQuery, con))
                        {
                            updateCmd.Parameters.AddWithValue("@Id", otpTrackingId);
                            updateCmd.ExecuteNonQuery();
                        }
                    }
                }
            }

            // 2. Route user based on validation results
            if (isValid)
            {
                // Store BOTH session variables for VMS_Registration to use
                Session["VerifiedMobile"] = mobile;
                Session["VerifiedEmail"] = email;

                // Read the BranchId securely from the Hidden Field
                string branchId = hfBranchId.Value;

                // *** CRITICAL CHECK TO JUSTIFY REGISTRATION PAGE_LOAD ***
                if (string.IsNullOrEmpty(branchId))
                {
                    lblMessage.ForeColor = System.Drawing.Color.Red;
                    lblMessage.Text = "Session lost. Please scan the physical QR code at the reception again.";
                    return;
                }

                bool hasActiveVisitToday = false;

                // 3. CHECKPOINT: Prevent Duplicates
                using (SqlConnection con = new SqlConnection(connStr))
                {
                    // Checking if they already have an active pass right now
                    string checkQuery = @"
                SELECT TOP 1 LogID 
                FROM VMS_VisitLog 
                WHERE (VisitorMobile = @Mobile OR VisitorEmail = @Email)
                  AND TargetBranchID = @BranchId
                  AND ApprovalStatus IN ('Pending', 'Approved')
                  AND ISNULL(VisitStatus, '') != 'Checked-Out'
                  AND ValidUpto >= GETDATE()";

                    using (SqlCommand cmdCheck = new SqlCommand(checkQuery, con))
                    {
                        cmdCheck.Parameters.AddWithValue("@Mobile", mobile);
                        cmdCheck.Parameters.AddWithValue("@Email", email);
                        cmdCheck.Parameters.AddWithValue("@BranchId", branchId);

                        con.Open();
                        if (cmdCheck.ExecuteScalar() != null)
                        {
                            hasActiveVisitToday = true;
                        }
                    }
                }

                // 4. Redirect based on duplicate check
                if (hasActiveVisitToday)
                {
                    Response.Redirect("VMS_MyVisits.aspx", false);
                    Context.ApplicationInstance.CompleteRequest();
                }
                else
                {
                    Response.Redirect($"VMS_Registration.aspx?BranchId={branchId}", false);
                    Context.ApplicationInstance.CompleteRequest();
                }
            }
            else
            {
                lblMessage.ForeColor = System.Drawing.Color.Red;
                lblMessage.Text = "Invalid OTPs or session expired. Please check both codes and try again.";
            }
        }

        protected void btnVerifyOTP_Click(object sender, EventArgs e)
        {
            // 1. Anti Brute-Force Rate Limiter
            int attempts = Session["OTPAttempts"] != null ? Convert.ToInt32(Session["OTPAttempts"]) : 0;
            if (attempts >= 5)
            {
                lblMessage.ForeColor = Color.Red;
                lblMessage.Text = "Too many failed attempts. Please request new verification codes.";
                return;
            }

            string mobile = txtMobile.Text.Trim();
            string email = txtEmail.Text.Trim();
            string enteredMobileOTP = txtMobileOTP.Text.Trim();
            string enteredEmailOTP = txtEmailOTP.Text.Trim();

            bool isValid = false;
            string otpTrackingId = "";

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();
                string query = @"
            SELECT Id FROM VMS_OTPTracking 
            WHERE MobileNo = @Mobile 
              AND EmailAddress = @Email
              AND MobileOTP = @MobileOTP 
              AND EmailOTP = @EmailOTP
              AND IsVerified = 0 
              AND ExpiresAt > GETDATE()";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Mobile", mobile);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@MobileOTP", enteredMobileOTP);
                    cmd.Parameters.AddWithValue("@EmailOTP", string.IsNullOrEmpty(email) ? "N/A" : enteredEmailOTP);

                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        isValid = true;
                        otpTrackingId = result.ToString();

                        string updateQuery = "UPDATE VMS_OTPTracking SET IsVerified = 1 WHERE Id = @Id";
                        using (SqlCommand updateCmd = new SqlCommand(updateQuery, con))
                        {
                            updateCmd.Parameters.AddWithValue("@Id", otpTrackingId);
                            updateCmd.ExecuteNonQuery();
                        }
                    }
                }
            }

            if (isValid)
            {
                Session.Remove("OTPAttempts");
                Session["VerifiedMobile"] = mobile;
                Session["VerifiedEmail"] = email;

                string branchId = hfBranchId.Value;
                if (string.IsNullOrEmpty(branchId))
                {
                    lblMessage.ForeColor = Color.Red;
                    lblMessage.Text = "Session lost. Please scan the physical QR code again.";
                    return;
                }

                // Duplicate Check Query with strict Email logic
                bool hasActiveVisit = false;
                using (SqlConnection con = new SqlConnection(connStr))
                {
                    string checkQuery = @"
                SELECT TOP 1 LogID FROM VMS_VisitLog 
                WHERE (VisitorMobile = @Mobile OR (@Email <> '' AND VisitorEmail = @Email))
                  AND TargetBranchID = @BranchId
                  AND ApprovalStatus IN ('Pending', 'Approved')
                  AND ISNULL(VisitStatus, '') != 'Checked-Out'
                  AND ValidUpto >= GETDATE()";

                    using (SqlCommand cmdCheck = new SqlCommand(checkQuery, con))
                    {
                        cmdCheck.Parameters.AddWithValue("@Mobile", mobile);
                        cmdCheck.Parameters.AddWithValue("@Email", email);
                        cmdCheck.Parameters.AddWithValue("@BranchId", branchId);

                        con.Open();
                        if (cmdCheck.ExecuteScalar() != null)
                        {
                            hasActiveVisit = true;
                        }
                    }
                }

                if (hasActiveVisit)
                {
                    Response.Redirect("VMS_MyVisits.aspx", false);
                }
                else
                {
                    Response.Redirect($"VMS_Registration.aspx?BranchId={branchId}", false);
                }
                Context.ApplicationInstance.CompleteRequest();
            }
            else
            {
                Session["OTPAttempts"] = attempts + 1;
                lblMessage.ForeColor = Color.Red;
                lblMessage.Text = $"Invalid verification codes. Attempt {attempts + 1} of 5.";
            }
        }

        protected void lnkResend_Click(object sender, EventArgs e)
        {
            btnSendOTP_Click(sender, e);
            lblMessage.ForeColor = Color.Green;
            lblMessage.Text = "New OTPs have been sent to your Mobile and Email.";
        }

        // --- DISPATCH METHODS ---

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
                // 1. SMSCountry Account Credentials
                string username = "anmolb"; // Replace with your actual API User
                string password = "Support@2023"; // Replace with your actual API Password

                // 2. DLT Approved Header (Sender ID)
                string senderId = "ANMOLB";

                // 3. DLT Approved Template ID (Often required by Indian gateways to bypass DLT scrubbers)
                string dltTemplateId = "1007583097816273950";

                // 4. Construct Exact Message (Replacing {#VAR} with the 6-digit OTP)
                // CRITICAL: Do not add extra spaces, line breaks, or punctuation that are not in the approved template.
                string rawMessage = $"OTP to login into your Anmol CRM Portal is {otp}. Thanks Team ANMOL INDUSTRIES LTD";

                // URL Encode the message to safely transmit spaces and special characters via HTTP GET
                string encodedMessage = HttpUtility.UrlEncode(rawMessage);

                // 5. Construct the SMSCountry HTTP API URL
                string url = $"http://api.smscountry.com/SMSCwebservice_bulk.aspx" +
                             $"?User={username}" +
                             $"&passwd={password}" +
                             $"&mobilenumber={mobile}" +
                             $"&message={encodedMessage}" +
                             $"&sid={senderId}" +
                             $"&mtype=N" + // N = Normal Text
                             $"&DR=Y";    // Y = Delivery Report Required

                // 6. Execute the HTTP Request
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        // The responseString will contain the SMS_JOB_NO (e.g., "OK: 123456789")
                        string responseString = reader.ReadToEnd();

                        // Optional: You can log this responseString to a DB table to track delivery statuses
                        System.Diagnostics.Debug.WriteLine($"SMS Sent Successfully. Response: {responseString}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log any connectivity issues without crashing the web page
                System.Diagnostics.Debug.WriteLine($"SMS API Error: {ex.Message}");
            }
        }
    }
}