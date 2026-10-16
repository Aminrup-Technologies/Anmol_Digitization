# Recovered conflict register — F-01 to F-18

**Status:** `recovered-evidence` · `open-items-preserved`
**Recovered:** 2026-10-15, during documentation review of this repository
**Nature:** evidence preservation. **Nothing is decided in this file.** It preserves a
register that existed only as conversation output, corrects that register's own summary
numbers against its own `Status` column, and records the decision register that the same
source exposes. Every `BLOCKED` entry keeps its owner decision open.

---

## 1. Provenance of this recovery

| Field | Value |
| --- | --- |
| Source kind | Freebuff desktop client cache of a conversation thread (application data, not a repository artifact) |
| Cache file | `C:\Users\ANMOL\AppData\Roaming\freebuff\Cache\Cache_Data\f_000113` |
| File size / mtime | 1,099,697 bytes · 2026-10-15 18:21:48 +0530 |
| Thread id | `1e2a93ff-f9f6-4a52-83d7-d3393029c6c3` |
| Thread title | TA/DA Workflow Authority Audit |
| Thread project | `D:\PersonalRnD\AminrupWorks\Anmol_Digitization` |
| Register location | message sequence **6** (assistant report), section `# 4. CONFLICT REGISTER` |
| Decision register | message sequence **2** (authority audit report), decision rows `D-1`–`D-9` |
| Decision package | message sequence **4**, section `## 4. Decision D-3 — confirm the Production procedure pair` |
| Thread extent | `seq 1`–`seq 10`, `hasOlder=false`, `hasNewer=false` — one thread carrying five successive tasks |

Reproduce the extraction (PowerShell/Git-Bash, Python 3):

```
python -c "import json;d=json.load(open(r'C:/Users/ANMOL/AppData/Roaming/freebuff/Cache/Cache_Data/f_000113',encoding='utf-8'));t='\n'.join(x.get('text','') for x in d['messages'][5]['parts'] if x.get('kind')=='text');print('\n'.join(t.split('\n')[62:82]))"
```

### What is verbatim here, and what is added

- **Verbatim (quoted record):** the classification key, the register table itself, the
  report's own summary lines, and the `D-1`–`D-9` rows. Copied character-for-character,
  including the source's own wording — and its own arithmetic errors, which are *not*
  silently corrected in place; they are quoted and then reconciled in §4 below.
- **Added by this file (explanatory):** section headings, the provenance table, the
  reconciliation in §4, the exclusions in §5, and every sentence marked **Note —**.
- **Display transformations on quoted text** — the only deviations from the source's
  literal characters, each reversible:
  1. `> ` markers and blank `>` lines render a quotation (§3, §6). The source's own `- `
     list markers are kept as characters.
  2. A quoted line longer than this file's wrap width continues on the next line; the wrap
     point is a newline where the source has a space. No word, digit, or punctuation mark
     is changed, added, or dropped.
  3. In §6 the `D-3` verified-fact sentence keeps the source's italics but flattens its
     markdown link on `Production_B_Apply.sql` to a code span naming the same path.
  4. In §6 the source heading is quoted inside an inline code span instead of being
     emitted as a heading, so that it does not become a heading of this file.

### Recovery limitations

1. The source is a **client-side cache**, not a repository file. It can be evicted at any
   time; nothing here claims it will persist.
2. The register's `Evidence` column cites repository paths, line numbers, commits and
   hashes. This recovery reproduces those citations **as quoted records**; it does not
   re-verify them. Where the same claims were independently re-checked earlier, the
   check is named in the resolution text of the relevant row.
3. Text fidelity: the register rows (§2), the four summary lines (§3), the `D-1`–`D-9`
   rows and the two `D-3` quotations (§6) were each compared programmatically against the
   transcript and match character-for-character apart from the display transformations
   listed above. The rest of the original report's narrative — its other sections — is
   **not** reproduced here and remains unavailable anywhere in this repository.
4. The transcript also contains the assistant's internal reasoning. Reasoning is **not**
   quoted as register content; it is referenced only where §5 must explain an exclusion.

---

## 2. The register as recovered (verbatim)

Classification: **A** safe repository-only correction · **B** business/process owner · **C** IT Application Owner / DB Owner · **D** Release Owner / CAB · **E** live-environment evidence unavailable.

