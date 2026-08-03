# 07 — Business Rules

**Constraint:** Only rules implemented in the studied application code (ASPX/CS/JS).  
Rules that would live solely inside `usp_RunHRMSSynchronization` are **Not determinable from current repository** and are listed under “Not found in application code.”

Each rule cites file + method/location.

---

## 1. Authentication / access rules

| ID | Rule | Source |
|---|---|---|
| AUTH-1 | If `Session["UserId"]` is null or empty string, redirect to `~/Login.aspx?msg=session_expired` with `endResponse: true`. | `HrmsDataImport.Page_Load`; `HrmsBatchManager.Page_Load` |
| AUTH-2 | No permission-key check (`RequirePermission` / `HasPermission`) is invoked by either HRMS page. | Absence in both code-behinds; contrast `BasePage.cs` helpers unused here |
| AUTH-3 | `BasePage.OnInit` may still validate login token heartbeat when `Session["LoginToken"]` is present. | `BasePage.EnsureSessionValid` |

---

## 2. Upload & mapping rules

| ID | Rule | Source |
|---|---|---|
| MAP-1 | Analyze requires a selected file (`fileUploadCsv.HasFile`); otherwise show error and stop. | `btnReadHeaders_Click` |
| MAP-2 | Uploaded file is stored under `~/TempUploads/{GuidN}_{originalFileName}`. | `btnReadHeaders_Click` |
| MAP-3 | CSV delimiter is comma; fields may be quote-enclosed (`HasFieldsEnclosedInQuotes = true`). | `btnReadHeaders_Click`, `btnProcess_Click` |
| MAP-4 | Header names are trimmed when collected. | `btnReadHeaders_Click` |
| MAP-5 | Mapping targets are exactly the `ExpectedDbColumns` list (fixed in code). | Field `ExpectedDbColumns`; bind loop |
| MAP-6 | Dropdown option with value `""` means “Ignore / Do Not Map”; ignored columns are omitted from `dbToExcelMap`. | `rptMapping_ItemDataBound`; `btnProcess_Click` map builder |
| MAP-7 | Auto-map occurs only when CSV header text **exactly** equals DB column name (`FindByText`). | `rptMapping_ItemDataBound` |
| MAP-8 | Process requires `Session["TempCsvPath"]` non-empty and file existing; else message + cancel reset. | `btnProcess_Click` |
| MAP-9 | During row import, header lookup is case-insensitive (`StringComparer.OrdinalIgnoreCase`). | `btnProcess_Click` `headerIndexMap` |
| MAP-10 | Cell values written to staging are `.Trim()`med. | `btnProcess_Click` |
| MAP-11 | Rows are flushed to SQL every **5000** staging rows. | `btnProcess_Click` |
| MAP-12 | There is no server-side check that `EmpCode` (or any column) must be mapped. | Absence in `btnProcess_Click` |
| MAP-13 | There is no server-side file extension or size validation. | Absence; only client `accept=".csv"` |

---

## 3. Batch creation rules

| ID | Rule | Source |
|---|---|---|
| BAT-1 | A batch row is inserted with `FileName = Session["OriginalFileName"]` and `UploadedBy = Session["UserId"]` or `"UnknownSystemUser"`. | `btnProcess_Click` |
| BAT-2 | New `BatchId` comes from `OUTPUT INSERTED.BatchId`. | same |
| BAT-3 | C# does not set `ImportBatchMaster.Status` or `UploadDate` on insert. | INSERT column list only `FileName`, `UploadedBy` |
| BAT-4 | Batch insert commits before bulk copy (separate connection/command). Failure after insert can leave a batch without compensating delete in C#. | Control flow in `btnProcess_Click` |

---

## 4. Overwrite / AllowOverwrite rules (application layer)

| ID | Rule | Source |
|---|---|---|
| OVR-1 | DataImport UI checkbox `chkAllowOverwrite` defaults to checked. | `HrmsDataImport.aspx` |
| OVR-2 | DataImport passes `chkAllowOverwrite.Checked` as `@AllowOverwrite`. | `ProcessBatch` |
| OVR-3 | DataImport hard-codes `@ProcessingMode = 0` with comment “Insert Only”, regardless of checkbox. | `ProcessBatch` |
| OVR-4 | DataImport always passes `@OverwriteFields = DBNull.Value`. | `ProcessBatch` |
| OVR-5 | BatchManager derives `allowOverwrite = (processingMode != 0)`. | `btnRun_Click` |
| OVR-6 | BatchManager mode `2` shows overwrite field UI; other modes hide `divOverwriteFields`. | `rblProcessingMode_SelectedIndexChanged` |
| OVR-7 | Overwrite field codes sent are checked items’ `FieldCode` values joined by `\|`. | `GetSelectedOverwriteFields` |
| OVR-8 | Field checkbox default checked state comes from DB column `IsDefault`. | `HrmsBatchManager.aspx` repeater bind |
| OVR-9 | Only fields with `IsVisible=1` are loaded for selection. | `LoadOverwriteFields` |

**How the SP interprets `@AllowOverwrite` / `@ProcessingMode` / `@OverwriteFields`:** Not determinable from current repository.

---

## 5. Processing mode rules (UI + parameter mapping)

