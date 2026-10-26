# Proposed Backlog Entry — Assessment of the 84 Remaining Dependabot Alerts

**Status:** **PROPOSAL ONLY — NOT APPROVED, NOT STARTED — AWAITING OWNER APPROVAL.** No dependency change may be made without separate approval. Created 2026-10-26 alongside `Dependabot_Critical_Remediation_Phase5_Closure_20261026.md`. Corrected 2026-10-26 against the Phase 6A review (verdict: NEEDS REVISION) and the Phase 6B classification matrix (verdict: READY FOR BACKLOG APPROVAL); this edit aligns the text with verified evidence and authorizes nothing.

## Baseline to assess (settled scanner state, 2026-10-26)

- **84 open alerts:** 29 high · 53 medium · 2 low · **0 critical**
- By scope: **35 runtime-scope**, 49 development-scope
- Re-verified live at **2026-10-26T10:22:37Z** (`gh api`, exit 0): counts unchanged from the recorded baseline.
- Origin: the vendored trees under `AnmolDristi/Resources/**`, the datetimepicker `.nuspec` metadata files, **and one runtime-scope `zod` alert in `scripts/cursor-sdk/package-lock.json`** (identified by the Phase 6A review); the application's own `AnmolDristi/packages.config` (37 NuGet runtime packages) contributes **zero** alerts.
- Classification status: all **84/84 rows** classified into buckets A–E in the Phase 6B matrix (A 23 · B 0 · C 2 · D 50 · E 9).

## Proposed priority order

1. **Shipped, page-referenced runtime dependencies (highest).** `Resources/jquery/dist/jquery.min.js` **jQuery 2.2.4**. **Six unique open XSS advisories account for 23 alert rows** across plugin manifests that declare jQuery ranges; the rows are counted separately from the advisories, and remediation targets the shipped asset, not the row count. The verified reference is **`Dristi.Master` (line 760)** — `login.aspx` contains no jQuery reference (re-tested 2026-10-26, 0 matches) and must not be cited. Assess exploitability by tracing whether untrusted HTML passes through jQuery DOM insertion, then define the upgrade target (3.7.1 minimum 3.5.0) and a full UI regression scope. This is the already-recognized separate jQuery workstream.
2. **Other runtime-scope rows — served status now established by Phase 6B.** Two plugin trees are **confirmed served**: `bootstrap-daterangepicker` (`daterangepicker.css` / `daterangepicker.js` referenced by app pages) and `jqvmap` (`jqvmap.min.css` referenced). Their runtime rows are jQuery range findings already covered by item 1. **Confirmed unserved** (0 references in `.aspx` / `.Master` / `.cs`): `bootstrap-datetimepicker`, `cropper`, `devbridge-autocomplete`, `ion.rangeSlider`, and the **2 `moment-timezone` rows** — `Resources/moment-timezone` does not exist in the repository; only moment core is served (`Resources/moment/min/moment.min.js`). Served and unserved clusters are assessed separately.
3. **Applicability-review workstream — the nine `.nuspec` rows.** The `bootstrap-datetimepicker/src/nuget/*.nuspec` alerts (Moment.js ×2, bootstrap ×6, Bootstrap.Less ×1) describe packages confirmed **absent from `packages.config`** (0 grep hits), held as packaging metadata in a vendored `src/nuget/` directory with no restore path in the csproj. This workstream produces an applicability evidence note only. **No alert may be dismissed or ignored under this proposal**; any future alert-state change requires separate owner authorization.
4. **Development-scope manifests (lowest) — per-manifest workflow evidence required before any removal is even proposed.** `Resources/Flot/package-lock.json` (10), `Resources/bootstrap/package.json` (7, now scanned standalone after the Phase 5 deletions), assorted plugin dev manifests (grunt/karma/bower/node-sass toolchains), and the runtime-scope `zod` row in `scripts/cursor-sdk/package-lock.json`. For **each** manifest, record before proposing anything: (a) a reference search outside the vendored tree, (b) a build/deploy/maintenance workflow search for its toolchain, and (c) a csproj/restore-path check — the Phase 5 gate, applied per file. **No manifest is presumed safe to delete.** Note that the parent libraries Flot, Chart.js, DateJS, bootstrap and jqvmap have served built assets: only their npm/Ruby toolchains are unexecuted, so deletion versus regeneration on a current toolchain is an owner decision.

## Proposed validation per change

- Release rebuild with the approved `-p:VisualStudioVersion=14.0` toolset override; `git diff --check` both variants; static asset-resolution check (all `.aspx`/`.Master` `Resources/…` references resolve); no served `dist/` asset modified unless that specific asset is the approved subject of the change; page smoke check where the environment supports it without database contact.
- Any change to a served runtime asset (e.g. jQuery) additionally requires the full UI regression cycle, its own commit group, and the established reviewed merge/push flow.

## Explicit constraints

- No dependency upgrades, deletions, alert dismissals, or ignores under this entry until separately approved.
- jQuery 2.2.4 stays untouched until its dedicated task is authorized.
- TA/DA UAT status remains **HOLD**; nothing in this backlog affects release gates.
- Do not bundle any of this with the TA/DA release train.
