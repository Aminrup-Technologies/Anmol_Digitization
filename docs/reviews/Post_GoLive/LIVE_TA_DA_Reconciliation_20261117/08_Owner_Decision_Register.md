# Owner decision register

**Status: BLOCKED.** Every decision below is unsigned. This file does not choose a winner. Source behaviour is described in `01`–`03`. Evidence IDs are in `07`.

**Additive scope note, 2026-11-18.** Owner scope decision A excludes WP-03 from the executable package `Database/Deployments/TA_DA_LIVE_GoLive_20261117/`. LIVE `dbo.usp_RecalculateApprovalRequest` is preserved. That instruction does not write a choice, name, or date into DR-C1, DR-C3, or DR-COUNTER. Those fields stay blank. C-1 and C-3 no longer gate WP-01 or WP-02 preparation. They are parked, not accepted.

**Closure record, 2026-11-18.** `DR-NOCHANGE` below records the catalog result from document 12: no functional SQL difference requiring deployment for the tested TA/DA scope. Its choice, name, and date are blank. Formal DBA/IT acceptance is unsigned. This note does not fill any signature field. Feature implementation for that tested scope is closed. DBA/IT governance sign-off stays pending. Runtime financial and payment verification stay untested.

`docs/INDEX.md` §8 is the standing conflict record. C-1 and C-3 are **pending** there. RC-017 documents and scripts listed in `07` do not decide them. `INDEX` still prints `HEAD` as `52c1f32` in the C-1 pair table. Current `HEAD` is `c9eba192a1e504a0c8e4da2869a1b57d29246cf4`. The two workflow files are unchanged since `52c1f32`. That fact does not select a pair.

A signature made before the evidence ID in the same row is accepted is not a valid decision. Blank name, choice, and date mean **unsigned**.

## DR-C1 — C-1 workflow ownership

| | |
| --- | --- |
| Question | Which procedure pair is the Auditor finalization contract that LIVE must match before any later design? |
| Evidence required first | EV-WORKFLOW |
| Owner | Release owner and IT Application Owner |
| Acceptance reviewer | CAB |
| Sources, both still open | Tag `tada-v1.1-postgolive-fsot-2026` (`3779d185`): `usp_ProcessApprovalAction` writes `Completed` on the last Auditor day, and `usp_MarkRequestReadyForPayment` requires `Completed`. Current `HEAD` pair: the first leaves the header `Pending` on the Auditor terminal path, and the second accepts `Pending` or `Completed`. Tag `tada-post-rc019-2026-09-28` (`bc87682f`) is not a candidate: its Mark Ready procedure still requires `Completed` while Process Approval no longer writes it. |
| Allowed choices | **A.** Certified tag pair `3779d185`. **B.** Current `HEAD` pair. **C.** The captured LIVE pair, quoted in full from EV-WORKFLOW, if it matches neither file and the owner accepts that captured pair as the contract to preserve. |
| What does not close it | Choosing C without pasting the EV-WORKFLOW text. Treating the 2026-10-14 checklist hashes as the current contract. |
| This package | Does not change `usp_ProcessApprovalAction`, `usp_MarkRequestReadyForPayment`, or `usp_ForwardApprovalRequest`. Scope decision A also keeps `usp_RecalculateApprovalRequest` unchanged, so the counter candidate does not run inside those procedures as part of this package. |
| Choice | |
| Name | |
| Date | |

## DR-C3 — which body may be `dbo.usp_RecalculateApprovalRequest`

| | |
| --- | --- |
| Question | Which repository file, if either, is allowed to replace the one database object? |
| Evidence required first | EV-RECALC and EV-COUNTER-SCHEMA |
| Owner | IT / DB owner |
| Acceptance reviewer | CAB, with BPO (Finance) for the payable amount |
| Sources, neither authoritative | `Database/Stored Procedures/Approval/usp_RecalculateApprovalRequest.sql` at `c9eba19` (local, not on origin). `Database/Stored Procedures/Utilities/usp_RecalculateApprovalRequest.sql` (last commit `1f1e8f2`). Both write `FinalApprovedAmount`. The Utilities body counts every detail row, starts its own transaction, and uses a different amount formula. `INDEX` C-3 remains pending. |
| Allowed choices | **A.** The Approval file may be compared with LIVE, and only that file may later be proposed. **B.** The Utilities file is the body LIVE must keep; `c9eba19` is rejected. **C.** Neither file may replace LIVE. The captured EV-RECALC text is the body to preserve. |
| What does not close it | Calling either file the LIVE procedure. Signing A or B before EV-RECALC is accepted. |
| Choice | |
| Name | |
| Date | |

