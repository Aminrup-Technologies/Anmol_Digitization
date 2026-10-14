# TA/DA Production Deployment Checklist (v1.1 Post-Go-Live FSOT)

| Field | Value |
| --- | --- |
| Branch | `Aug26_AILWorks_AmimnrupSupport` |
| Production baseline tag | `tada-v1.1-postgolive-fsot-2026` |
| Supersedes | `tada-v1.0-go-live-2026` (`94bc2089`) — historical RC-001–RC-013 only; **do not ship alone** |
| Certification | `TA_DA_PostGoLive_Consolidated_Certification.md` (2026-09-16) |
| UAT financial proof | `docs/reviews/Post_GoLive/RC015G_FSOT_UAT_Execution.md` — Request **97** Final = Total = Snapshots = Excel = **₹7,736.44** |
| Schema / data scripts | None for DDL. Procedure bodies + `EXEC usp_RecalculateApprovalRequest` backfill only. |

Tick boxes during the production window. Do not push or publish until CAB approval.

---

## Pre-deployment

### Backup

- [ ] Full backup of the production SQL database (equivalent of `AnmolDristiDev`).
- [ ] Backup file / job name: `________________`
- [ ] Verify backup completed (size / job success).
- [ ] Script-out current live definitions (rollback source) for:

```text
dbo.usp_UpdateActivityKM
dbo.usp_ForwardApprovalRequest
dbo.usp_UpdateSalesDailyExpenseStatus
dbo.usp_ProcessApprovalAction
dbo.usp_MarkRequestReadyForPayment
dbo.usp_RecalculateApprovalRequest
dbo.usp_GetApprovalRequest
dbo.usp_Export_Auditor_Final_Excel
```

- [ ] Copy the live IIS site folder (at minimum `bin\AnmolDristi.dll` and published ASPX).
- [ ] IIS backup path: `________________`
- [ ] Live DLL timestamp / version: `________________`

### IIS readiness

- [ ] Application pool identified: `________________`
- [ ] Site bindings confirmed.
- [x] Change window agreed for the `usp_MarkRequestReadyForPayment` Production promotion by Kaushik Kumar Majhi at `2026-10-14 07:54 +05:30`. Idle or stop the TA/DA site only if the window requires it. This check does not approve the other package rows in this checklist.
- [ ] Do not deploy unrelated HRMS or Switch User packages in the same window unless separately signed off.

### Branch verification

- [ ] Package is built from `Aug26_AILWorks_AmimnrupSupport` at tag `tada-v1.1-postgolive-fsot-2026`.
- [ ] Release commit SHA: `________________`
- [ ] Confirm RC-015B four-part Stationary is in the package (`usp_UpdateSalesDailyExpenseStatus` — **mandatory**; not yet live on UAT expense SP).
- [ ] `.cursor/` is not in the package.
- [ ] No extra schema scripts in the packet.

### Package inventory (v1.1 delta over v1.0)

| Path | RCs |
| --- | --- |
| `AnmolDristi/ClaimsApproval_v2.aspx` / `.cs` / `.designer.cs` | RC-009–013 + RC-014D Open + RC-015A bind/Recalc |
| `AnmolDristi/SalesMonthlyReview.aspx` / `.cs` | RC-001, RC-007, RC-008, RC-010 |
| `AnmolDristi/MyClaimsView_v2.aspx` / `.cs` | RC-003–006, RC-010 + FSOT footer |
| `AnmolDristi/ApprovedClaimsView.aspx.cs` | RC-004 + FSOT bind |
| `Database/Stored Procedures/Approval/usp_ProcessApprovalAction.sql` | **RC-014B** |
| `Database/Stored Procedures/Approval/usp_MarkRequestReadyForPayment.sql` | **RC-014D** |
| `Database/Stored Procedures/Approval/usp_RecalculateApprovalRequest.sql` | **RC-015A** |
| `Database/Stored Procedures/Approval/usp_GetApprovalRequest.sql` (+ Retrieval twin) | **RC-015A** |
| `Database/Stored Procedures/Approval/usp_Export_Auditor_Final_Excel.sql` (+ Retrieval twin) | **RC-015C** |
| `Database/Stored Procedures/Approval/usp_UpdateSalesDailyExpenseStatus.sql` | RC-012B + **RC-015B Stationary** |
| Tag-era (already in v1.0): `usp_UpdateActivityKM`, `usp_ForwardApprovalRequest` | RC-008, RC-012A |

Inherited v1.0 objects still required if Production is behind UAT go-live:

| Path | RCs |
| --- | --- |
| `Database/Stored Procedures/Submission/usp_UpdateActivityKM.sql` | RC-008 |
| `Database/Stored Procedures/Approval/usp_ForwardApprovalRequest.sql` | RC-012A |

---

## Deployment

### Publish application

- [ ] After **manual** push and CAB approval, publish / copy the application package to the IIS site.
- [ ] Confirm published output is newer than the IIS backup.
- [ ] Record publish timestamp: `________________`

### Deploy stored procedures (order)

Apply repository `CREATE OR ALTER` (or shop-standard ALTER) bodies. Preserve signatures. Prefer Approval path; keep Retrieval twins in sync if Production deploys from Retrieval.

**If Production is still on pre-go-live (RC-001–013 gap):**

1. `usp_UpdateActivityKM` (RC-008)
2. `usp_ForwardApprovalRequest` (RC-012A)

**Post-go-live / FSOT package (mandatory for v1.1):**

3. **RC-015A Recalc** — `usp_RecalculateApprovalRequest.sql`
4. **Backfill `FinalApprovedAmount`** — execute Recalc for ReadyForPayment / Completed (and any other statuses Finance requires). Pattern: `EXEC dbo.usp_RecalculateApprovalRequest @ApprovalRequestId = <Id>;` — do **not** invent a separate UPDATE formula.
5. **RC-015A Get** — `usp_GetApprovalRequest.sql` (Approval + Retrieval twin if used)
6. **RC-015C Export** — `usp_Export_Auditor_Final_Excel.sql` (Approval + Retrieval twin if used)
7. **RC-015B expense SP** — `usp_UpdateSalesDailyExpenseStatus.sql` (four-part: DA+Hotel+Travel+**Stationary**). **Mandatory in this release** even if UAT still shows the three-part body.
8. **RC-014B** — `usp_ProcessApprovalAction.sql` (if not already on Production)
9. **RC-014D** — `usp_MarkRequestReadyForPayment.sql` (if not already on Production)

Post-apply checks:

- [ ] Recalc writes `FinalApprovedAmount` from latest non-Rejected/Returned DayNet.
- [ ] Get returns `ISNULL(FinalApprovedAmount, TotalApprovedAmount)` as Net Payable.
- [ ] Export uses latest Approved detail per ActivityId (no duplicate RESUBMIT rows).
- [ ] Expense status SP sets `Snapshot_StationaryAmount` and `ExpenseTotal = DA+Hotel+Travel+Stationary`.
- [ ] ProcessApprovalAction last-day Auditor path → `Completed` (not premature on first day).
- [ ] MarkReady uses named history INSERT; C# opens connection before call.
- [ ] No table, index, or ad-hoc money UPDATE script executed outside Recalc.

| Object | Production modify_date |
| --- | --- |
| `usp_UpdateActivityKM` | |
| `usp_ForwardApprovalRequest` | |
| `usp_UpdateSalesDailyExpenseStatus` (**015B**) | |
| `usp_ProcessApprovalAction` (**014B**) | |
| `usp_MarkRequestReadyForPayment` (**014D**) | |
| `usp_RecalculateApprovalRequest` (**015A**) | |
| `usp_GetApprovalRequest` (**015A**) | |
| `usp_Export_Auditor_Final_Excel` (**015C**) | |

---

## Post-deployment

### IIS recycle

- [ ] Recycle the application pool.
- [ ] Hit `SalesMonthlyReview.aspx`, `ClaimsApproval_v2.aspx`, `MyClaimsView_v2.aspx`, `ApprovedClaimsView.aspx` (authenticated).
- [ ] Confirm no yellow-screen / compile error.

### Browser cache

- [ ] Instruct L1/Auditor testers to hard-refresh (Ctrl+F5) or clear cache for `ClaimsApproval_v2` and `SalesMonthlyReview`.
- [ ] Confirm Forward confirmation modal and auto-close scripts load (RC-011 / RC-013).

### Smoke verification (v1.0 paths)

Repeat the scenarios in `TA_DA_Smoke_Test_Execution.md` on production after recycle:

- [ ] NEW submission
- [ ] Return
- [ ] KM edit
- [ ] RESUBMIT
- [ ] Latest-version (one row per day after RESUBMIT)
- [ ] Return notification
- [ ] Forward confirmation
- [ ] Auditor routing (header stays Pending until last day)
- [ ] Modal auto-close (no F5)

### Financial / FSOT proof (v1.1 — mandatory)

Retain Request **97** UAT proof as the certified reference (`RC015G`). On Production, re-proof an equivalent Ready-for-Payment claim (or Request 97 if same DB lineage):

- [x] Header `FinalApprovedAmount` = latest snapshot DayNet sum
- [x] `TotalApprovedAmount` reconciles (Stationary may still be omitted from computed Total; **Final is authority**)
- [x] Excel export SUM = Final (latest-version Approved rows only)
- [x] Status remains ReadyForPayment through Recalc/Get/Export
- [x] If Stationary ≠ 0 on any day: confirm expense SP snapshot includes Stationary and Final still matches (RC-015B gate)
- [x] Record Production request id / amounts: request **1541**, Final **16318.44**, latest snapshot sum **16318.44**, export expression **16318.44**, `TotalApprovedAmount` **15748.44**, Stationary **570.00**, observed `2026-10-14T02:46:28.9539131Z`

These boxes record the 08:16 read on `AnmolDristi`. DayNet is that snapshot sum (claim amount plus `Snapshot_ExpenseTotal`), not a separate object. The export figure is the deployed procedure text, evaluated by a read, not a spreadsheet file. Recalc and Get were not executed. Status was already `ReadyForPayment`. Request 97 stays excluded.

---

## Rollback

Use only if production smoke or financial proof fails, or the site will not start.

1. Copy the IIS backup over the live site (`AnmolDristi.dll` and published files).
2. Recycle the application pool.
3. Run the **pre-deploy** scripted-out procedure definitions (not the repository RC files).
4. Confirm `modify_date` reverted.
5. Do not restore tables or data unless Finance explicitly authorizes a Final backfill undo (Recalc is re-runnable).

Exit criteria after rollback: L1 inbox loads; employee month grid loads; incident logged with SHA, backup path, and failed scenario.

---

## Sign-off

| Check | Owner | Time | Result |
| --- | --- | --- | --- |
| IIS backup taken | | | |
| DB backup taken | | | |
| App published | | | |
| RC-015A Recalc + Final backfill applied | | | |
| RC-015A Get applied | | | |
| RC-015C Export applied | | | |
| RC-015B expense Stationary SP applied | | | |
| RC-014B / RC-014D applied (if needed) | | | |
| IIS recycled | | | |
| Browser cache cleared | | | |
| Production smoke PASS | | | |
| Financial proof (Req 97 pattern) PASS | | | |
| Production approved / rolled back | | | |

Deployer: Kaushik Kumar Majhi — recorded `2026-10-14 07:53 +05:30` for the `usp_MarkRequestReadyForPayment` Production promotion. This does not sign the earlier package rows above.  
Approver (CAB): Kaushik Kumar Majhi — recorded `2026-10-14 07:53 +05:30`. The `13-10-2026 07:45 AM` time was stated on 14 October and was not used.  
Date: `2026-10-14 07:53 +05:30`

---

## RC-023 — Production Hardening (additive package)

Use this section when promoting commit `4644baf` after the v1.1 / RC-017–022 baselines. Does not replace the v1.1 steps above.

| Field | Value |
| --- | --- |
| Commit (short) | `4644baf` |
| Commit (full) | `4644baf4caa051b0f5d32cc76f1479a0d344a4d4` |
| Commit timestamp | 2026-09-23 13:04:41 +0530 |
| RC-023 UAT Tag | `rc-023-uat-certified` @ `4644baf` (pushed) |
| DLL SHA256 | `04D0250EE80DABB519992CCC70FAB1CB8FCF35D562ACBE33D5A9E9E055AB09BF` |
| Build timestamp | 2026-09-23 12:54:28 +0530 |
| Diff | 5 files, +328 / −73 |

### Scope (5 files)

| Path | Notes |
| --- | --- |
| `AnmolDristi/ClaimsApproval_v2.aspx.cs` | RC-022-aligned finalize + ReadyForPayment lock |
| `AnmolDristi/SalesApprovalDetailVersionHelper.cs` | `DetailVersion` → `MAX(ApprovalRequestDetailId)` |
| `Database/Stored Procedures/Approval/usp_UpdateSalesDailyExpenseStatus.sql` | Auth + payment lock + **RC-015B Stationary** |
| `Database/Stored Procedures/Approval/usp_ProcessApprovalAction.sql` | Version→Id + Completed/ReadyForPayment lock |
| `Database/Stored Procedures/Approval/usp_MarkRequestReadyForPayment.sql` | Optional unless target body differs from RC-014D. |

