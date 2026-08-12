using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnmolDristi.ITSM.DAL.Entities
{
    /// <summary>
    /// Read-only map of dbo.MST_Plant — primary routing context (Plant/Depot vs Corporate).
    /// </summary>
    [Table("MST_Plant", Schema = "dbo")]
    public class Plant
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [StringLength(100)]
        [Column(TypeName = "varchar")]
        public string PlantID_Description { get; set; }

        [StringLength(50)]
        [Column(TypeName = "varchar")]
        public string SAP_Code { get; set; }

        public virtual ICollection<Ticket> Tickets { get; set; }

        public virtual ICollection<UserMaster> Users { get; set; }

        public Plant()
        {
            Tickets = new HashSet<Ticket>();
            Users = new HashSet<UserMaster>();
        }
    }
}
