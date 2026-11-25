# ITSM FreeBuff remediation review — revision 4

**Document:** `ITSM_FREEBUFF_REMEDIATION_REVIEW_REV4.md`
**Date:** 25 November 2026
**Type:** Independent read-only review of the remediation document set. Not an approval. Not an implementation authorisation. Does not create a reconciliation or a design revision.
**Verdict:** **Accepted with Corrections**
**Implementation readiness:** **NOT READY FOR IMPLEMENTATION**

**No implementation authorization is granted by this review.**

This file reviews `ITSM_FREEBUFF_REMEDIATION_RECONCILIATION_REV3.md` against the source tree at `HEAD` and against the tracked schema. It changes no input document, no application file, no SQL, no migration, and no configuration. It executed no SQL, opened no UAT or LIVE connection, ran no build, ran no test, started no site, and did not read `AnmolDristi/Web.config`.

---

## 1. Verdict

**Accepted with Corrections.**

The third review's blocking finding, Y-17, is resolved. `ITSM_FREEBUFF_REMEDIATION_RECONCILIATION_REV3.md` §3 withdraws the AR-13 index-name clause and the historical TC-52 index-name sentence as acceptance criteria while keeping both identifiers, and §5.1 states one retry rule that detects duplicates by `SqlException.Number` 2601 or 2627 and confirms them with an existence probe. TC-52 and TC-62 remain two live, non-contradictory cases. I verified the rule against the source that would carry it and found it consistent.

The corrections are not complete. This review records three defects the reconciliation does not close:

| ID | Class | Summary |
|---|---|---|
| Y-26 | Material, source-contradicted | §5.4 asserts that an existing actor-length check returns `Fail` before mutation when the actor code exceeds 50 characters. No such check exists in the application. |
| Y-27 | Material, precision | The `OldValue` value on the `Current` resolution audit row is not stated in §5.4, although §11 presents "one `OldValue` grammar" as settled. |
| Z-8 | Material, introduced risk | §5.2 step 5 orders the unfiltered unique index **before** the build that removes the `RequestTypeId` filter, creating a live window in which the old allocator runs against the stricter index with no retry. Rollback ordering between the index and the build is unspecified. |

None of the three reopens Y-17 or restores the Y-3 contradiction. None changes the retry rule, the preservation field list, the output list, or the 4a sequence. Each is a specification-text correction, and each can be closed without a new reconciliation revision.

**Residual test coverage gaps:** TG-10, TG-11, TG-12, TG-13 (§7).

**Remaining blocking corrections: none.** The three defects above are material and must be corrected before a later authorisation adopts §5.2 or §5.4, but none blocks acceptance of this document as the design of record.

---

## 2. Method, baseline, and limits

### 2.1 Baseline

| Item | Value |
|---|---|
| `HEAD` | `f1f4bdb2c82013e913ac682aa5e31832a3fa94fc` (`f1f4bdb`) |
| Source read | `AnmolDristi/` working tree at that commit, read on 25 November 2026 |
| Schema basis | `AnmolDristi/ITSM/DAL/Migrations/202608120550515_InitialItsmSchema.cs` and the entity classes |
| Deployed database | Not read. Not queried. No collation, prefix column definition, population count, or grant is asserted here. |

Working-tree line numbers are not a certified baseline and are not a deployed-database result.

### 2.2 Evidence labels

| Label | Meaning |
|---|---|
| Source-verified | Read in the named file at `HEAD` `f1f4bdb` for this review. |
| Quoted record | A sentence copied from an earlier document. Not re-certified. |
| Framework or engine contract | Published behaviour of ASP.NET, ADO.NET, EF6, or SQL Server. Not executed here. |
| Environment-unread | A deployed row, collation, column definition, or count. Not queried. |
| NOT RUN | The test was not executed. |

### 2.3 Documents read

| Document | Role in this review |
|---|---|
| `ITSM_FREEBUFF_REMEDIATION_RECONCILIATION_REV3.md` | Subject of this review. 641 lines. Read in full. Not modified. |
| `ITSM_TECHNICAL_REMEDIATION_DESIGN_REV2.md` | Prior design. AR-13, Gate B retry paragraph, TC-52, A1 record-and-do-not-change sentence. Quoted only. |
| `ITSM_FREEBUFF_REMEDIATION_REVIEW_REV3.md` | Third review, author of Y-17 and Y-18 through Y-25. Quoted only. |
| `ITSM_FREEBUFF_REMEDIATION_RECONCILIATION_REV2.md` | Second reconciliation. §4.1.5, §4.1.7, §4.1.8, §4.2.3, §4.2.4, §4.4, TC-62. Quoted only. |
| `ITSM_SOURCE_TO_FINDING_TRACEABILITY.md` | Source map. Quoted only. |
| `ITSM_BUSINESS_DECISION_REGISTER.md` | §10 lines 564–590, read on 25 November 2026. D-1 through D-18. |

