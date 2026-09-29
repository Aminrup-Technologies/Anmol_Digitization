/*
    Object:      [dbo].[MST_UserMaster]
    Type:        TABLE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE TABLE [dbo].[MST_UserMaster] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [EmployeeCode] varchar(50) NOT NULL,
    [EmployeeName] varchar(100) NOT NULL,
    [BranchId] int NOT NULL,
    [PlantId] int NOT NULL,
    [EmployeeStatus] varchar(50) NULL,
    [CompanyId] int NULL,
    [DepartmentId] int NULL,
    [GradeId] int NULL,
    [SubDepartmentId] int NULL,
    [DOJ] date NULL,
    [CategoryId] int NULL,
    [Email] varchar(100) NULL,
    [Mobile] varchar(10) NULL,
    [Gender] varchar(50) NULL,
    [RegionId] int NULL,
    [DivisionId] int NULL,
    [CreatedDate] smalldatetime NULL CONSTRAINT [DF_MST_UserMaster_CreatedDate_IST] DEFAULT (dateadd(minute,(330),sysutcdatetime())),
    [CreatedBy] varchar(50) NULL,
    [CreationMode] varchar(50) NULL,
    [Password] varchar(50) NULL,
    [LastLoginDate] smalldatetime NULL,
    [LastLogoutDate] smalldatetime NULL,
    [Module] varchar(50) NULL,
    [SQ1] varchar(200) NULL,
    [SQAns1] varchar(200) NULL,
    [SQ2] varchar(200) NULL,
    [SQAns2] varchar(200) NULL,
    [Pass_UpdateDate] smalldatetime NULL,
    [PassUpdatedByName] varchar(50) NULL,
    [PassUpdatedByWrk] varchar(50) NULL,
    [PasswordExpiry] smalldatetime NULL,
    [LastLogout] smalldatetime NULL,
    [ReportingManager] varchar(100) NULL,
    [ReportingManagerCode] varchar(50) NULL,
    [ReportingManagersManagerName] varchar(100) NULL,
    [ReportingManagersManagerCode] varchar(50) NULL,
    [Designation_ID] int NULL,
    [PasswordHash] nvarchar(400) NULL,
    [PasswordSalt] nvarchar(200) NULL,
    [PasswordIterations] int NULL,
    [DPDPConsentGiven] bit NOT NULL CONSTRAINT [DF__MST_UserM__DPDPC__4A78EF25] DEFAULT ((0)),
    [DPDPConsentDate] datetime NULL
);
GO
ALTER TABLE [dbo].[MST_UserMaster] ADD CONSTRAINT [PK_MST_UserMaster] PRIMARY KEY CLUSTERED ([EmployeeCode] ASC);
ALTER TABLE [dbo].[MST_UserMaster] ADD CONSTRAINT [UQ_MST_UserMaster_Id] UNIQUE ([Id] ASC);
CREATE NONCLUSTERED INDEX [IX_MST_UserMaster_Emp] ON [dbo].[MST_UserMaster] ([EmployeeCode] ASC) INCLUDE ([Email], [EmployeeName]);
CREATE NONCLUSTERED INDEX [IX_MST_UserMaster_EmpCode] ON [dbo].[MST_UserMaster] ([EmployeeCode] ASC);