| ID | Sev. | Conflict | Evidence | Demonstrable? | Impact / dependencies | Class | Resolution + verification | Status |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| **F-01** | High | No agent-instruction file exists; every agent re-derives build, boundaries, and SQL ownership rules | `AGENTS.md`, `.cursor/`, `.cursorrules`, `CLAUDE.md` **all absent**; root `README.md` is one line; only vendor `CONTRIBUTING.md` under `AnmolDristi/Resources/*` | Demonstrable | Repeated wrong turns on mutation boundaries, baselines, and the `Web.config` boundary | **A** | Created root `AGENTS.md`; verified no existing equivalent and no higher-authority instruction file | **RESOLVED** |
| **F-02** | Medium | No documentation index; 378 tracked `.md` files with three mutually exclusive self-declared baselines and no supersession map | `docs/` contains 12 sub-trees; no `INDEX`/`README` at `docs/` root | Demonstrable | A reader cannot tell which document is current | **A** | Created `docs/INDEX.md`; verified every path it names resolves or is explicitly recorded as unavailable | **RESOLVED** |
| **F-03** | High | `L2_Auditor_Status_Transitions.md` (as-is capture 2026-09-15) states the Auditor terminal leaves header `Pending` and that Mark Ready is refused, with no note that RC-014B changed this | File §1, §6, §7 vs `usp_ProcessApprovalAction.sql` changelog `auditor_terminal_completed_status` (L67–72) present at `3779d185` | Demonstrable | An agent reading only this file would "fix" the certified behaviour | **A** (notice) + **B/D** (underlying) | Appended additive conflict notice; original text untouched | **NOTICE ADDED** |
| **F-04** | High | `Post_GoLive_Baseline.md` declares itself "the frozen reference point for all future TA/DA work" / "the Sprint 3 starting line"; it predates RC-017–RC-024 and the checkpoint change | File intro + closing "Freeze statement"; tags `47f3f65b`, `9ab2e588`, `3f60f966`, `bc87682f`; commit `e1ddf06` | Demonstrable | A future RC could be planned against a stale SQL registry (`usp_MarkRequestReadyForPayment` UAT 2026-09-15) | **A** (notice) + **B** (which baseline governs) | Appended additive conflict notice | **NOTICE ADDED** |
| **F-05** | High | Consolidated certification §3.1 records "Ready-for-Payment — PASS"; later records mark finalization BLOCKED pending owner approval | File §3.1 vs `L2_Auditor_Finalization_Contract.md` supersession notes (2026-10-12/13: "L2-R19: BLOCKED — OWNER APPROVAL REQUIRED") | Demonstrable | A reader could treat finalization as certified and shippable | **A** (notice) + **B/D** | Appended clearly separated appendix; certification body untouched | **NOTICE ADDED** |
| **F-06** | Medium | `Database/Deployments/L2R19_UAT/README.md` L48 states four rollback preimages are **missing**; they exist, are embedded in `D_Rollback`, and hash-verified | `D_Rollback_L2R19_Finalization_UAT.sql` L20, L2136, L2155 ("Four UAT preimages are embedded and hash-checked"); `docs/reviews/L2_Auditor_AsIs/L2R19_preimage_export_2026-10-13T071700Z/` with four `.utf16le.bin` files and `HASH_VERIFICATION.txt` (`match=True` ×4) | Demonstrable | An agent could re-export or paste over verified preimages | **A** | Corrected L48, L58, L75; kept every gate and the "do not substitute git/LIVE/RC014B" caution verbatim | **RESOLVED** |
| **F-07** | Low | Same file L75: "Only the Mark Ready preimage is present" | Same as F-06 | Demonstrable | Misleading package inventory | **A** | Corrected, with the gate values restated | **RESOLVED** |
| **F-08** | Critical | `usp_MarkRequestReadyForPayment.sql` L13 says "Not authorized for UAT or LIVE execution", yet the deployment checklist records a Production promotion of a body matching that hash | Candidate banner L1–14 vs `TA_DA_Deployment_Checklist.md` L800–812, L843 | Demonstrable as a document conflict; deployed state **not** verified here | Production may be running an unauthorized pair; selecting it is owner-reserved | **E** + **D** | Appended conflict notice to the package README, marked "quoted record"; no live contact | **DOCUMENTED** |
| **F-09** | Critical | **78 RC-018 release artifacts were deleted after the tag that certifies them.** `docs/reviews/Post_GoLive/RC018/` has 36 files at `9ab2e588`, 35 at `main` `13e1694`, **1 at `HEAD`**; `Database/Scripts/` 56 → **23** (`016`–`029*` absent) | `git show --name-status 9fdeb78` (78 deletions); `git ls-tree` at each ref; `9fdeb78` is a descendant of `9ab2e588` and an ancestor of `HEAD` | Demonstrable | Certified hash manifest and RC-018 scripts unverifiable from `HEAD`; RC-019 constraints 2 and 3 cannot be checked; numbering gap `015`–`019`, `021`–`029` | **D** | **Documented only** — restoring certified evidence is a Release Owner/CAB decision. Appended availability notice with the exact commit and counts | **BLOCKED** |
| **F-10** | Medium | `.gitattributes` was replaced by the same commit, removing `*.sql text eol=crlf`, `*.config text eol=crlf`, `*.xml text eol=crlf`, `*.md text eol=lf` and substituting `* text=auto` + VS boilerplate | `git show 9fdeb78 -- .gitattributes`; `diff` of the file at `9ab2e588` vs `HEAD` → DIFFERS | Demonstrable | Changing it now could mass-renormalize a working tree that already holds uncommitted work | **C** | **Not changed** — needs a controlled renormalization decision | **BLOCKED** |
| **F-11** | Medium | `RC019_Foundation_Gate.md` constraints 2 and 3 depend on RC-018 packaging absent at `HEAD` | File L67–73, L88–91, L114–117 | Demonstrable | The constraints read as satisfiable when they are not checkable | **A** (notice) + **D** | Appended availability notice with the verified file counts and the deleting commit | **NOTICE ADDED** |
| **F-12** | High | Two tracked definitions both write `FinalApprovedAmount`: `Approval/usp_RecalculateApprovalRequest.sql` (L162 read, L181 write) and `Utilities/usp_RecalculateApprovalRequest.sql` (L193 read, L229 write) | Both files; `L2_Auditor_Risk_Register.md` notes both | Demonstrable | Two writers of the single payable column; wrong pick changes money | **C** | **Documented only** (`docs/INDEX.md` C-3); no canonical body selected | **BLOCKED** |
| **F-13** | Critical | `usp_ProcessApprovalAction` and `usp_MarkRequestReadyForPayment` form incompatible pairs across refs; `bc87682f` has no reachable writer of the `Completed` its Mark Ready requires | `git show <ref>:…` for both files at `3779d185`, `bc87682f`, `HEAD` | Demonstrable | Any edit to either file is an unauthorized pair selection | **B/D** | **Documented only** (`AGENTS.md` §5, `docs/INDEX.md` C-1) | **BLOCKED** |
| **F-14** | High | FS Rule H-2 forbids the approval engine writing `Completed`; both candidate pairs write it. The specification's own approval table is unsigned | FS L375, L436–445, L1301 (G-05 Critical); TD L1134–1136 ("Forbidden"); FS L16 status "Issued for approval"; FS §1.3 L29–37 blank signature block | Demonstrable | Decides whether either pair is legal at all | **B** | **Documented only** (`docs/INDEX.md` C-2) | **BLOCKED** |
| **F-15** | Low | Auditor→L1 routing implemented in SQL but deliberately disabled in the UI | `Database/Stored Procedures/Approval/usp_ReturnApprovalRequestToL1.sql` exists; `ClaimsApproval_v2.aspx.cs` L1206–1208 returns "not available in this release" | Demonstrable | An agent could wire an unapproved workflow | **B** | **Documented only** (`docs/INDEX.md` C-10) | **BLOCKED** |
| **F-16** | — | **Scan hits that are NOT defects** (recorded so they are not "fixed" later) | `Database/Scripts/015*_RC018_Phase0_*.sql`: never tracked in any commit (correctly labelled local-only). `docs/reviews/RC019/evidence/` and `executions/`: templates state "It starts empty". `AnmolDristi/PolicyResolutionService.cs`: a spec row listing two candidate locations; the real file is `AnmolDristi/BLL/PolicyResolutionService.cs`. `AnmolDristi/Database/Operational/TA_DA_SQL_Reconciliation/`: in no commit and no stash. `packages/**/README.md`: vendored | Verified | Editing any of these would assert a fact the repository does not establish | **E** (not applicable) | **Deliberately left unchanged**; recorded in `docs/INDEX.md` §9 | **NO ACTION** |
| **F-17** | High | Repository, UAT, and LIVE procedure bodies differ (documented by the project itself) | `L2_Auditor_Risk_Register.md` L2-R25; `TA_DA_Deployment_Checklist.md` L837–851 (four distinct bodies for `usp_SubmitSalesDailyActivities`) | Quoted record, not independently verified | A deploy from repository files could regress deployed behaviour | **E** | **Documented only** (`docs/INDEX.md` C-4) | **BLOCKED** |
| **F-18** | Critical | Recovery gate closed: no valid pre-test restore point for state-changing UAT | Checklist L403 (newest qualifying UAT backup 2026-10-08 11:02:17, "BackupGate DOES NOT QUALIFY", `RESTORE VERIFYONLY` not run); required finish > 2026-10-12 14:02:57.583 | Demonstrable | No authorized UAT test can be reversed | **D/E** | **Documented only** (`docs/INDEX.md` §8 recovery gate) | **BLOCKED** |

