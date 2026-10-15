# Documentation index — Anmol Digitization

**Purpose.** A map of the governance documents in `docs/` (and the release
artifacts that live under `Database/` and `AnmolDristi/`), with a status column
and explicit pointers to the places where documents disagree.

**What this file is not.** It invents no authority, grants no approval, and
resolves no conflict. Where two sources disagree, both are named and the decision
is marked **pending**. Nothing here should be read as a decision by the release
owner, a Business Process Owner, Finance, Audit, IT/DB, CAB, or Ops/DBA.

Companion file: [`AGENTS.md`](../AGENTS.md) — how to work in this repository.

**Status vocabulary**

| Status | Meaning |
| --- | --- |
| `authoritative-spec (unsigned)` | The document that defines the agreed workflow, but its own approval block is not signed |
| `certified-evidence` | Evidence recorded against a release tag |
| `execution-record` | A log of what an operator ran; a quoted record, not an independent verification |
| `as-is-capture` | A point-in-time capture of deployed behaviour at a stated date |
| `superseded` | Kept for history; no longer describes the current intent |
| `unapproved-candidate` | Present in the repository but not authorized |
| `open-conflict` | Contradicted elsewhere; decision pending — see §8 |
| `blocked` | Cannot proceed; a gate is closed |
| `not-in-git` | Referenced by a document but never committed, or absent from `HEAD` |

---

## 1. Authoritative specifications

| Path | Status | Note |
| --- | --- | --- |
| `docs/FunctionalSpecification_MonthlySalesClaims_Phase1_v1.0.md` | `authoritative-spec (unsigned)` · `open-conflict` | FS-MSC-P1, "Issued for approval". §1.3 approval table (L29–37) has five rows with blank name/signature/date. Rules H-1/H-2/H-3 at L373/375/377; transitions T-09/T-10/T-11 at L413–415; forbidden set F-02…F-11 at L436–445; G-05 (Critical) at L1301. See §8. |
| `docs/TechnicalDesignSpecification_MonthlySalesClaims_Phase1_v1.0.md` | `authoritative-spec (unsigned)` · `open-conflict` | TDS-MSC-P1, "Issued for technical review". `usp_MarkRequestReadyForPayment` contract §4.7 (L766–790); transition matrix §6.5 (L1134–1136, `Pending → Completed` marked **Forbidden — [G-05]**); §6.6 (L1192–1204) on `ReadyForPayment`/`Completed`. |
| `docs/Production_Hardening_Review_MonthlySalesClaims.md` | reference | Hardening review referenced by the FS as R1. |
| `docs/PolicyException_Integrity_Review.md`, `docs/PolicyException_Operational_Experience.md` | reference | Policy-exception module reviews. |
| `docs/05-data-architecture/phase-1-schema.md`, `docs/05-data-architecture/compliance-and-security.md` | reference | Data architecture and controls. |
| `docs/06-implementation/technical-guidelines.md` | reference ("Accepted") | **Scoped to ITSM (`~/ITSM/`)**, not to Sales TA/DA. Not a TA/DA rule source. |
| `docs/04-adrs/0001-itsm-architecture-strategy.md`, `0002-identity-bridge-sso.md`, `0003-itsm-data-store-strategy.md` | reference | ADRs. |

## 2. Release and certification records (Sales TA/DA)

