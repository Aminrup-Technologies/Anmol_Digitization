/* ============================================================================
   NAME:        RC-018 Phase 3 / Table 7 — ApprovalRequestStatusHistory schema
   WHEN:        2026-09-25
   SOURCE:      docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md
                Database/Tables/ApprovalRequestStatusHistory.sql
   TARGET:      LIVE dbo.ApprovalRequestStatusHistory (AnmolDristi)
   WHAT:        Add five nullable audit columns if absent, then add
                FK_ApprovalHistory_Detail WITH CHECK only when orphan count = 0.
   PRESERVE:    Existing history rows. PK. FK_ApprovalHistory_Request.
   FORBIDDEN:   DELETE, TRUNCATE, recreate, UPDATE of existing rows, WITH NOCHECK.
   NOTE:        DO NOT EXECUTE until Production Gate. Default @DryRun = 1.
                New-column references use dynamic SQL (same-batch binding).
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
DECLARE @BackupTable SYSNAME      = N'dbo.RC018_Backup_ApprovalRequestStatusHistory_' + @RunTag;
DECLARE @Sql         NVARCHAR(MAX);
DECLARE @Orphans     INT = 0;
DECLARE @BeforeCnt   INT;

PRINT N'RC-018 P3-T07 ApprovalRequestStatusHistory | RunTag=' + @RunTag
    + N' | DryRun=' + CAST(@DryRun AS NVARCHAR(1));

IF OBJECT_ID(N'dbo.ApprovalRequestStatusHistory', N'U') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: dbo.ApprovalRequestStatusHistory not found. Abort.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'dbo.SalesApprovalRequestDetail', N'U') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: dbo.SalesApprovalRequestDetail not found. Abort.', 16, 1);
    RETURN;
END;

SELECT @BeforeCnt = COUNT(*) FROM dbo.ApprovalRequestStatusHistory;
PRINT N'Preflight row count=' + CAST(@BeforeCnt AS NVARCHAR(20))
    + N' (Phase 0 LIVE baseline 4403; must not decrease).';

PRINT N'--- PLANNED DDL (nullable, no row rewrite) ---';
PRINT N'ADD ApprovalRequestDetailId INT NULL if absent';
PRINT N'ADD ActivityId INT NULL if absent';
PRINT N'ADD ApproverLevel INT NULL if absent';
PRINT N'ADD ActionType NVARCHAR(30) NULL if absent';
PRINT N'ADD DetailVersion INT NULL if absent';
PRINT N'FK_ApprovalHistory_Detail WITH CHECK only if non-null orphans = 0';

IF @DryRun = 1
BEGIN
    PRINT N'DryRun=1 — backup and DDL skipped.';
    RETURN;
END;

/* =========================================================================
   1) BACKUP before any DDL
   ========================================================================= */
IF OBJECT_ID(@BackupTable, N'U') IS NOT NULL
BEGIN
    RAISERROR(N'BLOCKER: backup table already exists for this RunTag. Abort.', 16, 1);
    RETURN;
END;

SET @Sql = N'SELECT * INTO ' + @BackupTable + N' FROM dbo.ApprovalRequestStatusHistory WITH (TABLOCKX);';
EXEC sys.sp_executesql @Sql;
PRINT N'Backup created: ' + @BackupTable;

/* =========================================================================
   2) COLUMNS — additive, nullable, only if absent
   ========================================================================= */
IF COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ApprovalRequestDetailId') IS NULL
    ALTER TABLE dbo.ApprovalRequestStatusHistory ADD ApprovalRequestDetailId INT NULL;

IF COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ActivityId') IS NULL
    ALTER TABLE dbo.ApprovalRequestStatusHistory ADD ActivityId INT NULL;

IF COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ApproverLevel') IS NULL
    ALTER TABLE dbo.ApprovalRequestStatusHistory ADD ApproverLevel INT NULL;

IF COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'ActionType') IS NULL
    ALTER TABLE dbo.ApprovalRequestStatusHistory ADD ActionType NVARCHAR(30) NULL;

IF COL_LENGTH(N'dbo.ApprovalRequestStatusHistory', N'DetailVersion') IS NULL
    ALTER TABLE dbo.ApprovalRequestStatusHistory ADD DetailVersion INT NULL;

PRINT N'Column step complete. Existing rows were not updated.';

DECLARE @AfterCnt INT = (SELECT COUNT(*) FROM dbo.ApprovalRequestStatusHistory);
IF @AfterCnt <> @BeforeCnt
BEGIN
    RAISERROR(N'BLOCKER: row count changed during DDL (%d -> %d). Abort before FK.', 16, 1, @BeforeCnt, @AfterCnt);
    RETURN;
END;

/* =========================================================================
   3) FK — WITH CHECK only when no non-null orphans
   ========================================================================= */
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_ApprovalHistory_Detail'
      AND parent_object_id = OBJECT_ID(N'dbo.ApprovalRequestStatusHistory')
)
BEGIN
    PRINT N'FK_ApprovalHistory_Detail already present — not recreated.';
END
ELSE
BEGIN
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

    PRINT N'Non-null ApprovalRequestDetailId orphans=' + CAST(@Orphans AS NVARCHAR(20));

    IF @Orphans = 0
    BEGIN
        SET @Sql = N'
            ALTER TABLE dbo.ApprovalRequestStatusHistory WITH CHECK
                ADD CONSTRAINT FK_ApprovalHistory_Detail
                FOREIGN KEY (ApprovalRequestDetailId)
                REFERENCES dbo.SalesApprovalRequestDetail (ApprovalRequestDetailId);';
        EXEC sys.sp_executesql @Sql;
        PRINT N'FK_ApprovalHistory_Detail created WITH CHECK.';
    END
    ELSE
    BEGIN
        PRINT N'FK NOT CREATED. Orphan ApprovalRequestDetailId values:';
        SET @Sql = N'
            SELECT h.HistoryId, h.ApprovalRequestId, h.ApprovalRequestDetailId
            FROM dbo.ApprovalRequestStatusHistory h
            WHERE h.ApprovalRequestDetailId IS NOT NULL
              AND NOT EXISTS (
                  SELECT 1
                  FROM dbo.SalesApprovalRequestDetail d
                  WHERE d.ApprovalRequestDetailId = h.ApprovalRequestDetailId
              )
            ORDER BY h.HistoryId;';
        EXEC sys.sp_executesql @Sql;
        PRINT N'Validation must FAIL until these orphans are resolved. WITH NOCHECK was not used.';
    END
END;

PRINT N'Backup for rollback reference: ' + @BackupTable;
PRINT N'Next: 022b_RC018_P3_T07_ApprovalRequestStatusHistory_Validate.sql';
GO
