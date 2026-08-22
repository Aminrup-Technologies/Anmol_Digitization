using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Configuration;
using System.IO;
using System.Web.Services;
using System.Web.Script.Services;
using System.Drawing;
using System.Drawing.Imaging;
using AnmolDristi.DAL;

// 2026-08-22: Added System.Drawing and System.Drawing.Imaging for server-side image validation.

namespace AnmolDristi
{
    public partial class VMS_Registration : System.Web.UI.Page
    {
        static string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        // ==========================================================
        // 2026-08-22: Named constants for photo validation thresholds.
        // Centralizes magic numbers for maintainability and clarity.
        // ==========================================================
        private const int MinPhotoSizeBytes = 5 * 1024;           // 5KB — reject tiny/blank images
        private const int MaxPhotoSizeBytes = 5 * 1024 * 1024;    // 5MB — prevent oversized uploads
        private const int MinPhotoDimension = 240;                 // px — face must be large enough
        private const int MaxPhotoDimension = 4096;                // px — prevent memory abuse
        private const double MaxAspectRatio = 3.0;                 // ratio — reject extreme panoramas
        private const int VarianceSampleGrid = 10;                 // 10x10 grid for pixel sampling
        private const double MinImageVariance = 350.0;             // below this = near-blank/uniform image

        protected void Page_Load(object sender, EventArgs e)
        {
            // 2026-08-22: rfvCompany lifecycle — re-evaluate on EVERY postback (not just initial load)
            // because ViewState preserves Enabled=false from a previous "I Work Here" submission.
            // When Purpose changes away from "I Work Here", rfvCompany must re-enable so external
            // visitors cannot bypass Company validation.
            bool isInternalNow = (ddlPurpose.SelectedValue == "I Work Here");
            rfvCompany.Enabled = !isInternalNow;

            if (!IsPostBack)
            {
                if (!InitializeRegistrationRequest())
                    return;

                InitializeRegistrationPage();
            }
        }

        /// <summary>
        /// Validates OTP session and BranchId, binds hfBranchId.
        /// Does not host-blind redirect: an existing pass for Host A must not block
        /// a new request to Host B. Duplicate checks run at submit against the selected host.
        /// </summary>
        private bool InitializeRegistrationRequest()
        {
            // 1. Existing Security Check (Ensure they passed OTP)
            if (Session["VerifiedMobile"] == null ||
                string.IsNullOrEmpty(Session["VerifiedMobile"].ToString()) ||
                Request.QueryString["BranchId"] == null)
            {
                Response.Redirect("VMS_SelfLogin.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return false;
            }

            hfBranchId.Value = Request.QueryString["BranchId"].ToString();
            int parsedBranchId;
            if (int.TryParse(hfBranchId.Value, out parsedBranchId) && parsedBranchId > 0)
                Session["VmsBranchId"] = parsedBranchId;
            return true;
        }

        /// <summary>
        /// Binds facility flag, quick hosts, contact fields, and default visit schedule controls.
        /// </summary>
        private void InitializeRegistrationPage()
        {
            string closedMsg;
            bool isOpen = VmsHelper.IsFacilityOpen(Convert.ToInt32(hfBranchId.Value), out closedMsg);
            hfIsFacilityOpen.Value = isOpen.ToString().ToLower();

            // Bind quick-select host chips from branch preconfigured hosts
            BindQuickHosts();

            // 3. Safely Pre-fill the contact fields (Existing Logic)
            if (Session["VerifiedMobile"] != null && !string.IsNullOrEmpty(Session["VerifiedMobile"].ToString()))
            {
                txtMobile.Text = Session["VerifiedMobile"].ToString();
                txtMobile.ReadOnly = true;
            }

            txtEmail.ReadOnly = false;

            string today = DateTime.Now.ToString("yyyy-MM-dd");
            txtVisitFromDate.Text = today;
            txtVisitToDate.Text = today;
            txtTimeFrom.Text = "09:00";
            txtTimeTo.Text = "18:00";
        }

        /// <summary>
        /// Persists the captured selfie from hfSelfieBase64 and returns the relative PhotoPath.
        /// 2026-08-22: Rewritten with comprehensive image validation.
        /// Validates: MIME type, Base64 integrity, image decodability, minimum dimensions,
        /// maximum payload, minimum useful size, and reasonable aspect ratio.
        /// </summary>
        private string PersistSelfie()
        {
            string base64Data = hfSelfieBase64.Value;

            // 2026-08-22: Reject empty photo payload
            if (string.IsNullOrWhiteSpace(base64Data))
                throw new Exception("No photo captured. Please take a photo using the camera.");

            // 2026-08-22: Validate and extract MIME type
            string detectedMime = "";
            if (base64Data.StartsWith("data:image/png;base64,"))
            {
                detectedMime = "image/png";
                base64Data = base64Data.Substring("data:image/png;base64,".Length);
            }
            else if (base64Data.StartsWith("data:image/jpeg;base64,") ||
                     base64Data.StartsWith("data:image/jpg;base64,"))
            {
                detectedMime = "image/jpeg";
                base64Data = base64Data.Substring(base64Data.IndexOf(",") + 1);
            }
            else
            {
                // 2026-08-22: Reject unrecognized MIME types to prevent arbitrary file uploads
                throw new Exception("Invalid photo format. Only PNG and JPEG images are accepted.");
            }

            // 2026-08-22: Validate Base64 integrity
            byte[] imageBytes;
            try
            {
                imageBytes = Convert.FromBase64String(base64Data);
            }
            catch (FormatException)
            {
                throw new Exception("Corrupted photo data. Please retake your photo.");
            }

            // 2026-08-22: Validate minimum payload size (reject tiny/blank images)
            if (imageBytes.Length < MinPhotoSizeBytes)
            {
                throw new Exception("Photo is too small or appears to be blank. Please retake a clear photo.");
            }

            // 2026-08-22: Validate maximum payload size (prevent oversized uploads)
            if (imageBytes.Length > MaxPhotoSizeBytes)
            {
                throw new Exception("Photo exceeds maximum size of 5MB. Please use a smaller image.");
            }

            // 2026-08-22: Validate image decodability and minimum dimensions
            ValidateImageDimensions(imageBytes, detectedMime);

            string folderPath = Server.MapPath("~/Uploads/VisitorPhotos/");

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string extension = detectedMime == "image/jpeg" ? ".jpg" : ".png";
            string fileName = $"Vis_{DateTime.Now.Ticks}{extension}";

            string physicalFullPath = Path.Combine(folderPath, fileName);

            File.WriteAllBytes(physicalFullPath, imageBytes);

            return "/Uploads/VisitorPhotos/" + fileName;
        }

        /// <summary>
        /// 2026-08-22: Validates decoded image bytes for decodability, minimum dimensions,
        /// reasonable aspect ratio, and image variance (rejects near-blank/uniform images).
        /// Uses System.Drawing for decode verification.
        /// </summary>
        private void ValidateImageDimensions(byte[] imageBytes, string mimeType)
        {
            using (MemoryStream ms = new MemoryStream(imageBytes))
            {
                Bitmap bmp;
                try
                {
                    bmp = new Bitmap(ms);
                }
                catch (Exception)
                {
                    // 2026-08-22: Image could not be decoded — corrupted, truncated, or format mismatch
                    throw new Exception("Photo could not be read. Please retake your photo.");
                }

                using (bmp)
                {
                    int width = bmp.Width;
                    int height = bmp.Height;

                    // 2026-08-22: Minimum dimension check (face must be large enough for identification)
                    if (width < MinPhotoDimension || height < MinPhotoDimension)
                    {
                        throw new Exception(
                            "Photo resolution too low for identification. " +
                            "Please ensure your face is clearly visible and close to the camera.");
                    }

                    // 2026-08-22: Maximum dimension check (prevent memory abuse)
                    if (width > MaxPhotoDimension || height > MaxPhotoDimension)
                    {
                        throw new Exception($"Photo dimensions too large. Maximum is {MaxPhotoDimension}x{MaxPhotoDimension}.");
                    }

                    // 2026-08-22: Reasonable aspect ratio check (reject extreme panoramas)
                    double ratio = (double)Math.Max(width, height) / Math.Min(width, height);
                    if (ratio > MaxAspectRatio)
                    {
                        throw new Exception("Photo aspect ratio is unusual. Please retake a standard photo.");
                    }

                    // 2026-08-22: Low-variance (near-blank image) rejection
                    // Samples a grid of pixels and calculates color variance. A near-blank image
                    // (white screen, hand covering lens, dark room) will have very low variance.
                    // Threshold of 350 chosen to reject uniformly colored images while accepting
                    // normal photos which typically have variance > 1000.
                    ValidateImageVariance(bmp, width, height);
                }
            }
        }

        /// <summary>
        /// 2026-08-22: Rejects near-blank or uniformly colored images by sampling a pixel grid
        /// and calculating RGB variance. A real face photo will have significant color variation
        /// (skin tones, background, features). A blank white/dark screen or hand covering the
        /// lens will have very low variance.
        /// </summary>
        private void ValidateImageVariance(Bitmap bmp, int width, int height)
        {
            // Sample a grid of pixels across the image for variance analysis
            int sampleCount = 0;
            double sumR = 0, sumG = 0, sumB = 0;
            int gridSize = VarianceSampleGrid;

            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    int x = (col * width) / gridSize + (width / (gridSize * 2));
                    int y = (row * height) / gridSize + (height / (gridSize * 2));
                    x = Math.Min(x, width - 1);
                    y = Math.Min(y, height - 1);

                    Color pixel = bmp.GetPixel(x, y);
                    sumR += pixel.R;
                    sumG += pixel.G;
                    sumB += pixel.B;
                    sampleCount++;
                }
            }

