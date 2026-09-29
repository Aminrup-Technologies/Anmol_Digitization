using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnmolDristi.ITSM.DAL.Entities
{
    /// <summary>
    /// Maps to itsm.MST_PriorityMatrix — Impact × Urgency → PriorityId lookup.
    /// </summary>
    [Table("MST_PriorityMatrix", Schema = "itsm")]
    public class PriorityMatrix
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int ImpactId { get; set; }

        [Required]
        public int UrgencyId { get; set; }

        [Required]
        public int PriorityId { get; set; }

        [ForeignKey("ImpactId")]
        public virtual Impact Impact { get; set; }

        [ForeignKey("UrgencyId")]
        public virtual Urgency Urgency { get; set; }

        [ForeignKey("PriorityId")]
        public virtual Priority Priority { get; set; }
    }
}
