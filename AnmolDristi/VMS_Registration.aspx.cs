using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Configuration;
using System.IO;
using System.Web.Services;
using System.Web.Script.Services;

namespace AnmolDristi
{
    public partial class VMS_Registration : System.Web.UI.Page
    {
        static string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // 1. Existing Security Check (Ensure they passed OTP)
                if (Session["VerifiedMobile"] == null ||
                    string.IsNullOrEmpty(Session["VerifiedMobile"].ToString()) ||
                    Request.QueryString["BranchId"] == null)
                {
                    Response.Redirect("VMS_SelfLogin.aspx", false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }

                hfBranchId.Value = Request.QueryString["BranchId"].ToString();

                string closedMsg;
                bool isOpen = VmsHelper.IsFacilityOpen(Convert.ToInt32(hfBranchId.Value), out closedMsg);
                hfIsFacilityOpen.Value = isOpen.ToString().ToLower();

                // Bind quick-select host chips from branch preconfigured hosts
                BindQuickHosts();

                // 2. Prevent Duplicate Registration
                CheckForActivePassAndRedirect();

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

        private void CheckForActivePassAndRedirect()
        {
            string visitorMobile = Session["VerifiedMobile"] != null ? Session["VerifiedMobile"].ToString() : "";
            string visitorEmail = Session["VerifiedEmail"] != null ? Session["VerifiedEmail"].ToString() : "";
            int branchId = Convert.ToInt32(hfBranchId.Value);

            if (VmsHelper.HasActiveVisit(visitorMobile, visitorEmail, branchId))
            {
                Response.Redirect("VMS_MyVisits.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
            }
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

                // ==========================================================
                // 2. BASIC VARIABLES
                // ==========================================================
                string visitorMobile = Session["VerifiedMobile"].ToString();
                if (visitorMobile.Length > 15)
                    visitorMobile = visitorMobile.Substring(0, 15);

                string visitorEmail = VmsHelper.NormalizeEmail(txtEmail.Text.Trim());

                int branchId = Convert.ToInt32(hfBranchId.Value);

                // ==========================================================
                // 3. DUPLICATE ACTIVE PASS CHECK
                // ==========================================================
                if (VmsHelper.HasActiveVisit(visitorMobile, visitorEmail, branchId))
                {
                    Response.Redirect("VMS_MyVisits.aspx", false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }

                // ==========================================================
                // 4. SELFIE VALIDATION
                // ==========================================================
                string base64Data = hfSelfieBase64.Value;

                if (string.IsNullOrWhiteSpace(base64Data))
                    throw new Exception("No photo captured.");

                base64Data = base64Data.Replace("data:image/png;base64,", "");

                byte[] imageBytes = Convert.FromBase64String(base64Data);

                string folderPath =
                    Server.MapPath("~/Uploads/VisitorPhotos/");

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string fileName =
                    $"Vis_{DateTime.Now.Ticks}.png";

                fullPath = Path.Combine(folderPath, fileName);

                File.WriteAllBytes(fullPath, imageBytes);

                dbPhotoPath = "/Uploads/VisitorPhotos/" + fileName;

                // ==========================================================
                // 5. HOST VALIDATION
                // ==========================================================
                string hostEmpCode = hfSelectedHostCode.Value;

                if (string.IsNullOrWhiteSpace(hostEmpCode))
                {
                    throw new Exception(
                        "Selecting a Host Employee is mandatory to generate a pass.");
                }

                // ==========================================================
                // 6. DEFAULT VISIT VALUES
                // ==========================================================
                string fullName =
                    txtFirstName.Text.Trim() + " " + txtLastName.Text.Trim();

                string hostEmail = "";
                string hostName = "";

                string purposeOfVisit = ddlPurpose.SelectedItem != null
                    ? ddlPurpose.SelectedItem.Text
                    : ddlPurpose.SelectedValue;

                string currentApprovalStatus = "Pending";
                string currentVisitStatus = "Scheduled";
                object actualInTime = DBNull.Value;
                object approvedOn = DBNull.Value;

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

                                currentApprovalStatus = "Approved";
                                currentVisitStatus = "Scheduled";
                                approvedOn = DateTime.Now;

                                fullName =
                                    dr["EmployeeName"].ToString();

                                string[] parts =
                                    fullName.Split(new[] { ' ' }, 2);

                                txtFirstName.Text =
                                    parts.Length > 0
                                    ? parts[0]
                                    : fullName;

                                txtLastName.Text =
                                    parts.Length > 1
                                    ? parts[1]
                                    : "";

                                txtCompany.Text =
                                    "Anmol Industries Ltd.";
                            }
                        }
                    }
                }

