// ========================================================================
// When: 2026-08-21
// What: Read-only map of dbo.MST_Company for ticket tenant display name.
// Why: Ticket.CompanyId snapshots MST_UserMaster.CompanyId (= MST_Company.Company_ID).
//      No FK from Tickets; this entity is lookup-only.
// ========================================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnmolDristi.ITSM.DAL.Entities
{
    [Table("MST_Company", Schema = "dbo")]
    public class Company
    {
        [Key]
        public int Id { get; set; }

        [StringLength(100)]
        [Column("Company_Description", TypeName = "varchar")]
        public string Company_Description { get; set; }

        [Column("Company_ID")]
        public int Company_ID { get; set; }
    }
}
