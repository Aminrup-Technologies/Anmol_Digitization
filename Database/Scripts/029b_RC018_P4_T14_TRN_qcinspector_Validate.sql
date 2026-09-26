/* ============================================================================
   NAME:        RC-018 Phase 4 / Table 14 — TRN_qcinspector VALIDATION
   WHEN:        2026-09-25
   TARGET:      LIVE dbo.TRN_qcinspector (AnmolDristi)
   WHAT:        Read-only PASS/FAIL. No DML.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Fail INT = 0;
DECLARE @Floor INT = 80474;

PRINT N'===== RC-018 P4-T14 TRN_qcinspector VALIDATION =====';

DECLARE @Cnt INT = (SELECT COUNT(*) FROM dbo.TRN_qcinspector);
PRINT N'CHECK Row count preserved | Actual=' + CAST(@Cnt AS NVARCHAR(20))
    + N' Floor=' + CAST(@Floor AS NVARCHAR(20)) + N' | '
    + CASE WHEN @Cnt >= @Floor THEN N'PASS' ELSE N'FAIL' END;
IF @Cnt < @Floor SET @Fail += 1;

DECLARE @Shared INT = (
    SELECT COUNT(*)
    FROM (VALUES
        (N'ID'), (N'PlantName'), (N'Line'), (N'ProductCategory'), (N'ProductBrand'), (N'SKUId'),
        (N'NumberOfPieces'), (N'GaugeValue'), (N'Length_comments'), (N'DryWeight'), (N'Breadth_comments'),
        (N'DippedWeight'), (N'Height_comments'), (N'VarietyOrLotNo'), (N'BakingTime'), (N'BakingTime_comments'),
        (N'ColorAppearance'), (N'CommentsForColorAppearance'), (N'FlavourAndTaste'), (N'CommentsForFlavourAndTaste'),
        (N'DesignImplementation'), (N'CommentsForDesignImplementation'), (N'TextureBite'), (N'CommentsForTextureBite'),
        (N'ShapeOrSize'), (N'CommentsForShapeOrSize'), (N'Moisture'), (N'Moisture_comments'), (N'GaugeLength'),
        (N'CommentsGaugeLength'), (N'WeightWithoutOil'), (N'WeightWithOil'), (N'OilPercentage'), (N'PacketWeight'),
        (N'DesignAndImplementation'), (N'ColourAndAppearance'), (N'SubmittedById'), (N'SubmittedDate'),
        (N'SubmittedTime'), (N'Shift'), (N'SubmittedByPNo'), (N'ViewMode'), (N'DeleteMode'), (N'BakingTime2'),
        (N'BakingTime2_comments'), (N'aW_max'), (N'aW_max_comments'), (N'pH_value'), (N'pH_value_comments'),
        (N'FormID'), (N'SubmittedByEmployeeCode'), (N'Approver1EmployeeCode'), (N'Approver1_Status'),
        (N'Approver1_TimeStamp'), (N'Approver2EmployeeCode'), (N'Approver2_Status'), (N'Approver2_TimeStamp'),
        (N'DottedLineApproverEmployeeCode'), (N'DottedApprover_Status'), (N'DottedApprover_TimeStamp'),
        (N'Remarks'), (N'Shell10Weight'), (N'Shell5CreamWeight'), (N'CreamPercentage'), (N'QC_Status')
    ) v(ColumnName)
    WHERE COL_LENGTH(N'dbo.TRN_qcinspector', v.ColumnName) IS NOT NULL
);
PRINT N'CHECK Shared schema | ' + CAST(@Shared AS NVARCHAR(10)) + N'/65 | '
    + CASE WHEN @Shared = 65 THEN N'PASS' ELSE N'FAIL' END;
IF @Shared <> 65 SET @Fail += 1;

DECLARE @ShiftDate BIT = CASE WHEN COL_LENGTH(N'dbo.TRN_qcinspector', N'ShiftDate') IS NOT NULL THEN 1 ELSE 0 END;
PRINT N'CHECK ShiftDate exists | ' + CASE WHEN @ShiftDate = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @ShiftDate = 0 SET @Fail += 1;

DECLARE @PkCols NVARCHAR(400);
SELECT @PkCols = STUFF((
    SELECT N',' + c.name
    FROM sys.key_constraints kc
    INNER JOIN sys.index_columns ic
        ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE kc.parent_object_id = OBJECT_ID(N'dbo.TRN_qcinspector')
      AND kc.type = N'PK' AND ic.is_included_column = 0
    ORDER BY ic.key_ordinal
    FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(400)'), 1, 1, N'');
PRINT N'CHECK PK | ' + ISNULL(@PkCols, N'<none>') + N' | '
    + CASE WHEN @PkCols = N'ID' THEN N'PASS' ELSE N'FAIL' END;
IF ISNULL(@PkCols, N'') <> N'ID' SET @Fail += 1;

DECLARE @PkIndex BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.TRN_qcinspector')
      AND is_primary_key = 1
      AND name = N'PK__TRN_qcin__3214EC270C3985F8'
) THEN 1 ELSE 0 END;
PRINT N'CHECK PK index remains | ' + CASE WHEN @PkIndex = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @PkIndex = 0 SET @Fail += 1;

DECLARE @ExpectedKeys NVARCHAR(200) = N'FormID,SubmittedDate,PlantName,Line,Shift';
DECLARE @ComplianceKeys NVARCHAR(400);
DECLARE @ReportKeys NVARCHAR(400);

SELECT @ComplianceKeys = STUFF((
    SELECT N',' + c.name
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic
        ON ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.TRN_qcinspector')
      AND i.name = N'IX_TRN_qcinspector_Compliance'
    ORDER BY ic.key_ordinal
    FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(400)'), 1, 1, N'');

SELECT @ReportKeys = STUFF((
    SELECT N',' + c.name
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic
        ON ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.TRN_qcinspector')
      AND i.name = N'IX_TRN_qcinspector_Report'
    ORDER BY ic.key_ordinal
    FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(400)'), 1, 1, N'');

PRINT N'CHECK IX_TRN_qcinspector_Compliance | ' + ISNULL(@ComplianceKeys, N'<missing>') + N' | '
    + CASE WHEN @ComplianceKeys = @ExpectedKeys THEN N'PASS' ELSE N'FAIL' END;
PRINT N'CHECK IX_TRN_qcinspector_Report | ' + ISNULL(@ReportKeys, N'<missing>') + N' | '
    + CASE WHEN @ReportKeys = @ExpectedKeys THEN N'PASS' ELSE N'FAIL' END;
IF ISNULL(@ComplianceKeys, N'') <> @ExpectedKeys OR ISNULL(@ReportKeys, N'') <> @ExpectedKeys
    SET @Fail += 1;

DECLARE @Fk1 BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.TRN_qcinspector')
      AND name = N'FK_TRN_qcinspector_UserMaster_Approver1' AND is_disabled = 0 AND is_not_trusted = 0
) THEN 1 ELSE 0 END;
DECLARE @Fk2 BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.TRN_qcinspector')
      AND name = N'FK_TRN_qcinspector_UserMaster_Approver2' AND is_disabled = 0 AND is_not_trusted = 0
) THEN 1 ELSE 0 END;
DECLARE @Fk3 BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.TRN_qcinspector')
      AND name = N'FK_TRN_qcinspector_UserMaster_DottedLineApprover' AND is_disabled = 0 AND is_not_trusted = 0
) THEN 1 ELSE 0 END;
PRINT N'CHECK three UserMaster FKs | '
    + CASE WHEN @Fk1 = 1 AND @Fk2 = 1 AND @Fk3 = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @Fk1 = 0 OR @Fk2 = 0 OR @Fk3 = 0 SET @Fail += 1;

PRINT N'--- DBCC CHECKCONSTRAINTS ---';
BEGIN TRY
    DBCC CHECKCONSTRAINTS (N'dbo.TRN_qcinspector') WITH NO_INFOMSGS;
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | PASS';
END TRY
BEGIN CATCH
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | FAIL: ' + ERROR_MESSAGE();
    SET @Fail += 1;
END CATCH;

PRINT N'';
PRINT N'----- PASS CHECKLIST -----';
PRINT N'Shared schema unchanged    | ' + CASE WHEN @Shared = 65 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'ShiftDate retained         | ' + CASE WHEN @ShiftDate = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'PK unchanged               | ' + CASE WHEN ISNULL(@PkCols, N'') = N'ID' THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Indexes remain             | ' + CASE WHEN @PkIndex = 1 AND ISNULL(@ComplianceKeys, N'') = @ExpectedKeys AND ISNULL(@ReportKeys, N'') = @ExpectedKeys THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Row count floor 80474      | ' + CASE WHEN @Cnt >= @Floor THEN N'PASS' ELSE N'FAIL' END;
PRINT N'DBCC CHECKCONSTRAINTS      | (see above)';
PRINT N'';
IF @Fail = 0
    PRINT N'===== OVERALL: PASS =====';
ELSE
    PRINT N'===== OVERALL: FAIL (' + CAST(@Fail AS NVARCHAR(10)) + N' checks) =====';
GO
