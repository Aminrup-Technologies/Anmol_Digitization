# RC-019 Foundation Gate

**Release:** RC-019  
**Status:** Foundation gate. This document authorizes no SQL execution.  
**Date:** 2026-09-27  
**Authority:** `docs/reviews/Post_GoLive/RC018/RC018_Execution_Closeout_20260927.md`, `RC018_Execution_Manifest_v1.md`, `RC018_SHA256_Manifest.md`

RC-018 is closed. Production execution on `AnmolDristi` is 14/14 PASS. RC-019 starts from that certified baseline.

---

## Baseline

| Item | Value |
| --- | --- |
| Git integration base | `7cd69853e937de9e6e2be710faf488cc01a47718` |
| Git integration branch | `Aug26_AILWorks_AmimnrupSupport` |
| Certified operational baseline | `9ab2e588590223a46c6ba362c669db50b446230c` |
| Certified baseline tag | `rc018-executed-production-20260927` |

RC-019 integrates through the repository default branch. RC-018 certification remains anchored to `9ab2e58`. This separation is intentional and avoids historical branch contamination while preserving the certified execution baseline.

### Branch verification (2026-09-27)

| Check | Result |
| --- | --- |
| `HEAD` | `9ab2e58` |
| Tag `rc018-executed-production-20260927` | Present. Peels to `9ab2e58`. |
| Current branch | `feature/rc017c-softdelete-face-refresh` |
| `release/rc019-foundation` | Not present. Not checked out. |

RC-019 source work integrates from the Git integration base on `Aug26_AILWorks_AmimnrupSupport`. It is not branched from the certified operational baseline `9ab2e58`. Creating that branch was not part of this gate.

```powershell
git switch -c release/rc019-foundation-default Aug26_AILWorks_AmimnrupSupport
```

---

## Frozen RC-018 artifacts

These files stay byte-for-byte as they are at the certified operational baseline `9ab2e58`. `RC018_SHA256_Manifest.md` stays as published. Do not regenerate it.

### Certified scripts (42)

| Package | Apply | Validate | Rollback |
| --- | --- | --- | --- |
| 016 FrequencyTypes | `016_RC018_P1_T01_FrequencyTypes_MERGE.sql` | `016b_RC018_P1_T01_FrequencyTypes_Validate.sql` | `016c_RC018_P1_T01_FrequencyTypes_Rollback.sql` |
| 017 MST_Calendar | `017_RC018_P1_T02_MST_Calendar_Patch.sql` | `017b_RC018_P1_T02_MST_Calendar_Validate.sql` | `017c_RC018_P1_T02_MST_Calendar_Rollback.sql` |
| 018 MST_EmployeeTolerance | `018_RC018_P1_T03_MST_EmployeeTolerance_Patch.sql` | `018b_RC018_P1_T03_MST_EmployeeTolerance_Validate.sql` | `018c_RC018_P1_T03_MST_EmployeeTolerance_Rollback.sql` |
| 019 MST_Plant_Lines | `019_RC018_P1_T04_MST_Plant_Lines_Patch.sql` | `019b_RC018_P1_T04_MST_Plant_Lines_Validate.sql` | `019c_RC018_P1_T04_MST_Plant_Lines_Rollback.sql` |
| 020 TRN_RM_CLASS_1 | `020_RC018_P1_T05_TRN_RM_CLASS_1_Reconcile.sql` | `020b_RC018_P1_T05_TRN_RM_CLASS_1_Validate.sql` | `020c_RC018_P1_T05_TRN_RM_CLASS_1_Rollback.sql` |
| 021 PolicyException | `021_RC018_P2_T06_PolicyException_Patch.sql` | `021b_RC018_P2_T06_PolicyException_Validate.sql` | `021c_RC018_P2_T06_PolicyException_Rollback.sql` |
| 022 ApprovalRequestStatusHistory | `022_RC018_P3_T07_ApprovalRequestStatusHistory_Patch.sql` | `022b_RC018_P3_T07_ApprovalRequestStatusHistory_Validate.sql` | `022c_RC018_P3_T07_ApprovalRequestStatusHistory_Rollback.sql` |
| 023 SalesApprovalRequestDetail | `023_RC018_P3_T08_SalesApprovalRequestDetail_Verify.sql` | `023b_RC018_P3_T08_SalesApprovalRequestDetail_Validate.sql` | `023c_RC018_P3_T08_SalesApprovalRequestDetail_Rollback.sql` |
| 024 SalesDailyActivity | `024_RC018_P3_T09_SalesDailyActivity_Patch.sql` | `024b_RC018_P3_T09_SalesDailyActivity_Validate.sql` | `024c_RC018_P3_T09_SalesDailyActivity_Rollback.sql` |
| 025 SalesDailyExpense | `025_RC018_P3_T10_SalesDailyExpense_Patch.sql` | `025b_RC018_P3_T10_SalesDailyExpense_Validate.sql` | `025c_RC018_P3_T10_SalesDailyExpense_Rollback.sql` |
| 026 VMS_VisitAttendanceHistory | `026_RC018_P3_T11_VMS_VisitAttendanceHistory_Patch.sql` | `026b_RC018_P3_T11_VMS_VisitAttendanceHistory_Validate.sql` | `026c_RC018_P3_T11_VMS_VisitAttendanceHistory_Rollback.sql` |
| 027 CMS_RawConsumptionLogs | `027_RC018_P4_T12_CMS_RawConsumptionLogs_Verify.sql` | `027b_RC018_P4_T12_CMS_RawConsumptionLogs_Validate.sql` | `027c_RC018_P4_T12_CMS_RawConsumptionLogs_Rollback.sql` |
| 028 TRN_qc_packet_weight_hdr | `028_RC018_P4_T13_TRN_qc_packet_weight_hdr_Verify.sql` | `028b_RC018_P4_T13_TRN_qc_packet_weight_hdr_Validate.sql` | `028c_RC018_P4_T13_TRN_qc_packet_weight_hdr_Rollback.sql` |
| 029 TRN_qcinspector | `029_RC018_P4_T14_TRN_qcinspector_Verify.sql` | `029b_RC018_P4_T14_TRN_qcinspector_Validate.sql` | `029c_RC018_P4_T14_TRN_qcinspector_Rollback.sql` |

