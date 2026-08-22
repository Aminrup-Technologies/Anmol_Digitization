// ========================================================================
// When: 2026-08-21
// Why:  Ticket tenant must be an immutable snapshot. Requester.CompanyId is
//       live/mutable (HRMS) and is not historical ticket ownership.
// What: Adds nullable itsm.Tickets.CompanyId (no FK). Does not backfill.
// ========================================================================

namespace AnmolDristi.ITSM.DAL.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddTicketCompanyId : DbMigration
    {
        public override void Up()
        {
            // No FK: MST_UserMaster.CompanyId has no FK to MST_Company in the
            // repository schema extract, and MST_Company is not EF-mapped.
            // Nullable: existing tickets cannot be deterministically backfilled
            // from current Requester.CompanyId (HRMS can change it after create).
            AddColumn("itsm.Tickets", "CompanyId", c => c.Int());
            CreateIndex("itsm.Tickets", "CompanyId");
        }

        public override void Down()
        {
            DropIndex("itsm.Tickets", new[] { "CompanyId" });
            DropColumn("itsm.Tickets", "CompanyId");
        }
    }
}