| Path | Status | Note |
| --- | --- | --- |
| `docs/releases/TA_DA_GoLive_2026/TA_DA_GoLive_Certification.md` | `certified-evidence` | RC-001–RC-013 against tag `tada-v1.0-go-live-2026` (`94bc2089`). |
| `docs/releases/TA_DA_GoLive_2026/TA_DA_Smoke_Test_Execution.md` | `certified-evidence` | Smoke and Forward-UX run, 2026-09-15 (UAT). |
| `docs/releases/TA_DA_GoLive_2026/TA_DA_PostGoLive_Consolidated_Certification.md` | `certified-evidence` · `open-conflict` | Absorbs RC-001–RC-015G for tag `tada-v1.1-postgolive-fsot-2026` (`3779d185`); §8 adds RC-023/RC-024. Predates the 2026-09-28 checkpoint change and the 2026-10-13 L2-R19 candidate. See §8. |
| `docs/releases/TA_DA_GoLive_2026/TA_DA_Deployment_Checklist.md` | `execution-record` | The most current record. Contains the L2-R19 UAT execution gate (from L386), the 2026-10-14 Production promotion records (from L800), catalog/backup reads (L837–869), and the Sign-off table (L186–208, owner/time/result cells blank). |
| `docs/release/security-admin-v2.3-switch-user/UAT_RESULTS.md` | `certified-evidence` | Switch User feature UAT. |

## 3. Post-go-live review and evidence packs

| Path | Status | Note |
| --- | --- | --- |
| `docs/reviews/Post_GoLive/Post_GoLive_Baseline.md` | `execution-record` · `open-conflict` | Declares itself the frozen reference point and the Sprint 3 starting line. Its certified SQL registry spans UAT `modify_date` **2026-07-28 through 2026-09-15** (`TA_DA_PostGoLive_Consolidated_Certification.md` §3.4). Predates RC-017–RC-024 and the 2026-09-28/10-13 changes. See §8. |
| `docs/reviews/Post_GoLive/Release_Traceability_RC014.md` | `execution-record` | Tag-vs-RC-014 gap. |
| `docs/reviews/Post_GoLive/Sprint3_FSOT_Design.md` | design | FSOT roll-up and `FinalApprovedAmount` ownership design. |
| `docs/reviews/Post_GoLive/Sprint3_Reporting_Reconciliation.md` | `execution-record` | Pre-FSOT money audit. |
| `docs/reviews/Post_GoLive/RC015E_Payroll_Proof.md` | `execution-record` | Pre-deploy payroll proof (FAIL at that time). |
| `docs/reviews/Post_GoLive/RC015F_FSOT_Deployment_Readiness.md` | `execution-record` | Deployment runbook for the FSOT objects. |
| `docs/reviews/Post_GoLive/RC015G_FSOT_UAT_Execution.md` | `certified-evidence` | FSOT deployment proof, 2026-09-15 (UAT). |
| `docs/reviews/Post_GoLive/RC017C_SoftDelete_Face_Refresh.md` | `execution-record` | RC-017C. Companion raw capture: `RC017C_UAT_Evidence_1136.txt`. |
| `docs/reviews/Post_GoLive/RC018/RC018_Execution_Closeout_20260927.md` | `execution-record` | The only RC-018 artifact left in this directory at `HEAD`. The rest of the RC-018 pack is **`not-in-git`** at `HEAD` — see §9. |
| `docs/reviews/Post_GoLive/SubmittedTravelBaseline_UAT_Execution.md` | `execution-record` · untracked | Kilometre-deduction UAT, verdict **HOLD**. States it does not amend `RC015G` or any RC-017 artifact. |

## 4. L2 Auditor As-Is pack

