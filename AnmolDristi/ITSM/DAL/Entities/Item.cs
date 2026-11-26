using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnmolDristi.ITSM.DAL.Entities
{
    /// <summary>
    /// Maps to itsm.MST_Item — optional Sub-Category under a Category.
    /// Deactivate with IsActive. Do not change SubCategoryId once a ticket references the row.
    /// </summary>
    [Table("MST_Item", Schema = "itsm")]
    public class Item
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int SubCategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// When: 2026-11-26
        /// What: Maps to itsm.MST_Item.SortOrder (INT NOT NULL).
        /// Why:  Display order among sub-categories that share a category.
        ///       Lower numbers appear first. Equal numbers tie-break by Id.
        ///       This table has no CompanyId. The column is added by
        ///       AnmolDristi/Database/itsm_catalog_display_order.sql.
        /// </summary>
        public int SortOrder { get; set; }

        public bool IsActive { get; set; }

        [ForeignKey("SubCategoryId")]
        public virtual SubCategory SubCategory { get; set; }

        public virtual ICollection<Ticket> Tickets { get; set; }

        public virtual ICollection<SupportMapping> SupportMappings { get; set; }

        public Item()
        {
            Tickets = new HashSet<Ticket>();
            SupportMappings = new HashSet<SupportMapping>();
            IsActive = true;
        }
    }
}