### SQL delta (apply after rollback script-out)

| Order | Procedure | CAB note |
| --- | --- | --- |
| 1 | `usp_UpdateSalesDailyExpenseStatus` | **Mandatory** — closes RC-015B Stationary + RC-023 auth/locks |
| 2 | `usp_ProcessApprovalAction` | **Mandatory** — latest-version + payment lock |
| 3 | `usp_MarkRequestReadyForPayment` | Optional unless target body differs from RC-014D. |

### UAT evidence placeholders

| Check | Result | Evidence |
| --- | --- | --- |
| UAT expense SP `modify_date` after ALTER | **PASS** | `2026-09-15 12:03:55.347` → `2026-09-23 13:51:38.003` (QI ON redeploy); pack `pvt-evidence/2026-09-23-rc023-uat/` |
| UAT ProcessApprovalAction `modify_date` after ALTER | **PASS** | `2026-09-15 18:04:06.090` → `2026-09-23 13:51:38.750` (CREATE OR ALTER + QI ON) |
| DLL published / AppDomain recycle | **PASS** | SHA `04D0250E…AB09BF`; recycle `2026-09-23 13:44:38.581 +05:30`; build stamp `2026-09-23 12:54:28`; Web.config not retargeted |
| Non-current approver expense refused | **PASS** | R106 ExpenseId `2410`; ActionBy `AHS1605` → *You are not the current approver…*; current `AHS1870` mutate PASS (txn rolled back) |
| Mutation blocked on Completed / ReadyForPayment | **PASS** | R98/R107 expense + day locks → *locked for payment…* (`06-auth-finalize-gates.txt`) |
| Stationary ≠ 0 Final / snapshot / Excel | **PASS** | R106 Stationary `800` Snap=Live=Header; Final `6971.06` PASS. R107 Excel 31 rows / Stationary `242` / FinalSum=`9384.29` = header |
| Level-aware finalize (L1-Approved blocks Auditor Ready) | **PASS** | R94: UnresolvedCount=`21` L1-Approved; `usp_MarkRequestReadyForPayment` → *Request not completed.* |

### CAB deployment inventory checklist

- [x] Rollback scripts captured for the three SQL objects above.
- [x] Package built from `4644baf` (DLL SHA matches table).
- [x] UAT ALTER order 1 → 2 (→ 3 optional) completed. MarkReady **SKIPPED** (UAT body = RC-014D runtime).
- [x] UAT evidence placeholders filled (**no Production cutover with blanks**).
- [x] Production ALTER same order + IIS publish/recycle.
- [x] Production `DbConn` / catalog retarget confirmed (`AnmolDristi` @ `115.112.186.71`).
- [x] RC-023 sign-off: Deployer Cursor Agent / CAB GO / Date 2026-09-23

### RC-024 LIVE CAB execution (2026-09-23)

Pack: `pvt-evidence/2026-09-23-rc024-live-cab/` (retained outside Git).

| Gate | Result | Evidence |
| --- | --- | --- |
| LIVE SQL COPY_ONLY backup | **PASS** (verified for cutover) | Pre-CAB pack `backups/`; LIVE proceed authorized after ops backup verification |
| IIS package backup | **PASS** | `backups/iis-package-20260923-140814/` @ `2026-09-23 14:08:14 +05:30` |
| DLL SHA match RC-023 | **PASS** | `04D0250EE80DABB519992CCC70FAB1CB8FCF35D562ACBE33D5A9E9E055AB09BF` |
| Schema baseline (RC-012) | **PASS** | `DetailVersion`, `PreviousDetailId`, version index promoted on LIVE |
| LIVE SP rollback script-out | **PASS** | `rollback/AnmolDristi/` |
| Phase B LIVE ALTER (QI ON) | **PASS** | Expense `15:50:10.997`; ProcessApprovalAction `15:50:12.267`; MarkReady `15:50:13.437` — all `QuotedIdentOn=1` |
| Phase C IIS publish / DbConn / recycle | **PASS** | Publish `16:02:51.339`; recycle `16:02:54.952`; catalog `AnmolDristi`; VMS preserved |
| Canary (appsuat → LIVE DB) | **PASS** | `RC024_Canary_Smoke_UAT_to_LIVE.md` — read-only SAFE TO REFRESH |
| Production refresh | **PASS** | https://apps.anmolindustries.com refreshed 23–24 Sep 2026 |
| UAT restore post-cutover | **PASS** | DeployPackages `DbConn` restored to `AnmolDristiDev` @ `2026-09-24 02:02:50 +05:30` |

**Production Go / No-Go:** **GO** — RC-023 package live on Production (`4644baf` / `rc-023-uat-certified`).

---

## LIVE deployment closure — SO-TA/DA_ClaimsApplication

Operational closure for the TA/DA Claims release. RC-001 through RC-024 records above are unchanged. No dedicated RC-017 execution log exists for this release; this section is the closure entry on the existing LIVE deployment checklist. Certified baseline documents and existing UAT evidence are not rewritten here.

UAT evidence: `AnmolDristi/Database/Operational/TA_DA_SQL_Reconciliation/TA_DA_UAT_CLOSURE_EVIDENCE.md`. SQL reconciliation package: `AnmolDristi/Database/Operational/TA_DA_SQL_Reconciliation/`.

### Classification

| Item | Classification |
| --- | --- |
| UAT | RESOLVED / PASS |
| SQL reconciliation | RESOLVED / semantic-executable parity PASS |
| SQL checksum exactness | ACCEPTED — text-only residue |
| LIVE artifact identity | ACCEPTED DEVIATION |
| Rollback artifacts | PRESERVED |
| Future LIVE incidents | Investigate against actual deployed state |

### Closure statement

LIVE TA/DA deployment is closed with an Accepted Deviation on application binary identity. LIVE SQL reconciliation achieved semantic/executable parity; the verification checksum retains known text-only residue and is not treated as executable SQL drift. UAT completed with GO. Pre-deployment LIVE application and bin rollback artifacts remain preserved. The actual LIVE AnmolDristi.dll SHA-256 is 15B6B16FDDF70338E9152E35C6B3C5B1C1A3B3043844786ECF2D558AF85B9708, which differs from the certified release artifact SHA-256 268FFFE7C61879220877F49B0237F31BC080F04D03BDCDAA3DE754206FEBAC58. This binary identity difference is accepted for the current LIVE state. Future LIVE incidents shall be assessed against the actual deployed binary, current LIVE SQL state, production evidence, and preserved rollback artifacts.

SQL semantic/executable parity PASS; checksum verifier retains accepted text-only residue.

### Identities

These four identities stay distinct. The certified DLL hash is the release baseline. It is not recorded as the DLL that is on LIVE.

| Role | Identity |
| --- | --- |
| Project | SO-TA/DA_ClaimsApplication |
| Lifecycle | Employee -> L1 -> Auditor -> Completed -> ReadyForPayment |
| Certified source commit | `70ff21a30cf146881507165e96b566a09184ed42` |
| Certified WebForms DLL SHA-256 | `268FFFE7C61879220877F49B0237F31BC080F04D03BDCDAA3DE754206FEBAC58` |
| Certified assembly / file version | 1.0.0.0 |
| Actual LIVE DLL | `D:\anmolapps\qaqcroot\bin\AnmolDristi.dll` |
| Actual LIVE DLL SHA-256 | `15B6B16FDDF70338E9152E35C6B3C5B1C1A3B3043844786ECF2D558AF85B9708` |
| LIVE artifact identity | ACCEPTED DEVIATION |

### LIVE environment

| Field | Value |
| --- | --- |
| SQL Server | `MUMANMOAPP01\SQLEXPRESS` |
| LIVE database | `AnmolDristi` |
| IIS server | `MUMANMOAPP01` |
| IIS site | `qaqc` |
| IIS app pool | `qaqc` |
| LIVE physical path | `D:\anmolapps\qaqcroot` |

### UAT

| Field | Value |
| --- | --- |
| UAT database | `AnmolDristiDev` |
| Tester | Ayushi Jaiswal |
| Test date | 2026-10-08 |
| UAT result | PASS |
| Release decision | GO |
| Requests exercised | #111 and #112 |

UAT closure evidence established latest-version behavior, resubmission preservation, approval flow, Completed, ReadyForPayment, and SQL-side approved amount evidence. The controlled returned-KM edit itself was not proven in the DB evidence.

### SQL reconciliation

TA/DA SQL reconciliation was executed against LIVE `AnmolDristi`. Semantic/executable parity is accepted. The verification output still reports checksum/length FAIL for ten procedures. The remaining differences are known text-only formatting/banner residue and are not executable SQL differences. This record does not treat that result as exact checksum equality.

### LIVE database backup

| Field | Value |
| --- | --- |
| Backup file | `D:\anmoldb\Backups\AnmolDristi_23Sep26.bak` |
| RESTORE HEADERONLY backup start | 2026-10-08 11:02:39 |
| RESTORE HEADERONLY backup finish | 2026-10-08 11:02:57 |
| Backup type | Full database backup |
| IsDamaged | 0 |
| Checksums | Present |
| RESTORE VERIFYONLY | Passed. "The backup set on file 1 is valid." |

### Rollback artifacts

Preserved. This closure does not modify them.

| Artifact | Value |
| --- | --- |
| Pre-deployment LIVE DLL copy | `D:\Last_Published\2026\10.Oct26\AnmolDristi.dll` |
| Pre-deployment LIVE DLL SHA-256 | `2387BB161F9A1C001451489FA162D501F5CF58B3C49CE9AA4BA7E7094F217F60` |
| Pre-deployment LIVE bin folder | `D:\Last_Published\2026\10.Oct26\bin` |

### Future LIVE incidents

Assess a future LIVE incident against:

1. the actual deployed LIVE DLL, SHA-256 `15B6B16FDDF70338E9152E35C6B3C5B1C1A3B3043844786ECF2D558AF85B9708`;
2. the current LIVE SQL definitions and state;
3. the actual production logs and data;
4. the preserved rollback artifacts, where relevant.

A future incident is not attributed to certified source commit `70ff21a30cf146881507165e96b566a09184ed42` merely because that commit was the intended release source.

No deployment clock, IIS recycle clock, LIVE smoke-test result, or user-incident status is stated in this section. Those facts were not part of the supplied closure evidence.

## L2-R19 UAT execution gate

This section does not close the 2026-10-08 UAT result above and does not authorize production. On 2026-10-13 the read-only preflight SELECT result sets were run on `AnmolDristiDev`. The reader rejected the literal `A_Preflight_ReadOnly_UAT.sql` batch because it also contains `SET NOCOUNT` and `THROW`. `B_Apply_L2R19_Finalization_UAT.sql`, `C_PostApply_Verification_UAT.sql`, `D_Rollback_L2R19_Finalization_UAT.sql`, and `E_UAT_Evidence_Queries.sql` were not executed. Every test below is **NOT TESTED**.

The two-procedure apply body is withdrawn. Do not run a saved copy. `B_Apply_L2R19_Finalization_UAT.sql` now contains five working-tree procedures and returns before any catalog read. Its older expected Mark Ready catalog hash `6E953857C3F13363040D0CF68F1A406DC6FB2788442F27E31538EF16B3994D62` (LEN 10438) is not the current candidate. The Recalculate catalog hash below is the same text as that earlier candidate.

| Field | Observed 2026-10-13 | Execution record |
| --- | --- | --- |
| Branch | `ai/rc-014b-usp-processapprovalaction` | |
| HEAD | `39a12b0a3dc9ebb9636e8b9fab62c20d3d9a235c` | |
| Tag `tada-v1.1-postgolive-fsot-2026` | `2f361f3434de3985949073ec3a06ba08ccb3aaa6` | |
| Checklist certified source commit | `70ff21a30cf146881507165e96b566a09184ed42` (recorded above; not re-resolved in this pass) | |
| `stash@{0}` | `97d5e0d6121f4a095eede5f6db2c450926f455f8` (not applied) | |
| UAT target | `AnmolDristiDev` on `MUMANMOAPP01\SQLEXPRESS` | Re-read 2026-10-13: server `MUMANMOAPP01\SQLEXPRESS`, database `AnmolDristiDev`, product `15.0.2000.5`, `@@TRANCOUNT` 0. Identity matches. |
| Operator | | Name: |
| Apply start / finish | | |
| Owner approval | Not given | Reference: |
| Newest full backup | 2026-10-08 11:02:17, type `D`, not damaged, checksums present, `D:\anmoldb\Backups\AnmolDristiDev_23Sep26.bak` | Preflight read again: same newest finish, `BackupGate` DOES NOT QUALIFY. `RESTORE VERIFYONLY` was not run. Gate: **BLOCKED**. |
| Pre-test backup finish | | Must be a new undamaged full backup after 2026-10-12 14:02:57.583 |
| `ClaimsApproval_v2.aspx.cs` SHA-256 | `C2039EFD6DE22F4A2B3D73BD757C24C8D55347B245C049132ED6ABE90BC9E4E2` | UAT IIS publish of this caller is not part of the SQL package and was not done. |
| Preflight evidence | `docs/reviews/L2_Auditor_AsIs/L2R19_preflight_readonly_2026-10-13T070445Z.txt` | Hash, object, index, and backup rows recorded. Definition bodies were read and their character lengths matched `LEN`. Those bodies were not pasted into `D_Rollback`. |

