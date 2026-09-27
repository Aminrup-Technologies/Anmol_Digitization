# RC-018 Execution Manifest v1

**Release:** RC-018 UAT → LIVE database synchronization  
**Target:** `AnmolDristi` on `115.112.186.71`  
**Authority:** `AnmolDristiDev` and `docs/reviews/Post_GoLive/RC018/`  
**Status:** LIVE execution complete on `AnmolDristi`. **14/14 PASS.** Closeout 2026-09-27.  
**Date:** 2026-09-25 (package). Execution window 2026-09-25 through 2026-09-27.  
**Rule:** Do not edit scripts 016–029. Certified package bytes and `RC018_SHA256_Manifest.md` stay unchanged. Re-hash only if a script file changes.

Every apply script defaults to `@DryRun = 1`. Set `@DryRun = 0` only after the CAB gates in section 3 and a successful dry run.

---

## 1. Script inventory

| Script | Table | Purpose | Risk | Expected production effect | @DryRun | Backup object | Validation | Rollback |
| --- | --- | --- | --- | --- | ---: | --- | --- | --- |
| 016 | FrequencyTypes | MERGE shared descriptions; retain `FrequencyFactor` | Low | No row change expected (6 = 6). Factor values stay 2,1,4,1,1,1 | 1 | `RC018_Backup_FrequencyTypes_<RunTag>` on apply | 016b | 016c |
| 017 | MST_Calendar | Add `FinancialWeek` if missing; fill only where NULL | Low | One nullable column. 7,671 rows kept. 13 LIVE columns kept | 1 | `RC018_Backup_MST_Calendar_<RunTag>` on apply | 017b | 017c |
| 018 | MST_EmployeeTolerance | Add `MaxKmModificationCount` and check; MERGE 3 seeds | Low | LIVE goes from 0 rows to 3. Identity insert of ToleranceId 1–3 | 1 | `RC018_Backup_MST_EmployeeTolerance_<RunTag>` before MERGE | 018b | 018c |
| 019 | MST_Plant_Lines | Verify. MERGE only if shared drift appears | Low | No-op expected. 37 rows and `IX_MST_Plant_Lines_Report` kept | 1 | `RC018_Backup_MST_Plant_Lines_<RunTag>` only if drift and `@DryRun = 0` | 019b | 019c |
| 020 | TRN_RM_CLASS_1 | Subset reconcile on `RMFID`. `RMF330` excluded | Medium | Missing keys = 0. 179 LIVE-only rows kept. `Material_Name` kept. `RMF330` not updated | 1 | `RC018_Backup_TRN_RM_CLASS_1_<RunTag>` on apply | 020b | 020c |
| 021 | PolicyException | Checks, index, MERGE. Financial rows gated | Medium | Default `@EnableFinancialOverrides = 0` inserts only ExceptionId 4 (inactive). Rows 1, 2, and 90002 stay out | 1 | `RC018_Backup_PolicyException_<RunTag>` before MERGE | 021b | 021c |
| 022 | ApprovalRequestStatusHistory | Add 5 nullable audit columns; FK only if no orphans | Medium | 4,403+ history rows not updated. `FK_ApprovalHistory_Detail` WITH CHECK only when orphans = 0 | 1 | `RC018_Backup_ApprovalRequestStatusHistory_<RunTag>` before DDL | 022b | 022c |
| 023 | SalesApprovalRequestDetail | Verify only | High | No DDL when gaps = 0. Identity, `DetailVersion`, and snapshots unchanged | 1 | `RC018_Backup_SalesApprovalRequestDetail_<RunTag>` only if a gap exists and `@DryRun = 0` | 023b | 023c |
| 024 | SalesDailyActivity | Recreate `CK_SalesDailyActivity_ClaimStatus` to allow `Returned` | High | No row updates. Draft, Submitted, Approved, Rejected remain. `Returned` becomes legal | 1 | `RC018_Backup_SalesDailyActivity_<RunTag>` before the constraint drop | 024b | 024c |
| 025 | SalesDailyExpense | Add 4 nullable approval columns | High | Existing rows, `ClaimStatus`, and receipt columns unchanged. New columns start NULL | 1 | `RC018_Backup_SalesDailyExpense_<RunTag>` before DDL | 025b | 025c |
| 026 | VMS_VisitAttendanceHistory | Add 2 indexes. Unique open-day index only if no duplicates | High | Rows unchanged. Unique index skipped and validation fails if two open rows share a `DayLogID` | 1 | `RC018_Backup_VMS_VisitAttendanceHistory_<RunTag>` only when an index will be created | 026b | 026c |
| 027 | CMS_RawConsumptionLogs | Verify only | Low | No DDL when gaps = 0. Unique dedup index kept. Logs not copied from UAT | 1 | `RC018_Backup_CMS_RawConsumptionLogs_<RunTag>` only if a gap exists and `@DryRun = 0` | 027b | 027c |
| 028 | TRN_qc_packet_weight_hdr | Verify only | Medium | No DDL when gaps = 0. `FormID` default `((21))` kept | 1 | `RC018_Backup_TRN_qc_packet_weight_hdr_<RunTag>` only if a gap exists and `@DryRun = 0` | 028b | 028c |
| 029 | TRN_qcinspector | Verify only | Medium | No DDL when gaps = 0. `ShiftDate` and Compliance/Report indexes kept | 1 | `RC018_Backup_TRN_qcinspector_<RunTag>` only if a gap exists and `@DryRun = 0` | 029b | 029c |

