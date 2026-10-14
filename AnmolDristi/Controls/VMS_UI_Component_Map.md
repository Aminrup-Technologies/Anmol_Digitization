# VMS shared UI — component usage map

Pages consume controls and `css/vms-ui.css` + `Scripts/VMS_UI.js`. Business rules stay in existing helpers/services.

| Control | Purpose | Used on |
|---|---|---|
| `VisitWindowControl.ascx` | Day schedule / Visit Window table | Registration (via helper HTML), My Visits, My Visitors, Host Quick Login, Receptionist Logs, Admin Pending, Digital Pass |
| `HoursWarningControl.ascx` | Working-hours notice (ARIA live) | Registration |
| `EmployeeCardControl.ascx` | I Work Here HR confirmation card | Registration |
| `StatusBadgeControl.ascx` | Standard statuses | My Visits, My Visitors (history), Receptionist modal, Digital Pass |
| `ApprovalSummaryControl.ascx` | Approval recap (summary + visit days) | Host Quick Login (post-action), Receptionist modal |

Shared scripts (`VMS_UI.js`):

| Function | Consumers |
|---|---|
| `formatClock` / `formatDayViolationBlock` / `classifyPlantHourWindow` | Registration, My Visitors, Host Quick Login (via notice builders) |
| `buildVisitorHoursNotice` | Registration |
| `buildHostHoursWarning` | My Visitors, Host Quick Login |
| `setHoursWarning` | Registration live region |
| `statusClass` | available for client rendering |

Shared CSS (`vms-ui.css`): badge colors, visit-window table, hours-warning panel, employee card, focus outlines, mobile table wrapping.

## Status colors

| Status | Class | Color |
|---|---|---|
| Pending / Scheduled | `badge-pending` | Amber `#F39C12` |
| Approved | `badge-approved` | Green `#1ABB9C` |
| Checked-In | `badge-checked-in` | Blue `#1565c0` |
| Checked-Out | `badge-checked-out` | Gray `#78909c` |
| Rejected | `badge-rejected` | Red `#E74C3C` |
| Not approved | `badge-not-approved` | Orange `#E67E22` |
