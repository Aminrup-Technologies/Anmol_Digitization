# Extraction Report — Monthly Sales Claims Database Baseline

| Attribute | Value |
| --- | --- |
| Extraction date | 2 August 2026 |
| Source | Live SQL Server instance, `115.112.186.71`, database `AnmolDristiDev` |
| Method | Direct connection to the target database; object existence and type confirmed via `sys.objects`/`sys.table_types`; module source text extracted verbatim via `OBJECT_DEFINITION()`; table and table-type structure reconstructed from `sys.columns`, `sys.key_constraints`, `sys.foreign_keys`, `sys.check_constraints`, `sys.default_constraints`, `sys.identity_columns`, and `sys.indexes` |
| Scope | Every database object referenced, directly or transitively, by the eight Monthly Sales Claims application pages (`SalesMonthlyReview.aspx`, `ClaimsApproval_v2.aspx`, `ClaimsApproval.aspx`, `MyClaimsView_v2.aspx`, `MyClaimsView.aspx`, `ApprovedClaimsView.aspx`, `ClaimsSubmitted.aspx`, `SubmitClaim.aspx`) |
| Objects modified in the database | **None.** This was a read-only extraction. No `INSERT`, `UPDATE`, `DELETE`, `CREATE`, `ALTER`, or `DROP` statement was issued against the target database at any point. |
| Objects modified in application code | **None.** |

This report should be read together with `Inventory.md` (the full object register) and `MissingObjects.md` (objects referenced by application code but not found in the database).

---

## 1. Objects Found

| Category | Count | Location |
| --- | --- | --- |
| Tables | 16 | `Database/Tables/` |
| Views | 1 | `Database/Views/` |
| Functions | 0 | `Database/Functions/` (confirmed absent, not omitted) |
| Table Types | 3 | `Database/Types/` |
| Triggers | 0 | `Database/Triggers/` (confirmed absent, not omitted) |
| Stored Procedures — Approval | 13 | `Database/Stored Procedures/Approval/` |
| Stored Procedures — Submission | 7 | `Database/Stored Procedures/Submission/` |
| Stored Procedures — Retrieval | 10 | `Database/Stored Procedures/Retrieval/` |
| Stored Procedures — Utilities | 6 | `Database/Stored Procedures/Utilities/` |
| **Total scripted** | **56** | |

Every extracted file was verified to contain a non-empty `CREATE` statement after generation; none failed silently. One procedure (`usp_ProcessApprovalAction`) was cross-checked against the version already committed at `Database/Scripts/005_usp_ProcessApprovalAction.sql` and found to be **content-identical** (the only difference is line-ending convention — CRLF as stored in the database versus LF as previously committed to source control — which has no functional effect). This confirms that at least one previously-documented procedure has not drifted from the deployed database since it was scripted.

---

## 2. Objects Missing

Three objects, all stored procedures, are referenced by application code but do not exist in the target database. Full detail, including exact file:line references and the reasoning for each, is in `MissingObjects.md`.

| Object | Referenced by |
| --- | --- |
| `usp_ProcessApprovalRequestDetails` | `ClaimsApproval.aspx.cs` — dead code only (`_OLD1`-suffixed handlers) |
| `usp_GetApprovalRequestsForUser` | `ClaimsSubmitted.aspx.cs` — unreferenced/orphan page |
| `usp_GetApprovers_WithComputedState` | `ClaimsSubmitted.aspx.cs` — unreferenced/orphan page |

No object required by a **live, reachable** application code path was found to be missing. All three gaps sit behind either explicitly dead code or a page with no navigational entry point.

---

## 3. Warnings

These are factual observations made during extraction. They are not analysis, and no remediation is proposed here — they are recorded so that whoever next works against this baseline is not surprised by it.

### 3.1 The target database contains four stored procedures that no examined application code calls

`usp_AdminRollbackApprovalRequest`, `usp_GetApprovalHistory`, `usp_RecalculateApprovalRequest`, and `usp_RollupSalesApprovalRequestAmounts` all exist in the target database, all operate directly on the core claims tables (`SalesApprovalRequest`, `SalesApprovalRequestDetail`, `SalesApprovalRequestApprover`, `ApprovalRequestStatusHistory`), and all were created or last modified between 9 January 2026 and 1 August 2026 — with three of the four modified within the seven days preceding this extraction. A repository-wide search confirms none is called from `ClaimsApproval_v2.aspx.cs`, `ClaimsApproval.aspx.cs`, or any other page in the application. By name and by internal structure (each has a self-documenting purpose comment, e.g. `usp_RecalculateApprovalRequest`: *"Recalculate all financial and activity summary values for an Approval Request"*; `usp_AdminRollbackApprovalRequest` contains an explicit guard — `IF @Status = 'Paid' THROW 50002` — against rolling back a paid claim), these appear to be recently-authored procedures awaiting integration into the application layer, rather than abandoned or legacy code. They have been extracted and inventoried in full because they exist and are unambiguously part of the claims-approval domain; their absence from any call site is recorded as a fact, not corrected.

