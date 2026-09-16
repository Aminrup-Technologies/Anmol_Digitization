# TA/DA Final UAT Test Pack

**Document type:** Tester-facing execution guide (documentation only)  
**Scope:** Certified behaviors RC-001 through RC-015G, aligned to RC-016/RC-017 production smoke intent  
**Do not modify:** application code, SQL, Git, tags, or deployment documents while executing this pack  

| Field | Value |
| --- | --- |
| Product | TA/DA Claims (AnmolDristi) |
| Baseline tag | `tada-v1.1-postgolive-fsot-2026` (`3779d185`) |
| Environments | UAT (`AnmolDristiDev`) and/or Production (post-deploy only) |
| Primary evidence sources | `TA_DA_Smoke_Test_Execution.md`, `RC014B_RC014D_UAT_Request97.md`, `RC014D_Deployment_Validation.md`, `RC015G_FSOT_UAT_Execution.md` |
| Financial UAT reference | Request **97** → **₹7,736.44** (Final = Total = Snapshots = Excel) |

### How to use

1. Copy this pack (or print the checklist sections).  
2. For each test: fill **Request ID**, **User**, **Timestamp**, **Result**, and attach screenshot path.  
3. Mark **PASS** only when every expected check in that test is observed.  
4. Do **not** copy UAT amounts into Production Actual columns unless Production yields the same values.  
5. For Production window smoke, prefer **§6 Production-Critical (P0)** first (~10 minutes), then full phases if time allows.

### Certified reference claims

| Claim | Role in this pack | Actors (UAT certified) | Month |
| --- | --- | --- | --- |
| **Request 96** | Employee / L1 / Forward / latest-version smoke (RC-001–013) | Employee Kishor Kumar Sarkar (`AHS1623`); L1 Tirupati Pandey (`AHS1605`) | Aug 2026 |
| **Request 97** | Auditor completion, Ready-for-Payment, FSOT money (RC-014B/D, RC-015G) | Employee Ajay Kumar Sahu (`AHS1454`); Auditor `Audit` | Aug 2026 |

New UAT or Production runs may use **new request IDs**. Record the live ID in every evidence block.

### Common pages

| Actor | Page |
| --- | --- |
| Employee | `SalesMonthlyReview.aspx`, `MyClaimsView_v2.aspx` |
| L1 / Auditor | `ClaimsApproval_v2.aspx` |
| Approver view | `ApprovedClaimsView.aspx` |

---

## §1 — Phase: Employee

**Objective:** Submit, correct Returned days, RESUBMIT, and view claims.  
**Reference:** Request **96** smoke (`TA_DA_Smoke_Test_Execution.md`).

### E-01 NEW submission

| Meta | Value |
| --- | --- |
| Actor | Employee |
| Page | `SalesMonthlyReview.aspx` |
| Reference | Req 96 — submitted 2026-09-15; `DetailVersion = 1` |

| # | Expected check | Pass? |
| --- | --- | --- |
| 1 | Submit for Approval creates a new header | [ ] |
| 2 | Header `Status = Pending` | [ ] |
| 3 | L1 + Auditor approver rows exist | [ ] |
| 4 | Request appears in L1 queue | [ ] |

| Evidence | |
| --- | --- |
| Request ID | _______________ |
| User | _______________ |
| Timestamp | _______________ |
| Result | PASS / FAIL |
| Screenshot | `screenshots/UAT_E01_submit.png` — _attach / paste path_ |
| Notes | |

### E-02 View Claims (My Claims)

| Meta | Value |
| --- | --- |
| Actor | Employee |
| Page | `MyClaimsView_v2.aspx` |
| Reference | RC-003 / RC-005 / RC-006 |

| # | Expected check | Pass? |
| --- | --- | --- |
| 1 | Submitted / Pending claim visible | [ ] |
| 2 | Paging works if > page size | [ ] |
| 3 | Returned status visible when applicable | [ ] |

