/* ============================================================================
   NAME:        RC-018 Phase 1 / Table 2 — MST_Calendar ROLLBACK
   WHEN:        2026-09-24
   TARGET:      LIVE dbo.MST_Calendar (AnmolDristi)
   WHAT:        Undo FinancialWeek data (and optionally DROP column) using
                RC018_Backup_MST_Calendar_<RunTag> from the patch script.
   PRESERVE:    All LIVE-only columns and indexes. Never DROP them.
   NOTE:        Set @BackupTable and @Confirm=1. Set @DropFinancialWeek=1 only
                if this RC added the column and it must be removed.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @BackupTable       SYSNAME = N'dbo.RC018_Backup_MST_Calendar_YYYYMMDD_HHMMSS'; /* <-- replace */
DECLARE @Confirm           BIT     = 0;  /* must be 1 to execute */
DECLARE @DropFinancialWeek BIT     = 0;  /* 1 = DROP COLUMN after restoring NULLs */

PRINT N'RC-018 P1-T02 MST_Calendar ROLLBACK | Backup=' + @BackupTable
    + N' | DropFW=' + CAST(@DropFinancialWeek AS NVARCHAR(1));

IF @Confirm <> 1
BEGIN
    PRINT N'Confirm=0 — no changes. Set @Confirm=1 after review.';
    RETURN;
END;

IF OBJECT_ID(@BackupTable, N'U') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: backup table not found. Abort.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'dbo.MST_Calendar', N'U') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: dbo.MST_Calendar missing. Abort.', 16, 1);
    RETURN;
END;

BEGIN TRY
    BEGIN TRAN;

    /* Safety copy of current state */
    DECLARE @Safety SYSNAME = N'dbo.RC018_PreRollback_MST_Calendar_'
        + CONVERT(CHAR(8), GETDATE(), 112)
        + N'_' + REPLACE(CONVERT(CHAR(8), GETDATE(), 108), N':', N'');
    DECLARE @Sql NVARCHAR(MAX) =
        N'SELECT * INTO ' + @Safety + N' FROM dbo.MST_Calendar;';
    EXEC sys.sp_executesql @Sql;
    PRINT N'Pre-rollback safety copy: ' + @Safety;

    /* Restore FinancialWeek from backup when backup has the column */
    IF COL_LENGTH(N'dbo.MST_Calendar', N'FinancialWeek') IS NOT NULL
       AND COL_LENGTH(@BackupTable, N'FinancialWeek') IS NOT NULL
    BEGIN
        SET @Sql = N'
            UPDATE tgt
            SET tgt.FinancialWeek = src.FinancialWeek
            FROM dbo.MST_Calendar AS tgt
            INNER JOIN ' + @BackupTable + N' AS src
                ON src.CalendarDate = tgt.CalendarDate;';
        EXEC sys.sp_executesql @Sql;
        PRINT N'FinancialWeek values restored from backup.';
    END
    ELSE IF COL_LENGTH(N'dbo.MST_Calendar', N'FinancialWeek') IS NOT NULL
            AND COL_LENGTH(@BackupTable, N'FinancialWeek') IS NULL
    BEGIN
        /* Backup taken before ADD — restore means NULL all, then optional DROP */
        UPDATE dbo.MST_Calendar SET FinancialWeek = NULL;
        PRINT N'Backup predates column — FinancialWeek set NULL on all rows.';
    END;

    /* Guard: LIVE-only columns unchanged vs backup */
    SET @Sql = N'
        IF EXISTS (
            SELECT 1
            FROM dbo.MST_Calendar c
            INNER JOIN ' + @BackupTable + N' b ON b.CalendarDate = c.CalendarDate
            WHERE ISNULL(c.IsHoliday, 0) <> ISNULL(b.IsHoliday, 0)
               OR ISNULL(c.IsWorkingDay, 0) <> ISNULL(b.IsWorkingDay, 0)
               OR ISNULL(c.DayOfYear, -1) <> ISNULL(b.DayOfYear, -1)
               OR ISNULL(c.YearMonthKey, -1) <> ISNULL(b.YearMonthKey, -1)
        )
        BEGIN
            RAISERROR(N''BLOCKER: LIVE-only column drift detected during rollback.'', 16, 1);
        END';
    EXEC sys.sp_executesql @Sql;

    DECLARE @Cnt INT = (SELECT COUNT(*) FROM dbo.MST_Calendar);
    IF @Cnt <> 7671
        PRINT N'WARNING: row count is ' + CAST(@Cnt AS NVARCHAR(20)) + N' (Phase 0 expected 7671).';

    COMMIT TRAN;

    /* DROP COLUMN outside the data transaction if requested */
    IF @DropFinancialWeek = 1
       AND COL_LENGTH(N'dbo.MST_Calendar', N'FinancialWeek') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.MST_Calendar DROP COLUMN FinancialWeek;
        PRINT N'DROP COLUMN FinancialWeek completed.';
    END;

    PRINT N'ROLLBACK finished. Safety copy: ' + @Safety;
    PRINT N'Re-run 017b validation.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'RC-018 P1-T02 ROLLBACK FAILED: %s', 16, 1, @Err);
END CATCH;
GO
