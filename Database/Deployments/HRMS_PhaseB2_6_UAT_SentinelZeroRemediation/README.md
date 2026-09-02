# HRMS Phase B2.6 — Controlled UAT Sentinel-Zero Historical Remediation

**One-time UAT data-contract remediation. Not an application runtime subsystem.**

| Item | Value |
| --- | --- |
| Authorized database | `AnmolDristiDev` only |
| Live (`AnmolDristi`) | **Forbidden** |
| Baseline | `Aug26_AILWorks_AmimnrupSupport` / merge `5ff3a3f` |
| Approved mappings | **Exactly 5** column-level SENTINEL_UNASSIGNED remaps to ERP `0` |
| B2.3 mappings | Out of scope — do not modify or re-audit |
| Designation_ID | Out of scope |
| Fixtures | `PHA9B2E_EX1`, `PHA9B2E_MGR`, `PHA9B2E_NW1`, `PHA9B2E_V7` — do not modify |
| Other zero-target records | Out of scope |
| Plant `3277` | Not a UserMaster conversion |
| Application / UI / stored procedures | Do not change |

This package remediates five historical UAT `MST_UserMaster` values that still store master **internal `Id`** whose authoritative ERP/business key is the established sentinel **`0`**.

It does **not** convert `0` to NULL. Phase B2.4/B2.5 established `0` as the source/ERP representation.

---

## Objective

```text
Correct exactly five approved historical MST_UserMaster organizational values
from legacy internal master IDs
to their established ERP/business sentinel value 0.
```

## Scope — exact mappings

| EmployeeCode | Column | Required Old Value | Required New Value |
| ------------ | ------ | -----------------: | -----------------: |
| 0001 | DivisionId | 4 | 0 |
| 0006 | DivisionId | 4 | 0 |
| 0034 | BranchId | 19 | 0 |
| 0034 | DivisionId | 4 | 0 |
| 0034 | SubDepartmentId | 110 | 0 |

Expected distribution: BranchId 1, DivisionId 3, SubDepartmentId 1. Total = 5.

## Explicit exclusions

* Live database access or modification
* Plant 3277 / `M2668.PlantId`
* `Designation_ID`
* Fixtures listed above
* The 115 B2.3 mappings and `dbo.AUDIT_UserMasterOrgRemediation_B23`
* Any additional zero-target or INTERNAL_ID_DRIFT row
* Application, ASPX, C#, stored procedure, or UI changes

---

## Scripts

| Script | Mode | Purpose |
| --- | --- | --- |
| `000_CreateAuditTable.sql` | DDL (UAT only) | Create `dbo.AUDIT_UserMasterOrgSentinelZero_B26` if missing |
| `A_Preflight.sql` | Read-only | Regenerate candidates from current data; STOP unless exactly the five mappings |
| `B_ControlledApply.sql` | Transactional apply | PENDING audit → 5 guarded column updates → validate → mark APPLIED → **COMMIT** |
| `C_PostApplyVerification.sql` | Read-only | Independent post-commit verification |
| `D_Rollback.sql` | Prepared only | Invert one explicit `RemediationRunId`; **do not run after a successful apply** |

Execution order:

```text
A_Preflight.sql
        ↓ (PASS only)
000_CreateAuditTable.sql
        ↓
B_ControlledApply.sql
        ↓
C_PostApplyVerification.sql
```

Keep `D_Rollback.sql` unused unless a later authorized rollback is requested with an explicit run id.

---

## Preconditions

* Regenerated candidate total = 5
* Exact old values as in the scope table
* `MST_Division.Id = 4` → `Division_ID = 0` (unique)
* `MST_Branch.Id = 19` → `Branch_ID = 0` (unique)
* `MST_SubDepartment.Id = 110` → `SubDepartment_ID = 0` (unique)
* Ambiguous targets = 0, NULL targets = 0, non-zero targets = 0
* Already remediated (current = 0) = 0
* B2.3 overlap = 0
* Fixture / designation / plant candidates = 0

If preflight is FAIL: **do not apply**.

---

## Safety controls

* Database gate: abort if not `AnmolDristiDev`; abort if `AnmolDristi`
* `SET XACT_ABORT ON`
* `BEGIN TRANSACTION` / `COMMIT` only after all assertions
* Any assertion failure → `ROLLBACK` entire transaction
* Candidate regeneration from current data (not a hardcoded UPDATE list alone)
* `UPDLOCK, HOLDLOCK, ROWLOCK` on the three candidate `MST_UserMaster` rows
* Five column-level `UPDATE`s, each requiring employee identity **and** old-value predicate
* Exact `@@ROWCOUNT` = 1 per statement; distribution 1 / 3 / 1; total 5
* In-transaction contract check: all five current values = 0
* Audit lifecycle PENDING → APPLIED inside the same transaction
* `NewValue = 0` is required (do not reuse the B2.3 non-zero-target assertion)
* Independent `C_PostApplyVerification.sql`
* Guarded rollback that restores only while current still equals 0

---

## Audit object

`dbo.AUDIT_UserMasterOrgSentinelZero_B26`

One-time migration artifact. Not referenced by application code. Do not create on Live. Do not reuse the B2.3 audit table.

Check constraints:

* `OldValue <> NewValue`
* `NewValue = 0`
* `TargetERPValue = 0`

---

## Known limitations (do not fix in this phase)

1. **UI sentinel collision.** Edit Employee binds `ListItem("Select","0")` in addition to the empty master row whose ERP key is `0`. Stored `0` can display as Select. This already affects the broader ERP-zero population.
2. **External `U_SITECODE` dependency unknown.** Login assigns `Session["U_SITECODE"]` from `SubDepartmentId`. No repository consumer was found. If employee `0034` later authenticates, the session string would be `"0"` instead of `"110"`.
3. **Branch ticket happy path not executed end-to-end** for employee `0034`. B2.5 showed TicketService throws on Branch ERP 19 and resolves placeholder Id 19 for ERP 0; no UAT ticket from a `BranchId = 0` requester was executed as a live create.

---

## Rollback

`D_Rollback.sql` restores `OldValue` only where the current column still equals the audited `NewValue` (`0`), scoped to **one explicit** `RemediationRunId`. The placeholder does **not** default to the latest run. Expected restore count = 5. Not executed as part of a successful B2.6 apply.

---

## UAT execution record (AnmolDristiDev)

| Item | Value |
| --- | --- |
| Status | **COMMITTED** |
| RemediationRunId | `B26-APPLY-2026-09-02T11:34:20` |
| AppliedAt | 2026-09-02 11:34:20.126880 |
| Audit object | `dbo.AUDIT_UserMasterOrgSentinelZero_B26` |
| Audit rows | 5 `APPLIED` |
| Column updates | 5 (Branch 1 / Division 3 / SubDepartment 1) |
| Remaining B2.6 zero-target drift | 0 |
| Remaining approved non-zero drift (B2.3 class) | 0 |
| Fixture drift instances | 32 (unchanged) |
| Designation ERP-code drift | 0 |
| `M2668.PlantId` | 3277 (unchanged) |
| Rollback executed | **No** |

To prepare a rollback if later authorized, set `@RemediationRunId` in `D_Rollback.sql` to `B26-APPLY-2026-09-02T11:34:20`. Do not execute rollback as part of a successful B2.6 apply.
