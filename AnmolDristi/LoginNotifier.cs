using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AnmolDristi
{
    public class LoginNotifier
    {
        public static void NotifyLogin(string username, string ipAddress)
        {
            try
            {
                // Fetch recipients for login success notifications
                var recipients = EmailRecipientManager.GetRecipients("LoginSuccessNotifications");
                //var recipients = new List<string> { "dgtlautomation@anmolindustries.com", "tech.team@aminruptechnologies.co.in" };
                // Prepare email content
                string subject = "Login Successful Notification";
                string body = $@"
                <p>User <b>{username}</b> logged in successfully.</p>
                <p>IP Address: {ipAddress}</p>
                <p>Timestamp: {DateTime.UtcNow}</p>";

                // Send email
                EmailNotifier.Notify(subject, body, recipients);
            }
            catch (Exception ex)
            {
                // Handle notification failure
                System.IO.File.AppendAllText("EmailErrorLog.txt", $"Failed to send login notification: {ex.Message}\n");
            }
        }
    }
}