| Path | Status | Note |
| --- | --- | --- |
| `docs/reviews/L2_Auditor_AsIs/L2_Auditor_AsIs_Flow.md` | `as-is-capture` | Flow capture, 2026-09-15. |
| `docs/reviews/L2_Auditor_AsIs/L2_Auditor_CallGraph.md` | `as-is-capture` | Call graph. |
| `docs/reviews/L2_Auditor_AsIs/L2_Auditor_SQL_Matrix.md` | `as-is-capture` | SQL dependency matrix. |
| `docs/reviews/L2_Auditor_AsIs/L2_Auditor_Status_Transitions.md` | `as-is-capture` · `open-conflict` | Captured 2026-09-15, **before RC-014B**. See the conflict notice at the foot of the file and §8. |
| `docs/reviews/L2_Auditor_AsIs/L2_Auditor_Finalization_Contract.md` | `as-is-capture` · `open-conflict` | Contains a historical FAIL verdict, a 2026-10-12 supersession note, and a 2026-10-13 "proposed candidate (not an approved contract)". Status recorded there: L2-R19 BLOCKED — OWNER APPROVAL REQUIRED. |
| `docs/reviews/L2_Auditor_AsIs/L2_Auditor_Risk_Register.md` | `open-conflict` | Open risks; **L2-R19** (stale approved totals / finalization) and **L2-R25** ("SQL provenance: repository ≠ deployed ≠ certified baseline") are the load-bearing entries. |
| `docs/reviews/L2_Auditor_AsIs/RC014A_UAT_Validation.md` | `execution-record` | RC-014A FAIL, superseded by RC-014B. |
| `docs/reviews/L2_Auditor_AsIs/RC014B_RC014D_UAT_Request97.md` | `certified-evidence` | Request 97 proof for RC-014B/RC-014D. |
| `docs/reviews/L2_Auditor_AsIs/RC014B_definition_shape_readonly_2026-10-10T230407.8740319.txt` | `execution-record` | RC-014B read-only definition-shape capture. |
| `docs/reviews/L2_Auditor_AsIs/RC014B_definition_shape_readonly_2026-10-10T232601.2660244.txt` | `execution-record` | RC-014B read-only definition-shape capture. |
| `docs/reviews/L2_Auditor_AsIs/RC014B_catalog_readonly_2026-10-11T083006.7022388.txt` | `execution-record` | RC-014B read-only catalog capture. |
| `docs/reviews/L2_Auditor_AsIs/RC014B_catalog_readonly_2026-10-11T083600.4916002.txt` | `execution-record` | RC-014B read-only catalog capture. |
| `docs/reviews/L2_Auditor_AsIs/RC014C_MarkReady_Contract.md` | `superseded` | Positional history INSERT (error 213), superseded by RC-014D. |
| `docs/reviews/L2_Auditor_AsIs/RC014D_Deployment_Validation.md` | `execution-record` | RC-014D named history INSERT. |
| `docs/reviews/L2_Auditor_AsIs/L2R19_preflight_readonly_2026-10-13T070445Z.txt` | `execution-record` | Read-only preflight output. |
| `docs/reviews/L2_Auditor_AsIs/L2R19_preimage_export_2026-10-13T071700Z/` | `execution-record` | Preimage export: four `.utf16le.bin` bodies, `HASH_VERIFICATION.txt` (all four `match=True`), `Assemble-L2R19Preimages.ps1`. Gates recorded there remain closed. |
| `docs/reviews/L2_Auditor_AsIs/screenshots/` | `execution-record` | Image evidence. |

## 5. RC-019 gate and templates

| Path | Status | Note |
| --- | --- | --- |
| `docs/reviews/RC019/RC019_Foundation_Gate.md` | `execution-record` · `open-conflict` | Gate record. Its constraints reference RC-018 packaging that is absent at `HEAD`. See the notice at the foot of the file and §9. |
| `docs/reviews/RC019/templates/README.md` and the three `RC019_*_Template.md` files | template | Prescribe destinations `docs/reviews/RC019/executions/` and `docs/reviews/RC019/evidence/`, both of which **do not exist yet by design** ("It starts empty"). Not broken links. |

## 6. Deployment packages, tests, and working-tree artifacts

