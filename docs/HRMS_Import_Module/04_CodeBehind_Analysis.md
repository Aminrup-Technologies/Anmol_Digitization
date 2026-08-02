# 04 — Code-Behind Analysis

Per-method analysis for `HrmsDataImport.aspx.cs` and `HrmsBatchManager.aspx.cs`.  
Designer files only declare controls (no business logic).

---

## A. HrmsDataImport.aspx.cs

**Class:** `AnmolDristi.HrmsDataImport : BasePage`

### Field: `ExpectedDbColumns`

| Attribute | Detail |
|---|---|
| Purpose | Fixed list of staging column names offered for mapping and used to shape the in-memory `DataTable` |
| Caller | `btnReadHeaders_Click`, `CreateStagingTable` |
| DB | Indirect — names must match `Staging_EmployeeData` columns for `SqlBulkCopy` mappings |
| Notes | Constant array; includes DOJ, Gender, Category_*, Company_*, SubDepartment_ID, manager fields |

---

### `GetConnectionString()`

| | |
|---|---|
| Purpose | Return `DbConn` connection string |
| Caller | `btnProcess_Click` (indirect via local var), `InsertBatch`, `ProcessBatch` |
| Called methods | `ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString` |
| Exceptions | NullReference if connection string missing |
| Returns | `string` |

---

### `Page_Load`

| | |
|---|---|
| Purpose | Session gate |
| Caller | ASP.NET runtime |
| Called | `Response.Redirect` when `Session["UserId"]` null/empty |
| DB | None |
| Exceptions | Redirect endResponse true |
| Dead block | `if (!IsPostBack) { }` empty |

---

### `btnReadHeaders_Click`

| | |
|---|---|
| Purpose | Persist uploaded CSV, read headers, bind mapping UI |
| Caller | Button postback |
| Called | `Directory.Exists/CreateDirectory`, `fileUploadCsv.SaveAs`, `TextFieldParser`, `rptMapping.DataBind`, `ShowMessage` |
| DB | None |
| Session writes | `TempCsvPath`, `OriginalFileName`, `ExcelHeaders` |
| Exceptions | Caught → `ShowMessage` with `ex.Message` |
| Returns | void |

**Complexity:** Moderate (IO + parse + bind).

---

### `rptMapping_ItemDataBound`

| | |
|---|---|
| Purpose | Populate each mapping dropdown; auto-select exact header match |
| Caller | Repeater DataBind |
| Called | `FindControl`, `Session["ExcelHeaders"]`, `Items.FindByText` |
| DB | None |
| Rule in code | Auto-select only when `FindByText(hfDbColumn.Value)` succeeds (exact text match) |

---

### `btnProcess_Click`

| | |
|---|---|
| Purpose | End-to-end: map → create batch → bulk insert → sync → cleanup UI |
| Caller | Button postback |
| Called | `btnCancel_Click`, `GetConnectionString`, `CreateStagingTable`, `InsertBatch`, `ProcessBatch`, `ShowMessage`, `File.Delete`, `GC.Collect`, `GC.WaitForPendingFinalizers` |
| DB | INSERT `ImportBatchMaster`; bulk `Staging_EmployeeData`; SP via `ProcessBatch` |
| Exceptions | Outer catch → message; finally cleanup swallows delete errors to Debug |
| Returns | void |

**Long / high-complexity method:** Yes — orchestration, parsing loop, DB, cleanup in one method (~140 lines).

**Possible exceptions:** Missing `Session["OriginalFileName"]` → NullReference on `.ToString()`; SQL errors; IO errors; bulk failures wrapped by `InsertBatch`.

---

### `btnCancel_Click`

| | |
|---|---|
| Purpose | Reset panels and clear some session keys |
| Caller | Button; also `btnProcess_Click` success and temp-missing path |
| Session clears | `TempCsvPath`, `ExcelHeaders` only |
| Does not | Delete temp file; clear `OriginalFileName` |

---

### `CreateStagingTable`

| | |
|---|---|
| Purpose | Build `DataTable` with `BatchId` + all `ExpectedDbColumns` as `string` |
| Caller | `btnProcess_Click` |
| Returns | `DataTable` |

