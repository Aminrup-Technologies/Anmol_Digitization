# Phase 6E — jQuery 3.7.1 Investigation Closure Record

**Date:** 2026-10-26
**Repository:** `Aminrup-Technologies/Anmol_Digitization` (worktree `D:\PersonalRnD\AminrupWorks\Anmol_Digitization`)
**Branch / HEAD at closure:** `feature/submitted-travel-baseline-deduction` @ `e5979353c33eb226d62fbca60daa1ab86f809092`
**Status:** **ASSESSMENT COMPLETE — IMPLEMENTATION PENDING RUNTIME VALIDATION**
**Status addendum (2026-10-29, draft for owner review):** deployment method **DECISION REQUIRED**; real-page regression remains **BLOCKED** (§5); commit of the candidate is not authorized (§7). §9.8 lists **PROPOSED COMMIT PRECONDITIONS — OWNER CONFIRMATION REQUIRED**; no owner decision on them is evidenced. See §9, which corrects parts of §§2, 4, 5 and 6 without removing them.

This is an administrative closure of the investigation only. It is **not** a fix confirmation, **not** proof of runtime compatibility, and **not** deployment approval. The jQuery vulnerability is **not** remediated in any released or deployed application.

---

## 1. Asset provenance and integrity — VERIFIED

- Target asset: `AnmolDristi/Resources/jquery/dist/jquery.min.js` (jQuery **2.2.4** → proposed **3.7.1**), referenced solely by `Dristi.Master:760`.
- Provenance chain: npm registry `jquery@3.7.1` tarball verified against the registry's own `shasum` (sha1 `083ef989…74de`) **and** `integrity` (sha512 `m4avr8yL…gkpLfg==`) — both byte-exact matches; the extracted `dist/jquery.min.js` (87,533 B, SHA-256 `fc9a93dd241f6b045cbff0481cf4e1901becd0e12fb45166a8f17f95823f0b1a`) is byte-identical to independent downloads from `code.jquery.com` and `cdn.jsdelivr.net`.
- Shipped-asset advisories addressed by 3.7.1: CVE-2015-9251, CVE-2019-11358, CVE-2020-11022, CVE-2020-11023 (all fixed at ≤3.7.1).

## 2. Change scope — ONE PATH, NO RESTRUCTURING

- The replacement is limited to **one existing asset path**, swapped in place. No project restructuring, no additional dependency changes, no solution/project/manifest edits, no plugin upgrades, no `Dristi.Master` or script-sequence change, no removal of jQuery 1.7.2, no jQuery Migrate.
- jQuery is not a NuGet package in this application (0 matches in `packages.config`) and is not referenced by the csproj.

> **Additive notice (2026-10-29, draft for owner review).** The csproj statement above stands, but it has a deployment consequence this section does not record: the inspected project and publish evidence does not establish that this asset reaches a deployment package. See §9.1 and §9.2.

## 3. Build and static checks — PASSED

- Release build: `MSBuild.exe AnmolDristi.sln -t:Rebuild -p:Configuration=Release -p:VisualStudioVersion=14.0` → **exit 0**, 0 errors, 33 pre-existing warnings, `AnmolDristi.dll` produced.
- Static asset resolution: **44/44** `Resources/…` references across 517 pages resolve; none points at a deleted Phase 5 manifest; `git diff --check` exit 0.

## 4. Isolated compatibility checks — PASSED, no demonstrated incompatibility

- jsdom/Node harness (run outside the repository, no IIS/DB): **36/36 checks pass** — jQuery 3.7.1 initialises, core selectors/DOM/events/form helpers work, and all **26** footer plugin scripts that bind to the swapped asset (bootstrap.bundle, Flot ×6, moment, daterangepicker, pnotify ×3, smartWizard, custom.js, …) evaluate cleanly; `daterangepicker` initialises and Bootstrap modal show/hide executes.
- Static API scan (positive-controlled): zero uses of APIs removed in jQuery 3.x (`.load(fn)` shorthand, `.unload`, `.size()`, `.andSelf`, `.live/.die`, `$.browser`, jqXHR `.success/.complete`) in the affected surface; deprecated-but-present APIs in use (`$.trim`, `$.isFunction`, `$.isArray`) verified working at runtime.
- Affected surface is narrow: page content renders **above** `Dristi.Master:760`, so application page scripts run on jQuery 1.7.2 as before; only the footer plugin block (761–784) and footer inline script bind to the swapped asset.

