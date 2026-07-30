using System;
using System.IO;
using System.Web;
using NLog;

namespace AnmolDristi
{
    /// <summary>
    /// Structured login/OTP audit logging via NLog with a file fallback.
    /// </summary>
    public static class LoginAuditLogger
    {
        private static readonly Logger Logger = LogManager.GetLogger("AnmolDristi.Login");

        public static void Info(string message, HttpRequest request = null, string employeeCode = null)
        {
            Write(LogLevel.Info, message, null, request, employeeCode);
        }

        public static void Warn(string message, HttpRequest request = null, string employeeCode = null)
        {
            Write(LogLevel.Warn, message, null, request, employeeCode);
        }

        public static void Error(string message, Exception ex = null, HttpRequest request = null, string employeeCode = null)
        {
            Write(LogLevel.Error, message, ex, request, employeeCode);
        }

        private static void Write(LogLevel level, string message, Exception ex, HttpRequest request, string employeeCode)
        {
            string context = BuildContext(request, employeeCode);
            string fullMessage = string.IsNullOrEmpty(context) ? message : message + " | " + context;

            if (ex != null)
                Logger.Log(level, ex, fullMessage);
            else
                Logger.Log(level, fullMessage);

            WriteFileFallback(level, fullMessage, ex);
        }

        private static string BuildContext(HttpRequest request, string employeeCode)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(employeeCode))
                parts.Add("Emp=" + employeeCode.Trim());

            if (request != null)
            {
                parts.Add("IP=" + GetClientIp(request));
                if (request.UserAgent != null)
                    parts.Add("UA=" + request.UserAgent);
            }

            return parts.Count == 0 ? string.Empty : string.Join("; ", parts);
        }

        public static string GetClientIp(HttpRequest request)
        {
            if (request == null) return "Unknown";
            string ip = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (!string.IsNullOrWhiteSpace(ip))
            {
                int comma = ip.IndexOf(',');
                if (comma > 0) ip = ip.Substring(0, comma);
                return ip.Trim();
            }
            ip = request.ServerVariables["REMOTE_ADDR"];
            return string.IsNullOrWhiteSpace(ip) ? "Unknown" : ip.Trim();
        }

        private static void WriteFileFallback(LogLevel level, string message, Exception ex)
        {
            try
            {
                string baseDir = HttpContext.Current != null
                    ? HttpContext.Current.Server.MapPath("~/Logs/")
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

                if (!Directory.Exists(baseDir))
                    Directory.CreateDirectory(baseDir);

                string path = Path.Combine(baseDir, "login_" + DateTime.Now.ToString("yyyy-MM-dd") + ".log");
                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    + " [" + level.Name + "] "
                    + message;

                if (ex != null)
                    line += Environment.NewLine + ex;

                File.AppendAllText(path, line + Environment.NewLine);
            }
            catch
            {
                // Never throw from logging.
            }
        }
    }
}
