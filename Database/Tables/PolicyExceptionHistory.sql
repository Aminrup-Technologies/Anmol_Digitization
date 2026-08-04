/*
    Object:      [dbo].[PolicyExceptionHistory]
    Type:        TABLE
    Purpose:     Immutable audit trail for PolicyException Create/Update/Activate/Deactivate.
                 PolicyException remains the latest-state table only.
*/

CREATE TABLE [dbo].[PolicyExceptionHistory] (
    [HistoryId] int IDENTITY(1,1) NOT NULL,
    [ExceptionId] int NOT NULL,
    [VersionNo] int NOT NULL,
    [ActionType] nvarchar(20) NOT NULL,
    [PolicyJson] nvarchar(max) NULL,
    [Priority] int NOT NULL,
    [EffectiveFrom] date NULL,
    [EffectiveTo] date NULL,
    [IsActive] bit NOT NULL,
    [ChangedBy] nvarchar(100) NULL,
    [ChangedOn] datetime2(7) NOT NULL CONSTRAINT [DF_PolicyExceptionHistory_ChangedOn]
        DEFAULT (CONVERT([datetime2],((sysutcdatetime() AT TIME ZONE 'UTC') AT TIME ZONE 'India Standard Time'))),
    [ChangeReason] nvarchar(500) NULL
);
GO
ALTER TABLE [dbo].[PolicyExceptionHistory] ADD CONSTRAINT [PK_PolicyExceptionHistory] PRIMARY KEY CLUSTERED ([HistoryId] ASC);
ALTER TABLE [dbo].[PolicyExceptionHistory] ADD CONSTRAINT [UQ_PolicyExceptionHistory_Exception_Version] UNIQUE ([ExceptionId] ASC, [VersionNo] ASC);
ALTER TABLE [dbo].[PolicyExceptionHistory] ADD CONSTRAINT [FK_PolicyExceptionHistory_PolicyException]
    FOREIGN KEY ([ExceptionId]) REFERENCES [dbo].[PolicyException] ([ExceptionId]);
ALTER TABLE [dbo].[PolicyExceptionHistory] ADD CONSTRAINT [CK_PolicyExceptionHistory_ActionType]
    CHECK ([ActionType] IN (N'Create', N'Update', N'Activate', N'Deactivate'));
ALTER TABLE [dbo].[PolicyExceptionHistory] ADD CONSTRAINT [CK_PolicyExceptionHistory_VersionNo]
    CHECK ([VersionNo] >= 1);
CREATE NONCLUSTERED INDEX [IX_PolicyExceptionHistory_ExceptionId_VersionNo]
    ON [dbo].[PolicyExceptionHistory] ([ExceptionId] ASC, [VersionNo] DESC);
GO
