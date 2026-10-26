# TA/DA Release Readiness and Decision Register

**As of:** 2026-10-16
**Status:** consolidation only. This register is **not** a release approval, a certification, a source of record, or an authority over any document it cites. It adds no baseline and selects none.

---

## 1. Purpose and authority

This is a navigation and decision register. It restates, in one place, what is already recorded in the detailed evidence, which questions remain open, who owns each, and which gates must be satisfied before any change is proposed. It replaces no source report and deletes nothing; every claim below points at the document or command output that establishes it.

**Audit context.** Branch and HEAD were re-captured immediately before this file was created:

| Field | Value |
| --- | --- |
| Repository root | `D:/PersonalRnD/AminrupWorks/Anmol_Digitization` |
| Branch | `feature/submitted-travel-baseline-deduction` |
| HEAD | `7a87804f329c924771858871c8ff9eb530aff009` |
| Working tree | 11 modified · 8 untracked · 0 staged · 0 deleted |
| Tags / stashes | 9 / 14 |

This is the state of **this local checkout** at that moment. It is not a claim about the remote, and two other worktrees exist that this register does not describe (`_tada_artifact_70ff21a` at `70ff21a`; `Anmol_RC019_Default` at `52c1f32`, the ref that `RC019_Foundation_Gate.md`'s own availability notice labels `HEAD`).

**Label convention.** `VERIFIED` = established from committed evidence or a read-only check, re-checkable at the named ref · `PARTIALLY VERIFIED` = part established, and the unestablished part is named · `BLOCKED` = an owner action or an absent prerequisite prevents the step · `OWNER DECISION REQUIRED` = the repository records the question and its owner, and asserts no answer · `NOT REASSESSED` = carried forward from a source without new verification here. `PASS` is not used for Production or business behaviour anywhere in this register, because no independent execution evidence for those was seen in this consolidation.

---

## 2. Executive status

| Workstream | Status | Evidence | Required next action |
| --- | --- | --- | --- |
| RC-018 artifact inventory | `VERIFIED` | Deletion commit `9fdeb78`; `docs/reviews/Post_GoLive/RC018/` 36 → 35 (`main`) → 1 (`HEAD`) files; `Database/Scripts` `.sql` 56 → 42 → 23; gate availability notice L129–133; RC-018 inventory report (conversation-supplied) | Owner decision on whether the pack is restored; no restoration is authorized by this register |
| RC-019 dependency and citation availability | `PARTIALLY VERIFIED` | 435 path references audited; 412 resolve at `HEAD`; 23 do not (2 tag-recoverable, 9 stash-only rows, 2 working-tree-only, 10 shorthand); RC-019 citation-availability audit (conversation-supplied) | Decide the source of record for the blocked checks (c-2, c-3, c-8, t-1, t-2) — certified tag or reloaded pack |
| Auditor / Finance terminal-state semantics | `OWNER DECISION REQUIRED` | FS **Rule H-2** L375; FS **G-05** L1301; TD L1136 “**Forbidden** — [G-05]”; FS forbidden-transition table L435–445; `docs/INDEX.md` C-2; recovered register F-14, D-2 | Signed FS §1.3, or an approved specification change request |
| Production procedure provenance | `OWNER DECISION REQUIRED` | Checklist L800–812 (2026-10-14 07:58 promotion table), L837–851 (catalog read), L44 / L204 / L779 (change-window and L2-R19 approvals); `Production_B_Apply.sql` L39–42 `@Execute = 0`; recovered register D-3; `docs/INDEX.md` C-4, C-5 | CAB + IT Application Owner accept-or-revert decision, with the Production smoke and financial-proof records the checklist states are absent |
| Canonical financial recalculation procedure | `OWNER DECISION REQUIRED` | `docs/INDEX.md` C-3; recovered register F-12 and D-4; `L2_Auditor_Risk_Register.md` L2-R19; checklist L837–851 (`usp_RecalculateApprovalRequest` LIVE LEN 6945 / `B8095136…`) | Written selection of `Approval/` or `Utilities/`, plus deprecation of the loser |
| Recovery gate and state-changing UAT | `BLOCKED` | Checklist L403 (newest qualifying UAT backup 2026-10-08 11:02:17; `BackupGate` **DOES NOT QUALIFY**; `RESTORE VERIFYONLY` not run); `docs/INDEX.md` §8 recovery gate; recovered register F-18, D-6 | Produce a new undamaged full backup finishing after `2026-10-12 14:02:57.583` and pass `RESTORE VERIFYONLY … WITH CHECKSUM`, with gates opened by a named owner |
| RC-018 / RC-019 source-document integrity | `PARTIALLY VERIFIED` | `RC019_Foundation_Gate.md` availability notice L121–139; 42/42 certified manifest rows reproduce from tag blobs under the tag's `eol=crlf` policy; `RC019_Scope_Gate.md` has no committed source (stash-only); `docs/INDEX.md` §9 L175 contradicts the stash object database | Owner confirmation of the RC-019 scope authority and of the `INDEX.md` §9 statement |
| Existing unapproved release or implementation changes | `NOT REASSESSED` | `docs/INDEX.md` §6 (L2R19 gates shipped closed; untracked `032`/`032b`; `AnmolDristi/sql/changes` with `@CommitChanges = 0`); `Database/Deployments/L2R19_UAT/README.md`; `docs/reviews/Post_GoLive/SubmittedTravelBaseline_UAT_Execution.md` (untracked, verdict **HOLD**); the 11 modified / 8 untracked working-tree paths recorded in §1 | Carried forward unchanged; nothing here approves, rejects, or executes any of them |

---

## 3. Decision register

IDs are reproduced from their sources. `D-1`–`D-9` come from the recovered decision register; `F-nn` from the recovered conflict register (whose four defects in its own summary are reconciled in that file's §4 — its `Status` column is the authority, not its narrative). `C-nn` are conflict IDs in `docs/INDEX.md` §8. Rows marked **finding only** are consolidations of the two audits supplied in this conversation and carry **no register ID**; none has been invented, and `D-3` has **not** been remapped to `C-5`.

This register does **not** reproduce the whole recovered register. Its four `RESOLVED` rows (`F-01`, `F-02`, `F-06`, `F-07`), four `NOTICE ADDED` rows (`F-03`, `F-04`, `F-05`, `F-11`) and one `NO ACTION` row (`F-16`) are omitted here and remain as recorded in [`Recovered_Conflict_Register_20261015.md`](Recovered_Conflict_Register_20261015.md) §2 and §4. The nine `BLOCKED` / `DOCUMENTED` rows are carried below because they gate work. The Functional Specification carries a separate, unrelated `F-01`–`F-11` transition series (FS L435–445) — always name the source when citing an `F-` ID.

| ID | Question to resolve | Evidence | Required owner | Status | Dependency / gate | Next action |
| --- | --- | --- | --- | --- | --- | --- |
| **D-1** | Which single procedure pair ships — RC-014B PAA + RC-014D MarkReady, or HF-231 PAA + the L2-R19 candidate MarkReady? | Recovered register D-1; `docs/INDEX.md` C-1 and its C-1 detail table; F-13 | Release owner / IT Application Owner | `OWNER DECISION REQUIRED` | Gates every other row | One written selection plus a passing UAT run of the L2-R19 matrix on a backup that post-dates the bodies under test |
| **D-2** | Does **Rule H-2** still stand — may the approval engine ever write `Completed`? | FS L375 (rule text), L1301 (G-05), L435–445; TD L1136; FS L16 “Issued for approval” and blank §1.3 signature block; F-14; `docs/INDEX.md` C-2 | Business Process Owner (Sales) + BPO (Finance) + Audit | `OWNER DECISION REQUIRED` | Follows D-1 | Signed FS §1.3, or an approved change request |
| **D-3** | Is the 2026-10-14 07:58 Production promotion of `usp_MarkRequestReadyForPayment` (`E9097D3A…`, LEN 11181) an acceptable Production state, given `usp_ProcessApprovalAction` was not applied in that window and `Production_B_Apply.sql` still reads `@Execute = 0`? | Checklist L800–812, L837–851, L44, L204, L779; `Production_B_Apply.sql` L39–42; candidate banner “Not authorized for UAT or LIVE execution” (F-08); recovered register §6 (D-3 package) | CAB + IT Application Owner | `OWNER DECISION REQUIRED` | Follows D-1; the repository cannot establish Production state | Production smoke and financial-proof records (checklist states none found) plus a post-promotion full backup; then accept or revert |
| **D-4** | Which `usp_RecalculateApprovalRequest` body is authoritative — `Approval/` or `Utilities/`? | `docs/INDEX.md` C-3; F-12 (L162/L181 vs L193/L229); `L2_Auditor_Risk_Register.md` L2-R19 (repository `Approval/` body stated equal to UAT and LIVE at `HEAD`); checklist L837–851 | IT / DB owner | `OWNER DECISION REQUIRED` | Feeds any recalculation change; the C# call sites were `NOT REASSESSED` here | Hash reconciliation of both bodies against deployed text, then deletion or explicit deprecation of the loser |
| **D-5** | `usp_ReturnApprovalRequestToL1`: ship, park with a status marker, or delete? | F-15 (`ClaimsApproval_v2.aspx.cs` L1206–1208 refuses the action); `docs/INDEX.md` C-10 | BPO (Sales) + Audit | `OWNER DECISION REQUIRED` | Independent of D-1 | A decision note; until then the index row stays `unapproved-candidate` |
| **D-6** | Which UAT / LIVE restore point is declared valid? | Checklist L403; recovered register D-6; F-18; `docs/INDEX.md` §8 recovery gate | Ops / DBA | `BLOCKED` | Gates all state-changing UAT | New undamaged full backup after the bodies under test, plus `RESTORE VERIFYONLY … WITH CHECKSUM` |
| **D-7** | Phase-2 Finance: name the intake, define `Completed` / `Processed`, confirm `usp_MarkRequestPaid` as transition owner | Recovered register D-7 | Finance + IT | `OWNER DECISION REQUIRED` | Follows D-2 | Signed Phase-2 scope note |
| **D-8** | Accept or refuse the waiver of the Approved Claims runtime test for the deduction package? | Recovered register D-8; `SubmittedTravelBaseline_UAT_Execution.md` (verdict **HOLD**, L8 / L227 / L229, states `032b` is not its rollback) | CAB | `OWNER DECISION REQUIRED` | Gated by the recovery gate (D-6 / F-18) | A package-specific CAB approval naming the deduction package |
| **D-9** | Are the `Approval/` and `Retrieval/` twins for `usp_GetApprovalRequest` to stay duplicated? | Recovered register D-9 | IT | `OWNER DECISION REQUIRED` (low risk) | Same class as D-4 | A consolidation commit, or a note accepting the duplication |
| **F-08** | The candidate is labelled unauthorized yet recorded as promoted | Recovered register F-08; `docs/INDEX.md` C-5 | CAB + IT Application Owner | `OWNER DECISION REQUIRED` | Same subject as D-3 | Same as D-3. **Note —** the index label `See §8, D-3.` names *this* register's `D-3`, while the index §8 holds `C-1`–`C-10`; the `D-3` → `C-5` mapping is recorded in the recovery as **awaiting explicit owner confirmation and is not asserted** |
| **F-09** | 78 RC-018 release artifacts were deleted after the tag that certifies them; **75 are absent at `HEAD`, and three were re-added later with different content** | Gate notice L133 (78 deletions); F-09; `git diff --name-status` tag → `HEAD`; RC-018 inventory report (which reconciles 78 as 75 current + 3 superseded restorations). Blob pairs for the three re-additions: `020` `ac47b07c95` → `e6b82b7e23`; `020b` `cab1f9a20b` → `c11e9bfd33`; closeout `95dab66a32` → `f3746260c6` (insertion-only, `80 0`) | Release Owner / CAB | `BLOCKED` | Gates findings below and RC-019 constraints c-2, c-3, c-8 | Restoration is a Release/CAB decision; not authorized here |
| **F-10** | `.gitattributes` was replaced by the same commit, dropping the `eol=crlf` / `eol=lf` entries | F-10; tag holds 3 `eol=` entries, `HEAD` holds 0 (the same values were re-derived in the citation audit) | IT / DB owner | `BLOCKED` | Affects any hash recompute | Controlled renormalization decision; changing it now could mass-renormalize a tree holding uncommitted work |
| **F-12** | Two tracked definitions both write `FinalApprovedAmount` | F-12; `docs/INDEX.md` C-3 | IT / DB owner | `BLOCKED` | Same subject as D-4 | Documented only; no canonical body selected |
| **F-13** | `usp_ProcessApprovalAction` and `usp_MarkRequestReadyForPayment` form incompatible pairs across refs | F-13; `docs/INDEX.md` C-1; `AGENTS.md` §5 | Release owner / IT Application Owner (B/D) | `BLOCKED` | Same root as D-1 | Documented only; any edit to either file is an unauthorized pair selection |
| **F-14** | FS Rule H-2 forbids the approval engine writing `Completed`; both candidate pairs write it | F-14; `docs/INDEX.md` C-2 | BPO (Sales) + BPO (Finance) + Audit | `BLOCKED` | Same subject as D-2 | Documented only |
| **F-15** | Auditor → L1 routing implemented in SQL but deliberately disabled in the UI | F-15; `docs/INDEX.md` C-10 | BPO (Sales) + Audit | `BLOCKED` | Same subject as D-5 | Documented only |
| **F-17** | Repository, UAT and LIVE procedure bodies differ | F-17; `L2_Auditor_Risk_Register.md` L2-R25; checklist L837–851; `docs/INDEX.md` C-4 | IT / DB owner, with live-environment evidence | `BLOCKED` | Constrains any deploy from repository files | Documented only |
| **F-18** | Recovery gate closed: no valid pre-test restore point for state-changing UAT | F-18; checklist L403; `docs/INDEX.md` §8 | Release Owner / CAB + Ops (D/E) | `BLOCKED` | Gates every state-changing test and the deduction package | Satisfy the recovery gate first |
| **finding only** (no register ID) | RC-018 hash and execution manifests are absent at `HEAD`; the certified tag is the only recovery reference | Tag blobs `541665c741` (`RC018_SHA256_Manifest.md`, 6813 B), `257c58aecd` (`RC018_Execution_Manifest_v1.md`, 16480 B), `de56c848a2` (`RC018_Master_Runbook.md`), `c1734c3be0` (`RC018_Phase0_Discovery_Report.md`); all absent at `HEAD`; all 42 manifest rows reproduce from tag blobs under `eol=crlf`; tag peels to `9ab2e588590223a46c6ba362c669db50b446230c` | Release Owner / CAB | `BLOCKED` at `HEAD` | Gates c-2, c-3, c-8, t-2 | Decide whether verification runs against the tag as source of record |
| **finding only** (no register ID) | The surviving `020` / `020b` at `HEAD` do not match their certified manifest hashes, and `020c` is absent | Manifest `020` `A0E516AF…`, `020b` `2DAF1510…`; `HEAD` `020` LF `266DCD62…` / CRLF `529758A3…`; `HEAD` `020b` LF `EE1DB419…` / CRLF `8C353C73…` — both mismatch under either policy; `020c` present at the tag (`e9bcf12e07`) and absent at `HEAD`; closeout diff tag → `HEAD` is `80 0` (insertion-only) and adds an entry “2026-09-29 — 020 TRN_RM_CLASS_1 production reconciliation … It does not replace the 2026-09-26 row above” | Release owner / IT (with D-1) | `OWNER DECISION REQUIRED` | Depends on D-1 | Select the governing pair/version. **Note —** the constraint is **not** asserted to have failed; the closeout documents a later `020` run and canonicality is unresolved |
| **finding only** (no register ID) | `RC019_Scope_Gate.md` is cited by `Database/Scripts/031_RC019_Execution_Template.sql` L5 but has no committed source | Absent at `HEAD`, absent in every local ref (per-ref check, 0 hits); present only as an untracked blob in `stash@{4}` (`ed276853…`, 6324 B); its text cites `66b42cd` and `1b37b15` (both exist) and `RC019_Phase0_Discovery_Report.md` (also stash-only); the RC-019 branches carry only the five committed `docs/reviews/RC019/` files | `OWNER TO BE CONFIRMED` | `OWNER DECISION REQUIRED` | RC-019 template authority | Confirm whether it is a source of record, and from where it should be read. Stash material is **not** equivalent to committed or approved evidence |
| **finding only** (no register ID) | `docs/INDEX.md` §9 states the TA/DA SQL reconciliation evidence “exists in no commit and no stash” | `docs/INDEX.md` §9 L175; F-16 repeats it; contradiction (derived): 30 paths under `AnmolDristi/Database/Operational/TA_DA_SQL_Reconciliation/`, including the file cited by `TA_DA_Deployment_Checklist.md` L289, are present in `stash@{1}`'s untracked payload, and 0 in that stash's tracked diff; the path remains absent from `HEAD`, every ref and the worktree | `OWNER TO BE CONFIRMED` | `OWNER DECISION REQUIRED` | Documentation integrity | An **additive** correction or an explicit decision to leave it as recorded. `docs/INDEX.md` was not edited by this register |
| **finding only** (no register ID) | The `015*` RC-018 Phase-0 scripts are absent from the worktree, and their governing constraints | Gate constraints 6–7 (L87–93) and local-only inventory (L111–117, L137); absent from `HEAD`, the certified tag and every ref; present only in `stash@{4}`'s untracked payload | Release Owner / IT (constraint owner) | `PARTIALLY VERIFIED` — the “never tracked” record is accurate; the content is not in the worktree | Constraint c-6 / c-7 | Decide on the Phase-0 content separately from RC-019 source; c-7's numbering rule (later discovery from `030`) is unaffected |
| **finding only** (no register ID) | The prerequisites for any state-changing UAT, all of which must hold before a test is run: a **backup** that post-dates the bodies under test, **checksum** and **restore-verification** evidence for it, the **preimage** captures for every object to be changed, the **target database** named and read back as such, and **explicit authorization** for that specific change | Checklist L403 (backup gate **BLOCKED**; a new full backup must finish after `2026-10-12 14:02:57.583`; `RESTORE VERIFYONLY` not run), L400–406 (preimage and preflight rows; the preflight evidence file is named there); `Database/Deployments/L2R19_UAT/README.md` (gates shipped closed; the four preimages are embedded and hash-verified per F-06, so they must not be re-exported or substituted); D-6; F-18; `docs/INDEX.md` §8 recovery gate | Ops / DBA + Release Owner / CAB, with the change-window and package approvals the checklist records | `BLOCKED` | Gates all of §4 | See §4 — all five items there are prerequisites, and none is a proof that has been met |

---

## 4. Execution gates (dependency order)

These are **gates, not proof that they have been met**. None is satisfied as at the date of this register.

1. **Confirm source-of-record and business-owner decisions.** D-1 first (it gates the rest), then D-2 for the meaning of `Completed`. Until these are answered in writing, no correction can be shown to be the intended one.
2. **Complete authorized, read-only Production provenance verification where required.** D-3's evidence list: fresh read-only catalog reads of the two procedures (`modify_date`, `LEN`, `DATALENGTH`, `HASHBYTES('SHA2_256', …)`, and whether the text carries the `hf231` / `59110` / `59112` markers), full-text comparison against `HEAD`'s candidate and the certified RC-014D body, plus the `msdb.dbo.backupset` record. Read-only only, and only under explicit authorization.
3. **Satisfy the complete recovery gate before state-changing UAT.** A UAT full backup finishing after `2026-10-12 14:02:57.583`, undamaged, passing `RESTORE VERIFYONLY … WITH CHECKSUM`, with the apply and rollback gates opened by a named owner; both are currently closed (`D_Rollback_L2R19_Finalization_UAT.sql` ships closed with the preimage verification intact).
4. **Approve a specific change proposal and its rollback plan if a correction is justified.** Name the body, the owner selection it implements, the exact file, and the proven rollback. No bulk restoration, and no stash cleanup.
5. **Execute controlled validation and capture evidence only after all required approvals.** One script, one window, one recorded RunTag, with the RC-019 evidence/matrix/register forms as the RC-019 templates prescribe; and never before the backup prerequisite in step 3.

---

## 5. Scope and non-negotiable safeguards

- No unapproved business-logic changes to application code, SQL, or configuration.
- No bulk restoration of deleted artifacts and no stash cleanup, extraction, or mutation.
- No alteration of the certified RC-018 tag or its baseline commit `9ab2e588`.
- No SQL execution and no state-changing UAT while the recovery gate is closed (F-18 / D-6).
- No Production deployment without the applicable approvals (D-3 / D-8 and the CAB records the checklist names).
- No deletion, rewriting, or "correction" of existing audit evidence; conflicts are recorded additively with both sides cited.
- No claim of Production verification based only on repository contents or file hashes. Repository evidence establishes repository state, not deployed state (C-4 / F-17 / L2-R25).

---

## 6. Evidence index

Paths are shown repository-relative; the links resolve from this file's directory.

| Source | Kind | What it establishes |
| --- | --- | --- |
| [`docs/INDEX.md`](../../INDEX.md) | committed | Governance index: §5 RC-019 gate, §6 packages and working-tree artifacts, §8 `C-1`–`C-10`, §9 stale-or-unavailable references, §10 baselines |
| [`Recovered_Conflict_Register_20261015.md`](Recovered_Conflict_Register_20261015.md) | committed | `F-01`–`F-18` verbatim with `Status` authority, and the `D-1`–`D-9` decision register and `D-3` package (§6) |
| [`Post_GoLive_Baseline.md`](Post_GoLive_Baseline.md) | committed | The 2026-09-15 frozen reference point, plus its own additive conflict notice at L251–263 |
| [`../RC019/RC019_Foundation_Gate.md`](../RC019/RC019_Foundation_Gate.md) | committed | The RC-019 constraints and the availability notice at L121–139 (whose own `HEAD` label is `52c1f32`) |
| [`docs/releases/TA_DA_GoLive_2026/TA_DA_Deployment_Checklist.md`](../../releases/TA_DA_GoLive_2026/TA_DA_Deployment_Checklist.md) | committed | Backup gate L403; promotion records and catalog reads L800–851; change-window and owner approvals L44 / L204 / L779 |
| [`docs/reviews/L2_Auditor_AsIs/L2_Auditor_Risk_Register.md`](../L2_Auditor_AsIs/L2_Auditor_Risk_Register.md) | committed | `L2-R19`, `L2-R24`, `L2-R25` |
| [`docs/reviews/L2_Auditor_AsIs/L2_Auditor_Finalization_Contract.md`](../L2_Auditor_AsIs/L2_Auditor_Finalization_Contract.md) | committed | Finalization status later recorded as owner-approval pending (F-05) |
| [`docs/FunctionalSpecification_MonthlySalesClaims_Phase1_v1.0.md`](../../FunctionalSpecification_MonthlySalesClaims_Phase1_v1.0.md) | committed | Rule H-2 L375; G-05 L1301; forbidden transitions L435–445 |
| [`docs/TechnicalDesignSpecification_MonthlySalesClaims_Phase1_v1.0.md`](../../TechnicalDesignSpecification_MonthlySalesClaims_Phase1_v1.0.md) | committed | The “Forbidden — [G-05]” row at L1136 |
| [`Database/Deployments/MarkReadyAuditorRow/Production_B_Apply.sql`](../../../Database/Deployments/MarkReadyAuditorRow/Production_B_Apply.sql) | committed | `@Execute = 0` and the early return at L39–42 |
| [`Database/Deployments/L2R19_UAT/README.md`](../../../Database/Deployments/L2R19_UAT/README.md) | committed; **modified in the working tree** | Package gates and the preimage inventory |
| [`docs/reviews/Post_GoLive/RC018/RC018_Execution_Closeout_20260927.md`](RC018/RC018_Execution_Closeout_20260927.md) | committed at `HEAD` | The only surviving RC-018 pack file; the certified range is byte-identical (insertion-only `80 0` diff) and holds the three CAB decisions (L18, L107–109) |
| `docs/reviews/Post_GoLive/RC018/RC018_SHA256_Manifest.md`, `RC018_Execution_Manifest_v1.md`, `RC018_Master_Runbook.md`, `RC018_Phase0_Discovery_Report.md` | **recoverable only from the certified tag** (blobs `541665c741`, `257c58aecd`, `de56c848a2`, `c1734c3be0`) | The certified hashes and the RC-018 execution record; absent at `HEAD` |
| [`SubmittedTravelBaseline_UAT_Execution.md`](SubmittedTravelBaseline_UAT_Execution.md) | **untracked**, present on disk | The kilometre-deduction package record, verdict **HOLD** |
| `Database/Scripts/032_Add_SubmittedClaimBaseline.sql`, `032b_Rollback_SubmittedClaimBaseline.sql`; `AnmolDristi/sql/changes/**`; `AnmolDristi/sql/diagnostics/**` | **untracked**, present on disk | Unapproved local packages; `@CommitChanges = 0` / `@DryRun = 1` defaults |
| RC-018 artifact inventory execution report | **conversation-supplied execution report; not confirmed as a repository artifact** | The 75-absent / 3-superseded inventory and blob recoverability |
| RC-019 dependency and citation-availability audit | **conversation-supplied execution report; not confirmed as a repository artifact** | The 435-reference audit: 412 resolved, 23 not, 0 ambiguous, plus the assertion suite |
| `…\freebuff\Cache\Cache_Data\f_000113` | **client cache**, not a repository artifact | The origin transcript of the recovered `F-` and `D-` registers, as recorded in that register's §1 |

---

## 7. Limitations

- Local remote-tracking refs may be **stale**: no `fetch` or `pull` was performed, so "in every ref" means every ref present locally.
- These audits establish **repository** state only. They do not establish deployed UAT or Production procedure definitions; C-4 / F-17 / L2-R25 record those differing.
- **Recoverability is not approval.** That a blob exists in a tag or a stash does not authorize restoring it, nor make it a source of record.
- **Stash-only material is not equivalent to committed or approved evidence**, and can be evicted; the recovered registers say the same of their own client-cache source.
- No new independent verification is implied by this consolidation: it re-cites named sources and names the checks that were not run.
- The citation audit's candidate extraction used an extension allowlist (`md sql txt csv json cs aspx config ps1 xml sln yml yaml png bin bak zip`); references to other file types are not enumerated by it. Its CRLF byte model is inferred from `.gitattributes` and `core.autocrlf`, corroborated by 42/42 matching and 0/42 raw, not observed from a checkout.

---

**No existing file, business logic, SQL, configuration, tag, or release baseline was changed in producing this register.** It is not committed, not approved, and not certified.
