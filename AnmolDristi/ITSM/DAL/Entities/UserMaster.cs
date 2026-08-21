using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnmolDristi.ITSM.DAL.Entities
{
    /// <summary>
    /// Read-only map of dbo.MST_UserMaster — ITSM identity and routing context.
    /// Treat as AsNoTracking(); SaveChanges must not update this entity.
    /// </summary>
    [Table("MST_UserMaster", Schema = "dbo")]
    public class UserMaster
    {
        [Key]
        [StringLength(50)]
        [Column(TypeName = "varchar")]
        public string EmployeeCode { get; set; }

        [Required]
        [StringLength(100)]
        [Column(TypeName = "varchar")]
        public string EmployeeName { get; set; }

        [StringLength(100)]
        [Column(TypeName = "varchar")]
        public string Email { get; set; }

        [StringLength(10)]
        [Column(TypeName = "varchar")]
        public string Mobile { get; set; }

        public int PlantId { get; set; }

        public int BranchId { get; set; }

        public int? DepartmentId { get; set; }

        /// <summary>
        /// Company tenant key from dbo.MST_UserMaster — ITGC isolation boundary.
        /// </summary>
        public int? CompanyId { get; set; }

        [StringLength(50)]
        [Column(TypeName = "varchar")]
        public string ReportingManagerCode { get; set; }

        [StringLength(50)]
        [Column(TypeName = "varchar")]
        public string EmployeeStatus { get; set; }

        [ForeignKey("PlantId")]
        public virtual Plant Plant { get; set; }

        [ForeignKey("BranchId")]
        public virtual Branch Branch { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }

        [InverseProperty("Requester")]
        public virtual ICollection<Ticket> RequestedTickets { get; set; }

        [InverseProperty("Assignee")]
        public virtual ICollection<Ticket> AssignedTickets { get; set; }

        public UserMaster()
        {
            RequestedTickets = new HashSet<Ticket>();
            AssignedTickets = new HashSet<Ticket>();
        }
    }
}
