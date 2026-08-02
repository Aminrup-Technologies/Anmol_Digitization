# Production Hardening Review — Monthly Sales Claims Workflow

**Type:** Analysis pass only. No code changed. No redesign proposed.
**Architecture assumed unchanged:** ASP.NET WebForms → ADO.NET → Stored Procedures → SQL Server.
**Date of review:** 2 Aug 2026
**Branch reviewed:** `July26_AILWorks_AminrupSupport` @ `bf99fd0`

---

## 1. Scope and method

Every file that participates in the Monthly Sales Claims workflow was read:

| Layer | Files reviewed |
| --- | --- |
| Employee entry / submit / resubmit | `AnmolDristi/SalesMonthlyReview.aspx` + `.aspx.cs` (≈4,900 lines) |
| Approver workflow (current) | `AnmolDristi/ClaimsApproval_v2.aspx` + `.aspx.cs` (2,790 lines) |
| Approver workflow (legacy) | `AnmolDristi/ClaimsApproval.aspx` + `.aspx.cs` |
| Employee read views | `AnmolDristi/MyClaimsView_v2.aspx.cs`, `MyClaimsView.aspx.cs`, `ApprovedClaimsView.aspx.cs`, `ClaimsSubmitted.aspx.cs` |
| Shared rules | `ClaimStatuses.cs`, `ClaimEditabilityHelper.cs`, `SalesApprovalDetailVersionHelper.cs`, `BasePage.cs`, `PermissionHelper.cs` |
| Entry / identity | `login.aspx.cs`, `SubmitClaim.aspx.cs`, `Web.config` |
| Stored procedures in source control | `Database/Scripts/005_usp_ProcessApprovalAction.sql` |

### 1.1 Evidence limitation that must be resolved first

Only **one** workflow stored procedure exists in source control: `usp_ProcessApprovalAction`. Git history contains no definition — current or deleted — for any of the following, all of which are called from live code:

`usp_SubmitSalesDailyActivities`, `usp_ForwardApprovalRequest`, `usp_MarkRequestReadyForPayment`, `usp_ReturnClaimToEmployee`, `usp_ProcessSalesApprovalRequestDetail`, `usp_ProcessApprovalRequestDetails_WithRemarks`, `usp_UpdateSalesDailyExpenseStatus`, `usp_UpdateActivityKM_WithHistory`, `usp_ReopenApprovalRequestByAuditor`, `usp_SaveDailyExpense`, `usp_SaveHotelExpense`, `usp_InsertTravel_WithLock`, `usp_GetMyClaims`.

There is also **no DDL** in the repository for `SalesApprovalRequest`, `SalesApprovalRequestDetail`, `SalesApprovalRequestApprover`, `SalesDailyActivity`, `SalesDailyExpense`, `SalesDailyActivityKMHistory`, `ApprovalRequestStatusHistory`, or the table type `TVP_ActivityUpdate`.

Findings below that depend on those procedure bodies are marked **[UNVERIFIED — SP body not in repo]**. They are stated as risks, not as confirmed defects. **Issue 30** covers scripting them into source control; it should be done before any fix is implemented, because several fixes belong inside those procedures.

---

## 2. State machine: specified vs. implemented

### 2.1 Specified

```
Draft → Submitted → Pending(L1) → [Pending(L2)] → [Pending(L3)] → Pending(Auditor)
      → ReadyForPayment → Completed
Returned ⇄ (employee correction, new DetailVersion, re-enters at L1)
```
`Approved` is never a header status. `Completed` means Finance has paid. Rollback allowed from Submitted / Pending / Returned / ReadyForPayment; never after Completed.

### 2.2 Implemented

```
Draft → Submitted → Pending  ──(all days resolved, next approver exists)──► Pending (level advanced)
                             ──(all days resolved, no next approver, role ≠ Auditor)──► Completed   ⚠ (Issue 8)
                             ──(all days resolved, no next approver, role = Auditor)──► Pending
                             ──(any latest detail Returned)──► Returned
Pending/Returned ──(auditor button)──► ReadyForPayment
ReadyForPayment ──► (nothing; no Finance step exists in the codebase)  ⚠ (Issue 17)
Pending ──(legacy page, auditor)──► "Reopened" / unknown  ⚠ (Issue 4, Issue 16)
```

Divergences:

1. `Completed` is reachable straight from manager approval without an Auditor and without payment (`005_usp_ProcessApprovalAction.sql:315-345`).
2. `Completed` therefore carries two contradictory meanings in the same system: "all approvals done, awaiting payment" (SP) and "Finance has paid" (spec).
3. Levels 2 and 3 can never be created, because the submit call hard-codes them to NULL (`SalesMonthlyReview.aspx.cs:4365-4366`).
4. There is more than one path between the same two states: day-approve auto-advance (SP) and the explicit Forward button both move `Pending → Pending(next level)`.
5. `Rejected` is used as a **header** status in employee gating logic (`SalesMonthlyReview.aspx.cs:227-229`, `4258`) even though it is not a valid header status.
6. `Submitted` is treated as a **detail** status inside the SP's pending count (`005:190`) though it is not a valid detail status.

---

## 3. Issue register

Severity scale: **Critical** = data corruption, financial loss, or authorization bypass reachable today. **High** = incorrect workflow outcome or money figure. **Medium** = integrity/observability defect. **Low** = hygiene / defence-in-depth.

Regression-risk scale: **Low** = additive guard, existing valid flows unaffected. **Medium** = may block flows that currently succeed by accident; needs UAT of the happy path. **High** = touches routing or totals; needs full-cycle regression.

---

### Issue 1 — No transition validates the actual current approver

**Severity:** Critical

**Current behaviour.** No layer compares the acting user against `SalesApprovalRequest.CurrentApproverLoginId` or `CurrentApproverLevel`. `usp_ProcessApprovalAction` accepts `@ActionBy` as a free-text label and never validates it (`005:21-127`); it reads `CurrentApproverLevel` only to *stamp* the row. The page resolves the user's role from a different table entirely:

```2286:2298:AnmolDristi/ClaimsApproval_v2.aspx.cs
                using (var cmd = new SqlCommand("SELECT TOP 1 ApproverLevel, ApproverRole FROM SalesApprovalRequestApprover WHERE ApprovalRequestId=@Id AND ApproverLoginId=@User AND Status='" + ClaimStatuses.Pending + @"'", con))
```

The predicate is "is this user *an* approver on this request whose row is still Pending" — not "is this user *the* current approver". Every approver from L1 to Auditor has a Pending row from the moment of submission until they act.

**Risk.** An Auditor (or an L3 manager) can approve, reject, or finalize a claim that is still sitting at L1, before the line manager has seen it. Segregation of duties in the approval chain is not enforced at any layer. Because `DetailApprovedLevel` is stamped with the *header's* level rather than the actor's, the audit trail records the action as having been taken by the level that was current, not the level that acted.

**Root cause.** Authorization was implemented as role discovery for UI rendering and then reused as the authorization decision. The authoritative routing columns (`CurrentApproverLoginId`, `CurrentApproverLevel`) are written but never read for enforcement.

**Recommended fix (minimum change).** Add an ownership guard as the first statement inside each transition procedure, before `BEGIN TRANSACTION`: resolve the request row and `RAISERROR` unless `CurrentApproverLoginId = @ActionBy` (and, where relevant, `CurrentApproverLevel` matches the approver row for `@ActionBy`). Do not change any C#; the pages already surface `RAISERROR` text into the modal message. Keep the existing C# role checks as UI hints.

**Files.** `ClaimsApproval_v2.aspx.cs` (2282-2310, 1818-1834, 2622-2681), `ClaimsApproval.aspx.cs` (852-867, 1274-1298, 2262-2308)

**Stored procedures.** `usp_ProcessApprovalAction`, `usp_ProcessSalesApprovalRequestDetail`, `usp_ForwardApprovalRequest`, `usp_MarkRequestReadyForPayment`, `usp_ReturnClaimToEmployee`, `usp_UpdateSalesDailyExpenseStatus`, `usp_UpdateActivityKM_WithHistory`

**Estimated regression risk.** Medium. Any workflow that currently depends on out-of-turn action (for example an auditor pre-clearing receipts while the claim is at L1) will start failing. That behaviour must be confirmed as unwanted before deployment; if the business does want auditor pre-review, allow it explicitly for read-only expense actions rather than leaving the gap open.

---

### Issue 2 — Payment lock does not exist: no mutation checks header status

**Severity:** Critical

**Current behaviour.** No mutating handler in either approval page, and no branch of `usp_ProcessApprovalAction`, reads `SalesApprovalRequest.Status` before writing. `EnsureCanFinalizeRequest` (`ClaimsApproval_v2.aspx.cs:600-628`) checks only that day and expense rows are resolved. The following remain callable against a claim in `ReadyForPayment` or `Completed`:

- day approve / return / reject (`gvDetails_RowCommand:1381`, `ProcessBulkAction:1577`, `ActionDayRowSilent:2624`)
- expense status change (`UpdateIndividualExpense:2112`)
- KM change (`UpdateActivityKM:2173`)
- header total recalculation (`btnRecalculate_Click:2430`, `btnRefreshCalculations_Click:1372`)
- forward (`btnForwardLevel_Click:631`), return-to-employee (`btnReturnToEmployee_Click:831`), mark-ready-for-payment again (`btnMarkReadyForPayment_Click:2010`)

