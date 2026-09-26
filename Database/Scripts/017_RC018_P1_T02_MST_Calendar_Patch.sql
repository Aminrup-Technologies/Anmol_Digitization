/* ============================================================================
   NAME:        RC-018 Phase 1 / Table 2 — MST_Calendar schema + FinancialWeek sync
   WHEN:        2026-09-24
   SOURCE:      docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md
   TARGET:      LIVE dbo.MST_Calendar (AnmolDristi)
   AUTHORITY:   UAT AnmolDristiDev for FinancialWeek values only
   WHAT:        1) ADD FinancialWeek int NULL if missing
                2) Backup then populate FinancialWeek from UAT where LIVE IS NULL
   PRESERVE:    All 13 LIVE-only columns + LIVE indexes (never DROP/ALTER them)
   FORBIDDEN:   DELETE, TRUNCATE, DROP TABLE, recreate, DROP LIVE-only columns
   NOTE:        DO NOT EXECUTE until Production Gate. Default @DryRun = 1.
                FinancialWeek references after ADD use dynamic SQL (batch binding).
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @DryRun BIT = 1;  /* 1 = preview only; 0 = apply DDL + data */

DECLARE @RunTag      NVARCHAR(32) = CONVERT(CHAR(8), GETDATE(), 112)
                                 + N'_' + REPLACE(CONVERT(CHAR(8), GETDATE(), 108), N':', N'');
DECLARE @BackupTable SYSNAME      = N'dbo.RC018_Backup_MST_Calendar_' + @RunTag;
DECLARE @Sql         NVARCHAR(MAX);
DECLARE @HadColumn   BIT = CASE WHEN COL_LENGTH(N'dbo.MST_Calendar', N'FinancialWeek') IS NOT NULL THEN 1 ELSE 0 END;
DECLARE @NullBefore  INT = 0;
DECLARE @Updated     INT = 0;
DECLARE @LiveCnt     INT;
DECLARE @UatCnt      INT;

PRINT N'RC-018 P1-T02 MST_Calendar | RunTag=' + @RunTag
    + N' | DryRun=' + CAST(@DryRun AS NVARCHAR(1))
    + N' | FinancialWeekExists=' + CAST(@HadColumn AS NVARCHAR(1));

/* =========================================================================
   0) PREFLIGHT
   ========================================================================= */
IF OBJECT_ID(N'dbo.MST_Calendar', N'U') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: dbo.MST_Calendar not found on LIVE. Abort.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'AnmolDristiDev.dbo.MST_Calendar', N'U') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: UAT AnmolDristiDev.dbo.MST_Calendar not reachable. Abort.', 16, 1);
    RETURN;
END;

IF COL_LENGTH(N'AnmolDristiDev.dbo.MST_Calendar', N'FinancialWeek') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: UAT FinancialWeek column missing — cannot source values. Abort.', 16, 1);
    RETURN;
END;

IF COL_LENGTH(N'dbo.MST_Calendar', N'DayOfYear') IS NULL
 OR COL_LENGTH(N'dbo.MST_Calendar', N'IsHoliday') IS NULL
 OR COL_LENGTH(N'dbo.MST_Calendar', N'IsWorkingDay') IS NULL
 OR COL_LENGTH(N'dbo.MST_Calendar', N'YearMonthKey') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: unexpected absence of LIVE enrichment columns. Abort.', 16, 1);
    RETURN;
END;

SELECT @LiveCnt = COUNT(*) FROM dbo.MST_Calendar;
SELECT @UatCnt  = COUNT(*) FROM AnmolDristiDev.dbo.MST_Calendar;
PRINT N'Preflight counts | LIVE=' + CAST(@LiveCnt AS NVARCHAR(20))
    + N' UAT=' + CAST(@UatCnt AS NVARCHAR(20));

IF @LiveCnt <> 7671 OR @UatCnt <> 7671
    PRINT N'WARNING: row count differs from Phase 0 baseline (7671). Review before apply.';

/* =========================================================================
   1) SCHEMA — ADD FinancialWeek IF NOT EXISTS
   ========================================================================= */
PRINT N'--- SCHEMA: FinancialWeek ---';
IF COL_LENGTH(N'dbo.MST_Calendar', N'FinancialWeek') IS NULL
BEGIN
    PRINT N'FinancialWeek missing on LIVE — will ADD int NULL.';
    IF @DryRun = 0
    BEGIN
        ALTER TABLE dbo.MST_Calendar ADD FinancialWeek INT NULL;
        PRINT N'ADD COLUMN FinancialWeek completed.';
    END
    ELSE
        PRINT N'DryRun=1 — ADD COLUMN skipped.';
END
ELSE
    PRINT N'FinancialWeek already present — no DDL.';

/* =========================================================================
   2) BACKUP before any data update (apply path only)
   ========================================================================= */
