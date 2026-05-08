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
        // Must be static to be used in the WebMethod
        static string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // 1. Ensure they passed the Dual-OTP screen
                // FIX: Changed from '||' to '&&' so it only kicks them out if BOTH are missing
                if ((Session["VerifiedMobile"] == null && Session["VerifiedEmail"] == null) || Request.QueryString["BranchId"] == null)
                {
                    Response.Redirect("VMS_SelfLogin.aspx", false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }

                // 2. Lock context into hidden fields
                hfBranchId.Value = Request.QueryString["BranchId"].ToString();

                // (Optional) You can uncomment these if you add txtMobile and txtEmail to your ASPX
                // txtMobile.Text = Session["VerifiedMobile"] != null ? Session["VerifiedMobile"].ToString() : "";
                // txtEmail.Text = Session["VerifiedEmail"] != null ? Session["VerifiedEmail"].ToString() : "";
                // txtMobile.ReadOnly = true;
                // txtEmail.ReadOnly = true;
            }
        }

        // --- STEP 3: AJAX Host Search WebMethod ---
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<HostEmployee> SearchHost(string prefixText, int branchId)
        {
            List<HostEmployee> hosts = new List<HostEmployee>();

            string query = @"
                SELECT TOP 10 
                    u.EmployeeCode, 
                    u.EmployeeName, 
                    ISNULL(d.Department_Description, 'General') AS DepartmentName
                FROM MST_UserMaster u
                LEFT JOIN MST_Department d ON u.DepartmentId = CAST(d.Department_ID AS INT)
                WHERE u.BranchId = @BranchId 
                  AND u.EmployeeStatus = 'Active' 
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

                                // Updated to hide the EmployeeCode from the UI dropdown
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

        // --- STEP 5: Form Submission & Notification Dispatch ---
        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. Process and Save the Base64 Image
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

                // ---------------------------------------------------------
                // 2. EXTRACT DATA (FIXED: Null Reference Handled Here)
                // ---------------------------------------------------------
                string visitorMobile = Session["VerifiedMobile"] != null ? Session["VerifiedMobile"].ToString() : "";
                string visitorEmail = Session["VerifiedEmail"] != null ? Session["VerifiedEmail"].ToString() : "";

                // SQL Truncation Safety Net
                if (visitorMobile.Length > 15) visitorMobile = visitorMobile.Substring(0, 15);

                string hostEmpCode = hfSelectedHostCode.Value;
                string fullName = txtFirstName.Text.Trim() + " " + txtLastName.Text.Trim();
                int branchId = Convert.ToInt32(hfBranchId.Value);
                int newVisitId = 0;
                string hostEmail = "", hostName = "";

                using (SqlConnection con = new SqlConnection(connStr))
                {
                    con.Open();
                    DateTime validFrom = DateTime.Now;
                    DateTime validUpto = DateTime.Now.AddHours(4);
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

                            if (validUpto <= validFrom)
                            {
                                validUpto = validFrom.AddHours(4);
                            }
                        }
                    }

                    // 3. Database Insertion
                    string insertQuery = @"
                    INSERT INTO VMS_VisitLog 
                    (PassCategory, VisitorName, VisitorFirstName, VisitorLastName, VisitorMobile, VisitorEmail, VisitorCompany, VisitorDesignation, PhotoPath, TargetBranchID, HostEmployeeCode, PurposeOfVisit, ApprovalStatus, ValidFrom, ValidUpto)
                    VALUES 
                    (@Category, @FullName, @FirstName, @LastName, @Mobile, @Email, @Company, @Designation, @PhotoPath, @BranchID, @HostCode, 'General Visit', 'Pending', @ValidFrom, @ValidUpto);
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
                        cmd.Parameters.AddWithValue("@ValidFrom", validFrom);
                        cmd.Parameters.AddWithValue("@ValidUpto", validUpto);

                        newVisitId = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // Get Host Details for Notification
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

                    // 4. Queue the Approval Notification to Host
                    if (!string.IsNullOrEmpty(hostEmail))
                    {
                        string subject = $"Approval Needed: Visitor {fullName} has arrived";
                        string body = $@"
                        <div style='font-family: Arial; padding: 20px;'>
                            <h3 style='color: #1ABB9C;'>Hello {hostName},</h3>
                            <p><strong>{fullName}</strong> from <strong>{txtCompany.Text}</strong> is waiting for your approval at the reception.</p>
                            <p><strong>Purpose:</strong> {ddlPurpose.SelectedItem.Text}</p>
                            <br/>
                            <a href='https://apps.anmolindustries.com/VMS_MyVisitors.aspx' style='background: #1ABB9C; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Review & Approve Visit</a>
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
                }

                // 5. Restore the 'VerifiedContact' session key for VMS_MyVisits.aspx to read smoothly!
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