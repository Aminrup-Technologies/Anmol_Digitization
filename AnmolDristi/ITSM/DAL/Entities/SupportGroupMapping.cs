using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnmolDristi.ITSM.DAL.Entities
{
    /// <summary>
    /// Routes Parent Category (CategoryId, SubCategoryId null) or Category
    /// (SubCategoryId set) to a support group for a company and branch.
    /// </summary>
    [Table("MST_SupportGroupMapping", Schema = "itsm")]
    public class SupportGroupMapping
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }

        [Required]
        public int BranchId { get; set; }

        /// <summary>
        /// Parent Category (MST_Category). Always stored; required for both levels.
        /// </summary>
        [Required]
        public int CategoryId { get; set; }

        /// <summary>
        /// UI Category (MST_SubCategory). Null = Parent Category mapping.
        /// </summary>
        public int? SubCategoryId { get; set; }

        [Required]
        public int SupportGroupId { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [Required]
        public DateTime CreatedOn { get; set; }

        public DateTime? ModifiedOn { get; set; }

        [ForeignKey("BranchId")]
        public virtual Branch Branch { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }

        [ForeignKey("SubCategoryId")]
        public virtual SubCategory SubCategory { get; set; }

        [ForeignKey("SupportGroupId")]
        public virtual SupportGroup SupportGroup { get; set; }
    }
}
