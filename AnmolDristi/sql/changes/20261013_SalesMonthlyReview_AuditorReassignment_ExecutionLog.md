# Sales Monthly Review auditor reassignment — execution log

## Outcome

**SQL-level auditor reassignment: COMPLETED.**

The operator confirmed a successful SSMS run against `MUMANMOAPP01\SQLEXPRESS` / `AnmolDristi`. MCP did not execute the update. The SSMS wall-clock time was not supplied with the closure, so it is not recorded here.

| SSMS field | Confirmed value |
|---|---|
| SSMS execution time | Not supplied |
| `@CommitChanges` | `1` |
| TransactionOutcome | `COMMITTED` |
| OpenTransactions | `0` |
| Auditor login updates saved | 15 (`Audit` to `AuditOne`) |
| Queue-owner updates saved | 5 (`CurrentApproverLoginId` `Audit` to `AuditOne`) |
| Post-transaction Auditor login | `AuditOne` |

A later run of the same script found the 15 Auditor rows already set to `AuditOne`, failed the original-state eligibility checks, and stopped before `BEGIN TRAN`. That run wrote nothing.

The application routing defect is outside this task. Sign-in and switch-user still store `Session["AUDITOR_CODE"] = "Audit"`. No application code was changed.

## Execution timestamp and target

| Item | Value |
|---|---|
| Server clock returned by the tool | 2026-10-13T04:58:25.495Z |
| Server | `MUMANMOAPP01\SQLEXPRESS` |
| Database | `AnmolDristi` |
| Tool | `user-AnmolDristi-LIVE-Reader` / `execute_query` |
| Triggers on `SalesApprovalRequest` | none |
| Triggers on `SalesApprovalRequestApprover` | none |

The tool serializes `datetime2` values with a `Z` suffix. These columns do not store an offset. `RequestDate` is the India-time wall clock written by `dbo.usp_SubmitSalesDailyActivities`.

## Scope

September 2026 (`YearNo = 2026`, `MonthNo = 9`), `RequestDate` from 2026-10-01 inclusive to 2026-11-01 exclusive, status `Pending`, region South, exactly these IDs:

`1247, 1276, 1293, 1324, 1536, 1562, 1648, 1667, 1675, 1676, 1470, 1517, 1558, 1563, 1616`

## Account check

| EmployeeCode | Id | EmployeeName | EmployeeStatus |
|---|---|---|---|
| Audit | 1106 | Rohan Saraf | Active |
| AuditOne | 4618 | Mehga Paramanick | Active |

## Preflight

One eligibility query returned `EligibleCount = 15`, `ReturnedCount = 15`, `ExactlyOneAuditor = 15`. No mismatched request ID. Region checks used `ISNULL` so a missing region description cannot pass as unknown.

## BEFORE snapshot — request and Auditor row

This is the MCP discovery read taken before the SSMS package was prepared. Auditor `ActionBy` and `ActionDate` were null on every row. It is evidence of that read, not a substitute for the `Baseline` result set produced when SSMS runs the script.

