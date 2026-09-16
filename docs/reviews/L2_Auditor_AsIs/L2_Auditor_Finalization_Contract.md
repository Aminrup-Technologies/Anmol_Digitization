# L2 Auditor Finalization Contract (Read-Only Proof)

**Phase:** RC-014 Phase 2  
**Captured:** 2026-09-15  
**Sources:** repository + SQL MCP `anmol_uat` → `AnmolDristiDev`  
**Constraint:** definitions/catalog only. No DML. No repository or SQL changes.

**Verdict: FAIL**

The live Auditor path never writes `SalesApprovalRequest.Status = 'Completed'`. `usp_MarkRequestReadyForPayment` requires that value. No live v2 caller patches the header afterward.

---

## 1. Required answers

### 1. Who writes `SalesApprovalRequest.Status='Completed'`?

On the **live Auditor (v2) path: nobody.**

The only UAT procedure that assigns `Status = 'Completed'` *on this workflow* is `dbo.usp_ProcessApprovalAction` (modify_date **2026-08-01**), and only in this branch:

```
all latest days resolved
AND no next required Pending approver
AND ISNULL(@CurrentApproverRole, '') <> 'Auditor'
```

When `@CurrentApproverRole = 'Auditor'`, the same procedure writes `Status = 'Pending'` instead (comment: remain Pending until `usp_MarkRequestReadyForPayment`).

Off-path writers exist and are **not** invoked by `ClaimsApproval_v2`:

| Writer | Live on Auditor v2? | Evidence |
| --- | --- | --- |
| `usp_ProcessApprovalAction` non-Auditor ELSE IF | No (role is Auditor) | UAT body |
| `usp_MarkRequestReadyForPayment` | No (reads Completed; writes ReadyForPayment) | UAT body |
| `usp_ForwardApprovalRequest` | No (refuses Auditor; never writes Completed) | UAT body THROW 51012 / 51014 |
| `ExecuteRecalculationSQL` | No (Approved\* amounts only) | `ClaimsApproval_v2.aspx.cs` 2757–2804 |
| `btnMarkReadyForPayment_Click` | No (calls MarkReady only) | aspx `OnClick="btnMarkReadyForPayment_Click"` |
| `btnMarkReadyForPayment_Click_OLD` | **Not wired** | method exists; aspx does not bind it. This OLD method *would* `UPDATE ... Status='Completed'` then call MarkReady. |
| `btnForwardLevel_Click_OLD` | **Not wired** | aspx binds `btnForwardLevel_Click`. OLD ELSE writes Completed when no next level. |
| `usp_ProcessApprovalRequestDetails_WithRemarks` | Legacy `ClaimsApproval.aspx.cs` only | UAT + repo; not referenced by v2 |
| `usp_RollupSalesApprovalRequestAmounts` | **No C# caller** in the repository | Repo grep: zero `.cs` hits. UAT SP can set Status Completed if invoked. |
| HRMS/staging SPs matching `'Completed'` | Unrelated modules | `usp_ProcessEmployeeBatch_V2/V3`, `usp_Run_Process_StagingToFinal_WithAudit` |

### 2. Exact gap

Two contracts were stacked without a connecting write:

1. Last Auditor approve (`usp_ProcessApprovalAction` ELSE Auditor branch) → header stays **`Pending`**.
2. Finalize (`usp_MarkRequestReadyForPayment` guard 2) → header must already be **`Completed`**.

Nothing on the wired v2 click path performs Pending → Completed for Auditor.

A second gap sits in the UI identity lookup (see §5): last Auditor approve also sets the Auditor **approver row** to `Approved` before leaving the header Pending. `SetCurrentUserApproverDetails` then finds no `Status='Pending'` row for the user, so `IsCurrentUserAuditor()` becomes false and **Ready for Payment is hidden**.

### 3. Can ReadyForPayment succeed immediately after the last Auditor approval?

**No.**

Even if the button were clicked, UAT `usp_MarkRequestReadyForPayment` executes:

```
IF NOT EXISTS (SELECT 1 FROM SalesApprovalRequest
               WHERE ApprovalRequestId = @ApprovalRequestId
                 AND Status = 'Completed')
    RAISERROR('Request not completed.', 16, 1);
    RETURN;
```

Immediate post-state of last Auditor approve is header `Pending`, so the SP returns without writing `ReadyForPayment`.

### 4. Hidden caller that patches the header later?

**No live hidden patcher on ClaimsApproval_v2.**

Post-approve refresh is `ExecuteRecalculationSQL` then `BindDetailsGrid` / `btnRefreshCalculations_Click`. Recalc updates snapshot and `Approved*` columns only. It does not set `Status`.

`usp_RollupSalesApprovalRequestAmounts` could patch Status to Completed, but it is not called from application code.

The only C# that *did* patch Pending → Completed before MarkReady is `btnMarkReadyForPayment_Click_OLD`, which is dead code.

### 5. Does `ApprovalRequestStatusHistory` reflect the transition?

