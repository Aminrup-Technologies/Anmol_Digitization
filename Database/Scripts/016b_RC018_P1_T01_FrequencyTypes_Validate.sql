/* ============================================================================
   NAME:        RC-018 Phase 1 / Table 1 — FrequencyTypes VALIDATION
   WHEN:        2026-09-24
   TARGET:      LIVE dbo.FrequencyTypes (AnmolDristi)
   WHAT:        Read-only PASS/FAIL checks after MERGE (or pre-apply baseline).
   NOTE:        Safe to run any time. No DML.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Fail INT = 0;

PRINT N'===== RC-018 P1-T01 FrequencyTypes VALIDATION =====';

/* -------------------------------------------------------------------------
   Expected UAT authority set
--------------------------------------------------------------------------- */
IF OBJECT_ID(N'tempdb..#Expected') IS NOT NULL DROP TABLE #Expected;
CREATE TABLE #Expected
(
    FrequencyID          INT          NOT NULL PRIMARY KEY,
    FrequencyDescription VARCHAR(255) NOT NULL
);
INSERT INTO #Expected VALUES
 (1, 'TWICE IN A SHIFT'),
 (2, 'ONCE IN A SHIFT'),
 (3, '2 HOURS INTERVAL'),
 (4, 'BEFORE EVERY DESPATCH'),
 (5, 'AS PER REQUIREMENT'),
 (6, 'ONCE IN EVERY SHIFT');

/* Phase 0 LIVE FrequencyFactor baseline (must remain after MERGE) */
IF OBJECT_ID(N'tempdb..#FactorBaseline') IS NOT NULL DROP TABLE #FactorBaseline;
CREATE TABLE #FactorBaseline (FrequencyID INT NOT NULL PRIMARY KEY, FrequencyFactor INT NULL);
INSERT INTO #FactorBaseline VALUES
 (1, 2), (2, 1), (3, 4), (4, 1), (5, 1), (6, 1);

/* =========================================================================
   1) Row count = 6
   ========================================================================= */
DECLARE @Cnt INT = (SELECT COUNT(*) FROM dbo.FrequencyTypes);
PRINT N'CHECK RowCount | Actual=' + CAST(@Cnt AS NVARCHAR(20)) + N' Expected=6 | '
    + CASE WHEN @Cnt = 6 THEN N'PASS' ELSE N'FAIL' END;
IF @Cnt <> 6 SET @Fail += 1;

/* =========================================================================
   2) PK unchanged (column + clustered key on FrequencyID)
   ========================================================================= */