                VisitScheduleRequest visitSchedule = VmsVisitScheduleHelper.ParseSchedule(
                    ddlScheduleType.SelectedValue,
                    txtVisitFromDate.Text,
                    txtVisitToDate.Text,
                    txtTimeFrom.Text,
                    txtTimeTo.Text);

                string scheduleSummary = VmsVisitScheduleHelper.FormatScheduleSummary(visitSchedule);

                // ==========================================================
                // 8. BEGIN DATABASE TRANSACTION
                // ==========================================================
                using (SqlConnection con =
                    new SqlConnection(connStr))
                {
                    con.Open();

                    using (SqlTransaction trans =
                        con.BeginTransaction())
                    {
                        try
                        {
                            // --------------------------------------------------
                            // CONTINUE IN PART-2
                            // Timing Validation
                            // Visit Insert
                            // Host Lookup
                            // Notification Queue
                            // --------------------------------------------------
                            // ==========================================================
                            // 9. VISITING HOURS VALIDATION
                            // ==========================================================
                            DateTime validFrom = visitSchedule.RequestedValidFrom;
                            DateTime validUpto = visitSchedule.RequestedValidUpto;

                            object qrTokenParam = currentApprovalStatus == "Approved"
                                ? (object)Guid.NewGuid()
                                : DBNull.Value;

                            // ==========================================================
                            // 11. INSERT VISITOR RECORD
                            // ==========================================================
                            string insertQuery = @"

                                INSERT INTO VMS_VisitLog
                                (
                                PassCategory,
                                VisitorName,
                                VisitorFirstName,
                                VisitorLastName,
                                VisitorMobile,
                                VisitorEmail,
                                VisitorCompany,
                                VisitorDesignation,
                                PhotoPath,
                                TargetBranchID,
                                HostEmployeeCode,
                                PurposeOfVisit,
                                ApprovalStatus,
                                ValidFrom,
                                ValidUpto,
                                QRToken,
                                VisitStatus,
                                ActualInTime,
                                ApprovedOn,
                                ScheduleType,
                                RequestedValidFrom,
                                RequestedValidUpto,
                                DailyTimeFrom,
                                DailyTimeTo,
                                ApprovedValidFrom,
                                ApprovedValidUpto
                                )
                                VALUES
                                (
                                @Category,
                                @FullName,
                                @FirstName,
                                @LastName,
                                @Mobile,
                                @Email,
                                @Company,
                                @Designation,
                                @PhotoPath,
                                @BranchID,
                                @HostCode,
                                @Purpose,
                                @ApprovalStatus,
                                @ValidFrom,
                                @ValidUpto,
                                @QRToken,
                                @VisitStatus,
                                @ActualInTime,
                                @ApprovedOn,
                                @ScheduleType,
                                @RequestedValidFrom,
                                @RequestedValidUpto,
                                @DailyTimeFrom,
                                @DailyTimeTo,
                                @ApprovedValidFrom,
                                @ApprovedValidUpto
                                );

                                SELECT SCOPE_IDENTITY();
                                ";

                            using (SqlCommand cmd =
                                new SqlCommand(insertQuery, con, trans))
                            {
                                cmd.Parameters.AddWithValue("@Category",
                                    ddlPurpose.SelectedValue);

                                cmd.Parameters.AddWithValue("@FullName",
                                    fullName);

                                cmd.Parameters.AddWithValue("@FirstName",
                                    txtFirstName.Text.Trim());

                                cmd.Parameters.AddWithValue("@LastName",
                                    txtLastName.Text.Trim());

                                cmd.Parameters.AddWithValue("@Mobile",
                                    visitorMobile);

                                cmd.Parameters.AddWithValue("@Email",
                                    visitorEmail);

                                cmd.Parameters.AddWithValue("@Company",
                                    txtCompany.Text.Trim());

                                cmd.Parameters.AddWithValue("@Designation",
                                    txtDesignation.Text.Trim());

                                cmd.Parameters.AddWithValue("@PhotoPath",
                                    dbPhotoPath);

                                cmd.Parameters.AddWithValue("@BranchID",
                                    branchId);

                                cmd.Parameters.AddWithValue("@HostCode",
                                    hostEmpCode);

                                cmd.Parameters.AddWithValue("@Purpose",
                                    purposeOfVisit);

                                cmd.Parameters.AddWithValue("@ApprovalStatus",
                                    currentApprovalStatus);

                                cmd.Parameters.AddWithValue("@ValidFrom",
                                    validFrom);

                                cmd.Parameters.AddWithValue("@ValidUpto",
                                    validUpto);

                                cmd.Parameters.AddWithValue("@QRToken",
                                    qrTokenParam);

                                cmd.Parameters.AddWithValue("@VisitStatus",
                                    currentVisitStatus);

                                cmd.Parameters.AddWithValue("@ActualInTime",
                                    actualInTime);

                                cmd.Parameters.AddWithValue("@ApprovedOn",
                                    approvedOn);

                                cmd.Parameters.AddWithValue("@ScheduleType",
                                    visitSchedule.ScheduleType);

                                cmd.Parameters.AddWithValue("@RequestedValidFrom",
                                    visitSchedule.RequestedValidFrom);

                                cmd.Parameters.AddWithValue("@RequestedValidUpto",
                                    visitSchedule.RequestedValidUpto);

                                cmd.Parameters.AddWithValue("@DailyTimeFrom",
                                    visitSchedule.TimeFrom);

                                cmd.Parameters.AddWithValue("@DailyTimeTo",
                                    visitSchedule.TimeTo);

                                cmd.Parameters.AddWithValue("@ApprovedValidFrom",
                                    currentApprovalStatus == "Approved" ? (object)validFrom : DBNull.Value);

                                cmd.Parameters.AddWithValue("@ApprovedValidUpto",
                                    currentApprovalStatus == "Approved" ? (object)validUpto : DBNull.Value);

                                newVisitId =
                                    Convert.ToInt32(cmd.ExecuteScalar());
                            }

                            VmsVisitScheduleHelper.CreateRequestedDayLogs(
                                con,
                                trans,
                                newVisitId,
                                visitSchedule,
                                currentApprovalStatus == "Approved");

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
                                new SqlCommand(hostQuery, con, trans))
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
                            // CONTINUE IN PART-3
                            // Notification Queue
                            // Commit
                            // Rollback
                            // File Cleanup
                            // Redirect
                            // ==========================================================
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

                                    byte[] plainTextBytes =
                                        System.Text.Encoding.UTF8.GetBytes(hostEmpCode);

                                    string encodedEmpCode =
                                        Convert.ToBase64String(plainTextBytes);

                                    string quickApproveLink =
                                        VmsHelper.BuildHostQuickLoginUrl(newVisitId, encodedEmpCode);

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
                                    new SqlCommand(queueQuery, con, trans))
                                {
                                    qCmd.Parameters.AddWithValue("@VisitId", newVisitId);
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
                                    WHERE Branch_ID = @BranchId", con, trans))
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

                                        using (SqlCommand ccCmd = new SqlCommand(ccQueueQuery, con, trans))
                                        {
                                            ccCmd.Parameters.AddWithValue("@VisitId", newVisitId);
                                            ccCmd.Parameters.AddWithValue("@Email", ccEmail);
                                            ccCmd.Parameters.AddWithValue("@Name", ccRecipientName);
                                            ccCmd.Parameters.AddWithValue("@Subject", ccSubject);
                                            ccCmd.Parameters.AddWithValue("@Body", body);
                                            ccCmd.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }

                            if (currentApprovalStatus == "Approved" && qrTokenParam != DBNull.Value)
                            {
                                VmsHelper.QueueVisitorPassNotification(
                                    con,
                                    newVisitId,
                                    (Guid)qrTokenParam,
                                    fullName,
                                    visitorEmail,
                                    visitorMobile,
                                    trans);
                            }

                            // ==========================================================
                            // 15. COMMIT TRANSACTION
                            // ==========================================================
                            trans.Commit();
                        }
                        catch
                        {
                            try
                            {
                                trans.Rollback();
                            }
                            catch
                            {
                                // Ignore rollback exceptions
                            }

                            throw;
                        }
                    }
                }

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

                            byte[] plainTextBytes = System.Text.Encoding.UTF8.GetBytes(hostEmpCode);
                            string encodedEmpCode = Convert.ToBase64String(plainTextBytes);

                            string quickApproveLink = VmsHelper.BuildHostQuickLoginUrl(newVisitId, encodedEmpCode);

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