**No terminal history is written on last Auditor approve.**

UAT `usp_ProcessApprovalAction` section 7 inserts history only when `@OldRequestStatus <> @NewRequestStatus`, except it **suppresses** APPROVE when both are Pending:

```
IF Old <> New
   AND NOT (APPROVE AND New='Pending' AND Old='Pending')
```

Last Auditor approve: Old=`Pending`, New=`Pending` → **no row**.

The Auditor ELSE branch itself has **no** `INSERT INTO ApprovalRequestStatusHistory`.

`usp_MarkRequestReadyForPayment` would insert `Completed` → `ReadyForPayment` with remark `'Auditor marked Ready for Payment'` **if** it ran. It does not run successfully after last Auditor approve.

---

## 2. Call graph (wired Auditor finalize path)

```
ClaimsApproval_v2.aspx
  btnApproveRow / ActionDayRowSilent('APPROVE') / btnApproveSelected
       └─ ProcessDayDetailAction(Approve)
            └─ ExecuteProcessApprovalAction
                 └─ dbo.usp_ProcessApprovalAction @Action='APPROVE'
                      ├─ detail Approved
                      ├─ if last latest day:
                      │     approver row → Approved
                      │     no next → role Auditor → header Status = Pending
                      │     no history insert
                      └─ header Approved* totals
       └─ ExecuteRecalculationSQL          ← amounts only
       └─ BindDetailsGrid / btnRefreshCalculations_Click
            └─ SetCurrentUserApproverDetails
                 SELECT ... ApproverLoginId=@User AND Status='Pending'
                 → 0 rows after last approve
                 → CurrentUserRole = ""
                 → btnMarkReadyForPayment.Visible = false

  btnMarkReadyForPayment  (OnClick = btnMarkReadyForPayment_Click, not _OLD)
       ├─ IsCurrentUserAuditor()           ← false after last approve rebind
       ├─ EnsureCanFinalizeRequest
       ├─ ExecuteRecalculationSQL
       └─ dbo.usp_MarkRequestReadyForPayment
            ├─ must be ApproverRole='Auditor' (row still exists; Status may be Approved)
            ├─ must be header Status='Completed'   ← FAIL
            └─ else Status='ReadyForPayment' + history

  btnForwardLevel (hidden for Auditor)
       └─ usp_ForwardApprovalRequest
            └─ Auditor → THROW 51012
```

Callers of `usp_ProcessApprovalAction` in C#:

| Site | File | Live? |
| --- | --- | --- |
| `ExecuteProcessApprovalAction` | `ClaimsApproval_v2.aspx.cs` 2013 | Yes — Approve/Return |
| `ProcessDayDetailAction` | same 1980 / 1984 | Yes |
| `ActionDayRowSilent` | same 2897 / 2902 | Yes |
| `gvDetails_RowCommand_OLD*` | same ~1553 | Unused methods |

Callers of `usp_MarkRequestReadyForPayment`:

| Site | Live? |
| --- | --- |
| `btnMarkReadyForPayment_Click` (`ClaimsApproval_v2.aspx.cs` 2158) | Yes (wired) |
| `btnMarkReadyForPayment_Click_OLD` (2120) | No |
| `ClaimsApproval.aspx.cs` 2637 (legacy v1) | Different page |

`ReadyForPayment` string uses: counters, badge CSS, `ClaimStatuses.ReadyForPayment`, MarkReady SP write, export/list filters. Display-only except MarkReady.

---

## 3. SQL evidence (UAT)

### 3.1 `usp_ProcessApprovalAction`

| | |
| --- | --- |
| modify_date | 2026-08-01 10:21:05 UTC |
| Parameters | `@ApprovalRequestDetailId INT`, `@Action NVARCHAR(20)`, `@ActionBy NVARCHAR(100)`, `@Remarks NVARCHAR(MAX)=NULL` |

**Header updates (APPROVE, last latest day, role = Auditor):**

```
UPDATE SalesApprovalRequest
SET Status = 'Pending', ModifiedBy = @ActionBy, ModifiedOn = @NowUtc
```

Pointer `CurrentApproverLoginId` / `CurrentApproverLevel` **not** cleared.

**Approver updates (all latest days resolved, before next/Auditor split):**

```
UPDATE SalesApprovalRequestApprover
SET Status = 'Approved', ActionBy, ActionDate, Remarks
WHERE ApproverLevel = @CurrentApproverLevel AND Status = 'Pending'
```

This runs for Auditor last-day as well.

**History:** none on Auditor last-day (Pending → Pending suppressed). Non-Auditor completion inserts Old Pending / New Completed, remark `'All approval levels finished. Awaiting payment processing.'`

**ReadyForPayment:** not written. Comment defers to MarkReady.

Repo file `Database/Stored Procedures/Approval/usp_ProcessApprovalAction.sql` contains the same Auditor-remain-Pending ELSE (lines 429–439) plus extra auth/role guards not present on UAT 2026-08-01.

### 3.2 `usp_MarkRequestReadyForPayment`

