# Submitted travel baseline — UAT execution

**Record created:** 2026-10-15

**Environment:** `AnmolDristiDev` on `MUMANMOAPP01\SQLEXPRESS`

**Reference claims:** Request **135** (positive deduction) and Request **134** (zero deduction)

**Constraint:** Documentation of evidence already observed. No application code, SQL object, claim, permission, or release-baseline change is part of this record.

**Overall verdict: HOLD.** Approver-screen proof passed. Approved Claims runtime remains unverified. A waiver of that runtime check has been requested and is not CAB-approved. CAB, backup, and rollback gates for this package are open. No bug was closed and nothing was deployed.

This file is the deduction-specific UAT record. It does not amend `RC015G_FSOT_UAT_Execution.md` or any RC-017 artifact.

---

## Purpose and scope

Informational kilometre deduction is the difference between the original submitted travel amount and the current travel snapshot on the latest detail:

`max(0, SubmittedClaimAmount − Snapshot_CalculatedClaimAmount)`

That figure is displayed. It is not subtracted again from `FinalApprovedAmount`.

Latest detail ownership is highest `DetailVersion`, then highest `ApprovalRequestDetailId`.

In scope for this record:

- Positive-deduction runtime on the approver screen
- Zero-deduction runtime on request 134, left unchanged
- Source review of the eight query arms
- Approved Claims runtime limitation
- Release governance still open

Out of scope: deployment, permission changes, and any further transition of request 135.

---

## Environment and build

| Item | Evidence |
| --- | --- |
| Database | `AnmolDristiDev` |
| Server | `MUMANMOAPP01\SQLEXPRESS` |
| Reported build | Visual Studio Rebuild All: 1 succeeded, 0 failed, 0 skipped. Existing warnings remained. |
| Assembly | `AnmolDristi.dll`, timestamp `2026-10-15 03:24:39`, size `3,145,728` bytes |
| Release baselines preserved | `3779d185` and `tada-v1.1-postgolive-fsot-2026` were not moved |

The assembly timestamp and size were read from the local `bin` copy. That read does not by itself prove which IIS process had loaded the assembly. The approver screen later showed the new breakdown wording, which is the runtime signal that the rebuilt display code was in use for that page.

---

## Identity sequence

Switch User remained the authorized path. Nested impersonation was not used. Each claim action below was taken only after the impersonation banner matched the intended employee.

| Stage | Effective identity | Original admin | Role in the test |
| --- | --- | --- | --- |
| Sign-in | `AHO445` Kaushik Kumar Majhi | — | Admin session allowed to switch |
| Employee submit | `AHS1260` Mankamuthaka Eswaraiah | `AHO445` | Submitted May 2026 activity 58328 |
| L1 forward | `AHS1396` Kadiyala Suresh Babu | `AHO445` | Approved and forwarded request 135 |
| Auditor adjustment and recheck | `Audit` Megha Paramanick | `AHO445` | Reduced kilometres; later reopened the approver summary |

`AHS1396` was not granted `UI.SO_TaDa_ViewApproved.View`. No role or permission row was changed.

---

## Runtime results

### Positive deduction — PASS

Observed on Claims Approval while impersonating `Audit`. Request 135 was not approved, returned, rejected, or marked Ready for Payment during or after this proof.

| Field | Observed |
| --- | --- |
| Request | `#135` |
| Detail | `#3860`, `DetailVersion` 1, detail status Approved |
| Activity | `#58328`, 8 May 2026 |
| Header | Pending, current approver `Audit`, level 2 |
| Submitted baseline | 12.23 km / ₹45.86 |
| Current snapshot | 10.00 km / ₹37.50 |
| Informational deduction | ₹8.36 / 2.23 km |
| `FinalApprovedAmount` | ₹37.50 |
| History | `#1009`: `Audit` reduced 12.23 km to 10.00 km. Reason text: “UAT positive deduction: reduce 8 May 2026 travel from 12.23 km to 10.00 km”, with the Auditor name appended by the application |

Claims Approval summary:

| Label | Displayed |
| --- | --- |
| Original claimed | ₹45.86 / 12.23 km |
| Total deductions | (−) ₹8.36 / (−) 2.23 km |
| Net approved | ₹37.50 / 10.00 km |

Breakdown row: 08 May 2026, “Submitted travel ₹45.86 to ₹37.50 (12.23 km to 10.00 km)”, reason “Current travel snapshot is below the submitted travel amount”, amount ₹8.36.

The payable figure on that screen equals `FinalApprovedAmount`. The ₹8.36 deduction was shown beside it and was not subtracted a second time.

### Zero deduction — PASS

Request 134 was the earlier baseline capture. It was read again after the positive-deduction test and was not modified.

| Field | Observed |
| --- | --- |
| Request | `#134` |
| Detail | `#3859`, `DetailVersion` 1, detail status Pending |
| Activity | `#58332`, 8 November 2026 |
| Header | Pending, current approver `AHS1396`, level 1 |
| Submitted baseline | 11.53 km / ₹43.24 |
| Current snapshot | 11.53 km / ₹43.24 |
| Positive deduction | None. Expected difference is 0 |
| `FinalApprovedAmount` | ₹43.24 |

