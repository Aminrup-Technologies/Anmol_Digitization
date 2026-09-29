# RC-018 Execution Closeout — 2026-09-27

**Release:** RC-018 UAT → LIVE database synchronization  
**Environment:** `AnmolDristi`  
**Authority:** `RC018_Execution_Manifest_v1.md`, `RC018_Master_Runbook.md`, `RC018_SHA256_Manifest.md`  
**Recommendation:** RC-018 CLOSED  

Certified script bytes under `Database/Scripts/016`–`029` (apply, `b`, and `c`) were not edited for this closeout. SHA-256 values in `RC018_SHA256_Manifest.md` were not regenerated.

---

## Executive summary

Production execution of RC-018 on `AnmolDristi` finished in the window 2026-09-25 through 2026-09-27. All 14 packages (016 through 029) are PASS. The final validator, `029b_RC018_P4_T14_TRN_qcinspector_Validate.sql`, reported OVERALL PASS.

No execution blocker remains. No rollback was required. The `c` scripts stay the rollback references and were not changed.

Three CAB decisions stay in force: the RMF330 exclusion, the PolicyException financial gate at 0, and the AHS1530 hold on ExceptionId 90002.

---

## Execution timeline

Start clocks below are RunTags taken from backup object names (`YYYYMMDD_HHMMSS`). A separate end clock was not captured. Packages without a named backup are placed by the required script order.

| When | What |
| --- | --- |
| 2026-09-25 | Package date. CAB gates recorded in the execution manifest. Window opens. |
| 2026-09-26, before 09:32:32 | 016 FrequencyTypes PASS. 017 MST_Calendar PASS. Backup RunTags for these two apply packages were not in the execution inventory. |
| 2026-09-26 09:32:32–09:33:06 | 018 dry-run STOP. RunTag `20260926_093232`. LiveCnt 28. Apply not run. No backup. Superseded by the apply below. |
| 2026-09-26 09:44:42 | 018 MST_EmployeeTolerance PASS. RunTag `20260926_094442`. |
| 2026-09-26, after 09:44:42 and before 09:56:44 | 019 MST_Plant_Lines PASS. Verify only. No backup. |
| 2026-09-26 09:56:44 | 020 TRN_RM_CLASS_1 PASS. RunTag `20260926_095644`. RMF330 exclusion preserved. |
| 2026-09-26 10:09:46 | 021 PolicyException PASS. RunTag `20260926_100946`. Financial gate remained 0. |
| 2026-09-27 18:26:05 | 022 ApprovalRequestStatusHistory PASS. RunTag `20260927_182605`. |
| 2026-09-27, after 18:26:05 and before 18:36:08 | 023 SalesApprovalRequestDetail PASS. Verify only. No backup. |
| 2026-09-27 18:36:08 | 024 SalesDailyActivity PASS. RunTag `20260927_183608`. |
| 2026-09-27 18:38:05 | 025 SalesDailyExpense PASS. RunTag `20260927_183805`. |
| 2026-09-27 18:45:11 | 026 VMS_VisitAttendanceHistory PASS. RunTag `20260927_184511`. |
| 2026-09-27, after 18:45:11 | 027, 028, and 029 PASS. Verify only. No backup. 029b OVERALL PASS. Window closed. |

---

## Table completion matrix

| Package | Table | Result | Validator |
| --- | --- | --- | --- |
| 016 | FrequencyTypes | PASS | 016b OVERALL PASS |
| 017 | MST_Calendar | PASS | 017b OVERALL PASS |
| 018 | MST_EmployeeTolerance | PASS | 018b OVERALL PASS |
| 019 | MST_Plant_Lines | PASS | 019b OVERALL PASS |
| 020 | TRN_RM_CLASS_1 | PASS | 020b OVERALL PASS |
| 021 | PolicyException | PASS | 021b OVERALL PASS |
| 022 | ApprovalRequestStatusHistory | PASS | 022b OVERALL PASS |
| 023 | SalesApprovalRequestDetail | PASS | 023b OVERALL PASS |
| 024 | SalesDailyActivity | PASS | 024b OVERALL PASS |
| 025 | SalesDailyExpense | PASS | 025b OVERALL PASS |
| 026 | VMS_VisitAttendanceHistory | PASS | 026b OVERALL PASS |
| 027 | CMS_RawConsumptionLogs | PASS | 027b OVERALL PASS |
| 028 | TRN_qc_packet_weight_hdr | PASS | 028b OVERALL PASS |
| 029 | TRN_qcinspector | PASS | 029b OVERALL PASS |

14/14 PASS. Final validator: 029b OVERALL PASS.

---

## Backup inventory

These objects were created on `AnmolDristi` during apply. Rollback, if ever required, uses the matching `c` script and that object name. No rollback ran.

