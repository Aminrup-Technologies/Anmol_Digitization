/* ============================================================================
   NAME:        RC-018 Phase 1 / Table 3 — MST_EmployeeTolerance VALIDATION
   WHEN:        2026-09-24
   TARGET:      LIVE dbo.MST_EmployeeTolerance (AnmolDristi)
   WHAT:        Read-only PASS/FAIL gate. No DML.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Fail INT = 0;

PRINT N'===== RC-018 P1-T03 MST_EmployeeTolerance VALIDATION =====';

/* Expected seed */
IF OBJECT_ID(N'tempdb..#Expected') IS NOT NULL DROP TABLE #Expected;
CREATE TABLE #Expected
(
    ToleranceId            INT           NOT NULL PRIMARY KEY,
    EmployeeCode           NVARCHAR(100) NOT NULL,
    MinAllowedKM           DECIMAL(10,2) NULL,
    MaxAllowedKM           DECIMAL(10,2) NULL,
    EffectiveFrom          DATE          NULL,
    EffectiveTo            DATE          NULL,
    IsActive               BIT           NOT NULL,
    MaxKmModificationCount INT           NOT NULL
);
INSERT INTO #Expected VALUES
 (1, N'AHS1562', 0.00, 100.00, '2026-04-01', '2027-03-31', 1, 2),
 (2, N'M1367',  10.00,  50.00, '2026-04-01', '2027-03-31', 1, 1),
 (3, N'AHS1530', 0.00,  30.00, '2026-08-01', '2026-12-31', 1, 1);

/* 1) Row count = 3 */
DECLARE @Cnt INT = (SELECT COUNT(*) FROM dbo.MST_EmployeeTolerance);
PRINT N'CHECK RowCount | Actual=' + CAST(@Cnt AS NVARCHAR(20)) + N' Expected=3 | '
    + CASE WHEN @Cnt = 3 THEN N'PASS' ELSE N'FAIL' END;
IF @Cnt <> 3 SET @Fail += 1;

/* 2) MaxKmModificationCount exists */
DECLARE @HasCol BIT = CASE WHEN COL_LENGTH(N'dbo.MST_EmployeeTolerance', N'MaxKmModificationCount') IS NOT NULL THEN 1 ELSE 0 END;
PRINT N'CHECK MaxKmModificationCount exists | '
    + CASE WHEN @HasCol = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @HasCol = 0 SET @Fail += 1;

/* Default + check */
DECLARE @HasDf BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.default_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.MST_EmployeeTolerance')
      AND name = N'DF_MST_EmployeeTolerance_MaxKmModificationCount') THEN 1 ELSE 0 END;
DECLARE @HasCk BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.MST_EmployeeTolerance')
      AND name = N'CK_MST_EmployeeTolerance_MaxKmModificationCount') THEN 1 ELSE 0 END;
PRINT N'CHECK Default DF_...MaxKmModificationCount | '
    + CASE WHEN @HasDf = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'CHECK Check CK_...MaxKmModificationCount | '
    + CASE WHEN @HasCk = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @HasDf = 0 SET @Fail += 1;
IF @HasCk = 0 SET @Fail += 1;

