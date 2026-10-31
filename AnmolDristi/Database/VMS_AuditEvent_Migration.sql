/*
  VMS unified audit event stream (enterprise timeline)

  Adds dbo.VMS_AuditEvent as a central append-only lifecycle timeline.
  Does NOT replace or alter:
    - VMS_VisitLog
    - VMS_VisitDayLog
    - VMS_VisitAttendanceHistory
    - VMS_AfterHoursApprovalAudit

  Those tables remain the operational / compliance stores.
  VMS_AuditEvent records that something happened (type, actor, time, optional JSON).
  It does not duplicate visitor, schedule, or attendance business columns.

  EventType is varchar so future types need no schema redesign.
  Run against AnmolDristi / AnmolDristiDev before deploying code that writes this table.
  Code paths guard IF OBJECT_ID so older databases continue to work.
*/

IF OBJECT_ID('dbo.VMS_AuditEvent', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.VMS_AuditEvent (
        EventID     int IDENTITY(1,1) NOT NULL,
        LogID       int NOT NULL,
        DayLogID    int NULL,
        EventType   varchar(50) NOT NULL,
        ActorRole   varchar(30) NULL,
        ActorID     varchar(50) NULL,
        EventTime   datetime NOT NULL
            CONSTRAINT DF_VMS_AuditEvent_EventTime DEFAULT (GETDATE()),
        Metadata    nvarchar(2000) NULL,
        CreatedOn   datetime NOT NULL
            CONSTRAINT DF_VMS_AuditEvent_CreatedOn DEFAULT (GETDATE()),
        CONSTRAINT PK_VMS_AuditEvent PRIMARY KEY CLUSTERED (EventID ASC)
    );

    CREATE INDEX IX_VMS_AuditEvent_LogID_EventTime
        ON dbo.VMS_AuditEvent (LogID, EventTime, EventID);
END
GO

IF OBJECT_ID('dbo.vw_VMS_VisitAuditTimeline', 'V') IS NOT NULL
    DROP VIEW dbo.vw_VMS_VisitAuditTimeline;
GO

CREATE VIEW dbo.vw_VMS_VisitAuditTimeline
AS
SELECT
    e.EventID,
    e.LogID,
    e.DayLogID,
    e.EventType,
    CASE e.EventType
        WHEN 'Registered'         THEN 'Registered'
        WHEN 'OTPVerified'        THEN 'OTP Verified'
        WHEN 'EmployeeConfirmed'  THEN 'Employee Confirmed'
        WHEN 'Approved'           THEN 'Approved'
        WHEN 'Rejected'           THEN 'Rejected'
        WHEN 'AfterHoursApproved' THEN 'After-Hours Approved'
        WHEN 'CheckedIn'          THEN 'Checked In'
        WHEN 'CheckedOut'         THEN 'Checked Out'
        WHEN 'AutoClosed'         THEN 'Auto-Closed'
        WHEN 'GateOverride'       THEN 'Gate Override'
        WHEN 'NotificationSent'   THEN 'Notification Sent'
        ELSE e.EventType
    END AS DisplayLabel,
    e.ActorRole,
    e.ActorID,
    e.EventTime,
    e.Metadata,
    e.CreatedOn
FROM dbo.VMS_AuditEvent e;
GO

/*
  Reusable timeline query (also implemented in VmsAuditEventRepository.GetTimeline):

    SELECT EventID, LogID, DayLogID, EventType, DisplayLabel,
           ActorRole, ActorID, EventTime, Metadata, CreatedOn
    FROM dbo.vw_VMS_VisitAuditTimeline
    WHERE LogID = @LogID
    ORDER BY EventTime ASC, EventID ASC;
*/
