/*
    Object:      [dbo].[PolicyMatrix]
    Type:        TABLE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE TABLE [dbo].[PolicyMatrix] (
    [PolicyId] int IDENTITY(1,1) NOT NULL,
    [RegionId] int NULL,
    [BranchId] int NULL,
    [GradeId] int NULL,
    [DesignationId] int NULL,
    [PolicyJson] nvarchar(max) NULL,
    [Priority] int NOT NULL CONSTRAINT [DF__PolicyMat__Prior__1D1C38C9] DEFAULT ((0)),
    [EffectiveFrom] date NULL,
    [EffectiveTo] date NULL,
    [IsActive] bit NOT NULL CONSTRAINT [DF__PolicyMat__IsAct__1E105D02] DEFAULT ((1)),
    [CreatedBy] nvarchar(100) NULL,
    [CreatedOn] datetime2(7) NOT NULL CONSTRAINT [DF__PolicyMat__Creat__1F04813B] DEFAULT (CONVERT([datetime2],((sysutcdatetime() AT TIME ZONE 'UTC') AT TIME ZONE 'India Standard Time'))),
    [ModifiedBy] nvarchar(100) NULL,
    [ModifiedOn] datetime2(7) NULL
);
GO
ALTER TABLE [dbo].[PolicyMatrix] ADD CONSTRAINT [PK__PolicyMa__2E1339A42554ACCE] PRIMARY KEY CLUSTERED ([PolicyId] ASC);
ALTER TABLE [dbo].[PolicyMatrix] ADD CONSTRAINT [FK_PolicyMatrix_Branch] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[MST_Branch] ([Id]);
ALTER TABLE [dbo].[PolicyMatrix] ADD CONSTRAINT [FK_PolicyMatrix_Designation] FOREIGN KEY ([DesignationId]) REFERENCES [dbo].[MST_Designation] ([Id]);
ALTER TABLE [dbo].[PolicyMatrix] ADD CONSTRAINT [FK_PolicyMatrix_Grade] FOREIGN KEY ([GradeId]) REFERENCES [dbo].[MST_Grade] ([Id]);
ALTER TABLE [dbo].[PolicyMatrix] ADD CONSTRAINT [FK_PolicyMatrix_Region] FOREIGN KEY ([RegionId]) REFERENCES [dbo].[MST_Region] ([Id]);
