/* ============================================================================
   NAME:        RC-018 Phase 2 / Table 6 — PolicyException VALIDATION
   WHEN:        2026-09-25
   TARGET:      LIVE dbo.PolicyException (AnmolDristi)
   WHAT:        Read-only PASS/FAIL gate. No DML.
   GATE:        @EnableFinancialOverrides defaults to 0. Expect ExceptionId 4
                only, active count 0, and print rows 1, 2, and 90002 as
                Pending (financial gate). Set the switch to 1 only when 021
                ran with the financial gate open (4 rows, 3 active).
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @EnableFinancialOverrides BIT = 0;
DECLARE @Fail INT = 0;

PRINT N'===== RC-018 P2-T06 PolicyException VALIDATION =====';
PRINT N'EnableFinancialOverrides=' + CAST(@EnableFinancialOverrides AS NVARCHAR(1));

IF OBJECT_ID(N'tempdb..#Expected') IS NOT NULL DROP TABLE #Expected;
CREATE TABLE #Expected
(
    ExceptionId   INT           NOT NULL PRIMARY KEY,
    EmployeeCode  NVARCHAR(100) NOT NULL,
    IsActive      BIT           NOT NULL,
    Priority      INT           NOT NULL,
    EffectiveFrom DATE          NULL,
    EffectiveTo   DATE          NULL,
    PolicyJson    NVARCHAR(MAX) NOT NULL
);
INSERT INTO #Expected VALUES
 (1, N'AHS1562', 1, 1, '2026-04-01', '2027-04-30',
  N'{"MaxKM":2250.0,"RatePerKm":5.75,"DA":250.0,"HotelCap":1000.0,"OutstationDA":450.0,"OwnArrangementDA":800.0,"Remarks":"Policy Exception"}'),
 (2, N'AHS628', 1, 1, '2026-04-01', '2027-03-31',
  N'{"MaxKM":2500.0,"RatePerKm":5.00,"DA":230.0,"HotelCap":1000.0,"OutstationDA":500.0,"OwnArrangementDA":1000.0,"Remarks":"Exception Policy"}'),
 (4, N'AHS1562', 0, 1, '2000-01-01', '2000-01-02',
  N'{"DA":99,"Remarks":"temp overlap"}'),
 (90002, N'AHS1530', 1, 1, '2026-08-01', '2026-12-31',
  N'{"MaxKM":3.0,"RatePerKm":6.95,"DA":600.0,"HotelCap":2000.0,"Remarks":"Exception"}');

IF @EnableFinancialOverrides = 0
BEGIN
    PRINT N'ExceptionId 1 (AHS1562) | Pending (financial gate)';
    PRINT N'ExceptionId 2 (AHS628) | Pending (financial gate)';
    PRINT N'ExceptionId 90002 (AHS1530) | Pending (financial gate)';
END;

DECLARE @ExpectedCnt INT = CASE WHEN @EnableFinancialOverrides = 1 THEN 4 ELSE 1 END;
DECLARE @ExpectedActive INT = CASE WHEN @EnableFinancialOverrides = 1 THEN 3 ELSE 0 END;

DECLARE @Cnt INT = (SELECT COUNT(*) FROM dbo.PolicyException);
IF @EnableFinancialOverrides = 1
    PRINT N'CHECK RowCount | Actual=' + CAST(@Cnt AS NVARCHAR(20)) + N' Expected=4 | '
        + CASE WHEN @Cnt = 4 THEN N'PASS' ELSE N'FAIL' END;
ELSE
    PRINT N'CHECK RowCount | Actual=' + CAST(@Cnt AS NVARCHAR(20)) + N' Expected=1 | '
        + CASE WHEN @Cnt = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @Cnt <> @ExpectedCnt SET @Fail += 1;

DECLARE @Missing INT = (
    SELECT COUNT(*)
    FROM #Expected e
    LEFT JOIN dbo.PolicyException l ON l.ExceptionId = e.ExceptionId
    WHERE (@EnableFinancialOverrides = 1 OR e.ExceptionId = 4)
      AND (
            l.ExceptionId IS NULL
         OR ISNULL(l.EmployeeCode, N'') <> e.EmployeeCode
         OR ISNULL(l.IsActive, 0) <> e.IsActive
         OR ISNULL(l.Priority, -1) <> e.Priority
         OR ISNULL(l.EffectiveFrom, '19000101') <> ISNULL(e.EffectiveFrom, '19000101')
         OR ISNULL(l.EffectiveTo, '19000101') <> ISNULL(e.EffectiveTo, '19000101')
         OR ISNULL(l.PolicyJson, N'') <> e.PolicyJson
          )
);
IF @EnableFinancialOverrides = 1
    PRINT N'CHECK Expected four records | DriftOrMissing=' + CAST(@Missing AS NVARCHAR(10)) + N' | '
        + CASE WHEN @Missing = 0 THEN N'PASS' ELSE N'FAIL' END;
ELSE
    PRINT N'CHECK Expected ExceptionId 4 | DriftOrMissing=' + CAST(@Missing AS NVARCHAR(10)) + N' | '
        + CASE WHEN @Missing = 0 THEN N'PASS' ELSE N'FAIL' END;
IF @Missing <> 0 SET @Fail += 1;

DECLARE @Dup INT = (
    SELECT COUNT(*) FROM (
        SELECT ExceptionId FROM dbo.PolicyException GROUP BY ExceptionId HAVING COUNT(*) > 1
    ) d
);
PRINT N'CHECK Duplicate ExceptionId | ' + CAST(@Dup AS NVARCHAR(10)) + N' | '
    + CASE WHEN @Dup = 0 THEN N'PASS' ELSE N'FAIL' END;
IF @Dup <> 0 SET @Fail += 1;

DECLARE @HasIx BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_PolicyException_Employee_Active_Dates'
      AND object_id = OBJECT_ID(N'dbo.PolicyException')
) THEN 1 ELSE 0 END;
DECLARE @HasPri BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_PolicyException_Priority'
      AND parent_object_id = OBJECT_ID(N'dbo.PolicyException')
) THEN 1 ELSE 0 END;
DECLARE @HasDates BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_PolicyException_EffectiveDates'
      AND parent_object_id = OBJECT_ID(N'dbo.PolicyException')
) THEN 1 ELSE 0 END;
PRINT N'CHECK Index IX_PolicyException_Employee_Active_Dates | '
    + CASE WHEN @HasIx = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'CHECK CK_PolicyException_Priority | '
    + CASE WHEN @HasPri = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'CHECK CK_PolicyException_EffectiveDates | '
    + CASE WHEN @HasDates = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @HasIx = 0 OR @HasPri = 0 OR @HasDates = 0 SET @Fail += 1;

DECLARE @PkCols NVARCHAR(4000);
SELECT @PkCols = STUFF((
    SELECT N',' + c.name
    FROM sys.key_constraints kc
    INNER JOIN sys.index_columns ic
        ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE kc.parent_object_id = OBJECT_ID(N'dbo.PolicyException')
      AND kc.type = N'PK' AND ic.is_included_column = 0
    ORDER BY ic.key_ordinal
    FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'');
PRINT N'CHECK PK | Columns=' + ISNULL(@PkCols, N'<none>') + N' | '
    + CASE WHEN @PkCols = N'ExceptionId' THEN N'PASS' ELSE N'FAIL' END;
IF ISNULL(@PkCols, N'') <> N'ExceptionId' SET @Fail += 1;

DECLARE @Active INT = (SELECT COUNT(*) FROM dbo.PolicyException WHERE IsActive = 1);
IF @EnableFinancialOverrides = 1
    PRINT N'CHECK Active records | ' + CAST(@Active AS NVARCHAR(10)) + N' Expected=3 | '
        + CASE WHEN @Active = 3 THEN N'PASS' ELSE N'FAIL' END;
ELSE
    PRINT N'CHECK Active records | ' + CAST(@Active AS NVARCHAR(10)) + N' Expected=0 | '
        + CASE WHEN @Active = 0 THEN N'PASS' ELSE N'FAIL' END;
IF @Active <> @ExpectedActive SET @Fail += 1;

PRINT N'--- DBCC CHECKCONSTRAINTS ---';
BEGIN TRY
    DBCC CHECKCONSTRAINTS (N'dbo.PolicyException') WITH NO_INFOMSGS;
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | PASS';
END TRY
BEGIN CATCH
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | FAIL: ' + ERROR_MESSAGE();
    SET @Fail += 1;
END CATCH;

PRINT N'';
PRINT N'----- PASS CHECKLIST -----';
IF @EnableFinancialOverrides = 1
BEGIN
    PRINT N'Row Count = 4              | ' + CASE WHEN @Cnt = 4 THEN N'PASS' ELSE N'FAIL' END;
    PRINT N'Expected four records      | ' + CASE WHEN @Missing = 0 THEN N'PASS' ELSE N'FAIL' END;
    PRINT N'Active records = 3         | ' + CASE WHEN @Active = 3 THEN N'PASS' ELSE N'FAIL' END;
END
ELSE
BEGIN
    PRINT N'Row Count = 1              | ' + CASE WHEN @Cnt = 1 THEN N'PASS' ELSE N'FAIL' END;
    PRINT N'Expected ExceptionId 4     | ' + CASE WHEN @Missing = 0 THEN N'PASS' ELSE N'FAIL' END;
    PRINT N'Active records = 0         | ' + CASE WHEN @Active = 0 THEN N'PASS' ELSE N'FAIL' END;
    PRINT N'ExceptionId 1 (AHS1562)    | Pending (financial gate)';
    PRINT N'ExceptionId 2 (AHS628)     | Pending (financial gate)';
    PRINT N'ExceptionId 90002 (AHS1530)| Pending (financial gate)';
END;
PRINT N'Index exists               | ' + CASE WHEN @HasIx = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Check constraints exist    | ' + CASE WHEN @HasPri = 1 AND @HasDates = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'PK Match                   | ' + CASE WHEN ISNULL(@PkCols, N'') = N'ExceptionId' THEN N'PASS' ELSE N'FAIL' END;
PRINT N'No Duplicate Keys          | ' + CASE WHEN @Dup = 0 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'DBCC CHECKCONSTRAINTS      | (see above)';
PRINT N'';
IF @Fail = 0
    PRINT N'===== OVERALL: PASS =====';
ELSE
    PRINT N'===== OVERALL: FAIL (' + CAST(@Fail AS NVARCHAR(10)) + N' checks) — do not proceed to Phase 3 =====';
GO