| | |
| --- | --- |
| modify_date | 2026-01-09 17:48:18 UTC |
| Parameters | `@ApprovalRequestId INT`, `@ActionBy NVARCHAR(100)` |

Guards:

1. A row exists on `SalesApprovalRequestApprover` with `ApproverLoginId = @ActionBy` AND `ApproverRole = 'Auditor'` (Status of that row is **not** checked).
2. Header `Status = 'Completed'`.

Writes:

- Header `Status = 'ReadyForPayment'`
- History positional INSERT: Old `Completed`, New `ReadyForPayment`, remark `'Auditor marked Ready for Payment'`
- **No** approver-row update

Repo file matches UAT.

### 3.3 `usp_ForwardApprovalRequest`

| | |
| --- | --- |
| modify_date | 2026-09-15 12:03:54 UTC |
| Parameters | `@ApprovalRequestId INT`, `@ActionBy NVARCHAR(100)` |

Auditor: `THROW 51012`. No next approver: `THROW 51014`. Explicitly does not write `Approved` or `ReadyForPayment`. Header Status remains Pending on success (L1 handover only).

---

## 4. Scenario trace (two remaining days, receipts already resolved)

Assume header `Pending`, `CurrentApproverRole = Auditor`, both latest details Pending, Auditor approver row Pending.

| Step | Header `Status` | Auditor approver `Status` | `CurrentApproverLoginId` | History row | Ready for Payment button | MarkReady if invoked |
| --- | --- | --- | --- | --- | --- | --- |
| Start | Pending | Pending | Auditor | (prior Forward: Pending→Pending) | Hidden (`pendingActionExists`) | n/a |
| Approve second-last day | Pending | Pending | Auditor | **None** (Pending→Pending skip) | Hidden | n/a |
| Approve last day | **Pending** | **Approved** | Auditor (unchanged) | **None** | **Hidden** (`SetCurrentUserApproverDetails` misses Pending role row → `IsCurrentUserAuditor()=false`) | `RAISERROR('Request not completed.')` |
| Intended finalize | would need Completed then ReadyForPayment | already Approved | — | would need Completed→ReadyForPayment | Visible only if `isAuditor && canFinalize` | cannot succeed |

Queue effect after last day: `usp_GetPendingApprovalsForUser_Filters` requires `saa.Status = 'Pending'` **and** `sar.Status = 'Pending'`. Approver row is now Approved, so the request **leaves the Auditor queue** while the header remains Pending with a stale current-approver pointer. Orphaned terminal state.

---

## 5. State transition table (Auditor terminal)

| Object | Before last day | After last Auditor APPROVE | After successful MarkReady (cannot reach) |
| --- | --- | --- | --- |
| `SalesApprovalRequest.Status` | Pending | Pending | ReadyForPayment |
| `SalesApprovalRequestApprover` (Auditor) | Pending | Approved | Approved (unchanged by MarkReady) |
| `SalesApprovalRequestDetail` (that day) | Pending | Approved | Approved |
| `ApprovalRequestStatusHistory` | no new row | no new row | Completed → ReadyForPayment |
| Queue membership | In Auditor queue | Out of queue | Out of queue |

---

## 6. Ownership matrix

| Write | Owner on Auditor last-approve path | Owner on ReadyForPayment path |
| --- | --- | --- |
| Header Status = Pending | `usp_ProcessApprovalAction` Auditor ELSE | — |
| Header Status = Completed | **unowned for Auditor** | **required input**, not produced |
| Header Status = ReadyForPayment | — | `usp_MarkRequestReadyForPayment` (unreachable) |
| Approver Status = Approved | `usp_ProcessApprovalAction` (all-days-resolved block) | none |
| History terminal row | **unowned** | MarkReady (unreachable) |
| Approved amounts | ProcessApprovalAction + `ExecuteRecalculationSQL` | Recalc again, then MarkReady (no amount writes) |
| Ready button visibility | `BindDetailsGrid`: `isAuditorCheck && canFinalize` | same; identity already lost |

---

## 7. PASS / FAIL

| Check | Result |
| --- | --- |
| Last Auditor day leaves a defined terminal header status that MarkReady accepts | **FAIL** (`Pending` vs required `Completed`) |
| ReadyForPayment can succeed immediately after last Auditor approval | **FAIL** |
| Wired v2 caller patches Completed later | **FAIL** (none) |
| History records last-approve or finalize | **FAIL** (suppressed Pending→Pending; MarkReady never inserts) |
| Ready for Payment button available after last approve | **FAIL** (Auditor Pending approver row consumed) |
| Forward is not an Auditor escape hatch | **PASS** (THROW 51012) |

**Contract verdict: FAIL.**

The intended chain in comments is:

`Auditor last approve → (header Completed?) → usp_MarkRequestReadyForPayment → ReadyForPayment`

The implemented chain is:

`Auditor last approve → header Pending + approver Approved + no history → MarkReady refuses → button/role lookup also fails`

No fix is specified in this file.
