/* ============================================================================
   NAME:        RC-018 Phase 1 / Table 1 — FrequencyTypes MERGE
   WHEN:        2026-09-24
   SOURCE:      docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md
   TARGET:      LIVE dbo.FrequencyTypes (AnmolDristi)
   AUTHORITY:   UAT AnmolDristiDev shared columns only
   WHAT:        Idempotent MERGE of FrequencyID + FrequencyDescription.
   PRESERVE:    LIVE FrequencyFactor (never UPDATE / DROP / NULL-out).
   FORBIDDEN:   DELETE, TRUNCATE, DROP COLUMN, reseeding identity.
   NOTE:        DO NOT EXECUTE until Production Gate review. Script only.
   USAGE:       Set @DryRun = 0 after review to apply. Re-run is safe (idempotent).
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

/* -------------------------------------------------------------------------
   Single-batch apply so @DryRun / @RunTag stay in scope.
--------------------------------------------------------------------------- */
DECLARE @DryRun BIT = 1;  /* 1 = preview only (no backup); 0 = backup + apply MERGE */

DECLARE @RunTag      NVARCHAR(32) = CONVERT(CHAR(8), GETDATE(), 112)
                                 + N'_' + REPLACE(CONVERT(CHAR(8), GETDATE(), 108), N':', N'');
DECLARE @BackupTable SYSNAME      = N'dbo.RC018_Backup_FrequencyTypes_' + @RunTag;
DECLARE @Sql         NVARCHAR(MAX);
DECLARE @HasFactor   BIT = CASE WHEN COL_LENGTH(N'dbo.FrequencyTypes', N'FrequencyFactor') IS NOT NULL THEN 1 ELSE 0 END;
DECLARE @Inserted    INT = 0;
DECLARE @Updated     INT = 0;
DECLARE @Unchanged   INT = 0;

PRINT N'RC-018 P1-T01 FrequencyTypes | RunTag=' + @RunTag
    + N' | DryRun=' + CAST(@DryRun AS NVARCHAR(1));

/* =========================================================================
   0) PREFLIGHT
   ========================================================================= */
IF OBJECT_ID(N'dbo.FrequencyTypes', N'U') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: dbo.FrequencyTypes not found on LIVE. Abort.', 16, 1);
    RETURN;
END;

IF COL_LENGTH(N'dbo.FrequencyTypes', N'FrequencyID') IS NULL
   OR COL_LENGTH(N'dbo.FrequencyTypes', N'FrequencyDescription') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: shared columns FrequencyID / FrequencyDescription missing. Abort.', 16, 1);
    RETURN;
END;

PRINT N'Preflight: FrequencyFactor present on LIVE = '
    + CASE WHEN @HasFactor = 1 THEN N'YES (will be preserved)' ELSE N'NO' END;

/* Guard: never drop FrequencyFactor even if someone adds DROP logic later */
IF @HasFactor = 0
    PRINT N'WARNING: FrequencyFactor not present — schema retain rule N/A for this env.';

/* =========================================================================
   1) UAT SOURCE (Phase 0 certified — FrequencyID 1..6 descriptions match LIVE)
   ========================================================================= */
IF OBJECT_ID(N'tempdb..#UatFrequencyTypes') IS NOT NULL
    DROP TABLE #UatFrequencyTypes;

CREATE TABLE #UatFrequencyTypes
(
    FrequencyID          INT          NOT NULL PRIMARY KEY,
    FrequencyDescription VARCHAR(255) NOT NULL
);

INSERT INTO #UatFrequencyTypes (FrequencyID, FrequencyDescription) VALUES
 (1, 'TWICE IN A SHIFT'),
 (2, 'ONCE IN A SHIFT'),
 (3, '2 HOURS INTERVAL'),
 (4, 'BEFORE EVERY DESPATCH'),
 (5, 'AS PER REQUIREMENT'),
 (6, 'ONCE IN EVERY SHIFT');

PRINT N'UAT source loaded: 6 rows.';

PRINT N'--- PREVIEW: would INSERT ---';
SELECT u.FrequencyID, u.FrequencyDescription
FROM #UatFrequencyTypes u
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.FrequencyTypes l WHERE l.FrequencyID = u.FrequencyID
);

PRINT N'--- PREVIEW: would UPDATE (description drift) ---';
SELECT
      l.FrequencyID
    , l.FrequencyDescription AS LIVE_Description
    , u.FrequencyDescription AS UAT_Description
FROM dbo.FrequencyTypes l
INNER JOIN #UatFrequencyTypes u ON u.FrequencyID = l.FrequencyID
WHERE ISNULL(l.FrequencyDescription, '') <> ISNULL(u.FrequencyDescription, '');

