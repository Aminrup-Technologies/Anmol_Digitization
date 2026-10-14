# RC-014B / RC-014D UAT — Request #97

**Date:** 2026-09-15  
**SQL:** parameterized `SELECT` on MCP `anmol_uat` → `AnmolDristiDev` (`execute_query` disabled)  
**App:** local IIS Express `http://localhost:53713`  
**Actor:** `Audit` (Rohan Saraf), impersonated from `AHO445`  
**Claim:** Request **97** (Ajay Kumar Sahu / `AHS1454`, August 2026)  
**Constraint:** no code change, no SQL deploy, no Git during this pass

**Overall verdict: RC-014B PASS · RC-014D FAIL**

The Auditor completion predicate held: header stayed `Pending` through the first, second, and bulk-except-last day, then moved to `Completed` only on the last remaining gatekeeper day, with one `Pending → Completed` history row (`Final Auditor Approval`). Ready for Payment appeared.

Confirming Ready for Payment did **not** persist `ReadyForPayment`. The running IIS Express process threw a closed-connection `ExecuteNonQuery` error before the live `usp_MarkRequestReadyForPayment` body ran. Header remains `Completed`. There is still no `Completed → ReadyForPayment` history row.

---

## Session

| Item | Evidence |
| --- | --- |
| Site | `http://localhost:53713/ClaimsApproval_v2.aspx` |
| Identity | Banner: Impersonating Rohan Saraf (`Audit`) \| Original: Kaushik Kumar Majhi (`AHO445`) |
| Filter | Year 2026 / Month August |
| Live `usp_ProcessApprovalAction` | `modify_date` **2026-09-15 18:04:06** (RC-014B body present) |
| Live `usp_MarkRequestReadyForPayment` | `modify_date` **2026-01-09 17:48:18** (RC-014D named INSERT **not** deployed) |
| Running `AnmolDristi.dll` | **2026-09-15 17:13:50** |
| Repo `ClaimsApproval_v2.aspx.cs` | **2026-09-15 18:42:10** (`con.Open()` restored in source; not in the running DLL) |

Queue after this pass: **1 Pending** — Request **94** only. Request 97 left the Pending-only list after `Completed`. The Completed / Ready for Payment tiles stayed at 0 because this page filters Pending.

![Pending queue after request 97 completed](screenshots/RC014B_06_queue_after_completed.png)

---

## Before (baseline)

### UI

Request 97 modal: yellow rows with Approve enabled. Footer: **Please action the 29 pending day row(s) to proceed.** Ready for Payment hidden. Queue behind the modal still showed **97** and **94**.

![Request 97 before first Auditor approve](screenshots/RC014B_01_req97_open.png)

### SQL

**`SalesApprovalRequest`**

| ApprovalRequestId | Status | CurrentApproverLevel | CurrentApproverLoginId | ModifiedBy | ModifiedOn |
| --- | --- | --- | --- | --- | --- |
| 97 | Pending | 2 | Audit | Audit | 2026-09-15 12:34:51 |

**`SalesApprovalRequestApprover`**

| Level | Login | Role | Status | ActionBy |
| --- | --- | --- | --- | --- |
| 1 | AHS1605 | L1 | Approved | AHS1605 |
| 2 | Audit | Auditor | Pending | (null) |

Latest details: **31/31** `Approved`. **29** at `DetailApprovedLevel=1`, **2** already at level 2. Unresolved-at-Auditor count = **29** (matches the UI footer).

History through **684**: last row `Pending → Pending`, Remarks `Level 1 completed. Forwarded to Auditor (Audit).` No `Completed` row.

---

## After first approve (01-Aug)

### Action

Clicked **Approve** on **01-Aug-2026**.

### UI after

| Check | Expected | Observed | Result |
| --- | --- | --- | --- |
| Row becomes Done | 01-Aug ✔ Done, green | Yes | PASS |
| Remaining counter decreases | 29 → 28 | Yes (“28 pending day row(s)”) | PASS |
| Header remains Pending | Pending | SQL `Status=Pending`, routing still L2/`Audit` | PASS |
| Ready for Payment hidden | Hidden | Hidden | PASS |
| No Forward confirm | None | None | PASS |
| No Completed history | None | No rows after HistoryId 684 | PASS |