| Request | Requester | Year | Month | RequestDate | Status | Current approver | Level | Auditor row | Auditor login | Auditor status | RegionId | Region | Stage |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1247 | AHS1373 | 2026 | 9 | 2026-10-04 17:06:48.197 | Pending | AHS1501 | 1 | 2466 | Audit | Pending | 2553 | South | Awaiting level 1 |
| 1276 | AHS1748 | 2026 | 9 | 2026-10-04 22:02:51.222 | Pending | AHS1501 | 1 | 2524 | Audit | Pending | 2553 | South | Awaiting level 1 |
| 1293 | AHS1913 | 2026 | 9 | 2026-10-05 10:16:43.920 | Pending | AHS1775 | 1 | 2558 | Audit | Pending | 2553 | South | Awaiting level 1 |
| 1324 | AHS1290 | 2026 | 9 | 2026-10-05 17:20:50.279 | Pending | AHS1501 | 1 | 2620 | Audit | Pending | 2553 | South | Awaiting level 1 |
| 1536 | AHS1759 | 2026 | 9 | 2026-10-08 06:56:59.437 | Pending | AHS1731 | 1 | 3044 | Audit | Pending | 2553 | South | Awaiting level 1 |
| 1562 | AHS1912 | 2026 | 9 | 2026-10-08 18:16:59.149 | Pending | AHS1795 | 1 | 3096 | Audit | Pending | 2553 | South | Awaiting level 1 |
| 1648 | AHS1664 | 2026 | 9 | 2026-10-11 09:24:52.428 | Pending | AHS1775 | 1 | 3268 | Audit | Pending | 2553 | South | Awaiting level 1 |
| 1667 | AHS1417 | 2026 | 9 | 2026-10-12 10:17:20.242 | Pending | AHS1459 | 1 | 3306 | Audit | Pending | 2553 | South | Awaiting level 1 |
| 1675 | AHS1649 | 2026 | 9 | 2026-10-12 20:04:38.138 | Pending | AHS1731 | 1 | 3322 | Audit | Pending | 2553 | South | Awaiting level 1 |
| 1676 | AHS1355 | 2026 | 9 | 2026-10-12 22:05:34.658 | Pending | AHS1458 | 1 | 3324 | Audit | Pending | 2553 | South | Awaiting level 1 |
| 1470 | AHS1769 | 2026 | 9 | 2026-10-07 09:15:39.546 | Pending | Audit | 2 | 2912 | Audit | Pending | 2553 | South | With auditor |
| 1517 | AHS1446 | 2026 | 9 | 2026-10-07 21:59:36.726 | Pending | Audit | 2 | 3006 | Audit | Pending | 2553 | South | With auditor |
| 1558 | AHS1471 | 2026 | 9 | 2026-10-08 17:05:26.728 | Pending | Audit | 2 | 3088 | Audit | Pending | 2553 | South | With auditor |
| 1563 | AHS1548 | 2026 | 9 | 2026-10-08 18:37:25.622 | Pending | Audit | 2 | 3098 | Audit | Pending | 2553 | South | With auditor |
| 1616 | AHS1260 | 2026 | 9 | 2026-10-09 21:01:11.995 | Pending | Audit | 2 | 3204 | Audit | Pending | 2553 | South | With auditor |

## BEFORE snapshot — other approver rows

These rows were not targets. Level-1 `ActionBy` values below belong to the manager action, not the Auditor row.

| Request | Row | Login | Role | Level | Status | ActionBy | ActionDate |
|---|---|---|---|---|---|---|---|
| 1247 | 2465 | AHS1501 | Reporting_Manager1 | 1 | Pending | null | null |
| 1276 | 2523 | AHS1501 | Reporting_Manager1 | 1 | Pending | null | null |
| 1293 | 2557 | AHS1775 | Reporting_Manager1 | 1 | Pending | null | null |
| 1324 | 2619 | AHS1501 | Reporting_Manager1 | 1 | Pending | null | null |
| 1470 | 2911 | AHS1459 | Reporting_Manager1 | 1 | Approved | AHS1459 | 2026-10-08 13:36:01.509 |
| 1517 | 3005 | AHS1775 | L1 | 1 | Approved | AHS1775 | 2026-10-12 17:38:39.112 |
| 1536 | 3043 | AHS1731 | L1 | 1 | Pending | null | null |
| 1558 | 3087 | AHS1459 | L1 | 1 | Approved | AHS1459 | 2026-10-08 12:33:42.020 |
| 1562 | 3095 | AHS1795 | L1 | 1 | Pending | null | null |
| 1563 | 3097 | AHS1396 | L1 | 1 | Approved | AHS1396 | 2026-10-08 14:42:21.809 |
| 1616 | 3203 | AHS1396 | L1 | 1 | Approved | AHS1396 | 2026-10-10 14:05:32.733 |
| 1648 | 3267 | AHS1775 | L1 | 1 | Pending | null | null |
| 1667 | 3305 | AHS1459 | L1 | 1 | Pending | null | null |
| 1675 | 3321 | AHS1731 | L1 | 1 | Pending | null | null |
| 1676 | 3323 | AHS1458 | L1 | 1 | Pending | null | null |