PRINT N'--- PREVIEW: LIVE-only rows (RETAINED; no DELETE) ---';
SELECT l.FrequencyID, l.FrequencyDescription
FROM dbo.FrequencyTypes l
WHERE NOT EXISTS (
    SELECT 1 FROM #UatFrequencyTypes u WHERE u.FrequencyID = l.FrequencyID
);

PRINT N'--- PREVIEW: FrequencyFactor snapshot (must be unchanged after apply) ---';
IF @HasFactor = 1
BEGIN
    SELECT FrequencyID, FrequencyFactor
    FROM dbo.FrequencyTypes
    ORDER BY FrequencyID;
END;

IF @DryRun = 1
BEGIN
    PRINT N'DryRun=1 — MERGE skipped. No backup created. Set @DryRun = 0 to apply.';
    RETURN;
END;

/* =========================================================================
   2) BACKUP (apply path only; enables rollback)
      Created only when @DryRun = 0 and this batch is entering MERGE.
   ========================================================================= */
IF OBJECT_ID(@BackupTable, N'U') IS NOT NULL
BEGIN
    RAISERROR(N'BLOCKER: backup table already exists for this RunTag. Abort.', 16, 1);
    RETURN;
END;

SET @Sql = N'SELECT * INTO ' + @BackupTable + N' FROM dbo.FrequencyTypes WITH (TABLOCKX);';
EXEC sys.sp_executesql @Sql;

SET @Sql = N'SELECT N''BACKUP'' AS Stage, COUNT(*) AS [RowCount] FROM ' + @BackupTable + N';';
EXEC sys.sp_executesql @Sql;
PRINT N'Backup created: ' + @BackupTable;

/* =========================================================================
   3) MERGE APPLY
   ========================================================================= */
BEGIN TRY
    BEGIN TRAN;

    DECLARE @ActionLog TABLE
    (
        MergeAction  NVARCHAR(10) NOT NULL,
        FrequencyID  INT          NOT NULL
    );

    SET IDENTITY_INSERT dbo.FrequencyTypes ON;

    MERGE dbo.FrequencyTypes AS tgt
    USING #UatFrequencyTypes AS src
        ON tgt.FrequencyID = src.FrequencyID
    WHEN MATCHED
         AND ISNULL(tgt.FrequencyDescription, '') <> ISNULL(src.FrequencyDescription, '')
        THEN UPDATE SET
            tgt.FrequencyDescription = src.FrequencyDescription
            /* FrequencyFactor omitted on purpose */
    WHEN NOT MATCHED BY TARGET
        THEN INSERT (FrequencyID, FrequencyDescription)
             VALUES (src.FrequencyID, src.FrequencyDescription)
    /* WHEN NOT MATCHED BY SOURCE omitted — never DELETE */
    OUTPUT $action, inserted.FrequencyID INTO @ActionLog (MergeAction, FrequencyID);

    SET IDENTITY_INSERT dbo.FrequencyTypes OFF;

    SELECT @Inserted = COUNT(*) FROM @ActionLog WHERE MergeAction = N'INSERT';
    SELECT @Updated  = COUNT(*) FROM @ActionLog WHERE MergeAction = N'UPDATE';
    SELECT @Unchanged = (SELECT COUNT(*) FROM #UatFrequencyTypes) - @Inserted - @Updated;

    /* Post-apply: FrequencyFactor must equal backup for every pre-existing ID */
    IF @HasFactor = 1
    BEGIN
        SET @Sql = N'
            IF EXISTS (
                SELECT 1
                FROM dbo.FrequencyTypes c
                INNER JOIN ' + @BackupTable + N' b ON b.FrequencyID = c.FrequencyID
                WHERE ISNULL(c.FrequencyFactor, -2147483648) <> ISNULL(b.FrequencyFactor, -2147483648)
            )
            BEGIN
                RAISERROR(N''BLOCKER: FrequencyFactor changed during MERGE. Rolling back.'', 16, 1);
            END';
        EXEC sys.sp_executesql @Sql;
    END;

    COMMIT TRAN;

    PRINT N'MERGE committed.';
    PRINT N'Inserted=' + CAST(@Inserted AS NVARCHAR(10))
        + N' Updated=' + CAST(@Updated AS NVARCHAR(10))
        + N' Unchanged(source)=' + CAST(@Unchanged AS NVARCHAR(10));
    PRINT N'Backup for rollback: ' + @BackupTable;
    PRINT N'Next: run 016b_RC018_P1_T01_FrequencyTypes_Validate.sql';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;

    BEGIN TRY
        SET IDENTITY_INSERT dbo.FrequencyTypes OFF;
    END TRY
    BEGIN CATCH
    END CATCH;

    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'RC-018 P1-T01 FAILED: %s', 16, 1, @Err);
END CATCH;
GO
