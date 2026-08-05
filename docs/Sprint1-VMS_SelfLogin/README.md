# Sprint 1 — `VMS_SelfLogin` Modernization Gate

## Process

```
Architecture Review
        │
        ▼
Analysis (RF-xx)          ← CURRENT GATE
        │
        ▼
Implementation (IMP-xx)   ← blocked until RF pack approved
        │
        ▼
Review by ChatGPT
        │
        ▼
Regression Checklist
        │
        ▼
Approve
        │
        ▼
Commit
```

## Scope

| In scope | Out of scope |
|----------|----------------|
| `VMS_SelfLogin.aspx` | Other VMS pages (except Master touch for visitor isolation) |
| `VMS_SelfLogin.aspx.cs` | ASP.NET Core / MVC conversion |
| `Dristi.Master` / `.cs` only where required for visitor auto-logout isolation (RF-01, RF-02) | Schema changes |
| Config for SMS credentials (RF-03) | Changing OTP length or expiry |
| | Implementing “today vs next working day” workflow (design-only RF-12) |

## Hard constraints (non-negotiable)

- OTP remains **exactly 6 numeric digits**.
- OTP expiry remains **10 minutes**.
- Visitor workflow (QR → OTP → MyVisits / Registration) must not change.
- Session variable **names** must not change: `VerifiedMobile`, `VerifiedEmail`, `OTPAttempts`.
- Public URLs must not change.
- No database schema changes.
- No breaking changes to live VMS.

## Documents in this folder

| File | Purpose | Gate |
|------|---------|------|
| `GapAnalysis.md` | RF-xx catalogue | Analysis |
| `ImplementationBacklog.md` | IMP-xx tickets mapped to RF-xx | Analysis → Implementation |
| `RegressionChecklist.md` | Manual regression tests | Post-IMP / pre-Approve |
| `ImplementationNotes.md` | Filled per IMP during coding | Implementation |

## Gate status

| Gate | Status |
|------|--------|
| Architecture Review | Complete (prior turn) |
| Analysis (RF-xx) | **Ready for ChatGPT / stakeholder review** |
| Implementation (IMP-xx) | Not started |
| Review by ChatGPT | Pending |
| Regression Checklist | Draft ready; execute after IMP |
| Approve | Pending |
| Commit (implementation) | Pending approval |
