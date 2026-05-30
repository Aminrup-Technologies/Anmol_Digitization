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
                if ((Session["VerifiedMobile"] == null && Session["VerifiedEmail"] == null) || Request.QueryString["BranchId"] == null)
                {
                    Response.Redirect("VMS_SelfLogin.aspx", false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }

                hfBranchId.Value = Request.QueryString["BranchId"].ToString();

                // 2. Prevent Duplicate Registration
                CheckForActivePassAndRedirect();

                // 3. 🌟 NEW: Early-Rejection Check for Office Hours
                int currentBranchId = Convert.ToInt32(hfBranchId.Value);
                if (!IsFacilityOpen(currentBranchId))
                {
                    lblMessage.ForeColor = System.Drawing.Color.Red;
                    lblMessage.Text = "Welcome to Anmol Industries. Our visiting hours for today have concluded. Please coordinate with your host to schedule a visit for our next working day.";

                    // Disable the form submission
                    btnSubmit.Visible = false;

                    // Optional: If you wrapped your input fields in an <asp:Panel ID="pnlRegistrationForm" runat="server">
                    // you can hide the whole form cleanly by uncommenting the next line:
                    pnlRegistrationForm.Visible = false; 

                    return; // Stop loading the rest of the page
                }

                // 4. Safely Pre-fill the contact fields (Existing Logic)
                if (Session["VerifiedMobile"] != null && !string.IsNullOrEmpty(Session["VerifiedMobile"].ToString()))
                {
                    txtMobile.Text = Session["VerifiedMobile"].ToString();
                    txtMobile.ReadOnly = true;
                }

                if (Session["VerifiedEmail"] != null && !string.IsNullOrEmpty(Session["VerifiedEmail"].ToString()))
                {
                    txtEmail.Text = Session["VerifiedEmail"].ToString();
                    txtEmail.ReadOnly = true;
                }
                else
                {
                    txtEmail.Text = "";
                    txtEmail.ReadOnly = false;
                }
            }
        }

        private void CheckForActivePassAndRedirect()
        {
            string visitorMobile = Session["VerifiedMobile"] != null ? Session["VerifiedMobile"].ToString() : "";
            string visitorEmail = Session["VerifiedEmail"] != null ? Session["VerifiedEmail"].ToString() : "";
            int branchId = Convert.ToInt32(hfBranchId.Value);

            using (SqlConnection con = new SqlConnection(connStr))
            {
                // Query checks for passes that are Pending/Approved AND haven't expired yet
                string checkQuery = @"
                SELECT COUNT(1) FROM VMS_VisitLog 
                WHERE (VisitorMobile = @Mobile OR VisitorEmail = @Email) 
                  AND TargetBranchID = @BranchId 
                  AND ApprovalStatus IN ('Pending', 'Approved')
                  AND ISNULL(VisitStatus, '') != 'Checked-Out'
                  AND ValidUpto >= GETDATE()";

                using (SqlCommand cmd = new SqlCommand(checkQuery, con))
                {
                    // Use "N/A" to prevent empty strings from accidentally matching bad database rows
                    cmd.Parameters.AddWithValue("@Mobile", string.IsNullOrEmpty(visitorMobile) ? "N/A" : visitorMobile);
                    cmd.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(visitorEmail) ? "N/A" : visitorEmail);
                    cmd.Parameters.AddWithValue("@BranchId", branchId);

                    con.Open();
                    int activePassCount = Convert.ToInt32(cmd.ExecuteScalar());

                    if (activePassCount > 0)
                    {
                        // An active pass already exists! 
                        // Set the contact session required by MyVisits page
                        Session["VerifiedContact"] = !string.IsNullOrEmpty(visitorMobile) ? visitorMobile : visitorEmail;

                        // Instantly redirect them to their pass so they don't fill the form again
                        Response.Redirect("VMS_MyVisits.aspx", false);
                        Context.ApplicationInstance.CompleteRequest();
                    }
                }
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

                string currentApprovalStatus = "Pending";

                // --- 5. Internal Employee Self-Approval Logic ---
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
                                    currentApprovalStatus = "Approved";
                                    fullName = sdrCheck["EmployeeName"].ToString();

                                    string[] nameParts = fullName.Split(new[] { ' ' }, 2);
                                    txtFirstName.Text = nameParts.Length > 0 ? nameParts[0] : fullName;
                                    txtLastName.Text = nameParts.Length > 1 ? nameParts[1] : "";
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

                    string insertQuery = @"
            INSERT INTO VMS_VisitLog 
            (PassCategory, VisitorName, VisitorFirstName, VisitorLastName, VisitorMobile, VisitorEmail, VisitorCompany, VisitorDesignation, PhotoPath, TargetBranchID, HostEmployeeCode, PurposeOfVisit, ApprovalStatus, ValidFrom, ValidUpto, QRToken)
            VALUES 
            (@Category, @FullName, @FirstName, @LastName, @Mobile, @Email, @Company, @Designation, @PhotoPath, @BranchID, @HostCode, 'General Visit', @ApprovalStatus, @ValidFrom, @ValidUpto, @QRToken);
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

                    // 🌟 NEW: Smart Fallback Email Routing
                    bool isFallbackEmail = false;
                    if (string.IsNullOrEmpty(hostEmail))
                    {
                        hostEmail = "dgtlautomation@anmolindustries.com";
                        isFallbackEmail = true;
                    }

                    if (!string.IsNullOrEmpty(hostEmail)) // This will now always execute
                    {
                        if (currentApprovalStatus == "Pending")
                        {
                            string subject = $"Approval Needed: Visitor {fullName} has arrived";
                            if (isFallbackEmail) subject = "[FALLBACK] " + subject; // Tag it for the IT team

                            byte[] plainTextBytes = System.Text.Encoding.UTF8.GetBytes(hostEmpCode);
                            string encodedEmpCode = Convert.ToBase64String(plainTextBytes);

                            string quickApproveLink = $"https://apps.anmolindustries.com/VMS_HostQuickLogin.aspx?vid={newVisitId}&t={encodedEmpCode}";

                            // Add a red warning note if routed to the fallback address
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
                                <p style='color: #73879C;'><em>Note: Their visitor pass has been automatically approved. No further action is required from your end.</em></p>
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

                // --- 8. Complete Request & Redirect ---
                Session["VerifiedContact"] = !string.IsNullOrEmpty(visitorMobile) ? visitorMobile : visitorEmail;
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
    }
}