| Evidence | |
| --- | --- |
| Request ID | _______________ |
| User | _______________ |
| Timestamp | _______________ |
| Result | PASS / FAIL |
| Screenshot | `screenshots/UAT_E02_myclaims.png` — _attach / paste path_ |
| Notes | |

### E-03 Returned day editable + KM gate

| Meta | Value |
| --- | --- |
| Actor | Employee |
| Page | `SalesMonthlyReview.aspx` |
| Reference | Req 96 ActivityId **73192** (01-Aug); RC-008 |

| # | Expected check | Pass? |
| --- | --- | --- |
| 1 | After L1 Return, Returned day is editable | [ ] |
| 2 | KM update allowed only on Returned latest version | [ ] |
| 3 | Snapshot frozen until RESUBMIT | [ ] |

| Evidence | |
| --- | --- |
| Request ID | _______________ |
| Activity / Date | _______________ |
| User | _______________ |
| Timestamp | _______________ |
| Result | PASS / FAIL |
| Screenshot | `screenshots/UAT_E03_km_edit.png` — _attach / paste path_ |
| Notes | |

### E-04 RESUBMIT

| Meta | Value |
| --- | --- |
| Actor | Employee |
| Page | `SalesMonthlyReview.aspx` |
| Reference | Req 96; RC-001 / RC-007 |

| # | Expected check | Pass? |
| --- | --- | --- |
| 1 | Only Returned activities included in RESUBMIT | [ ] |
| 2 | Same `ApprovalRequestId` retained | [ ] |
| 3 | New `DetailVersion` Pending | [ ] |
| 4 | Header returns to L1 Pending | [ ] |
| 5 | UI shows **one row per day** (no duplicate versions) | [ ] |

| Evidence | |
| --- | --- |
| Request ID | _______________ |
| DetailVersion before → after | _______________ |
| User | _______________ |
| Timestamp | _______________ |
| Result | PASS / FAIL |
| Screenshot | `screenshots/UAT_E04_resubmit.png` — _attach / paste path_ |
| Notes | |

**Phase Employee result:** PASS / FAIL · Tester: _______________ · Date: _______________

---

## §2 — Phase: L1

**Objective:** Return, Forward confirmation, modal auto-close.  
**Reference:** Request **96** smoke (RC-009–013).

### L-01 Return day

| Meta | Value |
| --- | --- |
| Actor | L1 Manager |
| Page | `ClaimsApproval_v2.aspx` |
| Reference | Req 96; Tirupati Pandey (`AHS1605`) |

| # | Expected check | Pass? |
| --- | --- | --- |
| 1 | Return of Pending day persists | [ ] |
| 2 | Latest detail `DetailStatus = Returned` | [ ] |
| 3 | Header `Status = Returned` | [ ] |
| 4 | Remarks required (empty Return blocked) | [ ] |
| 5 | Employee grid shows Returned badge / reason | [ ] |

| Evidence | |
| --- | --- |
| Request ID | _______________ |
| Day / ActivityId | _______________ |
| User | _______________ |
| Timestamp | _______________ |
| Result | PASS / FAIL |
| Screenshot | `screenshots/UAT_L01_return.png` — _attach / paste path_ |
| Notes | |

### L-02 Already Returned UI

| Meta | Value |
| --- | --- |
| Actor | L1 |
| Page | `ClaimsApproval_v2.aspx` |
| Reference | RC-009 |

| # | Expected check | Pass? |
| --- | --- | --- |
| 1 | Already-Returned day does not offer invalid re-Return path | [ ] |
| 2 | UI messaging clear for Returned rows | [ ] |

| Evidence | |
| --- | --- |
| Request ID | _______________ |
| User | _______________ |
| Timestamp | _______________ |
| Result | PASS / FAIL |
| Screenshot | `screenshots/UAT_L02_already_returned.png` — _attach / paste path_ |
| Notes | |

### L-03 Forward confirmation + auto-close

| Meta | Value |
| --- | --- |
| Actor | L1 |
| Page | `ClaimsApproval_v2.aspx` |
| Reference | RC-011 / RC-013; Req 96 smoke |