![01-Aug Done; 28 days still pending; Ready hidden](screenshots/RC014B_02_after_01aug_approve.png)

### SQL

Header: `Pending`, level 2, `Audit`. Auditor row still `Pending`. Latest: **28** at level 1, **3** at level 2. History: no new rows.

This is the RC-014A failure mode inverted: the first Auditor day no longer completes the claim.

---

## Intermediate (02-Aug, then bulk except last)

### 02-Aug

Clicked **Approve** on **02-Aug-2026**. Footer: **27 pending day row(s).** Ready still hidden.

![02-Aug also Done; header still Pending](screenshots/RC014B_03_after_02aug_approve.png)

SQL: `Status=Pending`, Auditor `Pending`, **4** latest rows at level 2.

### Bulk except 31-Aug

Approve Selected on the remaining days except **31-Aug**. Footer: **Please action the 1 pending day row(s) to proceed.** Ready still hidden. Approve Selected still visible.

![One day left; Ready still hidden](screenshots/RC014B_04_one_day_left.png)

SQL: `Status=Pending`, Auditor `Pending`, **30** at level 2, **1** still below level 2.

| Check | Expected | Observed | Result |
| --- | --- | --- | --- |
| Header stays Pending until last day | Pending | Pending through 30/31 | PASS |
| Ready hidden while any gatekeeper day remains | Hidden | Hidden | PASS |
| No `Final Auditor Approval` history yet | None | None | PASS |

---

## Final day (31-Aug) — RC-014B terminal

### Action

Clicked **Approve** on **31-Aug-2026**.

### UI after

All 31 rows ✔ Done. Footer cleared. **Ready for Payment** visible. No Forward confirmation (Auditor is final).

![All days Done; Ready for Payment visible](screenshots/RC014B_05_ready_button.png)

### SQL after last approve

**`SalesApprovalRequest`**

| Column | Before last day | After last day |
| --- | --- | --- |
| Status | Pending | **Completed** |
| CurrentApproverLevel | 2 | **NULL** |
| CurrentApproverLoginId | Audit | **NULL** |
| ModifiedBy | Audit | Audit |
| ModifiedOn | 2026-09-15 15:47:58 (first approve) | **2026-09-15 15:55:02** |

**Auditor row:** Pending → **Approved** (`ActionBy=Audit`).

Latest details: **31/31** `Approved`, `DetailApprovedLevel=2`.

**History 687:** `Pending → Completed`, ChangedBy=`Audit`, Remarks=`Final Auditor Approval`. Only new history row after 684.

Queue (Pending filter) dropped #97. Remaining Pending: **94**.

| Check | Expected | Observed | Result |
| --- | --- | --- | --- |
| Header Completed only after last day | Yes | Yes (15:55:02) | PASS |
| Ready for Payment appears | Yes | Yes | PASS |
| No Forward confirmation | None | None | PASS |
| Single Completed history | One `Final Auditor Approval` | HistoryId 687 only | PASS |

---

## Ready for Payment — RC-014D

### Action

Clicked **Ready for Payment**. Confirm dialog: “Mark this request Ready For Payment?” / “This completes the approval workflow.” Clicked **Confirm**.

![Ready for Payment confirm dialog](screenshots/RC014B_07_ready_confirm.png)

The confirm summary showed **Days Approved = 0** (and Returned/Rejected = 0) while all 31 rows were already Done. Out of scope for RC-014B/D; noted only.

### UI after Confirm

Footer error (red):

> Error: ExecuteNonQuery requires an open and available Connection. The connection's current state is closed.

Ready for Payment remained visible. Amounts unchanged (Original ₹ 8,017.95 / Net ₹ 7,736.44). No success text.

![Ready for Payment closed-connection error](screenshots/RC014D_01_ready_connection_closed.png)

### SQL after Confirm

Unchanged from the Completed snapshot:

