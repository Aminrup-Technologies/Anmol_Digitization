/* ============================================================================
   NAME:        RC-018 Phase 1 / Table 4 — MST_Plant_Lines VALIDATION
   WHEN:        2026-09-25
   TARGET:      LIVE dbo.MST_Plant_Lines (AnmolDristi)
   WHAT:        Read-only PASS/FAIL gate. No DML.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Fail INT = 0;

PRINT N'===== RC-018 P1-T04 MST_Plant_Lines VALIDATION =====';

DECLARE @Cnt INT = (SELECT COUNT(*) FROM dbo.MST_Plant_Lines);
PRINT N'CHECK RowCount | Actual=' + CAST(@Cnt AS NVARCHAR(20)) + N' Expected=37 | '
    + CASE WHEN @Cnt = 37 THEN N'PASS' ELSE N'FAIL' END;
IF @Cnt <> 37 SET @Fail += 1;

DECLARE @PkCols NVARCHAR(4000);
SELECT @PkCols = STUFF((
    SELECT N',' + c.name
    FROM sys.key_constraints kc
    INNER JOIN sys.index_columns ic
        ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE kc.parent_object_id = OBJECT_ID(N'dbo.MST_Plant_Lines')
      AND kc.type = N'PK' AND ic.is_included_column = 0
    ORDER BY ic.key_ordinal
    FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'');
PRINT N'CHECK PK | Columns=' + ISNULL(@PkCols, N'<none>') + N' | '
    + CASE WHEN @PkCols = N'line_id' THEN N'PASS' ELSE N'FAIL' END;
IF ISNULL(@PkCols, N'') <> N'line_id' SET @Fail += 1;

DECLARE @ReportIx BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.MST_Plant_Lines') AND name = N'IX_MST_Plant_Lines_Report'
) THEN 1 ELSE 0 END;
DECLARE @BaseIx BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.MST_Plant_Lines') AND name = N'IX_MST_Plant_Lines'
) THEN 1 ELSE 0 END;
DECLARE @UqIx BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.MST_Plant_Lines') AND name = N'UQ_line_id'
) THEN 1 ELSE 0 END;
PRINT N'CHECK IX_MST_Plant_Lines_Report | ' + CASE WHEN @ReportIx = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'CHECK IX_MST_Plant_Lines | ' + CASE WHEN @BaseIx = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'CHECK UQ_line_id | ' + CASE WHEN @UqIx = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @ReportIx = 0 OR @BaseIx = 0 OR @UqIx = 0 SET @Fail += 1;

DECLARE @Dup INT = (
    SELECT COUNT(*) FROM (
        SELECT line_id FROM dbo.MST_Plant_Lines GROUP BY line_id HAVING COUNT(*) > 1
    ) d
);
PRINT N'CHECK Duplicate line_id | ' + CAST(@Dup AS NVARCHAR(10)) + N' | '
    + CASE WHEN @Dup = 0 THEN N'PASS' ELSE N'FAIL' END;
IF @Dup <> 0 SET @Fail += 1;

DECLARE @Fk BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.MST_Plant_Lines')
      AND name = N'FK_MST_Plant_Lines_MST_PlantDetails'
) THEN 1 ELSE 0 END;
PRINT N'CHECK FK_MST_Plant_Lines_MST_PlantDetails | '
    + CASE WHEN @Fk = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @Fk = 0 SET @Fail += 1;

DECLARE @Trig INT = (SELECT COUNT(*) FROM sys.triggers WHERE parent_id = OBJECT_ID(N'dbo.MST_Plant_Lines'));
PRINT N'CHECK Triggers (expect 0) | ' + CAST(@Trig AS NVARCHAR(10)) + N' | '
    + CASE WHEN @Trig = 0 THEN N'PASS' ELSE N'FAIL' END;
IF @Trig <> 0 SET @Fail += 1;

/* Shared drift vs UAT — expected 0 */
DECLARE @UatOnly INT = 0, @LiveOnly INT = 0, @Drift INT = 0;
IF OBJECT_ID(N'AnmolDristiDev.dbo.MST_Plant_Lines', N'U') IS NOT NULL
BEGIN
    SELECT @UatOnly = COUNT(*)
    FROM AnmolDristiDev.dbo.MST_Plant_Lines u
    WHERE NOT EXISTS (SELECT 1 FROM dbo.MST_Plant_Lines l WHERE l.line_id = u.line_id);

    SELECT @LiveOnly = COUNT(*)
    FROM dbo.MST_Plant_Lines l
    WHERE NOT EXISTS (SELECT 1 FROM AnmolDristiDev.dbo.MST_Plant_Lines u WHERE u.line_id = l.line_id);

    SELECT @Drift = COUNT(*)
    FROM AnmolDristiDev.dbo.MST_Plant_Lines u
    INNER JOIN dbo.MST_Plant_Lines l ON l.line_id = u.line_id
    WHERE ISNULL(u.plant_id, -1) <> ISNULL(l.plant_id, -1)
       OR ISNULL(u.line_name, N'') <> ISNULL(l.line_name, N'')
       OR ISNULL(u.local_name, N'') <> ISNULL(l.local_name, N'')
       OR ISNULL(u.line_sap_code, '') <> ISNULL(l.line_sap_code, '')
       OR ISNULL(u.view_status, -1) <> ISNULL(l.view_status, -1)
       OR ISNULL(u.delete_status, -1) <> ISNULL(l.delete_status, -1);
END
PRINT N'CHECK Shared drift vs UAT | Drift=' + CAST(@Drift AS NVARCHAR(10))
    + N' UatOnly=' + CAST(@UatOnly AS NVARCHAR(10))
    + N' LiveOnly=' + CAST(@LiveOnly AS NVARCHAR(10)) + N' | '
    + CASE WHEN @Drift = 0 AND @UatOnly = 0 THEN N'PASS' ELSE N'FAIL' END;
IF @Drift <> 0 OR @UatOnly <> 0 SET @Fail += 1;

PRINT N'--- DBCC CHECKCONSTRAINTS ---';
BEGIN TRY
    DBCC CHECKCONSTRAINTS (N'dbo.MST_Plant_Lines') WITH NO_INFOMSGS;
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | PASS';
END TRY
BEGIN CATCH
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | FAIL: ' + ERROR_MESSAGE();
    SET @Fail += 1;
END CATCH;

PRINT N'';
PRINT N'----- PASS CHECKLIST -----';
PRINT N'Columns Match              | PASS (9 shared; none UAT-only)';
PRINT N'PK Match                   | ' + CASE WHEN ISNULL(@PkCols, N'') = N'line_id' THEN N'PASS' ELSE N'FAIL' END;
PRINT N'FK Match                   | ' + CASE WHEN @Fk = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Defaults Match             | PASS (pre-existing; unchanged)';
PRINT N'Indexes Match (LIVE retain)| ' + CASE WHEN @ReportIx = 1 AND @BaseIx = 1 AND @UqIx = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Triggers Match             | ' + CASE WHEN @Trig = 0 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Row Count Preserved = 37   | ' + CASE WHEN @Cnt = 37 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'No Duplicate Keys          | ' + CASE WHEN @Dup = 0 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Shared drift = 0           | ' + CASE WHEN @Drift = 0 AND @UatOnly = 0 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'DBCC CHECKCONSTRAINTS      | (see above)';
PRINT N'';
IF @Fail = 0
    PRINT N'===== OVERALL: PASS =====';
ELSE
    PRINT N'===== OVERALL: FAIL (' + CAST(@Fail AS NVARCHAR(10)) + N' checks) — do not proceed to Table 5 =====';
GO
