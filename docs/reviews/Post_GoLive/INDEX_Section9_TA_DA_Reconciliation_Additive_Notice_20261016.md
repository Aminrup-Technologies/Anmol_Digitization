# Additive notice — INDEX §9 / `F-16`: availability of `TA_DA_SQL_Reconciliation/`

**Status:** ADDITIVE NOTICE — DRAFT FOR OWNER REVIEW. **Not applied, not approved,
not certified.** Neither tracked statement below was edited by this notice.
**Decision:** PENDING OWNER CONFIRMATION.
**As of:** 2026-10-16.
**Repository snapshot:** branch `feature/submitted-travel-baseline-deduction`,
`HEAD` `7a87804f329c924771858871c8ff9eb530aff009`, working tree 11 modified /
9 untracked / 0 staged / 0 deleted.

This file follows the rule in `docs/INDEX.md` §7 of `AGENTS.md`: when a recorded
statement is contradicted, the correction is **additive**. Both sources stay
intact and as written. Nothing here rewrites the index, the conflict register, or
any deployment checklist.

---

## 1. What prompted this notice

A read-only audit of this checkout found that the statement *"in no commit and no
stash"* about `AnmolDristi/Database/Operational/TA_DA_SQL_Reconciliation/` is not
supported by the object database: the directory is absent from every commit, but
30 of its paths — including the specific file the checklist cites — are present in
the untracked payload of one stash.

The related half of the same sentence ("in no commit") **is** corroborated.

---

## 2. The two recorded statements — quoted records, left as written

**Source A — `docs/INDEX.md` §9, row at L175.** Verbatim:

> | `AnmolDristi/Database/Operational/TA_DA_SQL_Reconciliation/` | `TA_DA_Deployment_Checklist.md` L289 | The path exists in no commit and no stash. Recorded evidence unavailable to this repository. |

**Source B — `docs/reviews/Post_GoLive/Recovered_Conflict_Register_20261015.md`
L92, row `F-16`.** The affected clause, verbatim:

> `AnmolDristi/Database/Operational/TA_DA_SQL_Reconciliation/`: in no commit and no stash.

**The statement both sources point at — `docs/releases/TA_DA_GoLive_2026/TA_DA_Deployment_Checklist.md` L289.**
Verbatim:

> UAT evidence: `AnmolDristi/Database/Operational/TA_DA_SQL_Reconciliation/TA_DA_UAT_CLOSURE_EVIDENCE.md`. SQL reconciliation package: `AnmolDristi/Database/Operational/TA_DA_SQL_Reconciliation/`.

**Scope of the correction — deliberately narrow.** Row `F-16` carries three other
items (`Database/Scripts/015*_RC018_Phase0_*.sql` as correctly-labelled
local-only, the intentionally empty `docs/reviews/RC019/evidence/` and
`executions/` destinations, and the `PolicyResolutionService.cs` spec row). None of
those clauses is contradicted by this audit and none is addressed here. Only the
one clause about this directory is in question.

---

## 3. Verified finding — this checkout, read-only probes

| Probe | Command (read-only) | Result |
| --- | --- | --- |
| Working tree | `test -e 'AnmolDristi/Database/Operational/TA_DA_SQL_Reconciliation'` | **ABSENT** |
| Any commit, any ref, any object | `git rev-list --all --objects \| grep -c 'TA_DA_SQL_Reconciliation'` | **0** |
| Any commit touching the path | `git log --all --oneline -- <path>` | **0 commits** |
| Every stash's tracked tree (`stash@{0}`…`stash@{13}`) | `git ls-tree -r --name-only stash@{N} \| grep -c …` | **0** in all 14 |
| Untracked payload of each stash (`stash@{N}^3`) | `git ls-tree -r --name-only stash@{N}^3 \| grep -c …` | **30** in `stash@{1}^3`; **0** in the other five payloads that exist; the remaining eight stashes have no third parent at all |
| The specific cited file | `git rev-parse 'stash@{1}^3:<path>/TA_DA_UAT_CLOSURE_EVIDENCE.md'` | present, blob `f991a3d653bb556650b7defd38606ea0473db9fd`, **17 818 B** |
| The directory as a tree object in that payload | `git rev-parse 'stash@{1}^3:<path>'` | tree `67f219a7f502aff39c149e714e9b4851ded31bdf` |
| Stash commit | `git rev-parse 'stash@{1}'` | `97d5e0d6121f4a095eede5f6db2c450926f455f8` |

**Payload scope — the correction's exact boundary.** `stash@{N}^3` exists only for
stashes created with `git stash -u`. In this repository **6** stashes carry an
untracked payload — `stash@{1}`, `stash@{3}`, `stash@{4}`, `stash@{6}`, `stash@{7}`,
`stash@{8}`, holding 92, 1, 9, 21, 9 and 15 entries respectively — and the other
**8** have two parents only, so they contain no untracked payload of any kind.
The audited path appears in **one** of the six payloads: `stash@{1}`, 30 of its 92
entries. "Zero in the other thirteen" would have been the wrong description: for
eight of them there is no payload to count.

**Bearing on each half of the sentence.**

- *"in no commit"* — **corroborated.** No commit in any ref carries the path, and
  the string appears in no object reachable from `--all`.
- *"no stash"* — **not supported.** The path exists as 30 entries in the untracked
  payload of `stash@{1}` (`On Aug26_AILWorks_AmimnrupSupport: pre-RC014B WIP stash
  2026-10-10`), and the exact file the checklist names is among them.