---

### `InsertBatch`

| | |
|---|---|
| Purpose | Transactional `SqlBulkCopy` into `dbo.Staging_EmployeeData` |
| Caller | `btnProcess_Click` (chunked) |
| Called | `SqlConnection`, `BeginTransaction`, `SqlBulkCopy.WriteToServer` |
| Timeout | `BulkCopyTimeout = 300` |
| Exceptions | Rollback; rethrow `Exception("Bulk insert failed: ...")` |
| Returns | void |

---

### `ProcessBatch` (active)

| | |
|---|---|
| Purpose | Execute `dbo.usp_RunHRMSSynchronization` and show summary counts |
| Caller | `btnProcess_Click` |
| Parameters passed | See § Variable tracking below |
| Result handling | `NextResult()` loop skips first RS; reads first subsequent RS with rows; optional columns via `HasColumn` |
| Exceptions | Not caught locally — bubble to `btnProcess_Click` |
| Comment | `"Temporary values until V2 is completed"` |

---

### `ProcessBatch` (commented dead code)

| | |
|---|---|
| Purpose | Former path calling `dbo.usp_ProcessEmployeeBatch_V2` |
| Status | Fully commented (~L345–372) — **dead code** |
| Expected columns | `ProcessedCount`, `ErrorCount`, `SkippedCount` |

---

### `HasColumn`

| | |
|---|---|
| Purpose | Case-insensitive column existence on `IDataRecord` |
| Caller | `ProcessBatch` |
| Returns | `bool` |

---

### `ShowMessage`

| | |
|---|---|
| Purpose | Set `lblMessage` text + CSS success/danger |
| Caller | Multiple handlers |

---

## B. HrmsBatchManager.aspx.cs

**Class:** `AnmolDristi.HrmsBatchManager : AnmolDristi.BasePage`

### `GetConnectionString`

Same pattern as DataImport — **duplicate logic**.

---

### `Page_Load`

| | |
|---|---|
| Purpose | Session gate; initial load of batches + overwrite fields |
| Called on !IsPostBack | `LoadBatches`, `LoadOverwriteFields` |

---

### `LoadBatches`

| | |
|---|---|
| Purpose | Query batch list with staging aggregates and optional filters |
| Caller | `Page_Load`, `btnFilter_Click`, `btnClear_Click`, `btnRun_Click` |
| SQL | Inline SELECT join `ImportBatchMaster` ⟕ `Staging_EmployeeData` |
| ImportStatus literals used | `'Processed'`, `'Error'`, `'Pending'` |
| Side effects | Clears `lblPageError`, `lblRunMessage` |
| Disposables | Adapter + DataTable in `using` |

**Complexity:** Moderate (dynamic SQL filters).

---

### `LoadOverwriteFields`

| | |
|---|---|
| Purpose | Bind `rptOverwriteFields` from `MST_HRMSImportField` where `IsVisible=1` |
| SQL | `SELECT * ... ORDER BY Category,DisplayOrder` |
| Disposables | Adapter/DataTable **not** in `using` |

---

### `rblProcessingMode_SelectedIndexChanged`

| | |
|---|---|
| Purpose | Show overwrite field panel only when mode == `"2"` |
| Returns | void |

---

### `btnSelectAll_Click` / `btnClearAll_Click`

| | |
|---|---|
| Purpose | Check/uncheck all `chkField` in repeater |
| Duplicate pattern | Symmetric loops |

---

### `GetSelectedOverwriteFields`

| | |
|---|---|
| Purpose | Join checked `FieldCode` values with `\|` |
| Caller | `btnRun_Click` |
| Returns | `string` (possibly empty) |

---

### Nested class `SyncExecutionContext`

| | |
|---|---|
| Purpose | DTO for BatchId, AllowOverwrite, ProcessingMode, Modules, OverwriteFields, ExecutedBy |
| Status | **Dead code** — never instantiated or referenced |

---

### `btnFilter_Click` / `btnClear_Click` / `ClearSelection`

| Method | Purpose |
|---|---|
| `btnFilter_Click` | Clear selection + reload batches |
| `btnClear_Click` | Reset filters + clear selection + reload |
| `ClearSelection` | Reset hidden field, labels, empty `gvResult` |