### 2.4 What this review did not do

It did not execute SQL, open a UAT or LIVE connection, build `AnmolDristi.sln`, run a test, start a site, render a page, or read `AnmolDristi/Web.config`. It did not modify, stage, commit, push, or tag anything. It did not treat a register recommendation as a ruling, and it did not mark any test passed.

### 2.5 Limitations

| Limitation | Effect on this review |
|---|---|
| No build or test execution | Every test disposition in §7 is a document finding, not an observed result. Source claims are read, not executed. |
| No SQL and no UAT/LIVE access | Reads 1, 4a, 4b, 4c, 5, 6, 7, 8, 9, 10, 11, 19, 20, 21, and 22 remain unverified. No deployed collation, prefix definition, or population count is asserted. |
| `AnmolDristi/Web.config` unread | Request-validation mode, `customErrors`, `httpErrors`, and ViewState MAC remain unknown. |
| Inputs are untracked | Git cannot prove the input documents are byte-identical to their earlier state. They were opened read-only. |
| TC preservation is a document assertion | I confirmed that every identifier TC-01 through TC-78 exists somewhere in the four documents and that REV3 deletes none (§6). I did not re-verify the byte-for-byte wording of all 78 earlier expected results. |

---

## 3. Y-3 / AR-13 / TC-52 / TC-62 — resolved

### 3.1 What the reconciliation changed

| Element | REV3 disposition | Review result |
|---|---|---|
| AR-13 index-name clause | Withdrawn as an acceptance criterion; AR-13's identifier retained (§3.1, §11) | **Correct.** The identifier still resolves; the false predicate is no longer a requirement. |
| Design REV2 §7 Gate B retry paragraph | Replaced by §5.1; "any other 2627 is not retried" survives only as a probe result (§3.1) | **Correct.** The probe replaces the name match without loosening the guard. |
| Design REV2 TC-52 index-name expected result | Withdrawn; AR-6 half kept; corrected expected result in §7.2 | **Correct and traceable.** The historical sentence is quoted in the same row. |
| TC-62 | Retained unchanged in substance; still the Y-3 case | **Correct.** Both identifiers stay live and non-overlapping. |
| TC-26 | Narrowed to a probe-confirmed collision that exhausts three attempts; unrelated unique violation moved to TC-62 | **Correct.** Removes the ambiguity that an unrelated 2601/2627 would produce the allocation message. |

### 3.2 The rule, checked against source

`§5.1` requires: innermost `SqlException`; detection by `Number` 2601 or 2627; no `Message` read; no index-name search or comparison; a new-context existence probe comparing the attempted ten characters ordinally or by the ASCII fold; retry only on a probe hit; propagation otherwise; three attempts; controlled exhaustion sentence; guarded rollback.

Source-verified:

- `TicketService.cs` lines 292–295 are the bare `catch { transaction.Rollback(); throw; }` that §5.1 identifies. A throwing `Rollback()` there loses the original exception. §5.1 places the guarded form on Gate B and leaves the six update methods on AR-6. That assignment matches the source shape.
- `TicketService.cs` lines 364–368 are the `??` fallback, trim, and `Substring(0, 2)` that §5.5 replaces.
- `TicketService.cs` lines 377–378 apply the `RequestTypeId` filter to the maximum read. This is the filter §5.2 step 5 removes, and it is the basis of Z-8.

The rule is internally consistent. A 2601 or 2627 on the unfiltered unique index can only arise from a value equal to an existing `TicketNumber` under the index collation, so the probe is a sound discriminator. A case variant of a different sequence fails the probe, as §5.1 states. No message text and no index name is required at any step.

### 3.3 Residual

The retry rule is **specification-complete for Gate B** and **not authorised**. Two things it does not settle, both recorded elsewhere in this review:

1. The probe-throws path has no test (TG-10).
2. The index-before-build ordering in §5.2 step 5 is a deployment hazard (Z-8).

Neither is a defect in the retry rule itself.

---

## 4. Y-18 through Y-25 — finding-by-finding

### Y-18 — One enablement sequence for read 4a

**Conclusion: resolved in text; carries Z-8.**

§5.2 is now the only 4a sequence: read 9 first, count under the read-9 column collation, a non-zero count blocks `CREATE UNIQUE INDEX`, the only way past is a rewrite or delete followed by a repeated count at zero, a case-sensitive collation does not create case-variant groups, then the index, then 4b, then 4c. The contradictory Reconciliation REV2 §4.1.7 and §4.1.8 cells are explicitly replaced.

