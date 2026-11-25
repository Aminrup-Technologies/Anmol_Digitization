# ITSM FreeBuff remediation reconciliation — revision 3

**Document:** ITSM_FREEBUFF_REMEDIATION_RECONCILIATION_REV3.md
**Date:** 24 November 2026
**Type:** Documentation-only reconciliation of the third remediation review. Not an approval. Not an implementation authorisation.
**Required verdict:** **NOT READY FOR IMPLEMENTATION**

This file is the design text a later authorisation would have to adopt for the findings below. It does not apply that text to code, SQL, or configuration.

**25 November 2026.** Y-26, Y-27, and Z-8 are closed in this file and in the two documents it corrects: `ITSM_TECHNICAL_REMEDIATION_DESIGN_REV2.md` and `ITSM_FREEBUFF_REMEDIATION_RECONCILIATION_REV2.md`. No further revision file was created. The verdict remains **NOT READY FOR IMPLEMENTATION**.

No gate receives implementation authorisation from this file.

---

## 1. Executive verdict

**NOT READY FOR IMPLEMENTATION.**

The third review's blocking contradiction is resolved here as specification text. There is one retry rule. It detects SQL duplicate-key errors by `SqlException.Number` 2601 or 2627, never reads `SqlException.Message`, never depends on an index-name string, and retries only when an existence probe confirms that the attempted ticket number is already stored. Unrelated constraint violations propagate. The attempt limit is three. Exhaustion throws a controlled message after rollback and commits nothing.

Y-18 through Y-25 are each given a disposition below. The Gate B enablement sequence is one sequence. The omitted `CreateTicket` output assignments are restored and classified. The preservation comment names `TicketId` and `CreatedOn`. The null-prefix and longer-prefix story is qualified against the tracked `char(2)` definition, and the deployed column is unverified. Exception redaction is specified separately from HTML encoding. The three precision items are tied to the source that was re-read, and the tests that assert them are updated.

Acceptance of these corrections is not permission to build them.

| Gate | Authorised by this document |
|---|---|
| A1 | **No.** |
| A2 | **No.** |
| A3 | **No.** Disabled until read 1 is recorded and reviewed. |
| B | **No.** |
| C | **No.** D-1 through D-18 remain Pending. |

Every test, TC-01 through TC-90, is **NOT RUN**. No test was executed to produce this file.

Working tree inspected: `HEAD` `f1f4bdb2c82013e913ac682aa5e31832a3fa94fc` (`f1f4bdb`). Line numbers below were read from that tree on 24 November 2026. They are not a certified baseline and not a deployed-database result. The Sales TA/DA tags in `docs/INDEX.md` are not an ITSM baseline and were not used.

---

## 2. Source and review baseline

### 2.1 Documents read

Read for this reconciliation, and not modified:

| Document | Role |
|---|---|
| `ITSM_FREEBUFF_REMEDIATION_REVIEW_REV3.md` | Third review. Raises Y-17 (the Y-3 contradiction) and Y-18 through Y-25. |
| `ITSM_FREEBUFF_REMEDIATION_RECONCILIATION_REV2.md` | Second reconciliation. §4.1.5 is the retry rule that forbids message inspection. §4.1.7 and §4.1.8 disagree on read 4a. §4.2.4 omits `TicketId` and `CreatedOn`. §4.4 is the A1 sink list that omits the `CreateTicket` group. TC-62. |
| `ITSM_TECHNICAL_REMEDIATION_DESIGN_REV2.md` | Proposed design. AR-13 and Gate B still require the ticket-number index name. TC-52 still expects that name. A1 says record and do not change the `CreateTicket` `InnerText` / `Attributes` rendering. |
| `ITSM_FREEBUFF_REMEDIATION_REVIEW_REV2.md` | Second review. Y-3 is the blocking finding that the index name is taken from `SqlException.Message`. |
| `ITSM_SOURCE_TO_FINDING_TRACEABILITY.md` | Source map for F-01 and the related allocation facts. |
| `ITSM_BUSINESS_DECISION_REGISTER.md` §10 and §11 | D-1 through D-18. Read on 24 November 2026. |

`ITSM_TECHNICAL_REMEDIATION_DESIGN_REV2.md` was not edited. `ITSM_FREEBUFF_REMEDIATION_RECONCILIATION_REV2.md` was not edited. Where those two disagree, this file states the rule that replaces the disputed sentence. Both earlier sentences remain in their own files as history.

### 2.2 Source re-read

Re-read on 24 November 2026 at `HEAD` `f1f4bdb` for the disputed claims:

| Claim | Source read | Result |
|---|---|---|
| Retry texts disagree | Design REV2 §6 AR-13, §7 Gate B retry paragraph, §10 TC-52. Reconciliation REV2 §4.1.5 and §6.2 TC-62. | Confirmed. AR-13 and TC-52 require the index name. §4.1.5 and TC-62 forbid reading the message. |
| `CreateTicket` output assignments | `CreateTicket.aspx.cs` lines 320–412 and 262; `CreateTicket.aspx` lines 539, 545, 566, 576, 743, 745. | The four `InnerText` assignments and the two `aria-label` assignments named by the third review are present. Two further `aria-label` assignments of the same shape are present at lines 367 and 397. |
| Preservation comment fields | `202608120550515_InitialItsmSchema.cs` lines 166–181; `TicketComment.cs`; `TicketUpdateService.cs` `AddComment` lines 489–496 and `AddAudit` lines 602–621; `ChangeStatus` lines 93–123; `TicketDetailService.cs` lines 186–206. | `TicketId` and `CreatedOn` are required. `AddComment` is the method that assigns both today. §4.2.4 does not assign them. Comments are ordered by `CreatedOn`. |
| Prefix definition | `RequestType.cs` lines 17–28; migration lines 236–243; `TicketService.cs` lines 364–368. | Tracked `Prefix` is required `char(2)`. The null-prefix and longer-prefix branches are in the C# and are not values that column can store. |
| Exception text shown to users | `CreateTicket.aspx.cs` lines 203–222 and 452–455; `TicketList.aspx.cs` line 301; `ApprovalConfig.aspx.cs` lines 80 and 106; `CreateTicketDiagnostics.cs` lines 206–214. | Five assignments concatenate `ex.Message`. The generic `CreateTicket` catch uses a sentence that contains no exception details. |
| `TicketNumber` length | Migration line 78: `varchar(10)`, `unicode: false`, not null. Reconciliation REV2 §4.1.2 uses `LEN(TicketNumber) = 10`. | The `LEN` predicate is the text under review. No stored row was read. |
| `OldValue` shapes | Design REV2 §7 A2 re-resolve sentence. Reconciliation REV2 §4.2.3 and §4.2.4. `TicketAuditLog.cs` lines 47–51: `OldValue` max 200. `Ticket.cs` lines 133–139: `ModifiedByCode` max 50. | Two shapes are specified for the same field. |
| Register | `ITSM_BUSINESS_DECISION_REGISTER.md` lines 564–590. | Owner ruling empty, Date empty, Status Pending, on D-1 through D-18. Section 3 and Section 4.4 are also Pending. |

### 2.3 Evidence labels

| Label | Meaning |
|---|---|
| Source-verified | Read in the cited file on 24 November 2026 at `HEAD` `f1f4bdb`. |
| Quoted record | A sentence copied from an earlier document. Not re-certified by this file. |
| Framework or engine contract | Published behaviour of ASP.NET, ADO.NET, EF6, or SQL Server named in the text. Not re-executed in a harness, and not observed on UAT or LIVE. |
| Environment-unread | A deployed row, collation, column definition, grant, job, or population count. Not queried. `AnmolDristi/Web.config` was not read. |
| Not granted | No owner ruling, no change authorisation, and no environment attachment exists. |
| NOT RUN | The test was not executed. |

### 2.4 What this reconciliation did not do

It did not execute SQL, open a UAT or LIVE connection, build the solution, run a test, or start a site. It did not read `AnmolDristi/Web.config`. It did not modify application files, SQL, migrations, configuration, or any input review. It did not commit or push. It did not mark a test passed. It did not treat a register recommendation as an approval. It did not invent a deployed collation, a deployed prefix definition, a population count, or an owner name.

---

## 3. Y-3 / AR-13 / TC-52 / TC-62 reconciliation

### 3.1 The contradiction

Four texts describe one retry. They do not say the same thing.

| Text | What it requires | Status in this reconciliation |
|---|---|---|
| Second review Y-3 | The predicate must not depend on the index name inside `SqlException.Message`, because SQL Server renders that message in the instance language. Blocking for Gate B. | Accepted. Not downgraded. |
| Reconciliation REV2 §4.1.5 and TC-62 | Retry only when `Number` is 2601 or 2627 and a new-context probe finds the attempted number. Do not read `SqlException.Message`. | Accepted as the direction. Restated in §5.1 with exhaustion, propagation, and rollback made explicit. |
| Design REV2 AR-13 | "Intermediate retries run only when the guarded rollback preserved 2601 or 2627 and the index name is the ticket-number index." | Historical. The index-name clause is withdrawn. AR-13's identifier is kept. |
| Design REV2 §7 Gate B | The same index-name predicate, and "Any other 2627 is not retried." | Historical. Replaced by §5.1. The sentence "any other 2627 is not retried" survives only as the probe result, not as a name match. |
| Design REV2 TC-52 | "The retry predicate observes `SqlException` 2601 or 2627 and the ticket-number index name. When `Rollback()` throws, the original exception is still the one observed." | Historical expected result withdrawn. The AR-6 half is kept. The corrected expected result is in §7. |

The second reconciliation listed the AR corrections it applied and did not name AR-13. It preserved TC-01 through TC-59 unchanged, including TC-52, and added TC-62 with the opposite discriminator. An implementer who follows AR-13 or the historical TC-52 rebuilds the defect Y-3 removed. This file is the correction the third review required. The input documents stay as they are.

### 3.2 The single rule

§5.1 is the only retry rule. In summary:

1. On `DbUpdateException`, take the innermost `SqlException`.
2. Duplicate-key detection uses `Number` 2601 or `Number` 2627. No other signal qualifies.
3. Do not read `SqlException.Message`. Do not search for an index name. Do not compare an index-name string.
4. When the number is 2601 or 2627, an existence probe on a new context determines whether a stored `TicketNumber` is the attempted value, by ordinal equality or by the ASCII fold of all ten characters compared ordinally.
5. Retry only when that probe finds the attempted number. That is the confirmation that the duplicate concerns ticket-number allocation.
6. Any other exception, and a 2601 or 2627 whose probe does not find the attempted number, is an unrelated constraint violation. Do not retry. Propagate the original exception. Do not replace it with the allocation message.
7. The guarded rollback captures the original exception, attempts `Rollback()`, ignores only a failure of that rollback, and rethrows the original. A throwing `Rollback()` must not mask the exception the predicate has to see.
8. The limit is three attempts. The third confirmed ticket-number collision does not start a fourth attempt. It throws `InvalidOperationException` with a controlled sentence, after the failed attempt has rolled back, and it commits no ticket, audit, approval, or notification row.

### 3.3 Relationship of TC-52 and TC-62

Both identifiers remain. Neither is deleted. TC-52 is not superseded.

| Test | Owns | Does not own |
|---|---|---|
| TC-52 | AR-6. After a probe-confirmed ticket-number violation, the predicate observes `Number` 2601 or 2627 and does not read `Message`. When `Rollback()` throws, the original exception is still the one observed. | It does not require an index name. It is not the case that proves a non-English message or an unrelated 2627. |
| TC-62 | Y-3. A confirmed ticket-number collision retries even when the message is not English, then allocates a higher sequence. A 2601 or 2627 whose probe does not find the attempted number does not retry and propagates. The third confirmed collision throws the controlled exception and commits nothing. The predicate does not read the message. | It does not inject a throwing `Rollback()`. That injection stays on TC-52. |

The two expected results can both pass. The historical TC-52 sentence that requires the index name is a quoted record of Design REV2 §10. It is not the expected result.

TC-26 remains the outcome case for a probe-confirmed collision that exhausts the three attempts: controlled `InvalidOperationException`, and no ticket, audit, approval, or notification row. Its historical wording "a unique-index violation" is narrowed in §7 so that an unrelated unique violation is TC-62's propagation case, not TC-26's controlled message.

### 3.4 What stays blocked

The rule is specified. Gate B is not authorised. D-4 stays Pending. No server language was read. The probe does not need a language precondition. The lock timeout, the canonical case, the schema mechanism, and the precondition reads remain outstanding (§8).

---

## 4. Y-18 through Y-25 disposition

### Y-18 — One enablement sequence for read 4a

| Field | Record |
|---|---|
| Finding | Reconciliation REV2 §4.1.7 says read 4a must be zero before the index is created, otherwise a case-insensitive index cannot be built. §4.1.8's enablement cell says "Zero groups, or a separate data authorisation." |
| Source | Quoted record of those two cells. The index "uses the column collation" is the reconciliation's own sentence in §4.1.7. Read 9, which would name that collation, is environment-unread. No deployed collation is asserted. |
| Verdict | **Accepted.** The two cells are incompatible. A data authorisation that leaves colliding rows in place does not make `CREATE UNIQUE INDEX` succeed when the index collation treats those values as equal. A case-sensitive column collation does not treat case variants as one key, so a `Latin1_General_CI_AS` group count is not, by itself, the count for that index. |
| Rule | §5.2 is the only 4a sequence. |
| Gate | B. Not authorised. |
| Test | TC-25's expected result is extended. TC-80 records read 9. TC-83 is the document checkpoint for the single sequence. All NOT RUN. |

### Y-19 — `CreateTicket` output assignments restored

| Field | Record |
|---|---|
| Finding | Reconciliation REV2 §4.4, presented as the A1 enumeration, omits the group Design REV2 §7 A1 says to record and not change. |
| Source-verified | `CreateTicket.aspx.cs`: line 331 `btn.InnerText = impact.Name`; line 350 `btn.InnerText = urgency.Name`; line 376 `name.InnerText = cat.Name`; line 406 `name.InnerText = rt.Name`. `Attributes["aria-label"]` at line 330 (`"Select impact: " + impact.Name`) and line 349 (`"Select urgency: " + urgency.Name`). The same attribute shape is also present at line 367 (`"Select category: " + cat.Name`) and line 397 (`"Select request type: " + rt.Name`). The third review named two `aria-label` assignments. The source contains four. |
| Classification | The four `InnerText` assignments are stored-text sinks. The value is a master-data name. `HtmlContainerControl.InnerText` encodes on assignment. That encoding is a framework contract and was not re-executed. The four `aria-label` assignments are attributes. Each value is a controlled literal prefix plus a stored name. They are not `Label.Text` sinks. Attribute encoding is the framework writer's contract and was not re-executed. The prefix does not make the stored name a controlled value. |
| Also recorded, unchanged | Lines 372 and 402 assign `InnerHtml` to a constant icon string. `onclick` and `onkeydown` concatenate integer ids. `CreateTicket.aspx` lines 539, 566, 743, and 745 assign constant strings to `innerHTML`. Lines 545 and 576 assign stored names with `o.text` on an option created by `document.createElement`. `hfCategoryData.Value` at line 262 receives `JavaScriptSerializer` output into a `HiddenField`. These rows are reviewed and are not to be rewritten. |
| Rule | §5.3. Record and do not change. Do not switch `InnerText` to `InnerHtml`. Do not encode a second time on top of `InnerText`. |
| Gate | A1. Required before A1 can be called complete. A1 is not authorised. |
| Test | TC-82. NOT RUN. |

### Y-20 — Preservation comment `TicketId` and `CreatedOn`

| Field | Record |
|---|---|
| Finding | Reconciliation REV2 §4.2.4 step 1 names `IsInternal`, `CreatedByCode`, and `CommentText`. It does not name `TicketId` or `CreatedOn`. It says the row is not submitted through `AddComment`. |
| Source-verified | Migration lines 166–181: `TicketId` not null, foreign key to `itsm.Tickets`; `CommentText` not null and unbounded; `CreatedByCode` not null, max 50, foreign key to `dbo.MST_UserMaster`; `CreatedOn` `c.DateTime(nullable: false)`; `IsInternal` not null. `TicketComment.cs` matches those requirements. `AddComment` (lines 489–496) is the code that assigns `TicketId` and `CreatedOn = DateTime.Now`. `AddAudit` (lines 611–620) assigns `TicketId = ticket.Id` and `ActionedOn = DateTime.Now`. `TicketDetailService.cs` lines 186–206 load comments for the ticket, hide internal comments from a viewer who cannot add an internal note, and order by `CreatedOn` descending. |
| Contract note | No `datetime2` reference was found under `AnmolDristi/ITSM`. EF6 maps `c.DateTime()` to SQL Server `datetime` unless a convention overrides it. None was found. An unassigned `DateTime` property is `0001-01-01`. SQL Server `datetime` accepts values from 1753-01-01. That range is an engine contract. This reconciliation did not execute the insert. Omitting `CreatedOn` therefore supplies a value the tracked column type rejects, and the existing `catch` rolls the transition back. Omitting `TicketId` supplies 0, which is not the ticket being updated and fails the foreign key. |
| Rule | §5.4. The preservation path assigns `TicketId` to the loaded ticket and `CreatedOn` to the transition's single `now`. Audit `ActionedOn` on the rows written by that transition uses the same `now`. The previous resolution characters are copied in full. |
| Gate | A2. The owner acknowledgements in §9 are not recorded. A2 is not authorised. |
| Test | TC-63 expected result is corrected. TC-85 covers visibility for a viewer who cannot add an internal note. Both NOT RUN. |

### Y-21 — Prefix null and length against the tracked column

| Field | Record |
|---|---|
| Finding | Reconciliation REV2's Y-12 row says a null prefix and a code of three or more characters allocate today, and §4.1.1 rule 2 treats that path as covered by the exact-2 rule in the same terms as a trimmed length of 0 or 1. |
| Source-verified | `RequestType.Prefix` is `[Required]`, `[StringLength(2)]`, `[Column(TypeName = "char")]` (`RequestType.cs` lines 25–28). The migration declares `Prefix` `nullable: false`, `maxLength: 2`, `fixedLength: true`, `unicode: false` (line 243). `Code` is `[Required]`, `[StringLength(10)]`, and `maxLength: 10` (entity lines 17–18, migration line 241). `GenerateTicketNumber` (lines 364–368) evaluates `requestType.Prefix ?? requestType.Code ?? "XX"`, trims, and if the length is greater than 2 takes `Substring(0, 2)`. |
| Verdict | **Qualified.** The C# branches exist. They are not reachable from a row that matches the tracked column. A non-null `char(2)` value is two characters before trim. Trim of a space-padded value such as `'C '` can produce length 1 or, for blanks, length 0. That short-prefix case is the reachable tracked-schema case and remains refused. A null prefix, the `Code` fallback, the `"XX"` fallback, and a length greater than 2 require an in-memory value the tracked column does not produce, or a deployed column that is nullable or wider than `char(2)`. |
| Deployed schema | **Unverified.** No catalog read was performed. Read 6 must record nullability, maximum length, and the fixed-length flag, with environment, statement, timestamp, and reviewer, before anyone treats the longer-prefix or null-prefix path as a property of a database. Until that record exists, those paths are drift guards in the application, not evidence of deployed data. |
| Rule | §5.5. Do not substitute `Code`. Do not substitute `"XX"`. Do not pad. Do not substring. Exact length 2 after trim, ASCII letters or digits only. |
| Gate | B. Not authorised. |
| Test | TC-21 remains the tracked-schema short-prefix refusal. TC-71 is restated as a drift-guard fixture and is not a claim that the tracked schema stores a longer prefix. TC-84 is the unread column-definition record. All NOT RUN. |

### Y-22 — Redaction is separate from encoding