| # | Expected check | Pass? |
| --- | --- | --- |
| 1 | Forward shows confirmation modal | [ ] |
| 2 | Confirm succeeds; routes to next approver | [ ] |
| 3 | Details modal auto-closes (no F5) | [ ] |
| 4 | Header stays Pending for Auditor (not Approved header) | [ ] |

| Evidence | |
| --- | --- |
| Request ID | _______________ |
| User | _______________ |
| Timestamp | _______________ |
| Result | PASS / FAIL |
| Screenshot | `screenshots/UAT_L03_forward.png` — _attach / paste path_ |
| Notes | |

**Phase L1 result:** PASS / FAIL · Tester: _______________ · Date: _______________

---

## §3 — Phase: Auditor

**Objective:** Gatekeeper completion and Ready-for-Payment.  
**Reference:** Request **97** (RC-014B / RC-014D).

### A-01 First / intermediate day approve (no premature Completed)

| Meta | Value |
| --- | --- |
| Actor | Auditor |
| Page | `ClaimsApproval_v2.aspx` |
| Reference | Req 97 — 01-Aug / 02-Aug / bulk-except-last |

| # | Expected check | Pass? |
| --- | --- | --- |
| 1 | Day row becomes Done / Approved at Auditor level | [ ] |
| 2 | Pending counter decreases | [ ] |
| 3 | Header remains **Pending** while any gatekeeper day remains | [ ] |
| 4 | Ready for Payment **hidden** | [ ] |
| 5 | No `Final Auditor Approval` history yet | [ ] |

| Evidence | |
| --- | --- |
| Request ID | _______________ |
| Days actioned | _______________ |
| User | _______________ |
| Timestamp | _______________ |
| Result | PASS / FAIL |
| Screenshot | `screenshots/UAT_A01_intermediate.png` — _attach / paste path_ |
| Notes | |

### A-02 Last day → Completed

| Meta | Value |
| --- | --- |
| Actor | Auditor |
| Page | `ClaimsApproval_v2.aspx` |
| Reference | Req 97 last day; HistoryId **687** UAT |

| # | Expected check | Pass? |
| --- | --- | --- |
| 1 | Header **Pending → Completed** only on last latest day | [ ] |
| 2 | Routing cleared (CurrentApprover NULL) | [ ] |
| 3 | History remarks include Final Auditor Approval | [ ] |
| 4 | Ready for Payment becomes visible | [ ] |
| 5 | Request leaves Pending queue | [ ] |

| Evidence | |
| --- | --- |
| Request ID | _______________ |
| History Id (if known) | _______________ |
| User | _______________ |
| Timestamp | _______________ |
| Result | PASS / FAIL |
| Screenshot | `screenshots/UAT_A02_completed.png` — _attach / paste path_ |
| Notes | |

### A-03 Ready for Payment

| Meta | Value |
| --- | --- |
| Actor | Auditor |
| Page | `ClaimsApproval_v2.aspx` |
| Reference | Req 97; HistoryId **688** UAT; RC-014D |

| # | Expected check | Pass? |
| --- | --- | --- |
| 1 | Confirm Mark Ready succeeds (no closed-connection error) | [ ] |
| 2 | Header **Completed → ReadyForPayment** | [ ] |
| 3 | History persists (named INSERT path) | [ ] |
| 4 | Status remains ReadyForPayment after refresh | [ ] |

| Evidence | |
| --- | --- |
| Request ID | _______________ |
| History Id (if known) | _______________ |
| User | _______________ |
| Timestamp | _______________ |
| Result | PASS / FAIL |
| Screenshot | `screenshots/UAT_A03_ready.png` — _attach / paste path_ |
| Notes | |

**Phase Auditor result:** PASS / FAIL · Tester: _______________ · Date: _______________

---

## §4 — Phase: Financial Proof (FSOT)

**Objective:** Net Payable ownership — Final = Snapshots = Excel.  
**Reference:** Request **97** / RC-015G. UAT certified amount **₹7,736.44**.

### F-01 Header Final / Total

