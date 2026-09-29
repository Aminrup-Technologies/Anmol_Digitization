# L2 / Auditor SQL Matrix (UAT `AnmolDristiDev`)

**Connection:** MCP `anmol_uat` → database `AnmolDristiDev`  
**Capture:** 2026-09-15 (definitions / catalog metadata only; no DML)  
**Row counts** below are catalog stats from `describe_table`, not sampled rows.

---

## 1. Stored procedures

### 1.1 `dbo.usp_ProcessApprovalAction`

| Field | UAT value |
| --- | --- |
| create_date | 2026-08-01 10:21:05 UTC |
| modify_date | 2026-08-01 10:21:05 UTC |
| Parameters | `@ApprovalRequestDetailId INT`, `@Action NVARCHAR(20)`, `@ActionBy NVARCHAR(100)`, `@Remarks NVARCHAR(MAX)=NULL` |

Latest-version logic (UAT body):

```
@ApprovalRequestDetailId must equal
  MAX(ApprovalRequestDetailId)
  FROM SalesApprovalRequestDetail
  WHERE ApprovalRequestId = @ApprovalRequestId AND ActivityId = @ActivityId
```

Mixed-state evaluation uses the same `MAX(ApprovalRequestDetailId) GROUP BY ActivityId` set.

Status / routing (UAT live body):

| Action | Detail | Activity | Expenses | Header |
| --- | --- | --- | --- | --- |
| APPROVE | `Approved` | `Approved` | non-Approved/Rejected → `Approved` | If any latest Returned → `Returned`. Else if no pending latest: complete current approver; if next required Pending exists → set CurrentApprover to next, Status stays `Pending`. Else if role ≠ Auditor → `Completed`. Else Auditor → Status stays `Pending`. Partial approve → `Pending`. |
| RETURN | `Returned` | `Returned` | non-Rejected → `Returned` | `Returned`. CurrentApprover **not** reset (resubmit SP resets to L1). |
| REJECT | `Rejected` | `Rejected` | non-Approved → `Rejected` | `Returned` if any latest Returned else `Pending`. |

**Repo drift:** `Database/Stored Procedures/Approval/usp_ProcessApprovalAction.sql` adds current-approver match, Pending-only header guard, Completed payment lock, and role rules (Manager cannot REJECT; Auditor cannot RETURN). Those guards are **not** present in the UAT `modify_date = 2026-08-01` definition returned by MCP.

---

### 1.2 `dbo.usp_ForwardApprovalRequest`

| Field | UAT value |
| --- | --- |
| create_date | 2026-07-30 00:14:06 UTC |
| modify_date | 2026-09-15 12:03:54 UTC |
| Parameters | `@ApprovalRequestId INT`, `@ActionBy NVARCHAR(100)` |

Latest-version logic: completeness counts latest `MAX(DetailVersion)` then `MAX(ApprovalRequestDetailId)` per `ActivityId`.

Routing:

- Header must be `Pending`; Completed is locked.
- `@ActionBy` must match `CurrentApproverLoginId`.
- Current role `Auditor` → `THROW 51012` (use Mark Ready For Payment).
- Unresolved latest days (Pending/Submitted/Returned, or Approved at a lower `DetailApprovedLevel`) → refuse.
- Latest-joined expenses still Pending/Returned → refuse.
- Next required Pending approver `ORDER BY ApproverLevel ASC` required; none → refuse (does **not** write `Approved` or `ReadyForPayment`).
- Success: current approver → `Approved`; header pointer moves; next approver stays/activated `Pending`; header Status remains `Pending`; history remark `Level N completed. Forwarded to {role} ({login})`.

This is the L1 → Auditor handover.

---

### 1.3 `dbo.usp_GetPendingApprovalsForUser_Filters`

| Field | UAT value |
| --- | --- |
| create_date | 2026-03-10 10:35:20 UTC |
| modify_date | 2026-06-15 11:10:29 UTC |
| Parameters | `@ApproverLoginId`, `@Year INT=NULL`, `@Month INT=NULL`, `@EmpCode NVARCHAR=NULL` |

