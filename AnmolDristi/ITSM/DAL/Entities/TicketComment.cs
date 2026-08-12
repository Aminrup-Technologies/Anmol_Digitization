using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnmolDristi.ITSM.DAL.Entities
{
    /// <summary>
    /// Maps to itsm.TicketComments — requester / IT communication audit trail.
    /// </summary>
    [Table("TicketComments", Schema = "itsm")]
    public class TicketComment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int TicketId { get; set; }

        [Required]
        public string CommentText { get; set; }

        /// <summary>
        /// FK → dbo.MST_UserMaster.EmployeeCode
        /// </summary>
        [Required]
        [StringLength(50)]
        public string CreatedByCode { get; set; }

        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// 1 = visible to IT staff only.
        /// </summary>
        public bool IsInternal { get; set; }

        [ForeignKey("TicketId")]
        public virtual Ticket Ticket { get; set; }

        [ForeignKey("CreatedByCode")]
        public virtual UserMaster CreatedBy { get; set; }
    }
}