Source file SHA-256 is the raw working-tree file. Catalog SHA-256 is `HASHBYTES('SHA2_256', CONVERT(varbinary(max), definition))` over UTF-16LE. Catalog text is the working-tree file with `CREATE OR ALTER PROCEDURE` normalized to `CREATE PROCEDURE`.

| Procedure | Source file SHA-256 | Candidate catalog LEN / SHA-256 | Live preflight LEN / SHA-256 | Rollback preimage |
| --- | --- | --- | --- | --- |
| `usp_UpdateSalesDailyExpenseStatus` | `EC79DABDF4D81223C28702965AF016F930702658B09A632933F3268F7B8DE9C5` | 12445 / `7C9007750F36C24F696B56EBC1AD7FC0A2CEC6441AD45A00815934D4FBC7E040` | 8010 / `3DA56C797403A1CC0EBFF57CE5F7FF5705DC7E036E9B6EFC7A1F272FBC34EC9E` | Live hash known. Body not pasted. **BLOCKED**. |
| `usp_ProcessApprovalAction` | `B8E2ADDCB0FDEFC3F8B46B643C65D888E90608059D1E06C17E76E5ECB4DA3059` | 42021 / `3E86754A9C605F752468C616D9923BD7B22F2396FE84363FAAB0E5CFBDA04EB9` | 33635 / `F6471A308779E26D74A2B84B00A6DAF71DAA84FDFD5A7C71189F0DA5B61E13EE` | Live hash known. Body not pasted. **BLOCKED**. |
| `usp_SubmitSalesDailyActivities` | `FBB818425BA37003CFC449A28D39555603D4445038FD857103F2D3483AD68EB9` | 28066 / `18B9D5854ED396CCDD2CCC94AD80E6904FA1891C9DF3F907BD94D312F1A472BF` | 24828 / `5BEDA23C85279789D325C66B573C37BB1E037DA5C0153695A435DEA9E7D37547` | Live hash known. Body not pasted. **BLOCKED**. |
| `usp_MarkRequestReadyForPayment` | `722E58AE836E9A7D4C25BAF33270A8461A065D0402985E58229B55E46C692B48` | 11179 / `118A8333EC49E8C11AAA2BCE46F4490A3DE7D11FD9EC84E01D6990DA8EBB4274` | 2882 / `8B2ECBCE8AC258D67A2C3CFCC6C45CC385F01354B8F5D8B5594409889685E158` | Live hash equals the hash already recorded for the embedded preimage. |
| `usp_RecalculateApprovalRequest` | `4E42FE742D03A34969A3529CCA92DAB901D55B5AB3467756D02C783F3D09DE34` | 7557 / `3ECC03CBD6C1AFF40CF7F6B7ECF5E637399A7D07B4A6AA4AB1FB219231F7900F` | 6940 / `D14FEF7DFF5F8C4E008B2485D445B897CA4B76FBB60D6E5CEF68745894BE5A9D` | Live hash known. Body not pasted. **BLOCKED**. |

`D_Rollback_L2R19_Finalization_UAT.sql` restores five modules only after the four live bodies whose hashes are now known are pasted from a hash-verified export. It does not restore claim rows. Claim recovery is the new full backup, followed by a separately authorized restore. No restore was run. Apply and rollback gates stay closed.

Create every test claim through `SalesMonthlyReview`. Do not insert claim rows. Do not use claims 97, 100, 111, 112, 118, 119, 121, or 122. Read evidence with `E_UAT_Evidence_Queries.sql` before and after each action. Capture the pre-state after the modal has opened, because opening the modal can recalculate before Mark Ready.

| Test | Setup | Action | Expected result | Evidence to retain |
| --- | --- | --- | --- | --- |
| 1. Pending Auditor | Application claim at Auditor. Header Pending. One Pending Auditor row. Latest days and expenses resolved at the current level. | Auditor clicks Mark Ready on the reviewed v2 page. | Header becomes ReadyForPayment. Auditor row becomes Approved once. History gains Pending to Completed, then Completed to ReadyForPayment. `FinalApprovedAmount` equals the latest-detail payable sum. | Two evidence outputs, the success message, and the history rows. |
| 2. Approved Auditor and Completed header | Application shape where the Auditor row is already Approved and the header is Completed. | Same Mark Ready click. | No second Auditor update. No second Pending to Completed history row. One Completed to ReadyForPayment history row. Header ReadyForPayment. | Evidence before and after, showing the Auditor row unchanged. |
| 3. Unresolved latest day, including Returned | Latest day is Returned, or Approved below the current level. | Mark Ready. | UI refuses. If the procedure is reached, error 59109 and no committed status, history, total, snapshot, or approver change from the pre-call evidence. | Error text and both evidence outputs. |
| 4. Unresolved expense | Latest activity has a non-deleted expense that is not Approved or Rejected. | Mark Ready. | Same refusal as test 3. | Error text and both evidence outputs. |
| 5. Stale approved totals | Header `Approved*` / `FinalApprovedAmount` differ from the latest-detail sum before the click. | Mark Ready. | After success, `FinalApprovedAmount` and `ApprovedKM` equal that sum. | Before and after evidence, including the reconciliation result. |
| 6. Second attempt | Claim already ReadyForPayment from test 1 or 2. | Mark Ready again. | Error 59104. No new history row and no second Auditor write. | Evidence showing the same history ids. |
| 7. Two sessions | Two authorized sessions, one request. Session A marks ready. Session B approves a day or forwards. | Run them together. On error 1205, the test client may retry Mark Ready at most 3 times. | One outcome commits. A deadlock rolls back the failed transaction. No partial history or total remains from the failed attempt. | Both session messages, evidence after each attempt, and any 1205 text. |
| 8. Failure after recalculation | Application claim that passes the Auditor and status checks, then a latest day or expense is returned through the application before the procedure finishes its checks. | Call `dbo.usp_MarkRequestReadyForPayment` once for that claim. The UI may refuse earlier; record that separately. | Error 59109. Totals, snapshots, status, history, and the Auditor row match the evidence captured immediately before the call. | Pre-call evidence, error 59109, and post-call evidence. |
| 9. Non-Auditor and invalid status | A non-Auditor user, and a claim whose header is not Pending or Completed. | Mark Ready. | Non-Auditor: error 59106. Other status: error 59105. No ReadyForPayment write. | Error text and evidence. |
| 10. Financial reconciliation | Any claim used above. | Run the evidence script. It selects the highest `DetailVersion`, then the highest `ApprovalRequestDetailId` in that version. | `FinalApprovedAmount` equals the sum of latest non-Rejected, non-Returned claim plus expense totals. `TotalApprovedAmount` remains the computed column without Stationary. | Reconciliation result set. |
| 11. Auditor row and history | Tests 1 and 2. | Compare approver and history result sets. | Pending shape: one Pending to Approved Auditor write and two history rows. Completed shape: Auditor row not written again and only the ReadyForPayment history row is new. | Approver and history outputs. |
| 12. Payment not released | After a successful test 1 or 2. | Do not use a payment action. | Header stays ReadyForPayment. The newest history row is Completed to ReadyForPayment. | Final evidence output. |
| 13. Expense return | Application claim pending with L1. One non-deleted expense is returned from `ClaimsApproval_v2`. | L1 returns that expense. | The expense update and the existing day-return path commit together or roll back together. Latest detail, activity, non-rejected expenses on that day, the current approver row, and the header are `Returned`. `FinalApprovedAmount` excludes that returned latest day. | Evidence before and after, plus the employee page showing the day open for correction. |
| 14. Resubmission recalculation | The returned day from test 13, corrected by the employee. | Employee resubmits from `SalesMonthlyReview`. | Header is `Pending` with current approver L1 at level 1. `FinalApprovedAmount` includes the new `Pending` latest row. | Evidence after resubmission, before an approver opens the claim. |
| 15. Current-approver Mark Ready | Pending request whose current approver is not the person clicking Mark Ready. | Mark Ready. | Error 59112. No status, history, total, or approver change. | Error text and both evidence outputs. |
| 16. Stale day-action authorization | Two sessions. One session returns or forwards the claim. The other then approves a day. | Run the second action after the first commits. | The second action is refused. The first outcome stays intact. | Both messages and evidence after each action. |
| 17. Unresolved-expense forwarding | Another latest activity still has a non-deleted `Pending` or `Returned` expense. | Approve the last open day, or use Forward. | The day action or forward rolls back. The refusal text is the 51013 message. Header and current approver stay as they were. | Error text and both evidence outputs. |

Cleanup for a failed or partial test is the reviewed pre-test backup restore. That restore is not available until the backup gate above is closed, and it is not authorized by this section. Procedure rollback is `D_Rollback_L2R19_Finalization_UAT.sql`, which stays closed and does not restore claim rows. Tests 13–17 are **NOT TESTED**. They do not replace tests 1–12.

### Execution attempt 2026-10-13 13:35 +05:30

Stopped before apply. No script in the UAT sequence was executed. The five procedures were not altered. Browser tests 1–17 were not run.

| Check | Observed |
| --- | --- |
| Connection | `MUMANMOAPP01\SQLEXPRESS` / `AnmolDristiDev`, `@@TRANCOUNT` 0, at `2026-10-13T13:35:55.391` |
| Apply-side preimage hashes in `B_Apply` | Populated for all five procedures. Not re-audited. |
| Rollback preimages | `HASH_VERIFICATION.txt` remains `rollback_embedded_recheck=PASS` for all five. Rollback flags stay `@Execute` 0, `@RollbackApproved` 0, `@AllFivePreimagesPresent` 0. Not re-audited. |
| `B_Apply` flags | Still closed: `@Execute` 0, `@OwnerApproved` 0, `@OwnerName` empty, `@RequiredBackupFinish` NULL, `@BackupVerifyOnlyPassed` 0, `@AppPublishAuthorizedBy` empty, `@PreimageBodiesSaved` 0 |
| Newest `AnmolDristiDev` full backup in `msdb` | `2026-10-08 11:02:17`, type `D`, not damaged, checksums present, `D:\anmoldb\Backups\AnmolDristiDev_23Sep26.bak`. Finish is before `2026-10-12 14:02:57.583`. |
| `AnmolDristi` full backup | Newest finish `2026-10-08 11:02:57`, `D:\anmoldb\Backups\AnmolDristi_23Sep26.bak`. This is the live database and does not satisfy the Dev gate. |
| Owner exception | None in `B_Apply`. None invented. |
| Scripts A, B, C, E, and `InspectSubmittedAndL1ReturnedClaims.sql` | Not run |
| `D_Rollback` | Not run |
| Tests 1–17 | **NOT TESTED** |

**Blocker:** no qualifying full backup of `AnmolDristiDev` and no approved exception. Apply stays stopped.

### Execution attempt 2026-10-13 14:15 +05:30

Stopped before claim mutations. `B_Apply_L2R19_Finalization_UAT.sql` was not executed. Its flags are still closed. `C_PostApply_Verification_UAT.sql`, `D_Rollback_L2R19_Finalization_UAT.sql`, and `E_UAT_Evidence_Queries.sql` were not executed. The reader comparison below is a SELECT of catalog length and SHA-256 only. Tests 1–17 were not run.

| Check | Observed |
| --- | --- |
| SQL identity | `MUMANMOAPP01\SQLEXPRESS` / `AnmolDristiDev`, product `15.0.2000.5`, `@@TRANCOUNT` 0 |
| `B_Apply` flags | Still closed: `@Execute` 0, `@OwnerApproved` 0, `@OwnerName` empty, `@RequiredBackupFinish` NULL, `@BackupVerifyOnlyPassed` 0, `@AppPublishAuthorizedBy` empty, `@PreimageBodiesSaved` 0 |
| Newest `AnmolDristiDev` full backup | `2026-10-13 13:39:00.000`, type `D`, not damaged, checksums present, `D:\anmoldb\Backups\AnmolDristiDev_23Sep26.bak`, size 991603712. Finish is after `2026-10-12 14:02:57.583`. `RESTORE VERIFYONLY` is not attested. |
| `AnmolDristi` full backup | Newest finish `2026-10-13 13:39:34.000`, same style of file under `D:\anmoldb\Backups\AnmolDristi_23Sep26.bak`. Live backup. It does not satisfy the Dev gate. |
| Release package | Local folder `D:\DeployPackages`. `bin\AnmolDristi.dll` `2026-10-13 14:02:57`, 2929664 bytes, SHA-256 `4ed28abd9dfb3ca3e7e9855a031e17ea938baedb57e3d4083cce5fd53d05b39e`. Published on workstation `AILL079KOL`. Not copied to UAT IIS by this run. |
| UAT IIS identity | `https://appsuat.anmolindustries.com` responds from Microsoft-IIS/10.0, ASP.NET `4.0.30319`. `\\MUMANMOAPP01\d$` was not reachable from this workstation, and local IIS configuration could not be read. Today's DLL was not shown to be the site binary. |
| Login page | **PASS** as an unauthenticated render. `login.aspx` HTTP 200. Logo, Bootstrap, Font Awesome, `custom.min.css`, `WebResource.axd`, and `ScriptResource.axd` returned 200. No yellow screen. |
| Authenticated navigation | **BLOCKED**. No AHO445 password was available in this run. Password hashes were not read. |
| `ClaimsApproval_v2.aspx`, `SalesMonthlyReview.aspx`, `SwitchUser.aspx` | Each unauthenticated request rendered the login page. No yellow screen and no missing resource on the login render. Signed-in TA/DA behavior was **NOT TESTED**. |
| Assembly-load symptom | Not observed on the login request. The published `Web.config` has no `System.Net.Http` binding redirect. The Release publish log was not re-opened. |
| Five target procedures | `modify_date` is `2026-10-13 14:10:34.793` through `2026-10-13 14:12:05.983`. This run did not alter them. Catalog SHA-256 does not match the L2-R19 candidate. |
| `usp_ForwardApprovalRequest` | Unchanged `modify_date` `2026-10-08 08:50:40.420`. Definition contains `51013`. |
| Tests 1–17 | **NOT TESTED** |

