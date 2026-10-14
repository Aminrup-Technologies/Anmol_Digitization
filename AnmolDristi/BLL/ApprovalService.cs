using System;
using System.Collections.Generic;
using AnmolDristi.DAL;

namespace AnmolDristi.BLL
{
    /// <summary>
    /// Single VMS approval orchestrator. Host, Receptionist, and Admin all call
    /// <see cref="Execute"/>. Role-specific permission is encoded on
    /// <see cref="ApprovalRequest.ActorRole"/>; DayLog / hours / notify rules are unchanged.
    /// </summary>
    public class ApprovalService
    {
        public const string SuccessMessage = "Visitor approved. Digital pass sent.";
        public const string VisitNotFoundMessage = "Unable to approve this visit. It was not found.";
        public const string NotAuthorizedMessage = "Unable to approve this visit. You are not authorized.";
        public const string AlreadyApprovedMessage = "Unable to approve this visit. It is no longer pending.";
        public const string InvalidDaySelectionMessage = "Please select at least one day to approve.";
        public const string ServiceErrorMessage = "Unable to approve this visit. Please try again.";
        public const string NotFoundOrNotOwnedMessage =
            "Unable to approve this visit. It was not found or you are not authorized.";

        private readonly VmsVisitorManagementRepository _repository;

        public ApprovalService()
            : this(new VmsVisitorManagementRepository())
        {
        }

        public ApprovalService(VmsVisitorManagementRepository repository)
        {
            if (repository == null)
                throw new ArgumentNullException("repository");
            _repository = repository;
        }

        /// <summary>
        /// Pipeline (same committed lifecycle as AR-VMS-02):
        /// load visit → validate status/ownership → validate selected days →
        /// evaluate hours (result warning) → DayLog flags → QR → VisitLog snapshot →
        /// schedule/validity → after-hours audit → notifications → structured result.
        /// Hours audit still runs after VisitLog/DayLog writes so it sees approved Valid*.
        /// </summary>
        public ApprovalResult Execute(ApprovalRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            if (request.LogID <= 0)
                return ApprovalResult.Fail(ApprovalOutcome.VisitNotFound, VisitNotFoundMessage, request.LogID);

            if (HasUncheckedDaySelection(request))
                return ApprovalResult.Fail(ApprovalOutcome.InvalidDaySelection, InvalidDaySelectionMessage, request.LogID);

            VisitorApprovalRequest lifecycleRequest = ToLifecycleRequest(request);
            ApprovalAuthorizationMode authMode = ToAuthorizationMode(request.ActorRole);

            VisitorApprovalResult raw;
            try
            {
                raw = _repository.ExecuteApprovalLifecycle(lifecycleRequest, authMode);
            }
            catch
            {
                return ApprovalResult.Fail(ApprovalOutcome.ServiceError, ServiceErrorMessage, request.LogID);
            }

            if (raw == null)
                return ApprovalResult.Fail(ApprovalOutcome.ServiceError, ServiceErrorMessage, request.LogID);

            ApprovalResult result = MapLifecycleResult(raw);
            if (result.Success)
            {
                try
                {
                    result.UpdatedDays = _repository.GetVisitDayLogs(result.LogId)
                        ?? new List<VisitDayLogInfo>();
                }
                catch
                {
                    result.UpdatedDays = new List<VisitDayLogInfo>();
                }
            }

            return result;
        }

        private static bool HasUncheckedDaySelection(ApprovalRequest request)
        {
            if (request.ApprovalMode == ApprovalMode.OverrideFullRange)
                return false;
            if (request.SelectedDays == null || request.SelectedDays.Count == 0)
                return false;

            foreach (SelectedVisitDay day in request.SelectedDays)
            {
                if (day != null && day.IsApproved)
                    return false;
            }

            return true;
        }

        private static ApprovalAuthorizationMode ToAuthorizationMode(ApprovalActorRole role)
        {
            return role == ApprovalActorRole.Host
                ? ApprovalAuthorizationMode.HostOwnership
                : ApprovalAuthorizationMode.PendingOnly;
        }

        private static VisitorApprovalRequest ToLifecycleRequest(ApprovalRequest request)
        {
            IList<SelectedVisitDay> selectedDays = request.ApprovalMode == ApprovalMode.OverrideFullRange
                ? null
                : request.SelectedDays;

            TimeSpan? fromTime = request.ValidFrom.HasValue
                ? request.ValidFrom.Value.TimeOfDay
                : (TimeSpan?)null;
            TimeSpan? toTime = request.ValidUntil.HasValue
                ? request.ValidUntil.Value.TimeOfDay
                : (TimeSpan?)null;
            if (!fromTime.HasValue || !toTime.HasValue)
            {
                fromTime = null;
                toTime = null;
            }

            string approvedBy = request.ActorID;
            if (request.ActorRole == ApprovalActorRole.Admin)
                approvedBy = string.IsNullOrWhiteSpace(request.ActorID) ? "Admin" : request.ActorID;
            else if (request.ActorRole == ApprovalActorRole.Receptionist)
                approvedBy = string.IsNullOrWhiteSpace(request.ActorID) ? "Receptionist" : request.ActorID;

            return new VisitorApprovalRequest
            {
                LogId = request.LogID,
                HostEmployeeCode = request.ActorRole == ApprovalActorRole.Host
                    ? (request.ActorID ?? string.Empty)
                    : string.Empty,
                SelectedDays = selectedDays,
                ApprovedFromTime = fromTime,
                ApprovedToTime = toTime,
                ApprovalRemarksSuffix = request.OverrideReason,
                ApprovedOnOverride = request.ApprovedOnOverride,
                QueueHostOnBehalfNotification = request.QueueHostOnBehalfNotification,
                OnBehalfHostEmail = request.OnBehalfHostEmail,
                OnBehalfHostName = request.OnBehalfHostName,
                ApprovedBy = approvedBy,
                ActorRole = request.ActorRole.ToString()
            };
        }

        private static ApprovalResult MapLifecycleResult(VisitorApprovalResult raw)
        {
            if (raw.Succeeded)
            {
                return new ApprovalResult
                {
                    Success = true,
                    Outcome = ApprovalOutcome.Success,
                    Message = SuccessMessage,
                    WarningMessage = raw.WarningMessage,
                    QRGenerated = raw.QrGenerated || raw.QrToken.HasValue,
                    AuditWritten = raw.AuditWritten,
                    UpdatedDays = new List<VisitDayLogInfo>(),
                    LogId = raw.LogId,
                    QrToken = raw.QrToken,
                    ValidUpto = raw.ValidUpto
                };
            }

            ApprovalOutcome outcome;
            string message;
            switch (raw.FailureReason)
            {
                case ApprovalFailureReason.NotPending:
                    outcome = ApprovalOutcome.AlreadyApproved;
                    message = AlreadyApprovedMessage;
                    break;
                case ApprovalFailureReason.NotFoundOrNotOwned:
                    outcome = ApprovalOutcome.VisitNotFound;
                    message = NotFoundOrNotOwnedMessage;
                    break;
                default:
                    outcome = ApprovalOutcome.ServiceError;
                    message = ServiceErrorMessage;
                    break;
            }

            return ApprovalResult.Fail(outcome, message, raw.LogId);
        }
    }
}