| Meta | Value |
| --- | --- |
| Actor | QA / Business (SQL or modal bind) |
| Source | `usp_GetApprovalRequest` / ClaimsApproval modal |
| Reference | RC-015A / RC-015G |

| # | Expected check | Pass? |
| --- | --- | --- |
| 1 | `FinalApprovedAmount` populated (not NULL on Ready/Completed after Recalc) | [ ] |
| 2 | `FinalApprovedAmount` = latest snapshot DayNet sum | [ ] |
| 3 | `TotalApprovedAmount` reconciles when Stationary = 0; **Final is authority** if Stationary ≠ 0 | [ ] |
| 4 | Status still ReadyForPayment through Recalc/Get | [ ] |

| Evidence | |
| --- | --- |
| Request ID | _______________ |
| FinalApprovedAmount (Actual) | _______________ |
| TotalApprovedAmount (Actual) | _______________ |
| DayNet sum (Actual) | _______________ |
| User | _______________ |
| Timestamp | _______________ |
| Result | PASS / FAIL |
| Screenshot / SQL log | `screenshots/UAT_F01_header.png` — _attach / paste path_ |
| Notes | UAT ref ₹7,736.44 — do not force-fit Production |

### F-02 Excel export latest-version

| Meta | Value |
| --- | --- |
| Actor | QA |
| Source | `usp_Export_Auditor_Final_Excel` / My Claims or Approved Claims export |
| Reference | RC-015C / RC-015G |

| # | Expected check | Pass? |
| --- | --- | --- |
| 1 | One latest Approved row per activity (no RESUBMIT duplicates) | [ ] |
| 2 | Excel SUM `[Final Approved Amount]` = header Final | [ ] |
| 3 | Row count = latest payable activities (UAT Req 97: **31**) | [ ] |

| Evidence | |
| --- | --- |
| Request ID | _______________ |
| Excel SUM (Actual) | _______________ |
| Row count (Actual) | _______________ |
| User | _______________ |
| Timestamp | _______________ |
| Result | PASS / FAIL |
| Screenshot / file | `screenshots/UAT_F02_excel.png` / `exports/…` — _attach / paste path_ |
| Notes | |

### F-03 Cross-surface consistency

| Surface | Amount / status | Match Final? |
| --- | --- | --- |
| Employee / My Claims | _______________ | [ ] |
| Approved Claims | _______________ | [ ] |
| Auditor / Ready modal | _______________ | [ ] |
| Excel | _______________ | [ ] |
| SQL Header | _______________ | [ ] |

| Evidence | |
| --- | --- |
| Request ID | _______________ |
| User | _______________ |
| Timestamp | _______________ |
| Result | PASS / FAIL |
| Screenshot | `screenshots/UAT_F03_cross_surface.png` — _attach / paste path_ |
| Notes | |

### F-04 Stationary writer (RC-015B) — if Stationary ≠ 0

| # | Expected check | Pass? |
| --- | --- | --- |
| 1 | Expense status SP snapshot includes Stationary | [ ] / N/A |
| 2 | Final still matches refreshed snapshots | [ ] / N/A |

| Evidence | |
| --- | --- |
| Request ID | _______________ |
| Stationary day amount | _______________ |
| Result | PASS / FAIL / N/A |
| Notes | Mandatory on Production package even if UAT expense SP was three-part at certification |

**Phase Financial result:** PASS / FAIL · Tester: _______________ · Date: _______________

---

## §5 — Phase: Latest-Version Regression

**Objective:** Confirm RESUBMIT / Forward / expense / export never use superseded snapshots.  
**Reference:** Req **96** (RC-012) + Req **97** export (RC-015C).

### V-01 Approval grid one row per day

| # | Expected check | Pass? |
| --- | --- | --- |
| 1 | After RESUBMIT, ClaimsApproval shows one row per `ActivityId` | [ ] |
| 2 | Superseded versions not actionable | [ ] |

| Evidence | Request ID _______________ · User _______________ · Timestamp _______________ · Result PASS/FAIL |
| Screenshot | `screenshots/UAT_V01_one_row.png` |

### V-02 Forward completeness