Candidate catalog versus the module text read after 14:12. Stripping the trailing CR LF does not produce the candidate hash.

| Procedure | Candidate LEN / SHA-256 | Live LEN / SHA-256 | Result |
| --- | --- | --- | --- |
| `usp_UpdateSalesDailyExpenseStatus` | 12445 / `7C9007750F36C24F696B56EBC1AD7FC0A2CEC6441AD45A00815934D4FBC7E040` | 12447 / `49A07B313C1A1217B3A00B5768E7CC8C81D2A12D03469872A64B1E528C9E8771` | **FAIL** |
| `usp_ProcessApprovalAction` | 42021 / `3E86754A9C605F752468C616D9923BD7B22F2396FE84363FAAB0E5CFBDA04EB9` | 42023 / `D2B1CA8BB2E5F03F705FBE5E198E24F7D677A65F1983549C1322A934C87D8FC1` | **FAIL** |
| `usp_SubmitSalesDailyActivities` | 28066 / `18B9D5854ED396CCDD2CCC94AD80E6904FA1891C9DF3F907BD94D312F1A472BF` | 28068 / `ACC25B22E08D140981CF5FA3F448EFF790B0002498ADEE1E86704B888ED2B29C` | **FAIL** |
| `usp_MarkRequestReadyForPayment` | 11179 / `118A8333EC49E8C11AAA2BCE46F4490A3DE7D11FD9EC84E01D6990DA8EBB4274` | 11181 / `E9097D3A83D66D61960F52C7B599F47F595B8943662601C4F4F0A9E6402FB409` | **FAIL** |
| `usp_RecalculateApprovalRequest` | 7557 / `3ECC03CBD6C1AFF40CF7F6B7ECF5E637399A7D07B4A6AA4AB1FB219231F7900F` | 7559 / `05D126B559E6FB182604F0E37C2921B3F4A357BD02D8B0A9706714C6B5F0BF6D` | **FAIL** |

The live Recalculate text contains `l2r19_recalculate_participates_in_caller_transaction`. The live Mark Ready text contains `59112` and `59109`. Those markers show a related body, not the reviewed catalog text.

**Blocker:** `B_Apply` is still closed, and the five live modules already differ from the candidate catalog. Apply was not repeated. Claim tests were not started.

### Employee submission 2026-10-13 19:12 +05:30

Local `http://localhost:53713` against `AnmolDristiDev`. No procedure script was executed. No day was edited and no expense was added. The September submit control posts every listed draft day; that is the page's monthly claim, not a one-day submit.

| Check | Observed |
| --- | --- |
| Effective employee | Banner: Impersonating Anup Kumar (AHS1121). Original user: Kaushik Kumar Majhi (AHO445). Policy label `AHS1121`. |
| Month | September 2026. 30 draft days. 1,336.77 km. TA ₹5,012.92. DA, Hotel, Travel, and Stationary ₹0.00. |
| Policy-disabled note | Info toast and checklist text: "Monthly processing policy is disabled." `EnablePreviousMonthPolicy` is false, so `TryValidateSubmitPolicy` allows the submit. The Submit button was enabled. |
| Confirmation | "Are you sure you want to submit all listed activities for approval? Once submitted, they will be locked for editing." |
| Result | Redirect to `MyClaimsView_v2.aspx?year=2026&month=9&requestId=130&open=view&mode=NEW&count=30`. Message: "Monthly claim submitted successfully. Request #130 has been created and sent for approval." |
| Claim | Request **130**. Status Pending. Pending with L1 Shtrughana Kumar. Requested ₹5,012.92. Submitted 13-Oct-2026 07:12 PM. Not a protected claim. |
| L1 queue | After returning to AHO445 and impersonating Shtrughana Kumar (AHS1624), `ClaimsApproval_v2.aspx` lists `130 Anup Kumar (AHS1121)`, September, ₹5,012.92, Pending, Action Required L1. No approval or forward was clicked. |
| Auditor label | Resolved policy modal listed Auditor Rohan Saraf. The submitted tracker listed Auditor Megha Paramanick. |

### L1 approval attempt 2026-10-13 20:35 +05:30

Request 130 was opened on `ClaimsApproval_v2.aspx` while impersonating AHS1624. No policy or routing row was changed. `B_Apply_L2R19_Finalization_UAT.sql` was not executed. The first confirm postback was lost when the session returned to `login.aspx`; a SELECT immediately after that redirect still showed 30 Pending days and only the initial history row. The session was signed in again as AHO445 and switched back to AHS1624 before the second attempt.

| Check | Observed |
| --- | --- |
| Banner before the action | Impersonating: Shtrughana Kumar (AHS1624). Original user: Kaushik Kumar Majhi (AHO445). |
| Queue row | `130 Anup Kumar (AHS1121)`, submitted 13-Oct-2026 07:12:25 PM, September, ₹5,012.92, Pending, Action Required L1. |
| Detail | Request #130. 30 days. Sample rows 01-Sep 7.66 km ₹28.73, 02-Sep 60.94 km ₹228.53, 03-Sep 0.00 km ₹0.00. Original claimed ₹5,012.92 / 1,336.77 km. Deductions ₹0.00 / 0.00 km. Net figure shown ₹5,012.92 / 1,336.77 km while the days were still pending. 30 Approve buttons enabled. Forward was hidden. Message: "Please action the 30 pending day row(s) to proceed." |
| Action | Select-all, then Approve Selected. The page opened "Forward to Next Approver?" with "All pending activities have been approved. This request will now be forwarded to the next approval level." Forward was clicked. Days were not written before that confirmation. |
| Result | Page message: "Error processing bulk action: Execution Timeout Expired. The timeout period elapsed prior to completion of the operation or the server is not responding." |
| Header after the error | `AnmolDristiDev` on `MUMANMOAPP01\SQLEXPRESS`. Status Pending. CurrentApproverLevel 1. CurrentApproverLoginId AHS1624. |
| Days after the error | 30 rows, all DetailStatus Pending. |
| Approvers after the error | Level 1 AHS1624, role L1, Pending, no action. Level 2 login `Audit`, role Auditor, Pending, no action. |
| History after the error | HistoryId 845 only. Null to Pending. ChangedBy AHS1121. ChangedOn 2026-10-13 19:12:25.520. Remarks "Initial Submission". |

**L1 result: BLOCKED.** The bulk approval transaction rolled back. Request 130 stayed in the L1 queue for AHS1624 until the later one-day batch below.

### Partial L1 batch 2026-10-13 20:44 +05:30

Same session and page. Select-all was cleared, then only 01-Sep-2026 was checked. Approve Selected was clicked once. The forward confirmation did not open. No SQL write was used.

| Check | Observed |
| --- | --- |
| Banner | Impersonating: Shtrughana Kumar (AHS1624). Original user: Kaushik Kumar Majhi (AHO445). |
| Selection | 1 of 30 checkboxes: 01-Sep-2026, 7.66 km, bike TA ₹28.73. |
| Action | `Approve Selected` (`btnApproveSelected`). Postback still running at about 8 seconds. Completed before the next read, about 25 seconds later. |
| Page result | 01-Sep button changed to "✔ Done". 29 Approve buttons remained. Message: "Please action the 29 pending day row(s) to proceed." Forward stayed hidden. Queue row stayed Pending, Action Required L1. |
| Header | Pending. CurrentApproverLevel 1. CurrentApproverLoginId AHS1624. |
| Days | 1 Approved, 29 Pending. Approved row is detail 3764, 01 Sep 2026, DetailApprovedBy AHS1624, DetailApprovedLevel 1, DetailApprovedOn `2026-10-13T15:14:53.586Z`. |
| Approvers | Unchanged. L1 AHS1624 Pending. Auditor login `Audit` Pending. |
| History | Still only HistoryId 845, initial submission. |
| Totals | Screen and header still show claimed ₹5,012.92 / 1,336.77 km and deductions ₹0.00. Stored `TotalKM` 1336.77, `TotalClaimAmount` 5012.92, `ApprovedKM` 1336.77, `ApprovedClaimAmount` 5012.92, `FinalApprovedAmount` 5012.92. `OriginalClaimAmount` is 0. |

**Auditor discrepancy, for triage. Routing was not changed.** The policy dialog name is the hardcoded session value `AUDITOR_NAME = "Rohan Saraf"` with `AUDITOR_CODE = "Audit"` in `login.aspx.cs` and `SwitchUserHelper.cs`. The tracker joins `SalesApprovalRequestApprover.ApproverLoginId` to `MST_UserMaster`. EmployeeCode `Audit` is Megha Paramanick, so the tracker shows Megha Paramanick. There is no `MST_UserMaster` row named Rohan Saraf. The stored auditor row is the pool login `Audit`, not a person code.

### L1 completion 2026-10-13 21:40 +05:30

Request 130 only. Remaining days were approved one at a time from `ClaimsApproval_v2.aspx` while impersonating AHS1624. The all-30 selection was not repeated. `B_Apply_L2R19_Finalization_UAT.sql` and `D_Rollback_L2R19_Finalization_UAT.sql` were not executed. No claim row was written through SQL. Statement-duration and blocking capture is **BLOCKED**: `E_UAT_Evidence_Queries.sql` and `A_Preflight_ReadOnly_UAT.sql` do not record duration or blocking, and no other approved capture was present. Equivalent read-only SELECTs were used. `E_UAT_Evidence_Queries.sql` was not executed as a script.

A three-day selection earlier the same evening timed out and rolled back. After that, only single non-final days were approved. This section starts from the next verified checkpoint.

| Check | Observed |
| --- | --- |
| Checkpoint before 24-Sep | Header Pending, level 1, current approver AHS1624. 23 Approved, 7 Pending. `FinalApprovedAmount` 5012.92, `TotalClaimAmount` 5012.92, `ApprovedKM` 1336.77, `TotalKM` 1336.77. |
| One-day approvals | 24-Sep through 29-Sep, each `Approve Selected` with one checkbox. Each page result was "Please action the N pending day row(s) to proceed." The forward dialog did not open. After 25-Sep, SQL read 25 Approved / 5 Pending, same header and totals. |
| Stop at one day | Page: 29 Done, 1 Approve, "Please action the 1 pending day row(s) to proceed." Pending row text: 30-Sep-2026. SQL: 29 Approved / 1 Pending. Pending detail 3792, ActivityId 84713, DetailVersion 1, claim ₹195.00, 52.00 km. Max DetailVersion 1. Max ApprovalRequestDetailId 3792. Approvers still both Pending. History still only HistoryId 845. Totals unchanged, including `ApprovedClaimAmount` 5012.92 and `TotalApprovedAmount` 5012.92. |
| SQL identity | `AnmolDristiDev` on `MUMANMOAPP01\SQLEXPRESS`, product `15.0.2000.5`, `@@TRANCOUNT` 0, rechecked before the final click. |
| First final click | Approve Selected for 30-Sep was sent with about 00:41 left on autologout. The browser then showed `login.aspx`. A SELECT immediately afterward was unchanged: 29 Approved / 1 Pending, detail 3792 still Pending, header still AHS1624 level 1, history still only 845. |
| Restored session | Signed in again as AHO445 and switched to AHS1624. Banner: Impersonating Shtrughana Kumar (AHS1624). Original user: Kaushik Kumar Majhi (AHO445). Request 130 reopened with the same 29 Done / 1 Approve state. |
| Final confirmation | Only 30-Sep was checked. Approve Selected once. Dialog "Forward to Next Approver?" opened with `hfPendingBulkApprove` = 1. The day was still pending at that point. Forward was clicked once. |
| Header after Forward | Pending. CurrentApproverLevel 2. CurrentApproverLoginId `Audit`. ModifiedBy AHS1624. ModifiedOn `2026-10-13 16:10:53.371`. Server offset at the later read was `+05:30`, local `2026-10-13 21:46:44`. |
| Final day | Detail 3792 Approved. DetailApprovedLevel 1. DetailApprovedBy AHS1624. DetailApprovedOn `2026-10-13T16:10:52.532Z`. All 30 details remain DetailVersion 1 and are Approved by AHS1624 at level 1. Unresolved detail rows: 0. |
| Approvers | Level 1 AHS1624, role L1, Approved, ActionBy AHS1624, ActionDate `2026-10-13T16:10:52.532Z`. Level 2 login `Audit`, role Auditor, still Pending. |
| History | HistoryId 845 unchanged. New HistoryId 846: Pending to Pending, ChangedBy AHS1624, ChangedOn `2026-10-13T16:10:52.543Z`, remarks "Level 1 completed. Forwarded to Auditor (Audit)." |
| Totals after Forward | `TotalKM` 1336.77, `ApprovedKM` 1336.77, `TotalClaimAmount` 5012.92, `ApprovedClaimAmount` 5012.92, `FinalApprovedAmount` 5012.92, `TotalApprovedAmount` 5012.92. DA, Hotel, and Travel approved amounts are 0. |
| L1 queue | As AHS1624, September 2026 counters were 0 Pending, 0 Returned, 0 Completed, 0 Ready for Payment. Page text: "No approval requests found". Browser clock `9:43:21 PM`. |
| Auditor queue | After returning to AHO445 and switching to Audit, banner: Impersonating Megha Paramanick (Audit). Original user: Kaushik Kumar Majhi (AHO445). September 2026 queue showed 3 Pending, including `130 Anup Kumar (AHS1121)`, 13-Oct-2026 07:12:25 PM, September, ₹5,012.92, Pending, Action Required: Auditor. The row was not opened. Requests 118 and 119 were also listed and were not opened. The page authorization predicate for login `Audit` matches request 130. Session then returned to AHO445. |

