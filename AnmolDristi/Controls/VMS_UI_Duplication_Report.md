# VMS shared UI — duplication reduction

Maintainability only. Approval, attendance, registration, and gate workflows are unchanged. Visit Window cells still render `BuildHostScheduleDisplayHtml` / `BuildVisitorDayScheduleHtml`. Hours confirm() copy is unchanged.

## Before

| Area | Copies |
|---|---|
| Visit Window + badge CSS | 6 pages (My Visitors, My Visits, Host Quick Login, Admin Pending, Receptionist Logs, Digital Pass) ≈ 15–25 lines each |
| Hours JS (`formatClock`, `formatDayViolationBlock`, classify, current-time lead-in) | 3 pages (Registration, My Visitors, Host Quick Login) ≈ 90 lines each |
| Employee confirmation card markup + CSS | Registration only, but mixed into the page (~40 lines markup + 16 CSS) |
| Status badge class switching | My Visits ItemDataBound, My Visitors Eval ternary, Digital Pass switch, Receptionist modal label |
| BindScheduleCell / BindHostScheduleCell | 4 code-behinds with the same FindControl + helper call |

Approximate duplicated surface: **~450 lines** of CSS/JS/markup.

## After

| Shared asset | Lines (approx) | Replaces |
|---|---|---|
| `css/vms-ui.css` | ~250 | 6 inline style blocks |
| `Scripts/VMS_UI.js` | ~210 | 3 inline JS copies |
| 5 `.ascx` controls | mount points | Literals / ad-hoc badges / employee card |
| `VmsUiStatus` | one CSS map | per-page class strings |
| `VisitWindowControl.BindHostFromRow` | one binder | 4 `BindScheduleCell` copies |

Page-local CSS kept only for layout unique to that screen (visit cards, camera box, filter panels).

## What did not change

- `ApproveVisitSchedule`, DayLog uniqueness, Gate Scanner, attendance history, after-hours audit
- Confirm() gating on registration and host/admin/reception approve
- Visit Window columns (Date, Status, Entry, Exit) and helper-generated text
- I Work Here lookup WebMethod and hidden-field contract (`hfInternalEmpConfirmed`)

## Visual unification (intentional)

- “Not approved” day status uses orange (`badge-not-approved`) instead of rejected red
- Digital Pass “NOT APPROVED FOR TODAY” uses the not-approved orange treatment
- Pending amber hex aligned to `#F39C12`
