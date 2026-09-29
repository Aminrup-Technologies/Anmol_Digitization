/*
  VMS after-hours approval audit + same-day attendance history
  - VMS_AfterHoursApprovalAudit: one row per after-hours approval event
  - VMS_VisitAttendanceHistory: append-only physical check-in/out cycles
  Does not alter UQ_VMS_VisitDayLog_LogDate or VisitLog/DayLog snapshot columns.
  Run against AnmolDristi / AnmolDristiDev before deploying code that writes these tables.
*/

IF OBJECT_ID('dbo.VMS_AfterHoursApprovalAudit', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.VMS_AfterHoursApprovalAudit (
        AuditID int IDENTITY(1,1) NOT NULL,
        LogID int NOT NULL,
        DayLogID int NULL,
        BranchID int NOT NULL,
        HostEmpCode varchar(50) NULL,
        ScheduledDate date NOT NULL,
        ScheduledFrom datetime NOT NULL,
        ScheduledTo datetime NOT NULL,
        PlantOpenTime time(7) NULL,
        PlantCloseTime time(7) NULL,
        ApprovedOn datetime NOT NULL CONSTRAINT DF_VMS_AfterHoursApprovalAudit_ApprovedOn DEFAULT (GETDATE()),
        ApprovedBy varchar(50) NULL,
        Reason varchar(500) NULL,
        CONSTRAINT PK_VMS_AfterHoursApprovalAudit PRIMARY KEY CLUSTERED (AuditID ASC)
    );

    CREATE INDEX IX_VMS_AfterHoursApprovalAudit_LogID
        ON dbo.VMS_AfterHoursApprovalAudit (LogID, ApprovedOn);
END
GO

IF OBJECT_ID('dbo.VMS_VisitAttendanceHistory', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.VMS_VisitAttendanceHistory (
        HistoryID int IDENTITY(1,1) NOT NULL,
        LogID int NOT NULL,
        DayLogID int NOT NULL,
        CheckInTime datetime NOT NULL,
        CheckOutTime datetime NULL,
        Source varchar(20) NOT NULL,
        Actor varchar(50) NULL,
        CreatedOn datetime NOT NULL CONSTRAINT DF_VMS_VisitAttendanceHistory_CreatedOn DEFAULT (GETDATE()),
        CONSTRAINT PK_VMS_VisitAttendanceHistory PRIMARY KEY CLUSTERED (HistoryID ASC)
    );

    CREATE INDEX IX_VMS_VisitAttendanceHistory_DayLogID
        ON dbo.VMS_VisitAttendanceHistory (DayLogID, CheckInTime);

    CREATE INDEX IX_VMS_VisitAttendanceHistory_LogID
        ON dbo.VMS_VisitAttendanceHistory (LogID, CheckInTime);

    /* One open (in-progress) attendance row per DayLog. */
    CREATE UNIQUE INDEX UQ_VMS_VisitAttendanceHistory_OpenDayLog
        ON dbo.VMS_VisitAttendanceHistory (DayLogID)
        WHERE CheckOutTime IS NULL;
END
GO