| Check | Expected | Observed | Result |
| --- | --- | --- | --- |
| Header = ReadyForPayment | ReadyForPayment | Still **Completed** | **FAIL** |
| Routing stays NULL | NULL | NULL | n/a (no transition) |
| New history `Completed → ReadyForPayment` | Yes | **None**. Last row remains 687 | **FAIL** |
| No SQL error 213 | No 213 | 213 **not reached**. Client closed-connection error first | **FAIL** (different defect) |
| ModifiedOn advances | Yes | Still **2026-09-15 15:55:02** | **FAIL** |

Live `usp_MarkRequestReadyForPayment` is still the 2026-01-09 positional INSERT. RC-014C proved that body dies with error 213 (`Column name or number of supplied values does not match table definition`) **if** it is invoked. This click never got that far: the running DLL still hits the closed-connection path in `btnMarkReadyForPayment_Click`.

Repo source at 18:42 restored `con.Open()` before `ExecuteNonQuery`. The IIS Express DLL timestamp is 17:13, so this UAT executed the **pre-RC-014D** C# handler. Per the no-deploy rule, the process was not rebuilt.

---

## Verdict matrix

| Phase | Check | Result |
| --- | --- | --- |
| Before | 97 Pending at Auditor, 29 gatekeeper days, history ends at L1 Forward | PASS |
| First approve (01-Aug) | Header stays Pending; Ready hidden; no Completed history | **PASS** (RC-014B) |
| Intermediate (02-Aug + bulk) | Header stays Pending with 1 day left | **PASS** (RC-014B) |
| Last day (31-Aug) | Header Completed; routing NULL; Auditor Approved; History 687; Ready visible | **PASS** (RC-014B) |
| Queue | 97 leaves Pending list; 94 remains | PASS |
| Ready Confirm | Header ReadyForPayment; history Completed→ReadyForPayment; no 213 | **FAIL** (RC-014D) |

**RC-014B is live-proven on Request 97.**  
**RC-014D is not live-proven:** running C# still closed-connection; live SP still undeployed named INSERT. Header is parked at `Completed`, which is the correct input state for a later MarkReady retry after rebuild + SP deploy.

No `usp_ProcessApprovalAction`, Forward, or authorization files were modified in this pass.

---

## Addendum 2026-10-14 — Auditor assignment read-back

This addendum does not change the 2026-09-15 Request 97 verdict above. `04_uat_entry_sequence.md` was not edited. No procedure was submitted and no claim row was written in this pass.

Read-back of `dbo.usp_MarkRequestReadyForPayment` on `AnmolDristiDev`, `MUMANMOAPP01\SQLEXPRESS`: `modify_date` `2026-10-13 14:11:49.980`, LEN 11181, SHA-256 `E9097D3A83D66D61960F52C7B599F47F595B8943662601C4F4F0A9E6402FB409`. That matches the repository procedure after `CREATE OR ALTER` is stored as `CREATE` plus three spaces. The deployed text sets a matching Pending Auditor row to Approved and throws 59110 when that row count is not 1.

| Record | Read-only result | Recovery |
| --- | --- | --- |
| Request 121, history 798 | Completed to ReadyForPayment, `Audit`, `2026-10-12 07:07:16.2551270` | Not executed |
| Request 121, approver 238 | Auditor `Audit`, level 2, Approved, ActionBy `Audit`, ActionDate `2026-10-12 07:07:16.2551270` | Pending and NULL-action preconditions do not match |
| Request 121 amounts | `TotalKM` 2275.15, `TotalClaimAmount` 8531.84, `FinalApprovedAmount` 9032.84 | Unchanged; no write |
| Request 100, history 702 | Completed to ReadyForPayment, `Audit`, `2026-09-17 13:20:44.2112321` | Not the same dated event as 121 |
| Request 100, approver 196 | Auditor `Audit`, level 2, Approved, ActionBy `Audit`, ActionDate `2026-09-17 13:20:44.2112321` | Not executed; already in the post-state |
| Request 100 amounts | `TotalKM` 1861.95, `TotalClaimAmount` 6959.15, `FinalApprovedAmount` 9294.15 | Unchanged; no write |

**UAT procedure read-back: PASS.** **Request 121 recovery: not executed.** **Request 100 recovery: not executed.** A new Mark Ready call and a repeat call were **NOT TESTED**. Payment disbursement remains **NOT TESTED**.

