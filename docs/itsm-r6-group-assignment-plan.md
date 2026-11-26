# ITSM R6 Support Group Assignment — Plan (implementable)

| | |
| :--- | :--- |
| **Branch** | `cursor/itsm-r6-group-assignment` |
| **Base** | `Aug26_AILWorks_AmimnrupSupport` @ `f1f4bdb2c82013e913ac682aa5e31832a3fa94fc` |
| **Worktree** | `D:\PersonalRnD\AminrupWorks\Anmol_ITSM_R6` |
| **When** | 26 November 2026 (Asia/Calcutta) |
| **Readiness** | READY to implement under the approved rules below. Parts that this tip cannot express without inventing a permission key are recorded as limited, not guessed. |

This plan replaces the older BLOCKED store document (Parent-Category-only, member self-assign, schema unnamed). It is written from **this** tip’s source plus the approved R6 rules in the implement request.

---

## 1. Labels

| Kind | Meaning |
| :--- | :--- |
| **Inspected** | Observed in `f1f4bdb` source |
| **Approved** | Explicit owner rule in the implement request |
| **Inferred** | Required to make an approved rule executable; called out |
| **Blocker** | Approved rule impossible without further owner input; that part stops |

---

## 2. Approved rules (owner)

1. Group attaches at **Parent Category** (`itsm.MST_Category` / `Tickets.CategoryId`) **or** **Category** (`itsm.MST_SubCategory` / `Tickets.SubCategoryId`).
2. Precedence: **Category group → Parent Category group → existing Parent Category individual agent → existing unmapped handling**.
3. Group win → **unclaimed group queue**; **do not** assign the Team Leader.
4. **Team Leader** may **claim** or **assign an active member**.
5. **One** active Team Leader; **many** active members.
6. **Individual** `MST_SupportMapping` rows **stay**.

Phase constraints (approved by the same request): company + branch isolation; no active group without a leader; inactive members cannot be assigned; do not deactivate a group that still owns unresolved tickets unless a safe path exists; no conflicting individual and group targets at the same level; ticket group reference only if the ticket cannot represent the queue; no SQL/migrations executed on any database; no `Web.config` touch; no commit/push/PR.

---

## 3. Inspected facts on `f1f4bdb`

| Fact | Evidence |
| :--- | :--- |
| Dispatch returns one `SupportAgentCode` or null via Item → SubCategory → Category → BranchDefault | `DispatchService.ResolveAssignee` |
| Create writes that code to `Tickets.AssigneeCode` | `TicketService.CreateTicket` |
| No `TakeOwnership`, Unassigned tab, `IsMappedAgent`, `CategoryConfig`, or `tests/` | UI + BLL inventory |
| `ReassignTicket` allows any active employee once process ACL passes (requester or assignee) | `TicketUpdateService` |
| Session keys on ITSM pages: `WORKMAN` / `USERID` / `BRANCH` (ApprovalConfig: `USERID` + `BRANCH`) | code-behinds |
| Admin permission present: `ITSM.ApprovalConfig.Manage` (ITSMAdmin). **No** `ITSM.CategoryConfig.Manage`, **no** group permission key | `AnmolDristi/Database/*.sql` |
| `Tickets.AssigneeCode` nullable; `RowVersion` present; audit `"Reassignment"` path exists | entities + update service |
| No SupportGroup entities/DbSets/migrations | DAL |

**Baseline note (inspected, not a stop):** This tip is weaker than Digitization/`cr/itsm-r1-r5-uat` (no claim UI, no Unassigned tab, no CategoryConfig). R6 on this tip **adds** the minimum claim + group-queue + group-admin surfaces rather than editing PR #249.

---

## 4. Decisions

| Topic | Kind | Decision |
| :--- | :--- | :--- |
| Group tables | **Inferred** names under Phase 2 mandate | `itsm.MST_SupportGroup`, `itsm.MST_SupportGroupMember`, `itsm.MST_SupportGroupMapping` |
| Mapping mode | **Approved** levels | Parent Category: `CategoryId` set, `SubCategoryId` null. Category: `SubCategoryId` set (store parent `CategoryId` for display). Scoped by `CompanyId` + `BranchId` |
| Ticket group ref | **Inferred** (queue cannot be rebuilt safely from live mapping alone) | Nullable `Tickets.SupportGroupId` snapshot at create on group win |
| Group win assignee | **Approved** | `AssigneeCode = null`; do not write Team Leader |
| Claim / assign actors | **Approved** | Active Team Leader of that group only |
| Assign targets | **Approved** | Active members of that group (including TL claiming self) |
| Individual mappings | **Approved** | Unchanged table; used only when no group wins |
| Item / SubCategory individuals vs groups | **Inferred** from listed precedence | Groups at Category / Parent Category are evaluated **before** the existing individual cascade (including Item / SubCategory / Category / BranchDefault individuals) |
| Conflict rule | **Approved** | Reject saving an active group mapping when an active individual mapping exists at the same branch + same taxonomy level (Parent Category: cat set / sub null / item null; Category: cat+sub set / item null). Reject the reverse |
| Deactivate with open tickets | **Approved** | Block deactivate when any non-terminal ticket has that `SupportGroupId` |
| Admin permission | **Inspected** + request rule | No group permission in catalog. Gate new `SupportGroupConfig` with existing `ITSM.ApprovalConfig.Manage` (same ITSMAdmin grant). **Do not invent** a new key |
| Manager assign | Older BLOCKED plan | **Out of scope** here; no Manager `RoleKey` on this tip |
| Member self-assign without TL | Older plan | **Not** approved in this request; only TL claims |