> **Additive correction (2026-10-29, draft for owner review; revised the same day after review).** The "affected surface is narrow" bullet is not established by source. Script order is verified (§9.3). Which jQuery instance a given piece of page code uses is `INFERENCE — NOT RUNTIME-OBSERVED`. Callbacks that look up the global `$` when they run would likely use the instance loaded at `Dristi.Master:760`. Code that captured jQuery at load time, or that runs from the `head` placeholder, would not. The affected surface may therefore be broader than stated — see §9.3. jQuery 1.7.2 also stays loaded — see §9.4. The "26 footer plugin scripts" count does not match the 24 script tags at L761–784 — see §9.5.

## 5. Real-page regression — NOT PERFORMED (blocking reason)

- Real-page regression (login/master render, ClaimsApproval_v2 and legacy Claims Approval, MyClaimsView/ApprovedClaimsView `UpdatePanel` postbacks, SalesMonthlyReview `.then()` chain, validation, dialogs, date controls, wizard, charts/notifications/maps, page-method AJAX, console/network capture) was **not executed**.
- Reason: the documented configuration of this worktree's own local IIS Express site (`http://localhost:53713/`, `physicalPath` = this worktree) is that its `DbConn` targets the **UAT database** — `AnmolDristiDev` on `MUMANMOAPP01\SQLEXPRESS` (evidence: `TA_DA_GoLive_Certification.md`, `TA_DA_Smoke_Test_Execution.md`, `RC014A_UAT_Validation.md`, ADR-0003). Launching the application could contact UAT, which the phase restrictions prohibit.
- A safe isolated database + application configuration has **not** been certified. Minimum unblock (per Phase 6E.3): a separate test copy of the application pointed at a new isolated database (e.g. on the local `AILL079KOL\SQLEXPRESS`), or explicit owner authorization of another safe validation strategy.
- Blocked tests are **not** passes; runtime compatibility remains **unproven**.

> **Additive notice (2026-10-29, draft for owner review).** "`physicalPath` = this worktree" is one of two conflicting IIS Express mappings in the repository's `.vs` folder; which one applies at launch is unresolved. See §9.7.

## 6. Working-tree state at closure — historical snapshot, then current observation

- **2026-10-26/29 evidence snapshot, not a current verification.** At HEAD `e5979353c33eb226d62fbca60daa1ab86f809092`, the jQuery 3.7.1 replacement was a local, uncommitted change in `AnmolDristi/Resources/jquery/dist/jquery.min.js` (SHA-256 `fc9a93dd…f0b1a`). That SHA-256 and that HEAD identify this snapshot only.
- **Observed 2026-11-05** on `Aug26_AILWorks_AmimnrupSupport` at `0a0689aaaa244dbc0dde519abf1e2b1bfd07a803`. The current worktree does not show `AnmolDristi/Resources/jquery/dist/jquery.min.js` as modified. The committed library still begins `/*! jQuery v2.2.4`. `Dristi.Master` still references `~/Resources/Scripts/jquery-1.7.2.min.js`. Runtime validation and deployment remain pending. Vulnerability remediation is **NOT CONFIRMED**. This record does not authorize a commit or deployment of a jQuery library replacement.
- Original asset rollback path recorded for that snapshot: `git checkout -- AnmolDristi/Resources/jquery/dist/jquery.min.js`, or the byte-identical backup `%TEMP%\jquery-2.2.4.rollback.min.js`, SHA-256 `b409c14a10b4caad6b54844aa63a5faf748b83eecc2dd0d4fb1d913f8de55365` (pre-change working file). That command is a historical note, not an instruction to run it.
- Excluded pre-existing changes recorded for that snapshot and left untouched by the investigation: `.gitignore`, `AnmolDristi/Web.config`, `Database/Stored Procedures/Approval/usp_ProcessApprovalAction.sql`.

> **Additive correction (2026-10-29, draft for owner review).** `usp_ProcessApprovalAction.sql` is not a modified path in the current working tree — see §9.6. The `git checkout --` rollback above is a human action; `AGENTS.md` §3.3 prohibits agents from running it.

## 7. Prohibitions preserved

- **No commit, push, merge, deployment, or Dependabot alert dismissal is authorized** by this record; none occurred.
- jQuery 2.2.4 remediation stays separate from the TA/DA release train; the 23 declared-range alert rows are untouched (separate manifest workstream).
- No database object, dependency manifest, or application configuration was modified.
- **TA/DA UAT status remains HOLD.**

