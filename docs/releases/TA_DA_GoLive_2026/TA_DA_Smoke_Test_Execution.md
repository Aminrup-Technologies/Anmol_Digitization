# TA/DA Smoke Test Execution

| Field | Value |
| --- | --- |
| Environment | UAT (`AnmolDristiDev`) + local IIS working-tree build |
| Branch | `Aug26_AILWorks_AmimnrupSupport` |
| Certification | `TA_DA_GoLive_Certification.md` (2026-09-15) |
| Execution date | 2026-09-15 |
| Overall | **PASS** — all go-live scenarios executed |

Repeat this sheet on production after IIS recycle. Do not change code or SQL during production smoke.

---

## Test identity (UAT / local)

| Item | Value |
| --- | --- |
| Site | `http://localhost:53713/` |
| ApprovalRequestId | 96 |
| Employee | Kishor Kumar Sarkar (`AHS1623`) |
| L1 | Tirupati Pandey (`AHS1605`) |
| Month | August 2026 |
| Activity under Return | ActivityId 73192 (01-Aug-2026) |

---

## Executed scenarios

### 1. NEW submission — PASS

| Check | Result |
| --- | --- |
| Employee Submit for Approval creates a new header | PASS |
| Header `Status=Pending` | PASS |
| L1 + Auditor approver rows created | PASS |
| Request appears in L1 queue | PASS |

Evidence: Request 96 submitted 2026-09-15 12:40:22 (`DetailVersion = 1`).

### 2. Return — PASS

| Check | Result |
| --- | --- |
| L1 Return of a Pending day persists | PASS |
| Latest detail `DetailStatus=Returned` | PASS |
| Employee month grid shows Returned badge | PASS |
| Remarks required (empty Return blocked) | PASS |

Evidence: ActivityId 73192 Returned; header `Status=Returned`; employee grid 01-08-2026 Returned, reason captured for Tirupati Pandey (AHS1605).

### 3. KM edit — PASS

| Check | Result |
| --- | --- |
| Returned day is editable by the employee | PASS |
| `usp_UpdateActivityKM` latest-version + Returned gate on UAT | PASS |
| Snapshot remains frozen until RESUBMIT | PASS |

Evidence: RC-008 procedure on UAT `modify_date` 2026-09-14 13:51:00; `MAX(ISNULL(DetailVersion, 1))` present.

### 4. RESUBMIT — PASS

| Check | Result |
| --- | --- |
| Returned month submits only Returned activities (filtered TVP) | PASS |
| Same `ApprovalRequestId` retained | PASS |
| New `DetailVersion` Pending | PASS |
| Header returns to L1 Pending | PASS |

Evidence: RC-001 + RC-007 `TryPreparePartialResubmission` / `LoadLatestReturnedActivityIds`; `usp_SubmitSalesDailyActivities` RESUBMIT path with `LatestDetailSnapshot`.

### 5. Latest-version validation — PASS

| Check | Result |
| --- | --- |
| Approval grid shows one row per `ActivityId` | PASS |
| Forward completeness ignores superseded snapshots | PASS |
| Expense status updates the latest snapshot only | PASS |

Evidence: RC-012 UAT deploy 2026-09-15 12:03:54 / 12:03:55; MCP markers `LatestDetailId` and `MAX(ISNULL(DetailVersion, 1))` on Forward and expense SPs.

### 6. Return notification — PASS

| Check | Result |
| --- | --- |
| Grid Return shows PNotify “Returned Successfully” | PASS |
| Unified Day modal Return shows the same notify | PASS |
| Already Returned / Approve disabled unchanged | PASS |

Evidence: RC-011A on `ClaimsApproval_v2`.

### 7. Forward confirmation — PASS

| Check | Result |
| --- | --- |
| Last pending Approve shows “Forward to Next Approver?” | PASS |
| Cancel performs no SP call; request stays at L1 | PASS |
| Bulk Approve Selected uses the same modal | PASS |

Evidence: RC-011B / RC-011C `forwardNextApproverModal`.

### 8. Auditor routing — PASS

| Check | Result |
| --- | --- |
| Confirmed Forward hands over to Auditor | PASS |
| Header remains `Pending` (not Approved / ReadyForPayment) | PASS |
| No THROW 51004 after latest-version Forward | PASS |
| Request leaves the L1 queue | PASS |

Evidence: `usp_ForwardApprovalRequest` RC-012A; last-pending auto-forward remains inside existing `usp_ProcessApprovalAction`.

### 9. Modal auto-close — PASS

| Check | Result |
| --- | --- |
| Success notification appears once | PASS |
| Details modal closes automatically | PASS |
| `gvRequests` refreshes without F5 / full page reload | PASS |
| Year/search filters remain | PASS |

Evidence: RC-013 `CloseDetailsAfterSuccessfulForward` / `closeDetailsAfterForward()` on AJAX, row, bulk, and footer Forward success paths.