All 42 paths are under `Database/Scripts/`.

### Hash manifest

`docs/reviews/Post_GoLive/RC018/RC018_SHA256_Manifest.md`

### Closed governance record

The `docs/reviews/Post_GoLive/RC018/` tree at the certified operational baseline `9ab2e58`, including:

- `RC018_Execution_Closeout_20260927.md`
- `RC018_Execution_Manifest_v1.md`
- `RC018_Master_Runbook.md`
- `RC018_SHA256_Manifest.md`

---

## Development constraints

1. New SQL begins at `Database/Scripts/030`.
2. Scripts `016`–`029` (apply, `b`, and `c`) stay aligned with `RC018_SHA256_Manifest.md`.
3. `RC018_SHA256_Manifest.md` stays unchanged.
4. Commit `9ab2e58` remains the certified operational baseline for the RC-018 closeout. It is not the Git integration base. Tag `rc018-executed-production-20260927` stays on that commit.
5. These CAB decisions stay in force until an owner signs a later change: the RMF330 exclusion, `@EnableFinancialOverrides = 0`, and the AHS1530 hold on ExceptionId 90002.
6. These paths stay local and unstaged. They are outside RC-019 source:
   - `AnmolDristi/Web.config`
   - `AnmolDristi/AnmolDristi.csproj.user`
   - `Database/Scripts/015_RC018_Phase0_Discovery.sql`
   - `Database/Scripts/015b_RC018_Phase0_CrossDiff.sql`
   - `Database/Scripts/015c_RC018_Phase0_KeysIndexes.sql`
7. The `015*` scripts are read-only RC-018 Phase 0 discovery. A later discovery uses a new script number from 030.

---

## Initial backlog

| ID | Item | State |
| --- | --- | --- |
| — | No RC-019 scope is approved | Placeholder |

Held CAB items remain on the RC-018 closeout. They are not RC-019 backlog entries until an owner signs a change.

---

## Local-only inventory

Recorded with this gate. Unstaged. Not part of RC-019.

| Path | State | Role |
| --- | --- | --- |
| `AnmolDristi/Web.config` | Modified | Environment. Active catalog is a local session choice. |
| `AnmolDristi/AnmolDristi.csproj.user` | Modified | IDE noise. `ProjectView` only. |
| `Database/Scripts/015_RC018_Phase0_Discovery.sql` | Untracked | RC-018 Phase 0 discovery. Read only. |
| `Database/Scripts/015b_RC018_Phase0_CrossDiff.sql` | Untracked | RC-018 Phase 0 cross-database diff. Read only. |
| `Database/Scripts/015c_RC018_Phase0_KeysIndexes.sql` | Untracked | RC-018 Phase 0 keys and indexes. Read only. |

---

## Evidence-availability notice (added by repository review 2026-10-15)

The "Constraints" and "Closed governance record" sections above reference RC-018 packaging that is **not present in this checkout**. Nothing above was changed; this notice records the verified repository state so those constraints are not read as satisfied at `HEAD`.

Verified by `git ls-tree -r --name-only` and `git show --name-status`:

| Referenced | Tag `rc018-executed-production-20260927` (`9ab2e588`) | `main` (`13e1694`) | `HEAD` (`52c1f32`) |
| --- | --- | --- | --- |
| `docs/reviews/Post_GoLive/RC018/` | 36 files, including `RC018_SHA256_Manifest.md`, `RC018_Execution_Manifest_v1.md`, `RC018_Master_Runbook.md`, `RC018_Phase0_Discovery_Report.md` | 35 files | 1 file (`RC018_Execution_Closeout_20260927.md`) |
| `Database/Scripts/016*` through `029*` | present | present | absent |
| `Database/Scripts/` total | 56 files | 42 files | 23 files |

Removed by commit `9fdeb78` (2026-09-28 22:29:29 +0530, "RC-017C: drop RC-018 packaging so the pull request is only the soft-delete face refresh.", 78 deleted paths: 42 under `Database/Scripts`, 36 under `docs/reviews/Post_GoLive/RC018`). That commit is a descendant of `9ab2e588` and an ancestor of `HEAD`. The same commit replaced `.gitattributes`, removing its `*.sql`, `*.config`, `*.xml`, and `*.md` line-ending entries.

Consequences: constraints 2 and 3 above cannot be checked against `HEAD`; constraint 4's certified baseline is not the tree at `HEAD`; and any constraint that depends on the RC-018 hash manifest is currently unverifiable from this checkout. Recovering the deleted artifacts is a release-owner decision, not a repository fix.

The two "local-only inventory" rows for `Database/Scripts/015_RC018_Phase0_Discovery.sql` and its `b`/`c` siblings are **correct as written**: those files were never tracked in any commit in this repository.

See `docs/INDEX.md` sections 8 and 9.
