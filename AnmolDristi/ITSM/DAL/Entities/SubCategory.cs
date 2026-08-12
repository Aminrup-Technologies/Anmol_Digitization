using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnmolDristi.ITSM.DAL.Entities
{
    /// <summary>
    /// Maps to itsm.MST_SubCategory — second-tier taxonomy under Category.
    /// </summary>
    [Table("MST_SubCategory", Schema = "itsm")]
    public class SubCategory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public bool IsActive { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }

        public virtual ICollection<Item> Items { get; set; }

        public virtual ICollection<Ticket> Tickets { get; set; }

        public virtual ICollection<SupportMapping> SupportMappings { get; set; }

        public SubCategory()
        {
            Items = new HashSet<Item>();
            Tickets = new HashSet<Ticket>();
            SupportMappings = new HashSet<SupportMapping>();
            IsActive = true;
        }
    }
}