| Field | Record |
|---|---|
| Finding | Reconciliation REV2 §4.4 says encode `lblMessage`, and says that encoding covers `ex.Message` at `CreateTicket.aspx.cs` lines 208 and 215. Encoding does not stop the message from being shown. |
| Source-verified | Line 208 assigns `ex.Message` of `InvalidOperationException` plus `" Reference: "` plus the correlation id. Line 215 does the same for `ArgumentException`. Line 222 assigns `GetUserFacingMessage()`, whose body (`CreateTicketDiagnostics.cs` lines 211–214) is a fixed sentence plus the correlation id and contains no exception details. `TicketList.aspx.cs` line 301 concatenates `ex.Message`. `ApprovalConfig.aspx.cs` lines 80 and 106 concatenate `ex.Message`. `ShowMessage` assigns `lblMessage.Text` directly (`CreateTicket.aspx.cs` lines 452–455). |
| Verdict | **Accepted**, and placed in A1 because this reconciliation's A1 requires redaction as well as encoding. The third review asked to state that closing the disclosure is outside A1. The task for this file requires A1 to include exception redaction. The rule below follows that requirement. It is still not authorised. |
| Rule | §5.6. HTML-encode the string that is displayed. Choose that string so it is not raw exception text. `customErrors` and `httpErrors` were not read and are not the control. |
| Gate | A1. No new business decision. Not authorised. |
| Test | TC-27 remains the encoding case. TC-81 is the redaction case. Both NOT RUN. |

### Y-23 — `LEN` and trailing spaces in the diagnostic counts

| Field | Record |
|---|---|
| Finding | §4.1.2 filters with `LEN(TicketNumber) = 10`. Reads 4b and 4c are written in the same length terms. |
| Source | `TicketNumber` is `c.String(nullable: false, maxLength: 10, unicode: false)` (migration line 78). `LEN` excludes trailing spaces. `DATALENGTH` on this non-unicode column counts the stored bytes, including trailing spaces. `String.Length` includes trailing spaces. Those three facts are engine and runtime contracts. They were not re-executed. No stored ticket number was read. |
| Verdict | **Accepted as a count correction.** A ten-character value that ends in a space has `LEN` 9, so the allocation predicate does not treat it as a sequence candidate. The C# rule requires length 10 and then four ASCII digits, so the same value does not advance the sequence. Allocation of a new number is unaffected. The under-count matters because 4b and 4c are evidence that gates the index. A trailing-space row omitted by `LEN` is missing from that evidence. |
| Rule | §5.7. Diagnostic counts do not use `LEN` as the definition of a ten-character value. Read 4c counts a trailing space whether or not `LEN` shortened the value. The allocation filter may continue to exclude those rows from the maximum. That exclusion is not the population count. |
| Gate | B. Not authorised. |
| Test | TC-50 remains the nine-character case. TC-79 is the ten-character value that contains a space. NOT RUN. |

### Y-24 — Where the `UPPER` agreement is anchored

| Field | Record |
|---|---|
| Finding | §4.1.1 rule 4 says the ASCII map is what SQL applies because read 6 has rejected every non-ASCII prefix. §4.1.2 applies `UPPER` to stored `TicketNumber` characters and then applies `COLLATE Latin1_General_BIN` to the result. |
| Source | Quoted record of those two sections. `UPPER` and `LOWER` use the collation of their input. A `COLLATE` on the result changes the comparison of the string `UPPER` already produced. It does not rerun the case mapping. That is an engine contract and was not executed against a server. Read 6, as specified, records active prefixes. It does not record the collation of stored ticket numbers. Read 9 is the collation read. Read 9 is environment-unread. No deployed collation is named. |
| Verdict | **Accepted.** The agreement between the C# ASCII map and SQL `UPPER` is a property of the column collation, so it belongs to read 9. Read 6 still gates the prefixes the allocator emits. A non-Latin1 collation can map a stored letter to a character the ASCII map does not produce. This file does not claim that mapping was observed. It withholds the agreement until read 9 records a Latin1 column collation. |
| Rule | §5.8. |
| Gate | B. Not authorised. |
| Test | TC-61 remains the illustration under a named canonical case. TC-80 is the unread collation record and the non-Latin1 enablement condition. NOT RUN. |

### Y-25 — One `OldValue` grammar

| Field | Record |
|---|---|
| Finding | Design REV2's fitting re-resolve stores `OldValue` as the previous `ResolvedOn` and `ModifiedByCode`, in prose, with no grammar. Reconciliation REV2 §4.2.4 stores `omitted;length={n};resolvedOn={stamp}` and does not include `ModifiedByCode`. Both are `OldValue` on a `Resolution` row. The column maximum is 200 (`TicketAuditLog.cs` lines 50–51). |
| Source-verified | `ModifiedByCode` on the ticket is `[StringLength(50)]` (`Ticket.cs` lines 138–139). `ResolvedOn` is a nullable `DateTime` (migration line 97). The two design sentences are quoted records. |
| Verdict | **Accepted.** `ModifiedByCode` is required in the marker on the fitting path and on the over-length path. One query then reads both rows. `Detail` still differs: the fitting path stores the previous text in `Detail`; the over-length path stores `Detail` null and the full text on the public comment. |
| Rule | §5.4 states the grammar `superseded;length={n};resolvedOn={stamp};modifiedBy={code}`. |
| Gate | A2. Not authorised. |
| Test | TC-05 keeps its historical `NewValue` assertions and gains the grammar assertion for the fitting path. TC-63's marker assertion is changed from `omitted;length=` to this grammar. Both NOT RUN. |

### Z-6 — One authoritative sink list

Design REV2 §7 A1 does not name the TicketList template bindings, the tenant badge, the CSAT label, or the ApprovalConfig labels. Reconciliation REV2 §4.4 does, and this file's §5.3 adds the `CreateTicket` group. An authorisation that cites Design REV2 §7 alone does not name the complete list. The in-place corrections for Y-26, Y-27, and Z-8 do not expand that sink list. Z-6 stays open until an A1 authorisation names §5.3. That authorisation does not exist.

### Z-7 — The preservation comment is visible

`IsInternal = false` places the previous resolution on the comment list of every viewer who can see the ticket, including a viewer who cannot add an internal note (`TicketDetailService.cs` lines 190–196). That visibility is the disclosure §9 asks the service owner to acknowledge. The acknowledgement is not recorded. TC-63 checks rows. TC-85 is the unread observation of the page or the service result for that viewer.

### Y-26 — Actor length follows EF6 validation, not a pre-mutation `Fail`

| Field | Record |
|---|---|
| Finding | §5.4 of this file said the existing actor-length check returns `Fail` before mutation when the length is over 50. Reconciliation REV2 §4.3.2 specified that `Fail` and the message `Actor code exceeds the 50-character audit limit.` |
| Source-verified | `TicketComment.CreatedByCode` is `[Required]` and `[StringLength(50)]` (`TicketComment.cs` lines 26–28). `TicketAuditLog.ActorCode` is `[Required]` and `[StringLength(50)]` (`TicketAuditLog.cs` lines 69–72). `Ticket.ModifiedByCode` is `[StringLength(50)]` (`Ticket.cs` lines 138–139). `ItsmDbContext` sets `LazyLoadingEnabled = false` and does not set `ValidateOnSaveEnabled`. A search of `TicketUpdateService.cs` and `TicketService.cs` found no comparison of actor length with 50 and no return of `Fail` for that reason. |
| Verdict | **Corrected.** There is no pre-mutation `Fail`. This design does not add one. EF6 validates the annotations during `SaveChanges` because the context leaves validation at the framework default. An actor string longer than 50 fails that call. The existing `catch` rolls the transaction back and rethrows. The result is not `TicketUpdateResult.Fail`. The exception type was not executed in a harness. |
| Rule | §5.10. |
| Gate | A2 for the preservation and update paths. The create path uses the same `SaveChanges` behaviour. Not authorised. |
| Test | TC-64's actor clause is corrected. TC-89 covers TG-13. Both NOT RUN. |

### Y-27 — One exact `OldValue` for the `Current` resolution row

| Field | Record |
|---|---|
| Finding | The superseded marker was defined. The `Current` row was not. Design REV2 stored `OldValue` as the prose `ResolvedOn=` plus a timestamp on first resolve, and left the re-resolve `Current` row without an `OldValue`. |
| Source | `OldValue` is `[StringLength(200)]` (`TicketAuditLog.cs` lines 50–51). `ResolvedOn` is set to one transition timestamp in §5.4. The round-trip format is the format Design REV2 already required for that timestamp. |
| Verdict | **Specified.** `ResolvedOn` is included. The persisted text is exact. `ModifiedByCode` stays on `ActorCode` and on the superseded marker. It is not copied into the `Current` marker. |
| Rule | §5.4. |
| Gate | A2. Not authorised. |
| Test | TC-90. TC-05 and TC-63 also assert this value on the `Current` row. All NOT RUN. |

### Z-8 — Allocator before the unique index, and the reverse on rollback

| Field | Record |
|---|---|
| Finding | §5.2 of this file, Reconciliation REV2 §4.1.7, and TC-78 created the unfiltered unique index before the build that removes the `RequestTypeId` filter. The previous allocator would then run against the new index. |
| Source | `GenerateTicketNumber` filters the maximum by `RequestTypeId` (`TicketService.cs` lines 374–378). The tracked migration has no unique index on `TicketNumber` (lines 117–128). Two request types that share a prefix therefore allocate separately until the filter is removed. |
| Verdict | **Corrected.** The corrected shared-prefix allocator is deployed before the index, or in the same window in which the previous allocator serves no create. Rollback drops the index and confirms it is absent before the previous build serves creates. |
| Rule | §5.2 steps 5 and 6. |
| Gate | B. Not authorised. D-4 stays Pending. |
| Test | TC-78 expected result corrected. TC-88 covers TG-12, including collision and rollback. Both NOT RUN. |

---

## 5. Updated technical design rules

These rules replace the disputed sentences in Design REV2 and Reconciliation REV2 for any later implementation discussion. They are not built. They authorise nothing. Sentences in those documents that this section does not change remain as written there.

### 5.1 Ticket-number duplicate retry

This section replaces Design REV2 AR-13's index-name clause, the Gate B retry predicate in Design REV2 §7, and Reconciliation REV2 §4.1.5. AR-6's guarded rollback remains in force and is restated here so the retry rule does not depend on a reader also holding the older cell.

**Detection.** On `DbUpdateException`, walk to the innermost `SqlException`. The duplicate-key signal is `Number` 2601 or `Number` 2627. Do not read `SqlException.Message`. Do not inspect `Errors[]` for text. Do not search for an index name. Do not store an index name in configuration for this comparison.

