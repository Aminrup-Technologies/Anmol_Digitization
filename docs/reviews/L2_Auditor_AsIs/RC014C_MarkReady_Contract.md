# RC-014C — `usp_MarkRequestReadyForPayment` Guard Failure (Read-Only)

**Date:** 2026-09-15  
**Scope:** Prove why MarkReady does not persist on a valid `Completed` request. No code or SQL changes.  
**SQL:** MCP `anmol_uat` → `AnmolDristiDev` (`connectionName=anmol_uat`)  
**Repo:** `Database/Stored Procedures/Approval/usp_MarkRequestReadyForPayment.sql`, `AnmolDristi/ClaimsApproval_v2.aspx.cs`  
**Claim:** Request **95** (Pankaj Mishra / `AHS1744`)

**Overall verdict: FAIL**

The procedure does **not** fail the Auditor identity guard or the header `Completed` guard. Those conditions are satisfied for request 95 with `@ActionBy = N'Audit'`.

The call dies on the **history INSERT**: a positional `INSERT … VALUES` with **6** expressions into `dbo.ApprovalRequestStatusHistory`, which now has **12** columns. SQL Server error:

`Column name or number of supplied values does not match table definition.`

Header remains `Completed`. There is still no `Completed → ReadyForPayment` history row. `usp_ProcessApprovalAction` is not implicated.

---

## Sources

| Source | What it proved |
| --- | --- |
| MCP `get_stored_procedure_definition` | Live SP body matches the repo file. `modify_date` **2026-01-09**. |
| MCP `describe_table` `ApprovalRequestStatusHistory` | **12** columns. `HistoryId` IDENTITY. Table `MODIFIED_DATE` **2026-07-29**. |
| MCP `list_triggers` | **No** triggers on `SalesApprovalRequest` or `ApprovalRequestStatusHistory`. |
| MCP `execute_query` | Disabled (`MSSQL_ENABLE_EXECUTE_QUERY` not set). |
| RC-014B parameterized `SELECT` on the same UAT instance | Request 95 header / auditor / history values used in the matrix below. |
| Direct `EXEC dbo.usp_MarkRequestReadyForPayment` as `Audit` (RC-014B UAT) | Exception above. Header still `Completed` afterwards. |

Live definition (MCP `OBJECT_DEFINITION()`, retrieved 2026-09-15T13:04:36Z):

```sql
CREATE   PROCEDURE dbo.usp_MarkRequestReadyForPayment
(
    @ApprovalRequestId INT,
    @ActionBy NVARCHAR(100)
)
AS
BEGIN
    SET NOCOUNT ON;

    /* 1 Auditor validation */
    IF NOT EXISTS (
        SELECT 1
        FROM SalesApprovalRequestApprover
        WHERE ApprovalRequestId = @ApprovalRequestId
          AND ApproverLoginId = @ActionBy
          AND ApproverRole = 'Auditor'
    )
    BEGIN
        RAISERROR('Only Auditor can mark Ready for Payment.',16,1);
        RETURN;
    END

    /* 2 Must be Completed */
    IF NOT EXISTS (
        SELECT 1
        FROM SalesApprovalRequest
        WHERE ApprovalRequestId = @ApprovalRequestId
          AND Status = 'Completed'
    )
    BEGIN
        RAISERROR('Request not completed.',16,1);
        RETURN;
    END

    /* 3 Update header */
    UPDATE SalesApprovalRequest
    SET
        Status = 'ReadyForPayment',
        ModifiedBy = @ActionBy,
        ModifiedOn = SYSUTCDATETIME()
    WHERE ApprovalRequestId = @ApprovalRequestId;

    INSERT INTO ApprovalRequestStatusHistory
    VALUES
    (
        @ApprovalRequestId,'Completed','ReadyForPayment',
        @ActionBy,SYSUTCDATETIME(),
        'Auditor marked Ready for Payment'
    );
END
```

