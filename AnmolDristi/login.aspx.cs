using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Threading;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Configuration;
using System.Net.Mail;
using System.Net;

namespace AnmolDristi
{
    public partial class login : System.Web.UI.Page
    {
        private const string GenericErrorMessage = "We could not complete your request right now. Please try again or contact IT support.";

        DB_Utility_OH4Y dbcl = new DB_Utility_OH4Y();
        DataTable dt = new DataTable();
        static string User_Photo = string.Empty;
        static readonly string rootFolder = @"C:\";
        static readonly string localFolder = @"D:\";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                Img_CpLogo.ImageUrl = "WebData/anmol_logo_GPTW.jpg";
                lbl_companyname.Text = "Anmol Industries Limited";
                lbl_currentyr.Text = DateTime.Now.Year.ToString();
                lbl_compfooter.Text = "Anmol Industries Limited";
                lbl_owner.Text = "IT Department";
                txt_loginid.Focus();
                ClearLoginMessage();
            }
        }

        protected void Page_Error(object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError();
            if (ex == null) return;

            LogLoginError("Unhandled login page error.", ex);
            Server.ClearError();
            ShowLoginMessage(GenericErrorMessage, isError: true);
        }

        protected void btn_signin_Click(object sender, EventArgs e)
        {
            RunSafe("SignIn", () =>
            {
                ClearLoginMessage();
                ProcessSignIn();
            });
        }

        private void RunSafe(string operation, Action action)
        {
            try
            {
                action();
            }
            catch (ThreadAbortException)
            {
                // Expected after Response.Redirect
            }
            catch (SqlException sqlEx)
            {
                LogLoginError(operation + " database error.", sqlEx, txt_loginid?.Text);
                ShowLoginMessage("A database error occurred. Please try again later.", isError: true);
            }
            catch (Exception ex)
            {
                LogLoginError(operation + " failed.", ex, txt_loginid?.Text);
                ShowLoginMessage(GenericErrorMessage, isError: true);
            }
        }

        private void ClearLoginMessage()
        {
            if (pnlLoginMessage != null) pnlLoginMessage.Visible = false;
            if (lblLoginMessage != null) lblLoginMessage.Text = string.Empty;
        }

        private void ShowLoginMessage(string message, bool isError)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            if (pnlLoginMessage != null)
            {
                pnlLoginMessage.Visible = true;
                pnlLoginMessage.CssClass = isError ? "alert alert-danger mb-3" : "alert alert-success mb-3";
            }
            if (lblLoginMessage != null)
                lblLoginMessage.Text = HttpUtility.HtmlEncode(message);
        }

        private void ShowAlert(string message, bool isError = true)
        {
            ShowLoginMessage(message, isError);
            string safe = HttpUtility.JavaScriptStringEncode(message ?? string.Empty);
            string script = "if (typeof alert === 'function') { alert('" + safe + "'); }";
            ScriptManager.RegisterStartupScript(this, GetType(), "loginAlert_" + Guid.NewGuid().ToString("N"), script, true);
        }

        private void LogDebug(string text)
        {
            LogLogin("INFO", text);
        }

        private void LogLogin(string level, string message, Exception ex = null, string employeeCode = null)
        {
            try
            {
                var parts = new List<string> { "IP=" + GetClientIP() };
                string emp = employeeCode ?? txt_loginid?.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(emp))
                    parts.Add("Emp=" + emp);
                if (Request?.UserAgent != null)
                    parts.Add("UA=" + Request.UserAgent);

                string fullMessage = message + " | " + string.Join("; ", parts);
                string baseDir = Server.MapPath("~/Logs/");
                if (!Directory.Exists(baseDir))
                    Directory.CreateDirectory(baseDir);

                string path = Path.Combine(baseDir, "login_" + DateTime.Now.ToString("yyyy-MM-dd") + ".log");
                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " [" + level + "] " + fullMessage;
                if (ex != null)
                    line += Environment.NewLine + ex;

                File.AppendAllText(path, line + Environment.NewLine);
            }
            catch
            {
                // Never throw from logging.
            }
        }

        private void LogLoginError(string message, Exception ex = null, string employeeCode = null)
        {
            LogLogin("ERROR", message, ex, employeeCode);
        }

        private void LogLoginWarn(string message, string employeeCode = null)
        {
            LogLogin("WARN", message, null, employeeCode);
        }

        private void LogLoginInfo(string message, string employeeCode = null)
        {
            LogLogin("INFO", message, null, employeeCode);
        }

        private void ProcessSignIn()
        {
            string id = txt_loginid.Text.Trim();
            string pass = txt_password.Text;
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pass))
            {
                ShowAlert("Please enter your employee code and password.");
                return;
            }

            DB_Utility_OH4Y db = new DB_Utility_OH4Y();
            UserAuthService auth = new UserAuthService(db);

            DataRow row = null;
            try
            {
                row = auth.AuthenticateAndMigrateIfNeeded(id, pass);
            }
            catch (Exception exAuth)
            {
                LogLoginError("Authentication failed.", exAuth, id);
                ShowAlert("Sign-in failed. Please verify your credentials and try again.");
                return;
            }

            if (row == null)
            {
                LogLoginWarn("Invalid credentials.", id);
                ShowAlert("Unknown user or password.");
                txt_loginid.Text = string.Empty;
                return;
            }

            string workStatus = row["EmployeeStatus"] == DBNull.Value ? "" : row["EmployeeStatus"].ToString();
            LogDebug("EmployeeStatus for " + id + " = " + workStatus);

            if (!workStatus.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                LogLoginWarn("Inactive user sign-in blocked. Status=" + workStatus, id);
                ShowAlert("Your account is inactive (" + workStatus + "). Please contact HR/IT.");
                txt_loginid.Text = string.Empty;
                return;
            }

                // after successful login and fetched fields:
                int userId = Convert.ToInt32(row["Id"]);
                string workman = row["EmployeeCode"].ToString();     // employee code
                string fullName = row["EmployeeName"].ToString();
                string firstName = string.IsNullOrWhiteSpace(fullName) ? ""
                                          : fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0];

                // MINIMAL session items (keep for new code)
                Session.Clear();
                Session["CurrentUserId"] = userId;
                Session["WORKMAN"] = workman;
                Session["USERFNAME"] = firstName;
                Session["USERNAME"] = fullName;
                Session["USERID"] = userId;
                Session["REGION"] = row["RegionId"] == DBNull.Value ? "" : row["RegionId"].ToString();
                Session["BRANCH"] = row["BranchId"] == DBNull.Value ? "" : row["BranchId"].ToString();
                Session["MODULE"] = row["Module"] == DBNull.Value ? "" : row["Module"].ToString();
                Session["PLANTID"] = row["PlantId"] == DBNull.Value ? "" : row["PlantId"].ToString();
                Session["U_SITE"] = row["DepartmentId"] == DBNull.Value ? "" : row["DepartmentId"].ToString();
                Session["U_SITECODE"] = row["SubDepartmentId"] == DBNull.Value ? "" : row["SubDepartmentId"].ToString();
                Session["DESG"] = row["Designation_ID"] == DBNull.Value ? "" : row["Designation_ID"].ToString();
                Session["GRADE"] = row["GradeId"] == DBNull.Value ? "" : row["GradeId"].ToString();

                Session["RONE_NAME"] = row["ReportingManager"] == DBNull.Value ? "" : row["ReportingManager"].ToString();
                Session["RONE_CODE"] = row["ReportingManagerCode"] == DBNull.Value ? "" : row["ReportingManagerCode"].ToString();
                Session["RTWO_NAME"] = row["ReportingManagersManagerName"] == DBNull.Value ? "" : row["ReportingManagersManagerName"].ToString();
                Session["RTWO_CODE"] = row["ReportingManagersManagerCode"] == DBNull.Value ? "" : row["ReportingManagersManagerCode"].ToString();
                Session["AUDITOR_NAME"] = "Rohan Saraf";
                Session["AUDITOR_CODE"] = "Audit";

                //Session["AUDITOR_NAME"] = row["AuditorName"] == DBNull.Value ? "" : row["AuditorName"].ToString();
                //Session["AUDITOR_CODE"] = row["AuditorCode"] == DBNull.Value ? "" : row["AuditorCode"].ToString();

                // ---------------------------------------------------------
                // --- NEW CODE: Fetch and Store ERP Plant ID Safely ---
                // ---------------------------------------------------------
                try
                {
                    int erpPlantId = 0;
                    // Uses the SAP_Code bridge column we established in the database
                    string erpQuery = @"
                        SELECT PD.plant_id 
                        FROM MST_UserMaster U
                        INNER JOIN MST_Plant P ON U.PlantId = P.PlantID_ID
                        INNER JOIN MST_PlantDetails PD ON P.SAP_Code = PD.sap_code
                        WHERE U.EmployeeCode = @EmployeeCode";

                    using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
                    using (SqlCommand cmd = new SqlCommand(erpQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@EmployeeCode", workman);
                        con.Open();
                        object erpResult = cmd.ExecuteScalar();

                        if (erpResult != null && erpResult != DBNull.Value)
                        {
                            erpPlantId = Convert.ToInt32(erpResult);
                        }
                    }

                    Session["ERPPlantId"] = erpPlantId;
                    LogDebug("Successfully fetched ERPPlantId=" + erpPlantId + " for user=" + workman);
                }
                catch (Exception exErp)
                {
                    // Failsafe: Set to 0 and log it, but do NOT block the login process
                    Session["ERPPlantId"] = 0;
                    LogDebug("Non-fatal error: Failed to fetch ERPPlantId for user " + workman + " : " + exErp.ToString());
                }

                // --- Begin: Hash-on-login / migration logic ---
                bool migrated = false;
                try
                {
                    string storedHash = row["PasswordHash"] == DBNull.Value ? null : row["PasswordHash"].ToString();
                    string storedSalt = row["PasswordSalt"] == DBNull.Value ? null : row["PasswordSalt"].ToString();
                    int storedIterations = PasswordHasher.DefaultIterations;
                    if (row["PasswordIterations"] != DBNull.Value)
                    {
                        int tmp;
                        if (int.TryParse(row["PasswordIterations"].ToString(), out tmp))
                            storedIterations = tmp;
                    }

                    bool needUpdate = false;
                    string newHash = null;
                    string newSalt = null;
                    int newIterations = PasswordHasher.DefaultIterations; // canonical desired iterations

                    // Case A: No stored hash (legacy plaintext present) -> migrate
                    if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
                    {
                        var tup = PasswordHasher.CreateHash(pass, newIterations);
                        newHash = tup.Item1;
                        newSalt = tup.Item2;
                        newIterations = tup.Item3;
                        needUpdate = true;
                    }
                    else
                    {
                        // Case B: Hash exists but iterations are weaker -> re-hash and update
                        if (storedIterations < PasswordHasher.DefaultIterations)
                        {
                            bool verified = PasswordHasher.Verify(pass, storedHash, storedSalt, storedIterations);
                            if (verified)
                            {
                                var tup = PasswordHasher.CreateHash(pass, newIterations);
                                newHash = tup.Item1;
                                newSalt = tup.Item2;
                                newIterations = tup.Item3;
                                needUpdate = true;
                            }
                        }
                    }

                    if (needUpdate)
                    {
                        string updateSql = @"UPDATE MST_UserMaster
                                     SET PasswordHash = @PasswordHash,
                                         PasswordSalt = @PasswordSalt,
                                         PasswordIterations = @Iterations,
                                         Password = NULL
                                     WHERE Id = @Id";

                        System.Data.SqlClient.SqlParameter[] upParams = new System.Data.SqlClient.SqlParameter[]
                        {
                    new System.Data.SqlClient.SqlParameter("@PasswordHash", (object)newHash ?? (object)DBNull.Value),
                    new System.Data.SqlClient.SqlParameter("@PasswordSalt", (object)newSalt ?? (object)DBNull.Value),
                    new System.Data.SqlClient.SqlParameter("@Iterations", newIterations),
                    new System.Data.SqlClient.SqlParameter("@Id", userId)
                        };

                        try
                        {
                            int rowsAffected = db.ExecuteNonQuery(updateSql, upParams);
                            if (rowsAffected > 0)
                            {
                                migrated = true;

                                // IMPORTANT: update the in-memory row so the rest of this request
                                // sees the new hash/iterations (and to avoid re-migration)
                                if (row.Table.Columns.Contains("PasswordHash"))
                                    row["PasswordHash"] = newHash;
                                if (row.Table.Columns.Contains("PasswordSalt"))
                                    row["PasswordSalt"] = newSalt;
                                if (row.Table.Columns.Contains("PasswordIterations"))
                                    row["PasswordIterations"] = newIterations;

                                LogDebug("Password hash migrated/upgraded for userId=" + userId);
                            }
                            else
                            {
                                LogDebug("Password migration update executed but affected 0 rows for userId=" + userId);
                            }
                        }
                        catch (Exception exUpdate)
                        {
                            // Log and continue (do not block login)
                            LogDebug("Password migration DB update failed for userId=" + userId + " : " + exUpdate.ToString());
                        }
                    }
                }
                catch (Exception exMig)
                {
                    // Log and continue — hashing errors should not block login
                    LogDebug("Password migration error for user " + workman + " : " + exMig.ToString());
                }
                // --- End migration logic ---

                // Create active session (SessionHelper returns Tuple<int,Guid>)
                Tuple<int, Guid> result = null;
                try
                {
                    result = SessionHelper.CreateOrReuseActiveSession(userId, workman);
                }
                catch (Exception exSess)
                {
                    LogDebug("CreateActiveSession exception: " + exSess.ToString());
                }

                LogDebug("CreateActiveSession result: " + (result == null ? "NULL" : ("id=" + result.Item1 + ", token=" + result.Item2)));

                if (result != null)
                {
                    int activeSessionId = result.Item1;
                    Guid loginToken = result.Item2;

                    Session["ActiveSessionId"] = activeSessionId;
                    Session["LoginToken"] = loginToken.ToString();

                    //Session["ActiveSessionId"] = result.Item1;
                    //Session["LoginToken"] = result.Item2.ToString();

                    Page.ClientScript.RegisterStartupScript(this.GetType(), "storeToken", $"sessionStorage.setItem('LoginToken','{loginToken}'); sessionStorage.setItem('ActiveSessionId','{activeSessionId}');", true);
                }
                else
                {
                    // don't fail login solely on session table insertion - still allow user in
                    LogDebug("Proceeding with login although session table insert returned null.");
                }

                // Update last login - wrap in try to avoid breaking flow
                try { UpdateUserLastLogin(userId); }
                catch (Exception exLast) { LogDebug("UpdateUserLastLogin failed: " + exLast.ToString()); }

            // Redirect - use CompleteRequest to ensure redirect completes without ThreadAbortException
            string target = "~/home.aspx";
            LogLoginInfo("Sign-in successful. Redirecting to home.", workman);
            Response.Redirect(target, false);
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }

        private void CredentialCheckerSecure_Debug()
        {
            ProcessSignIn();
        }


        private void CredentialCheckerSecure_Debug_OLD()
        {
            try
            {
                string id = txt_loginid.Text.Trim();
                string pass = txt_password.Text;
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pass))
                {
                    ShowAlert("Missing credentials");
                    return;
                }

                // instantiate db + auth service
                DB_Utility_OH4Y db = new DB_Utility_OH4Y();
                UserAuthService auth = new UserAuthService(db);

                DataRow row = null;
                try
                {
                    row = auth.AuthenticateAndMigrateIfNeeded(id, pass);
                }
                catch (Exception exAuth)
                {
                    LogDebug("Authenticate failed: " + exAuth.ToString());
                    ShowAlert("Auth error: " + exAuth.Message);
                    return;
                }

                if (row == null)
                {
                    LogDebug("Authenticate returned NULL for EmployeeCode=" + id);
                    ShowAlert("Unknown user / password.");
                    txt_loginid.Text = "";
                    return;
                }

                string workStatus = row["EmployeeStatus"] == DBNull.Value ? "" : row["EmployeeStatus"].ToString();
                LogDebug("EmployeeStatus for " + id + " = " + workStatus);

                if (!workStatus.Equals("Active", StringComparison.OrdinalIgnoreCase))
                {
                    ShowAlert("User ID is InActive: " + workStatus);
                    txt_loginid.Text = "";
                    return;
                }

                //int userId = Convert.ToInt32(row["Id"]);
                //string Workman = row["EmployeeCode"].ToString();
                //string fullName = row["EmployeeName"].ToString();
                //string firstName = string.IsNullOrWhiteSpace(fullName) ? "" : fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0];

                //// Set minimal session
                //Session.Clear();
                //Session["CurrentUserId"] = userId;
                //Session["WORKMAN"] = Workman;
                //Session["USERFNAME"] = firstName;
                //Session["USERNAME"] = fullName;

                // after successful login and fetched fields:
                int userId = Convert.ToInt32(row["Id"]);
                string workman = row["EmployeeCode"].ToString();     // employee code
                string fullName = row["EmployeeName"].ToString();
                string firstName = string.IsNullOrWhiteSpace(fullName) ? ""
                                          : fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0];

                // MINIMAL session items (keep for new code)
                Session["EmployeeCode"] = workman;
                Session["CurrentUserId"] = userId;
                Session["WORKMAN"] = workman;
                Session["USERFNAME"] = firstName;
                Session["USERNAME"] = fullName;

                // ALSO set legacy keys expected by home.aspx
                // NOTE: home.aspx checks for Session["USERID"] and Session["USERNAME"]
                Session["USERID"] = workman;   // or set to userId.ToString() if your app expects numeric; match earlier behavior
                Session["USERNAME"] = fullName;

                // Create active session (SessionHelper returns Tuple<int,Guid>)
                Tuple<int, Guid> result = null;
                try
                {
                    result = SessionHelper.CreateActiveSession(userId, workman);
                }
                catch (Exception exSess)
                {
                    LogDebug("CreateActiveSession exception: " + exSess.ToString());
                }

                LogDebug("CreateActiveSession result: " + (result == null ? "NULL" : ("id=" + result.Item1 + ", token=" + result.Item2)));

                if (result != null)
                {
                    Session["ActiveSessionId"] = result.Item1;
                    Session["LoginToken"] = result.Item2.ToString();
                }
                else
                {
                    // don't fail login solely on session table insertion - still allow user in
                    LogDebug("Proceeding with login although session table insert returned null.");
                }

                // Update last login - wrap in try to avoid breaking flow
                try { UpdateUserLastLogin(userId); }
                catch (Exception exLast) { LogDebug("UpdateUserLastLogin failed: " + exLast.ToString()); }

                // Redirect - use CompleteRequest to ensure redirect completes without ThreadAbortException
                string target = "~/home.aspx";
                LogDebug("Redirecting to " + target);
                Response.Redirect(target, false);
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
            catch (Exception ex)
            {
                LogDebug("CredentialCheckerSecure_Debug top-level exception: " + ex.ToString());
                ShowAlert("Unexpected error: " + ex.Message);
            }
        }

        private void CredentialCheckerSecure_Debug_OLD2()
        {
            try
            {
                string id = txt_loginid.Text.Trim();
                string pass = txt_password.Text;
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pass))
                {
                    ShowAlert("Missing credentials");
                    return;
                }

                // instantiate db + auth service
                DB_Utility_OH4Y db = new DB_Utility_OH4Y();
                UserAuthService auth = new UserAuthService(db);

                DataRow row = null;
                try
                {
                    row = auth.AuthenticateAndMigrateIfNeeded(id, pass);
                }
                catch (Exception exAuth)
                {
                    LogDebug("Authenticate failed: " + exAuth.ToString());
                    ShowAlert("Auth error: " + exAuth.Message);
                    return;
                }

                if (row == null)
                {
                    LogDebug("Authenticate returned NULL for EmployeeCode=" + id);
                    ShowAlert("Unknown user / password.");
                    txt_loginid.Text = "";
                    return;
                }

                string workStatus = row["EmployeeStatus"] == DBNull.Value ? "" : row["EmployeeStatus"].ToString();
                LogDebug("EmployeeStatus for " + id + " = " + workStatus);

                if (!workStatus.Equals("Active", StringComparison.OrdinalIgnoreCase))
                {
                    ShowAlert("User ID is InActive: " + workStatus);
                    txt_loginid.Text = "";
                    return;
                }

                // after successful login and fetched fields:
                int userId = Convert.ToInt32(row["Id"]);
                string workman = row["EmployeeCode"].ToString();     // employee code
                string fullName = row["EmployeeName"].ToString();
                string firstName = string.IsNullOrWhiteSpace(fullName) ? "" : fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0];

                Session.Clear();
                // MINIMAL session items (keep for new code)
                Session["CurrentUserId"] = userId;
                Session["WORKMAN"] = workman;
                Session["USERFNAME"] = firstName;
                Session["USERNAME"] = fullName;
                Session["USERID"] = workman;

                Session["REGION"] = row["RegionId"] == DBNull.Value ? "" : row["RegionId"].ToString();
                Session["BRANCH"] = row["BranchId"] == DBNull.Value ? "" : row["BranchId"].ToString();
                Session["MODULE"] = row["Module"] == DBNull.Value ? "" : row["Module"].ToString();
                Session["PLANTID"] = row["PlantId"] == DBNull.Value ? "" : row["PlantId"].ToString();
                Session["U_SITE"] = row["DepartmentId"] == DBNull.Value ? "" : row["DepartmentId"].ToString();
                Session["U_SITECODE"] = row["SubDepartmentId"] == DBNull.Value ? "" : row["SubDepartmentId"].ToString();
                Session["DESG"] = row["Designation_ID"] == DBNull.Value ? "" : row["Designation_ID"].ToString();
                Session["GRADE"] = row["GradeId"] == DBNull.Value ? "" : row["GradeId"].ToString();

                // Reporting managers
                Session["RONE_NAME"] = row["ReportingManager"] == DBNull.Value ? "" : row["ReportingManager"].ToString();
                Session["RONE_CODE"] = row["ReportingManagerCode"] == DBNull.Value ? "" : row["ReportingManagerCode"].ToString();
                Session["RTWO_NAME"] = row["ReportingManagersManagerName"] == DBNull.Value ? "" : row["ReportingManagersManagerName"].ToString();
                Session["RTWO_CODE"] = row["ReportingManagersManagerCode"] == DBNull.Value ? "" : row["ReportingManagersManagerCode"].ToString();

                // --- Begin: Hash-on-login / migration logic ---
                try
                {
                    string storedHash = row["PasswordHash"] == DBNull.Value ? null : row["PasswordHash"].ToString();
                    string storedSalt = row["PasswordSalt"] == DBNull.Value ? null : row["PasswordSalt"].ToString();
                    int storedIterations = PasswordHasher.DefaultIterations;
                    if (row["PasswordIterations"] != DBNull.Value)
                    {
                        int tmp;
                        if (int.TryParse(row["PasswordIterations"].ToString(), out tmp))
                            storedIterations = tmp;
                    }

                    bool needUpdate = false;
                    string newHash = null;
                    string newSalt = null;
                    int newIterations = PasswordHasher.DefaultIterations; // canonical desired iterations

                    // Case A: No stored hash (legacy plaintext present) -> migrate
                    if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
                    {
                        var tup = PasswordHasher.CreateHash(pass, newIterations);
                        newHash = tup.Item1;
                        newSalt = tup.Item2;
                        newIterations = tup.Item3;
                        needUpdate = true;
                    }
                    else
                    {
                        // Case B: Hash exists but iterations are weaker -> re-hash and update
                        if (storedIterations < PasswordHasher.DefaultIterations)
                        {
                            bool verified = PasswordHasher.Verify(pass, storedHash, storedSalt, storedIterations);
                            if (verified)
                            {
                                var tup = PasswordHasher.CreateHash(pass, newIterations);
                                newHash = tup.Item1;
                                newSalt = tup.Item2;
                                newIterations = tup.Item3;
                                needUpdate = true;
                            }
                        }
                    }

                    if (needUpdate)
                    {
                        string updateSql = @"UPDATE MST_UserMaster
                                     SET PasswordHash = @PasswordHash,
                                         PasswordSalt = @PasswordSalt,
                                         PasswordIterations = @Iterations,
                                         Password = NULL
                                     WHERE Id = @Id";

                        System.Data.SqlClient.SqlParameter[] upParams = new System.Data.SqlClient.SqlParameter[]
                        {
                    new System.Data.SqlClient.SqlParameter("@PasswordHash", (object)newHash ?? (object)DBNull.Value),
                    new System.Data.SqlClient.SqlParameter("@PasswordSalt", (object)newSalt ?? (object)DBNull.Value),
                    new System.Data.SqlClient.SqlParameter("@Iterations", newIterations),
                    new System.Data.SqlClient.SqlParameter("@Id", userId)
                        };

                        try
                        {
                            db.ExecuteNonQuery(updateSql, upParams);
                            LogDebug("Password hash migrated/upgraded for userId=" + userId);
                        }
                        catch (Exception exUpdate)
                        {
                            // Log and continue (do not block login)
                            LogDebug("Password migration DB update failed for userId=" + userId + " : " + exUpdate.ToString());
                        }
                    }
                }
                catch (Exception exMig)
                {
                    // Log and continue — hashing errors should not block login
                    LogDebug("Password migration error for user " + workman + " : " + exMig.ToString());
                }
                // --- End migration logic ---

                // Create active session (SessionHelper returns Tuple<int,Guid>)
                Tuple<int, Guid> result = null;
                try
                {
                    result = SessionHelper.CreateActiveSession(userId, workman);
                }
                catch (Exception exSess)
                {
                    LogDebug("CreateActiveSession exception: " + exSess.ToString());
                }

                LogDebug("CreateActiveSession result: " + (result == null ? "NULL" : ("id=" + result.Item1 + ", token=" + result.Item2)));

                if (result != null)
                {
                    Session["ActiveSessionId"] = result.Item1;
                    Session["LoginToken"] = result.Item2.ToString();
                }
                else
                {
                    // don't fail login solely on session table insertion - still allow user in
                    LogDebug("Proceeding with login although session table insert returned null.");
                }

                // Update last login - wrap in try to avoid breaking flow
                try { UpdateUserLastLogin(userId); }
                catch (Exception exLast) { LogDebug("UpdateUserLastLogin failed: " + exLast.ToString()); }

                // Redirect - use CompleteRequest to ensure redirect completes without ThreadAbortException
                string target = "~/home.aspx";
                LogDebug("Redirecting to " + target);
                Response.Redirect(target, false);
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
            catch (Exception ex)
            {
                LogDebug("CredentialCheckerSecure_Debug top-level exception: " + ex.ToString());
                ShowAlert("Unexpected error: " + ex.Message);
            }
        }


        private void CredentialCheckerSecure()
        {
            string id = txt_loginid.Text.Trim();
            string pass = txt_password.Text;
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pass))
            {
                ShowAlert("Unknown user / password.");
                return;
            }

            var authService = new UserAuthService(dbcl);
            var row = authService.AuthenticateAndMigrateIfNeeded(id, pass);

            if (row == null) { ShowAlert("Auth failed: row==null"); return; }

            string workStatus = row["EmployeeStatus"].ToString();
            if (!string.Equals(workStatus, "Active", StringComparison.OrdinalIgnoreCase))
            {
                ShowAlert("User ID is InActive");
                txt_loginid.Text = "";
                return;
            }

            // pull minimal fields
            int userId = Convert.ToInt32(row["Id"]);
            string RegionId = row["RegionId"].ToString();
            string BranchId = row["BranchId"].ToString();
            string PlantId = row["PlantId"].ToString();
            string Workman = row["EmployeeCode"].ToString();
            string fullName = row["EmployeeName"].ToString();
            string firstName = string.IsNullOrWhiteSpace(fullName) ? "" : fullName.Split(' ')[0];

            // minimal session items only
            Session.Clear(); // optional: clear any previous sensitive data
            Session["CurrentUserId"] = userId;
            Session["WORKMAN"] = Workman;
            Session["USERFNAME"] = firstName;
            Session["USERNAME"] = fullName;
            Session["REGION"] = RegionId;
            Session["BRANCH"] = BranchId;
            Session["MODULE"] = row["Module"].ToString();
            Session["PLANTID"] = PlantId;
            Session["U_SITE"] = row["DepartmentId"].ToString();
            Session["U_SITECODE"] = row["SubDepartmentId"].ToString();
            Session["DESG"] = row["Designation_ID"].ToString();
            Session["GRADE"] = row["GradeId"].ToString();

            UpdateUserLastLogin(userId);
            //var result = SessionHelper.CreateActiveSession(userId, Workman);
            //if (result != null)
            //{
            //    Session["ActiveSessionId"] = result.Item1;
            //    Session["LoginToken"] = result.Item2.ToString();
            //}

            var result = SessionHelper.CreateActiveSession(userId, Workman);
            if (result != null && result.Item1 > 0)
            {
                Session["ActiveSessionId"] = result.Item1;          // page's Session is fine here
                Session["LoginToken"] = result.Item2.ToString();
            }

            Response.Redirect("home.aspx", false);
        }

        private void HandleException(Exception ex)
        {
            LogLoginError("Legacy login handler exception.", ex, txt_loginid?.Text);
            ShowLoginMessage(GenericErrorMessage, isError: true);
        }

        private void CredentialChecker1()
        {
            if (txt_loginid.Text != "" && txt_password.Text != "")
            {
                //string id = "ATS00200";
                //string pass = "UDB17v";

                string id = txt_loginid.Text;
                string pass = txt_password.Text;

                string query = "SELECT * from MST_UserMaster where EmployeeCode=@EmployeeCode and Password=@Password";
                SqlParameter[] pram = {
                                          new SqlParameter("@EmployeeCode",id),
                                          new SqlParameter("@Password",pass),
                                      };
                dt = dbcl.SPreturn_dt(query, pram);
                if (dt.Rows.Count > 0)
                {
                    string WorkStatus = dt.Rows[0]["EmployeeStatus"].ToString();

                    if (WorkStatus == "Active")
                    {
                        string EmployeeCode = dt.Rows[0]["Id"].ToString();
                        int userId = Convert.ToInt32(dt.Rows[0]["Id"]);
                        string RegionId = dt.Rows[0]["RegionId"].ToString();
                        string BranchId = dt.Rows[0]["BranchId"].ToString();
                        string PlantId = dt.Rows[0]["PlantId"].ToString();
                        string Workman = dt.Rows[0]["EmployeeCode"].ToString();

                        string Module = dt.Rows[0]["Module"].ToString();
                        //string User_FirstName = dt.Rows[0]["EmployeeName"].ToString();

                        // Assuming the input string is in dt.Rows[0]["EmployeeName"]
                        string fullName = dt.Rows[0]["EmployeeName"].ToString();

                        // Split the full name by spaces and take the first part
                        string User_FirstName = fullName.Split(' ')[0];

                        string User_FullName = dt.Rows[0]["EmployeeName"].ToString();

                        //string User_Type = dt.Rows[0]["User_RoleType"].ToString();
                        //string User_Permission = dt.Rows[0]["Role_Permission"].ToString();

                        string User_Worksite = dt.Rows[0]["DepartmentId"].ToString();
                        string User_WRKSTCode = dt.Rows[0]["SubDepartmentId"].ToString();
                        string User_Grade = dt.Rows[0]["GradeId"].ToString();
                        string User_Desg = dt.Rows[0]["Designation_ID"].ToString();

                        string User_ROne_Code = dt.Rows[0]["ReportingManagerCode"].ToString();
                        string User_Rone_Name = dt.Rows[0]["ReportingManager"].ToString();
                        string User_RTwo_Code = dt.Rows[0]["ReportingManagersManagerCode"].ToString();
                        string User_RTWo_Name = dt.Rows[0]["ReportingManagersManagerName"].ToString();

                        //User_Photo = dt.Rows[0]["PrfPicFile"].ToString();
                        //string User_PhotoPath = dt.Rows[0]["PrfPicPath"].ToString();

                        Session["USERID"] = EmployeeCode;
                        Session["Password"] = pass;
                        Session["WORKMAN"] = Workman;
                        Session["USERFNAME"] = User_FirstName;
                        Session["USERNAME"] = User_FullName;
                        Session["CurrentUserId"] = userId;          // NEW: numeric Id (49 for ADMIN)
                        //Session["USERTYPE"] = User_Type;
                        //Session["PERMISSION"] = User_Permission;
                        Session["REGION"] = RegionId;
                        Session["BRANCH"] = BranchId;
                        Session["MODULE"] = Module;
                        Session["PLANTID"] = PlantId;
                        Session["U_SITE"] = User_Worksite;
                        Session["U_SITECODE"] = User_WRKSTCode;

                        Session["DESG"] = User_Desg;
                        Session["GRADE"] = User_Grade;
                        //if (User_Photo == null || User_Photo == "")
                        //{
                        //    Session["User_Photo"] = "No_Image.jpg";
                        //}
                        //else
                        //{
                        //    //Check for physical file
                        //    bool File = FlieExistence();
                        //    if (File == true)
                        //    {
                        //        Session["User_Photo"] = User_Photo;
                        //    }
                        //    else
                        //    {
                        //        Session["User_Photo"] = "No_Image.jpg";
                        //    }
                        //}
                        //dbcl.WriteToFile("User " + User_FullName + "[" + Workman + "]" + " Logined Successfully");

                        // Get the user's IP address
                        //string ipAddress = HttpContext.Current.Request.UserHostAddress;

                        // Notify admin or security team
                        //LoginNotifier.NotifyLogin(User_FullName, ipAddress);

                        UpdateUserLastLogin(userId);                       // update last login
                        //int activeSessionId = CreateActiveSession(userId, Workman);
                        //Session["ActiveSessionId"] = activeSessionId;

                        // After validating user credentials...
                        var result = SessionHelper.CreateActiveSession(userId, Workman);
                        if (result != null)
                        {
                            Session["ActiveSessionId"] = result.Item1;          // DB identity
                            Session["LoginToken"] = result.Item2.ToString();    // GUID token
                        }

                        Response.Redirect("home.aspx", false);
                    }
                    else
                    {
                        ClientScript.RegisterStartupScript(typeof(Page), "AlertMessage", "<script>alert('User ID is InActive');</script>");
                        txt_loginid.Text = "";
                    }

                }
                else
                {
                    ClientScript.RegisterStartupScript(typeof(Page), "AlertMessage", "<script>alert('Unknown user / password.');</script>");
                    txt_loginid.Text = "";
                }
            }
            else
            {
                ClientScript.RegisterStartupScript(typeof(Page), "AlertMessage", "<script>alert('Unknown user / password.');</script>");
                txt_loginid.Text = "";
            }
        }

        private bool FlieExistence()
        {
            if (File.Exists(Path.Combine(rootFolder, User_Photo)))
            {
                return true;
            }
            else if (File.Exists(Path.Combine(localFolder, User_Photo)))
            {
                return true;
            }
            else
            {
                string title = "Notifications :";
                string body = "NO Physical File Found...!!";
                ClientScript.RegisterStartupScript(this.GetType(), "Popup", "ShowPopup('" + title + "', '" + body + "');", true);
                return false;
            }
        }

        private string GetClientIP()
        {
            string ip = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (!string.IsNullOrWhiteSpace(ip))
            {
                int comma = ip.IndexOf(',');
                if (comma > 0) ip = ip.Substring(0, comma);
                return ip.Trim();
            }

            ip = Request.ServerVariables["REMOTE_ADDR"];
            return string.IsNullOrWhiteSpace(ip) ? "Unknown" : ip.Trim();
        }

        // Call this after successful credential check (right before redirect)
        private int CreateActiveSession(int userId, string Workman)
        {
            string sessionId = Session.SessionID;
            string ip = GetClientIP();
            string browser = Request.Browser.Browser + " " + Request.Browser.Version;
            string host = Request.UserHostName;

            string sql = @"
            INSERT INTO ActiveUserSession (UserId, EmployeeCode, SessionId, LoginTime, ClientIP, HostName, BrowserInfo, IsActive)
            VALUES (@UserId, @EmployeeCode, @SessionId, GETDATE(), @ClientIP, @HostName, @BrowserInfo, 1);
            SELECT SCOPE_IDENTITY();";

            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@EmployeeCode", (object)Workman ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SessionId", sessionId);
                cmd.Parameters.AddWithValue("@ClientIP", ip);
                cmd.Parameters.AddWithValue("@HostName", host ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@BrowserInfo", browser ?? (object)DBNull.Value);

                con.Open();
                object o = cmd.ExecuteScalar();
                int activeSessionId = Convert.ToInt32(o);
                return activeSessionId;
            }
        }

        private void UpdateUserLastLogin(int userId)
        {
            string sql = "UPDATE MST_UserMaster SET LastLoginDate = GETDATE() WHERE Id = @Id";
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@Id", userId);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        #region First Time Login & OTP Flow

        protected void lnkForgotPassword_Click(object sender, EventArgs e)
        {
            RunSafe("ForgotPassword", () =>
            {
                ClearLoginMessage();
                SwitchPanel(pnlRequestOTP);
            });
        }

        protected void lnkBackToLogin_Click(object sender, EventArgs e)
        {
            RunSafe("BackToLogin", () =>
            {
                Session.Remove("Reset_OTP");
                Session.Remove("Reset_EmpCode");
                Session.Remove("Reset_OTP_Expiry");
                ClearLoginMessage();
                SwitchPanel(pnlLogin);
            });
        }

        protected void btnSendOTP_Click(object sender, EventArgs e)
        {
            RunSafe("SendOTP", () =>
            {
                ClearLoginMessage();
                string empCode = txtResetEmpCode.Text.Trim();
                if (string.IsNullOrEmpty(empCode))
                {
                    ShowAlert("Please enter your employee code.");
                    return;
                }

                string query = "SELECT Email, Mobile, EmployeeName FROM MST_UserMaster WHERE EmployeeCode = @EmpCode AND EmployeeStatus = 'Active'";
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@EmpCode", empCode);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dtUser = new DataTable();
                    da.Fill(dtUser);

                    if (dtUser.Rows.Count == 0)
                    {
                        LogLoginWarn("OTP request for unknown/inactive employee.", empCode);
                        ShowAlert("Invalid employee code or inactive user.");
                        return;
                    }

                    string email = dtUser.Rows[0]["Email"].ToString();
                    string mobile = dtUser.Rows[0]["Mobile"].ToString();
                    string name = dtUser.Rows[0]["EmployeeName"].ToString();

                    if (string.IsNullOrEmpty(mobile))
                    {
                        ShowAlert("No registered mobile number found for this user. Please contact HR/IT.");
                        return;
                    }

                    string otp = new Random().Next(100000, 999999).ToString();
                    Session["Reset_OTP"] = otp;
                    Session["Reset_EmpCode"] = empCode;
                    Session["Reset_OTP_Expiry"] = DateTime.Now.AddMinutes(10);

                    SendOtpSms(mobile, otp);
                    SendEmailOTP(email, name, otp);

                    LogLoginInfo("OTP sent for password recovery.", empCode);
                    lblMaskedContact.Text = "OTP sent successfully to mobile ending in " + MaskMobile(mobile);
                    SwitchPanel(pnlVerifyOTP);
                }
            });
        }

        protected void lnkNeedToUpdateContact_Click(object sender, EventArgs e)
        {
            RunSafe("NeedToUpdateContact", () =>
            {
                txtVerifyEmpCode.Text = txtResetEmpCode.Text;
                SwitchPanel(pnlSelfVerify);
            });
        }

        protected void txtVerifyEmpCode_TextChanged(object sender, EventArgs e)
        {
            RunSafe("VerifyEmpCodeLookup", () =>
            {
            string empCode = txtVerifyEmpCode.Text.Trim();
            if (string.IsNullOrEmpty(empCode))
            {
                lblFetchedEmpName.Text = "";
                return;
            }

            string query = "SELECT EmployeeName, EmployeeStatus FROM MST_UserMaster WHERE EmployeeCode = @EmpCode";
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@EmpCode", empCode);
                con.Open();
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        if (rd["EmployeeStatus"].ToString().Equals("Active", StringComparison.OrdinalIgnoreCase))
                        {
                            lblFetchedEmpName.Text = "✔ " + rd["EmployeeName"].ToString();
                            lblFetchedEmpName.CssClass = "small mt-1 d-block text-success font-weight-bold";
                            txtVerifyManagerCode.Focus(); // Auto-move to next field
                        }
                        else
                        {
                            lblFetchedEmpName.Text = "✖ Account is Inactive. Contact HR.";
                            lblFetchedEmpName.CssClass = "small mt-1 d-block text-danger font-weight-bold";
                        }
                    }
                    else
                    {
                        lblFetchedEmpName.Text = "✖ Employee Code not found.";
                        lblFetchedEmpName.CssClass = "small mt-1 d-block text-danger font-weight-bold";
                    }
                }
            }
            });
        }

        protected void txtVerifyManagerCode_TextChanged(object sender, EventArgs e)
        {
            RunSafe("VerifyManagerCodeLookup", () =>
            {
            string mgrCode = txtVerifyManagerCode.Text.Trim();
            if (string.IsNullOrEmpty(mgrCode))
            {
                lblFetchedManagerName.Text = "";
                return;
            }

            // Since Managers are also employees in MST_UserMaster, we check the same table
            string query = "SELECT EmployeeName FROM MST_UserMaster WHERE EmployeeCode = @MgrCode AND EmployeeStatus = 'Active'";
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@MgrCode", mgrCode);
                con.Open();
                object result = cmd.ExecuteScalar();

                if (result != null)
                {
                    lblFetchedManagerName.Text = "✔ " + result.ToString();
                    lblFetchedManagerName.CssClass = "small mt-1 d-block text-success font-weight-bold";
                    txtVerifyDOB.Focus(); // Auto-move to next field
                }
                else
                {
                    lblFetchedManagerName.Text = "✖ Manager Code not found or inactive.";
                    lblFetchedManagerName.CssClass = "small mt-1 d-block text-danger font-weight-bold";
                }
            }
            });
        }

        protected void btnVerifyAndUpdate_Click(object sender, EventArgs e)
        {
            RunSafe("VerifyAndUpdateContact", () =>
            {
            // 1. DPDP Compliance Enforcement
            if (!chkDPDPConsent.Checked)
            {
                ShowAlert("DPDP Compliance: You must provide explicit consent to store your personal contact data before proceeding.");
                return;
            }

            string empCode = txtVerifyEmpCode.Text.Trim();
            string managerCodeInput = txtVerifyManagerCode.Text.Trim();
            string dojInput = txtVerifyDOB.Text.Trim(); // Date of Joining
            string newMobile = txtNewMobile.Text.Trim();
            string newEmail = txtNewEmail.Text.Trim();

            if (string.IsNullOrEmpty(empCode) || string.IsNullOrEmpty(managerCodeInput) || string.IsNullOrEmpty(newMobile))
                return;

            // 2. Fetch the user record based ONLY on EmployeeCode first (Smart Validation)
            string fetchQuery = @"
                SELECT EmployeeName, ReportingManagerCode, DOJ, EmployeeStatus 
                FROM MST_UserMaster 
                WHERE EmployeeCode = @EmpCode";

            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(fetchQuery, con))
                {
                    cmd.Parameters.AddWithValue("@EmpCode", empCode);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    // A. Check if Employee exists
                    if (dt.Rows.Count == 0)
                    {
                        ShowAlert("Employee Code not found. If you recently joined, please allow 24-48 hours for HR data to sync.");
                        return;
                    }

                    DataRow row = dt.Rows[0];
                    string status = row["EmployeeStatus"].ToString();
                    string dbManagerCode = row["ReportingManagerCode"].ToString();

                    // Format DB DOJ to match HTML5 Date Picker (YYYY-MM-DD)
                    string dbDoj = row["DOJ"] != DBNull.Value ? Convert.ToDateTime(row["DOJ"]).ToString("yyyy-MM-dd") : "";

                    // B. Check if Employee is Active
                    if (!status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                    {
                        ShowAlert("This account is currently marked as Inactive. Please contact IT Helpdesk.");
                        return;
                    }

                    // C. Check Manager Code match
                    if (string.IsNullOrEmpty(dbManagerCode) || !dbManagerCode.Equals(managerCodeInput, StringComparison.OrdinalIgnoreCase))
                    {
                        ShowAlert("Manager Code mismatch. If your manager recently changed, your HR record may be outdated. Please contact IT Support.");
                        return;
                    }

                    // D. Check Date of Joining match
                    if (dbDoj != dojInput)
                    {
                        ShowAlert("Identity verification failed. Date of Joining mismatch.");
                        return;
                    }

                    // --- IF ALL CHECKS PASS: Log DPDP Consent & Proceed with Update ---
                    string employeeName = row["EmployeeName"].ToString();

                    // Log the DPDP consent explicitly for audit purposes
                    LogDebug($"DPDP Consent: User {empCode} ({employeeName}) explicitly agreed to contact data storage from IP {GetClientIP()} at {DateTime.Now}");

                    string updateQuery = "UPDATE MST_UserMaster SET Mobile = @Mobile, Email = @Email WHERE EmployeeCode = @EmpCode";
                    using (SqlCommand cmdUpdate = new SqlCommand(updateQuery, con))
                    {
                        // Note: Ensure column name here matches DB exactly. Often it's Mobile, but your old code had 'Mobile'. Adjust if needed.
                        cmdUpdate.Parameters.AddWithValue("@Mobile", newMobile);
                        cmdUpdate.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(newEmail) ? (object)DBNull.Value : newEmail);
                        cmdUpdate.Parameters.AddWithValue("@EmpCode", empCode);
                        con.Open();
                        cmdUpdate.ExecuteNonQuery();
                        con.Close();
                    }

                    // Generate OTP and send to the NEW number
                    string otp = new Random().Next(100000, 999999).ToString();
                    Session["Reset_OTP"] = otp;
                    Session["Reset_EmpCode"] = empCode;
                    Session["Reset_OTP_Expiry"] = DateTime.Now.AddMinutes(10);

                    // Trigger SMS via SMSCountry
                    SendOtpSms(newMobile, otp);

                    // Fire the DPDP Legal notice if an email was provided
                    if (!string.IsNullOrEmpty(newEmail))
                    {
                        SendDPDPConsentNotice(newEmail, employeeName, empCode, newMobile);
                    }

                    // Reset the UI fields
                    chkDPDPConsent.Checked = false;
                    lblFetchedEmpName.Text = "";
                    lblFetchedManagerName.Text = "";

                    // Switch panels
                    lblMaskedContact.Text = "Identity confirmed. OTP sent to your new mobile ending in " + MaskMobile(newMobile);
                    SwitchPanel(pnlVerifyOTP);
                }
            }
            });
        }

        private void SendDPDPConsentNotice(string toEmail, string employeeName, string employeeCode, string mobile)
        {
            if (string.IsNullOrEmpty(toEmail)) return;

            try
            {
                string subject = "Notice of Consent - Data Protection Compliance";

                // Safely extract last 4 digits of mobile
                string maskedMobile = mobile.Length >= 4 ? mobile.Substring(mobile.Length - 4) : mobile;

                string body = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd; max-width: 600px;'>
                <h2 style='color: #1ABB9C;'>Anmol One Technology Portal</h2>
                <p>Dear {employeeName} ({employeeCode}),</p>
                <p>This is an automated confirmation that on <b>{DateTime.Now:dd-MMM-yyyy HH:mm:ss} (IST)</b>, you provided explicit consent for Anmol Industries Limited to collect, store, and process your personal contact details.</p>
                
                <div style='background: #f8f9fa; padding: 15px; border-left: 4px solid #1ABB9C; margin: 15px 0;'>
                    <b>Data Registered:</b><br/>
                    Mobile: ******{maskedMobile}<br/>
                    Email: {toEmail}<br/><br/>
                    <b>Purpose of Processing:</b><br/>
                    System authentication (OTP generation) and critical official communications.
                </div>

                <p style='color: #777; font-size: 12px;'><i>If you wish to review, update, or withdraw this consent at any time, please contact the IT Helpdesk or your HR representative.</i></p>
                <br/><p>Regards,<br/>IT & Cybersecurity Department<br/>Anmol Industries Limited</p>
            </div>";

                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
                {
                    string queueQuery = @"
                INSERT INTO NotificationQueue 
                (FormSubmissionId, EscalationLevel, RecipientEmail, RecipientName, Subject, Body, NotificationType, Status, SourceModule) 
                VALUES 
                (0, 0, @Email, @Name, @Subject, @Body, 'Email', 'Pending', 'DPDP_CONSENT')";

                    using (SqlCommand cmd = new SqlCommand(queueQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@Email", toEmail);
                        cmd.Parameters.AddWithValue("@Name", employeeName ?? "User");
                        cmd.Parameters.AddWithValue("@Subject", subject);
                        cmd.Parameters.AddWithValue("@Body", body);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                LogDebug($"DPDP Notice Email queued successfully for {employeeCode}");
            }
            catch (Exception ex)
            {
                LogDebug("Failed to queue DPDP notice: " + ex.Message);
            }
        }

        //private void SendDPDPConsentNotice(string toEmail, string employeeName, string employeeCode, string mobile)
        //{
        //    if (string.IsNullOrEmpty(toEmail)) return;

        //    string subject = "Notice of Consent - Data Protection Compliance";
        //    string body = $@"
        //        Dear {employeeName} ({employeeCode}),<br/><br/>
        //        This is an automated confirmation that on <b>{DateTime.Now:dd-MMM-yyyy HH:mm:ss} (IST)</b>, you provided explicit consent for Anmol Industries Limited to collect, store, and process your personal contact details.<br/><br/>
        //        <b>Data Registered:</b><br/>
        //        Mobile: ******{mobile.Substring(mobile.Length - 4)}<br/>
        //        Email: {toEmail}<br/><br/>
        //        <b>Purpose of Processing:</b><br/>
        //        System authentication (OTP generation) and critical official communications.<br/><br/>
        //        <i>If you wish to review, update, or withdraw this consent at any time, please contact the IT Helpdesk or your HR representative.</i><br/><br/>
        //        Regards,<br/>
        //        IT & Compliance Department<br/>
        //        Anmol Industries Limited";

        //    // Fire-and-forget asynchronous thread to prevent UI blocking
        //    System.Threading.Tasks.Task.Run(() =>
        //    {
        //        try
        //        {
        //            MailMessage mail = new MailMessage();
        //            mail.From = new MailAddress("sapsystem@anmolindustries.com", "Anmol IT Portal");
        //            mail.To.Add(toEmail);
        //            mail.Subject = subject;
        //            mail.Body = body;
        //            mail.IsBodyHtml = true;

        //            using (SmtpClient smtp = new SmtpClient("smtp.office365.com", 587))
        //            {
        //                smtp.Credentials = new System.Net.NetworkCredential("sapsystem@anmolindustries.com", "Basis@2024");
        //                smtp.EnableSsl = true;
        //                smtp.Send(mail);
        //            }

        //            LogDebug($"DPDP Notice Email sent successfully to {toEmail} for {employeeCode}");
        //        }
        //        catch (Exception ex)
        //        {
        //            LogDebug($"Failed to send DPDP notice to {toEmail}: {ex.Message}");
        //        }
        //    });
        //}

        protected void btnVerifyAndUpdate_Click_OLD(object sender, EventArgs e)
        {
            string empCode = txtVerifyEmpCode.Text.Trim();
            string managerCode = txtVerifyManagerCode.Text.Trim();
            string dobStr = txtVerifyDOB.Text.Trim(); // Change this if using PAN/DOJ
            string newMobile = txtNewMobile.Text.Trim();
            string newEmail = txtNewEmail.Text.Trim();

            if (string.IsNullOrEmpty(empCode) || string.IsNullOrEmpty(managerCode) || string.IsNullOrEmpty(newMobile)) return;

            // 1. Verify Identity against MST_UserMaster
            // IMPORTANT: Adjust 'DateOfBirth' to match your actual database column name (e.g., PANNo, DateOfJoining)
            string verifyQuery = @"
                SELECT EmployeeName 
                FROM MST_UserMaster 
                WHERE EmployeeCode = @EmpCode 
                  AND ReportingManagerCode = @ManagerCode 
                  AND DOJ = @DOJ 
                  AND EmployeeStatus = 'Active'";

            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand(verifyQuery, con))
                {
                    cmd.Parameters.AddWithValue("@EmpCode", empCode);
                    cmd.Parameters.AddWithValue("@ManagerCode", managerCode);
                    cmd.Parameters.AddWithValue("@DOJ", dobStr);

                    con.Open();
                    object result = cmd.ExecuteScalar();

                    if (result != null) // Identity Confirmed!
                    {
                        string employeeName = result.ToString();

                        // 2. Update the Contact Info in the Database
                        string updateQuery = @"UPDATE MST_UserMaster 
                           SET Mobile = @Mobile, 
                               Email = @Email, 
                               DPDPConsentGiven = 1, 
                               DPDPConsentDate = GETDATE() 
                           WHERE EmployeeCode = @EmpCode";

                        using (SqlCommand cmdUpdate = new SqlCommand(updateQuery, con))
                        {
                            cmdUpdate.Parameters.AddWithValue("@Mobile", newMobile);
                            cmdUpdate.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(newEmail) ? (object)DBNull.Value : newEmail);
                            cmdUpdate.Parameters.AddWithValue("@EmpCode", empCode);
                            con.Open();
                            cmdUpdate.ExecuteNonQuery();
                            con.Close();
                        }

                        // 3. Generate OTP and send to the NEW number
                        string otp = new Random().Next(100000, 999999).ToString();

                        Session["Reset_OTP"] = otp;
                        Session["Reset_EmpCode"] = empCode;
                        Session["Reset_OTP_Expiry"] = DateTime.Now.AddMinutes(10);

                        // Trigger SMS
                        SendOtpSms(newMobile, otp);

                        // NEW: Fire the legal notice if an email was provided
                        if (!string.IsNullOrEmpty(newEmail))
                        {
                            SendDPDPConsentNotice(newEmail, employeeName, empCode, newMobile);
                        }

                        // Update UI
                        lblMaskedContact.Text = $"Details Updated! OTP sent to Mobile ending in {MaskMobile(newMobile)}";
                        SwitchPanel(pnlVerifyOTP);
                    }
                    else
                    {
                        // Security failed
                        ShowAlert("Verification failed. The details provided do not match our HR records.");
                    }
                }
            }
        }

        // --- Core SMS Gateway Integration ---
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

                        // Optional: Log this responseString to a DB table to track delivery statuses
                        LogDebug($"SMS Sent Successfully. Response: {responseString}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogLoginError("SMS API error while sending OTP.", ex);
            }
        }

        protected void btnVerifyOTP_Click(object sender, EventArgs e)
        {
            RunSafe("VerifyOTP", () =>
            {
                string enteredOTP = txtOTP.Text.Trim();

                if (Session["Reset_OTP"] == null || Session["Reset_OTP_Expiry"] == null)
                {
                    ShowAlert("Session expired. Please start over.");
                    SwitchPanel(pnlRequestOTP);
                    return;
                }

                DateTime expiry = (DateTime)Session["Reset_OTP_Expiry"];
                if (DateTime.Now > expiry)
                {
                    ShowAlert("OTP has expired. Please request a new one.");
                    SwitchPanel(pnlRequestOTP);
                    return;
                }

                if (enteredOTP == Session["Reset_OTP"].ToString())
                {
                    LogLoginInfo("OTP verified successfully.", Session["Reset_EmpCode"] as string);
                    SwitchPanel(pnlSetPassword);
                }
                else
                {
                    LogLoginWarn("Invalid OTP entered.", Session["Reset_EmpCode"] as string);
                    ShowAlert("Invalid OTP entered. Try again.");
                }
            });
        }

        protected void btnSavePassword_Click(object sender, EventArgs e)
        {
            RunSafe("SavePassword", () =>
            {
                string newPassword = txtNewPassword.Text.Trim();
                string empCode = Session["Reset_EmpCode"] as string;

                if (string.IsNullOrEmpty(empCode) || string.IsNullOrEmpty(newPassword))
                {
                    ShowAlert("Session expired. Please restart the process.");
                    SwitchPanel(pnlLogin);
                    return;
                }

                var hashData = PasswordHasher.CreateHash(newPassword, PasswordHasher.DefaultIterations);
                string newHash = hashData.Item1;
                string newSalt = hashData.Item2;
                int newIterations = hashData.Item3;

                string updateSql = @"UPDATE MST_UserMaster 
                                     SET PasswordHash = @PasswordHash, 
                                         PasswordSalt = @PasswordSalt, 
                                         PasswordIterations = @Iterations, 
                                         Password = NULL 
                                     WHERE EmployeeCode = @EmpCode";

                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
                using (SqlCommand cmd = new SqlCommand(updateSql, con))
                {
                    cmd.Parameters.AddWithValue("@PasswordHash", newHash);
                    cmd.Parameters.AddWithValue("@PasswordSalt", newSalt);
                    cmd.Parameters.AddWithValue("@Iterations", newIterations);
                    cmd.Parameters.AddWithValue("@EmpCode", empCode);

                    con.Open();
                    if (cmd.ExecuteNonQuery() <= 0)
                        throw new InvalidOperationException("Password update affected zero rows for employee " + empCode);
                }

                Session.Remove("Reset_OTP");
                Session.Remove("Reset_EmpCode");
                Session.Remove("Reset_OTP_Expiry");

                LogLoginInfo("Password reset completed. Auto sign-in starting.", empCode);
                txt_loginid.Text = empCode;
                txt_password.Text = newPassword;
                ProcessSignIn();
            });
        }

        // --- Helper Methods ---

        private void SwitchPanel(System.Web.UI.WebControls.Panel targetPanel)
        {
            pnlLogin.Visible = false;
            pnlRequestOTP.Visible = false;
            pnlSelfVerify.Visible = false; // <-- Add this
            pnlVerifyOTP.Visible = false;
            pnlSetPassword.Visible = false;

            targetPanel.Visible = true;
        }


        private bool SendEmailOTP(string toEmail, string name, string otp)
        {
            if (string.IsNullOrEmpty(toEmail)) return false;

            try
            {
                string subject = "Your Account Setup / Reset OTP";
                string body = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd; max-width: 600px;'>
                <h2 style='color: #1ABB9C;'>Anmol IT Portal</h2>
                <p>Dear {name},</p>
                <p>Your One Time Password (OTP) for account setup/recovery is:</p>
                <h1 style='letter-spacing: 5px; color: #333; background: #f4f4f4; padding: 10px; text-align: center; border-radius: 5px;'>{otp}</h1>
                <p style='color: #777; font-size: 12px;'>This OTP is valid for 10 minutes. Please do not share this with anyone.</p>
                <br/><p>Regards,<br/>IT Department<br/>Anmol Industries Limited</p>
            </div>";

                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString))
                {
                    string queueQuery = @"
                INSERT INTO NotificationQueue 
                (FormSubmissionId, EscalationLevel, RecipientEmail, RecipientName, Subject, Body, NotificationType, Status, SourceModule) 
                VALUES 
                (0, 0, @Email, @Name, @Subject, @Body, 'Email', 'Pending', 'AUTH_OTP')";

                    using (SqlCommand cmd = new SqlCommand(queueQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@Email", toEmail);
                        cmd.Parameters.AddWithValue("@Name", name ?? "User");
                        cmd.Parameters.AddWithValue("@Subject", subject);
                        cmd.Parameters.AddWithValue("@Body", body);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                LogDebug($"OTP Email successfully queued for {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                LogDebug("Email Queue Error (OTP): " + ex.ToString());
                return false;
            }
        }

        //private bool SendEmailOTP(string toEmail, string name, string otp)
        //{
        //    try
        //    {
        //        // Configure this with your actual SMTP details in Web.config or hardcode temporarily
        //        MailMessage mail = new MailMessage();
        //        mail.From = new MailAddress("sapsystem@anmolindustries.com", "Anmol IT Portal");
        //        mail.To.Add(toEmail);
        //        mail.Subject = "Your Account Setup / Reset OTP";
        //        mail.Body = $"Dear {name},<br/><br/>Your One Time Password (OTP) for account setup/recovery is: <b>{otp}</b>.<br/><br/>This OTP is valid for 10 minutes. Please do not share this with anyone.<br/><br/>Regards,<br/>IT Department";
        //        mail.IsBodyHtml = true;

        //        using (SmtpClient smtp = new SmtpClient("smtp.office365.com", 587))
        //        {
        //            smtp.Credentials = new System.Net.NetworkCredential("sapsystem@anmolindustries.com", "Basis@2024");
        //            smtp.EnableSsl = true;
        //            smtp.Send(mail); // Executing the email send
        //        }

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        LogDebug("Email Send Error: " + ex.ToString());
        //        return false;
        //    }
        //}

        private string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains("@")) return email;
            string[] parts = email.Split('@');
            string username = parts[0];
            if (username.Length > 2)
            {
                username = username.Substring(0, 2) + new String('*', username.Length - 2);
            }
            return username + "@" + parts[1];
        }

        private string MaskMobile(string mobile)
        {
            if (string.IsNullOrEmpty(mobile) || mobile.Length < 4) return mobile;
            return new string('*', mobile.Length - 4) + mobile.Substring(mobile.Length - 4);
        }

        

        #endregion

    }
}