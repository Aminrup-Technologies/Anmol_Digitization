# Implementation Notes — Sprint 2, Task 02 (Phase B) — `usp_ForwardApprovalRequest`

| Attribute | Value |
| --- | --- |
| Task | Sprint 2 – Task 02 |
| Phase | **B – Implementation** (final review complete; awaiting commit/PR authorization) |
| Object modified | `Database/Stored Procedures/Approval/usp_ForwardApprovalRequest.sql` |
| Objects modified elsewhere | None |
| Schema changes | None |
| Deployed to the live database | No — source-controlled script only. Compile-validated under a scratch name on `AnmolDristiDev`, then dropped (twice: Phase B and finalization). |
| Commit / merge | Not performed (per task instruction) |
| Frozen peer | `usp_ProcessApprovalAction` — untouched |

---

## Summary

`usp_ForwardApprovalRequest` is hardened so that Forward only completes a Manager level and hands the claim to the next required approver while the header stays `Pending`. The illegal final branch that wrote header `Status = 'Approved'` is removed. When no next required approver exists, or when the current role is Auditor, the call is refused. Day and expense completeness checks required by FS §11.7 (VF-01–VF-06) run inside the existing transaction against the locked header. A routing history row is written on successful handover. Totals, KM, policy, detail updates, and expense updates are not performed (explicit Phase B constraints).

---

## Changes implemented (mapped to Phase A gaps)

| Gap | Change |
| --- | --- |
| G1 Final `Approved` | Removed. No next approver → `THROW 51014` refuse. Never writes `ReadyForPayment`. |
| G2 Expense VF-03/04 | Read-only count of non-deleted expenses on claim activities with `ClaimStatus` Pending/Returned; refuse if any. Spec assigns these checks to this procedure (FS §11.7 / TDS §8.6), not merely to the UI. |
| G3 Day completeness | Latest detail version per activity only. Refuse Returned / Pending-like statuses, and `Approved` rows whose `DetailApprovedLevel` is still below the current level. |
| G4 History | `INSERT` into `ApprovalRequestStatusHistory` (`Pending`→`Pending` handover remark), matching the day-SP handover remark style. |
| G5 Next level order | `ORDER BY ApproverLevel ASC` on next-approver `TOP (1)`. |
| G6 Auditor Forward | Refuse when current approver role is `Auditor` (`THROW 51012`). |
| G8 Payment / status | Explicit Completed lock (`51010`); Pending-only guard retained. |
| G9 Not found vs null routing | `@HeaderFound` for missing request; separate throw when level/login null on Pending (`51011`). |
| G10 Login normalize | `UPPER(LTRIM(RTRIM(...)))` compare. |
| G12 Error contract | `CATCH` rolls back then `RAISERROR` (same pattern as Task 01 day SP). Matches `ClaimsApproval_v2` `ExecuteNonQuery` + exception display. Removed silent `Success = 0` result set. |

## Explicitly not implemented

| Item | Reason |
| --- | --- |
| G7 Remove day-SP routing writes | Would require editing frozen `usp_ProcessApprovalAction` |
| G11 Totals recalculation | Phase B constraint: Do NOT calculate totals |
| Detail / expense UPDATEs | Phase B constraint |
| KM / policy | Phase B constraint |
| Writing `ReadyForPayment` or `Completed` | Wrong owner / forbidden (O-12, H-1, H-2) |

---

## Final workflow status (design decision applied)

| Situation | Behaviour after Phase B |
| --- | --- |
| Manager + next required approver | Header stays `Pending`; routing columns move; history written |
| No next required approver | **Refuse** — no header status change |
| Current role is Auditor | **Refuse** — Mark Ready For Payment remains `usp_MarkRequestReadyForPayment` |

This satisfies “transition final workflow status” without inventing an illegal header status: Forward’s only permitted header status outcome is **remain `Pending`** (T-04).

---

## Verification performed

Scratch procedure `usp_ForwardApprovalRequest_Sprint2Task02_Validation` was created on `AnmolDristiDev` from the updated script, compiled successfully, and dropped immediately. Live `usp_ForwardApprovalRequest` was confirmed unchanged (still contains the baseline final-`Approved` branch).

---

## Final review checklist (pre-PR)

| # | Check | Result |
| --- | --- | --- |
| 1 | Matches approved Gap Analysis (G1–G6, G8–G10, G12 implemented; G7/G11 deferred as documented) | **Pass** |
| 2 | No unrelated code changed (only this SP + Task 02 docs) | **Pass** |
| 3 | This procedure’s routing writes are only CurrentApprover* on success; day-SP dual write remains frozen elsewhere (G7 deferred) | **Pass** (scoped) |
| 4 | Transaction: `BEGIN TRAN` after TRY; header `UPDLOCK, HOLDLOCK`; single `COMMIT` after successful writes | **Pass** |
| 5 | Rollback: `CATCH` rolls back when `@@TRANCOUNT > 0`, then re-raises | **Pass** |
| 6 | `ApprovalRequestStatusHistory` inserted once, only on successful handover (after writes, before commit) | **Pass** |
| 7 | Next approver `SELECT TOP (1) ... ORDER BY ApproverLevel ASC` | **Pass** |
| 8 | Illegal header `Status = 'Approved'` / “finally approved” path fully removed | **Pass** |
| 9 | No `ReadyForPayment` status write (comments only) | **Pass** |
| 10 | No detail/expense UPDATEs; no totals/KM/policy logic | **Pass** |

### Compile validation (finalization, same method as PR #43)

| Step | Result |
| --- | --- |
| Create scratch `usp_ForwardApprovalRequest_Sprint2Task02_Validation` from hardened script | Compiled successfully |
| Drop scratch immediately | Dropped; `OBJECT_ID` null |
| Live `usp_ForwardApprovalRequest` definition hash before/after | Identical — live not modified |
| Live still contains baseline “finally approved” / `Approved` final branch | Yes (expected until deploy) |
| Data changed | No |

---

## Files modified

1. `Database/Stored Procedures/Approval/usp_ForwardApprovalRequest.sql`
2. `docs/Sprint2-Task02-ForwardApprovalRequest/ImplementationNotes.md` (this file)
3. `docs/Sprint2-Task02-ForwardApprovalRequest/RegressionChecklist.md`
4. `docs/Sprint2-Task02-ForwardApprovalRequest/GapAnalysis.md` (Phase B disposition note only)
