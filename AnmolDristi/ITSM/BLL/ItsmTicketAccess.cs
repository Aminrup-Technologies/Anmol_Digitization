// ========================================================================
// When: 2026-08-21
// What: Split ticket authorization by operation: CanViewTicket,
//       CanApproveTicket, CanProcessTicket. Pending approval is not
//       general processing. Historical approval rows are not view/process.
//       2026-08-24: Added RBAC-gated ITSM.Ticket.View authorization.
//       A user with the permission can view tickets belonging to their
//       authorized tenant (Ticket.CompanyId), not arbitrary tickets.
// Why: A TicketApprovals row must not make an approver a general processor.
//       Non-terminal status is not authorization. The permission gate
//       must be tenant-scoped to prevent cross-company access via ?id=.
// ========================================================================

using System;
using System.Data.Entity;
using System.Linq;
using AnmolDristi.ITSM.DAL;

namespace AnmolDristi.ITSM.BLL
{
    /// <summary>
    /// Operation-specific ticket authorization from stored relationships.
    /// SupportMapping is create-time dispatch, not a live ACL.
    /// ITSM.Ticket.View permission is evaluated by the UI layer and passed
    /// in — BLL remains System.Web-agnostic.
    /// </summary>
    internal static class ItsmTicketAccess
    {
        /// <summary>
        /// Determines whether the actor may view ticket detail.
        /// Authorization paths:
        ///   1. Actor is the ticket requester
        ///   2. Actor is the ticket assignee
        ///   3. Actor has a pending approval for the ticket
        ///   4. Actor has ITSM.Ticket.View AND ticketCompanyId matches
        ///      authorizedCompanyId (tenant-scoped permission)
        /// The hasTicketViewPermission and authorizedCompanyId parameters
        /// are resolved by the UI layer (BasePage.HasPermission + tenant
        /// resolution). BLL never references System.Web.
        /// </summary>
        public static bool CanViewTicket(
            ItsmDbContext context,
            int ticketId,
            string actorCode,
            string requesterCode,
            string assigneeCode,
            bool hasTicketViewPermission,
            int authorizedCompanyId,
            int? ticketCompanyId,
            int? supportGroupId = null)
        {
            if (IsRequesterOrAssignee(actorCode, requesterCode, assigneeCode))
            {
                return true;
            }

            if (HasPendingApproval(context, ticketId, actorCode))
            {
                return true;
            }

            // RBAC: ITSM.Ticket.View — tenant-scoped.
            // Permission alone is not sufficient; the ticket must belong
            // to the user's authorized company.
            if (hasTicketViewPermission
                && ticketCompanyId.HasValue
                && ticketCompanyId.Value == authorizedCompanyId)
            {
                return true;
            }

            // R6: active members of the snapshotted support group may view
            // an unassigned group-queue ticket in the same company.
            if (string.IsNullOrWhiteSpace(assigneeCode)
                && supportGroupId.HasValue
                && ticketCompanyId.HasValue
                && ticketCompanyId.Value > 0
                && IsActiveGroupMember(context, supportGroupId.Value, actorCode, ticketCompanyId.Value))
            {
                return true;
            }

            return false;
        }

        public static bool IsActiveGroupMember(
            ItsmDbContext context,
            int supportGroupId,
            string employeeCode,
            int ticketCompanyId)
        {
            if (string.IsNullOrWhiteSpace(employeeCode) || ticketCompanyId <= 0)
            {
                return false;
            }

            return context.SupportGroupMembers.AsNoTracking()
                .Any(m => m.SupportGroupId == supportGroupId
                    && m.IsActive
                    && m.EmployeeCode == employeeCode
                    && m.Employee != null
                    && m.Employee.EmployeeStatus == "Active"
                    && m.Employee.CompanyId == ticketCompanyId
                    && m.SupportGroup != null
                    && m.SupportGroup.IsActive
                    && m.SupportGroup.CompanyId == ticketCompanyId);
        }

        public static bool IsActiveTeamLeader(
            ItsmDbContext context,
            int supportGroupId,
            string employeeCode,
            int ticketCompanyId)
        {
            if (string.IsNullOrWhiteSpace(employeeCode) || ticketCompanyId <= 0)
            {
                return false;
            }

            return context.SupportGroupMembers.AsNoTracking()
                .Any(m => m.SupportGroupId == supportGroupId
                    && m.IsActive
                    && m.IsTeamLeader
                    && m.EmployeeCode == employeeCode
                    && m.Employee != null
                    && m.Employee.EmployeeStatus == "Active"
                    && m.Employee.CompanyId == ticketCompanyId
                    && m.SupportGroup != null
                    && m.SupportGroup.IsActive
                    && m.SupportGroup.CompanyId == ticketCompanyId);
        }

        public static bool CanProcessTicket(
            string actorCode,
            string requesterCode,
            string assigneeCode)
        {
            return IsRequesterOrAssignee(actorCode, requesterCode, assigneeCode);
        }

        public static bool CanApproveTicket(
            ItsmDbContext context,
            int ticketId,
            string actorCode,
            string requesterCode)
        {
            if (string.IsNullOrWhiteSpace(actorCode))
            {
                return false;
            }

            if (string.Equals(requesterCode, actorCode, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return HasPendingApproval(context, ticketId, actorCode);
        }

        private static bool IsRequesterOrAssignee(
            string actorCode,
            string requesterCode,
            string assigneeCode)
        {
            if (string.IsNullOrWhiteSpace(actorCode))
            {
                return false;
            }

            if (string.Equals(requesterCode, actorCode, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(assigneeCode)
                && string.Equals(assigneeCode, actorCode, StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasPendingApproval(
            ItsmDbContext context,
            int ticketId,
            string actorCode)
        {
            return context.TicketApprovals.AsNoTracking()
                .Any(a => a.TicketId == ticketId
                          && a.ApproverCode == actorCode
                          && a.ApprovalStatus == "Pending");
        }
    }
}