| ID | Rule | Source |
|---|---|---|
| MODE-1 | UI values: `0` “Insert New Employees Only”; `1` “Update All Existing Employees”; `2` “Update Selected Fields Only”. | `HrmsBatchManager.aspx` `rblProcessingMode` |
| MODE-2 | Default selected mode on BatchManager is `2`. | `Selected="True"` on value 2 |
| MODE-3 | Selected value is parsed with `int.TryParse` (failure leaves `0`). | `btnRun_Click` |
| MODE-4 | DataImport never reads UI mode; always sends `0`. | `ProcessBatch` |

Semantic meaning of modes inside SQL: Not determinable from current repository.

---

## 6. Module synchronization rules (UI + parameter mapping)

| ID | Rule | Source |
|---|---|---|
| MOD-1 | BatchManager modules: `MASTER`, `EMPLOYEE`, `HIERARCHY`; all selected by default. | `chkModules` markup |
| MOD-2 | Selected modules are trimmed, uppercased, joined with `\|`. | `GetSelectedModules` |
| MOD-3 | DataImport sends literal `@Modules = "Employees"` (not uppercased token `EMPLOYEE`). | `ProcessBatch` |
| MOD-4 | No C# validation requires at least one module selected. | Absence in `btnRun_Click` |

SQL module handling: Not determinable from current repository.

---

## 7. ExecutedBy / audit identity rules

| ID | Rule | Source |
|---|---|---|
| EXB-1 | DataImport `@ExecutedBy` = `Session["UserId"]` string or `"SYSTEM"`. | `ProcessBatch` |
| EXB-2 | BatchManager `@ExecutedBy` = `Session["UserId"]` string or `""`. | `btnRun_Click` |
| EXB-3 | Batch `UploadedBy` uses `"UnknownSystemUser"` fallback (different from ExecutedBy fallbacks). | `btnProcess_Click` |

---

## 8. ImportStatus rules (as used in C#)

| ID | Rule | Source |
|---|---|---|
| STA-1 | Batch grid counts rows with `ImportStatus = 'Processed'` as ProcessedCount. | `LoadBatches` |
| STA-2 | Counts `ImportStatus = 'Error'` as ErrorCount. | `LoadBatches` |
| STA-3 | Counts `ImportStatus = 'Pending'` as WaitingCount. | `LoadBatches` |
| STA-4 | Export includes raw `ImportStatus` and `ErrorMessage` per staging row. | `btnExportReport_Click` |

**Who sets ImportStatus / legal transitions / duplicate detection / hierarchy validation:** Not determinable from current repository (no SP/table defaults in repo).

---

## 9. Batch Status filter rules (UI)

| ID | Rule | Source |
|---|---|---|
| BST-1 | Filter dropdown offers `Processed`, `Pending`, `Uploaded`, or all. | `ddlStatus` markup |
| BST-2 | When a value is selected, SQL adds `b.Status = @Status`. | `LoadBatches` |
| BST-3 | C# never assigns `ImportBatchMaster.Status`. | INSERT omits Status; no UPDATE |

Meaning of each status value: Not determinable from current repository.

---

## 10. Run / export gating rules

| ID | Rule | Source |
|---|---|---|
| RUN-1 | Client blocks run without selected batch; confirms with dialog. | `confirmRun` JS |
| RUN-2 | Server blocks run if `hfSelectedBatch` whitespace. | `btnRun_Click` |
| RUN-3 | Server blocks export if `hfSelectedBatch` empty. | `btnExportReport_Click` |
| RUN-4 | No C# check prevents re-running a processed/in-progress batch. | Absence vs AdminBatches pattern |
| RUN-5 | SP command timeout is 300 seconds. | `ProcessBatch`; `btnRun_Click` |
| RUN-6 | Bulk copy timeout is 300 seconds. | `InsertBatch` |

---

## 11. Date filter rules

| ID | Rule | Source |
|---|---|---|
| DAT-1 | From date applied only if `DateTime.TryParse(txtFrom.Text)` succeeds → `UploadDate >= @FromDate`. | `LoadBatches` |
| DAT-2 | To date applied only if parse succeeds → `UploadDate <=` end of that calendar day (`AddDays(1).AddSeconds(-1)`). | `LoadBatches` |
| DAT-3 | Parameters added when parsed value `!= DateTime.MinValue`. | `LoadBatches` |

---

## 12. Reporting rules

| ID | Rule | Source |
|---|---|---|
| RPT-1 | Export filename `HRMS_BatchReport_{batchId}.csv`. | `btnExportReport_Click` |
| RPT-2 | CSV header line: `EmpCode,EmpName,Email,Mobile,Status,ErrorMessage`. | same |
| RPT-3 | Only `ErrorMessage` field is CSV-quote escaped. | same |

---

## 13. Cleanup rules

| ID | Rule | Source |
|---|---|---|
| CLN-1 | On successful process path, temp file is deleted then UI reset via `btnCancel_Click`. | `btnProcess_Click` |
| CLN-2 | `finally` also attempts temp delete (with GC collect) if file still exists. | `btnProcess_Click` |
| CLN-3 | Cancel clears session path/headers but does not delete the temp file. | `btnCancel_Click` |

---

## 14. Not found in application code (explicit)

The following were requested as examples but **cannot be documented as implemented rules from current C#/ASPX**:

- Duplicate employee detection algorithm  
- Identity validation beyond session presence  
- Hierarchy resolution/validation rules  
- Employee field validation (email/mobile/DOJ formats)  
- Exact overwrite field semantics per FieldCode  
- Synchronization transactional rules inside SQL  
- Idempotency / skip-already-processed logic  

All of the above: **Not determinable from current repository** without `usp_RunHRMSSynchronization` (and related) source.
