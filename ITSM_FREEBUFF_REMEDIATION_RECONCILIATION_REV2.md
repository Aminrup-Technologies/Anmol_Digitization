# ITSM FreeBuff remediation reconciliation — revision 2

**Document:** ITSM_FREEBUFF_REMEDIATION_RECONCILIATION_REV2.md
**Date:** 24 November 2026
**Type:** Read-only reconciliation of the second remediation review against the current source. Not an approval. Not an implementation authorisation.
**Required verdict:** **NOT READY FOR IMPLEMENTATION**

This file records corrections that a later authorisation would have to include. It does not apply them. It does not modify `ITSM_TECHNICAL_REMEDIATION_DESIGN_REV2.md` or any earlier review.

## 1. Executive verdict

**NOT READY FOR IMPLEMENTATION.**

Revision 2 correctly keeps every gate closed and correctly keeps D-1 through D-18 Pending. The direction of X-1 through X-5 is confirmed in source. The second review's blocking and material corrections are also confirmed in source. This reconciliation accepts them and writes the missing design rules. Acceptance of a correction is not permission to build it.

No gate receives implementation authorisation from this file. A1, A2, A3, B, and C stay closed. A3 stays disabled until its recorded status read passes. Gate C retains all 18 business decisions as Pending. No formal approval evidence was found in `ITSM_BUSINESS_DECISION_REGISTER.md` section 10: every Owner ruling cell and every Date cell is empty, and every Status cell is Pending.

The five defects the task required to be resolved in the design text are resolved here as specification, and they remain unbuilt:

| Defect | Result of this reconciliation |
|---|---|
| Y-1 Numeric sequence | ASCII-digit extraction only. Zero, a sign, whitespace, any non-digit, and a next value outside 1..9999 are refused before formatting. Formatting of an accepted value cannot emit a sign. |
| Y-2 Collation | One canonical-case fold, implemented the same way in C# and SQL. A single other-case row occupies its sequence, so it cannot force the same number to be retried. The precondition counts that single row. |
| Y-4 Long historical resolutions | Existing over-length text is not truncated, not cleared, and not assumed to be editable. Reopen keeps it. Re-resolve copies it in full to a public comment and writes an audit marker instead of a cut-down body. |
| Y-5 Audit contract | One contract: callers return `Fail` before mutation; `AddAudit` throws before insert if any of the five fields exceeds its column; the transaction rolls back. No silent truncation and no partial commit. |
| Y-7 / Y-8 Output sinks | Every TicketList sink identified by the review, `lblTenantBadge`, `lblCsatFeedback`, both ApprovalConfig labels, and all four `lblMessage` assignments are named in A1, together with the other sinks the review identified. |

Y-3 stays blocking for Gate B. The review classified it with Y-1 through Y-5. It is not downgraded. The retry discriminator below does not read `SqlException.Message`.

Working tree inspected: `HEAD` `f1f4bdb2c82013e913ac682aa5e31832a3fa94fc` (`f1f4bdb`). Line numbers below were read from that tree on 24 November 2026. They are not a certified baseline and not a deployed-database result. The Sales TA/DA tags in `docs/INDEX.md` are not an ITSM baseline and were not used.

### 1.1 Documents and source

Documents read in full: `ITSM_TECHNICAL_REMEDIATION_DESIGN_REV2.md`, `ITSM_FREEBUFF_REMEDIATION_REVIEW_REV2.md`, `ITSM_SOURCE_TO_FINDING_TRACEABILITY.md`, and `ITSM_BUSINESS_DECISION_REGISTER.md` section 10 together with the decision index. `ITSM_FREEBUFF_REMEDIATION_RECONCILIATION.md` and `ITSM_FREEBUFF_REMEDIATION_REVIEW.md` were read at the sections the second review relies on (C-18, C-27, AR-9, the Gate A split). Revision 2 was not edited.

Source re-read for every disputed claim: `TicketService.cs` `GenerateTicketNumber` and `EnqueueTicketCreatedNotification`; `TicketUpdateService.cs` `ChangeStatus`, `ApproveTicket`, `RejectTicket`, `OverridePriority`, `AddComment`, `AddAudit`, and `Truncate`; `TicketDetail.aspx` and `TicketDetail.aspx.cs` `LoadTicket`; `TicketList.aspx` and `TicketList.aspx.cs` `gvTickets_RowDataBound`; `ApprovalConfig.aspx` and `ApprovalConfig.aspx.cs`; `CreateTicket.aspx`; `TicketAuditLog.cs`; `Ticket.cs`; `TicketComment.cs`; `TicketWorkflow.cs`; `Configuration.cs` seed; `202608120550515_InitialItsmSchema.cs`; `202608201200000_AddTicketAuditLog.cs`. Repository searches: `TicketNumber` assignments, `Truncate(`, and anti-forgery APIs under `AnmolDristi/ITSM`.

### 1.2 Evidence labels

| Label | Meaning |
|---|---|
| Source-verified | Read in the cited file on 24 November 2026 at `HEAD` `f1f4bdb`. |
| Accepted | The second review's claim matches that source, or matches Revision 2's own text. The design correction in this file is the rule a later build would follow. |
| Citation corrected | The finding stands. A line number or a character count in the review does not match the file. The corrected location is stated. |
| Environment-unread | A deployed row, grant, collation, job, or population count. Not queried. `AnmolDristi/Web.config` was not read. |
| Not granted | No owner ruling, no change authorisation, and no environment attachment exists. |

### 1.3 What this reconciliation did not do

It did not execute SQL, open a UAT or LIVE connection, build the solution, run a test, or start a site. It did not read `AnmolDristi/Web.config`. It did not modify application files, SQL, migrations, configuration, or any input review. It did not commit or push. It did not mark a test passed and did not treat a register recommendation as an approval.

---

## 2. Disposition of Y-1, Y-2, Y-3, Y-4, Y-5, and Y-7 / Y-8

### Y-1 — Numeric sequence validation

| Field | Record |
|---|---|
| Finding ID | Y-1, including the rule-2 length inconsistency stated with it. Also Z-2 and TG-1. |
| Source evidence | Source-verified. `GenerateTicketNumber` (`TicketService.cs` lines 359–401) takes `n.Substring(n.Length - 4)` when `n.Length >= 10` and calls the two-argument `int.TryParse` (line 390). That overload uses `NumberStyles.Integer` and the current culture. `NumberStyles.Integer` allows a leading sign and leading and trailing white space. Revision 2 X-1 rule 3 says `int.TryParse` with the invariant culture and does not name a `NumberStyles` value that excludes the sign and spaces. Rule 5 is "one more than the maximum, or 1 when none parse." Rule 6 refuses only a next value greater than 9999. Rule 7 refuses only a formatted length other than 10. The standard format `D4` pads the digit count and does not consume a character for the sign, so `(-122).ToString("D4")` is the four characters `-122`. A six-character namespace plus those four characters is length 10 and passes rule 7. Rule 2's phrase "exactly 10" is the same condition as `namespace.Length + 4` only when the namespace is six characters. |
| Verdict | **Accepted.** Not downgraded. The current parser and the Revision 2 parser both accept a signed or padded sequence. |
| Design correction | Section 4.1. No `int.TryParse`. Four ASCII digits only. Values 0 and anything outside 1..9999 never advance and are never formatted. |
| Gate | B. Blocking. |
| Approval or environment evidence | Not granted. D-4 stays Pending. The normalisation case stays unnamed. No database count of signed or blank sequences was run. |
| Test ID | TC-60. NOT RUN. |

### Y-2 — Collation consistency

| Field | Record |
|---|---|
| Finding ID | Y-2. Also TG-2. |
| Source evidence | Source-verified. The maximum read is EF `StartsWith` (line 378), which becomes a `LIKE` under the column collation. C# `string.StartsWith` has no SQL collation. Revision 2 X-1 rule 2 says the value "starts with that namespace under the collation recorded by the precondition read" and does not name a `StringComparison`. Revision 2 read 4 counts duplicate groups, including case-variant groups. A group requires more than one row. One stored lower-case number produces no group. |
| Verdict | **Accepted.** A comparison strategy that is the same in C# and SQL is specified in section 4.1. The single-row case is a separate count from the duplicate-group count. |
| Design correction | Canonical-case ASCII fold, ordinal compare in C#, `Latin1_General_BIN` compare in SQL. Case-variant rows are included in the maximum. The unique index, the precondition, the read, and the retry all use that fold. Demonstration in section 4.1.4. |
| Gate | B. Blocking. |
| Approval or environment evidence | Not granted. Read 9 (column and database collation) is environment-unread. IT has not named the canonical case. A data authorisation is required before enablement if read 4b is not zero. D-4 stays Pending. |
| Test ID | TC-61. NOT RUN. |

### Y-3 — Retry discriminator

| Field | Record |
|---|---|
| Finding ID | Y-3. The review marks this blocking. This reconciliation does not reclassify it as material. |
| Source evidence | Revision 2 Gate B retry text: the predicate is `DbUpdateException` whose innermost `SqlException.Number` is 2601 or 2627 and whose index name is the ticket-number index. The index name is obtained from the exception message. SQL Server embeds that identifier in a message whose surrounding words follow the instance language. There is no ITSM caller of `sp_getapplock` or of this predicate today; the defect is in the proposed predicate. |
| Verdict | **Accepted.** |
| Design correction | Section 4.1.5. Do not read `SqlException.Message`. Retry only when the number is 2601 or 2627 and a new-context probe finds the attempted number under the same fold used for allocation. Any other unique violation is not retried. |
| Gate | B. Blocking. |
| Approval or environment evidence | Not granted. No server language was read. The probe does not require a language precondition. |
| Test ID | TC-62. NOT RUN. |

### Y-4 — Existing long resolutions