The dependent facts hold against the tracked schema: `TicketNumber` is `varchar(10)` not null (migration line 78), so an index can be built and a null-value caveat is not needed. Read 9 remains environment-unread and correctly gates the interpretation.

Defect: step 5 places index creation **before** the build that removes the `RequestTypeId` filter. See Z-8.

### Y-19 — `CreateTicket` output assignments

**Conclusion: resolved, and the third review's count is corrected.**

Source-verified at `CreateTicket.aspx.cs`:

| Line | Assignment | REV3 class | Review |
|---|---|---|---|
| 331 | `btn.InnerText = impact.Name` | Stored-text sink | Correct |
| 350 | `btn.InnerText = urgency.Name` | Stored-text sink | Correct |
| 376 | `name.InnerText = cat.Name` | Stored-text sink | Correct |
| 406 | `name.InnerText = rt.Name` | Stored-text sink | Correct |
| 330 | `aria-label = "Select impact: " + impact.Name` | Attribute, controlled prefix, stored suffix | Correct |
| 349 | `aria-label = "Select urgency: " + urgency.Name` | Same | Correct |
| 367 | `aria-label = "Select category: " + cat.Name` | Same | Correct; omitted from the third review's count of two |
| 397 | `aria-label = "Select request type: " + rt.Name` | Same | Correct; omitted from the third review's count of two |

The four `InnerText` and four `aria-label` assignments are present, and the reconciliation's correction of the earlier undercount is accurate. The constant `InnerHtml` rows (372, 402), the constant `innerHTML` rows at `CreateTicket.aspx` lines 539, 566, 743, and 745, the `o.text` rows at 545 and 576, the integer-id script concatenation, and `hfCategoryData.Value` at line 262 are correctly classified as record-and-do-not-change. The instruction not to encode a second time on top of `InnerText` is correct for `HtmlContainerControl`.

### Y-20 — Preservation comment `TicketId` and `CreatedOn`

**Conclusion: resolved as design text.**

Source-verified: migration lines 166–181 declare `TicketId` not null with a foreign key to `itsm.Tickets`, `CommentText` not null and unbounded, `CreatedByCode` not null with a 50-character maximum and a foreign key to `dbo.MST_UserMaster`, `CreatedOn` not nullable, and `IsInternal` not null. `TicketComment.cs` matches. `AddComment` inserts at lines 489–496 and assigns `CreatedOn = DateTime.Now` at line 494. `TicketDetailService.cs` lines 186–196 load comments for the ticket, hide internal comments from a viewer who cannot add an internal note, and order by `CreatedOn` descending.

§5.4 names `TicketId`, `CreatedOn`, `CreatedByCode`, `CommentText`, and `IsInternal`, and requires the comment to be written inside the existing `ChangeStatus` transaction and the single `SaveChanges` in `Commit` (`TicketUpdateService.cs` line 634). That placement is correct and is what keeps the comment and the replace atomic.

The EF `c.DateTime()` → SQL Server `datetime` contract in the reconciliation's contract note is a framework claim that was not executed here. Its consequence is sound: an unassigned `DateTime` is `0001-01-01`, which `datetime` rejects, so omitting `CreatedOn` would fail rather than silently store the default.

One statement in this section is false: the claim about an existing actor-length check. See **Y-26**.

### Y-21 — Prefix null and length against the tracked column

**Conclusion: resolved and correctly qualified.**

Source-verified: `RequestType.Prefix` is `[Required]`, `[StringLength(2)]`, `[Column(TypeName = "char")]` (`RequestType.cs` lines 25–28); migration line 243 declares it non-nullable, maximum 2, fixed length, non-unicode. `Code` is `[Required]`, `[StringLength(10)]` (lines 17–18).

The reconciliation's qualification is correct: a null prefix, the `Code` fallback, the `"XX"` fallback, and a length greater than two are not reachable from a row the tracked column can store. Only a trimmed length of 0 or 1 from a space-padded `char(2)`, or a non-ASCII character, can fire the exact-2 refusal on the tracked definition. Read 6's record of the deployed column definition is correctly required before any wider claim, and TC-84 is the correct checkpoint. No deployed prefix definition is invented.

### Y-22 — Redaction separate from encoding

**Conclusion: resolved.**

Source-verified at the named sites: `CreateTicket.aspx.cs` line 208 (`InvalidOperationException.Message` plus correlation id), line 215 (`ArgumentException.Message` plus correlation id), line 222 (`GetUserFacingMessage()`, a fixed sentence plus correlation id at `CreateTicketDiagnostics.cs` lines 210–214), `TicketList.aspx.cs` line 301, and `ApprovalConfig.aspx.cs` lines 80 and 106. All five concatenation sites are present exactly as §5.6's table states. `ShowMessage` assigns `lblMessage.Text` at `CreateTicket.aspx.cs` lines 452–455.

