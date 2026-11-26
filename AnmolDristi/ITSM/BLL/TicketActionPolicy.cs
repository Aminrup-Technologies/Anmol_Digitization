// ========================================================================
// When: 2026-11-25
// What: Actor rules for status changes. Assignee processes work.
//       Requester comments, cancels before work is in progress, and is the
//       only actor who closes or returns a resolution. Hold uses one class.
// Why: Standard-practice register options D-2, D-5, and D-6. No new status
//      and no invented reopen day count. SLA restart is out of this build.
// ========================================================================

using System;
using System.Collections.Generic;

namespace AnmolDristi.ITSM.BLL
{
    internal static class TicketActionPolicy
    {
        public const string HoldRequester = "Requester";
        public const string HoldVendor = "Vendor";
        public const string HoldInternal = "Internal";

        public static bool SameCode(string left, string right)
        {
            return !string.IsNullOrWhiteSpace(left)
                && !string.IsNullOrWhiteSpace(right)
                && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsHoldClass(string holdClass)
        {
            if (string.IsNullOrWhiteSpace(holdClass))
            {
                return false;
            }

            var value = holdClass.Trim();
            return value == HoldRequester || value == HoldVendor || value == HoldInternal;
        }

        public static string HoldDetail(string holdClass)
        {
            return "HoldClass: " + holdClass.Trim();
        }

        /// <summary>
        /// Status names this actor may move to. The list is the server rule.
        /// Page visibility is not the authority.
        /// </summary>
        public static List<string> AllowedTargetNames(
            string currentStatus,
            bool isTerminal,
            string actorCode,
            string requesterCode,
            string assigneeCode)
        {
            var names = new List<string>();
            if (isTerminal || string.IsNullOrWhiteSpace(currentStatus) || string.IsNullOrWhiteSpace(actorCode))
            {
                return names;
            }

            var asRequester = SameCode(actorCode, requesterCode);
            var asAssignee = SameCode(actorCode, assigneeCode);

            if (asAssignee)
            {
                if (currentStatus == "New")
                {
                    names.Add("In Progress");
                    names.Add("On Hold");
                }
                else if (currentStatus == "In Progress")
                {
                    names.Add("Resolved");
                    names.Add("On Hold");
                    names.Add("Cancelled");
                }
                else if (currentStatus == "On Hold")
                {
                    names.Add("In Progress");
                    names.Add("Cancelled");
                }
            }

            if (asRequester)
            {
                if (currentStatus == "New" || currentStatus == "Pending Approval")
                {
                    if (!names.Contains("Cancelled"))
                    {
                        names.Add("Cancelled");
                    }
                }

                if (currentStatus == "Resolved")
                {
                    names.Add("Closed");
                    names.Add("In Progress");
                }
            }

            return names;
        }
    }
}
