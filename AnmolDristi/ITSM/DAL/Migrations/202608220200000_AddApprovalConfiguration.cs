namespace AnmolDristi.ITSM.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddApprovalConfiguration : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "itsm.MST_ApprovalConfiguration",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ItHeadApproverCode = c.String(nullable: false, maxLength: 50, unicode: false),
                        IsActive = c.Boolean(nullable: false),
                        CreatedOn = c.DateTime(nullable: false),
                        ModifiedOn = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.MST_UserMaster", t => t.ItHeadApproverCode)
                .Index(t => t.ItHeadApproverCode);
            
            // Filtered unique index: only one active IT Head configuration may exist.
            // SQL Server supports filtered indexes; EF6 migration SQL handles this directly.
            Sql(@"
                CREATE UNIQUE INDEX UIX_MST_ApprovalConfiguration_IsActive
                ON itsm.MST_ApprovalConfiguration (IsActive)
                WHERE IsActive = 1;
            ");
        }
        
        public override void Down()
        {
            DropIndex("itsm.MST_ApprovalConfiguration", new[] { "ItHeadApproverCode" });
            DropTable("itsm.MST_ApprovalConfiguration");
        }
    }
}
