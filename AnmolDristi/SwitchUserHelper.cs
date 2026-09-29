using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Web;
using System.Web.SessionState;

namespace AnmolDristi
{
    /// <summary>
    /// Secure impersonation (Switch User) engine.
    /// Reuses the existing session architecture — no parallel auth system.
    /// 
    /// Session keys captured during impersonation:
    ///   Original_CurrentUserId, Original_WORKMAN, Original_USERFNAME,
    ///   Original_USERNAME, Original_USERID, Original_REGION, Original_BRANCH,
    ///   Original_MODULE, Original_PLANTID, Original_U_SITE, Original_U_SITECODE,
    ///   Original_DESG, Original_GRADE, Original_RONE_NAME, Original_RONE_CODE,
    ///   Original_RTWO_NAME, Original_RTWO_CODE, Original_AUDITOR_NAME,
    ///   Original_AUDITOR_CODE, Original_ERPPlantId, Original_ActiveSessionId,
    ///   Original_LoginToken, Original_PH_UserPermSet
    ///   
    /// Impersonation marker:
    ///   IsImpersonating = "true"
    /// </summary>
    public static class SwitchUserHelper
    {
        private const string ImpersonationMarkerKey = "IsImpersonating";
        private const string OriginalPrefix = "Original_";
        private const string AppSettingsKey = "SwitchUserAuthorizedUsers";
        private const string EnabledKey = "SwitchUserEnabled";
        private const string LogSubDir = "SwitchUser";

        /// <summary>
        /// All authentication session keys that get captured/restored during impersonation.
        /// Matches the keys set by login.aspx.cs ProcessSignIn().
        /// </summary>
        private static readonly string[] AuthSessionKeys = new string[]
        {
            "CurrentUserId",
            "WORKMAN",
            "USERFNAME",
            "USERNAME",
            "USERID",
            "REGION",
            "BRANCH",
            "MODULE",
            "PLANTID",
            "U_SITE",
            "U_SITECODE",
            "DESG",
            "GRADE",
            "RONE_NAME",
            "RONE_CODE",
            "RTWO_NAME",
            "RTWO_CODE",
            "AUDITOR_NAME",
            "AUDITOR_CODE",
            "ERPPlantId",
            "ActiveSessionId",
            "LoginToken"
        };

        #region Authorization Checks