| Path | Status | Note |
| --- | --- | --- |
| `Database/Deployments/L2R19_UAT/` (`README.md`, `A_`–`E_`) | `unapproved-candidate` · `blocked` | Apply/rollback gates shipped closed. See the notice in that `README.md`. |
| `Database/Deployments/MarkReadyAuditorRow/` (`Production_A_`–`Production_D_`, `B_`, `D_`, `R100_`, `R121_`) | `execution-record` · `open-conflict` | Eleven files. Two `*_preimage*.txt` captures (`..._AnmolDristi_preimage.txt`, `..._AnmolDristiDev_preimage.txt`) plus `usp_MarkRequestReadyForPayment_expected_stored.txt`, which is an **expected-body capture, not a preimage**. `Production_B_Apply.sql` ships with `@Execute = 0` (L39–42). See §8. The cross-reference label `D-3` formerly written at the end of this row matches no entry in that register and is recorded as **unresolved** in §9. |
| `Database/Deployments/RC014B/`, `ReturnClaimToEmployee_UAT/`, `AuditorKmFinancial/`, `KmModificationDefault/`, `SubmitSalesDailyActivities_TotalExpenseEntries/`, `HRMS_*` | `execution-record` | Per-RC apply/rollback scripts. |
| `Database/Tests/RC014B/`, `Database/Tests/MarkReadyAuditorRow/`, `Database/Tests/AuditorReturnToL1/` | test plan | Read-only verification plans. `MarkReadyAuditorRow/02_regression_plan_not_run.sql` is explicitly **not run**. |
| `Database/Scripts/001`–`014`, `020`, `020b`, `030`–`031c`, `Diagnostics/` | mixed | Numbering is not contiguous: `015`–`019` and `021`–`029` are absent at `HEAD` — see §9. |
| `Database/Scripts/032_Add_SubmittedClaimBaseline.sql`, `032b_Rollback_SubmittedClaimBaseline.sql` | untracked | Not executed as part of the deduction UAT filing. `032b` is explicitly **not** the rollback for that package. |
| `AnmolDristi/sql/changes/`, `AnmolDristi/sql/diagnostics/` | untracked | Local change/recovery scripts. `20261015_M2562_1493_ReadyForPayment_Return.sql` ships with `@CommitChanges = 0`. |

## 7. Other documentation sets in this repository

`docs/HRMS_Import_Module/` (30+ module documents), `docs/Sprint1-VMS_SelfLogin/`,
`docs/Sprint2-Task01-ProcessApprovalAction/`,
`docs/Sprint2-Task02-ForwardApprovalRequest/` are module- or sprint-scoped and
are not indexed row-by-row here. `AnmolDristi/Resources/**/CONTRIBUTING.md` files
are third-party vendor documents and are not project instructions.

## 8. Open conflicts and pending decisions

Both sides are preserved and the decision is **pending**. No resolution is
asserted by this index.

