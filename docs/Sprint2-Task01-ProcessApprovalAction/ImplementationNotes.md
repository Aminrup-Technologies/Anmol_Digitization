# Implementation Notes — Sprint 2, Task 01 — `usp_ProcessApprovalAction` Hardening

| Attribute | Value |
| --- | --- |
| Task | Sprint 2 – Task 01 |
| Object modified | `Database/Stored Procedures/Approval/usp_ProcessApprovalAction.sql` |
| Objects modified elsewhere | None |
| Schema changes | None |
| Deployed to the live database | No — this task modifies the source-controlled script only. Deployment is a separate, later step. |
| Status | Complete, awaiting review before merge |

## Summary

`usp_ProcessApprovalAction` processes a single day-level approval decision (APPROVE, RETURN, or REJECT) against one `SalesApprovalRequestDetail` row. Before this task, the procedure enforced no authorization, no header-status precondition, and no role restriction — any authenticated caller who supplied a valid detail identifier could process any of the three actions against a request in any status, regardless of whose turn it was. This task adds four authorization/state guards required by the approved Functional Specification and Technical Design Specification, all evaluated as precondition checks before the transaction opens, in the same style as the guard clauses the procedure already used for input validation. No workflow routing was removed; two routing responsibilities the procedure currently performs were identified, left fully functional, and marked for migration to `usp_ForwardApprovalRequest` in Sprint 2, Task 02.

The procedure's core responsibility — process one approval action — is otherwise unchanged: the same tables are updated, in the same order, with the same cascading rules for the activity and its expenses, the same mixed-state evaluation, the same header totals recalculation, and the same audit history logic.

## Business Rules Implemented

| # | Rule | Where in the procedure |
| --- | --- | --- |
| 1 | **Current Approver validation.** The acting user (`@ActionBy`) must equal the header's `CurrentApproverLoginId`, compared case- and whitespace-insensitively. If the header has no current approver, or the caller does not match, the call is rejected. | New guard, immediately after the header and approver-role lookups, before the action-to-status mapping. |
| 2 | **Header status validation.** The action is processed only while `SalesApprovalRequest.Status = 'Pending'`. `Draft`, `Submitted`, `Returned`, `ReadyForPayment`, and `Completed` are all rejected. | New guard, immediately before the Current Approver check. |
| 3 | **Payment Lock.** A `Completed` request is immutable. This is enforced as its own explicit, separately-commented guard ahead of the general status guard, so the single highest-severity rule in the approved specifications has a dedicated, unambiguous check rather than being folded silently into rule #2. | New guard, first of the four, immediately after the header lookup. |
| 4 | **Role enforcement for day-level actions.** A Manager (any non-Auditor role) may Approve or Return a day but must not Reject one. The Auditor may Approve or Reject a day but must not Return one. | Two new guards, after the Current Approver check. |

## Behaviour Changes

| Before | After |
| --- | --- |
| Any caller could process any action against any detail, regardless of who currently held the request. | Only the current approver named on the header may process an action; anyone else receives a business error and nothing is written. |
| An action could be processed against a request in `Draft`, `Submitted`, `Returned`, `ReadyForPayment`, or `Completed` status. | Only a `Pending` request accepts an action; all other statuses are rejected before the transaction opens. |
| A `Completed` request had no special protection beyond the general status check (which did not exist). | A `Completed` request is rejected by its own explicit, first-checked guard. |
| A Manager could REJECT a day; the Auditor could RETURN a day. | A Manager attempting REJECT, or the Auditor attempting RETURN, receives a business error. |
| The procedure's own routing logic (next-approver lookup, `CurrentApproverLevel`/`CurrentApproverLoginId` update, `Completed` transition) executed with no annotation. | The same logic executes identically, now preceded by `-- TODO (Sprint 2 - Task 02)` comments identifying it for migration. No behavioural change to the routing outcome itself. |

No change was made to: totals recalculation, expense cascade rules, activity/detail snapshot fields, the mixed-state evaluation, the audit history insert conditions, or the error-handling `CATCH` block.

## Deferred Items

These were identified during this task but are explicitly **not** addressed here, per the task's scope and the refined instruction to keep the workflow functional between merges:

| Item | Why deferred | Where it will be addressed |
| --- | --- | --- |
| `usp_ProcessApprovalAction` writing `CurrentApproverLevel`/`CurrentApproverLoginId` on level completion | `usp_ForwardApprovalRequest` has not yet been hardened to own this responsibility. Removing it now would strand every multi-level claim at Level 1. | Sprint 2, Task 02 (per the two `-- TODO (Sprint 2 - Task 02)` markers added to the procedure). |
| `usp_ProcessApprovalAction` writing `Status = 'Completed'` on non-Auditor final-level completion | Same reasoning: this is currently the *only* path in the system that reaches `Completed` (Finance payment processing does not yet exist — Functional Specification §15.1). Removing it now would make every claim permanently unpayable. | Sprint 2, Task 02, in coordination with the eventual Finance payment procedure (Phase 2 per the Functional Specification). |
| The underlying business-rule question of whether a non-Auditor completing the final level *should* reach `Completed` at all (Functional Specification: `Completed` may only be written by Finance) | This is a routing/forward-ownership design question, not a day-level approval-action question, and is explicitly out of scope for this task. | Sprint 2, Task 02, and/or the Finance payment procedure design. |
| Expense-level Approve/Return/Reject role enforcement | Out of scope — belongs to `usp_UpdateSalesDailyExpenseStatus`, a different procedure not touched by this task. | A future task scoped to `usp_UpdateSalesDailyExpenseStatus`. |
| Terminal-state guard on the target detail itself (a `Rejected` detail can still be re-actioned; an already-`Approved` detail at the same level can be re-approved) | Not one of the six numbered business rules for this task. | Recorded in the Production Hardening Review (issue 20) and Technical Design Specification (gap G-13); not addressed here. |
| Error re-raise losing the original error number (`RAISERROR(@ErrMsg, @ErrSeverity, @ErrState)` in the `CATCH` block) | Not one of the six numbered business rules for this task. | Recorded in the Production Hardening Review (issue 27); not addressed here. |
| Header row not read under a lock before the transaction opens (concurrency) | Not one of the six numbered business rules for this task. | Recorded in the Production Hardening Review (issue 18); not addressed here. |

