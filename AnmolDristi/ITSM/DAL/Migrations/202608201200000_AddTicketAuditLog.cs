namespace AnmolDristi.ITSM.DAL.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddTicketAuditLog : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "itsm.TicketAuditLog",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TicketId = c.Int(nullable: false),
                        CompanyId = c.Int(nullable: false),
                        ActionType = c.String(nullable: false, maxLength: 50),
                        OldValue = c.String(maxLength: 200),
                        NewValue = c.String(maxLength: 200),
                        Detail = c.String(maxLength: 1000),
                        ActorCode = c.String(nullable: false, maxLength: 50, unicode: false),
                        ActionedOn = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("itsm.Tickets", t => t.TicketId)
                .Index(t => t.TicketId)
                .Index(t => t.CompanyId)
                .Index(t => new { t.TicketId, t.CompanyId })
                .Index(t => t.ActionedOn);
        }

        public override void Down()
        {
            DropForeignKey("itsm.TicketAuditLog", "TicketId", "itsm.Tickets");
            DropIndex("itsm.TicketAuditLog", new[] { "ActionedOn" });
            DropIndex("itsm.TicketAuditLog", new[] { "TicketId", "CompanyId" });
            DropIndex("itsm.TicketAuditLog", new[] { "CompanyId" });
            DropIndex("itsm.TicketAuditLog", new[] { "TicketId" });
            DropTable("itsm.TicketAuditLog");
        }
    }
}
