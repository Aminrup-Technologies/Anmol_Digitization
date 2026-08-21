// ========================================================================
// File:    TicketAuditLog.cs
// Layer:   DAL — Entity
// When:    2026-08-21
// What:    CompanyId documented as Ticket.CompanyId (ticket tenant), not actor company.
// Why:     Shared-service processing audits the ticket tenant being mutated.
// ========================================================================

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnmolDristi.ITSM.DAL.Entities
{
    /// <summary>
    /// Maps to itsm.TicketAuditLog — immutable audit trail for ticket workflow mutations.
    /// Each row records a single atomic operation: state transition, reassignment,
    /// priority override, approval, rejection, or comment.
    /// </summary>
    [Table("TicketAuditLog", Schema = "itsm")]
    public class TicketAuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// FK → itsm.Tickets.Id — the ticket this audit record belongs to.
        /// </summary>
        [Required]
        public int TicketId { get; set; }

        /// <summary>
        /// Tenant of the ticket being audited (Ticket.CompanyId snapshot), not actor CompanyId.
        /// </summary>
        [Required]
        public int CompanyId { get; set; }

        /// <summary>
        /// The type of mutation: "StatusChange", "Reassignment", "PriorityOverride",
        /// "Approval", "Rejection", "Comment", "Created", "Resolution".
        /// </summary>
        [Required]
        [StringLength(50)]
        public string ActionType { get; set; }

        /// <summary>
        /// Previous value before mutation. Null for "Created" and "Comment" actions.
        /// </summary>
        [StringLength(200)]
        public string OldValue { get; set; }

        /// <summary>
        /// New value after mutation. Null for "Comment" actions.
        /// </summary>
        [StringLength(200)]
        public string NewValue { get; set; }

        /// <summary>
        /// Free-text detail — mandatory for Rejection, optional otherwise.
        /// Provides the rejection reason or additional context.
        /// </summary>
        [StringLength(1000)]
        public string Detail { get; set; }

        /// <summary>
        /// FK → dbo.MST_UserMaster.EmployeeCode — who performed this action.
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column(TypeName = "varchar")]
        public string ActorCode { get; set; }

        /// <summary>
        /// UTC or local timestamp of when the action was performed.
        /// </summary>
        [Required]
        public DateTime ActionedOn { get; set; }

        // --- Navigation ---

        [ForeignKey("TicketId")]
        public virtual Ticket Ticket { get; set; }
    }
}