IF @DryRun = 0
BEGIN
    IF OBJECT_ID(@BackupTable, N'U') IS NOT NULL
    BEGIN
        RAISERROR(N'BLOCKER: backup table already exists for this RunTag. Abort.', 16, 1);
        RETURN;
    END;

    SET @Sql = N'SELECT * INTO ' + @BackupTable + N' FROM dbo.MST_Calendar WITH (TABLOCKX);';
    EXEC sys.sp_executesql @Sql;
    PRINT N'Backup created: ' + @BackupTable;

    SET @Sql = N'SELECT N''BACKUP'' AS Stage, COUNT(*) AS [RowCount] FROM ' + @BackupTable;
    EXEC sys.sp_executesql @Sql;
END
ELSE
    PRINT N'DryRun=1 — backup deferred until apply (@DryRun=0).';

/* =========================================================================
   3) DATA — populate FinancialWeek from UAT where LIVE IS NULL only
      (dynamic SQL so newly added column is visible in this batch)
   ========================================================================= */
IF COL_LENGTH(N'dbo.MST_Calendar', N'FinancialWeek') IS NULL
BEGIN
    /* Dry-run path: column not added yet */
    SELECT @NullBefore = COUNT(*)
    FROM dbo.MST_Calendar l
    INNER JOIN AnmolDristiDev.dbo.MST_Calendar u ON u.CalendarDate = l.CalendarDate
    WHERE u.FinancialWeek IS NOT NULL;

    PRINT N'PREVIEW: after ADD, eligible first-fill rows ≈ '
        + CAST(@NullBefore AS NVARCHAR(20));
    PRINT N'DryRun=1 — FinancialWeek UPDATE skipped.';
END
ELSE
BEGIN
    SET @Sql = N'
        SELECT @NullOut = COUNT(*)
        FROM dbo.MST_Calendar
        WHERE FinancialWeek IS NULL;';
    EXEC sys.sp_executesql @Sql, N'@NullOut INT OUTPUT', @NullOut = @NullBefore OUTPUT;

    PRINT N'Rows with FinancialWeek IS NULL (eligible): '
        + CAST(@NullBefore AS NVARCHAR(20));

    PRINT N'--- PREVIEW sample (LIVE NULL ← UAT value) ---';
    SET @Sql = N'
        SELECT TOP 20
              l.CalendarDate
            , l.FinancialWeek AS LIVE_FW
            , u.FinancialWeek AS UAT_FW
        FROM dbo.MST_Calendar l
        INNER JOIN AnmolDristiDev.dbo.MST_Calendar u
            ON u.CalendarDate = l.CalendarDate
        WHERE l.FinancialWeek IS NULL
          AND u.FinancialWeek IS NOT NULL
        ORDER BY l.CalendarDate;';
    EXEC sys.sp_executesql @Sql;

    IF @DryRun = 0 AND @NullBefore > 0
    BEGIN
        BEGIN TRY
            BEGIN TRAN;

            SET @Sql = N'
                UPDATE l
                SET l.FinancialWeek = u.FinancialWeek
                FROM dbo.MST_Calendar AS l
                INNER JOIN AnmolDristiDev.dbo.MST_Calendar AS u
                    ON u.CalendarDate = l.CalendarDate
                WHERE l.FinancialWeek IS NULL
                  AND u.FinancialWeek IS NOT NULL;

                SET @UpdOut = @@ROWCOUNT;';
            EXEC sys.sp_executesql @Sql, N'@UpdOut INT OUTPUT', @UpdOut = @Updated OUTPUT;

            /* Guard: LIVE-only enrichment must be unchanged vs backup */
            SET @Sql = N'
                IF EXISTS (
                    SELECT 1
                    FROM dbo.MST_Calendar c
                    INNER JOIN ' + @BackupTable + N' b ON b.CalendarDate = c.CalendarDate
                    WHERE ISNULL(c.IsHoliday, 0) <> ISNULL(b.IsHoliday, 0)
                       OR ISNULL(c.IsWorkingDay, 0) <> ISNULL(b.IsWorkingDay, 0)
                       OR ISNULL(c.DayOfYear, -1) <> ISNULL(b.DayOfYear, -1)
                       OR ISNULL(c.YearMonthKey, -1) <> ISNULL(b.YearMonthKey, -1)
                       OR ISNULL(c.HolidayName, N'''') <> ISNULL(b.HolidayName, N'''')
                       OR ISNULL(c.Remarks, N'''') <> ISNULL(b.Remarks, N'''')
                )
                BEGIN
                    RAISERROR(N''BLOCKER: LIVE-only column values changed. Rolling back.'', 16, 1);
                END';
            EXEC sys.sp_executesql @Sql;

            COMMIT TRAN;
            PRINT N'DATA UPDATE committed. RowsUpdated=' + CAST(@Updated AS NVARCHAR(20));
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0 ROLLBACK TRAN;
            DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
            RAISERROR(N'RC-018 P1-T02 DATA FAILED: %s', 16, 1, @Err);
            RETURN;
        END CATCH;
    END
    ELSE IF @DryRun = 1
        PRINT N'DryRun=1 — FinancialWeek UPDATE skipped.';
    ELSE
        PRINT N'No NULL FinancialWeek rows — data step is a no-op (idempotent).';
END;

PRINT N'Next: run 017b_RC018_P1_T02_MST_Calendar_Validate.sql';
IF @DryRun = 0
    PRINT N'Backup for rollback: ' + @BackupTable;
GO