**The 30 paths** are the reconciliation package and its captured baselines:
8 top-level files (`Build-Package.ps1`, `Build-Rollback.ps1`,
`TA_DA_LATEST_VERSION_RECONCILIATION.md`, `TA_DA_SQL_DEPENDENCY_MAP.md`,
`TA_DA_SQL_RECONCILIATION.sql`, `TA_DA_SQL_RECONCILIATION_ROLLBACK.sql`,
`TA_DA_SQL_RECONCILIATION_VERIFY.sql`, `TA_DA_TV_SQL_PREDEPLOY_SNAPSHOT.md`),
`TA_DA_UAT_CLOSURE_EVIDENCE.md`, `captured/tv/` (9 procedures), `captured/uat/`
(15 procedures), and `intended/usp_ProcessApprovalAction.sql`.

---

## 4. Reachability and durability — a fact the two recorded statements do not cover

`refs/stash` is the only stash ref in this repository
(`git for-each-ref | grep -c stash` → **1**), and `stash@{1}` is **not** an
ancestor of `refs/stash` and **not** an ancestor of `HEAD`.

Consequently the 30 objects are reachable **only through the `refs/stash` reflog
entry** — not through any branch, tag, or ref. They are recoverable in this
checkout today, and they are correspondingly exposed: dropping or expiring
`stash@{1}`, or any reflog maintenance that discards it, would leave those objects
unreachable and prunable.

This is recorded as a **durability consideration**, not an instruction. Nothing
here authorizes dropping, extracting, or promoting a stash.

---

## 5. What this notice does not claim

- It does **not** assert that the stash payload is accurate, complete, current, or
  the correct version of anything. A copy in a stash is a copy, not a certification.
- It does **not** assert that the stash content matches what was ever run in UAT or
  Production. The repository is not evidence of deployed state.
- It does **not** make the payload committed evidence. It sits in the object
  database only, outside every commit.
- It does **not** approve, authorize, or recommend recovery, restoration,
  execution, or use of these artifacts as release evidence.
- It does **not** close any decision. It records an availability distinction.

---

## 6. Proposed additive wording — NOT APPLIED

For an owner to accept or rewrite. Each option preserves the existing text and
appends a notice; nothing is deleted or silently reconciled.

**Option 1 — `docs/INDEX.md` §9, that row only.** Keep the existing Finding cell as
written and append:

> **Additive notice (2026-10-16, draft for owner review).** The clause "and no
> stash" is not supported: the directory is absent from every commit, but 30 of its
> paths — including the `TA_DA_UAT_CLOSURE_EVIDENCE.md` this row cites — sit in the
> untracked payload of `stash@{1}` (`97d5e0d6…`, blob `f991a3d6…`). Reachable via
> the `refs/stash` reflog only. Availability distinction recorded; **decision
> pending owner confirmation.** Originals left as written.

**Option 2 — `Recovered_Conflict_Register_20261015.md` `F-16`.** Keep the row and
its status as written and append the same notice, scoped to the one clause:

> **Additive notice (2026-10-16, draft for owner review).** The clause "in no commit
> and no stash" is **half-corroborated**. "In no commit" verifies. "And no stash"
> does not: 30 paths are present in `stash@{1}^3`. The row's other three items are
> unaffected. **Decision pending owner confirmation.**

**Option 3 — no edit anywhere.** Leave both tracked statements as written and keep
this finding in the review record alone. This is a legitimate outcome and **the
default if no owner acts**, consistent with §9's own principle that a "correction"
must not assert a fact the repository does not establish.

---

## 7. Owner action required

1. Confirm the stash evidence (or have it re-probed independently).
2. Choose Option 1, Option 2, Option 3, or a rewording.
3. If any option is adopted, decide whether the `refs/stash`-reflog durability point
   belongs in the same notice or in a separate operational note.

Until an owner answers, the two tracked statements stand unamended and the
decision is **PENDING**.

---

## 8. Limitations

- No `git fetch` was run; remote-tracking refs may be stale, and remote-only
  branches or tags were not probed.
- The probe was a path-string match (`TA_DA_SQL_Reconciliation`). The referenced
  content was not verified against UAT, Production, or any certification.
- The 14 named stashes were enumerated; older reflog entries no longer listed were
  not probed, so "in no stash" is falsified — the stronger claim "present in exactly
  one stash" is bounded by the visible reflog.
- Only untracked payloads (`stash@{N}^3`) and stash trees (`stash@{N}`) were
  inspected; no stash index or working-tree parent was enumerated beyond those. An
  earlier draft of this table reported "0 in the other 13" payloads; that figure was
  an artifact of suppressing the error from stashes with no third parent. It is
  corrected above to the measured 1-of-6 / 8-with-none split.
- Body-level claims (that the payload *is* the closure evidence, or that the
  captured procedure bodies match a certified baseline) were not tested and are not
  asserted.
- This notice establishes repository state only. It says nothing about what is
  deployed.
- This file is unapproved, uncertified, and non-authoritative over its sources.

---

## 9. Provenance

Every probe behind §3 and §4 was read-only. The commands are listed inline above;
no object, ref, stash, index entry, working-tree file, or tracked document was
created, modified, moved, or deleted by this notice. The tracked statements quoted
in §2 were read from `HEAD`, not from the working tree.
