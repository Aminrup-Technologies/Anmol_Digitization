using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnmolDristi.ITSM.DAL.Entities
{
    /// <summary>
    /// Maps to itsm.MST_SupportMapping — branch + taxonomy → default support agent (Intelligent Dispatch).
    /// </summary>
    [Table("MST_SupportMapping", Schema = "itsm")]
    public class SupportMapping
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// FK → dbo.MST_Branch.Id (required).
        /// </summary>
        [Required]
        public int BranchId { get; set; }

        public int? CategoryId { get; set; }

        public int? SubCategoryId { get; set; }

        public int? ItemId { get; set; }

        /// <summary>
        /// FK → dbo.MST_UserMaster.EmployeeCode.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string SupportAgentCode { get; set; }

        /// <summary>
        /// 1 = branch-wide fallback row (no category set).
        /// </summary>
        public bool IsBranchDefault { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime? ModifiedOn { get; set; }

        [ForeignKey("BranchId")]
        public virtual Branch Branch { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }

        [ForeignKey("SubCategoryId")]
        public virtual SubCategory SubCategory { get; set; }

        [ForeignKey("ItemId")]
        public virtual Item Item { get; set; }

        [ForeignKey("SupportAgentCode")]
        public virtual UserMaster SupportAgent { get; set; }

        public SupportMapping()
        {
            IsActive = true;
        }
    }
}
