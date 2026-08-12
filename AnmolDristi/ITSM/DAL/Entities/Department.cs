using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnmolDristi.ITSM.DAL.Entities
{
    /// <summary>
    /// Read-only map of dbo.MST_Department — organizational context for prioritization.
    /// </summary>
    [Table("MST_Department", Schema = "dbo")]
    public class Department
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [StringLength(100)]
        [Column(TypeName = "varchar")]
        public string Department_Description { get; set; }

        public virtual ICollection<UserMaster> Users { get; set; }

        public Department()
        {
            Users = new HashSet<UserMaster>();
        }
    }
}