**Risk.** A paid claim can be re-approved, re-returned, re-costed, or have its approved amounts recalculated after Finance has disbursed. The stored figures no longer match the payment. This is the single largest financial-integrity gap in the workflow.

**Root cause.** Requirement 11 (payment lock) was never implemented anywhere. Header status is used for display and for button visibility only, and button visibility is computed from row counts (`ClaimsApproval_v2.aspx.cs:506-517`), not from status.

**Recommended fix (minimum change).** One reusable guard block at the top of every mutating stored procedure: read `Status` into a variable with `(UPDLOCK, HOLDLOCK)` and `RAISERROR` when it is `Completed`, or when it is `ReadyForPayment` for anything other than an explicitly allowed rollback. No page changes required. Add the equivalent status read to `EnsureCanFinalizeRequest` only as a fast-fail UI courtesy.

**Files.** `ClaimsApproval_v2.aspx.cs` (600-628, 631, 831, 1381, 1577, 2010, 2110-2196, 2430, 2624), `ClaimsApproval.aspx.cs` (94-115, 1274, 2262, 2631, 1974)

**Stored procedures.** all workflow procedures listed in §1.1

**Estimated regression risk.** Low. Nothing in the specified workflow legitimately writes to a Completed claim.

---

### Issue 3 — Employee can edit days that are terminal, and the edits never reach the approval snapshot

**Severity:** Critical

**Current behaviour.** Two rules combine badly. First, a day is considered reopened for correction when its latest detail status is Returned **or Rejected**:

```129:133:AnmolDristi/ClaimEditabilityHelper.cs
        public static bool IsDetailStatusEditableForCorrection(string detailStatus)
        {
            return ClaimStatuses.Equals(detailStatus, ClaimStatuses.Rejected)
                || ClaimStatuses.Equals(detailStatus, ClaimStatuses.Returned);
        }
```

Second, the month gate has no branch for `ReadyForPayment` or `Completed`; those fall into the generic "locked" branch, which then unlocks any day matching the rule above and re-enables the Submit button:

```313:317:AnmolDristi/SalesMonthlyReview.aspx.cs
                            // ⬅️ 3. NEW: If there are returned/rejected rows, UNLOCK the Submit button!
                            if (editableActivityIds.Count > 0)
                            {
                                allowSubmission = true;
                            }
```

Meanwhile the resubmission collector selects **Returned only** (`SalesMonthlyReview.aspx.cs:4274`), so a Rejected day the employee just edited is silently dropped from the TVP.

**Risk.** After an auditor rejects a day — a terminal decision — the employee can still change its KM, amounts, and receipts, including after the claim is paid. Those edits are written to the live `SalesDailyActivity` / `SalesDailyExpense` rows but never versioned into `SalesApprovalRequestDetail` and never resubmitted. The audited snapshot and the live data permanently disagree, and every downstream report that joins live expense rows (all the deduction-breakdown queries do) shows figures that were never approved. Additionally, the save gate used on the server for expenses is the weaker detail-only check, which does not exclude Rejected:

```87:90:AnmolDristi/ClaimEditabilityHelper.cs
        public static bool CanEditExpenseByDetailStatus(string detailStatus)
        {
            return !IsSubmittedApprovedOrPending(detailStatus);
        }
```

**Root cause.** "Rejected" was treated as a correction state at the time the reopen rule was written, and the later decision that rejection is terminal was applied to the resubmission filter but not to the editability helper or the month gate.

**Recommended fix (minimum change).** Three edits, no restructuring: (a) drop `Rejected` from `IsDetailStatusEditableForCorrection` so it returns true only for `Returned`; (b) in `BindGrid`, treat `ReadyForPayment` and `Completed` as hard-locked — skip the per-day unlock loop entirely; (c) make the server-side expense save gate use `CanEditExpense` (which already excludes Rejected) instead of `CanEditExpenseByDetailStatus`, and add the matching `ClaimStatus NOT IN ('Rejected','Approved')` predicate inside `usp_SaveDailyExpense` / `usp_SaveHotelExpense` / `usp_InsertTravel_WithLock` so the rule holds even if a page is bypassed.

**Files.** `ClaimEditabilityHelper.cs` (87-90, 129-133), `SalesMonthlyReview.aspx.cs` (216-318, 2561-2574, 4262-4274)

**Stored procedures.** `usp_SaveDailyExpense`, `usp_SaveHotelExpense`, `usp_InsertTravel_WithLock`, `usp_UpdateActivityKM`, `usp_SubmitSalesDailyActivities`

**Estimated regression risk.** Medium. Employees who currently use the rejected-day edit path to "fix and hope" will lose it. This is the intended outcome, but it will generate support traffic; the returned-day path is unaffected.

---

### Issue 4 — The legacy approval page is still deployed and runs a second, contradictory workflow engine

**Severity:** Critical

**Current behaviour.** `ClaimsApproval.aspx` is no longer linked from the menu (`Dristi.Master:564` points only at `ClaimsApproval_v2.aspx`), but it is still compiled, still deployed, and still served to any authenticated session. It calls `RequirePermission` nowhere. It drives the workflow through a different set of procedures:

| Action | v2 (current) | v1 (legacy, still live) |
| --- | --- | --- |
| Day approve/return | `usp_ProcessApprovalAction` | `usp_ProcessSalesApprovalRequestDetail` |
| Bulk approve | `usp_ProcessApprovalAction` in a transaction | `usp_ProcessApprovalRequestDetails_WithRemarks` |
| Reopen a claim | not available | `usp_ReopenApprovalRequestByAuditor` (`ClaimsApproval.aspx.cs:1980`) |
| Ready for payment | auditor-checked | no auditor re-check on click (`ClaimsApproval.aspx.cs:2631-2644`) |
| Return to employee | `usp_ReturnClaimToEmployee` | not available |
| Auditor identification | `ApproverRole = 'Auditor'` | hard-coded `currentUserLevel == 2` (`ClaimsApproval.aspx.cs:611`) |

v1 also gates its Ready-for-Payment button on the header being `Completed` (`ClaimsApproval.aspx.cs:1835-1836`), the inverse of the v2 rule.

**Risk.** Anyone with the URL can run approvals through a code path that has no Returned handling, no recalculation, no transaction, weaker authorization, and an undocumented reopen capability. Two engines writing the same tables under different rules is how a workflow ends up in a state neither engine can resolve.

**Root cause.** v2 was introduced beside v1 rather than replacing it; v1 was unlinked from the menu but never retired.

**Recommended fix (minimum change).** Do not delete or refactor. Add the standard `RequirePermission` call to `ClaimsApproval.aspx.cs:Page_Load` against a permission key granted to nobody, or add a `Response.Redirect` to `ClaimsApproval_v2.aspx` at the top of `Page_Load`. Apply the same to the orphaned `MyClaimsView.aspx` and `ClaimsSubmitted.aspx`. One line per page.

**Files.** `ClaimsApproval.aspx.cs` (28-37, 611, 1835-1836, 1974-1987, 2631-2644), `MyClaimsView.aspx.cs`, `ClaimsSubmitted.aspx.cs`, `Dristi.Master` (559-565)

**Stored procedures.** `usp_ProcessApprovalRequestDetails_WithRemarks`, `usp_ReopenApprovalRequestByAuditor` (both v1-only)

**Estimated regression risk.** Low, provided nobody is currently working from a bookmark. Check IIS logs for hits on `/ClaimsApproval.aspx` before switching it off.

---

### Issue 5 — Forward ownership is violated: the day-approval procedure re-routes the claim

**Severity:** Critical

**Current behaviour.** The rule is that only `usp_ForwardApprovalRequest` may change `CurrentApproverLoginId` and `CurrentApproverLevel`. `usp_ProcessApprovalAction` does exactly that whenever the last open day at a level is approved:

```279:286:Database/Scripts/005_usp_ProcessApprovalAction.sql
                    UPDATE dbo.SalesApprovalRequest
                    SET
                          CurrentApproverLevel    = @NextApproverLevel
                        , CurrentApproverLoginId  = @NextApproverLoginId
                        , Status                  = 'Pending'
```

The Forward button therefore usually fires against a claim that has *already* been forwarded by the last day-approval, and `usp_ForwardApprovalRequest` is asked to advance a claim whose level has already moved on.

**Risk.** Two owners of the routing columns means the claim can skip a level (auto-advance then manual forward advances twice) or leave the approver rows and the header disagreeing. It also makes the Forward-validation requirement unenforceable: auto-advance happens inside a per-day action that never evaluates pending or returned expenses at the claim level.

**Root cause.** Auto-advance was added to the day-approval procedure as a convenience so managers would not have to press Forward, while the explicit Forward button was retained.

**Recommended fix (minimum change).** Delete the routing block from `usp_ProcessApprovalAction` (lines 265-314) and leave it setting `Status` only. The Forward button becomes the single owner. If auto-advance must stay for usability, the inverse is also acceptable and smaller: keep the block but have it call `usp_ForwardApprovalRequest` rather than writing the columns itself, so the forward validations execute in one place. Do not implement both.

