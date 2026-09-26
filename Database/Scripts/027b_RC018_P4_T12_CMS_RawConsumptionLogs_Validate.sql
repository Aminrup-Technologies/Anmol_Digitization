/* ============================================================================
   NAME:        RC-018 Phase 4 / Table 12 — CMS_RawConsumptionLogs VALIDATION
   WHEN:        2026-09-25
   TARGET:      LIVE dbo.CMS_RawConsumptionLogs (AnmolDristi)
   WHAT:        Read-only PASS/FAIL. No DML.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Fail INT = 0;
DECLARE @Floor BIGINT = 772493;

PRINT N'===== RC-018 P4-T12 CMS_RawConsumptionLogs VALIDATION =====';

DECLARE @Cnt BIGINT = (SELECT COUNT_BIG(*) FROM dbo.CMS_RawConsumptionLogs);
PRINT N'CHECK Row count preserved | Actual=' + CAST(@Cnt AS NVARCHAR(20))
    + N' Floor=' + CAST(@Floor AS NVARCHAR(20)) + N' | '
    + CASE WHEN @Cnt >= @Floor THEN N'PASS' ELSE N'FAIL' END;
IF @Cnt < @Floor SET @Fail += 1;

DECLARE @PkCols NVARCHAR(4000);
SELECT @PkCols = STUFF((
    SELECT N',' + c.name
    FROM sys.key_constraints kc
    INNER JOIN sys.index_columns ic
        ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE kc.parent_object_id = OBJECT_ID(N'dbo.CMS_RawConsumptionLogs')
      AND kc.type = N'PK' AND ic.is_included_column = 0
    ORDER BY ic.key_ordinal
    FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'');
PRINT N'CHECK PK | ' + ISNULL(@PkCols, N'<none>') + N' | '
    + CASE WHEN @PkCols = N'LogId' THEN N'PASS' ELSE N'FAIL' END;
IF ISNULL(@PkCols, N'') <> N'LogId' SET @Fail += 1;

DECLARE @Ident INT = COLUMNPROPERTY(OBJECT_ID(N'dbo.CMS_RawConsumptionLogs'), N'LogId', N'IsIdentity');
PRINT N'CHECK LogId identity | ' + CASE WHEN @Ident = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @Ident <> 1 SET @Fail += 1;

DECLARE @Shared INT = (
    SELECT COUNT(*)
    FROM (VALUES
        (N'LogId'), (N'EmployeeCode'), (N'EnrollNumber'), (N'VerifyMode'), (N'InOutMode'),
        (N'PunchTime'), (N'CreatedOn'), (N'BranchID'), (N'MachineIP'), (N'EmployeeName')
    ) v(ColumnName)
    WHERE COL_LENGTH(N'dbo.CMS_RawConsumptionLogs', v.ColumnName) IS NOT NULL
);
PRINT N'CHECK Shared columns | ' + CAST(@Shared AS NVARCHAR(10)) + N'/10 | '
    + CASE WHEN @Shared = 10 THEN N'PASS' ELSE N'FAIL' END;
IF @Shared <> 10 SET @Fail += 1;

DECLARE @UxKeys NVARCHAR(400);
SELECT @UxKeys = STUFF((
    SELECT N',' + c.name
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic
        ON ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.CMS_RawConsumptionLogs')
      AND i.name = N'UX_CMS_RawConsumptionLogs_Dedup'
      AND i.is_unique = 1
    ORDER BY ic.key_ordinal
    FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(400)'), 1, 1, N'');
PRINT N'CHECK UX_CMS_RawConsumptionLogs_Dedup | Keys=' + ISNULL(@UxKeys, N'<missing>') + N' | '
    + CASE WHEN @UxKeys = N'MachineIP,EnrollNumber,PunchTime' THEN N'PASS' ELSE N'FAIL' END;
IF ISNULL(@UxKeys, N'') <> N'MachineIP,EnrollNumber,PunchTime' SET @Fail += 1;

DECLARE @Trig INT = (
    SELECT COUNT(*) FROM sys.triggers
    WHERE parent_id = OBJECT_ID(N'dbo.CMS_RawConsumptionLogs')
);
PRINT N'CHECK Triggers (expect 0) | ' + CAST(@Trig AS NVARCHAR(10)) + N' | '
    + CASE WHEN @Trig = 0 THEN N'PASS' ELSE N'FAIL' END;
IF @Trig <> 0 SET @Fail += 1;

PRINT N'--- DBCC CHECKCONSTRAINTS ---';
BEGIN TRY
    DBCC CHECKCONSTRAINTS (N'dbo.CMS_RawConsumptionLogs') WITH NO_INFOMSGS;
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | PASS';
END TRY
BEGIN CATCH
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | FAIL: ' + ERROR_MESSAGE();
    SET @Fail += 1;
END CATCH;

PRINT N'';
PRINT N'----- PASS CHECKLIST -----';
PRINT N'Schema unchanged           | ' + CASE WHEN @Shared = 10 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Unique index retained      | ' + CASE WHEN ISNULL(@UxKeys, N'') = N'MachineIP,EnrollNumber,PunchTime' THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Row count floor 772493     | ' + CASE WHEN @Cnt >= @Floor THEN N'PASS' ELSE N'FAIL' END;
PRINT N'PK unchanged               | ' + CASE WHEN ISNULL(@PkCols, N'') = N'LogId' THEN N'PASS' ELSE N'FAIL' END;
PRINT N'LogId identity             | ' + CASE WHEN @Ident = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'DBCC CHECKCONSTRAINTS      | (see above)';
PRINT N'';
IF @Fail = 0
    PRINT N'===== OVERALL: PASS =====';
ELSE
    PRINT N'===== OVERALL: FAIL (' + CAST(@Fail AS NVARCHAR(10)) + N' checks) — do not proceed to Table 13 =====';
GO
