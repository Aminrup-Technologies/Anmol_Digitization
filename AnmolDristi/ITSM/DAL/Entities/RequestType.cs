using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnmolDristi.ITSM.DAL.Entities
{
    /// <summary>
    /// Maps to itsm.MST_RequestType — IR / SR / CR request families and ticket-number prefixes.
    /// </summary>
    [Table("MST_RequestType", Schema = "itsm")]
    public class RequestType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        public string Code { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(2)]
        [Column(TypeName = "char")]
        public string Prefix { get; set; }

        public bool RequiresManagerApproval { get; set; }

        public bool RequiresItHeadApproval { get; set; }

        public bool IsActive { get; set; }

        public virtual ICollection<Ticket> Tickets { get; set; }

        public RequestType()
        {
            Tickets = new HashSet<Ticket>();
            IsActive = true;
        }
    }
}