**Probe.** When the number is 2601 or 2627, open a new context after the failed attempt's rollback and read whether any stored `TicketNumber` equals the value this attempt tried to insert. Equality is `StringComparison.Ordinal` on the full ten characters, or the ASCII fold of all ten characters compared ordinally. The fold is the same map §4.1.1 of Reconciliation REV2 specifies. The probe does not use `LIKE`, does not use the column collation as the decision, and does not parse the exception. If the probe throws, propagate that exception. Do not convert a probe failure into a retry and do not convert it into the allocation message.

**Retry.** Retry only when the probe finds the attempted number. That finding is what confirms the duplicate concerns ticket-number allocation. A case variant of a different sequence fails the probe. A concurrent committed insert of the same canonical number passes the probe. The next attempt re-reads the maximum and moves forward.

**Propagation.** Do not retry, and do not replace the exception, when:

- the exception is not a `DbUpdateException` wrapping a `SqlException` numbered 2601 or 2627, or
- the number is 2601 or 2627 and the probe does not find the attempted ticket number.

Those are unrelated constraint violations, including a unique violation on another index. Propagate the original exception after the guarded rollback. The page must not describe that failure as an exhausted ticket-number allocation.

**Transaction integrity.** Each attempt uses a new context, a new transaction, a re-read of inputs, and a new `createdOn`. The page does not own the retry. Only the successful attempt is committed. A failed attempt commits no ticket, no `Created` audit row, no approval row, and no notification row.

The attempt's catch captures the original exception, calls `Rollback()`, and if `Rollback()` throws, ignores only that secondary failure and rethrows the captured exception. `Dispose` may roll the transaction back again. A second rollback of a transaction that has already completed is ignored. Today's `CreateTicket` catch is the bare `catch` at `TicketService.cs` lines 292–295, which calls `Rollback()` and then `throw`. If that `Rollback()` throws, the original exception is lost. Gate B includes the guarded form on `CreateTicket`. The six update methods stay on the A1 assignment of AR-6.

A rolled-back insert may consume an identity value. That gap is SQL Server identity behaviour, an engine contract, and was not observed here. It is not repaired.

**Limit and exhaustion.** At most three attempts.

| Attempt | Confirmed ticket-number duplicate | Any other failure |
|---|---|---|
| 1 | Roll back. Retry on a new context. | Stop. Propagate the original exception. |
| 2 | Roll back. Retry on a new context. | Stop. Propagate the original exception. |
| 3 | Roll back. Do not start attempt 4. Throw `InvalidOperationException`. | Stop. Propagate the original exception. |

The exhaustion exception's `Message` is a controlled sentence: the ticket number could not be allocated after three attempts. The sentence does not contain `SqlException.Message`, an index name, a stack trace, or the attempted SQL. The failed third attempt is already rolled back before this exception is thrown. No row from any failed attempt is committed.

The same controlled-message rule applies to the prefix refusal, the sequence exhaustion in Reconciliation REV2 §4.1.3, and the lock-failure path in X-2. Those are application sentences. They are not SQL text. Showing one of them is allowed only for that condition. Y-22 still forbids showing a raw exception at the page.

### 5.2 Read 4a, read 4b, and the index

This section replaces Reconciliation REV2 §4.1.7's sentence "Read 4a must be zero before the index is created" and §4.1.8's enablement cell "Zero groups, or a separate data authorisation." Read 4b's wording is kept: zero rows, or a data authorisation that rewrites them to the canonical case before allocator enablement.

The following order is the only order. A later step does not excuse an earlier unread step.

1. **Read 9 is attached** before any index decision. It records the column collation of `itsm.Tickets.TicketNumber` and the database collation, with environment, statement, timestamp, and reviewer. Read 9 is environment-unread. Until it exists, the index is not created and no data authorisation is treated as sufficient.
2. **Read 4a is counted under the column collation from read 9**, which is the collation the unique index will use. A group is two or more `TicketNumber` values that compare equal under that collation. A single row is not a group. Do not use a fixed `Latin1_General_CI_AS` count as the enablement count unless read 9 shows that the column collation is `Latin1_General_CI_AS`.
3. **A non-zero 4a blocks `CREATE UNIQUE INDEX`.** The only way past it is a separate data authorisation that rewrites or deletes the colliding rows, followed by a repeat of read 4a that returns zero groups under the same collation. An authorisation that leaves the rows stored does not enable the index. This is the rule for a case-insensitive column collation, and for any collation under which the stored values compare equal.
4. **A case-sensitive column collation** does not treat values that differ only by case as one 4a group. Those values do not block the index for that reason. They remain subject to read 4b before the allocator is enabled.
5. **Deploy the corrected shared-prefix allocator before the index.** The build that removes the `RequestTypeId` filter is in service, and the previous per-request-type allocator is not, before `CREATE UNIQUE INDEX` on `itsm.Tickets(TicketNumber)`. The alternative is one maintenance window that deploys that build and creates the index while the previous allocator accepts no create. Creating the index and leaving the previous build in service is withdrawn. The index is still created only after step 3 has produced zero groups. `TicketNumber` is not null in the tracked migration. Online versus blocking build remains read 10 and is environment-unread.
6. **Rollback is one stopped-create sequence.** Ticket creation is stopped first. `DROP INDEX` runs next, and the index is confirmed absent before the previous build is allowed to serve creates. Rows stay. Creation stays stopped until that confirmation. If the drop fails, creation stays stopped or the new build remains the only build allowed to serve creates while the index is still present. The previous allocator is not started against the index. A collision that makes the index build fail is a non-zero read 4a. TC-88 is the disposable-database check. It was not run.
7. **Read 4b** then gates allocator enablement. Zero rows, or a data authorisation that rewrites the variant rows to the canonical case named by IT. 4a does not clear 4b. 4b does not clear 4a.
8. **Read 4c** is recorded under §5.7 and does not rewrite rows.
9. **IT names the canonical case and the lock timeout** before enablement. This file names neither. AR-14's schema mechanism remains unselected. This sequence does not choose an EF migration or a standalone script, and it does not create either.

### 5.3 Output handling, including the restored `CreateTicket` group

§5.3, together with the encode-at-assignment rows already listed in Reconciliation REV2 §4.4, is the A1 output list. Design REV2 §7 A1 is not a sufficient citation by itself (Z-6).

Encode with `HttpUtility.HtmlEncode` at each `Label.Text` assignment named in Reconciliation REV2 §4.4, each component before concatenation. Do not encode a string that was already encoded. Do not build a CSS class from encoded text. Derive list badge classes from the raw data item.

**Record and do not change** the following `CreateTicket` assignments. They were reviewed against source on 24 November 2026.

| Location | Assignment | Class | Rule |
|---|---|---|---|
| `CreateTicket.aspx.cs` line 331 | `btn.InnerText = impact.Name` | Stored-text sink. Master-data name. | Leave on `InnerText`. The framework encodes on assignment. Do not also call `HtmlEncode`. |
| Line 350 | `btn.InnerText = urgency.Name` | Stored-text sink. | Same. |
| Line 376 | `name.InnerText = cat.Name` | Stored-text sink. | Same. |
| Line 406 | `name.InnerText = rt.Name` | Stored-text sink. | Same. |
| Line 330 | `aria-label = "Select impact: " + impact.Name` | Attribute. Controlled prefix. Stored-text suffix. | Leave on the attribute. The writer encodes attribute values. Do not treat the prefix as making the name controlled. |
| Line 349 | `aria-label = "Select urgency: " + urgency.Name` | Attribute. Controlled prefix. Stored-text suffix. | Same. |
| Line 367 | `aria-label = "Select category: " + cat.Name` | Attribute. Controlled prefix. Stored-text suffix. Present in source and omitted from the third review's count of two. | Same. |
| Line 397 | `aria-label = "Select request type: " + rt.Name` | Attribute. Controlled prefix. Stored-text suffix. Present in source and omitted from the third review's count of two. | Same. |
| Lines 372 and 402 | `InnerHtml` of a constant icon element | Controlled constant. | Do not change. Do not interpolate a stored name into this string. |
| `onclick` / `onkeydown` beside the binders | Integer id concatenated into a script call | Controlled value. | Do not change. |
| `CreateTicket.aspx` lines 545 and 576 | `o.text =` subcategory or item name | Stored name assigned through the DOM text property. | Do not change. Do not assign that name through `innerHTML`. |
| Lines 539, 566, 743, 745 | `innerHTML` of a constant option placeholder | Controlled constant. | Do not change. |
| `CreateTicket.aspx.cs` line 262 | `hfCategoryData.Value` from `JavaScriptSerializer` | Serializer output written to a hidden field, which renders as an attribute. | Do not change. |

`BoundField` columns, `ListItem` text, `lblActive`, and the internal-comment marker stay on the framework path named in Reconciliation REV2 §4.4.

Redaction of exception text is §5.6. It is not accomplished by the encode column of §4.4.

### 5.4 Historical resolution preservation

This section replaces Reconciliation REV2 §4.2.4 step 1's field list and the two `OldValue` shapes identified in Y-25. The compatibility direction is unchanged: a stored resolution is not truncated, not cleared, and not assumed to be editable. Reopen keeps it. A new resolution longer than 1000 characters returns `Fail` and writes nothing.

**One `now`.** After the new text has passed the blank check and the 1000-character check, and before any column is assigned, capture one `DateTime now` for the transition. Use it for `ticket.ModifiedOn`, `ticket.ResolvedOn`, the preservation comment's `CreatedOn`, and `ActionedOn` on every audit row this transition inserts. Do not call `DateTime.Now` again inside `AddAudit` for these rows. Today's `AddAudit` assigns `ActionedOn = DateTime.Now` at line 620. The preservation path passes `now` in.

**When the previous text length is 1000 or less.** Write one `Resolution` audit row whose `Detail` is the previous text, `NewValue` is `Superseded`, and `OldValue` is the superseded marker below. Then store the new trimmed text and write the `Current` row whose `Detail` is that new text and whose `OldValue` is the `Current` marker below. No preservation comment.

