# Decision gates

**Additive notice, 2026-11-18.** The Dev-to-LIVE read does not sign any gate. WP-01 and WP-02 already match Dev, so the next decision is to accept no further SQL change, not to put `DR-COLUMNS-A` or `DR-KM-A` into scripts `20` and `30`. Signing those literals in order to re-apply is not authorized by the reconciliation. WP-03 and the workflow procedures stay unsigned and are not recommended for replacement. See document 12 in the reconciliation folder.

**Closure status: DOCUMENTATION UPDATED — NO-CHANGE DEPLOYMENT DECISION PENDING FORMAL ACCEPTANCE.** `DR-NOCHANGE` records that question with choice, name, and date blank. No gate literal in the SQL files is changed by this note.

**Status: BLOCKED.** This file does not choose a side. Signatures belong in `docs/reviews/Post_GoLive/LIVE_TA_DA_Reconciliation_20261117/08_Owner_Decision_Register.md` and in `05_Execution_Log_Template.md`. A signature made before the evidence named on that row is accepted is not a valid decision.

The SQL files in this revision contain the literal `UNSIGNED`, `NOT-FILED`, or a blank login. A later revision may replace a literal only after the matching row below is signed. Replacing a literal is a reviewed source change. It is not something to type into a session at execution time.

## Gates that still block WP-01 and WP-02

| Gate | Question left open | Signed value that lets a later revision proceed | Value that is a decision to stop | Owner |
| --- | --- | --- | --- | --- |
| Three columns | Whether the only history-table change is `ApprovalRequestId int NULL`, `ApproverRole nvarchar(20) NULL`, `ApproverLevel int NULL`, with no default, foreign key, or index | `DR-COLUMNS-A` and `NO-FK-NO-INDEX` in `20_WP01_Schema.sql` and `30_WP02_KM_Procedure.sql` | `DR-COLUMNS-B`, or `DR-COLUMNS-C` plus a written amendment of type, nullability, default, foreign key, and index | IT / DB owner |
| `21975c2` | Whether that commit is the KM body a LIVE replace may use, including `Designation_ID` in the KM gate while `usp_GetResolvedPolicy` still reads `CategoryId` when its designation parameter is empty. The canonical module hash is `F6B07BE8B35F801CFE20053393B6B49E57BA175752FD56578049367E912915B9` | `DR-KM-A` in `30_WP02` | `DR-KM-B` | IT / DB owner and BPO (Sales) |
| Claim transaction | Whether `21975c2` may call the LIVE `usp_RecalculateSalesClaim` whose fingerprint is `FC60A9CFC68BA480454A1D4A4653EAA9426D281267E6750AC3CD96DCC9A33ABC`. The source text was received as conversation evidence. It is not stored in this repository. The temporary-table contract is a source-level MATCH. Owner acceptance of the transaction behaviour is still open | `DR-CLAIM-TX-A` and `FULL-MODULE-TEXT-FILED` in a later reviewed revision of `30_WP02_KM_Procedure.sql`, only after the module text is actually filed and accepted. This revision leaves `NOT-FILED` | `UNSIGNED` or `NOT-FILED`. Receiving the text does not sign this row and does not supply a hash to invent | IT / DB owner and BPO (Finance) |
| Resolver preimages | The LIVE text of `usp_ResolveRegion`, `usp_ResolveBranch`, and `usp_ResolveGrade` | Three 64-character SHA-256 literals, copied from a saved preflight grid, in `30_WP02` | Blank literals. This revision leaves them blank | IT / DB owner |
| Deployment identity | Which login may run the state-changing files | `DR-IDENTITY-A` and the login name in `20`, `30`, `60`, and `61`. The name is not `sa`. Rights are the matrix in `01_Deployment_Manifest.md` | Blank login or `UNSIGNED` | Security owner and DBA |
| Backup and restore | Whether the new full backup, its `RESTORE VERIFYONLY WITH CHECKSUM`, and the isolated restore are accepted | `DR-BACKUP-A`, `VERIFYONLY-CHECKSUM-ACCEPTED`, `ISOLATED-RESTORE-ACCEPTED`, the exact `msdb.dbo.backupset.backup_finish_date`, and a new isolated database name | `UNSIGNED` or a blank finish time | DBA |
| Evidence table | Whether a DBA has created `dbo.TaDaLiveGoLive_20261117_Preimage` with the definition in the manifest, outside this identity | The table exists with that definition before `20` runs. This file does not contain a signed login for that DBA step | The table is absent, or its check allows `WP03` | DBA |
| CategoryId rollback | Whether a saved KM preimage that contains `CategoryId` may be restored. The discovery summary says the current LIVE module references `CategoryId`. The bytes themselves are not in this package | `CATEGORYID-PREIMAGE-APPROVED` in `60_Rollback_Procedures.sql` | `UNSIGNED`. The script then leaves the current KM module in place | IT / DB owner |

`DR-GO` in document 08 is also unsigned. Reconciling this folder did not sign it.

## Parked with WP-03

Owner scope decision A excludes WP-03 from the executable path. The rows below stay unsigned. They are not accepted, and they do not block preparation of WP-01 or WP-02. File `40` throws before any of its historical literals are used.

| Gate | Still unsigned | Why it does not block WP-01 or WP-02 |
| --- | --- | --- |
| C-1 | Choice, name, and date are blank in document 08 | This package does not change `usp_ProcessApprovalAction`, `usp_MarkRequestReadyForPayment`, or `usp_ForwardApprovalRequest` |
| C-3 | Choice, name, and date are blank in document 08 | This package does not replace `dbo.usp_RecalculateApprovalRequest` |
| `c9eba19` | Choice, name, and date are blank in document 08 | Activity counters are not deployed. `ReturnedActivities` is not assigned by this package |

No row above is filled in by this package.
