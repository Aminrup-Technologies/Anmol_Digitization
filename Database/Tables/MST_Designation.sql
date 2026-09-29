/*
    Object:      [dbo].[MST_Designation]
    Type:        TABLE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE TABLE [dbo].[MST_Designation] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Designation_Description] varchar(50) NULL,
    [Designation_ID] varchar(50) NULL
);
GO
ALTER TABLE [dbo].[MST_Designation] ADD CONSTRAINT [PK_MST_Designation] PRIMARY KEY CLUSTERED ([Id] ASC);