## AFTER snapshot

The operator confirmed the committed result. A row-level after grid was not pasted into this log. The cited evidence is `AfterAuditReassignments(1).xlsx`, the successful SSMS output, and `Pasted text(20261013-051853).txt`. Those files are not in the repository.

Confirmed for all 15 requests: Auditor login `AuditOne`, request status `Pending`, auditor action fields still null. The ten level-1 requests kept their queue owner and level. The five level-2 requests have `CurrentApproverLoginId = AuditOne` and level 2.

## Per-request change summary

| Requests | Confirmed database change |
|---|---|
| 1247, 1276, 1293, 1324, 1536, 1562, 1648, 1667, 1675, 1676 | Auditor login `Audit` to `AuditOne`. Current approver and level unchanged. |
| 1470, 1517, 1558, 1563, 1616 | Auditor login and `CurrentApproverLoginId` from `Audit` to `AuditOne`. Level stayed 2. |

Request `1240` is an August 2026 claim and is not in the script.

## Update counts

| Write | Expected if SSMS commits | Confirmed saved |
|---|---|---|
| Auditor `ApproverLoginId` | 15 | 15 |
| Header `CurrentApproverLoginId` | 5 | 5 |

## SSMS package

`sql/changes/20261013_SalesMonthlyReview_AuditorReassignment.sql` is the script to run. `@CommitChanges` defaults to `0`, so a successful validation still rolls back.

The first draft would have committed whenever its checks passed. Its eligibility test also inner-joined the employee, so a missing requester could drop out of the failure test. The revised script:

- returns the baseline before it opens a transaction, and keeps that baseline in a table variable that survives rollback
- requires exactly one `MST_Region` row whose `Region_ID` equals `MST_UserMaster.RegionId`, and requires that description to be South
- requires both `Audit` and `AuditOne` to be Active
- requires unique filtered index `UX_SAR_Auditor_Once`
- locks the header and every approver row for the 15 IDs, then stops if they differ from the baseline
- updates the existing Auditor row only, and updates `CurrentApproverLoginId` only for `1470`, `1517`, `1558`, `1563`, and `1616`
- rolls back unless the saved counts are 15 and 5 and the protected fields still match

## Expected preview result

These are the grids the preview run is expected to show. They are not a record of an execution.

| Result set | Expected |
|---|---|
| Session | Server `MUMANMOAPP01\SQLEXPRESS`, database `AnmolDristi`, `CommitChanges = 0` |
| Accounts | `Audit` and `AuditOne`, both Active |
| EligibilityFailures | No rows |
| Baseline | 15 rows, Auditor login `Audit`, region South, September 2026, October `RequestDate` |
| ProposedChanges | All 15 Auditor logins become `AuditOne`. Only the five queue-owner requests change `CurrentApproverLoginId`. |
| UpdateCounts | 15 and 5 |
| AfterSnapshotInsideTransaction | Auditor login `AuditOne`. Level stays 1 for the ten requests and 2 for the five. |
| TransactionOutcome | `ROLLED BACK (preview)` |
| PostTransactionState | Auditor login back to `Audit` for all 15 |

## Expected committed result

Only after a separate run with `@CommitChanges = 1`, and only if that run's own grids match the preview:

- Auditor row on each of the 15 IDs is still the same `ApprovalRequestApproverId`, with `ApproverLoginId = AuditOne`, status `Pending`, and null action fields.
- `1470`, `1517`, `1558`, `1563`, and `1616` have `CurrentApproverLoginId = AuditOne` and `CurrentApproverLevel = 2`.
- The other ten keep the current approver and level shown in the discovery baseline.
- `TransactionOutcome` is `COMMITTED` and `OpenTransactions` is `0`.
- `PostTransactionState` matches the inside-transaction snapshot.

## If SSMS rolls back

Leave `@CommitChanges` at `0`, or let an assertion error roll the batch back. Then `PostTransactionState` must still show Auditor login `Audit`. The `Baseline` grid from that same run remains in the SSMS results. Record the error text in this log. Do not rerun with the commit switch on until the preview grids match the expected result above.