---

## 3. The report's own summary, quoted (including its errors)

Quoted from the same report (message sequence 6): the four lines its own summary is made
of. Their text is **character-for-character**, but the four are *not* contiguous in the
source — the first is `seq 6` line 46 and the three list items are `seq 6` lines
571–573 — so they are grouped here for reading. The `> ` markers are this file's quoting;
the `- ` list markers are the source's own characters.

> **Outcome:** 10 conflicts registered. **3 resolved with repository-only edits**, **2 corrected as stale navigation**, **2 given additive conflict notices**, **3 documented only** (blocked on owners). No SQL, schema, config, code, tag, or environment change was made.
>
> - **Resolved (3 + 2 corrections):** F-01 (no instruction file), F-02 (no index), F-03/F-04/F-05/F-08/F-11 (five additive, both-sides-preserved notices), F-06/F-07 (stale preimage status corrected).
> - **Documented only (6 blocked):** F-09 (deleted RC-018 evidence), F-10 (`.gitattributes`), F-12 (duplicate payable writer), F-13 (procedure pairs), F-14 (`Completed` semantics), F-15 (Auditor→L1), F-17 (deployed-body divergence), F-18 (recovery gate).
> - **Deliberately no action (1):** F-16 — scan hits that are correct as written; "fixing" them would assert unverified equivalence.