The separation is correct: encoding chooses how a string renders, redaction chooses the string. The controlled-sentence allowlist is bounded to conditions the design defines, and the generic sentence covers every other `InvalidOperationException`. The acknowledgement that `customErrors` and `httpErrors` are unread and are not the control is correct, and the AR-9 disclosure is properly carried into A1 rather than added.

Citation nuance, not a defect: `CreateTicketDiagnostics.cs` is under `ITSM/BLL`, and REV3 cites it by file name only. No incorrect path is stated.

### Y-23 — `LEN` and trailing spaces

**Conclusion: resolved as a count correction.**

`TicketNumber` is `varchar(10)`, non-unicode, not null (migration line 78). On that column `DATALENGTH` counts stored bytes, which equal characters, including trailing spaces; `LEN` excludes trailing spaces. §5.7 is therefore correct to separate the three counts: allocation may keep `LEN(TicketNumber) = 10`, 4b must use `DATALENGTH = 10`, and 4c must count any space or non-printing character with no `LEN` filter.

The reconciliation correctly stops short of asserting any count. No population was queried.

### Y-24 — Where the `UPPER` agreement is anchored

**Conclusion: resolved.**

The agreement between the C# ASCII fold and SQL `UPPER`/`LOWER` is a property of the column collation, not of the active prefixes. Anchoring it to read 9 is correct, and §5.8's behaviour when read 9 is missing (Gate B stays disabled) and when read 9 is non-Latin1 (the agreement is withdrawn and 4b must be zero, with no invented replacement predicate) is the right handling of an unread environment. Read 6 continues to gate the prefixes the allocator emits, which is the correct narrower role.

### Y-25 — One `OldValue` grammar

**Conclusion: resolved for superseded rows; incomplete for the `Current` row.**

Source-verified: `TicketAuditLog.OldValue` is `[StringLength(200)]` and nullable; `Ticket.ModifiedByCode` is `[StringLength(50)]`; `ResolvedOn` is a nullable `DateTime` (migration line 97). The grammar `superseded;length={n};resolvedOn={stamp};modifiedBy={code}` fits the 200-character cap with room for a decimal length, a round-trip timestamp, and a 50-character code.

§5.4 correctly requires `ModifiedByCode` on both the fitting and over-length paths, which closes the divergence between Design REV2's prose and Reconciliation REV2's `omitted;length=` marker. TC-05 gains the grammar assertion and TC-63's marker assertion is corrected from `omitted;length=` to this grammar. Traceability is preserved by quoting the historical sentence in the same row.

Gap: the `Current` row's `OldValue` is not stated. See **Y-27**.

### Z-6 and Z-7

Carried, not re-scored. Z-6 (one authoritative sink list) is addressed in the sense that §5.3 plus Reconciliation REV2 §4.4 is named as the A1 list a later authorisation must cite; the design authorisation that would close it does not exist. Z-7 (the preservation comment is visible to every viewer, including one who cannot add an internal note) is correctly recorded as a disclosure requiring a §9 acknowledgement, with TC-85 as the unread observation.

---

## 5. New findings

### Y-26 — The actor-length claim is contradicted by source

| Field | Record |
|---|---|
| Severity | **Material.** Source-contradicted statement in the design of record. Not blocking, because the failure outcome is still safe. |
| Location | `ITSM_FREEBUFF_REMEDIATION_RECONCILIATION_REV3.md` §5.4, preservation-comment table, `CreatedByCode` row: "The existing actor-length check returns `Fail` before mutation when the length is over 50." |
| Finding | No such check exists. |
| Evidence | `TicketUpdateService` performs no `.Length` comparison on the actor code. `ItsmTicketAccess.CanProcessTicket` validates that the actor code is not blank; it does not test a maximum. `ItsmDbContext` sets no `ValidateOnSaveEnabled = false`, so EF6 validation is active on `SaveChanges`. `TicketComment.CreatedByCode` and `TicketAuditLog.ActorCode` are both `[Required] [StringLength(50)]`, and the comment's `CreatedByCode` is also a foreign key to `dbo.MST_UserMaster`. A code longer than 50 characters therefore fails inside `SaveChanges` — EF entity validation, or the foreign key — after the entity state has been mutated, not before it. `Commit` (`TicketUpdateService.cs` lines 634–640) does not translate that failure into a `Fail` result; it propagates. |
| Consequence | The specified behaviour is still safe: the transaction rolls back, the long resolution stays stored, and no partial comment or audit row commits, which is exactly what §5.4's later "Failure" paragraph requires. What is wrong is the stated mechanism and the claim of a pre-mutation `Fail`. An implementer reading §5.4 would believe a guard exists that would have to be written, and would believe the over-50 case is handled by a result code rather than by an exception. |
| Correction required | Replace the sentence with a statement of the actual contract: the comment's `CreatedByCode` is required, maximum 50, and a foreign key; there is no length guard today; a value longer than 50 fails at `SaveChanges`; the transition rolls back and rethrows, and the caller sees the exception rather than `Fail`. If a `Fail` result is wanted for that condition, it must be specified as new code, not described as existing. |
| Gate | A2 |
| Test | **TG-13**, which does not exist. |