---

## 2. Execution order

Do not start a script until the previous script’s validation is PASS.

### Phase 1 — reference

1. 016 FrequencyTypes  
2. 017 MST_Calendar  
3. 018 MST_EmployeeTolerance  
4. 019 MST_Plant_Lines  
5. 020 TRN_RM_CLASS_1  

### Phase 2 — policy

6. 021 PolicyException  

### Phase 3 — transactions

7. 022 ApprovalRequestStatusHistory  
8. 023 SalesApprovalRequestDetail  
9. 024 SalesDailyActivity  
10. 025 SalesDailyExpense  
11. 026 VMS_VisitAttendanceHistory  

### Phase 4 — operational

12. 027 CMS_RawConsumptionLogs  
13. 028 TRN_qc_packet_weight_hdr  
14. 029 TRN_qcinspector  

For each number, run the apply script, then the matching `b` validation script, before the next number.

---

## 3. CAB business gates

These are not implied by `@DryRun = 0`.

| Gate | Where | Default behavior | Required approval before changing the default |
| --- | --- | --- | --- |
| RMF330 accepted production divergence | 020 | `RMFID <> 'RMF330'` is hard-coded. LIVE operational measurements stay | Do not remove the exclusion. Recorded as Accepted |
| PolicyException financial gate | 021 | `@EnableFinancialOverrides = 0` | Owner approval for AHS1562 (ExceptionId 1) and AHS628 (ExceptionId 2) before setting the switch to 1 |
| AHS1530 3 km confirmation | 021 ExceptionId 90002 | Held while the switch is 0 | Explicit confirmation that monthly KM cap 160 → 3 is intended |
| ExceptionId 4 historical row | 021 | Always eligible. Inactive, window 2000-01-01 to 2000-01-02 | Approve insertion of this inactive fixture. It does not change `ResolvePolicy` on 2026-09-25 |

`usp_GetResolvedPolicy` does not read `PolicyException`. Claim screens use `PolicyResolutionService.ResolvePolicy`, which overlays an active in-window exception after the matrix. ExceptionId 4 does not overlay. ExceptionIds 1, 2, and 90002 would, if the switch is later set to 1.

---

## 4. Pre-flight checklist

- [ ] Full production backup of `AnmolDristi` completed and restore tested, or restore point confirmed by the DBA. Script backups do not replace this.
- [ ] Connected database is `AnmolDristi`, not `AnmolDristiDev`.
- [ ] Every apply script still has `@DryRun = 1` at the first execution.
- [ ] 021 still has `@EnableFinancialOverrides = 0` unless the CAB rows above are signed.
- [ ] The login can `SELECT INTO` backup tables in `dbo`.
- [ ] Disk space covers the large gap backups if 027, 028, or 029 report gaps (`CMS_RawConsumptionLogs` is about 772,493 rows).
- [ ] Row-count floors still make sense immediately before apply:

| Table | LIVE floor |
| --- | ---: |
| FrequencyTypes | 6 |
| MST_Calendar | 7,671 |
| MST_EmployeeTolerance | 0 before 018; 3 after |
| MST_Plant_Lines | 37 |
| TRN_RM_CLASS_1 | 509 (LIVE ≥ UAT; equality not required) |
| PolicyException | 0 before 021; 1 after default gate (ExceptionId 4 only) |
| ApprovalRequestStatusHistory | 4,403 |
| SalesApprovalRequestDetail | 34,257 |
| SalesDailyActivity | 87,402 |
| SalesDailyExpense | 30,181 |
| VMS_VisitAttendanceHistory | 4 |
| CMS_RawConsumptionLogs | 772,493 |
| TRN_qc_packet_weight_hdr | 37,952 |
| TRN_qcinspector | 80,474 |

Counts may be higher if the application wrote rows after Phase 0. A count below the floor is a stop.

---

## 5. Execution matrix

Filled after LIVE execution on `AnmolDristi`. Where a backup name is known, Start is that RunTag clock (`YYYYMMDD_HHMMSS` from `GETDATE()` at batch start). A separate end clock was not captured.

016 and 017 are apply packages. Their backup object names were not in the execution inventory used for this closeout, so no RunTag is assigned to them here. 019, 023, 027, 028, and 029 intentionally created no backup.

| Script | Start | End | Result | Evidence | Rollback needed |
| --- | --- | --- | --- | --- | --- |
| 016 | 2026-09-26 | not recorded | PASS | AnmolDristi. 016b OVERALL PASS. Sequence: before the 018 dry-run at 09:32:32. Backup RunTag not in the execution inventory. | No |
| 016b | 2026-09-26 | not recorded | PASS | OVERALL PASS | No |
| 017 | 2026-09-26 | not recorded | PASS | AnmolDristi. 017b OVERALL PASS. Sequence: before the 018 dry-run at 09:32:32. Backup RunTag not in the execution inventory. | No |
| 017b | 2026-09-26 | not recorded | PASS | OVERALL PASS | No |
| 018 | 2026-09-26 09:44:42 | not recorded | PASS | RunTag 20260926_094442. Backup `dbo.RC018_Backup_MST_EmployeeTolerance_20260926_094442`. 018b OVERALL PASS. Prior dry-run STOP 2026-09-26 09:32:32–09:33:06, RunTag 20260926_093232, LiveCnt 28, INSERT preview 0, LIVE-only ids 4–28, ids 1–3 AHS1058 / AHS1092 / AHS1156, AHS1530 id 12, apply not run, no backup. That stop was superseded by this apply. | No |
| 018b | 2026-09-26 | not recorded | PASS | OVERALL PASS after apply RunTag 20260926_094442 | No |
| 019 | 2026-09-26 | not recorded | PASS | Verify only. No backup (intentional). 019b OVERALL PASS. Sequence: after 018 apply 09:44:42 and before 020 09:56:44. | No |
| 019b | 2026-09-26 | not recorded | PASS | OVERALL PASS | No |
| 020 | 2026-09-26 09:56:44 | not recorded | PASS | RunTag 20260926_095644. Backup `dbo.RC018_Backup_TRN_RM_CLASS_1_20260926_095644`. RMF330 exclusion preserved. 020b OVERALL PASS. | No |
| 020b | 2026-09-26 | not recorded | PASS | OVERALL PASS | No |
| 021 | 2026-09-26 10:09:46 | not recorded | PASS | RunTag 20260926_100946. Backup `dbo.RC018_Backup_PolicyException_20260926_100946`. `@EnableFinancialOverrides` remained 0. 021b OVERALL PASS. | No |
| 021b | 2026-09-26 | not recorded | PASS | OVERALL PASS under the default financial gate (ExceptionId 4 only) | No |
| 022 | 2026-09-27 18:26:05 | not recorded | PASS | RunTag 20260927_182605. Backup `dbo.RC018_Backup_ApprovalRequestStatusHistory_20260927_182605`. 022b OVERALL PASS. | No |
| 022b | 2026-09-27 | not recorded | PASS | OVERALL PASS | No |
| 023 | 2026-09-27 | not recorded | PASS | Verify only. No backup (intentional). 023b OVERALL PASS. Sequence: after 022 18:26:05 and before 024 18:36:08. | No |
| 023b | 2026-09-27 | not recorded | PASS | OVERALL PASS | No |
| 024 | 2026-09-27 18:36:08 | not recorded | PASS | RunTag 20260927_183608. Backup `dbo.RC018_Backup_SalesDailyActivity_20260927_183608`. 024b OVERALL PASS. | No |
| 024b | 2026-09-27 | not recorded | PASS | OVERALL PASS. Apply backup `dbo.RC018_Backup_SalesDailyActivity_20260927_183608`. | No |
| 025 | 2026-09-27 18:38:05 | not recorded | PASS | RunTag 20260927_183805. Backup `dbo.RC018_Backup_SalesDailyExpense_20260927_183805`. 025b OVERALL PASS. | No |
| 025b | 2026-09-27 | not recorded | PASS | OVERALL PASS. Apply backup `dbo.RC018_Backup_SalesDailyExpense_20260927_183805`. | No |
| 026 | 2026-09-27 18:45:11 | not recorded | PASS | RunTag 20260927_184511. Backup `dbo.RC018_Backup_VMS_VisitAttendanceHistory_20260927_184511`. 026b OVERALL PASS. | No |
| 026b | 2026-09-27 | not recorded | PASS | OVERALL PASS. Apply backup `dbo.RC018_Backup_VMS_VisitAttendanceHistory_20260927_184511`. | No |
| 027 | 2026-09-27 | not recorded | PASS | Verify only. No backup (intentional). Gaps = 0. 027b OVERALL PASS. Sequence: after 026 18:45:11. | No |
| 027b | 2026-09-27 | not recorded | PASS | OVERALL PASS | No |
| 028 | 2026-09-27 | not recorded | PASS | Verify only. No backup (intentional). Gaps = 0. 028b OVERALL PASS. | No |
| 028b | 2026-09-27 | not recorded | PASS | OVERALL PASS | No |
| 029 | 2026-09-27 | not recorded | PASS | Verify only. No backup (intentional). Gaps = 0. 029b OVERALL PASS. | No |
| 029b | 2026-09-27 | not recorded | PASS | OVERALL PASS. Final validator for RC-018. | No |

