# 24 — Orchestrator Production Readiness Review

**Procedure under review:** `dbo.usp_RunHRMSSynchronization`  
**Script:** [`Database/Stored Procedures/HRMS/usp_RunHRMSSynchronization.sql`](../../Database/Stored%20Procedures/HRMS/usp_RunHRMSSynchronization.sql)  
**Mode:** Analysis only. **No SQL or C# was modified.**  
**UI consumer:** `HrmsBatchManager.btnRun_Click` → `SqlDataAdapter.Fill` → `gvResult` (first result set only).  
**Validation:** Performed by the UI via `usp_HSE_ValidateBatch` before this procedure is called (not inside the orchestrator).

---

## Executive summary

| Area | Assessment |
|---|---|
| Transaction + TRY/CATCH | Fundamentally sound; strengthen with `XACT_STATE()` |
| Child call order / module gates | Correct vs architecture |
| ProcessingMode gating | Correct; minor redundant IF |
| Result set contract | Compatible with BatchManager when children stay nested-silent |
| `SkippedCount` | **Semantically overloaded** — High |
| `NoChangeCount` | **Dropped** — never returned — Medium |
| Staging finalize vs `[WARNING]` | Warnings can block `Processed` — High |
| Validation inside SP | Intentionally absent — Medium (defense-in-depth gap) |

---

## 1. Transaction lifecycle

### Current behaviour

```text
BEGIN TRY
    BEGIN TRANSACTION
        Status = Processing
        EXEC children…
        Finalize staging Pending → Processed
        Status = Processed | Failed
    COMMIT
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK
    Best-effort Status = Failed (autocommit)
    RAISERROR / RETURN   ← no summary result set
END CATCH
SELECT summary…          ← success path only
```

- Single ambient transaction wraps all child work.  
- Children are nested-transaction-safe (`@@TRANCOUNT > 0` → join ambient).  
- Pre-TRY parameter/batch guards `RAISERROR` + `RETURN` **without** opening a transaction (correct).

### Recommended behaviour

Keep one transaction for the sync body. In `CATCH`, roll back using `XACT_STATE()` (see §3). Optionally return a failed summary RS instead of only raising (see §12 / §13).

### Reason

Current lifecycle matches the designed orchestrator model. Hard failures correctly undo child DML; status `Failed` is applied after rollback so it persists.

| Severity | Medium |
|---|---|
| Finding | Success commits once; failure rolls back then stamps `Failed`. No savepoints / partial commit of children (by design). |

---

## 2. TRY/CATCH

### Current behaviour

- Sync body is inside `TRY/CATCH`.  
- Outer guards are outside `TRY` (batch/modules/mode).  
- Inner `TRY/CATCH` around post-rollback `Status = Failed` swallows secondary errors.  
- Original error is re-raised via `RAISERROR(ERROR_MESSAGE(), …)`.

### Recommended behaviour

- Keep structure.  
- Prefer `THROW;` (SQL Server 2012+) to preserve original error number/line, **or** keep `RAISERROR` if callers depend on message-only.  
- Ensure `CATCH` always ends transaction cleanly via `XACT_STATE()` before re-throw.

### Reason

`RAISERROR` with only `ERROR_MESSAGE()` loses error number / procedure / line, which hurts production diagnostics.

| Severity | Medium |
|---|---|
| Finding | Error re-raise loses native error metadata. |

---

## 3. `XACT_STATE`

### Current behaviour

```sql
SET XACT_ABORT ON;
…
IF @@TRANCOUNT > 0
    ROLLBACK TRANSACTION;
```

`XACT_STATE()` is **never** inspected.

### Recommended behaviour

```sql
IF XACT_STATE() <> 0
    ROLLBACK TRANSACTION;
-- else: no open / doomed transaction to clear
```

### Reason

With `XACT_ABORT ON`, most runtime errors doom the transaction (`XACT_STATE() = -1`). Rolling back on `@@TRANCOUNT > 0` usually works, but `XACT_STATE()` is the supported pattern and avoids edge cases where `ROLLBACK` is inappropriate or `@@TRANCOUNT` alone is ambiguous with nested patterns.