### Y-27 — The `Current` resolution row's `OldValue` is unspecified

| Field | Record |
|---|---|
| Severity | **Material, precision.** Divergent implementations remain possible on a field the reconciliation presents as settled. |
| Location | §5.4, "**`OldValue` grammar for every superseded `Resolution` row**", and §11's checklist row "One `OldValue` grammar, including `ModifiedByCode`, on both resolution paths". |
| Finding | §5.4 states the superseded row's `OldValue` grammar and states the `Current` row's `Detail` and `NewValue`, but never states the `Current` row's `OldValue`. Design REV2's resolution-history prose did state a `Current`-row shape in the form `ResolvedOn=` plus a timestamp (line 229), and §5.4 does not restate or withdraw it. |
| Ambiguity | Two readings are available: (a) the `Current` row carries `OldValue` in the `ResolvedOn={stamp}` form inherited from Design REV2, or (b) the `Current` row carries `OldValue` null. If (a), §5.4 does not say whether `{stamp}` is the previous `ResolvedOn` or the transition's new `now` — the only `now` the section defines. If (b), the design departs from Design REV2 without saying so. |
| Consequence | No write failure. `TicketAuditLog.OldValue` is `[StringLength(200)]` and nullable, so either reading stores. The defect is that two implementers can produce different audit rows for the same transition, and a later query that reads `OldValue` across resolution rows would see inconsistent shapes. §11's "one grammar" claim is stronger than §5.4's text supports. |
| Correction required | State the `Current` row's `OldValue` explicitly — null, or the `ResolvedOn=` form with a named timestamp — and extend the grammar heading from "every superseded `Resolution` row" to the full `Resolution` row set. TC-05 should assert the `Current` row's `OldValue` as well as the superseded row's. |
| Gate | A2 |
| Test | TC-05 asserts the superseded row only. Add the `Current`-row assertion. |

### Z-8 — Index-before-build ordering and unspecified rollback order

| Field | Record |
|---|---|
| Severity | **Material risk.** Introduced by the ordering rule the reconciliation adds. Not blocking the specification, but it must be closed before Gate B enablement. |
| Location | §5.2 step 5: "Create the unfiltered unique index only after step 3 has produced zero groups, **and before the application build that removes the `RequestTypeId` filter**." |
| Mechanism | The currently deployed allocator computes its maximum over `RequestTypeId` (`TicketService.cs` lines 377–378). Creating the unfiltered unique index first makes the index stricter than the allocator's assumption. Until the new build is live, the old allocator can re-emit a number that another request type already used under a shared prefix: the two sequences are independent, the duplicate is not visible to the filtered maximum read, and the insert then violates the new unique index. |
| Why it is live | D-4 and D-9 are Pending, and §9 records that two request types sharing a prefix will share one sequence as an acknowledgement that is **not recorded**. The precondition the ordering assumes — that no two request types currently share a prefix, or that their sequences do not overlap — is unverified. Read 4a being zero proves only that stored values are unique now; it does not prove the old allocator cannot create a new collision. |
| Failure mode | The deployed `CreateTicket` has no bounded retry. Its catch at `TicketService.cs` lines 292–295 rolls back and rethrows, so a 2601 or 2627 raised in this window reaches the page as a failure, not as the controlled allocation message. The outcome is a spurious ticket-creation error for a user, in a window the design creates deliberately. |
| Rollback gap | §5.2 says only "Rollback of the index is `DROP INDEX` and keeps the rows." It does not state the order relative to a build rollback. If the build is rolled back while the unfiltered index remains, the old allocator is left running under the stricter index indefinitely. If `DROP INDEX` runs while the new build remains live, the allocator loses the uniqueness backstop it now assumes, and the bounded retry rule has nothing to retry on. |
| Correction required | Either (a) deploy the allocator build and the index inside one maintenance window with the application stopped, so no old-build insert can occur between them; or (b) order the build first and create the index immediately after, accepting that the allocator briefly has no unique backstop; and in either case state the rollback order explicitly, including that `DROP INDEX` and a build rollback are performed together or in a named sequence. Add the matching checkpoint to TG-12. |
| Gate | B |
| Test | **TG-12**, which does not exist. TC-83 covers only the 4a sequence. |