**Files.** `ClaimsApproval_v2.aspx.cs` (631-754)

**Stored procedures.** `usp_ProcessApprovalAction` (265-314), `usp_ForwardApprovalRequest`

**Estimated regression risk.** High. This changes when claims move between levels. Requires a full multi-level UAT cycle including the L1-only case.

---

### Issue 6 — AJAX endpoints mutate workflow state with no ownership check, and the role they trust bleeds across claims

**Severity:** Critical

**Current behaviour.** Three `[WebMethod]`s mutate data. None validates that the supplied identifier belongs to a claim the caller may action:

```2112:2117:AnmolDristi/ClaimsApproval_v2.aspx.cs
        public static object UpdateIndividualExpense(int expenseId, string action, string remark)
        {
            try
            {
                string user = GetLoggedInEmployeeCodeFromSession() ?? "System";
                string approverRole = GetApproverRoleFromSession();
```

`approverRole` is fetched and then never used. `UpdateActivityKM` (2173) has no check at all. `ActionDayRowSilent` (2624) checks a role — but that role is a single session slot written whenever any claim's modal is opened:

```2295:2298:AnmolDristi/ClaimsApproval_v2.aspx.cs
                        string role = rdr["ApproverRole"].ToString();
                        ViewState["CurrentUserLevel"] = Convert.ToInt32(rdr["ApproverLevel"]);
                        ViewState["CurrentUserRole"] = role;
                        Session[SessionApproverRoleKey] = role;
```

A user who is Auditor on claim A and L1 manager on claim B keeps `Auditor` in session after opening A, and can then reject days on claim B — rejection being auditor-only and terminal. Note also `user` falls back to the literal `"System"` when the session has expired, so the audit trail can record `System` as the actor.

**Risk.** Terminal rejection and expense/KM mutation are reachable by identifier guessing, from any authenticated session, with an attributable-to-nobody actor string.

**Root cause.** The unified day modal was added as an AJAX layer over handlers whose authorization lived in page state, and the page state could not be reached from a static WebMethod, so a session slot was substituted.

**Recommended fix (minimum change).** Two changes. In C#: pass `reqId` to `UpdateIndividualExpense` and `UpdateActivityKM` and reject the call when the session user has no Pending approver row for that request — the query already exists at 2286 and can be reused as a static helper. Replace `?? "System"` with a hard failure. In SQL: the Issue 1 guard makes the role slot irrelevant, because the procedure will verify the actor against `CurrentApproverLoginId` for the specific request.

**Files.** `ClaimsApproval_v2.aspx.cs` (569-572, 2110-2196, 2282-2310, 2622-2681), `ClaimsApproval.aspx.cs` (94-115, 1667-1715)

**Stored procedures.** `usp_UpdateSalesDailyExpenseStatus`, `usp_UpdateActivityKM_WithHistory`, `usp_ProcessApprovalAction`, `usp_ProcessSalesApprovalRequestDetail`

**Estimated regression risk.** Low. Legitimate users always act on a claim they hold.

---

### Issue 7 — Workflow operations are not atomic: multiple connections per logical transition

**Severity:** Critical

**Current behaviour.** Every claim-level operation is composed of two or more independent auto-commit round trips on separate connections.

| Operation | Steps | Atomic? |
| --- | --- | --- |
| Forward (`631-754`) | `ExecuteRecalculationSQL` on connection A, then `usp_ForwardApprovalRequest` on connection B | No |
| Ready for payment (`2010-2106`) | recalculation, then `usp_MarkRequestReadyForPayment` | No |
| Day reject (`1853-1856`) | `usp_ProcessSalesApprovalRequestDetail`, then inline `MarkActivityRejectedForDay` | No, except in bulk |
| Single day action (`1406-1412`) | procedure call, then recalculation on a new connection | No |
| Bulk day action (`1658-1684`) | procedures inside a transaction, recalculation **after** commit | Partially |
| Employee resubmit (`4294-4352`) | status sync SQL on connection A, then `usp_SubmitSalesDailyActivities` on connection B | No |

The transactional versions of these flows exist in the file, commented out — for example the entire transaction wrapper around Forward at `ClaimsApproval_v2.aspx.cs:645-729` and around Ready-for-Payment at `2033-2091`.

**Risk.** A failure or timeout between steps leaves the claim forwarded but with pre-forward totals, rejected at detail level but not at activity level, or with activity rows flipped to Returned and no submission behind them. There is no compensation logic anywhere; recovery is manual SQL.

**Root cause.** Transactions were removed when the inline SQL was migrated into stored procedures, on the assumption that one procedure call equals one unit of work. It does not, because recalculation and the reject cascade stayed outside.

**Recommended fix (minimum change).** Do not reintroduce C# transactions. Move the second step into the procedure that owns the transition, so each operation is one call: fold the recalculation statements into `usp_ForwardApprovalRequest` and `usp_MarkRequestReadyForPayment` (they are the same statements already present in `usp_ProcessApprovalAction` §6), and fold `MarkActivityRejectedForDay`'s two UPDATEs into `usp_ProcessSalesApprovalRequestDetail`. Then delete the now-redundant C# calls. This also resolves Issues 12 and 21.

**Files.** `ClaimsApproval_v2.aspx.cs` (631-754, 1381-1421, 1577-1713, 1893-1924, 2010-2106, 2538-2586), `SalesMonthlyReview.aspx.cs` (4294-4352)

**Stored procedures.** `usp_ForwardApprovalRequest`, `usp_MarkRequestReadyForPayment`, `usp_ProcessSalesApprovalRequestDetail`, `usp_SubmitSalesDailyActivities`

**Estimated regression risk.** Medium. Behaviour is unchanged on the success path; only failure handling changes. Verify the recalculation runs against the same row set once moved inside the transaction.

---

### Issue 8 — Manager approval can set the header to Completed, bypassing Auditor and Finance

**Severity:** High

**Current behaviour.**

```315:327:Database/Scripts/005_usp_ProcessApprovalAction.sql
                ELSE IF ISNULL(@CurrentApproverRole, '') <> 'Auditor'
                BEGIN
                    -- Non-auditor final level with all days resolved -> Completed.
                    UPDATE dbo.SalesApprovalRequest
                    SET
                          Status                  = 'Completed'
```

If the current level is the highest configured approver and that approver is not an Auditor, the claim jumps to `Completed`. It never passes through `ReadyForPayment`, and `Completed` is supposed to mean Finance has paid.

**Risk.** A claim can present as paid when no auditor has reviewed it and no payment has occurred. Combined with Issue 2, that claim is then treated as terminal by nothing at all, and combined with Issue 9 (auditor provisioning depends on a hard-coded code) the branch is genuinely reachable whenever the auditor row is missing.

**Root cause.** The procedure treats `Completed` as "approval chain exhausted", which was the meaning before `ReadyForPayment` was introduced. The remark it writes says so: `'All approval levels finished. Awaiting payment processing.'`

**Recommended fix (minimum change).** Replace the `Completed` write with an error. If no Auditor level exists, the claim configuration is wrong and the transition should fail loudly rather than silently completing. Leave `Status = 'Pending'` and `RAISERROR('No auditor level configured for this request.')`, or leave the claim Pending with a history row for manual intervention. Do not add a new status.

**Files.** none (SQL only)

**Stored procedures.** `usp_ProcessApprovalAction` (315-345)

**Estimated regression risk.** Medium. Any claim currently relying on this branch to finish will stop finishing. Query production for requests whose approver set contains no `Auditor` row before deploying.

---

### Issue 9 — Optional L2/L3 cannot exist, and the auditor is a hard-coded string

**Severity:** High

**Current behaviour.** The submit call passes the approver hierarchy from session, with levels 2 and 3 pinned to NULL:

```4364:4367:AnmolDristi/SalesMonthlyReview.aspx.cs
                cmd.Parameters.AddWithValue("@L1Code", Session["RONE_CODE"] != null ? (object)Session["RONE_CODE"].ToString() : DBNull.Value);
                cmd.Parameters.AddWithValue("@L2Code", DBNull.Value);
                cmd.Parameters.AddWithValue("@L3Code", DBNull.Value);
                cmd.Parameters.AddWithValue("@AuditorCode", Session["AUDITOR_CODE"] != null ? (object)Session["AUDITOR_CODE"].ToString() : DBNull.Value);
```

`Session["RTWO_CODE"]` is populated at login (`login.aspx.cs:228`) and simply not used. The auditor code is a literal, with the real lookup commented out:

```230:233:AnmolDristi/login.aspx.cs
                Session["AUDITOR_CODE"] = "Audit";
...
                //Session["AUDITOR_CODE"] = row["AuditorCode"] == DBNull.Value ? "" : row["AuditorCode"].ToString();
```

The whole hierarchy is also read from session state captured at login, so a mid-month change of reporting manager does not affect claims submitted afterwards in an existing session.

**Risk.** The two optional manager levels in the specified workflow are unreachable. Auditor routing depends on a row existing for the login id `Audit`; if it does not, the claim has no auditor level, which activates the Issue 8 branch and completes the claim on manager approval.