| Severity | Medium |
|---|---|
| Finding | Missing `XACT_STATE()` check in `CATCH`. |

---

## 4. Output parameter initialization

### Current behaviour

Orchestrator exposes **no** stored-procedure `OUTPUT` parameters. Aggregates are locals:

| Local | Initialized |
|---|---|
| `@InsertedCount`, `@UpdatedCount`, `@SkippedCount`, `@ErrorCount` | `0` |
| `@MastersInserted`, `@MastersUpdated`, `@HierarchyUpdated` | `0` |
| `@Step*` temps | Reset to `0` immediately before each child `EXEC` |

Child `OUTPUT`s are received into `@Step*` then copied/accumulated with `ISNULL(..., 0)`.

### Recommended behaviour

Keep zero-initialization and per-step reset (already correct). Document that the public contract is the **result set**, not OUTPUT params.

### Reason

Prevents NULL propagation from children that leave OUTPUTs unset on early `RETURN`.

| Severity | Low |
|---|---|
| Finding | Initialization is adequate. |

---

## 5. Output parameter accumulation

### Current behaviour

| Metric | Accumulation |
|---|---|
| Masters | Replace (`=`), not sum (single master step) |
| Insert counts | `+=` Inserted / Skipped / Error |
| Update counts | `+=` Updated / Skipped / Error |
| Hierarchy | HierarchyUpdated `=`; Error `+=` |
| `NoChangeCount` | **Not read / not accumulated** |
| Hierarchy `SkippedCount` / `MissingManagerCount` | Only via `@ErrorCount` (missing manager); skipped/no-change discarded |

### Recommended behaviour

- Keep summing **Error** across steps if “any soft/hard child error” drives batch status.  
- **Do not** sum differently defined `SkippedCount` values into one column without renaming (see §14).  
- Surface `NoChangeCount` (from Update; optionally Hierarchy) as its own summary column if the UI should show it.

### Reason

Current `+=` for Skipped mixes incompatible meanings; silent drop of `NoChangeCount` hides “eligible but unchanged” volume.

| Severity | High (Skipped); Medium (NoChange) |
|---|---|
| Finding | Accumulation is mechanically fine but semantically mixed / incomplete. |

---

## 6. Execution order

### Current behaviour

Fixed order when gated on:

1. `usp_HSE_SynchronizeMasters`  
2. `usp_HSE_InsertNewEmployees`  
3. `usp_HSE_UpdateChangedEmployees`  
4. `usp_HSE_UpdateReportingHierarchy`  

### Recommended behaviour

Keep this order (masters before employee FK resolution; insert before update; hierarchy last so new employees exist).

### Reason

Matches approved HSE architecture and Designation/org Id resolution dependencies.

| Severity | — (OK) |
|---|---|
| Finding | Order is correct. |

---

## 7. Conditional module execution

### Current behaviour

`@Modules` normalized: uppercase, spaces removed; token match via `LIKE '%MASTER%'| '%EMPLOYEE%'| '%HIERARCHY%'`.

| Token present | Child |
|---|---|
| MASTER | SynchronizeMasters |
| EMPLOYEE | Insert (+ Update if mode 1/2) |
| HIERARCHY | UpdateReportingHierarchy |

Empty / unknown-only modules → `RAISERROR` before `TRY`.

### Recommended behaviour

Keep. Optionally reject unknown tokens (today `"FOO|MASTER"` still runs MASTER) — Low hardening.

### Reason

Substring `LIKE` is consistent with BatchManager’s pipe tokens; false positives are unlikely with current vocabulary.

| Severity | Low |
|---|---|
| Finding | Works; unknown tokens ignored rather than rejected. |

---

## 8. ProcessingMode handling

### Current behaviour

| Mode | Insert | Update |
|---|---|---|
| `0` | Yes (if EMPLOYEE) | No |
| `1` | Yes | Yes |
| `2` | Yes | Yes (`@OverwriteFields` passed through) |

