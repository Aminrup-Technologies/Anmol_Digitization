/* ============================================================================
   NAME:        RC-018 Phase 3 / Table 8 — SalesApprovalRequestDetail ROLLBACK
   WHEN:        2026-09-25
   TARGET:      LIVE dbo.SalesApprovalRequestDetail (AnmolDristi)
   WHAT:        No-op. The verification package does not change rows, identity,
                indexes, defaults, or constraints when the schema is aligned.
                A backup created because gaps were found is left in place.
                This script does not copy that backup back over LIVE.
   NOTE:        @Confirm must be 1. It still performs no DML.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;

DECLARE @Confirm BIT = 0;

PRINT N'RC-018 P3-T08 SalesApprovalRequestDetail ROLLBACK';

IF @Confirm <> 1
BEGIN
    PRINT N'Confirm=0 — no changes.';
    RETURN;
END;

PRINT N'No schema or data change is rolled back.';
PRINT N'Detail rows, identity, snapshot columns, and DetailVersion stay as they are.';
PRINT N'If RC018_Backup_SalesApprovalRequestDetail_<RunTag> exists, it is a review copy only.';
PRINT N'This script will not DELETE or reload SalesApprovalRequestDetail.';
GO
