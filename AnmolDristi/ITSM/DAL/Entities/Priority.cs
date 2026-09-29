using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnmolDristi.ITSM.DAL.Entities
{
    /// <summary>
    /// Maps to itsm.MST_Priority — auto-assigned from Impact × Urgency matrix (P1–P4).
    /// </summary>
    [Table("MST_Priority", Schema = "itsm")]
    public class Priority
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

        public int SlaResolutionHours { get; set; }

        public bool IsActive { get; set; }

        public virtual ICollection<Ticket> Tickets { get; set; }

        public virtual ICollection<PriorityMatrix> PriorityMatrices { get; set; }

        public Priority()
        {
            Tickets = new HashSet<Ticket>();
            PriorityMatrices = new HashSet<PriorityMatrix>();
            IsActive = true;
        }
    }
}