| Field | Record |
|---|---|
| Finding ID | Y-4. Also Z-1 and TG-4. |
| Source evidence | Source-verified. `ChangeStatus` replaces `ResolutionText` only when the target name is `Resolved` (lines 98–102) and does not clear it on any other transition. `TicketWorkflow` allows `Resolved` → `In Progress` and `Resolved` → `Closed` (lines 23–26). `ResolutionText` has no `[StringLength]` (`Ticket.cs` line 107) and the column is unbounded (`202608120550515_InitialItsmSchema.cs` line 93). `TicketAuditLog.Detail` is 1000 (`TicketAuditLog.cs` lines 63–64; migration line 19). `TicketComments.CommentText` is unbounded (migration line 173) and has no `[StringLength]`. Revision 2 A2 says a previous text longer than 1000 returns `Fail` and changes nothing, and reopen leaves the column unchanged. No method clears the column. Viewers who cannot add an internal note do not receive internal comments (`TicketDetailService.cs` lines 190–192). They do receive `lblResolution`. |
| Verdict | **Accepted.** Revision 2 creates a dead end for a ticket that already holds a long resolution. The population of such tickets was not queried and is not assumed to be zero. Those rows are not assumed to be editable or clearable. |
| Design correction | Section 4.2. Keep the stored characters. Do not truncate them. Reopen does not refuse. Re-resolve with a new legal text copies the previous characters in full onto a public comment in the same transaction and writes `Detail` null on the superseded audit row. A new text over 1000 still returns `Fail` and writes nothing. |
| Gate | A2. Blocking. |
| Approval or environment evidence | Not granted. The preservation path is an owner acknowledgement in section 8 and is not recorded. D-17 stays Pending. Read 19 (the inventory of existing over-length resolutions) is environment-unread. |
| Test ID | TC-63. NOT RUN. Requires a disposable-database precondition. Not UAT. Not LIVE. |

### Y-5 — Audit failure contract

| Field | Record |
|---|---|
| Finding ID | Y-5. Also TG-3. Y-14 is the fifth field of the same contract. |
| Source evidence | Source-verified. `AddAudit` is `private static void` (lines 602–622). It assigns `Truncate` for `ActionType`, `OldValue`, `NewValue`, and `Detail`, and assigns `ActorCode` raw (line 619). `Truncate` (lines 624–632) returns `Substring(0, maxLength)` and returns no signal. Every caller invokes `AddAudit` after it has already assigned ticket or approval fields: `ChangeStatus` 95–110, `ReassignTicket` 167–176, `OverridePriority` 240–245, `ApproveTicket` 316–340, `RejectTicket` 407–440, `AddComment` 489–504. `CreateTicket` writes the `Created` row itself (lines 198–209) and does not call `Truncate`. `TicketDetail.aspx.cs` posts call `ApplyResult` and have no catch around `ChangeStatus`, `ApproveTicket`, or `RejectTicket`. `ApplyResult` (lines 366–376) shows `result.Error` only when the service returns `Fail`. A thrown exception leaves that page. The service `catch (Exception)` rolls back and rethrows. |
| Verdict | **Accepted.** Revision 2's phrase "Fail or a thrown exception" is not a contract. One contract is chosen below. |
| Design correction | Section 4.3. User-controlled over-length input returns `Fail` before any mutation, with a fixed message that names the limit and does not echo the text. `AddAudit` does not truncate. If it is reached with any of the five fields over its column, it throws before `TicketAuditLogs.Add`. The existing rollback rethrows. No page gains a handler. |
| Gate | A2. Blocking. The `Created` row is the same no-truncation rule and stays in the create transaction (Gate B for the retry; the no-truncation rule itself is part of this contract). |
| Approval or environment evidence | Not granted. Comment-capacity and resolution-maximum acknowledgements are not recorded. Deployed name lengths were not queried. |
| Test ID | TC-64. NOT RUN. |

### Y-7 / Y-8 — Missing output sinks

| Field | Record |
|---|---|
| Finding ID | Y-7 and Y-8. Also TG-5. Y-15 is the notification sink and is dispositioned in section 3; A1's list includes it so the gate is complete. |
| Source evidence | Source-verified. TicketList template sinks that are not `BoundField`: `TicketList.aspx` line 141 `Eval("TicketNumber")` inside the anchor, line 151 `Eval("PriorityName")` on `lblPriorityBadge`, line 157 `Eval("StatusName")` on `lblStatusBadge`. `BoundField` columns on that grid are Title, RequestTypeName, RequesterName, AssigneeName, CategoryName, BranchName, and CreatedOn (lines 145–165). RowDataBound reads `lblPriority.Text` (lines 144–160) and `lblStatus.Text` (lines 163–168) to build CSS classes. `lblTenantBadge.Text` is assigned at `TicketDetail.aspx.cs` line 231 from `FormatTenantBadge` (lines 302–315). `lblCsatFeedback.Text` is assigned at line 260 inside the `CsatScore.HasValue` branch that starts at line 256. The review's "line 256" is the branch; the assignment is line 260. **Citation corrected.** The finding stands. `ApprovalConfig.aspx.cs` lines 126–128 assign `lblCurrentItHead` from approver code and name, and `lblCurrentItHeadStatus` from status. Four `lblMessage` assignments: `TicketDetail.aspx.cs` line 393 (clear at 388), `TicketList.aspx.cs` line 336 (clear at 331), `CreateTicket.aspx.cs` line 454 (clear at 449), `ApprovalConfig.aspx.cs` line 155 (clear at 150). `CreateTicket.aspx` has no other `asp:Label`. |
| Verdict | **Accepted.** Revision 2's A1 list does not name the three TicketList bindings, the tenant badge, the CSAT feedback label, or the two ApprovalConfig labels. The four `lblMessage` controls exist at the cited assignment lines and are confirmed. |
| Design correction | Section 4.4 is the A1 enumeration. Encode at each named assignment. Derive list badge classes from the raw data item, not from already-encoded `Label.Text`. `string.Empty` clears are not encoded. `BoundField` and `ListItem` text stay on the framework encoder. |
| Gate | A1. Required before A1 can be considered complete. A1 is still not authorised. |
| Approval or environment evidence | A later A1 change authorisation must name this list and must carry the AR-9 disclosure (Z-4). No business ruling is required for encoding. Not granted. |
| Test ID | TC-66 for the three TicketList sinks. TC-67 for `lblTenantBadge`, `lblCsatFeedback`, and both ApprovalConfig labels. TC-27 already covers the four `lblMessage` controls and stays NOT RUN. TC-73 covers the notification number. |

---

## 3. Full reconciliation

Every finding in the second review is recorded. None is omitted. A finding is not downgraded. Where this file adds a rule, the rule is the design correction and the gate stays closed.

### 3.1 Material findings Y-6 through Y-12

Y-7 and Y-8 are fully specified in section 2 and section 4.4. They are listed here so the set of seven is complete.

| ID | Source evidence | Verdict | Design correction | Gate | Approval or environment evidence | Test ID |
|---|---|---|---|---|---|---|
| Y-6 | `ApproveTicket` assigns `approval.Comments = comments` with no trim (line 318) and passes `comments` to `AddAudit` (line 340). `RejectTicket` stores `reason.Trim()` (line 409). `TicketApproval.Comments` is 500 characters (migration line 139). `btnApprove_Click` has no catch. | **Accepted.** | The string checked is the string stored. Section 4.3.2. A non-null approval comment is trimmed, the trimmed value is stored, and that value is refused when its length is over 500, before any approval field is assigned. | A2 | Not granted. Same release as the other length checks. | TC-65. NOT RUN. |
| Y-7 | Section 2. | **Accepted.** | Section 4.4. | A1 | A1 authorisation not granted. | TC-66. NOT RUN. |
| Y-8 | Section 2. Line citation for CSAT feedback corrected from 256 to 260. | **Accepted.** | Section 4.4. | A1 | A1 authorisation not granted. | TC-67. NOT RUN. |
| Y-9 | `txtResolution` (`TicketDetail.aspx` lines 254–256) has no `MaxLength` and the label at line 253 does not state a maximum. C-18 in the first review required the UI to state the maximum before the user types. Revision 2 section 13 records an acknowledgement and does not require the page to state the limit. | **Accepted.** C-18 stays open until the page text in section 4.2.4 is part of the authorised design. | The resolution label states the 1000-character maximum. The server check remains the rule. | A2 | The section 8 acknowledgement is not recorded. D-17 stays Pending. | TC-68. NOT RUN. |
| Y-10 | `ChangeStatus` returns before the resolution branch when `currentStatus.IsTerminal` (lines 73–77). The seed writes `Resolved` with `IsTerminal = false` only when the status table is empty (`Configuration.cs` lines 26–38). Revision 2 attaches `IsTerminal` to A3's read 1 and not to A2. | **Accepted.** | A2 is not promoted unless the same read shows an active, non-terminal row named `Resolved`. A seed is not that attachment. | A2 | Read 1 is environment-unread. | TC-69. NOT RUN. Named-environment read. |
| Y-11 | Repository search on this date: the only assignment to `itsm.Tickets.TicketNumber` is `TicketService.cs` line 172. No `*.sql` file in the repository contains `TicketNumber`. `AGENTS.md` records that ITSM, HRMS import, and VMS share `DbConn`. | **Accepted.** The repository search does not prove the absence of a writer outside this tree. | Read 22 in section 7. Gate B stays disabled until that record exists. The application lock protects only writers that take it. | B | Environment-unread for jobs and for the other applications on `DbConn`. | TC-70. NOT RUN. |
| Y-12 | Lines 364–368: `Prefix`, or `Code` when `Prefix` is null, is trimmed, and a result longer than two characters is cut with `Substring(0, 2)`. `Prefix` is `char(2)` (migration line 243). `Code` is `maxLength: 10` (line 241). A null prefix and a code of three or more characters therefore allocates today. AR-12 names the trim-to-one-character case and does not name this cut. | **Accepted.** | The exact-2 rule blocks trimmed length 0, 1, and greater than 2. It also blocks a prefix character outside ASCII letters and digits (section 4.1.1). No pad, no substring, no `Code` fallback. The section 8 acknowledgement names the longer-prefix block as well as the short-prefix block. | B | Service-owner acceptance for each blocked type, or a separate data correction. Not granted. Prefix inventory is read 6 and is environment-unread. | TC-71. NOT RUN. |