**Final-day confirmation: PASS.** The single last day committed and the header moved to Auditor login `Audit` at level 2. This is separate from the earlier one-day approvals. Mark Ready was not used. Auditor to L1 routing was not used. Go-live approval is not implied. The timeout cause for the earlier 30-day and 3-day attempts remains **NOT ESTABLISHED**.

### Auditor day approvals 2026-10-13 22:21 +05:30

Request 130 only. Signed in as AHO445 and switched to login `Audit`. Each day used that row's Approve button. Approve Selected was not used. The 24 remaining days were not selected together. `B_Apply` was not executed. No claim row was written through SQL.

On open, the page said "Please action the 30 pending day row(s) to proceed." All 30 rows were already Approved at level 1, so the Auditor level still treated them as actionable. `btnMarkReadyForPayment` and `btnReturnToL1` were not rendered. `hfIsAuditor` was 1. Banner: Impersonating Megha Paramanick (Audit). Original user: Kaushik Kumar Majhi (AHO445). SQL identity remained `AnmolDristiDev` on `MUMANMOAPP01\SQLEXPRESS`, `@@TRANCOUNT` 0.

| Check | Observed |
| --- | --- |
| First committed day | 01-Sep. Page then showed 29 pending. SQL: 1 day at level 2 by `Audit`, 29 still level 1. Header Pending, level 2, `Audit`. Totals unchanged. |
| Checkpoint after 21-Sep | Page: 9 pending. SQL: 21 days level 2 by `Audit`, 9 still level 1. `FinalApprovedAmount` 5012.92, `ApprovedKM` 1336.77, `TotalKM` 1336.77. |
| Stop at one day | Page: 29 Done, 1 Approve, 30-Sep-2026. SQL: 29 level 2 by `Audit`, 1 still level 1. Remaining detail 3792. Max DetailVersion 1. Header still Pending, level 2, `Audit`. Totals unchanged. |
| Last day | The 30-Sep row Approve button was clicked once. "Forward to Next Approver?" did not open. No timeout text. Page then showed 30 Done and rendered Ready for Payment. |
| Header after the last day | Pending. CurrentApproverLevel 2. CurrentApproverLoginId `Audit`. ModifiedBy `Audit`. ModifiedOn `2026-10-13 16:51:25.081`. |
| Days after the last day | 30 Approved at DetailApprovedLevel 2 by `Audit`. DetailVersion remains 1. Detail 3792 Approved, level 2, DetailApprovedBy `Audit`, DetailApprovedOn `2026-10-13T16:51:22.377Z`. |
| Approvers | L1 AHS1624 remains Approved. Auditor login `Audit` remains Pending, ActionBy null. |
| History | Still HistoryId 845 and 846 only. No new row from these day approvals. |
| Totals | `FinalApprovedAmount` 5012.92, `ApprovedClaimAmount` 5012.92, `TotalApprovedAmount` 5012.92, `ApprovedKM` 1336.77, `TotalKM` 1336.77. DA, Hotel, and Travel approved amounts remain 0. |
| Ready for Payment | Button is now visible. It was not clicked. |
| Return to L1 | Control remains absent. |

**Auditor day approvals: PASS.** This does not close Ready for Payment, and it does not explain the earlier multi-day timeout. Go-live approval is not implied.

### Ready for Payment 2026-10-13 22:25 +05:30

Request 130 only. One UI confirmation. No SQL write. `B_Apply_L2R19_Finalization_UAT.sql` remains closed: `@Execute` 0, `@OwnerApproved` 0, `@OwnerName` empty, `@RequiredBackupFinish` NULL, `@BackupVerifyOnlyPassed` 0, `@AppPublishAuthorizedBy` empty, `@PreimageBodiesSaved` 0.

The page handler `btnMarkReadyForPayment_Click` requires an Auditor session, calls `EnsureCanFinalizeRequest`, then executes the installed procedure once with `@ApprovalRequestId` and `@ActionBy`. The installed module name is `usp_MarkRequestReadyForPayment`, `modify_date` `2026-10-13T14:11:49.980Z`, definition length 11181. It contains errors 59112 and 59109. A Pending request is refused unless `@ActionBy` is the current approver. The assigned Auditor row must be the only Auditor row and must be Pending or Approved. Latest days approved below the current level, and unresolved expenses, raise 59109. On success it approves a Pending Auditor row, writes Pending to Completed, then Completed to ReadyForPayment, in one transaction.

| Check | Observed |
| --- | --- |
| Identity | Banner: Impersonating Megha Paramanick (Audit). Original user: Kaushik Kumar Majhi (AHO445). `btnReturnToL1` was absent. |
| SQL identity | `AnmolDristiDev` on `MUMANMOAPP01\SQLEXPRESS`, product `15.0.2000.5`, `@@TRANCOUNT` 0. |
| Before | Status Pending. Level 2. Current approver `Audit`. L1 AHS1624 Approved. Auditor `Audit` Pending, no action. History 845 and 846 only. 30 details Approved, level 2, by `Audit`, DetailVersion 1, ids 3763–3792. `FinalApprovedAmount` 5012.92, `ApprovedClaimAmount` 5012.92, `TotalApprovedAmount` 5012.92, `ApprovedKM` 1336.77, `TotalKM` 1336.77. Snapshot claim sum 5012.92 and km sum 1336.77. |
| Confirmation | "Mark this request Ready For Payment?" Days Approved 30, Returned 0, Rejected 0, deductions ₹0.00, Final Payable ₹5,012.92 and 1,336.77 km. Confirm was clicked once. |
| Header after | Status `ReadyForPayment`. Level remains 2. Current approver remains `Audit`. ModifiedBy `Audit`. ModifiedOn `2026-10-13 16:55:35.000`. |
| Approvers after | L1 unchanged. Auditor `Audit` Approved, ActionBy `Audit`, ActionDate `2026-10-13T16:55:35.000Z`. |
| History after | HistoryId 847: Pending to Completed, ChangedBy `Audit`, remarks "Auditor finalized all rows before Ready for Payment." HistoryId 848: Completed to ReadyForPayment, ChangedBy `Audit`, remarks "Auditor marked Ready for Payment." Both ChangedOn `2026-10-13T16:55:34.684Z`. |
| Days and totals after | 30 details remain Approved, level 2, by `Audit`, DetailVersion 1. Claim sum 5012.92. Km sum 1336.77. Header amounts unchanged, including DA, Hotel, and Travel at 0. |
| Queues | Pending queue refreshed from 3 Pending to 2 Pending and no longer lists 130. Its Ready counter stayed 0. `ApprovedClaimsView.aspx` for September 2026 lists `130 Anup Kumar (AHS1121)` as ReadyForPayment, ₹5,012.92. The row was not opened and nothing was exported. |

**Ready for Payment: PASS.** The multi-day timeout remains **NOT ESTABLISHED**. The Rohan Saraf / Megha Paramanick display difference was not changed. Backup verification and named SQL deployment approval remain closed. Go-live approval is not implied.

### Multi-day timeout diagnosis 2026-10-13 22:33 +05:30

Request 130 was not reopened. No SQL package was applied. Backup and approval gates, and the auditor display name, were left as accepted for this decision.

Working tree is branch `ai/rc-014b-usp-processapprovalaction` at `39a12b0`. Tag `tada-v1.1-postgolive-fsot-2026` is `2f361f3`. Commit `3779d185001b3182042d5c8d38e5b111675d8dee` is an ancestor of HEAD. The tag was not moved. Unrelated working-tree edits were already present and were not reverted.

Installed `usp_ProcessApprovalAction` on `AnmolDristiDev`: definition length 42023, `modify_date` `2026-10-13T14:10:58.683Z`. The body contains `BEGIN TRANSACTION`, `UPDLOCK` with `HOLDLOCK`, and a call to recalculate the request. The repository procedure has the same structure: header lock, one detail write, full-request recalculation, then `COMMIT`. That commit does not release locks while `ProcessBulkAction` still owns the outer transaction.

`ProcessBulkAction` calls `ProcessDayDetailAction` once per selected row inside one `SqlTransaction`. Each call runs `usp_ProcessApprovalAction` as its own command. Before that command, `GetLatestDetailStatus` opened a second connection and aggregated every detail row for the request. After the first day, recalculation has exclusive locks on the latest detail rows. The second connection's shared read waits on those locks. The command timeout is the 30-second default. The page then rolls the whole batch back. A one-day batch never takes that second read while locks are held. The 3-day failure did not reach the final-day branch. The post-commit recalculation runs only after `tran.Commit`, so it does not explain the rollback.

The status and approved-level reads inside an already open batch now use that connection and transaction. Command timeout was not raised. Recalculation, formulas, routing, and request 130 were not changed.

**Build: PASS** for compilation only. MSBuild 14.0 compiled `AnmolDristi.csproj` Debug to `bin\timeout-diag\AnmolDristi.dll`. The normal `bin` output was locked by the running site, so that site was not replaced. No C# error was reported for this change. There is no automated test project for this page. **Multi-day performance verification: NOT TESTED.** No second multi-day approval was run against UAT. Go-live approval is not implied.

### Bulk-approval runtime deploy 2026-10-13 22:57 +05:30

Working tree remained branch `ai/rc-014b-usp-processapprovalaction` at `39a12b0`. The status-read change was still the uncommitted edit in `ClaimsApproval_v2.aspx.cs`: `GetLatestDetailStatus` and `GetLatestDetailApprovedLevel` take the batch `SqlConnection` and `SqlTransaction`, and `ProcessDayDetailAction` passes that batch connection into both reads. Command timeout was not changed. No stored procedure, formula, or request row was written by this run.

The running site is IIS Express, not the full IIS worker. HTTP.sys registered `HTTP://LOCALHOST:53713/` on request queue `Clr4IntegratedAppPool{DDD44C3C-5BF4-47CB-B456-D887AC68D42D}`, controller `iisexpress.exe`. The solution config `.vs\config\applicationhost.config` binds that URL to physical path `D:\PersonalRnD\AminrupWorks\Anmol_Digitization\AnmolDristi`. Project `OutputPath` for `Debug|AnyCPU` is `bin\`. The older project config that points at `D:\AIL_AminrupTech\AnmolDristi` was not the site that answered. `w3wp.exe` was left running.

| Check | Observed |
| --- | --- |
| Previous site process | IIS Express PID 25484, started 14:58:35. Stopped with `Stop-Process -Id 25484` only. |
| Assembly lock before rebuild | Exclusive open of `bin\AnmolDristi.dll` succeeded after that stop. |
| Build | MSBuild 14.0.23107.0, `AnmolDristi.csproj`, `/t:Build /p:Configuration=Debug /p:Platform=AnyCPU`. Exit 0. Output line: `AnmolDristi -> D:\PersonalRnD\AminrupWorks\Anmol_Digitization\AnmolDristi\bin\AnmolDristi.dll`. |
| Previous bin identity | 2026-10-13 14:02:39, 3140608 bytes, SHA-256 `486394E5124F9871EE4A65F44C43F20AFEA2B4A1152E42EF8E61C7A724E93D1D`. |
| Deployed bin identity | 2026-10-13 22:57:19, 3140608 bytes, SHA-256 `84D088E58ED04879E7F8EB52C7564EF51EE32286DC226ADCAD8EBD2311A74D43`. |
| Loaded assembly | Shadow copy `Temporary ASP.NET Files\root\27ed8435\97fa32a2\assembly\dl3\3239b65e\a4ec8119_385bdd01\AnmolDristi.DLL` has the same SHA-256. The previous shadow copy from 14:58 still has `486394E5...`. |
| IL | `GetLatestDetailStatus(int32, int32, [opt] SqlConnection, [opt] SqlTransaction)`. |
| Restart | IIS Express PID 38516, started 22:58:12, same solution `applicationhost.config`, site `AnmolDristi`. `http://localhost:53713/login.aspx` returned HTTP 200. |
| App connection | `Web.config` `DbConn` Initial Catalog `AnmolDristiDev`. `SELECT DB_NAME(), @@SERVERNAME, @@TRANCOUNT` on that connection returned `AnmolDristiDev`, `MUMANMOAPP01\SQLEXPRESS`, 0. |
| UAT reader | Same three values: `AnmolDristiDev`, `MUMANMOAPP01\SQLEXPRESS`, 0. |
| New claim | Not created. `MAX(ApprovalRequestId)` remained 130. |
| Sign-in | Login form rendered. Session mode is the default in-process `sessionState`. The previous AHO445 session did not survive the IIS Express restart. This run did not have an authorized password and did not bypass login, Switch User, or claim insert. |

