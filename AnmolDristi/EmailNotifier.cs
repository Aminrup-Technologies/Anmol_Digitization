using System;
using System.Collections.Generic;
using System.Web;
using System.Net;
using System.Net.Mail;
using System.Configuration;
using System.IO;

namespace AnmolDristi
{
    public class EmailNotifier
    {
        public static void Notify(string subject, string body, List<string> recipients)
        {
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

                //var smtpSettings = new
                //{
                //    Host = ConfigurationManager.AppSettings["SmtpSettings:Host"] ?? "smtp-mail.outlook.com",
                //    Port = int.Parse(ConfigurationManager.AppSettings["SmtpSettings:Port"] ?? "587"),
                //    EnableSsl = bool.Parse(ConfigurationManager.AppSettings["SmtpSettings:EnableSsl"] ?? "true"),
                //    Username = ConfigurationManager.AppSettings["SmtpSettings:Username"] ?? "abc@xyz.com",
                //    Password = ConfigurationManager.AppSettings["SmtpSettings:Password"] ?? "Welc0meB@ckSymp2024",
                //    FromEmail = ConfigurationManager.AppSettings["SmtpSettings:FromEmail"] ?? "abc@xyz.com"
                //};

                var smtpSettings = new
                {
                    //Host = ConfigurationManager.AppSettings["SmtpSettings:Host"],
                    //Port = int.Parse(ConfigurationManager.AppSettings["SmtpSettings:Port"]),
                    //EnableSsl = bool.Parse(ConfigurationManager.AppSettings["SmtpSettings:EnableSsl"]),
                    //Username = ConfigurationManager.AppSettings["SmtpSettings:Username"],
                    //Password = ConfigurationManager.AppSettings["SmtpSettings:Password"],
                    //FromEmail = ConfigurationManager.AppSettings["SmtpSettings:FromEmail"]

                    Host = "smtp.office365.com",
                    Port = 587,
                    EnableSsl = true,
                    Username = "symphonymis@anmolindustries.com",
                    Password = "Welc0meB@ckSymp2024",
                    FromEmail = "symphonymis@anmolindustries.com"

                };

                using (var client = new SmtpClient(smtpSettings.Host, smtpSettings.Port))
                {
                    client.Credentials = new NetworkCredential(smtpSettings.Username, smtpSettings.Password);
                    client.EnableSsl = smtpSettings.EnableSsl;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(smtpSettings.FromEmail),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };

                    // Add recipients
                    foreach (var recipient in recipients)
                    {
                        mailMessage.To.Add(recipient);
                    }

                    client.Send(mailMessage);
                }
            }
            catch (Exception ex)
            {
                LogEmailError(ex);
            }
        }

        private static void LogEmailError_1(Exception ex)
        {
            try
            {
                // Define the log file path
                string targetFolderPath = HttpContext.Current.Server.MapPath("~/Logs/EmailLogs/");
                if (!Directory.Exists(targetFolderPath))
                {
                    Directory.CreateDirectory(targetFolderPath);
                }

                string logFilePath = Path.Combine(targetFolderPath, $"EmailErrorLog_{DateTime.Now:yyyy-MM-dd}.txt");

                // Append the error details to the log file
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error: {ex.Message}\nStack Trace:\n{ex.StackTrace}\n\n";
                File.AppendAllText(logFilePath, logEntry);
            }
            catch
            {
                // If logging fails, do not throw an exception to avoid masking the original error
            }
        }

        private static void LogEmailError(Exception ex)
        {
            try
            {
                string targetFolderPath = HttpContext.Current.Server.MapPath("~/Logs/EmailLogs/");
                if (!Directory.Exists(targetFolderPath))
                {
                    Directory.CreateDirectory(targetFolderPath);
                }

                string logFilePath = Path.Combine(targetFolderPath, $"EmailErrorLog_{DateTime.Now:yyyy-MM-dd}.txt");

                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error: {ex.Message}\n";
                if (ex.InnerException != null)
                {
                    logEntry += $"Inner Exception: {ex.InnerException.Message}\n";
                }
                logEntry += $"Stack Trace:\n{ex.StackTrace}\n\n";

                File.AppendAllText(logFilePath, logEntry);
            }
            catch
            {
                // Silent catch for logging failures
            }
        }

    }
}