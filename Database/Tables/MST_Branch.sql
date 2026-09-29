/*
    Object:      [dbo].[MST_Branch]
    Type:        TABLE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE TABLE [dbo].[MST_Branch] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Branch_Description] varchar(50) NULL,
    [Branch_ID] int NULL,
    [VMS_Menu] bit NULL CONSTRAINT [DF_MST_Branch_VMS_Menu] DEFAULT ((0))
);
GO
ALTER TABLE [dbo].[MST_Branch] ADD CONSTRAINT [PK_MST_Branch] PRIMARY KEY CLUSTERED ([Id] ASC);
