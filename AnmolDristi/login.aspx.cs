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

namespace AnmolDristi
{
    public partial class login : System.Web.UI.Page
    {
        DB_Utility_OH4Y dbcl = new DB_Utility_OH4Y();
        DataTable dt = new DataTable();
        static string User_Photo = string.Empty;
        // Default folders
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
            }
        }

        protected void btn_signin_Click(object sender, EventArgs e)
        {
            //Response.Redirect("home.aspx");

            try
            {
                //CredentialChecker1();
                CredentialCheckerSecure_Debug();
            }
            catch (ThreadAbortException)
            {
                // Ignore the ThreadAbortException as it's expected after Response.Redirect
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private void CredentialCheckerSecure_Debug()
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
                string firstName = string.IsNullOrWhiteSpace(fullName) ? ""
                                          : fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0];

                // MINIMAL session items (keep for new code)
                Session.Clear();
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

                Session["RONE_NAME"] = row["ReportingManager"] == DBNull.Value ? "" : row["ReportingManager"].ToString();
                Session["RONE_CODE"] = row["ReportingManagerCode"] == DBNull.Value ? "" : row["ReportingManagerCode"].ToString();
                Session["RTWO_NAME"] = row["ReportingManagersManagerName"] == DBNull.Value ? "" : row["ReportingManagersManagerName"].ToString();
                Session["RTWO_CODE"] = row["ReportingManagersManagerCode"] == DBNull.Value ? "" : row["ReportingManagersManagerCode"].ToString();

                //Session["AUDITOR_NAME"] = row["AuditorName"] == DBNull.Value ? "" : row["AuditorName"].ToString();
                //Session["AUDITOR_CODE"] = row["AuditorCode"] == DBNull.Value ? "" : row["AuditorCode"].ToString();


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


        // small helper to write a debug line to disk (use a path your app can write to)
        private void LogDebug_OLD(string text)
        {
            try
            {
                string path = Server.MapPath("~/Logs/login_debug.log");
                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - " + text + Environment.NewLine;
                System.IO.File.AppendAllText(path, line);
            }
            catch { /* swallow to avoid interfering with auth flow */ }
        }

        private void LogDebug(string text)
        {
            try
            {
                // Ensure Logs folder exists
                string logFolder = Server.MapPath("~/Logs/");
                if (!System.IO.Directory.Exists(logFolder))
                {
                    System.IO.Directory.CreateDirectory(logFolder);
                }

                // Create daily log file: login_YYYY-MM-DD.log
                string fileName = "login_" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
                string path = System.IO.Path.Combine(logFolder, fileName);

                // Create log line with timestamp
                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - " + text + Environment.NewLine;

                // Append to file
                System.IO.File.AppendAllText(path, line);
            }
            catch
            {
                // swallow any logging errors to avoid interrupting the login process
            }
        }


        private void ShowAlert(string message)
        {
            ClientScript.RegisterStartupScript(typeof(Page), "AlertMessage", "<script>alert('" + HttpUtility.JavaScriptStringEncode(message) + "');</script>");
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
            string title = "Notifications :";
            string body = ex.Message;
            ClientScript.RegisterStartupScript(this.GetType(), "Popup", "ShowPopup('" + title + "', '" + body + "');", true);
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

        // helper to get client IP (basic)
        private string GetClientIP()
        {
            string ip = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrEmpty(ip)) ip = Request.ServerVariables["REMOTE_ADDR"];
            return ip ?? "Unknown";
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

    }
}