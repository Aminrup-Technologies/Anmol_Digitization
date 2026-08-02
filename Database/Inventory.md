# Database Object Inventory — Monthly Sales Claims

Baseline extraction target: `AnmolDristiDev` (live SQL Server instance, `115.112.186.71`), extracted 2 August 2026. All objects below were located and scripted from the live database; none were invented or reconstructed from assumption. See `ExtractionReport.md` for methodology and `MissingObjects.md` for objects referenced by application code but not found.

**Dependencies** = other database objects this object reads from, writes to, or calls, per `sys.sql_expression_dependencies` (for modules) or foreign keys (for tables).
**Referenced By** = application code and/or other database objects that call or query this object.

---

## 1. Tables (16)

| Object Name | Folder | Dependencies | Referenced By |
| --- | --- | --- | --- |
| `SalesApprovalRequest` | `Database/Tables/` | Self (via `CK_TotalApprovedAmount_Formula`, referencing its own computed column) | Application: `SalesMonthlyReview.aspx.cs`, `ClaimsApproval_v2.aspx.cs`, `ClaimsApproval.aspx.cs`, `MyClaimsView_v2.aspx.cs`, `MyClaimsView.aspx.cs`, `ApprovedClaimsView.aspx.cs`. Database: read/written by 21 of the 36 extracted stored procedures (see §4). |
| `SalesApprovalRequestDetail` | `Database/Tables/` | FK → `SalesDailyActivity` (`ActivityId`); FK → `SalesApprovalRequest` (`ApprovalRequestId`) | Application: same six pages as above. Database: read/written by 18 of the 36 extracted stored procedures. |
| `SalesApprovalRequestApprover` | `Database/Tables/` | None declared (no FK found) | Application: `ClaimsApproval_v2.aspx.cs`, `ClaimsApproval.aspx.cs`, `MyClaimsView_v2.aspx.cs`, `MyClaimsView.aspx.cs`. Database: read/written by 14 stored procedures. |
| `SalesDailyActivity` | `Database/Tables/` | None declared (no FK found) | Application: `SalesMonthlyReview.aspx.cs`, `ClaimsApproval_v2.aspx.cs`, `ClaimsApproval.aspx.cs`, `MyClaimsView_v2.aspx.cs`, `MyClaimsView.aspx.cs`, `ApprovedClaimsView.aspx.cs`. Database: read/written by 15 stored procedures and `vw_SalesDailyActivity_UserMismatch`. Also read/written by `Process_StagingToFinal_FromJson`, `Process_StagingToFinal_FromJson_Idempotent`, `usp_SyncHierarchyAndSalesActivity`, `usp_SyncHierarchyForBatch` — **out of module scope**, see §6. |
| `SalesDailyExpense` | `Database/Tables/` | None declared (no FK found) | Application: `SalesMonthlyReview.aspx.cs`, `ClaimsApproval_v2.aspx.cs`, `MyClaimsView_v2.aspx.cs`, `MyClaimsView.aspx.cs`, `ApprovedClaimsView.aspx.cs`. Database: read/written by 15 stored procedures. |
| `SalesDailyActivityKMHistory` | `Database/Tables/` | None declared (no FK found) | Application: `SalesMonthlyReview.aspx.cs`, `ClaimsApproval_v2.aspx.cs`, `ClaimsApproval.aspx.cs`, `MyClaimsView_v2.aspx.cs`, `MyClaimsView.aspx.cs`, `ApprovedClaimsView.aspx.cs` (read-only, deduction breakdown). Database: written by `usp_UpdateActivityKM`, `usp_UpdateActivityKM_WithHistory`; read by `usp_GetSalesDailyActivityKMHistory`. |
| `ApprovalRequestStatusHistory` | `Database/Tables/` | None declared (no FK found) | Application: `ClaimsApproval_v2.aspx.cs` (indirectly, via `usp_ProcessApprovalAction`), `ClaimsSubmitted.aspx.cs` (read, via missing procedure — see `MissingObjects.md`). Database: written by 8 stored procedures; read by `usp_GetApprovalHistory`. |
| `SalesMonthlyDraftSummary` | `Database/Tables/` | None declared (no FK found) | Application: `SalesMonthlyReview.aspx.cs` (via `usp_SaveMonthlyDraftSummary`, which is never invoked from any wired control — see §4 notes). Database: written/read by `usp_SaveMonthlyDraftSummary` only. |
| `SalesExpenseAdjustment` | `Database/Tables/` | Self (via computed column `DeductionAmount = ClaimedAmount − ApprovedAmount`) | **None.** Not referenced by any examined application code, and not referenced by any stored procedure in this inventory. Reserved for partial-expense-approval functionality (Phase 2 per the Functional Specification); the table exists but is currently unused end-to-end. |
| `MST_UserMaster` | `Database/Tables/` | None declared within this extraction's scope | Application: all six claims pages that display an employee or approver name. Database: read by 7 of the extracted stored procedures and by `vw_SalesDailyActivity_UserMismatch`. This is a shared employee master table, not owned by the Monthly Sales Claims module; extracted because it is read directly by claims code and claims procedures. |
| `MST_EmployeeTolerance` | `Database/Tables/` | None declared | Application: `SalesMonthlyReview.aspx.cs` (inline read of kilometre tolerance). Database: read by `usp_GetSalesDailyActivity_Month`, `usp_UpdateActivityKM`. Shared master table; extracted for the same reason as `MST_UserMaster`. |
| `PolicyMatrix` | `Database/Tables/` | FK → `MST_Branch` (`BranchId`); FK → `MST_Designation` (`DesignationId`); FK → `MST_Grade` (`GradeId`); FK → `MST_Region` (`RegionId`) | Database only: read by `usp_GetResolvedPolicy`. Not referenced directly by any application code. Extracted as a transitive dependency — see §5, Utilities. |
| `MST_Branch` | `Database/Tables/` | None declared | Database only: read by `usp_ResolveBranch`; FK target of `PolicyMatrix`. Transitive dependency of `usp_GetResolvedPolicy`, itself called by the claims capture page. **Not owned by this module** — see the scope note in §6. |
| `MST_Designation` | `Database/Tables/` | None declared | Database only: read by `usp_ResolveDesignation`; FK target of `PolicyMatrix`. Same scope note as `MST_Branch`. |
| `MST_Grade` | `Database/Tables/` | None declared | Database only: read by `usp_ResolveGrade`; FK target of `PolicyMatrix`. Same scope note as `MST_Branch`. |
| `MST_Region` | `Database/Tables/` | None declared | Database only: read by `usp_ResolveRegion`; FK target of `PolicyMatrix`. Same scope note as `MST_Branch`. |