| # | Expected check | Pass? |
| --- | --- | --- |
| 1 | Forward uses latest DetailVersion then latest detail id | [ ] |
| 2 | Header not incorrectly set to Approved by Forward | [ ] |

| Evidence | Request ID _______________ · User _______________ · Timestamp _______________ · Result PASS/FAIL |
| Screenshot | `screenshots/UAT_V02_forward_latest.png` |

### V-03 Expense status latest snapshot

| # | Expected check | Pass? |
| --- | --- | --- |
| 1 | Expense Approve/Reject updates latest snapshot only | [ ] |
| 2 | (If 015B live) ExpenseTotal = DA+Hotel+Travel+Stationary | [ ] / N/A |

| Evidence | Request ID _______________ · User _______________ · Timestamp _______________ · Result PASS/FAIL |
| Screenshot | `screenshots/UAT_V03_expense.png` |

### V-04 Export no duplicate Approved versions

| # | Expected check | Pass? |
| --- | --- | --- |
| 1 | Export row count = latest Approved activities only | [ ] |

| Evidence | Request ID _______________ · User _______________ · Timestamp _______________ · Result PASS/FAIL |
| Screenshot / file | `screenshots/UAT_V04_export.png` |

**Phase Latest-Version result:** PASS / FAIL · Tester: _______________ · Date: _______________

---

## §6 — Production-Critical Smoke (P0) — ~10 minutes

Run **only** these after Production IIS recycle (or as first UAT gate). Stop the window on any FAIL.

| # | Test | Actor | Expected (one-liner) | Result | Request ID | Timestamp |
| --- | --- | --- | --- | --- | --- | --- |
| P0-1 | Employee Submit | Employee | New Pending request created | PASS/FAIL | | |
| P0-2 | L1 Return + Employee RESUBMIT | L1 → Emp | Returned then new DetailVersion; one row/day | PASS/FAIL | | |
| P0-3 | L1 Forward confirm | L1 | Modal confirm + auto-close; routes to Auditor | PASS/FAIL | | |
| P0-4 | Auditor last-day Completed | Auditor | Header Completed only on last day | PASS/FAIL | | |
| P0-5 | Ready for Payment | Auditor | Completed → ReadyForPayment persists | PASS/FAIL | | |
| P0-6 | Financial reconcile | QA | Final = Snapshots = Excel on Ready claim | PASS/FAIL | | |

| P0 Evidence | |
| --- | --- |
| Environment | UAT / Production |
| Build / DLL timestamp | _______________ |
| Tester | _______________ |
| Start / End | _______________ / _______________ |
| Overall P0 | PASS / FAIL |
| Screenshot set | `screenshots/P0_*.png` — _attach_ |

**P0 fail rule:** Do not issue GO-LIVE / Phase 9 CERTIFIED. Escalate to Release Commander; consider rollback per RC-017.

---

## §7 — Pack sign-off

| Phase | Result | Tester | Date |
| --- | --- | --- | --- |
| §1 Employee | | | |
| §2 L1 | | | |
| §3 Auditor | | | |
| §4 Financial | | | |
| §5 Latest-Version | | | |
| §6 P0 Smoke | | | |

| Role | Name | Signature | Date |
| --- | --- | --- | --- |
| QA | | | |
| Business | | | |
| Release / CAB (if Production) | | | |

### Traceability (do not re-test from scratch unless regressing)

| Behavior | Certified RC / pack |
| --- | --- |
| Submit / Return / KM / RESUBMIT / Forward / modal | RC-001–013 · Smoke · Req **96** |
| Auditor gatekeeper → Completed | RC-014B · Req **97** |
| Ready for Payment | RC-014D · Req **97** |
| FSOT Final / Excel | RC-015A/C/G · Req **97** = ₹7,736.44 |
| Stationary expense writer | RC-015B (package mandatory; prove if Stationary ≠ 0) |
| Production evidence log | `RC017_Production_Execution_Log.md` |

---

*End of Final UAT Test Pack. Documentation only — no application, SQL, Git, or deployment changes.*