---

## 5. Dispatch algorithm (implement)

Inputs: `companyId`, `branchId` (`MST_Branch.Id`), `categoryId`, `subCategoryId`, `itemId?`.

1. Active group mapping for `(CompanyId, BranchId, SubCategoryId = subCategoryId)` → `{ AssigneeCode: null, SupportGroupId }`
2. Else active group mapping for `(CompanyId, BranchId, CategoryId = categoryId, SubCategoryId IS NULL)` → same
3. Else existing individual cascade (Item → SubCategory → Category → BranchDefault) → `{ AssigneeCode, SupportGroupId: null }`
4. Else `{ null, null }` (unmapped)

Create persists `AssigneeCode` and `SupportGroupId` from that outcome. Status / ticket number / approvals unchanged.

---

## 6. Schema (Phase 2)

- `MST_SupportGroup`: `Id`, `CompanyId`, `BranchId`, `Name`, `IsActive`, audit columns
- `MST_SupportGroupMember`: `Id`, `SupportGroupId`, `EmployeeCode`, `IsTeamLeader`, `IsActive`, audit columns. Unique `(SupportGroupId, EmployeeCode)`. Filtered unique: one active `IsTeamLeader = 1` per group
- `MST_SupportGroupMapping`: `Id`, `CompanyId`, `BranchId`, `CategoryId`, `SubCategoryId?`, `SupportGroupId`, `IsActive`, audit columns. Filtered uniques for active Parent-Category and Category mappings
- `Tickets.SupportGroupId` nullable FK

Migration file under `AnmolDristi/ITSM/DAL/Migrations/` plus DBA script `AnmolDristi/Database/itsm_r6_support_groups.sql`. **Not run** on any database in this work.

---

## 7. BLL (Phase 3)

| Area | Change |
| :--- | :--- |
| Pure rules | `SupportGroupRules` — resolution, TL claim/assign predicates, conflict, leader, deactivate checks (no `System.Web`, testable without SQL) |
| `DispatchService` | `ResolveDispatch` using rules + EF `AsNoTracking` reads |
| `TicketService.CreateTicket` | Persist outcome; company on every path |
| `SupportGroupAdminService` | CRUD groups/members/mappings; company+branch on every op; flat DTO `Select` |
| `TicketUpdateService` | `TakeOwnership` (TL claim); extend reassign so TL may assign unassigned group ticket to active member; same transaction + audit |
| `TicketListService` | `GetGroupUnassignedQueue` for actor who is active member/TL of ticket’s group; R4-style sort (priority hours, `CreatedOn` desc, `Id` desc) |
| `TicketDetailService` | `CanTakeOwnership`, group member dropdown source |
| `ItsmTicketAccess` | Unassigned group ticket view for active group members (company first) |

---

## 8. UI (Phase 4)

| Page | Change |
| :--- | :--- |
| `SupportGroupConfig.aspx` | New admin page; session `USERID` + `BRANCH`; permission `ITSM.ApprovalConfig.Manage`; lightweight ViewState ids |
| `TicketDetail.aspx` | Keep `ddlReassignTo`, `btnReassign`, `lblAssignee`, `hfTicketId`, `hfRowVersion`. Add `pnlTakeOwnership` / `btnTakeOwnership` (absent on this tip — required for TL claim) |
| `TicketList.aspx` | Keep existing tab ids. Add `tabUnassigned` / `lnkUnassigned` for group unclaimed queue |

---

## 9. Tests (Phase 5) — 15 cases

Console project `tests/ITSM.SupportGroupTests` mirroring AttachmentTests style (Expect counters, compile pure BLL). Cases:

1. Category group beats Parent Category group  
2. Parent Category group wins when Category has none  
3. Category group beats Parent Category individual  
4. Parent Category individual wins when no groups  
5. Unmapped when nothing matches  
6. Group win leaves assignee null (not TL code)  
7. Individual Item mapping still used when no group wins  
8. Conflicting individual + group at same level rejected  
9. Second active Team Leader rejected  
10. Activate group without active TL rejected  
11. Inactive member cannot be assignment target  
12. Non-TL cannot claim  
13. Active TL can claim  
14. Active TL can assign active member  
15. Deactivate blocked while unresolved tickets reference the group  

No browser / IIS / database execution claimed.

---

## 10. Blockers / limits (stop that part only)

| Item | Status |
| :--- | :--- |
| Schema / BLL / claim / queue / admin under `ApprovalConfig.Manage` | Implement |
| New permission key for groups | **Not invented**; documented; closest existing admin permission used |
| Manager role assignment | **Stopped** — no Manager `RoleKey` on this tip |
| Applying migration / SQL to UAT or any DB | **Not run** (forbidden) |
| Digitization dirty Category Config worktree / PR #249 | **Untouched** |

---

## 11. File boundary

New: entities, migration (+ designer/resx), `SupportGroupRules`, `SupportGroupAdminService`, `SupportGroupConfig.aspx(+cs/designer)`, DBA SQL, tests, this plan.  
Edit: `ItsmDbContext`, `Ticket`, `DispatchService`, `TicketService`, `TicketUpdateService`, `TicketListService`, `TicketDetailService`, `ItsmTicketAccess`, `TicketDetail`/`TicketList` UI, `AnmolDristi.csproj`.  
Never: `AnmolDristi/Web.config`, Digitization checkout, PR #249.
