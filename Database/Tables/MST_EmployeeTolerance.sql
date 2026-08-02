/*
    Object:      [dbo].[MST_EmployeeTolerance]
    Type:        TABLE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE TABLE [dbo].[MST_EmployeeTolerance] (
    [ToleranceId] int IDENTITY(1,1) NOT NULL,
    [EmployeeCode] nvarchar(100) NOT NULL,
    [MinAllowedKM] decimal(10,2) NULL,
    [MaxAllowedKM] decimal(10,2) NULL,
    [EffectiveFrom] date NULL,
    [EffectiveTo] date NULL,
    [CreatedBy] nvarchar(100) NULL,
    [CreatedDateTime] datetime NOT NULL CONSTRAINT [DF__MST_Emplo__Creat__62EF9734] DEFAULT (dateadd(minute,(330),sysutcdatetime())),
    [ModifiedBy] nvarchar(100) NULL,
    [ModifiedDateTime] datetime NULL,
    [IsActive] bit NOT NULL CONSTRAINT [DF_MST_EmployeeTolerance_IsActive] DEFAULT ((1)),
    [MaxKmModificationCount] int NOT NULL CONSTRAINT [DF_MST_EmployeeTolerance_MaxKmModificationCount] DEFAULT ((1))
);
GO
ALTER TABLE [dbo].[MST_EmployeeTolerance] ADD CONSTRAINT [PK__MST_Empl__A9A07C540260869D] PRIMARY KEY CLUSTERED ([ToleranceId] ASC);
ALTER TABLE [dbo].[MST_EmployeeTolerance] ADD CONSTRAINT [CK_MST_EmployeeTolerance_MaxKmModificationCount] CHECK ([MaxKmModificationCount]>=(1));
CREATE NONCLUSTERED INDEX [IX_MST_EmployeeTolerance_Lookup] ON [dbo].[MST_EmployeeTolerance] ([EmployeeCode] ASC, [EffectiveFrom] ASC, [EffectiveTo] ASC) INCLUDE ([MinAllowedKM], [MaxAllowedKM], [IsActive]);
