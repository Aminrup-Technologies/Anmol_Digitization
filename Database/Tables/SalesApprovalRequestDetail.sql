/*
    Object:      [dbo].[SalesApprovalRequestDetail]
    Type:        TABLE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE TABLE [dbo].[SalesApprovalRequestDetail] (
    [ApprovalRequestDetailId] int IDENTITY(1,1) NOT NULL,
    [ApprovalRequestId] int NOT NULL,
    [ActivityId] int NOT NULL,
    [Snapshot_TotalDistanceKM] decimal(10,2) NULL,
    [Snapshot_CalculatedClaimAmount] decimal(18,2) NULL,
    [Snapshot_DAAmount] decimal(18,2) NULL,
    [Snapshot_HotelAmount] decimal(18,2) NULL,
    [Snapshot_TravelAmount] decimal(18,2) NULL,
    [Snapshot_ExpenseTotal] decimal(18,2) NULL,
    [CreatedOn] datetime2(7) NOT NULL CONSTRAINT [DF__SalesAppr__Creat__4400FBC0] DEFAULT (sysutcdatetime()),
    [CreatedBy] nvarchar(100) NULL,
    [DetailStatus] nvarchar(50) NULL,
    [DetailRemarks] nvarchar(1000) NULL,
    [DetailApprovedBy] nvarchar(100) NULL,
    [DetailApprovedOn] datetime2(7) NULL,
    [ModifiedOn] datetime2(7) NULL,
    [ModifiedBy] nvarchar(100) NULL,
    [DetailApprovedLevel] int NULL,
    [Snapshot_StationaryAmount] decimal(18,2) NULL,
    [DetailVersion] int NOT NULL CONSTRAINT [DF__SalesAppr__Detai__5B6E70FD] DEFAULT ((1)),
    [PreviousDetailId] int NULL
);
GO
ALTER TABLE [dbo].[SalesApprovalRequestDetail] ADD CONSTRAINT [PK__SalesApp__8FD88CB356D8F65D] PRIMARY KEY CLUSTERED ([ApprovalRequestDetailId] ASC);
ALTER TABLE [dbo].[SalesApprovalRequestDetail] ADD CONSTRAINT [FK_SARDetail_Activity] FOREIGN KEY ([ActivityId]) REFERENCES [dbo].[SalesDailyActivity] ([ActivityId]);
ALTER TABLE [dbo].[SalesApprovalRequestDetail] ADD CONSTRAINT [FK_SARDetail_Request] FOREIGN KEY ([ApprovalRequestId]) REFERENCES [dbo].[SalesApprovalRequest] ([ApprovalRequestId]);
CREATE NONCLUSTERED INDEX [IX_SalesApprovalRequestDetail_RequestId] ON [dbo].[SalesApprovalRequestDetail] ([ApprovalRequestId] ASC);
CREATE NONCLUSTERED INDEX [IX_SalesApprovalRequestDetail_Request_Activity_Version] ON [dbo].[SalesApprovalRequestDetail] ([ApprovalRequestId] ASC, [ActivityId] ASC, [DetailVersion] DESC);
