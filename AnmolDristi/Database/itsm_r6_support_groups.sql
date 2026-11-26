-- =============================================================================
-- ITSM R6 Support Groups — DBA prerequisite (NOT executed by the agent)
-- Branch: cursor/itsm-r6-group-assignment
-- Base:   Aug26_AILWorks_AmimnrupSupport
-- Purpose: Create group / member / mapping tables and Tickets.SupportGroupId.
-- Apply only when an owner authorizes a named environment.
-- =============================================================================

IF OBJECT_ID(N'itsm.MST_SupportGroup', N'U') IS NULL
BEGIN
    CREATE TABLE itsm.MST_SupportGroup
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MST_SupportGroup PRIMARY KEY,
        CompanyId INT NOT NULL,
        BranchId INT NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL,
        CreatedOn DATETIME NOT NULL,
        ModifiedOn DATETIME NULL,
        CreatedByCode NVARCHAR(50) NULL,
        ModifiedByCode NVARCHAR(50) NULL,
        CONSTRAINT FK_MST_SupportGroup_Branch FOREIGN KEY (BranchId) REFERENCES dbo.MST_Branch(Id)
    );

    CREATE INDEX IX_MST_SupportGroup_Company_Branch
        ON itsm.MST_SupportGroup (CompanyId, BranchId);
END
GO

IF OBJECT_ID(N'itsm.MST_SupportGroupMember', N'U') IS NULL
BEGIN
    CREATE TABLE itsm.MST_SupportGroupMember
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MST_SupportGroupMember PRIMARY KEY,
        SupportGroupId INT NOT NULL,
        EmployeeCode VARCHAR(50) NOT NULL,
        IsTeamLeader BIT NOT NULL,
        IsActive BIT NOT NULL,
        CreatedOn DATETIME NOT NULL,
        ModifiedOn DATETIME NULL,
        CONSTRAINT FK_MST_SupportGroupMember_Group FOREIGN KEY (SupportGroupId)
            REFERENCES itsm.MST_SupportGroup(Id),
        CONSTRAINT FK_MST_SupportGroupMember_User FOREIGN KEY (EmployeeCode)
            REFERENCES dbo.MST_UserMaster(EmployeeCode)
    );

    CREATE UNIQUE INDEX UX_MST_SupportGroupMember_Group_Employee
        ON itsm.MST_SupportGroupMember (SupportGroupId, EmployeeCode);

    CREATE UNIQUE INDEX UIX_MST_SupportGroupMember_OneActiveLeader
        ON itsm.MST_SupportGroupMember (SupportGroupId)
        WHERE IsActive = 1 AND IsTeamLeader = 1;
END
GO

IF OBJECT_ID(N'itsm.MST_SupportGroupMapping', N'U') IS NULL
BEGIN
    CREATE TABLE itsm.MST_SupportGroupMapping
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MST_SupportGroupMapping PRIMARY KEY,
        CompanyId INT NOT NULL,
        BranchId INT NOT NULL,
        CategoryId INT NOT NULL,
        SubCategoryId INT NULL,
        SupportGroupId INT NOT NULL,
        IsActive BIT NOT NULL,
        CreatedOn DATETIME NOT NULL,
        ModifiedOn DATETIME NULL,
        CONSTRAINT FK_MST_SupportGroupMapping_Branch FOREIGN KEY (BranchId)
            REFERENCES dbo.MST_Branch(Id),
        CONSTRAINT FK_MST_SupportGroupMapping_Category FOREIGN KEY (CategoryId)
            REFERENCES itsm.MST_Category(Id),
        CONSTRAINT FK_MST_SupportGroupMapping_SubCategory FOREIGN KEY (SubCategoryId)
            REFERENCES itsm.MST_SubCategory(Id),
        CONSTRAINT FK_MST_SupportGroupMapping_Group FOREIGN KEY (SupportGroupId)
            REFERENCES itsm.MST_SupportGroup(Id)
    );

    CREATE INDEX IX_MST_SupportGroupMapping_Company_Branch
        ON itsm.MST_SupportGroupMapping (CompanyId, BranchId);

    CREATE UNIQUE INDEX UIX_MST_SupportGroupMapping_ActiveParent
        ON itsm.MST_SupportGroupMapping (CompanyId, BranchId, CategoryId)
        WHERE IsActive = 1 AND SubCategoryId IS NULL;

    CREATE UNIQUE INDEX UIX_MST_SupportGroupMapping_ActiveCategory
        ON itsm.MST_SupportGroupMapping (CompanyId, BranchId, SubCategoryId)
        WHERE IsActive = 1 AND SubCategoryId IS NOT NULL;
END
GO

IF COL_LENGTH(N'itsm.Tickets', N'SupportGroupId') IS NULL
BEGIN
    ALTER TABLE itsm.Tickets ADD SupportGroupId INT NULL;
    ALTER TABLE itsm.Tickets WITH CHECK
        ADD CONSTRAINT FK_Tickets_SupportGroup
        FOREIGN KEY (SupportGroupId) REFERENCES itsm.MST_SupportGroup(Id);
    CREATE INDEX IX_Tickets_SupportGroupId ON itsm.Tickets (SupportGroupId);
END
GO
