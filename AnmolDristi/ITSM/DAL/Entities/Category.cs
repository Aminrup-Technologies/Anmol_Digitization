using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnmolDristi.ITSM.DAL.Entities
{
    /// <summary>
    /// Maps to itsm.MST_Category — top-level taxonomy (Hardware, Software, Network, etc.).
    /// </summary>
    [Table("MST_Category", Schema = "itsm")]
    public class Category
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public bool IsActive { get; set; }

        public virtual ICollection<SubCategory> SubCategories { get; set; }

        public virtual ICollection<Ticket> Tickets { get; set; }

        public virtual ICollection<SupportMapping> SupportMappings { get; set; }

        public Category()
        {
            SubCategories = new HashSet<SubCategory>();
            Tickets = new HashSet<Ticket>();
            SupportMappings = new HashSet<SupportMapping>();
            IsActive = true;
        }
    }
}