### My Claims while Pending — PASS

Observed on `MyClaimsView_v2` while impersonating `AHS1260`, with `AHO445` as the original user. The header was still Pending. The summary showed original claimed ₹37.50 / 10.00 km and did not show the deduction summary line. Opening the breakdown for 8 May 2026 showed “Submitted travel ₹45.86 to ₹37.50 (12.23 km to 10.00 km)” and deduction ₹8.36.

Returned-state visibility was not exercised. Legacy `MyClaimsView.aspx` was not opened.

---

## Source review — eight query arms

This section is source inspection. It is not a claim that all eight arms were executed in the browser.

Each arm selects the latest detail by highest `DetailVersion`, then highest `ApprovalRequestDetailId`. The amount is `SubmittedClaimAmount − ISNULL(Snapshot_CalculatedClaimAmount, 0)`, and the row is included only when `SubmittedClaimAmount` is not null and is greater than the snapshot. `FinalApprovedAmount` is not reduced by that amount inside these queries. The approver screen binds its net payable from the header payable figure, which matched ₹37.50 at runtime.

| # | Page | Arm | Source location | Runtime |
| --- | --- | --- | --- | --- |
| 1 | `ClaimsApproval_v2.aspx.cs` | Summary | about line 545 | **PASS** — screen showed ₹8.36 and net ₹37.50 |
| 2 | `ClaimsApproval_v2.aspx.cs` | Breakdown | about lines 3511 and 3519 | **PASS** — “Submitted travel ₹45.86 to ₹37.50 (12.23 km to 10.00 km)” |
| 3 | `MyClaimsView_v2.aspx.cs` | Summary | about line 651 | **PASS** for Pending visibility. Original claimed showed ₹37.50 / 10.00 km and the deduction summary line was absent |
| 4 | `MyClaimsView_v2.aspx.cs` | Breakdown | about lines 885 and 893 | **PASS** — “Submitted travel ₹45.86 to ₹37.50 (12.23 km to 10.00 km)”, ₹8.36 |
| 5 | `MyClaimsView.aspx.cs` | Summary | about line 455 | **NOT TESTED** |
| 6 | `MyClaimsView.aspx.cs` | Breakdown | about lines 611 and 619 | **NOT TESTED** |
| 7 | `ApprovedClaimsView.aspx.cs` | Summary | about line 365 | **BLOCKED** — page combination below |
| 8 | `ApprovedClaimsView.aspx.cs` | Breakdown | about lines 531 and 539 | **BLOCKED** — page combination below |

`MyClaimsView.aspx.cs` still has an older approved-total query that sums detail versions without the latest-detail filter. That query was not changed for this deduction fix and was not runtime-tested here.

---

## Additional test cases

| Case | Result | Basis |
| --- | --- | --- |
| Missing submitted-amount baseline (`SubmittedClaimAmount` null) | **NOT TESTED** | Source excludes the row when the submitted amount is null. No screen was opened on a null-baseline claim. Historical rows were not backfilled. |
| Null distance baseline with a submitted amount present | **NOT TESTED** | Source sets deducted kilometres to 0 when `SubmittedDistanceKM` is null and can still show an amount row. No such row was exercised. |
| Latest-version ownership across more than one detail version | **NOT TESTED** | Requests 134 and 135 each have a single detail, version 1. The version-then-id predicate was reviewed in source only. |
| L1 return, employee correction, and resubmit | **NOT TESTED** | Not executed. A resubmit stores a new baseline from the activity row, so it is the wrong path for preserving the original submitted amount on the latest detail. |
| Pending or Returned summary visibility on My Claims | **PASS** for Pending on `MyClaimsView_v2`. **NOT TESTED** for Returned and for legacy `MyClaimsView.aspx` |
| Approved Claims summary, breakdown, and payable display | **BLOCKED** | Runtime not verified. Waiver requested; not CAB-approved. See the next section. |
| Fixed travel allowance | **NOT TESTED** | No policy row with a fixed travel allowance was used. |

---

## Approved Claims — BLOCKED

`ApprovedClaimsView.aspx` requires `UI.SO_TaDa_ViewApproved.View` (`PermissionId` 31). On `AnmolDristiDev` that permission is granted to SuperAdmin (role 1) and External Auditor (role 11). It is not granted to ASM.

`dbo.usp_GetApprovedClaimsByUser` lists a request only when the signed-in user’s own approver row is Approved, Completed, Processed, or ReadyForPayment.

| Identity | Page permission | Approver row on request 135 | Can the deduction be opened there? |
| --- | --- | --- | --- |
| `AHS1396` | No. `UserRole` has no row for this employee | L1 status Approved | No. Direct navigation returned Access denied for `UI.SO_TaDa_ViewApproved.View` |
| `Audit` | Yes. External Auditor | Auditor status Pending | No. The page opened. A May 2026 search for `AHS1260` did not list request 135 |
| `AHO445` | Yes. SuperAdmin and External Auditor | Not an approver on request 135 | No |

