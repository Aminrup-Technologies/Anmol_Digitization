using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnmolDristi.ITSM.DAL.Entities
{
    /// <summary>
    /// Maps to itsm.TicketApprovals — multi-level approval workflow for SR and CR tickets.
    /// </summary>
    [Table("TicketApprovals", Schema = "itsm")]
    public class TicketApproval
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int TicketId { get; set; }

        /// <summary>
        /// Manager or ITHead.
        /// </summary>
        [Required]
        [StringLength(20)]
        public string ApprovalLevel { get; set; }

        /// <summary>
        /// FK → dbo.MST_UserMaster.EmployeeCode
        /// </summary>
        [Required]
        [StringLength(50)]
        public string ApproverCode { get; set; }

        /// <summary>
        /// Pending, Approved, or Rejected.
        /// </summary>
        [Required]
        [StringLength(20)]
        public string ApprovalStatus { get; set; }

        [StringLength(500)]
        public string Comments { get; set; }

        /// <summary>
        /// When approved/rejected.
        /// </summary>
        public DateTime? ActionedOn { get; set; }

        /// <summary>
        /// When the approval row was created.
        /// </summary>
        public DateTime CreatedOn { get; set; }

        [ForeignKey("TicketId")]
        public virtual Ticket Ticket { get; set; }

        [ForeignKey("ApproverCode")]
        public virtual UserMaster Approver { get; set; }
    }
}
