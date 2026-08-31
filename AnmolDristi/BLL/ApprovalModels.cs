using System;
using System.Collections.Generic;
using AnmolDristi.DAL;

namespace AnmolDristi.BLL
{
    public enum ApprovalActorRole
    {
        Host = 0,
        Receptionist = 1,
        Admin = 2
    }

    /// <summary>
    /// Intent of the approval. Does not change DayLog uniqueness or hours policy.
    /// HostSelective: optional SelectedDays + optional ValidFrom/Until times.
    /// OverrideFullRange: Admin/Receptionist full requested window (SelectedDays ignored).
    /// </summary>
    public enum ApprovalMode
    {
        HostSelective = 0,
        OverrideFullRange = 1
    }

    public enum ApprovalOutcome
    {
        Success = 0,
        VisitNotFound = 1,
        NotAuthorized = 2,
        AlreadyApproved = 3,
        InvalidDaySelection = 4,
        ServiceError = 5
    }

    public class ApprovalRequest
    {
        public int LogID { get; set; }
        public ApprovalActorRole ActorRole { get; set; }
        public string ActorID { get; set; }
        public ApprovalMode ApprovalMode { get; set; }
        public IList<SelectedVisitDay> SelectedDays { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string OverrideReason { get; set; }

        /// <summary>Admin schedule datetime → ApprovedOn only. Not the visit window.</summary>
        public DateTime? ApprovedOnOverride { get; set; }

        public bool QueueHostOnBehalfNotification { get; set; }
        public string OnBehalfHostEmail { get; set; }
        public string OnBehalfHostName { get; set; }

        public static ApprovalRequest ForHost(
            int logId,
            string actorId,
            IList<SelectedVisitDay> selectedDays = null,
            TimeSpan? approvedFromTime = null,
            TimeSpan? approvedToTime = null)
        {
            return new ApprovalRequest
            {
                LogID = logId,
                ActorRole = ApprovalActorRole.Host,
                ActorID = actorId,
                ApprovalMode = ApprovalMode.HostSelective,
                SelectedDays = selectedDays,
                ValidFrom = approvedFromTime.HasValue ? DateTime.Today.Add(approvedFromTime.Value) : (DateTime?)null,
                ValidUntil = approvedToTime.HasValue ? DateTime.Today.Add(approvedToTime.Value) : (DateTime?)null
            };
        }

        public static ApprovalRequest ForReceptionist(int logId, string actorId, string overrideReason)
        {
            return new ApprovalRequest
            {
                LogID = logId,
                ActorRole = ApprovalActorRole.Receptionist,
                ActorID = actorId,
                ApprovalMode = ApprovalMode.OverrideFullRange,
                OverrideReason = overrideReason
            };
        }

        public static ApprovalRequest ForAdmin(
            int logId,
            string actorId,
            string overrideReason,
            DateTime? approvedOnOverride,
            string hostEmail,
            string hostName)
        {
            return new ApprovalRequest
            {
                LogID = logId,
                ActorRole = ApprovalActorRole.Admin,
                ActorID = actorId,
                ApprovalMode = ApprovalMode.OverrideFullRange,
                OverrideReason = overrideReason,
                ApprovedOnOverride = approvedOnOverride,
                QueueHostOnBehalfNotification = true,
                OnBehalfHostEmail = hostEmail,
                OnBehalfHostName = hostName
            };
        }
    }

    public class ApprovalResult
    {
        public bool Success { get; set; }
        public ApprovalOutcome Outcome { get; set; }
        public string Message { get; set; }
        public string WarningMessage { get; set; }
        public bool QRGenerated { get; set; }
        public bool AuditWritten { get; set; }
        public IList<VisitDayLogInfo> UpdatedDays { get; set; }
        public int LogId { get; set; }
        public Guid? QrToken { get; set; }
        public DateTime? ValidUpto { get; set; }

        public static ApprovalResult Fail(ApprovalOutcome outcome, string message, int logId)
        {
            return new ApprovalResult
            {
                Success = false,
                Outcome = outcome,
                Message = message,
                UpdatedDays = new List<VisitDayLogInfo>(),
                LogId = logId
            };
        }
    }
}