**When the previous text length is greater than 1000.** In the one existing `ChangeStatus` transaction and the one `SaveChanges` inside `Commit` (`TicketUpdateService.cs` lines 634–640), insert the following. Do not call `AddComment`. `AddComment` would apply the 1000 / 989 comment cap and would take a second timestamp.

Preservation comment, all of these fields:

| Field | Value | Why this value |
|---|---|---|
| `TicketId` | `ticket.Id` of the row `LoadForProcess` already loaded | Required, foreign key to `itsm.Tickets`. Default 0 is not that ticket. |
| `CommentText` | The header `Previous resolution retained in full before re-resolve because it exceeds the 1000-character audit limit.` then a newline, then the exact previous `ResolutionText` characters | The column is unbounded. The header is not a slice of the body. The previous characters are not trimmed and not cut. |
| `CreatedByCode` | The actor `LoadForProcess` already accepted | Required, `[StringLength(50)]`, foreign key to `dbo.MST_UserMaster`. No caller returns `Fail` for length. An actor longer than 50 fails at `SaveChanges` and the transaction rolls back (§5.10). |
| `CreatedOn` | The transition's `now` | Required. Keeps the value inside the tracked `datetime` range and places the comment at the transition in the `CreatedOn` ordering (`TicketDetailService.cs` lines 195–196). |
| `IsInternal` | `false` | A viewer who cannot add an internal note does not receive internal comments (lines 190–192) and does receive the resolution today. |

Superseded audit row: `TicketId = ticket.Id`, `CompanyId` from the ticket (`RequireRecordedTenant` has already required it), `ActionType` `Resolution`, `Detail` null, `NewValue` `Superseded`, `ActorCode` the actor, `ActionedOn` the same `now`, `OldValue` the marker below.

Then replace `ResolutionText` with the new trimmed text, set `ResolvedOn` to `now`, and insert the `Current` resolution row whose `Detail` is the new text and whose `OldValue` is the `Current` marker below. The `StatusChange` row remains, with `Detail` null and `ActionedOn` equal to `now`.

**`OldValue` grammar for every superseded `Resolution` row**, fitting path and over-length path:

```text
superseded;length={n};resolvedOn={stamp};modifiedBy={code}
```

- `{n}` is the C# character length of the previous `ResolutionText`.
- `{stamp}` is the previous `ResolvedOn` in round-trip form, or the literal `null` when `ResolvedOn` is null.
- `{code}` is the previous `ModifiedByCode`, or the literal `null` when that column is null. `ModifiedByCode` is included on the fitting superseded row and on the over-length superseded row.
- `NewValue` on this row is `Superseded`.
- If the marker's length would exceed 200, return `Fail` and write nothing. Do not truncate the marker and do not truncate the body into `Detail`. `ModifiedByCode` is at most 50 characters in the tracked entity. A decimal length, a `DateTime` round-trip, and that code fit in 200 with the fixed labels. The 200 check remains the backstop. It was not executed.

**`OldValue` for every `Current` resolution row**, first resolve and the new row on re-resolve. This is the only text stored in that field:

```text
current;resolvedOn={stamp}
```

- `{stamp}` is the transition's single `now`, produced by `DateTime.ToString("o", CultureInfo.InvariantCulture)`. It is the same value written to `ticket.ResolvedOn`. There is no space before or after `=`.
- `ResolvedOn` is included. The earlier prose `ResolvedOn=` plus a timestamp is withdrawn.
- `ModifiedByCode` is not part of this marker. The actor is `ActorCode` on the same audit row.
- `NewValue` is the exact string `Current`. `Detail` is the new trimmed resolution.
- If this marker would exceed 200 characters, return `Fail` and write nothing. A round-trip timestamp with this prefix fits in 200. The check was not executed.
- TC-90 compares the persisted `OldValue` with that exact concatenation.

**Failure.** A failure of the comment insert or the audit insert, including a foreign-key failure on `CreatedByCode` or `TicketId`, rolls the transaction back. The long text stays on the ticket. There is no partial comment and no partial replace.

**Reopen.** From `Resolved` to `In Progress`, leave `ResolutionText` and `ResolvedOn` unchanged. Do not insert a preservation comment. Do not insert a `Resolution` row for a pre-change ticket. `OldValue` on the status row remains `Resolution retained`.

**Display.** `lblResolution` continues to show the stored value in full. Encoding is A1 and does not cut the string.

The page states the maximum before the user types: `Resolution notes (1000 characters maximum)`. The server check is the rule. D-17 stays Pending. This path does not approve D-17.

### 5.5 Prefix rule, qualified

Reconciliation REV2 §4.1.1 rules 1 and 2 remain the application behaviour, with this qualification of what they mean.

1. Take `Prefix`. Trim spaces. Do not substitute `Code`. Do not substitute `"XX"`. Do not pad. Do not take `Substring`.
2. Accept the prefix only when the trimmed length is exactly 2 and both characters are in `A–Z`, `a–z`, or `0–9`. Otherwise throw `InvalidOperationException` with a controlled message and persist nothing.
3. Against the tracked schema, the refusal that can fire on a stored row is trimmed length 0 or 1, from a space-padded `char(2)`, or a character outside ASCII letters and digits. TC-21 is that case.
4. The `??` fallback and the length-greater-than-2 cut in `TicketService.cs` lines 364–368 are removed by this rule. They are not evidence that a tracked `MST_RequestType` row has a null prefix or a prefix longer than two characters. See Y-21.
5. Read 6 records the active prefixes and, before Gate B enablement is discussed against a database, the deployed column definition: nullability, maximum length, and whether the column is fixed-length. That definition is environment-unread. Until the record exists, the deployed column is unverified.
6. Service-owner acceptance is required for each request type the exact-2 rule blocks. For the tracked definition, that is the short-prefix or non-ASCII case. Acceptance of a longer prefix is not requested unless read 6 shows the deployed column can store one. No acceptance is recorded.

### 5.6 Exception redaction, separate from encoding

**Encoding** makes the characters of a chosen string render as text. Apply it at every `lblMessage` assignment named in Reconciliation REV2 §4.4, including `CreateTicket.aspx.cs` line 454, `TicketList.aspx.cs` line 336, and `ApprovalConfig.aspx.cs` line 155. Encoding a raw exception still displays that exception.

**Redaction** chooses the string. The user-facing text must not contain exception details: no `Exception.Message`, no `ToString()`, no inner-exception text, no SQL text, no index name, and no stack trace.

| Site | Today, source-verified | Required text |
|---|---|---|
| `CreateTicket.aspx.cs` line 208 | `InvalidOperationException.Message` plus the correlation id | A controlled sentence plus the correlation id. The sentence may be one of the fixed sentences this design defines (prefix refusal, sequence exhaustion, lock failure, allocation exhaustion after three attempts). Any other `InvalidOperationException` uses the generic sentence. |
| Line 215 | `ArgumentException.Message` plus the correlation id | The generic sentence plus the correlation id. Do not append `ex.Message`. |
| Line 222 | `GetUserFacingMessage()` | Already a fixed sentence with no exception details. Leave that property of the sentence in place. |
| `TicketList.aspx.cs` line 301 | `"Error loading ticket queue: " + ex.Message` | A fixed sentence. Do not append `ex.Message`. |
| `ApprovalConfig.aspx.cs` line 80 | `"Error updating approval policy: " + ex.Message` | A fixed sentence. Do not append `ex.Message`. |
| `ApprovalConfig.aspx.cs` line 106 | `"Error setting IT Head: " + ex.Message` | A fixed sentence. Do not append `ex.Message`. |

`ApplyResult` on the ticket detail page shows `result.Error` only when the service returns `Fail`. Those `Fail` messages are the fixed sentences in Reconciliation REV2 §4.3.3. They do not echo the rejected text. A thrown exception on that page is unchanged: the page does not gain a catch. The service rollback rethrows. That rethrow is not a user-facing encoding path.

`AnmolDristi/Web.config` was not read. `customErrors` and `httpErrors` are unknown. They are not the redaction control, and this design does not claim they hide the five assignments above. The AR-9 disclosure remains part of any later A1 authorisation: no anti-forgery token is added, no security owner is named, no date is assigned, and the ViewState MAC setting is unread.

### 5.7 Diagnostic length counts

`LEN(TicketNumber) = 10` in the allocation predicate excludes a trailing-space value from the maximum. Keep that exclusion for allocation. Do not describe it as the count of ten-character stored values.

| Read | Inclusion rule |
|---|---|
| Allocation candidate set | May use `LEN(TicketNumber) = 10`, then the ASCII fold and the four-digit test. A trailing-space value does not advance the sequence. |
| 4b | "Length 10" means `DATALENGTH(TicketNumber) = 10` on this non-unicode `varchar(10)` column. A trailing space keeps the row in the count. The rest of the 4b definition is unchanged: folded first six characters equal a namespace, and the raw first six characters are not ordinally equal to that namespace. |
| 4c | Every `TicketNumber` that contains a space, trailing or embedded, or a non-printing character. No `LEN` filter. A row that `LEN` would shorten is counted. Recorded. Not rewritten by this design. |

No population was queried. A zero result is not assumed.

### 5.8 Case-fold agreement

The C# fold remains the explicit ASCII map in Reconciliation REV2 §4.1.1. It is not `ToUpperInvariant` and not the current culture.

The SQL predicate in §4.1.2 may use `UPPER` or `LOWER`, then `COLLATE Latin1_General_BIN`, only when read 9 is attached and the recorded column collation is a Latin1 collation. For the 52 ASCII letters, that is the condition under which this design treats the SQL fold as the same map the C# uses. The condition was not executed on a server.

If read 9 is missing, Gate B stays disabled. The predicate is not enabled on the strength of read 6.

If read 9 records a collation outside the Latin1 family, the `UPPER` / `LOWER` agreement is withdrawn. Read 4b must be zero before enablement. The maximum read must not depend on SQL `UPPER` or `LOWER` of stored `TicketNumber` characters. This file does not choose a replacement predicate, because no collation was read and a replacement would be an invention. Gate B stays disabled until a later authorisation, informed by that read, names the read.

Read 6 still rejects a non-ASCII active prefix before the allocator is enabled. Read 6 does not prove the fold of historical ticket numbers.

### 5.9 What this design still does not change

