/* ============================================================================
   NAME:        RC-018 Phase 1 / Table 4 — MST_Plant_Lines
   WHEN:        2026-09-25
   SOURCE:      docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md
   TARGET:      LIVE dbo.MST_Plant_Lines (AnmolDristi)
   FINDING:     Shared attributes MATCH UAT (0 drift, checksum equal).
                Package is verification-first. MERGE runs only if drift > 0.
   PRESERVE:    PK_MST_Plant_Lines, UQ_line_id, IX_MST_Plant_Lines,
                IX_MST_Plant_Lines_Report (LIVE-only). Never DROP indexes.
   FORBIDDEN:   DELETE, TRUNCATE, DROP TABLE, DROP INDEX, recreate.
   NOTE:        DO NOT EXECUTE until Production Gate. Default @DryRun = 1.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @DryRun BIT = 1;  /* 1 = preview + drift report; 0 = backup (+ MERGE only if drift) */

DECLARE @RunTag      NVARCHAR(32) = CONVERT(CHAR(8), GETDATE(), 112)
                                 + N'_' + REPLACE(CONVERT(CHAR(8), GETDATE(), 108), N':', N'');
DECLARE @BackupTable SYSNAME      = N'dbo.RC018_Backup_MST_Plant_Lines_' + @RunTag;
DECLARE @Sql         NVARCHAR(MAX);
DECLARE @LiveCnt     INT;
DECLARE @UatCnt      INT;
DECLARE @UatOnly     INT;
DECLARE @LiveOnly    INT;
DECLARE @Drift       INT;
DECLARE @Inserted    INT = 0;
DECLARE @Updated     INT = 0;

PRINT N'RC-018 P1-T04 MST_Plant_Lines | RunTag=' + @RunTag
    + N' | DryRun=' + CAST(@DryRun AS NVARCHAR(1));

/* =========================================================================
   0) PREFLIGHT
   ========================================================================= */
IF OBJECT_ID(N'dbo.MST_Plant_Lines', N'U') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: dbo.MST_Plant_Lines not found on LIVE. Abort.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'AnmolDristiDev.dbo.MST_Plant_Lines', N'U') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: UAT AnmolDristiDev.dbo.MST_Plant_Lines not reachable. Abort.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.MST_Plant_Lines')
      AND name = N'IX_MST_Plant_Lines_Report'
)
BEGIN
    RAISERROR(N'BLOCKER: LIVE index IX_MST_Plant_Lines_Report missing. Abort (retain rule).', 16, 1);
    RETURN;
END;

SELECT @LiveCnt = COUNT(*) FROM dbo.MST_Plant_Lines;
SELECT @UatCnt  = COUNT(*) FROM AnmolDristiDev.dbo.MST_Plant_Lines;
PRINT N'Counts | LIVE=' + CAST(@LiveCnt AS NVARCHAR(20))
    + N' UAT=' + CAST(@UatCnt AS NVARCHAR(20));

SELECT @UatOnly = COUNT(*)
FROM AnmolDristiDev.dbo.MST_Plant_Lines u
WHERE NOT EXISTS (SELECT 1 FROM dbo.MST_Plant_Lines l WHERE l.line_id = u.line_id);

SELECT @LiveOnly = COUNT(*)
FROM dbo.MST_Plant_Lines l
WHERE NOT EXISTS (SELECT 1 FROM AnmolDristiDev.dbo.MST_Plant_Lines u WHERE u.line_id = l.line_id);

SELECT @Drift = COUNT(*)
FROM AnmolDristiDev.dbo.MST_Plant_Lines u
INNER JOIN dbo.MST_Plant_Lines l ON l.line_id = u.line_id
WHERE ISNULL(u.id, -1) <> ISNULL(l.id, -1)
   OR ISNULL(u.plant_id, -1) <> ISNULL(l.plant_id, -1)
   OR ISNULL(u.line_name, N'') <> ISNULL(l.line_name, N'')
   OR ISNULL(u.local_name, N'') <> ISNULL(l.local_name, N'')
   OR ISNULL(u.line_sap_code, '') <> ISNULL(l.line_sap_code, '')
   OR ISNULL(u.added_on, '19000101') <> ISNULL(l.added_on, '19000101')
   OR ISNULL(u.view_status, -1) <> ISNULL(l.view_status, -1)
   OR ISNULL(u.delete_status, -1) <> ISNULL(l.delete_status, -1);

PRINT N'Key delta | UAT_only=' + CAST(@UatOnly AS NVARCHAR(20))
    + N' LIVE_only=' + CAST(@LiveOnly AS NVARCHAR(20))
    + N' SharedAttrDrift=' + CAST(@Drift AS NVARCHAR(20));

IF @UatOnly = 0 AND @LiveOnly = 0 AND @Drift = 0
    PRINT N'MODE: verification-only (Phase 0 + 2026-09-25 probe: no shared drift). MERGE will not run.';
ELSE
    PRINT N'MODE: drift detected — MERGE will run when @DryRun = 0. LIVE-only keys are retained (no DELETE).';

