# Stop / go matrix

**Additive notice, 2026-11-18 catalog read.** Document 12 records that WP-01 and WP-02 already match Dev. That is not a GO. No row below is signed. Do not treat a STOP as an instruction to run scripts `20` or `30`, and do not treat a current-catalog mismatch against the 2026-11-17 snapshot as an instruction to roll back.

**Additive notice, WP-03 stored values.** Document 12 records that the four-counter formula is already on both catalogs and that the stored values do not match it. The proposed correction is counter-only, excludes `ReadyForPayment`, leaves `ReturnedActivities` unchanged, and has not been executed. That finding does not turn DR-COUNTER or WP-03 to GO. File `40` stays excluded. Status: `WP-03 DESIGN PREPARED — CORRECTION NOT EXECUTED — FEATURE CLOSURE PENDING`.

**Additive notice, 2026-11-17 discovery.** `10_LIVE_Discovery_Capture.md` records a read-only capture of current LIVE fingerprints. Captured evidence is not accepted evidence. Every row below remains STOP until the acceptance reviewer named in `07` signs it and the decisions in `08` are signed.

**Package status: BLOCKED.**

Go requires every row in this matrix to read GO. One STOP keeps the package BLOCKED. Unsigned evidence and unsigned decisions are STOP. No row is GO on 2026-11-17.

The evidence rule in `07` applies to every evidence row: a script-declared hash, a source file, old backup metadata, or a historical checklist is not a current LIVE preimage and is not an approval.

`dbo.usp_RecalculateSalesClaim` is row EV-CLAIM. The KM candidate calls it. There is no repository body. The 2026-11-18 closeout in document 10 records that the complete LIVE text was supplied as conversation evidence, with the same SHA-256, 28786 characters, and 57572 bytes. The source-capture prerequisite is satisfied. Acceptance of that text is unsigned, so EV-CLAIM stays STOP and the package stays BLOCKED. The deployment script keeps `NOT-FILED` because the body is not in the repository.

**Additive scope note, 2026-11-18.** Owner scope decision A excludes WP-03. Rows marked PARKED below are out of the executable path. They are not GO and they are not signed. They do not gate WP-01 or WP-02. For those rows only, this note supersedes the 2026-11-17 sentence that every row remains STOP and the sentence that every row must read GO. Every STOP row still blocks the package. The recalc and workflow fingerprints remain must-not-change checks.

| ID | Required item | Closes | Now | Becomes GO only when |
| --- | --- | --- | --- | --- |
| EV-KM | LIVE `usp_UpdateActivityKM_WithHistory` | KM preimage | STOP | Full module captured on `AnmolDristi` and accepted by IT / DB owner |
| EV-RECALC | LIVE `usp_RecalculateApprovalRequest` | Replacement acceptance | PARKED | Scope decision A preserves the quoted fingerprint. The row is not signed. A fingerprint move is still a postflight FAIL |
| EV-CLAIM | LIVE `usp_RecalculateSalesClaim` | Financial callee of the KM candidate | STOP | Source text is received (document 10, 2026-11-18 note). The row becomes GO only when IT / DB owner accepts that received text. Temporary-table shape is a source-level MATCH and is not this acceptance. Transaction acceptance and runtime verification are separate unsigned gates |
| EV-POLICY | LIVE `usp_GetResolvedPolicy` and `usp_ResolveDesignation` | Policy dependency | STOP | Both modules captured and accepted |
| EV-WORKFLOW | LIVE Process Approval, Mark Ready, Forward | C-1 input | PARKED | Scope decision A does not change these procedures. Their quoted fingerprints stay pinned. The row is not signed |
| EV-RESOLVERS | LIVE region, branch, and grade resolvers | KM callees | STOP | All three captured, or a recorded absence accepted as a block on DR-KM |
| EV-KM-SCHEMA | LIVE `SalesDailyActivityKMHistory` | Column proposal | STOP | Columns, constraints, indexes, and row count captured and accepted |
| EV-COUNTER-SCHEMA | LIVE counter columns on `SalesApprovalRequest` | Counter write target | PARKED | Scope decision A does not deploy counters. Preflight prints these rows and does not fail on them |
| EV-BACKUP | New backup, `RESTORE VERIFYONLY`, isolated restore | Recovery | STOP | DBA evidence accepted. The 2026-10-13 quoted backup does not count |
| EV-LOGIN | Deployment login and rights | Identity | STOP | Security owner accepts a non-`sa` login limited to the matrix in the go-live manifest. The earlier "two procedures and one table" wording is not that matrix |
| EV-WINDOW | Shared-application window | Scope | STOP | Application owner states that ITSM, HRMS, VMS, `Web.config`, and the LIVE site are outside the window |
| DR-C1 | C-1 pair | Workflow contract | PARKED | Unsigned. Scope decision A does not select a pair and does not change the workflow procedures |
| DR-C3 | Which recalc file may be proposed | Procedure ownership | PARKED | Unsigned. Scope decision A does not replace `usp_RecalculateApprovalRequest`. Choice A here is not the scope decision |
| DR-COLUMNS | Three nullable history columns | Schema approval | STOP | Choice A in `08` after EV-KM-SCHEMA. Choice B or C leaves the package BLOCKED |
| DR-KM | Commit `21975c2` | KM candidate | STOP | Choice A in `08` after EV-KM, EV-CLAIM, EV-POLICY, EV-RESOLVERS, and DR-COLUMNS choice A. Choice B leaves the package BLOCKED |
| DR-COUNTER | Commit `c9eba19` | Counter candidate | PARKED | Unsigned. Scope decision A does not deploy `c9eba19` |
| DR-IDENTITY | DBA and security approval of the login | Deployment identity | STOP | Choice A in `08` after EV-LOGIN. Choice B leaves the package BLOCKED |
| DR-BACKUP | Backup and isolated restore acceptance | Recovery approval | STOP | Choice A in `08` after EV-BACKUP. Choice B leaves the package BLOCKED |
| DR-GO | Permission to draft a LIVE design | Next session | STOP | CAB signs A in `08` after every other row is GO |

## Reading the matrix

A signed refusal closes the question and keeps the package **BLOCKED**. It does not turn that row to GO.

DR-C1 is GO on choice A, B, or C after EV-WORKFLOW, because each of those choices names the contract to preserve. DR-C3 choice C preserves the captured LIVE body and leaves DR-COUNTER at STOP, so the package stays BLOCKED for any design that would deploy `c9eba19`.

No current UAT script, rollback file, or RC-017 artifact moves a row to GO.

## Current result

**BLOCKED.** The claim-procedure source text is recorded as received and is not filed in the repository, so EV-CLAIM stays STOP. C-1, C-3, and DR-COUNTER are unsigned and parked by scope decision A. They are not GO. Every remaining evidence row and every remaining decision stays STOP. LIVE execution is not claimed. No deployment is authorized. The WP-03 stored-value design in document 12 is also unsigned and was not executed.