---

## 2. Views (1)

| Object Name | Folder | Dependencies | Referenced By |
| --- | --- | --- | --- |
| `vw_SalesDailyActivity_UserMismatch` | `Database/Views/` | `SalesDailyActivity`, `MST_UserMaster` | **None.** Confirmed by repository-wide search: no application file references this view, and no other database object depends on it (`sys.sql_expression_dependencies` reverse lookup returns zero rows). It is a data-quality comparison view (denormalised employee/manager fields on `SalesDailyActivity` versus the current values on `MST_UserMaster`), apparently created for ad hoc diagnostic use. Extracted because it directly targets a core claims table; flagged here as an orphan. |

---

## 3. Table Types (3)

| Object Name | Folder | Dependencies | Referenced By |
| --- | --- | --- | --- |
| `TVP_ActivityUpdate` | `Database/Types/` | None (standalone structured type) | Application: `SalesMonthlyReview.aspx.cs`, as the `@Updates` parameter type for `usp_SubmitSalesDailyActivities` (both the live submit path and the dormant `usp_SaveActivitiesDraft` path). |
| `TVP_DetailRemarks` | `Database/Types/` | None | Application: `ClaimsApproval.aspx.cs` only, as the `@DetailRemarks` parameter type for the legacy bulk-approval helpers (`usp_ProcessApprovalRequestDetails_WithRemarks` and the missing `usp_ProcessApprovalRequestDetails` — see `MissingObjects.md`). Not used by `ClaimsApproval_v2.aspx.cs`. |
| `TVP_IntIds` | `Database/Types/` | None | Application: `ClaimsApproval.aspx.cs` only, as the `@DetailIds` parameter type for the same legacy bulk-approval helpers. Not used by `ClaimsApproval_v2.aspx.cs`. |

