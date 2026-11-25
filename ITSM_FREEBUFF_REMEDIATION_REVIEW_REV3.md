# ITSM remediation — third independent review

**Document:** ITSM_FREEBUFF_REMEDIATION_REVIEW_REV3.md
**Date:** 24 November 2026
**Type:** Read-only independent review. Not an approval. Not an implementation authorisation.
**Working tree inspected:** `HEAD` `f1f4bdb` (`f1f4bdb2c82013e913ac682aa5e31832a3fa94fc`). Line numbers are locations in that working tree. They are not a certified baseline and not a deployed-database result.
**Required position, stated at the top and at the end:** **No implementation authorization is granted by this review.**

---

## 1. Overall verdict

**ACCEPTED WITH CORRECTIONS.**

The reconciliation in `ITSM_FREEBUFF_REMEDIATION_RECONCILIATION_REV2.md` resolves four of the five required corrections as **specification text**, and it resolves the output-encoding completeness finding completely against the source I re-read. It keeps every gate closed, keeps D-1 through D-18 Pending, and keeps every test NOT RUN. None of that is in dispute.

It is **not** yet internally consistent, and one of the five required corrections is only half applied:

- **Y-3 is not resolved across the document set.** The retry rule was corrected in the reconciliation §4.1.5 to a 2601/2627 plus existence-probe discriminator with no message reading, but `ITSM_TECHNICAL_REMEDIATION_DESIGN_REV2.md` **AR-13** still requires "the index name is the ticket-number index", and **TC-52** still asserts that the predicate observes "the ticket-number index name". The reconciliation declared AR-13 as holding and preserved TC-01 through TC-59 unchanged. An implementer following either of those two places reintroduces exactly the localized-message dependency that Y-3 removed. **Blocking for Gate B.**
- **Eight further corrections remain** (Y-18 through Y-25): one contradiction inside the Gate B precondition table, two omissions in text the reconciliation presents as complete (the A1 sink enumeration and the preservation-comment insert), one premise in Y-12 that the tracked schema does not support, one gate statement that encoding is not (Y-22), and precision items on the fold, the length predicate and the audit marker.
- **Two risks are introduced by the reconciliation** (Z-6, Z-7) and **three test gaps remain** (TG-7 through TG-9).

The five required corrections, one line each:

| Required correction | Result in this review |
|---|---|
| Y-1 ASCII-only sequence validation | **Resolved as design text.** Four ASCII digits at offset 6, integer 1..9999, formatting refused outside that range, no `ToString("D4")` on a non-positive value, no `int.TryParse`. |
| Y-2 Consistent C#/SQL case folding | **Resolved as design text, with two carve-outs.** The fold, the single-row count 4b, the index-collation argument and the retry all align. Y-18 (read 4a's enablement cell contradicts §4.1.7) and Y-24 (`UPPER()` evaluated under the column collation, not `Latin1_General_BIN`) remain. |
| Y-3 Safe duplicate-key retry | **Not resolved across the document set.** §4.1.5 is correct; AR-13 and TC-52 are not updated. Y-17, blocking. |
| Y-4 Historical resolution preservation | **Resolved as design text and as a disclosure.** Owner approval is still **not recorded**, so A2 stays blocked. Y-20 (the new comment row's `CreatedOn`) remains. |
| Y-5 Audit validation contract and atomic rollback | **Resolved.** The five maxima in §4.3.1 match the entity exactly. No second channel. |
| Y-7 / Y-8 Completeness of untrusted output sinks | **Resolved.** §4.4 names every stored-text sink I could find in the four ITSM pages, including the three `TicketList` bindings, the tenant badge, the CSAT feedback label and both ApprovalConfig labels. Y-19 is a documentation gap in the "record and do not change" half, not a missing encode. |

**No gate is ready for implementation authorisation.** A1, A2, A3, B and C are closed. A3 stays disabled. Gate B is further from readiness than the reconciliation states, because Y-17 leaves two of its own governing texts contradicting its retry rule.

---

## 2. Basis, method and evidence labels

### 2.1 Documents read

| Document | Role |
|---|---|
| `ITSM_FREEBUFF_REMEDIATION_RECONCILIATION_REV2.md` | The subject of this review. 629 lines. Read in full. |
| `ITSM_TECHNICAL_REMEDIATION_DESIGN_REV2.md` | The proposed design the reconciliation corrects. 529 lines. Read in full across §1–§2, §6–§10, §13–§15. |
| `ITSM_FREEBUFF_REMEDIATION_REVIEW_REV2.md` | The second review whose Y, Z and TG items the reconciliation dispositions. 520 lines. §1–§3 read directly; §4–§15 checked where a disposition relied on them. |
| `ITSM_SOURCE_TO_FINDING_TRACEABILITY.md` | R-01, F-01 through F-10. 99 lines. Read in full. |
| `ITSM_BUSINESS_DECISION_REGISTER.md` | D-1 through D-18. 642 lines. §10 read directly (lines 564–590); §11 checked for its own constraints. |
| `AGENTS.md` | Repository boundaries, including the `Web.config` prohibition and the read-only default. |

`ITSM_FREEBUFF_REMEDIATION_RECONCILIATION.md` and `ITSM_FREEBUFF_REMEDIATION_REVIEW.md` were consulted only where the reconciliation or the register refers to a specific earlier finding.

### 2.2 Source re-read for this review

- `AnmolDristi/ITSM/BLL/TicketService.cs` — `CreateTicket` lines 60–300, `GenerateTicketNumber` 359–401, `ResolveInitialStatusId` 404–432, `EnqueueTicketCreatedNotification` 445–506.
- `AnmolDristi/ITSM/BLL/TicketUpdateService.cs` — `ChangeStatus` 55–124, `ApproveTicket` 300–352, `AddComment` 480–519, `LoadForTicket`/`RequireRecordedTenant` 575–597, `AddAudit` 599–622, `Truncate` 624–632, `Commit` 634–640.
- `AnmolDristi/ITSM/BLL/TicketDetailService.cs` — `CanApprove`/`CanAddInternalNote`/comments projection, lines 150–209.
- `AnmolDristi/ITSM/UI/TicketDetail.aspx` — lines 230–446, including both repeaters, the audit grid and the action panels.
- `AnmolDristi/ITSM/UI/TicketDetail.aspx.cs` — `LoadTicket` 225–315, `GetActorCode` 341, `ShowMessage`/`ApplyResult` 366–393.
- `AnmolDristi/ITSM/UI/TicketList.aspx` and `.aspx.cs` — 140–179 and 296–336.
- `AnmolDristi/ITSM/UI/CreateTicket.aspx` and `.aspx.cs` — 190–250, 320–410 and the client script 530–610.
- `AnmolDristi/ITSM/UI/ApprovalConfig.aspx` and `.aspx.cs` — 40–128, 150–155.
- Entities: `TicketAuditLog.cs`, `RequestType.cs`; `TicketComment`/`TicketStatus` columns via the migration.
- `AnmolDristi/ITSM/DAL/Migrations/202608120550515_InitialItsmSchema.cs` — `Tickets` 73–128, `TicketComments` 166–180, `MST_RequestType` 236–248, `MST_TicketStatus` 250–259.
- Repository-wide grep for `TC-nn` identifiers across all five inputs, and for `.Text =`, `Eval(`, `BoundField`, `InnerText`, `Attributes[` and `innerHTML` under `AnmolDristi/ITSM/UI`.

### 2.3 Evidence labels

| Label | Meaning |
|---|---|
| Source-verified | Read at the cited file and range on 24 November 2026. |
| Confirmed | The reconciliation's disposition matches the source or matches Revision 2's own text. |
| Disputed | The reconciliation's text contradicts itself, contradicts a document it declares unchanged, or is not implementable as written. |
| Environment-unread | A deployed row, collation, grant, job or population. Not queried. `AnmolDristi/Web.config` was not read. |
| Not granted | No owner ruling, no change authorisation and no environment attachment exists. |

### 2.4 What this review did not do

No SQL was executed. No UAT or LIVE connection was opened. Nothing was built, and no test in TC-01 through TC-78 was run. No page was rendered and no site was started. No application, SQL, migration or configuration file was edited. No input document was modified. Nothing was staged, committed or pushed. `AnmolDristi/Web.config` was not read. Section 14 records these as limitations rather than passes.

---

## 3. The five required corrections, verified against source

### 3.1 Y-1 — ASCII-only sequence validation

**Confirmed resolved as design text.**

The reconciliation §4.1.2 replaces the parse entirely: length exactly 10, ordinal comparison of the first six characters after an explicit ASCII fold, each of indexes 6–9 an ASCII digit, and the integer in 1..9999. §4.1.3 refuses a next value below 1 or above 9999 before formatting, then formats four digits with leading zeros, and keeps a post-format length-and-digit backstop.

That closes the defect I confirmed in source. `GenerateTicketNumber` (`TicketService.cs` lines 387–397) rejects only `n.Length >= 10` and otherwise calls the two-argument `int.TryParse` on `n.Substring(n.Length - 4)`; `(-122).ToString("D4")` is four characters, so a signed value yields a ten-character number that passes a length test. Source-verified: line 399 is `numberPrefix + nextSequence.ToString("D4", CultureInfo.InvariantCulture)`.

Two things are now correct that were not before: the sequence is read at offset 6 rather than from the end, and `0000` is explicitly non-advancing rather than merely non-parsing. `TicketNumber` is `varchar(10)` (migration line 78: `maxLength: 10, unicode: false`), so a ten-character sign-bearing value was storable. It no longer originates here.

Precision noted at Y-23: because the column is `varchar(10)`, rule 1's "exactly 10" can never see a longer stored value, and `LEN()` differs from `String.Length` on trailing spaces. That affects the diagnostic counts, not the emitted number.

### 3.2 Y-2 — Consistent case folding in C# and SQL

**Confirmed resolved as design text, with two carve-outs (Y-18, Y-24).**

§4.1.1 fixes the namespace at exactly six characters and folds with an explicit ASCII map; §4.1.2 states the maximum read as a SQL predicate with `COLLATE Latin1_General_BIN`; §4.1.4 demonstrates a single lower-case row advancing the sequence rather than being dropped; §4.1.8 adds read 4b, a per-row count, because read 4a's group count cannot see a single row. That is the missing case the second review identified.

Source context re-read: the maximum read at `TicketService.cs` lines 374–383 is `AsNoTracking` with a `RequestTypeId` filter and `StartsWith`, materialised with `ToList()`; `Prefix` is `char(2)` (migration line 243), so a returned value may carry a trailing pad that the reconciliation's `.Trim()` step covers.

Remaining inside Y-2:

- **Y-18** — §4.1.7 says read 4a "must be zero before the index is created"; §4.1.8's enablement cell says "Zero groups, or a separate data authorisation". A data authorisation that does not rewrite or delete the rows cannot make a case-insensitive unique index buildable, and if the column collation turns out to be case-sensitive the same groups are harmless. Two rules, one field.
- **Y-24** — §4.1.1 rule 4 says "The same map is what SQL applies, because the prefix read has already rejected every non-ASCII character", but §4.1.2 applies `UPPER()` to *stored* `TicketNumber` characters, and `UPPER()` is evaluated under the column collation; `COLLATE Latin1_General_BIN` is applied to its result. On a non-Latin1 collation the two maps can disagree for a stored value. The agreement argument belongs to read 9, not only read 6.

### 3.3 Y-3 — Safe duplicate-key retry

**Disputed. Not resolved across the document set. Blocking for Gate B. See Y-17.**

§4.1.5 is correct on its own terms: innermost `SqlException`, number 2601 or 2627, an existence probe on a new context for the attempted value by ordinal comparison or by the ten-character ASCII fold, no `SqlException.Message`, no index-name search, no retry when the probe finds nothing, `InvalidOperationException` with a controlled message otherwise, at most three attempts, new context, new inputs and new `createdOn` per attempt, page does not own the retry.

But the reconciliation did not carry that rule into the two places that still teach the old one. `ITSM_TECHNICAL_REMEDIATION_DESIGN_REV2.md` §6 AR-13 ends: "Intermediate retries run only when the guarded rollback preserved 2601 or 2627 **and the index name is the ticket-number index**." §10 TC-52 expects: "the retry predicate observes `SqlException` 2601 or 2627 **and the ticket-number index name**." The reconciliation §3.6 lists the AR corrections it applies — Y-13 on AR-1, Y-12 on AR-12, the AR-5 character count — and does not name AR-13; §6.1 preserves TC-01 through TC-59 with "their expected results ... preserved". TC-62, added by the reconciliation, asserts the opposite: "The predicate does not read the exception message."

A test matrix that asserts both a message-derived discriminator and its prohibition cannot be run to a single expected result, and an implementer reading AR-13 will write the code Y-3 removed.

### 3.4 Y-4 — Historical resolution preservation and its disclosure

**Confirmed resolved as design text. Owner approval is not recorded; A2 stays blocked. See Y-20.**

§4.2 keeps the stored characters, does not truncate, does not clear, does not require the user to edit them, does not refuse reopen, and on a re-resolve writes a public comment carrying the exact previous characters with a `Superseded` audit row whose `Detail` is null and whose `OldValue` is a length marker. The disclosure is explicit: the comment is `IsInternal = false` because viewers who cannot add an internal note cannot see internal comments and can see the resolution today. Source-verified: `TicketDetailService.cs` lines 186–193 filter internal comments when `CanAddInternalNote` is false, and `CanAddInternalNote` is requester-or-assignee with the requester excluded (`TicketDetailService.cs` lines 181–185). `lblResolution` is not permission-gated (`TicketDetail.aspx.cs` line 249).

The disclosure is a **new** one: the previous resolution stops being visible on the ticket when a new one replaces it, and this path makes it visible to every viewer who can see the ticket, including the requester, through the comment list ordered by `CreatedOn` (`TicketDetailService.cs` lines 195–206). The reconciliation discloses it and makes it an owner acknowledgement (§8, "a public comment appears on the ticket"). That is the right treatment. It is not recorded, so A2 is not authorised.

Also confirmed: the audit boundary. `ResolutionText` has no `[StringLength]` and the column is unbounded; `TicketAuditLog.Detail` is 1000 (`TicketAuditLog.cs`, `[StringLength(1000)]`). The reconciliation's reason for the cap is the audit column, not the resolution column, and it says so.

### 3.5 Y-5 — Audit validation contract, caller behaviour and atomic rollback

**Confirmed resolved.**

§4.3.1's five maxima — `ActionType` 50, `OldValue` 200, `NewValue` 200, `Detail` 1000, `ActorCode` 50 — match the entity exactly: `TicketAuditLog.cs` declares `[StringLength(50)]` on `ActionType`, `[StringLength(200)]` on `OldValue` and `NewValue`, `[StringLength(1000)]` on `Detail`, and `[Required] [StringLength(50)] [Column(TypeName = "varchar")]` on `ActorCode`. The one contract is coherent: callers return `Fail` before mutation, `AddAudit` throws before adding the entity, the existing `catch (Exception)` rolls back and rethrows, no page gains a handler, and `CreateTicket`'s direct `Created` row follows the same no-truncation rule.

Source-verified: `AddAudit` is `private static void` (lines 599–622) calling `Truncate` on four fields and assigning `ActorCode = actorCode` raw; `Truncate` (624–632) returns a cut string with no signal; `Commit` (634–640) is one `SaveChanges` then `Commit`. Deleting `Truncate` is safe: its only references are the four inside `AddAudit`; a grep for `Truncate(` under `AnmolDristi/ITSM` returns those lines only. `CreateTicket` contains no `Truncate` call at all.

The caller table in §4.3.2 matches what the methods actually do today. `ApproveTicket` assigns `approval.Comments = comments` with no trim and passes the same value to `AddAudit` (lines 316–318 and 336–338); `RejectTicket` stores `reason.Trim()`; `AddComment` stores `commentText.Trim()` and passes `(isInternal ? "[Internal] " : "") + commentText.Trim()` (lines 490–503), which is the 11-character prefix the 989 user-character cap is derived from.

One consequence is accepted rather than fixed, and correctly labelled: an over-length status or priority name now hits the backstop and rolls the transition back where it was previously truncated and committed. `MST_TicketStatus.Name` and `MST_Priority.Name` are `maxLength: 100` (migration lines 255 and 218), so the reachable case is a name between 201 and 1000 characters, which the schema forbids. The behaviour change is theoretical and the contract is still the right one.

---

## 4. Y-7 / Y-8 — Completeness of the untrusted output sinks

**Confirmed. The enumeration is complete for every sink I could find.**

I enumerated every assignment and binding in the four ITSM pages independently of the reconciliation, then compared:

| Sink class | Locations found | In §4.4 |
|---|---|---|
| `Label.Text` in `LoadTicket` | `TicketDetail.aspx.cs` 231, 233, 234–237, 238–240, 241, 242, 244–246, 249, 251, 254, 259, 260 | Yes, each named |
| Status badge class | `TicketDetail.aspx.cs` 252–253 (`"badge-status badge-" + dto.StatusName.ToLower().Replace(" ", "-")`) | Yes, whitelist from the raw name |
| Repeater bindings | `TicketDetail.aspx` 361 `CreatedByName`, 381 `ApprovalLevel`, 382 `ApproverName`, 386 `ApprovalStatus` text and class; 367 and 394 already encoded | Yes |
| `lblMessage` | `TicketDetail.aspx.cs` 393, `TicketList.aspx.cs` 336, `CreateTicket.aspx.cs` 454, `ApprovalConfig.aspx.cs` 155, with clears at 388, 331, 449, 150 | Yes, all four |
| `TicketList` template bindings | `TicketList.aspx` 141 `Eval("TicketNumber")`, 151 `PriorityName`, 157 `StatusName`; `BoundField`s 145–165 | Yes |
| `TicketList` badge classes | `TicketList.aspx.cs` 144–160 reads `lblPriority.Text.Trim().ToUpper()`; 163–168 reads `lblStatus.Text.Trim().ToLower().Replace(" ", "-")` | Yes, and correctly requires the raw data item instead |
| `ApprovalConfig` labels | `ApprovalConfig.aspx.cs` 126–128 | Yes |
| Mail body and subject | `TicketService.cs` 478 and 502 | Yes |
| Constants, counts and dates | `lblTicketCount` 296, `lblCreatedOn` 243, `lblActive` 124–125, `lblCsatScore` 259 | Yes, each classified |
| `ListItem` text and `BoundField`s | status/agent/priority/employee dropdowns, `gvAudit` | Yes, framework encoding, unchanged |

The three `TicketList` template bindings and the two `ApprovalConfig` labels were the substance of Y-7 and Y-8; both are now named with the rule attached, and the badge-class rule correctly forbids building a class from already-encoded text.

**Y-19** is the remaining defect in this area, and it is a documentation gap rather than a missing encode: Revision 2's A1 requires the `CreateTicket` `InnerText` / `Attributes` rendering to be **recorded and not changed**, and §4.4 — which the reconciliation presents as "the A1 enumeration" — does not carry that row. See §6.

---

## 5. The reconciliation's dispositions, confirmed or qualified

### 5.1 Y-6 through Y-12

| ID | Result | What I verified |
|---|---|---|
| Y-6 Untrimmed approval comment | **Confirmed** | `ApproveTicket` assigns `approval.Comments = comments` untrimmed (line 318) and passes `comments` to `AddAudit` (338). `TicketApproval.Comments` is 500 (migration line 139). §4.3.2's "the string checked is the string stored" closes it. |
| Y-7 TicketList sinks | **Confirmed** | Section 4 above. `TicketList.aspx` 141, 151, 157 unencoded; `RowDataBound` 144–168 derives classes from `Label.Text`. |
| Y-8 Tenant badge, CSAT, ApprovalConfig | **Confirmed** | `TicketDetail.aspx.cs` 231, 260 (inside the branch that starts at 256), and `ApprovalConfig.aspx.cs` 126–128. The reconciliation's correction of the CSAT citation from 256 to 260 is right: 256 opens the `CsatScore.HasValue` branch, 260 is the assignment. |
| Y-9 Page does not state the maximum | **Confirmed open** | `TicketDetail.aspx` `txtResolution` has no `MaxLength` and its label reads `Resolution Notes *` with no limit. §4.2.4's required label text is the C-18 surface; it is a requirement, not a shipped page. |
| Y-10 A2 lacks a `Resolved` precondition | **Confirmed** | `ChangeStatus` returns before the resolution branch when `currentStatus.IsTerminal` (lines 73–77). `MST_TicketStatus` has both `IsTerminal` and `IsActive` (migration 256–257), so the read is implementable. |
| Y-11 No read for other `TicketNumber` writers | **Confirmed** | The only assignment in the tree is `TicketService.cs` line 172. `DbConn` is shared per `AGENTS.md` §1, so the repository search is the source half only. Read 22 is the right instrument. |
| Y-12 Prefix cut above two characters | **Confirmed as a code fact, disputed as a reachable scenario** | Lines 364–368 do trim and cut. But `RequestType.Prefix` is `[Required] [StringLength(2)] [Column(TypeName = "char")]` (`RequestType.cs` lines 25–28) and `char(2)` `NOT NULL` (migration 243), so the fallback and the cut cannot execute against an undeformed schema. See Y-21. |

### 5.2 Y-13 through Y-16

| ID | Result | What I verified |
|---|---|---|
| Y-13 `ResolveInitialStatusId` is per flag pair | **Confirmed** | The method takes one `bool` (lines 404–432), passed as `requiresManagerApproval \|\| requiresItHeadApproval` from line 166. Two types with the same pair produce one lookup. Recording per pair with the type names listed is the correct narrowing. |
| Y-14 `ActorCode` is the fifth bounded field | **Confirmed** | `ActorCode = actorCode` at line 619 is outside `Truncate`; entity is `[Required] [StringLength(50)] varchar`; migration `maxLength: 50`. An over-length code fails EF validation rather than being cut. |
| Y-15 Notification body and subject | **Confirmed** | Body append at `TicketService.cs` 478 writes `ticket.TicketNumber` raw while the title on 479 is `WebUtility.HtmlEncode`d; the subject at 502 concatenates the number. The corrected citation (478, not 490–503) is right. |
| Y-16 TC-32 belongs in the owner-decision list | **Confirmed** | TC-32 is the 1001-character resolution refusal, the same acknowledgement as TC-06. Listing it changes no expected result. |

### 5.3 Z-1 through Z-5, TG-1 through TG-6

All eleven are present, each with a source citation, a gate, an approval state and a test ID, and none is downgraded. I re-checked the two whose source claims matter most:

- **Z-3 / TG-6** — the proposed lock is inside the transaction opened at `TicketService.cs` line 91 and released at `transaction.Commit()` (line 287). A three-attempt retry can take it three times. `@timeoutMs` is unnamed in both documents, so TC-74 cannot yet accept an enabled rule. Correct as stated.
- **Z-5** — Revision 2 §9 has no count of `ResolutionText`, `CommentText` or `TicketAuditLog.Detail` values above the new caps. Reads 19–21 are the right instrument, and §8's acknowledgement is correctly marked uninformed without them.

The concurrency note the reconciliation carries forward is also correct: `ApproveTicket` and `RejectTicket` set `ticket.ModifiedOn` unconditionally (lines 335 and 434) after `LoadForTicket` writes `RowVersion.OriginalValue` (line 583), and `TicketApproval` has no concurrency token. TC-45 stays the test, and stays NOT RUN.

### 5.4 Confirmed with no further change

X-2, X-3, X-5 and the F-06 through F-10 verdicts hold on re-reading. Two citations I re-derived rather than accepted:

- **X-3** — the internal prefix `"[Internal] "` is 11 characters (`TicketUpdateService.cs` line 502), so 1000 − 11 = 989. Correct.
- **X-2** — the only `ExecuteSqlCommand` in `AnmolDristi/ITSM` is the notification insert at lines 497–503. Correct.

---

## 6. Remaining findings

### 6.1 Blocking

#### Y-17 — Y-3's retry rule is contradicted by AR-13 and TC-52

| Field | Record |
|---|---|
| Source-verified | `ITSM_TECHNICAL_REMEDIATION_DESIGN_REV2.md` §6 AR-13 and §10 TC-52 both still require the ticket-number index name, obtained from `SqlException.Message`. The reconciliation §4.1.5 forbids exactly that, and its own TC-62 asserts "The predicate does not read the exception message". |
| Why it blocks | AR-13 is the disposition an implementer reads for the retry's gating condition; TC-52 is the acceptance case. Y-3 was raised because SQL Server embeds the index identifier in a message whose surrounding words follow the instance language. Leaving two governing texts on the old rule means the defect can be rebuilt from the design that was supposed to remove it. |
| Correction required | Restate AR-13 with the 2601/2627-plus-existent-row probe and delete the index-name clause; restate TC-52's expected result to match §4.1.5 and TC-62, or mark TC-52 superseded by TC-62 explicitly. Add TC-52 to the reconciliation §3.6 list of AR corrections applied. |
| Gate | B. Blocking. |
| Approval evidence | None. D-4 stays Pending. |

### 6.2 Material

#### Y-18 — Read 4a has two incompatible enablement rules

| Field | Record |
|---|---|
| Source-verified | §4.1.7: "Read 4a must be zero before the index is created, otherwise a case-insensitive index cannot be built." §4.1.8: "4a | Groups of two or more `TicketNumber` values that compare equal under `Latin1_General_CI_AS` ... | Zero groups, or a separate data authorisation." |
| Why it matters | The index "uses the column collation" (§4.1.7). If that collation is case-sensitive, groups counted under `Latin1_General_CI_AS` do not collide and the index builds. If it is case-insensitive, a bare authorisation does not make it buildable; the rows must be rewritten or removed. §4.1.8's cell as written can send a release plan into a failing `CREATE UNIQUE INDEX`. |
| Correction required | Make 4a's enablement conditional on the collation read (read 9): zero under the target index collation, or a data authorisation that rewrites or deletes the rows. Keep 4b's wording, which already says "rewrites them". |
| Gate | B. |

#### Y-19 — The A1 sink enumeration drops Revision 2's "record and do not change" group

| Field | Record |
|---|---|
| Source-verified | `CreateTicket.aspx.cs` line 331 `btn.InnerText = impact.Name`, 350 `btn.InnerText = urgency.Name`, 376 `name.InnerText = cat.Name`, 406 `name.InnerText = rt.Name`, plus `Attributes["aria-label"]` at 330 and 349. Revision 2 §7 A1 requires these to be "Record[ed], and do not change". Reconciliation §4.4, presented as "the A1 enumeration", lists `BoundField`s, `ListItem` text, `lblActive` and the internal-comment marker, and omits this group. |
| Source verdict | The rendering is safe: `HtmlContainerControl.InnerText` encodes on assignment and attribute values are encoded by the framework writer. The client script is also safe — `CreateTicket.aspx` 537–582 builds options with `document.createElement` and `o.text`, and its three `innerHTML` assignments (539, 565, 743–746) are constant strings. `hfCategoryData` is `JavaScriptSerializer` output written to a `HiddenField` (line 262), whose value is attribute-encoded. |
| Why it matters | This is the one part of the sink work whose safety argument is "the framework encodes it, leave it alone". Dropping it from the enumeration §5 calls complete means a later A1 authorisation cannot tell whether those eight assignments were reviewed or missed. |
| Correction required | Add the group to §4.4 with the reason, and add the client-script rows (`o.text`, constant `innerHTML`) as reviewed-and-unchanged. |
| Gate | A1. Required before A1 can be called complete. |

#### Y-20 — The preservation comment's `CreatedOn` and `TicketId` are not specified

| Field | Record |
|---|---|
| Source-verified | `TicketComments` (migration lines 166–180): `TicketId` `nullable: false`, `CommentText` `nullable: false` and unbounded, `CreatedByCode` `nullable: false` `maxLength: 50`, `CreatedOn` `nullable: false`, `IsInternal` `nullable: false`. §4.2.4 step 1 names only `IsInternal`, `CreatedByCode` and `CommentText`, and states the row is "not submitted through `AddComment`". `AddComment` is the code that supplies `CreatedOn = DateTime.Now` (`TicketUpdateService.cs` lines 490–498) and `TicketId`. |
| Why it matters | A row built to that instruction carries `default(DateTime)`, which is below SQL Server `datetime`'s 1753 floor; the insert fails at `Commit`, the existing `catch (Exception)` rolls back, and the re-resolve fails after the actor has supplied a valid new resolution. §4.2.4's own step 3 requires "the transition's single `now`", so the timestamp is in scope and simply not assigned to this row. |
| Correction required | Name `TicketId` and `CreatedOn = the transition's single now` in §4.2.4 step 1. |
| Gate | A2. |

#### Y-21 — Y-12's premise is not reachable in the tracked schema

| Field | Record |
|---|---|
| Source-verified | `RequestType.cs` lines 25–28: `[Required] [StringLength(2)] [Column(TypeName = "char")] public string Prefix`. Migration line 243: `Prefix = c.String(nullable: false, maxLength: 2, fixedLength: true, unicode: false)`. `Code` is `maxLength: 10` (line 241), `[Required] [StringLength(10)]` in the entity. |
| Why it matters | `requestType.Prefix ?? requestType.Code ?? "XX"` and `if (prefix.Length > 2) prefix = prefix.Substring(0, 2)` (lines 364–368) can only execute if the deployed column is nullable or wider than two characters. The reconciliation's Y-12 row asserts "A null prefix and a code of three or more characters therefore allocates today", and its §8 acknowledgement and TC-71 are built on that case. Neither the acknowledgement nor read 6 records **the deployed column definition**, which is the only thing that makes the fallback reachable. The genuinely reachable short-prefix case is different and is covered: a `char(2)` value such as `'C '` trims to length 1. |
| Correction required | Either state that the longer-prefix and null-prefix paths are reachable only through deployment drift, and add the deployed `MST_RequestType.Prefix` definition (nullable? length? fixed?) to read 6, or withdraw the longer-prefix half of the acknowledgement and TC-71. Do not leave the premise unqualified. |
| Gate | B. |

#### Y-22 — Encoding is not redaction, and A1 reads as if it were

| Field | Record |
|---|---|
| Source-verified | `CreateTicket.aspx.cs` 203–215: `ShowMessage(ex.Message + " Reference: " + diag.CorrelationId, false)` for both `InvalidOperationException` and `ArgumentException`. `TicketList.aspx.cs` 301 concatenates `ex.Message`. `ApprovalConfig.aspx.cs` 80 and 106 pass `ex.Message`. Reconciliation §4.4 says "Encode. This covers `ex.Message` at lines 208 and 215." |
| Why it matters | Encoding stops markup injection; it does not stop an internal exception message from being displayed. `customErrors` and `httpErrors` are `Web.config` facts, and `AGENTS.md` §3.4 forbids reading that file, so the mitigation cannot be verified in this repository — only recorded as unknown. A1's description currently reads as a complete treatment of those four sinks. |
| Correction required | In A1, separate "encode" from "disclose": state that exception text remains user-visible where the catches at `CreateTicket.aspx.cs` 203, 215, `TicketList.aspx.cs` 301, and `ApprovalConfig.aspx.cs` 80, 106 fire, that `customErrors`/`httpErrors` are unread, and that closing the disclosure is not in A1. |
| Gate | A1 statement; no new business decision. |

### 6.3 Precision

#### Y-23 — `LEN()` and `String.Length` disagree on trailing spaces in the diagnostic counts

§4.1.2's predicate uses `LEN(TicketNumber) = 10`. `TicketNumber` is `varchar(10)`, so a stored ten-character value ending in a space has `LEN` 9 and is excluded, while the C# rule's `Length == 10` includes it and the digit test then rejects it. Allocation is unaffected. Reads 4b and 4c are written in the same terms as the predicate, so those rows are undercounted in the evidence that gates the index. Say whether the counts use `LEN` or `DATALENGTH`, and state that trailing-space rows are counted under 4c regardless of length.

#### Y-24 — The `UPPER()` agreement argument is anchored to the wrong read

§4.1.1 rule 4 justifies SQL `UPPER` by read 6 ("every active prefix is two ASCII alphanumerics"). §4.1.2 applies `UPPER` to stored `TicketNumber` characters, which read 6 does not cover, and `UPPER` is evaluated under the column collation before `COLLATE Latin1_General_BIN` is applied to the result. On a non-Latin1 collation a stored lower-case `i` can upper-case to a character that fails the `BIN` comparison, making the SQL read under-inclusive — the one direction §4.1.2 says it must not fail in. Tie the agreement to read 9 and require a non-Latin1 result to force read 4b to zero before enablement.

#### Y-25 — Two shapes for the same `OldValue` field

§4.2.3 keeps Revision 2's `OldValue` for the superseded resolution row as the previous `ResolvedOn` **and `ModifiedByCode`**; §4.2.4's marker is `omitted;length={n};resolvedOn={stamp}`. Both are `OldValue` (200 characters) on a `Resolution` row. State whether `ModifiedByCode` is required in the over-length path too, so one query can read both rows.

---

## 7. Risks introduced by the reconciliation

| ID | Risk | Evidence | Required record |
|---|---|---|---|
| Z-6 | **A1 is no longer self-sufficient in Revision 2.** The complete sink list now exists only in the reconciliation §4.4. Revision 2 §7 A1 still says "Encode every untrusted string assigned to `Label.Text` in `LoadTicket`" without naming the `TicketList` template bindings, the tenant badge, the CSAT label or the ApprovalConfig labels. A later authorisation that cites Revision 2 alone ships an incomplete list. | `ITSM_TECHNICAL_REMEDIATION_DESIGN_REV2.md` §7 A1 versus reconciliation §4.4. | The reconciliation already requires the authorisation to name §4.4. Add that Revision 2 §7 A1 is superseded for encoding by §4.4, so the design document set has one answer. |
| Z-7 | **The public preservation comment is a new disclosure channel, and no test observes it.** `IsInternal = false` puts the previous resolution on the comment list for every viewer who can see the ticket, ordered by `CreatedOn`. | `TicketDetailService.cs` 186–206; `TicketDetail.aspx` 356–370. | A rendered-page observation as a viewer who is neither requester nor assignee and cannot add an internal note. TG-7. |

---

## 8. Test matrix TC-01 through TC-78

**Inventory.** `ITSM_TECHNICAL_REMEDIATION_DESIGN_REV2.md` §10 defines TC-01 through TC-59. The reconciliation §6.2 defines TC-60 through TC-78. A repository-wide identifier grep returns exactly 78 distinct IDs across the two documents, contiguous, with no duplicates and no gaps. Every disposition in both documents is **NOT RUN**, and both documents exclude UAT and LIVE from every row.

**Coverage.** Each new row maps to the finding it closes: TC-60 to Y-1/TG-1, TC-61 to Y-2/TG-2, TC-62 to Y-3, TC-63 to Y-4/TG-4, TC-64 to Y-5/TG-3/Y-14, TC-65 to Y-6, TC-66 to Y-7, TC-67 to Y-8, TC-68 to Y-9, TC-69 to Y-10, TC-70 to Y-11, TC-71 to Y-12, TC-72 to Y-13, TC-73 to Y-15, TC-74 to Z-3/TG-6, TC-75 to C-27, TC-76 to Z-5, TC-77 to Z-4, TC-78 to the index-before-allocator order. No finding in the second review is without a case.

**Dependencies.** The reconciliation's need-class lists are consistent with the rows: ten new cases need a disposable database, three need a named-environment read, and TC-77 and TC-78 are document checkpoints. Two corrections are needed:

1. **TC-52 contradicts TC-62** on the retry discriminator. See Y-17. Both cannot pass.
2. **TC-16 and TC-60 are the only cases that exercise formatting**, and neither starts from a stored value whose last four characters are a sign or a space **in a lower-case prefix**. TC-60's fixtures (`-123`, `+123`, ` 123`, `0000`, `12a4`) are correct for Y-1; the Y-2 single-row case is TC-61. The combination is not covered, and it is the case in which a dropped row and a signed row interact. This is a coverage note, not a blocking gap, because neither input survives the digit test.

**Status reporting.** Every row is NOT RUN in both documents, and no row was executed for this review. Two rows — TC-77 and TC-78 — are document checkpoints and must not be counted as product coverage in a readiness statement.

---

## 9. Test coverage gaps

| ID | Gap | Why it matters | Proposed case |
|---|---|---|---|
| TG-7 | Nothing observes the preservation comment through the page as a viewer who cannot add an internal note. TC-63 checks rows in a database. | Z-7. The disclosure the owner is asked to acknowledge is a *visibility* change; a row-level assertion does not demonstrate it. | A rendered or service-level read as a third-party viewer: the previous resolution appears, and an internal note does not. |
| TG-8 | No case covers a stored ten-character `TicketNumber` that contains a space — trailing or embedded — under the allocation read and the queue. TC-50 covers nine-character values only. | Reads 4b and 4c count these rows, and Y-23 shows the count and the C# test use different length semantics. | A disposable database: the row stays stored, remains openable and searchable, does not advance the sequence, and is counted by 4c. |
| TG-9 | No case covers a deployed `MST_RequestType.Prefix` that is nullable or wider than `char(2)`, which is the only way Y-12's `Code` fallback is reachable. | Y-21. Read 6 records prefix length and `Code`, not the column definition. | A source inspection of the deployed column definition, recorded with environment, statement, timestamp and reviewer. |

---

## 10. Gate readiness

No gate is ready. The table states what each gate still needs, not what it is missing for style.

| Gate | Technical state | Still required | Ready for authorisation |
|---|---|---|---|
| **A1** — encoding, panels, `OrderBy`, guarded rollback | Sink rules are complete and correct for every stored-text sink in the four pages. Badge classes are correctly derived from raw data. `itsm-prewrap` is specified. Panel binding and the dispatch `OrderBy` are unchanged from Revision 2. | Y-19 (record the `CreateTicket` `InnerText`/`Attributes` group), Y-22 (disclosure statement, not an encode), Z-6 (one authoritative list), the AR-9 / Z-4 anti-forgery disclosure, the blank-agent count for the fall-through half, and a change authorisation that does not exist. | **No.** |
| **A2** — lengths, audit contract, resolution history | The audit contract matches the entity exactly and has one channel. The resolution history now has a backward-compatible path for a stored value over 1000 characters, and no path clears or truncates it. | Y-20 (the comment row's `CreatedOn`), Y-25 (marker shape), the §8 acknowledgements for the resolution maximum, the comment-capacity reduction and the preservation disclosure — all Not recorded — and reads 19–21. A2 also needs read 1's `Resolved` `IsTerminal`/`IsActive` flags, because A2 is not promoted without an active non-terminal `Resolved` row. | **No.** |
| **A3** — fail-closed approve and reject | Behaviour is unchanged and correctly disabled. `ResolveInitialStatusId` is excluded. Read 1 is narrowed to one result per approval-flag pair, which matches the single `bool` the method takes. | Read 1, attached to a named environment with statement, timestamp and reviewer. Nothing else. A failed read withholds promotion; it is never a reason to enable the branch. | **No.** Disabled. |
| **B** — allocation, lock, unique indexes, retry | The sequence rule, the fold, the single-row count, the format ban, the probe and the index-before-allocator order are specified and implementable. | **Y-17 first**: AR-13 and TC-52 still teach the removed discriminator. Then Y-18 (read 4a's two rules), Y-21 (prefix reachability and the column definition), Y-23/Y-24 (count semantics and fold anchoring), reads 4a/4b/4c/5–11 and 22, a named canonical case, a named lock timeout in milliseconds, one schema mechanism, the shared-sequence acknowledgement, and service-owner acceptance of every prefix the exact-2 rule blocks. D-4 and D-9 stay Pending. | **No.** |
| **C** — business decisions | No behaviour in A1, A2, A3 or B depends on a Gate C ruling. | A written owner ruling in the register for each decision. None exists. | **No.** All 18 Pending. |

Sequencing, as the documents state it, is sound and I have no correction to offer: A1 before A2 is a release preference, not a technical prerequisite — A2's refusal strings are fixed text assigned to `lblMessage`, which A1 encodes. A3 is independent of both and disabled. B is independent of A1/A2/A3 but depends on the schema mechanism, and its index must precede the build that removes the `RequestTypeId` filter. That ordering is right and TC-78 records it.

---

## 11. D-1 through D-18

Read directly from `ITSM_BUSINESS_DECISION_REGISTER.md` §10 (lines 564–590) on 24 November 2026. The `Owner ruling` column is empty on all 18 rows. The `Date` column is empty on all 18 rows. The `Status` column reads `Pending` on all 18 rows. The two additional rows for `Section 3` and `Section 4.4` are also empty and Pending. No approval email, signature or dated ruling accompanies the table, and §10's own preamble says "No names are recorded. No row is approved."

| ID | Status | Ruling | Date | This review |
|---|---|---|---|---|
| D-1 | Pending | empty | empty | Stays Pending. AR-16 residual unchanged. |
| D-2 | Pending | empty | empty | Stays Pending. A2's reopen path adds no window and no clock reset. |
| D-3 | Pending | empty | empty | Stays Pending. |
| D-4 | Pending | empty | empty | Stays Pending. The Gate B method matches the register's *recommendation*, which is not a ruling. |
| D-5 | Pending | empty | empty | Stays Pending. No D-id owns the comment box. |
| D-6 | Pending | empty | empty | Stays Pending. |
| D-7 | Pending | empty | empty | Stays Pending. No SLA column. |
| D-8 | Pending | empty | empty | Stays Pending. Y-15 does not choose a public origin. |
| D-9 | Pending | empty | empty | Stays Pending. Shared prefixes are not a flag policy. |
| D-10 | Pending | empty | empty | Stays Pending. |
| D-11 | Pending | empty | empty | Stays Pending. |
| D-12 | Pending | empty | empty | Stays Pending. |
| D-13 | Pending | empty | empty | Stays Pending. |
| D-14 | Pending | empty | empty | Stays Pending. Blank-agent fall-through stays conditional. |
| D-15 | Pending | empty | empty | Stays Pending. Null `CompanyId` stays refused. |
| D-16 | Pending | empty | empty | Stays Pending. |
| D-17 | Pending | empty | empty | Stays Pending. The 1000-character maximum and the preservation path are acknowledgements, not a ruling on the minimum. |
| D-18 | Pending | empty | empty | Stays Pending. |

No formal approval evidence exists for any of the 18. No recommendation in the register is treated as a ruling by this review. §3 and §4.4 are Pending and are not additional decision numbers.

---

## 12. Compatibility, transactions, concurrency and security

**Backward compatibility.** Historical numbers are not rewritten, including sub-ten-character values and signed values already stored. Historical audit rows are not updated or deleted. Pre-change resolutions are not backfilled. The transition map is unchanged. Session keys stay `WORKMAN`, `USERID` and `BRANCH`. A rolled-back create consumes an identity value and that is not repaired. All of this is consistent with Revision 2 §11 and I found no contradiction.

**Transactions.** Every mutating method keeps one transaction and one `SaveChanges` inside `Commit` (`TicketUpdateService.cs` 634–640). `CreateTicket` keeps one transaction across the ticket insert, the `Created` audit, the approval rows and the `NotificationQueue` insert (`TicketService.cs` 91–289). A2's preservation comment is inside that transaction, which is correct and is what makes it atomic. The unguarded `Rollback()` in the six update methods and in `CreateTicket` (lines 119–122, 185–188, 254–257, 349–352, 449–452, 513–516 and 292–295) is still in the source; AR-6 names all seven sites and assigns `CreateTicket` to Gate B with the retry, which is the correct allocation. The reconciliation's §4.1.5 does not restate it, so the AR-6 text is load-bearing — noted, not a defect.

**Concurrency.** `Ticket.RowVersion` covers ticket mutations. `TicketApproval` has no concurrency token, so two approvals of the same ticket are serialized by the ticket row only when the ticket row is updated; a partial approval that leaves the ticket in `Pending Approval` writes `ModifiedOn` anyway (line 335), so the token is still touched. TC-45 remains the case that proves this, and remains NOT RUN.

**Security.** The anti-forgery absence under `AnmolDristi/ITSM` is unchanged and correctly kept outside every gate with no owner and no date. `Web.config` was not read, so the ViewState MAC setting and the `customErrors`/`httpErrors` configuration remain unknown — which is also why Y-22 must be stated as an unresolved disclosure rather than a mitigated one. Output encoding, once implemented as §4.4 specifies, closes stored-markup injection at every sink I enumerated; it does not close exception-text disclosure, and it does not add a request-verification token.

---

## 13. Explicit implementation authorization status

**NO IMPLEMENTATION AUTHORIZATION IS GRANTED BY THIS REVIEW.**

- No application file may be edited on the authority of this review.
- No SQL file, migration, index, trigger, or configuration change may be written or executed on the authority of this review.
- Gates A1, A2, A3, B and C are closed. None is ready for an authorisation discussion until the blocking and material findings above are dispositioned in the documents.
- **Y-17 must be fixed before Gate B is discussed at all**, because two of its own governing texts still instruct an implementer to rebuild the defect Y-3 removed.
- A3 must not be enabled in order to observe a live status table. Read 1 precedes promotion, not the reverse.
- Gate B must not be written. The sequence design is specified so a later authorisation has an implementable rule; the specification is not the authorisation.
- A2's owner acknowledgements (§8 of the reconciliation, §13 of Revision 2) are not recorded and cannot be inferred from a recommendation in the register.
- Gate C retains D-1 through D-18 as Pending. No register recommendation is a ruling.
- Every test disposition, TC-01 through TC-78, is **NOT RUN**.
- No SQL was executed and no UAT or LIVE connection was opened to produce this file.
- `AnmolDristi/Web.config` was not read, and its existing working-tree modification was not touched.

---

## 14. Checks performed and not performed

**Performed.**

- `git rev-parse --short HEAD` → `f1f4bdb`; `git status --porcelain=v1` before and after this file was written.
- Full read of the reconciliation's 629 lines; targeted reads of Revision 2's §1–§2, §6–§10 and §13–§15; §1–§3 of the second review; all 99 lines of the traceability matrix; register §10 and §11.
- Source inspection of every file and range cited in this review, at `f1f4bdb`, including the four ITSM pages and their code-behind, both services, both entities and the initial migration.
- Independent enumeration of `.Text =`, `Eval(`, `BoundField`, `InnerText`, `Attributes[` and `innerHTML` across the ITSM UI, compared sink by sink against §4.4.
- Repository-wide `TC-nn` identifier extraction from all five inputs, to establish 78 distinct contiguous IDs and to find the TC-52 / TC-62 conflict.
- Cross-check of the five audit field maxima against `TicketAuditLog.cs`, and of the `TicketComments` and `MST_RequestType` column definitions against the migration.

**Not performed, and therefore not reported as passes.**

- No build. MSBuild was not invoked.
- No test. TC-01 through TC-78 were not executed.
- No SQL, no UAT connection, no LIVE connection. No deployed row, collation, grant, job, prefix definition or population count was observed.
- No page was rendered and no site was started, so TC-66, TC-67 and TC-68 remain source requirements rather than demonstrated pages.
- `AnmolDristi/Web.config` was not read, so request validation, `customErrors`, `httpErrors` and the ViewState MAC setting remain unknown.
- The .NET and EF behaviours cited (`NumberStyles.Integer`, `D4` padding, `LEN` trailing-space semantics, `InnerText` encoding, attribute encoding, `DbUpdateException` wrapping) are the documented contracts of those APIs. They were not re-executed in a harness for this file.
- No approval of any kind is implied by a confirmed line number or a closed citation.

---

## 15. Repository status

This review created one file and changed nothing else.

| Item | State |
|---|---|
| File created | `ITSM_FREEBUFF_REMEDIATION_REVIEW_REV3.md`, repository root. |
| `HEAD` | `f1f4bdb` — unchanged. |
| Input documents | `ITSM_FREEBUFF_REMEDIATION_RECONCILIATION_REV2.md`, `ITSM_TECHNICAL_REMEDIATION_DESIGN_REV2.md`, `ITSM_FREEBUFF_REMEDIATION_REVIEW_REV2.md`, `ITSM_SOURCE_TO_FINDING_TRACEABILITY.md`, `ITSM_BUSINESS_DECISION_REGISTER.md` — read only, not modified. |
| Application, SQL, migration, configuration | Not modified. |
| `AnmolDristi/Web.config` | Not read, not modified. Its pre-existing ` M` working-tree entry is present before and after this work. |
| Pre-existing untracked files | The ITSM document set and `Database/Deployments/ApprovalRequestActivityCounters/`, `Database/Deployments/ApproverKmEdit/D_Rollback_ApproverKmEdit_UAT.sql` are still present and untouched. |
| Commit, push, tag | None. Nothing was staged. |
| SQL execution, environment connection | None. |

Because the input documents are untracked, Git cannot prove they are byte-identical to their earlier state. They were opened for reading only, and this review wrote no path other than the one file named above.

---

**No implementation authorization is granted by this review.**