Contrast: `usp_ProcessApprovalAction` writes history with an **explicit column list** (`ApprovalRequestId, OldStatus, NewStatus, ChangedBy, Remarks`). That insert succeeded for request 95 (`HistoryId` 686).

---

## Call stack

### UI path (button — not reached on 95 after last approve)

1. `ClaimsApproval_v2.aspx` `btnMarkReadyForPayment`  
   Text `✔ Ready for Payment`. `Visible="false"` until bind.  
   `OnClientClick="return showApprovalSummaryConfirm('payment');"`  
   `OnClick="btnMarkReadyForPayment_Click"`
2. Client confirm → `__doPostBack(btnMarkReadyForPayment.UniqueName, '')`
3. `btnMarkReadyForPayment_Click` (`ClaimsApproval_v2.aspx.cs` ~2143)
4. `reqId = Convert.ToInt32(hfActionRequestId.Value)`  
   Set in `ShowDetailsModal` from the View command argument.
5. `auditor = GetLoggedInEmployeeCode()`  
   `return (Session["WORKMAN"] ?? Session["workman"]) as string;`
6. `IsCurrentUserAuditor()`  
   `ViewState["CurrentUserRole"] == "Auditor"` (set by `SetCurrentUserApproverDetails`)
7. `EnsureCanFinalizeRequest(reqId)` — C# only; not inside the SP
8. `ExecuteRecalculationSQL(reqId)` — C# only
9. `SqlCommand("dbo.usp_MarkRequestReadyForPayment")`  
   `@ApprovalRequestId = reqId`  
   `@ActionBy = auditor`

On this UAT pass the Ready button was **never clicked**. After last Auditor approve, `usp_GetPendingApprovalsForUser_Filters` dropped request 95 (Completed is not in that queue), and the bulk postback timed out so the modal never rebound `btnMarkReadyForPayment.Visible`.

Independent C# defect on the live handler (not executed on 95, still blocking if the button is clicked):

```2143:2164:AnmolDristi/ClaimsApproval_v2.aspx.cs
        protected void btnMarkReadyForPayment_Click(object sender, EventArgs e)
        {
            try
            {
                int reqId = Convert.ToInt32(hfActionRequestId.Value);
                string auditor = GetLoggedInEmployeeCode();
                // ...
                using (var con = new SqlConnection(connString))
                {
                    using (var cmd = new SqlCommand("dbo.usp_MarkRequestReadyForPayment", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ApprovalRequestId", reqId);
                        cmd.Parameters.AddWithValue("@ActionBy", auditor);
                        cmd.ExecuteNonQuery();
```

`con.Open()` is commented out with the old transaction block (`//con.Open();` at line 2166). v1 `ClaimsApproval.aspx.cs` **does** call `con.Open()`. A v2 button click would throw `ExecuteNonQuery requires an open and available Connection` **before** the SP runs. That is a separate UI-path failure, not the exception observed on the direct `EXEC`.

### Direct SP path (what actually ran)

```
EXEC dbo.usp_MarkRequestReadyForPayment
     @ApprovalRequestId = 95,
     @ActionBy          = N'Audit';
```

That is the same parameter contract the button would send while impersonating Rohan Saraf (`Session["WORKMAN"] = 'Audit'`).

---

## C# identity (no assumptions)

