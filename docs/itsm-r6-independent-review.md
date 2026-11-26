# Independent review — ITSM R6 support group assignment

| | |
| :--- | :--- |
| **Worktree** | `D:\PersonalRnD\AminrupWorks\Anmol_ITSM_R6` |
| **Branch** | `cursor/itsm-r6-group-assignment` |
| **HEAD** | `f1f4bdb2c82013e913ac682aa5e31832a3fa94fc` (unchanged; all work uncommitted) |
| **Base tip** | `Aug26_AILWorks_AmimnrupSupport` @ `f1f4bdb` |
| **When** | 26 November 2026 (Asia/Calcutta) — release-readiness close |
| **Method** | Full uncommitted diff vs `f1f4bdb`; EF6 Target re-validated against repo baseline + live model; migration vs SQL read-only; tests + Debug compile re-run |
| **Boundaries** | No commit/push/PR/deploy; no migration or SQL apply; `Web.config` and Digitization worktree untouched |

---

## Final disposition (release-readiness)

| Gate | Status |
| :--- | :--- |
| Uncommitted R6 diff inspected vs `f1f4bdb` | Done |
| EF6 Designer `Target` vs baseline + current model | **Pass** — live `EdmxWriter` output matches stored Target (SHA256 unchanged on re-run); not a copy of `AddApprovalConfiguration`; hash not invented |
| Scaffolding (`MigrationsPendingException`) explained | Documented below; offline Target path used |
| Migration vs `itsm_r6_support_groups.sql` | **Aligned** on schema intent; discrepancies listed (indexes / names / idempotency) |
| High fixes H1–H5 | Confirmed in code |
| Tests 1–17 | **PASS** — build EXIT **0**, run EXIT **0** |
| AnmolDristi Debug compile | EXIT **0** → `D:\PersonalRnD\AminrupWorks\_r6_review_build_out\` |
| Accepted product limits recorded | `ITSM.ApprovalConfig.Manage` reuse; individual-save does not reject group (group still wins at dispatch) |
| Browser / IIS / database | **Not claimed** |
| Ready to commit/PR from this agent | **No** — stop per instruction; owner decides next |

**Verdict:** Code and offline EF snapshot are release-review complete for R6 group assignment on this tip. Environment apply remains an **owner-authorized** step. Do not treat this review as a database PASS.

---

## Discovery notes — `Update-Database` target resolution (26 Nov 2026 Asia/Calcutta)

**Classification:** Target-resolution / discovery failure only. No apply, no SQL, no `__MigrationHistory` change. AnmolDristiDev / `115.112.186.71` **not** contacted. No second migration; MigrationId unchanged; hash not invented.

### Why `Update-Database -TargetMigration 202611261430000_AddSupportGroups` said it does not exist

EF6 error text: **`'{0}' is not a valid migration.`** raised from **`DbMigrator.GetMigrationId`**, which resolves the target against **local** `MigrationAssembly.MigrationIds` (name / suffix match) — **not** against `__MigrationHistory`.

So this is **not** explained by “`Get-Migrations` only lists applied history.” That PMC cmdlet is a separate applied-history view. `Update-Database` failed because the **assembly PMC loaded** (`AnmolDristi\bin\AnmolDristi.dll` via project `OutputPath`) did **not** yet contain `AddSupportGroups` (DLL missing/stale while sources were already wired). Target resolution never reached schema apply.

### Source wiring (no source fix required)

| Item | Value |
| :--- | :--- |
| Compile | `.cs` + `.Designer.cs` (`DependentUpon`) |
| EmbeddedResource | `.resx` (`DependentUpon`) |
| Class / namespace | `AnmolDristi.ITSM.DAL.Migrations.AddSupportGroups` |
| Designer MigrationId | `202611261430000_AddSupportGroups` |
| ContextKey (Configuration) | `AnmolDristi.ITSM.DAL.Migrations.Configuration` |
| Target metadata | resx `Target` length `14156` |

### Rebuild + proof (this pass)

- `MSBuild /t:Rebuild` Debug → EXIT **0**
- Assembly PMC uses: `D:\PersonalRnD\AminrupWorks\Anmol_ITSM_R6\AnmolDristi\bin\AnmolDristi.dll`
- `DbMigrator.GetMigrationId("202611261430000_AddSupportGroups")` → **`202611261430000_AddSupportGroups`**
- `GetMigrationId("AddSupportGroups")` → same
- `GetLocalMigrations()` includes all six IDs ending with **`202611261430000_AddSupportGroups`**
- Pending vs applied on AnmolDristiDev: **not queried** (forbidden); last known owner evidence still history through `AddApprovalConfiguration` — do not assume history changed

### `Get-Migrations` (separate)

Still lists **applied** history only; will omit pending `AddSupportGroups` until apply. That is expected and unrelated to the `Update-Database` target-resolution error above.

### Owner next step (not run here)

With the rebuilt bin DLL, authorized `Update-Database -TargetMigration 202611261430000_AddSupportGroups` (or the one-path SQL script) can proceed.

---

## EF6 snapshot validation (no database apply)

### Repo migration baseline

Chain discovered by `DbMigrator` on this tip:

1. `202608120550515_InitialItsmSchema`
2. `202608201200000_AddTicketAuditLog`
3. `202608210700000_AddTicketCompanyId`
4. `202608220132564_AddBranchLegacyId`
5. `202608240451107_AddApprovalConfiguration` ← **model baseline for R6**
6. `202611261430000_AddSupportGroups` ← R6 (untracked)

Designer Id = `202611261430000_AddSupportGroups`; Source = `null` (same pattern as prior chain); Target from resx.

### Why scaffolding throws `MigrationsPendingException`

EF6 `MigrationScaffolder` / `ef6 migrations add` refuses to scaffold when the **target database** has migrations in the assembly that are **not yet recorded** in `__MigrationHistory`. On an empty LocalDB (or any DB without the Aug26 ITSM history), *every* prior migration is pending, so scaffolding aborts before it can emit a correct `Target`.

Applying those pending migrations would execute `Up()` SQL against a database. That was **forbidden** for this review (no apply / no modify). Therefore the CLI/scaffolder path cannot be completed here.

### Which baseline is required

A database whose `__MigrationHistory` already ends at **`202608240451107_AddApprovalConfiguration`** (Aug26 / `f1f4bdb` ITSM model), with the corresponding schema present. That is the last committed migration before `AddSupportGroups`. Scaffolding or `Update-Database` for R6 is only valid against that baseline (or a disposable clone of it).

### Offline validation performed (hash not invented)

Tool: `tools/Ef6SnapshotRefresh` — `EdmxWriter.WriteEdmx(ItsmDbContext)` → GZip → Base64 (EF’s resx envelope). Re-run 26 Nov 2026 Asia/Calcutta:

| Check | Result |
| :--- | :--- |
| AddSupportGroups Target == ApprovalConfiguration Target? | **False** (lengths `14156` vs `12492`) |
| Baseline ApprovalConfiguration EDMX | `123232` chars; has `MST_ApprovalConfiguration`; **no** `MST_SupportGroup` |
| Current model EDMX | `148847` chars; has `MST_SupportGroup` / `Member` / `Mapping` + `SupportGroupId` |
| New compressed Target length | `14156` |
| SHA256 of resx before/after re-run | **Identical** `83A551930C14D48DBC67CCF7D73EE730AD443F989CF2A8ED85E0004B5C2FA86F` — stored Target already matches live model |
| Round-trip decode contains `MST_SupportGroup` | Yes |
| Designer returns AddSupportGroups Id | Yes |
| Database modified | **No** |

### Safe next validation step (owner-authorized only)

1. Point tools at a **named** environment (or disposable LocalDB clone) that already has Aug26 schema + `__MigrationHistory` through `AddApprovalConfiguration`.
2. Generate a script for **only** `AddSupportGroups` (e.g. `Update-Database -Script -SourceMigration AddApprovalConfiguration -TargetMigration AddSupportGroups`) **or** apply the checked-in `itsm_r6_support_groups.sql` — pick **one** path per environment, not both blindly.
3. After apply: confirm `__MigrationHistory` row for `202611261430000_AddSupportGroups`, compare stored Model blob to Designer Target, and smoke the app for “model backing the context has changed.”
4. Do **not** invent or paste a Target hash by hand.

---

## Migration vs `itsm_r6_support_groups.sql`

**Aligned (intent):**

- Tables: `itsm.MST_SupportGroup`, `MST_SupportGroupMember`, `MST_SupportGroupMapping`
- Columns (including group audit codes, member TL/active flags, mapping Category/SubCategory levels)
- FKs to `dbo.MST_Branch`, `dbo.MST_UserMaster`, `itsm.MST_Category` / `MST_SubCategory` / `MST_SupportGroup`
- Named indexes: `IX_MST_SupportGroup_Company_Branch`, `UX_MST_SupportGroupMember_Group_Employee`, `IX_MST_SupportGroupMapping_Company_Branch`
- Filtered unique indexes: `UIX_MST_SupportGroupMember_OneActiveLeader`, `UIX_MST_SupportGroupMapping_ActiveParent`, `UIX_MST_SupportGroupMapping_ActiveCategory`
- `itsm.Tickets.SupportGroupId` nullable + FK to `MST_SupportGroup` + index

**Discrepancies (report, do not “fix” by inventing a second schema):**

| Topic | Migration (`AddSupportGroups`) | SQL script |
| :--- | :--- | :--- |
| Extra non-unique FK indexes | Also creates indexes on `BranchId` (group), `SupportGroupId` + `EmployeeCode` (member), `BranchId`/`CategoryId`/`SubCategoryId`/`SupportGroupId` (mapping) | Omits those; relies on unique/filtered/company-branch indexes |
| Constraint / index names | EF default names for most FKs; Tickets index via `CreateIndex("SupportGroupId")` | Explicit `PK_*`, `FK_*`, `FK_Tickets_SupportGroup`, `IX_Tickets_SupportGroupId` |
| Idempotency | Not idempotent (fails if objects exist) | `IF OBJECT_ID` / `COL_LENGTH` guards |
| History bookkeeping | Writes `__MigrationHistory` when applied via EF | Does **not** insert history — using SQL alone leaves EF history behind the model |

**Ops rule:** Per environment, choose EF migration **or** SQL script. If SQL is used, DBA must also reconcile `__MigrationHistory` (or accept that EF migrate is not the apply path for that env).

---

## Accepted limits (product / tip)

### 1. Permission: `ITSM.ApprovalConfig.Manage`

**Accepted for R6 on this tip.** No new permission key invented.

Real path:

1. Catalog: `AnmolDristi/Database/itsm_navigation_permissions.sql` → `ITSM.ApprovalConfig.Manage` (PagePath `ApprovalConfig.aspx`)
2. Grant: `itsm_role_permissions.sql` → `ITSMAdmin`
3. Runtime: `BasePage.HasPermission` / `RequirePermission`
4. R6 UI: `SupportGroupConfig.aspx.cs` uses the same key as `ApprovalConfig.aspx.cs`

**Consequence:** Anyone who can manage approval config can manage support groups. A dedicated key remains an **owner decision**, not an agent invent.

### 2. Individual-save vs group mapping

**Accepted limitation on this tip:**

- **Group save → individual conflict:** `SupportGroupAdminService.SaveMapping` **rejects** when an active individual mapping conflicts (`ValidateNoConflictingIndividual`).
- **Individual save → group conflict:** There is **no** individual-mapping admin writer/BLL on the Aug26 tip that rejects an active group mapping at the same level. Individual-save therefore **does not** block a group mapping.
- **Dispatch if both rows exist:** `SupportGroupRules.ResolveDispatch` still prefers Category group, then Parent Category group, then the individual cascade — **the group wins**, so a stale individual row is ignored at create-time routing.

---

## High fixes (confirmed)

| Id | Fix | Evidence |
| :--- | :--- | :--- |
| H1 | Leader requires HR `EmployeeStatus == Active` | Code + test **16** PASS |
| H2 | Claim/member/queue require active group + company | Code (`ItsmTicketAccess`, queue, member list) |
| H3 | `ReassignTicket` may clear assignee on process path; TL assign still requires target | Code |
| H4 | Category group match needs `CategoryId` + `SubCategoryId` | Code + test **17** PASS |
| H5 | Member upsert + agent list enforce company **and** branch | Code |

---

## Build / test results (this close-out)

| Step | Exit code | Notes |
| :--- | ---: | :--- |
| `ItsmSupportGroupTests` Debug build | **0** | `tests/ITSM.SupportGroupTests/ItsmSupportGroupTests.csproj` |
| `ItsmSupportGroupTests.exe` run | **0** | All assertions 1–17 PASS |
| `AnmolDristi.csproj` Debug | **0** | Output `D:\PersonalRnD\AminrupWorks\_r6_review_build_out\` (temp path; avoids locked bin DLL) |
| EF6 snapshot re-validate tool | **0** | Target SHA unchanged |

Pre-existing zkemkeeper/CS warnings only. **Not claimed:** browser, IIS, database, migration apply.

---

## Remaining risks

1. **Apply not done** — first authorized env must pick EF **or** SQL and reconcile `__MigrationHistory` if SQL is used.  
2. **`ITSM.ApprovalConfig.Manage` too wide** — accepted until owner adds a dedicated key.  
3. **Individual-save cannot reject group mappings** on this tip; dispatch group-wins masks conflict at runtime but data can still coexist.  
4. **No automated tests** for inactive-group claim, clear-assignee, company/branch admin writes, permission gate, or concurrency (code-reviewed only).  
5. **Index/name shape** differs between migration and SQL — DBA must not apply both.  
6. **Future `migrations add`** still needs the Aug26 history baseline DB; offline Target is validated but is not a substitute for an authorized apply smoke.

---

## Changed files and git status

**Branch:** `cursor/itsm-r6-group-assignment`  
**HEAD:** `f1f4bdb2c82013e913ac682aa5e31832a3fa94fc` (no new commits)

### Modified (15)

- `AnmolDristi/AnmolDristi.csproj`
- `AnmolDristi/ITSM/BLL/DispatchService.cs`
- `AnmolDristi/ITSM/BLL/ItsmTicketAccess.cs`
- `AnmolDristi/ITSM/BLL/TicketDetailService.cs`
- `AnmolDristi/ITSM/BLL/TicketListService.cs`
- `AnmolDristi/ITSM/BLL/TicketService.cs`
- `AnmolDristi/ITSM/BLL/TicketUpdateService.cs`
- `AnmolDristi/ITSM/DAL/Entities/Ticket.cs`
- `AnmolDristi/ITSM/DAL/ItsmDbContext.cs`
- `AnmolDristi/ITSM/UI/TicketDetail.aspx`
- `AnmolDristi/ITSM/UI/TicketDetail.aspx.cs`
- `AnmolDristi/ITSM/UI/TicketDetail.aspx.designer.cs`
- `AnmolDristi/ITSM/UI/TicketList.aspx`
- `AnmolDristi/ITSM/UI/TicketList.aspx.cs`
- `AnmolDristi/ITSM/UI/TicketList.aspx.designer.cs`

### Untracked

- `AnmolDristi/Database/itsm_r6_support_groups.sql`
- `AnmolDristi/ITSM/BLL/SupportGroupAdminService.cs`
- `AnmolDristi/ITSM/BLL/SupportGroupRules.cs`
- `AnmolDristi/ITSM/DAL/Entities/SupportGroup.cs`
- `AnmolDristi/ITSM/DAL/Entities/SupportGroupMapping.cs`
- `AnmolDristi/ITSM/DAL/Entities/SupportGroupMember.cs`
- `AnmolDristi/ITSM/DAL/Migrations/202611261430000_AddSupportGroups.cs`
- `AnmolDristi/ITSM/DAL/Migrations/202611261430000_AddSupportGroups.Designer.cs`
- `AnmolDristi/ITSM/DAL/Migrations/202611261430000_AddSupportGroups.resx`
- `AnmolDristi/ITSM/UI/SupportGroupConfig.aspx`
- `AnmolDristi/ITSM/UI/SupportGroupConfig.aspx.cs`
- `AnmolDristi/ITSM/UI/SupportGroupConfig.aspx.designer.cs`
- `docs/itsm-r6-group-assignment-plan.md`
- `docs/itsm-r6-independent-review.md`
- `tests/ITSM.SupportGroupTests/` (csproj + `Program.cs`; local `bin/`/`obj/` build outputs may also appear)
- `tools/Ef6SnapshotRefresh/` (`Program.cs`, csproj, `App.config`)

### Untouched

- `AnmolDristi/Web.config` — not in `git status`; not read/edited/staged  
- `D:\PersonalRnD\AminrupWorks\Anmol_Digitization` — not touched (still `cursor/itsm-category-order-filters` with its own pre-existing dirty tree)

**Diffstat vs `f1f4bdb` (tracked modifications only):** 15 files, +483 / −118 (excludes untracked R6 sources and excludes `Web.config`).

No commit, push, merge, or PR from this pass.