| ID | Conflict | Sources | Decision owner |
| --- | --- | --- | --- |
| C-1 | **Which procedure pair owns Auditor finalization.** At tag `tada-v1.1-postgolive-fsot-2026` (`3779d185`) `usp_ProcessApprovalAction` writes `Completed` on the last Auditor day and `usp_MarkRequestReadyForPayment` requires `Completed`. At `HEAD` and tag `tada-post-rc019-2026-09-28` (`bc87682f`) the first leaves the header `Pending`, while the second still requires `Completed` at `bc87682f` and additionally accepts `Pending` at `HEAD`. | `Database/Stored Procedures/Approval/usp_ProcessApprovalAction.sql`; `.../usp_MarkRequestReadyForPayment.sql`; the two tags | Release owner / IT Application Owner |
| C-2 | **Is `Completed` allowed to be an approval-engine state?** Phase 1 says no (H-2; TD "Forbidden — [G-05]"), yet both candidate pairs write it. The specification's own approval table is unsigned. | `docs/FunctionalSpecification_MonthlySalesClaims_Phase1_v1.0.md` L373–377, L436–445, L1301; `docs/TechnicalDesignSpecification_MonthlySalesClaims_Phase1_v1.0.md` L1134–1136; the two procedures | BPO (Sales), BPO (Finance), Audit |
| C-3 | **Two definitions write the same payable column.** `Approval/usp_RecalculateApprovalRequest.sql` (L162/L181) and `Utilities/usp_RecalculateApprovalRequest.sql` (L193/L229) both write `FinalApprovedAmount`. | those two files | IT / DB owner |
| C-4 | **Repository vs deployed bodies.** Documented as BLOCKED in L2-R25: repository, UAT, and LIVE bodies differ for several procedures. | `L2_Auditor_Risk_Register.md` (L2-R25); `TA_DA_Deployment_Checklist.md` L837–851 | IT / DB owner, with live-environment evidence |
| C-5 | **The candidate is labelled unauthorized yet recorded as promoted.** `usp_MarkRequestReadyForPayment.sql` L13 says "Not authorized for UAT or LIVE execution"; the checklist records a Production promotion of a body matching that catalog hash on 2026-10-14 07:59. | both files | CAB / IT Application Owner |
| C-6 | **The as-is capture contradicts the certified tag.** | `L2_Auditor_Status_Transitions.md` vs `3779d185` | Release owner (follows C-1) |
| C-7 | **The Post-Go-Live baseline predates RC-017–RC-024** while declaring itself the frozen reference point. | `Post_GoLive_Baseline.md` vs `TA_DA_PostGoLive_Consolidated_Certification.md` §8 and later records | Release owner |
| C-8 | **RC-018 release evidence and scripts are absent at `HEAD`.** 36 files in `docs/reviews/Post_GoLive/RC018/` and 42 scripts `Database/Scripts/016`–`029*` exist at `9ab2e588` and 35/… at `main` `13e1694`, but 1 file and 23 scripts at `HEAD`. | §9 below | Release owner / CAB |
| C-9 | **`.gitattributes` line-ending policy differs from the certified baseline.** | `.gitattributes` at `9ab2e588` vs `HEAD` | IT / DB owner |
| C-10 | **Auditor→L1 routing is implemented in SQL but disabled in the UI.** `usp_ReturnApprovalRequestToL1.sql` exists; `ClaimsApproval_v2.aspx.cs` L1206–1208 refuses the action. | both files | BPO (Sales) + Audit |

### C-1 detail — the two terminal procedure pairs

| Ref | `usp_ProcessApprovalAction` | `usp_MarkRequestReadyForPayment` |
| --- | --- | --- |
| tag `tada-v1.1-postgolive-fsot-2026` (`3779d185`) | last Auditor day writes `Completed` | requires `Completed` |
| tag `tada-post-rc019-2026-09-28` (`bc87682f`) | leaves the header `Pending` | requires `Completed` — no reachable writer |
| `HEAD` (`52c1f32`) | leaves the header `Pending` | review candidate; also accepts `Pending` |

Nothing in this repository resolves which pair ships; the decision is pending.
Before trusting any ref, verify the terminal **pair**, not the tree:

```
git show <ref>:"Database/Stored Procedures/Approval/usp_ProcessApprovalAction.sql"
git show <ref>:"Database/Stored Procedures/Approval/usp_MarkRequestReadyForPayment.sql"
```

**Recovery gate (not a decision, a precondition).** No state-changing UAT is
safe until a UAT full backup finishing after `2026-10-12 14:02:57.583` exists,
passes `RESTORE VERIFYONLY … WITH CHECKSUM`, and the apply/rollback gates are
opened by a named owner. As recorded in `TA_DA_Deployment_Checklist.md` L403,
the newest qualifying UAT backup finished **2026-10-08 11:02:17**, so the gate is
**BLOCKED**.

## 9. Stale or unavailable references — recorded, not "fixed"

These were checked and deliberately **left as written**, because "correcting"
them would assert a fact that the repository does not establish.