| Item | Source | Value on this UAT |
| --- | --- | --- |
| RequestId | `hfActionRequestId` / `@ApprovalRequestId` | **95** (modal `Request #95`; EXEC used 95) |
| EmployeeCode / ActionBy | `GetLoggedInEmployeeCode()` → `Session["WORKMAN"]` | **`Audit`** (banner: Impersonating Rohan Saraf (`Audit`) \| Original `AHO445`) |
| Auditor identity (C#) | `IsCurrentUserAuditor()` → `ViewState["CurrentUserRole"]` | Role loaded from `SalesApprovalRequestApprover` where `ApproverLoginId = @User`. Fallback includes `ApproverRole = 'Auditor' AND r.Status = 'Completed'` (`SetCurrentUserApproverDetails`). |
| Auditor identity (SP) | `ApproverLoginId = @ActionBy AND ApproverRole = 'Auditor'` | `@ActionBy = N'Audit'`. Does **not** read `CurrentApproverLoginId` (NULL after RC-014A terminal write). |

`EnsureCanFinalizeRequest` counts latest details whose status is not `Approved`/`Rejected`, plus non-terminal expenses. After RC-014B last approve, all 31 latest details were `Approved` at level 2. That C# gate would **PASS** if the button path ran. It is not a stored-procedure guard.

---

## SQL guard table (execution order)

SP has **no** `BEGIN TRAN`, **no** `SET XACT_ABORT ON`, **no** `CurrentApproverLoginId` check, **no** trigger.

| # | Step | SQL condition | Expected for a valid call | Request #95 actual | Pass / Fail |
| --- | --- | --- | --- | --- | --- |
| 1 | Auditor identity / approver-row lookup | `EXISTS` on `SalesApprovalRequestApprover` where `ApprovalRequestId = @Id` AND `ApproverLoginId = @ActionBy` AND `ApproverRole = 'Auditor'` | One Auditor row for login `Audit` | Auditor row present. Pre-complete header `CurrentApproverLoginId = Audit`. After complete: `ApproverRole=Auditor`, `Status=Approved`, `ActionBy=Audit`. Direct EXEC did **not** raise `Only Auditor can mark Ready for Payment.` | **PASS** (data). Not the failing step. |
| 2 | Current-user / routing pointer | *Not present in the SP.* Header `CurrentApproverLoginId` / `CurrentApproverLevel` are not read. | N/A | Both **NULL** (cleared by RC-014A terminal branch) | **N/A** — cannot fail this SP |
| 3 | Header Status | `EXISTS` header `Status = 'Completed'` | `Completed` | `Status = Completed` | **PASS**. Not the failing step. Direct EXEC did **not** raise `Request not completed.` |
| 4 | Header update | `UPDATE SalesApprovalRequest SET Status='ReadyForPayment', ModifiedBy=@ActionBy, ModifiedOn=SYSUTCDATETIME()` | Status becomes `ReadyForPayment` | After failed EXEC: still `Completed`, `ModifiedOn` still the ProcessApprovalAction timestamp `2026-09-15 12:48:19` | **Did not persist.** Reached only if the module compiled past this statement. Post-state proves it did not commit. |
| 5 | History insert | `INSERT INTO ApprovalRequestStatusHistory VALUES ( @Id, 'Completed', 'ReadyForPayment', @ActionBy, SYSUTCDATETIME(), 'Auditor marked Ready for Payment' )` — **6** values, **no column list** | Named-column insert of `Completed → ReadyForPayment` | Live table has **12** columns (see mapping below). EXEC exception: column count mismatch. No `Completed → ReadyForPayment` row. | **FAIL — exact failing statement** |
| 6 | Transaction / rollback | None declared. `SET NOCOUNT ON` only. No triggers. | Autocommit per statement unless compile aborts the batch | Header still `Completed` after the exception → the `UPDATE` did not remain committed. Consistent with **module compile abort** on the `INSERT` (table exists, so column-count is bound at compile) so **no statement in the procedure executed**. | Compile abort; no committed MarkReady work |

### Positional `VALUES` mapping vs live table

`HistoryId` is IDENTITY. SQL Server still requires the `VALUES` list to match **all** columns when no column list is given, except that identity is not supplied as a value — the supplied count must equal the non-identity column count.

| Ordinal | Column | IDENTITY / null | Positional value supplied |
| --- | --- | --- | --- |
| 1 | `HistoryId` | IDENTITY | (skipped if count matches non-identity columns) |
| 2 | `ApprovalRequestId` | NOT NULL | `@ApprovalRequestId` |
| 3 | `OldStatus` | NULL | `'Completed'` |
| 4 | `NewStatus` | NOT NULL | `'ReadyForPayment'` |
| 5 | `ChangedBy` | NULL | `@ActionBy` |
| 6 | `ChangedOn` | NOT NULL, default `sysutcdatetime()` | `SYSUTCDATETIME()` |
| 7 | `Remarks` | NULL | `'Auditor marked Ready for Payment'` |
| 8 | `ApprovalRequestDetailId` | NULL | **missing** |
| 9 | `ActivityId` | NULL | **missing** |
| 10 | `ApproverLevel` | NULL | **missing** |
| 11 | `ActionType` | NULL | **missing** |
| 12 | `DetailVersion` | NULL | **missing** |

Non-identity columns: **11**. Values supplied: **6**. Error 213.

Schema drift: history table last modified **2026-07-29**; MarkReady procedure last modified **2026-01-09**. The extra five columns were added after this procedure was last deployed. The original seven-column table (`HistoryId` + six payload columns) would have accepted this `VALUES` list.

---

## Request #95 evidence matrix

Captured after RC-014B last Auditor approve, and again after the failed MarkReady `EXEC`. MCP `execute_query` was disabled; values are from parameterized `SELECT` on `anmol_uat` / `AnmolDristiDev`.

### Header `SalesApprovalRequest`

| Column | After last Auditor approve | After failed MarkReady EXEC |
| --- | --- | --- |
| `ApprovalRequestId` | 95 | 95 |
| `Status` | `Completed` | `Completed` |
| `CurrentApproverLevel` | NULL | NULL |
| `CurrentApproverLoginId` | NULL | NULL |
| `ModifiedBy` | `Audit` | `Audit` |
| `ModifiedOn` | 2026-09-15 12:48:19 | 2026-09-15 12:48:19 (unchanged) |

### Auditor row `SalesApprovalRequestApprover`

| Column | Value |
| --- | --- |
| `ApproverRole` | `Auditor` |
| `Status` | `Approved` |
| `ActionBy` | `Audit` |

While the request was still Pending at Auditor, header `CurrentApproverLoginId` was `Audit`. The SP lookup is `ApproverLoginId = @ActionBy`, not the header routing pointer.

### History `ApprovalRequestStatusHistory`

| HistoryId | OldStatus | NewStatus | ChangedBy | Remarks |
| --- | --- | --- | --- | --- |
| 686 | Pending | Completed | Audit | Final Auditor Approval |

Count of `Pending → Completed`: **1**. Count of `Completed → ReadyForPayment`: **0**.

### Direct EXEC

| Item | Value |
| --- | --- |
| `@ApprovalRequestId` | 95 |
| `@ActionBy` | `Audit` |
| Result | SqlException: `Column name or number of supplied values does not match table definition.` |
| Messages that did **not** appear | `Only Auditor can mark Ready for Payment.` / `Request not completed.` |

---

## Exact failing guard

**History insert (step 5)** — positional `INSERT INTO ApprovalRequestStatusHistory VALUES (…6 expressions…)` against a 12-column table.

Not:

- Header `Status = 'Completed'` (PASS)
- Auditor `ApproverLoginId = @ActionBy AND ApproverRole = 'Auditor'` (PASS)
- NULL `CurrentApproverLoginId` (not checked)
- A trigger (none)
- An explicit rollback (none declared; compile abort prevented the `UPDATE` from committing)

---

## PASS / FAIL verdict

| Contract | Verdict |
| --- | --- |
| RC-014A/B left request 95 in `Completed` so MarkReady is eligible | **PASS** |
| Auditor `@ActionBy = 'Audit'` matches the Auditor approver row | **PASS** |
| `usp_MarkRequestReadyForPayment` persists `ReadyForPayment` + history | **FAIL** |
| Failing statement | Positional history `INSERT` (6 values / 12 columns) |
| C# Ready button on 95 after last approve | **Not exercised** (queue SP omits `Completed`; bulk postback timed out) |
| C# `btnMarkReadyForPayment_Click` live path | **Would fail independently** — `con.Open()` is commented out |

No implementation change in this pass. Fix belongs in a later RC: named-column history insert matching `usp_ProcessApprovalAction`, and restore `con.Open()` on the v2 handler before any UI retest.