---

## 4. Stored Procedures — Submission (7)

| Object Name | Folder | Dependencies | Referenced By |
| --- | --- | --- | --- |
| `usp_SubmitSalesDailyActivities` | `Database/Stored Procedures/Submission/` | `ApprovalRequestStatusHistory`, `SalesApprovalRequest`, `SalesApprovalRequestApprover`, `SalesApprovalRequestDetail`, `SalesDailyActivity`, `SalesDailyExpense` | Application: `SalesMonthlyReview.aspx.cs`, `ExecuteSubmitForApproval` — the sole submission/resubmission entry point. |
| `usp_SaveActivitiesDraft` | `Database/Stored Procedures/Submission/` | `SalesDailyActivity` | Application: `SalesMonthlyReview.aspx.cs`, entirely inside a commented-out method (`SaveDraft`); **never invoked from any live control.** |
| `usp_SaveMonthlyDraftSummary` | `Database/Stored Procedures/Submission/` | `SalesDailyActivity`, `SalesDailyExpense`, `SalesMonthlyDraftSummary` | Application: `SalesMonthlyReview.aspx.cs`, inside the commented-out `btn_save_draft_Click` block; **never invoked from any live control.** |
| `usp_SaveDailyExpense` | `Database/Stored Procedures/Submission/` | `SalesDailyExpense` | Application: `SalesMonthlyReview.aspx.cs`, three call sites (DA save, hotel-triggered auto-DA, travel-triggered auto-DA). |
| `usp_SaveHotelExpense` | `Database/Stored Procedures/Submission/` | `SalesDailyExpense` | Application: `SalesMonthlyReview.aspx.cs`, hotel expense save. |
| `usp_InsertTravel_WithLock` | `Database/Stored Procedures/Submission/` | `SalesDailyExpense` | Application: `SalesMonthlyReview.aspx.cs`, travel expense save. |
| `usp_UpdateActivityKM` | `Database/Stored Procedures/Submission/` | `MST_EmployeeTolerance`, `SalesDailyActivity`, `SalesDailyActivityKMHistory` | Application: `SalesMonthlyReview.aspx.cs`, three call sites (grid update, modal save, expense-linked KM save) — employee-side kilometre correction. |

## 5. Stored Procedures — Approval (13)