## 8. Reopening conditions

The implementation task reopens **only** when:
1. an isolated test database and matching application configuration are available and certified, **or**
2. the owner explicitly approves another safe validation strategy.

No further remediation phase, source modification, or infrastructure provisioning is created by this closure. Until reopening, the shipped application continues to carry jQuery 2.2.4 with its open advisories, and this investigation's outcome is documentation only.

---

## 9. Additive corrections and deployment-governance findings (2026-10-29, draft for owner review)

Added after a read-only review of `feature/submitted-travel-baseline-deduction` at HEAD `e5979353c33eb226d62fbca60daa1ab86f809092`. Sections 1–8 are kept as written. Where this section corrects them, the earlier text stays in place and is superseded only to the extent stated here. No build, publish, deployment, browser, IIS, SQL, or CDN action was taken for this section. The §1 provenance downloads, and the §3 build `PASSED` and 44/44 static-resolution results, are historical results reported for 2026-10-26; they were not rerun during this review. This section was revised the same day after an independent documentation review, which is not recorded in the repository. The revision changes only §9, its pointer notices, and the status addendum.

Labels used below: `VERIFIED` = re-read from repository source on 2026-10-29 (source text and order only, not runtime behaviour) · `PRIOR FINDING` = observed in the 2026-10-29 read-only investigation and not re-checked when this section was written · `INFERENCE` = expected behaviour that was not reproduced; `INFERENCE — NOT RUNTIME-OBSERVED` marks claims about which jQuery instance code uses · `UNRESOLVED` · `DECISION REQUIRED`.

### 9.1 Deployment package does not establish delivery of the asset

