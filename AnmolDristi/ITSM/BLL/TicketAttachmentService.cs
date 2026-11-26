using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using AnmolDristi.ITSM.DAL;
using AnmolDristi.ITSM.DAL.Entities;

namespace AnmolDristi.ITSM.BLL
{
    /// <summary>
    /// Persists attachment metadata after the ticket transaction has committed,
    /// and resolves an authorized download. Paths stay under the ITSM root.
    /// </summary>
    public sealed class TicketAttachmentService
    {
        public List<AttachmentFailure> SaveAfterCreate(
            int ticketId,
            string uploadedByCode,
            string attachmentRoot,
            IList<IncomingAttachment> files)
        {
            if (string.IsNullOrWhiteSpace(uploadedByCode))
            {
                throw new ArgumentException("Uploader is required.", "uploadedByCode");
            }

            return AttachmentUploadService.SaveFiles(
                attachmentRoot,
                ticketId,
                files,
                (relativePath, displayName) =>
                    Insert(ticketId, uploadedByCode, relativePath, displayName));
        }

        public AttachmentDownload ResolveDownload(
            int attachmentId,
            string actorCode,
            bool hasTicketViewPermission,
            int authorizedCompanyId,
            string attachmentRoot)
        {
            using (var context = new ItsmDbContext())
            {
                var row = (from attachment in context.TicketAttachments.AsNoTracking()
                           join ticket in context.Tickets.AsNoTracking()
                               on attachment.TicketId equals ticket.Id
                           where attachment.Id == attachmentId
                           select new
                           {
                               attachment.FileName,
                               attachment.FilePath,
                               attachment.TicketId,
                              ticket.RequesterCode,
                              ticket.AssigneeCode,
                              ticket.CompanyId,
                              ticket.BranchId,
                              ticket.CategoryId,
                              ticket.SubCategoryId,
                              ticket.ItemId,
                              ticket.SupportGroupId
                          }).FirstOrDefault();

                if (row == null)
                {
                    return null;
                }

                var canView = ItsmTicketAccess.CanViewTicket(
                    context,
                    row.TicketId,
                    actorCode,
                    row.RequesterCode,
                    row.AssigneeCode,
                    hasTicketViewPermission,
                    authorizedCompanyId,
                    row.CompanyId,
                    row.BranchId,
                    row.CategoryId,
                    row.SubCategoryId,
                    row.ItemId,
                    row.SupportGroupId);

                if (AttachmentAccessRules.ConcealDownload(true, canView))
                {
                    return null;
                }

                var fullPath = AttachmentUploadService.ResolveExistingFile(
                    attachmentRoot,
                    row.FilePath);
                if (fullPath == null)
                {
                    return null;
                }

                return new AttachmentDownload
                {
                    PhysicalPath = fullPath,
                    DownloadName = row.FileName
                };
            }
        }

        private static Exception Insert(
            int ticketId,
            string uploadedByCode,
            string relativePath,
            string displayName)
        {
            try
            {
                using (var context = new ItsmDbContext())
                {
                    var requester = context.Tickets
                        .AsNoTracking()
                        .Where(ticket => ticket.Id == ticketId)
                        .Select(ticket => ticket.RequesterCode)
                        .FirstOrDefault();

                    if (!string.Equals(
                        requester,
                        uploadedByCode,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return new InvalidOperationException(
                            "Attachment uploader does not match the ticket requester.");
                    }

                    context.TicketAttachments.Add(new TicketAttachment
                    {
                        TicketId = ticketId,
                        FileName = displayName,
                        FilePath = relativePath,
                        UploadedByCode = uploadedByCode,
                        UploadedOn = DateTime.Now
                    });
                    context.SaveChanges();
                    return null;
                }
            }
            catch (DataException ex)
            {
                return ex;
            }
            catch (InvalidOperationException ex)
            {
                return ex;
            }
        }
    }
}
