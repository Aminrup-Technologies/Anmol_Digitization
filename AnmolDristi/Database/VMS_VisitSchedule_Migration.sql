/*
  VMS Visit Schedule migration
  - Allows visitors to request single or multi-day visits with daily time windows
  - Gate check-in/out is tracked per day in VMS_VisitDayLog
  Run against AnmolDristi / AnmolDristiDev before deploying code changes.
*/

IF COL_LENGTH('dbo.VMS_VisitLog', 'ScheduleType') IS NULL
BEGIN
    ALTER TABLE dbo.VMS_VisitLog ADD ScheduleType varchar(20) NULL;
    ALTER TABLE dbo.VMS_VisitLog ADD CONSTRAINT DF_VMS_VisitLog_ScheduleType DEFAULT ('SingleDay') FOR ScheduleType;
END
GO

IF COL_LENGTH('dbo.VMS_VisitLog', 'RequestedValidFrom') IS NULL
    ALTER TABLE dbo.VMS_VisitLog ADD RequestedValidFrom datetime NULL;
GO

IF COL_LENGTH('dbo.VMS_VisitLog', 'RequestedValidUpto') IS NULL
    ALTER TABLE dbo.VMS_VisitLog ADD RequestedValidUpto datetime NULL;
GO

IF COL_LENGTH('dbo.VMS_VisitLog', 'DailyTimeFrom') IS NULL
    ALTER TABLE dbo.VMS_VisitLog ADD DailyTimeFrom time(7) NULL;
GO

IF COL_LENGTH('dbo.VMS_VisitLog', 'DailyTimeTo') IS NULL
    ALTER TABLE dbo.VMS_VisitLog ADD DailyTimeTo time(7) NULL;
GO

IF COL_LENGTH('dbo.VMS_VisitLog', 'ApprovedValidFrom') IS NULL
    ALTER TABLE dbo.VMS_VisitLog ADD ApprovedValidFrom datetime NULL;
GO

IF COL_LENGTH('dbo.VMS_VisitLog', 'ApprovedValidUpto') IS NULL
    ALTER TABLE dbo.VMS_VisitLog ADD ApprovedValidUpto datetime NULL;
GO

IF OBJECT_ID('dbo.VMS_VisitDayLog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.VMS_VisitDayLog (
        DayLogID int IDENTITY(1,1) NOT NULL,
        LogID int NOT NULL,
        VisitDate date NOT NULL,
        DayValidFrom datetime NOT NULL,
        DayValidUpto datetime NOT NULL,
        VisitStatus varchar(20) NULL CONSTRAINT DF_VMS_VisitDayLog_VisitStatus DEFAULT ('Scheduled'),
        ActualInTime datetime NULL,
        ActualOutTime datetime NULL,
        IsDayApproved bit NOT NULL CONSTRAINT DF_VMS_VisitDayLog_IsDayApproved DEFAULT (0),
        CheckedOutBy varchar(50) NULL,
        CONSTRAINT PK_VMS_VisitDayLog PRIMARY KEY CLUSTERED (DayLogID ASC),
        CONSTRAINT UQ_VMS_VisitDayLog_LogDate UNIQUE (LogID, VisitDate)
    );

    CREATE INDEX IX_VMS_VisitDayLog_VisitDate_Status ON dbo.VMS_VisitDayLog (VisitDate, VisitStatus);
END
GO
