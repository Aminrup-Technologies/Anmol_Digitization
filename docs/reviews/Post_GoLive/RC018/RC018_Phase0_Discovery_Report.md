# RC-018 Phase 0 — Discovery Matrix (Read Only)

**Date:** 2026-09-24 (UTC)  
**UAT:** `AnmolDristiDev` @ `115.112.186.71` (`MUMANMOAPP01\SQLEXPRESS`)  
**LIVE:** `AnmolDristi` @ same instance  
**Status:** Discovery complete. **No DDL / DML applied.**  
**Evidence:** `docs/reviews/Post_GoLive/RC018/Phase0_*.txt`  
**Scripts:** `Database/Scripts/015*_RC018_Phase0_*.sql`

---

## Governance rules applied

| Rule | Result |
| --- | --- |
| Never recreate tables | Confirmed — all 14 tables exist on both |
| Never truncate LIVE | N/A (read-only) |
| Never overwrite transactional records | N/A (read-only) |
| LIVE-only columns | **Retain** — do not drop |
| UAT-only columns | Candidate **ADD** to LIVE (schema patches) |
| Data-type mismatch | **STOP** — none found among shared columns |
| Triggers | None on any of the 14 tables (both envs) |
| Computed columns | None on any of the 14 tables |

---

## Executive summary

| Phase | Tables needing work | Notes |
| --- | --- | --- |
| **1 Reference data** | `MST_EmployeeTolerance` (data+col), `MST_Calendar` (schema both ways), `FrequencyTypes` (LIVE-only col retain), `MST_Plant_Lines` (index only), `TRN_RM_CLASS_1` (**blocker** — LIVE has more rows; COUNT match impossible without delete) | |
| **2 Policy** | `PolicyException` — columns match; LIVE empty (0); UAT has 4 rows + index + 2 checks | |
| **3 Transaction schema** | `ApprovalRequestStatusHistory` (+5 cols, +1 FK), `SalesDailyExpense` (+4 cols), `SalesDailyActivity` (check constraint), `VMS_VisitAttendanceHistory` (indexes) | `SalesApprovalRequestDetail` schema **MATCH** |
| **4 Operational** | `TRN_qc_packet_weight_hdr` (LIVE-only `FormID` retain), `TRN_qcinspector` (LIVE-only `ShiftDate` retain + LIVE indexes), `CMS_RawConsumptionLogs` (LIVE unique index retain) | No UAT→LIVE log data migration |

---

## A. Row counts

| # | Table | UAT | LIVE | Phase intent | Count-match expectation |
| ---: | --- | ---: | ---: | --- | --- |
| 1 | FrequencyTypes | 6 | 6 | MERGE attrs | Likely already aligned |
| 2 | MST_Calendar | 7,671 | 7,671 | MERGE attrs | Dates already equal |
| 3 | MST_EmployeeTolerance | 3 | **0** | MERGE insert | After MERGE → 3 |
| 4 | MST_Plant_Lines | 37 | 37 | MERGE attrs | Likely aligned |
| 5 | TRN_RM_CLASS_1 | 330 | **509** | MERGE missing only | **LIVE will stay ≥509** — Phase 1 “counts must match” **cannot pass** without deleting LIVE rows (forbidden) |
| 6 | PolicyException | 4 | **0** | Insert missing | After insert → 4 (if no LIVE-specific) |
| 7 | ApprovalRequestStatusHistory | 707 | 4,403 | Schema only | Preserve LIVE |
| 8 | SalesApprovalRequestDetail | 3,023 | 34,257 | Schema only | Preserve LIVE |
| 9 | SalesDailyActivity | 81,171 | 87,402 | Schema only | Preserve LIVE |
| 10 | SalesDailyExpense | 2,377 | 30,181 | Schema only | Preserve LIVE |
| 11 | VMS_VisitAttendanceHistory | 6 | 4 | Schema only | Preserve LIVE |
| 12 | CMS_RawConsumptionLogs | 0 | 772,493 | Schema/index only | Preserve LIVE |
| 13 | TRN_qc_packet_weight_hdr | 3,797 | 37,952 | Schema only | Preserve LIVE |
| 14 | TRN_qcinspector | 62,926 | 80,474 | Schema only | Preserve LIVE |

---

## B. Object inventory matrix (per table)

### Legend — Action

