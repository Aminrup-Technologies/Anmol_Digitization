/*
    Object:      [dbo].[SalesDailyExpense]
    Type:        TABLE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE TABLE [dbo].[SalesDailyExpense] (
    [ExpenseId] int IDENTITY(1,1) NOT NULL,
    [ActivityId] int NOT NULL,
    [ExpenseType] nvarchar(50) NOT NULL,
    [ExpenseDate] date NULL,
    [FromDate] date NULL,
    [ToDate] date NULL,
    [BillNo] nvarchar(200) NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Currency] nvarchar(10) NULL,
    [Purpose] nvarchar(1000) NULL,
    [AttachmentFileName] nvarchar(500) NULL,
    [AttachmentContent] varbinary(max) NULL,
    [CreatedBy] nvarchar(100) NULL,
    [CreatedDateTime] datetime NOT NULL CONSTRAINT [DF__SalesDail__Creat__4D005615] DEFAULT (dateadd(minute,(330),sysutcdatetime())),
    [AttachmentPath] nvarchar(1000) NULL,
    [TravelType] nvarchar(50) NULL,
    [FromPlace] nvarchar(200) NULL,
    [ToPlace] nvarchar(200) NULL,
    [TravelDate] date NULL,
    [PolicyMode] nvarchar(50) NULL,
    [PolicySnapshot] nvarchar(max) NULL,
    [IsAutoDA] bit NOT NULL CONSTRAINT [DF_SalesDailyExpense_IsAutoDA] DEFAULT ((0)),
    [CreatedByIP] nvarchar(45) NULL,
    [Source] nvarchar(50) NULL,
    [ClaimStatus] nvarchar(50) NULL CONSTRAINT [DF_SalesDailyExpense_ClaimStatus] DEFAULT ('Draft'),
    [ClaimProcessedBy] nvarchar(200) NULL,
    [ClaimProcessedDate] datetime NULL,
    [ExpenseSeq] int NULL,
    [ModifiedOn] datetime2(7) NULL,
    [ModifiedBy] nvarchar(100) NULL,
    [IsDeleted] bit NOT NULL CONSTRAINT [DF_SalesDailyExpense_IsDeleted] DEFAULT ((0)),
    [DeletedBy] nvarchar(100) NULL,
    [DeletedOn] datetime2(7) NULL,
    [DeleteReason] nvarchar(500) NULL,
    [Remarks] nvarchar(1000) NULL,
    [ApprovalStatus] nvarchar(50) NULL,
    [ApprovalRemarks] nvarchar(1000) NULL,
    [ApprovedBy] nvarchar(100) NULL,
    [ApprovedOn] datetime2(7) NULL
);
GO
ALTER TABLE [dbo].[SalesDailyExpense] ADD CONSTRAINT [PK__SalesDai__1445CFD3F22EAE53] PRIMARY KEY CLUSTERED ([ExpenseId] ASC);
ALTER TABLE [dbo].[SalesDailyExpense] ADD CONSTRAINT [CK_SalesDailyExpense_Amount_NonNegative] CHECK ([Amount]>=(0));
ALTER TABLE [dbo].[SalesDailyExpense] ADD CONSTRAINT [CK_SalesDailyExpense_NoUpdate_WhenDeleted] CHECK ([IsDeleted]=(0) OR [ClaimStatus]='Draft');
ALTER TABLE [dbo].[SalesDailyExpense] ADD CONSTRAINT [FK_SalesDailyExpense_Activity] FOREIGN KEY ([ActivityId]) REFERENCES [dbo].[SalesDailyActivity] ([ActivityId]);
CREATE UNIQUE NONCLUSTERED INDEX [UQ_SalesDailyExpense_ActivityDateExpenseType_Seq] ON [dbo].[SalesDailyExpense] ([ActivityId] ASC, [ExpenseDate] ASC, [ExpenseType] ASC, [ExpenseSeq] ASC) WHERE ([IsDeleted]=(0));
