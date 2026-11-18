# Outstanding evidence and DBA approval list

**Status: BLOCKED.** Deployment is not approved. This list is the gate. An item is closed only when the named owner supplies the evidence. This review closes none of them.

**Additive scope note, 2026-11-18.** Owner scope decision A excludes WP-03. E-2, E-6, D-A, D-B, and D-D no longer gate the WP-01/WP-02 package. They stay unsigned. E-8's earlier wording, which allowed alter of `usp_RecalculateApprovalRequest`, conflicts with the manifest matrix. The scripts follow the matrix and refuse that alter. E-8 is not closed. E-3 remains unfiled in the repository: the deployment script literal is still `NOT-FILED`.

## Evidence still required

| ID | Evidence | Class now | Owner | Closes when |
| --- | --- | --- | --- | --- |
| E-1 | Full current definition of `dbo.usp_UpdateActivityKM_WithHistory` on `AnmolDristi`, including `LEN`, `DATALENGTH`, `modify_date`, and a SHA-256 of the stored Unicode module. | NOT VERIFIED. Supplied evidence says the LIVE body references `CategoryId`. | DBA, read-only | The text is saved and compared with `21975c2`. |
| E-2 | Full current definition of `dbo.usp_RecalculateApprovalRequest` on `AnmolDristi`, same fingerprint. | NOT VERIFIED. Supplied evidence says the activity-counter columns are not referenced. | DBA, read-only | The financial section is compared with the Approval parent `98947eb` and with the Utilities file. |
| E-3 | Full current definition of `dbo.usp_RecalculateSalesClaim` on `AnmolDristi`. | NOT VERIFIED. No repository source exists. | DBA, read-only | The body is saved before any KM procedure replace, because the KM procedure executes it. |
| E-4 | Full current definitions of `dbo.usp_GetResolvedPolicy`, `dbo.usp_ResolveDesignation`, `dbo.usp_ProcessApprovalAction`, `dbo.usp_MarkRequestReadyForPayment`, and `dbo.usp_ForwardApprovalRequest`. | NOT VERIFIED. Older checklist rows from 2026-10-14 are quoted records. | DBA, read-only | Each module is saved. They stay out of the change set unless the captured text shows a real dependency break. |
| E-5 | Current columns, types, nullability, defaults, indexes, and foreign keys of `dbo.SalesDailyActivityKMHistory`, plus the row count. | NOT VERIFIED. Supplied evidence says the three audit columns are absent. The repository table script also lacks them. | DBA, read-only | The list confirms absence, or records a conflicting type. |
| E-6 | Current nullability and defaults of the five activity-counter columns on `dbo.SalesApprovalRequest`, including `ReturnedActivities`. | NOT VERIFIED. Repository script has them as `int NOT NULL` default 0. | DBA, read-only | The list is saved. No DDL is proposed for these columns. |
| E-7 | A new full backup of `AnmolDristi` taken for this change, with `is_damaged = 0`, checksums present, and `RESTORE VERIFYONLY … WITH CHECKSUM` recorded. | NOT VERIFIED. Supplied evidence: at the audit date the newest backup metadata was about 21 days old, and no fresh restore proof was supplied. Quoted checklist finish `2026-10-13T13:39:34.000Z` is not that proof. `docs/INDEX.md` §8 still records an older UAT backup gate as blocked; that UAT sentence is a different catalog and is not reused as a LIVE pass. | DBA | The verify output names the backup that would be restored. |
| E-8 | The login that will deploy, its server roles, and its database permissions. | NOT VERIFIED. Supplied evidence: a prior audit used `sa`. | DBA and security owner | The login is not `sa` and can alter only `dbo.usp_UpdateActivityKM_WithHistory`, `dbo.usp_RecalculateApprovalRequest`, and `dbo.SalesDailyActivityKMHistory`. |
| E-9 | Confirmation that ITSM, HRMS import, and VMS on the same instance and `DbConn` are outside this window. | NOT VERIFIED. | Application owner | The window does not publish application files and does not change `Web.config`. |

## Decisions still pending

| ID | Decision | Why it is not inferred | Owner |
| --- | --- | --- | --- |
| D-A | Which file may overwrite the single database object `dbo.usp_RecalculateApprovalRequest`. | `docs/INDEX.md` C-3. The Utilities file counts every detail version, starts a transaction, and uses a different amount formula. The Approval file at `c9eba19` is the counter candidate and is not pushed. | IT / DB owner |
| D-B | Whether the Auditor finalization pair is the certified tag `3779d185` or the `HEAD` pair. | `docs/INDEX.md` C-1. This package does not change that pair. The counter procedure runs inside both `usp_ProcessApprovalAction` and `usp_MarkRequestReadyForPayment`. | Release owner / IT Application Owner |
| D-C | Whether `Designation_ID` in the KM gate is approved while `usp_GetResolvedPolicy` still reads `CategoryId`. | The two reads are both in source. Changing policy resolution needs an explicit approval. This package does not change `usp_GetResolvedPolicy`. | BPO and IT / DB owner |
| D-D | Whether `Returned` latest rows should also update `ReturnedActivities`. | The `c9eba19` candidate counts `Returned` inside `TotalActivities` only. The column exists in the table script and is not assigned. | BPO (Sales) and IT / DB owner |
| D-E | Whether a foreign key or an index on the new history columns is required. | The only schema artifact adds nullable columns and nothing else. | IT / DB owner |
| D-F | Whether the 2026-10-14 production promotion of `usp_MarkRequestReadyForPayment` is accepted. | `docs/INDEX.md` C-5. Out of this change set, and still blocking any claim that LIVE payment code matches the file header. | CAB / IT Application Owner |
| D-G | Authorization to build a LIVE script later. | Every current apply script aborts on `AnmolDristi` or targets a different object. | CAB, after E-1 through E-8 and D-A through D-C |

## What this review already established

| Item | Result |
| --- | --- |
| Branch and `HEAD` | CONFIRMED: `Aug26_AILWorks_AmimnrupSupport`, `c9eba192a1e504a0c8e4da2869a1b57d29246cf4`. |
| Immutable baseline | CONFIRMED: tag `tada-v1.1-postgolive-fsot-2026` = `3779d185001b3182042d5c8d38e5b111675d8dee`, ancestor of `HEAD`. |
| KM source location | CONFIRMED on origin at `21975c2`, and the working tree matches that file. |
| Activity-counter source location | CONFIRMED in `c9eba19`, which is not on origin. The UAT apply script is untracked and forbids LIVE. |
| LIVE alignment of any of the eight procedures | NOT VERIFIED. |
| Deployment performed | None. |

## Approval block

| Role | Name | Decision | Date |
| --- | --- | --- | --- |
| DBA | | | |
| IT / DB owner | | | |
| Release owner | | | |
| CAB | | | |
| BPO (Sales) | | | |
| Security owner for the deployment login | | | |

Blank cells are unsigned. This document does not record a PASS.
