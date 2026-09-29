using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnmolDristi.ITSM.DAL.Entities
{
    /// <summary>
    /// Maps to itsm.MST_ApprovalConfiguration — global IT Head approver configuration.
    /// Only one active row may exist at any time (enforced by filtered unique index).
    /// </summary>
    [Table("MST_ApprovalConfiguration", Schema = "itsm")]
    public class ApprovalConfiguration
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// FK → dbo.MST_UserMaster.EmployeeCode — the configured IT Head approver.
        /// Only one active configuration is permitted.
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column(TypeName = "varchar")]
        public string ItHeadApproverCode { get; set; }

        /// <summary>
        /// Only one row may be active at any time (filtered unique index).
        /// </summary>
        public bool IsActive { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime? ModifiedOn { get; set; }

        // --- Navigation ---

        [ForeignKey("ItHeadApproverCode")]
        public virtual UserMaster ItHeadApprover { get; set; }
    }
}
