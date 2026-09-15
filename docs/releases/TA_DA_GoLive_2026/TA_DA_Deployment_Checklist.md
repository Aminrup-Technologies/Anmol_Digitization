# TA/DA Production Deployment Checklist

| Field | Value |
| --- | --- |
| Branch | `Aug26_AILWorks_AmimnrupSupport` |
| Tag | `tada-v1.0-go-live-2026` |
| Certification | `TA_DA_GoLive_Certification.md` (2026-09-15) |
| Smoke evidence | `TA_DA_Smoke_Test_Execution.md` — all scenarios PASS |
| Schema / data scripts | None. Procedure bodies only. |

Tick boxes during the production window. Do not push or publish from this packaging task.

---

## Pre-deployment

### Backup

- [ ] Full backup of the production SQL database (equivalent of `AnmolDristiDev`).
- [ ] Backup file / job name: `________________`
- [ ] Verify backup completed (size / job success).
- [ ] Script-out current live definitions (rollback source) for:

```text
dbo.usp_UpdateActivityKM
dbo.usp_ForwardApprovalRequest
dbo.usp_UpdateSalesDailyExpenseStatus
```

- [ ] Copy the live IIS site folder (at minimum `bin\AnmolDristi.dll` and published ASPX).
- [ ] IIS backup path: `________________`
- [ ] Live DLL timestamp / version: `________________`

### IIS readiness

- [ ] Application pool identified: `________________`
- [ ] Site bindings confirmed.
- [ ] Change window agreed; idle or stop the TA/DA site only if the window requires it.
- [ ] Do not deploy unrelated HRMS or Switch User packages in the same window unless separately signed off.

### SQL verification (before apply)

Confirm production is behind or equal to certified UAT. Certified UAT `modify_date` values:

| Object | UAT certified modify_date |
| --- | --- |
| `usp_SubmitSalesDailyActivities` | 2026-07-28 12:39:02 |
| `usp_ProcessApprovalAction` | 2026-08-01 10:21:05 |
| `usp_UpdateActivityKM` | 2026-09-14 13:51:00 |
| `usp_ForwardApprovalRequest` | 2026-09-15 12:03:54 |
| `usp_UpdateSalesDailyExpenseStatus` | 2026-09-15 12:03:55 |

### Branch verification

- [ ] Package is built from `Aug26_AILWorks_AmimnrupSupport` at tag `tada-v1.0-go-live-2026`.
- [ ] Release commit SHA: `________________`
- [ ] Confirm these 11 application/SQL paths are in the build:

| Path | RCs |
| --- | --- |
| `AnmolDristi/SalesMonthlyReview.aspx` | RC-008, RC-010 |
| `AnmolDristi/SalesMonthlyReview.aspx.cs` | RC-001, RC-007, RC-008, RC-010 |
| `AnmolDristi/ClaimsApproval_v2.aspx` | RC-009, RC-011, RC-013 |
| `AnmolDristi/ClaimsApproval_v2.aspx.cs` | RC-009, RC-011, RC-012, RC-013 |
| `AnmolDristi/ClaimsApproval_v2.aspx.designer.cs` | RC-011 controls |
| `AnmolDristi/MyClaimsView_v2.aspx` | RC-005, RC-006 |
| `AnmolDristi/MyClaimsView_v2.aspx.cs` | RC-003, RC-005, RC-006, RC-010 |
| `AnmolDristi/ApprovedClaimsView.aspx.cs` | RC-004 |
| `Database/Stored Procedures/Submission/usp_UpdateActivityKM.sql` | RC-008 |
| `Database/Stored Procedures/Approval/usp_ForwardApprovalRequest.sql` | RC-012A |
| `Database/Stored Procedures/Approval/usp_UpdateSalesDailyExpenseStatus.sql` | RC-012B |

- [ ] No extra schema scripts in the packet.
- [ ] `.cursor/` is not in the package.

---

## Deployment

### Publish application

- [ ] After **manual** push and change approval, publish / copy the application package to the IIS site.
- [ ] Confirm published output is newer than the IIS backup.
- [ ] Record publish timestamp: `________________`

### Deploy stored procedures

If production SPs are behind UAT, apply **only** these repository scripts (`CREATE OR ALTER PROCEDURE`; preserve signatures):

1. `Database/Stored Procedures/Submission/usp_UpdateActivityKM.sql`
2. `Database/Stored Procedures/Approval/usp_ForwardApprovalRequest.sql`
3. `Database/Stored Procedures/Approval/usp_UpdateSalesDailyExpenseStatus.sql`

Post-apply checks:

- [ ] `usp_UpdateActivityKM` contains `MAX(ISNULL(DetailVersion, 1))` and Returned KM gate.
- [ ] `usp_ForwardApprovalRequest` completeness uses latest version then latest detail id. Does **not** set header `Status='Approved'`.
- [ ] `usp_UpdateSalesDailyExpenseStatus` resolves `ActivityId → latest DetailVersion → latest ApprovalRequestDetailId`.
- [ ] No table, index, or data script executed.

| Object | Production modify_date |
| --- | --- |
| `usp_UpdateActivityKM` | |
| `usp_ForwardApprovalRequest` | |
| `usp_UpdateSalesDailyExpenseStatus` | |

---

## Post-deployment

### IIS recycle

- [ ] Recycle the application pool.
- [ ] Hit `SalesMonthlyReview.aspx`, `ClaimsApproval_v2.aspx`, `MyClaimsView_v2.aspx` (authenticated).
- [ ] Confirm no yellow-screen / compile error.

### Browser cache

- [ ] Instruct L1/Auditor testers to hard-refresh (Ctrl+F5) or clear cache for `ClaimsApproval_v2` and `SalesMonthlyReview`.
- [ ] Confirm Forward confirmation modal and auto-close scripts load (RC-011 / RC-013).

### Smoke verification

Repeat the scenarios in `TA_DA_Smoke_Test_Execution.md` on production after recycle:

- [ ] NEW submission
- [ ] Return
- [ ] KM edit
- [ ] RESUBMIT
- [ ] Latest-version (one row per day after RESUBMIT)
- [ ] Return notification
- [ ] Forward confirmation
- [ ] Auditor routing (header stays Pending)
- [ ] Modal auto-close (no F5)

---

## Rollback

Use only if production smoke fails or the site will not start.

1. Copy the IIS backup over the live site (`AnmolDristi.dll` and published files).
2. Recycle the application pool.
3. Run the three **pre-deploy** scripted-out procedure definitions (not the repository RC files).
4. Confirm `modify_date` reverted.
5. Do not restore tables or data (this release has no DML packet).

Exit criteria after rollback: L1 inbox loads; employee month grid loads; incident logged with SHA, backup path, and failed scenario.

---

## Sign-off

| Check | Owner | Time | Result |
| --- | --- | --- | --- |
| IIS backup taken | | | |
| DB backup taken | | | |
| App published | | | |
| Three SPs applied / verified | | | |
| IIS recycled | | | |
| Browser cache cleared | | | |
| Production smoke PASS | | | |
| Production approved / rolled back | | | |

Deployer: ________________  
Approver: ________________  
Date: ________________