---

### `btnRun_Click`

| | |
|---|---|
| Purpose | Execute synchronization for selected batch with UI config |
| Caller | Button (+ client confirm) |
| Called | `GetSelectedModules`, `GetSelectedOverwriteFields`, `LoadBatches` |
| DB | `usp_RunHRMSSynchronization` via `SqlDataAdapter.Fill` |
| Exceptions | catch → `lblPageError = ex.Message` |
| Success UX | Always sets “Synchronization completed successfully.” if no exception |

**Long method:** Moderate length; orchestration of params + SP + bind.

**Disposable note:** `SqlDataAdapter` / `DataTable` not wrapped in `using`.

---

### `GetSelectedModules`

| | |
|---|---|
| Purpose | Selected `chkModules` values, trimmed, **ToUpper**, joined by `\|` |
| Returns | e.g. `MASTER\|EMPLOYEE\|HIERARCHY` |

---

### `btnExportReport_Click`

| | |
|---|---|
| Purpose | Stream CSV of staging rows for selected batch |
| SQL | `SELECT EmpCode, EmpName, Email, MobileNo, ImportStatus, ErrorMessage FROM dbo.Staging_EmployeeData WHERE BatchId=@BatchId` |
| Escaping | Only `ErrorMessage` quote-escaped |
| Exit | `CompleteRequest()`; `Response.End` commented |

---

## C. Call trees

### DataImport process path

```
btnProcess_Click
├─ ShowMessage / btnCancel_Click (guard)
├─ GetConnectionString
├─ INSERT ImportBatchMaster (inline SQL)
├─ CreateStagingTable
├─ TextFieldParser loop
│  └─ InsertBatch*
│     └─ SqlBulkCopy → Staging_EmployeeData
├─ ProcessBatch
│  ├─ usp_RunHRMSSynchronization
│  ├─ HasColumn*
│  └─ ShowMessage
├─ File.Delete
└─ btnCancel_Click
finally → File.Delete (+ GC.Collect)
```

### BatchManager run path

```
confirmRun (JS)
└─ btnRun_Click
   ├─ GetSelectedModules
   ├─ GetSelectedOverwriteFields
   ├─ usp_RunHRMSSynchronization
   ├─ gvResult.DataBind
   └─ LoadBatches
        └─ inline aggregate SQL
```

---

## D. Dead code

| Item | File | Evidence |
|---|---|---|
| Commented `ProcessBatch` V2 | `HrmsDataImport.aspx.cs` | Entire method commented |
| Empty `!IsPostBack` block | `HrmsDataImport.Page_Load` | No statements |
| `SyncExecutionContext` | `HrmsBatchManager.aspx.cs` | No references |
| `using System.Linq` | `HrmsBatchManager.aspx.cs` | No LINQ usage |
| Commented ASPX `txtProcessedBy` / `chkAllowOverwrite` | `HrmsBatchManager.aspx` | Server comment block |
| Commented `Response.End` | `btnExportReport_Click` | Replaced by `CompleteRequest` |

---

## E. Duplicate code

| Pattern | Locations |
|---|---|
| `GetConnectionString` | Both code-behinds |
| Session `UserId` redirect | Both `Page_Load` |
| SP invoke `usp_RunHRMSSynchronization` | `ProcessBatch` vs `btnRun_Click` (different parameter defaults) |
| TextFieldParser header setup | `btnReadHeaders_Click` and `btnProcess_Click` |
| Select-all / clear-all loops | Two nearly identical methods |

---

## F. Long / high-complexity methods

| Method | Why |
|---|---|
| `HrmsDataImport.btnProcess_Click` | Multi-phase orchestration, parsing, DB, cleanup |
| `HrmsDataImport.ProcessBatch` | Multi-result-set reader logic |
| `HrmsBatchManager.LoadBatches` | Dynamic SQL + aggregates |
| `HrmsBatchManager.btnRun_Click` | Config gather + SP + UI refresh |
| `HrmsBatchManager.btnExportReport_Click` | Query + CSV build + response streaming |
