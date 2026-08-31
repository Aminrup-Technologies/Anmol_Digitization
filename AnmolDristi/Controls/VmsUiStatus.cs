using System;

namespace AnmolDristi.Controls
{
    /// <summary>
    /// Single status-to-CSS map for VMS badges. Does not change workflow data.
    /// </summary>
    public static class VmsUiStatus
    {
        public const string Pending = "badge-pending";
        public const string Approved = "badge-approved";
        public const string CheckedIn = "badge-checked-in";
        public const string CheckedOut = "badge-checked-out";
        public const string Rejected = "badge-rejected";
        public const string NotApproved = "badge-not-approved";

        public static string CssClass(string status)
        {
            string value = (status ?? string.Empty).Trim();
            if (value.Length == 0)
                return Pending;

            if (string.Equals(value, "Approved", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Valid Pass", StringComparison.OrdinalIgnoreCase))
                return Approved;

            if (string.Equals(value, "Pending", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Scheduled", StringComparison.OrdinalIgnoreCase))
                return Pending;

            if (string.Equals(value, "Rejected", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Expired", StringComparison.OrdinalIgnoreCase))
                return Rejected;

            if (string.Equals(value, "Checked-In", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Checked In", StringComparison.OrdinalIgnoreCase))
                return CheckedIn;

            if (string.Equals(value, "Checked-Out", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Checked Out", StringComparison.OrdinalIgnoreCase))
                return CheckedOut;

            if (value.IndexOf("not approved", StringComparison.OrdinalIgnoreCase) >= 0)
                return NotApproved;

            return Pending;
        }

        public static string BadgeClass(string status)
        {
            return "vms-status-badge " + CssClass(status);
        }
    }
}