The transition map. Who may resolve, reopen, or close. The approval order checks. `CanProcessTicket`. `ResolveInitialStatusId`'s fail-open fallback. Queue company visibility and the description search. Session keys. No status insert. No SLA column. No anti-forgery token. No rewrite of historical ticket numbers, historical resolutions, or historical audit rows. No change to `AnmolDristi/Web.config`.

### 5.10 Actor length

This section replaces the withdrawn pre-mutation `Fail` for actor length. It does not add a new guard.

`CreatedByCode` on the preservation comment, `ActorCode` on the audit row, and `ModifiedByCode` on the ticket are assigned the actor string the caller already passed. No method returns `TicketUpdateResult.Fail` because that string is longer than 50, and this design does not add a check that does so.

`SaveChanges` validates `[StringLength(50)]` because `ItsmDbContext` does not disable `ValidateOnSaveEnabled`. An actor value whose length is greater than 50 fails that call. `ChangeStatus`, `AddComment`, and the other update methods catch the exception, roll the transaction back, and rethrow (`TicketUpdateService.cs` lines 119–122 and the matching catches). `CreateTicket` does the same in the bare `catch` at lines 292–295. No ticket change, comment, or audit row from the failed attempt is committed. `ApplyResult` does not run, because the method did not return `Fail`.

The user-text limits that this design does add — resolution 1000, public comment 1000, internal note 989, rejection and approval and override 500 — still return `Fail` before mutation. Those are explicit checks. Actor length is not among them.

The `DbEntityValidationException` path is the EF6 contract for an enabled validator. It was not executed in a harness. TC-89 is the test. TG-13 is that gap.

---

## 6. Revised gate dependency matrix

No row is an authorisation. "Specified" means the design text in this file is complete enough to review. It does not mean the gate is open.

| Gate | Technical state after this reconciliation | Still required before the gate could be considered | Authorised now |
|---|---|---|---|
| A1 | The output list is §5.3 plus Reconciliation REV2 §4.4. It includes the four `CreateTicket` `InnerText` sinks, the four `aria-label` attributes, and the client-script rows. Badge classes stay derived from raw data. Exception redaction is specified separately from encoding (§5.6). The AR-9 dependency is explicit: a later authorisation names the missing token, the unread ViewState MAC, the absence of an owner and a date, and that A1 adds no token. Panel binding, `OrderBy`, and the guarded rollback on the six update methods remain as Design REV2 specifies them. | A change authorisation that names this file's §5.3 and §5.6, names Reconciliation REV2 §4.4, and carries the AR-9 disclosure. The blank-agent count for fall-through. That authorisation does not exist. | **No.** |
| A2 | Preservation keeps the historical resolution characters in full. The comment row names `TicketId` and `CreatedOn`. Audit rows in the same transition share one `now`. The superseded `OldValue` and the `Current` `OldValue` each have one exact grammar. An actor longer than 50 fails at `SaveChanges` and rolls back. No path clears or truncates a stored resolution. | Change authorisation. §9 acknowledgements, informed by reads 19, 20, and 21. Read 1's active non-terminal `Resolved` row. D-17 stays Pending. Those records do not exist. | **No.** |
| A3 | Behaviour is unchanged and disabled. `ResolveInitialStatusId` is excluded. Read 1 is one result per approval-flag pair, and it also carries the `Resolved` flags A2 needs. | Read 1 attached to a named environment with statement, timestamp, and reviewer, and then reviewed. A failed or missing read withholds promotion. It is never a reason to enable the branch. | **No.** Disabled. |
| B | One allocation rule, one comparison fold, one retry rule (§5.1), one 4a sequence (§5.2), one schema-qualification of the prefix (§5.5), and one length-count rule (§5.7). The corrected shared-prefix allocator is deployed before the unfiltered unique index, or in the same window in which the previous allocator serves no create. Rollback drops the index before the previous build returns. Unrelated constraint violations propagate. The attempt limit and the exhaustion message are defined. | Read 9 before 4a is interpreted. Reads 4a, 4b, 4c, 5, 6 (including the deployed prefix column definition), 7, 8, 10, 11, and 22. A named canonical case. A named lock timeout in milliseconds. One schema mechanism. The shared-sequence acknowledgement. Service-owner acceptance of each prefix the exact-2 rule blocks on the tracked definition. D-4 and D-9 stay Pending. None of these exist. | **No.** |
| C | No behaviour in A1, A2, A3, or B depends on a Gate C ruling. | A written owner ruling in the register for each of D-1 through D-18. None exists. | **No.** All 18 Pending. |

| Item | Depends on | State |
|---|---|---|
| A1 encoding, restored `CreateTicket` rows, and redaction | Change authorisation naming §5.3 and §5.6, plus the AR-9 disclosure | Not granted |
| A1 empty-agent fall-through | Blank-agent count. D-14 Pending | Not granted |
| A2 preservation of `TicketId`, `CreatedOn`, and historical resolution content | §9 acknowledgements and reads 19–21. Read 1 `Resolved` flags | Not granted |
| A3 | Read 1 recorded and reviewed for the named environment | Not granted. Gate disabled |
| Gate B | §5.1, §5.2, §5.5, §5.7, §5.8, and the reads in §8 | Not granted |
| Gate C | Register rulings | All 18 Pending |
| Anti-forgery token | A security change outside these gates. A1 must disclose it and must not add it | Owner not named |

A1 before A2 remains a release preference so that refusal text is encoded. It is not a technical prerequisite for writing the A2 rules. A3 is independent of both and stays disabled. Gate B does not wait on A1, A2, or A3. It does wait on the schema mechanism, on read 9 before the index, and on the corrected allocator being the build that serves creates before that index exists.

---

## 7. Updated test matrix

### 7.1 Preservation

TC-01 through TC-78 remain. No historical identifier is deleted. No historical expected result is removed from the document that first stated it. Where this reconciliation changes what a later run must observe, the corrected expected result is stated here and the earlier sentence is a quoted record.

Every row in this section, including every preserved row, is **NOT RUN**. None was executed. UAT and LIVE are excluded from every row.

TC-01 through TC-51, TC-53 through TC-61, TC-65 through TC-70, TC-72 through TC-77, and TC-79 through TC-85 keep the expected results already stated for them, except where §7.2 names a correction. Their needs stay disposable database, rendered page, named-environment read, source inspection, or document checkpoint. TC-64 and TC-78 are corrected in §7.2.

Document checkpoints must not be counted as product coverage: TC-57, TC-77, TC-78, and TC-83. TC-88 includes a disposable-database check and is not only a document checkpoint.

### 7.2 Corrected expected results

The historical sentences are quoted so the identifier still traces to them.

| Case | Historical expected result (quoted record) | Expected result that applies | Needs | Disposition |
|---|---|---|---|---|
| TC-05 | Design REV2: resolve, reopen, resolve. The first resolution row has `NewValue` `Superseded`. The second has `NewValue` `Current`. The ticket shows the second text. `StatusChange.Detail` is null. | Those assertions stand. The superseded row's `OldValue` is `superseded;length={n};resolvedOn={stamp};modifiedBy={code}` for the fitting path. `Detail` on that row is the previous text. The `Current` row's `OldValue` is exactly `current;resolvedOn=` plus the second transition's round-trip timestamp. | Disposable database. The §9 acknowledgement is not recorded, so this cannot accept an enabled rule. Not UAT. Not LIVE. | NOT RUN |
| TC-25 | Design REV2: a deliberate duplicate makes index creation fail and the read report the group. After a data authorisation removes it, creation succeeds and a duplicate insert is rejected. | Those assertions stand. A data authorisation that does not rewrite or delete the colliding rows does not make creation succeed. The group is counted under the column collation recorded by read 9, once that read exists. | Disposable copy. Not UAT. Not LIVE. Not an index on a shared environment. | NOT RUN |
| TC-26 | Design REV2: a unique-index violation produces the controlled `InvalidOperationException` after the bounded retry. Ticket, audit, approvals, and notification are absent. | A probe-confirmed ticket-number collision on all three attempts produces the controlled `InvalidOperationException`. The message contains no SQL text and no index name. No ticket, audit, approval, or notification row is committed. An unrelated unique violation is TC-62, not this case. | Disposable database. Gate B objects are not authorised on UAT or LIVE. | NOT RUN |
| TC-52 | Design REV2: after a ticket-number unique violation, the retry predicate observes `SqlException` 2601 or 2627 and the ticket-number index name. When `Rollback()` throws, the original exception is still the one observed. | The predicate observes `Number` 2601 or 2627 and does not read `SqlException.Message`. It does not require an index name. The probe has found the attempted number. When `Rollback()` throws, the original exception is still the one observed. | Disposable database. Fault injection of a throwing `Rollback()`. Not UAT. Not LIVE. | NOT RUN |
| TC-62 | Reconciliation REV2: a ticket-number unique violation whose message is not English still retries when the existence probe finds the attempted number, and then allocates a higher sequence. A 2601 or 2627 that is not a ticket-number collision does not retry. The predicate does not read the exception message. | Those assertions stand. The unrelated 2601 or 2627 is propagated and is not replaced by the allocation message. After three probe-confirmed collisions, the controlled exception is thrown and no row is committed. The predicate never reads `Message`. | Disposable database. Not UAT. Not LIVE. | NOT RUN |
| TC-63 | Reconciliation REV2: the long fixture is preserved in full; reopen adds no preservation comment; a 1001-character re-resolve changes nothing; a legal re-resolve writes a public comment that ends with the exact previous characters, a superseded audit row with `Detail` null and the `omitted;length=` marker, and a current audit row whose `Detail` is the new text. | Those assertions stand, with these replacements. The comment's `TicketId` is the ticket's id. The comment's `CreatedOn`, the ticket's `ResolvedOn`, and `ActionedOn` on the superseded, current, and status audit rows are the same transition timestamp. `OldValue` on the superseded row is `superseded;length={n};resolvedOn={stamp};modifiedBy={code}`, not `omitted;length=`. `OldValue` on the `Current` row is exactly `current;resolvedOn=` plus that same timestamp in round-trip form. No stored copy of the previous text is shorter than the original. | Disposable database. Preservation acknowledgement not recorded, so this cannot accept an enabled rule. Not UAT. Not LIVE. | NOT RUN |
| TC-64 | Reconciliation REV2: an over-length rejection reason, approval comment, override reason, public comment, internal note, and actor code each return `Fail` or, for the actor-code backstop, roll back. | The user-text cases still return `Fail` and commit nothing. An actor code longer than 50 does not return `Fail`. `SaveChanges` fails and the transaction rolls back. No ticket column, comment, or audit row is committed. TC-89 is the dedicated actor case. | Disposable database. Not UAT. Not LIVE. | NOT RUN |
| TC-78 | Reconciliation REV2: the release plan creates the `TicketNumber` unique index before the build that removes the `RequestTypeId` filter. | The corrected shared-prefix allocator serves creates before that index is created, or both change in one window in which the previous allocator serves no create. Rollback drops the index and confirms it is absent before the previous build serves creates. A plan that runs the previous allocator while the index exists fails. No seek is claimed unless a captured plan shows one. The index-first expected result is withdrawn. | Document checkpoint. Not an index creation. Not UAT. Not LIVE. | NOT RUN |
| TC-71 | Reconciliation REV2: a request type whose trimmed prefix length is greater than 2 is refused. No ticket is persisted. The prefix row is not substring-corrected by the application. | The application refuses a presented prefix whose trimmed length is not exactly 2, including a fixture longer than 2, and does not substring it. That fixture is not a tracked `char(2)` value and is not evidence of deployed data. The tracked-schema refusal remains TC-21. | Disposable database. Drift fixture. Owner acceptance is separate and not recorded. Not UAT. Not LIVE. | NOT RUN |

