using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnmolDristi.ITSM.DAL.Entities
{
    /// <summary>
    /// Maps to itsm.Tickets — central ITSM transactional record.
    /// RowVersion enables EF6 optimistic concurrency (see technical guidelines §2).
    /// </summary>
    [Table("Tickets", Schema = "itsm")]
    public class Ticket
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        [Column(TypeName = "varchar")]
        public string TicketNumber { get; set; }

        [Required]
        public int RequestTypeId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// FK → dbo.MST_UserMaster.EmployeeCode
        /// </summary>
        [Required]
        [StringLength(50)]
        public string RequesterCode { get; set; }

        /// <summary>
        /// FK → dbo.MST_UserMaster.EmployeeCode — set by Intelligent Dispatch Matrix.
        /// </summary>
        [StringLength(50)]
        public string AssigneeCode { get; set; }

        /// <summary>
        /// FK → dbo.MST_Plant.Id
        /// </summary>
        public int? PlantId { get; set; }

        /// <summary>
        /// FK → dbo.MST_Branch.Id — from requester context; drives dispatch.
        /// </summary>
        [Required]
        public int BranchId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public int SubCategoryId { get; set; }

        /// <summary>
        /// Finest taxonomy selection (optional).
        /// </summary>
        public int? ItemId { get; set; }

        /// <summary>
        /// User-selected; drives priority calculation.
        /// </summary>
        [Required]
        public int ImpactId { get; set; }

        /// <summary>
        /// User-selected; drives priority calculation.
        /// </summary>
        [Required]
        public int UrgencyId { get; set; }

        /// <summary>
        /// Auto-calculated from Impact × Urgency matrix — not chosen on the form.
        /// </summary>
        [Required]
        public int PriorityId { get; set; }

        [Required]
        public int StatusId { get; set; }

        /// <summary>
        /// Agent resolution notes — required when moving to Resolved.
        /// </summary>
        public string ResolutionText { get; set; }

        /// <summary>
        /// Customer satisfaction score (1–5) — captured at Closed. TINYINT in SQL.
        /// </summary>
        public byte? CsatScore { get; set; }

        /// <summary>
        /// Optional free-text CSAT comment from requester.
        /// </summary>
        [StringLength(500)]
        public string CsatFeedback { get; set; }

        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Set when agent marks ticket Resolved (step 1 of two-step closure).
        /// </summary>
        public DateTime? ResolvedOn { get; set; }

        /// <summary>
        /// Set when ticket moves to Closed after CSAT or 72-hour auto-close.
        /// </summary>
        public DateTime? ClosedOn { get; set; }

        [Required]
        [StringLength(50)]
        public string CreatedByCode { get; set; }

        public DateTime? ModifiedOn { get; set; }

        [StringLength(50)]
        public string ModifiedByCode { get; set; }

        /// <summary>
        /// SQL Server rowversion — EF6 optimistic concurrency token.
        /// Database-generated; do not set in application code.
        /// </summary>
        [Timestamp]
        public byte[] RowVersion { get; set; }

        // --- Navigation properties ---

        [ForeignKey("RequestTypeId")]
        public virtual RequestType RequestType { get; set; }

        [ForeignKey("RequesterCode")]
        public virtual UserMaster Requester { get; set; }

        [ForeignKey("AssigneeCode")]
        public virtual UserMaster Assignee { get; set; }

        [ForeignKey("PlantId")]
        public virtual Plant Plant { get; set; }

        [ForeignKey("BranchId")]
        public virtual Branch Branch { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }

        [ForeignKey("SubCategoryId")]
        public virtual SubCategory SubCategory { get; set; }

        [ForeignKey("ItemId")]
        public virtual Item Item { get; set; }

        [ForeignKey("ImpactId")]
        public virtual Impact Impact { get; set; }

        [ForeignKey("UrgencyId")]
        public virtual Urgency Urgency { get; set; }

        [ForeignKey("PriorityId")]
        public virtual Priority Priority { get; set; }

        [ForeignKey("StatusId")]
        public virtual TicketStatus Status { get; set; }

        public virtual ICollection<TicketComment> Comments { get; set; }

        public virtual ICollection<TicketAttachment> Attachments { get; set; }

        public virtual ICollection<TicketApproval> Approvals { get; set; }

        public virtual ICollection<TicketAuditLog> AuditLogs { get; set; }

        public Ticket()
        {
            Comments = new HashSet<TicketComment>();
            Attachments = new HashSet<TicketAttachment>();
            Approvals = new HashSet<TicketApproval>();
            AuditLogs = new HashSet<TicketAuditLog>();
        }
    }
}
