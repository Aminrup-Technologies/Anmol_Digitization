/*
    Object:      [dbo].[SalesApprovalRequest]
    Type:        TABLE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE TABLE [dbo].[SalesApprovalRequest] (
    [ApprovalRequestId] int IDENTITY(1,1) NOT NULL,
    [RequesterLoginId] nvarchar(100) NOT NULL,
    [RequestDate] datetime2(7) NOT NULL CONSTRAINT [DF__SalesAppr__Reque__5EB4F1FC] DEFAULT (sysutcdatetime()),
    [YearNo] int NOT NULL,
    [MonthNo] int NOT NULL,
    [Status] nvarchar(50) NOT NULL CONSTRAINT [DF__SalesAppr__Statu__5FA91635] DEFAULT ('Pending'),
    [RequestRemarks] nvarchar(1000) NULL,
    [CreatedOn] datetime2(7) NOT NULL CONSTRAINT [DF__SalesAppr__Creat__609D3A6E] DEFAULT (sysdatetime()),
    [CreatedBy] nvarchar(100) NULL,
    [ModifiedOn] datetime2(7) NULL,
    [ModifiedBy] nvarchar(100) NULL,
    [TotalKM] decimal(12,2) NULL,
    [TotalClaimAmount] decimal(18,2) NULL,
    [TotalDAAmount] decimal(18,2) NULL,
    [TotalHotelAmount] decimal(18,2) NULL,
    [TotalTravelAmount] decimal(18,2) NULL,
    [TotalExpenseAmount] decimal(18,2) NULL,
    [CurrentApproverLoginId] nvarchar(100) NULL,
    [CurrentApproverLevel] int NULL,
    [ApprovedDAAmount] decimal(18,2) NULL,
    [ApprovedHotelAmount] decimal(18,2) NULL,
    [ApprovedTravelAmount] decimal(18,2) NULL,
    [ApprovedKM] decimal(12,2) NULL,
    [ApprovedClaimAmount] decimal(18,2) NULL,
    [ApprovedExpenseAmount] AS ((isnull([ApprovedDAAmount],(0))+isnull([ApprovedHotelAmount],(0)))+isnull([ApprovedTravelAmount],(0))),
    [TotalApprovedAmount] AS (((isnull([ApprovedClaimAmount],(0))+isnull([ApprovedDAAmount],(0)))+isnull([ApprovedHotelAmount],(0)))+isnull([ApprovedTravelAmount],(0))),
    [OriginalClaimAmount] decimal(18,2) NULL CONSTRAINT [DF__SalesAppr__Origi__17ED6F58] DEFAULT ((0)),
    [TotalStationaryAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_SAR_TotalStationaryAmount] DEFAULT ((0)),
    [TotalActivities] int NOT NULL CONSTRAINT [DF_SAR_TotalActivities] DEFAULT ((0)),
    [TotalExpenseEntries] int NOT NULL CONSTRAINT [DF_SAR_TotalExpenseEntries] DEFAULT ((0)),
    [TotalAttachmentCount] int NOT NULL CONSTRAINT [DF_SAR_TotalAttachmentCount] DEFAULT ((0)),
    [ApprovedActivities] int NOT NULL CONSTRAINT [DF_SAR_ApprovedActivities] DEFAULT ((0)),
    [RejectedActivities] int NOT NULL CONSTRAINT [DF_SAR_RejectedActivities] DEFAULT ((0)),
    [PendingActivities] int NOT NULL CONSTRAINT [DF_SAR_PendingActivities] DEFAULT ((0)),
    [FinalApprovedAmount] decimal(18,2) NULL,
    [TotalDeductionAmount] decimal(18,2) NULL,
    [BulkEligibleActivities] int NOT NULL CONSTRAINT [DF_SAR_BulkEligible] DEFAULT ((0)),
    [ManualReviewActivities] int NOT NULL CONSTRAINT [DF_SAR_ManualReview] DEFAULT ((0)),
    [ReturnedActivities] int NOT NULL CONSTRAINT [DF_SAR_ReturnedActivities] DEFAULT ((0))
);
GO
ALTER TABLE [dbo].[SalesApprovalRequest] ADD CONSTRAINT [PK__SalesApp__9791331781240304] PRIMARY KEY CLUSTERED ([ApprovalRequestId] ASC);
ALTER TABLE [dbo].[SalesApprovalRequest] ADD CONSTRAINT [CK_TotalApprovedAmount_Formula] CHECK ([TotalApprovedAmount]=(((isnull([ApprovedClaimAmount],(0))+isnull([ApprovedDAAmount],(0)))+isnull([ApprovedHotelAmount],(0)))+isnull([ApprovedTravelAmount],(0))));
CREATE NONCLUSTERED INDEX [IX_SalesApprovalRequest_Employee] ON [dbo].[SalesApprovalRequest] ([RequesterLoginId] ASC);
CREATE NONCLUSTERED INDEX [IX_SalesApprovalRequest_Status] ON [dbo].[SalesApprovalRequest] ([Status] ASC);
