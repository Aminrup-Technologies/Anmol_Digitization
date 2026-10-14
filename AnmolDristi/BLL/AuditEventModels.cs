using System;

namespace AnmolDristi.BLL
{
    /// <summary>
    /// Known VMS audit event types. Stored as varchar so new types need no schema change.
    /// </summary>
    public static class AuditEventType
    {
        public const string Registered = "Registered";
        public const string OTPVerified = "OTPVerified";
        public const string EmployeeConfirmed = "EmployeeConfirmed";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string AfterHoursApproved = "AfterHoursApproved";
        public const string CheckedIn = "CheckedIn";
        public const string CheckedOut = "CheckedOut";
        public const string AutoClosed = "AutoClosed";
        public const string GateOverride = "GateOverride";
        public const string NotificationSent = "NotificationSent";

        public static string ToDisplayLabel(string eventType)
        {
            if (string.IsNullOrEmpty(eventType))
                return eventType;

            switch (eventType)
            {
                case OTPVerified:
                    return "OTP Verified";
                case EmployeeConfirmed:
                    return "Employee Confirmed";
                case AfterHoursApproved:
                    return "After-Hours Approved";
                case CheckedIn:
                    return "Checked In";
                case CheckedOut:
                    return "Checked Out";
                case AutoClosed:
                    return "Auto-Closed";
                case GateOverride:
                    return "Gate Override";
                case NotificationSent:
                    return "Notification Sent";
                default:
                    return eventType;
            }
        }
    }

    public static class AuditActorRole
    {
        public const string Host = "Host";
        public const string Admin = "Admin";
        public const string Receptionist = "Receptionist";
        public const string Gate = "Gate";
        public const string Visitor = "Visitor";
        public const string System = "System";
    }

    /// <summary>
    /// One timeline row from VMS_AuditEvent / vw_VMS_VisitAuditTimeline.
    /// Does not carry VisitLog business columns.
    /// </summary>
    public class AuditEventRecord
    {
        public int EventID { get; set; }
        public int LogID { get; set; }
        public int? DayLogID { get; set; }
        public string EventType { get; set; }
        public string DisplayLabel { get; set; }
        public string ActorRole { get; set; }
        public string ActorID { get; set; }
        public DateTime EventTime { get; set; }
        public string Metadata { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
