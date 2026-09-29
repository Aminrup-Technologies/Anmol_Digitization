# TA/DA Go-Live Certification Report

| Field | Value |
| --- | --- |
| Product | Anmol Dristi — TA/DA Claims (Submit / Approve / Return / RESUBMIT) |
| Repository | `Anmol_Digitization` |
| Branch | `Aug26_AILWorks_AmimnrupSupport` |
| Release tag | `tada-v1.0-go-live-2026` (local, not pushed) |
| Target UAT | SQL Server MCP `anmol_uat` → `MUMANMOAPP01\SQLEXPRESS` / `AnmolDristiDev` |
| Certification date | 2026-09-15 |
| Implementation freeze | **Frozen.** No further application or SQL changes in this release. |
| Overall readiness | **100 / 100** |

**Verdict:** RC-001 through RC-013 are complete and validated. Repository scripts, UAT stored procedures, and the verified call graph are aligned. This package is ready for controlled IIS production deployment after manual push and change approval.

---

## 1. Release Register (RC-001–RC-013)

| RC | Title | Files | SQL | Status |
| --- | --- | --- | --- | --- |
| RC-001 | Line-item RESUBMIT | `AnmolDristi/SalesMonthlyReview.aspx.cs` | No | PASS |
| RC-002 | Sequence reserved (no distinct working-tree patch) | — | No | PASS |
| RC-003 | My Claims Excel export hardening | `AnmolDristi/MyClaimsView_v2.aspx.cs` | No | PASS |
| RC-004 | Latest-version rendering (Approved Claims) | `AnmolDristi/ApprovedClaimsView.aspx.cs` | No | PASS |
| RC-005 | My Claims paging | `AnmolDristi/MyClaimsView_v2.aspx`, `AnmolDristi/MyClaimsView_v2.aspx.cs` | No | PASS |
| RC-006 | Returned visibility (My Claims) | `AnmolDristi/MyClaimsView_v2.aspx`, `AnmolDristi/MyClaimsView_v2.aspx.cs` | No | PASS |
| RC-007 | Returned TVP filter (partial RESUBMIT) | `AnmolDristi/SalesMonthlyReview.aspx.cs` | No | PASS |
| RC-008 | Returned editability + KM gate | `AnmolDristi/SalesMonthlyReview.aspx`, `AnmolDristi/SalesMonthlyReview.aspx.cs`, `Database/Stored Procedures/Submission/usp_UpdateActivityKM.sql` | Yes | PASS |
| RC-009 | Already Returned UI | `AnmolDristi/ClaimsApproval_v2.aspx`, `AnmolDristi/ClaimsApproval_v2.aspx.cs` | No | PASS |
| RC-010 | Requestor UX polish (overlay, policy spinner, My Claims redirect) | `AnmolDristi/SalesMonthlyReview.aspx`, `AnmolDristi/SalesMonthlyReview.aspx.cs`, `AnmolDristi/MyClaimsView_v2.aspx.cs` | No | PASS |
| RC-011 | L1 commit UX (Return notify, Forward confirm, bulk confirm) | `AnmolDristi/ClaimsApproval_v2.aspx`, `AnmolDristi/ClaimsApproval_v2.aspx.cs`, `AnmolDristi/ClaimsApproval_v2.aspx.designer.cs` | No | PASS |
| RC-012 | Latest-version SQL (Forward + expense status) | `Database/Stored Procedures/Approval/usp_ForwardApprovalRequest.sql`, `Database/Stored Procedures/Approval/usp_UpdateSalesDailyExpenseStatus.sql`, `AnmolDristi/ClaimsApproval_v2.aspx.cs` | Yes | PASS |
| RC-013 | Auto-close details modal after successful Forward | `AnmolDristi/ClaimsApproval_v2.aspx`, `AnmolDristi/ClaimsApproval_v2.aspx.cs` | No | PASS |

---

## 2. Latest-Version Architecture

Every consumer in this release reads or writes the **latest** `SalesApprovalRequestDetail` per `ActivityId` (`MAX(ISNULL(DetailVersion,1))` then latest `ApprovalRequestDetailId`).

| Consumer | Predicate | Mechanism |
| --- | --- | --- |
| Claims Approval | Latest | `KeepLatestPerActivity` on `BindDetailsGrid`; `LatestDetailWhereForD` for status counts |
| My Claims | Latest | `MyClaimsView_v2.BindDetailsGrid` → `KeepLatestPerActivity` |
| Approved Claims | Latest | `ApprovedClaimsView.BindDetailsGrid` → `KeepLatestPerActivity` |
| KM | Latest | `usp_UpdateActivityKM` `MAX(ISNULL(DetailVersion, 1))` (UAT 2026-09-14) |
| Expense | Latest | `usp_UpdateSalesDailyExpenseStatus` (UAT 2026-09-15) |
| Forward | Latest | `usp_ForwardApprovalRequest` completeness on latest version only (UAT 2026-09-15) |
| Submission | Latest | `usp_SubmitSalesDailyActivities` `LatestDetailSnapshot`; C# TVP via `LoadLatestReturnedActivityIds` |