### 7.3 Tests added

Added because no existing identifier covers the corrected behaviour. Each is **NOT RUN**.

| Case | Covers | Expected result | Needs | Disposition |
|---|---|---|---|---|
| TC-79 | Y-23, TG-8 | A stored ten-character `TicketNumber` that contains a trailing space, and one that contains an embedded space, stay stored. Each remains openable and searchable. Neither advances the sequence. Read 4c counts both. A count that uses `LEN` as its only length test is not accepted as 4c. | Disposable database. Not UAT. Not LIVE. | NOT RUN |
| TC-80 | Y-24, Y-18's collation step | Read 9 is attached with environment, statement, timestamp, reviewer, column collation, and database collation. No row is updated. If the column collation is outside the Latin1 family, the record states that read 4b must be zero before enablement and that the `UPPER` agreement is withdrawn. If read 9 is absent, Gate B is not enabled. | Named-environment read. No row update. Not a schema change. | NOT RUN |
| TC-81 | Y-22 | At `CreateTicket` lines 208 and 215, `TicketList` line 301, and `ApprovalConfig` lines 80 and 106, the rendered message does not contain the exception message, SQL text, or an index name. A correlation id may appear where the page already has one. TC-27 still covers encoding of `<`, `>`, and `"`. Passing TC-27 does not pass TC-81. | Rendered page, after an A1 build that does not yet exist. Site was not started. Not UAT. Not LIVE. | NOT RUN |
| TC-82 | Y-19 | Impact, urgency, category, and request-type names that contain `<` render as text through the four `InnerText` assignments. The four `aria-label` values expose the name as text, not as markup. The assignments are still `InnerText` and `aria-label`. They have not been moved to `InnerHtml`. | Rendered page. Site was not started. Not UAT. Not LIVE. | NOT RUN |
| TC-83 | Y-18 | The release plan states one 4a sequence: read 9, count under that collation, zero groups, or rewrite or delete and re-count to zero, then create the index. A plan that still allows a data authorisation to leave colliding rows in place fails this checkpoint. This row does not create an index. | Document checkpoint. | NOT RUN |
| TC-84 | Y-21, TG-9 | Read 6's attachment records the deployed `MST_RequestType.Prefix` nullability, maximum length, and fixed-length flag, with environment, statement, timestamp, and reviewer. No row is updated. Until that attachment exists, the deployed column is unverified, and a null or longer-than-two prefix is not reported as a tracked-schema fact. | Named-environment read. No row update. Not a schema change. | NOT RUN |
| TC-85 | Z-7, TG-7 | As a viewer who is neither requester nor assignee and cannot add an internal note, the previous over-length resolution appears in the comment list, and an internal note on the same ticket does not. The comment's `TicketId` is that ticket. | Rendered page or service-level read. Preservation acknowledgement not recorded, so this cannot accept an enabled rule. Site was not started. Not UAT. Not LIVE. | NOT RUN |
| TC-86 | TG-10 | The existence probe throws. The exception propagates. The attempt does not retry, does not throw the allocation `InvalidOperationException` in place of the probe failure, and commits no ticket, audit, approval, or notification row. | Disposable database. Fault injection. Not UAT. Not LIVE. | NOT RUN |
| TC-87 | TG-11 | The preservation-comment insert fails. The transaction rolls back. `ResolutionText` and `ResolvedOn` stay as they were. No preservation comment, no superseded audit row, and no `Current` audit row from that attempt are committed. | Disposable database. Fault injection. Preservation acknowledgement not recorded. Not UAT. Not LIVE. | NOT RUN |
| TC-88 | TG-12, Z-8 | On a disposable database, two numbers the previous per-request-type allocator can treat as independent block creation of the unfiltered unique index. After a clean index exists, rollback confirms the index is absent before the previous allocator is eligible to run. A sequence that enables the previous allocator while the index is present fails. | Disposable database. Not an index on UAT or LIVE. | NOT RUN |
| TC-89 | TG-13, Y-26 | An actor code of 51 characters on `ChangeStatus` or `AddComment` does not return `Fail`. `SaveChanges` fails. The transaction rolls back. The ticket, comments, and audit rows are unchanged. | Disposable database. Not UAT. Not LIVE. | NOT RUN |
| TC-90 | Y-27 | A first resolve persists one `Resolution` row whose `NewValue` is `Current` and whose `OldValue` is exactly `current;resolvedOn=` concatenated with `now.ToString("o", CultureInfo.InvariantCulture)`. `Detail` is the trimmed resolution. No other characters are stored in `OldValue`. | Disposable database. The §9 acknowledgement is not recorded, so this cannot accept an enabled rule. Not UAT. Not LIVE. | NOT RUN |

### 7.4 Needs, for the corrected and new rows

| Need | Cases | Constraint |
|---|---|---|
| Disposable database, excluded from UAT and LIVE | TC-05, TC-25, TC-26, TC-52, TC-62, TC-63, TC-64, TC-71, TC-79, TC-86, TC-87, TC-88, TC-89, TC-90, and the preserved disposable-database rows | No shared environment. TC-25, TC-26, TC-52, TC-62, and TC-88 observe Gate B behaviour that is not authorised to be created on UAT or LIVE. |
| Schema change on a disposable database only | Preserved TC-13, TC-47, TC-58 (`ALTER`). TC-88 creates and drops an index only on that disposable database | Not UAT. Not LIVE. |
| Named-environment read, no row update | TC-20, TC-49, TC-54, TC-69, TC-70's environment half, TC-76, TC-80, TC-84 | Statement, timestamp, reviewer, and environment are part of the record. A missing read withholds promotion. |
| Owner acknowledgement before the row can accept an enabled rule | TC-06, TC-08, TC-17, TC-32, TC-48's fail-closed branch, TC-63, TC-68, TC-85, TC-87, TC-90, and the prefix acceptance behind TC-21 and TC-71 | §9. Not recorded. |
| Rendered page, site not started | Preserved rendered-page list, plus TC-81, TC-82, TC-85 | Not a demonstrated page. |
| Document checkpoint | TC-57, TC-77, TC-78, TC-83 | Not product coverage. TC-78's expected result is the corrected allocator-before-index order. |

---

## 8. Database and environment preconditions

None of these was run. Nothing in this section is an instruction to execute SQL. A result copied from an earlier document would be a quoted record. None is copied here. A failed or missing read withholds promotion. It is never a reason to enable a branch.

Design REV2 reads 1 through 18 remain outstanding, with the corrections already stated in Reconciliation REV2 §7, plus the following.

| Read | What this reconciliation adds | Gate it blocks |
|---|---|---|
| 1 | Still unread. A3 stays disabled until this read is recorded and reviewed. A2 is not promoted unless the same read shows an active, non-terminal row named `Resolved`. | A3, and A2's promotion condition |
| 4a | Counted only under the column collation from read 9. Non-zero blocks the index until the rows are rewritten or deleted and the count is repeated at zero. | B |
| 4b | "Length 10" uses `DATALENGTH`, not `LEN`. Zero rows, or a rewrite to the canonical case, before allocator enablement. If read 9 is non-Latin1, 4b must be zero and the `UPPER` agreement is withdrawn. | B |
| 4c | Counts trailing spaces and embedded spaces with no `LEN` filter. Recorded. Not a rewrite. | B, as evidence |
| 6 | Active prefixes, and the deployed `MST_RequestType.Prefix` definition: nullable or not, maximum length, fixed-length or not. The definition is unverified until this record exists. | B |
| 9 | Column collation and database collation, before 4a is interpreted and before `UPPER` is treated as the ASCII map. Environment-unread. No collation is asserted. | B |
| 19, 20, 21 | Unchanged. Population counts for over-length resolutions, comments, and audit detail. The §9 acknowledgements are uninformed without them. | A2 |
| 22 | Writers of `TicketNumber` outside `TicketService.CreateTicket`. The repository half is not a proof about other applications on `DbConn`. | B |

`AnmolDristi/Web.config` was not read. Request-validation mode, `customErrors`, `httpErrors`, and the ViewState MAC setting remain unknown.

---

## 9. Owner acknowledgements

These are not new decision numbers. They are not written into the register. They are not approvals. An empty cell is not a record. Every row is **Not recorded**.

