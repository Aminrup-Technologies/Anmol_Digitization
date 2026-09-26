/* ============================================================================
   NAME:        RC-018 Phase 3 / Table 11 — VMS_VisitAttendanceHistory indexes
   WHEN:        2026-09-25
   SOURCE:      docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md
                AnmolDristi/Database/VMS_AttendanceHistory_AfterHoursAudit_Migration.sql
   TARGET:      LIVE dbo.VMS_VisitAttendanceHistory (AnmolDristi)
   WHAT:        Create two non-unique indexes if absent.
                Create UQ_VMS_VisitAttendanceHistory_OpenDayLog only when no
                DayLogID has more than one row with CheckOutTime IS NULL.
   FORBIDDEN:   DELETE, TRUNCATE, recreate, data changes, forced unique index.
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
DECLARE @BackupTable SYSNAME      = N'dbo.RC018_Backup_VMS_VisitAttendanceHistory_' + @RunTag;
DECLARE @Sql         NVARCHAR(MAX);
DECLARE @BeforeCnt   INT;
DECLARE @DupGroups   INT = 0;
DECLARE @NeedDay     BIT = 0;
DECLARE @NeedLog     BIT = 0;
DECLARE @NeedUq      BIT = 0;

PRINT N'RC-018 P3-T11 VMS_VisitAttendanceHistory | RunTag=' + @RunTag
    + N' | DryRun=' + CAST(@DryRun AS NVARCHAR(1));

IF OBJECT_ID(N'dbo.VMS_VisitAttendanceHistory', N'U') IS NULL
BEGIN
    RAISERROR(N'BLOCKER: dbo.VMS_VisitAttendanceHistory not found. Abort.', 16, 1);
    RETURN;
END;

SELECT @BeforeCnt = COUNT(*) FROM dbo.VMS_VisitAttendanceHistory;
PRINT N'Preflight rows=' + CAST(@BeforeCnt AS NVARCHAR(20)) + N' (Phase 0 LIVE 4).';

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.VMS_VisitAttendanceHistory')
      AND name = N'IX_VMS_VisitAttendanceHistory_DayLogID'
)
    SET @NeedDay = 1;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.VMS_VisitAttendanceHistory')
      AND name = N'IX_VMS_VisitAttendanceHistory_LogID'
)
    SET @NeedLog = 1;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.VMS_VisitAttendanceHistory')
      AND name = N'UQ_VMS_VisitAttendanceHistory_OpenDayLog'
)
    SET @NeedUq = 1;

PRINT N'Missing DayLogID index=' + CAST(@NeedDay AS NCHAR(1))
    + N' LogID index=' + CAST(@NeedLog AS NCHAR(1))
    + N' OpenDayLog unique=' + CAST(@NeedUq AS NCHAR(1));

SELECT @DupGroups = COUNT(*)
FROM (
    SELECT DayLogID
    FROM dbo.VMS_VisitAttendanceHistory
    WHERE CheckOutTime IS NULL
    GROUP BY DayLogID
    HAVING COUNT(*) > 1
) d;

PRINT N'Open DayLogID duplicate groups=' + CAST(@DupGroups AS NVARCHAR(20));

IF @DupGroups > 0
BEGIN
    PRINT N'Duplicate open rows (CheckOutTime IS NULL). Unique index will not be created.';
    SELECT h.HistoryID, h.LogID, h.DayLogID, h.CheckInTime, h.CheckOutTime, h.Source, h.Actor
    FROM dbo.VMS_VisitAttendanceHistory h
    INNER JOIN (
        SELECT DayLogID
        FROM dbo.VMS_VisitAttendanceHistory
        WHERE CheckOutTime IS NULL
        GROUP BY DayLogID
        HAVING COUNT(*) > 1
    ) d ON d.DayLogID = h.DayLogID
    WHERE h.CheckOutTime IS NULL
    ORDER BY h.DayLogID, h.HistoryID;
END;

IF @NeedDay = 0 AND @NeedLog = 0 AND @NeedUq = 0
BEGIN
    PRINT N'All three indexes already exist. No DDL.';
    RETURN;
END;

IF @NeedUq = 1 AND @DupGroups > 0 AND @NeedDay = 0 AND @NeedLog = 0
BEGIN
    PRINT N'Only the unique index is missing, and duplicates block it. No index change. No backup.';
    RETURN;
END;

IF @DryRun = 1
BEGIN
    PRINT N'DryRun=1 — backup and CREATE INDEX skipped.';
    RETURN;
END;

IF OBJECT_ID(@BackupTable, N'U') IS NOT NULL
BEGIN
    RAISERROR(N'BLOCKER: backup table already exists for this RunTag. Abort.', 16, 1);
    RETURN;
END;

SET @Sql = N'SELECT * INTO ' + @BackupTable + N' FROM dbo.VMS_VisitAttendanceHistory WITH (TABLOCKX);';
EXEC sys.sp_executesql @Sql;
PRINT N'Backup created: ' + @BackupTable;

BEGIN TRY
    BEGIN TRAN;

    IF @NeedDay = 1
    BEGIN
        CREATE NONCLUSTERED INDEX IX_VMS_VisitAttendanceHistory_DayLogID
            ON dbo.VMS_VisitAttendanceHistory (DayLogID ASC, CheckInTime ASC);
        PRINT N'Created IX_VMS_VisitAttendanceHistory_DayLogID.';
    END;

    IF @NeedLog = 1
    BEGIN
        CREATE NONCLUSTERED INDEX IX_VMS_VisitAttendanceHistory_LogID
            ON dbo.VMS_VisitAttendanceHistory (LogID ASC, CheckInTime ASC);
        PRINT N'Created IX_VMS_VisitAttendanceHistory_LogID.';
    END;

    IF @NeedUq = 1 AND @DupGroups = 0
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UQ_VMS_VisitAttendanceHistory_OpenDayLog
            ON dbo.VMS_VisitAttendanceHistory (DayLogID ASC)
            WHERE CheckOutTime IS NULL;
        PRINT N'Created UQ_VMS_VisitAttendanceHistory_OpenDayLog.';
    END
    ELSE IF @NeedUq = 1
        PRINT N'SKIP UQ_VMS_VisitAttendanceHistory_OpenDayLog. Duplicate open DayLogID rows were printed above.';

    IF (SELECT COUNT(*) FROM dbo.VMS_VisitAttendanceHistory) <> @BeforeCnt
        RAISERROR(N'BLOCKER: row count changed during index build.', 16, 1);

    COMMIT TRAN;
    PRINT N'Index step committed. Rows preserved=' + CAST(@BeforeCnt AS NVARCHAR(20));
    PRINT N'Next: 026b_RC018_P3_T11_VMS_VisitAttendanceHistory_Validate.sql';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'RC-018 P3-T11 FAILED: %s', 16, 1, @Err);
END CATCH;
GO
