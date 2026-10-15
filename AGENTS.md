# AGENTS.md — Anmol Digitization

Read this file before editing anything. It records verified repository facts and
the boundaries that follow from them, and grants no approval for any change.
`docs/INDEX.md` is the governance index: conflict register, open decisions, the
procedure-pair table, and the baseline rule. Unresolved decisions are marked
pending in both files rather than guessed.

## 1. What this repository is

- `AnmolDristi/` — ASP.NET Web Forms on .NET Framework, IIS-hosted, ADO.NET
  (`System.Data.SqlClient`) to SQL Server. Solution `AnmolDristi.sln`.
- `Database/` — T-SQL objects, deployment packages, tests, schema scripts.
- `docs/` — specifications, release records, review and evidence packs.
- ITSM, HRMS import, and VMS share this application and the `DbConn` string.

## 2. Build

Build with MSBuild against `AnmolDristi.sln`. There is no root `package.json` and
no Python build step; do not add a bundler, package manager, or CI system.

## 3. Hard boundaries

1. **Read-only by default.** Do not execute SQL, connect to a UAT or Production
   server, deploy, publish, or run a state-changing test unless the task
   explicitly authorizes it and names the environment.
2. **Do not commit, push, tag, or move a tag** unless asked, and never modify or
   delete an existing release tag (`tada-*`, `rc-*`, `hotfix-*`).
3. **Do not overwrite work you did not make.** Never `git add -A`, `git stash`,
   `git reset --hard`, `git checkout --`, or `git clean`.
4. **`AnmolDristi/Web.config` is a security boundary.** Do not read, print, or
   commit its values and do not change its environment target. It is **tracked at
   `HEAD`**, while its `.gitignore` entry is an uncommitted working-tree change, so
   treat the file as local configuration whatever `git status` reports.
5. **Do not fabricate evidence.** Never record a PASS, signature, date, or
   approval that was not observed. Label anything copied from an earlier record
   as a *quoted record*.
6. **Do not resolve an owner decision by inference.** Open conflicts:
   `docs/INDEX.md` §8.

## 4. SQL Server owns workflow state and the payable amount

- Header status (`SalesApprovalRequest.Status`) is written by stored procedures,
  not by C#. Do not patch `Status` from application code.
- `FinalApprovedAmount` is the payable figure, written by
  `usp_RecalculateApprovalRequest`. Its two tracked definitions — under
  `Database/Stored Procedures/Approval/` and `.../Utilities/` — have different
  bodies. **No canonical body is selected**; changing either needs owner sign-off.
- Latest-detail ownership is `MAX(DetailVersion)` then
  `MAX(ApprovalRequestDetailId)` per `ActivityId`, implemented in
  `AnmolDristi/SalesApprovalDetailVersionHelper.cs`. Reuse the helper.
- `Database/Stored Procedures/Retrieval/usp_GetApprovalRequestDetails.sql`
  returns **all** detail versions; the caller filters.

## 5. Unresolved workflow conflict — do not resolve by inference

`usp_ProcessApprovalAction` and `usp_MarkRequestReadyForPayment` jointly own the
Auditor finalization step, and the repository holds incompatible pairs across
`HEAD` and two tags. Do not pick one. Pair table: `docs/INDEX.md` §8.

## 6. Choosing a baseline

State the question first: *what was certified* → tag
`tada-v1.1-postgolive-fsot-2026`; *what was executed in production* → tag
`rc018-executed-production-20260927` plus the deployment checklist; *what should
ship next* → unresolved. Tag table and terminal-pair checks: `docs/INDEX.md` §10.

## 7. Documentation changes

When two documents conflict, add an **additive** conflict notice: keep both
sources intact, mark the decision pending, cite both sides. Never silently
reconcile competing requirements or rewrite certified evidence. Record a path
that never existed, or exists only outside Git, instead of "fixing" it.

## 8. Before you finish

- Verify by reading the final file, not by assuming the edit landed.
- Report checks you could not run as limitations rather than passes.
- Confirm the working-tree status and prove that pre-existing changes survived.