| Acknowledgement | Why it is separate from a closed decision | Who would have to record it | State |
|---|---|---|---|
| A new resolution longer than 1000 characters will be refused, and the page will say so | D-17 asks for the minimum. The column is unbounded in the tracked migration. | Service owner. IT leadership for the audit ceiling. D-17 stays Pending. | Not recorded |
| A historical resolution longer than 1000 characters is kept in full, copied to a public comment on re-resolve, and not truncated into `Detail` | The comment is visible to every viewer of the ticket, including the requester. `Detail` on the superseded row is null. `TicketId` and `CreatedOn` are specified in §5.4 and do not by themselves record this acknowledgement. | Service owner. IT leadership, because `Detail` is null on that row. | Not recorded |
| The preservation comment uses the loaded ticket's `TicketId` and the transition's `CreatedOn`, and the audit rows of that transition use the same timestamp | This keeps the foreign key and the comment order valid. It is part of the same disclosure, not a second product decision. | The same owners. | Not recorded |
| Public comments move from a 2000-character hint to 1000, and internal notes to 989 user characters. The preservation comment is not subject to that cap | No D-id owns the comment box. D-5 stays Pending. | Service owner. | Not recorded |
| Reads 19, 20, and 21 were shown before the capacity acknowledgements were signed | An acknowledgement without the counts is uninformed. | The same owners, after the reads exist. | Not recorded. Reads not run |
| Two request types that share a prefix will share one monthly sequence and one 9999 ceiling | D-4 and D-9 stay Pending. | IT leadership for the method. Service owner for the catalogue. | Not recorded |
| A request type whose trimmed prefix is not exactly two ASCII letters or digits cannot create tickets | Against the tracked schema this is the short-prefix or non-ASCII case. A longer or null prefix is not asserted as a current catalogue fact. Deployed column definition is unverified (read 6). | Service owner to accept the block or to authorise a data correction. | Not recorded |
| Canonical case, and the lock timeout in milliseconds | Naming them is not a D-4 ruling. | IT. | Not selected |
| Read 4a collisions are rewritten or deleted, and read 4b case variants are rewritten or explicitly accepted, before Gate B | A bare authorisation does not build a unique index over values the index collation equates. | IT / DBA for the rewrite. Service owner if displayed numbers change. | Not recorded |
| Empty `SupportAgentCode` falls through only after its count is known | D-14 stays Pending. | IT, and the service owner if the count is not zero. | Not recorded |
| Schema mechanism for the new indexes | EF migration or standalone script. | IT and DBA. | Not selected |
| Anti-forgery owner and date | Outside this design. A1 must disclose the gap and must not add a token. | Not named. | Not assigned |
| Other writers of `TicketNumber`, if read 22 finds any | The lock does not cover them. | IT / DBA. | Not recorded |

Exception redaction (Y-22) is an A1 change-authorisation item. It is not a new register row.

---

## 10. D-1 through D-18 status

Source: `ITSM_BUSINESS_DECISION_REGISTER.md` §10, lines 564–590, read on 24 November 2026. The preamble says no names are recorded and no row is approved. The Owner ruling column is empty on every row. The Date column is empty on every row. The Status column is Pending on every row. No approval email, signature, or dated ruling was present in that table. Recommendations are not approvals. This reconciliation does not change the register.

| ID | Register status | Owner ruling | Date | This reconciliation |
|---|---|---|---|---|
| D-1 | Pending | Empty | Empty | Stays Pending. |
| D-2 | Pending | Empty | Empty | Stays Pending. Reopen adds no window and no clock reset. |
| D-3 | Pending | Empty | Empty | Stays Pending. |
| D-4 | Pending | Empty | Empty | Stays Pending. Gate B describes the register's recommended method. The recommendation is not a ruling. The shared-sequence acknowledgement does not close D-4. |
| D-5 | Pending | Empty | Empty | Stays Pending. No D-id owns the comment box. |
| D-6 | Pending | Empty | Empty | Stays Pending. |
| D-7 | Pending | Empty | Empty | Stays Pending. No SLA column. |
| D-8 | Pending | Empty | Empty | Stays Pending. Encoding a ticket number does not choose a public origin. |
| D-9 | Pending | Empty | Empty | Stays Pending. Shared prefixes are not a flag policy. |
| D-10 | Pending | Empty | Empty | Stays Pending. |
| D-11 | Pending | Empty | Empty | Stays Pending. |
| D-12 | Pending | Empty | Empty | Stays Pending. |
| D-13 | Pending | Empty | Empty | Stays Pending. |
| D-14 | Pending | Empty | Empty | Stays Pending. Blank-agent fall-through stays conditional. |
| D-15 | Pending | Empty | Empty | Stays Pending. Null `CompanyId` stays refused. |
| D-16 | Pending | Empty | Empty | Stays Pending. |
| D-17 | Pending | Empty | Empty | Stays Pending. The 1000-character maximum and the preservation path are acknowledgements, not a ruling on the minimum. |
| D-18 | Pending | Empty | Empty | Stays Pending. |

Section 3 and Section 4.4 of the register are also Pending. They are not additional decision numbers and they are not closed.

No formal owner-approval evidence was found for any of the 18. This document does not create any.

---

## 11. Implementation-readiness checklist

Every authorisation, environment, and execution item is unmet. Rows marked "Specified" are text in this file only.

| Item | State |
|---|---|
| One retry rule: numbers 2601 and 2627, no message, no index name, existence probe, retry only for a confirmed ticket-number duplicate, unrelated violations propagated, three attempts, exhaustion defined, guarded rollback | Specified in §5.1. Not implemented. Not authorised. |
| AR-13 index-name clause and historical TC-52 index-name sentence withdrawn as acceptance criteria, identifiers retained | Specified in §3 and §7. The source documents were not edited. |
| TC-52 and TC-62 consistent and both required | Specified. **NOT RUN.** |
| One read-4a sequence, conditional on read 9, rewrite or delete required when the count is not zero | Specified in §5.2. Reads not run. |
| `CreateTicket` four `InnerText` sinks and four `aria-label` attributes classified and included in the A1 list | Specified in §5.3. Not a code change. |
| Exception redaction specified separately from HTML encoding, including the five `ex.Message` assignments | Specified in §5.6. Not implemented. `Web.config` unread. |
| AR-9 disclosure required of any later A1 authorisation, owner not named | Specified. Not granted. |
| Preservation assigns `TicketId` and `CreatedOn`, keeps the historical characters, and uses one `now` on the audit rows | Specified in §5.4. Acknowledgement not recorded. |
| Superseded `OldValue` grammar, and the exact `Current` `OldValue` `current;resolvedOn={stamp}` | Specified in §5.4. Not implemented. TC-90 NOT RUN. |
| Actor length fails at `SaveChanges` and rolls back. No pre-mutation `Fail` is added | Specified in §5.10. Not implemented. TC-89 NOT RUN. |
| Allocator before the unique index, and index drop before the previous allocator returns | Specified in §5.2. Not implemented. TC-78 and TC-88 NOT RUN. |
| Prefix null and length qualified against `[Required]`, `[StringLength(2)]`, and `char(2)`; deployed column unverified | Specified in §5.5. Read 6 not run. |
| `LEN` versus trailing-space counts separated for allocation and for reads 4b and 4c | Specified in §5.7. No row read. |
| `UPPER` agreement anchored to read 9 | Specified in §5.8. Read 9 not run. |
| A1 change authorisation naming §5.3, §5.6, and the AR-9 disclosure | **Not granted.** |
| A2 acknowledgements and reads 19–21, plus read 1 `Resolved` flags | **Not recorded. Not run.** |
| A3 read 1 recorded and reviewed | **Not run.** Gate disabled. |
| Gate B reads, named case, named timeout, schema mechanism, prefix acceptance | **Not met.** |
| D-1 through D-18 formal approval | **Pending.** No evidence. |
| TC-01 through TC-90 executed | **NOT RUN.** |
| UAT or LIVE connection | **Not opened.** |
| Application, SQL, migration, or configuration change | **Not made.** |

The checklist does not contain a met authorisation item. The specification rows do not open a gate.

---

## 12. Explicit authorization status

**NOT READY FOR IMPLEMENTATION.**

**No implementation authorization is granted by this reconciliation.**

- No application file may be edited on the authority of this document.
- No SQL file, migration, index, trigger, or configuration change may be written or executed on the authority of this document.
- Gates A1, A2, A3, B, and C are closed.
- A1 is not authorised. Completeness for a later discussion requires the restored `CreateTicket` sinks, exception redaction separate from encoding, and an authorisation that carries the AR-9 disclosure. This file is not that authorisation.
- A2 is not authorised. `TicketId`, `CreatedOn`, the historical resolution characters, and the §9 acknowledgements are required. The acknowledgements are not recorded.
- A3 stays disabled until read 1 is recorded and reviewed. A failed read does not enable the branch.
- B is not authorised. The allocation, retry, comparison, schema qualification, and 4a sequence in §5 are one design. The specification is not the authorisation.
- C retains D-1 through D-18 as Pending. No recommendation in the business decision register is a ruling.
- Every test disposition, TC-01 through TC-90, is **NOT RUN**. TG-10 through TG-13 are TC-86 through TC-89 and are **NOT RUN**.
- No SQL was executed and no UAT or LIVE connection was opened to produce this file.
- `AnmolDristi/Web.config` was not read and was not modified. Its pre-existing working-tree modification is left in place.
- The in-place correction edited this file, `ITSM_TECHNICAL_REMEDIATION_DESIGN_REV2.md`, and `ITSM_FREEBUFF_REMEDIATION_RECONCILIATION_REV2.md`. No application file, SQL file, migration, or configuration file was modified. Nothing was staged, committed, pushed, or tagged.

### Repository confirmation

| Item | State |
|---|---|
| Files changed in place | `ITSM_FREEBUFF_REMEDIATION_RECONCILIATION_REV3.md`, `ITSM_TECHNICAL_REMEDIATION_DESIGN_REV2.md`, `ITSM_FREEBUFF_REMEDIATION_RECONCILIATION_REV2.md`. |
| `HEAD` | `f1f4bdb2c82013e913ac682aa5e31832a3fa94fc` (`f1f4bdb`). Not moved by this work. |
| Application, SQL, migration, configuration | Not modified. |
| `AnmolDristi/Web.config` | Not read. Not modified. The pre-existing ` M` entry remains. |
| Commit, push, tag | None. |

No new revision file was created. D-1 through D-18 remain Pending. Gates A1, A2, A3, B, and C remain closed.

**NOT READY FOR IMPLEMENTATION.**
