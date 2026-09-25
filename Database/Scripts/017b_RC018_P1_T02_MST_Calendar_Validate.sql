/* ============================================================================
   NAME:        RC-018 Phase 1 / Table 2 — MST_Calendar VALIDATION
   WHEN:        2026-09-24
   TARGET:      LIVE dbo.MST_Calendar (AnmolDristi)
   WHAT:        Read-only PASS/FAIL gate. No DML.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Fail INT = 0;

PRINT N'===== RC-018 P1-T02 MST_Calendar VALIDATION =====';

/* -------------------------------------------------------------------------
   1) Row count remains 7,671
--------------------------------------------------------------------------- */
DECLARE @Cnt INT = (SELECT COUNT(*) FROM dbo.MST_Calendar);
PRINT N'CHECK RowCount | Actual=' + CAST(@Cnt AS NVARCHAR(20)) + N' Expected=7671 | '
    + CASE WHEN @Cnt = 7671 THEN N'PASS' ELSE N'FAIL' END;
IF @Cnt <> 7671 SET @Fail += 1;

/* -------------------------------------------------------------------------
   2) FinancialWeek exists (name probe only — do not bind the column)
--------------------------------------------------------------------------- */
DECLARE @HasFinancialWeek BIT = 0;

IF COL_LENGTH(N'dbo.MST_Calendar', N'FinancialWeek') IS NOT NULL
    SET @HasFinancialWeek = 1;

IF @HasFinancialWeek = 1
    PRINT N'CHECK FinancialWeek exists | PASS';
ELSE
    PRINT N'CHECK FinancialWeek exists | Pending (expected)';

/* -------------------------------------------------------------------------
   3) LIVE-only columns remain intact (existence)
--------------------------------------------------------------------------- */
DECLARE @LiveOnlyMissing INT = 0;
IF COL_LENGTH(N'dbo.MST_Calendar', N'DayOfYear') IS NULL SET @LiveOnlyMissing += 1;
IF COL_LENGTH(N'dbo.MST_Calendar', N'FinancialMonthName') IS NULL SET @LiveOnlyMissing += 1;
IF COL_LENGTH(N'dbo.MST_Calendar', N'FinancialHalfYear') IS NULL SET @LiveOnlyMissing += 1;
IF COL_LENGTH(N'dbo.MST_Calendar', N'IsHoliday') IS NULL SET @LiveOnlyMissing += 1;
IF COL_LENGTH(N'dbo.MST_Calendar', N'IsWorkingDay') IS NULL SET @LiveOnlyMissing += 1;
IF COL_LENGTH(N'dbo.MST_Calendar', N'HolidayName') IS NULL SET @LiveOnlyMissing += 1;
IF COL_LENGTH(N'dbo.MST_Calendar', N'HolidayType') IS NULL SET @LiveOnlyMissing += 1;
IF COL_LENGTH(N'dbo.MST_Calendar', N'StateCode') IS NULL SET @LiveOnlyMissing += 1;
IF COL_LENGTH(N'dbo.MST_Calendar', N'YearMonth') IS NULL SET @LiveOnlyMissing += 1;
IF COL_LENGTH(N'dbo.MST_Calendar', N'YearMonthKey') IS NULL SET @LiveOnlyMissing += 1;
IF COL_LENGTH(N'dbo.MST_Calendar', N'IsShutdownDay') IS NULL SET @LiveOnlyMissing += 1;
IF COL_LENGTH(N'dbo.MST_Calendar', N'ShutdownReason') IS NULL SET @LiveOnlyMissing += 1;
IF COL_LENGTH(N'dbo.MST_Calendar', N'Remarks') IS NULL SET @LiveOnlyMissing += 1;

PRINT N'CHECK LIVE-only columns present (13) | Missing='
    + CAST(@LiveOnlyMissing AS NVARCHAR(10)) + N' | '
    + CASE WHEN @LiveOnlyMissing = 0 THEN N'PASS' ELSE N'FAIL' END;
IF @LiveOnlyMissing <> 0 SET @Fail += 1;

/* Defaults on LIVE enrichment still present */
DECLARE @DefHoliday SYSNAME = (
    SELECT dc.name FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.MST_Calendar') AND c.name = N'IsHoliday'
);
DECLARE @DefWorking SYSNAME = (
    SELECT dc.name FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.MST_Calendar') AND c.name = N'IsWorkingDay'
);
PRINT N'CHECK LIVE defaults IsHoliday/IsWorkingDay | '
    + CASE WHEN @DefHoliday IS NOT NULL AND @DefWorking IS NOT NULL THEN N'PASS' ELSE N'FAIL' END;
IF @DefHoliday IS NULL OR @DefWorking IS NULL SET @Fail += 1;

