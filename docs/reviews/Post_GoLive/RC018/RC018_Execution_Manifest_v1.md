# RC-018 Execution Manifest v1

**Release:** RC-018 UAT → LIVE database synchronization  
**Target:** `AnmolDristi` on `115.112.186.71`  
**Authority:** `AnmolDristiDev` and `docs/reviews/Post_GoLive/RC018/`  
**Status:** Packages generated. **Not executed on LIVE.**  
**Date:** 2026-09-25  
**Rule:** Do not edit scripts 016–029 during execution. Re-hash if any file changes (`RC018_SHA256_Manifest.md`).

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

Leave times blank until the run.

| Script | Start | End | Result | Evidence | Rollback needed |
| --- | --- | --- | --- | --- | --- |
| 016 | | | | | |
| 016b | | | | | |
| 017 | | | | | |
| 017b | | | | | |
| 018 | | | | | |
| 018b | | | | | |
| 019 | | | | | |
| 019b | | | | | |
| 020 | | | | | |
| 020b | | | | | |
| 021 | | | | | |
| 021b | | | | | |
| 022 | | | | | |
| 022b | | | | | |
| 023 | | | | | |
| 023b | | | | | |
| 024 | | | | | |
| 024b | | | | | |
| 025 | | | | | |
| 025b | | | | | |
| 026 | | | | | |
| 026b | | | | | |
| 027 | | | | | |
| 027b | | | | | |
| 028 | | | | | |
| 028b | | | | | |
| 029 | | | | | |
| 029b | | | | | |

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

Checksums for all 42 files are in `RC018_SHA256_Manifest.md`. Compare them on the deployment workstation before the first script.
