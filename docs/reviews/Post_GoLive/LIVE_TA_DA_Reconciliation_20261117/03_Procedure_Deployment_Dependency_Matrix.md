# Procedure deployment dependency matrix

**Status: BLOCKED.** This is the order a future package would have to respect. It is not an executable deployment. No script in `Database/Deployments` was modified.

## Existing scripts that are not LIVE packages

| Script | What it guards | Why it is not a LIVE package |
| --- | --- | --- |
| `Database/Deployments/ApproverKmEdit/B_Apply_ApproverKmEdit_UAT.sql` | Server `MUMANMOAPP01\SQLEXPRESS`, catalog `AnmolDristiDev`, abort on `AnmolDristi`. `@Execute` defaults to 0. Preimage is the Auditor-only body, declared length 15451. The embedded next body reads `CategoryId`. | It refuses LIVE. Its next body is not the current git procedure, which reads `Designation_ID`. A LIVE catalog that already references `CategoryId` (supplied evidence) would also fail the Auditor-only preimage check. |
| `Database/Deployments/ApproverKmEdit/C_Apply_DesignationIdCorrection_UAT.sql` | Same server and `AnmolDristiDev` guards. Declared previous SHA-256 `B1C1F94904E4F84DD22C23E0ECE21F62B1AF43F83AEE614589D47EF760A04EC2`. Declared next SHA-256 `F6B07BE8B35F801CFE20053393B6B49E57BA175752FD56578049367E912915B9`. Those hashes are text inside the script. They were not recomputed here. | It refuses LIVE. It applies only when the stored module is already the `CategoryId` candidate, or is already the correction. It does not install the columns. |
| `Database/Deployments/ApproverKmEdit/D_Rollback_ApproverKmEdit_UAT.sql` | Untracked. Restores the Auditor-only body and can drop the three columns. See document 05. | Invalid for LIVE, and invalid for the `Designation_ID` candidate. |
| `Database/Deployments/ApprovalRequestActivityCounters/B_Apply_ActivityCounters_UAT.sql` | Untracked. Refuses `AnmolDristi`. Declared previous SHA-256 `3ECC03CBD6C1AFF40CF7F6B7ECF5E637399A7D07B4A6AA4AB1FB219231F7900F`, pinned in comments to git `98947eb`. Declared next SHA-256 `6FF6F1F59420DE3684ADC54EB3CBDBB14C46A97B128AC27D0C9F88C1B4CBE094`, declared LEN 8745. The comment says the hash was calculated in the repository and was not read from UAT. It also says the 2026-10-13 rollback hash `D14FEF7DFF5F8C4E008B2485D445B897CA4B76FBB60D6E5CEF68745894BE5A9D` is a different module. | It refuses LIVE. Its preimage is a git fingerprint, not a captured LIVE module. There is no rollback file beside it. |
| `Database/Deployments/MarkReadyAuditorRow/Production_B_Apply.sql` | A different procedure, `usp_MarkRequestReadyForPayment`, with `@Execute = 0`. | Out of scope. Its hashes are not preimages for the KM or counter objects. |

## Proposed order, after the evidence gates in document 06

The gates are open. Nothing below is authorized to run.

| Step | Object | Delta | Prerequisite | Preimage | Postimage | Transaction and errors | Expected impact | Stop |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Evidence | None | Named backup, current definitions, column metadata, least-privilege login, owner decision on C-3 for the recalc body | — | — | — | No catalog change | Any missing item in document 06 |
| 1 | `dbo.SalesDailyActivityKMHistory` | Add `ApprovalRequestId int NULL`, `ApproverRole nvarchar(20) NULL`, `ApproverLevel int NULL`. No default, FK, or index. | Step 0 shows all three names absent. Existing PK, FK, and activity index unchanged. | Column list and row count captured. | The three columns match the types above. Row count unchanged. Every pre-existing column unchanged. New values NULL. | One transaction with the procedure in step 2, so a failed procedure replace rolls the columns back. SQL Server rolls back these `ALTER TABLE` statements with the transaction. | Metadata only until a later approver save writes them. Employee history inserts keep working. | Any column already present with a different type, length, or nullability. Any partial set. Any open transaction at start. |
| 2 | `dbo.usp_UpdateActivityKM_WithHistory` | Replace the stored module with the body in `Database/Stored Procedures/Approval/usp_UpdateActivityKM_WithHistory.sql` at `21975c2` / current `HEAD` (they are the same file). Stored form will be `CREATE`/`ALTER` with the spacing SQL Server writes. Compare the normalized module, not a raw git hash. | Step 1 columns exist. `dbo.usp_RecalculateSalesClaim`, `usp_GetResolvedPolicy`, `usp_ResolveDesignation`, `usp_ResolveRegion`, `usp_ResolveBranch`, and `usp_ResolveGrade` are present and their captured definitions are accepted as the ones this procedure already calls. | Full `OBJECT_DEFINITION` captured and matched to an owner-accepted LIVE preimage. | Module equals the candidate after the same `ALTER`/`CREATE` normalization the UAT scripts use. `Designation_ID` present once. `CategoryId` absent. Insert column list contains the three audit names. | The procedure's own body uses `TRY/CATCH`, `XACT_ABORT OFF`, and rolls back when `XACT_STATE() <> 0`. The deployment transaction must still be the outer transaction. | Future approver KM saves can write the audit columns and then call `usp_RecalculateSalesClaim` for every current day on that request. Header status and approver pointers are not set by this procedure. | Preimage mismatch. Claim procedure definition not accepted. Policy or designation procedures would have to change to make the save safe. |
| 3 | `dbo.usp_RecalculateApprovalRequest` | Replace the single database object with `Database/Stored Procedures/Approval/usp_RecalculateApprovalRequest.sql` at `c9eba19` only if the captured LIVE financial section is the parent body (`98947eb`, same as `755b94e` for this file) plus nothing else. | Owner decision that this file, not `Database/Stored Procedures/Utilities/usp_RecalculateApprovalRequest.sql`, is the body allowed to overwrite the database object. C-3 is that decision. | Full LIVE module captured. Financial `SUM`, snapshot update, and `ReadyForPayment` return shown to match the parent. | Module contains the four counter assignments. The approved-amount `SUM` still excludes `Rejected` and `Returned`. No `BEGIN TRAN` / `COMMIT` inside the procedure. | The Approval candidate does not begin a transaction. Callers that already have one keep control. The Utilities file does begin a transaction and must not be deployed. | The next call, while status is not `ReadyForPayment`, refreshes the four counters and also rewrites `Approved*` and `FinalApprovedAmount` with the candidate formula. `ReadyForPayment` requests are unchanged. No mass backfill is part of this package. | LIVE financial section differs and the owner has not approved that replacement. Utilities file selected. Any caller that depends on the Utilities formula. |