## DR-COLUMNS — three nullable KM history columns

| | |
| --- | --- |
| Question | Are these three columns approved as the only history-table change? |
| Evidence required first | EV-KM-SCHEMA |
| Owner | IT / DB owner |
| Acceptance reviewer | CAB |
| Proposed shape, from `B_Apply_ApproverKmEdit_UAT.sql`, not from a LIVE read | `ApprovalRequestId int NULL`, `ApproverRole nvarchar(20) NULL`, `ApproverLevel int NULL`. No default, no foreign key, no new index, no backfill, no drop of existing columns. Existing rows would remain, with the new columns NULL. |
| Allowed choices | **A.** Approve that shape only if EV-KM-SCHEMA shows the three names absent. **B.** Reject the columns. **C.** Amend the shape. The amendment must name type, nullability, default, FK, and index. An unsigned amendment is not a choice. |
| Related, still inside this decision | DR-E in `06` (FK or index) is closed by A only when the owner writes "no FK and no index". Otherwise choose C. |
| Choice | |
| Name | |
| Date | |

## DR-KM — accept `21975c2` as the KM procedure candidate

| | |
| --- | --- |
| Question | Is commit `21975c200d9c9bed1f24a7b97ec802ff22faa2f4` the procedure body a later LIVE design may propose? |
| Evidence required first | EV-KM, EV-CLAIM, EV-POLICY, EV-RESOLVERS, and DR-COLUMNS choice A |
| Owner | IT / DB owner and BPO (Sales) |
| Acceptance reviewer | CAB |
| What the commit contains | `Database/Stored Procedures/Approval/usp_UpdateActivityKM_WithHistory.sql` on origin. It reads `MST_UserMaster.Designation_ID` for its local match gate, inserts the three audit columns, and executes `dbo.usp_RecalculateSalesClaim`. It does not change header status or the current approver. |
| Condition inside this decision | `usp_GetResolvedPolicy` in source reads `CategoryId` when its designation parameter is empty, and the KM procedure calls it that way. The owner either accepts that split, or rejects `21975c2`. This register does not change `usp_GetResolvedPolicy`. |
| Allowed choices | **A.** Accept `21975c2` after the evidence above, including the designation split. **B.** Reject it. |
| What does not close it | `B_Apply` or `C_Apply` hashes. Those scripts target `AnmolDristiDev` and are not a LIVE approval. The 2026-11-18 closeout records the LIVE claim-module text as received. A blank choice, name, and date below still leave this decision unsigned. The designation split and the other evidence rows named above remain open. |
| Choice | |
| Name | |
| Date | |

## DR-COUNTER — accept `c9eba19` as the activity-counter candidate

| | |
| --- | --- |
| Question | Is commit `c9eba192a1e504a0c8e4da2869a1b57d29246cf4` the counter body a later LIVE design may propose? |
| Evidence required first | EV-RECALC, EV-COUNTER-SCHEMA, and DR-C3 choice A |
| Owner | IT / DB owner and BPO (Sales) |
| Acceptance reviewer | CAB |
| What the commit contains | A local, unpushed edit to the Approval file only. It counts the latest detail per `ActivityId` (`DetailVersion` descending, then `ApprovalRequestDetailId` descending). It assigns `TotalActivities`, `PendingActivities`, `ApprovedActivities`, and `RejectedActivities`. A latest `Returned` row increments `TotalActivities` only. `ReturnedActivities` is not assigned. `ReadyForPayment` returns before any write. The approved-amount `SUM` is unchanged versus parent `98947eb` / `755b94e` for that file. |
| Allowed choices | **A.** Accept `c9eba19` only when EV-RECALC's financial section matches that parent, or when the owner writes a separate sentence accepting each financial difference. **B.** Reject `c9eba19`. |
| What does not close it | The untracked file `Database/Deployments/ApprovalRequestActivityCounters/B_Apply_ActivityCounters_UAT.sql`. Its declared hash was not read from LIVE, and the script aborts on `AnmolDristi`. DR-C3 choice B or C. |
| Returned-column question | If the owner wants `ReturnedActivities` updated, that is a new candidate, not `c9eba19`. Record it as choice B and a follow-on change request. |
| Choice | |
| Name | |
| Date | |

