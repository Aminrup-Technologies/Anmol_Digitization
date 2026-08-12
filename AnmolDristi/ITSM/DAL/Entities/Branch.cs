using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnmolDristi.ITSM.DAL.Entities
{
    /// <summary>
    /// Read-only map of dbo.MST_Branch — dispatch anchor for Intelligent Dispatch Matrix.
    /// </summary>
    [Table("MST_Branch", Schema = "dbo")]
    public class Branch
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [StringLength(50)]
        [Column(TypeName = "varchar")]
        public string Branch_Description { get; set; }

        public virtual ICollection<Ticket> Tickets { get; set; }

        public virtual ICollection<SupportMapping> SupportMappings { get; set; }

        public virtual ICollection<UserMaster> Users { get; set; }

        public Branch()
        {
            Tickets = new HashSet<Ticket>();
            SupportMappings = new HashSet<SupportMapping>();
            Users = new HashSet<UserMaster>();
        }
    }
}