- **Add** = bring UAT object onto LIVE  
- **Retain** = LIVE enrichment; do not drop  
- **None** = already aligned  
- **Review** = decide before Phase N  
- **STOP** = datatype conflict (none in this sweep)

---

### 1. FrequencyTypes — Risk: Low | Phase 1

| Object | UAT | LIVE | Action |
| --- | --- | --- | --- |
| Exists | Yes | Yes | None |
| PK | `FrequencyID` | `FrequencyID` | None (MATCH) |
| Identity | `FrequencyID` (1,1) curr=6 | same curr=6 | Preserve; never reseed |
| Columns | 2 | 3 | — |
| `FrequencyFactor` int NULL | No | Yes | **Retain** (LIVE) |
| FK / Check / Trigger | None | None | None |
| Indexes (non-PK) | None | None | None |
| Data (FrequencyID 1–6 descriptions) | Match | Match | MERGE update if attrs drift; Factor stays LIVE |

---

### 2. MST_Calendar — Risk: Low | Phase 1

| Object | UAT | LIVE | Action |
| --- | --- | --- | --- |
| PK | `CalendarDate` | `CalendarDate` | None |
| Identity | No | No | — |
| Shared columns (22) | Match | Match | MERGE attrs on shared cols only |
| `FinancialWeek` int NULL | Yes | No | **Add** to LIVE |
| LIVE enrichment cols (13) | No | Yes | **Retain all** (DayOfYear, FinancialMonthName, FinancialHalfYear, IsHoliday+DF(0), IsWorkingDay+DF(1), HolidayName, HolidayType, StateCode, YearMonth, YearMonthKey, IsShutdownDay+DF(0), ShutdownReason, Remarks) |
| Indexes | None extra | `IX_MST_Calendar_Date/Financial/Month/YearMonth` | **Retain** LIVE indexes |
| FK / Check / Trigger | None | None | None |

> **Note:** Do not drop LIVE holiday/working-day columns to “match” UAT.

---

### 3. MST_EmployeeTolerance — Risk: Low | Phase 1

| Object | UAT | LIVE | Action |
| --- | --- | --- | --- |
| PK | `ToleranceId` | `ToleranceId` | None |
| Identity | curr=3 | curr=1 | Preserve LIVE identity; INSERT with IDENTITY_INSERT if PK must match UAT |
| Shared columns (11) | Match | Match | — |
| `MaxKmModificationCount` int NOT NULL DF(1) | Yes | No | **Add** + default |
| Check `CK_..._MaxKmModificationCount` | Yes (≥1) | No | **Add** |
| Rows | 3 (AHS1562, M1367, AHS1530) | **0** | **MERGE insert** |
| Trigger | None | None | None |

---

### 4. MST_Plant_Lines — Risk: Low | Phase 1

| Object | UAT | LIVE | Action |
| --- | --- | --- | --- |
| Columns (9) | Match | Match | None |
| PK | `line_id` | `line_id` | None |
| Identity | `id` (not PK) curr=39 | same | Preserve |
| FK | → `MST_PlantDetails` | same | None |
| Index `IX_MST_Plant_Lines_Report` | No | Yes | **Retain** |
| Rows | 37 | 37 | MERGE attrs; validate no LIVE-only keys lost |

---

### 5. TRN_RM_CLASS_1 — Risk: Low* | Phase 1 — **VALIDATION BLOCKER**

| Object | UAT | LIVE | Action |
| --- | --- | --- | --- |
| Shared columns (70) | Match | Match | None |
| `Material_Name` varchar(50) NULL | No | Yes | **Retain** |
| PK | `RMFID` | `RMFID` | None |
| Identity | `Id` curr=330 | curr=509 | Preserve; never reseed LIVE |
| Rows | 330 | **509** | MERGE missing UAT→LIVE only; **never delete** LIVE extras |
| Phase 1 rule “COUNT must match” | — | — | **Cannot satisfy** without forbidden DELETE |

\*Schema risk low; process risk medium because of conflicting validation rule.

---

### 6. PolicyException — Risk: Medium | Phase 2

| Object | UAT | LIVE | Action |
| --- | --- | --- | --- |
| Columns (15) | Match | Match | None |
| PK | `ExceptionId` | `ExceptionId` | None |
| Identity | curr=90002 | curr=1 | Use IDENTITY_INSERT for UAT keys if preserving ExceptionId |
| Index `IX_PolicyException_Employee_Active_Dates` | Yes | No | **Add** |
| Checks EffectiveDates / Priority | Yes | No | **Add** |
| Rows | 4 | **0** | Insert missing (Ids 1,2,4,90002) |
| Trigger | None | None | None |