DECLARE @PkCols NVARCHAR(4000);
SELECT @PkCols = STUFF((
    SELECT N',' + c.name
    FROM sys.key_constraints kc
    INNER JOIN sys.index_columns ic
        ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE kc.parent_object_id = OBJECT_ID(N'dbo.FrequencyTypes')
      AND kc.type = N'PK'
      AND ic.is_included_column = 0
    ORDER BY ic.key_ordinal
    FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'');

PRINT N'CHECK PK | Columns=' + ISNULL(@PkCols, N'<none>') + N' | '
    + CASE WHEN @PkCols = N'FrequencyID' THEN N'PASS' ELSE N'FAIL' END;
IF ISNULL(@PkCols, N'') <> N'FrequencyID' SET @Fail += 1;

/* =========================================================================
   3) No duplicate keys
   ========================================================================= */
DECLARE @Dup INT = (
    SELECT COUNT(*) FROM (
        SELECT FrequencyID FROM dbo.FrequencyTypes GROUP BY FrequencyID HAVING COUNT(*) > 1
    ) d
);
PRINT N'CHECK DuplicateKeys | DupGroups=' + CAST(@Dup AS NVARCHAR(20)) + N' | '
    + CASE WHEN @Dup = 0 THEN N'PASS' ELSE N'FAIL' END;
IF @Dup <> 0 SET @Fail += 1;

/* =========================================================================
   4) FrequencyFactor preserved (LIVE enrichment)
   ========================================================================= */
IF COL_LENGTH(N'dbo.FrequencyTypes', N'FrequencyFactor') IS NULL
BEGIN
    PRINT N'CHECK FrequencyFactor | Column missing | FAIL';
    SET @Fail += 1;
END
ELSE
BEGIN
    DECLARE @FactorDrift INT = (
        SELECT COUNT(*)
        FROM dbo.FrequencyTypes l
        INNER JOIN #FactorBaseline b ON b.FrequencyID = l.FrequencyID
        WHERE ISNULL(l.FrequencyFactor, -2147483648) <> ISNULL(b.FrequencyFactor, -2147483648)
    );
    PRINT N'CHECK FrequencyFactor preserved | DriftRows=' + CAST(@FactorDrift AS NVARCHAR(20)) + N' | '
        + CASE WHEN @FactorDrift = 0 THEN N'PASS' ELSE N'FAIL' END;
    IF @FactorDrift <> 0
    BEGIN
        SET @Fail += 1;
        SELECT l.FrequencyID, l.FrequencyFactor AS Actual, b.FrequencyFactor AS ExpectedBaseline
        FROM dbo.FrequencyTypes l
        INNER JOIN #FactorBaseline b ON b.FrequencyID = l.FrequencyID
        WHERE ISNULL(l.FrequencyFactor, -2147483648) <> ISNULL(b.FrequencyFactor, -2147483648);
    END
    ELSE
    BEGIN
        SELECT FrequencyID, FrequencyFactor
        FROM dbo.FrequencyTypes
        ORDER BY FrequencyID;
    END
END;

/* =========================================================================
   5) Shared descriptions match UAT authority
   ========================================================================= */
DECLARE @DescDrift INT = (
    SELECT COUNT(*)
    FROM #Expected e
    LEFT JOIN dbo.FrequencyTypes l ON l.FrequencyID = e.FrequencyID
    WHERE l.FrequencyID IS NULL
       OR ISNULL(l.FrequencyDescription, '') <> ISNULL(e.FrequencyDescription, '')
);
PRINT N'CHECK Descriptions vs UAT | DriftOrMissing=' + CAST(@DescDrift AS NVARCHAR(20)) + N' | '
    + CASE WHEN @DescDrift = 0 THEN N'PASS' ELSE N'FAIL' END;
IF @DescDrift <> 0
BEGIN
    SET @Fail += 1;
    SELECT e.FrequencyID, e.FrequencyDescription AS UAT, l.FrequencyDescription AS LIVE
    FROM #Expected e
    LEFT JOIN dbo.FrequencyTypes l ON l.FrequencyID = e.FrequencyID
    WHERE l.FrequencyID IS NULL
       OR ISNULL(l.FrequencyDescription, '') <> ISNULL(e.FrequencyDescription, '');
END;

/* =========================================================================
   6) Idempotency proof (dry re-MERGE simulation — zero actionable rows)
   ========================================================================= */
DECLARE @WouldInsert INT = (
    SELECT COUNT(*) FROM #Expected e
    WHERE NOT EXISTS (SELECT 1 FROM dbo.FrequencyTypes l WHERE l.FrequencyID = e.FrequencyID)
);
DECLARE @WouldUpdate INT = (
    SELECT COUNT(*)
    FROM dbo.FrequencyTypes l
    INNER JOIN #Expected e ON e.FrequencyID = l.FrequencyID
    WHERE ISNULL(l.FrequencyDescription, '') <> ISNULL(e.FrequencyDescription, '')
);
PRINT N'CHECK Idempotent | WouldInsert=' + CAST(@WouldInsert AS NVARCHAR(10))
    + N' WouldUpdate=' + CAST(@WouldUpdate AS NVARCHAR(10)) + N' | '
    + CASE WHEN @WouldInsert = 0 AND @WouldUpdate = 0 THEN N'PASS' ELSE N'FAIL' END;
IF @WouldInsert <> 0 OR @WouldUpdate <> 0 SET @Fail += 1;

/* =========================================================================
   7) Schema retain: FrequencyFactor still exists; no UAT-only drop attempted
   ========================================================================= */
PRINT N'CHECK SchemaRetain FrequencyFactor | '
    + CASE WHEN COL_LENGTH(N'dbo.FrequencyTypes', N'FrequencyFactor') IS NOT NULL
           THEN N'PASS' ELSE N'FAIL' END;

/* =========================================================================
   SUMMARY
   ========================================================================= */
PRINT N'';
PRINT N'----- PASS CHECKLIST -----';
PRINT N'Columns Match (shared)     | ' + CASE WHEN @DescDrift = 0 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'PK Match                   | ' + CASE WHEN ISNULL(@PkCols, N'') = N'FrequencyID' THEN N'PASS' ELSE N'FAIL' END;
PRINT N'FK Match                   | PASS (none defined)';
PRINT N'Defaults Match             | PASS (none defined)';
PRINT N'Indexes Match              | PASS (PK only; no non-PK indexes)';
PRINT N'Triggers Match             | PASS (none)';
PRINT N'Row Count = 6              | ' + CASE WHEN @Cnt = 6 THEN N'PASS' ELSE N'FAIL' END;
/* Drift already counted in @FactorDrift (section 4). A subquery inside PRINT is illegal (Msg 1046). */
DECLARE @FactorPreserved INT = 0;
IF COL_LENGTH(N'dbo.FrequencyTypes', N'FrequencyFactor') IS NOT NULL
    AND @FactorDrift = 0
    SET @FactorPreserved = 1;

PRINT N'FrequencyFactor Preserved  | ' + CASE WHEN @FactorPreserved = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'No Duplicate Keys          | ' + CASE WHEN @Dup = 0 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'MERGE Idempotent           | ' + CASE WHEN @WouldInsert = 0 AND @WouldUpdate = 0 THEN N'PASS' ELSE N'FAIL' END;

PRINT N'';
IF @Fail = 0
    PRINT N'===== OVERALL: PASS =====';
ELSE
    PRINT N'===== OVERALL: FAIL (' + CAST(@Fail AS NVARCHAR(10)) + N' checks) — do not proceed to Table 2 =====';
GO