- `VERIFIED` — `AnmolDristi/AnmolDristi.csproj` L49 sets `FilesToIncludeForPublish` to `OnlyFilesToRunTheApp` for `Release|AnyCPU`. A case-insensitive search of the csproj for `Resources` returns 0 matches. `Dristi.Master` (L307) and `Scripts\claims-ux-stabilization.js` (L360) are `Content` items.
- `VERIFIED` — `AnmolDristi/Properties/PublishProfiles/LocalDeploy.pubxml`: L8 `FileSystem`, L18 `publishUrl` `D:\DeployPackages`, L19 `DeleteExistingFiles` `True`. `…/ailqaqc.aminruptechnologies.co.in - Web Deploy.pubxml`: L8 `MSDeploy`, L18 `SkipExtraFilesOnServer` `True`.
- `PRIOR FINDING` — The csproj has no custom publish target; L1843–1894 contain imports, the NuGet guard, and `CleanLockedAspNetMergeDir`. No `.wpp.targets` file or publish script exists outside the vendored `Resources\` tree.
- `PRIOR FINDING` (observed package contents) — `D:\DeployPackages` contained `Dristi.Master`, `Scripts\claims-ux-stabilization.js` and `bin\AnmolDristi.dll`, and **no `Resources` folder**. The Visual Studio publish file list for `LocalDeploy` (an untracked profile user file, not opened when this section was written) listed no `Resources\` path and no jQuery file.
- `INFERENCE` (no publish was reproduced) — A Release publish with either profile would leave `Resources/jquery/dist/jquery.min.js` out of the package. Under `LocalDeploy`, the package folder would not contain it. Under the Web Deploy profile, `SkipExtraFilesOnServer` would leave any existing server-side `Resources\jquery\dist\jquery.min.js` in place.
- `INFERENCE` (operational risk, following from the inference above; not observed) — The 3.7.1 asset may never reach the server even when a release is recorded as published. If so, the server would keep serving whatever file it already holds; 2.2.4 is expected but not verified. A rollback limited to DLL and published pages would likewise not touch the asset. `VERIFIED`: the reference `ResolveUrl("~/Resources/jquery/dist/jquery.min.js")` (`Dristi.Master` L760) has no version token. That browser or proxy caches could therefore hide which file is served is also `INFERENCE`.
- `UNRESOLVED` — How `Resources\` reached the existing UAT and LIVE servers is not documented by any repository record or script found in this review.

### 9.2 Deployment method — `DECISION REQUIRED`

No approved mechanism exists to deliver, verify, or roll back a `Resources\` asset. Copying the single file manually is a **proposal only**, not an approved procedure. Any approved mechanism must specify at least:

1. The exact target path on each server: `<site physical path>\Resources\jquery\dist\jquery.min.js`.
2. A pre-deployment backup of the server's existing file, and how to recover it.
3. Post-deployment verification against the expected version or an approved content hash. The candidate is Git blob `7f37b5d99122b4928505d1e0ac5ca7279edd56ed`; its LF working-tree SHA-256 is `fc9a93dd…f0b1a`. `PRIOR FINDING`: this repository checks text out with CRLF on Windows (`core.autocrlf=true`, `text=auto`), so the SHA-256 of a checked-out copy can differ. The approved hash must name the exact bytes it applies to.
4. Rollback of the asset itself, to the server backup or to Git blob `4024b6622b889bb65476341a48a4e5b6485b16b9` (2.2.4).
5. Evidence capture and the named responsible operator.

No owner for this decision is named in the repository.

### 9.3 Script order versus jQuery instance usage (corrects the §4 surface bullet)

Static source order is verified below. Which jQuery instance any code actually uses has **not** been observed at runtime.

- `VERIFIED` (source order, `Dristi.Master`):
  - L99–100: the `head` content placeholder.
  - L102: `~/Resources/Scripts/jquery-1.7.2.min.js`.
  - L491: `</head>`.
  - L683: `ContentPlaceHolder1`.
  - L760: the candidate asset.
  - L761–784: plugin scripts.
- `VERIFIED` (library source, read statically):
  - The candidate 3.7.1 file ends by assigning `window.jQuery` and `window.$` when loaded as a plain script (`"undefined"==typeof e&&(ie.jQuery=ie.$=ce)`).
  - The local `Resources/bootstrap/dist/js/bootstrap.bundle.min.js` (Bootstrap 4.3.1, loaded at L761) passes the global `jQuery` present at its own load time into its factory (`e((t=t||self).bootstrap={},t.jQuery)`).
- `INFERENCE — NOT RUNTIME-OBSERVED` — This follows from synchronous script execution and the order above:
  - Page code between L102 and L760 that uses `$` immediately would use 1.7.2.
  - Code that looks up the global `$` only when it later runs would use the instance loaded at L760. That covers `$(document).ready` and `Sys.Application.add_load` callbacks, event handlers, and AJAX callbacks.
  - Bootstrap would capture the L760 instance.
  - No browser run has observed any of this. It is not true of all later code; see the exceptions below.
- Exceptions (source `VERIFIED`; consequence `INFERENCE — NOT RUNTIME-OBSERVED`):
  1. **`head` placeholder.** Content-page markup injected at `Dristi.Master` L99–100 is parsed before L102. Script there that runs immediately may execute before either jQuery instance is loaded.
  2. **Closure capture.** `Scripts/HrmsBatchManager.js` wraps its code in a function that receives `window.jQuery` as `$` when the script loads (L5 and L261). `HrmsBatchManager.aspx` loads it at L395, inside its `ContentPlaceHolder1` content (L8–396), which places it before `Dristi.Master` L760 in source order. Its later callbacks would keep using the captured instance (inferred to be 1.7.2), not the global `$`.
- Example: `ClaimsApproval_v2.aspx`. This was reported earlier as a `PRIOR FINDING`; the source lines below were re-verified in this revision.
  - `VERIFIED` (source):
    - DataTables 1.10.20 and its extensions are loaded from `cdn.datatables.net` at L1034–1037, inside `ContentPlaceHolder1` content that starts at L424.
    - `applyDataTables` is L3007–3029 and `initDataTablesSafe` is L3031–3037; both test the global `$.fn.dataTable`.
    - `initClaimsApprovalPageUx`, which calls `initDataTablesSafe`, is registered through `Sys.Application.add_load` at L3051.
    - Modal event handlers are registered with `$(document).on(...)` at L3060–3086, in a top-level script block (L1039–3119).
  - `UNRESOLVED` — Which jQuery instance the CDN DataTables build attaches to. The CDN file was not inspected. The local copies (`Resources/datatables.net`, 1.10.12; `Resources/DataTables`, 1.13.1) are different builds from the one the page loads, so they do not establish its behaviour.
  - `INFERENCE — NOT RUNTIME-OBSERVED` — The modal handlers would be registered through 1.7.2, and Bootstrap would dispatch its modal events through the instance it captured. If those differ, the handlers might not receive the events. Bootstrap's event-dispatch path was not traced in source.
- `UNRESOLVED` — Whether any of these cross-instance paths fail, under 2.2.4 or 3.7.1, needs a paired browser run. The `SalesMonthlyReview` `.then()` chain listed in §5 is likewise only inferred to run on the L760 instance.

### 9.4 jQuery 1.7.2 remains loaded

- `VERIFIED` — `Dristi.Master` L102 still loads `jquery-1.7.2.min.js`, and the candidate replaces only the file loaded at L760. Upgrading that file neither removes nor upgrades the L102 reference.
- `INFERENCE — NOT RUNTIME-OBSERVED` — On pages that use this master, 1.7.2 would serve two kinds of code: page code that runs immediately between L102 and L760, and code that captured it at load time (§9.3, exception 2). Content in the `head` placeholder (L99–100) is parsed before L102 and would not have it (§9.3, exception 1).
- Public advisory ranges, not re-verified here, place 1.7.2 within CVE-2012-6708, CVE-2015-9251, CVE-2019-11358, CVE-2020-11022 and CVE-2020-11023. Deploying 3.7.1 at L760 would therefore not by itself close the jQuery advisory exposure. The preface statement that the jQuery vulnerability is not remediated remains accurate.
- Disposition of 1.7.2 (remove with its own regression, or accept the risk): `DECISION REQUIRED`.

### 9.5 Footer plugin count (26 vs 24) — `UNRESOLVED`, preserved

- `VERIFIED` — `Dristi.Master` L761–784 contain **24** `<script src>` tags. Inline script blocks sit at L747 and L786.
- §4's "26 footer plugin scripts" comes from a harness run outside the repository that cannot be re-inspected. It is neither confirmed nor corrected here. §4's 36/36 harness result remains a quoted record, not a result verified in this review.

### 9.6 Working-tree statement in §6 (correction)

- `VERIFIED` (`git status` at HEAD `e5979353`, 2026-10-29) — The working-tree changes are `.gitignore`, `AnmolDristi/Resources/jquery/dist/jquery.min.js` and `AnmolDristi/Web.config` (modified), plus this record (untracked). `Database/Stored Procedures/Approval/usp_ProcessApprovalAction.sql` is **not** modified. This snapshot was captured before this documentation task added its notices to `docs/releases/TA_DA_GoLive_2026/TA_DA_Deployment_Checklist.md`, so it does not include that later change.
- §6's listing of `usp_ProcessApprovalAction.sql` as an excluded pre-existing change does not describe the current tree. Whether it was modified on 2026-10-26 cannot be determined from current evidence.

### 9.7 IIS Express mapping conflict (affects §5) — `UNRESOLVED`

- `VERIFIED` — `.vs/config/applicationhost.config` L163–168 maps site `AnmolDristi` on `*:53713:localhost` to `D:\PersonalRnD\AminrupWorks\Anmol_Digitization\AnmolDristi`. `.vs/AnmolDristi/config/applicationhost.config` L156–161 maps the same site name and binding to `D:\AIL_AminrupTech\AnmolDristi`.
- `PRIOR FINDING` — `D:\AIL_AminrupTech\AnmolDristi` did not exist on 2026-10-29.
- Which file Visual Studio applies at launch is not established from source. §5's "`physicalPath` = this worktree" is one of two configured mappings. Proposed (§9.8): a regression environment should use a single, dedicated mapping to an isolated application copy.

### 9.8 Regression blocked; commit preconditions proposed

- Real-page regression: **BLOCKED** (unchanged from §5). None of the following prerequisites is in place.
  - From §5 and §8: an isolated database and a matching application configuration.
  - From `AGENTS.md` §3.1: authorization naming the environment.
  - Proposed by this review: external-network isolation, including the UAT and LIVE database hosts, plus a decision on CDN-hosted page scripts.
  - Proposed by this review: a single IIS Express mapping.
- Commit of the candidate is not authorized by this record (§7). **PROPOSED COMMIT PRECONDITIONS — OWNER CONFIRMATION REQUIRED:**
  - paired regression evidence;
  - an approved deployment mechanism (§9.2);
  - a recorded disposition of jQuery 1.7.2 (§9.4).

  This review evidences no owner decision on these preconditions. The owner must decide explicitly whether the deployment mechanism and the 1.7.2 disposition must be settled before a commit, or only before a deployment.
- §8's reopening conditions are unchanged. The proposed preconditions above neither amend nor replace them.
- This section authorizes no deployment, test, commit, or release, and records no PASS.