Production `AnmolDristi` still has LEN 2223 / SHA-256 `6701D1FD52B2BC5652E5EEC561BF7F3E8DE424693C85A111D771CC1F9484D451` and does not contain the Auditor assignment update. `Production_B_Apply.sql` remains `@Execute = 0`. `Production_C_PostCheck.sql` and `Production_D_Rollback.sql` were not executed. **Production promotion: pending.**

### Production preflight 2026-10-14 07:41 +05:30

`Production_A_Preflight.sql` completed with:

`PREFLIGHT PASS | AnmolDristi still has the 2223-character preimage. No procedure was changed.`

Completion time: `2026-10-14T07:41:55.0653031+05:30`.

That message is printed only after the `AnmolDristi`, `MUMANMOAPP01\SQLEXPRESS`, object id `1860917701`, LEN 2223 / DATALENGTH 4446, `modify_date` `2026-10-08 12:46:41.807`, SHA-256 `6701D1FD52B2BC5652E5EEC561BF7F3E8DE424693C85A111D771CC1F9484D451`, and absent-`59110` guards pass. **Production preflight: PASS.** No procedure was changed. Apply remains **NOT TESTED**. This is not Go-LIVE.

### Production apply gate 2026-10-14 07:43 +05:30

`Production_B_Apply.sql` was inspected. `@Execute` remains `0`. It was not executed. Current deployment authorization was not confirmed: the checklist CAB approver, deployer, and date are blank, the change window is unchecked, and the L2-R19 owner-approval and app-publish fields are still empty. The 12 October deadline has passed, and no later approval is recorded.

**Production apply: BLOCKED.** **Production post-check: NOT TESTED.** **Production rollback: NOT TESTED.** Payment disbursement remains **NOT TESTED**. Bulk-approval rollback remains **NOT TESTED**.

### Deployment attempt closed 2026-10-14 07:45 +05:30

`Production_B_Apply.sql` was rechecked. `@Execute` is still `0`. No apply, post-check, or rollback was run. The preflight PASS at `2026-10-14T07:41:55.0653031+05:30` remains the read-only preimage confirmation.

| Gate | Status |
| --- | --- |
| Production preflight | PASS |
| Authorization completeness | FAIL |
| Production apply | BLOCKED |
| Production post-check | NOT TESTED |
| Production rollback | NOT TESTED |
| Go-LIVE | NOT AUTHORIZED / NOT COMPLETE |

The authorization search found blank checklist lines for deployer, CAB approver, and date, an unchecked change window, and L2-R19 `@OwnerApproved = 0` with empty `@OwnerName` and `@AppPublishAuthorizedBy`. Resume only after those existing governance fields are explicitly recorded. The next technical step is a fresh Production preflight. If the hash is no longer `6701D1FD52B2BC5652E5EEC561BF7F3E8DE424693C85A111D771CC1F9484D451`, stop.

Rechecked `2026-10-14 07:47 +05:30`. Those same fields are still empty. No approver and no actual grant time were found, so none was written. **Authorization completeness: FAIL.** **Production apply: BLOCKED.** `@Execute` remains `0`.

At `2026-10-14 07:48 +05:30` the session operator wrote: "You proceed with my Authorizations." The checklist deployer, CAB approver, date, and change window, and the L2-R19 owner fields, were still empty. That sentence was not entered as an approval. **Authorization completeness: FAIL.** **Production apply: BLOCKED.** `@Execute` remains `0`.

### Approval statement received 2026-10-14 07:51 +05:30

Kaushik Kumar Majhi stated approver and deployer Kaushik Kumar Majhi, and stated approval at `13-10-2026 07:45 AM`. That statement was received at `2026-10-14 07:51 +05:30`. The 13 October time was not in the checklist at the `07:47` recheck, so it was not backdated into the CAB date line.

### Approval recorded 2026-10-14 07:53 +05:30

Deployer and CAB approver recorded as Kaushik Kumar Majhi at `2026-10-14 07:53 +05:30`. The `13-10-2026 07:45 AM` time was not used.

