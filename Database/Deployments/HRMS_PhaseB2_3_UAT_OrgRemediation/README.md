# HRMS Phase B2.3 — Controlled UAT Org-Column Remediation

**One-time UAT data-contract remediation. Not an application runtime subsystem.**

| Item | Value |
| --- | --- |
| Authorized database | `AnmolDristiDev` only |
| Live (`AnmolDristi`) | **Forbidden** — do not run these scripts on Live |
| Baseline | `Aug26_AILWorks_AmimnrupSupport` / merge `5ff3a3f` |
| Approved mappings | **Exactly 115** column-level INTERNAL_ID_DRIFT remaps |
| Designation_ID | Out of scope |
| Fixtures | `PHA9B2E_EX1`, `PHA9B2E_MGR`, `PHA9B2E_NW1`, `PHA9B2E_V7` — do not modify |
| Zero-target mappings | 5 rows — do not modify (see below) |
| Plant `3277` | Not a UserMaster conversion |

This package remediates historical UAT `MST_UserMaster` values that store master **internal `Id`** where the contract requires the master **ERP/business key**.

It does **not** change application code, stored procedures, HRMS import flow, or Live.

---

## Scripts

| Script | Mode | Purpose |
| --- | --- | --- |
| `000_CreateAuditTable.sql` | DDL (UAT only) | Minimal durable audit table |
| `A_Preflight.sql` | Read-only | Regenerate candidates; STOP if count/distribution ≠ approved |
| `B_ControlledApply.sql` | Transactional apply | Audit → 115 guarded column updates → validate → **COMMIT** |
| `C_PostApplyVerification.sql` | Read-only | Independent post-commit classification |
| `D_Rollback.sql` | Prepared only | Invert one `RemediationRunId`; **do not run after a successful apply** |

Execution order: `000` → `A` → (stop unless PASS) → `B` → `C`. Keep `D` unused unless an authorized rollback is requested.

---

## Approved distribution

| Column | Count |
| --- | ---: |
| BranchId | 17 |
| DepartmentId | 18 |
| GradeId | 6 |
| PlantId | 18 |
| RegionId | 18 |
| DivisionId | 15 |
| CategoryId | 6 |
| SubDepartmentId | 17 |
| **TOTAL** | **115** |

## Excluded zero-targets (leave unchanged)

| Employee | Column | Old → ERP |
| --- | --- | --- |
| 0001 | DivisionId | 4 → 0 |
| 0006 | DivisionId | 4 → 0 |
| 0034 | BranchId | 19 → 0 |
| 0034 | DivisionId | 4 → 0 |
| 0034 | SubDepartmentId | 110 → 0 |

---

## Concurrency

`READ COMMITTED` (database SNAPSHOT/RCSI remain OFF).

Apply transaction takes `UPDLOCK, HOLDLOCK, ROWLOCK` on the distinct candidate `MST_UserMaster` rows only, then every `UPDATE` repeats `WHERE column = OldValue`.

Any `@@ROWCOUNT` mismatch rolls back the entire transaction.

---

## Audit object

`dbo.AUDIT_UserMasterOrgRemediation_B23`

One-time migration artifact. Not referenced by application code. Do not create on Live.

---

## Rollback

`D_Rollback.sql` restores `OldValue` only where the current column still equals the audited `NewValue`, scoped to one `RemediationRunId`. Expected restore count = 115. Not executed as part of a successful B2.3 apply.

---

## UAT execution record (AnmolDristiDev)

| Item | Value |
| --- | --- |
| Status | COMMITTED |
| RemediationRunId | `B23-APPLY-2026-09-02T10:53:36` |
| AppliedAt | 2026-09-02 10:53:36.393259 |
| Audit rows | 115 `APPLIED` |
| Column updates | 115 (17/18/6/18/18/15/6/17) |
| Remaining approved historical non-zero drift | 0 |
| Fixture drift instances | 32 (unchanged) |
| Zero-target exclusions | 5 (unchanged) |
| Designation ERP-code drift | 0 |
| Rollback executed | **No** |

To prepare a rollback if later authorized, set `@RemediationRunId` in `D_Rollback.sql` to `B23-APPLY-2026-09-02T10:53:36`.