No other detail on this database had `SubmittedClaimAmount` greater than its snapshot. No permission was added and request 135 was not moved to Completed or Ready for Payment to force the row onto the page.

### Follow-up search — 2026-10-15

A later read-only search on `AnmolDristiDev` / `MUMANMOAPP01\SQLEXPRESS` again found only one latest detail with `SubmittedClaimAmount` greater than the snapshot: request `#135`, detail `#3860`, version 1. Submitted baseline 12.23 km / ₹45.86. Snapshot 10.00 km / ₹37.50. Header Pending with `Audit` at level 2. Payable ₹37.50.

| Identity | Result of that search |
| --- | --- |
| `AHS1396` | Approver row Approved. No role, so the page permission is absent |
| `Audit` | External Auditor, so the page permission is present. Approver row still Pending, so `usp_GetApprovedClaimsByUser` does not list the request |
| `AHO445` | Not an approver on request 135 |

The application was on the login page. No identity and no Switch User banner were available. Approved Claims was not opened. Request `#134` and activity `#58332` were not in the positive-deduction result and were not changed. No claim transition and no permission change were performed.

Approved Claims runtime acceptance remains unverified. The user has elected to pursue a waiver of that runtime test and to rely on the observed Claims Approval and My Claims evidence. That waiver is not CAB-approved. It does not change this result to PASS.

---

## Workflow state left in place

| Request | Header | Current approver | Payable | Touched by this filing |
| --- | --- | --- | --- | --- |
| 135 | Pending, level 2 | `Audit` | ₹37.50 | No |
| 134 | Pending, level 1 | `AHS1396` | ₹43.24 | No |

Activity 58332 remains the November activity submitted on request 134. Activity 58328 remains the May activity on request 135.

---

## Governance

| Gate | Status | Notes |
| --- | --- | --- |
| Approver summary, breakdown, and payable display | **PASS** | Claims Approval, request 135, impersonating `Audit` |
| My Claims Pending summary and breakdown | **PASS** | `MyClaimsView_v2`, request 135, impersonating `AHS1260`. Returned state and legacy `MyClaimsView.aspx` remain **NOT TESTED** |
| Baseline and snapshot consistency | **PASS** | Submitted values unchanged; snapshot is the reduced travel amount; payable equals the snapshot total |
| Approved Claims summary, breakdown, and payable display | **BLOCKED** | Runtime unverified. Waiver requested and pending package-specific CAB approval. Not a pass |
| UAT record filed | **PASS** | This file. Read back after it was written |
| Bug / issue record | **BLOCKED** | No application issue was registered. See tracking note below |
| Package-specific CAB approval | **BLOCKED** | Checklist sign-off on 2026-10-14 covers `usp_MarkRequestReadyForPayment`, not this deduction package. The Approved Claims waiver is part of that open approval |
| Backup evidence | **BLOCKED** | Checklist production-database backup, IIS copy path, and live DLL identity lines are blank |
| Live procedure script-out | **BLOCKED** | The checklist script-out list does not include `dbo.usp_SubmitSalesDailyActivities`. No script-out of that live body was found for this package |
| Rollback readiness | **BLOCKED** | Checklist recovery is the pre-deploy IIS copy plus those script-outs. Those artifacts were not found. `032b` is not a substitute |
| Deployment | **NOT TESTED** | Not started |
| Bug closure | **NOT TESTED** | Approved Claims acceptance and the governance gates are still open |

Release checklist: `docs/releases/TA_DA_GoLive_2026/TA_DA_Deployment_Checklist.md`.

Routine rollback, if a later deploy fails, is the checklist sequence: restore the pre-deploy IIS copy of `AnmolDristi.dll` and the published files, recycle the application pool, and reapply the scripted-out procedure definitions taken before that deploy. Do not restore claim tables unless Finance authorizes it.

`Database/Scripts/032b_Rollback_SubmittedClaimBaseline.sql` is not that rollback. It drops `SubmittedDistanceKM` and `SubmittedClaimAmount`. Its own header states that a populated value cannot be reconstructed from kilometre history or a rate. `@Confirm` and `@AllowDropPopulated` default to 0. Do not execute it to undo this package.

`Database/Scripts/032_Add_SubmittedClaimBaseline.sql` adds those columns when they are missing and does not backfill. It was not executed as part of this UAT filing.

### Issue tracking

Repository UAT evidence for this product is an RC-numbered or purpose-named Markdown file under `docs/reviews/Post_GoLive/`, with execution packs under `pvt-evidence/` when a change was deployed. No GitHub or other issue was opened for this deduction. Third-party `ISSUE_TEMPLATE` files under `Resources/` are not the application tracker. No bug id is assigned here.

---

## Decision

**HOLD.**

Do not deploy this deduction package and do not close a bug for it. Approved Claims runtime is unverified. A waiver of that check has been requested and is pending package-specific CAB approval. HOLD also remains until that approval, a verified production database backup, a pre-deployment IIS copy with path and DLL identity, a script-out of the live procedures including `dbo.usp_SubmitSalesDailyActivities`, and a rollback proven from those artifacts are all present. `032b_Rollback_SubmittedClaimBaseline.sql` is not that rollback.