Filter: `sar.Status = 'Pending'` AND `sar.CurrentApproverLoginId = @ApproverLoginId` AND join `saa.Status = 'Pending' AND saa.ApproverLoginId = @ApproverLoginId`. Optional year/month/employee LIKE. Order: `RequestDate DESC, ApprovalRequestId DESC`.

No detail-version predicate (header queue only). Returns `ApproverRole`, `PendingWithRole`, `ApprovalProgress`.

---

### 1.4 `dbo.usp_GetApprovalRequestDetails`

| Field | UAT value |
| --- | --- |
| create_date | 2026-01-03 18:29:14 UTC |
| modify_date | 2026-07-30 01:53:16 UTC |
| Parameters | `@ApprovalRequestId INT` |

Returns **all** `SalesApprovalRequestDetail` rows for the request (`ORDER BY ApprovalRequestDetailId`). No latest-version filter. C# applies `KeepLatestPerActivity` after fill.

Includes snapshot amounts, `DetailStatus` / `DetailApprovedLevel`, `IsFinalApproved` (Approved at MAX required ApproverLevel), expense JSON (`IsDeleted = 0`; Submitted/Draft/'' mapped to `Pending`), `ManualReview` / `BulkEligible` (Hotel/Travel/Stationary).

---

### 1.5 `dbo.usp_UpdateSalesDailyExpenseStatus`

| Field | UAT value |
| --- | --- |
| create_date | 2026-02-17 12:05:28 UTC |
| modify_date | 2026-09-15 12:03:55 UTC |
| Parameters | `@ExpenseId`, `@NewStatus NVARCHAR(50)`, `@ActionBy`, `@Remarks NVARCHAR(MAX)=NULL` |

Latest-version logic (header/snapshot rewrite): for the expense’s `ActivityId`, take `MAX(DetailVersion)` then `MAX(ApprovalRequestDetailId)`.

Behavior:

- Refuses deleted expenses.
- Updates `SalesDailyExpense.ClaimStatus`, `Remarks`, `ClaimProcessedBy/Date`.
- Rewrites latest detail snapshot DA/Hotel/Travel (excludes Rejected); `Snapshot_ExpenseTotal = DA+Hotel+Travel` (Stationary not included in this SP’s total).
- Rewrites header `TotalClaimAmount` / `TotalExpenseAmount` from latest snapshots (includes Returned latest rows).
- Does **not** validate `@NewStatus` against Approved/Rejected/Returned.
- Does **not** enforce Auditor vs Manager.
- Comment in header still says `'Approved' or 'Rejected'`; C# also sends `Returned`.

---

### 1.6 `dbo.usp_SubmitSalesDailyActivities` (upstream)

| Field | UAT value |
| --- | --- |
| create_date | 2026-07-01 17:55:54 UTC |
| modify_date | 2026-07-28 12:39:02 UTC |
| Parameters | `@Updates TVP_ActivityUpdate`, `@LoginId`, `@Year`, `@Month`, `@Remarks`, `@L1Code`, `@L2Code`, `@L3Code`, `@AuditorCode` |

Latest-version: insert snapshot with `ISNULL(lds.DetailVersion,0)+1` and `PreviousDetailId`; totals use `ROW_NUMBER` on `DetailVersion DESC, ApprovalRequestDetailId DESC`.

Routing: NEW builds L1 + optional L2/L3 + Auditor. RESUBMIT resets every approver to Pending and points header at L1. Requires `@L1Code` and `@AuditorCode`; they must differ.

---

### 1.7 Additional SPs touched by the Auditor page (not in the original retrieve list)

