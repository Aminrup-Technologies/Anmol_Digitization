/* ============================================================================
   NAME:        RC-018 Phase 4 / Table 12 — CMS_RawConsumptionLogs verification
   WHEN:        2026-09-25
   SOURCE:      docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md
   TARGET:      LIVE dbo.CMS_RawConsumptionLogs (AnmolDristi)
   WHAT:        Verification only. Exit with no DDL when alignment gaps = 0.
                Never drop or rebuild UX_CMS_RawConsumptionLogs_Dedup.
                Backup only if a gap means a later change would be required.
   FORBIDDEN:   DELETE, TRUNCATE, recreate, data changes, index alter.
   NOTE:        DO NOT EXECUTE until Production Gate. Default @DryRun = 1.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @DryRun BIT = 1;

DECLARE @RunTag      NVARCHAR(32) = CONVERT(CHAR(8), GETDATE(), 112)
                                 + N'_' + REPLACE(CONVERT(CHAR(8), GETDATE(), 108), N':', N'');
DECLARE @BackupTable SYSNAME      = N'dbo.RC018_Backup_CMS_RawConsumptionLogs_' + @RunTag;
DECLARE @Sql         NVARCHAR(MAX);
DECLARE @Gaps        INT = 0;
DECLARE @RowCnt      BIGINT;
DECLARE @UxKeys      NVARCHAR(400);

PRINT N'RC-018 P4-T12 CMS_RawConsumptionLogs | RunTag=' + @RunTag
    + N' | DryRun=' + CAST(@DryRun AS NVARCHAR(1));

IF OBJECT_ID(N'dbo.CMS_RawConsumptionLogs', N'U') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: dbo.CMS_RawConsumptionLogs not found. Abort.', 16, 1);
    RETURN;
END;

SELECT @RowCnt = COUNT_BIG(*) FROM dbo.CMS_RawConsumptionLogs;
PRINT N'LIVE row count=' + CAST(@RowCnt AS NVARCHAR(20))
    + N' (Phase 0 floor 772493). UAT row count is not a target.';

IF NOT EXISTS (
    SELECT 1
    FROM sys.key_constraints kc
    INNER JOIN sys.index_columns ic
        ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id AND ic.key_ordinal = 1
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE kc.parent_object_id = OBJECT_ID(N'dbo.CMS_RawConsumptionLogs')
      AND kc.type = N'PK'
      AND c.name = N'LogId'
)
BEGIN
    PRINT N'GAP: PK is not LogId.';
    SET @Gaps += 1;
END;

IF COLUMNPROPERTY(OBJECT_ID(N'dbo.CMS_RawConsumptionLogs'), N'LogId', N'IsIdentity') <> 1
BEGIN
    PRINT N'GAP: LogId is not an identity column.';
    SET @Gaps += 1;
END;

IF (
    SELECT COUNT(*)
    FROM (VALUES
        (N'LogId'), (N'EmployeeCode'), (N'EnrollNumber'), (N'VerifyMode'), (N'InOutMode'),
        (N'PunchTime'), (N'CreatedOn'), (N'BranchID'), (N'MachineIP'), (N'EmployeeName')
    ) v(ColumnName)
    WHERE COL_LENGTH(N'dbo.CMS_RawConsumptionLogs', v.ColumnName) IS NOT NULL
) <> 10
BEGIN
    PRINT N'GAP: one or more of the 10 shared columns is missing.';
    SET @Gaps += 1;
END;

SELECT @UxKeys = STUFF((
    SELECT N',' + c.name
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic
        ON ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.CMS_RawConsumptionLogs')
      AND i.name = N'UX_CMS_RawConsumptionLogs_Dedup'
      AND i.is_unique = 1
    ORDER BY ic.key_ordinal
    FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(400)'), 1, 1, N'');

IF ISNULL(@UxKeys, N'') <> N'MachineIP,EnrollNumber,PunchTime'
BEGIN
    PRINT N'GAP: UX_CMS_RawConsumptionLogs_Dedup missing or key list is not MachineIP, EnrollNumber, PunchTime. It will not be altered.';
    SET @Gaps += 1;
END
ELSE
    PRINT N'UX_CMS_RawConsumptionLogs_Dedup present and retained.';

IF EXISTS (
    SELECT 1 FROM sys.triggers
    WHERE parent_id = OBJECT_ID(N'dbo.CMS_RawConsumptionLogs')
)
BEGIN
    PRINT N'GAP: unexpected trigger.';
    SET @Gaps += 1;
END;

PRINT N'Alignment gaps=' + CAST(@Gaps AS NVARCHAR(10));

IF @Gaps = 0
BEGIN
    PRINT N'MODE: verification-only. No change would occur. Backup not created. Unique index not altered.';
    RETURN;
END;

PRINT N'MODE: a change would be required. This package does not ALTER CMS_RawConsumptionLogs.';

IF @DryRun = 1
BEGIN
    PRINT N'DryRun=1 — backup skipped. A full backup of this log table is large. Re-run with @DryRun=0 only to capture it for review.';
    RETURN;
END;

IF OBJECT_ID(@BackupTable, N'U') IS NOT NULL
BEGIN
    RAISERROR(N'BLOCKER: backup table already exists for this RunTag. Abort.', 16, 1);
    RETURN;
END;

SET @Sql = N'SELECT * INTO ' + @BackupTable + N' FROM dbo.CMS_RawConsumptionLogs WITH (TABLOCKX);';
EXEC sys.sp_executesql @Sql;
PRINT N'Backup created because a change would be required: ' + @BackupTable;
PRINT N'No DDL applied.';
GO
