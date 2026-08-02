/*
    Object:      [dbo].[SalesExpenseAdjustment]
    Type:        TABLE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE TABLE [dbo].[SalesExpenseAdjustment] (
    [AdjustmentId] int IDENTITY(1,1) NOT NULL,
    [ExpenseId] int NOT NULL,
    [ActivityId] int NOT NULL,
    [ApprovalRequestId] int NOT NULL,
    [ExpenseType] nvarchar(50) NOT NULL,
    [ClaimedAmount] decimal(18,2) NOT NULL,
    [ApprovedAmount] decimal(18,2) NOT NULL,
    [DeductionAmount] AS ([ClaimedAmount]-[ApprovedAmount]),
    [AdjustmentReason] nvarchar(1000) NOT NULL,
    [ResolvedPolicyJson] nvarchar(max) NULL,
    [AdjustedBy] nvarchar(100) NOT NULL,
    [AdjustedOn] datetime2(7) NOT NULL CONSTRAINT [DF__SalesExpe__Adjus__68C86C1B] DEFAULT (sysutcdatetime()),
    [ApproverLevel] int NOT NULL
);
GO
ALTER TABLE [dbo].[SalesExpenseAdjustment] ADD CONSTRAINT [PK__SalesExp__E60DB8936D877767] PRIMARY KEY CLUSTERED ([AdjustmentId] ASC);
ALTER TABLE [dbo].[SalesExpenseAdjustment] ADD CONSTRAINT [FK_Adjustment_Expense] FOREIGN KEY ([ExpenseId]) REFERENCES [dbo].[SalesDailyExpense] ([ExpenseId]);
ALTER TABLE [dbo].[SalesExpenseAdjustment] ADD CONSTRAINT [FK_Adjustment_Activity] FOREIGN KEY ([ActivityId]) REFERENCES [dbo].[SalesDailyActivity] ([ActivityId]);
ALTER TABLE [dbo].[SalesExpenseAdjustment] ADD CONSTRAINT [FK_Adjustment_Request] FOREIGN KEY ([ApprovalRequestId]) REFERENCES [dbo].[SalesApprovalRequest] ([ApprovalRequestId]);
