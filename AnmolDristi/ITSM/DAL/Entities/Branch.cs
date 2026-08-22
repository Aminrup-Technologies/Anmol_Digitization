using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnmolDristi.ITSM.DAL.Entities
{
    /// <summary>
    /// Read-only map of dbo.MST_Branch — dispatch anchor for Intelligent Dispatch Matrix.
    /// When: 2026-08-22
    /// What: Added Branch_ID legacy business key for MST_UserMaster.BranchId resolution.
    /// Why: MST_UserMaster.BranchId stores Branch_ID (legacy value, e.g. 1966),
    ///      NOT MST_Branch.Id (identity PK, e.g. 4). TicketService resolves
    ///      Branch_ID → Id before assigning Tickets.BranchId.
    /// </summary>
    [Table("MST_Branch", Schema = "dbo")]
    public class Branch
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Legacy business branch identifier. MST_UserMaster.BranchId stores this value.
        /// TicketService resolves Branch_ID → Id for Tickets.BranchId FK.
        /// Verified unique: no two MST_Branch rows share the same Branch_ID.
        /// </summary>
        public int Branch_ID { get; set; }

        [StringLength(50)]
        [Column(TypeName = "varchar")]
        public string Branch_Description { get; set; }

        public virtual ICollection<Ticket> Tickets { get; set; }

        public virtual ICollection<SupportMapping> SupportMappings { get; set; }

        public virtual ICollection<UserMaster> Users { get; set; }

        public Branch()
        {
            Tickets = new HashSet<Ticket>();
            SupportMappings = new HashSet<SupportMapping>();
            Users = new HashSet<UserMaster>();
        }
    }
}