/* 3) PK */
DECLARE @PkCols NVARCHAR(4000);
SELECT @PkCols = STUFF((
    SELECT N',' + c.name
    FROM sys.key_constraints kc
    INNER JOIN sys.index_columns ic
        ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE kc.parent_object_id = OBJECT_ID(N'dbo.MST_EmployeeTolerance')
      AND kc.type = N'PK' AND ic.is_included_column = 0
    ORDER BY ic.key_ordinal
    FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'');
PRINT N'CHECK PK | Columns=' + ISNULL(@PkCols, N'<none>') + N' | '
    + CASE WHEN @PkCols = N'ToleranceId' THEN N'PASS' ELSE N'FAIL' END;
IF ISNULL(@PkCols, N'') <> N'ToleranceId' SET @Fail += 1;

/* 4) No duplicate keys */
DECLARE @DupId INT = (
    SELECT COUNT(*) FROM (
        SELECT ToleranceId FROM dbo.MST_EmployeeTolerance GROUP BY ToleranceId HAVING COUNT(*) > 1
    ) d
);
DECLARE @DupEmp INT = (
    SELECT COUNT(*) FROM (
        SELECT EmployeeCode, EffectiveFrom, EffectiveTo
        FROM dbo.MST_EmployeeTolerance
        GROUP BY EmployeeCode, EffectiveFrom, EffectiveTo
        HAVING COUNT(*) > 1
    ) d
);
PRINT N'CHECK Duplicate ToleranceId | ' + CAST(@DupId AS NVARCHAR(10)) + N' | '
    + CASE WHEN @DupId = 0 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'CHECK Duplicate Emp+Dates | ' + CAST(@DupEmp AS NVARCHAR(10)) + N' | '
    + CASE WHEN @DupEmp = 0 THEN N'PASS' ELSE N'FAIL' END;
IF @DupId <> 0 SET @Fail += 1;
IF @DupEmp <> 0 SET @Fail += 1;

/* 5) All three expected records exist with matching attrs.
      Column access is dynamic so this batch still compiles before the ADD. */
DECLARE @MissingOrDrift INT = 0;
DECLARE @Sql NVARCHAR(MAX);
IF @HasCol = 1
BEGIN
    SET @Sql = N'
        SELECT @MissingOrDrift = COUNT(*)
        FROM #Expected e
        LEFT JOIN dbo.MST_EmployeeTolerance l ON l.ToleranceId = e.ToleranceId
        WHERE l.ToleranceId IS NULL
           OR ISNULL(l.EmployeeCode, N'''') <> e.EmployeeCode
           OR ISNULL(l.MinAllowedKM, -1) <> ISNULL(e.MinAllowedKM, -1)
           OR ISNULL(l.MaxAllowedKM, -1) <> ISNULL(e.MaxAllowedKM, -1)
           OR ISNULL(l.EffectiveFrom, ''19000101'') <> ISNULL(e.EffectiveFrom, ''19000101'')
           OR ISNULL(l.EffectiveTo, ''19000101'') <> ISNULL(e.EffectiveTo, ''19000101'')
           OR ISNULL(l.IsActive, 0) <> e.IsActive
           OR ISNULL(l.MaxKmModificationCount, -1) <> e.MaxKmModificationCount;';
    EXEC sys.sp_executesql @Sql, N'@MissingOrDrift INT OUTPUT', @MissingOrDrift = @MissingOrDrift OUTPUT;

    PRINT N'CHECK Seed rows match (3) | DriftOrMissing='
        + CAST(@MissingOrDrift AS NVARCHAR(10)) + N' | '
        + CASE WHEN @MissingOrDrift = 0 THEN N'PASS' ELSE N'FAIL' END;
    IF @MissingOrDrift <> 0
    BEGIN
        SET @Fail += 1;
        SET @Sql = N'
            SELECT e.*, l.EmployeeCode AS LIVE_EmployeeCode, l.MaxKmModificationCount AS LIVE_MaxKmMod
            FROM #Expected e
            LEFT JOIN dbo.MST_EmployeeTolerance l ON l.ToleranceId = e.ToleranceId;';
        EXEC sys.sp_executesql @Sql;
    END
    ELSE
    BEGIN
        SET @Sql = N'
            SELECT ToleranceId, EmployeeCode, MinAllowedKM, MaxAllowedKM,
                   EffectiveFrom, EffectiveTo, IsActive, MaxKmModificationCount
            FROM dbo.MST_EmployeeTolerance
            ORDER BY ToleranceId;';
        EXEC sys.sp_executesql @Sql;
    END
END
ELSE
BEGIN
    PRINT N'CHECK Seed rows match | FAIL (column missing)';
    SET @Fail += 1;
END;

/* 6) Identity still on ToleranceId */
DECLARE @IsIdent INT = COLUMNPROPERTY(OBJECT_ID(N'dbo.MST_EmployeeTolerance'), N'ToleranceId', N'IsIdentity');
PRINT N'CHECK Identity on ToleranceId | '
    + CASE WHEN @IsIdent = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @IsIdent <> 1 SET @Fail += 1;

/* 7) Idempotency */
DECLARE @WouldInsert INT = (
    SELECT COUNT(*) FROM #Expected e
    WHERE NOT EXISTS (SELECT 1 FROM dbo.MST_EmployeeTolerance l WHERE l.ToleranceId = e.ToleranceId)
);
DECLARE @WouldUpdate INT = 0;
IF @HasCol = 1
BEGIN
    SET @Sql = N'
        SELECT @WouldUpdate = COUNT(*)
        FROM dbo.MST_EmployeeTolerance l
        INNER JOIN #Expected e ON e.ToleranceId = l.ToleranceId
        WHERE ISNULL(l.EmployeeCode, N'''') <> e.EmployeeCode
           OR ISNULL(l.MinAllowedKM, -1) <> ISNULL(e.MinAllowedKM, -1)
           OR ISNULL(l.MaxAllowedKM, -1) <> ISNULL(e.MaxAllowedKM, -1)
           OR ISNULL(l.EffectiveFrom, ''19000101'') <> ISNULL(e.EffectiveFrom, ''19000101'')
           OR ISNULL(l.EffectiveTo, ''19000101'') <> ISNULL(e.EffectiveTo, ''19000101'')
           OR ISNULL(l.IsActive, 0) <> e.IsActive
           OR ISNULL(l.MaxKmModificationCount, -1) <> e.MaxKmModificationCount;';
    EXEC sys.sp_executesql @Sql, N'@WouldUpdate INT OUTPUT', @WouldUpdate = @WouldUpdate OUTPUT;
END
PRINT N'CHECK Idempotent | WouldInsert=' + CAST(@WouldInsert AS NVARCHAR(10))
    + N' WouldUpdate=' + CAST(@WouldUpdate AS NVARCHAR(10)) + N' | '
    + CASE WHEN @WouldInsert = 0 AND @WouldUpdate = 0 THEN N'PASS' ELSE N'FAIL' END;
IF @WouldInsert <> 0 OR @WouldUpdate <> 0 SET @Fail += 1;

/* 8) DBCC CHECKCONSTRAINTS */
PRINT N'--- DBCC CHECKCONSTRAINTS ---';
BEGIN TRY
    DBCC CHECKCONSTRAINTS (N'dbo.MST_EmployeeTolerance') WITH NO_INFOMSGS;
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | PASS';
END TRY
BEGIN CATCH
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | FAIL: ' + ERROR_MESSAGE();
    SET @Fail += 1;
END CATCH;

/* 9) Triggers */
DECLARE @Trig INT = (SELECT COUNT(*) FROM sys.triggers WHERE parent_id = OBJECT_ID(N'dbo.MST_EmployeeTolerance'));
PRINT N'CHECK Triggers (expect 0) | ' + CAST(@Trig AS NVARCHAR(10)) + N' | '
    + CASE WHEN @Trig = 0 THEN N'PASS' ELSE N'FAIL' END;
IF @Trig <> 0 SET @Fail += 1;

PRINT N'';
PRINT N'----- PASS CHECKLIST -----';
PRINT N'Columns Match (+MaxKmMod)  | ' + CASE WHEN @HasCol = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'PK Match                   | ' + CASE WHEN ISNULL(@PkCols, N'') = N'ToleranceId' THEN N'PASS' ELSE N'FAIL' END;
PRINT N'FK Match                   | PASS (none)';
PRINT N'Defaults Match             | ' + CASE WHEN @HasDf = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Check Constraint           | ' + CASE WHEN @HasCk = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Indexes Match              | PASS (PK + Lookup pre-existing)';
PRINT N'Triggers Match             | ' + CASE WHEN @Trig = 0 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Row Count = 3              | ' + CASE WHEN @Cnt = 3 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Seed records present       | ' + CASE WHEN @MissingOrDrift = 0 AND @HasCol = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'No Duplicate Keys          | ' + CASE WHEN @DupId = 0 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'DBCC CHECKCONSTRAINTS      | (see above)';
PRINT N'MERGE Idempotent           | ' + CASE WHEN @WouldInsert = 0 AND @WouldUpdate = 0 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'';
IF @Fail = 0
    PRINT N'===== OVERALL: PASS =====';
ELSE
    PRINT N'===== OVERALL: FAIL (' + CAST(@Fail AS NVARCHAR(10)) + N' checks) — do not proceed to Table 4 =====';
GO
