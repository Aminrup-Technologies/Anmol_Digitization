/*
    Object:      [dbo].[SalesDailyActivityKMHistory]
    Type:        TABLE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE TABLE [dbo].[SalesDailyActivityKMHistory] (
    [HistoryId] int IDENTITY(1,1) NOT NULL,
    [ActivityId] int NOT NULL,
    [OldKM] decimal(10,2) NULL,
    [NewKM] decimal(10,2) NULL,
    [ModifiedBy] nvarchar(100) NULL,
    [ModifiedDateTime] datetime NOT NULL CONSTRAINT [DF__SalesDail__Modif__4C0C31DC] DEFAULT (dateadd(minute,(330),sysutcdatetime())),
    [Reason] nvarchar(1000) NULL,
    [RouteOriginName] nvarchar(255) NULL,
    [RouteDestinationName] nvarchar(255) NULL,
    [RouteWaypointsJSON] nvarchar(max) NULL,
    [TravelMode] nvarchar(20) NULL
);
GO
ALTER TABLE [dbo].[SalesDailyActivityKMHistory] ADD CONSTRAINT [PK__SalesDai__4D7B4ABD9107B9CA] PRIMARY KEY CLUSTERED ([HistoryId] ASC);
ALTER TABLE [dbo].[SalesDailyActivityKMHistory] ADD CONSTRAINT [FK_KMHistory_Activity] FOREIGN KEY ([ActivityId]) REFERENCES [dbo].[SalesDailyActivity] ([ActivityId]);
CREATE NONCLUSTERED INDEX [IX_SalesDailyActivityKMHistory_Activity] ON [dbo].[SalesDailyActivityKMHistory] ([ActivityId] ASC, [ModifiedDateTime] DESC);
