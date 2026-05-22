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

                // 3. NEW: Safely Pre-fill the contact fields
                if (Session["VerifiedMobile"] != null && !string.IsNullOrEmpty(Session["VerifiedMobile"].ToString()))
                {
                    txtMobile.Text = Session["VerifiedMobile"].ToString();
                    txtMobile.ReadOnly = true; // Lock it since it was verified via OTP
                }

                if (Session["VerifiedEmail"] != null && !string.IsNullOrEmpty(Session["VerifiedEmail"].ToString()))
                {
                    txtEmail.Text = Session["VerifiedEmail"].ToString();
                    txtEmail.ReadOnly = true; // Lock it since it was verified via OTP
                }
                else
                {
                    // If they skipped email at login, leave the box blank and editable
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

                //string visitorMobile = Session["VerifiedMobile"] != null ? Session["VerifiedMobile"].ToString() : "";
                //string visitorEmail = Session["VerifiedEmail"] != null ? Session["VerifiedEmail"].ToString() : "";
                //if (visitorMobile.Length > 15) visitorMobile = visitorMobile.Substring(0, 15);

                string visitorMobile = Session["VerifiedMobile"] != null ? Session["VerifiedMobile"].ToString() : "";
                if (visitorMobile.Length > 15) visitorMobile = visitorMobile.Substring(0, 15);

                // 🌟 NEW: Smart Email Handling
                string visitorEmail = txtEmail.Text.Trim(); // Check if they typed it into the optional frontend box

                // If the box is empty, check if we have a verified email in the session
                if (string.IsNullOrEmpty(visitorEmail) && Session["VerifiedEmail"] != null)
                {
                    visitorEmail = Session["VerifiedEmail"].ToString();
                }

                // If it's STILL empty (Mobile-Only user who skipped the optional box), default to "N/A"
                if (string.IsNullOrEmpty(visitorEmail))
                {
                    visitorEmail = "N/A";
                }

                string hostEmpCode = hfSelectedHostCode.Value;
                // STRICT BACKEND VALIDATION: Host is mandatory for EVERYONE
                if (string.IsNullOrEmpty(hostEmpCode))
                {
                    throw new Exception("Selecting a Host Employee is mandatory to generate a pass.");
                }

                string fullName = txtFirstName.Text.Trim() + " " + txtLastName.Text.Trim();
                int branchId = Convert.ToInt32(hfBranchId.Value);
                int newVisitId = 0;
                string hostEmail = "", hostName = "";

                // --- 1. Internal Employee Self-Approval Logic ---
                string currentApprovalStatus = "Pending";

                if (ddlPurpose.SelectedValue == "I Work Here")
                {
                    string internalCode = txtInternalEmpCode.Text.Trim();
                    if (string.IsNullOrEmpty(internalCode))
                    {
                        throw new Exception("You must provide your internal Employee Code to self-approve your pass.");
                    }

                    // 🌟 UPGRADE: Fetch actual employee details instead of just a COUNT
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

                                    // 2. Fetch the official name from the Master Table
                                    fullName = sdrCheck["EmployeeName"].ToString();

                                    // 3. Silently override the manual text inputs to ensure DB consistency
                                    string[] nameParts = fullName.Split(new[] { ' ' }, 2);
                                    txtFirstName.Text = nameParts.Length > 0 ? nameParts[0] : fullName;
                                    txtLastName.Text = nameParts.Length > 1 ? nameParts[1] : "";

                                    // 4. Force the company name to your internal standard
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

                using (SqlConnection con = new SqlConnection(connStr))
                {
                    con.Open();
                    DateTime validFrom = DateTime.Now;
                    DateTime validUpto = DateTime.Now.AddHours(8);
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
                            if (validUpto <= validFrom) validUpto = validFrom.AddHours(8);
                        }
                    }

                    // --- 2. Generate a secure, unguessable token for the QR Code ---
                    Guid newQrToken = Guid.NewGuid();

                    // --- 3. Insert the Visit Log ---
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
                        cmd.Parameters.AddWithValue("@QRToken", newQrToken); // Added QRToken parameter

                        newVisitId = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // 4. Get Host Details for Notification
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

                    // 5. Queue the appropriate email based on the Approval Status
                    if (!string.IsNullOrEmpty(hostEmail))
                    {
                        if (currentApprovalStatus == "Pending")
                        {
                            // Standard Visitor: Requires Action & Approval
                            string subject = $"Approval Needed: Visitor {fullName} has arrived";
                            byte[] plainTextBytes = System.Text.Encoding.UTF8.GetBytes(hostEmpCode);
                            string encodedEmpCode = Convert.ToBase64String(plainTextBytes);

                            string quickApproveLink = $"https://apps.anmolindustries.com/VMS_HostQuickLogin.aspx?vid={newVisitId}&t={encodedEmpCode}";

                            string body = $@"
                            <div style='font-family: Arial; padding: 20px;'>
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
                            // Internal Employee: Auto-approved, send FYI only
                            string fyiSubject = $"FYI: Internal Colleague {fullName} has arrived";

                            string fyiBody = $@"
                            <div style='font-family: Arial; padding: 20px;'>
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
    }
}