**Root cause.** L2/L3 were stubbed out during rollout and the auditor lookup was temporarily hard-coded; neither was restored.

**Recommended fix (minimum change).** Pass `Session["RTWO_CODE"]` as `@L2Code` (leaving L3 NULL until a source exists for it) and restore the commented auditor lookup. Better, and still minimal: resolve the whole hierarchy inside `usp_SubmitSalesDailyActivities` from the employee master using `@LoginId`, and ignore the four code parameters. That removes the session-staleness problem too and puts the rule in SQL, per requirement 12.

**Files.** `SalesMonthlyReview.aspx.cs` (4346-4378), `login.aspx.cs` (226-233, 609-611)

**Stored procedures.** `usp_SubmitSalesDailyActivities`

**Estimated regression risk.** High. Adding an L2 level changes routing for every new claim. Deploy behind a switch or verify against a known-good employee hierarchy first.

---

### Issue 10 — Returning a day reverts already-approved expenses

**Severity:** High

**Current behaviour.** The comment says approved rows are protected; the predicate protects only rejected rows.

```161:182:Database/Scripts/005_usp_ProcessApprovalAction.sql
        --    Approved/Rejected terminal rows are left untouched on RETURN.
        ...
             OR (@NormalizedAction = 'RETURN'  AND ISNULL(ClaimStatus, '') NOT IN ('Rejected'))
```

**Risk.** A manager returning one day for a KM correction silently reopens every receipt on that day that an approver had already cleared, including receipts approved at a higher level. Those approvals are lost and must be redone, and the approval history for the expense is overwritten.

**Root cause.** Copy/paste divergence between the APPROVE and RETURN predicates; the comment records the intended rule.

**Recommended fix (minimum change).** Add `'Approved'` to the RETURN exclusion list, matching the comment: `NOT IN ('Rejected', 'Approved')`.

**Files.** none (SQL only)

**Stored procedures.** `usp_ProcessApprovalAction` (165-182)

**Estimated regression risk.** Low. Single-token change; the intended behaviour is documented in the adjacent comment.

---

### Issue 11 — Rejecting a day leaves approved expenses stranded on it

**Severity:** High

**Current behaviour.** Both reject implementations deliberately skip approved expenses. In SQL: `OR (@NormalizedAction = 'REJECT' AND ISNULL(ClaimStatus,'') NOT IN ('Approved'))` (`005:181`). In C#:

```1909:1915:AnmolDristi/ClaimsApproval_v2.aspx.cs
                    UPDATE dbo.SalesDailyExpense
                    SET ClaimStatus = '" + ClaimStatuses.Rejected + @"',
                        ModifiedBy = @User,
                        ModifiedOn = SYSUTCDATETIME()
                    WHERE ActivityId = @ActivityId
                      AND IsDeleted = 0
                      AND ISNULL(ClaimStatus, '') <> '" + ClaimStatuses.Approved + @"';
```

**Risk.** An orphan state: an `Approved` expense whose parent day is `Rejected`. The finalization check (`EnsureCanFinalizeRequest:605-618`) accepts it because Approved counts as resolved, so the claim can be finalized carrying approved expense lines on a rejected day. Header totals exclude the rejected day (`005:404`) while the deduction-breakdown reports read the expense rows directly, so the two disagree about the same money.

**Root cause.** Both implementations protect "already decided" expenses without distinguishing "decided independently" from "invalidated by the parent decision".

**Recommended fix (minimum change).** Cascade rejection to all non-deleted expenses on the day, with no exclusion, in whichever procedure ends up owning REJECT after Issue 12 is resolved. If preserving the prior approval decision is genuinely required for audit, record it in remarks; do not leave the status inconsistent.

**Files.** `ClaimsApproval_v2.aspx.cs` (1893-1924)

**Stored procedures.** `usp_ProcessApprovalAction` (165-182), `usp_ProcessSalesApprovalRequestDetail`

**Estimated regression risk.** Low.

---

### Issue 12 — Three implementations of "reject a day", and the one in source control is dead

**Severity:** High

**Current behaviour.** `usp_ProcessApprovalAction` accepts and fully implements `REJECT` (`005:56, 124, 181, 373-384`). No caller ever passes it: the only invocations pass `APPROVE` or `RETURN` (`ClaimsApproval_v2.aspx.cs:1847, 1851, 2661, 2666`). Rejection is instead performed by `usp_ProcessSalesApprovalRequestDetail` followed by inline C# SQL, and separately by the legacy page's `usp_ProcessApprovalRequestDetails_WithRemarks`.

Consequence: the reject path gets none of the behaviour that lives in the reviewed procedure — no latest-version check, no header total recalculation, no status-history row, no mixed-state re-evaluation. The only reject implementation that is under review and version control is the one that never runs.

**Risk.** Rejection is the terminal, money-reducing action, and it is the least controlled transition in the system. Any fix applied to `usp_ProcessApprovalAction` will silently not apply to rejection.

**Root cause.** Rejection was left on the older procedure during the migration to `usp_ProcessApprovalAction`, and the dead branch was written anyway.

**Recommended fix (minimum change).** Route rejection through `usp_ProcessApprovalAction` by passing `"REJECT"` from `ProcessDayDetailAction` and `ActionDayRowSilent`, delete the `MarkActivityRejectedForDay` call, and retire `usp_ProcessSalesApprovalRequestDetail` from the v2 page. The SQL branch already exists and already handles the activity and expense cascade; it needs only the Issue 11 correction. This is a two-line C# change and gives one owner per transition, as required by objective 3.

**Files.** `ClaimsApproval_v2.aspx.cs` (1842-1878, 1893-1924, 2655-2673), `ClaimsApproval.aspx.cs` (1274-1298, 2262-2308)

**Stored procedures.** `usp_ProcessApprovalAction` (373-384), `usp_ProcessSalesApprovalRequestDetail`, `usp_ProcessApprovalRequestDetails_WithRemarks`

**Estimated regression risk.** Medium. Rejection outcomes change: the header total and history will now update on reject, which they currently do not. That is the correct behaviour but it will move numbers on existing test data.

---

### Issue 13 — Approving a day does not require its expenses to be resolved, except in bulk

**Severity:** High

**Current behaviour.** The bulk path checks child expenses properly (`TryEvaluateBulkEligibility:1237-1275`, `HasPendingReceiptsFromDb:1179-1187`). The single-row path does not:

```1406:1409:AnmolDristi/ClaimsApproval_v2.aspx.cs
                    ProcessDayDetailAction(detailId, reqId, action, remark);
                    ExecuteRecalculationSQL(reqId);
```

The only barrier for a single row is that the Approve button is rendered disabled (`ApplyGatekeeperLogic:1004-1011`), and the flag driving it is a substring match against a JSON blob whose broader form was commented out:

```917:921:AnmolDristi/ClaimsApproval_v2.aspx.cs
                //bool hasPendingReceipts = expensesJson.Contains("\"ClaimStatus\":\"Submitted\"") ||
                //                          expensesJson.Contains("\"ClaimStatus\":\"Draft\"") ||
                //                          expensesJson.Contains("\"ClaimStatus\":\"Pending\"");

                bool hasPendingReceipts = expensesJson.Contains("\"ClaimStatus\":\"Pending\"") || expensesJson.Contains("\"ClaimStatus\":\"Returned\"");
```

Expenses with `Draft`, `Submitted`, or NULL status are no longer detected — by the button, and also by `HasPendingReceiptsFromDb`, which tests only Pending and Returned. `ActionDayRowSilent` has no expense check whatsoever. `usp_ProcessApprovalAction` has none either; it simply sweeps every non-terminal expense to `Approved` (`005:179`).

**Risk.** A day can be approved with unreviewed receipts, and the approval then marks those receipts Approved as a side effect. That is precisely the inversion the business rule forbids: approval propagates to expenses without an explicit expense decision, and money is approved that nobody looked at.

**Root cause.** The gate was implemented once for bulk against the database, once for single-row against rendered JSON, and not at all for the AJAX modal or the procedure.

**Recommended fix (minimum change).** Put the check in one place: at the top of the APPROVE branch of `usp_ProcessApprovalAction`, count non-deleted expenses for the activity whose `ClaimStatus` is not in (`Approved`,`Rejected`) and `RAISERROR` if any exist. Then remove the sweep at `005:179` for APPROVE so expense status is only ever set by an explicit expense action. Leave the C# gates in place as UI hints; delete none of them.

**Files.** `ClaimsApproval_v2.aspx.cs` (905-1031, 1179-1195, 1237-1275, 1381-1421, 2622-2681)

**Stored procedures.** `usp_ProcessApprovalAction` (165-182, 249-372)

**Estimated regression risk.** Medium. Days with legacy Draft/NULL expense rows will become unapprovable until those rows are cleaned. Run a pre-deployment count of `SalesDailyExpense` rows with a non-canonical `ClaimStatus`.

---

### Issue 14 — Expense adjustments are never recorded; `SalesExpenseAdjustment` is unreferenced

**Severity:** High

