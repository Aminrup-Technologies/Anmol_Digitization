namespace AnmolDristi.ITSM.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddBranchLegacyId : DbMigration
    {
        public override void Up()
        {
            // Existing legacy columns are already present in the database.
            // This migration synchronizes EF6 migration metadata with the
            // current entity model; no physical schema changes are required.
        }

        public override void Down()
        {
            // Intentionally empty.
            // Existing legacy columns must not be dropped by rollback.
        }
    }
}
