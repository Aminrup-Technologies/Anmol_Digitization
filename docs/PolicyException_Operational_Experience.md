# Policy Exception – Operational Experience (PR #70)

UI/UX productivity improvements only. No Policy Resolution, Claim Processing, Stored Procedure, History, Audit, Integrity, or Search-logic changes.

## Features delivered

| # | Feature | Implementation |
|---|---------|----------------|
| 1 | Policy Status | Derived from `IsActive` + `EffectiveFrom`/`EffectiveTo`; coloured badges in Grid, View, History |
| 2 | Effective Period | Visual banner (Currently Effective / Starts / Expired / Inactive) |
| 3 | Override Indicators | Per-field Inherited / Overridden from PolicyJson property presence |
| 4 | History UX | Collapse/Expand timeline, search versions, action filters, Newest First (client-side, read-only) |
| 5 | Quick Search | Employee autocomplete (MST_UserMaster via existing lookup source) + last 10 recent searches (localStorage) |
| 6 | Expiry Alerts | ⚠ within 30 days on grid + view banner |
| 7 | Summary Panel | Employee, Status, Priority, Effective/Expiry, Last Updated/By, Version, History count |
| 8 | Visual polish | Cards, spacing, timeline colours, responsive toolbar |
| 9 | Admin safety | Confirm before Deactivate and before Update |
| 10 | Read-only mode | READ ONLY badge/banner, grey panel, Save hidden; History/Compare/Export remain |

## Files changed

- `AnmolDristi/ManagePolicyException.aspx` (+ CSS/JS)
- `AnmolDristi/ManagePolicyException.aspx.cs`
- `AnmolDristi/ManagePolicyException.aspx.designer.cs`
- `AnmolDristi/BLL/PolicyExceptionService.cs` — `SuggestEmployees` (read-only)
- `AnmolDristi/DAL/PolicyExceptionDataAccess.cs` — `SuggestEmployees` against `MST_UserMaster` (no SP/DDL)

## Status derivation rules

1. **Inactive** — `IsActive = 0`
2. **Future** — `IsActive = 1` and `EffectiveFrom > Today`
3. **Expired** — `IsActive = 1` and `EffectiveTo < Today`
4. **Active** — everything else

## Override rule

Property present in PolicyJson (deserialized HasValue) → **Overridden**; otherwise **Inherited**. No resolved-policy comparison.

## Regression checklist

- [ ] Search returns same rows for same filters (logic unchanged)
- [ ] Create / Update / Activate / Deactivate still work
- [ ] View shows summary, period, status, overrides
- [ ] History timeline / grid / compare / export unchanged functionally
- [ ] Deactivate confirm shows claim-impact message
- [ ] Update confirm mentions immutable history version
- [ ] Read-only View: no editable controls; Compare/Export available
- [ ] Autocomplete suggests employees; recent searches persist in browser
- [ ] Expiry alert only when active and EffectiveTo within 30 days
- [ ] Policy Resolution / Claim Processing untouched

## Performance observations

- Status/period/override markup is O(rows) string formatting on bind — negligible for admin grids.
- Autocomplete: `TOP 10` LIKE on `MST_UserMaster`; minLength 1; client merges recent localStorage.
- History filters/sort are pure client-side DOM — no extra postbacks.
- No SP/index/schema changes; no impact on resolution path.

## Out of scope

Policy Resolution, Claim Processing, SP/DDL, History immutability logic, Search criteria behaviour.