### Authorization closed 2026-10-14 07:54 +05:30

Change window agreed by Kaushik Kumar Majhi at `2026-10-14 07:54 +05:30` for this procedure promotion. L2-R19 owner approval recorded as Kaushik Kumar Majhi at the same time. The UAT `B_Apply_L2R19_Finalization_UAT.sql` flags were left closed. `Production_B_Apply.sql` remains `@Execute = 0`. Apply and post-check were not run.

**Authorization record: closed.** **Production apply: NOT TESTED.** **Production post-check: NOT TESTED.** **Go-LIVE: NOT COMPLETE.**

### Fresh preflight attempt 2026-10-14 07:56 +05:30

`Production_A_Preflight.sql` was refused by the Production reader: "Only SELECT queries are allowed." No `PREFLIGHT PASS` message was returned. A separate read at `2026-10-14T02:26:06.7573577Z` showed `AnmolDristi` on `MUMANMOAPP01\SQLEXPRESS`, SHA-256 `6701D1FD52B2BC5652E5EEC561BF7F3E8DE424693C85A111D771CC1F9484D451`, LEN 2223 / 4446, `modify_date` `2026-10-08 12:46:41.807`, `59110` absent. `Production_B_Apply.sql` remains `@Execute = 0`.

**Fresh preflight script: BLOCKED.** **Production apply: NOT TESTED.** **Production post-check: NOT TESTED.** **Go-LIVE: NOT COMPLETE.**

### Production promotion 2026-10-14 07:58 +05:30

Operator SSMS results:

- Preflight `2026-10-14T07:58:25.2475100+05:30`: `PREFLIGHT PASS | AnmolDristi still has the 2223-character preimage. No procedure was changed.`
- Apply `2026-10-14T07:59:03.0478972+05:30`: `COMMITTED | Production procedure matches E9097D3A83D66D61960F52C7B599F47F595B8943662601C4F4F0A9E6402FB409.`
- Post-check `2026-10-14T07:59:21.6342414+05:30`: `POST-CHECK PASS | AnmolDristi procedure matches the UAT-verified module. No row was written.`

Read-back at `2026-10-14T02:29:54.6223913Z`: `AnmolDristi` on `MUMANMOAPP01\SQLEXPRESS`, object id `1860917701`, `modify_date` `2026-10-14 07:59:02.660`, LEN 11181 / DATALENGTH 22362, SHA-256 `E9097D3A83D66D61960F52C7B599F47F595B8943662601C4F4F0A9E6402FB409`, with `59110` and `ActionDate = @NowUtc` present. The file `Production_B_Apply.sql` still has `@Execute = 0`. No claim row was written.

**Fresh preflight: PASS.** **Production apply: PASS.** **Production post-check: PASS.** **Production rollback: NOT TESTED.** Payment disbursement remains **NOT TESTED**. Bulk-approval rollback remains **NOT TESTED**. **Go-LIVE: NOT COMPLETE.**

### Go-LIVE reconciliation 2026-10-14 08:03 +05:30

The 07:58 procedure promotion remains PASS. Go-LIVE is not closed. The checklist sign-off table has blank owner, time, and result cells. No owner or exception was added.

`usp_MarkRequestReadyForPayment` on `AnmolDristi` is the verified object. `usp_ProcessApprovalAction`, RC-015A, RC-015B, RC-015C, Production backup for this window, IIS publish, smoke, and Production financial proof have no observed result in that sign-off. Payment disbursement and bulk-approval rollback remain **NOT TESTED**, with no approved exception. The 2026-09-23 RC-024 GO and the 2026-10-08 UAT GO are earlier records and are not this decision.