### 3.2 Precision findings Y-13 through Y-16

| ID | Source evidence | Verdict | Design correction | Gate | Approval or environment evidence | Test ID |
|---|---|---|---|---|---|---|
| Y-13 | `ResolveInitialStatusId` (`TicketService.cs` lines 404–432) takes one boolean, `requiresManagerApproval \|\| requiresItHeadApproval`, passed from line 166. Two active request types with the same two flags produce one lookup. Revision 2 AR-1 and read 1 say "per active request type". | **Accepted.** | Read 1 records the result per distinct pair of approval flags and lists the active types in that pair. It does not require two identical lookups. | A3, as a narrowing of the unread read. The gate stays disabled. | Read 1 is environment-unread. | TC-72. NOT RUN. |
| Y-14 | `ActorCode = actorCode` at line 619, outside `Truncate`. Entity `[StringLength(50)]` (lines 70–72). Migration `ActorCode` `maxLength: 50` (line 20). `UserMaster.EmployeeCode` is `[StringLength(50)]`. An over-length code fails EF validation rather than being cut. | **Accepted.** The source fact is the EF validation failure. The pre-mutation `Fail` formerly written in this cell is withdrawn by the in-place correction in section 12 (Y-26). | `ActorCode` is not truncated. No caller returns `Fail` for length. `SaveChanges` fails the annotation and the existing catch rolls back. Section 12. | A2 | Not granted. | TC-64 actor clause corrected. TC-89. Both NOT RUN. |
| Y-15 | Body append at `TicketService.cs` line 478 writes `ticket.TicketNumber` with no encode. Title on the next line is encoded. Subject parameter at line 502 concatenates the number. The review cites lines 490–503, which contain the subject parameter and not the body append. **Citation corrected.** The finding stands. `MST_RequestType.Prefix` has no character check constraint. A1 is specified to ship before Gate B. | **Accepted.** | Section 4.4. The HTML body encodes the number in A1. The subject is plain text: strip CR, LF, and other control characters, and encode `&`, `<`, `>`, `"`, and `'` so a later HTML consumer cannot treat the subject as markup. Html-encoding the entire subject was rejected as the subject encoding because the column is a mail subject, not an HTML body. The safety requirement is accepted. | A1 | Not granted. D-8 stays Pending. Encoding the number does not choose a public origin. | TC-73. NOT RUN. |
| Y-16 | Revision 2 section 10 lists TC-06, TC-08, TC-17, and TC-48 as waiting on an owner decision. TC-32 is a 1001-character resolution refusal, which is the same acknowledgement as TC-06. | **Accepted.** | TC-32 is added to that list. TC-63 and TC-68 wait on the same resolution acknowledgement, including the preservation path. | A2 | Acknowledgements in section 8 are not recorded. | TC-32, already in the preserved matrix. NOT RUN. No new ID. |

### 3.3 Risks introduced or left open by Revision 2

| ID | Source evidence | Verdict | Design correction | Gate | Approval or environment evidence | Test ID |
|---|---|---|---|---|---|---|
| Z-1 | Same mechanism as Y-4. | **Accepted.** The dead end is removed by section 4.2. The risk stays open until that design is authorised and the population read exists. | Section 4.2. | A2 | Section 8 preservation acknowledgement not recorded. Read 19 unread. | TC-63. NOT RUN. |
| Z-2 | Same mechanism as Y-1. | **Accepted.** The format path cannot emit `CR2608-122` once section 4.1 is the rule. The risk stays open until Gate B is authorised. | Section 4.1. | B | Not granted. | TC-60. NOT RUN. |
| Z-3 | The proposed lock is taken before the insert and released at `transaction.Commit()` (`TicketService.cs` lines 159 and 287). The create method calls `SaveChanges` for the ticket, the audit, and each approval row, then `ExecuteSqlCommand` for the notification, then commits. A retry of three attempts can take the lock three times. `@timeoutMs` is unnamed. | **Accepted.** Operational. Not reclassified as a correctness defect. The hold window and the missing budget are now stated. | Section 4.1.6. Gate B is not enabled until IT names the milliseconds. | B | IT must name the timeout. Not selected. | TC-74. NOT RUN. Cannot become an acceptance test until the timeout is named. |
| Z-4 | No `AntiForgery`, `RequestVerificationToken`, or `ViewStateUserKey` reference under `AnmolDristi/ITSM`. Revision 2 AR-9 assigns neither an owner nor a date and places the token outside every gate. ViewState MAC is a `Web.config` fact and was not read. | **Accepted.** This reconciliation does not invent an owner or a date. | A1's later change authorisation must name the missing token, the unread ViewState MAC setting, and that A1 does not add a token. A1 stays closed. | A1 disclosure. The token itself stays outside A1, A2, A3, B, and C. | Security owner not named. Not granted. | TC-56 remains the source inspection and is not a security pass. TC-77 is the document checkpoint that the authorisation text carries the disclosure. Both NOT RUN. |
| Z-5 | Revision 2 section 9 has no count of `ResolutionText`, `CommentText`, or `TicketAuditLog.Detail` values above the new caps. Section 13 asks the owner to accept the caps without that population. | **Accepted.** | Reads 19, 20, and 21 in section 7. The section 8 acknowledgements are not informed, and are not recorded, until those counts are attached. | A2 | Environment-unread. | TC-76. NOT RUN. Named-environment read. |

The second review's concurrency note is confirmed and is not a new risk. `ApproveTicket` and `RejectTicket` set `ticket.ModifiedOn` unconditionally (lines 335 and 434) after loading the ticket through `LoadMutableTicket`, which writes `RowVersion.OriginalValue` (line 584). `TicketApproval` has no concurrency token. TC-45 stays the test of that behaviour and stays NOT RUN.

### 3.4 Test gaps TG-1 through TG-6

| ID | Source evidence | Verdict | Design correction | Gate | Approval or environment evidence | Test ID |
|---|---|---|---|---|---|---|
| TG-1 | TC-22 covers stored lengths 6, 7, 8, and 9. No preserved case feeds a ten-character value whose last four characters are `-123`, `+123`, ` 123`, or `0000`. | **Accepted.** | TC-60. | B | Disposable database. Not UAT. Not LIVE. | TC-60. NOT RUN. |
| TG-2 | TC-24 covers two request types that normalise to one namespace. It does not cover one stored lower-case row at the current maximum. | **Accepted.** | TC-61, using the section 4.1.4 figures. | B | Disposable database. Not UAT. Not LIVE. | TC-61. NOT RUN. |
| TG-3 | TC-37 expects 100-character names to be stored in full. It does not refuse an over-length audit field. | **Accepted.** | TC-64. | A2 | Disposable database. Not UAT. Not LIVE. | TC-64. NOT RUN. |
| TG-4 | TC-06, TC-32, and TC-35 do not start from a resolution that is already longer than 1000. | **Accepted.** | TC-63. The long text is a fixture, not a successful call to the new resolve rule. | A2 | Disposable database. Preservation acknowledgement not recorded. | TC-63. NOT RUN. |
| TG-5 | TC-02 covers the list title, which is a `BoundField`. It does not cover the anchor or the two badge labels. | **Accepted.** | TC-66. | A1 | Rendered page. The site was not started. | TC-66. NOT RUN. |
| TG-6 | TC-23 covers a refused lock. It does not show that commit releases the lock. | **Accepted.** | TC-74. | B | Disposable database, two sessions, and a named timeout. The timeout is not named. | TC-74. NOT RUN. |

### 3.5 Other second-review observations that are not Y, Z, or TG items

| ID | Source evidence | Verdict | Design correction | Gate | Approval or environment evidence | Test ID |
|---|---|---|---|---|---|---|
| C-18 | Open in the second review. Same evidence as Y-9. | **Accepted.** Remains not closed by Revision 2. Closed in this design text only as a requirement, not as a shipped page. | Section 4.2.4. | A2 | Section 8. Not recorded. | TC-68. NOT RUN. |
| C-27 | Open in the second review. `OrderBy(m => m.Id)` makes one call deterministic against one set of rows. Revision 2 does not say that a later insert of a lower `Id` may change the assignee. TC-12 says "unchanged data" and does not forbid a stability assertion across a change. | **Accepted.** | A1's dispatch sentence: ordering is per call. A test must not assert that the assignee is unchanged after a mapping row is inserted, deleted, or deactivated. | A1 | Not granted. D-14 stays Pending. | TC-75. NOT RUN. |
| AR-9 | Section 3.3 Z-4. No token API in the ITSM tree. | **Accepted.** Held as a record. Owner and date stay unassigned on purpose. Assigning either here would fabricate an owner. | A1 authorisation disclosure. Token remains outside the gates. | Outside the five gates, disclosed on any later A1 authorisation. | Not assigned. | TC-56 and TC-77. NOT RUN. |
| Review §10.2 index and allocator order | Initial migration indexes on `itsm.Tickets` are lines 117–128. None is on `TicketNumber`. Removing the `RequestTypeId` filter widens the maximum read. The exclusive lock would be held across that read. | **Corrected in place (Z-8).** The widened allocator is deployed before the index, or in the same window in which the previous allocator serves no create. The previous cell created the index first. That order is withdrawn because the previous allocator must not run against the new index. The folded predicate is still not claimed to be a seek. Whether the plan seeks is environment-unread. | Section 4.1.7 and section 12. | B | Schema mechanism not chosen. Read 10 unread. Not granted. | TC-78 corrected. TC-88. Both NOT RUN. |
| Review §10.1 A2 without A1 | A2's refusal strings are fixed. They are assigned to `lblMessage`, which A1 encodes. Shipping A2 first would send those strings through an unencoded label. | **Accepted** as a release preference, not as a new defect. The review is right that A2 does not technically depend on A1. | When a release plan exists, A1 ships before A2. Neither ships on the authority of this file. | A1 before A2, preference only. | Not granted. | No new test. TC-27 covers encoded `lblMessage` once A1 exists. NOT RUN. |
| AR-5 character count | The fixed sibling comment at `RejectTicket` lines 420–421 is `Closed because the ticket was rejected at another approval level.` That string is 65 characters. The review says 53. | **Citation corrected.** AR-5's conclusion holds: stranding is an approve-path outcome, and the constant fits the 500-character comment column. | No design change. | Unchanged. | None. | TC-39 and TC-53. NOT RUN. |