---

## 6. Test matrix, TC-01 through TC-85

### 6.1 Preservation and inventory

REV3 §7.1 states that TC-01 through TC-78 remain, that no historical identifier is deleted, and that no historical expected result is removed from the document that first stated it. I checked the inventory by identifier across the four documents:

| Check | Result |
|---|---|
| Every identifier TC-01 through TC-78 appears in at least one of Design REV2, Reconciliation REV2, Review REV3, or Reconciliation REV3 | **Pass.** No identifier in that range is missing from the set. |
| REV3 deletes or renumbers a historical identifier | **No.** §3.3 keeps TC-52 and TC-62 as two live cases; §7.1 preserves the range; §7.3 adds new identifiers above it. |
| Added identifiers | TC-79 through TC-85, seven cases, all NOT RUN. |
| Referenced test set after this reconciliation | TC-01 through TC-85. The user's focus range TC-01 through TC-78 is fully preserved; the set now extends seven further. |

The preservation claim is a document assertion about wording, and I did not re-verify all 78 earlier expected results verbatim. I verified identifier presence and absence of deletion.

### 6.2 Corrected expected results (§7.2)

| Case | Correction | Review |
|---|---|---|
| TC-05 | Superseded row gains the `superseded;length=…;modifiedBy=` grammar; `Detail` on that row is the previous text | Correct for the superseded row. Does not assert the `Current` row's `OldValue` (Y-27). |
| TC-25 | A data authorisation that leaves colliding rows in place does not make creation succeed; the group is counted under the read-9 collation | Correct, and consistent with §5.2 step 3. |
| TC-26 | Narrowed to a probe-confirmed collision exhausting three attempts; unrelated unique violation is TC-62 | Correct. |
| TC-52 | Index name removed; `Number` 2601/2627 and no `Message` read; probe confirmed; throwing `Rollback()` still preserves the original exception | Correct; AR-6 half retained. |
| TC-62 | Probe-confirmed collision retries even with a non-English message; unrelated 2601/2627 propagates; third collision throws controlled exception; never reads `Message` | Correct, and the two expected results can both pass. |
| TC-63 | Comment `TicketId` and one transition timestamp across comment, `ResolvedOn`, and audit rows; `OldValue` changed from `omitted;length=` to the grammar | Correct. Does not assert the `Current` row's `OldValue` (Y-27). |
| TC-71 | Presented prefix of trimmed length other than two is refused and not substring-corrected; the fixture is drift, not tracked-schema evidence; TC-21 remains the tracked case | Correct, and consistent with Y-21. |

### 6.3 Added tests (§7.3)

| Case | Covers | Review |
|---|---|---|
| TC-79 | Trailing-space and embedded-space ticket numbers; 4c must not use `LEN` alone | Correct scope. |
| TC-80 | Read 9 attached with environment, statement, timestamp, reviewer, both collations; non-Latin1 consequence recorded; absent read disables Gate B | Correct scope. |
| TC-81 | Redaction at the five `ex.Message` sites; does not overlap TC-27's encoding assertions | Correct scope. |
| TC-82 | Four `InnerText` and four `aria-label` assignments render stored names as text | Correct scope, and the count is four and four. |
| TC-83 | Document checkpoint for one 4a sequence | Correct, and correctly excluded from product coverage. |
| TC-84 | Read 6's deployed prefix column definition | Correct scope. |
| TC-85 | Over-length resolution visible to a viewer who cannot add an internal note; internal note hidden | Correct scope. |

### 6.4 Every disposition

**TC-01 through TC-85: NOT RUN.** No test was executed for this review or reported as executed by the reconciliation. Document checkpoints TC-57, TC-77, TC-78, and TC-83 are not product coverage, and REV3 correctly says so.

---

## 7. Test coverage gaps introduced or left open

| ID | Gap | Why it matters | Where it belongs |
|---|---|---|---|
| TG-10 | No case injects a **throwing probe**. TC-52 injects a throwing `Rollback()`; TC-26 and TC-62 cover probe hits and misses. | §5.1 requires a probe failure to propagate and forbids converting it into a retry or into the allocation message. Nothing asserts that. | New case, disposable database with a fault-injected probe. |
| TG-11 | No case fails the **preservation comment insert** — for example a `CreatedByCode` foreign-key failure or a `TicketId` mismatch — and asserts rollback with the long text retained and no partial audit row. | §5.4's "Failure" paragraph is the atomicity guarantee for A2. TC-63 covers the success path only. | New case, disposable database. |
| TG-12 | No checkpoint covers **index/build ordering and rollback ordering** (Z-8). TC-83 covers the 4a sequence only. | The ordering is the one place the design can create a live failure window, and the rollback order is unstated. | Document checkpoint plus a release-plan review item under Gate B. |
| TG-13 | No case covers an **actor code longer than 50** on a resolution transition. | Y-26 shows the stated mechanism is wrong; the actual behaviour (rollback and rethrow, not `Fail`) is untested. | New case, disposable database, after Y-26's correction. |

