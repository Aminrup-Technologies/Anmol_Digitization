# 11 — Refactoring Backlog

**DO NOT implement here.** Backlog items only.  
Effort uses complexity sizing (S/M/L/XL), not calendar duration.

---

## BL-01 — Split stage vs synchronize on DataImport

| Field | Content |
|---|---|
| Title | Make DataImport stage-only; reserve sync for BatchManager |
| Description | Remove or feature-flag `ProcessBatch` call from `btnProcess_Click` so Claims Config “Import” vs “Process” matches runtime. Optionally set batch Status=`Uploaded` after bulk. |
| Affected files | `HrmsDataImport.aspx.cs`, possibly `HrmsDataImport.aspx` (remove overwrite checkbox if unused) |
| Estimated effort | M |
| Priority | Critical |
| Risk | Medium — changes operator workflow; needs SP/status coordination |
| Dependency | Clarify intended product workflow; may need Status default/SP changes (SQL not in repo) |
| Expected benefit | Eliminates double-sync ambiguity; aligns UI labels with behavior |

---

## BL-02 — Unify SP invocation helper

| Field | Content |
|---|---|
| Title | Shared `HrmsSynchronizationService` for parameter packing + multi-RS read |
| Description | One method builds `@BatchId/@AllowOverwrite/@ProcessingMode/@Modules/@OverwriteFields/@ExecutedBy`, executes SP, returns typed summary + optional debug table. |
| Affected files | New helper under `AnmolDristi/` or `BLL/`; both page code-behinds |
| Estimated effort | M |
| Priority | High |
| Risk | Low–Medium |
| Dependency | Agree summary RS contract with DB owners |
| Expected benefit | Removes duplicate call logic; consistent result handling |

---

## BL-03 — Align module tokens and processing mode policy

| Field | Content |
|---|---|
| Title | Single constants for Modules and ProcessingMode |
| Description | Replace magic strings `"Employees"`, `"EMPLOYEE"`, etc. with shared constants matching SP contract; wire DataImport checkbox to mode or remove it. |
| Affected files | Both code-behinds; ASPX checkbox/radio labels |
| Estimated effort | S |
| Priority | Critical |
| Risk | High if SP token contract misunderstood — verify against live SP |
| Dependency | Extract/read `usp_RunHRMSSynchronization` |
| Expected benefit | Prevents silent no-op sync paths |

---

## BL-04 — Fix BatchManager result-set binding

| Field | Content |
|---|---|
| Title | Bind summary result set to `gvResult` / toast |
| Description | Use `DataSet` or reader skip logic consistent with DataImport; show Inserted/Updated/Errors in `lblRunMessage`. |
| Affected files | `HrmsBatchManager.aspx.cs` |
| Estimated effort | S |
| Priority | High |
| Risk | Low |
| Dependency | Confirm RS order from SP |
| Expected benefit | Operators see real outcomes |

---

## BL-05 — Compensating actions for failed imports

| Field | Content |
|---|---|
| Title | Transactional batch lifecycle + failure status |
| Description | Create batch + bulk in coordinated fashion; on failure mark `Status=Failed` or delete orphan batch/staging; never leave silent orphans. |
| Affected files | `HrmsDataImport.aspx.cs`; likely SQL status rules |
| Estimated effort | L |
| Priority | High |
| Risk | Medium |
| Dependency | Table Status domain values; possible SP changes |
| Expected benefit | Data integrity |

---

## BL-06 — Authorization for HRMS import

| Field | Content |
|---|---|
| Title | Enforce permission keys on both pages |
| Description | Call `RequirePermission` (or equivalent) for upload and process actions; hide Claims_Config buttons without permission. |
| Affected files | Both pages; `Claims_Config.aspx(.cs)`; Permission seed data |
| Estimated effort | M |
| Priority | High |
| Risk | Low (may lock out users until roles updated) |
| Dependency | Permission catalog entries |
| Expected benefit | Reduces unauthorized master-data mutation |

---

## BL-07 — Hardening file upload

| Field | Content |
|---|---|
| Title | Validate extension/size; store outside web root; cleanup on cancel |
| Description | Server-side `.csv` check, max size, unique folder ACL; delete on cancel; scheduled sweep for orphans. |
| Affected files | `HrmsDataImport.aspx.cs`; IIS/static config; possibly `Web.config` |
| Estimated effort | M |
| Priority | High |
| Risk | Low |
| Dependency | Ops decision on storage path |
| Expected benefit | Security + disk hygiene |

---

## BL-08 — Re-run / concurrency guards

| Field | Content |
|---|---|
| Title | Prevent concurrent or completed-batch re-run without explicit override |
| Description | Mirror patterns from `AdminBatches.GetBatchState` adapted to int BatchId + ImportBatchMaster.Status / staging state. |
| Affected files | `HrmsBatchManager.aspx.cs`; possibly SQL |
| Estimated effort | M |
| Priority | High |
| Risk | Medium |
| Dependency | Reliable Status transitions from SP |
| Expected benefit | Concurrency safety |

---

## BL-09 — Extract SQL objects into repo

| Field | Content |
|---|---|
| Title | Version-control HRMS tables + `usp_RunHRMSSynchronization` |
| Description | Extract from live DB into `Database/` like other modules; document nested deps. |
| Affected files | New SQL under `Database/`; Inventory docs |
| Estimated effort | L |
| Priority | Critical |
| Risk | Low for extract-only |
| Dependency | DB access |
| Expected benefit | Enables real SP review, CI, and safer change |

---

## BL-10 — Remove dead code

| Field | Content |
|---|---|
| Title | Delete unused types, commented V2, commented ASPX, unused usings |
| Description | Remove `SyncExecutionContext`, commented ProcessBatch V2, BatchManager commented overwrite UI if permanently replaced. |
| Affected files | Both ASPX/CS |
| Estimated effort | S |
| Priority | Low |
| Risk | Low |
| Dependency | None |
| Expected benefit | Clarity |

---

## BL-11 — CSV export correctness

| Field | Content |
|---|---|
| Title | RFC-style escape all CSV fields; stream writer |
| Description | Quote all fields; handle newlines; optional BOM. |
| Affected files | `HrmsBatchManager.aspx.cs` `btnExportReport_Click` |
| Estimated effort | S |
| Priority | Medium |
| Risk | Low |
| Dependency | None |
| Expected benefit | Reliable reports |

---

## BL-12 — Select2 / script cleanup

| Field | Content |
|---|---|
| Title | Use one local Select2 version |
| Description | Prefer `Resources/select2` already in repo; remove CDN 3/4 mix. |
| Affected files | `HrmsDataImport.aspx` |
| Estimated effort | S |
| Priority | Medium |
| Risk | Low |
| Dependency | None |
| Expected benefit | Stable mapping UX |

---

## BL-13 — UpdatePanel selection persistence

| Field | Content |
|---|---|
| Title | Re-apply selected row highlight after async rebind |
| Description | `RowDataBound` like AdminBatches; keep `hfSelectedBatch` across filter. |
| Affected files | `HrmsBatchManager.aspx(.cs)` |
| Estimated effort | S |
| Priority | Low |
| Risk | Low |
| Dependency | None |
| Expected benefit | UX consistency |

---

## Priority overview

| Priority | Items |
|---|---|
| Critical | BL-01, BL-03, BL-09 |
| High | BL-02, BL-04, BL-05, BL-06, BL-07, BL-08 |
| Medium | BL-11, BL-12 |
| Low | BL-10, BL-13 |