### 3.6 Findings the second review confirmed, with no further design change

These were re-checked only where this reconciliation relies on them. Confirming them does not open a gate.

| ID | Verdict | Evidence relied on |
|---|---|---|
| X-2 | Confirmed. The unnamed timeout is Z-3, not a reversal of X-2. | The only `ExecuteSqlCommand` in the ITSM tree is the notification insert at lines 497–503. `GenerateTicketNumber` is called inside the transaction opened at line 91. |
| X-3 | Confirmed. The literal at line 503 is 11 characters. 1000 − 11 = 989. | `TicketUpdateService.cs` line 503. `Detail` max 1000. |
| X-5 | Confirmed. F-08 stays in A3 only. | `ApproveTicket` lines 316–332 and `RejectTicket` lines 407–432 commit the approval change when the status row is missing. |
| F-06, F-07, F-08, F-09 | Confirmed, as the second review states. | No correction was found that reverses those verdicts. F-07's `OrderBy` gains the C-27 sentence only. |
| F-10 | Confirmed. The seed is not a deployed reading. | `Configuration.cs` lines 26–38. |
| AR-1 through AR-8, AR-10 through AR-16 | Hold, with the corrections already named: Y-13 on AR-1, Y-12 on AR-12, the 65-character citation on AR-5. | Second-review locations matched the files read for this reconciliation. |
| C-01 through C-17, C-19 through C-26, C-28 through C-32 | Closed in Revision 2, except where a Y finding re-opens the point (C-01 by Y-2, C-02 by Y-3, C-05 by Y-12, C-13 by Y-15, C-16 by Y-6). | Those Y findings are the disposition. |
| D-4, D-9, D-17 treatment in Revision 2 | Confirmed. Recommendations are not approvals. | Register section 10, read on this date. |

---

## 4. Revised technical design requirements

These requirements replace the disputed sentences in Revision 2 for any later implementation discussion. They are not built. They authorise nothing. Revision 2's rules that this section does not change remain as written there.

### 4.1 Sequence allocation

#### 4.1.1 Namespace

1. Take the request type's `Prefix`. Do not substitute `Code`. Do not substitute `XX`. Trim spaces.
2. Accept the prefix only when its length is exactly 2 and both characters are in `A–Z`, `a–z`, or `0–9`. Otherwise throw `InvalidOperationException` with a controlled message and persist nothing. This covers a trimmed length of 0 or 1 (AR-12) and a trimmed length greater than 2 (Y-12), including a null `Prefix` whose `Code` is longer than two characters.
3. IT names one canonical case, upper or lower, before Gate B is enabled. This file does not name it. The examples below use upper only as an illustration.
4. Fold with an explicit ASCII map, not `ToUpperInvariant` and not the current culture. `a–z` map to `A–Z` when the canonical case is upper, and `A–Z` map to `a–z` when it is lower. Digits are unchanged. The same map is what SQL applies, because the prefix read has already rejected every non-ASCII character.
5. The namespace is the folded prefix plus `yy` plus `MM` from `createdOn`, invariant calendar, four digits. It is exactly six characters.

#### 4.1.2 Which stored values advance the sequence

A stored `TicketNumber` advances the sequence only when all of the following are true:

1. Its length is exactly 10. This is the only length rule. The parenthetical `namespace.Length + 4` is not a second rule. It is true because the namespace is fixed at 6.
2. The ASCII fold of characters at indexes 0 through 5 compares equal to the namespace with `StringComparison.Ordinal`.
3. Each of the characters at indexes 6, 7, 8, and 9 is an ASCII digit `0`–`9`. A sign, a space, a decimal point, a letter, or a non-ASCII digit fails this test. `int.TryParse` is not used.
4. The integer value of those four digits is in 1..9999. The value 0 (`0000`) does not advance the sequence and is not rewritten.

Malformed values, including lengths 6, 7, 8, and 9, values that fail the fold, non-digit sequences, and `0000`, stay stored. They are not updated by allocation.

The maximum read is not EF `StartsWith`. It is a SQL predicate with an explicit collation, so a case-sensitive column cannot hide the other case:

```sql
SELECT TicketNumber
FROM itsm.Tickets
WHERE LEN(TicketNumber) = 10
  AND UPPER(LEFT(TicketNumber, 6)) COLLATE Latin1_General_BIN
      = @namespaceCanonicalUpper COLLATE Latin1_General_BIN;
```

When IT names lower as the canonical case, the predicate uses `LOWER` and the lower namespace. `UPPER` and `LOWER` are used only after read 6 has shown that every active prefix is two ASCII alphanumerics, for which this fold and the C# map agree. The C# digit test is then applied to the result. The SQL predicate may over-include. It must not drop a case variant. The application includes every row that passes the four tests above, including a row whose raw prefix differs from the namespace only by case.

#### 4.1.3 Next value and formatting

1. The next sequence is one more than the maximum accepted integer, or 1 when no row is accepted.
2. If the next sequence is less than 1 or greater than 9999, throw `InvalidOperationException` stating that the monthly sequence for that prefix is exhausted or is not a positive sequence. Do not format. Do not insert.
3. Format only an integer in 1..9999. Produce exactly four ASCII digits, with leading zeros, and no sign. Concatenate the six-character namespace and those four digits.
4. If that result is not length 10, or if any character of the sequence portion is not an ASCII digit, throw and do not insert. With the rules above this backstop does not fire for a legal value. It is what stops a later edit from calling `ToString("D4")` on a negative number.
5. Historical numbers are not rewritten. New allocation emits only this ten-character form.

A negative number cannot be produced. `D4` is not applied to a value outside 1..9999. Zero is not emitted.

#### 4.1.4 Why one lower-case row cannot cause a false retry or a false exhaustion

Illustration only. The canonical case in the illustration is upper. The namespace is `CR2608`. Stored rows are `CR26080003` and `cr26080007`.

| Step | Result |
|---|---|
| Fold of `cr26080007` | `CR2608`, ordinal-equal to the namespace. The row is not dropped. |
| Digits | `0007` are ASCII digits. The integer 7 is inside 1..9999, so the row contributes 7. |
| Next | max(3, 7) + 1 = 8. Format `CR26080008`. |
| Equality with `cr26080007` | Not equal ordinally. Not equal after the ASCII fold of the whole value (`CR26080007` versus `CR26080008`). |
| Unique index | A case-sensitive index and a case-insensitive index both treat those two strings as different keys. The insert does not raise 2601 or 2627. The retry does not run. |
| Remaining sequences | 0009 through 9999 are still available. The month is not exhausted. |

The failure mode the review described is the opposite rule: an ordinal `StartsWith("CR2608")` drops `cr26080007`, the next formatted value is `CR26080004` if that sequence is the one the dropped row holds, and a case-insensitive unique index treats `CR26080004` and `cr26080004` as the same key. The retry re-reads, drops the row again, and emits the same value until the attempt limit. Section 4.1.2 does not drop the row, so that loop does not start.

A single lower-case `cr26089999` and no higher accepted value produces next = 10000. The operation throws before format and before insert. That is genuine exhaustion of 9999, not a retry of one colliding value, and not a permanent stall while numbers remain. The row is left stored.

#### 4.1.5 Retry

On `DbUpdateException`, take the innermost `SqlException`. Retry only when its `Number` is 2601 or 2627 and a read on a new context finds a `TicketNumber` equal to the attempted value, either by `StringComparison.Ordinal` or by the ASCII fold of all ten characters compared ordinally. Do not read `SqlException.Message`. Do not search the message for the index name. The server language is then irrelevant.

If the probe finds no such row, do not retry. Throw `InvalidOperationException` with the controlled message. The page path that already shows `InvalidOperationException.Message` (`CreateTicket.aspx.cs` lines 203–208) therefore does not show a raw SQL message for this failure.

A case-variant of a different sequence fails the probe, so it cannot start a retry. A case-variant of the same sequence is not generated, because section 4.1.2 consumed that integer. A concurrent insert of the same canonical number passes the probe; the next attempt re-reads the maximum and moves forward. At most three attempts. Each attempt uses a new context, re-reads inputs, and takes a new `createdOn`. The page does not own the retry.

#### 4.1.6 Lock

The X-2 batch is unchanged in shape. `@timeoutMs` is still unnamed. The lock is held from acquisition, before `Tickets.Add`, through `transaction.Commit()`, including the ticket insert, the audit insert, the approval inserts, and the notification insert. A user action that retries can acquire it up to three times. Gate B is not enabled until IT names the milliseconds, knowing that the maximum read may scan `itsm.Tickets` while the exclusive lock is held.