---

### 7. ApprovalRequestStatusHistory — Risk: Medium | Phase 3 (schema only)

| Object | UAT | LIVE | Action |
| --- | --- | --- | --- |
| Base columns | Match | Match | None |
| `ActionType` nvarchar(30) NULL | Yes | No | **Add** |
| `ActivityId` int NULL | Yes | No | **Add** |
| `ApprovalRequestDetailId` int NULL | Yes | No | **Add** |
| `ApproverLevel` int NULL | Yes | No | **Add** |
| `DetailVersion` int NULL | Yes | No | **Add** |
| FK `FK_ApprovalHistory_Request` | Yes | Yes | None |
| FK `FK_ApprovalHistory_Detail` | Yes | No | **Add** (nullable FK; safe) |
| Rows | 707 | 4,403 | **Do not migrate / modify** |
| Trigger | None | None | None |

> Audit ledger — additive schema only. Aligns with RC-014C history column growth.

---

### 8. SalesApprovalRequestDetail — Risk: High | Phase 3

| Object | UAT | LIVE | Action |
| --- | --- | --- | --- |
| Columns (21) | Match | Match | **None** |
| PK / FKs / Indexes | Match | Match | **None** |
| Identity | curr=3141 | curr=34645 | Never reseed |
| Rows | 3,023 | 34,257 | Preserve snapshots / DetailVersion |

**Verdict:** Schema already synchronized. Phase 3 = validation-only PASS expected.

---

### 9. SalesDailyActivity — Risk: High | Phase 3

| Object | UAT | LIVE | Action |
| --- | --- | --- | --- |
| Columns (55) | Match | Match | None |
| PK | `ActivityId` | Match | None |
| Check `CK_SalesDailyActivity_ClaimStatus` | Includes **`Returned`** | Missing `Returned` | **Review → Add `Returned`** to LIVE check (UAT-certified TA/DA) |
| Rows | 81,171 | 87,402 | Preserve |

---

### 10. SalesDailyExpense — Risk: High | Phase 3

| Object | UAT | LIVE | Action |
| --- | --- | --- | --- |
| Shared columns (35) | Match | Match | None |
| `ApprovalStatus` nvarchar(50) NULL | Yes | No | **Add** |
| `ApprovalRemarks` nvarchar(1000) NULL | Yes | No | **Add** |
| `ApprovedBy` nvarchar(100) NULL | Yes | No | **Add** |
| `ApprovedOn` datetime2(7) NULL | Yes | No | **Add** |
| Checks Amount / NoUpdate_WhenDeleted | Match | Match | None |
| FK → SalesDailyActivity | Match | Match | None |
| Rows | 2,377 | 30,181 | Preserve ClaimStatus / receipts |

---

### 11. VMS_VisitAttendanceHistory — Risk: High | Phase 3

| Object | UAT | LIVE | Action |
| --- | --- | --- | --- |
| Columns (8) | Match | Match | None |
| PK | `HistoryID` | Match | None |
| Indexes `IX_..._DayLogID`, `IX_..._LogID`, `UQ_..._OpenDayLog` | Yes | No | **Add** (validate UQ against LIVE data first) |
| Rows | 6 | 4 | Preserve |

> Gate: before creating `UQ_VMS_VisitAttendanceHistory_OpenDayLog`, verify LIVE has no duplicate open DayLogID rows.

---

### 12. CMS_RawConsumptionLogs — Risk: Low | Phase 4

| Object | UAT | LIVE | Action |
| --- | --- | --- | --- |
| Columns (10) | Match | Match | None |
| Unique `UX_CMS_RawConsumptionLogs_Dedup` | No | Yes | **Retain** |
| Rows | 0 | 772,493 | **Do not migrate** logs |

---

### 13. TRN_qc_packet_weight_hdr — Risk: Medium | Phase 4

| Object | UAT | LIVE | Action |
| --- | --- | --- | --- |
| Shared columns (26) | Match | Match | None |
| `FormID` int NULL DF(21) | No | Yes | **Retain** |
| FK → TRN_qcinspector | Match | Match | None |
| Rows | 3,797 | 37,952 | Preserve |

