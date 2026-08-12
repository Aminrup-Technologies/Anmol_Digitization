using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnmolDristi.ITSM.DAL.Entities
{
    /// <summary>
    /// Maps to itsm.MST_Urgency — user-selected urgency (Low / Medium / High).
    /// </summary>
    [Table("MST_Urgency", Schema = "itsm")]
    public class Urgency
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string Code { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; }

        public virtual ICollection<Ticket> Tickets { get; set; }

        public virtual ICollection<PriorityMatrix> PriorityMatrices { get; set; }

        public Urgency()
        {
            Tickets = new HashSet<Ticket>();
            PriorityMatrices = new HashSet<PriorityMatrix>();
            IsActive = true;
        }
    }
}
