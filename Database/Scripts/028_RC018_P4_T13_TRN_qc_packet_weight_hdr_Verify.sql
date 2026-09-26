/* ============================================================================
   NAME:        RC-018 Phase 4 / Table 13 — TRN_qc_packet_weight_hdr verification
   WHEN:        2026-09-25
   SOURCE:      docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md
   TARGET:      LIVE dbo.TRN_qc_packet_weight_hdr (AnmolDristi)
   WHAT:        Verification only. Exit with no DDL when shared schema matches
                and FormID is still present. Never ALTER FormID.
                Backup only if a genuine alignment gap exists.
   FORBIDDEN:   DELETE, TRUNCATE, recreate, data changes, FormID alter.
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
DECLARE @BackupTable SYSNAME      = N'dbo.RC018_Backup_TRN_qc_packet_weight_hdr_' + @RunTag;
DECLARE @Sql         NVARCHAR(MAX);
DECLARE @Gaps        INT = 0;
DECLARE @RowCnt      INT;
DECLARE @Shared      INT;

PRINT N'RC-018 P4-T13 TRN_qc_packet_weight_hdr | RunTag=' + @RunTag
    + N' | DryRun=' + CAST(@DryRun AS NVARCHAR(1));

IF OBJECT_ID(N'dbo.TRN_qc_packet_weight_hdr', N'U') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: dbo.TRN_qc_packet_weight_hdr not found. Abort.', 16, 1);
    RETURN;
END;

SELECT @RowCnt = COUNT(*) FROM dbo.TRN_qc_packet_weight_hdr;
PRINT N'LIVE row count=' + CAST(@RowCnt AS NVARCHAR(20))
    + N' (Phase 0 floor 37952). UAT count is not a target.';

IF COL_LENGTH(N'dbo.TRN_qc_packet_weight_hdr', N'FormID') IS NULL
BEGIN
    PRINT N'GAP: LIVE-only FormID is missing. This package will not add or alter it.';
    SET @Gaps += 1;
END
ELSE IF NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.TRN_qc_packet_weight_hdr')
      AND c.name = N'FormID'
      AND REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(ISNULL(dc.definition, N''), N' ', N''), CHAR(9), N''), CHAR(13), N''), CHAR(10), N''), N'(', N''), N')', N'') = N'21'
)
BEGIN
    PRINT N'GAP: FormID exists but its default is no longer ((21)). FormID will not be altered.';
    SET @Gaps += 1;
END
ELSE
    PRINT N'FormID present with default ((21)). It will not be altered.';

SELECT @Shared = COUNT(*)
FROM (VALUES
    (N'PacketHdrID'), (N'QCInspectorID'), (N'PlantName'), (N'Line'), (N'SKUId'), (N'Variety'),
    (N'StandardWeight'), (N'Shift'), (N'EntryDate'), (N'WindowStartTime'), (N'WindowEndTime'),
    (N'MinWeight'), (N'MaxWeight'), (N'AvgWeight'), (N'GiveAwayPercent'), (N'Status'),
    (N'SubmittedById'), (N'SubmittedOn'), (N'ProductCategory'), (N'ProductBrand'), (N'TareWeight'),
    (N'NetMinWeight'), (N'NetMaxWeight'), (N'NetAvgWeight'), (N'IsUnderweight'), (N'UnderweightDeficit')
) v(ColumnName)
WHERE COL_LENGTH(N'dbo.TRN_qc_packet_weight_hdr', v.ColumnName) IS NOT NULL;

IF @Shared <> 26
BEGIN
    PRINT N'GAP: shared columns present=' + CAST(@Shared AS NVARCHAR(10)) + N' expected=26.';
    SET @Gaps += 1;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.key_constraints kc
    INNER JOIN sys.index_columns ic
        ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id AND ic.key_ordinal = 1
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE kc.parent_object_id = OBJECT_ID(N'dbo.TRN_qc_packet_weight_hdr')
      AND kc.type = N'PK'
      AND c.name = N'PacketHdrID'
)
BEGIN
    PRINT N'GAP: PK is not PacketHdrID.';
    SET @Gaps += 1;
END;

IF COLUMNPROPERTY(OBJECT_ID(N'dbo.TRN_qc_packet_weight_hdr'), N'PacketHdrID', N'IsIdentity') <> 1
BEGIN
    PRINT N'GAP: PacketHdrID is not an identity column.';
    SET @Gaps += 1;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.TRN_qc_packet_weight_hdr')
      AND name = N'FK_QCInspector_PacketHdr'
      AND is_disabled = 0
)
BEGIN
    PRINT N'GAP: FK_QCInspector_PacketHdr missing or disabled.';
    SET @Gaps += 1;
END;

PRINT N'Alignment gaps=' + CAST(@Gaps AS NVARCHAR(10));

IF @Gaps = 0
BEGIN
    PRINT N'MODE: verification-only. Shared schema matches. FormID retained. No DDL. No backup.';
    RETURN;
END;

PRINT N'MODE: a genuine alignment gap exists. This package does not ALTER the table or FormID.';

IF @DryRun = 1
BEGIN
    PRINT N'DryRun=1 — backup skipped.';
    RETURN;
END;

IF OBJECT_ID(@BackupTable, N'U') IS NOT NULL
BEGIN
    RAISERROR(N'BLOCKER: backup table already exists for this RunTag. Abort.', 16, 1);
    RETURN;
END;

SET @Sql = N'SELECT * INTO ' + @BackupTable + N' FROM dbo.TRN_qc_packet_weight_hdr WITH (TABLOCKX);';
EXEC sys.sp_executesql @Sql;
PRINT N'Backup created because an alignment gap exists: ' + @BackupTable;
PRINT N'No DDL applied. FormID was not altered.';
GO
