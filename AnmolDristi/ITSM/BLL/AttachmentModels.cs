using System;
using System.Collections.Generic;
using System.IO;

namespace AnmolDristi.ITSM.BLL
{
    /// <summary>
    /// One posted file. The UI copies HttpPostedFile into this type so the
    /// BLL does not reference System.Web.
    /// </summary>
    public sealed class IncomingAttachment
    {
        public string FileName { get; set; }

        public long DeclaredLength { get; set; }

        public Stream Content { get; set; }
    }

    [Serializable]
    public sealed class AttachmentFailure
    {
        public string FileName { get; set; }

        public string Reason { get; set; }
    }

    /// <summary>
    /// One-time notice carried across the post-redirect from Create Ticket
    /// to Ticket Detail. Shown only for the ticket that was just created.
    /// </summary>
    [Serializable]
    public sealed class AttachmentFailureNotice
    {
        public int TicketId { get; set; }

        public List<AttachmentFailure> Failures { get; set; }
    }

    public sealed class AttachmentDownload
    {
        public string PhysicalPath { get; set; }

        public string DownloadName { get; set; }
    }
}