| Procedure | modify_date (UAT) | Parameters | Auditor relevance |
| --- | --- | --- | --- |
| `usp_GetApprovalRequest` | 2026-07-30 01:09:05 | `@ApprovalRequestId` | Header + `MaxApproverLevel` for final-approver flag |
| `usp_MarkRequestReadyForPayment` | 2026-01-09 17:48:18 | `@ApprovalRequestId`, `@ActionBy` | Actor must be ApproverRole Auditor. Header must already be `Completed`. Then `ReadyForPayment`. |
| `usp_ProcessSalesApprovalRequestDetail` | 2026-07-23 15:08:59 | `@ApprovalRequestDetailId`, `@ApprovalRequestId`, `@Action` APPROVE/REJECT, `@ActionBy`, `@ActionRemarks` | Auditor day **Reject** path. Current-approver check. **No latest-version check.** REJECT does not update expenses (C# does). |
| `usp_ReturnClaimToEmployee` | 2026-06-15 11:10:29 | `@ApprovalRequestId`, `@ActionBy`, `@ReturnReason` | Whole-claim return; hidden for Auditor |
| `usp_UpdateActivityKM_WithHistory` | 2026-07-22 10:58:44 | `@ActivityId`, `@NewKM`, `@ChangedBy`, `@Reason` | Unified modal KM edit |

---

## 2. Tables

### 2.1 `dbo.SalesApprovalRequest`

| | |
| --- | --- |
| PK | `ApprovalRequestId` (identity) |
| FKs | none declared on this table |
| Status | `Status NVARCHAR(50)` default `'Pending'` |
| Routing | `CurrentApproverLoginId`, `CurrentApproverLevel` |
| Latest-version | none (header). Counts `TotalActivities` / `PendingActivities` / `ReturnedActivities` exist as columns; submit SP maintains `TotalActivities` via per-activity max `DetailVersion`. |
| Money | submitted totals (`Total*`) vs approved totals (`Approved*`); computed `ApprovedExpenseAmount`, `TotalApprovedAmount` |
| UAT stats | created 2026-01-04; modified 2026-07-31; ~90 rows |

Indexes: PK, `IX_SalesApprovalRequest_Employee (RequesterLoginId)`, `IX_SalesApprovalRequest_Status (Status)`.

---

### 2.2 `dbo.SalesApprovalRequestDetail`

| | |
| --- | --- |
| PK | `ApprovalRequestDetailId` (identity) |
| FKs | `ApprovalRequestId` → `SalesApprovalRequest`; `ActivityId` → `SalesDailyActivity` |
| Latest-version | `DetailVersion INT NOT NULL default 1`; `PreviousDetailId INT NULL` |
| Status | `DetailStatus`; audit `DetailApprovedBy/On/Level`, `DetailRemarks` |
| Snapshots | KM, claim, DA, Hotel, Travel, Stationary, ExpenseTotal |
| UAT stats | modified 2026-07-29; ~2638 rows |

Index `IX_SalesApprovalRequestDetail_Request_Activity_Version (ApprovalRequestId, ActivityId, DetailVersion DESC)`.

---

### 2.3 `dbo.SalesApprovalRequestApprover`

| | |
| --- | --- |
| PK | `ApprovalRequestApproverId` |
| FKs | `ApprovalRequestId` → `SalesApprovalRequest` |
| Routing | `ApproverLoginId`, `ApproverRole` (`L1`/`L2`/`L3`/`Auditor`), `ApproverLevel`, `IsRequired` |
| Status | `Status` default `'Pending'`; `ActionBy`, `ActionDate`, `Remarks` |
| UAT stats | ~180 rows |

Filtered unique index (repo DDL; MCP `list_indexes` omits the filter predicate):

```
UX_SAR_Auditor_Once ON (ApprovalRequestId) WHERE (ApproverRole='Auditor')
```

One Auditor row per request.

---

### 2.4 `dbo.ApprovalRequestStatusHistory`

| | |
| --- | --- |
| PK | `HistoryId` |
| FKs | `ApprovalRequestId` → header; `ApprovalRequestDetailId` → detail (nullable) |
| Status | `OldStatus`, `NewStatus` |
| Routing extras (nullable) | `ActivityId`, `ApproverLevel`, `ActionType`, `DetailVersion` |
| UAT stats | ~645 rows |

Forward and ProcessApprovalAction insert header-level history. Several inserts do not populate the extra routing columns.

---

### 2.5 `dbo.SalesDailyExpense`

| | |
| --- | --- |
| PK | `ExpenseId` |
| FKs | `ActivityId` → `SalesDailyActivity` |
| Status (authoritative in this flow) | `ClaimStatus` default `'Draft'` |
| Parallel unused-looking columns | `ApprovalStatus`, `ApprovalRemarks`, `ApprovedBy`, `ApprovedOn` — not written by the SPs listed above |
| Routing | none (parent activity / request) |
| Latest-version | none; versioning lives on `SalesApprovalRequestDetail` |
| UAT stats | ~2062 rows |

Checks: `Amount >= 0`; `IsDeleted = 0 OR ClaimStatus = 'Draft'`.

---

## 3. Latest-version compliance matrix

Verdicts are against the **Auditor click path** as implemented in `ClaimsApproval_v2` plus **UAT** procedure bodies.

| Consumer | Latest-only? | Evidence |
| --- | --- | --- |
| Grid (`BindDetailsGrid`) | PASS (UI layer) | SP returns all versions; `KeepLatestPerActivity` keeps `DetailVersion` then `ApprovalRequestDetailId`. Footer SQL uses `LatestDetailWhereForD` (`MAX(ApprovalRequestDetailId)`). |
| Request modal (same bind) | PASS (UI layer) | Same as grid. Version badge shown when `DetailVersion > 1`. |
| Unified Day modal | PASS (indirect) | `lnkReview` `data-detailid` comes from the filtered grid row. `ActionDayRowSilent` then hits `usp_ProcessApprovalAction` (MAX Id) or Reject SP (no version check). |
| Bulk Approve | PASS | `LoadBulkDetailContextsFromDatabase` + `KeepLatestPerActivity`; non-latest selected IDs get `IsLatestDetailVersion = false` and skip. Then `usp_ProcessApprovalAction` re-checks MAX Id. |
| Bulk Return | N/A (Auditor) | C# exits: “Auditors cannot return days.” Manager path would use the same latest context loader. |
| Bulk Reject | PARTIAL | Latest filter on selection exists, but persist is `usp_ProcessSalesApprovalRequestDetail` (no MAX-version/Id guard). |
| Expense approval | PASS (UAT 2026-09-15) | `usp_UpdateSalesDailyExpenseStatus` rewrites snapshot on `MAX(DetailVersion)` then `MAX(ApprovalRequestDetailId)`. Expense row itself is keyed by `ExpenseId` (expenses are not versioned). |
| Receipt approval | PASS (same SP) | Same as expense approval; UI `Preview Receipt` is path-only. |
| Day Approve (SP) | PASS (Id-only) | UAT `usp_ProcessApprovalAction` refuses unless detail id = `MAX(ApprovalRequestDetailId)` for that activity. Does not use `DetailVersion`. |
| Day Reject (SP) | FAIL | UAT `usp_ProcessSalesApprovalRequestDetail` updates the posted id with no latest predicate. |
| Recalculate | PASS (Id-only) | `ExecuteRecalculationSQL` updates only `MAX(ApprovalRequestDetailId)` per activity. |
| L1 Forward | PASS (version then Id) | UAT `usp_ForwardApprovalRequest` completeness (2026-09-15). |
| Details retrieval SP | FAIL (raw) | `usp_GetApprovalRequestDetails` has no latest filter. |

**Grid/Modal/Bulk Approve/Expense: PASS at the consumer that the Auditor uses, with the predicate split in §4.** Reject persist is the Auditor FAIL.

## 4. Predicate mismatch (latest-version)

| Consumer | Predicate |
| --- | --- |
| C# `LatestDetailWhereForD` / recalc / ProcessApprovalAction (UAT) | `MAX(ApprovalRequestDetailId)` per ActivityId |
| C# `KeepLatestPerActivity` | `DetailVersion DESC`, then `ApprovalRequestDetailId DESC` |
| `usp_ForwardApprovalRequest` (UAT 2026-09-15) | `MAX(DetailVersion)` then `MAX(ApprovalRequestDetailId)` |
| `usp_UpdateSalesDailyExpenseStatus` (UAT 2026-09-15) | same as Forward |
| `usp_SubmitSalesDailyActivities` | `DetailVersion DESC`, then `ApprovalRequestDetailId DESC` |
| `usp_GetApprovalRequestDetails` | **none** |
| `usp_ProcessSalesApprovalRequestDetail` | **none** (exact detail id) |

Identity IDs normally move with new versions, so the two predicates usually agree. They diverge if a lower `DetailVersion` ever received a higher identity, or if a caller posts a superseded `ApprovalRequestDetailId`.