IF @DryRun = 1
BEGIN
    IF @Drift > 0 OR @UatOnly > 0
    BEGIN
        PRINT N'--- PREVIEW: shared drift sample ---';
        SELECT TOP 20
              l.line_id
            , l.line_name AS LIVE_name
            , u.line_name AS UAT_name
            , l.view_status AS LIVE_view
            , u.view_status AS UAT_view
            , l.delete_status AS LIVE_del
            , u.delete_status AS UAT_del
        FROM AnmolDristiDev.dbo.MST_Plant_Lines u
        INNER JOIN dbo.MST_Plant_Lines l ON l.line_id = u.line_id
        WHERE ISNULL(u.id, -1) <> ISNULL(l.id, -1)
           OR ISNULL(u.plant_id, -1) <> ISNULL(l.plant_id, -1)
           OR ISNULL(u.line_name, N'') <> ISNULL(l.line_name, N'')
           OR ISNULL(u.local_name, N'') <> ISNULL(l.local_name, N'')
           OR ISNULL(u.line_sap_code, '') <> ISNULL(l.line_sap_code, '')
           OR ISNULL(u.added_on, '19000101') <> ISNULL(l.added_on, '19000101')
           OR ISNULL(u.view_status, -1) <> ISNULL(l.view_status, -1)
           OR ISNULL(u.delete_status, -1) <> ISNULL(l.delete_status, -1);
    END
    PRINT N'DryRun=1 — backup and MERGE skipped. Set @DryRun=0 to backup (and MERGE only if drift).';
    PRINT N'Next: 019b_RC018_P1_T04_MST_Plant_Lines_Validate.sql';
    RETURN;
END;

/* =========================================================================
   1) BACKUP (always on apply — even when verification-only)
   ========================================================================= */
IF OBJECT_ID(@BackupTable, N'U') IS NOT NULL
BEGIN
    RAISERROR(N'BLOCKER: backup table already exists for this RunTag. Abort.', 16, 1);
    RETURN;
END;

SET @Sql = N'SELECT * INTO ' + @BackupTable + N' FROM dbo.MST_Plant_Lines WITH (TABLOCKX);';
EXEC sys.sp_executesql @Sql;
PRINT N'Backup created: ' + @BackupTable;

IF @UatOnly = 0 AND @LiveOnly = 0 AND @Drift = 0
BEGIN
    PRINT N'Verification-only complete. No MERGE. Indexes untouched.';
    PRINT N'Next: 019b_RC018_P1_T04_MST_Plant_Lines_Validate.sql';
    RETURN;
END;

/* =========================================================================
   2) MERGE — only when UAT-missing keys or shared attribute drift exist
   ========================================================================= */
IF EXISTS (
    SELECT 1
    FROM AnmolDristiDev.dbo.MST_Plant_Lines u
    INNER JOIN dbo.MST_Plant_Lines l ON l.line_id = u.line_id
    WHERE ISNULL(u.id, -1) <> ISNULL(l.id, -1)
)
BEGIN
    RAISERROR(N'BLOCKER: identity id differs for a shared line_id. Do not rewrite identity. Abort.', 16, 1);
    RETURN;
END;

BEGIN TRY
    BEGIN TRAN;

    IF OBJECT_ID(N'tempdb..#ActionLog') IS NOT NULL DROP TABLE #ActionLog;
    CREATE TABLE #ActionLog
    (
        MergeAction NVARCHAR(10) NOT NULL,
        line_id     INT          NOT NULL
    );

    SET IDENTITY_INSERT dbo.MST_Plant_Lines ON;

    MERGE dbo.MST_Plant_Lines AS tgt
    USING AnmolDristiDev.dbo.MST_Plant_Lines AS src
        ON tgt.line_id = src.line_id
    WHEN MATCHED AND (
            ISNULL(tgt.plant_id, -1) <> ISNULL(src.plant_id, -1)
         OR ISNULL(tgt.line_name, N'') <> ISNULL(src.line_name, N'')
         OR ISNULL(tgt.local_name, N'') <> ISNULL(src.local_name, N'')
         OR ISNULL(tgt.line_sap_code, '') <> ISNULL(src.line_sap_code, '')
         OR ISNULL(tgt.added_on, '19000101') <> ISNULL(src.added_on, '19000101')
         OR ISNULL(tgt.view_status, -1) <> ISNULL(src.view_status, -1)
         OR ISNULL(tgt.delete_status, -1) <> ISNULL(src.delete_status, -1)
        )
        THEN UPDATE SET
              tgt.plant_id      = src.plant_id
            , tgt.line_name     = src.line_name
            , tgt.local_name    = src.local_name
            , tgt.line_sap_code = src.line_sap_code
            , tgt.added_on      = src.added_on
            , tgt.view_status   = src.view_status
            , tgt.delete_status = src.delete_status
    WHEN NOT MATCHED BY TARGET
        THEN INSERT
             (id, plant_id, line_id, line_name, local_name, line_sap_code, added_on, view_status, delete_status)
             VALUES
             (src.id, src.plant_id, src.line_id, src.line_name, src.local_name, src.line_sap_code, src.added_on, src.view_status, src.delete_status)
    /* WHEN NOT MATCHED BY SOURCE omitted — never DELETE LIVE-only rows */
    OUTPUT $action, inserted.line_id INTO #ActionLog (MergeAction, line_id);

    SET IDENTITY_INSERT dbo.MST_Plant_Lines OFF;

    SELECT @Inserted = COUNT(*) FROM #ActionLog WHERE MergeAction = N'INSERT';
    SELECT @Updated  = COUNT(*) FROM #ActionLog WHERE MergeAction = N'UPDATE';

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.MST_Plant_Lines')
          AND name = N'IX_MST_Plant_Lines_Report'
    )
    BEGIN
        RAISERROR(N'BLOCKER: IX_MST_Plant_Lines_Report disappeared. Rolling back.', 16, 1);
    END;

    COMMIT TRAN;

    PRINT N'MERGE committed. Inserted=' + CAST(@Inserted AS NVARCHAR(10))
        + N' Updated=' + CAST(@Updated AS NVARCHAR(10));
    PRINT N'Backup for rollback: ' + @BackupTable;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    BEGIN TRY SET IDENTITY_INSERT dbo.MST_Plant_Lines OFF; END TRY BEGIN CATCH END CATCH;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'RC-018 P1-T04 FAILED: %s', 16, 1, @Err);
END CATCH;
GO
