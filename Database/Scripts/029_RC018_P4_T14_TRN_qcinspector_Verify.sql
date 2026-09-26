/* ============================================================================
   NAME:        RC-018 Phase 4 / Table 14 — TRN_qcinspector verification
   WHEN:        2026-09-25
   SOURCE:      docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md
   TARGET:      LIVE dbo.TRN_qcinspector (AnmolDristi)
   WHAT:        Verification only. Exit with no DDL when the shared schema
                matches and ShiftDate plus the LIVE report indexes remain.
                Never ALTER ShiftDate. Backup only if a genuine gap exists.
   FORBIDDEN:   DELETE, TRUNCATE, recreate, data changes, ShiftDate alter.
   NOTE:        DO NOT EXECUTE until Production Gate. Default @DryRun = 1.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @DryRun BIT = 1;

DECLARE @RunTag      NVARCHAR(32) = CONVERT(CHAR(8), GETDATE(), 112)
                                 + N'_' + REPLACE(CONVERT(CHAR(8), GETDATE(), 108), N':', N'');
DECLARE @BackupTable SYSNAME      = N'dbo.RC018_Backup_TRN_qcinspector_' + @RunTag;
DECLARE @Sql         NVARCHAR(MAX);
DECLARE @Gaps        INT = 0;
DECLARE @RowCnt      INT;
DECLARE @Shared      INT;
DECLARE @ComplianceKeys NVARCHAR(400);
DECLARE @ReportKeys     NVARCHAR(400);

PRINT N'RC-018 P4-T14 TRN_qcinspector | RunTag=' + @RunTag
    + N' | DryRun=' + CAST(@DryRun AS NVARCHAR(1));

IF OBJECT_ID(N'dbo.TRN_qcinspector', N'U') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: dbo.TRN_qcinspector not found. Abort.', 16, 1);
    RETURN;
END;

SELECT @RowCnt = COUNT(*) FROM dbo.TRN_qcinspector;
PRINT N'LIVE row count=' + CAST(@RowCnt AS NVARCHAR(20))
    + N' (Phase 0 floor 80474). UAT count is not a target.';

IF COL_LENGTH(N'dbo.TRN_qcinspector', N'ShiftDate') IS NULL
BEGIN
    PRINT N'GAP: LIVE-only ShiftDate is missing. This package will not add or alter it.';
    SET @Gaps += 1;
END
ELSE
    PRINT N'ShiftDate present. It will not be altered.';

SELECT @Shared = COUNT(*)
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
WHERE COL_LENGTH(N'dbo.TRN_qcinspector', v.ColumnName) IS NOT NULL;

IF @Shared <> 65
BEGIN
    PRINT N'GAP: shared columns present=' + CAST(@Shared AS NVARCHAR(10)) + N' expected=65.';
    SET @Gaps += 1;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.key_constraints kc
    INNER JOIN sys.index_columns ic
        ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id AND ic.key_ordinal = 1
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE kc.parent_object_id = OBJECT_ID(N'dbo.TRN_qcinspector')
      AND kc.type = N'PK'
      AND c.name = N'ID'
)
BEGIN
    PRINT N'GAP: PK is not ID.';
    SET @Gaps += 1;
END;

IF COLUMNPROPERTY(OBJECT_ID(N'dbo.TRN_qcinspector'), N'ID', N'IsIdentity') <> 1
BEGIN
    PRINT N'GAP: ID is not an identity column.';
    SET @Gaps += 1;
END;

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

IF ISNULL(@ComplianceKeys, N'') <> N'FormID,SubmittedDate,PlantName,Line,Shift'
 OR ISNULL(@ReportKeys, N'') <> N'FormID,SubmittedDate,PlantName,Line,Shift'
BEGIN
    PRINT N'GAP: IX_TRN_qcinspector_Compliance or IX_TRN_qcinspector_Report is missing or its keys changed. They will not be altered.';
    SET @Gaps += 1;
END
ELSE
    PRINT N'LIVE indexes Compliance and Report retained.';

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.TRN_qcinspector')
      AND name = N'FK_TRN_qcinspector_UserMaster_Approver1'
      AND is_disabled = 0
)
 OR NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.TRN_qcinspector')
      AND name = N'FK_TRN_qcinspector_UserMaster_Approver2'
      AND is_disabled = 0
)
 OR NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.TRN_qcinspector')
      AND name = N'FK_TRN_qcinspector_UserMaster_DottedLineApprover'
      AND is_disabled = 0
)
BEGIN
    PRINT N'GAP: one of the three MST_UserMaster foreign keys is missing or disabled.';
    SET @Gaps += 1;
END;

PRINT N'Alignment gaps=' + CAST(@Gaps AS NVARCHAR(10));

IF @Gaps = 0
BEGIN
    PRINT N'MODE: verification-only. Shared schema matches. ShiftDate retained. No DDL. No backup.';
    RETURN;
END;

PRINT N'MODE: a genuine alignment gap exists. This package does not ALTER the table or ShiftDate.';

IF @DryRun = 1
BEGIN
    PRINT N'DryRun=1 — backup skipped. A gap backup of this table is large.';
    RETURN;
END;

IF OBJECT_ID(@BackupTable, N'U') IS NOT NULL
BEGIN
    RAISERROR(N'BLOCKER: backup table already exists for this RunTag. Abort.', 16, 1);
    RETURN;
END;

SET @Sql = N'SELECT * INTO ' + @BackupTable + N' FROM dbo.TRN_qcinspector WITH (TABLOCKX);';
EXEC sys.sp_executesql @Sql;
PRINT N'Backup created because an alignment gap exists: ' + @BackupTable;
PRINT N'No DDL applied. ShiftDate was not altered.';
GO
