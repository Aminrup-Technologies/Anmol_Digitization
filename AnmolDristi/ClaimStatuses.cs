using System;

namespace AnmolDristi
{
    /// <summary>
    /// Canonical claim / approval status values used across sales claims workflow.
    /// </summary>
    public static class ClaimStatuses
    {
        public const string Draft = "Draft";
        public const string Submitted = "Submitted";
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Returned = "Returned";
        public const string Completed = "Completed";
        public const string ReadyForPayment = "ReadyForPayment";
        public const string Processed = "Processed";
        public const string Unknown = "Unknown";

        public static bool Equals(string left, string right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }
}