---

## 6. Stop conditions

Stop the deployment immediately. Do not start the next script.

- The current `b` validation is not OVERALL PASS.
- The apply script emits DDL that its package did not describe.
- 024 constraint drop or recreate fails, or any current `ClaimStatus` is outside Draft, Submitted, Approved, Rejected, and Returned.
- 022 finds non-null `ApprovalRequestDetailId` orphans. Do not create the FK `WITH NOCHECK`.
- 026 prints duplicate open `DayLogID` rows. Do not force `UQ_VMS_VisitAttendanceHistory_OpenDayLog`.
- Row count falls below the floor for that table.
- `ActivityId`, `ExpenseId`, `DetailVersion`, `FrequencyFactor`, `FormID`, or `ShiftDate` is altered.
- The session is not in `AnmolDristi`.

Record the stop in the execution matrix and use the matching `c` script only if that script’s own `@Confirm` rules allow it.

---

## 7. Post-deployment validation

Run after each apply script, and again as a closing sweep.

| Order | Script |
| --- | --- |
| 016 | `Database/Scripts/016b_RC018_P1_T01_FrequencyTypes_Validate.sql` |
| 017 | `Database/Scripts/017b_RC018_P1_T02_MST_Calendar_Validate.sql` |
| 018 | `Database/Scripts/018b_RC018_P1_T03_MST_EmployeeTolerance_Validate.sql` |
| 019 | `Database/Scripts/019b_RC018_P1_T04_MST_Plant_Lines_Validate.sql` |
| 020 | `Database/Scripts/020b_RC018_P1_T05_TRN_RM_CLASS_1_Validate.sql` |
| 021 | `Database/Scripts/021b_RC018_P2_T06_PolicyException_Validate.sql` |
| 022 | `Database/Scripts/022b_RC018_P3_T07_ApprovalRequestStatusHistory_Validate.sql` |
| 023 | `Database/Scripts/023b_RC018_P3_T08_SalesApprovalRequestDetail_Validate.sql` |
| 024 | `Database/Scripts/024b_RC018_P3_T09_SalesDailyActivity_Validate.sql` |
| 025 | `Database/Scripts/025b_RC018_P3_T10_SalesDailyExpense_Validate.sql` |
| 026 | `Database/Scripts/026b_RC018_P3_T11_VMS_VisitAttendanceHistory_Validate.sql` |
| 027 | `Database/Scripts/027b_RC018_P4_T12_CMS_RawConsumptionLogs_Validate.sql` |
| 028 | `Database/Scripts/028b_RC018_P4_T13_TRN_qc_packet_weight_hdr_Validate.sql` |
| 029 | `Database/Scripts/029b_RC018_P4_T14_TRN_qcinspector_Validate.sql` |