## DR-IDENTITY — deployment identity and DBA approval

| | |
| --- | --- |
| Question | Who may later deploy, and has the DBA approved that identity? |
| Evidence required first | EV-LOGIN |
| Owner | Security owner and DBA |
| Acceptance reviewer | CAB |
| Allowed choices | **A.** Named login from EV-LOGIN, with the rights listed in `07`, and an explicit DBA approval of the change window. **B.** Reject the proposed login. |
| What does not close it | A prior audit that used `sa`. A login whose rights are wider than the matrix in `Database/Deployments/TA_DA_LIVE_GoLive_20261117/01_Deployment_Manifest.md`. The earlier sentence that named `CONTROL` on both procedures is not this matrix and is not a grant. |
| Login name | |
| DBA name | |
| Date | |

## DR-BACKUP — backup and isolated restore

| | |
| --- | --- |
| Question | Is the backup that would be the restore source accepted, including an isolated restore? |
| Evidence required first | EV-BACKUP |
| Owner | DBA |
| Acceptance reviewer | CAB |
| Allowed choices | **A.** Accept the new full backup whose `RESTORE VERIFYONLY … WITH CHECKSUM` succeeded, and accept the isolated restore to the new database name recorded in EV-BACKUP. **B.** Reject it. |
| What does not close it | Backup metadata about 21 days old at the earlier audit. The quoted `2026-10-13T13:39:34.000Z` checklist row. A verify that does not name the device. A restore onto `AnmolDristi`. |
| Backup finish time | |
| Isolated database name | |
| Date | |

## DR-GO — permission to write a LIVE design

| | |
| --- | --- |
| Question | May a later session draft a LIVE deployment design? |
| Evidence required first | Every EV row in `07` accepted, and DR-C1, DR-C3, DR-COLUMNS, DR-KM, DR-COUNTER, DR-IDENTITY, and DR-BACKUP signed |
| Owner | CAB |
| Acceptance reviewer | Release owner |
| Allowed choices | **A.** Authorize a design document only. **B.** Refuse. |
| What A does not mean | A does not authorize execution, a script, or a change to LIVE. |
| Choice | |
| Name | |
| Date | |

## DR-NOCHANGE — no further SQL deployment for the tested scope

| | |
| --- | --- |
| Question | Does the IT / DB owner accept that the tested TA/DA SQL scope needs no further deployment? |
| Catalog result, not a signature | Document 12. WP-01 and WP-02 are already present and matched. Other compared procedures differ by formatting only and are not to be normalized. No functional difference requiring deployment was identified. |
| What stays out | WP-03 stays excluded. Scripts `20`, `30`, `40`, `60`, and `61` are NOT TO BE EXECUTED for this outcome. `RESTORE VERIFYONLY` and an isolated restore are NOT AVAILABLE. Runtime financial and payment verification are NOT TESTED. |
| What this row does not close | DR-C1, DR-C3, DR-COLUMNS, DR-KM, DR-COUNTER, DR-IDENTITY, DR-BACKUP, and DR-GO. Their choice, name, and date fields stay blank. |
| Owner | IT / DB owner |
| Acceptance reviewer | CAB |
| Allowed choices | **A.** Accept no further SQL change for this scope. **B.** Reject the no-change result and name the object that must still change. |
| What does not close it | This documentation update. A blank choice, name, or date. |
| Choice | |
| Name | |
| Date | |