| Reference | Where | Finding |
| --- | --- | --- |
| `docs/reviews/Post_GoLive/RC018/` pack (36 files) and `Database/Scripts/016`–`029*` (42 files) | `docs/reviews/RC019/RC019_Foundation_Gate.md` L67–73, L90 | Absent at `HEAD`. Removed by commit `9fdeb78` (2026-09-28 22:29:29 +0530, "RC-017C: drop RC-018 packaging so the pull request is only the soft-delete face refresh.", 78 paths deleted). Present at `9ab2e588` and at `main` `13e1694`. Restoring them is a release/CAB decision. |
| `Database/Scripts/015*_RC018_Phase0_*.sql` | `docs/reviews/RC019/RC019_Foundation_Gate.md` L88–91, L114–117 | Correctly recorded as **untracked local-only** files. Verified: they never existed in any commit. Not a stale reference. |
| `AnmolDristi/Database/Operational/TA_DA_SQL_Reconciliation/` | `TA_DA_Deployment_Checklist.md` L289 | The path exists in no commit and no stash. Recorded evidence unavailable to this repository. |
| `docs/reviews/RC019/executions/`, `docs/reviews/RC019/evidence/` | `docs/reviews/RC019/templates/*` | Prescribed, intentionally empty destinations. Not broken links. |
| `AnmolDristi/PolicyResolutionService.cs` | `TechnicalDesignSpecification_...` L185 | The row lists two candidate locations; the existing file is `AnmolDristi/BLL/PolicyResolutionService.cs`. Editing a specification to pick one is a design claim, not a navigation fix. |
| `See §8, D-3.` (formerly on the `Database/Deployments/MarkReadyAuditorRow/` row) | `docs/INDEX.md` §6 | **Unresolved — no target established.** The §8 register uses `C-1`–`C-10` and no `D-n` series exists anywhere in this repository; a worktree-wide search for `D-3` returns only unrelated matches (`MOD-3` in `docs/HRMS_Import_Module/07_Business_Rules.md`, vendor library strings, and addresses inside uploaded CSV files). By subject matter the row is closest to `C-5` (an unapproved candidate whose apply script ships closed), but no repository artifact states that `C-5` was the intended target, so no replacement is asserted here. **Evidence needed:** the authoring source of the `D-3` label — the review report in which it was written, which is conversation output and not a repository file — or an owner decision naming the intended register entry. |
| `packages/**/README.md` relative links | vendored NuGet packages | Third-party content; out of scope. |

## 10. Verified baseline references

| Ref | Target | Use |
| --- | --- | --- |
| tag `tada-v1.0-go-live-2026` | `94bc2089` | RC-001–RC-013 go-live object. Historical; shipping it alone is recorded as forbidden. |
| tag `tada-v1.1-postgolive-fsot-2026` | `3779d185` | The certified application + SQL baseline (RC-014B/D + RC-015A/B/C). |
| tag `rc-017-022-uat-certified` | `369b47c` | RC-017–RC-022 UAT certification. |
| tag `rc-023-uat-certified` | `4644baf` | RC-023 UAT certification. |
| tag `tada-v1.1-rc017-live-hotfix-2026` | `47f3f65b` | RC-017 LIVE hotfix baseline. |
| tag `rc018-executed-production-20260927` | `9ab2e588` | RC-018 production **execution** closeout; holds the full RC-018 evidence pack. |
| tag `hotfix-auditor-selectall-20260928` | `55432ece` | Auditor bulk Select-All UI hotfix. Carries the **certified** procedure shapes. |
| tag `rc019-foundation-20260928` | `3f60f966` | RC-019 foundation. |
| tag `tada-post-rc019-2026-09-28` | `bc87682f` | **Do not use as a code baseline** — its two terminal procedures are mutually inconsistent (C-1). |

All nine tags are ancestors of `HEAD`. Prefer
`git merge-base --is-ancestor` over commit dates: several tags share 2026-09-28.

### Choosing a baseline

Name the question before choosing:

| Question | Baseline |
| --- | --- |
| *What was certified?* | tag `tada-v1.1-postgolive-fsot-2026` (`3779d185`) |
| *What was executed in production?* | tag `rc018-executed-production-20260927` (`9ab2e588`) plus `docs/releases/TA_DA_GoLive_2026/TA_DA_Deployment_Checklist.md` |
| *What should ship next?* | unresolved — see §8, C-1 |

Never use `tada-post-rc019-2026-09-28` as a code baseline. Do not treat a
repository note as proof of live state, and do not treat the repository as proof
of what is deployed; the two are known to differ.
