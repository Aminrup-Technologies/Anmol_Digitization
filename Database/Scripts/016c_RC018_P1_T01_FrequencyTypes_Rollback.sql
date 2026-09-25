/* ============================================================================
   NAME:        RC-018 Phase 1 / Table 1 — FrequencyTypes ROLLBACK
   WHEN:        2026-09-24
   TARGET:      LIVE dbo.FrequencyTypes (AnmolDristi)
   WHAT:        Restore from RC018_Backup_FrequencyTypes_<RunTag> created by MERGE script.
   PRESERVE:    Restores full row including FrequencyFactor.
   FORBIDDEN:   DROP of FrequencyFactor; DELETE of unrelated tables.
   NOTE:        Set @BackupTable to the exact backup name printed by the MERGE run.
                DO NOT EXECUTE unless a failed apply must be undone.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

/* -------------------------------------------------------------------------
   PARAMETERS — edit before run
--------------------------------------------------------------------------- */
DECLARE @BackupTable SYSNAME = N'dbo.RC018_Backup_FrequencyTypes_YYYYMMDD_HHMMSS'; /* <-- replace */
DECLARE @Confirm     BIT     = 0;  /* must set to 1 to execute */

PRINT N'RC-018 P1-T01 FrequencyTypes ROLLBACK | Backup=' + @BackupTable;

IF @Confirm <> 1
BEGIN
    PRINT N'Confirm=0 — no changes. Set @Confirm=1 and @BackupTable after review.';
    RETURN;
END;

IF OBJECT_ID(@BackupTable, N'U') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: backup table not found. Abort.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'dbo.FrequencyTypes', N'U') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: dbo.FrequencyTypes missing. Abort.', 16, 1);
    RETURN;
END;

BEGIN TRY
    BEGIN TRAN;

    /* Safety copy of current (post-MERGE) state before overwrite */
    DECLARE @Safety SYSNAME = N'dbo.RC018_PreRollback_FrequencyTypes_'
        + CONVERT(CHAR(8), GETDATE(), 112)
        + N'_' + REPLACE(CONVERT(CHAR(8), GETDATE(), 108), N':', N'');
    DECLARE @Sql NVARCHAR(MAX) =
        N'SELECT * INTO ' + @Safety + N' FROM dbo.FrequencyTypes;';
    EXEC sys.sp_executesql @Sql;
    PRINT N'Pre-rollback safety copy: ' + @Safety;

    /*
       Restore strategy (no TRUNCATE):
       1) UPDATE matched rows from backup (all columns present in backup)
       2) INSERT backup rows missing from live (IDENTITY_INSERT)
       3) DELETE live rows that were inserted by MERGE and are absent from backup
          — ONLY keys that exist in safety-vs-backup delta from this RC run.
          Scoped delete of MERGE-inserted orphans only; not a table wipe.
    */

    /* 1) UPDATE shared + FrequencyFactor from backup for existing IDs */
    SET @Sql = N'
        UPDATE tgt
        SET
              tgt.FrequencyDescription = src.FrequencyDescription
            , tgt.FrequencyFactor      = src.FrequencyFactor
        FROM dbo.FrequencyTypes AS tgt
        INNER JOIN ' + @BackupTable + N' AS src
            ON src.FrequencyID = tgt.FrequencyID;';
    EXEC sys.sp_executesql @Sql;

    /* 2) INSERT missing backup rows */
    SET IDENTITY_INSERT dbo.FrequencyTypes ON;
    SET @Sql = N'
        INSERT INTO dbo.FrequencyTypes (FrequencyID, FrequencyDescription, FrequencyFactor)
        SELECT src.FrequencyID, src.FrequencyDescription, src.FrequencyFactor
        FROM ' + @BackupTable + N' AS src
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.FrequencyTypes tgt WHERE tgt.FrequencyID = src.FrequencyID
        );';
    EXEC sys.sp_executesql @Sql;
    SET IDENTITY_INSERT dbo.FrequencyTypes OFF;

    /* 3) Remove rows inserted by MERGE that were not in backup (scoped) */
    SET @Sql = N'
        DELETE tgt
        FROM dbo.FrequencyTypes AS tgt
        WHERE NOT EXISTS (
            SELECT 1 FROM ' + @BackupTable + N' AS src WHERE src.FrequencyID = tgt.FrequencyID
        );';
    EXEC sys.sp_executesql @Sql;

    /* Verify counts match backup */
    DECLARE @LiveCnt INT = (SELECT COUNT(*) FROM dbo.FrequencyTypes);
    DECLARE @BakCnt  INT;
    SET @Sql = N'SELECT @c = COUNT(*) FROM ' + @BackupTable;
    EXEC sys.sp_executesql @Sql, N'@c INT OUTPUT', @c = @BakCnt OUTPUT;

    IF @LiveCnt <> @BakCnt
    BEGIN
        RAISERROR(N'ROLLBACK VERIFY FAIL: live count %d <> backup count %d', 16, 1, @LiveCnt, @BakCnt);
    END;

    COMMIT TRAN;

    PRINT N'ROLLBACK committed. LiveCount=' + CAST(@LiveCnt AS NVARCHAR(20));
    PRINT N'Re-run 016b validation if needed. Safety copy retained: ' + @Safety;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    BEGIN TRY SET IDENTITY_INSERT dbo.FrequencyTypes OFF; END TRY BEGIN CATCH END CATCH;

    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'RC-018 P1-T01 ROLLBACK FAILED: %s', 16, 1, @Err);
END CATCH;
GO
