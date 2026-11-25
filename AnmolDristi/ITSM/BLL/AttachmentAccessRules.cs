using System;

namespace AnmolDristi.ITSM.BLL
{
    /// <summary>
    /// Same view decision as <see cref="ItsmTicketAccess.CanViewTicket"/>.
    /// Pending-approval lookup stays in ItsmTicketAccess so this type does
    /// not touch the database. A missing attachment and a denied actor are
    /// both concealed: the handler returns one 404 for either case.
    /// </summary>
    public static class AttachmentAccessRules
    {
        public static bool IsRequesterOrAssignee(
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

        public static bool AllowsTicketView(
            string actorCode,
            string requesterCode,
            string assigneeCode,
            bool hasPendingApproval,
            bool hasTicketViewPermission,
            int authorizedCompanyId,
            int? ticketCompanyId)
        {
            if (IsRequesterOrAssignee(actorCode, requesterCode, assigneeCode))
            {
                return true;
            }

            if (hasPendingApproval)
            {
                return true;
            }

            return hasTicketViewPermission
                && ticketCompanyId.HasValue
                && ticketCompanyId.Value == authorizedCompanyId;
        }

        public static bool ConcealDownload(bool attachmentExists, bool canViewTicket)
        {
            return !attachmentExists || !canViewTicket;
        }

        public static int DownloadStatusCode(bool sendFile)
        {
            return sendFile ? 200 : 404;
        }
    }
}
