/* ============================================================================
   NAME:        RC-018 Phase 4 / Table 12 — CMS_RawConsumptionLogs ROLLBACK
   WHEN:        2026-09-25
   TARGET:      LIVE dbo.CMS_RawConsumptionLogs (AnmolDristi)
   WHAT:        No-op. The verification package does not change schema or rows
                when alignment gaps are zero. It does not rebuild or drop
                UX_CMS_RawConsumptionLogs_Dedup. A review backup is not copied
                back over LIVE.
   NOTE:        @Confirm must be 1. It still performs no DML.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;

DECLARE @Confirm BIT = 0;

PRINT N'RC-018 P4-T12 CMS_RawConsumptionLogs ROLLBACK';

IF @Confirm <> 1
BEGIN
    PRINT N'Confirm=0 — no changes.';
    RETURN;
END;

PRINT N'No schema or log rows are rolled back.';
PRINT N'UX_CMS_RawConsumptionLogs_Dedup is left in place.';
PRINT N'If RC018_Backup_CMS_RawConsumptionLogs_<RunTag> exists, it is a review copy only.';
PRINT N'This script will not DELETE or reload CMS_RawConsumptionLogs.';
GO
