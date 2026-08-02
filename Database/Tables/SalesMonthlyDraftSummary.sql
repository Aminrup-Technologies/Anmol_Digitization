/*
    Object:      [dbo].[SalesMonthlyDraftSummary]
    Type:        TABLE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE TABLE [dbo].[SalesMonthlyDraftSummary] (
    [DraftId] int IDENTITY(1,1) NOT NULL,
    [Login_Id] nvarchar(100) NOT NULL,
    [YearNo] int NOT NULL,
    [MonthNo] int NOT NULL,
    [TotalKM] decimal(12,2) NOT NULL,
    [TotalClaimAmount] decimal(18,2) NOT NULL,
    [CreatedOn] datetime2(7) NOT NULL CONSTRAINT [DF__SalesMont__Creat__4FDCC2C0] DEFAULT (switchoffset(sysdatetimeoffset(),'+05:30')),
    [UpdatedOn] datetime2(7) NULL
);
GO
ALTER TABLE [dbo].[SalesMonthlyDraftSummary] ADD CONSTRAINT [PK__SalesMon__3E93D65BECFC7164] PRIMARY KEY CLUSTERED ([DraftId] ASC);