**Two-day bulk approval: BLOCKED.** The new assembly and `AnmolDristiDev` target were verified. The approval was not clicked because the effective employee and L1 role could not be signed in. Second-day status-read duration, batch duration, day results, and history were **NOT TESTED**. **Three-day batch: NOT TESTED.** Rollback behavior: **NOT TESTED**. Request 130 was not opened. Production was not written. Go-live approval is not implied.

### AHS1556 two-day attempt 2026-10-14 02:50 +05:30

Local `http://localhost:53713`. No procedure, formula, or Production change. Request 130 was not opened. The loaded site binary remained `bin\AnmolDristi.dll`, SHA-256 `84D088E58ED04879E7F8EB52C7564EF51EE32286DC226ADCAD8EBD2311A74D43`, matching the shadow copy. The app connection and the UAT reader both returned `AnmolDristiDev` on `MUMANMOAPP01\SQLEXPRESS`, `@@TRANCOUNT` 0.

The existing AHS1556 claim is request 112, September 2026, `ReadyForPayment`. That request is on the protected list and was not used. No other AHS1556 request existed. June 2026 had 30 unattached Draft days, so a new claim was submitted through `SalesMonthlyReview`.

| Check | Observed |
| --- | --- |
| Employee switch | Banner: Impersonating Kalim Ahamad Khan (AHS1556). Original user: Kaushik Kumar Majhi (AHO445). Switch User allowlist is `ADMIN,AHO445`. |
| Month submitted | June 2026. 30 Draft days. 1,778.47 km. Travel ₹6,669.28. DA, Hotel, Travel, and Stationary ₹0.00. Submit confirmed once. |
| New claim | Request **131**. Redirect message: created and sent for approval. Status Pending. Level 1. Current approver `AHS1152`, role L1, Pending. Auditor login `Audit`, Pending. |
| Days before approval | 30 details, all Pending, DetailVersion 1. Earliest rows 3793 (01-Jun) and 3794 (02-Jun). |
| Amounts before approval | `TotalClaimAmount` 6669.28, `TotalKM` 1778.47, `FinalApprovedAmount` 6669.28. |
| History before approval | HistoryId 849 only. Null to Pending. ChangedBy AHS1556. ChangedOn `2026-10-14T02:50:31.386Z`. Remarks "Initial Submission". |
| Approver switch | Not completed. The page returned to `login.aspx` before a second Switch User selection. No approval button was clicked. |
| Days after the stop | Still 30 Pending, 0 Approved. Header still Pending, level 1, `AHS1152`. Amounts unchanged. |

**Two-day bulk approval: BLOCKED.** Second-day status-read duration, total batch duration, day results, and transaction outcome were **NOT TESTED**. **Three-day batch: NOT TESTED.** Rollback: **NOT TESTED**. No SQL timeout was observed because the batch was not started. Go-live approval is not implied.

### AHS1556 bulk approval 2026-10-14 01:44 UTC

The login session was restored as AHO445 and switched to the claim’s L1. Banner: Impersonating Shashi Mishra (AHS1152). Original user: Kaushik Kumar Majhi (AHO445). Database rechecked as `AnmolDristiDev` on `MUMANMOAPP01\SQLEXPRESS`, `@@TRANCOUNT` 0. Request 130 was not opened. No procedure, formula, or Production change.

`sys.dm_exec_query_stats` was not readable through the UAT reader, so `GetLatestDetailStatus` has no separate elapsed-time row. The measured interval is `DetailApprovedOn` from one selected day to the next. That interval includes the later status read and that day’s approval procedure. It is not the status read alone. The old failure stopped at the 30-second command timeout and rolled the batch back.

| Check | Observed |
| --- | --- |
| Two-day click | `2026-10-14T01:44:21.641Z`. Exactly 01-Jun (detail 3793) and 02-Jun (detail 3794). Approve Selected once. Forward was not confirmed. `hfPendingBulkApprove` stayed 0. |
| Two-day writes | 3793 Approved, level 1, by AHS1152, `2026-10-14T01:44:27.567Z`. 3794 Approved, level 1, by AHS1152, `2026-10-14T01:44:28.974Z`. Gap 1.407 seconds. Click to second write 7.333 seconds. |
| Two-day page | “Please action the 28 pending day row(s) to proceed.” No timeout text. |
| Two-day outcome | Committed. 2 Approved, 28 Pending. Header Pending, level 1, `AHS1152`. DetailVersion 1. History still only HistoryId 849. Both approver rows still Pending. Amounts unchanged: claim 6669.28, km 1778.47, final 6669.28. `@@TRANCOUNT` 0. |
| Three-day click | `2026-10-14T01:45:42.754Z`. Exactly 03-Jun, 04-Jun, and 05-Jun. Approve Selected once. Forward was not confirmed. |
| Three-day writes | 3795 `01:45:48.357Z`, 3796 `01:45:49.705Z`, 3797 `01:45:50.870Z`. Successive gaps 1.348 and 1.165 seconds. Click to last write 8.116 seconds. |
| Three-day page | “Please action the 25 pending day row(s) to proceed.” No timeout text. |
| Three-day outcome | Committed. 5 Approved, 25 Pending. Header, approver rows, history, version, and amounts unchanged from the two-day read. `@@TRANCOUNT` 0. |

**Two-day bulk approval: PASS.** **Three-day batch: PASS.** Rollback was not induced. **Rollback: NOT TESTED.** No SQL timeout, deadlock, or partial approval was observed. Go-live approval is not implied.

### Auditor assignment read-back 2026-10-14

No new procedure was submitted to UAT in this pass. No claim row was written. Request 131 was not opened and its amounts were not changed. Request 130 was not queried. The two-day and three-day bulk approvals were not repeated. Baselines `3779d185` and `tada-v1.1-postgolive-fsot-2026` were not moved. `Database/Tests/RC014B/04_uat_entry_sequence.md` was not edited. Payment disbursement remains **NOT TESTED**. Bulk-approval rollback remains **NOT TESTED**.

The repository file `Database/Stored Procedures/Approval/usp_MarkRequestReadyForPayment.sql` already contains the Auditor assignment update: when the matching Auditor row is Pending, it sets `Status = Approved`, `ActionBy = @ActionBy`, `ActionDate = @NowUtc`, and `Remarks = ISNULL(@Remarks, Remarks)`, then requires `@@ROWCOUNT = 1` or throws 59110. `AnmolDristiDev` on `MUMANMOAPP01\SQLEXPRESS` was read back as object `usp_MarkRequestReadyForPayment`, `modify_date` `2026-10-13 14:11:49.980`, LEN 11181, DATALENGTH 22362, SHA-256 `E9097D3A83D66D61960F52C7B599F47F595B8943662601C4F4F0A9E6402FB409`. That hash equals the repository text after SQL Server stores `CREATE OR ALTER PROCEDURE` as `CREATE   PROCEDURE`. No other text difference was found. The deployed body contains the same Auditor update and 59110.

`Database/Deployments/MarkReadyAuditorRow/B_Apply_AuditorRowUpdate.sql` still pins the older LEN 2267 / SHA-256 `97ACDA7C...` module and the LEN 2882 expected text. It was not executed. Its header now says that pin is obsolete for the current catalog.

The current `ClaimsApproval_v2.aspx.cs` click opens one connection, checks the auditor and `EnsureCanFinalizeRequest`, then calls the procedure with `@ApprovalRequestId` and `@ActionBy`. It does not call `EnsureHeaderCompletedForPayment` and it does not open a C# transaction. The legacy `ClaimsApproval.aspx.cs` caller passes the same two parameters. `@Remarks` stays optional. That page behavior was not changed.

| Check | Observed |
| --- | --- |
| UAT procedure identity | `AnmolDristiDev`, `MUMANMOAPP01\SQLEXPRESS`. LEN 11181. SHA-256 `E9097D3A83D66D61960F52C7B599F47F595B8943662601C4F4F0A9E6402FB409`. |
| Auditor update in the deployed text | Present. Pending Auditor row, matching login and role, set to Approved with `ActionBy`, `ActionDate`, and remarks. Throw 59110 when the row count is not 1. |
| New UAT ALTER | Not submitted. The catalog already matched the reviewed body. |
| Request 121 header | `ReadyForPayment`. ModifiedBy `Audit`. ModifiedOn `2026-10-12 07:07:16.4216939`. |
| Request 121 amounts | `TotalKM` 2275.15, `TotalClaimAmount` 8531.84, `FinalApprovedAmount` 9032.84, DA 350.00, Hotel 100.00, Travel 51.00, expense 501.00, stationary 0.00, approved km 2275.15, approved claim 8531.84, deduction NULL. |
| Request 121 history | 797 Pending to Completed at `2026-10-12 07:07:16.1926027`. 798 Completed to ReadyForPayment at `2026-10-12 07:07:16.2551270`. Both ChangedBy `Audit`. |
| Request 121 approvers | 237 L1 `AHS1806` remains Approved. 238 Auditor `Audit` level 2 is Approved, ActionBy `Audit`, ActionDate `2026-10-12 07:07:16.2551270`, remarks NULL. It is not Pending. |
| Request 121 recovery | Not executed. The Pending and NULL-action preconditions do not match. `R121_Recover_Approver238.sql` was not run. |
| Request 100 header | `ReadyForPayment`. ModifiedBy `Audit`. ModifiedOn `2026-09-17 13:20:44.4299643`. |
| Request 100 amounts | `TotalKM` 1861.95, `TotalClaimAmount` 6959.15, `FinalApprovedAmount` 9294.15, DA 1775.00, Hotel 450.00, Travel 1350.00, expense 3535.00, stationary 1510.00, approved DA 1550.00, approved hotel 0.00, approved travel 600.00, approved km 1662.44, approved claim 6234.15, deduction NULL. |
| Request 100 history | 701 Pending to Completed at `2026-09-17 13:20:43.8825423`. 702 Completed to ReadyForPayment at `2026-09-17 13:20:44.2112321`. Both ChangedBy `Audit`. |
| Request 100 approvers | 195 L1 `MHS242` remains Approved. 196 Auditor `Audit` level 2 is Approved, ActionBy `Audit`, ActionDate `2026-09-17 13:20:44.2112321`, remarks NULL. |
| Request 100 recovery | Not executed. The row is already in the intended post-state, so the same Pending recovery is not justified. `R100_Recover_Approver196.sql` was not run. History 702 is dated 2026-09-17, which is earlier than the 2026-10-13 procedure body, so this pass does not treat 100 as the same event as 121. |
| Future Mark Ready call | **NOT TESTED.** No procedure call was made. |
| Repeat call on an already final request | **NOT TESTED.** The deployed text throws 59104 when the header is already ReadyForPayment. That path was not invoked. |
| Production procedure | `AnmolDristi` on `MUMANMOAPP01\SQLEXPRESS`, object id 1860917701, `modify_date` `2026-10-08 12:46:41.807`, LEN 2223, DATALENGTH 4446, SHA-256 `6701D1FD52B2BC5652E5EEC561BF7F3E8DE424693C85A111D771CC1F9484D451`. It checks the Auditor role and a Completed header, updates the header, and inserts history. It does not update the Auditor assignment and does not contain 59110. |
| Production scripts | `Production_A_Preflight.sql` executed. `Production_B_Apply.sql` remains `@Execute` 0 and was not run. `Production_C_PostCheck.sql` and `Production_D_Rollback.sql` were not run. No Production claim row was written. |

**UAT procedure read-back: PASS.** The deployed definition contains the reviewed Auditor assignment update. **UAT data recovery: not executed** because both Auditor rows are already Approved. **Production promotion: pending.** **Payment disbursement: NOT TESTED.** Go-live approval is not implied.

### Production preflight 2026-10-14 07:41 +05:30

