/* ============================================================================
   NAME:        RC-018 Phase 3 / Table 8 — SalesApprovalRequestDetail verification
   WHEN:        2026-09-25
   SOURCE:      docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md
                Database/Tables/SalesApprovalRequestDetail.sql
   TARGET:      LIVE dbo.SalesApprovalRequestDetail (AnmolDristi)
   WHAT:        Verification only. No DDL when the shared schema is already aligned.
                Backup is created only when a change would otherwise be required,
                and even then this script does not apply that change.
   PRESERVE:    Rows, identity, DetailVersion, snapshot columns, PK.
   FORBIDDEN:   DELETE, TRUNCATE, recreate, reseed, automatic ALTER.
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
DECLARE @BackupTable SYSNAME      = N'dbo.RC018_Backup_SalesApprovalRequestDetail_' + @RunTag;
DECLARE @Sql         NVARCHAR(MAX);
DECLARE @Gaps        INT = 0;
DECLARE @RowCnt      INT;

PRINT N'RC-018 P3-T08 SalesApprovalRequestDetail | RunTag=' + @RunTag
    + N' | DryRun=' + CAST(@DryRun AS NVARCHAR(1));

IF OBJECT_ID(N'dbo.SalesApprovalRequestDetail', N'U') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: dbo.SalesApprovalRequestDetail not found. Abort.', 16, 1);
    RETURN;
END;

SELECT @RowCnt = COUNT(*) FROM dbo.SalesApprovalRequestDetail;
PRINT N'LIVE row count=' + CAST(@RowCnt AS NVARCHAR(20))
    + N' (Phase 0 baseline 34257; UAT 3023 is not a target count).';

/* Required shared objects. Default names differ by database; definitions are checked. */
IF NOT EXISTS (
    SELECT 1
    FROM sys.key_constraints kc
    INNER JOIN sys.index_columns ic
        ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id AND ic.key_ordinal = 1
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE kc.parent_object_id = OBJECT_ID(N'dbo.SalesApprovalRequestDetail')
      AND kc.type = N'PK'
      AND c.name = N'ApprovalRequestDetailId'
)
BEGIN
    PRINT N'GAP: PK is not ApprovalRequestDetailId.';
    SET @Gaps += 1;
END;

IF COLUMNPROPERTY(OBJECT_ID(N'dbo.SalesApprovalRequestDetail'), N'ApprovalRequestDetailId', N'IsIdentity') <> 1
BEGIN
    PRINT N'GAP: ApprovalRequestDetailId is not an identity column.';
    SET @Gaps += 1;
END;

IF COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'DetailVersion') IS NULL
 OR COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'PreviousDetailId') IS NULL
 OR COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'Snapshot_ExpenseTotal') IS NULL
 OR COL_LENGTH(N'dbo.SalesApprovalRequestDetail', N'Snapshot_StationaryAmount') IS NULL
BEGIN
    PRINT N'GAP: snapshot or version column missing.';
    SET @Gaps += 1;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.SalesApprovalRequestDetail')
      AND name = N'IX_SalesApprovalRequestDetail_RequestId'
)
BEGIN
    PRINT N'GAP: IX_SalesApprovalRequestDetail_RequestId missing.';
    SET @Gaps += 1;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.SalesApprovalRequestDetail')
      AND name = N'IX_SalesApprovalRequestDetail_Request_Activity_Version'
)
BEGIN
    PRINT N'GAP: IX_SalesApprovalRequestDetail_Request_Activity_Version missing.';
    SET @Gaps += 1;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.SalesApprovalRequestDetail')
      AND name = N'FK_SARDetail_Activity'
)
 OR NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.SalesApprovalRequestDetail')
      AND name = N'FK_SARDetail_Request'
)
BEGIN
    PRINT N'GAP: FK_SARDetail_Activity or FK_SARDetail_Request missing.';
    SET @Gaps += 1;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.SalesApprovalRequestDetail')
      AND c.name = N'CreatedOn'
      AND dc.definition LIKE N'%sysutcdatetime%'
)
 OR NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.SalesApprovalRequestDetail')
      AND c.name = N'DetailVersion'
      AND REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(ISNULL(dc.definition, N''), N' ', N''), CHAR(9), N''), CHAR(13), N''), CHAR(10), N''), N'(', N''), N')', N'') = N'1'
)
BEGIN
    PRINT N'GAP: CreatedOn or DetailVersion default definition missing.';
    SET @Gaps += 1;
END;

IF EXISTS (
    SELECT 1 FROM sys.triggers
    WHERE parent_id = OBJECT_ID(N'dbo.SalesApprovalRequestDetail')
)
BEGIN
    PRINT N'GAP: unexpected trigger on SalesApprovalRequestDetail.';
    SET @Gaps += 1;
END;

/* Parent side of P3-T07 FK_ApprovalHistory_Detail */
PRINT N'Parent key for FK_ApprovalHistory_Detail is ApprovalRequestDetailId.';

IF COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ApprovalRequestDetailId') IS NULL
    PRINT N'History.ApprovalRequestDetailId is not on LIVE yet. Orphan check waits for P3-T07.';
ELSE
BEGIN
    DECLARE @Orphans INT;
    SET @Sql = N'
        SELECT @c = COUNT(*)
        FROM dbo.ApprovalRequestStatusHistory h
        WHERE h.ApprovalRequestDetailId IS NOT NULL
          AND NOT EXISTS (
              SELECT 1
              FROM dbo.SalesApprovalRequestDetail d
              WHERE d.ApprovalRequestDetailId = h.ApprovalRequestDetailId
          );';
    EXEC sys.sp_executesql @Sql, N'@c INT OUTPUT', @c = @Orphans OUTPUT;
    PRINT N'History detail orphans=' + CAST(@Orphans AS NVARCHAR(20));
    IF @Orphans > 0
    BEGIN
        PRINT N'GAP: ApprovalRequestStatusHistory references missing detail ids.';
        SET @Gaps += 1;
    END
END;

PRINT N'Alignment gaps=' + CAST(@Gaps AS NVARCHAR(10));

IF @Gaps = 0
BEGIN
    PRINT N'MODE: verification-only. No change would occur. Backup not created. Identity not touched.';
    IF @DryRun = 1
        PRINT N'DryRun=1 — same no-op. Next: 023b validation.';
    RETURN;
END;

PRINT N'MODE: change would be required. This package does not ALTER the snapshot table.';

IF @DryRun = 1
BEGIN
    PRINT N'DryRun=1 — backup skipped. Re-run with @DryRun=0 only to capture a backup for review.';
    RETURN;
END;

IF OBJECT_ID(@BackupTable, N'U') IS NOT NULL
BEGIN
    RAISERROR(N'BLOCKER: backup table already exists for this RunTag. Abort.', 16, 1);
    RETURN;
END;

SET @Sql = N'SELECT * INTO ' + @BackupTable + N' FROM dbo.SalesApprovalRequestDetail WITH (TABLOCKX);';
EXEC sys.sp_executesql @Sql;
PRINT N'Backup created because a change would be required: ' + @BackupTable;
PRINT N'No DDL applied. Review the GAP lines before any separate change.';
GO