        /// <summary>
        /// Returns true if the Switch User feature is enabled via AppSettings.
        /// Fail-closed: returns false if the key is missing or not "true".
        /// </summary>
        public static bool IsFeatureEnabled()
        {
            string val = ConfigurationManager.AppSettings[EnabledKey];
            return string.Equals(val, "true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns true if the employee code is in the Web.config allowlist.
        /// Fail-closed: returns false for null/empty/whitespace.
        /// </summary>
        public static bool IsEmployeeAuthorized(string employeeCode)
        {
            if (string.IsNullOrWhiteSpace(employeeCode))
                return false;

            string raw = ConfigurationManager.AppSettings[AppSettingsKey];
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            // Split by comma, trim each, case-insensitive comparison
            var allowed = raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var code in allowed)
            {
                if (string.Equals(code.Trim(), employeeCode.Trim(), StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the current session is in impersonation mode.
        /// Fail-closed: returns false on any error.
        /// </summary>
        public static bool IsCurrentlyImpersonating(HttpSessionStateBase session)
        {
            try
            {
                return session != null
                    && string.Equals(session[ImpersonationMarkerKey] as string, "true", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if the current session is in impersonation mode (WebForms variant).
        /// </summary>
        public static bool IsCurrentlyImpersonating(HttpSessionState session)
        {
            try
            {
                return session != null
                    && string.Equals(session[ImpersonationMarkerKey] as string, "true", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Employee Lookup

        /// <summary>
        /// Searches MST_UserMaster by EmployeeCode (exact match) or EmployeeName (LIKE match).
        /// Returns null if not found or on error. Only Active employees are returned.
        /// </summary>
        public static DataTable SearchEmployees(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return null;

            string connStr = ConfigurationManager.ConnectionStrings["DbConn"]?.ConnectionString;
            if (string.IsNullOrEmpty(connStr))
                return null;

            try
            {
                using (var con = new SqlConnection(connStr))
                using (var cmd = con.CreateCommand())
                {
                    // Parameterized query — no SQL injection possible
                    cmd.CommandText = @"
                        SELECT TOP 20
                            Id,
                            EmployeeCode,
                            EmployeeName,
                            EmployeeStatus,
                            Module,
                            PlantId,
                            BranchId,
                            DepartmentId,
                            SubDepartmentId,
                            Designation_ID,
                            GradeId,
                            RegionId,
                            ReportingManager,
                            ReportingManagerCode,
                            ReportingManagersManagerName,
                            ReportingManagersManagerCode
                        FROM MST_UserMaster
                        WHERE EmployeeStatus = 'Active'
                          AND (
                              EmployeeCode LIKE @Search
                              OR EmployeeName LIKE @Search
                          )
                        ORDER BY EmployeeCode";

                    cmd.Parameters.AddWithValue("@Search", "%" + searchTerm.Trim() + "%");

                    var dt = new DataTable();
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                    return dt;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Returns a single active employee row by exact EmployeeCode, or null.
        /// </summary>
        public static DataRow GetEmployeeByCode(string employeeCode)
        {
            if (string.IsNullOrWhiteSpace(employeeCode))
                return null;

            string connStr = ConfigurationManager.ConnectionStrings["DbConn"]?.ConnectionString;
            if (string.IsNullOrEmpty(connStr))
                return null;

            try
            {
                using (var con = new SqlConnection(connStr))
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT TOP 1
                            Id, EmployeeCode, EmployeeName, EmployeeStatus,
                            Module, PlantId, BranchId, DepartmentId, SubDepartmentId,
                            Designation_ID, GradeId, RegionId,
                            ReportingManager, ReportingManagerCode,
                            ReportingManagersManagerName, ReportingManagersManagerCode
                        FROM MST_UserMaster
                        WHERE EmployeeCode = @EmpCode AND EmployeeStatus = 'Active'";

                    cmd.Parameters.AddWithValue("@EmpCode", employeeCode.Trim());

                    var dt = new DataTable();
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                    return dt.Rows.Count > 0 ? dt.Rows[0] : null;
                }
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Session Capture (Before Impersonation)

        /// <summary>
        /// Captures all authentication session values into Original_* keys.
        /// Must be called BEFORE overwriting any session values.
        /// </summary>
        public static void CaptureOriginalSession(HttpSessionState session)
        {
            if (session == null) return;

            foreach (string key in AuthSessionKeys)
            {
                string originalKey = OriginalPrefix + key;
                if (session[key] != null)
                {
                    session[originalKey] = session[key];
                }
                else
                {
                    // Store empty string to ensure restore clears impersonated values
                    session[originalKey] = string.Empty;
                }
            }

            // Also capture the permission set
            var permSet = session["PH_UserPermSet"] as HashSet<string>;
            session[OriginalPrefix + "PH_UserPermSet"] = permSet != null
                ? new HashSet<string>(permSet, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Mark as impersonating
            session[ImpersonationMarkerKey] = "true";
        }

        #endregion

        #region Session Hydration (Impersonated User)

        /// <summary>
        /// Overwrites the session with the target employee's identity.
        /// The session should be indistinguishable from a normal login.
        /// </summary>
        public static void HydrateTargetSession(HttpSessionState session, DataRow targetRow)
        {
            if (session == null || targetRow == null) return;

            int userId = Convert.ToInt32(targetRow["Id"]);
            string workman = targetRow["EmployeeCode"].ToString();
            string fullName = targetRow["EmployeeName"].ToString();
            string firstName = string.IsNullOrWhiteSpace(fullName)
                ? ""
                : fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0];

            // Set all session keys — mirrors login.aspx.cs ProcessSignIn()
            session["CurrentUserId"] = userId;
            session["WORKMAN"] = workman;
            session["USERFNAME"] = firstName;
            session["USERNAME"] = fullName;
            session["USERID"] = userId;
            session["REGION"] = targetRow["RegionId"] == DBNull.Value ? "" : targetRow["RegionId"].ToString();
            session["BRANCH"] = targetRow["BranchId"] == DBNull.Value ? "" : targetRow["BranchId"].ToString();
            session["MODULE"] = targetRow["Module"] == DBNull.Value ? "" : targetRow["Module"].ToString();
            session["PLANTID"] = targetRow["PlantId"] == DBNull.Value ? "" : targetRow["PlantId"].ToString();
            session["U_SITE"] = targetRow["DepartmentId"] == DBNull.Value ? "" : targetRow["DepartmentId"].ToString();
            session["U_SITECODE"] = targetRow["SubDepartmentId"] == DBNull.Value ? "" : targetRow["SubDepartmentId"].ToString();
            session["DESG"] = targetRow["Designation_ID"] == DBNull.Value ? "" : targetRow["Designation_ID"].ToString();
            session["GRADE"] = targetRow["GradeId"] == DBNull.Value ? "" : targetRow["GradeId"].ToString();
            session["RONE_NAME"] = targetRow["ReportingManager"] == DBNull.Value ? "" : targetRow["ReportingManager"].ToString();
            session["RONE_CODE"] = targetRow["ReportingManagerCode"] == DBNull.Value ? "" : targetRow["ReportingManagerCode"].ToString();
            session["RTWO_NAME"] = targetRow["ReportingManagersManagerName"] == DBNull.Value ? "" : targetRow["ReportingManagersManagerName"].ToString();
            session["RTWO_CODE"] = targetRow["ReportingManagersManagerCode"] == DBNull.Value ? "" : targetRow["ReportingManagersManagerCode"].ToString();
            session["AUDITOR_NAME"] = "Rohan Saraf";
            session["AUDITOR_CODE"] = "Audit";

            // Resolve ERP Plant ID (same logic as login.aspx.cs)
            session["ERPPlantId"] = ResolveErpPlantId(workman);

            // Rebuild the permission set for the target user
            // Remove cached permissions so they reload for the new user
            session.Remove("PH_UserPermSet");
        }

        /// <summary>
        /// Resolves ERP Plant ID by joining MST_UserMaster -> MST_Plant -> MST_PlantDetails.
        /// Mirrors the logic in login.aspx.cs. Returns 0 on any failure.
        /// </summary>
        private static int ResolveErpPlantId(string employeeCode)
        {
            string connStr = ConfigurationManager.ConnectionStrings["DbConn"]?.ConnectionString;
            if (string.IsNullOrEmpty(connStr))
                return 0;

            try
            {
                using (var con = new SqlConnection(connStr))
                using (var cmd = new SqlCommand(@"
                    SELECT PD.plant_id 
                    FROM MST_UserMaster U
                    INNER JOIN MST_Plant P ON U.PlantId = P.PlantID_ID
                    INNER JOIN MST_PlantDetails PD ON P.SAP_Code = PD.sap_code
                    WHERE U.EmployeeCode = @EmployeeCode", con))
                {
                    cmd.Parameters.AddWithValue("@EmployeeCode", employeeCode);
                    con.Open();
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        return Convert.ToInt32(result);
                }
            }
            catch
            {
                // Non-fatal: failsafe return 0
            }
            return 0;
        }

        #endregion

        #region Return to Original User

        /// <summary>
        /// Restores the original session from Original_* keys and removes impersonation state.
        /// Returns true on success, false if no original session was captured.
        /// </summary>
        public static bool ReturnToOriginalSession(HttpSessionState session)
        {
            if (session == null) return false;

            // Verify there IS an original session to return to
            if (session[OriginalPrefix + "WORKMAN"] == null)
                return false;

            // Restore all authentication session keys
            foreach (string key in AuthSessionKeys)
            {
                string originalKey = OriginalPrefix + key;
                if (session[originalKey] != null)
                {
                    session[key] = session[originalKey];
                    session.Remove(originalKey);
                }
                else
                {
                    session.Remove(key);
                    session.Remove(originalKey);
                }
            }

            // Restore permission set
            var originalPerms = session[OriginalPrefix + "PH_UserPermSet"] as HashSet<string>;
            session["PH_UserPermSet"] = originalPerms != null
                ? new HashSet<string>(originalPerms, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            session.Remove(OriginalPrefix + "PH_UserPermSet");

            // Remove impersonation marker
            session.Remove(ImpersonationMarkerKey);

            return true;
        }

        #endregion

        #region Impersonation Entry Point

        /// <summary>
        /// Performs the full impersonation flow:
        /// 1. Validates authorization (allowlist)
        /// 2. Validates not already impersonating (no nested impersonation)
        /// 3. Validates target employee exists and is Active
        /// 4. Captures original session
        /// 5. Hydrates target session
        /// 6. Logs audit trail
        /// 
        /// Returns true on success, false with errorMessage on failure.
        /// </summary>
        public static bool PerformSwitch(
            HttpSessionState session,
            string targetEmployeeCode,
            out string errorMessage)
        {
            errorMessage = null;

            // 0. Feature toggle check
            if (!IsFeatureEnabled())
            {
                errorMessage = "Switch User feature is currently disabled.";
                return false;
            }

            // 1. Validate caller is logged in
            if (session["WORKMAN"] == null)
            {
                errorMessage = "You must be logged in to use Switch User.";
                return false;
            }

            string callerCode = session["WORKMAN"].ToString();

            // 2. Validate caller is in the allowlist
            if (!IsEmployeeAuthorized(callerCode))
            {
                errorMessage = "You are not authorized to use Switch User. Contact IT.";
                AuditLog(callerCode, targetEmployeeCode, "DENY_NOT_AUTHORIZED", session);
                return false;
            }

            // 3. Prevent nested impersonation
            if (IsCurrentlyImpersonating(session))
            {
                errorMessage = "Nested impersonation is not allowed. Return to your original account first.";
                AuditLog(callerCode, targetEmployeeCode, "DENY_NESTED", session);
                return false;
            }

            // 4. Validate target employee
            if (string.IsNullOrWhiteSpace(targetEmployeeCode))
            {
                errorMessage = "Please specify a target employee code.";
                return false;
            }

            // Prevent switching to yourself
            if (string.Equals(callerCode, targetEmployeeCode.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "You cannot switch to your own account.";
                return false;
            }

            DataRow targetRow = GetEmployeeByCode(targetEmployeeCode.Trim());
            if (targetRow == null)
            {
                errorMessage = "Target employee not found or is inactive.";
                AuditLog(callerCode, targetEmployeeCode, "DENY_TARGET_MISSING", session);
                return false;
            }

            // 5. Capture original session BEFORE hydration
            CaptureOriginalSession(session);

            // 6. Hydrate with target employee's identity
            HydrateTargetSession(session, targetRow);

            // 7. Audit log
            string targetName = targetRow["EmployeeName"]?.ToString() ?? "";
            AuditLog(callerCode, targetEmployeeCode, "SWITCH_START", session, targetName);

            return true;
        }

        /// <summary>
        /// Returns to the original user. Logs audit trail.
        /// Returns true on success.
        /// </summary>
        public static bool PerformReturn(HttpSessionState session, out string errorMessage)
        {
            errorMessage = null;

            if (!IsCurrentlyImpersonating(session))
            {
                errorMessage = "You are not currently impersonating another user.";
                return false;
            }

            string originalCode = session[OriginalPrefix + "WORKMAN"]?.ToString() ?? "Unknown";
            string currentCode = session["WORKMAN"]?.ToString() ?? "Unknown";

            bool restored = ReturnToOriginalSession(session);
            if (restored)
            {
                AuditLog(originalCode, currentCode, "SWITCH_RETURN", session);
            }
            else
            {
                errorMessage = "Failed to restore original session.";
            }

            return restored;
        }

        #endregion

        #region Audit Trail

        /// <summary>
        /// Logs every impersonation event to ~/Logs/SwitchUser/switch_user_YYYY-MM-DD.log.
        /// Uses the existing file-based logging pattern (same as login.aspx.cs).
        /// </summary>
        private static void AuditLog(
            string originalEmployeeCode,
            string targetEmployeeCode,
            string action,
            HttpSessionState session,
            string targetName = null)
        {
            try
            {
                string basePath = HttpContext.Current.Server.MapPath("~/Logs/" + LogSubDir + "/");
                if (!Directory.Exists(basePath))
                    Directory.CreateDirectory(basePath);

                string logFile = Path.Combine(basePath, "switch_user_" + DateTime.Now.ToString("yyyy-MM-dd") + ".log");

                string sessionId = HttpContext.Current?.Session?.SessionID ?? "N/A";
                string clientIp = "N/A";
                try
                {
                    var req = HttpContext.Current?.Request;
                    if (req != null)
                    {
                        string ip = req.ServerVariables["HTTP_X_FORWARDED_FOR"];
                        if (string.IsNullOrWhiteSpace(ip))
                            ip = req.ServerVariables["REMOTE_ADDR"];
                        if (!string.IsNullOrWhiteSpace(ip))
                            clientIp = ip;
                    }
                }
                catch { /* ignore */ }

                string currentUser = session?["USERNAME"]?.ToString() ?? "N/A";

                string line = string.Format(
                    "{0} | Action={1} | OriginalEmp={2} | TargetEmp={3} | TargetName={4} | User={5} | SessionID={6} | IP={7}",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    action,
                    originalEmployeeCode ?? "",
                    targetEmployeeCode ?? "",
                    targetName ?? "",
                    currentUser,
                    sessionId,
                    clientIp);

                File.AppendAllText(logFile, line + Environment.NewLine);
            }
            catch
            {
                // Never throw from audit logging
            }
        }

        #endregion
    }
}
