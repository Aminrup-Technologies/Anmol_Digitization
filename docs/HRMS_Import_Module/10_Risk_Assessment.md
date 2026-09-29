# 10 — Risk Assessment

Risks are grounded in observed code behavior. Where SQL internals are unknown, that uncertainty is itself a risk.

---

## Critical

| ID | Risk | Category | Evidence |
|---|---|---|---|
| C-1 | Upload path always sends `@ProcessingMode=0` while UI offers “Allow Overwrite”, so operators may believe updates occur when the app forces Insert Only. | Business / Data integrity | `HrmsDataImport.ProcessBatch` hard-codes mode `0`; checkbox still shown |
| C-2 | Same batch can be synchronized from DataImport and again from BatchManager with no app-level re-run guard; outcome depends on unknown SP idempotency. | Data integrity / Concurrency | Both call `usp_RunHRMSSynchronization`; no status lock in C# |
| C-3 | Module token `"Employees"` (DataImport) vs `"EMPLOYEE"` (BatchManager) may cause divergent SP behavior. | Business / Data integrity | `ProcessBatch` vs `GetSelectedModules` |
| C-4 | HRMS SQL objects are not version-controlled in repo → production drift / unreviewable sync logic affecting `MST_UserMaster` (and possibly more). | Maintainability / Business | Missing scripts under `Database/`; Inventory notes related hierarchy SPs elsewhere |

---

## High

| ID | Risk | Category | Evidence |
|---|---|---|---|
| H-1 | Orphan `ImportBatchMaster` (+ partial staging) if failure occurs after INSERT and during/after bulk copy. | Data integrity | Separate connections; no compensating transaction in `btnProcess_Click` |
| H-2 | BatchManager `Fill(DataTable)` likely surfaces first SP result set; DataImport skips first RS looking for summary — operators may see debug data or empty useful summary while toast says success. | Business / Maintainability | `btnRun_Click` vs `ProcessBatch` reader |
| H-3 | Any authenticated session can reach import URLs; no `RequirePermission` on these pages. | Security | Both `Page_Load` only check `Session["UserId"]` |
| H-4 | Temp CSV with employee PII stored under web-accessible `~/TempUploads/`; cancel does not delete file; sample dumps present in tree. | Security | `btnReadHeaders_Click`; `btnCancel_Click`; `TempUploads/*.csv` |
| H-5 | Connection string with credentials committed in `Web.config`. | Security | `Web.config` `DbConn` |

---

## Medium

| ID | Risk | Category | Evidence |
|---|---|---|---|
| M-1 | CSV export breaks on commas/quotes in EmpName/Email/etc. (only ErrorMessage escaped). | Data integrity / Reporting | `btnExportReport_Click` |
| M-2 | No server-side CSV type/size validation → DoS or parse abuse. | Security / Performance | `fileUploadCsv` client `accept` only |
| M-3 | `CompleteRequest` after CSV write may still allow leftover page bytes in some pipelines. | Reporting | `btnExportReport_Click` |
| M-4 | Select2 3.5.3 CSS + 4.1.0 script + 3.5.3 fallback → broken mapping UX risk. | Maintainability | `HrmsDataImport.aspx` scripts |
| M-5 | `OverwriteFields` still collected when mode ≠ 2 (panel hidden) and sent to SP. | Business | `GetSelectedOverwriteFields` + `btnRun_Click` |
| M-6 | Filter/Clear async postback clears `gvResult` in code but `upResult` may not refresh. | Maintainability / UX | UpdatePanel triggers |
| M-7 | `Session["OriginalFileName"]` never cleared; stale name if session reused oddly. | Data integrity | `btnCancel_Click` omits it |
| M-8 | Forced `GC.Collect` during cleanup can stall IIS thread pool under load. | Performance | `btnProcess_Click` finally |
| M-9 | Large imports hold request thread up to 300s (bulk + SP). | Performance / Concurrency | Timeouts 300; synchronous page request |

---

## Low

| ID | Risk | Category | Evidence |
|---|---|---|---|
| L-1 | Dead code (`SyncExecutionContext`, commented V2, unused usings) confuses maintainers. | Maintainability | `HrmsBatchManager.aspx.cs`; `HrmsDataImport.aspx.cs` |
| L-2 | Selected-row CSS lost after async grid rebind (client-only highlight). | UX | `onRowSelect` vs no RowDataBound re-apply |
| L-3 | Status filter values may not match actual `ImportBatchMaster.Status` writers. | Business | UI list vs unknown writers |
| L-4 | Inconsistent `@ExecutedBy` fallbacks (`SYSTEM` vs `""`). | Maintainability / Audit | Two call sites |
| L-5 | `SqlDataAdapter`/`DataTable` not disposed in some BatchManager paths. | Maintainability | `LoadOverwriteFields`, `btnRun_Click` |
| L-6 | BatchManager SP name omits `dbo.` schema prefix. | Maintainability | `btnRun_Click` command text |

---

## Risk map by category

| Category | IDs |
|---|---|
| Business | C-1, C-3, H-2, M-5, L-3 |
| Data integrity | C-1, C-2, C-3, H-1, M-1, M-7 |
| Concurrency | C-2, M-9 |
| Security | H-3, H-4, H-5, M-2 |
| Maintainability | C-4, H-2, M-4, M-6, L-1, L-4, L-5, L-6 |
| Performance | M-2, M-8, M-9 |

---

## Uncertainty risk (explicit)

Because `usp_RunHRMSSynchronization` and staging/batch table scripts are absent:

- Cannot confirm transactional atomicity across masters.  
- Cannot confirm whether failed rows leave masters half-updated.  
- Cannot confirm whether ProcessingMode/Modules short-circuit correctly.  

These are **Critical knowledge gaps**, even when not expressible as a concrete defect line in C#.