| Object Name | Folder | Dependencies | Referenced By |
| --- | --- | --- | --- |
| `usp_ProcessApprovalAction` | `Database/Stored Procedures/Approval/` | `ApprovalRequestStatusHistory`, `SalesApprovalRequest`, `SalesApprovalRequestApprover`, `SalesApprovalRequestDetail`, `SalesDailyActivity`, `SalesDailyExpense` | Application: `ClaimsApproval_v2.aspx.cs`, day-level approve/return (bulk and single row) and the unified day-action AJAX endpoint. Confirmed byte-identical (content, modulo line-ending convention) to `Database/Scripts/005_usp_ProcessApprovalAction.sql`, already under source control. |
| `usp_ProcessSalesApprovalRequestDetail` | `Database/Stored Procedures/Approval/` | `ApprovalRequestStatusHistory`, `SalesApprovalRequest`, `SalesApprovalRequestDetail`, `SalesDailyExpense` | Application: `ClaimsApproval_v2.aspx.cs` (reject path) and `ClaimsApproval.aspx.cs` (per-row decisions). |
| `usp_ForwardApprovalRequest` | `Database/Stored Procedures/Approval/` | `SalesApprovalRequest`, `SalesApprovalRequestApprover`, `SalesApprovalRequestDetail` | Application: `ClaimsApproval_v2.aspx.cs`, `btnForwardLevel_Click` — sole live forward entry point. |
| `usp_ReturnClaimToEmployee` | `Database/Stored Procedures/Approval/` | `ApprovalRequestStatusHistory`, `SalesApprovalRequest`, `SalesApprovalRequestApprover`, `SalesApprovalRequestDetail`, `SalesDailyActivity`, `SalesDailyExpense` | Application: `ClaimsApproval_v2.aspx.cs`, `btnReturnToEmployee_Click`. |
| `usp_MarkRequestReadyForPayment` | `Database/Stored Procedures/Approval/` | `ApprovalRequestStatusHistory`, `SalesApprovalRequest`, `SalesApprovalRequestApprover` | Application: `ClaimsApproval_v2.aspx.cs` **and** `ClaimsApproval.aspx.cs` — two call sites with different C#-side preconditions. |
| `usp_UpdateSalesDailyExpenseStatus` | `Database/Stored Procedures/Approval/` | `SalesApprovalRequest`, `SalesApprovalRequestDetail`, `SalesDailyExpense` | Application: `ClaimsApproval_v2.aspx.cs` and `ClaimsApproval.aspx.cs`, expense decision endpoints. |
| `usp_UpdateActivityKM_WithHistory` | `Database/Stored Procedures/Approval/` | `SalesApprovalRequestApprover`, `SalesApprovalRequestDetail`, `SalesDailyActivity`, `SalesDailyActivityKMHistory` | Application: `ClaimsApproval_v2.aspx.cs` and `ClaimsApproval.aspx.cs`, approver-side kilometre amendment. |
| `usp_ReopenApprovalRequestByAuditor` | `Database/Stored Procedures/Approval/` | `ApprovalRequestStatusHistory`, `SalesApprovalRequest`, `SalesApprovalRequestApprover`, `SalesApprovalRequestDetail` | Application: `ClaimsApproval.aspx.cs` only, `btnReopenRequest_Click`. Not present on `ClaimsApproval_v2.aspx.cs`. |
| `usp_ProcessApprovalRequestDetails_WithRemarks` | `Database/Stored Procedures/Approval/` | `ApprovalRequestStatusHistory`, `SalesApprovalRequest`, `SalesApprovalRequestApprover`, `SalesApprovalRequestDetail` | Application: `ClaimsApproval.aspx.cs` only, `ProcessSelectedDetails` — legacy bulk approve/reject. |
| `usp_UpdateApprovalRequestDetailStatus` | `Database/Stored Procedures/Approval/` | `SalesApprovalRequestDetail` | Application: `ClaimsApproval.aspx.cs`, referenced only from unreachable/superseded code paths (per the file's own naming and structure). |
| `usp_UpdateApprovalRequestDetailStatus_AndMaybeActivity` | `Database/Stored Procedures/Approval/` | `SalesApprovalRequest`, `SalesApprovalRequestApprover`, `SalesApprovalRequestDetail`, `SalesDailyActivity`, `SalesDailyExpense` | Application: `ClaimsApproval.aspx.cs`, same status as above. |
| `usp_AdminRollbackApprovalRequest` | `Database/Stored Procedures/Approval/` | `ApprovalRequestStatusHistory`, `SalesApprovalRequest`, `SalesApprovalRequestApprover`, `SalesApprovalRequestDetail`, `SalesDailyActivity`, `SalesDailyExpense` | **None found in any examined application code.** Exists in the target database (created 1 Aug 2026) and is not called from `ClaimsApproval_v2.aspx.cs`, `ClaimsApproval.aspx.cs`, or any other page reviewed. See the warning in `ExtractionReport.md`. |
| `usp_RollupSalesApprovalRequestAmounts` | `Database/Stored Procedures/Approval/` | `ApprovalRequestStatusHistory`, `SalesApprovalRequest`, `SalesApprovalRequestDetail` | **None found in any examined application code.** Exists in the target database (created 9 Jan 2026, last modified same day). See the warning in `ExtractionReport.md`. |

## 6. Stored Procedures — Retrieval (10)

| Object Name | Folder | Dependencies | Referenced By |
| --- | --- | --- | --- |
| `usp_GetSalesDailyActivity_Month` | `Database/Stored Procedures/Retrieval/` | `MST_EmployeeTolerance`, `SalesApprovalRequestDetail`, `SalesDailyActivity`, `SalesDailyExpense` | Application: `SalesMonthlyReview.aspx.cs`, month bind. |
| `usp_GetPendingApprovalsForUser_Fast` | `Database/Stored Procedures/Retrieval/` | `MST_UserMaster`, `SalesApprovalRequest`, `SalesApprovalRequestApprover` | Application: `ClaimsApproval.aspx.cs` only (legacy queue bind). |
| `usp_GetPendingApprovalsForUser_Filters` | `Database/Stored Procedures/Retrieval/` | `MST_UserMaster`, `SalesApprovalRequest`, `SalesApprovalRequestApprover` | Application: `ClaimsApproval_v2.aspx.cs`, live queue bind with filters. |
| `usp_GetApprovalRequest` | `Database/Stored Procedures/Retrieval/` | `MST_UserMaster`, `SalesApprovalRequest`, `SalesApprovalRequestApprover` | Application: `ClaimsApproval_v2.aspx.cs`, `SetFinalApproverFlag`. |
| `usp_GetApprovalRequestDetails` | `Database/Stored Procedures/Retrieval/` | `SalesApprovalRequest`, `SalesApprovalRequestApprover`, `SalesApprovalRequestDetail`, `SalesDailyActivity`, `SalesDailyExpense` | Application: `ClaimsApproval_v2.aspx.cs`, `MyClaimsView_v2.aspx.cs`, `MyClaimsView.aspx.cs`. |
| `usp_GetApprovedClaimsByUser` | `Database/Stored Procedures/Retrieval/` | `MST_UserMaster`, `SalesApprovalRequest`, `SalesApprovalRequestApprover` | Application: `ApprovedClaimsView.aspx.cs`. |
| `usp_GetMyClaims` | `Database/Stored Procedures/Retrieval/` | `MST_UserMaster`, `SalesApprovalRequest`, `SalesApprovalRequestApprover` | Application: `MyClaimsView_v2.aspx.cs`, `MyClaimsView.aspx.cs`. |
| `usp_GetSalesDailyActivityKMHistory` | `Database/Stored Procedures/Retrieval/` | `SalesDailyActivityKMHistory`, `SalesDailyExpense` | Application: `ClaimsApproval.aspx.cs` only. |
| `usp_Export_Auditor_Final_Excel` | `Database/Stored Procedures/Retrieval/` | `MST_UserMaster`, `SalesApprovalRequest`, `SalesApprovalRequestDetail`, `SalesDailyActivity`, `SalesDailyExpense` | Application: `MyClaimsView_v2.aspx.cs`, `MyClaimsView.aspx.cs`, `ApprovedClaimsView.aspx.cs`. |
| `usp_GetApprovalHistory` | `Database/Stored Procedures/Retrieval/` | `ApprovalRequestStatusHistory`, `SalesApprovalRequest`, `SalesApprovalRequestApprover` | **None found in any examined application code.** Exists in the target database (created 30 Jul 2026). See the warning in `ExtractionReport.md`. |

## 7. Stored Procedures — Utilities (6)

| Object Name | Folder | Dependencies | Referenced By |
| --- | --- | --- | --- |
| `usp_GetResolvedPolicy` | `Database/Stored Procedures/Utilities/` | `MST_UserMaster`, `PolicyMatrix`, `usp_ResolveBranch`, `usp_ResolveDesignation`, `usp_ResolveGrade`, `usp_ResolveRegion` | Application: `AnmolDristi/DAL/PolicyResolutionDataAccess.cs`, called by `PolicyResolutionService`, called by `SalesMonthlyReview.aspx.cs` (`_policyResolutionService`). |
| `usp_ResolveBranch` | `Database/Stored Procedures/Utilities/` | `MST_Branch` | Database only: called from within `usp_GetResolvedPolicy`. Not called directly by any application code. |
| `usp_ResolveDesignation` | `Database/Stored Procedures/Utilities/` | `MST_Designation` | Database only: called from within `usp_GetResolvedPolicy`. |
| `usp_ResolveGrade` | `Database/Stored Procedures/Utilities/` | `MST_Grade` | Database only: called from within `usp_GetResolvedPolicy`. |
| `usp_ResolveRegion` | `Database/Stored Procedures/Utilities/` | `MST_Region` | Database only: called from within `usp_GetResolvedPolicy`. |
| `usp_RecalculateApprovalRequest` | `Database/Stored Procedures/Utilities/` | `SalesApprovalRequest`, `SalesApprovalRequestDetail`, `SalesDailyActivity` | **None found in any examined application code.** Exists in the target database (created 31 Jul 2026), self-documented purpose: "Recalculate all financial and activity summary values for an Approval Request." See the warning in `ExtractionReport.md`. |

---

## 8. Views, Functions, Triggers not present

| Category | Finding |
| --- | --- |
| Functions | None located that are used by, or referenced from, the Monthly Sales Claims workflow. See `Database/Functions/README.md` for the verification method. |
| Triggers | None exist on any of the sixteen tables in §1. One unrelated trigger (`TRG_FormsApprovalMatrix_AutoVersion`) exists on `MST_FormsApprovalMatrix`, a QA/QC module table — out of scope. See `Database/Triggers/README.md`. |

---

## 9. Objects investigated but excluded from module scope

The following real, existing database objects were discovered during dependency traversal because they reference `SalesDailyActivity`. They are **not extracted** because they belong, by name and evident purpose, to the HRMS/staging data-import module (`ImportCsv.aspx`, `HrmsDataImport.aspx`, `HrmsBatchManager.aspx`), not to the Monthly Sales Claims approval workflow. They are recorded here for transparency, not omitted silently.

| Object Name | Type | Dependency reason | Why excluded |
| --- | --- | --- | --- |
| `Process_StagingToFinal_FromJson` | Stored Procedure | Writes to `SalesDailyActivity` | Staging-to-final import job, not a claims workflow transition |
| `Process_StagingToFinal_FromJson_Idempotent` | Stored Procedure | Writes to `SalesDailyActivity` | Same as above, idempotent variant |
| `usp_SyncHierarchyAndSalesActivity` | Stored Procedure | Writes to `SalesDailyActivity` | HRMS reporting-hierarchy synchronisation |
| `usp_SyncHierarchyForBatch` | Stored Procedure | Writes to `SalesDailyActivity` | Same as above, batch variant |

---

## 10. Objects referenced but not found

See `MissingObjects.md` for full detail: `usp_ProcessApprovalRequestDetails`, `usp_GetApprovalRequestsForUser`, `usp_GetApprovers_WithComputedState`.

---

## 11. Totals

| Category | Count |
| --- | --- |
| Tables extracted | 16 |
| Views extracted | 1 |
| Functions extracted | 0 (none exist) |
| Table Types extracted | 3 |
| Triggers extracted | 0 (none exist) |
| Stored Procedures extracted | 36 |
| **Total objects extracted** | **56** |
| Objects referenced but missing from the database | 3 |
| Related objects identified and excluded as out-of-module-scope | 4 |