These are additive. None replaces a corrected case, and none weakens an existing assertion.

---

## 8. Gate readiness

No gate is open. "Specified" means the design text is reviewable; it is not an authorisation.

| Gate | Specification state | What still blocks it | Ready |
|---|---|---|---|
| A1 | Complete as text: §5.3 plus Reconciliation REV2 §4.4; four `InnerText` and four `aria-label` rows; redaction separated from encoding in §5.6; AR-9 disclosure required and not added | A change authorisation naming §5.3, §5.6, and Reconciliation REV2 §4.4, carrying the AR-9 disclosure: no anti-forgery token, unread ViewState MAC, no named security owner, no date. The blank-agent count for fall-through (D-14 Pending). None exists. | **No** |
| A2 | Preservation field list complete, one `now` across the transition, one superseded-row `OldValue` grammar | Y-26 (false actor-length claim) and Y-27 (`Current`-row `OldValue`) must be corrected. §9 acknowledgements not recorded. Reads 19, 20, 21 not run. Read 1's active non-terminal `Resolved` row not recorded. D-17 Pending. | **No** |
| A3 | Behaviour unchanged and disabled; `ResolveInitialStatusId` excluded; read 1 also carries the `Resolved` flags A2 needs | Read 1 attached to a named environment with statement, timestamp, reviewer, then reviewed. A failed or missing read withholds promotion and never enables the branch. | **No — disabled** |
| B | One retry rule (§5.1), one 4a sequence (§5.2), one prefix qualification (§5.5), one length-count rule (§5.7), one fold anchor (§5.8) | Z-8's ordering and rollback order must be closed. Read 9 before 4a is interpreted; reads 4a, 4b, 4c, 5, 6, 7, 8, 9, 10, 11, 22; a named canonical case; a named lock timeout in milliseconds; one selected schema mechanism; the shared-sequence acknowledgement; service-owner acceptance of each prefix the exact-2 rule blocks. D-4 and D-9 Pending. | **No** |
| C | No A1, A2, A3, or B behaviour depends on a Gate C ruling | A written owner ruling for each of D-1 through D-18. All 18 Pending. | **No** |

### 8.1 Dependency ordering

The ordering is non-circular and the reconciliation states it correctly:

1. Read 9 precedes the 4a interpretation.
2. 4a at zero under the read-9 collation precedes `CREATE UNIQUE INDEX`.
3. The index precedes the build that removes the `RequestTypeId` filter. This is Z-8's window.
4. 4b precedes allocator enablement, independently of 4a.
5. A1 precedes A2 only as a release preference, so refusal text is encoded. It is not a technical prerequisite for writing A2's rules.
6. A3 is independent of A1 and A2.
7. B waits on its own reads and on the index-before-build order, not on A1, A2, or A3.
8. C is orthogonal: no behaviour in A1, A2, A3, or B depends on a Gate C ruling.

No cycle exists. The one soft seam is step 3, where satisfying the ordering is what creates Z-8.

---

## 9. D-1 through D-18

Read from `ITSM_BUSINESS_DECISION_REGISTER.md` §10, lines 564–590, on 25 November 2026.

| Check | Result |
|---|---|
| Rows present | D-1 through D-18, eighteen rows. |
| Owner ruling column | Empty on all eighteen. |
| Date column | Empty on all eighteen. |
| Status column | Pending on all eighteen. |
| Section 3 | Pending. |
| Section 4.4 | Pending. |
| Approval evidence | No signature, dated ruling, or approval email was present in the register. |
| Changed by the reconciliation | No. §10 records the same state and does not alter the register. |

Recommendations remain recommendations. All eighteen stay Pending, and C stays closed.

---

## 10. Findings register for this review

