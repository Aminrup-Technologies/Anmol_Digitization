using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnmolDristi.ITSM.DAL.Entities
{
    /// <summary>
    /// Membership of an employee in a support group. At most one active
    /// Team Leader per group (enforced in BLL and by a filtered unique index).
    /// </summary>
    [Table("MST_SupportGroupMember", Schema = "itsm")]
    public class SupportGroupMember
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int SupportGroupId { get; set; }

        [Required]
        [StringLength(50)]
        public string EmployeeCode { get; set; }

        [Required]
        public bool IsTeamLeader { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [Required]
        public DateTime CreatedOn { get; set; }

        public DateTime? ModifiedOn { get; set; }

        [ForeignKey("SupportGroupId")]
        public virtual SupportGroup SupportGroup { get; set; }

        [ForeignKey("EmployeeCode")]
        public virtual UserMaster Employee { get; set; }
    }
}