Invalid mode → pre-TRY `RAISERROR`.

Redundant gate:

```sql
IF @RunEmployee = 1 AND @ProcessingMode IN (0, 1, 2)
BEGIN
    IF @ProcessingMode = 0 OR @ProcessingMode IN (1, 2)  -- always true here
```

`@AllowOverwrite` is forwarded only to Update (Insert ignores it) — consistent with UI deriving AllowOverwrite from mode ≠ 0.

### Recommended behaviour

Simplify Insert gate to `IF @RunEmployee = 1`. Keep Update on modes `1`/`2` only.

### Reason

Dead inner IF adds noise; behaviour is already correct.

| Severity | Low |
|---|---|
| Finding | Redundant nested IF for Insert. |

---

## 9. Validation call integration

### Current behaviour

- Orchestrator **does not** call `usp_HSE_ValidateBatch`.  
- Header documents UI-side validation (Phase 3.3).  
- Orchestrator only does lightweight BatchId / modules / mode guards.

### Recommended behaviour

**Option A (current architecture):** Keep validation in UI; document that any non-UI caller must validate first.  
**Option B (defense in depth):** Optionally call ValidateBatch at the start of the orchestrator and abort when `@IsValid = 0` (would be a future change — not done here).

### Reason

UI integration is intentional and working for BatchManager. Direct `EXEC usp_RunHRMSSynchronization` from SSMS/jobs can bypass validation.

| Severity | Medium |
|---|---|
| Finding | No in-proc validation; safe only if all callers enforce Phase 3.3. |

---

## 10. Summary result set

### Current behaviour (success path)

Single RS after `COMMIT`:

| Column | Source |
|---|---|
| `BatchId` | `@BatchId` |
| `TotalRecords` | Staging count (pre-tran, `NOLOCK`) |
| `InsertedCount` | Insert child |
| `UpdatedCount` | Update child |
| `SkippedCount` | Insert.Skipped **+** Update.Skipped |
| `ErrorCount` | Sum of child Error OUTPUTs |
| `MastersInserted` / `MastersUpdated` | Masters child |
| `HierarchyUpdated` | Hierarchy child |
| `Status` | `@BatchStatus` |
| `Message` | `'Synchronization completed.'` |

**Not returned:** `NoChangeCount`, hierarchy skipped / missing-manager detail (except inside `ErrorCount`), per-module breakdown.

### Recommended behaviour

Keep a **single** RS for `Fill` compatibility. Consider adding `NoChangeCount` (and optionally renaming Skipped — §14) in a versioned contract change coordinated with UI.

### Reason

BatchManager binds the first RS blindly; extra columns are generally safe, renames/removals are not.

| Severity | Medium |
|---|---|
| Finding | Contract is stable but incomplete for change-detection observability. |

---

## 11. `ImportBatchMaster` status transitions

### Current behaviour

| Point | Status |
|---|---|
| Enter sync TRY | `Processing` |
| Success, `ErrorCount = 0` | `Processed` |
| Success, `ErrorCount > 0` and any productive DML counts | `Processed` (partial) |
| Success, `ErrorCount > 0` and no productive DML | `Failed` |
| CATCH after rollback | `Failed` (best-effort) |
| Pre-TRY guard failure | **Unchanged** (no status write) |

There is no distinct `'Partial'` status value.

### Recommended behaviour

- Keep `Processing` → terminal status.  
- Either document that `Processed` includes partial success, **or** introduce `'Partial'` if operators need it (UI/filter impact).  
- Optionally set `Failed` on pre-TRY hard failures when BatchId exists.

### Reason

Partial success as `Processed` can hide row-level problems unless operators inspect staging / `ErrorCount` in the grid.

| Severity | Medium |
|---|---|
| Finding | Partial success collapsed into `Processed`. |

---

## 12. Error handling

### Current behaviour

| Failure class | Behaviour |
|---|---|
| Guard (bad BatchId/modules/mode) | `RAISERROR` + `RETURN`; no RS; status untouched |
| Child runtime error | Rollback all; status `Failed`; `RAISERROR`; no RS |
| Child soft counts (`ErrorCount` OUTPUT) | Commit; status Processed/Failed per §11; success RS |