| Package | RunTag | Backup table |
| --- | --- | --- |
| 018 | `20260926_094442` | `dbo.RC018_Backup_MST_EmployeeTolerance_20260926_094442` |
| 020 | `20260926_095644` | `dbo.RC018_Backup_TRN_RM_CLASS_1_20260926_095644` |
| 021 | `20260926_100946` | `dbo.RC018_Backup_PolicyException_20260926_100946` |
| 022 | `20260927_182605` | `dbo.RC018_Backup_ApprovalRequestStatusHistory_20260927_182605` |
| 024 | `20260927_183608` | `dbo.RC018_Backup_SalesDailyActivity_20260927_183608` |
| 025 | `20260927_183805` | `dbo.RC018_Backup_SalesDailyExpense_20260927_183805` |
| 026 | `20260927_184511` | `dbo.RC018_Backup_VMS_VisitAttendanceHistory_20260927_184511` |

016 and 017 are apply packages, so a backup is created when `@DryRun = 0`. Their object names were not in the execution inventory. This closeout does not invent a RunTag for them.

The earlier 018 dry-run (`20260926_093232`) created no backup.

---

## Verification-only packages

These packages intentionally created no backup. Alignment gaps were 0, so backup and DDL were skipped.

| Package | Table |
| --- | --- |
| 019 | MST_Plant_Lines |
| 023 | SalesApprovalRequestDetail |
| 027 | CMS_RawConsumptionLogs |
| 028 | TRN_qc_packet_weight_hdr |
| 029 | TRN_qcinspector |

023c, 027c, 028c, and 029c perform no DML. They do not reload a review backup over LIVE.

---

## Governance decisions preserved

| Decision | Where | What remains in force |
| --- | --- | --- |
| RMF330 accepted divergence | 020 | `RMFID <> 'RMF330'` stayed hard-coded. LIVE operational measurements for RMF330 were not updated. |
| PolicyException financial gate | 021 | `@EnableFinancialOverrides = 0`. ExceptionId 4 (inactive fixture) is the default insert. ExceptionIds 1 (AHS1562) and 2 (AHS628) stayed out. |
| AHS1530 governance | 021 ExceptionId 90002 | Held while the financial switch is 0. The monthly KM cap change 160 → 3 was not applied. |

These are follow-up CAB items only if an owner later signs a change. They are not open execution blockers, and they are not a failed deployment.

Certified files still default to `@DryRun = 1`. Session changes used during the window were not written back into the scripts.

---

## Final recommendation

RC-018 CLOSED.

No further SQL execution is required for this release. Do not regenerate hashes unless a script file actually changes. Do not treat a later financial-override approval as part of this execution package.

---

## 2026-09-29 — 020 TRN_RM_CLASS_1 production reconciliation

This entry records the later `020` / `020b` run. It does not replace the 2026-09-26 row above.

| Field | Value |
| --- | --- |
| Execution date | 2026-09-29 |
| RunTag | `20260929_100340` |
| DryRun | 0 |
| Completion | 2026-09-29T10:03:40.0959562+05:30 |
| LIVE | 510 |
| UAT | 330 |
| LIVE >= UAT | YES |
| Missing UAT keys on LIVE | 0 |
| LIVE-only | 180 |
| Actionable shared drift | 0 |
| Accepted RMF330 | 1 |
| Shared RMFID Id mismatch | 0 |
| Backup | `dbo.RC018_Backup_TRN_RM_CLASS_1_20260929_100340` |

Accepted production divergence:

RMF330 | LIVE present | shared attributes differ from UAT | ACCEPTED | excluded from MERGE

Safeguards confirmed by execution output:

- LIVE-only rows are ignored and not deleted.
- Material_Name is not in the UPDATE list.
- RMF330 is not in the MERGE source.

Mutation result: no missing keys, no actionable shared drift, no INSERT required, no UPDATE required. MERGE skipped after backup.

No missing keys and no shared drift. MERGE skipped after backup.

`@DryRun = 0`, so this was an actual production run. The MERGE was skipped because the execution found zero missing UAT keys and zero actionable shared drift. No INSERT or UPDATE statement executed. The backup named above was created. This entry does not record a restore or a test of that backup.

### Validation

Completion: 2026-09-29T10:02:45.5300578+05:30

CHECK UAT reachable | PASS

CHECK LIVE count >= UAT count | LIVE=510 UAT=330 | PASS

CHECK Missing UAT keys in LIVE | 0 | PASS

CHECK Duplicate business keys (RMFID) | 0 | PASS

CHECK Material_Name exists | PASS

CHECK PK | Columns=RMFID | PASS

CHECK Identity still on Id | PASS

CHECK Shared RMFID Id values unchanged | 0 | PASS

CHECK Actionable shared drift (excludes RMF330) | 0 | PASS

CHECK RMF330 accepted production divergence | LIVE=present DriftVsUat=1 | ACCEPTED

CHECK Triggers (expect 0) | 0 | PASS

CHECK DBCC CHECKCONSTRAINTS | PASS

Overall: PASS

### Disposition

RC-018 P1-T05 operational validation: PASS

RC-018 P1-T05 production reconciliation: PASS / no mutation required

RESOLVED: P1-T05 implementation, production safety gates, identity/PK safeguards, Material_Name protection, LIVE-only row retention, RMF330 MERGE exclusion, this production reconciliation, and this validation.

ACCEPTED: RMF330 production divergence. Existing EXCEPT/NULL comparison semantics. Backup transaction boundary, as previously reviewed.

The 2026-09-27 closeout above stays closed. This section does not reopen it.
