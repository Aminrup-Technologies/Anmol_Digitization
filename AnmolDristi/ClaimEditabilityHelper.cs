using System;

namespace AnmolDristi
{
    /// <summary>
    /// Centralized edit/delete eligibility rules for sales claim activities and expenses.
    /// </summary>
    public static class ClaimEditabilityHelper
    {
        public static bool IsReturnedDay(string claimStatus, string detailStatus)
        {
            return ClaimStatuses.Equals(detailStatus, ClaimStatuses.Returned)
                || ClaimStatuses.Equals(claimStatus, ClaimStatuses.Returned);
        }

        public static bool IsRejectedDay(string claimStatus, string detailStatus)
        {
            return ClaimStatuses.Equals(detailStatus, ClaimStatuses.Rejected)
                || ClaimStatuses.Equals(claimStatus, ClaimStatuses.Rejected);
        }

        public static bool IsSubmittedApprovedOrPending(string status)
        {
            return ClaimStatuses.Equals(status, ClaimStatuses.Submitted)
                || ClaimStatuses.Equals(status, ClaimStatuses.Approved)
                || ClaimStatuses.Equals(status, ClaimStatuses.Pending);
        }

        /// <summary>
        /// Inline row edit / update gate (activity ClaimStatus).
        /// </summary>
        public static bool CanEditActivity(string activityClaimStatus)
        {
            return !IsSubmittedApprovedOrPending(activityClaimStatus)
                && !ClaimStatuses.Equals(activityClaimStatus, ClaimStatuses.Rejected);
        }

        /// <summary>
        /// KM modal save gate (activity ClaimStatus).
        /// </summary>
        public static bool CanEditActivityKm(string activityClaimStatus)
        {
            return !IsSubmittedApprovedOrPending(activityClaimStatus);
        }

        /// <summary>
        /// Grid edit-link visibility (activity row).
        /// </summary>
        public static bool CanEditActivity(string activityClaimStatus, string detailStatus, bool isEditableRow)
        {
            string effectiveStatus = ResolveEffectiveVisualStatus(activityClaimStatus, detailStatus, isEditableRow);
            bool showEdit = isEditableRow && !ClaimStatuses.Equals(effectiveStatus, ClaimStatuses.Rejected);
            if (!showEdit && ClaimStatuses.Equals(activityClaimStatus, ClaimStatuses.Draft))
                showEdit = true;
            return showEdit;
        }

        public static bool CanEditActivityKmInGrid(bool isEditableRow, string activityClaimStatus, string detailStatus)
        {
            string effectiveStatus = ResolveEffectiveVisualStatus(activityClaimStatus, detailStatus, isEditableRow);
            return isEditableRow && !ClaimStatuses.Equals(effectiveStatus, ClaimStatuses.Rejected);
        }

        public static bool CanOpenExpensesForActivity(string activityClaimStatus, string detailStatus)
        {
            string effectiveStatus = ResolveEffectiveVisualStatus(activityClaimStatus, detailStatus, true);
            return !ClaimStatuses.Equals(effectiveStatus, ClaimStatuses.Rejected);
        }

        /// <summary>
        /// Expense modal editability (activity + detail status).
        /// </summary>
        public static bool CanEditExpense(string activityClaimStatus, string detailStatus)
        {
            if (IsReturnedDay(activityClaimStatus, detailStatus))
                return true;

            if (IsRejectedDay(activityClaimStatus, detailStatus))
                return false;

            return !IsSubmittedApprovedOrPending(activityClaimStatus);
        }

        /// <summary>
        /// Expense save gate when only detail status is known.
        /// </summary>
        public static bool CanEditExpenseByDetailStatus(string detailStatus)
        {
            return !IsSubmittedApprovedOrPending(detailStatus);
        }

        public static bool IsRemovableExpenseClaimStatus(string expenseClaimStatus)
        {
            return ClaimStatuses.Equals(expenseClaimStatus, ClaimStatuses.Draft)
                || ClaimStatuses.Equals(expenseClaimStatus, ClaimStatuses.Returned);
        }

        /// <summary>
        /// Expense line delete eligibility.
        /// </summary>
        public static bool CanDeleteExpense(string expenseClaimStatus, bool isAutoDa = false)
        {
            if (isAutoDa)
                return false;

            if (!IsRemovableExpenseClaimStatus(expenseClaimStatus))
                return false;

            if (!string.IsNullOrWhiteSpace(expenseClaimStatus)
                && (IsSubmittedApprovedOrPending(expenseClaimStatus)
                    || ClaimStatuses.Equals(expenseClaimStatus, ClaimStatuses.Rejected)))
                return false;

            return true;
        }

        /// <summary>
        /// Activity-level gate before deleting an expense.
        /// </summary>
        public static bool CanDeleteExpenseForActivity(string activityClaimStatus, string detailStatus)
        {
            return IsReturnedDay(activityClaimStatus, detailStatus)
                || ClaimStatuses.Equals(activityClaimStatus, ClaimStatuses.Draft);
        }

        /// <summary>
        /// Detail statuses that reopen a day for employee correction.
        /// </summary>
        public static bool IsDetailStatusEditableForCorrection(string detailStatus)
        {
            return ClaimStatuses.Equals(detailStatus, ClaimStatuses.Rejected)
                || ClaimStatuses.Equals(detailStatus, ClaimStatuses.Returned);
        }

        /// <summary>
        /// Approver KM edit on ClaimsApproval (legacy) pages.
        /// </summary>
        public static bool CanEditKmAsApprover(string claimStatus, bool isAuditor, bool isProcessed)
        {
            if (isProcessed)
                return false;

            return ClaimStatuses.Equals(claimStatus, ClaimStatuses.Draft)
                || ClaimStatuses.Equals(claimStatus, ClaimStatuses.Submitted)
                || isAuditor;
        }

        public static string ResolveEffectiveVisualStatus(string claimStatus, string detailStatus, bool isEditableRow)
        {
            if (IsReturnedDay(claimStatus, detailStatus))
                return ClaimStatuses.Returned;

            if (IsRejectedDay(claimStatus, detailStatus))
                return ClaimStatuses.Rejected;

            if (ClaimStatuses.Equals(claimStatus, ClaimStatuses.Draft))
                return ClaimStatuses.Draft;

            if (isEditableRow)
                return ClaimStatuses.Returned;

            return !string.IsNullOrEmpty(claimStatus) ? claimStatus : ClaimStatuses.Unknown;
        }

        public static string ResolveEffectiveRowStatus(string claimStatus, string detailStatus, bool isEditableRow)
        {
            if (IsReturnedDay(claimStatus, detailStatus))
                return ClaimStatuses.Returned;

            if (IsRejectedDay(claimStatus, detailStatus))
                return ClaimStatuses.Rejected;

            if (!string.IsNullOrWhiteSpace(claimStatus))
                return claimStatus;

            return isEditableRow ? ClaimStatuses.Returned : ClaimStatuses.Unknown;
        }
    }
}
