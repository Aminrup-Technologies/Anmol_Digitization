using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnmolDristi.ITSM.DAL.Entities
{
    /// <summary>
    /// Maps to itsm.TicketAttachments — IIS file-system metadata (not database blobs).
    /// </summary>
    [Table("TicketAttachments", Schema = "itsm")]
    public class TicketAttachment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int TicketId { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        /// <summary>
        /// Relative path to the secure upload folder.
        /// </summary>
        [Required]
        [StringLength(500)]
        public string FilePath { get; set; }

        /// <summary>
        /// FK → dbo.MST_UserMaster.EmployeeCode
        /// </summary>
        [Required]
        [StringLength(50)]
        public string UploadedByCode { get; set; }

        public DateTime UploadedOn { get; set; }

        [ForeignKey("TicketId")]
        public virtual Ticket Ticket { get; set; }

        [ForeignKey("UploadedByCode")]
        public virtual UserMaster UploadedBy { get; set; }
    }
}