`Production_A_Preflight.sql` was executed against Production. The script prints its success line only after the database, server, object id, length, `modify_date`, hash, and `59110` guards all pass. No procedure or claim row was changed. `Production_B_Apply.sql` was not enabled.

| Check | Observed |
| --- | --- |
| Message | `PREFLIGHT PASS \| AnmolDristi still has the 2223-character preimage. No procedure was changed.` |
| Completion | `2026-10-14T07:41:55.0653031+05:30` |
| Database guard | Passed. The script throws 58401 on `AnmolDristiDev` and 58402 unless the database is `AnmolDristi`. |
| Server guard | Passed. The script throws 58403 unless the server is `MUMANMOAPP01\SQLEXPRESS`. |
| Object guard | Passed. Object id `1860917701`. |
| Length guard | Passed. LEN 2223 / DATALENGTH 4446. |
| `modify_date` guard | Passed. `2026-10-08 12:46:41.807`. |
| Hash guard | Passed. SHA-256 `6701D1FD52B2BC5652E5EEC561BF7F3E8DE424693C85A111D771CC1F9484D451`. |
| `59110` guard | Passed. The preimage does not contain 59110. |

**Production preflight: PASS.** This is not a Production deployment. Apply, post-check, and rollback remain **NOT TESTED**. Go-live approval is not implied.

### Production apply gate 2026-10-14 07:43 +05:30

`Production_B_Apply.sql` was reviewed and was not executed. `@Execute` remains `0`. The script refuses every database other than `AnmolDristi` on `MUMANMOAPP01\SQLEXPRESS`, requires object id `1860917701`, and requires the preimage SHA-256 `6701D1FD52B2BC5652E5EEC561BF7F3E8DE424693C85A111D771CC1F9484D451` before replacing the procedure with the UAT-verified body SHA-256 `E9097D3A83D66D61960F52C7B599F47F595B8943662601C4F4F0A9E6402FB409`. Those guards were not weakened.

Current deployment authorization was not confirmed. The checklist CAB approver, deployer, and date lines are blank. The change-window box is unchecked. `B_Apply_L2R19_Finalization_UAT.sql` still has `@OwnerApproved = 0` and an empty `@AppPublishAuthorizedBy`. The previously stated 12 October deadline has passed. No later approval or deployment time is recorded. No replacement approval was created.

**Production apply: BLOCKED.** **Production post-check: NOT TESTED.** **Production rollback: NOT TESTED.** Payment disbursement remains **NOT TESTED**. Bulk-approval rollback remains **NOT TESTED**. Go-live approval is not implied.

### Deployment attempt closed 2026-10-14 07:45 +05:30

The preflight remains the read-only result from `2026-10-14T07:41:55.0653031+05:30`. It confirms the pinned preimage. It does not authorize a change. `Production_B_Apply.sql` was rechecked and `@Execute` is still `0`. Apply, post-check, and rollback were not run.

| Gate | Status |
| --- | --- |
| Production preflight | PASS |
| Authorization completeness | FAIL |
| Production apply | BLOCKED |
| Production post-check | NOT TESTED |
| Production rollback | NOT TESTED |
| Go-LIVE | NOT AUTHORIZED / NOT COMPLETE |

Authorization search found the CAB approver, deployer, and date blank at the checklist sign-off lines, the change-window box unchecked, and `B_Apply_L2R19_Finalization_UAT.sql` still at `@OwnerApproved = 0`, `@OwnerName` empty, and `@AppPublishAuthorizedBy` empty. The 12 October deadline has passed. No later approval was found or created.

Resume only after those existing fields record the CAB approver, deployer, deployment date and window, change-window approval, and L2-R19 owner approval. Immediately before promotion, rerun the Production preflight. If the module hash is no longer `6701D1FD52B2BC5652E5EEC561BF7F3E8DE424693C85A111D771CC1F9484D451`, stop. Do not bypass the guard. Already-passed UAT tests are not repeated by this resume step.

Rechecked `2026-10-14 07:47 +05:30`. The checklist deployer, CAB approver, and date lines are still blank. The change-window box is still unchecked. `B_Apply_L2R19_Finalization_UAT.sql` still has `@OwnerApproved = 0`, an empty `@OwnerName`, and an empty `@AppPublishAuthorizedBy`. No approval record with an approver and an actual grant time was found. This recheck does not create one. **Authorization completeness: FAIL.** **Production apply: BLOCKED.** `@Execute` remains `0`.

At `2026-10-14 07:48 +05:30` the session operator wrote: "You proceed with my Authorizations." The same checklist and L2-R19 fields were read again and were still empty. That sentence names no CAB approver, deployer, deployment window, change-window approval, or L2-R19 owner, and it supplies no grant time. It was not copied into those fields. **Authorization completeness: FAIL.** **Production apply: BLOCKED.** `@Execute` remains `0`.

### Approval statement received 2026-10-14 07:51 +05:30

Kaushik Kumar Majhi stated that the approver is Kaushik Kumar Majhi, the deployer is Kaushik Kumar Majhi, and the approval time is `13-10-2026 07:45 AM`. The statement was received at `2026-10-14 07:51 +05:30`. The checklist sign-off lines were still blank at the `07:47` recheck, so the 13 October time is not an artifact from 13 October. It was not written into the CAB date line.

### Approval recorded 2026-10-14 07:53 +05:30

The same name was recorded with today’s timestamp. Deployer: Kaushik Kumar Majhi. CAB approver: Kaushik Kumar Majhi. Recorded time: `2026-10-14 07:53 +05:30`. The `13-10-2026 07:45 AM` claim was not used as the approval time.

### Authorization closed 2026-10-14 07:54 +05:30

The same details were applied to the two open gates. Change window: agreed by Kaushik Kumar Majhi at `2026-10-14 07:54 +05:30` for this procedure promotion. L2-R19 owner approval: Kaushik Kumar Majhi at `2026-10-14 07:54 +05:30`, recorded in this checklist. `B_Apply_L2R19_Finalization_UAT.sql` was left at `@Execute = 0` and `@OwnerApproved = 0` so that separate UAT package cannot run from this record. `Production_B_Apply.sql` remains `@Execute = 0`. No apply or post-check was run. The `07:41` preflight is not a fresh preflight for a later promotion.

| Gate | Status |
| --- | --- |
| Production preflight | PASS at `2026-10-14T07:41:55.0653031+05:30` |
| Authorization completeness | Closed at `2026-10-14 07:54 +05:30` |
| Production apply | NOT TESTED |
| Production post-check | NOT TESTED |
| Production rollback | NOT TESTED |
| Go-LIVE | NOT COMPLETE |

**Authorization record: closed.** **Production apply: NOT TESTED.** Payment disbursement remains **NOT TESTED**. Bulk-approval rollback remains **NOT TESTED**.

### Fresh preflight attempt 2026-10-14 07:56 +05:30

The authorization record closed at `2026-10-14 07:54 +05:30` has no separate end time and was still the recorded window. `Production_A_Preflight.sql` was submitted to the Production reader and refused: "Only SELECT queries are allowed." The script did not print `PREFLIGHT PASS` and did not throw. `@Execute` was left at `0`. Apply and post-check were not run.

A separate read at `2026-10-14T02:26:06.7573577Z` (`07:56:06 +05:30`) returned `AnmolDristi` on `MUMANMOAPP01\SQLEXPRESS`, object id `1860917701`, `modify_date` `2026-10-08 12:46:41.807`, LEN 2223 / DATALENGTH 4446, SHA-256 `6701D1FD52B2BC5652E5EEC561BF7F3E8DE424693C85A111D771CC1F9484D451`, and `59110` absent. That read is not the preflight script.

**Fresh preflight script: BLOCKED.** **Production apply: NOT TESTED.** **Production post-check: NOT TESTED.** **Go-LIVE: NOT COMPLETE.** Payment disbursement remains **NOT TESTED**. Bulk-approval rollback remains **NOT TESTED**.

### Production promotion 2026-10-14 07:58 +05:30

The operator ran the three Production scripts in SSMS. The repository copy of `Production_B_Apply.sql` still has `@Execute = 0`. No claim row was written. The separate UAT L2-R19 script was not run.

| Step | Message | Completion |
| --- | --- | --- |
| `Production_A_Preflight.sql` | `PREFLIGHT PASS \| AnmolDristi still has the 2223-character preimage. No procedure was changed.` | `2026-10-14T07:58:25.2475100+05:30` |
| `Production_B_Apply.sql` | `COMMITTED \| Production procedure matches E9097D3A83D66D61960F52C7B599F47F595B8943662601C4F4F0A9E6402FB409.` | `2026-10-14T07:59:03.0478972+05:30` |
| `Production_C_PostCheck.sql` | `POST-CHECK PASS \| AnmolDristi procedure matches the UAT-verified module. No row was written.` | `2026-10-14T07:59:21.6342414+05:30` |

A later read at `2026-10-14T02:29:54.6223913Z` (`07:59:54 +05:30`) returned `AnmolDristi` on `MUMANMOAPP01\SQLEXPRESS`, object id `1860917701`, `modify_date` `2026-10-14 07:59:02.660`, LEN 11181 / DATALENGTH 22362, SHA-256 `E9097D3A83D66D61960F52C7B599F47F595B8943662601C4F4F0A9E6402FB409`. The deployed text contains `59110` and `ActionDate = @NowUtc`.

**Fresh preflight: PASS.** **Production apply: PASS.** **Production post-check: PASS.** **Production rollback: NOT TESTED.** Payment disbursement remains **NOT TESTED**. Bulk-approval rollback remains **NOT TESTED**. **Go-LIVE: NOT COMPLETE.**

### Go-LIVE reconciliation 2026-10-14 08:03 +05:30

This reconciliation does not change the 07:58 promotion results. It does not mark Go-LIVE complete. No exception was approved for an untested gate. Owner cells in the sign-off table above are blank, and no owner is added here.

The procedure promotion is one object. The sign-off row `RC-014B / RC-014D applied (if needed)` stays blank because `usp_ProcessApprovalAction` was not applied in the 07:58 window. `usp_MarkRequestReadyForPayment` on `AnmolDristi` is the PASS recorded above.

| Existing gate | Evidence in this checklist | Status |
| --- | --- | --- |
| Production backup for this promotion | Backup section unchecked. The 2026-10-08 full backup is a prior record, not a backup taken before the 07:58 apply. | No observed result for this window |
| IIS backup, publish, recycle, browser cache | Sign-off rows blank. | No observed result for this window |
| RC-015A Recalc, Get, and Final backfill | Sign-off rows blank. | No observed result for this window |
| RC-015C Export | Sign-off row blank. | No observed result for this window |
| RC-015B expense Stationary procedure | Sign-off row blank. The v1.1 list calls this mandatory for that package. | No observed result for this window |
| RC-014B `usp_ProcessApprovalAction` | Not part of the 07:58 apply. | No observed result for this window |
| RC-014D `usp_MarkRequestReadyForPayment` | Preflight, apply, post-check, and read-back PASS. | PASS |
| Production smoke | Sign-off row blank. | No observed result for this window |
| Financial proof, Request 97 pattern, on Production | Sign-off row blank. UAT proof for Request 97 remains a UAT record. | No observed result for this window |
| Production approved / rolled back | Sign-off row blank. | No observed result for this window |
| Payment disbursement | No acceptance row and no approved exception. | NOT TESTED |
| Bulk-approval rollback | Recorded on the AHS1556 bulk-approval pass. No approved exception. | NOT TESTED |

Prior GO records stay where they were written: RC-024 on 2026-09-23, and the 2026-10-08 UAT closure. They are not this promotion's Go-LIVE decision. The 2026-10-13 L2-R19 test table was not re-scored.

### Catalog and backup read 2026-10-14 08:07 +05:30

Read-only catalog query on `AnmolDristi` at `2026-10-14T02:37:47.4422581Z`. A module on the server is not a functional PASS. `TA_DA_Smoke_Test_Execution.md` is UAT on 2026-09-15. `RC015G_FSOT_UAT_Execution.md` is UAT on 2026-09-15. No Production smoke or Production financial-proof file was found.

| Object | Production `modify_date` | LEN / SHA-256 | This read |
| --- | --- | --- | --- |
| `usp_MarkRequestReadyForPayment` | `2026-10-14 07:59:02.660` | 11181 / `E9097D3A83D66D61960F52C7B599F47F595B8943662601C4F4F0A9E6402FB409` | Matches the 07:59 promotion |
| `usp_ProcessApprovalAction` | `2026-10-08 12:46:29.240` | 33961 / `594AA8596BEE359D4226C1704E56326D0BA60EA368705279C8A28DE1A9939D01` | Present. Not the 14 October promotion |
| `usp_RecalculateApprovalRequest` | `2026-09-24 10:40:51.487` | 6945 / `B8095136DA3CB725CE691A1A1CBE3A4B71DB46997C32435B89E39A059581DA11` | Text contains `FinalApprovedAmount`. Backfill not observed |
| `usp_GetApprovalRequest` | `2026-09-24 10:40:52.190` | 2769 / `C33A0593A811A68000E9C4EC6D0E8ADAA81892CA6EDC6F0C0BAE224900F8FF11` | Text contains `FinalApprovedAmount`. Behavior not observed |
| `usp_Export_Auditor_Final_Excel` | `2026-10-08 12:47:03.270` | 3231 / `CCE041499D32481C5457AA3231F0360614E88983920E194A65FB32C3E5804686` | Present. Export proof not observed |
| `usp_UpdateSalesDailyExpenseStatus` | `2026-10-08 12:46:10.510` | 7962 / `4653032060A5A32DD0841A39A05993AEF063198404C1CED16E9F314AC1D230FC` | Text contains `Stationary`. Formula proof not observed |

