/*
  VMS_AuditEvent regression checklist (run after migration).

  Operational tables must still exist and are not replaced by the event stream.
*/

SELECT 'VMS_VisitLog' AS TableName, CASE WHEN OBJECT_ID('dbo.VMS_VisitLog', 'U') IS NULL THEN 'MISSING' ELSE 'OK' END AS Status
UNION ALL
SELECT 'VMS_VisitDayLog', CASE WHEN OBJECT_ID('dbo.VMS_VisitDayLog', 'U') IS NULL THEN 'MISSING' ELSE 'OK' END
UNION ALL
SELECT 'VMS_VisitAttendanceHistory', CASE WHEN OBJECT_ID('dbo.VMS_VisitAttendanceHistory', 'U') IS NULL THEN 'MISSING' ELSE 'OK' END
UNION ALL
SELECT 'VMS_AfterHoursApprovalAudit', CASE WHEN OBJECT_ID('dbo.VMS_AfterHoursApprovalAudit', 'U') IS NULL THEN 'MISSING' ELSE 'OK' END
UNION ALL
SELECT 'VMS_AuditEvent', CASE WHEN OBJECT_ID('dbo.VMS_AuditEvent', 'U') IS NULL THEN 'MISSING' ELSE 'OK' END
UNION ALL
SELECT 'vw_VMS_VisitAuditTimeline', CASE WHEN OBJECT_ID('dbo.vw_VMS_VisitAuditTimeline', 'V') IS NULL THEN 'MISSING' ELSE 'OK' END;

/* Timeline shape for a visit (replace @LogID). */
DECLARE @LogID int = 0;

SELECT EventTime, DisplayLabel, EventType, ActorRole, ActorID, DayLogID, Metadata
FROM dbo.vw_VMS_VisitAuditTimeline
WHERE LogID = @LogID
ORDER BY EventTime ASC, EventID ASC;

/*
  Manual workflow checks (application):
  1. Register a visitor → Registered (+ OTPVerified; + EmployeeConfirmed for I Work Here).
  2. Host approve in-hours → Approved + NotificationSent. AfterHoursApprovalAudit unchanged (no extra row).
  3. Host approve after-hours → Approved + NotificationSent + AfterHoursApproved AND AfterHoursApprovalAudit row.
  4. Host reject → Rejected. No pass notification.
  5. Gate check-in / check-out → CheckedIn / CheckedOut AND VisitAttendanceHistory still written.
  6. Same-day re-entry with open history → AutoClosed then CheckedIn; attendance unique open index still holds.
  7. Admin inline in/out correction → GateOverride. VisitLog/DayLog times still updated. No CheckInDay event pair.
  8. Pages still do not INSERT into VMS_AuditEvent.
*/
