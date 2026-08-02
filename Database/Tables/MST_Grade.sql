/*
    Object:      [dbo].[MST_Grade]
    Type:        TABLE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE TABLE [dbo].[MST_Grade] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Grade_Description] varchar(50) NULL,
    [Grade_ID] varchar(50) NULL
);
GO
ALTER TABLE [dbo].[MST_Grade] ADD CONSTRAINT [PK_MST_Grade] PRIMARY KEY CLUSTERED ([Id] ASC);