**Note —** the summary above is retained as written. It is wrong in four places, and §4 records the difference rather than editing the quotation.

---

## 4. Reconciled counts, taken from the register's own `Status` column

**Note —** this table is the correction the summary needs. Its authority is the `Status`
cell of each row above, not the narrative summary and not any later interpretation.

| Status (verbatim from the register) | Count | IDs |
| --- | --- | --- |
| `RESOLVED` | **4** | F-01, F-02, F-06, F-07 |
| `NOTICE ADDED` | **4** | F-03, F-04, F-05, F-11 |
| `DOCUMENTED` | **1** | F-08 |
| `BLOCKED` | **8** | F-09, F-10, F-12, F-13, F-14, F-15, F-17, F-18 |
| `NO ACTION` | **1** | F-16 |
| **Total registered** | **18** | F-01 – F-18 |

The four defects in the quoted summary:

1. **Total.** "10 conflicts registered" against a register holding **18** rows.
2. **Resolved.** "3 + 2 corrections" implies five, while the same line names **four**
   resolved IDs (F-01, F-02, F-06, F-07).
3. **Notices.** "2 given additive conflict notices" against the register's **four**
   `NOTICE ADDED` rows; the summary line instead claims five, and wrongly includes
   **F-08**, whose register status is `DOCUMENTED`.
4. **Blocked.** "Documented only (6 blocked)" lists **eight** IDs, and the register marks
   all eight `BLOCKED` — "documented only" is not their register status.

By class, for reference: **A** = F-01, F-02, F-06, F-07 (and as notice F-03, F-04, F-05,
F-11); **B** = F-14, F-15 (with `B/D` on F-03, F-05, F-13); **C** = F-10, F-12;
**D** = F-09 (with `D/E` on F-18 and `E + D` on F-08); **E** = F-16, F-17.

---

## 5. IDs that are **not** register entries, and a numbering collision

**Excluded —** `F-19` and `F-20` do not appear in this register. They existed only in the
assistant's internal reasoning in the same message ("F-19 Recovery gate blocked (UAT
backup older than bodies) → class D/E → DOCUMENTED ONLY"; "F-20 Database/Scripts
numbering gap 015–019, 021–029 → consequence of F-09"), together with a drafted `F-17`
for `.freebuff/project-id` that was marked "Skip." In the final register the recovery gate
became **F-18**, the numbering gap was folded into **F-09**'s impact cell, and the
`.freebuff` item was dropped. They are recorded here as **reasoning-only** and must not be
cited as register findings.