**Current behaviour.** The string `SalesExpenseAdjustment` does not appear anywhere in the repository. There is also no notion of an approved amount that differs from the claimed amount: `usp_UpdateSalesDailyExpenseStatus` is called with a status and remarks only (`ClaimsApproval_v2.aspx.cs:2154-2157`), never with an amount. Deductions are not stored; they are re-derived at read time by four separate near-duplicate queries (`ClaimsApproval_v2.aspx.cs:360-443`, `ApprovedClaimsView.aspx.cs:320-384`, `MyClaimsView.aspx.cs:427-491`, `MyClaimsView_v2.aspx.cs:488-582`) which reconstruct the original claim as approved + deducted.

**Risk.** The rule "insert into `SalesExpenseAdjustment` only when `ApprovedAmount < ClaimedAmount`" cannot be satisfied because neither the table nor the concept is wired in. Partial approval of an expense is impossible: an approver can only approve the full amount, reject it entirely, or return it. Every deduction figure shown to an employee or exported to Finance is recomputed from live rows at display time, so it changes whenever the underlying rows change — including after payment, per Issue 2.

**Root cause.** Partial-amount approval was specified but never implemented; the reporting layer compensated by deriving deductions instead.

**Recommended fix.** This one cannot be made minimal and correct at the same time, so it needs a scoping decision before implementation. The smallest honest step is to persist what is already being derived: at the moment `usp_MarkRequestReadyForPayment` runs, snapshot the per-expense claimed vs. approved figures into `SalesExpenseAdjustment` inside the same transaction, and have the read-time queries prefer the stored rows when present. Adding an `ApprovedAmount` input to the expense action is a larger change to the modal and the procedure and should be a separate, approved work item.

**Files.** `ClaimsApproval_v2.aspx.cs` (360-443, 2110-2169, 2452-2536), `ApprovedClaimsView.aspx.cs` (320-384, 469-504), `MyClaimsView.aspx.cs` (427-491), `MyClaimsView_v2.aspx.cs` (488-582, 679-792)

**Stored procedures.** `usp_UpdateSalesDailyExpenseStatus`, `usp_MarkRequestReadyForPayment`

**Estimated regression risk.** Medium for the snapshot step; High if partial-amount approval is added.

---

### Issue 15 — Two total-recalculation engines, and the AJAX paths run neither

**Severity:** High

**Current behaviour.** Header totals are computed in two places with different rules: `usp_ProcessApprovalAction` §6 (`005:389-468`) and `ExecuteRecalculationSQL` (`ClaimsApproval_v2.aspx.cs:2538-2586`). The C# version additionally recomputes the per-day `Snapshot_*` category columns from live expense rows; the SQL version does not, so it sums whatever the C# version last wrote.

The C# recalculation is invoked from eight places (`643, 764, 1375, 1409, 1566, 1683, 1944, 2021, 2437`) and from none of the three AJAX endpoints. So the unified day modal, the expense approve/reject action, and the KM change all leave `Snapshot_DAAmount`, `Snapshot_HotelAmount`, `Snapshot_TravelAmount`, `Snapshot_StationaryAmount`, and `Snapshot_ExpenseTotal` stale, and the header totals are then summed from those stale snapshots.

`Snapshot_CalculatedClaimAmount` and `Snapshot_TotalDistanceKM` are never recomputed by either engine, so an approver's KM change through `usp_UpdateActivityKM_WithHistory` does not flow into the approved claim amount at all.

**Risk.** Approved amounts that do not match the approved detail. Whether they match depends on which UI control the approver happened to use last — the grid buttons recalculate, the modal does not.

**Root cause.** Recalculation was implemented in C# for the grid flows and duplicated into the procedure later, and the AJAX layer was added without either.