021b expects four policy rows only after financial overrides are enabled. With the default gate, expect ExceptionId 4 only, and do not treat a count of 4 as the success condition for the default run.

025b and 026b should have `@BackupTable` set to the name printed by the apply script when a backup was created.

---

## 8. Rollback matrix

Run only the rollback for the script that failed, and only after reading its header. Several rollbacks refuse to drop columns or restore checks when production has already written new values.

| Script | Rollback |
| --- | --- |
| 016 | `Database/Scripts/016c_RC018_P1_T01_FrequencyTypes_Rollback.sql` |
| 017 | `Database/Scripts/017c_RC018_P1_T02_MST_Calendar_Rollback.sql` |
| 018 | `Database/Scripts/018c_RC018_P1_T03_MST_EmployeeTolerance_Rollback.sql` |
| 019 | `Database/Scripts/019c_RC018_P1_T04_MST_Plant_Lines_Rollback.sql` |
| 020 | `Database/Scripts/020c_RC018_P1_T05_TRN_RM_CLASS_1_Rollback.sql` |
| 021 | `Database/Scripts/021c_RC018_P2_T06_PolicyException_Rollback.sql` |
| 022 | `Database/Scripts/022c_RC018_P3_T07_ApprovalRequestStatusHistory_Rollback.sql` |
| 023 | `Database/Scripts/023c_RC018_P3_T08_SalesApprovalRequestDetail_Rollback.sql` |
| 024 | `Database/Scripts/024c_RC018_P3_T09_SalesDailyActivity_Rollback.sql` |
| 025 | `Database/Scripts/025c_RC018_P3_T10_SalesDailyExpense_Rollback.sql` |
| 026 | `Database/Scripts/026c_RC018_P3_T11_VMS_VisitAttendanceHistory_Rollback.sql` |
| 027 | `Database/Scripts/027c_RC018_P4_T12_CMS_RawConsumptionLogs_Rollback.sql` |
| 028 | `Database/Scripts/028c_RC018_P4_T13_TRN_qc_packet_weight_hdr_Rollback.sql` |
| 029 | `Database/Scripts/029c_RC018_P4_T14_TRN_qcinspector_Rollback.sql` |

023c, 027c, 028c, and 029c perform no DML. They do not reload a review backup over LIVE.

---

## 9. SHA-256

Checksums for all 42 files are in `RC018_SHA256_Manifest.md`. Those hashes were not regenerated at closeout. Certified script bytes were not edited for this record.

---

# RC-018 Execution Complete

- Environment: `AnmolDristi`
- Execution window: 2026-09-25 → 2026-09-27
- Packages completed: 14/14
- Final validator: 029b OVERALL PASS
- No remaining execution blockers
- RMF330 accepted divergence preserved
- PolicyException financial gate preserved (`@EnableFinancialOverrides = 0`)
- AHS1530 governance preserved (ExceptionId 90002 held while the financial switch is 0)

No rollback was required. Rollback references remain the matching `c` scripts in section 8. Closeout narrative: `RC018_Execution_Closeout_20260927.md`.