### 3.2 One of the newly found procedures tests for a header status value (`'Paid'`) that does not appear in `ClaimStatuses.cs` or in any application code

`usp_AdminRollbackApprovalRequest` contains `IF @Status = 'Paid' THROW 50002,'Paid requests cannot be rolled back.',1;`. No application file, and no other extracted procedure, writes or compares against the literal `'Paid'`. This is recorded as a discrepancy between the status vocabulary implied by the newest database logic and the status vocabulary documented in the application's `ClaimStatuses.cs` (`Draft`, `Submitted`, `Pending`, `Approved`, `Rejected`, `Returned`, `Completed`, `ReadyForPayment`, `Processed`, `Unknown`).

### 3.3 `SalesDailyActivity.ClaimStatus` is constrained at the database level to five values that do not include `'Pending'`

`CK_SalesDailyActivity_ClaimStatus` permits only `'Rejected'`, `'Approved'`, `'Returned'`, `'Submitted'`, or `'Draft'`. Application code elsewhere compares `SalesDailyActivity.ClaimStatus` against `ClaimStatuses.Pending` (e.g. `ClaimEditabilityHelper.cs`, `SalesMonthlyReview.aspx.cs`). Whether this constraint is stale relative to the application's expectations, or whether the application in practice never actually writes `'Pending'` to this specific column, could not be determined from a schema-only extraction and is recorded as an open discrepancy.

### 3.4 `SalesApprovalRequest` and `SalesDailyActivity` carry columns that no examined application code reads or writes

