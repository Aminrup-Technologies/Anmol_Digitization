/*
    Object:      [dbo].[SalesApprovalRequestApprover]
    Type:        TABLE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE TABLE [dbo].[SalesApprovalRequestApprover] (
    [ApprovalRequestApproverId] int IDENTITY(1,1) NOT NULL,
    [ApprovalRequestId] int NOT NULL,
    [ApproverLoginId] nvarchar(100) NULL,
    [ApproverRole] nvarchar(100) NULL,
    [ApproverLevel] int NULL,
    [IsRequired] bit NOT NULL CONSTRAINT [DF__SalesAppr__IsReq__48C5B0DD] DEFAULT ((1)),
    [Status] nvarchar(50) NOT NULL CONSTRAINT [DF__SalesAppr__Statu__49B9D516] DEFAULT ('Pending'),
    [ActionBy] nvarchar(100) NULL,
    [ActionDate] datetime2(7) NULL,
    [Remarks] nvarchar(1000) NULL,
    [CreatedOn] datetime2(7) NOT NULL CONSTRAINT [DF__SalesAppr__Creat__4AADF94F] DEFAULT (sysutcdatetime()),
    [CreatedBy] nvarchar(100) NULL,
    [ModifiedOn] datetime2(7) NULL,
    [ModifiedBy] nvarchar(100) NULL
);
GO
ALTER TABLE [dbo].[SalesApprovalRequestApprover] ADD CONSTRAINT [PK__SalesApp__27EBB5C7CAAD2575] PRIMARY KEY CLUSTERED ([ApprovalRequestApproverId] ASC);
ALTER TABLE [dbo].[SalesApprovalRequestApprover] ADD CONSTRAINT [FK_SalesApprovalRequestApprover_Request] FOREIGN KEY ([ApprovalRequestId]) REFERENCES [dbo].[SalesApprovalRequest] ([ApprovalRequestId]);
CREATE UNIQUE NONCLUSTERED INDEX [UX_SAR_Auditor_Once] ON [dbo].[SalesApprovalRequestApprover] ([ApprovalRequestId] ASC) WHERE ([ApproverRole]='Auditor');
