/* ============================================================================
   NAME:        RC-018 Phase 4 / Table 13 — TRN_qc_packet_weight_hdr ROLLBACK
   WHEN:        2026-09-25
   TARGET:      LIVE dbo.TRN_qc_packet_weight_hdr (AnmolDristi)
   WHAT:        No-op. The verification package does not change schema or rows
                when the shared schema matches. It does not alter FormID.
                A review backup is not copied back over LIVE.
   NOTE:        @Confirm must be 1. It still performs no DML.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;

DECLARE @Confirm BIT = 0;

PRINT N'RC-018 P4-T13 TRN_qc_packet_weight_hdr ROLLBACK';

IF @Confirm <> 1
BEGIN
    PRINT N'Confirm=0 — no changes.';
    RETURN;
END;

PRINT N'No schema or packet-weight rows are rolled back.';
PRINT N'FormID and DF_TRN_qc_packet_weight_hdr_FormID are left in place.';
PRINT N'If RC018_Backup_TRN_qc_packet_weight_hdr_<RunTag> exists, it is a review copy only.';
PRINT N'This script will not DELETE or reload TRN_qc_packet_weight_hdr.';
GO
