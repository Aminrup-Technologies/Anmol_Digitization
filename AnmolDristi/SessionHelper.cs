using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web;

namespace AnmolDristi
{
    public class SessionHelper
    {
        private static string ConnStr
        => ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

        public static Tuple<int, Guid> CreateActiveSession_OLD(int userId, string employeeCode)
        {
            string connStr = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;

            string sql = @"
            INSERT INTO ActiveUserSession 
                (UserId, EmployeeCode, SessionId, LoginTime, LastHeartbeat, ClientIP, HostName, BrowserInfo, IsActive, CreatedAt, LoginToken)
            OUTPUT inserted.ActiveSessionId, inserted.LoginToken
            VALUES 
                (@UserId, @EmployeeCode, @SessionId, GETDATE(), GETDATE(), @ClientIP, @HostName, @BrowserInfo, 1, GETDATE(), NEWID());";

            using (SqlConnection con = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@EmployeeCode", (object)employeeCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SessionId", HttpContext.Current.Session.SessionID);
                cmd.Parameters.AddWithValue("@ClientIP", GetClientIP());
                cmd.Parameters.AddWithValue("@HostName", HttpContext.Current.Request.UserHostName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@BrowserInfo", HttpContext.Current.Request.Browser.Browser + " " + HttpContext.Current.Request.Browser.Version);

                con.Open();
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        int activeSessionId = rdr.GetInt32(0);
                        Guid loginToken = rdr.GetGuid(1);
                        return Tuple.Create(activeSessionId, loginToken);
                    }
                }
            }
            return null;
        }


        // Create ActiveUserSession row on login
        public static Tuple<int, Guid> CreateActiveSession(int userId, string employeeCode)
        {
            string sql = @"
            INSERT INTO ActiveUserSession 
                (UserId, EmployeeCode, SessionId, LoginTime, LastHeartbeat, ClientIP, HostName, BrowserInfo, IsActive, CreatedAt, LoginToken)
            OUTPUT inserted.ActiveSessionId, inserted.LoginToken
            VALUES 
                (@UserId, @EmployeeCode, @SessionId, GETDATE(), GETDATE(), @ClientIP, @HostName, @BrowserInfo, 1, GETDATE(), NEWID());";

            using (SqlConnection con = new SqlConnection(ConnStr))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@EmployeeCode", (object)employeeCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SessionId", HttpContext.Current.Session.SessionID);
                cmd.Parameters.AddWithValue("@ClientIP", GetClientIP());
                cmd.Parameters.AddWithValue("@HostName", HttpContext.Current.Request.UserHostName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@BrowserInfo", HttpContext.Current.Request.Browser.Browser + " " + HttpContext.Current.Request.Browser.Version);

                con.Open();
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        int activeSessionId = rdr.GetInt32(0);
                        Guid loginToken = rdr.GetGuid(1);
                        return Tuple.Create(activeSessionId, loginToken);
                    }
                }
            }
            return null;
        }

        private static string GetClientIP()
        {
            var req = HttpContext.Current.Request;
            var ip = req.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrEmpty(ip)) ip = req.ServerVariables["REMOTE_ADDR"];
            return ip ?? "0.0.0.0";
        }


        // End ActiveUserSession row on logout (by LoginToken or ActiveSessionId)
        public static bool EndActiveSession_OLD1(Guid loginToken)
        {
            string sql = "UPDATE ActiveUserSession SET IsActive = 0, LogoutTime = GETDATE() WHERE LoginToken = @t";

            using (SqlConnection con = new SqlConnection(ConnStr))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@t", loginToken);
                con.Open();
                int rows = cmd.ExecuteNonQuery();
                return rows > 0;
            }
        }

        public static bool EndActiveSession_OLD2(int activeSessionId)
        {
            string sql = "UPDATE ActiveUserSession SET IsActive = 0, LogoutTime = GETDATE() WHERE ActiveSessionId = @id";

            using (SqlConnection con = new SqlConnection(ConnStr))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@id", activeSessionId);
                con.Open();
                int rows = cmd.ExecuteNonQuery();
                return rows > 0;
            }
        }


        public static bool EndActiveSession_OLD3(int activeSessionId, int userId)
        {
            string sql = @"
        UPDATE ActiveUserSession 
        SET IsActive = 0, LogoutTime = GETDATE() 
        WHERE ActiveSessionId = @id;

        UPDATE MST_UserMaster
        SET LastLogoutDate = GETDATE()
        WHERE Id = @userId;";

            using (SqlConnection con = new SqlConnection(ConnStr))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@id", activeSessionId);
                cmd.Parameters.AddWithValue("@userId", userId);
                con.Open();
                int rows = cmd.ExecuteNonQuery();
                return rows > 0;
            }
        }

        // Public overload: end by LoginToken (GUID)
        public static bool EndActiveSession_OLD(Guid loginToken)
        {
            return EndActiveSessionInternal("LoginToken = @token", new SqlParameter[] {
            new SqlParameter("@token", loginToken)
            });
        }

        public static bool EndActiveSession(Guid loginToken)
        {
            if (loginToken == Guid.Empty) return false;

            try
            {
                string where = "LoginToken = @token";
                var parameters = new SqlParameter[]
                {
            new SqlParameter("@token", loginToken)
                };

                return EndActiveSessionInternal(where, parameters);
            }
            catch (Exception ex)
            {
                // Optional: log the exception using your logging mechanism
                // LogError("EndActiveSession(Guid) failed: " + ex.Message);
                return false;
            }
        }


        // Public overload: end by ActiveSessionId (int)
        public static bool EndActiveSession(int activeSessionId)
        {
            return EndActiveSessionInternal("ActiveSessionId = @id", new SqlParameter[] {
            new SqlParameter("@id", activeSessionId)
        });
        }

        // Public overload: end by ActiveSessionId and update by userId (keeps compatibility)
        public static bool EndActiveSession(int activeSessionId, int userId)
        {
            // Simple wrapper that updates both tables by matching ActiveSessionId and userId for extra safety
            return EndActiveSessionInternal("ActiveSessionId = @id AND UserId = @userId", new SqlParameter[] {
            new SqlParameter("@id", activeSessionId),
            new SqlParameter("@userId", userId)
        });
        }

        // Private common implementation: updates ActiveUserSession and MST_UserMaster.LastLogoutDate in a single operation
        private static bool EndActiveSessionInternal_OLD(string whereClause, SqlParameter[] parameters)
        {
            // We will perform the update inside a transaction so both tables get updated atomically.
            string sql = $@"
            BEGIN TRAN;

            -- Set session inactive and logout time for the matching session rows
            UPDATE ActiveUserSession
            SET IsActive = 0,
                LogoutTime = GETDATE()
            WHERE {whereClause};

            -- Update MST_UserMaster.LastLogoutDate for any users affected by the above update
            UPDATE u
            SET u.LastLogoutDate = GETDATE()
            FROM MST_UserMaster u
            INNER JOIN ActiveUserSession a ON u.Id = a.UserId
            WHERE {whereClause} AND a.IsActive = 0;

            COMMIT TRAN;
        ";

            using (SqlConnection con = new SqlConnection(ConnStr))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                // attach parameters (clone them to avoid parameter reuse issues)
                foreach (var p in parameters)
                {
                    var pClone = new SqlParameter(p.ParameterName, p.SqlDbType) { Value = p.Value };
                    cmd.Parameters.Add(pClone);
                }

                con.Open();
                int affected = cmd.ExecuteNonQuery();
                // ExecuteNonQuery returns sum of rows affected by all statements; treat >0 as success
                return affected > 0;
            }
        }

        private static bool EndActiveSessionInternal(string whereClause, SqlParameter[] parameters)
        {
            // Use a single batch which captures affected user ids via OUTPUT, then updates MST_UserMaster
            string sql = $@"
                DECLARE @AffectedUsers TABLE (UserId INT);

                UPDATE ActiveUserSession
                SET IsActive = 0,
                    LogoutTime = GETDATE()
                OUTPUT inserted.UserId INTO @AffectedUsers
                WHERE {whereClause};

                UPDATE u
                SET u.LastLogoutDate = GETDATE()
                FROM MST_UserMaster u
                INNER JOIN (SELECT DISTINCT UserId FROM @AffectedUsers) a ON u.Id = a.UserId;
        
                -- Return number of affected rows (ActiveUserSession)
                SELECT COUNT(1) FROM @AffectedUsers;
            ";

            using (SqlConnection con = new SqlConnection(ConnStr))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                // Add parameters safely using AddWithValue to avoid SqlDbType clone issues
                if (parameters != null)
                {
                    foreach (var p in parameters)
                    {
                        // note: parameter names should include '@'
                        cmd.Parameters.AddWithValue(p.ParameterName, p.Value ?? DBNull.Value);
                    }
                }

                con.Open();
                using (SqlTransaction tx = con.BeginTransaction())
                {
                    cmd.Transaction = tx;
                    try
                    {
                        // Execute and get the count of affected user ids (from the SELECT at the end)
                        object result = cmd.ExecuteScalar();
                        int affectedCount = 0;
                        if (result != null && int.TryParse(result.ToString(), out affectedCount))
                        {
                            tx.Commit();
                            return affectedCount > 0;
                        }
                        else
                        {
                            tx.Commit();
                            return false;
                        }
                    }
                    catch
                    {
                        try { tx.Rollback(); } catch { /* ignore rollback errors */ }
                        throw; // rethrow so caller can log or handle
                    }
                }
            }
        }

        public static bool UpdateHeartbeat(Guid loginToken)
        {
            string sql = "UPDATE ActiveUserSession SET LastHeartbeat = GETDATE() WHERE LoginToken = @t AND IsActive = 1";
            using (SqlConnection con = new SqlConnection(ConnStr))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@t", loginToken);
                con.Open();
                int rows = cmd.ExecuteNonQuery();
                return rows > 0;
            }
        }

        public static void CleanupStaleSessions(int minutes)
        {
            string sql = @"
            UPDATE ActiveUserSession
            SET IsActive = 0,
                LogoutTime = GETDATE()
            WHERE IsActive = 1
              AND (
                    (LastHeartbeat IS NOT NULL AND LastHeartbeat < DATEADD(MINUTE, -@minutes, GETDATE()))
                    OR (LastHeartbeat IS NULL AND LoginTime < DATEADD(MINUTE, -@minutes, GETDATE()))
                  );";

            using (SqlConnection con = new SqlConnection(ConnStr))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@minutes", minutes);
                con.Open();
                int rows = cmd.ExecuteNonQuery();

                LogCleanup($"Cleanup ran: {rows} session(s) expired (threshold {minutes} min).");
            }
        }


        private static void LogCleanup(string message)
        {
            try
            {
                string basePath = HttpContext.Current.Server.MapPath("~/Logs/CleanupLogs/");
                string dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
                string folderPath = System.IO.Path.Combine(basePath, dateFolder);

                if (!System.IO.Directory.Exists(folderPath))
                    System.IO.Directory.CreateDirectory(folderPath);

                string logFile = System.IO.Path.Combine(folderPath, "cleanup.log");
                string logLine = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";

                System.IO.File.AppendAllText(logFile, logLine + Environment.NewLine);
            }
            catch
            {
                // swallow errors to avoid breaking cleanup
            }
        }

    }
}