`msdb.dbo.backupset` at `2026-10-14T02:37:47.2531084Z`: newest full backup of `AnmolDristi` finished `2026-10-13T13:39:34.000Z`, size 2,342,628,352, `is_damaged` 0, device `D:\anmoldb\Backups\AnmolDristi_23Sep26.bak`. The same path holds the older 2026-10-08 and 2026-09-23 full backups, so those earlier files are no longer the file at that path. No full backup after the 14 October 07:58 promotion was found. IIS publish, recycle, and cache for this window were not found.

### Backup verification reported 2026-10-14 08:10 +05:30

`RESTORE VERIFYONLY` for `D:\anmoldb\Backups\AnmolDristi_23Sep26.bak` was reported valid at `2026-10-14T08:10:15.4829310+05:30`. This session did not run that command. The file is the 13 October full backup above, which finishes before the 07:58 procedure promotion.

**Production backup verification: PASS** for that reported check. **Production smoke: NOT TESTED.** `TA_DA_Smoke_Test_Execution.md` is the 2026-09-15 UAT run. Its production repeat comes after an IIS recycle, and that recycle is not recorded. The local site uses `AnmolDristiDev`, so it was not used as Production. Every smoke assertion remains **NOT TESTED**. A read at `2026-10-14T02:41:48.2098185Z` on `AnmolDristi` shows request 97 `ReadyForPayment`, `TotalApprovedAmount` 9648.70, `FinalApprovedAmount` NULL. That is not the UAT Request 97 proof of 7736.44.

### Production financial proof 2026-10-14 08:16 +05:30

Read-only on `AnmolDristi` at `2026-10-14T02:46:28.9539131Z`. Request 97 was not used. No recalculation was run and no claim row was written. The selected claim is **1541**, status `ReadyForPayment`, `FinalApprovedAmount` 16318.44. Latest rows are one per activity: maximum `DetailVersion`, then highest `ApprovalRequestDetailId` within that version. Detail ids 46542 through 46571, 30 rows, all `Approved`.

| Comparison | Persisted or calculated | Value | Result |
| --- | --- | --- | --- |
| Header `FinalApprovedAmount` | Persisted | 16318.44 | |
| Latest snapshot DayNet | Calculated: claim amount plus `Snapshot_ExpenseTotal`, excluding Rejected and Returned | 16318.44 | PASS |
| Export total | Calculated with the deployed export predicate: latest Approved row, expense total plus claim amount | 16318.44 | PASS |
| `TotalApprovedAmount` | Persisted | 15748.44 | PASS. It is 570.00 below Final, equal to Stationary |
| Approved `Snapshot_StationaryAmount` | Calculated | 570.00 | PASS. Matches header `TotalStationaryAmount` 570 |
| Status | Persisted | ReadyForPayment | PASS |

No Excel file was generated. The export figure is the deployed procedure’s row and amount expression, evaluated by a read. Payment disbursement remains **NOT TESTED**. Bulk-approval rollback remains **NOT TESTED**. Production smoke remains **NOT TESTED**. **Go-LIVE: NOT COMPLETE.**

### Financial-proof reconciliation 2026-10-14 08:17 +05:30

The 08:16 PASS for request 1541 stands. It was compared with the Financial / FSOT proof checkboxes, `RC015E_Payroll_Proof.md`, `RC015F_FSOT_Deployment_Readiness.md`, and `RC015G_FSOT_UAT_Execution.md`. The query was not repeated. The 08:03 row that says this proof had no observed result is the state at 08:03, before this read.

| Criterion | Rule used | 1541 | Result |
| --- | --- | --- | --- |
| Persisted approved amount | Header `FinalApprovedAmount` | 16318.44 | PASS |
| Latest snapshot sum | Highest `DetailVersion`, then highest `ApprovalRequestDetailId`; claim plus `Snapshot_ExpenseTotal`; not Rejected or Returned | 16318.44, 30 rows, ids 46542–46571 | PASS |
| Export total | Same claim and version ownership; `DetailStatus = Approved`; deployed export amount expression | 16318.44, 30 Approved rows | PASS |
| Rounding | Exact equality to the recorded cent. No tolerance | 15748.44 + 570.00 = 16318.44 | PASS |
| Stationary | Final remains the authority when the computed total omits Stationary | Snapshot Stationary 570.00 equals the header; Final still equals the snapshot sum | PASS |
| Status | Remains ReadyForPayment | ReadyForPayment | PASS |

Request 97 remains excluded: Production `FinalApprovedAmount` is NULL. The UAT amount 7736.44 is not Production evidence. RC-015A, RC-015B, and RC-015C functional checks stay at their earlier status. Production smoke remains **BLOCKED**, and every smoke assertion remains **NOT TESTED**, because the required IIS recycle is not recorded. Payment disbursement remains **NOT TESTED**. Bulk-approval rollback remains **NOT TESTED**. **Go-LIVE: NOT COMPLETE.** The next mandatory blocker is that Production smoke.

### Production smoke prerequisite check 2026-10-14 08:33 +05:30

No IIS recycle was run. No smoke assertion was executed. Request 1541 was not reread.

The smoke sheet `TA_DA_Smoke_Test_Execution.md` is the 2026-09-15 UAT pass on `http://localhost:53713/` against `AnmolDristiDev`. Its Production row is blank and says to repeat the sheet after an IIS recycle. The checklist recycle is the post-publish step for the identified application pool, followed by authenticated page loads. Rollback copies the IIS backup over the live site before recycling. The recorded change window at `2026-10-14 07:54 +05:30` covers only the `usp_MarkRequestReadyForPayment` promotion and does not approve the other package rows.

The 2026-10-08 LIVE identity section names IIS server `MUMANMOAPP01`, site `qaqc`, application pool `qaqc`, and path `D:\anmolapps\qaqcroot`. That section was not rechecked on the server today. This session is on workstation `AILL079KOL`. `appcmd list site` and `appcmd list apppool` failed at `2026-10-14T03:03:08.703Z` because `redirection.config` could not be read. The IIS backup path, application-pool checkbox, publish timestamp, and sign-off cells for IIS backup, publish, recycle, cache, and smoke are still blank.

| Prerequisite | Observed | Result |
| --- | --- | --- |
| Change window covers an IIS recycle | The 07:54 window is the procedure promotion only | BLOCKED |
| IIS backup path recorded | Blank | BLOCKED |
| Application published for this window | Sign-off blank | NOT TESTED |
| Recycle of pool `qaqc` on `MUMANMOAPP01` | Not run. This workstation cannot read the local IIS configuration | NOT TESTED |
| Production smoke assertions | Not run. The sheet’s submission, return, kilometre edit, and resubmission steps have no approved Production test-data plan | NOT TESTED |

**Production smoke: BLOCKED.** A recycle, once authorized and observed, still would not by itself pass the smoke test. Payment disbursement remains **NOT TESTED**. Bulk-approval rollback remains **NOT TESTED**. RC-015A, RC-015B, and RC-015C functional checks remain **NOT TESTED**. **Go-LIVE: NOT COMPLETE.**

### IIS change-window record 2026-10-14 08:43 +05:30

A statement received at `2026-10-14 08:43 +05:30` said the IIS backup is in a different location on the same server. No path, backup time, covered configuration or content, or verification result was given. That statement is not a backup record and is not a verification. The IIS backup path in this checklist stays blank.

No separate Production IIS change window was approved in that statement. The approval at `2026-10-14 07:54 +05:30` remains the `usp_MarkRequestReadyForPayment` promotion only. Approver, approval time, and scope for an IIS backup, publish, application-pool recycle, or Production smoke test are not established. No recycle, publish, claim submission, return, kilometre edit, or resubmission was run.

| Item | Record |
| --- | --- |
| IIS change-window approval | Not granted |
| Approver | Not named for this scope |
| Approval time | None |
| Approved scope | None |
| Backup path | Not supplied |
| Backup time | Not supplied |
| Backup contents | Not supplied |
| Backup verification | NOT TESTED |
| Production smoke | BLOCKED. Every assertion NOT TESTED |

**Go-LIVE: NOT COMPLETE.**

### Production smoke attempt 2026-10-14 08:52 +05:30

An operator statement at `2026-10-14 08:52 +05:30` said an IIS change authorization was granted at that timestamp, that the Production site, pool, and physical path are recorded, that a copy of the live application files was captured, and that a database backup was captured and verified. This session did not independently verify those four statements. No approver name, file-copy path, new backup path, backup time, or backup contents was supplied with the statement. The only backup verification that already has a path remains the reported `RESTORE VERIFYONLY` of `D:\anmoldb\Backups\AnmolDristi_23Sep26.bak` at `2026-10-14T08:10:15.4829310+05:30`. That is a database backup, not an IIS content backup.

No publish was observed. No application-pool recycle was run. No Production page was opened. The local working-tree `Web.config` catalog is `AnmolDristiDev`, so `http://localhost:53713/` was not used. From workstation `AILL079KOL`, `Test-Path` of `\\MUMANMOAPP01\d$\anmolapps\qaqcroot` returned False at `2026-10-14T03:23:44.693Z`. The 2026-10-08 record of site `qaqc`, pool `qaqc`, and path `D:\anmolapps\qaqcroot` was not revalidated on the server.

| Check | Result |
| --- | --- |
| Publish start, completion, artifact, and target path | NOT TESTED. No publish was observed |
| Publish targeted site `qaqc` | NOT TESTED |
| Recycle of pool `qaqc` | NOT TESTED. Not run |
| Production application responds on `AnmolDristi` | NOT TESTED |
| Approved Production test claim, identity, and recovery boundary | Not supplied |

| Smoke assertion | Result |
| --- | --- |
| NEW submission | NOT TESTED |
| Return | NOT TESTED |
| KM edit | NOT TESTED |
| RESUBMIT | NOT TESTED |
| Latest-version | NOT TESTED |
| Return notification | NOT TESTED |
| Forward confirmation | NOT TESTED |
| Auditor routing | NOT TESTED |
| Modal auto-close | NOT TESTED |

**Production smoke: BLOCKED.** The exact blocker is that no publish to `D:\anmolapps\qaqcroot` was observed, so the recycle and the smoke sequence were not started. Payment disbursement remains **NOT TESTED**. Bulk-approval rollback remains **NOT TESTED**. RC-015A, RC-015B, and RC-015C functional checks remain **NOT TESTED**. **Go-LIVE: NOT COMPLETE.**

### Local publish artifact 2026-10-14 08:56 +05:30

Read on workstation `AILL079KOL` at `2026-10-14T03:26:29.468Z`. This folder is a local publish output. It was not copied to Production.

| Item | Observed |
| --- | --- |
| Path | `D:\DeployPackages` |
| Contents | 277 files, 9 directories. Top directories: `bin`, `Controls`, `css`, `ITSM`, `Scripts`, `WebData` |
| `bin\AnmolDristi.dll` | 2,929,664 bytes. Last write `2026-10-14 08:53:31.456`. File version `1.0.0.0`. SHA-256 `7ACE68D13E86406719AD875CD94EC1501549FF1FDB07F1575A6A3F4A5FACAAD4` |
| `PrecompiledApp.config` | `version="2"`, `updatable="true"`, 49 bytes, `2026-10-14 08:53:32` |
| Active `Web.config` connection | Data source `aminruptechnologies.co.in`, catalog `AnmolDristi` |

The previously recorded LIVE binary is `D:\anmolapps\qaqcroot\bin\AnmolDristi.dll`, SHA-256 `15B6B16FDDF70338E9152E35C6B3C5B1C1A3B3043844786ECF2D558AF85B9708`. This local package is a different file. The recorded Production SQL instance is `MUMANMOAPP01\SQLEXPRESS`. The package data source was not shown to be that instance.

The IIS authorization record still has no approver. The `2026-10-14 07:54 +05:30` approval remains the procedure promotion. The `08:52` operator statement names no approver and no IIS file-backup path. The database file `D:\anmoldb\Backups\AnmolDristi_23Sep26.bak` is not an IIS content backup. Rollback still requires a copy of the live site, and that copy path is blank. The share was not opened again in this pass. Its earlier `Test-Path` result of False is not evidence that `D:\anmolapps\qaqcroot` is absent. No server-side copy was run.

No publish, recycle, or smoke assertion was executed. Every smoke assertion remains **NOT TESTED**. **Production publish: BLOCKED.** **Go-LIVE: NOT COMPLETE.**