### Recommended behaviour

- Keep hard errors as abort + rollback.  
- Align soft-error semantics: decide whether Update/Hierarchy “missing master/manager” should drive batch `Failed` or only inflate a `WarningCount`.  
- Consider always returning a summary RS on failure (Status/Message/ErrorCount) **in addition to** throw — only if UI is updated to read it; today UI uses `catch (Exception)`.

### Reason

Soft errors and hard errors both influence operator experience differently; current mixing of Missing* into `ErrorCount` blurs that.

| Severity | Medium |
|---|---|
| Finding | Soft child errors affect batch status via summed `ErrorCount`; hard errors yield exception with no RS. |

---

## 13. Result Set 1 contract with `HrmsBatchManager`

### Current behaviour

```text
btnRun_Click
  → ValidateBatch (separate command; may bind gvResult on failure)
  → usp_RunHRMSSynchronization
  → SqlDataAdapter.Fill(DataTable)   // first RS only
  → gvResult
```

Orchestrator emits **one** RS on success. Children are designed to suppress standalone RSs when nested (`@StartedTran = 0`), so they should not prepend rows before the summary.

Failure path: exception → `lblPageError.Text = ex.Message`; no sync summary bind.

### Recommended behaviour

- Keep single success RS.  
- Regression-test that no child emits a client RS under nested execution.  
- Do not add intermediate SELECTs to the orchestrator body.

### Reason

Any extra RS before the summary breaks `Fill` / grid semantics.

| Severity | High (if a child regresses); Low (current design) |
|---|---|
| Finding | Contract is correct **given** nested-silent children. |

---

## 14. `UpdatedCount` / `SkippedCount` / `NoChangeCount`

### Child semantics (source of truth)

| Child | `UpdatedCount` | `SkippedCount` | `NoChangeCount` |
|---|---|---|---|
| Insert | n/a | EmpCode **already in** `MST_UserMaster` | n/a |
| Update | Rows with ≥1 applied change | EmpCode **not in** `MST_UserMaster` (and mode-0 short-circuit counts existing as skipped) | Existing matches with **no differing** in-scope values |
| Hierarchy | (`HierarchyUpdatedCount`) | Exists separately on child; **not** read by orchestrator | Exists on child; **not** read |

### What the orchestrator shows

| Summary column | Meaning today |
|---|---|
| `UpdatedCount` | Update child row updates only — **semantically OK** |
| `SkippedCount` | Insert.Skipped **+** Update.Skipped — **mixed** |
| `NoChangeCount` | **Absent** |

### Is `SkippedCount` semantically correct?

**Not as a single business metric.**

Example (Mode `1`, EMPLOYEE module, 100 staging rows, 80 already in UserMaster, 20 new):

| Step | SkippedCount meaning | Typical value |
|---|---|---|
| Insert | “Already exists — not inserted” | 80 |
| Update | “Not in master — not updated” | 0 (after insert of 20) |
| Orchestrator sum | Shown as `SkippedCount` | **80** |

That `80` means “skipped insert because already present,” **not** “skipped update as unchanged.” Unchanged existing rows are `NoChangeCount` inside Update and **never appear** in the summary.

Another Mode `0` case: Insert skips 80 existing → summary `SkippedCount = 80`, `UpdatedCount = 0`. Operators may read “80 skipped” as failures; they are expected insert skips.

### Recommended behaviour

| Approach | Description |
|---|---|
| A (minimal) | Rename UI/summary clarity: e.g. expose `InsertSkippedCount` + `UpdateSkippedCount` separately; stop summing them |
| B | Map Insert.Skipped → separate column; keep `SkippedCount` = Update.Skipped only |
| C | Add `NoChangeCount` from Update (and optionally Hierarchy) to the summary RS |

### Reason

Summing differently defined skip counters produces a misleading KPI for BatchManager’s “Last RUN Result.”

