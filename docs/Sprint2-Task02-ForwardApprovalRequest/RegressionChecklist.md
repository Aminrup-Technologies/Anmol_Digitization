# Regression Checklist — Sprint 2, Task 02 — `usp_ForwardApprovalRequest`

**Phase B:** Expected results below match the hardened source script  
`Database/Stored Procedures/Approval/usp_ForwardApprovalRequest.sql`.  
Deploy the script to a non-production database before executing these tests. The live `AnmolDristiDev` procedure was **not** replaced during compile validation.

Call the procedure with `@ApprovalRequestId` / `@ActionBy`. Failures now surface as SQL errors (`RAISERROR` after rollback), not as a `Success = 0` result set. Success still returns the `Success`/`Message`/routing result set with `RequestStatus = Pending`.

Use disposable data. Do not leave test rows that could be mistaken for real claims.

---

## Final-status and routing

| # | Test | Setup | Action | Expected result |
| --- | --- | --- | --- | --- |
| 1 | **Manager forward with next level** | `Pending` at L1; current login = manager; all latest days resolved at L1 (`DetailApprovedLevel >= 1`); no Pending/Returned expenses; required L2 Pending | Call with that manager | Succeeds. Header stays `Pending`. Routing → L2. L1 approver row `Approved`. History remark contains level completed / forwarded. |
| 2 | **No next approver (Manager)** | `Pending` at final Manager level; no further required Pending approver | Call with current manager | Error `51014` / message about no next required approver. Header unchanged (still `Pending`, routing unchanged). No history row. |
| 3 | **Auditor attempts Forward** | `Pending` at Auditor level; current approver is Auditor | Call with Auditor | Error `51012`. Header unchanged. |
| 4 | **ReadyForPayment not written** | Cases #1–#3 | Inspect header | `Status` is never `ReadyForPayment` and never `Approved` after this procedure. |
| 5 | **Mark Ready For Payment still separate** | Eligible Auditor claim | Call `usp_MarkRequestReadyForPayment` | Unchanged ownership; not part of Forward. |

---

## Authorization and state guards

| # | Test | Setup | Action | Expected result |
| --- | --- | --- | --- | --- |
| 6 | **Wrong approver** | `Pending`; `CurrentApproverLoginId = 'MGR001'` | `@ActionBy = 'MGR002'` | Error `51003`. No writes. |
| 7 | **Case / whitespace login** | `CurrentApproverLoginId = 'mgr001'` | `@ActionBy = 'MGR001'` or `' MGR001 '` | Succeeds when other preconditions met. |
| 8 | **Completed payment lock** | `Status = 'Completed'` | Call | Error `51010`. |
| 9 | **Non-Pending (other)** | `Returned` / `ReadyForPayment` / `Submitted` / `Draft` | Call | Error `51002`. |
| 10 | **Missing request** | Non-existent id | Call | Error `51001`. |
| 11 | **Pending, null routing** | Existing `Pending` row; `CurrentApproverLevel` or login null | Call | Error `51011` (not “not found”). |

---

## Completeness (VF-01–VF-05)

| # | Test | Setup | Action | Expected result |
| --- | --- | --- | --- | --- |
| 12 | **Pending day** | Latest detail `Pending` | Forward | Error `51004`. |
| 13 | **Returned day** | Latest detail `Returned` | Forward | Error `51004`. |
| 14 | **Prior-level Approved not resolved at current level** | At L2; latest detail `Approved` with `DetailApprovedLevel = 1` | L2 forwards | Error `51004`. |
| 15 | **Pending expense** | Days resolved at level; one expense `Pending` | Forward | Error `51013`. |
| 16 | **Returned expense** | Days resolved; one expense `Returned` | Forward | Error `51013`. |
| 17 | **Historical detail noise** | Older version Pending/Returned; latest resolved at level | Forward | Succeeds (latest-only). |
| 18 | **Next level order** | Required levels 2 and 4 Pending; current 1 | Forward | Moves to level **2**. |

---

## Side effects

| # | Test | Setup | Action | Expected result |
| --- | --- | --- | --- | --- |
| 19 | **Current approver completed** | Happy path #1 | Inspect approver row | Current level `Approved`; `ActionBy`/`ActionDate` set. |
| 20 | **Next activated / pointed** | Happy path | Inspect header + next row | Header points at next; next row `Pending`. |
| 21 | **Double forward** | Current approver row already `Approved` while header still points here | Forward | Error `51005`. No partial routing move. |
| 22 | **History on success only** | Happy vs refuse | Inspect history | One handover row on success; none on refuse. |
| 23 | **No detail/expense/totals writes** | Happy path | Compare detail, expense, approved-total columns before/after | Unchanged by this procedure. |

---

## Concurrency and error contract

| # | Test | Setup | Action | Expected result |
| --- | --- | --- | --- | --- |
| 24 | **Concurrent forward** | Two sessions, same claim | Both call | One succeeds; other fails; no torn routing. |
| 25 | **Error reaches ExecuteNonQuery caller** | Force refuse (e.g. #6) | Call via same pattern as `ClaimsApproval_v2` | Client receives SqlException / error message (not a silent success). |

---

## Explicit non-goals (do not fail Task 02)

| # | Item | Why |
| --- | --- | --- |
| N1 | Day SP still auto-advances routing | Frozen `usp_ProcessApprovalAction` |
| N2 | Day SP can still set `Completed` | Frozen / deferred G-05 |
| N3 | Frontend button enablement | Out of scope |
| N4 | Finance payment | Phase 2 |

---

## Sign-off

| Check | Pass / Fail | Notes |
| --- | --- | --- |
| No header `Approved` from Forward | | |
| No header `ReadyForPayment` from Forward | | |
| Manager + next → Pending + routing + history | | |
| No next / Auditor → refuse | | |
| Expense + current-level day checks enforced | | |
| `ORDER BY ApproverLevel` verified | | |
| `usp_ProcessApprovalAction` untouched | | |
| Totals/details/expenses not updated by Forward | | |
| Scratch compile-test dropped; live SP unchanged until deploy | | |