**Numbering collision —** the Functional Specification carries a *separate and unrelated*
`F-01`–`F-11` series: its forbidden-transition table (`docs/FunctionalSpecification_MonthlySalesClaims_Phase1_v1.0.md`, L435–445), where `F-02` is "`Pending` → `Completed`"
and `F-11` is "Any transition on a claim whose header is `Completed`". Those IDs have
nothing to do with this register. **Always name the source when citing an `F-` ID.**

---

## 6. The decision register recovered from the same thread

**Note —** the same thread's earlier authority audit produced a decision register
(`D-1`–`D-9`). It is reproduced because it is the origin of the `D-3` label that appears
unresolved in `docs/INDEX.md` §9. Owners are as stated in the source; no decision is
made or implied here.

| ID | Decision (verbatim) | Why it matters (verbatim) | Owner and evidence required (verbatim) |
| --- | --- | --- | --- |
| **D-1** | Which single pair ships: (a) RC-014B PAA (`Completed`) + RC-014D MarkReady (`Completed`→`ReadyForPayment`), or (b) HF-231 PAA (stays `Pending`) + L2-R19 candidate MarkReady (`Pending`→`Completed`→`ReadyForPayment`)? | Every other item is downstream. Production currently runs **neither pair** — it has the *pre-HF-231* PAA with the *candidate* MarkReady. | Release owner / IT Application Owner. Evidence: one written selection plus a passing UAT run of the L2-R19 test matrix (tests 1–17, currently all NOT TESTED) on a backup that post-dates the bodies under test. |
| **D-2** | Does Rule H-2 still stand (the approval engine may never write `Completed`)? If it stands, RC-014B **and** the candidate are both deviations and require a specification change request under FS §1.4. | Determines whether `Completed` can keep meaning "approval finished" or must be reserved for paid. TD §6.6 warns the completed population cannot be treated as the paid population. | Business Process Owner (Sales) + Business Process Owner (Finance) + Audit. Evidence: signed FS §1.3, or an approved change request. Note: the FS approval table is currently entirely blank. |
| **D-3** | Is the 2026-10-14 07:58 Production promotion of `usp_MarkRequestReadyForPayment` (`E9097D3A…`, LEN 11181) accepted as a valid production state, given that `usp_ProcessApprovalAction` was **not** applied in that window and `Production_B_Apply.sql` still reads `@Execute = 0`? | Production is running a candidate body that paired with a different PAA than the one it was designed against. | CAB + IT Application Owner. Evidence: a Production smoke record, a Production financial proof (checklist records none — "No Production smoke or Production financial-proof file was found"), and a post-promotion full backup (none found after the apply). |
| **D-4** | Which `usp_RecalculateApprovalRequest` body is authoritative — `Approval/` or `Utilities/`? | Two tracked writers of the single payable column. A wrong pick silently changes money. | IT / DB owner. Evidence: hash reconciliation of both repository bodies against the deployed text, and deletion or explicit deprecation of the loser. |
| **D-5** | `usp_ReturnApprovalRequestToL1`: ship, park with a status marker, or delete? | A live-tracking object exists while the UI control is deliberately off; an agent could wire it by inference. | Business Process Owner (Sales) + Audit. Evidence: a decision note; until then the `docs/INDEX.md` row must mark it `unapproved-candidate`. |
| **D-6** | Which UAT/LIVE restore point is declared valid? | The newest undamaged UAT full backup (2026-10-08) predates the current procedure bodies; the checklist's own rollback gate is BLOCKED, so no authorized test can be safely reversed. | Ops / DBA. Evidence: a new undamaged full backup finishing after the bodies under test, plus `RESTORE VERIFYONLY … WITH CHECKSUM`. |
| **D-7** | Phase-2 Finance: name the intake (Excel export vs `FinalApprovedAmount`), define `Completed` and `Processed`, and confirm `usp_MarkRequestPaid` as the transition owner. | T-10 and `ClaimStatuses.Processed` are unimplemented; R-05/G-10 record the ERP intake as unknown. | Finance + IT. Evidence: a signed Phase-2 scope note. |
| **D-8** | Accept or refuse the requested waiver of the Approved Claims runtime test for the current deduction package? | The package's own UAT record is **HOLD**; a waiver is not CAB-approved and CAB sign-off on 2026-10-14 covers a different procedure. | CAB. Evidence: a package-specific approval naming the deduction package. |
| **D-9** | Are the `Approval/` and `Retrieval/` twins for `usp_GetApprovalRequest` to stay duplicated? | Low risk today (comments/whitespace only), but it is the same class of fork as D-4. | IT. Evidence: either a consolidation commit or a note accepting the duplication. |

