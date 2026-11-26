namespace AnmolDristi.ITSM.DAL.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddSupportGroups : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "itsm.MST_SupportGroup",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CompanyId = c.Int(nullable: false),
                        BranchId = c.Int(nullable: false),
                        Name = c.String(nullable: false, maxLength: 100),
                        IsActive = c.Boolean(nullable: false),
                        CreatedOn = c.DateTime(nullable: false),
                        ModifiedOn = c.DateTime(),
                        CreatedByCode = c.String(maxLength: 50),
                        ModifiedByCode = c.String(maxLength: 50),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.MST_Branch", t => t.BranchId)
                .Index(t => t.BranchId)
                .Index(t => new { t.CompanyId, t.BranchId }, name: "IX_MST_SupportGroup_Company_Branch");

            CreateTable(
                "itsm.MST_SupportGroupMember",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        SupportGroupId = c.Int(nullable: false),
                        EmployeeCode = c.String(nullable: false, maxLength: 50, unicode: false),
                        IsTeamLeader = c.Boolean(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        CreatedOn = c.DateTime(nullable: false),
                        ModifiedOn = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("itsm.MST_SupportGroup", t => t.SupportGroupId)
                .ForeignKey("dbo.MST_UserMaster", t => t.EmployeeCode)
                .Index(t => t.SupportGroupId)
                .Index(t => t.EmployeeCode)
                .Index(t => new { t.SupportGroupId, t.EmployeeCode }, unique: true, name: "UX_MST_SupportGroupMember_Group_Employee");

            CreateTable(
                "itsm.MST_SupportGroupMapping",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CompanyId = c.Int(nullable: false),
                        BranchId = c.Int(nullable: false),
                        CategoryId = c.Int(nullable: false),
                        SubCategoryId = c.Int(),
                        SupportGroupId = c.Int(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        CreatedOn = c.DateTime(nullable: false),
                        ModifiedOn = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.MST_Branch", t => t.BranchId)
                .ForeignKey("itsm.MST_Category", t => t.CategoryId)
                .ForeignKey("itsm.MST_SubCategory", t => t.SubCategoryId)
                .ForeignKey("itsm.MST_SupportGroup", t => t.SupportGroupId)
                .Index(t => t.BranchId)
                .Index(t => t.CategoryId)
                .Index(t => t.SubCategoryId)
                .Index(t => t.SupportGroupId)
                .Index(t => new { t.CompanyId, t.BranchId }, name: "IX_MST_SupportGroupMapping_Company_Branch");

            AddColumn("itsm.Tickets", "SupportGroupId", c => c.Int());
            CreateIndex("itsm.Tickets", "SupportGroupId");
            AddForeignKey("itsm.Tickets", "SupportGroupId", "itsm.MST_SupportGroup", "Id");

            Sql(@"
CREATE UNIQUE INDEX UIX_MST_SupportGroupMember_OneActiveLeader
ON itsm.MST_SupportGroupMember (SupportGroupId)
WHERE IsActive = 1 AND IsTeamLeader = 1;

CREATE UNIQUE INDEX UIX_MST_SupportGroupMapping_ActiveParent
ON itsm.MST_SupportGroupMapping (CompanyId, BranchId, CategoryId)
WHERE IsActive = 1 AND SubCategoryId IS NULL;

CREATE UNIQUE INDEX UIX_MST_SupportGroupMapping_ActiveCategory
ON itsm.MST_SupportGroupMapping (CompanyId, BranchId, SubCategoryId)
WHERE IsActive = 1 AND SubCategoryId IS NOT NULL;
");
        }

        public override void Down()
        {
            Sql(@"
DROP INDEX UIX_MST_SupportGroupMapping_ActiveCategory ON itsm.MST_SupportGroupMapping;
DROP INDEX UIX_MST_SupportGroupMapping_ActiveParent ON itsm.MST_SupportGroupMapping;
DROP INDEX UIX_MST_SupportGroupMember_OneActiveLeader ON itsm.MST_SupportGroupMember;
");

            DropForeignKey("itsm.Tickets", "SupportGroupId", "itsm.MST_SupportGroup");
            DropIndex("itsm.Tickets", new[] { "SupportGroupId" });
            DropColumn("itsm.Tickets", "SupportGroupId");

            DropForeignKey("itsm.MST_SupportGroupMapping", "SupportGroupId", "itsm.MST_SupportGroup");
            DropForeignKey("itsm.MST_SupportGroupMapping", "SubCategoryId", "itsm.MST_SubCategory");
            DropForeignKey("itsm.MST_SupportGroupMapping", "CategoryId", "itsm.MST_Category");
            DropForeignKey("itsm.MST_SupportGroupMapping", "BranchId", "dbo.MST_Branch");
            DropIndex("itsm.MST_SupportGroupMapping", "IX_MST_SupportGroupMapping_Company_Branch");
            DropIndex("itsm.MST_SupportGroupMapping", new[] { "SupportGroupId" });
            DropIndex("itsm.MST_SupportGroupMapping", new[] { "SubCategoryId" });
            DropIndex("itsm.MST_SupportGroupMapping", new[] { "CategoryId" });
            DropIndex("itsm.MST_SupportGroupMapping", new[] { "BranchId" });
            DropTable("itsm.MST_SupportGroupMapping");

            DropForeignKey("itsm.MST_SupportGroupMember", "EmployeeCode", "dbo.MST_UserMaster");
            DropForeignKey("itsm.MST_SupportGroupMember", "SupportGroupId", "itsm.MST_SupportGroup");
            DropIndex("itsm.MST_SupportGroupMember", "UX_MST_SupportGroupMember_Group_Employee");
            DropIndex("itsm.MST_SupportGroupMember", new[] { "EmployeeCode" });
            DropIndex("itsm.MST_SupportGroupMember", new[] { "SupportGroupId" });
            DropTable("itsm.MST_SupportGroupMember");

            DropForeignKey("itsm.MST_SupportGroup", "BranchId", "dbo.MST_Branch");
            DropIndex("itsm.MST_SupportGroup", "IX_MST_SupportGroup_Company_Branch");
            DropIndex("itsm.MST_SupportGroup", new[] { "BranchId" });
            DropTable("itsm.MST_SupportGroup");
        }
    }
}