`SalesDailyActivity` includes a `RowVersion` column of type `timestamp` (SQL Server's native optimistic-concurrency token) and eight paired claimed/approved/rejected amount columns (`ClaimedDA`, `ApprovedDA`, `RejectedDA`, `ClaimedHotel`, `ApprovedHotel`, `RejectedHotel`, `ClaimedTravel`, `ApprovedTravel`, `RejectedTravel`, `ApprovedDistanceKM`, `ApprovedCalculatedClaimAmount`). `SalesApprovalRequest` includes eleven counter/rollup columns (`TotalActivities`, `TotalExpenseEntries`, `TotalAttachmentCount`, `ApprovedActivities`, `RejectedActivities`, `PendingActivities`, `BulkEligibleActivities`, `ManualReviewActivities`, `ReturnedActivities`, `FinalApprovedAmount`, `TotalDeductionAmount`), a `RequestRemarks` column, and an `OriginalClaimAmount` column. A repository-wide search confirms zero references to any of these column names in any `.cs` file. Several of these — particularly the claimed/approved/rejected split on `SalesDailyActivity` — appear structurally related to the approved-amount tracking that `usp_RollupSalesApprovalRequestAmounts` (§3.1) computes, suggesting coordinated but not-yet-integrated schema evolution.

### 3.5 The earlier Technical Design Specification's inferred schema differs from the extracted schema in several places

The Technical Design Specification (`docs/TechnicalDesignSpecification_MonthlySalesClaims_Phase1_v1.0.md`), written before this extraction, reconstructed table structure from the application's query surface because no DDL was then available. Comparing that inference against this extraction: `SalesApprovalRequest` has 39 columns, not the smaller set previously inferred, and does not have a persisted `MaxApproverLevel` column — `MaxApproverLevel` is in fact a derived value computed inline within `usp_GetApprovalRequest`'s `SELECT`, not a stored column. `SalesDailyActivity` has 55 columns, substantially more than previously inferred, including denormalised employee/manager snapshot fields (`User_Name`, `Reporting_Manager1` through `Reporting_Manager3` and their `_Code` variants) that are the subject of `vw_SalesDailyActivity_UserMismatch` (see Inventory.md §2). This is noted so that the two documents are not read as contradictory; the earlier document was explicit about being an inference pending exactly this kind of extraction (see its §1.8), and this extraction is the correction.

### 3.6 The database is under active development

Several extracted procedures carry `modify_date` values within the seven days preceding this extraction, and one (`usp_ProcessApprovalAction`) was created on the same calendar day as this extraction. `AnmolDristiDev` is a development instance and should be expected to change again after this baseline is taken. This extraction is a point-in-time snapshot, not a guarantee of the database's current state at any later date.

---

## 4. Potential Risks

| # | Risk | Basis |
| --- | --- | --- |
| R-01 | A future change to the application that begins calling `usp_AdminRollbackApprovalRequest`, `usp_RecalculateApprovalRequest`, `usp_RollupSalesApprovalRequestAmounts`, or `usp_GetApprovalHistory` will introduce behaviour that has not been reviewed by the Functional Specification, the Technical Design Specification, or the Production Hardening Review, all three of which were written without knowledge that these procedures exist. | §3.1 |
| R-02 | The undocumented `'Paid'` status literal, if it is or becomes a real transition value, is not covered by the header status vocabulary in `ClaimStatuses.cs` or by the state machine documented in the Functional Specification, and would need reconciliation before any dependent logic is safely written. | §3.2 |
| R-03 | If `CK_SalesDailyActivity_ClaimStatus` is enforced against a live write path that attempts to set `ClaimStatus = 'Pending'` on `SalesDailyActivity`, that write will fail at the database level. Whether this currently happens, and how it is currently handled, was not established by this extraction and would require runtime testing, not schema inspection, to confirm. | §3.3 |
| R-04 | The unused columns identified in §3.4 represent either dormant technical debt or a partially-delivered feature (most plausibly, split claimed/approved/rejected expense tracking and a not-yet-adopted optimistic-concurrency token). Either way, any future schema change to `SalesApprovalRequest` or `SalesDailyActivity` should account for these columns explicitly rather than assume they are safe to drop, since their purpose could not be confirmed from this extraction alone. | §3.4 |
| R-05 | This baseline will drift from the live database if development continues on `AnmolDristiDev` after the extraction date. Any consumer of this baseline (for code review, for a future migration, or for the remediation roadmap in the Technical Design Specification) should re-verify against the live database before relying on it for anything beyond historical reference. | §3.6 |
| R-06 | The four objects in Inventory.md §9 (`Process_StagingToFinal_FromJson`, `Process_StagingToFinal_FromJson_Idempotent`, `usp_SyncHierarchyAndSalesActivity`, `usp_SyncHierarchyForBatch`) write to `SalesDailyActivity` from outside the Monthly Sales Claims workflow. Because this table is also the source of the immutable detail snapshot taken at submission, any change to those procedures has the potential to affect claims data without going through any procedure documented in this baseline. This baseline does not extract or analyse those procedures; a full review of the staging/import module would be needed to assess this risk further. | Inventory.md §9 |
| R-07 | Three objects that application code expects to exist do not (`MissingObjects.md`). While all three sit behind dead code or an unreachable page today, any future change that reactivates that code or that page (for example, re-linking `ClaimsSubmitted.aspx` from navigation) would fail at runtime until the missing procedures are either restored or the referencing code is updated. | §2 |

---

## 5. Extraction integrity statement

- No business logic was modified. Every stored procedure and view in this baseline was captured via `OBJECT_DEFINITION()`, which returns the exact text SQL Server has stored for that object — nothing was retyped, reformatted, or reinterpreted.
- No object was renamed. Every file name matches the object's name exactly as it exists in the database.
- No SQL was optimised, rewritten, or corrected, including SQL that this report identifies as inconsistent (§3.2, §3.3) or duplicated in effect (the several procedures across §5–§7 of `Inventory.md` that independently write to `SalesApprovalRequest` or `SalesDailyExpense`).
- No formatting was changed for module objects (procedures, views) — the stored text is reproduced verbatim, including whatever indentation, casing, and comment style the original author used, which is why formatting is inconsistent from file to file within this baseline.
- Table and table-type definitions were necessarily generated from catalog metadata rather than copied from stored text, because SQL Server does not retain the original `CREATE TABLE` statement after execution. This is standard, reversible scripting practice; every column, type, length, precision, scale, nullability, default, identity property, primary key, foreign key, check constraint, unique constraint, and non-key index was read from `sys.*` catalog views and transcribed without alteration or omission.
- No object was generated for anything that could not be located. The three objects in `MissingObjects.md` are documented, not fabricated.