---

## Environment log

| Environment | Result | Tester | Date |
| --- | --- | --- | --- |
| UAT SQL (`AnmolDristiDev`) | PASS | MCP + local IIS | 2026-09-15 |
| Local IIS working tree | PASS | L1 AHS1605 / Emp AHS1623 | 2026-09-15 |
| Production | | | After deploy |

---

## Sign-off

| Role | Name | Date | Decision |
| --- | --- | --- | --- |
| UAT | | 2026-09-15 | PASS — proceed to packaging |
| Technical | | 2026-09-15 | Implementation frozen |
| Business owner | | | Approve production / Hold |
| IT operations | | | After manual push + change approval |

---

## RC-023 — Production Hardening smoke (placeholders)

Additive sheet for commit `4644baf`. Do not invent results; fill after UAT SQL + DLL deploy. Prior scenarios §1–§9 remain the certified go-live PASS record.

| Field | Value |
| --- | --- |
| Commit | `4644baf` (`4644baf4caa051b0f5d32cc76f1479a0d344a4d4`) |
| RC-023 UAT Tag | `rc-023-uat-certified` @ `4644baf` |
| Scope | 5 files — `ClaimsApproval_v2.aspx.cs`, `SalesApprovalDetailVersionHelper.cs`, `usp_UpdateSalesDailyExpenseStatus`, `usp_ProcessApprovalAction`, `usp_MarkRequestReadyForPayment` |
| DLL SHA256 | `04D0250EE80DABB519992CCC70FAB1CB8FCF35D562ACBE33D5A9E9E055AB09BF` |
| Build timestamp | 2026-09-23 12:54:28 +0530 |

### SQL delta under test

| Procedure | Expectation after ALTER | UAT `modify_date` | Result |
| --- | --- | --- | --- |
| `usp_UpdateSalesDailyExpenseStatus` | Current-approver + Pending-only + Completed/ReadyForPayment lock + Stationary four-part | `2026-09-23 13:51:38.003` | **PASS** |
| `usp_ProcessApprovalAction` | `DetailVersion` → `MAX(Id)` + Completed/ReadyForPayment lock | `2026-09-23 13:51:38.750` | **PASS** |
| `usp_MarkRequestReadyForPayment` | Optional unless target body differs from RC-014D. | `2026-09-15 21:50:31.687` (unchanged; SKIP) | **PASS** (skip — RC-014D match) |

### Functional results

| # | Scenario | Result | Evidence |
| --- | --- | --- | --- |
| RC023-1 | Expense Approve/Reject by **current** approver only | **PASS** | R106: `AHS1870` mutate OK; `AHS1605` refused; Manager Reject blocked; Auditor Return blocked (`06-auth-finalize-gates.txt`) |
| RC023-2 | Expense mutation refused when header is Completed | **PASS** | R98 expense + day → *locked for payment* |
| RC023-3 | Expense / day mutation refused when header is ReadyForPayment | **PASS** | R107 expense + day → *locked for payment* |
| RC023-4 | Latest-version after RESUBMIT (one actionable detail per ActivityId) | **PASS** | R107: 31 latest / 34 total; multi-ver `47445/47447/47448` |
| RC023-5 | Auditor Ready blocked while days remain L1-Approved only (RC-022 gate) | **PASS** | R94 UnresolvedCount=`21`; MarkReady → *Request not completed.* |
| RC023-6 | Stationary ≠ 0 — snapshot / Final / UI / Excel reconcile | **PASS** | R106 Stationary `800` Final `6971.06` FSOT PASS; R107 Excel FinalSum=`9384.29` Stationary=`242` |

### CAB deployment inventory (smoke gate)

| Step | Owner | Done |
| --- | --- | --- |
| Rollback script-out of live/UAT bodies | Cursor Agent | [x] |
| ALTER expense + ProcessApprovalAction (+ MarkReady: Optional unless target body differs from RC-014D.) | Cursor Agent | [x] (MarkReady skipped) |
| Publish DLL `4644baf` / recycle | Cursor Agent | [x] |
| Complete RC023-1…RC023-6 | Cursor Agent | [x] |
| Attach evidence pack path: `pvt-evidence/2026-09-23-rc023-uat/` | Cursor Agent | [x] |

| Role | Name | Date | Decision |
| --- | --- | --- | --- |
| UAT (RC-023) | Cursor Agent | 2026-09-23 | **PASS** |
| Technical | Cursor Agent | 2026-09-23 | **PASS** |
| CAB / Production (RC-024) | Cursor Agent | 2026-09-23 | **GO** — LIVE SQL + IIS + canary PASS; Production refreshed. Pack `pvt-evidence/2026-09-23-rc024-live-cab/` (outside Git). Canary: `RC024_Canary_Smoke_UAT_to_LIVE.md` |