## Dependencies that stay out of the package

These procedures are callers or callees. Their text is not part of the delta.

| Object | Relationship | Why it stays unchanged |
| --- | --- | --- |
| `dbo.usp_RecalculateSalesClaim` | Callee of the KM procedure. No repository body. | Financial authority for the KM save. A change would be a formula change. |
| `dbo.usp_GetResolvedPolicy` | Callee of the KM procedure, with NULL organisation parameters. Reads `CategoryId`. | Policy resolution. Rule 9. |
| `dbo.usp_ResolveDesignation`, `usp_ResolveRegion`, `usp_ResolveBranch`, `usp_ResolveGrade` | Callees of the KM gate. | Designation and organisation resolution. Rule 9. |
| `dbo.usp_ProcessApprovalAction` | Calls `usp_RecalculateApprovalRequest` inside its transaction (line 839). | Workflow pair. C-1 pending. |
| `dbo.usp_MarkRequestReadyForPayment` | Calls recalc before it writes `ReadyForPayment` (line 145). | Payment gate. C-1 and C-5 pending. |
| `dbo.usp_ForwardApprovalRequest` | Does not call recalc. Auditor forward raises 51012. | Routing. C-10 remains parked. |
| `dbo.usp_SubmitSalesDailyActivities` | Calls recalc after a new latest version (line 927). | Resubmission path. A counter deployment changes what that call persists. |
| `dbo.usp_ReturnClaimToEmployee` | Calls recalc (line 342). | L1 / employee return path. |
| `dbo.usp_UpdateSalesDailyExpenseStatus` | Calls recalc (line 258). | Expense decisions. |
| `dbo.usp_UpdateActivityKM` | Separate employee KM history insert. | Must keep compiling after the nullable columns exist. |
| `AnmolDristi/ClaimsApproval_v2.aspx.cs` | Calls `dbo.usp_RecalculateApprovalRequest`. | Application code is out of scope. |

## Financial and workflow effect of the proposed delta

| Area | Effect if steps 1–3 were later applied exactly as written |
| --- | --- |
| `TotalKM`, `ApprovedKM` | Step 2 can change kilometres only when an approver saves a new KM. Step 3's `SUM` for `ApprovedKM` is unchanged versus its parent, and it runs again on every caller. |
| `TotalClaimAmount`, `ApprovedClaimAmount`, `FinalApprovedAmount` | Step 2 delegates the write to `usp_RecalculateSalesClaim` (NOT VERIFIED). Step 3 rewrites `ApprovedClaimAmount` and `FinalApprovedAmount` with the Approval-file formula on the next non-payment call. |
| Current approver and level | Neither candidate updates those header columns. |
| Claim status | Unchanged by both candidates. `ReadyForPayment` remains a write lock inside recalc. |
| Approval history | Neither candidate inserts `ApprovalRequestStatusHistory`. |
| Payment readiness | Unchanged. Mark Ready's own status writes stay as they are. |
| Employee versus approver KM audit | Employee inserts leave the three columns NULL. Approver inserts fill them. Historical rows are not rewritten. |

If the captured LIVE financial body differs from the Approval parent, step 3 is a financial replacement and stops. The counter lines are not separable from the rest of the module once `CREATE/ALTER PROCEDURE` replaces the whole definition.