#### 4.1.7 Index order and compatible uniqueness

Deploy the corrected shared-prefix allocator, which removes the `RequestTypeId` filter, before creating the unfiltered unique index on `itsm.Tickets(TicketNumber)`, or apply both in one window during which the previous per-request-type allocator serves no create. The previous sentence in this paragraph, which created the index first, is withdrawn (section 12, Z-8). `TicketNumber` is `NOT NULL`. Rollback drops the index and confirms it is absent before the previous allocator is allowed to serve creates. Rows remain. The previous allocator must not run while the index is present.

The index uses the column collation. Compatibility does not require this file to choose a case-sensitive or case-insensitive index:

- The allocator emits only the canonical-case namespace plus four ASCII digits.
- Every accepted row, either case, occupies its integer.
- The inserted integer is therefore not already stored in either case.
- Both collations accept that insert.
- Read 4a must be zero before the index is created, otherwise a case-insensitive index cannot be built.
- Read 4b must be zero, or a data authorisation must rewrite the variant rows to the canonical case, before enablement. The inclusion rule in section 4.1.2 is what keeps a leftover variant from exhausting the retry. Enablement still wants the stored prefixes to match the canonical case.

#### 4.1.8 Precondition counts that replace the case half of read 4

| Count | What it counts | Enablement |
|---|---|---|
| 4a | Groups of two or more `TicketNumber` values that compare equal under `Latin1_General_CI_AS`. A single row is not a group. | Zero groups, or a separate data authorisation. |
| 4b | Rows of length 10 whose ASCII-folded first six characters equal a normalised namespace and whose raw first six characters are not ordinally equal to that namespace. A count of one is a hit. | Zero rows, or a data authorisation that rewrites them before enablement. |
| 4c | Ticket numbers that contain whitespace or a non-printing character. | Recorded. Not rewritten by this design. |

### 4.2 Historical resolutions

#### 4.2.1 Inventory, with no assumption

Before A2 can be promoted, read 19 returns the ticket id and `LEN(ResolutionText)` for every row whose resolution is longer than 1000 characters. This reconciliation did not run that read. The count is unknown. The design does not update, trim, or delete those rows. It does not require a user to edit them. It does not require a data-repair script.

#### 4.2.2 Display and reopen

`lblResolution` continues to show the stored value in full, including a value longer than 1000. Encoding is A1's job and does not cut the string.

Reopen from `Resolved` to `In Progress` leaves `ResolutionText` and `ResolvedOn` unchanged, whatever the length is. `StatusChange.Detail` stays null. `OldValue` on that status row is `Resolution retained`. Reopen does not insert a preservation comment and does not insert a `Resolution` audit row for a pre-change ticket.

If the deployed `Resolved` row is missing, inactive, or terminal, A2 is not promoted (Y-10). The history rules are then unreachable.

#### 4.2.3 Re-resolve when the previous text fits in 1000 characters

Write one `Resolution` row with `Detail` equal to the previous text and `NewValue` `Superseded`. The superseded `OldValue` is the marker in section 12. Then store the new trimmed text under the 1000-character check and write `NewValue` `Current` with `OldValue` exactly `current;resolvedOn=` plus that transition's round-trip timestamp. One `SaveChanges`. A new text longer than 1000 returns `Fail` and writes nothing.

#### 4.2.4 Re-resolve when the previous text is longer than 1000 characters

This replaces Revision 2's "return Fail and change nothing."

The new text is trimmed first. If the trimmed new text is empty, keep today's blank refusal. If its length is greater than 1000, return `Fail` with `Resolution must be 1000 characters or fewer so the audit row can store it.` Change nothing: no comment, no audit row, no column change.

If the trimmed new text length is 1..1000, do all of the following in the one existing transaction and the one `SaveChanges` inside `Commit`:

1. Insert one `TicketComment`. `IsInternal` is false, because viewers who cannot add an internal note cannot see internal comments and can see the resolution today (`TicketDetailService.cs` lines 190–192). `CreatedByCode` is the actor already accepted by `LoadForProcess`. `CommentText` is the exact previous `ResolutionText` characters, preceded by this header and a newline:

    `Previous resolution retained in full before re-resolve because it exceeds the 1000-character audit limit.`

    The header is not a slice of the body. The previous characters follow it unmodified. This comment is not submitted through `AddComment` and is not subject to the 1000 / 989 comment cap. That cap exists so a comment can be copied into `Detail`. This row exists because `Detail` cannot hold the historical body.

2. Insert one `Resolution` audit row. `Detail` is null. `NewValue` is `Superseded`. `OldValue` is `superseded;length={n};resolvedOn={stamp};modifiedBy={code}` where `{n}` is the character length of the previous text, `{stamp}` is the previous `ResolvedOn` in round-trip form or the literal `null`, and `{code}` is the previous `ModifiedByCode` or the literal `null`. The earlier `omitted;length=` form is withdrawn. If that marker would exceed 200 characters, return `Fail` and write nothing. Do not truncate the marker and do not truncate the body into `Detail`.

3. Replace `ResolutionText` with the new trimmed text and set `ResolvedOn` to the transition's single `now`. Insert the `Current` resolution row with `Detail` equal to that new text and `OldValue` exactly `current;resolvedOn=` plus the round-trip form of that same `now`. The `StatusChange` row remains, with `Detail` null.

A failure of the comment insert, including a foreign-key failure on `CreatedByCode`, rolls the transaction back. The long text stays on the ticket. There is no partial replace.

The page states the maximum before the user types. The resolution label reads `Resolution notes (1000 characters maximum)`. `MaxLength="1000"` may be set as a hint. The server check is the rule. This is the C-18 surface.

Half-populated pairs stay as Revision 2 describes them. Pre-change tickets are not backfilled. D-17, the minimum-length question, stays Pending. Refusing to copy an over-length body into `Detail` is consistent with the register's unapproved option A for D-17. It is not an approval of D-17.

### 4.3 Audit failure contract

One contract. There is no second channel.

#### 4.3.1 Rules

1. Delete the `Truncate` calls. Delete `Truncate`. The only caller is `AddAudit`.
2. The five fields and their maxima are `ActionType` 50, `OldValue` 200, `NewValue` 200, `Detail` 1000, and `ActorCode` 50. Null is within the limit for `OldValue`, `NewValue`, and `Detail`. `ActorCode` is required.
3. Each public method decides the exact strings it will store, checks those strings, and returns `TicketUpdateResult.Fail` before it assigns a ticket, approval, or comment field and before it calls `AddAudit`. The message is one of the fixed sentences in section 4.3.3. The message does not contain the rejected text.
4. `AddAudit` adds an entity only when all five fields are within those maxima. Otherwise it throws `InvalidOperationException` with `Audit field {name} length {n} exceeds {max}.` and does not include the field value. The throw is the backstop for a programmer error, including an over-length `ActorCode` that a caller failed to check, and an over-length status or priority name that the migration limits should have prevented.
5. The throw happens inside the transaction. The existing `catch (Exception)` rolls back and rethrows. `TicketDetail` does not gain a catch. `ApplyResult` remains the only user-facing path, and it runs only for `Fail`.
6. `CreateTicket`'s direct `Created` insert does not gain `Truncate`. If any of its five fields would exceed a column, it throws before `Tickets.Add`, and the transaction commits nothing.
7. Update methods keep one `SaveChanges` that writes the ticket change and the audit row together. A refused call never reaches `Commit`. There is no approval-status change without its audit row, and no audit row without its ticket change.

#### 4.3.2 Callers, and the string each one checks

| Caller | Check before mutation | Stored value |
|---|---|---|
| `RejectTicket` | Blank check, then trimmed length greater than 500, both before the context opens. | `reason.Trim()` on the approval comment and on `Detail`. |
| `ApproveTicket` | When `comments` is null, store null and pass null. When it is non-null, trim it, refuse a trimmed length greater than 500, and store that trimmed string. The check is before any approval field is assigned. | The trimmed string, or null. |
| `OverridePriority` | Trimmed reason length greater than 500, beside the existing blank check, before the priority is changed. | That trimmed string as `Detail`. |
| `AddComment` | Build the detail first. Public: trimmed length greater than 1000. Internal: trimmed user text greater than 989, because the 11-character prefix shares `Detail`. Return `Fail` before the comment is added and before `ModifiedOn` is set. | The comment row stores the trimmed user text. `Detail` stores the built string. |
| `ChangeStatus` | Resolution rules in section 4.2. Status names are not user text; an over-length name hits the `AddAudit` backstop and rolls back. Actor length is not a `Fail` check (section 12). | Resolution column and audit rows as section 4.2. `StatusChange` detail stays null. |
| `ReassignTicket` | Assignee codes already come from the user table, maximum 50, or the constant `Unassigned`. | Those values. No truncation. |
| Every mutating method | No pre-mutation `Fail` for actor length. An actor string longer than 50 fails EF6 validation at `SaveChanges`. The existing catch rolls back and rethrows. Section 12. | `ActorCode` and `CreatedByCode` are stored in full or the attempt commits nothing. |
| `CreateTicket` `Created` row | Current literals fit: `Created`, `Pending Approval` or `New`, and a ticket number of at most 10 characters once Gate B's formatter is in place. An actor code longer than 50 fails at `SaveChanges` and rolls back. It does not return `Fail`. | No silent cut. |

`ApproveTicket` as called by `btnApprove_Click` passes null comments. That path stores null and does not fail the length check.

#### 4.3.3 Messages

