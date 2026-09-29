using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;
using System.Web.UI;

namespace AnmolDristi
{
    public class TimeParser
    {
        public static bool TryParseTimeFromTextBox(System.Web.UI.WebControls.TextBox textBox, out TimeSpan result, out string errorMessage)
        {
            result = TimeSpan.Zero;
            errorMessage = string.Empty;

            string input = textBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(input))
            {
                errorMessage = "Time value is required.";
                return false;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(input, @"^\d{1,3}:[0-5]\d:[0-5]\d$"))
            {
                errorMessage = "Invalid time format. Please use HH:MM:SS (e.g., 36:00:00).";
                return false;
            }

            if (!TimeSpan.TryParseExact(input, @"h\:mm\:ss", CultureInfo.InvariantCulture, out result))
            {
                errorMessage = "Time could not be parsed. Ensure it’s in HH:MM:SS format.";
                return false;
            }

            return true;
        }

        public static bool TryParseTimeFromString(
        Page page,
        string inputValue,
        string fieldLabel,
        out TimeSpan result)
        {
            result = TimeSpan.Zero;

            string input = inputValue?.Trim() ?? "";

            // ✅ Allow empty input — treat as 00:00:00
            if (string.IsNullOrWhiteSpace(input))
            {
                result = TimeSpan.Zero;
                return true;
            }

            //if (string.IsNullOrWhiteSpace(input))
            //{
            //    ShowNotification(page, $"{fieldLabel} is required.");
            //    return false;
            //}

            if (!System.Text.RegularExpressions.Regex.IsMatch(input, @"^\d{1,3}:[0-5]\d:[0-5]\d$"))
            {
                //ShowNotification(page, $"{fieldLabel} must be in HH:MM:SS format (e.g., 36:00:00).");
                return false;
            }

            if (!TimeSpan.TryParseExact(input, @"h\:mm\:ss", CultureInfo.InvariantCulture, out result))
            {
                //ShowNotification(page, $"{fieldLabel} could not be parsed. Ensure format is HH:MM:SS.");
                return false;
            }

            return true;
        }

        private static void ShowNotification(Page page, string message)
        {
            string escapedMessage = message.Replace("'", "\\'");
            string errorScript = "<script type='text/javascript'>\n" +
                                 $"new PNotify({{\n" +
                                 "    title: 'Error',\n" +
                                 $"    text: '{escapedMessage}',\n" +
                                 "    type: 'error',\n" +
                                 "    styling: 'bootstrap3'\n" +
                                 "}});\n" +
                                 "</script>";

            page.ClientScript.RegisterStartupScript(page.GetType(), Guid.NewGuid().ToString(), errorScript, false);
        }
    }
}