Catalog read `2026-10-14T02:37:47.4422581Z` on `AnmolDristi`: ProcessApprovalAction `2026-10-08 12:46:29.240` LEN 33961 SHA-256 `594AA8596BEE359D4226C1704E56326D0BA60EA368705279C8A28DE1A9939D01`; Recalculate `2026-09-24 10:40:51.487`; Get `2026-09-24 10:40:52.190`; Export `2026-10-08 12:47:03.270`; expense-status procedure `2026-10-08 12:46:10.510` and its text contains `Stationary`. These are catalog facts, not functional PASS results. Newest full backup finished `2026-10-13T13:39:34.000Z` at `D:\anmoldb\Backups\AnmolDristi_23Sep26.bak`, size 2,342,628,352, `is_damaged` 0. No backup after the 07:58 promotion was found. Smoke evidence `TA_DA_Smoke_Test_Execution.md` and financial evidence `RC015G_FSOT_UAT_Execution.md` are UAT, dated 2026-09-15.

`RESTORE VERIFYONLY` of that backup file was reported valid at `2026-10-14T08:10:15.4829310+05:30`. Production smoke was not run. Production request 97, read `2026-10-14T02:41:48.2098185Z`, is `ReadyForPayment` with `TotalApprovedAmount` 9648.70 and `FinalApprovedAmount` NULL. That read is not the UAT financial proof.

Production financial proof, read-only at `2026-10-14T02:46:28.9539131Z`, used request **1541**. `FinalApprovedAmount` 16318.44 equals the latest snapshot DayNet and the deployed export expression. `TotalApprovedAmount` 15748.44 is 570.00 lower, matching Stationary 570.00. Status stayed `ReadyForPayment`. No Excel file was produced and no claim was written. **Production financial proof: PASS** for request 1541. **Go-LIVE: NOT COMPLETE.**

Reconciled at `2026-10-14 08:17 +05:30` against the checklist Financial / FSOT proof and RC-015E, RC-015F, and RC-015G. The PASS stands. The three required figures are the persisted Final 16318.44, the latest snapshot sum 16318.44, and the deployed export expression 16318.44. The difference from `TotalApprovedAmount` is exact: 570.00. Request 97 stays excluded. The next mandatory blocker is Production smoke, which remains **BLOCKED**.

Production smoke was not run. At `2026-10-14T03:03:08.703Z` this workstation was `AILL079KOL`, and `appcmd` could not read `redirection.config`. The checklist’s 2026-10-08 LIVE identity names site `qaqc`, pool `qaqc`, and path `D:\anmolapps\qaqcroot` on `MUMANMOAPP01`; that identity was not rechecked today. The 07:54 change window does not authorize an IIS recycle, and the IIS backup path is blank. `TA_DA_Smoke_Test_Execution.md` remains the UAT sheet for `http://localhost:53713/` and `AnmolDristiDev`. Every Production smoke assertion stays **NOT TESTED**. **Go-LIVE: NOT COMPLETE.**

At `2026-10-14 08:43 +05:30` a statement said the IIS backup is in a different location on the same server. No path, time, contents, or verification result was supplied, so the backup path stays blank and verification stays **NOT TESTED**. No separate IIS change window was approved. The 07:54 approval stays limited to the procedure promotion. **Production smoke: BLOCKED.** **Go-LIVE: NOT COMPLETE.**

At `2026-10-14 08:52 +05:30` the operator stated that an IIS change authorization was granted at that timestamp, that a live-file copy was captured, and that a database backup was captured and verified. No approver name, file-copy path, or new backup path was supplied, and this session did not verify them. `Test-Path` of `\\MUMANMOAPP01\d$\anmolapps\qaqcroot` from `AILL079KOL` returned False at `2026-10-14T03:23:44.693Z`. No publish, recycle, or smoke assertion was run. **Production smoke: BLOCKED.** **Go-LIVE: NOT COMPLETE.**

Local artifact read at `2026-10-14T03:26:29.468Z`: `D:\DeployPackages` has 277 files. `bin\AnmolDristi.dll` is 2,929,664 bytes, written `2026-10-14 08:53:31.456`, version `1.0.0.0`, SHA-256 `7ACE68D13E86406719AD875CD94EC1501549FF1FDB07F1575A6A3F4A5FACAAD4`. Its active connection catalog is `AnmolDristi` on data source `aminruptechnologies.co.in`. That folder was not copied to `MUMANMOAPP01`. The IIS approver and IIS file-backup path are still absent, so publish stayed stopped. **Production publish: BLOCKED.** **Go-LIVE: NOT COMPLETE.**