### The D-3 decision package (message sequence 4)

Section heading, verbatim: `## 4. Decision D-3 — confirm the Production procedure pair`
"**Requested from:** CAB, with IT Application Owner. **No Production connection was made
by this package.**" Its verified repository fact, verbatim:

> `Database/Deployments/MarkReadyAuditorRow/Production_B_Apply.sql` declares `@Execute bit = 0` (L39) and returns before any change (L40–42), yet *the checklist records the three Production scripts being run in SSMS*. The repository apply script and the recorded execution disagree, so the repository cannot tell you what Production holds.

Its evidence list, condensed in order (six items): a fresh read-only catalog read of
`dbo.usp_ProcessApprovalAction` and `dbo.usp_MarkRequestReadyForPayment` on production
(`modify_date`, `LEN`, `DATALENGTH`, `HASHBYTES('SHA2_256', …)`, and whether the text
contains the `hf231` / `59110` / `59112` markers — "The `hf231` marker is the decisive
discriminator between the two PAA shapes"); a full-text comparison of the production Mark
Ready body against `HEAD`'s candidate and against the certified RC-014D body; a
post-promotion full backup record (`msdb.dbo.backupset`) plus `RESTORE VERIFYONLY … WITH
CHECKSUM`; an IIS publish/recycle record for the same window; a production smoke record
and a production financial-proof record (the checklist states none exists); and a written
accept-or-revert decision, with the recorded 2223-character pre-change production Mark
Ready preimage available if revert is chosen.

### The unresolved `docs/INDEX.md` label, and what is still needed

`docs/INDEX.md` §6 carries, on the `Database/Deployments/MarkReadyAuditorRow/` row, the
phrase `See §8, D-3.` That label is now explained by the source recovered above: it names
**decision `D-3` of this audit's decision register**, while `§8` of the index holds a
*different* register (`C-1`–`C-10`) — which is why it does not resolve inside that file.

**Note —** `D-3`'s subject matter corresponds to the index's register entry **C-5** ("The
candidate is labelled unauthorized yet recorded as promoted", owner "CAB / IT Application
Owner" — the same owner), and the fact the index row cites
(`Production_B_Apply.sql` `@Execute = 0`, L39–42) is verbatim `D-3`'s "verified repository
fact". **However, no source states that the label was intended to mean `C-5`.** The
mapping is therefore recorded here as **awaiting explicit owner confirmation** and is not
asserted. Until that confirmation exists, `docs/INDEX.md` §9 keeps the reference marked
unresolved.

---

## 7. What remains open after this recovery

1. **All eight `BLOCKED` entries** (F-09, F-10, F-12, F-13, F-14, F-15, F-17, F-18) —
   each needs the owner named in its `Class` cell; none is resolved by this file.
2. **F-08** (`DOCUMENTED`) — the unauthorized-promotion conflict still awaits CAB.
3. **The `D-1`–`D-9` decisions** — all nine remain open. `D-1` gates the others.
4. **The `D-3` → `C-5` mapping** — one owner confirmation, or an edit re-pointing the
   index row.
5. **Recovery-gate precondition** (`docs/INDEX.md` §8): no state-changing UAT is safe
   until a qualifying UAT backup exists and the gates are opened by a named owner.

---

## 8. Verification performed for this file

| Check | Method | Result |
| --- | --- | --- |
| Register rows present | counted ID cells matching the F-NN pattern in §2 | **18** |
| Columns per row | pipe count per row against the header | **9** |
| Rows match the source | each row byte-compared against the transcript's message `seq 6` | identical |
| Narrative quoted blocks | §3's four summary lines against `seq 6` L46 and L571–573; §6's `D-1`–`D-9` rows against `seq 2`; the two `D-3` quotations against `seq 4` | identical, modulo the display transformations in §1 |
| Status counts | tallied from the `Status` column | 4 + 4 + 1 + 8 + 1 = **18** |
| Counts cross-checked | §4's table re-derived from §2's `Status` values | every status matches; each `F-NN` appears once |
| Exclusions | `F-19`/`F-20` declared reasoning-only | recorded in §5 |
| No decision asserted | every `BLOCKED` row retains its owner | confirmed by review of §2 and §7 |

**No file in this repository other than this document and the cross-references added to
`docs/INDEX.md` was touched by this recovery. No SQL, application code, configuration,
schema, tag, baseline, or deployment artifact was modified, and no database, UAT, or
Production system was contacted.**