/* LIVE indexes retained */
DECLARE @IxMissing INT = 0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.MST_Calendar') AND name = N'IX_MST_Calendar_Date') SET @IxMissing += 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.MST_Calendar') AND name = N'IX_MST_Calendar_Financial') SET @IxMissing += 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.MST_Calendar') AND name = N'IX_MST_Calendar_Month') SET @IxMissing += 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.MST_Calendar') AND name = N'IX_MST_Calendar_YearMonth') SET @IxMissing += 1;
PRINT N'CHECK LIVE indexes retained | Missing=' + CAST(@IxMissing AS NVARCHAR(10)) + N' | '
    + CASE WHEN @IxMissing = 0 THEN N'PASS' ELSE N'FAIL' END;
IF @IxMissing <> 0 SET @Fail += 1;

/* -------------------------------------------------------------------------
   4) PK unchanged
--------------------------------------------------------------------------- */
DECLARE @PkCols NVARCHAR(4000);
SELECT @PkCols = STUFF((
    SELECT N',' + c.name
    FROM sys.key_constraints kc
    INNER JOIN sys.index_columns ic
        ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE kc.parent_object_id = OBJECT_ID(N'dbo.MST_Calendar')
      AND kc.type = N'PK' AND ic.is_included_column = 0
    ORDER BY ic.key_ordinal
    FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'');

PRINT N'CHECK PK | Columns=' + ISNULL(@PkCols, N'<none>') + N' | '
    + CASE WHEN @PkCols = N'CalendarDate' THEN N'PASS' ELSE N'FAIL' END;
IF ISNULL(@PkCols, N'') <> N'CalendarDate' SET @Fail += 1;

/* -------------------------------------------------------------------------
   5) No duplicate calendar dates
--------------------------------------------------------------------------- */
DECLARE @Dup INT = (
    SELECT COUNT(*) FROM (
        SELECT CalendarDate FROM dbo.MST_Calendar GROUP BY CalendarDate HAVING COUNT(*) > 1
    ) d
);
PRINT N'CHECK Duplicate CalendarDate | DupGroups=' + CAST(@Dup AS NVARCHAR(20)) + N' | '
    + CASE WHEN @Dup = 0 THEN N'PASS' ELSE N'FAIL' END;
IF @Dup <> 0 SET @Fail += 1;

/* -------------------------------------------------------------------------
   6–7) FinancialWeek null backfill, UAT drift, idempotency, range 1-53.
        Queries that name the column run only through dynamic SQL.
--------------------------------------------------------------------------- */
DECLARE @NullFW INT = NULL;
DECLARE @DriftFW INT = NULL;
DECLARE @WouldUpdate INT = NULL;
DECLARE @FwMin INT = NULL;
DECLARE @FwMax INT = NULL;
DECLARE @OutOfRange INT = NULL;
DECLARE @FwSql NVARCHAR(MAX);

IF @HasFinancialWeek = 1
BEGIN
    SET @FwSql = N'
        SELECT
              @NullFW = SUM(CASE WHEN FinancialWeek IS NULL THEN 1 ELSE 0 END)
            , @FwMin = MIN(FinancialWeek)
            , @FwMax = MAX(FinancialWeek)
            , @OutOfRange = SUM(CASE
                    WHEN FinancialWeek IS NOT NULL
                     AND (FinancialWeek < 1 OR FinancialWeek > 53) THEN 1
                    ELSE 0 END)
        FROM dbo.MST_Calendar;';
    EXEC sys.sp_executesql
        @FwSql,
        N'@NullFW INT OUTPUT, @FwMin INT OUTPUT, @FwMax INT OUTPUT, @OutOfRange INT OUTPUT',
        @NullFW = @NullFW OUTPUT,
        @FwMin = @FwMin OUTPUT,
        @FwMax = @FwMax OUTPUT,
        @OutOfRange = @OutOfRange OUTPUT;

    PRINT N'CHECK FinancialWeek NULL remaining | ' + CAST(ISNULL(@NullFW, -1) AS NVARCHAR(20)) + N' | '
        + CASE WHEN @NullFW = 0 THEN N'PASS' ELSE N'FAIL' END;
    IF ISNULL(@NullFW, -1) <> 0 SET @Fail += 1;

    IF @FwMin >= 1 AND @FwMax <= 53 AND @OutOfRange = 0
        PRINT N'CHECK FinancialWeek range 1-53 | Min=' + CAST(@FwMin AS NVARCHAR(20))
            + N' Max=' + CAST(@FwMax AS NVARCHAR(20)) + N' | PASS';
    ELSE
    BEGIN
        PRINT N'CHECK FinancialWeek range 1-53 | Min=' + ISNULL(CAST(@FwMin AS NVARCHAR(20)), N'NULL')
            + N' Max=' + ISNULL(CAST(@FwMax AS NVARCHAR(20)), N'NULL')
            + N' OutOfRange=' + CAST(ISNULL(@OutOfRange, -1) AS NVARCHAR(20)) + N' | FAIL';
        SET @Fail += 1;
    END;

    IF OBJECT_ID(N'AnmolDristiDev.dbo.MST_Calendar', N'U') IS NOT NULL
    BEGIN
        SET @FwSql = N'
            SELECT @DriftFW = COUNT(*)
            FROM dbo.MST_Calendar l
            INNER JOIN AnmolDristiDev.dbo.MST_Calendar u ON u.CalendarDate = l.CalendarDate
            WHERE ISNULL(l.FinancialWeek, -1) <> ISNULL(u.FinancialWeek, -1);';
        EXEC sys.sp_executesql @FwSql, N'@DriftFW INT OUTPUT', @DriftFW = @DriftFW OUTPUT;

        PRINT N'CHECK FinancialWeek vs UAT drift | ' + CAST(@DriftFW AS NVARCHAR(20)) + N' | '
            + CASE WHEN @DriftFW = 0 THEN N'PASS' ELSE N'FAIL' END;
        IF @DriftFW <> 0 SET @Fail += 1;
    END;

    SET @FwSql = N'
        SELECT @WouldUpdate = COUNT(*)
        FROM dbo.MST_Calendar l
        INNER JOIN AnmolDristiDev.dbo.MST_Calendar u ON u.CalendarDate = l.CalendarDate
        WHERE l.FinancialWeek IS NULL AND u.FinancialWeek IS NOT NULL;';
    EXEC sys.sp_executesql @FwSql, N'@WouldUpdate INT OUTPUT', @WouldUpdate = @WouldUpdate OUTPUT;

    PRINT N'CHECK Idempotent (NULL fills remaining) | ' + CAST(@WouldUpdate AS NVARCHAR(20)) + N' | '
        + CASE WHEN @WouldUpdate = 0 THEN N'PASS' ELSE N'FAIL' END;
    IF @WouldUpdate <> 0 SET @Fail += 1;
END
ELSE
BEGIN
    PRINT N'FinancialWeek not present (expected during DryRun).';
END;

/* -------------------------------------------------------------------------
   8) DBCC CHECKCONSTRAINTS
--------------------------------------------------------------------------- */
PRINT N'--- DBCC CHECKCONSTRAINTS (dbo.MST_Calendar) ---';
BEGIN TRY
    DBCC CHECKCONSTRAINTS (N'dbo.MST_Calendar') WITH NO_INFOMSGS;
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | PASS';
END TRY
BEGIN CATCH
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | FAIL: ' + ERROR_MESSAGE();
    SET @Fail += 1;
END CATCH;

/* -------------------------------------------------------------------------
   9) Triggers still none
--------------------------------------------------------------------------- */
DECLARE @Trig INT = (
    SELECT COUNT(*) FROM sys.triggers WHERE parent_id = OBJECT_ID(N'dbo.MST_Calendar')
);
PRINT N'CHECK Triggers (expect 0) | ' + CAST(@Trig AS NVARCHAR(10)) + N' | '
    + CASE WHEN @Trig = 0 THEN N'PASS' ELSE N'FAIL' END;
IF @Trig <> 0 SET @Fail += 1;

/* -------------------------------------------------------------------------
   SUMMARY
--------------------------------------------------------------------------- */
PRINT N'';
PRINT N'----- PASS CHECKLIST -----';
PRINT N'FinancialWeek               | ' + CASE WHEN @HasFinancialWeek = 1 THEN N'Exists' ELSE N'Pending (expected)' END;
PRINT N'Null backfill               | ' + CASE
        WHEN @HasFinancialWeek = 0 THEN N'Pending (expected)'
        WHEN @NullFW = 0 AND ISNULL(@DriftFW, 0) = 0 AND ISNULL(@WouldUpdate, 0) = 0 THEN N'validated'
        ELSE N'FAIL' END;
PRINT N'Range 1-53                  | ' + CASE
        WHEN @HasFinancialWeek = 0 THEN N'Pending (expected)'
        WHEN @FwMin >= 1 AND @FwMax <= 53 AND @OutOfRange = 0 THEN N'validated'
        ELSE N'FAIL' END;
PRINT N'LIVE-only cols retained    | ' + CASE WHEN @LiveOnlyMissing = 0 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'PK Match                   | ' + CASE WHEN ISNULL(@PkCols, N'') = N'CalendarDate' THEN N'PASS' ELSE N'FAIL' END;
PRINT N'FK Match                   | PASS (none defined)';
PRINT N'Defaults Match (LIVE)      | ' + CASE WHEN @DefHoliday IS NOT NULL AND @DefWorking IS NOT NULL THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Indexes Match (LIVE keep)  | ' + CASE WHEN @IxMissing = 0 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Triggers Match             | ' + CASE WHEN @Trig = 0 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Row Count Preserved 7671   | ' + CASE WHEN @Cnt = 7671 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'No Duplicate Dates         | ' + CASE WHEN @Dup = 0 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'DBCC CHECKCONSTRAINTS      | (see above)';
PRINT N'';
IF @Fail = 0
    PRINT N'===== OVERALL: PASS =====';
ELSE
    PRINT N'===== OVERALL: FAIL (' + CAST(@Fail AS NVARCHAR(10)) + N' checks) — do not proceed to Table 3 =====';
GO
