# Phase 5 — Dependabot Critical Remediation: Final Closure Record

**Date:** 2026-10-26
**Repository:** `Aminrup-Technologies/Anmol_Digitization`
**Scope of this record:** the approved vendored-manifest cleanup only, and the verified reduction of critical Dependabot alerts that resulted from it.
**Status:** **PHASE 5 COMPLETE — LIMITED SCOPE.** This record does **not** mark the security backlog complete, does not change any Dependabot alert state, and does not alter any release gate.

---

## 1. Change and integration

| Item | Verified value |
| --- | --- |
| Remediation commit | `862cf402c894631221d20ad3b8c0c1c08a930fa5` — `chore(security): remove unused vendored build manifests` |
| Approved deletions | **Exactly four**, `4 files changed, 11085 deletions(-)`, 0 insertions, 0 modifications |
| Files deleted | `AnmolDristi/Resources/bootstrap/package-lock.json` · `AnmolDristi/Resources/bootstrap/Gemfile` · `AnmolDristi/Resources/bootstrap/Gemfile.lock` · `AnmolDristi/Resources/jquery.easy-pie-chart/package.json` |
| Target branch | `Aug26_AILWorks_AmimnrupSupport` |
| Integration method | `git merge --ff-only` in worktree `D:\PersonalRnD\AminrupWorks\Anmol_RC019_Default`; `Updating f566eea..862cf40`, exit 0, no conflicts (pre-verified with `git merge-tree`) |
| Verified remote SHA | `862cf402c894631221d20ad3b8c0c1c08a930fa5` — confirmed by `git ls-remote origin refs/heads/Aug26_AILWorks_AmimnrupSupport` after `PUSH_EXIT=0` |
| Push scope | **Target branch only.** Feature branch not pushed (remote remains `f566eea`); no tags pushed |

## 2. Dependabot scanner results (measured, not assumed)

| When | Total open | Critical | High | Medium | Low |
| --- | --- | --- | --- | --- | --- |
| Before (baseline) | 284 | 25 | 123 | 116 | 20 |
| Mid-rescan observation | 151 | 6 | — | — | — |
| **Settled (stable across two consecutive queries)** | **84** | **0** | 29 | 53 | 2 |

- **All 25 critical alerts are resolved.** Zero open alerts reference any of the three critical-bearing manifests that were deleted (`bootstrap/package-lock.json`, `bootstrap/Gemfile.lock`, `jquery.easy-pie-chart/package.json`).
- No alert was dismissed or ignored to achieve this result; the reduction came from removing the manifests the alerts were anchored to.
- Residual alerts: 49 development-scope, 35 runtime-scope, all in the untouched vendored `AnmolDristi/Resources/**` trees and the datetimepicker `.nuspec` metadata files. They are tracked as a separate backlog entry (see `Dependabot_Backlog_Assessment_Proposal_20261026.md`).

## 3. Build and static asset verification evidence

- **Command:** `MSBuild.exe AnmolDristi.sln -t:Rebuild -p:Configuration=Release -p:VisualStudioVersion=14.0 -v:minimal -nologo`
- **Result:** `BUILD_EXIT=0`, **0 errors**, 33 warning lines — identical pre-existing set (CS0168 ×19, CS0219 ×7, CS1998 ×2, CS0414 ×1, MSB3305 ×4) — `AnmolDristi\bin\AnmolDristi.dll` produced.
- **Static asset verification:** all 40 `Resources/…` asset references extracted from every `.aspx`/`.Master` page resolve to files on disk (**0 missing**); **no page references any deleted manifest**; probes for `bootstrap/dist/css/bootstrap.min.css`, `bootstrap/dist/js/bootstrap.bundle.min.js`, `jquery/dist/jquery.min.js` all present.
- Diff proof: the commit contains only `D` entries; filters for `dist/`, `/js/`, `/css/`, `.min.js`, `.min.css`, `jquery.js` returned no matches. `git diff --check` and `git diff --cached --check` both exit 0 before and after the commit and merge.
- **Limitation:** verification is build + static asset resolution, **not runtime validation**. No live page-load test was performed because it requires IIS and a database connection, which this phase prohibited.

## 4. Prohibitions and gates — preserved

- **No deployment. No database execution.** No SQL ran against any environment; no UAT/Production contact.
- **jQuery 2.2.4 not modified.** Its six unique runtime XSS advisories remain open and are scheduled as a **separate** remediation with its own regression cycle.
- **TA/DA UAT status remains HOLD.** This cleanup satisfies **none** of the pending gates: package-specific CAB approval, qualifying production backup + `RESTORE VERIFYONLY`, pre-deployment IIS copy, live stored-procedure script-outs (incl. `dbo.usp_SubmitSalesDailyActivities`), and a rollback proven from those artifacts. `032` has still never been executed.
- Excluded working-tree files untouched and uncommitted throughout: `AnmolDristi/Web.config`, `.gitignore`, `Database/Stored Procedures/Approval/usp_ProcessApprovalAction.sql` (G4, pending C-1).
- Stashes 14 and tags 9 unchanged; no unrelated worktree, branch, or alert state altered.

## 5. Phase 5 completion statement

Phase 5 is **complete and closed**, limited to: deletion of the four approved unused vendored build manifests, the reviewed fast-forward integration and verified push of `Aug26_AILWorks_AmimnrupSupport`, and the confirmed reduction of critical Dependabot alerts from 25 to 0. Nothing beyond that scope was done, and the remaining 84 open alerts are **not** covered by this closure.
