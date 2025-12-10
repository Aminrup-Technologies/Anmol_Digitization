using System;
using System.Configuration;
using System.Data;
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
        private static string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        }

        // Returns (ActiveSessionId, LoginToken)
        public static Tuple<int, Guid> CreateOrReuseActiveSession(int userId, string employeeCode)
        {
            using (var con = new SqlConnection(GetConnectionString()))
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = @"
                    DECLARE @existingId INT, @existingToken UNIQUEIDENTIFIER;

                    SELECT TOP 1 @existingId = ActiveSessionId, @existingToken = LoginToken
                    FROM dbo.ActiveUserSession
                    WHERE UserId = @UserId AND IsActive = 1
                      AND ClientIP = @ClientIP AND BrowserInfo = @BrowserInfo
                      AND (
                           (LastHeartbeat IS NOT NULL AND LastHeartbeat >= DATEADD(MINUTE, -5, SYSUTCDATETIME()))
                           OR (LastHeartbeat IS NULL AND LoginTime >= DATEADD(MINUTE, -5, SYSUTCDATETIME()))
                          )
                    ORDER BY LoginTime DESC;

                    IF @existingId IS NOT NULL
                    BEGIN
                      UPDATE dbo.ActiveUserSession
                      SET SessionId = @SessionId, LastHeartbeat = SYSUTCDATETIME(), LoginTime = SYSUTCDATETIME(), HostName = @HostName
                      WHERE ActiveSessionId = @existingId;

                      SELECT @existingId AS ActiveSessionId, @existingToken AS LoginToken;
                      RETURN;
                    END

                    INSERT INTO dbo.ActiveUserSession
                    (UserId, EmployeeCode, SessionId, LoginTime, LastHeartbeat, ClientIP, HostName, BrowserInfo, IsActive, LoginToken)
                    VALUES (@UserId, @EmployeeCode, @SessionId, SYSUTCDATETIME(), SYSUTCDATETIME(), @ClientIP, @HostName, @BrowserInfo, 1, NEWID());

                    SELECT CAST(SCOPE_IDENTITY() AS INT) AS ActiveSessionId, (SELECT LoginToken FROM dbo.ActiveUserSession WHERE ActiveSessionId = SCOPE_IDENTITY()) AS LoginToken;
                    ";
                cmd.CommandType = CommandType.Text;

                string sessionId = HttpContext.Current.Session != null ? HttpContext.Current.Session.SessionID : string.Empty;
                var req = HttpContext.Current.Request;
                string clientIp = req.UserHostAddress ?? "";
                string browser = req.Browser != null ? req.Browser.Browser + " " + req.Browser.Version : "";
                string hostName = req.UserHostName ?? "";

                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@EmployeeCode", (object)employeeCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SessionId", (object)sessionId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ClientIP", (object)clientIp ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BrowserInfo", (object)browser ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@HostName", (object)hostName ?? DBNull.Value);

                con.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        int activeId = Convert.ToInt32(rdr["ActiveSessionId"]);
                        Guid token = rdr["LoginToken"] != DBNull.Value ? (Guid)rdr["LoginToken"] : Guid.Empty;
                        return Tuple.Create(activeId, token);
                    }
                }
            }
            return Tuple.Create(0, Guid.Empty);
        }


        public static Tuple<int, Guid> CreateActiveSession(int userId, string employeeCode)
        {
            if (HttpContext.Current == null)
                throw new InvalidOperationException("HttpContext.Current is null. This method must be called in a web request context.");

            // Collect client info from HttpContext.Current.Request
            var request = HttpContext.Current.Request;
            string sessionId = HttpContext.Current.Session != null ? HttpContext.Current.Session.SessionID : String.Empty;
            string clientIp = request.UserHostAddress ?? String.Empty;
            string browserInfo = (request.Browser != null) ? (request.Browser.Browser + " " + request.Browser.Version) : String.Empty;
            string hostName = request.UserHostName ?? String.Empty;
            Guid loginToken = Guid.NewGuid();

            // SQL: parameterized insert with SCOPE_IDENTITY()
            string sql = @"
            INSERT INTO ActiveUserSession 
                (UserId, EmployeeCode, SessionId, LoginTime, ClientIP, HostName, BrowserInfo, IsActive, LoginToken)
            VALUES 
                (@UserId, @EmployeeCode, @SessionId, GETDATE(), @ClientIP, @HostName, @BrowserInfo, 1, @LoginToken);
            SELECT SCOPE_IDENTITY();";

            object inserted;
            using (var con = new SqlConnection(GetConnectionString()))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@EmployeeCode", (object)employeeCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SessionId", (object)sessionId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ClientIP", (object)clientIp ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@HostName", (object)hostName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BrowserInfo", (object)browserInfo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@LoginToken", loginToken.ToString());

                con.Open();
                inserted = cmd.ExecuteScalar();
            }

            // Convert returned identity to int
            int activeSessionId = 0;
            if (inserted != null && inserted != DBNull.Value)
            {
                // SCOPE_IDENTITY() returns decimal in SQL Server; convert safely
                try
                {
                    activeSessionId = Convert.ToInt32(inserted);
                }
                catch
                {
                    // fallback: try decimal -> int
                    try
                    {
                        activeSessionId = Decimal.ToInt32((decimal)inserted);
                    }
                    catch
                    {
                        activeSessionId = 0;
                    }
                }
            }

            // Return the tuple to caller; caller can set session variables if desired.
            return Tuple.Create(activeSessionId, loginToken);
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
            using (var con = new SqlConnection(GetConnectionString()))
            using (var cmd = new SqlCommand("UPDATE dbo.ActiveUserSession SET LastHeartbeat = SYSUTCDATETIME() WHERE LoginToken = @t AND IsActive = 1", con))
            {
                cmd.Parameters.Add("@t", SqlDbType.UniqueIdentifier).Value = loginToken;
                con.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }


        public static bool UpdateHeartbeat_09Dec25(Guid loginToken)
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


        public static bool IsSessionActive(Guid loginToken)
        {
            if (loginToken == Guid.Empty) return false;

            string sql = "SELECT TOP 1 1 FROM ActiveUserSession WHERE LoginToken = @t AND IsActive = 1";
            using (SqlConnection con = new SqlConnection(ConnStr))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@t", loginToken);
                con.Open();
                object o = cmd.ExecuteScalar();
                return o != null;
            }
        }


    }
}