| Condition | Message |
|---|---|
| Rejection reason over 500 | `Rejection reason must be 500 characters or fewer.` |
| Approval comment over 500 | `Approval comment must be 500 characters or fewer.` |
| Override reason over 500 | `Priority override reason must be 500 characters or fewer.` |
| New resolution over 1000 | `Resolution must be 1000 characters or fewer so the audit row can store it.` |
| Public comment over 1000 | `Comment must be 1000 characters or fewer.` |
| Internal note over 989 user characters | `Internal note must be 989 characters or fewer because the audit detail includes an 11-character prefix.` |
| Actor code over 50 | Withdrawn. This design does not return that `Fail`. `SaveChanges` validation fails and the transaction rolls back (section 12). |
| Backstop, not shown by `ApplyResult` | `Audit field {name} length {n} exceeds {max}.` |

### 4.4 Output encoding required before A1 is complete

Encode with `HttpUtility.HtmlEncode` at the assignment, each component before concatenation. Do not encode a string that was already encoded. Do not build a CSS class from encoded text.

| Sink | Location | Rule |
|---|---|---|
| `lblTenantBadge` | `TicketDetail.aspx.cs` line 231 | Encode the company name. The numeric fallback and the constant `Tenant not recorded` have no stored text. |
| `lblTicketNumber` | line 233 | Encode `dto.TicketNumber`. The `#` prefix is a constant. |
| `lblTitle`, `lblRequestType`, `lblRequester`, `lblAssignee`, `lblBranch` | lines 234–237 and 241 | Encode each stored string. |
| `lblCategory` | lines 238–240 | Encode category, subcategory, and item separately, then concatenate with the constant separators. |
| `lblImpactUrgency` | line 242 | Encode each name, then concatenate with the constant separator. |
| `lblDescription` | lines 244–246 | Encode the description. The empty constant stays a constant. |
| `lblResolution` | line 249 | Encode the resolution. Do not truncate it. |
| `lblStatusBadge.Text` | line 251 | Encode the visible name. |
| `lblStatusBadge.CssClass` | lines 252–253 | Whitelist from the raw status name. |
| `lblPriorityBadge` | line 254 | Encode the visible name. |
| `lblCsatScore` | line 259 | The score is a byte rendered as a number. Encoding the formatted number is harmless and is included so the branch has no raw `Label.Text` assignment. |
| `lblCsatFeedback` | line 260 | Encode `dto.CsatFeedback`. The empty constant stays a constant. |
| `CreatedByName` | `TicketDetail.aspx` line 361 | Encode. |
| `ApprovalLevel`, `ApproverName` | lines 381–382 | Encode. |
| Approval status text | line 386 | Encode the visible text. The class attribute stays the closed set `success`, `danger`, `warning`. |
| Comment text and approval comments | lines 367 and 394 | Already encoded. Leave them. |
| `lblMessage` | `TicketDetail.aspx.cs` line 393 | Encode. This covers `ApplyResult` and every `ShowMessage` caller on the page. The clear at line 388 is `string.Empty`. |
| Ticket-number anchor | `TicketList.aspx` line 141 | Encode the visible number. `Eval("Id")` in the href is an integer and stays the route value. |
| `lblPriorityBadge` | `TicketList.aspx` line 151 | Encode the visible name. |
| `lblStatusBadge` | `TicketList.aspx` line 157 | Encode the visible name. |
| List badge classes | `TicketList.aspx.cs` lines 144–168 | Read the raw priority name and the raw status name from the data item. Do not read `Label.Text` after encoding. Status classes use the same whitelist as the detail page. |
| `lblMessage` | `TicketList.aspx.cs` line 336 | Encode. This covers the queue-load catch at line 301, which concatenates `ex.Message`. The clear at line 331 is `string.Empty`. |
| `lblTicketCount` | line 296 | A count plus a constant. Not a stored-text sink. No change. |
| `lblMessage` | `CreateTicket.aspx.cs` line 454 | Encode. This covers `ex.Message` at lines 208 and 215. The clear at line 449 is `string.Empty`. `CreateTicket.aspx` has no other label. |
| `lblCurrentItHead` | `ApprovalConfig.aspx.cs` lines 126–127 | Encode the code and the name separately, then concatenate with the constant separator. |
| `lblCurrentItHeadStatus` | line 128 | Encode. |
| `lblMessage` | `ApprovalConfig.aspx.cs` line 155 | Encode. This covers `ex.Message` at lines 80 and 106. The clear at line 150 is `string.Empty`. |
| Notification body | `TicketService.cs` line 478 | Html-encode the ticket number in A1. The title at line 479 stays encoded. |
| Notification subject | line 502 | Plain-text subject. Remove CR, LF, and other control characters from the number, and encode `&`, `<`, `>`, `"`, and `'`. Do not Html-encode characters that are not markup and are not controls. |
| `BoundField` columns | Ticket list, `gvAudit`, ApprovalConfig grids | Framework encoding. Do not change. |
| `lblActive` | `ApprovalConfig.aspx` lines 123–125 | Text is the constant `Yes` or `No`. Class is chosen from a boolean. Not a stored-text sink. |
| `ListItem` text | Status, agent, priority, and employee dropdowns | Framework encoding. Do not change. |
| Internal-comment marker | `TicketDetail.aspx` lines 358 and 363–364 | The colour and the word `Internal` are chosen from a boolean. Not stored text. |

`itsm-prewrap` on the description and resolution containers remains as Revision 2 specifies it. Encoding preserves the newline characters; the CSS preserves the breaks.

### 4.5 Dispatch ordering sentence

`OrderBy(m => m.Id)` fixes the order of one call against the rows present for that call. It does not freeze an assignment. A test must not assert that the same assignee is returned after a mapping row is inserted, deleted, or deactivated. Empty-agent fall-through stays behind its unread count, as Revision 2 AR-3 states.

### 4.6 What this design still does not change

The transition map. Who may resolve, reopen, or close (D-2, D-6). The approval order checks. `CanProcessTicket`. `ResolveInitialStatusId`'s fail-open fallback. Queue company visibility and the description search (D-1, AR-16). Session keys. No status insert. No SLA column. No anti-forgery token. No rewrite of historical ticket numbers or historical audit rows.

---

## 5. Gate dependency matrix

No row is an authorisation. "Specified" means the design text is complete enough to review. It does not mean the gate is open.

| Gate | Technical state after this reconciliation | Still required before the gate could be considered | Authorised now |
|---|---|---|---|
| A1 | The sink list in section 4.4 names every output the second review identified, the four `lblMessage` assignments, the class derivation from raw values, the C-27 sentence, and the Y-15 notification encoding. Panel bindings, `OrderBy`, and the guarded rollback remain as Revision 2 specifies them. | A change authorisation that names A1, names section 4.4, and carries the AR-9 / Z-4 disclosure: no anti-forgery token, no owner, no date, ViewState MAC unread. Empty-agent fall-through still waits on its count. | **No.** |
| A2 | The resolution path is backward-compatible for a historical over-length value. The audit contract is one contract, including `ActorCode`. The checked string is the stored string. The page states the 1000-character maximum. A2 promotion requires an active non-terminal `Resolved` row. | Change authorisation. Section 8 acknowledgements, informed by reads 19–21. Read 1's `Resolved` flags. D-17 stays Pending. Preference, not a technical prerequisite: ship A1 first so refusal text is encoded. | **No.** |
| A3 | Unchanged behaviour: fail closed only after read 1 passes, after `RequireRecordedTenant`, with no status insert. Read 1 is narrowed to one result per approval-flag pair. | The read does not exist. Until it is attached and passes, A3 stays disabled. A failed read is not a reason to enable the branch. | **No.** Disabled. |
| B | The sequence rule, the fold, the single-row precondition, the retry probe, the format ban on negative and non-numeric results, and the allocator-before-index order in section 12 are specified. The previous index-before-allocator order is withdrawn. | Schema mechanism (AR-14). Reads 4a, 4b, 4c, 5, 6, 7, 8, 9, 10, 11, and 22. Shared-sequence acknowledgement. Named timeout. Named canonical case. Owner acceptance of each prefix the exact-2 rule blocks, including a prefix longer than two characters. D-4 and D-9 stay Pending. | **No.** |
| C | No behaviour in A1, A2, A3, or B depends on a Gate C ruling. | A written owner ruling in the register for each decision. None exists. | **No.** All 18 Pending. |

| Item | Depends on | State |
|---|---|---|
| A1 encoding and panels | Change authorisation naming section 4.4, plus the AR-9 disclosure | Not granted |
| A1 empty-agent fall-through | Blank-agent count. D-14 Pending | Not granted |
| A2 lengths, audit contract, resolution preservation | Section 8 acknowledgements and reads 19–21. A1 is not a technical prerequisite. Release preference is A1 then A2 | Not granted |
| A3 | Read 1 passes for the named environment, with the Y-13 grouping and the Y-10 `Resolved` flags | Not granted. Read not run |
| Gate B | Section 4.1, the reads in section 7, named case, named timeout, schema mechanism, prefix acceptance | Not granted |
| Gate C | Register rulings | All 18 Pending |
| Anti-forgery token | A security change outside these gates | Owner not named |
| F-09 / F-10 reports | A read authorisation that names the environment | Not granted |

---

## 6. Updated test matrix

### 6.1 Preserved cases

TC-01 through TC-59 are unchanged from `ITSM_TECHNICAL_REMEDIATION_DESIGN_REV2.md` section 10. Their expected results, their needs, and their disposition **NOT RUN** are preserved. This reconciliation did not execute any of them.

One list correction, from Y-16: tests that need an owner decision before they can accept an enabled rule are TC-06, TC-08, TC-17, TC-32, TC-48's fail-closed branch, TC-63, and TC-68. TC-32 was missing from Revision 2's list. It is the same class of case as TC-06.

UAT and LIVE remain excluded from every row, preserved and new.

### 6.2 New cases