**Recommended fix (minimum change).** Make the procedure the only owner: move the two `Snapshot_*` category statements from `ExecuteRecalculationSQL` into `usp_ProcessApprovalAction` immediately before §6, add the same recalculation to `usp_UpdateSalesDailyExpenseStatus` and `usp_UpdateActivityKM_WithHistory` (including `Snapshot_CalculatedClaimAmount` from the day's KM and rate), then reduce `ExecuteRecalculationSQL` to a call of a single new `usp_RecalculateApprovalRequestTotals` used by the manual Recalculate button only. One statement body, four call sites.

**Files.** `ClaimsApproval_v2.aspx.cs` (2538-2586, 2588-2620 dead, and all nine call sites)

**Stored procedures.** `usp_ProcessApprovalAction` (389-468), `usp_UpdateSalesDailyExpenseStatus`, `usp_UpdateActivityKM_WithHistory`, `usp_ForwardApprovalRequest`, `usp_MarkRequestReadyForPayment`

**Estimated regression risk.** High. Totals will move on any claim that is currently stale. Snapshot before/after figures for a sample of in-flight claims.

---

### Issue 16 — Rollback has no allowed-state list and no owner

**Severity:** High

**Current behaviour.** The specification allows rollback from Submitted, Pending, Returned, and ReadyForPayment, and never after Completed. What exists instead: `usp_ReopenApprovalRequestByAuditor`, callable from the legacy page with no status check in C# (`ClaimsApproval.aspx.cs:1974-1987`) and no auditor re-check on click; and a `"Reopened"` header value tested at `ClaimsApproval.aspx.cs:367` that is not in `ClaimStatuses`. **[UNVERIFIED — SP body not in repo]**

There is no rollback capability at all on the current v2 page.

**Risk.** The one rollback mechanism in the system is on the retired page, is not status-guarded in the caller, and may introduce a status word the rest of the application does not recognise. Nothing prevents it from being applied to a Completed claim.

**Root cause.** Rollback was implemented before the status vocabulary was finalized and was not carried forward to v2.

**Recommended fix (minimum change).** Add the allowed-state guard inside `usp_ReopenApprovalRequestByAuditor` — proceed only when `Status IN ('Submitted','Pending','Returned','ReadyForPayment')`, error otherwise — and confirm it writes a canonical status rather than `Reopened`. Combined with Issue 4 (blocking the legacy page), this closes the gap without building a new rollback feature. If rollback is needed on v2, that is a separate approved work item.

**Files.** `ClaimsApproval.aspx.cs` (367-375, 1974-1987), `ClaimsApproval.aspx` (656), `ClaimStatuses.cs`

**Stored procedures.** `usp_ReopenApprovalRequestByAuditor`

**Estimated regression risk.** Low.

---

### Issue 17 — The Finance payment step does not exist in the codebase

**Severity:** High

**Current behaviour.** Nothing moves a claim from `ReadyForPayment` to `Completed`. There is no page, handler, scheduled job, or procedure call named for payment, finance, disbursement, settlement, UTR, or paid; no payment date or payment reference is written anywhere. The only writer of `Completed` is the manager-approval branch described in Issue 8. A `Processed` status and an `Accounts` approver role are referenced in the legacy page (`ClaimsApproval.aspx.cs:613-614, 1673`) with no action page behind them.

**Risk.** `ReadyForPayment` is a terminal state in practice. Either payment is being recorded by direct SQL against production — unaudited, untransacted, and outside the history table — or the two meanings of `Completed` are being conflated in reporting.

**Root cause.** The workflow was built to the auditor stage; the Finance stage was never delivered.

**Recommended fix.** Confirm with the business how payment is currently recorded before changing anything. The minimum in-architecture step is one procedure, `usp_MarkRequestPaid`, that moves `ReadyForPayment → Completed`, writes the payment reference and date, and inserts the history row — plus one button on an existing Finance-permissioned page. That is new functionality and should be an approved work item, not part of the hardening pass. Within this pass, the actionable part is Issue 8: stop the approval engine from writing `Completed`.

**Files.** none currently

**Stored procedures.** none currently; `usp_MarkRequestReadyForPayment` is the last step that exists

**Estimated regression risk.** N/A for the analysis; High for any implementation.

---

### Issue 18 — Read-modify-write race in the approval engine

**Severity:** High

**Current behaviour.** `usp_ProcessApprovalAction` reads the request row, the current level, and the latest-detail check **before** `BEGIN TRANSACTION` (`005:81-127`), with no `UPDLOCK`/`HOLDLOCK` and at the default isolation level. The mixed-state evaluation inside the transaction (`187-211`) is likewise an unlocked read of the sibling rows.

**Risk.** Two approvers, or one approver double-clicking, acting on two different days of the same claim concurrently can both observe "no pending days remain" and both execute the routing block, advancing the level twice or writing conflicting `CurrentApproverLoginId` values. The `@OldRequestStatus` captured outside the transaction can be stale by the time the history row is written, producing a history entry that describes a transition that did not occur.

**Root cause.** The procedure was written as a sequence of reads followed by a transaction, rather than opening the transaction first and taking a row lock on the header.

**Recommended fix (minimum change).** Move `BEGIN TRANSACTION` above the first `SELECT`, and add `WITH (UPDLOCK, HOLDLOCK)` to the read of `dbo.SalesApprovalRequest`. That serializes concurrent actions on the same claim without changing any logic. `SET XACT_ABORT ON` is already present and correct.

**Files.** none (SQL only)

**Stored procedures.** `usp_ProcessApprovalAction` (81-130); the same pattern should be applied to every transition procedure

**Estimated regression risk.** Low, with a small increase in lock contention on claims that are being bulk-approved.

---

### Issue 19 — Three different definitions of "latest detail version"

**Severity:** Medium

**Current behaviour.**

| Definition | Location |
| --- | --- |
| `MAX(ApprovalRequestDetailId)` per activity | `SalesApprovalDetailVersionHelper.cs:17-23, 29-35`; `005:94-104, 204-209, 397-401`; `ClaimsApproval_v2.aspx.cs:2550-2555` |
| `DetailVersion` DESC, then `ApprovalRequestDetailId` DESC | `SalesApprovalDetailVersionHelper.cs:47-54` (C# in-memory) |
| `ROW_NUMBER() OVER (... ORDER BY ISNULL(DetailVersion,1) DESC, ApprovalRequestDetailId DESC)` | `SalesMonthlyReview.aspx.cs:244-255, 4263-4274` |

They agree only while identity order and version order coincide.

**Risk.** The grid can display one row as current while the procedure actions a different one. Requirement 6 — only the latest version is modifiable — is enforced against a definition that the UI does not share.

**Recommended fix (minimum change).** Pick `MAX(ApprovalRequestDetailId)` (the definition the write path already uses), and change the C# comparer at `SalesApprovalDetailVersionHelper.cs:47-54` and the two `ROW_NUMBER` queries to match it. Three small edits, no schema change.

**Files.** `SalesApprovalDetailVersionHelper.cs` (37-67), `SalesMonthlyReview.aspx.cs` (244-255, 4263-4274)

**Stored procedures.** `usp_ProcessApprovalAction`

**Estimated regression risk.** Low.

---

### Issue 20 — Terminal and already-decided details can be re-actioned

**Severity:** Medium

**Current behaviour.** `usp_ProcessApprovalAction` validates that the target is the latest version (`005:94-104`) but never validates its current `DetailStatus`. A `Rejected` or `Returned` detail can be approved; an `Approved` detail can be re-approved, overwriting `DetailApprovedBy`, `DetailApprovedOn`, and `DetailApprovedLevel`. The only barrier is `IsDetailActionableAtCurrentLevel` (`ClaimsApproval_v2.aspx.cs:1164-1177`), which is applied on the bulk path only.

**Risk.** Terminal rejection is not terminal at the data layer. Re-approval silently rewrites who approved what and when.

**Recommended fix (minimum change).** After the latest-version check, read `DetailStatus` and `RAISERROR` when it is `Rejected`, or when it is `Returned` and the action is not a re-submission, or when it equals the requested outcome at the same level. Roughly eight lines of SQL in one place.

**Files.** `ClaimsApproval_v2.aspx.cs` (1164-1177)

**Stored procedures.** `usp_ProcessApprovalAction` (93-149)

**Estimated regression risk.** Low.

---

### Issue 21 — History is incomplete and per-level approvals are deliberately suppressed

**Severity:** Medium

**Current behaviour.** `ApprovalRequestStatusHistory` is written only by `usp_ProcessApprovalAction`, and even there the general write is suppressed for the most common transition:

```473:478:Database/Scripts/005_usp_ProcessApprovalAction.sql
        IF ISNULL(@OldRequestStatus, '') <> ISNULL(@NewRequestStatus, '')
           AND NOT (
               @NormalizedAction = 'APPROVE'
               AND @NewRequestStatus = 'Pending'
               AND ISNULL(@OldRequestStatus, 'Pending') = 'Pending'
           )
```

Because the header stays `Pending` across levels, day-level approvals produce no history at all unless they trigger the level-advance branch. No history is written by any C# path (the inline history inserts survive only in the dead `_OLD` handlers at `788-808` and `1957-1960`). Expense status changes, KM changes, and detail transitions have no entry in this table. Detail history relies on the snapshot row being updated in place (`005:136-144`) despite the comment describing it as immutable — a new version is created only on resubmission.

**Risk.** The workflow cannot be reconstructed after the fact. For an approval system handling employee reimbursement, that is an audit finding in its own right, and it also makes every other issue in this report harder to investigate in production.

**Recommended fix (minimum change).** Two changes, both inside the procedures. First, remove the suppression clause so every action writes a row, including the `Pending → Pending` case, with the level and the action in the remark. Second, add the same history insert to `usp_ForwardApprovalRequest`, `usp_MarkRequestReadyForPayment`, `usp_ReturnClaimToEmployee`, and `usp_ReopenApprovalRequestByAuditor`, inside their existing transactions. Do not add a new table.

**Files.** `ClaimsApproval_v2.aspx.cs` (677-691, 788-808, 1957-1960 — all dead, for reference only)

**Stored procedures.** `usp_ProcessApprovalAction` (470-505), `usp_ForwardApprovalRequest`, `usp_MarkRequestReadyForPayment`, `usp_ReturnClaimToEmployee`, `usp_ReopenApprovalRequestByAuditor`, `usp_UpdateSalesDailyExpenseStatus`

**Estimated regression risk.** Low. Volume in the history table increases; nothing reads it in a way that breaks.

---

### Issue 22 — `DetailApprovedLevel` records the wrong level

**Severity:** Medium

**Current behaviour.** The procedure stamps the header's level (`005:142`, `DetailApprovedLevel = @CurrentApproverLevel`), while the page compares that column against the acting user's own approver level (`ClaimsApproval_v2.aspx.cs:1172-1176`, `892`, `942-951`) to decide whether the row still needs action at this level.

**Risk.** Whenever the two diverge — which is exactly the out-of-turn case Issue 1 permits — a day is either shown as already handled at a level that never saw it, or offered again to a level that already approved it.

**Recommended fix (minimum change).** Once Issue 1 is fixed the two values are always equal, so this becomes moot. Until then, stamp the acting approver's level: look it up from `SalesApprovalRequestApprover` for `@ActionBy` and use that instead of the header's level.

**Files.** `ClaimsApproval_v2.aspx.cs` (889-892, 942-951, 1164-1177)

**Stored procedures.** `usp_ProcessApprovalAction` (106-117, 136-144)

**Estimated regression risk.** Low.

---

### Issue 23 — Expense remarks are discarded

**Severity:** Medium

**Current behaviour.**

```172:175:Database/Scripts/005_usp_ProcessApprovalAction.sql
            , Remarks            = CASE
                                       WHEN @Remarks IS NOT NULL THEN ISNULL(Remarks, @Remarks)
                                       ELSE Remarks
                                   END
```

When a remark already exists, `ISNULL(Remarks, @Remarks)` returns the old one. The new remark is written only when the column was NULL.

**Risk.** The reason a receipt was returned or rejected is lost on the second and subsequent actions, which is the case that matters most.

**Recommended fix (minimum change).** `Remarks = ISNULL(@Remarks, Remarks)` — the pattern already used correctly for `DetailRemarks` at `005:139`.

**Files.** none (SQL only)

**Stored procedures.** `usp_ProcessApprovalAction` (165-182)

**Estimated regression risk.** Low.

---

### Issue 24 — Submit has no server-side status re-check and no idempotency guard

**Severity:** Medium

**Current behaviour.** `btn_submit_Click` (`SalesMonthlyReview.aspx.cs:4600-4614`) validates the policy window and collects the grid, but never re-reads `ViewState["AllowSubmission"]` or the header status. The only duplicate-submit protection is client-side button disabling in the modal (`SalesMonthlyReview.aspx:2432-2444`). Whether a second submit creates a second request depends entirely on `usp_SubmitSalesDailyActivities`. **[UNVERIFIED — SP body not in repo]**

**Risk.** A double postback, a browser retry, or a slow connection can produce a duplicate approval request for the same employee-month, which the month gate then handles ambiguously because it selects `TOP 1 ... ORDER BY RequestDate DESC` (`201`, `4233-4236`).

**Recommended fix (minimum change).** Inside `usp_SubmitSalesDailyActivities`, take an `UPDLOCK` on any existing request for the employee-month and reject the call when the status is not Draft, Returned, or absent. Add a unique index on (`RequesterLoginId`, `YearNo`, `MonthNo`) if the data supports it. No page change.

**Files.** `SalesMonthlyReview.aspx.cs` (192-232, 4504-4519, 4600-4614)

**Stored procedures.** `usp_SubmitSalesDailyActivities`

**Estimated regression risk.** Medium. Verify no employee legitimately has two requests for one month before adding the index.

---

### Issue 25 — Resubmission pre-sync runs outside the submit transaction

**Severity:** Medium

**Current behaviour.** Before calling the submit procedure, the page flips activity and expense `ClaimStatus` to `Returned` on a separate connection:

```4313:4317:AnmolDristi/SalesMonthlyReview.aspx.cs
                using (SqlCommand syncCmd = new SqlCommand(syncSql, con))
                {
                    syncCmd.Parameters.AddWithValue("@User", loginId);
                    syncCmd.ExecuteNonQuery();
                }
```

The comment above it explains why: the procedure validates `SalesDailyActivity.ClaimStatus = 'Returned'`, but editing and soft-delete can leave it as `Draft`.

**Risk.** If the submit then fails, rows are left flipped with no submission behind them. More importantly, this is a workaround for a status-synchronization defect elsewhere: the employee edit path does not maintain `ClaimStatus` consistently with `DetailStatus`, and the fix is applied at submit time from the presentation layer.

**Recommended fix (minimum change).** Move the two UPDATE statements verbatim into the start of `usp_SubmitSalesDailyActivities`, inside its transaction, scoped to the activities present in the TVP. Delete the C# block. Same statements, one fewer connection, atomic.

**Files.** `SalesMonthlyReview.aspx.cs` (4294-4327)

**Stored procedures.** `usp_SubmitSalesDailyActivities`

**Estimated regression risk.** Low.

---

### Issue 26 — Business rules duplicated between C# and SQL

**Severity:** Medium

**Current behaviour.** Rules that exist in both layers, with C# as the effective owner:

| Rule | C# location | Notes |
| --- | --- | --- |
| Claim amount from KM and rate | `SalesMonthlyReview.aspx.cs:171, 2509, 4192` via `TravelAllowanceHelper` | Computed in C#, sent to the procedure in the TVP as fact |
| KM tolerance range | `1811-1836, 1564, 4181-4186` via `KmToleranceHelper` | Also validated in `usp_UpdateActivityKM` |
| Max KM modification count | `1450-1507` | Counts history rows in C# |
| Hotel and DA caps | `3249-3293` `ValidateExpenseBeforeSave` | **Dead code — never called**; the live path relies on the procedure's `@ErrCode` |
| Duplicate DA per activity | `2821-2828` | `COUNT` in C# before insert; racy |
| Finalization completeness | `ClaimsApproval_v2.aspx.cs:600-628` | Duplicated by whatever `usp_ForwardApprovalRequest` checks |
| Editability matrix | `ClaimEditabilityHelper.cs` (entire file) | Partially duplicated by SQL predicates at `SalesMonthlyReview.aspx.cs:2287-2288` and `4297-4311` |
| Latest detail version | three variants, see Issue 19 | |

**Risk.** Where the two disagree, whichever layer is bypassed wins. The dead cap validation is the clearest example: a rule that looks implemented, is reviewed as implemented, and does not run.

**Recommended fix (minimum change).** Do not move the calculation helpers — they drive the UI and moving them is a redesign. Instead, per requirement 12, make SQL authoritative for *validation* while C# stays authoritative for *display*: have `usp_SaveDailyExpense`, `usp_UpdateActivityKM`, and `usp_SubmitSalesDailyActivities` re-validate tolerance, caps, duplicate DA, and the recomputed claim amount, and reject mismatches through the existing `@ErrCode`/`@ErrMessage` contract the pages already handle. Delete `ValidateExpenseBeforeSave` or wire it up — leaving dead validation in place is worse than either.

**Files.** `SalesMonthlyReview.aspx.cs` (171-178, 1450-1507, 1811-1836, 2821-2828, 3249-3293, 4181-4194), `ClaimEditabilityHelper.cs`, `KmToleranceHelper.cs`, `TravelAllowanceHelper.cs`, `ClaimsApproval_v2.aspx.cs` (600-628)

**Stored procedures.** `usp_SaveDailyExpense`, `usp_SaveHotelExpense`, `usp_InsertTravel_WithLock`, `usp_UpdateActivityKM`, `usp_SubmitSalesDailyActivities`, `usp_ForwardApprovalRequest`

**Estimated regression risk.** Medium. Saves that currently pass on rounding differences will start failing; use a tolerance on the amount comparison.

---

### Issue 27 — Error handling loses SQL error identity and leaks internals to the browser

**Severity:** Medium

**Current behaviour.** The CATCH block re-raises with a message string only:

```513:517:Database/Scripts/005_usp_ProcessApprovalAction.sql
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrState INT = ERROR_STATE();

        RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
```

The original error number is lost, and `RAISERROR` treats `%` in the message as a format specifier, so a message containing one is mangled or throws. On the C# side, raw `ex.Message` is written into the page (`ClaimsApproval_v2.aspx.cs:750, 1416, 2102`) and returned in AJAX JSON (`2167, 2195, 2679`), with `customErrors mode="Off"` in `Web.config:14`.

**Risk.** Callers cannot distinguish a business-rule rejection from an infrastructure failure, so the pages present both identically. Internal SQL text, object names, and stack information reach the browser.

**Recommended fix (minimum change).** Use `THROW;` instead of `RAISERROR(@ErrMsg, ...)` — it preserves number, message, and state, and it is available on the target platform. In C#, keep showing `ex.Message` only for `SqlException` with a number in the user-defined range (>= 50000) and show a generic message with a correlation id otherwise. Set `customErrors mode="RemoteOnly"`.

**Files.** `ClaimsApproval_v2.aspx.cs` (748-753, 1414-1419, 2100-2105, 2165-2168, 2677-2680), `Web.config` (14)

**Stored procedures.** all — the CATCH pattern is copied throughout

**Estimated regression risk.** Low.

---

### Issue 28 — Dead alternate implementations retained alongside live ones

**Severity:** Medium

**Current behaviour.** `ClaimsApproval_v2.aspx.cs` carries complete `_OLD` implementations of the workflow: `btnForwardLevel_Click_OLD` (756-829) with inline `CurrentApprover*` and `Status = 'Completed'` writes, `btnMarkReadyForPayment_Click_OLD` (1937-2008) with a hard-coded `ApproverLevel = 2`, `ProcessBulkAction_OLD` (1715-1804), `gvDetails_RowCommand_OLD300726` (1423-1508), `ExecuteRecalculationSQL_OLD` (2588-2620) with no latest-version filtering, and `ActionDayRowSilent_OLD` (2685+). `MapExpenseActionToClaimStatus` (1926-1934) is defined and never called. Roughly 40% of the mutating code in the file is unreachable.

**Risk.** These are event handlers. Rewiring one from the markup is a one-attribute change, and several of them write header status and routing columns directly. They also make review and diffing materially harder, which is how Issue 12 (a dead REJECT branch in the procedure) went unnoticed.

**Recommended fix (minimum change).** Delete the `_OLD` handlers and the unused helper in a single commit that touches nothing else, so the diff is verifiable as pure removal. Confirm first that no markup attribute references them.

**Files.** `ClaimsApproval_v2.aspx.cs` (756-829, 1423-1508, 1715-1804, 1926-1934, 1937-2008, 2588-2620, 2685-2790), `ClaimsApproval.aspx.cs` (980-1230, 2229-2260, 2429-2560)

**Stored procedures.** `usp_UpdateApprovalRequestDetailStatus`, `usp_UpdateApprovalRequestDetailStatus_AndMaybeActivity`, `usp_ProcessApprovalRequestDetails` — referenced only by dead code

**Estimated regression risk.** Low, if the removal commit is isolated.

---

### Issue 29 — Page-level authorization is inconsistent across the claims module

**Severity:** Low

**Current behaviour.** `RequirePermission` is called by `SubmitClaim.aspx.cs:15-16` and `ApprovedClaimsView.aspx.cs:31`, and by no other claims page. `ClaimsApproval_v2` does not even inherit `BasePage`:

```16:16:AnmolDristi/ClaimsApproval_v2.aspx.cs
    public partial class ClaimsApproval_v2 : System.Web.UI.Page
```

It checks `Session["USERID"]` directly (33-37). `SalesMonthlyReview`, `MyClaimsView`, `MyClaimsView_v2`, `ClaimsApproval`, and `ClaimsSubmitted` inherit `BasePage` but call no permission check. `BasePage` itself enforces nothing unless `Session["LoginToken"]` is set (`BasePage.cs:36-38`), and `ValidateTokenOnEveryRequest` (16) is declared but never referenced.

**Risk.** Access control for the approval console is "has a session", not "has the approval permission". The menu is hidden by `PermissionHelper`, which is presentation only.

**Recommended fix (minimum change).** Add one `RequirePermission("UI.SO_TaDa_Approve.View")` line to `ClaimsApproval_v2.Page_Load` and the equivalent to the other claims pages, matching the keys already used by the master page menu. Change `ClaimsApproval_v2` to inherit `BasePage` so the call is available.

**Files.** `ClaimsApproval_v2.aspx.cs` (16, 28-45), `SalesMonthlyReview.aspx.cs` (41-50), `MyClaimsView_v2.aspx.cs` (16, 34-38), `BasePage.cs` (16, 24-38, 188-198), `Dristi.Master` (559-565)

**Stored procedures.** none

**Estimated regression risk.** Medium. If the permission rows are not provisioned for existing approvers, they lose access on deployment. Verify grants first.

---

### Issue 30 — Workflow stored procedures and schema are not in source control

**Severity:** Low (as a defect) / blocking (as a prerequisite)

**Current behaviour.** See §1.1: thirteen live workflow procedures and all claim table DDL exist only in the database. `Database/Scripts` contains five scripts, only one of which is a workflow procedure.

**Risk.** Half of the business logic cannot be reviewed, diffed, or rolled back. Two of this report's most serious findings (Issues 1 and 2) must be fixed inside procedures nobody can currently see in a pull request. There is also no way to tell whether the deployed `usp_ProcessApprovalAction` matches script 005.

**Recommended fix (minimum change).** Script every `usp_*` procedure the claims workflow touches, plus the DDL for the eight claim tables and `TVP_ActivityUpdate`, into `Database/Scripts` as numbered idempotent scripts following the existing convention. Then diff the deployed `usp_ProcessApprovalAction` against script 005 to confirm they match.

**Files.** `Database/Scripts/`

**Stored procedures.** all listed in §1.1

**Estimated regression risk.** None — read-only extraction.

---

### Issue 31 — Configuration and session hygiene

**Severity:** Low

**Current behaviour.**

| Setting | Location | Value |
| --- | --- | --- |
| Debug compilation | `Web.config:13` | `debug="true"` |
| Custom errors | `Web.config:14` | `mode="Off"` |
| Connection string | `Web.config:7-11` | Plaintext `sa` credentials; two further commented strings with live credentials |
| Machine key | `Web.config:22` | Hard-coded in source |
| Cookie transport | `Web.config:26` | `requireSSL="false"` |
| Command timeout | all claims pages | Never set; default 30s against `executionTimeout="3600"` |

Also: `MyClaimsView_v2.aspx.cs:122` reads `Session["workman"]` while `login.aspx.cs:212` writes `Session["WORKMAN"]`, so the employee filter silently falls back to the literal `"Guest"`; and the read-only WebMethods `GetDeductionBreakdown` / `GetKMHistory` (`MyClaimsView_v2.aspx.cs:679, 794`) accept any identifier with no ownership check, exposing other employees' claim details.

**Risk.** Credential exposure through any unhandled exception page; forgeable ViewState and auth tickets if the key leaks with the source; cookies transmissible in clear; and cross-employee data disclosure through two AJAX endpoints.

**Recommended fix (minimum change).** `debug="false"`, `customErrors mode="RemoteOnly"`, `requireSSL="true"`; move the connection string and machine key to an encrypted config section or environment-level configuration and rotate the credentials, which must be treated as compromised since they are in git history. Fix the session key casing to `WORKMAN`. Add a `RequesterLoginId = @SessionUser` predicate to both read WebMethods. Set an explicit `CommandTimeout` on the bulk and export commands.

**Files.** `Web.config` (7-26), `MyClaimsView_v2.aspx.cs` (34-38, 122-131, 679-792, 794-865), `login.aspx.cs` (212)

**Stored procedures.** `usp_GetMyClaims`

**Estimated regression risk.** Low, except for the credential rotation, which requires a coordinated deployment.

---

### Issue 32 — Attachment validation is extension-only

**Severity:** Low

**Current behaviour.** Receipt upload (`SalesMonthlyReview.aspx.cs:2756-2781`) checks size (5 MB) and an extension whitelist of `.jpg/.jpeg/.png/.pdf`, then calls `SaveAs` into `~/UploadedFiles/ClaimsAttachment/{yyyy}/{MM}/`. No content-type or magic-byte check. `@AttachmentContent` is always passed as `DBNull` (`2849, 2902`), so the file exists only on disk while the database holds the path.

**Risk.** A renamed file passes validation. Because the content is not stored in the database, a lost or altered file leaves an approved claim with an unverifiable receipt, and the two stores can drift.

**Recommended fix (minimum change).** Add a magic-byte check for the four permitted types alongside the existing extension check, and ensure the upload directory has execute permissions removed at the IIS level. Whether to store content in the database is a separate decision.

**Files.** `SalesMonthlyReview.aspx.cs` (2756-2781, 2831-2867, 2891-2917)

**Stored procedures.** `usp_SaveDailyExpense`, `usp_SaveHotelExpense`, `usp_InsertTravel_WithLock`

**Estimated regression risk.** Low.

---

## 4. Prioritised fix order

### 4.1 Critical

Fix in this order; each depends on the ones above it being in place.

| # | Issue | Why first |
| --- | --- | --- |
| 1 | **Issue 30** — script procedures and DDL into source control | Prerequisite. Issues 1, 2, 5, 8, 10, 11, 13, 18, 20, 21, 23 are all SQL-side and cannot be reviewed otherwise. |
| 2 | **Issue 2** — payment lock: status guard in every mutating procedure | Largest financial exposure; one reusable guard block; lowest regression risk of the criticals. |
| 3 | **Issue 1** — validate the acting user against `CurrentApproverLoginId` / `CurrentApproverLevel` | Same guard block, same procedures. Do it in the same change as Issue 2. |
| 4 | **Issue 4** — block the legacy `ClaimsApproval.aspx` (and the other orphan pages) | One line per page; removes an entire second engine from the risk surface, which shrinks the test matrix for everything after it. |
| 5 | **Issue 3** — stop treating Rejected as editable; hard-lock ReadyForPayment and Completed for employees | Closes the snapshot-divergence path. |
| 6 | **Issue 6** — ownership checks on the three AJAX mutators | Mostly subsumed by 2 and 3, but the `"System"` fallback and the session role slot need their own fix. |
| 7 | **Issue 7** — fold recalculation and the reject cascade into the owning procedures | Makes each transition one round trip; also delivers Issues 12, 15, 21, 25. |
| 8 | **Issue 5** — single owner for `CurrentApproverLoginId` / `CurrentApproverLevel` | Highest regression risk of the criticals; do it last, with a full multi-level UAT. |

### 4.2 High priority

| Order | Issue | Note |
| --- | --- | --- |
| 9 | **Issue 18** — `UPDLOCK`/`HOLDLOCK` and transaction placement | Small, and it protects everything above from concurrent double-action. |
| 10 | **Issue 8** — stop the approval engine writing `Completed` | Pair with Issue 9, which makes the branch reachable today. |
| 11 | **Issue 10** — RETURN must not revert approved expenses | One token. |
| 12 | **Issue 11** — REJECT must cascade to approved expenses | One predicate. |
| 13 | **Issue 12** — route REJECT through `usp_ProcessApprovalAction` | Two lines of C#; retires a duplicate owner. |
| 14 | **Issue 13** — enforce child-expense completeness in SQL, and stop APPROVE sweeping expenses | Delivers requirements 5 and 7. |
| 15 | **Issue 15** — one recalculation owner, invoked by every mutating path | Highest regression risk in this tier. |
| 16 | **Issue 9** — restore L2 and the auditor lookup | Routing change; needs its own UAT cycle. |
| 17 | **Issue 16** — allowed-state guard on reopen | Mostly closed by Issue 4; complete it in SQL. |
| 18 | **Issue 14** — persist expense adjustments at ReadyForPayment | Scope the snapshot step only; partial-amount approval is a separate work item. |
| 19 | **Issue 17** — Finance step | Requires a business decision before any code. |

### 4.3 Medium priority

Issue 21 (history completeness) → Issue 19 (one definition of latest version) → Issue 20 (reject terminal and duplicate actions) → Issue 24 (submit idempotency) → Issue 25 (fold resubmit sync into the procedure) → Issue 26 (SQL-authoritative validation; delete or wire up the dead cap check) → Issue 23 (expense remarks) → Issue 22 (approved level) → Issue 27 (`THROW` and error classification) → Issue 28 (delete dead handlers, isolated commit).

### 4.4 Low priority

Issue 29 (page permissions) → Issue 31 (config, credential rotation, session key casing, read-endpoint ownership) → Issue 32 (attachment magic bytes).

---

## 5. Requirement coverage

| # | Review objective | Status | Issues |
| --- | --- | --- | --- |
| 1 | Authorization on every action | **Fails** | 1, 2, 4, 6, 29 |
| 2 | Valid transitions only, one path between states | **Fails** | 5, 8, 12, 16, 20 |
| 3 | One procedure owns each transition | **Fails** | 5, 12, 15, 21 |
| 4 | Forward blocked on pending/returned days and expenses | **Partial** — enforced in `EnsureCanFinalizeRequest`, bypassed by SP auto-advance | 5, 13 |
| 5 | Only Auditor may set ReadyForPayment; all details resolved first | **Partial** — role checked in C# only; `Completed` reachable without an auditor | 1, 8, 9 |
| 6 | Only the latest DetailVersion is modifiable | **Partial** — enforced in the SP under one of three definitions | 19, 20 |
| 7 | No approval with incomplete child expenses; no orphan expense states | **Fails** | 10, 11, 13 |
| 8 | Single transaction, rollback, XACT_ABORT, no partial updates | **Partial** — correct inside `usp_ProcessApprovalAction`; absent across multi-step operations | 7, 18, 25 |
| 9 | Consistent history for every transition | **Fails** | 21 |
| 10 | Totals always from the latest detail snapshots, no stale or duplicate calculation | **Fails** | 15, 19 |
| 11 | Everything read-only after Completed | **Fails** | 2, 3 |
| 12 | Business rules defined once, preferably in SQL | **Fails** | 15, 19, 26 |

---

## 6. Awaiting approval

No code was changed. Recommended next step is to approve, in order: (a) Issue 30, so the remaining work is reviewable; then (b) a single implementation pass covering Issues 2, 1, 4, and 3, which together close the authorization and payment-lock gaps and are individually low-risk; then (c) the remaining Critical and High items in the order given in §4.
