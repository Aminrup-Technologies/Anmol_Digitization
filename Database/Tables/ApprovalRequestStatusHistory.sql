/*
    Object:      [dbo].[ApprovalRequestStatusHistory]
    Type:        TABLE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE TABLE [dbo].[ApprovalRequestStatusHistory] (
    [HistoryId] int IDENTITY(1,1) NOT NULL,
    [ApprovalRequestId] int NOT NULL,
    [OldStatus] nvarchar(50) NULL,
    [NewStatus] nvarchar(50) NOT NULL,
    [ChangedBy] nvarchar(100) NULL,
    [ChangedOn] datetime2(7) NOT NULL CONSTRAINT [DF__ApprovalR__Chang__524F1B17] DEFAULT (sysutcdatetime()),
    [Remarks] nvarchar(1000) NULL,
    [ApprovalRequestDetailId] int NULL,
    [ActivityId] int NULL,
    [ApproverLevel] int NULL,
    [ActionType] nvarchar(30) NULL,
    [DetailVersion] int NULL
);
GO
ALTER TABLE [dbo].[ApprovalRequestStatusHistory] ADD CONSTRAINT [PK__Approval__4D7B4ABD48F7BB10] PRIMARY KEY CLUSTERED ([HistoryId] ASC);
ALTER TABLE [dbo].[ApprovalRequestStatusHistory] ADD CONSTRAINT [FK_ApprovalHistory_Detail] FOREIGN KEY ([ApprovalRequestDetailId]) REFERENCES [dbo].[SalesApprovalRequestDetail] ([ApprovalRequestDetailId]);
ALTER TABLE [dbo].[ApprovalRequestStatusHistory] ADD CONSTRAINT [FK_ApprovalHistory_Request] FOREIGN KEY ([ApprovalRequestId]) REFERENCES [dbo].[SalesApprovalRequest] ([ApprovalRequestId]);
