namespace AnmolDristi.ITSM.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialItsmSchema : DbMigration
    {
        public override void Up()
        {          
            CreateTable(
                "itsm.MST_SupportMapping",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        BranchId = c.Int(nullable: false),
                        CategoryId = c.Int(),
                        SubCategoryId = c.Int(),
                        ItemId = c.Int(),
                        SupportAgentCode = c.String(nullable: false, maxLength: 50, unicode: false),
                        IsBranchDefault = c.Boolean(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        CreatedOn = c.DateTime(nullable: false),
                        ModifiedOn = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.MST_Branch", t => t.BranchId)
                .ForeignKey("itsm.MST_Item", t => t.ItemId)
                .ForeignKey("itsm.MST_SubCategory", t => t.SubCategoryId)
                .ForeignKey("itsm.MST_Category", t => t.CategoryId)
                .ForeignKey("dbo.MST_UserMaster", t => t.SupportAgentCode)
                .Index(t => t.BranchId)
                .Index(t => t.CategoryId)
                .Index(t => t.SubCategoryId)
                .Index(t => t.ItemId)
                .Index(t => t.SupportAgentCode);
            
            CreateTable(
                "itsm.MST_Category",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "itsm.MST_SubCategory",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CategoryId = c.Int(nullable: false),
                        Name = c.String(nullable: false, maxLength: 100),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("itsm.MST_Category", t => t.CategoryId)
                .Index(t => t.CategoryId);
            
            CreateTable(
                "itsm.MST_Item",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        SubCategoryId = c.Int(nullable: false),
                        Name = c.String(nullable: false, maxLength: 100),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("itsm.MST_SubCategory", t => t.SubCategoryId)
                .Index(t => t.SubCategoryId);
            
            CreateTable(
                "itsm.Tickets",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TicketNumber = c.String(nullable: false, maxLength: 10, unicode: false),
                        RequestTypeId = c.Int(nullable: false),
                        Title = c.String(nullable: false, maxLength: 200),
                        Description = c.String(),
                        RequesterCode = c.String(nullable: false, maxLength: 50, unicode: false),
                        AssigneeCode = c.String(maxLength: 50, unicode: false),
                        PlantId = c.Int(),
                        BranchId = c.Int(nullable: false),
                        CategoryId = c.Int(nullable: false),
                        SubCategoryId = c.Int(nullable: false),
                        ItemId = c.Int(),
                        ImpactId = c.Int(nullable: false),
                        UrgencyId = c.Int(nullable: false),
                        PriorityId = c.Int(nullable: false),
                        StatusId = c.Int(nullable: false),
                        ResolutionText = c.String(),
                        CsatScore = c.Byte(),
                        CsatFeedback = c.String(maxLength: 500),
                        CreatedOn = c.DateTime(nullable: false),
                        ResolvedOn = c.DateTime(),
                        ClosedOn = c.DateTime(),
                        CreatedByCode = c.String(nullable: false, maxLength: 50),
                        ModifiedOn = c.DateTime(),
                        ModifiedByCode = c.String(maxLength: 50),
                        RowVersion = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.MST_UserMaster", t => t.AssigneeCode)
                .ForeignKey("dbo.MST_Plant", t => t.PlantId)
                .ForeignKey("dbo.MST_UserMaster", t => t.RequesterCode)
                .ForeignKey("dbo.MST_Branch", t => t.BranchId)
                .ForeignKey("itsm.MST_Category", t => t.CategoryId)
                .ForeignKey("itsm.MST_Priority", t => t.PriorityId)
                .ForeignKey("itsm.MST_Urgency", t => t.UrgencyId)
                .ForeignKey("itsm.MST_Impact", t => t.ImpactId)
                .ForeignKey("itsm.MST_Item", t => t.ItemId)
                .ForeignKey("itsm.MST_RequestType", t => t.RequestTypeId)
                .ForeignKey("itsm.MST_TicketStatus", t => t.StatusId)
                .ForeignKey("itsm.MST_SubCategory", t => t.SubCategoryId)
                .Index(t => t.RequestTypeId)
                .Index(t => t.RequesterCode)
                .Index(t => t.AssigneeCode)
                .Index(t => t.PlantId)
                .Index(t => t.BranchId)
                .Index(t => t.CategoryId)
                .Index(t => t.SubCategoryId)
                .Index(t => t.ItemId)
                .Index(t => t.ImpactId)
                .Index(t => t.UrgencyId)
                .Index(t => t.PriorityId)
                .Index(t => t.StatusId);
            
            CreateTable(
                "itsm.TicketApprovals",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TicketId = c.Int(nullable: false),
                        ApprovalLevel = c.String(nullable: false, maxLength: 20),
                        ApproverCode = c.String(nullable: false, maxLength: 50, unicode: false),
                        ApprovalStatus = c.String(nullable: false, maxLength: 20),
                        Comments = c.String(maxLength: 500),
                        ActionedOn = c.DateTime(),
                        CreatedOn = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.MST_UserMaster", t => t.ApproverCode)
                .ForeignKey("itsm.Tickets", t => t.TicketId)
                .Index(t => t.TicketId)
                .Index(t => t.ApproverCode);
            
            CreateTable(
                "itsm.TicketAttachments",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TicketId = c.Int(nullable: false),
                        FileName = c.String(nullable: false, maxLength: 255),
                        FilePath = c.String(nullable: false, maxLength: 500),
                        UploadedByCode = c.String(nullable: false, maxLength: 50, unicode: false),
                        UploadedOn = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("itsm.Tickets", t => t.TicketId)
                .ForeignKey("dbo.MST_UserMaster", t => t.UploadedByCode)
                .Index(t => t.TicketId)
                .Index(t => t.UploadedByCode);
            
            CreateTable(
                "itsm.TicketComments",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TicketId = c.Int(nullable: false),
                        CommentText = c.String(nullable: false),
                        CreatedByCode = c.String(nullable: false, maxLength: 50, unicode: false),
                        CreatedOn = c.DateTime(nullable: false),
                        IsInternal = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.MST_UserMaster", t => t.CreatedByCode)
                .ForeignKey("itsm.Tickets", t => t.TicketId)
                .Index(t => t.TicketId)
                .Index(t => t.CreatedByCode);
            
            CreateTable(
                "itsm.MST_Impact",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(nullable: false, maxLength: 20),
                        Name = c.String(nullable: false, maxLength: 100),
                        SortOrder = c.Int(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "itsm.MST_PriorityMatrix",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ImpactId = c.Int(nullable: false),
                        UrgencyId = c.Int(nullable: false),
                        PriorityId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("itsm.MST_Impact", t => t.ImpactId)
                .ForeignKey("itsm.MST_Priority", t => t.PriorityId)
                .ForeignKey("itsm.MST_Urgency", t => t.UrgencyId)
                .Index(t => t.ImpactId)
                .Index(t => t.UrgencyId)
                .Index(t => t.PriorityId);
            
            CreateTable(
                "itsm.MST_Priority",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(nullable: false, maxLength: 20),
                        Name = c.String(nullable: false, maxLength: 100),
                        SlaResolutionHours = c.Int(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "itsm.MST_Urgency",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(nullable: false, maxLength: 20),
                        Name = c.String(nullable: false, maxLength: 100),
                        SortOrder = c.Int(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "itsm.MST_RequestType",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(nullable: false, maxLength: 10),
                        Name = c.String(nullable: false, maxLength: 100),
                        Prefix = c.String(nullable: false, maxLength: 2, fixedLength: true, unicode: false),
                        RequiresManagerApproval = c.Boolean(nullable: false),
                        RequiresItHeadApproval = c.Boolean(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "itsm.MST_TicketStatus",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                        IsTerminal = c.Boolean(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
        }
        
        public override void Down()
        {
            DropForeignKey("itsm.MST_SupportMapping", "SupportAgentCode", "dbo.MST_UserMaster");
            DropForeignKey("itsm.MST_SupportMapping", "CategoryId", "itsm.MST_Category");
            DropForeignKey("itsm.MST_SupportMapping", "SubCategoryId", "itsm.MST_SubCategory");
            DropForeignKey("itsm.Tickets", "SubCategoryId", "itsm.MST_SubCategory");
            DropForeignKey("itsm.Tickets", "StatusId", "itsm.MST_TicketStatus");
            DropForeignKey("itsm.Tickets", "RequestTypeId", "itsm.MST_RequestType");
            DropForeignKey("itsm.Tickets", "ItemId", "itsm.MST_Item");
            DropForeignKey("itsm.Tickets", "ImpactId", "itsm.MST_Impact");
            DropForeignKey("itsm.Tickets", "UrgencyId", "itsm.MST_Urgency");
            DropForeignKey("itsm.MST_PriorityMatrix", "UrgencyId", "itsm.MST_Urgency");
            DropForeignKey("itsm.Tickets", "PriorityId", "itsm.MST_Priority");
            DropForeignKey("itsm.MST_PriorityMatrix", "PriorityId", "itsm.MST_Priority");
            DropForeignKey("itsm.MST_PriorityMatrix", "ImpactId", "itsm.MST_Impact");
            DropForeignKey("itsm.TicketComments", "TicketId", "itsm.Tickets");
            DropForeignKey("itsm.TicketComments", "CreatedByCode", "dbo.MST_UserMaster");
            DropForeignKey("itsm.Tickets", "CategoryId", "itsm.MST_Category");
            DropForeignKey("itsm.Tickets", "BranchId", "dbo.MST_Branch");
            DropForeignKey("itsm.TicketAttachments", "UploadedByCode", "dbo.MST_UserMaster");
            DropForeignKey("itsm.TicketAttachments", "TicketId", "itsm.Tickets");
            DropForeignKey("itsm.TicketApprovals", "TicketId", "itsm.Tickets");
            DropForeignKey("itsm.TicketApprovals", "ApproverCode", "dbo.MST_UserMaster");
            DropForeignKey("itsm.Tickets", "RequesterCode", "dbo.MST_UserMaster");
            DropForeignKey("dbo.MST_UserMaster", "PlantId", "dbo.MST_Plant");
            DropForeignKey("itsm.Tickets", "PlantId", "dbo.MST_Plant");
            DropForeignKey("dbo.MST_UserMaster", "DepartmentId", "dbo.MST_Department");
            DropForeignKey("dbo.MST_UserMaster", "BranchId", "dbo.MST_Branch");
            DropForeignKey("itsm.Tickets", "AssigneeCode", "dbo.MST_UserMaster");
            DropForeignKey("itsm.MST_SupportMapping", "ItemId", "itsm.MST_Item");
            DropForeignKey("itsm.MST_Item", "SubCategoryId", "itsm.MST_SubCategory");
            DropForeignKey("itsm.MST_SubCategory", "CategoryId", "itsm.MST_Category");
            DropForeignKey("itsm.MST_SupportMapping", "BranchId", "dbo.MST_Branch");
            DropIndex("itsm.MST_PriorityMatrix", new[] { "PriorityId" });
            DropIndex("itsm.MST_PriorityMatrix", new[] { "UrgencyId" });
            DropIndex("itsm.MST_PriorityMatrix", new[] { "ImpactId" });
            DropIndex("itsm.TicketComments", new[] { "CreatedByCode" });
            DropIndex("itsm.TicketComments", new[] { "TicketId" });
            DropIndex("itsm.TicketAttachments", new[] { "UploadedByCode" });
            DropIndex("itsm.TicketAttachments", new[] { "TicketId" });
            DropIndex("itsm.TicketApprovals", new[] { "ApproverCode" });
            DropIndex("itsm.TicketApprovals", new[] { "TicketId" });
            DropIndex("itsm.Tickets", new[] { "StatusId" });
            DropIndex("itsm.Tickets", new[] { "PriorityId" });
            DropIndex("itsm.Tickets", new[] { "UrgencyId" });
            DropIndex("itsm.Tickets", new[] { "ImpactId" });
            DropIndex("itsm.Tickets", new[] { "ItemId" });
            DropIndex("itsm.Tickets", new[] { "SubCategoryId" });
            DropIndex("itsm.Tickets", new[] { "CategoryId" });
            DropIndex("itsm.Tickets", new[] { "BranchId" });
            DropIndex("itsm.Tickets", new[] { "PlantId" });
            DropIndex("itsm.Tickets", new[] { "AssigneeCode" });
            DropIndex("itsm.Tickets", new[] { "RequesterCode" });
            DropIndex("itsm.Tickets", new[] { "RequestTypeId" });
            DropIndex("itsm.MST_Item", new[] { "SubCategoryId" });
            DropIndex("itsm.MST_SubCategory", new[] { "CategoryId" });
            DropIndex("itsm.MST_SupportMapping", new[] { "SupportAgentCode" });
            DropIndex("itsm.MST_SupportMapping", new[] { "ItemId" });
            DropIndex("itsm.MST_SupportMapping", new[] { "SubCategoryId" });
            DropIndex("itsm.MST_SupportMapping", new[] { "CategoryId" });
            DropIndex("itsm.MST_SupportMapping", new[] { "BranchId" });
            DropTable("itsm.MST_TicketStatus");
            DropTable("itsm.MST_RequestType");
            DropTable("itsm.MST_Urgency");
            DropTable("itsm.MST_Priority");
            DropTable("itsm.MST_PriorityMatrix");
            DropTable("itsm.MST_Impact");
            DropTable("itsm.TicketComments");
            DropTable("itsm.TicketAttachments");
            DropTable("itsm.TicketApprovals");
            DropTable("itsm.Tickets");
            DropTable("itsm.MST_Item");
            DropTable("itsm.MST_SubCategory");
            DropTable("itsm.MST_Category");
            DropTable("itsm.MST_SupportMapping");
        }
    }
}
