# 22 — SubDepartment Schema Alignment

**Status:** Delivered.  
**Scope:** End-to-end support for `SubDepartment_Description` so `MST_SubDepartment` can sync like other org masters.  
**Constraint:** No redesign of HSE architecture. Minimal, backward-compatible alignment only.

---

## 1. Root cause

| Layer | State before alignment |
|---|---|
| ZingHR CSV | Includes `SubDepartment_Description` |
| `MST_SubDepartment` | Has `SubDepartment_Description` |
| `usp_HSE_SynchronizeMasters` | Already upserts using `SubDepartment_ID` + `SubDepartment_Description` |
| `dbo.Staging_EmployeeData` | Had `SubDepartment_ID` only — **missing** `SubDepartment_Description` |
| `HrmsDataImport.ExpectedDbColumns` | Listed `SubDepartment_ID` only |

The synchronization procedure was correct; the staging schema and upload mapping list were incomplete. That blocked description-driven create/update of `MST_SubDepartment`.

---

## 2. Previous limitation

1. Upload mapping UI never offered `SubDepartment_Description`.
2. SqlBulkCopy could not populate a non-existent / unmapped staging column.
3. `usp_HSE_ValidateBatch` treated every unresolved `SubDepartment_ID` as a hard `Error` (no description available to allow SynchronizeMasters createability).
4. `usp_HSE_SynchronizeMasters` SubDepartment insert/update remained `0` whenever description was NULL.

---

## 3. Database change

**Script:** [`Database/Scripts/006_Add_Staging_SubDepartment_Description.sql`](../../Database/Scripts/006_Add_Staging_SubDepartment_Description.sql)

```sql
ALTER TABLE dbo.Staging_EmployeeData
ADD SubDepartment_Description VARCHAR(255) NULL;
```

| Property | Value |
|---|---|
| Column | `SubDepartment_Description` |
| Type | `VARCHAR(255) NULL` |
| Idempotent | Yes (`COL_LENGTH` guard) |
| Existing rows | Remain valid (`NULL`) |

`usp_HSE_SynchronizeMasters` continues to write `LEFT(description, 50)` into `MST_SubDepartment` (master description width), unchanged.

---

## 4. Frontend / mapping UI change

**ASPX:** No markup change required.

`HrmsDataImport` builds the mapping repeater from `ExpectedDbColumns`. Adding the column name automatically:

- shows `SubDepartment_Description` in the mapping UI
- enables exact-name auto-map (same as `Department_Description`, `Designation_Description`, etc.)
- requires no new controls

---

## 5. Code-behind change

**File:** `AnmolDristi/HrmsDataImport.aspx.cs`

`ExpectedDbColumns` now includes:

```text
"SubDepartment_ID", "SubDepartment_Description",
```

immediately after `SubDepartment_ID`, preserving the ID/Description pairing style used by other masters.

No other `ExpectedDbColumns` entries changed.

### CSV import impact

`btnProcess_Click` / `InsertBatch` already:

1. Create DataTable columns from `ExpectedDbColumns`
2. Map CSV headers by name (case-insensitive)
3. SqlBulkCopy map by column name to `dbo.Staging_EmployeeData`

With the staging column present and ExpectedDbColumns updated, mapped `SubDepartment_Description` values flow into staging with no special-case code.

---

## 6. Validation review (`usp_HSE_ValidateBatch`)

**Change required: yes.**

Previously SubDepartment used a hard-Error path because description could not be staged. After alignment, validation matches Department / Designation / Grade / etc.:

| Staging state | Result |
|---|---|
| Unknown `SubDepartment_ID` + empty `SubDepartment_Description` | Blocking `Error` |
| Unknown `SubDepartment_ID` + non-empty `SubDepartment_Description` | `[WARNING]` only; SynchronizeMasters can create |
| Known `SubDepartment_ID` | No SubDepartment master error |

Functional edit is confined to the SubDepartment master-reference block in `usp_HSE_ValidateBatch.sql`.

---

## 7. Synchronization review (`usp_HSE_SynchronizeMasters`)

**Functional change: none required.**

The procedure already:

1. Groups distinct `SubDepartment_ID` for `@BatchId`
2. Updates `MST_SubDepartment.SubDepartment_Description` when staging description differs
3. Inserts missing SubDepartment rows when description is non-empty

Only the header comment was refreshed to reflect schema alignment. After migration + upload mapping, SubDepartment sync behaves like the other masters.

---

## 8. Import pipeline impact

```text
CSV (SubDepartment_Description)
  → HrmsDataImport mapping (ExpectedDbColumns)
  → SqlBulkCopy → Staging_EmployeeData.SubDepartment_Description
  → usp_HSE_ValidateBatch (Error / WARNING rules)
  → usp_HSE_SynchronizeMasters → MST_SubDepartment
  → Insert/Update employees resolve SubDepartmentId ← MST_SubDepartment.Id
```

---

## 9. Backward compatibility

| Concern | Behaviour |
|---|---|
| Old staging rows | `SubDepartment_Description` is NULL — same as before for those batches |
| Unmapped description | Remains NULL; unresolved SubDepartment without description still Errors (safe) |
| Mapped description | New path: WARNING + SynchronizeMasters create/update |
| Other HSE children | Insert/Update/Hierarchy only use `SubDepartment_ID` / master Id — unaffected |
| Re-run migration | Idempotent PRINT, no failure |

---

## 10. Deployment order

1. **Deploy SQL migration** `006_Add_Staging_SubDepartment_Description.sql`  
   (must precede any upload that maps the new column)
2. **Deploy** updated `usp_HSE_ValidateBatch`  
3. **Deploy** app build with updated `HrmsDataImport.aspx.cs`  
4. `usp_HSE_SynchronizeMasters` comment-only refresh may deploy with other HRMS scripts (no behaviour dependency beyond step 1)

Recommended verification:

```sql
-- Column present
SELECT COL_LENGTH(N'dbo.Staging_EmployeeData', N'SubDepartment_Description');

-- After a mapped upload
SELECT TOP 20 SubDepartment_ID, SubDepartment_Description
FROM dbo.Staging_EmployeeData
WHERE BatchId = @BatchId;
```

Then run ValidateBatch → SynchronizeMasters (MASTER module) and confirm `MST_SubDepartment` insert/update counts for new codes with descriptions.

---

## 11. Files touched

| File | Change |
|---|---|
| `Database/Scripts/006_Add_Staging_SubDepartment_Description.sql` | **Added** — migration |
| `AnmolDristi/HrmsDataImport.aspx.cs` | ExpectedDbColumns + `SubDepartment_Description` |
| `Database/Stored Procedures/HRMS/usp_HSE_ValidateBatch.sql` | SubDepartment Error/WARNING parity |
| `Database/Stored Procedures/HRMS/usp_HSE_SynchronizeMasters.sql` | Comment clarification only |
| `docs/HRMS_Import_Module/22_SubDepartment_Schema_Alignment.md` | This document |
| `HrmsDataImport.aspx` | **No change** |
| Other HSE child procedures | **No change** |
| Orchestrator | **No change** |