---

### 14. TRN_qcinspector — Risk: Medium | Phase 4

| Object | UAT | LIVE | Action |
| --- | --- | --- | --- |
| Shared columns (65) | Match | Match | None |
| `ShiftDate` date NULL | No | Yes | **Retain** |
| Indexes Compliance / Report | No | Yes | **Retain** |
| FKs → MST_UserMaster (3) | Match | Match | None |
| Rows | 62,926 | 80,474 | Preserve |

---

## C. Column-diff rollup (actionable only)

| Table | Diff | UAT | LIVE | Action |
| --- | --- | --- | --- | --- |
| FrequencyTypes | FrequencyFactor | — | int NULL | Retain |
| MST_Calendar | FinancialWeek | int NULL | — | Add |
| MST_Calendar | 13 holiday/FY cols | — | present | Retain |
| MST_EmployeeTolerance | MaxKmModificationCount | int NOT NULL DF(1) | — | Add |
| TRN_RM_CLASS_1 | Material_Name | — | varchar(50) NULL | Retain |
| ApprovalRequestStatusHistory | ActionType, ActivityId, ApprovalRequestDetailId, ApproverLevel, DetailVersion | present | — | Add |
| SalesDailyExpense | ApprovalStatus, ApprovalRemarks, ApprovedBy, ApprovedOn | present | — | Add |
| TRN_qc_packet_weight_hdr | FormID | — | int NULL DF(21) | Retain |
| TRN_qcinspector | ShiftDate | — | date NULL | Retain |

**No shared-column datatype mismatches.** No destructive ALTER required for type conversion.

---

## D. Constraint / index / trigger summary

| Category | Finding |
| --- | --- |
| PK columns | All 14 **MATCH** (names may differ system-generated suffixes on some) |
| FK | Only gap: UAT `FK_ApprovalHistory_Detail` missing on LIVE |
| Check | UAT-only: EmpTol MaxKm, PolicyException×2; **CHECK_DIFF**: SalesDailyActivity ClaimStatus (`Returned`) |
| Indexes | See per-table; several LIVE-only retained; UAT-only on PolicyException + VMS |
| Triggers | **None** on all 14 tables both sides |
| Computed columns | **None** |

---

## E. Phase 1 gate conflict (flag for decision)

`TRN_RM_CLASS_1` Phase 1 validation (“UAT vs LIVE COUNT must match”) **conflicts** with “Never delete LIVE rows” because LIVE already has **179 extra** rows.

**Recommended rule change for this table only:**  
Validate `LIVE_COUNT >= UAT_COUNT` and `UAT keys ⊆ LIVE keys` after MERGE — not equality.

---

## F. Recommended next step

Per staged workflow: **start Phase 1 Table 1 — `FrequencyTypes`**.

Deliverables for that iteration (when approved):

1. Difference summary (this matrix slice)  
2. Idempotent MERGE + schema retain notes  
3. Validation SQL  
4. Rollback SQL  
5. Risk = Low  
6. PASS gate before Table 2

**No LIVE execution until you approve Table 1 patch.**

---

## G. Artifacts produced this phase

| File | Purpose |
| --- | --- |
| `Database/Scripts/015_RC018_Phase0_Discovery.sql` | Per-DB inventory |
| `Database/Scripts/015b_RC018_Phase0_CrossDiff.sql` | Cross-DB column/row diff |
| `Database/Scripts/015c_RC018_Phase0_KeysIndexes.sql` | PK/FK/index/check/trigger/identity |
| `docs/reviews/Post_GoLive/RC018/Phase0_UAT_AnmolDristiDev.txt` | Raw UAT dump |
| `docs/reviews/Post_GoLive/RC018/Phase0_LIVE_AnmolDristi.txt` | Raw LIVE dump |
| `docs/reviews/Post_GoLive/RC018/Phase0_CrossDiff.txt` | Column + count matrix |
| `docs/reviews/Post_GoLive/RC018/Phase0_KeysIndexes.txt` | Keys/indexes matrix |
| `docs/reviews/Post_GoLive/RC018/Phase0_SampleData.txt` | FrequencyTypes / Policy / EmpTol peek |
| `docs/reviews/Post_GoLive/RC018/RC018_Phase0_Discovery_Report.md` | This report |
