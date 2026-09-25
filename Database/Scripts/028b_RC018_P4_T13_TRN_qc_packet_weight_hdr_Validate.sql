/* ============================================================================
   NAME:        RC-018 Phase 4 / Table 13 — TRN_qc_packet_weight_hdr VALIDATION
   WHEN:        2026-09-25
   TARGET:      LIVE dbo.TRN_qc_packet_weight_hdr (AnmolDristi)
   WHAT:        Read-only PASS/FAIL. No DML.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Fail INT = 0;
DECLARE @Floor INT = 37952;

PRINT N'===== RC-018 P4-T13 TRN_qc_packet_weight_hdr VALIDATION =====';

DECLARE @Cnt INT = (SELECT COUNT(*) FROM dbo.TRN_qc_packet_weight_hdr);
PRINT N'CHECK Row count preserved | Actual=' + CAST(@Cnt AS NVARCHAR(20))
    + N' Floor=' + CAST(@Floor AS NVARCHAR(20)) + N' | '
    + CASE WHEN @Cnt >= @Floor THEN N'PASS' ELSE N'FAIL' END;
IF @Cnt < @Floor SET @Fail += 1;

DECLARE @Shared INT = (
    SELECT COUNT(*)
    FROM (VALUES
        (N'PacketHdrID'), (N'QCInspectorID'), (N'PlantName'), (N'Line'), (N'SKUId'), (N'Variety'),
        (N'StandardWeight'), (N'Shift'), (N'EntryDate'), (N'WindowStartTime'), (N'WindowEndTime'),
        (N'MinWeight'), (N'MaxWeight'), (N'AvgWeight'), (N'GiveAwayPercent'), (N'Status'),
        (N'SubmittedById'), (N'SubmittedOn'), (N'ProductCategory'), (N'ProductBrand'), (N'TareWeight'),
        (N'NetMinWeight'), (N'NetMaxWeight'), (N'NetAvgWeight'), (N'IsUnderweight'), (N'UnderweightDeficit')
    ) v(ColumnName)
    WHERE COL_LENGTH(N'dbo.TRN_qc_packet_weight_hdr', v.ColumnName) IS NOT NULL
);
PRINT N'CHECK Shared schema | ' + CAST(@Shared AS NVARCHAR(10)) + N'/26 | '
    + CASE WHEN @Shared = 26 THEN N'PASS' ELSE N'FAIL' END;
IF @Shared <> 26 SET @Fail += 1;

DECLARE @FormId BIT = CASE WHEN COL_LENGTH(N'dbo.TRN_qc_packet_weight_hdr', N'FormID') IS NOT NULL THEN 1 ELSE 0 END;
DECLARE @FormDf BIT = CASE WHEN EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.TRN_qc_packet_weight_hdr')
      AND c.name = N'FormID'
      AND REPLACE(REPLACE(dc.definition, N' ', N''), N'(', N'') LIKE N'%21%'
) THEN 1 ELSE 0 END;
PRINT N'CHECK FormID exists with default ((21)) | '
    + CASE WHEN @FormId = 1 AND @FormDf = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @FormId = 0 OR @FormDf = 0 SET @Fail += 1;

DECLARE @PkCols NVARCHAR(400);
SELECT @PkCols = STUFF((
    SELECT N',' + c.name
    FROM sys.key_constraints kc
    INNER JOIN sys.index_columns ic
        ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE kc.parent_object_id = OBJECT_ID(N'dbo.TRN_qc_packet_weight_hdr')
      AND kc.type = N'PK' AND ic.is_included_column = 0
    ORDER BY ic.key_ordinal
    FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(400)'), 1, 1, N'');
PRINT N'CHECK PK | ' + ISNULL(@PkCols, N'<none>') + N' | '
    + CASE WHEN @PkCols = N'PacketHdrID' THEN N'PASS' ELSE N'FAIL' END;
IF ISNULL(@PkCols, N'') <> N'PacketHdrID' SET @Fail += 1;

DECLARE @Ident INT = COLUMNPROPERTY(OBJECT_ID(N'dbo.TRN_qc_packet_weight_hdr'), N'PacketHdrID', N'IsIdentity');
PRINT N'CHECK PacketHdrID identity | ' + CASE WHEN @Ident = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @Ident <> 1 SET @Fail += 1;

DECLARE @PkIndex BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.TRN_qc_packet_weight_hdr')
      AND is_primary_key = 1
      AND name = N'PK__TRN_qc_p__12D873D48888808D'
) THEN 1 ELSE 0 END;
PRINT N'CHECK PK index remains | '
    + CASE WHEN @PkIndex = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @PkIndex = 0 SET @Fail += 1;

DECLARE @Fk BIT = CASE WHEN EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.TRN_qc_packet_weight_hdr')
      AND name = N'FK_QCInspector_PacketHdr'
      AND is_disabled = 0
      AND is_not_trusted = 0
) THEN 1 ELSE 0 END;
PRINT N'CHECK FK_QCInspector_PacketHdr trusted | '
    + CASE WHEN @Fk = 1 THEN N'PASS' ELSE N'FAIL' END;
IF @Fk = 0 SET @Fail += 1;

PRINT N'--- DBCC CHECKCONSTRAINTS ---';
BEGIN TRY
    DBCC CHECKCONSTRAINTS (N'dbo.TRN_qc_packet_weight_hdr') WITH NO_INFOMSGS;
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | PASS';
END TRY
BEGIN CATCH
    PRINT N'CHECK DBCC CHECKCONSTRAINTS | FAIL: ' + ERROR_MESSAGE();
    SET @Fail += 1;
END CATCH;

PRINT N'';
PRINT N'----- PASS CHECKLIST -----';
PRINT N'Shared schema unchanged    | ' + CASE WHEN @Shared = 26 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'FormID retained            | ' + CASE WHEN @FormId = 1 AND @FormDf = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'PK unchanged               | ' + CASE WHEN ISNULL(@PkCols, N'') = N'PacketHdrID' THEN N'PASS' ELSE N'FAIL' END;
PRINT N'PK index remains           | ' + CASE WHEN @PkIndex = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'FK remains                 | ' + CASE WHEN @Fk = 1 THEN N'PASS' ELSE N'FAIL' END;
PRINT N'Row count floor 37952      | ' + CASE WHEN @Cnt >= @Floor THEN N'PASS' ELSE N'FAIL' END;
PRINT N'DBCC CHECKCONSTRAINTS      | (see above)';
PRINT N'';
IF @Fail = 0
    PRINT N'===== OVERALL: PASS =====';
ELSE
    PRINT N'===== OVERALL: FAIL (' + CAST(@Fail AS NVARCHAR(10)) + N' checks) — do not proceed to Table 14 =====';
GO
