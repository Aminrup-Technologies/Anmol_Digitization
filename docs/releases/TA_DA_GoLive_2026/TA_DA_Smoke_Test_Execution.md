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