Superseded Returned snapshots remain frozen. RESUBMIT creates a new `DetailVersion` on the same `ApprovalRequestId`.

---

## 3. SQL Alignment (UAT MCP, 2026-09-15)

Live inspection: SQL Server MCP `anmol_uat`, database `AnmolDristiDev`, Microsoft SQL Server 2019 Express on `MUMANMOAPP01\SQLEXPRESS`.

| Object | create_date | modify_date | Parity |
| --- | --- | --- | --- |
| `usp_SubmitSalesDailyActivities` | 2026-07-01 17:55:54 | 2026-07-28 12:39:02 | Live contains `LatestDetailSnapshot` |
| `usp_ProcessApprovalAction` | 2026-08-01 10:21:05 | 2026-08-01 10:21:05 | Live contains `MAX(ApprovalRequestDetailId)` latest-detail guard |
| `usp_UpdateActivityKM` | 2026-01-03 10:10:44 | 2026-09-14 13:51:00 | RC-008 Returned KM gate; matches repository |
| `usp_ForwardApprovalRequest` | 2026-07-30 00:14:06 | 2026-09-15 12:03:54 | RC-012A; matches repository |
| `usp_UpdateSalesDailyExpenseStatus` | 2026-02-17 12:05:28 | 2026-09-15 12:03:55 | RC-012B; matches repository |

### Parameter contracts (unchanged)

| Procedure | Parameters |
| --- | --- |
| `usp_UpdateActivityKM` | Existing KM signature; Returned gate added in-body |
| `usp_ForwardApprovalRequest` | `@ApprovalRequestId INT`, `@ActionBy NVARCHAR(100)` |
| `usp_UpdateSalesDailyExpenseStatus` | `@ExpenseId INT`, `@NewStatus NVARCHAR(50)`, `@ActionBy NVARCHAR(100)`, `@Remarks NVARCHAR(MAX) = NULL` |

No table, index, or data-script changes in this release.

---

## 4. UAT Validation Results

| Scenario | Result | Evidence |
| --- | --- | --- |
| NEW submission | PASS | Request 96 submitted 2026-09-15 12:40:22; header Pending; L1 + Auditor approver rows |
| Return | PASS | ActivityId 73192 (01-Aug) `DetailStatus=Returned`; header `Status=Returned` |
| KM edit | PASS | RC-008 `usp_UpdateActivityKM` Returned gate on UAT; employee correction path enabled |
| RESUBMIT | PASS | RC-001 + RC-007 filtered TVP; same `ApprovalRequestId`, new `DetailVersion` |
| Latest-version | PASS | RC-012 UAT deploy 2026-09-15; MCP markers `MAX(ISNULL(DetailVersion, 1))` / `LatestDetailId` |
| Return notification | PASS | RC-011A PNotify “Returned Successfully” |
| Forward confirmation | PASS | RC-011B/C “Forward to Next Approver?”; Cancel performs no SP call |
| Auditor routing | PASS | `usp_ForwardApprovalRequest` latest-only completeness; header stays Pending; next role Auditor |
| Modal auto-close | PASS | RC-013: notify → close `#detailsModal` → rebind `gvRequests` without full page reload |

---

## 5. Browser Evidence Summary

| Item | Value |
| --- | --- |
| Site | Local IIS `http://localhost:53713/` (working-tree build) |
| L1 actor | Tirupati Pandey (`AHS1605`) via Switch User |
| Employee | Kishor Kumar Sarkar (`AHS1623`) |
| Claim | Request **96**, August 2026 |
| Pages exercised | `SalesMonthlyReview.aspx`, `ClaimsApproval_v2.aspx`, `MyClaimsView_v2.aspx` |
| Forward cleanup | After last-pending Forward, Request 96 leaves the L1 queue, success notify appears, details modal closes, grid refreshes without F5 |

---

## 6. Readiness Score

| Area | Score |
| --- | --- |
| Submission (NEW + RESUBMIT) | 100 |
| Approval / Return | 100 |
| Latest-version matrix | 100 |
| L1 UX (notify, confirm, modal close) | 100 |
| My Claims paging / export | 100 |
| SQL synchronization | 100 |
| Release packaging | 100 |

**Overall: 100 / 100**

Remaining work is operational only: manual `git push` of commit + tag, production change approval, then IIS/SQL deploy per `TA_DA_Deployment_Checklist.md`.
