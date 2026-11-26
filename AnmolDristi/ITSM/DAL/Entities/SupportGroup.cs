using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnmolDristi.ITSM.DAL.Entities
{
    /// <summary>
    /// R6 support group — company- and branch-scoped. Tickets may snapshot
    /// <see cref="Ticket.SupportGroupId"/> when dispatch wins a group queue.
    /// </summary>
    [Table("MST_SupportGroup", Schema = "itsm")]
    public class SupportGroup
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Tenant key aligned with Tickets.CompanyId / MST_UserMaster.CompanyId.
        /// </summary>
        [Required]
        public int CompanyId { get; set; }

        /// <summary>
        /// FK → dbo.MST_Branch.Id (same identity used by Tickets.BranchId).
        /// </summary>
        [Required]
        public int BranchId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [Required]
        public DateTime CreatedOn { get; set; }

        public DateTime? ModifiedOn { get; set; }

        [StringLength(50)]
        public string CreatedByCode { get; set; }

        [StringLength(50)]
        public string ModifiedByCode { get; set; }

        [ForeignKey("BranchId")]
        public virtual Branch Branch { get; set; }

        public virtual ICollection<SupportGroupMember> Members { get; set; }

        public virtual ICollection<SupportGroupMapping> Mappings { get; set; }

        public SupportGroup()
        {
            Members = new HashSet<SupportGroupMember>();
            Mappings = new HashSet<SupportGroupMapping>();
        }
    }
}
