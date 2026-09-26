/* ============================================================================
   NAME:        RC-018 Phase 4 / Table 14 — TRN_qcinspector ROLLBACK
   WHEN:        2026-09-25
   TARGET:      LIVE dbo.TRN_qcinspector (AnmolDristi)
   WHAT:        No-op. The verification package does not change schema or rows
                when the shared schema matches. It does not alter ShiftDate
                or the LIVE Compliance/Report indexes. A review backup is not
                copied back over LIVE.
   NOTE:        @Confirm must be 1. It still performs no DML.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;

DECLARE @Confirm BIT = 0;

PRINT N'RC-018 P4-T14 TRN_qcinspector ROLLBACK';

IF @Confirm <> 1
BEGIN
    PRINT N'Confirm=0 — no changes.';
    RETURN;
END;

PRINT N'No schema or inspector rows are rolled back.';
PRINT N'ShiftDate, IX_TRN_qcinspector_Compliance, and IX_TRN_qcinspector_Report stay in place.';
PRINT N'If RC018_Backup_TRN_qcinspector_<RunTag> exists, it is a review copy only.';
PRINT N'This script will not DELETE or reload TRN_qcinspector.';
GO