Every new row is **NOT RUN**. None was executed. A later run records expected result, actual result, environment, data, evidence, and a disposition that was observed.

| Case | Covers | Expected result | Needs | Disposition |
|---|---|---|---|---|
| TC-60 | Y-1, TG-1, Z-2 | Stored ten-character values whose sequence characters are `-123`, `+123`, ` 123`, `0000`, or `12a4` do not advance the sequence and are not rewritten. The next emitted number is ten ASCII characters, the sequence is four digits, and the integer is in 1..9999. No emitted value contains a sign or a non-digit. | Disposable database. Not UAT. Not LIVE. | NOT RUN |
| TC-61 | Y-2, TG-2 | With stored `CR26080003` and `cr26080007` and canonical upper case, the next number is `CR26080008`. The insert does not raise a unique-index violation. A retry does not run. Sequences above 0008 remain available. A lone `cr26089999` refuses with exhaustion before insert and does not loop. | Disposable database. Not UAT. Not LIVE. | NOT RUN |
| TC-62 | Y-3 | A ticket-number unique violation whose message is not English still retries when the existence probe finds the attempted number, and then allocates a higher sequence. A 2601 or 2627 that is not a ticket-number collision does not retry. The predicate does not read the exception message. | Disposable database. Not UAT. Not LIVE. | NOT RUN |
| TC-63 | Y-4, TG-4, Z-1 | A fixture, not `ChangeStatus`, stores a resolved ticket whose `ResolutionText` length is greater than 1000. The page can show that text in full. Reopen leaves the text and `ResolvedOn` unchanged and adds no preservation comment. A re-resolve attempt with 1001 characters changes nothing. A re-resolve with a legal short text stores the new text, writes a public comment that ends with the exact previous characters, writes a superseded audit row with `Detail` null and the `omitted;length=` marker, and writes a current audit row whose `Detail` is the new text. No stored copy of the previous text is shorter than the original. | Disposable database. Preservation acknowledgement not recorded, so this cannot accept an enabled rule. Not UAT. Not LIVE. | NOT RUN |
| TC-64 | Y-5, TG-3, Y-14 | An over-length rejection reason, approval comment, override reason, public comment, internal note, and actor code each return `Fail` or, for the actor-code backstop if forced past the caller, roll back. No ticket column, approval column, comment, or audit row is committed. `ActorCode` is not truncated. A 100-character status name remains TC-37 and is unchanged. | Disposable database. Not UAT. Not LIVE. | NOT RUN |
| TC-65 | Y-6 | An approval comment of 500 characters plus trailing spaces is stored trimmed to 500 and succeeds. A trimmed comment of 501 characters returns `Fail` and changes nothing. Null comments still approve. | Service call. Not UAT. Not LIVE. | NOT RUN |
| TC-66 | Y-7, TG-5 | The list anchor text, the priority label, and the status label render `<`, `>`, and `"` as text. Badge classes still follow the raw names. | Rendered page. Site was not started. | NOT RUN |
| TC-67 | Y-8 | A company name, a CSAT feedback value, an IT Head code, an IT Head name, and an IT Head status that contain `<` render as text. | Rendered page. Site was not started. | NOT RUN |
| TC-68 | Y-9, C-18 | The resolution label states the 1000-character maximum before the user types. Submitting 1001 characters is still refused by the server. | Rendered page, after the section 8 acknowledgement. Not an acceptance test before that record. | NOT RUN |
| TC-69 | Y-10 | The named-environment attachment shows whether `Resolved` is present, active, and non-terminal. If it is not, A2 is not promoted. The seed is not accepted as the attachment. | Named-environment read. No row update. | NOT RUN |
| TC-70 | Y-11 | The repository writer list stays limited to `TicketService.CreateTicket`, and a recorded search of the other `DbConn` applications and of SQL Agent jobs reports any other writer. A writer that does not take the lock blocks Gate B. | Source inspection plus an environment record. The environment record was not made. | NOT RUN |
| TC-71 | Y-12 | A request type whose trimmed prefix length is greater than 2 is refused. No ticket is persisted. The prefix row is not substring-corrected by the application. | Disposable database. Owner acceptance or a data correction is separate. Not UAT. Not LIVE. | NOT RUN |
| TC-72 | Y-13 | Two active request types with the same two approval flags produce one `ResolveInitialStatusId` result in the read, and the read names both types. | Inspection of the read attachment, or a disposable copy. | NOT RUN |
| TC-73 | Y-15 | The notification HTML body contains the encoded ticket number. The subject contains no raw `<`, `>`, `&`, quote, or control character from the number. | Disposable database. Not UAT. Not LIVE. | NOT RUN |
| TC-74 | TG-6, Z-3 | After a successful create, a second create in the same prefix and month acquires the lock without waiting out the named timeout. The first transaction does not hold the lock after commit. | Disposable database, two sessions, named timeout. The timeout is not named, so this cannot accept an enabled rule. Not UAT. Not LIVE. | NOT RUN |
| TC-75 | C-27 | Two calls against unchanged mapping rows return the lower `Id`. After a new active row with a lower `Id` is inserted, a later call may return the new row. The test fails if it requires the first assignee to survive that insert. | Disposable database. Not UAT. Not LIVE. | NOT RUN |
| TC-76 | Z-5 | Reads 19, 20, and 21 are attached with environment, statement, timestamp, and reviewer. No row is updated. The section 8 acknowledgement is not treated as informed without them. | Named-environment read. | NOT RUN |
| TC-77 | Z-4, AR-9 | The A1 change authorisation, when one exists, names the missing anti-forgery control, the unread ViewState MAC setting, and that A1 adds no token. Absence of that sentence means A1 is not authorised. This row is not a security-test pass. | Document checkpoint. | NOT RUN |
| TC-78 | Review §10.2, corrected by Z-8 | The corrected shared-prefix allocator is the build serving creates before the `TicketNumber` unique index is created, or both change in one window in which the previous allocator serves no create. Rollback drops the index and confirms it is absent before the previous build serves creates. A plan that leaves the previous allocator active while the index exists fails this checkpoint. The plan does not claim a seek unless a captured plan shows one. The earlier expected result, index before the new allocator, is withdrawn. | Document checkpoint. Not an index creation. Not UAT. Not LIVE. | NOT RUN |

### 6.3 Needs, restated for the new rows

Database precondition or write, and excluded from UAT and LIVE: TC-60, TC-61, TC-62, TC-63, TC-64, TC-65, TC-71, TC-73, TC-74, TC-75.

Named-environment read, with no row update: TC-69, TC-70's environment half, TC-76. TC-20, TC-49, and TC-54 remain the preserved named-environment rows.

Owner decision before the row can accept an enabled rule: TC-06, TC-08, TC-17, TC-32, TC-48's fail-closed branch, TC-63, TC-68.

Schema change required to run the test: none of TC-60 through TC-78. TC-74 and TC-61 observe Gate B objects that are not authorised to be created on UAT or LIVE. TC-13, TC-47, and TC-58 remain the preserved rows that need `ALTER` on a disposable database.

Rendered page, site not started: TC-66, TC-67, TC-68, plus the preserved rendered-page list.

---

## 7. Outstanding environment and database evidence

None of these was run. No statement in this section is an instruction to execute SQL now. A result copied from an earlier document would be a quoted record. None is copied here. A failed or missing read withholds promotion. It is never a reason to enable a branch and watch it refuse.

Revision 2 reads 1 through 18 remain outstanding, with these corrections:

| Read | Correction |
|---|---|
| 1 | Still the A3 pass condition: active `In Progress`, and active `Rejected` or `Cancelled`. Also the A2 condition: active non-terminal `Resolved`. `ResolveInitialStatusId` is recorded per distinct approval-flag pair, with the type names listed (Y-13). |
| 4 | Split into 4a, 4b, and 4c in section 4.1.8. 4a alone does not clear a single case-variant row. |
| 9 | Still required. The fold does not wait on this read for correctness. The read tells IT whether the existing column collation is case-sensitive before the index is chosen. |
| 17 | Disposable database only. UAT and LIVE excluded. |

Additional reads:

| Read | What it records | Gate it blocks |
|---|---|---|
| 19 | Ticket id and character length of every `ResolutionText` longer than 1000. No update. | A2 acknowledgement is uninformed without it. |
| 20 | Count of `TicketComments.CommentText` longer than 1000, and count longer than 989. No update. | A2 comment-capacity acknowledgement. |
| 21 | Count of `TicketAuditLog.Detail` whose length is 1000. This count is a ceiling population, not proof that a value was truncated. No update. | A2 acknowledgement. |
| 22 | Writers of `itsm.Tickets.TicketNumber` outside `TicketService.CreateTicket`, including HRMS import, VMS, and SQL Agent jobs on the shared `DbConn`. The repository search in this file is the source half only. | Gate B. |

`AnmolDristi/Web.config` was not read. Request-validation mode, `customErrors`, and the ViewState MAC setting remain unknown.

---

## 8. Owner acknowledgements

These are not D-19 through D-n. They are not written into the register. They are not approvals. An empty cell is not a record. Every row below is **Not recorded**.

