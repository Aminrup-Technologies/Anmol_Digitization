/*
  VMS same-day multi-visit history
  - VMS_VisitDayLog remains one row per (LogID, VisitDate) — Admin SingleDay sync depends on that
  - Completed check-in/check-out cycles are stored here so a later visit cannot overwrite earlier times
  Run against AnmolDristi / AnmolDristiDev before deploying code changes.
*/

IF OBJECT_ID('dbo.VMS_VisitAttendanceHistory', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.VMS_VisitAttendanceHistory (
        HistoryID int IDENTITY(1,1) NOT NULL,
        LogID int NOT NULL,
        DayLogID int NULL,
        VisitDate date NOT NULL,
        CheckInTime datetime NOT NULL,
        CheckOutTime datetime NOT NULL,
        RecordedOn datetime NOT NULL CONSTRAINT DF_VMS_VisitAttendanceHistory_RecordedOn DEFAULT (GETDATE()),
        CONSTRAINT PK_VMS_VisitAttendanceHistory PRIMARY KEY CLUSTERED (HistoryID ASC)
    );

    CREATE NONCLUSTERED INDEX IX_VMS_VisitAttendanceHistory_LogID
        ON dbo.VMS_VisitAttendanceHistory (LogID, HistoryID);
END
GO