            double avgR = sumR / sampleCount;
            double avgG = sumG / sampleCount;
            double avgB = sumB / sampleCount;

            // Calculate variance across sampled pixels
            double varR = 0, varG = 0, varB = 0;
            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    int x = (col * width) / gridSize + (width / (gridSize * 2));
                    int y = (row * height) / gridSize + (height / (gridSize * 2));
                    x = Math.Min(x, width - 1);
                    y = Math.Min(y, height - 1);

                    Color pixel = bmp.GetPixel(x, y);
                    varR += (pixel.R - avgR) * (pixel.R - avgR);
                    varG += (pixel.G - avgG) * (pixel.G - avgG);
                    varB += (pixel.B - avgB) * (pixel.B - avgB);
                }
            }

            double totalVariance = (varR + varG + varB) / sampleCount;

            // Threshold: images with variance below MinImageVariance are likely blank,
            // uniformly colored, or have a hand/object covering the lens. Normal face
            // photos typically have variance > 1000 due to skin tones and features.
            if (totalVariance < MinImageVariance)
            {
                throw new Exception(
                    "Photo appears to be blank or uniformly colored. " +
                    "Please ensure your face is clearly visible and well-lit.");
            }
        }

        /// <summary>
        /// Queues host approval/FYI, escalation CC, and optional visitor pass notifications.
        /// </summary>
        private void QueueNotifications(
            int visitId,
            string hostEmpCode,
            string fullName,
            string currentApprovalStatus,
            string scheduleSummary,
            string visitorEmail,
            string visitorMobile,
            object qrTokenParam,
            bool shouldQueueVisitorPass)
        {
            string hostEmail = "";
            string hostName = "";

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                // ==========================================================
                // 12. FETCH HOST DETAILS
                // ==========================================================
                string hostQuery = @"
                            SELECT
                            EmployeeName,
                            Email
                            FROM MST_UserMaster
                            WHERE EmployeeCode=@HostCode";

                using (SqlCommand hostCmd =
                    new SqlCommand(hostQuery, con))
                {
                    hostCmd.Parameters.AddWithValue(
                        "@HostCode",
                        hostEmpCode);

                    using (SqlDataReader dr =
                        hostCmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            hostName =
                                dr["EmployeeName"].ToString();

                            hostEmail =
                                dr["Email"].ToString();
                        }
                    }
                }

                // ==========================================================
                // 13. FALLBACK EMAIL LOGIC
                // ==========================================================
                bool isFallbackEmail = false;

                if (string.IsNullOrWhiteSpace(hostEmail))
                {
                    hostEmail = "dgtlautomation@anmolindustries.com";
                    isFallbackEmail = true;
                }

                string fallbackNote = "";

                if (isFallbackEmail)
                {
                    fallbackNote =
                            $@"<p style='color:red;font-size:12px;'>
                                <em>
                                System Note:
                                This email was routed to the fallback address because
                                no valid email was found for Host Code:
                                {hostEmpCode}.
                                </em>
                                </p>";
                }

                // ==========================================================
                // 14. QUEUE EMAIL NOTIFICATION
                // ==========================================================

                if (!string.IsNullOrWhiteSpace(hostEmail))
                {
                    string subject;
                    string body;
                    string sourceModule;

                    if (currentApprovalStatus == "Pending")
                    {
                        subject = $"Approval Needed: Visitor {fullName} has arrived";

                        if (isFallbackEmail)
                            subject = "[FALLBACK] " + subject;

                        string quickApproveLink =
                            VmsHelper.BuildHostQuickLoginUrl(visitId, hostEmpCode);

                        body = $@"
                                            <div style='font-family:Arial;padding:20px;'>

                                            {fallbackNote}

                                            <h3 style='color:#1ABB9C;'>
                                            Hello {hostName},
                                            </h3>

                                            <p>
                                            <strong>{fullName}</strong>
                                            from
                                            <strong>{txtCompany.Text}</strong>
                                            has requested a visit on:
                                            <strong>{scheduleSummary}</strong>
                                            </p>

                                            <br/>

                                            <a href='{quickApproveLink}'
                                            style='background:#1ABB9C;
                                            color:white;
                                            padding:12px 25px;
                                            text-decoration:none;
                                            border-radius:5px;
                                            font-weight:bold;'>

                                            Review & Approve Visitor

                                            </a>

                                            </div>";

                        sourceModule = "VMS_APPROVAL";
                    }
                    else
                    {
                        subject =
                            $"FYI: Internal Colleague {fullName} has arrived";

                        if (isFallbackEmail)
                            subject = "[FALLBACK] " + subject;

                        body = $@"
                                        <div style='font-family:Arial;padding:20px;'>

                                        {fallbackNote}

                                        <h3 style='color:#1ABB9C;'>

                                        Hello {hostName},

                                        </h3>

                                        <p>

                                        <strong>{fullName}</strong>

                                        from

                                        <strong>{txtCompany.Text}</strong>

                                        (Internal Employee)

                                        is currently at the reception to meet you.

                                        </p>

                                        <p style='color:#73879C;'>

                                        <em>

                                        Their visitor pass has already been automatically approved.

                                        They will check in at the security gate using their digital pass.

                                        </em>

                                        </p>

                                        </div>";

                        sourceModule = "VMS_FYI_ARRIVAL";
                    }

                    string queueQuery = @"

                                    INSERT INTO NotificationQueue
                                    (
                                    FormSubmissionId,
                                    EscalationLevel,
                                    RecipientEmail,
                                    RecipientName,
                                    Subject,
                                    Body,
                                    NotificationType,
                                    Status,
                                    SourceModule
                                    )

                                    VALUES
                                    (
                                    @VisitId,
                                    0,
                                    @Email,
                                    @Name,
                                    @Subject,
                                    @Body,
                                    'Email',
                                    'Pending',
                                    @SourceModule
                                    )";

                    using (SqlCommand qCmd =
                        new SqlCommand(queueQuery, con))
                    {
                        qCmd.Parameters.AddWithValue("@VisitId", visitId);
                        qCmd.Parameters.AddWithValue("@Email", hostEmail);
                        qCmd.Parameters.AddWithValue("@Name", hostName);
                        qCmd.Parameters.AddWithValue("@Subject", subject);
                        qCmd.Parameters.AddWithValue("@Body", body);
                        qCmd.Parameters.AddWithValue("@SourceModule", sourceModule);

                        qCmd.ExecuteNonQuery();
                    }

                    // ==========================================================
                    // 14b. ESCALATION / CC ROUTING (branch EscalationEmailList)
                    // ==========================================================
                    string escalationEmailList = null;
                    using (SqlCommand escCmd = new SqlCommand(@"
                                    SELECT EscalationEmailList
                                    FROM VMS_OfficeSettings
                                    WHERE Branch_ID = @BranchId", con))
                    {
                        escCmd.Parameters.AddWithValue("@BranchId", Convert.ToInt32(hfBranchId.Value));
                        object escResult = escCmd.ExecuteScalar();
                        if (escResult != null && escResult != DBNull.Value)
                            escalationEmailList = escResult.ToString();
                    }

                    if (!string.IsNullOrWhiteSpace(escalationEmailList))
                    {
                        string ccQueueQuery = @"
                                        INSERT INTO NotificationQueue
                                        (
                                            FormSubmissionId,
                                            EscalationLevel,
                                            RecipientEmail,
                                            RecipientName,
                                            Subject,
                                            Body,
                                            NotificationType,
                                            Status,
                                            SourceModule
                                        )
                                        VALUES
                                        (
                                            @VisitId,
                                            0,
                                            @Email,
                                            @Name,
                                            @Subject,
                                            @Body,
                                            'Email',
                                            'Pending',
                                            'VMS_REG_CC'
                                        )";

                        string ccSubject = "[CC] " + subject;
                        string ccRecipientName = "[CC] " + (string.IsNullOrWhiteSpace(hostName) ? "Escalation" : hostName);

                        foreach (string rawEmail in escalationEmailList.Split(','))
                        {
                            string ccEmail = rawEmail.Trim();
                            if (!IsValidNotificationEmail(ccEmail))
                                continue;

                            using (SqlCommand ccCmd = new SqlCommand(ccQueueQuery, con))
                            {
                                ccCmd.Parameters.AddWithValue("@VisitId", visitId);
                                ccCmd.Parameters.AddWithValue("@Email", ccEmail);
                                ccCmd.Parameters.AddWithValue("@Name", ccRecipientName);
                                ccCmd.Parameters.AddWithValue("@Subject", ccSubject);
                                ccCmd.Parameters.AddWithValue("@Body", body);
                                ccCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }

                if (shouldQueueVisitorPass)
                {
                    VmsHelper.QueueVisitorPassNotification(
                        con,
                        visitId,
                        (Guid)qrTokenParam,
                        fullName,
                        visitorEmail,
                        visitorMobile);
                }
            }
        }

        private class ApprovalResolution
        {
            public string ApprovalStatus { get; set; }
            public string VisitStatus { get; set; }
            public object ActualInTime { get; set; }
            public object ApprovedOn { get; set; }
            public string FullName { get; set; }
            public object QrTokenParam { get; set; }
            public bool ShouldQueueVisitorPass { get; set; }
        }

        /// <summary>
        /// Resolves Pending vs internal-employee auto-Approved mode, including QR/pass-queue flags.
        /// </summary>
        private ApprovalResolution ResolveApprovalMode(string fullName)
        {
            ApprovalResolution resolution = new ApprovalResolution
            {
                ApprovalStatus = "Pending",
                VisitStatus = "Scheduled",
                ActualInTime = DBNull.Value,
                ApprovedOn = DBNull.Value,
                FullName = fullName,
                QrTokenParam = DBNull.Value,
                ShouldQueueVisitorPass = false
            };

            // ==========================================================
            // 7. INTERNAL EMPLOYEE AUTO APPROVAL
            // ==========================================================
            if (ddlPurpose.SelectedValue == "I Work Here")
            {
                string internalCode =
                    txtInternalEmpCode.Text.Trim();

                if (string.IsNullOrWhiteSpace(internalCode))
                {
                    throw new Exception(
                        "You must provide your internal Employee Code to self-approve your pass.");
                }

                using (SqlConnection conCheck =
                    new SqlConnection(connStr))
                {
                    conCheck.Open();

                    string empQuery = @"
                        SELECT
                            EmployeeName,
                            Email,
                            Mobile
                        FROM MST_UserMaster
                        WHERE EmployeeCode=@EmpCode
                        AND EmployeeStatus='Active'";

                    using (SqlCommand cmd =
                        new SqlCommand(empQuery, conCheck))
                    {
                        cmd.Parameters.AddWithValue("@EmpCode",
                            internalCode);

                        using (SqlDataReader dr =
                            cmd.ExecuteReader())
                        {
                            if (!dr.Read())
                            {
                                throw new Exception(
                                    "The Employee Code entered is invalid or inactive. Registration denied.");
                            }

                            resolution.ApprovalStatus = "Approved";
                            resolution.VisitStatus = "Scheduled";
                            resolution.ApprovedOn = DateTime.Now;

                            resolution.FullName =
                                dr["EmployeeName"].ToString();

                            string[] parts =
                                resolution.FullName.Split(new[] { ' ' }, 2);

                            txtFirstName.Text =
                                parts.Length > 0
                                ? parts[0]
                                : resolution.FullName;

                            txtLastName.Text =
                                parts.Length > 1
                                ? parts[1]
                                : "";

                            txtCompany.Text =
                                "Anmol Industries Ltd.";

                            resolution.QrTokenParam = Guid.NewGuid();
                            resolution.ShouldQueueVisitorPass = true;
                        }
                    }
                }
            }

            return resolution;
        }

        private void BindQuickHosts()
        {
            int branchId = Convert.ToInt32(hfBranchId.Value);
            List<HostEmployee> quickHosts = GetPreconfiguredHosts(branchId);

            if (quickHosts != null && quickHosts.Count > 0)
            {
                rptQuickHosts.DataSource = quickHosts;
                rptQuickHosts.DataBind();
                divQuickHosts.Visible = true;
            }
            else
            {
                rptQuickHosts.DataSource = null;
                rptQuickHosts.DataBind();
                divQuickHosts.Visible = false;
            }
        }

        private static bool IsValidNotificationEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            email = email.Trim();
            if (email.Length < 5 || email.Length > 254)
                return false;

            int at = email.IndexOf('@');
            if (at <= 0 || at != email.LastIndexOf('@') || at == email.Length - 1)
                return false;

            string domain = email.Substring(at + 1);
            return domain.IndexOf('.') > 0;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<HostEmployee> SearchHost(string prefixText, int branchId)
        {
            List<HostEmployee> hosts = new List<HostEmployee>();
            string query = @"SELECT TOP 10 EmployeeCode, EmployeeName, ISNULL(d.Department_Description, 'General') AS DepartmentName
                             FROM MST_UserMaster u
                             LEFT JOIN MST_Department d ON u.DepartmentId = CAST(d.Department_ID AS INT)
                             WHERE u.BranchId = @BranchId AND u.EmployeeStatus = 'Active' 
                               AND (u.EmployeeName LIKE @SearchText OR u.EmployeeCode LIKE @SearchText)
                             ORDER BY u.EmployeeName ASC";

            using (SqlConnection con = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@BranchId", branchId);
                    cmd.Parameters.AddWithValue("@SearchText", "%" + prefixText + "%");
                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            hosts.Add(new HostEmployee
                            {
                                EmpCode = sdr["EmployeeCode"].ToString(),
                                DisplayText = $"{sdr["EmployeeName"].ToString()} - {sdr["DepartmentName"].ToString()}"
                            });
                        }
                    }
                }
            }
            return hosts;
        }

        public class HostEmployee
        {
            public string EmpCode { get; set; }
            public string DisplayText { get; set; }
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            string fullPath = "";
            string dbPhotoPath = "";
            int newVisitId = 0;

            try
            {
                lblMessage.Text = "";

                // ==========================================================
                // 1. SESSION VALIDATION
                // ==========================================================
                if (Session["VerifiedMobile"] == null ||
                    string.IsNullOrEmpty(Session["VerifiedMobile"].ToString()))
                {
                    Response.Redirect($"VMS_SelfLogin.aspx?BranchId={hfBranchId.Value}", false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }

                // 2026-08-22: ASP.NET Validation — validates RequiredFieldValidators in ValidationGroup="Registration"
                // NOTE: rfvCompany.Enabled is managed in Page_Load on every postback.
                // It is disabled when Purpose="I Work Here" (Company auto-filled by ResolveApprovalMode)
                // and re-enabled for all other purposes.
                Page.Validate("Registration");
                if (!Page.IsValid)
                {
                    lblMessage.ForeColor = System.Drawing.Color.Red;
                    lblMessage.Text = "Please correct the highlighted fields before submitting.";
                    return;
                }

                // 2026-08-22: Server-side camera activation check (workflow indicator — blocks submission if camera was never active)
                // SECURITY NOTE: hfCameraActive is a client-controlled workflow indicator. It is NOT proof of
                // live capture. An attacker could set it to 'true' via DevTools. Its absence (value != 'true')
                // confirms the camera permission was never granted and getUserMedia never activated, which
                // provides a practical denylist. True liveness detection requires additional infrastructure.
                if (hfCameraActive.Value != "true")
                {
                    lblMessage.ForeColor = System.Drawing.Color.Red;
                    lblMessage.Text = "Camera access is required. Please allow camera permissions and take a photo.";
                    return;
                }

                // ==========================================================
                // 2. BASIC VARIABLES
                // ==========================================================
                string visitorMobile = Session["VerifiedMobile"].ToString();
                if (visitorMobile.Length > 15)
                    visitorMobile = visitorMobile.Substring(0, 15);

                string visitorEmail = VmsHelper.NormalizeEmail(txtEmail.Text.Trim());

                int branchId = Convert.ToInt32(hfBranchId.Value);

                // ==========================================================
                // 3. HOST + SAME-HOST DUPLICATE CHECK
                // ==========================================================
                string hostEmpCode = hfSelectedHostCode.Value == null
                    ? ""
                    : hfSelectedHostCode.Value.Trim();
                if (string.IsNullOrEmpty(hostEmpCode))
                {
                    // 2026-08-22 FIX: Replaced throw new Exception with graceful validation message
                    lblMessage.ForeColor = System.Drawing.Color.Red;
                    lblMessage.Text = "Selecting a Host Employee is mandatory to generate a pass.";
                    return;
                }

                if (VmsHelper.HasActiveVisit(visitorMobile, visitorEmail, branchId, hostEmpCode))
                {
                    Response.Redirect($"VMS_MyVisits.aspx?BranchId={branchId}", false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }

                // ==========================================================
                // 4. SELFIE VALIDATION
                // ==========================================================
                string photoPath = PersistSelfie();
                fullPath = Server.MapPath("~" + photoPath);
                dbPhotoPath = photoPath;

                // ==========================================================
                // 6. DEFAULT VISIT VALUES / APPROVAL RESOLUTION
                // ==========================================================
                string purposeOfVisit = ddlPurpose.SelectedItem != null
                    ? ddlPurpose.SelectedItem.Text
                    : ddlPurpose.SelectedValue;

                ApprovalResolution approval = ResolveApprovalMode(
                    txtFirstName.Text.Trim() + " " + txtLastName.Text.Trim());

                string fullName = approval.FullName;
                string currentApprovalStatus = approval.ApprovalStatus;
                string currentVisitStatus = approval.VisitStatus;
                object actualInTime = approval.ActualInTime;
                object approvedOn = approval.ApprovedOn;
                object qrTokenParam = approval.QrTokenParam;

                // 2026-08-22: Server-side Company Name enforcement (after ResolveApprovalMode auto-fill for internal employees)
                string companyValue = txtCompany.Text.Trim();
                if (string.IsNullOrWhiteSpace(companyValue))
                {
                    lblMessage.ForeColor = System.Drawing.Color.Red;
                    lblMessage.Text = "Company Name is required.";
                    return;
                }

                // 2026-08-22: Server-side Designation enforcement (authoritative — cannot be bypassed)
                string designationValue = txtDesignation.Text.Trim();
                if (string.IsNullOrWhiteSpace(designationValue))
                {
                    lblMessage.ForeColor = System.Drawing.Color.Red;
                    lblMessage.Text = "Designation is required.";
                    return;
                }

                VisitScheduleRequest visitSchedule = VmsVisitScheduleHelper.ParseSchedule(
                    ddlScheduleType.SelectedValue,
                    txtVisitFromDate.Text,
                    txtVisitToDate.Text,
                    txtTimeFrom.Text,
                    txtTimeTo.Text);

                string scheduleSummary = VmsVisitScheduleHelper.FormatScheduleSummary(visitSchedule);

                // ==========================================================
                // 8-11. PERSIST VISITOR VISIT (repository transaction)
                // ==========================================================
                DateTime validFrom = visitSchedule.RequestedValidFrom;
                DateTime validUpto = visitSchedule.RequestedValidUpto;

                VmsVisitorRepository visitorRepository = new VmsVisitorRepository();
                newVisitId = visitorRepository.CreateVisitorVisit(
                    ddlPurpose.SelectedValue,
                    fullName,
                    txtFirstName.Text.Trim(),
                    txtLastName.Text.Trim(),
                    visitorMobile,
                    visitorEmail,
                    txtCompany.Text.Trim(),
                    txtDesignation.Text.Trim(),
                    dbPhotoPath,
                    branchId,
                    hostEmpCode,
                    purposeOfVisit,
                    currentApprovalStatus,
                    validFrom,
                    validUpto,
                    qrTokenParam,
                    currentVisitStatus,
                    actualInTime,
                    approvedOn,
                    visitSchedule);

                QueueNotifications(
                    newVisitId,
                    hostEmpCode,
                    fullName,
                    currentApprovalStatus,
                    scheduleSummary,
                    visitorEmail,
                    visitorMobile,
                    qrTokenParam,
                    approval.ShouldQueueVisitorPass);

                // ==========================================================
                // 16. SUCCESS
                // ==========================================================
                Session["VerifiedEmail"] = visitorEmail != "N/A" ? visitorEmail : string.Empty;
                Response.Redirect("VMS_MyVisits.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
            }
            catch (Exception ex)
            {
                // ==========================================================
                // DELETE ORPHANED SELFIE
                // ==========================================================
                if (!string.IsNullOrWhiteSpace(fullPath))
                {
                    try
                    {
                        if (File.Exists(fullPath))
                            File.Delete(fullPath);
                    }
                    catch
                    {
                        // Ignore cleanup failure
                    }
                }

                lblMessage.ForeColor = System.Drawing.Color.Red;
                lblMessage.Text = "Submission Error: " + ex.Message;
            }
        }

        protected void btnSubmit_Click_OLD(object sender, EventArgs e)
        {
            try
            {
                lblMessage.Text = ""; // Reset errors

                // --- 1. Gather Identifying Variables Early ---
                string visitorMobile = Session["VerifiedMobile"] != null ? Session["VerifiedMobile"].ToString() : "";
                if (visitorMobile.Length > 15) visitorMobile = visitorMobile.Substring(0, 15);

                string visitorEmail = txtEmail.Text.Trim();
                if (string.IsNullOrEmpty(visitorEmail) && Session["VerifiedEmail"] != null)
                {
                    visitorEmail = Session["VerifiedEmail"].ToString();
                }
                if (string.IsNullOrEmpty(visitorEmail))
                {
                    visitorEmail = "N/A";
                }

                int branchId = Convert.ToInt32(hfBranchId.Value);

                // --- 2. 🌟 ANTI-SPAM FAILSAFE: Prevent Duplicate Rapid-Clicks ---
                using (SqlConnection con = new SqlConnection(connStr))
                {
                    string checkQuery = @"
            SELECT COUNT(1) FROM VMS_VisitLog 
            WHERE (VisitorMobile = @Mobile OR VisitorEmail = @Email) 
              AND TargetBranchID = @BranchId 
              AND ApprovalStatus IN ('Pending', 'Approved')
              AND ISNULL(VisitStatus, '') != 'Checked-Out'
              AND ValidUpto >= GETDATE()";

                    using (SqlCommand cmdCheck = new SqlCommand(checkQuery, con))
                    {
                        cmdCheck.Parameters.AddWithValue("@Mobile", string.IsNullOrEmpty(visitorMobile) ? "N/A" : visitorMobile);
                        cmdCheck.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(visitorEmail) ? "N/A" : visitorEmail);
                        cmdCheck.Parameters.AddWithValue("@BranchId", branchId);

                        con.Open();
                        int activePassCount = Convert.ToInt32(cmdCheck.ExecuteScalar());
                        con.Close();

                        if (activePassCount > 0)
                        {
                            // A pass was literally just generated a millisecond ago. 
                            // Redirect them peacefully without inserting a duplicate.
                            Session["VerifiedContact"] = !string.IsNullOrEmpty(visitorMobile) ? visitorMobile : visitorEmail;
                            Response.Redirect("VMS_MyVisits.aspx", false);
                            Context.ApplicationInstance.CompleteRequest();
                            return;
                        }
                    }
                }

                // --- 3. Process Selfie Image ---
                string base64Data = hfSelfieBase64.Value;
                if (string.IsNullOrEmpty(base64Data)) throw new Exception("No photo captured.");
                base64Data = base64Data.Replace("data:image/png;base64,", "");
                byte[] imageBytes = Convert.FromBase64String(base64Data);

                string fileName = $"Vis_{DateTime.Now.Ticks}.png";
                string folderPath = Server.MapPath("~/Uploads/VisitorPhotos/");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                string fullPath = Path.Combine(folderPath, fileName);
                File.WriteAllBytes(fullPath, imageBytes);
                string dbPhotoPath = "/Uploads/VisitorPhotos/" + fileName;

                // --- 4. Validate Host ---
                string hostEmpCode = hfSelectedHostCode.Value;
                if (string.IsNullOrEmpty(hostEmpCode))
                {
                    throw new Exception("Selecting a Host Employee is mandatory to generate a pass.");
                }

                string fullName = txtFirstName.Text.Trim() + " " + txtLastName.Text.Trim();
                int newVisitId = 0;
                string hostEmail = "", hostName = "";

                // 🌟 DEFAULT STATUSES (For Standard Visitors)
                string currentApprovalStatus = "Pending";
                string currentVisitStatus = "Scheduled";
                object actualInTime = DBNull.Value;

                // --- 5. Internal Employee Auto-Approval & Auto Check-In Logic ---
                if (ddlPurpose.SelectedValue == "I Work Here")
                {
                    string internalCode = txtInternalEmpCode.Text.Trim();
                    if (string.IsNullOrEmpty(internalCode))
                    {
                        throw new Exception("You must provide your internal Employee Code to self-approve your pass.");
                    }

                    using (SqlConnection conCheck = new SqlConnection(connStr))
                    {
                        conCheck.Open();
                        string empCheckQuery = "SELECT EmployeeName, Email, Mobile FROM MST_UserMaster WHERE EmployeeCode = @EmpCode AND EmployeeStatus = 'Active'";

                        using (SqlCommand cmdCheck = new SqlCommand(empCheckQuery, conCheck))
                        {
                            cmdCheck.Parameters.AddWithValue("@EmpCode", internalCode);
                            using (SqlDataReader sdrCheck = cmdCheck.ExecuteReader())
                            {
                                if (sdrCheck.Read())
                                {
                                    // 1. Grant Auto-Approval
                                    currentApprovalStatus = "Approved";

                                    // 2. 🌟 GRANT AUTO CHECK-IN
                                    currentVisitStatus = "Checked-In";
                                    actualInTime = DateTime.Now;

                                    // 3. Fetch the official name from the Master Table
                                    fullName = sdrCheck["EmployeeName"].ToString();

                                    // 4. Silently override the manual text inputs to ensure DB consistency
                                    string[] nameParts = fullName.Split(new[] { ' ' }, 2);
                                    txtFirstName.Text = nameParts.Length > 0 ? nameParts[0] : fullName;
                                    txtLastName.Text = nameParts.Length > 1 ? nameParts[1] : "";

                                    // 5. Force the company name
                                    txtCompany.Text = "Anmol Industries Ltd.";
                                }
                                else
                                {
                                    throw new Exception("The Employee Code entered is invalid or inactive. Registration denied.");
                                }
                            }
                        }
                    }
                }

                // --- 6. DB Insertion & Timings Logic ---
                using (SqlConnection con = new SqlConnection(connStr))
                {
                    con.Open();
                    DateTime validFrom = DateTime.Now;
                    DateTime validUpto = validFrom.AddHours(8);
                    string currentDay = validFrom.ToString("dddd");

                    string timingQuery = "SELECT CloseTime FROM VMS_PlantTimings WHERE Branch_ID = @BranchID AND DayOfWeek = @DayOfWeek";
                    using (SqlCommand timeCmd = new SqlCommand(timingQuery, con))
                    {
                        timeCmd.Parameters.AddWithValue("@BranchID", branchId);
                        timeCmd.Parameters.AddWithValue("@DayOfWeek", currentDay);
                        object closeTimeObj = timeCmd.ExecuteScalar();

                        if (closeTimeObj != null && closeTimeObj != DBNull.Value)
                        {
                            TimeSpan closingTime = (TimeSpan)closeTimeObj;
                            validUpto = new DateTime(validFrom.Year, validFrom.Month, validFrom.Day, closingTime.Hours, closingTime.Minutes, closingTime.Seconds);

                            if (validFrom >= validUpto)
                            {
                                throw new Exception($"Registration denied. The visiting hours for this facility ended at {validUpto.ToString("hh:mm tt")}.");
                            }
                        }
                    }

                    Guid newQrToken = Guid.NewGuid();

                    // 🌟 UPDATED INSERT QUERY: Added VisitStatus and ActualInTime
                    string insertQuery = @"
            INSERT INTO VMS_VisitLog 
            (PassCategory, VisitorName, VisitorFirstName, VisitorLastName, VisitorMobile, VisitorEmail, VisitorCompany, VisitorDesignation, PhotoPath, TargetBranchID, HostEmployeeCode, PurposeOfVisit, ApprovalStatus, ValidFrom, ValidUpto, QRToken, VisitStatus, ActualInTime)
            VALUES 
            (@Category, @FullName, @FirstName, @LastName, @Mobile, @Email, @Company, @Designation, @PhotoPath, @BranchID, @HostCode, 'General Visit', @ApprovalStatus, @ValidFrom, @ValidUpto, @QRToken, @VisitStatus, @ActualInTime);
            SELECT SCOPE_IDENTITY();";

                    using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@Category", ddlPurpose.SelectedValue);
                        cmd.Parameters.AddWithValue("@FullName", fullName);
                        cmd.Parameters.AddWithValue("@FirstName", txtFirstName.Text.Trim());
                        cmd.Parameters.AddWithValue("@LastName", txtLastName.Text.Trim());
                        cmd.Parameters.AddWithValue("@Mobile", visitorMobile);
                        cmd.Parameters.AddWithValue("@Email", visitorEmail);
                        cmd.Parameters.AddWithValue("@Company", txtCompany.Text.Trim());
                        cmd.Parameters.AddWithValue("@Designation", txtDesignation.Text.Trim());
                        cmd.Parameters.AddWithValue("@PhotoPath", dbPhotoPath);
                        cmd.Parameters.AddWithValue("@BranchID", branchId);
                        cmd.Parameters.AddWithValue("@HostCode", hostEmpCode);
                        cmd.Parameters.AddWithValue("@ApprovalStatus", currentApprovalStatus);
                        cmd.Parameters.AddWithValue("@ValidFrom", validFrom);
                        cmd.Parameters.AddWithValue("@ValidUpto", validUpto);
                        cmd.Parameters.AddWithValue("@QRToken", newQrToken);

                        // 🌟 NEW PARAMETERS ADDED HERE
                        cmd.Parameters.AddWithValue("@VisitStatus", currentVisitStatus);
                        cmd.Parameters.AddWithValue("@ActualInTime", actualInTime);

                        newVisitId = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // --- 7. Queue Notifications ---
                    string hostQuery = "SELECT EmployeeName, Email FROM MST_UserMaster WHERE EmployeeCode = @HostCode";
                    using (SqlCommand hostCmd = new SqlCommand(hostQuery, con))
                    {
                        hostCmd.Parameters.AddWithValue("@HostCode", hostEmpCode);
                        using (SqlDataReader sdr = hostCmd.ExecuteReader())
                        {
                            if (sdr.Read())
                            {
                                hostName = sdr["EmployeeName"].ToString();
                                hostEmail = sdr["Email"].ToString();
                            }
                        }
                    }

                    // Smart Fallback Email Routing
                    bool isFallbackEmail = false;
                    if (string.IsNullOrEmpty(hostEmail))
                    {
                        hostEmail = "dgtlautomation@anmolindustries.com";
                        isFallbackEmail = true;
                    }

                    if (!string.IsNullOrEmpty(hostEmail))
                    {
                        if (currentApprovalStatus == "Pending")
                        {
                            string subject = $"Approval Needed: Visitor {fullName} has arrived";
                            if (isFallbackEmail) subject = "[FALLBACK] " + subject;

                            string quickApproveLink = VmsHelper.BuildHostQuickLoginUrl(newVisitId, hostEmpCode);

                            string fallbackNote = isFallbackEmail
                                ? $"<p style='color: red; font-size: 12px;'><em>System Note: This email was routed to the fallback address because no valid email was found in the database for Host Code: {hostEmpCode}.</em></p>"
                                : "";

                            string body = $@"
                    <div style='font-family: Arial; padding: 20px;'>
                        {fallbackNote}
                        <h3 style='color: #1ABB9C;'>Hello {hostName},</h3>
                        <p><strong>{fullName}</strong> from <strong>{txtCompany.Text}</strong> is waiting for your approval at the reception.</p>
                        <br/>
                        <a href='{quickApproveLink}' style='background: #1ABB9C; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Review & Approve Visitor</a>
                    </div>";

                            string queueQuery = @"
                    INSERT INTO NotificationQueue 
                    (FormSubmissionId, EscalationLevel, RecipientEmail, RecipientName, Subject, Body, NotificationType, Status, SourceModule) 
                    VALUES 
                    (@VisitId, 0, @Email, @Name, @Subject, @Body, 'Email', 'Pending', 'VMS_APPROVAL')";

                            using (SqlCommand qCmd = new SqlCommand(queueQuery, con))
                            {
                                qCmd.Parameters.AddWithValue("@VisitId", newVisitId);
                                qCmd.Parameters.AddWithValue("@Email", hostEmail);
                                qCmd.Parameters.AddWithValue("@Name", hostName);
                                qCmd.Parameters.AddWithValue("@Subject", subject);
                                qCmd.Parameters.AddWithValue("@Body", body);
                                qCmd.ExecuteNonQuery();
                            }
                        }
                        else if (currentApprovalStatus == "Approved")
                        {
                            string fyiSubject = $"FYI: Internal Colleague {fullName} has arrived";
                            if (isFallbackEmail) fyiSubject = "[FALLBACK] " + fyiSubject;

                            string fallbackNote = isFallbackEmail
                                ? $"<p style='color: red; font-size: 12px;'><em>System Note: This email was routed to the fallback address because no valid email was found in the database for Host Code: {hostEmpCode}.</em></p>"
                                : "";

                            string fyiBody = $@"
                    <div style='font-family: Arial; padding: 20px;'>
                        {fallbackNote}
                        <h3 style='color: #1ABB9C;'>Hello {hostName},</h3>
                        <p><strong>{fullName}</strong> from <strong>{txtCompany.Text}</strong> (Internal Employee) is currently at the reception to meet you.</p>
                        <p style='color: #73879C;'><em>Note: Their visitor pass has been automatically approved and they have been checked in. No further action is required from your end.</em></p>
                    </div>";

                            string queueQuery = @"
                    INSERT INTO NotificationQueue 
                    (FormSubmissionId, EscalationLevel, RecipientEmail, RecipientName, Subject, Body, NotificationType, Status, SourceModule) 
                    VALUES 
                    (@VisitId, 0, @Email, @Name, @Subject, @Body, 'Email', 'Pending', 'VMS_FYI_ARRIVAL')";

                            using (SqlCommand qCmd = new SqlCommand(queueQuery, con))
                            {
                                qCmd.Parameters.AddWithValue("@VisitId", newVisitId);
                                qCmd.Parameters.AddWithValue("@Email", hostEmail);
                                qCmd.Parameters.AddWithValue("@Name", hostName);
                                qCmd.Parameters.AddWithValue("@Subject", fyiSubject);
                                qCmd.Parameters.AddWithValue("@Body", fyiBody);
                                qCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }

                // Smooth redirect to the dashboard
                Response.Redirect("VMS_MyVisits.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
            }
            catch (Exception ex)
            {
                lblMessage.ForeColor = System.Drawing.Color.Red;
                lblMessage.Text = "Submission Error: " + ex.Message;
            }
        }

        private bool IsFacilityOpen(int branchId)
        {
            using (SqlConnection con = new SqlConnection(connStr))
            {
                DateTime now = DateTime.Now;
                string currentDay = now.ToString("dddd");

                string timingQuery = "SELECT CloseTime FROM VMS_PlantTimings WHERE Branch_ID = @BranchID AND DayOfWeek = @DayOfWeek";

                using (SqlCommand cmd = new SqlCommand(timingQuery, con))
                {
                    cmd.Parameters.AddWithValue("@BranchID", branchId);
                    cmd.Parameters.AddWithValue("@DayOfWeek", currentDay);

                    con.Open();
                    object closeTimeObj = cmd.ExecuteScalar();

                    if (closeTimeObj != null && closeTimeObj != DBNull.Value)
                    {
                        TimeSpan closingTime = (TimeSpan)closeTimeObj;
                        DateTime closingDateTime = new DateTime(now.Year, now.Month, now.Day, closingTime.Hours, closingTime.Minutes, closingTime.Seconds);

                        // If the current time is past the official closing time, return false
                        if (now >= closingDateTime)
                        {
                            return false;
                        }
                    }
                }
            }
            // Defaults to true so that if a branch forgets to configure their timings in the DB, 
            // it doesn't accidentally lock everyone out.
            return true;
        }


        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<HostEmployee> GetPreconfiguredHosts(int branchId)
        {
            List<HostEmployee> hosts = new List<HostEmployee>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                string query = @"
            SELECT u.EmployeeCode, u.EmployeeName, ISNULL(d.Department_Description, 'General') AS DepartmentName
            FROM VMS_OfficeSettings s
            CROSS APPLY STRING_SPLIT(s.PreconfiguredHostCodes, ',') split
            INNER JOIN MST_UserMaster u ON RTRIM(LTRIM(split.value)) = u.EmployeeCode
            LEFT JOIN MST_Department d ON u.DepartmentId = CAST(d.Department_ID AS INT)
            WHERE s.Branch_ID = @BranchId AND u.EmployeeStatus = 'Active'";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@BranchId", branchId);
                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            hosts.Add(new HostEmployee
                            {
                                EmpCode = sdr["EmployeeCode"].ToString(),
                                DisplayText = $"{sdr["EmployeeName"]} - {sdr["DepartmentName"]}"
                            });
                        }
                    }
                }
            }
            return hosts;
        }
    }
}