| ID | Severity | Status | Gate |
|---|---|---|---|
| Y-26 | Material, source-contradicted | Open | A2 |
| Y-27 | Material, precision | Open | A2 |
| Z-8 | Material, introduced risk | Open | B |
| TG-10 | Test gap | Open | B |
| TG-11 | Test gap | Open | A2 |
| TG-12 | Test gap | Open | B |
| TG-13 | Test gap | Open | A2 |
| Y-3 / Y-17 | Blocking | **Closed by REV3 §3 and §5.1** | B |
| Y-18 | Blocking | **Closed in text**, carries Z-8 | B |
| Y-19 | Blocking | **Closed**, count corrected to four and four | A1 |
| Y-20 | Blocking | **Closed as design text**, carries Y-26; correction required to one sentence | A2 |
| Y-21 | Material | **Closed and qualified**; deployed column unverified | B |
| Y-22 | Material | **Closed**; redaction separated from encoding | A1 |
| Y-23 | Precision | **Closed**; `DATALENGTH` for 4b, no `LEN` for 4c | B |
| Y-24 | Precision | **Closed**; anchored to read 9 | B |
| Y-25 | Precision | **Closed for superseded rows**; `Current` row open as Y-27 | A2 |
| Z-6 | Risk | Open by design; closed only by a later A1 authorisation | A1 |
| Z-7 | Risk | Recorded; acknowledgement not recorded | A2 |

---

## 11. Implementation authorization status

**NOT READY FOR IMPLEMENTATION.**

**No implementation authorization is granted by this review.**

- No application file may be edited on the authority of this review or of the reconciliation it reviews.
- No SQL file, migration, index, trigger, or configuration change may be written or executed on the authority of either document.
- Gates A1, A2, A3, B, and C are closed. No gate is ready.
- A1 is not authorised. Completeness for a later discussion requires §5.3 plus Reconciliation REV2 §4.4, the redaction rules in §5.6, and an authorisation that carries the AR-9 disclosure. This review is not that authorisation.
- A2 is not authorised. Y-26 and Y-27 must be corrected, the §9 acknowledgements recorded, reads 19, 20, and 21 run, and read 1's `Resolved` flags recorded.
- A3 remains disabled until read 1 is recorded and reviewed for a named environment.
- B is not authorised. Z-8's ordering and rollback order must be specified, and the reads, the canonical case, the lock timeout, the schema mechanism, the prefix acceptance, and the shared-sequence acknowledgement are all outstanding.
- C retains D-1 through D-18 as Pending, with empty ruling and date columns.
- Every test, TC-01 through TC-85, is **NOT RUN**.
- No SQL was executed and no UAT or LIVE connection was opened to produce this file.

---

## 12. Checks run and not run

| Check | State |
|---|---|
| Read `ITSM_FREEBUFF_REMEDIATION_RECONCILIATION_REV3.md` in full (641 lines) | Run |
| Read the named source files at `HEAD` `f1f4bdb` for every disputed claim | Run |
| Verify the migration's `TicketNumber`, `TicketComments`, `Prefix`, and `ResolvedOn` definitions | Run |
| Verify `CreateTicket` output sinks and the five `ex.Message` sites | Run |
| Verify `AddComment` insert lines 489–496 with `CreatedOn` at 494, `AddAudit`, and `Commit` at 634 | Run |
| Verify absence of an actor-length guard in `TicketUpdateService`, `ItsmTicketAccess`, and `ItsmDbContext` | Run |
| Verify `TicketAuditLog.OldValue` is `[StringLength(200)]` and nullable | Run |
| Inventory check that every identifier TC-01 through TC-78 exists in the four documents | Run |
| Read register §10 lines 564–590 | Run |
| Build `AnmolDristi.sln` | **Not run** |
| Execute any test, TC-01 through TC-85 | **Not run** |
| Execute SQL, query a database, or open a UAT or LIVE connection | **Not run** |
| Render a page or start a site | **Not run** |
| Read `AnmolDristi/Web.config` | **Not read** |
| Modify an input document, application file, SQL, migration, or configuration | **None** |
| Commit, push, or tag | **None** |

---

## 13. Repository confirmation

| Item | State |
|---|---|
| File created | `ITSM_FREEBUFF_REMEDIATION_REVIEW_REV4.md`, repository root, untracked. |
| `HEAD` | `f1f4bdb2c82013e913ac682aa5e31832a3fa94fc` (`f1f4bdb`). Not moved by this work. |
| Input documents | Read only. Not modified. |
| Application, SQL, migration, configuration | Not modified. |
| `AnmolDristi/Web.config` | Not read. Not modified. Its pre-existing ` M` entry remains. |
| Pre-existing untracked documents and deployment directories | Left in place. |
| Stage, commit, push, tag | None. |

Because the input documents are untracked, Git cannot prove they are byte-identical to their earlier state. They were opened for reading only. This review wrote no path other than the file named above.

**Verdict: Accepted with Corrections.** The Y-3 contradiction is closed and the Y-18 through Y-25 corrections are substantially complete. Y-26, Y-27, and Z-8 are material and open, and TG-10 through TG-13 are uncovered. No gate is ready.

**No implementation authorization is granted by this review.**
