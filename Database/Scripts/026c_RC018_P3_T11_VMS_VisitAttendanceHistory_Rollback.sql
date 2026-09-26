/* ============================================================================
   NAME:        RC-018 Phase 3 / Table 11 — VMS_VisitAttendanceHistory ROLLBACK
   WHEN:        2026-09-25
   TARGET:      LIVE dbo.VMS_VisitAttendanceHistory (AnmolDristi)
   WHAT:        Drop the three indexes this RC adds. Does not drop the PK.
                Does not delete or update rows.
   NOTE:        Set @Confirm = 1.
============================================================================ */

USE [AnmolDristi];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Confirm BIT = 0;

PRINT N'RC-018 P3-T11 VMS_VisitAttendanceHistory ROLLBACK';

IF @Confirm <> 1
BEGIN
    PRINT N'Confirm=0 — no changes.';
    RETURN;
END;

BEGIN TRY
    BEGIN TRAN;

    DECLARE @Before INT = (SELECT COUNT(*) FROM dbo.VMS_VisitAttendanceHistory);

    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.VMS_VisitAttendanceHistory')
          AND name = N'UQ_VMS_VisitAttendanceHistory_OpenDayLog'
    )
    BEGIN
        DROP INDEX UQ_VMS_VisitAttendanceHistory_OpenDayLog ON dbo.VMS_VisitAttendanceHistory;
        PRINT N'Dropped UQ_VMS_VisitAttendanceHistory_OpenDayLog.';
    END;

    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.VMS_VisitAttendanceHistory')
          AND name = N'IX_VMS_VisitAttendanceHistory_DayLogID'
    )
    BEGIN
        DROP INDEX IX_VMS_VisitAttendanceHistory_DayLogID ON dbo.VMS_VisitAttendanceHistory;
        PRINT N'Dropped IX_VMS_VisitAttendanceHistory_DayLogID.';
    END;

    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.VMS_VisitAttendanceHistory')
          AND name = N'IX_VMS_VisitAttendanceHistory_LogID'
    )
    BEGIN
        DROP INDEX IX_VMS_VisitAttendanceHistory_LogID ON dbo.VMS_VisitAttendanceHistory;
        PRINT N'Dropped IX_VMS_VisitAttendanceHistory_LogID.';
    END;

    DECLARE @After INT = (SELECT COUNT(*) FROM dbo.VMS_VisitAttendanceHistory);
    IF @After <> @Before
        RAISERROR(N'BLOCKER: rollback changed row count %d -> %d.', 16, 1, @Before, @After);

    COMMIT TRAN;
    PRINT N'ROLLBACK committed. Rows preserved. Count=' + CAST(@After AS NVARCHAR(20));
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'RC-018 P3-T11 ROLLBACK FAILED: %s', 16, 1, @Err);
END CATCH;
GO