| Acknowledgement | Why it is separate from a closed decision | Who would have to record it | State |
|---|---|---|---|
| A new resolution longer than 1000 characters will be refused, and the page will say so | D-17 asks for the minimum. The column is unbounded today. | Service owner. IT leadership for the audit ceiling, as the register names that role for D-17's audit copy. D-17 stays Pending. | Not recorded |
| A historical resolution longer than 1000 characters is kept in full, copied to a public comment on re-resolve, and not truncated into `Detail` | This is the Y-4 compatibility path. It is not a data-repair programme and it does not close D-17. | Service owner, because a public comment appears on the ticket. IT leadership, because `Detail` is null on that superseded row. | Not recorded |
| Public comments move from a 2000-character hint to 1000, and internal notes to 989 user characters. The preservation comment is not subject to that cap. | No D-id owns the comment box. D-5 stays Pending. | Service owner. | Not recorded |
| The population counts from reads 19, 20, and 21 were shown before the two capacity acknowledgements were signed | An acknowledgement without the counts is Z-5. | The same owners, after the reads exist. | Not recorded. Reads not run. |
| Two request types that share a prefix will share one monthly sequence and one 9999 ceiling | D-4 and D-9 stay Pending. | IT leadership for the method. Service owner for the catalogue. | Not recorded |
| A request type whose trimmed prefix is not exactly two ASCII letters or digits will be unable to create tickets, including a prefix longer than two characters | Y-12. The exact-2 rule removes today's substring behaviour. | Service owner to accept the block or to authorise a data correction. | Not recorded |
| Canonical case, and the lock timeout in milliseconds | Technical parameters. Naming them is not a D-4 ruling. | IT. | Not selected |
| Case-variant ticket numbers (read 4b), including a single row, are rewritten or explicitly accepted before Gate B | The allocator is safe if one remains. Enablement still requires a decision. | IT / DBA for the rewrite. Service owner if displayed numbers change case. | Not recorded |
| Empty `SupportAgentCode` will fall through only after its count is known | D-14 stays Pending. | IT, and the service owner if the count is not zero. | Not recorded |
| `ResolveInitialStatusId` continues to fail open | A3 does not change it. | No decision requested by A3. | No decision requested |
| Schema mechanism for the new indexes | EF migration or standalone script. | IT and DBA. | Not selected |
| Anti-forgery owner and date | Outside this design. This file does not choose a person. | Not named. | Not assigned |
| Other writers of `TicketNumber`, if read 22 finds any | The lock does not cover them. | IT / DBA. | Not recorded |

---

## 9. D-1 through D-18 status

Source: `ITSM_BUSINESS_DECISION_REGISTER.md` section 10, read on 24 November 2026. The Owner ruling column and the Date column are empty on every row. Status is Pending on every row. No approval email, signature, or dated ruling was present in that table. Recommendations are not approvals. This reconciliation does not change the register.

| ID | Register status | Owner ruling | Date | This reconciliation |
|---|---|---|---|---|
| D-1 | Pending | Empty | Empty | Stays Pending. AR-16 remains a residual. |
| D-2 | Pending | Empty | Empty | Stays Pending. Process rights are unchanged. |
| D-3 | Pending | Empty | Empty | Stays Pending. |
| D-4 | Pending | Empty | Empty | Stays Pending. Gate B uses the register's recommended method only as an unapproved description. The shared-sequence acknowledgement does not close D-4. |
| D-5 | Pending | Empty | Empty | Stays Pending. |
| D-6 | Pending | Empty | Empty | Stays Pending. Reopen behaviour in section 4.2 does not add a window or a clock reset. |
| D-7 | Pending | Empty | Empty | Stays Pending. No SLA column. |
| D-8 | Pending | Empty | Empty | Stays Pending. Y-15 does not choose a mail origin. |
| D-9 | Pending | Empty | Empty | Stays Pending. Shared prefixes are not a flag policy. |
| D-10 | Pending | Empty | Empty | Stays Pending. |
| D-11 | Pending | Empty | Empty | Stays Pending. |
| D-12 | Pending | Empty | Empty | Stays Pending. |
| D-13 | Pending | Empty | Empty | Stays Pending. |
| D-14 | Pending | Empty | Empty | Stays Pending. |
| D-15 | Pending | Empty | Empty | Stays Pending. Null `CompanyId` stays refused. |
| D-16 | Pending | Empty | Empty | Stays Pending. |
| D-17 | Pending | Empty | Empty | Stays Pending. The 1000-character maximum and the preservation path are acknowledgements, not a ruling on the minimum. |
| D-18 | Pending | Empty | Empty | Stays Pending. |

Section 3 and section 4.4 of the register are also Pending. They are not additional decision numbers and they are not closed.

---

## 10. Explicit implementation authorisation status

**NOT READY FOR IMPLEMENTATION.**

- This reconciliation authorises no change to any application file, SQL file, migration, index, trigger, or configuration file.
- No gate is open. A1, A2, A3, B, and C are closed.
- A1 is not complete for authorisation until a later change authorisation names section 4.4 and carries the AR-9 disclosure. This file is not that authorisation.
- A2 is not complete for authorisation until the preservation strategy and the audit contract are adopted by a change authorisation and the section 8 acknowledgements exist. This file is not that authorisation.
- A3 must not be enabled in order to observe a live status table. It stays disabled until read 1 is attached and passes.
- B must not be written. The sequence design is specified so that a later authorisation has an implementable rule. The specification is not the authorisation.
- C retains D-1 through D-18 as Pending. No recommendation in the business decision register is treated as a ruling.
- Every test disposition, TC-01 through TC-78, is **NOT RUN**. TC-86 through TC-90, added by the in-place correction in section 12, are also **NOT RUN**.
- No SQL was executed and no UAT or LIVE connection was opened to produce this file.
- `AnmolDristi/Web.config` was not read and was not modified by this work.

Revision 2 remains the proposed design this file corrects. The authorisation to implement either document is not this file.

---

## 11. Checks and limitations

### 11.1 Checks performed

- Source inspection of the files and line ranges cited above, at `HEAD` `f1f4bdb`.
- Repository search for `TicketNumber` writers and for anti-forgery APIs under `AnmolDristi/ITSM`.
- Register section 10 read for D-1 through D-18. No Approved status was present.
- `git rev-parse HEAD`, `git log -1 --oneline`, and `git status --porcelain` before this file was added.

### 11.2 Checks not performed, and therefore not reported as passes

- No build. MSBuild was not invoked.
- No test. TC-01 through TC-78 were not executed.
- No SQL. No UAT connection. No LIVE connection. No deployed row, collation, grant, job, or population count was observed.
- No page was rendered. Encoding requirements are source requirements, not a demonstrated page.
- `AnmolDristi/Web.config` was not read.
- The `D4` sign behaviour and `int.TryParse` style flags are the published .NET contracts of the APIs named in source. They were not re-executed in a harness for this file.
- No approval of any kind is implied by a confirmed line number.

---

## 12. In-place corrections — Y-26, Y-27, Z-8

**Date:** 25 November 2026. This section is part of this file. It is not a new reconciliation and it grants no implementation authorisation. Gates A1, A2, A3, B, and C stay closed. D-1 through D-18 stay Pending. Every test named here is **NOT RUN**. The same rules are in `ITSM_TECHNICAL_REMEDIATION_DESIGN_REV2.md` §16 and in `ITSM_FREEBUFF_REMEDIATION_RECONCILIATION_REV3.md`.

### Y-26 — Actor length matches EF6

`TicketComment.CreatedByCode` is `[Required]` `[StringLength(50)]`. `TicketAuditLog.ActorCode` is `[Required]` `[StringLength(50)]`. `Ticket.ModifiedByCode` is `[StringLength(50)]`. `ItsmDbContext` does not set `ValidateOnSaveEnabled`. No ITSM update or create method returns `Fail` when the actor string is longer than 50. That was checked in `TicketUpdateService.cs` and `TicketService.cs` on 25 November 2026.

The row in §4.3.2 that returned `Fail` with `Actor code exceeds the 50-character audit limit.` before the context opened is withdrawn. This design does not add that validation. The deterministic behaviour is: the actor string is assigned as the code assigns it today; `SaveChanges` fails EF6 validation; the existing `catch` rolls the transaction back and rethrows; no ticket column, comment, or audit row from that attempt commits. The result is not `TicketUpdateResult.Fail`. The validation exception was not executed in a harness. TC-89 is the test. The user-text `Fail` checks in §4.3.2 for resolution, comment, rejection, approval, and override remain, because those checks are explicit design additions. Actor length is not one of them.

### Y-27 — Exact `OldValue` for a `Current` resolution row

Every `Resolution` audit row with `NewValue` `Current` persists this `OldValue` and no other characters:

```text
current;resolvedOn={stamp}
```

`{stamp}` is the transition's single `now` from `DateTime.ToString("o", CultureInfo.InvariantCulture)`, the same value stored in `ticket.ResolvedOn`. `ResolvedOn` is included. `ModifiedByCode` is not part of this marker. `Detail` is the new trimmed resolution and stays within 1000 characters. A marker that would exceed 200 characters returns `Fail` and writes nothing. The superseded row keeps `superseded;length={n};resolvedOn={stamp};modifiedBy={code}` and is not rewritten into the `Current` format. TC-90 asserts the persisted `Current` value. NOT RUN.

### Z-8 — Deployment and rollback order

1. Confirm read 4a is zero under the index collation, or the colliding rows have been rewritten or deleted and the count repeated at zero. This read was not run.
2. Deploy the corrected shared-prefix allocator while the unfiltered unique index is absent, or perform the deploy and the index creation in one maintenance window during which the previous per-request-type allocator accepts no create.
3. Create the unfiltered unique index only after that allocator is the one that will serve the next create.
4. Do not leave the previous allocator running after the index exists. The previous allocator filters by `RequestTypeId` and can attempt a number the shared sequence already stored.
5. Rollback, with ticket creation stopped for the whole sequence: `DROP INDEX`, confirm the index is absent, and only then restore the previous build and allow creates. Rows stay. If the drop fails, the previous allocator stays stopped.
6. Collision validation is TC-88 on a disposable database: a pair that the previous allocator can duplicate blocks index creation, and the rollback check refuses to enable the previous allocator while the index is present. Not UAT. Not LIVE. NOT RUN.

Gate B is not authorised by this order. D-4 stays Pending.