## Regression Risks

| Risk | Assessment |
| --- | --- |
| A legitimate workflow currently relies on an approver acting out of turn (for example, an auditor pre-clearing a day while the claim is still at a manager level). | **Possible.** If any current operational practice depends on this, the Current Approver guard will now block it. This should be confirmed with the business before deployment; the Functional Specification treats out-of-turn action as an authorization gap to be closed, not a feature to preserve. |
| A legitimate call site invokes this procedure against a request whose status is not `Pending` (for example, a retry after the header has already moved on). | **Low.** No call site identified during the review of `ClaimsApproval_v2.aspx.cs` invokes this procedure against a non-Pending header by design; the header status guard should only ever reject calls that were already logically invalid. |
| A Manager-initiated REJECT, or an Auditor-initiated RETURN, is currently reachable and relied upon somewhere. | **Low.** The application layer (`ClaimsApproval_v2.aspx.cs`) already restricts REJECT to the Auditor role and blocks RETURN for the Auditor in its own C# guard clauses; this change makes the same restriction authoritative in the data layer and should not change any currently-successful call pattern. |
| Case or whitespace variance between how `CurrentApproverLoginId` is stored and how `@ActionBy` is supplied by the application causes a legitimate approver to be rejected. | **Low, but worth testing explicitly.** The new guard normalizes both sides with `UPPER(LTRIM(RTRIM(...)))` before comparing, which should absorb any such variance; the Regression Checklist includes a case-variance test. |
| The two TODO-marked routing blocks behave differently than before. | **None expected.** No executable line inside either block was changed; only comments were added above and between existing statements. |
| Any other stored procedure was affected. | **None.** No other object was opened for editing during this task. |

## Verification performed before delivery

The updated procedure body was deployed under a temporary scratch name (`usp_ProcessApprovalAction_Sprint2Task01_Validation`) directly against the live `AnmolDristiDev` database, confirmed to compile without error against the real table and column definitions, and then dropped immediately. The live `usp_ProcessApprovalAction` procedure itself was confirmed unmodified throughout. No data was read, written, or altered by this verification step beyond the creation and immediate removal of the scratch procedure object.

## Final review addendum — `@CurrentApproverRole` casing

During final review before merge, it was asked whether the three comparisons of `@CurrentApproverRole` against the literal `'Auditor'` (the two new guards at lines 180 and 186, and the pre-existing routing check at line 398) should be normalized with `UPPER(LTRIM(RTRIM(ISNULL(@CurrentApproverRole, ''))))` and compared against `'AUDITOR'`, matching the normalization already applied to the `CurrentApproverLoginId`/`@ActionBy` comparison.

**No change was made.** This was verified empirically against the live `AnmolDristiDev` database rather than assumed:

- The database's default collation is `SQL_Latin1_General_CP1_CI_AS` — case-insensitive (`CI`), accent-sensitive (`AS`).
- `SalesApprovalRequestApprover.ApproverRole` carries no column-level `COLLATE` override; it inherits the database default.
- `@CurrentApproverRole` is declared `NVARCHAR(100)` with no `COLLATE` clause, so it also takes the database default collation.
- A direct test replicating the procedure's exact comparison (`ISNULL(@CurrentApproverRole, '') <> 'Auditor'`) against the values `'AUDITOR'`, `'auditor'`, and `'Auditor'` returned the identical result — *allowed* — in all three cases; only `NULL` produced *blocked*, which is the correct, casing-independent, deny-by-default behaviour.
- The only value ever actually stored in `ApproverRole` today is `'Auditor'` (confirmed by querying distinct values: `Auditor`, `L1`, `Reporting_Manager1`).

Under this collation, `UPPER(@CurrentApproverRole) = 'AUDITOR'` and `@CurrentApproverRole = 'Auditor'` are the same comparison and produce the same result for every input, including `NULL`. Adding the normalization would not change behaviour for any current or plausible future value in this column; it would be a stylistic no-op, not a correctness fix. Per the task's own constraint not to modify SQL unless required for correctness, and to avoid unrelated changes, it was left as-is — including at line 398, which is pre-existing routing logic this task does not otherwise touch.

This differs from the `CurrentApproverLoginId`/`@ActionBy` normalization added earlier in this same task: that comparison is between two independently-supplied values (a stored column and an application-supplied parameter) rather than a column against a fixed, single-valued literal, so normalizing it costs nothing and removes any dependency on collation assumptions for that specific check. `@CurrentApproverRole` was not revisited to match, since doing so was not requested and the reasoning above shows it is unnecessary; if a future change to this database's collation is ever made, the recommendation would be to revisit `ApproverRole` comparisons at that time rather than pre-emptively.