| Severity | **High** |
|---|---|
| Finding | `SkippedCount` is not a single coherent semantic; `NoChangeCount` is omitted. |

---

## Issue register (severity-ordered)

| ID | Severity | Topic | Current behaviour | Recommended behaviour | Reason |
|---|---|---|---|---|---|
| ORCH-01 | **High** | `SkippedCount` semantics | Sum of Insert.Skipped + Update.Skipped | Split or redefine; do not sum incompatible meanings | Misleading sync KPI |
| ORCH-02 | **High** | Staging finalize vs warnings | `Pending` + non-empty `ErrorMessage` (incl. `[WARNING]`) never → `Processed` | Finalize `Pending` rows that only contain warnings, **or** clear warnings after successful sync; keep true `Error` rows | Warning-only rows stay Pending forever |
| ORCH-03 | Medium | `XACT_STATE` | Rollback on `@@TRANCOUNT > 0` only | Use `IF XACT_STATE() <> 0 ROLLBACK` | Standard doomed-transaction handling |
| ORCH-04 | Medium | `NoChangeCount` | Dropped | Add to summary RS (versioned) | Observability for mode 1/2 |
| ORCH-05 | Medium | Soft errors → `ErrorCount` | Missing master/manager counts fail/partial batch status | Distinguish WarningCount vs ErrorCount in summary/status rules | Avoid Failed batches for soft skips |
| ORCH-06 | Medium | Validation not in SP | UI-only ValidateBatch | Document caller contract; optional in-proc gate later | Defense in depth for non-UI EXEC |
| ORCH-07 | Medium | Failure return shape | CATCH: exception, no RS | Keep throw for UI **or** add failed summary RS if UI taught to read it | Consistent operator feedback |
| ORCH-08 | Medium | Partial → `Processed` | Partial success uses `Processed` | Document or add `Partial` status | Operator clarity |
| ORCH-09 | Medium | `RAISERROR` vs `THROW` | Message-only re-raise | Prefer `THROW` | Preserve error metadata |
| ORCH-10 | Low | Redundant Insert IF | Nested mode check always true | `IF @RunEmployee = 1` | Clarity |
| ORCH-11 | Low | Module token parsing | Unknown tokens ignored | Optionally reject unknowns | Stricter API |
| ORCH-12 | Low | OUTPUT init | Locals zeroed; steps reset | Keep | Already correct |

---

## Result Set 1 — BatchManager compatibility checklist

| Check | Status |
|---|---|
| Exactly one RS on success | Yes |
| Columns include historic Inserted/Updated/Skipped/Error | Yes |
| Extended master/hierarchy columns present | Yes (additive) |
| Children silent when nested | Designed yes — protect with tests |
| Failure returns RS | No (exception) — UI uses catch |

---

## Recommendations summary (no code changes in this review)

1. **Treat ORCH-01 / ORCH-02 as the top production follow-ups** before calling the orchestrator “KPI-accurate.”  
2. Keep transaction scope and child order as-is.  
3. Harden `CATCH` with `XACT_STATE()` + prefer `THROW`.  
4. Decide product meaning of soft Missing* counts vs batch `Failed`.  
5. Run a nested-EXEC smoke test ensuring `Fill` receives only the summary RS.  
6. Do **not** change C#/SQL in this review artifact; implement fixes in a dedicated change set after product sign-off on skip/warning semantics.

---

## Final determination

| Question | Answer |
|---|---|
| Is the orchestrator structurally production-viable? | **Yes**, with known KPI/status caveats |
| Is `UpdatedCount` trustworthy? | **Yes** (update-applied rows) |
| Is `SkippedCount` semantically correct? | **No** — overloaded sum |
| Is `NoChangeCount` available to the UI? | **No** — not in summary |
| Must SQL be changed before go-live? | **Not blocking** for basic sync; **recommended** before trusting skip/warning reporting |

**Bottom line:** Coordination, transaction boundaries, module/mode gating, and the success result-set shape are sound. The highest-priority correctness issues are **SkippedCount semantics** and **staging finalization leaving warning rows Pending**, not the thin-orchestrator architecture itself.
