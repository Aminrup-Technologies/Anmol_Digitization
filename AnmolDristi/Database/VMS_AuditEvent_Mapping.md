# VMS_AuditEvent mapping

Central timeline for visit lifecycle events. **Does not replace** `VMS_VisitLog`, `VMS_VisitDayLog`, `VMS_VisitAttendanceHistory`, or `VMS_AfterHoursApprovalAudit`.

Publisher: `AuditEventService.Publish`. Pages do not insert into `VMS_AuditEvent`.

`Metadata` is optional tiny JSON (source, channel, field). Business columns stay on the operational tables.

## Existing writes → events

| Workflow | Existing write (unchanged) | Event | Publisher call site |
|---|---|---|---|
| Registration | `INSERT VMS_VisitLog` + DayLogs | `Registered` | `VmsVisitorRepository.CreateVisitorVisit` (same tx) |
| OTP (self-login) | `VMS_OTPTracking.IsVerified` (no LogID yet) | `OTPVerified` | Same registration tx when session mobile was verified |
| I Work Here confirm | Form flags `hfInternalEmpConfirmed` (no LogID at lookup) | `EmployeeConfirmed` | Same registration tx when confirmed |
| Host / Admin / Reception approval | `ExecuteApprovalLifecycle` VisitLog + DayLog | `Approved` | After guarded approval update succeeds (same tx) |
| After-hours approval | `INSERT VMS_AfterHoursApprovalAudit` | `AfterHoursApproved` | `TryWriteAfterHoursApprovalAudit` when a row is written |
| Pass email / SMS | `INSERT NotificationQueue` (`VMS_PASS` / `VMS_PASS_SMS`) | `NotificationSent` | `VmsHelper.QueueVisitorPassNotification` |
| Host reject | VisitLog `Rejected` + DayLog `Cancelled` | `Rejected` | `RejectVisitor` after parent update |
| Gate / Host / Reception check-in | DayLog + VisitLog + `VMS_VisitAttendanceHistory` open row | `CheckedIn` | `VmsVisitScheduleHelper.CheckInDay` |
| Check-in auto-close of open cycle | `UPDATE VMS_VisitAttendanceHistory` set `CheckOutTime` then new open row | `AutoClosed` then `CheckedIn` | `WriteCheckInHistory` via `CheckInDay` |
| Legacy parent-only check-in | VisitLog only (no DayLog today) | `CheckedIn` | Host / Receptionist / Gate legacy paths |
| Check-out | DayLog + VisitLog + close open attendance row | `CheckedOut` | `VmsVisitScheduleHelper.CheckOutDay` |
| Legacy parent-only check-out | VisitLog only | `CheckedOut` | Host / Receptionist / Gate legacy paths |
| Admin inline in/out time | VisitLog (+ DayLog) remarks; **not** CheckInDay/CheckOutDay | `GateOverride` | `AdminCorrectInTime` / `AdminCorrectOutTime` |

## Event order at registration

When both hints apply, insert order (EventID) is:

1. `OTPVerified`
2. `EmployeeConfirmed`
3. `Registered`

OTP is bound to the visit once `LogID` exists. Pre-registration OTP is session-only until submit.

## Event order at approval

1. `Approved`
2. `NotificationSent` (if email and/or SMS queued)
3. `AfterHoursApproved` (only if `VMS_AfterHoursApprovalAudit` inserted)

## Actor roles

`Host`, `Admin`, `Receptionist`, `Gate`, `Visitor`, `System`.

`AutoClosed` uses `System`. Attendance `Actor` / guard id is stored in `ActorID`.
